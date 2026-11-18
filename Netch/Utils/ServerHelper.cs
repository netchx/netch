using System.Reflection;
using Netch.Enums;
using Netch.Interfaces;

namespace Netch.Utils;

public static class ServerHelper
{
    static ServerHelper()
    {
        var serversUtilsTypes = Assembly.GetExecutingAssembly()
            .GetExportedTypes()
            .Where(type => type.GetInterfaces().Contains(typeof(IServerUtil)));

        ServerUtilDictionary = serversUtilsTypes.Select(t => (IServerUtil)Activator.CreateInstance(t)!).ToDictionary(util => util.TypeName);
    }

    public static Dictionary<string, IServerUtil> ServerUtilDictionary { get; }

    public static string GetDisplayName(EConfigType type) => GetDisplayName(GetUtilByTypeName(type.ToString()));

    public static string GetDisplayName(IServerUtil util) => util.TypeName switch
    {
        nameof(EConfigType.SOCKS) => "SOCKS5",
        nameof(EConfigType.HTTP) => "HTTP / HTTPS",
        nameof(EConfigType.ShadowsocksR) => "ShadowsocksR (SSR)",
        _ => util.FullName
    };

    public static IServerUtil GetUtilByTypeName(string typeName)
    {
        if (typeName.Equals("Anytls", StringComparison.OrdinalIgnoreCase))
            typeName = nameof(EConfigType.AnyTLS);

        return ServerUtilDictionary.GetValueOrDefault(typeName) ?? throw new NotSupportedException("Specified server type is not supported.");
    }

    public static IServerUtil? GetUtilByUriScheme(string scheme)
    {
        return ServerUtilDictionary.Values.SingleOrDefault(i => i.UriScheme.Any(s => s.Equals(scheme)));
    }

    public static Type GetTypeByTypeName(string typeName)
    {
        return GetUtilByTypeName(typeName).ServerType;
    }
}
