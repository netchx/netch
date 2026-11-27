using Netch.Models;
using Netch.Servers;

namespace Netch.Utils;

public enum NodeSortField
{
    None,
    Name,
    Core,
    Delay
}

public static class NodeSortHelper
{
    public static string GetDisplayName(Server server)
    {
        ArgumentNullException.ThrowIfNull(server);
        return string.IsNullOrWhiteSpace(server.Remarks)
            ? $"{server.Address}:{server.Port}"
            : server.Remarks;
    }

    public static IEnumerable<Server> Sort(
        IEnumerable<Server> servers,
        NodeSortField field,
        bool descending,
        StringComparer? textComparer = null)
    {
        ArgumentNullException.ThrowIfNull(servers);
        var comparer = textComparer ?? StringComparer.CurrentCultureIgnoreCase;

        return field switch
        {
            NodeSortField.Name => descending
                ? servers.OrderByDescending(GetDisplayName, comparer)
                    .ThenByDescending(static s => s.Address ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(static s => s.Port)
                : servers.OrderBy(GetDisplayName, comparer)
                    .ThenBy(static s => s.Address ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static s => s.Port),
            NodeSortField.Core => (descending
                    ? servers.OrderByDescending(static s => CoreSelector.GetCoreDisplayName(s), comparer)
                    : servers.OrderBy(static s => CoreSelector.GetCoreDisplayName(s), comparer))
                .ThenBy(static s => ServerHelper.GetDisplayName(s.ConfigType), comparer)
                .ThenBy(GetDisplayName, comparer),
            NodeSortField.Delay => servers
                .OrderBy(s => DelaySortKey(s, descending))
                .ThenBy(GetDisplayName, comparer),
            _ => servers
        };
    }

    private static (int Group, int Value, int InvalidRank) DelaySortKey(Server server, bool descending)
    {
        var delay = server.Delay;
        if (DelayStatus.IsValid(delay))
            return (0, descending ? -delay : delay, 0);

        return (1, 0, InvalidDelayRank(delay));
    }

    private static int InvalidDelayRank(int delay) => delay switch
    {
        DelayStatus.Testing => 0,
        DelayStatus.Untested => 1,
        DelayStatus.Timeout => 2,
        DelayStatus.Unreachable => 3,
        DelayStatus.DnsFailed => 4,
        _ => 5
    };
}
