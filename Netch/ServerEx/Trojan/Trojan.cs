using Netch.Models;

namespace Netch.ServerEx.Trojan
{
    public class Trojan : Server
    {
        public Trojan()
        {
            Type = "Trojan";
        }

        public string Password { get; set; }
        public string Host { get; set; }
    }
}