using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Netch.App.Helpers;
using Netch.App.Pages;
using Netch.Controllers;
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
        LegacyTxt,
        SceneOverlay
    }

    private static bool _confirmOpen;

    public static ModeSaveKind Classify(Redirector mode)
    {
        if (IsMultiProcessScene(mode))
            return ModeSaveKind.SceneOverlay;

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
        ModeSaveKind.SceneOverlay => FindExistingSceneOverlay(Session.Current.ActiveScene) != null
            ? T("Save to scene rules", "保存到场景规则")
            : T("Create & attach scene rules", "生成并挂载场景规则"),
        _ => T("Save", "保存")
    };

    public static string? SecondarySaveLabel(Redirector mode) => Classify(mode) switch
    {
        ModeSaveKind.CustomJson => T("Save as copy", "另存为副本"),
        ModeSaveKind.BuiltInJson => T("Overwrite built-in mode", "直接覆盖内置模式"),
        ModeSaveKind.LegacyTxt => null,
        ModeSaveKind.SceneOverlay => null,
        _ => null
    };

    public static string DescribeTarget(Redirector mode) => Classify(mode) switch
    {
        ModeSaveKind.CustomJson => Path.GetFileName(mode.FullName),
        ModeSaveKind.BuiltInJson => $"Custom/{SafeCustomName(mode)}.json",
        ModeSaveKind.LegacyTxt => $"{SafeGroupOrCustom(mode)}/{SafeBaseName(mode)}.json",
        ModeSaveKind.SceneOverlay => FindExistingSceneOverlay(Session.Current.ActiveScene) is { } existing
            ? Path.GetFileName(existing.FullName)
            : $"Custom/{SafeOverlayName(Session.Current.ActiveScene)}.json",
        _ => mode.FullName
    };

    public static Task SavePrimaryAsync(Redirector mode) => Classify(mode) switch
    {
        ModeSaveKind.CustomJson => OverwriteModeAsync(mode),
        ModeSaveKind.BuiltInJson => SaveAsCustomCopyAsync(mode),
        ModeSaveKind.LegacyTxt => UpgradeTxtToJsonAsync(mode),
        ModeSaveKind.SceneOverlay => SaveToSceneOverlayAsync(mode),
        _ => OverwriteModeAsync(mode)
    };

    public static Task SaveSecondaryAsync(Redirector mode) => Classify(mode) switch
    {
        ModeSaveKind.CustomJson => SaveAsCustomCopyAsync(mode),
        ModeSaveKind.BuiltInJson => OverwriteModeAsync(mode),
        ModeSaveKind.SceneOverlay => SaveToSceneOverlayAsync(mode),
        _ => UpgradeTxtToJsonAsync(mode)
    };

    public static Task OverwriteModeAsync(Redirector mode)
    {
        if (IsMultiProcessScene(mode))
            return SaveToSceneOverlayAsync(mode);

        if (Path.GetExtension(mode.FullName).Equals(".txt", StringComparison.OrdinalIgnoreCase))
            return UpgradeTxtToJsonAsync(mode);

        var source = ResolveSourceForSingleProcess(mode);
        var target = CloneWithOverlay(source);
        target.FullName = source.FullName;
        target.Remark = new(source.Remark);
        Directory.CreateDirectory(Path.GetDirectoryName(target.FullName)!);
        target.WriteFile();

        var modeId = SceneService.GetModeId(target);
        ModeService.Instance.Load(completeSceneDefaults: true);
        var reloaded = FindReloadedRedirector(modeId) ?? target;
        ConnectionTrackerService.Instance.CommitSavedBaseMode(ResolveEffectiveCommitMode(Session.Current.ActiveScene, reloaded));
        return Task.CompletedTask;
    }

    public static async Task SaveAsCustomCopyAsync(Redirector mode)
    {
        var source = ResolveSourceForSingleProcess(mode);
        var copyName = SafeCustomName(source);
        var copy = CloneWithOverlay(source);
        copy.FullName = ModeEditor.NewPath("Custom", copyName);
        copy.Remark = new Dictionary<string, string>
        {
            [i18N.LangCode] = copyName,
            ["en"] = copyName
        };

        var oldId = SceneService.GetModeId(source);
        var newId = SceneService.GetModeId(copy);
        var activeScene = Session.Current.ActiveScene;
        var shouldSwitchScene = activeScene != null && activeScene.UsesMode(oldId);
        var previousModeIds = activeScene?.GetEffectiveModeIds().ToArray();
        var previousOverlayId = activeScene?.HotRuleModeId;

        Directory.CreateDirectory(Path.GetDirectoryName(copy.FullName)!);
        copy.WriteFile();

        if (shouldSwitchScene && activeScene != null)
        {
            activeScene.ReplaceModeId(oldId, newId);
            try
            {
                await Configuration.SaveAsync(throwOnError: true);
            }
            catch
            {
                if (previousModeIds != null)
                {
                    activeScene.SetModeIds(previousModeIds);
                    activeScene.HotRuleModeId = previousOverlayId;
                }
                File.Delete(copy.FullName);
                throw;
            }
        }

        ModeService.Instance.Load(completeSceneDefaults: true);
        var reloaded = FindReloadedRedirector(newId) ?? copy;
        ConnectionTrackerService.Instance.CommitSavedBaseMode(ResolveEffectiveCommitMode(activeScene, reloaded));
    }

    public static async Task UpgradeTxtToJsonAsync(Redirector mode)
    {
        var source = ResolveSourceForSingleProcess(mode);
        var baseName = SafeBaseName(source);
        var group = SafeGroupOrCustom(source);
        var upgraded = CloneWithOverlay(source);
        upgraded.FullName = ModeEditor.NewPath(group, baseName);
        upgraded.Remark = new(source.Remark);
        upgraded.i18NRemark = baseName;
        if (!upgraded.Remark.ContainsKey("en"))
            upgraded.Remark["en"] = baseName;

        var oldId = SceneService.GetModeId(source);
        var newId = SceneService.GetModeId(upgraded);
        var affectedScenes = SceneService.Scenes
            .Where(s => s.UsesMode(oldId))
            .ToArray();
        var snapshots = affectedScenes
            .Select(s => (Scene: s, ModeIds: s.GetEffectiveModeIds().ToArray(), HotRuleModeId: s.HotRuleModeId))
            .ToArray();

        Directory.CreateDirectory(Path.GetDirectoryName(upgraded.FullName)!);
        upgraded.WriteFile();

        if (affectedScenes.Length > 0)
        {
            foreach (var scene in affectedScenes)
                scene.ReplaceModeId(oldId, newId);
            try
            {
                await Configuration.SaveAsync(throwOnError: true);
            }
            catch
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Scene.SetModeIds(snapshot.ModeIds);
                    snapshot.Scene.HotRuleModeId = snapshot.HotRuleModeId;
                }
                File.Delete(upgraded.FullName);
                throw;
            }
        }

        ModeService.Instance.Load(completeSceneDefaults: true);
        var reloaded = FindReloadedRedirector(newId) ?? upgraded;
        ConnectionTrackerService.Instance.CommitSavedBaseMode(ResolveEffectiveCommitMode(Session.Current.ActiveScene, reloaded));
    }

    public static async Task SaveToSceneOverlayAsync(Redirector mode)
    {
        var activeScene = Session.Current.ActiveScene
            ?? throw new InvalidOperationException("No active scene is running.");

        var previousModeIds = activeScene.GetEffectiveModeIds().ToArray();
        var previousOverlayId = activeScene.HotRuleModeId;

        if (FindExistingSceneOverlay(activeScene) is { } existing)
        {
            var target = CloneWithOverlay(existing);
            target.FullName = existing.FullName;
            target.Remark = new(existing.Remark);
            Directory.CreateDirectory(Path.GetDirectoryName(target.FullName)!);
            target.WriteFile();

            var overlayId = SceneService.GetModeId(target);
            var ids = activeScene.GetEffectiveModeIds().ToList();
            if (!string.Equals(ids.LastOrDefault(), overlayId, StringComparison.OrdinalIgnoreCase))
            {
                ids.RemoveAll(id => string.Equals(id, overlayId, StringComparison.OrdinalIgnoreCase));
                ids.Add(overlayId);
                activeScene.SetModeIds(ids);
                activeScene.HotRuleModeId = overlayId;
                try
                {
                    await Configuration.SaveAsync(throwOnError: true);
                }
                catch
                {
                    activeScene.SetModeIds(previousModeIds);
                    activeScene.HotRuleModeId = previousOverlayId;
                    throw;
                }
            }

            ModeService.Instance.Load(completeSceneDefaults: true);
            var reloadedOverlay = FindReloadedRedirector(overlayId) ?? target;
            ConnectionTrackerService.Instance.CommitSavedBaseMode(ResolveEffectiveCommitMode(activeScene, reloadedOverlay));
            return;
        }

        var sceneBaseName = SafeSceneBaseName(activeScene);
        var overlayName = SafeOverlayName(activeScene);
        var overlay = new Redirector
        {
            FullName = ModeEditor.NewPath("Custom", overlayName),
            Remark = new Dictionary<string, string>
            {
                [i18N.LangCode] = overlayName,
                ["en"] = $"{sceneBaseName} (Rules)"
            },
            FilterTCP = mode.FilterTCP,
            FilterUDP = mode.FilterUDP,
            FilterICMP = mode.FilterICMP,
            FilterDNS = mode.FilterDNS,
            ICMPDelay = mode.ICMPDelay,
            FilterParent = mode.FilterParent,
            DNSProxy = mode.DNSProxy,
            DNSHost = mode.DNSHost,
            HandleOnlyDNS = mode.HandleOnlyDNS,
            FilterLoopback = mode.FilterLoopback,
            FilterIntranet = mode.FilterIntranet
        };
        ConnectionTrackerService.Instance.PendingHotRules.ApplyToMode(overlay);

        Directory.CreateDirectory(Path.GetDirectoryName(overlay.FullName)!);
        overlay.WriteFile();

        var newId = SceneService.GetModeId(overlay);
        activeScene.AddModeId(newId);
        activeScene.HotRuleModeId = newId;
        try
        {
            await Configuration.SaveAsync(throwOnError: true);
        }
        catch
        {
            activeScene.SetModeIds(previousModeIds);
            activeScene.HotRuleModeId = previousOverlayId;
            if (File.Exists(overlay.FullName))
                File.Delete(overlay.FullName);
            throw;
        }

        ModeService.Instance.Load(completeSceneDefaults: true);
        var reloaded = FindReloadedRedirector(newId) ?? overlay;
        ConnectionTrackerService.Instance.CommitSavedBaseMode(ResolveEffectiveCommitMode(activeScene, reloaded));
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
            PrimaryButtonText = Classify(mode) switch
            {
                ModeSaveKind.CustomJson => T("Save & continue", "保存并继续"),
                ModeSaveKind.LegacyTxt => T("Save as new mode & continue", "存为新模式并继续"),
                ModeSaveKind.SceneOverlay => T("Save & continue", "保存并继续"),
                _ => T("Save copy & continue", "存为副本并继续")
            },
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

    private static bool IsMultiProcessScene(Redirector mode) =>
        string.IsNullOrEmpty(mode.FullName) ||
        MainController.SourceModes.OfType<Redirector>().Count() > 1;

    private static Redirector ResolveSourceForSingleProcess(Redirector activeMode) =>
        MainController.SourceModes.OfType<Redirector>().FirstOrDefault(m =>
            !string.IsNullOrEmpty(m.FullName) &&
            string.Equals(m.FullName, activeMode.FullName, StringComparison.OrdinalIgnoreCase))
        ?? activeMode;

    private static Redirector? FindExistingSceneOverlay(Scene? scene)
    {
        if (scene?.HotRuleModeId is not { Length: > 0 } overlayId || !scene.UsesMode(overlayId))
            return null;

        return FindReloadedRedirector(overlayId);
    }

    private static Redirector ResolveEffectiveCommitMode(Scene? scene, Redirector fallback)
    {
        if (scene == null)
            return fallback;

        var modes = SceneService.FindModes(scene);
        if (modes.Count > 1 && ModeComposer.ValidateSelection(modes, out _))
            return ModeComposer.Compose(modes).EffectiveProcessMode ?? fallback;

        return fallback;
    }

    private static string SafeSceneBaseName(Scene? scene) =>
        string.IsNullOrWhiteSpace(scene?.Name) ? "Scene" : scene.Name.Trim();

    private static string SafeOverlayName(Scene? scene) =>
        TF("{0} (Rules)", "{0} (附加规则)", SafeSceneBaseName(scene));

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
