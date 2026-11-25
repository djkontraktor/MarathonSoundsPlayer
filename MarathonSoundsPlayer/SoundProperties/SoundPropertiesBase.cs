using MarathonFileUtils.Sounds;
using NAudio.Effects;
using System.Collections.ObjectModel;

namespace MarathonSoundsPlayer.SoundProperties
{
    public class SoundPropertiesBase
    {
        public SoundEngine SoundEngine { get; set; }
        public double Pitch { get; set; }
        public SoundsFileFormat Format
        {
            get
            {
                return SoundEngine.Format;
            }
        }
        public ObservableCollection<IAudioEffect> Effects { get; set; }

        public SoundPropertiesBase(SoundEngine soundEngine, double pitch, ObservableCollection<IAudioEffect> effects)
        {
            SoundEngine = soundEngine;
            Pitch = pitch;
            Effects = effects;
        }
    }
}
