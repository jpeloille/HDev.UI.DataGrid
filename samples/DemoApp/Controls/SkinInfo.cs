using Avalonia.Media;

namespace DemoApp.Controls;

public class SkinInfo
{
    public required string Name { get; init; }
    public required string ThemeResourcePath { get; init; }
    public required Color AccentColor { get; init; }
    public required Color SecondaryColor { get; init; }
    public required Color TertiaryColor { get; init; }
}
