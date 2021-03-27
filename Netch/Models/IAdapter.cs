using System.Net;
using System.Net.NetworkInformation;

namespace Netch.Models
{
    public interface IAdapter
    {
        int InterfaceIndex { get; }

        IPAddress Gateway { get; }

        NetworkInterface NetworkInterface { get; }
    }
}