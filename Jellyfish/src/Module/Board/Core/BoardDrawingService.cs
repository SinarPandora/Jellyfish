using Jellyfish.Core.Resource;
using SkiaSharp;

namespace Jellyfish.Module.Board.Core;

/// <summary>
///     Board drawing service
/// </summary>
public static class BoardDrawingService
{
    /// <summary>
    ///     Base text paint
    /// </summary>
    /// <returns>SKPaint object</returns>
    private static SKPaint BaseText()
    {
        var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.TextAlign = SKTextAlign.Center;
        paint.Typeface = SystemResources.Font;
        return paint;
    }

    /// <summary>
    ///     Create stroke layer for text
    /// </summary>
    /// <param name="setting">Score text setting</param>
    /// <returns>SKPaint object</returns>
    private static SKPaint StrokeLayer(BoardTextSetting setting)
    {
        var paint = BaseText();
        paint.Color = setting.StrokeColor;
        paint.TextSize = setting.TextSize;
        paint.StrokeWidth = setting.StrokeWidth;
        paint.Style = SKPaintStyle.Stroke;
        return paint;
    }

    /// <summary>
    ///     Create text layer
    /// </summary>
    /// <param name="setting">Score text setting</param>
    /// <returns>SKPaint object</returns>
    private static SKPaint TextLayer(BoardTextSetting setting)
    {
        var paint = BaseText();
        paint.Color = setting.Color;
        paint.TextSize = setting.TextSize;
        paint.Style = SKPaintStyle.Fill;
        return paint;
    }

    /// <summary>
    ///     Draw score text image
    /// </summary>
    /// <param name="score">Score content</param>
    /// <param name="setting">Setting Object</param>
    /// <returns>Generated image data</returns>
    public static SKImage DrawScoreTextImage(string score, BoardTextSetting setting)
    {
        using var bitmap = new SKBitmap(setting.Width, setting.Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        var x = bitmap.Width / 2.0f;
        var y = (bitmap.Height + setting.TextSize) / 2;
        canvas.DrawText(score, x, y, StrokeLayer(setting));
        canvas.DrawText(score, x, y, TextLayer(setting));
        return SKImage.FromBitmap(bitmap);
    }
}
