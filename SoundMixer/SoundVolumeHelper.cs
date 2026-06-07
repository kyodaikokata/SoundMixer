using System.Collections.Concurrent;
using FFXIVClientStructs.FFXIV.Client.Sound;

namespace SoundMixer;

internal readonly struct PlayContext
{
    internal string Path { get; init; }
    internal uint SoundNumber { get; init; }
    internal float Multiplier { get; init; }
}

internal static unsafe class SoundVolumeHelper
{
    private static readonly ConcurrentDictionary<string, float> PendingMultipliers = new();

    [ThreadStatic]
    private static PlayContext? s_playContext;

    [ThreadStatic]
    private static bool s_setVolumeCalledThisPlay;

    internal static void BeginPlay(string path, uint soundNumber, float multiplier)
    {
        s_setVolumeCalledThisPlay = false;

        if (multiplier is > 0.999f and < 1.001f)
        {
            return;
        }

        s_playContext = new PlayContext
        {
            Path = path,
            SoundNumber = soundNumber,
            Multiplier = multiplier,
        };

        PendingMultipliers[MakeKey(path, soundNumber)] = multiplier;
    }

    internal static void EndPlay()
    {
        if (s_playContext is { } ctx)
        {
            PendingMultipliers.TryRemove(MakeKey(ctx.Path, ctx.SoundNumber), out _);
        }

        s_playContext = null;
    }

    internal static void MarkSetVolumeCalled()
    {
        s_setVolumeCalledThisPlay = true;
    }

    internal static bool WasSetVolumeCalledThisPlay => s_setVolumeCalledThisPlay;

    internal static float GetMultiplier(VolumeCalculator calculator, string path, uint soundNumber)
    {
        path = path.ToLowerInvariant();
        return calculator.GetVolumeForSound($"{path}/{soundNumber}");
    }

    internal static float ResolveMultiplier(
        VolumeCalculator calculator,
        string path,
        uint soundNumber
    )
    {
        path = path.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(path))
        {
            var byPathAndNumber = GetMultiplier(calculator, path, soundNumber);
            if (Math.Abs(byPathAndNumber - 1.0f) > 0.001f)
            {
                return byPathAndNumber;
            }

            return calculator.GetVolumeForSound(path);
        }

        if (s_playContext is { } ctx)
        {
            if (!string.IsNullOrWhiteSpace(ctx.Path))
            {
                return calculator.GetVolumeForSound(ctx.Path);
            }

            if (ctx.SoundNumber == soundNumber)
            {
                return ctx.Multiplier;
            }
        }

        foreach (var (key, multiplier) in PendingMultipliers)
        {
            if (key.EndsWith($":{soundNumber}", StringComparison.Ordinal))
            {
                return multiplier;
            }
        }

        return 1.0f;
    }

    internal static float ScaleVolume(float volume, float multiplier)
    {
        if (multiplier <= 0.001f)
        {
            return 0f;
        }

        return volume * multiplier;
    }

    internal static unsafe string NormalizePath(byte* path)
    {
        return Util.ReadTerminatedString(path).ToLowerInvariant();
    }

    internal static string GetPathFromSoundData(SoundData* soundData)
    {
        if (!TryGetPathFromSoundData(soundData, out var path))
        {
            return string.Empty;
        }

        return path;
    }

    /// <summary>
    /// Reads the SCD path from SoundResourceHandle only. Does not call ISoundData.GetFileName()
    /// (native virtual); some mount / loop sounds crash when that vfunc runs on a live node.
    /// </summary>
    internal static bool TryGetPathFromSoundData(SoundData* soundData, out string path)
    {
        path = string.Empty;
        if (soundData == null || !SoundDataSafety.IsReadable((nint)soundData))
        {
            return false;
        }

        try
        {
            var handle = soundData->SoundResourceHandle;
            if (handle == null || !SoundDataSafety.IsReadable((nint)handle, 0x40))
            {
                return false;
            }

            var name = handle->FileName.ToString();
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            path = name.ToLowerInvariant();
            return true;
        }
        catch (Exception ex)
        {
            Services.PluginLog.Verbose(ex, "SoundMixer: failed to read path from SoundData");
            return false;
        }
    }

    private static string MakeKey(string path, uint soundNumber)
    {
        return $"{path.ToLowerInvariant()}:{soundNumber}";
    }
}
