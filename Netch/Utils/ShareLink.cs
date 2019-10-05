using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

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

        public static List<Models.Server> Parse(string text)
        {
            var list = new List<Models.Server>();

            try
            {
                if (text.StartsWith("tg://socks?") || text.StartsWith("https://t.me/socks?"))
                {
                    var data = new Models.Server();
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
                    if (dict.ContainsKey("user") && !String.IsNullOrWhiteSpace(dict["user"]))
                    {
                        data.Username = dict["user"];
                    }
                    if (dict.ContainsKey("pass") && !String.IsNullOrWhiteSpace(dict["pass"]))
                    {
                        data.Password = dict["pass"];
                    }

                    list.Add(data);
                }
                else if (text.StartsWith("ss://"))
                {
                    var data = new Models.Server();
                    data.Type = "SS";
                    /*
                    try
                    {
                        if(!text.Contains("/?"))
                        {
                            var finder = new Regex(@"ss://(?<base64>[A-Za-z0-9+-/=_]+)(?:#(?<tag>\S+))?", RegexOptions.IgnoreCase);
                            var parser = new Regex(@"^((?<method>.+?):(?<password>.*)@(?<hostname>.+?):(?<port>\d+?))$", RegexOptions.IgnoreCase);
                            var match = finder.Match(text);
                            if (!match.Success)
                            {
                                throw new FormatException();
                            }

                            var base64 = match.Groups["base64"].Value.TrimEnd('/');
                            var tag = match.Groups["tag"].Value;
                            if (!String.IsNullOrEmpty(tag))
                            {
                                data.Remark = HttpUtility.UrlDecode(tag);
                            }

                            match = parser.Match(URLSafeBase64Decode(base64));
                            if (!match.Success)
                            {
                                throw new FormatException();
                            }

                            data.Address = match.Groups["hostname"].Value;
                            data.Port = int.Parse(match.Groups["port"].Value);
                            data.Password = match.Groups["password"].Value;
                            data.EncryptMethod = match.Groups["method"].Value;
                        }
                        else
                        {
                            if (text.Contains("#"))
                            {
                                data.Remark = HttpUtility.UrlDecode(text.Split('#')[1]);
                                text = text.Split('#')[0];
                            }
                            var finder = new Regex(@"ss://(?<base64>.+?)@(?<server>.+?):(?<port>\d+?)/\?plugin=(?<plugin>.+)");
                            var parser = new Regex(@"^(?<method>.+?):(?<password>.+)$");
                            var match = finder.Match(text);
                            if (!match.Success)
                            {
                                throw new FormatException();
                            }

                            data.Address = match.Groups["server"].Value;
                            data.Port = int.Parse(match.Groups["port"].Value);
                            var plugins = HttpUtility.UrlDecode(match.Groups["plugin"].Value).Split(';');
                            if (plugins[0] == "obfs-local")
                                plugins[0] = "simple-obfs";

                            var base64 = URLSafeBase64Decode(match.Groups["base64"].Value);
                            match = parser.Match(base64);
                            if (!match.Success)
                            {
                                throw new FormatException();
                            }

                            data.EncryptMethod = match.Groups["method"].Value;
                            data.Password = match.Groups["password"].Value;
                            data.Plugin = plugins[0];
                            data.PluginOption = plugins[1];
                        }

                        if (!Global.EncryptMethods.SS.Contains(data.EncryptMethod))
                        {
                            Logging.Info(String.Format("不支持的 SS 加密方式：{0}", data.EncryptMethod));
                            return null;
                        }

                        list.Add(data);
                    }
                    catch (FormatException)
                    {
                        try
                        {
                            var uri = new Uri(text);
                            var userinfo = URLSafeBase64Decode(uri.UserInfo).Split(new char[] { ':' }, 2);
                            if (userinfo.Length != 2)
                            {
                                return null;
                            }

                            data.Remark = uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);
                            data.Address = uri.IdnHost;
                            data.Port = uri.Port;
                            data.Password = userinfo[1];
                            data.EncryptMethod = userinfo[0];

                            if (!Global.EncryptMethods.SS.Contains(data.EncryptMethod))
                            {
                                Logging.Info(String.Format("不支持的 SS 加密方式：{0}", data.EncryptMethod));
                                return null;
                            }

                            list.Add(data);
                        }
                        catch (UriFormatException)
                        {
                            return null;
                        }
                    }
                    */
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
                            var finder = new Regex(@"^(?<data>.+?)\?plugin=(?<plugin>.+)$");
                            var match = finder.Match(text);

                            if (match.Success)
                            {
                                var plugins = HttpUtility.UrlDecode(match.Groups["plugin"].Value);
                                var plugin = plugins.Substring(0, plugins.IndexOf(";"));
                                var pluginopts = plugins.Substring(plugins.IndexOf(";") + 1);
                                if (plugin == "obfs-local" || plugin == "simple-obfs")
                                {
                                    plugin = "simple-obfs";
                                    if (!pluginopts.Contains("obfs="))
                                        pluginopts = "obfs=http;obfs-host=" + pluginopts;
                                }
                                else if(plugin == "simple-obfs-tls")
                                {
                                    plugin = "simple-obfs";
                                    if (!pluginopts.Contains("obfs="))
                                        pluginopts = "obfs=tls;obfs-host=" + pluginopts;
                                }

                                data.Plugin = plugin;
                                data.PluginOption = pluginopts;
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
                            Logging.Info(String.Format("不支持的 SS 加密方式：{0}", data.EncryptMethod));
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
                    var json = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.SSD.Main>(URLSafeBase64Decode(text.Substring(6)));

                    foreach (var server in json.servers)
                    {
                        var data = new Models.Server();
                        data.Type = "SS";

                        data.Remark = server.remarks;
                        data.Hostname = server.server;
                        data.Port = (server.port != 0) ? server.port : json.port;
                        data.Password = (server.password != null) ? server.password : json.password;
                        data.EncryptMethod = (server.encryption != null) ? server.encryption : json.encryption;
                        data.Plugin = (String.IsNullOrEmpty(json.plugin)) ? (String.IsNullOrEmpty(server.plugin) ? null : server.plugin) : json.plugin;
                        data.PluginOption = (String.IsNullOrEmpty(json.plugin_options)) ? (String.IsNullOrEmpty(server.plugin_options) ? null : server.plugin_options) : json.plugin_options;

                        if (Global.EncryptMethods.SS.Contains(data.EncryptMethod))
                        {
                            list.Add(data);
                        }
                    }
                }
                else if (text.StartsWith("ssr://"))
                {
                    var data = new Models.Server();
                    data.Type = "SSR";

                    text = text.Substring(6);
                    /*
                    var shadowsocksr = URLSafeBase64Decode(text).Split(':');

                    if (shadowsocksr.Length > 6)
                    {
                        var buff = "";

                        for (int i = 0; i < shadowsocksr.Length - 5; i++)
                        {
                            buff += shadowsocksr[i];
                            buff += ":";
                        }

                        data.Address = buff.Substring(0, buff.Length - 1).Trim();
                    }
                    else
                    {
                        data.Address = shadowsocksr[0];
                    }
                    data.Port = int.Parse(shadowsocksr[shadowsocksr.Length - 5]);
                    data.Protocol = shadowsocksr[shadowsocksr.Length - 4];
                    if (!Global.Protocols.Contains(data.Protocol))
                    {
                        Logging.Info(String.Format("不支持的 SSR 协议：{0}", data.Protocol));
                        return null;
                    }

                    data.EncryptMethod = shadowsocksr[shadowsocksr.Length - 3];
                    if (!Global.EncryptMethods.SSR.Contains(data.EncryptMethod))
                    {
                        Logging.Info(String.Format("不支持的 SSR 加密方式：{0}", data.EncryptMethod));
                        return null;
                    }

                    data.OBFS = shadowsocksr[shadowsocksr.Length - 2];
                    if (!Global.OBFSs.Contains(data.OBFS))
                    {
                        Logging.Info(String.Format("不支持的 SSR 混淆：{0}", data.OBFS));
                        return null;
                    }

                    var info = shadowsocksr[shadowsocksr.Length - 1].Split('/');
                    data.Password = URLSafeBase64Decode(info[0]);

                    var dict = new Dictionary<string, string>();
                    if (info.Length > 1 && info[1].Length > 1)
                    {
                        foreach (var str in info[1].Substring(1).Split('&'))
                        {
                            var splited = str.Split('=');

                            dict.Add(splited[0], splited[1]);
                        }
                    }

                    if (dict.ContainsKey("remarks"))
                    {
                        data.Remark = URLSafeBase64Decode(dict["remarks"]);
                    }

                    if (dict.ContainsKey("protoparam"))
                    {
                        data.ProtocolParam = URLSafeBase64Decode(dict["protoparam"]);
                    }

                    if (dict.ContainsKey("obfsparam"))
                    {
                        data.OBFSParam = URLSafeBase64Decode(dict["obfsparam"]);
                    }

                    if (data.EncryptMethod != "none" && data.Protocol == "origin" && data.OBFS == "plain")
                    {
                        data.Type = "SS";
                    }
                    */
                    var parser = new Regex(@"^(?<server>.+):(?<port>\d+?):(?<protocol>.+?):(?<method>.+?):(?<obfs>.+?):(?<password>.+?)/\?(?<info>.*)$");
                    var match = parser.Match(URLSafeBase64Decode(text));

                    if(match.Success)
                    {
                        data.Hostname = match.Groups["server"].Value;
                        data.Port = int.Parse(match.Groups["port"].Value);
                        data.Password = URLSafeBase64Decode(match.Groups["password"].Value);

                        data.EncryptMethod = match.Groups["method"].Value;
                        if (!Global.EncryptMethods.SSR.Contains(data.EncryptMethod))
                        {
                            Logging.Info(String.Format("不支持的 SSR 加密方式：{0}", data.EncryptMethod));
                            return null;
                        }

                        data.Protocol = match.Groups["protocol"].Value;
                        if (!Global.Protocols.Contains(data.Protocol))
                        {
                            Logging.Info(String.Format("不支持的 SSR 协议：{0}", data.Protocol));
                            return null;
                        }

                        data.OBFS = match.Groups["obfs"].Value;
                        if (!Global.OBFSs.Contains(data.OBFS))
                        {
                            Logging.Info(String.Format("不支持的 SSR 混淆：{0}", data.OBFS));
                            return null;
                        }

                        var info = match.Groups["info"].Value;
                        var dict = new Dictionary<string, string>();
                        foreach (var str in info.Split('&'))
                        {
                            var splited = str.Split('=');
                            dict.Add(splited[0], splited[1]);
                        }

                        if (dict.ContainsKey("remarks"))
                        {
                            data.Remark = URLSafeBase64Decode(dict["remarks"]);
                        }

                        if (dict.ContainsKey("protoparam"))
                        {
                            data.ProtocolParam = URLSafeBase64Decode(dict["protoparam"]);
                        }

                        if (dict.ContainsKey("obfsparam"))
                        {
                            data.OBFSParam = URLSafeBase64Decode(dict["obfsparam"]);
                        }

                        if (Global.EncryptMethods.SS.Contains(data.EncryptMethod) && data.Protocol == "origin" && data.OBFS == "plain")
                        {
                            data.OBFS = "";
                            data.Type = "SS";
                        }
                    }

                    list.Add(data);
                }
                else if (text.StartsWith("vmess://"))
                {
                    var data = new Models.Server();
                    data.Type = "VMess";

                    text = text.Substring(8);
                    var vmess = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.VMess>(URLSafeBase64Decode(text));

                    data.Remark = vmess.ps;
                    data.Hostname = vmess.add;
                    data.Port = vmess.port;
                    data.UserID = vmess.id;
                    data.AlterID = vmess.aid;

                    data.TransferProtocol = vmess.net;
                    if (!Global.TransferProtocols.Contains(data.TransferProtocol))
                    {
                        Logging.Info(String.Format("不支持的 VMess 传输协议：{0}", data.TransferProtocol));
                        return null;
                    }

                    data.FakeType = vmess.type;
                    if (!Global.FakeTypes.Contains(data.FakeType))
                    {
                        Logging.Info(String.Format("不支持的 VMess 伪装类型：{0}", data.FakeType));
                        return null;
                    }

                    if(vmess.v == null || vmess.v == "1")
                    {
                        var info = vmess.host.Split(';');
                        if(info.Length == 2)
                        {
                            vmess.host = info[0];
                            vmess.path = info[1];
                        }
                    }
                    if (data.TransferProtocol == "quic")
                    {
                        if (!Global.EncryptMethods.VMessQUIC.Contains(vmess.host))
                        {
                            Logging.Info(String.Format("不支持的 VMess QUIC 加密方式：{0}", vmess.host));
                            return null;
                        }
                        else
                        {
                            data.QUICSecure = vmess.host;
                            data.QUICSecret = vmess.path;
                        }

                    }
                    else
                    {
                        data.Host = vmess.host;
                        data.Path = vmess.path;
                    }
                    data.TLSSecure = vmess.tls == "tls";

                    data.EncryptMethod = "auto"; // V2Ray 加密方式不包括在链接中，主动添加一个

                    list.Add(data);
                }
                else if (text.StartsWith("Netch://"))
                {
                    text = text.Substring(8);
                    var NetchLink = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.Server>(URLSafeBase64Decode(text));
                    if (!String.IsNullOrEmpty(NetchLink.Hostname) || NetchLink.Port > 65536 || NetchLink.Port > 0)
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
                                Logging.Info($"不支持的 SS 加密方式：{NetchLink.EncryptMethod}");
                                return null;
                            }
                            break;
                        case "SSR":
                            if (!Global.EncryptMethods.SSR.Contains(NetchLink.EncryptMethod))
                            {
                                Logging.Info($"不支持的 SSR 加密方式：{NetchLink.EncryptMethod}");
                                return null;
                            }
                            if (!Global.Protocols.Contains(NetchLink.Protocol))
                            {
                                Logging.Info($"不支持的 SSR 协议：{NetchLink.Protocol}");
                                return null;
                            }
                            if (!Global.OBFSs.Contains(NetchLink.OBFS))
                            {
                                Logging.Info($"不支持的 SSR 混淆：{NetchLink.OBFS}");
                                return null;
                            }
                            break;
                        case "VMess":
                            if (!Global.TransferProtocols.Contains(NetchLink.TransferProtocol))
                            {
                                Logging.Info($"不支持的 VMess 传输协议：{NetchLink.TransferProtocol}");
                                return null;
                            }
                            if (!Global.FakeTypes.Contains(NetchLink.FakeType))
                            {
                                Logging.Info($"不支持的 VMess 伪装类型：{NetchLink.FakeType}");
                                return null;
                            }
                            if (NetchLink.TransferProtocol == "quic")
                            {
                                if (!Global.EncryptMethods.VMessQUIC.Contains(NetchLink.QUICSecure))
                                {
                                    Logging.Info($"不支持的 VMess QUIC 加密方式：{NetchLink.QUICSecure}");
                                    return null;
                                }
                            }
                            break;
                        default:
                            return null;
                    }
                    list.Add(NetchLink);
                }

                else
                {
                    System.Windows.Forms.MessageBox.Show("未找到可导入的链接！", "错误", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
                    return null;
                }
            }
            catch (Exception e)
            {
                Logging.Info(e.ToString());
                return null;
            }

            return list;
        }
    }
}
