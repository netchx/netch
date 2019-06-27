# Netch

[![](https://img.shields.io/badge/Telegram-Group-blue.svg)](https://t.me/NetchX)
[![](https://img.shields.io/badge/Telegram-Channel-blue.svg)](https://t.me/NetchXChannel)

Game accelerator

[简体中文](docs/README.zh-Hans.md)

## Description

Netch is an open source game accelerator. Unlike [SSTap](https://www.sockscap64.com/sstap-enjoy-gaming-enjoy-sstap/), which needs to add rules to function as a blacklist proxy, Netch is more similar to [Sockscap64](https://www.sockscap64.com/homepage/), which can scan the game directory to get their process names specifically and forward their network traffic through the proxy server. Now supports Socks5, Shadowsocks, ShadowsocksR. VMess is on the way.

As well, Netch avoid the restricted NAT problem caused by SSTap<escape><a name = "ref_1_s"><a href="#ref_1_d"><sup>[1]</sup></a></a><a name = "ref_2_s"><a href="#ref_2_d"><sup>[2]</sup></a></a></escape>. When using SSTap to speed up some P2P gaming connections or the game is required for that kind of open NAT type, you may experience some bad situations such as unable to join the game.

## Usage

[简体中文](docs/usage.zh-Hans.md)

[English](docs/usage.en.md)

## Screenshots

![](docs/screenshots/main.png)

## Requirements

- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)

## Explanatory note

Click up arrow to go back.

<escape><a name = "ref_1_d"><a href = "#ref_1_d">[1]</a></a>&nbsp;<a href = "#ref_1_s">&nbsp;↑&nbsp;</a>&nbsp;<a href = "https://en.wikipedia.org/wiki/Network_address_translation#Methods_of_translation">Network address translation wikipedia</a></br><a name = "ref_2_d"><a href = "#ref_2_d">[2]</a></a>&nbsp;<a href = "#ref_2_s">&nbsp;↑&nbsp;</a>&nbsp;<a href = "https://github.com/HMBSbige/NatTypeTester">NATTypeTester</a></escape>