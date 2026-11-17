# Decisions

## 1. Key Decisions
- **Time comes only from TimeAPI.io.** The server calls it on every clock in/out. The browser and server clocks are never used.
- **If the time API fails, nothing is recorded.** A retry is better than a wrong time.
- **A hook blocks local clock code.** A rule in CLAUDE.md can be missed; a hook can't.
- **Times are stored with their Zurich offset** (`datetimeoffset`), and the offset is checked against Zurich rules, so every time is exact, even around daylight saving changes.
- **The database enforces the rules:** one open shift per employee, and clock-out after clock-in, even with simultaneous requests.
- **Kiosk sign-in with employee number and PIN.** The app runs on one shared device, and the employee is signed out after each clock in/out.
- **Forgotten clock-out is marked, not guessed.** "I forgot" marks the shift as missing a clock-out instead of inventing a time.
- **History shows the last 30 shifts by month,** with a monthly total. It reads stored data only.
- **Framework internals may use the system clock** (for example cookie expiry). They never record a clock event.

## 2. Edge Cases
- **Double click, two tabs, or two clock-outs at once:** the second request is rejected.
- **Clock in twice, or clock out with no open shift:** a clear error, and nothing changes.
- **Time API down or returns bad data:** a clear error, and nothing is recorded. Status and history still work.
- **Shift across midnight:** it counts on the day it started.
- **Daylight saving changes:** durations are exact, and the repeated autumn hour is not ambiguous.
- **Computer in another time zone:** all times are shown in Zurich time.
- **Wrong PIN or unknown number:** the same error, so it doesn't reveal which numbers exist.

## 3. What I Chose Not To Do
- **No manager screens** (approving hours, fixing shifts, adding employees). A missing clock-out tells the employee to contact their manager.
- **No adding a forgotten clock-in,** because its time wouldn't come from the API.
- **No live timer,** because it would need the current time on screen all the time.
- **No breaks, overtime or exports.**
- **No automated frontend tests.** Screens are checked manually; the backend edge cases have automated tests.

## 4. Known Limitations
- **One time service.** If TimeAPI.io is down, nobody can clock in or out.
- **The recorded time is when the API answered,** up to 3 seconds after the click.
- **PINs can be guessed.** There's no limit on wrong attempts.
- **Idle sign-out runs in the browser only.**
- **Windows only** (Windows authentication, PowerShell hook).
