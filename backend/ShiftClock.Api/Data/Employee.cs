namespace ShiftClock.Api.Data;

public class Employee
{
    public int Id { get; set; }

    public required string EmployeeNumber { get; set; }

    public required string DisplayName { get; set; }

    public string PinHash { get; set; } = "";

    public bool IsActive { get; set; }
}
