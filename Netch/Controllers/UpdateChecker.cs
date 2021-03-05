using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Netch.Models.GitHubRelease;
using Netch.Utils;

namespace Netch.Controllers
{
    public static class UpdateChecker
    {
        public const string Owner = @"NetchX";
        public const string Repo = @"Netch";

        public const string Name = @"Netch";
        public const string Copyright = @"Copyright © 2019 - 2021";

        public const string AssemblyVersion = @"1.8.1";
        private const string Suffix = @"";

        public static readonly string Version = $"{AssemblyVersion}{(string.IsNullOrEmpty(Suffix) ? "" : $"-{Suffix}")}";

        public static Release LatestRelease = null!;

        public static string LatestVersionNumber => LatestRelease.tag_name;

        public static string LatestVersionUrl => LatestRelease.html_url;

        public static event EventHandler? NewVersionFound;

        public static event EventHandler? NewVersionFoundFailed;

        public static event EventHandler? NewVersionNotFound;

        public static async Task Check(bool isPreRelease)
        {
            try
            {
                var updater = new GitHubRelease(Owner, Repo);
                var url = updater.AllReleaseUrl;

                var json = await WebUtil.DownloadStringAsync(WebUtil.CreateRequest(url));

                var releases = JsonSerializer.Deserialize<List<Release>>(json)!;
                LatestRelease = VersionUtil.GetLatestRelease(releases, isPreRelease);
                Logging.Info($"Github 最新发布版本: {LatestRelease.tag_name}");
                if (VersionUtil.CompareVersion(LatestRelease.tag_name, Version) > 0)
                {
                    Logging.Info("发现新版本");
                    NewVersionFound?.Invoke(null, new EventArgs());
                }
                else
                {
                    Logging.Info("目前是最新版本");
                    NewVersionNotFound?.Invoke(null, new EventArgs());
                }
            }
            catch (Exception e)
            {
                if (e is WebException)
                    Logging.Warning($"获取新版本失败: {e.Message}");
                else
                    Logging.Warning(e.ToString());

                NewVersionFoundFailed?.Invoke(null, new EventArgs());
            }
        }

        public static bool GetFileNameAndHashFromMarkdownForm(in string text, out string fileName, out string sha256, string? keyword = null)
        {
            IEnumerable<Match> matches;
            try
            {
                matches = Regex.Matches(text, @"^\| (?<filename>.*) \| (?<sha256>.*) \|\r?$", RegexOptions.Multiline).Cast<Match>().Skip(2);
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
                throw new Exception(i18N.Translate("Find update filename and hash failed"));
            }

            Match match = keyword == null ? matches.First() : matches.First(m => m.Groups["filename"].Value.Contains(keyword));

            if (match != null)
            {
                fileName = match.Groups["filename"].Value;
                sha256 = match.Groups["sha256"].Value;
                return true;
            }

            fileName = string.Empty;
            sha256 = string.Empty;
            return false;
        }
    }
}