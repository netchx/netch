using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Netch.Utils.HttpProxyHandler
{
    /// <summary>
    /// 提供PAC功能支持
    /// </summary>
    class PACListHandle
    {
        public event EventHandler<ResultEventArgs> UpdateCompleted;

        public class ResultEventArgs : EventArgs
        {
            public bool Success;

            public ResultEventArgs(bool success)
            {
                Success = success;
            }
        }

        private const string GFWLIST_URL = "https://raw.githubusercontent.com/gfwlist/gfwlist/master/gfwlist.txt";

        private static readonly IEnumerable<char> IgnoredLineBegins = new[] { '!', '[' };

        /// <summary>
        /// 更新PAC文件（GFWList）
        /// </summary>
        //public void UpdatePACFromGFWList()
        //{
        //    WebClient http = new WebClient();
        //    http.DownloadStringCompleted += http_DownloadStringCompleted;
        //    http.DownloadStringAsync(new Uri(GFWLIST_URL));
        //}

        //private void http_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        //{
        //    try
        //    {
        //        File.WriteAllText(NUtils.GetTempPath("gfwlist.txt"), e.Result, Encoding.UTF8);
        //        List<string> lines = ParseResult(e.Result);
        //        string abpContent = NUtils.UnGzip(Resources.abp_js);
        //        abpContent = abpContent.Replace("__RULES__", JsonConvert.SerializeObject(lines, Formatting.Indented));
        //        File.WriteAllText(NUtils.GetPath("pac.txt"), abpContent, Encoding.UTF8);
        //        if (UpdateCompleted != null) UpdateCompleted(this, new ResultEventArgs(true));
        //    }
        //    catch (Exception ex)
        //    {
        //        Logging.Error("http_DownloadStringCompleted():" + ex.Message);
        //    }
        //}

        //public static List<string> ParseResult(string response)
        //{
        //    byte[] bytes = Convert.FromBase64String(response);
        //    string content = Encoding.ASCII.GetString(bytes);
        //    List<string> valid_lines = new List<string>();
        //    using (var sr = new StringReader(content))
        //    {
        //        foreach (var line in sr.NonWhiteSpaceLines())
        //        {
        //            if (line.BeginWithAny(IgnoredLineBegins))
        //                continue;
        //            valid_lines.Add(line);
        //        }
        //    }
        //    return valid_lines;
        //}
    }
}
