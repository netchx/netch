# Netch
[![Group](https://img.shields.io/badge/Telegram-Group-green)](https://t.me/Netch_Discuss_Group)
[![Channel](https://img.shields.io/badge/Telegram-Channel-blue)](https://t.me/Netch)
[![Platform](https://img.shields.io/badge/platform-windows-orange.svg)](https://github.com/NetchX/Netch)
[![Version](https://img.shields.io/github/v/release/NetchX/Netch)](https://github.com/NetchX/Netch/releases)
[![Downloads](https://img.shields.io/github/downloads/NetchX/Netch/total.svg)](https://github.com/NetchX/Netch/releases)
[![Netch CI](https://github.com/NetchX/Netch/workflows/Netch%20CI/badge.svg)](https://github.com/NetchX/Netch/actions)
[![License](https://img.shields.io/badge/license-MIT-yellow.svg)](LICENSE)

[文档网站](https://netch.org/) [常见问题](https://netch.org/#/docs/zh-CN/faq) 

[中文说明](README.zh-CN.md)

Game network accelerator

## TOC
- [Netch](#netch)
	- [TOC](#toc)
	- [Description](#description)
	- [Sponsor](#sponsor)
	- [Donate](#donate)
	- [Screenshots](#screenshots)
	- [Requirements](#requirements)
	- [Quote](#quote)

## Description
Netch is an open source game network accelerator. Unlike SSTap, which needs to add rules to function as a blacklist proxy, Netch is more similar to SocksCap64, which can scan the game directory to get their process names specifically and forward their network traffic through the proxy server

Currently supports the following protocols
- Socks5
- Shadowsocks
- ShadowsocksR
- Trojan
- VMess
- VLESS

As well, Netch avoid the restricted NAT problem caused by SSTap. You can use an NATTypeTester to test out what your NAT type is. When using SSTap to speed up some P2P gaming connections or the game is required for that kind of open NAT type, you may experience some bad situations such as unable to join the game

## Sponsor
<a href="https://www.jetbrains.com/?from=Netch"><img src="jetbrains.svg" alt="JetBrains" width="200"/></a>

- [RabbitHosts](https://rabbithosts.com/cart.php)
- [ManSora](https://www.mansora.co/cart.php)
- [NyanCat](https://nyancat.info/register)

## Donate
- XMR *48ju3ELNZEa6wwPBMexCJ9G218BGY2XwhH6B6bmkFuJ3QgM4hPw2Pra35jPtuBZSc7SLNWeBpiWJZWjQeMAiLnTx2tH2Efx*

## Screenshots
![](screenshots/main.png)

## Requirements
- [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0/runtime)

## Quote
- [core](https://github.com/aiocloud/core)
- [aiodns](https://github.com/aiocloud/aiodns)
- [tun2socks](https://github.com/aiocloud/tun2socks)
- [Redirector](https://github.com/aiocloud/Redirector)
- [RouteHelper](https://github.com/aiocloud/RouteHelper)
- [NatTypeTester](https://github.com/HMBSbige/NatTypeTester)
- [NetFilter SDK](https://netfiltersdk.com)
- [pcap2socks](https://github.com/zhxie/pcap2socks)
- [shadowsocks-libev](https://github.com/shadowsocks/shadowsocks-libev)
- [shadowsocksr-libev](https://github.com/shadowsocksrr/shadowsocksr-libev)
- [trojan](https://github.com/trojan-gfw/trojan)
- [xray-core](https://github.com/XTLS/Xray-core)
- [dnsmasq-china-list](https://github.com/felixonmars/dnsmasq-china-list)

[![Stargazers over time](https://starchart.cc/NetchX/Netch.svg)](https://starchart.cc/NetchX/Netch)     
