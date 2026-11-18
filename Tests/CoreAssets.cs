using Microsoft.VisualStudio.TestTools.UnitTesting;
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
}
