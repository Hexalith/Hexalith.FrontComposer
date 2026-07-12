---
title: 'Story 11.9: Shell layering declaration and route/label relocation'
type: 'refactor'
created: '2026-07-12T20:00:00+02:00'
status: 'done'
baseline_revision: 'f4fdfa0074046c95bd74dceaf2a31b1a242960b3'
final_revision: '1946da3f3e3b1531868aff4b80e5b9984d947cbf'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '_bmad-output/project-context.md'
  - '_bmad-output/project-docs/architecture-quality-review-2026-07-04.md'
  - '_bmad-output/implementation-artifacts/spec-11-7-command-projection-route-contract-implementation.md'
warnings: [oversized]
---

<intent-contract>

## Intent

**Problem:** Shell runtime orchestration is filed under `State`, and state effects derive projection routes through public statics on the `FrontComposerNavigation` Razor component. The real dependency direction is therefore obscured and an internal render-layer dependency can recur unnoticed.

**Approach:** Place concrete polling/scheduling implementations in `Infrastructure`, centralize projection route/label derivation in `Routing`, document the actual Shell sublayers and cross-cutting telemetry exception, and pin those boundaries with a source architecture test.

## Boundaries & Constraints

**Always:** Preserve scoped DI lifetimes, Story 11.2 disposal/reconnect behavior, projection route bytes (`/{bounded-context-lowercase}/{simple-type-kebab}`), simple-name labels with original casing, and Story 11.7 command routes. Keep `IProjectionFallbackRefreshScheduler`, its lane/result models, `IPendingCommandPollingCoordinator`, state services, and Fluxor state in `State`; only concrete orchestration implementations move. Treat `Infrastructure.Telemetry` as cross-cutting and document every retained non-telemetry exception rather than pretending the graph is acyclic.

**Block If:** The relocation cannot retain the existing generated-view contract or DI behavior without changing SourceTools output, or package validation requires an unplanned public compatibility decision beyond updating the owned Shell suppression baseline.

**Never:** Move telemetry physically, change command/projection URL contracts, alter polling timing/budgets/lifetimes, edit generated output, broaden into Rx/observer convergence, storage/snapshot consolidation (11.15), helper consolidation (11.16), one-type-per-file cleanup, or any `references/Hexalith.*` submodule.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Namespaced projection | `Counter.Domain.Projections.CounterView`, context `Counter` | route `/counter/counter-view`, label `CounterView` | No error expected |
| Acronym projection | `Reporting.Projections.XMLReportView` / `SKUList` | `/reporting/xml-report-view` / `/commerce/sku-list` | Preserve canonical acronym runs |
| Invalid route input | blank context or projection | same argument exception behavior as today | Never emit a partial route |
| Worker resolution | quickstart and EventStore registrations | relocated implementations resolve scoped and start/dispose once | Existing bounded-disposal behavior remains |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor{,.cs}` -- current render host for projection route/label logic.
- `src/Hexalith.FrontComposer.Shell/Routing/CommandRouteBuilder.cs` -- canonical kebab helper retained from Story 11.7; new projection helper belongs beside it.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingDriver.cs` and `State/ProjectionConnection/{ProjectionFallbackPollingDriver,ProjectionFallbackRefreshScheduler}.cs` -- three misplaced concrete orchestration implementations; the latter file also retains State-owned contracts and models.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` and `Extensions/{ServiceCollectionExtensions,EventStoreServiceExtensions}.cs` -- worker consumers and scoped DI registrations.
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/` -- existing source-governance style and destination for the layer guard.
- `_bmad-output/{planning-artifacts,project-docs}/architecture.md` -- planning and detailed runtime architecture descriptions.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.FrontComposer.Shell/Routing/ProjectionRouteBuilder.cs`, `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor`, `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.cs`, `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor.cs`, `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeCard.razor`, and `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs` -- move projection `BuildRoute`/`ProjectionLabel` logic to a pure Routing helper and update every production consumer; keep the two existing public component methods as behavior-identical delegating compatibility facades so the refactor does not remove the 2.x API.
- `tests/Hexalith.FrontComposer.Shell.Tests/Routing/ProjectionRouteBuilderTests.cs` and `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerNavigationTests.cs` -- move pure route/label cases to Routing tests while retaining rendered navigation and compatibility-facade assertions; cover the matrix including acronym and invalid inputs.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/PendingCommands/PendingCommandPollingDriver.cs`, `src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs`, `src/Hexalith.FrontComposer.Shell/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs`, `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs`, `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`, `src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`, `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`, and `src/Hexalith.FrontComposer.Shell/CompatibilitySuppressions.xml` -- relocate the three concrete implementations while leaving scheduler interfaces/models and pending mutation coordination in State; update namespaces, DI, consumers, and owned package suppressions.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/PendingCommands/PendingCommandPollingDriverTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriverTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/ProjectionConnection/ProjectionFallbackRefreshSchedulerTests.cs`, and the existing `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingCoordinatorTests.cs` plus `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs` -- align implementation-test namespaces and preserve coordinator/state, disposal, reconnect, nudge, and reconciliation behavior.
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellLayeringTests.cs` -- source-scan the Shell to require namespace/folder agreement, forbid `State -> Components`, keep Routing independent of Components/State/Services/Infrastructure, locate the three concrete workers under Infrastructure, and whitelist only documented cross-cutting/legacy edges; include a synthetic forbidden-edge case proving the guard can fail.
- `_bmad-output/planning-artifacts/architecture.md` and `_bmad-output/project-docs/architecture.md` -- declare Components as render composition, Routing as pure derivation, State as Fluxor/state contracts and mutation, Infrastructure as external adapters/background orchestration, Telemetry as cross-cutting, and list retained exceptions with the enforced direction.

