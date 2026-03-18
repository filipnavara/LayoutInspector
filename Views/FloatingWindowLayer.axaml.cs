using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Schaumamal.Models.Repository;
using Schaumamal.Shared;
using Schaumamal.ViewModels;

namespace Schaumamal.Views;

public partial class FloatingWindowLayer : UserControl
{
    public FloatingWindowLayer()
    {
        InitializeComponent();
    }

    public void Show()
    {
        if (DataContext is not AppViewModel vm) return;
        PopulateHistory(vm);
        Overlay.IsVisible = true;
    }

    public void Hide()
    {
        Overlay.IsVisible = false;
    }

    private void PopulateHistory(AppViewModel vm)
    {
        var entries = new List<Control>();
        foreach (var dump in vm.Content.Dumps)
        {
            var isSelected = dump == vm.SelectedDump;
            entries.Add(CreateHistoryEntry(dump, isSelected, vm));
        }
        HistoryList.ItemsSource = entries;
    }

    private Control CreateHistoryEntry(DumpData dump, bool isSelected, AppViewModel vm)
    {
        var row = new Border
        {
            CornerRadius = new CornerRadius(Dimensions.MediumCornerRadius),
            Background = isSelected
                ? new SolidColorBrush(AppColors.AccentColor, 0.3)
                : Brushes.Transparent,
            Padding = new Thickness(Dimensions.LargePadding, Dimensions.MediumPadding),
            Cursor = new Cursor(StandardCursorType.Hand),
        };

        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Dimensions.LargePadding };

        // Thumbnail
        if (vm.ResolvedDumpThumbnails.TryGetValue(dump, out var thumbPath) && File.Exists(thumbPath))
        {
            content.Children.Add(new Image
            {
                Source = new Bitmap(thumbPath),
                Width = 30, Height = 30,
                Stretch = Stretch.Uniform,
            });
        }

        // Info column
        var info = new StackPanel { Spacing = 2 };
        info.Children.Add(new TextBlock
        {
            Text = dump.Nickname,
            Foreground = AppColors.PrimaryTextBrush,
            FontSize = 16,
        });

        var detailRow = new DockPanel();
        var countText = new TextBlock
        {
            Text = $"{dump.Displays.Count} display{(dump.Displays.Count > 1 ? "s" : "")}",
            Foreground = AppColors.DiscreteTextBrush,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        DockPanel.SetDock(countText, Dock.Right);
        detailRow.Children.Add(countText);
        detailRow.Children.Add(new TextBlock
        {
            Text = FormatDate(dump.TimeMilliseconds),
            Foreground = AppColors.DiscreteTextBrush,
        });
        info.Children.Add(detailRow);

        content.Children.Add(info);
        row.Child = content;

        row.PointerPressed += (_, _) =>
        {
            Hide();
            vm.SelectDump(dump);
        };

        return row;
    }

    private static string FormatDate(long millis)
    {
        if (millis <= 0) return "";
        return DateTimeOffset.FromUnixTimeMilliseconds(millis).LocalDateTime.ToString("dd MMM yyyy, HH:mm");
    }

    private void OnBackdropPressed(object? sender, PointerPressedEventArgs e) => Hide();
    private void OnCardPressed(object? sender, PointerPressedEventArgs e) => e.Handled = true;
    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Hide();
}
