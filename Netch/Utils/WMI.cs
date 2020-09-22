using System.Management;

namespace Netch.Utils
{
    static internal class WMI
    {
        public static ManagementObject GetManagementObjectByDeviceNameOrDefault(string deviceName)
        {
            foreach (ManagementObject mo in new ManagementClass("Win32_NetworkAdapterConfiguration").GetInstances())
            {
                if (((string) mo["Caption"]).EndsWith(deviceName))
                {
                    return mo;
                }
            }

            return null;
        }
    }
}