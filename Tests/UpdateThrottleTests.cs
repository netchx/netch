#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Services;

namespace Tests;

[TestClass]
public class UpdateThrottleTests
{
    [TestMethod]
    public void AutoCheckIntervalRespectsCooldownAndOffState()
    {
        var now = new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

        Assert.IsFalse(GitHubRateLimitPolicy.ShouldRunAutoCheck(0, null, now));
        Assert.IsTrue(GitHubRateLimitPolicy.ShouldRunAutoCheck(24, null, now));
        Assert.IsFalse(GitHubRateLimitPolicy.ShouldRunAutoCheck(24, now - TimeSpan.FromHours(12), now));
        Assert.IsTrue(GitHubRateLimitPolicy.ShouldRunAutoCheck(24, now - TimeSpan.FromHours(24), now));
        Assert.IsTrue(GitHubRateLimitPolicy.ShouldRunAutoCheck(6, now - TimeSpan.FromHours(7), now));
    }

    [TestMethod]
    public void RateLimitHeaderParsingCovers403ResetRetryAfterAndPreemptiveZeroRemaining()
    {
        var now = new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

        using var forbiddenWithReset = new HttpResponseMessage(HttpStatusCode.Forbidden);
        forbiddenWithReset.Headers.TryAddWithoutValidation("X-RateLimit-Remaining", "0");
        var resetEpoch = (now + TimeSpan.FromMinutes(30)).ToUnixTimeSeconds();
        forbiddenWithReset.Headers.TryAddWithoutValidation("X-RateLimit-Reset", resetEpoch.ToString());
        Assert.IsTrue(GitHubRateLimitPolicy.TryParseRateLimitReset(
            forbiddenWithReset.StatusCode, forbiddenWithReset.Headers, now, out var parsedReset));
        Assert.AreEqual(now + TimeSpan.FromMinutes(30), parsedReset);

        using var tooManyWithRetryAfter = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        tooManyWithRetryAfter.Headers.TryAddWithoutValidation("Retry-After", "120");
        Assert.IsTrue(GitHubRateLimitPolicy.TryParseRateLimitReset(
            tooManyWithRetryAfter.StatusCode, tooManyWithRetryAfter.Headers, now, out var retryReset));
        Assert.AreEqual(now + TimeSpan.FromSeconds(120), retryReset);

        // 200 OK with X-RateLimit-Remaining: 0 should trip preemptively before the next request hits 403.
        using var okExhausted = new HttpResponseMessage(HttpStatusCode.OK);
        okExhausted.Headers.TryAddWithoutValidation("X-RateLimit-Remaining", "0");
        okExhausted.Headers.TryAddWithoutValidation("X-RateLimit-Reset", resetEpoch.ToString());
        Assert.IsTrue(GitHubRateLimitPolicy.TryParseRateLimitReset(
            okExhausted.StatusCode, okExhausted.Headers, now, out var preemptiveReset));
        Assert.AreEqual(now + TimeSpan.FromMinutes(30), preemptiveReset);

        using var bareForbidden = new HttpResponseMessage(HttpStatusCode.Forbidden);
        Assert.IsTrue(GitHubRateLimitPolicy.TryParseRateLimitReset(
            bareForbidden.StatusCode, bareForbidden.Headers, now, out var fallbackReset));
        Assert.AreEqual(now + GitHubRateLimitPolicy.DefaultBackoff, fallbackReset);

        using var normalOk = new HttpResponseMessage(HttpStatusCode.OK);
        normalOk.Headers.TryAddWithoutValidation("X-RateLimit-Remaining", "55");
        Assert.IsFalse(GitHubRateLimitPolicy.TryParseRateLimitReset(
            normalOk.StatusCode, normalOk.Headers, now, out _));
    }

