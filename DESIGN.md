# SoundMixer 架构说明 / Architecture

> 对齐当前实现 **0.2.3.1**。早期 MVP 草案（FMOD 缓冲、500% 上限等）已过时；以本文档、游戏内「更新日志」、[KNOWN_ISSUES.md](./KNOWN_ISSUES.md) 与 [POSTMORTEM_0.2.3.1.md](./POSTMORTEM_0.2.3.1.md) 为准。

## 概述

SoundMixer 通过 Dalamud 在 `SoundManager` 播放链与 `SoundData` 音量链上挂 hook，按 SCD 路径 / Glob 规则对 FFXIV 音效施加 **0–200% 线性增益**（专家模式最高 **350%**，听感上限钳制）。

核心能力：

- **路径混音**：播放 hook + `SetVolume` / `GetVolume` + 每帧 tracked enforcement
- **分组与预设**：嵌套树、Glob、拖拽排序、根分组颜色
- **安全与黑名单**：始终安全路径解析（禁止 `GetFileName`）、官方/用户黑名单、行动守卫（Hook guards）
- **IPC 临时覆盖**：其他插件 tag + priority，登出清除
- **调试**：逐 hook 手动开关（CTD 溯源）

---

## 模块结构

```
SoundMixer/
├── Plugin.cs                  # 生命周期、Framework.Update、配置迁移、IPC 门控
├── Filter.cs                  # 全部 hook、播放/音量拦截、刷新与监听
├── OneShotPlayRegistry.cs     # UI 等池化一次性音效：延迟 SetVolume、透传保护
├── SoundDataSafety.cs         # VirtualQuery + 安全链表遍历
├── MountTransitionGuard.cs    # 骑乘/上下马 grace、坐骑路径检测
├── SoundVolumeTracker.cs      # SoundData 追踪、池节点恢复、淡出感知
├── SoundEnforcement.cs        # 强制音量与监听 log 同源（TryResolveSafeSoundPath）
├── FootstepPlaybackBridge.cs  # 脚步声 setup index vs 播放 /1、材质路径桥接
├── VolumeCalculator.cs        # 分组叠乘、Glob、IPC、单独路径
├── HookGuardPolicy.cs         # 官方/用户行动守卫
├── Configuration.cs             # 持久化配置（含 HookDebugSettings）
├── PluginUI*.cs               # ImGui 主界面 / 高级设置 / 调试 / 更新日志
├── OfficialSoundBlacklist.json
├── OfficialHookGuards.json    # 官方行动守卫手册（rev3）
├── Api/                       # IPC Provider + TemporaryOverrideManager
├── Localization/              # 中英 LocStrings + ChangelogBody
└── SoundManagerHookResolver.cs
```

---

## Hook 策略（0.2.3.1）

| Hook | 默认 | 说明 |
|------|------|------|
| `PlaySpecificSound` | 开 | 主要 SCD 索引播放入口 |
| `SetVolume` / `GetVolume` | 开 | 音量读写与追踪 |
| `PlayBGMSound` / `PlayWeatherSound` | 开 | BGM / 环境持续音 |
| `PlaySystemSound` / `PlayClipSound` / `PlayMovieSound` | 开 | 带路径的播放 API |
| **`PlaySound`** | **关（不加载）** | **已确认 CTD**；仅调试页手动控制时可加载 |
| 资源 hook ×3 | 开 | SCD 指针 → 路径缓存 |

解析顺序：`MemberFunctionPointers` → `TryScanText`；失败时合并 Info，见 `KNOWN_ISSUES.md` §1。

**BGM / 环境音**：播放时记录路径；`RefreshGroupSounds` / 每帧 enforcement 对 tracked 节点刷新。骑乘 BGM 路径不再被 mount-loop 黑名单整体屏蔽。

### 一次性音效（UI 等）

池化 `SoundData` 节点在 `PlaySpecificSound` 路径下：

- 播放时 `EnterPlayBypass()`，仅缩放 play 参数
- `OneShotPlayRegistry` 登记节点，每帧 `ProcessOneShotVolumeApplies` 经原生 `SetVolume.Original` 单次写入
- 已登记节点对 `SetVolume`/`GetVolume` 完全透传，避免二次缩放

工具栏 **清除缓存** 调用 `Filter.ClearRuntimeCache()`：释放追踪、清 registry、恢复 inactive 池节点。

### Native 安全（0.2.1.1+）

`SoundDataSafety` + `VisitSoundList`（环检测、4096 上限）。外勤机 grace 期间**仅跳过活跃链表扫描**，不再挂起 `SetVolume`/`GetVolume`。

### 路径解析与骑乘

| 机制 | 行为 |
|------|------|
| **安全路径（强制）** | 全插件 `TryResolveSafeSoundPath`；禁止 `ISoundData.GetFileName()` |
| **SoundEnforcement** | `GetMultiplier` / `ForceRefresh` 与监听 log 同一 resolver |
| **官方黑名单 / grace** | 外勤机等跳过活跃链表扫描；SetVolume/GetVolume 保持启用 |
| **行动守卫** | 骑乘禁用 PlaySystem/Clip/Movie；PlaySound 默认不加载 |

详见 `KNOWN_ISSUES.md` §3、§6、§7 与 [POSTMORTEM_0.2.3.1.md](./POSTMORTEM_0.2.3.1.md)。

---

## 音量叠乘

`VolumeCalculator` 顺序：

1. 插件总开关 + 黑名单 / 守卫 bypass
2. IPC 覆盖（分组 / Glob / 单独路径）
3. `IndividualVolumes`
4. 分组树 Glob + 精确路径（父 → 子叠乘）
5. 默认 1.0

UI 分组树 **实际 X%** 仅统计**该分组自身**路径规则；无规则且 100% 时不显示。

---

## IPC（0.2.0+）

- 入口：`SoundMixer.Api`（Dalamud IPC）
- `TemporaryOverrideManager`：会话内覆盖，UI 绿字 `(IPC X%)`
- 工具栏 `(IPC)` 徽章

---

## UI 分区

| Tab | 内容 |
|-----|------|
| 混音器 | 分组树、预设、监听、工具栏（含清除缓存） |
| 高级设置 | 音效黑名单、行动守卫 |
| 调试 | 逐 hook 开关；PlaySound 危险说明 |
| 更新日志 | `LocStrings.ChangelogBody` |

---

## 构建与发布

| 步骤 | 路径 |
|------|------|
| 开发 | `WorkInProgress/SoundMixer` |
| 双端 zip | `scripts/build-dual.ps1` → `dist/cn\|global/latest.zip` |
| Catalog | `KKT-Catalog/scripts/publish-plugin.ps1` |

版本号：`SoundMixer.csproj` `$(Version)` → 构建时同步 `SoundMixer.json` `AssemblyVersion`。

---

## 依赖

- `Dalamud.CN.NET.Sdk` 15.0.0
- `DotNet.Glob` 3.1.3

## 许可

AGPL-3.0（源自 SoundFilter）
