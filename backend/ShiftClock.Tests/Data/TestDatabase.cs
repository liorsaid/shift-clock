using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShiftClock.Api.Data;

namespace ShiftClock.Tests.Data;

public sealed class TestDatabase
{
    public const string ConnectionString =
        "Server=localhost;Database=ShiftClock_Tests;Integrated Security=True;TrustServerCertificate=True";

    public const string Pin = "2468";

    public TestDatabase()
    {
        using var db = CreateContext();
        db.Database.EnsureDeleted();
        db.Database.Migrate();
    }

    public ShiftClockDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ShiftClockDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task<Employee> AddEmployeeAsync(bool isActive = true)
    {
        var employee = new Employee { EmployeeNumber = UniqueEmployeeNumber(), DisplayName = "Test Employee", IsActive = isActive };
        employee.PinHash = new PasswordHasher<Employee>().HashPassword(employee, Pin);

        await using var db = CreateContext();
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return employee;
    }

    public static string UniqueEmployeeNumber() => Guid.NewGuid().ToString("N").Substring(0, 20);
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<TestDatabase>
{
    public const string Name = "Database";
}
