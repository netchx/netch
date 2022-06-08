using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public class VLiteUtil : IServerUtil
{
    public ushort Priority { get; } = 2;

    public string TypeName { get; } = "VLite";

    public string FullName { get; } = "VLite";

    public string ShortName { get; } = "VLU";

    public string[] UriScheme { get; } = { "vlite" };

    public Type ServerType { get; } = typeof(VLiteServer);

    public void Edit(Server s)
    {
        new VLiteForm((VLiteServer)s).ShowDialog();
    }

    public void Create()
    {
        new VLiteForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        return V2rayUtils.GetVShareLink(s, "vlite");
    }

    public IServerController GetController()
    {
        return new V2rayController("v2ray.exe");
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        return V2rayUtils.ParseVUri(text);
    }

    public bool CheckServer(Server s)
    {
        return true;
    }
}