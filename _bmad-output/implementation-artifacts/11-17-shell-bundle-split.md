---
baseline_commit: 0a84e818b0ce220f291510ad094340f7296bb488
---
# Story 11.17d: Shell bundle split

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->
<!-- Type: mechanical refactor; fourth executable child of the non-implementable Story 11.17 decomposition parent. -->

## Story

As a FrontComposer maintainer,
I want the Shell interface, implementation, state, and DTO bundles split one direct top-level type per file,
so that the Shell follows the repository organization convention without changing runtime, UI, Fluxor, package, or public-API behavior.

## Acceptance Criteria

This story refines PRD FR-25, FR-28, and FR-29 and closes only the Shell child of architecture finding **M14**. The parent Story 11.17 remains a decomposition record, not an executable story.

1. **Mechanical Shell split.** **Given** baseline `0a84e818` contains 35 multi-type Shell production files with 149 direct top-level declarations, of which the six exact pure Fluxor action groups in AC3 contain 38 declarations, **when** the Shell child is complete, **then** the other 29 bundles and their 111 declarations are reorganized into one same-named direct top-level type per file in the existing folder and namespace. The result is 23 retained/eponymous originals, 6 removed aggregate-only originals, and 88 new same-named files (82 net additional files), with no non-exempt multi-type or filename-mismatch violation.

2. **Identity and behavior preservation.** **Given** the 111 target declarations are 97 public and 14 internal (33 classes, 22 enums, 17 interfaces, 37 records, and 2 readonly record structs), **when** declarations move, **then** each preserves its namespace, assembly, CLR full name, top-level/non-nested identity, accessibility, type kind and modifiers, base type/interfaces, generic and primary-constructor shape, attributes, enum values, record parameter order/defaults, constants, member order and bodies, XML/comments, nullable/directive semantics, exception behavior, DI lifetime, and serialization/reflection shape. Only source-document path/line information and the smallest compilation-required per-file import set may differ. Private nested helper types remain nested with their current owner.

3. **Narrow Fluxor action-group exception.** **Given** the repository permits the existing pure action-group convention to remain, **when** the organization guard evaluates exceptions, **then** only these six exact files are exempt: `CapabilityDiscoveryActions.cs`, `CommandPaletteActions.cs`, `GridViewHydratedAction.cs`, `DensityActions.cs`, `NavigationActions.cs`, and `ThemeActions.cs`. Their exact 38 public record identities and modifiers remain pinned; every direct declaration ends in `Action`; `ThemeChangedAction` remains intentionally non-sealed; and no wildcard, folder-level, `*Actions.cs`, or newly-created group exception is accepted. `ReconciliationSweepState.cs` is split because it mixes state, actions, a feature, and reducers.

4. **Durable non-vacuous governance.** **Given** file organization can regress silently, **when** `ShellTypeOrganizationGovernanceTests` runs, **then** it scans all handwritten Shell `.cs` files recursively, excludes only build/generated outputs, fails on an empty source census, traverses file/block/nested namespaces, counts declarations in conditional branches, treats only direct type/delegate declarations as candidates, normalizes `Foo.razor.cs` to `Foo`, enforces declaration/file parity, validates the exact six exceptions, and reports repository-relative paths plus declaration names. Synthetic negatives cover an interface+implementation/DTO bundle, mixed declaration kinds, inactive conditional declarations, nested namespaces, filename mismatch, and an unallowlisted action group. Source and reflection pins prove the exact 111 target identities, kinds/modifiers, public/internal split, and top-level assembly ownership.

5. **Shell and package contracts stay frozen.** **Given** this is a behavior-preserving organization change, **when** focused and broad validation runs, **then** the Shell organization/identity guard, existing layering and ownership guards, the broad Shell non-Contract lane, the solution default and Governance lanes, and version-aligned Shell package validation pass. `PublicAPI.FcTbl.Shipped.txt` is byte-identical and `FcTblPackageBoundaryTests` remains green; no compatibility suppression, package/project/solution, package inventory, dependency/version, Razor/CSS/JS, localization, generated output, snapshot, schema, wire, or published-product documentation change is introduced.

6. **Mechanical evidence is exact.** **Given** green tests alone cannot prove a mechanical move, **when** the story is promoted to review, **then** the Dev Agent Record contains a baseline-to-final normalized declaration-body comparison for all 111 targets, before/after census, exact tracked/untracked/submodule ledger, CRLF/UTF-8/final-newline checks for changed C# files, received/generated-artifact audit, and `git diff --check`. The File List matches the complete story-owned path union. Finding M14 is recorded as closed only for the Shell child; remaining Contracts/Testing debt and Stories 11.18/11.19 stay out of scope.

## Tasks / Subtasks

