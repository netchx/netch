using System;
using System.IO;
using System.Linq;
using WindowsFirewallHelper;
using WindowsFirewallHelper.FirewallRules;

namespace Netch.Utils
{
    public static class Firewall
    {
        private const string Netch = "Netch";

        /// <summary>
        ///     Netch 自带程序添加防火墙
        /// </summary>
        public static void AddNetchFwRules()
        {
            if (!FirewallWAS.IsSupported)
            {
                Global.Logger.Warning("不支持防火墙");
                return;
            }

            try
            {
                var rule = FirewallManager.Instance.Rules.FirstOrDefault(r => r.Name == Netch);
                if (rule != null)
                {
                    if (rule.ApplicationName.StartsWith(Global.NetchDir))
                        return;

                    RemoveNetchFwRules();
                }

                foreach (var path in Directory.GetFiles(Global.NetchDir, "*.exe", SearchOption.AllDirectories))
                    AddFwRule(Netch, path);
            }
            catch (Exception e)
            {
                Global.Logger.Warning("添加防火墙规则错误(如已关闭防火墙则可无视此错误)\n" + e);
            }
        }

        /// <summary>
        ///     清除防火墙规则 (Netch 自带程序)
        /// </summary>
        public static void RemoveNetchFwRules()
        {
            if (!FirewallWAS.IsSupported)
                return;

            try
            {
                foreach (var rule in FirewallManager.Instance.Rules.Where(r
                    => r.ApplicationName?.StartsWith(Global.NetchDir, StringComparison.OrdinalIgnoreCase) ?? r.Name == Netch))
                    FirewallManager.Instance.Rules.Remove(rule);
            }
            catch (Exception e)
            {
                Global.Logger.Warning("清除防火墙规则错误\n" + e);
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
}