using System;
using System.Drawing;
using System.IO;
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

        public void InitServer()
        {
            ServerComboBox.Items.Clear();
            ServerComboBox.Items.AddRange(Global.Settings.Server.ToArray());

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

        private void InitMode()
        {
            ModeComboBox.Items.Clear();
            Global.ModeFiles.Clear();

            if (Directory.Exists("mode"))
            {
                foreach (var name in Directory.GetFiles("mode", "*.txt"))
                {
                    var ok = true;
                    var mode = new Models.Mode();

                    using (var sr = new StringReader(File.ReadAllText(name)))
                    {
                        var i = 0;
                        string text;

                        while ((text = sr.ReadLine()) != null)
                        {
                            if (i == 0)
                            {
                                var splited = text.Trim().Substring(1).Split(',');

                                if (splited.Length == 0)
                                {
                                    ok = false;
                                    break;
                                }

                                if (splited.Length >= 1)
                                {
                                    mode.Remark = i18N.Translate(splited[0].Trim());
                                }

                                if (splited.Length >= 2)
                                {
                                    if (int.TryParse(splited[1], out var result))
                                    {
                                        mode.Type = result;
                                    }
                                    else
                                    {
                                        ok = false;
                                        break;
                                    }
                                }

                                if (splited.Length >= 3)
                                {
                                    if (int.TryParse(splited[2], out var result))
                                    {
                                        mode.BypassChina = result == 1;
                                    }
                                    else
                                    {
                                        ok = false;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (!text.StartsWith("#") && !string.IsNullOrWhiteSpace(text))
                                {
                                    mode.Rule.Add(text.Trim());
                                }
                            }

                            i++;
                        }
                    }

                    if (ok)
                    {
                        mode.FileName = Path.GetFileNameWithoutExtension(name);
                        Global.ModeFiles.Add(mode);
                    }
                }

                var array = Global.ModeFiles.ToArray();
                Array.Sort(array, (a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal));

                ModeComboBox.Items.AddRange(array);

                SelectLastMode();
            }
        }

        private void SelectLastMode()
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

        public void AddMode(Models.Mode mode)
        {
            ModeComboBox.Items.Clear();
            Global.ModeFiles.Add(mode);
            var array = Global.ModeFiles.ToArray();
            Array.Sort(array, (a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal));
            ModeComboBox.Items.AddRange(array);

            SelectLastMode();
        }

        public void UpdateMode(Models.Mode NewMode, Models.Mode OldMode)
        {
            ModeComboBox.Items.Clear();
            Global.ModeFiles.Remove(OldMode);
            Global.ModeFiles.Add(NewMode);
            var array = Global.ModeFiles.ToArray();
            Array.Sort(array, (a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal));
            ModeComboBox.Items.AddRange(array);

            SelectLastMode();
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
    }
}