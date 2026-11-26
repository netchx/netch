using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Netch.App.Helpers;
using Netch.App.Pages;
using Netch.Models;
using Netch.Models.Modes.ProcessMode;
using Netch.Services;
using Netch.Utils;
using static Netch.App.Helpers.Ui;

namespace Netch.App.Services;

internal static class HotRulePersistence
{
    public enum ModeSaveKind
    {
        CustomJson,
        BuiltInJson,
        LegacyTxt
    }

    private static bool _confirmOpen;

    public static ModeSaveKind Classify(Redirector mode)
    {
        if (Path.GetExtension(mode.FullName).Equals(".txt", StringComparison.OrdinalIgnoreCase))
            return ModeSaveKind.LegacyTxt;

        var origin = Downloads.Origin(mode.FullName);
        return origin is ModeOrigin.Bundled or ModeOrigin.Update or ModeOrigin.Modified
            ? ModeSaveKind.BuiltInJson
            : ModeSaveKind.CustomJson;
    }

    public static string PrimarySaveLabel(Redirector mode) => Classify(mode) switch
    {
        ModeSaveKind.CustomJson => T("Save to mode", "保存到模式"),
        ModeSaveKind.BuiltInJson => T("Save as copy", "存为副本"),
        ModeSaveKind.LegacyTxt => T("Save as new mode", "保存为新模式"),
        _ => T("Save", "保存")
    };

    public static string? SecondarySaveLabel(Redirector mode) => Classify(mode) switch
    {
        ModeSaveKind.CustomJson => T("Save as copy", "另存为副本"),
        ModeSaveKind.BuiltInJson => T("Overwrite built-in mode", "直接覆盖内置模式"),
        ModeSaveKind.LegacyTxt => null,
        _ => null
    };

    public static string DescribeTarget(Redirector mode) => Classify(mode) switch
    {
        ModeSaveKind.CustomJson => Path.GetFileName(mode.FullName),
        ModeSaveKind.BuiltInJson => $"Custom/{SafeCustomName(mode)}.json",
        ModeSaveKind.LegacyTxt => $"{SafeGroupOrCustom(mode)}/{SafeBaseName(mode)}.json",
        _ => mode.FullName
    };

    public static Task SavePrimaryAsync(Redirector mode) => Classify(mode) switch
    {
        ModeSaveKind.CustomJson => OverwriteModeAsync(mode),
        ModeSaveKind.BuiltInJson => SaveAsCustomCopyAsync(mode),
        ModeSaveKind.LegacyTxt => UpgradeTxtToJsonAsync(mode),
        _ => OverwriteModeAsync(mode)
    };

    public static Task SaveSecondaryAsync(Redirector mode) => Classify(mode) switch
    {
        ModeSaveKind.CustomJson => SaveAsCustomCopyAsync(mode),
        ModeSaveKind.BuiltInJson => OverwriteModeAsync(mode),
        _ => UpgradeTxtToJsonAsync(mode)
    };

    public static Task OverwriteModeAsync(Redirector mode)
    {
        if (Path.GetExtension(mode.FullName).Equals(".txt", StringComparison.OrdinalIgnoreCase))
            return UpgradeTxtToJsonAsync(mode);

        var target = CloneWithOverlay(mode);
        target.FullName = mode.FullName;
        target.Remark = new(mode.Remark);
        Directory.CreateDirectory(Path.GetDirectoryName(target.FullName)!);
        target.WriteFile();

        var modeId = SceneService.GetModeId(target);
        ModeService.Instance.Load(completeSceneDefaults: true);
        var reloaded = FindReloadedRedirector(modeId) ?? target;
        ConnectionTrackerService.Instance.CommitSavedBaseMode(reloaded);
        return Task.CompletedTask;
    }

