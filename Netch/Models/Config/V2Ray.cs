namespace Netch.Models.Config
{
    public class V2Ray
    {
        /// <summary>
        ///     跳过证书认证
        /// </summary>
        public bool Insecure = false;

        /// <summary>
        ///     多路复用
        /// </summary>
        public bool Multiplex = false;

        /// <summary>
        ///     KCP 设定
        /// </summary>
        public V2RayKCP KCP = new();
    }

    public class V2RayKCP
    {
        /// <summary>
        ///     MTU
        /// </summary>
        public int MTU = 1450;

        /// <summary>
        ///     TTI
        /// </summary>
        public int TTI = 50;

        /// <summary>
        ///     上行链路流量
        /// </summary>
        public int UPC = 5;

        /// <summary>
        ///     下行链路流量
        /// </summary>
        public int DLC = 20;

        /// <summary>
        ///     读取缓冲区大小（MB）
        /// </summary>
        public int RBS = 2;

        /// <summary>
        ///     写入缓冲区大小（MB）
        /// </summary>
        public int WBS = 2;

        /// <summary>
        ///     拥塞控制
        /// </summary>
        public bool BBR = false;
    }
}
