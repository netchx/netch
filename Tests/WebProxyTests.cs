#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Controllers;
using Netch.Enums;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.WebProxyMode;
using Netch.Servers;
using Netch.Services;
using Netch.Utils;

namespace Tests;

[TestClass]
public class WebProxyTests
{
    private sealed class FakeSystemProxyService : ISystemProxyService
    {
        public SystemProxySnapshot Current { get; set; } = new(SystemProxySnapshot.ProxyTypeDirect, null, null, null);
        public int SetGlobalCalls { get; private set; }
        public int SetPacCalls { get; private set; }
        public int SetDirectCalls { get; private set; }
        public int RestoreCalls { get; private set; }

        public SystemProxySnapshot Query() => Current;

        public void SetGlobal(string proxyServer, string? bypass)
        {
            SetGlobalCalls++;
            Current = new SystemProxySnapshot(
                SystemProxySnapshot.ProxyTypeProxy | SystemProxySnapshot.ProxyTypeDirect,
                proxyServer,
                bypass ?? string.Empty,
                string.Empty);
        }

        public void SetPac(string pacUrl)
        {
            SetPacCalls++;
            Current = new SystemProxySnapshot(
                SystemProxySnapshot.ProxyTypeAutoProxyUrl | SystemProxySnapshot.ProxyTypeDirect,
                Current.ProxyServer,
                Current.ProxyBypass,
                pacUrl);
        }

        public void SetDirect()
        {
            SetDirectCalls++;
            Current = new SystemProxySnapshot(SystemProxySnapshot.ProxyTypeDirect, string.Empty, string.Empty, string.Empty);
        }

        public void Restore(SystemProxySnapshot snapshot)
        {
            RestoreCalls++;
            Current = snapshot;
        }
    }

