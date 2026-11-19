# Netch WinUI 3 界面设计规范

> 状态：已定稿（v3）。示意图见 [mockups.html](mockups.html)，用浏览器打开，顶部可切换浅色/深色。
> 示意图用 HTML 模拟 WinUI 外观，实现时以本文档的控件与资源约定为准，不按像素照搬。

## 1. 已确定的决策

| 项 | 决定 |
|---|---|
| 框架 | WinUI 3（Windows App SDK），非打包（`WindowsPackageType=None`），自包含，x64 |
| 主交互 | 以「场景」为中心：场景 = 一个模式 + 一个**固定节点** + 可选热键。点卡片即启动，同一时间只运行一个场景 |
| 视觉 | Fluent，Mica 背景，跟随系统强调色；支持 跟随系统 / 浅色 / 深色 |
| 首次使用 | 三步向导（节点 → 加速什么 → 完成），可跳过 |
| 节点失效 | 不自动切换。场景卡片显示警告，提供「更换节点」 |
| 模式设置 | 全部保存在模式文件里，设置页不再有「模式默认值」 |
| 标题栏 | 只有图标、标题、窗口按钮；不放搜索框，不做 Ctrl+K 命令面板 |
| 比例 | 主窗口默认 960×720 DIP，最小 800×600 DIP（4:3）；向导 640×480 固定 |

## 2. 信息架构

`NavigationView`（`PaneDisplayMode=Left`，窗口宽度 < 860 时 `LeftCompact`），导航名统一为两个字：

| 位置 | 页面 | 内容 | 取代的旧窗体 |
|---|---|---|---|
| 顶部 | 场景 | 当前连接状态 + 场景卡片墙（首页） | MainForm 主体、Profile 按钮 |
| 顶部 | 节点 | 左：订阅分类；右：订阅信息卡 + 节点列表 | ServerForm、SubscriptionForm |
| 顶部 | 模式 | 左：分组（对应 `mode/` 子目录）；右：模式列表；编辑页 | ProcessForm、RouteForm |
| 顶部 | 下载 | 左：程序与核心 / 模式规则 / 下载记录 | Help 菜单的检查更新（新功能为主） |
| 顶部 | 日志 | 过滤 + 虚拟化日志列表 | LogForm |
| 底部 | 关于 | Logo、版本、外部链接、运行信息、开源致谢 | AboutForm |
| 底部 | 设置 | 常规 / 网络 / 内核 / 更新与下载 / 维护 | SettingForm、GlobalBypassIPForm（并入 TUN 模式） |

窗口外：托盘浮窗（左键）、托盘菜单（右键）、首次向导窗口。

## 3. 布局与视觉

- **背景**：窗口 `MicaBackdrop`；内容区用 `LayerFillColorDefaultBrush` + 左上 8px 圆角（同 Windows 设置）。
- **页面**：内容边距 24（左右）/ 16（上）；页标题 `TitleTextBlockStyle`；区块标题 `BodyStrongTextBlockStyle`；次要文字 `CaptionTextBlockStyle` + `TextFillColorSecondaryBrush`。
- **卡片**：`CardBackgroundFillColorDefaultBrush` + `CardStrokeColorDefaultBrush`，圆角 `OverlayCornerRadius`（8），卡片间距 8。
- **主从页（节点 / 模式 / 下载）**：统一结构 = 左侧分类栏（宽 140–160，选中项左侧强调条）+ 右侧「信息卡片 + 筛选行 + 分组列表」。
- **状态色**：成功/延迟低 `SystemFillColorSuccessBrush`，警告 `SystemFillColorCautionBrush`，错误 `SystemFillColorCriticalBrush`；延迟阈值 <100 ms 绿、100–200 黄、超时红。
- **颜色只用主题资源**，禁止写死色值，保证深色模式自动适配。
- **主题切换**：设置项写入配置，启动及修改时设置根元素 `RequestedTheme`，并同步标题栏按钮颜色。

## 4. 页面规范

### 4.1 首次向导（640×480，不可调整大小）
- 顶部步骤条；内容 `Frame` + `SlideNavigationTransition`。
- 第 1 步：订阅链接 / 剪贴板导入 / 手动添加。左下「稍后再说」= 跳过整个向导。**标题栏不放「跳过向导」**。
- 第 2 步：扫描已安装程序（复用 ProcessForm 扫描逻辑）+ 全部流量（TUN）。左下「上一步」，标题栏「跳过向导」。
- 第 3 步：确认场景名、节点（默认延迟最低）、开机自动连接。左下「上一步」，标题栏「跳过向导」。
- 完成或跳过都写 `OnboardingCompleted=true`；关于页可重新打开。

