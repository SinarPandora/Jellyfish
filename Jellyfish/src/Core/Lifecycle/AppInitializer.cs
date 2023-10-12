using Jellyfish.Core.Cache;
using Jellyfish.Core.Job;
using Jellyfish.Core.Kook;

namespace Jellyfish.Core.Lifecycle;

/// <summary>
///     App initializer
/// </summary>
public class AppInitializer : IStartupFilter
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AppInitializer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<CacheLoader>().Load().Wait();
                scope.ServiceProvider.GetRequiredService<KookLoader>().Load().Wait();
                scope.ServiceProvider.GetRequiredService<JobLoader>().Load();
            }

            next(builder);
        };
    }
}