    [TestMethod]
    public void ApplicationAssetCandidatesMatchDeploymentPreference()
    {
        var sc = GitHubRateLimitPolicy.GetApplicationAssetCandidates("v2.0.1", selfContained: true);
        CollectionAssert.AreEqual(
            new[]
            {
                "Netch-2.0.1-winui-x64-self-contained.7z",
                "Netch-2.0.1-winui-x64-self-contained.zip",
                "Netch-2.0.1-winui-x64.7z",
                "Netch-2.0.1-winui-x64.zip",
                "Netch-v2.0.1-winui-x64-self-contained.7z",
                "Netch-v2.0.1-winui-x64-self-contained.zip",
                "Netch-v2.0.1-winui-x64.7z",
                "Netch-v2.0.1-winui-x64.zip"
            },
            sc.ToArray());

        var fdd = GitHubRateLimitPolicy.GetApplicationAssetCandidates("2.0.1", selfContained: false);
        CollectionAssert.AreEqual(
            new[]
            {
                "Netch-2.0.1-winui-x64-framework-dependent.7z",
                "Netch-2.0.1-winui-x64-framework-dependent.zip",
                "Netch-2.0.1-winui-x64-self-contained.7z",
                "Netch-2.0.1-winui-x64-self-contained.zip",
                "Netch-2.0.1-winui-x64.7z",
                "Netch-2.0.1-winui-x64.zip"
            },
            fdd.ToArray());
    }

    [TestMethod]
    public void UpdateArchiveExtractorExtractsZipSolid7zAndRejectsUnsupported()
    {
        const string solid7zBase64 = "N3q8ryccAAS7kqfO9AAAAAAAAAAiAAAAAAAAAII10CDgAD4ALl0ANxmItlqknB9TjpwS5ZMlf1O3S59X3rEJ55shAUh7VejGEYzFp4Xpj6IKm9QQIAAAAIEzB64P0VujJKCQoHdexUAYkWpwUsECIlKXQnp+hmiI0d+Ei0VNBSMJnP9JMx025UQFmZcyGLoGaZYUzeQtkcdHYs18vU1wXxjPRi+7ZIkWmdxrtR0sh+PAHlGXytjeiGgkWHckthjnFOuedlWdh0w3eAiwY7wkjosTcV0uzrekHHfz6cv0XbOEOJkcxmJhBs1xWSJzA3N/HoymRWLPaH0T52WhonvSKE+dYHanFrj1tKWBEB+V363xpyxAFwY2AQmAvgAHCwEAASMDAQEFXQAQAAAMgU4KAeYkFfAAAA==";

        var tempDir = Directory.CreateTempSubdirectory("netch-archive-extract-test-");
        try
        {
            var zipPath = Path.Combine(tempDir.FullName, "update.zip");
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                WriteEntry(zip, "Netch.App.exe", "zip-app-payload");
                WriteEntry(zip, "bin/nfdriver.sys", "zip-nfdriver-payload");
                WriteEntry(zip, "bin/sing_box/libcronet.dll", "zip-cronet-payload");
            }

            var zipOut = Path.Combine(tempDir.FullName, "out-zip");
            UpdateArchiveExtractor.ExtractToDirectory(zipPath, zipOut);
            Assert.AreEqual("zip-app-payload", File.ReadAllText(Path.Combine(zipOut, "Netch.App.exe")));
            Assert.AreEqual("zip-nfdriver-payload", File.ReadAllText(Path.Combine(zipOut, "bin", "nfdriver.sys")));
            Assert.AreEqual("zip-cronet-payload", File.ReadAllText(Path.Combine(zipOut, "bin", "sing_box", "libcronet.dll")));

            var sevenZipBytes = Convert.FromBase64String(solid7zBase64);
            var sevenZipPath = Path.Combine(tempDir.FullName, "update.7z");
            File.WriteAllBytes(sevenZipPath, sevenZipBytes);

            var sevenZipOut = Path.Combine(tempDir.FullName, "out-7z");
            UpdateArchiveExtractor.ExtractToDirectory(sevenZipPath, sevenZipOut);
            Assert.AreEqual("app-exe-payload", File.ReadAllText(Path.Combine(sevenZipOut, "Netch.App.exe")));
            Assert.AreEqual("nfdriver-payload", File.ReadAllText(Path.Combine(sevenZipOut, "bin", "nfdriver.sys")));
            Assert.AreEqual("redirector-payload", File.ReadAllText(Path.Combine(sevenZipOut, "bin", "Redirector.bin")));
            Assert.AreEqual("cronet-payload", File.ReadAllText(Path.Combine(sevenZipOut, "bin", "sing_box", "libcronet.dll")));

            // Signature detection should not rely on the file extension.
            var neutralPath = Path.Combine(tempDir.FullName, "update.tmp");
            File.WriteAllBytes(neutralPath, sevenZipBytes);
            var neutralOut = Path.Combine(tempDir.FullName, "out-tmp");
            UpdateArchiveExtractor.ExtractToDirectory(neutralPath, neutralOut);
            Assert.AreEqual("app-exe-payload", File.ReadAllText(Path.Combine(neutralOut, "Netch.App.exe")));

            var invalidPath = Path.Combine(tempDir.FullName, "corrupt.bin");
            File.WriteAllBytes(invalidPath, Encoding.ASCII.GetBytes("not-an-archive"));
            Assert.ThrowsExactly<InvalidDataException>(() =>
                UpdateArchiveExtractor.ExtractToDirectory(invalidPath, Path.Combine(tempDir.FullName, "out-invalid")));
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }

