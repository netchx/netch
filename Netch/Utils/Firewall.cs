using System;
using System.IO;
using NetFwTypeLib;

namespace Netch.Utils
{
    public class Firewall
    {
        private static readonly string[] ProgramPath =
        {
            "bin/NTT.exe",
            "bin/Privoxy.exe",
            "bin/Redirector.exe",
            "bin/Shadowsocks.exe",
            "bin/ShadowsocksR.exe",
            "bin/Trojan.exe",
            "bin/tun2socks.exe",
            "bin/unbound.exe",
            "bin/v2ray.exe",
            "Netch.exe"
        };

        private const string Netch = "Netch";
        private const string NetchAutoRule = "NetchAutoRule";

        /// <summary>
        /// 添加防火墙规则 (非 Netch 自带程序)
        /// </summary>
        /// <param name="exeFullPath"></param>
        public static void AddFwRule(string exeFullPath)
        {
            AddFwRule(NetchAutoRule, exeFullPath);
        }

        /// <summary>
        /// 清除防火墙规则 (非 Netch 自带程序)
        /// </summary>
        public static void RemoveFwRules()
        {
            RemoveFwRules(NetchAutoRule);
        }

        /// <summary>
        /// Netch 自带程序添加防火墙
        /// </summary>
        public static void AddNetchFwRules()
        {
            if (GetFwRulePath(Netch).StartsWith(Global.NetchDir) && GetFwRulesNumber(Netch) >= ProgramPath.Length) return;
            RemoveNetchFwRules();
            foreach (var p in ProgramPath)
            {
                var path = Path.GetFullPath(p);
                if (File.Exists(path))
                {
                    AddFwRule("Netch", path);
                }
            }
        }

        /// <summary>
        /// 清除防火墙规则 (Netch 自带程序)
        /// </summary>
        private static void RemoveNetchFwRules()
        {
            RemoveFwRules(Netch);
        }

        #region 封装

        private static readonly INetFwPolicy2 FwPolicy = (INetFwPolicy2) Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

        private static void AddFwRule(string ruleName, string exeFullPath)
        {
            var rule = NewFwRule();

            rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            // ApplicationName 大小不敏感
            rule.ApplicationName = exeFullPath;
            // rule.Description = "Used to block all internet access.";
            rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            rule.Enabled = true;
            rule.InterfaceTypes = "All";
            rule.Name = ruleName;

            FwPolicy.Rules.Add(rule);
        }

        private static void RemoveFwRules(string ruleName)
        {
            var c = GetFwRulesNumber(ruleName);
            foreach (var _ in new bool[c])
            {
                FwPolicy.Rules.Remove(ruleName);
            }
        }

        private static INetFwRule NewFwRule()
        {
            return (INetFwRule) Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
        }


        private static string GetFwRulePath(string ruleName)
        {
            try
            {
                var rule = (INetFwRule2)FwPolicy.Rules.Item(ruleName);
            return rule.ApplicationName;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static int GetFwRulesNumber(string ruleName)
        {
            // https://stackoverflow.com/a/53601691
            var i = 0;
            foreach (INetFwRule2 rule in FwPolicy.Rules)
            {
                if (rule.Name == ruleName)
                    i++;
            }
            return i;
        }

        #endregion
    }
}