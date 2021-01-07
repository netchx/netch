using System;
using System.Management;
using Netch.Controllers;
using Netch.Models.WinFW;
using NETCONLib;

namespace Netch.Utils
{
    public static class ICSHelper
    {
        public static bool? Enabled
        {
            get
            {
                AutoSearchTapAdapter();

                if (Global.TUNTAP.Adapter == null)
                    return null;

                foreach (NetworkConnection connection in new NetworkConnectionCollection())
                {
                    try
                    {
                        if (connection.DeviceName == Global.TUNTAP.Adapter?.Description)
                        {
                            return connection.SharingEnabled;
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.Warning(e.ToString());
                    }
                }

                return null;
            }
        }

        private static void AutoSearchTapAdapter()
        {
            if (Global.TUNTAP.Adapter == null)
                TUNTAPController.SearchTapAdapter();
        }

        public static bool Enable()
        {
            Utils.SearchOutboundAdapter(false);
            AutoSearchTapAdapter();

            if (Global.TUNTAP.Adapter == null || Global.Outbound.Adapter == null)
            {
                return false;
            }

            try
            {
                CleanupWMISharingEntries();

                #region Save Outbound IP Config

                bool dhcpEnabled;
                string[] ipAddress = null;
                string[] subnetMask = null;
                string[] gateway = null;
                ushort[] gatewayMetric = null;
                string[] dns = null;

                var outboundWmi = GetManagementObjectByDeviceNameOrDefault(Global.Outbound.Adapter.Description);

                if (outboundWmi == null)
                {
                    return false;
                }

                if (!(dhcpEnabled = (bool) outboundWmi["DHCPEnabled"]))
                {
                    ipAddress = (string[]) outboundWmi["IPAddress"];
                    subnetMask = (string[]) outboundWmi["IPSubnet"];
                    gateway = (string[]) outboundWmi["DefaultIPGateway"];
                    gatewayMetric = (ushort[]) outboundWmi["GatewayCostMetric"];
                    dns = (string[]) outboundWmi["DNSServerSearchOrder"];

                    ipAddress = new[] {ipAddress[0]};
                    subnetMask = new[] {subnetMask[0]};
                }

                #endregion

                #region Setting ICS

                foreach (NetworkConnection connection in new NetworkConnectionCollection())
                {
                    if (connection.DeviceName == Global.TUNTAP.Adapter.Description)
                    {
                        if (connection.SharingEnabled)
                            connection.DisableSharing();
                        connection.EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PUBLIC);
                    }
                    else if (connection.DeviceName == Global.Outbound.Adapter.Description)
                    {
                        if (connection.SharingEnabled)
                            connection.DisableSharing();
                        connection.EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PRIVATE);
                    }
                }

                #endregion

                #region Reset Outbound IP Config

                if (dhcpEnabled)
                {
                    outboundWmi.InvokeMethod("EnableDHCP", null, null);
                }
                else
                {
                    //Set static IP and subnet mask
                    var ip = outboundWmi.GetMethodParameters("EnableStatic");
                    ip["IPAddress"] = ipAddress;
                    ip["SubnetMask"] = subnetMask;
                    outboundWmi.InvokeMethod("EnableStatic", ip, null);
                    //Set default gateway
                    var newGateway = outboundWmi.GetMethodParameters("SetGateways");
                    newGateway["DefaultIPGateway"] = gateway;
                    newGateway["GatewayCostMetric"] = gatewayMetric;
                    outboundWmi.InvokeMethod("SetGateways", newGateway, null);
                    //Set dns servers
                    var newDns = outboundWmi.GetMethodParameters("SetDNSServerSearchOrder");
                    newDns["DNSServerSearchOrder"] = dns;
                    outboundWmi.InvokeMethod("SetDNSServerSearchOrder", newDns, null);
                }

                #endregion

                return true;
            }
            catch (Exception e)
            {
                try
                {
                    Disable();
                }
                catch
                {
                    // ignored
                }

                Logging.Error($"网络连接共享设置失败: {e}");

                return false;
            }
        }

        public static void Disable()
        {
            foreach (NetworkConnection connection in new NetworkConnectionCollection())
            {
                try
                {
                    if (connection.SharingEnabled)
                    {
                        connection.DisableSharing();
                    }
                }
                catch (Exception e)
                {
                    Logging.Warning(e.ToString());
                }
            }

            CleanupWMISharingEntries();
        }

        private static void CleanupWMISharingEntries()
        {
            var scope = new ManagementScope("root\\Microsoft\\HomeNet");
            scope.Connect();

            var searchResults = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM HNet_ConnectionProperties"));
            foreach (var o in searchResults.Get())
            {
                var entry = (ManagementObject) o;
                if ((bool) entry["IsIcsPrivate"])
                    entry["IsIcsPrivate"] = false;
                if ((bool) entry["IsIcsPublic"])
                    entry["IsIcsPublic"] = false;

                entry.Put(new PutOptions
                {
                    Type = PutType.UpdateOnly
                });
            }
        }

        private static ManagementObject GetManagementObjectByDeviceNameOrDefault(string deviceName)
        {
            foreach (var o in new ManagementClass("Win32_NetworkAdapterConfiguration").GetInstances())
            {
                var mo = (ManagementObject) o;
                if (((string) mo["Caption"]).EndsWith(deviceName))
                {
                    return mo;
                }
            }

            return null;
        }
    }
}