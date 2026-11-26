using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.Threading;
using Microsoft.Windows.Storage.Pickers;
using Netch.App.Helpers;
using Netch.App.Services;
using Netch.Models;
using Netch.Services;
using Netch.Utils;
using Windows.System;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

public sealed class ConnectionRowVm : INotifyPropertyChanged
{
    private string _processTitle = string.Empty;
    private string _pidSubtitle = string.Empty;
    private string _protocol = string.Empty;
    private string _endpoint = string.Empty;
    private string _ruleDisplay = string.Empty;
    private string _uploadRateDisplay = string.Empty;
    private string _downloadRateDisplay = string.Empty;
    private string _trafficDisplay = string.Empty;
    private string _durationDisplay = string.Empty;

    public ConnectionRowVm(ProcessConnectionSnapshot snapshot)
    {
        Snapshot = snapshot;
        UpdateFrom(snapshot);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProcessConnectionSnapshot Snapshot { get; private set; }
    public ulong Id { get; private set; }

    public string ProcessTitle
    {
        get => _processTitle;
        private set => SetField(ref _processTitle, value);
    }

    public string PidSubtitle
    {
        get => _pidSubtitle;
        private set => SetField(ref _pidSubtitle, value);
    }

    public string Protocol
    {
        get => _protocol;
        private set => SetField(ref _protocol, value);
    }

    public string Endpoint
    {
        get => _endpoint;
        private set => SetField(ref _endpoint, value);
    }

    public string RuleDisplay
    {
        get => _ruleDisplay;
        private set => SetField(ref _ruleDisplay, value);
    }

    public string UploadRateDisplay
    {
        get => _uploadRateDisplay;
        private set => SetField(ref _uploadRateDisplay, value);
    }

    public string DownloadRateDisplay
    {
        get => _downloadRateDisplay;
        private set => SetField(ref _downloadRateDisplay, value);
    }

    public string TrafficDisplay
    {
        get => _trafficDisplay;
        private set => SetField(ref _trafficDisplay, value);
    }

    public string DurationDisplay
    {
        get => _durationDisplay;
        private set => SetField(ref _durationDisplay, value);
    }

    public void UpdateFrom(ProcessConnectionSnapshot snapshot)
    {
        Snapshot = snapshot;
        Id = snapshot.Id;
        ProcessTitle = string.IsNullOrEmpty(snapshot.ProcessName) ? $"PID {snapshot.Pid}" : snapshot.ProcessName;
        PidSubtitle = snapshot.Pid == 0 ? string.Empty : $"PID {snapshot.Pid}";
        Protocol = snapshot.Protocol;
        Endpoint = snapshot.Endpoint;
        RuleDisplay = FormatRuleDisplay(snapshot);
        UploadRateDisplay = snapshot.IsActive ? FormatSingleSpeed(snapshot.UpSpeedBps) : "-";
        DownloadRateDisplay = snapshot.IsActive ? FormatSingleSpeed(snapshot.DownSpeedBps) : "-";
        TrafficDisplay = Format.Bytes(snapshot.UpBytes + snapshot.DownBytes);
        DurationDisplay = snapshot.IsActive ? Format.Elapsed(snapshot.Duration) : snapshot.State;
    }

    internal static string FormatRuleDisplay(ProcessConnectionSnapshot snapshot) => snapshot.IsDirect
        ? T("Direct", "直连")
        : string.IsNullOrEmpty(snapshot.MatchedRule)
            ? snapshot.RuleSource
            : string.IsNullOrEmpty(snapshot.RuleSource)
                ? snapshot.MatchedRule
                : $"{snapshot.MatchedRule} · {snapshot.RuleSource}";

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatSingleSpeed(ulong bps) =>
        bps == 0 ? "0 B/s" : $"{Format.Bytes(bps).Replace(" ", string.Empty, StringComparison.Ordinal)}/s";
}

public sealed class ConnectionLogRowVm : INotifyPropertyChanged
{
    private string _seqText;
    private string _timeText;
    private string _repeatText;
    private Visibility _repeatVisibility;

