using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Netch.Interops;
using Netch.Models;
using Serilog;

namespace Netch.Utils
{
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
            if (o.Network.AddressFamily != o.Gateway.AddressFamily)
            {
                Log.Warning("Address({Address}) and Gateway({Gateway}) Address Family Different", o.Network, o.Gateway);
                return false;
            }

            return RouteHelper.CreateRoute(o.Network.AddressFamily,
                o.Network.ToString(),
                o.Cidr,
                o.Gateway.ToString(),
                (ulong)o.InterfaceIndex,
                o.Metric);
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
            return RouteHelper.DeleteRoute(o.Network.AddressFamily,
                o.Network.ToString(),
                o.Cidr,
                o.Gateway.ToString(),
                (ulong)o.InterfaceIndex,
                o.Metric);
        }

        public static bool TryParseIPNetwork(string ipNetwork, [NotNullWhen(true)] out IPAddress? ip, out int cidr)
        {
            ip = null;
            cidr = 0;

            var s = ipNetwork.Split('/');
            if (s.Length != 2)
                return false;

            ip = IPAddress.Parse(s[0]);
            cidr = int.Parse(s[1]);
            return true;
        }
    }
}