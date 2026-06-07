using System;
using System.Numerics;
using System.Linq;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Utility;
using SoundMixer.Api;
using SoundMixer.Localization;
using static SoundMixer.Localization.Loc.Keys;

namespace SoundMixer;

public class PluginUI : IDisposable
{
    private Plugin Plugin { get; }
    private WindowSystem WindowSystem { get; }
    private MainWindow MainWindow { get; }

    public bool IsVisible
    {
        get => MainWindow.IsOpen;
        set => MainWindow.IsOpen = value;
    }

    public PluginUI(Plugin plugin)
    {
        Plugin = plugin;

        WindowSystem = new WindowSystem("SoundMixer");
        MainWindow = new MainWindow(plugin);
        WindowSystem.AddWindow(MainWindow);
    }

    public void Draw()
    {
        WindowSystem.Draw();
    }

    public void SaveWindowLayout() => MainWindow.PersistWindowLayout();

    public void Dispose()
    {
        MainWindow.PersistWindowLayout();
        WindowSystem.RemoveAllWindows();
    }
}

public partial class MainWindow : Window
{
    private const string KofiUrl = "https://ko-fi.com/kokatakyodai";

    private static string L(string key) => Loc.Get(key);

    private static string LF(string key, params object[] args) => Loc.Format(key, args);
    private Plugin Plugin { get; }
    private string _searchText = "";
    private string? _selectedGroupId;

    private string _newPatternInput = "";
    private string _newGroupName = "";
    private string? _newGroupParentId;
    private string _renameGroupInput = "";
    private string _individualVolumePath = "";
    private float _individualVolume = 1.0f;
    private bool _showNewGroupPopup;
    private bool _showPatternPopup;
    private bool _showRenamePopup;
    private bool _showIndividualVolumePopup;
    private bool _showPathAliasPopup;
    private bool _showEditPatternPopup;
    private bool _showEditSoundPathPopup;
    private string _pathAliasFrom = "";
    private string _pathAliasTo = "";
    private int _editingPatternIndex = -1;
    private string _editPatternInput = "";
    private string _editSoundPathInput = "";
    private string _editSoundPathOriginal = "";
    private bool _showVolumeEditPopup;
    private string _volumeEditText = "100";
    private float _volumeEditMax = 2.0f;
    private Action<float>? _volumeEditApply;
    private string? _statusMessage;
    private DateTime _statusMessageExpiry;
    private int _selectedPresetIndex;
    private string _newPresetName = "";
    private string _copyPresetName = "";
    private bool _showNewPresetPopup;
    private bool _showCopyPresetPopup;
    private bool _showDeletePresetPopup;
    private Vector2? _cachedWindowPos;
    private Vector2? _cachedWindowSize;

    private const float DefaultWindowWidth = 900f;
    private const float DefaultWindowHeight = 600f;
    private const float MinWindowWidth = 400f;
    private const float MinWindowHeight = 300f;
    private static readonly Vector4 OverrideVolumeColor = new(0.4f, 1f, 0.6f, 1f);

    public MainWindow(Plugin plugin) : base(
        "###SoundMixerMain",
        ImGuiWindowFlags.None)
    {
        Plugin = plugin;
        RestoreWindowLayout();
    }

    private void RestoreWindowLayout()
    {
        var cfg = Plugin.Config;
        if (cfg.MainWindowWidth is >= MinWindowWidth and var w
            && cfg.MainWindowHeight is >= MinWindowHeight and var h)
        {
            Size = new Vector2(w, h);
        }
        else
        {
            Size = new Vector2(DefaultWindowWidth, DefaultWindowHeight);
        }

        SizeCondition = ImGuiCond.Once;

        if (cfg.MainWindowX is float x && cfg.MainWindowY is float y)
        {
            Position = new Vector2(x, y);
            PositionCondition = ImGuiCond.Once;
        }
    }

    private void UpdateWindowLayoutCache()
    {
        _cachedWindowPos = ImGui.GetWindowPos();
        _cachedWindowSize = ImGui.GetWindowSize() / ImGuiHelpers.GlobalScale;
        PersistWindowLayout();
    }

    public void PersistWindowLayout()
    {
        if (_cachedWindowSize is not { X: >= MinWindowWidth, Y: >= MinWindowHeight } size
            || _cachedWindowPos is not { } pos)
        {
            return;
        }

        var cfg = Plugin.Config;
        if (cfg.MainWindowX == pos.X
            && cfg.MainWindowY == pos.Y
            && cfg.MainWindowWidth == size.X
            && cfg.MainWindowHeight == size.Y)
        {
            return;
        }

        cfg.MainWindowX = pos.X;
        cfg.MainWindowY = pos.Y;
        cfg.MainWindowWidth = size.X;
        cfg.MainWindowHeight = size.Y;
        cfg.Save();
    }

    public override void OnClose()
    {
        PersistWindowLayout();
    }

    public override void Draw()
    {
        Loc.Bind(Plugin.Config);
        WindowName = L(WindowTitle);

        if (ImGui.BeginTabBar("###SoundMixerTabBar"))
        {
            if (ImGui.BeginTabItem($"{L(TabMain)}###SoundMixerMainTab"))
            {
                DrawLanguageBar();
                ImGui.Separator();
                DrawToolbar();
                ImGui.Separator();

                if (Plugin.Config.ShowRecentSounds)
                {
                    DrawMonitoringPanel();
                    ImGui.Separator();
                }

                DrawTemporaryOverridesPanel();
                ImGui.Separator();

                DrawPresetBar();
                ImGui.Separator();

                DrawSearchBar();
                ImGui.Separator();

                DrawMainContent();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem($"{L(TabChangelog)}###SoundMixerChangelogTab"))
            {
                DrawChangelogTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        DrawPopups();
        UpdateWindowLayoutCache();
    }

    private void DrawLanguageBar()
    {
        ImGui.Text(L(LangLabel));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(140);
        var language = (int)Plugin.Config.UiLanguage;
        var labels = new[] { L(LangSystem), L(LangChinese), L(LangEnglish) };
        if (ImGui.Combo("##UiLanguage", ref language, labels, labels.Length))
        {
            Plugin.Config.UiLanguage = (LanguageMode)language;
            DefaultGroupLocalization.Apply(Plugin.Config);
            Plugin.Config.Save();
        }
    }

    private void DrawToolbar()
    {
        ImGui.Text(L(StatusLabel));
        ImGui.SameLine();

        if (Plugin.IsEffectivelyEnabled)
        {
            ImGui.TextColored(new Vector4(0, 1, 0, 1), L(StatusEnabled));
        }
        else
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), L(StatusDisabled));
        }

