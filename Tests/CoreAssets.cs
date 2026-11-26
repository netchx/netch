using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Controllers;
using Netch.Models.GitHubRelease;
using Netch.Models.Modes.ProcessMode;
using Netch.Servers;
using Netch.Utils;

namespace Tests;

[TestClass]
public class CoreAssets
{
    [TestMethod]
    public void EmbeddedAssetsLoadFromCore()
    {
        var zh = EmbedUtils.GetEmbedBytes("Netch.Resources.zh-CN");
        Assert.IsTrue(zh.Length > 0);

        Assert.IsFalse(string.IsNullOrEmpty(EmbedUtils.GetEmbedText(Netch.Constants.V2raySampleHttpRequestFileName)));
    }

    [TestMethod]
    [DoNotParallelize]
    public void ChineseTranslationLoads()
    {
        try
        {
            i18N.Load("zh-CN");
            Assert.AreNotEqual("Start", i18N.Translate("Start"));
        }
        finally
        {
            i18N.Load("en-US");
        }
    }

    [TestMethod]
    [DoNotParallelize]
    [DataRow("zh-CN", "已导入 3 个节点。")]
    [DataRow("zh-TW", "已匯入 3 個節點。")]
    [DataRow("zh-HK", "已匯入 3 個節點。")]
    [DataRow("ja-JP", "3 個のノードをインポートしました。")]
    [DataRow("ar", "تم استيراد 3 عقدة.")]
    [DataRow("ru", "Импортировано узлов: 3.")]
    [DataRow("fa-IR", "3 گره وارد شد.")]
    public void FormatsEmbeddedTranslations(string language, string expected)
    {
        try
        {
            CollectionAssert.Contains(i18N.GetTranslateList(), "ja-JP");
            i18N.Load(language);
            Assert.AreEqual(expected, i18N.TranslateFormat("Imported {0} nodes.", 3));
        }
        finally
        {
            i18N.Load("en-US");
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void SystemLanguageUsesUiCultureAndModesHaveFallbackNames()
    {
        var current = CultureInfo.CurrentCulture;
        var currentUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-TW");
            i18N.Load("System");
            Assert.AreEqual("zh-TW", i18N.LangCode);

            var mode = new Redirector { FullName = "Custom.json", Remark = { ["zh-CN"] = "自定义规则" } };
            Assert.AreEqual("Custom", mode.i18NRemark);
            mode.Remark["en"] = "Custom rules";
            Assert.AreEqual("Custom rules", mode.i18NRemark);
        }
        finally
        {
            CultureInfo.CurrentCulture = current;
            CultureInfo.CurrentUICulture = currentUi;
            i18N.Load("en-US");
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void RegionalOverrideKeepsEmbeddedTranslationsAndIgnoresInvalidFormat()
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var temp = Directory.CreateTempSubdirectory("netch-test-locale-");
        try
        {
            Directory.SetCurrentDirectory(temp.FullName);
            Directory.CreateDirectory("i18n");
            File.WriteAllText(Path.Combine("i18n", "zh-HK"), """
                {"Settings":"自訂設定","Imported {0} nodes.":"已匯入 {1} 個節點。"}
                """);

            i18N.Load("zh-HK");
            Assert.AreEqual("zh-HK", i18N.LangCode);
            Assert.AreEqual("自訂設定", i18N.Translate("Settings"));
            Assert.AreEqual("已匯入 3 個節點。", i18N.TranslateFormat("Imported {0} nodes.", 3));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            i18N.Load("en-US");
            temp.Delete(recursive: true);
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void ChineseTranslationCoversAllValidatorMessagesAndProtocolLabels()
    {
        var keys = new[]
        {
            "Enter an address and a valid port.",
            "Enter both a SOCKS username and password, or leave both empty.",
            "HTTP headers must use 'Name: Value' format.",
            "Check the required protocol fields.",
            "Invalid VLESS encryption setting.",
            "Check the Shadowsocks encryption method and password.",
            "Enter valid Base64 WireGuard keys.",
            "The WireGuard local interface address is invalid.",
            "WireGuard reserved must contain three values between 0 and 255.",
            "Enter an obfuscation password.",
            "Gecko packet sizes must be positive integers.",
            "Gecko min packet size cannot exceed max packet size.",
            "Port hopping range is invalid.",
            "Hop interval must be at least 5 seconds.",
            "Enter a valid TUIC UUID and password.",
            "URI template must start with '/'.",
            "Invalid PEM certificate.",
            "Enter a Reality public key.",
            "Short ID must be an even-length hex string up to 16 characters.",
            "Choose a transport that supports REALITY or change the TLS security mode.",
            "XHTTP extra must be a valid JSON object.",
            "Legacy flow",
            "Gecko min packet size",
            "Gecko max packet size",
            "Idle check interval (s)",
            "Idle session timeout (s)",
            "Min idle sessions",
            "QUIC congestion control",
            "UDP relay mode",
            "Reduce RTT",
            "Disable SNI",
            "Enter a port between 1 and 65535.",
            "Import Servers From File…",
            "Start automatically at launch",
            "Connect the startup or last used scene",
            "Minimize to tray when started",
            "Update subscriptions at launch",
            "Update (direct)",
            "Update via proxy",
            "Update subscriptions",
            "Update all (direct)",
            "Update all via proxy",
            "Select proxy node",
            "Select a node to use as a temporary proxy for updating subscriptions.",
            "No nodes available to use as proxy.",
            "The session is busy. Try again when it is ready.",
            "The temporary proxy could not start.",
            "Enter a valid group name.",
            "A group with this name already exists.",
            "AWG version",
            "Header protection key",
            "Health check",
            "Port range",
            "Multiplexing",
            "Handshake mode",
            "Traffic pattern",
            "QUIC versions",
            "TrustTunnel does not support REALITY.",
            "Max connections and max streams cannot both be set.",
            "Mieru transport must be TCP or UDP.",
            "Mieru port range must be a single start-end range.",
            "Mieru traffic pattern must be valid Base64.",
            "ShadowQUIC versions must be v1 or v2.",
            "AmneziaWG supports at most one IPv4 and one IPv6 local address.",
            "AmneziaWG Jmin cannot exceed Jmax.",
            "AmneziaWG H1-H4 must be unsigned integers or min-max ranges."
        };

        try
        {
            i18N.Load("zh-CN");
            foreach (var key in keys)
                Assert.AreNotEqual(key, i18N.Translate(key), $"Missing zh-CN translation for: {key}");
        }
        finally
        {
            i18N.Load("en-US");
        }
    }

    [TestMethod]
    public void BuildSocks5ProxyUriAndCreateProxyHandleCredentials()
    {
        Assert.AreEqual("socks5://127.0.0.1:2801/", WebUtil.BuildSocks5ProxyUri(new SocksServer("0.0.0.0", 2801)));
        Assert.AreEqual("socks5://127.0.0.1:2801/", WebUtil.BuildSocks5ProxyUri(new SocksServer("[::]", 2801)));

        var authed = WebUtil.BuildSocks5ProxyUri(new SocksServer("::", 2801, "user@a:1", "p:2/3%"));
        Assert.AreEqual("socks5://user%40a%3A1:p%3A2%2F3%25@127.0.0.1:2801/", authed);

        var createProxy = typeof(WebUtil).GetMethod("CreateProxy", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(createProxy);
        var proxy = (WebProxy)createProxy.Invoke(null, [$"  {authed}  "])!;
        Assert.AreEqual("socks5://127.0.0.1:2801/", proxy.Address!.AbsoluteUri);
        var credential = proxy.Credentials!.GetCredential(proxy.Address, "Basic");
        Assert.IsNotNull(credential);
        Assert.AreEqual("user@a:1", credential.UserName);
        Assert.AreEqual("p:2/3%", credential.Password);

        var unauthedProxy = (WebProxy)createProxy.Invoke(null, ["socks5://127.0.0.1:2801/"])!;
        Assert.IsNull(unauthedProxy.Credentials);

        using var client = WebUtil.CreateHttpClient(proxyServer: authed);
        Assert.IsNotNull(client);
    }

    [TestMethod]
    [DoNotParallelize]
    public void UpdateCheckerExtractsWinUiZipFromReleaseNotes()
    {
        var original = UpdateChecker.LatestRelease;
        const string expectedHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        try
        {
            UpdateChecker.LatestRelease = new Release
            {
                tag_name = "v2.0.2",
                body = $"""
                    ## 亮点

                    - WinUI release.

                    ## 下载与启动

                    1. 下载 `Netch-v2.0.2-winui-x64.zip`。

                    ## SHA256

                    | 文件 | SHA256 |
                    | --- | --- |
                    | `Netch-v2.0.2-winui-x64.zip` | `{expectedHash}` |
                    """
            };

            var (fileName, sha256) = UpdateChecker.GetLatestUpdateFileNameAndHash();
            Assert.AreEqual("Netch-v2.0.2-winui-x64.zip", fileName);
            Assert.AreEqual(expectedHash, sha256);
        }
        finally
        {
            UpdateChecker.LatestRelease = original;
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public void PrereleaseVersionOrderingAndBilingualReleaseNotesParseCorrectly()
    {
        Assert.AreEqual("2.0.0-beta3", UpdateChecker.Version);
        Assert.IsTrue(VersionUtil.CompareVersion("2.0.0-beta1", "1.9.7") > 0);
        Assert.IsTrue(VersionUtil.CompareVersion("2.0.0-beta2", "2.0.0-beta1") > 0);
        Assert.IsTrue(VersionUtil.CompareVersion("2.0.0-beta3", "2.0.0-beta2") > 0);
        Assert.IsTrue(VersionUtil.CompareVersion("2.0.0-beta4", "2.0.0-beta3") > 0);
        Assert.IsTrue(VersionUtil.CompareVersion("2.0.0", "2.0.0-beta1") > 0);
        Assert.IsTrue(VersionUtil.CompareVersion("2.0.0", "2.0.0-beta2") > 0);
        Assert.IsTrue(VersionUtil.CompareVersion("2.0.0", "2.0.0-beta3") > 0);
        Assert.AreEqual(0, VersionUtil.CompareVersion("2.0.0-beta1", "2.0.0-beta1"));

        var original = UpdateChecker.LatestRelease;
        const string expectedHash = "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210";
        try
        {
            UpdateChecker.LatestRelease = new Release
            {
                tag_name = "v2.0.0-beta1",
                prerelease = true,
                body = $"""
                    > [!NOTE]
                    > Pre-release note.

                    ## 中文更新日志
                    - 重构 WinUI 3 客户端。

                    ## English Changelog
                    - Rewrote the desktop client with WinUI 3.

                    ## 下载与启动 / Download & Launch

                    1. 下载 `Netch-v2.0.0-beta1-winui-x64.zip`。
                    2. Download `Netch-v2.0.0-beta1-winui-x64.zip`.

                    ## SHA256

                    | 文件 / File | SHA256 |
                    | --- | --- |
                    | `Netch-v2.0.0-beta1-winui-x64.zip` | `{expectedHash}` |
                    """
            };

            var content = UpdateChecker.GetLatestReleaseContent();
            StringAssert.Contains(content, "## 中文更新日志");
            StringAssert.Contains(content, "## English Changelog");
            Assert.IsFalse(content.Contains("下载与启动", StringComparison.Ordinal));
            Assert.IsFalse(content.Contains("SHA256", StringComparison.Ordinal));

            var (fileName, sha256) = UpdateChecker.GetLatestUpdateFileNameAndHash();
            Assert.AreEqual("Netch-v2.0.0-beta1-winui-x64.zip", fileName);
            Assert.AreEqual(expectedHash, sha256);
        }
        finally
        {
            UpdateChecker.LatestRelease = original;
        }
    }
}
