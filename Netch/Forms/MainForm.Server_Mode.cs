using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Netch.Utils;

namespace Netch.Forms
{
    public partial class Dummy
    {
    }

    partial class MainForm
    {
        #region Server

        private void InitServer()
        {
            var comboBoxInitialized = _comboBoxInitialized;
            _comboBoxInitialized = false;

            ServerComboBox.Items.Clear();
            ServerComboBox.Items.AddRange(Global.Settings.Server.ToArray());
            SelectLastServer();
            _comboBoxInitialized = comboBoxInitialized;
        }

        private static void TestServer()
        {
            try
            {
                Parallel.ForEach(Global.Settings.Server, new ParallelOptions {MaxDegreeOfParallelism = 16},
                    server => { server.Test(); });
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void SelectLastServer()
        {
            // 如果值合法，选中该位置
            if (Global.Settings.ServerComboBoxSelectedIndex > 0 &&
                Global.Settings.ServerComboBoxSelectedIndex < ServerComboBox.Items.Count)
            {
                ServerComboBox.SelectedIndex = Global.Settings.ServerComboBoxSelectedIndex;
            }
            // 如果值非法，且当前 ServerComboBox 中有元素，选择第一个位置
            else if (ServerComboBox.Items.Count > 0)
            {
                ServerComboBox.SelectedIndex = 0;
            }

            // 如果当前 ServerComboBox 中没元素，不做处理
        }

        #endregion

        #region Mode

        public void InitMode()
        {
            var comboBoxInitialized = _comboBoxInitialized;
            _comboBoxInitialized = false;

            ModeComboBox.Items.Clear();
            ModeComboBox.Items.AddRange(Global.Modes.ToArray());
            ModeComboBox.Tag = null;
            SelectLastMode();
            _comboBoxInitialized = comboBoxInitialized;
        }

        public void SelectLastMode()
        {
            // 如果值合法，选中该位置
            if (Global.Settings.ModeComboBoxSelectedIndex > 0 &&
                Global.Settings.ModeComboBoxSelectedIndex < ModeComboBox.Items.Count)
            {
                ModeComboBox.SelectedIndex = Global.Settings.ModeComboBoxSelectedIndex;
            }
            // 如果值非法，且当前 ModeComboBox 中有元素，选择第一个位置
            else if (ModeComboBox.Items.Count > 0)
            {
                ModeComboBox.SelectedIndex = 0;
            }

            // 如果当前 ModeComboBox 中没元素，不做处理
        }

        #endregion

        /// <summary>
        ///     Init at <see cref="MainForm_Load"/>
        /// </summary>
        private int _eWidth;

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {
                if (!(sender is ComboBox cbx))
                {
                    return;
                }

                // 绘制背景颜色
                e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);

                if (e.Index < 0) return;

                // 绘制 备注/名称 字符串
                e.Graphics.DrawString(cbx.Items[e.Index].ToString(), cbx.Font, new SolidBrush(Color.Black), e.Bounds);

                switch (cbx.Items[e.Index])
                {
                    case Models.Server item:
                    {
                        // 计算延迟底色
                        SolidBrush brush;
                        if (item.Delay > 200)
                            brush = new SolidBrush(Color.Red);
                        else if (item.Delay > 80)
                            brush = new SolidBrush(Color.Yellow);
                        else if (item.Delay >= 0)
                            brush = new SolidBrush(Color.FromArgb(50, 255, 56));
                        else
                            brush = new SolidBrush(Color.Gray);

                        // 绘制延迟底色
                        e.Graphics.FillRectangle(brush, _eWidth * 9, e.Bounds.Y, _eWidth, e.Bounds.Height);

                        // 绘制延迟字符串
                        e.Graphics.DrawString(item.Delay.ToString(), cbx.Font, new SolidBrush(Color.Black),
                            _eWidth * 9 + _eWidth / 30, e.Bounds.Y);
                        break;
                    }
                    case Models.Mode item:
                    {
                        // 绘制 模式Box 底色
                        e.Graphics.FillRectangle(new SolidBrush(Color.Gray), _eWidth * 9, e.Bounds.Y, _eWidth,
                            e.Bounds.Height);

                        // 绘制 模式行数 字符串
                        e.Graphics.DrawString(item.Rule.Count.ToString(), cbx.Font, new SolidBrush(Color.Black),
                            _eWidth * 9 + _eWidth / 30, e.Bounds.Y);
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void AddAddServerToolStripMenuItems()
        {
            foreach (var serversUtil in ServerHelper.ServerUtils.Where(i => !string.IsNullOrEmpty(i.FullName)))
            {
                var fullName = serversUtil.FullName;
                var control = new ToolStripMenuItem
                {
                    Name = $"Add{fullName}ServerToolStripMenuItem",
                    Size = new Size(259, 22),
                    Text = i18N.TranslateFormat("Add [{0}] Server", fullName),
                };
                _mainFormText.Add(control.Name, new[] {"Add [{0}] Server", fullName});
                control.Click += AddServerToolStripMenuItem_Click;
                ServerToolStripMenuItem.DropDownItems.Add(control);
            }
        }
    }
}