using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftClock.Api.Clock;
using ShiftClock.Api.Data;
using ShiftClock.Api.Time;
using ShiftClock.Tests.Data;

namespace ShiftClock.Tests.Clock;

[Collection(DatabaseCollection.Name)]
public class ClockTests : IClassFixture<ShiftClockApp>
{
    private static readonly DateTimeOffset Morning = Time("2026-11-17T08:00:00+01:00");
    private static readonly DateTimeOffset Evening = Time("2026-11-17T17:00:00+01:00");

    private readonly TestDatabase database;
    private readonly ShiftClockApp app;

    public ClockTests(TestDatabase database, ShiftClockApp app)
    {
        this.database = database;
        this.app = app;
        app.TimeSource.Now = Morning;
        app.TimeSource.IsUnavailable = false;
    }

    [Fact]
    public async Task Clocks_in_with_the_api_time_and_signs_out()
    {
        var employee = await database.AddEmployeeAsync();
        var client = await SignedInClientAsync(employee);

        var response = await client.PostAsync("/api/clock-in", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var clockIn = await response.Content.ReadFromJsonAsync<ClockEndpoints.ClockInResponse>();
        Assert.Equal(Morning, clockIn!.ClockInAt);
        Assert.Equal(Morning.Offset, clockIn.ClockInAt.Offset);
        var shift = Assert.Single(await ShiftsOfAsync(employee));
        Assert.Equal(Morning, shift.ClockInAt);
        await AssertSignedOutAsync(client);
    }

    [Fact]
    public async Task Rejects_a_second_clock_in_without_asking_the_time_api()
    {
        var employee = await database.AddEmployeeAsync();
        await ClockInAsync(employee);
        var client = await SignedInClientAsync(employee);
        app.TimeSource.IsUnavailable = true;

        var response = await client.PostAsync("/api/clock-in", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(await ShiftsOfAsync(employee));
    }

    [Fact]
    public async Task Clocks_out_with_the_api_time_and_signs_out()
    {
        var employee = await database.AddEmployeeAsync();
        await ClockInAsync(employee);
        var client = await SignedInClientAsync(employee);
        app.TimeSource.Now = Evening;

        var response = await client.PostAsync("/api/clock-out", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var clockOut = await response.Content.ReadFromJsonAsync<ClockEndpoints.ClockOutResponse>();
        Assert.Equal(Morning, clockOut!.ClockInAt);
        Assert.Equal(Evening, clockOut.ClockOutAt);
        var shift = Assert.Single(await ShiftsOfAsync(employee));
        Assert.Equal(Evening, shift.ClockOutAt);
        await AssertSignedOutAsync(client);
    }

    [Fact]
    public async Task Rejects_a_clock_out_with_no_open_shift_without_asking_the_time_api()
    {
        var employee = await database.AddEmployeeAsync();
        var client = await SignedInClientAsync(employee);
        app.TimeSource.IsUnavailable = true;

        var response = await client.PostAsync("/api/clock-out", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(await ShiftsOfAsync(employee));
    }

    [Fact]
    public async Task Records_nothing_and_stays_signed_in_when_the_time_api_is_down_on_clock_in()
    {
        var employee = await database.AddEmployeeAsync();
        var client = await SignedInClientAsync(employee);
        app.TimeSource.IsUnavailable = true;

        var response = await client.PostAsync("/api/clock-in", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Empty(await ShiftsOfAsync(employee));
        await AssertSignedInAsync(client);
    }

    [Fact]
    public async Task Records_nothing_and_stays_signed_in_when_the_time_api_is_down_on_clock_out()
    {
        var employee = await database.AddEmployeeAsync();
        await ClockInAsync(employee);
        var client = await SignedInClientAsync(employee);
        app.TimeSource.IsUnavailable = true;

        var response = await client.PostAsync("/api/clock-out", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var shift = Assert.Single(await ShiftsOfAsync(employee));
        Assert.Null(shift.ClockOutAt);
        await AssertSignedInAsync(client);
    }

    [Fact]
    public async Task Rejects_an_api_time_at_or_before_the_clock_in()
    {
        var employee = await database.AddEmployeeAsync();
        await ClockInAsync(employee);
        var client = await SignedInClientAsync(employee);
        app.TimeSource.Now = Morning;

        var response = await client.PostAsync("/api/clock-out", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var shift = Assert.Single(await ShiftsOfAsync(employee));
        Assert.Null(shift.ClockOutAt);
    }

    [Fact]
    public async Task Clocks_out_in_the_repeated_autumn_hour_when_the_api_time_reads_earlier_than_the_clock_in()
    {
        var clockInBeforeTheChange = Time("2026-10-25T02:30:00+02:00");
        var clockOutAfterTheChange = Time("2026-10-25T02:10:00+01:00");
        var employee = await database.AddEmployeeAsync();
        app.TimeSource.Now = clockInBeforeTheChange;
        await ClockInAsync(employee);
        var client = await SignedInClientAsync(employee);
        app.TimeSource.Now = clockOutAfterTheChange;

        var response = await client.PostAsync("/api/clock-out", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var shift = Assert.Single(await ShiftsOfAsync(employee));
        Assert.Equal(TimeSpan.FromHours(2), shift.ClockInAt.Offset);
        Assert.Equal(TimeSpan.FromHours(1), shift.ClockOutAt!.Value.Offset);
        Assert.Equal(TimeSpan.FromMinutes(40), shift.ClockOutAt.Value - shift.ClockInAt);
    }

    [Fact]
    public async Task Marks_a_forgotten_clock_out_without_a_time_and_lets_the_employee_clock_in_again()
    {
        var employee = await database.AddEmployeeAsync();
        await ClockInAsync(employee);
        var client = await SignedInClientAsync(employee);
        app.TimeSource.IsUnavailable = true;

        var response = await client.PostAsync("/api/forgot-clock-out", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var forgotten = Assert.Single(await ShiftsOfAsync(employee));
        Assert.True(forgotten.IsMissingClockOut);
        Assert.Null(forgotten.ClockOutAt);
        app.TimeSource.IsUnavailable = false;
        app.TimeSource.Now = Evening;
        var clockIn = await client.PostAsync("/api/clock-in", null);
        Assert.Equal(HttpStatusCode.OK, clockIn.StatusCode);
    }

    [Fact]
    public async Task Rejects_forgot_with_no_open_shift()
    {
        var employee = await database.AddEmployeeAsync();
        var client = await SignedInClientAsync(employee);

        var response = await client.PostAsync("/api/forgot-clock-out", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/clock-in")]
    [InlineData("/api/clock-out")]
    [InlineData("/api/forgot-clock-out")]
    public async Task Rejects_a_request_without_a_cookie(string url)
    {
        var response = await app.CreateClient().PostAsync(url, null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Accepts_only_one_of_two_clock_ins_at_once()
    {
        var employee = await database.AddEmployeeAsync();
        var firstTab = await SignedInClientAsync(employee);
        var secondTab = await SignedInClientAsync(employee);

        var responses = await Task.WhenAll(firstTab.PostAsync("/api/clock-in", null), secondTab.PostAsync("/api/clock-in", null));

        var statuses = responses.Select(response => response.StatusCode).Order();
        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Conflict], statuses);
        Assert.Single(await ShiftsOfAsync(employee));
    }

    [Fact]
    public async Task Accepts_only_one_of_two_clock_outs_at_once()
    {
        var employee = await database.AddEmployeeAsync();
        await ClockInAsync(employee);
        var firstTab = await SignedInClientAsync(employee);
        var secondTab = await SignedInClientAsync(employee);
        app.TimeSource.Now = Evening;

        var responses = await Task.WhenAll(firstTab.PostAsync("/api/clock-out", null), secondTab.PostAsync("/api/clock-out", null));

        var statuses = responses.Select(response => response.StatusCode).Order();
        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Conflict], statuses);
        var shift = Assert.Single(await ShiftsOfAsync(employee));
        Assert.Equal(Evening, shift.ClockOutAt);
    }

    [Fact]
    public void Gives_the_time_api_a_three_second_timeout()
    {
        var clients = app.Services.GetRequiredService<IHttpClientFactory>();

        var timeApiClient = clients.CreateClient(nameof(ITimeSource));

        Assert.Equal(TimeSpan.FromSeconds(3), timeApiClient.Timeout);
    }

    private async Task<HttpClient> SignedInClientAsync(Employee employee)
    {
        var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/sign-in", new { employee.EmployeeNumber, pin = TestDatabase.Pin });
        response.EnsureSuccessStatusCode();
        return client;
    }

    private async Task ClockInAsync(Employee employee)
    {
        var client = await SignedInClientAsync(employee);
        var response = await client.PostAsync("/api/clock-in", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task<List<Shift>> ShiftsOfAsync(Employee employee)
    {
        await using var db = database.CreateContext();
        return await db.Shifts.Where(s => s.EmployeeId == employee.Id).ToListAsync();
    }

    private static async Task AssertSignedOutAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task AssertSignedInAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static DateTimeOffset Time(string iso) => DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture);
}
