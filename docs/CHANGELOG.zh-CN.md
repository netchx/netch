## 更新日志

[English](../CHANGELOG.md)

本项目的所有明显改变将被记录在这个文件里。

本文件的格式将会基于[Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，同时本项目的版本号将遵守[Semantic Versioning](https://semver.org/lang/zh-CN/)。

### [未发布]

### [1.3.3] - 2019-10-08

#### 添加(1.3.3)

- 添加清理程序内部 DNS 缓存功能
- 添加自定义 Redirector 处理 TCP 时的端口的选项
- 添加 TUN/TAP Fake DNS 支持
- 添加自定义订阅时使用的 User-Agent 的功能
- 添加 Netch 通用订阅 URL 导入支持（Generic Subscription Format）
- 添加使用代理更新订阅的功能
- 在 Redirector 中设置绕过 IPv6 环路流量

#### 修改(1.3.3)

- 优化界面交互
- 修复 SIP002 链接解析问题
- 更新预编译文件（已使用 UPX 压缩）
- 修复在模式 4、6 中使用 Socks5 服务器时配置错误的问题
- 将 cloak 插件同步上游更新至 2.1.1（已使用 UPX 压缩）
- 移除以前对 N3RO 节点名过长的优化代码
- 优化进程代理模式创建界面
- 优化 TUN/TAP 的默认出口搜索代码（现在使用 Windows API 搜索）
- 同步上游更新自带模式文件
- 去除了对 1.2.9 以前老配置文件的兼容代码

### [1.3.2] - 2019-09-29

#### 修改(1.3.2)

- 更新 Redirector 程序
- 去除关于页面的 Telegram 群组链接
- 日志删除机制改为主程序每次启动时删除上一次的日志
- 更新 ACL 文件
- 更新 China IP 列表
- 暂时去除流量统计功能

### [1.3.1] - 2019-09-27

#### 修改(1.3.1)

- 修复 HTTP 代理的监听地址设置问题
- 修复配置文件保存问题
- 限制测速时的并行任务，现在最多同时执行 16 个任务
- 随上游更新自带模式列表
- 修改赞助商为 YoYu
- 优化订阅链接解析。优化 IPv6 节点解析，修复 ShadowsocksR 自动转换至 Shadowsocks 的机制，支持 VMess 第一代链接
- 更新 Redirector 程序，在获取长路径失败时直接返回已获取的短路径
- 修复创建进程模式的标题未翻译问题
- 修改 ShadowsocksR 启动方式。现在可以直接写命令行参数，不再使用配置文件启动
- 将 Objects 替换成 Models

### [1.3.0] - 2019-09-18

#### 修改(1.3.0)

- 修改 `bin/Redirector.exe` 程序以修复中文路径程序无法正常代理问题

### [1.2.9] - 2019-09-17

#### 添加(1.2.9)

- 允许自定义本地代理监听地址，如 127.0.0.1 或 0.0.0.0
- 添加系统架构检查，架构不一致时禁止运行
- 添加 Shadowsocks 的 cloak 插件支持
- 添加 HiDPI 支持

#### 修改(1.2.9)

- 修复 ShadowsocksR、VMess 无法设置本地 Socks5 端口的问题
- 优化 `bin/Redirector.exe`
- 修复 ShadowsocksD 订阅中的 SIP003 插件支持
- 优化 ShadowsocksR 稳定性并将所有 DLL 打包
- 随上游更新自带模式列表

### [1.2.8-BETA] - 2019-09-04

#### 添加(1.2.8-BETA)

- 添加 Redirector UDP 连接日志
- 添加自定义本地 Socks5 和 HTTP 代理端口的功能 [issue #52](https://github.com/NetchX/Netch/issues/52)

#### 修改(1.2.8-BETA)

- 修复终止非 Netch 创建的进程的问题 [issue #86](https://github.com/NetchX/Netch/issues/86)
- 修复关闭非 Netch 创建的系统代理的问题 [issue #107](https://github.com/NetchX/Netch/issues/107)
- 重构配置文件，将`link.dat`, `server.dat`, `settings.dat`, `bypass.dat`, `tuntap.ini`合并成`settings.json`
- 优化代码

### [1.2.7-STABLE] - 2019-08-31

#### 修改(1.2.7-STABLE)

- 优化界面显示，仅模式一显示流量统计标签
- 优化延迟检测
- 更新上游 Netfilter SDK 驱动
- 更新 `bin/Redirector.exe` 程序

### [1.2.6-STABLE] - 2019-08-30

#### 添加(1.2.6-STABLE)

- 添加速度和流量显示

#### 修改(1.2.6-STABLE)

- 优化代码

### [1.2.5-STABLE] - 2019-08-30

#### 添加(1.2.5-STABLE)

- 添加自动删除 TUN/TAP 日志

#### 修改(1.2.5-STABLE)

- 优化菜单布局
- 更新 ShadowsocksR 预编译文件
- UPX 压缩所有预编译文件（EXE + DLL）
- 更新 `bin/Redirector.exe` 程序（现在有详细的日志供你们调试是否代理上）

### [1.2.4-STABLE] - 2019-08-28

#### 修改(1.2.4-STABLE)

- 修复使用 Shadowsocks 节点时无法正常启动的问题
- 修复当上次 Netch 进程未正常退出时 进程残留导致无法再次启动服务的问题
- 修复当第一次启动 Netch 即使用 Bypass LAN (and China) 模式时 无法正常启动服务的问题

### [1.2.3-STABLE] - 2019-08-28

#### 添加(1.2.3-STABLE)

- 添加 Shadowsocks Simple OBFS 插件及 V2Ray 插件的支持

#### 修改(1.2.3-STABLE)

- 修复 VMess 服务器使用 Bypass LAN (and China) 代理模式时未正常绕过的问题
- 优化代码

### [1.2.2-STABLE] - 2019-08-26

#### 添加(1.2.2-STABLE)

- 添加全局错误处理
- 在 Socks5 下自动将域名解析成 IP 地址

#### 修改(1.2.2-STABLE)

- 修复托盘图标不显示的问题

### [1.2.1-STABLE] - 2019-08-10

#### 添加(1.2.1-STABLE)

- 添加关于窗口

#### 修改(1.2.1-STABLE)

- 修复延迟显示，把 0 也显示成绿色
- 优化代码

### [1.2.0-STABLE] - 2019-08-10

#### 添加(1.2.0-STABLE)

- 添加软 DNS 缓存处理

#### 修改(1.2.0-STABLE)

- 删除鼠标停留在工具栏时的提示信息
- 优化延迟显示，未测试显示 -1，DNS 查询失败显示 -2，发生错误显示 -4

### [1.2.0-PRE-RELEASE] - 2019-07-26

#### 添加(1.2.0-PRE-RELEASE)

- 添加 VMess 支持
- 添加互斥检查防止多开
- 添加 TUN/TAP 支持
- 添加了几个不会自动设置系统代理的模式

#### 修改(1.2.0-PRE-RELEASE)

- 修改托盘图标

### [1.1.1-STABLE] - 2019-07-10

#### 添加(1.1.1-STABLE)

- 添加托盘

### [1.1.0-STABLE] - 2019-07-01

#### 添加(1.1.0-STABLE)

- 添加服务器，模式上次选择位置存储
- 添加模式四用于代理网页，即 [4] Bypass LAN and China 和 [4] Bypass LAN 两个模式

#### 修改(1.1.0-STABLE)

- 修改工具栏
- 修复 ShadowsocksR 加密方式用了 Shadowsocks 的列表的问题

### [1.0.9-STABLE] - 2019-06-29

#### 添加(1.0.9-STABLE)

- 添加了在关于栏的删除服务和重启服务的按钮
- 添加 ShadowsocksR 支持

#### 修改(1.0.9-STABLE)

- 修改工具栏以去除靠右边的小黑边
- 修改延迟检测，即启动后不会再自动测试，手动测试将会自动刷新界面
- 修改系统语言检测
- 修改目录扫描和目录选择界面以支持直接输入路径扫描

### [1.0.8-STABLE] - 2019-06-27

#### 添加(1.0.8-STABLE)

- 添加 `DriverUpdater.exe` 用于强制更新驱动
- 添加更多内置模式

### [1.0.7-STABLE] - 2019-06-26

#### 添加(1.0.7-STABLE)

- 添加最新驱动文件
- 添加更多内置模式

### [1.0.6-STABLE] - 2019-06-26

#### 添加(1.0.6-STABLE)

- 添加 SSD 订阅 / 分享链接支持

#### 修改(1.0.6-STABLE)

- 修改延迟检测策略

### [1.0.5-STABLE] - 2019-06-25

#### 添加(1.0.5-STABLE)

- 添加自带模式
- 添加自带语言

[未发布]: https://github.com/NetchX/Netch/compare/1.3.3...HEAD
[1.3.3]: https://github.com/NetchX/Netch/compare/1.3.2...1.3.3
[1.3.2]: https://github.com/NetchX/Netch/compare/1.3.1...1.3.2
[1.3.1]: https://github.com/NetchX/Netch/compare/1.3.0...1.3.1
[1.3.0]: https://github.com/NetchX/Netch/compare/1.2.9...1.3.0
[1.2.9]: https://github.com/NetchX/Netch/compare/1.2.8-BETA...1.2.9
[1.2.8-BETA]: https://github.com/NetchX/Netch/compare/1.2.7-STABLE...1.2.8-BETA
[1.2.7-STABLE]: https://github.com/NetchX/Netch/compare/1.2.6-STABLE...1.2.7-STABLE
[1.2.6-STABLE]: https://github.com/NetchX/Netch/compare/1.2.5-STABLE...1.2.6-STABLE
[1.2.5-STABLE]: https://github.com/NetchX/Netch/compare/1.2.4-STABLE...1.2.5-STABLE
[1.2.4-STABLE]: https://github.com/NetchX/Netch/compare/1.2.3-STABLE...1.2.4-STABLE
[1.2.3-STABLE]: https://github.com/NetchX/Netch/compare/1.2.2-STABLE...1.2.3-STABLE
[1.2.2-STABLE]: https://github.com/NetchX/Netch/compare/1.2.1-STABLE...1.2.2-STABLE
[1.2.1-STABLE]: https://github.com/NetchX/Netch/compare/1.2.0-STABLE...1.2.1-STABLE
[1.2.0-STABLE]: https://github.com/NetchX/Netch/compare/1.2.0-PRE-RELEASE...1.2.0-STABLE
[1.2.0-PRE-RELEASE]: https://github.com/NetchX/Netch/compare/1.1.1-STABLE...1.2.0-PRE-RELEASE
[1.1.1-STABLE]: https://github.com/NetchX/Netch/compare/1.1.0-STABLE...1.1.1-STABLE
[1.1.0-STABLE]: https://github.com/NetchX/Netch/compare/1.0.9-STABLE...1.1.0-STABLE
[1.0.9-STABLE]: https://github.com/NetchX/Netch/compare/1.0.8-STABLE...1.0.9-STABLE
[1.0.8-STABLE]: https://github.com/NetchX/Netch/compare/1.0.7-STABLE...1.0.8-STABLE
[1.0.7-STABLE]: https://github.com/NetchX/Netch/compare/1.0.6-STABLE...1.0.7-STABLE
[1.0.6-STABLE]: https://github.com/NetchX/Netch/compare/1.0.5-STABLE...1.0.6-STABLE
[1.0.5-STABLE]: https://github.com/NetchX/Netch/releases/tag/1.0.5-STABLE
