using MarathonFileUtils.Sounds;
using NAudio.Effects;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;

namespace MarathonSoundsPlayer
{
    public class PlatformSoundSource : IDisposable
    {

        #region Public Properties

        public SoundsFileFormat Format
        {
            get
            {
                return mFormat;
            }
        }

        public SoundSlot PlatformStartOpenSound
        {
            get
            {
                lock (mSync)
                {
                    return mPlatformStartOpenSound;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mPlatformStartOpenSound = value;
                }
            }
        }

        public SoundSlot PlatformStartCloseSound
        {
            get
            {
                lock (mSync)
                {
                    return mPlatformStartCloseSound;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mPlatformStartCloseSound = value;
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

        public SoundSlot PlatformStopOpenSound
        {
            get
            {
                lock (mSync)
                {
                    return mPlatformStopOpenSound;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mPlatformStopOpenSound = value;
                }
            }
        }

        public SoundSlot PlatformStopCloseSound
        {
            get
            {
                lock (mSync)
                {
                    return mPlatformStopCloseSound;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mPlatformStopCloseSound = value;
                }
            }
        }

        public double StartVolume
        {
            get
            {
                lock (mSync)
                {
                    return mStartVolume;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mStartVolume = ClampFinite(value, 0.0, 1.0, nameof(value));
                    UpdateActiveVoice();
                }
            }
        }

        public double StopVolume
        {
            get
            {
                lock (mSync)
                {
                    return mStopVolume;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mStopVolume = ClampFinite(value, 0.0, 1.0, nameof(value));
                    UpdateActiveVoice();
                }
            }
        }

        public double Pitch
        {
            get
            {
                lock (mSync)
                {
                    return mPitch;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mPitch = ClampFinite(value, mMinimumPitch, mMaximumPitch, nameof(value));
                    UpdateActiveVoice();
                }
            }
        }

        public double StartPan
        {
            get
            {
                lock (mSync)
                {
                    return mStartPan;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mStartPan = ClampFinite(value, -1.0, 1.0, nameof(value));
                    UpdateActiveVoice();
                }
            }
        }

        public double StopPan
        {
            get
            {
                lock (mSync)
                {
                    return mStopPan;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mStopPan = ClampFinite(value, -1.0, 1.0, nameof(value));
                    UpdateActiveVoice();
                }
            }
        }

        public bool Playing
        {
            get
            {
                lock (mSync)
                {
                    return mPlaying && !mDisposed;
                }
            }
        }

        public double OpenPlatformSpeed_WU_s
        {
            get
            {
                lock (mSync)
                {
                    return mOpenPlatformSpeed_WU_s;
                }
            }
            set
            {
                ValidatePositiveFinite(value, nameof(value));

                lock (mSync)
                {
                    mOpenPlatformSpeed_WU_s = value;
                }

                WakeScheduler();
            }
        }

        public double ClosePlatformSpeed_WU_s
        {
            get
            {
                lock (mSync)
                {
                    return mClosePlatformSpeed_WU_s;
                }
            }
            set
            {
                ValidatePositiveFinite(value, nameof(value));

                lock (mSync)
                {
                    mClosePlatformSpeed_WU_s = value;
                }

                WakeScheduler();
            }
        }

        public ObservableCollection<IAudioEffect> Effects
        {
            get;
            set;
        }

        #endregion

        #region Private Properties

        private const double mMinimumPitch = 0.5;
        private const double mMaximumPitch = 2.0;
        private SoundsFileFormat mFormat;
        private SoundSourceType mSourceType;
        private readonly object mSync = new object();
        private readonly Random mRng = new Random();
        private double mStartVolume;
        private double mStopVolume;
        private double mPitch;
        private double mStartPan;
        private double mStopPan;
        private bool mPlaying;
        private double mOpenPlatformSpeed_WU_s;
        private double mClosePlatformSpeed_WU_s;
        private double mOpenDelay_s;
        private double mCloseDelay_s;
        private double mPeriod_s;
        private double mRandomPeriod_s;
        private double mRandomPeriodOffset_s;
        private SoundSlot mPlatformStartOpenSound;
        private SoundSlot mPlatformStartCloseSound;
        private SoundSlot mPlatformRunningSound;
        private SoundSlot mPlatformStopOpenSound;
        private SoundSlot mPlatformStopCloseSound;
        private SoundEngine mSoundEngine;
        private MixerVoice mActiveVoice;
        private CancellationTokenSource mSchedulerCancellation;
        private Task mSchedulerTask;
        private bool mDisposed;

        #endregion

        #region Constructor

        public PlatformSoundSource(SoundSourceType type, SoundsFileFormat format, SoundEngine soundEngine,
            SoundSlot platformStartOpenSound, SoundSlot platformStartCloseSound, SoundSlot platformRunningSound, SoundSlot platformStopOpenSound, SoundSlot platformStopCloseSound,
            double startVolume, double stopVolume, double pitch, double startPan, double stopPan, double openPlatformSpeed_WU_s, double closePlatformSpeed_WU_s, double openDelay_s, double closeDelay_s, double period_s, double randomPeriod_s, double randomPeriodOffset_s)
        {
            ArgumentNullException.ThrowIfNull(soundEngine);
            ArgumentNullException.ThrowIfNull(platformStartOpenSound);
            ArgumentNullException.ThrowIfNull(platformStartCloseSound);
            ArgumentNullException.ThrowIfNull(platformRunningSound);
            ArgumentNullException.ThrowIfNull(platformStopOpenSound);
            ArgumentNullException.ThrowIfNull(platformStopCloseSound);

            ValidatePositiveFinite(startVolume, nameof(startVolume));
            ValidatePositiveFinite(stopVolume, nameof(stopVolume));
            ValidatePositiveFinite(pitch, nameof(pitch));
            ValidateFinite(startPan, nameof(startPan));
            ValidateFinite(stopPan, nameof(stopPan));
            ValidatePositiveFinite(openPlatformSpeed_WU_s, nameof(openPlatformSpeed_WU_s));
            ValidatePositiveFinite(closePlatformSpeed_WU_s, nameof(closePlatformSpeed_WU_s));
            ValidatePositiveFinite(openDelay_s, nameof(openDelay_s));
            ValidatePositiveFinite(closeDelay_s, nameof(closeDelay_s));
            ValidatePositiveFinite(period_s, nameof(period_s));
            ValidateFinite(randomPeriod_s, nameof(randomPeriod_s));
            ValidateFinite(randomPeriodOffset_s, nameof(randomPeriodOffset_s));

            mSoundEngine = soundEngine;
            mSourceType = type;
            mFormat = format;
            mPlatformStartOpenSound = platformStartOpenSound;
            mPlatformStartCloseSound = platformStartCloseSound;
            mPlatformRunningSound = platformRunningSound;
            mPlatformStopOpenSound = platformStopOpenSound;
            mPlatformStopCloseSound = platformStopCloseSound;
            mStartVolume = startVolume;
            mStopVolume = stopVolume;
            mPitch = pitch;
            mStartPan = startPan;
            mStopPan = stopPan;
            mOpenPlatformSpeed_WU_s = openPlatformSpeed_WU_s;
            mClosePlatformSpeed_WU_s = closePlatformSpeed_WU_s;
            mOpenDelay_s = openDelay_s;
            mCloseDelay_s = closeDelay_s;
            mPeriod_s = period_s;

            Effects = new ObservableCollection<IAudioEffect>();
            Effects.CollectionChanged += Effects_CollectionChanged;

            mStartVolume = ClampFinite(startVolume, 0.0, 1.0, nameof(startVolume));
            mStopVolume = ClampFinite(stopVolume, 0.0, 1.0, nameof(stopVolume));
            mPitch = ClampFinite(pitch, mMinimumPitch, mMaximumPitch, nameof(pitch));
            mStartPan = ClampFinite(startPan, -1.0, 1.0, nameof(startPan));
            mStopPan = ClampFinite(stopPan, -1.0, 1.0, nameof(stopPan));

            mRandomPeriod_s = Math.Max(0.0, randomPeriod_s);
            mRandomPeriodOffset_s = randomPeriodOffset_s;
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
                    PlayPlatformStartStopSound(PlatformStartStopType.StartOpen);
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

            // Do not synchronously wait on schedulerTask here. Stop() may be
            // called from a UI thread, and waiting is unnecessary because the
            // cancellation token ends the scheduler promptly.
            MixerVoice voice;

            lock (mSync)
            {
                ThrowIfDisposed();

                mPlaying = false;

                voice = mActiveVoice;
                mActiveVoice = null;
            }

            if (voice != null)
            {
                mSoundEngine.RemoveInput(voice.Provider);
                voice.Dispose();
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
                UpdateActiveVoice();
            }
        }

        private void PlayPlatformStartStopSound(PlatformStartStopType platformEvent)
        {
            MixerVoice previousVoice;
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
                    case PlatformStartStopType.StartOpen:
                        eventSound = mPlatformStartOpenSound;
                        break;
                    case PlatformStartStopType.StartClose:
                        eventSound = mPlatformStartOpenSound;
                        break;
                    case PlatformStartStopType.StopOpen:
                        eventSound = mPlatformStartOpenSound;
                        break;
                    case PlatformStartStopType.StopClosed:
                        eventSound = mPlatformStartOpenSound;
                        break;
                    default:
                        throw new ArgumentException("Invalid platform event");
                }

                SoundPermutation selectedSound = GetRandomElement(eventSound.Samples);
                PlaybackParameters parameters = new PlaybackParameters();

                // Construct the replacement before removing the current input.
                // This minimizes the replacement interval.
                newVoice = CreateVoice(selectedSound, parameters);

                previousVoice = mActiveVoice;
                mActiveVoice = newVoice;
            }

            if (previousVoice != null)
            {
                mSoundEngine.RemoveInput(previousVoice.Provider);
                previousVoice.Dispose();
            }

            try
            {
                mSoundEngine.AddInput(newVoice.Provider);
            }
            catch
            {
                lock (mSync)
                {
                    if (ReferenceEquals(mActiveVoice, newVoice))
                    {
                        mActiveVoice = null;
                    }
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

        /// <summary>
        /// Called after a voice finishes naturally or stops with an error.
        /// </summary>
        private void OnVoiceFinished(Exception exception)
        {
            if (exception != null)
            {
                OnPlaybackError(exception);
            }
        }

        private void OnPlaybackError(Exception exception)
        {
            // Add application logging here if desired.
            // Debug.WriteLine(exception);
        }

        private double NextRandomSignedValue(double amount)
        {
            lock (mSync)
            {
                ValidateFinite(amount, nameof(amount));

                if (amount <= 0.0)
                {
                    return 0.0;
                }

                return ((mRng.NextDouble() * 2.0) - 1.0) * amount;
            }
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            if (value > maximum)
            {
                return maximum;
            }

            return value;
        }

        private static void ValidateFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "The value must be a finite number.");
            }
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

        private void UpdateActiveVoice()
        {
            MixerVoice voice = mActiveVoice;

            if (voice == null)
            {
                return;
            }

            double volumeRandomContribution = voice.Parameters.Volume - voice.Parameters.BaseVolumeAtStart;
            double pitchRandomContribution = voice.Parameters.Pitch - voice.Parameters.BasePitchAtStart;
            double panRandomContribution = voice.Parameters.Pan - voice.Parameters.BasePanAtStart;

            voice.Parameters.Pitch = Clamp(mPitch + pitchRandomContribution, mMinimumPitch, mMaximumPitch);

            voice.Parameters.BasePitchAtStart = mPitch;

            voice.VolumePanProvider.Volume = (float)voice.Parameters.Volume;
            voice.PitchProvider.PitchFactor = (float)voice.Parameters.Pitch;
            voice.VolumePanProvider.Pan = (float)voice.Parameters.Pan;

            voice.Effects.Clear();

            foreach (IAudioEffect effect in Effects)
            {
                voice.Effects.Add(effect);
            }
        }

        private double GetRandomSignedValueUnsafe(double amount)
        {
            if (amount <= 0.0)
            {
                return 0.0;
            }

            return ((mRng.NextDouble() * 2.0) - 1.0) * amount;
        }

        private T GetRandomElement<T>(IList<T> list)
        {
            ArgumentNullException.ThrowIfNull(list);

            if (list.Count == 0)
            {
                throw new InvalidOperationException("Cannot select an element from an empty list.");
            }

            return list[mRng.Next(list.Count)];
        }

        private static double ClampFinite(double value, double minimum, double maximum, string parameterName)
        {
            ValidateFinite(value, parameterName);
            return Clamp(value, minimum, maximum);
        }

        private void ThrowIfDisposed()
        {
            if (mDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundSource));
            }
        }

