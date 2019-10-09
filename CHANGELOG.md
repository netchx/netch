## Changelog

[简体中文](docs/CHANGELOG.zh-Hans.md)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

### [Unreleased]

### [1.3.3] - 2019-10-08

#### Added(1.3.3)

- Add the feature to clean internal DNS cache.
- Add the feature to modify `bin/Redirector` port when it processes TCP.
- Add TUN/TAP Fake DNS support.
- Add custom User-Agent header support to subscription download.
- Add support for Netch generic subscription format URL import.
- Add proxy support for subscription update.
- Add feature to bypass IPv6 loop traffic in the `bin/Redirector`.

#### Changed(1.3.3)

- Optimize user interface.
- Fix support for parsing plugins in SIP002 links.
- Update ShadowsocksR pre-compiled files with UPX compressed.
- Fix the issue that Socks5 server config starts local http proxy server with wrong IP address in mode 4/6.
- Update cloak plugin from upstream to 2.1.1 with UPX compressed.
- Remove the optimization of N3RO in the subscription.
- Optimize ProcessForm.
- Optimize Configuration.SearchOutbounds() by using iphlpapi.
- Update the built-in mode files with the upstream.
- Remove the compatible codes to read the config files from the version before 1.2.9.

### [1.3.2] - 2019-09-29

#### Changed(1.3.2)

- Update `bin/Redirector.exe`.
- Remove the Telegram Group invite link in the about form.
- The log deletion mechanism is changed to delete the last log every time when the main program starts.
- Update ACL file.
- Update China IP list.
- Temporarily remove the traffic statistics.

### [1.3.1] - 2019-09-27

#### Changed(1.3.1)

- Fix the HTTP local proxy server listening address issue.
- Fix the config saving issue.
- Limit the parallel degrees of the server delay test. Now at most 16 tasks are executed simultaneously.
- Update the built-in mode files with the upstream.
- Change the sponsorship to YoYu.
- Optimize the subscription link parsing. Optimize IPv6 server config parsing. Fix the method that automatically transform the ShadowsocksR to Shadowsocks. Add support for the first generation VMess URL.
- Update `bin/Redirector.exe`. Now it can return the obtained short path directly when fails to fetch full-length path.
- Fix the translation problem on the title of "Create process mode".
- Change the way to run ShadowsocksR. Now it can run directly from the command line arguments rather than the config file.
- Replace Objects with Models

### [1.3.0] - 2019-09-18

#### Changed(1.3.0)

- Modify `bin/Redirector.exe` to fix the program with Chinese  char path can not be properly proxy problem.

### [1.2.9] - 2019-09-17

#### Added(1.2.9)

- Allow custom local proxy server listening addresses, such as 127.0.0.1 or 0.0.0.0.
- Add system architecture check to prevent the program running on a system with different architecture.
- Add Shadowsocks cloak plugin support.
- Add HiDPI support.

#### Changed(1.2.9)

- Fix ShadowsocksR、VMess local Socks5 proxy port can't be set issue.
- Optimize `bin/Redirector.exe`.
- Fix SIP003 plugin support in ShadowsocksD subscription.
- Improve the stability of ShadowsocksR and package all the DLL.
- Update the upstream Netfilter SDK driver.

### [1.2.8-BETA] - 2019-09-04

#### Added(1.2.8-BETA)

- Add Redirector UDP connection log.
- Add custom local Socks5 and HTTP proxy port features.

#### Changed(1.2.8-BETA)

