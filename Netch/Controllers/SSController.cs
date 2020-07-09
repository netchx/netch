using System.Diagnostics;
using System.Text;
using System.Threading;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers
{
    public class SSController : ServerClient
    {
        public SSController()
        {
            MainName = "Shadowsocks";
            ready = BeforeStartProgress();
        }

        public override bool Start(Server server, Mode mode)
        {
            //从DLL启动Shaowsocks
            if (Global.Settings.BootShadowsocksFromDLL && (mode.Type == 0 || mode.Type == 1 || mode.Type == 2 || mode.Type == 3))
            {
                State = State.Starting;
                var client = Encoding.UTF8.GetBytes($"0.0.0.0:{Global.Settings.Socks5LocalPort}");
                var remote = Encoding.UTF8.GetBytes($"{server.Hostname}:{server.Port}");
                var passwd = Encoding.UTF8.GetBytes($"{server.Password}");
                var method = Encoding.UTF8.GetBytes($"{server.EncryptMethod}");
                if (!NativeMethods.Shadowsocks.Info(client, remote, passwd, method))
                {
                    State = State.Stopped;
                    Logging.Info("DLL SS INFO 设置失败！");
                    return false;
                }

                Logging.Info("DLL SS INFO 设置成功！");

                if (!NativeMethods.Shadowsocks.Start())
                {
                    State = State.Stopped;
                    Logging.Info("DLL SS 启动失败！");
                    return false;
                }

                Logging.Info("DLL SS 启动成功！");
                State = State.Started;
                return true;
            }

            Instance = MainController.GetProcess("bin\\Shadowsocks.exe");

            #region Instance.Arguments

            if (!string.IsNullOrWhiteSpace(server.Plugin) && !string.IsNullOrWhiteSpace(server.PluginOption))
                Instance.StartInfo.Arguments = $"-s {server.Hostname} -p {server.Port} -b {Global.Settings.LocalAddress} -l {Global.Settings.Socks5LocalPort} -m {server.EncryptMethod} -k \"{server.Password}\" -u --plugin {server.Plugin} --plugin-opts \"{server.PluginOption}\"";
            else
                Instance.StartInfo.Arguments = $"-s {server.Hostname} -p {server.Port} -b {Global.Settings.LocalAddress} -l {Global.Settings.Socks5LocalPort} -m {server.EncryptMethod} -k \"{server.Password}\" -u";
            if (mode.BypassChina) Instance.StartInfo.Arguments += " --acl default.acl";

            #endregion

            Instance.OutputDataReceived += OnOutputDataReceived;
            Instance.ErrorDataReceived += OnOutputDataReceived;

            State = State.Starting;
            Instance.Start();
            Instance.BeginOutputReadLine();
            Instance.BeginErrorReadLine();
            for (var i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);

                if (State == State.Started) return true;

                if (State == State.Stopped)
                {
                    Logging.Info("SS 进程启动失败");

                    Stop();
                    return false;
                }
            }

            Logging.Info("SS 进程启动超时");
            Stop();
            return false;
        }

        /// <summary>
        ///     SSController 停止
        /// </summary>
        public new void Stop()
        {
            base.Stop();
            if (Global.Settings.BootShadowsocksFromDLL) NativeMethods.Shadowsocks.Stop();
        }

        public override void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!WriteLog(e)) return;
            if (State == State.Starting)
            {
                if (Instance.HasExited)
                    State = State.Stopped;
                else if (e.Data.Contains("listening at"))
                    State = State.Started;
                else if (e.Data.Contains("Invalid config path") || e.Data.Contains("usage") || e.Data.Contains("plugin service exit unexpectedly")) State = State.Stopped;
            }
        }
    }
}