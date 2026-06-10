using System.Globalization;

namespace SoundMixer.Localization;

internal static class Loc
{
    internal static class Keys
    {
        internal const string WindowTitle = "Window.Title";
        internal const string CommandHelp = "Command.Help";

        internal const string TabMain = "Tab.Main";
        internal const string TabChangelog = "Tab.Changelog";
        internal const string TabBlacklist = "Tab.Blacklist";
        internal const string TabAdvancedSettings = "Tab.AdvancedSettings";
        internal const string TabSoundBlacklist = "Tab.SoundBlacklist";
        internal const string TabActionGuards = "Tab.ActionGuards";
        internal const string TabDebug = "Tab.Debug";
        internal const string AdvancedTabHint = "Advanced.TabHint";

        internal const string LangLabel = "Lang.Label";
        internal const string LangSystem = "Lang.System";
        internal const string LangChinese = "Lang.Chinese";
        internal const string LangEnglish = "Lang.English";

        internal const string StatusLabel = "Status.Label";
        internal const string StatusEnabled = "Status.Enabled";
        internal const string StatusDisabled = "Status.Disabled";
        internal const string StatusIpcBadge = "Status.IpcBadge";
        internal const string StatusIpcTip = "Status.IpcTip";
        internal const string BtnEnable = "Btn.Enable";
        internal const string BtnDisable = "Btn.Disable";
        internal const string BtnRefreshAll = "Btn.RefreshAll";
        internal const string BtnRefreshAllTip = "Btn.RefreshAll.Tip";
        internal const string BtnClearCache = "Btn.ClearCache";
        internal const string BtnClearCacheTip = "Btn.ClearCache.Tip";
        internal const string ExpertMode = "ExpertMode";
        internal const string ExpertModeTip = "ExpertMode.Tip";
        internal const string Monitoring = "Monitoring";
        internal const string MonitoringTip = "Monitoring.Tip";
        internal const string TrackedScd = "TrackedScd";

        internal const string HookStatusLine = "Hook.StatusLine";
        internal const string HookStatusTip = "Hook.StatusTip";
        internal const string HookOn = "Hook.On";
        internal const string HookMissing = "Hook.Missing";

        internal const string MonitorTitle = "Monitor.Title";
        internal const string MonitorHideMatched = "Monitor.HideMatched";
        internal const string MonitorHideMatchedTip = "Monitor.HideMatched.Tip";
        internal const string MonitorHideKeywords = "Monitor.HideKeywords";
        internal const string MonitorHideKeywordsTip = "Monitor.HideKeywords.Tip";
        internal const string MonitorHint = "Monitor.Hint";
        internal const string MonitorHookActiveScan = "Monitor.Hook.ActiveScan";
        internal const string MonitorHookPathResolve = "Monitor.Hook.PathResolve";
        internal const string MonitorHookFilter = "Monitor.HookFilter";
        internal const string MonitorHookFilterTip = "Monitor.HookFilter.Tip";
        internal const string MonitorHookFilterSelectAll = "Monitor.HookFilter.SelectAll";
        internal const string MonitorHookFilterClear = "Monitor.HookFilter.Clear";
        internal const string MonitorEmpty = "Monitor.Empty";
        internal const string MonitorPlayingTag = "Monitor.PlayingTag";
        internal const string MonitorPathResolveFailed = "Monitor.PathResolveFailed";
        internal const string MonitorPathResolveFailedDetail = "Monitor.PathResolveFailed.Detail";

