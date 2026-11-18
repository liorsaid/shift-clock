using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using ShiftClock.Api.Data;
using ShiftClock.Api.SignIn;
using ShiftClock.Api.Time;

namespace ShiftClock.Api.Clock;

public static class ClockEndpoints
{
    public sealed record ClockInResponse(DateTimeOffset ClockInAt);

    public sealed record ClockOutResponse(DateTimeOffset ClockInAt, DateTimeOffset ClockOutAt);

    public static void MapClockEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/clock-in", ClockInAsync);
        app.MapPost("/api/clock-out", ClockOutAsync);
        app.MapPost("/api/forgot-clock-out", ForgotClockOutAsync);
    }

    private static async Task<IResult> ClockInAsync(HttpContext http, ShiftClockDbContext db, ITimeSource timeSource)
    {
        var employee = await SignedInEmployee.FindActiveAsync(http, db);
        if (employee is null) return await SignedInEmployee.SignOutAndRejectAsync(http);
        if (await FindOpenShiftAsync(db, employee) is not null) return Results.Conflict();

        var clockInAt = await AskTimeApiAsync(timeSource, http.RequestAborted);
        if (clockInAt is null) return TimeServiceUnavailable();

        db.Shifts.Add(new Shift { EmployeeId = employee.Id, ClockInAt = clockInAt.Value });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (ShiftClockDbContext.IsOneOpenShiftViolation(exception))
        {
            return Results.Conflict();
        }

        await http.SignOutAsync();
        return Results.Ok(new ClockInResponse(clockInAt.Value));
    }

    private static async Task<IResult> ClockOutAsync(HttpContext http, ShiftClockDbContext db, ITimeSource timeSource)
    {
        var employee = await SignedInEmployee.FindActiveAsync(http, db);
        if (employee is null) return await SignedInEmployee.SignOutAndRejectAsync(http);
        var openShift = await FindOpenShiftAsync(db, employee);
        if (openShift is null) return Results.Conflict();

        var clockOutAt = await AskTimeApiAsync(timeSource, http.RequestAborted);
        if (clockOutAt is null) return TimeServiceUnavailable();
        if (!EndsAfterClockIn(clockOutAt.Value, openShift)) return TimeServiceUnavailable();

        openShift.ClockOutAt = clockOutAt;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict();
        }

        await http.SignOutAsync();
        return Results.Ok(new ClockOutResponse(openShift.ClockInAt, clockOutAt.Value));
    }

    private static async Task<IResult> ForgotClockOutAsync(HttpContext http, ShiftClockDbContext db)
    {
        var employee = await SignedInEmployee.FindActiveAsync(http, db);
        if (employee is null) return await SignedInEmployee.SignOutAndRejectAsync(http);
        var openShift = await FindOpenShiftAsync(db, employee);
        if (openShift is null) return Results.Conflict();

        openShift.IsMissingClockOut = true;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict();
        }

        return Results.NoContent();
    }

    private static Task<Shift?> FindOpenShiftAsync(ShiftClockDbContext db, Employee employee) =>
        db.Shifts.SingleOrDefaultAsync(s => s.EmployeeId == employee.Id && s.ClockOutAt == null && !s.IsMissingClockOut);

    private static bool EndsAfterClockIn(DateTimeOffset clockOutAt, Shift openShift) => clockOutAt > openShift.ClockInAt;

    private static async Task<DateTimeOffset?> AskTimeApiAsync(ITimeSource timeSource, CancellationToken cancellationToken)
    {
        try
        {
            return await timeSource.GetZurichNowAsync(cancellationToken);
        }
        catch (TimeSourceUnavailableException)
        {
            return null;
        }
    }

    private static IResult TimeServiceUnavailable() => Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
}