- [x] **Task 1 — Freeze the implementation baseline and live census (AC: #1, #2, #3, #6)**
  - [x] Record the implementation-start commit, tracked/untracked paths, and root gitlink state before editing. Preserve the concurrent Story 11.17a and sprint-status work present at story creation without absorbing, reverting, or relabeling it.
  - [x] Re-run the Roslyn-equivalent direct-declaration census. If HEAD differs from `baseline_commit`, reconcile the exact 35/149, 6/38, 29/111, 97/14, 23/6/88, and type-kind counts in this story before implementation rather than silently using stale numbers.
  - [x] Capture each target declaration independently of its original file so the final comparison proves body, modifiers, comments, directives, and nested-type ownership were preserved after newline normalization.

- [x] **Task 2 — Split the 29 non-exempt bundles into same-named files (AC: #1, #2, #5)**
  - [x] Apply the exact mapping in **Dev Notes > Current-State Split Map**. Retain the eponymous declaration in 23 originals, remove the six aggregate-only originals, and add the 88 listed same-directory files.
  - [x] Keep private nested helpers with their owners: `FrontComposerClaimExtractor.SegmentResult`, `FrontComposerUserTokenStore.StoredToken`, `NewItemIndicatorStateService.TrackedEntry`, `NewItemIndicatorStateService.TimerState`, and `ProjectionConnectionStateService.ConnectionLogBucket`.
  - [x] Preserve the current folder/namespace and Shell sublayer. Do not relocate public-looking DTOs into Contracts/Contracts.UI, combine this with layering cleanup, or unnest private implementation details.
  - [x] Copy only the imports required by each moved declaration. Removing a demonstrably unused copied import is permitted; declaration or behavior cleanup is not.

- [x] **Task 3 — Retain and document the exact Fluxor action exception (AC: #3, #4)**
  - [x] Leave the six action-group source files unchanged unless an import-only adjustment is strictly required to compile. Pin their exact relative paths, namespace, 38 declaration names, record kinds, accessibility, and modifiers.
  - [x] Update `_bmad-output/project-docs/source-tree-analysis.md` only enough to document the six grandfathered pure-action groups as the complete exception. State that new groups and mixed action/state/feature/reducer bundles are forbidden.
  - [x] Do not edit the repository-instruction submodule, action producers/consumers, reducers/effects, or single-writer ownership merely to restyle the retained groups.

- [x] **Task 4 — Add Shell organization and identity governance (AC: #2, #3, #4)**
  - [x] Add `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellTypeOrganizationGovernanceTests.cs`, reusing the reviewed CLI/SourceTools/MCP Roslyn guard patterns and the existing `Microsoft.CodeAnalysis.CSharp` reference. Do not add a package/project reference or edit either `.csproj`.
  - [x] Count direct compilation-unit/namespace types and delegates, recurse nested namespace declarations, activate inactive conditional branches, allow zero-declaration files such as `GlobalUsings.cs`, and fail if the production source locator returns no files.
  - [x] Enforce one declaration plus same-name parity (`.razor.cs` aware) everywhere except the exact action map. Assert every exception remains a non-empty pure `*Action` record group and reject an unlisted action group.
  - [x] Add synthetic negatives for every blind spot named in AC4 and exact source/reflection pins for the 111 moved declarations. Do not use a self-hosting census class whose own declaration changes the set it claims to pin.

- [x] **Task 5 — Prove behavior and public API did not move (AC: #2, #5)**
  - [x] Run existing tests covering components, bootstrap markers, EventStore classification/hub seams, storage, authentication/token relay, authorization, command feedback/empty-state CTA/JS interop, capability/palette/grid state, pending commands, projection connection/fallback, and reconciliation. Do not replace behavior tests with construction-only tests.
  - [x] Run `ShellLayeringTests`, `ShellOwnershipIdentityTests`, the new organization/identity class, and `FcTblPackageBoundaryTests`. Verify `PublicAPI.FcTbl.Shipped.txt` and `CompatibilitySuppressions.xml` are byte-identical to the implementation baseline.
  - [x] Pack Shell with a version-aligned build and `EnableFrontComposerPackageValidation=true`. Do not use `--no-build` with stale `1.0.0.0` assets; that produces a false CP0003 and is not grounds for a suppression file.
  - [x] Confirm there is still no general Shell PublicAPI baseline. Do not invent one: declaration-body/identity pins, FC-TBL baseline, behavior lanes, and package ApiCompat are the compatibility evidence.

- [x] **Task 6 — Run broad gates and reconcile the exact artifact ledger (AC: #5, #6)**
  - [x] Perform a Release-aligned restore and serialized build, then run the focused guard, broad Shell non-Contract, solution default, and Governance commands under `DiffEngine_Disabled=true` as listed below.
  - [x] Build the File List from the union of `git diff --name-status <baseline>` and `git ls-files --others --exclude-standard`; audit root gitlinks separately. `git diff` alone cannot see the 88 new unstaged files.
  - [x] Verify all changed/new C# files are UTF-8, CRLF-only, final-newline clean, and free of trailing whitespace. Audit `*.received.*`, generated output, package/API/suppression changes, and run `git diff --check`.
  - [x] Record before/after counts, exact commands/results, baseline-to-final body comparison, unchanged exceptions/baselines, current test totals, and the exact story-owned File List before moving to review.

## Dev Notes

### Scope and Current-State Decisions

1. **This is Story 11.17d, not the parent.** The parent is explicitly non-implementable. CLI, SourceTools, and MCP/runtime are independent 11.17a-c children; this file promotes only the Shell child.
2. **The live tree overrides July estimates.** The architecture review counted 31 Shell `.cs` offenders plus four component code-behinds. After Stories 11.9 and 11.11-11.16, the verified baseline still totals 35 files / 149 direct declarations. Six pure action groups account for 38; the executable split is 29 files / 111 declarations / 82 excess.
3. **Same-name parity changes the path math.** Six bundles have no eponymous declaration: `FrontComposerBootstrapMarkers.cs`, `FrontComposerTokenRelay.cs`, `PendingCommandModels.cs`, `ProjectionConnectionState.cs`, `ProjectionFallbackRefreshContracts.cs`, and `ReconnectionReconciliationState.cs`. Delete those six after extraction. The exact production delta is therefore 23 modified originals + 6 deletions + 88 additions, not “29 modified + 82 additions.”
4. **Direct top-level only.** Nested types retain containing-type identity and private accessibility. The guard must not force them to top level.
5. **Mechanical means source-preserving.** Do not modernize constructors, validation, null checks, cancellation, logging, collection expressions, synchronization, exception handling, authorization, serialization, or comments while moving declarations. Small import cleanup is the only permitted incidental source change.
6. **UX impact is N/A by intent.** No `.razor`, CSS, JS, copy, route, focus, selector, responsive, accessibility, or visual behavior changes are expected. Fluent UI v5/Fluent 2, UX-DR3/4/6/8, FC-IA-1, and lifecycle-truth contracts remain preservation gates.

### Current-State Split Map

Every listed UPDATE file was read in full during story creation. “Move” means create the same-named file in the same directory while leaving the named owner in the original; “replace” means the aggregate filename has no owner declaration and is deleted after all declarations move.

| Current UPDATE file | Current responsibility and declarations | Required mechanical change / preservation focus |
|---|---|---|
| `Components/Badges/FcDesaturatedBadge.razor.cs` | Badge component + `OptimisticBadgeState` | Move `OptimisticBadgeState`; preserve component partial identity, appearance, names, and accessibility behavior. |
| `Components/DataGrid/FcColumnPrioritizer.razor.cs` | Grid component + `ColumnDescriptor` + `ColumnVisibilityContext` | Move both support types; preserve component parameters/visibility behavior and exact FC-TBL API baseline. |
| `Components/Home/FcHomeDirectory.razor.cs` | Home component + `HomeCardModel` + `HomeProjectionRow` | Move both DTOs; preserve grouping, urgency sorting, counts, and Razor partial identity. |
| `Components/Lifecycle/LifecycleUiState.cs` | `LifecycleUiState` + `LifecycleTimerPhase` | Move the enum; preserve lifecycle phases, computed state, and timer semantics. |
| `Extensions/FrontComposerBootstrapMarkers.cs` | Bootstrap stage, marker interface, Quickstart/Domain/EventStore marker records | Replace with five same-named files; preserve stage values/order and startup ordering/fail-fast behavior. |
| `Infrastructure/EventStore/EventStoreResponseClassifier.cs` | Classifier + command/query classification records + outcome enum | Move `EventStoreCommandClassification`, `EventStoreQueryClassification`, `QueryClassificationOutcome`; preserve HTTP/status/problem-details bounds and redaction. |
| `Infrastructure/EventStore/IProjectionHubConnection.cs` | Hub and factory interfaces + connection state enum/change record | Move `IProjectionHubConnectionFactory`, `ProjectionHubConnectionState`, `ProjectionHubConnectionStateChanged`; preserve SignalR callback/overload/wire seam. |
| `Infrastructure/Storage/LocalStorageService.cs` | Storage implementation + internal `PendingWrite` | Move `PendingWrite`; preserve JS interop, write coalescing, disposal, and scoped lifetime. |
| `Options/FrontComposerAuthenticationOptions.cs` | Root auth options + provider enum + OIDC/SAML/GitHub/custom/redirect/cookie/token-relay options | Move the eight sibling types to same-named files; preserve public binding names, defaults, validation, and mutually exclusive recipes. |
| `Services/Auth/FrontComposerClaimExtractor.cs` | Claim extractor + `FrontComposerClaimExtractionResult` | Move the result; keep `SegmentResult` nested; preserve fail-closed claim validation and support-safe values. |
| `Services/Auth/FrontComposerTokenRelay.cs` | Token store, circuit accessor/handler, gateway authorization handler | Replace with four same-named files; keep `StoredToken` nested; preserve expiry/eviction, circuit-safe acquisition, lifetimes, and no-token-logging rule. |
| `Services/Authorization/CommandAuthorizationDecision.cs` | Three enums + request/resource/decision records | Move five siblings; preserve enum values, resource/request shape, and each record's redacting `PrintMembers`. |
| `Services/Feedback/ICommandFeedbackPublisher.cs` | Publisher interface + `CommandFeedbackWarning` | Move the warning record; preserve feedback payload and publish contract. |
| `Services/IEmptyStateCtaResolver.cs` | Resolver interface + `EmptyStateCta` | Move the CTA record; preserve route, policy, disabled, and support-safe resolution semantics. |
| `Services/IExpandInRowJSModule.cs` | JS-module interface + `ExpandInRowJSModule` implementation | Move the implementation; preserve interop calls, lifetime, and disposal. |
| `State/CapabilityDiscovery/FrontComposerCapabilityDiscoveryState.cs` | Capability state + hydration enum | Move `CapabilityDiscoveryHydrationState`; preserve state defaults and hydration lifecycle. |
| `State/CommandPalette/PaletteResult.cs` | Result record + category/load enums | Move `PaletteResultCategory` and `PaletteLoadState`; preserve record ordering/defaults and palette state semantics. |
| `State/DataGridNavigation/IProjectionPageLoader.cs` | Loader interface + page result + null loader | Move `ProjectionPageResult` and `NullProjectionPageLoader`; preserve default failure/empty behavior and interface shape. |
| `State/DataGridNavigation/LoadedPageReducers.cs` | Loaded-page reducers + virtualization reducers | Move `VirtualizationViewStateReducers`; preserve every `[ReducerMethod]` and Fluxor discovery. |
| `State/PendingCommands/ICommandExecutionAdmissionGate.cs` | Admission interface/request/denial enum/disposable admission/internal releaser | Move four siblings; preserve FC-CNC locking, denial reasons, and release/disposal semantics. |
| `State/PendingCommands/NewItemIndicatorStateService.cs` | Entry record + interface + service | Move `NewItemIndicatorEntry` and `INewItemIndicatorStateService`; keep `TrackedEntry`/`TimerState` nested; preserve timer generation and scope flush. |
| `State/PendingCommands/PendingCommandModels.cs` | Nine registration/status/outcome/entry/result/observation models | Replace with `PendingCommandRegistration`, `PendingCommandStatus`, `PendingCommandTerminalOutcome`, `PendingCommandRegistrationStatus`, `PendingCommandResolutionStatus`, `PendingCommandEntry`, `PendingCommandRegistrationResult`, `PendingCommandTerminalObservation`, and `PendingCommandResolutionResult` files; preserve ULID validation, record parameters, enum ordinals, and no-raw-payload boundary. |
| `State/PendingCommands/PendingCommandOutcomeResolver.cs` | Source/status enums + observation/result + resolver interface/implementation | Move the five sibling declarations; preserve resolution precedence, idempotency, and terminal semantics. |
| `State/PendingCommands/PendingCommandPollingCoordinator.cs` | Status-query/null-query + polling interface/implementation | Move `IPendingCommandStatusQuery`, `NullPendingCommandStatusQuery`, `IPendingCommandPollingCoordinator`; preserve cadence, expiry, cancellation, and polling ownership. |
| `State/ProjectionConnection/ProjectionConnectionState.cs` | Status/snapshot/transition + interface/service | Replace with five same-named files; keep `ConnectionLogBucket` nested; preserve publisher, deduplication, rate-limiting, and connection transitions. |
| `State/ProjectionConnection/ProjectionFallbackRefreshContracts.cs` | Lane/outcome/group key + scheduler interface + reconciliation result | Replace with five same-named files; preserve group equality, default interface members, and refresh-result shape. |
| `State/ReconnectionReconciliation/ReconciliationSweepState.cs` | Sweep state/marker + two actions + feature/reducers | Retain `ReconciliationSweepState`; move the five siblings. Preserve Fluxor feature/reducer/action discovery; do not create a seventh action exception. |
| `State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs` | Coordinator interface + implementation | Move `IReconnectionReconciliationCoordinator`; preserve epoch, subscription, and disposal behavior. |
| `State/ReconnectionReconciliation/ReconnectionReconciliationState.cs` | Status/snapshot + interface/service | Replace with four same-named files; preserve snapshot publication, epoch transitions, and state-service lifetime. |

### Exact Fluxor Exception Inventory

The exception is frozen to these paths and identities; it is not a naming pattern:

- `State/CapabilityDiscovery/CapabilityDiscoveryActions.cs`: `BadgeCountsSeededAction`, `BadgeCountChangedAction`, `CapabilityVisitedAction`, `SeenCapabilitiesHydratedAction`.
- `State/CommandPalette/CommandPaletteActions.cs`: `PaletteOpenedAction`, `PaletteClosedAction`, `PaletteQueryChangedAction`, `PaletteScopeChangedAction`, `PaletteResultsComputedAction`, `PaletteSelectionMovedAction`, `PaletteResultActivatedAction`, `RecentRouteVisitedAction`, `PaletteHydratedAction`, `PaletteHydratingAction`, `PaletteHydratedCompletedAction`.
- `State/DataGridNavigation/GridViewHydratedAction.cs`: `GridViewHydratedAction`, `DataGridNavigationHydratingAction`, `DataGridNavigationHydratedCompletedAction`.
- `State/Density/DensityActions.cs`: `DensityChangedAction`, `UserPreferenceChangedAction`, `UserPreferenceClearedAction`, `DensityHydratedAction`, `EffectiveDensityRecomputedAction`, `DensityHydratingAction`, `DensityHydratedCompletedAction`.
- `State/Navigation/NavigationActions.cs`: `SidebarToggledAction`, `NavGroupToggledAction`, `ViewportTierChangedAction`, `SidebarExpandedAction`, `NavigationHydratedAction`, `LastActiveRouteChangedAction`, `LastActiveRouteHydratedAction`, `StorageReadyAction`, `NavigationHydratingAction`, `NavigationHydratedCompletedAction`.
- `State/Theme/ThemeActions.cs`: `ThemeChangedAction`, `ThemeHydratingAction`, `ThemeHydratedCompletedAction`.

### Architecture Compliance and Never-List

- Preserve the Shell sublayers already enforced by `ShellLayeringTests`: Components render; Routing is pure; State owns slices/contracts/coordinators; Infrastructure owns external adapters/workers; Telemetry is cross-cutting. This story changes physical source files, not ownership or dependency direction.
- Shell remains a packable/trimmable net10 Razor SDK project referencing Contracts + Contracts.UI. Shell.Tests already references Roslyn. SDK default compile items include new `.cs` files automatically; no `.csproj` edit is required.
- Keep file-scoped namespace = folder, using directives outside namespaces, Allman braces, nullable/implicit usings, CRLF UTF-8, final newline, no headers, and warnings as errors.
- Do not touch: LoggerMessage migration (11.18), CS1591/analyzer policy/localization (11.19), contracts/package ownership, generated code, query/schema/wire formats, auth or state behavior, CSS/visual work, release inventory, submodules, dependency versions, compatibility suppressions, or unrelated multi-type files in Contracts/Testing.
- `PublicAPI.FcTbl.Shipped.txt`, `CompatibilitySuppressions.xml`, Shell/Shell.Tests project files, `Directory.Packages.props`, solution files, release inventory, Razor/CSS/JS, and generated/snapshot outputs are expected unchanged.

### Library and Framework Requirements

- Stay on repository pins: .NET SDK `10.0.301`, net10/C# latest, Fluxor `6.9.0`, Fluent UI Blazor `5.0.0-rc.4-26180.1`, Roslyn `5.6.0`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and the current Shell dependency graph. No upgrade is part of this story.
- The .NET SDK implicitly includes `.cs` compile items by default, so the 88 files require no project entries. [Microsoft.NET.Sdk default item inclusion](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#default-item-inclusion-properties)
- File-scoped namespaces apply to the whole file and preserve the namespace-qualified type identity when declarations move without changing namespace. Keep using placement semantically equivalent. [C# namespaces and using directives](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/namespaces)
- `InternalsVisibleTo` remains assembly-based, so moving the 14 internal declarations inside the same Shell assembly requires no friend-assembly change. [Friend assemblies](https://learn.microsoft.com/en-us/dotnet/standard/assembly/friend)
- xUnit v3 supports direct executable `-class` and trait filtering; use the repository-pinned runner and commands below rather than upgrading test packages. [xUnit v3 filtering](https://xunit.net/docs/getting-started/v3/whats-new#test-filtering-expressions)

### Testing Requirements

Use a Release-aligned restore because Release consumes published Hexalith dependencies. The current 4.0 compatibility line must build the assembly used for packing; a stale `--no-build` pack can create false ApiCompat evidence.

```bash
dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj \
  -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests \
  -class Hexalith.FrontComposer.Shell.Tests.Architecture.ShellTypeOrganizationGovernanceTests \
  -parallel none

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests \
  -class Hexalith.FrontComposer.Shell.Tests.Architecture.ShellLayeringTests \
  -class Hexalith.FrontComposer.Shell.Tests.Architecture.ShellOwnershipIdentityTests \
  -class Hexalith.FrontComposer.Shell.Tests.Components.DataGrid.FcTblPackageBoundaryTests \
  -parallel none

DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests \
  -notrait Category=Contract -notrait Category=Performance -notrait Category=e2e-palette \
  -notrait Category=NightlyProperty -notrait Category=Quarantined -parallel none

DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --no-restore \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 \
  --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"

DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --no-restore \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 --filter "Category=Governance"

dotnet pack src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj \
  -c Release --no-restore -m:1 -o /tmp/frontcomposer-11-17d-pack \
  -p:Version=4.0.0-review.shellsplit -p:MinVerVersionOverride=4.0.0 \
  -p:EnableFrontComposerPackageValidation=true -p:NuGetAudit=false

BASELINE=<implementation-start-commit>
git diff --name-status "$BASELINE"
git ls-files --others --exclude-standard
git diff --submodule=short "$BASELINE" -- references
rg --files -g '*.received.*' -g '!references/**' -g '!**/bin/**' -g '!**/obj/**'
git diff --check
```

If a broad gate is environmentally blocked, record the exact command and blocker separately from the focused direct-xUnit evidence; do not weaken or relabel the gate. Test totals are evidence, not hard-coded expectations.

### Previous-Story Intelligence

- Story 11.16 proved that a green build is insufficient for a mechanical refactor: review found a dropped null/empty guard and an incomplete gitlink ledger. Compare declaration bodies directly and reconcile tracked, untracked, and submodule paths separately.
- Story 11.16 also required an explicit Release restore because a configuration-neutral restore selected Debug/source assets. Keep restore/build/pack configuration aligned.
- Story 11.12 is the strongest prior Shell split: it moved DataGrid/ExpandedRow Fluxor actions out of Contracts one per file while preserving constructors and generated consumers. It also demonstrates that public-looking Shell actions/DTOs stay Shell-owned; do not move this story's types back into Contracts.
- Preserve the current Story 11.15/11.16 hardening: storage scope resolution, snapshot publication, fatal/cancellation policy, hydration state, JSON profiles, and generated-literal behavior are not cleanup opportunities here.

### Git Intelligence

- Creation baseline: `0a84e818b0ce220f291510ad094340f7296bb488` (`feat(tests): add BenchmarkHarnessGovernanceTests and update BenchmarkHarnessTests`), current tag `v3.2.2`; the approved next compatibility line is 4.0 because Story 11.17c removed MCP benchmark API.
- `7f53cf3f` / the reviewed Story 11.17a CLI split established declaration-body comparison, exact identity pins, non-empty Roslyn scans, recursive namespace traversal, inactive-branch counting, exported-type evidence, and exact tracked/untracked ledgers.
- `6ee67cfb` split the SourceTools Drift aggregate into same-named files and deleted the non-eponymous aggregate while preserving generated bytes and incremental-cache seams.
- `a7e94471` split MCP/runtime and relocated benchmark ownership. Shell differs: every target remains in the same assembly/namespace, so no ApiCompat removal, suppression, migration guide, package-version decision, or resource move is authorized.
- `0a84e818` moved an MCP benchmark census guard outside the guarded set. Keep the Shell guard's census independent so it does not invalidate itself.
- The worktree became concurrently dirty during creation in `11-17-cli-package-split.md` and `sprint-status.yaml`. Those changes belong to other work and must be preserved; the implementation File List must not absorb them.

### Project Structure Notes

- Production scope: exactly the 29 current UPDATE bundles and their same-directory NEW files in the split map. The six pure action-group files are retained exceptions, not implementation targets.
- Expected test ADD: `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellTypeOrganizationGovernanceTests.cs`.
- Expected documentation UPDATE: `_bmad-output/project-docs/source-tree-analysis.md`, limited to the exact exception and resulting Shell one-type-per-file topology.
- Creation-time artifact changes are limited to this story file and the surgical sprint-status transition. Implementation must replace the initial File List below with its exact story-owned ledger before review.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Epic 11 implementation order and Story 11.17 decomposition]
- [Source: `_bmad-output/planning-artifacts/prd.md` — FR-25, FR-28, FR-29; NFR-1, NFR-11]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — Shell sublayers and key invariants]
- [Source: `_bmad-output/planning-artifacts/ux-design.md` — UX-DR3/4/6/8 preservation]
- [Source: `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` — finding M14]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-readiness-major-issues.md` — Proposal 2B]
- [Source: `_bmad-output/implementation-artifacts/11-16-fatal-hydration-json-and-generated-literal-helper-consolidation.md`]
- [Source: `_bmad-output/implementation-artifacts/11-17-cli-package-split.md`]
- [Source: `_bmad-output/implementation-artifacts/11-17-sourcetools-package-split.md`]
- [Source: `_bmad-output/implementation-artifacts/11-17-mcp-runtime-split-and-benchmark-relocation.md`]
- [Source: `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`]
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj`]
- [Source: `src/Hexalith.FrontComposer.Shell/PublicAPI.FcTbl.Shipped.txt`]
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcTblPackageBoundaryTests.cs`]
- [Source: `tests/Hexalith.FrontComposer.Cli.Tests/Architecture/CliTypeOrganizationGovernanceTests.cs`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-15 creation: loaded the complete Epic 11, PRD, architecture, UX, project-context, prior-story, sibling-story, git/package, and current Shell source evidence.
- 2026-07-15 creation census: 35 multi-type files / 149 declarations; six exact action groups / 38 declarations; implementation scope 29 files / 111 declarations (97 public, 14 internal) / 82 excess; same-name topology = 23 retained + 6 deleted + 88 new.
- 2026-07-15 current-baseline evidence: Shell.Tests Release build 0 warnings/errors; broad Shell non-Contract 2,305/2,305; Governance 144/144; FC-TBL 1/1; version-aligned Shell package validation passed against the 3.0 baseline.
- 2026-07-15 implementation plan: preserve declaration text mechanically in story order; split the 29 bundles; document only the six exact action exceptions; add a Roslyn organization/source/reflection guard; then run focused, broad, package, artifact, encoding, and declaration-body comparison gates.
- 2026-07-15 implementation baseline: HEAD and `baseline_commit` are both `0a84e818b0ce220f291510ad094340f7296bb488`. Pre-edit unrelated/concurrent state is `11-17-cli-package-split.md`, `deferred-work.md`, the pre-existing sprint-status changes, and the `references/Hexalith.Tenants` gitlink; the new story file is untracked. These paths are preserved and excluded from production ownership.
- 2026-07-15 live Roslyn census: 300 handwritten Shell C# sources; 35 multi-type files / 149 declarations; exact exception 6 / 38; split target 29 / 111; public/internal 97/14; kinds 33 classes, 22 enums, 17 interfaces, 37 records, 2 readonly record structs; topology 23 retained + 6 removed + 88 added. Per-declaration namespace/name/kind/accessibility and newline-normalized SHA-256 prefixes were captured for final comparison.
- 2026-07-15 pre-edit focused preservation lane: `ShellLayeringTests` + `ShellOwnershipIdentityTests` + `FcTblPackageBoundaryTests` passed 9/9 via the Release direct xUnit v3 runner.
- 2026-07-15 Aspire baseline: root `aspire start` could not reach resource startup because the unchanged Parties submodule fails HFC0001 at `PartiesAdminPortalApiClient.cs` lines 108, 160, and 262. A clean retry removed transient file-lock noise; `--no-build` could not start because no AppHost binary exists. No AppHost remained running, no submodule was edited, and Shell Release/focused gates remain the story validation path.
- 2026-07-15 Task 2 RED: the pre-split Roslyn organization check failed as expected with 29 non-exempt bundles / 111 declarations.
- 2026-07-15 Task 2 GREEN/refactor: mechanically produced 23 retained owner files, six aggregate deletions, and 88 same-directory additions. `dotnet format` IDE0005 removed only unnecessary copied imports. The post-split census is 382 sources with only the six exact action groups multi-type; baseline-to-current comparison reports 111 targets / zero newline-normalized declaration differences. Shell.Tests Release build passed with zero warnings/errors.
- 2026-07-15 Task 2 regression repair: the first broad Shell lane exposed three filename-coupled source-scan failures. `AuthBoundaryTests` now retains its exact provider/token exemptions on the split option and gateway-handler files, and `HydrationStateConsolidationTests` locates the distinct capability enum in its same-named file. Focused repair lane passed 8/8; layering/ownership/FC-TBL passed 9/9; broad non-Contract passed 2,305/2,305.
- 2026-07-15 Task 3: all six exact Fluxor action files are byte-identical to baseline; the live Roslyn census remains six files / 38 declarations. `source-tree-analysis.md` now records every exact path and identity, the intentional non-sealed `ThemeChangedAction`, and the ban on wildcard/new/mixed group exceptions. No action producer, consumer, reducer, effect, or submodule changed.
- 2026-07-15 Task 4 RED/GREEN: the required Shell organization guard was absent before implementation. The new Roslyn guard scans a non-empty handwritten source set, exhaustively activates up to eight conditional symbols per file and flattens literal-disabled branches, traverses block/file/nested namespaces, normalizes `.razor.cs`, permits zero declarations, freezes the exact six action paths / 38 identities and modifiers, and pins all 111 split declarations in source and reflection (97 public / 14 internal). Six synthetic negative tests cover interface/implementation/DTO bundles, mixed kinds (including a literal `#if false` declaration), nested namespaces, filename mismatch, and an unallowlisted action group. The focused class passed 9/9 without project/package changes.
- 2026-07-15 Task 5 compatibility proof: the post-split broad behavior lane remained green 2,305/2,305. A Release/4.0-aligned Shell.Tests build completed with zero warnings/errors; organization, layering, ownership identity, and FC-TBL boundary classes passed 18/18. `PublicAPI.FcTbl.Shipped.txt` and `CompatibilitySuppressions.xml` are byte-identical to baseline, and Shell still has no general PublicAPI file. A build-enabled `4.0.0-review.shellsplit` pack with package validation produced the nupkg/snupkg successfully without compatibility suppressions.
- 2026-07-15 Task 6 release gates: Release restore succeeded; serialized solution build with `MinVerVersionOverride=4.0.0` completed with zero warnings/errors. The focused organization class passed 9/9. The first broad Shell run found the new manifest in the exact auth source scanner; adding only `ShellTypeOrganizationGovernanceTests.cs` to that test's explicit allowlist repaired the governance interaction (affected test 1/1). Final broad Shell non-Contract passed 2,314/2,314, solution default passed 4,108/4,108, and Governance passed 302/302 under `DiffEngine_Disabled=true`.
- 2026-07-15 Task 6 mechanical evidence: final census is 382 handwritten Shell C# sources with only the exact six action groups / 38 declarations multi-type, versus baseline 300 sources / 35 files / 149 declarations; split targets moved from 29 files / 111 declarations to zero. Newline-normalized Roslyn comparison reports 111 targets / zero declaration differences. The story owns 33 tracked modified/deleted paths and 90 untracked paths (123 changed paths total); the File List also classifies the two unchanged baseline-evidence files. All 114 changed/new C# files are valid UTF-8, CRLF-only, final-newline clean, and trailing-whitespace-free. No `*.received.*` or generated artifact was introduced; the six action files, FC-TBL API baseline, compatibility suppressions, Shell/Shell.Tests projects, packages props, and solution are unchanged; `git diff --check` passed.
- 2026-07-15 Task 6 artifact reconciliation: `validate-story-artifacts.py` passed with the concurrent 11.17a record, deferred-work ledger, and Tenants gitlink explicitly classified as unrelated. Root gitlink audit contains only the pre-existing `references/Hexalith.Tenants` advance `7b271a452b51c0d8e50a12ccbc7ee2d2393c71de` -> `28630b94a7b4931dcd6796eb50ad1c21b092055d`; no submodule was edited by this story.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 11.17d is independently executable; the parent remains non-implementable and siblings 11.17a-c remain separately tracked.
- Live-source scope, exact Fluxor exceptions, preservation map, non-vacuous guard design, validation commands, package-validation pitfall, and artifact-ledger requirements are resolved without user intervention.
- Task 1 froze the exact implementation baseline, unrelated-work ledger, Roslyn census, and per-declaration preservation hashes.
- Task 2 completed the exact 29-bundle mechanical split with 111/111 declaration bodies preserved and all current Shell non-Contract regressions green.
- Task 3 preserved the six grandfathered pure-action groups byte-for-byte and documented their exact closed exception set.
- Task 4 added the non-vacuous Shell organization/source/reflection governance guard; its production and synthetic lanes pass 9/9.
- Task 5 preserved the existing Shell behavior/API seams and passed the version-aligned package compatibility gate without a new baseline or suppression.
- Task 6 completed all Release, focused, broad, solution, Governance, package, body-preservation, encoding, artifact, and story-ledger gates. M14 is closed for the Shell child only; remaining Contracts/Testing organization debt and Stories 11.18/11.19 remain out of scope.

### Documented Unrelated Workspace State

- `_bmad-output/implementation-artifacts/11-17-cli-package-split.md` — concurrent Story 11.17a implementation record that pre-dated this story.
- `_bmad-output/implementation-artifacts/deferred-work.md` — concurrent deferred-work ledger changes that pre-dated this story.
- `references/Hexalith.Tenants` — accepted pre-existing root gitlink drift; this story did not edit the submodule.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` is a shared dirty path: only the 11.17d transition comments/value are story-owned; all concurrent pre-existing edits are preserved.

### File List

- `src/Hexalith.FrontComposer.Shell/CompatibilitySuppressions.xml` (named exception baseline evidence — byte-identical and intentionally unchanged)
- `src/Hexalith.FrontComposer.Shell/PublicAPI.FcTbl.Shipped.txt` (named exception baseline evidence — byte-identical and intentionally unchanged)
- `src/Hexalith.FrontComposer.Shell/Components/Badges/FcDesaturatedBadge.razor.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/Components/Badges/OptimisticBadgeState.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/ColumnDescriptor.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/ColumnVisibilityContext.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/Components/Home/HomeCardModel.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Components/Home/HomeProjectionRow.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/LifecycleTimerPhase.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/LifecycleUiState.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/Extensions/DomainBootstrapMarker.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Extensions/EventStoreBootstrapMarker.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapMarkers.cs` (deleted — replaced aggregate-only bundle)
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerBootstrapStage.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Extensions/IFrontComposerBootstrapMarker.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Extensions/QuickstartBootstrapMarker.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClassification.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClassification.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnection.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/IProjectionHubConnectionFactory.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionHubConnectionState.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionHubConnectionStateChanged.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/QueryClassificationOutcome.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/PendingWrite.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthCookieOptions.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthRedirectOptions.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthenticationOptions.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthenticationProviderKind.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerCustomBrokeredOptions.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerGitHubOAuthOptions.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerOpenIdConnectOptions.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerSaml2Options.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerTokenRelayOptions.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/CircuitServicesAccessor.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerCircuitServicesHandler.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerClaimExtractionResult.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerClaimExtractor.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerGatewayAuthorizationHandler.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs` (deleted — replaced aggregate-only bundle)
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerUserTokenStore.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecisionKind.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationReason.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationRequest.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationResource.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationSurface.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/EmptyStateCta.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/ExpandInRowJSModule.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Feedback/CommandFeedbackWarning.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/Services/Feedback/ICommandFeedbackPublisher.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/Services/IEmptyStateCtaResolver.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/Services/IExpandInRowJSModule.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryHydrationState.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/FrontComposerCapabilityDiscoveryState.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteLoadState.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteResult.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/PaletteResultCategory.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/IProjectionPageLoader.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageReducers.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/NullProjectionPageLoader.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/ProjectionPageResult.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/VirtualizationViewStateReducers.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/CommandExecutionAdmission.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/CommandExecutionAdmissionDenialReason.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/CommandExecutionAdmissionRequest.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/ICommandExecutionAdmissionGate.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/ICommandExecutionAdmissionReleaser.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/INewItemIndicatorStateService.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/IPendingCommandOutcomeResolver.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/IPendingCommandPollingCoordinator.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/IPendingCommandStatusQuery.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorEntry.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NullPendingCommandStatusQuery.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandEntry.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs` (deleted — replaced aggregate-only bundle)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeObservation.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolutionResult.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolutionStatus.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeSource.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRegistration.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRegistrationResult.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRegistrationStatus.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandResolutionResult.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandResolutionStatus.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStatus.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandTerminalObservation.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandTerminalOutcome.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/IProjectionConnectionState.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/IProjectionFallbackRefreshScheduler.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionSnapshot.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionState.cs` (deleted — replaced aggregate-only bundle)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStateService.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStatus.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionTransition.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackGroupKey.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackLane.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackLaneRefreshOutcome.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshContracts.cs` (deleted — replaced aggregate-only bundle)
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionReconciliationRefreshResult.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ClearExpiredReconciliationSweepsAction.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/IReconnectionReconciliationCoordinator.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/IReconnectionReconciliationState.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/MarkReconciliationSweepAction.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepFeature.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepMarker.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepReducers.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepState.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs` (modified — retained eponymous declaration)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationSnapshot.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationState.cs` (deleted — replaced aggregate-only bundle)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationStateService.cs` (new — extracted same-named declaration)
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationStatus.cs` (new — extracted same-named declaration)
- `_bmad-output/project-docs/source-tree-analysis.md` (modified — exact six-file Shell action-group exception and one-type topology)
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/AuthBoundaryTests.cs` (modified — exact split-file security-source exemptions)
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellTypeOrganizationGovernanceTests.cs` (new — exact organization, action-exception, source, reflection, and synthetic governance)
- `tests/Hexalith.FrontComposer.Shell.Tests/State/HydrationStateConsolidationTests.cs` (modified — same-named capability enum source path)
- `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md` (new — Story 11.17d implementation and evidence record)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (updated — Story 11.17d moved from in-progress to review; concurrent entries preserved)

## Change Log

- 2026-07-15: Created executable Story 11.17d and marked it ready-for-dev. Scoped the live Shell census, exact six-file Fluxor exception, 29-bundle/111-declaration mechanical split, durable organization/identity guard, behavior/API/package preservation lanes, and Shell-only M14 closure evidence.
- 2026-07-15: Implemented Story 11.17d. Split 29 Shell bundles into one same-named declaration per file while retaining the exact six/38 action exception, added non-vacuous source/reflection governance, preserved 111/111 declaration bodies and all behavior/API/package seams, passed Release/default/Governance/package/artifact gates, and moved the story to review.
