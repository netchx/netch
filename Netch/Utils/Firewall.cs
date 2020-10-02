using System;
using System.IO;
using System.Linq;
using NetFwTypeLib;

namespace Netch.Utils
{
    public static class Firewall
    {
        private static readonly string[] ProgramPath =
        {
            "bin/NTT.exe",
            "bin/Privoxy.exe",
            "bin/Shadowsocks.exe",
            "bin/ShadowsocksR.exe",
            "bin/Trojan.exe",
            "bin/tun2socks.exe",
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
            try
            {
                RemoveFwRules(NetchAutoRule);
            }
            catch (Exception e)
            {
                Logging.Warning("添加防火墙规则错误\n" + e);
            }
        }

        /// <summary>
        /// Netch 自带程序添加防火墙
        /// </summary>
        public static void AddNetchFwRules()
        {
            try
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
            catch (Exception e)
            {
                Logging.Warning("添加防火墙规则错误(如已关闭防火墙则可无视此错误)\n" + e);
            }
        }

        /// <summary>
        /// 清除防火墙规则 (Netch 自带程序)
        /// </summary>
        private static void RemoveNetchFwRules()
        {
            try
            {
                RemoveFwRules(Netch);
            }
            catch (Exception e)
            {
                Logging.Warning("清除防火墙规则错误\n" + e);
                // ignored
            }
        }

        #region 封装

        private static readonly INetFwPolicy2 FwPolicy = (INetFwPolicy2) Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

        private static void AddFwRule(string ruleName, string exeFullPath)
        {
            var rule = NewFwRule();

            rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            // ApplicationName 大小不敏感
            rule.ApplicationName = exeFullPath;
            // rule.Description = "";
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
                var rule = (INetFwRule2) FwPolicy.Rules.Item(ruleName);
                return rule.ApplicationName;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static int GetFwRulesNumber(string ruleName)
        {
            return FwPolicy.Rules.Cast<INetFwRule2>().Count(rule => rule.Name == ruleName);
        }

        #endregion
    }
}