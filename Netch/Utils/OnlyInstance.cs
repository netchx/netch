using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Netch.Utils
{
    public static class OnlyInstance
    {
        public enum Commands
        {
            Show,
            Exit
        }

        public static event EventHandler<Commands> Called;

        private static void OnCalled(Commands e)
        {
            Called?.Invoke(null, e);
        }

        public static async void Server()
        {
            try
            {
                const int tryLimit = 3;
                var i = tryLimit;
                while (i > 0)
                    try
                    {
                        PortHelper.CheckPort(Global.Settings.UDPSocketPort, PortType.UDP);
                        if (i != tryLimit)
                            Configuration.Save();
                        break;
                    }
                    catch
                    {
                        Global.Settings.UDPSocketPort = PortHelper.GetAvailablePort(PortType.UDP);
                        i--;
                    }

                var data = new byte[1024];
                var newsock = new UdpClient(new IPEndPoint(IPAddress.Loopback, Global.Settings.UDPSocketPort));

                while (true)
                {
                    var result = await newsock.ReceiveAsync();
                    data = result.Buffer;
                    if (Enum.TryParse<Commands>(Encoding.ASCII.GetString(data, 0, data.Length), out var command))
                        OnCalled(command);
                }
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
            }
        }

        public static async void Send(Commands command)
        {
            try
            {
                using var udpClient = new UdpClient(Global.Settings.UDPSocketPort);
                udpClient.Connect(IPAddress.Loopback, Global.Settings.UDPSocketPort);
                var sendBytes = Encoding.ASCII.GetBytes(command.ToString());
                await udpClient.SendAsync(sendBytes, sendBytes.Length);

                udpClient.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}