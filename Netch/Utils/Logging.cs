using System;
using System.IO;
using Netch.Models;

namespace Netch.Utils
{
    public static class Logging
    {
        public const string LogFile = "logging\\application.log";

        private static readonly object FileLock = new();

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
            var contents = $@"[{DateTime.Now}][{logLevel.ToString()}] {text}{Constants.EOF}";
#if DEBUG
            switch (logLevel)
            {
                case LogLevel.DEBUG:
                case LogLevel.INFO:
                case LogLevel.WARNING:
                    Console.Write(contents);
                    break;
                case LogLevel.ERROR:
                    Console.Error.Write(contents);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
#else
            lock (FileLock)
                File.AppendAllText(LogFile, contents);
#endif
        }

        public static void Debug(string s)
        {
#if DEBUG
            Write(s, LogLevel.DEBUG);
#endif
        }
    }
}