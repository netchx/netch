<p align="center"><img src="https://github.com/NetchX/Netch/blob/master/Netch/Resources/Netch.png?raw=true" width="128" /></p>

<div align="center">

# Netch
A simple proxy client

[![](https://img.shields.io/badge/telegram-group-green?style=flat-square)](https://t.me/netch_group)
[![](https://img.shields.io/badge/telegram-channel-blue?style=flat-square)](https://t.me/netch_channel)
[![](https://img.shields.io/github/downloads/netchx/netch/total.svg?style=flat-square)](https://github.com/netchx/netch/releases)
[![](https://img.shields.io/github/v/release/netchx/netch?style=flat-square)](https://github.com/netchx/netch/releases)
</div>

## Features
Some features may not be implemented in version 1

### Modes
- `ProcessMode` - Use Netfilter driver to intercept process traffic
- `ShareMode` - Share your network based on WinPcap / Npcap
- `TapMode` - Use TAP-Windows driver to create virtual adapter
- `TunMode` - Use WinTUN driver to create virtual adapter
- `WebMode` - Web proxy mode
- `WmpMode` - Proxy forwarding (eg. OBS Streaming)

### Protocols
- [`Socks5`](https://www.wikiwand.com/en/SOCKS)
- [`Shadowsocks`](https://github.com/shadowsocks/shadowsocks-libev)
- [`ShadowsocksR`](https://github.com/shadowsocksrr/shadowsocksr-libev)
- [`Trojan`](https://github.com/p4gefau1t/trojan-go)
- [`VMess`](https://github.com/v2fly/v2ray-core)
- [`VLess`](https://github.com/xtls/xray-core)

### Others
- UDP NAT FullCone (Limited by your server)
- .NET 5.0 x64

## Donate
- `XMR` `48ju3ELNZEa6wwPBMexCJ9G218BGY2XwhH6B6bmkFuJ3QgM4hPw2Pra35jPtuBZSc7SLNWeBpiWJZWjQeMAiLnTx2tH2Efx`
- `ETH` `0x23dac0a93bcd71fec7a95833ad030338f167f185`

## Sponsor
<a href="https://www.jetbrains.com/?from=Netch"><img src="jetbrains.svg" alt="JetBrains" width="200"/></a>

- [NeroCloud](https://nerocloud.io)

## License
```
The MIT License (MIT)

Copyright (c) 2019 Netch

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
