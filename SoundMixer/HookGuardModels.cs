using Newtonsoft.Json;
using SoundMixer.Localization;

namespace SoundMixer;

public enum HookGuardTrigger
{
    Mounted = 0,
    GuideroidGrace = 1,
    InCombat = 2,
    WeaponsOut = 3,
    OccupiedInEvent = 4,
    BoundByDuty = 5,
    Jumping = 6,
    Casting = 7,
}

[Serializable]
public class UserHookGuardEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public bool Enabled { get; set; } = true;

    public HookGuardTrigger Trigger { get; set; } = HookGuardTrigger.Mounted;

    public List<string> DisabledHooks { get; set; } = new();

    public bool SkipActiveListScan { get; set; }

    public string Note { get; set; } = string.Empty;
}

internal sealed class OfficialHookGuardNoteDto
{
    [JsonProperty("zh")]
    internal string Zh { get; set; } = string.Empty;

    [JsonProperty("en")]
    internal string En { get; set; } = string.Empty;
}

internal sealed class OfficialHookGuardEntryDto
{
    [JsonProperty("id")]
    internal string Id { get; set; } = string.Empty;

    [JsonProperty("trigger")]
    internal string Trigger { get; set; } = "mounted";

    [JsonProperty("disableHooks")]
    internal List<string> DisabledHooks { get; set; } = new();

    [JsonProperty("skipActiveListScan")]
    internal bool SkipActiveListScan { get; set; }

    [JsonProperty("notes")]
    internal OfficialHookGuardNoteDto? Notes { get; set; }

    internal HookGuardTrigger ResolveTrigger()
    {
        return Trigger?.Trim().ToLowerInvariant() switch
        {
            "guideroid_grace" or "guideroid" => HookGuardTrigger.GuideroidGrace,
            _ => HookGuardTrigger.Mounted,
        };
    }

    internal string ResolveNote(LanguageMode languageMode)
    {
        if (Notes == null)
        {
            return string.Empty;
        }

        var preferChinese = languageMode switch
        {
            LanguageMode.Chinese => true,
            LanguageMode.English => false,
            _ => System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith(
                "zh",
                StringComparison.OrdinalIgnoreCase
            ),
        };

        var primary = preferChinese ? Notes.Zh : Notes.En;
        var fallback = preferChinese ? Notes.En : Notes.Zh;
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary.Trim();
        }

        return fallback?.Trim() ?? string.Empty;
    }
}

internal sealed class OfficialHookGuardsDto
{
    [JsonProperty("revision")]
    internal int Revision { get; set; }

    [JsonProperty("updated")]
    internal string? Updated { get; set; }

    [JsonProperty("entries")]
    internal List<OfficialHookGuardEntryDto> Entries { get; set; } = new();
}

internal readonly struct CompiledHookGuardRule
{
    internal HookGuardTrigger Trigger { get; init; }
    internal HashSet<HookDebugId> DisabledHooks { get; init; }
    internal bool SkipActiveListScan { get; init; }
    internal bool FromOfficial { get; init; }
}
