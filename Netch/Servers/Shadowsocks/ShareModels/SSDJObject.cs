#nullable disable
namespace Netch.Servers;

public class SSDJObject
{
    /// <summary>
    ///     机场名
    /// </summary>
    public string airport;

    /// <summary>
    ///     加密方式
    /// </summary>
    public string encryption;

    /// <summary>
    ///     密码
    /// </summary>
    public string password;

    /// <summary>
    ///     插件
    /// </summary>
    public string plugin;

    /// <summary>
    ///     插件参数
    /// </summary>
    public string plugin_options;

    /// <summary>
    ///     端口
    /// </summary>
    public ushort port;

    /// <summary>
    ///     服务器数组
    /// </summary>
    public List<SSDServerJObject> servers;
}