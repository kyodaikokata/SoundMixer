using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using SoundMixer.Localization;

namespace SoundMixer;

internal static class HookGuardTriggerEvaluator
{
    internal static readonly HookGuardTrigger[] UserSelectable =
    [
        HookGuardTrigger.Mounted,
        HookGuardTrigger.InCombat,
        HookGuardTrigger.WeaponsOut,
        HookGuardTrigger.Casting,
        HookGuardTrigger.OccupiedInEvent,
        HookGuardTrigger.BoundByDuty,
        HookGuardTrigger.Jumping,
    ];

    internal static bool IsActive(HookGuardTrigger trigger)
    {
        try
        {
            return trigger switch
            {
                HookGuardTrigger.Mounted => MountTransitionGuard.IsActive,
                HookGuardTrigger.GuideroidGrace => MountTransitionGuard.IsGuideroidLoopSafetyActive,
                HookGuardTrigger.InCombat => Services.Condition[ConditionFlag.InCombat],
                HookGuardTrigger.WeaponsOut => IsWeaponDrawn(),
                HookGuardTrigger.Casting => Services.Condition[ConditionFlag.Casting]
                    || Services.Condition[ConditionFlag.Casting87],
                HookGuardTrigger.OccupiedInEvent => Services.Condition[ConditionFlag.OccupiedInEvent],
                HookGuardTrigger.BoundByDuty => Services.Condition[ConditionFlag.BoundByDuty]
                    || Services.Condition[ConditionFlag.BoundByDuty56]
                    || Services.Condition[ConditionFlag.BoundByDuty95],
                HookGuardTrigger.Jumping => Services.Condition[ConditionFlag.Jumping]
                    || Services.Condition[ConditionFlag.Jumping61],
                _ => false,
            };
        }
        catch
        {
            return false;
        }
    }

    internal static string GetLabelKey(HookGuardTrigger trigger) =>
        trigger switch
        {
            HookGuardTrigger.InCombat => Loc.Keys.GuardTriggerInCombat,
            HookGuardTrigger.WeaponsOut => Loc.Keys.GuardTriggerWeaponsOut,
            HookGuardTrigger.Casting => Loc.Keys.GuardTriggerCasting,
            HookGuardTrigger.OccupiedInEvent => Loc.Keys.GuardTriggerOccupiedInEvent,
            HookGuardTrigger.BoundByDuty => Loc.Keys.GuardTriggerBoundByDuty,
            HookGuardTrigger.Jumping => Loc.Keys.GuardTriggerJumping,
            HookGuardTrigger.GuideroidGrace => Loc.Keys.GuardTriggerGuideroidGrace,
            _ => Loc.Keys.GuardTriggerMounted,
        };

    private static bool IsWeaponDrawn()
    {
        var player = Services.ObjectTable.LocalPlayer;
        return player != null && player.StatusFlags.HasFlag(StatusFlags.WeaponOut);
    }
}