        private ISampleProvider ConvertToEngineFormat(ISampleProvider source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            ISampleProvider converted = source;

            // VolumePanSampleProvider should already output stereo, but retain this
            // validation in case the provider chain changes later.
            if (converted.WaveFormat.Channels == 1)
            {
                converted = new MonoToStereoSampleProvider(converted);
            }
            else if (converted.WaveFormat.Channels != 2)
            {
                throw new NotSupportedException("Only mono and stereo audio is supported.");
            }

            if (converted.WaveFormat.SampleRate != mSoundEngine.WaveFormat.SampleRate)
            {
                converted = new WdlResamplingSampleProvider(converted, mSoundEngine.WaveFormat.SampleRate);
            }

            return converted;
        }

        private void FinishVoiceOutsideMixerRead(MixerVoice finishedVoice)
        {
            bool continuePlayback;

            lock (mSync)
            {
                // If another sound was already assigned, this completion belongs
                // to an obsolete voice and must not start another sound.
                if (!ReferenceEquals(mActiveVoice, finishedVoice))
                {
                    finishedVoice.Dispose();
                    return;
                }

                mActiveVoice = null;

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

        private bool TrySetStarted()
        {
            lock (mSync)
            {
                ThrowIfDisposed();

                if (mPlaying)
                {
                    return false;
                }

                mPlaying = true;

                return true;
            }
        }

        private void SetStopped()
        {
            lock (mSync)
            {
                mPlaying = false;
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
                    PlayPlatformStartStopSound(PlatformStartStopType.StopOpen);
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

        private static void ValidatePositiveFinite(double value, string parameterName)
        {
            ValidateFinite(value, parameterName);

            if (value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Frequency must be greater than zero.");
            }
        }

        #endregion

    }
}
