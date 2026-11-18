using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Netch.App.ViewModels;
using Netch.Models;
using Netch.Models.Modes;
using Windows.System;
using Windows.UI.Core;

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

    /// <summary>Records the pressed combination; Backspace or Delete clears it, Tab, Enter and Escape keep their usual meaning.</summary>
    private void HotkeyBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Tab or VirtualKey.Enter or VirtualKey.Escape:
                return;
            case VirtualKey.Back or VirtualKey.Delete:
                ViewModel.Hotkey = string.Empty;
                e.Handled = true;
                return;
        }

        e.Handled = true;
        if (Hotkey.IsModifier(e.Key))
            return;

        var modifiers = HotkeyModifiers.None;
        if (IsDown(VirtualKey.Control))
            modifiers |= HotkeyModifiers.Control;
        if (e.KeyStatus.IsMenuKeyDown || IsDown(VirtualKey.Menu))
            modifiers |= HotkeyModifiers.Alt;
        if (IsDown(VirtualKey.Shift))
            modifiers |= HotkeyModifiers.Shift;
        if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
            modifiers |= HotkeyModifiers.Windows;

        if (modifiers != HotkeyModifiers.None)
            ViewModel.Hotkey = new Hotkey(modifiers, e.Key).ToString();
    }

    private static bool IsDown(VirtualKey key) => InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
}
