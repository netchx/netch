using Netch.Enums;
using Netch.Forms;
using Netch.Utils;

namespace Netch.Servers;

[Fody.ConfigureAwait(true)]
public class HttpForm : ServerForm
{
    public HttpForm(HttpServer? server = default)
    {
        server ??= new HttpServer();
        Server = server;
        CreateTextBox("Username", "Username", s => true, s => server.Username = s, server.Username);
        CreateTextBox("Password", "Password", s => true, s => server.Password = s, server.Password);
    }

    protected override EConfigType TypeName { get; } = EConfigType.HTTP;
}