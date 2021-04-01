using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Netch.Interops
{
    public static class NFAPI
    {
        private const string nfapinet_bin = "nfapinet.dll";

        [DllImport(nfapinet_bin, CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_registerDriver(string driverName);

        [DllImport(nfapinet_bin, CallingConvention = CallingConvention.Cdecl)]
        public static extern NF_STATUS nf_unRegisterDriver(string driverName);
    }
}