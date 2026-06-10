using static SoundMixer.Localization.Loc.Keys;

namespace SoundMixer.Localization;

internal static class LocStrings
{
    internal static readonly Dictionary<string, string> Zh = new()
    {
        [WindowTitle] = "SoundMixer - 音效混音器",
        [CommandHelp] = "打开 SoundMixer 音效混音器配置界面",

        [TabMain] = "混音器",
        [TabChangelog] = "更新日志",
        [TabBlacklist] = "黑名单",
        [TabAdvancedSettings] = "高级设置",
        [TabSoundBlacklist] = "音效黑名单",
        [TabActionGuards] = "行动守卫",
        [TabDebug] = "调试",
        [AdvancedTabHint] = "音效黑名单与行动守卫等进阶选项。",

        [LangLabel] = "语言",
        [LangSystem] = "跟随系统",
        [LangChinese] = "中文",
        [LangEnglish] = "English",

        [StatusLabel] = "状态",
        [StatusEnabled] = "启用",
        [StatusDisabled] = "禁用",
        [StatusIpcBadge] = "(IPC)",
        [StatusIpcTip] = "当前存在外部插件设置的临时覆盖，登出或手动清除后恢复已保存配置",
        [BtnEnable] = "启用",
        [BtnDisable] = "禁用",
        [BtnRefreshAll] = "刷新所有音效",
        [BtnRefreshAllTip] = "立即将当前音量规则应用到所有正在播放的音效\n适用于 BGM、环境音等持续播放、不会重新触发的音频\n修改分组音量后可点此立即生效",
        [BtnClearCache] = "清除缓存",
        [BtnClearCacheTip] = "清除插件运行时追踪与音量缓存，并尽量恢复被改写的 SoundData 池节点\n不修改已保存的分组配置；UI 音效异常时可点此，无需重开游戏",
        [ExpertMode] = "专家模式 (最大350%)",
        [ExpertModeTip] = "专家模式允许设置 200% - 350%\n实测引擎听感上限约在 300% - 350% 区间\n超过 350% 不会再更响，插件会自动钳制\n200%=+6dB, 350%≈+10.9dB",
        [Monitoring] = "实时监听",
        [MonitoringTip] = "在顶部面板显示最近播放的音效路径与匹配分组",
        [TrackedScd] = "已追踪 SCD: {0}",

        [HookStatusLine] = "音频钩子: 音量写入={0} | 音量读取={1} | BGM={2} | 环境天气={3}",
        [HookStatusTip] = "音量写入/读取: 实时调节音效音量\nBGM: 背景音乐播放钩子\n环境天气: 天气与环境持续音钩子\n「未找到」表示当前游戏版本签名未匹配",
        [HookOn] = "已启用",
        [HookMissing] = "未找到",

        [MonitorTitle] = ">> 最近播放的音效 (右键操作)",
        [MonitorHideMatched] = "隐藏已匹配规则",
        [MonitorHideMatchedTip] = "隐藏已被插件规则处理的音效\n包括: 已加入分组、已设独立音量、或被路径模式 (Glob) 匹配\n开启后便于集中查看尚未配置的音效",
        [MonitorHideKeywords] = "隐藏关键词",
        [MonitorHideKeywordsTip] = "路径或分组名包含关键词的音效将被隐藏\n多个关键词用半角逗号分隔，例如: foot,voice,bgm\n匹配不区分大小写，可与左侧开关叠加使用",
        [MonitorHint] = "保留最近 {0} 条记录，列表最多显示 {1} 条 (筛选后在全部记录中选取)。BGM/环境音等持续播放的音效会标 {2}；路径后为拦截 hook。右键 →「添加到分组」",
        [MonitorHookActiveScan] = "活跃扫描",
        [MonitorHookPathResolve] = "路径解析",
        [MonitorHookFilter] = "Hook 筛选",
        [MonitorHookFilterTip] = "勾选后仅显示对应 hook 拦截的音效；不勾选任何项则显示全部。可多选。",
        [MonitorHookFilterSelectAll] = "全选",
        [MonitorHookFilterClear] = "清除",
        [MonitorEmpty] = "(无符合筛选条件的音效)",
        [MonitorPlayingTag] = "[播放中]",
        [MonitorPathResolveFailed] = "路径解析失败",
        [MonitorPathResolveFailedDetail] =
            "{0} | SD=0x{1} #{2} active={3} tracked={4} handle={5} scds={6}",

        [DebugTabHint] = "用于 CTD 溯源：开启「手动控制」后可逐个启用/禁用 hook。「已解析」= 签名匹配且 hook 对象存在；「运行中」= 当前是否 Enable。关闭手动控制时恢复插件默认策略。",
        [DebugManualControl] = "手动控制 hook",
        [DebugManualControlTip] =
            "开启后，下方勾选直接决定各 hook 是否运行，并覆写全部行动守卫（含骑乘、外勤机 grace）。\n"
            + "插件启动、禁用插件或关闭窗口时会自动关闭手动模式。",
        [DebugManualControlUnsafeTip] =
            "不安全调试模式：行动守卫已禁用，错误组合可能导致游戏崩溃（CTD）。\n"
            + "仅在需要单独测试 hook 时短暂开启；插件启动、禁用插件或关闭窗口将自动恢复自动管理。",
        [DebugExtremeVolume] = "极限音量调试 (最高 10000%)",
        [DebugExtremeVolumeTip] =
            "仅用于实验：滑条与引擎应用上限提升至 100 倍（10000%）。\n"
            + "极高音量可能失真、爆音或导致 CTD；请戴耳机时格外小心。\n"
            + "关闭后恢复默认 350% 听感钳制。",
        [DebugExtremeVolumeActiveNote] = "极限音量已开启：分组/单独路径最高可设 10000%，引擎写入不再钳制在 350%。",
        [DebugHookAllOn] = "全部启用",
        [DebugHookAllOff] = "全部禁用",
        [DebugHookApply] = "重新应用",
        [DebugHookColumnName] = "Hook",
        [DebugHookColumnResolved] = "已解析",
        [DebugHookColumnRuntime] = "运行中",
        [DebugHookColumnDesired] = "手动启用",
        [DebugHookResolvedYes] = "是",
        [DebugHookResolvedNo] = "否",
        [DebugHookRuntimeOn] = "ON",
        [DebugHookRuntimeOff] = "OFF",
        [DebugPluginDisabledNote] = "插件已禁用：hook 已全部 Disable，请先启用插件。",
        [DebugAutoModeNote] = "当前为自动模式：勾选列不可用；运行中列 = 实际状态（已叠加行动守卫）。",
        [DebugManualModeNote] = "手动模式已开启：行动守卫已禁用。插件启动、禁用插件或关闭窗口将自动恢复自动管理。",
        [DebugGuardMount] = "骑乘 guard",
        [DebugGuardGuideroid] = "外勤机 grace",
        [DebugGuardActive] = "激活",
        [DebugGuardInactive] = "未激活",
        [DebugHookPlaySpecificSound] = "PlaySpecificSound",
        [DebugHookPlaySound] = "PlaySound",
        [DebugHookPlaySoundDanger] =
            "危险：已确认会导致游戏崩溃。默认不加载此 hook，仅可在排查问题时手动开启，风险自负。",
        [DebugHookPlaySystemSound] = "PlaySystemSound",
        [DebugHookPlayClipSound] = "PlayClipSound",
        [DebugHookPlayMovieSound] = "PlayMovieSound",
        [DebugHookPlayBgmSound] = "PlayBGMSound",
        [DebugHookPlayWeatherSound] = "PlayWeatherSound",
        [DebugHookSetVolume] = "SoundData.SetVolume",
        [DebugHookGetVolume] = "SoundData.GetVolume",
        [DebugHookLoadSoundFile] = "LoadSoundFile",
        [DebugHookGetResourceSync] = "GetResource (sync)",
        [DebugHookGetResourceAsync] = "GetResource (async)",

        [BlacklistTabHint] = "按音效路径（关键词 / 完整路径 / Glob）跳过混音、强制刷新与监听扫描。与「行动守卫」分开：此处只管「哪些音」，不管「何时禁 hook」。",
        [BlacklistUserSection] = "我的黑名单",
        [BlacklistUserHint] = "可新建关键词、具体路径或 Glob 规则，并填写备注。右键条目可删除。",
        [BlacklistUserCount] = "自定义条目: {0}",
        [BlacklistUserEmpty] = "（暂无自定义黑名单）",
        [BlacklistOfficialSection] = "官方黑名单（只读）",
        [BlacklistOfficialHint] = "由插件作者在 GitHub 维护，启动时自动同步；此处不可编辑。",
        [BlacklistOfficialMeta] = "官方 rev {0} · {1} 条",
        [BlacklistOfficialEmpty] = "（暂无官方条目）",
        [BlacklistMatchKind] = "匹配类型",
        [BlacklistMatchLabel] = "匹配内容",
        [BlacklistMatchTip] = "关键词: 路径子串\n路径: 完整 SCD 路径（如 sound/battle/etc/foo.scd）\nGlob: 如 **/guideroid**",
        [BlacklistNoteLabel] = "备注",
        [BlacklistAddEntry] = "添加",
        [BlacklistDeleteEntry] = "删除",
        [BlacklistKindKeyword] = "关键词",
        [BlacklistKindPath] = "路径",
        [BlacklistKindGlob] = "Glob",
        [BlacklistRefreshOfficial] = "拉取官方列表",
        [BlacklistRefreshOfficialFetching] = "拉取中…",
        [BlacklistRefreshOfficialCooldown] = "拉取官方列表 ({0}s)",
        [BlacklistRefreshOfficialTip] = "从 GitHub 手动拉取最新 OfficialSoundBlacklist.json（10 秒冷却）",
        [BlacklistCtxAddPath] = "加入我的黑名单（路径）",
        [BlacklistCtxAddGlob] = "加入我的黑名单（Glob）",
        [MsgBlacklistAdded] = "已加入我的黑名单: {0}",
        [MsgBlacklistAddedUser] = "已添加自定义黑名单条目",
        [MsgBlacklistRemoved] = "已删除自定义黑名单条目",
        [MsgBlacklistSynced] = "正在拉取官方黑名单…",
        [MsgBlacklistFetchUpdated] = "官方黑名单已更新 (rev {0}，{1} 条)",
        [MsgBlacklistFetchUpToDate] = "官方黑名单已是最新 (rev {0})",
        [MsgBlacklistFetchFailed] = "拉取官方黑名单失败，请稍后再试",

        [GuardTabHint] = "行动守卫：在特定游戏状态下物理禁用 hook 或跳过危险逻辑（如扫活跃 SoundData 链表）。自定义规则可编辑；官方手册为已确认的 CTD 规避策略，只读。调试 Tab 手动控制 hook 时，行动守卫暂不生效。",
        [GuardUserSection] = "我的行动守卫",
        [GuardUserHint] = "选择触发条件与要禁用的 hook；可勾选跳过活跃链表扫描。右键删除。外勤机 grace 仅出现在官方手册（需配合官方黑名单）。",
        [GuardUserCount] = "自定义规则: {0}",
        [GuardUserEmpty] = "（暂无自定义行动守卫）",
        [GuardOfficialHandbook] = "官方手册（只读）",
        [GuardOfficialHint] = "由插件作者维护，启动时自动同步；触发时自动生效，不可在此编辑。",
        [GuardOfficialMeta] = "官方 rev {0} · {1} 条",
        [GuardOfficialEmpty] = "（暂无官方条目）",
        [GuardTriggerLabel] = "触发行动",
        [GuardTriggerMounted] = "骑乘 / 上下马过渡",
        [GuardTriggerGuideroidGrace] = "外勤机循环音 grace（官方）",
        [GuardTriggerInCombat] = "战斗中",
        [GuardTriggerWeaponsOut] = "拔刀 / 武器拔出",
        [GuardTriggerOccupiedInEvent] = "事件 / 过场占用",
        [GuardTriggerBoundByDuty] = "副本 / 任务区域内",
        [GuardTriggerJumping] = "跳跃中",
        [GuardTriggerCasting] = "咏唱 / 读条中",
        [GuardHooksLabel] = "禁用的 hook（勾选）",
        [GuardSkipActiveListScan] = "跳过活跃 SoundData 链表扫描",
        [GuardSkipScanBadge] = " · 跳过链表扫描",
        [GuardNoteLabel] = "备注",
        [GuardAddEntry] = "添加",
        [GuardDeleteEntry] = "删除",
        [GuardStatusActive] = "生效中",
        [GuardStatusInactive] = "未触发",
        [GuardStatusManualOverride] = "手动 hook 覆盖",
        [GuardManualOverrideDetail] = "调试页「手动控制 hook」已开启，本条守卫不会禁用 hook（触发状态：{0}）。",
        [GuardRefreshOfficial] = "拉取官方手册",
        [GuardRefreshOfficialFetching] = "拉取中…",
        [GuardRefreshOfficialCooldown] = "拉取官方手册 ({0}s)",
        [GuardRefreshOfficialTip] = "从 GitHub 拉取最新 OfficialHookGuards.json（10 秒冷却）",
        [MsgGuardAddedUser] = "已添加自定义行动守卫",
        [MsgGuardRemoved] = "已删除自定义行动守卫",
        [MsgGuardSynced] = "正在拉取官方行动守卫手册…",
        [MsgGuardFetchUpdated] = "官方手册已更新 (rev {0}，{1} 条)",
        [MsgGuardFetchUpToDate] = "官方手册已是最新 (rev {0})",
        [MsgGuardFetchFailed] = "拉取官方手册失败，请稍后再试",

        [IpcOverridesTitle] = ">> 外部插件临时覆盖 (IPC)",
        [IpcOverridesTitleCount] = ">> 外部插件临时覆盖 (IPC) · {0}",
        [IpcOverridesEmpty] = "(当前无外部插件临时设置)",
        [IpcOverridesClearAll] = "清除全部临时覆盖",
        [IpcOverridesClearAllTip] = "移除所有外部插件通过 IPC 设置的临时覆盖，恢复为已保存配置",
        [IpcOverridesClearTagTip] = "点击移除此插件的全部临时覆盖",
        [IpcOverrideEnabledOn] = "启用",
        [IpcOverrideEnabledOff] = "禁用",
        [IpcOverridePreset] = "预设: {0}",
        [GroupTreeEffectiveVolume] = "(实际 {0}%)",
        [GroupTreeOverrideVolume] = "(覆写 {0}%)",
        [IpcOverridePriority] = "优先级 {0}",
        [MsgIpcOverridesClearedTag] = "已清除 {0} 的临时覆盖",
        [MsgIpcOverridesClearedAll] = "已清除全部临时覆盖",

        [PresetLabel] = "预设",
        [PresetEmpty] = "(无预设)",
        [PresetTip] = "切换不同的分组与音量配置方案",
        [PresetNew] = "新建",
        [PresetNewTip] = "创建空白预设（含一个默认分组）",
        [PresetCopy] = "复制",
        [PresetCopyTip] = "复制当前预设",
        [PresetDelete] = "删除",
        [PresetDeleteTip] = "删除当前预设（需确认）",
        [PresetDeleteDisabledTip] = "至少保留一个预设",

        [SearchLabel] = "搜索",
        [SearchHint] = "按名称筛选左侧分组树、分组详情与监听列表中的音效（不区分大小写）",
        [BtnClearSearch] = "清除搜索",
        [BtnClearFilter] = "清除筛选",

        [GroupTitle] = "分组",
        [GroupDragHint] = "拖到行上部/下部排序 | 拖到行中部放入子分组 | 拖到根级区域移出",
        [GroupDragMoving] = "移动: {0}",
        [GroupDropBefore] = "上 · 排在前面",
        [GroupDropInto] = "中 · 放入子分组",
        [GroupDropAfter] = "下 · 排在后面",
        [GroupDropRootHover] = "根级 · 移出父分组",
        [GroupDropRoot] = "  拖放到此处 → 移出父分组 (根级)",
        [GroupNewRoot] = "+ 新建根分组",
        [GroupSelectHint] = "选择一个分组以查看详情",
        [GroupRename] = "重命名",
        [GroupDelete] = "删除分组",
        [GroupNewChild] = "+ 子分组",
        [GroupParent] = "父分组: {0}",
        [GroupRemoveParent] = "移出父分组",
        [GroupVolume] = "分组音量",
        [GroupRefresh] = "刷新此分组",
        [GroupRefreshTip] = "仅刷新匹配此分组（含子分组）的正在播放音效\n适用于 BGM、环境音等持续播放的音频",
        [GroupVolumeSliderTip] = "拖动调节该分组下所有匹配音效的线性倍率\n范围: 0% - {0}%  |  括号内为约 dB\n实测听感上限约 350%，超过后不会再更响\n双击滑条可手动输入精确百分比",
        [GroupPatterns] = "路径模式 (Glob)",
        [GroupPatternsHint] = "例: **/job/war/**  或  **/env/rain/**",
        [GroupPatternsEmpty] = "(无路径模式 - 从监听面板右键添加)",
        [GroupAddPattern] = "+ 添加路径模式",
        [GroupSounds] = "单独指定的音效",
        [GroupSoundsEmpty] = "(从监听面板右键添加具体音效)",
        [GroupCtxNewChild] = "新建子分组",
        [GroupCtxRemoveParent] = "移出父分组",
        [GroupCtxDelete] = "删除分组",
        [GroupKeepOne] = "至少保留一个分组",
        [GroupColor] = "分组颜色",
        [GroupColorTip] = "仅根级分组可设置；用于分组树与监听日志中的名称颜色",
        [GroupOverrideColor] = "显示颜色",
        [GroupOverrideColorTip] = "子分组在分组树与监听日志中的名称颜色（与根级的「分组颜色」独立）",
        [GroupSyncColor] = "同步颜色",
        [GroupSyncColorTip] = "将当前「显示颜色」同步为父级分组当前正在使用的颜色（根级取「分组颜色」，子级取「显示颜色」）",
        [GroupColorChildHint] = "子分组不支持自定义颜色",
        [GroupScaleByFather] = "继承父组音量",
        [GroupScaleByFatherTip] = "勾选时本分组匹配的音效会叠乘父级及祖先分组音量；取消后仅使用本分组自身音量\n默认开启",
        [GroupHideFromMonitor] = "从监听日志隐藏此分组",
        [GroupHideFromMonitorTip] = "隐藏匹配此分组（含子分组）的音效，不在上方实时监听列表中显示\n默认关闭",

        [PathAliasHeader] = "路径修正 ({0})",
        [PathAliasHint] = "全局映射：unknown/指针 → 真实 SCD 路径。在上方监听列表右键添加。",

        [CtxAddToGroup] = "添加到分组",
        [CtxAddPattern] = "添加路径模式到分组",
        [CtxIndividualVol] = "设置独立音量...",
        [CtxFixPath] = "修正路径...",
        [CtxFixPathTip] = "仅当路径显示为 unknown/... 时需要\n将内存指针映射为真实 SCD 路径，修正后分组与音量才会生效",
        [CtxCopyPath] = "复制路径",
        [CtxNeedGroup] = "(请先在左侧创建分组)",

        [BtnEdit] = "编辑",
        [BtnDelete] = "删除",
        [BtnRemove] = "移除",
        [BtnSave] = "保存",
        [BtnCancel] = "取消",
        [BtnCreate] = "创建",
        [BtnConfirm] = "确定",
        [BtnAdd] = "添加",
        [BtnReset] = "重置",

        [PopupNewGroup] = "新建分组",
        [PopupRenameGroup] = "重命名分组",
        [PopupGroupName] = "分组名称",
        [PopupParentRoot] = "父分组: (根级)",
        [PopupParent] = "父分组: {0}",
        [PopupAddPattern] = "添加路径模式",
        [PopupEditPattern] = "编辑路径模式",
        [PopupGlobPattern] = "Glob 模式",
        [PopupAddPatternHint] = "例: **/footstep/**  匹配所有脚步声",
        [PopupEditSoundPath] = "编辑音效路径",
        [PopupOriginalPath] = "原路径: {0}",
        [PopupNewPath] = "新路径",
        [PopupVolumeInput] = "输入音量",
        [PopupVolumePct] = "输入音量百分比 (0 - {0})",
        [PopupVolumePctHint] = "支持小数，例如 37.5 表示 37.5%",
        [PopupIndividualVol] = "设置独立音量",
        [PopupIndividualVolTip] = "仅对此音效路径生效，优先级高于分组音量\n范围: 0% - {0}%\n双击滑条可手动输入精确百分比",
        [PopupFixPath] = "修正路径",
        [PopupFixPathIntro] = "用于 unknown/内存指针 路径。填写真实 .scd 路径后，分组与音量规则才能匹配到该音效。",
        [PopupAliasFrom] = "原路径 (unknown/...)",
        [PopupAliasTo] = "真实 SCD 路径",
        [PopupAliasHint] = "例: sound/xxx/yyy.scd",

        [PopupNewPreset] = "新建预设",
        [PopupNewPresetIntro] = "创建空白预设，包含一个默认分组。当前预设会先自动保存。",
        [PopupPresetName] = "预设名称",
        [PopupPresetNameInvalid] = "名称不能为空或重复",
        [PopupCopyPreset] = "复制预设",
        [PopupCopyFrom] = "复制自: {0}",
        [PopupCopyPresetName] = "新预设名称",
        [PopupDeletePreset] = "删除预设",
        [PopupDeletePresetConfirm] = "确定删除预设「{0}」吗？此操作不可撤销。",
        [PopupKeepOnePreset] = "至少保留一个预设",

        [PresetDefaultName] = "默认",
        [PresetNewName] = "新预设",
        [PresetCopySuffix] = " 副本",
        [GroupNewDefault] = "新分组",

        [MsgRefreshedAll] = "已刷新 {0} 个正在播放的音效",
        [MsgClearedCache] = "已清除缓存（释放 {0} 个追踪，刷新 {1} 个音效）",
        [MsgRefreshedGroup] = "已刷新「{0}」的 {1} 个音效",
        [MsgSwitchedPreset] = "已切换预设: {0}",
        [MsgCreatedPreset] = "已创建预设: {0}",
        [MsgCopiedPreset] = "已复制预设: {0}",
        [MsgDeletedPreset] = "已删除预设: {0}",
        [MsgRemovedParent] = "已将「{0}」移出父分组",
        [MsgMovedRoot] = "已移出到根级分组",
        [MsgMovedInto] = "已将分组放入「{0}」",
        [MsgReordered] = "已调整分组顺序",
        [MsgAddedSound] = "已将 {0} 添加到「{1}」",
        [MsgAddedPattern] = "已将路径模式 {0} 添加到「{1}」",
        [MsgSavedAlias] = "已保存路径修正: {0} → {1}",

        [SupportKofi] = "在 Ko-fi 支持作者",
        [SupportKofiTip] = "打开 Ko-fi 页面",
        [ChangelogTitle] = "更新日志",
        [ChangelogBody] =
            "v0.2.3.4\n"
            + "· 子组新增「继承父组音量」(ScaleByFather)，默认开启；取消勾选后仅使用本组音量，不受父级影响\n"
            + "· 配置迁移 v9（从旧版 ApplyToChildren 字段继承）\n"
            + "\n"
            + "v0.2.3.3\n"
            + "· 修复父组音量不随子组生效：子组 glob 匹配时沿祖先链叠乘父组音量（战斗/环境等无路径规则的父组）\n"
            + "· 强制音量、监听 log 与分组树「实际音量」使用同一套叠乘逻辑\n"
            + "\n"
            + "v0.2.3.2\n"
            + "· 修复 mod 音乐路径分裂：短路径 sound/*.scd 自动别名到 visual mod 完整路径，监听 log 与强制音量使用同一倍率\n"
            + "· Scds 缓存保留更长 mod 路径；LoadSoundFile 不再用短 FileName 覆盖 GetResource 登记的完整路径\n"
            + "· 修复音量基准污染：LastGameVolume 记录原生播放音量，低音量开播后仍可正确放大到 >100%\n"
            + "· >100% 放大：字段优先写入、BypassVolumeRules、强制刷新时跳过淡入等待\n"
            + "· 调试页：可选极限音量调试（引擎写入上限 10000%）\n"
            + "· PlaySound（手动开启时）：播后从 SoundData 升级路径；含 /music/ 的 mod 路径走 ForceRefresh；放大播放使用 BypassVolumeRules\n"
            + "\n"
            + "v0.2.3.1\n"
            + "· 移除安全模式开关：路径解析与强制音量始终走安全路径（禁止 ISoundData.GetFileName）\n"
            + "· 修复插件加载瞬间 CTD：SoundEnforcement 统一 TryResolveSafeSoundPath，关闭安全模式时不再回退 GetFileName\n"
            + "· 修复嵌套脚步声子组（不开 PlaySound hook）：SoundEnforcement 与监听 log 同源；父子叠乘倍率\n"
            + "· 脚步声：FileName 为 foot/foot 容器时选用 Scds 材质路径；跳过 /27 等 setup 索引，仅缩放 /1 播放节点\n"
            + "· 配置迁移 v8；根因与复盘见仓库 POSTMORTEM_0.2.3.1.md\n"
            + "\n"
            + "v0.2.3.0\n"
            + "· PlaySound 已确认会导致崩溃：默认不加载，调试页显示危险说明；一键全开也不会启用\n"
            + "· 修复 UI 一次性音效在分组音量调整后全部静音；工具栏新增清除缓存\n"
            + "· 外勤机 grace 不再挂起 SetVolume/GetVolume；骑乘 BGM 可正常调节音量\n"
            + "· 分组树仅按自身路径规则显示实际音量；拖动改色仅父级变化时同步子组\n"
            + "· 移除 PlaySound CTD 相关冗余保护；官方行动守卫 rev3\n"
            + "\n"
            + "v0.2.2.31\n"
            + "· 调试手动 hook 覆写行动守卫；ⓘ 提示不安全，离开调试 Tab/关窗/禁用插件时自动关闭\n"
            + "· 移除「忽略外勤机 guard」勾选（手动模式始终跳过全部守卫）\n"
            + "\n"
            + "v0.2.2.30\n"
            + "· PlaySound 对 castlp 路径透传，避免与 PlaySpecificSound 双重拦截导致武士读条音静音\n"
            + "· 调试手动 hook：行动守卫优先于勾选；「忽略外勤机 guard」现已生效\n"
            + "\n"
            + "v0.2.2.29\n"
            + "· 修复 v0.2.2.28 回归：castlp 读条循环音（如武士）在分组音量≠100% 时仍被静音\n"
            + "· SetVolume(0) 仅在真正静音时解除追踪，不再误伤 castlp 淡入初始化\n"
            + "· 淡入完成后可正常应用倍率；PlaySpecificSound 优先用返回值追踪节点\n"
            + "\n"
            + "v0.2.2.28\n"
            + "· 淡入/淡出期间 GetVolume 与强制刷新透传原生音量，不再打断 castlp 淡入与战斗 BGM 淡出\n"
            + "· SetVolume(0) 时解除追踪，防止战斗结束后 BGM 被分组刷新拉回播放\n"
            + "· 拖动 BGM 滑条时跳过正在淡出的曲目\n"
            + "\n"
            + "v0.2.2.27\n"
            + "· 修复武士等读条循环音（castlp）在分组音量≠100% 时仍被静音：PlaySound 期间 SetVolume(0) 不再污染追踪状态\n"
            + "· 原生淡入进行中时跳过强制写字段，淡入结束后由 SetVolume/强制刷新应用倍率\n"
            + "\n"
            + "v0.2.2.26\n"
            + "· 修复战斗 BGM 等异步流媒体未入追踪列表、分组音量不生效的问题\n"
            + "· SetVolume 淡入时不再用 0 音量覆盖已记录的基准音量\n"
            + "\n"
            + "v0.2.2.25\n"
            + "· 不再在每次启动时自动向 BGM 等内置分组补 Glob；默认分组仅首次安装时从嵌入配置导入\n"
            + "· 战技音效分组增加说明：Glob 使用官方路径拼写 weponskill\n"
            + "\n"
            + "v0.2.2.24\n"
            + "· 修复战技读条循环音（如武士 castlp）在分组音量≠100% 时被静音的问题\n"
            + "\n"
            + "v0.2.2.23\n"
            + "· 新增「高级设置」Tab；「音效黑名单」「行动守卫」下沉为其子 Tab\n"
            + "\n"
            + "v0.2.2.22\n"
            + "· 黑名单 Tab 更名为「音效黑名单」；新增「行动守卫」Tab（自定义 + 官方手册）\n"
            + "· 官方手册：骑乘禁用 PlaySound 家族；外勤机 grace 禁用 SetVolume/GetVolume 并跳过链表扫描\n"
            + "\n"
            + "v0.2.2.21\n"
            + "· 修复外勤机 PlaySound CTD：骑乘/坐骑路径透传，SetVolume 未启用时不再从播放 hook 探测链表安装\n"
            + "· 播后不再对 mount-loop 返回的 SoundData 做 TrackPlayPath 字段读取\n"
            + "\n"
            + "v0.2.2.20\n"
            + "· 新增「调试」标签页：可手动逐个启用/禁用 12 个 hook，便于 CTD 溯源\n"
            + "\n"
            + "v0.2.2.19\n"
            + "· 安全模式改为禁止 GetFileName 路径回退；解析失败时在最近播放列表红字显示诊断信息\n"
            + "· 骑乘/安全模式不再挂起或透传音频 hook（外勤机黑名单仍挂起 SetVolume/GetVolume）\n"
            + "\n"
            + "v0.2.2.14\n"
            + "· 修复分组拖拽排序无效（总变成放入子分组）：松手前先判定 Before/After；行上/下缘排序、中部放入\n"
            + "\n"
            + "v0.2.2.13\n"
            + "· 修复 BGM/持续播放音乐拖动滑条后音量不更新：记录播放路径、安全解析路径、实时 enforcement 与刷新\n"
            + "· 启动时自动为内置 BGM 分组补全 **/bgm/** 匹配\n"
            + "\n"
            + "v0.2.2.12\n"
            + "· 修复开始拖动分组时的 NullReferenceException：无效拖拽 payload 时先检查 IsNull 再读取 Data\n"
            + "\n"
            + "v0.2.2.11\n"
            + "· 修复拖动分组排序导致 CTD/无法进游戏：拖拽仅在松手时生效（此前悬停每帧重复移动并 Save）；启动时自动修复损坏的分组层级\n"
            + "\n"
            + "v0.2.2.10\n"
            + "· 修复切换预设后仍按旧预设混音：运行时快照改为读取当前 config；切换时清空 SoundData 追踪并刷新全部活跃音效\n"
            + "\n"
            + "v0.2.2.9\n"
            + "· 修复拖动音量滑条偶发「拒绝访问」崩溃：拖动时仅实时混音，松手后再写配置；Save 失败不再中断 Draw()\n"
            + "\n"
            + "v0.2.2.8\n"
            + "· 修复 BGM 滑条不能实时生效：0.2.2.7 移除 Enable() 后 hook 未再启用，PlayBGM/SetVolume 失效\n"
            + "· RefreshGroupSounds 不再因「任意骑乘」整段跳过；仅跳过 mount loop 链表扫描\n"
            + "\n"
            + "v0.2.2.7\n"
            + "· 修复任意坐骑走几步崩溃（未骑外勤机也会）：骑乘期间不再扫描 ActiveSoundData 链表、SetVolume/GetVolume 不再解析 mount loop 路径\n"
            + "· 安全模式关闭时，播放 hook 仍可混脚步音；仅 volume hook 与链表维护在骑乘期间暂停\n"
            + "· 修复 Enable() 每次 IPC 刷新都会无条件重新 Enable 全部 hook 导致的 hook 闪烁\n"
            + "\n"
            + "v0.2.2.6\n"
            + "· 修复非外勤机坐骑崩溃：外勤机 grace 不再绑定「任意骑乘」；陆行鸟上马会清除残留 grace\n"
            + "· 官方黑名单仅挂起 SetVolume/GetVolume（不再 disable 播放/资源 hook）\n"
            + "\n"
            + "v0.2.2.5\n"
            + "· 收窄官方黑名单：仅外勤机路径触发时挂起 hook（陆行鸟等其它坐骑仍可混音）\n"
            + "· 新安装默认开启安全模式\n"
            + "· IPC 切换预设/改分组音量时，外勤机安全窗口内不再扫描活跃音效链表\n"
            + "\n"
            + "v0.2.2.4\n"
            + "· 外勤机崩溃：骑乘 + 官方 guideroid 黑名单时自动挂起全部音频 hook（无需手动开安全模式）\n"
            + "· Framework 每帧不再触碰 SoundData（跳过 EnforceTrackedVolumes / PruneInactivePointers）\n"
            + "\n"
            + "v0.2.2.3\n"
            + "· 修复外勤机仍崩溃：黑名单 bypass 不再扫描 ActiveSoundData 链表\n"
            + "· 骑乘 + 官方 guideroid 规则期间跳过 SoundData 字段写入与监听链表扫描\n"
            + "· 工具栏显示程序集版本号（便于确认已安装正确 DLL）\n"
            + "\n"
            + "v0.2.2.2\n"
            + "· 骑乘 + 官方 mount-loop 黑名单时物理挂起 SetVolume/GetVolume hook\n"
            + "· GetPathFromSoundData 在骑乘 guard 期间不再解析路径\n"
            + "\n"
            + "v0.2.2.1\n"
            + "· 修复官方黑名单始终显示 rev 0：嵌入/远程 JSON 反序列化（DTO JsonProperty）\n"
            + "· 恢复 TryGetPathFromSoundData；官方同步改为主线程保存，失败日志升级为 Warning\n"
            + "\n"
            + "v0.2.2.0\n"
            + "· 骑乘 hook 挂起改为可选「安全模式」（默认关闭，位于专家模式后）\n"
            + "· 黑名单 Tab 支持手动拉取官方列表（10 秒冷却）\n"
            + "\n"
            + "v0.2.1.9\n"
            + "· 官方黑名单备注支持中英双语（OfficialSoundBlacklist.json notes.zh / notes.en）\n"
            + "\n"
            + "v0.2.1.8\n"
            + "· 黑名单独立标签页：玩家自定义（可编辑+备注）与官方列表（只读）分开显示\n"
            + "· 支持关键词 / 路径 / Glob 三种匹配类型\n"
            + "\n"
            + "v0.2.1.7\n"
            + "· 音效黑名单初版（已改为 0.2.1.8 独立 Tab 设计）\n"
            + "\n"
            + "v0.2.1.6\n"
            + "· 骑乘期间挂起全部音频 hook（播放 / SetVolume / 资源加载）\n"
            + "· 骑乘时跳过 RefreshGroupSounds 活跃音效链表扫描（修复 HeelsDesignLinker IPC 脚步声刷新）\n"
            + "· IPC 触发 Enable 前先同步坐骑 guard\n"
            + "\n"
            + "v0.2.1.5\n"
            + "· 坐骑过渡期间物理卸载 SetVolume/GetVolume vtable hook（非仅 detour 透传）\n"
            + "· 坐骑音效路径触发时提前进入 guard（早于 Mounted 条件旗标）\n"
            + "· 上下马后 5 秒 grace；HeelsDesignLinker 等 IPC 重载 hook 时仍遵守 guard\n"
            + "\n"
            + "v0.2.1.4\n"
            + "· 坐骑 / 上下马过渡期间音频 hook 完全透传，不再缩放或写字段\n"
            + "· SetVolume 在过渡节点上不再跳过 Original 调用\n"
            + "\n"
            + "v0.2.1.3\n"
            + "· 修复外勤机坐骑上启用插件即崩溃：不再对全部活跃音效解析路径 / 调用 GetFileName\n"
            + "· 每帧音量强制仅作用于已跟踪音效；淡出进行中跳过写字段\n"
            + "· 启用插件时不再自动刷新全部正在播放音效\n"
            + "\n"
            + "v0.2.1.2\n"
            + "· 修复上/下坐骑时 SetVolume hook 可能导致的崩溃\n"
            + "· 无效或已释放 SoundData 不再调用 Original / 写字段；淡出（fadeDuration>0）时不强制写字段\n"
            + "· 音量刷新路径不再重入 SetVolume Original\n"
            + "\n"
            + "v0.2.1.1\n"
            + "· 修复实时监听 / 音量强制扫描时访问已释放 native 指针导致的崩溃\n"
            + "· VirtualQuery 内存校验后再解引用；统一安全链表遍历（环检测 + 节点上限）\n"
            + "\n"
            + "v0.2.1.0\n"
            + "· 可拖动调节 UI 分区：上方控制区 / 主内容、分组树 / 详情（Glamourer/OtterGui 分割条实现）\n"
            + "· 记住 TopPanelHeight、LeftPanelWidth，松手后自动保存\n"
            + "· 移动或缩放主窗口时立即保存位置与尺寸\n"
            + "· 分组树展开/折叠状态修改后立即保存\n"
            + "\n"
            + "v0.2.0\n"
            + "· 外部插件 IPC 临时覆盖 API（tag + priority，登出自动清除）\n"
            + "· 可折叠「最近播放」/「IPC 临时覆盖」面板（不可从界面移除）\n"
            + "· 分组树显示父级叠乘有效音量；IPC 覆写项绿色「覆写」标注\n"
            + "· IPC 面板按 tag 列出覆盖，支持清除单项或全部\n"
            + "· 播放 hook 优先 ClientStructs 解析；不可用 hook 合并为一条 Info\n"
            + "· 实时监听扫描崩溃防护；已知问题见 KNOWN_ISSUES.md\n"
            + "· 发行包附带 DotNet.Glob.dll（不再 ILRepack 合并）\n"
            + "\n"
            + "v0.1.0\n"
            + "· 按 SCD 路径精细调节音效音量\n"
            + "· 分组、Glob 路径模式、预设方案\n"
            + "· 嵌套分组与拖拽排序\n"
            + "· BGM / 天气环境音支持与一键刷新\n"
            + "· 实时监听、筛选与路径修正\n"
            + "· 专家模式 (实测听感上限约 350%)\n"
            + "· 中 / 英本地化",

        [BuiltinJob] = "职业技能",
        [BuiltinBgm] = "背景音乐",
        [BuiltinEnv] = "环境音效",
        [BuiltinBattle] = "战斗音效",
        [BuiltinUi] = "UI音效",

        [DefaultGroupBattleRoot] = "战斗",
        [DefaultGroupEnvRoot] = "环境",
        [DefaultGroupWeaponSkill] = "战技音效",
        [DefaultGroupWeaponSkillDesc] = "Glob 使用游戏官方路径拼写 weponskill（非 weaponskill），与 SCD 资源目录一致。",
        [DefaultGroupMagic] = "魔法音效",
        [DefaultGroupBuff] = "Buff音效",
        [DefaultGroupWeapon] = "武器音效",
        [DefaultGroupBattleVoice] = "战斗语音",
        [DefaultGroupFootstep] = "脚步声",
        [DefaultGroupCloth] = "衣服摩擦",
        [DefaultGroupUnknown] = "Unknown",

        [PresetCurrent] = "当前预设",

        [VolumeSilent] = "静音",
        [VolumeRangeNormal] = "0–200%：听感变化最明显",
        [VolumeRangeExpert] = "200–350%：仍有提升，但幅度变小",
        [VolumeRangeDebugExtreme] = "350%–10000%：调试极限模式，听感与稳定性不保证",
        [VolumeAtCap] = "已达实测引擎听感上限（约 350%）",
        [VolumeLinearTip] = "线性 {0}%  约合 {1}",
        [VolumeAbove100Tip] = "音量已放大至 100% 以上 ({0})",
        [VolumeMaxBadge] = "[MAX]",
        [VolumeApproxBadge] = "[~]",
        [VolumeBoostBadge] = "[+]",

        [ClassifyJobWar] = "职业技能/战士",
        [ClassifyJobSam] = "职业技能/武士",
        [ClassifyEnvWind] = "环境音效/风声",
        [ClassifyEnvRain] = "环境音效/雨声",
        [ClassifyEnvFoot] = "环境音效/脚步声",
        [ClassifyEnvAmbient] = "环境音效/氛围",
        [ClassifyUncategorized] = "未分类",
    };

