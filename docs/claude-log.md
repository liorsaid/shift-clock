# Working with Claude Code

Where Claude helped, and where I had to correct it.
Claude drafts the entries; I review and edit them.

## 2026-11-16 · CLAUDE.md

Claude turned my notes into a clean CLAUDE.md. It copied my incomplete list of server-clock APIs and assumed a Linux shell, so I added `DateTimeOffset.UtcNow`, `TimeProvider.System` and my Windows setup. I also removed design decisions my own prompt had pushed in before I had made them.

## 2026-11-17 · Subagents

Claude built the code-reviewer and qa subagents and caught on its own that `git diff` misses new files. Its reviewer only checked the backend build, so I added an `npm run build` check for the frontend.

## 2026-11-17 · Committed without approval

Claude started committing because my request included a commit message. I stopped it: a commit message is not approval, and I added that to CLAUDE.md.
