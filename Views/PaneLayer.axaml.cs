using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Schaumamal.Models;
using Schaumamal.Models.Parser;
using Schaumamal.Shared;
using Schaumamal.ViewModels;
using System.ComponentModel;

namespace Schaumamal.Views;

public partial class PaneLayer : UserControl
{
    private bool _isVerticalDragging;
    private bool _isHorizontalDragging;
    private Point _lastDragPoint;
    private IReadOnlyList<PaneTreeItem> _treeRoots = Array.Empty<PaneTreeItem>();
    private bool _isSynchronizingTreeSelection;
    private int _treeSelectionVersion;

    public PaneLayer()
    {
        InitializeComponent();
        Width = Dimensions.InitialPaneWidth + 30;
        DataContextChanged += (_, _) =>
        {
            if (DataContext is AppViewModel vm)
                vm.PropertyChanged += OnVmChanged;
        };
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not AppViewModel vm) return;
        if (e.PropertyName is nameof(AppViewModel.SelectedDisplayData) or nameof(AppViewModel.State))
            UpdateTree(vm);
        if (e.PropertyName is nameof(AppViewModel.SelectedNode) or nameof(AppViewModel.IsNodeSelected))
        {
            UpdateTreeSelection(vm);
            UpdateProperties(vm);
        }
    }

    private void UpdateTree(AppViewModel vm)
    {
        var show = vm.State == InspectorState.Populated;
        TreePlaceholder.IsVisible = !show;
        TreeScroll.IsVisible = show;
        if (!show)
        {
            _treeRoots = Array.Empty<PaneTreeItem>();
            TreeItems.ItemsSource = null;
            SetTreeSelection(null);
            return;
        }

        RebuildTree(vm);
    }

    private void RebuildTree(AppViewModel vm)
    {
        _treeRoots = new[] { BuildTreeItem(vm.SelectedDisplayData.DisplayNode, null) };
        TreeItems.ItemsSource = _treeRoots;
        UpdateTreeSelection(vm);
    }

    private static PaneTreeItem BuildTreeItem(INode node, PaneTreeItem? parent)
    {
        var item = new PaneTreeItem(
            node,
            GetNodeDisplayText(node),
            parent);
        item.SetChildren(node.Children.Select(child => BuildTreeItem(child, item)).ToArray());
        return item;
    }

    private void UpdateTreeSelection(AppViewModel vm)
    {
        if (vm.State != InspectorState.Populated) return;
        var selectedItem = vm.IsNodeSelected && vm.SelectedNode != GenericNode.Empty
            ? FindTreeItem(vm.SelectedNode, _treeRoots)
            : null;
        SetTreeSelection(selectedItem);
    }

    private PaneTreeItem? FindTreeItem(GenericNode node, IEnumerable<PaneTreeItem> items)
    {
        foreach (var item in items)
        {
            if (item.Node is GenericNode genericNode && ReferenceEquals(genericNode, node))
                return item;

            var childMatch = FindTreeItem(node, item.Children);
            if (childMatch != null)
                return childMatch;
        }

        return null;
    }

    private void SetTreeSelection(PaneTreeItem? item)
    {
        var selectionVersion = ++_treeSelectionVersion;
        if (item == null)
        {
            SelectTreeItem(null);
            return;
        }

        var treePath = GetTreePath(item);
        ExpandAndSelectTreeItem(treePath, 0, TreeItems, selectionVersion);
    }

    private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isSynchronizingTreeSelection || DataContext is not AppViewModel vm)
            return;

        if (TreeItems.SelectedItem is PaneTreeItem { Node: GenericNode node })
        {
            vm.SelectNode(node);
            return;
        }

        UpdateTreeSelection(vm);
    }

    private void ExpandAndSelectTreeItem(
        IReadOnlyList<PaneTreeItem> treePath,
        int level,
        ItemsControl parentControl,
        int selectionVersion)
    {
        if (selectionVersion != _treeSelectionVersion)
            return;

        var targetItem = treePath[level];
        if (parentControl.ContainerFromItem(targetItem) is not TreeViewItem container)
        {
            Dispatcher.UIThread.Post(
                () => ExpandAndSelectTreeItem(treePath, level, parentControl, selectionVersion),
                DispatcherPriority.Loaded);
            return;
        }

        if (level < treePath.Count - 1)
        {
            container.IsExpanded = true;
            Dispatcher.UIThread.Post(
                () => ExpandAndSelectTreeItem(treePath, level + 1, container, selectionVersion),
                DispatcherPriority.Loaded);
            return;
        }

        SelectTreeItem(targetItem);
        container.BringIntoView();
    }

    private void SelectTreeItem(PaneTreeItem? item)
    {
        _isSynchronizingTreeSelection = true;
        try
        {
            TreeItems.SelectedItem = item;
        }
        finally
        {
            _isSynchronizingTreeSelection = false;
        }
    }

    private static IReadOnlyList<PaneTreeItem> GetTreePath(PaneTreeItem item)
    {
        var path = new List<PaneTreeItem>();
        for (var current = item; current != null; current = current.Parent)
            path.Add(current);
        path.Reverse();
        return path;
    }

    private static string GetNodeDisplayText(INode node) => node switch
    {
        DisplayNode dn => $"Display {{ id={dn.Id} windows={dn.Children.Count} }}",
        WindowNode wn => $"({wn.Index}) Window {{ title=\"{wn.Title}\" }} {wn.Bounds}",
        GenericNode gn => FormatGenericNodeText(gn),
        _ => node.ToString() ?? ""
    };

    private static string FormatGenericNodeText(GenericNode gn)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"({gn.Index})");
        var shortClass = gn.ClassName.Contains('.') ? gn.ClassName.Split('.')[^1] : gn.ClassName;
        sb.Append($" {shortClass}");
        if (!string.IsNullOrEmpty(gn.ResourceId))
        {
            var shortId = gn.ResourceId.Contains(':') ? gn.ResourceId.Split(':')[^1] : gn.ResourceId;
            sb.Append($" {shortId}");
        }
        if (!string.IsNullOrEmpty(gn.Text))
        {
            var displayText = gn.Text.Length > 10 ? gn.Text[..10] + "..." : gn.Text;
            sb.Append($" {{ text=\"{displayText}\" }}");
        }
        if (!string.IsNullOrEmpty(gn.ContentDesc))
            sb.Append($" - \"{gn.ContentDesc}\"");
        return sb.ToString();
    }

    private void UpdateProperties(AppViewModel vm)
    {
        var show = vm.State == InspectorState.Populated && vm.IsNodeSelected
                   && vm.SelectedNode != GenericNode.Empty;
        PropertiesPlaceholder.IsVisible = !show;
        PropertiesScroll.IsVisible = show;
        if (!show) { PropertyItems.ItemsSource = null; return; }

        var props = GetPropertyMap(vm.SelectedNode);
        var rows = new List<Control>();
        foreach (var (key, value) in props)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(Dimensions.SmallPadding, 2)
            };
            row.Children.Add(new TextBlock
            {
                Text = key,
                Foreground = AppColors.PrimaryTextBrush,
                Width = Dimensions.PropertyNameWidth,
            });
            var valText = value;
            var valBlock = new TextBlock
            {
                Text = valText,
                Foreground = AppColors.PrimaryTextBrush,
                Cursor = new Cursor(StandardCursorType.Hand),
                Padding = new Thickness(Dimensions.SmallPadding),
                MaxWidth = Dimensions.MaximumPropertyValueWidth,
            };
            valBlock.PointerPressed += async (_, _) =>
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null) await clipboard.SetTextAsync(valText);
            };
            row.Children.Add(valBlock);
            rows.Add(row);
        }
        PropertyItems.ItemsSource = rows;
    }

    private static Dictionary<string, string> GetPropertyMap(GenericNode node)
    {
        var map = new Dictionary<string, string>
        {
            [PropertyName.Node.Index] = node.Index.ToString(),
            [PropertyName.Node.Text] = node.Text,
            [PropertyName.Node.ResourceId] = node.ResourceId,
            [PropertyName.Node.Class] = node.ClassName,
            [PropertyName.Node.Package] = node.PackageName,
            [PropertyName.Node.ContentDescription] = node.ContentDesc,
            [PropertyName.Node.Checkable] = node.Checkable.ToString(),
            [PropertyName.Node.Checked] = node.Checked.ToString(),
            [PropertyName.Node.Clickable] = node.Clickable.ToString(),
            [PropertyName.Node.Enabled] = node.Enabled.ToString(),
            [PropertyName.Node.Focusable] = node.Focusable.ToString(),
            [PropertyName.Node.Focused] = node.Focused.ToString(),
            [PropertyName.Node.Scrollable] = node.Scrollable.ToString(),
            [PropertyName.Node.LongClickable] = node.LongClickable.ToString(),
            [PropertyName.Node.Password] = node.Password.ToString(),
            [PropertyName.Node.Selected] = node.Selected.ToString(),
            [PropertyName.Node.Bounds] = node.Bounds,
        };
        foreach (var key in map.Keys.ToList())
            if (string.IsNullOrEmpty(map[key])) map[key] = "-";
        return map;
    }

    private void OnVerticalWedgePressed(object? sender, PointerPressedEventArgs e)
    {
        _isVerticalDragging = true;
        _lastDragPoint = e.GetPosition(Parent as Visual);
        e.Pointer.Capture(sender as IInputElement);
    }

    private void OnVerticalWedgeMoved(object? sender, PointerEventArgs e)
    {
        if (!_isVerticalDragging) return;
        var cur = e.GetPosition(Parent as Visual);
        var delta = _lastDragPoint.X - cur.X;
        var newWidth = Width + delta;
        if (newWidth >= Dimensions.MinimumPaneDimension && newWidth <= 800)
            Width = newWidth;
        _lastDragPoint = cur;
    }

    private void OnVerticalWedgeReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isVerticalDragging = false;
        e.Pointer.Capture(null);
    }

    private void OnHorizontalWedgePressed(object? sender, PointerPressedEventArgs e)
    {
        _isHorizontalDragging = true;
        _lastDragPoint = e.GetPosition(this);
        e.Pointer.Capture(sender as IInputElement);
    }

    private void OnHorizontalWedgeMoved(object? sender, PointerEventArgs e)
    {
        if (!_isHorizontalDragging) return;
        var cur = e.GetPosition(this);
        var delta = cur.Y - _lastDragPoint.Y;
        var newHeight = UpperPane.Height + delta;
        if (newHeight >= Dimensions.MinimumPaneDimension &&
            newHeight <= Bounds.Height - Dimensions.MinimumPaneDimension - 40)
            UpperPane.Height = newHeight;
        _lastDragPoint = cur;
    }

    private void OnHorizontalWedgeReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isHorizontalDragging = false;
        e.Pointer.Capture(null);
    }
}

internal sealed class PaneTreeItem(INode node, string text, PaneTreeItem? parent)
{
    public INode Node { get; } = node;
    public string Text { get; } = text;
    public PaneTreeItem? Parent { get; } = parent;
    public IReadOnlyList<PaneTreeItem> Children { get; private set; } = Array.Empty<PaneTreeItem>();
    public IBrush Foreground { get; } = node is GenericNode ? AppColors.PrimaryTextBrush : AppColors.DiscreteTextBrush;

    public void SetChildren(IReadOnlyList<PaneTreeItem> children) => Children = children;
}
