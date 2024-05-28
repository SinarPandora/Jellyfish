using SkiaSharp;

namespace Jellyfish.Core.Drawing.Core;

/// <summary>
///     Resource for drawing
/// </summary>
public static class DrawingResources
{
    /// <summary>
    ///     Base font (WenQuanYi Micro Hei)
    /// </summary>
    public static readonly SKTypeface Font = SKTypeface.FromFile(
        Path.Join(Directory.GetCurrentDirectory(), "Resources", "Font", "wqy-microhei.ttc")
    );
}
