using DNS.Protocol;
using System.Threading;
using System.Threading.Tasks;

namespace DNS.Client.RequestResolver
{
    public interface IRequestResolver
    {
        Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default(CancellationToken));
    }
}
