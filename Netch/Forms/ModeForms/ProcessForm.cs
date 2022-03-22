using System.Net;
using Netch.Controllers;
using Netch.Enums;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
using Netch.Properties;
using Netch.Services;
using Netch.Utils;

namespace Netch.Forms.ModeForms;

[Fody.ConfigureAwait(true)]
public partial class ProcessForm : BindingForm
{
    private readonly bool IsCreateMode;

    private readonly Redirector _mode;

    /// <summary>
    ///     编辑模式
    /// </summary>
    /// <param name="mode">模式</param>
    public ProcessForm(Mode? mode = null)
    {
        switch (mode)
        {
            case Redirector processMode:
                IsCreateMode = false;
                _mode = processMode;
                break;
            case null:
                IsCreateMode = true;
                _mode = new Redirector();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        InitializeComponent();
        Icon = Resources.icon;
        InitBindings();

        var g = Global.Settings.Redirector;
        BindTextBox(RemarkTextBox, _ => true, s => _mode.i18NRemark = s, _mode.i18NRemark);
        BindSyncGlobalCheckBox(HandleTCPCheckBox, b => _mode.FilterTCP = b, _mode.FilterTCP, g.FilterTCP);
        BindSyncGlobalCheckBox(HandleUDPCheckBox, b => _mode.FilterUDP = b, _mode.FilterUDP, g.FilterUDP);
        BindSyncGlobalCheckBox(HandleDNSCheckBox, b => _mode.FilterDNS = b, _mode.FilterDNS, g.FilterDNS);
        BindTextBox(DNSTextBox, s => IPEndPoint.TryParse(s, out _), s => _mode.DNSHost = s, _mode.DNSHost ?? $"{Constants.DefaultPrimaryDNS}:53");

        BindSyncGlobalCheckBox(HandleProcDNSCheckBox, b => _mode.HandleOnlyDNS = b, _mode.HandleOnlyDNS, g.HandleOnlyDNS);
        BindSyncGlobalCheckBox(ProxyDNSCheckBox, b => _mode.DNSProxy = b, _mode.DNSProxy, g.DNSProxy);
        BindSyncGlobalCheckBox(HandleICMPCheckBox, b => _mode.FilterICMP = b, _mode.FilterICMP, g.FilterICMP);
        BindTextBox<int>(ICMPDelayTextBox, s => s >= 0, s => _mode.ICMPDelay = s, _mode.ICMPDelay ?? 10);
        BindCheckBox(HandleLoopbackCheckBox, b => _mode.FilterLoopback = b, _mode.FilterLoopback);
        BindCheckBox(HandleLANCheckBox, b => _mode.FilterIntranet = b, _mode.FilterIntranet);
        BindSyncGlobalCheckBox(HandleChildProcCheckBox, b => _mode.FilterParent = b, _mode.FilterParent, g.FilterParent);

        BindTextBox(BypassRuleRichTextBox, s => true, s => _mode.Bypass = s.GetLines().ToList(), string.Join(Constants.EOF, _mode.Bypass));
        BindTextBox(HandleRuleRichTextBox, s => true, s => _mode.Handle = s.GetLines().ToList(), string.Join(Constants.EOF, _mode.Handle));
    }

    private void InitBindings()
    {
        DNSTextBox.DataBindings.Add(new Binding("Enabled", HandleDNSCheckBox, "Checked", true));
        HandleProcDNSCheckBox.DataBindings.Add(new Binding("Enabled", HandleDNSCheckBox, "Checked", true));
        ProxyDNSCheckBox.DataBindings.Add(new Binding("Enabled", HandleDNSCheckBox, "Checked", true));
        ICMPDelayTextBox.DataBindings.Add(new Binding("Enabled", HandleICMPCheckBox, "Checked", true));
    }

    public void ModeForm_Load(object sender, EventArgs e)
    {
        if (!IsCreateMode)
        {
            Text = "Edit Process Mode";

            RemarkTextBox.TextChanged -= RemarkTextBox_TextChanged;
            RemarkTextBox.Text = _mode.i18NRemark;
            FilenameTextBox.Text = ModeService.Instance.GetRelativePath(_mode.FullName);

            if (!_mode.FullName.EndsWith(".json"))
                ControlButton.Enabled = false;
        }

        i18N.TranslateForm(this);
    }

    private void SelectButton_Click(object sender, EventArgs e)
    {
        RichTextBox ruleRichTextBox;
        if (sender == HandleSelectButton)
            ruleRichTextBox = HandleRuleRichTextBox;
        else if (sender == BypassSelectButton)
            ruleRichTextBox = BypassRuleRichTextBox;
        else
        {
            throw new InvalidOperationException();
        }

        var dialog = new FolderBrowserDialog();

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var path = dialog.SelectedPath;
            if (!path.EndsWith(@"\"))
                path += @"\";

            AppendText(ruleRichTextBox, $"^{path.ToRegexString()}");
        }
    }

    private static void AppendText(Control ruleTextBox, string value)
    {
        if (ruleTextBox.Text.Any())
            ruleTextBox.Text = ruleTextBox.Text.Trim() + Constants.EOF + value;
        else
            ruleTextBox.Text = value;
    }

    public void ControlButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RemarkTextBox.Text))
        {
            MessageBoxX.Show(i18N.Translate("Please enter a mode remark"));
            return;
        }