        if (Plugin.HasTemporaryOverrides)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1, 0.8f, 0.2f, 1), L(StatusIpcBadge));
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(L(StatusIpcTip));
            }
        }

        ImGui.SameLine();
        if (ImGui.Button(Plugin.Config.Enabled ? L(BtnDisable) : L(BtnEnable)))
        {
            Plugin.SetSavedEnabled(!Plugin.Config.Enabled);
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(!Plugin.IsEffectivelyEnabled);
        if (ImGui.Button(L(BtnRefreshAll)))
        {
            var count = Plugin.Filter.RefreshAllActiveSounds();
            SetStatusMessage(LF(MsgRefreshedAll, count));
        }

        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(L(BtnRefreshAllTip));
        }

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(20, 0));
        ImGui.SameLine();

        var expertMode = Plugin.Config.ExpertMode;
        if (ImGui.Checkbox(L(ExpertMode), ref expertMode))
        {
            Plugin.Config.ExpertMode = expertMode;
            if (!expertMode)
            {
                ClampAllVolumes(Plugin.Config.GetMaxVolume());
            }

            Plugin.Config.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(ExpertModeTip));
        }

        ImGui.SameLine();
        var monitoring = Plugin.Config.EnableMonitoring;
        if (ImGui.Checkbox(L(Monitoring), ref monitoring))
        {
            Plugin.Config.EnableMonitoring = monitoring;
            Plugin.Config.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(MonitoringTip));
        }

        ImGui.SameLine();
        ImGui.TextDisabled(LF(TrackedScd, Plugin.Filter.TrackedScdCount));

        if (Plugin.Config.Enabled)
        {
            var hookLine = LF(
                HookStatusLine,
                Loc.HookState(Plugin.Filter.SetVolumeHookActive),
                Loc.HookState(Plugin.Filter.GetVolumeHookActive),
                Loc.HookState(Plugin.Filter.PlayBgmSoundHookActive),
                Loc.HookState(Plugin.Filter.PlayWeatherSoundHookActive)
            );
            ImGui.TextDisabled(hookLine);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(L(HookStatusTip));
            }
        }
    }

    private void DrawMonitoringPanel()
    {
        ImGui.SetNextItemOpen(Plugin.Config.RecentSoundsPanelExpanded, ImGuiCond.Once);
        var expanded = ImGui.CollapsingHeader(L(MonitorTitle));
        if (expanded != Plugin.Config.RecentSoundsPanelExpanded)
        {
            Plugin.Config.RecentSoundsPanelExpanded = expanded;
            Plugin.Config.Save();
        }

        if (!expanded)
        {
            return;
        }

        var hideMatched = Plugin.Config.HideMatchedMonitoringLogs;
        if (ImGui.Checkbox(L(MonitorHideMatched), ref hideMatched))
        {
            Plugin.Config.HideMatchedMonitoringLogs = hideMatched;
            Plugin.Config.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(MonitorHideMatchedTip));
        }

        ImGui.SameLine();
        ImGui.Text(L(MonitorHideKeywords));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(280);
        var hideKeywords = Plugin.Config.MonitoringHideKeywords;
        if (ImGui.InputText("##MonitoringHideKeywords", ref hideKeywords, 256))
        {
            Plugin.Config.MonitoringHideKeywords = hideKeywords;
            Plugin.Config.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(MonitorHideKeywordsTip));
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(string.IsNullOrWhiteSpace(hideKeywords));
        if (ImGui.Button($"{L(BtnClearFilter)}##ClearMonitorFilter"))
        {
            Plugin.Config.MonitoringHideKeywords = "";
            Plugin.Config.Save();
        }

        ImGui.EndDisabled();

        ImGui.TextDisabled(
            LF(
                MonitorHint,
                Configuration.MonitoringHistorySize,
                Configuration.MonitoringDisplayCount,
                Loc.PlayingTag()
            )
        );

        if (_statusMessage != null && DateTime.Now <= _statusMessageExpiry)
        {
            ImGui.TextColored(new Vector4(0.4f, 1f, 0.6f, 1f), _statusMessage);
        }

        if (ImGui.BeginChild("RecentSounds", new Vector2(0, 130), true))
        {
            var visibleCount = 0;
            foreach (var sound in Plugin.Filter.RecentSounds.Reverse())
            {
                if (!MatchesSearch(sound.FullPath) && !MatchesSearch(sound.Category))
                {
                    continue;
                }

                if (!PassesMonitoringFilters(sound))
                {
                    continue;
                }

                if (visibleCount >= Configuration.MonitoringDisplayCount)
                {
                    break;
                }

                visibleCount++;
                var volumePct = (int)(sound.Volume * 100);
                var volumeDb = VolumePerception.FormatDecibels(sound.Volume);
                var label = $"{sound.LastPlayed:HH:mm:ss}  [{volumePct}% {volumeDb}]  {sound.Category}  {sound.FullPath}";
                var groupId = Plugin.VolumeCalculator.GetMatchedGroupId(sound.FullPath);
                var labelColor = GroupColorHelper.TryGetDisplayColorForGroupId(Plugin.Config, groupId, out var color)
                    ? color
                    : (Vector4?)null;

                ImGui.PushID(sound.FullPath);
                if (labelColor.HasValue)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor.Value);
                }

                ImGui.Selectable(label);

                if (labelColor.HasValue)
                {
                    ImGui.PopStyleColor();
                }
                if (ImGui.BeginPopupContextItem("###SoundContext"))
                {
                    DrawSoundContextMenu(sound.FullPath);
                    ImGui.EndPopup();
                }

                ImGui.PopID();
            }

            if (visibleCount == 0)
            {
                ImGui.TextDisabled(L(MonitorEmpty));
            }
        }
        ImGui.EndChild();

        DrawPathAliasesSection();
    }

    private void DrawTemporaryOverridesPanel()
    {
        var summaries = Plugin.Api.GetTemporaryOverrideSummaries();
        var header = summaries.Count > 0
            ? LF(IpcOverridesTitleCount, summaries.Count)
            : L(IpcOverridesTitle);

        ImGui.SetNextItemOpen(Plugin.Config.IpcOverridesPanelExpanded, ImGuiCond.Once);
        var expanded = ImGui.CollapsingHeader(header);
        if (expanded != Plugin.Config.IpcOverridesPanelExpanded)
        {
            Plugin.Config.IpcOverridesPanelExpanded = expanded;
            Plugin.Config.Save();
        }

        if (!expanded)
        {
            return;
        }

        ImGui.BeginDisabled(!Plugin.HasTemporaryOverrides);
        if (ImGui.Button(L(IpcOverridesClearAll)))
        {
            Plugin.Api.RemoveAllTemporaryOverrides();
            SetStatusMessage(L(MsgIpcOverridesClearedAll));
        }

        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(L(IpcOverridesClearAllTip));
        }

        if (ImGui.BeginChild("###IpcOverridesList", new Vector2(0, 110), true))
        {
            if (summaries.Count == 0)
            {
                ImGui.TextDisabled(L(IpcOverridesEmpty));
            }
            else
            {
                foreach (var summary in summaries)
                {
                    ImGui.PushID(summary.Tag);
                    if (ImGui.Button("##IpcOverrideRow", new Vector2(-1, 0)))
                    {
                        Plugin.Api.RemoveTemporaryOverrides(summary.Tag);
                        SetStatusMessage(LF(MsgIpcOverridesClearedTag, summary.Tag));
                    }

                    DrawIpcOverrideButtonLabel(summary);

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(L(IpcOverridesClearTagTip));
                    }

                    ImGui.PopID();
                }
            }
        }

        ImGui.EndChild();
    }

    private void DrawIpcOverrideButtonLabel(IpcOverrideSummary summary)
    {
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();
        var textY = min.Y + (max.Y - min.Y - ImGui.GetTextLineHeight()) * 0.5f;
        var x = min.X + ImGui.GetStyle().FramePadding.X;

        var normalColor = ImGui.GetColorU32(ImGuiCol.Text);
        var overrideColor = GetOverrideVolumeColorU32();

        void DrawPart(string text, uint color)
        {
            if (text.Length == 0)
            {
                return;
            }

            drawList.AddText(new Vector2(x, textY), color, text);
            x += ImGui.CalcTextSize(text).X;
        }

        DrawPart(summary.Tag, normalColor);
        if (summary.Priority != 0)
        {
            DrawPart(" · ", normalColor);
            DrawPart(Loc.Format(IpcOverridePriority, summary.Priority), normalColor);
        }

        foreach (var line in summary.DetailLines)
        {
            DrawPart(" · ", normalColor);
            DrawPart(line, normalColor);
        }

        foreach (var groupVolume in summary.GroupVolumeLines)
        {
            DrawPart(" · ", normalColor);
            DrawPart(groupVolume.GroupName, normalColor);
            DrawPart(LF(GroupTreeOverrideVolume, groupVolume.EffectivePercent), overrideColor);
        }
    }

    private static uint GetOverrideVolumeColorU32() => ImGui.GetColorU32(OverrideVolumeColor);

    private static void DrawNameWithVolumeSuffix(
        string name,
        string volumeSuffix,
        bool suffixGreen,
        Vector4? nameColor
    )
    {
        var min = ImGui.GetItemRectMin();
        var size = ImGui.GetItemRectSize();
        var drawList = ImGui.GetWindowDrawList();
        var textY = min.Y + (size.Y - ImGui.GetTextLineHeight()) * 0.5f;
        var x = min.X + ImGui.GetStyle().FramePadding.X;

        var nameColorU32 = nameColor.HasValue
            ? ImGui.GetColorU32(nameColor.Value)
            : ImGui.GetColorU32(ImGuiCol.Text);
        var suffixColorU32 = suffixGreen ? GetOverrideVolumeColorU32() : nameColorU32;

        drawList.AddText(new Vector2(x, textY), nameColorU32, name);
        x += ImGui.CalcTextSize(name).X;
        drawList.AddText(new Vector2(x, textY), suffixColorU32, volumeSuffix);
    }

    private bool PassesMonitoringFilters(SoundInfo sound)
    {
        if (Plugin.Config.HideMatchedMonitoringLogs
            && Plugin.VolumeCalculator.HasMatchingRule(sound.FullPath))
        {
            return false;
        }

        if (MatchesMonitoringHideKeywords(sound))
        {
            return false;
        }

        if (Plugin.VolumeCalculator.IsHiddenFromMonitorByGroup(sound.FullPath))
        {
            return false;
        }

        return true;
    }

    private bool MatchesMonitoringHideKeywords(SoundInfo sound)
    {
        var keywords = Plugin.Config.MonitoringHideKeywords;
        if (string.IsNullOrWhiteSpace(keywords))
        {
            return false;
        }

        var haystack = $"{sound.FullPath}\n{sound.Path}\n{sound.Category}";
        foreach (var part in keywords.Split(','))
        {
            var keyword = part.Trim();
            if (keyword.Length == 0)
            {
                continue;
            }

            if (haystack.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void DrawSoundContextMenu(string soundPath)
    {
        ImGui.TextDisabled(soundPath);
        ImGui.Separator();

        if (Plugin.Config.Groups.Count == 0)
        {
            ImGui.TextDisabled(L(CtxNeedGroup));
        }
        else if (ImGui.BeginMenu(L(CtxAddToGroup)))
        {
            DrawHierarchicalGroupMenu(group =>
            {
                AddSoundToGroup(soundPath, group);
                _selectedGroupId = group.Id;
            });
            ImGui.EndMenu();
        }

        if (Plugin.Config.Groups.Count > 0 && ImGui.BeginMenu(L(CtxAddPattern)))
        {
            var scdPath = GetScdPath(NormalizeSoundPath(soundPath));
            var pattern = $"**/{scdPath}**";
            DrawHierarchicalGroupMenu(group =>
            {
                AddPatternToGroup(pattern, group);
                _selectedGroupId = group.Id;
                SetStatusMessage(LF(MsgAddedPattern, pattern, group.Name));
            });
            ImGui.EndMenu();
        }

        if (ImGui.MenuItem(L(CtxIndividualVol)))
        {
            var normalizedPath = NormalizeSoundPath(soundPath);
            _individualVolumePath = normalizedPath;
            _individualVolume = Plugin.Config.IndividualVolumes.GetValueOrDefault(
                normalizedPath,
                1.0f
            );
            _showIndividualVolumePopup = true;
        }

        if (ImGui.MenuItem(L(CtxFixPath)))
        {
            _pathAliasFrom = GetScdPath(NormalizeSoundPath(soundPath));
            _pathAliasTo = Plugin.Config.PathAliases.GetValueOrDefault(_pathAliasFrom, "");
            _showPathAliasPopup = true;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(CtxFixPathTip));
        }

        if (ImGui.MenuItem(L(CtxCopyPath)))
        {
            ImGui.SetClipboardText(soundPath);
        }
    }

    private void DrawPresetBar()
    {
        ImGui.Text(L(PresetLabel));
        ImGui.SameLine();

        var presets = Plugin.Config.Presets;
        if (presets.Count == 0)
        {
            ImGui.TextDisabled(L(PresetEmpty));
            return;
        }

        SyncPresetComboIndex();
        var presetLabels = presets.Select(p => p.Name).ToArray();
        if (ImGui.Combo("##PresetCombo", ref _selectedPresetIndex, presetLabels, presetLabels.Length))
        {
            var selected = presets[_selectedPresetIndex];
            if (selected.Id != Plugin.Config.ActivePresetId)
            {
                PresetManager.SwitchPreset(Plugin.Config, selected.Id, Plugin.Filter);
                _selectedGroupId = null;
                Plugin.VolumeCalculator.ClearCache();
                if (Plugin.Config.Enabled)
                {
                    Plugin.Filter.RefreshAllActiveSounds();
                }

                SetStatusMessage(LF(MsgSwitchedPreset, selected.Name));
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(PresetTip));
        }

        ImGui.SameLine();
        if (ImGui.Button(L(PresetNew)))
        {
            _newPresetName = GetSuggestedPresetName(L(PresetNewName));
            _showNewPresetPopup = true;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(PresetNewTip));
        }

        ImGui.SameLine();
        if (ImGui.Button(L(PresetCopy)))
        {
            var active = PresetManager.FindPreset(Plugin.Config, Plugin.Config.ActivePresetId);
            _copyPresetName = GetSuggestedPresetName($"{active?.Name ?? L(PresetLabel)}{L(PresetCopySuffix)}");
            _showCopyPresetPopup = true;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(PresetCopyTip));
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(!PresetManager.CanDeletePreset(Plugin.Config));
        if (ImGui.Button(L(PresetDelete)))
        {
            _showDeletePresetPopup = true;
        }

        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(
                PresetManager.CanDeletePreset(Plugin.Config)
                    ? L(PresetDeleteTip)
                    : L(PresetDeleteDisabledTip)
            );
        }
    }

    private void SyncPresetComboIndex()
    {
        var presets = Plugin.Config.Presets;
        var activeIndex = presets.FindIndex(p => p.Id == Plugin.Config.ActivePresetId);
        if (activeIndex >= 0)
        {
            _selectedPresetIndex = activeIndex;
        }
    }

    private string GetSuggestedPresetName(string baseName)
    {
        var name = baseName;
        var suffix = 1;
        while (!PresetManager.IsNameAvailable(Plugin.Config, name))
        {
            name = $"{baseName} {suffix}";
            suffix++;
        }

        return name;
    }

    private void DrawSearchBar()
    {
        ImGui.Text(L(SearchLabel));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(300);
        ImGui.InputText("##Search", ref _searchText, 256);
        ImGui.SameLine();
        ImGui.BeginDisabled(string.IsNullOrWhiteSpace(_searchText));
        if (ImGui.Button($"{L(BtnClearSearch)}##ClearSearch"))
        {
            _searchText = "";
        }

        ImGui.EndDisabled();
        ImGui.TextDisabled(L(SearchHint));
    }

    private void DrawMainContent()
    {
        var leftPanelWidth = 260f;

        if (ImGui.BeginChild("LeftPanel", new Vector2(leftPanelWidth, 0), true))
        {
            DrawGroupTree();
        }
        ImGui.EndChild();

        ImGui.SameLine();

        if (ImGui.BeginChild("RightPanel", new Vector2(0, 0), true))
        {
            DrawGroupDetails();
        }
        ImGui.EndChild();
    }

    private void DrawGroupTree()
    {
        ImGui.Text(L(GroupTitle));
        ImGui.TextDisabled(L(GroupDragHint));

        DrawRootDropZone();

        if (ImGui.Button(L(GroupNewRoot), new Vector2(-1, 0)))
        {
            OpenNewGroupPopup(null);
        }

        ImGui.Spacing();

        foreach (var group in GroupHierarchy.GetRoots(Plugin.Config))
        {
            if (!GroupMatchesSearch(group))
            {
                continue;
            }

            DrawGroupTreeNode(group, 0);
        }
    }

    private void DrawRootDropZone()
    {
        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.2f, 0.35f, 0.2f, 0.35f));
        ImGui.Selectable(L(GroupDropRoot), false);
        if (ImGui.BeginDragDropTarget())
        {
            var draggedId = GroupHierarchy.ReadDragPayload();
            if (draggedId != null
                && GroupHierarchy.ApplyDrop(Plugin.Config, draggedId, null, GroupDropIntent.ToRoot))
            {
                SaveGroupChanges();
                SetStatusMessage(L(MsgMovedRoot));
            }

            ImGui.EndDragDropTarget();
        }

        ImGui.PopStyleColor();
    }

    private void DrawGroupTreeNode(SoundGroup group, int depth)
    {
        var children = GroupHierarchy.GetChildren(Plugin.Config, group.Id);
        var hasChildren = children.Count > 0;
        if (GroupMatchesSearch(group) && !string.IsNullOrWhiteSpace(_searchText))
        {
            group.IsExpanded = true;
        }

        ImGui.PushID(group.Id);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + depth * 14f);

        if (hasChildren)
        {
            if (ImGui.ArrowButton("expand", group.IsExpanded ? ImGuiDir.Down : ImGuiDir.Right))
            {
                group.IsExpanded = !group.IsExpanded;
                SaveGroupChanges();
            }

            ImGui.SameLine();
        }
        else
        {
            ImGui.Dummy(new Vector2(20, 0));
            ImGui.SameLine();
        }

        var effectivePct = (int)(Plugin.Api.GetEffectiveGroupVolume(group.Id) * 100);
        var hasOverride = Plugin.Api.HasTemporaryGroupVolume(group.Id);
        var volumeSuffix = LF(
            hasOverride ? GroupTreeOverrideVolume : GroupTreeEffectiveVolume,
            effectivePct
        );
        var isSelected = _selectedGroupId == group.Id;
        var labelColor = GroupColorHelper.TryGetDisplayColor(Plugin.Config, group, out var color)
            ? color
            : (Vector4?)null;

        if (ImGui.Selectable("##groupRow", isSelected))
        {
            _selectedGroupId = group.Id;
        }

        DrawNameWithVolumeSuffix(group.Name, volumeSuffix, hasOverride, labelColor);

        if (ImGui.BeginPopupContextItem($"groupctx##{group.Id}"))
        {
            if (ImGui.MenuItem(L(GroupCtxNewChild)))
            {
                OpenNewGroupPopup(group.Id);
            }

            if (!string.IsNullOrWhiteSpace(group.ParentId) && ImGui.MenuItem(L(GroupCtxRemoveParent)))
            {
                GroupHierarchy.RemoveFromParent(Plugin.Config, group.Id);
                SaveGroupChanges();
                SetStatusMessage(LF(MsgRemovedParent, group.Name));
            }

            if (ImGui.MenuItem(L(GroupCtxDelete)))
            {
                if (!PresetManager.CanDeleteGroup(Plugin.Config))
                {
                    SetStatusMessage(L(GroupKeepOne));
                }
                else
                {
                    GroupHierarchy.DeleteGroup(Plugin.Config, group.Id);
                    if (_selectedGroupId == group.Id)
                    {
                        _selectedGroupId = null;
                    }

                    SaveGroupChanges();
                }
            }

            ImGui.EndPopup();
        }

        GroupHierarchy.BeginDragSource(group.Id, group.Name);

        if (ImGui.BeginDragDropTarget())
        {
            var draggedId = GroupHierarchy.ReadDragPayload();
            if (draggedId != null)
            {
                var intent = GroupHierarchy.GetDropIntentForItem();
                if (GroupHierarchy.ApplyDrop(Plugin.Config, draggedId, group.Id, intent))
                {
                    SaveGroupChanges();
                    var message = intent switch
                    {
                        GroupDropIntent.Into => LF(MsgMovedInto, group.Name),
                        GroupDropIntent.Before => L(MsgReordered),
                        GroupDropIntent.After => L(MsgReordered),
                        _ => L(MsgReordered),
                    };
                    SetStatusMessage(message);
                }
            }

            ImGui.EndDragDropTarget();
        }

        ImGui.PopID();

        if (!hasChildren || !group.IsExpanded)
        {
            return;
        }

        foreach (var child in children)
        {
            if (!GroupMatchesSearch(child))
            {
                continue;
            }

            DrawGroupTreeNode(child, depth + 1);
        }
    }

    private void DrawGroupDetails()
    {
        var group = GetSelectedGroup();
        if (group == null)
        {
            ImGui.TextDisabled(L(GroupSelectHint));
            return;
        }

        DrawGroupTitle(group);
        ImGui.SameLine();
        if (ImGui.Button(L(GroupRename)))
        {
            _renameGroupInput = group.Name;
            _showRenamePopup = true;
        }

        ImGui.SameLine();
        if (ImGui.Button(L(GroupDelete)))
        {
            if (!PresetManager.CanDeleteGroup(Plugin.Config))
            {
                SetStatusMessage(L(GroupKeepOne));
            }
            else
            {
                GroupHierarchy.DeleteGroup(Plugin.Config, group.Id);
                _selectedGroupId = null;
                SaveGroupChanges();
                return;
            }
        }

        ImGui.SameLine();
        if (ImGui.Button(L(GroupNewChild)))
        {
            OpenNewGroupPopup(group.Id);
        }

        var parentName = GroupHierarchy.GetParentName(Plugin.Config, group);
        if (!string.IsNullOrWhiteSpace(parentName))
        {
            ImGui.TextDisabled(LF(GroupParent, parentName));
            if (ImGui.Button(L(GroupRemoveParent)))
            {
                GroupHierarchy.RemoveFromParent(Plugin.Config, group.Id);
                SaveGroupChanges();
                SetStatusMessage(LF(MsgRemovedParent, group.Name));
            }

            DrawGroupOverrideColorPicker(group);
        }
        else
        {
            DrawGroupColorPicker(group);
        }

        var hideFromMonitor = group.HideFromMonitorLog;
        if (ImGui.Checkbox(L(GroupHideFromMonitor), ref hideFromMonitor))
        {
            group.HideFromMonitorLog = hideFromMonitor;
            SaveGroupChanges();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(GroupHideFromMonitorTip));
        }

        ImGui.Separator();
        DrawVolumeSlider(group);

        ImGui.Spacing();
        ImGui.Text(L(GroupPatterns));
        ImGui.TextDisabled(L(GroupPatternsHint));

        if (ImGui.BeginChild("Patterns", new Vector2(0, 120), true))
        {
            for (var i = 0; i < group.PathPatterns.Count; i++)
            {
                var pattern = group.PathPatterns[i];
                ImGui.Text($"- {pattern}");
                ImGui.SameLine();
                if (ImGui.SmallButton($"{L(BtnEdit)}##pattern{i}"))
                {
                    _editingPatternIndex = i;
                    _editPatternInput = pattern;
                    _showEditPatternPopup = true;
                }

                ImGui.SameLine();
                if (ImGui.SmallButton($"{L(BtnDelete)}##pattern{i}"))
                {
                    group.PathPatterns.RemoveAt(i);
                    Plugin.Config.InvalidateGlobCache();
                    Plugin.Config.Save();
                    Plugin.VolumeCalculator.ClearCache();
                    break;
                }
            }

            if (group.PathPatterns.Count == 0)
            {
                ImGui.TextDisabled(L(GroupPatternsEmpty));
            }
        }
        ImGui.EndChild();

        if (ImGui.Button(L(GroupAddPattern)))
        {
            _newPatternInput = "";
            _showPatternPopup = true;
        }

        ImGui.Spacing();
        ImGui.Text(L(GroupSounds));

        if (ImGui.BeginChild("Sounds", new Vector2(0, 0), true))
        {
            var sounds = group.SoundPaths.ToList();
            foreach (var soundPath in sounds)
            {
                var vol = Plugin.Config.IndividualVolumes.GetValueOrDefault(soundPath, group.GroupVolume);
                ImGui.Text($"{soundPath}  [{(int)(vol * 100)}%]");
                ImGui.SameLine();
                if (ImGui.SmallButton($"{L(BtnEdit)}##sound{soundPath.GetHashCode()}"))
                {
                    _editSoundPathOriginal = soundPath;
                    _editSoundPathInput = soundPath;
                    _showEditSoundPathPopup = true;
                }

                ImGui.SameLine();
                if (ImGui.SmallButton($"{L(BtnRemove)}##{soundPath}"))
                {
                    group.SoundPaths.Remove(soundPath);
                    Plugin.Config.SoundToGroup.Remove(soundPath);
                    Plugin.Config.Save();
                    Plugin.VolumeCalculator.ClearCache();
                }
            }

            if (group.SoundPaths.Count == 0)
            {
                ImGui.TextDisabled(L(GroupSoundsEmpty));
            }
        }
        ImGui.EndChild();
    }

    private void DrawPathAliasesSection()
    {
        if (Plugin.Config.PathAliases.Count == 0)
        {
            return;
        }

        if (ImGui.CollapsingHeader(LF(PathAliasHeader, Plugin.Config.PathAliases.Count)))
        {
            ImGui.TextDisabled(L(PathAliasHint));

            foreach (var entry in Plugin.Config.PathAliases.ToList())
            {
                ImGui.Text($"{entry.Key}  ->  {entry.Value}");
                ImGui.SameLine();
                if (ImGui.SmallButton($"{L(BtnEdit)}##alias{entry.Key.GetHashCode()}"))
                {
                    _pathAliasFrom = entry.Key;
                    _pathAliasTo = entry.Value;
                    _showPathAliasPopup = true;
                }

                ImGui.SameLine();
                if (ImGui.SmallButton($"{L(BtnDelete)}##aliasdel{entry.Key.GetHashCode()}"))
                {
                    Plugin.Filter.RemovePathAlias(entry.Key);
                }
            }
        }
    }

    private void DrawGroupTitle(SoundGroup group)
    {
        if (GroupColorHelper.TryGetDisplayColor(Plugin.Config, group, out var titleColor))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, titleColor);
            ImGui.Text(group.Name);
            ImGui.PopStyleColor();
            return;
        }

        ImGui.Text(group.Name);
    }

    private void DrawGroupColorPicker(SoundGroup group)
    {
        ImGui.Text(L(GroupColor));
        ImGui.SameLine();

        var color = GroupColorHelper.GetPickerColor(group.LabelColorArgb);
        if (ImGui.ColorEdit4(
                $"##GroupColor{group.Id}",
                ref color,
                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
        {
            group.LabelColorArgb = GroupColorHelper.FromVector4(color);
            SaveGroupChanges();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(GroupColorTip));
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(group.LabelColorArgb == 0);
        if (ImGui.Button($"{L(BtnReset)}##ResetGroupColor{group.Id}"))
        {
            group.LabelColorArgb = 0;
            SaveGroupChanges();
        }

        ImGui.EndDisabled();
    }

    private void DrawGroupOverrideColorPicker(SoundGroup group)
    {
        ImGui.Text(L(GroupOverrideColor));
        ImGui.SameLine();

        var color = GroupColorHelper.GetPickerColor(group.OverrideColorArgb);
        if (ImGui.ColorEdit4(
                $"##GroupOverrideColor{group.Id}",
                ref color,
                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
        {
            group.OverrideColorArgb = GroupColorHelper.FromVector4(color);
            SaveGroupChanges();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(GroupOverrideColorTip));
        }

        ImGui.SameLine();
        if (ImGui.Button($"{L(GroupSyncColor)}##SyncGroupOverrideColor{group.Id}"))
        {
            GroupColorHelper.SyncOverrideColorFromParent(Plugin.Config, group);
            SaveGroupChanges();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(GroupSyncColorTip));
        }
    }

    private void DrawVolumeSlider(SoundGroup group)
    {
        ImGui.Text(L(GroupVolume));
        ImGui.SameLine();
        ImGui.BeginDisabled(!Plugin.Config.Enabled);
        if (ImGui.Button($"{L(GroupRefresh)}##refresh{group.Id}"))
        {
            var count = Plugin.Filter.RefreshGroupSounds(group.Id);
            SetStatusMessage(LF(MsgRefreshedGroup, group.Name, count));
        }

        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(L(GroupRefreshTip));
        }

        var maxVolume = Plugin.Config.GetMaxVolume();
        var volume = Configuration.ClampToUiRange(group.GroupVolume, maxVolume);
        if (Math.Abs(volume - group.GroupVolume) > 0.001f)
        {
            group.GroupVolume = volume;
            Plugin.Config.Save();
            Plugin.VolumeCalculator.ClearCache();
        }

        var maxPct = (int)(maxVolume * 100);

        DrawEditableVolumeSlider(
            "GroupVolume",
            volume,
            maxVolume,
            LF(GroupVolumeSliderTip, maxPct),
            newVolume =>
            {
                group.GroupVolume = newVolume;
                Plugin.Config.Save();
                Plugin.VolumeCalculator.ClearCache();
                if (Plugin.Config.Enabled)
                {
                    Plugin.Filter.RefreshGroupSounds(group.Id);
                }
            }
        );
    }

    private void DrawEditableVolumeSlider(
        string id,
        float volume,
        float maxVolume,
        string tooltip,
        Action<float> setVolume
    )
    {
        var value = volume;
        ImGui.SetNextItemWidth(300);
        if (ImGui.SliderFloat($"##{id}", ref value, 0.0f, maxVolume))
        {
            setVolume(value);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                OpenVolumeEditPopup(value, maxVolume, setVolume);
            }
        }

        ImGui.SameLine();
        ImGui.Text($"{value * 100:F0}%  ({VolumePerception.FormatDecibels(value)})");

        if (VolumePerception.IsAtEngineCap(value))
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1, 0, 0, 1), L(VolumeMaxBadge));
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(VolumePerception.DescribeLinearGain(value));
            }
        }
        else if (value > 2.0f)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.6f, 0.8f, 1f, 1f), L(VolumeApproxBadge));
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(
                    $"{VolumePerception.DescribeLinearGain(value)}\n"
                        + LF(VolumeLinearTip, $"{value * 100:F0}", VolumePerception.FormatDecibels(value))
                );
            }
        }
        else if (value > 1.0f)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1, 1, 0, 1), L(VolumeBoostBadge));
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(LF(VolumeAbove100Tip, VolumePerception.FormatDecibels(value)));
            }
        }
    }

    private void OpenVolumeEditPopup(float currentVolume, float maxVolume, Action<float> apply)
    {
        _volumeEditText = (currentVolume * 100f).ToString("F1");
        _volumeEditMax = maxVolume;
        _volumeEditApply = apply;
        _showVolumeEditPopup = true;
    }

    private void DrawPopups()
    {
        DrawVolumeEditPopup();
        DrawNewGroupPopup();
        DrawPatternPopup();
        DrawRenamePopup();
        DrawIndividualVolumePopup();
        DrawPathAliasPopup();
        DrawEditPatternPopup();
        DrawEditSoundPathPopup();
        DrawNewPresetPopup();
        DrawCopyPresetPopup();
        DrawDeletePresetPopup();
    }

    private void DrawVolumeEditPopup()
    {
        if (_showVolumeEditPopup && !ImGui.IsPopupOpen("###SoundMixerVolumeEditPopup"))
        {
            ImGui.OpenPopup("###SoundMixerVolumeEditPopup");
        }

        var popupOpen = _showVolumeEditPopup;
        var maxPct = (int)(_volumeEditMax * 100);
        if (ImGui.BeginPopupModal($"{L(PopupVolumeInput)}###SoundMixerVolumeEditPopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text(LF(PopupVolumePct, maxPct));
            ImGui.SetNextItemWidth(120);
            ImGui.InputText("##VolumePct", ref _volumeEditText, 16);
            ImGui.TextDisabled(L(PopupVolumePctHint));

            if (ImGui.Button(L(BtnConfirm), new Vector2(120, 0)))
            {
                if (TryParseVolumePercent(_volumeEditText, _volumeEditMax, out var volume))
                {
                    _volumeEditApply?.Invoke(volume);
                }

                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button(L(BtnCancel), new Vector2(120, 0)))
            {
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        _showVolumeEditPopup = popupOpen;
    }

    private static bool TryParseVolumePercent(string text, float maxVolume, out float volume)
    {
        volume = 0f;
        if (!float.TryParse(text.Trim().TrimEnd('%'), out var percent))
        {
            return false;
        }

        volume = Configuration.ClampToUiRange(percent / 100f, maxVolume);
        return true;
    }

    private void DrawNewGroupPopup()
    {
        if (_showNewGroupPopup && !ImGui.IsPopupOpen("###SoundMixerNewGroupPopup"))
        {
            ImGui.OpenPopup("###SoundMixerNewGroupPopup");
        }

        var popupOpen = _showNewGroupPopup;
        if (ImGui.BeginPopupModal($"{L(PopupNewGroup)}###SoundMixerNewGroupPopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            var parent = GroupHierarchy.FindById(Plugin.Config, _newGroupParentId);
            if (parent != null)
            {
                ImGui.TextDisabled(LF(PopupParent, parent.Name));
            }
            else
            {
                ImGui.TextDisabled(L(PopupParentRoot));
            }

            ImGui.InputText(L(PopupGroupName), ref _newGroupName, 64);
            if (ImGui.Button(L(BtnCreate), new Vector2(120, 0)))
            {
                var created = GroupHierarchy.CreateGroup(Plugin.Config, _newGroupName, _newGroupParentId);
                _selectedGroupId = created.Id;
                _newGroupParentId = null;
                SaveGroupChanges();
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button(L(BtnCancel), new Vector2(120, 0)))
            {
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        _showNewGroupPopup = popupOpen;
    }

    private void DrawPatternPopup()
    {
        if (_showPatternPopup && !ImGui.IsPopupOpen("###SoundMixerPatternPopup"))
        {
            ImGui.OpenPopup("###SoundMixerPatternPopup");
        }

        var popupOpen = _showPatternPopup;
        if (ImGui.BeginPopupModal($"{L(PopupAddPattern)}###SoundMixerPatternPopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.InputText(L(PopupGlobPattern), ref _newPatternInput, 256);
            ImGui.TextDisabled(L(PopupAddPatternHint));

            if (ImGui.Button(L(BtnAdd), new Vector2(120, 0)) && GetSelectedGroup() is { } selectedGroup)
            {
                AddPatternToGroup(_newPatternInput, selectedGroup);
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button(L(BtnCancel), new Vector2(120, 0)))
            {
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        _showPatternPopup = popupOpen;
    }

    private void DrawRenamePopup()
    {
        if (_showRenamePopup && !ImGui.IsPopupOpen("###SoundMixerRenamePopup"))
        {
            ImGui.OpenPopup("###SoundMixerRenamePopup");
        }

        var popupOpen = _showRenamePopup;
        if (ImGui.BeginPopupModal($"{L(PopupRenameGroup)}###SoundMixerRenamePopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.InputText(L(PopupGroupName), ref _renameGroupInput, 64);
            if (ImGui.Button(L(BtnConfirm), new Vector2(120, 0)) && GetSelectedGroup() is { } selectedGroup)
            {
                selectedGroup.Name = _renameGroupInput;
                SaveGroupChanges();
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button(L(BtnCancel), new Vector2(120, 0)))
            {
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        _showRenamePopup = popupOpen;
    }

    private void DrawIndividualVolumePopup()
    {
        if (_showIndividualVolumePopup && !ImGui.IsPopupOpen("###SoundMixerIndividualVolumePopup"))
        {
            ImGui.OpenPopup("###SoundMixerIndividualVolumePopup");
        }

        var popupOpen = _showIndividualVolumePopup;
        if (ImGui.BeginPopupModal($"{L(PopupIndividualVol)}###SoundMixerIndividualVolumePopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextDisabled(_individualVolumePath);
            var maxVolume = Plugin.Config.GetMaxVolume();
            _individualVolume = Configuration.ClampToUiRange(_individualVolume, maxVolume);
            var maxPct = (int)(maxVolume * 100);
            DrawEditableVolumeSlider(
                "IndividualVolume",
                _individualVolume,
                maxVolume,
                LF(PopupIndividualVolTip, maxPct),
                newVolume => _individualVolume = newVolume
            );

            if (ImGui.Button(L(BtnSave), new Vector2(120, 0)))
            {
                Plugin.Config.IndividualVolumes[_individualVolumePath] = _individualVolume;
                Plugin.Config.Save();
                Plugin.VolumeCalculator.ClearCache();
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button(L(BtnReset), new Vector2(120, 0)))
            {
                Plugin.Config.IndividualVolumes.Remove(_individualVolumePath);
                Plugin.Config.Save();
                Plugin.VolumeCalculator.ClearCache();
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        _showIndividualVolumePopup = popupOpen;
    }

    private void DrawPathAliasPopup()
    {
        if (_showPathAliasPopup && !ImGui.IsPopupOpen("###SoundMixerPathAliasPopup"))
        {
            ImGui.OpenPopup("###SoundMixerPathAliasPopup");
        }

        var popupOpen = _showPathAliasPopup;
        if (ImGui.BeginPopupModal($"{L(PopupFixPath)}###SoundMixerPathAliasPopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextWrapped(L(PopupFixPathIntro));
            ImGui.Spacing();
            ImGui.InputText(L(PopupAliasFrom), ref _pathAliasFrom, 256);
            ImGui.InputText(L(PopupAliasTo), ref _pathAliasTo, 256);
            ImGui.TextDisabled(L(PopupAliasHint));

            if (ImGui.Button(L(BtnSave), new Vector2(120, 0)))
            {
                Plugin.Filter.RegisterPathAlias(_pathAliasFrom, _pathAliasTo);
                SetStatusMessage(LF(MsgSavedAlias, _pathAliasFrom, _pathAliasTo));
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button(L(BtnCancel), new Vector2(120, 0)))
            {
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        _showPathAliasPopup = popupOpen;
    }

    private void DrawEditPatternPopup()
    {
        if (_showEditPatternPopup && !ImGui.IsPopupOpen("###SoundMixerEditPatternPopup"))
        {
            ImGui.OpenPopup("###SoundMixerEditPatternPopup");
        }

        var popupOpen = _showEditPatternPopup;
        if (ImGui.BeginPopupModal($"{L(PopupEditPattern)}###SoundMixerEditPatternPopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.InputText(L(PopupGlobPattern), ref _editPatternInput, 256);

            if (ImGui.Button(L(BtnSave), new Vector2(120, 0))
                && GetSelectedGroup() is { } selectedGroup
                && _editingPatternIndex >= 0)
            {
                var group = selectedGroup;
                if (_editingPatternIndex < group.PathPatterns.Count)
                {
                    group.PathPatterns[_editingPatternIndex] = _editPatternInput.Trim().ToLowerInvariant();
                    Plugin.Config.InvalidateGlobCache();
                    Plugin.Config.Save();
                    Plugin.VolumeCalculator.ClearCache();
                }

                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button(L(BtnCancel), new Vector2(120, 0)))
            {
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        _showEditPatternPopup = popupOpen;
    }

    private void DrawEditSoundPathPopup()
    {
        if (_showEditSoundPathPopup && !ImGui.IsPopupOpen("###SoundMixerEditSoundPathPopup"))
        {
            ImGui.OpenPopup("###SoundMixerEditSoundPathPopup");
        }

        var popupOpen = _showEditSoundPathPopup;
        if (ImGui.BeginPopupModal($"{L(PopupEditSoundPath)}###SoundMixerEditSoundPathPopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextDisabled(LF(PopupOriginalPath, _editSoundPathOriginal));
            ImGui.InputText(L(PopupNewPath), ref _editSoundPathInput, 256);

            if (ImGui.Button(L(BtnSave), new Vector2(120, 0)) && GetSelectedGroup() is { } selectedGroup)
            {
                RenameSoundPath(_editSoundPathOriginal, _editSoundPathInput, selectedGroup);
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button(L(BtnCancel), new Vector2(120, 0)))
            {
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        _showEditSoundPathPopup = popupOpen;
    }

    private void RenameSoundPath(string oldPath, string newPath, SoundGroup group)
    {
        oldPath = oldPath.Trim().ToLowerInvariant();
        newPath = newPath.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(oldPath) || string.IsNullOrWhiteSpace(newPath) || oldPath == newPath)
        {
            return;
        }

        var index = group.SoundPaths.IndexOf(oldPath);
        if (index >= 0)
        {
            group.SoundPaths[index] = newPath;
        }

        if (Plugin.Config.SoundToGroup.Remove(oldPath, out var groupId))
        {
            Plugin.Config.SoundToGroup[newPath] = groupId;
        }

        if (Plugin.Config.IndividualVolumes.Remove(oldPath, out var volume))
        {
            Plugin.Config.IndividualVolumes[newPath] = volume;
        }

        if (oldPath.StartsWith("unknown/", StringComparison.Ordinal))
        {
            Plugin.Filter.RegisterPathAlias(GetScdPath(oldPath), newPath);
        }

        Plugin.Config.Save();
        Plugin.VolumeCalculator.ClearCache();
    }

    private void AddSoundToGroup(string fullPath, SoundGroup group)
    {
        fullPath = NormalizeSoundPath(fullPath);
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return;
        }

        RegisterSoundPathForGroup(fullPath, group);

        var scdPath = GetScdPath(fullPath);
        var resolvedScd = PathResolver.ResolveScdPath(Plugin.Config, scdPath);
        if (resolvedScd != scdPath)
        {
            if (PathResolver.TrySplitSoundIndex(fullPath, out _, out var index))
            {
                RegisterSoundPathForGroup(PathResolver.BuildSpecificPath(resolvedScd, index), group);
            }
            else
            {
                RegisterSoundPathForGroup(resolvedScd, group);
            }
        }

        Plugin.Config.Save();
        Plugin.VolumeCalculator.ClearCache();
        SetStatusMessage(LF(MsgAddedSound, fullPath, group.Name));
        Services.PluginLog.Info($"Added {fullPath} to group {group.Name}");
    }

    private void RegisterSoundPathForGroup(string soundPath, SoundGroup group)
    {
        if (!group.SoundPaths.Contains(soundPath))
        {
            group.SoundPaths.Add(soundPath);
        }

        Plugin.Config.SoundToGroup[soundPath] = group.Id;
    }

    private static string NormalizeSoundPath(string path)
    {
        return path.Trim().ToLowerInvariant();
    }

    private void SetStatusMessage(string message)
    {
        _statusMessage = message;
        _statusMessageExpiry = DateTime.Now.AddSeconds(3);
    }

    private void AddPatternToGroup(string pattern, SoundGroup group)
    {
        pattern = pattern.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        if (!group.PathPatterns.Contains(pattern))
        {
            group.PathPatterns.Add(pattern);
        }

        Plugin.Config.InvalidateGlobCache();
        Plugin.Config.Save();
        Plugin.VolumeCalculator.ClearCache();
        Services.PluginLog.Info($"Added pattern {pattern} to group {group.Name}");
    }

    private static string GetScdPath(string fullPath)
    {
        fullPath = NormalizeSoundPath(fullPath);
        return PathResolver.TrySplitSoundIndex(fullPath, out var scdPath, out _)
            ? scdPath
            : fullPath;
    }

    private SoundGroup? GetSelectedGroup()
    {
        return GroupHierarchy.FindById(Plugin.Config, _selectedGroupId);
    }

    private void OpenNewGroupPopup(string? parentId)
    {
        _newGroupName = L(GroupNewDefault);
        _newGroupParentId = parentId;
        _showNewGroupPopup = true;
    }

    private void SaveGroupChanges()
    {
        PresetManager.EnsureAtLeastOneGroup(Plugin.Config);
        Plugin.Config.Save();
        Plugin.Config.InvalidateGlobCache();
        Plugin.VolumeCalculator.ClearCache();
    }

    private void DrawNewPresetPopup()
    {
        if (_showNewPresetPopup && !ImGui.IsPopupOpen("###SoundMixerNewPresetPopup"))
        {
            ImGui.OpenPopup("###SoundMixerNewPresetPopup");
        }

        var popupOpen = _showNewPresetPopup;
        if (ImGui.BeginPopupModal($"{L(PopupNewPreset)}###SoundMixerNewPresetPopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextWrapped(L(PopupNewPresetIntro));
            ImGui.Spacing();
            ImGui.InputText(L(PopupPresetName), ref _newPresetName, 64);

            var nameValid = PresetManager.IsNameAvailable(Plugin.Config, _newPresetName);
            if (!nameValid)
            {
                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), L(PopupPresetNameInvalid));
            }

            if (ImGui.Button(L(BtnCreate), new Vector2(120, 0)) && nameValid)
            {
                PresetManager.SyncActivePreset(Plugin.Config);
                var preset = PresetManager.CreateNew(_newPresetName.Trim());
                Plugin.Config.Presets.Add(preset);
                Plugin.Config.ActivePresetId = preset.Id;
                PresetManager.ApplyToConfig(Plugin.Config, preset, Plugin.Filter);
                _selectedGroupId = preset.Groups[0].Id;
                _selectedPresetIndex = Plugin.Config.Presets.Count - 1;
                Plugin.Config.Save();
                Plugin.VolumeCalculator.ClearCache();
                SetStatusMessage(LF(MsgCreatedPreset, preset.Name));
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button(L(BtnCancel), new Vector2(120, 0)))
            {
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        _showNewPresetPopup = popupOpen;
    }

    private void DrawCopyPresetPopup()
    {
        if (_showCopyPresetPopup && !ImGui.IsPopupOpen("###SoundMixerCopyPresetPopup"))
        {
            ImGui.OpenPopup("###SoundMixerCopyPresetPopup");
        }

        var popupOpen = _showCopyPresetPopup;
        if (ImGui.BeginPopupModal($"{L(PopupCopyPreset)}###SoundMixerCopyPresetPopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            var active = PresetManager.FindPreset(Plugin.Config, Plugin.Config.ActivePresetId);
            ImGui.TextDisabled(LF(PopupCopyFrom, active?.Name ?? L(PresetCurrent)));
            ImGui.InputText(L(PopupCopyPresetName), ref _copyPresetName, 64);

            var nameValid = PresetManager.IsNameAvailable(Plugin.Config, _copyPresetName);
            if (!nameValid)
            {
                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), L(PopupPresetNameInvalid));
            }

            if (ImGui.Button(L(PresetCopy), new Vector2(120, 0)) && nameValid && active != null)
            {
                PresetManager.SyncActivePreset(Plugin.Config);
                var copy = PresetManager.ClonePreset(active, _copyPresetName.Trim());
                Plugin.Config.Presets.Add(copy);
                Plugin.Config.ActivePresetId = copy.Id;
                PresetManager.ApplyToConfig(Plugin.Config, copy, Plugin.Filter);
                _selectedGroupId = copy.Groups.FirstOrDefault()?.Id;
                _selectedPresetIndex = Plugin.Config.Presets.Count - 1;
                Plugin.Config.Save();
                Plugin.VolumeCalculator.ClearCache();
                if (Plugin.Config.Enabled)
                {
                    Plugin.Filter.RefreshAllActiveSounds();
                }

                SetStatusMessage(LF(MsgCopiedPreset, copy.Name));
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button(L(BtnCancel), new Vector2(120, 0)))
            {
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        _showCopyPresetPopup = popupOpen;
    }

    private void DrawDeletePresetPopup()
    {
        if (_showDeletePresetPopup && !ImGui.IsPopupOpen("###SoundMixerDeletePresetPopup"))
        {
            ImGui.OpenPopup("###SoundMixerDeletePresetPopup");
        }

        var popupOpen = _showDeletePresetPopup;
        if (ImGui.BeginPopupModal($"{L(PopupDeletePreset)}###SoundMixerDeletePresetPopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            var active = PresetManager.FindPreset(Plugin.Config, Plugin.Config.ActivePresetId);
            ImGui.TextWrapped(LF(PopupDeletePresetConfirm, active?.Name ?? L(PresetCurrent)));

            if (!PresetManager.CanDeletePreset(Plugin.Config))
            {
                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), L(PopupKeepOnePreset));
            }

            if (ImGui.Button(L(PresetDelete), new Vector2(120, 0)) && active != null
                && PresetManager.TryDeletePreset(Plugin.Config, active.Id))
            {
                SyncPresetComboIndex();
                _selectedGroupId = Plugin.Config.Groups.FirstOrDefault()?.Id;
                Plugin.Filter.ReloadPathAliases();
                Plugin.Config.Save();
                Plugin.VolumeCalculator.ClearCache();
                if (Plugin.Config.Enabled)
                {
                    Plugin.Filter.RefreshAllActiveSounds();
                }

                SetStatusMessage(LF(MsgDeletedPreset, active.Name));
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button(L(BtnCancel), new Vector2(120, 0)))
            {
                popupOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        _showDeletePresetPopup = popupOpen;
    }

    private void DrawHierarchicalGroupMenu(Action<SoundGroup> onPick)
    {
        void DrawNode(SoundGroup group, int depth)
        {
            var indent = new string(' ', depth * 2);
            if (ImGui.MenuItem($"{indent}{group.Name}"))
            {
                onPick(group);
            }

            foreach (var child in GroupHierarchy.GetChildren(Plugin.Config, group.Id))
            {
                DrawNode(child, depth + 1);
            }
        }

        foreach (var root in GroupHierarchy.GetRoots(Plugin.Config))
        {
            DrawNode(root, 0);
        }
    }

    private bool GroupMatchesSearch(SoundGroup group)
    {
        return GroupHierarchy.HasMatchingDescendant(Plugin.Config, group, MatchesSearch);
    }

    private bool MatchesSearch(string text)
    {
        return string.IsNullOrWhiteSpace(_searchText)
               || text.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    }

    private void ClampAllVolumes(float maxVolume)
    {
        foreach (var group in Plugin.Config.Groups)
        {
            group.GroupVolume = Configuration.ClampToUiRange(group.GroupVolume, maxVolume);
        }

        foreach (var key in Plugin.Config.IndividualVolumes.Keys.ToList())
        {
            Plugin.Config.IndividualVolumes[key] = Configuration.ClampToUiRange(
                Plugin.Config.IndividualVolumes[key],
                maxVolume
            );
        }

        Plugin.VolumeCalculator.ClearCache();
    }
}
