# SoundMixer

**Current version:** 0.2.0.0 · Dalamud API 15

**Source repository:** https://github.com/kyodaikokata/SoundMixer

Per-SCD-path volume control for FFXIV sound effects — custom groups, Glob patterns, presets, live monitor, IPC overrides, and BGM/ambient support.

**Install and updates** are distributed through the unified **KKT-Catalog** custom plugin repository (not this repo). Add **one** repo URL in Dalamud → Settings → Custom Plugin Repositories:

| Launcher | URL |
|----------|-----|
| XIVLauncher 国服 (CN) | `https://raw.githubusercontent.com/kyodaikokata/KKT-Catalog/main/pluginmaster.cn.json` |
| XIVLauncher 国际 (Global) | `https://raw.githubusercontent.com/kyodaikokata/KKT-Catalog/main/pluginmaster.global.json` |

Download links and the in-game plugin icon are hosted in [KKT-Catalog](https://github.com/kyodaikokata/KKT-Catalog) (`plugins/SoundMixer/latest-cn.zip`, `plugins/SoundMixer/latest-global.zip`, `images/SoundMixer/icon.png`). This repository contains **source code only**.

---

## v0.2.0 highlights

- **IPC temporary overrides** for external plugins (tag + priority, cleared on logout)
- Collapsible monitor / IPC panels (always visible); group tree shows stacked effective volume with green override labels
- Play hooks via ClientStructs `MemberFunctionPointers`; consolidated logging when signatures mismatch
- Live monitor scan guards; see [KNOWN_ISSUES.md](./KNOWN_ISSUES.md)
- Release zip ships **`DotNet.Glob.dll`** alongside the plugin (4 files, no ILRepack)
- Chinese / English UI including in-game changelog tab

---

## Release build (maintainers)

Build and publish scripts live **only** under WorkInProgress. Run from there:

```powershell
cd E:\work\DalamudProject\WorkInProgress\SoundMixer
.\scripts\publish-release.ps1
# CN only: .\scripts\publish-release.ps1 -SkipGlobal
```

This builds CN + Global zips into `dist/`, then publishes into [KKT-Catalog](https://github.com/kyodaikokata/KKT-Catalog) via `publish-plugin.ps1` with `-WorkInProgressRoot` pointing at the WIP folder (so `PublishHelpers.ps1` is loaded from WIP, not from this repo).

A wrapper script exists at `Release/SoundMixer/scripts/publish-release.ps1` but it delegates build/publish paths to WorkInProgress.

To refresh **this** source repo from WIP after edits:

```powershell
cd E:\work\DalamudProject\Release\SoundMixer
.\sync-from-wip.ps1
```

`sync-from-wip.ps1` copies source and images only — not `scripts/`, `dist/`, or build outputs.

---

## English

Fine-grained FFXIV audio mixing by **SCD path**. Group sounds with **Glob** patterns, nest groups, use **presets**, and watch the **live monitor** while playing.

### Commands

| Command | Action |
|---------|--------|
| `/soundmixer` | Toggle main window |
| `/smix` | Alias for `/soundmixer` |

Open the window from Dalamud plugin list (gear icon) as well.

### Features

- **Per-path volume** — match `sound/...` paths or Glob patterns (e.g. `**/weaponskill/**`)
- **Nested groups** — drag-and-drop ordering; root groups can have custom label colors
- **Presets** — save and switch full layouts (groups + volumes)
- **IPC temporary overrides** — session-only control for external plugins (tag + priority)
- **Live monitor** — recent sounds with filters; collapsible panels; optional hide matched / keyword hide
- **BGM & weather/ambient** — group-based control with refresh actions
- **Expert mode** — up to **350%** engine-audible cap (default UI max 200%)
- **Localization** — 中文 / English UI

### Quick start

1. Install via **KKT-Catalog** (table above).
2. Open the plugin: `/soundmixer`.
3. Use built-in groups or create your own; adjust sliders (0–200%, or higher in Expert mode).
4. Enable **live monitor** to see paths, then drag or assign patterns to groups.
5. Save a **preset** when you want different layouts (e.g. quiet housing vs. loud combat).

### Feedback

Use GitHub Issues on this repository. When reporting matching problems, include the **sound path** and steps to reproduce. See [KNOWN_ISSUES.md](./KNOWN_ISSUES.md).

---

## 中文

按 **SCD 路径** 精细调节最终幻想 XIV 音效音量。支持 **Glob** 分组、嵌套分组、**预设**、**IPC 临时覆盖** 与 **实时监听**。

### 命令

| 命令 | 作用 |
|------|------|
| `/soundmixer` | 打开/关闭主窗口 |
| `/smix` | `/soundmixer` 的简写 |

也可在 Dalamud 插件列表中点击齿轮图标打开。

### 功能

- **按路径音量** — 匹配 `sound/...` 或 Glob（如 `**/weaponskill/**`）
- **嵌套分组** — 拖放排序；根分组可自定义标签颜色
- **预设** — 保存/切换整套分组与音量
- **IPC 临时覆盖** — 供外部插件会话内控制（tag + priority）
- **实时监听** — 可折叠面板；可隐藏已匹配项或按关键词过滤
- **BGM / 天气环境音** — 分组控制与刷新
- **专家模式** — 最高约 **350%** 听感上限（默认界面最大 200%）
- **界面语言** — 中文 / English

### 快速上手

1. 通过 **KKT-Catalog** 安装（见上文表格）。
2. 游戏内输入 `/soundmixer` 打开界面。
3. 使用内置分组或新建分组，拖动滑块调节音量。
4. 开启 **实时监听** 查看路径，再分配到分组或添加 Glob 规则。
5. 不同场景可保存 **预设**。

### 反馈

请在 GitHub 本仓库提交 Issue。反馈匹配问题时请附上 **音效路径** 与复现步骤。已知问题见 [KNOWN_ISSUES.md](./KNOWN_ISSUES.md)。

---

## 仓库分工 · Repo layout

| 内容 | WorkInProgress | 本仓库 (SoundMixer) | KKT-Catalog |
|------|:--------------:|:-------------------:|:-----------:|
| 源码 `*.cs` | ✅ | ✅ | ❌ |
| `scripts/`、`dist/` | ✅ | ❌ | ❌ |
| 发行 zip | ❌ | ❌ | ✅ `plugins/SoundMixer/` |
| 插件图标源图 | ✅ `images/` | ❌ | ✅ `images/SoundMixer/icon.png` |
| manifest 草稿 | ✅ `pluginmaster.*.json` | ❌ | ✅ `pluginmaster.cn.json` |
