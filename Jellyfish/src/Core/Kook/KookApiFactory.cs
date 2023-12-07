using Jellyfish.Core.Config;
using Kook;
using Kook.Rest;

namespace Jellyfish.Core.Kook;

public class KookApiFactory(AppConfig config)
{
    /// <summary>
    ///     Kook Restful API Client
    /// </summary>
    /// <returns>Logged-in API Client</returns>
    public async Task<KookRestClient> CreateApiClient()
    {
        var apiClient = new KookRestClient();
        await apiClient.LoginAsync(TokenType.Bot, config.KookToken);
        return apiClient;
    }
}
