# Netch Protocol & Core Mapping Reference

## 1. Active Protocols (19)

| Protocol (`EConfigType`) | Model Class | Util Class (`*Util.cs`) | Accepted URI Schemes & Default Port | Clash YAML `type` | Cores (`CoreSelector`) |
| --- | --- | --- | --- | --- | --- |
| `VLESS` | `VLESSServer` | `VLESSUtil` | `vless://` (explicit port req.) | `vless` | Mihomo, Xray (`xhttp`), sing-box (`quic` / PEM cert) |
| `VMess` | `VMessServer` | `VMessUtil` | `vmess://` (JSON & std URI, explicit port req.) | `vmess` | Mihomo, Xray (`xhttp`), sing-box (`quic` / plain `h2` / PEM cert) |
| `Trojan` | `TrojanServer` | `TrojanUtil` | `trojan://` (443) | `trojan` | Mihomo, Xray (`xhttp`), sing-box (`h2` / `tls:false` / `quic` / PEM cert) |
| `Shadowsocks` | `ShadowsocksServer` | `ShadowsocksUtil` | `ss://` (SIP002 & legacy), `ssd://` (explicit port req.) | `ss` | Mihomo, Xray (`xhttp`), sing-box (`mode=quic` / PEM cert) |
| `ShadowsocksR` | `ShadowsocksRServer` | `ShadowsocksRUtil` | `ssr://` | `ssr` | Mihomo |
| `SOCKS` | `SocksServer` | `SocksUtil` | `socks://`, `socks5://`, `socks5h://`, `tg://socks?`, `https://t.me/socks?` | `socks5` | Mihomo, sing-box (`uot: true`) |
| `HTTP` | `HttpServer` | `HttpUtil` | `http://` (80), `https://` (443); exports `Netch://` | `http` | Mihomo, sing-box (multi-value headers / ALPN / fp / PEM cert) |
| `Hysteria2` | `Hysteria2Server` | `Hysteria2Util` | `hysteria2://`, `hy2://` (443; host `mport` supported) | `hysteria2` | Mihomo, sing-box (`hop-interval < 5s` / `hop-interval-max` / `bbr-profile` / PEM cert) |
| `TUIC` | `TUICServer` | `TUICUtil` | `tuic://` (explicit port req.) | `tuic` | Mihomo, sing-box (PEM cert) |
| `AnyTLS` | `AnyTLSServer` | `AnyTLSUtil` | `anytls://` (443) | `anytls` | Mihomo, sing-box (PEM cert) |
| `WireGuard` | `WireGuardServer` | `WireGuardUtil` | `wireguard://`, `wg://` (explicit port req.; promotes to `AmneziaWG` if AWG params present) | `wireguard` | Mihomo, sing-box (>1 address in same IP family) |
| `Naive` | `NaiveServer` | `NaiveUtil` | `naive://`, `naive+https://`, `naive+quic://` (443) | `naive` | sing-box (+ `libcronet.dll`) |
| `MASQUE` | `MASQUEServer` | `MASQUEUtil` | `masque://` (443) | `masque` | sing-box (`>= 1.15.0-alpha.7` CONNECT-IP; bundled 1.14.4 lacks it) |
| `SSH` | `SSHServer` | `SSHUtil` | `ssh://` (22); exports `Netch://` on extended SSH options | `ssh` | Mihomo, sing-box (`client-version` / PEM cert) |
| `ShadowTLS` | `ShadowTLSServer` | `ShadowTLSUtil` | `ss://` (`plugin=shadow-tls`); exports `Netch://` on extended options | `ss` (`plugin: shadow-tls`) | Mihomo (v2/v3), sing-box (v1-v3 / `uot` / CertSha / ECH / PEM cert) |
| `TrustTunnel` | `TrustTunnelServer` | `TrustTunnelUtil` | `trusttunnel://`, `tt://` (443; supports RFC 9000 varint TLV deep-links); exports `Netch://` with Cert/ECH | `trusttunnel` | Mihomo |
| `Mieru` | `MieruServer` | `MieruUtil` | `mierus://`, `mieru://` (`port=` / single `port-range`) | `mieru` | Mihomo |
| `ShadowQUIC` | `ShadowQUICServer` | `ShadowQUICUtil` | `shadowquic://` (443) | `shadowquic` | Mihomo |
| `AmneziaWG` | `AmneziaWGServer` | `AmneziaWGUtil` | `amneziawg://`, `awg://` (explicit port req.) | `wireguard` (`amnezia-wg-option`) | Mihomo |

