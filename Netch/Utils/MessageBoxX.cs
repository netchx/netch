using System.Windows.Forms;

namespace Netch.Utils
{
    class MessageBoxX
    {
        /// <summary>
        /// </summary>
        /// <param name="text">内容</param>
        /// <param name="title">自定义标题</param>
        /// <param name="info">弹窗等级 (标题, 图标)</param>
        /// <param name="confirm">需要确认</param>
        /// <param name="owner">阻止 owner Focus() 直到 Messageox 被关闭</param>
        public static DialogResult Show(string text, string title = "", bool info = true, bool confirm = false,IWin32Window owner = null)
        {
            return MessageBox.Show(
                owner: owner,
                text: text,
                caption: i18N.Translate(string.IsNullOrWhiteSpace(title) ? (info ? "Information" : "Error") : title),
                buttons: confirm ? MessageBoxButtons.OKCancel : MessageBoxButtons.OK,
                icon: info ? MessageBoxIcon.Information : MessageBoxIcon.Exclamation);
        }
    }
}