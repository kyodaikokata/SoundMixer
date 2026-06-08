using System.Numerics;
using Dalamud.Bindings.ImGui;
using static SoundMixer.Localization.Loc.Keys;

namespace SoundMixer;

public partial class MainWindow
{
    private string _newGuardNote = string.Empty;
    private int _newGuardTriggerIndex;
    private bool _newGuardSkipScan;
    private readonly Dictionary<HookDebugId, bool> _newGuardHookSelection = new();
    private DateTime _officialGuardsFetchReadyAt = DateTime.MinValue;
    private bool _officialGuardsFetchInProgress;

    private const double OfficialGuardsFetchCooldownSeconds = 10;
    private static readonly Vector4 GuardManualOverrideColor = new(1f, 0.35f, 0.35f, 1f);

    private void DrawActionGuardsTab()
    {
        MountTransitionGuard.Update();
        ImGui.TextDisabled(L(GuardTabHint));

        if (_statusMessage != null && DateTime.Now <= _statusMessageExpiry)
        {
            ImGui.TextColored(new Vector4(0.4f, 1f, 0.6f, 1f), _statusMessage);
        }

        ImGui.Separator();
        DrawUserHookGuardsSection();

        ImGui.Separator();
        DrawOfficialHookGuardsHandbook();
    }

    private void DrawUserHookGuardsSection()
    {
        ImGui.TextUnformatted(L(GuardUserSection));
        ImGui.TextDisabled(L(GuardUserHint));

        ImGui.Text(L(GuardTriggerLabel));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(260);
        var triggerLabels = HookGuardTriggerEvaluator.UserSelectable
            .Select(trigger => L(HookGuardTriggerEvaluator.GetLabelKey(trigger)))
            .ToArray();
        if (_newGuardTriggerIndex >= triggerLabels.Length)
        {
            _newGuardTriggerIndex = 0;
        }

        ImGui.Combo("##GuardNewTrigger", ref _newGuardTriggerIndex, triggerLabels, triggerLabels.Length);

        ImGui.TextDisabled(L(GuardHooksLabel));
        EnsureGuardHookSelectionInitialized();
        foreach (var hookId in HookGuardIds.AllHooks)
        {
            var selected = _newGuardHookSelection[hookId];
            if (ImGui.Checkbox($"{FormatHookGuardName(hookId)}##NewGuardHook{(int)hookId}", ref selected))
            {
                _newGuardHookSelection[hookId] = selected;
            }
        }

        if (ImGui.Checkbox(L(GuardSkipActiveListScan), ref _newGuardSkipScan))
        {
        }

        ImGui.Text(L(GuardNoteLabel));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(360);
        ImGui.InputText("##GuardNewNote", ref _newGuardNote, 256);

        ImGui.SameLine();
        if (ImGui.Button(L(GuardAddEntry)))
        {
            var hooks = _newGuardHookSelection
                .Where(pair => pair.Value)
                .Select(pair => pair.Key)
                .ToList();
            if (Plugin.AddUserHookGuardEntry(
                    HookGuardTriggerEvaluator.UserSelectable[_newGuardTriggerIndex],
                    hooks,
                    _newGuardSkipScan,
                    _newGuardNote))
            {
                _newGuardNote = string.Empty;
                _newGuardSkipScan = false;
                ResetGuardHookSelection();
                SetStatusMessage(L(MsgGuardAddedUser));
            }
        }

        ImGui.TextDisabled(LF(GuardUserCount, HookGuardPolicy.UserRuleCount));

        if (ImGui.BeginChild("###UserHookGuardsList", new Vector2(0, 200), true))
        {
            if (Plugin.Config.UserHookGuards.Count == 0)
            {
                ImGui.TextDisabled(L(GuardUserEmpty));
            }
            else
            {
                foreach (var entry in Plugin.Config.UserHookGuards.ToList())
                {
                    ImGui.PushID(entry.Id);
                    var enabled = entry.Enabled;
                    if (ImGui.Checkbox("##Enabled", ref enabled))
                    {
                        Plugin.SetUserHookGuardEnabled(entry.Id, enabled);
                    }

                    ImGui.SameLine();
                    var hooks = FormatHookIdList(entry.DisabledHooks);
                    var scan = entry.SkipActiveListScan ? L(GuardSkipScanBadge) : string.Empty;
                    var line = string.IsNullOrWhiteSpace(entry.Note)
                        ? $"[{FormatGuardTrigger(entry.Trigger)}] {hooks}{scan}"
                        : $"[{FormatGuardTrigger(entry.Trigger)}] {hooks}{scan} — {entry.Note}";
                    ImGui.TextUnformatted(line);

                    if (ImGui.BeginPopupContextItem("###UserGuardCtx"))
                    {
                        if (ImGui.MenuItem(L(GuardDeleteEntry)))
                        {
                            Plugin.RemoveUserHookGuardEntry(entry.Id);
                            SetStatusMessage(L(MsgGuardRemoved));
                        }

                        ImGui.EndPopup();
                    }

                    ImGui.PopID();
                }
            }
        }

        ImGui.EndChild();
    }

