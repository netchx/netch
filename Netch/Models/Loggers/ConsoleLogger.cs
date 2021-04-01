using Netch.Interfaces;
using System;

namespace Netch.Models.Loggers
{
    public class ConsoleLogger : ILogger
    {
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

        private void Write(string text, LogLevel logLevel)
        {
            var contents = $@"[{DateTime.Now}][{logLevel.ToString()}] {text}{Constants.EOF}";
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
        }

        public void Debug(string s)
        {
#if DEBUG
            Write(s, LogLevel.DEBUG);
#endif
        }

        public void ShowLog()
        {
        }
    }
}