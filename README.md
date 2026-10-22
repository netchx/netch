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
- .NET SDK 10（已使用 `10.0.402` 验证；应用目标框架为 `net6.0-windows`）
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
- TCP 可用但 UDP 失败：检查节点、服务端和模式是否支持 UDP，并查看日志中的 SOCKS UDP 或 STUN 错误。
- VLESS + WebSocket 无法连接：依次核对 UUID、路径、Host/SNI、TLS 和服务端反向代理配置。
- NAT 显示 `UnsupportedServer`：更换支持 RFC 5780 的 STUN 服务器；这通常不是代理节点故障。
- 构建失败：从 Visual Studio Developer PowerShell 运行，并确认 C++ x64 工具链与 .NET SDK 可用。

## 上游与社区

- 上游项目：[NetchX/Netch](https://github.com/NetchX/Netch)
- Telegram：[频道](https://t.me/netch_channel) · [群组](https://t.me/netch_group)

## 许可证

本项目基于 [GNU General Public License v3.0](LICENSE) 发布。第三方组件适用其各自许可证。
