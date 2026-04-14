# Deferred Work

## Deferred from: code review of story files 1-3/1-4/1-5/1-6/1-7 (2026-04-14)

- **DisplayLabel unsupported end-to-end** — `BoundedContextAttribute` has no `DisplayLabel` property; parser, IR, and transform all lack support. Story 1-5 review finding acknowledged. No current story owns the fix.
- **Namespace-collision-safe source naming** — Generated source keys rely on simple TypeName, which breaks for same-named projections in different namespaces. Story 1-5 review finding acknowledged. No current story addresses this.
- **Generated Fluxor actions missing CorrelationId** — ADR-008 / Story 1-3 mandates all actions include `CorrelationId`. `FluxorActionsEmitter.cs` generates actions without it. Pre-existing gap between the architecture decision and the generator implementation.
- **Release workflow NuGet push ordering risk** — In semantic-release plugin chain, `@semantic-release/exec` (NuGet push) runs before `@semantic-release/github` (GitHub Release). If GitHub Release fails, packages are already on NuGet with no rollback. Matches the EventStore pattern — a known inherited risk.

## Deferred from: code review of story 1-5 round 2 (2026-04-14)

- **Conflicting DisplayLabels across BoundedContext** — Two projections sharing the same `[BoundedContext]` but specifying different `DisplayLabel` values emit silently with no diagnostic. Deferred until bounded-context aggregation is implemented (likely Story 1-6 or later).

- **BoundedContext name with invalid C# identifier chars** — Names like `"My Orders"` or `"Order-Management"` produce invalid class names in `RegistrationEmitter`. Pre-existing; no sanitization or diagnostic exists.
- **Hint name sanitization for exotic namespace formats** — `GetQualifiedHintPrefix` does not sanitize namespaces from `ToDisplayString()`. Theoretical risk with `global::` or generic types. Pre-existing.
- **XML doc comment escaping** — BoundedContext name with `<`, `>`, or `&` chars is placed into XML doc comments without escaping, potentially breaking doc tooling. Pre-existing across all emitters.
- **Incremental generator caching edge case** — Changing only `DisplayLabel` on `[BoundedContext]` in a separate partial declaration may not trigger re-generation if `[Projection]` is on a different partial. Speculative; needs investigation.

## Deferred from: code review of 1-7-ci-pipeline-and-semantic-release.md (2026-04-14)

- **Stable release packing fails while Shell depends on prerelease Fluent UI** — `dotnet pack Hexalith.FrontComposer.sln --no-build --configuration Release --output ./nupkgs -p:Version=1.2.3` currently fails with `NU5104` because `Hexalith.FrontComposer.Shell` depends on `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1`. Pre-existing dependency choice surfaced by Story 1.7 release automation.
- **Shallow clone + 3-level submodule nesting may cause CI failures** — FrontComposer → EventStore → Tenants chain with `fetch-depth: 1` + `submodules: recursive` is fragile when gitlinks point to non-HEAD commits in deeply nested submodules. Pre-existing architecture.
- **CI and Release race on push to main** — Both workflows trigger on `push: branches: [main]`. CI has `cancel-in-progress: true`, Release has `false`. When advisory mode is removed in Epic 2, a fast-follow merge could ship un-CI-validated code. Needs `workflow_run` or `needs:` dependency.
- **@semantic-release/git push may fail with persist-credentials: false** — Release checkout uses `persist-credentials: false` with manual remote URL token injection. If `@semantic-release/git` uses a different push mechanism, the CHANGELOG commit push will fail. Will surface on first release attempt.