    public ConnectionLogRowVm(FilteredConnectionEventRow row)
    {
        Event = row.Event;
        Count = row.Count;
        _seqText = $"#{row.Event.Seq}";
        _timeText = row.Event.TimestampLocal.ToString("HH:mm:ss.fff");
        ActionText = row.Event.Action;
        Protocol = row.Event.Protocol;
        ProcessDisplay = string.IsNullOrEmpty(row.Event.ProcessName)
            ? $"pid:{row.Event.Pid}"
            : $"{row.Event.ProcessName} ({row.Event.Pid})";
        Endpoint = row.Event.Endpoint;
        RuleDisplay = string.IsNullOrEmpty(row.Event.MatchedRule)
            ? row.Event.RuleSource
            : string.IsNullOrEmpty(row.Event.RuleSource)
                ? row.Event.MatchedRule
                : $"{row.Event.MatchedRule} · {row.Event.RuleSource}";
        _repeatText = row.Count > 1 ? $"×{row.Count}" : string.Empty;
        _repeatVisibility = row.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProcessConnectionEvent Event { get; private set; }
    public int Count { get; private set; }

    public string SeqText
    {
        get => _seqText;
        private set => SetField(ref _seqText, value);
    }

    public string TimeText
    {
        get => _timeText;
        private set => SetField(ref _timeText, value);
    }

    public string ActionText { get; }
    public string Protocol { get; }
    public string ProcessDisplay { get; }
    public string Endpoint { get; }
    public string RuleDisplay { get; }

    public string RepeatText
    {
        get => _repeatText;
        private set => SetField(ref _repeatText, value);
    }

    public Visibility RepeatVisibility
    {
        get => _repeatVisibility;
        private set => SetField(ref _repeatVisibility, value);
    }

    public void MergeRepeat(ProcessConnectionEvent latest)
    {
        Event = latest;
        Count++;
        SeqText = $"#{latest.Seq}";
        TimeText = latest.TimestampLocal.ToString("HH:mm:ss.fff");
        RepeatText = $"×{Count}";
        RepeatVisibility = Visibility.Visible;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed partial class ConnectionsPage : Page
{
    private const int MaxVisibleLogRows = 5000;
    private static readonly Thickness CompactButtonPadding = new(8, 4, 8, 4);
    private static readonly Thickness TabButtonPadding = new(10, 4, 10, 4);

    private readonly ConnectionTrackerService _tracker = ConnectionTrackerService.Instance;
    private readonly ObservableCollection<ConnectionRowVm> _connRows = [];
    private readonly ObservableCollection<ConnectionLogRowVm> _logRows = [];

    private readonly Button _tabConnections;
    private readonly Button _tabLogs;
    private readonly TextBlock _telemetryText;
    private readonly ToggleSwitch _trackUnmatchedToggle;
    private readonly ToggleSwitch _writeLogToggle;
    private readonly InfoBar _feedbackBar;
    private readonly Border _hotRuleBanner;
    private readonly TextBlock _hotRuleSummary;
    private readonly StackPanel _hotRuleChips;
    private readonly StackPanel _hotRuleSaveHost;

    private enum ConnectionSortColumn
    {
        None,
        Process,
        Protocol,
        Target,
        Rule,
        Upload,
        Download,
        Traffic,
        Duration
    }

    // View 1 (Connections) controls
    private readonly Grid _connectionsPane = new() { RowSpacing = 8 };
    private readonly Button _connFilterActiveBtn;
    private readonly Button _connFilterClosedBtn;
    private readonly Button _connFilterAllBtn;
    private readonly ComboBox _connRouteBox;
    private readonly ComboBox _connProtocolBox;
    private readonly TextBox _connSearchBox;
    private readonly Dictionary<ConnectionSortColumn, (Button Button, string Label, TextBlock Text)> _connSortHeaders = new();
    private readonly Button _pauseBtn;
    private readonly Button _closeAllBtn;
    private readonly Button _clearClosedBtn;
    private readonly ListView _connList;
    private readonly TextBlock _connEmptyText;

    // View 2 (Connection Logs) controls
    private readonly Grid _logsPane = new() { RowSpacing = 8 };
    private readonly Button _logActionAllBtn;
    private readonly Button _logActionProxyBtn;
    private readonly Button _logActionBypassBtn;
    private readonly Button _logActionCloseBtn;
    private readonly Button _logActionErrorBtn;
    private readonly TextBox _logSearchBox;
    private readonly ToggleButton _logRegexToggle;
    private readonly ToggleButton _logDedupToggle;
    private readonly ToggleButton _logFollowToggle;
    private readonly ListView _logList;
    private readonly TextBlock _logEmptyText;

    // Right Inspector drawer (Scheme A + D)
    private readonly Border _inspectorCard;
    private readonly ScrollViewer _inspectorScroll;
    private readonly TextBlock _inspectorTitle;
    private readonly StackPanel _inspectorFields;
    private readonly StackPanel _inspectorActions;

    // Live-updated Inspector field references (avoids rebuilding DOM on 1s poll)
    private TextBlock? _inspectorStateVal;
    private TextBlock? _inspectorTrafficVal;
    private TextBlock? _inspectorDurationVal;
    private TextBlock? _inspectorCloseReasonVal;
    private StackPanel? _inspectorCloseReasonContainer;
    private Button? _inspectorCloseBtn;

    private int _activeView; // 0 = Connections, 1 = Connection Logs
    private int _connStateFilter; // 0 = Active, 1 = Closed, 2 = All
    private ConnectionSortColumn _connSortColumn = ConnectionSortColumn.None;
    private bool _connSortDescending = true;
    private string _logActionFilter = "ALL";
    private bool _syncingControls;
    private bool _suppressSelectionChanged;
    private bool _hasRegexError;
    private bool _showingEtwWarning;
    private bool _savingHotRules;
    private bool? _pauseBtnPausedState;
    private ProcessConnectionSnapshot? _selectedConn;
    private ProcessConnectionEvent? _selectedLog;

    public ConnectionsPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;

        _tabConnections = new Button
        {
            Style = Resource<Style>("NetchWizardTabActive"),
            Padding = TabButtonPadding,
            VerticalAlignment = VerticalAlignment.Center
        };
        _tabLogs = new Button
        {
            Style = Resource<Style>("NetchWizardTab"),
            Padding = TabButtonPadding,
            VerticalAlignment = VerticalAlignment.Center
        };
        _tabConnections.Click += (_, _) => SwitchView(0);
        _tabLogs.Click += (_, _) => SwitchView(1);

        _telemetryText = Caption(string.Empty);
        _telemetryText.FontFamily = new FontFamily("Consolas");
        _telemetryText.FontSize = 12;
        _telemetryText.MaxWidth = 230;
        _telemetryText.TextTrimming = TextTrimming.CharacterEllipsis;
        _telemetryText.VerticalAlignment = VerticalAlignment.Center;

        _trackUnmatchedToggle = CompactToggle(new ToggleSwitch());
        _trackUnmatchedToggle.IsOn = _tracker.TrackUnmatched;
        var unmatchedLabel = Caption(T("Unmatched", "未命中进程"));
        unmatchedLabel.VerticalAlignment = VerticalAlignment.Center;
        AutomationProperties.SetName(_trackUnmatchedToggle, T("Track unmatched processes", "监控未命中进程"));
        var unmatchedTip = T(
            "Monitor unmatched/direct process traffic via Windows ETW and log unmatched connections",
            "通过 Windows ETW 旁路监控本机未命中进程的实时流量，并在连接日志中记录未命中事件");
        ToolTipService.SetToolTip(_trackUnmatchedToggle, unmatchedTip);
        ToolTipService.SetToolTip(unmatchedLabel, unmatchedTip);

        _writeLogToggle = CompactToggle(new ToggleSwitch());
        _writeLogToggle.IsOn = Global.Settings.Redirector.LogConnectionsToFile;
        _writeLogToggle.Toggled += (_, _) =>
        {
            if (_syncingControls)
                return;
            _tracker.SetLogToFile(_writeLogToggle.IsOn);
            Configuration.SaveAsync().Forget();
        };
        var writeLogLabel = Caption(T("Save logs", "保存日志"));
        writeLogLabel.VerticalAlignment = VerticalAlignment.Center;
        AutomationProperties.SetName(_writeLogToggle, T("Save connection logs to file", "自动保存连接日志到文件"));
        var writeLogTip = T("Automatically append connection logs to logging/nf-connections.log", "自动写入 logging/nf-connections.log");
        ToolTipService.SetToolTip(_writeLogToggle, writeLogTip);
        ToolTipService.SetToolTip(writeLogLabel, writeLogTip);

        var headerMain = Row(Text(T("Connections", "连接"), 24, strong: true), _tabConnections, _tabLogs);
        headerMain.Spacing = 8;
        var headerRight = Row(
            Row(unmatchedLabel, _trackUnmatchedToggle),
            Row(writeLogLabel, _writeLogToggle));
        headerRight.Spacing = 12;
        var topBar = Split(headerMain, headerRight, 12);

        _feedbackBar = new InfoBar { IsOpen = false, IsClosable = true };

        // Pending Hot Rules banner
        _hotRuleSummary = Text(string.Empty, 13, strong: true);
        _hotRuleSummary.TextTrimming = TextTrimming.CharacterEllipsis;
        _hotRuleChips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var chipScroll = new ScrollViewer
        {
            Content = _hotRuleChips,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = 34
        };
        _hotRuleSaveHost = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        var hotRuleLeft = new StackPanel { Spacing = 6, MinWidth = 0, Children = { _hotRuleSummary, chipScroll } };
        _hotRuleBanner = new Border
        {
            Style = Resource<Style>("NetchWarningStrip"),
            Padding = new Thickness(12, 8, 12, 8),
            Visibility = Visibility.Collapsed,
            Child = Split(hotRuleLeft, _hotRuleSaveHost, 10)
        };

        // View 1: Connections controls & list (2-row toolbar for narrow widths)
        _connFilterActiveBtn = MakeCompactTabButton(T("Active", "活跃"), () => { _connStateFilter = 0; UpdateSubFilterTabs(); RefreshConnections(); });
        _connFilterClosedBtn = MakeCompactTabButton(T("Closed", "已关闭"), () => { _connStateFilter = 1; UpdateSubFilterTabs(); RefreshConnections(); });
        _connFilterAllBtn = MakeCompactTabButton(T("All", "全部"), () => { _connStateFilter = 2; UpdateSubFilterTabs(); RefreshConnections(); });

        _connRouteBox = Choice(T("Route", "流向"), [T("All routes", "全部流向"), T("Proxied", "仅代理"), T("Direct", "仅直连")], 0);
        _connRouteBox.MinWidth = 94;
        _connRouteBox.Visibility = _tracker.TrackUnmatched ? Visibility.Visible : Visibility.Collapsed;
        _connRouteBox.SelectionChanged += (_, _) =>
        {
            if (!_syncingControls)
                RefreshConnections();
        };
        _trackUnmatchedToggle.Toggled += (_, _) =>
        {
            if (_syncingControls)
                return;

            if (!_trackUnmatchedToggle.IsOn)
            {
                _syncingControls = true;
                try
                {
                    _connRouteBox.SelectedIndex = 0;
                }
                finally
                {
                    _syncingControls = false;
                }
            }

            _tracker.SetTrackUnmatched(_trackUnmatchedToggle.IsOn);
        };

        _connProtocolBox = Choice(T("Protocol", "协议"), [T("All", "全部"), "TCP", "UDP"], 0);
        _connProtocolBox.MinWidth = 84;
        _connProtocolBox.SelectionChanged += (_, _) =>
        {
            if (!_syncingControls)
                RefreshConnections();
        };

        _connSearchBox = Input(T("Filter process, IP, rule, PID", "筛选进程、IP、规则、PID"), search: true);
        _connSearchBox.TextChanged += (_, _) => RefreshConnections();

        _pauseBtn = Button(T("Pause", "暂停"), TogglePause, glyph: Icons.Pause);
        _pauseBtn.Padding = CompactButtonPadding;
        ToolTipService.SetToolTip(_pauseBtn, T("Pause/Resume live refresh", "暂停或恢复实时刷新"));

        _closeAllBtn = Button(T("Close all", "断开全部"), CloseAllActiveConnections, glyph: Icons.CloseWindow);
        _closeAllBtn.Padding = CompactButtonPadding;
        ToolTipService.SetToolTip(_closeAllBtn, T("Close all active connections", "断开全部活跃连接"));

        _clearClosedBtn = Button(T("Clear", "清空"), () => _tracker.ClearClosedConnections(), glyph: Icons.Delete);
        _clearClosedBtn.Padding = CompactButtonPadding;
        AutomationProperties.SetName(_clearClosedBtn, T("Clear closed connections", "清空已关闭连接"));
        ToolTipService.SetToolTip(_clearClosedBtn, T("Clear closed connections", "清空已关闭连接"));

        var connToolbarLeft = Row(_connFilterActiveBtn, _connFilterClosedBtn, _connFilterAllBtn);
        connToolbarLeft.Spacing = 4;
        var connToolbarRight = Row(_telemetryText, _pauseBtn, _closeAllBtn, _clearClosedBtn);
        connToolbarRight.Spacing = 6;
        var connFiltersRight = Row(_connRouteBox, _connProtocolBox);
        connFiltersRight.Spacing = 6;
        var connToolbarRow1 = Split(connToolbarLeft, connToolbarRight, 8);
        var connToolbarRow2 = Split(_connSearchBox, connFiltersRight, 8);
        var connToolbar = new StackPanel { Spacing = 6, Children = { connToolbarRow1, connToolbarRow2 } };

        _connList = new ListView
        {
            SelectionMode = ListViewSelectionMode.Single,
            SingleSelectionFollowsFocus = false,
            ShowsScrollingPlaceholders = false,
            ItemsSource = _connRows,
            ItemContainerStyle = Resource<Style>("NetchLogItem"),
            ItemContainerTransitions = new TransitionCollection(),
            Transitions = new TransitionCollection(),
            Padding = new Thickness(0, 0, ScrollBarGutter, 0)
        };
        _connList.ItemTemplate = BuildConnectionRowTemplate();
        _connList.SelectionChanged += (_, _) =>
        {
            if (_suppressSelectionChanged)
                return;
            if (_connList.SelectedItem is ConnectionRowVm vm)
            {
                _selectedConn = vm.Snapshot;
                _selectedLog = null;
                RenderInspector();
            }
        };
        _connList.ContainerContentChanging += (_, e) =>
        {
            if (e.ItemContainer is not ListViewItem item)
                return;

            if (e.InRecycleQueue)
            {
                item.Tag = null;
                return;
            }

            if (e.Item is ConnectionRowVm row)
                item.Tag = row;

            if (item.ContextFlyout == null)
            {
                var flyout = new MenuFlyout();
                flyout.Opening += (_, _) =>
                {
                    var currentRow = _connList.ItemFromContainer(item) as ConnectionRowVm ?? item.Tag as ConnectionRowVm;
                    if (currentRow != null)
                        PopulateConnectionMenu(flyout, currentRow.Snapshot);
                };
                item.ContextFlyout = flyout;
            }
        };

        _connEmptyText = Caption(string.Empty);
        _connEmptyText.HorizontalAlignment = HorizontalAlignment.Center;
        _connEmptyText.VerticalAlignment = VerticalAlignment.Center;

        var connHeaderStrip = BuildConnectionHeaderStrip();
        UpdateConnectionSortHeaders();
        var connTableSurface = new Grid
        {
            RowSpacing = 4,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            },
            Children = { connHeaderStrip, new Grid { Children = { _connList, _connEmptyText } } }
        };
        Grid.SetRow((FrameworkElement)connTableSurface.Children[1], 1);

        var connCard = Card(connTableSurface, 8);
        connCard.Padding = new Thickness(6, 8, 6, 4);
        _connectionsPane.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _connectionsPane.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _connectionsPane.Children.Add(connToolbar);
        Grid.SetRow(connCard, 1);
        _connectionsPane.Children.Add(connCard);

        // View 2: Dedicated Connection Logs (Scheme A + D, 2-row toolbar)
        _logActionAllBtn = MakeCompactTabButton(T("All", "全部"), () => SetLogActionFilter("ALL"));
        _logActionProxyBtn = MakeCompactTabButton(T("Proxy", "代理"), () => SetLogActionFilter("PROXY"));
        _logActionBypassBtn = MakeCompactTabButton(T("Bypass", "绕过"), () => SetLogActionFilter("BYPASS"));
        _logActionCloseBtn = MakeCompactTabButton(T("Close", "关闭"), () => SetLogActionFilter("CLOSE"));
        _logActionErrorBtn = MakeCompactTabButton(T("Error", "错误"), () => SetLogActionFilter("ERROR"));

        _logSearchBox = Input(T("Filter IP, process, rule, detail", "筛选 IP、进程、规则、详情"), search: true);
        _logSearchBox.TextChanged += (_, _) => RefreshLogs();

        _logRegexToggle = new ToggleButton
        {
            Content = T("Regex", "正则"),
            Padding = TabButtonPadding
        };
        _logRegexToggle.Checked += (_, _) => RefreshLogs();
        _logRegexToggle.Unchecked += (_, _) => RefreshLogs();

        _logDedupToggle = new ToggleButton
        {
            Content = IconContent(Icons.Filter, T("Merge", "合并")),
            Padding = CompactButtonPadding,
            IsChecked = true
        };
        AutomationProperties.SetName(_logDedupToggle, T("Merge repeated consecutive events", "合并连续重复事件"));
        ToolTipService.SetToolTip(_logDedupToggle, T("Merge repeated consecutive events", "合并连续重复事件"));
        _logDedupToggle.Checked += (_, _) => RefreshLogs();
        _logDedupToggle.Unchecked += (_, _) => RefreshLogs();

        _logFollowToggle = new ToggleButton
        {
            Content = IconContent(Icons.ArrowDown, T("Follow", "跟随")),
            Padding = CompactButtonPadding,
            IsChecked = true
        };
        AutomationProperties.SetName(_logFollowToggle, T("Auto-scroll to latest log", "自动滚动到最新日志"));
        ToolTipService.SetToolTip(_logFollowToggle, T("Auto-scroll to latest log", "自动滚动到最新日志"));
        _logFollowToggle.Checked += (_, _) => ScrollLogsToEnd();

        var exportBtn = AsyncButton(T("Export", "导出"), ExportLogsAsync, glyph: Icons.OpenFile);
        exportBtn.Padding = CompactButtonPadding;
        AutomationProperties.SetName(exportBtn, T("Export .log file", "导出 .log 文件"));
        ToolTipService.SetToolTip(exportBtn, T("Export .log file", "导出 .log 文件"));

        var clearLogBtn = Button(T("Clear", "清空"), () =>
        {
            _tracker.ClearEvents();
            if (_selectedLog != null)
                CollapseInspector();
            RefreshLogs();
        }, glyph: Icons.Delete);
        clearLogBtn.Padding = CompactButtonPadding;
        AutomationProperties.SetName(clearLogBtn, T("Clear log buffer", "清空日志"));
        ToolTipService.SetToolTip(clearLogBtn, T("Clear log buffer", "清空日志"));

        var logToolbarLeft = Row(_logActionAllBtn, _logActionProxyBtn, _logActionBypassBtn, _logActionCloseBtn, _logActionErrorBtn);
        logToolbarLeft.Spacing = 4;
        var logToolbarRight = Row(_logDedupToggle, _logFollowToggle, exportBtn, clearLogBtn);
        logToolbarRight.Spacing = 6;
        var logToolbarRow1 = Split(logToolbarLeft, logToolbarRight, 8);
        var logToolbarRow2 = Split(_logSearchBox, _logRegexToggle, 8);
        var logToolbar = new StackPanel { Spacing = 6, Children = { logToolbarRow1, logToolbarRow2 } };

        _logList = new ListView
        {
            SelectionMode = ListViewSelectionMode.Single,
            SingleSelectionFollowsFocus = false,
            ShowsScrollingPlaceholders = false,
            ItemsSource = _logRows,
            ItemContainerStyle = Resource<Style>("NetchLogItem"),
            ItemContainerTransitions = new TransitionCollection(),
            Transitions = new TransitionCollection(),
            Padding = new Thickness(0, 0, ScrollBarGutter, 0)
        };
        _logList.ItemTemplate = BuildLogRowTemplate();
        _logList.SelectionChanged += (_, _) =>
        {
            if (_suppressSelectionChanged)
                return;
            if (_logList.SelectedItem is ConnectionLogRowVm vm)
            {
                _selectedLog = vm.Event;
                _selectedConn = null;
                RenderInspector();
            }
        };
        _logList.ContainerContentChanging += (_, e) =>
        {
            if (e.InRecycleQueue || e.Item is not ConnectionLogRowVm row)
                return;
            if (e.ItemContainer is ListViewItem item)
                item.ContextFlyout = BuildLogMenu(row.Event);
            if (e.ItemContainer.ContentTemplateRoot is Grid root && root.FindName("ActionText") is TextBlock actionTb)
            {
                actionTb.Style = Resource<Style>(row.ActionText switch
                {
                    "ERROR" => "NetchErrorText",
                    "BYPASS_IP" or "DNS_DIRECT" => "NetchWarningText",
                    "CLOSE" or "BYPASS_PROC" or "DIRECT" => "NetchSecondaryText",
                    _ => "NetchPrimaryText"
                });
            }
        };

        _logEmptyText = Caption(string.Empty);
        _logEmptyText.HorizontalAlignment = HorizontalAlignment.Center;
        _logEmptyText.VerticalAlignment = VerticalAlignment.Center;

        var logHeaderStrip = BuildLogHeaderStrip();
        var logTableSurface = new Grid
        {
            RowSpacing = 4,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            },
            Children = { logHeaderStrip, new Grid { Children = { _logList, _logEmptyText } } }
        };
        Grid.SetRow((FrameworkElement)logTableSurface.Children[1], 1);

        var logCard = Card(logTableSurface, 8);
        logCard.Padding = new Thickness(6, 8, 6, 4);
        _logsPane.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _logsPane.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _logsPane.Children.Add(logToolbar);
        Grid.SetRow(logCard, 1);
        _logsPane.Children.Add(logCard);
        _logsPane.Visibility = Visibility.Collapsed;

        // Right Inspector drawer
        _inspectorTitle = Text(T("Inspector", "详情检查器"), 15, strong: true);
        _inspectorFields = new StackPanel { Spacing = 8 };
        _inspectorScroll = Scroll(_inspectorFields);
        _inspectorActions = new StackPanel { Spacing = 6 };
        var inspectorCloseBtn = IconButton(Icons.CloseWindow, T("Close inspector", "关闭检查器"), CollapseInspector, iconSize: 12);
        var inspectorBody = new Grid
        {
            RowSpacing = 10,
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = GridLength.Auto }
            },
            Children =
            {
                Split(_inspectorTitle, inspectorCloseBtn),
                _inspectorScroll,
                _inspectorActions
            }
        };
        Grid.SetRow((FrameworkElement)inspectorBody.Children[1], 1);
        Grid.SetRow((FrameworkElement)inspectorBody.Children[2], 2);