    public static async Task SaveAsCustomCopyAsync(Redirector mode)
    {
        var copyName = SafeCustomName(mode);
        var copy = CloneWithOverlay(mode);
        copy.FullName = ModeEditor.NewPath("Custom", copyName);
        copy.Remark = new Dictionary<string, string>
        {
            [i18N.LangCode] = copyName,
            ["en"] = copyName
        };

        var oldId = SceneService.GetModeId(mode);
        var newId = SceneService.GetModeId(copy);
        var activeScene = Session.Current.ActiveScene;
        var shouldSwitchScene = activeScene != null &&
            string.Equals(activeScene.ModeId, oldId, StringComparison.OrdinalIgnoreCase);

        Directory.CreateDirectory(Path.GetDirectoryName(copy.FullName)!);
        copy.WriteFile();

        if (shouldSwitchScene && activeScene != null)
        {
            activeScene.ModeId = newId;
            try
            {
                await Configuration.SaveAsync(throwOnError: true);
            }
            catch
            {
                activeScene.ModeId = oldId;
                File.Delete(copy.FullName);
                throw;
            }
        }

        ModeService.Instance.Load(completeSceneDefaults: true);
        var reloaded = FindReloadedRedirector(newId) ?? copy;
        ConnectionTrackerService.Instance.CommitSavedBaseMode(reloaded);
    }

