<p align="center">
  <img src="Netch.App/Assets/Netch.png" width="112" alt="Netch" />
</p>

<h1 align="center">Netch</h1>

<p align="center">
  专为游戏加速打造的 Windows x64 开源网络代理客户端
</p>

<p align="center">
  <a href="https://github.com/NetchX/Netch/actions/workflows/build.yml"><img src="https://github.com/NetchX/Netch/actions/workflows/build.yml/badge.svg?branch=v2" alt="Build" /></a>
  <a href="https://github.com/NetchX/Netch/releases"><img src="https://img.shields.io/github/v/release/NetchX/Netch?include_prereleases&style=flat-square" alt="Release" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-GPLv3-blue?style=flat-square" alt="GPLv3" /></a>
  <img src="https://img.shields.io/badge/platform-Windows%20x64-0078D6?style=flat-square" alt="Windows x64" />
</p>

<p align="center">
  <b>简体中文</b> · <a href="README.en.md">English</a>
</p>

<p align="center">
  <a href="#快速开始">快速开始</a> ·
  <a href="#核心特性">核心特性</a> ·
  <a href="#工作模式">工作模式</a> ·
  <a href="#支持协议">支持协议</a> ·
  <a href="#安装与环境依赖">安装与依赖</a> ·
  <a href="#faq">常见问题</a> ·
  <a href="#从源码构建">从源码构建</a> ·
  <a href="https://github.com/NetchX/Netch/releases">下载 Releases</a>
</p>

> [!IMPORTANT]
> 需 **Windows 10 1809+ / Windows 11 x64** 与**管理员权限**；请完整解压后运行 `Netch.App.exe`。

## 核心特性

- **低延迟游戏转发**：面向游戏优化的 TCP/UDP 转发与进程优先级提升；内置百余套游戏规则，支持子进程链回溯与目标 IP/CIDR 过滤。
- **场景化加速**：将「节点 + 模式」组合为场景卡片，支持多模式组合、托盘菜单、全局快捷键、开机自启与启动即连。
- **多核心与自定义扩展**：按协议与传输特性自动调度 **Mihomo**（默认）、**sing-box** 或 **Xray-core**，支持 **External CLI** 与按协议自定义核心。
- **连接监控与网页代理**：进程模式支持实时连接速率、ETW 直连观测与热调规则；网页代理支持 PAC 分流、全局代理与本地混合端口（HTTP/SOCKS5）。
- **测速诊断与内置维护**：内置 Hybrid / URLTest / TCPing / ICMPing 测速、GeoIP 与 RFC 5780 NAT 检测，支持应用内核心/规则更新回滚与运行依赖自检。
- **WinUI 3 桌面体验**：深浅色主题，支持剪贴板 URI、Clash YAML、二维码分享与首次向导，内置 7 种语言并支持外置 `i18n/` 词典。

## 工作模式

| 模式 | 机制 | 适用场景 | 额外条件 |
| --- | --- | --- | --- |
| **进程模式** | NetFilter 驱动（`nfdriver.sys`）按进程与目标 IP 拦截 | PC 游戏首选，支持实时连接监控与热规则 | 系统 BFE 服务需运行 |
| **TUN 模式** | WinTUN + tun2socks + 内置 AioDNS（UDP / TCP / DoT / DoH） | 难捕获游戏、UWP 与全局/分流路由 | CPU 需支持 AVX2 (`x86-64-v3`) |
| **共享模式** | `pcap2socks` 抓包转发 | 主机（PS / Xbox / Switch）及局域网设备 | 需安装 Npcap 与 VC++ x64 |
| **网页代理** | 本地混合端口 + WinINet / PAC 系统代理 | 浏览器与日常网页，免驱动、无 UDP 要求 | 无 |

> [!TIP]
> 单场景支持多选**进程模式**并叠加**网页代理**或**共享模式**并发运行；**TUN 模式**可多选合并路由，但与上述三类模式互斥。

## 支持协议

内置支持 **19 种**代理协议，并支持 **External CLI 外部节点** 与 **按协议自定义核心**：

