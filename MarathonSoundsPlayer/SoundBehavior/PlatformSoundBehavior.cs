using MarathonFileUtils.Sounds.DataStructures;
using MarathonSoundsPlayer.Interfaces;
using MarathonSoundsPlayer.SampleProviders;
using MarathonSoundsPlayer.SoundProperties;
using NAudio.Wave;
using System.Collections.Specialized;

namespace MarathonSoundsPlayer.SoundBehavior
{
    public class PlatformSoundBehavior : SoundBehaviorBase, ISoundBehavior, IDisposable
    {

        #region ISoundBehavior Members

        #region Used

        public SoundSlot PlatformStartExtendSound
        {
            get
            {
                lock (mSync)
                {
                    return mPlatformStartExtendSound;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mPlatformStartExtendSound = value;
                }
            }
        }

        public SoundSlot PlatformStartRetractSound
        {
            get
            {
                lock (mSync)
                {
                    return mPlatformStartRetractSound;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mPlatformStartRetractSound = value;
                }
            }
        }

        public SoundSlot PlatformStopExtendSound
        {
            get
            {
                lock (mSync)
                {
                    return mPlatformStopExtendSound;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mPlatformStopExtendSound = value;
                }
            }
        }

        public SoundSlot PlatformStopRetractSound
        {
            get
            {
                lock (mSync)
                {
                    return mPlatformStopRetractSound;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mPlatformStopRetractSound = value;
                }
            }
        }

        public SoundSlot PlatformRunningSound
        {
            get
            {
                lock (mSync)
                {
                    return mPlatformRunningSound;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mPlatformRunningSound = value;
                }
            }
        }

        public double RetractedVolume
        {
            get
            {
                lock (mSync)
                {
                    return mRetractedVolume;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mRetractedVolume = ClampFinite(value, 0.0, 1.0, nameof(value));
                }
            }
        }

        public double ExtendedVolume
        {
            get
            {
                lock (mSync)
                {
                    return mExtendedVolume;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mExtendedVolume = ClampFinite(value, 0.0, 1.0, nameof(value));
                }
            }
        }

        public double RetractedPan
        {
            get
            {
                lock (mSync)
                {
                    return mRetractedPan;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mRetractedPan = ClampFinite(value, -1.0, 1.0, nameof(value));
                }
            }
        }

        public double ExtendedPan
        {
            get
            {
                lock (mSync)
                {
                    return mExtendedPan;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mExtendedPan = ClampFinite(value, -1.0, 1.0, nameof(value));
                }
            }
        }

        public double ExtendSpeed_WU_s
        {
            get
            {
                lock (mSync)
                {
                    return mExtendSpeed_WU_s;
                }
            }
            set
            {
                ValidatePositiveFinite(value, nameof(value));

                lock (mSync)
                {
                    mExtendSpeed_WU_s = value;
                }

                WakeScheduler();
            }
        }

        public double RetractSpeed_WU_s
        {
            get
            {
                lock (mSync)
                {
                    return mRetractSpeed_WU_s;
                }
            }
            set
            {
                ValidatePositiveFinite(value, nameof(value));

                lock (mSync)
                {
                    mRetractSpeed_WU_s = value;
                }

                WakeScheduler();
            }
        }

        public double ExtendDelay_s
        {
            get
            {
                lock (mSync)
                {
                    return mExtendDelay_s;
                }
            }
            set
            {
                ValidatePositiveFinite(value, nameof(value));

                lock (mSync)
                {
                    mExtendDelay_s = value;
                }

                WakeScheduler();
            }
        }

        public double RetractDelay_s
        {
            get
            {
                lock (mSync)
                {
                    return mRetractDelay_s;
                }
            }
            set
            {
                ValidatePositiveFinite(value, nameof(value));

                lock (mSync)
                {
                    mRetractDelay_s = value;
                }

                WakeScheduler();
            }
        }

        public double Distance_WU
        {
            get
            {
                lock (mSync)
                {
                    return mDistance_WU;
                }
            }
            set
            {
                ValidatePositiveFinite(value, nameof(value));

                lock (mSync)
                {
                    mDistance_WU = value;
                }

                WakeScheduler();
            }
        }

        public double Period_s
        {
            get
            {
                lock (mSync)
                {
                    return mPeriod_s;
                }
            }
            set
            {
                ValidatePositiveFinite(value, nameof(value));

                lock (mSync)
                {
                    mPeriod_s = value;
                }

                WakeScheduler();
            }
        }

