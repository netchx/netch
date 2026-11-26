#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Enums;
using Netch.JsonConverter;
using Netch.Models;
using Netch.Servers;
using Netch.Utils;

namespace Tests;

[TestClass]
public class ExternalCli
{
    private Setting _savedSettings = null!;

    [TestInitialize]
    public void SaveSettings()
    {
        _savedSettings = Netch.Global.Settings;
        Netch.Global.Settings = new Setting();
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        DelayTestHelper.Cancel();
        Netch.Global.Settings = _savedSettings;
    }

    [TestMethod]
    public void ExternalCliDefaultsNormalizeValidateRoundTripAndRedact()
    {
        var direct = new ExternalCliServer();
        Assert.AreEqual("127.0.0.1", direct.Address);
        Assert.AreEqual(1080, direct.Port);

        var util = new ExternalCliUtil();
        var server = (ExternalCliServer)ServerEditorHelper.CreateDefault(util);
        Assert.AreEqual("127.0.0.1", server.Address);
        Assert.AreEqual(1080, server.Port);
        Assert.IsFalse(server.SupportsTcpPreScreen());

        Assert.AreEqual("Enter the CLI executable path.", ServerEditorHelper.ValidateForSave(server, util));

        server.Address = "";
        server.Port = 0;
        server.ProtoExtra.CliExecutable = "  \"C:\\Tools\\my-cli.exe\"  ";
        Assert.IsNotNull(ServerEditorHelper.ValidateForSave(server, util));

        server.Port = 2080;
        server.ProtoExtra.CliArguments = "  -s 1.2.3.4 -p 2211 -k secret-arg  ";
        ServerEditorHelper.PrepareForSave(server);
        Assert.AreEqual("127.0.0.1", server.Address);
        Assert.AreEqual("C:\\Tools\\my-cli.exe", server.ProtoExtra.CliExecutable);
        Assert.AreEqual("-s 1.2.3.4 -p 2211 -k secret-arg", server.ProtoExtra.CliArguments);
        Assert.IsNull(ServerEditorHelper.ValidateForSave(server, util));

        var masked = server.MaskedData();
        Assert.IsTrue(masked.Contains("my-cli.exe", StringComparison.Ordinal));
        Assert.IsFalse(masked.Contains("secret-arg", StringComparison.Ordinal));

        var jsonOptions = Netch.Global.NewCustomJsonSerializerOptions();
        jsonOptions.Converters.Add(new ServerConverterWithTypeDiscriminator());
        var roundTripped = JsonSerializer.Deserialize<Server>(JsonSerializer.Serialize<Server>(server, jsonOptions), jsonOptions);
        Assert.IsInstanceOfType<ExternalCliServer>(roundTripped);
        var restored = (ExternalCliServer)roundTripped;
        Assert.AreEqual(2080, restored.Port);
        Assert.AreEqual("C:\\Tools\\my-cli.exe", restored.ProtoExtra.CliExecutable);
        Assert.AreEqual("-s 1.2.3.4 -p 2211 -k secret-arg", restored.ProtoExtra.CliArguments);
    }

    [TestMethod]
    public void ExternalCliShareLinksAreBlockedAndCustomCoreIdIsStripped()
    {
        var cli = new ExternalCliServer
        {
            Remarks = "My CLI",
            Address = "127.0.0.1",
            Port = 1080,
            ProtoExtra = { CliExecutable = @"C:\Windows\System32\cmd.exe", CliArguments = "/c echo hi" }
        };

        Assert.Throws<NotSupportedException>(() => ShareLink.GetShareLink(cli));
        Assert.Throws<NotSupportedException>(() => ShareLink.GetNetchLink(cli));

        var crafted = "Netch://" + ShareLink.URLSafeBase64Encode(JsonSerializer.Serialize(new
        {
            ConfigType = (int)EConfigType.ExternalCli,
            Remarks = "Evil",
            Address = "127.0.0.1",
            Port = 1080,
            ProtoExtra = new { CliExecutable = "cmd.exe", CliArguments = "/c calc.exe" }
        }));
        Assert.AreEqual(0, ShareLink.ParseText(crafted, out var skipped).Count);
        Assert.AreEqual(1, skipped);

        var ss = new ShadowsocksServer
        {
            Remarks = "SS",
            Address = "1.2.3.4",
            Port = 443,
            Password = "pw",
            ProtoExtra = { SsMethod = "aes-128-gcm", CustomCoreId = "local-core-1" }
        };
        var link = ShareLink.GetNetchLink(ss);
        var imported = ShareLink.ParseText(link).Single();
        Assert.IsNull(imported.ProtoExtra.CustomCoreId);
    }

