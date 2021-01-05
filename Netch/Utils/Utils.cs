using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaxMind.GeoIP2;
using TaskScheduler;

namespace Netch.Utils
{
    public static class Utils
    {
        public static bool Open(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<int> TCPingAsync(IPAddress ip, int port, int timeout = 1000, CancellationToken ct = default)
        {
            using var client = new TcpClient(ip.AddressFamily);

            var stopwatch = Stopwatch.StartNew();

            var task = client.ConnectAsync(ip, port);

            var resTask = await Task.WhenAny(task, Task.Delay(timeout, ct));

            stopwatch.Stop();
            if (resTask == task && client.Connected)
            {
                var t = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
                return t;
            }

            return timeout;
        }

        public static async Task<int> ICMPing(IPAddress ip, int timeout = 1000)
        {
            var reply = new Ping().Send(ip, timeout);

            if (reply?.Status == IPStatus.Success)
            {
                return Convert.ToInt32(reply.RoundtripTime);
            }

            return timeout;
        }

        public static string GetCityCode(string Hostname)
        {
            if (Hostname.Contains(":"))
            {
                Hostname = Hostname.Split(':')[0];
            }

            string Country;
            try
            {
                var databaseReader = new DatabaseReader("bin\\GeoLite2-Country.mmdb");

                if (IPAddress.TryParse(Hostname, out _))
                {
                    Country = databaseReader.Country(Hostname).Country.IsoCode;
                }
                else
                {
                    var DnsResult = DNS.Lookup(Hostname);

                    if (DnsResult != null)
                    {
                        Country = databaseReader.Country(DnsResult).Country.IsoCode;
                    }
                    else
                    {
                        Country = "Unknown";
                    }
                }
            }
            catch (Exception)
            {
                Country = "Unknown";
            }

            return Country == null ? "Unknown" : Country;
        }

        public static string SHA256CheckSum(string filePath)
        {
            try
            {
                var sha256 = SHA256.Create();
                var fileStream = File.OpenRead(filePath);
                return sha256.ComputeHash(fileStream).Aggregate(string.Empty, (current, b) => current + b.ToString("x2"));
            }
            catch
            {
                return "";
            }
        }

        public static void KillProcessByName(string name)
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(name))
                    if (p.MainModule != null && p.MainModule.FileName.StartsWith(Global.NetchDir))
                        p.Kill();
            }
            catch (Win32Exception e)
            {
                Logging.Error($"结束进程 {name} 错误：" + e.Message);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static string GetFileVersion(string file) => File.Exists(file) ? FileVersionInfo.GetVersionInfo(file).FileVersion : string.Empty;

        public static bool SearchOutboundAdapter(bool logging = true)
        {
            // 寻找出口适配器
            if (Win32Native.GetBestRoute(BitConverter.ToUInt32(IPAddress.Parse("114.114.114.114").GetAddressBytes(), 0),
                0, out var pRoute) != 0)
            {
                Logging.Error("GetBestRoute 搜索失败");
                return false;
            }

            Global.Outbound.Index = pRoute.dwForwardIfIndex;
            // 根据 IP Index 寻找 出口适配器
            try
            {
                var adapter = NetworkInterface.GetAllNetworkInterfaces().First(_ =>
                {
                    try
                    {
                        return _.GetIPProperties().GetIPv4Properties().Index == Global.Outbound.Index;
                    }
                    catch
                    {
                        return false;
                    }
                });
                Global.Outbound.Adapter = adapter;
                Global.Outbound.Gateway = new IPAddress(pRoute.dwForwardNextHop);
                if (logging)
                {
                    Logging.Info($"出口 IPv4 地址：{Global.Outbound.Address}");
                    Logging.Info($"出口 网关 地址：{Global.Outbound.Gateway}");
                    Logging.Info($"出口适配器：{adapter.Name} {adapter.Id} {adapter.Description}, index: {Global.Outbound.Index}");
                }

                return true;
            }
            catch (Exception e)
            {
                Logging.Error($"找不到出口IP所在网卡: {e}");
                return false;
            }
        }

        public static void LoggingAdapters(string id)
        {
            var adapter = NetworkInterface.GetAllNetworkInterfaces().First(adapter => adapter.Id == id);
            Logging.Warning($"检索此网卡信息出错: {adapter.Name} {adapter.Id} {adapter.Description}");
        }

        public static void DrawCenterComboBox(object sender, DrawItemEventArgs e)
        {
            if (sender is ComboBox cbx)
            {
                e.DrawBackground();

                if (e.Index >= 0)
                {
                    var brush = new SolidBrush(cbx.ForeColor);

                    if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                        brush = SystemBrushes.HighlightText as SolidBrush;

                    e.Graphics.DrawString(cbx.Items[e.Index].ToString(), cbx.Font, brush, e.Bounds, new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Center
                    });
                }
            }
        }

