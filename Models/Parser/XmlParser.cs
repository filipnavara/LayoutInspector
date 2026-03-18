using System.Xml;

namespace Schaumamal.Models.Parser;

public class XmlParser
{
    public List<DisplayNode> ParseSystem(string filePath)
    {
        var doc = new XmlDocument();
        doc.Load(filePath);

        var displayNodes = new List<DisplayNode>();
        var displayElements = doc.GetElementsByTagName(NodeName.Display);
        foreach (XmlElement el in displayElements)
            displayNodes.Add(ParseDisplay(el));
        return displayNodes;
    }

    private DisplayNode ParseDisplay(XmlElement element)
    {
        var windowNodes = new List<WindowNode>();
        var windowElements = element.GetElementsByTagName(NodeName.Window);
        foreach (XmlElement el in windowElements)
            windowNodes.Add(ParseWindow(el));
        return new DisplayNode(element.GetAttribute(PropertyName.Display.Id), windowNodes);
    }

    private WindowNode ParseWindow(XmlElement element)
    {
        var genericNodes = new List<GenericNode>();
        var hierarchyElements = element.GetElementsByTagName(NodeName.Hierarchy);
        if (hierarchyElements.Count > 0)
        {
            foreach (XmlNode child in hierarchyElements[0]!.ChildNodes)
            {
                if (child is XmlElement el && el.Name == NodeName.Node)
                    genericNodes.Add(ParseNode(el));
            }
        }

        return new WindowNode(
            int.Parse(element.GetAttribute(PropertyName.Window.Index)),
            element.GetAttribute(PropertyName.Window.Id),
            element.GetAttribute(PropertyName.Window.Title),
            element.GetAttribute(PropertyName.Window.Bounds),
            bool.TryParse(element.GetAttribute(PropertyName.Window.Active), out var active) && active,
            element.GetAttribute(PropertyName.Window.Type),
            int.TryParse(element.GetAttribute(PropertyName.Window.Layer), out var layer) ? layer : 0,
            bool.TryParse(element.GetAttribute(PropertyName.Window.Focused), out var focused) && focused,
            bool.TryParse(element.GetAttribute(PropertyName.Window.AccessibilityFocused), out var af) && af,
            genericNodes);
    }

    private GenericNode ParseNode(XmlElement element)
    {
        var children = new List<GenericNode>();
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child is XmlElement el && el.Name == NodeName.Node)
                children.Add(ParseNode(el));
        }

        return new GenericNode(
            int.TryParse(element.GetAttribute(PropertyName.Node.Index), out var idx) ? idx : 0,
            element.GetAttribute(PropertyName.Node.Text),
            element.GetAttribute(PropertyName.Node.ResourceId),
            element.GetAttribute(PropertyName.Node.Class),
            element.GetAttribute(PropertyName.Node.Package),
            element.GetAttribute(PropertyName.Node.ContentDescription),
            bool.TryParse(element.GetAttribute(PropertyName.Node.Checkable), out var v1) && v1,
            bool.TryParse(element.GetAttribute(PropertyName.Node.Checked), out var v2) && v2,
            bool.TryParse(element.GetAttribute(PropertyName.Node.Clickable), out var v3) && v3,
            bool.TryParse(element.GetAttribute(PropertyName.Node.Enabled), out var v4) && v4,
            bool.TryParse(element.GetAttribute(PropertyName.Node.Focusable), out var v5) && v5,
            bool.TryParse(element.GetAttribute(PropertyName.Node.Focused), out var v6) && v6,
            bool.TryParse(element.GetAttribute(PropertyName.Node.Scrollable), out var v7) && v7,
            bool.TryParse(element.GetAttribute(PropertyName.Node.LongClickable), out var v8) && v8,
            bool.TryParse(element.GetAttribute(PropertyName.Node.Password), out var v9) && v9,
            bool.TryParse(element.GetAttribute(PropertyName.Node.Selected), out var v10) && v10,
            element.GetAttribute(PropertyName.Node.Bounds),
            children);
    }
}
