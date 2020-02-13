# Netch
[![](https://img.shields.io/badge/Telegram-频道-blue.svg)](https://t.me/Netch)
> ~~issue已关，有问题可进tele群问，不保证回答，不保证解决，咕~~

游戏加速工具

## TOC
- [Netch](#netch)
	- [TOC](#toc)
	- [简介](#%e7%ae%80%e4%bb%8b)
	- [赞助商](#%e8%b5%9e%e5%8a%a9%e5%95%86)
    - [新手入门](Basic-usage.md)
    - [进阶用法](https://github.com/NormanBB/NetchMode/blob/master/docs/README.zh-CN.md)
	- [依赖](#%e4%be%9d%e8%b5%96)
    - [语言支持](#语言支持)
    
## 简介

Netch 是一款 Windows 平台的开源游戏加速工具，Netch 可以实现类似 SocksCap64 那样的进程代理，也可以实现 SSTap 那样的全局 TUN/TAP 代理，和 Shadowsocks-Windows 那样的本地 Socks5，HTTP 和系统代理。至于连接至远程服务器的代理协议，目前 Netch 支持以下代理协议：Shadowsocks，VMess，Socks5，ShadowsocksR

与此同时 Netch 避免了 SSTap 的 NAT 问题 ，检查 NAT 类型即可知道是否有 NAT 问题。使用 SSTap 加速部分 P2P 联机，对 NAT 类型有要求的游戏时，可能会因为 NAT 类型严格遇到无法加入联机，或者其他影响游戏体验的情况

## 赞助商
开发不易，以下为恰饭时间

[![NyanCAT](sponsor/nyancat.jpg)](https://nyancat.info)

NyanCAT Network，全中转高质量节点，多条低倍率节点保证流量无忧，节点极低延迟涵盖五大洲。Netflix 视频党，游戏党，海外回国党必备，无需年付，月付 19 元起
[Telegram 群组](https://t.me/NyanCaaaat) 

[![ManSora](sponsor/mansora.jpg)](https://www.mansora.net/cart.php)

## 新手入门

[新手入门教程](Basic-usage.md)

## 进阶用法
[进阶教程](https://github.com/NormanBB/NetchMode/blob/master/docs/README.zh-CN.md)
## 依赖
- [Visual C++ 运行库合集](https://www.google.com/search?q=Visual+C%2B%2B+%E8%BF%90%E8%A1%8C%E5%BA%93%E5%90%88%E9%9B%86)
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/thank-you/net48-offline-installer)
- [TAP-Windows](https://build.openvpn.net/downloads/releases/tap-windows-9.21.2.exe)

## 语言支持

Netch 支持多种语言，在启动时会根据系统语言选择自身语言。如果需要手动切换语言，可以在启动时加入命令行参数，命令行参数为目前支持的语言代码，可以去 [NetchTranslation/i18n](https://github.com/NetchX/NetchTranslation/tree/master/i18n) 文件夹下查看外部支持的语言代码文件。Netch 目前内置 en-US，zh-CN，外置 zh-TW。欢迎大家为 [NetchTranslation](https://github.com/NetchX/NetchTranslation) 提供其他语言的翻译


