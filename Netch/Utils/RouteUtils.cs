using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using Netch.Interops;
using Netch.Models;

namespace Netch.Utils;

// #define DEBUG_TUN
public static class RouteUtils
{
    private const uint ErrorObjectAlreadyExists = 5010;

    public static void CreateRouteFill(NetRoute template, IEnumerable<string> rules, int? metric = null)
    {
        foreach (var rule in rules)
            CreateRouteFill(template, rule, metric);
    }

    public static bool CreateRouteFill(NetRoute template, string rule, int? metric = null)
    {
        if (!TryParseIPNetwork(rule, out var network, out var cidr))
        {
            Log.Warning("invalid rule {Rule}", rule);
            return false;
        }

        return CreateRoute(template.FillTemplate(network, (byte)cidr, metric));
    }

    public static bool CreateRoute(NetRoute o)
    {
        if (!IPAddress.TryParse(o.Network, out var network))
            return false;

        var family = network.AddressFamily;
        if (family is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6))
            return false;

        var status = RouteHelper.CreateRouteStatus(family, o.Network, o.Cidr, o.Gateway, (ulong)o.InterfaceIndex, o.Metric);
        var Result = status is 0 or ErrorObjectAlreadyExists;

        if (status == ErrorObjectAlreadyExists)
        {
            Log.Information("CreateRoute already exists, reusing: {Address}/{Cidr} via {Gateway} interface {Interface} metric {Metric}",
                o.Network, o.Cidr, o.Gateway, o.InterfaceIndex, o.Metric);
        }
        else if (!Result)
        {
            Log.Warning("CreateRoute failed: {Address}/{Cidr} via {Gateway} interface {Interface} metric {Metric}, Windows status {Status} ({StatusName})",
                o.Network, o.Cidr, o.Gateway, o.InterfaceIndex, o.Metric, status, new Win32Exception((int)status).Message);
        }

#if DEBUG_TUN
        Log.Verbose("CreateRoute {InterNetwork} {Address} {Cidr} {Gateway} {Interface} {Metric} Result: {Result}",
            family,
            o.Network,
            o.Cidr,
            o.Gateway,
            (ulong)o.InterfaceIndex,
            o.Metric,
            Result);
#endif
        return Result;
    }

    public static void DeleteRouteFill(NetRoute template, IEnumerable<string> rules, int? metric = null)
    {
        foreach (var rule in rules)
            DeleteRouteFill(template, rule, metric);
    }

    public static bool DeleteRouteFill(NetRoute template, string rule, int? metric = null)
    {
        if (!TryParseIPNetwork(rule, out var network, out var cidr))
        {
            Log.Warning("invalid rule {Rule}", rule);
            return false;
        }

        return DeleteRoute(template.FillTemplate(network, (byte)cidr, metric));
    }

    public static bool DeleteRoute(NetRoute o)
    {
        if (!IPAddress.TryParse(o.Network, out var network))
            return false;

        var family = network.AddressFamily;
        if (family is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6))
            return false;

        var Result = RouteHelper.DeleteRoute(family, o.Network, o.Cidr, o.Gateway, (ulong)o.InterfaceIndex, o.Metric);

#if DEBUG_TUN
        Log.Verbose("DeleteRoute {InterNetwork} {Address} {Cidr} {Gateway} {Interface} {Metric} Result: {Result}",
            family,
            o.Network,
            o.Cidr,
            o.Gateway,
            (ulong)o.InterfaceIndex,
            o.Metric,
            Result);
#endif
        return Result;
    }

    public static bool TryParseIPNetwork(string ipNetwork, [NotNullWhen(true)] out string? ip, out int cidr)
    {
        ip = null;
        cidr = 0;

        var s = ipNetwork.Split('/');
        if (s.Length != 2 ||
            !IPAddress.TryParse(s[0], out var address) ||
            address.AddressFamily is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6) ||
            !int.TryParse(s[1], NumberStyles.None, CultureInfo.InvariantCulture, out cidr) ||
            cidr is < 0 || (address.AddressFamily == AddressFamily.InterNetwork && cidr > 32) ||
            (address.AddressFamily == AddressFamily.InterNetworkV6 && cidr > 128))
            return false;

        ip = address.ToString();
        return true;
    }
}
