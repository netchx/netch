using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Tests
{
    [TestClass]
    public class Global
    {
        [TestMethod]
        public void Test()
        {
            Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
        }
        [TestMethod]
        public void VLESS_UUID5()
        {
            //https://github.com/XTLS/Xray-core/discussions/715

            byte[] bytes = new byte[16];
            var str = "example";

            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] StrBytes = Encoding.UTF8.GetBytes(str);

            List<byte> byteSource = new List<byte>();
            byteSource.AddRange(bytes);
            byteSource.AddRange(StrBytes);

            byte[] Sha1Bytes = sha1.ComputeHash(byteSource.ToArray()).Skip(0).Take(16).ToArray();
            sha1.Dispose();

            //UUIDv5: [254 181 68 49 48 27 82 187 166 221 225 233 62 129 187 158]

            Sha1Bytes[6] = (byte)((Sha1Bytes[6] & 0x0f) | (5 << 4));
            Sha1Bytes[8] = (byte)(Sha1Bytes[8] & (0xff >> 2) | (0x02 << 6));

            var result = BitConverter.ToString(Sha1Bytes).Replace("-", "").Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-").ToLower();
            Console.WriteLine(result);
            //UUIDv5: feb54431-301b-52bb-a6dd-e1e93e81bb9e
        }
    }
}
