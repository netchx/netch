using Netch.Services;

namespace Netch.Forms;

// Bridges core notifications (AppEvents) to the WinForms UI. The core no longer references
// MainForm directly so the same core can drive the WinUI front end.
public partial class MainForm
{
    private static readonly Lazy<MainForm> LazyInstance = new(() => new MainForm());

    public static MainForm Instance => LazyInstance.Value;

    private LogForm? _shareModeLogForm;

    public static void ActivateVisibleWindows()
    {
        var forms = Application.OpenForms.Cast<Form>().Where(f => f.Visible).ToList();
        if (!forms.Any())
        {
            Instance.Show();
            Instance.WindowState = FormWindowState.Normal;
            Instance.Activate();
        }
        else
        {
            foreach (var f in forms)
            {
                f.WindowState = FormWindowState.Normal;
                f.Activate();
            }
        }
    }

    private void SubscribeAppEvents()
    {
        AppEvents.StatusReported += text => StatusText(text);
        AppEvents.NotificationRequested += (text, info) => NotifyTip(text, info: info);
        AppEvents.ModesChanged += LoadModes;
        AppEvents.ModeDeleted += OnModeDeleted;
        AppEvents.BandwidthAvailabilityChanged += BandwidthState;
        AppEvents.BandwidthUpdated += OnBandwidthUpdated;

        AppEvents.ShareModeLogOpened += () => Invoke(() =>
        {
            _shareModeLogForm?.Dispose();
            _shareModeLogForm = new LogForm(this);
            _shareModeLogForm.CreateControl();
        });
        AppEvents.ShareModeLogShown += () => BeginInvoke(() => _shareModeLogForm?.Show());
        AppEvents.ShareModeLogLine += line => BeginInvoke(() =>
        {
            if (_shareModeLogForm is { IsDisposed: false } form)
                form.richTextBox1.AppendText(line + "\n");
        });
        AppEvents.ShareModeLogClosed += () => Invoke(() =>
        {
            _shareModeLogForm?.Close();
            _shareModeLogForm?.Dispose();
            _shareModeLogForm = null;
        });
    }

    private void OnModeDeleted(Models.Modes.Mode mode)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnModeDeleted(mode));
            return;
        }

        ModeComboBox.Items.Remove(mode);
    }
}
