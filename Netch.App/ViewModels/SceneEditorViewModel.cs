using CommunityToolkit.Mvvm.ComponentModel;
using Netch.App.Helpers;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Services;
using Netch.Utils;

namespace Netch.App.ViewModels;

public sealed partial class SceneEditorViewModel : ObservableObject
{
    private readonly Scene? _scene;

    /// <param name="scene">The scene to edit, or null to create one.</param>
    public SceneEditorViewModel(Scene? scene)
    {
        _scene = scene;
        Modes = Global.Modes.Select(m => new ModeOption(m)).ToList();
        Nodes = Global.Settings.Server.Select(s => new NodeOption(s)).ToList();

        var mode = scene == null ? null : SceneService.FindMode(scene);
        var server = scene == null ? null : SceneService.FindServer(scene);
        Icon = scene?.Icon ?? Icons[0];
        SceneName = scene?.Name ?? string.Empty;
        SelectedMode = Modes.FirstOrDefault(m => m.Mode == mode);
        SelectedNode = Nodes.FirstOrDefault(n => n.Server == server);
        PinnedToTray = scene?.PinnedToTray ?? false;
        Hotkey = scene?.Hotkey ?? string.Empty;
    }

    public string Title => i18N.Translate(_scene == null ? "New scene" : "Edit scene");

    public IReadOnlyList<string> Icons { get; } = ["🎮", "🌐", "🧭", "🕹", "🛠", "💬", "📺", "🎵", "💼", "🚀", "⚡", "🔒", "🏠", "🖥", "📱", "☁"];

    public List<ModeOption> Modes { get; }

    public List<NodeOption> Nodes { get; }

    public string ModePlaceholder => i18N.Translate(Modes.Count == 0 ? "No modes found" : "Select a mode");

    public string NodePlaceholder => i18N.Translate(Nodes.Count == 0 ? "No nodes yet. Import nodes on the Nodes page first." : "Select a node");

    [ObservableProperty]
    public partial string Icon { get; set; }

    [ObservableProperty]
    public partial string SceneName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave), nameof(HasMode), nameof(NamePlaceholder))]
    public partial ModeOption? SelectedMode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial NodeOption? SelectedNode { get; set; }

    [ObservableProperty]
    public partial bool PinnedToTray { get; set; }

    /// <summary>Text form such as "Ctrl+Alt+1", or empty for none.</summary>
    [ObservableProperty]
    public partial string Hotkey { get; set; }

    public bool HasMode => SelectedMode != null;

    public bool CanSave => SelectedMode != null && SelectedNode != null;

    /// <summary>An empty name falls back to the mode name.</summary>
    public string NamePlaceholder => SelectedMode?.Mode.i18NRemark ?? string.Empty;

    /// <summary>Writes the edits into the edited scene, or into a new one.</summary>
    public Scene Apply()
    {
        var scene = _scene ?? new Scene();
        scene.Icon = Icon;
        scene.Name = SceneName.IsNullOrWhiteSpace() ? NamePlaceholder : SceneName.Trim();
        scene.ModeId = SceneService.GetModeId(SelectedMode!.Mode);
        scene.ServerId = SelectedNode!.Server.Id;
        scene.PinnedToTray = PinnedToTray;
        scene.Hotkey = Hotkey.Length == 0 ? null : Hotkey;
        return scene;
    }
}

public sealed class ModeOption(Mode mode)
{
    public Mode Mode { get; } = mode;

    public override string ToString() => $"{Format.ModeTypeName(Mode.Type)} · {Mode.i18NRemark}";
}

public sealed class NodeOption(Server server)
{
    public Server Server { get; } = server;

    public string Label { get; } = $"{Format.NodeName(server)} · {server.ConfigType} · {server.Group}";

    public string DelayText { get; } = Format.Delay(server.Delay);

    public bool IsFast => Server.Delay is >= 0 and < 100;

    public bool IsSlow => Server.Delay is >= 100 and < 1000;

    public bool IsTimeout => Server.Delay is >= 1000 or < -1;

    /// <summary>Used by the combo box text search.</summary>
    public override string ToString() => Format.NodeName(Server);
}
