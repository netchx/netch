# 打包组件

| 路径 | 用途与来源 |
| --- | --- |
| bin/mihomo.exe | 默认代理内核。https://github.com/MetaCubeX/mihomo |
| bin/xray.exe | 支持由 Xray 处理的 XHTTP 节点。https://github.com/XTLS/Xray-core |
| bin/sing_box/sing-box.exe、libcronet.dll | sing-box 及 NaiveProxy 运行时。两者位于同一目录。https://github.com/SagerNet/sing-box |
| bin/pcap2socks.exe | 共享模式。https://github.com/zhxie/pcap2socks |
| bin/tun2socks-windows-amd64-v3.exe、wintun.dll | TUN 转发与虚拟网卡。https://github.com/xjasonlyu/tun2socks · https://www.wintun.net |
| bin/nfdriver.sys、nfapi.dll、Redirector.bin | 进程模式驱动与转发组件。Redirector 由本仓库构建。 |
| bin/RouteHelper.bin | 路由辅助组件，由本仓库构建。 |
| bin/aiodns.bin、aiodns.conf | DNS 辅助组件与规则。 |
| bin/GeoLite2-Country.mmdb | 可选的出口国家识别数据。https://github.com/Loyalsoldier/geoip |
| bin/stun.txt | NAT 测试服务器列表。 |

许可证与归属声明随包提供，见 `LICENSE`、`NOTICE.txt` 和 `bin/sing_box/LICENSE`。
