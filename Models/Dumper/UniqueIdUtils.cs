namespace Schaumamal.Models.Dumper;

public static class UniqueIdUtils
{
    public static string Hash() => Guid.NewGuid().ToString("N");
}
