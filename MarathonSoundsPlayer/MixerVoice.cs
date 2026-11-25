using MarathonSoundsPlayer.SampleProviders;
using NAudio.Effects;
using NAudio.Wave;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MarathonSoundsPlayer
{
    public class MixerVoice : IDisposable
    {

        #region Public Properties

        public MemoryStream MemoryStream { get; private set; }
        public WaveFileReader WaveReader { get; private set; }

        public PitchAndSpeedSampleProvider PitchProvider
        {
            get;
            private set;
        }

        public VolumePanSampleProvider VolumePanProvider
        {
            get;
            private set;
        }

        public AutoDisposeSampleProvider Provider
        {
            get;
            private set;
        }

        public PlaybackParameters Parameters
        {
            get;
            private set;
        }

        public ObservableCollection<IAudioEffect> Effects
        {
            get;
            private set;
        }

        #endregion

        #region Private Properties

        private bool mDisposed;
        private readonly ISampleProvider mDryProvider;
        private readonly SwitchableSampleProvider mEffectOutput;

        #endregion

        #region Constructor

        public MixerVoice(MemoryStream memoryStream, WaveFileReader waveReader, PitchAndSpeedSampleProvider pitchProvider, VolumePanSampleProvider volumePanProvider, ISampleProvider engineFormatProvider, AutoDisposeSampleProvider provider, PlaybackParameters parameters)
        {
            MemoryStream = memoryStream;
            WaveReader = waveReader;
            PitchProvider = pitchProvider;
            VolumePanProvider = volumePanProvider;
            Provider = provider;
            Parameters = parameters;

            mDryProvider = engineFormatProvider;

            Effects = new ObservableCollection<IAudioEffect>();
            Effects.CollectionChanged += Effects_CollectionChanged;
            mEffectOutput = new SwitchableSampleProvider(mDryProvider);

            Provider = new AutoDisposeSampleProvider(mEffectOutput, provider.FinishedCallback);
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

            Effects.CollectionChanged -= Effects_CollectionChanged;
            Effects.Clear();

            /*
             * Provider does not dispose its source. The voice owns the reader
             * and MemoryStream and disposes them here.
             */
            Provider = null;
            PitchProvider = null;
            VolumePanProvider = null;
            Parameters = null;

            if (WaveReader != null)
            {
                WaveReader.Dispose();
                WaveReader = null;
            }

            if (MemoryStream != null)
            {
                MemoryStream.Dispose();
                MemoryStream = null;
            }
        }

        private void Effects_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildEffectChain();
        }

        private void RebuildEffectChain()
        {
            if (mDisposed)
            {
                return;
            }

            if (Effects.Count == 0)
            {
                mEffectOutput.Source = mDryProvider;
                return;
            }

            EffectChain chain = new EffectChain(mDryProvider);

            foreach (IAudioEffect effect in Effects)
            {
                if (effect != null)
                {
                    chain.Add(effect);
                }
            }

            mEffectOutput.Source = chain;
        }

        #endregion

    }
}
