using Jellyfish.Core.Config;
using Jellyfish.Util;
using Polly;
using Polly.Retry;
using PuppeteerSharp;

namespace Jellyfish.Core.Puppeteer;

/// <summary>
///     Browser page factory for Puppeteer Chromium
/// </summary>
public class BrowserPageFactory(AppConfig config, ILogger<BrowserPageFactory> log)
{
    private IBrowser? _browser;

    /// <summary>
    ///     Open new browser page with resolution ratio is 1920x1080
    ///     Retry when brower crashes
    /// </summary>
    /// <returns>New browser page</returns>
    public async Task<IPage> OpenPage()
    {
        _browser ??= await GetBrowserProcess();

        return await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<PuppeteerException>(),
                MaxRetryAttempts = 2,
                DelayGenerator = PollyHelper.DefaultProgressiveDelayGenerator,
                OnRetry = async retry =>
                {
                    log.LogWarning("Browser process crashed, retrying {Times} time(s)...", retry.AttemptNumber);
                    _browser = await GetBrowserProcess();
                }
            })
            .Build()
            .ExecuteAsync(async _ =>
            {
                var page = await _browser.NewPageAsync();
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = 1920,
                    Height = 1080
                });
                return page;
            });
    }

    /// <summary>
    ///     Internal method to start a new browser process
    /// </summary>
    /// <returns>New browser process</returns>
    private Task<IBrowser> GetBrowserProcess()
    {
        return PuppeteerSharp.Puppeteer.LaunchAsync(new LaunchOptions
        {
            ExecutablePath = config.ChromiumPath,
            // Hide the Chromium window
            Headless = true,
            Browser = SupportedBrowser.Chromium,
            // Use the Chinese language as browser default language
            Args = ["--accept-lang=zh-CN"]
        });
    }
}
