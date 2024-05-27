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
    ///     Open new browser page with the resolution ratio is 1920x1080
    ///     Retry when browser crashes
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
                    log.LogWarning("浏览器已崩溃，正在重启进程（第{Times} 次)", retry.AttemptNumber);
                    _browser = await GetBrowserProcess();
                    log.LogWarning("浏览器进程已重启");
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
    private async Task<IBrowser> GetBrowserProcess()
    {
        var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new LaunchOptions
        {
            ExecutablePath = config.ChromiumPath,
            // Hide the Chromium window
            Headless = true,
            Browser = SupportedBrowser.Chromium,
            // Use the Chinese language as browser default language
            Args = ["--no-sandbox", "--accept-lang=zh-CN"]
        });
        log.LogInformation("浏览器进程已启动，PID：{Pid}", browser.Process.Id);
        return browser;
    }
}
