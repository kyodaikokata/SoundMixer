namespace SoundMixer.Api;

/// <summary>IPC gate names for external plugins. Subscribe via IDalamudPluginInterface.GetIpcSubscriber.</summary>
public static class SoundMixerIpcGates
{
    public const int ApiVersion = 1;

    public const string Initialized = "SoundMixer.Initialized";
    public const string Disposed = "SoundMixer.Disposed";
    public const string StateChanged = "SoundMixer.StateChanged";

    public const string GetApiVersion = "SoundMixer.GetApiVersion";
    public const string GetEnabled = "SoundMixer.GetEnabled";
    public const string GetSavedEnabled = "SoundMixer.GetSavedEnabled";
    public const string GetPresetNames = "SoundMixer.GetPresetNames";
    public const string GetActivePresetName = "SoundMixer.GetActivePresetName";
    public const string GetSavedActivePresetName = "SoundMixer.GetSavedActivePresetName";
    public const string GetGroupsJson = "SoundMixer.GetGroupsJson";
    public const string GetGroupVolume = "SoundMixer.GetGroupVolume";

    public const string SetEnabled = "SoundMixer.SetEnabled";
    public const string SwitchPreset = "SoundMixer.SwitchPreset";
    public const string SetGroupVolume = "SoundMixer.SetGroupVolume";

    public const string SetTemporaryEnabled = "SoundMixer.SetTemporaryEnabled";
    public const string ClearTemporaryEnabled = "SoundMixer.ClearTemporaryEnabled";
    public const string SetTemporaryPreset = "SoundMixer.SetTemporaryPreset";
    public const string ClearTemporaryPreset = "SoundMixer.ClearTemporaryPreset";
    public const string SetTemporaryGroupVolume = "SoundMixer.SetTemporaryGroupVolume";
    public const string ClearTemporaryGroupVolume = "SoundMixer.ClearTemporaryGroupVolume";
    public const string RemoveTemporaryOverrides = "SoundMixer.RemoveTemporaryOverrides";
    public const string RemoveAllTemporaryOverrides = "SoundMixer.RemoveAllTemporaryOverrides";
}
