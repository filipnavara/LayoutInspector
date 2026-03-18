using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Schaumamal.Shared;
using Schaumamal.ViewModels;
using Schaumamal.ViewModels.Notifications;
using System.Collections.ObjectModel;

namespace Schaumamal.Views;

public partial class NotificationLayer : UserControl
{
    private readonly ObservableCollection<Control> _pills = new();
    private IDisposable? _subscription;

    public NotificationLayer()
    {
        InitializeComponent();
        NotificationStack.ItemsSource = _pills;
        DataContextChanged += (_, _) =>
        {
            _subscription?.Dispose();
            if (DataContext is AppViewModel vm)
                _subscription = vm.NotificationManager.Notifications.Subscribe(OnNewNotification);
        };
    }

    private void OnNewNotification(Notification notification)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (notification.ExitStrategy is NotificationExitStrategy.Timeout t &&
                t.Value.TotalMilliseconds == 0) return;

            var (pill, progressBar) = CreateNotificationPill(notification);
            _pills.Insert(0, pill);

            if (notification.ExitStrategy is NotificationExitStrategy.Timeout timeout)
                StartTimeout(pill, timeout.Value, progressBar);
        });
    }

    private void StartTimeout(Control pill, TimeSpan duration, ProgressBar? progressBar)
    {
        var state = new TimeoutState
        {
            RemainingMs = duration.TotalMilliseconds,
            TotalMs = duration.TotalMilliseconds,
            ProgressBar = progressBar,
        };
        pill.Tag = state;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        state.Timer = timer;
        timer.Tick += (_, _) =>
        {
            if (state.IsPaused) return;
            state.RemainingMs -= 50;
            if (state.ProgressBar != null)
                state.ProgressBar.Value = Math.Max(0, state.RemainingMs / state.TotalMs);
            if (state.RemainingMs <= 0)
            {
                timer.Stop();
                _pills.Remove(pill);
            }
        };
        timer.Start();
    }

    private (Control pill, ProgressBar? progressBar) CreateNotificationPill(Notification notification)
    {
        var outerPanel = new Panel { Width = Dimensions.NotificationWidth, Margin = new Thickness(0, 2) };

        // Main card
        var card = new Border
        {
            CornerRadius = new CornerRadius(Dimensions.LargeCornerRadius),
            Background = AppColors.ElevatedBackgroundBrush,
            BorderBrush = AppColors.PaneBorderBrush,
            BorderThickness = new Thickness(Dimensions.PaneBorderWidth),
            Margin = new Thickness(0, Dimensions.MediumPadding, Dimensions.MediumPadding, 0),
            ClipToBounds = true,
        };

        var cardLayout = new DockPanel();

        // Progress bar (for timeout notifications)
        ProgressBar? progressBar = null;
        if (notification.ExitStrategy is NotificationExitStrategy.Timeout)
        {
            progressBar = new ProgressBar
            {
                Minimum = 0, Maximum = 1, Value = 1,
                Foreground = AppColors.AccentBrush,
                Background = Brushes.Transparent,
                MinHeight = 3,
            };
            DockPanel.SetDock(progressBar, Dock.Top);
            cardLayout.Children.Add(progressBar);
        }

        var cardContent = new StackPanel
        {
            Margin = new Thickness(Dimensions.MediumPadding + Dimensions.SmallPadding),
        };

        // Header: icon + title + timestamp
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Dimensions.SmallPadding };
        header.Children.Add(CreateSeverityIcon(notification.Severity));
        header.Children.Add(new TextBlock
        {
            Text = notification.Title,
            FontWeight = FontWeight.ExtraBold,
            Foreground = AppColors.PrimaryTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
        });
        header.Children.Add(new TextBlock
        {
            Text = $"@ {FormatTime(notification.Timestamp)}",
            Foreground = AppColors.DiscreteTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
        });
        if (notification.ExitStrategy is NotificationExitStrategy.Manual)
        {
            header.Children.Add(new TextBlock
            {
                Text = "(persistent)",
                Foreground = AppColors.DiscreteTextBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
            });
        }
        cardContent.Children.Add(header);

        // Description
        cardContent.Children.Add(new SelectableTextBlock
        {
            Text = notification.Description,
            Foreground = AppColors.PrimaryTextBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, Dimensions.MediumPadding, 0, 0),
        });

        // Actions
        if (notification.Actions.Count > 0)
        {
            var actionsPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, Dimensions.MediumPadding, 0, 0),
            };
            foreach (var action in notification.Actions)
            {
                var capturedAction = action;
                var actionBtn = new TextBlock
                {
                    Text = action.Title,
                    Foreground = new SolidColorBrush(AppColors.NotificationActionTextColor),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Padding = new Thickness(Dimensions.SmallPadding),
                };
                actionBtn.PointerPressed += (_, _) =>
                {
                    capturedAction.Block();
                    if (capturedAction.ShouldHideNotification)
                    {
                        if (outerPanel.Tag is TimeoutState ts) ts.Timer?.Stop();
                        _pills.Remove(outerPanel);
                    }
                };
                actionsPanel.Children.Add(actionBtn);
            }
            cardContent.Children.Add(actionsPanel);
        }

        cardLayout.Children.Add(cardContent);
        card.Child = cardLayout;

        // Close button
        var closeBtn = new Button
        {
            Width = 25, Height = 25,
            CornerRadius = new CornerRadius(12.5),
            Background = AppColors.ElevatedBackgroundBrush,
            BorderBrush = AppColors.PaneBorderBrush,
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        closeBtn.Content = new TextBlock
        {
            Text = "\u2715",
            Foreground = AppColors.PrimaryTextBrush,
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        closeBtn.Click += (_, _) =>
        {
            if (outerPanel.Tag is TimeoutState ts) ts.Timer?.Stop();
            _pills.Remove(outerPanel);
        };

        outerPanel.Children.Add(card);
        outerPanel.Children.Add(closeBtn);

        // Hover to pause timeout
        outerPanel.PointerEntered += (_, _) =>
        {
            if (outerPanel.Tag is TimeoutState ts) ts.IsPaused = true;
        };
        outerPanel.PointerExited += (_, _) =>
        {
            if (outerPanel.Tag is TimeoutState ts) ts.IsPaused = false;
        };

        return (outerPanel, progressBar);
    }

    private static Border CreateSeverityIcon(NotificationSeverity severity)
    {
        var (text, color) = severity switch
        {
            NotificationSeverity.Info => ("i", AppColors.InfoIconColor),
            NotificationSeverity.Warning => ("!", AppColors.WarningIconColor),
            NotificationSeverity.Error => ("\u2716", AppColors.ErrorIconColor),
            _ => ("i", AppColors.InfoIconColor),
        };
        return new Border
        {
            Width = 25, Height = 25,
            CornerRadius = new CornerRadius(12.5),
            Background = new SolidColorBrush(color, 0.2),
            Child = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(color),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeight.Bold,
            },
        };
    }

    private static string FormatTime(long millis)
    {
        if (millis <= 0) return "";
        return DateTimeOffset.FromUnixTimeMilliseconds(millis).LocalDateTime.ToString("HH:mm:ss");
    }

    private class TimeoutState
    {
        public double RemainingMs { get; set; }
        public double TotalMs { get; set; }
        public bool IsPaused { get; set; }
        public DispatcherTimer? Timer { get; set; }
        public ProgressBar? ProgressBar { get; set; }
    }
}