        internal const string DebugTabHint = "Debug.TabHint";
        internal const string DebugManualControl = "Debug.ManualControl";
        internal const string DebugManualControlTip = "Debug.ManualControl.Tip";
        internal const string DebugManualControlUnsafeTip = "Debug.ManualControl.UnsafeTip";
        internal const string DebugExtremeVolume = "Debug.ExtremeVolume";
        internal const string DebugExtremeVolumeTip = "Debug.ExtremeVolume.Tip";
        internal const string DebugExtremeVolumeActiveNote = "Debug.ExtremeVolume.ActiveNote";
        internal const string DebugHookAllOn = "Debug.Hook.AllOn";
        internal const string DebugHookAllOff = "Debug.Hook.AllOff";
        internal const string DebugHookApply = "Debug.Hook.Apply";
        internal const string DebugHookColumnName = "Debug.Hook.Column.Name";
        internal const string DebugHookColumnResolved = "Debug.Hook.Column.Resolved";
        internal const string DebugHookColumnRuntime = "Debug.Hook.Column.Runtime";
        internal const string DebugHookColumnDesired = "Debug.Hook.Column.Desired";
        internal const string DebugHookResolvedYes = "Debug.Hook.Resolved.Yes";
        internal const string DebugHookResolvedNo = "Debug.Hook.Resolved.No";
        internal const string DebugHookRuntimeOn = "Debug.Hook.Runtime.On";
        internal const string DebugHookRuntimeOff = "Debug.Hook.Runtime.Off";
        internal const string DebugPluginDisabledNote = "Debug.PluginDisabledNote";
        internal const string DebugAutoModeNote = "Debug.AutoModeNote";
        internal const string DebugManualModeNote = "Debug.ManualModeNote";
        internal const string DebugGuardMount = "Debug.Guard.Mount";
        internal const string DebugGuardGuideroid = "Debug.Guard.Guideroid";
        internal const string DebugGuardActive = "Debug.Guard.Active";
        internal const string DebugGuardInactive = "Debug.Guard.Inactive";
        internal const string DebugHookPlaySpecificSound = "Debug.Hook.PlaySpecificSound";
        internal const string DebugHookPlaySound = "Debug.Hook.PlaySound";
        internal const string DebugHookPlaySoundDanger = "Debug.Hook.PlaySound.Danger";
        internal const string DebugHookPlaySystemSound = "Debug.Hook.PlaySystemSound";
        internal const string DebugHookPlayClipSound = "Debug.Hook.PlayClipSound";
        internal const string DebugHookPlayMovieSound = "Debug.Hook.PlayMovieSound";
        internal const string DebugHookPlayBgmSound = "Debug.Hook.PlayBgmSound";
        internal const string DebugHookPlayWeatherSound = "Debug.Hook.PlayWeatherSound";
        internal const string DebugHookSetVolume = "Debug.Hook.SetVolume";
        internal const string DebugHookGetVolume = "Debug.Hook.GetVolume";
        internal const string DebugHookLoadSoundFile = "Debug.Hook.LoadSoundFile";
        internal const string DebugHookGetResourceSync = "Debug.Hook.GetResourceSync";
        internal const string DebugHookGetResourceAsync = "Debug.Hook.GetResourceAsync";

        internal const string BlacklistTabHint = "Blacklist.TabHint";
        internal const string BlacklistUserSection = "Blacklist.UserSection";
        internal const string BlacklistUserHint = "Blacklist.UserHint";
        internal const string BlacklistUserCount = "Blacklist.UserCount";
        internal const string BlacklistUserEmpty = "Blacklist.UserEmpty";
        internal const string BlacklistOfficialSection = "Blacklist.OfficialSection";
        internal const string BlacklistOfficialHint = "Blacklist.OfficialHint";
        internal const string BlacklistOfficialMeta = "Blacklist.OfficialMeta";
        internal const string BlacklistOfficialEmpty = "Blacklist.OfficialEmpty";
        internal const string BlacklistMatchKind = "Blacklist.MatchKind";
        internal const string BlacklistMatchLabel = "Blacklist.MatchLabel";
        internal const string BlacklistMatchTip = "Blacklist.MatchTip";
        internal const string BlacklistNoteLabel = "Blacklist.NoteLabel";
        internal const string BlacklistAddEntry = "Blacklist.AddEntry";
        internal const string BlacklistDeleteEntry = "Blacklist.DeleteEntry";
        internal const string BlacklistKindKeyword = "Blacklist.Kind.Keyword";
        internal const string BlacklistKindPath = "Blacklist.Kind.Path";
        internal const string BlacklistKindGlob = "Blacklist.Kind.Glob";
        internal const string BlacklistRefreshOfficial = "Blacklist.RefreshOfficial";
        internal const string BlacklistRefreshOfficialFetching = "Blacklist.RefreshOfficial.Fetching";
        internal const string BlacklistRefreshOfficialCooldown = "Blacklist.RefreshOfficial.Cooldown";
        internal const string BlacklistRefreshOfficialTip = "Blacklist.RefreshOfficial.Tip";
        internal const string BlacklistCtxAddPath = "Blacklist.Ctx.AddPath";
        internal const string BlacklistCtxAddGlob = "Blacklist.Ctx.AddGlob";
        internal const string MsgBlacklistAdded = "Msg.Blacklist.Added";
        internal const string MsgBlacklistAddedUser = "Msg.Blacklist.AddedUser";
        internal const string MsgBlacklistRemoved = "Msg.Blacklist.Removed";
        internal const string MsgBlacklistSynced = "Msg.Blacklist.Synced";
        internal const string MsgBlacklistFetchUpdated = "Msg.Blacklist.FetchUpdated";
        internal const string MsgBlacklistFetchUpToDate = "Msg.Blacklist.FetchUpToDate";
        internal const string MsgBlacklistFetchFailed = "Msg.Blacklist.FetchFailed";

