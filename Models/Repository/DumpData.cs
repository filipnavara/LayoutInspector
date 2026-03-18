using System.Text.Json.Serialization;

namespace Schaumamal.Models.Repository;

public record DumpData(
    [property: JsonPropertyName("directoryName")] string DirectoryName,
    [property: JsonPropertyName("nickname")] string Nickname,
    [property: JsonPropertyName("timeMilliseconds")] long TimeMilliseconds,
    [property: JsonPropertyName("xmlTreeFileName")] string XmlTreeFileName,
    [property: JsonPropertyName("displays")] List<DisplayInfo> Displays
)
{
    public static readonly DumpData Empty = new("", "", -1, "", new List<DisplayInfo>());
}