### 4.2 场景（首页）
- 顶部状态卡：电源按钮（启动/停止当前场景）、场景名、模式 · 节点 · 运行时长、速率迷你图、延迟、NAT、下载速率与累计流量。
- 场景墙：`ItemsView` + `UniformGridLayout`，最多 3 列；右上方提供「＋ 新建场景」。
- 卡片：图标、名称、模式 · 节点、底部状态（运行中 / 上次使用 / ⚠ 节点不可用）+ 热键。
- 单击启动（已有运行场景则先停止）；右键 `MenuFlyout`：编辑、复制、更换节点、固定到托盘、删除。
- 空状态：「还没有场景」及一句场景定义。有节点时主按钮为「新建场景」，无节点时为「导入订阅」。两种状态均可打开向导，并在规则有更新时显示提示。

### 4.3 编辑场景（`ContentDialog`）
- 图标、名称、模式（`ComboBox`）+「打开此模式的设置 ›」、节点（可搜索 `ComboBox`，项内显示延迟）、全局热键、固定到托盘。
- **不提供**覆盖 DNS/UDP/ICMP 等模式参数。

### 4.4 节点
- 工具栏：从剪贴板导入、＋订阅、＋节点。
- 左：全部节点 / 订阅（显示更新状态）/ 本地（手动添加）。
- 右：订阅信息卡（链接、自动更新周期、剩余流量、到期，`⋯` = 编辑 / 复制链接 / 删除）、筛选 + 测速 + 排序、节点列表（名称、协议标签、被引用场景数、延迟）。
- 双击节点 = 编辑（按协议动态表单，沿用 ServerForm 字段）；右键 = 分享链接 / 二维码 / 测 NAT / 删除（被场景引用时提示）。

### 4.5 模式 / 模式编辑
- 列表：分组 = `mode/` 子目录；来源标签：内置、有更新、本地已修改、自定义；被引用场景数；支持按程序名反查。
- 顶部 `InfoBar` 提示在线规则更新 →「去下载」。
- 编辑页：`BreadcrumbBar`、名称 / 类型 / 分组；左侧「代理这些程序」「排除这些程序」（程序显示图标与路径，手写正则显示为等宽字体 +「正则」标签）；右侧开关组：
  - 进程模式：TCP、UDP、ICMP（+延迟）、DNS 劫持（+DNS 地址）、DNS 走代理、子进程、本地回环、局域网。
  - TUN 模式：地址、子网掩码、网关、DNS、代理 DNS、IPv6、全局绕过 IP。

### 4.6 下载
- 页头：上次检查时间、检查更新、全部更新(n) / 同步所选(n)。
- **程序与核心**：正在使用的核心更新时用警告 `InfoBar` 说明会断开并自动重连；Netch 本体卡片（当前 → 最新、更新日志、更新并重启）；核心列表（sing-box、Xray-core、Mihomo、tun2socks、WinTUN、pcap2socks）：本地版本 → 最新版本、更新 / 下载 / 进度条 + 暂停，`⋯` = 指定版本 / 回滚 / 打开 Release。NF 驱动、Redirector、aiodns 随 Netch 本体更新。
- **模式规则**：来源卡片（默认 `netchx/netch` · `main` · `Storage/mode`，可更换）；分组「新增 / 更新 / 冲突」带复选框；更新项显示 +/- 规则数与「差异」；冲突默认不勾选，可选 保留本地 / 使用远端 / 另存为副本。
- 下载通道、镜像、预发布等**设置不放在本页**，在 设置 › 更新与下载。

### 4.7 日志
- 自动滚动、复制、打开目录；过滤框、来源（netch / sing-box / mihomo / xray / nf）、级别。
- `ItemsRepeater` 虚拟化；连接失败时首页提示可跳转并预设错误过滤。

### 4.8 设置（`SettingsCard` / `SettingsExpander`，CommunityToolkit）
- 常规：外观、开机启动（管理员）、关闭窗口时、语言。
- 网络：本地混合端口 + 允许局域网、测速方式 · STUN、DNS（AioDNS、出站 DNS）›。
- 内核：Xray / sing-box（指纹、分片、Mux、KCP）›、Hysteria 带宽 ›。
- 更新与下载：下载通道（直连 / 通过当前代理 / GitHub 镜像）、启动时检查更新、包含预发布。
- 维护：清理 DNS 缓存 · 移除防火墙规则、卸载 NF 驱动服务。

### 4.9 关于（参考 frpc-desktop）
- 居中：Logo、名称、版本标签 + 「检查更新」、一句话简介、外部链接（GitHub、Telegram 频道、问题反馈、常见问题）。
- 两张卡片：运行信息（系统、运行时、管理员权限、NF 驱动、数据目录；复制诊断信息、打开目录）、开源致谢（核心与依赖、GPL-3.0）。
- 底部：重新打开首次向导、版权。

