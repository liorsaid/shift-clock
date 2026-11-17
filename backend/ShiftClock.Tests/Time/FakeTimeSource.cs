using ShiftClock.Api.Time;

namespace ShiftClock.Tests.Time;

public sealed class FakeTimeSource(DateTimeOffset now) : ITimeSource
{
    public DateTimeOffset Now { get; set; } = now;

    public bool IsUnavailable { get; set; }

    public Task<DateTimeOffset> GetZurichNowAsync(CancellationToken cancellationToken) =>
        IsUnavailable
            ? Task.FromException<DateTimeOffset>(new TimeSourceUnavailableException("fake failure"))
            : Task.FromResult(Now);
}
