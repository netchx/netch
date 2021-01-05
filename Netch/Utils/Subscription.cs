using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Netch.Models;

namespace Netch.Utils
{
    public static class Subscription
    {
        private static readonly object ServerLock = new object();

        public static async Task UpdateServersAsync(string proxyServer = default)
        {
            await Task.WhenAll(
                Global.Settings.SubscribeLink.Select(item =>
                    Task.Run(() => UpdateServer(item, proxyServer))
                ).ToArray()
            );
        }

        public static void UpdateServer(SubscribeLink item, string proxyServer)
        {
            try
            {
                if (!item.Enable)
                {
                    return;
                }
                var request = WebUtil.CreateRequest(item.Link);

                if (!string.IsNullOrEmpty(item.UserAgent)) request.UserAgent = item.UserAgent;
                if (!string.IsNullOrEmpty(proxyServer)) request.Proxy = new WebProxy(proxyServer);

                List<Server> servers;

                var result = WebUtil.DownloadString(request, out var rep);
                if (rep.StatusCode == HttpStatusCode.OK)
                    servers = ShareLink.ParseText(result);
                else
                    throw new Exception($"{item.Remark} Response Status Code: {rep.StatusCode}");

                foreach (var server in servers) server.Group = item.Remark;

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
                Logging.Error(e.ToString());
            }
        }
    }
}