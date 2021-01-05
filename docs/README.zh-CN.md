# Netch
[![群组](https://img.shields.io/badge/Telegram-群组-green)](https://t.me/Netch_Discuss_Group)
[![频道](https://img.shields.io/badge/Telegram-频道-blue)](https://t.me/Netch)
[![平台](https://img.shields.io/badge/平台-windows-orange.svg)](https://github.com/NetchX/Netch)
[![版本](https://img.shields.io/github/v/release/NetchX/Netch)](https://github.com/NetchX/Netch/releases)
[![下载](https://img.shields.io/github/downloads/NetchX/Netch/total.svg)](https://github.com/NetchX/Netch/releases)
[![Netch CI](https://github.com/NetchX/Netch/workflows/Netch%20CI/badge.svg)](https://github.com/NetchX/Netch/actions)
[![License](https://img.shields.io/badge/license-MIT-yellow.svg)](LICENSE)

游戏加速工具

[网站](https://netch.org/)

## TOC
- [Netch](#Netch)
	- [TOC](#TOC)
	- [简介](#简介)
    - [赞助商](#赞助商)
	- [捐赠](#捐赠)
    - [新手入门](Quickstart.zh-CN.md)
    - [进阶用法](Advanced_Usage.zh-CN.md)
	- [依赖](#依赖)
    - [语言支持](#语言支持)

## 简介
Netch 是一款 Windows 平台的开源游戏加速工具，Netch 可以实现类似 SocksCap64 那样的进程代理，也可以实现 SSTap 那样的全局 TUN/TAP 代理，和 Shadowsocks-Windows 那样的本地 Socks5，HTTP 和系统代理

至于连接至远程服务器的代理协议，目前 Netch 支持以下代理协议
- Socks5
- Shadowsocks
- ShadowsocksR
- Trojan
- VMess
- VLess

与此同时 Netch 避免了 SSTap 的 NAT 问题 ，检查 NAT 类型即可知道是否有 NAT 问题。使用 SSTap 加速部分 P2P 联机，对 NAT 类型有要求的游戏时，可能会因为 NAT 类型严格遇到无法加入联机，或者其他影响游戏体验的情况


需要更多特性请移步魔改仓库 [Netch-ForOwnUse](https://github.com/AmazingDM/Netch-ForOwnUse)，

## 赞助商
<a href="https://www.jetbrains.com/?from=Netch"><img src="../.github/jetbrains-variant-4.svg" alt="JetBrains" width="200"/></a>

- [RabbitHosts](https://rabbithosts.com/cart.php)
- [ManSora](https://www.mansora.co/cart.php)
- [NyanCat](https://nyancat.info/register)

## 捐赠
- XMR *48ju3ELNZEa6wwPBMexCJ9G218BGY2XwhH6B6bmkFuJ3QgM4hPw2Pra35jPtuBZSc7SLNWeBpiWJZWjQeMAiLnTx2tH2Efx*

## 新手入门
[新手入门教程](Quickstart.zh-CN.md)

## 进阶用法
[进阶教程](Advanced_Usage.zh-CN.md)
## 依赖
- [Visual C++ 运行库合集](https://www.google.com/search?q=Visual+C%2B%2B+%E8%BF%90%E8%A1%8C%E5%BA%93%E5%90%88%E9%9B%86)
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/thank-you/net48-offline-installer)
- [TAP-Windows](https://build.openvpn.net/downloads/releases/tap-windows-9.21.2.exe)

## 语言支持
Netch 支持多种语言，在启动时会根据系统语言选择自身语言。如果需要手动切换语言，可以在启动时加入命令行参数，命令行参数为目前支持的语言代码，可以去 [NetchTranslation/i18n](https://github.com/NetchX/NetchTranslation/tree/master/i18n) 文件夹下查看外部支持的语言代码文件。Netch 目前内置 en-US，zh-CN，外置 zh-TW。欢迎大家为 [NetchTranslation](https://github.com/NetchX/NetchTranslation) 提供其他语言的翻译

## 引用
- [core](https://github.com/aiocloud/core)
- [aiodns](https://github.com/aiocloud/aiodns)
- [Redirector](https://github.com/aiocloud/Redirector)
- [go-tun2socks](https://github.com/eycorsican/go-tun2socks)
- [shadowsocks-libev](https://github.com/shadowsocks/shadowsocks-libev)
- [shadowsocksr-libev](https://github.com/shadowsocksrr/shadowsocksr-libev)
- [v2ray-core](https://github.com/v2ray/v2ray-core)
- [trojan](https://github.com/trojan-gfw/trojan)
- [ACL4SSR](https://github.com/ACL4SSR/ACL4SSR)
- [PAC](https://github.com/HMBSbige/Text_Translation/blob/master/ShadowsocksR/ss_white.pac)
- [dnsmasq-china-list](https://github.com/felixonmars/dnsmasq-china-list)
- [tap-windows6](https://github.com/OpenVPN/tap-windows6)
- [Privoxy](https://www.privoxy.org/)
- [NatTypeTester](https://github.com/HMBSbige/NatTypeTester)
- [NetFilter SDK](https://netfiltersdk.com/)