    [TestMethod]
    public void WebProxyModeJsonRoundTrip()
    {
        var temp = Directory.CreateTempSubdirectory("netch-webproxy-json-");
        try
        {
            var presets = new[]
            {
                new WebProxyMode
                {
                    FullName = Path.Combine(temp.FullName, "pac-proxylist.json"),
                    Strategy = WebProxyStrategy.Pac,
                    PacPreset = PacRulePreset.ProxyList,
                    BypassLan = true,
                    Handle = ["example.org", "full:api.test.io"],
                    Bypass = ["internal.local"],
                    PacRuleUrl = "https://example.com/proxy-list.txt",
                    Remark = { ["en"] = "PAC Proxy", ["zh-CN"] = "PAC 代理" }
                },
                new WebProxyMode
                {
                    FullName = Path.Combine(temp.FullName, "pac-custom.json"),
                    Strategy = WebProxyStrategy.Pac,
                    PacPreset = PacRulePreset.CustomRulesOnly,
                    DefaultProxy = true,
                    BypassLan = false,
                    Bypass = ["cn.example.com"],
                    Remark = { ["en"] = "Custom Only" }
                },
                new WebProxyMode
                {
                    FullName = Path.Combine(temp.FullName, "pac-script.json"),
                    Strategy = WebProxyStrategy.Pac,
                    PacPreset = PacRulePreset.CustomScript,
                    PacScriptPath = "custom.pac",
                    Remark = { ["en"] = "Script" }
                },
                new WebProxyMode
                {
                    FullName = Path.Combine(temp.FullName, "global.json"),
                    Strategy = WebProxyStrategy.Global,
                    BypassLan = false,
                    Bypass = ["*.corp.internal", "10.*"],
                    Remark = { ["en"] = "Global" }
                },
                new WebProxyMode
                {
                    FullName = Path.Combine(temp.FullName, "manual.json"),
                    Strategy = WebProxyStrategy.Manual,
                    Remark = { ["en"] = "Manual" }
                }
            };

            foreach (var source in presets)
            {
                source.WriteFile();
                var loaded = ModeHelper.LoadMode(source.FullName) as WebProxyMode;
                Assert.IsNotNull(loaded);
                Assert.AreEqual(ModeType.WebProxyMode, loaded.Type);
                Assert.AreEqual(source.Strategy, loaded.Strategy);
                Assert.AreEqual(source.PacPreset, loaded.PacPreset);
                Assert.AreEqual(source.DefaultProxy, loaded.DefaultProxy);
                Assert.AreEqual(source.BypassLan, loaded.BypassLan);
                CollectionAssert.AreEqual(source.Handle, loaded.Handle);
                CollectionAssert.AreEqual(source.Bypass, loaded.Bypass);
                Assert.AreEqual(source.PacScriptPath, loaded.PacScriptPath);
                Assert.AreEqual(source.PacRuleUrl, loaded.PacRuleUrl);
            }
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void PacScriptBuilderParsesLoyalsoldierMetaCubeXAndAutoProxy()
    {
        const string mixedRules = """
            # comment
            // comment 2
            google.com
            full:api.github.com
            regexp:^ewcdn[0-9]+\.nowe\.com$
            +.openai.com
            *.anthropic.com
            .claude.ai
            """;

        var parsed = PacScriptBuilder.ParseRuleText(mixedRules);
        CollectionAssert.Contains(parsed.SuffixDomains.ToList(), "google.com");
        CollectionAssert.Contains(parsed.SuffixDomains.ToList(), "openai.com");
        CollectionAssert.Contains(parsed.SuffixDomains.ToList(), "anthropic.com");
        CollectionAssert.Contains(parsed.SuffixDomains.ToList(), "claude.ai");
        CollectionAssert.Contains(parsed.ExactDomains.ToList(), "api.github.com");
        CollectionAssert.Contains(parsed.RegexPatterns, @"^ewcdn[0-9]+\.nowe\.com$");

        const string autoProxyRaw = """
            [AutoProxy 0.2.9]
            ! Checksum: dummy
            ||youtube.com
            @@||bilibili.com
            |https://strict.example.org/v1
            """;
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(autoProxyRaw));
        var decoded = PacScriptBuilder.ParseRuleText(base64);
        CollectionAssert.Contains(decoded.SuffixDomains.ToList(), "youtube.com");
        CollectionAssert.Contains(decoded.BypassSuffixDomains.ToList(), "bilibili.com");
        CollectionAssert.Contains(decoded.ExactDomains.ToList(), "strict.example.org");
    }

    [TestMethod]
    public void PacScriptBuilderEnforcesPriorityAndNoDnsResolve()
    {
        var temp = Directory.CreateTempSubdirectory("netch-pac-builder-");
        try
        {
            var binPac = Path.Combine(temp.FullName, "bin", "pac");
            var dataPac = Path.Combine(temp.FullName, "data", "pac");
            Directory.CreateDirectory(binPac);
            Directory.CreateDirectory(dataPac);

            File.WriteAllText(Path.Combine(binPac, "proxy-list.txt"), "google.com\nfull:exact.example.net\n");
            File.WriteAllText(Path.Combine(binPac, "direct-list.txt"), "baidu.com\n");

            var mode = new WebProxyMode
            {
                Strategy = WebProxyStrategy.Pac,
                PacPreset = PacRulePreset.ProxyList,
                BypassLan = true,
                Handle = ["custom-proxy.dev"],
                Bypass = ["custom-direct.google.com"]
            };

            var pacJs = PacScriptBuilder.BuildPacScript(mode, "PROXY 127.0.0.1:2801;", temp.FullName);
            Assert.IsFalse(pacJs.Contains("dnsResolve", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(pacJs.Contains(".endsWith(", StringComparison.Ordinal));
            StringAssert.Contains(pacJs, "lastIndexOf(\":\") === idx");
            var isLocalIdx = pacJs.IndexOf("function isLocalHost", StringComparison.Ordinal);
            var isLocalBody = pacJs[isLocalIdx..];
            var ipv6CheckIdx = isLocalBody.IndexOf("host.indexOf(\":\") >= 0", StringComparison.Ordinal);
            var plainHostIdx = isLocalBody.IndexOf("isPlainHostName(host)", StringComparison.Ordinal);
            Assert.IsTrue(ipv6CheckIdx > 0 && plainHostIdx > ipv6CheckIdx);
            StringAssert.Contains(pacJs, "var proxy = \"PROXY 127.0.0.1:2801;\";");
            StringAssert.Contains(pacJs, "var invertBase = false;");
            StringAssert.Contains(pacJs, "var defaultResult = direct;");
            StringAssert.Contains(pacJs, "\"custom-direct.google.com\":1");
            StringAssert.Contains(pacJs, "\"custom-proxy.dev\":1");
            StringAssert.Contains(pacJs, "\"google.com\":1");
            StringAssert.Contains(pacJs, "\"exact.example.net\":1");

            // Check priority order inside FindProxyForURL: isLocalHost -> modeBypass -> modeHandle -> base.
            var fnIdx = pacJs.IndexOf("function FindProxyForURL", StringComparison.Ordinal);
            var body = pacJs[fnIdx..];
            var lanIdx = body.IndexOf("isLocalHost", StringComparison.Ordinal);
            var bypassIdx = body.IndexOf("modeBypass", StringComparison.Ordinal);
            var handleIdx = body.IndexOf("modeHandle", StringComparison.Ordinal);
            var baseIdx = body.IndexOf("baseExact", StringComparison.Ordinal);
            Assert.IsTrue(lanIdx > 0 && bypassIdx > lanIdx && handleIdx > bypassIdx && baseIdx > handleIdx);

            // BypassChina inverts base list and defaults to proxy.
            var bypassChina = new WebProxyMode
            {
                Strategy = WebProxyStrategy.Pac,
                PacPreset = PacRulePreset.BypassChina
            };
            var chinaJs = PacScriptBuilder.BuildPacScript(bypassChina, "__PROXY__", temp.FullName);
            StringAssert.Contains(chinaJs, "var invertBase = true;");
            StringAssert.Contains(chinaJs, "var defaultResult = proxy;");
            StringAssert.Contains(chinaJs, "\"baidu.com\":1");

            // CustomScript supports __PROXY__ and var proxy = '...'
            var scriptPath = Path.Combine(temp.FullName, "custom.pac");
            File.WriteAllText(scriptPath, "var proxy = '__PROXY__';\nfunction FindProxyForURL(url, host) { return proxy; }");
            var customScriptMode = new WebProxyMode
            {
                Strategy = WebProxyStrategy.Pac,
                PacPreset = PacRulePreset.CustomScript,
                PacScriptPath = "custom.pac"
            };
            PacScriptBuilder.ValidateMode(customScriptMode, temp.FullName);
            var builtCustom = PacScriptBuilder.BuildPacScript(customScriptMode, "PROXY 127.0.0.1:2801;", temp.FullName);
            StringAssert.Contains(builtCustom, "var proxy = 'PROXY 127.0.0.1:2801;';");

            var hardcodedScript = Path.Combine(temp.FullName, "legacy.pac");
            File.WriteAllText(hardcodedScript, "var proxy = \"SOCKS5 127.0.0.1:1080\";\nfunction FindProxyForURL(url, host) { return proxy; }");
            customScriptMode.PacScriptPath = hardcodedScript;
            var builtLegacy = PacScriptBuilder.BuildPacScript(customScriptMode, "PROXY 127.0.0.1:2801;", temp.FullName);
            StringAssert.Contains(builtLegacy, "var proxy = \"PROXY 127.0.0.1:2801;\";");

            Assert.IsTrue(PacScriptBuilder.ValidateRules(["example.com", "*.sub.net", "full:a.b", "keyword:video", "regexp:^foo\\d+$", "<local>"], out var validErrs));
            Assert.AreEqual(0, validErrs.Count);
            Assert.IsFalse(PacScriptBuilder.ValidateRules(["https://example.com/path", "regexp:[unclosed", "a;b", "@@foo.com", "keyword:"], out var invalid));
            Assert.AreEqual(5, invalid.Count);

            // Strategy-scoped validation ignores inactive PacRuleUrl / CustomScript fields.
            PacScriptBuilder.ValidateMode(new WebProxyMode
            {
                Strategy = WebProxyStrategy.Global,
                PacPreset = PacRulePreset.CustomScript,
                PacScriptPath = "missing.pac",
                PacRuleUrl = "not-a-url",
                Bypass = ["internal.corp"]
            });
            PacScriptBuilder.ValidateMode(new WebProxyMode
            {
                Strategy = WebProxyStrategy.Pac,
                PacPreset = PacRulePreset.CustomRulesOnly,
                PacRuleUrl = "not-a-url"
            });

            // Compaction (decodeDomains) & pruning for large lists (>= 64 domains).
            var manyRules = new List<string> { "cn", "sub.cn", "full:api.sub.cn", "baidu.com", "baidubce.com", "baidupcs.com" };
            for (var i = 0; i < 70; i++)
                manyRules.Add($"site{i:D2}.example.org");
            var compactJs = PacScriptBuilder.BuildPacScript(new WebProxyMode
            {
                Strategy = WebProxyStrategy.Pac,
                PacPreset = PacRulePreset.CustomRulesOnly,
                Handle = manyRules
            });
            StringAssert.Contains(compactJs, "decodeDomains({");
            Assert.IsFalse(compactJs.Contains("\"sub.cn\"", StringComparison.Ordinal));
            Assert.IsFalse(compactJs.Contains("\"api.sub.cn\"", StringComparison.Ordinal));
            StringAssert.Contains(compactJs, "\"cn\"");
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task PacHttpServerServesPacOverLoopback()
    {
        const string template = "var proxy = \"__PROXY__\";\nfunction FindProxyForURL(url, host) { return proxy; }";
        using var server = new PacHttpServer();
        server.Start(template, 2801, "0.0.0.0", 0);
        try
        {
            Assert.IsTrue(server.ListeningPort > 0);
            StringAssert.StartsWith(server.PacUrl, $"http://127.0.0.1:{server.ListeningPort}/pac?t=");

            using var handler = new HttpClientHandler { UseProxy = false };
            using var client = new HttpClient(handler);

            using var getResp = await client.GetAsync(server.PacUrl);
            Assert.AreEqual(HttpStatusCode.OK, getResp.StatusCode);
            Assert.AreEqual("application/x-ns-proxy-autoconfig", getResp.Content.Headers.ContentType?.MediaType);
            Assert.IsTrue(getResp.Headers.CacheControl?.NoStore == true);
            var body = await getResp.Content.ReadAsStringAsync();
            StringAssert.Contains(body, "PROXY 127.0.0.1:2801;");
            StringAssert.Contains(body, "FindProxyForURL");

            using var hostReq = new HttpRequestMessage(HttpMethod.Get, server.PacUrl);
            hostReq.Headers.Host = $"192.168.50.10:{server.ListeningPort}";
            using var hostResp = await client.SendAsync(hostReq);
            var lanBody = await hostResp.Content.ReadAsStringAsync();
            StringAssert.Contains(lanBody, "PROXY 192.168.50.10:2801;");

            using var tcp = new TcpClient();
            await tcp.ConnectAsync(IPAddress.Loopback, server.ListeningPort);
            await using (var netStream = tcp.GetStream())
            {
                var rawReq = Encoding.ASCII.GetBytes($"GET http://127.0.0.1:{server.ListeningPort}/pac?t=1 HTTP/1.1\r\nHost: fe80::1\r\nConnection: close\r\n\r\n");
                await netStream.WriteAsync(rawReq);
                await netStream.FlushAsync();
                using var reader = new StreamReader(netStream, Encoding.UTF8);
                var rawResp = await reader.ReadToEndAsync();
                StringAssert.Contains(rawResp, "200 OK");
                StringAssert.Contains(rawResp, "PROXY [fe80::1]:2801;");
            }

            using var headResp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, server.PacPrefix));
            Assert.AreEqual(HttpStatusCode.OK, headResp.StatusCode);
            Assert.AreEqual(0, (await headResp.Content.ReadAsByteArrayAsync()).Length);

            using var notFound = await client.GetAsync($"http://127.0.0.1:{server.ListeningPort}/favicon.ico");
            Assert.AreEqual(HttpStatusCode.NotFound, notFound.StatusCode);
        }
        finally
        {
            await server.StopAsync();
        }

        Assert.AreEqual(0, server.ListeningPort);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task WebProxyControllerLifecycleAndGuards()
    {
        var temp = Directory.CreateTempSubdirectory("netch-webproxy-ctrl-");
        var origLocal = Netch.Global.Settings.LocalAddress;
        var origPacPort = Netch.Global.Settings.WebProxy.PacPort;
        try
        {
            Netch.Global.Settings.LocalAddress = "127.0.0.1";
            Netch.Global.Settings.WebProxy.PacPort = 0;

            var binPac = Path.Combine(temp.FullName, "bin", "pac");
            Directory.CreateDirectory(binPac);
            await File.WriteAllTextAsync(Path.Combine(binPac, "gfw.txt"), "google.com\n");
            var statePath = Path.Combine(temp.FullName, "data", "system-proxy.state.json");

            // 1. Global mode + 0.0.0.0 loopback normalization + restore previous
            var fake = new FakeSystemProxyService
            {
                Current = new SystemProxySnapshot(SystemProxySnapshot.ProxyTypeProxy | SystemProxySnapshot.ProxyTypeDirect, "10.0.0.2:8888", "localhost", "")
            };
            var controller = new WebProxyController(fake, statePath, temp.FullName);
            var globalMode = new WebProxyMode { Strategy = WebProxyStrategy.Global, BypassLan = true };
            await controller.StartAsync(new SocksServer("0.0.0.0", 2801), globalMode);

            Assert.AreEqual(1, fake.SetGlobalCalls);
            Assert.AreEqual("127.0.0.1:2801", fake.Current.ProxyServer);
            Assert.IsTrue(File.Exists(statePath));

            await controller.StopAsync();
            Assert.AreEqual(1, fake.RestoreCalls);
            Assert.AreEqual("10.0.0.2:8888", fake.Current.ProxyServer);
            Assert.IsFalse(File.Exists(statePath));

            // 2. PAC mode + HTTP serving + stop cleanup
            fake.Current = new SystemProxySnapshot(SystemProxySnapshot.ProxyTypeDirect, "", "", "");
            var pacController = new WebProxyController(fake, statePath, temp.FullName);
            var pacMode = new WebProxyMode { Strategy = WebProxyStrategy.Pac, PacPreset = PacRulePreset.GfwList };
            await pacController.StartAsync(new SocksServer("::", 2801), pacMode);

            Assert.AreEqual(1, fake.SetPacCalls);
            Assert.IsNotNull(pacController.ActivePacUrl);
            Assert.AreEqual(pacController.ActivePacUrl, fake.Current.AutoConfigUrl);
            StringAssert.Matches(pacController.ActivePacUrl, new Regex(@"^http://127\.0\.0\.1:\d+/pac\?t=\d+$"));

            await pacController.StopAsync();
            Assert.IsNull(pacController.ActivePacUrl);
            Assert.IsTrue(fake.Current.IsDirect);
            Assert.IsFalse(File.Exists(statePath));

            // 3. Manual mode does not touch system proxy
            var manualFake = new FakeSystemProxyService();
            var manualController = new WebProxyController(manualFake, statePath, temp.FullName);
            await manualController.StartAsync(new SocksServer("127.0.0.1", 2801), new WebProxyMode { Strategy = WebProxyStrategy.Manual });
            await manualController.StopAsync();
            Assert.AreEqual(0, manualFake.SetGlobalCalls + manualFake.SetPacCalls + manualFake.SetDirectCalls + manualFake.RestoreCalls);

            // 4. External modification guard: do not overwrite user's manual change
            var extFake = new FakeSystemProxyService();
            var extController = new WebProxyController(extFake, statePath, temp.FullName);
            await extController.StartAsync(new SocksServer("127.0.0.1", 2801), globalMode);
            extFake.Current = new SystemProxySnapshot(SystemProxySnapshot.ProxyTypeProxy | SystemProxySnapshot.ProxyTypeDirect, "127.0.0.1:9999", "", "");
            await extController.StopAsync();
            Assert.AreEqual("127.0.0.1:9999", extFake.Current.ProxyServer);
            Assert.IsFalse(File.Exists(statePath));

            // 5. Previous state pointed to stale Netch PAC -> fall back to Direct instead of restoring dead PAC
            var stalePrev = new SystemProxySnapshot(SystemProxySnapshot.ProxyTypeAutoProxyUrl | SystemProxySnapshot.ProxyTypeDirect, "", "", "http://127.0.0.1:2803/pac?t=20260101000000000");
            var staleFake = new FakeSystemProxyService { Current = stalePrev };
            var staleController = new WebProxyController(staleFake, statePath, temp.FullName);
            await staleController.StartAsync(new SocksServer("127.0.0.1", 2801), globalMode);
            await staleController.StopAsync();
            Assert.AreEqual(1, staleFake.SetDirectCalls);
            Assert.IsTrue(staleFake.Current.IsDirect);

            // 6. Crash recovery restores previous when current matches, skips when modified externally
            var crashFake = new FakeSystemProxyService
            {
                Current = new SystemProxySnapshot(SystemProxySnapshot.ProxyTypeProxy | SystemProxySnapshot.ProxyTypeDirect, "127.0.0.1:2801", "localhost", "")
            };
            SystemProxyStateStore.SaveState(
                new SystemProxyRecoveryState(
                    new SystemProxySnapshot(SystemProxySnapshot.ProxyTypeDirect, "", "", ""),
                    "Global",
                    "127.0.0.1:2801"),
                statePath);
            SystemProxyStateStore.RecoverStaleState(crashFake, statePath);
            Assert.AreEqual(1, crashFake.RestoreCalls);
            Assert.IsTrue(crashFake.Current.IsDirect);
            Assert.IsFalse(File.Exists(statePath));

            // 7. Bare IPv6 loopback host formatted as [::1]:port and missing custom PAC throws MessageException
            var ipv6Fake = new FakeSystemProxyService();
            var ipv6Controller = new WebProxyController(ipv6Fake, statePath, temp.FullName);
            await ipv6Controller.StartAsync(new SocksServer("::1", 2801), globalMode);
            Assert.AreEqual("[::1]:2801", ipv6Fake.Current.ProxyServer);
            await ipv6Controller.StopAsync();

            var badController = new WebProxyController(new FakeSystemProxyService(), statePath, temp.FullName);
            await Assert.ThrowsExactlyAsync<MessageException>(() => badController.StartAsync(
                new SocksServer("127.0.0.1", 2801),
                new WebProxyMode { Strategy = WebProxyStrategy.Pac, PacPreset = PacRulePreset.CustomScript, PacScriptPath = "does-not-exist.pac" }));
        }
        finally
        {
            Netch.Global.Settings.LocalAddress = origLocal;
            Netch.Global.Settings.WebProxy.PacPort = origPacPort;
            temp.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void MainControllerWebProxyCompatibility()
    {
        var controller = ModeService.GetModeControllerByType(ModeType.WebProxyMode, out var port, out _);
        Assert.IsInstanceOfType<WebProxyController>(controller);
        Assert.IsNull(port);
        Assert.IsTrue(controller.Features.HasFlag(ModeFeature.RequireLocalHttpProxy));
        Assert.IsTrue(controller.Features.HasFlag(ModeFeature.SupportSocks5Auth));
        Assert.IsTrue(controller.Features.HasFlag(ModeFeature.SupportIPv6));

        var mode = new WebProxyMode { Strategy = WebProxyStrategy.Pac };
        MainController.EnsureUdpCompatibility(new HttpServer { Address = "127.0.0.1", Port = 8080 }, mode);
        MainController.EnsureUdpCompatibility(new SSHServer { Address = "127.0.0.1", Port = 22 }, mode);
        MainController.EnsureUdpCompatibility(new NaiveServer { Address = "example.com", Port = 443 }, mode);
        MainController.EnsureUdpCompatibility(new ShadowTLSServer { Address = "example.com", Port = 443 }, mode);
    }

    [TestMethod]
    public void BundledWebProxyPresetModesLoadCleanly()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? webModeDir = null;
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Storage", "mode", "Web");
            if (Directory.Exists(candidate))
            {
                webModeDir = candidate;
                break;
            }
            dir = dir.Parent;
        }

        Assert.IsNotNull(webModeDir, "Could not locate Storage/mode/Web directory.");
        var files = Directory.GetFiles(webModeDir, "*.json");
        Assert.AreEqual(4, files.Length);

        foreach (var file in files)
        {
            var mode = ModeHelper.LoadMode(file) as WebProxyMode;
            Assert.IsNotNull(mode, $"Failed to load {file} as WebProxyMode.");
            PacScriptBuilder.ValidateMode(mode);
        }
    }

    [TestMethod]
    public void PacRuleServiceResolvesUserOverBundledAndDownloadUrls()
    {
        var temp = Directory.CreateTempSubdirectory("netch-pac-service-");
        try
        {
            var binFile = Path.Combine(temp.FullName, "bin", "pac", "gfw.txt");
            var dataFile = Path.Combine(temp.FullName, "data", "pac", "gfw.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(binFile)!);
            File.WriteAllText(binFile, "google.com\n");

            Assert.AreEqual(binFile, PacRuleService.ResolveRuleFilePath(PacRulePreset.GfwList, temp.FullName));
            var bundledStatus = PacRuleService.GetRuleStatus(PacRulePreset.GfwList, temp.FullName);
            Assert.IsTrue(bundledStatus.Exists);
            Assert.IsFalse(bundledStatus.IsUserUpdated);
            Assert.AreEqual(1, bundledStatus.RuleCount);

            Directory.CreateDirectory(Path.GetDirectoryName(dataFile)!);
            File.WriteAllText(dataFile, "google.com\nyoutube.com\n");
            Assert.AreEqual(dataFile, PacRuleService.ResolveRuleFilePath(PacRulePreset.GfwList, temp.FullName));
            var userStatus = PacRuleService.GetRuleStatus(PacRulePreset.GfwList, temp.FullName);
            Assert.IsTrue(userStatus.IsUserUpdated);
            Assert.AreEqual(2, userStatus.RuleCount);

            Assert.AreEqual("https://raw.githubusercontent.com/Loyalsoldier/v2ray-rules-dat/release/proxy-list.txt",
                PacRuleService.GetDefaultUrl(PacRulePreset.ProxyList));
            Assert.AreEqual("https://cdn.jsdelivr.net/gh/Loyalsoldier/v2ray-rules-dat@release/direct-list.txt",
                PacRuleService.GetFallbackUrl(PacRulePreset.BypassChina));
            Assert.AreEqual("https://mirror.example/https://raw.githubusercontent.com/Loyalsoldier/v2ray-rules-dat/release/gfw.txt",
                PacRuleService.ResolveDownloadUrl(PacRulePreset.GfwList, null, null, "https://mirror.example/"));
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task PacRuleServiceVersionComparisonAndSaveValidation()
    {
        Assert.IsTrue(PacRuleService.TryParseReleaseTagTimestampUtc("202611232203", out var parsedUtc));
        Assert.AreEqual(new DateTimeOffset(2026, 11, 23, 22, 3, 0, TimeSpan.Zero), parsedUtc);
        Assert.IsFalse(PacRuleService.TryParseReleaseTagTimestampUtc("not-a-tag", out _));

        Assert.IsTrue(PacRuleService.IsUpdateAvailable(new PacRuleStatus(false, null, false, null, 0), null, "202611232203"));

        var bundledOld = new PacRuleStatus(true, "bin/pac/proxy-list.txt", false, new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc), 100);
        Assert.IsTrue(PacRuleService.IsUpdateAvailable(bundledOld, null, "202611232203"));

        var bundledFresh = new PacRuleStatus(true, "bin/pac/proxy-list.txt", false, new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc), 100);
        Assert.IsFalse(PacRuleService.IsUpdateAvailable(bundledFresh, null, "202611232203"));

        var userStatus = new PacRuleStatus(true, "data/pac/proxy-list.txt", true, new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc), 100);
        Assert.IsFalse(PacRuleService.IsUpdateAvailable(userStatus, "202611232203", "202611232203"));
        Assert.IsTrue(PacRuleService.IsUpdateAvailable(userStatus, "202611200000", "202611232203"));
        Assert.IsFalse(PacRuleService.IsUpdateAvailable(userStatus, "202611200000", null));

        var temp = Directory.CreateTempSubdirectory("netch-pac-save-");
        try
        {
            var (savedPath, count) = await PacRuleService.SaveValidatedContentAsync(PacRulePreset.ProxyList, "google.com\nyoutube.com\n", temp.FullName);
            Assert.IsTrue(File.Exists(savedPath));
            Assert.AreEqual(2, count);

            var metadataOnly = PacRuleService.GetRuleStatus(PacRulePreset.ProxyList, temp.FullName, includeRuleCount: false);
            Assert.IsTrue(metadataOnly.Exists);
            Assert.IsTrue(metadataOnly.IsUserUpdated);
            Assert.IsNotNull(metadataOnly.LastWriteTime);
            Assert.AreEqual(0, metadataOnly.RuleCount);

            var withCount = PacRuleService.GetRuleStatus(PacRulePreset.ProxyList, temp.FullName, includeRuleCount: true);
            Assert.IsTrue(withCount.Exists);
            Assert.IsTrue(withCount.IsUserUpdated);
            Assert.AreEqual(2, withCount.RuleCount);

            await Assert.ThrowsExactlyAsync<InvalidDataException>(() =>
                PacRuleService.SaveValidatedContentAsync(PacRulePreset.ProxyList, "# empty comment only\n", temp.FullName));
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }
}
