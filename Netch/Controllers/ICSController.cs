using System;
using System.Management;
using Netch.Utils;
using NETCONLib;
using WinFW;

namespace Netch.Controllers
{
    public class ICSController
    {
        public static bool Enabled
        {
            get
            {
                TUNTAPController.SearchTapAdapter();
                foreach (NetworkConnection connection in new NetworkConnectionCollection())
                {
                    if (connection.DeviceName == Global.TUNTAP.Adapter.Description)
                    {
                        return connection.SharingEnabled;
                    }
                }

                return false;
            }
        }

        public static bool Enable()
        {
            Utils.Utils.SearchOutboundAdapter();
            TUNTAPController.SearchTapAdapter();

            if (Global.TUNTAP.Adapter == null || Global.Outbound.Adapter == null)
            {
                return false;
            }

            try
            {
                CleanupWMISharingEntries();

                #region Save Outbound IP Config

                var wmi = new ManagementClass("Win32_NetworkAdapterConfiguration");
                var moc = wmi.GetInstances();

                var dhcpEnabled = true;
                string[] ipAddress = null;
                string[] subnetMask = null;
                string[] gateway = null;
                ushort[] gatewayMetric = null;
                string[] dns = null;

                var outboundWmi = WMI.GetManagementObjectByDeviceNameOrDefault(Global.Outbound.Adapter.Description);

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
                    var newIP = outboundWmi.GetMethodParameters("EnableStatic");
                    newIP["IPAddress"] = ipAddress;
                    newIP["SubnetMask"] = subnetMask;
                    outboundWmi.InvokeMethod("EnableStatic", newIP, null);
                    //Set default gateway
                    var newGateway = outboundWmi.GetMethodParameters("SetGateways");
                    newGateway["DefaultIPGateway"] = gateway;
                    newGateway["GatewayCostMetric"] = gatewayMetric;
                    outboundWmi.InvokeMethod("SetGateways", newGateway, null);
                    //Set dns servers
                    var newDNS = outboundWmi.GetMethodParameters("SetDNSServerSearchOrder");
                    newDNS["DNSServerSearchOrder"] = dns;
                    outboundWmi.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);
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
                if (connection.SharingEnabled)
                    connection.DisableSharing();
            }

            CleanupWMISharingEntries();
        }

        private static void CleanupWMISharingEntries()
        {
            var scope = new ManagementScope("root\\Microsoft\\HomeNet");
            scope.Connect();

            var options = new PutOptions();
            options.Type = PutType.UpdateOnly;

            var query = new ObjectQuery("SELECT * FROM HNet_ConnectionProperties");
            var srchr = new ManagementObjectSearcher(scope, query);
            foreach (ManagementObject entry in srchr.Get())
            {
                if ((bool) entry["IsIcsPrivate"])
                    entry["IsIcsPrivate"] = false;
                if ((bool) entry["IsIcsPublic"])
                    entry["IsIcsPublic"] = false;
                entry.Put(options);
            }
        }
    }
}