        public double RandomPeriod_s
        {
            get
            {
                lock (mSync)
                {
                    return mRandomPeriod_s;
                }
            }
            set
            {
                lock (mSync)
                {
                    mRandomPeriod_s = Math.Max(0.0, value);
                }

                WakeScheduler();
            }
        }

        public double RandomPeriodOffset_s
        {
            get
            {
                lock (mSync)
                {
                    return mRandomPeriodOffset_s;
                }
            }
            set
            {
                lock (mSync)
                {
                    mRandomPeriodOffset_s = value;
                }

                WakeScheduler();
            }
        }

        #endregion

        #region Unused

        public SoundSlot CurrentSound
        {
            get
            {
                return null;
            }
            set
            {

            }
        }
        public double Volume { get; set; }
        public double RandomVolume { get; set; }
        public double RandomVolumeOffset { get; set; }
        public double RandomPitch { get; set; }
        public double RandomPitchOffset { get; set; }
        public double Pan { get; set; }
        public double RandomPan { get; set; }
        public double RandomPanOffset { get; set; }
        public double Frequency_Hz { get; set; }
        public double RandomFrequency_Hz { get; set; }
        public double RandomFrequencyOffset_Hz { get; set; }

        #endregion

        #endregion

        #region Private Properties

        private SoundSlot mPlatformStartExtendSound;
        private SoundSlot mPlatformStartRetractSound;
        private SoundSlot mPlatformStopExtendSound;
        private SoundSlot mPlatformStopRetractSound;
        private SoundSlot mPlatformRunningSound;
        private double mRetractedVolume;
        private double mExtendedVolume;
        private double mRetractedPan;
        private double mExtendedPan;
        private double mExtendSpeed_WU_s;
        private double mRetractSpeed_WU_s;
        private double mExtendDelay_s;
        private double mRetractDelay_s;
        private double mDistance_WU;
        private double mPeriod_s;
        private double mRandomPeriod_s;
        private double mRandomPeriodOffset_s;

        #endregion

        #region Constructor

        public PlatformSoundBehavior(SoundPropertiesPlatform properties) :
            base(properties)
        {
            ArgumentNullException.ThrowIfNull(properties.PlatformStartExtendSound);
            ArgumentNullException.ThrowIfNull(properties.PlatformStartRetractSound);
            ArgumentNullException.ThrowIfNull(properties.PlatformStopExtendSound);
            ArgumentNullException.ThrowIfNull(properties.PlatformStopRetractSound);
            ArgumentNullException.ThrowIfNull(properties.PlatformRunningSound);

            ValidatePositiveFinite(properties.RetractedVolume, nameof(properties.RetractedVolume));
            ValidatePositiveFinite(properties.ExtendedVolume, nameof(properties.ExtendedVolume));
            ValidateFinite(properties.RetractedPan, nameof(properties.RetractedPan));
            ValidateFinite(properties.ExtendedPan, nameof(properties.ExtendedPan));
            ValidatePositiveFinite(properties.ExtendSpeed_WU_s, nameof(properties.ExtendSpeed_WU_s));
            ValidatePositiveFinite(properties.RetractSpeed_WU_s, nameof(properties.RetractSpeed_WU_s));
            ValidatePositiveFinite(properties.ExtendDelay_s, nameof(properties.ExtendDelay_s));
            ValidatePositiveFinite(properties.RetractDelay_s, nameof(properties.RetractDelay_s));
            ValidatePositiveFinite(properties.Distance_WU, nameof(properties.Distance_WU));
            ValidatePositiveFinite(properties.Period_s, nameof(properties.Period_s));
            ValidateFinite(properties.RandomPeriod_s, nameof(properties.RandomPeriod_s));
            ValidateFinite(properties.RandomPeriodOffset_s, nameof(properties.RandomPeriodOffset_s));

            mPlatformStartExtendSound = properties.PlatformStartExtendSound;
            mPlatformStartRetractSound = properties.PlatformStartRetractSound;
            mPlatformStopExtendSound = properties.PlatformStopExtendSound;
            mPlatformStopRetractSound = properties.PlatformStopRetractSound;
            mPlatformRunningSound = properties.PlatformRunningSound;
            mSoundCatalog.Add("PlatformStartExtendSound", mPlatformStartExtendSound);
            mSoundCatalog.Add("PlatformStartRetractSound", mPlatformStartRetractSound);
            mSoundCatalog.Add("PlatformStopExtendSound", mPlatformStopExtendSound);
            mSoundCatalog.Add("PlatformStopRetractSound", mPlatformStopRetractSound);
            mSoundCatalog.Add("PlatformRunningSound", mPlatformRunningSound);

            mExtendSpeed_WU_s = properties.ExtendSpeed_WU_s;
            mRetractSpeed_WU_s = properties.RetractSpeed_WU_s;
            mExtendDelay_s = properties.ExtendDelay_s;
            mRetractDelay_s = properties.RetractDelay_s;
            mDistance_WU = properties.Distance_WU;
            mPeriod_s = properties.Period_s;

            Effects.CollectionChanged += Effects_CollectionChanged;

            mRetractedVolume = ClampFinite(properties.RetractedVolume, 0.0, 1.0, nameof(properties.RetractedVolume));
            mExtendedVolume = ClampFinite(properties.ExtendedVolume, 0.0, 1.0, nameof(properties.ExtendedVolume));
            mRetractedPan = ClampFinite(properties.RetractedPan, -1.0, 1.0, nameof(properties.RetractedPan));
            mExtendedPan = ClampFinite(properties.ExtendedPan, -1.0, 1.0, nameof(properties.ExtendedPan));

            mRandomPeriod_s = Math.Max(0.0, properties.RandomPeriod_s);
            mRandomPeriodOffset_s = properties.RandomPeriodOffset_s;
        }

