<p align="center">
  <img src="Netch.App/Assets/Netch.png" width="112" alt="Netch" />
</p>

<h1 align="center">Netch</h1>

<p align="center">
  An open-source Windows x64 proxy client built for low-latency game acceleration.
</p>

<p align="center">
  <a href="https://github.com/NetchX/Netch/actions/workflows/build.yml"><img src="https://github.com/NetchX/Netch/actions/workflows/build.yml/badge.svg?branch=v2" alt="Build" /></a>
  <a href="https://github.com/NetchX/Netch/releases"><img src="https://img.shields.io/github/v/release/NetchX/Netch?include_prereleases&style=flat-square" alt="Release" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-GPLv3-blue?style=flat-square" alt="GPLv3" /></a>
  <img src="https://img.shields.io/badge/platform-Windows%20x64-0078D6?style=flat-square" alt="Windows x64" />
</p>

<p align="center">
  <a href="README.md">简体中文</a> · <b>English</b>
</p>

<p align="center">
  <a href="#quick-start">Quick Start</a> ·
  <a href="#features">Features</a> ·
  <a href="#modes">Modes</a> ·
  <a href="#supported-protocols">Protocols</a> ·
  <a href="#installation--requirements">Installation</a> ·
  <a href="#faq">FAQ</a> ·
  <a href="#build-from-source">Build</a> ·
  <a href="https://github.com/NetchX/Netch/releases">Releases</a>
</p>

> [!IMPORTANT]
> Requires **Windows 10 1809+ / Windows 11 x64** and **Administrator privileges**. Extract the entire archive before running `Netch.App.exe`.

## Features

- **Low-Latency Game Forwarding**: Game-oriented TCP/UDP forwarding with process priority boosting, 100+ built-in game rule presets, child-process tree tracking, and destination IP/CIDR filtering.
- **Scene-Based Acceleration**: Combine nodes and modes into Scene cards with multi-mode stacking, tray menu controls, global hotkeys, launch at startup, and auto-connect on start.
- **Multi-Core Routing & Custom Extensions**: Automatically dispatches **Mihomo** (default), **sing-box**, or **Xray-core** based on protocol and transport capabilities, plus **External CLI** nodes and per-protocol custom cores.
- **Connection Monitor & Web Proxy**: Real-time per-connection throughput, ETW direct-traffic inspection, and hot rule editing in Process Mode; PAC split routing, global proxy, and local mixed port (HTTP/SOCKS5) in Web Proxy mode.
- **Latency Testing & Built-In Maintenance**: Built-in Hybrid / URLTest / TCPing / ICMPing benchmarks, GeoIP lookup, RFC 5780 NAT type detection, in-app core/rule updates with rollback, and runtime dependency checks.
- **WinUI 3 Desktop Experience**: Light/dark themes, clipboard URI / Clash YAML import, QR code sharing, first-run onboarding wizard, 7 built-in languages, and external `i18n/` dictionary support.

## Modes

| Mode | Mechanism | Best For | Extra Requirements |
| --- | --- | --- | --- |
| **Process Mode** | NetFilter driver (`nfdriver.sys`) intercepting by process & target IP | Recommended for PC games; supports live connection monitor & hot rules | Windows BFE service running |
| **TUN Mode** | WinTUN + tun2socks + built-in AioDNS (UDP / TCP / DoT / DoH) | Hard-to-hook games, UWP apps, and global/split routing | CPU with AVX2 (`x86-64-v3`) |
| **Share Mode** | `pcap2socks` packet capture & forwarding | Consoles (PS / Xbox / Switch) and LAN devices | Npcap & VC++ x64 Redistributable |
| **Web Proxy** | Local mixed port + WinINet / PAC system proxy | Browsers and everyday web traffic; driverless, no UDP needed | None |

> [!TIP]
> A single Scene can combine multiple **Process Mode** rules and run concurrently with **Web Proxy** or **Share Mode**. Multiple **TUN Mode** rules can be merged together, but TUN Mode is mutually exclusive with the other three modes.

## Supported Protocols

Built-in support for **19 proxy protocols**, plus **External CLI** nodes and **per-protocol custom cores**:

