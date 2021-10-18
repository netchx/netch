using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Netch.Utils
{
    public static class Netfilter
    {
        public static class Methods
        {
            [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern bool aio_register([MarshalAs(UnmanagedType.LPWStr)] string name);

            [DllImport("Redirector.bin", CallingConvention = CallingConvention.Cdecl)]
            public static extern bool aio_unregister([MarshalAs(UnmanagedType.LPWStr)] string driverName);
        }

        public static readonly string dName = "netfilter2";
        public static readonly string oPath = Path.Combine(Environment.SystemDirectory, "drivers\\netfilter2.sys");
        public static readonly string nPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin\\netfilter2.sys");

        /// <summary>
        ///     注册 Netfilter 驱动
        /// </summary>
        /// <returns></returns>
        public static bool Create()
        {
            try
            {
                if (!Delete())
                {
                    return false;
                }

                File.Copy(nPath, oPath);
                if (!Methods.aio_register(dName))
                {
                    Global.Logger.Error($"注册 Netfilter 驱动失败");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Global.Logger.Error(e.ToString());

                return false;
            }
        }

        /// <summary>
        ///     更新 Netfilter 驱动
        /// </summary>
        /// <returns></returns>
        public static bool Update()
        {
            if (!File.Exists(oPath))
            {
                return Create();
            }

            if (!FileHelper.Equals(oPath, nPath))
            {
                return Create();
            }

            return true;
        }

        /// <summary>
        ///     删除 Netfilter 驱动
        /// </summary>
        /// <returns></returns>
        public static bool Delete()
        {
            try
            {
                if (File.Exists(oPath))
                {
                    if (!Methods.aio_unregister(dName))
                    {
                        Global.Logger.Error($"取消注册 Netfilter 驱动失败");
                        return false;
                    }

                    File.Delete(oPath);
                }
            }
            catch (Exception e)
            {
                Global.Logger.Error($"删除 Netfilter 驱动失败：{e}");
            }

            return true;
        }
    }
}
