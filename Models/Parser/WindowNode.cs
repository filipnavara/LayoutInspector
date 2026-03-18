namespace Schaumamal.Models.Parser;

public record WindowNode(
    int Index, string Id, string Title, string Bounds,
    bool Active, string Type, int Layer,
    bool Focused, bool AccessibilityFocused,
    IReadOnlyList<GenericNode> GenericChildren
) : INode
{
    public IReadOnlyList<INode> Children => GenericChildren;
}
