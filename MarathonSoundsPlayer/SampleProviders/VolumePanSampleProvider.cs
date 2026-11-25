using NAudio.Wave;

namespace MarathonSoundsPlayer.SampleProviders
{
    public class VolumePanSampleProvider : ISampleProvider
    {

        #region Public Properties

        public WaveFormat WaveFormat
        {
            get
            {
                return mWaveFormat;
            }
        }

        /// <summary>
        /// Linear amplitude multiplier. Normally 0.0 through 1.0.
        /// </summary>
        public float Volume
        {
            get
            {
                return mVolume;
            }
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                mVolume = Clamp(value, 0.0f, 1.0f);
            }
        }

        /// <summary>
        /// -1.0 is fully left, 0.0 is center, and 1.0 is fully right.
        /// </summary>
        public float Pan
        {
            get
            {
                return mPan;
            }
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                mPan = Clamp(value, -1.0f, 1.0f);
            }
        }

        #endregion

        #region Private Properties

        private readonly ISampleProvider mSource;
        private readonly WaveFormat mWaveFormat;
        private readonly int mInputChannels;
        private float[] mSourceBuffer;
        private volatile float mVolume = 1.0f;
        private volatile float mPan;

        #endregion

        #region Constructor

        public VolumePanSampleProvider(ISampleProvider source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source.WaveFormat.Channels != 1 && source.WaveFormat.Channels != 2)
            {
                throw new ArgumentException("Only mono and stereo sources are supported.", nameof(source));
            }

            mSource = source;
            mInputChannels = source.WaveFormat.Channels;

            mWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 2);
        }

        #endregion

        #region Public Members

        public int Read(Span<float> buffer)
        {
            /*
             * Stereo output requires complete left/right sample pairs.
             */
            int requestedOutputSamples = buffer.Length - (buffer.Length % 2);

            if (requestedOutputSamples == 0)
            {
                return 0;
            }

            int requestedFrames = requestedOutputSamples / 2;
            int requestedInputSamples = requestedFrames * mInputChannels;

            EnsureSourceBufferSize(requestedInputSamples);

            int inputSamplesRead = mSource.Read(mSourceBuffer.AsSpan(0, requestedInputSamples));

            int framesRead = inputSamplesRead / mInputChannels;

            float volume = mVolume;
            float pan = mPan;

            /*
             * Constant-power pan:
             *
             * Pan -1: left  = 1, right = 0
             * Pan  0: left  = 0.707, right = 0.707
             * Pan +1: left  = 0, right = 1
             */
            double panAngle = (pan + 1.0) * Math.PI / 4.0;

            float leftGain = volume * (float)Math.Cos(panAngle);
            float rightGain = volume * (float)Math.Sin(panAngle);

            int outputIndex = 0;

            if (mInputChannels == 1)
            {
                for (int frame = 0; frame < framesRead; frame++)
                {
                    float sample = mSourceBuffer[frame];

                    buffer[outputIndex++] = sample * leftGain;
                    buffer[outputIndex++] = sample * rightGain;
                }
            }
            else
            {
                int inputIndex = 0;

                for (int frame = 0; frame < framesRead; frame++)
                {
                    float leftSample = mSourceBuffer[inputIndex++];
                    float rightSample = mSourceBuffer[inputIndex++];

                    buffer[outputIndex++] = leftSample * leftGain;
                    buffer[outputIndex++] = rightSample * rightGain;
                }
            }

            return framesRead * 2;
        }

        #endregion

        #region Private Members

        private void EnsureSourceBufferSize(int requiredSize)
        {
            if (mSourceBuffer == null || mSourceBuffer.Length < requiredSize)
            {
                mSourceBuffer = new float[requiredSize];
            }
        }

        private static float Clamp(float value, float minimum, float maximum)
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

        #endregion

    }
}
