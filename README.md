<p align="center">
  <img src="Netch/Resources/Netch.png" width="128" alt="Netch logo" />
</p>

<h1 align="center">Netch</h1>

<p align="center">
  Windows x64 游戏与应用流量代理客户端<br />
  A lightweight proxy client for Windows, optimized for game traffic and UDP relay.
</p>

<p align="center">
  <a href="https://github.com/cbear2024/netch/actions/workflows/build.yml"><img src="https://github.com/cbear2024/netch/actions/workflows/build.yml/badge.svg?branch=main" alt="Build" /></a>
  <a href="https://github.com/cbear2024/netch/releases"><img src="https://img.shields.io/github/v/release/cbear2024/netch?style=flat-square" alt="Release" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-GPLv3-blue.svg?style=flat-square" alt="GPLv3" /></a>
  <img src="https://img.shields.io/badge/platform-Windows%20x64-0078D6?style=flat-square" alt="Windows x64" />
</p>

> [!IMPORTANT]
> 这是 Netch 1.x 的社区维护分支，并非 NetchX 官方发行版。程序需要管理员权限来加载网络驱动和修改系统路由。

当前维护者：[@cbear2024](https://github.com/cbear2024) · 项目地址：[cbear2024/netch](https://github.com/cbear2024/netch)

## 主要特性

- 按进程接管游戏或应用流量，无需修改目标程序。
- 支持 SOCKS5、Shadowsocks、ShadowsocksR、Trojan、VMess、VLESS 和 WireGuard 等协议。
- 集成 Xray-core，支持 TLS、REALITY、Vision、WebSocket、XHTTP 等常用传输方式。
- 提供进程代理、共享代理、TUN 和 Web 代理模式。
- 显示 HTTP 延迟、出口地址与 UDP NAT 检测结果。
- 保留 SagerNet 核心以兼容 ShadowsocksR、SSH、插件和部分旧传输。

### 本分支优化

- 为游戏 TCP 连接启用 `TCP_NODELAY`，减少小包交互延迟。
- UDP 转发采用散布/聚集发送，减少每包复制与临时内存分配。
- 支持最大合法 UDP 数据报，并容错处理 Windows UDP reset 与异常数据报。
- SOCKS 和 STUN 地址并行解析，DNS 缓存支持并发访问。
- UDP NAT 检测使用明确的单步与总超时，支持取消、IPv4 优先和 IPv6 回退。
- 停止代理或检测失败时能够可靠回收资源，避免界面长期停留在“Testing”。
- 进程模式下的代理 DNS 复用同一个 SOCKS5 UDP 关联，按事务 ID 多路复用，不再为每次查询重新握手（以及在 VLESS/VMess/Trojan 等协议上重新建立 TLS 连接）。
- Release 版 Redirector 不再在 Netfilter 回调线程上逐连接、逐 DNS 包输出日志；ICMP 模拟延迟不再阻塞回调线程；TCP 转发缓冲区扩大到 16 KiB。
- 节点延迟取三次探测中的最佳成功结果；DNS 解析不再全局串行，同名查询会合并。
- NAT 检测有 20 秒总超时，HTTP 检测遵循“请求超时”设置；GeoIP 数据库只加载一次。
- 加速期间提升 Netch 与代理核心（xray、sing-box、tun2socks、pcap2socks）的进程优先级，并关闭 Windows 电源节流（EcoQoS）；Redirector 转发线程以高优先级运行。可在 `data\settings.json` 中将 `GameProcessBoost` 设为 `false` 关闭。
- 转发用 UDP 套接字缓冲区扩大到 1 MiB，减少游戏与语音突发流量时的丢包。
- 进程模式的流量统计直接读取 Redirector 计数，不再启动系统级内核网络追踪。
- 进程规则匹配不区分大小写；“包括子进程”沿父进程链最多向上追溯 6 层（用进程创建时间排除 PID 复用）。
- 内置“游戏合集”规则（`mode\Game`）：EA App + EA 游戏、Steam + Steam 游戏、Rockstar Games + GTA 全家桶、PUBG、CS2 集中在一个模式里，包含常见反作弊（EAC、BattlEye、EA AntiCheat、FACEIT）与子进程，局域网流量直连。

## v2.0.1 更新说明

本版本围绕游戏加速的延迟、稳定性和资源占用做了一轮全面优化，并修复了若干缺陷。

### 网络转发（Redirector）

- **代理 DNS 复用连接**：进程模式下的 DNS 查询共享一个长期存在的 SOCKS5 UDP 关联，按 DNS 事务 ID 多路复用。之前每次查询都要重新握手、创建套接字和两个线程，使用 VLESS/VMess/Trojan 等协议时还会额外建立一次 TLS 连接。连续多次无响应时会自动重建关联。（`Redirector/DNSHandler.cpp`）
- **转发线程高优先级**：TCP 接入/转发、UDP 接收、DNS 转发线程以 `THREAD_PRIORITY_HIGHEST` 运行，游戏占满 CPU 时转发不再排队。（`Redirector/Utils.cpp`）
- **UDP 缓冲区 1 MiB**：减少游戏与语音突发流量时的系统丢包。（`Redirector/SocksHelper.cpp`）
- **TCP 转发缓冲区 16 KiB**：原为 1446 字节，降低大流量时的系统调用次数，小包延迟不变。（`Redirector/TCPHandler.cpp`）
- **Release 版不再逐连接/逐包输出日志**，ICMP 模拟延迟不再阻塞驱动回调线程。（`Redirector/EventHandler.cpp`、`Redirector/IPEventHandler.cpp`）
- **进程规则不区分大小写**，与 Windows 路径行为一致。（`Redirector/Redirector.cpp`）
- **“包括子进程”沿父进程链最多追溯 6 层**：原来只检查直接父进程，启动器 → 引导程序 → 游戏的多层启动会漏掉。使用进程创建时间排除 PID 复用造成的误匹配。（`Redirector/EventHandler.cpp`）

### 进程调度

- 加速期间将 Netch、xray、sing-box、tun2socks、pcap2socks 提升到“高于正常”优先级，并关闭 Windows 电源节流（EcoQoS）；停止后 Netch 恢复默认。可在 `data\settings.json` 中设置 `"GameProcessBoost": false` 关闭。（`Netch/Utils/ProcessTuning.cs`）
- 进程模式的流量统计改为读取 Redirector 计数，不再启动系统级内核网络追踪（ETW），降低游戏时的 CPU 占用。（`Netch/Utils/Bandwidth.cs`）

### 游戏规则

- 新增进程模式“游戏合集”：EA App + EA 游戏、Steam + Steam 游戏、Rockstar Games + GTA 全家桶、PUBG、CS2，包含 EasyAntiCheat、BattlEye、EA AntiCheat、FACEIT 等反作弊与子进程，局域网流量直连。（`Storage/mode/Game/Game Collection (EA Steam Rockstar PUBG CS2).json`）

### 缺陷修复与性能

- DNS 解析去掉全局锁，改为并发解析并合并相同查询，系统解析器增加超时。（`Netch/Utils/DnsUtils.cs`）
- 节点测速取三次探测中的最佳成功结果，不再因最先返回的失败结果误报。（`Netch/Models/Server.cs`）
- 修复进程模式规则校验的延迟求值问题，规则被清空后又被重新注册。（`Netch/Controllers/NFController.cs`）
- 修复快速停止/启动时 NAT 与 HTTP 检测互相释放取消令牌的竞争；NAT 检测增加 20 秒总超时，HTTP 检测遵循“请求超时”设置，界面不再停留在“Testing”。（`Netch/Forms/MainForm.cs`、`Netch/Utils/Socks5ServerTestUtils.cs`、`Netch/Controllers/MainController.cs`）
- 修复单节点测速未等待结果、启动后测速间隔为 0 时的死循环、每秒整窗重绘、通过代理更新订阅失败后代理未停止。（`Netch/Forms/MainForm.cs`）
- 代理核心启动改为事件驱动，核心提前退出时立即报错，不再等满 50 秒。（`Netch/Controllers/Guard.cs`）
- GeoIP 数据库只加载一次，`Ping` 对象正确释放；防火墙规则每次运行只检查一次；TUN 模式并行解析、更快检测虚拟网卡。（`Netch/Utils/Utils.cs`、`Netch/Utils/Firewall.cs`、`Netch/Controllers/TUNController.cs`）
- 两个 GBK 编码的源文件转为 UTF-8；启用可空注解上下文，消除约 670 条 CS8632 警告。

### 构建

- `build.ps1` 出错即停止；本地默认总是重新编译，CI 使用 `-UseCache`；GeoIP 下载失败时回退到本地文件；自动通过 vswhere 定位 MSBuild，普通 PowerShell 也能构建；自动解除子脚本的网络下载标记。
- CI 更新到当前版本的 Actions，显式安装 .NET SDK，移除未使用的 Rust 步骤，收紧权限。
- 版本号更新为 2.0.1。

## 工作模式

| 模式 | 说明 |
| --- | --- |
| `ProcessMode` | 使用 Netfilter 驱动按进程拦截流量，适合游戏和指定应用。 |
| `ShareMode` | 基于 WinPcap/Npcap 共享网络。 |
| `TunMode` | 使用 WinTUN 创建虚拟网卡并接管流量。 |
| `WebMode` | 提供系统 Web 代理。 |

## 支持协议

- [SOCKS5](https://www.rfc-editor.org/rfc/rfc1928)
- [Shadowsocks](https://shadowsocks.org/)
- ShadowsocksR
- [WireGuard](https://www.wireguard.com/)
- [Trojan](https://trojan-gfw.github.io/trojan/)
- [VMess](https://www.v2fly.org/)
- [VLESS](https://xtls.github.io/)

## 快速开始

1. 从 [Releases](https://github.com/cbear2024/netch/releases) 下载并解压程序。
2. 以管理员身份运行 `Netch.exe`。
3. 导入订阅或手动添加节点。
4. 选择节点、工作模式和需要代理的进程，然后点击启动。
5. 点击状态栏中的 NAT 结果可以重新执行 UDP 检测。

### VLESS + WebSocket

VLESS + WebSocket 配置由 Xray-core 处理。请确保地址、端口、UUID、WebSocket 路径、Host/SNI 和 TLS 设置与服务端一致。节点、服务端和所选工作模式都必须支持 UDP，UDP 转发才能正常工作。

较新的 Xray-core 不接受部分旧配置中的 `Allow Insecure`。连接失败时，应优先使用有效证书并关闭该选项，而不是跳过证书校验。

## UDP 与 NAT 检测

NAT 检测通过当前节点的 SOCKS5 UDP 通道访问 STUN 服务器。结果含义如下：

| 结果 | 含义 |
| --- | --- |
| `1` | Endpoint-Independent Mapping，并且过滤行为适合入站打洞。 |
| `2` | 地址映射较宽松，但过滤行为更严格。 |
| `3` | Address/Port-Dependent Mapping，通常不利于 P2P 打洞。 |
| `NoUDP` | STUN Binding 请求未通过当前代理通道完成。 |
| `Timeout` | DNS、代理握手或 STUN 请求超过设置的总超时。 |
| `UnsupportedServer` | STUN 服务器不支持完整的 RFC 5780 映射/过滤测试，不等同于节点不支持 UDP。 |

> Google 等部分公共 STUN 服务只支持基础 Binding。它们可以返回公网地址，但可能无法完成 NAT 类型分类。遇到 `UnsupportedServer` 时，请在设置中更换支持 RFC 5780 的 STUN 服务器后重试。

## 代理核心

构建脚本会打包固定版本并校验 SHA-256 的 Windows x64 Xray-core。Xray 用于兼容 TLS、REALITY、Vision、XHTTP、WebSocket 和 WireGuard 配置；SagerNet 核心用于 ShadowsocksR、SSH、插件及 Xray 已移除的旧传输。

## 从源码构建

### 环境要求

- Windows 10/11 x64
- Visual Studio 2022，并安装“使用 C++ 的桌面开发”和 MSBuild
- .NET SDK 10（已使用 `10.0.402` 验证；应用目标框架为 `net8.0-windows`）
- PowerShell 5.1 或更高版本
- 可访问依赖源和构建脚本所需下载地址的网络

在 **Developer PowerShell for Visual Studio** 中执行：

```powershell
git clone https://github.com/cbear2024/netch.git
cd netch
dotnet restore .\Netch\Netch.csproj
.\build.ps1 -Configuration Release -OutputPath release
```

完整发布文件位于 `release` 目录。`build.ps1` 会构建 .NET 前端、Redirector、RouteHelper 和所需代理核心，并生成可直接运行的目录结构。

默认每次都会重新编译各组件，避免打包旧二进制。CI 从缓存恢复 `Netch\bin`、`Redirector\bin`、`RouteHelper\bin` 后使用 `-UseCache` 跳过已有组件的编译：

```powershell
.\build.ps1 -Configuration Release -OutputPath release -UseCache
```

### 分组件调试构建

```powershell
dotnet build .\Netch\Netch.csproj -c Debug -p:Platform=x64
msbuild .\Redirector\Redirector.vcxproj /p:Configuration=Debug /p:Platform=x64
msbuild .\RouteHelper\RouteHelper.vcxproj /p:Configuration=Debug /p:Platform=x64
```

## 目录结构

| 目录 | 内容 |
| --- | --- |
| `Netch` | WinForms 界面、配置、节点管理和代理核心编排。 |
| `Redirector` | 基于 Netfilter 的 TCP/UDP 流量重定向组件。 |
| `RouteHelper` | 路由与网络辅助组件。 |
| `Other` | Xray、SagerNet 等代理核心的构建和打包脚本。 |
| `Storage` | 模式、翻译、STUN 列表和运行时资源。 |

## 故障排查

- 启动失败：确认以管理员身份运行，安全软件未阻止驱动或 `bin` 目录中的组件。
- TUN 模式网页首屏较慢：在 AioDNS 设置中优先使用 `udp://DNS:53`；`tcp://` 和 `tls://` 仍可用于需要 TCP DNS 的网络。UDP 返回截断响应时会自动回退到 TCP。
- TCP 可用但 UDP 失败：检查节点、服务端和模式是否支持 UDP，并查看日志中的 SOCKS UDP 或 STUN 错误。
- VLESS + WebSocket 无法连接：依次核对 UUID、路径、Host/SNI、TLS 和服务端反向代理配置。
- NAT 显示 `UnsupportedServer`：更换支持 RFC 5780 的 STUN 服务器；这通常不是代理节点故障。
- 构建失败：从 Visual Studio Developer PowerShell 运行，并确认 C++ x64 工具链与 .NET SDK 可用。

## 上游与社区

- 上游项目：[NetchX/Netch](https://github.com/NetchX/Netch)
- Telegram：[频道](https://t.me/netch_channel) · [群组](https://t.me/netch_group)

## 许可证

本项目基于 [GNU General Public License v3.0](LICENSE) 发布。第三方组件适用其各自许可证。
