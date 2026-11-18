# 06 · Status and shift history

## Goal
A signed-in employee can see their status (off shift, or on shift since when) and their shifts one full Zurich month at a time, with the month's total. Everything reads stored data only, so it never calls the time API and keeps working when it's down.

## What's built
- `GET /api/status` in ClockEndpoints (`backend/ShiftClock.Api/Clock/`).
- HistoryEndpoints in `backend/ShiftClock.Api/History/`: `GET /api/shifts/months` and `GET /api/shifts?year=&month=`.
- ShiftHistory in the same folder: puts shifts into Zurich months and adds up totals, with no database code.
- Tests in `backend/ShiftClock.Tests/History/`.

## How it works
| Endpoint | Case | Response |
|---|---|---|
| `GET /api/status` | on shift | 200 `{ onShiftSince: "2026-11-17T08:02:13+01:00" }` |
| | off shift | 200 `{ onShiftSince: null }` |
| `GET /api/shifts/months` | signed in | 200 `[ { year, month, totalSeconds } ]`, newest first |
| `GET /api/shifts?year=&month=` | signed in | 200 `[ { clockInAt, clockOutAt, isMissingClockOut } ]`, newest first |
| | year or month missing or invalid | 400 |
| all three | not signed in | 401 |

- A shift belongs to the Zurich month of its clock-in, so a shift across midnight counts in the month it started.
- The screen opens on the newest month with shifts, so it never needs today's date.
- A month's total adds up its completed shifts. Durations subtract real moments, so a shift across a daylight saving change counts its real length.
- A Missing clock-out shift and the current shift are listed but not counted, because neither has a clock-out time.
- Responses carry data only: exact totals in seconds and times with their offset. The frontend formats them and shows all text.

## Tests
- A Missing clock-out shift and the current shift are not counted in the month's total.
- A shift that crosses midnight counts in the month it started.
- A shift across a daylight saving change counts its real length.
- Status and history work while the time API is down.
- An employee sees only their own shifts.
- Without sign-in, all three return 401.
