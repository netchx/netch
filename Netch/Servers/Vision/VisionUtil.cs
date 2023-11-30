using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public class VisionUtil : IServerUtil
{
    public ushort Priority { get; } = 2;

    public string TypeName { get; } = "Vision";

    public string FullName { get; } = "Vision";

    public string ShortName { get; } = "Vi";

    public string[] UriScheme { get; } = { "vision" };

    public Type ServerType { get; } = typeof(VisionServer);

    public void Edit(Server s)
    {
        new VisionForm((VisionServer)s).ShowDialog();
    }

    public void Create()
    {
        new VisionForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        return V2rayUtils.GetVShareLink(s, "vision");
    }

    public IServerController GetController()
    {
        return new V2rayController();
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