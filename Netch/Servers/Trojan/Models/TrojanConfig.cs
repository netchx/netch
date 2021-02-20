using System.Collections.Generic;

namespace Netch.Servers.Trojan.Models
{
    public class TrojanConfig
    {
        /// <summary>
        ///     监听地址
        /// </summary>
        public string local_addr = "127.0.0.1";

        /// <summary>
        ///     监听端口
        /// </summary>
        public int local_port = 2801;

        /// <summary>
        ///     日志级别
        /// </summary>
        public int log_level = 1;

        /// <summary>
        ///     密码
        /// </summary>
        public List<string> password;

        /// <summary>
        ///     远端地址
        /// </summary>
        public string remote_addr;

        /// <summary>
        ///     远端端口
        /// </summary>
        public int remote_port;
        /// <summary>
        ///     启动类型
        /// </summary>
        public string run_type = "client";

        public TrojanSSL ssl = new();
        public TrojanTCP tcp = new();
    }

    public class TrojanSSL
    {
        public List<string> alpn = new()
        {
            "h2",
            "http/1.1"
        };
        public string cert;
        public string cipher =
            "ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-AES256-SHA:ECDHE-ECDSA-AES128-SHA:ECDHE-RSA-AES128-SHA:ECDHE-RSA-AES256-SHA:DHE-RSA-AES128-SHA:DHE-RSA-AES256-SHA:AES128-SHA:AES256-SHA:DES-CBC3-SHA";
        public string cipher_tls13 = "TLS_AES_128_GCM_SHA256:TLS_CHACHA20_POLY1305_SHA256:TLS_AES_256_GCM_SHA384";
        public string curves = "";

        public bool reuse_session = true;
        public bool session_ticket = true;
        public string sni = string.Empty;
        public bool verify = false;
        public bool verify_hostname = false;
    }

    public class TrojanTCP
    {
        public bool fast_open = true;
        public int fast_open_qlen = 20;
        public bool keep_alive = true;
        public bool no_delay = false;
        public bool reuse_port = false;
    }
}