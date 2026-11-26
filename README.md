<p align="center">
  <img src="Netch.App/Assets/Netch.png" width="112" alt="Netch" />
</p>

<h1 align="center">Netch</h1>

<p align="center">
  专为游戏加速打造的 Windows x64 开源网络代理客户端<br />
  An open-source Windows proxy client built for low-latency game acceleration.
</p>

<p align="center">
  <a href="https://github.com/NetchX/Netch/actions/workflows/build.yml"><img src="https://github.com/NetchX/Netch/actions/workflows/build.yml/badge.svg?branch=v2" alt="Build" /></a>
  <a href="https://github.com/NetchX/Netch/releases"><img src="https://img.shields.io/github/v/release/NetchX/Netch?include_prereleases&style=flat-square" alt="Release" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-GPLv3-blue?style=flat-square" alt="GPLv3" /></a>
  <img src="https://img.shields.io/badge/platform-Windows%20x64-0078D6?style=flat-square" alt="Windows x64" />
</p>

> [!IMPORTANT]
> Netch 需要管理员权限以加载网络驱动并配置系统路由。

## 核心能力

- **面向游戏优化的转发链路**：TCP 关闭 Nagle（`TCP_NODELAY`），UDP 散布/聚集转发与大缓冲区抗突发丢包，代理 DNS 复用长连接；加速期间自动提升转发线程与核心进程优先级并关闭 Windows 电源节流（EcoQoS）。
- **场景化一键加速**：将“加速模式 + 节点”组合为场景卡片，支持全局快捷键切换、托盘置顶、开机自启与启动即连。
- **内置游戏规则与精细分流**：自带“游戏合集”及数十款热门游戏与平台规则，进程模式支持子进程沿父进程链追溯（覆盖启动器 → 游戏本体 → 反作弊服务）及目标 IPv4 / IPv6 / CIDR 代理与绕过过滤。
- **实时连接监控与热规则调试**：提供「连接」页面实时查看活跃连接（独立上传 / 下载速率与点击列排序）与连接日志，可选开启 Windows Kernel ETW 观测未匹配 / 直连进程流量，支持在加速期间暂存并热应用进程与目标 IP 规则（含系统关键进程安全防护）。
- **日常网页与系统代理**：提供 PAC 自动分流、全局系统代理与仅本地端口（HTTP/SOCKS5 混合）三种网页代理策略，内置海外代理列表、精简 GFWList 与绕过中国大陆规则集，支持自定义域名规则/PAC 脚本及异常退出自动恢复系统代理。
- **多核心自动调度**：按节点协议与传输特性自动分流至 Mihomo、sing-box 或 Xray-core，开箱即用。
- **联机质量诊断**：内置混合测速（Hybrid / 真连接 / TCPing / ICMPing）、出口国家识别与 RFC 5780 UDP NAT 类型检测。
- **内建更新与规则同步**：应用内一键检查、更新与回滚核心组件，在线比对并同步游戏模式规则与 PAC 域名规则表，支持直连、代理与 GitHub 镜像通道。
- **现代桌面体验**：WinUI 3 原生界面与深浅色主题，支持剪贴板 URI、Clash YAML、订阅链接导入与二维码分享，内置 7 种界面语言（简体中文 / 繁體中文 / English / 日本語 / Русский / العربية / فارسی）并支持外置 `i18n/` 自定义词典。

## 工作模式

| 模式 | 机制 | 适用场景 |
| --- | --- | --- |
| **进程模式** | NetFilter 驱动按进程路径与目标 IP/CIDR 拦截 | PC 游戏首选，精准接管游戏与反作弊，支持实时连接监控与热规则 |
| **TUN 模式** | WinTUN + tun2socks + AioDNS | 难捕获游戏、UWP 与全局/分流路由 |
| **共享模式** | pcap2socks 抓包（需 Npcap 兼容） | 主机（PS / Xbox / Switch）及局域网 |
| **网页代理** | 混合端口 + WinINet / PAC 系统代理 | 浏览器与日常网页，免驱动、无 UDP 要求 |

## 支持协议

Netch 当前支持 **19 种** 代理协议，支持剪贴板 URI、Clash YAML 与订阅导入，并按特性自动选择对应核心：

| 协议 | UDP | 默认核心 | 扩展核心 |
| --- | :---: | --- | --- |
| **VLESS** / **VMess** / **Trojan** | ✅ | Mihomo | Xray-core · sing-box |
| **Shadowsocks** | ✅ UoT | Mihomo | Xray-core · sing-box |
| **ShadowsocksR** | ✅ | Mihomo | — |
| **ShadowTLS** | ⚠️ UoT | Mihomo | sing-box |
| **Hysteria2** / **TUIC** / **AnyTLS** | ✅ | Mihomo | sing-box |
| **TrustTunnel** / **Mieru** / **ShadowQUIC** | ✅ | Mihomo | — |
| **WireGuard** / **AmneziaWG** | ✅ | Mihomo | sing-box（WireGuard） |
| **NaiveProxy** | ❌ | sing-box | 附带 `libcronet.dll` |
| **MASQUE** | ✅ | sing-box | 需 `>= 1.15.0-alpha.7` |
| **SOCKS5** | ✅ UoT | 直连 / Mihomo | sing-box（UoT） |
| **HTTP / HTTPS** / **SSH** | ❌ | Mihomo | sing-box |

