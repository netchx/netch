#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch;
using Netch.Models;
using Netch.Utils;

namespace Tests;

[TestClass]
public class SettingsAndPresetsTests
{
    [TestMethod]
    public void StunEndpointFormatsAndParsesCleanly()
    {
        Assert.AreEqual("stun.fitauto.ru", EndpointPresetHelper.FormatStunEndpoint("stun.fitauto.ru", 3478));
        Assert.AreEqual("stun.example.com:5349", EndpointPresetHelper.FormatStunEndpoint("stun.example.com", 5349));
        Assert.AreEqual("[2001:db8::1]:5349", EndpointPresetHelper.FormatStunEndpoint("2001:db8::1", 5349));

        Assert.IsTrue(EndpointPresetHelper.TryParseStunEndpoint("stun.fitauto.ru", out var host1, out var port1));
        Assert.AreEqual("stun.fitauto.ru", host1);
        Assert.AreEqual(3478, port1);

        Assert.IsTrue(EndpointPresetHelper.TryParseStunEndpoint("stun.example.com:5349", out var host2, out var port2));
        Assert.AreEqual("stun.example.com", host2);
        Assert.AreEqual(5349, port2);

        Assert.IsFalse(EndpointPresetHelper.TryParseStunEndpoint("", out _, out _));
        Assert.IsFalse(EndpointPresetHelper.TryParseStunEndpoint("stun://stun.fitauto.ru", out _, out _));
        Assert.IsFalse(EndpointPresetHelper.TryParseStunEndpoint("stun.example.com:0", out _, out _));
        Assert.IsFalse(EndpointPresetHelper.TryParseStunEndpoint("stun.example.com:70000", out _, out _));
        Assert.IsFalse(EndpointPresetHelper.TryParseStunEndpoint("stun example.com", out _, out _));
    }

    [TestMethod]
    public void CustomPresetListSupportsAddDedupAndProtectedPresets()
    {
        var presets = new[] { "https://1.1.1.1/dns-query", "udp://223.5.5.5:53" };
        var custom = new List<string> { "https://dns.example.org/dns-query" };

        Assert.IsFalse(EndpointPresetHelper.RecordCustomValue(custom, presets, "https://1.1.1.1/dns-query"));
        Assert.IsFalse(EndpointPresetHelper.RecordCustomValue(custom, presets, "https://DNS.example.org/dns-query"));
        Assert.IsTrue(EndpointPresetHelper.RecordCustomValue(custom, presets, "https://doh.custom.test/dns-query"));

        var options = EndpointPresetHelper.BuildOptions(presets, custom, "https://doh.custom.test/dns-query");
        CollectionAssert.AreEqual(
            new[]
            {
                "https://1.1.1.1/dns-query",
                "udp://223.5.5.5:53",
                "https://dns.example.org/dns-query",
                "https://doh.custom.test/dns-query"
            },
            options);

        Assert.IsFalse(EndpointPresetHelper.IsCustomValue(custom, presets, "https://1.1.1.1/dns-query"));
        Assert.IsTrue(EndpointPresetHelper.IsCustomValue(custom, presets, "https://doh.custom.test/dns-query"));

        // Built-in presets cannot be removed; custom items can.
        Assert.IsFalse(EndpointPresetHelper.RemoveCustomValue(custom, presets, "https://1.1.1.1/dns-query"));
        Assert.IsTrue(EndpointPresetHelper.RemoveCustomValue(custom, presets, "https://DOH.custom.test/dns-query"));
        Assert.AreEqual(1, custom.Count);
    }

