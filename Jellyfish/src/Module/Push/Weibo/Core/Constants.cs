namespace Jellyfish.Module.Push.Weibo.Core;

/// <summary>
///     Constants for Weibo
/// </summary>
public static class Constants
{
    public const string WeiboRootUrl = "https://m.weibo.cn/u/";
    public const string WeiboPicProxy = "https://i0.wp.com/";

    public static class Selectors
    {
        public const string Item = "div.weibo-member div.card-main";
        public const string PinTopBadge = "div.card-title h4";
        public const string Image = "div.weibo-media-wraps img";
        public const string Username = "h3.m-text-cut";
        public const string Content = "article.weibo-main div.weibo-text";
    }
}
