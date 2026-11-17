using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using ShiftClock.Api.Data;
using ShiftClock.Api.SignIn;
using ShiftClock.Tests.Data;

namespace ShiftClock.Tests.SignIn;

[Collection(DatabaseCollection.Name)]
public class SignInTests(TestDatabase database, ShiftClockApp app) : IClassFixture<ShiftClockApp>
{
    private const string Pin = "2468";

    [Fact]
    public async Task Signs_in_a_valid_employee_and_remembers_them()
    {
        var employee = await AddEmployeeAsync(isActive: true);
        var client = app.CreateClient();

        var response = await SignInAsync(client, employee.EmployeeNumber, Pin);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(employee.DisplayName, (await response.Content.ReadFromJsonAsync<SignInEndpoints.EmployeeResponse>())!.DisplayName);
        var signedIn = await client.GetFromJsonAsync<SignInEndpoints.EmployeeResponse>("/api/me");
        Assert.Equal(employee.EmployeeNumber, signedIn!.EmployeeNumber);
    }

    [Fact]
    public async Task Gives_the_same_response_for_a_wrong_pin_and_an_unknown_number()
    {
        var employee = await AddEmployeeAsync(isActive: true);

        var wrongPin = await SignInAsync(app.CreateClient(), employee.EmployeeNumber, "0000");
        var unknownNumber = await SignInAsync(app.CreateClient(), "no-such-number", Pin);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPin.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownNumber.StatusCode);
        Assert.Equal(await wrongPin.Content.ReadAsStringAsync(), await unknownNumber.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(Pin, HttpStatusCode.Forbidden)]
    [InlineData("0000", HttpStatusCode.Unauthorized)]
    public async Task Tells_an_inactive_employee_only_after_a_correct_pin(string pin, HttpStatusCode expectedStatus)
    {
        var employee = await AddEmployeeAsync(isActive: false);

        var response = await SignInAsync(app.CreateClient(), employee.EmployeeNumber, pin);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{\"employeeNumber\":")]
    public async Task Rejects_a_missing_or_malformed_body(string body)
    {
        var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        var response = await app.CreateClient().PostAsync("/api/sign-in", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Replaces_the_previous_employee_when_the_next_one_signs_in()
    {
        var previous = await AddEmployeeAsync(isActive: true);
        var next = await AddEmployeeAsync(isActive: true);
        var kiosk = app.CreateClient();
        await SignInAsync(kiosk, previous.EmployeeNumber, Pin);

        await SignInAsync(kiosk, next.EmployeeNumber, Pin);

        var signedIn = await kiosk.GetFromJsonAsync<SignInEndpoints.EmployeeResponse>("/api/me");
        Assert.Equal(next.EmployeeNumber, signedIn!.EmployeeNumber);
    }

    [Fact]
    public async Task Rejects_who_is_signed_in_without_a_cookie()
    {
        var response = await app.CreateClient().GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signs_out()
    {
        var employee = await AddEmployeeAsync(isActive: true);
        var client = app.CreateClient();
        await SignInAsync(client, employee.EmployeeNumber, Pin);

        var response = await client.PostAsync("/api/sign-out", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Signs_out_an_employee_deactivated_after_signing_in()
    {
        var employee = await AddEmployeeAsync(isActive: true);
        var client = app.CreateClient();
        await SignInAsync(client, employee.EmployeeNumber, Pin);

        await using (var db = database.CreateContext())
        {
            (await db.Employees.FindAsync(employee.Id))!.IsActive = false;
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Sets_an_http_only_same_site_strict_session_cookie()
    {
        var employee = await AddEmployeeAsync(isActive: true);

        var response = await SignInAsync(app.CreateClient(), employee.EmployeeNumber, Pin);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expires=", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("max-age=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Returns_only_the_employee_number_and_name()
    {
        var employee = await AddEmployeeAsync(isActive: true);

        var response = await SignInAsync(app.CreateClient(), employee.EmployeeNumber, Pin);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(["employeeNumber", "displayName"], body.RootElement.EnumerateObject().Select(property => property.Name));
    }

    private static Task<HttpResponseMessage> SignInAsync(HttpClient client, string employeeNumber, string pin) =>
        client.PostAsJsonAsync("/api/sign-in", new { employeeNumber, pin });

    private async Task<Employee> AddEmployeeAsync(bool isActive)
    {
        var employee = new Employee { EmployeeNumber = Guid.NewGuid().ToString("N")[..20], DisplayName = "Test Employee", IsActive = isActive };
        employee.PinHash = new PasswordHasher<Employee>().HashPassword(employee, Pin);
        await using var db = database.CreateContext();
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return employee;
    }
}
