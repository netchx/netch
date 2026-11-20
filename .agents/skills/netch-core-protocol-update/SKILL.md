---
name: netch-core-protocol-update
description: >-
  Use this skill when updating Netch's bundled proxy/auxiliary cores (mihomo,
  sing-box with libcronet.dll, xray, tun2socks, aiodns, pcap2socks, wintun),
  updating or adding proxy protocol parameters across URI/Clash parsers,
  validators, core config generators, WinUI and WinForms node editors, i18n,
  and unit tests, or packaging releases.
---

# Netch Core, Protocol, Parser & Dual-UI Update Runbook

Keep this repo synchronized across **core binaries**, **Netch.Core** (models, validators, URI/Clash parsers, core routing & config builders, `zh-CN` i18n), **WinUI** (`Netch.App`), **WinForms** (`Netch`), and **Tests**.

Refer to [references/protocol-matrix.md](./references/protocol-matrix.md) for the complete 13-protocol file map, `CoreSelector` routing table, and new-protocol creation checklist.

---

## 1. Updating Bundled Cores

| Component | Output Location | Build / Update Source | SHA-256 Check | In-App Updater (`Netch.App`) |
| --- | --- | --- | --- | --- |
| `mihomo` | `Other/release/mihomo.exe` -> `bin/` | [Other/mihomo/build.ps1](../../../Other/mihomo/build.ps1) | `$null` in [build.ps1](../../../build.ps1) | `Downloads.Cores` (`Mihomo`) |
| `sing-box` + `libcronet.dll` | `Storage/sing_box/` -> `bin/sing_box/` | Replace from `SagerNet/sing-box` Windows amd64 zip | — | `Downloads.Cores` (`sing-box`) + `CoreFiles` |
| `xray` | `Other/release/xray.exe` -> `bin/` | [Other/xray/build.ps1](../../../Other/xray/build.ps1) | Both `Other/xray/build.ps1` & [build.ps1](../../../build.ps1) | `Downloads.Cores` (`Xray-core`) |
| `tun2socks` | `Other/release/tun2socks-windows-amd64-v3.exe` -> `bin/` | [Other/tun2socks/build.ps1](../../../Other/tun2socks/build.ps1) | Both `Other/tun2socks/build.ps1` & [build.ps1](../../../build.ps1) | `Downloads.Cores` (`tun2socks`) |
| `pcap2socks` | `Other/release/pcap2socks.exe` -> `bin/` | [Other/pcap2socks/build.ps1](../../../Other/pcap2socks/build.ps1) | Both `Other/pcap2socks/build.ps1` & [build.ps1](../../../build.ps1) | `Downloads.Cores` (`pcap2socks`) |
| `wintun` | `Other/release/wintun.dll` -> `bin/` | [Other/wintun/build.ps1](../../../Other/wintun/build.ps1) | Both `Other/wintun/build.ps1` & [build.ps1](../../../build.ps1) | `Downloads.Cores` (`WinTUN`) |
| `aiodns` | `Other/release/aiodns.bin` -> `bin/` | [Other/aiodns/build.ps1](../../../Other/aiodns/build.ps1) | `$null` in [build.ps1](../../../build.ps1) | — |

### Procedure

1. **`Other/*` binary downloads (`xray`, `tun2socks`, `pcap2socks`, `wintun`, `mihomo`)**:
   - Bump `$version`, `$expectedSha256` (archive), and `$expectedBinarySha256` (extracted binary) in `Other/<core>/build.ps1`.
   - Run `& .\Other\<core>\build.ps1` to verify the download, extract into `Other/release/`, and get the extracted SHA-256.
   - Update the matching entry in `Test-OtherCoresCurrent` inside [build.ps1](../../../build.ps1) (CI caches `Other/release/` keyed by `./sha256.ps1 ./Other` in [.github/workflows/build.yml](../../../.github/workflows/build.yml)).