### 4.10 托盘
- 左键：无边框浮窗（`DesktopAcrylicBackdrop`，失焦收起）：连接状态 + 断开按钮、固定到托盘的场景磁贴（2 列）、速率与 NAT、打开 Netch / 退出。
- 右键：原生菜单（场景列表可切换、断开连接、打开 Netch、退出）。

## 5. 数据模型变化

| 变化 | 说明 |
|---|---|
| `Scene` | 取代 `Profile`：`Id`、`Name`、`Icon`、`ModeId`（模式相对路径）、`ServerId`、`Hotkey`、`PinnedToTray`、`LastUsed`。首次启动从旧 Profile 迁移 |
| 节点 / 模式稳定 ID | 场景引用不能再靠 Remark 文本匹配；节点需稳定 ID（订阅更新后按 地址+端口+协议 重新关联） |
| `ProcessMode` 字段非空 | null 不再表示「回退全局」。**读取时**用固定默认值补齐，不写回文件，避免在线同步把内置模式判为本地已修改 |
| `TunMode` 新字段 | 地址、掩码、网关、DNS、代理 DNS、IPv6、绕过 IP（从全局 `TUNConfig` 迁入）|
| 全局设置迁移 | 首次启动新版时，把旧全局进程/TUN 设置写入引用它的场景所用的模式副本或作为新默认值，保证升级后行为不变 |
| `mode/.manifest.json` | 记录每个在线同步文件的远端 blob SHA，用于区分「远端有更新」和「本地已修改」 |
| 新设置项 | `Theme`、`OnboardingCompleted`、下载通道 / 镜像 / 预发布 / 启动检查 |

## 6. 实施计划

| 阶段 | 内容 | 验收 |
|---|---|---|
| **P1 核心解耦** | 去掉非 UI 代码对 `Global.MainForm` 的直接调用，改为 `AppEvents` 事件 + 运行状态集中管理；WinForms 版订阅事件，行为不变 | 现有 WinForms 版构建通过、测试通过、功能一致 |
| **P2 拆出 Netch.Core** | 把 Controllers / Models / Servers / Services / Utils / Interops 等移入类库 `Netch.Core`，WinForms 与测试引用它 | 两个前端都能引用核心；WinForms 版行为不变 |
| **P3 WinUI 外壳** | 新建 `Netch.App`：Mica、自定义标题栏、NavigationView、6 个页面占位、主题切换、单实例、管理员清单 | 能以管理员启动，导航与主题切换正常 |
| **P4 首页闭环** | Scene 模型与 Profile 迁移、场景墙、编辑场景、启动 / 停止、状态卡（速率、延迟、NAT） | 能通过场景启动和停止代理 |
| **P5 托盘** | 托盘图标、浮窗、右键菜单、全局热键；实测管理员态行为 | 托盘与热键在管理员态可用 |
| **P6 节点 / 模式** | 节点页（订阅 + 节点编辑）、模式页与编辑页、模式字段迁移 | 可脱离 WinForms 完成节点与模式管理 |
| **P7 下载** | 核心版本检查与更新、模式规则在线同步（manifest、冲突处理）| 可更新核心与同步规则 |
| **P8 其余页面** | 日志、设置、关于、首次向导 | 功能覆盖旧版 |
| **P9 切换** | WinUI 版成为默认入口，打包脚本更新，旧 WinForms 版移除 | 发布包只含新版 |

## 7. 待验证风险
- 管理员态下托盘图标、全局热键、拖放的行为（P5 原型实测）。
- 自包含 Windows App SDK 的包体与冷启动时间。
- WinUI 3 与现有单实例库（HMBSbige.SingleInstance）的兼容。

实测结果（Windows 10 19044，Windows App SDK 2.7）：
- 单实例：与 WinForms 版共用 `Global\Netch`，第二个实例把「显示」交给第一个后退出，可用。
- 管理员态：全局热键可用（热键启停场景已实测）；关闭窗口隐藏到托盘、托盘菜单退出可用。托盘窗口对 `TaskbarCreated` 和托盘回调消息放开了 UIPI。从资源管理器真实点击托盘图标未能自动化实测，需手动确认。拖放待 P6 实测。
- Windows 10 没有 Mica，主窗口回退为纯色背景；托盘浮窗的 Desktop Acrylic 可用。
- CommunityToolkit 8.2 的 SettingsCard 在 Windows App SDK 2.7 上可用（它依赖元包，所以项目引用完整的 `Microsoft.WindowsAppSDK`）。
