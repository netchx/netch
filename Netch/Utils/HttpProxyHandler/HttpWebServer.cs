using System;
using System.Net;
using System.Text;
using System.Threading;

namespace Netch.Utils.HttpProxyHandler
{
    public class HttpWebServer
    {
        private HttpListener? _listener;
        private readonly Func<HttpListenerRequest, string>? _responderMethod;

        public HttpWebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
        {
            try
            {
                _listener = new HttpListener();

                if (!HttpListener.IsSupported)
                    throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");

                // URI prefixes are required, for example 
                // "http://localhost:8080/index/".
                if (prefixes == null || prefixes.Length == 0)
                    throw new ArgumentException("prefixes");

                // A responder method is required
                if (method == null)
                    throw new ArgumentException("method");

                foreach (var s in prefixes)
                    _listener.Prefixes.Add(s);

                _responderMethod = method;
                _listener.Start();
            }
            catch (Exception ex)
            {
                Logging.Error("HttpWebServer():" + ex.Message);
            }
        }

        public HttpWebServer(Func<HttpListenerRequest, string> method, params string[] prefixes) : this(prefixes, method)
        {
        }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Logging.Info("Webserver running...");
                try
                {
                    while (_listener!.IsListening)
                        ThreadPool.QueueUserWorkItem(c =>
                            {
                                var ctx = (HttpListenerContext) c;
                                try
                                {
                                    var rstr = _responderMethod!(ctx.Request);
                                    var buf = Encoding.UTF8.GetBytes(rstr);
                                    ctx.Response.StatusCode = 200;
                                    ctx.Response.ContentType = "application/x-ns-proxy-autoconfig";
                                    ctx.Response.ContentLength64 = buf.Length;
                                    ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                                }
                                catch
                                {
                                    // ignored
                                }
                                finally
                                {
                                    ctx.Response.OutputStream.Close();
                                }
                            },
                            _listener.GetContext());
                }
                catch (Exception ex)
                {
                    Logging.Error(ex.Message);
                }
            });
        }

        public void Stop()
        {
            if (_listener != null)
            {
                _listener.Stop();
                _listener.Close();
                _listener = null;
            }
        }
    }
}