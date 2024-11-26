namespace Jellyfish.Module.Push.Weibo.Core;

/// <summary>
///     Weibo Item
/// </summary>
public record WeiboItem(string Username, string Time, string Content, string[] Images)
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
        return HashCode.Combine(Content, string.Empty.Join(Images));
    }

    /// <summary>
    ///     Are all contents in this item empty?
    /// </summary>
    /// <returns>Is empty or not</returns>
    public bool IsEmpty()
    {
        return Username.IsEmpty() && Time.IsEmpty() && Content.IsEmpty() && Images.IsEmpty();
    }
}
