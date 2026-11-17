using Microsoft.EntityFrameworkCore;
using ShiftClock.Api.Data;

namespace ShiftClock.Tests.Data;

public sealed class TestDatabase
{
    private const string ConnectionString =
        "Server=localhost;Database=ShiftClock_Tests;Integrated Security=True;TrustServerCertificate=True";

    public TestDatabase()
    {
        using var db = CreateContext();
        db.Database.EnsureDeleted();
        db.Database.Migrate();
    }

    public ShiftClockDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ShiftClockDbContext>().UseSqlServer(ConnectionString).Options);
}
