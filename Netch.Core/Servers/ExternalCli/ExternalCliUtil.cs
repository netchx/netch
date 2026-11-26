using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;

namespace Netch.Servers;

public sealed class ExternalCliUtil : ServerUtilBase, IServerUtil
{
    public ushort Priority { get; } = 15;

    public string TypeName { get; } = nameof(EConfigType.ExternalCli);

    public string FullName { get; } = "External CLI";

    public string ShortName { get; } = "CLI";

    public string[] UriScheme { get; } = [];

    public Type ServerType { get; } = typeof(ExternalCliServer);

    public string GetShareLink(Server s) => string.Empty;

    public IEnumerable<Server> ParseUri(string text) => [];

    public bool CheckServer(Server s) => s is ExternalCliServer && ServerValidator.Validate(s) == null;
}
