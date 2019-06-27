# Netch

[![](https://img.shields.io/badge/Telegram-群组-blue.svg)](https://t.me/NetchX)
[![](https://img.shields.io/badge/Telegram-频道-blue.svg)](https://t.me/NetchXChannel)

游戏加速工具

[English](../README.md)

## 简介

Netch是一款开源的游戏加速工具，不同于SSTap那样需要通过添加规则来实现黑名单代理，Netch原理更类似[Sockscap64](https://www.sockscap64.com/homepage/)，通过扫描游戏目录获得需要代理的进程名进行代理。

与此同时Netch避免了SSTap的NAT问题<escape><a name = "ref_1_s"><a href="#ref_1_d"><sup>[1]</sup></a></a><a name = "ref_2_s"><a href="#ref_2_d"><sup>[2]</sup></a></a></escape>，使用SSTap加速部分P2P联机，对NAT类型有要求的游戏时，可能会因为NAT类型严格遇到无法加入联机，或者其他影响游戏体验的情况。

## 使用方法

[简体中文](usage.zh-Hans.md)

[English](usage.en.md)

## 截图

<escape><div title="主界面" align="middle"><img src="screenshots/main.png" height="80%" width="80%"></div><div align="middle">主界面</div></escape>

## 依赖

- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)

## 注释

点击上箭头可返回原位置。

<escape><a name = "ref_1_d"><a href = "#ref_1_d">[1]</a></a>&nbsp;<a href = "#ref_1_s">&nbsp;↑&nbsp;</a>&nbsp;<a href = "https://www.right.com.cn/forum/thread-199299-1-1.html">NAT原理</a></br><a name = "ref_2_d"><a href = "#ref_2_d">[2]</a></a>&nbsp;<a href = "#ref_2_s">&nbsp;↑&nbsp;</a>&nbsp;<a href = "https://github.com/HMBSbige/NatTypeTester">NAT类型检测工具</a></escape>