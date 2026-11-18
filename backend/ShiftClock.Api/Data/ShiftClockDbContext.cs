using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ShiftClock.Api.Data;

public class ShiftClockDbContext(DbContextOptions<ShiftClockDbContext> options) : DbContext(options)
{
    private const string OneOpenShiftPerEmployeeIndex = "IX_Shifts_OneOpenShiftPerEmployee";
    private static readonly int[] DuplicateKeyErrorNumbers = [2601, 2627];

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Shift> Shifts => Set<Shift>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(employee =>
        {
            employee.Property(e => e.EmployeeNumber).HasMaxLength(20);
            employee.Property(e => e.DisplayName).HasMaxLength(100);
            employee.Property(e => e.PinHash).HasMaxLength(200);
            employee.HasIndex(e => e.EmployeeNumber).IsUnique();
        });

        modelBuilder.Entity<Shift>(shift =>
        {
            shift.HasOne<Employee>().WithMany().HasForeignKey(s => s.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            shift.Property(s => s.RowVersion).IsRowVersion();
            shift.HasIndex(s => s.EmployeeId)
                .IsUnique()
                .HasFilter("[ClockOutAt] IS NULL AND [IsMissingClockOut] = 0")
                .HasDatabaseName(OneOpenShiftPerEmployeeIndex);
            shift.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Shifts_ClockOutAfterClockIn", "[ClockOutAt] IS NULL OR [ClockOutAt] > [ClockInAt]");
                table.HasCheckConstraint("CK_Shifts_MissingClockOutHasNoTime", "[IsMissingClockOut] = 0 OR [ClockOutAt] IS NULL");
            });
        });
    }

    public static bool IsOneOpenShiftViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException sqlException
        && DuplicateKeyErrorNumbers.Contains(sqlException.Number)
        && sqlException.Message.Contains(OneOpenShiftPerEmployeeIndex);
}