2. **`sing-box` + Naive `libcronet.dll`**:
   - Replace `sing-box.exe`, `libcronet.dll`, and `LICENSE` together in `Storage/sing_box/` from the same official release archive. Never ship `sing-box.exe` without sibling `libcronet.dll`.
   - Verify `Netch.App/Services/Downloads.cs` (`Cores` asset filename pattern, version CLI argument, and `CoreFiles` copying sibling `libcronet.dll`).
   - Check version-specific error hints or guards in [SingboxController.cs](../../../Netch.Core/Servers/Singbox/SingboxController.cs) (e.g., cert pin or MASQUE support messages) and [SingboxServerForm.cs](../../../Netch/Forms/ServerForms/SingboxServerForm.cs).
3. **`aiodns`**:
   - Edit [Other/aiodns/main.go](../../../Other/aiodns/main.go) / `go.mod` and rebuild via `& .\Other\aiodns\build.ps1` (requires Go + MinGW UCRT64 GCC for `CGO_ENABLED=1`).
4. **Upstream schema & metadata audit**:
   - Inspect upstream release notes for new/renamed/deprecated protocol fields, TLS options, or CLI flags (`Guard` args / `StartedKeywords` / `FailedKeywords`).
   - Update [Storage/README.md](../../../Storage/README.md) (packaged as `licenses/Components.md`) if component notes change.
   - Run offline config checks (`mihomo.exe -t`, `sing-box.exe check`, `xray.exe run -test`) and `dotnet test Tests/Tests.csproj -p:Platform=x64`.

---

## 2. Updating Protocol Parameters (End-to-End Checklist)

Follow this order whenever adding or changing a protocol field so import, export, routing, validation, and both UIs stay aligned:

1. **Model & Constants**:
   - Add or update properties on `Server` / `ProtocolExtraItem` in [Server.cs](../../../Netch.Core/Models/Server.cs) or the protocol subclass in `Netch.Core/Servers/<Proto>/`.
   - Add option lists (ciphers, modes, congestion controls) to [Constants.cs](../../../Netch.Core/Constants.cs).
   - **Never renumber** existing members in [EConfigType.cs](../../../Netch.Core/Enums/EConfigType.cs); only append at the end to preserve user `settings.json`.
2. **Validation**:
   - Add field rules to `ServerValidator.Validate` in [ServerValidator.cs](../../../Netch.Core/Utils/ServerValidator.cs) so WinUI `NodeEditor` and parsers share the exact same checks.
   - Re-use those helpers in each protocol's `CheckServer(Server)` method.
3. **URI Import & Export**:
   - Shared TLS / transport / query helpers live in [ServerUtilBase.cs](../../../Netch.Core/Services/ServerUtilBase.cs) and [SingboxUtilBase.cs](../../../Netch.Core/Servers/Singbox/SingboxUtilBase.cs).
   - Update `ParseUri` and `GetShareLink` in `Netch.Core/Servers/<Proto>/*Util.cs` (and [VmessQRCode.cs](../../../Netch.Core/Servers/V2ray/ShareModels/VmessQRCode.cs) for VMess JSON).
   - Check [ShareLink.cs](../../../Netch.Core/Utils/ShareLink.cs) and [ServerHelper.cs](../../../Netch.Core/Utils/ServerHelper.cs) if scheme handling or display names change.
4. **Clash YAML Import**:
   - Map all official Clash/Mihomo aliases and option types in [ClashProxyList.cs](../../../Netch.Core/Utils/ClashProxyList.cs).
   - Explicitly consume fields (`Take`/`Flag`/`Number`/`Text`) — `Fields.EnsureEmpty()` rejects unknown keys intentionally so unsupported options are skipped cleanly rather than silently dropped.
5. **Core Selection & Config Generation**:
   - Update routing predicates (`UsesXhttp`, `CanUseMihomoXhttp`, `UsesSingbox`) in [CoreSelector.cs](../../../Netch.Core/Servers/V2ray/CoreSelector.cs) if a parameter determines which core runs the node.
   - Wire the parameter into [MihomoConfig.cs](../../../Netch.Core/Servers/Mihomo/MihomoConfig.cs), [SingboxController.cs](../../../Netch.Core/Servers/Singbox/SingboxController.cs), and/or [V2rayConfigUtils.cs](../../../Netch.Core/Servers/V2ray/V2rayConfigUtils.cs). Throw `MessageException` with a translated message when a core cannot support a parameter combination instead of silently ignoring it.
