using System.Collections.Concurrent;

using FFXIVClientStructs.FFXIV.Client.Sound;



namespace SoundMixer;



internal sealed class TrackedSoundVolume

{

    internal string ScdPath { get; set; } = string.Empty;

    internal uint SoundNumber { get; set; }

    internal float LastGameVolume { get; set; } = 1.0f;

    internal float LastEffectiveVolume { get; set; }

    internal bool LastWriteByPlugin { get; set; }

}



internal static unsafe class SoundVolumeTracker

{

    private const int VolumeFadeTargetOffset = 0x64;



    private static readonly ConcurrentDictionary<nint, TrackedSoundVolume> Tracked = new();



    internal static float GetMultiplier(SoundData* soundData, VolumeCalculator calculator)

    {

        if (soundData == null)

        {

            return 1.0f;

        }



        var ptr = (nint)soundData;

        var path = SoundVolumeHelper.GetPathFromSoundData(soundData);

        var soundNumber = soundData->SoundNumber;



        if (!Tracked.TryGetValue(ptr, out var tracked))

        {

            var multiplier = SoundVolumeHelper.ResolveMultiplier(calculator, path, soundNumber);

            if (Math.Abs(multiplier - 1.0f) < 0.001f)

            {

                return 1.0f;

            }



            tracked = new TrackedSoundVolume

            {

                ScdPath = path,

                SoundNumber = soundNumber,

            };

            Tracked[ptr] = tracked;

        }



        if (!string.IsNullOrWhiteSpace(path))

        {

            tracked.ScdPath = path;

        }



        tracked.SoundNumber = soundNumber;

        return SoundVolumeHelper.ResolveMultiplier(

            calculator,

            tracked.ScdPath,

            tracked.SoundNumber

        );

    }



    internal static void Register(

        SoundData* soundData,

        VolumeCalculator calculator,

        float gameVolume,

        float effectiveVolume

    )

    {

        if (soundData == null)

        {

            return;

        }



        var multiplier = GetMultiplier(soundData, calculator);

        if (Math.Abs(multiplier - 1.0f) < 0.001f)

        {

            return;

        }



        var tracked = Tracked[(nint)soundData];

        tracked.LastGameVolume = gameVolume;

        tracked.LastEffectiveVolume = effectiveVolume;

        tracked.LastWriteByPlugin = true;

    }



    internal static float GetEffectiveVolume(SoundData* soundData, float fieldVolume, float multiplier)

    {

        if (Math.Abs(multiplier - 1.0f) < 0.001f)

        {

            return fieldVolume;

        }



        if (Tracked.TryGetValue((nint)soundData, out var tracked) && tracked.LastGameVolume > 0.001f)

        {

            return Configuration.ClampToEngineCap(
                SoundVolumeHelper.ScaleVolume(tracked.LastGameVolume, multiplier)
            );

        }



        return Configuration.ClampToEngineCap(
            SoundVolumeHelper.ScaleVolume(InferGameVolume(fieldVolume, multiplier), multiplier)
        );

    }



    internal static void ApplyFieldVolume(SoundData* soundData, float effectiveVolume)

    {

        var engineVolume = Configuration.ClampToEngineCap(effectiveVolume);

        soundData->Volume = engineVolume;

        WriteFadeTarget(soundData, engineVolume);

    }



    internal static void EnforceActiveSound(SoundData* soundData, VolumeCalculator calculator)

    {

        if (soundData == null || !soundData->IsActive)

        {

            return;

        }



        var path = SoundVolumeHelper.GetPathFromSoundData(soundData);

        var multiplier = SoundVolumeHelper.ResolveMultiplier(

            calculator,

            path,

            soundData->SoundNumber

        );

        if (Math.Abs(multiplier - 1.0f) < 0.001f)

        {

            return;

        }



        GetMultiplier(soundData, calculator);

        TryEnforceFieldVolume(soundData, calculator);

    }



    internal static bool TryEnforceFieldVolume(SoundData* soundData, VolumeCalculator calculator)

