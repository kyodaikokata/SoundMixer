# SoundMixer

**Current version:** 0.2.1.0

**Source repository:** https://github.com/kyodaikokata/SoundMixer

> Fine-tune **individual sound effects** by SCD path—not just the game's global SFX slider. Group sounds with Glob patterns, nest groups, switch presets, and watch the live monitor to catch what you hear.
>
> **按 SCD 路径精细调节音效**——不只是游戏里的全局 SFX 滑条。用 Glob 分组、嵌套分组与预设方案管理音量，实时监听帮你找到正在播放的声音。

Install and updates are distributed through the unified **KKT-Catalog** custom plugin repository. Add **one** repo URL in Dalamud (Settings → Custom Plugin Repositories):

| Launcher | URL |
|----------|-----|
| XIVLauncher 国服 (CN) | `https://raw.githubusercontent.com/kyodaikokata/KKT-Catalog/main/pluginmaster.cn.json` |
| XIVLauncher 国际 (Global) | `https://raw.githubusercontent.com/kyodaikokata/KKT-Catalog/main/pluginmaster.global.json` |

### Documentation

| Document | Description |
|----------|-------------|
| [KNOWN_ISSUES.md](./KNOWN_ISSUES.md) | Known issues and troubleshooting |

In-game changelog: **更新日志 / Changelog** tab (`/soundmixer` or `/smix`).

---

## English

Per-SCD-path volume mixer for FFXIV sound effects. Match sounds by **Glob path patterns** or explicit paths, organize them in **nested groups**, and apply **0–200% linear gain** (Expert Mode up to **350%**, audible engine cap).

### What can you control?

| Area | Examples |
|------|----------|
| **Groups** | Nested tree, drag-and-drop order, root-group colors, stacked effective volume in the tree |
| **Matching** | Glob patterns (`**/footstep/**`), explicit sound paths, path fixes for `unknown/…` aliases |
| **Presets** | Switch whole layouts (groups + volumes); create, copy, delete |
| **Live monitor** | Recently played paths, filters, right-click → add to group / set volume / fix path |
| **Looping audio** | BGM and weather/ambient hooks with **Refresh All** or per-group refresh |
| **External control** | Session-only IPC overrides from other plugins (tag + priority); cleared on logout |

Volume is **linear gain**: 200% ≈ +6 dB, 350% ≈ +10.9 dB. Values above the audible cap are clamped.

### Quick start

1. Open the window: `/soundmixer` or `/smix`
2. Enable the plugin in the toolbar if it is off
3. Select or create a **group** in the left tree
4. Set **group volume** (double-click the slider to type an exact %)
5. Add **Glob path patterns**, or use the **live monitor** (right-click a line → add to group)
6. Use **Refresh All Sounds** after changing volumes for BGM / ambient loops that keep playing
7. Drag splitters to resize the top panel, group tree, or detail pane—layout is saved automatically

**Tips**

- Turn on **Hide matched rules** in the monitor to focus on sounds you have not configured yet
- **Expert Mode** unlocks 200–350%; watch for `[MAX]` / `[~]` badges on sliders
- Toolbar **(IPC)** means another plugin has temporary overrides active

### Recent highlights (0.2.1)

- **Resizable UI**: drag between top controls and main content, and between group tree and details
- **Layout memory**: panel sizes, window position/size, and group-tree expand state save automatically
- See the in-game **Changelog** tab for full history

---

## 中文

按 **SCD 路径** 调节 FF14 音效音量。用 **Glob 路径模式** 或单独路径匹配，在 **嵌套分组** 中管理，**0–200% 线性增益**（专家模式最高 **350%**，实测听感上限）。

### 可以控制什么？

| 功能 | 说明 |
|------|------|
| **分组** | 树形嵌套、拖拽排序、根分组颜色、树中显示叠乘后的有效音量 |
| **匹配** | Glob（如 `**/footstep/**`）、单独路径、`unknown/…` 路径修正 |
| **预设** | 切换整套分组与音量方案；新建、复制、删除 |
| **实时监听** | 最近播放路径、筛选、右键 → 加入分组 / 设音量 / 修正路径 |
| **持续播放** | BGM、天气/环境音 hook；**刷新所有音效** 或按分组刷新 |
| **外部控制** | 其他插件 IPC 临时覆盖（tag + priority）；登出后清除 |

音量为 **线性倍率**：200% ≈ +6 dB，350% ≈ +10.9 dB；超过听感上限会自动钳制。

### 安装

在 Dalamud → 设置 → 自定义插件库 中添加 **KKT-Catalog** 源（见上文表格）。

### 快速上手

1. 打开窗口：`/soundmixer` 或 `/smix`
2. 若未启用，在工具栏点击 **启用**
3. 在左侧树选择或 **新建分组**
4. 调节 **分组音量**（双击滑条可输入精确百分比）
5. 添加 **Glob 路径模式**，或在 **实时监听** 中右键条目 → 加入分组
6. 修改 BGM / 环境音等持续播放的音量后，点 **刷新所有音效** 立即生效
7. 拖动分割条调整上方控制区、分组树与详情区大小——布局会自动保存

**小贴士**

- 监听面板开启 **隐藏已匹配规则**，便于发现尚未配置的音效
- **专家模式** 解锁 200–350%；滑条上的 `[MAX]` / `[~]` 为听感提示
- 工具栏 **(IPC)** 表示有其他插件的临时覆盖生效中

### 近期要点（0.2.1）

- **可拖动 UI 分区**：上方控制区 / 主内容、分组树 / 详情
- **布局记忆**：面板尺寸、窗口位置/大小、分组树展开状态自动保存
- 完整历史见游戏内 **更新日志** 标签
