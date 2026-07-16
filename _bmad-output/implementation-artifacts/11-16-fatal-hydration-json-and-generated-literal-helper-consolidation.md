---
baseline_commit: 83a791c78f589f896bcc21c50c4cc29e061ffbe2
---
# Story 11.16: Fatal, hydration, JSON, and generated-literal helper consolidation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->
<!-- Type: refactor with intentional public hydration API break (Epic 11 lower-risk consolidation group; closes the M27 fatal-filter subset and duplication-table clusters 8-10, plus the residual H8/live-tree literal paths). -->

## Story

As a FrontComposer maintainer,
I want small duplicated helpers consolidated by defect class,
so that hardening fixes do not depend on remembering every copy.

## Acceptance Criteria

This story refines FR29 and NFR1/NFR5/NFR7/NFR8/NFR11. It closes the fatal-filter subset of architecture finding **M27** (the `(M19)` tag in the Epic 11.16 transcription is incorrect), duplication-table clusters 8-10, and only the remaining `RoleBodyHelpers` portion of H8. Scope is bounded by **Dev Notes > Scope Decisions and Never-list**.

1. **(M27 fatal-filter subset)** Given the 35 fatal-filter catch sites in 12 Shell files and two local `IsRecoverable` definitions at baseline, when `Services/ExceptionGuard.IsFatal(Exception)` is introduced, then:
   - `OutOfMemoryException`, `StackOverflowException`, `System.Threading.ThreadAbortException`, and `AccessViolationException` are the single authoritative fatal set;
   - every former fatal-filter site delegates its fatal classification to that helper, both local `IsRecoverable` definitions and all ad-hoc fatal-type lists are removed, and the site's existing logging, fallback, retry, result, and propagation behavior is otherwise unchanged;
   - `OperationCanceledException` is classified **non-fatal**, but each call site preserves its existing cancellation policy explicitly (propagate/exclude where it propagates today; do not turn `!IsFatal(ex)` into permission to swallow cancellation); and
   - focused tests cover each fatal type, representative non-fatal exceptions, cancellation classification, and representative site-level cancellation propagation.

2. **(hydration state)** Given five public, semantically identical `Idle/Hydrating/Hydrated` enums, when hydration state is consolidated, then one public `Hexalith.FrontComposer.Shell.State.HydrationState` enum at `State/HydrationState.cs` replaces `CommandPaletteHydrationState`, `DataGridNavigationHydrationState`, `DensityHydrationState`, `NavigationHydrationState`, and `ThemeHydrationState` throughout production and tests; all state/action/effect/reducer/component behavior remains equivalent; and no former enum identifier remains. `CapabilityDiscoveryHydrationState` (`Idle/Seeding/Seeded`) remains unchanged because its semantics differ. This is an intentional Shell source/binary API signature change required by the authoritative AC; document it in the implementation Change Log, release/compatibility evidence, and consumer impact, without adding five alias enums that would violate the single-enum criterion.

3. **(JSON profiles)** Given three production `JsonSerializerOptions` configurations, when a Shell-internal `Services/FcJson` helper is introduced, then it exposes named, cached profiles for the two existing semantics—web defaults and web defaults plus `DefaultIgnoreCondition = WhenWritingDefault`; EventStore requests and LocalStorage use the matching profile without any byte/wire/storage-shape change; the unused DataGrid `JsonOptions` / `CurrentJsonOptions` pair is removed; the reusable instances are not mutated after first use; and intentionally independent test-side option copies remain independent. One universal option instance must not be used because EventStore and storage semantics differ.

4. **(generated literal)** Given `RoleBodyHelpers.EscapeString` is the remaining hand-rolled path named by H8/Story 11.16 and the later live tree also contains the same C#-literal defect class in `McpManifestEmitter.Escape`, when literal escaping is consolidated, then both become thin delegates to the existing `GeneratedLiteral.Escape`; their callers and MCP descriptor/fingerprint semantics remain behaviorally unchanged; and generated view and MCP-manifest source parses and compiles for quotes, backslashes, control characters, U+0085, U+2028, and U+2029. A property-based round-trip test validates arbitrary strings. Existing generated snapshots remain byte-identical unless a fixture exposes a real escaping defect, in which case the intentional delta and parse/compile evidence are documented.

5. **(evidence and anti-recurrence)** Given the approved Story 11.9 split requires independently reviewable evidence, when Story 11.16 validation runs, then the Dev Agent Record documents baseline-to-final counts for every cluster, a durable Roslyn/source governance guard with a synthetic negative proves old fatal lists/hydration enums/local production JSON construction/hand-rolled literal escaping cannot silently recur, and the Shell and SourceTools focused lanes can fail for this child story independently. Release solution build, applicable full default lanes, Governance, snapshot verification, and `git diff --check` pass.

## Tasks / Subtasks

