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
        if (Global.Settings.Scenes != null)
            return false;

        Global.Settings.Scenes = Global.Settings.Profiles.OrderBy(p => p.Index).Select(FromProfile).ToList();
        return true;
    }

    private static Scene FromProfile(Profile profile)
    {
        var server = Global.Settings.Server.FirstOrDefault(s => s.Remarks == profile.ServerRemark);
        var mode = Global.Modes.FirstOrDefault(m => m.Remark.Any(r => r.Value == profile.ModeRemark));

        return new Scene
        {
            Name = profile.ProfileName.IsNullOrWhiteSpace() ? profile.ModeRemark : profile.ProfileName,
            Icon = mode?.Type switch
            {
                ModeType.TunMode => "🌐",
                ModeType.ShareMode => "🕹",
                _ => "🎮"
            },
            ModeId = mode == null ? string.Empty : GetModeId(mode),
            ServerId = server?.Id ?? string.Empty
        };
    }

    public static string GetModeId(Mode mode) => ModeService.Instance.GetRelativePath(mode.FullName);

    public static Mode? FindMode(Scene scene) => scene.ModeId.Length == 0
        ? null
        : Global.Modes.FirstOrDefault(m => string.Equals(GetModeId(m), scene.ModeId, StringComparison.OrdinalIgnoreCase));

    public static Server? FindServer(Scene scene) => scene.ServerId.Length == 0
        ? null
        : Global.Settings.Server.FirstOrDefault(s => s.Id == scene.ServerId);
}
