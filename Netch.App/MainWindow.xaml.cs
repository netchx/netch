using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Netch.App.Pages;
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
    private bool _canClose;

    public MainWindow()
    {
        InitializeComponent();
        _hwnd = new HWND(Win32Interop.GetWindowFromWindowId(AppWindow.Id));

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Netch.ico"));

        var scale = PInvoke.GetDpiForWindow(_hwnd) / 96.0;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = (int)(MinimumWidth * scale);
            presenter.PreferredMinimumHeight = (int)(MinimumHeight * scale);
        }

        ResizeAndCenter((int)(DefaultWidth * scale), (int)(DefaultHeight * scale));

        ApplyTheme(Global.Settings.Theme);
        RootGrid.ActualThemeChanged += (_, _) => UpdateCaptionButtons();
        AppWindow.Closing += AppWindow_Closing;

        NavView.SelectedItem = NavView.MenuItems[0];
    }

    /// <summary>Selects the navigation item with this tag, e.g. "Nodes".</summary>
    public void Navigate(string tag)
    {
        NavView.SelectedItem = NavView.MenuItems.Concat(NavView.FooterMenuItems)
            .OfType<NavigationViewItem>()
            .First(item => (string)item.Tag == tag);
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_canClose)
            return;

        args.Cancel = true;
        AppWindow.Hide();
        await App.ShutdownAsync();
        _canClose = true;
        Close();
    }

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

    private void ResizeAndCenter(int width, int height)
    {
        var workArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        width = Math.Min(width, workArea.Width);
        height = Math.Min(height, workArea.Height);
        AppWindow.MoveAndResize(new RectInt32(workArea.X + (workArea.Width - width) / 2, workArea.Y + (workArea.Height - height) / 2, width, height));
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
            ContentFrame.Navigate(pageType, null, args.RecommendedNavigationTransitionInfo);
    }
}
