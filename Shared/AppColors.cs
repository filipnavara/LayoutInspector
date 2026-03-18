using Avalonia.Media;

namespace Schaumamal.Shared;

public static class AppColors
{
    // Generic
    public static readonly Color PrimaryTextColor = Color.Parse("#B4B7C2");
    public static readonly Color DiscreteTextColor = Color.Parse("#71727B");
    public static readonly Color PrimaryElementColor = PrimaryTextColor;
    public static readonly Color DisabledPrimaryElementColor = Color.Parse("#53555B");
    public static readonly Color AccentColor = Color.Parse("#705575");
    public static readonly Color VibrantAccentColor = Color.Parse("#753b99");
    public static readonly Color BackgroundColor = Color.Parse("#121317");
    public static readonly Color ElevatedBackgroundColor = Color.Parse("#1E2024");

    // Button Layer
    public static readonly Color ExtractionButtonColor = Color.Parse("#BA1A1A");
    public static readonly Color ExtractionProgressBarColor = Color.Parse("#006622");

    // Pane Layer
    public static readonly Color WedgeColor = Color.FromArgb(0xBB, 0xC3, 0xC6, 0xD2);
    public static readonly Color ScrollbarHoverColor = Color.FromArgb(128, 255, 255, 255);
    public static readonly Color ScrollbarUnhoverColor = Color.FromArgb(31, 255, 255, 255);
    public static readonly Color PaneBorderColor = Color.FromArgb(31, 255, 255, 255);

    // Screenshot Layer
    public static readonly Color HighlighterColor = Colors.Red;

    // Notifications
    public static readonly Color InfoIconColor = Color.Parse("#3997FE");
    public static readonly Color WarningIconColor = Color.Parse("#FFE74B");
    public static readonly Color ErrorIconColor = Color.Parse("#FF3A42");
    public static readonly Color NotificationActionTextColor = Color.Parse("#4C82FE");

    // Brushes
    public static readonly IBrush PrimaryTextBrush = new SolidColorBrush(PrimaryTextColor);
    public static readonly IBrush DiscreteTextBrush = new SolidColorBrush(DiscreteTextColor);
    public static readonly IBrush BackgroundBrush = new SolidColorBrush(BackgroundColor);
    public static readonly IBrush ElevatedBackgroundBrush = new SolidColorBrush(ElevatedBackgroundColor);
    public static readonly IBrush PaneBorderBrush = new SolidColorBrush(PaneBorderColor);
    public static readonly IBrush AccentBrush = new SolidColorBrush(AccentColor);
}