**Acceptance Criteria:**
- Given projection navigation from the sidebar, home, or palette, when routes and labels are derived, then every consumer calls the Routing helper and observable URLs/labels remain byte-for-byte unchanged.
- Given command palette state effects, when architecture scanning runs, then no State source references a Razor component; Story 11.7 command activation still uses `CommandRouteBuilder` and its canonical command family.
- Given quickstart or EventStore composition, when a scope resolves and disposes polling services, then the three concrete implementations come from Infrastructure with unchanged scoped lifecycle, bounded disposal, reconnect, fallback, nudge, and reconciliation behavior.
- Given the documented Shell layer matrix, when a forbidden `State -> Components` or Routing-outward dependency is introduced in synthetic input or production source, then the architecture test fails with the offending path and edge.

## Spec Change Log

## Review Triage Log

### 2026-07-12 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 9: (high 3, medium 4, low 2)
- defer: 2: (high 0, medium 1, low 1)
- reject: 3: (high 0, medium 1, low 2)
- addressed_findings:
  - `[medium]` `[patch]` Restored exact whitespace, trailing-separator, exception-parameter, and compatibility-facade behavior in the relocated projection helper.
  - `[low]` `[patch]` Made `ProjectionRouteBuilder` internal so the ownership move does not add an unnecessary package API.
  - `[high]` `[patch]` Replaced the lexical architecture scan with Roslyn syntax/semantic analysis covering fully qualified, aliased, global/static, and trivia-spoofed dependencies.
  - `[medium]` `[patch]` Added Razor placement enforcement so render files cannot bypass State/Routing layer ownership.
  - `[medium]` `[patch]` Aligned documentation and enforcement for boundary-safe Telemetry plus the exact two-type `LoadPageEffects` EventStore exception.
  - `[medium]` `[patch]` Required unique concrete worker declarations at the Infrastructure paths and absence of all old State paths/declarations.
  - `[high]` `[patch]` Added composed production-DI tests for scoped registration, exact injection, cross-scope isolation, and deterministic teardown of the relocated workers.
  - `[high]` `[patch]` Reclassified the three public worker relocations as exact `v3.0` moves and tightened Shell CP0001 evidence to parsed type/destination/assembly parity.
  - `[low]` `[patch]` Removed the untracked scheduler file's extra blank line and verified tracked plus untracked whitespace.

