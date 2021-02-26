#nullable disable
using System;

namespace Netch.Models.GitHubRelease
{
    public class Release
    {
        public string url { get; set; }

        public string assets_url { get; set; }

        public string upload_url { get; set; }

        public string html_url { get; set; }

        public int id { get; set; }

        public string node_id { get; set; }

        public string tag_name { get; set; }

        public string target_commitish { get; set; }

        public string name { get; set; }

        public bool draft { get; set; }

        public GitHubUser author { get; set; }

        public bool prerelease { get; set; }

        public DateTime created_at { get; set; }

        public DateTime published_at { get; set; }

        public Asset[] assets { get; set; }

        public string tarball_url { get; set; }

        public string zipball_url { get; set; }

        public string body { get; set; }
    }
}