    [TestMethod]
    public void CoreSelectorResolvesExternalCliAndCustomCoresAcrossAllModes()
    {
        var cmdExe = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        var cliNode = new ExternalCliServer
        {
            Address = "127.0.0.1",
            Port = 10801,
            ProtoExtra = { CliExecutable = cmdExe, CliArguments = "/c exit 0" }
        };

        Assert.AreEqual(CoreSelector.CoreKind.ExternalCli, CoreSelector.SelectCore(cliNode));
        var cliController = CoreSelector.CreateController(cliNode);
        Assert.IsInstanceOfType<ExternalCliController>(cliController);
        Assert.AreEqual((ushort)10801, cliController.Socks5LocalPort);

        var missingNode = new ExternalCliServer
        {
            Address = "127.0.0.1",
            Port = 10801,
            ProtoExtra = { CliExecutable = @"C:\__missing__\no-such-cli.exe" }
        };
        Assert.Throws<Netch.Models.MessageException>(() => CoreSelector.CreateController(missingNode));

        var cliCore = new CustomCore
        {
            Id = "ss-cli",
            Name = "Golink SS",
            Protocol = EConfigType.Shadowsocks,
            ExecutablePath = cmdExe,
            Mode = CustomCoreMode.CliArguments,
            ArgumentsTemplate = "-s {address} -p {port}"
        };
        var mihomoCore = new CustomCore
        {
            Id = "ss-mihomo",
            Name = "Mod Mihomo",
            Protocol = EConfigType.Shadowsocks,
            ExecutablePath = cmdExe,
            Mode = CustomCoreMode.MihomoConfig
        };
        var singboxCore = new CustomCore
        {
            Id = "ss-singbox",
            Name = "Mod sing-box",
            Protocol = EConfigType.Shadowsocks,
            ExecutablePath = cmdExe,
            Mode = CustomCoreMode.SingboxConfig
        };
        var xrayCore = new CustomCore
        {
            Id = "ss-xray",
            Name = "Mod Xray",
            Protocol = EConfigType.Shadowsocks,
            ExecutablePath = cmdExe,
            Mode = CustomCoreMode.XrayConfig
        };
        Netch.Global.Settings.CustomCores = [cliCore, mihomoCore, singboxCore, xrayCore];

        var ss = new ShadowsocksServer
        {
            Address = "1.2.3.4",
            Port = 443,
            Password = "pw",
            ProtoExtra = { SsMethod = "aes-128-gcm", CustomCoreId = cliCore.Id }
        };
        Assert.IsInstanceOfType<ExternalCliController>(CoreSelector.CreateController(ss));

        ss.ProtoExtra.CustomCoreId = mihomoCore.Id;
        Assert.IsInstanceOfType<MihomoController>(CoreSelector.CreateController(ss));

        ss.ProtoExtra.CustomCoreId = singboxCore.Id;
        Assert.IsInstanceOfType<SingboxController>(CoreSelector.CreateController(ss));

        ss.ProtoExtra.CustomCoreId = xrayCore.Id;
        Assert.IsInstanceOfType<XrayController>(CoreSelector.CreateController(ss));
    }

