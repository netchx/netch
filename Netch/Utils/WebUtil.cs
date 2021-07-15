using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Netch.Utils
{
    public static class WebUtil
    {
        public const string DefaultUserAgent =
            @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.67";

        static WebUtil()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        private static int DefaultGetTimeout => Global.Settings.RequestTimeout;

        public static HttpWebRequest CreateRequest(string url, int? timeout = null, string? userAgent = null)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = string.IsNullOrWhiteSpace(userAgent) ? DefaultUserAgent : userAgent;
            req.Accept = "*/*";
            req.KeepAlive = true;
            req.Timeout = timeout ?? DefaultGetTimeout;
            req.ReadWriteTimeout = timeout ?? DefaultGetTimeout;
            req.Headers.Add("Accept-Charset", "utf-8");
            return req;
        }

        /// <summary>
        ///     异步下载
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static async Task<byte[]> DownloadBytesAsync(HttpWebRequest req)
        {
            using var webResponse = await req.GetResponseAsync();
            await using var memoryStream = new MemoryStream();
            await using var input = webResponse.GetResponseStream();

            await input.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        /// <summary>
        ///     异步下载并编码为字符串
        /// </summary>
        /// <param name="req"></param>
        /// <param name="rep"></param>
        /// <param name="encoding">编码，默认UTF-8</param>
        /// <returns></returns>
        public static (HttpStatusCode, string) DownloadString(HttpWebRequest req, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            using var rep = (HttpWebResponse)req.GetResponse();
            using var responseStream = rep.GetResponseStream();
            using var streamReader = new StreamReader(responseStream, encoding);

            return (rep.StatusCode, streamReader.ReadToEnd());
        }

        /// <summary>
        ///     异步下载并编码为字符串
        /// </summary>
        /// <param name="req"></param>
        /// <param name="encoding">编码，默认UTF-8</param>
        /// <returns></returns>
        public static async Task<(HttpStatusCode, string)> DownloadStringAsync(HttpWebRequest req, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            using var webResponse = (HttpWebResponse)await req.GetResponseAsync();
            await using var responseStream = webResponse.GetResponseStream();
            using var streamReader = new StreamReader(responseStream, encoding);

            return (webResponse.StatusCode, await streamReader.ReadToEndAsync());
        }

        public static async Task DownloadFileAsync(string address, string fileFullPath, IProgress<int>? progress = null)
        {
            await DownloadFileAsync(CreateRequest(address), fileFullPath, progress);
        }

        public static async Task DownloadFileAsync(HttpWebRequest req, string fileFullPath, IProgress<int>? progress)
        {
            await using (var fileStream = File.Open(fileFullPath, FileMode.Create, FileAccess.Write))
            using (var webResponse = (HttpWebResponse)await req.GetResponseAsync())
            await using (var input = webResponse.GetResponseStream())
            using (var downloadTask = input.CopyToAsync(fileStream))
            {
                if (progress != null)
                    ReportProgressAsync(webResponse.ContentLength, downloadTask, fileStream, progress, 200).Forget();

                await downloadTask;
            }

            progress?.Report(100);
        }

        private static async Task ReportProgressAsync(long total, IAsyncResult downloadTask, Stream stream, IProgress<int> progress, int interval)
        {
            var n = 0;
            while (!downloadTask.IsCompleted)
            {
                var n1 = (int)((double)stream.Length / total * 100);
                if (n != n1)
                {
                    n = n1;
                    progress.Report(n);
                }

                await Task.Delay(interval).ConfigureAwait(false);
            }
        }
    }
}