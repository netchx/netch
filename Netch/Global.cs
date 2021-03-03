using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
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
        private static readonly Lazy<MainForm> LazyMainForm = new(() => new MainForm());

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

            private static readonly Lazy<bool> LazySupportFakeDns = new(() => new TUNTAPController().TestFakeDNS());

            public static bool SupportFakeDns => LazySupportFakeDns.Value;
        }

        /// <summary>
        ///     主窗体的静态实例
        /// </summary>
        public static MainForm MainForm => LazyMainForm.Value;

        public static JsonSerializerOptions NewDefaultJsonSerializerOptions => new()
        {
            WriteIndented = true,
            IgnoreNullValues = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}