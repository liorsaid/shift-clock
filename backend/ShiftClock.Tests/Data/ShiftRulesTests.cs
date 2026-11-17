using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ShiftClock.Api.Data;

namespace ShiftClock.Tests.Data;

public class ShiftRulesTests(TestDatabase database) : IClassFixture<TestDatabase>
{
    private static readonly DateTimeOffset Morning = Time("2026-11-17T08:00:00+01:00");
    private static readonly DateTimeOffset Evening = Time("2026-11-17T17:00:00+01:00");

    [Fact]
    public async Task Rejects_a_second_open_shift_for_the_same_employee()
    {
        var employeeId = await AddEmployeeAsync();
        await AddShiftAsync(new Shift { EmployeeId = employeeId, ClockInAt = Morning });

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            AddShiftAsync(new Shift { EmployeeId = employeeId, ClockInAt = Evening }));
    }

    [Fact]
    public async Task Allows_open_shifts_for_different_employees()
    {
        await AddShiftAsync(new Shift { EmployeeId = await AddEmployeeAsync(), ClockInAt = Morning });
        await AddShiftAsync(new Shift { EmployeeId = await AddEmployeeAsync(), ClockInAt = Morning });
    }

    [Fact]
    public async Task Allows_a_new_shift_after_a_missing_clock_out()
    {
        var employeeId = await AddEmployeeAsync();
        await AddShiftAsync(new Shift { EmployeeId = employeeId, ClockInAt = Morning, IsMissingClockOut = true });

        await AddShiftAsync(new Shift { EmployeeId = employeeId, ClockInAt = Evening });
    }

    [Fact]
    public async Task Allows_a_new_shift_after_a_clocked_out_shift()
    {
        var employeeId = await AddEmployeeAsync();
        await AddShiftAsync(new Shift { EmployeeId = employeeId, ClockInAt = Morning, ClockOutAt = Time("2026-11-17T12:00:00+01:00") });

        await AddShiftAsync(new Shift { EmployeeId = employeeId, ClockInAt = Evening });
    }

    [Fact]
    public async Task Allows_a_clock_out_in_the_repeated_autumn_hour_that_reads_earlier_than_the_clock_in()
    {
        var shift = new Shift
        {
            EmployeeId = await AddEmployeeAsync(),
            ClockInAt = Time("2026-10-25T02:30:00+02:00"),
            ClockOutAt = Time("2026-10-25T02:10:00+01:00"),
        };

        await AddShiftAsync(shift);
    }

    [Fact]
    public async Task Rejects_a_second_employee_with_the_same_employee_number()
    {
        var employeeNumber = Guid.NewGuid().ToString("N")[..20];
        await using var db = database.CreateContext();
        db.Employees.Add(new Employee { EmployeeNumber = employeeNumber, DisplayName = "First", IsActive = true });
        await db.SaveChangesAsync();
        db.Employees.Add(new Employee { EmployeeNumber = employeeNumber, DisplayName = "Second", IsActive = true });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Theory]
    [InlineData("2026-11-17T07:59:59+01:00")]
    [InlineData("2026-11-17T07:00:00Z")]
    public async Task Rejects_a_clock_out_before_or_at_the_clock_in(string clockOutAt)
    {
        var shift = new Shift { EmployeeId = await AddEmployeeAsync(), ClockInAt = Morning, ClockOutAt = Time(clockOutAt) };

        await Assert.ThrowsAsync<DbUpdateException>(() => AddShiftAsync(shift));
    }

    [Fact]
    public async Task Rejects_a_missing_clock_out_shift_with_a_clock_out_time()
    {
        var shift = new Shift { EmployeeId = await AddEmployeeAsync(), ClockInAt = Morning, ClockOutAt = Evening, IsMissingClockOut = true };

        await Assert.ThrowsAsync<DbUpdateException>(() => AddShiftAsync(shift));
    }

    [Fact]
    public async Task Rejects_the_second_of_two_simultaneous_clock_outs()
    {
        var shiftId = await AddShiftAsync(new Shift { EmployeeId = await AddEmployeeAsync(), ClockInAt = Morning });
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var firstCopy = await first.Shifts.SingleAsync(s => s.Id == shiftId);
        var secondCopy = await second.Shifts.SingleAsync(s => s.Id == shiftId);

        firstCopy.ClockOutAt = Evening;
        await first.SaveChangesAsync();
        secondCopy.ClockOutAt = Evening.AddMinutes(1);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    private async Task<int> AddEmployeeAsync()
    {
        await using var db = database.CreateContext();
        var employee = new Employee { EmployeeNumber = Guid.NewGuid().ToString("N")[..20], DisplayName = "Test", IsActive = true };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return employee.Id;
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
