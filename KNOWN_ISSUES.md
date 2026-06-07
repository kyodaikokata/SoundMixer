# SoundMixer 已知问题

记录已确认、尚未完全根治的问题。修复后会在此标注状态与版本。

| 字段 | 说明 |
|------|------|
| **发现时间** | 首次在日志/测试中确认的日期 |
| **发现版本** | 当时插件与 Dalamud 版本 |
| **状态** | `开放` / `已缓解` / `已修复` |

---

## 1. 国服客户端 SoundManager 播放钩子签名未匹配

| 项目 | 内容 |
|------|------|
| **发现时间** | 2026-06-06 |
| **发现版本** | SoundMixer **0.1.0**（`0.1.0.0`）· Dalamud API **15** · `Dalamud.CN.NET.Sdk 15.0.0` · 国服 XIVLauncher |
| **状态** | 已缓解（功能降级，签名待随 ClientStructs / 游戏版本更新） |

### 现象

插件加载时出现多条与 `SoundManager` 播放函数相关的警告（旧版日志示例）：

```text
[SoundMixer] SoundMixer: failed to resolve call target for E8 ?? ?? ?? ?? 83 FB ?? 41 BF
…（PlaySound / PlaySystemSound / PlayClipSound / PlayMovieSound / PlayBGMSound / PlayWeatherSound）
```

启用后日志中对应 hook 为 `False`（`PlaySound=False` … `PlayWeather=False`），而 `PlaySpecific=True`、`SetVolume=True` 仍正常。

### 原因

上述 6 个 hook 依赖 **E8 调用点字节签名**（与 [FFXIVClientStructs `SoundManager`](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Sound/SoundManager.cs) 中 `MemberFunction` 一致）。国服客户端经小版本更新后，二进制中对应调用点序列已与当前 Dalamud CN 内置的 ClientStructs / 签名缓存不一致，扫描失败。

这与插件逻辑错误无关，属于 **游戏版本领先于 Dalamud / ClientStructs 签名库** 的常见情况。

### 影响

| 能力 | 是否可用 |
|------|----------|
| 普通 SCD 音效路径混音（`PlaySpecificSound` + `SetVolume` / `GetVolume`） | ✅ 可用 |
| 播放瞬间拦截（`PlaySound` 等 6 个 hook） | ❌ 不可用 |
| BGM / 天气环境音「播放瞬间」注入音量 | ❌ 弱于预期 |
| 正在播放音效的帧扫描监听与 `SetVolume` 强制 | ✅ 可用（见问题 2 的防护） |

### 缓解（0.2.0 代码侧）

- 优先使用 FFXIVClientStructs 已解析的 `MemberFunctionPointers`，`TryScanText` 为备选。
- 不再对每条签名单独刷 Warning；合并为一条 Info，列出未安装的 play hook，并说明核心混音仍可用。
- 待 Dalamud CN / ClientStructs 更新签名后，无需改业务逻辑即可恢复 hook（若指针非空）。

### 根治方向

- 跟进 [FFXIVClientStructs `SoundManager.cs`](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Sound/SoundManager.cs) 与国服实际二进制，更新 6 个 `MemberFunction` 签名；或等待 XIVLauncher CN 自带 ClientStructs 同步。

---

## 2. 实时监听扫描在 Framework.Update 中崩溃

| 项目 | 内容 |
|------|------|
| **发现时间** | 2026-06-06 |
| **发现版本** | SoundMixer **0.1.0**（`0.1.0.0`）· Dalamud API **15** |
| **状态** | **已修复**（**0.2.1.1**：`SoundDataSafety` + `VirtualQuery` 内存校验 + 统一链表遍历） |

### 现象

启用「实时监听」后，Dalamud 日志反复出现：

```text
Exception in event handler "IFramework::Update"
  at SoundMixer.Filter.ScanSingleSoundForMonitoring … Filter.cs
  at SoundMixer.Plugin.OnFrameworkUpdate … Plugin.cs
```

### 原因

`EnforceTrackedVolumes` 每帧扫描游戏内 `SoundData` 链表做监听；在部分时机（加载、切图、音效释放）会读到 **无效或已释放** 的 native 指针（内存地址，非配置路径），触发访问异常。

### 修复

- 新增 `SoundDataSafety`：`VirtualQuery` 校验可读内存后再解引用。
- 所有 `SoundData` 链表遍历统一走 `VisitSoundList`（环检测 + 4096 节点上限 + 逐节点保护）。
- `SoundVolumeTracker` / `GetPathFromSoundData` / 音量强制逻辑在读写前均校验指针。
- `EnforceTrackedVolumes` 保留整体 `try/catch` 作为最后防线。

### 残留风险

极端竞态下可能漏记单条监听日志，不应再导致 `IFramework::Update` 反复报错。

---

## 3. 上/下坐骑时游戏崩溃

| 项目 | 内容 |
|------|------|
| **发现时间** | 2026-06-06 |
| **发现版本** | SoundMixer **0.2.1.1** 及更早 |
| **状态** | **已修复**（**0.2.1.2** 起 SetVolume 安全；**0.2.2.0** 将骑乘 hook 挂起改为可选「安全模式」，**默认关闭**，骑乘期仍可混音） |

### 现象

上坐骑（或下坐骑）时游戏崩溃；禁用 SoundMixer 后不复现。  
**外勤机（Guideroid）** 上：插件一旦启用（含进图已在骑乘、骑乘中点启用）即崩溃；其他坐骑可能正常。

### 原因

