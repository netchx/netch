using System.ComponentModel;
using System.Windows.Forms;
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
    [DesignerCategory("")] public partial class Dummy { }
    partial class MainForm
    {
        private void CheckUpdate()
        {
            var updater = new UpdateChecker();
            updater.NewVersionFound += (o, args) =>
            {
                NotifyIcon.ShowBalloonTip(5,
                    UpdateChecker.Name,
                    $"{i18N.Translate(@"New version available",": ")}{updater.LatestVersionNumber}",
                    ToolTipIcon.Info);
            };
            updater.Check(false, false);
        }


    }
}