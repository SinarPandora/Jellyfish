using System.Collections.Immutable;
using Jellyfish.Core.Puppeteer;
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
        await using var page = pbf.OpenPage(Constants.WeiboRootUrl + uid).Result;
        await page.WaitForNetworkIdleAsync();
        await page.WaitForSelectorAsync(Constants.Selectors.Item);

        List<WeiboItem> results = [];

        for (var tryTime = 0; tryTime < MaxScanTimes; tryTime++)
        {
            var items = await CrawlAsync(page, uid);
            if (items.IsEmpty()) break;
            results.AddRange(items.Where(item => !results.Contains(item)));
            if (items.Count >= ScanItemLimit) break;
        }

        await page.CloseAsync();
        return [..results];
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

        var expandBtn = await elm.QuerySelectorAsync(Constants.Selectors.ExpandBtn);
        if (expandBtn is not null)
        {
            await expandBtn.ClickAsync();
            await elm.WaitForSelectorAsync(Constants.Selectors.CollapseBtn);
        }

        var contents = await Task.WhenAll(
            ExtractText(elm, Constants.ClassNames.Username),
            ExtractText(elm, Constants.ClassNames.Title),
            ExtractText(elm, Constants.ClassNames.Content)
        );

        var images = await Task.WhenAll(
            (await elm.QuerySelectorAllAsync(Constants.Selectors.Image))
            .Select(img => img.EvaluateFunctionAsync<string>("e => e.src"))
        );

        var item = new WeiboItem(
            Username: contents[0],
            Time: contents[1],
            Content: expandBtn is null ? contents[2] : contents[2][..^2],
            Images: images
        );

        return item.IsEmpty() ? null : item;
    }

    private static async Task<string> ExtractText(IElementHandle elm, string cssName)
    {
        var child = await elm.QuerySelectorAsync($"[class*='{cssName}']");
        return child is null
            ? string.Empty
            : await child.InnerTextAsync();
    }
}
