# CLAUDE.md

## Project

Shift Clock is a time & attendance system where employees clock in and clock out of shifts.

**Hard constraint:** every Clock In / Clock Out is based on the time received from an
external time API, in the `Europe/Zurich` time zone. The browser clock and the server's
local clock are never used to record a clock event.

## Stack & environment

- Backend: ASP.NET Core Web API on .NET 10, EF Core, SQL Server on localhost (Windows authentication).
- Frontend: React + Vite + TypeScript.
- Dev environment: Windows, PowerShell, VS Code. Write shell commands in PowerShell syntax.

## Repo layout

| Path                 | Purpose                                                                                   |
|----------------------|-------------------------------------------------------------------------------------------|
| `backend/`           | ASP.NET Core solution                                                                     |
| `frontend/`          | React app                                                                                 |
| `docs/plans/`        | Approved work plans, `NN-short-name.md`                                                   |
| `docs/claude-log.md` | Where Claude helped and where I corrected it                                              |
| `DECISIONS.md`       | Key decisions and why, edge cases and how they were handled, what was not done, known limitations |
| `README.md`          | Local run instructions, including database setup                                          |

## Code quality

- Clear, intention-revealing names.
- Small, focused methods and components.
- No dead or commented-out code.

## MUST NOT

- Never use `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow`,
  `TimeProvider.System` or JS `Date()` to record clock events.
- No secrets in the repo (keys, passwords, connection strings with credentials).
- Do not add features that are not in the approved plan.
- Do not modify, refactor or reformat code unrelated to the current task.
- Do not add NuGet/npm packages, delete files or rename files without asking first.
- Do not skip, disable or weaken tests to make them pass.
- Never force-push or rewrite git history.

## Workflow

1. **Plan first.** For any non-trivial change, propose a plan and wait for approval.
   Save the approved plan to `docs/plans/NN-short-name.md`.
2. **Small steps.** After each step that adds or changes business logic, run the qa subagent
   on that feature. Before proposing a commit, run the code-reviewer subagent on the diff and
   fix BLOCKER/MAJOR findings. Then show a short summary of what changed, the review verdict,
   and a proposed commit message.
3. **Commit only on approval.** Commit and push only after explicit approval. Stage only
   the related files by path (never `git add .` or `git add -A`), then commit and push.
4. **Commit messages.** Conventional Commits (`feat` / `fix` / `test` / `docs` /
   `refactor` / `chore`), imperative mood, short subject, one logical change per commit.
5. **Flag decisions.** When a product or architecture decision comes up, say so explicitly
   so it can be recorded in `DECISIONS.md`. Do not decide silently.
6. **Log corrections.** When I correct you, draft a short, specific entry for `docs/claude-log.md`: what you did, what I corrected, and why. Show it to me before saving. Write entries in my voice: 'I' is the developer, Claude is third person.

