namespace Schaumamal.ViewModels.Notifications;

public record Notification(
    string Title, string Description, NotificationSeverity Severity,
    long Timestamp = -1, NotificationExitStrategy? ExitStrategy = null,
    List<NotificationAction>? Actions = null
)
{
    public long Timestamp { get; init; } = Timestamp == -1 ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : Timestamp;
    public NotificationExitStrategy ExitStrategy { get; init; } = ExitStrategy ?? NotificationExitStrategy.Manual.Instance;
    public List<NotificationAction> Actions { get; init; } = Actions ?? new List<NotificationAction>();
    public static readonly Notification Empty = new("", "", NotificationSeverity.Info);
}

public abstract record NotificationExitStrategy
{
    public record Timeout(TimeSpan Value) : NotificationExitStrategy;
    public record Manual : NotificationExitStrategy { public static readonly Manual Instance = new(); }
}

public record NotificationAction(string Title, Action Block, bool ShouldHideNotification = false);

public enum NotificationSeverity { Info, Warning, Error }
