namespace Netch
{
    public static class Constants
    {
        public const string EOF = "\r\n";
        public const string OutputTemplate = @"[{Timestamp:yyyy-MM-dd HH:mm:ss}][{Level}] {Message:lj}{NewLine}{Exception}";
        public const string LogFile = "logging\\application.log";

        public static class Parameter
        {
            public const string Show = "-show";
            public const string ForceUpdate = "-forceUpdate";
        }
    }
}