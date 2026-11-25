using MarathonFileUtils.Sounds;
using MarathonSoundsPlayer.Interfaces;
using MarathonSoundsPlayer.SoundBehavior;
using MarathonSoundsPlayer.SoundProperties;
using NAudio.Effects;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace MarathonSoundsPlayer
{
    public class SoundEngine : IDisposable
    {

        #region Public Properties

        public SoundsFile SoundsFile;
        public bool SoundsFileLoaded = false;

        public SoundsFileFormat Format
        {
            get
            {
                return mFormat;
            }
        }

        public WaveFormat WaveFormat
        {
            get
            {
                return mMixer.WaveFormat;
            }
        }

        public bool OutputEnabled
        {
            get
            {
                lock (mSync)
                {
                    return mOutputEnabled;
                }
            }
        }

        public int ActiveSourceCount
        {
            get
            {
                lock (mSync)
                {
                    return mActiveSources.Count;
                }
            }
        }

        #endregion

        #region Private Properties

        private SoundsFileFormat mFormat;
        private readonly object mSync = new object();
        private readonly MixingSampleProvider mMixer;
        private readonly WaveOut mOutputDevice;
        private readonly HashSet<object> mActiveSources = new HashSet<object>();
        private bool mOutputEnabled;
        private bool mDisposed;
        private string mSoundsFilePath;

        #endregion

        #region Constructor

        public SoundEngine(int sampleRate = 44100, int desiredLatency_ms = 100)
        {
            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }

            if (desiredLatency_ms <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(desiredLatency_ms));
            }

            WaveFormat mixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);

            mMixer = new MixingSampleProvider(mixerFormat)
            {
                // This is the critical setting. The mixer returns silence instead
                // of returning zero when it temporarily has no inputs. Therefore,
                // WaveOutEvent does not interpret an empty mixer as end-of-stream.
                ReadFully = true,
            };

            mOutputDevice = new WaveOut
            {
                BufferMilliseconds = desiredLatency_ms,
                NumberOfBuffers = 2,
            };

            mOutputDevice.Init(mMixer);
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Registers a SoundSource as active. The first active source starts
        /// the persistent output device.
        /// </summary>
        public void ActivateSource(object owner)
        {
            if (SoundsFileLoaded)
            {
                ArgumentNullException.ThrowIfNull(owner);

                lock (mSync)
                {
                    ThrowIfDisposed();

                    mActiveSources.Add(owner);

                    if (!mOutputEnabled)
                    {
                        mOutputDevice.Play();
                        mOutputEnabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Marks a SoundSource as inactive. When no sources remain, the output
        /// can either stop or continue playing silence, depending on the argument.
        /// </summary>
        public void DeactivateSource(object owner)
        {
            if (owner == null)
            {
                return;
            }

            lock (mSync)
            {
                if (mDisposed)
                {
                    return;
                }

                mActiveSources.Remove(owner);
            }
        }

        public void AddInput(ISampleProvider input)
        {
            if (SoundsFileLoaded)
            {
                ArgumentNullException.ThrowIfNull(input);

                lock (mSync)
                {
                    ThrowIfDisposed();

                    if (!WaveFormatsMatch(input.WaveFormat, mMixer.WaveFormat))
                    {
                        throw new ArgumentException("The input format does not match the mixer format. Expected " + mMixer.WaveFormat + ", but received " + input.WaveFormat + ".", nameof(input));
                    }

                    mMixer.AddMixerInput(input);
                }
            }
        }

        public void RemoveInput(ISampleProvider input)
        {
            if (input == null)
            {
                return;
            }

            lock (mSync)
            {
                if (mDisposed)
                {
                    return;
                }

                mMixer.RemoveMixerInput(input);
            }
        }

        public void LoadSoundsFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            mSoundsFilePath = filePath;

            if (ext.Equals(".snda"))
            {
                SoundsFile = new M2SoundsFile(filePath, out mFormat);
            }
            else if (ext.Equals(".sndz"))
            {
                SoundsFile = new M1SoundsFile(filePath);
                mFormat = SoundsFileFormat.M1;
            }

            SoundsFileLoaded = true;
        }

        public void Dispose()
        {
            lock (mSync)
            {
                if (mDisposed)
                {
                    return;
                }

                mDisposed = true;
                mActiveSources.Clear();

                if (mOutputEnabled)
                {
                    mOutputDevice.Stop();
                    mOutputEnabled = false;
                }

                mOutputDevice.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        public List<SoundSource> LoadFromFile(string filePath, out string soundsFilePath)
        {
            List<SoundSource> soundSources = new List<SoundSource>();

            XDocument document = XDocument.Load(filePath, LoadOptions.SetLineInfo);
            XElement root = document.Element("Soundscape");

            if (root == null)
            {
                throw new InvalidDataException("The soundscape file does not contain a Soundscape root element.");
            }

            XElement soundsPathElement = root.Element("SoundsFilePath");

            if (soundsPathElement == null)
            {
                throw CreateMissingElementException(root, "SoundsFilePath");
            }

            soundsFilePath = soundsPathElement.Value;
            mSoundsFilePath = soundsFilePath;
            LoadSoundsFile(mSoundsFilePath);

            XElement sourcesElement = root.Element("SoundSources");

            if (sourcesElement == null)
            {
                throw CreateMissingElementException(root, "SoundSources");
            }

            foreach (XElement sourceElement in sourcesElement.Elements("SoundSource"))
            {
                SoundSourceType sourceType = ReadEnum<SoundSourceType>(sourceElement, "SourceType");

                int soundSlotIndex = ReadInt32(sourceElement, "SoundSlotIndex");
                double volume = ReadDouble(sourceElement, "Volume");
                double randomVolume = ReadDouble(sourceElement, "RandomVolume");
                double randomVolumeOffset = ReadDouble(sourceElement, "RandomVolumeOffset");
                double pitch = ReadDouble(sourceElement, "Pitch");
                double randomPitch = ReadDouble(sourceElement, "RandomPitch");
                double randomPitchOffset = ReadDouble(sourceElement, "RandomPitchOffset");
                double pan = ReadDouble(sourceElement, "Pan");
                double randomPan = ReadDouble(sourceElement, "RandomPan");
                double randomPanOffset = ReadDouble(sourceElement, "RandomPanOffset");
                double frequency_Hz = ReadDouble(sourceElement, "Frequency_Hz");
                double randomFrequency_Hz = ReadDouble(sourceElement, "RandomFrequency_Hz");
                double randomFrequencyOffset_Hz = ReadDouble(sourceElement, "RandomFrequencyOffset_Hz");

                SoundSlot soundSlot = SoundsFile.SoundSlots.Where(s => s.ID.Index == soundSlotIndex).First();

                if (soundSlot == null)
                {
                    throw new InvalidDataException("No SoundSlot could be created for index " + soundSlotIndex.ToString(CultureInfo.InvariantCulture) + ".");
                }

                ISoundBehavior behavior;

                switch (sourceType)
                {
                    case SoundSourceType.Ambient:

                        EnvironmentalSoundProperties ambientProperties = new EnvironmentalSoundProperties(this, soundSlot, volume, randomVolume, randomVolumeOffset, pitch, randomPitch, randomPitchOffset, pan, randomPan, randomPanOffset, new ObservableCollection<IAudioEffect>());
                        behavior = new AmbientSoundBehavior(ambientProperties);

                        break;
                    case SoundSourceType.Random:

                        RandomSoundProperties randomProperties = new RandomSoundProperties(this, soundSlot, volume, randomVolume, randomVolumeOffset, pitch, randomPitch, randomPitchOffset, pan, randomPan, randomPanOffset, frequency_Hz, randomFrequency_Hz, randomFrequencyOffset_Hz, new ObservableCollection<IAudioEffect>());
                        behavior = new RandomSoundBehavior(randomProperties);

                        break;

                    case SoundSourceType.Platform:

                        SoundPropertiesPlatform platformProperties = new SoundPropertiesPlatform(this, soundSlot, soundSlot, soundSlot, soundSlot, soundSlot, volume, volume, pitch, pan, pan, 1, 1, 0, 0, 1, 1, 0, 0, new ObservableCollection<IAudioEffect>());
                        behavior = new PlatformSoundBehavior(platformProperties);

                        break;

                    default:
                        throw new XmlException("Could not load a valid SourceType from .soundsource file.");
                }

                /*
                 * Assign SourceType last if changing it can stop and restart
                 * the SoundSource. The newly created source should normally
                 * not be playing, but this order minimizes side effects.
                 */
                SoundSource soundSource = new SoundSource(behavior);

                soundSources.Add(soundSource);
            }

            return soundSources;
        }

        public void SaveToFile(string filePath, List<SoundSource> soundSources)
        {
            string fullPath = Path.GetFullPath(filePath);
            string directory = Path.GetDirectoryName(fullPath);

            XElement sourcesElement = new XElement("SoundSources");

            for (int index = 0; index < soundSources.Count; index++)
            {
                SoundSource soundSource = soundSources[index];

                if (soundSource == null)
                {
                    throw new InvalidDataException("The sound-source list contains a null item at index " + index.ToString(CultureInfo.InvariantCulture) + ".");
                }

                if (soundSource.SoundBehavior.CurrentSound == null)
                {
                    throw new InvalidDataException("The SoundSource at list index " + index.ToString(CultureInfo.InvariantCulture) + " does not have a CurrentSound.");
                }

                SoundSourceType sourceType;

                switch (soundSource.SoundBehavior)
                {
                    case (AmbientSoundBehavior amb):
                        sourceType = SoundSourceType.Ambient;
                        break;
                    case (RandomSoundBehavior rand):
                        sourceType = SoundSourceType.Random;
                        break;
                    case (PlatformSoundBehavior plat):
                        sourceType = SoundSourceType.Platform;
                        break;
                    default:
                        throw new InvalidDataException("This sound behavior has not yet been implemented.");
                }

                sourcesElement.Add(
                    new XElement(
                        "SoundSource",
                        new XElement("SourceType", sourceType.ToString()),
                        new XElement("SoundSlotIndex", soundSource.SoundBehavior.CurrentSound.ID.Index),
                        new XElement("Volume", FormatDouble(soundSource.SoundBehavior.Volume)),
                        new XElement("RandomVolume", FormatDouble(soundSource.SoundBehavior.RandomVolume)),
                        new XElement("RandomVolumeOffset", FormatDouble(soundSource.SoundBehavior.RandomVolumeOffset)),
                        new XElement("Pitch", FormatDouble(soundSource.SoundBehavior.Pitch)),
                        new XElement("RandomPitch", FormatDouble(soundSource.SoundBehavior.RandomPitch)),
                        new XElement("RandomPitchOffset", FormatDouble(soundSource.SoundBehavior.RandomPitchOffset)),
                        new XElement("Pan", FormatDouble(soundSource.SoundBehavior.Pan)),
                        new XElement("RandomPan", FormatDouble(soundSource.SoundBehavior.RandomPan)),
                        new XElement("RandomPanOffset", FormatDouble(soundSource.SoundBehavior.RandomPanOffset)),
                        new XElement("Frequency_Hz", FormatDouble(soundSource.SoundBehavior.Frequency_Hz)),
                        new XElement("RandomFrequency_Hz", FormatDouble(soundSource.SoundBehavior.RandomFrequency_Hz)),
                        new XElement("RandomFrequencyOffset_Hz", FormatDouble(soundSource.SoundBehavior.RandomFrequencyOffset_Hz))));
            }

            XDocument document = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("Soundscape", new XElement("SoundsFilePath", mSoundsFilePath), sourcesElement));

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = false
            };

            /*
             * Write to a temporary file first so an exception does not leave
             * the original soundscape file partially overwritten.
             */
            string temporaryPath = fullPath + ".tmp";

            try
            {
                using (XmlWriter writer = XmlWriter.Create(temporaryPath, settings))
                {
                    document.Save(writer);
                }

                if (File.Exists(fullPath))
                {
                    string backupPath = fullPath + ".bak";

                    try
                    {
                        File.Replace(temporaryPath, fullPath, backupPath, true);

                        if (File.Exists(backupPath))
                        {
                            File.Delete(backupPath);
                        }
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Delete(fullPath);
                        File.Move(temporaryPath, fullPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

        }

        #endregion

        #region Private Members

        private static bool WaveFormatsMatch(WaveFormat first, WaveFormat second)
        {
            return first.SampleRate == second.SampleRate &&
                   first.Channels == second.Channels &&
                   first.Encoding == second.Encoding &&
                   first.BitsPerSample == second.BitsPerSample;
        }

        private void ThrowIfDisposed()
        {
            if (mDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundEngine));
            }
        }

        private static string FormatDouble(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new InvalidDataException("A sound-source numeric value is not finite.");
            }

            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static double ReadDouble(XElement parent, string elementName)
        {
            string value = ReadRequiredValue(parent, elementName);

            double result;

            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                throw CreateInvalidValueException(parent.Element(elementName), elementName, value);
            }

            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw CreateInvalidValueException(parent.Element(elementName), elementName, value);
            }

            return result;
        }

        private static int ReadInt32(XElement parent, string elementName)
        {
            string value = ReadRequiredValue(parent, elementName);

            int result;

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                throw CreateInvalidValueException(parent.Element(elementName), elementName, value);
            }

            return result;
        }

        private static TEnum ReadEnum<TEnum>(XElement parent, string elementName) where TEnum : struct
        {
            string value = ReadRequiredValue(parent, elementName);

            TEnum result;

            if (!Enum.TryParse(value, true, out result) || !Enum.IsDefined(typeof(TEnum), result))
            {
                throw CreateInvalidValueException(parent.Element(elementName), elementName, value);
            }

            return result;
        }

        private static string ReadRequiredValue(XElement parent, string elementName)
        {
            XElement element = parent.Element(elementName);

            if (element == null)
            {
                throw CreateMissingElementException(parent, elementName);
            }

            return element.Value;
        }

        private static InvalidDataException CreateMissingElementException(XElement parent, string elementName)
        {
            return new InvalidDataException("Required XML element '" + elementName + "' is missing from '" + parent.Name.LocalName + "'." + GetLineDescription(parent));
        }

        private static InvalidDataException CreateInvalidValueException(XElement element, string elementName, string value)
        {
            return new InvalidDataException("The value '" + value + "' is invalid for XML element '" + elementName + "'." + GetLineDescription(element));
        }

        private static string GetLineDescription(XElement element)
        {
            IXmlLineInfo lineInfo = element as IXmlLineInfo;

            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                return " Line " + lineInfo.LineNumber.ToString(CultureInfo.InvariantCulture) + ", position " + lineInfo.LinePosition.ToString(CultureInfo.InvariantCulture) + ".";
            }

            return string.Empty;
        }

        #endregion

    }
}
