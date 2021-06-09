using Netch.Forms;
using Netch.Models;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Netch.Services;

namespace Netch
{
    public static class Global
    {
        private static readonly Lazy<MainForm> LazyMainForm = new(DI.GetRequiredService<MainForm>);
        private static readonly Lazy<Setting> LazySetting = new(DI.GetRequiredService<Setting>);

        public static readonly List<Mode> Modes = new();

        public static Setting Settings => LazySetting.Value;

        public static MainForm MainForm => LazyMainForm.Value;

        public static readonly string NetchDir = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string NetchExecutable = Application.ExecutablePath;
    }
}