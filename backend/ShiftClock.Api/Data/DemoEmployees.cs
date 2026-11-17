using Microsoft.AspNetCore.Identity;

namespace ShiftClock.Api.Data;

public static class DemoEmployees
{
    public static void AddIfEmpty(ShiftClockDbContext db)
    {
        if (db.Employees.Any()) return;

        db.Employees.AddRange(
            Create("1001", "Anna Meier", "1234", isActive: true),
            Create("1002", "Luca Rossi", "5678", isActive: true),
            Create("1003", "Sara Keller", "4321", isActive: false));
        db.SaveChanges();
    }

    private static Employee Create(string employeeNumber, string displayName, string pin, bool isActive)
    {
        var employee = new Employee { EmployeeNumber = employeeNumber, DisplayName = displayName, IsActive = isActive };
        employee.PinHash = new PasswordHasher<Employee>().HashPassword(employee, pin);
        return employee;
    }
}