    [TestMethod]
    public void CustomCorePriorityChainValidationSaveDeleteAndArgumentsWorkCorrectly()
    {
        var invalid = new CustomCore { Name = "", Protocol = EConfigType.Shadowsocks, ExecutablePath = "a.exe", Mode = CustomCoreMode.CliArguments, ArgumentsTemplate = "" };
        Assert.AreEqual("Enter a custom core name.", CustomCoreHelper.Validate(invalid));
        invalid.Name = "Golink";
        invalid.Protocol = EConfigType.ExternalCli;
        Assert.AreEqual("Choose a protocol for the custom core.", CustomCoreHelper.Validate(invalid));
        invalid.Protocol = EConfigType.Shadowsocks;
        invalid.ExecutablePath = "  ";
        Assert.AreEqual("Enter the CLI executable path.", CustomCoreHelper.Validate(invalid));
        invalid.ExecutablePath = "a.exe";
        Assert.AreEqual("Enter the CLI arguments template.", CustomCoreHelper.Validate(invalid));
        invalid.Mode = CustomCoreMode.MihomoConfig;
        Assert.IsNull(CustomCoreHelper.Validate(invalid));

        var coreA = new CustomCore
        {
            Id = "core-a",
            Name = "Core A",
            Protocol = EConfigType.Shadowsocks,
            ExecutablePath = "\"C:\\Tools\\a.exe\"",
            Mode = CustomCoreMode.CliArguments,
            ArgumentsTemplate = "-s {address} -p {port} -b {local_addr} -l {local_port} -m {method} -k {password} -u \"{username}\"",
            IsProtocolDefault = true
        };
        var coreB = new CustomCore
        {
            Id = "core-b",
            Name = "Core B",
            Protocol = EConfigType.Shadowsocks,
            ExecutablePath = "C:\\Tools\\b.exe",
            Mode = CustomCoreMode.CliArguments,
            ArgumentsTemplate = "-s {address}",
            IsProtocolDefault = true
        };
        CustomCoreHelper.Save(coreA);
        CustomCoreHelper.Save(coreB);
        Assert.AreEqual("C:\\Tools\\a.exe", coreA.ExecutablePath);
        Assert.IsFalse(coreA.IsProtocolDefault);
        Assert.IsTrue(coreB.IsProtocolDefault);

        var sub = new Subscription { Remark = "Airport", Link = "https://example.com/sub", CustomCoreId = coreA.Id };
        Netch.Global.Settings.Subscription.Add(sub);

        var subSs = new ShadowsocksServer { Group = "Airport", IsSub = true, Address = "1.1.1.1", Port = 8388, Password = "p w\"x", Username = "u\"1", ProtoExtra = { SsMethod = "rc4-md5" } };
        var subVless = new VLESSServer { Group = "Airport", IsSub = true, Address = "2.2.2.2", Port = 443, Password = "uuid" };
        var manualSs = new ShadowsocksServer { Group = "NONE", IsSub = false, Address = "3.3.3.3", Port = 8388, Password = "pw", ProtoExtra = { SsMethod = "rc4-md5" } };
        Netch.Global.Settings.Server.AddRange([subSs, subVless, manualSs]);

        // Protocol default applies to manual SS; subscription SS uses sub override; subscription VLESS ignores SS core.
        Assert.AreEqual(coreB.Id, CustomCoreHelper.ResolveForServer(manualSs)?.Id);
        Assert.AreEqual(coreA.Id, CustomCoreHelper.ResolveForServer(subSs)?.Id);
        Assert.IsNull(CustomCoreHelper.ResolveForServer(subVless));

        // Node override can force built-in or another core.
        subSs.ProtoExtra.CustomCoreId = CustomCoreHelper.BuiltInCoreId;
        Assert.IsNull(CustomCoreHelper.ResolveForServer(subSs));
        subSs.ProtoExtra.CustomCoreId = coreB.Id;
        Assert.AreEqual(coreB.Id, CustomCoreHelper.ResolveForServer(subSs)?.Id);

        // Subscription InheritIds preserves per-node CustomCoreId.
        var refreshedSubSs = new ShadowsocksServer { Group = "Airport", IsSub = true, Address = "1.1.1.1", Port = 8388, Password = "p w\"x", ProtoExtra = { SsMethod = "rc4-md5" } };
        SubscriptionUtil.InheritIds([subSs], [refreshedSubSs]);
        Assert.AreEqual(coreB.Id, refreshedSubSs.ProtoExtra.CustomCoreId);

        // RenderArguments quotes unquoted spaces/quotes while avoiding double quotes inside "...".
        var rendered = CustomCoreHelper.RenderArguments(coreA.ArgumentsTemplate, subSs, "10.0.0.1", "127.0.0.1", 2801);
        Assert.AreEqual("-s 10.0.0.1 -p 8388 -b 127.0.0.1 -l 2801 -m rc4-md5 -k \"p w\\\"x\" -u \"u\\\"1\"", rendered);

        // Preserve trailing backslashes inside quotes when not followed by '"' and ignore template '\"' when tracking quotes.
        subSs.Username = "user\\";
        subSs.Password = "pw\\";
        var backslashRendered = CustomCoreHelper.RenderArguments("-a \"{username}:{password}\" -t \\\"lit\\\" -k {password}", subSs, "10.0.0.1", "127.0.0.1", 2801);
        Assert.AreEqual("-a \"user\\:pw\\\\\" -t \\\"lit\\\" -k pw\\", backslashRendered);

        var mixedQuotesRendered = CustomCoreHelper.RenderArguments("-t \\\"foo\\\" -p {password}", new ShadowsocksServer { Password = "a b" }, "10.0.0.1", "127.0.0.1", 2801);
        Assert.AreEqual("-t \\\"foo\\\" -p \"a b\"", mixedQuotesRendered);

        // Changing core protocol on save clears stale node overrides of the previous protocol, while keeping matching ones.
        var tempCore = new CustomCore
        {
            Id = "core-reproto",
            Name = "Reproto",
            Protocol = EConfigType.Shadowsocks,
            ExecutablePath = "C:\\Tools\\reproto.exe",
            Mode = CustomCoreMode.MihomoConfig
        };
        CustomCoreHelper.Save(tempCore);
        manualSs.ProtoExtra.CustomCoreId = tempCore.Id;
        var vmessNode = new VMessServer { Address = "4.4.4.4", Port = 443, Password = "uuid", ProtoExtra = { CustomCoreId = tempCore.Id } };
        Netch.Global.Settings.Server.Add(vmessNode);
        var updatedCore = new CustomCore
        {
            Id = tempCore.Id,
            Name = tempCore.Name,
            Protocol = EConfigType.VMess,
            ExecutablePath = tempCore.ExecutablePath,
            Mode = tempCore.Mode
        };
        CustomCoreHelper.Save(updatedCore);
        Assert.IsNull(manualSs.ProtoExtra.CustomCoreId);
        Assert.AreEqual(tempCore.Id, vmessNode.ProtoExtra.CustomCoreId);
        CustomCoreHelper.Delete(tempCore.Id);

        // NormalizeAll strips invalid/ExternalCli protocol cores, replaces BuiltInCoreId collisions, and resets invalid Mode.
        var badSettings = new Setting
        {
            CustomCores =
            [
                new CustomCore { Id = "1", Name = "Ext", Protocol = EConfigType.ExternalCli, ExecutablePath = "x.exe" },
                new CustomCore { Id = "2", Name = "BadProto", Protocol = (EConfigType)9999, ExecutablePath = "y.exe" },
                new CustomCore { Id = CustomCoreHelper.BuiltInCoreId, Name = "ShadowBuiltin", Protocol = EConfigType.Shadowsocks, ExecutablePath = "  \"z.exe\"  ", Mode = (CustomCoreMode)99 }
            ]
        };
        CustomCoreHelper.NormalizeAll(badSettings);
        Assert.AreEqual(1, badSettings.CustomCores.Count);
        Assert.AreNotEqual(CustomCoreHelper.BuiltInCoreId, badSettings.CustomCores[0].Id);
        Assert.AreEqual("z.exe", badSettings.CustomCores[0].ExecutablePath);
        Assert.AreEqual(CustomCoreMode.CliArguments, badSettings.CustomCores[0].Mode);

        // Unsupported Xray protocol throws MessageException.
        Assert.Throws<Netch.Models.MessageException>(() => V2rayConfigUtils.GenerateOutbound(new ShadowsocksRServer
        {
            Address = "1.2.3.4",
            Port = 8388,
            Password = "pw",
            EncryptMethod = "rc4-md5",
            Protocol = "origin",
            OBFS = "plain"
        }));

        // IsBuiltInCoreId is case-insensitive and Save refuses BuiltInCoreId collisions.
        Assert.IsTrue(CustomCoreHelper.IsBuiltInCoreId(" __BUILTIN__ "));
        var builtinShadow = new CustomCore
        {
            Id = CustomCoreHelper.BuiltInCoreId.ToUpperInvariant(),
            Name = "Collision",
            Protocol = EConfigType.Shadowsocks,
            ExecutablePath = "C:\\Tools\\c.exe",
            Mode = CustomCoreMode.MihomoConfig
        };
        CustomCoreHelper.Save(builtinShadow);
        Assert.AreNotEqual(CustomCoreHelper.BuiltInCoreId, builtinShadow.Id, StringComparer.OrdinalIgnoreCase);
        CustomCoreHelper.Delete(builtinShadow.Id);

        // NormalizeArgumentsInput & PrepareForSave flatten accidental line breaks into single-line arguments.
        Assert.AreEqual("-a 1 -b 2 -c 3", CustomCoreHelper.NormalizeArgumentsInput("  -a 1\r\n-b 2\n-c 3  "));
        var nlCli = new ExternalCliServer
        {
            ProtoExtra = { CliExecutable = "cmd.exe", CliArguments = " -s 1.2.3.4\r\n-p 443\n-l 1080 " }
        };
        ServerEditorHelper.PrepareForSave(nlCli);
        Assert.AreEqual("-s 1.2.3.4 -p 443 -l 1080", nlCli.ProtoExtra.CliArguments);

        // Delete cleans up both server and subscription references.
        CustomCoreHelper.Delete(coreB.Id);
        Assert.IsNull(subSs.ProtoExtra.CustomCoreId);
        CustomCoreHelper.Delete(coreA.Id);
        Assert.IsNull(sub.CustomCoreId);
        Assert.AreEqual(0, CustomCoreHelper.GetAll().Count);
    }

