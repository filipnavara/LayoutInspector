namespace Schaumamal.Models.Parser;

public interface INode
{
    IReadOnlyList<INode> Children { get; }
}
