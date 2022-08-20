using System.Text;
using Microsoft.VisualStudio.Threading;
using Netch.Forms;
using Netch.Interfaces;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.ShareMode;
using Netch.Servers;
using Netch.Utils;

namespace Netch.Controllers;

public class PcapController : Guard, IModeController
{
    private readonly LogForm _form;
    private ShareMode _mode = null!;
    private Socks5Server _server = null!;

    public PcapController() : base("pcap2socks.exe", encoding: Encoding.UTF8)
    {
        _form = new LogForm(Global.MainForm);
        _form.CreateControl();
    }

    protected override IEnumerable<string> StartedKeywords { get; } = new[] { "└" };

    public override string Name => "pcap2socks";

    public ModeFeature Features => ModeFeature.SupportSocks5Auth;

    public async Task StartAsync(Socks5Server server, Mode mode)
    {
        if (mode is not ShareMode shareMode)
            throw new InvalidOperationException();

        _server = server;
        _mode = shareMode;

        var outboundNetworkInterface = NetworkInterfaceUtils.GetBest();

        var arguments = new List<object?>
        {
            "--interface", $@"\Device\NPF_{outboundNetworkInterface.Id}",
            "--destination", $"{await _server.AutoResolveHostnameAsync()}:{_server.Port}",
            _mode.Argument, SpecialArgument.Flag
        };

        if (_server.Auth())
            arguments.AddRange(new[]
            {
                "--username", server.Username,
                "--password", server.Password
            });

        await StartGuardAsync(Arguments.Format(arguments));
    }

    public override async Task StopAsync()
    {
        Global.MainForm.Invoke(() => { _form.Close(); });
        await StopGuardAsync();
    }

    ~PcapController()
    {
        _form.Dispose();
    }

    protected override void OnReadNewLine(string line)
    {
        Global.MainForm.BeginInvoke(() =>
        {
            if (!_form.IsDisposed)
                _form.richTextBox1.AppendText(line + "\n");
        });
    }

    protected override void OnStarted()
    {
        Global.MainForm.BeginInvoke(() => _form.Show());
    }

    protected override void OnStartFailed()
    {
        if (new FileInfo(LogPath).Length == 0)
        {
            Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    Utils.Utils.Open("https://github.com/zhxie/pcap2socks#dependencies");
                })
                .Forget();

            throw new MessageException("Pleases install pcap2socks's dependency");
        }

        Utils.Utils.Open(LogPath);
    }
}