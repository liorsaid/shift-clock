using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ShiftClock.Api.Data;

namespace ShiftClock.Tests.Data;

[Collection(DatabaseCollection.Name)]
public class ShiftRulesTests(TestDatabase database)
{
    private const string OneSecondBeforeMorning = "2026-11-17T07:59:59+01:00";
    private const string MorningInUtc = "2026-11-17T07:00:00Z";

    private static readonly DateTimeOffset Morning = Time("2026-11-17T08:00:00+01:00");
    private static readonly DateTimeOffset Evening = Time("2026-11-17T17:00:00+01:00");

    [Fact]
    public async Task Rejects_a_second_open_shift_for_the_same_employee()
    {
        var employee = await database.AddEmployeeAsync();
        await AddShiftAsync(new Shift { EmployeeId = employee.Id, ClockInAt = Morning });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            AddShiftAsync(new Shift { EmployeeId = employee.Id, ClockInAt = Evening }));
        Assert.True(ShiftClockDbContext.IsOneOpenShiftViolation(exception));
    }

    [Fact]
    public async Task Allows_open_shifts_for_different_employees()
    {
        var first = await database.AddEmployeeAsync();
        var second = await database.AddEmployeeAsync();

        await AddShiftAsync(new Shift { EmployeeId = first.Id, ClockInAt = Morning });
        await AddShiftAsync(new Shift { EmployeeId = second.Id, ClockInAt = Morning });
    }

    [Fact]
    public async Task Allows_a_new_shift_after_a_missing_clock_out()
    {
        var employee = await database.AddEmployeeAsync();
        await AddShiftAsync(new Shift { EmployeeId = employee.Id, ClockInAt = Morning, IsMissingClockOut = true });

        await AddShiftAsync(new Shift { EmployeeId = employee.Id, ClockInAt = Evening });
    }

    [Fact]
    public async Task Allows_a_new_shift_after_a_clocked_out_shift()
    {
        var employee = await database.AddEmployeeAsync();
        await AddShiftAsync(new Shift { EmployeeId = employee.Id, ClockInAt = Morning, ClockOutAt = Time("2026-11-17T12:00:00+01:00") });

        await AddShiftAsync(new Shift { EmployeeId = employee.Id, ClockInAt = Evening });
    }

    [Fact]
    public async Task Allows_a_clock_out_in_the_repeated_autumn_hour_that_reads_earlier_than_the_clock_in()
    {
        var employee = await database.AddEmployeeAsync();
        var shift = new Shift
        {
            EmployeeId = employee.Id,
            ClockInAt = Time("2026-10-25T02:30:00+02:00"),
            ClockOutAt = Time("2026-10-25T02:10:00+01:00"),
        };

        await AddShiftAsync(shift);
    }

    [Fact]
    public async Task Rejects_a_second_employee_with_the_same_employee_number()
    {
        var employeeNumber = TestDatabase.UniqueEmployeeNumber();
        await using var db = database.CreateContext();
        db.Employees.Add(new Employee { EmployeeNumber = employeeNumber, DisplayName = "First", IsActive = true });
        await db.SaveChangesAsync();
        db.Employees.Add(new Employee { EmployeeNumber = employeeNumber, DisplayName = "Second", IsActive = true });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.False(ShiftClockDbContext.IsOneOpenShiftViolation(exception));
    }

    [Theory]
    [InlineData(OneSecondBeforeMorning)]
    [InlineData(MorningInUtc)]
    public async Task Rejects_a_clock_out_before_or_at_the_clock_in(string clockOutAt)
    {
        var employee = await database.AddEmployeeAsync();
        var shift = new Shift { EmployeeId = employee.Id, ClockInAt = Morning, ClockOutAt = Time(clockOutAt) };

        await Assert.ThrowsAsync<DbUpdateException>(() => AddShiftAsync(shift));
    }

    [Fact]
    public async Task Rejects_a_missing_clock_out_shift_with_a_clock_out_time()
    {
        var employee = await database.AddEmployeeAsync();
        var shift = new Shift { EmployeeId = employee.Id, ClockInAt = Morning, ClockOutAt = Evening, IsMissingClockOut = true };

        await Assert.ThrowsAsync<DbUpdateException>(() => AddShiftAsync(shift));
    }

    [Fact]
    public async Task Rejects_the_second_of_two_simultaneous_clock_outs()
    {
        var employee = await database.AddEmployeeAsync();
        var shiftId = await AddShiftAsync(new Shift { EmployeeId = employee.Id, ClockInAt = Morning });
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var firstCopy = await first.Shifts.SingleAsync(s => s.Id == shiftId);
        var secondCopy = await second.Shifts.SingleAsync(s => s.Id == shiftId);

        firstCopy.ClockOutAt = Evening;
        await first.SaveChangesAsync();
        secondCopy.ClockOutAt = Evening.AddMinutes(1);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    private async Task<int> AddShiftAsync(Shift shift)
    {
        await using var db = database.CreateContext();
        db.Shifts.Add(shift);
        await db.SaveChangesAsync();
        return shift.Id;
    }

    private static DateTimeOffset Time(string iso) => DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture);
}
