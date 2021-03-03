using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Netch.Models;

namespace Netch.Controllers
{
    public class PcapController : Guard, IModeController
    {
        public override string Name { get; } = "pcap2socks";

        public override string MainFile { get; protected set; } = "pcap2socks.exe";

        protected override IEnumerable<string> StartedKeywords { get; } = new[] {"└"};

        private readonly OutboundAdapter _outbound = new();

        protected override Encoding? InstanceOutputEncoding { get; } = Encoding.UTF8;

        public void Start(in Mode mode)
        {
            StartInstanceAuto($@"-i \Device\NPF_{_outbound.NetworkInterface.Id} {mode.FullRule.FirstOrDefault() ?? "-P n"}");
        }

        protected override void OnKeywordStarted()
        {
            Thread.Sleep(300);
            Utils.Utils.Open(LogPath);
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
            StopInstance();
        }
    }
}