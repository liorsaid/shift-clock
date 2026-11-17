using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace ShiftClock.Api.Time;

public sealed class TimeApiTimeSource(HttpClient httpClient, IOptions<TimeApiOptions> options, ILogger<TimeApiTimeSource> logger)
    : ITimeSource
{
    private const string Zurich = "Europe/Zurich";
    private static readonly TimeZoneInfo ZurichTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Zurich);
    private static readonly Regex OffsetAtEnd = new(@"(Z|[+-]\d{2}:\d{2})$");

    public async Task<DateTimeOffset> GetZurichNowAsync(CancellationToken cancellationToken)
    {
        try
        {
            var body = await FetchAsync(cancellationToken);
            return Parse(body);
        }
        catch (TimeSourceUnavailableException exception)
        {
            logger.LogWarning(exception, "Time API failed");
            throw;
        }
    }

    private async Task<string> FetchAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(options.Value.Url, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new TimeSourceUnavailableException($"HTTP {(int)response.StatusCode}");
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException exception) when (IsTimeout(cancellationToken))
        {
            throw new TimeSourceUnavailableException("timed out", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new TimeSourceUnavailableException("request failed", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new TimeSourceUnavailableException("response could not be read", exception);
        }
    }

    private static DateTimeOffset Parse(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            var timeZone = json.RootElement.GetProperty("timezone").GetString();
            var dateTime = json.RootElement.GetProperty("date_time");
            var reportedOffset = TimeSpan.FromSeconds(json.RootElement.GetProperty("utc_offset_seconds").GetInt32());

            if (timeZone != Zurich) throw new TimeSourceUnavailableException($"wrong time zone '{timeZone}'");
            if (!HasOffsetFromApi(dateTime.GetString())) throw new TimeSourceUnavailableException("date_time has no explicit offset");
            if (!dateTime.TryGetDateTimeOffset(out var time)) throw new TimeSourceUnavailableException("date_time is not a timestamp");
            if (time.Offset != reportedOffset) throw new TimeSourceUnavailableException("offset differs from utc_offset_seconds");
            if (time.Offset != ZurichTimeZone.GetUtcOffset(time)) throw new TimeSourceUnavailableException("offset breaks Zurich rules");

            return time;
        }
        catch (Exception exception) when (IsMalformedBody(exception))
        {
            throw new TimeSourceUnavailableException("malformed response", exception);
        }
    }

    private static bool IsTimeout(CancellationToken callerToken) => !callerToken.IsCancellationRequested;

    private static bool HasOffsetFromApi(string? dateTime) => dateTime is not null && OffsetAtEnd.IsMatch(dateTime);

    private static bool IsMalformedBody(Exception exception) =>
        exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException;
}
