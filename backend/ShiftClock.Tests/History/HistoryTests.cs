using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using ShiftClock.Api.Clock;
using ShiftClock.Api.Data;
using ShiftClock.Api.History;
using ShiftClock.Tests.Data;

namespace ShiftClock.Tests.History;

[Collection(DatabaseCollection.Name)]
public class HistoryTests : IClassFixture<ShiftClockApp>
{
    private static readonly DateTimeOffset Morning = Time("2026-11-17T08:00:00+01:00");
    private static readonly DateTimeOffset Evening = Time("2026-11-17T17:00:00+01:00");

    private readonly TestDatabase database;
    private readonly ShiftClockApp app;

    public HistoryTests(TestDatabase database, ShiftClockApp app)
    {
        this.database = database;
        this.app = app;
        app.TimeSource.IsUnavailable = true;
    }

    [Fact]
    public async Task Shows_status_and_history_while_the_time_api_is_down()
    {
        var employee = await database.AddEmployeeAsync();
        await AddShiftAsync(new Shift { EmployeeId = employee.Id, ClockInAt = Morning });
        var client = await SignedInClientAsync(employee);

        var status = await client.GetFromJsonAsync<ClockEndpoints.StatusResponse>("/api/status");
        var months = await client.GetFromJsonAsync<List<ShiftHistory.MonthTotal>>("/api/shifts/months");
        var shifts = await client.GetFromJsonAsync<List<HistoryEndpoints.ShiftResponse>>("/api/shifts?year=2026&month=11");

        Assert.Equal(Morning, status!.OnShiftSince);
        Assert.Equal(new ShiftHistory.MonthTotal(2026, 11, 0), Assert.Single(months!));
        Assert.Equal(new HistoryEndpoints.ShiftResponse(Morning, null, false), Assert.Single(shifts!));
    }

    [Fact]
    public async Task Shows_only_the_signed_in_employees_shifts()
    {
        var employee = await database.AddEmployeeAsync();
        var colleague = await database.AddEmployeeAsync();
        await AddShiftAsync(new Shift { EmployeeId = employee.Id, ClockInAt = Morning, ClockOutAt = Evening });
        await AddShiftAsync(new Shift { EmployeeId = colleague.Id, ClockInAt = Morning.AddDays(1), ClockOutAt = Evening.AddDays(1) });
        var client = await SignedInClientAsync(employee);

        var months = await client.GetFromJsonAsync<List<ShiftHistory.MonthTotal>>("/api/shifts/months");
        var shifts = await client.GetFromJsonAsync<List<HistoryEndpoints.ShiftResponse>>("/api/shifts?year=2026&month=11");

        var month = Assert.Single(months!);
        Assert.Equal(new ShiftHistory.MonthTotal(2026, 11, TimeSpan.FromHours(9).TotalSeconds), month);
        var shift = Assert.Single(shifts!);
        Assert.Equal(new HistoryEndpoints.ShiftResponse(Morning, Evening, false), shift);
    }

    [Theory]
    [InlineData("/api/status")]
    [InlineData("/api/shifts/months")]
    [InlineData("/api/shifts?year=2026&month=11")]
    public async Task Rejects_a_request_without_a_cookie(string url)
    {
        var response = await app.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> SignedInClientAsync(Employee employee)
    {
        var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/sign-in", new { employee.EmployeeNumber, pin = TestDatabase.Pin });
        response.EnsureSuccessStatusCode();
        return client;
    }

    private async Task AddShiftAsync(Shift shift)
    {
        await using var db = database.CreateContext();
        db.Shifts.Add(shift);
        await db.SaveChangesAsync();
    }

    private static DateTimeOffset Time(string iso) => DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture);
}
