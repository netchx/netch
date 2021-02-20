using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Netch.Models.GitHubRelease;
using Netch.Utils;
using Newtonsoft.Json;
using static Netch.Updater.Updater;

namespace Netch.Controllers
{
    public static class UpdateChecker
    {
        public const string Owner = @"NetchX";
        public const string Repo = @"Netch";

        public const string Name = @"Netch";
        public const string Copyright = @"Copyright © 2019 - 2021";

        public const string AssemblyVersion = @"1.7.6";
        private const string Suffix = @"";

        public static readonly string Version = $"{AssemblyVersion}{(string.IsNullOrEmpty(Suffix) ? "" : $"-{Suffix}")}";

        public static string LatestVersionNumber;
        public static string LatestVersionUrl;
        public static Release LatestRelease;

        public static event EventHandler NewVersionFound;

        public static event EventHandler NewVersionFoundFailed;

        public static event EventHandler NewVersionNotFound;

        public static async void Check(bool isPreRelease)
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

        public static async Task DownloadUpdate(DownloadProgressChangedEventHandler onDownloadProgressChanged)
        {
            using WebClient client = new();

            var latestVersionDownloadUrl = LatestRelease.assets[0].browser_download_url;
            var tagPage = await client.DownloadStringTaskAsync(LatestVersionUrl);
            var match = Regex.Match(tagPage, @"<td .*>(?<sha256>.*)</td>", RegexOptions.Singleline);

            // TODO Replace with regex get basename and sha256 
            var fileName = Path.GetFileName(new Uri(latestVersionDownloadUrl).LocalPath);
            fileName = fileName.Insert(fileName.LastIndexOf('.'), LatestVersionNumber);
            var fileFullPath = Path.Combine(Global.NetchDir, "data", fileName);

            var sha256 = match.Groups["sha256"].Value;

            if (File.Exists(fileFullPath))
            {
                if (Utils.Utils.SHA256CheckSum(fileFullPath) == sha256)
                {
                    UpdateNetch(fileFullPath);
                    return;
                }

                File.Delete(fileFullPath);
            }

            try
            {
                client.DownloadProgressChanged += onDownloadProgressChanged;
                await client.DownloadFileTaskAsync(new Uri(latestVersionDownloadUrl), fileFullPath);
                client.DownloadProgressChanged -= onDownloadProgressChanged;
            }
            catch (Exception e)
            {
                throw new Exception(i18N.Translate("Download Update Failed", ": ") + e.Message);
            }

            if (Utils.Utils.SHA256CheckSum(fileFullPath) != sha256)
                throw new Exception(i18N.Translate("The downloaded file has the wrong hash"));

            UpdateNetch(fileFullPath);
        }
    }
}