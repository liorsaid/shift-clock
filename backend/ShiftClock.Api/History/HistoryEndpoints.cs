using Microsoft.EntityFrameworkCore;
using ShiftClock.Api.Data;
using ShiftClock.Api.SignIn;

namespace ShiftClock.Api.History;

public static class HistoryEndpoints
{
    public sealed record ShiftResponse(DateTimeOffset ClockInAt, DateTimeOffset? ClockOutAt, bool IsMissingClockOut);

    public static void MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/shifts/months", GetMonthsAsync);
        app.MapGet("/api/shifts", GetShiftsOfMonthAsync);
    }

    private static async Task<IResult> GetMonthsAsync(HttpContext http, ShiftClockDbContext db)
    {
        var employee = await SignedInEmployee.FindActiveAsync(http, db);
        if (employee is null) return await SignedInEmployee.SignOutAndRejectAsync(http);

        var shifts = await LoadShiftsAsync(db, employee);
        return Results.Ok(ShiftHistory.Months(shifts));
    }

    private static async Task<IResult> GetShiftsOfMonthAsync(int? year, int? month, HttpContext http, ShiftClockDbContext db)
    {
        var employee = await SignedInEmployee.FindActiveAsync(http, db);
        if (employee is null) return await SignedInEmployee.SignOutAndRejectAsync(http);
        if (year is null || month is null || !IsCalendarMonth(month.Value)) return Results.BadRequest();

        var shifts = await LoadShiftsAsync(db, employee);
        var shiftsOfMonth = ShiftHistory.ShiftsIn(shifts, year.Value, month.Value);
        return Results.Ok(shiftsOfMonth.Select(ToResponse));
    }

    private static Task<List<Shift>> LoadShiftsAsync(ShiftClockDbContext db, Employee employee) =>
        db.Shifts.AsNoTracking().Where(s => s.EmployeeId == employee.Id).ToListAsync();

    private static bool IsCalendarMonth(int month) => month >= 1 && month <= 12;

    private static ShiftResponse ToResponse(Shift shift) => new(shift.ClockInAt, shift.ClockOutAt, shift.IsMissingClockOut);
}
