namespace Netch;

public static class Constants
{
    public const string TempConfig = "data\\last.json";
    public const string TempRouteFile = "data\\route.txt";

    public const string AioDnsRuleFile = "bin\\aiodns.conf";
    public const string NFDriver = "bin\\nfdriver.sys";
    public const string STUNServersFile = "bin\\stun.txt";

    public const string LogFile = "logging\\application.log";

    public const string OutputTemplate = @"[{Timestamp:yyyy-MM-dd HH:mm:ss}][{Level}] {Message:lj}{NewLine}{Exception}";
    public const string EOF = "\r\n";

    public const string DefaultGroup = "NONE";

    public static class Parameter
    {
        public const string Show = "-show";
        public const string ForceUpdate = "-forceUpdate";
    }

    public const string WintunDllFile = "bin\\wintun.dll";
    public const string DisableModeDirectoryFileName = "disabled";

    public const string DefaultPrimaryDNS = "1.1.1.1";
    public const string DefaultCNPrimaryDNS = "223.5.5.5";
}