# Deferred Work

## Deferred from: code review of story files 1-3/1-4/1-5/1-6/1-7 (2026-04-14)

- **DisplayLabel unsupported end-to-end** — `BoundedContextAttribute` has no `DisplayLabel` property; parser, IR, and transform all lack support. Story 1-5 review finding acknowledged. No current story owns the fix.
- **Namespace-collision-safe source naming** — Generated source keys rely on simple TypeName, which breaks for same-named projections in different namespaces. Story 1-5 review finding acknowledged. No current story addresses this.
- ~~**Generated Fluxor actions missing CorrelationId**~~ — RESOLVED 2026-04-14 in Story 2-1 Task 0.5. `FluxorActionsEmitter.cs` now emits `CorrelationId` as the leading parameter on `LoadRequestedAction`, `LoadedAction`, and `LoadFailedAction`. Snapshot (`FluxorActionsEmitterTests.Actions_Snapshot.verified.txt`) re-approved; `CounterStoryVerificationTests` updated to pass a generated CorrelationId to each dispatch.
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

## Deferred from: code review of story 2-1-command-form-generation-and-field-type-inference.md (2026-04-14)

- **AC3 density and ARIA live regions** — Story 2.1 AC3 calls for Comfortable density defaults and `aria-describedby` / `aria-live="polite"` on validation messaging. The generated form sets wrapper `aria-label`, per-field labels, and inline `Message`/`MessageState` for numeric parse failures, but does not explicitly set density or region-level `aria-live`. Deferred until someone verifies Fluent UI v5 `FluentTextInput` / `FluentValidationSummary` / `EditForm` defaults (or adds targeted attributes + a bUnit/axe assertion).

## Deferred from: code review of story 1-8 — third review (2026-04-14)

- **CI: isolate performance tests when advisory mode ends** — Today `continue-on-error: true` on the whole `build-and-test` job makes benchmark regressions and functional failures equally non-blocking. When Epic 2 removes that flag, add a dedicated test step filtered with `--filter Category=Performance` and its own `continue-on-error: true` so only perf flakes stay advisory. Documented inline in `.github/workflows/ci.yml` (Gate 3 comment); tracked here so Epic 2 CI work does not lose the intent.

## Deferred from: code review of story 2-1 (2026-04-15)

- **AC3 accessibility verification — axe-core + manual smoke** — Task 9.3/9.4 (manual Counter.Web smoke test of the full 5-state lifecycle, axe-core scan for zero serious/critical violations) were not executed in the 2026-04-14 implementation pass. Gate the follow-up behind the Aspire MCP + Chrome MCP automation path so it stays automated (per user preference). Coverage for AC3 remains unverified end-to-end.
- **Task 8.3–8.7 test coverage gap** — 10 bUnit rendering tests, 9 numeric-converter edge-case tests, 5 FcFieldPlaceholder a11y tests, 3 FsCheck property tests all deferred. `FsCheck.Xunit.v3` was added to `Directory.Packages.props` but is unused. Pick up during story 2-2 or a dedicated test-hardening pass; parseability + Counter compile is not a substitute for per-field render/a11y correctness.
- **Dual command registration path (ADR-012 intent)** — Reflection-based `AddHexalithDomain` command aggregation runs alongside the new generator-emitted `{Command}CommandRegistration.g.cs`; registry dedupes. ADR-012 intended the generator to be the single source. Remove the reflection path once all adopters have regenerated.
- **Task 7.2 `_Imports.razor`** — Counter.Web should have `@using Counter.Domain` added so `<IncrementCommandForm />` can be used without a FQN. Cosmetic; current FQN usage works.
- **Parent-driven `OnParametersSet` on generated form** — When a parent passes a changing `InitialValue`, the emitted form does not re-initialize `_model`. v0.1 has no documented parent-driven reinit requirement; revisit if adopters need it.
- **`AddHexalithFrontComposer` duplicate `Configure` no-op** — Repeated calls accumulate no-op `services.Configure<StubCommandServiceOptions>(_ => { })` registrations. Low-impact; consider a `TryAdd*`-style guard later.
- **Dead `NumberStyles_Any` helper property** — Emitted per numeric field but never referenced. Code-gen cleanup only.
- **CounterProjectionEffects increment race** — Concurrent `ConfirmedAction` events read `_state.Value.Items[0].Count` before the prior `LoadedAction` commits, dropping increments under rapid clicks. Sample code only; documented in Completion Notes.

## Added during patch pass (2026-04-15)

- **DateOnly / DateOnly? `FluentDatePicker` emission** — Fluent UI v5 `FluentDatePicker` is bound to `DateTime?`; `FluentDatePicker<DateOnly>` fails to compile at adopter time. Counter sample does not use `DateOnly`, so not blocking. Route via `FluentTextInput type="date"` with parse converter when the first real `DateOnly`-shaped command lands (or mark unsupported via HFC1004).
- **Enum without a zero-defined member** — `FluentSelect` binds `_model.{Prop} = 0` by default and renders the literal `"0"`. Emit an initializer that picks the first declared value, or surface via `[Required]` selection. Revisit when the first command with a non-zero-default enum lands.
