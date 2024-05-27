using System.Data;

namespace Jellyfish.Core.Config;

public class AppConfig(IConfiguration configuration)
{
    public readonly int KookConnectTimeout = configuration.GetValue<int?>("Kook:ConnectTimeout") ?? 6000;
    public readonly bool KookEnableDebug = configuration.GetValue<bool?>("Kook:EnableDebug") ?? false;

    public readonly string KookToken = configuration.GetValue<string>("Kook:Token")
                                       ?? throw new NoNullAllowedException("请在 appsettings.json 中配置 Token 以连接 Kook 服务");

    public readonly string ChromiumPath = configuration.GetValue<string>("Puppeteer:ChromiumPath")
                                          ?? throw new NoNullAllowedException(
                                              "请在 appsettings.json 中配置 ChromiumPath 以使用依赖浏览器的附加功能");
}
