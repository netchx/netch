using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Netch.Utils
{
    public static class FileHelper
    {
        /// <summary>
        ///     计算文件 SHA256 校验和
        /// </summary>
        /// <param name="name">文件路径</param>
        /// <returns></returns>
        public static byte[] Checksum(string name)
        {
            using (var algo = SHA256.Create())
            {
                using (var fs = File.OpenRead(name))
                {
                    return algo.ComputeHash(fs);
                }
            }
        }

        /// <summary>
        ///     比较两个文件是否完全相同
        /// </summary>
        /// <param name="oPath">文件路径</param>
        /// <param name="nPath">文件路径</param>
        /// <returns></returns>
        public static bool Equals(string oPath, string nPath) => Checksum(oPath).SequenceEqual(Checksum(nPath));
    }
}
