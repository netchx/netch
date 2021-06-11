using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DynamicData;
using Netch.Controllers;
using Netch.Forms;
using Netch.Forms.Mode;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;
using Netch.Utils;
using ReactiveUI;
using Serilog;
using Vanara.PInvoke;
using Clipboard = System.Windows.Clipboard;

namespace Netch.ViewModels
{
    public class MainWindowViewModel
    {
        private readonly Setting _setting;
        private readonly Configuration _configuration;
        private readonly SourceList<Server> _serverList;
        private readonly SourceCache<Mode, string> _modeList;
        private readonly ModeService _modeService;

        public readonly ReadOnlyObservableCollection<Server> ServerList;
        public readonly ReadOnlyObservableCollection<Mode> ModeList;

        private MainForm _mainForm => DI.GetRequiredService<MainForm>();

        public readonly ReactiveCommand<Unit, Unit> ImportServerFromClipBoardCommand;
        public readonly ReactiveCommand<object?, Unit> CreateServerCommand;

        public readonly ReactiveCommand<Unit, Unit> CreateProcessModeCommand;
        public readonly ReactiveCommand<Unit, Unit> CreateRouteTableModeCommand;

        public readonly ReactiveCommand<Unit, Unit> ManageSubscribeLinksCommand;
        public readonly ReactiveCommand<Unit, Unit> UpdateServersFromSubscribeCommand;

        public readonly ReactiveCommand<Unit, Unit> OpenDirectoryCommand;
        public readonly ReactiveCommand<Unit, Unit> CleanDnsCommand;
        public readonly ReactiveCommand<Unit, Unit> RemoveNetchFirewallRulesCommand;
        public readonly ReactiveCommand<Unit, Unit> ShowHideConsoleCommand;

        public readonly ReactiveCommand<Unit, Unit> OpenFaqPageCommand;

        public readonly ReactiveCommand<Unit, Unit> OpenReleasesPageCommand;

        public MainWindowViewModel(Setting setting, Configuration configuration, SourceList<Server> serverList, SourceCache<Mode, string> modeList,ModeService modeService)
        {
            _setting = setting;
            _configuration = configuration;
            _serverList = serverList;
            _modeList = modeList;
            _modeService = modeService;

            ImportServerFromClipBoardCommand = ReactiveCommand.CreateFromTask(ImportServerFromClipBoard);
            CleanDnsCommand = ReactiveCommand.CreateFromTask(CleanDns);
            UpdateServersFromSubscribeCommand = ReactiveCommand.CreateFromTask(UpdateServersFromSubscribe);

            CreateServerCommand = ReactiveCommand.CreateFromTask<object?>(CreateServer);

            CreateProcessModeCommand = ReactiveCommand.Create(CreateProcessMode);
            CreateRouteTableModeCommand = ReactiveCommand.Create(CreateRouteTableMode);

            ManageSubscribeLinksCommand = ReactiveCommand.Create(ManageSubscribeLinks);

            OpenDirectoryCommand = ReactiveCommand.Create(OpenDirectory);
            RemoveNetchFirewallRulesCommand = ReactiveCommand.Create(RemoveNetchFirewallRules);
            ShowHideConsoleCommand = ReactiveCommand.Create(ShowHideConsole);

            OpenFaqPageCommand = ReactiveCommand.Create(OpenFaqPage);

            OpenReleasesPageCommand = ReactiveCommand.Create(OpenReleasesPage);

            _serverList.AddRange(_setting.Server);
            
            serverList.Connect()
                // .ObserveOnDispatcher()
                .Bind(out ServerList)
                .DisposeMany()
                .Subscribe();

            modeList.Connect()
                .Sort(Comparer<Mode>.Create((a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal)))
                // .ObserveOnDispatcher()
                .Bind(out ModeList)
                .DisposeMany()
                .Subscribe();
        }

        private async Task ImportServerFromClipBoard()
        {
            var texts = Clipboard.GetText();
            if (!string.IsNullOrWhiteSpace(texts))
            {
                var servers = ShareLink.ParseText(texts);
                _setting.Server.AddRange(servers);
                _mainForm.NotifyTip(i18N.TranslateFormat("Import {0} server(s) form Clipboard", servers.Count));

                _mainForm.LoadServers();
                await _configuration.SaveAsync();
            }
        }

        private async Task CleanDns()
        {
            await Task.Run(() =>
            {
                NativeMethods.RefreshDNSCache();
                DnsUtils.ClearCache();
            });

            _mainForm.NotifyTip(i18N.Translate("DNS cache cleanup succeeded"));
        }

        private async Task UpdateServersFromSubscribe()
        {
            if (_setting.SubscribeLink.Count <= 0)
            {
                MessageBoxX.Show(i18N.Translate("No subscription link"));
                return;
            }

            _mainForm.StatusText(i18N.Translate("Starting update subscription"));
            _mainForm.ServerControls(false);

            try
            {
                await Subscription.UpdateServersAsync();

                _mainForm.LoadServers();
                await _configuration.SaveAsync();
                _mainForm.StatusText(i18N.Translate("Subscription updated"));
            }
            catch (Exception e)
            {
                _mainForm.NotifyTip(i18N.Translate("update servers failed") + "\n" + e.Message, info: false);
                Log.Error(e, "更新服务器失败");
            }
            finally
            {
                _mainForm.ServerControls(true);
            }
        }

        private async Task CreateServer(object? sender)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            var util = (IServerUtil)((ToolStripMenuItem)sender).Tag;

            _mainForm.Hide();
            util.Create();

            await _configuration.SaveAsync();
            _mainForm.Show();
        }

        private void CreateProcessMode()
        {
            _mainForm.Hide();
            new Process().ShowDialog();
            _mainForm.Show();
        }

        private void CreateRouteTableMode()
        {
            _mainForm.Hide();
            new Route().ShowDialog();
            _mainForm.Show();
        }

        private void ManageSubscribeLinks()
        {
            _mainForm.Hide();

            DI.GetRequiredService<SubscribeForm>().ShowDialog();

            _mainForm.Show();
        }

        private void OpenDirectory()
        {
            Misc.Open(".\\");
        }

        private void RemoveNetchFirewallRules()
        {
            Firewall.RemoveNetchFwRules();
        }

        private void ShowHideConsole()
        {
            var windowStyles = (User32.WindowStyles)User32.GetWindowLong(Netch.ConsoleHwnd, User32.WindowLongFlags.GWL_STYLE);
            var visible = windowStyles.HasFlag(User32.WindowStyles.WS_VISIBLE);
            User32.ShowWindow(Netch.ConsoleHwnd, visible ? ShowWindowCommand.SW_HIDE : ShowWindowCommand.SW_SHOWNOACTIVATE);
        }

        private void OpenReleasesPage()
        {
            Misc.Open($"https://github.com/{UpdateChecker.Owner}/{UpdateChecker.Repo}/releases");
        }

        private void OpenFaqPage()
        {
            Misc.Open("https://netch.org/#/docs/zh-CN/faq");
        }
    }
}