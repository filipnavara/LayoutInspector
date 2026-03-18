namespace Schaumamal.Models.Parser;

public record SystemNode(IReadOnlyList<DisplayNode> DisplayChildren) : INode
{
    public IReadOnlyList<INode> Children => DisplayChildren;
    public static readonly SystemNode Empty = new(Array.Empty<DisplayNode>());
}
