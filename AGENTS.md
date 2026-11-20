# AGENTS.md

Framely: Local photographers have no photo-first community of their own: big platforms bury their work under ads and video, and they can't see who actually follows their craft.

## Read first
Everything about this project lives in the hidden `.agentic/` folder.
Start with `.agentic/README.md`; it says which file answers which question.
Load only the files your task needs.

## Hard rules
- Work in thin vertical slices: one ticket from `.agentic/backlog.md` at a time.
- No change without a test that fails first. Run the full test suite before you finish.
- A decision that is hard to reverse gets an ADR in `.agentic/decisions/`.
- Never commit secrets, real client data or personal data. Use `.env.example`.
- Do not grow this file. New knowledge goes in the right `.agentic/` file; history goes in `.agentic/changelog.md`.
- When you learn something that would help on the next project, add it to `.agentic/lessons.md`.

## What is live
- Slice 1: post a photo (EXIF stripped unless location sharing) and followers see it first; feeds pre-built on write, 25 per page with a cursor; follow/unfollow.
- API `src/Framely.Api` on :5101 (in-memory, Development seed); web `web/` on :5181. Design: `docs/DESIGN.md`.
