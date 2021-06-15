using System.Collections.Generic;

namespace Netch.Utils
{
    public static class GitHub
    {
        /// <summary>
        ///     地址
        /// </summary>
        public static readonly string URL = "https://api.github.com/repos/NetchX/Netch/releases";

        /// <summary>
        ///     检查是否有更新
        /// </summary>
        /// <returns></returns>
        public static int HasUpdate()
        {
            var list = GetReleaseList();
            if (list.Count < 1)
            {
                return 0;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Draft)
                {
                    continue;
                }

                if (Global.Config.Generic.Unstable)
                {
                    if (list[i].Unstable)
                    {
                        continue;
                    }
                }

                if (list[i].VerCode.Equals(Global.VerCode))
                {
                    return 0;
                }

                return list[i].ID;
            }

            return 0;
        }

        /// <summary>
        ///     获取单个发布
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Models.GitHub.Release GetRelease(int id)
        {
            var data = HTTP.GetString($"{URL}/{id}");

            return Newtonsoft.Json.JsonConvert.DeserializeObject<Models.GitHub.Release>(data);
        }

        /// <summary>
        ///     获取发布列表
        /// </summary>
        /// <returns></returns>
        public static List<Models.GitHub.Release> GetReleaseList()
        {
            var data = HTTP.GetString(URL);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<Models.GitHub.Release>>(data);
        }
    }
}
