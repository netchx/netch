using System;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;

namespace Netch.Utils
{
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

            var dns = new[] {primaryDns};
            if (secondDns != null)
                dns = dns.Append(secondDns).ToArray();

            var inPar = mo.GetMethodParameters("SetDNSServerSearchOrder");
            inPar["DNSServerSearchOrder"] = dns;

            mo.InvokeMethod("SetDNSServerSearchOrder", inPar, null);
        }
    }
}