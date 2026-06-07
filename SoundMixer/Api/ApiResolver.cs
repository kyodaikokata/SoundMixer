using System.Linq;

namespace SoundMixer.Api;

internal static class ApiResolver
{
    internal static SoundPreset? FindPreset(Configuration config, string nameOrId)
    {
        if (string.IsNullOrWhiteSpace(nameOrId))
        {
            return null;
        }

        var byId = PresetManager.FindPreset(config, nameOrId);
        if (byId != null)
        {
            return byId;
        }

        return config.Presets.FirstOrDefault(
            p => string.Equals(p.Name, nameOrId, StringComparison.OrdinalIgnoreCase)
        );
    }

    internal static SoundGroup? FindGroup(Configuration config, string nameOrId)
    {
        if (string.IsNullOrWhiteSpace(nameOrId))
        {
            return null;
        }

        var byId = GroupHierarchy.FindById(config, nameOrId);
        if (byId != null)
        {
            return byId;
        }

        return config.Groups.FirstOrDefault(
            g => string.Equals(g.Name, nameOrId, StringComparison.OrdinalIgnoreCase)
        );
    }

    internal static SoundGroup? FindGroup(EffectiveSnapshot snapshot, string nameOrId)
    {
        if (string.IsNullOrWhiteSpace(nameOrId))
        {
            return null;
        }

        var byId = snapshot.Groups.FirstOrDefault(g => g.Id == nameOrId);
        if (byId != null)
        {
            return byId;
        }

        return snapshot.Groups.FirstOrDefault(
            g => string.Equals(g.Name, nameOrId, StringComparison.OrdinalIgnoreCase)
        );
    }
}
