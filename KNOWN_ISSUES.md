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
| 播放瞬间拦截（`PlaySystemSound` 等 5 个 hook） | ❌ 不可用（国服签名） |
| `PlaySound` hook | ❌ **默认不加载**（0.2.3.0 起已确认会引发 CTD，见问题 5） |
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
- **0.2.3.1 起已移除该开关**；路径解析始终安全，骑乘保护依赖官方黑名单与行动守卫。

**0.2.3.0**

- **PlaySound** 已确认 CTD 源：默认不加载签名与 hook；调试页手动开启，附危险说明。
- 外勤机 grace（`OfficialHookGuards` rev3）：**仅**跳过活跃 `SoundData` 链表扫描，**不再**挂起 `SetVolume`/`GetVolume`。
- 骑乘 BGM / ride BGM 路径豁免 mount-loop 黑名单，分组音量可作用于正在播放的 BGM。
- UI 一次性音效（池化 `SoundData`）经 `OneShotPlayRegistry` 修复；工具栏 **清除缓存** 可恢复异常状态。

---

## 4. PlaySound hook 导致崩溃

| 项目 | 内容 |
|------|------|
| **发现时间** | 2026-06-08 |
| **发现版本** | SoundMixer **0.2.2.x** 及更早（调试页可手动启用时） |
| **状态** | **已缓解**（**0.2.3.0** 起默认不加载；仅 CTD 溯源时手动开启，风险自负） |

### 现象

启用 **PlaySound** hook 后，特定场景（含外勤机骑乘）游戏崩溃；禁用该 hook 或整插件后不复现。

### 原因

`PlaySound` 与 `PlaySpecificSound` / `SetVolume` 链路叠加时，对坐骑循环、池化节点等 native 状态干扰过大；hook 本身在敏感时机进入 detour 即可触发 CTD。

### 缓解（0.2.3.0）

- 默认 **不解析、不安装** `PlaySound` hook（`Desired(..., autoDefault: false)`）。
- 配置迁移 v7 强制关闭已保存的 `HookDebug.PlaySound`。
- 调试 Tab 显示危险说明；「一键全开」仍保持 PlaySound 关闭。
- 移除仅为 PlaySound CTD 叠代的冗余骑乘透传/守卫项；castlp 透传仅在手动启用 PlaySound 时生效。

### 根治方向

- 长期不默认启用 PlaySound；混音依赖 `PlaySpecificSound` + `SetVolume` + tracked enforcement（已覆盖绝大多数 SCD 音效）。

---

## 6. 嵌套脚步声子组不生效（父组有效、子组无效）

| 项目 | 内容 |
|------|------|
| **发现时间** | 2026-06-08 |
| **发现版本** | SoundMixer **0.2.3.0** 及更早 |
| **状态** | **已修复**（**0.2.3.1**：`SoundEnforcement` + 路径/规则/index 统一） |

### 现象

典型配置：父组「脚步声」`foot/foot**` 200%，子组「木地板」`foot/foot/fs_wood**` 0%。父组变响，子组仍听得见。  
监听 log 可显示正确材质路径 `fs_wood…/1` 与 0% 倍率，但听感未静音。  
开启 **PlaySound** hook 时父子往往都「有效」，易误判为缺 hook。

### 原因（四条分裂叠加）

1. **路径分裂**：监听用 `TryResolveSoundPath`（FileName + Scds）得材质路径；强制音量曾用 `tracked.ScdPath`、`GetPathFromSoundData`（仅容器 `foot/foot.scd`）等旁路。  
2. **规则分裂**：刷新/监听用 `GroupOwnRulesMatch` 叠乘父子组；`GetVolumeForSound` 曾只取单组，且叠乘 ≈100% 时回退宽路径。  
3. **index 分裂**：`PlaySpecificSound` 打在 `/27` 等 setup index；可听的是 PlaySound `/1`，默认不 hook 时原生播放未缩放。  
4. **PlaySound 掩盖**：手动开 hook 后在播放链补缩放，掩盖上述分裂。

### 修复（0.2.3.1）

- 不变量：**log 中的 `specificPath` + 倍率 = 强制音量唯一输入**（`SoundEnforcement`）。  
- `GetVolumeForSound` 叠乘所有匹配分组；`ChooseNodeEnforcementPath` 选材质路径；脚步声跳过 setup index、仅缩放 `/1`。  
- 详细时序与复盘见 [POSTMORTEM_0.2.3.1.md](./POSTMORTEM_0.2.3.1.md)。

---

## 7. 插件加载瞬间崩溃（统一 SoundEnforcement 后）

| 项目 | 内容 |
|------|------|
| **发现时间** | 2026-06-08 |
| **发现版本** | SoundMixer **0.2.3.0**（接入 `SoundEnforcement`、SafeMode 可关） |
| **状态** | **已修复**（**0.2.3.1**：始终安全路径，移除 SafeMode） |

### 现象

插件加载（Enable hook）瞬间游戏 CTD，无 Dalamud 托管异常。

### 原因

`GetMultiplier` / SetVolume hook 在 Enable 后立即对活跃音效（含 BGM/流媒体）调用 `SoundEnforcement` → `TryResolveSoundPath`。  
当 **安全模式关闭** 时，安全解析失败会回退 **`ISoundData.GetFileName()`** 虚函数；部分 native 节点对该调用敏感 → **访问冲突**，C# 无法捕获。

### 修复（0.2.3.1）

- 删除 `SafeMode` 开关与 `TryGetUnsafeFileName`；全插件禁止 `GetFileName()`。  
- `ResolveSoundEnforcement` 仅 `TryResolveSafeSoundPath`（handle CString + Scds 缓存）。  
- 配置迁移 v8。

### 复盘

多轮修复只统一了「监听」管道，强制音量仍保留 `GetFileName` 可选回退；直到热路径全面接入 `SoundEnforcement` 才组合爆炸。见 [POSTMORTEM_0.2.3.1.md](./POSTMORTEM_0.2.3.1.md) §4。

---

## 5. 文档与早期 MVP 文案

`DESIGN.md` 中部分早期设计草案（如 FMOD 缓冲修改、500% 上限）**未反映当前实现**。以本文档、游戏内「更新日志」、`SoundMixer.json` / Catalog manifest 为准。

---

## 更新记录

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-06-08 | 0.2.3.1 | 移除 SafeMode；SoundEnforcement；嵌套脚步子组；加载 CTD；POSTMORTEM |
| 2026-06-08 | 0.2.3.0 | PlaySound 默认禁用（CTD）；UI 音效/清除缓存；外勤机 grace 与骑乘 BGM；守卫 rev3 |
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