        #endregion

        #region Public Members

        public void Start()
        {
            lock (mSync)
            {
                if (mSchedulerTask != null && !mSchedulerTask.IsCompleted)
                {
                    return;
                }

                mSchedulerCancellation = new CancellationTokenSource();

                CancellationToken token = mSchedulerCancellation.Token;

                // Set the base started state and play the first trigger
                // immediately.
                lock (mSync)
                {
                    ThrowIfDisposed();

                    if (mPlaying)
                    {
                        return;
                    }

                    mPlaying = true;
                }

                try
                {
                    mSoundEngine.ActivateSource(this);
                    PlayPlatformStartStopSound(PlatformStartStopType.StartExtend);
                }
                catch
                {
                    lock (mSync)
                    {
                        mPlaying = false;
                    }

                    mSoundEngine.DeactivateSource(this);

                    throw;
                }

                mSchedulerTask = Task.Run(() => RunSchedulerAsync(token), token);
            }
        }

        public void Stop()
        {
            CancellationTokenSource cancellation;
            Task schedulerTask;

            lock (mSync)
            {
                cancellation = mSchedulerCancellation;
                schedulerTask = mSchedulerTask;

                mSchedulerCancellation = null;
                mSchedulerTask = null;
            }

            if (cancellation != null)
            {
                cancellation.Cancel();
            }

            lock (mSync)
            {
                ThrowIfDisposed();

                mPlaying = false;
            }

            mSoundEngine.DeactivateSource(this);

            if (cancellation != null)
            {
                cancellation.Dispose();
            }
        }

        public virtual void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Members

        private void Effects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            lock (mSync)
            {
                ThrowIfDisposed();
            }
        }

        private void PlayPlatformStartStopSound(PlatformStartStopType platformEvent)
        {
            MixerVoice newVoice;

            lock (mSync)
            {
                ThrowIfDisposed();

                if (!mPlaying)
                {
                    return;
                }

                SoundSlot eventSound;

                switch (platformEvent)
                {
                    case PlatformStartStopType.StartExtend:
                        eventSound = mPlatformStartExtendSound;
                        break;
                    case PlatformStartStopType.StartRetract:
                        eventSound = mPlatformStartRetractSound;
                        break;
                    case PlatformStartStopType.StopExtend:
                        eventSound = mPlatformStopExtendSound;
                        break;
                    case PlatformStartStopType.StopRetract:
                        eventSound = mPlatformStopRetractSound;
                        break;
                    default:
                        throw new ArgumentException("Invalid platform event");
                }

                SoundPermutation selectedSound = GetRandomElement(eventSound.Samples);
                PlaybackParameters parameters = new PlaybackParameters();

                // Construct the replacement before removing the current input.
                // This minimizes the replacement interval.
                newVoice = CreateVoice(selectedSound, parameters);
            }

            try
            {
                mSoundEngine.AddInput(newVoice.Provider);
            }
            catch
            {
                lock (mSync)
                {

                }

                newVoice.Dispose();
                throw;
            }
        }