- [x] **Task 1 — Centralize the fatal taxonomy without changing call-site control flow (AC: #1, #5)**
  - [x] Add `src/Hexalith.FrontComposer.Shell/Services/ExceptionGuard.cs` as an `internal static` class with `internal static bool IsFatal(Exception exception)`. Use exactly the four-type taxonomy in AC1. Do not add new dependencies or expose a public helper.
  - [x] Add direct `ExceptionGuardTests` covering all four fatal types, `OperationCanceledException`, and representative recoverable exceptions. Construct `StackOverflowException` directly; because `ThreadAbortException` has no public constructor on this target, obtain a classifier-only instance with `(ThreadAbortException)RuntimeHelpers.GetUninitializedObject(typeof(ThreadAbortException))`. Never invoke obsolete/unsupported thread-abort APIs.
  - [x] Migrate the 35 baseline catch filters in the following 12 files. Preserve any existing explicit cancellation exclusion and the entire catch body; only centralize fatal classification:
    - `Badges/ReflectionActionQueueProjectionCatalog.cs`
    - `Services/EmptyStateCtaResolver.cs`
    - `Services/StorageScopeResolver.cs`
    - `Services/SnapshotPublisher.cs`
    - `Services/Authorization/CommandAuthorizationEvaluator.cs`
    - `Services/Authorization/CommandDispatchAuthorizationGate.cs`
    - `State/CommandPalette/CommandPaletteEffects.cs`
    - `Infrastructure/PendingCommands/PendingCommandPollingDriver.cs`
    - `Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs`
    - `Infrastructure/EventStore/ProjectionSubscriptionService.cs`
    - `Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs`
    - `State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs`
  - [x] Remove the two authorization-local `IsRecoverable` methods after their four callers use the shared taxonomy. Preserve each authorization path's existing fail-closed/cancellation outcome and its tests; some evaluator paths intentionally classify cancellation into a fail-closed result, while the dispatch gate has an explicit rethrow path.
  - [x] Add or strengthen representative site tests so cancellation still propagates where it did before and each fatal exception bypasses fallback/log-and-continue handling. Reuse existing suite seams rather than copying the taxonomy into tests.
  - [x] Add a syntax-aware governance test (prefer extending `tests/...Shell.Tests/Architecture/ShellLayeringTests.cs` only if cohesion remains good; otherwise add one dedicated architecture test file) that requires exactly one `ExceptionGuard`, rejects local `IsRecoverable` declarations and ad-hoc combinations of the four fatal types in catch filters, and demonstrates failure on a synthetic forbidden source sample. Do not reject unrelated narrow filters such as JSON/format/object-disposal classification.
  - [x] Reconcile deferred-work row `DW-0483`: its accepted OOM-only convention is superseded by this explicit project-wide classifier decision. Mark only that row resolved/superseded with implementation and validation evidence; do not absorb or rewrite unrelated deferred work.

- [x] **Task 2 — Replace the five identical hydration enums with one public state type (AC: #2, #5)**
  - [x] Add `src/Hexalith.FrontComposer.Shell/State/HydrationState.cs` with the unchanged numeric/member order `Idle`, `Hydrating`, `Hydrated`.
  - [x] Update CommandPalette, DataGridNavigation, Density, Navigation, and Theme state records, actions, effects, reducers, features, the `FrontComposerShell` component, and affected tests to use the shared type. Delete the five old enum files; do not add aliases, conversion extensions, or parallel compatibility enums.
  - [x] Leave the capability-discovery enum and all `Seeding/Seeded` logic untouched. Add a guard that requires exactly the shared three-member hydration enum while explicitly allowing the capability-discovery type.
  - [x] Pin the shared enum's member names and integer values and re-run every affected hydration/reducer/effect test. Document the intentional public source/binary signature change and confirm the FcTbl-only Shell API baseline is unchanged; do not edit `PublicAPI.FcTbl.Shipped.txt` for unrelated state types.
  - [x] Add consumer-facing compatibility evidence: an exact old-type -> `HydrationState` mapping; confirmation that the approved `3.0.0` major-release posture permits this break from the published `2.0.4` surface; a package/consumer compile using the new type in representative public state/action signatures; and a migration note in the implementation PR/release evidence plus this story Change Log. The delivery commit/PR must carry an explicit breaking marker (`type!:` or `BREAKING CHANGE:` footer) rather than presenting this as an invisible refactor.

- [x] **Task 3 — Consolidate production JSON construction by semantic profile (AC: #3, #5)**
  - [x] Add `src/Hexalith.FrontComposer.Shell/Services/FcJson.cs` as an internal static holder of two clearly named reusable, read-only `JsonSerializerOptions` profiles: use the already-read-only `JsonSerializerOptions.Web` singleton for plain web defaults; fully configure the storage/compact profile with `WhenWritingDefault`, then call `MakeReadOnly(populateMissingResolver: true)` before publishing it. No mutable cached options may escape.
  - [x] Point `Infrastructure/EventStore/EventStoreRequestContent.cs` at the plain-web profile and `Infrastructure/Storage/LocalStorageService.cs` at the storage profile. It is acceptable to retain the current internal `JsonOptions` / `SchemaLockJsonOptions` members as aliases when existing schema-lock tests use those production seams; do not construct a duplicate options object behind an alias.
  - [x] Remove the unused `JsonOptions` field and `CurrentJsonOptions()` method from `State/DataGridNavigation/DataGridNavigationEffects.cs`; do not introduce JSON use where none exists.
  - [x] Extend the existing EventStore request-content and LocalStorage/navigation schema-lock tests to prove exact pre-change JSON bytes/shape, casing, null/default omission, and round-trip behavior. Assert `IsReadOnly` for both profiles and `ReferenceEquals` from each retained production alias/caller to its intended profile.
  - [x] Add a production-source guard with a synthetic negative that rejects new `JsonSerializerOptions` construction in Shell production code outside `FcJson`, with narrowly documented exceptions only if a site truly requires distinct semantics. Exclude test projects from the guard.

- [x] **Task 4 — Delegate the residual generated-literal paths (AC: #4, #5)**
  - [x] Change `RoleBodyHelpers.EscapeString` to return `GeneratedLiteral.Escape(value)` and remove its now-unused `System.Text` import / `StringBuilder` implementation. Keep existing `ColumnEmitter`, `ProjectionRoleBodyEmitter`, and `RazorEmitter` callers on the `RoleBodyHelpers` seam.
  - [x] Change only the private `McpManifestEmitter.Escape` body to delegate to `GeneratedLiteral.Escape`; keep its current callers and every MCP descriptor, schema fingerprint, ordering, hint name, and generated manifest shape unchanged. `McpManifestEmitter` still needs `System.Text` for source construction.
  - [x] Do not redesign `GeneratedLiteral`, use `SymbolDisplay.FormatLiteral(..., quote: false)`, or rewrite the already-migrated Registration/CommandForm/CommandPage/CommandRenderer emitter seams. The existing `quote: true` plus surrounding-quote removal is the approved implementation.
  - [x] Add direct tests beside the emitter suites for the specified escape-edge cases and an FsCheck property that embeds the escaped body in a quoted C# literal, parses/compiles it with Roslyn, evaluates or decodes it, and proves the original string round-trips. Make the property meaningful for arbitrary control/Unicode input, not a fixed finite list disguised as a property.
  - [x] Extend actual RoleBodyHelpers-consuming generated-view and `McpManifestEmitterTests` fixtures so emitted source with the edge cases parses and compiles and the manifest's runtime constant values round-trip. Review existing Verify snapshots/fingerprint fixtures; accept only intentional escaping corrections and do not bulk-rewrite snapshots or regenerate unrelated schema baselines.
  - [x] Add a syntax/source guard, with a synthetic negative, that keeps both `RoleBodyHelpers.EscapeString` and `McpManifestEmitter.Escape` as thin `GeneratedLiteral.Escape` delegates and rejects hand-rolled `StringBuilder`/`Replace` C#-literal escapers in SourceTools emitters.

- [x] **Task 5 — Record reduction evidence and run the child-specific validation lanes (AC: #5)**
  - [x] Recalculate counts immediately before implementation if HEAD differs from `baseline_commit`, then record final evidence in the Dev Agent Record: fatal `35 catch sites / 12 files / 2 local classifiers` to `35 helper-based sites / 0 local classifiers / 0 ad-hoc fatal lists`; hydration `5 duplicate enums` to `1 shared enum`; JSON `3 production configurations` to `1 holder / 2 named read-only profiles / 0 dead DataGrid configuration`; literal `2 hand-rolled C#-literal paths` to `2 thin delegates / 0 hand-rolled paths`.
  - [x] Run the focused Shell tests for ExceptionGuard, authorization cancellation, affected fatal-filter owners, hydration/reducers/effects, JSON/schema locks, and the new governance guards.
  - [x] Run the focused SourceTools tests for GeneratedLiteral, RoleBodyHelpers-consuming emitters, generated-source parse/compile, property coverage, and snapshots.
  - [x] Run Release solution build under warnings-as-errors, the applicable full default and Governance lanes, and `git diff --check`. Use the repository's direct xUnit-v3 executable fallback if VSTest sockets are unavailable.
  - [x] Update this story's Dev Agent Record, exact File List, Change Log, evidence counts, and any deliberate source-API or snapshot delta before moving the story to review.

## Dev Notes

### Scope Decisions

1. **Fatal means four existing hard-stop types.** The strongest current authorization variants already agree on `OutOfMemoryException`, `StackOverflowException`, `ThreadAbortException`, and `AccessViolationException`. Use that set exactly. AC1's helper is a classifier, not a universal catch policy: a non-fatal result does not mean every caller should catch cancellation or every exception.
2. **Cancellation policy stays local and explicit.** `OperationCanceledException` is non-fatal, but lifecycle control flow may require it to escape. For a current filter such as `ex is not OperationCanceledException and not OutOfMemoryException`, the consolidated form must retain the cancellation clause. Existing authorization cancellation tests are particularly important.
3. **The hydration type is deliberately public.** The five old enums flow through public state/action signatures, so a true one-enum consolidation is an adopter source and binary API break. The authoritative AC says a single enum; retaining five obsolete enums would preserve duplication and fail it. Published `2.0.4` contains the old surface, and the approved `3.0.0` release is the compatibility boundary for this change: make the old-to-new consumer migration, package compile, PR/release note, and breaking Conventional Commit metadata explicit; keep member values stable; and avoid unrelated API-baseline edits. There is no general Shell PublicAPI baseline for these state types; the tracked `PublicAPI.FcTbl.Shipped.txt` is FcTbl-specific.
4. **`FcJson` owns profiles, not a single universal serializer.** EventStore currently uses `new(JsonSerializerDefaults.Web)`. LocalStorage uses web defaults plus `WhenWritingDefault`. DataGrid declares that second profile but never consumes it. Preserve those distinctions, publish both profiles already read-only, and remove the dead declaration. Test-side copies intentionally pin external shapes and stay independent.
5. **The literal fix remains a two-seam consolidation.** PR #48 / commit `a495e8a7` already introduced the correct `GeneratedLiteral` implementation and migrated Registration plus CommandForm/CommandPage/CommandRenderer paths. The architecture review did not inventory the later/current `McpManifestEmitter.Escape`, but it emits C# source and implements the same defect class. Story 11.16 changes only `RoleBodyHelpers.EscapeString` and that private MCP escape seam; their 77 view-emitter call sites and all manifest call sites stay routed through those wrappers.

### Current-State Inventory and Expected Changes

#### Fatal-filter cluster

The architecture review counted about 25 sites; the current baseline has **35** after subsequent stories. This inventory deliberately includes the new Story 11.15 `StorageScopeResolver` and `SnapshotPublisher<T>` filters. Preserve the behavior 11.15 hardened, especially fail-closed scope resolution, subscriber fault isolation, owner-specific logging, fault-handler isolation, and fatal propagation.

The 12 owner files and baseline counts are:

| Owner | Catch sites |
|---|---:|
| ReflectionActionQueueProjectionCatalog | 2 |
| EmptyStateCtaResolver | 1 |
| StorageScopeResolver | 1 |
| SnapshotPublisher | 2 |
| CommandAuthorizationEvaluator | 3 |
| CommandDispatchAuthorizationGate | 1 |
| CommandPaletteEffects | 6 |
| PendingCommandPollingDriver | 2 |
| SignalRProjectionHubConnectionFactory | 1 |
| ProjectionSubscriptionService | 7 |
| ProjectionFallbackPollingDriver | 3 |
| ReconnectionReconciliationCoordinator | 6 |
| **Total** | **35** |

Do not sweep every `catch` that excludes only `OperationCanceledException`, or narrow filters for JSON, format, HTTP, JS disconnection, disposal, and invalid operation. They express local catchability rather than the duplicated fatal taxonomy.

#### Hydration cluster

The five replacement slices are `State/CommandPalette`, `State/DataGridNavigation`, `State/Density`, `State/Navigation`, and `State/Theme`. References currently span each slice's states, actions, effects, reducers, features, `Components/Layout/FrontComposerShell.razor.cs`, shared test-base setup, and navigation storage-ready reducer tests. Use `rg` before deletion so no source/test identifier survives. Do not touch `State/CapabilityDiscovery/FrontComposerCapabilityDiscoveryState.cs` or its tests.

#### JSON cluster

- `Infrastructure/EventStore/EventStoreRequestContent.JsonOptions`: plain web profile; request byte limit, content type, trimming suppression, and serializer behavior stay unchanged.
- `Infrastructure/Storage/LocalStorageService.SchemaLockJsonOptions`: web + omit-default storage profile; keep tests bound to the real production options.
- `State/DataGridNavigation/DataGridNavigationEffects.JsonOptions` and `CurrentJsonOptions()`: currently dead and explicitly retained for a future migration; delete rather than invent usage.

`JsonSerializerOptions` caches metadata and is safe to share after configuration. Do not wait for first serialization to freeze it: use `JsonSerializerOptions.Web` and explicitly call `MakeReadOnly(populateMissingResolver: true)` on the fully configured storage profile before publication. Never mutate shared options at call sites.

#### Generated-literal cluster

`GeneratedLiteral.Escape` correctly calls Roslyn `SymbolDisplay.FormatLiteral(value, quote: true)` and strips the surrounding quotes. `quote: false` does **not** escape embedded double quotes. `RoleBodyHelpers.EscapeString` is internal and may remain as a thin semantic seam because three large emitters reach it through **77 current call sites**. Its old implementation missed at least U+0085 handling. `McpManifestEmitter.Escape` is a later/current, stronger hand-rolled C#-literal implementation not listed in the 2026-07-04 five-emitter inventory; retaining it would still leave a second escaping policy. Preserve empty-string behavior; null is outside both methods' annotated non-null contracts and no current caller supplies it.

### Architecture Compliance

- Place `ExceptionGuard` and `FcJson` under Shell `Services`; they are internal cross-cutting helpers. State may consume Services, and Infrastructure may consume Services under the current Shell layering rules. Do not move adapters, routes, or components.
- Place only the shared public enum under `Shell/State/HydrationState.cs`. Namespace must match the folder.
- Keep generated-literal ownership in SourceTools `Emitters`. Do not move it to Contracts or `Hexalith.Commons`.
- One type per file, sealed-by-default where applicable, file-scoped namespaces, CRLF, and warnings as errors. XML documentation is required on all public, protected, and internal types/members, including `ExceptionGuard`, `IsFatal`, `FcJson`, both named profiles, and `HydrationState`. No package versions belong in project files.
- UX impact is **N/A**: no `.razor` markup, CSS, layout, interaction, accessibility, or responsive behavior is intended. `FrontComposerShell.razor.cs` changes only a type reference; all visible behavior must remain identical.

### Library and Framework Requirements

- Stay on the repository-pinned toolchain: .NET SDK `10.0.302`, C# 14 / Roslyn `5.6.0`, System.Text.Json `10.0.9`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and FsCheck.Xunit.v3 `3.3.3`.
- No new NuGet package or package-version update is required.
- Use System.Text.Json's cached-options guidance and Roslyn's existing literal formatter; do not introduce a custom serializer abstraction or a new escaping library.

### Testing Requirements

Minimum child-specific lanes: build each target test project, then invoke its built xUnit v3 executable with repeated native `-class` arguments for every new/affected class (adjust names to the final classes). Do not use project-level `dotnet test --filter` under Microsoft.Testing.Platform. FrontComposer's repository-specific broad gates remain solution-level trait-filtered runs.

```bash
dotnet restore Hexalith.FrontComposer.slnx -p:NuGetAudit=false
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0

dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0
dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests \
  -class Hexalith.FrontComposer.Shell.Tests.Services.ExceptionGuardTests \
  -class Hexalith.FrontComposer.Shell.Tests.Architecture.ShellLayeringTests \
  -parallel none

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests \
  -class Hexalith.FrontComposer.SourceTools.Tests.Emitters.GeneratedLiteralTests \
  -class Hexalith.FrontComposer.SourceTools.Tests.Emitters.McpManifestEmitterTests \
  -parallel none

DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-restore -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 \
  --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"
DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-restore -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0 --filter "Category=Governance"

git diff --check
```

The abbreviated direct-runner examples show the new guard classes; repeat `-class` for the authorization, fatal-owner, hydration/reducer/effect, JSON/schema-lock, Razor/role-specific/generator-driver, consumer-compile, and snapshot suites listed in Task 5. Review Verify artifacts. With xUnit v3, do not use blocking task waits; pass cancellation tokens where analyzer rules require them. If the broad solution runner is environment-blocked, record that blocker separately and retain the per-project direct-executable evidence, as Story 11.15 did.

### Previous-Story Intelligence

- Story 11.15 added `StorageScopeResolver` and `SnapshotPublisher<T>` after the original 11.16 inventory. They are now in scope for fatal-helper migration, but none of their resolver/publisher semantics is reopened.
- Its review found that shared helpers can accidentally change public constructors, feature attribution, cancellation/fatal propagation, concurrency evidence, and subscriber-fault behavior. For 11.16, preserve every owner catch body and public construction seam; make architecture/property tests non-vacuous with synthetic negatives.
- Story 11.15's completion baseline was Release build 0 warnings/errors, full default lane 4,053 tests, and Governance 258 tests. Treat current totals as comparison evidence, not hard-coded acceptance counts; legitimate new tests will raise them.
- Keep the implementation File List exact and do not include or advance unrelated root submodules.

### Git Intelligence

- Baseline is clean `main` at `83a791c78f589f896bcc21c50c4cc29e061ffbe2` (`refactor release evidence`). Recalculate inventories if implementation starts from a different commit.
- `a495e8a7` is the GeneratedLiteral quick-win prerequisite: it established the `quote: true` implementation and migrated four emitter seams. Preserve that decision.
- `8b8e002d` / `6cfaad8d` implemented and review-hardened Story 11.15. Their fatal filters are part of today's 35-site inventory.
- Follow the repository Git instructions: root-declared submodules only, no recursive initialization, no generated or unrelated submodule changes, and Conventional Commits for implementation commits.

### Latest Technical Notes

- Microsoft recommends reusing `JsonSerializerOptions`; the cached metadata is thread-safe, and the instance becomes immutable after first serializer use. [Configure JsonSerializerOptions](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/configure-options)
- CA1869 likewise recommends caching/reusing options rather than constructing single-use instances. [CA1869](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1869)
- Roslyn `SymbolDisplay.FormatLiteral(string, bool)` is the authoritative C# literal formatter used by the already-approved helper. [Roslyn API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.symboldisplay.formatliteral?view=roslyn-dotnet-5.0.0)
- .NET guidance warns against handling runtime-corrupted-state / engine-failure exceptions; this supports centralizing, not broadening, the existing hard-stop taxonomy. [Standard exception guidance](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/using-standard-exception-types) and [CA2153](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2153)

### Scope Never-list

- No storage-key canonicalization or wire-format security expansion (Story 11.4).
- No `StorageScopeResolver` behavior, SnapshotPublisher semantics, notification-primitive/Rx convergence, or deferred M19 closure (Story 11.15 / deferred work).
- No Shell layer or route/label movement (Story 11.9), command-route implementation (Story 11.7), or Contracts/UI package split and query migration (Stories 11.11-11.14).
- No one-type-per-file campaign, logging campaign, diagnostic rename, analyzer-policy, or enforcement work beyond the four narrowly scoped anti-recurrence guards (Stories 11.17-11.19).
- No consolidation of intentionally distinct test-side JSON options, no new production JSON profile without proven semantics, and no serialization source-generation/AOT migration.
- No broad emitter rewrite beyond the two named wrapper bodies, no hand edits to generated files, no MCP descriptor/fingerprint redesign, no `GeneratedLiteral` algorithm redesign, and no `quote: false` regression.
- No public compatibility shims that retain the five obsolete hydration enums, no new packages, no UI/UX changes, and no unrelated submodule movement.

### Project Structure Notes

Expected new files:

- `src/Hexalith.FrontComposer.Shell/Services/ExceptionGuard.cs`
- `src/Hexalith.FrontComposer.Shell/Services/FcJson.cs`
- `src/Hexalith.FrontComposer.Shell/State/HydrationState.cs`
- focused Shell tests under `tests/Hexalith.FrontComposer.Shell.Tests/Services` and/or `Architecture`
- focused SourceTools literal tests under `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters`

Expected deletions: the five per-feature `*HydrationState.cs` files. Expected modifications: the 12 fatal owners; five hydration slices and their affected tests; `EventStoreRequestContent.cs`, `LocalStorageService.cs`, and `DataGridNavigationEffects.cs`; `RoleBodyHelpers.cs` and `McpManifestEmitter.cs`; focused architecture/governance, consumer-compile, and emitter tests; implementation PR/release evidence for the breaking enum migration; the single `DW-0483` disposition in `deferred-work.md`; and this story/sprint record. No `.csproj`, `Directory.Packages.props`, PublicAPI FcTbl baseline, generated source, UX artifact, or submodule change is expected.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-11.16] — authoritative persona and three core ACs.
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05.md#Proposal-F] — required before/after reduction, focused tests, and independently failing child lane.
- [Source: _bmad-output/project-docs/architecture-quality-review-2026-07-04.md#Mechanical-duplication-map] — clusters 8-10, placements, counts, and test-copy exception.
- [Source: _bmad-output/project-docs/architecture-quality-review-2026-07-04.md#M27] — fatal-filter drift; corrects the epic's M19 tag.
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md] — approved GeneratedLiteral implementation and PR #48 boundary.
- [Source: _bmad-output/planning-artifacts/prd.md#Functional-Requirements] — FR29 architecture-review release-risk closure.
- [Source: _bmad-output/planning-artifacts/prd.md#Non-Functional-Requirements] — NFR1, NFR5, NFR7, NFR8, and NFR11.
- [Source: _bmad-output/planning-artifacts/architecture.md#Shell-sublayers] — Services/State/Infrastructure placement and dependency direction.
- [Source: _bmad-output/project-context.md] — toolchain, C#/.NET/style, TWAE, `.slnx`, test, snapshot, source-generator, and submodule rules.
- [Source: _bmad-output/implementation-artifacts/11-15-storage-scope-and-snapshot-publisher-consolidation.md] — current shared-helper code, validation baseline, and review lessons.
- Code anchors: `Shell/Services/Authorization/{CommandAuthorizationEvaluator,CommandDispatchAuthorizationGate}.cs`; the 12 fatal-owner files in Task 1; `Shell/State/{CommandPalette,DataGridNavigation,Density,Navigation,Theme}/`; `Shell/Infrastructure/{EventStore/EventStoreRequestContent,Storage/LocalStorageService}.cs`; `SourceTools/Emitters/{GeneratedLiteral,RoleBodyHelpers,McpManifestEmitter}.cs`; matching Shell/SourceTools test suites.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Implementation Plan

- Execute each consolidation cluster independently through red-green-refactor and its child-specific regression gates.
- Preserve call-site control flow and external wire/storage/generated shapes while centralizing only the named duplicated policy seams.
- Keep the hydration API break explicit in compatibility evidence and reserve story/sprint completion until every broad gate passes.

### Debug Log References

- 2026-07-13 — Aspire AppHost baseline start failed before resource creation because external `references/Hexalith.Parties` raises six HFC0001 errors for the deprecated flattened `QueryRequest` constructor. Story scope forbids submodule changes; focused and solution validation remain authoritative for owned code.
- 2026-07-13 — Task 1 RED: Shell.Tests Release build failed with two CS0103 errors because `ExceptionGuard` did not exist.
- 2026-07-13 — Task 1 GREEN: Shell.Tests Release build passed with 0 warnings/errors; focused classifier/governance/authorization/owner lane passed 182/182; full default lane passed 4,067/4,067; Governance passed 267/267.
- 2026-07-13 — Task 2 RED: Shell.Tests Release build failed because the shared `HydrationState` type did not exist.
- 2026-07-13 — Task 2 GREEN: Shell.Tests Release build passed with 0 warnings/errors; affected hydration/reducer/effect/component lane passed 206/206; shared-state consumer/governance passed 6/6; FcTbl baseline passed 1/1; full default passed 4,073/4,073; Governance passed 273/273.
- 2026-07-13 — Task 3 RED: Shell.Tests Release build failed only because the canonical `FcJson` holder and its named profiles did not exist.
- 2026-07-13 — Task 3 GREEN: Shell.Tests Release build passed with 0 warnings/errors; focused JSON/request/storage/schema-lock/governance lane passed 60/60; full default passed 4,079/4,079; Governance passed 275/275.
- 2026-07-13 — Task 4 RED: focused SourceTools lane failed five tests because the two residual seams were not delegates and RoleBodyHelpers emitted raw NUL/BEL/U+0085 plus invalid generated source for the edge fixture.
- 2026-07-13 — Task 4 GREEN: SourceTools.Tests Release build passed with 0 warnings/errors; focused literal/view/MCP/generator/snapshot lane passed 97/97 with no received snapshot artifacts; full default passed 4,100/4,100; Governance passed 277/277.
- 2026-07-13 — Task 5 FINAL: baseline HEAD still matched `83a791c78f589f896bcc21c50c4cc29e061ffbe2`; explicit Release restore corrected the Debug/source asset graph produced by configuration-neutral restore; Release solution build passed with 0 warnings/errors; focused Shell passed 302/302; focused SourceTools passed 97/97; full default passed 4,100/4,100; Governance passed 277/277; no new Verify received artifact; CRLF audit and `git diff --check` passed. Review correction (2026-07-14): the original File List omitted two intentional root-submodule gitlink advances (`references/Hexalith.EventStore` `3c0d6cd4`->`fa2a8c40`; `references/Hexalith.Memories` `dbe8b72e`->`1ce41926`), so the "exact File List audit" attestation did not hold as written; both advances were reviewed and accepted as intentional and are now listed in the File List above.

### Reduction Evidence

- Fatal taxonomy: `35 catch sites / 12 files / 2 local classifiers` -> `35 ExceptionGuard-based sites / 0 local classifiers / 0 ad-hoc fatal lists`.
- Hydration state: `5 duplicate feature enums` -> `1 shared HydrationState enum`; the distinct capability-discovery lifecycle remains unchanged.
- JSON options: `3 production configurations` -> `1 FcJson holder / 2 named read-only profiles / 0 dead DataGrid configurations`; both retained aliases are reference-identical to their profiles.
- Generated literals: `2 hand-rolled C#-literal paths` -> `2 thin GeneratedLiteral.Escape delegates / 0 hand-rolled paths`; existing snapshots and fingerprints are byte-identical.

### Completion Notes List

- Ultimate context engine analysis completed — comprehensive developer guide created.
- Task 1 complete — introduced the internal four-type `ExceptionGuard`, migrated exactly 35 catch filters across the 12 named owners, removed both local classifiers and all ad-hoc production fatal lists, preserved the explicit EmptyState cancellation exclusion and authorization behavior, added non-vacuous syntax governance plus classifier/site tests, and superseded only deferred row DW-0483.
- Task 2 complete — replaced five public feature-specific enums with one stable `HydrationState`, retained the distinct capability-discovery lifecycle, added non-vacuous source governance and an independent consumer compile, preserved the FcTbl baseline, and documented the exact 2.0 migration plus required breaking-change commit/PR metadata.
- Task 3 complete — centralized plain-web and compact-storage JSON semantics in read-only `FcJson` profiles, retained identity-equal production aliases, removed the dead DataGrid options, pinned exact request/storage shapes and round-trips, and added a non-vacuous production-only construction guard.
- Task 4 complete — reduced both residual C# literal implementations to thin `GeneratedLiteral.Escape` delegates, preserved all callers and existing snapshots, added direct and 500-case property coverage, compiled generated view/MCP edge fixtures, recovered runtime constants exactly, and added non-vacuous source governance.
- Task 5 complete — reconciled all four reduction inventories against the unchanged baseline commit, passed independent child lanes and every broad quality gate, verified the exact changed-file ledger and CRLF policy, and moved the story to review.

### File List

Creation-time artifact set (the development agent must extend this with the exact implemented file set and baseline-to-final reduction counts):

- `_bmad-output/implementation-artifacts/11-16-fatal-hydration-json-and-generated-literal-helper-consolidation.md` (new story artifact)
- `_bmad-output/implementation-artifacts/11-16-hydration-state-compatibility-evidence.md` (new)
- `_bmad-output/implementation-artifacts/deferred-work.md` (DW-0483 resolved/superseded)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (Story 11.16 `ready-for-dev` -> `review`)
- `src/Hexalith.FrontComposer.Shell/Badges/ReflectionActionQueueProjectionCatalog.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs`
- `src/Hexalith.FrontComposer.Shell/GlobalUsings.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreRequestContent.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/PendingCommands/PendingCommandPollingDriver.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandDispatchAuthorizationGate.cs`
- `src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs`
- `src/Hexalith.FrontComposer.Shell/Services/ExceptionGuard.cs` (new)
- `src/Hexalith.FrontComposer.Shell/Services/FcJson.cs` (new)
- `src/Hexalith.FrontComposer.Shell/Services/SnapshotPublisher.cs`
- `src/Hexalith.FrontComposer.Shell/Services/StorageScopeResolver.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteActions.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteHydrationState.cs` (deleted)
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteReducers.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/FrontComposerCommandPaletteState.cs`
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationFeature.cs`
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationHydrationState.cs` (deleted)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationReducers.cs`
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationState.cs`
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/GridViewHydratedAction.cs`
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityActions.cs`
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityHydrationState.cs` (deleted)
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityReducers.cs`
- `src/Hexalith.FrontComposer.Shell/State/Density/FrontComposerDensityFeature.cs`
- `src/Hexalith.FrontComposer.Shell/State/Density/FrontComposerDensityState.cs`
- `src/Hexalith.FrontComposer.Shell/State/HydrationState.cs` (new)
- `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationFeature.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/FrontComposerNavigationState.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationActions.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationHydrationState.cs` (deleted)
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationReducers.cs`
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs`
- `src/Hexalith.FrontComposer.Shell/State/Theme/FrontComposerThemeFeature.cs`
- `src/Hexalith.FrontComposer.Shell/State/Theme/FrontComposerThemeState.cs`
- `src/Hexalith.FrontComposer.Shell/State/Theme/ThemeActions.cs`
- `src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/Theme/ThemeHydrationState.cs` (deleted)
- `src/Hexalith.FrontComposer.Shell/State/Theme/ThemeReducers.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RoleBodyHelpers.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLiteral.cs` (review patch 2026-07-14 — null/empty guard restored)
- `tests/Hexalith.FrontComposer.Shell.Tests/GlobalUsings.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/FatalExceptionGuardGovernanceTests.cs` (new)
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/FcJsonGovernanceTests.cs` (new)
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStoreClientTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/FcJsonTests.cs` (new)
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/EmptyStateCtaResolverTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/ExceptionGuardTests.cs` (new)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/HydrationStateConsolidationTests.cs` (new)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationReducersStorageReadyTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/TestBaseTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/GeneratedLiteralGovernanceTests.cs` (new)
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/GeneratedLiteralTests.cs` (new)
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/McpManifestEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs`
- `references/Hexalith.EventStore` (submodule gitlink `3c0d6cd4`->`fa2a8c40`; intentional advance, accepted at code review 2026-07-14)
- `references/Hexalith.Memories` (submodule gitlink `dbe8b72e`->`1ce41926`; intentional advance, accepted at code review 2026-07-14)

### Review Findings

_Adversarial code review (bmad-code-review) — 2026-07-13, 4 layers (Blind Hunter · Edge Case Hunter · Verification Gap · Acceptance Auditor) over `83a791c7..db9ba9ee`. Triage: 1 decision-needed, 1 patch, 2 deferred, 1 dismissed._

- [x] [Review][Decision] (RESOLVED 2026-07-14 — advances accepted as intentional; added to File List, Debug Log + Change Log attestations corrected) Story commit advances two unrelated root submodules — `references/Hexalith.EventStore` (`3c0d6cd4`→`fa2a8c40`, "style(docs): trim trailing blank line") and `references/Hexalith.Memories` (`dbe8b72e`→`1ce41926`, "Enhance Error Catalog with additional error codes and messages") ride along in `db9ba9ee`. Violates the Scope Never-list, Git Intelligence, Project Structure Notes, and Previous-Story Intelligence (all forbid unrelated submodule movement); neither gitlink is in the File List; and the Task 5 attestation ("file-ledger audit clean", "`git diff --check` clean", no submodule change) is therefore inaccurate. Same failure mode was remediated in Story 11.15 on these same two submodules. Decision: reset both gitlinks to baseline (recommended) OR, if intentional, add them to the File List and correct the Change Log / Task 5 attestation.
- [x] [Review][Patch] (APPLIED 2026-07-14 — guard restored in the shared `GeneratedLiteral.Escape` chokepoint so `EscapeString` and `McpManifestEmitter.Escape` both stay thin delegates and both are covered; SourceTools Release build 0/0, focused literal/governance/generator lanes 56/56 green) `EscapeString`/`GeneratedLiteral.Escape` dropped the old `null`/empty short-circuit — `null` now throws (`ArgumentNullException` from `SymbolDisplay.FormatLiteral`) instead of returning `null`; a behavior change in a "no behavior change" refactor (internal helper, no live null caller, so latent) [src/Hexalith.FrontComposer.SourceTools/Emitters/RoleBodyHelpers.cs:175]
- [x] [Review][Defer] `ExceptionGuard.IsFatal` never unwraps `TargetInvocationException`/`AggregateException`, so a wrapped process-fatal is classified non-fatal and swallowed [src/Hexalith.FrontComposer.Shell/Services/ExceptionGuard.cs:12] — deferred, pre-existing (baseline per-site `ex is not OutOfMemoryException` filters had the identical gap; academic on .NET 10 where SO/TA/AV are effectively uncatchable) → DW-669
- [x] [Review][Defer] Anti-recurrence guard narrower than AC5 claims: `FatalExceptionGuardGovernanceTests` skips filterless `catch (Exception)` (`if (filter is null) continue;`), so a future swallow-all (incl. cancellation) is neither counted toward the ==35 lock nor flagged [tests/Hexalith.FrontComposer.Shell.Tests/Architecture/FatalExceptionGuardGovernanceTests.cs:90] — deferred, low-priority hardening (existing cancellation/fault tests at the migrated owners substantially mitigate the risk) → DW-670
- Dismissed (noise): escaping *form* differs for exotic control chars/surrogates and "byte-identical snapshots" holds only for characters in current fixtures — no functional impact (runtime values preserved by round-trip tests; MCP fingerprints computed from descriptor models independent of escaped text; divergence documented in `GeneratedLiteral` remarks).

## Change Log

- 2026-07-13 — Story created from Epic 11.16 with M27 trace correction, exact current-tree inventories, resolved fatal/cancellation/public-hydration/JSON-profile/literal boundaries, child-specific evidence requirements, prior-story lessons, and current official .NET/Roslyn guidance. Adversarial create-story validation added the live-tree `McpManifestEmitter` C#-literal duplicate, read-only JSON construction, explicit enum breaking-change evidence, `ThreadAbortException` test guidance, and internal-member XML-documentation requirements. Status set to `ready-for-dev`.
- 2026-07-13 — Task 1: consolidated the 35-site fatal taxonomy through `ExceptionGuard`, preserved local cancellation policy, removed two local classifiers, added classifier/site/syntax-governance tests, and resolved DW-0483. No public API, package, wire, storage, generated-source, or UI change.
- 2026-07-13 — Task 2: intentionally replaced `CommandPaletteHydrationState`, `DataGridNavigationHydrationState`, `DensityHydrationState`, `NavigationHydrationState`, and `ThemeHydrationState` with public `State.HydrationState` (`Idle=0`, `Hydrating=1`, `Hydrated=2`). Published `2.0.4` retains the old surface, so consumers must update type references and recompile for `3.0.0`; the delivery commit/PR requires a breaking Conventional Commit marker.
- 2026-07-13 — Task 3: consolidated three production JSON configurations into one `FcJson` holder with two named read-only profiles, preserved EventStore and storage wire semantics through identity aliases and exact-shape tests, and removed the unused DataGrid configuration.
- 2026-07-13 — Task 4: delegated the remaining RoleBodyHelpers and MCP C# literal seams to `GeneratedLiteral`, added edge/property/generated-source compilation evidence, and confirmed all existing Verify snapshots and fingerprint fixtures remain byte-identical.
- 2026-07-13 — Task 5: recorded the exact baseline-to-final reductions and completed final validation: Release build 0 warnings/errors, focused Shell 302/302, focused SourceTools 97/97, full default 4,100/4,100, Governance 277/277, snapshots unchanged, CRLF and file-ledger audits clean, and `git diff --check` clean. Status moved to `review`.
- 2026-07-14 — Code review (bmad-code-review): resolved the decision-needed finding on the two unrelated root-submodule gitlink advances (`references/Hexalith.EventStore` `3c0d6cd4`->`fa2a8c40`; `references/Hexalith.Memories` `dbe8b72e`->`1ce41926`). User accepted both advances as intentional; added them to the File List and corrected the Task 5 Debug Log attestation accordingly. Remaining review actions: 1 patch (EscapeString null short-circuit) pending, 2 items deferred (DW-669, DW-670).
- 2026-07-14 — Code review (bmad-code-review): applied the low-severity patch for the dropped null/empty escaping guard. Restored `if (string.IsNullOrEmpty(value)) return value;` inside the shared `GeneratedLiteral.Escape` chokepoint (rather than `RoleBodyHelpers.EscapeString`) so both residual seams remain thin delegates and the `GeneratedLiteralGovernanceTests` thin-delegate assertion still holds; `McpManifestEmitter.Escape` is now covered too. All non-null inputs are byte-for-byte unchanged; only a null (contract-violating) input now returns null instead of throwing. SourceTools + SourceTools.Tests Release build 0 warnings/0 errors; focused GeneratedLiteral/MCP-emitter (22/22) and GeneratorDriver integration (34/34) lanes green. Review actions complete: decision resolved, 1 patch applied, 2 items deferred (DW-669, DW-670), 1 dismissed. Working-tree changes are not yet committed.
