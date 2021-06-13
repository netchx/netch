using System.Threading.Tasks;

namespace Netch.Interfaces
{
    public interface IConfigService
    {
        string FileFullName { get; }
        Task LoadAsync();
        Task SaveAsync();
    }
}