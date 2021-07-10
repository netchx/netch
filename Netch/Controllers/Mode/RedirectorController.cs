using System;
using System.Runtime.InteropServices;

namespace Netch.Controllers.Mode
{
    public class RedirectorController : Interface.IController
    {
        private enum NameList : int
        {
            TYPE_FILTERLOOPBACK,
            TYPE_FILTERTCP,
            TYPE_FILTERUDP,
            TYPE_CLRNAME,
            TYPE_ADDNAME,
            TYPE_BYPNAME,
            TYPE_DNSHOST,
            TYPE_TCPLISN,
            TYPE_TCPTYPE,
            TYPE_TCPHOST,
            TYPE_TCPUSER,
            TYPE_TCPPASS,
            TYPE_TCPMETH,
            TYPE_TCPPROT,
            TYPE_TCPPRPA,
            TYPE_TCPOBFS,
            TYPE_TCPOBPA,
            TYPE_UDPLISN,
            TYPE_UDPTYPE,
            TYPE_UDPHOST,
            TYPE_UDPUSER,
            TYPE_UDPPASS,
            TYPE_UDPMETH,
            TYPE_UDPPROT,
            TYPE_UDPPRPA,
            TYPE_UDPOBFS,
            TYPE_UDPOBPA
        }

        private static class Methods
        {
            [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern bool aio_dial(NameList name, [MarshalAs(UnmanagedType.LPWStr)] string value);

            [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern bool aio_init();

            [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern bool aio_free();

            [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern ulong aio_getUP();

            [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern ulong aio_getDL();
        }

        public bool Create(Models.Server.Server s, Models.Mode.Mode m)
        {
            Global.Logger.Info(String.Format("{0:x} Redirector.bin", Utils.FileHelper.Checksum("Bin\\Redirector.bin")));

            var mode = m as Models.Mode.ProcessMode.ProcessMode;
            Methods.aio_dial(NameList.TYPE_FILTERLOOPBACK, mode.Loopback ? "true" : "false");
            Methods.aio_dial(NameList.TYPE_FILTERTCP, mode.TCP ? "true" : "false");
            Methods.aio_dial(NameList.TYPE_FILTERUDP, mode.UDP ? "true" : "false");

            Methods.aio_dial(NameList.TYPE_CLRNAME, "");
            Methods.aio_dial(NameList.TYPE_BYPNAME, AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "\\\\"));
            for (int i = 0; i < mode.HandleList.Count; i++) if (!Methods.aio_dial(NameList.TYPE_ADDNAME, mode.HandleList[i])) return false;
            for (int i = 0; i < mode.BypassList.Count; i++) if (!Methods.aio_dial(NameList.TYPE_BYPNAME, mode.BypassList[i])) return false;

            Methods.aio_dial(NameList.TYPE_TCPLISN, Global.Config.Ports.Redir.ToString());
            Methods.aio_dial(NameList.TYPE_UDPLISN, Global.Config.Ports.Redir.ToString());

            switch (s.Type)
            {
                case Models.Server.ServerType.Socks:
                    {
                        var node = s as Models.Server.Socks.Socks;
                        Methods.aio_dial(NameList.TYPE_TCPTYPE, "Socks");
                        Methods.aio_dial(NameList.TYPE_UDPTYPE, "Socks");
                        Methods.aio_dial(NameList.TYPE_TCPHOST, $"{node.Resolve()}:{node.Port}");
                        Methods.aio_dial(NameList.TYPE_UDPHOST, $"{node.Resolve()}:{node.Port}");

                        if (!String.IsNullOrEmpty(node.Username))
                        {
                            Methods.aio_dial(NameList.TYPE_TCPUSER, node.Username);
                            Methods.aio_dial(NameList.TYPE_UDPUSER, node.Username);
                        }

                        if (!String.IsNullOrEmpty(node.Password))
                        {
                            Methods.aio_dial(NameList.TYPE_TCPPASS, node.Password);
                            Methods.aio_dial(NameList.TYPE_UDPPASS, node.Password);
                        }
                    }
                    break;
                case Models.Server.ServerType.Shadowsocks:
                    {
                        var node = s as Models.Server.Shadowsocks.Shadowsocks;
                        if (String.IsNullOrEmpty(node.OBFS))
                        {
                            Methods.aio_dial(NameList.TYPE_TCPTYPE, "Shadowsocks");
                            Methods.aio_dial(NameList.TYPE_UDPTYPE, "Shadowsocks");
                            Methods.aio_dial(NameList.TYPE_TCPHOST, $"{node.Resolve()}:{node.Port}");
                            Methods.aio_dial(NameList.TYPE_UDPHOST, $"{node.Resolve()}:{node.Port}");
                            Methods.aio_dial(NameList.TYPE_TCPPASS, node.Passwd);
                            Methods.aio_dial(NameList.TYPE_UDPPASS, node.Passwd);
                            Methods.aio_dial(NameList.TYPE_TCPMETH, node.Method);
                            Methods.aio_dial(NameList.TYPE_UDPMETH, node.Method);
                        }
                        else
                        {
                            Methods.aio_dial(NameList.TYPE_TCPTYPE, "Socks");
                            Methods.aio_dial(NameList.TYPE_UDPTYPE, "Socks");
                            Methods.aio_dial(NameList.TYPE_TCPHOST, $"127.0.0.1:{Global.Config.Ports.Socks}");
                            Methods.aio_dial(NameList.TYPE_UDPHOST, $"127.0.0.1:{Global.Config.Ports.Socks}");
                        }
                    }
                    break;
                default:
                    {
                        Methods.aio_dial(NameList.TYPE_TCPTYPE, "Socks");
                        Methods.aio_dial(NameList.TYPE_UDPTYPE, "Socks");
                        Methods.aio_dial(NameList.TYPE_TCPHOST, $"127.0.0.1:{Global.Config.Ports.Socks}");
                        Methods.aio_dial(NameList.TYPE_UDPHOST, $"127.0.0.1:{Global.Config.Ports.Socks}");
                    }
                    break;
            }

            return Methods.aio_init();
        }

        public bool Delete()
        {
            return Methods.aio_free();
        }
    }
}
