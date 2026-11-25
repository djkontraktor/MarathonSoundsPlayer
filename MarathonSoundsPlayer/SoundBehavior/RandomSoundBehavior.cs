using MarathonFileUtils.Sounds.DataStructures;
using MarathonSoundsPlayer.Interfaces;
using MarathonSoundsPlayer.SampleProviders;
using MarathonSoundsPlayer.SoundProperties;
using NAudio.Wave;
using System.Collections.Specialized;

namespace MarathonSoundsPlayer.SoundBehavior
{
    public class RandomSoundBehavior : EnvironmentalSoundBehavior, ISoundBehavior
    {

        #region ISoundBehavior Members

        #region Used

        public double Frequency_Hz
        {
            get
            {
                lock (mSync)
                {
                    return mFrequency_Hz;
                }
            }
            set
            {
                ValidatePositiveFinite(value, nameof(value));

                lock (mSync)
                {
                    mFrequency_Hz = value;
                }

                WakeScheduler();
            }
        }

        public double RandomFrequency_Hz
        {
            get
            {
                lock (mSync)
                {
                    return mRandomFrequency_Hz;
                }
            }
            set
            {
                ValidateFinite(value, nameof(value));

                lock (mSync)
                {
                    mRandomFrequency_Hz = Math.Max(0.0, value);
                }

                WakeScheduler();
            }
        }

        public double RandomFrequencyOffset_Hz
        {
            get
            {
                lock (mSync)
                {
                    return mRandomFrequencyOffset_Hz;
                }
            }
            set
            {
                ValidateFinite(value, nameof(value));

                lock (mSync)
                {
                    mRandomFrequencyOffset_Hz = value;
                }

                WakeScheduler();
            }
        }

        #endregion

        #region Unused

        public double ExtendedVolume { get; set; }
        public double RetractedVolume { get; set; }
        public double ExtendedPan { get; set; }
        public double RetractedPan { get; set; }
        public double ExtendSpeed_WU_s { get; set; }
        public double RetractSpeed_WU_s { get; set; }
        public double ExtendDelay_s { get; set; }
        public double RetractDelay_s { get; set; }
        public double Distance_WU { get; set; }
        public double Period_s { get; set; }
        public double RandomPeriod_s { get; set; }
        public double RandomPeriodOffset_s { get; set; }

        #endregion

        #endregion

        #region Private Properties

        private const double mMinimumEffectiveFrequency_Hz = 0.001;
        private const double mMaximumEffectiveFrequency_Hz = 1000.0;
        private double mFrequency_Hz;
        private double mRandomFrequency_Hz;
        private double mRandomFrequencyOffset_Hz;

        #endregion

        #region Constructor

        public RandomSoundBehavior(RandomSoundProperties properties) :
            base(properties)
        {
            ValidatePositiveFinite(properties.Frequency_Hz, nameof(properties.Frequency_Hz));
            ValidateFinite(properties.RandomFrequency_Hz, nameof(properties.RandomFrequency_Hz));
            ValidateFinite(properties.RandomFrequencyOffset_Hz, nameof(properties.RandomFrequencyOffset_Hz));

            Effects.CollectionChanged += RandomEffects_CollectionChanged;

            mFrequency_Hz = properties.Frequency_Hz;
            mRandomFrequency_Hz = Math.Max(0.0, properties.RandomFrequency_Hz);
            mRandomFrequencyOffset_Hz = properties.RandomFrequencyOffset_Hz;
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

                    ValidateSoundSlot();
                    mPlaying = true;
                }

                try
                {
                    mSoundEngine.ActivateSource(this);
                    PlaySoundSingle();
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

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Members

        private void RandomEffects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            lock (mSync)
            {
                ThrowIfDisposed();
                UpdateActiveVoice();
            }
        }

        private void PlaySoundSingle()
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

                ValidateSoundSlot();

                SoundPermutation selectedSound = GetRandomElement(mCurrentSound.Samples);
                PlaybackParameters parameters = CreateRandomPlaybackParameters();

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

        private PlaybackParameters CreateRandomPlaybackParameters()
        {
            double randomizedVolume = Clamp(mVolume + mRandomVolumeOffset + GetRandomSignedValueUnsafe(mRandomVolume), 0.0, 1.0);
            double randomizedPitch = Clamp(mPitch + mRandomPitchOffset + GetRandomSignedValueUnsafe(mRandomPitch), mMinimumPitch, mMaximumPitch);
            double randomizedPan = Clamp(mPan + mRandomPanOffset + GetRandomSignedValueUnsafe(mRandomPan), -1.0, 1.0);

            return new PlaybackParameters
            {
                Volume = randomizedVolume,
                Pitch = randomizedPitch,
                Pan = randomizedPan,
                BaseVolumeAtStart = mVolume,
                BasePitchAtStart = mPitch,
                BasePanAtStart = mPan
            };
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

        private void ValidateSoundSlot()
        {
            if (mCurrentSound == null)
            {
                throw new InvalidOperationException("A sound slot has not been assigned.");
            }

            if (mCurrentSound.Samples == null || mCurrentSound.Samples.Count == 0)
            {
                throw new InvalidOperationException("The SoundSlot does not contain any samples.");
            }
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
                    PlaySoundSingle();
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
            double baseFrequency_Hz;
            double randomFrequency_Hz;
            double frequencyOffset_Hz;

            lock (mSync)
            {
                baseFrequency_Hz = mFrequency_Hz;
                randomFrequency_Hz = mRandomFrequency_Hz;
                frequencyOffset_Hz = mRandomFrequencyOffset_Hz;
            }

            double effectiveFrequency_Hz = baseFrequency_Hz + frequencyOffset_Hz + NextRandomSignedValue(randomFrequency_Hz);
            effectiveFrequency_Hz = Clamp(effectiveFrequency_Hz, mMinimumEffectiveFrequency_Hz, mMaximumEffectiveFrequency_Hz);

            double delay_s = 1.0 / effectiveFrequency_Hz;

            return TimeSpan.FromSeconds(delay_s);
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
