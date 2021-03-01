using System;
using System.Net;
using System.Text;

namespace Netch.Utils.HttpProxyHandler
{
    public class HttpWebServer
    {
        private readonly Func<HttpListenerRequest, string>? _responderMethod;
        private HttpListener? _listener;

        public HttpWebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
        {
            _listener = new HttpListener();

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

        public HttpWebServer(Func<HttpListenerRequest, string> method, params string[] prefixes) : this(prefixes, method)
        {
        }

        public void StartWaitingRequest()
        {
            Logging.Info("Webserver running...");
            while (_listener?.IsListening ?? false)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = _listener.GetContext();
                }
                catch
                {
                    break;
                }

                try
                {
                    var rstr = _responderMethod!(ctx.Request);
                    var buf = Encoding.UTF8.GetBytes(rstr);
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/x-ns-proxy-autoconfig";
                    ctx.Response.ContentLength64 = buf.Length;
                    ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                }
                finally
                {
                    ctx.Response.OutputStream.Close();
                }
            }
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