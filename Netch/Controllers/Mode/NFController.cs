using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Netch.Forms;
using Netch.Models;
using Netch.Utils;
using nfapinet;

namespace Netch.Controllers
{
    public class NFController : ModeController
    {
        /// <summary>
        ///     流量变动处理器
        /// </summary>
        /// <param name="upload">上传</param>
        /// <param name="download">下载</param>
        public delegate void BandwidthUpdateHandler(long upload, long download);

        private readonly string _binDriverPath;

        private readonly string _driverPath = $"{Environment.SystemDirectory}\\drivers\\netfilter2.sys";
        private readonly ServiceController _service = new ServiceController("netfilter2");
        private string _systemDriverVersion;

        public NFController()
        {
            MainFile = "Redirector";
            InitCheck();
            // 生成系统版本
            var winNTver = $"{Environment.OSVersion.Version.Major.ToString()}.{Environment.OSVersion.Version.Minor.ToString()}";
            var driverName = "";
            switch (winNTver)
            {
                case "10.0":
                    driverName = "Win-10.sys";
                    break;
                case "6.3":
                case "6.2":
                    driverName = "Win-8.sys";
                    break;
                case "6.1":
                case "6.0":
                    driverName = "Win-7.sys";
                    break;
                default:
                    Logging.Error($"不支持的系统版本：{winNTver}");
                    Ready = false;
                    return;
            }

            _binDriverPath = "bin\\" + driverName;

            if (!File.Exists(_driverPath))
            {
                InstallDriver();
            }
            // 驱动版本
            _systemDriverVersion = FileVersionInfo.GetVersionInfo(_driverPath).FileVersion;
        }

        /// <summary>
        ///     流量变动事件
        /// </summary>
        public event BandwidthUpdateHandler OnBandwidthUpdated;

        public override bool Start(Server server, Mode mode)
        {
            if (!CheckDriverReady())
            {
                if (File.Exists(_driverPath))
                    UninstallDriver();
                if (!InstallDriver())
                    return false;
            }

            var processList = "";
            foreach (var proc in mode.Rule)
                processList += proc + ",";
            processList += "NTT.exe";

            Instance = GetProcess("bin\\Redirector.exe");
            if (server.Type != "Socks5")
            {
                Instance.StartInfo.Arguments += $"-r 127.0.0.1:{Global.Settings.Socks5LocalPort} -p \"{processList}\"";
            }

            else
            {
                var result = DNS.Lookup(server.Hostname);
                if (result == null)
                {
                    Logging.Info("无法解析服务器 IP 地址");
                    return false;
                }

                Instance.StartInfo.Arguments += $"-r {result}:{server.Port} -p \"{processList}\"";
                if (!string.IsNullOrWhiteSpace(server.Username) && !string.IsNullOrWhiteSpace(server.Password)) Instance.StartInfo.Arguments += $" -username \"{server.Username}\" -password \"{server.Password}\"";
            }

            Instance.StartInfo.Arguments += $" -t {Global.Settings.RedirectorTCPPort}";
            Instance.OutputDataReceived += OnOutputDataReceived;
            Instance.ErrorDataReceived += OnOutputDataReceived;

            for (var i = 0; i < 2; i++)
            {
                State = State.Starting;
                Instance.Start();
                Instance.BeginOutputReadLine();
                Instance.BeginErrorReadLine();

                for (var j = 0; j < 40; j++)
                {
                    Thread.Sleep(250);

                    if (State == State.Started) return true;
                }

                Logging.Error("NF 进程启动超时");
                Stop();
                if (!RestartService()) return false;
            }

            return false;
        }

        private bool RestartService()
        {
            try
            {
                switch (_service.Status)
                {
                    // 启动驱动服务
                    case ServiceControllerStatus.Running:
                        // 防止其他程序占用 重置 NF 百万连接数限制
                        _service.Stop();
                        _service.WaitForStatus(ServiceControllerStatus.Stopped);
                        MainForm.Instance.StatusText(i18N.Translate("Starting netfilter2 Service"));
                        _service.Start();
                        break;
                    case ServiceControllerStatus.Stopped:
                        MainForm.Instance.StatusText(i18N.Translate("Starting netfilter2 Service"));
                        _service.Start();
                        break;
                }
            }
            catch (Exception e)
            {
                Logging.Error("启动驱动服务失败：\n" + e);

                var result = NFAPI.nf_registerDriver("netfilter2");
                if (result != NF_STATUS.NF_STATUS_SUCCESS)
                {
                    Logging.Error($"注册驱动失败，返回值：{result}");
                    return false;
                }

                Logging.Info("注册驱动成功");
            }

            return true;
        }

        private bool CheckDriverReady()
        {
            // 检查驱动是否存在
            if (!File.Exists(_driverPath)) return false;

            // 检查驱动版本号
            var binVersion = FileVersionInfo.GetVersionInfo(_binDriverPath).FileVersion;
            return _systemDriverVersion.Equals(binVersion);
        }

        public bool UninstallDriver()
        {
            try
            {
                var service = new ServiceController("netfilter2");
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            if (!File.Exists(_driverPath)) return true;
            try
            {
                NFAPI.nf_unRegisterDriver("netfilter2");

                File.Delete(_driverPath);
                _systemDriverVersion = "";
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool InstallDriver()
        {
            if (!Ready) return false;
            Logging.Info("安装驱动中");
            try
            {
                File.Copy(_binDriverPath, _driverPath);
            }
            catch (Exception e)
            {
                Logging.Error("驱动复制失败\n" + e);
                return false;
            }

            MainForm.Instance.StatusText(i18N.Translate("Register driver"));
            // 注册驱动文件
            var result = NFAPI.nf_registerDriver("netfilter2");
            if (result == NF_STATUS.NF_STATUS_SUCCESS)
            {
                _systemDriverVersion = FileVersionInfo.GetVersionInfo(_driverPath).FileVersion;
                Logging.Info($"驱动安装成功，当前驱动版本:{_systemDriverVersion}");
            }
            else
            {
                Logging.Error($"注册驱动失败，返回值：{result}");
                return false;
            }

            return true;
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!WriteLog(e)) return;
            if (State == State.Starting)
            {
                if (Instance.HasExited)
                    State = State.Stopped;
                else if (e.Data.Contains("Started"))
                    State = State.Started;
                else if (e.Data.Contains("Failed") || e.Data.Contains("Unable")) State = State.Stopped;
            }
            else if (State == State.Started)
            {
                if (e.Data.StartsWith("[APP][Bandwidth]"))
                {
                    var splited = e.Data.Replace("[APP][Bandwidth]", "").Trim().Split(',');
                    if (splited.Length == 2)
                    {
                        var uploadSplited = splited[0].Split(':');
                        var downloadSplited = splited[1].Split(':');

                        if (uploadSplited.Length == 2 && downloadSplited.Length == 2)
                            if (long.TryParse(uploadSplited[1], out var upload) && long.TryParse(downloadSplited[1], out var download))
                                Task.Run(() => OnBandwidthUpdated(upload, download));
                    }
                }
            }
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}