using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Schaumamal.Models;
using Schaumamal.Models.Parser;
using Schaumamal.Shared;
using Schaumamal.ViewModels;
using System.ComponentModel;

namespace Schaumamal.Views;

public partial class ScreenshotLayer : UserControl
{
    private bool _isPanning;
    private Point _lastPanPoint;
    private double _currentScale = 1.0;
    private ScaleTransform _screenshotScale = null!;
    private TranslateTransform _screenshotTranslate = null!;

    public ScreenshotLayer()
    {
        InitializeComponent();
        var panel = this.FindControl<Panel>("ScreenshotPanel")!;
        var tg = (TransformGroup)panel.RenderTransform!;
        _screenshotScale = (ScaleTransform)tg.Children[0];
        _screenshotTranslate = (TranslateTransform)tg.Children[1];
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
            UpdateScreenshot(vm);
        if (e.PropertyName is nameof(AppViewModel.SelectedNode) or nameof(AppViewModel.IsNodeSelected))
            UpdateHighlighter(vm);
    }

    private void UpdateScreenshot(AppViewModel vm)
    {
        if (vm.State != InspectorState.Populated) { ScreenshotImage.IsVisible = false; return; }
        var path = vm.SelectedDisplayData.ScreenshotFilePath;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            ScreenshotImage.Source = new Bitmap(path);
            ScreenshotImage.IsVisible = true;
        }
    }

    private void UpdateHighlighter(AppViewModel vm)
    {
        HighlighterCanvas.Children.Clear();
        if (!vm.IsNodeSelected || vm.SelectedNode == GenericNode.Empty) return;
        var bounds = ParseBounds(vm.SelectedNode.Bounds);
        if (bounds == null) return;
        var imgBounds = ScreenshotImage.Bounds;
        if (imgBounds.Width <= 0) return;
        var source = ScreenshotImage.Source as Bitmap;
        if (source == null) return;
        var sx = imgBounds.Width / source.PixelSize.Width;
        var sy = imgBounds.Height / source.PixelSize.Height;
        var rect = new Rectangle
        {
            Stroke = Brushes.Red,
            StrokeThickness = Dimensions.DefaultHighlighterStrokeWidth / _currentScale,
            Fill = Brushes.Transparent,
            Width = (bounds.Value.r - bounds.Value.l) * sx,
            Height = (bounds.Value.b - bounds.Value.t) * sy
        };
        Canvas.SetLeft(rect, bounds.Value.l * sx);
        Canvas.SetTop(rect, bounds.Value.t * sy);
        HighlighterCanvas.Children.Add(rect);
    }

    private static (float l, float t, float r, float b)? ParseBounds(string s)
    {
        try
        {
            var p = s.Replace("[", "").Replace("]", ",").TrimEnd(',').Split(',');
            return p.Length >= 4 ? (float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3])) : null;
        }
        catch { return null; }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is AppViewModel vm && vm.State == InspectorState.Populated)
        {
            var props = e.GetCurrentPoint(ScreenshotImage).Properties;
            if (props.IsLeftButtonPressed)
            {
                var pt = e.GetPosition(ScreenshotImage);
                var src = ScreenshotImage.Source as Bitmap;
                if (src == null) return;
                var sx = src.PixelSize.Width / ScreenshotImage.Bounds.Width;
                var sy = src.PixelSize.Height / ScreenshotImage.Bounds.Height;
                var nodes = GetNodesUnder(vm.SelectedDisplayData.DisplayNode, (float)(pt.X * sx), (float)(pt.Y * sy));
                if (nodes.Count > 0)
                {
                    var smallest = nodes.OrderBy(n => { var b = ParseBounds(n.Bounds); return b == null ? float.MaxValue : (b.Value.r - b.Value.l) * (b.Value.b - b.Value.t); }).First();
                    vm.SelectNode(smallest);
                }
                return;
            }
        }
        if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed || e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            _isPanning = true;
            _lastPanPoint = e.GetPosition(this);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning) return;
        var cur = e.GetPosition(this);
        _screenshotTranslate.X += cur.X - _lastPanPoint.X;
        _screenshotTranslate.Y += cur.Y - _lastPanPoint.Y;
        _lastPanPoint = cur;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => _isPanning = false;

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var factor = 1.0 + e.Delta.Y / AppValues.ScrollZoomFactor;
        var ns = _currentScale * factor;
        if (ns < AppValues.MinScreenshotScale || ns > AppValues.MaxScreenshotScale) return;
        _currentScale = ns;
        _screenshotScale.ScaleX = _currentScale;
        _screenshotScale.ScaleY = _currentScale;
        e.Handled = true;
    }

    private static List<GenericNode> GetNodesUnder(DisplayNode dn, float x, float y)
    {
        var result = new List<GenericNode>();
        foreach (var w in dn.WindowChildren)
            foreach (var r in w.GenericChildren)
                Collect(r, x, y, result);
        return result;
    }

    private static void Collect(GenericNode n, float x, float y, List<GenericNode> result)
    {
        var b = ParseBounds(n.Bounds);
        if (b != null && x >= b.Value.l && x <= b.Value.r && y >= b.Value.t && y <= b.Value.b)
            result.Add(n);
        foreach (var c in n.GenericChildren) Collect(c, x, y, result);
    }
}