### 2026-07-12 — Follow-up review pass
- intent_gap: 0
- bad_spec: 0
- patch: 1: (high 0, medium 1, low 0)
- defer: 0
- reject: 11: (high 0, medium 2, low 9)
- addressed_findings:
  - `[medium]` `[patch]` Corrected the layering guard and both architecture docs: `IProjectionPageLoader` is a `State.DataGridNavigation` type, not an `Infrastructure.EventStore` seam, so the guard's `PageLoaderType` whitelist entry was dead and the docs misstated the story's headline retained-exception. Removed the dead constant/whitelist entry and re-documented the sole retained non-telemetry State→Infrastructure edge as `ProjectionSchemaMismatchException` reached via the `Infrastructure.EventStore` namespace import. Guard re-run green (24/24 layering + routing tests).
- rejected/deferred rationale:
  - The three concrete workers staying `public` (forcing the `v3.0` CP0001 moves), the guard enforcing only the intent's enumerated edges rather than the full layer matrix, the retained public `FrontComposerNavigation` route/label facades, and the `v3.0`-vs-shipped-version suppression binding were all rejected as out of scope on the intent's own authority (the intent authorizes a *move*, gates public-compat decisions via Block-If, and mandates byte-for-byte preservation) or as low-consequence hardening of an already-sound new guard.
  - Route-input hardening (protocol-relative / malformed-segment / open-redirect edge inputs) and the one-type-per-file split are pre-existing and already tracked in `deferred-work.md` from the prior pass; no new ledger entry was appended.

## Design Notes

The three concrete moves are intentionally narrow: `PendingCommandPollingCoordinator` remains State because it owns pending-state mutation; `ProjectionConnectionStateService` remains State because it publishes state snapshots; scheduler interfaces and lane/result models remain State so generated views keep their existing contract. Existing `LoadPageEffects -> Infrastructure.EventStore` and cross-cutting telemetry imports must be named exceptions in the documented/tested matrix, not silently generalized permissions.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --filter "FullyQualifiedName~ProjectionRouteBuilderTests|FullyQualifiedName~ShellLayeringTests|FullyQualifiedName~ProjectionFallbackPollingDriverTests|FullyQualifiedName~ProjectionFallbackRefreshSchedulerTests|FullyQualifiedName~PendingCommandPollingDriverTests|FullyQualifiedName~PendingCommandPollingCoordinatorTests|FullyQualifiedName~ProjectionSubscriptionServiceTests"` -- focused routing, layering, DI/lifecycle behavior passes.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- Shell default lane passes.
- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false && pwsh ./eng/validate-docs.ps1 && git diff --check` -- Release/package/doc/format gates pass with no warnings.

## Auto Run Result

Status: done

Summary: Declared and enforced the Shell's real internal layering, moved concrete pending-command and projection polling/scheduling workers from State to Infrastructure, and centralized projection route/label derivation in an internal Routing helper while preserving the public `FrontComposerNavigation` facades and every existing observable route/label edge case.

Files changed:
- `_bmad-output/planning-artifacts/architecture.md` and `_bmad-output/project-docs/architecture.md` -- document Components, Routing, State, Infrastructure, cross-cutting Telemetry, and the exact retained EventStore seam.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- records pre-existing route-hardening and one-type-per-file follow-ups outside Story 11.9.
- `docs/diagnostics/compatibility-suppressions.json`, `src/Hexalith.FrontComposer.Shell/CompatibilitySuppressions.xml`, and `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs` -- govern the three exact public worker relocations as parsed `v3.0` CP0001 moves.
- `src/Hexalith.FrontComposer.Shell/Routing/ProjectionRouteBuilder.cs` -- owns internal, baseline-compatible projection route and label derivation.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor{,.cs}`, `Components/Home/FcHomeDirectory.razor.cs`, `Components/Home/FcHomeCard.razor`, and `State/CommandPalette/CommandPaletteEffects.cs` -- consume Routing directly while retaining component compatibility facades.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/PendingCommands/PendingCommandPollingDriver.cs` and `Infrastructure/ProjectionConnection/{ProjectionFallbackPollingDriver,ProjectionFallbackRefreshScheduler}.cs` -- relocate the three concrete scoped workers without changing algorithms.
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshContracts.cs` -- retains generated-view scheduler interfaces and lane/result models in State; the three old State worker files are removed.
- `src/Hexalith.FrontComposer.Shell/Extensions/{ServiceCollectionExtensions,EventStoreServiceExtensions}.cs` and `Infrastructure/EventStore/ProjectionSubscriptionService.cs` -- register and inject the relocated scoped implementations.
- `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj` and `Architecture/ShellLayeringTests.cs` -- add centrally versioned Roslyn analysis and enforce semantic dependency direction, Razor placement, exact exceptions, namespace/folder agreement, and unique worker ownership.
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/RelocatedInfrastructureRegistrationTests.cs` -- proves scoped production registration, injection, isolation, and teardown.
- `tests/Hexalith.FrontComposer.Shell.Tests/Routing/ProjectionRouteBuilderTests.cs` and `Components/Layout/FrontComposerNavigationTests.cs` -- pin exact route/label behavior and delegating facade compatibility.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/{PendingCommands,ProjectionConnection}/` -- move the three worker behavior suites with their implementations; the old State test files are removed.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs` and its two fault-injection integration suites -- consume relocated workers and retain reconnect/nudge coverage.
- `_bmad-output/implementation-artifacts/spec-11-9-shell-layering-declaration-and-route-label-relocation.md` -- records the intent, implementation, verification, review triage, and final result.

