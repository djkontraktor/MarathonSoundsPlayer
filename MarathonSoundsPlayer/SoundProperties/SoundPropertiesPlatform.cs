using MarathonFileUtils.Sounds.DataStructures;
using NAudio.Effects;
using System.Collections.ObjectModel;

namespace MarathonSoundsPlayer.SoundProperties
{
    public class SoundPropertiesPlatform : SoundPropertiesBase
    {

        public SoundSlot PlatformStartExtendSound { get; set; }
        public SoundSlot PlatformStartRetractSound { get; set; }
        public SoundSlot PlatformStopExtendSound { get; set; }
        public SoundSlot PlatformStopRetractSound { get; set; }
        public SoundSlot PlatformRunningSound { get; set; }
        public double ExtendedVolume { get; set; }
        public double RetractedVolume { get; set; }
        public double ExtendedPan { get; set; }
        public double RetractedPan { get; set; }
        public double ExtendSpeed_WU_s { get; set; }
        public double RetractSpeed_WU_s { get; set; }
        public double ExtendDelay_s { get; set; }
        public double RetractDelay_s { get; set; }
        public double Distance_WU { get; set; }

        public SoundPropertiesPlatform(SoundEngine soundEngine,
            SoundSlot platformStartExtendSound, SoundSlot platformStartRetractSound, SoundSlot platformStopExtendSound, SoundSlot platformStopRetractSound, SoundSlot platformRunningSound,
            double extendedVolume, double retractedVolume, double pitch, double extendedPan, double retractedPan, double extendSpeed_WU_s, double retractSpeed_WU_s, double extendDelay_s, double retractDelay_s, double distance_WU, ObservableCollection<IAudioEffect> effects) : base(soundEngine, pitch, effects)
        {
            PlatformStartExtendSound = platformStartExtendSound;
            PlatformStartRetractSound = platformStartRetractSound;
            PlatformStopExtendSound = platformStopExtendSound;
            PlatformStopRetractSound = platformStopRetractSound;
            PlatformRunningSound = platformRunningSound;
            ExtendedVolume = extendedVolume;
            RetractedVolume = retractedVolume;
            ExtendedPan = extendedPan;
            RetractedPan = retractedPan;
            ExtendSpeed_WU_s = extendSpeed_WU_s;
            RetractSpeed_WU_s = retractSpeed_WU_s;
            ExtendDelay_s = extendDelay_s;
            RetractDelay_s = retractDelay_s;
            Distance_WU = distance_WU;
        }

    }
}
