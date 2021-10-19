using System.IO;
using System.Net;
using System.Text;

namespace Netch.Utils
{
    public static class HTTP
    {
        static HTTP()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls13;
        }

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
            var request = WebRequest.Create(url) as HttpWebRequest;
            request.UserAgent = DefaultUA;
            request.Accept = "*/*";
            request.KeepAlive = true;
            request.Timeout = timeout;

            return request;
        }

        public static string GetString(string url)
        {
            var request = CreateRequest(url, 10000);
            var response = request.GetResponse() as HttpWebResponse;

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
            var response = request.GetResponse() as HttpWebResponse;

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
            var response = request.GetResponse() as HttpWebResponse;

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
