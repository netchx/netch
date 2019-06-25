using System.Collections.Generic;

namespace Netch.Objects
{
    public class Mode
    {
        /// <summary>
        ///		备注
        /// </summary>
        public string Remark;

        /// <summary>
        ///		规则
        /// </summary>
        public List<string> Rule = new List<string>();

        /// <summary>
        ///		获取备注
        /// </summary>
        /// <returns>备注</returns>
        public override string ToString()
        {
            return Remark;
        }
    }
}
