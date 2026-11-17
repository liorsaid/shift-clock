namespace ShiftClock.Api.Time;

public interface ITimeSource
{
    Task<DateTimeOffset> GetZurichNowAsync(CancellationToken cancellationToken);
}
