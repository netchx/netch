using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
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
using ReactiveUI.Fody.Helpers;
using Serilog;
using Vanara.PInvoke;
using Clipboard = System.Windows.Clipboard;

namespace Netch.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly IConfigService _configService;
        private readonly SourceCache<Mode, string> _modeCache;
        private readonly ServerService _serverService;
        private readonly ModeService _modeService;
        private readonly SourceList<Server> _serverList;
        private readonly Setting _setting;

        #region Commands

        public ReactiveCommand<Unit, Unit> CleanDnsCommand { get; }

        public ReactiveCommand<Unit, Unit> CreateProcessModeCommand { get; }

        public ReactiveCommand<Unit, Unit> CreateRouteTableModeCommand { get; }

        public ReactiveCommand<IServerUtil, Unit> CreateServerCommand { get; }

        public ReactiveCommand<Unit, Unit> ImportServerFromClipBoardCommand { get; }

        public ReactiveCommand<Unit, Unit> ManageSubscribeLinksCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenDirectoryCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenFaqPageCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenReleasesPageCommand { get; }

        public ReactiveCommand<Unit, Unit> RemoveNetchFirewallRulesCommand { get; }

        public ReactiveCommand<Unit, Unit> ShowHideConsoleCommand { get; }

        public ReactiveCommand<Unit, Unit> UpdateServersFromSubscribeCommand { get; }

        public ReactiveCommand<Server, Unit> DeleteServerCommand { get; }

        #endregion

        public BindingList<Server> ServerList { get; } = new();

        public BindingList<Mode> ModeCache { get; } = new();

        public MainWindowViewModel(Setting setting,
            IConfigService configService,
            SourceList<Server> serverList,
            SourceCache<Mode, string> modeCache,
            ServerService serverService,
            ModeService modeService)
        {
            _setting = setting;
            _configService = configService;
            _serverList = serverList;
            _modeCache = modeCache;
            _serverService = serverService;
            _modeService = modeService;

            _serverList.Connect()
                // .ObserveOn(SynchronizationContext.Current)
                .Bind(ServerList)
                // .DisposeMany()
                .Subscribe();

            _modeCache.Connect()
                // .ObserveOn(SynchronizationContext.Current)
                .Sort(Comparer<Mode>.Create((a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal)))
                .Bind(ModeCache)
                // .DisposeMany()
                .Subscribe();

            ImportServerFromClipBoardCommand = ReactiveCommand.CreateFromTask(ImportServerFromClipBoard);
            CleanDnsCommand = ReactiveCommand.CreateFromTask(CleanDns);
            UpdateServersFromSubscribeCommand = ReactiveCommand.CreateFromTask(UpdateServersFromSubscribe);

            CreateServerCommand = ReactiveCommand.CreateFromTask<IServerUtil>(CreateServer);

            CreateProcessModeCommand = ReactiveCommand.Create(CreateProcessMode);
            CreateRouteTableModeCommand = ReactiveCommand.Create(CreateRouteTableMode);

            ManageSubscribeLinksCommand = ReactiveCommand.Create(ManageSubscribeLinks);

            OpenDirectoryCommand = ReactiveCommand.Create(OpenDirectory);
            RemoveNetchFirewallRulesCommand = ReactiveCommand.Create(RemoveNetchFirewallRules);
            ShowHideConsoleCommand = ReactiveCommand.Create(ShowHideConsole);

            OpenFaqPageCommand = ReactiveCommand.Create(OpenFaqPage);

            OpenReleasesPageCommand = ReactiveCommand.Create(OpenReleasesPage);
            DeleteServerCommand = ReactiveCommand.Create<Server>(DeleteServer);
        }

        private MainForm _mainForm => DI.GetRequiredService<MainForm>();

        [Reactive] public State State { get; set; }

        private async Task ImportServerFromClipBoard()
        {
            var texts = Clipboard.GetText();
            if (!string.IsNullOrWhiteSpace(texts))
            {
                var servers = ShareLink.ParseText(texts);
                _setting.Server.AddRange(servers);
                _mainForm.NotifyTip(i18N.TranslateFormat("Import {0} server(s) form Clipboard", servers.Count));

                _mainForm.SelectLastServer();
                await _configService.SaveAsync();
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

                _mainForm.SelectLastServer();
                await _configService.SaveAsync();
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

        private async Task CreateServer(IServerUtil util)
        {
            _mainForm.Hide();
            util.Create();

            await _configService.SaveAsync();
            _mainForm.Show();
        }

        private void CreateProcessMode()
        {
            _mainForm.Hide();
            var form = new Process();
            form.ShowDialog();

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
            var windowStyles = (User32.WindowStyles) User32.GetWindowLong(Netch.ConsoleHwnd, User32.WindowLongFlags.GWL_STYLE);
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

        private void DeleteServer(Server server)
        {
            _serverService.RemoveServer(server);
        }
    }
}