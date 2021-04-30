using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Netch.Interfaces;
using Netch.Utils;

namespace Netch.Controllers
{
    public class NTTController : Guard, IController
    {
        public override string MainFile { get; protected set; } = "NTT.exe";

        public override string Name { get; } = "NTT";

        public override void Stop()
        {
            StopInstance();
        }

        /// <summary>
        ///     启动 NatTypeTester
        /// </summary>
        /// <returns></returns>
        public async Task<(string?, string?, string?)> Start()
        {
            string? localEnd = null, publicEnd = null, result = null, bindingTest = null;

            try
            {
                InitInstance($" {Global.Settings.STUN_Server} {Global.Settings.STUN_Server_Port}");
                Instance!.Start();

                var output = await Instance.StandardOutput.ReadToEndAsync();
                var error = await Instance.StandardError.ReadToEndAsync();

                try
                {
                    File.WriteAllText(Path.Combine(Global.NetchDir, $"logging\\{Name}.log"), $"{output}\r\n{error}");
                }
                catch (Exception e)
                {
                    Global.Logger.Warning($"写入 {Name} 日志错误：\n" + e.Message);
                }

                if (output.IsNullOrWhiteSpace())
                    if (!error.IsNullOrWhiteSpace())
                    {
                        error = error.Trim();
                        var errorFirst = error.Substring(0, error.IndexOf('\n')).Trim();
                        return (errorFirst.SplitTrimEntries(':').Last(), null, null);
                    }

                foreach (var line in output.Split('\n'))
                {
                    var str = line.SplitTrimEntries(':');
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
                    }
                }

                if (bindingTest == "Fail")
                    result = "Fail";

                return (result, localEnd, publicEnd);
            }
            catch (Exception e)
            {
                Global.Logger.Error($"{Name} 控制器出错:\n" + e);
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
    }
}