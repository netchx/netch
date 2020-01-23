namespace Netch.Models.SS
{
    public class ShadowsocksServer
    {
        public string server { get; set; }
        public int server_port { get; set; }
        public string password { get; set; }
        public string method { get; set; }
        public string remarks { get; set; }
        public string plugin { get; set; }
        public string plugin_opts { get; set; }
    }
}