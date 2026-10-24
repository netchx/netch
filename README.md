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
> 这是 Netch v2.0.0 社区维护版本，并非上游官方发行版。程序需要管理员权限来加载网络驱动和修改系统路由。

当前维护者：[@cbear2024](https://github.com/cbear2024) · 当前版本：**v2.0.0** · 项目地址：[cbear2024/netch](https://github.com/cbear2024/netch)

最新发布：[Netch v2.0.0](https://github.com/cbear2024/netch/releases/tag/v2.0.0) · 上游 PR：[BoyceLig/Netch#5](https://github.com/BoyceLig/Netch/pull/5)

## 主要特性

- 按进程接管游戏或应用流量，无需修改目标程序。
- 支持 SOCKS5、HTTP、Shadowsocks、ShadowsocksR、Trojan、VMess、VLESS、WireGuard、Hysteria2、TUIC、AnyTLS、Naive、MASQUE 和 Sing-box 等协议。
- 集成 Xray-core，支持 TLS、REALITY、Vision、WebSocket、XHTTP 等常用传输方式。
- 提供进程代理、共享代理、TUN 和 Web 代理模式。
- TUN 模式支持 IPv4/IPv6 地址、DNS、服务器出口路由和 IPv6 CIDR 规则。
- 显示 HTTP 延迟、出口地址与 UDP NAT 检测结果。
- 集成 tun2socks v3、Wintun、Redirector 和 RouteHelper，适配 Windows x64。
- 保留 SagerNet 核心以兼容 ShadowsocksR、SSH、插件和部分旧传输。

### 本分支优化

- 为游戏 TCP 连接启用 `TCP_NODELAY`，减少小包交互延迟。
- UDP 转发采用散布/聚集发送，减少每包复制与临时内存分配。
- 支持最大合法 UDP 数据报，并容错处理 Windows UDP reset 与异常数据报。
- SOCKS 和 STUN 地址并行解析，DNS 缓存支持并发访问。
- UDP NAT 检测使用明确的单步与总超时，支持取消、IPv4 优先和 IPv6 回退。
- 停止代理或检测失败时能够可靠回收资源，避免界面长期停留在“Testing”。
- 加强 tun2socks 启停、失败清理、路由复用和 DNS 路由日志，便于定位启动失败问题。
- 订阅更新支持使用当前代理访问订阅地址，降低网络受限环境下更新失败的概率。

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
- [Hysteria2](https://v2.hysteria.network/)
- [TUIC](https://github.com/EAimTY/tuic)
- AnyTLS、Naive、MASQUE、HTTP 和 Sing-box

## 快速开始

1. 从 [Releases](https://github.com/cbear2024/netch/releases/latest) 下载 `Netch-v2.0.0-win-x64.zip`。
2. **完整解压**压缩包，保持 `Netch.exe`、`bin`、`mode` 和 `i18n` 目录结构。
3. 右键 `Netch.exe`，选择“以管理员身份运行”。
4. 导入订阅或手动添加节点。
5. 选择节点、工作模式和需要代理的进程，然后点击启动。
6. 点击状态栏中的 NAT 结果可以重新执行 UDP 检测。

> `Netch-v2.0.0-win-x64.exe` 是单文件版本，适合测试或已有完整运行目录的用户；它不包含外部代理核心、Wintun、tun2socks 和模式资源。普通用户应下载完整 zip 压缩包。

### TUN 与 IPv6

TUN 模式默认启用 IPv6。可以在“选项”中配置 IPv4 地址、网关、DNS，以及 IPv6 地址、网关和前缀长度。启动时 Netch 会为节点服务器、DNS、绕过规则和模式规则创建对应的 IPv4/IPv6 路由；停止时只清理本次启动创建的路由。

如果本机网络或节点不支持 IPv6，可以在设置中关闭 IPv6，或者只使用 IPv4 节点和 IPv4 CIDR 规则。

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

## 代理核心与运行文件

完整发布包的 `bin` 目录包含运行所需的代理核心和网络组件：Xray-core、V2Ray/SagerNet、AioDNS、pcap2socks、Redirector、RouteHelper、tun2socks v3、Wintun 以及 Netfilter 驱动。不要只复制 `Netch.exe` 到其他目录运行。

构建脚本会打包固定版本并校验 SHA-256 的 Windows x64 Xray-core。Xray 用于兼容 TLS、REALITY、Vision、XHTTP、WebSocket 和 WireGuard 配置；SagerNet 核心用于 ShadowsocksR、SSH、插件及 Xray 已移除的旧传输。

## 从源码构建

### 环境要求

- Windows 10/11 x64
- Visual Studio 2022，并安装“使用 C++ 的桌面开发”和 MSBuild
- .NET 8 SDK（应用目标框架为 `net8.0-windows10.0.17763.0`）
- PowerShell 5.1 或更高版本
- 可访问依赖源和构建脚本所需下载地址的网络

在 **Developer PowerShell for Visual Studio** 中执行：

```powershell
git clone https://github.com/cbear2024/netch.git
cd netch
dotnet restore .\Netch\Netch.csproj
.\build.ps1 -Configuration Release -OutputPath release
```

完整发布文件位于 `release` 目录。`build.ps1` 会构建 .NET 前端、Redirector、RouteHelper 和所需代理核心，并生成可直接运行的目录结构。发布目录必须整体分发，不能只分发其中的 `Netch.exe`。

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
| `Storage` | 模式、翻译、STUN 列表、Wintun、tun2socks 和运行时资源。 |
| `logging` | 运行时生成的 `application.log` 和 `tun2socks.log`。 |
| `data` | 本地设置、临时配置和最近使用记录。 |

## 故障排查

- 启动失败：确认以管理员身份运行，安全软件未阻止驱动或 `bin` 目录中的组件。
- 提示“Please extract all files then run the program”：说明只运行了单独的 exe，请下载并完整解压 zip 压缩包。
- 提示 `tun2socks start failed`：确认 `bin/tun2socks-windows-amd64-v3.exe`、`bin/wintun.dll` 存在，并检查安全软件是否拦截。
- 提示 `Failed to create TUN route`：检查是否以管理员身份运行、TUN 地址/网关/前缀是否正确，以及是否有其他 TUN 或代理程序占用路由。
- TCP 可用但 UDP 失败：检查节点、服务端和模式是否支持 UDP，并查看日志中的 SOCKS UDP 或 STUN 错误。
- VLESS + WebSocket 无法连接：依次核对 UUID、路径、Host/SNI、TLS 和服务端反向代理配置。
- NAT 显示 `UnsupportedServer`：更换支持 RFC 5780 的 STUN 服务器；这通常不是代理节点故障。
- 提交问题时，请附上 Netch 菜单“帮助”中的日志，或压缩 `logging` 目录；日志中通常包含节点启动、路由、DNS 和 tun2socks 的具体错误。
- 构建失败：从 Visual Studio Developer PowerShell 运行，并确认 C++ x64 工具链与 .NET SDK 可用。

## 上游与社区

- 上游项目：[BoyceLig/Netch](https://github.com/BoyceLig/Netch)
- 本版本上游 PR：[BoyceLig/Netch#5](https://github.com/BoyceLig/Netch/pull/5)
- Telegram：[频道](https://t.me/netch_channel) · [群组](https://t.me/netch_group)

## 许可证

本项目基于 [GNU General Public License v3.0](LICENSE) 发布。第三方组件适用其各自许可证。
