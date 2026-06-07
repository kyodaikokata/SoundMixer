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
        [ExpertMode] = "专家模式 (最大350%)",
        [ExpertModeTip] = "专家模式允许设置 200% - 350%\n实测引擎听感上限约在 300% - 350% 区间\n超过 350% 不会再更响，插件会自动钳制\n200%=+6dB, 350%≈+10.9dB",
        [SafeMode] = "安全模式",
        [SafeModeTip] = "开启后：骑乘及上下马过渡期间挂起全部音频 hook，跳过活跃音效链表扫描\n"
            + "关闭后（默认）：骑乘时仍应用音量规则；若在外勤机等坐骑上崩溃，请开启此项或依赖官方黑名单",
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
        [MonitorHint] = "保留最近 {0} 条记录，列表最多显示 {1} 条 (筛选后在全部记录中选取)。BGM/环境音等持续播放的音效会标 {2}。右键 →「添加到分组」",
        [MonitorEmpty] = "(无符合筛选条件的音效)",
        [MonitorPlayingTag] = "[播放中]",

        [BlacklistTabHint] = "黑名单中的音效不会被混音、强制刷新或监听扫描。玩家自定义与官方列表分开维护；官方条目只读。",
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
        [GroupDragHint] = "拖拽排序 | 拖到分组上放入 | 拖到根级区域移出",
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
        [GroupSyncColorTip] = "将显示颜色设为父级分组的「分组颜色」",
        [GroupColorChildHint] = "子分组不支持自定义颜色",
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
            "v0.2.2.0\n"
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
        [ExpertMode] = "Expert Mode (max 350%)",
        [ExpertModeTip] = "Expert mode allows 200% - 350% linear gain.\nAudible engine cap is around 300% - 350%.\nValues above 350% are clamped.\n200% = +6 dB, 350% ≈ +10.9 dB",
        [SafeMode] = "Safe Mode",
        [SafeModeTip] = "When on: suspend all audio hooks while mounted and during mount/dismount transitions; "
            + "skip active-sound list scans.\n"
            + "When off (default): volume rules still apply while mounted. "
            + "Enable if you crash on mounts such as Guideroid, or rely on the official blacklist.",
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
        [MonitorHint] = "Keeps the latest {0} entries, shows up to {1} after filters. Looping BGM/ambient sounds are marked {2}. Right-click → Add to group.",
        [MonitorEmpty] = "(No sounds match the current filters)",
        [MonitorPlayingTag] = "[Playing]",

        [BlacklistTabHint] = "Blacklisted sounds skip mixing, refresh, enforcement, and monitor scans. User and official lists are separate; official entries are read-only.",
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
        [GroupDragHint] = "Drag to reorder | Drop on group to nest | Drop on root to unnest",
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
        [GroupSyncColorTip] = "Set display color to the parent group's Group Color.",
        [GroupColorChildHint] = "Sub-groups cannot use custom colors.",
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
            "v0.2.2.0\n"
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
