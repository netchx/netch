using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.Threading;
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
            if (!File.Exists(FileFullName) && !File.Exists(BackupFileFullName))
            {
                await SaveAsync();
                return;
            }

            await using var _ = await _lock.ReadLockAsync();

            if (File.Exists(FileFullName) && await LoadCoreAsync(FileFullName))
                return;

            Log.Information("Load backup configuration \"{FileName}\"", BackupFileFullName);
            if (!File.Exists(BackupFileFullName) || !await LoadCoreAsync(BackupFileFullName))
                throw new InvalidDataException("Neither settings.json nor its backup could be loaded. Both files were preserved.");

            string? preservedFile = null;
            if (File.Exists(FileFullName))
            {
                preservedFile = $"{FileFullName}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
                File.Move(FileFullName, preservedFile);
            }

            // Restore the readable backup before a future save rotates it.
            File.Copy(BackupFileFullName, FileFullName);
            MessageBox.Show(
                preservedFile == null
                    ? "The main configuration was missing. A valid backup has been restored."
                    : $"The main configuration was unreadable and has been preserved as:\n{preservedFile}\n\nA valid backup has been restored.",
                "Netch configuration recovered", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception e)
        {
            Log.Error(e, "Load configuration failed");
            MessageBox.Show(
                $"Netch cannot load its configuration or backup. The original files have been preserved; Netch will close to prevent overwriting them.\n\n{e.Message}",
                "Netch configuration error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(-1);
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

    private static void CheckSetting(Setting settings)
    {
        settings.Profiles.RemoveAll(p => p.ServerRemark == string.Empty || p.ModeRemark == string.Empty);

        if (settings.Profiles.Any(p => settings.Profiles.Any(p1 => p1 != p && p1.Index == p.Index)))
            for (var i = 0; i < settings.Profiles.Count; i++)
                settings.Profiles[i].Index = i;

        settings.AioDNS.ChinaDNS = DnsUtils.AppendPort(settings.AioDNS.ChinaDNS);
        settings.AioDNS.OtherDNS = DnsUtils.AppendPort(settings.AioDNS.OtherDNS);
    }

    /// <summary>
    ///     保存配置
    /// </summary>
    public static async Task SaveAsync()
    {
        if (_lock.IsWriteLockHeld)
            return;

        try
        {
            await using var _ = await _lock.WriteLockAsync();
            Log.Verbose("Save Configuration");

            if (!Directory.Exists(DataDirectoryFullName))
                Directory.CreateDirectory(DataDirectoryFullName);

            var tempFile = Path.Combine(DataDirectoryFullName, FileFullName + ".tmp");
            await using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await JsonSerializer.SerializeAsync(fileStream, Global.Settings, JsonSerializerOptions);
            }

            await EnsureConfigFileExistsAsync();

            File.Replace(tempFile, FileFullName, BackupFileFullName);
        }
        catch (Exception e)
        {
            Log.Error(e, "Save Configuration error");
        }
    }

    private static async ValueTask EnsureConfigFileExistsAsync()
    {
        if (!File.Exists(FileFullName))
        {
            await using var fs = new FileStream(FileFullName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, true);
        }
    }
}