    {

        if (soundData == null || !soundData->IsActive)

        {

            return false;

        }



        var ptr = (nint)soundData;

        if (!Tracked.TryGetValue(ptr, out var tracked))

        {

            return false;

        }



        var multiplier = SoundVolumeHelper.ResolveMultiplier(

            calculator,

            tracked.ScdPath,

            tracked.SoundNumber

        );

        if (Math.Abs(multiplier - 1.0f) < 0.001f)

        {

            Tracked.TryRemove(ptr, out _);

            return false;

        }



        var gameVolume = tracked.LastGameVolume > 0.001f

            ? tracked.LastGameVolume

            : InferGameVolume(soundData->Volume, multiplier);

        var effective = Configuration.ClampToEngineCap(
            SoundVolumeHelper.ScaleVolume(gameVolume, multiplier)
        );



        tracked.LastGameVolume = gameVolume;

        tracked.LastEffectiveVolume = effective;

        tracked.LastWriteByPlugin = true;



        if (Math.Abs(soundData->Volume - effective) < 0.002f

            && Math.Abs(ReadFadeTarget(soundData) - effective) < 0.002f)

        {

            return true;

        }



        ApplyFieldVolume(soundData, effective);

        return true;

    }



    internal static void PruneInactive()

    {

        foreach (var (ptr, _) in Tracked)

        {

            var soundData = (SoundData*)ptr;

            if (soundData == null || !soundData->IsActive)

            {

                Tracked.TryRemove(ptr, out _);

            }

        }

    }



    internal static void Clear()

    {

        Tracked.Clear();

    }

    internal delegate void ApplyVolumeDelegate(SoundData* soundData, float effectiveVolume);

    internal static bool ForceRefreshActiveSound(
        SoundData* soundData,
        VolumeCalculator calculator,
        ApplyVolumeDelegate? applyVolume
    )
    {
        if (soundData == null || !soundData->IsActive)
        {
            return false;
        }

        var path = SoundVolumeHelper.GetPathFromSoundData(soundData);
        var soundNumber = soundData->SoundNumber;
        var multiplier = SoundVolumeHelper.ResolveMultiplier(calculator, path, soundNumber);
        if (Math.Abs(multiplier - 1.0f) < 0.001f && !string.IsNullOrWhiteSpace(path))
        {
            multiplier = calculator.GetVolumeForSound(path);
        }

        var ptr = (nint)soundData;
        var fieldVolume = soundData->Volume;
        float gameVolume;

        if (Tracked.TryGetValue(ptr, out var tracked) && tracked.LastGameVolume > 0.001f)
        {
            gameVolume = tracked.LastGameVolume;
            if (!string.IsNullOrWhiteSpace(path))
            {
                tracked.ScdPath = path;
            }

            tracked.SoundNumber = soundNumber;
        }
        else
        {
            gameVolume = InferGameVolume(fieldVolume, multiplier);
            Tracked[ptr] = new TrackedSoundVolume
            {
                ScdPath = path,
                SoundNumber = soundNumber,
            };
        }

        if (Math.Abs(multiplier - 1.0f) < 0.001f)
        {
            if (!Tracked.TryGetValue(ptr, out tracked) || !tracked.LastWriteByPlugin)
            {
                return false;
            }

            var restoreVolume = tracked.LastGameVolume > 0.001f ? tracked.LastGameVolume : fieldVolume;
            if (applyVolume != null)
            {
                applyVolume(soundData, restoreVolume);
            }
            else
            {
                ApplyFieldVolume(soundData, restoreVolume);
            }

            Tracked.TryRemove(ptr, out _);
            return true;
        }

        var effectiveVolume = Configuration.ClampToEngineCap(
            SoundVolumeHelper.ScaleVolume(gameVolume, multiplier)
        );
        tracked = Tracked[ptr];
        tracked.LastGameVolume = gameVolume;
        tracked.LastEffectiveVolume = effectiveVolume;
        tracked.LastWriteByPlugin = true;

        if (applyVolume != null)
        {
            applyVolume(soundData, effectiveVolume);
        }
        else
        {
            ApplyFieldVolume(soundData, effectiveVolume);
        }

        return true;
    }



    private static float InferGameVolume(float fieldVolume, float multiplier)

    {

        if (multiplier < 1.0f - 0.001f && fieldVolume <= multiplier + 0.02f)

        {

            return fieldVolume / multiplier;

        }



        if (multiplier > 1.0f + 0.001f && fieldVolume > multiplier + 0.02f)

        {

            return fieldVolume / multiplier;

        }



        return fieldVolume;

    }



    private static void WriteFadeTarget(SoundData* soundData, float effectiveVolume)

    {

        *(float*)((nint)soundData + VolumeFadeTargetOffset) = effectiveVolume;

    }



    private static float ReadFadeTarget(SoundData* soundData)

    {

        return *(float*)((nint)soundData + VolumeFadeTargetOffset);

    }

}


