using System.ComponentModel;
using Netch.Utils;

namespace Netch.Forms;

/// <summary>WinForms-only helpers that used to live in the core Utils / i18N classes.</summary>
public static class WinFormsUtils
{
    public static void DrawCenterComboBox(object sender, DrawItemEventArgs e)
    {
        if (sender is ComboBox cbx)
        {
            e.DrawBackground();

            if (e.Index < 0)
                return;

            TextRenderer.DrawText(e.Graphics, cbx.Items[e.Index].ToString(), cbx.Font, e.Bounds, (e.State & DrawItemState.Selected) == DrawItemState.Selected ? SystemColors.HighlightText : cbx.ForeColor, TextFormatFlags.HorizontalCenter);
        }
    }

    public static void ComponentIterator(in Component component, in Action<Component> func)
    {
        func.Invoke(component);
        switch (component)
        {
            case ListView listView:
                // ListView sub item
                foreach (var item in listView.Columns.Cast<ColumnHeader>())
                    ComponentIterator(item, func);

                break;
            case ToolStripMenuItem toolStripMenuItem:
                // Iterator Menu strip sub item
                foreach (var item in toolStripMenuItem.DropDownItems.Cast<ToolStripItem>())
                    ComponentIterator(item, func);

                break;
            case MenuStrip menuStrip:
                // Menu Strip
                foreach (var item in menuStrip.Items.Cast<ToolStripItem>())
                    ComponentIterator(item, func);

                break;
            case ContextMenuStrip contextMenuStrip:
                foreach (var item in contextMenuStrip.Items.Cast<ToolStripItem>())
                    ComponentIterator(item, func);

                break;
            case Control control:
                foreach (var c in control.Controls.Cast<Control>())
                    ComponentIterator(c, func);

                if (control.ContextMenuStrip != null)
                    ComponentIterator(control.ContextMenuStrip, func);

                break;
        }
    }

    public static void ChangeControlForeColor(Component component, Color color)
    {
        switch (component)
        {
            case TextBox _:
            case ComboBox _:
                if (((Control)component).ForeColor != color)
                    ((Control)component).ForeColor = color;

                break;
        }
    }

    public static void TranslateForm(in Control c)
    {
        ComponentIterator(c,
            component =>
            {
                switch (component)
                {
                    case TextBoxBase:
                    case ListControl:
                        break;
                    case Control control:
                        control.Text = i18N.Translate(control.Text);
                        break;
                    case ToolStripItem toolStripItem:
                        toolStripItem.Text = i18N.Translate(toolStripItem.Text);
                        break;
                    case ColumnHeader columnHeader:
                        columnHeader.Text = i18N.Translate(columnHeader.Text);
                        break;
                }
            });
    }
}
