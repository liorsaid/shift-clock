# 07 · Sign-in screen and Clock tab

## Goal
An employee signs in at the kiosk with their number and PIN, sees their status, and clocks in, clocks out, or uses "I forgot". The My shifts tab is an empty tab until step 9.

## What's built
All in `frontend/src/`, no new packages:
- `App.tsx`: shows the sign-in screen or the signed-in screen, and carries a sign-out message to sign-in.
- `screens/SignInScreen.tsx`: the sign-in form.
- `screens/SignedInScreen.tsx`: the employee's name, a Sign out button, the Clock and My shifts tabs, the 30-second idle sign-out, and the 3-second confirmation after Clock In, Clock Out or Sign out.
- `screens/ClockTab.tsx`: status and the clock buttons.
- `shared/api.ts`: one small function per endpoint. Each returns the status code and the data.
- `shared/messages.ts`: all the messages below, in one place.
- `shared/zurichTime.ts`: formats a time from the backend in Europe/Zurich, for example "Tue 17 Nov, 08:02".
- `index.css`: simple, large kiosk styling.

## How it works
**Opening the app.** It calls `GET /api/me`. 200 shows the signed-in screen; anything else shows the sign-in screen.

**Sign-in screen.** Employee number, PIN and a Sign in button. If a field isn't valid, a short message shows under it, and Sign in stays disabled. Typing clears a sign-in error.

**Signed-in screen.** "Signed in as Anna Meier", a Sign out button, and two tabs: Clock (open first) and My shifts (empty for now). After 30 seconds without a tap or key press, the app signs out.

**Clock tab.** It loads `GET /api/status` and shows:
- Off shift: "Off shift" and a Clock In button.
- On shift: "On shift since Tue 17 Nov, 08:02", a Clock Out button, and the link "I forgot to clock out earlier". The link asks "Mark your open shift as missing a clock-out?" before it sends, since it can't be undone.

After a successful Clock In or Clock Out, the screen shows only "Clocked in at Thu 19 Nov, 14:38." (or "Clocked out at …") and "Signing you out…", with no buttons or tabs, since the server has already signed the employee out. After 3 seconds the app goes to the sign-in screen, empty.

While a request is running, the buttons are disabled, so a double tap sends once. Times come only from the backend and are shown in Zurich time; the browser clock is never read.

**When the app returns to the sign-in screen.** Only when the employee is really signed out on the server:
- Returns after: a successful Clock In, Clock Out or Sign out (each after its 3-second confirmation), 30 seconds idle, and any 401.
- Stays signed in after: "I forgot", a 409, a 503, and a failed sign-out (with "Something went wrong").

**Messages for each response.** All text lives in the frontend. Errors show in red.

| Request | Response | What the employee sees |
|---|---|---|
| Employee number field | not digits only | "Employee number must be digits only." |
| PIN field | a non-digit or more than 4 digits, or fewer than 4 when leaving the field | "PIN must be 4 digits." |
| Sign in | 401 | "Wrong employee number or PIN." |
| | 403 | "Your account is inactive. Please contact your manager." |
| Clock In | 200 | "Clocked in at Tue 17 Nov, 08:02." and "Signing you out…", then the sign-in screen |
| | 409 | "You're already clocked in." Status reloads. |
| | 503 | "The time service is unavailable. Nothing was recorded. Please try again." |
| Clock Out | 200 | "Clocked out at Tue 17 Nov, 17:00." and "Signing you out…", then the sign-in screen |
| | 409 | "You're not clocked in." Status reloads. |
| | 503 | Same as Clock In 503 |
| I forgot | 204 | "Shift marked as missing a clock-out. Please contact your manager to approve your hours." Status reloads. |
| | 409 | "You're not clocked in." Status reloads. |
| Sign out | 204 | Only "You're signed out." for 3 seconds, then an empty sign-in screen |
| 30 seconds idle | 204 | Sign-in screen with "You were signed out after 30 seconds without activity." |
| Any signed-in request | 401 | Sign-in screen with "Your session has ended. Please sign in again." |
| Any request | other code or no connection | "Something went wrong. Please try again." |

## Tests
Manual checks in the browser, with the backend and frontend running and the demo employees:
1. Sign in as 1001 / a wrong PIN, and as 9999 / any PIN: the same "Wrong employee number or PIN."
2. Type "10a1" as the number: its message shows right away. Type "12" as the PIN: no message until you leave the field. Sign in stays disabled. Fix both: the messages go away and Sign in is enabled.
3. Sign in as 1003 / 4321 (inactive): "Your account is inactive…"
4. Sign in as 1001 / 1234: "Signed in as Anna Meier", Clock tab, "Off shift".
5. Clock In: "Clocked in at …" in Zurich time and "Signing you out…", then after 3 seconds an empty sign-in screen.
6. Sign in again: "On shift since …" matches the clock-in time.
7. Clock Out: "Clocked out at …", then after 3 seconds an empty sign-in screen.
8. Clock in, sign in again, use "I forgot": the missing clock-out message, then "Off shift", still signed in.
9. Double-tap Clock In: one clock-in, no error.
10. Restart the backend with `$env:TimeApi__Url = 'https://timeapi.invalid/'`, then Clock In: the time service message, still signed in, still off shift.
11. Wait 30 seconds without touching anything: back to the sign-in screen with "You were signed out after 30 seconds without activity."
12. Sign out: only "You're signed out." for 3 seconds, then an empty sign-in screen.
13. Stop the backend, then Sign out: "Something went wrong", still signed in.