using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Utils;

namespace Netch.Services;

public class ModeService
{
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

    public void Load()
    {
        Global.Modes.Clear();
        LoadCore(ModeDirectoryFullName);
        Sort();
        Global.MainForm.LoadModes();
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

        Global.Modes.Add(mode);
        Sort();

        mode.WriteFile();
    }

    public void Sort()
    {
        SortCollection();
        Global.MainForm.LoadModes();
    }

    public static void Delete(Mode mode)
    {
        if (mode.FullName == null)
            throw new ArgumentException(nameof(mode.FullName));

        Global.MainForm.ModeComboBox.Items.Remove(mode);
        Global.Modes.Remove(mode);

        if (File.Exists(mode.FullName))
            File.Delete(mode.FullName);
    }

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
            default:
                Log.Error("Unknown Mode Type \"{Type}\"", (int)type);
                throw new MessageException("Unknown Mode Type");
        }
    }
}