using MarathonFileUtils.Sounds;
using NAudio.Effects;
using System.Collections.ObjectModel;

namespace MarathonSoundsPlayer.SoundProperties
{
    public class RandomSoundProperties : EnvironmentalSoundProperties
    {

        public double Frequency_Hz { get; set; }
        public double RandomFrequency_Hz { get; set; }
        public double RandomFrequencyOffset_Hz { get; set; }

        public RandomSoundProperties(SoundEngine soundEngine, SoundSlot soundSlot, double volume, double randomVolume, double randomVolumeOffset, double pitch, double randomPitch, double randomPitchOffset, double pan, double randomPan, double randomPanOffset, double frequency_Hz, double randomFrequency_Hz, double randomFrequencyOffset_Hz, ObservableCollection<IAudioEffect> effects) :
            base(soundEngine, soundSlot, volume, randomVolume, randomVolumeOffset, pitch, randomPitch, randomPitchOffset, pan, randomPan, randomPanOffset, effects)
        {
            Frequency_Hz = frequency_Hz;
            RandomFrequency_Hz = randomFrequency_Hz;
            RandomFrequencyOffset_Hz = randomFrequencyOffset_Hz;
        }
    }
}
