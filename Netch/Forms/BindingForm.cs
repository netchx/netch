using Netch.Models;

namespace Netch.Forms;

[Fody.ConfigureAwait(true)]
public class BindingForm : Form
{
    // validation actions
    private readonly Dictionary<Control, Func<string, bool>> _checkActions = new();
    private readonly Dictionary<Control, Action<Control>> _saveActions = new();

    protected void BindTextBox(TextBoxBase control, Func<string, bool> check, Action<string> save, object value)
    {
        BindTextBox<string>(control, check, save, value);
    }

    protected virtual void BindTextBox<T>(TextBoxBase control, Func<T, bool> check, Action<T> save, object value)
    {
        control.Text = value.ToString();
        _checkActions.Add(control,
            s =>
            {
                try
                {
                    return check((T)Convert.ChangeType(s, typeof(T)));
                }
                catch
                {
                    return false;
                }
            });

        _saveActions.Add(control, c => save((T)Convert.ChangeType(((TextBoxBase)c).Text, typeof(T))));
    }

    protected void BindCheckBox(CheckBox control, Action<bool> save, bool value)
    {
        control.Checked = value;
        _saveActions.Add(control, c => save(((CheckBox)c).Checked));
    }

    protected void BindSyncGlobalCheckBox(SyncGlobalCheckBox control, Action<bool?> save, bool? value, bool globalValue)
    {
        control.Value = value;
        control.GlobalValue = globalValue;
        _saveActions.Add(control, c => save(((SyncGlobalCheckBox)c).Value));
    }

    protected void BindRadioBox(RadioButton control, Action<bool> save, bool value)
    {
        control.Checked = value;
        _saveActions.Add(control, c => save(((RadioButton)c).Checked));
    }

    protected void BindListComboBox<T>(ComboBox comboBox, Action<T> save, IEnumerable<T> values, T value) where T : notnull
    {
        if (comboBox.DropDownStyle != ComboBoxStyle.DropDownList)
            throw new ArgumentOutOfRangeException();

        var tagItems = values.Select(o => new TagItem<T>(o, o.ToString()!)).ToArray();
        comboBox.Items.AddRange(tagItems.Cast<object>().ToArray());

        comboBox.ValueMember = nameof(TagItem<T>.Value);
        comboBox.DisplayMember = nameof(TagItem<T>.Text);

        _saveActions.Add(comboBox, c => save(((TagItem<T>)((ComboBox)c).SelectedItem).Value));
        Load += (_, _) => { comboBox.SelectedItem = tagItems.SingleOrDefault(t => t.Value.Equals(value)); };
    }

    protected void BindComboBox(ComboBox control, Func<string, bool> check, Action<string> save, string value, object[]? values = null)
    {
        if (values != null)
            control.Items.AddRange(values);

        _checkActions.Add(control, check);
        _saveActions.Add(control, c => save(((ComboBox)c).Text));

        Load += (_, _) => { control.Text = value; };
    }

    protected List<Control> GetInvalidateValueControls()
    {
        return _checkActions.Keys.Where(c => !Validate(c)).ToList();
    }

    protected bool Validate(Control c)
    {
        if (!_checkActions.ContainsKey(c))
            throw new ArgumentException();

        return _checkActions[c](c.Text);
    }

    protected void SaveBinds()
    {
        foreach (var pair in _saveActions)
            pair.Value(pair.Key);
    }
}