| 协议 | UDP | 默认核心 | 扩展 / 备注 |
| --- | :---: | --- | --- |
| **VLESS** / **VMess** / **Trojan** | ✅ | Mihomo | Xray-core（XHTTP）· sing-box |
| **Shadowsocks** | ✅ UoT | Mihomo | Xray-core（XHTTP）· sing-box |
| **ShadowsocksR** | ✅ | Mihomo | — |
| **ShadowTLS** | ⚠️ UoT | Mihomo | sing-box（v1 / 自定义 PEM） |
| **Hysteria2** / **TUIC** / **AnyTLS** | ✅ | Mihomo | sing-box |
| **TrustTunnel** / **Mieru** / **ShadowQUIC** | ✅ | Mihomo | — |
| **WireGuard** / **AmneziaWG** | ✅ | Mihomo | sing-box（WireGuard 多接口地址） |
| **NaiveProxy** | ❌ | sing-box | 随附 `libcronet.dll` |
| **MASQUE** | ✅ | sing-box | 需在下载页替换为 `>= 1.15.0-alpha.7` |
| **SOCKS5** | ✅ UoT | 直连 / Mihomo | sing-box（UoT） |
| **HTTP / HTTPS** / **SSH** | ❌ | Mihomo | sing-box |
| **External CLI**（外部命令行节点） | 按核心而定 | 第三方 CLI | 可直出 SOCKS5；网页代理经 Mihomo 桥接 HTTP |

✅ 原生支持 UDP · ✅ UoT 支持 UDP 及 UoT · ⚠️ UoT 需开启 UoT · ❌ 不支持 UDP

<details>
<summary>查看多核心自动分流与自定义核心细则</summary>

- **VLESS / VMess / Trojan**：VLESS 标准 XHTTP 由 Mihomo 处理；XHTTP 高级特性（REALITY / HTTP/3 等）及 VMess / Trojan 的 XHTTP 由 Xray-core 处理；QUIC、明文 H2、Trojan H2/明文及自定义 PEM 由 sing-box 处理。
- **Shadowsocks / ShadowsocksR**：Shadowsocks 支持 `obfs-local`、`v2ray-plugin` 与 `ssd://`，XHTTP 由 Xray-core 处理，`v2ray-plugin` QUIC 模式与自定义 PEM 由 sing-box 处理；ShadowsocksR 支持 `ssr://` 与 Clash YAML 导入。
- **ShadowTLS / Hysteria2 / TUIC / AnyTLS**：ShadowTLS v2/v3 由 Mihomo 处理，v1 或自定义 PEM 由 sing-box 处理；Hysteria2 短跳跃间隔（`<5s`）/ `hop-interval-max` / `bbr-profile` 及三者的自定义 PEM 由 sing-box 处理。
- **TrustTunnel / Mieru / ShadowQUIC / AmneziaWG**：均由内置 Mihomo 原生处理并支持 UDP（TrustTunnel 支持 `tt://` / HTTP/2 / HTTP/3 QUIC / ECH；Mieru 支持 `mierus://` / `TCP`/`UDP`；ShadowQUIC 支持 `shadowquic://` / 0-RTT；AmneziaWG 支持 `amneziawg://` / `awg://`）。
- **WireGuard / SOCKS5 / MASQUE / HTTP / SSH**：WireGuard 同地址族多接口地址、SOCKS5 UoT、HTTP 多值 Header / ALPN / Fingerprint / PEM 与 SSH 自定义 `client-version` 由 sing-box 处理；MASQUE 需自行将 sing-box 更新至 `>= 1.15.0-alpha.7`。
- **按协议自定义核心**：在「设置 → 自定义核心」中可按协议绑定外部核心及配置模板，按「节点 → 订阅 → 协议默认 → 内置核心」优先级解析。

</details>

> [!NOTE]
> 游戏加速（进程 / TUN / 共享模式）通常依赖 UDP 转发；`HTTP / HTTPS`、`SSH`、`NaiveProxy` 不支持 UDP，`ShadowTLS` 需开启 UoT，同时需确认服务端已开启 UDP 转发。

## 安装与环境依赖

