namespace SoundMixer;

internal static class SoundMonitorHookIds
{
    internal const string ActiveScan = "ActiveScan";
    internal const string PathResolve = "PathResolve";

    internal static readonly string[] FilterOptions =
    [
        From(HookDebugId.PlaySpecificSound),
        From(HookDebugId.PlaySound),
        From(HookDebugId.PlaySystemSound),
        From(HookDebugId.PlayClipSound),
        From(HookDebugId.PlayMovieSound),
        From(HookDebugId.PlayBgmSound),
        From(HookDebugId.PlayWeatherSound),
        ActiveScan,
        PathResolve,
    ];

    internal static string From(HookDebugId hookId) => HookGuardIds.ToId(hookId);
}