    public static async Task UpgradeTxtToJsonAsync(Redirector mode)
    {
        var baseName = SafeBaseName(mode);
        var group = SafeGroupOrCustom(mode);
        var upgraded = CloneWithOverlay(mode);
        upgraded.FullName = ModeEditor.NewPath(group, baseName);
        upgraded.Remark = new(mode.Remark);
        upgraded.i18NRemark = baseName;
        if (!upgraded.Remark.ContainsKey("en"))
            upgraded.Remark["en"] = baseName;

        var oldId = SceneService.GetModeId(mode);
        var newId = SceneService.GetModeId(upgraded);
        var affectedScenes = SceneService.Scenes
            .Where(s => string.Equals(s.ModeId, oldId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Directory.CreateDirectory(Path.GetDirectoryName(upgraded.FullName)!);
        upgraded.WriteFile();

        if (affectedScenes.Length > 0)
        {
            foreach (var scene in affectedScenes)
                scene.ModeId = newId;
            try
            {
                await Configuration.SaveAsync(throwOnError: true);
            }
            catch
            {
                foreach (var scene in affectedScenes)
                    scene.ModeId = oldId;
                File.Delete(upgraded.FullName);
                throw;
            }
        }

        ModeService.Instance.Load(completeSceneDefaults: true);
        var reloaded = FindReloadedRedirector(newId) ?? upgraded;
        ConnectionTrackerService.Instance.CommitSavedBaseMode(reloaded);
    }

    public static async Task<bool> ConfirmBeforeLeaveAsync()
    {
        var tracker = ConnectionTrackerService.Instance;
        if (!tracker.HasUnsavedHotRules)
            return true;

        var mode = tracker.ActiveMode;
        if (mode == null)
        {
            tracker.PendingHotRules.Clear();
            return true;
        }

        if (_confirmOpen)
            return false;

        App.ShowMainWindow();
        var window = App.MainWindow;
        var root = window?.Content?.XamlRoot;
        if (root == null)
        {
            await Task.Delay(60);
            root = App.MainWindow?.Content?.XamlRoot;
        }

        if (root == null)
            return false;

        var pending = tracker.PendingHotRules.Entries;
        var summary = Caption(TF(
            "{0} rule(s) were added dynamically in this session. Target: {1}",
            "当前会话有 {0} 条临时热更新规则。默认保存目标：{1}",
            pending.Count,
            DescribeTarget(mode)));
        summary.TextWrapping = TextWrapping.Wrap;

        var listPanel = new StackPanel { Spacing = 4 };
        var console = new FontFamily("Consolas");
        foreach (var entry in pending.Take(12))
        {
            var badge = Pill(FormatEntryBadge(entry.Kind));
            var pattern = Caption(entry.Pattern);
            pattern.FontFamily = console;
            listPanel.Children.Add(Row(badge, pattern));
        }

        if (pending.Count > 12)
        {
            listPanel.Children.Add(Caption(TF("… and {0} more", "… 其余 {0} 条", pending.Count - 12)));
        }

        var errorBar = new InfoBar
        {
            Severity = InfoBarSeverity.Error,
            IsClosable = false,
            Margin = new Thickness(0, 0, 0, 6)
        };

        var body = Body(errorBar, Stack(summary, listPanel));
        var dialog = new ContentDialog
        {
            XamlRoot = root,
            RequestedTheme = (root.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default,
            Style = Resource<Style>("DefaultContentDialogStyle"),
            Title = T("Unsaved session rules", "有未保存的临时规则"),
            Content = Scroll(body),
            PrimaryButtonText = Classify(mode) == ModeSaveKind.CustomJson
                ? T("Save & continue", "保存并继续")
                : Classify(mode) == ModeSaveKind.LegacyTxt
                    ? T("Save as new mode & continue", "存为新模式并继续")
                    : T("Save copy & continue", "存为副本并继续"),
            SecondaryButtonText = T("Discard (session only)", "放弃（仅本次生效）"),
            CloseButtonText = T("Cancel", "取消"),
            DefaultButton = ContentDialogButton.Primary
        };

        if (root.Size.Height > 0)
            dialog.Resources["ContentDialogMaxHeight"] = Math.Max(360, root.Size.Height - 40);

        _confirmOpen = true;
        try
        {
            while (true)
            {
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        await SavePrimaryAsync(mode);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        errorBar.Message = ex.Message;
                        errorBar.IsOpen = true;
                        continue;
                    }
                }

                if (result == ContentDialogResult.Secondary)
                {
                    tracker.DiscardHotRules();
                    return true;
                }

                return false;
            }
        }
        finally
        {
            _confirmOpen = false;
        }
    }

    public static string FormatEntryBadge(HotRuleKind kind) => kind switch
    {
        HotRuleKind.HandleProcess => T("+Proc", "+进程"),
        HotRuleKind.BypassProcess => T("-Proc", "绕过进程"),
        HotRuleKind.HandleIp => T("+IP", "+IP"),
        HotRuleKind.BypassIp => T("-IP", "绕过IP"),
        _ => string.Empty
    };

    private static Redirector CloneWithOverlay(Redirector source)
    {
        var clone = new Redirector
        {
            FilterTCP = source.FilterTCP,
            FilterUDP = source.FilterUDP,
            FilterICMP = source.FilterICMP,
            FilterDNS = source.FilterDNS,
            ICMPDelay = source.ICMPDelay,
            FilterParent = source.FilterParent,
            DNSProxy = source.DNSProxy,
            DNSHost = source.DNSHost,
            HandleOnlyDNS = source.HandleOnlyDNS,
            FilterLoopback = source.FilterLoopback,
            FilterIntranet = source.FilterIntranet,
            Handle = [.. source.Handle],
            Bypass = [.. source.Bypass],
            HandleIPs = [.. source.HandleIPs],
            BypassIPs = [.. source.BypassIPs]
        };
        ConnectionTrackerService.Instance.PendingHotRules.ApplyToMode(clone);
        return clone;
    }

    private static Redirector? FindReloadedRedirector(string modeId) =>
        Global.Modes.OfType<Redirector>().FirstOrDefault(m =>
            string.Equals(SceneService.GetModeId(m), modeId, StringComparison.OrdinalIgnoreCase));

    private static string SafeBaseName(Redirector mode) =>
        string.IsNullOrWhiteSpace(mode.i18NRemark)
            ? Path.GetFileNameWithoutExtension(mode.FullName)
            : mode.i18NRemark.Trim();

    private static string SafeCustomName(Redirector mode)
    {
        var baseName = SafeBaseName(mode);
        return baseName.EndsWith("(Custom)", StringComparison.OrdinalIgnoreCase)
            ? baseName
            : $"{baseName} (Custom)";
    }

    private static string SafeGroupOrCustom(Redirector mode)
    {
        var group = ModesPage.GroupOf(mode);
        return string.IsNullOrWhiteSpace(group) ? "Custom" : group;
    }
}
