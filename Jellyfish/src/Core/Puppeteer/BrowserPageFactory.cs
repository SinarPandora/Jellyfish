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
    /// <param name="url">Page url</param>
    /// <returns>New browser page</returns>
    public async Task<IPage> OpenPage(string? url = null)
    {
        _browser ??= await GetBrowserProcess();

        IPage? page = null;
        return await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<PuppeteerException>(),
                MaxRetryAttempts = 2,
                DelayGenerator = PollyHelper.DefaultProgressiveDelayGenerator,
                OnRetry = async retry =>
                {
                    if (retry.Outcome.Exception is NavigationException)
                    {
                        log.LogWarning("访问超时，正在重试（地址：{Url}，第 {Times} 次)", url, retry.AttemptNumber);
                        if (page is { IsClosed: false })
                        {
                            try
                            {
                                await page.CloseAsync();
                            }
                            catch (Exception)
                            {
                                // Ignore all errors
                            }
                        }
                    }
                    else
                    {
                        log.LogWarning("浏览器已崩溃，正在重启进程（错误：{Err}，第 {Times} 次)",
                            retry.Outcome.Exception?.GetType().Name ?? "未知错误",
                            retry.AttemptNumber
                        );
                        _browser = await GetBrowserProcess();
                        log.LogWarning("浏览器进程已重启");
                    }
                }
            })
            .Build()
            .ExecuteAsync(async _ =>
            {
                page = await _browser.NewPageAsync();
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = 1920,
                    Height = 1080
                });

                if (url is not null)
                {
                    await page.GoToAsync(url);
                }

                return page;
            });
    }

    /// <summary>
    ///     Internal method to start a new browser process
    /// </summary>
    /// <returns>New browser process</returns>
    private async Task<IBrowser> GetBrowserProcess()
    {
        if (_browser is { IsClosed: false })
        {
            try
            {
                await _browser.CloseAsync();
            }
            catch (Exception)
            {
                // Ignore all errors
            }
        }

        var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new LaunchOptions
        {
            ExecutablePath = config.ChromiumPath,
            // Hide the Chromium window
            Headless = true,
            Browser = SupportedBrowser.Chromium,
            Args = config.ChromiumArgs
        });
        log.LogInformation("浏览器进程已启动，PID：{Pid}", browser.Process.Id);
        return browser;
    }
}