✅ 原生支持 UDP · ✅ UoT 支持 UDP 及 UoT · ⚠️ UoT 需开启 UoT · ❌ 不支持 UDP

<details>
<summary>查看多核心自动分流细则</summary>

- **VLESS / VMess / Trojan**：VLESS 标准 XHTTP 由 Mihomo 处理；XHTTP 高级特性（REALITY / HTTP/3 等）及 VMess / Trojan 的 XHTTP 由 Xray-core 处理；QUIC、明文 H2、Trojan H2 / 明文及自定义 PEM 由 sing-box 处理。
- **Shadowsocks / ShadowsocksR**：Shadowsocks 支持 `obfs-local`、`v2ray-plugin` 与 `ssd://`，XHTTP 由 Xray-core 处理，`v2ray-plugin` QUIC 模式及自定义 PEM 由 sing-box 处理；ShadowsocksR 支持 `ssr://` 与 Clash YAML 导入。
- **ShadowTLS**：v2/v3（含 UoT）由 Mihomo 处理；v1 或自定义 PEM 证书由 sing-box 处理。
- **Hysteria2 / TUIC / AnyTLS**：Hysteria2 短跳跃间隔（`<5s`）/ `hop-interval-max` / `bbr-profile` 及三者的自定义 PEM 证书由 sing-box 处理。
- **TrustTunnel / Mieru / ShadowQUIC**：均由内置 Mihomo 原生处理并支持 UDP（TrustTunnel 支持 `tt://` / `trusttunnel://`、HTTP/2、HTTP/3 QUIC、内联 PEM 与 ECH；Mieru 支持 `mierus://` / `mieru://`、`TCP`/`UDP` 传输与单段 `port-range`；ShadowQUIC 支持 `shadowquic://`、0-RTT 与 BBR/Brutal）。
- **WireGuard / AmneziaWG / SOCKS5 / MASQUE**：WireGuard 同地址族多接口地址由 sing-box 处理，AmneziaWG（支持 `amneziawg://` / `awg://`、`wg://` 自动识别与 Clash `amnezia-wg-option`）由 Mihomo 处理；SOCKS5 在进程 / TUN / 共享模式支持时直连、网页代理模式经本地混合端口转接、开启 UoT 时由 sing-box 处理；MASQUE 基于 CONNECT-IP，需在下载页替换为支持 `masque-client` 的 sing-box 版本（如 `>= 1.15.0-alpha.7`）。
- **HTTP / HTTPS / SSH**：HTTP 多值 Header / ALPN / Fingerprint / PEM，以及 SSH 自定义 `client-version` 由 sing-box 处理。

</details>

> [!NOTE]
> 游戏加速（进程 / TUN / 共享模式）通常依赖 UDP 转发。`HTTP / HTTPS`、`SSH`、`NaiveProxy` 不支持 UDP，适合用于网页代理模式；`ShadowTLS` 用于游戏加速需开启 UDP over TCP（UoT）。使用需要 UDP 的模式时，请同时确认服务端已开启 UDP 转发。

## 快速开始

1. 前往 [Releases](https://github.com/NetchX/Netch/releases) 下载 Windows x64 压缩包并完整解压。
2. 以管理员身份运行 `Netch.App.exe`。
3. 在「节点」页导入订阅、Clash YAML 或剪贴板分享链接，并执行测速。
4. 在「场景」页选择加速模式与节点，点击卡片即可开始加速；进程模式下可在「连接」页实时查看连接并热调整进程 / IP 规则。

## 从源码构建

需要 Windows 10 1809+ / Windows 11 x64、Visual Studio 2026（含“使用 C++ 的桌面开发”工作负载）、.NET 10 SDK、PowerShell 5.1+（重新构建 AioDNS 另需 Go 1.26+ 与 x64 MinGW GCC）。

```powershell
git clone https://github.com/NetchX/Netch.git
cd Netch
.\build.ps1 -Configuration Release -OutputPath release
```

构建完成后，直接运行 `release\Netch.App.exe`。

## 致谢

Netch 的实现离不开以下优秀的开源项目、驱动与数据维护者，谨此致谢：

### 核心与转发组件

- [Mihomo](https://github.com/MetaCubeX/mihomo) — 默认多协议代理核心
- [sing-box](https://github.com/SagerNet/sing-box) — NaiveProxy、MASQUE 及扩展协议核心（附带 [NaiveProxy](https://github.com/klzgrad/naiveproxy) `libcronet.dll` 运行时）
- [Xray-core](https://github.com/XTLS/Xray-core) — XHTTP 等特性代理核心
- [tun2socks](https://github.com/xjasonlyu/tun2socks) — TUN 模式流量转发引擎
- [pcap2socks](https://github.com/zhxie/pcap2socks) — 局域网共享模式转发引擎
- [miekg/dns](https://github.com/miekg/dns) — AioDNS 底层 DNS 解析库

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

- Telegram：[频道](https://t.me/netch_channel) · [群组](https://t.me/netch_group)
- 本项目基于 [GNU General Public License v3.0](LICENSE) 发布，各第三方组件遵循其各自许可证（详见 [NOTICE.txt](NOTICE.txt)）。