Review findings breakdown: 9 patches applied (3 high, 4 medium, 2 low), 2 pre-existing items deferred, and 3 findings rejected as conflicting hardening or overbroad layer-model scope. Follow-up review recommendation: true, because review replaced the architecture guard with semantic analysis, added production DI lifecycle evidence, and corrected next-major compatibility governance.

Verification:
- Focused routing/layering/worker/DI matrix passed 82/82; compatibility suppression evidence passed 1/1.
- Shell default lane passed 2251/2251 with no skips.
- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings and 0 errors.
- Shell package ApiCompat passed at `Version=3.0.0`; docs validation passed and emitted `artifacts/docs/validation-manifest.json`.
- Tracked `git diff --check` and per-untracked `git diff --no-index --check` passed.

Residual risks:
- The required pre-change Aspire baseline could not launch because the existing Debug UI graph resolves duplicate project/package copies of `Hexalith.FrontComposer.Shell` (`CS1704`); no resources were started. The Release solution build is green.
- The public worker namespace moves require the next major release and are recorded as exact `v3.0` compatibility moves.
- Protocol-relative projection-route hardening and mechanical one-type-per-file splitting remain deferred to dedicated stories.

### Follow-up review pass (2026-07-12)

An independent four-layer follow-up review (adversarial, edge-case, verification-gap, intent-alignment) ran over the committed `f4fdfa00..HEAD` diff. Outcome: one patch, eleven rejects, no intent gaps, no bad-spec loopbacks, no new deferrals.

- Patch applied — the layering guard declared `PageLoaderType = Infrastructure.EventStore.IProjectionPageLoader`, but that interface actually lives in `State.DataGridNavigation`; `LoadPageEffects`'s only real State→Infrastructure seam is `ProjectionSchemaMismatchException` (referenced via the `Infrastructure.EventStore` namespace import). The dead whitelist constant/entry was removed and both `architecture.md` copies were corrected so the story's documented retained-exception matches the code. Files touched: `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellLayeringTests.cs`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/project-docs/architecture.md`.
- Verification — `dotnet build` of the Shell test project passed with 0 warnings / 0 errors; the `ShellLayeringTests` + `ProjectionRouteBuilderTests` classes passed 24/24 via the direct xUnit v3 runner (`DiffEngine_Disabled=true`). No production `src/**` code changed, so behavior is unaffected.
- Rejected findings concerned public-API/versioning choices the intent authorizes as a move or gates through Block-If (workers kept public, retained facades, `v3.0` suppression binding), guard-scope beyond the intent's enumerated edges, and low-consequence robustness nits on the new guard. Route-input hardening and the one-type-per-file split were already tracked in `deferred-work.md`.
- `followup_review_recommended` set to `false`: this pass made a single localized, low-risk accuracy fix to a test guard and documentation, with no behavioral, API, or data impact.

Residual artifacts (left uncommitted, not part of this change): `_bmad-output/implementation-artifacts/sprint-status.yaml` (orchestrator-owned, modified before this run started).
