using System.Net;
using System.Text;
using Microsoft.VisualStudio.Threading;

namespace Netch.Utils;

public static class WebUtil
{
    public const string DefaultUserAgent =
        @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.61 Safari/537.36 Edg/94.0.992.31";

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

    public static async Task<byte[]> DownloadBytesAsync(HttpWebRequest req)
    {
        using var webResponse = await req.GetResponseAsync();
        var memoryStream = new MemoryStream();
        await using (memoryStream)
        {
            var input = webResponse.GetResponseStream();
            await using (input)
            {
                await input.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }

    public static async Task<(HttpStatusCode, string)> DownloadStringAsync(HttpWebRequest req, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        using var webResponse = (HttpWebResponse)await req.GetResponseAsync();

        var responseStream = webResponse.GetResponseStream();
        await using (responseStream)
        {
            using var streamReader = new StreamReader(responseStream, encoding);

            return (webResponse.StatusCode, await streamReader.ReadToEndAsync());
        }
    }

    public static Task DownloadFileAsync(string address, string fileFullPath, IProgress<int>? progress = null)
    {
        return DownloadFileAsync(CreateRequest(address), fileFullPath, progress);
    }

    public static async Task DownloadFileAsync(HttpWebRequest req, string fileFullPath, IProgress<int>? progress)
    {
        var fileStream = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await using (fileStream)
        {
            using var webResponse = (HttpWebResponse)await req.GetResponseAsync();
            var input = webResponse.GetResponseStream();
            await using (input)
            {
                using var downloadTask = input.CopyToAsync(fileStream);
                if (progress != null)
                    ReportProgressAsync(webResponse.ContentLength, downloadTask, fileStream, progress, 200).Forget();

                await downloadTask;
            }
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

            await Task.Delay(interval);
        }
    }
}