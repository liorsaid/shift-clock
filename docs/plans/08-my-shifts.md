# 08 · My shifts tab

## Goal
The employee sees their shifts one full Zurich month at a time, with the month's total, and moves between months with arrows. It uses the two history endpoints from step 7 and never calls the time API.

## What's built
All in `frontend/src/`, no new packages:
- `MyShiftsTab.tsx`: the month, its total, its shifts, and the arrows.
- `api.ts`: `getShiftMonths()` and `getShifts(year, month)`.
- `zurichTime.ts`: `formatMonth` ("November 2026") and `formatDuration` ("38 h 15 min").
- `messages.ts`: the new messages.
- `SignedInScreen.tsx`: shows MyShiftsTab on the My shifts tab.
- `index.css`: the month row with its arrows.

## How it works
**Opening the tab.** It loads `GET /api/shifts/months` (newest first) and opens on the first month, the newest with shifts, so it never needs today's date.

**The month.** At the top: ‹ November 2026 ›.
- ‹ goes to the next older month in the list, and › to the next newer one. Each is disabled at the end of the list.
- The arrows move only between months that have shifts, so there's never an empty month to show.
- Each month change loads `GET /api/shifts?year=&month=`. If a reply arrives after the employee has moved on, it's ignored.

Under it: "Total: 38 h 15 min". The total comes from the backend and counts completed shifts only. It's shown in hours and minutes, with leftover seconds dropped.

**The shifts.** One line per shift, newest first, all in Zurich time:
- Completed: "Tue 17 Nov, 08:00 → Tue 17 Nov, 17:00"
- Missing clock-out: "Tue 17 Nov, 08:00 → Missing clock-out. Please contact your manager to approve your hours."
- Current shift: "Tue 17 Nov, 08:00 → On shift"

Missing clock-out shifts and the current shift are listed but not counted, as in plan 06. Times come only from the backend; the browser clock is never read.

**Messages.** All text lives in the frontend. Errors show in red.

| Case | What the employee sees |
|---|---|
| No shifts at all | "No shifts yet." |
| Month total | "Total: 38 h 15 min" |
| Missing clock-out shift | "Missing clock-out. Please contact your manager to approve your hours." |
| Current shift | "On shift" |
| 401 on either request | Sign-in screen with "Your session has ended. Please sign in again." |
| Other code or no connection | "Something went wrong. Please try again." |

## Tests
Manual checks in the browser, with the backend and frontend running and the demo employees:
1. An employee with no shifts opens My shifts: "No shifts yet."
2. Sign in as 1001 with a few shifts this month: the newest month opens, each shift is listed newest first, and the total matches the completed shifts.
3. A Missing clock-out shift and the current shift are listed with their text, and the total doesn't change.
4. Add an earlier-month shift in SQL Server (database `ShiftClock`):
   `INSERT INTO Shifts (EmployeeId, ClockInAt, ClockOutAt, IsMissingClockOut) SELECT Id, '2026-10-15T08:00:00+02:00', '2026-10-15T16:30:00+02:00', 0 FROM Employees WHERE EmployeeNumber = '1001';`
   Then ‹ shows October 2026 with "Total: 8 h 30 min", › goes back, and each arrow is disabled at the end.
