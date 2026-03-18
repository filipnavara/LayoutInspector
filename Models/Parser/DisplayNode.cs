namespace Schaumamal.Models.Parser;

public record DisplayNode(string Id, IReadOnlyList<WindowNode> WindowChildren) : INode
{
    public IReadOnlyList<INode> Children => WindowChildren;
    public static readonly DisplayNode Empty = new("-1", Array.Empty<WindowNode>());
}
