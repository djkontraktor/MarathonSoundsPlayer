using NAudio.Wave;

namespace MarathonSoundsPlayer.SampleProviders
{
    public sealed class OwnedSampleProvider : IDisposable
    {

        #region Public Properties

        public ISampleProvider Provider { get; private set; }

        #endregion

        #region Private Properties

        private readonly IDisposable[] mOwnedResources;
        private bool mDisposed;

        #endregion

        #region Constructor

        public OwnedSampleProvider(ISampleProvider provider, params IDisposable[] ownedResources)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            Provider = provider;

            mOwnedResources = ownedResources ?? new IDisposable[0];
        }

        #endregion

        #region Public Members

        public void Dispose()
        {
            if (mDisposed)
            {
                return;
            }

            mDisposed = true;
            Provider = null;

            /*
             * Dispose in the supplied order. Pass WaveFileReader before its
             * underlying MemoryStream when constructing this object.
             */
            for (int index = 0; index < mOwnedResources.Length; index++)
            {
                IDisposable resource = mOwnedResources[index];

                if (resource != null)
                {
                    resource.Dispose();
                }
            }
        }

        #endregion

    }
}
