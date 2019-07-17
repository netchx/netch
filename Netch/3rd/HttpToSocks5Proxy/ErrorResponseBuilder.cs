using MihaZupan.Enums;

namespace MihaZupan
{
    internal static class ErrorResponseBuilder
    {
        public static string Build(SocketConnectionResult error, string httpVersion)
        {
            switch (error)
            {
                case SocketConnectionResult.AuthenticationError:
                    return httpVersion + "401 Unauthorized\r\n\r\n";

                case SocketConnectionResult.HostUnreachable:
                case SocketConnectionResult.ConnectionRefused:
                case SocketConnectionResult.ConnectionReset:
                    return string.Concat(httpVersion, "502 ", error.ToString(), "\r\n\r\n");

                default:
                    return string.Concat(httpVersion, "500 Internal Server Error\r\nX-Proxy-Error-Type: ", error.ToString(), "\r\n\r\n");
            }
        }
    }
}
