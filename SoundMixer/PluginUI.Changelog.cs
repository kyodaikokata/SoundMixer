using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using static SoundMixer.Localization.Loc.Keys;

namespace SoundMixer;

public partial class MainWindow
{
    private void DrawChangelogTab()
    {
        if (ImGui.Button(L(SupportKofi)))
        {
            Process.Start(new ProcessStartInfo(KofiUrl) { UseShellExecute = true });
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(L(SupportKofiTip));
        }

        ImGui.Spacing();
        ImGui.Text(L(ChangelogTitle));
        ImGui.Separator();

        if (ImGui.BeginChild("ChangelogBody", new Vector2(0, 0), false))
        {
            ImGui.TextWrapped(L(ChangelogBody));
        }

        ImGui.EndChild();
    }
}
