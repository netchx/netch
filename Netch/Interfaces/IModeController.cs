using System.Threading.Tasks;
using Netch.Models;

namespace Netch.Interfaces
{
    public interface IModeController : IController
    {
        public Task StartAsync(Server server, Mode mode);
    }
}