using NAudio.Wave;

namespace MarathonSoundsPlayer.SampleProviders
{
    /// <summary>
    /// Maintains one permanent mixer input and switches to a newly created
    /// provider whenever the current provider reaches its end.
    /// </summary>
    public sealed class ContinuousRandomSampleProvider : ISampleProvider, IDisposable
    {

        #region Public Properties

        public WaveFormat WaveFormat
        {
            get
            {
                return mWaveFormat;
            }
        }

        #endregion

        #region Private Properties

        private readonly object mSync = new object();
        private readonly WaveFormat mWaveFormat;
        private readonly Func<OwnedSampleProvider> mProviderFactory;
        private OwnedSampleProvider mCurrentProvider;
        private bool mStopped;
        private bool mDisposed;

        #endregion

        #region Constructor

        public ContinuousRandomSampleProvider(WaveFormat waveFormat, Func<OwnedSampleProvider> providerFactory)
        {
            if (waveFormat == null)
            {
                throw new ArgumentNullException(nameof(waveFormat));
            }

            if (providerFactory == null)
            {
                throw new ArgumentNullException(nameof(providerFactory));
            }

            mWaveFormat = waveFormat;
            mProviderFactory = providerFactory;
        }

        #endregion

        #region Public Members

        public int Read(Span<float> buffer)
        {
            lock (mSync)
            {
                if (mDisposed || mStopped)
                {
                    return 0;
                }

                int totalSamplesRead = 0;
                int consecutiveEmptyProviders = 0;

                while (totalSamplesRead < buffer.Length)
                {
                    if (mCurrentProvider == null)
                    {
                        mCurrentProvider = CreateNextProvider();
                    }

                    /*
                     * Give the provider only the unfilled portion of the
                     * destination buffer.
                     */
                    Span<float> remainingBuffer = buffer.Slice(totalSamplesRead);

                    int samplesRead = mCurrentProvider.Provider.Read(remainingBuffer);

                    if (samplesRead > 0)
                    {
                        totalSamplesRead += samplesRead;
                        consecutiveEmptyProviders = 0;
                        continue;
                    }

                    /*
                     * The current sound ended. Dispose it and create another
                     * provider during this same Read() call.
                     */
                    DisposeCurrentProvider();
                    consecutiveEmptyProviders++;

                    /*
                     * Prevent an infinite audio-thread loop if the slot contains
                     * malformed or zero-length sounds.
                     */
                    if (consecutiveEmptyProviders >= 16)
                    {
                        buffer.Slice(totalSamplesRead).Clear();

                        /*
                         * The remaining buffer was filled with silence, so report
                         * the entire requested buffer as supplied.
                         */
                        return buffer.Length;
                    }
                }

                return totalSamplesRead;
            }
        }

        public void Stop()
        {
            lock (mSync)
            {
                if (mStopped)
                {
                    return;
                }

                mStopped = true;
                DisposeCurrentProvider();
            }
        }

        public void Dispose()
        {
            lock (mSync)
            {
                if (mDisposed)
                {
                    return;
                }

                mStopped = true;
                mDisposed = true;

                DisposeCurrentProvider();
            }
        }

        #endregion

        #region Private Members

        private OwnedSampleProvider CreateNextProvider()
        {
            OwnedSampleProvider provider = mProviderFactory();

            if (provider == null)
            {
                throw new InvalidOperationException("The ambient provider factory returned null.");
            }

            if (provider.Provider == null)
            {
                provider.Dispose();
                throw new InvalidOperationException("The ambient provider factory returned a null provider.");
            }

            if (!FormatsMatch(provider.Provider.WaveFormat, mWaveFormat))
            {
                WaveFormat actualFormat = provider.Provider.WaveFormat;
                provider.Dispose();
                throw new InvalidOperationException("The ambient provider format does not match the mixer. Expected " + mWaveFormat + ", but received " + actualFormat + ".");
            }

            return provider;
        }

        private void DisposeCurrentProvider()
        {
            OwnedSampleProvider provider = mCurrentProvider;
            mCurrentProvider = null;

            if (provider != null)
            {
                provider.Dispose();
            }
        }

        private static bool FormatsMatch(WaveFormat first, WaveFormat second)
        {
            return first.SampleRate == second.SampleRate &&
                   first.Channels == second.Channels &&
                   first.Encoding == second.Encoding &&
                   first.BitsPerSample == second.BitsPerSample;
        }

        #endregion

    }
}
