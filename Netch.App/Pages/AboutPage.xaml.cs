using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Netch.App.Helpers;
using Netch.Controllers;
using Netch.Utils;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Pages;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        using var identity = WindowsIdentity.GetCurrent();
        var admin = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        var driver = DriverStatus();
        var directory = Configuration.DataDirectoryFullName;
        var deployment = UpdateChecker.IsSelfContained
            ? T("Self-contained", "自带框架")
            : T("Framework-dependent", "系统框架");
        var data = new (string Key, string Value)[]
        {
            (T("System", "系统"), RuntimeInformation.OSDescription),
            (T("Runtime", "运行时"), $"{RuntimeInformation.FrameworkDescription} ({deployment})"),
            (T("Privileges", "权限"), admin ? T("Administrator", "管理员") : T("Standard user", "标准用户")),
            (T("NF driver", "NF 驱动"), driver),
            (T("Data directory", "数据目录"), directory)
        };
        var diag = Stack(Text(T("Runtime information", "运行信息"), strong: true));
        diag.Spacing = 5.25;
        foreach (var (label, value) in data)
        {
            var field = Caption(label);
            field.Width = 69;
            var detail = Text(value);
            ToolTipService.SetToolTip(detail, value);
            diag.Children.Add(TwoColumns(field, detail, new GridLength(69)));
        }
        diag.Children.Add(Row(Button(T("Copy diagnostics", "复制诊断信息"), () =>
            Copy($"Netch {UpdateChecker.Version} · {RuntimeInformation.ProcessArchitecture}\n" + string.Join("\n", data.Select(row => $"{row.Key}: {row.Value}"))), glyph: Icons.Copy),
            Button(T("Open directory", "打开目录"), () => Utils.Utils.Open(directory), glyph: Icons.FolderOpen)));

        var names = Caption("sing-box · Mihomo · Xray-core · tun2socks · WinTUN · pcap2socks · NetFilter SDK · Serilog · Stun.Net · Windows App SDK");
        names.TextWrapping = TextWrapping.Wrap;
        var credits = Stack(Text(T("Open-source acknowledgments", "开源致谢"), strong: true), names,
            Row(Caption("GPL-3.0"), Link(T("View license", "查看许可证"), UpdateChecker.ProjectUrl + "/blob/v2/LICENSE")));
        var logo = Center(new Image
        {
            Width = 80,
            Height = 80,
            Source = new BitmapImage(new Uri("ms-appx:///Assets/Netch.png"))
        });
        var versionText = Text("v" + UpdateChecker.Version, 13);
        versionText.Style = Resource<Style>("NetchAccentText");
        versionText.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas");
        var version = new Border { Style = Resource<Style>("NetchPill"), Padding = new Thickness(7, 2, 7, 2), Child = versionText,
            VerticalAlignment = VerticalAlignment.Center };
        var check = new HyperlinkButton { Content = T("Check for updates", "检查更新"), Padding = new Thickness(2, 0, 2, 0), Height = 30, MinHeight = 0 };
        check.Click += (_, _) => App.MainWindow?.Navigate("Downloads", "check");
        var links = Center(Row(Link("GitHub", UpdateChecker.ProjectUrl), Link(T("Telegram group", "Telegram 群组"), "https://t.me/netch_group"),
            Link(T("Report an issue", "问题反馈"), UpdateChecker.ProjectUrl + "/issues"), Link(T("FAQ", "常见问题"), UpdateChecker.ProjectUrl + "/wiki")));
        links.Margin = new Thickness(0, 7, 0, 0);
        var heading = Stack(logo, Center(Text("Netch", 26, true)),
            Center(Row(version, Caption(RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant() + " · " +
#if DEBUG
                "Debug"), check)),
#else
                "Release"), check)),
#endif
            Center(Text(T("Open-source Windows process proxy and game accelerator", "开源的 Windows 进程代理与游戏加速工具"), 14.15, secondary: true)),
            links);
        heading.Margin = new Thickness(0, 9, 0, 9);
        var reopen = Button(T("Reopen welcome wizard", "重新打开首次向导"), App.OpenWizard);
        reopen.Height = 30;
        reopen.MinHeight = 0;
        reopen.Padding = new Thickness(7, 4, 7, 4);
        var footer = Center(Row(reopen, Caption("·  © Netch contributors")));
        var cards = TwoColumns(Card(diag, 14), Card(credits, 14), new GridLength(1, GridUnitType.Star));
        var layout = Body(heading, cards, new Border(), footer);
        layout.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
        layout.RowDefinitions[3].Height = GridLength.Auto;
        Content = Ui.Page(layout);
    }

    private static TElement Center<TElement>(TElement element) where TElement : FrameworkElement
    {
        element.HorizontalAlignment = HorizontalAlignment.Center;
        return element;
    }

    private static Button Link(string label, string url)
    {
        var link = Button(label, () => Utils.Utils.Open(url));
        link.Height = 30;
        link.MinHeight = 0;
        link.Padding = new Thickness(12, 4, 12, 4);
        return link;
    }

    private static string DriverStatus()
    {
        try
        {
            using var service = new ServiceController("netfilter2");
            return service.Status == ServiceControllerStatus.Running ? T("Running", "运行中") : T("Installed", "已安装");
        }
        catch (InvalidOperationException) { return T("Not installed", "未安装"); }
    }
}
