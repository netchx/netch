using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Win32;
using Netch.App.Dialogs;
using Netch.App.Helpers;
using Netch.App.Pages;
using Netch.App.Services;
using Netch.App.ViewModels;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
using Netch.Models.Modes.TunMode;
using Netch.Services;
using Netch.Utils;
using Windows.Graphics;
using static Netch.App.Helpers.Ui;

namespace Netch.App;

internal sealed class WelcomeWindow : Window
{
    private readonly Grid _root;
    private readonly TextBox _link = Input(T("Subscription URL", "订阅链接"));
    private readonly TextBlock _status = Caption("");
    private readonly HyperlinkButton _skip;
    private readonly ContentControl _steps = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly ContentControl _body = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly ContentControl _footer = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly HashSet<string> _programs = new(StringComparer.OrdinalIgnoreCase);
    private readonly TextBox _name = Input(T("Name", "名称"));
    private readonly ToggleSwitch _auto = CompactToggle(new ToggleSwitch
    {
        Width = 46,
        RenderTransform = new ScaleTransform { ScaleX = 1.125, ScaleY = 1.2 },
        RenderTransformOrigin = new Windows.Foundation.Point(0, 0.5)
    }, 24);
    private IReadOnlyList<(string Name, string Rule)>? _installed;
    private Task<bool>? _subscriptionImport;
    private Server? _server;
    private bool _tun;
    private int _step;

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
            Margin = new Thickness(0, 0, 58, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        _skip.Click += async (_, _) => await SkipAsync();
        var bar = Split(drag, _skip);
        _root = Body(bar, new Grid
        {
            Margin = new Thickness(22, 8, 22, 19),
            Children = { Body(_steps, PlaceBodyAndFooter()) }
        });
        if (!Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            _root.Style = Resource<Style>("NetchShellFallback");
        _root.RequestedTheme = App.MainWindow?.Theme ?? ElementTheme.Default;
        _root.ActualThemeChanged += (_, _) => AppWindow.TitleBar.PreferredTheme = _root.ActualTheme == ElementTheme.Dark
            ? TitleBarTheme.Dark : TitleBarTheme.Light;
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
            WindowSize.Center(this, App.MainWindow!, (int)(640 * dpi), (int)(480 * dpi));
        };
        var dpi = Windows.Win32.PInvoke.GetDpiForWindow(new Windows.Win32.Foundation.HWND(Win32Interop.GetWindowFromWindowId(AppWindow.Id))) / 96.0;
        WindowSize.Center(this, App.MainWindow!, (int)(640 * dpi), (int)(480 * dpi));
        AppWindow.TitleBar.PreferredTheme = _root.ActualTheme == ElementTheme.Dark ? TitleBarTheme.Dark : TitleBarTheme.Light;
        ShowStep();
    }

    public void ApplyTheme(ElementTheme theme) => _root.RequestedTheme = theme;

    private Grid PlaceBodyAndFooter()
    {
        var layout = Body(_body, _footer);
        layout.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
        layout.RowDefinitions[1].Height = GridLength.Auto;
        _body.ContentTransitions = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection { new ContentThemeTransition() };
        return layout;
    }

