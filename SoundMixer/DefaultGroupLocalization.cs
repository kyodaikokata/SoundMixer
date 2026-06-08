using SoundMixer.Localization;
using static SoundMixer.Localization.Loc.Keys;

namespace SoundMixer;

internal static class DefaultGroupLocalization
{
    private const string DefaultPresetId = "2fecadaf-c027-4586-9385-2ed1d307855a";

    private const string WeaponSkillGroupId = "c725104f-2983-48bf-b3aa-4be5d9b9204c";

    private static readonly (string GroupId, string LocKey)[] GroupMappings =
    [
        ("f3707363-c966-4a3d-b90d-d941a73bcba0", DefaultGroupBattleRoot),
        ("a3675df1-7787-4415-a1a6-7829785a7d6e", DefaultGroupEnvRoot),
        ("bf7664ec-4c60-42cc-aa46-68ab9545125c", BuiltinBgm),
        ("a8a8dba3-a7ed-4d79-b85a-e3f600e23b59", BuiltinUi),
        ("ab397304-0ef4-46fe-baf2-d2b177a9685c", BuiltinBattle),
        ("c725104f-2983-48bf-b3aa-4be5d9b9204c", DefaultGroupWeaponSkill),
        ("c0de5559-f489-4d9b-883d-86c191d3a0dc", DefaultGroupMagic),
        ("e42763ee-8428-421c-a0dc-b98d477c55f3", DefaultGroupBuff),
        ("6f4a819b-1d96-490a-8294-7b807a9c8254", DefaultGroupWeapon),
        ("87190d35-83f1-4056-94fd-0c9a36992105", DefaultGroupBattleVoice),
        ("4c18d51b-9bac-4e2a-ab77-57e3cfe9073c", BuiltinEnv),
        ("0ddb4933-b143-41d4-917f-d2d782f93309", DefaultGroupFootstep),
        ("bac1cbb7-3d99-4122-a1a1-85a4f03d9f7a", DefaultGroupCloth),
        ("6ffc8ac7-3a7b-4344-9060-e448b88a6410", DefaultGroupUnknown),
    ];

    internal static void Apply(Configuration config)
    {
        Loc.Bind(config);

        foreach (var (groupId, locKey) in GroupMappings)
        {
            var localizedName = Loc.Get(locKey);
            UpdateGroupName(config.Groups, groupId, localizedName);

            foreach (var preset in config.Presets)
            {
                UpdateGroupName(preset.Groups, groupId, localizedName);
            }
        }

        var weaponSkillNote = Loc.Get(DefaultGroupWeaponSkillDesc);
        UpdateGroupDescription(config.Groups, WeaponSkillGroupId, weaponSkillNote);
        foreach (var preset in config.Presets)
        {
            UpdateGroupDescription(preset.Groups, WeaponSkillGroupId, weaponSkillNote);
        }

        var defaultPreset = config.Presets.Find(p => p.Id == DefaultPresetId);
        if (defaultPreset != null)
        {
            defaultPreset.Name = Loc.Get(PresetDefaultName);
        }
    }

    private static void UpdateGroupName(List<SoundGroup> groups, string groupId, string name)
    {
        var group = groups.Find(g => g.Id == groupId);
        if (group != null)
        {
            group.Name = name;
        }
    }

    private static void UpdateGroupDescription(List<SoundGroup> groups, string groupId, string description)
    {
        var group = groups.Find(g => g.Id == groupId);
        if (group != null)
        {
            group.Description = description;
        }
    }
}
