using System.ComponentModel.DataAnnotations.Schema;
using Jellyfish.Module.Push.Weibo.Core;
using Kook;

namespace Jellyfish.Module.Push.Weibo.Data;

/// <summary>
///     Crawl history for Weibo
/// </summary>
public class WeiboCrawlHistory(string uid, string hash, string username, string content, string images, string mid)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Uid { get; set; } = uid;
    public string Hash { get; set; } = hash;
    public string Username { get; set; } = username;
    public string Content { get; set; } = content;
    public string Images { get; set; } = images;
    public string Mid { get; set; } = mid;
    [Column(TypeName = "timestamp")] public DateTime CreateTime { get; init; } = DateTime.Now;

    /// <summary>
    ///     Convert to kook Markdown card
    /// </summary>
    /// <returns>Markdown card instance</returns>
    public ICard ToCard()
    {
        var cardBuilder = new CardBuilder()
            .AddModule<HeaderModuleBuilder>(m => m.Text = $"{Username}的新微博")
            .AddModule<SectionModuleBuilder>(s => s.WithText(Content))
            .AddModule<DividerModuleBuilder>();

        if (Images.IsNotNullOrWhiteSpace())
        {
            cardBuilder.AddModule<ImageGroupModuleBuilder>(ig =>
            {
                foreach (var image in Images.Split(",").Take(9))
                {
                    ig.AddElement(i => i.Source = image);
                }
            });
        }

        return cardBuilder
            .AddModule<ContextModuleBuilder>(c =>
                c.AddElement(new KMarkdownElementBuilder($"[原帖地址]({Constants.WeiboPostUrl + Mid})")))
            .WithSize(CardSize.Large)
            .Build();
    }
}
