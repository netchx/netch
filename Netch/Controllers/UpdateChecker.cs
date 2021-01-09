using System;
using System.Collections.Generic;
using System.Net;
using Netch.Models.GitHubRelease;
using Netch.Utils;
using Newtonsoft.Json;

namespace Netch.Controllers
{
    public class UpdateChecker
    {
        public const string Owner = @"NetchX";
        public const string Repo = @"Netch";

        public const string Name = @"Netch";
        public const string Copyright = @"Copyright © 2019 - 2020";

        public const string AssemblyVersion = @"1.7.1";
        private const string Suffix = @"";

        public static readonly string Version = $"{AssemblyVersion}{(string.IsNullOrEmpty(Suffix) ? "" : $"-{Suffix}")}";

        public string LatestVersionNumber;
        public string LatestVersionUrl;
        public Release LatestRelease;

        public event EventHandler NewVersionFound;
        public event EventHandler NewVersionFoundFailed;
        public event EventHandler NewVersionNotFound;

        public async void Check(bool isPreRelease)
        {
            try
            {
                var updater = new GitHubRelease(Owner, Repo);
                var url = updater.AllReleaseUrl;

                var json = await WebUtil.DownloadStringAsync(WebUtil.CreateRequest(url));

                var releases = JsonConvert.DeserializeObject<List<Release>>(json);
                LatestRelease = VersionUtil.GetLatestRelease(releases, isPreRelease);
                LatestVersionNumber = LatestRelease.tag_name;
                LatestVersionUrl = LatestRelease.html_url;
                Logging.Info($"Github 最新发布版本: {LatestRelease.tag_name}");
                if (VersionUtil.CompareVersion(LatestRelease.tag_name, Version) > 0)
                {
                    Logging.Info("发现新版本");
                    NewVersionFound?.Invoke(this, new EventArgs());
                }
                else
                {
                    Logging.Info("目前是最新版本");
                    NewVersionNotFound?.Invoke(this, new EventArgs());
                }
            }
            catch (Exception e)
            {
                if (e is WebException)
                    Logging.Warning($"获取新版本失败: {e.Message}");
                else
                {
                    Logging.Warning(e.ToString());
                }

                NewVersionFoundFailed?.Invoke(this, new EventArgs());
            }
        }
    }
}
