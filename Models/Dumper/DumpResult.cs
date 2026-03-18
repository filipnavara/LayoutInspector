using Schaumamal.Models.Repository;
using Schaumamal.ViewModels.Notifications;

namespace Schaumamal.Models.Dumper;

public abstract record DumpResult
{
    public record Error(Notification Notification) : DumpResult;
    public record Success(DumpData Dump) : DumpResult;
}
