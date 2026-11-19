using Netch.Models;
using Netch.Models.Modes;
using Netch.Utils;

namespace Netch.Services;

public static class SceneService
{
    public static List<Scene> Scenes => Global.Settings.Scenes ?? throw new InvalidOperationException($"{nameof(SceneService)}.{nameof(Initialize)} has not run");

    /// <summary>
    ///     On the first WinUI start, turns the WinForms profiles into scenes. Needs servers and modes loaded.
    /// </summary>
    /// <returns>True when scenes were created and the configuration should be saved.</returns>
    public static bool Initialize()
    {
        var settings = Global.Settings;
        if (settings.Scenes != null)
            return false;

        var scenes = settings.Profiles.OrderBy(p => p.Index).Select(FromProfile).ToList();
        settings.Scenes = scenes;
        var hasSelection = settings.ServerComboBoxSelectedIndex >= 0 || settings.ModeComboBoxSelectedIndex >= 0;
        if (settings.Server.Count == 0 || Global.Modes.Count == 0 || (!settings.StartWhenOpened && !hasSelection))
            return true;

        var server = settings.Server[LegacySelectionIndex(settings.ServerComboBoxSelectedIndex, settings.Server.Count)];
        var mode = Global.Modes[LegacySelectionIndex(settings.ModeComboBoxSelectedIndex, Global.Modes.Count)];
        var modeId = GetModeId(mode);
        var selected = scenes.Find(scene => scene.ServerId == server.Id &&
            string.Equals(scene.ModeId, modeId, StringComparison.OrdinalIgnoreCase));
        if (selected == null)
        {
            selected = CreateScene(mode.i18NRemark.ValueOrDefault() ?? Path.GetFileNameWithoutExtension(modeId), mode, server);
            scenes.Add(selected);
        }

        settings.StartupSceneId = selected.Id;
        return true;
    }

    private static int LegacySelectionIndex(int index, int count) => index >= 0 && index < count ? index : 0;

    private static Scene FromProfile(Profile profile)
    {
        var server = Global.Settings.Server.FirstOrDefault(s => s.Remarks == profile.ServerRemark);
        var mode = Global.Modes.FirstOrDefault(m => m.Remark.Any(r => r.Value == profile.ModeRemark));

        return CreateScene(profile.ProfileName.IsNullOrWhiteSpace() ? profile.ModeRemark : profile.ProfileName, mode, server);
    }

    private static Scene CreateScene(string name, Mode? mode, Server? server) => new()
    {
        Name = name,
        Icon = mode?.Type switch
        {
            ModeType.TunMode => "🌐",
            ModeType.ShareMode => "🕹",
            _ => "🎮"
        },
        ModeId = mode == null ? string.Empty : GetModeId(mode),
        ServerId = server?.Id ?? string.Empty
    };

    public static string GetModeId(Mode mode) => ModeService.Instance.GetRelativePath(mode.FullName);

    public static Mode? FindMode(Scene scene) => scene.ModeId.Length == 0
        ? null
        : Global.Modes.FirstOrDefault(m => string.Equals(GetModeId(m), scene.ModeId, StringComparison.OrdinalIgnoreCase));

    public static Server? FindServer(Scene scene) => scene.ServerId.Length == 0
        ? null
        : Global.Settings.Server.FirstOrDefault(s => s.Id == scene.ServerId);
}
