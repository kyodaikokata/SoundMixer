using System;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SoundMixer.Localization;
using static SoundMixer.Localization.Loc.Keys;

namespace SoundMixer;

public class Plugin : IDalamudPlugin
{
    public string Name => "SoundMixer";

    internal Configuration Config { get; private set; }
    internal Filter Filter { get; private set; }
    internal VolumeCalculator VolumeCalculator { get; private set; }
    internal PluginUI UI { get; private set; }

    private const string CommandName = "/soundmixer";
    private const string CommandNameShort = "/smix";

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();

        Config = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Loc.Bind(Config);

        VolumeCalculator = new VolumeCalculator(Config);
        Filter = new Filter(this);
        
        if (Config.Enabled)
        {
            Filter.Enable();
        }

        UI = new PluginUI(this);

        var commandHelp = Loc.Get(CommandHelp);
        Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = commandHelp
        });

        Services.CommandManager.AddHandler(CommandNameShort, new CommandInfo(OnCommand)
        {
            HelpMessage = commandHelp
        });

        Services.PluginInterface.UiBuilder.Draw += UI.Draw;
        Services.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        Services.PluginInterface.UiBuilder.OpenMainUi += OpenConfigUi;
        Services.Framework.Update += OnFrameworkUpdate;

        ApplyDefaultConfiguration();
        MigrateClearGroupIcons();
        MigrateBuiltinSoundGroups();
        MigrateVolumeAboveEngineCap();
        PresetManager.Initialize(Config);
        DefaultGroupLocalization.Apply(Config);
    }

    private void MigrateVolumeAboveEngineCap()
    {
        var changed = false;

        foreach (var group in Config.Groups)
        {
            var clamped = Configuration.ClampToEngineCap(group.GroupVolume);
            if (Math.Abs(clamped - group.GroupVolume) > 0.001f)
            {
                group.GroupVolume = clamped;
                changed = true;
            }
        }

        foreach (var key in Config.IndividualVolumes.Keys.ToList())
        {
            var clamped = Configuration.ClampToEngineCap(Config.IndividualVolumes[key]);
            if (Math.Abs(clamped - Config.IndividualVolumes[key]) > 0.001f)
            {
                Config.IndividualVolumes[key] = clamped;
                changed = true;
            }
        }

        if (changed)
        {
            Config.Save();
            VolumeCalculator.ClearCache();
            Services.PluginLog.Info(
                $"SoundMixer: clamped saved volumes to engine audible cap ({Configuration.EngineAudibleCap * 100:F0}%)"
            );
        }
    }

    private void OnCommand(string command, string args)
    {
        UI.IsVisible = !UI.IsVisible;
    }

    private void OpenConfigUi()
    {
        UI.IsVisible = true;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        Filter.EnforceTrackedVolumes();
    }

    private void ApplyDefaultConfiguration()
    {
        if (Config.Groups.Count > 0 || Config.Presets.Count > 0)
        {
            return;
        }

        if (!DefaultConfigLoader.TryLoad(out var defaults))
        {
            return;
        }

        DefaultConfigLoader.ApplyGroupsAndPresets(Config, defaults);
        DefaultGroupLocalization.Apply(Config);
        Config.Save();
        Services.PluginLog.Info(
            $"SoundMixer: applied embedded default configuration ({Config.Groups.Count} groups)"
        );
    }

    private void MigrateClearGroupIcons()
    {
        var changed = false;
        foreach (var group in Config.Groups)
        {
            if (!string.IsNullOrEmpty(group.Icon))
            {
                group.Icon = string.Empty;
                changed = true;
            }
        }

        if (changed)
        {
            Config.Save();
        }
    }

    private void MigrateBuiltinSoundGroups()
    {
        if (Config.Version >= 4)
        {
            return;
        }

        var changed = false;

        changed |= RemoveDuplicateBgmBuiltinGroups();

        if (!HasGroupWithAnyPattern("**/bgm/**", "**/music/**"))
        {
            Config.Groups.Insert(
                Math.Min(1, Config.Groups.Count),
                new SoundGroup
                {
                    Name = Loc.Get(BuiltinBgm),
                    IsBuiltIn = true,
                    PathPatterns = new() { "**/music/**", "**/bgm/**" },
                }
            );
            changed = true;
        }

        changed |= EnsureBuiltInPatterns(
            g => g.PathPatterns.Contains("**/env/**") || g.PathPatterns.Contains("**/weather/**"),
            "**/ambient/**",
            "**/stream/**"
        );

        Config.Version = 4;
        Config.InvalidateGlobCache();
        Config.Save();
        VolumeCalculator.ClearCache();
        if (changed)
        {
            Services.PluginLog.Info("SoundMixer: migrated built-in BGM/environment groups");
        }
    }

    private bool HasGroupWithAnyPattern(params string[] patterns)
    {
        return Config.Groups.Any(
            g => patterns.Any(pattern => g.PathPatterns.Contains(pattern))
        );
    }

    private bool RemoveDuplicateBgmBuiltinGroups()
    {
        var duplicates = Config.Groups
            .Where(
                g => g.IsBuiltIn
                     && (g.PathPatterns.Contains("**/bgm/**") || g.PathPatterns.Contains("**/music/**"))
            )
            .Skip(1)
            .ToList();

        if (duplicates.Count == 0)
        {
            return false;
        }

        foreach (var group in duplicates)
        {
            RemoveGroupForMigration(group.Id);
        }

        return true;
    }

    private void RemoveGroupForMigration(string groupId)
    {
        var group = GroupHierarchy.FindById(Config, groupId);
        if (group == null)
        {
            return;
        }

        var parentId = group.ParentId;
        foreach (var child in GroupHierarchy.GetChildren(Config, groupId))
        {
            child.ParentId = parentId;
        }

        foreach (var path in group.SoundPaths)
        {
            Config.SoundToGroup.Remove(path);
        }

        foreach (var key in Config.SoundToGroup.Where(kv => kv.Value == groupId).Select(kv => kv.Key).ToList())
        {
            Config.SoundToGroup.Remove(key);
        }

        Config.Groups.Remove(group);
    }

    private bool EnsureBuiltInPatterns(Func<SoundGroup, bool> predicate, params string[] patterns)
    {
        var group = Config.Groups.Find(g => predicate(g));
        if (group == null)
        {
            return false;
        }

        var changed = false;
        foreach (var pattern in patterns)
        {
            if (!group.PathPatterns.Contains(pattern))
            {
                group.PathPatterns.Add(pattern);
                changed = true;
            }
        }

        return changed;
    }

    public void Dispose()
    {
        Services.Framework.Update -= OnFrameworkUpdate;

        if (UI != null)
        {
            Services.PluginInterface.UiBuilder.Draw -= UI.Draw;
            Services.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
            Services.PluginInterface.UiBuilder.OpenMainUi -= OpenConfigUi;
            UI.Dispose();
        }
        
        Filter?.Dispose();

        Services.CommandManager.RemoveHandler(CommandName);
        Services.CommandManager.RemoveHandler(CommandNameShort);
    }
}
