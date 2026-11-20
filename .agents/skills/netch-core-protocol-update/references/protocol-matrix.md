# Netch Protocol & Core Mapping Reference

## 1. Active Protocols (13)

`EConfigType.SSH` in [EConfigType.cs](../../../../Netch.Core/Enums/EConfigType.cs) is a legacy enum placeholder only (no util, editor, or core implementation).

| Protocol (`EConfigType`) | Model Class | Util Class (`*Util.cs`) | Accepted URI Schemes & Default Port | Clash YAML `type` | Cores (`CoreSelector`) | WinForms Editor |
| --- | --- | --- | --- | --- | --- | --- |
| `VLESS` | `VLESSServer` | `VLESSUtil` | `vless://` (explicit port req.) | `vless` | Mihomo, Xray (`xhttp`), sing-box (`quic` / PEM cert) | `VLESSForm` |
| `VMess` | `VMessServer` | `VMessUtil` | `vmess://` (JSON & std URI, explicit port req.) | `vmess` | Mihomo, Xray (`xhttp`), sing-box (`quic` / plain `h2` / PEM cert) | `VMessForm` |
| `Trojan` | `TrojanServer` | `TrojanUtil` | `trojan://` (443) | `trojan` | Mihomo, Xray (`xhttp`), sing-box (`h2` / `tls:false` / `quic` / PEM cert) | `TrojanForm` |
| `Shadowsocks` | `ShadowsocksServer` | `ShadowsocksUtil` | `ss://` (SIP002 & legacy), `ssd://` (explicit port req.) | `ss` | Mihomo, Xray (`xhttp`), sing-box (`mode=quic` / PEM cert) | `ShadowsocksForm` |
| `ShadowsocksR` | `ShadowsocksRServer` | `ShadowsocksRUtil` | `ssr://` | `ssr` | Mihomo | `ShadowsocksREditor` (`ServerEditors.cs`) |
| `SOCKS` | `SocksServer` | `SocksUtil` | `socks://`, `socks5://`, `socks5h://`, `tg://socks?`, `https://t.me/socks?` | `socks5` | Mihomo, sing-box (`uot: true`) | `SocksForm` |
| `HTTP` | `HttpServer` | `HttpUtil` | `http://` (80), `https://` (443); exports `Netch://` | `http` | Mihomo, sing-box (multi-value headers / ALPN / fp / PEM cert) | `HttpForm` |
| `Hysteria2` | `Hysteria2Server` | `Hysteria2Util` | `hysteria2://`, `hy2://` (443; host `mport` supported) | `hysteria2` | Mihomo, sing-box (`hop-interval < 5s` / PEM cert) | `Hysteria2Form` |
| `TUIC` | `TUICServer` | `TUICUtil` | `tuic://` (explicit port req.) | `tuic` | Mihomo, sing-box (PEM cert) | `SingboxServerForm` |
| `AnyTLS` | `AnyTLSServer` | `AnyTLSUtil` | `anytls://` (443) | `anytls` | Mihomo, sing-box (PEM cert) | `SingboxServerForm` |
| `WireGuard` | `WireGuardServer` | `WireGuardUtil` | `wireguard://`, `wg://` (explicit port req.) | `wireguard` | Mihomo, sing-box (>1 address in same IP family) | `WireGuardForm` |
| `Naive` | `NaiveServer` | `NaiveUtil` | `naive://`, `naive+https://`, `naive+quic://` (443) | `naive` | sing-box (+ `libcronet.dll`) | `SingboxServerForm` |
| `MASQUE` | `MASQUEServer` | `MASQUEUtil` | `masque://` (443) | `masque` | sing-box (`>= 1.15.0-alpha.7` CONNECT-IP; bundled 1.14.4 lacks it) | `SingboxServerForm` |

- **WinUI Editor**: all 13 protocols are edited dynamically inside `NodeEditor.EditAsync` in [NodeEditor.cs](../../../../Netch.App/Dialogs/NodeEditor.cs).

---

## 2. `CoreSelector` Routing Rules

Checked in [CoreSelector.cs](../../../../Netch.Core/Servers/V2ray/CoreSelector.cs):

