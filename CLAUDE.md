# CLAUDE.md

## Project

Shift Clock is a time & attendance system where employees clock in and clock out of shifts.

**Hard constraint:** every Clock In / Clock Out uses the time from an external time API, in the `Europe/Zurich` time zone. Never the browser clock or the server's local clock.

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

## Commands

Run from the repo root.

- Restore tools and packages: `dotnet tool restore` and `npm ci --prefix frontend`
- Build: `dotnet build backend/ShiftClock.slnx` and `npm --prefix frontend run build`
- Test: `dotnet test backend/ShiftClock.slnx`
- Add a migration: `dotnet ef migrations add <Name> --project backend/ShiftClock.Api`
- Run the backend: `dotnet run --project backend/ShiftClock.Api` (http://localhost:5163)
- Run the frontend: `npm --prefix frontend run dev` (http://localhost:5173, proxies `/api` to the backend)

## Code quality

- Clear, intention-revealing names.
- Small, focused methods and components.
- Keep it simple. Build only what the plan asks for.
- No dead or commented-out code.
- Docs and Claude Code config (CLAUDE.md, .claude/, DECISIONS.md, README.md, docs/): simple, focused and clear. Describe the current state, not how it got there. Exception: docs/claude-log.md records what happened.

## MUST NOT

- Never read a local clock to record a clock event (for example `DateTime.Now`, `Date.now()`, `GETDATE()`).
- No secrets in the repo (keys, passwords, connection strings with credentials).
- Do not add features that are not in the approved plan.
- Do not modify, refactor or reformat code unrelated to the current task.
- Do not add NuGet/npm packages, delete files or rename files without asking first.
- Do not skip, disable or weaken tests to make them pass.
- Never force-push or rewrite git history.

## Workflow

1. **Plan first.** For any non-trivial change, propose a plan and wait for approval.
   Save the approved plan to `docs/plans/NN-short-name.md`.
   Each step plan has these sections: Goal, What's built, How it works, Tests. Keep it short.
   Before implementing, read DECISIONS.md together with the plan.
2. **Small steps.** After each step that adds or changes business logic, run the qa subagent
   on that feature. Before proposing a commit, run the code-reviewer subagent on the diff and
   fix BLOCKER/MAJOR findings. Then show a short summary of what changed, the review verdict,
   and a proposed commit message.
3. **Commit only on approval.** Commit and push only after explicit approval. Stage only
   the related files by path (never `git add .` or `git add -A`).
   A commit message in my request is not approval.
4. **Commit messages.** Conventional Commits (`feat` / `fix` / `test` / `docs` /
   `refactor` / `chore`), imperative mood, short subject, one logical change per commit.
5. **Flag decisions.** When a key decision comes up (a real choice in product, architecture or security), say so;
   don't decide silently. Record it in `DECISIONS.md` with one line on why.
6. **Log help and corrections.** Draft an entry for `docs/claude-log.md` right away when I correct you, or when you catch something I missed. Skip routine work. Show it to me before saving. Write in my voice: 'I' is the developer, Claude is third person.

