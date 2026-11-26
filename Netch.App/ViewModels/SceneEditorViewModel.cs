using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Netch.App.Helpers;
using Netch.App.Pages;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
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
        Icon = SceneIcon.NormalizeKey(scene?.Icon);
        SceneName = scene?.Name ?? string.Empty;
        SelectedMode = Modes.FirstOrDefault(m => m.Mode == mode);
        SelectedNode = Nodes.FirstOrDefault(n => n.Server == server);
        PinnedToTray = scene?.PinnedToTray ?? false;
        Hotkey = scene?.Hotkey ?? string.Empty;
    }

    public string Title => i18N.Translate(_scene == null ? "New scene" : "Edit scene");

    public IReadOnlyList<SceneIconOption> Icons { get; } = SceneIcon.Presets
        .Select(p => new SceneIconOption(p.Key, p.Glyph, i18N.Translate(p.LabelKey)))
        .ToArray();

    public List<ModeOption> Modes { get; }

    public List<NodeOption> Nodes { get; }

    public string ModePlaceholder => i18N.Translate(Modes.Count == 0 ? "No modes found" : "Select a mode");

    public string NodePlaceholder => i18N.Translate(Nodes.Count == 0 ? "No nodes yet. Import nodes on the Nodes page first." : "Select a node");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IconGlyph))]
    public partial string Icon { get; set; }

    public string IconGlyph => SceneIcon.ToGlyph(Icon);

    [ObservableProperty]
    public partial string SceneName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(CanSave),
        nameof(HasMode),
        nameof(NoMode),
        nameof(NamePlaceholder),
        nameof(SelectedModeGroupName),
        nameof(SelectedModeName),
        nameof(SelectedModeType),
        nameof(ModeButtonAutomationName))]
    public partial ModeOption? SelectedMode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(CanSave),
        nameof(HasNode),
        nameof(NoNode),
        nameof(SelectedNodeGroupName),
        nameof(SelectedNodeName),
        nameof(SelectedNodeProtocol),
        nameof(SelectedNodeDelayText),
        nameof(SelectedNodeFastDelay),
        nameof(SelectedNodeSlowDelay),
        nameof(SelectedNodeTimeout),
        nameof(NodeButtonAutomationName))]
    public partial NodeOption? SelectedNode { get; set; }

    [ObservableProperty]
    public partial bool PinnedToTray { get; set; }

    /// <summary>Text form such as "Ctrl+Alt+1", or empty for none.</summary>
    [ObservableProperty]
    public partial string Hotkey { get; set; }

    public bool HasMode => SelectedMode != null;

    public bool NoMode => !HasMode;

    public bool HasNode => SelectedNode != null;

    public bool NoNode => !HasNode;

    public bool CanSave => SelectedMode != null && SelectedNode != null;

    public string SelectedModeGroupName => SelectedMode?.GroupName ?? string.Empty;

    public string SelectedModeName => SelectedMode?.Name ?? string.Empty;

    public string SelectedModeType => SelectedMode?.TypeName ?? string.Empty;

    public string ModeButtonAutomationName => SelectedMode == null ? ModePlaceholder : $"{SelectedMode.GroupName} · {SelectedMode.Name}";

    public string SelectedNodeGroupName => SelectedNode?.GroupName ?? string.Empty;

    public string SelectedNodeName => SelectedNode?.Name ?? string.Empty;

    public string SelectedNodeProtocol => SelectedNode?.Protocol ?? string.Empty;

    public string SelectedNodeDelayText => SelectedNode?.DelayText ?? string.Empty;

    public Visibility SelectedNodeFastDelay => SelectedNode?.FastDelay ?? Visibility.Collapsed;

    public Visibility SelectedNodeSlowDelay => SelectedNode?.SlowDelay ?? Visibility.Collapsed;

    public Visibility SelectedNodeTimeout => SelectedNode?.Timeout ?? Visibility.Collapsed;

    public string NodeButtonAutomationName => SelectedNode == null ? NodePlaceholder : SelectedNode.Label;

    /// <summary>An empty name falls back to the mode name.</summary>
    public string NamePlaceholder => SelectedMode?.Mode.i18NRemark ?? string.Empty;

    public IReadOnlyList<ModeOptionGroup> GetGroupedModes(string? query = null)
    {
        var filter = query?.Trim() ?? string.Empty;
        return Modes
            .Where(m => m.Matches(filter))
            .GroupBy(m => m.GroupKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => Format.ModeGroupOrder(g.Key))
            .ThenBy(g => Format.ModeGroupName(g.Key), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ModeOptionGroup(g.Key, Format.ModeGroupName(g.Key), g))
            .ToArray();
    }

    public IReadOnlyList<NodeOptionGroup> GetGroupedNodes(string? query = null)
    {
        var filter = query?.Trim() ?? string.Empty;
        var filtered = Nodes.Where(n => n.Matches(filter)).ToArray();
        if (filtered.Length == 0)
            return [];

        var groups = new List<NodeOptionGroup>();
        var subGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sub in Global.Settings.Subscription)
        {
            var remark = NodeGroupHelper.NormalizeGroup(sub.Remark);
            if (NodeGroupHelper.IsManualGroup(remark) || !subGroups.Add(remark))
                continue;

            var items = filtered.Where(n => n.IsSubscription && NodeGroupHelper.GroupEquals(n.GroupKey, remark)).ToArray();
            if (items.Length > 0)
                groups.Add(new NodeOptionGroup(remark, remark, items));
        }

        foreach (var imported in filtered
                     .Where(n => !NodeGroupHelper.IsManualGroup(n.GroupKey) && (!n.IsSubscription || !subGroups.Contains(n.GroupKey)))
                     .GroupBy(n => n.GroupKey, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            groups.Add(new NodeOptionGroup(imported.Key, imported.Key, imported));
        }

        var manual = filtered.Where(n => NodeGroupHelper.IsManualGroup(n.GroupKey)).ToArray();
        if (manual.Length > 0)
            groups.Add(new NodeOptionGroup(Constants.DefaultGroup, Ui.T("Added manually", "手动添加"), manual));

        return groups;
    }

    /// <summary>Captures the proposed edits before the asynchronous save.</summary>
    public Action<Scene> CaptureEdits()
    {
        var icon = SceneIcon.NormalizeKey(Icon);
        var name = SceneName.IsNullOrWhiteSpace() ? NamePlaceholder : SceneName.Trim();
        var modeId = SceneService.GetModeId(SelectedMode!.Mode);
        var serverId = SelectedNode!.Server.Id;
        var pinned = PinnedToTray;
        var hotkey = Hotkey.Length == 0 ? null : Hotkey;
        return scene =>
        {
            scene.Icon = icon;
            scene.Name = name;
            scene.ModeId = modeId;
            scene.ServerId = serverId;
            scene.PinnedToTray = pinned;
            scene.Hotkey = hotkey;
        };
    }
}