- Fix killing processes not created by Netch [issue #86](https://github.com/NetchX/Netch/issues/86).
- Fix stopping system proxy not set by Netch [issue #107](https://github.com/NetchX/Netch/issues/107).
- Refactor config files. Combining `link.dat`, `server.dat`, `settings.dat`, `bypass.dat`, `tuntap.ini` into `settings.json`.
- Optimize codes.

### [1.2.7-STABLE] - 2019-08-31

#### Changed(1.2.7-STABLE)

- Optimized interface that only mode 1 shows the traffic statistics label.
- Optimize delay detection.
- Update the upstream Netfilter SDK driver.
- Update `bin/Redirector.exe`.

### [1.2.6-STABLE] - 2019-08-30

#### Added(1.2.6-STABLE)

- Add speed and bandwidth label.

#### Changed(1.2.6-STABLE)

- Optimize codes.

### [1.2.5-STABLE] - 2019-08-30

#### Added(1.2.5-STABLE)

- Add the function to auto-delete the TUN/TAP log files.

#### Changed(1.2.5-STABLE)

- Optimize menu layout.
- Update ShadowsocksR pre-compiled files.
- Use UPX to compress all the pre-compiled files like EXE and  DLL.
- Update `bin/Redirector.exe` with debug log support.

### [1.2.4-STABLE] - 2019-08-28

#### Changed(1.2.4-STABLE)

- Fix a critical bug which will cause Shadowsocks servers without plugin to stop working.
- Fix start up failure caused by replicated proxy processes.
- Fix a bug that service cannot be started when using Bypass LAN (and China) mode at the first run of Netch.

### [1.2.3-STABLE] - 2019-08-28

#### Added(1.2.3-STABLE)

- Add support for Simple OBFS plugin and V2Ray plugin for Shadowsocks server.

#### Changed(1.2.3-STABLE)

- Fix bypass LAN (and China) mode for VMess server.
- Optimize codes.

### [1.2.2-STABLE] - 2019-08-26

#### Added(1.2.2-STABLE)

- Add global error handling.
- Automatically resolve domain names to IP addresses when use Socks5.

#### Changed(1.2.2-STABLE)

- Fix the problem that the tray icon is not displayed.

### [1.2.1-STABLE] - 2019-08-10

#### Added(1.2.1-STABLE)

- Add about form.

#### Changed(1.2.1-STABLE)

- Fix delay display that display green background for 0 delay.
- Optimize codes.

### [1.2.0-STABLE] - 2019-08-10

#### Added(1.2.0-STABLE)

- Add built-in DNS cache processing.

#### Changed(1.2.0-STABLE)

- Set AutoToolTip to false.
- Optimize delay display. -1 for not been tested. -2 for DNS query failure. -4 for other failures.

### [1.2.0-PRE-RELEASE] - 2019-07-26

#### Added(1.2.0-PRE-RELEASE)

- Add VMess support.
- Add mutual exclusion check to prevent multiple instance running.
- Add TUN/TAP support.
- Add some non-system-proxy modes.

#### Changed(1.2.0-PRE-RELEASE)

- Change NotifyIcon.

### [1.1.1-STABLE] - 2019-07-10

#### Added(1.1.1-STABLE)

- Add NotifyIcon.

### [1.1.0-STABLE] - 2019-07-01

#### Added(1.1.0-STABLE)

- Add server and mode combobox last selected position storage.
- Add mode 4 for browser proxy which includes "[4] Bypass LAN and China" and "[4] Bypass LAN".

#### Changed(1.1.0-STABLE)

- Change main form menu.
- Fix ShadowsocksR encryption method wrongly using the list that Shadowsocks is using.

### [1.0.9-STABLE] - 2019-06-29

#### Added(1.0.9-STABLE)

- Add the buttons to uninstall service and restart service in about menu.
- Add support for ShadowsocksR.

#### Changed(1.0.9-STABLE)

- Fix the black border at the right side of the tool strip.
- Change the delay detection method. It will not be automatically tested after startup, and the manual test will automatically refresh the interface.
- Change the method to detect the system language.
- Change the directory scan form to input directory directly.

### [1.0.8-STABLE] - 2019-06-27

#### Added(1.0.8-STABLE)

- Add `DriverUpdater.exe` to force driver files to update.
- Add more built-in modes.

### [1.0.7-STABLE] - 2019-06-26

#### Added(1.0.7-STABLE)

- Add latest driver files.
- Add more built-in modes.

### [1.0.6-STABLE] - 2019-06-26

#### Added(1.0.6-STABLE)

- Add SSD subscription / sharing URL import support.

#### Changed(1.0.6-STABLE)

- Change the delay detection method.

### [1.0.5-STABLE] - 2019-06-25

#### Added(1.0.5-STABLE)

- Add built-in modes.
- Add built-in language support.

[Unreleased]: https://github.com/NetchX/Netch/compare/1.3.3...HEAD
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
