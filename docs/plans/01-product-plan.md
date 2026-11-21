# 01 · Product plan

## Product
- **Kiosk sign-in:** employee number + PIN on a shared device.
- **Sign-out:** the server signs out after each clock action; the UI after 30s idle.
- **Clock tab:** status (*Off shift* / *On shift since Tue 17 Nov, 08:02*), Clock In / Clock Out button, and an "I forgot to clock out earlier" link while a shift is open.
- **My shifts tab:** one Zurich month at a time, with arrows to move between months, opening on the newest month with shifts, with the month's total. Missing clock-out shifts are listed, not counted, with "Please contact your manager to approve your hours."
- All times shown in Europe/Zurich.

## Time API
- `https://timeapi.io/api/v1/time/current/zone?timezone=Europe/Zurich`, returns the time with an explicit offset.
- Called by the server on each clock event only, through `ITimeSource`.
- Validated against Zurich time zone rules. 10s timeout, no retry. Any failure → 503, nothing recorded.

## Data model
- **Employee:** Id, EmployeeNumber (unique), DisplayName, PinHash, IsActive.
- **Shift:** Id, EmployeeId, ClockInAt, ClockOutAt, IsMissingClockOut, RowVersion.
- Times as `datetimeoffset`. A filtered unique index allows one open shift per employee; check constraints block invalid rows.

## Local run
- Windows auth connection string (no credentials). `dotnet-ef` pinned to 10.x.
- Migrations and seed run at startup in Development.

## Testing
- **Backend:** xUnit with a fake time source; integration tests against local SQL Server.
- **Frontend:** checked manually, no automated tests.

## Work plan
1. Product plan and decisions
2. Scaffold backend, backend tests and frontend
3. External time source
4. Employee and shift data model
5. Kiosk sign-in
6. Clock in, clock out and "I forgot"
7. Status and shift history
8. Clock screen
9. My shifts tab
10. README and final pass on DECISIONS
