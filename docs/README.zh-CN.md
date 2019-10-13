# Netch
[![](https://img.shields.io/badge/Telegram-频道-blue.svg)](https://t.me/NetchX)

游戏加速工具

## 目录

1. [下载与安装](#下载与安装)
2. [简介](#简介)
3. [使用方法](#使用方法)
4. [常见问题 （Frequently Asked Questions）](#常见问题-frequently-asked-questions)
   - 4.1 [错误报告类问题](#错误报告类问题)
     - 4.1.1 [无法运行](#无法运行)
     - 4.1.2 [订阅无法导入](#订阅无法导入)
     - 4.1.3 [进程模式下无法进入游戏 / 无法成功代理](#进程模式下无法进入游戏--无法成功代理)
       - 4.1.3.1 [进程模式规则问题](#进程模式规则问题)
       - 4.1.3.2 [进程名重复问题](#进程名重复问题)
       - 4.1.3.3 [shadowsocks 参数问题](#shadowsocks-参数问题)
       - 4.1.3.4 [bin/Redirector.exe 问题](#binredirectorexe-问题)
       - 4.1.3.5 [进程模式以外的方法](#进程模式以外的方法)
     - 4.1.4 [NAT 类型限制](#NAT-类型限制)
     - 4.1.5 [Steam / 浏览器无法正常打开页面](#Steam--浏览器无法正常打开页面)
     - 4.1.6 [UWP 应用无法代理](#UWP-应用无法代理)
   - 4.2 [功能建议类问题](#功能建议类问题)
     - 4.2.1 [加入本地代理功能](#加入本地代理功能)
     - 4.2.2 [加入更多的 SSR 参数支持](#加入更多的-SSR-参数支持)
     - 4.2.3 [加入 macos 支持](#加入-macos-支持)
5. [截图](#截图)
6. [依赖](#依赖)
7. [证书](#证书)
8. [注释](#注释)

## 下载与安装

当前发布版本为免安装版本，解压后点击 Netch.exe 即可使用，目前仅支持 Windows

**注意**

- Windows 64 位系统使用 x64 版本
- Windows 32 位系统使用 x86 版本
- 否则你会遇到驱动问题

[最新版下载地址](https://github.com/netchx/Netch/releases)

## 简介

Netch 是一款 Windows 平台的开源游戏加速工具，Netch 可以实现类似 [SocksCap64](https://www.sockscap64.com/homepage/) 那样的进程代理，也可以实现 [SSTap](https://github.com/mayunbaba2/SSTap-beta-setup) 那样的全局 TUN/TAP 代理，和 [shadowsocks-windows](https://github.com/shadowsocks/shadowsocks-windows) 那样的本地 Socks5，HTTP 和系统代理。至于连接至远程服务器的代理协议，目前 Netch 支持以下代理协议：Shadowsocks，Vmess，Socks5，ShadowsocksR

与此同时 Netch 避免了 SSTap 的 NAT 问题 <escape><a name = "ref_1_s"><a href="#ref_1_d"><sup>[1]</sup></a></a></escape>，检查 NAT 类型 <escape><a name = "ref_2_s"><a href="#ref_2_d"><sup>[2]</sup></a></a></escape> 即可知道是否有 NAT 问题。使用 SSTap 加速部分 P2P 联机，对 NAT 类型有要求的游戏时，可能会因为 NAT 类型严格遇到无法加入联机，或者其他影响游戏体验的情况

[更新日志](CHANGELOG.zh-CN.md)

进群提问前请务必先看下方使用方法和常见问题

## 使用方法

包括模式说明，驱动更新，进程模式创建的方法等

[NetchMode/docs/README.zh-CN.md](https://github.com/NetchX/NetchMode/blob/master/docs/README.zh-CN.md)

## 常见问题 （Frequently Asked Questions）

编辑自 Netch 版本发布频道[第 50 条消息](https://t.me/NetchXChannel/50)

### 错误报告类问题

#### 无法运行

>- Q：我的系统无法运行(秒出启动失败)
>- A：是不是 64 位系统下着 32 位的包？
>- Q：好像是的，眼瞎了 ……
>- A：……

>- Q：我的 win7 系统无法运行(秒出启动失败)，已确认是系统和软件版本位数一致
>- A：如果是驱动问题，详见 [issue #14](https://github.com/netchx/Netch/issues/14) ，安装补丁 kb4503292 或者将系统更新至最新

>- Q：我的系统无法运行（打都打不开）
>- A：看下面，装一下运行库
>- Q：装了啊，提示已经安装，但是还是不行
>- A：建议您重装一下系统（已知有用户系统被玩坏了，安装其实根本没装上）

>- Q：我的疯狂报错
>- A：安装一下 .NET Framework 4.8 打上最新的 Visual C++ 合集先
>- Q：照做了，还是有问题
>- A：重装系统谢谢（已知有用户系统被玩坏了，安装其实根本没装上）

>- Q：有时候报错提示 ShadowsocksR 进程已停止运行
>- A：您好，这个问题我这里处理不了，我没法去修改 ssr-libev 的代码让其不异常退出，未来版本也会取消内置的 ShadowsocksR 支持，参考[加入更多的 SSR 参数支持](#加入更多的-SSR-参数支持)

>如果重装系统不能解决问题。建议大哥考虑一下购买一台新电脑

#### 订阅无法导入

>- Q：为什么订阅导入不完整？
>- A：导入后看看 logging 目录里的 application.log 吧（也许会暗示什么）
>- Q：啥也没有
>- A：私发订阅链接看看（加群后联系 @ConnectionRefused），一般来讲是订阅链接中有不被识别的 unicode 字符导致的，类似的问题参见 [issue #7](https://github.com/netchx/Netch/issues/7) ，这可能会是一个功能改进，但是目前没有时间表

#### 进程模式下无法进入游戏 / 无法成功代理

>- Q： xxx 游戏扫描后仍然无法代理
>- A：除了自带的模式经测试后可用，其他游戏确实会出现代理后反而无法连接进入游戏的情况

##### 进程模式规则问题

譬如守望先锋必须只代理 Overwatch Launcher.exe 而不是其他 exe 才能进游戏

##### 进程名重复问题

你代理的进程名需要和 Netch 使用到的 exe 名称不一样，否则可能会发生代理回环。譬如 `bin` 文件夹下的 `Shadowsocks.exe`，如果你使用 `Shadowsocks` 代理，模式中就不应该出现 `Shadowsocks.exe` 这样的进程名。你可以通过修改你要代理的 exe 的名称，或者替换为进程名的全路径名（譬如 `C:\xxx\xxx.exe`）来避免这个问题

##### shadowsocks 参数问题

譬如 shadowsocks 参数中就不建议 timeout 参数设置过短，否则会影响战网客户端的正常连接，建议删掉该参数保持默认值即可

##### bin/Redirector.exe 问题

**关于 `bin/Redirector.exe` 的新 issue 请统一到 [issue #152](https://github.com/NetchX/Netch/issues/152) 按照格式来回复**

该文件是闭源的，主要是负责和底层 Netfilter SDK 的控制，其各个版本之间还有细微差距，经反馈，较为稳定的为 1.0.9-STABLE - 1.2.4-STABLE（无流量统计）和 1.2.9 的版本，以下为推荐的旧版本下载链接，请大家自行尝试。下载后，只需将 `bin/Redirector.exe` 覆盖即可

只有 1.3.0 及以后和 1.2.4-STABLE 及以前的 `bin/Redirector.exe` 有处理进程所在中文路径的能力，如果需要使用不支持中文路径的 `bin/Redirector.exe`，请自行修改进程所在路径

请根据 Netch 的 x86/x64 版本来下载 `bin/Redirector.exe`

1.2.9 `bin/Redirector.exe`

- x64 版[下载链接](https://github.com/NetchX/Netch/raw/349c44f8947e5f6aae8677b2ea93ea7eb441a537/binaries/x64/Redirector.exe)
- x86 版[下载链接](https://github.com/NetchX/Netch/raw/349c44f8947e5f6aae8677b2ea93ea7eb441a537/binaries/x86/Redirector.exe)

1.0.9-STABLE - 1.2.4-STABLE `bin/Redirector.exe`

- x64 版[下载链接](https://github.com/NetchX/Netch/raw/acb4bc24651509c21558420d97865262e959bc0c/binaries/x64/Redirector.exe)
- x86 版[下载链接](https://github.com/NetchX/Netch/raw/acb4bc24651509c21558420d97865262e959bc0c/binaries/x86/Redirector.exe)

其他版本（1.3.0 及之前）：

将最左列的 hash 值复制替换掉该链接指定位置即可用于下载 `https://github.com/NetchX/Netch/raw/替换这里/binaries/x64/Redirector.exe`，x86 版本换掉 x64 即可

```bash
$ git log --pretty=oneline --decorate --source --tags binaries/x64/Redirector.exe
6a6a1db17092c668546eb073ac5b79bb717b0b7a 190929 1.3.3 [Redirector] Bypass IPv6 loopback
fc94119e7a68e9da16d5ee857c798ce908e1e54f 190928 1.3.2 Update x64 Redirector
e3a9a75343bd808593a5e93781e42e414e9c8e1c 190927 1.3.1 Return short path when fetching long path fails
4860e038c7d667026b48e7ea7e42a777646c6782 190917 1.3.0 Fix path contains chinese
349c44f8947e5f6aae8677b2ea93ea7eb441a537 190906 1.2.9 Update redirector, now support custom tcp port with -t arg
ed60a46dee8179836773731c0970d2e004375024 190904 1.2.9 Fix and optimize redirector
fee275a25c86b2f7c18a9362ff12a0882ae90bc1 190902 1.2.8-BETA [Redirector] Optimize speed statistics, Optimize performance, Add logs for UDP
9b837629fda39c1f30a4579cbe343076c0e14380 190831 1.2.7-STABLE Recompile redirector with new driver
ac57ae0be6137fcd5abf9b0529d55206fd81359b 190830 1.2.6-STABLE (tag: 1.2.6-STABLE) Optimize
a0a5b64833b520a065084024a425fe8ada2967f3 190830 1.2.6-STABLE Speed and bandwidth optimized
7b30473f41e4468d6744456dd040f0d62a271e7a 190830 1.2.6-STABLE Speed and bandwidth working now (Need optimize)
b8164a02419d630753fdfa27981100289abd9b89 190830 1.2.5-STABLE Update prebuilt binares
45954d7f4ed9500014d4dfae48c23b0887db1b77 190830 1.2.5-STABLE Update prebuilt binaries with upx compress
acb4bc24651509c21558420d97865262e959bc0c 190629 1.0.9-STABLE Rollback
5012a4d3011eafa3368f6cc97901e21af2e2874d 190628 1.0.9-STABLE Merge redirector and update version code to 1.0.9-STABLE
666050c3071dba67e2f0c6aae5eb5381a5acb39d 190625 1.0.5-STABLE Updated
```

##### 进程模式以外的方法

如果你遇到的问题仍无法解决，你还可以将模式切换为 TUN/TAP 模式来加速游戏，不同于 SSTap，Netch 底层使用的 tun2socks 不存在 NAT 类型严格的问题，只是这样就是全局代理了。如果有按规则代理的需要，可以参考 [NetchMode/docs/README.zh-CN.md](https://github.com/NetchX/NetchMode/blob/master/docs/README.zh-CN.md)。如果 TUN/TAP 模式还是不行，建议使用 outline 或者 SSTap 来解决问题，其中 outline 也没有 NAT 问题，如果不在意规则，能接受全局，建议使用 outline

#### NAT 类型限制

>- Q： xxx 游戏对 NAT 类型有要求，你们这个加速器代理后 NAT 类型还是严格 xxx ，我甚至用 NATTypeTester 测过了，还是不行 xxx
>- A：经过测试这款软件是可以做到 Full Cone 的 NATType 。如果你自己测试不行，需要考虑三个方面的问题
>   - 第一个是你的服务器是否支持 Full Cone 的 NATType ，这可以通过其他软件的测试来佐证，譬如使用 Sockscap64 之类
>   - 第二个是你本地的网络环境问题，首先，**关闭 windows 防火墙**，经测试 windows 防火墙会将 Full Cone 限制到 Port Restricted Cone，无论你是使用 TUN/TAP 模式，还是进程模式，除非你的游戏对 NAT 不敏感，否则请务必先把 windows 防火墙关闭。其次，某些杀软的防火墙可能也会影响到 NAT 类型，根据情况你可以关闭杀软的防火墙，或者卸载杀软来避免问题发生
>   - 第三个是运营商的网络问题，经测试联通数据和长宽等网络，即使在代理后也无法做到 Full cone ，就算服务器是支持 Full cone 的。这种情况下你可能需要切换全局的 VPN 代理工具（WireGuard ， Badvpn ， Openvpn ， tinyfecVPN 等），也可以尝试 Netch 的 TUN/TAP 模式，或者更换本地网络环境
>   - 第四个是某些游戏的代理模式有问题，可能遇到各种玄学问题，参见[上方](#无法进入游戏-模式无法使用)

#### Steam / 浏览器无法正常打开页面

>- Q：用来加速 Steam / 浏览器，结果无法正常打开页面
>- A：有人测试可行有人测试不可行。首先声明一下，本软件的功能主要不是用来代理 Steam / 浏览器打开页面的，建议使用专门的工具，如 [SteamCommunity 302](https://www.dogfight360.com/blog/686/)，浏览器则建议用 shadowsocks-windows， clash for windows 等等，你甚至可以尝试 ~~shadowsocks-windows over Netch~~，这可能会是一个功能改进，但是目前没有时间表

#### UWP 应用无法代理

>- Q：UWP 应用 xxx 无法代理
>- A：请按照[此方法](https://nekosc.com/technology/uwp_fiddler.html)设置即可

### 功能建议类问题

#### 加入本地代理功能

>- Q：我想在电脑上代理斯维奇
>- A：~~腾讯加速器好像可以免费加速主机游戏~~
>- A：会考虑加入，但不会是高优先级，你可以考虑通过 Pull Request 的方式为本软件加入该支持

#### 加入更多的 SSR 参数支持

>- Q：希望能加入更多的 SSR 参数支持，我那个机场的订阅好多节点无法导入 [issue #11](https://github.com/netchx/Netch/issues/11)
>- A：根据最新的 [项目计划表](https://github.com/NetchX/Netch/projects/1#card-24809942)，shadowsocksr的支持将在未来的版本由于各种原因而被放弃。在未来的版本中，可以[通过 Socks5 代理进行中转](https://github.com/NetchX/NetchMode/blob/master/docs/README.zh-CN.md#socks-5-代理中转)

#### 加入 MacOS 支持

>- Q：希望加入 MacOS 支持
>- A：这位亲请考虑赞助一台 MacBook Pro 给开发者使用呢

## 截图

![主界面](screenshots/main.png)

## 依赖

- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)

## 证书

- [MIT](../LICENSE)

## 注释

<escape><a name = "ref_1_d"><a href = "#ref_1_d">[1]</a></a>&nbsp;<a href = "#ref_1_s">&nbsp;↑&nbsp;</a>&nbsp;<a href = "https://www.right.com.cn/forum/thread-199299-1-1.html">NAT 原理</a></br><a name = "ref_2_d"><a href = "#ref_2_d">[2]</a></a>&nbsp;<a href = "#ref_2_s">&nbsp;↑&nbsp;</a>&nbsp;<a href = "https://github.com/HMBSbige/NatTypeTester">NAT 类型检测工具</a></escape>
