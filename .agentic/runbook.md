# Runbook

## Prerequisites
.NET 8 SDK, Node 22. No database in slice 1 (in-memory stores; restart = fresh seed).

## Run the API (port 5101)
```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Framely.Api   # launchSettings uses http://localhost:5101
curl http://localhost:5101/health/ready
curl -H "X-Member-Id: 6f2a0000-0000-4000-8000-000000000001" http://localhost:5101/api/feed
```
Development seeds 9 invented photographers, follows and 50 posts (SVG placeholder photos as data URIs). Default viewer: Thandi Mahlangu (`...000000000001`). She does not follow Kagiso (`...08`) or Fatima (`...09`).

Post a photo:
```bash
curl -H "X-Member-Id: 6f2a0000-0000-4000-8000-000000000002" -H "Idempotency-Key: $(uuidgen)" \
  -F "photo=@shot.jpg;type=image/jpeg" -F "caption=Harbour at dawn" -F "altText=Boats" http://localhost:5101/api/posts
```

## Run the web app (port 5181)
```bash
cd web && npm ci && npm run dev    # proxies /api and /media to http://localhost:5101
```
Open http://localhost:5181. Use "Viewing as" to switch member and see follower vs non-follower feeds.

## Run the tests
```bash
dotnet test                                  # CI (needs NuGet)
# offline in this workspace:
/home/claude/tools/run-xunit-offline.sh tests/Framely.Tests src/Framely.Api/Framely.Api.csproj
cd web && npx vitest run && npm run build
```

## Configuration
| Key | Default | Meaning |
|---|---|---|
| `Media:RootPath` | `App_Data/media` | photo folder (relative to content root); git-ignored |
| `Media:OriginRequestPath` | `/media` | path the API serves photos from (local CDN origin) |
| `Media:PublicBaseUrl` | `/media` | base of image URLs; set to the CDN host in production |
| `Media:MaxUploadBytes` | `10485760` | max photo size (10 MB) |
| `Feed:PageSize` | `25` | feed page size |
| `Feed:MaxTimelineLength` | `1000` | entries kept per pre-built feed |
| `Feed:BackfillCount` | `50` | posts copied into a feed on follow |

## Deploy
Not yet automated. Target: one container (API + static web build) behind a load balancer; photos in blob storage behind a CDN (`Media:PublicBaseUrl` = CDN host); Postgres from slice 2.

## Monitor and alerts
- `/health/live` for restarts, `/health/ready` for traffic (fails when the media store isn't writable).
- Meter `Framely.Feed`: `framely.feed.fanout.duration` p95 > 2 s is the ADR-0001 evolution trigger.
- SLOs: `quality.md`.

## Common incidents
- **Uploads fail with 500 and the ready check is unhealthy:** media folder full or read-only. Free space or fix permissions; clients retry with the same Idempotency-Key.
- **Feed empty after restart:** expected in slice 1 (in-memory). Slice 2 adds Postgres.
- **415 on upload:** only JPEG is accepted; the bytes are sniffed, not just the header.