    [TestMethod]
    public void DnsEndpointParsesAllSupportedTransportsAndRejectsInvalidOnes()
    {
        Assert.IsTrue(DnsEndpoint.TryParse("223.5.5.5", out var bare));
        Assert.AreEqual(DnsTransport.Udp, bare.Transport);
        Assert.AreEqual(53, bare.Port);
        Assert.IsTrue(bare.IsIpAddress);
        Assert.AreEqual("udp://223.5.5.5:53", bare.Normalized);

        Assert.IsTrue(DnsEndpoint.TryParse("1.1.1.1:5353", out var barePort));
        Assert.AreEqual("udp://1.1.1.1:5353", barePort.Normalized);

        Assert.IsTrue(DnsEndpoint.TryParse("udp://119.29.29.29", out var udp));
        Assert.AreEqual(DnsTransport.Udp, udp.Transport);
        Assert.AreEqual("udp://119.29.29.29:53", udp.Normalized);

        Assert.IsTrue(DnsEndpoint.TryParse("tcp://8.8.8.8", out var tcp));
        Assert.AreEqual(DnsTransport.Tcp, tcp.Transport);
        Assert.AreEqual("tcp://8.8.8.8:53", tcp.Normalized);

        Assert.IsTrue(DnsEndpoint.TryParse("tls://dns.google", out var dot));
        Assert.AreEqual(DnsTransport.Tls, dot.Transport);
        Assert.AreEqual(853, dot.Port);
        Assert.IsFalse(dot.IsIpAddress);
        Assert.AreEqual("tls://dns.google:853", dot.Normalized);

        Assert.IsTrue(DnsEndpoint.TryParse("https://dns.alidns.com/dns-query", out var doh));
        Assert.AreEqual(DnsTransport.Https, doh.Transport);
        Assert.AreEqual("dns.alidns.com", doh.Host);
        Assert.AreEqual(443, doh.Port);
        Assert.AreEqual("/dns-query", doh.PathAndQuery);
        Assert.IsFalse(doh.IsIpAddress);
        Assert.AreEqual("https://dns.alidns.com/dns-query", doh.Normalized);

        Assert.IsTrue(DnsEndpoint.TryParse("https://[2606:4700:4700::1111]/dns-query", out var dohV6));
        Assert.AreEqual("2606:4700:4700::1111", dohV6.Host);
        Assert.IsTrue(dohV6.IsIpAddress);
        Assert.AreEqual("https://[2606:4700:4700::1111]/dns-query", dohV6.Normalized);

        Assert.IsFalse(DnsEndpoint.TryParse("http://1.1.1.1/dns-query", out _));
        Assert.IsFalse(DnsEndpoint.TryParse("dns.google", out _));
        Assert.IsFalse(DnsEndpoint.TryParse("1.1.1.1:0", out _));
        Assert.IsFalse(DnsEndpoint.TryParse("8888", out _));
        Assert.IsFalse(DnsEndpoint.TryParse("1.2", out _));
        Assert.IsFalse(DnsEndpoint.TryParse("8888:53", out _));
        Assert.IsFalse(DnsEndpoint.TryParse("udp://1.1.1.1/dns-query", out _));
        Assert.IsFalse(DnsEndpoint.TryParse("quic://dns.adguard-dns.com", out _));

        Assert.IsTrue(DnsEndpoint.TryParseStrictIpAddress("223.5.5.5", out var strictV4));
        Assert.AreEqual("223.5.5.5", strictV4!.ToString());
        Assert.IsTrue(DnsEndpoint.TryParseStrictIpAddress("2606:4700:4700::1111", out var strictV6));
        Assert.AreEqual("2606:4700:4700::1111", strictV6!.ToString());
        Assert.IsFalse(DnsEndpoint.TryParseStrictIpAddress("8888", out _));
        Assert.IsFalse(DnsEndpoint.TryParseStrictIpAddress("1.2", out _));
        Assert.IsFalse(DnsEndpoint.TryParseStrictIpAddress("8888:53", out _));
    }

    [TestMethod]
    public void CheckSettingMigratesAutoUpdateIntervalAndNormalizesCustomLists()
    {
        var enabledLegacy = new Setting
        {
            CheckUpdateWhenOpened = true,
            AutoCheckUpdateIntervalHours = -1,
            AioDNS = new AioDNSConfig
            {
                ChinaDNS = "https://dns.alidns.com/dns-query",
                OtherDNS = "tls://1.1.1.1",
                CustomChinaDns = ["https://dns.alidns.com/dns-query", "invalid-dns", "https://doh.example.cn/dns-query", "https://DOH.example.cn/dns-query"]
            },
            CustomStunServers = ["stun.fitauto.ru", "stun.custom.test:3478", "stun://bad", "stun.custom.test:5349"],
            CustomDelayTestUrls = ["http://www.gstatic.com/generate_204", "ftp://bad", "https://example.org/204"],
            CustomOutboundDns = ["223.5.5.5", "9.9.9.9", "not-an-ip", "94.140.14.14"]
        };

        Configuration.CheckSetting(enabledLegacy);

        Assert.AreEqual(Constants.DefaultAutoCheckUpdateIntervalHours, enabledLegacy.AutoCheckUpdateIntervalHours);
        Assert.IsTrue(enabledLegacy.CheckUpdateWhenOpened);
        Assert.AreEqual("https://dns.alidns.com/dns-query", enabledLegacy.AioDNS.ChinaDNS);
        Assert.AreEqual("tls://1.1.1.1:853", enabledLegacy.AioDNS.OtherDNS);
        CollectionAssert.AreEqual(new[] { "https://doh.example.cn/dns-query" }, enabledLegacy.AioDNS.CustomChinaDns);
        CollectionAssert.AreEqual(new[] { "stun.custom.test", "stun.custom.test:5349" }, enabledLegacy.CustomStunServers);
        CollectionAssert.AreEqual(new[] { "https://example.org/204" }, enabledLegacy.CustomDelayTestUrls);
        CollectionAssert.AreEqual(new[] { "94.140.14.14" }, enabledLegacy.CustomOutboundDns);

        var disabledLegacy = new Setting
        {
            CheckUpdateWhenOpened = false,
            AutoCheckUpdateIntervalHours = -1
        };
        Configuration.CheckSetting(disabledLegacy);
        Assert.AreEqual(0, disabledLegacy.AutoCheckUpdateIntervalHours);
        Assert.IsFalse(disabledLegacy.CheckUpdateWhenOpened);
    }
}
