using WindowsFirewallHelper;
using WindowsFirewallHelper.FirewallRules;

namespace Netch.Utils;

public static class Firewall
{
    private const string Netch = "Netch";

    // Enumerating every firewall rule through COM is slow on systems with many
    // rules. The install directory cannot change while Netch runs, so one
    // successful check per process is enough.
    private static volatile bool _rulesVerified;

    /// <summary>
    ///     Netch 自带程序添加防火墙
    /// </summary>
    public static void AddNetchFwRules()
    {
        if (_rulesVerified)
            return;

        if (!FirewallWAS.IsLocallySupported)
        {
            Log.Warning("Windows Firewall Locally Unsupported");
            return;
        }

        try
        {
            var rule = FirewallManager.Instance.Rules.FirstOrDefault(r => r.Name == Netch);
            if (rule != null)
            {
                if (rule.ApplicationName?.StartsWith(Global.NetchDir, StringComparison.OrdinalIgnoreCase) == true)
                {
                    _rulesVerified = true;
                    return;
                }

                RemoveNetchFwRules();
            }

            foreach (var path in Directory.GetFiles(Global.NetchDir, "*.exe", SearchOption.AllDirectories))
                AddFwRule(Netch, path);

            _rulesVerified = true;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Create Netch Firewall rules error");
        }
    }

    /// <summary>
    ///     清除防火墙规则 (Netch 自带程序)
    /// </summary>
    public static void RemoveNetchFwRules()
    {
        if (!FirewallWAS.IsLocallySupported)
            return;

        try
        {
            // Materialize first: removing while enumerating the live COM
            // collection is not supported.
            var rules = FirewallManager.Instance.Rules.Where(r
                    => r.ApplicationName?.StartsWith(Global.NetchDir, StringComparison.OrdinalIgnoreCase) ?? r.Name == Netch)
                .ToList();

            foreach (var rule in rules)
                FirewallManager.Instance.Rules.Remove(rule);

            _rulesVerified = false;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Remove Netch Firewall rules error");
        }
    }

    #region 封装

    private static void AddFwRule(string ruleName, string exeFullPath)
    {
        var rule = new FirewallWASRule(ruleName,
            exeFullPath,
            FirewallAction.Allow,
            FirewallDirection.Inbound,
            FirewallProfiles.Private | FirewallProfiles.Public | FirewallProfiles.Domain);

        FirewallManager.Instance.Rules.Add(rule);
    }

    #endregion
}