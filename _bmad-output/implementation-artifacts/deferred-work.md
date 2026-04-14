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

## Deferred from: Story 1-8 (Hot Reload & Fluent UI Contingency) — 2026-04-14

- **Gate emit stage on clean parse of malformed `[Projection]` types** — Story 1.8 Task 2.3 expected contract (a) says "zero generated files for the malformed type". Actual behavior: the generator emits output for any `[Projection]`-decorated type based on Roslyn's error-recovered semantic model. Tolerance properties actually enforced by `MalformedProjection_ToleratedWithoutGeneratorException`: (b) Roslyn surfaces a CS* diagnostic, (c) generator does not throw, (c') generator emits no error-severity diagnostics, (c'') re-compiling the generator's output with the malformed source introduces no *new* CS* errors beyond the original pre-generation set. Together (b–c'') keep the inner loop usable even without (a). Closing (a) requires the emit stage to gate on "all properties parsed cleanly" and is out of Story 1.8 scope.
- **HFC1010 analyzer implementation** — HFC1010 is reserved as a comment in `DiagnosticDescriptors.cs` and a table row in `AnalyzerReleases.Unshipped.md` (Story 1.8 AC4). No `DiagnosticDescriptor` field because the check requires diffing the previous compilation against the current one, which is an analyzer concern, not a generator one. Until implemented, `RS2002` is suppressed in `Hexalith.FrontComposer.SourceTools.csproj`.
- **RS2002 guard test** — `NoWarn;RS2002` is project-wide in `Hexalith.FrontComposer.SourceTools.csproj`, which silences RS2002 for any *future* DiagnosticDescriptor added without an `AnalyzerReleases.Unshipped.md` entry. Add a unit test that enumerates all `DiagnosticDescriptor` fields via reflection and asserts each diagnostic ID has a matching row in `AnalyzerReleases.Shipped.md` + `AnalyzerReleases.Unshipped.md`. Retires the blanket suppression once implemented.

## Deferred from: code review of story 1-8 round 2 — NFR10 deviation (2026-04-14)

- **NFR10 end-to-end latency deviation (harness-dominated) — non-harness re-measurement required** — Story 1.8 AC1 measurement via Aspire MCP + Claude browser recorded harness-level wall-clock latencies of **~38s** (scenario 1.2, add property) and **~23s** (scenario 1.3, add `[Display(Name=…)]`) on Windows 11 / AMD Ryzen 9 9950X3D / .NET SDK 10.0.104 / commit `6769092` (see `docs/hot-reload-guide.md` §3.1 environment block and §3.2). Both values exceed the NFR10 budget (<2s) by >10×, which per AC1 requires a deferred-work entry. **Caveat:** the `dotnet watch` log (`b4pvifrgb.output` lines 48–67) shows `Hot reload succeeded` within ~1s of every `File updated` event, so the rebuild itself is inside budget — the excess is MCP round-trip + Playwright settle overhead, not the thing under test. **Proposed revised threshold (action):** create a non-harness re-measurement task (stopwatch-in-code or minimal console harness timing file-save → assembly-reload) before the next major release. If the non-harness number is still >2s, escalate to an NFR revision or a generator-performance story; if <2s, retire this deferral and update AC1 to explicitly exclude harness-dominated measurements.
