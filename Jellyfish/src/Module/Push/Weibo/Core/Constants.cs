namespace Jellyfish.Module.Push.Weibo.Core;

/// <summary>
///     Constants for Weibo
/// </summary>
public static class Constants
{
    public const string WeiboRootUrl = "https://m.weibo.cn/u/";
    public const string WeiboPostUrl = "https://m.weibo.cn/detail/";
    public const string WeiboPicProxy = "https://i0.wp.com/";

    public static class Selectors
    {
        public const string Item = "div.weibo-member div.card-main";
        public const string Image = "div.weibo-media-wraps img";
        public const string Username = "h3.m-text-cut";
        public const string Content = "div.weibo-text";
    }
}
