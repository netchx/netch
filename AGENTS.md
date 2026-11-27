# Netch 构建与打包

- Windows x64 的 WinUI 测试包和发布包统一从仓库根目录运行 `./build.ps1 -Configuration Release -OutputPath release` 构建（默认 `-SelfContained $true`，自带 `.NET 10` 与 Windows App SDK 运行时）；若构建使用系统 `.NET 10 Runtime (x64)` 的精简包，传入 `-SelfContained $false` 并输出至独立目录（例如 `./build.ps1 -Configuration Release -OutputPath release-framework-dependent -SelfContained $false`）。两种打包方式均保持单文件 `PublishSingleFile` 打包，根目录的启动文件为 `Netch.App.exe`（均内置应用程序集、Windows App SDK 运行时及界面资源）。
- 在 EXE 旁保留 `bin/`（代理内核、驱动和辅助程序）、`mode/`（规则与模式）、`logging/`（日志）、`data/`（用户配置）和 `licenses/`（许可证与组件说明）。内置词典随 EXE 发布，用户可在同级 `i18n/` 放置自定义词典。发测试包时归档整个构建输出目录，保留空目录。
- 外置目录以 `Environment.ProcessPath` 指向的 EXE 为基准。WinUI 单文件解包后的 `AppContext.BaseDirectory` 用于读取内置界面资源。
- 构建会重建输出目录。用新目录构建，打包前检查根目录入口和外置目录，并确认 `bin/sing_box/libcronet.dll` 随包交付。首次使用完整解压后启动 `Netch.App.exe`；运行 `-SelfContained $false` 版本时，目标系统需已安装 `.NET 10 Runtime (x64)`。
