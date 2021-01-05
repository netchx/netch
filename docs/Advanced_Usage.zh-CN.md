# Netch 模式

## 目录

1. [模式介绍](#模式介绍)
   - 1.1 [模式 1 进程代理模式](#模式-1-进程代理模式)
   - 1.2 [模式 2（需要自己新建模式文件） TUN/TAP IP 黑名单代理模式](#模式-2需要自己新建模式文件-tuntap-ip-黑名单代理模式)
   - 1.3 [模式 3 TUN/TAP （IP 白名单）全局代理模式](#模式-3-tuntap-ip-白名单全局代理模式)
   - 1.4 [模式 4 HTTP 系统代理](#模式-4-http-系统代理)
   - 1.5 [模式 5 本地 Socks5 代理](#模式-5-本地-socks5-代理)
   - 1.6 [模式 6 本地 Socks5 和 HTTP 代理](#模式-6-本地-socks5-和-http-代理)
2. [Socks 5 代理中转](#socks-5-代理中转)
3. [新建进程代理模式](#新建进程代理模式)
   - 3.1 [模式](#模式)
   - 3.2 [扫描](#扫描)
   - 3.3 [启动](#启动)

## 模式介绍

目前 Netch 所有模式文件都在 `mode` 文件夹下。模式号即模式菜单中最左边中括号内数字

内置的模式中，如果模式名中有 Bypass China 的部分，即该模式会绕过中国 IP 段

模式 1 和模式 2 里面除了第一行格式不同，其他内容和 [SSTap-Rule](https://github.com/FQrabbit/SSTap-Rule) 相同。是否绕过中国的功能依赖于 [CNIP 文件](https://github.com/NetchX/Netch/blob/master/Netch/Resources/CNIP)

模式 3 到模式 5 的是否绕过中国的功能依赖于 [acl 文件](https://github.com/NetchX/Netch/blob/master/binaries/default.acl)

第一行格式均为如下样式，不同模式之间第一行的具体区别可以参照后面的内容

```Python
# 备注, 类型（是主项目 USAGE.zh-CN.md 里提到的模式类型的值减一）, 是否绕过中国（1 为是, 0 为否）
```

### 模式 1 进程代理模式

- 根据进程名进行代理
- 底层依赖于 [NetFilter SDK](https://netfiltersdk.com)
- 对于第一次使用 Netch 的用户而言，不需要做多余的事情
  - 若 [NetFilter SDK](https://netfiltersdk.com) 的驱动不存在，会自动安装
  - 若驱动版本过低，会自动更新

范例文件

在这个模式里，第一行只有备注是有用的，规则内容支持C++正则表达式

```
# 备注
进程名 1（会被代理）
!进程名 2（不会被代理）
csgo.exe
\\steam\\（代理运行路径包含steam的所有程序）
...
```

### 模式 2（需要自己新建模式文件） TUN/TAP IP 黑名单代理模式

- 黑名单代理指的是，除了名单内的 IP 走代理，其他连接都不走代理
- 需要自己新建模式文件，第一行写法同模式 3，只是需要把 2 改成 1
- 后续内容的格式同 [SSTap-rules](https://github.com/FQrabbit/SSTap-Rule)，任何规则问题建议到那边去提
- 可以通过左下角的`设置`来配置 IP 地址，子网掩码，网关，DNS
- 该模式下直连 IP 段无效，暂时没有代码实现
- 底层依赖于 [Tap-Windows](https://github.com/OpenVPN/tap-windows) 适配器等
- 如果 Netch 提示没有该适配器，可以直接安装 [Tap-Windows](https://build.openvpn.net/downloads/releases/latest/tap-windows-latest-stable.exe) 或者通过安装 [OpenVPN](https://openvpn.net/community-downloads/)，[SSTap](https://github.com/mayunbaba2/SSTap-beta-setup) 的方式获得该适配器

范例文件

在这个模式里，是否绕过中国的值是无效的

```
# 备注, 1
无类别域间路由写法 1（目的 IP 在这个子网内的网络请求都会被代理）
无类别域间路由写法 2
...
```

### 模式 3 TUN/TAP （IP 白名单）全局代理模式

- 白名单代理指的是，除了名单内的 IP 不走代理，其他连接都走代理
- 可以通过左下角的`设置`来配置 IP 地址，子网掩码，网关，DNS，直连 IP 段
- 底层依赖于 [Tap-Windows](https://github.com/OpenVPN/tap-windows) 适配器，tun2socks 等
- 如果 Netch 提示没有该适配器，可以直接安装 [Tap-Windows](https://build.openvpn.net/downloads/releases/latest/tap-windows-latest-stable.exe) 或者通过安装 [OpenVPN](https://openvpn.net/community-downloads/)，[SSTap](https://github.com/mayunbaba2/SSTap-beta-setup) 的方式获得该适配器

范例文件

```
# 备注, 2, 是否绕过中国（1 为是, 0 为否）
无类别域间路由写法 1（目的 IP 只有在这个子网内的网络请求不会被代理，其他的都会被代理）
无类别域间路由写法 2
...
```

### 模式 4 HTTP 系统代理

- 默认地址和端口为 127.0.0.1:2802
- 端口可以在左下角设置里面更改
- 会被设置为系统代理

范例文件

```
# 备注, 3, 是否绕过中国（1 为是, 0 为否）
（目前只有第一行是有效的）
```

### 模式 5 本地 Socks5 代理

- 默认地址和端口为 127.0.0.1:2801
- 端口可以在左下角设置里面更改
- 不会被设置为系统代理，对于 Chrome 之类使用系统代理的浏览器需要设置使用插件 SwitchyOmega 之后才能被正常代理
- 注意如果是使用 Firefox 的网络设置，请仅设置 Socks5 代理，清除其他代理配置，并取消勾选`为所有协议使用相同的代理服务器`
- 其他模式均含 Socks5 代理，本模式可以理解为仅 Socks5 代理\

范例文件

```
# 备注, 4, 是否绕过中国（1 为是, 0 为否）
（目前只有第一行是有效的）
```

### 模式 6 本地 Socks5 和 HTTP 代理

- Socks5 代理的默认地址和端口为 127.0.0.1:2801
- HTTP 代理的默认地址和端口为 127.0.0.1:2802
- 端口可以在左下角设置里面更改
- 不会被设置为系统代理

范例文件

```
# 备注, 5, 是否绕过中国（1 为是, 0 为否）
（目前只有第一行是有效的）
```

## Socks 5 代理中转

说明一下，Netch 并非是以网页代理为目的开发的程序，如果需要网络代理为目的的程序，需要 PAC，规则分流，订阅管理等功能的，请尽量参考使用以下软件而非 Netch（均为 Windows 平台）

ShadowsocksR

- [HMBSbige/ShadowsocksR-Windows](https://github.com/HMBSbige/ShadowsocksR-Windows/releases)

Shadowsocks

- [Clash for Windows](https://github.com/Fndroid/clash_for_windows_pkg/releases)

V2Ray

- [V2RayN](https://github.com/2dust/v2rayN/releases)

如果你想使用的代理工具目前 Netch 还不支持，或者需要一些 Netch 目前没有的功能，如 V2Ray 自定义配置，Socks5 本地代理规则分流的，可以在 Netch 里添加对应工具的本地 Socks5 代理端口后使用，注意如果你用的是模式 3 TUN/TAP （IP 白名单）全局代理模式，记得在`设置 - 全局直连 IP`中添加你的服务器 IP 地址，否则会产生代理回环

## 新建进程代理模式

- 现在软件还处在早期开发阶段，可能后续版本会发生很大变化，操作仅供参考

当前版本已添加配置编辑功能，根据自己的情况，使用订阅或者别的方法添加代理配置，我这里使用的是剪贴板导入

![剪贴板导入](/docs/screenshots/advanced/importServer.png)

如果你发现你的程序没我截图的看起来清晰，可以右键 `Netch.exe - 属性 - 兼容性 - 更改高 DPI 设置 - 替代高 DPI 缩放执行 - 系统（增强）`

### 模式

如果你的游戏的模式已经被收录，也可以考虑在模式菜单中，选择使用已收录的模式。所有模式的文件，都在 `./mode/` 文件夹下，如果你需要多个模式的合并文件，可以使用记事本将其打开，将多个文件合并

ping 的值未必准确，因为这只是你本地到代理服务器而非游戏服务器的延迟

如果你的游戏的模式没被收录，可以看接下来的扫描步骤来手动创建模式

接着点击菜单栏上的`模式 - 创建进程模式`

![模式 - 创建进程模式](/docs/screenshots/advanced/createMode.png)

### 扫描

在弹出的窗口中点击`扫描`

![扫描](/docs/screenshots/advanced/modeForm.png)

选择你要加速的游戏的安装路径，根据游戏不同，可能需要选择多个不同的目录进行扫描，参见[萌鹰的 Netch 教程](https://www.eaglemoe.com/archives/142)（包括 GTAOL 和 R6S 的配置方法）

>4. 选定 GTA5 游戏目录，点击确定，软件会自动扫描目录下的 exe 程式并填写进去
>5. 再次点击扫描，选择 SocialClub 的安装地址（一般为 C:\Program Files\Rockstar Games\Social Club），点击确定，点击保存
>
>注意：加入游戏时请不要忘记加入社交组件，比如说 GTA 不要忘记 SocialClub ，彩虹六号不要忘记 Uplay，如果游戏进程名与其他进程名重复，则可手动修改已创建好的模式文件，在进程名前加上绝对路径即可。csgo.exe -> C:\steam\game\Counter-Strike Global Offensive\csgo.exe

这里以CSGO为例，只需添加CSGO游戏根目录即可

![选择路径](/docs/screenshots/advanced/scan.png)

扫描时可能需要稍等片刻，扫描后记得填写备注

如果需要添加单个程序，也可以在添加按钮左侧的编辑栏中手动输入并添加

之后点保存进行`保存`

![保存](/docs/screenshots/advanced/saveMode.png)

### 启动

最后确认服务器一栏和模式一栏均为之前自己添加并需要使用的，没问题后点击`启动`即可

![启动](/docs/screenshots/advanced/started.png)

启动后，你再去游戏根目录或者别的启动器如 Steam，Uplay 启动游戏即可。此时游戏就已经被代理了

如果在 Netch 启动前就启动了游戏，建议重启游戏

如果需要 Steam，Uplay 等启动器也被代理，参照前面的方式对 Steam，Uplay 根目录也进行扫描即可

## 语言支持

Netch 支持多种语言，在启动时会根据系统语言选择自身语言。如果需要手动切换语言，可以在启动时加入命令行参数，命令行参数为目前支持的语言代码，可以去 [NetchTranslation/i18n](https://github.com/NetchX/NetchTranslation/tree/master/i18n) 文件夹下查看外部支持的语言代码文件。Netch 目前内置 en-US，zh-CN，外置 zh-TW。欢迎大家为 [NetchTranslation](https://github.com/NetchX/NetchTranslation) 提供其他语言的翻译
