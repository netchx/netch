using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using WindowsProxy;

namespace Netch.Utils.HttpProxyHandler
{
    /// <summary>
    /// 提供PAC功能支持
    /// </summary>
    class PACServerHandle
    {
        private static Hashtable httpWebServer = new Hashtable();
        private static Hashtable pacList = new Hashtable();

        public static void InitPACServer(string address)
        {
            try
            {
                if (!pacList.ContainsKey(address))
                {
                    pacList.Add(address, GetPacList(address));
                }

                string prefixes = string.Format("http://{0}:{1}/pac/", address, Global.Settings.Pac_Port);

                HttpWebServer ws = new HttpWebServer(SendResponse, prefixes);
                ws.Run();

                if (!httpWebServer.ContainsKey(address) && ws != null)
                {
                    httpWebServer.Add(address, ws);
                }
                Global.Settings.Pac_Url = GetPacUrl();

                using var service = new ProxyService
                {
                    AutoConfigUrl = Global.Settings.Pac_Url
                };
                service.Pac();

                Logging.Info(service.Set(service.Query()) + "");
                Logging.Info($"Webserver InitServer OK: {Global.Settings.Pac_Url}");
            }
            catch (Exception ex)
            {
                Logging.Error("Webserver InitServer " + ex.Message);
            }
        }

        public static string SendResponse(HttpListenerRequest request)
        {
            try
            {
                string[] arrAddress = request.UserHostAddress.Split(':');
                string address = "127.0.0.1";
                if (arrAddress.Length > 0)
                {
                    address = arrAddress[0];
                }
                return pacList[address].ToString();
            }
            catch (Exception ex)
            {
                Logging.Error("Webserver SendResponse " + ex.Message);
                return ex.Message;
            }
        }

        public static void Stop()
        {
            try
            {
                if (httpWebServer == null)
                {
                    return;
                }
                foreach (var key in httpWebServer.Keys)
                {
                    Logging.Info("Webserver Stop " + key.ToString());
                    ((HttpWebServer)httpWebServer[key]).Stop();
                }
                httpWebServer.Clear();
            }
            catch (Exception ex)
            {
                Logging.Error("Webserver Stop " + ex.Message);
            }
        }

        private static string GetPacList(string address)
        {
            try
            {
                List<string> lstProxy = new List<string>();
                lstProxy.Add(string.Format("PROXY {0}:{1};", address, Global.Settings.HTTPLocalPort));

                var proxy = string.Join("", lstProxy.ToArray());
                string strPacfile = Path.Combine(Global.NetchDir, $"bin\\pac.txt");

                var pac = File.ReadAllText(strPacfile, Encoding.UTF8).Replace("__PROXY__", proxy);
                return pac;
            }
            catch
            { }
            return "No pac content";
        }

        /// <summary>
        /// 获取PAC地址
        /// </summary>
        /// <returns></returns>
        public static string GetPacUrl()
        {
            string pacUrl = string.Format("http://127.0.0.1:{0}/pac/?t={1}", Global.Settings.Pac_Port,
                          DateTime.Now.ToString("yyyyMMddHHmmssfff"));

            return pacUrl;
        }
    }
}
