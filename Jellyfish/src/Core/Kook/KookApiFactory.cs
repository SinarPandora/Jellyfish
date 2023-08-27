using Jellyfish.Core.Config;
using Kook;
using Kook.Rest;

namespace Jellyfish.Core.Kook;

public class KookApiFactory
{
    private readonly AppConfig _appConfig;

    public KookApiFactory(AppConfig config)
    {
        _appConfig = config;
    }

    /// <summary>
    ///     Kook Restful API Client
    /// </summary>
    /// <returns>Logged-in API Client</returns>
    public async Task<KookRestClient> CreateApiClient()
    {
        var apiClient = new KookRestClient();
        await apiClient.LoginAsync(TokenType.Bot, _appConfig.KookToken);
        return apiClient;
    }
}