        private void VoiceFinished(MixerVoice finishedVoice)
        {
            if (finishedVoice == null)
            {
                return;
            }

            // AutoDisposeSampleProvider calls this from inside the audio mixer's
            // Read() operation. Do not dispose the current provider or add a new
            // mixer input until that Read() has returned.
            ThreadPool.QueueUserWorkItem(delegate { FinishVoiceOutsideMixerRead(finishedVoice); });
        }

        private MixerVoice CreateVoice(SoundPermutation sound, PlaybackParameters parameters)
        {
            ArgumentNullException.ThrowIfNull(sound);

            if (sound.Data == null)
            {
                throw new ArgumentNullException(nameof(sound.Data));
            }

            if (sound.Header == null)
            {
                throw new ArgumentNullException(nameof(sound.Header));
            }

            MemoryStream memoryStream = null;
            WaveFileReader waveReader = null;

            try
            {
                memoryStream = MarathonFileUtils.Sounds.Utils.CreateWaveStream(sound.Data, sound.Header);

                if (memoryStream == null)
                {
                    throw new InvalidOperationException("CreateWaveStream returned null.");
                }

                memoryStream.Position = 0;

                waveReader = new WaveFileReader(memoryStream);
                ISampleProvider sourceProvider = waveReader.ToSampleProvider();

                PitchAndSpeedSampleProvider pitchProvider = new PitchAndSpeedSampleProvider(sourceProvider, (float)parameters.Pitch);

                VolumePanSampleProvider volumePanProvider = new VolumePanSampleProvider(pitchProvider)
                {
                    Volume = (float)parameters.Volume,
                    Pan = (float)parameters.Pan,
                };

                ISampleProvider engineFormatProvider = ConvertToEngineFormat(volumePanProvider);

                MixerVoice voice = null;

                AutoDisposeSampleProvider endingProvider = new AutoDisposeSampleProvider(engineFormatProvider, delegate { VoiceFinished(voice); });

                voice = new MixerVoice(memoryStream, waveReader, pitchProvider, volumePanProvider, engineFormatProvider, endingProvider, parameters);

                return voice;
            }
            catch
            {
                if (waveReader != null)
                {
                    waveReader.Dispose();
                }

                if (memoryStream != null)
                {
                    memoryStream.Dispose();
                }

                throw;
            }
        }

        private void FinishVoiceOutsideMixerRead(MixerVoice finishedVoice)
        {
            bool continuePlayback;

            lock (mSync)
            {
                continuePlayback = mPlaying && !mDisposed;
            }

            // The mixer has already removed this provider after it returned zero.
            // It is now safe to dispose the WAV reader and MemoryStream.
            finishedVoice.Dispose();

            if (!continuePlayback)
            {
                return;
            }

            try
            {
                OnVoiceFinished(null);
            }
            catch (ObjectDisposedException)
            {
                // Stop() or Dispose() raced with natural completion.
            }
            catch (Exception exception)
            {
                OnPlaybackError(exception);
            }
        }

        private async Task RunSchedulerAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && Playing)
                {
                    TimeSpan delay = GetNextTriggerDelay();

                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested || !Playing)
                    {
                        break;
                    }

                    // This starts a new voice without stopping any voice that is
                    // already running. Random volume, pitch, and pan are selected
                    // independently for this trigger.
                    PlayPlatformStartStopSound(PlatformStartStopType.StopExtend);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when Stop() is called.
            }
            catch (ObjectDisposedException)
            {
                // Expected if disposal races with the scheduler.
            }
            catch (Exception exception)
            {
                OnPlaybackError(exception);
            }
        }

        private TimeSpan GetNextTriggerDelay()
        {
            double basePeriod_s;
            double randomPeriod_s;
            double randomPeriodOffset_s;

            lock (mSync)
            {
                basePeriod_s = mPeriod_s;
                randomPeriod_s = mRandomPeriod_s;
                randomPeriodOffset_s = mRandomPeriodOffset_s;
            }

            double effectivePeriod_s = basePeriod_s + randomPeriodOffset_s + NextRandomSignedValue(randomPeriod_s);
            return TimeSpan.FromSeconds(effectivePeriod_s);
        }

        private void WakeScheduler()
        {
            /*
             * Property changes affect the next scheduled period. The currently
             * active Task.Delay is not interrupted.
             *
             * If you want changes to reset the current delay immediately, use a
             * separate scheduler wake CancellationTokenSource.
             */
        }

        #endregion

    }
}
