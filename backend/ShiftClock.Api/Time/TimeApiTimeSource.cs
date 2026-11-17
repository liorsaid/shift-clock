using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace ShiftClock.Api.Time;

public sealed class TimeApiTimeSource(HttpClient httpClient, IOptions<TimeApiOptions> options, ILogger<TimeApiTimeSource> logger)
    : ITimeSource
{
    private const string Zurich = "Europe/Zurich";
    private static readonly TimeZoneInfo ZurichTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Zurich);
    private static readonly Regex EndsWithOffset = new(@"(Z|[+-]\d{2}:\d{2})$");

    public async Task<DateTimeOffset> GetZurichNowAsync(CancellationToken cancellationToken)
    {
        try
        {
            return Parse(await FetchAsync(cancellationToken));
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
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeSourceUnavailableException("timed out", exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            throw new TimeSourceUnavailableException("request failed", exception);
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
            if (!EndsWithOffset.IsMatch(dateTime.GetString() ?? "") || !dateTime.TryGetDateTimeOffset(out var time))
                throw new TimeSourceUnavailableException("date_time is not a timestamp with an explicit offset");
            if (time.Offset != reportedOffset) throw new TimeSourceUnavailableException("offset differs from utc_offset_seconds");
            if (time.Offset != ZurichTimeZone.GetUtcOffset(time)) throw new TimeSourceUnavailableException("offset breaks Zurich rules");

            return time;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new TimeSourceUnavailableException("malformed response", exception);
        }
    }
}
