using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Utils;

namespace Netch.Services;

public class ModeService
{
    public const string ManifestFileName = ".manifest.json";

    public static readonly ModeService Instance = new();

    public string ModeDirectoryFullName => Path.Combine(Global.NetchDir, "mode");

    public string GetRelativePath(string fullName)
    {
        var length = ModeDirectoryFullName.Length;
        if (!ModeDirectoryFullName.EndsWith("\\"))
            length++;

        return fullName.Substring(length);
    }

    public string GetFullPath(string relativeName)
    {
        return Path.Combine(ModeDirectoryFullName, relativeName);
    }

    public void Load(bool completeSceneDefaults = false)
    {
        Global.Modes.Clear();
        LoadCore(ModeDirectoryFullName);
        if (completeSceneDefaults)
            foreach (var mode in Global.Modes)
                ModeHelper.CompleteSceneDefaults(mode);
        // Sort() would refresh the combo box as well; populate it only once.
        SortCollection();
        AppEvents.RaiseModesChanged();
    }

    private void LoadCore(string modeDirectory)
    {
        foreach (var directory in Directory.GetDirectories(modeDirectory))
            LoadCore(directory);

        // skip Directory with a disabled file in
        if (File.Exists(Path.Combine(modeDirectory, Constants.DisableModeDirectoryFileName)))
            return;

        foreach (var file in Directory.GetFiles(modeDirectory))
        {
            if (Path.GetFileName(file).Equals(ManifestFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                Global.Modes.Add(ModeHelper.LoadMode(file));
            }
            catch (NotSupportedException)
            {
                // ignored
            }
            catch (Exception e)
            {
                Log.Warning(e, "Load mode \"{FileName}\" failed", file);
            }
        }
    }

    private static void SortCollection()
    {
        // TODO better sort need to discuss
        // TODO replace Mode Collection type
        Global.Modes.Sort((a, b) => string.Compare(a.i18NRemark, b.i18NRemark, StringComparison.Ordinal));
    }

    public void Add(Mode mode)
    {
        if (mode.FullName == null)
            throw new InvalidOperationException();

        mode.WriteFile();
        Global.Modes.Add(mode);
        Sort();
    }

    public void Sort()
    {
        SortCollection();
        AppEvents.RaiseModesChanged();
    }

    public static void Delete(Mode mode)
    {
        if (mode.FullName == null)
            throw new ArgumentException(nameof(mode.FullName));

        if (File.Exists(mode.FullName))
            File.Delete(mode.FullName);

        AppEvents.RaiseModeDeleted(mode);
        Global.Modes.Remove(mode);
    }

    public static ModeFeature GetModeFeatures(ModeType type) => type switch
    {
        ModeType.ProcessMode => ModeFeature.SupportIPv6 | ModeFeature.SupportSocks5Auth,
        ModeType.TunMode => ModeFeature.SupportIPv6 | ModeFeature.SupportSocks5Auth,
        ModeType.ShareMode => ModeFeature.SupportSocks5Auth,
        ModeType.WebProxyMode => ModeFeature.SupportIPv6 | ModeFeature.SupportSocks5Auth | ModeFeature.RequireLocalHttpProxy,
        _ => throw new MessageException("Unknown Mode Type")
    };

    public static IModeController GetModeControllerByType(ModeType type, out ushort? port, out string portName)
    {
        port = null;
        portName = string.Empty;
        switch (type)
        {
            case ModeType.ProcessMode:
                return new NFController();
            case ModeType.TunMode:
                return new TUNController();
            case ModeType.ShareMode:
                return new PcapController();
            case ModeType.WebProxyMode:
                return new WebProxyController();
            default:
                Log.Error("Unknown Mode Type \"{Type}\"", (int)type);
                throw new MessageException("Unknown Mode Type");
        }
    }
}
