using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Utils;

namespace UnitTest
{
    [TestClass]
    public class Function : TestBase
    {
        [TestMethod]
        public void LoadLanguage()
        {
            void TestLoad(string t)
            {
                Console.WriteLine($"Load: {t}");
                i18N.Load(t);
                Console.WriteLine($"Result: {i18N.LangCode}\n");
            }

            Directory.CreateDirectory("logging");
            TestLoad("System");
            TestLoad("en-US");
            TestLoad("zh-CN");
            TestLoad("zh-HK");
            TestLoad("zh");
            TestLoad("HND123&*$_-^$@SAUI");
            TestLoad("");
            TestLoad("-");
        }

        [TestMethod]
        public void TestMaskToCidr()
        {
            Assert.AreEqual(Utils.SubnetToCidr("0.0.0.0"), 0);
            Assert.AreEqual(Utils.SubnetToCidr("248.0.0.0"), 5);
            Assert.AreEqual(Utils.SubnetToCidr("255.0.0.0"), 8);
            Assert.AreEqual(Utils.SubnetToCidr("255.255.0.0"), 16);
            Assert.AreEqual(Utils.SubnetToCidr("255.255.248.0"), 21);
            Assert.AreEqual(Utils.SubnetToCidr("255.255.255.0"), 24);
            Assert.AreEqual(Utils.SubnetToCidr("255.255.255.255"), 32);
        }
    }
}