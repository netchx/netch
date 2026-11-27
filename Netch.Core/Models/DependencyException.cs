namespace Netch.Models;

public enum DependencyScope
{
    Startup,
    CoreFiles,
    ProcessMode,
    TunMode,
    ShareMode
}

public enum NpcapStatus
{
    Ready,
    Missing,
    MissingWinPcapCompat
}

public enum DependencyActionKind
{
    None,
    OpenUrl,
    OpenDirectory,
    OpenServices
}

public sealed record DependencyIssue(
    string Code,
    DependencyScope Scope,
    string Title,
    string Detail,
    DependencyActionKind ActionKind = DependencyActionKind.None,
    string? ActionLabel = null,
    string? ActionTarget = null);

public sealed class DependencyException : MessageException
{
    public DependencyException(string scopeTitle, IReadOnlyList<DependencyIssue> issues)
        : base(BuildMessage(scopeTitle, issues))
    {
        ArgumentNullException.ThrowIfNull(scopeTitle);
        ArgumentNullException.ThrowIfNull(issues);

        ScopeTitle = scopeTitle;
        Issues = issues;
    }

    public string ScopeTitle { get; }

    public IReadOnlyList<DependencyIssue> Issues { get; }

    private static string BuildMessage(string scopeTitle, IReadOnlyList<DependencyIssue> issues)
    {
        if (issues.Count == 0)
            return scopeTitle;

        var lines = issues.Select(issue => string.IsNullOrWhiteSpace(issue.Detail)
            ? $"• {issue.Title}"
            : $"• {issue.Title}: {issue.Detail}");
        return $"{scopeTitle}\n{string.Join('\n', lines)}";
    }
}
