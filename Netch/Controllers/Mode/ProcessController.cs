using System;
using System.Runtime.InteropServices;

namespace Netch.Controllers.Mode
{
    public class ProcessController : Interface.IController
    {
        private enum NameList : int
        {
            AIO_FILTERLOOPBACK,
            AIO_FILTERINTRANET,
            AIO_FILTERICMP,
            AIO_FILTERTCP,
            AIO_FILTERUDP,

            AIO_TGTHOST,
            AIO_TGTPORT,
            AIO_TGTUSER,
            AIO_TGTPASS,

            AIO_CLRNAME,
            AIO_ADDNAME,
            AIO_BYPNAME
        }

        private static class Methods
        {
            [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern bool aio_dial(NameList name, [MarshalAs(UnmanagedType.LPWStr)] string value);

            [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern bool aio_init();

            [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern void aio_free();

            [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern ulong aio_getUP();

            [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern ulong aio_getDL();
        }

        public bool Create(Models.Server.Server s, Models.Mode.Mode m)
        {
            Global.Logger.Info(String.Format("{0:x} Redirector.bin", Utils.FileHelper.Checksum("bin\\Redirector.bin")));

            var mode = m as Models.Mode.ProcessMode.ProcessMode;
            Methods.aio_dial(NameList.AIO_FILTERLOOPBACK, mode.Loopback ? "true" : "false");
            Methods.aio_dial(NameList.AIO_FILTERINTRANET, mode.Intranet ? "true" : "false");
            Methods.aio_dial(NameList.AIO_FILTERTCP, mode.TCP ? "true" : "false");
            Methods.aio_dial(NameList.AIO_FILTERUDP, mode.UDP ? "true" : "false");

            Methods.aio_dial(NameList.AIO_CLRNAME, "");
            Methods.aio_dial(NameList.AIO_BYPNAME, AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "\\\\"));
            for (int i = 0; i < mode.BypassList.Count; i++) if (!Methods.aio_dial(NameList.AIO_BYPNAME, mode.BypassList[i])) return false;
            for (int i = 0; i < mode.HandleList.Count; i++) if (!Methods.aio_dial(NameList.AIO_ADDNAME, mode.HandleList[i])) return false;

            switch (s.Type)
            {
                case Models.Server.ServerType.Socks:
                    {
                        var node = s as Models.Server.Socks.Socks;
                        Methods.aio_dial(NameList.AIO_TGTHOST, node.Resolve());
                        Methods.aio_dial(NameList.AIO_TGTPORT, node.Port.ToString());

                        if (!String.IsNullOrEmpty(node.Username))
                        {
                            Methods.aio_dial(NameList.AIO_TGTUSER, node.Username);
                        }

                        if (!String.IsNullOrEmpty(node.Password))
                        {
                            Methods.aio_dial(NameList.AIO_TGTPASS, node.Password);
                        }
                    }
                    break;
                default:
                    {
                        Methods.aio_dial(NameList.AIO_TGTHOST, "127.0.0.1");
                        Methods.aio_dial(NameList.AIO_TGTPORT, Global.Config.Ports.Socks.ToString());
                    }
                    break;
            }

            return Methods.aio_init();
        }

        public bool Delete()
        {
            Methods.aio_free();

            return true;
        }
    }
}