- **WinUI Editor**: all 19 protocols are edited dynamically inside `NodeEditor.EditAsync` in [NodeEditor.cs](../../../../Netch.App/Dialogs/NodeEditor.cs), with shared defaults, normalization, and validation in [ServerEditorHelper.cs](../../../../Netch.Core/Utils/ServerEditorHelper.cs).

---

## 2. `CoreSelector` Routing Rules

Checked in [CoreSelector.cs](../../../../Netch.Core/Servers/V2ray/CoreSelector.cs):

1. **XHTTP (`UsesXhttp`)**: `VLESS` / `VMess` / `Trojan` / `Shadowsocks` with `Network == "xhttp"`.
   - Uses **Mihomo** (`CanUseMihomoXhttp`) ONLY when `VLESS` + empty/`{}` `Extra` + valid `XhttpMode` + `none`/`tls` security + no `Cert`/`CertSha`/`EchConfigList`/`EchForceQuery`/`Flow`/`VlessEncryption` + ALPN limited to `h2`/`http/1.1` + Mux off.
   - Otherwise uses **Xray** (`XrayController` + `V2rayConfigUtils`).
2. **sing-box (`UsesSingbox`)**:
   - Never for `TrustTunnel`, `Mieru`, `ShadowQUIC`, or `AmneziaWG` (all route to Mihomo).
   - Always for `Naive` and `MASQUE`.
   - Any node with PEM `Cert` (except `SSH`, `ShadowsocksR`, `SOCKS`, `WireGuard`, and the four Mihomo-only protocols above).
   - `SOCKS` with `Uot == true`.
   - V2Ray transports with `Network == "quic"`.
   - `Trojan` with `Network` in `http`/`h2` OR `StreamSecurity` empty/`none`.
   - `VMess` with `Network` in `http`/`h2` AND `StreamSecurity` empty/`none`.
   - `HTTP` with duplicate header names (multi-value list), `ALPN`, or non-empty/`none` `Fingerprint`.
   - `Hysteria2` with `Ports` + `HopInterval < 5s`, `HopIntervalMax`, or `BbrProfile`.
   - `SSH` with `SshClientVersion`.
   - `ShadowTLS` with `ShadowTlsVersion == 1`, `ShadowTlsVersion == 2 && Uot == true`, `CertSha`, or `EchConfigList`.
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
6. **Validation & Normalization**: Add protocol rules in [ServerValidator.cs](../../../../Netch.Core/Utils/ServerValidator.cs), call from `CheckServer`, and update defaults/cleanup in [ServerEditorHelper.cs](../../../../Netch.Core/Utils/ServerEditorHelper.cs).
7. **Clash YAML**: Add `type` branch and option extraction in [ClashProxyList.cs](../../../../Netch.Core/Utils/ClashProxyList.cs).
8. **Core Selection & Config**: Update [CoreSelector.cs](../../../../Netch.Core/Servers/V2ray/CoreSelector.cs) and add builder support in [MihomoConfig.cs](../../../../Netch.Core/Servers/Mihomo/MihomoConfig.cs), [SingboxController.cs](../../../../Netch.Core/Servers/Singbox/SingboxController.cs), and/or [V2rayConfigUtils.cs](../../../../Netch.Core/Servers/V2ray/V2rayConfigUtils.cs).
9. **WinUI Editor**: Add protocol controls, defaults, cleanup, and TLS/transport wiring in [NodeEditor.cs](../../../../Netch.App/Dialogs/NodeEditor.cs).
10. **Localization**: Translate menu/label/validation strings in [Netch.Core/Resources/zh-CN](../../../../Netch.Core/Resources/zh-CN).
11. **Tests**: Update `AllFifteenProtocolsAreRegisteredWithDisplayNamesAndDefaults` & `EditorSavesParametersUsedByItsCore` in [Tests/NodeEditors.cs](../../../../Tests/NodeEditors.cs), plus URI round-trip, Clash YAML, `ServerValidator`, `CoreSelector`, config builder, and `zh-CN` tests.
