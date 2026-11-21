# Shift Clock

Employees clock in and out of shifts at a shared kiosk. Every clock-in and clock-out time comes from TimeAPI.io, in Zurich time.

## What you need

- Windows
- .NET 10 SDK
- Node.js 22 or later
- SQL Server running as the default instance on this computer (Developer edition is fine). Your Windows account must be allowed to create databases.
- Internet access, for TimeAPI.io

## Database

There's nothing to set up by hand. When the backend starts, it creates the `ShiftClock` database with Windows authentication, applies the migrations and adds three demo employees. The tests create their own `ShiftClock_Tests` database.

## Run

Open two PowerShell windows in the repo folder.

Window 1, the backend (http://localhost:5163):

```powershell
dotnet run --project backend/ShiftClock.Api
```

Window 2, the frontend:

```powershell
npm ci --prefix frontend
npm --prefix frontend run dev
```

If PowerShell blocks `npm` because running scripts is disabled, use `npm.cmd` instead, for example `npm.cmd ci --prefix frontend`.

Then open http://localhost:5173.

## Demo employees

| Number | PIN  | Name        | Note     |
|--------|------|-------------|----------|
| 1001   | 1234 | Anna Meier  |          |
| 1002   | 5678 | Luca Rossi  |          |
| 1003   | 4321 | Sara Keller | inactive |

## Tests

```powershell
dotnet test backend/ShiftClock.slnx
```

Key decisions, edge cases and known limitations are in [DECISIONS.md](DECISIONS.md).
