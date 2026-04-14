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
