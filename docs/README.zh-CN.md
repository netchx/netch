# Netch

[![](https://img.shields.io/badge/Telegram-群组-blue.svg)](https://t.me/NetchX)
[![](https://img.shields.io/badge/Telegram-频道-blue.svg)](https://t.me/NetchXChannel)

游戏加速工具

## 目录

1. [下载与安装](#下载与安装)
2. [简介](#简介)
3. [使用方法](#使用方法)
4. [常见问题 \(Frequently Asked Questions\)](#常见问题-frequently-asked-questions)
   - 4.1 [错误报告类问题](#错误报告类问题)
     - 4.1.1 [无法运行](#无法运行)
     - 4.1.2 [订阅无法导入](#订阅无法导入)
     - 4.1.3 [无法进入游戏 / 模式无法使用](#无法进入游戏---模式无法使用)
     - 4.1.4 [NAT 类型限制](#NAT-类型限制)
     - 4.1.5 [Steam / 浏览器无法正常打开页面](#Steam---浏览器无法正常打开页面)
   - 4.2 [功能建议类问题](#功能建议类问题)
     - 4.2.1 [加入全局功能](#加入全局功能)
     - 4.2.2 [加入 VMess 代理功能](#加入-VMess-代理功能)
     - 4.2.3 [加入任务栏托盘功能](#加入任务栏托盘功能)
     - 4.2.4 [加入本地代理功能](#加入本地代理功能)
     - 4.2.5 [加入更多的 SSR 参数支持](#加入更多的-SSR-参数支持)
     - 4.2.6 [加入 macos 支持](#加入-macos-支持)
5. [截图](#截图)
6. [依赖](#依赖)
7. [证书](#证书)
8. [注释](#注释)

## 下载与安装

当前发布版本为免安装版本，解压后点击 Netch.exe 即可使用，目前仅支持 Windows。

**注意**

- Windows-64 位系统安装 x64 版本
- Windows-32 位系统安装 x86 版本
- 否则你会遇到驱动问题

[最新版下载地址](https://github.com/netchx/Netch/releases/latest)

## 简介

Netch 是一款 Windows 平台的开源游戏加速工具，不同于 SSTap 那样需要通过添加规则来实现黑名单代理， Netch 原理更类似 [SocksCap64](https://www.sockscap64.com/homepage/) ，通过扫描游戏目录获得需要代理的进程名进行代理

与此同时 Netch 避免了 SSTap 的 NAT 问题 <escape><a name = "ref_1_s"><a href="#ref_1_d"><sup>[1]</sup></a></a></escape>，检查 NAT 类型 <escape><a name = "ref_2_s"><a href="#ref_2_d"><sup>[2]</sup></a></a></escape> 即可知道是否有 NAT 问题。使用 SSTap 加速部分 P2P 联机，对 NAT 类型有要求的游戏时，可能会因为 NAT 类型严格遇到无法加入联机，或者其他影响游戏体验的情况

进群提问前请务必先看下方使用方法和常见问题

## 使用方法

[USAGE.zh-CN.md](USAGE.zh-CN.md)

## 常见问题 (Frequently Asked Questions)

编辑自 Netch 版本发布频道[第 50 条消息](https://t.me/NetchXChannel/50)。

### 错误报告类问题

#### 无法运行

>- Q：我的系统无法运行(秒出启动失败)
>- A：是不是 64 位系统下着 32 位的包？
>- Q：好像是的，眼瞎了 ……
>- A：……

>- Q：我的 win7 系统无法运行(秒出启动失败)，已确认是系统和软件版本位数一致
>- A：如果是驱动问题，详见 [issue #14](https://github.com/netchx/Netch/issues/14) ，安装补丁 kb4503292 或者将系统更新至最新

>- Q：我的系统无法运行(打都打不开)
>- A：看下面，装一下运行库
>- Q：装了啊，提示已经安装，但是还是不行
>- A：建议您重装一下系统（已知有用户系统被玩坏了，安装其实根本没装上）

>- Q：我的疯狂报错
>- A：安装一下 .NET Framework 4.8 打上最新的 Visual C++ 合集先
>- Q：照做了，还是有问题
>- A：重装系统谢谢（已知有用户系统被玩坏了，安装其实根本没装上）

>- Q：有时候报错提示 ShadowsocksR 进程已停止运行
>- A：您好，这个问题我这里处理不了。我没法去修改 ssr-libev 的代码让其不异常退出

>如果重装系统不能解决问题。建议大哥考虑一下购买一台新电脑

#### 订阅无法导入

>- Q：为什么订阅导入不完整？
>- A：导入后看看 logging 目录里的 application.log 吧(也许会暗示什么)
>- Q：啥也没有
>- A：私发订阅链接看看（加群后联系@ConnectionRefused），一般来讲是订阅链接中有不被识别的 unicode 字符导致的，类似的问题参见 [issue #7](https://github.com/netchx/Netch/issues/7) ，这可能会是一个功能改进，但是目前没有时间表

#### 无法进入游戏 / 模式无法使用

>- Q： xxx 游戏扫描后仍然无法代理
>- A：除了自带的模式经测试后可用，其他游戏确实会出现代理后反而无法连接进入游戏的情况，譬如守望先锋必须只代理 Overwatch Launcher.exe 而不是其他 exe 才能进游戏。还有就是确保你的代理参数中没有不适合游戏的部分，譬如 shadowsocks 参数中就不建议 timeout 参数设置过短，否则会影响战网客户端的正常连接

#### NAT 类型限制

>- Q： xxx 游戏对 NAT 类型有要求，你们这个加速器代理后 NAT 类型还是严格 xxx ，我甚至用 NATTypeTester 测过了，还是不行 xxx
>- A：经过测试这款软件是可以做到 Full cone 的 NATType 。如果你自己测试不行，需要考虑三个方面的问题
>   - 第一个是你的服务器是否支持 Full cone 的 NATType ，这可以通过其他软件的测试来佐证，譬如使用 Sockscap64 之类
>   - 第二个是你本地的网络环境问题，经测试联通数据和长宽等网络，即使在代理后也无法做到 Full cone ，就算服务器是支持 Full cone 的。这种情况下你可能需要切换全局的 VPN 代理工具（WireGuard ， Badvpn ， Openvpn ， tinyfecVPN 等），或者更换本地网络环境
>   - 第三个是某些游戏的代理模式有问题，可能遇到各种玄学问题，参见[上方](#无法进入游戏-模式无法使用)

#### Steam / 浏览器无法正常打开页面

>- Q：用来加速 Steam / 浏览器，结果无法正常打开页面
>- A：有人测试可行有人测试不可行。首先声明一下，本软件的功能主要不是用来代理 Steam / 浏览器打开页面的，建议使用专门的工具，如 [steamcommunity 302](https://www.dogfight360.com/blog/686/)，浏览器则建议用 shadowsocks-windows， clash for windows 等等，你甚至可以尝试 ~~shadowsocks-windows over Netch~~，这可能会是一个功能改进，但是目前没有时间表

### 功能建议类问题

#### 加入全局功能

>- Q：为什么不能全局代理
>- A：请等待我加入 TUN/TAP 技术支持（不会很快开发出来）

#### 加入 VMess 代理功能

>- Q：说好的支持 V2Ray 呢，怎么没有，~~还搞小弹框~~
>- A： VMess 参数看得脑壳疼，~~但既然写了支持那么还是会迟早加入的~~
>- A：目前可以[通过 Socks5 代理进行中转](USAGE.zh-CN.md#新建代理配置)

#### 加入任务栏托盘功能

>- Q：为什么不加入任务栏托盘功能
>- A：~~项目的主要开发者不喜欢用任务栏托盘~~
>- A：会考虑加入，但不会是高优先级，你可以考虑通过 Pull Request 的方式为本软件加入该支持

#### 加入本地代理功能

>- Q：我想在电脑上代理斯维奇
>- A：~~腾讯加速器好像可以免费加速主机游戏~~
>- A：会考虑加入，但不会是高优先级，你可以考虑通过 Pull Request 的方式为本软件加入该支持

#### 加入更多的 SSR 参数支持

>- Q：希望能加入更多的 SSR 参数支持，我那个机场的订阅好多节点无法导入 [issue #11](https://github.com/netchx/Netch/issues/11)
>- A：这得看 [shadowsocksr-libev](https://github.com/shadowsocksr-backup/shadowsocksr-libev) 对这方面的支持程度了，目前没有把 shadowsocksr 相关代码集成进软件内的计划，你同样可以考虑[通过 Socks5 代理进行中转](USAGE.zh-CN.md#新建代理配置)

#### 加入 macos 支持

>- Q：希望加入 macos 支持
>- A：这位亲请考虑赞助一台 mac 给开发者使用呢

## 截图

<escape><div title="主界面" align="middle"><img src="screenshots/main.png" height="80%" width="80%"></div><div align="middle">主界面</div></escape>

## 依赖

- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)

## 证书

- [MIT](../LICENSE)

## 注释

<escape><a name = "ref_1_d"><a href = "#ref_1_d">[1]</a></a>&nbsp;<a href = "#ref_1_s">&nbsp;↑&nbsp;</a>&nbsp;<a href = "https://www.right.com.cn/forum/thread-199299-1-1.html">NAT 原理</a></br><a name = "ref_2_d"><a href = "#ref_2_d">[2]</a></a>&nbsp;<a href = "#ref_2_s">&nbsp;↑&nbsp;</a>&nbsp;<a href = "https://github.com/HMBSbige/NatTypeTester">NAT 类型检测工具</a></escape>
