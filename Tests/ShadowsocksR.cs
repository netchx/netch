using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.JsonConverter;
using Netch.Models;
using Netch.Servers;
using Netch.Utils;

namespace Tests;

[TestClass]
public class ShadowsocksR
{
    [TestMethod]
    public void SavedNodeKeepsItsTypeAndFieldsThroughSubscriptionImport()
    {
        const string saved = """
            {
              "ConfigType": 2,
              "Address": "2001:db8::1",
              "Port": 443,
              "Password": "local-test",
              "EncryptMethod": "aes-256-cfb",
              "Protocol": "auth_chain_a",
              "ProtocolParam": "10:local-test",
              "OBFS": "http_simple",
              "OBFSParam": "example.test",
              "Remarks": "SSR sample",
              "Group": "Subscription"
            }
            """;
        var options = new JsonSerializerOptions { Converters = { new ServerConverterWithTypeDiscriminator() } };
        var node = (ShadowsocksRServer)JsonSerializer.Deserialize<Server>(saved, options);
        var link = ShareLink.GetShareLink(node);
        var subscription = ShareLink.URLSafeBase64Encode(link);
        var imported = ShareLink.ParseText(subscription).Single();

        Assert.AreEqual(JsonSerializer.Serialize<Server>(node, options), JsonSerializer.Serialize(imported, options));
    }
}
