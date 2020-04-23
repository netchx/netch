namespace Netch.Models
{
    public class Profile
    {
        public string ServerRemark;
        public string ModeRemark;
        public string ProfileName;

        public bool IsDummy = true;

        public Profile(Server server, Mode mode, string name)
        {
            ServerRemark = server.Remark;
            ModeRemark = mode.Remark;
            ProfileName = name;
            IsDummy = false;
        }

        /// <summary>
        /// Return a dummy one.
        /// </summary>
        public Profile()
        {
        }


    }
}
