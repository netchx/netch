using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Netch.App.ViewModels;
using Netch.Models;
using Netch.Models.Modes;

namespace Netch.App.Dialogs;

public sealed partial class SceneEditorDialog : ContentDialog
{
    /// <param name="scene">The scene to edit, or null to create one.</param>
    public SceneEditorDialog(Scene? scene)
    {
        ViewModel = new SceneEditorViewModel(scene);
        InitializeComponent();
        Title = ViewModel.Title;
    }

    public SceneEditorViewModel ViewModel { get; }

    /// <summary>Set when the dialog closed because the user asked for the mode settings.</summary>
    public Mode? ModeToOpen { get; private set; }

    private void IconGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        ViewModel.Icon = (string)e.ClickedItem;
        IconFlyout.Hide();
    }

    private void OpenModeSettings_Click(object sender, RoutedEventArgs e)
    {
        ModeToOpen = ViewModel.SelectedMode?.Mode;
        Hide();
    }
}