6. **WinUI Editor (`Netch.App`)**:
   - Wire the control, visibility condition, reset/cleanup logic on save, and defaults in [NodeEditor.cs](../../../Netch.App/Dialogs/NodeEditor.cs).
   - Keep clipboard/file import entrypoints in sync in [NodesPage.xaml.cs](../../../Netch.App/Pages/NodesPage.xaml.cs).
7. **WinForms Editor (`Netch`)**:
   - Update shared transport/security fields in [ServerForm.cs](../../../Netch/Forms/ServerForm.cs), protocol-specific forms in `Netch/Forms/ServerForms/*.cs` (and `ShadowsocksREditor` in [ServerEditors.cs](../../../Netch/Forms/ServerEditors.cs)), or [MainForm.cs](../../../Netch/Forms/MainForm.cs).
8. **Localization & Unit Tests**:
   - Add every new label and validation string to [Netch.Core/Resources/zh-CN](../../../Netch.Core/Resources/zh-CN) (and update `Storage/i18n/*` if needed).
   - Add round-trip, negative-input, Clash YAML, `ServerValidator`, `CoreSelector`, core config, WinForms editor, and `zh-CN` coverage in `Tests/`.

---

## 3. Verification & Packaging Gate

Run tests and both frontend builds after every change:

```pwsh
dotnet test Tests/Tests.csproj -p:Platform=x64 --nologo
dotnet build Netch.App/Netch.App.csproj -p:Platform=x64 --nologo -v:minimal
dotnet build Netch/Netch.csproj -p:Platform=x64 --nologo -v:minimal
```

Validate [Netch.Core/Resources/zh-CN](../../../Netch.Core/Resources/zh-CN) with case-sensitive JSON (never PowerShell `ConvertFrom-Json`, because `zh-CN` has legacy case-variant keys like `OBFS` / `Obfs`):

```pwsh
[System.Text.Json.JsonDocument]::Parse((Get-Content Netch.Core\Resources\zh-CN -Raw -Encoding UTF8)) | Out-Null
```

When preparing portable packages (see repo root `AGENTS.md` and [.github/workflows/release.yml](../../../.github/workflows/release.yml)):

- **WinUI package**: `./build.ps1 -Configuration Release -OutputPath release` -> verify root `Netch.App.exe`, sibling `bin/sing_box/libcronet.dll`, `bin/`, `mode/`, `logging/`, `data/`, and `licenses/Components.md`.
- **WinForms package**: `./build.ps1 -Configuration Release -OutputPath release-winforms -Frontend WinForms` -> root `Netch.exe`.

---

## 4. Repo-Specific Pitfalls & Invariants

- **Strict analyzers on `Netch.Core`**: `CodeAnalysisTreatWarningsAsErrors=true` and `Nullable.Extended.Analyzer` (`NX0001`–`NX0003`) are enabled. Never use the null-forgiving operator (`!`) in `Netch.Core`.
- **Single URL-decode**: `Utils.ParseQueryString` already splits with `Split('=', 2)` and decodes keys/values once; never call `UrlDecode` again on values returned by `ParseQueryString`.
- **HTTP `Netch://` export invariant**: `HttpUtil.GetShareLink` intentionally returns `string.Empty` so `ShareLink.GetShareLink` falls back to `Netch://` (`Tests/Scenes.cs`).
- **Clash SS UDP-over-TCP**: `udp-over-tcp: true` without `udp-over-tcp-version: 2` must skip the node (`Tests/ClashImport.cs`).
- **WinForms fixed dialog size**: `ServerForm` must not resize `ClientSize` inside `UpdateFields` (`Tests/NodeEditors.cs`).
- **WinUI `ComboBox` selection vs clearing**: Ensure empty-by-default fields allow resetting, while non-empty defaults (`"auto"`, `"none"`, `"bbr"`) stay selected when creating new nodes (`NodeEditor.Options`).
- **Test assembly boundary**: `Tests.csproj` references `Netch.csproj` (WinForms + `Netch.Core`), not `Netch.App.csproj`. Keep shared validation and parsing in `Netch.Core` so tests exercise the same code used by WinUI.
