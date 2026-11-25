using MarathonFileUtils.Sounds;
using MarathonFileUtils.Sounds.DataStructures;
using MarathonSoundsPlayer.SoundProperties;
using NAudio.Effects;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.ObjectModel;

namespace MarathonSoundsPlayer.SoundBehavior
{
    public abstract class SoundBehaviorBase
    {

        #region Public Properties

        public SoundsFileFormat Format
        {
            get
            {
                return mFormat;
            }
        }

        public Dictionary<string, SoundSlot> SoundCatalog
        {
            get
            {
                return mSoundCatalog;
            }
            set
            {
                mSoundCatalog = value;
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

        public ObservableCollection<IAudioEffect> Effects { get; set; }

        #endregion

        #region Protected Properties

        protected SoundsFileFormat mFormat;
        protected Dictionary<string, SoundSlot> mSoundCatalog = new Dictionary<string, SoundSlot>();
        protected const double mMinimumPitch = 0.5;
        protected const double mMaximumPitch = 2.0;
        protected double mPitch;
        protected readonly object mSync = new object();
        protected readonly Random mRng = new Random();
        protected SoundEngine mSoundEngine;
        protected MixerVoice mActiveVoice;
        protected bool mDisposed;
        protected bool mPlaying;
        protected CancellationTokenSource mSchedulerCancellation;
        protected Task mSchedulerTask;

        #endregion

        #region Constructor

        public SoundBehaviorBase(SoundPropertiesBase properties)
        {
            ArgumentNullException.ThrowIfNull(properties.SoundEngine);

            mFormat = properties.Format;
            mSoundEngine = properties.SoundEngine;

            ValidatePositiveFinite(properties.Pitch, nameof(properties.Pitch));
            mPitch = ClampFinite(properties.Pitch, mMinimumPitch, mMaximumPitch, nameof(properties.Pitch));

            Effects = properties.Effects;
        }

        #endregion

        #region Protected Members

        protected abstract void UpdateActiveVoice();

        protected ISampleProvider ConvertToEngineFormat(ISampleProvider source)
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

        protected T GetRandomElement<T>(IList<T> list)
        {
            ArgumentNullException.ThrowIfNull(list);

            if (list.Count == 0)
            {
                throw new InvalidOperationException("Cannot select an element from an empty list.");
            }

            return list[mRng.Next(list.Count)];
        }

        protected double GetRandomSignedValueUnsafe(double amount)
        {
            if (amount <= 0.0)
            {
                return 0.0;
            }

            return ((mRng.NextDouble() * 2.0) - 1.0) * amount;
        }

        protected double NextRandomSignedValue(double amount)
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

        protected static double Clamp(double value, double minimum, double maximum)
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

        protected static double ClampFinite(double value, double minimum, double maximum, string parameterName)
        {
            ValidateFinite(value, parameterName);
            return Clamp(value, minimum, maximum);
        }

        protected static void ValidateFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "The value must be a finite number.");
            }
        }

        protected static void ValidatePositiveFinite(double value, string parameterName)
        {
            ValidateFinite(value, parameterName);

            if (value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Frequency must be greater than zero.");
            }
        }

        protected void ThrowIfDisposed()
        {
            if (mDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundSource));
            }
        }

        /// <summary>
        /// Called after a voice finishes naturally or stops with an error.
        /// </summary>
        protected void OnVoiceFinished(Exception exception)
        {
            if (exception != null)
            {
                OnPlaybackError(exception);
            }
        }

        protected void OnPlaybackError(Exception exception)
        {
            // Add application logging here if desired.
            // Debug.WriteLine(exception);
        }

        #endregion

    }
}