    private void DrawOfficialHookGuardsHandbook()
    {
        ImGui.TextUnformatted(L(GuardOfficialHandbook));
        ImGui.TextDisabled(L(GuardOfficialHint));
        ImGui.TextDisabled(
            LF(
                GuardOfficialMeta,
                Math.Max(Plugin.Config.OfficialHookGuardsRevision, HookGuardPolicy.OfficialRevision),
                HookGuardPolicy.OfficialRuleCount
            )
        );

        DrawOfficialGuardsFetchButton();

        if (ImGui.BeginChild("###OfficialHookGuardsHandbook", new Vector2(0, 240), true))
        {
            var entries = HookGuardPolicy.OfficialHandbook;
            if (entries.Count == 0)
            {
                ImGui.TextDisabled(L(GuardOfficialEmpty));
            }
            else
            {
                var manualHookOverride = Plugin.Config.HookDebug.ManualControl;
                foreach (var entry in entries)
                {
                    ImGui.PushID(entry.Id);
                    var trigger = entry.ResolveTrigger();
                    var active = HookGuardPolicy.IsTriggerActive(trigger);
                    var hooks = FormatHookIdList(entry.DisabledHooks);
                    var scan = entry.SkipActiveListScan ? L(GuardSkipScanBadge) : string.Empty;
                    var note = entry.ResolveNote(Plugin.Config.UiLanguage);
                    var triggerLabel = FormatGuardTrigger(trigger);

                    if (manualHookOverride)
                    {
                        var triggerState = active ? L(GuardStatusActive) : L(GuardStatusInactive);
                        ImGui.TextColored(
                            GuardManualOverrideColor,
                            $"[{L(GuardStatusManualOverride)}] [{triggerLabel}] {hooks}{scan}"
                        );
                        ImGui.TextColored(
                            GuardManualOverrideColor,
                            LF(GuardManualOverrideDetail, triggerState)
                        );
                    }
                    else
                    {
                        var statusColor = active
                            ? new Vector4(1f, 0.55f, 0.35f, 1f)
                            : new Vector4(0.55f, 0.55f, 0.55f, 1f);
                        var status = active ? L(GuardStatusActive) : L(GuardStatusInactive);
                        ImGui.TextColored(statusColor, $"[{status}] [{triggerLabel}] {hooks}{scan}");
                    }

                    if (!string.IsNullOrWhiteSpace(note))
                    {
                        ImGui.TextDisabled(note);
                    }

                    ImGui.PopID();
                }
            }
        }

        ImGui.EndChild();
    }

