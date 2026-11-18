using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using ShiftClock.Api.Data;

namespace ShiftClock.Api.SignIn;

public static class SignedInEmployee
{
    public static async Task<Employee?> FindActiveAsync(HttpContext http, ShiftClockDbContext db)
    {
        var signedInId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(signedInId, out var employeeId)) return null;

        var employee = await db.Employees.FindAsync(employeeId);
        if (employee is null || !employee.IsActive) return null;

        return employee;
    }

    public static async Task<IResult> SignOutAndRejectAsync(HttpContext http)
    {
        await http.SignOutAsync();
        return Results.Unauthorized();
    }
}
