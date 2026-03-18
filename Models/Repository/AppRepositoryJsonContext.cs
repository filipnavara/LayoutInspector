using System.Text.Json.Serialization;

namespace Schaumamal.Models.Repository;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Content))]
[JsonSerializable(typeof(Settings))]
internal partial class AppRepositoryJsonContext : JsonSerializerContext;
