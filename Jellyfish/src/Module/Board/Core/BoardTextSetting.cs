using SkiaSharp;

namespace Jellyfish.Module.Board.Core;

/// <summary>
///     Setting-Object for drawing score text
/// </summary>
public record BoardTextSetting(
    SKColor Color,
    SKColor StrokeColor,
    int Width = 300,
    int Height = 300,
    float TextSize = 150f,
    float StrokeWidth = 8f
);
