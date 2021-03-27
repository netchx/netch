using System;
using System.ComponentModel;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace Netch.Forms
{
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
            SetWindowPos(Handle,
                HWND.HWND_TOPMOST,
                0,
                0,
                0,
                0,
                SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_SHOWWINDOW);

            SetWindowPos(Handle,
                HWND.HWND_NOTOPMOST,
                0,
                0,
                0,
                0,
                SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE | SetWindowPosFlags.SWP_SHOWWINDOW);
        }

        private void richTextBox1_TextChanged(object? sender, EventArgs? e)
        {
            if (!checkBox1.Checked)
                return;

            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        private void Notifycation_Load(object? sender, EventArgs? e)
        {
            _parent.LocationChanged += Parent_Move;
            _parent.SizeChanged += Parent_Move;
            _parent.Activated += Parent_Activated;
        }

        protected override void OnClosing(CancelEventArgs? e)
        {
            _parent.Activated -= Parent_Activated;
            _parent.LocationChanged -= Parent_Move;
            _parent.SizeChanged -= Parent_Move;
            base.OnClosing(e!);
        }
    }
}