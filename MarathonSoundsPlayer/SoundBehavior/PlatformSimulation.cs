using System.Diagnostics;

namespace MarathonSoundsPlayer.SoundBehavior
{
    public class PlatformSimulation
    {

        #region Public Properties

        public double Distance_WU
        {
            get { return mDistance_WU; }
        }

        public double Position { get; private set; }

        public double Speed_WU_s
        {
            get { return mSpeed_WU_s; }
        }

        public bool IsRunning
        {
            get { return mState != PlatformState.Stopped; }
        }

        public bool IsMoving
        {
            get
            {
                return mState == PlatformState.Retracting || mState == PlatformState.Extending;
            }
        }

        public TimeSpan OneWayTravelTime
        {
            get { return TimeSpan.FromSeconds(Distance_WU / Speed_WU_s); }
        }

        public TimeSpan FullCycleTime
        {
            get
            {
                double travelSeconds = Distance_WU / Speed_WU_s;
                return TimeSpan.FromSeconds((travelSeconds * 2.0) + mExtendDelay_s + mRetractDelay_s);
            }
        }

        #endregion

        #region Private Properties

        private const double mPositionTolerance = 1e-12;
        private const int mMaximumTransitionsPerAdvance = 100000;
        private readonly double mDistance_WU;
        private readonly double mSpeed_WU_s;
        private readonly double mExtendDelay_s;
        private readonly double mRetractDelay_s;
        private readonly Action mStopRetract;
        private readonly Action mStartExtend;
        private readonly Action mStopExtend;
        private readonly Action mStartRetract;
        private PlatformState mState;
        private double mRemainingDelay_s;
        private readonly object mLock = new object();
        private CancellationTokenSource mCts;
        private Task mRunTask;

        #endregion

        #region Constructor

        public PlatformSimulation(double distance_WU, double speed_WU_s, double extendDelay_s, double retractDelay_s, Action startExtend, Action stopExtend, Action startRetract, Action stopRetract)
        {
            if (double.IsNaN(distance_WU) || double.IsInfinity(distance_WU))
            {
                throw new ArgumentOutOfRangeException(nameof(distance_WU));
            }

            if (double.IsNaN(speed_WU_s) || double.IsInfinity(speed_WU_s) || speed_WU_s <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(speed_WU_s), "Platform speed must be greater than zero.");
            }

            if (extendDelay_s < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(extendDelay_s));
            }

