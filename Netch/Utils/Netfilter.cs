using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Netch.Utils
{
    public static class Netfilter
    {
        public static class Methods
        {
            public enum NF_STATUS : int
            {
                NF_STATUS_SUCCESS = 0,
                NF_STATUS_FAIL = -1,
                NF_STATUS_INVALID_ENDPOINT_ID = -2,
                NF_STATUS_NOT_INITIALIZED = -3,
                NF_STATUS_IO_ERROR = -4
            }

            [DllImport("nfapinet", CallingConvention = CallingConvention.Cdecl)]
            public static extern NF_STATUS nf_registerDriver(string name);

            [DllImport("nfapinet", CallingConvention = CallingConvention.Cdecl)]
            public static extern NF_STATUS nf_unRegisterDriver(string driverName);
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
                var status = Methods.nf_registerDriver(dName);
                if (status != Methods.NF_STATUS.NF_STATUS_SUCCESS)
                {
                    Global.Logger.Error($"注册 Netfilter 驱动失败：{status}");
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
                    var status = Methods.nf_unRegisterDriver(dName);
                    if (status != Methods.NF_STATUS.NF_STATUS_SUCCESS)
                    {
                        Global.Logger.Error($"取消注册 Netfilter 驱动失败：{status}");
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
