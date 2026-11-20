using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Models.Modes.ProcessMode;
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

        var sevenZip = EmbedUtils.GetEmbedBytes("Netch.Resources.7za.exe");
        Assert.AreEqual((byte)'M', sevenZip[0]);
        Assert.AreEqual((byte)'Z', sevenZip[1]);

        Assert.IsFalse(string.IsNullOrEmpty(EmbedUtils.GetEmbedText(Netch.Constants.V2raySampleHttpRequestFileName)));
    }

    [TestMethod]
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
    [DataRow("zh-CN", "已导入 3 个节点。")]
    [DataRow("zh-TW", "已匯入 3 個節點。")]
    [DataRow("zh-HK", "已匯入 3 個節點。")]
    [DataRow("ja-JP", "3 個のノードをインポートしました。")]
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
}
