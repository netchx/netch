using System;
using System.IO;

namespace Netch.Utils
{
    public static class WinTUN
    {
        public static string oPath = Path.Combine(Environment.SystemDirectory, "wintun.dll");
        public static string nPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bin\\wintun.bin");

        /// <summary>
        ///     注册 WinTUN 驱动
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
            }
            catch (Exception e)
            {
                Global.Logger.Error($"注册 WinTUN 驱动失败：{e}");

                return false;
            }

            return true;
        }

        /// <summary>
        ///     更新 WinTUN 驱动
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
        ///     删除 WinTUN 驱动
        /// </summary>
        /// <returns></returns>
        public static bool Delete()
        {
            try
            {
                if (File.Exists(oPath))
                {
                    File.Delete(oPath);
                }
            }
            catch (Exception e)
            {
                Global.Logger.Error($"删除 WinTUN 驱动失败：{e}");

                return false;
            }

            return true;
        }
    }
}
