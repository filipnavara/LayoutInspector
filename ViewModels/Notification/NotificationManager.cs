using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Schaumamal.ViewModels.Notifications;

public class NotificationManager
{
    private readonly Subject<Notification> _notifications = new();
    public IObservable<Notification> Notifications => _notifications.AsObservable();
    public void Notify(Notification notification) => _notifications.OnNext(notification);
}
