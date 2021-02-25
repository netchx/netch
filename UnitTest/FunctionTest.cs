using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Utils;

namespace UnitTest
{
    [TestClass]
    public class FunctionTest : TestBase
    {
        [TestMethod]
        public void TestLoadI18N()
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
    }
}