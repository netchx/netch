using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Netch.Interops;
using Netch.Models;

namespace Netch.Utils;

// #define DEBUG_TUN
public static class RouteUtils
{
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
        var Result = RouteHelper.CreateRoute(AddressFamily.InterNetwork, o.Network, o.Cidr, o.Gateway, (ulong)o.InterfaceIndex, o.Metric);

#if DEBUG_TUN
        Log.Verbose("CreateRoute {InterNetwork} {Address} {Cidr} {Gateway} {Interface} {Metric} Result: {Result}",
            AddressFamily.InterNetwork,
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
        var Result = RouteHelper.DeleteRoute(AddressFamily.InterNetwork, o.Network, o.Cidr, o.Gateway, (ulong)o.InterfaceIndex, o.Metric);

#if DEBUG_TUN
        Log.Verbose("DeleteRoute {InterNetwork} {Address} {Cidr} {Gateway} {Interface} {Metric} Result: {}",
            AddressFamily.InterNetwork,
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
        if (s.Length != 2)
            return false;

        ip = s[0];
        cidr = int.Parse(s[1]);
        return true;
    }
}