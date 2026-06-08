using System.Numerics;
using Dalamud.Bindings.ImGui;

using static SoundMixer.Localization.Loc.Keys;



namespace SoundMixer;



public partial class MainWindow

{

    private int _selectedAdvancedTab;



    private void DrawAdvancedSettingsTab()

    {

        ImGui.TextDisabled(L(AdvancedTabHint));



        if (ImGui.BeginTabBar("###SoundMixerAdvancedTabBar"))

        {

            if (ImGui.BeginTabItem($"{L(TabSoundBlacklist)}###SoundMixerAdvancedSoundBlacklistTab"))

            {

                _selectedAdvancedTab = 0;

                ImGui.EndTabItem();

            }



            if (ImGui.BeginTabItem($"{L(TabActionGuards)}###SoundMixerAdvancedActionGuardsTab"))

            {

                _selectedAdvancedTab = 1;

                ImGui.EndTabItem();

            }



            ImGui.EndTabBar();

        }



        var contentHeight = Math.Max(ImGui.GetContentRegionAvail().Y, 1f);

        ImGui.BeginChild("###SoundMixerAdvancedTabContent", new Vector2(0, contentHeight), false);

        switch (_selectedAdvancedTab)

        {

            case 1:

                DrawActionGuardsTab();

                break;

            default:

                DrawBlacklistTab();

                break;

        }



        ImGui.EndChild();

    }

}

