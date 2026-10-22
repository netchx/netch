using System.Text.Json;
using System.Web;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public class ShadowsocksUtil : IServerUtil
{
    public ushort Priority { get; } = 1;

    public string TypeName { get; } = "SS";

    public string FullName { get; } = "Shadowsocks";

    public string ShortName { get; } = "SS";

    public string[] UriScheme { get; } = { "ss", "ssd" };

    public Type ServerType { get; } = typeof(ShadowsocksServer);

    public void Edit(Server s)
    {
        new ShadowsocksForm((ShadowsocksServer)s).ShowDialog();
    }

    public void Create()
    {
        new ShadowsocksForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        var server = (ShadowsocksServer)s;
        // SIP002: ss://base64(method:password)@host:port/?plugin=...#remark
        var userInfo = ShareLink.URLSafeBase64Encode($"{server.EncryptMethod}:{server.Password}");
        var plugin = string.IsNullOrWhiteSpace(server.Plugin)
            ? ""
            : "?plugin=" + Uri.EscapeDataString(server.Plugin +
                (string.IsNullOrEmpty(server.PluginOption) ? "" : ";" + server.PluginOption));
        var remark = string.IsNullOrEmpty(server.Remark) ? "" : "#" + Uri.EscapeDataString(server.Remark);
        return $"ss://{userInfo}@{ProxyUri.Host(server.Hostname)}:{server.Port}/" + plugin + remark;
    }

    public IServerController GetController()
    {
        return new ShadowsocksController();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        if (text.StartsWith("ss://", StringComparison.OrdinalIgnoreCase))
            return new[] { ParseSsUri(text) };

        if (text.StartsWith("ssd://", StringComparison.OrdinalIgnoreCase))
            return ParseSsdUri(text);

        throw new FormatException();
    }

    public bool CheckServer(Server s)
    {
        var server = (ShadowsocksServer)s;
        if (string.IsNullOrWhiteSpace(server.Hostname) || server.Port == 0 ||
            !SSGlobal.EncryptMethods.Contains(server.EncryptMethod))
        {
            Log.Warning("Invalid Shadowsocks server or unsupported method: {Method}", server.EncryptMethod);
            return false;
        }

        return true;
    }

    public IEnumerable<Server> ParseSsdUri(string s)
    {
        var json = JsonSerializer.Deserialize<SSDJObject>(ShareLink.URLSafeBase64Decode(s.Substring(6)))!;

        return json.servers.Select(server => new ShadowsocksServer
            {
                Remark = server.remarks,
                Hostname = server.server,
                Port = server.port != 0 ? server.port : json.port,
                Password = server.password ?? json.password,
                EncryptMethod = server.encryption ?? json.encryption,
                Plugin = string.IsNullOrEmpty(json.plugin) ? string.IsNullOrEmpty(server.plugin) ? null : server.plugin : json.plugin,
                PluginOption = string.IsNullOrEmpty(json.plugin_options)
                    ? string.IsNullOrEmpty(server.plugin_options) ? null : server.plugin_options
                    : json.plugin_options
            })
            .Where(CheckServer);
    }

    public ShadowsocksServer ParseSsUri(string text)
    {
        var data = new ShadowsocksServer();

        var fragment = text.IndexOf('#');
        if (fragment >= 0)
        {
            data.Remark = Uri.UnescapeDataString(text[(fragment + 1)..]);
            text = text[..fragment];
        }

        var queryStart = text.IndexOf('?');
        if (queryStart >= 0)
        {
            var plugins = HttpUtility.ParseQueryString(text[(queryStart + 1)..]).Get("plugin");
            if (plugins != null)
            {
                var separator = plugins.IndexOf(';');
                var plugin = separator < 0 ? plugins : plugins[..separator];
                var pluginopts = separator < 0 ? "" : plugins[(separator + 1)..];
                switch (plugin)
                {
                    case "simple-obfs":
                        plugin = "obfs-local";
                        if (!pluginopts.Contains("obfs="))
                            pluginopts = "obfs=http;obfs-host=" + pluginopts;
                        break;
                    case "simple-obfs-tls":
                        plugin = "obfs-local";
                        if (!pluginopts.Contains("obfs="))
                            pluginopts = "obfs=tls;obfs-host=" + pluginopts;
                        break;
                }

                data.Plugin = plugin;
                data.PluginOption = pluginopts;
            }

            text = text[..queryStart];
        }

        var authority = text["ss://".Length..].TrimEnd('/');
        string credentials;
        Uri endpoint;
        if (authority.Contains('@'))
        {
            endpoint = ProxyUri.Parse("ss://" + authority, "ss");
            credentials = endpoint.UserInfo;
        }
        else
        {
            var decoded = ShareLink.URLSafeBase64Decode(authority);
            var separator = decoded.LastIndexOf('@');
            if (separator < 1)
                throw new FormatException("Invalid legacy Shadowsocks link");
            credentials = decoded[..separator];
            endpoint = ProxyUri.Parse("ss://user@" + decoded[(separator + 1)..], "ss");
        }

        try
        {
            credentials = ShareLink.URLSafeBase64Decode(credentials);
        }
        catch (FormatException)
        {
            credentials = Uri.UnescapeDataString(credentials);
        }

        var methodEnd = credentials.IndexOf(':');
        if (methodEnd < 1)
            throw new FormatException("Invalid Shadowsocks credentials");

        data.Hostname = ProxyUri.HostName(endpoint);
        data.Port = (ushort)endpoint.Port;
        data.EncryptMethod = credentials[..methodEnd];
        data.Password = credentials[(methodEnd + 1)..];

        return CheckServer(data) ? data : throw new FormatException();
    }
}
