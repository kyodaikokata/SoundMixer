using System.Collections.Generic;
using SoundMixer.Localization;
using static SoundMixer.Localization.Loc.Keys;

namespace SoundMixer;

internal static class SoundClassifier
{
    private static readonly (string[] Patterns, string CategoryKey, string Icon)[] Rules =
    [
        (["**/music/**", "**/bgm/**"], BuiltinBgm, "[M]"),
        (["**/job/war/**", "**/warrior/**"], ClassifyJobWar, "[J]"),
        (["**/job/sam/**", "**/samurai/**"], ClassifyJobSam, "[J]"),
        (["**/job/**", "**/skill/**"], BuiltinJob, "[J]"),
        (["**/env/wind/**", "**/weather/wind/**"], ClassifyEnvWind, "[W]"),
        (["**/env/rain/**", "**/weather/rain/**"], ClassifyEnvRain, "[R]"),
        (["**/foot/**", "**/footstep/**", "**/step/**"], ClassifyEnvFoot, "[F]"),
        (["**/ambient/**", "**/stream/**"], ClassifyEnvAmbient, "[E]"),
        (["**/env/**", "**/weather/**"], BuiltinEnv, "[E]"),
        (["**/battle/**", "**/combat/**", "**/critical/**"], BuiltinBattle, "[B]"),
        (["**/ui/**", "**/menu/**"], BuiltinUi, "[U]"),
    ];

    private static readonly Dictionary<string, DotNet.Globbing.Glob> GlobCache = new();

    public static (string Category, string Icon) Classify(string soundPath)
    {
        var normalized = soundPath.ToLowerInvariant();

        foreach (var (patterns, categoryKey, icon) in Rules)
        {
            foreach (var pattern in patterns)
            {
                if (!GlobCache.TryGetValue(pattern, out var glob))
                {
                    glob = DotNet.Globbing.Glob.Parse(pattern);
                    GlobCache[pattern] = glob;
                }

                if (glob.IsMatch(normalized))
                {
                    return (Loc.Get(categoryKey), icon);
                }
            }
        }

        return (Loc.Get(ClassifyUncategorized), "[?]");
    }
}
