using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MarathonSoundsPlayer
{
    public class PitchAndSpeedSampleProvider : ISampleProvider
    {

        #region Public Properties

        public WaveFormat WaveFormat
        {
            get
            {
                return mResampler.WaveFormat;
            }
        }

        public double PitchFactor
        {
            get
            {
                return mPitchFactor;
            }
            set
            {
                mPitchFactor = value;
                UpdatePitch();
            }
        }

        #endregion

        #region Private Properties

        private ISampleProvider mSource;
        private ISampleProvider mResampler;
        private double mPitchFactor;
        private int mOutputSampleRate;

        #endregion

        #region Constructor

        public PitchAndSpeedSampleProvider(ISampleProvider source, double pitchFactor)
        {
            mSource = source;
            mPitchFactor = pitchFactor;

            if (mSource == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (mPitchFactor <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(pitchFactor), "Pitch factor must be greater than zero.");
            }

            mOutputSampleRate = mSource.WaveFormat.SampleRate;

            UpdatePitch();
        }

        #endregion

        #region Public Members

        public int Read(Span<float> buffer)
        {
            return mResampler.Read(buffer);
        }

        #endregion

        #region Private Members

        private void UpdatePitch()
        {
            int interpretedSampleRate = (int)Math.Round(mOutputSampleRate * mPitchFactor);
            ISampleProvider reinterpretedSource = new SampleRateOverrideSampleProvider(mSource, interpretedSampleRate);
            // Convert the reinterpreted stream back to the output device's
            // required sample rate.
            mResampler = new WdlResamplingSampleProvider(reinterpretedSource, mOutputSampleRate);
        }

        #endregion

    }
}
