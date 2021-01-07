using System;
using System.IO;
using System.Linq;
using Netch.Utils;

namespace Netch.Controllers
{
    public class NTTController : Guard, IController
    {
        public override string Name { get; protected set; } = "NTT";
        public override string MainFile { get; protected set; } = "NTT.exe";

        /// <summary>
        ///     启动 NatTypeTester
        /// </summary>
        /// <returns></returns>
        public (string, string, string) Start()
        {
            string localEnd=null;
            string publicEnd=null;
            string result =null;
            string bindingTest=null;

            try
            {
                InitInstance($" {Global.Settings.STUN_Server} {Global.Settings.STUN_Server_Port}");
                Instance.OutputDataReceived += OnOutputDataReceived;
                Instance.ErrorDataReceived += OnOutputDataReceived;
                Instance.Start();
                var output = Instance.StandardOutput.ReadToEnd();
                try
                {
                    File.WriteAllText(Path.Combine(Global.NetchDir, $"logging\\{Name}.log"), output);
                }
                catch (Exception e)
                {
                    Logging.Warning($"写入 {Name} 日志错误：\n" + e.Message);
                }

                foreach (var line in output.Split('\n'))
                {
                    var str = line.Split(':').Select(s => s.Trim()).ToArray();
                    if (str.Length < 2)
                        continue;
                    var key = str[0];
                    var value = str[1];
                    switch (key)
                    {
                        case "Other address is":
                        case "Nat mapping behavior":
                        case "Nat filtering behavior":
                            break;
                        case "Binding test":
                            bindingTest = value;
                            break;
                        case "Local address":
                            localEnd = value;
                            break;
                        case "Mapped address":
                            publicEnd = value;
                            break;
                        case "result":
                            result = value;
                            break;
                        default:
                            result = str.Last();
                            break;
                    }
                }

                if (bindingTest == "Fail")
                    result = "UdpBlocked";
                return (result, localEnd, publicEnd);
            }
            catch (Exception e)
            {
                Logging.Error($"{Name} 控制器出错:\n" + e);
                try
                {
                    Stop();
                }
                catch
                {
                    // ignored
                }

                return (null, null, null);
            }
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}