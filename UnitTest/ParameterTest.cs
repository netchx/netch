using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Models;

namespace UnitTest
{
    [TestClass]
    public class ParameterTest
    {
        [Verb]
        private class VerbAndRealName : ParameterBase
        {
            [RealName("v")]
            public string v1 { get; } = "a";

            [RealName("v")]
            public string v2 { get; } = "b";
        }

        private class Full : ParameterBase
        {
            [RealName("f")]
            public string f1 { get; } = "a";

            [RealName("f")]
            public string f2 { get; } = "b";
        }

        private class FullWithVerb : ParameterBase
        {
            public string f { get; } = "a";

            [Verb]
            public string v { get; } = "b";
        }

        [Verb]
        private class VerbWithFull : ParameterBase
        {
            public string v { get; } = "a";

            [Full]
            public string f { get; } = "b";
        }

        private class QuoteValue : ParameterBase
        {
            public static string pathValue = @"C:\Programe Files\Damn thats space";

            [Quote]
            public string path { get; set; } = pathValue;
        }

        private class FlagAndOptional : ParameterBase
        {
            public bool a { get; set; } = true;

            public bool b { get; set; } = false;

            [Optional]
            public string c { get; set; } = string.Empty;

            [Optional]
            public string? d { get; set; } = null;
        }

        private class RequiredEmpty : ParameterBase
        {
            public string? udp { get; set; } = string.Empty;
        }

        private class RequiredNull : ParameterBase
        {
            public string? udp { get; set; } = null;
        }

        [TestMethod]
        public void Test()
        {
            Assert.AreEqual(new VerbAndRealName().ToString(), "-v a -v b");
            Assert.AreEqual(new Full().ToString(), "--f a --f b");
            Assert.AreEqual(new FullWithVerb().ToString(), "--f a -v b");
            Assert.AreEqual(new VerbWithFull().ToString(), "-v a --f b");
            Assert.AreEqual(new QuoteValue().ToString(), $"--path \"{QuoteValue.pathValue}\"");
            Assert.AreEqual(new FlagAndOptional().ToString(), "--a");
            Assert.ThrowsException<RequiredArgumentValueInvalidException>(() => { _ = new RequiredEmpty().ToString(); });
            Assert.ThrowsException<RequiredArgumentValueInvalidException>(() => { _ = new RequiredNull().ToString(); });
        }
    }
}