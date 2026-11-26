using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;

public sealed class SSHUtil : ServerUtilBase, IServerUtil
{
    public ushort Priority { get; } = 10;
    public string TypeName { get; } = nameof(EConfigType.SSH);
    public string FullName { get; } = "SSH";
    public string ShortName { get; } = "SSH";
    public string[] UriScheme { get; } = ["ssh"];
    public Type ServerType { get; } = typeof(SSHServer);

    public string GetShareLink(Server s)
    {
        if (s.ProtoExtra.SshPrivateKey.IsNotEmpty()
            || s.ProtoExtra.SshPrivateKeyPassphrase.IsNotEmpty()
            || s.ProtoExtra.SshHostKey.IsNotEmpty()
            || s.ProtoExtra.SshHostKeyAlgorithms.IsNotEmpty()
            || s.ProtoExtra.SshClientVersion.IsNotEmpty())
        {
            return string.Empty;
        }

        var username = s.Username ?? string.Empty;
        var userInfo = s.Password.IsNotEmpty()
            ? $"{Utils.Utils.UrlEncode(username)}:{Utils.Utils.UrlEncode(s.Password)}"
            : Utils.Utils.UrlEncode(username);
        var auth = string.IsNullOrEmpty(userInfo) ? string.Empty : $"{userInfo}@";
        var remark = s.Remarks.IsNullOrEmpty() ? string.Empty : $"#{Utils.Utils.UrlEncode(s.Remarks)}";
        return $"{Constants.ProtocolShares[EConfigType.SSH]}{auth}{GetIpv6(s.Address)}:{s.Port}{remark}";
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var url = Utils.Utils.TryUri(text);
        if (url == null
            || !url.Scheme.Equals("ssh", StringComparison.OrdinalIgnoreCase)
            || url.IdnHost.IsNullOrEmpty())
        {
            return [];
        }

        var port = url.Port < 0 ? 22 : url.Port;
        if (!ServerValidator.IsValidPort(port))
            return [];

        var (user, pass, _) = SplitUserInfo(url.UserInfo);
        var server = new SSHServer
        {
            Address = url.IdnHost,
            Port = port,
            Username = user,
            Password = pass,
            Remarks = url.GetComponents(UriComponents.Fragment, UriFormat.Unescaped)
        };

        var query = Utils.Utils.ParseQueryString(url.Query);
        if (query.Count > 0)
        {
            var hostKey = GetFirstQueryValue(query, "host-key", "host_key");
            if (hostKey.IsNotEmpty())
                server.ProtoExtra.SshHostKey = hostKey;

            var hostKeyAlgorithms = GetFirstQueryValue(query, "host-key-algorithms", "host_key_algorithms");
            if (hostKeyAlgorithms.IsNotEmpty())
                server.ProtoExtra.SshHostKeyAlgorithms = hostKeyAlgorithms;

            var clientVersion = GetFirstQueryValue(query, "client-version", "client_version");
            if (clientVersion.IsNotEmpty())
                server.ProtoExtra.SshClientVersion = clientVersion;
        }

        return [server];
    }

    public bool CheckServer(Server s)
    {
        if (s is not SSHServer || string.IsNullOrWhiteSpace(s.Address) || !ServerValidator.IsValidPort(s.Port))
            return false;

        if (string.IsNullOrWhiteSpace(s.Username))
            return false;

        if (!string.IsNullOrWhiteSpace(s.ProtoExtra.SshPrivateKey))
            return ServerValidator.IsValidSshPrivateKey(s.ProtoExtra.SshPrivateKey);

        return s.Password.IsNotEmpty();
    }
}