public sealed class ModeOptionGroup : List<ModeOption>
{
    public ModeOptionGroup(string key, string name, IEnumerable<ModeOption> items) : base(items)
    {
        Key = key;
        Name = name;
    }

    public string Key { get; }
    public string Name { get; }
    public string CountText => Count.ToString();
    public override string ToString() => $"{Name} ({Count})";
}

public sealed class NodeOptionGroup : List<NodeOption>
{
    public NodeOptionGroup(string key, string name, IEnumerable<NodeOption> items) : base(items)
    {
        Key = key;
        Name = name;
    }

    public string Key { get; }
    public string Name { get; }
    public string CountText => Count.ToString();
    public override string ToString() => $"{Name} ({Count})";
}

public sealed class ModeOption
{
    public ModeOption(Mode mode)
    {
        Mode = mode;
        GroupKey = ModesPage.GroupOf(mode);
        GroupName = Format.ModeGroupName(GroupKey);
        Name = mode.i18NRemark;
        TypeName = Format.ModeTypeName(mode.Type);
        var rules = mode switch
        {
            Redirector process => string.Join(' ', process.Handle.Concat(process.Bypass)),
            Models.Modes.WebProxyMode.WebProxyMode web => $"{web.Strategy} {web.PacPreset} {string.Join(' ', web.Handle.Concat(web.Bypass))}",
            _ => string.Empty
        };
        SearchIndex = $"{Name} {GroupName} {GroupKey} {TypeName} {rules}";
    }

    public Mode Mode { get; }

    public string GroupKey { get; }

    public string GroupName { get; }

    public string Name { get; }

    public string TypeName { get; }

    private string SearchIndex { get; }

    public bool Matches(string query) =>
        string.IsNullOrEmpty(query) || SearchIndex.Contains(query, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => $"{Format.ModeTypeLongName(Mode.Type)} · {Name}";
}

public sealed class NodeOption
{
    public NodeOption(Server server)
    {
        Server = server;
        Name = Format.NodeName(server);
        Protocol = ServerHelper.GetDisplayName(server.ConfigType);
        IsSubscription = server.IsSub && !NodeGroupHelper.IsManualGroup(server.Group);
        GroupKey = NodeGroupHelper.NormalizeGroup(server.Group);
        GroupName = NodeGroupHelper.IsManualGroup(GroupKey)
            ? Ui.T("Added manually", "手动添加")
            : GroupKey;
        Label = $"{Name} · {Protocol} · {GroupName}";
        SearchIndex = $"{Name} {server.Address} {Protocol} {GroupName}";
    }

    public Server Server { get; }

    public bool IsSubscription { get; }

    public string Name { get; }

    public string Protocol { get; }

    public string GroupKey { get; }

    public string GroupName { get; }

    public string Label { get; }

    public string DelayText => Format.Delay(Server.Delay);

    public string PendingDelayText => Server.Delay == DelayStatus.Testing ? "…" : "—";

    private string SearchIndex { get; }

    public Visibility FastDelay => Server.Delay is >= 0 and < 200 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SlowDelay => Server.Delay >= 200 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility Timeout => DelayStatus.IsFailure(Server.Delay) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PendingDelay => Server.Delay is DelayStatus.Untested or DelayStatus.Testing ? Visibility.Visible : Visibility.Collapsed;

    public bool Matches(string query) =>
        string.IsNullOrEmpty(query) || SearchIndex.Contains(query, StringComparison.OrdinalIgnoreCase);

    /// <summary>Used by the combo box text search.</summary>
    public override string ToString() => Name;
}

public sealed record SceneIconOption(string Key, string Glyph, string Label);
