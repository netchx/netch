using Netch.Models;

namespace Netch.Interfaces
{
    public interface IModeController : IController
    {
        public void Start(in Mode mode);
    }
}