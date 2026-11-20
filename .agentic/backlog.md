# Backlog

Vertical slices, smallest valuable first.

## Now
- [x] Sketch leg: tokens + three variants of the slice-1 screen (`sketch-ui`) → `ux/decision.md`, final `ux/screens/home-feed.html`
- [x] Slice 1: A photographer posts a photo with a caption and their followers see it at the top of their home feed
  - [x] Given A follows B, when B posts, then the post is first in A's home feed
  - [x] Given C does not follow B, then B's post is not in C's feed
  - [x] Given a post, then the stored image has no GPS EXIF data unless location sharing is on
  - [x] Given 25+ posts, then the feed pages with a cursor (25 per page)
  - [x] Test notes: one test per criterion; seeded invented data.

## Next
- [ ] Slice 2: persistence (Postgres via EF Core) behind the same repository interfaces
- [ ] CI green with all gates

## Later
- [ ] Auth slice: OIDC bearer tokens replace the demo `X-Member-Id` header
- [ ] Fan-out worker fed by an outbox; pull fallback for accounts with ~100k followers
- [ ] Image renditions (display sizes) + apply EXIF orientation before stripping
- [ ] Delete-my-account: erase photos, posts and feed entries within 30 days (POPIA)
- [ ] Playwright + axe e2e per flow in `ux/flows.md`
- [ ] OpenTelemetry export (needs NuGet in CI)