| Protocol | UDP | Default Core | Extensions / Notes |
| --- | :---: | --- | --- |
| **VLESS** / **VMess** / **Trojan** | ✅ | Mihomo | Xray-core (XHTTP) · sing-box |
| **Shadowsocks** | ✅ UoT | Mihomo | Xray-core (XHTTP) · sing-box |
| **ShadowsocksR** | ✅ | Mihomo | — |
| **ShadowTLS** | ⚠️ UoT | Mihomo | sing-box (v1 / custom PEM) |
| **Hysteria2** / **TUIC** / **AnyTLS** | ✅ | Mihomo | sing-box |
| **TrustTunnel** / **Mieru** / **ShadowQUIC** | ✅ | Mihomo | — |
| **WireGuard** / **AmneziaWG** | ✅ | Mihomo | sing-box (WireGuard multi-address) |
| **NaiveProxy** | ❌ | sing-box | Bundled with `libcronet.dll` |
| **MASQUE** | ✅ | sing-box | Requires updating sing-box to `>= 1.15.0-alpha.7` in Downloads |
| **SOCKS5** | ✅ UoT | Direct / Mihomo | sing-box (UoT) |
| **HTTP / HTTPS** / **SSH** | ❌ | Mihomo | sing-box |
| **External CLI** (custom command-line node) | Depends on core | Third-party CLI | Direct SOCKS5 output; Web Proxy bridged to HTTP via Mihomo |

✅ Native UDP · ✅ UoT Supports UDP & UoT · ⚠️ UoT Requires UoT enabled · ❌ No UDP support

<details>
<summary>Multi-core auto-dispatch & custom core rules</summary>

- **VLESS / VMess / Trojan**: Standard VLESS XHTTP runs on Mihomo; advanced XHTTP features (REALITY / HTTP/3, etc.) and VMess / Trojan XHTTP run on Xray-core; QUIC, plaintext H2, Trojan H2/plaintext, and custom PEM certificates run on sing-box.
- **Shadowsocks / ShadowsocksR**: Shadowsocks supports `obfs-local`, `v2ray-plugin`, and `ssd://`; XHTTP runs on Xray-core; `v2ray-plugin` QUIC mode and custom PEM certificates run on sing-box; ShadowsocksR supports `ssr://` and Clash YAML import.
- **ShadowTLS / Hysteria2 / TUIC / AnyTLS**: ShadowTLS v2/v3 runs on Mihomo, while v1 or custom PEM runs on sing-box; Hysteria2 short hop intervals (`<5s`), `hop-interval-max`, `bbr-profile`, and custom PEM for all three run on sing-box.
- **TrustTunnel / Mieru / ShadowQUIC / AmneziaWG**: Handled natively by the bundled Mihomo core with UDP support (TrustTunnel: `tt://` / HTTP/2 / HTTP/3 QUIC / ECH; Mieru: `mierus://` / `TCP`/`UDP`; ShadowQUIC: `shadowquic://` / 0-RTT; AmneziaWG: `amneziawg://` / `awg://`).
- **WireGuard / SOCKS5 / MASQUE / HTTP / SSH**: WireGuard multiple addresses per address family, SOCKS5 UoT, HTTP multi-value headers / ALPN / Fingerprint / PEM, and SSH custom `client-version` run on sing-box; MASQUE requires manually updating sing-box to `>= 1.15.0-alpha.7`.
- **Per-Protocol Custom Cores**: Bind external core binaries and config templates per protocol in **Settings → Custom Cores**, resolved in priority order: Node → Subscription → Protocol Default → Built-in Core.

</details>

> [!NOTE]
> Game acceleration (Process / TUN / Share modes) typically requires UDP forwarding. `HTTP / HTTPS`, `SSH`, and `NaiveProxy` do not support UDP; `ShadowTLS` requires UoT; also ensure UDP forwarding is enabled on your server.

## Installation & Requirements

