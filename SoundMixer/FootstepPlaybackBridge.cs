namespace SoundMixer;

/// <summary>
/// Footsteps: PlaySpecificSound uses setup indices (e.g. /27); PlaySound plays audible /1.
/// Without PlaySound hook we must scale the /1 node only, using the material path from PlaySpecificSound.
/// </summary>
internal static class FootstepPlaybackBridge
{
    internal const uint PlaybackSoundIndex = 1;

    internal static bool IsFootMaterialPath(string? path)
    {
        return !string.IsNullOrWhiteSpace(path)
               && path.Contains("/foot/foot/fs_", StringComparison.Ordinal);
    }

    internal static bool IsFootSetupPlay(string? path, int index)
    {
        return IsFootMaterialPath(path) && index != (int)PlaybackSoundIndex;
    }

    internal static bool IsFootPlaybackCandidate(string? nodePath, uint soundNumber, string materialPath)
    {
        if (soundNumber != PlaybackSoundIndex || !IsFootMaterialPath(materialPath))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(nodePath))
        {
            return true;
        }

        nodePath = nodePath.ToLowerInvariant();
        if (IsFootMaterialPath(nodePath))
        {
            return PathResolver.ShareScdFile(nodePath, materialPath);
        }

        return SoundVolumeHelper.IsFootContainerPath(nodePath);
    }
}
