using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.Threading;
using Netch.Enums;
using Netch.JsonConverter;
using Netch.Models;

namespace Netch.Utils;

public static class Configuration
{
    /// <summary>
    ///     数据目录
    /// </summary>
    public static string DataDirectoryFullName => Path.Combine(Global.NetchDir, "data");

    public static string FileFullName => Path.Combine(DataDirectoryFullName, FileName);

    private static string BackupFileFullName => Path.Combine(DataDirectoryFullName, BackupFileName);

    private const string FileName = "settings.json";

    private const string BackupFileName = "settings.json.bak";

    private static readonly AsyncReaderWriterLock _lock = new(null);

    private static readonly JsonSerializerOptions JsonSerializerOptions = Global.NewCustomJsonSerializerOptions();

    static Configuration()
    {
        JsonSerializerOptions.Converters.Add(new ServerConverterWithTypeDiscriminator());
        JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(FileFullName))
            {
                await SaveAsync();
                return;
            }

            await using var _ = await _lock.ReadLockAsync();

            if (await LoadCoreAsync(FileFullName))
                return;

            Log.Information("Load backup configuration \"{FileName}\"", BackupFileFullName);
            if (await LoadCoreAsync(BackupFileFullName))
                return;

            var preservedFiles = new[] { FileFullName, BackupFileFullName }
                .Select(PreserveCorruptFile)
                .Where(path => path != null)
                .ToArray();
            var preservedMessage = preservedFiles.Length == 0
                ? string.Empty
                : $"\nPreserved files:\n{string.Join("\n", preservedFiles)}";
            throw new InvalidDataException($"Both configuration files are invalid.{preservedMessage}");
        }
        catch (Exception e)
        {
            Log.Error(e, "Load configuration failed");
            // Native message box: the core cannot depend on a UI framework, and no window exists yet.
            Windows.Win32.PInvoke.MessageBox(Windows.Win32.Foundation.HWND.Null,
                $"Netch could not load its configuration. The original files were preserved when possible.\n\n{e.Message}",
                "Netch configuration error",
                Windows.Win32.UI.WindowsAndMessaging.MESSAGEBOX_STYLE.MB_OK | Windows.Win32.UI.WindowsAndMessaging.MESSAGEBOX_STYLE.MB_ICONERROR);
            Environment.Exit(-1);
        }
    }

    private static string? PreserveCorruptFile(string filename)
    {
        if (!File.Exists(filename))
            return null;

        var preservedName = $"{filename}.corrupt.{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        try
        {
            File.Move(filename, preservedName);
            return preservedName;
        }
        catch (Exception e)
        {
            Log.Error(e, "Preserve corrupt configuration file \"{FileName}\" failed", filename);
            return null;
        }
    }

    private static async ValueTask<bool> LoadCoreAsync(string filename)
    {
        try
        {
            Setting settings;

            await using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                settings = (await JsonSerializer.DeserializeAsync<Setting>(fs, JsonSerializerOptions))!;
            }

            CheckSetting(settings);
            Global.Settings = settings;
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "Load configuration file \"{FileName}\" error ", filename);
            return false;
        }
    }

    internal static void CheckSetting(Setting settings)
    {
        settings.Profiles.RemoveAll(p => p.ServerRemark == string.Empty || p.ModeRemark == string.Empty);

        if (settings.Subscription != null)
        {
            foreach (var sub in settings.Subscription)
                sub.Remark = (sub.Remark ?? string.Empty).Trim();
        }

        var subscriptionRemarks = (settings.Subscription ?? [])
            .Select(sub => sub.Remark)
            .Where(remark => remark.Length > 0 && !NodeGroupHelper.IsManualGroup(remark))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (settings.Server != null)
        {
            foreach (var server in settings.Server)
            {
                server.Group = NodeGroupHelper.NormalizeGroup(server.Group);
                if (server.IsSub && (NodeGroupHelper.IsManualGroup(server.Group) || !subscriptionRemarks.Contains(server.Group)))
                    server.IsSub = false;
            }
        }

        if (settings.Scenes != null)
        {
            foreach (var scene in settings.Scenes)
                scene.Icon = SceneIcon.NormalizeKey(scene.Icon);
        }

        CustomCoreHelper.NormalizeAll(settings);

        if (settings.Profiles.Any(p => settings.Profiles.Any(p1 => p1 != p && p1.Index == p.Index)))
            for (var i = 0; i < settings.Profiles.Count; i++)
                settings.Profiles[i].Index = i;

        // The original defaults used TCP DNS for both split-DNS upstreams.
        // Migrate only those untouched defaults; explicitly customized DNS
        // transports remain unchanged.
        var legacyChinaDns = $"tcp://{Constants.DefaultCNPrimaryDNS}:53";
        var legacyOtherDns = $"tcp://{Constants.DefaultPrimaryDNS}:53";
        if (string.Equals(settings.AioDNS.ChinaDNS, legacyChinaDns, StringComparison.OrdinalIgnoreCase))
            settings.AioDNS.ChinaDNS = $"udp://{Constants.DefaultCNPrimaryDNS}:53";
        if (string.Equals(settings.AioDNS.OtherDNS, legacyOtherDns, StringComparison.OrdinalIgnoreCase))
            settings.AioDNS.OtherDNS = $"udp://{Constants.DefaultPrimaryDNS}:53";

        settings.AioDNS.ChinaDNS = DnsUtils.AppendPort(settings.AioDNS.ChinaDNS);
        settings.AioDNS.OtherDNS = DnsUtils.AppendPort(settings.AioDNS.OtherDNS);

        settings.WebProxy ??= new();
        if (settings.WebProxy.PacPort == 0)
            settings.WebProxy.PacPort = 2803;
        if (string.IsNullOrWhiteSpace(settings.WebProxy.DefaultBypass))
            settings.WebProxy.DefaultBypass = WebProxyConfig.DefaultBypassValue;

        if (string.Equals(settings.ModeRepository, "netchx/netch", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(settings.ModeBranch, "main", StringComparison.Ordinal) &&
            string.Equals(settings.ModeRepositoryPath, "Storage/mode", StringComparison.Ordinal))
        {
            settings.ModeBranch = "v2";
        }

        NormalizeDelaySettings(settings);
    }

    public static void NormalizeDelaySettings(Setting settings)
    {
        if (settings.DelayTestConfigVersion is null or < 1)
        {
            if (settings.DetectionTick == 10)
                settings.DetectionTick = 0;

            if (!settings.ServerTCPing)
                settings.DelayTestMode = DelayTestMode.ICMPing;

            settings.DelayTestConfigVersion = 1;
        }

        if (!Enum.IsDefined(settings.DelayTestMode))
            settings.DelayTestMode = DelayTestMode.Hybrid;

        if (settings.DetectionTick < 0)
            settings.DetectionTick = 0;
        else if (settings.DetectionTick > 0)
            settings.DetectionTick = Math.Clamp(settings.DetectionTick, 10, 86400);

        settings.DelayTestTimeout = Math.Clamp(settings.DelayTestTimeout, Constants.MinDelayTestTimeout, Constants.MaxDelayTestTimeout);

        if (string.IsNullOrWhiteSpace(settings.DelayTestUrl) ||
            !Uri.TryCreate(settings.DelayTestUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            settings.DelayTestUrl = Constants.DefaultDelayTestUrl;
        }
        else
        {
            settings.DelayTestUrl = settings.DelayTestUrl.Trim();
        }
    }

    /// <summary>
    ///     保存配置
    /// </summary>
    public static Task SaveAsync() => SaveAsync(throwOnError: false);

    public static Task SaveAsync(bool throwOnError) => SaveAsync(Global.Settings, throwOnError);

    public static async Task SaveAsync(Setting settings, bool throwOnError)
    {
        if (_lock.IsWriteLockHeld)
            return;

        try
        {
            await using var _ = await _lock.WriteLockAsync();
            await WriteSettingsAsync(settings);
        }
        catch (Exception e)
        {
            Log.Error(e, "Save Configuration error");
            if (throwOnError)
                throw;
        }
    }

    public static async Task ApplyChangeAsync(Action apply, Action restore)
    {
        await using var _ = await _lock.WriteLockAsync();
        try
        {
            apply();
            await WriteSettingsAsync(Global.Settings);
        }
        catch
        {
            try { restore(); }
            catch (Exception e) { Log.Warning(e, "Could not restore settings"); }
            throw;
        }
    }

    private static async Task WriteSettingsAsync(Setting settings)
    {
        Log.Verbose("Save Configuration");
        Directory.CreateDirectory(DataDirectoryFullName);
        var tempFile = FileFullName + ".tmp";
        await using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            await JsonSerializer.SerializeAsync(fileStream, settings, JsonSerializerOptions);
        }

        await EnsureConfigFileExistsAsync();
        File.Replace(tempFile, FileFullName, BackupFileFullName);
    }

    private static async ValueTask EnsureConfigFileExistsAsync()
    {
        if (!File.Exists(FileFullName))
        {
            await using var fs = new FileStream(FileFullName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, true);
        }
    }
}
