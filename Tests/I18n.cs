#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Utils;

namespace Tests;

[TestClass]
[DoNotParallelize]
public class I18n
{
    [TestCleanup]
    public void RestoreLanguage() => i18N.Load("en-US");

    [TestMethod]
    public void TranslateListMatchesExpectedBuiltInOrder()
    {
        var list = i18N.GetTranslateList();

        CollectionAssert.AreEqual(
            new[] { "System", "zh-CN", "zh-TW", "en-US", "ar", "ja-JP", "ru", "fa-IR" },
            list.Take(8).ToArray());
    }

    [TestMethod]
    public void DisplayNamesShowLanguageOnlyInNativeScript()
    {
        Assert.AreEqual("简体中文", i18N.GetDisplayName("zh-CN"));
        Assert.AreEqual("繁體中文", i18N.GetDisplayName("zh-TW"));
        Assert.AreEqual("English", i18N.GetDisplayName("en-US"));
        Assert.AreEqual("العربية", i18N.GetDisplayName("ar"));
        Assert.AreEqual("日本語", i18N.GetDisplayName("ja-JP"));
        Assert.AreEqual("Русский", i18N.GetDisplayName("ru"));
        Assert.AreEqual("فارسی", i18N.GetDisplayName("fa-IR"));

        Assert.AreEqual("繁體中文", i18N.GetDisplayName("zh-HK"));
        Assert.AreEqual("العربية", i18N.GetDisplayName("ar-SA"));
        Assert.AreEqual("Русский", i18N.GetDisplayName("ru-RU"));
        Assert.AreEqual("فارسی", i18N.GetDisplayName("fa"));
        Assert.AreEqual("Français", i18N.GetDisplayName("fr-FR"));
        Assert.AreEqual("한국어", i18N.GetDisplayName("ko-KR"));
        Assert.AreEqual("Deutsch", i18N.GetDisplayName("de-DE"));

        foreach (var code in new[] { "zh-CN", "zh-TW", "en-US", "ar", "ja-JP", "ru", "fa-IR", "fr-FR", "ko-KR", "de-DE" })
        {
            var name = i18N.GetDisplayName(code);
            Assert.IsFalse(name.Contains('(') || name.Contains('（'), $"Unexpected region in display name for {code}: {name}");
        }
    }

    [TestMethod]
    public void BuiltInLanguagesLoadAndPreserveFormatPlaceholders()
    {
        i18N.Load("zh-CN");
        Assert.AreEqual("zh-CN", i18N.LangCode);
        var referenceCount = i18N.Data.Count;
        Assert.IsTrue(referenceCount > 500);

        foreach (var (code, expectedLanguageLabel, expectedYamlLabel) in new[]
                 {
                     ("zh-CN", "语言", "Clash YAML 文件"),
                     ("zh-TW", "語言", "Clash YAML 檔案"),
                     ("ar", "اللغة", "ملف Clash YAML"),
                     ("ja-JP", "言語", "Clash YAML ファイル"),
                     ("ru", "Язык", "Файл Clash YAML"),
                     ("fa-IR", "زبان", "فایل Clash YAML")
                 })
        {
            i18N.Load(code);
            Assert.AreEqual(code, i18N.LangCode);
            Assert.IsTrue(i18N.Data.Count > 500, $"Expected populated translations for {code}");
            if (code is "ar" or "ru")
                Assert.AreEqual(referenceCount, i18N.Data.Count, $"Expected full key parity with zh-CN for {code}");
            Assert.AreEqual(expectedLanguageLabel, i18N.Translate("Language"));
            Assert.AreEqual(expectedYamlLabel, i18N.Translate("Clash YAML file"));

            foreach (DictionaryEntry entry in i18N.Data)
            {
                var key = (string)entry.Key;
                var value = (string?)entry.Value;
                Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"Blank translation for '{key}' in {code}");
                if (key.Contains('{') || value!.Contains('{'))
                {
                    Assert.AreEqual(
                        CompositeFormat.Parse(key).MinimumArgumentCount,
                        CompositeFormat.Parse(value!).MinimumArgumentCount,
                        $"Format placeholder mismatch for '{key}' in {code}");
                }
            }
        }

        i18N.Load("en-US");
        Assert.AreEqual("en-US", i18N.LangCode);
        Assert.AreEqual(0, i18N.Data.Count);
        Assert.AreEqual("Language", i18N.Translate("Language"));
    }

    [TestMethod]
    public void RegionalAndUnknownLanguagesResolveCleanly()
    {
        foreach (var (requested, resolved) in new[]
                 {
                     ("zh-SG", "zh-CN"),
                     ("zh-HK", "zh-TW"),
                     ("en-GB", "en-US"),
                     ("ar-SA", "ar"),
                     ("ar-EG", "ar"),
                     ("ja", "ja-JP"),
                     ("ru-RU", "ru"),
                     ("fa", "fa-IR"),
                     ("zz-ZZ", "en-US")
                 })
        {
            i18N.Load(requested);
            Assert.AreEqual(resolved, i18N.LangCode);
        }
    }

    [TestMethod]
    public void ExternalTranslateListAcceptsNeutralCulturesAndIgnoresNonCultureFiles()
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var temp = Directory.CreateTempSubdirectory("netch-test-i18n-");
        try
        {
            Directory.SetCurrentDirectory(temp.FullName);
            Directory.CreateDirectory("i18n");
            File.WriteAllText(Path.Combine("i18n", "fr"), "{}");
            File.WriteAllText(Path.Combine("i18n", "ar"), "{}");
            File.WriteAllText(Path.Combine("i18n", "README.txt"), "not a locale");
            File.WriteAllText(Path.Combine("i18n", "notes"), "not a locale");

            var list = i18N.GetTranslateList();
            CollectionAssert.Contains(list, "fr");
            Assert.AreEqual(1, list.Count(code => string.Equals(code, "ar", StringComparison.OrdinalIgnoreCase)));
            CollectionAssert.DoesNotContain(list, "README.txt");
            CollectionAssert.DoesNotContain(list, "notes");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            temp.Delete(recursive: true);
        }
    }
}
