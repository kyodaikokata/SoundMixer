using System;
using System.Collections.Generic;

namespace SoundMixer;

internal static class PathResolver
{
    internal static string ResolveScdPath(Configuration config, string rawPath, nint scdDataPtr = 0)
    {
        return ResolveScdPath(config, rawPath, config.PathAliases, scdDataPtr);
    }

    internal static string ResolveScdPath(
        Configuration config,
        string rawPath,
        IReadOnlyDictionary<string, string> aliases,
        nint scdDataPtr = 0
    )
    {
        rawPath = rawPath.ToLowerInvariant().Trim();

        var alias = LookupAlias(aliases, rawPath, scdDataPtr);
        if (alias != null)
        {
            return alias;
        }

        if (TrySplitSoundIndex(rawPath, out var scdPath, out var index))
        {
            alias = LookupAlias(aliases, scdPath, scdDataPtr);
            if (alias != null)
            {
                return BuildSpecificPath(alias, index);
            }
        }

        return rawPath;
    }

    internal static string BuildSpecificPath(string scdPath, int soundIndex)
    {
        return $"{scdPath.ToLowerInvariant().Trim()}/{soundIndex}";
    }

    internal static bool TrySplitSoundIndex(string path, out string scdPath, out int index)
    {
        scdPath = path;
        index = -1;

        var slash = path.LastIndexOf('/');
        if (slash <= 0)
        {
            return false;
        }

        var suffix = path[(slash + 1)..];
        if (!int.TryParse(suffix, out index))
        {
            return false;
        }

        scdPath = path[..slash];
        return true;
    }

    internal static bool TryParseUnknownPointer(string path, out nint pointer)
    {
        pointer = 0;
        const string prefix = "unknown/";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var hex = path[prefix.Length..];
        var slash = hex.IndexOf('/');
        if (slash >= 0)
        {
            hex = hex[..slash];
        }

        if (long.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var value))
        {
            pointer = (nint)value;
            return true;
        }

        return false;
    }

    private static string? LookupAlias(IReadOnlyDictionary<string, string> aliases, string path, nint scdDataPtr)
    {
        if (aliases.TryGetValue(path, out var alias))
        {
            return alias.ToLowerInvariant().Trim();
        }

        if (scdDataPtr != 0)
        {
            var pointerKey = $"unknown/{scdDataPtr:X}".ToLowerInvariant();
            if (aliases.TryGetValue(pointerKey, out alias))
            {
                return alias.ToLowerInvariant().Trim();
            }
        }

        return null;
    }
}
