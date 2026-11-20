using NAudio.Wave;

namespace MarathonSoundsPlayer
{
    public class AutoDisposeSampleProvider : ISampleProvider
    {

        #region Public Properties

        public WaveFormat WaveFormat
        {
            get
            {
                return mSource.WaveFormat;
            }
        }

        public Action FinishedCallback
        {
            get
            {
                return mFinishedCallback;
            }
        }

        #endregion

        #region Private Properties

        private readonly ISampleProvider mSource;
        private readonly Action mFinishedCallback;
        private int mFinished;

        #endregion

        #region Constructor

        public AutoDisposeSampleProvider(ISampleProvider source, Action finishedCallback)
        {
            ArgumentNullException.ThrowIfNull(source);

            mSource = source;
            mFinishedCallback = finishedCallback;
        }

        #endregion

        #region Public Members

        public int Read(Span<float> buffer)
        {
            int samplesRead = mSource.Read(buffer);

            if (samplesRead == 0)
            {
                SignalFinished();
            }

            return samplesRead;
        }

        public void SignalFinished()
        {
            if (Interlocked.Exchange(ref mFinished, 1) != 0)
            {
                return;
            }

            if (mFinishedCallback != null)
            {
                mFinishedCallback();
            }
        }

        #endregion

    }
}