    private void ShowStep()
    {
        var steps = new Grid { Name = "WizardSteps", ColumnSpacing = 7 };
        var names = new[] { T("Nodes", "节点"), T("Accelerate", "加速"), T("Finish", "完成") };
        for (var i = 0; i < names.Length; i++)
        {
            var badge = Text(i < _step ? "✓" : (i + 1).ToString(), 12, true);
            if (i <= _step)
                badge.Style = Resource<Style>("NetchStepLabel");
            badge.HorizontalAlignment = HorizontalAlignment.Center;
            badge.VerticalAlignment = VerticalAlignment.Center;
            var circle = new Border
            {
                Style = Resource<Style>(i < _step ? "NetchStepDone" : i == _step ? "NetchStepCurrent" : "NetchStep"),
                Width = 24, Height = 24, Child = badge
            };
            var label = Row(circle, Text(names[i], 14, i == _step));
            label.Spacing = 0;
            AddStep(label, GridLength.Auto);
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
        back.Height = next.Height = 33;
        back.MinWidth = _step == 0 ? 94 : 78;
        next.MinWidth = _step == 2 ? 109 : 78;
        _footer.Content = Split(back, next);
        foreach (var reusable in new FrameworkElement[] { _link, _name, _auto, _status })
            if (reusable.Parent is Panel parent)
                parent.Children.Remove(reusable);
        _body.Content = _step switch
        {
            0 => ImportStep(),
            1 => ProgramsStep(),
            _ => ConfirmStep()
        };
    }

    private UIElement ImportStep()
    {
        var add = new Button
        {
            Height = 33,
            Content = T("+ Add manually", "＋ 手动添加"), HorizontalAlignment = HorizontalAlignment.Stretch,
            Flyout = Menu(ServerEditorHelper.GetOrderedProtocols().Select(util =>
                (ServerHelper.GetDisplayName(util), (Action)(async () =>
                {
                    if (await NodeEditor.EditAsync(_root.XamlRoot, null, util.TypeName))
                        UpdateNodeCount();
                }))).ToArray())
        };
        var clipboard = AsyncButton(T("📋 Import from clipboard", "📋 从剪贴板导入"), async () =>
        {
            var count = await NodeEditor.ImportClipboardAsync();
            UpdateNodeCount(count == 0);
        });
        clipboard.HorizontalAlignment = HorizontalAlignment.Stretch;
        clipboard.Height = 33;

        var divider = new Grid { Name = "ImportDivider", ColumnSpacing = 12, Height = 20 };
        foreach (var width in new[] { new GridLength(1, GridUnitType.Star), GridLength.Auto, new GridLength(1, GridUnitType.Star) })
            divider.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
        var middle = Caption(T("or", "或"));
        Grid.SetColumn(middle, 1);
        divider.Children.Add(middle);
        foreach (var column in new[] { 0, 2 })
        {
            var line = new Border
            {
                Style = Resource<Style>("NetchDivider"), Height = 1, VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(line, column);
            divider.Children.Add(line);
        }

        _link.KeyDown -= LinkKeyDown;
        _link.KeyDown += LinkKeyDown;
        _link.Header = null;
        _link.Name = "SubscriptionLink";
        _link.MinHeight = 43;
        _link.Padding = new Thickness(12, 10, 48, 10);
        var import = new Button
        {
            Content = new FontIcon { Glyph = "\uE77F", FontSize = 16 },
            Background = new SolidColorBrush(Colors.Transparent), BorderThickness = new Thickness(0),
            Width = 39, Height = 34, Padding = new Thickness(0), Margin = new Thickness(0, 0, 3, 0),
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center,
            Command = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(ImportSubscriptionAsync)
        };
        AutomationProperties.SetName(import, T("Import subscription", "导入订阅"));
        var linkField = new Grid { Children = { _link, import } };
        var actions = TwoColumns(clipboard, add, new GridLength(1, GridUnitType.Star));
        actions.Name = "ImportActions";
        var status = new Grid { Name = "ImportStatus", MinHeight = 21, Children = { _status } };
        var step = Stack(WizardTitle(T("Import your nodes", "导入你的节点")),
            WizardField(T("Subscription URL", "订阅链接"), linkField), divider, actions, status);
        step.Spacing = 12;
        return step;
    }

    private async void LinkKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs args)
    {
        if (args.Key == Windows.System.VirtualKey.Enter)
        {
            args.Handled = true;
            await ImportSubscriptionAsync();
        }
    }

    private void UpdateNodeCount(bool invalid = false)
    {
        _status.Style = Resource<Style>(invalid ? "NetchErrorText" : "NetchSuccessText");
        var total = Global.Settings.Server.Count;
        _status.Text = invalid ? T("No supported nodes were found.", "未找到可用节点。")
            : TF("✓ {0} nodes available", "✓ 已识别 {0} 个节点", total);
    }

    private Task<bool> ImportSubscriptionAsync() => _subscriptionImport is { IsCompleted: false } pending
        ? pending : _subscriptionImport = ImportSubscriptionCoreAsync();

    private async Task<bool> ImportSubscriptionCoreAsync()
    {
        if (!Uri.TryCreate(_link.Text.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            _status.Style = Resource<Style>("NetchErrorText");
            _status.Text = T("Enter an HTTP or HTTPS subscription URL.", "请输入 HTTP 或 HTTPS 订阅链接。");
            return false;
        }

        _link.IsEnabled = false;
        _status.Style = Resource<Style>("NetchSecondaryText");
        _status.Text = T("Importing subscription…", "正在导入订阅…");
        try
        {
            var subscription = Global.Settings.Subscription.FirstOrDefault(s => s.Link == uri.AbsoluteUri);
            if (subscription == null)
            {
                var name = uri.Host;
                for (var n = 2; Global.Settings.Subscription.Any(s => s.Remark == name); n++)
                    name = $"{uri.Host} ({n})";
                subscription = new Subscription { Link = uri.AbsoluteUri, Remark = name, UpdateIntervalHours = 6 };
                Global.Settings.Subscription.Add(subscription);
            }
            var imported = await SubscriptionUtil.UpdateServerAsync(subscription);
            UpdateNodeCount(imported == 0);
            if (imported > 0)
                _status.Text = TF("✓ Imported {0} nodes ({1})", "✓ 已识别 {0} 个节点（{1}）", imported, subscription.Remark);
            await Configuration.SaveAsync();
            return imported > 0;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Import subscription failed");
            _status.Style = Resource<Style>("NetchErrorText");
            _status.Text = e.Message;
            return false;
        }
        finally
        {
            _link.IsEnabled = true;
        }
    }

    private static bool HasImportedSubscription(string link)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri))
            return false;
        var subscription = Global.Settings.Subscription.FirstOrDefault(s => s.Link == uri.AbsoluteUri);
        return subscription != null && Global.Settings.Server.Any(server => server.IsSub && server.Group == subscription.Remark);
    }

    private UIElement ProgramsStep()
    {
        var list = Stack();
        list.Spacing = 2;
        var all = new CheckBox { Content = T("🌐 All traffic (TUN)", "🌐 全部流量（TUN）"), IsChecked = _tun, FontSize = 16 };
        all.Checked += (_, _) =>
        {
            _tun = true;
            foreach (var row in list.Children.OfType<Grid>())
                ((CheckBox)row.Children[0]).IsChecked = false;
        };
        all.Unchecked += (_, _) => _tun = false;
        foreach (var (name, rule) in _installed ?? [])
        {
            var select = new CheckBox
            {
                Content = "🎮 " + name, IsChecked = _programs.Contains(rule), FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            };
            select.Checked += (_, _) => { _programs.Add(rule); all.IsChecked = false; };
            select.Unchecked += (_, _) => _programs.Remove(rule);
            var row = Split(select, Text(T("Detected", "已检测"), 14.5, secondary: true));
            row.MinHeight = 35;
            list.Children.Add(row);
        }
        var programs = Stack(new ScrollViewer { Content = list, MaxHeight = 109 }, all);
        programs.Spacing = 4;
        var card = Card(programs);
        card.Name = "WizardPrograms";
        card.Padding = new Thickness(16, 5, 16, 5);
        var step = Stack(WizardTitle(T("Which programs should be accelerated?", "要加速哪些程序？")), card);
        step.Spacing = 14;
        return step;
    }

    private UIElement ConfirmStep()
    {
        var nodes = Global.Settings.Server.OrderBy(server => server.Delay is >= 0 and < 1000 ? server.Delay : int.MaxValue).ToArray();
        var index = Array.FindIndex(nodes, server => server.Id == _server?.Id);
        var nodeLabel = T("Node (ordered by latency)", "节点（已按延迟推荐）");
        var selector = new ComboBox
        {
            Name = "WizardNode", Height = 41, HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = nodes.Select(server => new NodeOption(server)).ToArray(),
            ItemTemplate = Resource<DataTemplate>("NetchWizardNodeTemplate"),
            SelectedIndex = Math.Max(0, index), IsTextSearchEnabled = true
        };
        AutomationProperties.SetName(selector, nodeLabel);
        if (nodes.Length > 0) _server = nodes[Math.Max(0, index)];
        selector.SelectionChanged += (_, _) => { if (selector.SelectedIndex >= 0) _server = nodes[selector.SelectedIndex]; };
        _name.Header = null;
        _name.Name = "WizardSceneName";
        _name.MinHeight = 40;
        _name.Padding = new Thickness(11, 9, 11, 9);
        var startup = T("Connect this scene after sign-in", "开机后自动连接此场景");
        AutomationProperties.SetName(_auto, startup);
        var autoRow = Row(_auto, Text(startup));
        autoRow.Name = "WizardAutostart";
        var nameField = WizardField(T("Name", "名称"), _name);
        var nodeField = WizardField(nodeLabel, selector);
        nameField.Spacing = nodeField.Spacing = 6;
        var step = Stack(WizardTitle(T("Confirm your first scene", "确认你的第一个场景")), nameField, nodeField, autoRow);
        step.Spacing = 12;
        return step;
    }

    private static Grid WizardTitle(string label)
    {
        return new Grid { Name = "WizardTitle", Height = 27, Children = { Text(label, 20, true) } };
    }

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
            if (_subscriptionImport is { IsCompleted: false } pending && !await pending)
                return;
            var link = _link.Text.Trim();
            if (link.Length > 0 && !HasImportedSubscription(link) && !await ImportSubscriptionAsync())
                return;
            if (Global.Settings.Server.Count == 0)
            {
                UpdateNodeCount(invalid: true);
                return;
            }

            _installed ??= await Task.Run(InstalledPrograms);
            await DelayTestHelper.PerformTestAsync(true);
        }
        else if (_step == 1)
        {
            if (!_tun && _programs.Count == 0)
            {
                await ShowAsync(_root.XamlRoot, T("Choose what to accelerate", "选择加速对象"),
                    Text(T("Select an installed program or all traffic.", "请选择程序或全部流量。")));
                return;
            }
            if (_name.Text.Length == 0)
                _name.Text = _tun ? T("Global TUN", "全局 TUN") : string.Join(" / ", _installed!.Where(app => _programs.Contains(app.Rule)).Select(app => app.Name));
        }
        else
        {
            if (_name.Text.Trim().Length == 0 || _server == null)
            {
                await ShowAsync(_root.XamlRoot, T("Scene details", "场景信息"), Text(T("Enter a scene name and select a node.", "请输入场景名称并选择节点。")));
                return;
            }

            Mode mode = _tun ? new TunMode { Handle = ["0.0.0.0/0", "::/0"] }
                : new Redirector { Handle = _programs.ToList() };
            ModeHelper.CompleteSceneDefaults(mode);
            mode.FullName = ModeEditor.NewPath("Custom", _name.Text.Trim());
            mode.Remark["en"] = mode.i18NRemark = _name.Text.Trim();
            var scene = new Scene
            {
                Name = _name.Text.Trim(), Icon = _tun ? "🌐" : "🎮",
                ServerId = _server.Id, ModeId = SceneService.GetModeId(mode), PinnedToTray = true
            };
            if (_auto.IsOn)
            {
                var previousStartup = Global.Settings.RunAtStartup;
                Global.Settings.RunAtStartup = true;
                try { Utils.Utils.RegisterNetchStartupItem(); }
                catch { Global.Settings.RunAtStartup = previousStartup; throw; }
            }
            ModeService.Instance.Add(mode);
            SceneService.Scenes.Add(scene);
            if (_auto.IsOn)
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
        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine, RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32) })
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
