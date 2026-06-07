using Newtonsoft.Json;
using SoundMixer.Localization;

namespace SoundMixer;

public enum SoundBlacklistMatchKind
{
    Keyword = 0,
    Path = 1,
    Glob = 2,
}

[Serializable]
public class UserSoundBlacklistEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public SoundBlacklistMatchKind MatchKind { get; set; } = SoundBlacklistMatchKind.Keyword;

    public string Match { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}

internal sealed class OfficialSoundBlacklistNoteDto
{
    [JsonProperty("zh")]
    internal string Zh { get; set; } = string.Empty;

    [JsonProperty("en")]
    internal string En { get; set; } = string.Empty;
}

internal sealed class OfficialSoundBlacklistEntryDto
{
    [JsonProperty("kind")]
    internal string Kind { get; set; } = "keyword";

    [JsonProperty("match")]
    internal string Match { get; set; } = string.Empty;

    /// <summary>Legacy single-language note; used when <see cref="Notes"/> is absent.</summary>
    [JsonProperty("note")]
    internal string Note { get; set; } = string.Empty;

    [JsonProperty("notes")]
    internal OfficialSoundBlacklistNoteDto? Notes { get; set; }

    internal string ResolveNote(LanguageMode languageMode)
    {
        if (Notes != null)
        {
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

            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback.Trim();
            }
        }

        return Note?.Trim() ?? string.Empty;
    }
}

internal sealed class OfficialSoundBlacklistDto
{
    [JsonProperty("revision")]
    internal int Revision { get; set; }

    [JsonProperty("updated")]
    internal string? Updated { get; set; }

    [JsonProperty("entries")]
    internal List<OfficialSoundBlacklistEntryDto> Entries { get; set; } = new();

    /// <summary>Legacy flat lists; used when <see cref="Entries"/> is empty.</summary>
    [JsonProperty("patterns")]
    internal List<string> Patterns { get; set; } = new();

    [JsonProperty("keywords")]
    internal List<string> Keywords { get; set; } = new();
}

internal readonly struct CompiledBlacklistRule
{
    internal SoundBlacklistMatchKind Kind { get; init; }
    internal string Match { get; init; }
    internal DotNet.Globbing.Glob? Glob { get; init; }
}
