using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;
using Microsoft.Windows.Storage.Pickers;
using Netch.App.Dialogs;
using Netch.App.Helpers;
using Netch.App.Pages;
using Netch.App.Services;
using Netch.App.ViewModels;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
using Netch.Models.Modes.TunMode;
using Netch.Models.Modes.WebProxyMode;
using Netch.Services;
using Netch.Utils;
using Windows.System;
using Windows.UI;
using static Netch.App.Helpers.Ui;

namespace Netch.App;

internal sealed class WelcomeWindow : Window
{
    private const int WindowWidth = 700;
    private const int WindowHeight = 500;

    private enum ImportSourceTab
    {
        Subscription,
        ClashYaml,
        Clipboard,
        Manual
    }

    private enum StatusSeverity
    {
        None,
        Info,
        Success,
        Error
    }

    private enum AccelerateTarget
    {
        Process,
        WebProxy,
        Tun
    }

    private enum WizardWebProxyOption
    {
        PacProxyList,
        PacBypassChina,
        Global,
        Manual
    }

    private enum WizardTunOption
    {
        Global,
        BypassChina,
        BypassLan
    }

    private readonly Grid _root;
    private readonly HyperlinkButton _skip;
    private readonly ContentControl _steps = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly ContentControl _body = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    private readonly ContentControl _footer = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };

    // Persistent wizard state (plain C# values avoid reparenting UI controls across step rebuilds).
    private readonly HashSet<string> _programs = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Name, string Rule)> _customPrograms = [];
    private IReadOnlyList<(string Name, string Rule)>? _installed;
    private ImportSourceTab _sourceTab = ImportSourceTab.Subscription;
    private string _subscriptionUrl = "";
    private string _statusMessage = "";
    private StatusSeverity _statusSeverity = StatusSeverity.None;
    private Task<bool>? _subscriptionImport;
    private bool _importingFileOrClipboard;

    private AccelerateTarget _target = AccelerateTarget.Process;
    private WizardWebProxyOption _webProxyOption = WizardWebProxyOption.PacProxyList;
    private WizardTunOption _tunOption = WizardTunOption.Global;
    private string _programFilterText = "";
    private string _sceneName = "";
    private string _autoSceneName = "";
    private bool _nameCustomized;
    private bool _autoConnect;
    private Server? _server;
    private bool _nodeManuallySelected;
    private bool _nodesDirtyForDelayTest;
    private int _step;
    private bool _closed;

    // Step 1 active references
    private readonly Dictionary<ImportSourceTab, Button> _tabButtons = [];
    private ContentControl? _sourceContent;
    private TextBox? _linkBox;
    private Button? _importSubButton;
    private Border? _statusStrip;
    private FontIcon? _statusIcon;
    private TextBlock? _statusText;
    private Border? _readyPill;
    private TextBlock? _readyPillText;

    // Step 2 active references
    private Grid? _programsBody;
    private StackPanel? _programListPanel;
    private readonly Dictionary<string, CheckBox> _programCheckboxes = new(StringComparer.OrdinalIgnoreCase);
    private ScrollViewer? _chipsScroll;
    private StackPanel? _chipsPanel;

    // Step 3 active references
    private ComboBox? _nodeCombo;
    private TextBlock? _nodeFieldLabel;
    private Button? _retestButton;
    private TextBlock? _previewName;
    private TextBlock? _previewSummary;
    private TextBlock? _previewDelay;
    private bool _suppressNodeSelection;
    private bool _pendingNodeRefresh;

    public WelcomeWindow()
    {
        Title = T("Welcome to Netch", "欢迎使用 Netch");
        ExtendsContentIntoTitleBar = true;
        SystemBackdrop = new MicaBackdrop();
        var title = Text(Title);
        title.Margin = new Thickness(0);
        var drag = Row(new Image
        {
            Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/Netch.png")),
            Width = 18, Height = 18, Margin = new Thickness(15, 0, 0, 0)
        }, title);
        drag.Height = 42;
        _skip = new HyperlinkButton
        {
            Content = T("Skip wizard", "跳过向导"),
            Padding = new Thickness(6, 0, 6, 0),
            Margin = new Thickness(0, 0, 52, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _skip.Click += (_, _) => SkipAsync().Forget();
        var bar = Split(drag, _skip);
        _root = Body(bar, new Grid
        {
            Margin = new Thickness(24, 10, 24, 20),
            Children = { Body(_steps, PlaceBodyAndFooter()) }
        });
        if (!Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            _root.Style = Resource<Style>("NetchShellFallback");
        _root.RequestedTheme = App.MainWindow?.Theme ?? ElementTheme.Default;
        _root.ActualThemeChanged += (_, _) => UpdateCaptionButtons();
        Content = _root;
        SetTitleBar(drag);
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Netch.ico"));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }
        _root.Loaded += (_, _) =>
        {
            var dpi = _root.XamlRoot.RasterizationScale;
            WindowSize.Center(this, App.MainWindow!, (int)(WindowWidth * dpi), (int)(WindowHeight * dpi));
            UpdateCaptionButtons();
        };
        var dpi = Windows.Win32.PInvoke.GetDpiForWindow(new Windows.Win32.Foundation.HWND(Win32Interop.GetWindowFromWindowId(AppWindow.Id))) / 96.0;
        WindowSize.Center(this, App.MainWindow!, (int)(WindowWidth * dpi), (int)(WindowHeight * dpi));
        UpdateCaptionButtons();

        AppEvents.NodeDelaysChanged += OnNodeDelaysChanged;
        AppEvents.DelayTestProgressChanged += OnDelayTestProgressChanged;
        Closed += (_, _) =>
        {
            _closed = true;
            AppEvents.NodeDelaysChanged -= OnNodeDelaysChanged;
            AppEvents.DelayTestProgressChanged -= OnDelayTestProgressChanged;
        };

        ShowStep();
    }

    public void ApplyTheme(ElementTheme theme)
    {
        _root.RequestedTheme = theme;
        UpdateCaptionButtons();
    }

    private void UpdateCaptionButtons()
    {
        var dark = _root.ActualTheme == ElementTheme.Dark;
        var titleBar = AppWindow.TitleBar;
        titleBar.PreferredTheme = dark ? TitleBarTheme.Dark : TitleBarTheme.Light;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonForegroundColor = dark ? Colors.White : Color.FromArgb(0xE4, 0, 0, 0);
        titleBar.ButtonHoverForegroundColor = titleBar.ButtonForegroundColor;
        titleBar.ButtonPressedForegroundColor = titleBar.ButtonForegroundColor;
        titleBar.ButtonInactiveForegroundColor = dark ? Color.FromArgb(0x5D, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x5C, 0, 0, 0);
        titleBar.ButtonHoverBackgroundColor = dark ? Color.FromArgb(0x0F, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x09, 0, 0, 0);
        titleBar.ButtonPressedBackgroundColor = dark ? Color.FromArgb(0x0A, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x06, 0, 0, 0);
    }

    private Grid PlaceBodyAndFooter()
    {
        var layout = Body(_body, _footer);
        layout.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
        layout.RowDefinitions[1].Height = GridLength.Auto;
        _body.ContentTransitions = new TransitionCollection { new ContentThemeTransition() };
        return layout;
    }

    private void ClearStepReferences()
    {
        _tabButtons.Clear();
        _sourceContent = null;
        _linkBox = null;
        _importSubButton = null;
        _statusStrip = null;
        _statusIcon = null;
        _statusText = null;
        _readyPill = null;
        _readyPillText = null;

        _programsBody = null;
        _programListPanel = null;
        _programCheckboxes.Clear();
        _chipsScroll = null;
        _chipsPanel = null;

        _nodeCombo = null;
        _nodeFieldLabel = null;
        _retestButton = null;
        _previewName = null;
        _previewSummary = null;
        _previewDelay = null;
        _pendingNodeRefresh = false;
    }

    private void ShowStep()
    {
        ClearStepReferences();

        var steps = new Grid { Name = "WizardSteps", ColumnSpacing = 8 };
        var names = new[] { T("Nodes", "节点"), T("Accelerate", "加速"), T("Finish", "完成") };
        for (var i = 0; i < names.Length; i++)
        {
            FrameworkElement badge;
            if (i < _step)
            {
                var checkIcon = GlyphIcon(Icons.CheckMark, 12);
                checkIcon.Style = Resource<Style>("NetchStepDoneIcon");
                badge = checkIcon;
            }
            else
            {
                var number = Text((i + 1).ToString(), 12, true);
                if (i == _step)
                    number.Style = Resource<Style>("NetchStepLabel");
                badge = number;
            }
            badge.HorizontalAlignment = HorizontalAlignment.Center;
            badge.VerticalAlignment = VerticalAlignment.Center;
            var circle = new Border
            {
                Style = Resource<Style>(i < _step ? "NetchStepDone" : i == _step ? "NetchStepCurrent" : "NetchStep"),
                Width = 24, Height = 24, Child = badge
            };
            var label = Row(circle, Text(names[i], 14, i == _step, secondary: i > _step));
            label.Spacing = 8;

            if (i < _step)
            {
                var targetStep = i;
                var jump = new Button
                {
                    Content = label,
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(4, 2, 6, 2),
                    MinHeight = 0
                };
                AutomationProperties.SetName(jump, names[i]);
                jump.Click += (_, _) =>
                {
                    _step = targetStep;
                    ShowStep();
                };
                AddStep(jump, GridLength.Auto);
            }
            else
            {
                AddStep(label, GridLength.Auto);
            }

            if (i < names.Length - 1)
                AddStep(new Border
                {
                    Style = Resource<Style>("NetchDivider"), Height = 1, VerticalAlignment = VerticalAlignment.Center
                }, new GridLength(1, GridUnitType.Star));
        }
        _steps.Content = steps;

        void AddStep(FrameworkElement element, GridLength width)
        {
            Grid.SetColumn(element, steps.ColumnDefinitions.Count);
            steps.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
            steps.Children.Add(element);
        }

        _skip.Visibility = _step == 0 ? Visibility.Collapsed : Visibility.Visible;
        var back = _step == 0 ? AsyncButton(T("Later", "稍后再说"), SkipAsync)
            : Button(T("Back", "上一步"), () => { _step--; ShowStep(); });
        var next = AsyncButton(_step == 2 ? T("Create and connect", "创建并连接") : T("Next", "下一步"), NextAsync, true);
        back.Name = "WizardBack";
        next.Name = "WizardNext";
        back.Height = next.Height = 34;
        back.MinWidth = _step == 0 ? 94 : 82;
        next.MinWidth = _step == 2 ? 118 : 86;

        if (_step == 0)
        {
            _readyPillText = Text("", 12.5);
            _readyPill = new Border
            {
                Child = _readyPillText,
                Padding = new Thickness(10, 4, 10, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            UpdateReadyPill();
            var right = Row(_readyPill, next);
            right.Spacing = 10;
            _footer.Content = Split(back, right);
        }
        else
        {
            _footer.Content = Split(back, next);
        }

        _body.Content = _step switch
        {
            0 => ImportStep(),
            1 => ProgramsStep(),
            _ => ConfirmStep()
        };
    }

    private UIElement ImportStep()
    {
        var tabs = Row(
            SourceTabButton(ImportSourceTab.Subscription, Icons.Link, T("Subscription URL", "订阅链接")),
            SourceTabButton(ImportSourceTab.ClashYaml, Icons.OpenFile, T("Clash YAML file", "Clash YAML 文件")),
            SourceTabButton(ImportSourceTab.Clipboard, Icons.Paste, T("Clipboard", "剪贴板")),
            SourceTabButton(ImportSourceTab.Manual, Icons.Add, T("Add manually", "手动添加")));
        tabs.Name = "ImportSourceTabs";
        tabs.Spacing = 6;

        var divider = new Border
        {
            Style = Resource<Style>("NetchDivider"),
            Height = 1
        };

        _sourceContent = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };

        _statusIcon = GlyphIcon(Icons.Info, 13);
        _statusIcon.VerticalAlignment = VerticalAlignment.Center;
        _statusText = new TextBlock
        {
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        var statusGrid = new Grid { ColumnSpacing = 8 };
        statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statusGrid.Children.Add(_statusIcon);
        Grid.SetColumn(_statusText, 1);
        statusGrid.Children.Add(_statusText);

        _statusStrip = new Border
        {
            Name = "ImportStatus",
            Style = Resource<Style>("NetchWizardStatusStrip"),
            Child = statusGrid
        };

        RefreshImportTab();
        UpdateStatusStrip();

        var step = Stack(
            WizardTitle(T("Choose node source", "选择节点来源")),
            tabs,
            divider,
            _sourceContent,
            _statusStrip);
        step.Spacing = 12;
        return step;
    }

    private Button SourceTabButton(ImportSourceTab tab, string glyph, string label)
    {
        var button = new Button
        {
            Content = IconContent(glyph, label, 13, 6),
            Height = 33
        };
        AutomationProperties.SetName(button, label);
        button.Click += (_, _) =>
        {
            if (_sourceTab == tab)
                return;
            _sourceTab = tab;
            RefreshImportTab();
        };
        _tabButtons[tab] = button;
        return button;
    }

    private void RefreshImportTab()
    {
        foreach (var (tab, button) in _tabButtons)
            button.Style = Resource<Style>(tab == _sourceTab ? "NetchWizardTabActive" : "NetchWizardTab");

        if (_sourceContent == null)
            return;

        _linkBox = null;
        _importSubButton = null;
        _sourceContent.Content = _sourceTab switch
        {
            ImportSourceTab.Subscription => BuildSubscriptionTab(),
            ImportSourceTab.ClashYaml => BuildClashYamlTab(),
            ImportSourceTab.Clipboard => BuildClipboardTab(),
            _ => BuildManualTab()
        };
    }

    private UIElement BuildSubscriptionTab()
    {
        var label = T("HTTP or HTTPS subscription URL", "HTTP / HTTPS 订阅地址");
        _linkBox = new TextBox
        {
            Name = "SubscriptionLink",
            PlaceholderText = "https://...",
            Text = _subscriptionUrl,
            MinHeight = 36,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(_linkBox, label);
        _linkBox.TextChanged += (_, _) => _subscriptionUrl = _linkBox.Text;
        _linkBox.KeyDown += LinkKeyDown;

        var importBusy = _subscriptionImport is { IsCompleted: false };
        _linkBox.IsEnabled = !importBusy;
        _importSubButton = new Button
        {
            Content = IconContent(Icons.Download, T("Import", "导入")),
            Height = 36,
            MinWidth = 88,
            IsEnabled = !importBusy
        };
        AutomationProperties.SetName(_importSubButton, T("Import", "导入"));
        _importSubButton.Click += (_, _) => ImportSubscriptionAsync().Forget();

        var inputRow = Split(_linkBox, _importSubButton, 8);
        var hint = Caption(T("Creates a subscription group updated every 6 hours.", "自动创建订阅分组，默认每 6 小时更新。"));
        hint.TextWrapping = TextWrapping.Wrap;

        var panel = Stack(WizardField(label, inputRow), hint);
        panel.Spacing = 8;
        return panel;
    }

    private UIElement BuildClashYamlTab()
    {
        var title = Text(T("Import from a local Clash file", "从本地文件导入 Clash 配置"), 14.5, strong: true);
        var desc = Caption(T("Supports .yaml, .yml, and .txt files. Nodes are grouped by file name.",
            "支持 .yaml / .yml / .txt，自动按文件名创建本地分组。"));
        desc.TextWrapping = TextWrapping.Wrap;

        var info = Stack(title, desc);
        info.Spacing = 4;

        var choose = AsyncButton(T("Choose file…", "选择文件…"), ImportFileAsync, glyph: Icons.OpenFile);
        choose.Height = 36;
        choose.MinWidth = 116;
        choose.VerticalAlignment = VerticalAlignment.Center;

        return Card(Split(info, choose, 14), 16);
    }

    private UIElement BuildClipboardTab()
    {
        var title = Text(T("Read nodes from clipboard", "读取剪贴板中的节点"), 14.5, strong: true);
        var desc = Caption(T("Detects share links, Base64 subscriptions, and Clash YAML.",
            "自动识别分享链接、Base64 列表或 Clash YAML 片段。"));
        desc.TextWrapping = TextWrapping.Wrap;

        var info = Stack(title, desc);
        info.Spacing = 4;

        var paste = AsyncButton(T("Read and import", "读取并导入"), ImportClipboardAsync, glyph: Icons.Paste);
        paste.Height = 36;
        paste.MinWidth = 120;
        paste.VerticalAlignment = VerticalAlignment.Center;

        return Card(Split(info, paste, 14), 16);
    }

    private UIElement BuildManualTab()
    {
        var (featured, moreProtocols) = ServerEditorHelper.GetWizardManualProtocols();
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        for (var c = 0; c < 3; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var showMore = moreProtocols.Count > 0;
        var totalSlots = featured.Count + (showMore ? 1 : 0);
        var rows = (totalSlots + 2) / 3;
        for (var r = 0; r < rows; r++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var i = 0; i < featured.Count; i++)
        {
            var util = featured[i];
            var displayName = ServerHelper.GetDisplayName(util);
            var button = AsyncButton(displayName, () => EditNewNodeAsync(util.TypeName), glyph: Icons.Add);
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Height = 36;
            Grid.SetRow(button, i / 3);
            Grid.SetColumn(button, i % 3);
            grid.Children.Add(button);
        }

        if (showMore)
        {
            var moreLabel = T("More protocols", "更多协议");
            var more = new Button
            {
                Content = IconContent(Icons.More, moreLabel),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 36,
                Flyout = Menu(moreProtocols.Select(util =>
                    (ServerHelper.GetDisplayName(util), (Action)(() => EditNewNodeAsync(util.TypeName).Forget()))).ToArray())
            };
            AutomationProperties.SetName(more, moreLabel);
            var moreIndex = featured.Count;
            Grid.SetRow(more, moreIndex / 3);
            Grid.SetColumn(more, moreIndex % 3);
            grid.Children.Add(more);
        }

        return grid;
    }

    private async void LinkKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.Enter)
        {
            args.Handled = true;
            await ImportSubscriptionAsync();
        }
    }

    private async Task EditNewNodeAsync(string typeName)
    {
        var before = Global.Settings.Server.Count;
        if (!await NodeEditor.EditAsync(_root.XamlRoot, null, typeName, AppWindow.Id))
            return;

        _nodesDirtyForDelayTest = true;
        AppEvents.RaiseSubscriptionChanged();
        if (_closed)
            return;

        var added = Global.Settings.Server.Count > before ? Global.Settings.Server[^1] : null;
        SetStatus(StatusSeverity.Success, added != null
            ? TF("Added {0} ({1}).", "已添加 {0}（{1}）。", Format.NodeName(added), ServerHelper.GetDisplayName(added.ConfigType))
            : TF("Added {0} node.", "已添加 {0} 个节点。", 1));
    }

    private Task ImportFileAsync() => ImportFromTextSourceAsync(async () =>
    {
        var picker = new FileOpenPicker(AppWindow.Id);
        picker.FileTypeFilter.Add(".yaml");
        picker.FileTypeFilter.Add(".yml");
        picker.FileTypeFilter.Add(".txt");
        if (await picker.PickSingleFileAsync() is not { } file)
            return null;

        return (await File.ReadAllTextAsync(file.Path), file.Path);
    }, isFileImport: true, "Import nodes from file");

    private Task ImportClipboardAsync() => ImportFromTextSourceAsync(async () =>
        (await ClipboardTextAsync(), null), isFileImport: false, "Import nodes from clipboard");

    private async Task ImportFromTextSourceAsync(Func<Task<(string Text, string? FilePath)?>> readInput, bool isFileImport, string operation)
    {
        if (_importingFileOrClipboard)
            return;

        _importingFileOrClipboard = true;
        try
        {
            if (await readInput() is not { } input)
                return;

            var (count, skipped, group) = await NodeEditor.ImportTextAsync(input.Text, input.FilePath, _closed ? null : _root.XamlRoot);
            if (count == 0)
            {
                if (_closed)
                    return;
                var msg = T("No supported nodes were found.", "未找到可用节点。");
                if (skipped > 0)
                    msg += " " + i18N.TranslateFormat("Skipped {0} invalid or unsupported nodes.", skipped);
                SetStatus(StatusSeverity.Error, msg);
                return;
            }

            _nodesDirtyForDelayTest = true;
            var summary = FormatProtocolSummary(Global.Settings.Server.TakeLast(count));
            AppEvents.RaiseSubscriptionChanged();
            if (_closed)
                return;

            string message;
            if (isFileImport)
            {
                message = summary.Length > 0
                    ? TF("Imported {0} nodes from {1} ({2}).", "已从 {1} 导入 {0} 个节点（{2}）。", count, group, summary)
                    : TF("Imported {0} nodes ({1})", "已识别 {0} 个节点（{1}）", count, group);
            }
            else
            {
                var groupLabel = NodeGroupHelper.IsManualGroup(group) ? T("Added manually", "手动添加") : group;
                message = summary.Length > 0
                    ? TF("Imported {0} nodes ({1}: {2}).", "已导入 {0} 个节点（{1}：{2}）。", count, groupLabel, summary)
                    : TF("Imported {0} nodes.", "已导入 {0} 个节点。", count);
            }

            if (skipped > 0)
                message += " " + i18N.TranslateFormat("Skipped {0} invalid or unsupported nodes.", skipped);
            SetStatus(StatusSeverity.Success, message);
        }
        catch (Exception e)
        {
            Log.Warning(e, "{Operation} failed", operation);
            if (!_closed)
                SetStatus(StatusSeverity.Error, e.Message);
        }
        finally
        {
            _importingFileOrClipboard = false;
        }
    }

    private static string FormatProtocolSummary(IEnumerable<Server> servers)
    {
        return string.Join(" · ", servers
            .GroupBy(s => s.ConfigType)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => ServerHelper.GetDisplayName(g.Key), StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(g => $"{ServerHelper.GetDisplayName(g.Key)} {g.Count()}"));
    }

    private void SetStatus(StatusSeverity severity, string message)
    {
        _statusSeverity = severity;
        _statusMessage = message;
        UpdateStatusStrip();
        UpdateReadyPill();
    }

    private void UpdateStatusStrip()
    {
        if (_statusStrip == null || _statusIcon == null || _statusText == null)
            return;

        if (_statusSeverity == StatusSeverity.None || string.IsNullOrWhiteSpace(_statusMessage))
        {
            _statusStrip.Visibility = Visibility.Collapsed;
            return;
        }

        _statusStrip.Visibility = Visibility.Visible;
        _statusText.Text = _statusMessage;
        string iconStyle;
        string textStyle;
        (_statusIcon.Glyph, iconStyle, textStyle) = _statusSeverity switch
        {
            StatusSeverity.Success => (Icons.CheckMark, "NetchSuccessIcon", "NetchSuccessText"),
            StatusSeverity.Error => (Icons.Warning, "NetchErrorIcon", "NetchErrorText"),
            _ => (Icons.Info, "NetchSecondaryIcon", "NetchSecondaryText")
        };
        _statusIcon.Style = Resource<Style>(iconStyle);
        _statusText.Style = Resource<Style>(textStyle);
    }

    private void UpdateReadyPill()
    {
        if (_readyPill == null || _readyPillText == null)
            return;

        var total = Global.Settings.Server.Count;
        if (total > 0)
        {
            _readyPill.Style = Resource<Style>("NetchUpdatedPill");
            _readyPillText.Style = Resource<Style>("NetchAccentText");
            _readyPillText.Text = TF("Ready · {0} nodes", "已就绪 · {0} 个节点", total);
        }
        else
        {
            _readyPill.Style = Resource<Style>("NetchPill");
            _readyPillText.Style = Resource<Style>("NetchSecondaryText");
            _readyPillText.Text = T("No nodes yet", "尚未导入节点");
        }
    }

    private Task<bool> ImportSubscriptionAsync() => _subscriptionImport is { IsCompleted: false } pending
        ? pending : _subscriptionImport = ImportSubscriptionCoreAsync();

    private async Task<bool> ImportSubscriptionCoreAsync()
    {
        if (!Uri.TryCreate(_subscriptionUrl.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            SetStatus(StatusSeverity.Error, T("Enter an HTTP or HTTPS subscription URL.", "请输入 HTTP 或 HTTPS 订阅链接。"));
            return false;
        }

        if (_linkBox != null)
            _linkBox.IsEnabled = false;
        if (_importSubButton != null)
            _importSubButton.IsEnabled = false;
        SetStatus(StatusSeverity.Info, T("Importing subscription…", "正在导入订阅…"));
        Subscription? subscription = null;
        var isNew = false;
        try
        {
            subscription = Global.Settings.Subscription.FirstOrDefault(s => s.Link == uri.AbsoluteUri);
            if (subscription == null)
            {
                var name = NodeGroupHelper.MakeUniqueGroupName(uri.Host);
                subscription = new Subscription { Link = uri.AbsoluteUri, Remark = name, UpdateIntervalHours = 6 };
                Global.Settings.Subscription.Add(subscription);
                isNew = true;
            }
            var imported = await SubscriptionUtil.UpdateServerAsync(subscription);
            if (isNew && imported == 0)
            {
                Global.Settings.Subscription.Remove(subscription);
                if (!_closed)
                    SetStatus(StatusSeverity.Error, T("No supported nodes were found.", "未找到可用节点。"));
                return false;
            }
            if (imported == 0)
            {
                if (!_closed)
                    SetStatus(StatusSeverity.Error, T("No supported nodes were found.", "未找到可用节点。"));
                return false;
            }

            _nodesDirtyForDelayTest = true;
            var subServers = Global.Settings.Server
                .Where(s => s.IsSub && NodeGroupHelper.GroupEquals(s.Group, subscription.Remark))
                .ToArray();
            if (isNew && !_closed)
                await NodeEditor.PromptImportCustomCoresAsync(_root.XamlRoot, subServers, subscription);
            var summary = FormatProtocolSummary(subServers);
            AppEvents.RaiseSubscriptionChanged();
            if (!_closed)
            {
                SetStatus(StatusSeverity.Success, summary.Length > 0
                    ? TF("Imported {0} nodes ({1}: {2}).", "已导入 {0} 个节点（{1}：{2}）。", imported, subscription.Remark, summary)
                    : TF("Imported {0} nodes ({1})", "已识别 {0} 个节点（{1}）", imported, subscription.Remark));
            }
            await Configuration.SaveAsync();
            return true;
        }
        catch (Exception e)
        {
            if (isNew && subscription != null)
                Global.Settings.Subscription.Remove(subscription);
            Log.Warning(e, "Import subscription failed");
            if (!_closed)
                SetStatus(StatusSeverity.Error, e.Message);
            return false;
        }
        finally
        {
            if (_linkBox != null)
                _linkBox.IsEnabled = true;
            if (_importSubButton != null)
                _importSubButton.IsEnabled = true;
        }
    }

    private static bool HasImportedSubscription(string link)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri))
            return false;
        var subscription = Global.Settings.Subscription.FirstOrDefault(s => s.Link == uri.AbsoluteUri);
        return subscription != null && Global.Settings.Server.Any(server => server.IsSub && NodeGroupHelper.GroupEquals(server.Group, subscription.Remark));
    }

    private UIElement ProgramsStep()
    {
        Button? procCard = null;
        Button? webCard = null;
        Button? tunCard = null;

        void SelectMode(AccelerateTarget target)
        {
            if (_target == target)
                return;
            _target = target;
            if (procCard != null)
                procCard.Style = Resource<Style>(_target == AccelerateTarget.Process ? "NetchWizardOptionCardActive" : "NetchWizardOptionCard");
            if (webCard != null)
                webCard.Style = Resource<Style>(_target == AccelerateTarget.WebProxy ? "NetchWizardOptionCardActive" : "NetchWizardOptionCard");
            if (tunCard != null)
                tunCard.Style = Resource<Style>(_target == AccelerateTarget.Tun ? "NetchWizardOptionCardActive" : "NetchWizardOptionCard");
            PopulateProgramsBody();
        }

        procCard = BuildModeCard(
            active: _target == AccelerateTarget.Process,
            glyph: Icons.Game,
            title: T("Specific programs", "指定程序"),
            subtitle: T("Process mode", "按进程分流"),
            onClick: () => SelectMode(AccelerateTarget.Process));

        webCard = BuildModeCard(
            active: _target == AccelerateTarget.WebProxy,
            glyph: Icons.Globe,
            title: T("Web proxy", "网页代理"),
            subtitle: T("PAC / System proxy", "PAC / 系统代理"),
            onClick: () => SelectMode(AccelerateTarget.WebProxy));

        tunCard = BuildModeCard(
            active: _target == AccelerateTarget.Tun,
            glyph: Icons.NetworkPort,
            title: T("All traffic (TUN)", "全部流量（TUN）"),
            subtitle: T("Virtual network adapter", "虚拟网卡接管"),
            onClick: () => SelectMode(AccelerateTarget.Tun));

        var modeSelector = new Grid { ColumnSpacing = 10 };
        for (var i = 0; i < 3; i++)
            modeSelector.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        modeSelector.Children.Add(procCard);
        Grid.SetColumn(webCard, 1);
        modeSelector.Children.Add(webCard);
        Grid.SetColumn(tunCard, 2);
        modeSelector.Children.Add(tunCard);

        _programsBody = new Grid();
        PopulateProgramsBody();

        var layout = Body(WizardTitle(T("Choose what to accelerate", "选择加速对象")), modeSelector, _programsBody);
        layout.RowSpacing = 11;
        return layout;
    }

    private Button BuildModeCard(bool active, string glyph, string title, string subtitle, Action onClick)
    {
        var icon = GlyphIcon(glyph, 16);
        icon.VerticalAlignment = VerticalAlignment.Center;
        var titleBlock = Text(title, 14, strong: true);
        titleBlock.TextTrimming = TextTrimming.CharacterEllipsis;
        var subtitleBlock = Caption(subtitle);
        subtitleBlock.TextTrimming = TextTrimming.CharacterEllipsis;
        var textStack = Stack(titleBlock, subtitleBlock);
        textStack.Spacing = 2;
        var content = TwoColumns(icon, textStack, GridLength.Auto, 10);

        var button = new Button
        {
            Content = content,
            Style = Resource<Style>(active ? "NetchWizardOptionCardActive" : "NetchWizardOptionCard")
        };
        AutomationProperties.SetName(button, $"{title} · {subtitle}");
        button.Click += (_, _) => onClick();
        return button;
    }

    private string WebProxyOptionLabel(WizardWebProxyOption option) => option switch
    {
        WizardWebProxyOption.PacProxyList => T("PAC Web Proxy (Proxy List)", "PAC 网页代理（海外列表）"),
        WizardWebProxyOption.PacBypassChina => T("PAC Web Proxy (Bypass China)", "PAC 网页代理（绕过中国大陆）"),
        WizardWebProxyOption.Global => T("Global Web Proxy", "全局网页代理"),
        _ => T("Local Proxy Only (Manual)", "仅本地代理（手动）")
    };

    private string TunOptionLabel(WizardTunOption option) => option switch
    {
        WizardTunOption.Global => T("Global TUN", "全局 TUN"),
        WizardTunOption.BypassChina => T("TUN (Bypass LAN & China)", "TUN（绕过局域网与中国大陆）"),
        _ => T("TUN (Bypass LAN)", "TUN（仅绕过局域网）")
    };

    private static TunMode? FindBuiltInTunMode(string modeId) =>
        Global.Modes.OfType<TunMode>().FirstOrDefault(m =>
            string.Equals(SceneService.GetModeId(m), modeId, StringComparison.OrdinalIgnoreCase));

    private static WebProxyMode BuildWizardWebProxyMode(WizardWebProxyOption option) => option switch
    {
        WizardWebProxyOption.PacProxyList => new WebProxyMode { Strategy = WebProxyStrategy.Pac, PacPreset = PacRulePreset.ProxyList, BypassLan = true },
        WizardWebProxyOption.PacBypassChina => new WebProxyMode { Strategy = WebProxyStrategy.Pac, PacPreset = PacRulePreset.BypassChina, BypassLan = true },
        WizardWebProxyOption.Global => new WebProxyMode { Strategy = WebProxyStrategy.Global, BypassLan = true },
        _ => new WebProxyMode { Strategy = WebProxyStrategy.Manual, BypassLan = true }
    };

    private void PopulateProgramsBody()
    {
        if (_programsBody == null)
            return;

        _programsBody.Children.Clear();
        _programsBody.RowDefinitions.Clear();
        _programCheckboxes.Clear();

        if (_target == AccelerateTarget.WebProxy)
        {
            var badge = new Border
            {
                Style = Resource<Style>("NetchWizardPreviewBadge"),
                Child = GlyphIcon(Icons.Globe, 18)
            };
            if (badge.Child is FontIcon icon)
                icon.Style = Resource<Style>("NetchWizardPreviewIcon");

            var title = Text(T("Proxy browser and app HTTP/HTTPS traffic", "代理浏览器与应用网页流量"), 14.5, strong: true);
            var desc = Caption(T("Configures Windows system proxy or PAC routing without a virtual adapter.",
                "通过 Windows 系统代理或 PAC 分流网页流量，无需虚拟网卡驱动。"));
            desc.TextWrapping = TextWrapping.Wrap;

            var strategyBox = Choice(T("Proxy strategy", "代理策略"),
                [
                    WebProxyOptionLabel(WizardWebProxyOption.PacProxyList),
                    WebProxyOptionLabel(WizardWebProxyOption.PacBypassChina),
                    WebProxyOptionLabel(WizardWebProxyOption.Global),
                    WebProxyOptionLabel(WizardWebProxyOption.Manual)
                ],
                (int)_webProxyOption);
            strategyBox.Header = T("Proxy strategy", "代理策略");
            strategyBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            strategyBox.SelectionChanged += (_, _) =>
            {
                if (strategyBox.SelectedIndex >= 0)
                    _webProxyOption = (WizardWebProxyOption)strategyBox.SelectedIndex;
            };

            var row = TwoColumns(badge, Stack(title, desc), GridLength.Auto, 12);
            var content = Stack(row, strategyBox);
            content.Spacing = 12;
            var card = Card(content, 16);
            card.VerticalAlignment = VerticalAlignment.Top;
            _programsBody.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _programsBody.Children.Add(card);
            return;
        }

        if (_target == AccelerateTarget.Tun)
        {
            var badge = new Border
            {
                Style = Resource<Style>("NetchWizardPreviewBadge"),
                Child = GlyphIcon(Icons.NetworkPort, 18)
            };
            if (badge.Child is FontIcon icon)
                icon.Style = Resource<Style>("NetchWizardPreviewIcon");

            var title = Text(T("Route all device traffic through TUN", "通过虚拟网卡（TUN）接管全部流量"), 14.5, strong: true);
            var desc = Caption(T("Choose whether to proxy all traffic or bypass LAN and China routes.",
                "可选择全部流量走代理，或绕过局域网与中国大陆 IP 路由。"));
            desc.TextWrapping = TextWrapping.Wrap;

            var routingBox = Choice(T("Routing mode", "路由模式"),
                [
                    TunOptionLabel(WizardTunOption.Global),
                    TunOptionLabel(WizardTunOption.BypassChina),
                    TunOptionLabel(WizardTunOption.BypassLan)
                ],
                (int)_tunOption);
            routingBox.Header = T("Routing mode", "路由模式");
            routingBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            routingBox.SelectionChanged += (_, _) =>
            {
                if (routingBox.SelectedIndex >= 0)
                    _tunOption = (WizardTunOption)routingBox.SelectedIndex;
            };

            var row = TwoColumns(badge, Stack(title, desc), GridLength.Auto, 12);
            var content = Stack(row, routingBox);
            content.Spacing = 12;
            var card = Card(content, 16);
            card.VerticalAlignment = VerticalAlignment.Top;
            _programsBody.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _programsBody.Children.Add(card);
            return;
        }

        var search = Input(T("Filter programs…", "搜索程序名…"), _programFilterText, search: true);
        search.MinHeight = 34;
        search.VerticalContentAlignment = VerticalAlignment.Center;
        search.TextChanged += (_, _) =>
        {
            _programFilterText = search.Text;
            RefreshProgramList();
        };

        var browse = AsyncButton(T("Browse .exe…", "浏览 .exe…"), BrowseExecutablesAsync, glyph: Icons.FolderOpen);
        browse.Height = 34;
        var toolbar = Split(search, browse, 8);

        _programListPanel = new StackPanel { Spacing = 2 };
        var programsScroll = Ui.Scroll(_programListPanel);
        var cardList = Card(programsScroll, 6);
        cardList.Name = "WizardPrograms";
        cardList.VerticalAlignment = VerticalAlignment.Stretch;

        _chipsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(0, 0, 0, Ui.ScrollBarGutter)
        };
        _chipsScroll = new ScrollViewer
        {
            Content = _chipsPanel,
            HorizontalScrollMode = ScrollMode.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollMode = ScrollMode.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        _programsBody.RowSpacing = 8;
        _programsBody.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _programsBody.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _programsBody.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _programsBody.Children.Add(toolbar);
        Grid.SetRow(cardList, 1);
        _programsBody.Children.Add(cardList);
        Grid.SetRow(_chipsScroll, 2);
        _programsBody.Children.Add(_chipsScroll);

        RefreshProgramList();
        RefreshProgramChips();
    }

    private IEnumerable<(string Name, string Rule)> AllProgramEntries() =>
        _customPrograms.Concat(_installed ?? []);

    private string DisplayNameForRule(string rule)
    {
        foreach (var entry in AllProgramEntries())
            if (entry.Rule.Equals(rule, StringComparison.OrdinalIgnoreCase))
                return entry.Name;
        return rule;
    }

    private void RefreshProgramList()
    {
        if (_programListPanel == null)
            return;

        _programListPanel.Children.Clear();
        _programCheckboxes.Clear();

        var filter = _programFilterText.Trim();
        var entries = AllProgramEntries()
            .Where(entry => filter.Length == 0 || entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                            entry.Rule.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (entries.Length == 0)
        {
            var empty = Caption(T("No matching programs", "没有匹配的程序"));
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            empty.Margin = new Thickness(0, 24, 0, 24);
            _programListPanel.Children.Add(empty);
            return;
        }

        foreach (var (name, rule) in entries)
        {
            var check = new CheckBox
            {
                Content = IconContent(Icons.Game, name, 14),
                IsChecked = _programs.Contains(rule),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(8, 4, 8, 4)
            };
            AutomationProperties.SetName(check, name);
            check.Checked += (_, _) =>
            {
                _programs.Add(rule);
                RefreshProgramChips();
            };
            check.Unchecked += (_, _) =>
            {
                _programs.Remove(rule);
                RefreshProgramChips();
            };
            _programCheckboxes[rule] = check;
            _programListPanel.Children.Add(check);
        }
    }

    private void RefreshProgramChips()
    {
        if (_chipsScroll == null || _chipsPanel == null)
            return;

        _chipsPanel.Children.Clear();
        if (_programs.Count == 0)
        {
            _chipsScroll.Visibility = Visibility.Collapsed;
            return;
        }

        _chipsScroll.Visibility = Visibility.Visible;
        var ordered = AllProgramEntries().Where(e => _programs.Contains(e.Rule)).ToList();
        var seen = new HashSet<string>(ordered.Select(e => e.Rule), StringComparer.OrdinalIgnoreCase);
        foreach (var extraRule in _programs)
            if (seen.Add(extraRule))
                ordered.Add((extraRule, extraRule));

        foreach (var (name, rule) in ordered)
        {
            var label = Text(name, 12);
            label.Style = Resource<Style>("NetchAccentText");

            var removeBtn = new Button
            {
                Content = GlyphIcon(Icons.Cancel, 10),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4, 2, 4, 2),
                MinWidth = 0,
                MinHeight = 0,
                VerticalAlignment = VerticalAlignment.Center
            };
            var accessible = TF("Remove {0}", "移除 {0}", name);
            AutomationProperties.SetName(removeBtn, accessible);
            ToolTipService.SetToolTip(removeBtn, accessible);
            removeBtn.Click += (_, _) =>
            {
                _programs.Remove(rule);
                if (_programCheckboxes.TryGetValue(rule, out var cb))
                    cb.IsChecked = false;
                RefreshProgramChips();
            };

            var row = Row(label, removeBtn);
            row.Spacing = 2;
            _chipsPanel.Children.Add(new Border
            {
                Style = Resource<Style>("NetchWizardChip"),
                Child = row
            });
        }
    }

    private async Task BrowseExecutablesAsync()
    {
        var picker = new FileOpenPicker(AppWindow.Id);
        picker.FileTypeFilter.Add(".exe");
        var files = await picker.PickMultipleFilesAsync();
        if (_closed || files == null || files.Count == 0)
            return;

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file.Path);
            if (string.IsNullOrWhiteSpace(fileName))
                continue;
            var rule = fileName.ToRegexString();
            var displayName = Path.GetFileNameWithoutExtension(file.Path);
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = fileName;

            if (!AllProgramEntries().Any(e => e.Rule.Equals(rule, StringComparison.OrdinalIgnoreCase)))
                _customPrograms.Add((displayName, rule));
            _programs.Add(rule);
        }

        RefreshProgramList();
        RefreshProgramChips();
    }

    private UIElement ConfirmStep()
    {
        var nodes = GetOrderedServers();
        ResolveSelectedServer(nodes);

        var badgeIcon = GlyphIcon(SceneIcon.ToGlyph(_target == AccelerateTarget.Process ? SceneIcon.Gamepad : SceneIcon.Globe), 18);
        badgeIcon.Style = Resource<Style>("NetchWizardPreviewIcon");
        var badge = new Border
        {
            Style = Resource<Style>("NetchWizardPreviewBadge"),
            Child = badgeIcon
        };

        _previewName = Text("", 15, strong: true);
        _previewSummary = Caption("");
        _previewDelay = Caption("");
        _previewDelay.FontFamily = new FontFamily("Consolas");
        _previewDelay.VerticalAlignment = VerticalAlignment.Center;

        var previewMain = TwoColumns(badge, Stack(_previewName, _previewSummary), GridLength.Auto, 12);
        var previewCard = new Border
        {
            Style = Resource<Style>("NetchWizardPreviewCard"),
            Child = Split(previewMain, _previewDelay, 12)
        };

        var nameBox = Input(T("Name", "名称"), _sceneName, search: true);
        nameBox.Name = "WizardSceneName";
        nameBox.MinHeight = 36;
        nameBox.VerticalContentAlignment = VerticalAlignment.Center;
        nameBox.TextChanged += (_, _) =>
        {
            _sceneName = nameBox.Text;
            _nameCustomized = !string.IsNullOrWhiteSpace(_sceneName) &&
                              !string.Equals(_sceneName.Trim(), _autoSceneName, StringComparison.Ordinal);
            UpdatePreview();
        };

        _nodeFieldLabel = Text("", 14.5, secondary: true);
        _nodeFieldLabel.Height = 20;

        _nodeCombo = new ComboBox
        {
            Name = "WizardNode",
            Height = 38,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemTemplate = Resource<DataTemplate>("NetchWizardNodeTemplate"),
            IsTextSearchEnabled = true
        };
        _nodeCombo.SelectionChanged += (_, _) =>
        {
            if (_suppressNodeSelection)
                return;
            if (_nodeCombo.SelectedItem is NodeOption option)
            {
                _server = option.Server;
                _nodeManuallySelected = true;
                UpdatePreview();
            }
        };
        _nodeCombo.DropDownClosed += (_, _) =>
        {
            if (_pendingNodeRefresh)
            {
                _pendingNodeRefresh = false;
                RefreshConfirmNodes();
            }
        };

        _retestButton = Button("", () =>
        {
            if (DelayTestHelper.IsTesting)
                DelayTestHelper.Cancel();
            else
                DelayTestHelper.PerformTestAsync(true).Forget();
        }, glyph: Icons.Flash);
        _retestButton.Height = 38;

        var nodeRow = Split(_nodeCombo, _retestButton, 8);
        var nodeField = Stack(_nodeFieldLabel, nodeRow);
        nodeField.Spacing = 5;

        var startupLabel = T("Connect this scene after sign-in", "开机后自动连接此场景");
        var autoToggle = CompactToggle(new ToggleSwitch(), 26);
        autoToggle.IsOn = _autoConnect;
        AutomationProperties.SetName(autoToggle, startupLabel);
        autoToggle.Toggled += (_, _) => _autoConnect = autoToggle.IsOn;

        var autoRow = new Border
        {
            Name = "WizardAutostart",
            Style = Resource<Style>("NetchWizardToggleCard"),
            Child = Split(Text(startupLabel), autoToggle)
        };

        var nameField = WizardField(T("Name", "名称"), nameBox);
        nameField.Spacing = 5;

        RefreshConfirmNodes();
        UpdateConfirmTestState(DelayTestHelper.IsTesting, DelayTestHelper.CurrentCompleted, DelayTestHelper.CurrentTotal);

        var step = Stack(WizardTitle(T("Confirm your first scene", "确认你的第一个场景")),
            previewCard, nameField, nodeField, autoRow);
        step.Spacing = 12;
        return step;
    }

    private static Server[] GetOrderedServers() =>
        Global.Settings.Server.OrderBy(server => DelayStatus.IsValid(server.Delay) ? server.Delay : int.MaxValue).ToArray();

    private void ResolveSelectedServer(Server[] nodes)
    {
        if (nodes.Length == 0)
        {
            _server = null;
            return;
        }

        if (_nodeManuallySelected && _server != null)
        {
            _server = nodes.FirstOrDefault(s => s.Id == _server.Id) ?? nodes[0];
            return;
        }

        _server = nodes[0];
    }

    private void RefreshConfirmNodes()
    {
        if (_nodeCombo == null)
            return;

        if (_nodeCombo.IsDropDownOpen)
        {
            _pendingNodeRefresh = true;
            UpdatePreview();
            return;
        }

        var nodes = GetOrderedServers();
        ResolveSelectedServer(nodes);

        _suppressNodeSelection = true;
        try
        {
            _nodeCombo.ItemsSource = nodes.Select(server => new NodeOption(server)).ToArray();
            _nodeCombo.SelectedIndex = nodes.Length == 0
                ? -1
                : Math.Max(0, Array.FindIndex(nodes, server => server.Id == _server?.Id));
        }
        finally
        {
            _suppressNodeSelection = false;
        }

        UpdatePreview();
    }

    private void UpdateConfirmTestState(bool isTesting, int completed, int total)
    {
        if (_nodeFieldLabel != null)
        {
            var label = isTesting
                ? (total > 0
                    ? TF("Node (testing latency… {0} / {1})", "节点（正在测速… {0} / {1}）", completed, total)
                    : T("Testing latency…", "正在测速…"))
                : T("Node (ordered by latency)", "节点（已按延迟推荐）");
            _nodeFieldLabel.Text = label;
            if (_nodeCombo != null)
                AutomationProperties.SetName(_nodeCombo, label);
        }

        if (_retestButton != null)
        {
            SetButtonContent(_retestButton,
                isTesting ? T("Stop testing", "停止测速") : T("Retest", "重测"),
                isTesting ? Icons.Stop : Icons.Flash);
        }
    }

    private void UpdatePreview()
    {
        if (_previewName == null || _previewSummary == null || _previewDelay == null)
            return;

        var title = string.IsNullOrWhiteSpace(_sceneName) ? _autoSceneName : _sceneName.Trim();
        _previewName.Text = title.Length > 0 ? title : T("Confirm your first scene", "确认你的第一个场景");

        var modeDesc = _target switch
        {
            AccelerateTarget.Tun => T("TUN mode", "TUN 模式"),
            AccelerateTarget.WebProxy => T("Web proxy mode", "网页代理模式"),
            _ => T("Process mode", "进程模式")
        };
        if (_server != null)
        {
            var groupLabel = NodeGroupHelper.IsManualGroup(_server.Group)
                ? T("Added manually", "手动添加")
                : NodeGroupHelper.NormalizeGroup(_server.Group);
            _previewSummary.Text = $"{modeDesc} · {Format.NodeName(_server)} ({groupLabel})";

            var delayText = _server.Delay == DelayStatus.Untested ? "—" : Format.Delay(_server.Delay);
            _previewDelay.Text = delayText;
            _previewDelay.Style = Resource<Style>(_server.Delay switch
            {
                DelayStatus.Untested or DelayStatus.Testing => "NetchSecondaryText",
                < 0 => "NetchErrorText",
                < 200 => "NetchSuccessText",
                _ => "NetchWarningText"
            });
        }
        else
        {
            _previewSummary.Text = modeDesc;
            _previewDelay.Text = "";
            _previewDelay.Style = Resource<Style>("NetchSecondaryText");
        }
    }

    private void OnNodeDelaysChanged() => DispatcherQueue.TryEnqueue(() =>
    {
        if (!_closed && _step == 2)
            RefreshConfirmNodes();
    });

    private void OnDelayTestProgressChanged(bool isTesting, int completed, int total) => DispatcherQueue.TryEnqueue(() =>
    {
        if (!_closed && _step == 2)
            UpdateConfirmTestState(isTesting, completed, total);
    });

    private static Grid WizardTitle(string label) =>
        new() { Name = "WizardTitle", Height = 27, Children = { Text(label, 20, true) } };

    private static StackPanel WizardField(string label, UIElement control)
    {
        var caption = Text(label, 14.5, secondary: true);
        caption.Height = 20;
        var field = Stack(caption, control);
        field.Spacing = 5;
        return field;
    }

    private async Task NextAsync()
    {
        if (_step == 0)
        {
            if (_importingFileOrClipboard)
                return;
            if (_subscriptionImport is { IsCompleted: false } pending)
            {
                if (!await pending || _closed)
                    return;
            }
            var link = _subscriptionUrl.Trim();
            if (link.Length > 0 && (_sourceTab == ImportSourceTab.Subscription || Global.Settings.Server.Count == 0) &&
                !HasImportedSubscription(link) && !await ImportSubscriptionAsync())
                return;
            if (_closed)
                return;
            if (Global.Settings.Server.Count == 0)
            {
                SetStatus(StatusSeverity.Error, T("No supported nodes were found.", "未找到可用节点。"));
                return;
            }

            _installed ??= await Task.Run(InstalledPrograms);
            if (_closed)
                return;
            if (_nodesDirtyForDelayTest || (!DelayTestHelper.IsTesting && Global.Settings.Server.Any(s => s.Delay == DelayStatus.Untested)))
            {
                _nodesDirtyForDelayTest = false;
                DelayTestHelper.PerformTestAsync(waitFinish: true).Forget();
            }
        }
        else if (_step == 1)
        {
            if (_target == AccelerateTarget.Process && _programs.Count == 0)
            {
                await ShowAsync(_root.XamlRoot, T("Choose what to accelerate", "选择加速对象"),
                    Text(T("Select a program, web proxy, or all traffic.", "请选择程序、网页代理或全部流量。")));
                return;
            }
            if (_closed)
                return;

            string defaultName;
            if (_target == AccelerateTarget.Tun)
            {
                defaultName = TunOptionLabel(_tunOption);
            }
            else if (_target == AccelerateTarget.WebProxy)
            {
                defaultName = WebProxyOptionLabel(_webProxyOption);
            }
            else
            {
                var selectedNames = AllProgramEntries()
                    .Where(app => _programs.Contains(app.Rule))
                    .Select(app => app.Name)
                    .ToList();
                if (selectedNames.Count == 0)
                    selectedNames.AddRange(_programs.Select(DisplayNameForRule));

                defaultName = selectedNames.Count <= 3
                    ? string.Join(" / ", selectedNames)
                    : $"{string.Join(" / ", selectedNames.Take(3))} (+{selectedNames.Count - 3})";
            }

            _autoSceneName = defaultName;
            if (!_nameCustomized || string.IsNullOrWhiteSpace(_sceneName))
            {
                _sceneName = defaultName;
                _nameCustomized = false;
            }
        }
        else
        {
            var finalName = _sceneName.Trim();
            if (finalName.Length == 0 || _server == null)
            {
                await ShowAsync(_root.XamlRoot, T("Scene details", "场景信息"),
                    Text(T("Enter a scene name and select a node.", "请输入场景名称并选择节点。")));
                return;
            }

            Mode? createdMode = null;

            string PrepareCustomMode(Mode mode)
            {
                ModeHelper.CompleteSceneDefaults(mode);
                mode.FullName = ModeEditor.NewPath("Custom", finalName);
                if (Path.GetDirectoryName(mode.FullName) is { } dir)
                    Directory.CreateDirectory(dir);
                mode.Remark["en"] = mode.i18NRemark = finalName;
                createdMode = mode;
                return SceneService.GetModeId(mode);
            }

            string modeId;
            if (_target == AccelerateTarget.Tun)
            {
                if (_tunOption == WizardTunOption.BypassChina)
                {
                    if (FindBuiltInTunMode("Bypass LAN and China.txt") is not { } existing)
                    {
                        await ShowAsync(_root.XamlRoot, T("Routing mode", "路由模式"),
                            Text(TF("Built-in rule file \"{0}\" was not found.", "未找到内置规则文件“{0}”。", "Bypass LAN and China.txt")));
                        return;
                    }
                    modeId = SceneService.GetModeId(existing);
                }
                else if (_tunOption == WizardTunOption.BypassLan && FindBuiltInTunMode("Bypass LAN.txt") is { } bypassLanMode)
                {
                    modeId = SceneService.GetModeId(bypassLanMode);
                }
                else
                {
                    modeId = PrepareCustomMode(new TunMode
                    {
                        Handle = ["0.0.0.0/0", "::/0"],
                        Bypass = _tunOption == WizardTunOption.BypassLan
                            ? ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"]
                            : []
                    });
                }
            }
            else
            {
                modeId = PrepareCustomMode(_target == AccelerateTarget.WebProxy
                    ? BuildWizardWebProxyMode(_webProxyOption)
                    : new Redirector { Handle = _programs.ToList() });
            }

            var scene = new Scene
            {
                Name = finalName, Icon = _target == AccelerateTarget.Process ? SceneIcon.Gamepad : SceneIcon.Globe,
                ServerId = _server.Id, PinnedToTray = true
            };
            scene.SetModeIds([modeId]);
            if (_autoConnect)
            {
                var previousStartup = Global.Settings.RunAtStartup;
                Global.Settings.RunAtStartup = true;
                try { Utils.Utils.RegisterNetchStartupItem(); }
                catch { Global.Settings.RunAtStartup = previousStartup; throw; }
            }
            if (createdMode != null)
                ModeService.Instance.Add(createdMode);
            SceneService.Scenes.Add(scene);
            if (_autoConnect)
            {
                Global.Settings.StartWhenOpened = true;
                Global.Settings.StartupSceneId = scene.Id;
            }
            await FinishAsync();
            await Session.Current.StartAsync(scene);
            return;
        }

        _step++;
        ShowStep();
    }

    private Task SkipAsync() => FinishAsync();

    private async Task FinishAsync()
    {
        Global.Settings.OnboardingCompleted = true;
        await Configuration.SaveAsync();
        App.ShowMainWindow();
        App.MainWindow?.Navigate("Scenes", force: true);
        Close();
    }

    private static IReadOnlyList<(string Name, string Rule)> InstalledPrograms()
    {
        const string uninstall = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
        var programs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var hklm32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine, hklm32 })
        {
            using var list = root.OpenSubKey(uninstall);
            foreach (var key in list?.GetSubKeyNames() ?? [])
            {
                using var item = list?.OpenSubKey(key);
                if (item?.GetValue("SystemComponent") is 1 || item?.GetValue("DisplayName") is not string name)
                    continue;
                var icon = (item.GetValue("DisplayIcon") as string ?? "").Split(',')[0].Trim('"', ' ');
                var dir = item.GetValue("InstallLocation") as string;
                var path = File.Exists(icon) && Path.GetExtension(icon).Equals(".exe", StringComparison.OrdinalIgnoreCase) ? icon
                    : Directory.Exists(dir) ? ModeHelper.ScanExecutables(dir!, 1).FirstOrDefault() : null;
                if (path != null)
                    programs.TryAdd(name, Path.GetFileName(path).ToRegexString());
            }
        }
        return programs.OrderBy(pair => pair.Key).Select(pair => (pair.Key, pair.Value)).ToArray();
    }
}
