using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace SoundMixer.Api;

internal sealed class SoundMixerIpcProviders : IDisposable
{
    private readonly List<ICallGateProvider> _providers = new();
    private readonly SoundMixerApi _api;
    private readonly Action<string> _stateChangedHandler;

    internal SoundMixerIpcProviders(IDalamudPluginInterface pluginInterface, SoundMixerApi api)
    {
        _api = api;

        AddFunc0(pluginInterface, SoundMixerIpcGates.GetApiVersion, () => _api.GetApiVersion());
        AddFunc0(pluginInterface, SoundMixerIpcGates.GetEnabled, () => _api.GetEnabled());
        AddFunc0(pluginInterface, SoundMixerIpcGates.GetSavedEnabled, () => _api.GetSavedEnabled());
        AddFunc0(pluginInterface, SoundMixerIpcGates.GetPresetNames, () => _api.GetPresetNames());
        AddFunc0(pluginInterface, SoundMixerIpcGates.GetActivePresetName, () => _api.GetActivePresetName());
        AddFunc0(pluginInterface, SoundMixerIpcGates.GetSavedActivePresetName, () => _api.GetSavedActivePresetName());
        AddFunc0(pluginInterface, SoundMixerIpcGates.GetGroupsJson, () => _api.GetGroupsJson());

        var getGroupVolume = pluginInterface.GetIpcProvider<string, (int, float)>(SoundMixerIpcGates.GetGroupVolume);
        getGroupVolume.RegisterFunc(groupIdOrName =>
        {
            var (ec, volume) = _api.GetGroupVolume(groupIdOrName);
            return ((int)ec, volume);
        });
        _providers.Add(getGroupVolume);

        var setEnabled = pluginInterface.GetIpcProvider<bool, int>(SoundMixerIpcGates.SetEnabled);
        setEnabled.RegisterFunc(enabled => (int)_api.SetEnabled(enabled));
        _providers.Add(setEnabled);

        var switchPreset = pluginInterface.GetIpcProvider<string, int>(SoundMixerIpcGates.SwitchPreset);
        switchPreset.RegisterFunc(presetNameOrId => (int)_api.SwitchPreset(presetNameOrId));
        _providers.Add(switchPreset);

        var setGroupVolume = pluginInterface.GetIpcProvider<string, float, int>(SoundMixerIpcGates.SetGroupVolume);
        setGroupVolume.RegisterFunc((groupIdOrName, volume) => (int)_api.SetGroupVolume(groupIdOrName, volume));
        _providers.Add(setGroupVolume);

        var setTemporaryEnabled = pluginInterface.GetIpcProvider<string, int, bool, int>(
            SoundMixerIpcGates.SetTemporaryEnabled
        );
        setTemporaryEnabled.RegisterFunc(
            (tag, priority, enabled) => (int)_api.SetTemporaryEnabled(tag, priority, enabled)
        );
        _providers.Add(setTemporaryEnabled);

        var clearTemporaryEnabled = pluginInterface.GetIpcProvider<string, int>(
            SoundMixerIpcGates.ClearTemporaryEnabled
        );
        clearTemporaryEnabled.RegisterFunc(tag => (int)_api.ClearTemporaryEnabled(tag));
        _providers.Add(clearTemporaryEnabled);

        var setTemporaryPreset = pluginInterface.GetIpcProvider<string, int, string, int>(
            SoundMixerIpcGates.SetTemporaryPreset
        );
        setTemporaryPreset.RegisterFunc(
            (tag, priority, presetNameOrId) => (int)_api.SetTemporaryPreset(tag, priority, presetNameOrId)
        );
        _providers.Add(setTemporaryPreset);

        var clearTemporaryPreset = pluginInterface.GetIpcProvider<string, int>(SoundMixerIpcGates.ClearTemporaryPreset);
        clearTemporaryPreset.RegisterFunc(tag => (int)_api.ClearTemporaryPreset(tag));
        _providers.Add(clearTemporaryPreset);

        var setTemporaryGroupVolume = pluginInterface.GetIpcProvider<string, int, string, float, int>(
            SoundMixerIpcGates.SetTemporaryGroupVolume
        );
        setTemporaryGroupVolume.RegisterFunc(
            (tag, priority, groupIdOrName, volume) =>
                (int)_api.SetTemporaryGroupVolume(tag, priority, groupIdOrName, volume)
        );
        _providers.Add(setTemporaryGroupVolume);

        var clearTemporaryGroupVolume = pluginInterface.GetIpcProvider<string, string, int>(
            SoundMixerIpcGates.ClearTemporaryGroupVolume
        );
        clearTemporaryGroupVolume.RegisterFunc(
            (tag, groupIdOrName) => (int)_api.ClearTemporaryGroupVolume(tag, groupIdOrName)
        );
        _providers.Add(clearTemporaryGroupVolume);

        var removeTemporaryOverrides = pluginInterface.GetIpcProvider<string, int>(
            SoundMixerIpcGates.RemoveTemporaryOverrides
        );
        removeTemporaryOverrides.RegisterFunc(tag => (int)_api.RemoveTemporaryOverrides(tag));
        _providers.Add(removeTemporaryOverrides);

        AddFunc0(
            pluginInterface,
            SoundMixerIpcGates.RemoveAllTemporaryOverrides,
            () => (int)_api.RemoveAllTemporaryOverrides()
        );

        var initializedProvider = pluginInterface.GetIpcProvider<object>(SoundMixerIpcGates.Initialized);
        var stateChangedProvider = pluginInterface.GetIpcProvider<string, object>(SoundMixerIpcGates.StateChanged);

        _stateChangedHandler = reason => stateChangedProvider.SendMessage(reason);
        _api.StateChanged += _stateChangedHandler;
        initializedProvider.SendMessage();
    }

    internal void NotifyDisposed(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.GetIpcProvider<object>(SoundMixerIpcGates.Disposed).SendMessage();
    }

    public void Dispose()
    {
        _api.StateChanged -= _stateChangedHandler;

        foreach (var provider in _providers)
        {
            provider.UnregisterFunc();
        }

        _providers.Clear();
    }

    private void AddFunc0<TRet>(IDalamudPluginInterface pluginInterface, string gate, Func<TRet> func)
    {
        var provider = pluginInterface.GetIpcProvider<TRet>(gate);
        provider.RegisterFunc(func);
        _providers.Add(provider);
    }
}
