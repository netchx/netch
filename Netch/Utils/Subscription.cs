using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Netch.Models;
using Serilog;

namespace Netch.Utils
{
    public static class Subscription
    {
        private static readonly object ServerLock = new();

        public static async Task UpdateServersAsync(string? proxyServer = default)
        {
            await Task.WhenAll(Global.Settings.SubscribeLink.Select(item => UpdateServerCoreAsync(item, proxyServer)));
        }

        private static async Task UpdateServerCoreAsync(SubscribeLink item, string? proxyServer)
        {
            try
            {
                if (!item.Enable)
                    return;

                var request = WebUtil.CreateRequest(item.Link);

                if (!string.IsNullOrEmpty(item.UserAgent))
                    request.UserAgent = item.UserAgent;

                if (!string.IsNullOrEmpty(proxyServer))
                    request.Proxy = new WebProxy(proxyServer);

                List<Server> servers;

                var (code, result) = await WebUtil.DownloadStringAsync(request);
                if (code == HttpStatusCode.OK)
                    servers = ShareLink.ParseText(result);
                else
                    throw new Exception($"{item.Remark} Response Status Code: {code}");

                foreach (var server in servers)
                    server.Group = item.Remark;

                lock (ServerLock)
                {
                    Global.Settings.Server.RemoveAll(server => server.Group.Equals(item.Remark));
                    Global.Settings.Server.AddRange(servers);
                }

                Global.MainForm.NotifyTip(i18N.TranslateFormat("Update {1} server(s) from {0}", item.Remark, servers.Count));
            }
            catch (Exception e)
            {
                Global.MainForm.NotifyTip($"{i18N.TranslateFormat("Update servers error from {0}", item.Remark)}\n{e.Message}", info: false);
                Log.Warning(e, "更新服务器失败");
            }
        }
    }
}