using System.Numerics;
using Dalamud.Bindings.ImGui;
using SoundMixer.Localization;
using static SoundMixer.Localization.Loc.Keys;

namespace SoundMixer;

public partial class MainWindow
{
    private string _newBlacklistMatch = string.Empty;
    private string _newBlacklistNote = string.Empty;
    private int _newBlacklistKind;
    private DateTime _officialBlacklistFetchReadyAt = DateTime.MinValue;
    private bool _officialBlacklistFetchInProgress;

    private const double OfficialBlacklistFetchCooldownSeconds = 10;

    private void DrawBlacklistTab()
    {
        ImGui.TextDisabled(L(BlacklistTabHint));

        if (_statusMessage != null && DateTime.Now <= _statusMessageExpiry)
        {
            ImGui.TextColored(new Vector4(0.4f, 1f, 0.6f, 1f), _statusMessage);
        }

        ImGui.Separator();
        DrawUserBlacklistSection();

        ImGui.Separator();
        DrawOfficialBlacklistSection();
    }

    private void DrawUserBlacklistSection()
    {
        ImGui.TextUnformatted(L(BlacklistUserSection));
        ImGui.TextDisabled(L(BlacklistUserHint));

        ImGui.Text(L(BlacklistMatchKind));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        var kindLabels = new[]
        {
            L(BlacklistKindKeyword),
            L(BlacklistKindPath),
            L(BlacklistKindGlob),
        };
        ImGui.Combo("##BlacklistNewKind", ref _newBlacklistKind, kindLabels, kindLabels.Length);

        ImGui.Text(L(BlacklistMatchLabel));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(360);
        ImGui.InputText("##BlacklistNewMatch", ref _newBlacklistMatch, 512);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(BlacklistMatchTip));
        }

        ImGui.Text(L(BlacklistNoteLabel));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(360);
        ImGui.InputText("##BlacklistNewNote", ref _newBlacklistNote, 256);

        ImGui.SameLine();
        if (ImGui.Button(L(BlacklistAddEntry)))
        {
            var kind = (SoundBlacklistMatchKind)_newBlacklistKind;
            if (Plugin.AddUserBlacklistEntry(kind, _newBlacklistMatch, _newBlacklistNote))
            {
                _newBlacklistMatch = string.Empty;
                _newBlacklistNote = string.Empty;
                SetStatusMessage(L(MsgBlacklistAddedUser));
            }
        }

        ImGui.TextDisabled(LF(BlacklistUserCount, SoundBlacklist.UserRuleCount));

        if (ImGui.BeginChild("###UserBlacklistList", new Vector2(0, 220), true))
        {
            if (Plugin.Config.UserSoundBlacklist.Count == 0)
            {
                ImGui.TextDisabled(L(BlacklistUserEmpty));
            }
            else
            {
                foreach (var entry in Plugin.Config.UserSoundBlacklist.ToList())
                {
                    ImGui.PushID(entry.Id);
                    var kindLabel = FormatBlacklistKind(entry.MatchKind);
                    var line = string.IsNullOrWhiteSpace(entry.Note)
                        ? $"[{kindLabel}] {entry.Match}"
                        : $"[{kindLabel}] {entry.Match} — {entry.Note}";
                    ImGui.Selectable(line);
                    if (ImGui.BeginPopupContextItem("###UserBlacklistCtx"))
                    {
                        if (ImGui.MenuItem(L(BlacklistDeleteEntry)))
                        {
                            Plugin.RemoveUserBlacklistEntry(entry.Id);
                            SetStatusMessage(L(MsgBlacklistRemoved));
                        }

                        ImGui.EndPopup();
                    }

                    ImGui.PopID();
                }
            }
        }

        ImGui.EndChild();
    }

    private void DrawOfficialBlacklistSection()
    {
        ImGui.TextUnformatted(L(BlacklistOfficialSection));
        ImGui.TextDisabled(L(BlacklistOfficialHint));
        ImGui.TextDisabled(
            LF(
                BlacklistOfficialMeta,
                Math.Max(Plugin.Config.OfficialBlacklistRevision, SoundBlacklist.OfficialRevision),
                SoundBlacklist.OfficialRuleCount
            )
        );

        DrawOfficialBlacklistFetchButton();

        if (ImGui.BeginChild("###OfficialBlacklistList", new Vector2(0, 220), true))
        {
            var entries = SoundBlacklist.OfficialEntries;
            if (entries.Count == 0)
            {
                ImGui.TextDisabled(L(BlacklistOfficialEmpty));
            }
            else
            {
                foreach (var entry in entries)
                {
                    ImGui.PushID($"{entry.Kind}:{entry.Match}");
                    var kindLabel = FormatOfficialKind(entry.Kind);
                    var note = entry.ResolveNote(Plugin.Config.UiLanguage);
                    var line = string.IsNullOrWhiteSpace(note)
                        ? $"[{kindLabel}] {entry.Match}"
                        : $"[{kindLabel}] {entry.Match} — {note}";
                    ImGui.TextDisabled(line);
                    ImGui.PopID();
                }
            }
        }

        ImGui.EndChild();
    }

    private void DrawOfficialBlacklistFetchButton()
    {
        var cooldownRemaining = (_officialBlacklistFetchReadyAt - DateTime.Now).TotalSeconds;
        var onCooldown = cooldownRemaining > 0;
        var disabled = onCooldown || _officialBlacklistFetchInProgress;

        if (disabled)
        {
            ImGui.BeginDisabled();
        }

        var buttonLabel = onCooldown
            ? LF(BlacklistRefreshOfficialCooldown, (int)Math.Ceiling(cooldownRemaining))
            : _officialBlacklistFetchInProgress
                ? L(BlacklistRefreshOfficialFetching)
                : L(BlacklistRefreshOfficial);

        if (ImGui.Button(buttonLabel) && !disabled)
        {
            BeginOfficialBlacklistFetch();
        }

        if (disabled)
        {
            ImGui.EndDisabled();
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(L(BlacklistRefreshOfficialTip));
        }
    }

    private void BeginOfficialBlacklistFetch()
    {
        _officialBlacklistFetchInProgress = true;
        _officialBlacklistFetchReadyAt = DateTime.Now.AddSeconds(OfficialBlacklistFetchCooldownSeconds);
        SetStatusMessage(L(MsgBlacklistSynced));

        OfficialBlacklistSync.RequestRemoteSync(
            Plugin,
            force: true,
            onComplete: (result, revision, entryCount) =>
            {
                Services.Framework.Run(() => CompleteOfficialBlacklistFetch(result, revision, entryCount));
            }
        );
    }

    private void CompleteOfficialBlacklistFetch(
        OfficialBlacklistSyncResult result,
        int revision,
        int entryCount
    )
    {
        _officialBlacklistFetchInProgress = false;

        switch (result)
        {
            case OfficialBlacklistSyncResult.Updated:
                SetStatusMessage(LF(MsgBlacklistFetchUpdated, revision, entryCount));
                break;
            case OfficialBlacklistSyncResult.UpToDate:
                SetStatusMessage(LF(MsgBlacklistFetchUpToDate, revision));
                break;
            default:
                SetStatusMessage(L(MsgBlacklistFetchFailed));
                break;
        }
    }

    private static string FormatBlacklistKind(SoundBlacklistMatchKind kind)
    {
        return kind switch
        {
            SoundBlacklistMatchKind.Path => Loc.Get(BlacklistKindPath),
            SoundBlacklistMatchKind.Glob => Loc.Get(BlacklistKindGlob),
            _ => Loc.Get(BlacklistKindKeyword),
        };
    }

    private static string FormatOfficialKind(string kind)
    {
        return kind?.Trim().ToLowerInvariant() switch
        {
            "path" => Loc.Get(BlacklistKindPath),
            "glob" => Loc.Get(BlacklistKindGlob),
            _ => Loc.Get(BlacklistKindKeyword),
        };
    }
}
