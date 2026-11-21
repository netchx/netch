using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Netch.App.ViewModels;
using Netch.Models;
using Netch.Models.Modes;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Netch.App.Dialogs;

public sealed partial class SceneEditorDialog : ContentDialog
{
    private readonly CollectionViewSource _modeGroups = new() { IsSourceGrouped = true };
    private readonly CollectionViewSource _nodeGroups = new() { IsSourceGrouped = true };

    /// <param name="scene">The scene to edit, or null to create one.</param>
    public SceneEditorDialog(Scene? scene, bool changeNode = false)
    {
        ViewModel = new SceneEditorViewModel(scene);
        InitializeComponent();
        Title = ViewModel.Title;
        RefreshModeGroups();
        RefreshNodeGroups();
        if (changeNode)
            Opened += (_, _) => NodeSelector.Focus(FocusState.Programmatic);
    }

    public SceneEditorViewModel ViewModel { get; }

    internal void ShowSaveError(string message)
    {
        SaveError.Message = message;
        SaveError.Visibility = Visibility.Visible;
        SaveError.IsOpen = true;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (ActualTheme == ElementTheme.Dark && !new AccessibilitySettings().HighContrast &&
            GetTemplateChild("SmokeLayerBackground") is Rectangle smoke)
            smoke.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(128, 0, 0, 0));
        if (GetTemplateChild("ContentScrollViewer") is ScrollViewer { Content: Grid body })
            body.Padding = new Thickness(24, 23, 24, 21);
        if (GetTemplateChild("CommandSpace") is Grid footer)
            footer.Padding = new Thickness(24, 15, 24, 15);
        if (GetTemplateChild("PrimaryButton") is Button primary)
            primary.Height = 34;
    }

    private string FormatHotkey(string hotkey) => string.Join(" + ", hotkey.Split('+', StringSplitOptions.TrimEntries));

    /// <summary>Set when the dialog closed because the user asked for the mode settings.</summary>
    public Mode? ModeToOpen { get; private set; }

    private void Flyout_Opening(object sender, object e)
    {
        if (sender is Flyout { Content: FrameworkElement content })
            content.RequestedTheme = ActualTheme;
    }

    private void ModeFlyout_Opening(object sender, object e)
    {
        ModeFlyoutRoot.RequestedTheme = ActualTheme;
        if (ModeSearchBox.Text.Length > 0)
            ModeSearchBox.Text = string.Empty;
        else
            RefreshModeGroups();
    }

    private void ModeFlyout_Opened(object sender, object e)
    {
        ModeSearchBox.Focus(FocusState.Programmatic);
        if (ViewModel.SelectedMode != null)
        {
            ModeListView.SelectedItem = ViewModel.SelectedMode;
            ModeListView.ScrollIntoView(ViewModel.SelectedMode);
        }
    }

    private void ModeSearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshModeGroups();

    private void ModeSearchBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Down)
        {
            ModeListView.Focus(FocusState.Keyboard);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Enter && _modeGroups.Source is IReadOnlyList<ModeOptionGroup> groups && groups.Count > 0 && groups[0].Count > 0)
        {
            ViewModel.SelectedMode = groups[0][0];
            ModeFlyout.Hide();
            e.Handled = true;
        }
    }

    private void ModeListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ModeOption option)
        {
            ViewModel.SelectedMode = option;
            ModeFlyout.Hide();
        }
    }

    private void RefreshModeGroups()
    {
        var groups = ViewModel.GetGroupedModes(ModeSearchBox.Text);
        _modeGroups.Source = groups;
        ModeListView.ItemsSource = _modeGroups.View;
        ModeEmptyText.Visibility = groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (ViewModel.SelectedMode != null && groups.Any(g => g.Contains(ViewModel.SelectedMode)))
            ModeListView.SelectedItem = ViewModel.SelectedMode;
    }

    private void NodeFlyout_Opening(object sender, object e)
    {
        NodeFlyoutRoot.RequestedTheme = ActualTheme;
        if (NodeSearchBox.Text.Length > 0)
            NodeSearchBox.Text = string.Empty;
        else
            RefreshNodeGroups();
    }

    private void NodeFlyout_Opened(object sender, object e)
    {
        NodeSearchBox.Focus(FocusState.Programmatic);
        if (ViewModel.SelectedNode != null)
        {
            NodeListView.SelectedItem = ViewModel.SelectedNode;
            NodeListView.ScrollIntoView(ViewModel.SelectedNode);
        }
    }

    private void NodeSearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshNodeGroups();

    private void NodeSearchBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Down)
        {
            NodeListView.Focus(FocusState.Keyboard);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Enter && _nodeGroups.Source is IReadOnlyList<NodeOptionGroup> groups && groups.Count > 0 && groups[0].Count > 0)
        {
            ViewModel.SelectedNode = groups[0][0];
            NodeFlyout.Hide();
            e.Handled = true;
        }
    }

    private void NodeListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is NodeOption option)
        {
            ViewModel.SelectedNode = option;
            NodeFlyout.Hide();
        }
    }

    private void RefreshNodeGroups()
    {
        var groups = ViewModel.GetGroupedNodes(NodeSearchBox.Text);
        _nodeGroups.Source = groups;
        NodeListView.ItemsSource = _nodeGroups.View;
        NodeEmptyText.Visibility = groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (ViewModel.SelectedNode != null && groups.Any(g => g.Contains(ViewModel.SelectedNode)))
            NodeListView.SelectedItem = ViewModel.SelectedNode;
    }

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
