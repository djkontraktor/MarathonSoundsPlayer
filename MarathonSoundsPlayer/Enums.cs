namespace MarathonSoundsPlayer
{
    public enum SoundSourceType
    {
        Ambient,
        Random,
        Platform,
    }

    public enum PlatformStartStopType
    {
        StartExtend,
        StartRetract,
        StopExtend,
        StopRetract,
    }

    public enum PlatformState
    {
        Stopped,
        RetractedDelay,
        Extending,
        ExtendedDelay,
        Retracting,
    }

}
