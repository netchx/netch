
# 使用方法

[English](usage.en.md)

其他中文教程：

- [【教程】运用 Netch ，享受游戏 - 萌鹰研究所](https://www.eaglemoe.com/archives/142)
- [Netch：一款开源的网络游戏加速工具 - Rat's Blog](https://www.moerats.com/archives/959/)

本页面的 [github pages](https://binglinggroup.github.io/archives/Netch_guide.html) 版本。

## 目录

1. [步骤](#步骤)
   - 1.1 [新建代理配置](#新建代理配置)
   - 1.2 [模式](#模式)
   - 1.3 [扫描](#扫描)
   - 1.4 [启动](#启动)
2. [线路选择](#线路选择)
3. [配合 udp2raw 使用](#配合udp2raw使用)

点击上箭头可返回目录。

## 步骤

- 现在软件还处在早期开发阶段，可能后续版本会发生很大变化，操作仅供参考。

### 新建代理配置

当前版本已添加配置编辑功能，根据自己的情况，使用订阅或者别的方法添加代理配置，我这里使用的是剪贴板导入。

如果你想使用的代理工具目前 Netch 还不支持，可以通过 socks5 代理进行中转，也就是让 Netch 访问你的代理工具提供的 socks5 代理。

<escape><div title="剪贴板导入.jpg" align="middle"><img src="https://raw.githubusercontent.com/BingLingGroup/BingLingGroup.github.io/img/Netch_guide/2019-06-24_210438.png" height="80%" width="80%"></div><div align="middle">剪贴板导入.jpg</div></escape>

~~如果你发现你的程序没我截图的看起来清晰，可以右键 Netch.exe -属性-兼容性-更改高 DPI 设置-替代高 DPI 缩放执行-系统(增强)。~~

<escape><a href = "#目录">&nbsp;↑&nbsp;</a></escape>

### 模式

如果你的游戏的模式已经被收录，也可以考虑直接使用已收录的模式。所有模式的文件，都在 `./mode/` 文件夹下，如果你需要多个模式的合并文件，可以使用记事本将其打开，将多个文件合并。

<escape><div title="点击快速创建模式.jpg" align="middle"><img src="https://raw.githubusercontent.com/BingLingGroup/BingLingGroup.github.io/img/Netch_guide/2019-06-24 211537.png" height="80%" width="80%"></div><div align="middle">点击快速创建模式.jpg</div></escape>

~~图中绿色的0是因为我使用了本地中转， Netch 内建的 ping 功能未能检测出真实的延迟数据。~~

ping 的值未必准确，因为这只是你本地到代理服务器而非游戏服务器的延迟。

如果你的游戏的模式没被收录，可以看接下来的扫描步骤来手动创建模式。

接着点击菜单栏上的快速创建模式。

<escape><a href = "#目录">&nbsp;↑&nbsp;</a></escape>

### 扫描

在弹出的窗口中点击扫描。

<escape><div title="扫描.jpg" align="middle"><img src="https://raw.githubusercontent.com/BingLingGroup/BingLingGroup.github.io/img/Netch_guide/2019-06-24 211842.png" height="50%" width="50%"></div><div align="middle">扫描.jpg</div></escape>

选择你要加速的游戏的安装路径，根据游戏不同，可能需要选择多个不同的目录进行扫描，参见[萌鹰的Netch教程](https://www.eaglemoe.com/archives/142)(包括 GTAOL 和 R6S 的配置方法)。

>4. 选定 GTA5 游戏目录，点击确定，软件会自动扫描目录下的exe程式并填写进去。
>5. 再次点击扫描，选择 socialclub 的安装地址（一般为 C:\Program Files\Rockstar Games\Social Club ），点击确定，点击保存。
>
>注意：加入游戏时请不要忘记加入社交组件，比如说 GTA 不要忘记 socialclub ，彩虹六号不要忘记 uplay 。

这里以战争雷霆为例，只需添加战争雷霆游戏根目录即可，当前版本暂时不支持输入目录路径进行扫描。

<escape><div title="选择路径.jpg" align="middle"><img src="https://raw.githubusercontent.com/BingLingGroup/BingLingGroup.github.io/img/Netch_guide/2019-06-24 212036.png" height="50%" width="50%"></div><div align="middle">选择路径.jpg</div></escape>

扫描时可能需要稍等片刻，扫描后记得填写备注，如果需要添加单个程序，也可以在添加按钮左侧的编辑栏中手动输入并添加。

之后点保存进行保存。

<escape><div title="保存.jpg" align="middle"><img src="https://raw.githubusercontent.com/BingLingGroup/BingLingGroup.github.io/img/Netch_guide/2019-06-24 212837.png" height="50%" width="50%"></div><div align="middle">保存.jpg</div></escape>

<escape><a href = "#目录">&nbsp;↑&nbsp;</a></escape>

### 启动

最后确认服务器一栏和模式一栏均为之前自己添加并需要使用的，没问题后点击启动即可。

<escape><div title="启动.jpg" align="middle"><img src="https://raw.githubusercontent.com/BingLingGroup/BingLingGroup.github.io/img/Netch_guide/2019-06-24 213121.png" height="80%" width="80%"></div><div align="middle">启动.jpg</div></escape>

启动后，你再去游戏根目录或者别的启动器如 Steam ， Uplay 启动游戏即可。此时游戏就已经被代理了。

如果在 Netch 启动前就启动了游戏，建议重启游戏。

如果需要 Steam ， Uplay 等启动器也被代理，参照前面的方式对 Steam ， Uplay 根目录也进行扫描即可。

<escape><a href = "#目录">&nbsp;↑&nbsp;</a></escape>

## 线路选择

普通人可以入手 [n3ro](https://n3ro.io/) 的线路(不负责推荐)，根据 [sabre 大佬的科普](https://t.me/sabershome/197)， iplc 的线路较为稳定。

## 配合 udp2raw 使用

打算使用自己租赁的服务器加速游戏的中二人士可以了解一下，多种网络工具配合使用，战公网。

[UDPSpeeder+Udp2raw 使用教程](https://www.moerats.com/archives/662/)

<escape><a href = "#目录">&nbsp;↑&nbsp;</a></escape>
