#nullable disable
using System.Collections.Generic;

namespace Netch.Servers.Trojan.Models
{
    public class TrojanConfig
    {
        /// <summary>
        ///     监听地址
        /// </summary>
        public string local_addr { get; set; } = "127.0.0.1";

        /// <summary>
        ///     监听端口
        /// </summary>
        public int local_port { get; set; } = 2801;

        /// <summary>
        ///     日志级别
        /// </summary>
        public int log_level { get; set; } = 1;

        /// <summary>
        ///     密码
        /// </summary>
        public List<string> password { get; set; }

        /// <summary>
        ///     远端地址
        /// </summary>
        public string remote_addr { get; set; }

        /// <summary>
        ///     远端端口
        /// </summary>
        public int remote_port { get; set; }

        /// <summary>
        ///     启动类型
        /// </summary>
        public string run_type { get; set; } = "client";

        public TrojanSSL ssl { get; set; } = new();

        public TrojanTCP tcp { get; set; } = new();
    }

    public class TrojanSSL
    {
        public List<string> alpn { get; set; } = new()
        {
            "h2",
            "http/1.1"
        };

        public string cert { get; set; }

        public string cipher { get; set; } =
            "ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-AES256-SHA:ECDHE-ECDSA-AES128-SHA:ECDHE-RSA-AES128-SHA:ECDHE-RSA-AES256-SHA:DHE-RSA-AES128-SHA:DHE-RSA-AES256-SHA:AES128-SHA:AES256-SHA:DES-CBC3-SHA";

        public string cipher_tls13 { get; set; } = "TLS_AES_128_GCM_SHA256:TLS_CHACHA20_POLY1305_SHA256:TLS_AES_256_GCM_SHA384";

        public string curves { get; set; } = string.Empty;

        public bool reuse_session { get; set; } = true;

        public bool session_ticket { get; set; } = true;

        public string sni { get; set; } = string.Empty;

        public bool verify { get; set; } = false;

        public bool verify_hostname { get; set; } = false;
    }

    public class TrojanTCP
    {
        public bool fast_open { get; set; } = true;

        public int fast_open_qlen { get; set; } = 20;

        public bool keep_alive { get; set; } = true;

        public bool no_delay { get; set; } = false;

        public bool reuse_port { get; set; } = false;
    }
}