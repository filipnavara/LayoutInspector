using Schaumamal.Models.Parser;

namespace Schaumamal.Models.DisplayDataResolver;

public record DisplayData(string ScreenshotFilePath, DisplayNode DisplayNode)
{
    public static readonly DisplayData Empty = new("", DisplayNode.Empty);
}
