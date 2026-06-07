using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace SoundMixer;

internal static class DefaultConfigLoader
{
    private const string ResourceName = "SoundMixer.DefaultConfiguration.json";

    internal static bool TryLoad(out Configuration configuration)
    {
        configuration = new Configuration();

        var assembly = typeof(DefaultConfigLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream == null)
        {
            Services.PluginLog.Warning($"SoundMixer: embedded default config not found ({ResourceName})");
            return false;
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var loaded = JsonConvert.DeserializeObject<Configuration>(json);
        if (loaded == null)
        {
            Services.PluginLog.Warning("SoundMixer: failed to deserialize embedded default config");
            return false;
        }

        configuration = loaded;
        return true;
    }

    internal static void ApplyGroupsAndPresets(Configuration target, Configuration source)
    {
        target.Presets = source.Presets;
        target.Groups = source.Groups;
        target.ActivePresetId = source.ActivePresetId;
        target.SoundToGroup = source.SoundToGroup;
        target.IndividualVolumes = source.IndividualVolumes;
        target.PathAliases = source.PathAliases;
        target.ExpertMode = source.ExpertMode;
        target.SafeMode = source.SafeMode;
        target.UiLanguage = source.UiLanguage;
        target.Version = source.Version;
        target.InvalidateGlobCache();
    }
}
