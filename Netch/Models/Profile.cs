namespace Netch.Models
{
    public class Profile
    {
        public int Index { get; set; }

        public string ModeRemark { get; set; }

        public string ProfileName { get; set; }

        public string ServerRemark { get; set; }

        public Profile(Server server, Mode mode, string name, int index)
        {
            ServerRemark = server.Remark;
            ModeRemark = mode.Remark;
            ProfileName = name;
            Index = index;
        }

        public Profile()
        {
            ServerRemark = string.Empty;
            ModeRemark = string.Empty;
            ProfileName = string.Empty;
            Index = 0;
        }
    }
}