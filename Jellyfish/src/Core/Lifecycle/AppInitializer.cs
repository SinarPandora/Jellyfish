using Jellyfish.Core.Cache;
using Jellyfish.Core.Job;
using Jellyfish.Core.Kook;
using Jellyfish.Core.Puppeteer;

namespace Jellyfish.Core.Lifecycle;

/// <summary>
///     App initializer
/// </summary>
public class AppInitializer(IServiceScopeFactory scopeFactory) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            using (var scope = scopeFactory.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<CacheLoader>().Load().Wait();
                scope.ServiceProvider.GetRequiredService<KookLoader>().Login().Wait();
                scope.ServiceProvider.GetRequiredService<JobLoader>().Load();
                // Warm-up Chromium browser
                scope
                    .ServiceProvider.GetRequiredService<BrowserPageFactory>()
                    .OpenPage("about:blank")
                    .Result.Dispose();
            }

            next(builder);
        };
    }
}
