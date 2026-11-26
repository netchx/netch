using Netch.Models;

namespace Netch.Utils;

public static class NodeGroupHelper
{
    public static bool IsManualGroup(string? group)
    {
        return string.IsNullOrWhiteSpace(group) ||
               group.Trim().Equals(Constants.DefaultGroup, StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeGroup(string? group)
    {
        return IsManualGroup(group)
            ? Constants.DefaultGroup
            : group?.Trim() ?? Constants.DefaultGroup;
    }

    public static bool GroupEquals(string? left, string? right)
    {
        return string.Equals(NormalizeGroup(left), NormalizeGroup(right), StringComparison.OrdinalIgnoreCase);
    }

    public static bool LooksLikeClashYaml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var content = text.TrimStart('\uFEFF');
        try
        {
            content = ShareLink.URLSafeBase64Decode(content);
        }
        catch
        {
            // Keep raw text when input is not base64-encoded.
        }

        return ClashProxyList.IsClashYaml(content);
    }

    public static IReadOnlyList<string> GetImportedGroups(IEnumerable<Server>? servers = null)
    {
        var source = servers ?? Global.Settings.Server;
        return source
            .Where(s => !s.IsSub && !IsManualGroup(s.Group))
            .Select(s => NormalizeGroup(s.Group))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string? ValidateGroupName(
        string? candidate,
        string? currentName = null,
        IEnumerable<Subscription>? subscriptions = null,
        IEnumerable<Server>? servers = null)
    {
        var trimmed = candidate?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || IsManualGroup(trimmed))
            return i18N.Translate("Enter a valid group name.");

        var currentTrimmed = currentName?.Trim();
        if (currentTrimmed != null &&
            currentTrimmed.Length > 0 &&
            trimmed.Equals(currentTrimmed, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var existingNames = CollectExistingGroupNames(subscriptions, servers);
        return existingNames.Contains(trimmed)
            ? i18N.Translate("A group with this name already exists.")
            : null;
    }

    public static string MakeUniqueGroupName(
        string? baseName,
        IEnumerable<Subscription>? subscriptions = null,
        IEnumerable<Server>? servers = null)
    {
        var normalized = baseName?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || IsManualGroup(normalized))
            normalized = "Imported";

        var existingNames = CollectExistingGroupNames(subscriptions, servers);
        if (!existingNames.Contains(normalized))
            return normalized;

        for (var index = 2; ; index++)
        {
            var candidate = $"{normalized} ({index})";
            if (!existingNames.Contains(candidate))
                return candidate;
        }
    }

    public static string ResolveImportGroupName(
        string text,
        string? filePath = null,
        IEnumerable<Subscription>? subscriptions = null,
        IEnumerable<Server>? servers = null)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var baseName = Path.GetFileNameWithoutExtension(filePath).Trim();
            if (baseName.Length == 0 || IsManualGroup(baseName))
                baseName = LooksLikeClashYaml(text) ? "Clash YAML" : "Imported";
            return MakeUniqueGroupName(baseName, subscriptions, servers);
        }

        return LooksLikeClashYaml(text)
            ? MakeUniqueGroupName("Clash YAML", subscriptions, servers)
            : Constants.DefaultGroup;
    }

    public static bool RenameGroup(
        string oldName,
        string newName,
        bool isSubscription,
        out string? error,
        IEnumerable<Subscription>? subscriptions = null,
        IEnumerable<Server>? servers = null)
    {
        error = ValidateGroupName(newName, oldName, subscriptions, servers);
        if (error != null)
            return false;

        var trimmedOld = oldName.Trim();
        var trimmedNew = newName.Trim();
        var subList = subscriptions ?? Global.Settings.Subscription;
        var serverList = servers ?? Global.Settings.Server;

        if (isSubscription)
        {
            foreach (var sub in subList)
            {
                if ((sub.Remark ?? string.Empty).Trim().Equals(trimmedOld, StringComparison.OrdinalIgnoreCase))
                    sub.Remark = trimmedNew;
            }

            foreach (var server in serverList)
            {
                if (server.IsSub && GroupEquals(server.Group, trimmedOld))
                    server.Group = trimmedNew;
            }
        }
        else
        {
            foreach (var server in serverList)
            {
                if (!server.IsSub && GroupEquals(server.Group, trimmedOld))
                    server.Group = trimmedNew;
            }
        }

        return true;
    }

    public static int CountAffectedScenes(IEnumerable<Server> groupServers, IEnumerable<Scene>? scenes = null)
    {
        var ids = groupServers.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        if (ids.Count == 0)
            return 0;

        var sceneList = scenes ?? Global.Settings.Scenes;
        return sceneList == null ? 0 : sceneList.Count(scene => ids.Contains(scene.ServerId));
    }

    private static HashSet<string> CollectExistingGroupNames(
        IEnumerable<Subscription>? subscriptions,
        IEnumerable<Server>? servers)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sub in subscriptions ?? Global.Settings.Subscription)
        {
            var remark = (sub.Remark ?? string.Empty).Trim();
            if (remark.Length > 0 && !IsManualGroup(remark))
                names.Add(remark);
        }

        foreach (var server in servers ?? Global.Settings.Server)
        {
            var group = NormalizeGroup(server.Group);
            if (!IsManualGroup(group))
                names.Add(group);
        }

        return names;
    }
}
