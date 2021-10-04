namespace Netch.Controllers
{
    public class MainController : Interface.IController
    {
        /// <summary>
        ///     节点控制器
        /// </summary>
        private Interface.IController NodeController;

        /// <summary>
        ///     模式控制器
        /// </summary>
        private Interface.IController ModeController;

        public bool Create(Models.Server.Server s, Models.Mode.Mode m)
        {
            switch (s.Type)
            {
                case Models.Server.ServerType.Socks:
                    break;
                case Models.Server.ServerType.Shadowsocks:
                    this.NodeController = new Server.ShadowsocksController();
                    break;
                case Models.Server.ServerType.ShadowsocksR:
                    this.NodeController = new Server.ShadowsocksRController();
                    break;
                case Models.Server.ServerType.WireGuard:
                    this.NodeController = new Server.WireGuardController();
                    break;
                case Models.Server.ServerType.Trojan:
                    this.NodeController = new Server.TrojanController();
                    break;
                case Models.Server.ServerType.VMess:
                    this.NodeController = new Server.VMessController();
                    break;
                case Models.Server.ServerType.VLess:
                    this.NodeController = new Server.VLessController();
                    break;
                default:
                    Global.Logger.Error($"未知的节点类型：{s.Type}");

                    return false;
            }

            {
                var status = this.NodeController?.Create(s, m);
                if (status.HasValue && !status.Value)
                {
                    return false;
                }
            }

            switch (m.Type)
            {
                case Models.Mode.ModeType.ProcessMode:
                    this.ModeController = new Mode.ProcessController();
                    break;
                case Models.Mode.ModeType.ShareMode:
                    this.ModeController = new Mode.ShareController();
                    break;
                case Models.Mode.ModeType.TunMode:
                    this.ModeController = new Mode.TunController();
                    break;
                case Models.Mode.ModeType.WebMode:
                    this.ModeController = new Mode.WebController();
                    break;
                default:
                    Global.Logger.Error($"未知的模式类型：{s.Type}");

                    return false;
            }

            {
                var status = this.ModeController?.Create(s, m);
                if (status.HasValue && !status.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Delete()
        {
            this.NodeController?.Delete();
            this.ModeController?.Delete();

            return true;
        }
    }
}
