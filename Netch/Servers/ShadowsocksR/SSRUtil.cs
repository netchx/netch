using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Netch.Interfaces;
using Netch.Models;
using Netch.Servers.Shadowsocks;
using Netch.Servers.ShadowsocksR.Form;
using Netch.Utils;

namespace Netch.Servers.ShadowsocksR
{
    public class SSRUtil : IServerUtil
    {
        public ushort Priority { get; } = 1;

        public string TypeName { get; } = "SSR";

        public string FullName { get; } = "ShadowsocksR";

        public string ShortName { get; } = "SR";

        public string[] UriScheme { get; } = { "ssr" };

        public Type ServerType { get; } = typeof(ShadowsocksR);

        public void Edit(Server s)
        {
            new ShadowsocksRForm((ShadowsocksR)s).ShowDialog();
        }

        public void Create()
        {
            new ShadowsocksRForm().ShowDialog();
        }

        public string GetShareLink(Server s)
        {
            var server = (ShadowsocksR)s;

            // https://github.com/shadowsocksr-backup/shadowsocks-rss/wiki/SSR-QRcode-scheme
            // ssr://base64(host:port:protocol:method:obfs:base64pass/?obfsparam=base64param&protoparam=base64param&remarks=base64remarks&group=base64group&udpport=0&uot=0)
            var paraStr =
                $"/?obfsparam={ShareLink.URLSafeBase64Encode(server.OBFSParam ?? "")}&protoparam={ShareLink.URLSafeBase64Encode(server.ProtocolParam ?? "")}&remarks={ShareLink.URLSafeBase64Encode(server.Remark)}";

            return "ssr://" +
                   ShareLink.URLSafeBase64Encode(
                       $"{server.Hostname}:{server.Port}:{server.Protocol}:{server.EncryptMethod}:{server.OBFS}:{ShareLink.URLSafeBase64Encode(server.Password)}{paraStr}");
        }

        public IServerController GetController()
        {
            return new SSRController();
        }

        /// <summary>
        ///     SSR链接解析器
        ///     Copy From
        ///     https://github.com/HMBSbige/ShadowsocksR-Windows/blob/d9dc8d032a6e04c14b9dc6c8f673c9cc5aa9f607/shadowsocks-csharp/Model/Server.cs#L428
        ///     Thx :D
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public IEnumerable<Server> ParseUri(string text)
        {
            // ssr://host:port:protocol:method:obfs:base64pass/?obfsparam=base64&remarks=base64&group=base64&udpport=0&uot=1
            var ssr = Regex.Match(text, "ssr://([A-Za-z0-9_-]+)", RegexOptions.IgnoreCase);
            if (!ssr.Success)
                throw new FormatException();

            var data = ShareLink.URLSafeBase64Decode(ssr.Groups[1].Value);
            var paramsDict = new Dictionary<string, string>();

            var paramStartPos = data.IndexOf("?", StringComparison.Ordinal);
            if (paramStartPos > 0)
            {
                paramsDict = ShareLink.ParseParam(data.Substring(paramStartPos + 1));
                data = data.Substring(0, paramStartPos);
            }

            if (data.IndexOf("/", StringComparison.Ordinal) >= 0)
                data = data.Substring(0, data.LastIndexOf("/", StringComparison.Ordinal));

            var urlFinder = new Regex("^(.+):([^:]+):([^:]*):([^:]+):([^:]*):([^:]+)");
            var match = urlFinder.Match(data);

            if (match == null || !match.Success)
                throw new FormatException();

            var serverAddr = match.Groups[1].Value;
            var serverPort = ushort.Parse(match.Groups[2].Value);
            var protocol = match.Groups[3].Value.Length == 0 ? "origin" : match.Groups[3].Value;
            protocol = protocol.Replace("_compatible", "");
            var method = match.Groups[4].Value;
            var obfs = match.Groups[5].Value.Length == 0 ? "plain" : match.Groups[5].Value;
            obfs = obfs.Replace("_compatible", "");
            var password = ShareLink.URLSafeBase64Decode(match.Groups[6].Value);
            var protocolParam = "";
            var obfsParam = "";
            var remarks = "";

            if (paramsDict.ContainsKey("protoparam"))
                protocolParam = ShareLink.URLSafeBase64Decode(paramsDict["protoparam"]);

            if (paramsDict.ContainsKey("obfsparam"))
                obfsParam = ShareLink.URLSafeBase64Decode(paramsDict["obfsparam"]);

            if (paramsDict.ContainsKey("remarks"))
                remarks = ShareLink.URLSafeBase64Decode(paramsDict["remarks"]);

            var group = paramsDict.ContainsKey("group") ? ShareLink.URLSafeBase64Decode(paramsDict["group"]) : string.Empty;

            if (SSGlobal.EncryptMethods.Contains(method) && protocol == "origin" && obfs == "plain")
                return new[]
                {
                    new Shadowsocks.Shadowsocks
                    {
                        Hostname = serverAddr,
                        Port = serverPort,
                        EncryptMethod = method,
                        Password = password,
                        Remark = remarks,
                        Group = group
                    }
                };

            return new[]
            {
                new ShadowsocksR
                {
                    Hostname = serverAddr,
                    Port = serverPort,
                    Protocol = protocol,
                    EncryptMethod = method,
                    OBFS = obfs,
                    Password = password,
                    ProtocolParam = protocolParam,
                    OBFSParam = obfsParam,
                    Remark = remarks,
                    Group = group
                }
            };
        }

        public bool CheckServer(Server s)
        {
            var server = (ShadowsocksR)s;
            if (!SSRGlobal.EncryptMethods.Contains(server.EncryptMethod))
            {
                Global.Logger.Error($"不支持的 SSR 加密方式：{server.EncryptMethod}");
                return false;
            }

            if (!SSRGlobal.Protocols.Contains(server.Protocol))
            {
                Global.Logger.Error($"不支持的 SSR 协议：{server.Protocol}");
                return false;
            }

            if (!SSRGlobal.OBFSs.Contains(server.OBFS))
            {
                Global.Logger.Error($"不支持的 SSR 混淆：{server.OBFS}");
                return false;
            }

            return true;
        }
    }
}