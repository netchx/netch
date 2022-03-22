using System.Text.Json;

namespace Netch.Models;

/// <summary>
///     用于读取和写入的配置的类
/// </summary>
public class Setting
{
    public RedirectorConfig Redirector { get; set; } = new();

    /// <summary>
    ///     服务器列表
    /// </summary>
    public List<Server> Server { get; set; } = new();

    public AioDNSConfig AioDNS { get; set; } = new();

    /// <summary>
    ///     是否检查 Beta 更新
    /// </summary>
    public bool CheckBetaUpdate { get; set; } = false;

    /// <summary>
    ///     是否打开软件时检查更新
    /// </summary>
    public bool CheckUpdateWhenOpened { get; set; } = true;

    /// <summary>
    ///     测试所有服务器心跳/秒
    /// </summary>
    public int DetectionTick { get; set; } = 10;

    /// <summary>
    ///     是否关闭窗口时退出
    /// </summary>
    public bool ExitWhenClosed { get; set; } = false;

    /// <summary>
    ///     HTTP 本地端口
    /// </summary>
    public ushort HTTPLocalPort { get; set; } = 2802;

    /// <summary>
    ///     语言设置
    /// </summary>
    public string Language { get; set; } = "System";

    /// <summary>
    ///     HTTP 和 Socks5 本地代理地址
    /// </summary>
    public string LocalAddress { get; set; } = "127.0.0.1";

    /// <summary>
    ///     是否启动后自动最小化
    /// </summary>
    public bool MinimizeWhenStarted { get; set; } = false;

    /// <summary>
    ///     模式选择位置
    /// </summary>
    public int ModeComboBoxSelectedIndex { get; set; } = -1;

    /// <summary>
    ///     快捷配置数量
    /// </summary>
    public int ProfileCount { get; set; } = 4;

    /// <summary>
    ///     已保存的快捷配置
    /// </summary>
    public List<Profile> Profiles { get; set; } = new();

    /// <summary>
    ///     配置最大列数
    /// </summary>
    public byte ProfileTableColumnCount { get; set; } = 5;

    /// <summary>
    ///     网页请求超时 毫秒
    /// </summary>
    public int RequestTimeout { get; set; } = 10000;

    /// <summary>
    ///     是否开机启动软件
    /// </summary>
    public bool RunAtStartup { get; set; } = false;

    /// <summary>
    ///     服务器选择位置
    /// </summary>
    public int ServerComboBoxSelectedIndex { get; set; } = -1;

    /// <summary>
    ///     服务器测试方式 false.ICMPing true.TCPing
    /// </summary>
    public bool ServerTCPing { get; set; } = true;

    /// <summary>
    ///     Socks5 本地端口
    /// </summary>
    public ushort Socks5LocalPort { get; set; } = 2801;

    /// <summary>
    ///     启动后延迟测试间隔/秒
    /// </summary>
    public int StartedPingInterval { get; set; } = -1;

    /// <summary>
    ///     是否打开软件时启动加速
    /// </summary>
    public bool StartWhenOpened { get; set; } = false;

    /// <summary>
    ///     是否退出时停止
    /// </summary>
    public bool StopWhenExited { get; set; } = false;

    /// <summary>
    ///     STUN测试服务器
    /// </summary>
    public string STUN_Server { get; set; } = "stun.syncthing.net";

    /// <summary>
    ///     STUN测试服务器
    /// </summary>
    public int STUN_Server_Port { get; set; } = 3478;

    /// <summary>
    ///     订阅链接列表
    /// </summary>
    public List<Subscription> Subscription { get; set; } = new();

    /// <summary>
    ///     TUNTAP 适配器配置
    /// </summary>
    public TUNConfig TUNTAP { get; set; } = new();

    /// <summary>
    ///     是否打开软件时更新订阅
    /// </summary>
    public bool UpdateServersWhenOpened { get; set; } = false;

    public V2rayConfig V2RayConfig { get; set; } = new();

    public bool NoSupportDialog { get; set; } = false;

    #region Migration

    [Obsolete]
    public JsonElement SubscribeLink
    {
        set
        {
            if (Subscription == null! || !Subscription.Any())
                Subscription = value.Deserialize<List<Subscription>>()!;
        }
    }

    #endregion

    public Setting ShallowCopy()
    {
        return (Setting)MemberwiseClone();
    }

    public void Set(Setting value)
    {
        foreach (var p in typeof(Setting).GetProperties())
            p.SetValue(this, p.GetValue(value));
    }
}