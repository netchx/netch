namespace Netch.Models
{
    public class Profile
    {
        public int Index;
        public string ModeRemark;
        public string ProfileName;
        public string ServerRemark;

        public Profile(Server server, Mode mode, string name, int index)
        {
            ServerRemark = server.Remark;
            ModeRemark = mode.Remark;
            ProfileName = name;
            Index = index;
        }

        /// <summary>
        ///     Return a dummy one.
        /// </summary>
        public Profile()
        {
        }
    }
}