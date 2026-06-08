using FFXIVClientStructs.FFXIV.Client.Sound;

namespace SoundMixer;

/// <summary>
/// Enforcement uses safe path resolution + GetVolumeForSound(specificPath), matching the monitor log when safe resolve succeeds.
/// </summary>
internal readonly struct ResolvedSoundEnforcement
{
    internal string ResolvedPath { get; init; }
    internal uint SoundNumber { get; init; }
    internal string SpecificPath { get; init; }
    internal float Multiplier { get; init; }
}

internal static unsafe class SoundEnforcement
{
    internal delegate bool ResolveDelegate(
        SoundData* soundData,
        VolumeCalculator calculator,
        out ResolvedSoundEnforcement result);

    private static ResolveDelegate? s_resolve;

    internal static void SetResolver(ResolveDelegate? resolve)
    {
        s_resolve = resolve;
    }

    internal static bool TryResolve(
        SoundData* soundData,
        VolumeCalculator calculator,
        out ResolvedSoundEnforcement result
    )
    {
        result = default;
        if (soundData == null || s_resolve == null)
        {
            return false;
        }

        return s_resolve(soundData, calculator, out result);
    }

    internal static float GetMultiplier(SoundData* soundData, VolumeCalculator calculator)
    {
        return TryResolve(soundData, calculator, out var resolved)
            ? resolved.Multiplier
            : 1.0f;
    }
}