| 发行包 / 场景 | 手动依赖 | 安装与说明 |
| --- | :---: | --- |
| **自带框架版** `*-winui-x64-self-contained.zip`（推荐） | **零依赖** | 已内置 `.NET 10` 与 `Windows App SDK 2.7`，除**共享模式**外解压即用。 |
| **系统框架版** `*-winui-x64-framework-dependent.zip` | **1 项** | 需先安装 **[.NET 10 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/10.0)**（`winget install Microsoft.DotNet.DesktopRuntime.10`，仅装基础 `.NET Runtime` 无法启动）。 |
| **仅启用共享模式**（主机 / 局域网，两包均同） | **2 项** | 1. **[Npcap](https://npcap.com/)**：安装时**必须勾选 `Install Npcap in WinPcap API-compatible Mode`**。<br />2. **[VC++ 2015–2022 Redistributable (x64)](https://aka.ms/vs/17/release/vc_redist.x64.exe)**（`winget install Microsoft.VCRedist.2015+.x64`），用于 `bin/pcap2socks.exe`。 |

<a id="faq"></a>

<details>
<summary>常见问题</summary>

- **提示缺少 `bin/` 文件**：通常为安全软件隔离了 `nfdriver.sys`、`Redirector.bin`、`nfapi.dll`、`RouteHelper.bin`、`wintun.dll` 或 `bin/sing_box/libcronet.dll`，将目录加入白名单后重新解压即可。
- **老电脑或虚拟机 TUN 启动失败**：`bin/tun2socks-windows-amd64-v3.exe` 要求 CPU 支持 **AVX2（`x86-64-v3`）**（虚拟机需开启 AVX2 透传），不支持时请改用进程模式或网页代理。
- **ARM64 Windows**：可在 x64 仿真下运行主界面与**网页代理模式**，但无法加载 x64 内核驱动（`nfdriver.sys` / `wintun.dll`），进程模式与 TUN 模式不可用。

</details>

## 快速开始

1. 从 [Releases](https://github.com/NetchX/Netch/releases) 下载对应发行包，解压后运行 `Netch.App.exe`。
2. 通过首次向导或在「节点」页导入订阅、Clash YAML 或剪贴板 URI 并测速。
3. 在「场景」页选择模式与节点，点击卡片即可开始加速。

## 从源码构建

需 Visual Studio 2026（“使用 C++ 的桌面开发”工作负载）与 .NET 10 SDK：

```powershell
git clone https://github.com/NetchX/Netch.git
cd Netch
.\build.ps1 -Configuration Release -OutputPath release                                        # 自带 .NET 10 运行时（默认）
.\build.ps1 -Configuration Release -OutputPath release-framework-dependent -SelfContained $false # 系统 .NET 10 Desktop Runtime
```

## 致谢

Netch 的实现离不开以下优秀的开源项目、驱动与数据维护者，谨此致谢：

### 核心与转发组件

- [Mihomo](https://github.com/MetaCubeX/mihomo) — 默认多协议代理核心
- [sing-box](https://github.com/SagerNet/sing-box) — NaiveProxy、MASQUE 及扩展协议核心（附带 [NaiveProxy](https://github.com/klzgrad/naiveproxy) `libcronet.dll` 运行时）
- [Xray-core](https://github.com/XTLS/Xray-core) — XHTTP 等特性代理核心
- [tun2socks](https://github.com/xjasonlyu/tun2socks) — TUN 模式流量转发引擎
- [pcap2socks](https://github.com/zhxie/pcap2socks) — 局域网共享模式转发引擎

### 驱动与系统依赖

- [NetFilter SDK](https://www.netfiltersdk.com/) — 进程模式内核驱动（`nfdriver.sys` / `nfapi.dll`）
- [WinTUN](https://www.wintun.net/) — TUN 模式三层虚拟网卡驱动（`wintun.dll`）
- [Npcap](https://nmap.org/npcap/) / [WinPcap](https://www.winpcap.org/) — 共享模式运行所需的抓包驱动

### 外部数据与检测

- [Loyalsoldier/geoip](https://github.com/Loyalsoldier/geoip) & [MaxMind GeoLite2](https://dev.maxmind.com/geoip/geolite2-free-geolocation-data) — 节点出口国家/地区识别数据库
- [Loyalsoldier/v2ray-rules-dat](https://github.com/Loyalsoldier/v2ray-rules-dat) — 网页代理 PAC 域名规则表（`proxy-list`、`gfw`、`direct-list`）来源
- [NatTypeTester / Stun.Net](https://github.com/HMBSbige/NatTypeTester) — RFC 5780 UDP NAT 检测实现与预设 STUN 列表
- [dnsmasq-china-list](https://github.com/felixonmars/dnsmasq-china-list) — AioDNS 国内域名分流列表来源
- [china_ip_list](https://github.com/17mon/china_ip_list) — 国内 IP 段分流规则参考来源

## 社区与许可证

Telegram：[频道](https://t.me/netch_channel) · [群组](https://t.me/netch_group) · 基于 [GNU General Public License v3.0](LICENSE) 发布，各第三方组件遵循各自许可证（详见 [NOTICE.txt](NOTICE.txt)）。
