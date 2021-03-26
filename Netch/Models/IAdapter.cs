using System.Net;
using System.Net.NetworkInformation;

namespace Netch.Models
{
    public interface IAdapter
    {
        string AdapterId { get; }

        int InterfaceIndex { get; }

        IPAddress Gateway { get; }

        NetworkInterface NetworkInterface { get; }
    }
}