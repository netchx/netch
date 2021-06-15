using System;

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
                    {
                        if (m.Type == Models.Mode.ModeType.ProcessMode)
                        {
                            var node = s as Models.Server.Shadowsocks.Shadowsocks;
                            if (String.IsNullOrEmpty(node.OBFS))
                            {
                                break;
                            }
                        }

                        this.NodeController = new Server.SSController();
                    }
                    break;
                case Models.Server.ServerType.ShadowsocksR:
                    this.NodeController = new Server.SRController();
                    break;
                case Models.Server.ServerType.Trojan:
                    this.NodeController = new Server.TRController();
                    break;
                case Models.Server.ServerType.VLess:
                    this.NodeController = new Server.VLController();
                    break;
                case Models.Server.ServerType.VMess:
                    this.NodeController = new Server.VMController();
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
                case Models.Mode.ModeType.TapMode:
                    this.ModeController = new Mode.TapController();
                    break;
                case Models.Mode.ModeType.TunMode:
                    this.ModeController = new Mode.TunController();
                    break;
                case Models.Mode.ModeType.WebMode:
                    this.ModeController = new Mode.WebController();
                    break;
                case Models.Mode.ModeType.WmpMode:
                    this.ModeController = new Mode.WmpController();
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
