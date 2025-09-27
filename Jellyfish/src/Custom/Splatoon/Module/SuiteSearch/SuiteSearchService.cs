using Jellyfish.Core.Puppeteer;
using Jellyfish.Custom.Splatoon.Data;
using Jellyfish.Util;
using Kook;
using Kook.WebSocket;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Jellyfish.Custom.Splatoon.Module.SuiteSearch;

/// <summary>
///     Service logic for the suite-build search
/// </summary>
public class SuiteSearchService(BrowserPageFactory bpf, KookSocketClient kook)
{
    private const int SearchLimit = 9;
    private const string SendouPageContainer = ".layout__main";

    /// <summary>
    ///     Run search on SendouInk and send the result to the channel
    /// </summary>
    /// <param name="keyword">User input keyword</param>
    /// <param name="channel">Current channel</param>
    /// <param name="user">Message sender</param>
    public async Task Search(string keyword, SocketTextChannel channel, SocketGuildUser user)
    {
        var cleanKeyword = Regexs.UnChineseEnglishOrNumber().Replace(keyword, string.Empty).ToUpper();
        var weapon = (
            from item in Constants.Weapons
            from alias in item.Alias
            where alias.Contains(cleanKeyword)
            select item
        ).FirstOrDefault();
        if (weapon is null)
        {
            await channel.SendInfoCardAsync($"未找到指定武器：{keyword}，这可能不是该武器的常见名称", true);
            return;
        }

        var infoMsg = await channel.SendCardSafeAsync(new CardBuilder()
            .AddModule<SectionModuleBuilder>(m => m.WithText($"💬 正在查询「{weapon.Name}」配装……"))
            .WithColor(Color.Blue)
            .Build()
        );

        var imgUrl = await SearchAndScreenshot(weapon.SendouSlug);
        await channel.SendCardSafeAsync(
            new CardBuilder()
                .AddModule<HeaderModuleBuilder>(m => m.Text = $"{weapon.Name} 常用配装（点击图片可放大）")
                .AddModule<SectionModuleBuilder>(m =>
                    m.WithText(
                        $"数据来源：[Sendou.ink]({Constants.SendouInkEndpoint}/builds/{weapon.SendouSlug}?{Constants.BuildFilter})",
                        true)
                )
                .AddModule<ImageGroupModuleBuilder>(m => m.AddElement(new ImageElementBuilder
                {
                    Source = imgUrl,
                    Alternative = "已找到武器配装，快来看看吧！"
                }))
                .AddModule<SectionModuleBuilder>(m => m.WithText(MentionUtils.KMarkdownMentionUser(user.Id), true))
                .Build()
        );
        if (infoMsg.HasValue)
        {
            await channel.DeleteMessageAsync(infoMsg.Value.Id);
        }
    }

    /// <summary>
    ///     Searches for a suite build and returns the screenshot URL (uploaded to kook)
    /// </summary>
    /// <param name="slug">Weapon slug</param>
    /// <returns>Image URL</returns>
    private async Task<string> SearchAndScreenshot(string slug)
    {
        await using var page =
            await bpf.OpenPage(
                $"{Constants.SendouInkEndpoint}/builds/{slug}?limit={SearchLimit}&{Constants.BuildFilter}");
        await page.WaitForSelectorAsync(SendouPageContainer);
        var element = await page.QuerySelectorAsync(SendouPageContainer);
        var box = await element.BoundingBoxAsync();
        var stream = await page.ScreenshotStreamAsync(new ScreenshotOptions
        {
            Type = ScreenshotType.Png,
            Clip = new Clip
            {
                Width = box.Width,
                Height = box.Height,
                X = box.X,
                Y = box.Y
            }
        });

        return await kook.Rest.CreateAssetAsync(stream, $"{slug}_{DateTime.UtcNow.Millisecond}.png");
    }
}
