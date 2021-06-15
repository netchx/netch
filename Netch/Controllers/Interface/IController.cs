namespace Netch.Controllers.Interface
{
    public interface IController
    {
        bool Create(Models.Server.Server s, Models.Mode.Mode m);

        bool Delete();
    }
}
