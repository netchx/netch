using Netch.Models;

namespace Netch.Interfaces
{
    public interface IModeController : IController
    {
        public void Start(Server server, Mode mode);
    }
}