using Netch.Models;
using Netch.Models.Modes;
using Netch.Utils;

namespace Netch.Services;

public static class SceneService
{
    public static List<Scene> Scenes => Global.Settings.Scenes ?? throw new InvalidOperationException($"{nameof(SceneService)}.{nameof(Initialize)} has not run");

    /// <summary>
    ///     On the first start, migrates legacy <see cref="Setting.Profiles" /> into scenes. Needs servers and modes loaded.
    /// </summary>
    /// <returns>True when scenes were created and the configuration should be saved.</returns>
    public static bool Initialize()
    {
        var settings = Global.Settings;
        if (settings.Scenes != null)
        {
            foreach (var scene in settings.Scenes)
            {
                scene.Icon = SceneIcon.NormalizeKey(scene.Icon);
                scene.NormalizeModes();
            }
            return false;
        }

        var scenes = settings.Profiles.OrderBy(p => p.Index).Select(FromProfile).ToList();
        settings.Scenes = scenes;
        var hasSelection = settings.ServerComboBoxSelectedIndex >= 0 || settings.ModeComboBoxSelectedIndex >= 0;
        if (settings.Server.Count == 0 || Global.Modes.Count == 0 || (!settings.StartWhenOpened && !hasSelection))
            return true;

        var server = settings.Server[LegacySelectionIndex(settings.ServerComboBoxSelectedIndex, settings.Server.Count)];
        var mode = Global.Modes[LegacySelectionIndex(settings.ModeComboBoxSelectedIndex, Global.Modes.Count)];
        var modeId = GetModeId(mode);
        var selected = scenes.Find(scene => scene.ServerId == server.Id &&
            scene.GetEffectiveModeIds().Count == 1 &&
            scene.UsesMode(modeId));
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

    private static Scene CreateScene(string name, Mode? mode, Server? server)
    {
        var scene = new Scene
        {
            Name = name,
            Icon = mode?.Type switch
            {
                ModeType.TunMode => SceneIcon.Globe,
                ModeType.WebProxyMode => SceneIcon.Globe,
                ModeType.ShareMode => SceneIcon.Console,
                _ => SceneIcon.Gamepad
            },
            ServerId = server?.Id ?? string.Empty
        };
        scene.SetModeIds(mode == null ? Array.Empty<string>() : [GetModeId(mode)]);
        return scene;
    }

    public static string GetModeId(Mode mode) => ModeService.Instance.GetRelativePath(mode.FullName);

    public static Mode? FindMode(Scene scene) => FindModes(scene).FirstOrDefault();

    public static IReadOnlyList<Mode> FindModes(Scene scene)
    {
        var ids = scene.GetEffectiveModeIds();
        if (ids.Count == 0)
            return Array.Empty<Mode>();

        var modes = new List<Mode>(ids.Count);
        foreach (var id in ids)
        {
            var found = Global.Modes.FirstOrDefault(m => string.Equals(GetModeId(m), id, StringComparison.OrdinalIgnoreCase));
            if (found != null)
                modes.Add(found);
        }

        return modes;
    }

    public static IReadOnlyList<string> FindMissingModeIds(Scene scene)
    {
        var ids = scene.GetEffectiveModeIds();
        if (ids.Count == 0)
            return Array.Empty<string>();

        var missing = new List<string>();
        foreach (var id in ids)
        {
            if (!Global.Modes.Any(m => string.Equals(GetModeId(m), id, StringComparison.OrdinalIgnoreCase)))
                missing.Add(id);
        }

        return missing;
    }

    public static bool HasValidModes(Scene scene) =>
        scene.GetEffectiveModeIds().Count > 0 && FindMissingModeIds(scene).Count == 0;

    public static Server? FindServer(Scene scene) => scene.ServerId.Length == 0
        ? null
        : Global.Settings.Server.FirstOrDefault(s => s.Id == scene.ServerId);
}
