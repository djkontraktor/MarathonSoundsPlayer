using NAudio.Wave;

namespace MarathonSoundsPlayer
{
    public class SwitchableSampleProvider : ISampleProvider
    {
        private readonly object mSyncRoot = new object();
        private ISampleProvider mSource;

        public SwitchableSampleProvider(ISampleProvider source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            mSource = source;
            WaveFormat = source.WaveFormat;
        }

        public WaveFormat WaveFormat { get; private set; }

        public ISampleProvider Source
        {
            get
            {
                lock (mSyncRoot)
                {
                    return mSource;
                }
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (!WaveFormatsMatch(WaveFormat, value.WaveFormat))
                {
                    throw new ArgumentException("The replacement provider must have the same wave format.", nameof(value));
                }

                lock (mSyncRoot)
                {
                    mSource = value;
                }
            }
        }

        public int Read(Span<float> buffer)
        {
            lock (mSyncRoot)
            {
                return mSource.Read(buffer);
            }
        }

        private static bool WaveFormatsMatch(WaveFormat first, WaveFormat second)
        {
            return first.SampleRate == second.SampleRate &&
                   first.Channels == second.Channels &&
                   first.Encoding == second.Encoding &&
                   first.BitsPerSample == second.BitsPerSample;
        }
    }
}
