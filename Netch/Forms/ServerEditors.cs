using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Servers;
using Netch.Utils;

namespace Netch.Forms;

/// <summary>
///     Opens the WinForms editor for a server type. Kept out of the core so <see cref="IServerUtil" />
///     stays UI-agnostic; each front end supplies its own editors.
/// </summary>
public static class ServerEditors
{
    public static void Create(IServerUtil util) => Show(Enum.Parse<EConfigType>(util.TypeName), null);

    public static void Edit(Server server) => Show(server.ConfigType, server);

    private static void Show(EConfigType type, Server? server)
    {
        switch (type)
        {
            case EConfigType.AnyTLS:
            case EConfigType.MASQUE:
            case EConfigType.Naive:
            case EConfigType.TUIC:
                new SingboxServerForm(server, type).ShowEditor();
                break;
            case EConfigType.HTTP:
                (server == null ? new HttpForm() : new HttpForm((HttpServer)server)).ShowEditor();
                break;
            case EConfigType.Hysteria2:
                (server == null ? new Hysteria2Form() : new Hysteria2Form((Hysteria2Server)server)).ShowEditor();
                break;
            case EConfigType.Shadowsocks:
                (server == null ? new ShadowsocksForm() : new ShadowsocksForm((ShadowsocksServer)server)).ShowEditor();
                break;
            case EConfigType.ShadowsocksR:
                new ShadowsocksREditor((ShadowsocksRServer?)server ?? new ShadowsocksRServer()).ShowEditor();
                break;
            case EConfigType.SOCKS:
                (server == null ? new SocksForm() : new SocksForm((SocksServer)server)).ShowEditor();
                break;
            case EConfigType.Trojan:
                (server == null ? new TrojanForm() : new TrojanForm((TrojanServer)server)).ShowEditor();
                break;
            case EConfigType.VLESS:
                (server == null ? new VLESSForm() : new VLESSForm((VLESSServer)server)).ShowEditor();
                break;
            case EConfigType.VMess:
                (server == null ? new VMessForm() : new VMessForm((VMessServer)server)).ShowEditor();
                break;
            case EConfigType.WireGuard:
                (server == null ? new WireGuardForm() : new WireGuardForm((WireGuardServer)server)).ShowEditor();
                break;
            default:
                throw new NotSupportedException($"No editor for server type {type}");
        }
    }

    private sealed class ShadowsocksREditor : ServerForm
    {
        protected override EConfigType TypeName => EConfigType.ShadowsocksR;

        public ShadowsocksREditor(ShadowsocksRServer server)
        {
            Server = server;
            CreateTextBox("Password", "Password", s => s.IsNotEmpty(), s => server.Password = s, server.Password);
            CreateComboBox("EncryptMethod", "Encryption",
                ["none", "rc4-md5", "aes-128-ctr", "aes-192-ctr", "aes-256-ctr", "aes-128-cfb", "aes-192-cfb",
                    "aes-256-cfb", "chacha20", "chacha20-ietf", "xchacha20"], s => server.EncryptMethod = s, server.EncryptMethod);
            CreateComboBox("Protocol", "Protocol",
                ["origin", "auth_sha1_v4", "auth_aes128_md5", "auth_aes128_sha1", "auth_chain_a", "auth_chain_b"],
                s => server.Protocol = s, server.Protocol);
            CreateTextBox("ProtocolParam", "Protocol Param", _ => true, s => server.ProtocolParam = s, server.ProtocolParam ?? "");
            CreateComboBox("OBFS", "Obfs", ["plain", "http_simple", "http_post", "tls1.2_ticket_auth", "tls1.2_ticket_fastauth", "random_head"],
                s => server.OBFS = s, server.OBFS);
            CreateTextBox("OBFSParam", "Obfs Param", _ => true, s => server.OBFSParam = s, server.OBFSParam ?? "");
        }
    }
}
