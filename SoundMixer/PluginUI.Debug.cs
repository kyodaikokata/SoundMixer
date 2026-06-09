using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using static SoundMixer.Localization.Loc.Keys;

namespace SoundMixer;

public partial class MainWindow
{
    private static readonly HookDebugId[] s_hookDebugOrder =
    [
        HookDebugId.PlaySpecificSound,
        HookDebugId.PlaySound,
        HookDebugId.PlaySystemSound,
        HookDebugId.PlayClipSound,
        HookDebugId.PlayMovieSound,
        HookDebugId.PlayBgmSound,
        HookDebugId.PlayWeatherSound,
        HookDebugId.SetVolume,
        HookDebugId.GetVolume,
        HookDebugId.LoadSoundFile,
        HookDebugId.GetResourceSync,
        HookDebugId.GetResourceAsync,
    ];

    private void DrawDebugTab()
    {
        ImGui.TextWrapped(L(DebugTabHint));
        ImGui.Spacing();

        DrawDebugGuardStatus();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var dbg = Plugin.Config.HookDebug;
        var manual = dbg.ManualControl;
        if (ImGui.Checkbox(L(DebugManualControl), ref manual))
        {
            dbg.ManualControl = manual;
            Plugin.Config.Save();
            Plugin.Filter.ApplyHookDebugSettings();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(DebugManualControlTip));
        }

        ImGui.SameLine();
        ImGuiComponents.HelpMarker(L(DebugManualControlUnsafeTip));

        ImGui.Spacing();

        var extremeVolume = dbg.DebugExtremeVolume;
        if (ImGui.Checkbox(L(DebugExtremeVolume), ref extremeVolume))
        {
            dbg.DebugExtremeVolume = extremeVolume;
            Plugin.BindEngineCapClamp();
            Plugin.VolumeCalculator.ClearCache();
            Plugin.Config.Save();
            Plugin.Api.ApplyLiveEffectiveState();
            if (Plugin.IsEffectivelyEnabled)
            {
                Plugin.Filter.RefreshAllActiveSounds();
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(DebugExtremeVolumeTip));
        }

        if (dbg.DebugExtremeVolume)
        {
            ImGui.TextColored(new Vector4(1f, 0.45f, 0.45f, 1f), L(DebugExtremeVolumeActiveNote));
        }

        ImGui.Spacing();

        if (ImGui.Button(L(DebugHookAllOn)))
        {
            dbg.SetAll(true);
            Plugin.Config.Save();
            Plugin.Filter.ApplyHookDebugSettings();
        }

        ImGui.SameLine();
        if (ImGui.Button(L(DebugHookAllOff)))
        {
            dbg.SetAll(false);
            Plugin.Config.Save();
            Plugin.Filter.ApplyHookDebugSettings();
        }

        ImGui.SameLine();
        if (ImGui.Button(L(DebugHookApply)))
        {
            Plugin.Filter.ApplyHookDebugSettings();
        }

        ImGui.Spacing();

        if (ImGui.BeginTable("###HookDebugTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn(L(DebugHookColumnName));
            ImGui.TableSetupColumn(L(DebugHookColumnResolved));
            ImGui.TableSetupColumn(L(DebugHookColumnRuntime));
            ImGui.TableSetupColumn(L(DebugHookColumnDesired));
            ImGui.TableHeadersRow();

            foreach (var hookId in s_hookDebugOrder)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(L(GetHookDebugNameKey(hookId)));
                if (hookId == HookDebugId.PlaySound)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.55f, 0.35f, 1f));
                    ImGui.TextWrapped(L(DebugHookPlaySoundDanger));
                    ImGui.PopStyleColor();
                }

                var status = Plugin.Filter.GetHookRuntimeStatus(hookId);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(status.IsResolved ? L(DebugHookResolvedYes) : L(DebugHookResolvedNo));

                ImGui.TableNextColumn();
                if (!status.IsResolved)
                {
                    ImGui.TextDisabled("-");
                }
                else
                {
                    var runtimeColor = status.IsRuntimeEnabled
                        ? new Vector4(0.4f, 1f, 0.55f, 1f)
                        : new Vector4(1f, 0.45f, 0.45f, 1f);
                    ImGui.TextColored(runtimeColor, status.IsRuntimeEnabled ? L(DebugHookRuntimeOn) : L(DebugHookRuntimeOff));
                }

                ImGui.TableNextColumn();
                ImGui.PushID((int)hookId);
                var desired = dbg.GetDesired(hookId);
                ImGui.BeginDisabled(!manual || !Plugin.IsEffectivelyEnabled);
                if (ImGui.Checkbox("##Desired", ref desired))
                {
                    dbg.SetDesired(hookId, desired);
                    Plugin.Config.Save();
                    Plugin.Filter.ApplyHookDebugSettings();
                }

                ImGui.EndDisabled();
                ImGui.PopID();
            }

            ImGui.EndTable();
        }

        if (!Plugin.IsEffectivelyEnabled)
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.75f, 0.3f, 1f), L(DebugPluginDisabledNote));
        }
        else if (!manual)
        {
            ImGui.Spacing();
            ImGui.TextDisabled(L(DebugAutoModeNote));
        }
        else
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.55f, 0.35f, 1f), L(DebugManualModeNote));
        }
    }

    private void DrawDebugGuardStatus()
    {
        if (ImGui.BeginTable("###HookDebugGuards", 2, ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextDisabled(L(DebugGuardMount));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(MountTransitionGuard.IsActive ? L(DebugGuardActive) : L(DebugGuardInactive));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextDisabled(L(DebugGuardGuideroid));
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(
                MountTransitionGuard.IsGuideroidLoopSafetyActive ? L(DebugGuardActive) : L(DebugGuardInactive)
            );

            ImGui.EndTable();
        }
    }

    private static string GetHookDebugNameKey(HookDebugId id) =>
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
}
