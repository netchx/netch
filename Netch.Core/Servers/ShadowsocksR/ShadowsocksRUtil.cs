using System.Text.RegularExpressions;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public sealed class ShadowsocksRUtil : IServerUtil
{
    private static readonly Regex LinkFields = new(@"^(.+):([^:]+):([^:]*):([^:]+):([^:]*):([^:]*)$");
    public ushort Priority => 1;
    public string TypeName => nameof(EConfigType.ShadowsocksR);
    public string FullName => "ShadowsocksR";
    public string ShortName => "SSR";
    public string[] UriScheme => ["ssr"];
    public Type ServerType => typeof(ShadowsocksRServer);

    public string GetShareLink(Server s)
    {
        var server = (ShadowsocksRServer)s;
        var password = ShareLink.URLSafeBase64Encode(server.Password);
        var obfs = ShareLink.URLSafeBase64Encode(server.OBFSParam ?? "");
        var protocol = ShareLink.URLSafeBase64Encode(server.ProtocolParam ?? "");
        var remarks = ShareLink.URLSafeBase64Encode(server.Remarks ?? "");
        var group = ShareLink.URLSafeBase64Encode(server.Group ?? Constants.DefaultGroup);
        return "ssr://" + ShareLink.URLSafeBase64Encode(
            $"{server.Address}:{server.Port}:{server.Protocol}:{server.EncryptMethod}:{server.OBFS}:{password}/?obfsparam={obfs}&protoparam={protocol}&remarks={remarks}&group={group}");
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        var payload = ShareLink.URLSafeBase64Decode(text["ssr://".Length..]);
        var parts = payload.Split("/?", 2, StringSplitOptions.None);
        if (parts.Length == 1)
            parts = payload.Split('?', 2, StringSplitOptions.None);
        var fields = LinkFields.Match(parts[0].TrimEnd('/'));
        if (!fields.Success || !ushort.TryParse(fields.Groups[2].Value, out var port) || port == 0)
            return [];

        var query = parts.Length == 2 ? ShareLink.ParseParam(parts[1]) : new Dictionary<string, string>();
        string Read(string key) => ShareLink.URLSafeBase64Decode(query.GetValueOrDefault(key, ""));
        var protocol = fields.Groups[3].Value;
        var obfs = fields.Groups[5].Value;
        var server = new ShadowsocksRServer
        {
            Address = fields.Groups[1].Value.Trim('[', ']'),
            Port = port,
            Protocol = protocol.IsNullOrEmpty() ? "origin" : protocol.Replace("_compatible", ""),
            EncryptMethod = fields.Groups[4].Value,
            OBFS = obfs.IsNullOrEmpty() ? "plain" : obfs.Replace("_compatible", ""),
            Password = ShareLink.URLSafeBase64Decode(fields.Groups[6].Value),
            ProtocolParam = Read("protoparam"),
            OBFSParam = Read("obfsparam"),
            Remarks = Read("remarks"),
            Group = Read("group") is { Length: > 0 } group ? group : Constants.DefaultGroup
        };
        return CheckServer(server) ? [server] : [];
    }

    public bool CheckServer(Server s) => s is ShadowsocksRServer server
        && server.Address.IsNotEmpty() && ServerValidator.IsValidPort(server.Port)
        && server.EncryptMethod.IsNotEmpty() && server.Password.IsNotEmpty()
        && server.Protocol.IsNotEmpty() && server.OBFS.IsNotEmpty();
}
