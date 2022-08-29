using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using MaxMind.GeoIP2;
using Microsoft.Win32.TaskScheduler;
using Task = System.Threading.Tasks.Task;

namespace Netch.Utils;

public static class Utils
{
    public static void Open(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = path,
                UseShellExecute = true
            });
        }
        catch (Exception e)
        {
            Log.Warning(e, "Open \"{Uri}\" failed", path);
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

    public static async Task<int> ICMPingAsync(IPAddress ip, int timeout = 1000)
    {
        var reply = await new Ping().SendPingAsync(ip, timeout);

        if (reply.Status == IPStatus.Success)
            return Convert.ToInt32(reply.RoundtripTime);

        return timeout;
    }

    public static async Task<string> GetCityCodeAsync(string address)
    {
        var i = address.IndexOf(':');
        if (i != -1)
            address = address[..i];

        string? country = null;
        try
        {
            var databaseReader = new DatabaseReader("bin\\GeoLite2-Country.mmdb");

            if (IPAddress.TryParse(address, out _))
            {
                country = databaseReader.Country(address).Country.IsoCode;
            }
            else
            {
                var dnsResult = await DnsUtils.LookupAsync(address);

                if (dnsResult != null)
                    country = databaseReader.Country(dnsResult).Country.IsoCode;
            }
        }
        catch
        {
            // ignored
        }

        country ??= "Unknown";

        return country;
    }

    public static async Task<string> Sha256CheckSumAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return "";

        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            return await Sha256ComputeCoreAsync(fileStream);
        }
        catch (Exception e)
        {
            Log.Warning(e, $"Compute file \"{filePath}\" sha256 failed");
            return "";
        }
    }

    private static async Task<string> Sha256ComputeCoreAsync(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }

    public static string GetFileVersion(string file)
    {
        if (File.Exists(file))
            return FileVersionInfo.GetVersionInfo(file).FileVersion ?? "";

        return "";
    }

    public static void DrawCenterComboBox(object sender, DrawItemEventArgs e)
    {
        if (sender is ComboBox cbx)
        {
            e.DrawBackground();

            if (e.Index < 0)
                return;

            TextRenderer.DrawText(e.Graphics,
                cbx.Items[e.Index].ToString(),
                cbx.Font,
                e.Bounds,
                (e.State & DrawItemState.Selected) == DrawItemState.Selected ? SystemColors.HighlightText : cbx.ForeColor,
                TextFormatFlags.HorizontalCenter);
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
                    ComponentIterator(item, func);

                break;
            case ToolStripMenuItem toolStripMenuItem:
                // Iterator Menu strip sub item
                foreach (var item in toolStripMenuItem.DropDownItems.Cast<ToolStripItem>())
                    ComponentIterator(item, func);

                break;
            case MenuStrip menuStrip:
                // Menu Strip
                foreach (var item in menuStrip.Items.Cast<ToolStripItem>())
                    ComponentIterator(item, func);

                break;
            case ContextMenuStrip contextMenuStrip:
                foreach (var item in contextMenuStrip.Items.Cast<ToolStripItem>())
                    ComponentIterator(item, func);

                break;
            case Control control:
                foreach (var c in control.Controls.Cast<Control>())
                    ComponentIterator(c, func);

                if (control.ContextMenuStrip != null)
                    ComponentIterator(control.ContextMenuStrip, func);

                break;
        }
    }

    public static void RegisterNetchStartupItem()
    {
        const string TaskName = "Netch Startup";
        var folder = TaskService.Instance.GetFolder("\\");
        var taskIsExists = folder.Tasks.Any(task => task.Name == TaskName);

        if (Global.Settings.RunAtStartup)
        {
            if (taskIsExists)
                folder.DeleteTask(TaskName, false);

            var td = TaskService.Instance.NewTask();

            td.RegistrationInfo.Author = "Netch";
            td.RegistrationInfo.Description = "Netch run at startup.";
            td.Principal.RunLevel = TaskRunLevel.Highest;

            td.Triggers.Add(new LogonTrigger());
            td.Actions.Add(new ExecAction(Global.NetchExecutable));

            td.Settings.ExecutionTimeLimit = TimeSpan.Zero;
            td.Settings.DisallowStartIfOnBatteries = false;
            td.Settings.StopIfGoingOnBatteries = false;
            td.Settings.IdleSettings.StopOnIdleEnd = false;
            td.Settings.IdleSettings.RestartOnIdle = false;
            td.Settings.RunOnlyIfIdle = false;
            td.Settings.Compatibility = TaskCompatibility.V2_1;

            TaskService.Instance.RootFolder.RegisterTaskDefinition("Netch Startup", td);
        }
        else
        {
            if (taskIsExists)
                folder.DeleteTask(TaskName, false);
        }
    }

    public static void ChangeControlForeColor(Component component, Color color)
    {
        switch (component)
        {
            case TextBox _:
            case ComboBox _:
                if (((Control)component).ForeColor != color)
                    ((Control)component).ForeColor = color;

                break;
        }
    }

    public static int SubnetToCidr(string value)
    {
        var subnet = IPAddress.Parse(value);
        return SubnetToCidr(subnet);
    }

    public static int SubnetToCidr(IPAddress subnet)
    {
        return subnet.GetAddressBytes().Sum(b => Convert.ToString(b, 2).Count(c => c == '1'));
    }

    public static string GetHostFromUri(string str)
    {
        var startIndex = str.LastIndexOf('/');
        if (startIndex != -1)
            str = str[(startIndex + 1)..];

        var endIndex = str.IndexOf(':');
        return endIndex == -1 ? str : str[..endIndex];
    }

    public static void ActivateVisibleWindows()
    {
        var forms = Application.OpenForms.Cast<Form>().Where(f => f.Visible).ToList();
        if (!forms.Any())
        {
            Global.MainForm.Show();
            Global.MainForm.WindowState = FormWindowState.Normal;
            Global.MainForm.Activate();
        }
        else
        {
            foreach (var f in forms)
            {
                f.WindowState = FormWindowState.Normal;
                f.Activate();
            }
        }
    }
}