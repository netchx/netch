# Netch

[![](https://img.shields.io/badge/Telegram-群组-blue.svg)](https://t.me/NetchX)
[![](https://img.shields.io/badge/Telegram-频道-blue.svg)](https://t.me/NetchXChannel)

游戏加速工具

## 目录

1. [下载与安装](#下载与安装)
2. [简介](#简介)
3. [使用方法](#使用方法)
4. [常见问题 \(Frequently Asked Questions\)](#常见问题-frequently-asked-questions)
5. [截图](#截图)
6. [依赖](#依赖)
7. [注释](#注释)

点击上箭头可返回目录。

## 下载与安装

当前发布版本为免安装版本，解压后点击 Netch.exe 即可使用，目前仅支持 Windows 

[最新版下载地址](https://github.com/netchx/Netch/releases/latest)

## 简介
Netch 是一款 Windows 平台的开源游戏加速工具，不同于 SSTap 那样需要通过添加规则来实现黑名单代理， Netch 原理更类似 [SocksCap64](https://www.sockscap64.com/homepage/) ，通过扫描游戏目录获得需要代理的进程名进行代理

与此同时 Netch 避免了 SSTap 的 NAT 问题 <escape><a name = "ref_1_s"><a href="#ref_1_d"><sup>[1]</sup></a></a></escape>，检查 NAT 类型 <escape><a name = "ref_2_s"><a href="#ref_2_d"><sup>[2]</sup></a></a></escape> 即可知道是否有 NAT 问题。使用 SSTap 加速部分 P2P 联机，对 NAT 类型有要求的游戏时，可能会因为 NAT 类型严格遇到无法加入联机，或者其他影响游戏体验的情况

进群提问前请务必先看下方使用方法和常见问题

## 使用方法
[USAGE.zh-CN.md](USAGE.zh-CN.md)

## 常见问题 (Frequently Asked Questions)
以下来自 Netch 版本发布频道[第 50 条消息](https://t.me/NetchXChannel/50)。

>- Q：我的系统无法运行（秒出启动失败）
>- A：是不是 64 位系统下着 32 位的包？
>- Q：好像是的，眼瞎了 ....
>- A：（艹，又一个浪费时间的）

>- Q：我的系统无法运行（打都打不开）
>- A：看下面，装一下运行库
>- Q：装了啊，提示已经安装，但是还是不行
>- A：建议您重装一下系统（已知有用户系统被玩坏了，安装其实根本没装上）

>- Q：我的疯狂报错
>- A：安装一下 .NET Framework 4.8 打上最新的 Visual C++ 合集先
>- Q：照做了，还是有问题
>- A：重装系统谢谢（已知有用户系统被玩坏了，安装其实根本没装上）

>- Q：我的无法正常加速进程
>- A：可能是您电脑上的其他加速器驱动和我的冲突了呢
>- Q：我都卸载了，但是还有问题
>- A：卸载是卸载不干净的，这边建议您重装系统谢谢

>- Q：有时候报错提示 ShadowsocksR 进程已停止运行
>- A：您好，这个问题我这里处理不了。我没法去修改 ssr-libev 的代码让其不异常退出

>- Q：为什么不能全局代理
>- A：请等待我加入 TUN/TAP 技术支持

>- Q：为什么订阅导入不完整？
>- A：导入后看看 logging 目录里的 application.log 吧（也许会暗示什么）
>- Q：啥也没有
>- A：私发订阅链接看看（@ConnectionRefused）

>如果重装系统不能解决问题。建议大哥考虑一下购买一台新电脑

## 截图
<escape><div title="主界面" align="middle"><img src="screenshots/main.png" height="80%" width="80%"></div><div align="middle">主界面</div></escape>

## 依赖
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)

## 注释
<escape><a name = "ref_1_d"><a href = "#ref_1_d">[1]</a></a>&nbsp;<a href = "#ref_1_s">&nbsp;↑&nbsp;</a>&nbsp;<a href = "https://www.right.com.cn/forum/thread-199299-1-1.html">NAT 原理</a></br><a name = "ref_2_d"><a href = "#ref_2_d">[2]</a></a>&nbsp;<a href = "#ref_2_s">&nbsp;↑&nbsp;</a>&nbsp;<a href = "https://github.com/HMBSbige/NatTypeTester">NAT 类型检测工具</a></escape>