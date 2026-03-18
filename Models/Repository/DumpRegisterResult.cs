using Schaumamal.ViewModels.Notifications;

namespace Schaumamal.Models.Repository;

public abstract record DumpRegisterResult
{
    public record Error(Notification Notification) : DumpRegisterResult;
    public record Success(Content Content) : DumpRegisterResult;
}
