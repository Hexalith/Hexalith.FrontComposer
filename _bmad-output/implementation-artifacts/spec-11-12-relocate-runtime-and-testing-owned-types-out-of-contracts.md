---
title: 'Story 11.12: Relocate runtime and testing-owned types out of Contracts'
type: 'refactor'
created: '2026-07-11T00:00:00+02:00'
status: 'done'
baseline_revision: 'c5d39c43012e4c349b06e9accc9bf8418e85c18d'
final_revision: '3c71ebbb213a52731184bf007de0aa6c36871b3d'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '_bmad-output/project-context.md'
  - '_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md'
  - '_bmad-output/project-docs/architecture-quality-review-2026-07-04.md'
warnings: [oversized]
---

<intent-contract>

## Intent

**Problem:** `Hexalith.FrontComposer.Contracts` exports a test fake, a mutable circuit service, Shell configuration, and 20 in-process Fluxor actions. These implementation types pollute the kernel, rev the Contracts package for Shell-only changes, and include live `TaskCompletionSource` state that is not a wire contract.

**Approach:** Move the 25 approved M24 types to Testing or Shell with ownership-correct namespaces, update generated source and consumers to the new identities, and add durable assembly/package evidence that Contracts no longer exports them while SourceTools remains kernel-only.

## Boundaries & Constraints

**Always:** Move `InMemoryStorageService` to Testing; move `FcShellOptions`, `FcShellDevModeOptions`, `CustomizationContractValidationMode`, and `InlinePopoverRegistry` to Shell; move the 18 DataGrid/filter/virtualization actions and two expanded-row actions beside their Shell reducers/effects. Keep one C# type per file. Preserve constructors, validation, cancellation, reducer/effect behavior, DI lifetimes, and generated behavior. Keep SourceTools targeting netstandard2.0 and referencing only Contracts; Shell identities may appear only as emitted strings. Treat removal from the already released v1 Contracts assembly as an intentional breaking package move and record exact old/new identities in the story result and package evidence.

**Block If:** A moved type is consumed without a Shell/Testing dependency and cannot migrate without reversing the dependency graph; package-only generated code cannot compile without SourceTools referencing Shell; or completion requires editing a `references/Hexalith.*` submodule. Record adopter fallout instead of changing a submodule.

