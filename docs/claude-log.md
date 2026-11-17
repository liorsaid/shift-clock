# Working with Claude Code

Where Claude helped, and where I had to correct it.
Claude drafts the entries; I review and edit them.

## 2026-11-16 · CLAUDE.md

Claude turned my notes into a clean CLAUDE.md. It copied my incomplete list of server-clock APIs and assumed a Linux shell, so I added `DateTimeOffset.UtcNow`, `TimeProvider.System` and my Windows setup. I also removed design decisions my own prompt had pushed in before I had made them.

## 2026-11-17 · Subagents

Claude built the code-reviewer and qa subagents and caught on its own that `git diff` misses new files. Its reviewer only checked the backend build, so I added an `npm run build` check for the frontend.

## 2026-11-17 · Committed without approval

Claude started committing because my request included a commit message. I stopped it: a commit message is not approval, and I added that to CLAUDE.md.

## 2026-11-17 · Clock hook

Claude built the clock hook and the git approval rule, and tested that they work. I noticed the hook missed DateTime.Today and database time functions like GETDATE(). Claude mapped every way to read the current time in the browser, server and database, and the hook now blocks them all.

## 2026-11-17 · Planning

Claude helped plan the project: it confirmed live that TimeAPI.io's v1 endpoint returns an explicit offset, designed the data model and edge cases, and its reviewer raised questions I had missed. For forgotten clock-outs I chose an always-available "I forgot" link with a "contact your manager" note.

## 2026-11-17 · Template clock read

The webapi template's weather demo used `DateTime.Now`. It came from a terminal command, so the hook never saw it; Claude caught it and replaced it.

## 2026-11-17 · One source for rules

I noticed the same rules were repeated in the plan, DECISIONS.md and qa.md. Now DECISIONS.md is the single source, and Claude and both agents read it.
