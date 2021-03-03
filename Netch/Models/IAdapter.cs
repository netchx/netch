using System.Net;
using System.Net.NetworkInformation;

namespace Netch.Models
{
    public interface  IAdapter
    {
        public abstract int Index { get; }

        public abstract IPAddress Gateway { get; }

        public abstract NetworkInterface NetworkInterface { get; }

    }
}