        internal const string GuardTabHint = "Guard.TabHint";
        internal const string GuardUserSection = "Guard.UserSection";
        internal const string GuardUserHint = "Guard.UserHint";
        internal const string GuardUserCount = "Guard.UserCount";
        internal const string GuardUserEmpty = "Guard.UserEmpty";
        internal const string GuardOfficialHandbook = "Guard.OfficialHandbook";
        internal const string GuardOfficialHint = "Guard.OfficialHint";
        internal const string GuardOfficialMeta = "Guard.OfficialMeta";
        internal const string GuardOfficialEmpty = "Guard.OfficialEmpty";
        internal const string GuardTriggerLabel = "Guard.TriggerLabel";
        internal const string GuardTriggerMounted = "Guard.Trigger.Mounted";
        internal const string GuardTriggerGuideroidGrace = "Guard.Trigger.GuideroidGrace";
        internal const string GuardTriggerInCombat = "Guard.Trigger.InCombat";
        internal const string GuardTriggerWeaponsOut = "Guard.Trigger.WeaponsOut";
        internal const string GuardTriggerOccupiedInEvent = "Guard.Trigger.OccupiedInEvent";
        internal const string GuardTriggerBoundByDuty = "Guard.Trigger.BoundByDuty";
        internal const string GuardTriggerJumping = "Guard.Trigger.Jumping";
        internal const string GuardTriggerCasting = "Guard.Trigger.Casting";
        internal const string GuardHooksLabel = "Guard.HooksLabel";
        internal const string GuardSkipActiveListScan = "Guard.SkipActiveListScan";
        internal const string GuardSkipScanBadge = "Guard.SkipScanBadge";
        internal const string GuardNoteLabel = "Guard.NoteLabel";
        internal const string GuardAddEntry = "Guard.AddEntry";
        internal const string GuardDeleteEntry = "Guard.DeleteEntry";
        internal const string GuardStatusActive = "Guard.Status.Active";
        internal const string GuardStatusInactive = "Guard.Status.Inactive";
        internal const string GuardStatusManualOverride = "Guard.Status.ManualOverride";
        internal const string GuardManualOverrideDetail = "Guard.ManualOverride.Detail";
        internal const string GuardRefreshOfficial = "Guard.RefreshOfficial";
        internal const string GuardRefreshOfficialFetching = "Guard.RefreshOfficial.Fetching";
        internal const string GuardRefreshOfficialCooldown = "Guard.RefreshOfficial.Cooldown";
        internal const string GuardRefreshOfficialTip = "Guard.RefreshOfficial.Tip";
        internal const string MsgGuardAddedUser = "Msg.Guard.AddedUser";
        internal const string MsgGuardRemoved = "Msg.Guard.Removed";
        internal const string MsgGuardSynced = "Msg.Guard.Synced";
        internal const string MsgGuardFetchUpdated = "Msg.Guard.FetchUpdated";
        internal const string MsgGuardFetchUpToDate = "Msg.Guard.FetchUpToDate";
        internal const string MsgGuardFetchFailed = "Msg.Guard.FetchFailed";

