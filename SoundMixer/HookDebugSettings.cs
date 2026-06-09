namespace SoundMixer;

internal enum HookDebugId
{
    PlaySpecificSound,
    PlaySound,
    PlaySystemSound,
    PlayClipSound,
    PlayMovieSound,
    PlayBgmSound,
    PlayWeatherSound,
    SetVolume,
    GetVolume,
    LoadSoundFile,
    GetResourceSync,
    GetResourceAsync,
}

[Serializable]
public class HookDebugSettings
{
    /// <summary>When on, per-hook checkboxes below control runtime Enable/Disable.</summary>
    public bool ManualControl { get; set; }

    /// <summary>Debug tab only: allow up to 10000% linear gain and raise engine apply cap to match.</summary>
    public bool DebugExtremeVolume { get; set; }

    public bool PlaySpecificSound { get; set; } = true;
    public bool PlaySound { get; set; } = false;
    public bool PlaySystemSound { get; set; } = true;
    public bool PlayClipSound { get; set; } = true;
    public bool PlayMovieSound { get; set; } = true;
    public bool PlayBgmSound { get; set; } = true;
    public bool PlayWeatherSound { get; set; } = true;
    public bool SetVolume { get; set; } = true;
    public bool GetVolume { get; set; } = true;
    public bool LoadSoundFile { get; set; } = true;
    public bool GetResourceSync { get; set; } = true;
    public bool GetResourceAsync { get; set; } = true;

    internal bool GetDesired(HookDebugId id) =>
        id switch
        {
            HookDebugId.PlaySpecificSound => PlaySpecificSound,
            HookDebugId.PlaySound => PlaySound,
            HookDebugId.PlaySystemSound => PlaySystemSound,
            HookDebugId.PlayClipSound => PlayClipSound,
            HookDebugId.PlayMovieSound => PlayMovieSound,
            HookDebugId.PlayBgmSound => PlayBgmSound,
            HookDebugId.PlayWeatherSound => PlayWeatherSound,
            HookDebugId.SetVolume => SetVolume,
            HookDebugId.GetVolume => GetVolume,
            HookDebugId.LoadSoundFile => LoadSoundFile,
            HookDebugId.GetResourceSync => GetResourceSync,
            HookDebugId.GetResourceAsync => GetResourceAsync,
            _ => true,
        };

    internal void SetDesired(HookDebugId id, bool enabled)
    {
        switch (id)
        {
            case HookDebugId.PlaySpecificSound: PlaySpecificSound = enabled; break;
            case HookDebugId.PlaySound: PlaySound = enabled; break;
            case HookDebugId.PlaySystemSound: PlaySystemSound = enabled; break;
            case HookDebugId.PlayClipSound: PlayClipSound = enabled; break;
            case HookDebugId.PlayMovieSound: PlayMovieSound = enabled; break;
            case HookDebugId.PlayBgmSound: PlayBgmSound = enabled; break;
            case HookDebugId.PlayWeatherSound: PlayWeatherSound = enabled; break;
            case HookDebugId.SetVolume: SetVolume = enabled; break;
            case HookDebugId.GetVolume: GetVolume = enabled; break;
            case HookDebugId.LoadSoundFile: LoadSoundFile = enabled; break;
            case HookDebugId.GetResourceSync: GetResourceSync = enabled; break;
            case HookDebugId.GetResourceAsync: GetResourceAsync = enabled; break;
        }
    }

    internal void SetAll(bool enabled)
    {
        PlaySpecificSound = enabled;
        PlaySound = false;
        PlaySystemSound = enabled;
        PlayClipSound = enabled;
        PlayMovieSound = enabled;
        PlayBgmSound = enabled;
        PlayWeatherSound = enabled;
        SetVolume = enabled;
        GetVolume = enabled;
        LoadSoundFile = enabled;
        GetResourceSync = enabled;
        GetResourceAsync = enabled;
    }
}

internal readonly struct HookRuntimeStatus
{
    internal bool IsResolved { get; init; }
    internal bool IsRuntimeEnabled { get; init; }
}
