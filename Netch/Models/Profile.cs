using System;

namespace Netch.Models
{
    public class Profile
	{
        public String ServerRemark;
        public String ModeRemark;
        public String ProfileName;

        public bool IsDummy = true;

        public Profile(Server server, Mode mode, String name)
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