    private void DrawOfficialGuardsFetchButton()
    {
        var cooldownRemaining = (_officialGuardsFetchReadyAt - DateTime.Now).TotalSeconds;
        var onCooldown = cooldownRemaining > 0;
        var disabled = onCooldown || _officialGuardsFetchInProgress;

        if (disabled)
        {
            ImGui.BeginDisabled();
        }

        var buttonLabel = onCooldown
            ? LF(GuardRefreshOfficialCooldown, (int)Math.Ceiling(cooldownRemaining))
            : _officialGuardsFetchInProgress
                ? L(GuardRefreshOfficialFetching)
                : L(GuardRefreshOfficial);

        if (ImGui.Button(buttonLabel) && !disabled)
        {
            BeginOfficialGuardsFetch();
        }

        if (disabled)
        {
            ImGui.EndDisabled();
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(L(GuardRefreshOfficialTip));
        }
    }

    private void BeginOfficialGuardsFetch()
    {
        _officialGuardsFetchInProgress = true;
        _officialGuardsFetchReadyAt = DateTime.Now.AddSeconds(OfficialGuardsFetchCooldownSeconds);
        SetStatusMessage(L(MsgGuardSynced));

        OfficialHookGuardsSync.RequestRemoteSync(
            Plugin,
            force: true,
            onComplete: (result, revision, entryCount) =>
            {
                Services.Framework.Run(() => CompleteOfficialGuardsFetch(result, revision, entryCount));
            }
        );
    }

    private void CompleteOfficialGuardsFetch(
        OfficialHookGuardsSyncResult result,
        int revision,
        int entryCount
    )
    {
        _officialGuardsFetchInProgress = false;

        switch (result)
        {
            case OfficialHookGuardsSyncResult.Updated:
                SetStatusMessage(LF(MsgGuardFetchUpdated, revision, entryCount));
                break;
            case OfficialHookGuardsSyncResult.UpToDate:
                SetStatusMessage(LF(MsgGuardFetchUpToDate, revision));
                break;
            default:
                SetStatusMessage(L(MsgGuardFetchFailed));
                break;
        }
    }

    private void EnsureGuardHookSelectionInitialized()
    {
        foreach (var hookId in HookGuardIds.AllHooks)
        {
            _newGuardHookSelection.TryAdd(hookId, false);
        }
    }

    private void ResetGuardHookSelection()
    {
        foreach (var hookId in HookGuardIds.AllHooks)
        {
            _newGuardHookSelection[hookId] = false;
        }
    }

    private static string FormatGuardTrigger(HookGuardTrigger trigger) =>
        L(HookGuardTriggerEvaluator.GetLabelKey(trigger));

    private static string FormatHookGuardName(HookDebugId id) => L(GetHookGuardNameKey(id));

    private static string GetHookGuardNameKey(HookDebugId id) =>
        id switch
        {
            HookDebugId.PlaySpecificSound => DebugHookPlaySpecificSound,
            HookDebugId.PlaySound => DebugHookPlaySound,
            HookDebugId.PlaySystemSound => DebugHookPlaySystemSound,
            HookDebugId.PlayClipSound => DebugHookPlayClipSound,
            HookDebugId.PlayMovieSound => DebugHookPlayMovieSound,
            HookDebugId.PlayBgmSound => DebugHookPlayBgmSound,
            HookDebugId.PlayWeatherSound => DebugHookPlayWeatherSound,
            HookDebugId.SetVolume => DebugHookSetVolume,
            HookDebugId.GetVolume => DebugHookGetVolume,
            HookDebugId.LoadSoundFile => DebugHookLoadSoundFile,
            HookDebugId.GetResourceSync => DebugHookGetResourceSync,
            HookDebugId.GetResourceAsync => DebugHookGetResourceAsync,
            _ => DebugHookPlaySpecificSound,
        };

    private static string FormatHookIdList(IEnumerable<string> hookIds)
    {
        var names = hookIds
            .Select(id => HookGuardIds.TryParse(id, out var hookId) ? FormatHookGuardName(hookId) : id)
            .Where(name => !string.IsNullOrWhiteSpace(name));
        return string.Join(", ", names);
    }
}
