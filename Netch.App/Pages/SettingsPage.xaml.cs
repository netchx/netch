using Microsoft.UI.Xaml.Controls;
using Netch.Enums;
using Netch.Utils;

namespace Netch.App.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        ThemeComboBox.SelectedIndex = (int)Global.Settings.Theme;
    }

    private async void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var theme = (AppTheme)ThemeComboBox.SelectedIndex;
        if (theme == Global.Settings.Theme)
            return;

        Global.Settings.Theme = theme;
        App.ApplyTheme(theme);
        await Configuration.SaveAsync();
    }
}
