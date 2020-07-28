using System;
using System.IO;
using Netch.Models;

namespace Netch.Utils
{
    public static partial class Logging
    {
        public const string LogFile = "logging\\application.log";

        /// <summary>
        ///     信息
        /// </summary>
        /// <param name="text">内容</param>
        public static void Info(string text)
        {
            Write(text, LogLevel.INFO);
        }

        /// <summary>
        ///     信息
        /// </summary>
        /// <param name="text">内容</param>
        public static void Warning(string text)
        {
            Write(text, LogLevel.WARNING);
        }

        /// <summary>
        ///     错误
        /// </summary>
        /// <param name="text">内容</param>
        public static void Error(string text)
        {
            Write(text, LogLevel.ERROR);
        }

        private static void Write(string text, LogLevel logLevel)
        {
            File.AppendAllText(LogFile, $@"[{DateTime.Now}][{logLevel.ToString()}] {text}{Global.EOF}");
        }
    }
}