using System.Collections.ObjectModel;
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

        var modes = scene == null ? Array.Empty<Mode>() : SceneService.FindModes(scene);
        var server = scene == null ? null : SceneService.FindServer(scene);
        Icon = SceneIcon.NormalizeKey(scene?.Icon);
        SceneName = scene?.Name ?? string.Empty;
        foreach (var mode in modes)
        {
            var option = Modes.FirstOrDefault(m => m.Mode == mode);
            if (option != null && !SelectedModes.Contains(option))
                SelectedModes.Add(option);
        }
        SelectedNode = Nodes.FirstOrDefault(n => n.Server == server);
        PinnedToTray = scene?.PinnedToTray ?? false;
        Hotkey = scene?.Hotkey ?? string.Empty;

        RefreshModeSelectionState();
    }

    public string Title => i18N.Translate(_scene == null ? "New scene" : "Edit scene");

    public IReadOnlyList<SceneIconOption> Icons { get; } = SceneIcon.Presets
        .Select(p => new SceneIconOption(p.Key, p.Glyph, i18N.Translate(p.LabelKey)))
        .ToArray();

    public List<ModeOption> Modes { get; }

    public ObservableCollection<ModeOption> SelectedModes { get; } = new();

    public List<NodeOption> Nodes { get; }

    public string ModePlaceholder => i18N.Translate(Modes.Count == 0 ? "No modes found" : "Select modes");

    public string NodePlaceholder => i18N.Translate(Nodes.Count == 0 ? "No nodes yet. Import nodes on the Nodes page first." : "Select a node");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IconGlyph))]
    public partial string Icon { get; set; }

    public string IconGlyph => SceneIcon.ToGlyph(Icon);

    [ObservableProperty]
    public partial string SceneName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCompositionWarning))]
    public partial string CompositionWarning { get; private set; } = string.Empty;

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

    public bool HasMode => SelectedModes.Count > 0;

    public bool NoMode => SelectedModes.Count == 0;

    public bool IsMultiMode => SelectedModes.Count > 1;

    public bool HasCompositionWarning => !string.IsNullOrEmpty(CompositionWarning);

    public bool HasNode => SelectedNode != null;

    public bool NoNode => !HasNode;

    public bool CanSave =>
        SelectedModes.Count > 0 &&
        SelectedNode != null &&
        ModeComposer.ValidateSelection(SelectedModes.Select(m => m.Mode).ToArray(), out _);

    public string SelectedModeTypesSummary =>
        Format.ModeTypesSummary(SelectedModes.Select(m => m.Mode).ToArray());

    public string ModeSummarySuffix => IsMultiMode ? $"· {SelectedModeTypesSummary}" : string.Empty;

    public string SelectedModesCountText => Ui.TF("Selected {0}", "已选 {0} 项", SelectedModes.Count);

    public string ModeButtonAutomationName => SelectedModes.Count switch
    {
        0 => ModePlaceholder,
        1 => $"{i18N.Translate("Select modes")} · {SelectedModes[0].Name}",
        _ => $"{i18N.Translate("Select modes")} · {SelectedModeTypesSummary}"
    };

    public string SelectedNodeGroupName => SelectedNode?.GroupName ?? string.Empty;

    public string SelectedNodeName => SelectedNode?.Name ?? string.Empty;

    public string SelectedNodeProtocol => SelectedNode?.Protocol ?? string.Empty;

    public string SelectedNodeDelayText => SelectedNode?.DelayText ?? string.Empty;

    public Visibility SelectedNodeFastDelay => SelectedNode?.FastDelay ?? Visibility.Collapsed;

    public Visibility SelectedNodeSlowDelay => SelectedNode?.SlowDelay ?? Visibility.Collapsed;

    public Visibility SelectedNodeTimeout => SelectedNode?.Timeout ?? Visibility.Collapsed;

    public string NodeButtonAutomationName => SelectedNode == null ? NodePlaceholder : SelectedNode.Label;

    /// <summary>An empty name falls back to the selected mode summary.</summary>
    public string NamePlaceholder => SelectedModes.Count switch
    {
        0 => string.Empty,
        1 => SelectedModes[0].Mode.i18NRemark,
        2 => $"{SelectedModes[0].Mode.i18NRemark} + {SelectedModes[1].Mode.i18NRemark}",
        _ => $"{SelectedModes[0].Mode.i18NRemark} + {SelectedModes[1].Mode.i18NRemark} +{SelectedModes.Count - 2}"
    };

    public bool ToggleMode(ModeOption option)
    {
        if (SelectedModes.Contains(option))
        {
            SelectedModes.Remove(option);
            RefreshModeSelectionState();
            return true;
        }

        var selectedRaw = SelectedModes.Select(o => o.Mode).ToArray();
        if (!ModeComposer.CanAddMode(selectedRaw, option.Mode, out _))
            return false;

        SelectedModes.Add(option);
        RefreshModeSelectionState();
        return true;
    }

    public void RemoveMode(ModeOption option)
    {
        if (SelectedModes.Remove(option))
            RefreshModeSelectionState();
    }

    public void ClearModes()
    {
        if (SelectedModes.Count == 0)
            return;

        SelectedModes.Clear();
        RefreshModeSelectionState();
    }

    private void RefreshModeSelectionState()
    {
        var selectedRaw = SelectedModes.Select(o => o.Mode).ToArray();
        var tunSelected = selectedRaw.Any(m => m.Type == ModeType.TunMode);
        foreach (var option in Modes)
        {
            var selectedIndex = SelectedModes.IndexOf(option);
            if (selectedIndex >= 0)
            {
                option.UpdateSelectionState(
                    isSelected: true,
                    isAvailable: true,
                    disabledReason: null,
                    conflictHint: null,
                    showTopDivider: selectedIndex > 0);
            }
            else
            {
                var canAdd = ModeComposer.CanAddMode(selectedRaw, option.Mode, out var reason);
                var conflictHint = canAdd
                    ? null
                    : tunSelected || option.Mode.Type == ModeType.TunMode
                        ? i18N.Translate("TUN conflict")
                        : i18N.Translate("Max 1");
                option.UpdateSelectionState(
                    isSelected: false,
                    isAvailable: canAdd,
                    disabledReason: canAdd ? null : reason,
                    conflictHint: conflictHint,
                    showTopDivider: false);
            }
        }

        if (selectedRaw.Length > 0 && ModeComposer.ValidateSelection(selectedRaw, out _))
        {
            var composition = ModeComposer.Compose(selectedRaw);
            CompositionWarning = string.Join(Environment.NewLine, composition.Warnings);
        }
        else
        {
            CompositionWarning = string.Empty;
        }

        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(HasMode));
        OnPropertyChanged(nameof(NoMode));
        OnPropertyChanged(nameof(IsMultiMode));
        OnPropertyChanged(nameof(SelectedModeTypesSummary));
        OnPropertyChanged(nameof(ModeSummarySuffix));
        OnPropertyChanged(nameof(SelectedModesCountText));
        OnPropertyChanged(nameof(ModeButtonAutomationName));
        OnPropertyChanged(nameof(NamePlaceholder));
    }

    public IReadOnlyList<ModeOptionGroup> GetGroupedModes(string? query = null)
    {
        var filter = query?.Trim() ?? string.Empty;
        var groups = new List<ModeOptionGroup>();
        var selectedMatching = SelectedModes.Where(m => m.Matches(filter)).ToArray();
        if (selectedMatching.Length > 0)
            groups.Add(new ModeOptionGroup("__selected__", i18N.Translate("Selected"), selectedMatching));

        groups.AddRange(Modes
            .Where(m => !m.IsSelected && m.Matches(filter))
            .GroupBy(m => m.GroupKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => Format.ModeGroupOrder(g.Key))
            .ThenBy(g => Format.ModeGroupName(g.Key), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ModeOptionGroup(g.Key, Format.ModeGroupName(g.Key), g)));

        return groups;
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
        var modeIds = SelectedModes.Select(m => SceneService.GetModeId(m.Mode)).ToArray();
        var serverId = SelectedNode!.Server.Id;
        var pinned = PinnedToTray;
        var hotkey = Hotkey.Length == 0 ? null : Hotkey;
        return scene =>
        {
            scene.Icon = icon;
            scene.Name = name;
            scene.SetModeIds(modeIds);
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

public sealed partial class ModeOption : ObservableObject
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

    public bool IsProcess => Mode.Type == ModeType.ProcessMode;

    public bool IsWebProxy => Mode.Type == ModeType.WebProxyMode;

    public bool IsTun => Mode.Type == ModeType.TunMode;

    public bool IsShare => Mode.Type == ModeType.ShareMode;

    public string FullTooltip => $"{GroupName} · {Name}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckVisibility), nameof(AutomationLabel))]
    public partial bool IsSelected { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowOpacity), nameof(HasConflictHint), nameof(AutomationLabel))]
    public partial bool IsAvailable { get; private set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AutomationLabel))]
    public partial string? DisabledReason { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConflictHint))]
    public partial string? ConflictHint { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TopDividerVisibility))]
    public partial bool ShowTopDivider { get; private set; }

    public Visibility CheckVisibility => IsSelected ? Visibility.Visible : Visibility.Collapsed;

    public double RowOpacity => IsAvailable ? 1.0 : 0.45;

    public bool HasConflictHint => !IsAvailable && !string.IsNullOrEmpty(ConflictHint);

    public Visibility TopDividerVisibility => ShowTopDivider ? Visibility.Visible : Visibility.Collapsed;

    public string AutomationLabel => IsSelected
        ? $"{Name} · {TypeName} · {i18N.Translate("Selected")}"
        : !IsAvailable && !string.IsNullOrEmpty(DisabledReason)
            ? $"{Name} · {TypeName} · {DisabledReason}"
            : $"{Name} · {TypeName}";

    internal void UpdateSelectionState(bool isSelected, bool isAvailable, string? disabledReason, string? conflictHint, bool showTopDivider)
    {
        IsSelected = isSelected;
        IsAvailable = isAvailable;
        DisabledReason = disabledReason;
        ConflictHint = conflictHint;
        ShowTopDivider = showTopDivider;
    }

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
