using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Netch.App.Helpers;
using Netch.App.ViewModels;
using Netch.Models;
using Netch.Models.Modes;
using Windows.System;
using Windows.UI.Core;
using Windows.Win32.UI.Input.KeyboardAndMouse;

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

        HOT_KEY_MODIFIERS modifiers = 0;
        if (IsDown(VirtualKey.Control))
            modifiers |= HOT_KEY_MODIFIERS.MOD_CONTROL;
        if (e.KeyStatus.IsMenuKeyDown || IsDown(VirtualKey.Menu))
            modifiers |= HOT_KEY_MODIFIERS.MOD_ALT;
        if (IsDown(VirtualKey.Shift))
            modifiers |= HOT_KEY_MODIFIERS.MOD_SHIFT;
        if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
            modifiers |= HOT_KEY_MODIFIERS.MOD_WIN;

        if (modifiers != 0)
            ViewModel.Hotkey = new Hotkey(modifiers, e.Key).ToString();
    }

    private static bool IsDown(VirtualKey key) => InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
}
