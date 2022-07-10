using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public class SSHUtil : IServerUtil
{
    public ushort Priority { get; } = 4;

    public string TypeName { get; } = "SSH";

    public string FullName { get; } = "SSH";

    public string ShortName { get; } = "SSH";

    public string[] UriScheme { get; } = { "ssh" };

    public Type ServerType { get; } = typeof(SSHServer);

    public void Edit(Server s)
    {
        new SSHForm((SSHServer)s).ShowDialog();
    }

    public void Create()
    {
        new SSHForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        return V2rayUtils.GetVShareLink(s, "ssh");
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