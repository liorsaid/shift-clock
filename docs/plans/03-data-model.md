# 03 · Data model

## Goal
Employees and shifts stored in SQL Server, with the shift rules enforced by the database itself, even for simultaneous requests. Sign-in and clock in/out build on it from step 5.

## What's built
- Employee, Shift and ShiftClockDbContext in `backend/ShiftClock.Api/Data/`.
- The first migration, `InitialCreate`.
- DemoEmployees: adds three demo employees when the Employee table is empty.
- In Development, startup applies migrations and then runs DemoEmployees.
- Connection strings for the app and the tests.
- Database tests in `backend/ShiftClock.Tests/Data/`.
- Packages: `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.Design` (10.0.15).

## How it works
- **Employee:** Id (int identity), EmployeeNumber (nvarchar(20), unique), DisplayName (nvarchar(100)), PinHash (nvarchar(200)), IsActive (bit).
- **Shift:** Id (int identity), EmployeeId (int, FK, no cascade delete), ClockInAt (datetimeoffset), ClockOutAt (datetimeoffset, null while open), IsMissingClockOut (bit), RowVersion (rowversion).
- A shift is open when ClockOutAt is null and IsMissingClockOut is 0.

Database rules:
1. One open shift per employee: a unique index on EmployeeId, filtered to open shifts.
2. Clock-out after clock-in: a check constraint, ClockOutAt is null or later than ClockInAt.
3. A missing clock-out shift has no clock-out time: a check constraint.
4. Simultaneous clock-outs: RowVersion makes the second update of the same shift fail.

- Demo employees: two active, one inactive. PINs are hashed with ASP.NET Core's built-in `PasswordHasher<Employee>`.
- Connection strings use Windows authentication with no credentials: `ShiftClock` in appsettings.json for the app, and `ShiftClock_Tests` in the test fixture.
- New command: `dotnet ef migrations add <Name> --project backend/ShiftClock.Api`.

## Tests
Against ShiftClock_Tests, deleted and rebuilt from the migrations before the tests. Fixed times only.
- A second open shift for the same employee is rejected.
- Open shifts for different employees are allowed.
- A new shift after a clocked-out or missing clock-out shift is allowed.
- A clock-out before or equal to the clock-in is rejected. In the repeated autumn hour, a later clock-out that reads earlier is allowed.
- A second employee with the same employee number is rejected.
- A missing clock-out shift with a clock-out time is rejected.
- Of two simultaneous clock-outs on one shift, the second is rejected.
