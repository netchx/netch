using Netch.Models;
using Netch.Services;
using Microsoft.VisualStudio.Threading;

namespace Netch.Utils;

public static class SubscriptionUtil
{
    private static readonly AsyncSemaphore UpdateLock = new(1);

    public static Task UpdateServersAsync(string? proxyServer = default)
    {
        return Task.WhenAll(Global.Settings.Subscription.Select(item => UpdateServerAsync(item, proxyServer)));
    }

    public static async Task<int> UpdateServerAsync(Subscription item, string? proxyServer = null)
    {
        using var update = await UpdateLock.EnterAsync();
        try
        {
            if (!item.Enable)
                return 0;

            item.LastChecked = DateTimeOffset.UtcNow;
            var link = item.Link;
            using var client = WebUtil.CreateHttpClient(userAgent: item.UserAgent, proxyServer: proxyServer);
            using var response = await client.GetAsync(link);
            response.EnsureSuccessStatusCode();
            var servers = ShareLink.ParseText(await response.Content.ReadAsStringAsync(), out var skipped);
            if (servers.Count == 0)
                throw new InvalidDataException(i18N.Translate("The subscription contains no supported nodes."));
            if (!item.Enable || !Global.Settings.Subscription.Contains(item) || item.Link != link)
                return 0;

            var groupName = NodeGroupHelper.NormalizeGroup(item.Remark);
            foreach (var server in servers)
            {
                server.Group = groupName;
                server.IsSub = true;
            }

            InheritIds(Global.Settings.Server.Where(server => server.IsSub && NodeGroupHelper.GroupEquals(server.Group, groupName)).ToList(), servers);
            Global.Settings.Server.RemoveAll(server => server.IsSub && NodeGroupHelper.GroupEquals(server.Group, groupName));
            Global.Settings.Server.AddRange(servers);
            item.LastUpdated = DateTimeOffset.UtcNow;
            item.UpdateFailed = false;
            item.TrafficRemaining = null;
            item.Expiration = null;
            if (response.Headers.TryGetValues("subscription-userinfo", out var values))
            {
                var info = values.SelectMany(value => value.Split(';'))
                    .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
                    .Where(part => part.Length == 2).ToLookup(part => part[0], part => part[1], StringComparer.OrdinalIgnoreCase);
                if (ulong.TryParse(info["total"].FirstOrDefault(), out var total) && total > 0)
                {
                    ulong.TryParse(info["upload"].FirstOrDefault(), out var upload);
                    ulong.TryParse(info["download"].FirstOrDefault(), out var download);
                    item.TrafficRemaining = upload <= total && download <= total - upload ? total - upload - download : 0;
                }
                if (long.TryParse(info["expire"].FirstOrDefault(), out var expires) && expires is > 0 and <= 253402300799)
                    item.Expiration = DateTimeOffset.FromUnixTimeSeconds(expires);
            }

            var message = i18N.TranslateFormat("Update {1} server(s) from {0}", item.Remark, servers.Count);
            if (skipped > 0)
                message += "\n" + i18N.TranslateFormat("Skipped {0} invalid or unsupported nodes.", skipped);
            AppEvents.Notify(message);
            return servers.Count;
        }
        catch (Exception e)
        {
            item.UpdateFailed = true;
            AppEvents.Notify($"{i18N.TranslateFormat("Update servers failed from {0}", item.Remark)}\n{e.Message}", false);
            Log.Warning(e, "Update servers failed");
            return 0;
        }
        finally
        {
            AppEvents.RaiseSubscriptionChanged();
        }
    }

    /// <summary>
    ///     Carries node ids over a subscription update so scenes keep their node: first match on
    ///     protocol, address, port and name, then on protocol, address and port alone.
    /// </summary>
    public static void InheritIds(List<Server> previous, IReadOnlyList<Server> updated)
    {
        var unmatched = updated.ToList();
        foreach (var byName in new[] { true, false })
        {
            foreach (var server in unmatched.ToList())
            {
                var match = previous.FirstOrDefault(old => old.ConfigType == server.ConfigType
                                                           && old.Port == server.Port
                                                           && string.Equals(old.Address, server.Address, StringComparison.OrdinalIgnoreCase)
                                                           && (!byName || old.Remarks == server.Remarks));
                if (match == null)
                    continue;

                server.Id = match.Id;
                previous.Remove(match);
                unmatched.Remove(server);
            }
        }
    }
}
