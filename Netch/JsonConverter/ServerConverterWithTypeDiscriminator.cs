using System.Text.Json;
using System.Text.Json.Serialization;
using Netch.Models;
using Netch.Utils;

namespace Netch.JsonConverter;

public class ServerConverterWithTypeDiscriminator : JsonConverter<Server>
{
    public override Server Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(ref reader);
        var type = ServerHelper.GetTypeByTypeName(jsonElement.GetProperty("Type").GetString()!);
        return (Server)jsonElement.Deserialize(type)!;
    }

    public override void Write(Utf8JsonWriter writer, Server value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize<object>(writer, value, options);
    }
}