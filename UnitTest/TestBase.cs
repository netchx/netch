using System.Text.Json;
using Netch;

namespace UnitTest
{
    public class TestBase
    {
        private readonly JsonSerializerOptions _serializerSettings = new()
        {
            WriteIndented = true,
            IgnoreNullValues = true
        };

        protected TestBase()
        {
#if DEBUG
            Global.Testing = true;
#endif
        }

        protected string JsonSerializerFormatted(object o)
        {
            return JsonSerializer.Serialize(o, _serializerSettings);
        }
    }
}