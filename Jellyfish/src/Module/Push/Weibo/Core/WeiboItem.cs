using Jellyfish.Util;
using Kook;

namespace Jellyfish.Module.Push.Weibo.Core;

/// <summary>
///     Weibo Item
/// </summary>
public record WeiboItem(string Username, string Content, string[] Images, string Url, string Md5)
{
    public virtual bool Equals(WeiboItem? other)
    {
        return other is not null
               && Content == other.Content
               && Images.Length == other.Images.Length
               && !Images.Where((t, i) => t != other.Images[i]).Any();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Md5);
    }

    /// <summary>
    ///     Are all contents in this item empty?
    /// </summary>
    /// <returns>Is empty or not</returns>
    public bool IsEmpty()
    {
        return Username.IsEmpty() && Content.IsEmpty() && Images.IsEmpty();
    }

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

        if (Images.IsNotEmpty())
        {
            cardBuilder.AddModule<ImageGroupModuleBuilder>(ig =>
            {
                foreach (var image in Images.Take(9))
                {
                    ig.AddElement(i => i.Source = image);
                }
            });
        }

        return cardBuilder
            .AddModule<ContextModuleBuilder>(c => c.AddElement(new KMarkdownElementBuilder($"[原帖地址]({Url})")))
            .WithSize(CardSize.Large)
            .Build();
    }

    public WeiboItem WithUrl(string url)
    {
        return this with { Url = url, Md5 = (Content + string.Empty.Join(Images) + url).ToMd5Hash() };
    }
}
