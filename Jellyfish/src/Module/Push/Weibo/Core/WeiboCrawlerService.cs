using System.Collections.Immutable;
using Jellyfish.Core.Puppeteer;
using Jellyfish.Util;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace Jellyfish.Module.Push.Weibo.Core;

/// <summary>
///     Service for crawling Weibo item
/// </summary>
public class WeiboCrawlerService(BrowserPageFactory pbf, ILogger<WeiboCrawlerService> log)
{
    private const int MaxScanTimes = 3;
    private const int ScanItemLimit = 5;


    /// <summary>
    ///     Crawl Weibo item for user with uid
    /// </summary>
    /// <param name="uid">User's id</param>
    /// <returns>Recent five more Weibo items</returns>
    public async Task<ImmutableArray<WeiboItem>> CrawlAsync(string uid)
    {
        await using var page = pbf.OpenPage().Result;
        List<string> urls = [];
        CatchWeiboUrls(page, urls);

        await page.GoToAsync(Constants.WeiboRootUrl + uid);
        await page.WaitForNetworkIdleAsync();
        await page.WaitForSelectorAsync(Constants.Selectors.Item);

        List<WeiboItem> results = [];

        for (var tryTime = 0; tryTime < MaxScanTimes; tryTime++)
        {
            var items = await CrawlAsync(page, uid);
            if (items.IsEmpty()) continue;
            results.AddRange(items.Where(item => !results.Contains(item)));
            if (items.Count >= ScanItemLimit) break;
        }

        await page.WaitForNetworkIdleAsync();
        await page.CloseAsync();

        // Fill URL
        return [..results.Select((t, i) => t.WithUrl(urls[i]))];
    }

    private static void CatchWeiboUrls(IPage page, List<string> urls)
    {
        var count = 1;
        page.Response += async (_, evt) =>
        {
            if (!evt.Response.Request.Url.Contains("api/container/getIndex")) return;

            if (count is > 1 and < MaxScanTimes)
            {
                var json = JObject.Parse(await evt.Response.TextAsync());
                urls.AddRange(
                    Enumerable
                        .Select<JToken, string>(json.SelectToken("$.data.cards")?
                                .Where(it => it["profile_type_id"]?.ToString().StartsWith("proweibo_") ?? false),
                            it => it["scheme"]?.ToString() ?? "") ?? []
                );
            }

            count++;
        };
    }

    private async Task<List<WeiboItem>> CrawlAsync(IPage page, string uid)
    {
        try
        {
            return await CrawlAsyncAux(page);
        }
        catch (Exception e)
        {
            log.LogError(e, "爬取微博信息时出错，用户 ID：{UID}", uid);
            return [];
        }
    }


    private static async Task<List<WeiboItem>> CrawlAsyncAux(IPage page)
    {
        var list = page.QuerySelectorAllAsync(Constants.Selectors.Item).Result;
        if (list.IsEmpty()) return [];

        List<WeiboItem> results = [];
        foreach (var elm in list)
        {
            var item = await CrawlOnceAsync(elm);
            if (item is null) continue;
            results.Add(item);
        }

        await list.Last().ScrollIntoViewAsync();
        await page.WaitForNetworkIdleAsync();
        return results;
    }

    private static async Task<WeiboItem?> CrawlOnceAsync(IElementHandle elm)
    {
        var isPin = await elm.QuerySelectorAsync(Constants.Selectors.PinTopBadge) is not null;
        if (isPin) return null;

        var contents = await Task.WhenAll(
            ExtractText(elm, Constants.Selectors.Username),
            ExtractText(elm, Constants.Selectors.Content)
        );

        var images = (await Task.WhenAll(
            (await elm.QuerySelectorAllAsync(Constants.Selectors.Image))
            .Select(img => img.EvaluateFunctionAsync<string>("e => e.src"))
        )).Select(url => url
            .Replace("http://", Constants.WeiboPicProxy)
            .Replace("https://", Constants.WeiboPicProxy)
        ).ToArray();

        var content = contents[1]
            // Remove [ZWSP]
            .Replace("\u200b", string.Empty);

        var item = new WeiboItem(
            Username: contents[0],
            Content: content,
            Images: images,
            Url: string.Empty,
            Md5: (contents + string.Empty.Join(images)).ToMd5Hash()
        );

        return item.IsEmpty() ? null : item;
    }

    private static async Task<string> ExtractText(IElementHandle elm, string selector)
    {
        var child = await elm.QuerySelectorAsync(selector);
        return child is null
            ? string.Empty
            : await child.InnerTextAsync();
    }
}