**Never:** Create `Contracts.UI`, change Contracts TFMs, move Blazor/Fluent rendering contracts, move `GridViewSnapshot` or `LifecycleOptions`, decompose `QueryRequest`, duplicate old/new action or options identities, add upward type-forwarding, change wire/schema/MCP/CLI shapes, or claim binary compatibility. Story 11.14 owns final migration/release documentation.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Testing storage | Test host resolves `IStorageService` | `Hexalith.FrontComposer.Testing.InMemoryStorageService` preserves null, prefix, remove, flush, and cancellation-compatible behavior | No Contracts implementation fallback |
| Generated grid | SourceTools emits virtualization, filtering, scrolling, and expanded-row dispatches | Generated code references Shell action identities and compiles in a Shell consumer | Analyzer stays loadable without a Shell dependency |
| Inline popovers | Two scoped popovers open in sequence | Shell registry closes the previous popover, preserves cancellation, stale release, and best-effort non-cancellation failure behavior | New popover still opens after a stale close failure |
| Shell configuration | Adopter binds/configures `FcShellOptions` | Existing defaults, annotations, cross-property validation, and generated renderer consumption remain unchanged | Invalid values fail through existing validation |
| Kernel inspection | Contracts netstandard2.0 assembly/package is inspected | Named implementations/actions and public `TaskCompletionSource<>` signatures are absent; approved seams remain | Governance test fails on ownership regression |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Contracts/{FcShellOptions.cs,Storage/InMemoryStorageService.cs,Rendering/*Actions.cs,Rendering/InlinePopoverRegistry.cs}` -- misplaced source to remove/split; retain `IStorageService`, `IInlinePopover`, and `GridViewSnapshot`.
- `src/Hexalith.FrontComposer.Shell/{Options,Services,State/DataGridNavigation,State/ExpandedRow}` -- owning destinations for options, runtime registry, and Fluxor actions.
- `src/Hexalith.FrontComposer.Testing` and `PublicAPI.Shipped.txt` -- owning fake implementation and intentional adopter-facing API baseline.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/{RazorEmitter,CommandPageEmitter,CommandRendererEmitter}.cs` -- emitted old CLR identities.
- `tests/Hexalith.FrontComposer.{Contracts,Shell,SourceTools,Testing}.Tests` -- ownership, behavior, generated-output, package, and API evidence.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.FrontComposer.Contracts/Rendering/DataGridNavigationActions.cs` and `src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj` -- leave `GridViewSnapshot` as its own kernel file, remove all named implementation/action exports, and remove `System.ComponentModel.Annotations` only after proving no Contracts source needs it.
- `src/Hexalith.FrontComposer.Shell/Options/{FcShellOptions,FcShellDevModeOptions,CustomizationContractValidationMode}.cs` and Shell/sample call sites -- relocate the three option types without default or validation drift.
- `src/Hexalith.FrontComposer.Shell/Services/InlinePopoverRegistry.cs` and `Extensions/ServiceCollectionExtensions.cs` -- relocate the scoped runtime implementation while keeping `IInlinePopover` inward.
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/*.cs` and `State/ExpandedRow/*.cs` -- create one file per 20 action types and update reducers, effects, components, XML references, and imports to the Shell namespaces.
- `src/Hexalith.FrontComposer.Testing/InMemoryStorageService.cs`, `FrontComposerTestHostBuilder.cs`, and `PublicAPI.Shipped.txt` -- relocate the fake, preserve test-host registration, and baseline its complete public surface.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/{RazorEmitter,CommandPageEmitter,CommandRendererEmitter}.cs` -- emit new Shell-qualified options, registry, and action identities without adding a Shell project/package reference to SourceTools.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Architecture/ContractsKernelOwnershipTests.cs` and `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj` -- pin the Contracts export/reference boundary and reference Testing only from the leaf Shell test project.
- `tests/Hexalith.FrontComposer.Contracts.Tests/{FcShellOptionsVirtualizationTests.cs,InMemoryStorageServiceTests.cs,Rendering/ExpandedRowActionsTests.cs,Rendering/FilterActionsTests.cs,Rendering/VirtualizationActionsTests.cs}` -- move storage coverage to `tests/Hexalith.FrontComposer.Testing.Tests/InMemoryStorageServiceTests.cs` and option/action coverage to matching `tests/Hexalith.FrontComposer.Shell.Tests/{Options,State}` paths; update all Shell test imports to the owning namespaces.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/*`, affected `.verified.txt` files, and `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs` -- update assertions/snapshots and compile a package-style generated grid/form against new Shell identities while inspecting the analyzer payload for Contracts-only dependencies.
- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- make the API enumeration cover the moved fake and extend the clean packed consumer to instantiate it; preserve package-only host behavior.

**Acceptance Criteria:**
- Given the built Contracts assembly for netstandard2.0 and net10.0, when its references and exported signatures are inspected, then none of the 25 moved types or public `TaskCompletionSource<>` signatures remain and `IStorageService`, `IInlinePopover`, and `GridViewSnapshot` remain available.
- Given Shell startup and generated command/projection output, when options, inline popovers, filtering, paging, scrolling, or expanded rows execute, then the same behavior and scoped lifetimes use Shell-owned identities and no duplicate Fluxor identity exists.
- Given the Testing package and a clean package-only consumer, when the test host resolves storage and exercises the fake, then the implementation comes from Testing and its public API baseline and behavior tests pass.
- Given SourceTools is packed/loaded as an analyzer and a consumer compiles generated UI, when emitted code references the moved types, then it compiles through the consumer's Shell dependency while SourceTools itself references only Contracts.
- Given current stable v1 package history, when compatibility evidence is reviewed, then exact old/new assembly-qualified identities and unavoidable binary breaks are explicit; no silent forwarding, duplicate shims, submodule edits, or unrelated 11.11/11.13/11.14 work is present.

## Spec Change Log

## Review Triage Log

### 2026-07-11 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 5: (high 0, medium 2, low 3)
- defer: 1: (high 0, medium 1, low 0)
- reject: 9: (high 0, medium 3, low 6)
- addressed_findings:
  - `[low]` `[patch]` Replaced stale Contracts-qualified action crefs in DataGrid and expanded-row production XML documentation with the relocated Shell identities.
  - `[medium]` `[patch]` Strengthened the packaged-analyzer consumer to assert that generation ran and emitted the Shell-owned options and paging-action identities.
  - `[medium]` `[patch]` Added Quickstart DI coverage proving `InlinePopoverRegistry` is scoped per circuit and non-scoped pre-registrations fail closed.
  - `[low]` `[patch]` Aligned the new breaking-change package fixture with a `2.0.0-review.*` test version instead of the historical pre-v1 value.
  - `[low]` `[patch]` Removed the obsolete claim that popover telemetry remained deferred to a future Shell wrapper after the implementation moved into Shell.

### 2026-07-11 — Review pass (follow-up)
- intent_gap: 0
- bad_spec: 0
- patch: 1: (high 0, medium 0, low 1)
- defer: 0
- reject: 9: (high 0, medium 0, low 9)
- addressed_findings:
  - `[low]` `[patch]` Removed the unused `Hexalith.FrontComposer.Contracts.Rendering` using directive (11 relocated action files) and the unused `System.Collections.Immutable` using directive (7 of those files) left behind by the one-type-per-file split of the DataGrid actions. Verified with a clean `-c Release` build of `Hexalith.FrontComposer.Shell` (0 warnings / 0 errors under `TreatWarningsAsErrors=true`), which also proves the removed usings were genuinely dead. Rejected findings this pass (all low, all confirmed non-defects by inspection): the netstandard2.0 `ContractsKernelOwnershipTests` reads an out-of-band-built DLL, but functions correctly in the sanctioned CI/verification flow (Gate 2 builds the full Release solution before tests), fails loudly with an actionable message otherwise, and cannot false-pass on a stale pre-move DLL (that DLL still exports the old types and fails `ShouldNotContain`) — belt-and-suspenders alongside the richer net10 live-reflection test; the `dotnet pack --no-build` switch requires a prior Release build but fails loudly and holds in CI; the packaged-consumer test's network/temp-dir usage matches the established package-boundary pattern; the popover close-failure observability gap is already recorded in the deferred-work ledger (prior pass) and was left untouched; the `OpenAsync_PreviousCloseFails_NewPopoverStillOpens` assertion transitively proves the guarded regression; the parallel identity-string lists, blanket Shell `GlobalUsings`, absence of a compat shim (an intentional breaking move per intent; Story 11.14 owns migration docs), and mixed test qualification style are all design/observation nits, not defects.

## Design Notes

Assembly-forwarding from Contracts to Shell/Testing would reverse the dependency graph; duplicate compatibility types would also break Fluxor and options DI through distinct runtime identities. Clean namespace and assembly moves are therefore intentional. `GridViewSnapshot` remains the inward data seam consumed by Shell actions; other rendering-contract migration remains Story 11.11.

## Verification

**Commands:**
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false` -- restore succeeds.
- `dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -f netstandard2.0 -c Release --no-restore -m:1 /nr:false` -- kernel builds cleanly.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj -c Release --no-restore` -- kernel ownership and retained contracts pass.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release --no-restore` -- fake behavior, API baseline, and package consumer pass.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --no-restore` -- emitted identities, snapshots, and generator compilation pass.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- Shell runtime behavior passes.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-restore --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- repository default lane passes.
- `dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 /nr:false && git diff --check` -- full build and whitespace gate pass.

## Auto Run Result

Status: done

Summary: Relocated all 25 approved runtime/testing-owned public types out of Contracts. The in-memory storage fake now ships from Testing; Shell owns its options, inline-popover registry, 18 DataGrid actions, and two expanded-row actions; Contracts retains only `IStorageService`, `IInlinePopover`, and `GridViewSnapshot`. SourceTools still references only Contracts while emitting the new Shell-qualified identities. These are intentional binary-breaking moves from stable v1 identities with no duplicate shims or upward forwarding.

Files changed:
- `_bmad-output/implementation-artifacts/spec-11-12-relocate-runtime-and-testing-owned-types-out-of-contracts.md` -- records the intent, implementation boundary, review triage, verification, and result.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- records the remaining sanitized-observability follow-up for suppressed stale popover-close failures.
- `samples/Counter/Counter.Web/Program.cs` -- imports the Shell-owned options namespace.
- `src/Hexalith.FrontComposer.Contracts/{FcShellOptions.cs,Storage/InMemoryStorageService.cs,Rendering/DataGridNavigationActions.cs,Rendering/ExpandedRowActions.cs,Rendering/FilterActions.cs,Rendering/InlinePopoverRegistry.cs,Rendering/VirtualizationActions.cs}` -- removes the 25 misplaced types from Contracts.
- `src/Hexalith.FrontComposer.Contracts/Rendering/GridViewSnapshot.cs` -- retains the approved inward data seam in its own file.
- `src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj` -- removes the now-unused annotations dependency from the kernel target.
- `src/Hexalith.FrontComposer.Shell/GlobalUsings.cs` -- centralizes Shell-owned option/action namespaces for existing consumers.
- `src/Hexalith.FrontComposer.Shell/Options/{FcShellOptions,FcShellDevModeOptions,CustomizationContractValidationMode}.cs` -- relocates and splits Shell configuration without default or validation drift.
- `src/Hexalith.FrontComposer.Shell/Services/InlinePopoverRegistry.cs` and `Extensions/ServiceCollectionExtensions.cs` -- relocate and register the per-circuit scoped runtime service.
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/{CaptureGridStateAction,RestoreGridStateAction,ClearGridStateAction,PruneExpiredAction,ColumnFilterChangedAction,StatusFilterToggledAction,GlobalSearchChangedAction,SortChangedAction,FiltersResetAction,LoadPageAction,LoadPageSucceededAction,LoadPageNotModifiedAction,LoadPageFailedAction,LoadPageCancelledAction,ClearPendingPagesAction,ColumnVisibilityChangedAction,ResetColumnVisibilityAction,ScrollCapturedAction}.cs` -- moves each DataGrid Fluxor action beside its reducers/effects.
- `src/Hexalith.FrontComposer.Shell/State/ExpandedRow/{ExpandRowAction,CollapseRowAction}.cs` -- moves expanded-row actions beside their reducers.
- `src/Hexalith.FrontComposer.Shell/State/{DataGridNavigation,ExpandedRow}/*.cs` -- updates production XML references to the new action identities.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/{RazorEmitter,CommandPageEmitter,CommandRendererEmitter}.cs` -- emits Shell-qualified action, options, and registry identities without adding a Shell reference.
- `src/Hexalith.FrontComposer.Testing/InMemoryStorageService.cs` and `PublicAPI.Shipped.txt` -- relocates the fake and pins its complete Testing public surface.
- `tests/Hexalith.FrontComposer.Contracts.Tests/Architecture/ContractsKernelOwnershipTests.cs` -- pins both Contracts TFMs, all exact old/new identities, dependency references, and public `TaskCompletionSource<>` absence.
- `tests/Hexalith.FrontComposer.Contracts.Tests/{FcShellOptionsVirtualizationTests.cs,InMemoryStorageServiceTests.cs,Rendering/*ActionsTests.cs}` -- removes tests from their former owner after rehoming.
- `tests/Hexalith.FrontComposer.Shell.Tests/{GlobalUsings.cs,Hexalith.FrontComposer.Shell.Tests.csproj}` -- imports owner namespaces and references Testing only from the leaf test project.
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellOwnershipIdentityTests.cs` -- pins all 24 Shell assembly-qualified identities.
- `tests/Hexalith.FrontComposer.Shell.Tests/{Options/FcShellOptionsVirtualizationTests.cs,Services/InlinePopoverRegistryTests.cs,State/DataGridNavigation/*ActionsTests.cs,State/ExpandedRow/ExpandedRowActionsTests.cs}` -- rehomes behavior tests and pins Quickstart scoped registration/fail-closed lifetime guards.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/{DataGridNavigation/LoadPageNotModifiedReducerTests.cs,Theme/ThemeEffectsScopeTests.cs,Theme/ThemeEffectsTests.cs}` -- updates explicit option identities.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs` -- proves the package payload stays Contracts-only and that the packaged generator actually emits Shell-qualified source in a clean consumer.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterItemsProviderTests.cs` and affected command/projection `.verified.txt` snapshots -- update generated-identity assertions intentionally.
- `tests/Hexalith.FrontComposer.Testing.Tests/{InMemoryStorageServiceTests.cs,PackageBoundaryTests.cs}` -- rehomes fake behavior and exercises it through a clean packed consumer without parallel output races.

Review findings breakdown: 5 patches applied (2 medium, 3 low), 1 medium pre-existing observability item deferred, and 9 findings rejected as duplicated, out of the separate Story 11.14 migration/release-doc boundary, or contradicted by the actual outer-build/package-test execution model. Follow-up review recommendation: true because review strengthened packaged generator execution evidence and production scoped-lifetime verification.

Verification:
- Restore passed with `NuGetAudit=false`; Contracts netstandard2.0 Release build passed with 0 warnings/errors.
- Contracts passed 205/205; Testing passed 57/57; the full SourceTools run passed 1064/1064 before review, and the review-strengthened packaged-analyzer test passed 1/1.
- Shell passed 2213/2213 after review; matrix-focused classes passed Contracts 2/2, Testing 11/11, packaged analyzer 1/1, and Shell registry/options 36/36 before the three added lifetime cases, with no skips.
- Final filtered solution lane passed Contracts 205, CLI 67, MCP 372, SourceTools 1058, Testing 57, and Shell 2213 with 0 failures.
- Final Release solution build passed with 0 warnings/errors; `git diff --check` reported no whitespace errors.

Residual risks and artifacts:
- External adopters using the old stable-v1 Contracts identities must migrate to the new Shell/Testing namespaces and packages; Story 11.14 still owns final migration/release documentation and package-compatibility guidance.
- No `references/Hexalith.*` adopter was edited. Known Tenants source uses of the old `FcShellOptions` and `InMemoryStorageService` identities remain external fallout requiring a separately authorized submodule change.
- Concurrent unrelated work remains visible under `references/Hexalith.Builds`, `references/Hexalith.EventStore`, `references/Hexalith.Memories`, and `references/Hexalith.Parties`; none is part of this reviewed change or commit.

### Follow-up review pass — 2026-07-11

Fresh follow-up review (the prior pass set `followup_review_recommended: true`) ran four adversarial layers — Blind Hunter, Edge Case Hunter, Verification Gap, Intent Alignment — over the full relocation diff (`c5d39c43..33f73104`). Edge Case Hunter and Verification Gap found nothing; Intent Alignment confirmed the diff faithfully implements the dominant reading of the intent (all 25 types moved to the correct owner, one-type-per-file, retained seams intact, no submodule touched). The only actionable finding was cosmetic.

Change applied this pass:
- Removed dead `using Hexalith.FrontComposer.Contracts.Rendering;` directives from 11 relocated action files (`RestoreGridStateAction`, `ClearGridStateAction`, `SortChangedAction`, `FiltersResetAction`, `LoadPageSucceededAction`, `LoadPageNotModifiedAction`, `LoadPageFailedAction`, `LoadPageCancelledAction`, `ClearPendingPagesAction`, `ResetColumnVisibilityAction`, `ScrollCapturedAction`) and dead `using System.Collections.Immutable;` from 7 of them — leftovers of the one-type-per-file split (files that still reference `GridViewSnapshot`/`ReservedFilterKeys`/`VirtualizationReservedKeys` in code or an unqualified `<see cref>` kept the using).

Verification:
- `dotnet build src/Hexalith.FrontComposer.Shell -c Release --no-restore` — passed, 0 warnings / 0 errors under `TreatWarningsAsErrors=true`, confirming the removed usings were dead.
- `git diff --check` on the edited files — no whitespace errors.

Findings breakdown: 1 low patch applied; 0 deferred (the previously-recorded popover-observability deferral was left untouched per the orchestrator-owned ledger); 9 low findings rejected as confirmed non-defects.

Follow-up review recommendation: false — this pass made a single localized, low-consequence cosmetic cleanup with no behavior, API, or verification impact.

Residual artifacts (in `git status`, not part of this commit): the updated story spec, `_bmad-output/implementation-artifacts/sprint-status.yaml`, and the `references/Hexalith.{Builds,EventStore,Memories,Parties}` submodule pointers.
