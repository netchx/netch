using Netch;

namespace UnitTest
{
    public class TestBase
    {
        protected TestBase()
        {
#if DEBUG
            Global.Testing = true;
#endif
        }
    }
}