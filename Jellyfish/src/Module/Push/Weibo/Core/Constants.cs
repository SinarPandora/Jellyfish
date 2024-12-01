namespace Jellyfish.Module.Push.Weibo.Core;

/// <summary>
///     Constants for Weibo
/// </summary>
public static class Constants
{
    public const string WeiboRootUrl = "https://weibo.com/u/";
    public const string WeiboPicProxy = "https://i0.wp.com/";

    public static class Selectors
    {
        public const string Item = ".vue-recycle-scroller__item-view";
        public const string PinTopBadge = "span[class*='title_title']";
        public const string ExpandBtn = "span.expand";
        public const string CollapseBtn = "span.collapse";
        public const string Image = "img[class*='woo-picture-img']";
    }

    public static class ClassNames
    {
        public const string Username = "head_name";
        public const string Title = "head-info_time";
        public const string Content = "detail_text";
    }
}
