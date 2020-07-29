using System.ComponentModel;
using System.Threading.Tasks;
using Netch.Controllers;
using Netch.Utils;

namespace Netch.Forms
{
    /// <summary lang="en">
    /// this class is used to disable Designer <para />
    /// </summary>
    /// <summary lang="zh">
    /// 此类用于禁用设计器
    /// </summary>
    [DesignerCategory("")]
    public partial class Dummy
    {
    }

    partial class MainForm
    {
        private void CheckUpdate()
        {
            var updater = new UpdateChecker();
            updater.NewVersionFound += (o, args) => { NotifyTip($"{i18N.Translate(@"New version available", ": ")}{updater.LatestVersionNumber}"); };
            updater.Check(false, Global.Settings.CheckBetaUpdate);
        }
    }
}