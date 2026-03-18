using System.Text.Json.Serialization;

namespace Schaumamal.Models.Repository;

public record DisplayInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("screenshotFileName")] string ScreenshotFileName
);
