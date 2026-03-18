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
    private TextBlock? _selectedTreeItem;

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
        if (!show) { TreeItems.ItemsSource = null; return; }
        RebuildTree(vm);
    }

    private void RebuildTree(AppViewModel vm)
    {
        _selectedTreeItem = null;
        var lines = new List<Control>();
        BuildFlatTree(vm.SelectedDisplayData.DisplayNode, 0, vm, lines);
        TreeItems.ItemsSource = lines;
        if (_selectedTreeItem != null)
            Dispatcher.UIThread.Post(() => _selectedTreeItem?.BringIntoView(), DispatcherPriority.Loaded);
    }

    private void BuildFlatTree(INode node, int depth, AppViewModel vm, List<Control> lines)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        if (depth > 0)
            panel.Children.Add(new Border { Width = Dimensions.StartPaddingPerDepthLevel * depth });

        var isSelected = node is GenericNode gn && ReferenceEquals(gn, vm.SelectedNode);
        var tb = new TextBlock
        {
            Text = GetNodeDisplayText(node),
            Foreground = node is GenericNode ? AppColors.PrimaryTextBrush : AppColors.DiscreteTextBrush,
            Background = isSelected ? AppColors.AccentBrush : Brushes.Transparent,
            Padding = new Thickness(Dimensions.SmallPadding),
            Cursor = node is GenericNode ? new Cursor(StandardCursorType.Hand) : null,
        };

        if (isSelected) _selectedTreeItem = tb;

        if (node is GenericNode clickNode)
        {
            tb.PointerPressed += (_, _) => vm.SelectNode(clickNode);
        }

        panel.Children.Add(tb);
        lines.Add(panel);

        foreach (var child in node.Children)
            BuildFlatTree(child, depth + 1, vm, lines);
    }

    private void UpdateTreeSelection(AppViewModel vm)
    {
        if (vm.State != InspectorState.Populated) return;
        RebuildTree(vm);
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
