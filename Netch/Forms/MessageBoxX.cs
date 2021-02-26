using System;
using System.Windows.Forms;
using Netch.Models;
using Netch.Utils;

namespace Netch.Forms
{
    public static class MessageBoxX
    {
        /// <summary>
        /// </summary>
        /// <param name="text">内容</param>
        /// <param name="title">自定义标题</param>
        /// <param name="level">弹窗等级 (标题, 图标)</param>
        /// <param name="confirm">需要确认</param>
        /// <param name="owner">阻止 owner Focus() 直到 Messageox 被关闭</param>
        public static DialogResult Show(string text,
            LogLevel level = LogLevel.INFO,
            string title = "",
            bool confirm = false,
            IWin32Window? owner = null)
        {
            MessageBoxIcon msgIcon;
            if (string.IsNullOrWhiteSpace(title))
                title = level switch
                        {
                            LogLevel.INFO => "Information",
                            LogLevel.WARNING => "Warning",
                            LogLevel.ERROR => "Error",
                            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
                        };

            msgIcon = level switch
                      {
                          LogLevel.INFO => MessageBoxIcon.Information,
                          LogLevel.WARNING => MessageBoxIcon.Warning,
                          LogLevel.ERROR => MessageBoxIcon.Exclamation,
                          _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
                      };

            return MessageBox.Show(owner, text, i18N.Translate(title), confirm ? MessageBoxButtons.OKCancel : MessageBoxButtons.OK, msgIcon);
        }
    }
}