    [TestMethod]
    public void ImportCustomCoreMatchesOrderByProtocolCountAndAppliesSelectionsSafely()
    {
        var ssCore = new CustomCore
        {
            Id = "ss-import-core",
            Name = "Golink SS",
            Protocol = EConfigType.Shadowsocks,
            ExecutablePath = "C:\\Tools\\ss.exe",
            Mode = CustomCoreMode.CliArguments,
            ArgumentsTemplate = "-s {address}"
        };
        var vmessCore = new CustomCore
        {
            Id = "vmess-import-core",
            Name = "Custom VMess",
            Protocol = EConfigType.VMess,
            ExecutablePath = "C:\\Tools\\vmess.exe",
            Mode = CustomCoreMode.MihomoConfig
        };
        CustomCoreHelper.Save(ssCore);
        CustomCoreHelper.Save(vmessCore);

        var imported = new Server[]
        {
            new VMessServer { Address = "1.1.1.1", Port = 443, Password = "uuid" },
            new ShadowsocksServer { Address = "2.2.2.2", Port = 8388, Password = "pw", ProtoExtra = { SsMethod = "aes-128-gcm" } },
            new VMessServer { Address = "3.3.3.3", Port = 443, Password = "uuid" },
            new VMessServer { Address = "4.4.4.4", Port = 443, Password = "uuid" },
            new TrojanServer { Address = "5.5.5.5", Port = 443, Password = "pw" },
            new ExternalCliServer { ProtoExtra = { CliExecutable = "cli.exe" } }
        };

        var matches = CustomCoreHelper.GetImportCustomCoreMatches(imported);
        Assert.AreEqual(2, matches.Count);
        Assert.AreEqual(EConfigType.VMess, matches[0].Protocol);
        Assert.AreEqual(3, matches[0].Servers.Count);
        Assert.AreEqual(EConfigType.Shadowsocks, matches[1].Protocol);
        Assert.AreEqual(1, matches[1].Servers.Count);

        var selections = new Dictionary<EConfigType, string?>
        {
            [EConfigType.VMess] = ssCore.Id, // Mismatched protocol should be ignored safely
            [EConfigType.Shadowsocks] = ssCore.Id
        };
        Assert.AreEqual(1, CustomCoreHelper.ApplyCustomCoreSelections(imported, selections));
        Assert.IsNull(imported[0].ProtoExtra.CustomCoreId);
        Assert.AreEqual(ssCore.Id, imported[1].ProtoExtra.CustomCoreId);

        // Apply built-in override to VMess and reset Shadowsocks back to Follow default (null)
        selections[EConfigType.VMess] = " __builtin__ ";
        selections[EConfigType.Shadowsocks] = null;
        Assert.AreEqual(3, CustomCoreHelper.ApplyCustomCoreSelections(imported, selections));
        Assert.AreEqual(CustomCoreHelper.BuiltInCoreId, imported[0].ProtoExtra.CustomCoreId);
        Assert.AreEqual(CustomCoreHelper.BuiltInCoreId, imported[2].ProtoExtra.CustomCoreId);
        Assert.AreEqual(CustomCoreHelper.BuiltInCoreId, imported[3].ProtoExtra.CustomCoreId);
        Assert.IsNull(imported[1].ProtoExtra.CustomCoreId);
        Assert.IsNull(imported[4].ProtoExtra.CustomCoreId);
        Assert.IsNull(imported[5].ProtoExtra.CustomCoreId);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task DelayTestsSkipExternalCliAndFallbackToTcpForProtocolCliNodes()
    {
        var cliNode = new ExternalCliServer
        {
            Address = "127.0.0.1",
            Port = 1080,
            ProtoExtra = { CliExecutable = Path.Combine(Environment.SystemDirectory, "cmd.exe") }
        };
        Assert.AreEqual(DelayStatus.Untested, await cliNode.PingAsync());

        Netch.Global.Settings.DelayTestMode = DelayTestMode.UrlTest;
        await DelayTestHelper.PerformTestAsync([cliNode], waitFinish: true);
        Assert.AreEqual(DelayStatus.Untested, cliNode.Delay);

        var cmdExe = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        var ssCliCore = new CustomCore
        {
            Id = "ss-cli",
            Name = "CLI SS",
            Protocol = EConfigType.Shadowsocks,
            ExecutablePath = cmdExe,
            Mode = CustomCoreMode.CliArguments,
            ArgumentsTemplate = "-s {address} -p {port} -l {local_port}"
        };
        Netch.Global.Settings.CustomCores.Add(ssCliCore);

        using var openListener = new TcpListener(IPAddress.Loopback, 0);
        openListener.Start();
        var openPort = ((IPEndPoint)openListener.LocalEndpoint).Port;

        var ssCliNode = new ShadowsocksServer
        {
            Address = "127.0.0.1",
            Port = openPort,
            Password = "pw",
            ProtoExtra = { SsMethod = "aes-128-gcm", CustomCoreId = ssCliCore.Id }
        };

        Netch.Global.Settings.DelayTestMode = DelayTestMode.Hybrid;
        await DelayTestHelper.PerformTestAsync([cliNode, ssCliNode], waitFinish: true);
        Assert.AreEqual(DelayStatus.Untested, cliNode.Delay);
        Assert.IsTrue(DelayStatus.IsValid(ssCliNode.Delay));
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task ExternalCliControllerFailsFastOnImmediateProcessExit()
    {
        var server = new ExternalCliServer
        {
            Address = "127.0.0.1",
            Port = 43210,
            ProtoExtra =
            {
                CliExecutable = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                CliArguments = "/c exit 7"
            }
        };

        var controller = new ExternalCliController(server);
        var ex = await Assert.ThrowsAsync<Netch.Models.MessageException>(() => controller.StartAsync(server));
        StringAssert.Contains(ex.Message, "7");
        await controller.StopAsync();
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task ExternalCliControllerStartsPollsSocks5AndStopsProcessCleanly()
    {
        var tempDir = Directory.CreateTempSubdirectory("netch-extcli-test-");
        try
        {
            var pingSrc = Path.Combine(Environment.SystemDirectory, "PING.EXE");
            var pingCopy = Path.Combine(tempDir.FullName, "dummy-cli.exe");
            File.Copy(pingSrc, pingCopy, overwrite: true);

            using var mockSocks = new TcpListener(IPAddress.Loopback, 0);
            mockSocks.Start();
            var socksPort = ((IPEndPoint)mockSocks.LocalEndpoint).Port;

            using var acceptCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var acceptTask = Task.Run(async () =>
            {
                using var client = await mockSocks.AcceptTcpClientAsync(acceptCts.Token);
                var stream = client.GetStream();
                var greeting = new byte[3];
                await stream.ReadExactlyAsync(greeting, acceptCts.Token);
                await stream.WriteAsync(new byte[] { 5, 0 }, acceptCts.Token);
            }, acceptCts.Token);

            // Standalone mode test
            var standaloneServer = new ExternalCliServer
            {
                Address = "127.0.0.1",
                Port = socksPort,
                ProtoExtra =
                {
                    CliExecutable = pingCopy,
                    CliArguments = "-n 30 127.0.0.1"
                }
            };

            var standaloneController = new ExternalCliController(standaloneServer);
            Assert.AreEqual(tempDir.FullName, standaloneController.Instance.StartInfo.WorkingDirectory);

            try
            {
                var started = await standaloneController.StartAsync(standaloneServer);
                await acceptTask;
                Assert.AreEqual("127.0.0.1", started.Address);
                Assert.AreEqual(socksPort, started.Port);
                Assert.IsFalse(standaloneController.Instance.HasExited);
            }
            finally
            {
                await standaloneController.StopAsync();
            }

            // Protocol CLI template mode test
            using var mockSocks2 = new TcpListener(IPAddress.Loopback, 0);
            mockSocks2.Start();
            var socksPort2 = ((IPEndPoint)mockSocks2.LocalEndpoint).Port;
            using var acceptCts2 = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var acceptTask2 = Task.Run(async () =>
            {
                using var client = await mockSocks2.AcceptTcpClientAsync(acceptCts2.Token);
                var stream = client.GetStream();
                var greeting = new byte[3];
                await stream.ReadExactlyAsync(greeting, acceptCts2.Token);
                await stream.WriteAsync(new byte[] { 5, 0 }, acceptCts2.Token);
            }, acceptCts2.Token);

            var customCore = new CustomCore
            {
                Id = "golink-test",
                Name = "Golink Test",
                Protocol = EConfigType.Shadowsocks,
                ExecutablePath = pingCopy,
                Mode = CustomCoreMode.CliArguments,
                ArgumentsTemplate = "-n 30 {local_addr}"
            };
            var ssNode = new ShadowsocksServer
            {
                Address = "127.0.0.1",
                Port = 6690,
                Password = "pw",
                ProtoExtra = { SsMethod = "rc4-md5" }
            };
            var protoController = new ExternalCliController(customCore)
            {
                Socks5LocalPort = (ushort)socksPort2,
                LocalAddress = "127.0.0.1"
            };
            try
            {
                var started2 = await protoController.StartAsync(ssNode);
                await acceptTask2;
                Assert.AreEqual("127.0.0.1", started2.Address);
                Assert.AreEqual(socksPort2, started2.Port);
                Assert.AreEqual("127.0.0.1", started2.RemoteHostname);
            }
            finally
            {
                await protoController.StopAsync();
            }
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