1. **XHTTP (`UsesXhttp`)**: `VLESS` / `VMess` / `Trojan` / `Shadowsocks` with `Network == "xhttp"`.
   - Uses **Mihomo** (`CanUseMihomoXhttp`) ONLY when `VLESS` + empty/`{}` `Extra` + valid `XhttpMode` + `none`/`tls` security + no `Cert`/`CertSha`/`EchConfigList`/`EchForceQuery`/`Flow`/`VlessEncryption` + ALPN limited to `h2`/`http/1.1` + Mux off.
   - Otherwise uses **Xray** (`XrayController` + `V2rayConfigUtils`).
2. **sing-box (`UsesSingbox`)**:
   - Always for `Naive` and `MASQUE`.
   - Any node with PEM `Cert`.
   - `SOCKS` with `Uot == true`.
   - V2Ray transports with `Network == "quic"`.
   - `Trojan` with `Network` in `http`/`h2` OR `StreamSecurity` empty/`none`.
   - `VMess` with `Network` in `http`/`h2` AND `StreamSecurity` empty/`none`.
   - `HTTP` with duplicate header names (multi-value list), `ALPN`, or non-empty/`none` `Fingerprint`.
   - `Hysteria2` with `Ports` + `HopInterval < 5s`.
   - `WireGuard` with multiple interface addresses in the same IPv4/IPv6 family.
   - `Shadowsocks` with `SsPluginOptions` containing `mode=quic`.
3. **Default**: **Mihomo** (`MihomoController` + `MihomoConfig`).

---

## 3. Adding a Brand-New Protocol Checklist

1. **Enum**: Append `NewProto` at the end of `EConfigType` in [EConfigType.cs](../../../../Netch.Core/Enums/EConfigType.cs) (never reorder).
2. **Model**: Create `Netch.Core/Servers/<Proto>/<Proto>Server.cs` inheriting `Server` (or `SingboxServerBase`) and add any extra fields to `ProtocolExtraItem` in [Server.cs](../../../../Netch.Core/Models/Server.cs).
3. **Constants**: Add scheme prefix to `Constants.ProtocolShares` (plus any valid option lists) in [Constants.cs](../../../../Netch.Core/Constants.cs).
4. **Util**: Create `Netch.Core/Servers/<Proto>/<Proto>Util.cs` implementing `IServerUtil` (`ServerHelper` auto-discovers exported `IServerUtil` types; set `Priority` for menu ordering).
5. **Display Name / Scheme Alias**: Update `ServerHelper.GetDisplayName` in [ServerHelper.cs](../../../../Netch.Core/Utils/ServerHelper.cs) and special URI handling in [ShareLink.cs](../../../../Netch.Core/Utils/ShareLink.cs) if needed.
6. **Validation**: Add protocol rules in [ServerValidator.cs](../../../../Netch.Core/Utils/ServerValidator.cs) and call from `CheckServer`.
7. **Clash YAML**: Add `type` branch and option extraction in [ClashProxyList.cs](../../../../Netch.Core/Utils/ClashProxyList.cs).
8. **Core Selection & Config**: Update [CoreSelector.cs](../../../../Netch.Core/Servers/V2ray/CoreSelector.cs) and add builder support in [MihomoConfig.cs](../../../../Netch.Core/Servers/Mihomo/MihomoConfig.cs), [SingboxController.cs](../../../../Netch.Core/Servers/Singbox/SingboxController.cs), and/or [V2rayConfigUtils.cs](../../../../Netch.Core/Servers/V2ray/V2rayConfigUtils.cs).
9. **WinForms Editor**: Create `Netch/Forms/ServerForms/<Proto>Form.cs` (or add a case to `SingboxServerForm` / `ServerEditors.cs`) and wire `ServerEditors.Show` in [ServerEditors.cs](../../../../Netch/Forms/ServerEditors.cs).
10. **WinUI Editor**: Add protocol controls, defaults, cleanup, and TLS/transport wiring in [NodeEditor.cs](../../../../Netch.App/Dialogs/NodeEditor.cs).
11. **Localization**: Translate menu/label/validation strings in [Netch.Core/Resources/zh-CN](../../../../Netch.Core/Resources/zh-CN).
12. **Tests**: Update `ServerMenuHasEveryProtocolAtTheFirstLevel` & `EditorSavesParametersUsedByItsCore` in [Tests/NodeEditors.cs](../../../../Tests/NodeEditors.cs), plus URI round-trip, Clash YAML, `ServerValidator`, `CoreSelector`, config builder, and `zh-CN` tests.
