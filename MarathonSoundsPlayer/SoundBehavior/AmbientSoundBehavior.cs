using MarathonFileUtils.Sounds.DataStructures;
using MarathonSoundsPlayer.Interfaces;
using MarathonSoundsPlayer.SampleProviders;
using MarathonSoundsPlayer.SoundProperties;
using NAudio.Effects;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Specialized;

namespace MarathonSoundsPlayer.SoundBehavior
{
    public class AmbientSoundBehavior : EnvironmentalSoundBehavior, ISoundBehavior
    {

        #region Unused ISoundBehavior Members

        public double Frequency_Hz { get; set; }
        public double RandomFrequency_Hz { get; set; }
        public double RandomFrequencyOffset_Hz { get; set; }
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

        #region Private Properties

        private ContinuousRandomSampleProvider mAmbientProvider;
        private EffectChain mAmbientEffectChain;

        #endregion

        #region Constructor

        public AmbientSoundBehavior(EnvironmentalSoundProperties properties) :
            base(properties)
        {
            Effects.CollectionChanged += AmbientEffects_CollectionChanged;
        }

        #endregion

        #region Public Members

        public void Start()
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

                mAmbientEffectChain = new EffectChain(provider);
                RebuildAmbientEffectChain();

                try
                {
                    mSoundEngine.ActivateSource(this);
                    mSoundEngine.AddInput(mAmbientEffectChain);
                    mAmbientProvider = provider;
                }
                catch
                {
                    SetStopped();
                    mSoundEngine.RemoveInput(mAmbientEffectChain);
                    mSoundEngine.DeactivateSource(this);
                    provider.Dispose();
                    throw;
                }
            }
        }

        public void Stop()
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

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Members

        private void AmbientEffects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            lock (mSync)
            {
                ThrowIfDisposed();
                RebuildAmbientEffectChain();
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

        private void RebuildAmbientEffectChain()
        {
            if (mAmbientEffectChain != null)
            {
                for (int i = mAmbientEffectChain.Effects.Count - 1; i >= 0; i--)
                {
                    mAmbientEffectChain.RemoveAt(i);
                }

                foreach (IAudioEffect effect in Effects)
                {
                    mAmbientEffectChain.Add(effect);
                }
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

        #endregion

    }
}