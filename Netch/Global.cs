using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using WindowsJobAPI;
using Netch.Controllers;
using Netch.Forms;
using Netch.Models;

namespace Netch
{
    public static class Global
    {
        /// <summary>
        ///     换行
        /// </summary>
        public const string EOF = "\r\n";

        public const string UserACL = "data\\user.acl";
        public const string BuiltinACL = "bin\\default.acl";

        public static readonly string NetchDir = Application.StartupPath;

        public static readonly string NetchExecutable = Application.ExecutablePath;

        /// <summary>
        ///     主窗体的静态实例
        /// </summary>
        public static MainForm MainForm;

        public static readonly Mutex Mutex = new(false, "Global\\Netch");

#if DEBUG
        public static bool Testing = false;
#else
        public const bool Testing = false;
#endif

        /// <summary>
        ///     用于读取和写入的配置
        /// </summary>
        public static Setting Settings = new();

        /// <summary>
        ///     用于存储模式
        /// </summary>
        public static readonly List<Mode> Modes = new();

        /// <summary>
        ///     Windows Job API
        /// </summary>
        public static readonly JobObject Job = new();

        public static class Flags
        {
            public static readonly bool IsWindows10Upper = Environment.OSVersion.Version.Major >= 10;

            private static bool? _supportFakeDns;

            public static bool SupportFakeDns => _supportFakeDns ??= new TUNTAPController().TestFakeDNS();
        }

        /// <summary>
        ///     出口适配器
        /// </summary>
        public static class Outbound
        {
            /// <summary>
            ///     索引
            /// </summary>
            public static int Index = -1;

            /// <summary>
            ///     网关
            /// </summary>
            public static IPAddress Gateway;

            public static NetworkInterface Adapter;

            /// <summary>
            ///     地址
            /// </summary>
            public static IPAddress Address => Adapter.GetIPProperties()
                .UnicastAddresses.First(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
                .Address;
        }

        /// <summary>
        ///     TUN/TAP 适配器
        /// </summary>
        public static class TUNTAP
        {
            /// <summary>
            ///     适配器
            /// </summary>
            public static NetworkInterface Adapter;

            /// <summary>
            ///     索引
            /// </summary>
            public static int Index = -1;

            /// <summary>
            ///     组件 ID
            /// </summary>
            public static string ComponentID = string.Empty;
        }
    }
}