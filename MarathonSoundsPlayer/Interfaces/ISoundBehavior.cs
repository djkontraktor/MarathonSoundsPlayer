using MarathonFileUtils.Sounds;
using MarathonFileUtils.Sounds.DataStructures;
using NAudio.Effects;
using System.Collections.ObjectModel;

namespace MarathonSoundsPlayer.Interfaces
{
    public interface ISoundBehavior : IDisposable
    {
        // Common
        SoundsFileFormat Format { get; }
        Dictionary<string, SoundSlot> SoundCatalog { get; set; }
        SoundSlot PlatformStartExtendSound { get; set; }
        SoundSlot PlatformStopExtendSound { get; set; }
        SoundSlot PlatformStartRetractSound { get; set; }
        SoundSlot PlatformStopRetractSound { get; set; }
        SoundSlot CurrentSound { get; set; }
        ObservableCollection<IAudioEffect> Effects { get; set; }
        double Pitch { get; set; }
        void Start();
        void Stop();
        // Ambient/Random Sounds
        double Volume { get; set; }
        double RandomVolume { get; set; }
        double RandomVolumeOffset { get; set; }
        double RandomPitch { get; set; }
        double RandomPitchOffset { get; set; }
        double Pan { get; set; }
        double RandomPan { get; set; }
        double RandomPanOffset { get; set; }
        bool Playing { get; }
        // Random Sounds
        double Frequency_Hz { get; set; }
        double RandomFrequency_Hz { get; set; }
        double RandomFrequencyOffset_Hz { get; set; }
        // Platform Sounds
        double ExtendedVolume { get; set; }
        double RetractedVolume { get; set; }
        double ExtendedPan { get; set; }
        double RetractedPan { get; set; }
        double ExtendSpeed_WU_s { get; set; }
        double RetractSpeed_WU_s { get; set; }
        double ExtendDelay_s { get; set; }
        double RetractDelay_s { get; set; }
        double Distance_WU { get; set; }
    }
}
