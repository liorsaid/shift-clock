using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ShiftClock.Api.Time;

namespace ShiftClock.Tests.Time;

public class TimeApiTimeSourceTests
{
    private const string RealResponse =
        """{"date_time":"2026-11-17T18:55:18.574232+01:00","date":"2026-11-17","time":"18:55:18.574232","day_of_week":"Tuesday","dst_active":false,"timezone":"Europe/Zurich","utc_offset_seconds":3600}""";

    private const string Url = "https://timeapi.test/zurich";

    [Fact]
    public async Task Sends_a_single_get_to_the_configured_url()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, RealResponse);

        await Source(handler).GetZurichNowAsync(CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(new Uri(Url), request.RequestUri);
    }

    [Fact]
    public async Task Returns_the_api_time_with_its_offset_and_full_precision()
    {
        var time = await ReadAsync(RealResponse);

        Assert.Equal(Time("2026-11-17T18:55:18.574232+01:00"), time);
        Assert.Equal(TimeSpan.FromHours(1), time.Offset);
    }

    [Fact]
    public async Task Accepts_both_readings_of_the_repeated_autumn_hour_as_one_hour_apart()
    {
        var first = await ReadAsync(Response("2026-10-25T02:30:00+02:00", 7200));
        var second = await ReadAsync(Response("2026-10-25T02:30:00+01:00", 3600));

        Assert.Equal(TimeSpan.FromHours(1), second - first);
    }

    [Theory]
    [InlineData("2026-03-29T01:59:59.999999+01:00", 3600)]
    [InlineData("2026-03-29T03:00:00+02:00", 7200)]
    [InlineData("2026-10-25T02:59:59.999999+02:00", 7200)]
    [InlineData("2026-10-25T03:00:00+01:00", 3600)]
    public async Task Accepts_times_at_the_edges_of_a_daylight_saving_change(string dateTime, int utcOffsetSeconds)
    {
        var time = await ReadAsync(Response(dateTime, utcOffsetSeconds));

        Assert.Equal(Time(dateTime), time);
        Assert.Equal(TimeSpan.FromSeconds(utcOffsetSeconds), time.Offset);
    }

    [Fact]
    public async Task Rejects_an_unsuccessful_status()
    {
        await AssertUnavailableAsync(StubHttpMessageHandler.Returning(HttpStatusCode.InternalServerError, RealResponse));
    }

    [Fact]
    public async Task Rejects_a_failed_request()
    {
        await AssertUnavailableAsync(StubHttpMessageHandler.Throwing(new HttpRequestException("connection refused")));
    }

    [Fact]
    public async Task Rejects_a_response_with_an_unknown_charset()
    {
        var content = new StringContent(RealResponse);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "no-such-charset" };

        await AssertUnavailableAsync(new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content })));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"date_time":"2026-11-17T18:55:18+01:00","timezone":"Europe/Zurich","utc_offset_seconds":"3600"}""")]
    [InlineData("""{"timezone":"Europe/Zurich","utc_offset_seconds":3600}""")]
    public async Task Rejects_a_malformed_or_incomplete_body(string body)
    {
        await AssertUnavailableAsync(body);
    }

    [Fact]
    public async Task Rejects_a_date_time_without_an_offset()
    {
        await AssertUnavailableAsync(Response("2026-11-17T18:55:18.574232"));
    }

    [Fact]
    public async Task Rejects_the_wrong_time_zone()
    {
        await AssertUnavailableAsync(Response("2026-11-17T18:55:18+01:00", timezone: "Europe/Berlin"));
    }

    [Fact]
    public async Task Rejects_an_offset_that_differs_from_utc_offset_seconds()
    {
        await AssertUnavailableAsync(Response("2026-11-17T18:55:18+01:00", 7200));
    }

    [Theory]
    [InlineData("2026-07-01T12:00:00+01:00", 3600)]
    [InlineData("2026-11-17T18:55:18+02:00", 7200)]
    [InlineData("2026-03-29T03:00:00+01:00", 3600)]
    [InlineData("2026-10-25T03:00:00+02:00", 7200)]
    public async Task Rejects_an_offset_that_breaks_zurich_rules(string dateTime, int utcOffsetSeconds)
    {
        await AssertUnavailableAsync(Response(dateTime, utcOffsetSeconds));
    }

    [Theory]
    [InlineData("2026-03-29T02:30:00+01:00", 3600)]
    [InlineData("2026-03-29T02:30:00+02:00", 7200)]
    public async Task Rejects_a_time_in_the_skipped_spring_hour(string dateTime, int utcOffsetSeconds)
    {
        await AssertUnavailableAsync(Response(dateTime, utcOffsetSeconds));
    }

    [Fact]
    public async Task Rejects_a_response_slower_than_the_timeout()
    {
        var source = Source(StubHttpMessageHandler.NeverResponding(), TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAsync<TimeSourceUnavailableException>(() => source.GetZurichNowAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Passes_a_caller_cancellation_through_as_cancellation()
    {
        var source = Source(StubHttpMessageHandler.NeverResponding());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.GetZurichNowAsync(new CancellationToken(canceled: true)));
    }

    private static string Response(string dateTime, int utcOffsetSeconds = 3600, string timezone = "Europe/Zurich") =>
        JsonSerializer.Serialize(new { date_time = dateTime, timezone, utc_offset_seconds = utcOffsetSeconds });

    private static TimeApiTimeSource Source(HttpMessageHandler handler, TimeSpan? timeout = null) =>
        new(timeout is null ? new HttpClient(handler) : new HttpClient(handler) { Timeout = timeout.Value },
            Options.Create(new TimeApiOptions { Url = Url }),
            NullLogger<TimeApiTimeSource>.Instance);

    private static Task<DateTimeOffset> ReadAsync(string body) =>
        Source(StubHttpMessageHandler.Returning(HttpStatusCode.OK, body)).GetZurichNowAsync(CancellationToken.None);

    private static Task AssertUnavailableAsync(string body) =>
        AssertUnavailableAsync(StubHttpMessageHandler.Returning(HttpStatusCode.OK, body));

    private static Task AssertUnavailableAsync(HttpMessageHandler handler) =>
        Assert.ThrowsAsync<TimeSourceUnavailableException>(() => Source(handler).GetZurichNowAsync(CancellationToken.None));

    private static DateTimeOffset Time(string iso) => DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture);
}
