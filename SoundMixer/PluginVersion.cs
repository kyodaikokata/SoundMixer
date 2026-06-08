using System.Reflection;

namespace SoundMixer;

internal static class PluginVersion
{
    internal static string Display { get; } =
        typeof(Plugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(Plugin).Assembly.GetName().Version?.ToString()
        ?? "?";
}
