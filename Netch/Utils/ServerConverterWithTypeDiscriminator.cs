using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Netch.Models;

namespace Netch.Utils
{
    public class ServerConverterWithTypeDiscriminator : JsonConverter<Server>
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Server);

        public override Server Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(ref reader)!;

            try
            {
                var type = ServerHelper.GetTypeByTypeName(jsonElement.GetProperty("Type").GetString()!);
                return (Server) JsonSerializer.Deserialize(jsonElement.GetRawText(), type)!;
            }
            catch
            {
                return JsonSerializer.Deserialize<Server>(jsonElement.GetRawText(), new JsonSerializerOptions())!;
            }
        }

        public override void Write(Utf8JsonWriter writer, Server value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize<object>(writer, value, options);
        }
    }
}