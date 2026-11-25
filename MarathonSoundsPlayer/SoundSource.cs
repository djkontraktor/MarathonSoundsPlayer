using MarathonSoundsPlayer.Interfaces;

namespace MarathonSoundsPlayer
{
    public class SoundSource : IDisposable
    {

        #region Public Properties

        public ISoundBehavior SoundBehavior
        {
            get
            {
                return mSoundBehavior;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (ReferenceEquals(mSoundBehavior, value))
                {
                    return;
                }

                mSoundBehavior.Stop();
                mSoundBehavior = value;
            }
        }

        #endregion

        #region Private Properties

        private ISoundBehavior mSoundBehavior;

        #endregion

        #region Constructor

        public SoundSource(ISoundBehavior soundBehavior)
        {
            mSoundBehavior = soundBehavior;
        }

        #endregion

        #region Public Members

        public void Start()
        {
            mSoundBehavior.Start();
        }

        public void Stop()
        {
            mSoundBehavior.Stop();
        }

        public void Dispose()
        {
            mSoundBehavior.Dispose();
        }

        #endregion

    }
}
