using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Netch.Forms;
using Netch.Interfaces;
using Netch.Models;
using Netch.Models.Adapter;
using Netch.Servers.Socks5;

namespace Netch.Controllers
{
    public class PcapController : Guard, IModeController
    {
        public override string Name { get; } = "pcap2socks";

        public override string MainFile { get; protected set; } = "pcap2socks.exe";

        protected override IEnumerable<string> StartedKeywords { get; set; } = new[] { "└" };

        private readonly OutboundAdapter _outbound = new();

        protected override Encoding? InstanceOutputEncoding { get; } = Encoding.UTF8;

        private LogForm? _form;

        public void Start(in Mode mode)
        {
            var server = MainController.Server!;

            _form = new LogForm(Global.MainForm);
            _form.CreateControl();

            var argument = new StringBuilder($@"-i \Device\NPF_{_outbound.NetworkInterface.Id}");
            if (server is Socks5 socks5 && !socks5.Auth())
                argument.Append($" --destination  {server.AutoResolveHostname()}:{server.Port}");
            else
                argument.Append($" --destination  127.0.0.1:{Global.Settings.Socks5LocalPort}");

            argument.Append($" {mode.FullRule.FirstOrDefault() ?? "-P n"}");
            StartInstanceAuto(argument.ToString());
        }

        protected override void OnReadNewLine(string line)
        {
            Global.MainForm.BeginInvoke(new Action(() =>
            {
                if (!_form!.IsDisposed)
                    _form.richTextBox1.AppendText(line + "\n");
            }));
        }

        protected override void OnKeywordStarted()
        {
            Global.MainForm.BeginInvoke(new Action(() => { _form!.Show(); }));
        }

        protected override void OnKeywordStopped()
        {
            if (File.ReadAllText(LogPath).Length == 0)
            {
                Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    Utils.Utils.Open("https://github.com/zhxie/pcap2socks#dependencies");
                });

                throw new MessageException("Pleases install pcap2socks's dependency");
            }

            Utils.Utils.Open(LogPath);
        }

        public override void Stop()
        {
            _form!.Close();
            StopInstance();
        }
    }
}