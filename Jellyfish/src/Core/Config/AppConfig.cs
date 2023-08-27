using System.Configuration;

namespace Jellyfish.Core.Config;

public class AppConfig
{
    public readonly int KookConnectTimeout;
    public readonly bool KookEnableDebug;
    public readonly string KookToken;

    public AppConfig()
    {
        KookToken = ConfigurationManager.ConnectionStrings["KookToken"].ConnectionString;
        KookEnableDebug = bool.Parse(ConfigurationManager.AppSettings["KookEnableDebug"] ?? "false");
        KookConnectTimeout = int.Parse(ConfigurationManager.AppSettings["KookConnectTimeout"] ?? "6000");
        if (string.IsNullOrEmpty(KookToken))
            throw new ArgumentNullException(nameof(KookToken), "请在 App.config 中配置 KookToken 以连接 Kook 服务");
    }
}
