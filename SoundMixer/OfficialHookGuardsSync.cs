using System.IO;
using System.Net.Http;
using Newtonsoft.Json;

namespace SoundMixer;

internal enum OfficialHookGuardsSyncResult
{
    Updated,
    UpToDate,
    Failed,
}

internal static class OfficialHookGuardsSync
{
    private const string EmbeddedResourceName = "SoundMixer.OfficialHookGuards.json";

    private const string RemoteUrl =
        "https://raw.githubusercontent.com/kyodaikokata/SoundMixer/main/SoundMixer/OfficialHookGuards.json";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(12),
    };

    static OfficialHookGuardsSync()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SoundMixer");
    }

    private static OfficialHookGuardsDto? s_cachedOfficial;

    internal static OfficialHookGuardsDto? CachedOfficial => s_cachedOfficial;

    internal static void Initialize(Plugin plugin)
    {
        s_cachedOfficial = LoadEmbedded();
        HookGuardPolicy.Rebuild(plugin.Config, s_cachedOfficial);
        Services.PluginLog.Info(
            $"SoundMixer: hook guards loaded (official rev {s_cachedOfficial?.Revision ?? 0}, "
                + $"user {HookGuardPolicy.UserRuleCount})"
        );

        PersistRevisionIfNeeded(plugin, s_cachedOfficial);
        _ = Task.Run(() => TrySyncRemoteAsync(plugin));
    }

    internal static void RebuildFromConfig(Configuration config)
    {
        HookGuardPolicy.Rebuild(config, s_cachedOfficial);
    }

    internal static void RequestRemoteSync(
        Plugin pluginInstance,
        bool force = false,
        Action<OfficialHookGuardsSyncResult, int, int>? onComplete = null
    )
    {
        _ = Task.Run(() => TrySyncRemoteAsync(pluginInstance, force, onComplete));
    }

    private static async Task TrySyncRemoteAsync(
        Plugin pluginInstance,
        bool force = false,
        Action<OfficialHookGuardsSyncResult, int, int>? onComplete = null
    )
    {
        try
        {
            var json = await HttpClient.GetStringAsync(RemoteUrl).ConfigureAwait(false);
            var remote = Deserialize(json);
            if (remote == null || remote.Entries.Count == 0)
            {
                Services.PluginLog.Warning("SoundMixer: official hook guards remote parse failed");
                NotifyComplete(onComplete, OfficialHookGuardsSyncResult.Failed, 0, 0);
                return;
            }

            var previousRevision = pluginInstance.Config.OfficialHookGuardsRevision;
            if (!force && remote.Revision <= previousRevision)
            {
                NotifyComplete(
                    onComplete,
                    OfficialHookGuardsSyncResult.UpToDate,
                    remote.Revision,
                    remote.Entries.Count
                );
                return;
            }

            s_cachedOfficial = remote;
            var result = remote.Revision > previousRevision
                ? OfficialHookGuardsSyncResult.Updated
                : OfficialHookGuardsSyncResult.UpToDate;

            await Services.Framework.Run(() =>
            {
                pluginInstance.Config.OfficialHookGuardsRevision = remote.Revision;
                try
                {
                    pluginInstance.Config.Save();
                }
                catch (Exception ex)
                {
                    Services.PluginLog.Warning(ex, "SoundMixer: failed to save hook guards revision");
                }

                HookGuardPolicy.Rebuild(pluginInstance.Config, s_cachedOfficial);
                pluginInstance.ApplyHookGuardState();
                NotifyComplete(onComplete, result, remote.Revision, remote.Entries.Count);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Services.PluginLog.Warning(ex, "SoundMixer: official hook guards remote sync failed");
            NotifyComplete(onComplete, OfficialHookGuardsSyncResult.Failed, 0, 0);
        }
    }

    private static void PersistRevisionIfNeeded(Plugin pluginInstance, OfficialHookGuardsDto? official)
    {
        if (official == null || official.Revision <= pluginInstance.Config.OfficialHookGuardsRevision)
        {
            return;
        }

        try
        {
            pluginInstance.Config.OfficialHookGuardsRevision = official.Revision;
            pluginInstance.Config.Save();
        }
        catch (Exception ex)
        {
            Services.PluginLog.Warning(ex, "SoundMixer: failed to persist hook guards revision");
        }
    }

    private static void NotifyComplete(
        Action<OfficialHookGuardsSyncResult, int, int>? onComplete,
        OfficialHookGuardsSyncResult result,
        int revision,
        int entryCount
    )
    {
        if (onComplete == null)
        {
            return;
        }

        try
        {
            Services.Framework.Run(() => onComplete(result, revision, entryCount));
        }
        catch
        {
            onComplete(result, revision, entryCount);
        }
    }

    private static OfficialHookGuardsDto? LoadEmbedded()
    {
        var assembly = typeof(OfficialHookGuardsSync).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
        if (stream == null)
        {
            Services.PluginLog.Warning($"SoundMixer: embedded hook guards not found ({EmbeddedResourceName})");
            return null;
        }

        using var reader = new StreamReader(stream);
        return Deserialize(reader.ReadToEnd());
    }

    private static OfficialHookGuardsDto? Deserialize(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<OfficialHookGuardsDto>(json);
        }
        catch (Exception ex)
        {
            Services.PluginLog.Warning(ex, "SoundMixer: failed to deserialize hook guards JSON");
            return null;
        }
    }
}
