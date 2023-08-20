using System.Configuration;

namespace Jellyfish.Config;

public class AppConfig
{
    public readonly string KookToken;

    public AppConfig()
    {
        KookToken = ConfigurationManager.ConnectionStrings["KookToken"].ConnectionString;
        if (string.IsNullOrEmpty(KookToken))
            throw new ArgumentNullException(nameof(KookToken), "请在 App.config 中配置 KookToken 以连接 Kook 服务");
    }
}
