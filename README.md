# Netch
[![](https://img.shields.io/badge/Telegram-Channel-blue.svg)](https://t.me/Netch)

Game accelerator

[简体中文](docs/README.zh-CN.md) (此版本内容更丰富)

## TOC
- [Netch](#netch)
	- [TOC](#toc)
	- [Description](#description)
	- [Sponsor](#sponsor)
	- [Screenshots](#screenshots)
	- [Requirements](#requirements)

## Description

Netch is an open source game accelerator. Unlike SSTap, which needs to add rules to function as a blacklist proxy, Netch is more similar to SocksCap64, which can scan the game directory to get their process names specifically and forward their network traffic through the proxy server. Now supports Socks5, Shadowsocks, ShadowsocksR, VMess.

As well, Netch avoid the restricted NAT problem caused by SSTap. You can use an NATTypeTester to test out what your NAT type is. When using SSTap to speed up some P2P gaming connections or the game is required for that kind of open NAT type, you may experience some bad situations such as unable to join the game.

## Sponsor

[![ManSora](docs/sponsor/mansora.jpg)](https://www.mansora.net/cart.php)
[![Across-GFW](docs/sponsor/across-gfw.jpg)](https://across-gfw.com/register?aff=4739)

这是我所选择的服务器提供商，IPLC 专线翻墙、稳定、速度快、价格便宜。欢迎大家使用我的推广链接前去注册：[这里](https://across-gfw.com/register?aff=4739)

## Screenshots

![](docs/screenshots/main_en.png)

## Requirements

- Microsoft Visual C++ Runtime
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)