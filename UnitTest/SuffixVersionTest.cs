using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Models.GitHubRelease;

namespace UnitTest
{
    [TestClass]
    public class SuffixVersionTest
    {
        [TestMethod]
        public void Test()
        {
            var rel = SuffixVersion.Parse("1.0.0");
            var a1 = SuffixVersion.Parse("1.0.0-Alpha1");
            var a3 = SuffixVersion.Parse("1.0.0-aLpHa3");
            var b2 = SuffixVersion.Parse("1.0.0-betA2");

            Assert.AreEqual(rel.ToString(), "1.0.0");
            Assert.AreEqual(a1.ToString(), "1.0.0-Alpha1");
            Assert.IsTrue(rel.CompareTo(a1) > 0);
            Assert.IsTrue(rel.CompareTo(b2) > 0);

            Assert.IsTrue(b2.CompareTo(a1) > 0);
            Assert.IsTrue(b2.CompareTo(a3) > 0);
        }
    }
}