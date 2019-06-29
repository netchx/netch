using System;
using System.IO;

namespace Netch.Utils
{
    public static class Logging
    {
        /// <summary>
        ///     换行
        /// </summary>
        public static string EOF = "\r\n";

        /// <summary>
        ///     信息
        /// </summary>
        /// <param name="text">内容</param>
        public static void Info(string text)
        {
            File.AppendAllText("logging\\application.log", String.Format("[{0}] {1}{2}", DateTime.Now, text, EOF));
        }
    }
}
