using System.Text.Json.Serialization;

namespace Schaumamal.Models.Repository;

public record Settings(
    [property: JsonPropertyName("maxDumps")] int MaxDumps
)
{
    public static readonly Settings DefaultEmpty = new(15);
}
