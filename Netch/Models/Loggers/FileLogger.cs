using Netch.Interfaces;
using System;
using System.IO;

namespace Netch.Models.Loggers
{
    public class FileLogger : ILogger
    {
        public string LogFile { get; set; } = Path.Combine(Global.NetchDir, "logging\\application.log");

        private readonly object _fileLock = new();

        public void Info(string text)
        {
            Write(text, LogLevel.INFO);
        }

        public void Warning(string text)
        {
            Write(text, LogLevel.WARNING);
        }

        public void Error(string text)
        {
            Write(text, LogLevel.ERROR);
        }

        public void Write(string text, LogLevel logLevel)
        {
            var contents = $@"[{DateTime.Now}][{logLevel.ToString()}] {text}{Constants.EOF}";

            lock (_fileLock)
                File.AppendAllText(LogFile, contents);
        }

        public void Debug(string s)
        {
#if DEBUG
            Write(s, LogLevel.DEBUG);
#endif
        }

        public void ShowLog()
        {
            Utils.Utils.Open(LogFile);
        }
    }
}