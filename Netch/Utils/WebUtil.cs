using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Netch.Utils
{
    public static class WebUtil
    {
        public const string DefaultUserAgent =
            @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.67 Safari/537.36 Edg/87.0.664.55";

        static WebUtil()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        private static int DefaultGetTimeout => Global.Settings.RequestTimeout;

        public static HttpWebRequest CreateRequest(string url, int? timeout = null, string? userAgent = null)
        {
            var req = (HttpWebRequest) WebRequest.Create(url);
            req.UserAgent = string.IsNullOrWhiteSpace(userAgent) ? DefaultUserAgent : userAgent;
            req.Accept = "*/*";
            req.KeepAlive = true;
            req.Timeout = timeout ?? DefaultGetTimeout;
            req.ReadWriteTimeout = timeout ?? DefaultGetTimeout;
            req.Headers.Add("Accept-Charset", "utf-8");
            return req;
        }

        public static IPEndPoint BestLocalEndPoint(IPEndPoint remoteIPEndPoint)
        {
            var testSocket = new Socket(remoteIPEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            testSocket.Connect(remoteIPEndPoint);
            return (IPEndPoint) testSocket.LocalEndPoint;
        }

        /// <summary>
        ///     异步下载
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static async Task<byte[]> DownloadBytesAsync(HttpWebRequest req)
        {
            using var webResponse = (HttpWebResponse) await req.GetResponseAsync();
            using var memoryStream = new MemoryStream();
            using var input = webResponse.GetResponseStream();

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
        public static string DownloadString(HttpWebRequest req, out HttpWebResponse rep, string encoding = "UTF-8")
        {
            rep = (HttpWebResponse) req.GetResponse();
            using var responseStream = rep.GetResponseStream();
            using var streamReader = new StreamReader(responseStream, Encoding.GetEncoding(encoding));

            return streamReader.ReadToEnd();
        }

        /// <summary>
        ///     异步下载并编码为字符串
        /// </summary>
        /// <param name="req"></param>
        /// <param name="encoding">编码，默认UTF-8</param>
        /// <returns></returns>
        public static async Task<string> DownloadStringAsync(HttpWebRequest req, string encoding = "UTF-8")
        {
            using var webResponse = await req.GetResponseAsync();
            using var responseStream = webResponse.GetResponseStream();
            using var streamReader = new StreamReader(responseStream, Encoding.GetEncoding(encoding));

            return await streamReader.ReadToEndAsync();
        }

        /// <summary>
        ///     异步下载到文件
        /// </summary>
        /// <param name="req"></param>
        /// <param name="fileFullPath"></param>
        /// <returns></returns>
        public static async Task DownloadFileAsync(HttpWebRequest req, string fileFullPath)
        {
            using var webResponse = (HttpWebResponse) await req.GetResponseAsync();
            using var input = webResponse.GetResponseStream();
            using var fileStream = File.OpenWrite(fileFullPath);

            await input.CopyToAsync(fileStream);
            fileStream.Flush();
        }
    }
}