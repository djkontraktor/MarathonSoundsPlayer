using MarathonFileUtils.Sounds;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MarathonSoundsPlayer
{
    public class SoundSource : IDisposable
    {

        #region Public Properties

        public SoundSourceType SourceType
        {
            get
            {
                return mSourceType;
            }
            set
            {
                if (Playing)
                {
                    Stop();
                    mSourceType = value;
                    Start();
                }
                else
                {
                    mSourceType = value;
                }
            }
        }

        public SoundsFileFormat Format
        {
            get
            {
                return mFormat;
            }
        }

        public SoundSlot CurrentSound
        {
            get
            {
                lock (mSync)
                {
                    return mCurrentSound;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mCurrentSound = value;
                }
            }
        }

        public double Volume
        {
            get
            {
                lock (mSync)
                {
                    return mVolume;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mVolume = ClampFinite(value, 0.0, 1.0, nameof(value));
                    UpdateActiveVoice();
                }
            }
        }

        public double RandomVolume
        {
            get
            {
                lock (mSync)
                {
                    return mRandomVolume;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    ValidateFinite(value, nameof(value));
                    mRandomVolume = Math.Max(0.0, value);
                }
            }
        }

        public double RandomVolumeOffset
        {
            get
            {
                lock (mSync)
                {
                    return mRandomVolumeOffset;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    ValidateFinite(value, nameof(value));
                    mRandomVolumeOffset = value;
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

        public double RandomPitch
        {
            get
            {
                lock (mSync)
                {
                    return mRandomPitch;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    ValidateFinite(value, nameof(value));
                    mRandomPitch = Math.Max(0.0, value);
                }
            }
        }

        public double RandomPitchOffset
        {
            get
            {
                lock (mSync)
                {
                    return mRandomPitchOffset;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    ValidateFinite(value, nameof(value));
                    mRandomPitchOffset = value;
                }
            }
        }

        public double Pan
        {
            get
            {
                lock (mSync)
                {
                    return mPan;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    mPan = ClampFinite(value, -1.0, 1.0, nameof(value));
                    UpdateActiveVoice();
                }
            }
        }

        public double RandomPan
        {
            get
            {
                lock (mSync)
                {
                    return mRandomPan;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    ValidateFinite(value, nameof(value));
                    mRandomPan = Math.Max(0.0, value);
                }
            }
        }

        public double RandomPanOffset
        {
            get
            {
                lock (mSync)
                {
                    return mRandomPanOffset;
                }
            }
            set
            {
                lock (mSync)
                {
                    ThrowIfDisposed();
                    ValidateFinite(value, nameof(value));
                    mRandomPanOffset = value;
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

        // Maybe effects here later?

        #endregion

        #region Private Properties

        private SoundsFileFormat mFormat;
        private ContinuousRandomSampleProvider mAmbientProvider;
        private const double mMinimumPitch = 0.5;
        private const double mMaximumPitch = 2.0;
        private SoundSourceType mSourceType;
        private readonly object mSync = new object();
        private readonly Random mRng = new Random();
        private SoundSlot mCurrentSound;
        private SoundEngine mSoundEngine;
        private MixerVoice mActiveVoice;
        private double mVolume;
        private double mRandomVolume;
        private double mRandomVolumeOffset;
        private double mPitch;
        private double mRandomPitch;
        private double mRandomPitchOffset;
        private double mPan;
        private double mRandomPan;
        private double mRandomPanOffset;
        private bool mDisposed;
        private bool mPlaying;
        private const double mMinimumEffectiveFrequency_Hz = 0.001;
        private const double mMaximumEffectiveFrequency_Hz = 1000.0;
        private double mFrequency_Hz;
        private double mRandomFrequency_Hz;
        private double mRandomFrequencyOffset_Hz;
        private CancellationTokenSource mSchedulerCancellation;
        private Task mSchedulerTask;

        #endregion

        #region Constructors

        public SoundSource(SoundSourceType type, SoundsFileFormat format, SoundEngine soundEngine, SoundSlot soundSlot, double volume, double randomVolume, double randomVolumeOffset, double pitch, double randomPitch, double randomPitchOffset, double pan, double randomPan, double randomPanOffset,
            double frequency_Hz, double randomFrequency_Hz, double randomFrequencyOffset_Hz)
        {
            ArgumentNullException.ThrowIfNull(soundEngine);
            ArgumentNullException.ThrowIfNull(soundSlot);

            ValidatePositiveFinite(frequency_Hz, nameof(frequency_Hz));
            ValidateFinite(randomFrequency_Hz, nameof(randomFrequency_Hz));
            ValidateFinite(randomFrequencyOffset_Hz, nameof(randomFrequencyOffset_Hz));
            ValidateFinite(randomVolume, nameof(randomVolume));
            ValidateFinite(randomVolumeOffset, nameof(randomVolumeOffset));
            ValidateFinite(randomPitch, nameof(randomPitch));
            ValidateFinite(randomPitchOffset, nameof(randomPitchOffset));
            ValidateFinite(randomPan, nameof(randomPan));
            ValidateFinite(randomPanOffset, nameof(randomPanOffset));

            mSoundEngine = soundEngine;
            mSourceType = type;
            mFormat = format;
            mCurrentSound = soundSlot;

            mVolume = ClampFinite(volume, 0.0, 1.0, nameof(volume));
            mRandomVolume = Math.Max(0.0, randomVolume);
            mRandomVolumeOffset = randomVolumeOffset;

            mPitch = ClampFinite(pitch, mMinimumPitch, mMaximumPitch, nameof(pitch));
            mRandomPitch = Math.Max(0.0, randomPitch);
            mRandomPitchOffset = randomPitchOffset;

            mPan = ClampFinite(pan, -1.0, 1.0, nameof(pan));
            mRandomPan = Math.Max(0.0, randomPan);
            mRandomPanOffset = randomPanOffset;

            mFrequency_Hz = frequency_Hz;
            mRandomFrequency_Hz = Math.Max(0.0, randomFrequency_Hz);
            mRandomFrequencyOffset_Hz = randomFrequencyOffset_Hz;
        }

        #endregion

        #region Public Members

        public void Start()
        {
            if (mSourceType.Equals(SoundSourceType.Ambient))
            {
                lock (mSync)
                {
                    if (mAmbientProvider != null)
                    {
                        return;
                    }

                    if (!TrySetStarted())
                    {
                        return;
                    }

                    ContinuousRandomSampleProvider provider = new ContinuousRandomSampleProvider(mSoundEngine.WaveFormat, CreateRandomOwnedProvider);

                    try
                    {
                        mSoundEngine.ActivateSource(this);
                        mSoundEngine.AddInput(provider);
                        mAmbientProvider = provider;
                    }
                    catch
                    {
                        SetStopped();
                        mSoundEngine.RemoveInput(provider);
                        mSoundEngine.DeactivateSource(this);
                        provider.Dispose();
                        throw;
                    }
                }
            }
            else
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
        }

        public void Stop()
        {
            if (mSourceType.Equals(SoundSourceType.Ambient))
            {
                ContinuousRandomSampleProvider provider;

                lock (mSync)
                {
                    provider = mAmbientProvider;
                    mAmbientProvider = null;
                    SetStopped();
                }

                if (provider != null)
                {
                    provider.Stop();
                    mSoundEngine.RemoveInput(provider);
                    provider.Dispose();
                }

                mSoundEngine.DeactivateSource(this);
            }
            else
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
        }

        public virtual void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Members

        private OwnedSampleProvider CreateRandomOwnedProvider()
        {
            SoundPermutation selectedSound;
            PlaybackParameters parameters;

            lock (mSync)
            {
                ThrowIfDisposed();
                ValidateSoundSlot();

                selectedSound = GetRandomElement(mCurrentSound.Samples);

                // A new randomized volume, pitch, and pan are generated every time this method is called.
                parameters = CreateRandomPlaybackParameters();
            }

            return CreateOwnedProvider(selectedSound, parameters);
        }

        private OwnedSampleProvider CreateOwnedProvider(SoundPermutation sound, PlaybackParameters parameters)
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
                ISampleProvider sampleProvider = waveReader.ToSampleProvider();

                SmbPitchShiftingSampleProvider pitchProvider = new SmbPitchShiftingSampleProvider(sampleProvider)
                {
                    PitchFactor = (float)parameters.Pitch,
                };

                VolumePanSampleProvider volumePanProvider = new VolumePanSampleProvider(pitchProvider)
                {
                    Volume = (float)parameters.Volume,
                    Pan = (float)parameters.Pan,
                };

                ISampleProvider engineFormatProvider = ConvertToEngineFormat(volumePanProvider);

                return new OwnedSampleProvider(engineFormatProvider, waveReader, memoryStream);
            }
            catch
            {
                if (waveReader != null)
                {
                    waveReader.Dispose();

                    // WaveFileReader normally disposes its underlying stream.
                    memoryStream = null;
                }

                if (memoryStream != null)
                {
                    memoryStream.Dispose();
                }

                throw;
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

            voice.Parameters.Volume = Clamp(mVolume + volumeRandomContribution, 0.0, 1.0);
            voice.Parameters.Pitch = Clamp(mPitch + pitchRandomContribution, mMinimumPitch, mMaximumPitch);
            voice.Parameters.Pan = Clamp(mPan + panRandomContribution, -1.0, 1.0);

            voice.Parameters.BaseVolumeAtStart = mVolume;
            voice.Parameters.BasePitchAtStart = mPitch;
            voice.Parameters.BasePanAtStart = mPan;

            voice.VolumePanProvider.Volume = (float)voice.Parameters.Volume;
            voice.PitchProvider.PitchFactor = (float)voice.Parameters.Pitch;
            voice.VolumePanProvider.Pan = (float)voice.Parameters.Pan;
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

                ValidateSoundSlot();
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
