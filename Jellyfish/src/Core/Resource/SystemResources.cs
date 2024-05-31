using SkiaSharp;

namespace Jellyfish.Core.Resource;

/// <summary>
///     System built-in resources
/// </summary>
public static class SystemResources
{
    /// <summary>
    ///     Base font (WenQuanYi Micro Hei)
    /// </summary>
    public static readonly SKTypeface Font = SKTypeface.FromFile(
        Path.Join(Directory.GetCurrentDirectory(), "Resources", "Font", "wqy-microhei.ttc")
    );
}
