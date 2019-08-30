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

        public static List<Objects.Server> Parse(string text)
        {
            var list = new List<Objects.Server>();

            try
            {
                if (text.StartsWith("tg://socks?") || text.StartsWith("https://t.me/socks?"))
                {
                    var data = new Objects.Server();
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

                    data.Address = dict["server"];
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
                    var data = new Objects.Server();
                    data.Type = "Shadowsocks";

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
                            data.OBFS = plugins[0];
                            data.OBFSParam = plugins[1];
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
                }
                else if (text.StartsWith("ssd://"))
                {
                    var json = Newtonsoft.Json.JsonConvert.DeserializeObject<Objects.SSD.Main>(URLSafeBase64Decode(text.Substring(6)));

                    foreach (var server in json.servers)
                    {
                        var data = new Objects.Server();
                        data.Type = "Shadowsocks";

                        data.Remark = server.remarks;
                        data.Address = server.server;
                        data.Port = (server.port != 0) ? server.port : json.port;
                        data.Password = (server.password != null) ? server.password : json.password;
                        data.EncryptMethod = (server.encryption != null) ? server.encryption : json.encryption;

                        if (Global.EncryptMethods.SS.Contains(data.EncryptMethod))
                        {
                            list.Add(data);
                        }
                    }
                }
                else if (text.StartsWith("ssr://"))
                {
                    var data = new Objects.Server();
                    data.Type = "ShadowsocksR";

                    text = text.Substring(6);
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
                        data.Type = "Shadowsocks";
                    }

                    list.Add(data);
                }
                else if (text.StartsWith("vmess://"))
                {
                    var data = new Objects.Server();
                    data.Type = "VMess";

                    text = text.Substring(8);
                    var vmess = Newtonsoft.Json.JsonConvert.DeserializeObject<Objects.VMess>(URLSafeBase64Decode(text));

                    data.Remark = vmess.ps;
                    data.Address = vmess.add;
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

                    if (data.TransferProtocol == "quic")
                    {
                        if (!Global.EncryptMethods.VMessQUIC.Contains(vmess.host))
                        {
                            Logging.Info(String.Format("不支持的 VMess QUIC 加密方式：{0}", vmess.host));
                            return null;
                        }
                        else
                        {
                            data.QUICSecurity = vmess.host;
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
