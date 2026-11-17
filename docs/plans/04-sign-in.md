# 04 · Sign-in

## Goal
An employee signs in at the kiosk with their employee number and PIN, the app can ask who is signed in, and the employee can sign out. Clock in/out builds on it from step 6.

## What's built
- SignInEndpoints in `backend/ShiftClock.Api/SignIn/`: sign in, who is signed in, sign out.
- Built-in cookie authentication, set up in Program.cs.
- API tests in `backend/ShiftClock.Tests/SignIn/`, running the real app against ShiftClock_Tests.
- Package: `Microsoft.AspNetCore.Mvc.Testing` (10.0.15) in the tests.

## How it works
| Endpoint | Case | Response |
|---|---|---|
| `POST /api/sign-in` `{ employeeNumber, pin }` | valid | 200 `{ employeeNumber, displayName }` + cookie |
| | wrong PIN or unknown number | 401 |
| | inactive, correct PIN | 403 |
| | no body or invalid JSON | 400 |
| `GET /api/me` | signed in | 200 `{ employeeNumber, displayName }` |
| | no cookie, or the employee is gone or inactive | 401 |
| `POST /api/sign-out` | always | 204, cookie removed |

- The PIN is checked with `PasswordHasher<Employee>`. An unknown number is checked against a dummy hash, so it takes as long as a wrong PIN.
- 403 for an inactive employee comes only after a correct PIN.
- Errors are status codes only, with no message text; the frontend shows the messages.
- The cookie holds only the employee Id. Cookie: `ShiftClock.Session`, HttpOnly, SameSite=Strict, Secure when the request is https, session-only cookie; no idle expiry added by the app. API requests get 401, never a redirect.
- Responses never contain the PIN or its hash, and nothing logs them.

## Tests
The real app runs with `WebApplicationFactory` against ShiftClock_Tests. Each test adds its own employees. The database tests and the API tests share one xUnit collection, so the database is rebuilt once and they don't run in parallel.
- A valid sign-in returns the employee, and `/api/me` then returns the same employee.
- Wrong PIN and unknown number get the identical 401 response.
- An inactive employee gets 403 with a correct PIN, and 401 with a wrong one.
- No body or invalid JSON returns 400.
- A second employee signing in replaces the first one's session.
- `/api/me` without a cookie returns 401.
- After sign-out, `/api/me` returns 401.
- An employee deactivated after signing in gets 401 from `/api/me`.
- The cookie is HttpOnly, SameSite=Strict and session-only.
- The sign-in response contains only employeeNumber and displayName.
