using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Netch.Utils
{
    public class WebUtil
    {
        private const string DefaultUserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.70 Safari/537.36";

        private static int DefaultGetTimeout => Global.Settings.RequestTimeout;

        public static HttpWebRequest CreateRequest(string url, int? timeout = null, string userAgent = null)
        {
            var req = (HttpWebRequest) WebRequest.Create(url);
            req.UserAgent = string.IsNullOrEmpty(userAgent) ? DefaultUserAgent : userAgent;
            req.Accept = "*/*";
            req.KeepAlive = true;
            req.Timeout = timeout ?? DefaultGetTimeout;
            req.ReadWriteTimeout = timeout ?? DefaultGetTimeout;
            req.Headers.Add("Accept-Charset", "utf-8");
            return req;
        }

        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="WebException"></exception>
        public static async Task<string> DownloadStringAsync(HttpWebRequest req)
        {
            string content;
            var response = (HttpWebResponse) await req.GetResponseAsync();
            using (var responseStream = response.GetResponseStream())
            {
                using (var sr = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")))
                {
                    content = await sr.ReadToEndAsync();
                }
            }

            response.Close();
            return content;
        }

        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="WebException"></exception>
        public static string DownloadString(HttpWebRequest req)
        {
            string content;
            var response = (HttpWebResponse) req.GetResponse();
            using (var responseStream = response.GetResponseStream())
            {
                using (var sr = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")))
                {
                    content = sr.ReadToEnd();
                }
            }

            response.Close();
            return content;
        }

        /// <param name="req"></param>
        /// <param name="fileFullPath"></param>
        /// <exception cref="WebException"></exception>
        public static async Task DownloadFileAsync(HttpWebRequest req, string fileFullPath)
        {
            var dir = Path.GetDirectoryName(fileFullPath) ?? throw new ArgumentException();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(fileFullPath, await DownloadStringAsync(req));
        }
    }
}