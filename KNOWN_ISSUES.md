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

### 缓解（0.1.0 起代码侧）

- 优先使用 FFXIVClientStructs 已解析的 `MemberFunctionPointers`，`TryScanText` 为备选；不再对每条签名单独刷 Warning。
- 合并为一条 Info，列出未安装的 play hook，并说明核心混音仍可用。
- 待 Dalamud CN / ClientStructs 更新签名后，无需改业务逻辑即可恢复 hook（若指针非空）。

### 根治方向

- 跟进 [FFXIVClientStructs `SoundManager.cs`](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Sound/SoundManager.cs) 与国服实际二进制，更新 6 个 `MemberFunction` 签名；或等待 XIVLauncher CN 自带 ClientStructs 同步。

---

## 2. 实时监听扫描在 Framework.Update 中崩溃

| 项目 | 内容 |
|------|------|
| **发现时间** | 2026-06-06 |
| **发现版本** | SoundMixer **0.1.0**（`0.1.0.0`）· Dalamud API **15** |
| **状态** | 已缓解（0.1.0 起增加防护，需重新编译部署后生效） |

### 现象

启用「实时监听」后，Dalamud 日志反复出现：

```text
Exception in event handler "IFramework::Update"
  at SoundMixer.Filter.ScanSingleSoundForMonitoring … Filter.cs
  at SoundMixer.Plugin.OnFrameworkUpdate … Plugin.cs
```

### 原因

`EnforceTrackedVolumes` 每帧扫描游戏内 `SoundData` 链表做监听；在部分时机（加载、切图、音效释放）会读到 **无效或已释放** 的 native 指针，触发访问异常。

### 缓解（0.1.0 起）

- `ScanSingleSoundForMonitoring` / 链表遍历：`try/catch`、环检测、节点上限。
- `GetPathFromSoundData`：读取失败返回空路径。
- `EnforceTrackedVolumes` 整体 `try/catch`，避免拖垮 `IFramework::Update`。

### 残留风险

极端情况下仍可能漏记单条监听，不应再导致插件每帧报错。若仍有堆栈，请附带 Dalamud 日志与当时场景（副本 / 过图 / BGM 切换等）。

---

## 3. README 中其它历史条目

`README.md` 里部分「已知问题」（如 unknown 路径、音量未实现）描述的是 **早期 MVP 文案**，与当前 0.1.0 实现不一致。以本文档与 `CHANGELOG` / 游戏内更新说明为准。

---

## 更新记录

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-06-06 | 0.2.0.0 | 版本号 bump；监听/hook 缓解随 0.2.0 发行 |
| 2026-06-06 | 0.1.0 | 初版：记录 SoundManager 签名未匹配、监听扫描崩溃及缓解措施 |
