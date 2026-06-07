using System.IO;
using System.Net.Http;
using Newtonsoft.Json;

namespace SoundMixer;

internal enum OfficialBlacklistSyncResult
{
    Updated,
    UpToDate,
    Failed,
}

internal static class OfficialBlacklistSync
{
    private const string EmbeddedResourceName = "SoundMixer.OfficialSoundBlacklist.json";

    private const string RemoteUrl =
        "https://raw.githubusercontent.com/kyodaikokata/SoundMixer/main/SoundMixer/OfficialSoundBlacklist.json";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(12),
    };

    static OfficialBlacklistSync()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SoundMixer");
    }

    private static OfficialSoundBlacklistDto? s_cachedOfficial;

    internal static OfficialSoundBlacklistDto? CachedOfficial => s_cachedOfficial;

    internal static void Initialize(Plugin plugin)
    {
        s_cachedOfficial = LoadEmbedded();
        SoundBlacklist.Rebuild(plugin.Config, s_cachedOfficial);
        Services.PluginLog.Info(
            $"SoundMixer: blacklist loaded (official rev {s_cachedOfficial?.Revision ?? 0}, "
                + $"user {SoundBlacklist.UserRuleCount}, official {SoundBlacklist.OfficialRuleCount})"
        );

        PersistRevisionIfNeeded(plugin, s_cachedOfficial);
        _ = Task.Run(() => TrySyncRemoteAsync(plugin));
    }

    internal static void RebuildFromConfig(Configuration config)
    {
        SoundBlacklist.Rebuild(config, s_cachedOfficial);
    }

    internal static void RequestRemoteSync(
        Plugin plugin,
        bool force = false,
        Action<OfficialBlacklistSyncResult, int, int>? onComplete = null
    )
    {
        _ = Task.Run(() => TrySyncRemoteAsync(plugin, force, onComplete));
    }

    private static async Task TrySyncRemoteAsync(
        Plugin plugin,
        bool force = false,
        Action<OfficialBlacklistSyncResult, int, int>? onComplete = null
    )
    {
        try
        {
            var json = await HttpClient.GetStringAsync(RemoteUrl).ConfigureAwait(false);
            var remote = DeserializeOfficial(json);
            if (remote == null || !LooksLikeValidOfficial(remote))
            {
                Services.PluginLog.Warning("SoundMixer: official blacklist remote parse failed");
                NotifyComplete(onComplete, OfficialBlacklistSyncResult.Failed, 0, 0);
                return;
            }

            var previousRevision = plugin.Config.OfficialBlacklistRevision;
            if (!force && remote.Revision <= previousRevision)
            {
                NotifyComplete(
                    onComplete,
                    OfficialBlacklistSyncResult.UpToDate,
                    remote.Revision,
                    remote.Entries.Count
                );
                return;
            }

            s_cachedOfficial = remote;
            var result = remote.Revision > previousRevision
                ? OfficialBlacklistSyncResult.Updated
                : OfficialBlacklistSyncResult.UpToDate;
            await Services.Framework.Run(() =>
            {
                SoundBlacklist.Rebuild(plugin.Config, remote);
                plugin.Config.OfficialBlacklistRevision = remote.Revision;
                plugin.Config.Save();

                Services.PluginLog.Info(
                    $"SoundMixer: official blacklist updated to rev {remote.Revision} "
                        + $"({SoundBlacklist.OfficialRuleCount} entries, updated {remote.Updated ?? "unknown"})"
                );

                NotifyComplete(onComplete, result, remote.Revision, SoundBlacklist.OfficialRuleCount);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Services.PluginLog.Warning(ex, "SoundMixer: official blacklist remote sync failed");
            NotifyComplete(onComplete, OfficialBlacklistSyncResult.Failed, 0, 0);
        }
    }

    private static void PersistRevisionIfNeeded(Plugin plugin, OfficialSoundBlacklistDto? official)
    {
        if (official == null || official.Revision <= plugin.Config.OfficialBlacklistRevision)
        {
            return;
        }

        Services.Framework.Run(() =>
        {
            plugin.Config.OfficialBlacklistRevision = official.Revision;
            plugin.Config.Save();
        });
    }

    private static OfficialSoundBlacklistDto? DeserializeOfficial(string json)
    {
        return JsonConvert.DeserializeObject<OfficialSoundBlacklistDto>(json);
    }

    private static bool LooksLikeValidOfficial(OfficialSoundBlacklistDto dto)
    {
        return dto.Revision > 0
            || dto.Entries.Count > 0
            || dto.Patterns.Count > 0
            || dto.Keywords.Count > 0;
    }

    private static void NotifyComplete(
        Action<OfficialBlacklistSyncResult, int, int>? onComplete,
        OfficialBlacklistSyncResult result,
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
            onComplete(result, revision, entryCount);
        }
        catch (Exception ex)
        {
            Services.PluginLog.Debug(ex, "SoundMixer: official blacklist sync callback failed");
        }
    }

    private static OfficialSoundBlacklistDto LoadEmbedded()
    {
        var assembly = typeof(OfficialBlacklistSync).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
        if (stream == null)
        {
            Services.PluginLog.Warning(
                $"SoundMixer: embedded official blacklist not found ({EmbeddedResourceName})"
            );
            return new OfficialSoundBlacklistDto();
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var loaded = DeserializeOfficial(json);
        if (loaded == null || !LooksLikeValidOfficial(loaded))
        {
            Services.PluginLog.Warning(
                "SoundMixer: embedded official blacklist failed to deserialize; "
                    + "check OfficialSoundBlacklist.json and DTO JsonProperty mappings"
            );
            return new OfficialSoundBlacklistDto();
        }

        return loaded;
    }
}
