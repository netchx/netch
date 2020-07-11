using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Netch.Models;
using Netch.Models.SS;
using Netch.Models.SSD;
using Newtonsoft.Json;
using Server = Netch.Models.Server;

namespace Netch.Utils
{
    public static class ShareLink
    {
        /// <summary>
		///		URL 传输安全的 Base64 解码
		/// </summary>
		/// <param name="text">需要解码的字符串</param>
		/// <returns>解码后的字符串</returns>
        public static string URLSafeBase64Decode(string text)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(text.Replace("-", "+").Replace("_", "/").PadRight(text.Length + (4 - text.Length % 4) % 4, '=')));
        }
        /// <summary>
        /// URL 传输安全的 Base64 加密
        /// </summary>
        /// <param name="text">需要加密的字符串</param>
        /// <returns>加密后的字符串</returns>
        public static string URLSafeBase64Encode(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        /// <summary>
        ///		根据服务器生成分享链接
        /// </summary>
        /// <param name="server">需要获取分享链接的服务器</param>
        /// <returns>解码后的字符串</returns>
        public static string GetShareLink(Server server)
        {
            var retLinkStr = "";
            switch (server.Type)
            {
                case "Socks5":
                    // https://t.me/socks?server=1.1.1.1&port=443
                    retLinkStr = string.Format("https://t.me/socks?server={0}&port={1}", server.Hostname, server.Port);

                    break;
                case "SS":
                    // ss://method:password@server:port#Remark
                    retLinkStr = "ss://" + URLSafeBase64Encode(string.Format("{0}:{1}@{2}:{3}", server.EncryptMethod, server.Password, server.Hostname, server.Port)) + "#" + HttpUtility.UrlEncode(server.Remark);

                    break;
                case "SSR":
                    // https://github.com/shadowsocksr-backup/shadowsocks-rss/wiki/SSR-QRcode-scheme
                    // ssr://base64(host:port:protocol:method:obfs:base64pass/?obfsparam=base64param&protoparam=base64param&remarks=base64remarks&group=base64group&udpport=0&uot=0)
                    var paraStr = string.Format("/?obfsparam={0}&protoparam={1}&remarks={2}", URLSafeBase64Encode(server.OBFSParam), URLSafeBase64Encode(server.ProtocolParam), URLSafeBase64Encode(server.Remark));
                    retLinkStr = "ssr://" + URLSafeBase64Encode(string.Format("{0}:{1}:{2}:{3}:{4}:{5}{6}", server.Hostname, server.Port, server.Protocol, server.EncryptMethod, server.OBFS, URLSafeBase64Encode(server.Password), paraStr));

                    break;
                case "VMess":
                    var vmessJson = JsonConvert.SerializeObject(new
                    {
                        v = "2",
                        ps = server.Remark,
                        add = server.Hostname,
                        port = server.Port,
                        id = server.UserID,
                        aid = server.AlterID,
                        net = server.TransferProtocol,
                        type = server.FakeType,
                        host = server.Host,
                        path = server.Path,
                        tls = server.TLSSecure ? "tls" : ""
                    });
                    retLinkStr = "vmess://" + URLSafeBase64Encode(vmessJson);

                    break;
                default:
                    return null;
            }
            return retLinkStr;
        }

        public static List<Server> Parse(string text)
        {
            var list = new List<Server>();
            try
            {
                try
                {
                    var ssServers = JsonConvert.DeserializeObject<List<ShadowsocksServer>>(text);
                    list.AddRange(ssServers.Select(shadowsocksServer => new Server
                    {
                        Type = "SS",
                        Hostname = shadowsocksServer.server,
                        Port = shadowsocksServer.server_port,
                        EncryptMethod = shadowsocksServer.method,
                        Password = shadowsocksServer.password,
                        Remark = shadowsocksServer.remarks,
                        Plugin = shadowsocksServer.plugin,
                        PluginOption = shadowsocksServer.plugin_opts
                    }));
                }
                catch (JsonReaderException)
                {
                    foreach (var line in text.GetLines())
                    {
                        var servers = ParseLine(line);
                        if (servers != null)
                        {
                            list.AddRange(servers);
                        }
                    }
                }

                if (list.Count == 0)
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
                return null;
            }

            return list;
        }

        private static IEnumerable<Server> ParseLine(string text)
        {
            var list = new List<Server>();

            try
            {
                if (text.StartsWith("tg://socks?") || text.StartsWith("https://t.me/socks?"))
                {
                    var data = new Server();
                    data.Type = "Socks5";

                    var dict = new Dictionary<string, string>();
                    foreach (var str in text.Replace("tg://socks?", "").Replace("https://t.me/socks?", "").Split('&'))
                    {
                        var splited = str.Split('=');

                        dict.Add(splited[0], splited[1]);
                    }

                    if (!dict.ContainsKey("server") || !dict.ContainsKey("port"))
                    {
                        return null;
                    }

                    data.Hostname = dict["server"];
                    data.Port = int.Parse(dict["port"]);
                    if (dict.ContainsKey("user") && !string.IsNullOrWhiteSpace(dict["user"]))
                    {
                        data.Username = dict["user"];
                    }
                    if (dict.ContainsKey("pass") && !string.IsNullOrWhiteSpace(dict["pass"]))
                    {
                        data.Password = dict["pass"];
                    }

                    list.Add(data);
                }
                else if (text.StartsWith("ss://"))
                {
                    var data = new Server();
                    data.Type = "SS";

                    text = text.Replace("/?", "?");
                    try
                    {
                        if (text.Contains("#"))
                        {
                            data.Remark = HttpUtility.UrlDecode(text.Split('#')[1]);
                            text = text.Split('#')[0];
                        }
                        if (text.Contains("?"))
                        {
                            var finder = new Regex(@"^(?<data>.+?)\?(.+)$");
                            var match = finder.Match(text);

                            if (match.Success)
                            {
                                var plugins = HttpUtility.UrlDecode(HttpUtility.ParseQueryString(new Uri(text).Query).Get("plugin"));
                                if (plugins != null)
                                {
                                    var plugin = plugins.Substring(0, plugins.IndexOf(";", StringComparison.Ordinal));
                                    var pluginopts = plugins.Substring(plugins.IndexOf(";", StringComparison.Ordinal) + 1);
                                    if (plugin == "obfs-local" || plugin == "simple-obfs")
                                    {
                                        plugin = "simple-obfs";
                                        if (!pluginopts.Contains("obfs="))
                                            pluginopts = "obfs=http;obfs-host=" + pluginopts;
                                    }
                                    else if (plugin == "simple-obfs-tls")
                                    {
                                        plugin = "simple-obfs";
                                        if (!pluginopts.Contains("obfs="))
                                            pluginopts = "obfs=tls;obfs-host=" + pluginopts;
                                    }

                                    data.Plugin = plugin;
                                    data.PluginOption = pluginopts;
                                }

                                text = match.Groups["data"].Value;
                            }
                            else
                            {
                                throw new FormatException();
                            }
                        }
                        if (text.Contains("@"))
                        {
                            var finder = new Regex(@"^ss://(?<base64>.+?)@(?<server>.+):(?<port>\d+)");
                            var parser = new Regex(@"^(?<method>.+?):(?<password>.+)$");
                            var match = finder.Match(text);
                            if (!match.Success)
                            {
                                throw new FormatException();
                            }

                            data.Hostname = match.Groups["server"].Value;
                            data.Port = int.Parse(match.Groups["port"].Value);

                            var base64 = URLSafeBase64Decode(match.Groups["base64"].Value);
                            match = parser.Match(base64);
                            if (!match.Success)
                            {
                                throw new FormatException();
                            }

                            data.EncryptMethod = match.Groups["method"].Value;
                            data.Password = match.Groups["password"].Value;
                        }
                        else
                        {
                            var parser = new Regex(@"^((?<method>.+?):(?<password>.+)@(?<server>.+):(?<port>\d+))");
                            var match = parser.Match(URLSafeBase64Decode(text.Replace("ss://", "")));
                            if (!match.Success)
                            {
                                throw new FormatException();
                            }

                            data.Hostname = match.Groups["server"].Value;
                            data.Port = int.Parse(match.Groups["port"].Value);
                            data.EncryptMethod = match.Groups["method"].Value;
                            data.Password = match.Groups["password"].Value;
                        }

                        if (!Global.EncryptMethods.SS.Contains(data.EncryptMethod))
                        {
                            Logging.Error($"不支持的 SS 加密方式：{data.EncryptMethod}");
                            return null;
                        }

                        list.Add(data);
                    }
                    catch (FormatException)
                    {
                        return null;
                    }
                }
                else if (text.StartsWith("ssd://"))
                {
                    var json = JsonConvert.DeserializeObject<Main>(URLSafeBase64Decode(text.Substring(6)));

                    foreach (var server in json.servers)
                    {
                        var data = new Server();
                        data.Type = "SS";

                        data.Remark = server.remarks;
                        data.Hostname = server.server;
                        data.Port = server.port != 0 ? server.port : json.port;
                        data.Password = server.password != null ? server.password : json.password;
                        data.EncryptMethod = server.encryption != null ? server.encryption : json.encryption;
                        data.Plugin = string.IsNullOrEmpty(json.plugin) ? string.IsNullOrEmpty(server.plugin) ? null : server.plugin : json.plugin;
                        data.PluginOption = string.IsNullOrEmpty(json.plugin_options) ? string.IsNullOrEmpty(server.plugin_options) ? null : server.plugin_options : json.plugin_options;

                        if (Global.EncryptMethods.SS.Contains(data.EncryptMethod))
                        {
                            list.Add(data);
                        }
                    }
                }
                else if (text.StartsWith("ssr://"))
                {

                    list.Add(SsrServerFromLink(text));

                }
                else if (text.StartsWith("vmess://"))
                {
                    var data = new Server();
                    data.Type = "VMess";

                    text = text.Substring(8);
                    var vmess = JsonConvert.DeserializeObject<VMess>(URLSafeBase64Decode(text));

                    data.Remark = vmess.ps;
                    data.Hostname = vmess.add;
                    data.Port = vmess.port;
                    data.UserID = vmess.id;
                    data.AlterID = vmess.aid;

                    data.TransferProtocol = vmess.net;
                    if (!Global.TransferProtocols.Contains(data.TransferProtocol))
                    {
                        Logging.Error($"不支持的 VMess 传输协议：{data.TransferProtocol}");
                        return null;
                    }

                    data.FakeType = vmess.type;
                    if (data.FakeType.Length != 0 && !Global.FakeTypes.Contains(data.FakeType))
                    {
                        Logging.Error($"不支持的 VMess 伪装类型：{data.FakeType}");
                        return null;
                    }

                    if (vmess.v == null || vmess.v == "1")
                    {
                        var info = vmess.host.Split(';');
                        if (info.Length == 2)
                        {
                            vmess.host = info[0];
                            vmess.path = info[1];
                        }
                    }
                    if (data.TransferProtocol == "quic")
                    {
                        if (!Global.EncryptMethods.VMessQUIC.Contains(vmess.host))
                        {
                            Logging.Error($"不支持的 VMess QUIC 加密方式：{vmess.host}");
                            return null;
                        }

                        data.QUICSecure = vmess.host;
                        data.QUICSecret = vmess.path;

                    }
                    else
                    {
                        data.Host = vmess.host;
                        data.Path = vmess.path;
                    }
                    data.TLSSecure = vmess.tls == "tls";

                    if (vmess.mux == null)
                    {
                        data.UseMux = false;
                    }
                    else
                    {
                        if (vmess.mux.enabled is bool enabled)
                        {
                            data.UseMux = enabled;
                        }
                        else if (vmess.mux.enabled is string muxEnabled)
                        {
                            data.UseMux = muxEnabled == "true";  // 针对使用字符串当作布尔值的情况
                        }
                        else
                        {
                            data.UseMux = false;
                        }
                    }

                    data.EncryptMethod = "auto"; // V2Ray 加密方式不包括在链接中，主动添加一个

                    list.Add(data);
                }
                else if (text.StartsWith("Netch://"))
                {
                    text = text.Substring(8);
                    var NetchLink = JsonConvert.DeserializeObject<Server>(URLSafeBase64Decode(text));
                    if (!string.IsNullOrEmpty(NetchLink.Hostname) || NetchLink.Port > 65536 || NetchLink.Port > 0)
                    {
                        return null;
                    }

                    switch (NetchLink.Type)
                    {
                        case "Socks5":
                            list.Add(NetchLink);
                            break;
                        case "SS":
                            if (!Global.EncryptMethods.SS.Contains(NetchLink.EncryptMethod))
                            {
                                Logging.Error($"不支持的 SS 加密方式：{NetchLink.EncryptMethod}");
                                return null;
                            }
                            break;
                        case "SSR":
                            if (!Global.EncryptMethods.SSR.Contains(NetchLink.EncryptMethod))
                            {
                                Logging.Error($"不支持的 SSR 加密方式：{NetchLink.EncryptMethod}");
                                return null;
                            }
                            if (!Global.Protocols.Contains(NetchLink.Protocol))
                            {
                                Logging.Error($"不支持的 SSR 协议：{NetchLink.Protocol}");
                                return null;
                            }
                            if (!Global.OBFSs.Contains(NetchLink.OBFS))
                            {
                                Logging.Error($"不支持的 SSR 混淆：{NetchLink.OBFS}");
                                return null;
                            }
                            break;
                        case "VMess":
                            if (!Global.TransferProtocols.Contains(NetchLink.TransferProtocol))
                            {
                                Logging.Error($"不支持的 VMess 传输协议：{NetchLink.TransferProtocol}");
                                return null;
                            }
                            if (!Global.FakeTypes.Contains(NetchLink.FakeType))
                            {
                                Logging.Error($"不支持的 VMess 伪装类型：{NetchLink.FakeType}");
                                return null;
                            }
                            if (NetchLink.TransferProtocol == "quic")
                            {
                                if (!Global.EncryptMethods.VMessQUIC.Contains(NetchLink.QUICSecure))
                                {
                                    Logging.Error($"不支持的 VMess QUIC 加密方式：{NetchLink.QUICSecure}");
                                    return null;
                                }
                            }
                            break;
                        default:
                            return null;
                    }
                    list.Add(NetchLink);
                }
                else if (text.StartsWith("trojan://"))
                {
                    var data = new Server();
                    data.Type = "Trojan";

                    text = text.Replace("/?", "?");
                    try
                    {
                        if (text.Contains("#"))
                        {
                            data.Remark = HttpUtility.UrlDecode(text.Split('#')[1]);
                            text = text.Split('#')[0];
                        }
                        if (text.Contains("?"))
                        {
                            var reg = new Regex(@"^(?<data>.+?)\?(.+)$");
                            var regmatch = reg.Match(text);

                            if (regmatch.Success)
                            {
                                var peer = HttpUtility.UrlDecode(HttpUtility.ParseQueryString(new Uri(text).Query).Get("peer"));

                                if (peer != null)
                                    data.Host = peer;

                                text = regmatch.Groups["data"].Value;
                            }
                            else
                            {
                                throw new FormatException();
                            }
                        }
                        var finder = new Regex(@"^trojan://(?<psk>.+?)@(?<server>.+):(?<port>\d+)");
                        var match = finder.Match(text);
                        if (!match.Success)
                        {
                            throw new FormatException();
                        }

                        data.Password = match.Groups["psk"].Value;
                        data.Hostname = match.Groups["server"].Value;
                        data.Port = int.Parse(match.Groups["port"].Value);

                        list.Add(data);
                    }
                    catch (FormatException)
                    {
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
                return null;
            }

            byte[] emoji_bytes = { 240, 159 };
            foreach (var node in list)
            {
                var remark = Encoding.UTF8.GetBytes(node.Remark);
                var start_index = 0;
                while (remark.Length > start_index + 1 && remark[start_index] == emoji_bytes[0] && remark[start_index + 1] == emoji_bytes[1])
                    start_index += 4;
                node.Remark = Encoding.UTF8.GetString(remark.Skip(start_index).ToArray()).Trim();
            }

            return list;
        }
        public static string UnBase64String(string value)
        {
            if (value == null || value == "")
            {
                return "";
            }
            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }
        public static string ToBase64String(string value)
        {
            if (value == null || value == "")
            {
                return "";
            }
            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// SSR链接解析器
        /// Copy From https://github.com/HMBSbige/ShadowsocksR-Windows/blob/d9dc8d032a6e04c14b9dc6c8f673c9cc5aa9f607/shadowsocks-csharp/Model/Server.cs#L428
        /// Thx :D
        /// </summary>
        /// <param name="ssrUrl"></param>
        /// <returns></returns>
        public static Server SsrServerFromLink(string ssrUrl)
        {
            // ssr://host:port:protocol:method:obfs:base64pass/?obfsparam=base64&remarks=base64&group=base64&udpport=0&uot=1
            var ssr = Regex.Match(ssrUrl, "ssr://([A-Za-z0-9_-]+)", RegexOptions.IgnoreCase);
            if (!ssr.Success)
                throw new FormatException();

            var data = URLSafeBase64Decode(ssr.Groups[1].Value);
            var params_dict = new Dictionary<string, string>();

            var param_start_pos = data.IndexOf("?", StringComparison.Ordinal);
            if (param_start_pos > 0)
            {
                params_dict = ParseParam(data.Substring(param_start_pos + 1));
                data = data.Substring(0, param_start_pos);
            }
            if (data.IndexOf("/", StringComparison.Ordinal) >= 0)
            {
                data = data.Substring(0, data.LastIndexOf("/", StringComparison.Ordinal));
            }

            var UrlFinder = new Regex("^(.+):([^:]+):([^:]*):([^:]+):([^:]*):([^:]+)");
            var match = UrlFinder.Match(data);

            if (match == null || !match.Success)
                throw new FormatException();

            var serverAddr = match.Groups[1].Value;
            var Server_Port = ushort.Parse(match.Groups[2].Value);
            var Protocol = match.Groups[3].Value.Length == 0 ? "origin" : match.Groups[3].Value;
            Protocol = Protocol.Replace("_compatible", "");
            var Method = match.Groups[4].Value;
            var obfs = match.Groups[5].Value.Length == 0 ? "plain" : match.Groups[5].Value;
            obfs = obfs.Replace("_compatible", "");
            var Password = URLSafeBase64Decode(match.Groups[6].Value);
            var ProtocolParam = "";
            var ObfsParam = "";
            var Remarks = "";
            var Group = "";

            if (params_dict.ContainsKey("protoparam"))
            {
                ProtocolParam = URLSafeBase64Decode(params_dict["protoparam"]);
            }
            if (params_dict.ContainsKey("obfsparam"))
            {
                ObfsParam = URLSafeBase64Decode(params_dict["obfsparam"]);
            }
            if (params_dict.ContainsKey("remarks"))
            {
                Remarks = URLSafeBase64Decode(params_dict["remarks"]);
            }
            Group = params_dict.ContainsKey("group") ? URLSafeBase64Decode(params_dict["group"]) : string.Empty;

            /*if (params_dict.ContainsKey("uot"))
            {
                UdpOverTcp = int.Parse(params_dict["uot"]) != 0;
            }
            if (params_dict.ContainsKey("udpport"))
            {
                Server_Udp_Port = ushort.Parse(params_dict["udpport"]);
            }*/
            var server = new Server();
            server.Type = "SSR";
            server.Hostname = serverAddr;
            server.Port = Server_Port;
            server.Protocol = Protocol;
            server.EncryptMethod = Method;
            server.OBFS = obfs;
            server.Password = Password;
            server.ProtocolParam = ProtocolParam;
            server.OBFSParam = ObfsParam;
            server.Remark = Remarks;
            server.Group = Group;
            return server;
        }
        private static Dictionary<string, string> ParseParam(string paramStr)
        {
            var paramsDict = new Dictionary<string, string>();
            var obfsParams = paramStr.Split('&');
            foreach (var p in obfsParams)
            {
                if (p.IndexOf('=') > 0)
                {
                    var index = p.IndexOf('=');
                    var key = p.Substring(0, index);
                    var val = p.Substring(index + 1);
                    paramsDict[key] = val;
                }
            }
            return paramsDict;
        }
    }
}
