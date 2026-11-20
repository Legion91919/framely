# Quality

## Definition of done
- Acceptance criteria met and demonstrated.
- Unit tests for rules, integration tests for each adapter, one end-to-end test per key flow.
- CI green: build, tests, lint, dependency and secret scan, web (Vitest + build).
- Docs in `.agentic/` updated in the same change.

## Service levels
| SLI | SLO | Why |
|---|---|---|
| Feed availability (non-5xx on `GET /api/feed`) | 99.9 % over 30 days | members open the feed many times a day |
| Feed latency p95 (`GET /api/feed`, server side) | < 300 ms | leaves room for the < 1 s on 4G outcome |
| Post success rate (`POST /api/posts`, excluding 4xx) | 99.5 % over 30 days | a failed post is a lost photo moment |
| Freshness: post to followers' feeds, p95 | < 5 s (alert at 2 s = ADR-0001 trigger) | product outcome |
| Image bytes served by the CDN | > 95 % | product outcome: bandwidth off the API |
| Location metadata leaks (stored photos with APP1 while sharing is off) | 0 | POPIA |

## Current proof (slice 1)
- 75 .NET tests (stripper, fan-out, paging, graph, notifications, ingest, real-HTTP acceptance, architecture).
- 13 Vitest tests (token contrast/drift, feed states and paging, compose validation and idempotent retry, API client).
- Gaps: Playwright + axe journeys, coverage gate run, OpenTelemetry export.
