using DnsClient;
using MaxMind.GeoIP2;
using Microsoft.Win32.TaskScheduler;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Task = System.Threading.Tasks.Task;

namespace Netch.Utils;

public static class Utils
{
    private static readonly string _tag = "Utils";
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
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await client.ConnectAsync(ip, port, timeoutCts.Token);
            stopwatch.Stop();
            return Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            stopwatch.Stop();
            return timeout;
        }
        catch (SocketException)
        {
            stopwatch.Stop();
            return timeout;
        }
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

            TextRenderer.DrawText(e.Graphics, cbx.Items[e.Index].ToString(), cbx.Font, e.Bounds, (e.State & DrawItemState.Selected) == DrawItemState.Selected ? SystemColors.HighlightText : cbx.ForeColor, TextFormatFlags.HorizontalCenter);
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
        if (Uri.TryCreate(str, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
            return uri.Host.Trim('[', ']');

        str = str.Trim();
        if (str.StartsWith('['))
        {
            var endBracket = str.IndexOf(']');
            if (endBracket > 1)
                return str[1..endBracket];
        }

        var startIndex = str.LastIndexOf('/');
        if (startIndex != -1)
            str = str[(startIndex + 1)..];

        return str.Count(c => c == ':') == 1 ? str[..str.IndexOf(':')] : str;
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

    /// <summary>
    /// Comma-separated string
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static List<string>? String2List(string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }

        try
        {
            str = str.Replace(Environment.NewLine, string.Empty);
            return new List<string>(str.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    /// <summary>
    /// Comma-separated string, sorted and then converted to List
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static List<string>? String2ListSorted(string str)
    {
        var lst = String2List(str);
        lst?.Sort();
        return lst;
    }

    public static bool ToBool(object obj)
    {
        try
        {
            return Convert.ToBoolean(obj);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsIpv6(string ip)
    {
        if (IPAddress.TryParse(ip, out var address))
        {
            return address.AddressFamily switch
            {
                AddressFamily.InterNetwork => false,
                AddressFamily.InterNetworkV6 => true,
                _ => false,
            };
        }

        return false;
    }

    /// <summary>
    /// Parse a possibly non-standard URL into scheme, domain, port, and path.
    /// If parsing fails, the entire input is returned as domain, and others are empty or zero.
    /// </summary>
    /// <param name="url">Input URL or string</param>
    /// <returns>(domain, scheme, port, path)</returns>
    public static (string domain, string scheme, int port, string path) ParseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return ("", "", 0, "");
        }

        // 1. First, try to parse using the standard Uri class.
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
        {
            var scheme = uri.Scheme;
            var domain = uri.Host;
            var port = uri.IsDefaultPort ? 0 : uri.Port;
            var path = uri.PathAndQuery;
            return (domain, scheme, port, path);
        }

        // 2. Try to handle more general cases with a regular expression, including non-standard schemes.
        // This regex captures the scheme (optional), authority (host+port), and path (optional).
        var match = Regex.Match(url, @"^(?:([a-zA-Z][a-zA-Z0-9+.-]*):/{2,})?([^/?#]+)([^?#]*)?.*$");

        if (match.Success)
        {
            var scheme = match.Groups[1].Value;
            var authority = match.Groups[2].Value;
            var path = match.Groups[3].Value;

            // Remove userinfo from the authority part.
            var atIndex = authority.LastIndexOf('@');
            if (atIndex > 0)
            {
                authority = authority.Substring(atIndex + 1);
            }

            var (domain, port) = ParseAuthority(authority);

            // If the parsed domain is empty, it means the authority part is malformed, so trigger the fallback.
            if (!string.IsNullOrEmpty(domain))
            {
                return (domain, scheme, port, path);
            }
        }

        // 3. If all of the above fails, execute the final fallback strategy.
        return (url, "", 0, "");
    }

    /// <summary>
    /// Helper function to parse domain and port from the authority part, with correct handling for IPv6.
    /// </summary>
    private static (string domain, int port) ParseAuthority(string authority)
    {
        if (string.IsNullOrEmpty(authority))
        {
            return ("", 0);
        }

        var port = 0;
        var domain = authority;

        // Handle IPv6 addresses, e.g., "[2001:db8::1]:443"
        if (authority.StartsWith('[') && authority.Contains(']'))
        {
            var closingBracketIndex = authority.LastIndexOf(']');
            if (closingBracketIndex < authority.Length - 1 && authority[closingBracketIndex + 1] == ':')
            {
                // Port exists
                var portStr = authority.Substring(closingBracketIndex + 2);
                if (int.TryParse(portStr, out var portNum))
                {
                    port = portNum;
                }
                domain = authority.Substring(0, closingBracketIndex + 1);
            }
            else
            {
                // No port
                domain = authority;
            }
        }
        else // Handle IPv4 or domain names
        {
            var lastColonIndex = authority.LastIndexOf(':');
            // Ensure there are digits after the colon and that this colon is not part of an IPv6 address.
            if (lastColonIndex > 0 && lastColonIndex < authority.Length - 1 && authority.Substring(lastColonIndex + 1).All(char.IsDigit))
            {
                var portStr = authority.Substring(lastColonIndex + 1);
                if (int.TryParse(portStr, out var portNum))
                {
                    port = portNum;
                    domain = authority.Substring(0, lastColonIndex);
                }
            }
        }

        return (domain, port);
    }

    public static Uri? TryUri(string url)
    {
        try
        {
            return new Uri(url);
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    public static string UrlDecode(string url)
    {
        return Uri.UnescapeDataString(url);
    }

    public static NameValueCollection ParseQueryString(string query)
    {
        var result = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
        if (query.IsNullOrEmpty())
        {
            return result;
        }

        var parts = query[1..].Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var keyValue = part.Split('=');
            if (keyValue.Length != 2)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(keyValue.First());
            var val = Uri.UnescapeDataString(keyValue.Last());

            if (result[key] is null)
            {
                result.Add(key, val);
            }
        }

        return result;
    }

    public static string UrlEncode(string url)
    {
        return Uri.EscapeDataString(url);
    }

    /// <summary>
    /// GUID
    /// </summary>
    /// <returns></returns>
    public static string GetGuid(bool full = true)
    {
        try
        {
            if (full)
            {
                return Guid.NewGuid().ToString("D");
            }
            else
            {
                return BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0).ToString();
            }
        }
        catch (Exception ex)
        {
            Log.Error(_tag, ex);
        }

        return string.Empty;
    }

    public static string GetTempPath(string filename = "")
    {
        var tempPath = Path.Combine(StartupPath(), "guiTemps");
        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }

        if (filename.IsNullOrEmpty())
        {
            return tempPath;
        }
        else
        {
            return Path.Combine(tempPath, filename);
        }
    }

    public static string StartupPath()
    {
        if (Environment.GetEnvironmentVariable(Constants.LocalAppData) == "1")
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Netch");
        }

        return GetBaseDirectory();
    }

    public static string GetBaseDirectory(string fileName = "")
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
    }

    /// <summary>
    /// Base64 Encode
    /// </summary>
    /// <param name="plainText"></param>
    /// <param name="removePadding"></param>
    /// <returns></returns>
    public static string Base64Encode(string plainText, bool removePadding = false)
    {
        try
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            var base64 = Convert.ToBase64String(plainTextBytes);
            if (removePadding)
            {
                base64 = base64.TrimEnd('=');
            }
            return base64;
        }
        catch (Exception ex)
        {
            Log.Error(_tag, ex);
        }

        return string.Empty;
    }

    /// <summary>
    /// Base64 Decode
    /// </summary>
    /// <param name="plainText"></param>
    /// <returns></returns>
    public static string Base64Decode(string? plainText)
    {
        try
        {
            if (plainText.IsNullOrEmpty())
            {
                return string.Empty;
            }

            plainText = plainText.Trim()
                .Replace(Environment.NewLine, "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace('_', '/')
                .Replace('-', '+')
                .Replace(" ", "");

            if (plainText.Length % 4 > 0)
            {
                plainText = plainText.PadRight(plainText.Length + 4 - (plainText.Length % 4), '=');
            }

            var data = Convert.FromBase64String(plainText);
            return Encoding.UTF8.GetString(data);
        }
        catch (Exception ex)
        {
            Log.Error(_tag, ex);
        }

        return string.Empty;
    }

    public static string ToString(object? obj)
    {
        try
        {
            return obj?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
