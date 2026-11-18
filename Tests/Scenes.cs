using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Models;
using Netch.Models.Modes.ProcessMode;
using Netch.Models.Modes.TunMode;
using Netch.Servers;
using Netch.Services;
using Netch.Utils;

namespace Tests;

[TestClass]
public class Scenes
{
    private Setting _savedSettings;

    [TestInitialize]
    public void SaveGlobals() => _savedSettings = Netch.Global.Settings;

    [TestCleanup]
    public void RestoreGlobals()
    {
        Netch.Global.Settings = _savedSettings;
        Netch.Global.Modes.Clear();
    }

    [TestMethod]
    public void ProfilesBecomeScenesOnce()
    {
        var hk = new SocksServer { Remarks = "HK 01", Address = "hk.example", Port = 1080 };
        var game = new Redirector { FullName = Path.Combine(ModeService.Instance.ModeDirectoryFullName, "Game", "Steam.json") };
        game.Remark["en"] = "Steam";
        var global = new TunMode { FullName = Path.Combine(ModeService.Instance.ModeDirectoryFullName, "Global.json") };
        global.Remark["zh-CN"] = "全局";
        Netch.Global.Modes.AddRange(new Netch.Models.Modes.Mode[] { game, global });

        Netch.Global.Settings = new Setting
        {
            Server = { hk },
            Profiles =
            {
                new Profile { Index = 1, ProfileName = "", ServerRemark = "HK 01", ModeRemark = "全局" },
                new Profile { Index = 0, ProfileName = "Games", ServerRemark = "HK 01", ModeRemark = "Steam" },
                new Profile { Index = 2, ProfileName = "Gone", ServerRemark = "removed node", ModeRemark = "Steam" }
            }
        };

        Assert.IsTrue(SceneService.Initialize());
        var scenes = SceneService.Scenes;

        CollectionAssert.AreEqual(new[] { "Games", "全局", "Gone" }, scenes.Select(s => s.Name).ToArray());
        Assert.AreEqual(@"Game\Steam.json", scenes[0].ModeId);
        Assert.AreSame(game, SceneService.FindMode(scenes[0]));
        Assert.AreSame(hk, SceneService.FindServer(scenes[0]));
        Assert.AreSame(global, SceneService.FindMode(scenes[1]));
        Assert.AreEqual("🌐", scenes[1].Icon);
        Assert.IsNull(SceneService.FindServer(scenes[2]), "a profile whose node is gone keeps its scene, without a node");

        scenes.Clear();
        Assert.IsFalse(SceneService.Initialize(), "deleting every scene must not bring the profiles back");
    }

    [TestMethod]
    public void SubscriptionUpdateKeepsNodeIds()
    {
        SocksServer Node(string remarks, string address, int port = 443) => new() { Remarks = remarks, Address = address, Port = port };

        var renamed = Node("HK 01", "hk.example");
        var twinA = Node("CDN A", "cdn.example");
        var twinB = Node("CDN B", "cdn.example");
        var dropped = Node("JP 01", "jp.example");
        var previous = new List<Server> { renamed, twinA, twinB, dropped };

        var update = new List<Server>
        {
            Node("CDN B", "cdn.example"),
            Node("Hong Kong 01", "HK.example"),
            Node("CDN A", "cdn.example"),
            Node("SG 01", "sg.example")
        };

        SubscriptionUtil.InheritIds(previous, update);

        Assert.AreEqual(twinB.Id, update[0].Id, "same endpoint twice: matched by name");
        Assert.AreEqual(renamed.Id, update[1].Id, "renamed node: matched by protocol, address and port");
        Assert.AreEqual(twinA.Id, update[2].Id);
        Assert.IsFalse(new[] { renamed.Id, twinA.Id, twinB.Id, dropped.Id }.Contains(update[3].Id), "new node keeps a new id");
    }
}
