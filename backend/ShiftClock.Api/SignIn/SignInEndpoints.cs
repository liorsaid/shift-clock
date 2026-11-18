using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShiftClock.Api.Data;

namespace ShiftClock.Api.SignIn;

public static class SignInEndpoints
{
    private static readonly PasswordHasher<Employee> PinHasher = new();
    private static readonly Employee UnknownEmployee = new() { EmployeeNumber = "", DisplayName = "" };
    private static readonly string UnknownEmployeePinHash = PinHasher.HashPassword(UnknownEmployee, "unknown");

    public sealed record SignInRequest(string? EmployeeNumber, string? Pin);

    public sealed record EmployeeResponse(string EmployeeNumber, string DisplayName);

    public static void MapSignInEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/sign-in", SignInAsync);
        app.MapGet("/api/me", GetSignedInEmployeeAsync);
        app.MapPost("/api/sign-out", async (HttpContext http) =>
        {
            await http.SignOutAsync();
            return Results.NoContent();
        });
    }

    private static async Task<IResult> SignInAsync(SignInRequest request, ShiftClockDbContext db, HttpContext http)
    {
        var employee = await db.Employees.SingleOrDefaultAsync(e => e.EmployeeNumber == request.EmployeeNumber);
        var pinMatches = CheckPinWithoutRevealingUnknownNumbers(employee, request.Pin);
        if (employee is null || !pinMatches) return Results.Unauthorized();
        if (!employee.IsActive) return Results.StatusCode(StatusCodes.Status403Forbidden);

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString())],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(new ClaimsPrincipal(identity));
        return Results.Ok(ToResponse(employee));
    }

    private static async Task<IResult> GetSignedInEmployeeAsync(HttpContext http, ShiftClockDbContext db)
    {
        var employee = await SignedInEmployee.FindActiveAsync(http, db);
        if (employee is null) return await SignedInEmployee.SignOutAndRejectAsync(http);

        return Results.Ok(ToResponse(employee));
    }

    private static bool CheckPinWithoutRevealingUnknownNumbers(Employee? employee, string? pin)
    {
        var pinHash = employee?.PinHash ?? UnknownEmployeePinHash;
        var result = PinHasher.VerifyHashedPassword(employee ?? UnknownEmployee, pinHash, pin ?? "");
        return result != PasswordVerificationResult.Failed;
    }

    private static EmployeeResponse ToResponse(Employee employee) => new(employee.EmployeeNumber, employee.DisplayName);
}
