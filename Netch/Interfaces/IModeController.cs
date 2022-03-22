using Netch.Models.Modes;
using Netch.Servers;

namespace Netch.Interfaces;

public interface IModeController : IController
{
    public ModeFeature Features { get; }

    public Task StartAsync(Socks5Server server, Mode mode);
}