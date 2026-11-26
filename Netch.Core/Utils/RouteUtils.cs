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

    public static bool TryNormalizeIpOrCidr(string? input, [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmed = input.Trim();
        var slashIndex = trimmed.IndexOf('/');
        var hasPrefix = slashIndex >= 0;
        var addrPart = hasPrefix ? trimmed[..slashIndex].Trim() : trimmed;
        var prefixPart = hasPrefix ? trimmed[(slashIndex + 1)..].Trim() : string.Empty;

        if (hasPrefix && (prefixPart.Length == 0 || prefixPart.Contains('/')))
            return false;

        if (addrPart.Length > 2 && addrPart[0] == '[' && addrPart[^1] == ']')
            addrPart = addrPart[1..^1];

        if (!IPAddress.TryParse(addrPart, out var address) ||
            address.AddressFamily is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6))
            return false;

        int cidr;
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            cidr = 32;
            if (hasPrefix && (!int.TryParse(prefixPart, NumberStyles.None, CultureInfo.InvariantCulture, out cidr) || cidr is < 0 or > 32))
                return false;
        }
        else
        {
            cidr = 128;
            if (hasPrefix && (!int.TryParse(prefixPart, NumberStyles.None, CultureInfo.InvariantCulture, out cidr) || cidr is < 0 or > 128))
                return false;

            if (address.IsIPv4MappedToIPv6 && cidr >= 96)
            {
                address = address.MapToIPv4();
                cidr -= 96;
            }
        }

        var bytes = address.GetAddressBytes();
        var fullBytes = cidr / 8;
        var remBits = cidr % 8;
        if (fullBytes < bytes.Length)
        {
            var zeroStart = fullBytes;
            if (remBits != 0)
            {
                bytes[fullBytes] &= (byte)(0xFF << (8 - remBits));
                zeroStart = fullBytes + 1;
            }

            for (var i = zeroStart; i < bytes.Length; i++)
                bytes[i] = 0;
        }

        normalized = $"{new IPAddress(bytes)}/{cidr}";
        return true;
    }

    public static bool TryToSubnet24(string? ipText, [NotNullWhen(true)] out string? subnetCidr)
    {
        subnetCidr = null;
        if (string.IsNullOrWhiteSpace(ipText))
            return false;

        var trimmed = ipText.Trim();
        var slashIndex = trimmed.IndexOf('/');
        if (slashIndex >= 0)
            trimmed = trimmed[..slashIndex].Trim();

        if (!IPAddress.TryParse(trimmed, out var address))
            return false;

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            bytes[3] = 0;
            subnetCidr = $"{new IPAddress(bytes)}/24";
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            for (var i = 8; i < bytes.Length; i++)
                bytes[i] = 0;
            subnetCidr = $"{new IPAddress(bytes)}/64";
            return true;
        }

        return false;
    }
}
