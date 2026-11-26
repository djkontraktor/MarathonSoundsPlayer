using MarathonFileUtils.Sounds.DataStructures;
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

        protected PlaybackParameters CreateRandomPlaybackParameters()
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

        #endregion

    }
}
