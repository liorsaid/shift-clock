namespace ShiftClock.Api.Data;

public class Shift
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateTimeOffset ClockInAt { get; set; }

    public DateTimeOffset? ClockOutAt { get; set; }

    public bool IsMissingClockOut { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
