using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ShiftClock.Api.SignIn;
using ShiftClock.Tests.Data;

namespace ShiftClock.Tests.SignIn;

[Collection(DatabaseCollection.Name)]
public class SignInTests(TestDatabase database, ShiftClockApp app) : IClassFixture<ShiftClockApp>
{
    private const string Pin = TestDatabase.Pin;
    private const string WrongPin = "0000";

    [Fact]
    public async Task Signs_in_a_valid_employee_and_remembers_them()
    {
        var employee = await database.AddEmployeeAsync();
        var client = app.CreateClient();

        var response = await SignInAsync(client, employee.EmployeeNumber, Pin);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var signedInResponse = await response.Content.ReadFromJsonAsync<SignInEndpoints.EmployeeResponse>();
        Assert.Equal(employee.DisplayName, signedInResponse!.DisplayName);
        var signedIn = await client.GetFromJsonAsync<SignInEndpoints.EmployeeResponse>("/api/me");
        Assert.Equal(employee.EmployeeNumber, signedIn!.EmployeeNumber);
    }

    [Fact]
    public async Task Gives_the_same_response_for_a_wrong_pin_and_an_unknown_number()
    {
        var employee = await database.AddEmployeeAsync();

        var wrongPin = await SignInAsync(app.CreateClient(), employee.EmployeeNumber, WrongPin);
        var unknownNumber = await SignInAsync(app.CreateClient(), "no-such-number", Pin);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPin.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownNumber.StatusCode);
        var wrongPinBody = await wrongPin.Content.ReadAsStringAsync();
        var unknownNumberBody = await unknownNumber.Content.ReadAsStringAsync();
        Assert.Equal(wrongPinBody, unknownNumberBody);
    }

    [Theory]
    [InlineData(Pin, HttpStatusCode.Forbidden)]
    [InlineData(WrongPin, HttpStatusCode.Unauthorized)]
    public async Task Tells_an_inactive_employee_only_after_a_correct_pin(string pin, HttpStatusCode expectedStatus)
    {
        var employee = await database.AddEmployeeAsync(isActive: false);

        var response = await SignInAsync(app.CreateClient(), employee.EmployeeNumber, pin);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{\"employeeNumber\":")]
    public async Task Rejects_no_body_or_invalid_json(string body)
    {
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await app.CreateClient().PostAsync("/api/sign-in", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Replaces_the_previous_employee_when_the_next_one_signs_in()
    {
        var previous = await database.AddEmployeeAsync();
        var next = await database.AddEmployeeAsync();
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
        var employee = await database.AddEmployeeAsync();
        var client = app.CreateClient();
        await SignInAsync(client, employee.EmployeeNumber, Pin);

        var response = await client.PostAsync("/api/sign-out", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var afterSignOut = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterSignOut.StatusCode);
    }

    [Fact]
    public async Task Signs_out_an_employee_deactivated_after_signing_in()
    {
        var employee = await database.AddEmployeeAsync();
        var client = app.CreateClient();
        await SignInAsync(client, employee.EmployeeNumber, Pin);

        await using (var db = database.CreateContext())
        {
            var stored = await db.Employees.FindAsync(employee.Id);
            stored!.IsActive = false;
            await db.SaveChangesAsync();
        }

        var afterDeactivation = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterDeactivation.StatusCode);
    }

    [Fact]
    public async Task Sets_an_http_only_same_site_strict_session_cookie()
    {
        var employee = await database.AddEmployeeAsync();

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
        var employee = await database.AddEmployeeAsync();

        var response = await SignInAsync(app.CreateClient(), employee.EmployeeNumber, Pin);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var propertyNames = body.RootElement.EnumerateObject().Select(property => property.Name);
        Assert.Equal(["employeeNumber", "displayName"], propertyNames);
    }

    private static Task<HttpResponseMessage> SignInAsync(HttpClient client, string employeeNumber, string pin) =>
        client.PostAsJsonAsync("/api/sign-in", new { employeeNumber, pin });
}
