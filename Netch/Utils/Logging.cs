using System;
using System.IO;

namespace Netch.Utils
{
    public static class Logging
    {

        /// <summary>
        ///     信息
        /// </summary>
        /// <param name="text">内容</param>
        public static void Info(string text)
        {
            File.AppendAllText("logging\\application.log", $@"[{DateTime.Now}] {text}{Global.EOF}");
        }
    }
}