        SaveBinds();

        if (IsCreateMode)
        {
            var relativePath = FilenameTextBox.Text;
            var fullName = ModeService.Instance.GetFullPath(relativePath);
            if (File.Exists(fullName))
            {
                MessageBoxX.Show(i18N.Translate("File already exists.\n Please Change the filename"));
                return;
            }

            _mode.FullName = fullName;

            ModeService.Instance.Add(_mode);
            MessageBoxX.Show(i18N.Translate("Mode added successfully"));
        }
        else
        {
            _mode.WriteFile();
            MessageBoxX.Show(i18N.Translate("Mode updated successfully"));
        }
        Global.MainForm.ModeComboBox.SelectedItem = _mode;
        Close();
    }

    private void RemarkTextBox_TextChanged(object? sender, EventArgs? e)
    {
        if (!IsHandleCreated)
            return;

        BeginInvoke(() =>
        {
            FilenameTextBox.Text = FilenameTextBox.Text = ModeEditorUtils.GetCustomModeRelativePath(RemarkTextBox.Text);
        });
    }

    private void ScanButton_Click(object sender, EventArgs e)
    {
        RichTextBox ruleRichTextBox;
        if (sender == HandleScanButton)
            ruleRichTextBox = HandleRuleRichTextBox;
        else if (sender == BypassScanButton)
            ruleRichTextBox = BypassRuleRichTextBox;
        else
        {
            throw new InvalidOperationException();
        }

        var dialog = new FolderBrowserDialog();

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var path = dialog.SelectedPath;
            var list = new List<string>();
            const uint maxCount = 50;
            try
            {
                ScanDirectory(path, list, maxCount);
            }
            catch
            {
                MessageBoxX.Show(i18N.Translate($"The number of executable files in the \"{path}\" directory is greater than {maxCount}"),
                    LogLevel.WARNING);

                return;
            }

            AppendText(ruleRichTextBox, string.Join(Constants.EOF, list));
        }
    }

    private void ScanDirectory(string directory, List<string> list, uint maxCount = 30)
    {
        foreach (var dir in Directory.GetDirectories(directory))
            ScanDirectory(dir, list, maxCount);

        list.AddRange(
            Directory.GetFiles(directory).Select(s => Path.GetFileName(s)).Where(s => s.EndsWith(".exe")).Select(s => s.ToRegexString()));

        if (maxCount != 0 && list.Count > maxCount)
            throw new Exception("The number of results is greater than maxCount");
    }

    private void ValidationButton_Click(object sender, EventArgs e)
    {
        if (!NFController.CheckRules(_mode.Bypass, out var results))
            MessageBoxX.Show(NFController.GenerateInvalidRulesMessage(results), LogLevel.WARNING);
        else
            MessageBoxX.Show("Fine");
    }
}