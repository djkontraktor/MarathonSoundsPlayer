using MarathonFileUtils.Sounds;
using MarathonSoundsPlayer.SoundProperties;
using NAudio.Effects;

namespace MarathonSoundsPlayer.SoundBehavior
{
    public abstract class EnvironmentalSoundBehavior : SoundBehaviorBase
    {

        #region ISoundBehavior Members

        public SoundSlot CurrentSound
        {
            get
            {
                return mCurrentSound;
            }
            set
            {
                mCurrentSound = value;
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

        #region Unused

        public double PlatformExtendedVolume { get; set; }
        public double PlatformRetractedVolume { get; set; }
        public double PlatformExtendedPan { get; set; }
        public double PlatformRetractedPan { get; set; }
        public double PlatformExtendSpeed_WU_s { get; set; }
        public double PlatformRetractSpeed_WU_s { get; set; }
        public double PlatformDistance_WU { get; set; }

        #endregion

        #endregion

        #region Private Properties

        protected SoundSourceType mSourceType;
        protected SoundSlot mCurrentSound;
        protected double mVolume;
        protected double mRandomVolume;
        protected double mRandomVolumeOffset;
        protected double mRandomPitch;
        protected double mRandomPitchOffset;
        protected double mPan;
        protected double mRandomPan;
        protected double mRandomPanOffset;

        #endregion

        #region Constructor

        public EnvironmentalSoundBehavior(EnvironmentalSoundProperties properties) : base(properties)
        {
            ArgumentNullException.ThrowIfNull(properties.SoundSlot);

            mSoundCatalog.Add("Sound", properties.SoundSlot);
            mCurrentSound = mSoundCatalog.First().Value;

            ValidateFinite(properties.Volume, nameof(properties.Volume));
            ValidateFinite(properties.RandomVolume, nameof(properties.RandomVolume));
            ValidateFinite(properties.RandomVolumeOffset, nameof(properties.RandomVolumeOffset));
            ValidateFinite(properties.RandomPitch, nameof(properties.RandomPitch));
            ValidateFinite(properties.RandomPitchOffset, nameof(properties.RandomPitchOffset));
            ValidateFinite(properties.RandomPan, nameof(properties.RandomPan));
            ValidateFinite(properties.RandomPanOffset, nameof(properties.RandomPanOffset));

            mVolume = ClampFinite(properties.Volume, 0.0, 1.0, nameof(properties.Volume));
            mRandomVolume = Math.Max(0.0, properties.RandomVolume);
            mRandomVolumeOffset = properties.RandomVolumeOffset;

            mRandomPitch = Math.Max(0.0, properties.RandomPitch);
            mRandomPitchOffset = properties.RandomPitchOffset;

            mPan = ClampFinite(properties.Pan, -1.0, 1.0, nameof(properties.Pan));
            mRandomPan = Math.Max(0.0, properties.RandomPan);
            mRandomPanOffset = properties.RandomPanOffset;
        }

        #endregion

        #region Protected Members

        protected override void UpdateActiveVoice()
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

            voice.Effects.Clear();

            foreach (IAudioEffect effect in Effects)
            {
                voice.Effects.Add(effect);
            }

        }

        #endregion

    }
}
