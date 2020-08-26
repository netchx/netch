using System.Text;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers
{
    public class SSController : EncryptedProxy
    {
        private bool dllFlag = false;

        public SSController()
        {
            Name = "Shadowsocks";
            MainFile = "Shadowsocks.exe";
            StartedKeywords.Add("listening at");
            StoppedKeywords.AddRange(new[] {"Invalid config path", "usage", "plugin service exit unexpectedly"});
        }

        public override bool Start(Server server, Mode mode)
        {
            //从DLL启动Shaowsocks
            if (Global.Settings.BootShadowsocksFromDLL && (mode.Type == 0 || mode.Type == 1 || mode.Type == 2))
            {
                dllFlag = true;
                State = State.Starting;
                var client = Encoding.UTF8.GetBytes($"{LocalAddress}:{Socks5LocalPort}");
                var remote = Encoding.UTF8.GetBytes($"{server.Hostname}:{server.Port}");
                var passwd = Encoding.UTF8.GetBytes($"{server.Password}");
                var method = Encoding.UTF8.GetBytes($"{server.EncryptMethod}");
                if (!NativeMethods.Shadowsocks.Info(client, remote, passwd, method))
                {
                    State = State.Stopped;
                    Logging.Error("DLL SS INFO 设置失败！");
                    return false;
                }

                Logging.Info("DLL SS INFO 设置成功！");

                if (!NativeMethods.Shadowsocks.Start())
                {
                    State = State.Stopped;
                    Logging.Error("DLL SS 启动失败！");
                    return false;
                }

                Logging.Info("DLL SS 启动成功！");
                State = State.Started;
                return true;
            }

            #region Argument

            var argument = new StringBuilder();
            argument.Append(
                $"-s {server.Hostname} -p {server.Port} -b {LocalAddress} -l {Socks5LocalPort} -m {server.EncryptMethod} -k \"{server.Password}\" -u");
            if (!string.IsNullOrWhiteSpace(server.Plugin) && !string.IsNullOrWhiteSpace(server.PluginOption))
                argument.Append($" --plugin {server.Plugin} --plugin-opts \"{server.PluginOption}\"");
            if (mode.BypassChina) argument.Append(" --acl default.acl");

            #endregion

            return StartInstanceAuto(argument.ToString());
        }

        /// <summary>
        ///     SSController 停止
        /// </summary>
        public override void Stop()
        {
            if (dllFlag) NativeMethods.Shadowsocks.Stop();
            else
                StopInstance();
        }
    }
}