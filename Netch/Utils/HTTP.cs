using System.IO;
using System.Net;
using System.Text;

namespace Netch.Utils
{
    public static class HTTP
    {
        /// <summary>
        ///     User Agent
        /// </summary>
        public static readonly string DefaultUA = $"Netch/{Global.VerCode}";

        /// <summary>
        ///     创建请求
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="timeout">超时</param>
        /// <returns></returns>
        public static HttpWebRequest CreateRequest(string url, int timeout = 0)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = DefaultUA;
            request.Accept = "*/*";
            request.KeepAlive = true;
            request.Timeout = timeout;

            return request;
        }

        public static string GetString(string url)
        {
            var request = CreateRequest(url, 10000);
            var response = (HttpWebResponse)request.GetResponse();

            using (var rs = response.GetResponseStream())
            {
                using (var sr = new StreamReader(rs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        public static string GetString(string url, int timeout)
        {
            var request = CreateRequest(url, timeout);
            var response = (HttpWebResponse)request.GetResponse();

            using (var rs = response.GetResponseStream())
            {
                using (var sr = new StreamReader(rs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        public static string GetString(HttpWebRequest request)
        {
            var response = (HttpWebResponse)request.GetResponse();

            using (var rs = response.GetResponseStream())
            {
                using (var sr = new StreamReader(rs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
