using System.ComponentModel;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace Netch.Forms;

[Fody.ConfigureAwait(true)]
public partial class LogForm : Form
{
    private readonly Form _parent;

    public LogForm(Form parent)
    {
        InitializeComponent();
        _parent = parent;
    }

    protected override void OnLoad(EventArgs? e)
    {
        base.OnLoad(e);
        Parent_Move(null!, null!);
    }

    private void Parent_Move(object? sender, EventArgs? e)
    {
        var cl = Location;
        var fl = _parent.Location;

        cl.X = fl.X + _parent.Width;
        cl.Y = fl.Y;
        Location = cl;
    }

    private void Parent_Activated(object? sender, EventArgs? e)
    {
        SetWindowPos(new HWND(Handle),
            new HWND(-1),
            0,
            0,
            0,
            0,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);

        SetWindowPos(new HWND(Handle),
            new HWND(-2),
            0,
            0,
            0,
            0,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
    }

    private void richTextBox1_TextChanged(object? sender, EventArgs? e)
    {
        if (!checkBox1.Checked)
            return;

        richTextBox1.SelectionStart = richTextBox1.Text.Length;
        richTextBox1.ScrollToCaret();
    }

    private void LogForm_Load(object? sender, EventArgs? e)
    {
        _parent.LocationChanged += Parent_Move;
        _parent.SizeChanged += Parent_Move;
        _parent.Activated += Parent_Activated;
        _parent.VisibleChanged += Parent_VisibleChanged;
    }

    private void Parent_VisibleChanged(object? sender, EventArgs e)
    {
        Visible = _parent.Visible;
    }

    protected override void OnClosing(CancelEventArgs? e)
    {
        _parent.LocationChanged -= Parent_Move;
        _parent.SizeChanged -= Parent_Move;
        _parent.Activated -= Parent_Activated;
        _parent.VisibleChanged -= Parent_VisibleChanged;
        base.OnClosing(e!);
    }
}