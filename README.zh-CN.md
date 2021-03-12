# Netch
[![群组](https://img.shields.io/badge/Telegram-群组-green)](https://t.me/Netch_Discuss_Group)
[![频道](https://img.shields.io/badge/Telegram-频道-blue)](https://t.me/Netch)
[![平台](https://img.shields.io/badge/平台-windows-orange.svg)](https://github.com/NetchX/Netch)
[![版本](https://img.shields.io/github/v/release/NetchX/Netch)](https://github.com/NetchX/Netch/releases)
[![下载](https://img.shields.io/github/downloads/NetchX/Netch/total.svg)](https://github.com/NetchX/Netch/releases)
[![Netch CI](https://github.com/NetchX/Netch/workflows/Netch%20CI/badge.svg)](https://github.com/NetchX/Netch/actions)
[![License](https://img.shields.io/badge/license-MIT-yellow.svg)](LICENSE)

[文档网站](https://netch.org/) [常见问题](https://netch.org/#/docs/zh-CN/faq)

游戏加速工具

## TOC
- [Netch](#Netch)
	- [TOC](#TOC)
	- [简介](#简介)
    - [赞助商](#赞助商)
	- [捐赠](#捐赠)
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
- VLESS

与此同时 Netch 避免了 SSTap 的 NAT 问题 ，检查 NAT 类型即可知道是否有 NAT 问题。使用 SSTap 加速部分 P2P 联机，对 NAT 类型有要求的游戏时，可能会因为 NAT 类型严格遇到无法加入联机，或者其他影响游戏体验的情况

## 赞助商
<a href="https://www.jetbrains.com/?from=Netch"><img src="images/jetbrains-variant-4.svg" alt="JetBrains" width="200"/></a>

- [RabbitHosts](https://rabbithosts.com/cart.php)
- [ManSora](https://www.mansora.co/cart.php)
- [NyanCat](https://nyancat.info/register)

## 捐赠
- XMR *48ju3ELNZEa6wwPBMexCJ9G218BGY2XwhH6B6bmkFuJ3QgM4hPw2Pra35jPtuBZSc7SLNWeBpiWJZWjQeMAiLnTx2tH2Efx*

## 依赖
- [.NET Framework 4.8 (Windows 10 1903+ 已包含)](https://dotnet.microsoft.com/download/dotnet-framework/thank-you/net48-offline-installer)

## 语言支持
Netch 内置 en-US 和 zh-CN，外置 zh-TW 等，默认根据系统语言选择语言。

[Netch 外置语言仓库](https://github.com/NetchX/NetchTranslation) ，欢迎提供其他语言的翻译

## 引用
- [core](https://github.com/aiocloud/core)
- [aiodns](https://github.com/aiocloud/aiodns)
- [Redirector](https://github.com/aiocloud/Redirector)
- [go-tun2socks](https://github.com/eycorsican/go-tun2socks)
- [shadowsocks-libev](https://github.com/shadowsocks/shadowsocks-libev)
- [shadowsocksr-libev](https://github.com/shadowsocksrr/shadowsocksr-libev)
- [Xray-core](https://github.com/XTLS/Xray-core)
- [trojan](https://github.com/trojan-gfw/trojan)
- [ACL4SSR](https://github.com/ACL4SSR/ACL4SSR)
- [PAC](https://github.com/HMBSbige/Text_Translation/blob/master/ShadowsocksR/ss_white.pac)
- [dnsmasq-china-list](https://github.com/felixonmars/dnsmasq-china-list)
- [tap-windows6](https://github.com/OpenVPN/tap-windows6)
- [Privoxy](https://www.privoxy.org/)
- [NatTypeTester](https://github.com/HMBSbige/NatTypeTester)
- [NetFilter SDK](https://netfiltersdk.com/)
- [pcap2socks](https://github.com/zhxie/pcap2socks)
