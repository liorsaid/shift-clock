using ShiftClock.Api.Data;

namespace ShiftClock.Api.History;

public static class ShiftHistory
{
    public sealed record MonthTotal(int Year, int Month, double TotalSeconds);

    public static IReadOnlyList<MonthTotal> Months(IReadOnlyCollection<Shift> shifts) =>
        shifts
            .Select(shift => (shift.ClockInAt.Year, shift.ClockInAt.Month))
            .Distinct()
            .OrderDescending()
            .Select(month => new MonthTotal(month.Year, month.Month, TotalWorked(ShiftsIn(shifts, month.Year, month.Month)).TotalSeconds))
            .ToList();

    public static IReadOnlyList<Shift> ShiftsIn(IEnumerable<Shift> shifts, int year, int month) =>
        shifts
            .Where(shift => StartedIn(shift, year, month))
            .OrderByDescending(shift => shift.ClockInAt)
            .ToList();

    private static bool StartedIn(Shift shift, int year, int month) =>
        shift.ClockInAt.Year == year && shift.ClockInAt.Month == month;

    private static TimeSpan TotalWorked(IEnumerable<Shift> shifts)
    {
        var total = TimeSpan.Zero;
        foreach (var shift in shifts)
        {
            if (shift.ClockOutAt is not null) total += shift.ClockOutAt.Value - shift.ClockInAt;
        }

        return total;
    }
}
