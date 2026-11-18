# Decisions

## 1. Key Decisions
- **Time comes only from TimeAPI.io.** The server calls it on every clock in/out. The browser and server clocks are never used for clock events, so no one can change a recorded time by changing a device clock.
- **If the time API fails, nothing is recorded.** A retry is better than a wrong time.
- **A hook blocks local clock code** in Claude's edits, and the reviewer checks the rest. A written rule can be missed; a hook can't be skipped.
- **Times are stored with their Zurich offset** (`datetimeoffset`), and the offset is checked against Zurich rules, so every time is exact, even around daylight saving changes.
- **The database enforces the rules:** one open shift per employee, and clock-out after clock-in. Only the database can stop two requests that arrive at the same moment.
- **Kiosk sign-in with employee number and PIN** on one shared device. The employee is signed out after each clock in/out, so the next person can't act as them.
- **PINs are stored hashed,** so the database never holds a real PIN.
- **The sign-in cookie is protected** (HttpOnly, SameSite=Strict), so no other code or website can steal or use an employee's session.
- **Framework internals may use the system clock** (for example cookie expiry), since they never record a clock event.
- **The frontend owns all screen text.** The backend returns only status codes. Each side has one job, so a message can change without touching the backend.

## 2. Edge Cases
- **Forgot to clock out:** "I forgot" marks the shift Missing clock-out instead of guessing a time. It's listed but not counted, and the employee stays signed in to clock in again.
- **Double click, two tabs, or two clock-outs at once:** the second request is rejected.
- **Clock in twice, or clock out with no open shift:** a clear error, and nothing changes.
- **Time API down or returns bad data:** a clear error. Status and history still work.
- **Time API returns a time at or before the clock-in:** rejected, and nothing is recorded.
- **Shift across midnight:** it counts on the day and month it started.
- **Daylight saving changes:** durations are exact, and the repeated autumn hour is not ambiguous.
- **Computer in another time zone:** all times are shown in Zurich time.
- **Wrong PIN or unknown number:** the same error and response time, so no one can find which numbers exist.
- **Inactive employee:** a "contact your manager" message, only after a correct PIN, so nothing is revealed. An open shift stays as is, since deactivation happens outside the app.
- **Deactivated while signed in:** signed out on the next request.

## 3. What I Chose Not To Do
- **No manager screens** (approving hours, fixing shifts, adding employees), so I could do the employee flow well. A missing clock-out tells the employee to contact their manager.
- **No adding a forgotten clock-in,** because its time wouldn't come from the API.
- **No live timer,** because it would need the browser clock.
- **No breaks, overtime or exports,** to keep the scope small and the core solid.
- **No automated frontend tests,** because all rules live in the backend, where they're tested; the screens only show results and are checked manually.

## 4. Known Limitations
- **One time service.** If TimeAPI.io is down, nobody can clock in or out.
- **The recorded time is when the API answered,** up to 3 seconds after the click.
- **PINs can be guessed.** There's no limit on wrong attempts.
- **Idle sign-out runs in the browser only.**
- **Windows only** (Windows authentication, PowerShell hook).
