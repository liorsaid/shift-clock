using System.Globalization;
using ShiftClock.Api.Data;
using ShiftClock.Api.History;

namespace ShiftClock.Tests.History;

public class ShiftHistoryTests
{
    [Fact]
    public void Does_not_count_a_missing_clock_out_or_an_open_shift()
    {
        Shift[] shifts =
        [
            Completed("2026-11-02T08:00:00+01:00", "2026-11-02T09:00:00+01:00"),
            MissingClockOut("2026-11-03T08:00:00+01:00"),
            Open("2026-11-04T08:00:00+01:00"),
        ];

        var month = Assert.Single(ShiftHistory.Months(shifts));

        Assert.Equal(Hours(1), month.TotalSeconds);
    }

    [Fact]
    public void Counts_a_shift_across_midnight_in_the_month_it_started()
    {
        Shift[] shifts = [Completed("2026-10-31T22:00:00+01:00", "2026-11-01T06:00:00+01:00")];

        var month = Assert.Single(ShiftHistory.Months(shifts));

        Assert.Equal(new ShiftHistory.MonthTotal(2026, 10, Hours(8)), month);
    }

    [Fact]
    public void Counts_the_real_length_of_a_shift_across_the_autumn_change()
    {
        Shift[] shifts = [Completed("2026-10-25T00:00:00+02:00", "2026-10-25T06:00:00+01:00")];

        var month = Assert.Single(ShiftHistory.Months(shifts));

        Assert.Equal(Hours(7), month.TotalSeconds);
    }

    private static Shift Completed(string clockInAt, string clockOutAt) =>
        new() { ClockInAt = Time(clockInAt), ClockOutAt = Time(clockOutAt) };

    private static Shift MissingClockOut(string clockInAt) => new() { ClockInAt = Time(clockInAt), IsMissingClockOut = true };

    private static Shift Open(string clockInAt) => new() { ClockInAt = Time(clockInAt) };

    private static double Hours(int hours) => TimeSpan.FromHours(hours).TotalSeconds;

    private static DateTimeOffset Time(string iso) => DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture);
}
