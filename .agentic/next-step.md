# Next step

- **Doing:** nothing in progress; slice 1 is done and verified (75 .NET tests, 13 Vitest, smoke curls on :5101).
- **Done so far:** Sketch leg (`ux/`), slice 1 end to end (API + web), design dossier `docs/DESIGN.md`.
- **Next action:** start slice 2 (persistence): add Postgres + EF Core behind `IMemberDirectory`/`ISocialGraph`, `IFeedStore` and the post store, one schema per module; first write a failing integration test with Testcontainers in CI.
- **Blocked by:** NuGet is blocked in the build workspace (EF Core, Testcontainers, OpenTelemetry need CI or a package mirror).
