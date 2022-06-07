using System.Text.Json;
using System.Text.RegularExpressions;
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
        // ss://method:password@server:port#Remark
        return "ss://" + ShareLink.URLSafeBase64Encode($"{server.EncryptMethod}:{server.Password}@{server.Hostname}:{server.Port}") + "#" +
               HttpUtility.UrlEncode(server.Remark);
    }

    public IServerController GetController()
    {
        return new ShadowsocksController();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        if (text.StartsWith("ss://"))
            return new[] { ParseSsUri(text) };

        if (text.StartsWith("ssd://"))
            return ParseSsdUri(text);

        throw new FormatException();
    }

    public bool CheckServer(Server s)
    {
        var server = (ShadowsocksServer)s;
        if (!SSGlobal.EncryptMethods.Contains(server.EncryptMethod))
        {
            Log.Warning("Unsupported SS Encrypt Method: {Method}", server.EncryptMethod);
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

        text = text.Replace("/?", "?");
        if (text.Contains("#"))
        {
            data.Remark = HttpUtility.UrlDecode(text.Split('#')[1]);
            text = text.Split('#')[0];
        }

        if (text.Contains("?"))
        {
            var finder = new Regex(@"^(?<data>.+?)\?(.+)$");
            var match = finder.Match(text);

            if (!match.Success)
                throw new FormatException();

            var plugins = HttpUtility.UrlDecode(HttpUtility.ParseQueryString(new Uri(text).Query).Get("plugin"));
            if (plugins != null)
            {
                var plugin = plugins.Substring(0, plugins.IndexOf(";", StringComparison.Ordinal));
                var pluginopts = plugins.Substring(plugins.IndexOf(";", StringComparison.Ordinal) + 1);
                switch (plugin)
                {
                    case "obfs-local":
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

            text = match.Groups["data"].Value;
        }

        if (text.Contains("@"))
        {
            var finder = new Regex(@"^ss://(?<base64>.+?)@(?<server>.+):(?<port>\d+)");
            var parser = new Regex(@"^(?<method>.+?):(?<password>.+)$");
            var match = finder.Match(text);
            if (!match.Success)
                throw new FormatException();

            data.Hostname = match.Groups["server"].Value;
            data.Port = ushort.Parse(match.Groups["port"].Value);

            var base64 = ShareLink.URLSafeBase64Decode(match.Groups["base64"].Value);
            match = parser.Match(base64);
            if (!match.Success)
                throw new FormatException();

            data.EncryptMethod = match.Groups["method"].Value;
            data.Password = match.Groups["password"].Value;
        }
        else
        {
            var parser = new Regex(@"^((?<method>.+?):(?<password>.+)@(?<server>.+):(?<port>\d+))");
            var match = parser.Match(ShareLink.URLSafeBase64Decode(text.Replace("ss://", "")));
            if (!match.Success)
                throw new FormatException();

            data.Hostname = match.Groups["server"].Value;
            data.Port = ushort.Parse(match.Groups["port"].Value);
            data.EncryptMethod = match.Groups["method"].Value;
            data.Password = match.Groups["password"].Value;
        }

        return CheckServer(data) ? data : throw new FormatException();
    }
}