1. **0.2.1.1 及更早**：坐骑切换时大量 **带淡出参数的 SetVolume**；hook 在淡出中强制写字段、重入 `Original`，导致 native 崩溃。
2. **0.2.1.2 仍可能触发（外勤机）**：启用后每帧 `EnforceTrackedVolumes` 遍历**全部**活跃 `SoundData` 并解析路径；部分坐骑循环音（外勤机）对 `ISoundData.GetFileName()` 或启用时 `RefreshAllActiveSounds` 全表扫描敏感，引发 **native 访问冲突**（与 fade 是否可见无关）。

### 修复

**0.2.1.2**

- `SetVolume` / `GetVolume` detour：无效指针直接返回；`fadeDuration > 0` 时不 `ApplyFieldVolume`；刷新路径不再重入 `SetVolume Original`。

**0.2.1.3**

- 路径解析仅读 `SoundResourceHandle`，**不再**调用 `GetFileName()` 虚函数。
- 每帧 enforcement **仅**处理已在 `Tracked` 中的音效（经 hook 登记过的），不扫描未知活跃节点。
- enforcement 在检测到 native fade 进行中（`fadeTarget ≠ volume`）时跳过写字段。
- 启用插件时**不再**自动 `RefreshAllActiveSounds`（避免启用瞬间全表探测）。

**0.2.1.4**

- `ConditionFlag.Mounted` / `Mounting` 等激活时，**SetVolume / GetVolume / 播放 hook 全部透传**（不缩放、不写字段、不 enforcement）。
- 上下马后保留约 3 秒 grace，覆盖脚步淡出等尾波 `SetVolume`。
- 过渡中的 `SoundData` 若安全校验失败，仍 **调用 Original**，不再静默跳过（旧逻辑会破坏上马瞬间音频状态）。

**0.2.1.5**

- 坐骑过渡期间 **物理 Disable** `SetVolume` / `GetVolume` vtable hook（游戏直接走原生，不经 detour）。
- 坐骑音效路径（`/mount/`、`se_bt_etc_mount`、`guideroid` 等）播放时 **提前** 进入 guard（早于 `Mounted` 条件旗标）。
- grace 延长至 5 秒；`Framework.Update` 每帧同步 guard 与 hook 状态，HeelsDesignLinker 等 IPC 重载 hook 时仍遵守 guard。

**0.2.1.6**

- 骑乘期间 **挂起全部 hook**（`PlaySpecific`、播放、资源、`SetVolume`/`GetVolume`），不仅 vtable。
- `RefreshGroupSounds` 在 guard 激活时 **直接跳过**（不再遍历活跃/非活跃 `SoundData` 链表）；修复 HeelsDesignLinker 上马时 IPC 狂刷脚步声分组导致扫描外勤机循环音。
- `ApplyEffectiveHookState` / `Enable()` 前先 `MountTransitionGuard.Update()`，骑乘中点启用或 IPC 重载时立即挂起。

**0.2.1.7**

- 音效黑名单初版（已并入 0.2.1.8 独立 Tab 设计）。

**0.2.1.8**

- **黑名单**独立标签页：**我的黑名单**（可编辑，关键词/路径/Glob + 备注）与 **官方黑名单**（只读，Git 同步）分开展示。
- 运行时两套规则均生效，但数据与 UI 不合并；官方条目玩家不可改。

**0.2.2.1**

- 修复官方黑名单嵌入/远程 JSON 反序列化失败，导致启动日志 `official rev 0`、列表为空。
- 恢复 `SoundVolumeHelper.TryGetPathFromSoundData`；官方同步在主线程写入配置。

**0.2.2.0**

- 骑乘 hook 挂起改为可选 **安全模式**（默认**关闭**；工具栏位于专家模式之后）。关闭时骑乘期仍应用音量规则。
- 若在外勤机等坐骑上仍崩溃：开启安全模式，和/或依赖官方黑名单（`se_bt_etc_mount_guideroid` 等）。

---

## 4. 文档与早期 MVP 文案

`DESIGN.md` 中部分早期设计草案（如 FMOD 缓冲修改、500% 上限）**未反映当前实现**。以本文档、游戏内「更新日志」、`SoundMixer.json` / Catalog manifest 为准。

---

## 更新记录

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-06-08 | 0.2.2.1 | 修复官方黑名单 JSON 反序列化（rev 0）；TryGetPathFromSoundData |
| 2026-06-08 | 0.2.2.0 | 安全模式（骑乘 hook 可选）；默认骑乘期仍混音 |
| 2026-06-08 | 0.2.1.8 | 黑名单独立 Tab；用户/官方分表；匹配类型+备注 |
| 2026-06-08 | 0.2.1.7 | 音效黑名单初版 |
| 2026-06-08 | 0.2.1.6 | 骑乘挂起全部 hook；跳过 RefreshGroupSounds；IPC 前同步 guard |
| 2026-06-08 | 0.2.1.5 | 坐骑过渡物理卸载 vtable hook；坐骑音效提前 guard；5s grace |
| 2026-06-08 | 0.2.1.4 | 坐骑/过渡透传 hook；SetVolume 过渡节点不再跳过 Original |
| 2026-06-08 | 0.2.1.3 | 外勤机坐骑：跳过 GetFileName、仅 tracked enforcement、启用时不全表刷新 |
| 2026-06-06 | 0.2.1.2 | 修复上/下坐骑 SetVolume hook 崩溃 |
| 2026-06-06 | 0.2.1.1 | 修复 native 指针扫描崩溃（SoundDataSafety + VirtualQuery） |
| 2026-06-06 | 0.2.0.0 | 监听/hook 缓解说明对齐 0.2.0；补充文档引用 |
| 2026-06-06 | 0.1.0 | 初版：记录 SoundManager 签名未匹配、监听扫描崩溃及缓解措施 |
