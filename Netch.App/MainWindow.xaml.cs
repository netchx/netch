using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Netch.App.Pages;
using Netch.App.Helpers;
using Netch.Enums;
using Windows.Graphics;
using Windows.UI;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Netch.App;

public sealed partial class MainWindow : Window
{
    private const int DefaultWidth = 1024;
    private const int DefaultHeight = 768;
    private const int MinimumWidth = 800;
    private const int MinimumHeight = 600;

    private static readonly Dictionary<string, Type> PageTypes = new()
    {
        ["Scenes"] = typeof(ScenesPage),
        ["Nodes"] = typeof(NodesPage),
        ["Modes"] = typeof(ModesPage),
        ["Downloads"] = typeof(DownloadsPage),
        ["Logs"] = typeof(LogsPage),
        ["About"] = typeof(AboutPage),
        ["Settings"] = typeof(SettingsPage)
    };

    private readonly HWND _hwnd;
    private double _minimumSizeScale;
    private object? _navigationParameter;

    public MainWindow()
    {
        InitializeComponent();
        if (!Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            RootGrid.Style = Ui.Resource<Style>("NetchShellFallback");
        _hwnd = new HWND(Win32Interop.GetWindowFromWindowId(AppWindow.Id));

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Netch.ico"));

        var scale = PInvoke.GetDpiForWindow(_hwnd) / 96.0;
        WindowSize.Center(this, this, (int)(DefaultWidth * scale), (int)(DefaultHeight * scale));
        RootGrid.Loaded += (_, _) =>
        {
            var root = RootGrid.XamlRoot;
            WindowSize.Center(this, this, (int)(DefaultWidth * root.RasterizationScale), (int)(DefaultHeight * root.RasterizationScale));
            ApplyMinimumSize(root.RasterizationScale);
            root.Changed += (_, _) => ApplyMinimumSize(root.RasterizationScale);
        };

        ApplyTheme(Global.Settings.Theme);
        RootGrid.ActualThemeChanged += (_, _) => UpdateCaptionButtons();
        AppWindow.Closing += AppWindow_Closing;

        NavView.SelectedItem = NavView.MenuItems[0];
        Services.Downloads.Changed += UpdateDownloadBadge;
    }

    private void UpdateDownloadBadge()
    {
        var count = Services.Downloads.PendingCount;
        DownloadBadge.Value = count;
        DownloadBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Selects the navigation item with this tag, e.g. "Nodes".</summary>
    public void Navigate(string tag, object? parameter = null, bool force = false)
    {
        var item = NavView.MenuItems.Concat(NavView.FooterMenuItems)
            .OfType<NavigationViewItem>()
            .First(item => (string)item.Tag == tag);
        if (parameter == null && !force)
        {
            NavView.SelectedItem = item;
            return;
        }

        if (ContentFrame.CurrentSourcePageType == PageTypes[tag] || NavView.SelectedItem == item)
            ContentFrame.Navigate(PageTypes[tag], parameter);
        else
        {
            _navigationParameter = parameter;
            NavView.SelectedItem = item;
        }
    }

    public void ReloadLanguage()
    {
        foreach (var item in NavView.MenuItems.Concat(NavView.FooterMenuItems).OfType<NavigationViewItem>())
            item.Content = Utils.i18N.Translate((string)item.Tag);
        Navigate((string)((NavigationViewItem)NavView.SelectedItem).Tag, force: true);
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (App.IsExiting)
            return;

        args.Cancel = true;
        if (Global.Settings.ExitWhenClosed)
            await App.ExitAsync();
        else
            App.HideToTray();
    }

    public ElementTheme Theme => RootGrid.RequestedTheme;

    internal ElementTheme ActualTheme => RootGrid.ActualTheme;

    public void ApplyTheme(AppTheme theme)
    {
        RootGrid.RequestedTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
        UpdateCaptionButtons();
    }

    public void BringToFront()
    {
        if (AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
            presenter.Restore();

        AppWindow.Show();
        Activate();
        PInvoke.SetForegroundWindow(_hwnd);
    }

    /// <summary>
    ///     The presenter takes physical pixels and keeps them when the window moves to a display with another
    ///     scale, so the 800×600 minimum is applied again for each scale.
    /// </summary>
    private void ApplyMinimumSize(double scale)
    {
        if (scale == _minimumSizeScale || AppWindow.Presenter is not OverlappedPresenter presenter)
            return;

        _minimumSizeScale = scale;
        var border = WindowSize.InvisibleBorder(this);
        presenter.PreferredMinimumWidth = (int)(MinimumWidth * scale) + border.Width;
        presenter.PreferredMinimumHeight = (int)(MinimumHeight * scale) + border.Height;
    }

    /// <summary>The system draws the caption buttons, so they do not follow RequestedTheme on their own.</summary>
    private void UpdateCaptionButtons()
    {
        var dark = RootGrid.ActualTheme == ElementTheme.Dark;
        var titleBar = AppWindow.TitleBar;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonForegroundColor = dark ? Colors.White : Color.FromArgb(0xE4, 0, 0, 0);
        titleBar.ButtonHoverForegroundColor = titleBar.ButtonForegroundColor;
        titleBar.ButtonPressedForegroundColor = titleBar.ButtonForegroundColor;
        titleBar.ButtonInactiveForegroundColor = dark ? Color.FromArgb(0x5D, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x5C, 0, 0, 0);
        titleBar.ButtonHoverBackgroundColor = dark ? Color.FromArgb(0x0F, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x09, 0, 0, 0);
        titleBar.ButtonPressedBackgroundColor = dark ? Color.FromArgb(0x0A, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x06, 0, 0, 0);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is string tag && PageTypes.TryGetValue(tag, out var pageType) && ContentFrame.CurrentSourcePageType != pageType)
        {
            var parameter = _navigationParameter;
            _navigationParameter = null;
            ContentFrame.Navigate(pageType, parameter, args.RecommendedNavigationTransitionInfo);
            ContentFrame.BackStack.Clear();
        }
    }
}