            if (retractDelay_s < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retractDelay_s));
            }

            mDistance_WU = distance_WU;
            mSpeed_WU_s = speed_WU_s;
            mExtendDelay_s = extendDelay_s;
            mRetractDelay_s = retractDelay_s;

            mStartExtend = startExtend;
            mStopExtend = stopExtend;
            mStartRetract = startRetract;
            mStopRetract = stopRetract;

            Position = 0;
            mState = PlatformState.Stopped;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Starts the simulation and continuously advances it using real elapsed time.
        /// The returned Task completes when Stop(), Reset(), or cancellation is requested.
        /// </summary>
        public Task RunAsync(TimeSpan updateInterval, CancellationToken cancellationToken)
        {
            if (updateInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(updateInterval));
            }

            lock (mLock)
            {
                if (mRunTask != null && !mRunTask.IsCompleted)
                {
                    return mRunTask;
                }

                mCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Start();

                mRunTask = RunLoopAsync(updateInterval, mCts.Token);

                return mRunTask;
            }
        }

        public Task RunAsync(TimeSpan updateInterval)
        {
            return RunAsync(updateInterval, CancellationToken.None);
        }

        private async Task RunLoopAsync(TimeSpan updateInterval, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            TimeSpan previousTime = stopwatch.Elapsed;

            try
            {
                while (!cancellationToken.IsCancellationRequested && IsRunning)
                {
                    await Task.Delay(updateInterval, cancellationToken).ConfigureAwait(false);

                    TimeSpan currentTime = stopwatch.Elapsed;
                    TimeSpan elapsed = currentTime - previousTime;
                    previousTime = currentTime;

                    Advance(elapsed);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when Stop(), Reset(), or external cancellation occurs.
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            Position = 0;
            mState = PlatformState.ExtendedDelay;
            mRemainingDelay_s = mExtendDelay_s;

            Invoke(mStopRetract);
        }

        public void Stop()
        {
            CancellationTokenSource cancellation = null;

            lock (mLock)
            {
                cancellation = mCts;
                mCts = null;

                mState = PlatformState.Stopped;
                mRemainingDelay_s = 0.0;
            }

            if (cancellation != null)
            {
                cancellation.Cancel();
                cancellation.Dispose();
            }
        }

        public void Reset()
        {
            CancellationTokenSource cancellation = null;

            lock (mLock)
            {
                cancellation = mCts;
                mCts = null;

                mState = PlatformState.Stopped;
                mRemainingDelay_s = 0.0;
                Position = 0;
            }

            if (cancellation != null)
            {
                cancellation.Cancel();
                cancellation.Dispose();
            }
        }

        public void Advance(TimeSpan elapsedTime)
        {
            if (elapsedTime < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedTime));
            }

            if (!IsRunning)
            {
                return;
            }

            double remainingSeconds = elapsedTime.TotalSeconds;
            int transitionCount = 0;

            /*
             * Use a do/while loop so zero-duration delays can transition even
             * when Advance(TimeSpan.Zero) is called.
             */
            bool processAgain;

            do
            {
                processAgain = false;

                transitionCount++;

                if (transitionCount > mMaximumTransitionsPerAdvance)
                {
                    throw new InvalidOperationException("Too many simulation transitions occurred during one Advance call. Use a smaller elapsed-time increment.");
                }

                switch (mState)
                {
                    case PlatformState.ExtendedDelay:
                        processAgain = ProcessStartDelay(ref remainingSeconds);
                        break;

                    case PlatformState.Extending:
                        processAgain = ProcessMovement(ref remainingSeconds, mDistance_WU, PlatformState.RetractedDelay, mRetractDelay_s, mStopExtend);
                        break;

                    case PlatformState.RetractedDelay:
                        processAgain = ProcessStopDelay(ref remainingSeconds);
                        break;

                    case PlatformState.Retracting:
                        processAgain = ProcessMovement(ref remainingSeconds, 0, PlatformState.ExtendedDelay, mExtendDelay_s, mStopRetract);
                        break;

                    case PlatformState.Stopped:
                        return;

                    default:
                        throw new InvalidOperationException("Unknown simulation state.");
                }
            }
            while (processAgain);
        }

        #endregion

        private bool ProcessStartDelay(ref double availableSeconds)
        {
            if (!ConsumeDelay(ref availableSeconds))
            {
                return false;
            }

            Invoke(mStartExtend);
            mState = PlatformState.Extending;

            return true;
        }

        private bool ProcessStopDelay(ref double availableSeconds)
        {
            if (!ConsumeDelay(ref availableSeconds))
            {
                return false;
            }

            Invoke(mStartRetract);
            mState = PlatformState.Retracting;

            return true;
        }

        private bool ConsumeDelay(ref double availableSeconds)
        {
            if (mRemainingDelay_s <= 0.0)
            {
                return true;
            }

            if (availableSeconds < mRemainingDelay_s)
            {
                mRemainingDelay_s -= availableSeconds;
                availableSeconds = 0.0;
                return false;
            }

            availableSeconds -= mRemainingDelay_s;
            mRemainingDelay_s = 0.0;

            return true;
        }

        private bool ProcessMovement(ref double availableSeconds, double destination, PlatformState arrivalState, double arrivalDelaySeconds, Action arrivalCallback)
        {
            double remainingDistance = Math.Abs(destination - Position);

            if (remainingDistance <= mPositionTolerance)
            {
                Position = destination;
                mState = arrivalState;
                mRemainingDelay_s = arrivalDelaySeconds;

                Invoke(arrivalCallback);
                return true;
            }

            double requiredSeconds = remainingDistance / mSpeed_WU_s;

            if (availableSeconds < requiredSeconds)
            {
                double direction = Math.Sign(destination - Position);
                Position += direction * mSpeed_WU_s * availableSeconds;

                availableSeconds = 0.0;
                return false;
            }

            availableSeconds -= requiredSeconds;
            Position = destination;

            mState = arrivalState;
            mRemainingDelay_s = arrivalDelaySeconds;

            Invoke(arrivalCallback);
            return true;
        }

        private static void Invoke(Action callback)
        {
            if (callback != null)
            {
                callback();
            }
        }

    }
}
