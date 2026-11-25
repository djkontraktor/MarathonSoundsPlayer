using MarathonFileUtils.Sounds;
using NAudio.Effects;
using System.Collections.ObjectModel;

namespace MarathonSoundsPlayer.SoundProperties
{
    public class EnvironmentalSoundProperties : SoundPropertiesBase
    {

        public SoundSlot SoundSlot { get; set; }
        public double Volume { get; set; }
        public double RandomVolume { get; set; }
        public double RandomVolumeOffset { get; set; }
        public double RandomPitch { get; set; }
        public double RandomPitchOffset { get; set; }
        public double Pan { get; set; }
        public double RandomPan { get; set; }
        public double RandomPanOffset { get; set; }

        public EnvironmentalSoundProperties(SoundEngine soundEngine, SoundSlot soundSlot, double volume, double randomVolume, double randomVolumeOffset, double pitch, double randomPitch, double randomPitchOffset, double pan, double randomPan, double randomPanOffset, ObservableCollection<IAudioEffect> effects) : base(soundEngine, pitch, effects)
        {
            SoundSlot = soundSlot;
            Volume = volume;
            RandomVolume = randomVolume;
            RandomVolumeOffset = randomVolumeOffset;
            RandomPitch = randomPitch;
            RandomPitchOffset = randomPitchOffset;
            Pan = pan;
            RandomPan = randomPan;
            RandomPanOffset = randomPanOffset;
        }
    }
}