| Package / Scenario | Manual Dependencies | Notes |
| --- | :---: | --- |
| **Self-Contained** `*-winui-x64-self-contained.zip` (Recommended) | **None** | Bundles `.NET 10` and `Windows App SDK 2.7`. Extract and run (except **Share Mode**). |
| **Framework-Dependent** `*-winui-x64-framework-dependent.zip` | **1** | Requires **[.NET 10 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/10.0)** (`winget install Microsoft.DotNet.DesktopRuntime.10`; base `.NET Runtime` alone will not launch). |
| **Share Mode Only** (Console / LAN, applies to both packages) | **2** | 1. **[Npcap](https://npcap.com/)**: You **must check `Install Npcap in WinPcap API-compatible Mode`** during setup.<br />2. **[VC++ 2015–2022 Redistributable (x64)](https://aka.ms/vs/17/release/vc_redist.x64.exe)** (`winget install Microsoft.VCRedist.2015+.x64`), required by `bin/pcap2socks.exe`. |

<a id="faq"></a>

<details>
<summary>FAQ</summary>

- **Missing `bin/` files warning**: Antivirus software commonly quarantines `nfdriver.sys`, `Redirector.bin`, `nfapi.dll`, `RouteHelper.bin`, `wintun.dll`, or `bin/sing_box/libcronet.dll`. Add the Netch folder to your antivirus exclusion list and re-extract the archive.
- **TUN mode fails to start on older PCs or VMs**: `bin/tun2socks-windows-amd64-v3.exe` requires a CPU with **AVX2 (`x86-64-v3`)** (enable AVX2 passthrough in VMs). If unsupported, use Process Mode or Web Proxy instead.
- **Windows on ARM64**: The UI and **Web Proxy mode** run under x64 emulation, but x64 kernel drivers (`nfdriver.sys` / `wintun.dll`) cannot load, so Process Mode and TUN Mode are unavailable.

</details>

## Quick Start

1. Download the release package from [Releases](https://github.com/NetchX/Netch/releases), extract the full archive, and run `Netch.App.exe`.
2. Use the first-run wizard or open the **Nodes** page to import a subscription, Clash YAML, or clipboard URI, then run a latency test.
3. Open the **Scenes** page, select a mode and node, and click the Scene card to start accelerating.

## Build from Source

Requires Visual Studio 2026 ("Desktop development with C++" workload) and the .NET 10 SDK:

```powershell
git clone https://github.com/NetchX/Netch.git
cd Netch
.\build.ps1 -Configuration Release -OutputPath release                                        # Self-contained .NET 10 runtime (default)
.\build.ps1 -Configuration Release -OutputPath release-framework-dependent -SelfContained $false # System .NET 10 Desktop Runtime
```

## Acknowledgments

Netch builds upon the work of these open-source projects, driver vendors, and dataset maintainers:

### Core & Forwarding Components

- [Mihomo](https://github.com/MetaCubeX/mihomo) — Default multi-protocol proxy core
- [sing-box](https://github.com/SagerNet/sing-box) — Proxy core for NaiveProxy, MASQUE, and extended protocols (with [NaiveProxy](https://github.com/klzgrad/naiveproxy) `libcronet.dll` runtime)
- [Xray-core](https://github.com/XTLS/Xray-core) — Proxy core for XHTTP and related features
- [tun2socks](https://github.com/xjasonlyu/tun2socks) — Traffic forwarding engine for TUN Mode
- [pcap2socks](https://github.com/zhxie/pcap2socks) — Packet forwarding engine for LAN Share Mode

### Drivers & System Dependencies

- [NetFilter SDK](https://www.netfiltersdk.com/) — Kernel driver for Process Mode (`nfdriver.sys` / `nfapi.dll`)
- [WinTUN](https://www.wintun.net/) — Layer 3 virtual network adapter driver for TUN Mode (`wintun.dll`)
- [Npcap](https://nmap.org/npcap/) / [WinPcap](https://www.winpcap.org/) — Packet capture driver required by Share Mode

### External Data & Diagnostics

- [Loyalsoldier/geoip](https://github.com/Loyalsoldier/geoip) & [MaxMind GeoLite2](https://dev.maxmind.com/geoip/geolite2-free-geolocation-data) — Geolocation database for exit node country/region detection
- [Loyalsoldier/v2ray-rules-dat](https://github.com/Loyalsoldier/v2ray-rules-dat) — Domain lists (`proxy-list`, `gfw`, `direct-list`) for Web Proxy PAC rules
- [NatTypeTester / Stun.Net](https://github.com/HMBSbige/NatTypeTester) — RFC 5780 UDP NAT detection and preset STUN server list
- [dnsmasq-china-list](https://github.com/felixonmars/dnsmasq-china-list) — China domain list source for AioDNS split DNS
- [china_ip_list](https://github.com/17mon/china_ip_list) — Reference list for China IP range split routing

## Community & License

Telegram: [Channel](https://t.me/netch_channel) · [Group](https://t.me/netch_group) · Released under the [GNU General Public License v3.0](LICENSE). Third-party components follow their respective licenses (see [NOTICE.txt](NOTICE.txt)).
