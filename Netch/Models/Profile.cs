namespace Netch.Models
{
    public class Profile
    {
        public string ServerRemark;
        public string ModeRemark;
        public string ProfileName;
        public int Index;

        public Profile(Server server, Mode mode, string name,int index)
        {
            ServerRemark = server.Remark;
            ModeRemark = mode.Remark;
            ProfileName = name;
            Index = index;
        }

        /// <summary>
        /// Return a dummy one.
        /// </summary>
        public Profile()
        {
        }


    }
}
