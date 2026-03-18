namespace Schaumamal.Models.Parser;

public record GenericNode(
    int Index, string Text, string ResourceId, string ClassName,
    string PackageName, string ContentDesc,
    bool Checkable, bool Checked, bool Clickable, bool Enabled,
    bool Focusable, bool Focused, bool Scrollable, bool LongClickable,
    bool Password, bool Selected, string Bounds,
    IReadOnlyList<GenericNode> GenericChildren
) : INode
{
    public IReadOnlyList<INode> Children => GenericChildren;

    public static readonly GenericNode Empty = new(
        Index: -1, Text: "", ResourceId: "", ClassName: "", PackageName: "",
        ContentDesc: "", Checkable: false, Checked: false, Clickable: false,
        Enabled: false, Focusable: false, Focused: false, Scrollable: false,
        LongClickable: false, Password: false, Selected: false,
        Bounds: "[0,0][0,0]", GenericChildren: Array.Empty<GenericNode>());
}