        internal const string IpcOverridesTitle = "IpcOverrides.Title";
        internal const string IpcOverridesTitleCount = "IpcOverrides.TitleCount";
        internal const string IpcOverridesEmpty = "IpcOverrides.Empty";
        internal const string IpcOverridesClearAll = "IpcOverrides.ClearAll";
        internal const string IpcOverridesClearAllTip = "IpcOverrides.ClearAll.Tip";
        internal const string IpcOverridesClearTagTip = "IpcOverrides.ClearTag.Tip";
        internal const string IpcOverrideEnabledOn = "IpcOverrides.EnabledOn";
        internal const string IpcOverrideEnabledOff = "IpcOverrides.EnabledOff";
        internal const string IpcOverridePreset = "IpcOverrides.Preset";
        internal const string GroupTreeEffectiveVolume = "Group.TreeEffectiveVolume";
        internal const string GroupTreeOverrideVolume = "Group.TreeOverrideVolume";
        internal const string IpcOverridePriority = "IpcOverrides.Priority";
        internal const string MsgIpcOverridesClearedTag = "Msg.IpcOverrides.ClearedTag";
        internal const string MsgIpcOverridesClearedAll = "Msg.IpcOverrides.ClearedAll";

        internal const string PresetLabel = "Preset.Label";
        internal const string PresetEmpty = "Preset.Empty";
        internal const string PresetTip = "Preset.Tip";
        internal const string PresetNew = "Preset.New";
        internal const string PresetNewTip = "Preset.New.Tip";
        internal const string PresetCopy = "Preset.Copy";
        internal const string PresetCopyTip = "Preset.Copy.Tip";
        internal const string PresetDelete = "Preset.Delete";
        internal const string PresetDeleteTip = "Preset.Delete.Tip";
        internal const string PresetDeleteDisabledTip = "Preset.Delete.DisabledTip";

        internal const string SearchLabel = "Search.Label";
        internal const string SearchHint = "Search.Hint";
        internal const string BtnClearSearch = "Btn.ClearSearch";
        internal const string BtnClearFilter = "Btn.ClearFilter";

        internal const string GroupTitle = "Group.Title";
        internal const string GroupDragHint = "Group.DragHint";
        internal const string GroupDragMoving = "Group.DragMoving";
        internal const string GroupDropBefore = "Group.Drop.Before";
        internal const string GroupDropInto = "Group.Drop.Into";
        internal const string GroupDropAfter = "Group.Drop.After";
        internal const string GroupDropRootHover = "Group.Drop.RootHover";
        internal const string GroupDropRoot = "Group.DropRoot";
        internal const string GroupNewRoot = "Group.NewRoot";
        internal const string GroupSelectHint = "Group.SelectHint";
        internal const string GroupRename = "Group.Rename";
        internal const string GroupDelete = "Group.Delete";
        internal const string GroupNewChild = "Group.NewChild";
        internal const string GroupParent = "Group.Parent";
        internal const string GroupRemoveParent = "Group.RemoveParent";
        internal const string GroupVolume = "Group.Volume";
        internal const string GroupRefresh = "Group.Refresh";
        internal const string GroupRefreshTip = "Group.Refresh.Tip";
        internal const string GroupVolumeSliderTip = "Group.VolumeSlider.Tip";
        internal const string GroupPatterns = "Group.Patterns";
        internal const string GroupPatternsHint = "Group.Patterns.Hint";
        internal const string GroupPatternsEmpty = "Group.Patterns.Empty";
        internal const string GroupAddPattern = "Group.AddPattern";
        internal const string GroupSounds = "Group.Sounds";
        internal const string GroupSoundsEmpty = "Group.Sounds.Empty";
        internal const string GroupCtxNewChild = "Group.Ctx.NewChild";
        internal const string GroupCtxRemoveParent = "Group.Ctx.RemoveParent";
        internal const string GroupCtxDelete = "Group.Ctx.Delete";
        internal const string GroupKeepOne = "Group.KeepOne";
        internal const string GroupColor = "Group.Color";
        internal const string GroupColorTip = "Group.Color.Tip";
        internal const string GroupOverrideColor = "Group.OverrideColor";
        internal const string GroupOverrideColorTip = "Group.OverrideColor.Tip";
        internal const string GroupSyncColor = "Group.SyncColor";
        internal const string GroupSyncColorTip = "Group.SyncColor.Tip";
        internal const string GroupColorChildHint = "Group.Color.ChildHint";
        internal const string GroupScaleByFather = "Group.ScaleByFather";
        internal const string GroupScaleByFatherTip = "Group.ScaleByFather.Tip";
        internal const string GroupHideFromMonitor = "Group.HideFromMonitor";
        internal const string GroupHideFromMonitorTip = "Group.HideFromMonitor.Tip";

