using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Netch.App.Services;
using Netch.Models;
using Netch.Services;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Dialogs;

internal static class DependencyDialog
{
    public static async Task<bool> ShowAsync(
        XamlRoot root,
        Scene scene,
        string scopeTitle,
        IReadOnlyList<DependencyIssue> initialIssues,
        Func<Task> retryStartAsync)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(initialIssues);
        ArgumentNullException.ThrowIfNull(retryStartAsync);

        if (!TryAcquireDialogSlot())
            return false;

        var hasFixable = initialIssues.Any(i => i.ActionKind != DependencyActionKind.None);

        var lead = Caption(T(
            "Install or restore the missing components below, then click \"Recheck and start\" to continue.",
            "检测到缺少以下运行依赖，安装或修复完成后点击「重新检测并启动」即可继续。"));
        lead.TextWrapping = TextWrapping.Wrap;
        lead.TextTrimming = TextTrimming.None;

        var warningBar = new InfoBar
        {
            Severity = InfoBarSeverity.Warning,
            IsClosable = false,
            Visibility = Visibility.Collapsed
        };

        var issueList = new StackPanel { Spacing = 8 };
        PopulateIssues(issueList, initialIssues);

        var scroll = Scroll(issueList, rightGutter: 8);
        if (root.Size.Height > 0)
            scroll.MaxHeight = Math.Max(200, root.Size.Height - 280);

        var body = new StackPanel { Spacing = 12 };
        if (hasFixable)
            body.Children.Add(lead);
        body.Children.Add(warningBar);
        body.Children.Add(scroll);

        var dialog = new ContentDialog
        {
            XamlRoot = root,
            RequestedTheme = (root.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default,
            Style = Resource<Style>("DefaultContentDialogStyle"),
            Title = TF("Cannot start \"{0}\"", "无法启动「{0}」", scopeTitle),
            Content = body,
            PrimaryButtonText = hasFixable ? T("Recheck and start", "重新检测并启动") : "",
            CloseButtonText = hasFixable ? T("Cancel", "取消") : T("Close", "关闭"),
            DefaultButton = hasFixable ? ContentDialogButton.Primary : ContentDialogButton.Close
        };
        dialog.Resources["ContentDialogMaxWidth"] = 560.0;
        if (root.Size.Height > 0)
            dialog.Resources["ContentDialogMaxHeight"] = Math.Max(380, root.Size.Height - 48);

        dialog.PrimaryButtonClick += (_, args) =>
        {
            var remaining = ReevaluateSceneIssues(scene);
            if (remaining.Count == 0)
                return;

            args.Cancel = true;
            PopulateIssues(issueList, remaining);
            dialog.IsPrimaryButtonEnabled = remaining.Any(i => i.ActionKind != DependencyActionKind.None);
            warningBar.Message = T("Some required components are still missing.", "仍有缺失项未完成安装或修复。");
            warningBar.Visibility = Visibility.Visible;
            warningBar.IsOpen = true;
        };

        ContentDialogResult result;
        Tray.Current.UnregisterHotkeys();
        try
        {
            result = await dialog.ShowAsync();
        }
        finally
        {
            Tray.Current.RegisterHotkeys();
            ReleaseDialogSlot();
        }

        if (result == ContentDialogResult.Primary)
            await retryStartAsync();

        return true;
    }

    private static IReadOnlyList<DependencyIssue> ReevaluateSceneIssues(Scene scene)
    {
        var server = SceneService.FindServer(scene);
        var modes = SceneService.FindModes(scene);
        if (server == null || modes.Count == 0)
            return Array.Empty<DependencyIssue>();

        var effectiveModes = ModeComposer.ValidateSelection(modes, out _)
            ? ModeComposer.Compose(modes).EffectiveModes
            : modes;

        var coreIssues = EnvironmentChecker.GetIssuesForBuiltInCore(server, effectiveModes);
        var modeIssues = EnvironmentChecker.GetIssuesForModes(effectiveModes);
        return coreIssues.Concat(modeIssues).ToArray();
    }

    private static void PopulateIssues(StackPanel container, IReadOnlyList<DependencyIssue> issues)
    {
        container.Children.Clear();
        var criticalBrush = Resource<Brush>("SystemFillColorCriticalBrush");

        foreach (var issue in issues)
        {
            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = criticalBrush,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 6, 10, 0)
            };

            var title = Text(issue.Title, strong: true);
            title.TextWrapping = TextWrapping.Wrap;
            title.TextTrimming = TextTrimming.None;

            var detail = Caption(issue.Detail);
            detail.TextWrapping = TextWrapping.Wrap;
            detail.TextTrimming = TextTrimming.None;

            var textStack = new StackPanel
            {
                Spacing = 2,
                Children = { title, detail }
            };

            var grid = new Grid { ColumnSpacing = 4 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            grid.Children.Add(dot);
            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            if (issue.ActionKind != DependencyActionKind.None && !string.IsNullOrWhiteSpace(issue.ActionTarget))
            {
                var captured = issue;
                var actionButton = Button(
                    issue.ActionLabel ?? T("Fix", "修复"),
                    () => ExecuteAction(captured));
                actionButton.VerticalAlignment = VerticalAlignment.Center;
                actionButton.Margin = new Thickness(12, 0, 0, 0);
                Grid.SetColumn(actionButton, 2);
                grid.Children.Add(actionButton);
            }

            container.Children.Add(Card(grid, 11));
        }
    }

    private static void ExecuteAction(DependencyIssue issue)
    {
        if (string.IsNullOrWhiteSpace(issue.ActionTarget))
            return;

        switch (issue.ActionKind)
        {
            case DependencyActionKind.OpenUrl:
                Utils.Utils.Open(issue.ActionTarget);
                break;
            case DependencyActionKind.OpenDirectory:
            case DependencyActionKind.OpenServices:
                try
                {
                    Process.Start(new ProcessStartInfo(issue.ActionTarget) { UseShellExecute = true });
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Failed to launch {Target}", issue.ActionTarget);
                }
                break;
        }
    }
}
