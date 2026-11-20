# Architecture

## Style and why
Advisor output (traits: MediaDelivery, ReadHeavy, PersonalData, SeveralBusinessAreas, SmallTeam):

### Recommendation: ModularMonolith

Several business areas need clear boundaries, but one deployment and one database keep it cheap to run and easy to hand over.

#### Patterns
- **CachedReadModel**: Serve searches from a separate, cached read side so reads never slow down writes.
- **Cdn**: Large files go through a CDN, never through your application servers.
- **PersonalDataProtection**: Collect the minimum, encrypt it, log who read it, and be able to delete it (POPIA/GDPR).

#### Closest reference systems
- Photo-sharing social feed (similarity 0.67): At global scale: media, graph, feed and notification services, a CDN for images, and feeds pre-built per follower (fan-out on write) with a pull fallback for very popular accounts.
- Video or music streaming (similarity 0.50): Microservices with a CDN for the media itself and an event stream for playback events.
- E-commerce shop (similarity 0.43): Modular monolith with a separate catalogue read model (document store, cache, CDN); extract search as a service when traffic grows.

#### Risks to watch
- Modules decay into a big ball of mud unless boundaries are enforced by tests (no cross-module database access).

#### Evolve when
- One module needs to scale or deploy on a different schedule: extract only that module as a service.
- Separate teams own separate modules and block each other's releases.
- Search becomes the bottleneck: move it to a dedicated search service.

Record the final choice with write_adr, including what you rejected.

Judgement: Launch as a modular monolith (Media, Graph, Feed, Notifications) with feeds pre-built per follower on write, images in blob storage behind a CDN. Evolve to services at global scale.

At global scale the advisor's answer is **Microservices**. Evolution trigger: Split Feed and Media into services when fan-out work delays posting by more than 2 seconds at p95, or a single account passes ~100k followers (then switch those accounts to pull-based feeds).
See `decisions/0001-architecture-style.md`.

## Context
```mermaid
flowchart LR
  photographer[Photographer] --> web[Framely web<br/>React + Vite]
  follower[Follower] --> web
  web -- /api JSON + multipart --> api[Framely.Api<br/>modular monolith]
  web -- /media images --> cdn[CDN origin<br/>local: API /media, prod: CDN + blob]
  api --> cdn
  subgraph api_modules[Modules inside Framely.Api]
    media[Media] ~~~ graph[Graph] ~~~ feed[Feed] ~~~ notif[Notifications]
  end
  api --> api_modules
```
Full C4, sequence, data-flow and ER diagrams: `docs/DESIGN.md`.

## Modules and boundaries
| Module | Project | Owns | Public contract | May call |
|---|---|---|---|---|
| Media | `src/Modules/Media/Framely.Media` | upload policy (JPEG, 10 MB, magic bytes), pure C# JPEG APP1/EXIF stripper, photo store (local disk; prod blob), `/media` origin with immutable caching | `IPhotoIngest`, `MediaOptions`, `UseMediaOrigin()`, `AddMediaStorageCheck()` | nothing |
| Graph | `src/Modules/Graph/Framely.Graph` | members, handles, location-sharing setting (opt-in), follow edges | `IMemberDirectory`, `ISocialGraph` | nothing |
| Feed | `src/Modules/Feed/Framely.Feed` | posts, idempotency keys, pre-built per-member feeds (fan-out on write) behind `IFeedStore`, keyset cursors (25/page) | `IFeedService`, port `IFollowerSource` | its port only |
| Notifications | `src/Modules/Notifications/Framely.Notifications` | in-app inbox (minimal stub) | `INotificationSender`, `INotificationInbox` | nothing |
| Api | `src/Framely.Api` | composition root, demo identity filter, ProblemDetails mapping, health, workflows (`PostPhotoWorkflow`, `FollowWorkflow`), `GraphFollowerSource` adapter, Development seed | HTTP | public module contracts |

Rules (enforced by `tests/Framely.Tests/ArchitectureTests.cs`): modules have no project or assembly references to each other or the Api; implementation types live in `*.Internal` namespaces and are `internal`; `InternalsVisibleTo` only for `Framely.Tests`. Each module owns its store; slice 2 gives each its own Postgres schema.

## Cross-cutting
- **Auth:** demo `X-Member-Id` header checked by `RequireMemberFilter` (401 otherwise). OIDC is a later slice; endpoints only use `HttpContext.Viewer()`.
- **Errors:** RFC 9457 ProblemDetails via `DomainExceptionHandler` (413/415 for photos, 400 cursor/caption, 404/409 graph). No stack traces.
- **Time:** `TimeProvider` everywhere (tests use a manual clock).
- **Idempotency:** `Idempotency-Key` on `POST /api/posts`; follows use idempotent `PUT`/`DELETE`.
- **Config:** `IOptions<T>` with validation on start (`Media:*`, `Feed:*`).
- **Telemetry:** `Framely.Feed` meter (`framely.feed.fanout.duration`, `framely.feed.fanout.width`); OpenTelemetry export planned (NuGet unavailable here).
- **Health:** `/health/live` (process), `/health/ready` (media store writable).
- **Media delivery:** production uses blob storage behind a CDN; `Media:PublicBaseUrl` points image URLs at the CDN host. The API's `/media` path only mimics the origin locally.