using System.Collections.Immutable;
using Jellyfish.Core.Puppeteer;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace Jellyfish.Module.Push.Weibo.Core;

/// <summary>
///     Service for crawling Weibo item
/// </summary>
public class WeiboCrawlerService(BrowserPageFactory pbf, ILogger<WeiboCrawlerService> log)
{
    private static readonly JArray EmptyJsonArray = [];


    /// <summary>
    ///     Crawl Weibo item for user with uid
    /// </summary>
    /// <param name="uid">User's id</param>
    /// <returns>Recent five more Weibo items</returns>
    public async Task<ImmutableArray<WeiboItem>> CrawlAsync(string uid)
    {
        await using var page = pbf.OpenPage(ua: "Baiduspider+(+http://www.baidu.com/search/spider.htm)").Result;
        List<WeiboMetadata> metadataList = [];
        CatchWeiboMetadata(page, metadataList);

        await page.GoToAsync(Constants.WeiboRootUrl + uid);
        await page.WaitForNetworkIdleAsync();
        await page.WaitForSelectorAsync(Constants.Selectors.Item);
        var results = await CrawlAsync(page, uid);
        log.LogInformation("捕获到元数据{Length}条，UID：{UID}", metadataList.Count, uid);
        log.LogInformation("爬取到微博{Length}条，UID：{UID}", results.Count, uid);
        await page.CloseAsync();

        if (metadataList.Count < results.Count)
        {
            log.LogWarning("微博元数据数量小于微博数量，跳过此次捕获，若此问题频繁发生，请上报该 bug");
            return [];
        }

        var items = new List<WeiboItem>();
        for (var i = 0; i < results.Count; i++)
        {
            var content = results[i];
            var metadata = metadataList[i];
            if (metadata.IsTop) continue;
            items.Add(WeiboItem.Combine(metadata, content));
        }

        // Keep latest at the bottom
        items.Reverse();
        return [..items];
    }

    private static void CatchWeiboMetadata(IPage page, List<WeiboMetadata> metadataList)
    {
        var count = 1;
        page.Response += async (_, evt) =>
        {
            if (!evt.Response.Request.Url.Contains("api/container/getIndex") || count > 2) return;

            if (count == 2)
            {
                var json = JObject.Parse(await evt.Response.TextAsync());
                metadataList.AddRange(
                    from weibo in json.SelectToken("$.data.cards") ?? EmptyJsonArray
                    where (weibo["card_type"]?.Value<int>() ?? 0) == 9
                    select new WeiboMetadata(
                        Mid: weibo["mblog"]["mid"]!.ToString(),
                        IsTop: (weibo["profile_type_id"]?.Value<string>() ?? string.Empty) == "proweibotop_"
                    )
                );
            }

            count++;
        };
    }

    private async Task<List<WeiboContent>> CrawlAsync(IPage page, string uid)
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


    private async Task<List<WeiboContent>> CrawlAsyncAux(IPage page)
    {
        var list = page.QuerySelectorAllAsync(Constants.Selectors.Item).Result;
        if (list.IsEmpty()) return [];

        List<WeiboContent> results = [];
        foreach (var elm in list)
        {
            var secondLink = (await elm.EvaluateFunctionAsync<string[]>(
                    "e => [...e.querySelectorAll('a')].filter(e => e.innerText === '全文').map(e => e.href)"
                ))
                .FirstOrDefault();
            var item = secondLink is not null
                // The content is incomplete and should be deeply crawled.
                ? await SecondCrawlAsync(secondLink)
                : await CrawlOnceAsync(elm);
            if (item is null) continue;
            results.Add(item);
        }

        return results;
    }

    private static async Task<WeiboContent?> CrawlOnceAsync(IElementHandle elm)
    {
        var contents = await Task.WhenAll(
            ExtractText(elm, Constants.Selectors.Username),
            ExtractTextAll(elm, Constants.Selectors.Content)
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
            .Replace("\u200b", string.Empty)
            .ReplaceLast("...全文", "...更多信息请查看原微博");

        return new WeiboContent(
            Username: contents[0],
            Content: content,
            Images: images
        );
    }

    private async Task<WeiboContent?> SecondCrawlAsync(string url)
    {
        await using var page = pbf.OpenPage(ua: "Baiduspider+(+http://www.baidu.com/search/spider.htm)").Result;
        await page.GoToAsync(url);
        await page.WaitForNetworkIdleAsync();
        await page.WaitForSelectorAsync(Constants.Selectors.Content);
        var container = await page.QuerySelectorAsync(Constants.Selectors.FullWeibo);
        if (container is null) return null;
        var content = await CrawlOnceAsync(container);
        await page.CloseAsync();
        return content;
    }

    private static async Task<string> ExtractText(IElementHandle elm, string selector)
    {
        var child = await elm.QuerySelectorAsync(selector);
        return child is null
            ? string.Empty
            : await child.InnerTextAsync();
    }

    private static async Task<string> ExtractTextAll(IElementHandle elm, string selector)
    {
        var child = (await elm.QuerySelectorAllAsync(selector)).ToList();
        return child.IsNullOrEmpty()
            ? string.Empty
            : (await Task.WhenAll(child.Select(it => it.InnerTextAsync()))).StringJoin("\n---\n");
    }
}