    internal static readonly Dictionary<string, string> En = new()
    {
        [WindowTitle] = "SoundMixer",
        [CommandHelp] = "Open the SoundMixer configuration window",

        [TabMain] = "Mixer",
        [TabChangelog] = "Changelog",
        [TabBlacklist] = "Blacklist",
        [TabAdvancedSettings] = "Advanced Settings",
        [TabSoundBlacklist] = "Sound Blacklist",
        [TabActionGuards] = "Action Guards",
        [TabDebug] = "Debug",
        [AdvancedTabHint] = "Advanced options such as sound blacklist and action guards.",

        [LangLabel] = "Language",
        [LangSystem] = "Follow System",
        [LangChinese] = "中文",
        [LangEnglish] = "English",

        [StatusLabel] = "Status",
        [StatusEnabled] = "Enabled",
        [StatusDisabled] = "Disabled",
        [StatusIpcBadge] = "(IPC)",
        [StatusIpcTip] = "Temporary overrides from other plugins are active. Saved settings return after logout or manual clear.",
        [BtnEnable] = "Enable",
        [BtnDisable] = "Disable",
        [BtnRefreshAll] = "Refresh All Sounds",
        [BtnRefreshAllTip] = "Apply current volume rules to all playing sounds immediately.\nUseful for BGM and ambient loops that do not re-trigger.\nClick after changing group volume for instant effect.",
        [BtnClearCache] = "Clear Cache",
        [BtnClearCacheTip] = "Clear runtime tracking and volume caches, and restore pooled SoundData nodes when possible.\nDoes not change saved group settings. Use when UI sounds glitch instead of restarting the game.",
        [ExpertMode] = "Expert Mode (max 350%)",
        [ExpertModeTip] = "Expert mode allows 200% - 350% linear gain.\nAudible engine cap is around 300% - 350%.\nValues above 350% are clamped.\n200% = +6 dB, 350% ≈ +10.9 dB",
        [Monitoring] = "Live Monitor",
        [MonitoringTip] = "Show recently played sound paths and matched groups at the top",
        [TrackedScd] = "Tracked SCD: {0}",

        [HookStatusLine] = "Audio hooks: SetVolume={0} | GetVolume={1} | BGM={2} | Weather={3}",
        [HookStatusTip] = "SetVolume / GetVolume: real-time volume scaling hooks.\nBGM: background music play hook.\nWeather: weather & ambient loop hook.\n\"Not found\" means the signature did not match this game version.",
        [HookOn] = "On",
        [HookMissing] = "Not found",

        [MonitorTitle] = ">> Recently Played (right-click)",
        [MonitorHideMatched] = "Hide matched rules",
        [MonitorHideMatchedTip] = "Hide sounds already handled by plugin rules.\nIncludes grouped sounds, individual volumes, and Glob matches.\nHelps focus on unconfigured sounds.",
        [MonitorHideKeywords] = "Hide keywords",
        [MonitorHideKeywordsTip] = "Hide sounds whose path or group name contains keywords.\nSeparate multiple keywords with commas, e.g. foot,voice,bgm.\nCase-insensitive; stacks with the toggle on the left.",
        [MonitorHint] = "Keeps the latest {0} entries, shows up to {1} after filters. Looping BGM/ambient sounds are marked {2}; hook name follows each path. Right-click → Add to group.",
        [MonitorHookActiveScan] = "Active scan",
        [MonitorHookPathResolve] = "Path resolve",
        [MonitorHookFilter] = "Hook filter",
        [MonitorHookFilterTip] = "Show only sounds intercepted by the selected hooks. Leave all unchecked to show every entry. Multi-select.",
        [MonitorHookFilterSelectAll] = "Select all",
        [MonitorHookFilterClear] = "Clear",
        [MonitorEmpty] = "(No sounds match the current filters)",
        [MonitorPlayingTag] = "[Playing]",
        [MonitorPathResolveFailed] = "Path resolve failed",
        [MonitorPathResolveFailedDetail] =
            "{0} | SD=0x{1} #{2} active={3} tracked={4} handle={5} scds={6}",

        [DebugTabHint] = "Trace CTDs: enable Manual control to toggle each hook. Resolved = signature matched; Runtime = currently Enabled. Turn manual control off to restore default plugin policy.",
        [DebugManualControl] = "Manual hook control",
        [DebugManualControlTip] =
            "When on, checkboxes directly control hooks and override all action guards (mounted, Guideroid grace, etc.).\n"
            + "Manual mode auto-disables on plugin load, plugin disable, or window close.",
        [DebugManualControlUnsafeTip] =
            "Unsafe debug mode: action guards are disabled; bad hook combinations may crash the game (CTD).\n"
            + "Use only for brief isolated tests. Plugin load, plugin disable, or window close restores auto management.",
        [DebugExtremeVolume] = "Extreme volume debug (max 10000%)",
        [DebugExtremeVolumeTip] =
            "Experimental only: raises slider and engine apply cap to 100× (10000%).\n"
            + "Very high gain may clip, distort, or CTD — use headphones with caution.\n"
            + "Turn off to restore the default 350% audible cap.",
        [DebugExtremeVolumeActiveNote] = "Extreme volume active: groups/paths can reach 10000%; engine writes are no longer capped at 350%.",
        [DebugHookAllOn] = "Enable all",
        [DebugHookAllOff] = "Disable all",
        [DebugHookApply] = "Re-apply",
        [DebugHookColumnName] = "Hook",
        [DebugHookColumnResolved] = "Resolved",
        [DebugHookColumnRuntime] = "Runtime",
        [DebugHookColumnDesired] = "Manual",
        [DebugHookResolvedYes] = "Yes",
        [DebugHookResolvedNo] = "No",
        [DebugHookRuntimeOn] = "ON",
        [DebugHookRuntimeOff] = "OFF",
        [DebugPluginDisabledNote] = "Plugin is disabled — all hooks are off. Enable the plugin first.",
        [DebugAutoModeNote] = "Auto mode: manual column disabled; runtime column = actual state (incl. action guards).",
        [DebugManualModeNote] = "Manual mode active: action guards disabled. Plugin load, plugin disable, or window close restores auto management.",
        [DebugGuardMount] = "Mount guard",
        [DebugGuardGuideroid] = "Guideroid grace",
        [DebugGuardActive] = "Active",
        [DebugGuardInactive] = "Inactive",
        [DebugHookPlaySpecificSound] = "PlaySpecificSound",
        [DebugHookPlaySound] = "PlaySound",
        [DebugHookPlaySoundDanger] =
            "Danger: confirmed to crash the game. This hook is not loaded by default; enable manually only for diagnosis at your own risk.",
        [DebugHookPlaySystemSound] = "PlaySystemSound",
        [DebugHookPlayClipSound] = "PlayClipSound",
        [DebugHookPlayMovieSound] = "PlayMovieSound",
        [DebugHookPlayBgmSound] = "PlayBGMSound",
        [DebugHookPlayWeatherSound] = "PlayWeatherSound",
        [DebugHookSetVolume] = "SoundData.SetVolume",
        [DebugHookGetVolume] = "SoundData.GetVolume",
        [DebugHookLoadSoundFile] = "LoadSoundFile",
        [DebugHookGetResourceSync] = "GetResource (sync)",
        [DebugHookGetResourceAsync] = "GetResource (async)",

        [BlacklistTabHint] = "Skip mixing, refresh, and monitor scans by sound path (keyword / exact path / Glob). Separate from Action Guards: this tab is which sounds, not when hooks are disabled.",
        [BlacklistUserSection] = "My blacklist",
        [BlacklistUserHint] = "Add keyword, exact path, or Glob rules with an optional note. Right-click an entry to delete.",
        [BlacklistUserCount] = "Custom entries: {0}",
        [BlacklistUserEmpty] = "(No custom blacklist entries)",
        [BlacklistOfficialSection] = "Official blacklist (read-only)",
        [BlacklistOfficialHint] = "Maintained by the author on GitHub and auto-synced on startup. Not editable here.",
        [BlacklistOfficialMeta] = "Official rev {0} · {1} entries",
        [BlacklistOfficialEmpty] = "(No official entries)",
        [BlacklistMatchKind] = "Match type",
        [BlacklistMatchLabel] = "Match",
        [BlacklistMatchTip] = "Keyword: path substring\nPath: full SCD path (e.g. sound/battle/etc/foo.scd)\nGlob: e.g. **/guideroid**",
        [BlacklistNoteLabel] = "Note",
        [BlacklistAddEntry] = "Add",
        [BlacklistDeleteEntry] = "Delete",
        [BlacklistKindKeyword] = "Keyword",
        [BlacklistKindPath] = "Path",
        [BlacklistKindGlob] = "Glob",
        [BlacklistRefreshOfficial] = "Fetch official list",
        [BlacklistRefreshOfficialFetching] = "Fetching…",
        [BlacklistRefreshOfficialCooldown] = "Fetch official list ({0}s)",
        [BlacklistRefreshOfficialTip] = "Manually fetch OfficialSoundBlacklist.json from GitHub (10s cooldown)",
        [BlacklistCtxAddPath] = "Add to my blacklist (path)",
        [BlacklistCtxAddGlob] = "Add to my blacklist (Glob)",
        [MsgBlacklistAdded] = "Added to my blacklist: {0}",
        [MsgBlacklistAddedUser] = "Custom blacklist entry added",
        [MsgBlacklistRemoved] = "Custom blacklist entry removed",
        [MsgBlacklistSynced] = "Fetching official blacklist…",
        [MsgBlacklistFetchUpdated] = "Official blacklist updated (rev {0}, {1} entries)",
        [MsgBlacklistFetchUpToDate] = "Official blacklist is already up to date (rev {0})",
        [MsgBlacklistFetchFailed] = "Failed to fetch official blacklist; try again later",

        [GuardTabHint] = "Action guards physically disable hooks or skip risky logic (e.g. active SoundData list scans) during specific game states. Custom rules are editable; the official handbook lists author-confirmed CTD mitigations (read-only). Manual hook control in Debug bypasses guards.",
        [GuardUserSection] = "My action guards",
        [GuardUserHint] = "Pick a trigger condition and hooks to disable; optionally skip active list scans. Right-click to delete. Guideroid grace is official-handbook only (requires official blacklist).",
        [GuardUserCount] = "Custom rules: {0}",
        [GuardUserEmpty] = "(No custom action guards)",
        [GuardOfficialHandbook] = "Official handbook (read-only)",
        [GuardOfficialHint] = "Maintained by the author on GitHub; auto-synced on startup and applied when triggered. Not editable here.",
        [GuardOfficialMeta] = "Official rev {0} · {1} entries",
        [GuardOfficialEmpty] = "(No official entries)",
        [GuardTriggerLabel] = "Trigger",
        [GuardTriggerMounted] = "Mounted / mount transition",
        [GuardTriggerGuideroidGrace] = "Guideroid loop grace (official)",
        [GuardTriggerInCombat] = "In combat",
        [GuardTriggerWeaponsOut] = "Weapons drawn",
        [GuardTriggerOccupiedInEvent] = "Event / cutscene occupied",
        [GuardTriggerBoundByDuty] = "Bound by duty / instance",
        [GuardTriggerJumping] = "Jumping",
        [GuardTriggerCasting] = "Casting",
        [GuardHooksLabel] = "Disable hooks",
        [GuardSkipActiveListScan] = "Skip active SoundData list scans",
        [GuardSkipScanBadge] = " · skip list scan",
        [GuardNoteLabel] = "Note",
        [GuardAddEntry] = "Add",
        [GuardDeleteEntry] = "Delete",
        [GuardStatusActive] = "ACTIVE",
        [GuardStatusInactive] = "idle",
        [GuardStatusManualOverride] = "manual hook override",
        [GuardManualOverrideDetail] = "Debug manual hook control is on; this guard will not disable hooks (trigger: {0}).",
        [GuardRefreshOfficial] = "Fetch official handbook",
        [GuardRefreshOfficialFetching] = "Fetching…",
        [GuardRefreshOfficialCooldown] = "Fetch handbook ({0}s)",
        [GuardRefreshOfficialTip] = "Fetch OfficialHookGuards.json from GitHub (10s cooldown)",
        [MsgGuardAddedUser] = "Custom action guard added",
        [MsgGuardRemoved] = "Custom action guard removed",
        [MsgGuardSynced] = "Fetching official action guard handbook…",
        [MsgGuardFetchUpdated] = "Official handbook updated (rev {0}, {1} entries)",
        [MsgGuardFetchUpToDate] = "Official handbook is up to date (rev {0})",
        [MsgGuardFetchFailed] = "Failed to fetch official handbook; try again later",

        [IpcOverridesTitle] = ">> External IPC Overrides",
        [IpcOverridesTitleCount] = ">> External IPC Overrides · {0}",
        [IpcOverridesEmpty] = "(No active temporary overrides from other plugins)",
        [IpcOverridesClearAll] = "Clear All Temporary Overrides",
        [IpcOverridesClearAllTip] = "Remove all session-only IPC overrides and revert to saved settings",
        [IpcOverridesClearTagTip] = "Click to remove this plugin's temporary overrides",
        [IpcOverrideEnabledOn] = "Enabled",
        [IpcOverrideEnabledOff] = "Disabled",
        [IpcOverridePreset] = "Preset: {0}",
        [GroupTreeEffectiveVolume] = "(effective {0}%)",
        [GroupTreeOverrideVolume] = "(override {0}%)",
        [IpcOverridePriority] = "Priority {0}",
        [MsgIpcOverridesClearedTag] = "Cleared temporary overrides for {0}",
        [MsgIpcOverridesClearedAll] = "Cleared all temporary overrides",

        [PresetLabel] = "Preset",
        [PresetEmpty] = "(No presets)",
        [PresetTip] = "Switch between saved group and volume layouts",
        [PresetNew] = "New",
        [PresetNewTip] = "Create a blank preset with one default group",
        [PresetCopy] = "Copy",
        [PresetCopyTip] = "Duplicate the current preset",
        [PresetDelete] = "Delete",
        [PresetDeleteTip] = "Delete the current preset (confirmation required)",
        [PresetDeleteDisabledTip] = "At least one preset must remain",

        [SearchLabel] = "Search",
        [SearchHint] = "Filter the group tree, group details, and monitor list by name or path (case-insensitive)",
        [BtnClearSearch] = "Clear Search",
        [BtnClearFilter] = "Clear Filter",

        [GroupTitle] = "Groups",
        [GroupDragHint] = "Drop on top/bottom edge to reorder | Drop on middle to nest | Drop on root zone to unnest",
        [GroupDragMoving] = "Moving: {0}",
        [GroupDropBefore] = "Top · insert above",
        [GroupDropInto] = "Middle · nest inside",
        [GroupDropAfter] = "Bottom · insert below",
        [GroupDropRootHover] = "Root · move out of parent",
        [GroupDropRoot] = "  Drop here → move to root level",
        [GroupNewRoot] = "+ New Root Group",
        [GroupSelectHint] = "Select a group to view details",
        [GroupRename] = "Rename",
        [GroupDelete] = "Delete Group",
        [GroupNewChild] = "+ Sub-group",
        [GroupParent] = "Parent: {0}",
        [GroupRemoveParent] = "Remove from Parent",
        [GroupVolume] = "Group Volume",
        [GroupRefresh] = "Refresh Group",
        [GroupRefreshTip] = "Refresh only sounds matching this group (including sub-groups).\nUseful for looping BGM and ambient audio.",
        [GroupVolumeSliderTip] = "Adjust linear gain for all sounds matched by this group.\nRange: 0% - {0}%  |  approximate dB in parentheses.\nAudible cap is around 350%.\nDouble-click the slider to type an exact value.",
        [GroupPatterns] = "Path Patterns (Glob)",
        [GroupPatternsHint] = "e.g. **/job/war/**  or  **/env/rain/**",
        [GroupPatternsEmpty] = "(No patterns - add from the monitor panel)",
        [GroupAddPattern] = "+ Add Path Pattern",
        [GroupSounds] = "Explicit Sounds",
        [GroupSoundsEmpty] = "(Add specific sounds from the monitor panel)",
        [GroupCtxNewChild] = "New Sub-group",
        [GroupCtxRemoveParent] = "Remove from Parent",
        [GroupCtxDelete] = "Delete Group",
        [GroupKeepOne] = "At least one group must remain",
        [GroupColor] = "Group Color",
        [GroupColorTip] = "Root groups only; used for names in the group tree and monitor log.",
        [GroupOverrideColor] = "Display Color",
        [GroupOverrideColorTip] = "Label color for sub-groups in the tree and monitor log (separate from the root Group Color).",
        [GroupSyncColor] = "Sync Color",
        [GroupSyncColorTip] = "Copy the parent's current display color into this sub-group (root parent uses Group Color; nested parent uses Display Color).",
        [GroupColorChildHint] = "Sub-groups cannot use custom colors.",
        [GroupScaleByFather] = "Scale by parent volume",
        [GroupScaleByFatherTip] = "When on, sounds matching this group also multiply parent and ancestor group volumes.\nWhen off, only this group's own volume applies.\nOn by default.",
        [GroupHideFromMonitor] = "Hide from monitor log",
        [GroupHideFromMonitorTip] = "Hide sounds matching this group (including sub-groups) from the live monitor above.\nOff by default.",

        [PathAliasHeader] = "Path Fixes ({0})",
        [PathAliasHint] = "Global mapping: unknown/pointer → real SCD path. Add via monitor right-click.",

        [CtxAddToGroup] = "Add to Group",
        [CtxAddPattern] = "Add Path Pattern to Group",
        [CtxIndividualVol] = "Set Individual Volume...",
        [CtxFixPath] = "Fix Path...",
        [CtxFixPathTip] = "Only needed when the path shows unknown/...\nMaps a memory pointer to the real SCD path so rules can match.",
        [CtxCopyPath] = "Copy Path",
        [CtxNeedGroup] = "(Create a group on the left first)",

        [BtnEdit] = "Edit",
        [BtnDelete] = "Delete",
        [BtnRemove] = "Remove",
        [BtnSave] = "Save",
        [BtnCancel] = "Cancel",
        [BtnCreate] = "Create",
        [BtnConfirm] = "OK",
        [BtnAdd] = "Add",
        [BtnReset] = "Reset",

        [PopupNewGroup] = "New Group",
        [PopupRenameGroup] = "Rename Group",
        [PopupGroupName] = "Group Name",
        [PopupParentRoot] = "Parent: (root)",
        [PopupParent] = "Parent: {0}",
        [PopupAddPattern] = "Add Path Pattern",
        [PopupEditPattern] = "Edit Path Pattern",
        [PopupGlobPattern] = "Glob Pattern",
        [PopupAddPatternHint] = "e.g. **/footstep/** matches all footstep sounds",
        [PopupEditSoundPath] = "Edit Sound Path",
        [PopupOriginalPath] = "Original: {0}",
        [PopupNewPath] = "New Path",
        [PopupVolumeInput] = "Enter Volume",
        [PopupVolumePct] = "Volume percent (0 - {0})",
        [PopupVolumePctHint] = "Decimals supported, e.g. 37.5 means 37.5%",
        [PopupIndividualVol] = "Individual Volume",
        [PopupIndividualVolTip] = "Applies only to this sound path; overrides group volume.\nRange: 0% - {0}%\nDouble-click the slider to type an exact value.",
        [PopupFixPath] = "Fix Path",
        [PopupFixPathIntro] = "For unknown/memory pointer paths. Enter the real .scd path so grouping and volume rules can match.",
        [PopupAliasFrom] = "Original (unknown/...)",
        [PopupAliasTo] = "Real SCD Path",
        [PopupAliasHint] = "e.g. sound/xxx/yyy.scd",

        [PopupNewPreset] = "New Preset",
        [PopupNewPresetIntro] = "Creates a blank preset with one default group. The current preset is saved first.",
        [PopupPresetName] = "Preset Name",
        [PopupPresetNameInvalid] = "Name cannot be empty or duplicate",
        [PopupCopyPreset] = "Copy Preset",
        [PopupCopyFrom] = "Copy from: {0}",
        [PopupCopyPresetName] = "New Preset Name",
        [PopupDeletePreset] = "Delete Preset",
        [PopupDeletePresetConfirm] = "Delete preset \"{0}\"? This cannot be undone.",
        [PopupKeepOnePreset] = "At least one preset must remain",

        [PresetDefaultName] = "Default",
        [PresetNewName] = "New Preset",
        [PresetCopySuffix] = " Copy",
        [GroupNewDefault] = "New Group",

        [MsgRefreshedAll] = "Refreshed {0} playing sound(s)",
        [MsgClearedCache] = "Cache cleared ({0} tracked released, {1} sound(s) refreshed)",
        [MsgRefreshedGroup] = "Refreshed {1} sound(s) in \"{0}\"",
        [MsgSwitchedPreset] = "Switched preset: {0}",
        [MsgCreatedPreset] = "Created preset: {0}",
        [MsgCopiedPreset] = "Copied preset: {0}",
        [MsgDeletedPreset] = "Deleted preset: {0}",
        [MsgRemovedParent] = "Moved \"{0}\" out of its parent group",
        [MsgMovedRoot] = "Moved to root level",
        [MsgMovedInto] = "Nested group under \"{0}\"",
        [MsgReordered] = "Group order updated",
        [MsgAddedSound] = "Added {0} to \"{1}\"",
        [MsgAddedPattern] = "Added pattern {0} to \"{1}\"",
        [MsgSavedAlias] = "Saved path fix: {0} → {1}",

        [SupportKofi] = "Support on Ko-fi",
        [SupportKofiTip] = "Open Ko-fi page",
        [ChangelogTitle] = "Changelog",
        [ChangelogBody] =
            "v0.2.3.4\n"
            + "· Child groups: ScaleByFather (inherit parent volume, on by default); off = only this group's volume applies\n"
            + "· Config migration v9 from legacy ApplyToChildren\n"
            + "\n"
            + "v0.2.3.3\n"
            + "· Fix parent group volume not applying to child-matched sounds: ancestor chain multiplies parent volumes (battle/environment parents with no globs)\n"
            + "· Enforcement, monitor log, and group-tree effective % use the same stacked math\n"
            + "\n"
            + "v0.2.3.2\n"
            + "· Fix mod music path split: auto-alias short sound/*.scd to full visual mod path; monitor log and enforcement share one multiplier\n"
            + "· Scds cache keeps longer mod path; LoadSoundFile no longer overwrites GetResource full path with short FileName\n"
            + "· Fix volume baseline pollution: LastGameVolume tracks native play volume so >100% boost works after a quiet start\n"
            + "· >100% boost: field-first ApplyEngineVolume, BypassVolumeRules, skip fade-in wait during enforcement\n"
            + "· Debug tab: optional extreme volume debug (up to 10000% engine cap)\n"
            + "· PlaySound when enabled: post-play path upgrade from SoundData; mod /music/ paths use ForceRefresh; boost plays with BypassVolumeRules\n"
            + "\n"
            + "v0.2.3.1\n"
            + "· Removed Safe Mode toggle: path resolve and enforcement always use safe reads (no ISoundData.GetFileName)\n"
            + "· Fix load-time CTD: SoundEnforcement uses TryResolveSafeSoundPath only; no GetFileName fallback when Safe Mode was off\n"
            + "· Fix nested footstep child groups without PlaySound hook: SoundEnforcement matches monitor log; stacked parent+child multipliers\n"
            + "· Footsteps: Scds material path when FileName is foot container; skip setup indices (/27), scale playback /1 only\n"
            + "· Config migration v8; root cause and postmortem in repo POSTMORTEM_0.2.3.1.md\n"
            + "\n"
            + "v0.2.3.0\n"
            + "· PlaySound confirmed to crash the game: not loaded by default; danger notice in Debug tab; All On keeps it off\n"
            + "· Fix UI one-shot sounds muted after group volume changes; toolbar Clear Cache button\n"
            + "· Guideroid grace no longer suspends SetVolume/GetVolume; ride BGM volume applies while mounted\n"
            + "· Group tree shows effective volume from own path rules only; color sync only when parent changes\n"
            + "· Removed redundant PlaySound CTD guards; official action guards rev3\n"
            + "\n"
            + "v0.2.2.31\n"
            + "· Debug manual hooks override action guards; ⓘ unsafe warning; auto-off when leaving tab/window/plugin disable\n"
            + "· Removed Ignore Guideroid guard checkbox (manual mode always bypasses all guards)\n"
            + "\n"
            + "v0.2.2.30\n"
            + "· Passthrough PlaySound for castlp paths; fixes SAM cast-loop mute from double interception with PlaySpecificSound\n"
            + "· Debug manual hooks: action guards override checkboxes; Ignore Guideroid guard now wired\n"
            + "\n"
            + "v0.2.2.29\n"
            + "· Fix v0.2.2.28 regression: cast-loop SFX (e.g. SAM castlp) still muted when group volume ≠ 100%\n"
            + "· SetVolume(0) releases tracking only for genuine silencing, not cast-loop fade-in setup\n"
            + "· Apply multiplier after fade-in completes; PlaySpecificSound tracks via return pointer first\n"
            + "\n"
            + "v0.2.2.28\n"
            + "· Passthrough native volume during fades; fix battle BGM stuck after fade-out when adjusting group volume\n"
            + "· Release tracking on SetVolume(0); skip refresh on silencing/fading sounds\n"
            + "\n"
            + "v0.2.2.27\n"
            + "· Fix cast-loop weapon skills (e.g. SAM castlp) still muted at non-100% volume when SetVolume(0) runs during PlaySound\n"
            + "· Skip field enforcement during native fade; apply gain after fade via SetVolume/refresh\n"
            + "\n"
            + "v0.2.2.26\n"
            + "· Fix battle BGM async streams not tracked and group volume not applied\n"
            + "· SetVolume fade-in no longer overwrites stored base volume with zero\n"
            + "\n"
            + "v0.2.2.25\n"
            + "· Stop auto-patching built-in group globs on startup; defaults only on first install\n"
            + "· Weapon-skill group note: glob uses official weponskill folder spelling\n"
            + "\n"
            + "v0.2.2.24\n"
            + "· Fix muted cast-loop weapon skills (e.g. SAM castlp) when group volume ≠ 100%\n"
            + "\n"
            + "v0.2.2.23\n"
            + "· Add Advanced Settings tab; Sound Blacklist and Action Guards moved under it as sub-tabs\n"
            + "\n"
            + "v0.2.2.22\n"
            + "· Rename blacklist tab to Sound Blacklist; add Action Guards tab (custom + official handbook)\n"
            + "· Official handbook: disable PlaySound family while mounted; Guideroid grace suspends SetVolume/GetVolume + list scans\n"
            + "\n"
            + "v0.2.2.21\n"
            + "· Fix Guideroid PlaySound CTD: passthrough while mounted/on mount paths; no list probe for SetVolume install when SetVolume is off\n"
            + "· Skip post-play TrackPlayPath field reads on mount-loop returns\n"
            + "\n"
            + "v0.2.2.20\n"
            + "· New Debug tab: manually enable/disable each of the 12 hooks for CTD isolation\n"
            + "\n"
            + "v0.2.2.19\n"
            + "· Safe Mode now blocks GetFileName path fallback; failures shown in red in the recent-sounds list with diagnostics\n"
            + "· Mount/safe mode no longer suspend or passthrough audio hooks (Guideroid blacklist still suspends SetVolume/GetVolume)\n"
            + "\n"
            + "v0.2.2.14\n"
            + "· Fix group drag reorder always nesting: capture Before/After before AcceptDragDropPayload; top/bottom edge = reorder, middle = nest\n"
            + "\n"
            + "v0.2.2.13\n"
            + "· Fix BGM/streaming music volume not updating while playing: track play path, safe resolve, live enforce + refresh\n"
            + "· Auto-add **/bgm/** glob to built-in BGM group when missing\n"
            + "\n"
            + "v0.2.2.12\n"
            + "· Fix NullReferenceException when starting a group drag: guard invalid AcceptDragDropPayload with IsNull before reading Data\n"
            + "\n"
            + "v0.2.2.11\n"
            + "· Fix CTD / game won't launch after dragging groups to reorder: drop applies once on release (was repeating every hover frame + Save spam); auto-repair corrupted hierarchy on load\n"
            + "\n"
            + "v0.2.2.10\n"
            + "· Fix old preset still applying after switch: runtime snapshot uses live config for active preset; clear SoundData tracking and refresh all active sounds on switch\n"
            + "\n"
            + "v0.2.2.9\n"
            + "· Fix occasional Access Denied crash while dragging volume sliders: live mix while dragging, save on release; save failures no longer abort Draw()\n"
            + "\n"
            + "v0.2.2.8\n"
            + "· Fix BGM slider not applying live: 0.2.2.7 stopped re-enabling hooks after Enable(); PlayBGM/SetVolume were inactive\n"
            + "· RefreshGroupSounds no longer fully skipped while mounted; only active-list scans are skipped for mount-loop safety\n"
            + "\n"
            + "v0.2.2.7\n"
            + "· Fix all-mount crash after a few steps (without ever riding Guideroid): skip ActiveSoundData list scans and SetVolume/GetVolume path resolution while mounted\n"
            + "· With Safe Mode off, play hooks still mix footsteps; only volume hooks and list maintenance pause while mounted\n"
            + "· Fix Enable() unconditionally re-enabling all hooks on every IPC refresh (hook flicker while mounted)\n"
            + "\n"
            + "v0.2.2.6\n"
            + "· Fix non-Guideroid mount crash: grace no longer latched to any mount; other mount SFX clears stale grace\n"
            + "· Official blacklist suspends SetVolume/GetVolume only (play/resource hooks stay enabled)\n"
            + "\n"
            + "v0.2.2.5\n"
            + "· Narrow official blacklist: hook suspend only after Guideroid paths (other mounts still mixable)\n"
            + "· Safe Mode on by default for new installs\n"
            + "· IPC preset/group refresh skips active-list scans during Guideroid safety window\n"
            + "\n"
            + "v0.2.2.4\n"
            + "· Guideroid crash: auto-suspend ALL audio hooks while mounted with official guideroid blacklist\n"
            + "· Framework tick skips all SoundData maintenance during mount-loop safety\n"
            + "\n"
            + "v0.2.2.3\n"
            + "· Fix Guideroid crash: blacklist bypass no longer scans ActiveSoundData lists\n"
            + "· Skip SoundData field writes and monitor list scans while mounted with official guideroid rules\n"
            + "· Toolbar shows assembly version (verify the correct DLL is installed)\n"
            + "\n"
            + "v0.2.2.2\n"
            + "· Physically suspend SetVolume/GetVolume hooks during mount-loop blacklist\n"
            + "· GetPathFromSoundData returns empty while mount guard is active\n"
            + "\n"
            + "v0.2.2.1\n"
            + "· Fix official blacklist stuck at rev 0: JsonProperty for embedded/remote JSON\n"
            + "· Restore TryGetPathFromSoundData; official sync saves on main thread; failures log as Warning\n"
            + "\n"
            + "v0.2.2.0\n"
            + "· Mount hook suspension is now optional Safe Mode (off by default, after Expert Mode)\n"
            + "· Blacklist tab: manual fetch for official list (10s cooldown)\n"
            + "\n"
            + "v0.2.1.9\n"
            + "· Official blacklist notes: bilingual zh/en in OfficialSoundBlacklist.json\n"
            + "\n"
            + "v0.2.1.8\n"
            + "· Dedicated Blacklist tab: editable user entries (with notes) vs read-only official list\n"
            + "· Keyword / path / Glob match kinds\n"
            + "\n"
            + "v0.2.1.7\n"
            + "· Initial blacklist (superseded by 0.2.1.8 tab design)\n"
            + "\n"
            + "v0.2.1.6\n"
            + "· Suspend all audio hooks while mounted (play + SetVolume + resource)\n"
            + "· Skip RefreshGroupSounds list scans during mount (HeelsDesignLinker footstep IPC)\n"
            + "· Sync mount guard before IPC-driven Enable()\n"
            + "\n"
            + "v0.2.1.5\n"
            + "· Physically disable SetVolume/GetVolume vtable hooks during mount transition (not just detour passthrough)\n"
            + "· Early mount guard when mount SFX paths play (before Mounted/Mounting flags)\n"
            + "· 5s grace after mount/dismount; IPC re-enable respects guard\n"
            + "\n"
            + "v0.2.1.4\n"
            + "· Passthrough audio hooks while mounted and during mount/dismount transitions\n"
            + "· SetVolume no longer skips Original on transitional SoundData nodes\n"
            + "\n"
            + "v0.2.1.3\n"
            + "· Fix Guideroid mount crash when enabling plugin: stop probing every active sound / calling GetFileName\n"
            + "· Per-frame enforcement only for tracked sounds; skip field writes while native fade is running\n"
            + "· Enabling the plugin no longer auto-refreshes all playing sounds\n"
            + "\n"
            + "v0.2.1.2\n"
            + "· Fix mount/dismount crashes in SetVolume hook\n"
            + "· Skip Original/field writes on invalid SoundData; no field poke during native fades (fadeDuration>0)\n"
            + "· Volume refresh no longer re-enters SetVolume Original\n"
            + "\n"
            + "v0.2.1.1\n"
            + "· Fix Framework.Update crashes when scanning freed SoundData native pointers (live monitor / volume enforcement)\n"
            + "· VirtualQuery validation before dereferencing; unified safe linked-list traversal\n"
            + "\n"
            + "v0.2.1.0\n"
            + "· Resizable UI splitters: top controls vs main content, group tree vs details (Glamourer/OtterGui-style handles)\n"
            + "· Persist TopPanelHeight and LeftPanelWidth; save on drag release\n"
            + "· Auto-save main window position/size while moving or resizing\n"
            + "· Auto-save group tree expand/collapse state\n"
            + "\n"
            + "v0.2.0\n"
            + "· External IPC temporary override API (tag + priority, cleared on logout)\n"
            + "· Collapsible Recent / IPC override panels (always visible, not dismissible)\n"
            + "· Group tree shows stacked effective volume; green override labels for IPC\n"
            + "· IPC panel lists overrides by tag with per-tag and clear-all actions\n"
            + "· Play hooks resolve via ClientStructs; unavailable hooks log once as Info\n"
            + "· Live monitor scan crash guards; see KNOWN_ISSUES.md\n"
            + "· Release zip ships DotNet.Glob.dll (no ILRepack merge)\n"
            + "\n"
            + "v0.1.0\n"
            + "· Per-SCD-path volume control\n"
            + "· Groups, Glob patterns, and presets\n"
            + "· Nested groups with drag-and-drop\n"
            + "· BGM / weather-ambient support and refresh actions\n"
            + "· Live monitor, filters, and path fixes\n"
            + "· Expert mode (audible cap around 350%)\n"
            + "· Chinese / English localization",

        [BuiltinJob] = "Job Skills",
        [BuiltinBgm] = "BGM",
        [BuiltinEnv] = "Environment",
        [BuiltinBattle] = "Battle",
        [BuiltinUi] = "UI",

        [DefaultGroupBattleRoot] = "Battle Related",
        [DefaultGroupEnvRoot] = "Environment Related",
        [DefaultGroupWeaponSkill] = "Weapon Skills",
        [DefaultGroupWeaponSkillDesc] = "Glob uses the game's official folder spelling weponskill (not weaponskill), matching SCD paths.",
        [DefaultGroupMagic] = "Magic",
        [DefaultGroupBuff] = "Buffs",
        [DefaultGroupWeapon] = "Weapons",
        [DefaultGroupBattleVoice] = "Battle Voice",
        [DefaultGroupFootstep] = "Footsteps",
        [DefaultGroupCloth] = "Cloth Rustle",
        [DefaultGroupUnknown] = "Unknown",

        [PresetCurrent] = "Current preset",

        [VolumeSilent] = "Silent",
        [VolumeRangeNormal] = "0–200%: most perceptible change",
        [VolumeRangeExpert] = "200–350%: still louder, diminishing returns",
        [VolumeRangeDebugExtreme] = "350%–10000%: debug extreme mode; perception and stability not guaranteed",
        [VolumeAtCap] = "At audible engine cap (~350%)",
        [VolumeLinearTip] = "Linear {0}% ≈ {1}",
        [VolumeAbove100Tip] = "Boosted above 100% ({0})",
        [VolumeMaxBadge] = "[MAX]",
        [VolumeApproxBadge] = "[~]",
        [VolumeBoostBadge] = "[+]",

        [ClassifyJobWar] = "Job/WAR",
        [ClassifyJobSam] = "Job/SAM",
        [ClassifyEnvWind] = "Environment/Wind",
        [ClassifyEnvRain] = "Environment/Rain",
        [ClassifyEnvFoot] = "Environment/Footsteps",
        [ClassifyEnvAmbient] = "Environment/Ambient",
        [ClassifyUncategorized] = "Uncategorized",
    };
}
