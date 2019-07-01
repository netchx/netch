using MihaZupan.Enums;
using MihaZupan.Enums.Socks5;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MihaZupan
{
    internal static class Socks5
    {
        public static SocketConnectionResult TryCreateTunnel(Socket socks5Socket, string destAddress, int destPort, ProxyInfo proxy, bool resolveHostnamesLocally)
        {
            try
            {
                // SEND HELLO
                socks5Socket.Send(BuildHelloMessage(proxy.Authenticate));

                // RECEIVE HELLO RESPONSE - HANDLE AUTHENTICATION
                byte[] buffer = new byte[255];
                if (socks5Socket.Receive(buffer) != 2)
                    return SocketConnectionResult.InvalidProxyResponse;
                if (buffer[0] != SocksVersion)
                    return SocketConnectionResult.InvalidProxyResponse;
                if (buffer[1] == (byte)Authentication.UsernamePassword)
                {
                    if (!proxy.Authenticate)
                    {
                        // Proxy server is requesting UserPass auth even tho we did not allow it
                        return SocketConnectionResult.InvalidProxyResponse;
                    }
                    else
                    {
                        // We have to try and authenticate using the Username and Password
                        // https://tools.ietf.org/html/rfc1929
                        socks5Socket.Send(proxy.AuthenticationMessage);
                        if (socks5Socket.Receive(buffer) != 2)
                            return SocketConnectionResult.InvalidProxyResponse;
                        if (buffer[0] != SubnegotiationVersion)
                            return SocketConnectionResult.InvalidProxyResponse;
                        if (buffer[1] != 0)
                            return SocketConnectionResult.AuthenticationError;
                    }
                }
                else if (buffer[1] != (byte)Authentication.NoAuthentication)
                    return SocketConnectionResult.AuthenticationError;

                if (resolveHostnamesLocally && Helpers.GetAddressType(destAddress) == AddressType.DomainName)
                {
                    destAddress = destAddress.Resolve().ToString();
                }

                // SEND REQUEST
                socks5Socket.Send(BuildRequestMessage(Command.Connect, Helpers.GetAddressType(destAddress), destAddress, destPort));

                // RECEIVE RESPONSE
                int received = socks5Socket.Receive(buffer);
                if (received < 8)
                    return SocketConnectionResult.InvalidProxyResponse;
                if (buffer[0] != SocksVersion)
                    return SocketConnectionResult.InvalidProxyResponse;
                if (buffer[1] > 8)
                    return SocketConnectionResult.InvalidProxyResponse;
                if (buffer[1] != 0)
                    return (SocketConnectionResult)buffer[1];
                if (buffer[2] != 0)
                    return SocketConnectionResult.InvalidProxyResponse;
                if (buffer[3] != 1 && buffer[3] != 3 && buffer[3] != 4)
                    return SocketConnectionResult.InvalidProxyResponse;

                AddressType boundAddress = (AddressType)buffer[3];
                if (boundAddress == AddressType.IPv4)
                {
                    if (received != 10)
                        return SocketConnectionResult.InvalidProxyResponse;
                }
                else if (boundAddress == AddressType.IPv6)
                {
                    if (received != 22)
                        return SocketConnectionResult.InvalidProxyResponse;
                }
                else
                {
                    int domainLength = buffer[4];
                    if (received != 7 + domainLength)
                        return SocketConnectionResult.InvalidProxyResponse;
                }

                return SocketConnectionResult.OK;
            }
            catch (SocketException ex)
            {
                return ex.ToConnectionResult();
            }
            catch
            {
                return SocketConnectionResult.UnknownError;
            }
        }

        private const byte SubnegotiationVersion = 0x01;
        private const byte SocksVersion = 0x05;

        private static byte[] BuildHelloMessage(bool doUsernamePasswordAuth)
        {
            byte[] hello = new byte[doUsernamePasswordAuth ? 4 : 3];
            hello[0] = SocksVersion;
            hello[1] = (byte)(doUsernamePasswordAuth ? 2 : 1);
            hello[2] = (byte)Authentication.NoAuthentication;
            if (doUsernamePasswordAuth)
            {
                hello[3] = (byte)Authentication.UsernamePassword;
            }
            return hello;
        }
        private static byte[] BuildRequestMessage(Command command, AddressType addressType, string address, int port)
        {
            int addressLength;
            byte[] addressBytes;
            switch (addressType)
            {
                case AddressType.IPv4:
                case AddressType.IPv6:
                    addressBytes = IPAddress.Parse(address).GetAddressBytes();
                    addressLength = addressBytes.Length;
                    break;

                case AddressType.DomainName:
                    byte[] domainBytes = Encoding.UTF8.GetBytes(address);
                    addressLength = 1 + domainBytes.Length;
                    addressBytes = new byte[addressLength];
                    addressBytes[0] = (byte)domainBytes.Length;
                    Array.Copy(domainBytes, 0, addressBytes, 1, domainBytes.Length);
                    break;

                default:
                    throw new ArgumentException("Unknown address type");
            }

            byte[] request = new byte[6 + addressLength];
            request[0] = SocksVersion;
            request[1] = (byte)command;
            //request[2] = 0x00;
            request[3] = (byte)addressType;
            Array.Copy(addressBytes, 0, request, 4, addressLength);
            request[request.Length - 2] = (byte)(port / 256);
            request[request.Length - 1] = (byte)(port % 256);
            return request;
        }
        public static byte[] BuildAuthenticationMessage(string username, string password)
        {
            byte[] usernameBytes = Encoding.UTF8.GetBytes(username);
            if (usernameBytes.Length > 255) throw new ArgumentOutOfRangeException("Username is too long");

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            if (passwordBytes.Length > 255) throw new ArgumentOutOfRangeException("Password is too long");

            byte[] authMessage = new byte[3 + usernameBytes.Length + passwordBytes.Length];
            authMessage[0] = SubnegotiationVersion;
            authMessage[1] = (byte)usernameBytes.Length;
            Array.Copy(usernameBytes, 0, authMessage, 2, usernameBytes.Length);
            authMessage[2 + usernameBytes.Length] = (byte)passwordBytes.Length;
            Array.Copy(passwordBytes, 0, authMessage, 3 + usernameBytes.Length, passwordBytes.Length);
            return authMessage;
        }
    }
}