        static void WriteEntry(ZipArchive archive, string entryName, string content)
        {
            var entry = archive.CreateEntry(entryName);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(content);
        }
    }

    [TestMethod]
    public void UpdateCacheStorePersistsETagsAndRecoverFromCorruptFile()
    {
        var tempDir = Directory.CreateTempSubdirectory("netch-update-cache-test-");
        try
        {
            var path = Path.Combine(tempDir.FullName, "update-cache.json");
            var store = new UpdateCacheStore(path);
            var now = new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

            var state = new UpdateCacheState
            {
                LastAutoCheckUtc = now,
                LastCheckedUtc = now,
                RateLimitResetUtc = now + TimeSpan.FromMinutes(20),
                RemotePacVersion = "202611270000",
                RulesSourceKey = "netchx/netch/v2/Storage/mode",
                RulesTreeSha = "abc1234",
                RemoteRuleCount = 42
            };
            state.ApiResponses["https://api.github.com/repos/netchx/netch/git/trees/v2?recursive=1"] = new CachedApiResponse
            {
                ETag = "W/\"etag-1\"",
                Body = "{\"sha\":\"abc1234\"}",
                FetchedAtUtc = now
            };
            state.CoreLatest["sing-box"] = new CachedUpdateEntry
            {
                Version = "1.14.5",
                Url = "https://github.com/SagerNet/sing-box/releases/download/v1.14.5/sing-box-1.14.5-windows-amd64.zip",
                ReleaseUrl = "https://github.com/SagerNet/sing-box/releases/tag/v1.14.5",
                CheckedAtUtc = now
            };

            store.Save(state);
            var reloaded = store.Load();

            Assert.AreEqual(now, reloaded.LastAutoCheckUtc);
            Assert.AreEqual("202611270000", reloaded.RemotePacVersion);
            Assert.AreEqual("W/\"etag-1\"", reloaded.ApiResponses["https://api.github.com/repos/netchx/netch/git/trees/v2?recursive=1"].ETag);
            Assert.AreEqual("1.14.5", reloaded.CoreLatest["sing-box"].Version);

            File.WriteAllText(path, "{corrupt json");
            var recovered = store.Load();
            Assert.IsNotNull(recovered);
            Assert.AreEqual(0, recovered.ApiResponses.Count);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
