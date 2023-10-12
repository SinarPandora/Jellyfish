using System.Data;

namespace Jellyfish.Core.Config;

public class AppConfig
{
    public readonly int KookConnectTimeout;
    public readonly bool KookEnableDebug;
    public readonly string KookToken;

    public AppConfig(IConfiguration configuration)
    {
        KookToken = configuration.GetValue<string>("Kook:Token") ??
                    throw new NoNullAllowedException("请在 appsettings.json 中配置 Token 以连接 Kook 服务");
        KookEnableDebug = configuration.GetValue<bool?>("Kook:EnableDebug") ?? false;
        KookConnectTimeout = configuration.GetValue<int?>("Kook:ConnectTimeout") ?? 6000;
    }
}
