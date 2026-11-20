# 0001. Architecture style

- Status: Accepted
- Date: 2026-11-19

## Context

Local photographers have no photo-first community of their own: big platforms bury their work under ads and video, and they can't see who actually follows their craft. Launch traits: MediaDelivery, ReadHeavy, PersonalData, SeveralBusinessAreas, SmallTeam. At global scale the advisor recommends Microservices.

## Decision

Launch as a modular monolith (Media, Graph, Feed, Notifications) with feeds pre-built per follower on write, images in blob storage behind a CDN. Evolve to services at global scale. Evolution trigger: Split Feed and Media into services when fan-out work delays posting by more than 2 seconds at p95, or a single account passes ~100k followers (then switch those accounts to pull-based feeds).

## Alternatives considered

- Microservices from day one: what the advisor recommends at global scale (reference #19), but three engineers can't run it and launch traffic doesn't need it.
- Single layered app with feeds computed on read: simplest, but feed queries get slower with every follow and every post.

## Consequences

- Module boundaries match the future services, so extraction is a deployment change, not a rewrite.
- One deployable and one database for the launch team to run.