        internal const string PathAliasHeader = "PathAlias.Header";
        internal const string PathAliasHint = "PathAlias.Hint";

        internal const string CtxAddToGroup = "Ctx.AddToGroup";
        internal const string CtxAddPattern = "Ctx.AddPattern";
        internal const string CtxIndividualVol = "Ctx.IndividualVol";
        internal const string CtxFixPath = "Ctx.FixPath";
        internal const string CtxFixPathTip = "Ctx.FixPath.Tip";
        internal const string CtxCopyPath = "Ctx.CopyPath";
        internal const string CtxNeedGroup = "Ctx.NeedGroup";

        internal const string BtnEdit = "Btn.Edit";
        internal const string BtnDelete = "Btn.Delete";
        internal const string BtnRemove = "Btn.Remove";
        internal const string BtnSave = "Btn.Save";
        internal const string BtnCancel = "Btn.Cancel";
        internal const string BtnCreate = "Btn.Create";
        internal const string BtnConfirm = "Btn.Confirm";
        internal const string BtnAdd = "Btn.Add";
        internal const string BtnReset = "Btn.Reset";

        internal const string PopupNewGroup = "Popup.NewGroup";
        internal const string PopupRenameGroup = "Popup.RenameGroup";
        internal const string PopupGroupName = "Popup.GroupName";
        internal const string PopupParentRoot = "Popup.ParentRoot";
        internal const string PopupParent = "Popup.Parent";
        internal const string PopupAddPattern = "Popup.AddPattern";
        internal const string PopupEditPattern = "Popup.EditPattern";
        internal const string PopupGlobPattern = "Popup.GlobPattern";
        internal const string PopupAddPatternHint = "Popup.AddPattern.Hint";
        internal const string PopupEditSoundPath = "Popup.EditSoundPath";
        internal const string PopupOriginalPath = "Popup.OriginalPath";
        internal const string PopupNewPath = "Popup.NewPath";
        internal const string PopupVolumeInput = "Popup.VolumeInput";
        internal const string PopupVolumePct = "Popup.VolumePct";
        internal const string PopupVolumePctHint = "Popup.VolumePct.Hint";
        internal const string PopupIndividualVol = "Popup.IndividualVol";
        internal const string PopupIndividualVolTip = "Popup.IndividualVol.Tip";
        internal const string PopupFixPath = "Popup.FixPath";
        internal const string PopupFixPathIntro = "Popup.FixPath.Intro";
        internal const string PopupAliasFrom = "Popup.AliasFrom";
        internal const string PopupAliasTo = "Popup.AliasTo";
        internal const string PopupAliasHint = "Popup.AliasHint";

        internal const string PopupNewPreset = "Popup.NewPreset";
        internal const string PopupNewPresetIntro = "Popup.NewPreset.Intro";
        internal const string PopupPresetName = "Popup.PresetName";
        internal const string PopupPresetNameInvalid = "Popup.PresetName.Invalid";
        internal const string PopupCopyPreset = "Popup.CopyPreset";
        internal const string PopupCopyFrom = "Popup.CopyFrom";
        internal const string PopupCopyPresetName = "Popup.CopyPresetName";
        internal const string PopupDeletePreset = "Popup.DeletePreset";
        internal const string PopupDeletePresetConfirm = "Popup.DeletePreset.Confirm";
        internal const string PopupKeepOnePreset = "Popup.KeepOnePreset";

        internal const string PresetDefaultName = "Preset.DefaultName";
        internal const string PresetNewName = "Preset.NewName";
        internal const string PresetCopySuffix = "Preset.CopySuffix";
        internal const string GroupNewDefault = "Group.NewDefault";