        _inspectorCard = Card(inspectorBody, 12);
        _inspectorCard.Width = 288;
        _inspectorCard.Margin = new Thickness(8, 0, 0, 0);
        _inspectorCard.Visibility = Visibility.Collapsed;

        KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Escape && _inspectorCard.Visibility == Visibility.Visible)
            {
                CollapseInspector();
                e.Handled = true;
            }
        };

        SizeChanged += (_, _) => _inspectorCard.Width = ActualWidth < 760 ? 252 : 288;

        var mainWorkspace = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children =
            {
                new Grid { Children = { _connectionsPane, _logsPane } },
                _inspectorCard
            }
        };
        Grid.SetColumn(_inspectorCard, 1);

        Content = Ui.Page(Body(topBar, _feedbackBar, _hotRuleBanner, mainWorkspace));
        UpdateSubFilterTabs();
        UpdateLogActionTabs();
        RefreshAll();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _tracker.ConnectionsUpdated += OnTrackerConnectionsUpdated;
        _tracker.EventsAppended += OnTrackerEventsAppended;
        _tracker.StateChanged += OnTrackerStateChanged;
        _tracker.PendingHotRules.Changed += OnHotRulesChanged;
        RefreshAll();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _tracker.ConnectionsUpdated -= OnTrackerConnectionsUpdated;
        _tracker.EventsAppended -= OnTrackerEventsAppended;
        _tracker.StateChanged -= OnTrackerStateChanged;
        _tracker.PendingHotRules.Changed -= OnHotRulesChanged;
        base.OnNavigatedFrom(e);
    }

    private void OnTrackerConnectionsUpdated() => DispatcherQueue?.TryEnqueue(() =>
    {
        UpdateHeaderCountersAndTelemetry();
        if (_activeView == 0)
            RefreshConnections();
        if (_selectedConn != null && _inspectorCard.Visibility == Visibility.Visible)
            UpdateInspectorLiveConnectionFields();
    });

    private void OnTrackerEventsAppended(IReadOnlyList<ProcessConnectionEvent> added) => DispatcherQueue?.TryEnqueue(() =>
    {
        UpdateHeaderCountersAndTelemetry();
        if (_activeView == 1 && !_hasRegexError)
            AppendFilteredEvents(added);
    });

    private void OnTrackerStateChanged() => DispatcherQueue?.TryEnqueue(RefreshAll);

    private void OnHotRulesChanged() => DispatcherQueue?.TryEnqueue(RefreshHotRuleBanner);

    private void RefreshAll()
    {
        _syncingControls = true;
        try
        {
            _trackUnmatchedToggle.IsOn = _tracker.TrackUnmatched;
            _writeLogToggle.IsOn = Global.Settings.Redirector.LogConnectionsToFile;
            _connRouteBox.Visibility = _tracker.TrackUnmatched ? Visibility.Visible : Visibility.Collapsed;
            if (!_tracker.TrackUnmatched)
                _connRouteBox.SelectedIndex = 0;
        }
        finally
        {
            _syncingControls = false;
        }

        if (_tracker.TrackUnmatched && !string.IsNullOrEmpty(_tracker.EtwError))
        {
            _showingEtwWarning = true;
            var etwWarning = string.Equals(_tracker.EtwError, EtwNetworkMonitor.AdminRequiredError, StringComparison.Ordinal)
                ? T(EtwNetworkMonitor.AdminRequiredError, "需要管理员权限以监控未命中进程流量")
                : _tracker.EtwError;
            ShowFeedback(etwWarning, InfoBarSeverity.Warning);
        }
        else if (_showingEtwWarning)
        {
            _showingEtwWarning = false;
            if (_feedbackBar.Severity == InfoBarSeverity.Warning)
                _feedbackBar.IsOpen = false;
        }

        UpdateHeaderCountersAndTelemetry();
        UpdateSubFilterTabs();
        UpdateLogActionTabs();
        RefreshHotRuleBanner();
        if (_activeView == 0)
            RefreshConnections();
        else
            RefreshLogs();
    }

    private void SwitchView(int viewIndex)
    {
        _activeView = viewIndex;
        _connectionsPane.Visibility = viewIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        _logsPane.Visibility = viewIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        _tabConnections.Style = Resource<Style>(viewIndex == 0 ? "NetchWizardTabActive" : "NetchWizardTab");
        _tabLogs.Style = Resource<Style>(viewIndex == 1 ? "NetchWizardTabActive" : "NetchWizardTab");
        CollapseInspector();
        if (viewIndex == 0)
            RefreshConnections();
        else
            RefreshLogs();
    }

    private void UpdateHeaderCountersAndTelemetry()
    {
        var active = _tracker.ActiveConnections;
        var closed = _tracker.ClosedConnections;
        var events = _tracker.Events;

        SetTextContentIfChanged(_tabConnections, TF("Live ({0})", "实时连接 ({0})", active.Count));
        SetTextContentIfChanged(_tabLogs, TF("Logs ({0})", "连接日志 ({0})", events.Count));

        SetTextContentIfChanged(_connFilterActiveBtn, TF("Active ({0})", "活跃 ({0})", active.Count));
        SetTextContentIfChanged(_connFilterClosedBtn, TF("Closed ({0})", "已关闭 ({0})", closed.Count));
        SetTextContentIfChanged(_connFilterAllBtn, TF("All ({0})", "全部 ({0})", active.Count + closed.Count));

        if (_tracker.IsProcessModeActive)
        {
            var upCompact = Format.Bytes(_tracker.TotalUpSpeedBps).Replace(" ", string.Empty, StringComparison.Ordinal);
            var downCompact = Format.Bytes(_tracker.TotalDownSpeedBps).Replace(" ", string.Empty, StringComparison.Ordinal);
            _telemetryText.Text = $"↑{upCompact}/s · ↓{downCompact}/s";
            var tip =
                $"↑ {Format.Bytes(_tracker.TotalUpBytes)} ({Format.Rate(_tracker.TotalUpSpeedBps)})\n" +
                $"↓ {Format.Bytes(_tracker.TotalDownBytes)} ({Format.Rate(_tracker.TotalDownSpeedBps)})";
            if (_tracker.TrackUnmatched)
            {
                tip += "\n" + TF("Direct (ETW): ↑ {0} · ↓ {1}", "直连 (ETW)：↑ {0} · ↓ {1}",
                    Format.Rate(_tracker.DirectUpSpeedBps), Format.Rate(_tracker.DirectDownSpeedBps));
            }
            ToolTipService.SetToolTip(_telemetryText, tip);
        }
        else if (_tracker.TrackUnmatched)
        {
            var upCompact = Format.Bytes(_tracker.DirectUpSpeedBps).Replace(" ", string.Empty, StringComparison.Ordinal);
            var downCompact = Format.Bytes(_tracker.DirectDownSpeedBps).Replace(" ", string.Empty, StringComparison.Ordinal);
            _telemetryText.Text = TF("Direct ↑{0}/s · ↓{1}/s", "直连 ↑{0}/s · ↓{1}/s", upCompact, downCompact);
            ToolTipService.SetToolTip(_telemetryText, TF(
                "Direct traffic (ETW): ↑ {0} · ↓ {1}",
                "直连流量 (ETW)：↑ {0} · ↓ {1}",
                Format.Rate(_tracker.DirectUpSpeedBps),
                Format.Rate(_tracker.DirectDownSpeedBps)));
        }
        else
        {
            _telemetryText.Text = T("Inactive", "未启动");
            ToolTipService.SetToolTip(_telemetryText, T("Process mode is not active", "进程模式未启动"));
        }

        _closeAllBtn.IsEnabled = _tracker.IsProcessModeActive && active.Any(c => c.CanClose);
        _clearClosedBtn.IsEnabled = closed.Count > 0;
        if (_pauseBtnPausedState != _tracker.IsPaused)
        {
            _pauseBtnPausedState = _tracker.IsPaused;
            SetButtonContent(_pauseBtn, _tracker.IsPaused ? T("Resume", "恢复") : T("Pause", "暂停"),
                _tracker.IsPaused ? Icons.Play : Icons.Pause);
        }
    }

    private static void SetTextContentIfChanged(Button button, string text)
    {
        if (!string.Equals(button.Content as string, text, StringComparison.Ordinal))
            button.Content = text;
    }

    private void RefreshHotRuleBanner()
    {
        var entries = _tracker.PendingHotRules.Entries;
        if (entries.Count == 0)
        {
            _hotRuleBanner.Visibility = Visibility.Collapsed;
            return;
        }

        _hotRuleBanner.Visibility = Visibility.Visible;
        var mode = _tracker.ActiveMode;
        var summaryText = mode == null
            ? TF("{0} unsaved session rule(s)", "有 {0} 条未保存的临时规则", entries.Count)
            : TF("{0} unsaved rule(s) -> {1}", "有 {0} 条未保存规则 -> {1}", entries.Count, HotRulePersistence.DescribeTarget(mode));
        _hotRuleSummary.Text = summaryText;
        ToolTipService.SetToolTip(_hotRuleSummary, summaryText);

        _hotRuleChips.Children.Clear();
        var console = new FontFamily("Consolas");
        foreach (var entry in entries)
        {
            var text = Caption($"{HotRulePersistence.FormatEntryBadge(entry.Kind)} {entry.Pattern}");
            text.FontFamily = console;
            text.VerticalAlignment = VerticalAlignment.Center;
            var removeBtn = IconButton(Icons.CloseWindow, T("Remove temporary rule", "移除临时规则"),
                () => _tracker.RemoveHotRule(entry), iconSize: 10);
            removeBtn.Padding = new Thickness(4, 2, 4, 2);
            removeBtn.MinWidth = 0;
            removeBtn.MinHeight = 0;
            removeBtn.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            removeBtn.BorderThickness = new Thickness(0);

            _hotRuleChips.Children.Add(new Border
            {
                Style = Resource<Style>("NetchWizardChip"),
                Child = Row(text, removeBtn)
            });
        }

        _hotRuleSaveHost.Children.Clear();
        if (mode != null)
        {
            var primaryLabel = HotRulePersistence.PrimarySaveLabel(mode);
            var secondaryLabel = HotRulePersistence.SecondarySaveLabel(mode);

            if (secondaryLabel != null)
            {
                var split = new SplitButton { Content = primaryLabel };
                split.Click += (_, _) => RunHotRuleSaveAsync(() => HotRulePersistence.SavePrimaryAsync(mode)).Forget();
                var flyout = new MenuFlyout();
                flyout.Items.Add(Ui.MenuItem(secondaryLabel, Icons.Copy,
                    () => RunHotRuleSaveAsync(() => HotRulePersistence.SaveSecondaryAsync(mode)).Forget()));
                split.Flyout = flyout;
                _hotRuleSaveHost.Children.Add(split);
            }
            else
            {
                var saveBtn = AsyncButton(primaryLabel, () => HotRulePersistence.SavePrimaryAsync(mode), accent: true, glyph: Icons.Save);
                saveBtn.Padding = CompactButtonPadding;
                if (HotRulePersistence.Classify(mode) == HotRulePersistence.ModeSaveKind.LegacyTxt)
                {
                    ToolTipService.SetToolTip(saveBtn, T(
                        "Legacy .txt modes do not support IP rules; saves as a new JSON mode and switches automatically.",
                        "旧版 .txt 模式不支持 IP 规则，将生成新版模式文件并自动切换"));
                }
                _hotRuleSaveHost.Children.Add(saveBtn);
            }
        }

        var discardBtn = Button(T("Discard", "放弃"), () => _tracker.DiscardHotRules(), glyph: Icons.Delete);
        discardBtn.Padding = CompactButtonPadding;
        _hotRuleSaveHost.Children.Add(discardBtn);
    }

    private async Task RunHotRuleSaveAsync(Func<Task> saveAction)
    {
        if (_savingHotRules)
            return;

        _savingHotRules = true;
        try
        {
            await saveAction();
            _hasRegexError = false;
            ShowFeedback(T("Rules saved to mode.", "规则已保存到模式文件。"), InfoBarSeverity.Success);
        }
        catch (Exception e)
        {
            _hasRegexError = false;
            ShowFeedback(e.Message, InfoBarSeverity.Error);
        }
        finally
        {
            _savingHotRules = false;
        }
    }

    private void RefreshConnections()
    {
        var active = _tracker.ActiveConnections;
        var closed = _tracker.ClosedConnections;
        IEnumerable<ProcessConnectionSnapshot> source = _connStateFilter switch
        {
            0 => active,
            1 => closed,
            _ => active.Concat(closed)
        };

        var protocol = _connProtocolBox.SelectedIndex switch
        {
            1 => "TCP",
            2 => "UDP",
            _ => "ALL"
        };

        var routeFilter = _connRouteBox.Visibility == Visibility.Visible
            ? _connRouteBox.SelectedIndex switch
            {
                1 => "PROXY",
                2 => "DIRECT",
                _ => "ALL"
            }
            : "ALL";

        var filtered = ConnectionTrackerService.FilterConnections(source, _connSearchBox.Text, protocol, routeFilter);
        var target = SortConnections(filtered).ToArray();
        var selectedId = _selectedConn?.Id;
        _suppressSelectionChanged = true;
        try
        {
            var common = Math.Min(_connRows.Count, target.Length);
            for (var i = 0; i < common; i++)
                _connRows[i].UpdateFrom(target[i]);

            for (var i = _connRows.Count - 1; i >= target.Length; i--)
                _connRows.RemoveAt(i);

            for (var i = common; i < target.Length; i++)
                _connRows.Add(new ConnectionRowVm(target[i]));

            if (selectedId is { } id)
            {
                var match = _connRows.FirstOrDefault(r => r.Id == id);
                if (match != null)
                {
                    if (!ReferenceEquals(_connList.SelectedItem, match))
                        _connList.SelectedItem = match;
                    _selectedConn = match.Snapshot;
                }
                else if (_connList.SelectedItem != null)
                {
                    _connList.SelectedItem = null;
                }
            }
        }
        finally
        {
            _suppressSelectionChanged = false;
        }

        _connEmptyText.Text = _tracker.IsProcessModeActive
            ? T("No matching connections", "暂无匹配的连接")
            : _tracker.TrackUnmatched
                ? _tracker.ActiveConnections.Count == 0 && _tracker.ClosedConnections.Count == 0
                    ? T("Monitoring direct traffic — waiting for local process activity...", "正在监控直连流量，等待本机进程产生网络活动…")
                    : T("No matching connections", "暂无匹配的连接")
                : T("Start a Process mode scene to view live connections.", "启动进程模式场景后将在此显示实时连接。");
        _connEmptyText.Visibility = _connRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshLogs()
    {
        var filtered = ConnectionTrackerService.FilterEvents(
            _tracker.Events,
            _logSearchBox.Text,
            _logRegexToggle.IsChecked == true,
            _logActionFilter,
            _logDedupToggle.IsChecked == true,
            out var regexError);

        if (regexError != null)
        {
            _hasRegexError = true;
            ShowFeedback(TF("Invalid regex: {0}", "正则表达式无效：{0}", regexError), InfoBarSeverity.Error);
        }
        else if (_hasRegexError)
        {
            _hasRegexError = false;
            if (_feedbackBar.Severity == InfoBarSeverity.Error)
                _feedbackBar.IsOpen = false;
        }

        var selectedSeq = _selectedLog?.Seq;
        _suppressSelectionChanged = true;
        try
        {
            _logRows.Clear();
            foreach (var item in filtered)
                _logRows.Add(new ConnectionLogRowVm(item));

            if (selectedSeq is { } seq)
            {
                var match = _logRows.FirstOrDefault(r => r.Event.Seq == seq);
                if (match != null)
                {
                    _logList.SelectedItem = match;
                    _selectedLog = match.Event;
                }
            }
        }
        finally
        {
            _suppressSelectionChanged = false;
        }

        _logEmptyText.Text = regexError != null
            ? T("Invalid regular expression", "正则表达式语法错误")
            : _tracker.Events.Count == 0
                ? T("No connection log entries yet", "暂无连接日志记录")
                : T("No matching log entries", "暂无符合筛选条件的记录");
        _logEmptyText.Visibility = _logRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (_logFollowToggle.IsChecked == true)
            ScrollLogsToEnd();
    }

    private void AppendFilteredEvents(IReadOnlyList<ProcessConnectionEvent> added)
    {
        if (added.Count == 0)
            return;

        Regex? compiledRegex = null;
        var trimmedQuery = _logSearchBox.Text?.Trim() ?? string.Empty;
        if (trimmedQuery.Length > 0 && _logRegexToggle.IsChecked == true)
        {
            try
            {
                compiledRegex = new Regex(trimmedQuery, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            catch (ArgumentException)
            {
                return;
            }
        }

        var dedup = _logDedupToggle.IsChecked == true;
        var appendedAny = false;

        _suppressSelectionChanged = true;
        try
        {
            foreach (var ev in added)
            {
                if (!MatchesLogEvent(ev, _logActionFilter, trimmedQuery, compiledRegex))
                    continue;

                appendedAny = true;
                if (dedup && _logRows.Count > 0 && IsSameConsecutiveLogEvent(_logRows[^1].Event, ev))
                {
                    var lastIdx = _logRows.Count - 1;
                    _logRows[lastIdx].MergeRepeat(ev);
                    if (_logList.SelectedIndex == lastIdx)
                        _selectedLog = ev;
                }
                else
                {
                    _logRows.Add(new ConnectionLogRowVm(new FilteredConnectionEventRow(ev, 1)));
                    if (_logRows.Count > MaxVisibleLogRows)
                        _logRows.RemoveAt(0);
                }
            }
        }
        finally
        {
            _suppressSelectionChanged = false;
        }

        if (!appendedAny)
            return;

        _logEmptyText.Visibility = Visibility.Collapsed;
        if (_logFollowToggle.IsChecked == true)
            ScrollLogsToEnd();
    }

    private void ScrollLogsToEnd()
    {
        if (_logRows.Count > 0)
            _logList.ScrollIntoView(_logRows[^1]);
    }

    private void UpdateSubFilterTabs()
    {
        _connFilterActiveBtn.Style = Resource<Style>(_connStateFilter == 0 ? "NetchWizardTabActive" : "NetchWizardTab");
        _connFilterClosedBtn.Style = Resource<Style>(_connStateFilter == 1 ? "NetchWizardTabActive" : "NetchWizardTab");
        _connFilterAllBtn.Style = Resource<Style>(_connStateFilter == 2 ? "NetchWizardTabActive" : "NetchWizardTab");
    }

    private void SetLogActionFilter(string filter)
    {
        _logActionFilter = filter;
        UpdateLogActionTabs();
        RefreshLogs();
    }

    private void UpdateLogActionTabs()
    {
        _logActionAllBtn.Style = Resource<Style>(_logActionFilter == "ALL" ? "NetchWizardTabActive" : "NetchWizardTab");
        _logActionProxyBtn.Style = Resource<Style>(_logActionFilter == "PROXY" ? "NetchWizardTabActive" : "NetchWizardTab");
        _logActionBypassBtn.Style = Resource<Style>(_logActionFilter == "BYPASS" ? "NetchWizardTabActive" : "NetchWizardTab");
        _logActionCloseBtn.Style = Resource<Style>(_logActionFilter == "CLOSE" ? "NetchWizardTabActive" : "NetchWizardTab");
        _logActionErrorBtn.Style = Resource<Style>(_logActionFilter == "ERROR" ? "NetchWizardTabActive" : "NetchWizardTab");
    }

    private void TogglePause()
    {
        _tracker.IsPaused = !_tracker.IsPaused;
        UpdateHeaderCountersAndTelemetry();
        if (!_tracker.IsPaused)
            _tracker.RefreshConnectionsNow();
    }

    private void CloseAllActiveConnections()
    {
        _tracker.CloseAllConnections();
        CollapseInspector();
        RefreshConnections();
    }

    private async Task ExportLogsAsync()
    {
        if (App.MainWindow is not { } window)
            return;

        var picker = new FileSavePicker(window.AppWindow.Id)
        {
            SuggestedFileName = $"netch-connections-{DateTime.Now:yyyyMMdd-HHmmss}.log"
        };
        picker.FileTypeChoices.Add("Log file", [".log"]);
        if (await picker.PickSaveFileAsync() is not { } file)
            return;

        var lines = _logRows.Select(r => r.Event.FormatLogLine()).ToArray();
        await File.WriteAllLinesAsync(file.Path, lines, new UTF8Encoding(false));
        _hasRegexError = false;
        ShowFeedback(TF("Exported {0} log line(s).", "已导出 {0} 条日志。", lines.Length), InfoBarSeverity.Success);
    }

    private void CollapseInspector()
    {
        _selectedConn = null;
        _selectedLog = null;
        ClearInspectorLiveRefs();
        _suppressSelectionChanged = true;
        try
        {
            _connList.SelectedItem = null;
            _logList.SelectedItem = null;
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
        _inspectorCard.Visibility = Visibility.Collapsed;
    }

    private void ClearInspectorLiveRefs()
    {
        _inspectorStateVal = null;
        _inspectorTrafficVal = null;
        _inspectorDurationVal = null;
        _inspectorCloseReasonVal = null;
        _inspectorCloseReasonContainer = null;
        _inspectorCloseBtn = null;
    }

    private void UpdateInspectorLiveConnectionFields()
    {
        if (_selectedConn == null)
            return;

        var latest = _tracker.ActiveConnections.FirstOrDefault(c => c.Id == _selectedConn.Id)
            ?? _tracker.ClosedConnections.FirstOrDefault(c => c.Id == _selectedConn.Id);
        if (latest == null)
            return;

        _selectedConn = latest;
        if (_inspectorStateVal != null)
            _inspectorStateVal.Text = FormatConnectionState(latest);
        if (_inspectorTrafficVal != null)
            _inspectorTrafficVal.Text = $"↑ {Format.Bytes(latest.UpBytes)} · ↓ {Format.Bytes(latest.DownBytes)}";
        if (_inspectorDurationVal != null)
            _inspectorDurationVal.Text = Format.Elapsed(latest.Duration);
        if (_inspectorCloseReasonVal != null && _inspectorCloseReasonContainer != null)
        {
            _inspectorCloseReasonVal.Text = latest.CloseReason;
            _inspectorCloseReasonContainer.Visibility = string.IsNullOrWhiteSpace(latest.CloseReason)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
        if (_inspectorCloseBtn != null)
            _inspectorCloseBtn.Visibility = latest.CanClose ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RenderInspector()
    {
        ClearInspectorLiveRefs();
        _inspectorFields.Children.Clear();
        _inspectorActions.Children.Clear();

        if (_selectedConn is { } conn)
        {
            _inspectorTitle.Text = T("Connection details", "连接详情");
            AddInspectorField(T("Process", "进程"), string.IsNullOrEmpty(conn.ProcessName) ? $"PID {conn.Pid}" : $"{conn.ProcessName} ({conn.Pid})");
            AddInspectorField(T("Process path", "进程路径"), conn.ProcessPath);
            if (!string.IsNullOrEmpty(conn.ParentName))
                AddInspectorField(T("Parent", "父进程"), conn.ParentName);
            _inspectorStateVal = AddInspectorField(T("Protocol / State", "协议 / 状态"), FormatConnectionState(conn));
            AddInspectorField(T("Destination", "目标"), conn.Endpoint);
            AddInspectorField(T("Rule", "命中规则"), conn.IsDirect
                ? T("Direct (ETW)", "直连 (ETW)")
                : string.IsNullOrEmpty(conn.MatchedRule)
                    ? conn.RuleSource
                    : $"{conn.MatchedRule} ({conn.RuleSource})");
            _inspectorTrafficVal = AddInspectorField(T("Traffic", "流量"), $"↑ {Format.Bytes(conn.UpBytes)} · ↓ {Format.Bytes(conn.DownBytes)}");
            _inspectorDurationVal = AddInspectorField(T("Duration", "持续时长"), Format.Elapsed(conn.Duration));

            (_inspectorCloseReasonContainer, _inspectorCloseReasonVal) = CreateInspectorFieldContainer(T("Close reason", "结束原因"), conn.CloseReason);
            if (string.IsNullOrWhiteSpace(conn.CloseReason))
                _inspectorCloseReasonContainer.Visibility = Visibility.Collapsed;
            _inspectorFields.Children.Add(_inspectorCloseReasonContainer);

            _inspectorCloseBtn = PopulateQuickRuleButtons(conn.RemoteIp, conn.ProcessName, conn.Id, conn.CanClose,
                $"{conn.ProcessName}({conn.Pid}) {conn.Protocol} {conn.Endpoint} [{(conn.IsDirect ? T("Direct", "直连") : conn.MatchedRule)}]");
            _inspectorCard.Visibility = Visibility.Visible;
            _inspectorScroll.ChangeView(null, 0, null, disableAnimation: true);
            return;
        }

        if (_selectedLog is { } ev)
        {
            _inspectorTitle.Text = T("Event details", "日志详情");
            AddInspectorField("# / Time", $"#{ev.Seq} · {ev.TimestampLocal:yyyy-MM-dd HH:mm:ss.fff}");
            AddInspectorField(T("Action / Proto", "动作 / 协议"), $"{ev.Action} · {ev.Protocol}");
            AddInspectorField(T("Process", "进程"), string.IsNullOrEmpty(ev.ProcessName) ? $"PID {ev.Pid}" : $"{ev.ProcessName} ({ev.Pid})");
            AddInspectorField(T("Process path", "进程路径"), ev.ProcessPath);
            if (!string.IsNullOrEmpty(ev.ParentName))
                AddInspectorField(T("Parent", "父进程"), ev.ParentName);
            AddInspectorField(T("Destination", "目标"), ev.Endpoint);
            AddInspectorField(T("Matched rule", "命中规则"), string.IsNullOrEmpty(ev.MatchedRule) ? ev.RuleSource : $"{ev.MatchedRule} ({ev.RuleSource})");
            if (ev.UpBytes > 0 || ev.DownBytes > 0)
                AddInspectorField(T("Traffic", "流量"), $"↑ {Format.Bytes(ev.UpBytes)} · ↓ {Format.Bytes(ev.DownBytes)}");
            if (ev.DurationMs > 0)
                AddInspectorField(T("Duration", "耗时"), Format.Elapsed(TimeSpan.FromMilliseconds(ev.DurationMs)));
            if (!string.IsNullOrEmpty(ev.Detail))
                AddInspectorField(T("Detail", "详情"), ev.Detail);

            var isStillActive = ev.ConnectionId != 0 && _tracker.ActiveConnections.Any(a => a.Id == ev.ConnectionId && a.CanClose);
            PopulateQuickRuleButtons(ev.RemoteIp, ev.ProcessName, ev.ConnectionId, isStillActive, ev.FormatLogLine());
            _inspectorCard.Visibility = Visibility.Visible;
            _inspectorScroll.ChangeView(null, 0, null, disableAnimation: true);
            return;
        }

        _inspectorCard.Visibility = Visibility.Collapsed;
    }

    private static string FormatConnectionState(ProcessConnectionSnapshot conn) =>
        $"{conn.Protocol} · {(conn.IsDirect ? T("Direct", "直连") : T("Proxied", "代理"))} · {conn.State}";

    private TextBlock? AddInspectorField(string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var (container, textBlock) = CreateInspectorFieldContainer(label, value);
        _inspectorFields.Children.Add(container);
        return textBlock;
    }

    private static (StackPanel Container, TextBlock ValueBlock) CreateInspectorFieldContainer(string label, string? value)
    {
        var title = Caption(label);
        var val = new TextBlock
        {
            Text = value ?? string.Empty,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true
        };
        return (new StackPanel { Spacing = 2, Children = { title, val } }, val);
    }

    private Button? PopulateQuickRuleButtons(string remoteIp, string processName, ulong connectionId, bool canClose, string copyPayload)
    {
        _inspectorActions.Children.Add(Caption(T("Quick rules & actions", "快速规则与操作")));

        if (!string.IsNullOrWhiteSpace(remoteIp) && RouteUtils.TryNormalizeIpOrCidr(remoteIp, out var singleCidr))
        {
            var isV6 = remoteIp.Contains(':');
            AddFullWidthActionButton(
                TF("Bypass IP ({0})", "绕过 IP ({0})", singleCidr),
                () => ApplyQuickRule(HotRuleKind.BypassIp, singleCidr, remoteIp));

            if (RouteUtils.TryToSubnet24(remoteIp, out var subnetCidr))
            {
                AddFullWidthActionButton(
                    isV6 ? TF("Bypass /64 ({0})", "绕过 /64 ({0})", subnetCidr) : TF("Bypass /24 ({0})", "绕过 /24 ({0})", subnetCidr),
                    () => ApplyQuickRule(HotRuleKind.BypassIp, subnetCidr, subnetCidr));
            }

            AddFullWidthActionButton(
                TF("Proxy IP ({0})", "仅代理 IP ({0})", singleCidr),
                () => ApplyQuickRule(HotRuleKind.HandleIp, singleCidr, remoteIp));
        }

        if (!string.IsNullOrWhiteSpace(processName) &&
            ConnectionTrackerService.ClassifyProcessRuleSafety(processName) != ProcessRuleSafety.Blocked)
        {
            var regex = ConnectionTrackerService.BuildProcessRegexFromName(processName);
            AddFullWidthActionButton(
                TF("Bypass process ({0})", "绕过此进程 ({0})", processName),
                () => ApplyQuickRule(HotRuleKind.BypassProcess, regex, processName));
            AddFullWidthActionButton(
                TF("Proxy process ({0})", "代理此进程 ({0})", processName),
                () => ApplyProxyProcessRuleAsync(regex, processName).Forget());
        }

        Button? closeBtn = null;
        if (canClose && connectionId != 0)
        {
            closeBtn = Button(T("Close connection", "断开此连接"), () =>
            {
                _tracker.CloseConnection(connectionId);
                RenderInspector();
            }, glyph: Icons.CloseWindow);
            closeBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
            _inspectorActions.Children.Add(closeBtn);
        }

        var copyBtn = Button(T("Copy details", "复制详情"), () => Copy(copyPayload), glyph: Icons.Copy);
        copyBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
        _inspectorActions.Children.Add(copyBtn);
        return closeBtn;
    }

    private void AddFullWidthActionButton(string label, Action action)
    {
        var btn = Button(label, action);
        btn.HorizontalAlignment = HorizontalAlignment.Stretch;
        btn.HorizontalContentAlignment = HorizontalAlignment.Left;
        _inspectorActions.Children.Add(btn);
    }

    private async Task ApplyProxyProcessRuleAsync(string regex, string processName)
    {
        if (ConnectionTrackerService.ClassifyProcessRuleSafety(processName) == ProcessRuleSafety.Caution)
        {
            var confirmed = await ShowAsync(
                XamlRoot,
                TF("Proxy all {0} services?", "代理全部 {0} 服务？", processName),
                Text(TF(
                    "Multiple Windows services (including DNS, updates, and Store) share {0}. Proxying by process name affects all of them and may disrupt system networking or increase proxy traffic.",
                    "Windows 中多个系统服务（含 DNS、系统更新、微软商店等）共用 {0}。按进程名代理会同时影响全部同类服务，可能导致系统网络异常或代理流量激增。",
                    processName), 13),
                confirm: T("Proxy anyway", "仍然代理"));
            if (!confirmed)
                return;
        }

        ApplyQuickRule(HotRuleKind.HandleProcess, regex, processName);
    }

    private void ApplyQuickRule(HotRuleKind kind, string pattern, string displayLabel)
    {
        if (_tracker.TryApplyHotRule(kind, pattern, displayLabel, out var error))
        {
            _hasRegexError = false;
            ShowFeedback(TF("Applied session rule: {0}", "已热下发临时规则：{0}", pattern), InfoBarSeverity.Informational);
        }
        else if (!string.IsNullOrEmpty(error))
        {
            _hasRegexError = false;
            ShowFeedback(error, InfoBarSeverity.Error);
        }
    }

    private void PopulateConnectionMenu(MenuFlyout menu, ProcessConnectionSnapshot conn)
    {
        menu.Items.Clear();
        AddRuleMenuItems(menu, conn.RemoteIp, conn.ProcessName);

        if (conn.CanClose)
        {
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(Ui.MenuItem(T("Close connection", "断开连接"), Icons.CloseWindow,
                () => _tracker.CloseConnection(conn.Id), destructive: true));
        }

        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(Ui.MenuItem(T("Copy target", "复制目标地址"), Icons.Copy, () => Copy(conn.Endpoint)));
    }

    private MenuFlyout BuildLogMenu(ProcessConnectionEvent ev)
    {
        var menu = new MenuFlyout();
        AddRuleMenuItems(menu, ev.RemoteIp, ev.ProcessName);

        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(Ui.MenuItem(T("Copy line", "复制此行"), Icons.Copy, () => Copy(ev.FormatLogLine())));
        return menu;
    }

    private void AddRuleMenuItems(MenuFlyout menu, string remoteIp, string processName)
    {
        if (!string.IsNullOrWhiteSpace(remoteIp) && RouteUtils.TryNormalizeIpOrCidr(remoteIp, out var singleCidr))
        {
            menu.Items.Add(Ui.MenuItem(TF("Bypass IP ({0})", "绕过 IP ({0})", singleCidr), Icons.Shield,
                () => ApplyQuickRule(HotRuleKind.BypassIp, singleCidr, remoteIp)));
            if (RouteUtils.TryToSubnet24(remoteIp, out var subCidr))
            {
                menu.Items.Add(Ui.MenuItem(TF("Bypass subnet ({0})", "绕过网段 ({0})", subCidr), Icons.Shield,
                    () => ApplyQuickRule(HotRuleKind.BypassIp, subCidr, subCidr)));
            }
            menu.Items.Add(Ui.MenuItem(TF("Proxy IP ({0})", "仅代理 IP ({0})", singleCidr), Icons.Globe,
                () => ApplyQuickRule(HotRuleKind.HandleIp, singleCidr, remoteIp)));
        }

        if (!string.IsNullOrWhiteSpace(processName) &&
            ConnectionTrackerService.ClassifyProcessRuleSafety(processName) != ProcessRuleSafety.Blocked)
        {
            var regex = ConnectionTrackerService.BuildProcessRegexFromName(processName);
            menu.Items.Add(Ui.MenuItem(TF("Bypass {0}", "绕过 {0}", processName), Icons.Filter,
                () => ApplyQuickRule(HotRuleKind.BypassProcess, regex, processName)));
            menu.Items.Add(Ui.MenuItem(TF("Proxy {0}", "代理 {0}", processName), Icons.Play,
                () => ApplyProxyProcessRuleAsync(regex, processName).Forget()));
        }
    }

    private void ShowFeedback(string message, InfoBarSeverity severity)
    {
        _feedbackBar.Message = message;
        _feedbackBar.Severity = severity;
        _feedbackBar.IsOpen = true;
    }

    private static bool MatchesLogEvent(ProcessConnectionEvent ev, string actionFilter, string query, Regex? compiledRegex)
    {
        var actionOk = actionFilter.ToUpperInvariant() switch
        {
            "ALL" => true,
            "PROXY" => ev.Action is "PROXY" or "DNS_PROXY",
            "BYPASS" => ev.Action is "BYPASS_IP" or "BYPASS_PROC" or "DNS_DIRECT" or "DIRECT",
            "CLOSE" => ev.Action is "CLOSE",
            "ERROR" => ev.Action is "ERROR",
            _ => string.Equals(ev.Action, actionFilter, StringComparison.OrdinalIgnoreCase)
        };
        if (!actionOk)
            return false;

        if (query.Length == 0)
            return true;

        if (compiledRegex != null)
        {
            return compiledRegex.IsMatch(ev.Action) ||
                   compiledRegex.IsMatch(ev.Protocol) ||
                   compiledRegex.IsMatch(ev.ProcessName) ||
                   compiledRegex.IsMatch(ev.ProcessPath) ||
                   compiledRegex.IsMatch(ev.Endpoint) ||
                   compiledRegex.IsMatch(ev.MatchedRule) ||
                   compiledRegex.IsMatch(ev.Detail);
        }

        return ev.Action.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               ev.Protocol.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               ev.ProcessName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               ev.ProcessPath.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               ev.ParentName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               ev.Endpoint.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               ev.MatchedRule.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               ev.RuleSource.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               ev.Detail.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               ev.Pid.ToString().Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameConsecutiveLogEvent(ProcessConnectionEvent a, ProcessConnectionEvent b) =>
        a.Pid == b.Pid &&
        a.RemotePort == b.RemotePort &&
        string.Equals(a.Action, b.Action, StringComparison.Ordinal) &&
        string.Equals(a.Protocol, b.Protocol, StringComparison.Ordinal) &&
        string.Equals(a.RemoteIp, b.RemoteIp, StringComparison.Ordinal) &&
        string.Equals(a.MatchedRule, b.MatchedRule, StringComparison.Ordinal) &&
        string.Equals(a.ProcessName, b.ProcessName, StringComparison.Ordinal);

    private static Button MakeCompactTabButton(string label, Action onClick)
    {
        var btn = new Button
        {
            Content = label,
            Style = Resource<Style>("NetchWizardTab"),
            Padding = CompactButtonPadding
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private Grid BuildConnectionHeaderStrip() => new()
    {
        Margin = new Thickness(10, 0, 10 + ScrollBarGutter, 0),
        ColumnSpacing = 6,
        ColumnDefinitions =
        {
            new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(50) },
            new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(0.95, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(76) },
            new ColumnDefinition { Width = new GridLength(76) },
            new ColumnDefinition { Width = new GridLength(70) },
            new ColumnDefinition { Width = new GridLength(66) }
        },
        Children =
        {
            SortableHeaderCol(T("Process", "进程"), 0, ConnectionSortColumn.Process),
            SortableHeaderCol(T("Proto", "协议"), 1, ConnectionSortColumn.Protocol),
            SortableHeaderCol(T("Target", "目标"), 2, ConnectionSortColumn.Target),
            SortableHeaderCol(T("Rule", "规则"), 3, ConnectionSortColumn.Rule),
            SortableHeaderCol(T("Upload", "上传"), 4, ConnectionSortColumn.Upload, HorizontalAlignment.Right),
            SortableHeaderCol(T("Download", "下载"), 5, ConnectionSortColumn.Download, HorizontalAlignment.Right),
            SortableHeaderCol(T("Traffic", "流量"), 6, ConnectionSortColumn.Traffic, HorizontalAlignment.Right),
            SortableHeaderCol(T("Time", "时长"), 7, ConnectionSortColumn.Duration, HorizontalAlignment.Right)
        }
    };

    private Button SortableHeaderCol(string label, int col, ConnectionSortColumn sortColumn, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var tb = Caption(label);
        tb.FontSize = 12;
        tb.TextTrimming = TextTrimming.CharacterEllipsis;
        tb.HorizontalAlignment = align;
        tb.VerticalAlignment = VerticalAlignment.Center;

        var btn = new Button
        {
            Content = tb,
            Style = Resource<Style>("NetchWizardTab"),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(3, 2, 3, 2),
            Margin = new Thickness(-3, 0, -3, 0),
            MinWidth = 0,
            MinHeight = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = align,
            VerticalAlignment = VerticalAlignment.Center
        };
        btn.Click += (_, _) => ToggleConnectionSort(sortColumn);
        Grid.SetColumn(btn, col);
        _connSortHeaders[sortColumn] = (btn, label, tb);
        return btn;
    }

    private static bool IsDefaultDescending(ConnectionSortColumn column) =>
        column is ConnectionSortColumn.Upload or ConnectionSortColumn.Download or ConnectionSortColumn.Traffic or ConnectionSortColumn.Duration;

    private void ToggleConnectionSort(ConnectionSortColumn column)
    {
        var defaultDescending = IsDefaultDescending(column);
        if (_connSortColumn != column)
        {
            _connSortColumn = column;
            _connSortDescending = defaultDescending;
        }
        else if (_connSortDescending == defaultDescending)
        {
            _connSortDescending = !defaultDescending;
        }
        else
        {
            _connSortColumn = ConnectionSortColumn.None;
        }

        UpdateConnectionSortHeaders();
        RefreshConnections();
    }

    private void UpdateConnectionSortHeaders()
    {
        foreach (var (col, (btn, label, tb)) in _connSortHeaders)
        {
            if (col == _connSortColumn)
            {
                var defaultDescending = IsDefaultDescending(col);
                var arrow = _connSortDescending ? "↓" : "↑";
                tb.Text = $"{label} {arrow}";
                tb.Style = Resource<Style>("NetchAccentText");
                tb.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                var dirText = _connSortDescending ? T("descending", "降序") : T("ascending", "升序");
                var nextHint = _connSortDescending == defaultDescending
                    ? (defaultDescending ? T("click to sort ascending", "点击切换升序") : T("click to sort descending", "点击切换降序"))
                    : T("click to reset", "点击恢复默认排序");
                var tip = TF("{0}: {1} ({2})", "{0}：{1}（{2}）", label, dirText, nextHint);
                AutomationProperties.SetName(btn, tip);
                ToolTipService.SetToolTip(btn, tip);
            }
            else
            {
                tb.Text = label;
                tb.Style = Resource<Style>("NetchSecondaryText");
                tb.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
                var tip = TF("Sort by {0}", "点击按{0}排序", label);
                AutomationProperties.SetName(btn, tip);
                ToolTipService.SetToolTip(btn, tip);
            }
        }
    }

    private IEnumerable<ProcessConnectionSnapshot> SortConnections(IEnumerable<ProcessConnectionSnapshot> items)
    {
        return (_connSortColumn, _connSortDescending) switch
        {
            (ConnectionSortColumn.Process, true) => items
                .OrderByDescending(GetProcessSortKey, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(c => c.Pid)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Process, false) => items
                .OrderBy(GetProcessSortKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Pid)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Protocol, true) => items
                .OrderByDescending(c => c.Protocol, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Protocol, false) => items
                .OrderBy(c => c.Protocol, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Target, true) => items
                .OrderByDescending(c => c.Endpoint, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Target, false) => items
                .OrderBy(c => c.Endpoint, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Rule, true) => items
                .OrderByDescending(ConnectionRowVm.FormatRuleDisplay, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Rule, false) => items
                .OrderBy(ConnectionRowVm.FormatRuleDisplay, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Upload, true) => items
                .OrderByDescending(GetUploadSpeedSortKey)
                .ThenByDescending(c => c.IsActive)
                .ThenByDescending(c => c.DownSpeedBps)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Upload, false) => items
                .OrderBy(GetUploadSpeedSortKey)
                .ThenByDescending(c => c.IsActive)
                .ThenBy(c => c.DownSpeedBps)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Download, true) => items
                .OrderByDescending(GetDownloadSpeedSortKey)
                .ThenByDescending(c => c.IsActive)
                .ThenByDescending(c => c.UpSpeedBps)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Download, false) => items
                .OrderBy(GetDownloadSpeedSortKey)
                .ThenByDescending(c => c.IsActive)
                .ThenBy(c => c.UpSpeedBps)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Traffic, true) => items
                .OrderByDescending(c => c.UpBytes + c.DownBytes)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Traffic, false) => items
                .OrderBy(c => c.UpBytes + c.DownBytes)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Duration, true) => items
                .OrderByDescending(c => c.Duration)
                .ThenByDescending(c => c.Id),
            (ConnectionSortColumn.Duration, false) => items
                .OrderBy(c => c.Duration)
                .ThenByDescending(c => c.Id),
            _ => items.OrderByDescending(c => c.Id)
        };

        static string GetProcessSortKey(ProcessConnectionSnapshot c) =>
            string.IsNullOrEmpty(c.ProcessName) ? $"PID {c.Pid}" : c.ProcessName;

        static ulong GetUploadSpeedSortKey(ProcessConnectionSnapshot c) =>
            c.IsActive ? c.UpSpeedBps : 0UL;

        static ulong GetDownloadSpeedSortKey(ProcessConnectionSnapshot c) =>
            c.IsActive ? c.DownSpeedBps : 0UL;
    }

    private Grid BuildLogHeaderStrip() => new()
    {
        Margin = new Thickness(10, 0, 10 + ScrollBarGutter, 0),
        ColumnSpacing = 6,
        ColumnDefinitions =
        {
            new ColumnDefinition { Width = new GridLength(46) },
            new ColumnDefinition { Width = new GridLength(82) },
            new ColumnDefinition { Width = new GridLength(82) },
            new ColumnDefinition { Width = new GridLength(36) },
            new ColumnDefinition { Width = new GridLength(1.05, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }
        },
        Children =
        {
            HeaderCol("#", 0),
            HeaderCol(T("Time", "时间"), 1),
            HeaderCol(T("Action", "动作"), 2),
            HeaderCol(T("Proto", "协议"), 3),
            HeaderCol(T("Process (PID)", "进程 (PID)"), 4),
            HeaderCol(T("Target", "目标地址"), 5),
            HeaderCol(T("Rule", "规则"), 6)
        }
    };

    private static TextBlock HeaderCol(string label, int col, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var tb = Caption(label);
        tb.FontSize = 12;
        tb.HorizontalAlignment = align;
        Grid.SetColumn(tb, col);
        return tb;
    }

    private static DataTemplate BuildConnectionRowTemplate() => (DataTemplate)XamlReader.Load("""
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
            <Grid ColumnSpacing="6">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="1.15*" />
                    <ColumnDefinition Width="50" />
                    <ColumnDefinition Width="1.3*" />
                    <ColumnDefinition Width="0.95*" />
                    <ColumnDefinition Width="76" />
                    <ColumnDefinition Width="76" />
                    <ColumnDefinition Width="70" />
                    <ColumnDefinition Width="66" />
                </Grid.ColumnDefinitions>
                <Grid ColumnSpacing="4">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>
                    <TextBlock FontFamily="Consolas" FontSize="12" Text="{Binding ProcessTitle}" TextTrimming="CharacterEllipsis" />
                    <TextBlock Grid.Column="1" FontFamily="Consolas" FontSize="10.5" Foreground="{ThemeResource TextFillColorTertiaryBrush}" VerticalAlignment="Center" Text="{Binding PidSubtitle}" />
                </Grid>
                <TextBlock Grid.Column="1" FontFamily="Consolas" FontSize="12" Text="{Binding Protocol}" />
                <TextBlock Grid.Column="2" FontFamily="Consolas" FontSize="12" Text="{Binding Endpoint}" TextTrimming="CharacterEllipsis" />
                <TextBlock Grid.Column="3" FontFamily="Consolas" FontSize="11.5" Foreground="{ThemeResource TextFillColorSecondaryBrush}" Text="{Binding RuleDisplay}" TextTrimming="CharacterEllipsis" />
                <TextBlock Grid.Column="4" FontFamily="Consolas" FontSize="11.5" HorizontalAlignment="Right" Text="{Binding UploadRateDisplay}" TextTrimming="CharacterEllipsis" />
                <TextBlock Grid.Column="5" FontFamily="Consolas" FontSize="11.5" HorizontalAlignment="Right" Text="{Binding DownloadRateDisplay}" TextTrimming="CharacterEllipsis" />
                <TextBlock Grid.Column="6" FontFamily="Consolas" FontSize="11.5" HorizontalAlignment="Right" Text="{Binding TrafficDisplay}" />
                <TextBlock Grid.Column="7" FontFamily="Consolas" FontSize="11.5" HorizontalAlignment="Right" Foreground="{ThemeResource TextFillColorSecondaryBrush}" Text="{Binding DurationDisplay}" />
            </Grid>
        </DataTemplate>
        """);

    private static DataTemplate BuildLogRowTemplate() => (DataTemplate)XamlReader.Load("""
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
            <Grid ColumnSpacing="6">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="46" />
                    <ColumnDefinition Width="82" />
                    <ColumnDefinition Width="82" />
                    <ColumnDefinition Width="36" />
                    <ColumnDefinition Width="1.05*" />
                    <ColumnDefinition Width="1.25*" />
                    <ColumnDefinition Width="1.0*" />
                </Grid.ColumnDefinitions>
                <TextBlock FontFamily="Consolas" FontSize="11.5" Foreground="{ThemeResource TextFillColorTertiaryBrush}" Text="{Binding SeqText}" />
                <TextBlock Grid.Column="1" FontFamily="Consolas" FontSize="11.5" Foreground="{ThemeResource TextFillColorTertiaryBrush}" Text="{Binding TimeText}" />
                <TextBlock x:Name="ActionText" Grid.Column="2" FontFamily="Consolas" FontSize="11.5" FontWeight="SemiBold" Text="{Binding ActionText}" TextTrimming="CharacterEllipsis" />
                <TextBlock Grid.Column="3" FontFamily="Consolas" FontSize="11.5" Text="{Binding Protocol}" />
                <TextBlock Grid.Column="4" FontFamily="Consolas" FontSize="12" Text="{Binding ProcessDisplay}" TextTrimming="CharacterEllipsis" />
                <TextBlock Grid.Column="5" FontFamily="Consolas" FontSize="12" Text="{Binding Endpoint}" TextTrimming="CharacterEllipsis" />
                <Grid Grid.Column="6" ColumnSpacing="6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>
                    <TextBlock FontFamily="Consolas" FontSize="11.5" Foreground="{ThemeResource TextFillColorSecondaryBrush}" Text="{Binding RuleDisplay}" TextTrimming="CharacterEllipsis" />
                    <Border Grid.Column="1" Visibility="{Binding RepeatVisibility}" Background="{ThemeResource SubtleFillColorSecondaryBrush}" Padding="5,0" CornerRadius="4" VerticalAlignment="Center">
                        <TextBlock FontFamily="Consolas" FontSize="11" Text="{Binding RepeatText}" />
                    </Border>
                </Grid>
            </Grid>
        </DataTemplate>
        """);
}
