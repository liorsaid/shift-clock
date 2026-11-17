using Microsoft.AspNetCore.Identity;

namespace ShiftClock.Api.Data;

public static class DemoEmployees
{
    public static void AddMissing(ShiftClockDbContext db)
    {
        Employee[] demoEmployees =
        [
            Create("1001", "Anna Meier", "1234", isActive: true),
            Create("1002", "Luca Rossi", "5678", isActive: true),
            Create("1003", "Sara Keller", "4321", isActive: false),
        ];

        var existingNumbers = db.Employees.Select(e => e.EmployeeNumber).ToHashSet();
        var missingEmployees = demoEmployees.Where(e => !existingNumbers.Contains(e.EmployeeNumber));

        db.Employees.AddRange(missingEmployees);
        db.SaveChanges();
    }

    private static Employee Create(string employeeNumber, string displayName, string pin, bool isActive)
    {
        var employee = new Employee { EmployeeNumber = employeeNumber, DisplayName = displayName, IsActive = isActive };
        employee.PinHash = new PasswordHasher<Employee>().HashPassword(employee, pin);
        return employee;
    }
}
