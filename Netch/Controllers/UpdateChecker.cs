using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        public const string Version = @"1.4.12";

        public string LatestVersionNumber;
        public string LatestVersionUrl;

        public event EventHandler NewVersionFound;
        public event EventHandler NewVersionFoundFailed;
        public event EventHandler NewVersionNotFound;

        public void Check(bool notifyNoFound, bool isPreRelease)
        {
            try
            {
                var updater = new GitHubRelease(Owner, Repo);
                var url = updater.AllReleaseUrl;

                var json = WebUtil.DownloadString(WebUtil.CreateRequest(url));

                var releases = JsonConvert.DeserializeObject<List<Release>>(json);
                var latestRelease = VersionUtil.GetLatestRelease(releases, isPreRelease);
                if (VersionUtil.CompareVersion(latestRelease.tag_name, Version) > 0)
                {
                    LatestVersionNumber = latestRelease.tag_name;
                    LatestVersionUrl = latestRelease.html_url;
                    NewVersionFound?.Invoke(this, new EventArgs());
                }
                else
                {
                    LatestVersionNumber = latestRelease.tag_name;
                    if (notifyNoFound) NewVersionNotFound?.Invoke(this, new EventArgs());
                }
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
                if (notifyNoFound) NewVersionFoundFailed?.Invoke(this, new EventArgs());
            }
        }
    }
}