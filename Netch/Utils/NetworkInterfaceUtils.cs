using System;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;

namespace Netch.Utils
{
    public static class NetworkInterfaceUtils
    {
        /// <summary>
        /// </summary>
        /// <param name="interfaceIndex"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <returns></returns>
        public static NetworkInterface Get(int interfaceIndex)
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .First(n =>
                {
                    var ipProperties = n.GetIPProperties();
                    int index;
                    if (n.Supports(NetworkInterfaceComponent.IPv4))
                        index = ipProperties.GetIPv4Properties().Index;
                    else if (n.Supports(NetworkInterfaceComponent.IPv6))
                        index = ipProperties.GetIPv6Properties().Index;
                    else
                        return false;

                    return index == interfaceIndex;
                });
        }
    }

    public static class NetworkInterfaceExtension
    {
        public static void SetDns(this NetworkInterface ni, string primaryDns, string? secondDns = null)
        {
            void VerifyDns(ref string s)
            {
                s = s.Trim();
                if (primaryDns.IsNullOrEmpty())
                    throw new ArgumentException("DNS format invalid", nameof(primaryDns));
            }

            VerifyDns(ref primaryDns);
            if (secondDns != null)
                VerifyDns(ref primaryDns);

            var wmi = new ManagementClass("Win32_NetworkAdapterConfiguration");
            var mos = wmi.GetInstances().Cast<ManagementObject>();

            var mo = mos.First(m => m["Description"].ToString() == ni.Description);

            var dns = new[] { primaryDns };
            if (secondDns != null)
                dns = dns.Append(secondDns).ToArray();

            var inPar = mo.GetMethodParameters("SetDNSServerSearchOrder");
            inPar["DNSServerSearchOrder"] = dns;

            mo.InvokeMethod("SetDNSServerSearchOrder", inPar, null);
        }
    }
}