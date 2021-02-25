using Netch;
using Newtonsoft.Json;

namespace UnitTest
{
    public class TestBase
    {
        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        protected TestBase()
        {
#if DEBUG
            Global.Testing = true;
#endif
        }

        protected string JsonSerializerFormatted(object o)
        {
            return JsonConvert.SerializeObject(o, _serializerSettings);
        }
    }
}