        public static void ComponentIterator(in Component component, in Action<Component> func)
        {
            func.Invoke(component);
            switch (component)
            {
                case ListView listView:
                    // ListView sub item
                    foreach (var item in listView.Columns.Cast<ColumnHeader>())
                    {
                        ComponentIterator(item, func);
                    }

                    break;
                case ToolStripMenuItem toolStripMenuItem:
                    // Iterator Menu strip sub item
                    foreach (var item in toolStripMenuItem.DropDownItems.Cast<ToolStripItem>())
                    {
                        ComponentIterator(item, func);
                    }

                    break;
                case MenuStrip menuStrip:
                    // Menu Strip
                    foreach (var item in menuStrip.Items.Cast<ToolStripItem>())
                    {
                        ComponentIterator(item, func);
                    }

                    break;
                case ContextMenuStrip contextMenuStrip:
                    foreach (var item in contextMenuStrip.Items.Cast<ToolStripItem>())
                    {
                        ComponentIterator(item, func);
                    }

                    break;
                case Control control:
                    foreach (var c in control.Controls.Cast<Control>())
                    {
                        ComponentIterator(c, func);
                    }

                    if (control.ContextMenuStrip != null)
                        ComponentIterator(control.ContextMenuStrip, func);

                    break;
            }
        }

        public static void RegisterNetchStartupItem()
        {
            var scheduler = new TaskSchedulerClass();
            scheduler.Connect();
            var folder = scheduler.GetFolder("\\");

            var taskIsExists = false;
            try
            {
                folder.GetTask("Netch Startup");
                taskIsExists = true;
            }
            catch
            {
                // ignored
            }

            if (Global.Settings.RunAtStartup)
            {
                if (taskIsExists)
                    folder.DeleteTask("Netch Startup", 0);

                var task = scheduler.NewTask(0);
                task.RegistrationInfo.Author = "Netch";
                task.RegistrationInfo.Description = "Netch run at startup.";
                task.Principal.RunLevel = _TASK_RUNLEVEL.TASK_RUNLEVEL_HIGHEST;

                task.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON);
                var action = (IExecAction) task.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC);
                action.Path = Application.ExecutablePath;


                task.Settings.ExecutionTimeLimit = "PT0S";
                task.Settings.DisallowStartIfOnBatteries = false;
                task.Settings.RunOnlyIfIdle = false;

                folder.RegisterTaskDefinition("Netch Startup", task, (int) _TASK_CREATION.TASK_CREATE, null, null,
                    _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN, "");
            }
            else
            {
                if (taskIsExists)
                    folder.DeleteTask("Netch Startup", 0);
            }
        }

        public static void ChangeControlForeColor(Component component, Color color)
        {
            switch (component)
            {
                case TextBox _:
                case ComboBox _:
                    if (((Control) component).ForeColor != color) ((Control) component).ForeColor = color;
                    break;
            }
        }
    }
}