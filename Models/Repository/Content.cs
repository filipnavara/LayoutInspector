using System.Text.Json.Serialization;

namespace Schaumamal.Models.Repository;

public record Content(
    [property: JsonPropertyName("tempDirectoryName")] string TempDirectoryName,
    [property: JsonPropertyName("dumpsDirectoryName")] string DumpsDirectoryName,
    [property: JsonPropertyName("dumps")] List<DumpData> Dumps
)
{
    public static readonly Content DefaultEmpty = new("tmp", "dumps", new List<DumpData>());
}
