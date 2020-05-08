using Netch.Models.GitHubRelease;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Netch.Controllers
{
    public class UpdateChecker
    {
        private const string DefaultUserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.70 Safari/537.36";

        private const int DefaultGetTimeout = 30000;

        private const string Owner = @"NetchX";
        private const string Repo = @"Netch";

        public string LatestVersionNumber;
        public string LatestVersionUrl;

        public event EventHandler NewVersionFound;
        public event EventHandler NewVersionFoundFailed;
        public event EventHandler NewVersionNotFound;

        public const string Name = @"Netch";
        public const string Copyright = @"Copyright © 2019 - 2020";
        public const string Version = @"1.4.6";

        public async void Check(bool notifyNoFound, bool isPreRelease)
        {
            try
            {
                var updater = new GitHubRelease(Owner, Repo);
                var url = updater.AllReleaseUrl;

                var json = await GetAsync(url, true);

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
                    if (notifyNoFound)
                    {
                        NewVersionNotFound?.Invoke(this, new EventArgs());
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                if (notifyNoFound)
                {
                    NewVersionFoundFailed?.Invoke(this, new EventArgs());
                }
            }
        }

        private static async Task<string> GetAsync(string url, bool useProxy, string userAgent = @"", double timeout = DefaultGetTimeout)
        {
            var httpClientHandler = new HttpClientHandler
            {
                UseProxy = useProxy
            };
            var httpClient = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromMilliseconds(timeout)
            };
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add(@"User-Agent", string.IsNullOrWhiteSpace(userAgent) ? DefaultUserAgent : userAgent);

            var response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();
            var resultStr = await response.Content.ReadAsStringAsync();
            return resultStr;
        }
    }
}
