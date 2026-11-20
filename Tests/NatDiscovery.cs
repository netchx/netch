using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Utils;
using STUN;
using STUN.Enums;
using STUN.StunResult;

namespace Tests;

[TestClass]
public class NatDiscovery
{
    [DataTestMethod]
    [DataRow(BindingTestResult.Success, MappingBehavior.EndpointIndependent, FilteringBehavior.EndpointIndependent, "1")]
    [DataRow(BindingTestResult.Success, MappingBehavior.EndpointIndependent, FilteringBehavior.AddressAndPortDependent, "2")]
    [DataRow(BindingTestResult.Success, MappingBehavior.AddressAndPortDependent, FilteringBehavior.AddressDependent, "3")]
    [DataRow(BindingTestResult.Success, MappingBehavior.EndpointIndependent, FilteringBehavior.UnsupportedServer, "UnsupportedServer")]
    [DataRow(BindingTestResult.Success, MappingBehavior.EndpointIndependent, FilteringBehavior.Unknown, "Incomplete")]
    [DataRow(BindingTestResult.Success, MappingBehavior.Fail, FilteringBehavior.EndpointIndependent, "Incomplete")]
    [DataRow(BindingTestResult.Fail, MappingBehavior.Unknown, FilteringBehavior.Unknown, "NoUDP")]
    public void OnlyCompletedProbesProduceANumericType(BindingTestResult binding, MappingBehavior mapping, FilteringBehavior filtering, string expected)
    {
        var method = typeof(Socks5ServerTestUtils).GetMethod("GetSimpleResult", BindingFlags.NonPublic | BindingFlags.Static);
        var state = new StunResult5389 { BindingTestResult = binding, MappingBehavior = mapping, FilteringBehavior = filtering };
        Assert.AreEqual(expected, method.Invoke(null, [state]));
    }

    [DataTestMethod]
    [DataRow("stun.fitauto.ru", "stun.fitauto.ru,stun.m-online.net")]
    [DataRow("stun.syncthing.net", "stun.syncthing.net,stun.fitauto.ru,stun.m-online.net")]
    [DataRow("stun.example.test:5349", "stun.example.test:5349")]
    public void PresetsCanFallBackButCustomServersAreTestedAlone(string address, string expected)
    {
        Assert.IsTrue(StunServer.TryParse(address, out var selected));
        Assert.IsTrue(StunServer.TryParse("stun.fitauto.ru", out var first));
        Assert.IsTrue(StunServer.TryParse("stun.m-online.net", out var second));
        var method = typeof(Socks5ServerTestUtils).GetMethod("GetCandidates", BindingFlags.NonPublic | BindingFlags.Static);
        var candidates = (IReadOnlyList<StunServer>)method.Invoke(null, [selected, new[] { first, second }]);
        Assert.AreEqual(expected, string.Join(",", candidates.Select(server => server.ToString())));
    }
}
