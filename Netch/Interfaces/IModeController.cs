using System.Threading.Tasks;
using Netch.Models;
using Netch.Servers;

namespace Netch.Interfaces
{
    public interface IModeController : IController
    {
        public Task StartAsync(Socks5Server server, Mode mode);
    }
}