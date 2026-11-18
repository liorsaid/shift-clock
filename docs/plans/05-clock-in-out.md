# 05 · Clock in, clock out and "I forgot"

## Goal
A signed-in employee can clock in, clock out, or mark a forgotten clock-out. Times come only from the time API, and the database rules hold even when two requests arrive at once.

## What's built
- ClockEndpoints in `backend/ShiftClock.Api/Clock/`: the three actions.
- SignedInEmployee in `backend/ShiftClock.Api/SignIn/`: finds the signed-in employee, shared with `/api/me`.
- Tests in `backend/ShiftClock.Tests/Clock/`, with FakeTimeSource.

## How it works
| Endpoint | Case | Response |
|---|---|---|
| `POST /api/clock-in` | success | 200 `{ clockInAt }`, signed out |
| | already has an open shift | 409 |
| | time API fails | 503, nothing recorded, still signed in |
| `POST /api/clock-out` | success | 200 `{ clockInAt, clockOutAt }`, signed out |
| | no open shift | 409 |
| | time API fails, or its time is at or before the clock-in | 503, nothing recorded, still signed in |
| `POST /api/forgot-clock-out` | success | 204, marked Missing clock-out, still signed in |
| | no open shift | 409 |
| all three | not signed in | 401 |

- The shift state is checked before the time API is called, so a request that will fail never calls it.
- "I forgot" never calls the time API.
- Two requests at once: the database stops the second. The one-open-shift index or RowVersion rejects it, and it becomes 409. Any other database error stays an error.

## Tests
- Every case in the table.
- 409 with the time API unavailable, which shows the state is checked first.
- Two clock-ins at once, and two clock-outs at once: one 200, one 409.
- A clock-out in the repeated autumn hour that reads earlier than the clock-in is accepted.
- Only a one-open-shift violation becomes 409.
- The real app's time-source HttpClient has a 3-second timeout.
