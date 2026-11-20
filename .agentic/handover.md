# Handover

What the client needs to own this without us.

- [ ] Source, infrastructure-as-code and pipelines in the client's own accounts
- [ ] Every secret rotated and owned by the client
- [ ] Architecture and decisions explained (`architecture.md`, `decisions/`)
- [ ] Runbook rehearsed by a client engineer
- [ ] Known issues and next steps listed below
- [ ] AI agent setup documented so their team can keep using it

## Known issues
- Slice 1 keeps everything in memory; restart loses posts (slice 2: Postgres).
- Identity is a demo header (`X-Member-Id`); do not expose publicly before the OIDC slice.
- EXIF stripping removes the orientation tag too; portrait photos from some phones may display rotated until renditions apply rotation first.
- Only JPEG is accepted (no PNG/HEIC yet).

## Recommended next steps
See `backlog.md` (Next / Later) and `docs/DESIGN.md` section 9 (scaling path).
