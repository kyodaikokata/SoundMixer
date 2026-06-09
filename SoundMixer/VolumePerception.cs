using System;
using SoundMixer.Localization;
using static SoundMixer.Localization.Loc.Keys;

namespace SoundMixer;

internal static class VolumePerception
{
    internal static float ToDecibels(float linearMultiplier)
    {
        if (linearMultiplier <= 0.0001f)
        {
            return -80f;
        }

        return 20f * MathF.Log10(linearMultiplier);
    }

    internal static string FormatDecibels(float linearMultiplier)
    {
        if (linearMultiplier <= 0.0001f)
        {
            return "-80 dB";
        }

        return $"{ToDecibels(linearMultiplier):+#0.0;-#0.0;0} dB";
    }

    internal static string DescribeLinearGain(float linearMultiplier, float engineCap = Configuration.EngineAudibleCap)
    {
        if (linearMultiplier <= 0.001f)
        {
            return Loc.Get(VolumeSilent);
        }

        if (linearMultiplier <= Configuration.NormalMaxVolume + 0.001f)
        {
            return Loc.Get(VolumeRangeNormal);
        }

        if (linearMultiplier < engineCap - 0.01f)
        {
            return engineCap > Configuration.EngineAudibleCap + 0.01f
                ? Loc.Get(VolumeRangeDebugExtreme)
                : Loc.Get(VolumeRangeExpert);
        }

        return Loc.Get(VolumeAtCap);
    }

    internal static bool IsAtEngineCap(float linearMultiplier, float engineCap = Configuration.EngineAudibleCap)
    {
        return linearMultiplier >= engineCap - 0.01f;
    }
}