        internal const string MsgRefreshedAll = "Msg.RefreshedAll";
        internal const string MsgClearedCache = "Msg.ClearedCache";
        internal const string MsgRefreshedGroup = "Msg.RefreshedGroup";
        internal const string MsgSwitchedPreset = "Msg.SwitchedPreset";
        internal const string MsgCreatedPreset = "Msg.CreatedPreset";
        internal const string MsgCopiedPreset = "Msg.CopiedPreset";
        internal const string MsgDeletedPreset = "Msg.DeletedPreset";
        internal const string MsgRemovedParent = "Msg.RemovedParent";
        internal const string MsgMovedRoot = "Msg.MovedRoot";
        internal const string MsgMovedInto = "Msg.MovedInto";
        internal const string MsgReordered = "Msg.Reordered";
        internal const string MsgAddedSound = "Msg.AddedSound";
        internal const string MsgAddedPattern = "Msg.AddedPattern";
        internal const string MsgSavedAlias = "Msg.SavedAlias";

        internal const string SupportKofi = "Support.Kofi";
        internal const string SupportKofiTip = "Support.Kofi.Tip";
        internal const string ChangelogTitle = "Changelog.Title";
        internal const string ChangelogBody = "Changelog.Body";

        internal const string BuiltinJob = "Builtin.Job";
        internal const string BuiltinBgm = "Builtin.Bgm";
        internal const string BuiltinEnv = "Builtin.Env";
        internal const string BuiltinBattle = "Builtin.Battle";
        internal const string BuiltinUi = "Builtin.Ui";

        internal const string DefaultGroupBattleRoot = "DefaultGroup.BattleRoot";
        internal const string DefaultGroupEnvRoot = "DefaultGroup.EnvRoot";
        internal const string DefaultGroupWeaponSkill = "DefaultGroup.WeaponSkill";
        internal const string DefaultGroupWeaponSkillDesc = "DefaultGroup.WeaponSkill.Desc";
        internal const string DefaultGroupMagic = "DefaultGroup.Magic";
        internal const string DefaultGroupBuff = "DefaultGroup.Buff";
        internal const string DefaultGroupWeapon = "DefaultGroup.Weapon";
        internal const string DefaultGroupBattleVoice = "DefaultGroup.BattleVoice";
        internal const string DefaultGroupFootstep = "DefaultGroup.Footstep";
        internal const string DefaultGroupCloth = "DefaultGroup.Cloth";
        internal const string DefaultGroupUnknown = "DefaultGroup.Unknown";

        internal const string PresetCurrent = "Preset.Current";

        internal const string VolumeSilent = "Volume.Silent";
        internal const string VolumeRangeNormal = "Volume.RangeNormal";
        internal const string VolumeRangeExpert = "Volume.RangeExpert";
        internal const string VolumeRangeDebugExtreme = "Volume.RangeDebugExtreme";
        internal const string VolumeAtCap = "Volume.AtCap";
        internal const string VolumeLinearTip = "Volume.LinearTip";
        internal const string VolumeAbove100Tip = "Volume.Above100Tip";
        internal const string VolumeMaxBadge = "Volume.MaxBadge";
        internal const string VolumeApproxBadge = "Volume.ApproxBadge";
        internal const string VolumeBoostBadge = "Volume.BoostBadge";

        internal const string ClassifyJobWar = "Classify.JobWar";
        internal const string ClassifyJobSam = "Classify.JobSam";
        internal const string ClassifyEnvWind = "Classify.EnvWind";
        internal const string ClassifyEnvRain = "Classify.EnvRain";
        internal const string ClassifyEnvFoot = "Classify.EnvFoot";
        internal const string ClassifyEnvAmbient = "Classify.EnvAmbient";
        internal const string ClassifyUncategorized = "Classify.Uncategorized";
    }

    private static Configuration? _config;

    internal static void Bind(Configuration config)
    {
        _config = config;
    }

    internal static string Get(string key)
    {
        var culture = ResolveCulture();
        if (LocStrings.Zh.TryGetValue(key, out var zh)
            && LocStrings.En.TryGetValue(key, out var en))
        {
            return culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? zh : en;
        }

        return key;
    }

    internal static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    internal static string HookState(bool active)
    {
        return active ? Get(Keys.HookOn) : Get(Keys.HookMissing);
    }

    internal static string PlayingTag()
    {
        return Get(Keys.MonitorPlayingTag);
    }

    private static string ResolveCulture()
    {
        var mode = _config?.UiLanguage ?? LanguageMode.System;
        return mode switch
        {
            LanguageMode.Chinese => "zh-CN",
            LanguageMode.English => "en-US",
            _ => CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? "zh-CN"
                : "en-US",
        };
    }
}
