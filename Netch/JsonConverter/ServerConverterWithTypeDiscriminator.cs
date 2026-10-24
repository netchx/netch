using System.Text.Json;
using System.Text.Json.Serialization;
using Netch.Enums;
using Netch.Models;
using Netch.Utils;

namespace Netch.JsonConverter;

public class ServerConverterWithTypeDiscriminator : JsonConverter<Server>
{
    public override Server Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(ref reader);
        var configType = jsonElement.GetProperty("ConfigType");
        var typeName = configType.ValueKind switch
        {
            JsonValueKind.String => configType.GetString(),
            JsonValueKind.Number when configType.TryGetInt32(out var value) => Enum.GetName(typeof(EConfigType), value),
            _ => null
        };
        if (typeName.IsNullOrWhiteSpace())
            throw new JsonException("Server ConfigType is missing or invalid.");

        var type = ServerHelper.GetTypeByTypeName(typeName);
        return (Server)jsonElement.Deserialize(type)!;
    }

    public override void Write(Utf8JsonWriter writer, Server value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize<object>(writer, value, options);
    }
}
