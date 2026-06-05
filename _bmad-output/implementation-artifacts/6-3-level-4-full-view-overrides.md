---
baseline_commit: 02840a0b9223e7e1a7870616d7bf19ecbf5eb95b
---

# Story 6.3: Level-4 full-view overrides

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **Brownfield reality - read this first.** This is primarily a **CONFIRM-AND-PIN + CONTRACT-PRODUCING**
> story, not a greenfield feature. The Level-4 full projection-view replacement surface already exists
> under historical source/test labels such as `Story 6-4`, `DN*`, `P*`, and `AC*`. Under the current
> `epics.md` plan, **Level-4 full-view overrides = Story 6.3**. Do not churn old labels unless editing
> the same line for a real code reason.
>
> The current implementation already has `ProjectionViewOverrideDescriptor`,
> `ProjectionViewOverrideContractVersion`, `ProjectionViewContext<TProjection>`,
> `IProjectionViewOverrideRegistry`, `ProjectionViewOverrideRegistry`, `ProjectionViewOverrideDescriptorSource`,
> `AddViewOverride<TProjection,TComponent>`, generated `ProjectionViewOverrideRegistry.Resolve(...)`
> precedence, `FcProjectionViewOverrideHost<TProjection>`, a live Counter `CounterFullViewReplacement`
> sample, and default-lane tests. The main danger is **lying about diagnostic phase**: HFC1042-HFC1046
> are reserved/registered in the diagnostic catalog, but the live Level-4 path is currently runtime
> descriptor registration plus registry/host validation, not a SourceTools marker/emitter path like
> `[ProjectionTemplate]`. This story must record the real phase with evidence.

## Story

As an adopter developer,
I want to override an entire projection view from an external assembly,
so that I have a final escape hatch for bespoke pages.

## Acceptance Criteria

**AC1 - A registered Level-4 view override supplies the full projection body.** *(Epic 6 AC; FR8)*
**Given** a Level-4 replacement registered through
`AddViewOverride<TProjection,TComponent>(ProjectionRole? role = null, ...)` or a
`ProjectionViewOverrideDescriptorSource` carrying a valid descriptor,
**When** the generated projection view renders for the matching projection and role,
**Then** `IProjectionViewOverrideRegistry.Resolve(projectionType, role)` selects the Level-4 descriptor
before any Level-2 template lookup,
**And** the generated view renders `FcProjectionViewOverrideHost<TProjection>` with a fresh
`ProjectionViewContext<TProjection>` instead of the generated default body,
**And** the framework-owned envelope around the projection body remains intact: shell, route/page wrapper,
loading/empty policy, authorization gates, lifecycle wrapper, telemetry/render context, grid envelope where
the generator owns it, and disposal hooks remain FrontComposer-owned.

**AC2 - Level-4 precedence over Level-2 is deterministic and pinned.** *(Epic 6 AC)*
**Given** both a Level-4 full-view override and a Level-2 projection template exist for the same
projection and role,
**When** the generated projection resolves its body,
**Then** the documented FC-CUST order is followed without ambiguity:
1. Level 4 full-view override;
2. Level 2 template;
3. generated default body.
**And** Level-3 field slots compose only when a Level-4 or Level-2 body explicitly delegates to
`Context.FieldRenderer(...)`, `Context.RowRenderer(...)`, `Context.SectionRenderer(...)`, or
`Context.DefaultBody`,
**And** `Context.DefaultBody` bypasses the active Level-4 descriptor for the same projection/role to
avoid recursion; remounting the generated projection component inside a replacement is unsupported.

**AC3 - Level-4 invalid/duplicate/contract diagnostics are truthful and pinned.**
**Given** invalid Level-4 projection, component, duplicate, contract-version, and accessibility cases,
**When** the dev agent audits build, startup, and render-time call sites,
**Then** the story records and proves the real phase for each diagnostic:
- **HFC1042** invalid projection type: currently reserved/defined for build-time diagnostics, but no
  `[ProjectionViewOverride]` marker/parser/emitter path is implemented or proven today.
- **HFC1043** invalid component: currently logged by `ProjectionViewOverrideRegistry`; invalid
  descriptors are ignored and generated/default lower-level rendering wins.
- **HFC1044** duplicate view override: currently a deterministic registry-construction failure for
  different components on the same `(projection, role)` tuple; idempotent same-component re-registration
  remains a no-op.
- **HFC1045** invalid/incompatible contract version: currently logged by `ProjectionViewOverrideRegistry`;
  invalid/major mismatch descriptors are ignored, minor drift is accepted and logged at Information.
- **HFC1046** accessibility warning: currently reserved for Level-4 accessibility diagnostics; broader
  HFC1050-HFC1055 customization accessibility analyzer work is Story 6.4.
- **HFC2121** render fault: currently runtime-only host fault isolation with redacted logging,
  diagnostic sink publication, bounded retry/recovery, and no payload/raw-exception leakage.
**And** no final story/dev record may claim HFC1042-HFC1046 are all build-time SourceTools diagnostics
unless the dev agent adds a real compile-visible marker/analyzer path and default-lane tests proving it.

**AC4 - Produce the FC-CUST Level-4 full-view override contract artifact.**
**Given** the existing Level-4 implementation and the Story 6.1 / 6.2 FC-CUST records,
**When** this story completes,
**Then** `_bmad-output/contracts/fc-cust-level4-full-view-override-contract-2026-06-05.md` exists and records,
with source and test citations:
- registration contract: `AddViewOverride` generic overload, descriptor-list source path, descriptor shape,
  `ProjectionViewOverrideContractVersion.Current = 1_000_000`, self-registration of
  `IProjectionViewOverrideRegistry`, quickstart registry registration, and descriptor-source defensive copy;
- resolution semantics: exact-role winner, any-role fallback, generated/template fallback on no descriptor
  or invalid descriptor, duplicate hard-fail startup behavior, idempotent same-component re-registration,
  descriptor snapshot immutability, and descriptor-only singleton cache safety;
- render contract: generated L4 -> L2 -> default precedence, per-render `ProjectionViewContext`, safe
  generated delegates, `DefaultBody` recursion boundary, `FcProjectionViewOverrideHost`, HFC2121 fault
  isolation, no payload/raw-exception logging, tenant/user hash-only correlation, and bounded recovery;
- diagnostic disposition for HFC1042-HFC1046 plus HFC2121 with phase/severity/fallback behavior and no
  vague "build reports" language;
- non-goals: Level-2 marker diagnostics except as precedence source, Level-3 field-slot behavior except
  delegated composition, HFC1050-HFC1055 accessibility-safety analyzer changes (Story 6.4), MCP resource
  security/schema negotiation changes, schema fingerprint changes, package/version bumps, generated output
  path changes, and broad docs-site rewrites.

**AC5 - Behavior is unchanged or minimally corrected, with evidence reconciled.**
**Given** the Level-4 surface is already implemented,
**When** this story completes,
**Then** existing Level-4 tests still pass and any new tests close real gaps rather than duplicate coverage,
**And** `.verified.txt`, pact, and owned `PublicAPI*.Shipped.txt` baselines remain byte-for-byte unchanged
unless an intentional contract change is made and explained,
**And** `CanonicalSchemaMaterial`, schema fingerprints, MCP projection URI/security behavior, package
versions, EventStore boundaries, and `IStorageService.SetAsync` tripwire behavior are untouched,
**And** the File List is reconciled against `git diff --name-only` before review promotion.

## Tasks / Subtasks

- [x] **Task 1 - Re-audit the live Level-4 implementation against AC1 (AC: #1)**
  - [x] Confirm public Contracts surface: `ProjectionViewOverrideDescriptor`,
        `ProjectionViewOverrideContractVersion`, `ProjectionViewContext<TProjection>`, and
        `IProjectionViewOverrideRegistry`.
  - [x] Confirm Shell registration/runtime: `ProjectionViewOverrideServiceCollectionExtensions.AddViewOverride`,
        `ProjectionViewOverrideDescriptorSource`, `ProjectionViewOverrideRegistry`, quickstart registry
        registration, and Counter sample registration.
  - [x] Confirm generated render wiring: `RazorEmitter` injects `IProjectionViewOverrideRegistry`, resolves
        Level 4 before Level 2, constructs fresh `ProjectionViewContext<TProjection>`, and hosts replacements
        through `FcProjectionViewOverrideHost<TProjection>`.
  - [x] Confirm runtime host behavior: replacement receives `Context`; errors render the diagnostic panel;
        replacement faults do not take down shell/navigation/sibling surfaces.

- [x] **Task 2 - Pin Level-4 precedence over Level-2 and safe delegation (AC: #2)**
  - [x] Re-run or add a focused default-lane test proving a registered Level-4 replacement wins when a
        Level-2 template is also registered for the same projection/role.
  - [x] Verify existing Counter evidence: `CounterProjectionView_Level4Replacement_RendersInsideFrameworkEnvelope_AndUsesSafeFieldDelegates`
        proves Level 4 replaces the generated DataGrid body while preserving the framework-owned envelope
        and allowing explicit `FieldRenderer` delegation to Level 3/default field rendering.
  - [x] If the L4-over-L2 precedence test is missing, add the smallest default-lane pin beside
        `CounterStoryVerificationTests`, using existing sample components where possible.
  - [x] Record `Context.DefaultBody` semantics: it is the safe generated-body escape hatch and must bypass
        the active Level-4 descriptor; direct re-mount of the generated view is unsupported because it
        re-enters the registry.

- [x] **Task 3 - Reconcile HFC1042-HFC1046 and HFC2121 honestly (AC: #3)**
  - [x] Create a diagnostic disposition table naming each current source call site, phase, severity,
        fallback behavior, and proving test for HFC1042, HFC1043, HFC1044, HFC1045, HFC1046, and HFC2121.
  - [x] If adding build-time SourceTools/analyzer emission for Level-4 registrations, keep it narrow:
        no broad third-party analyzer package, no `CompilationProvider` drift coupling, no `ISymbol` escaping
        parse/analysis state, and default-lane tests proving build diagnostics.
  - [x] If not adding build-time emission, explicitly record runtime/startup/reserved disposition in the
        contract and add an open follow-up for build-time validation/catalog reconciliation rather than
        marking the diagnostic catalog as already satisfied.
  - [x] Check `docs/diagnostics/HFC1042.md` through `HFC1046.md`, `diagnostic-registry.json`,
        `AnalyzerReleases.Unshipped.md`, and `_bmad-output/project-docs/api-contracts.md` for phase wording
        that could mislead adopters; update only story-owned docs, and record cross-story catalog seams as
        follow-ups when ownership belongs to Story 9-4/9-5 or Story 6.4.

- [x] **Task 4 - Produce the FC-CUST Level-4 contract artifact (AC: #4)**
  - [x] Create `_bmad-output/contracts/fc-cust-level4-full-view-override-contract-2026-06-05.md`.
  - [x] Include registration, resolution, render, diagnostic, cache-safety, recursion-boundary, accessibility
        boundary, and non-goal sections listed in AC4.
  - [x] Cross-reference Story 6.1's FC-CUST precedence contract and Story 6.2's Level-3 contract:
        Level 4 -> Level 2 -> generated default, with Level 3 composing only through explicit generated
        delegates inside whichever body renders.
  - [x] Mark every open item with owner, reason, risk, and follow-up story; do not leave unowned
        "needs review" text.

- [x] **Task 5 - Add only gap-closing tests (AC: #1, #2, #3, #5)**
  - [x] Prefer existing default-lane tests when they already prove the AC. Do not duplicate
        `ProjectionViewOverrideContractsTests`, `ProjectionViewOverrideServiceCollectionExtensionsTests`,
        `FcProjectionViewOverrideHostTests`, or Counter Level-4 generated-view assertions.
  - [x] Candidate gaps to verify before adding tests: L4-over-L2 same projection/role precedence,
        `ProjectionViewOverrideDescriptorSource` defensive-copy/snapshot immutability, HFC1045 minor-drift
        acceptance logging, or docs/catalog phase drift around HFC1042-HFC1046.
  - [x] If SourceTools build diagnostics are added, include build-diagnostic tests for HFC1042-HFC1046 and
        preserve generated-output snapshots unless generator output intentionally changes.

- [x] **Task 6 - Verify no regression (AC: #5)**
  - [x] `dotnet build Hexalith.FrontComposer.slnx -c Release` with 0 warnings / 0 errors.
  - [x] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
        If local VSTest is socket-blocked with `SocketException (13): Permission denied`, record that CI
        remains the solution-level gate and use the established xUnit v3 in-process fallback for focused evidence.
  - [x] Focused lanes: Contracts `ProjectionViewOverrideContractsTests`; Shell
        `ProjectionViewOverrideServiceCollectionExtensionsTests`, `FcProjectionViewOverrideHostTests`,
        `CounterStoryVerificationTests` Level-4 methods; SourceTools emitter snapshot tests only if generator
        output is touched; diagnostic descriptor/catalog tests only if HFC1042-HFC1046 metadata is touched.
  - [x] Confirm `.verified.txt`, pacts, and owned `PublicAPI*.Shipped.txt` are unchanged unless the story
        intentionally owns and documents the change.

- [x] **Task 7 - Record-keeping and File List reconciliation (AC: #5)**
  - [x] Record exact disposition in the Dev Agent Record: confirm-and-pin vs source change, diagnostic phase
        decision, tests run, failure counts, and any local sandbox caveats.
  - [x] Run `git diff --name-only` before review and reconcile every story-owned changed file into the File
        List. Keep pre-existing unrelated `_bmad-output/story-automator/orchestration-1-20260604-140358.md`
        changes out of this story File List unless this story edits it.

## Dev Notes

### Previous Story Intelligence

- Story 6.1 produced `_bmad-output/contracts/fc-cust-override-resolution-and-level2-template-contract-2026-06-05.md`
  and confirmed the render order: **Level 4 full-view override -> Level 2 template -> generated default body**.
  Story 6.3 must build on that record, not re-decide precedence.
- Story 6.2 produced `_bmad-output/contracts/fc-cust-level3-field-slot-contract-2026-06-05.md` and confirmed
  Level 3 slots compose inside whichever body renders through generated delegates. For Level 4, this means
  slots appear only when the replacement calls `Context.FieldRenderer(...)`, `RowRenderer`, `SectionRenderer`,
  or `DefaultBody`.
- Story 6.2 review caught a residual diagnostic-catalog seam for HFC1038-HFC1041. Apply the same discipline
  here: do not let HFC1042-HFC1046 registry/doc metadata imply build-time enforcement unless a build-time
  implementation and tests actually exist.
- Stories 6.1 and 6.2 both had File List drift caught during review. Treat changed-file reconciliation as
  a hard gate before promotion to review.

### Existing Implementation Surface

| Slice | Anchors | Notes |
|---|---|---|
| Contracts | `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewOverrideDescriptor.cs`; `ProjectionViewOverrideContractVersion.cs`; `ProjectionViewContext.cs`; `IProjectionViewOverrideRegistry.cs` | Descriptor metadata only; context is per-render and exposes projection metadata, current items, render context, lifecycle state, labels, default body, and generated section/row/field delegates. Net10/Blazor-only context is under `#if NET10_0_OR_GREATER`. |
| Registration | `src/Hexalith.FrontComposer.Shell/Extensions/ProjectionViewOverrideServiceCollectionExtensions.cs`; `ServiceCollectionExtensions.cs` | `AddViewOverride<TProjection,TComponent>` catches non-component mismatches at compile time and self-registers `IProjectionViewOverrideRegistry`; quickstart also registers the registry. |
| Registry | `src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs`; `ProjectionViewOverrideDescriptorSource.cs` | Exact-role then any-role fallback; invalid component/version ignored; duplicate different components hard-fail registry construction with HFC1044; same component/version re-registration is idempotent; descriptor source defensive-copies input. |
| Generated render wiring | `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` | Generated views inject `IProjectionViewOverrideRegistry`, build `defaultBody`, resolve Level 4 before Level 2, construct `ProjectionViewContext<TProjection>`, and host `FcProjectionViewOverrideHost<TProjection>`. |
| Runtime host | `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.cs` | Hosts replacement inside `ErrorBoundary`; passes explicit `Context`; redacts HFC2121 logs; publishes diagnostics once per fault episode; hashes tenant/user; recovers on descriptor/render-context changes, not item-list churn. |
| Sample | `samples/Counter/Counter.Web/Program.cs`; `samples/Counter/Counter.Web/Components/Replacements/CounterFullViewReplacement.razor` | Live reference registration replaces the Counter body; sample replacement keeps `aria-labelledby`, `aria-live`, and uses `Context.FieldRenderer` for Count/LastUpdated. |
| Diagnostics catalog | `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`; `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`; `docs/diagnostics/HFC1042.md`-`HFC1046.md`; `docs/diagnostics/HFC2121.md`; `docs/diagnostics/diagnostic-registry.json` | IDs/descriptors/docs exist, but HFC1042-HFC1046 are not all proven as build-time SourceTools emissions for Level-4 registrations. HFC2121 is runtime host fault isolation. |
| How-to docs | `docs/how-to/customization-gradient-cookbook.md` | Published docs show Level 4 as "full projection-body replacement" and warn that shell/lifecycle/authorization/telemetry/diagnostics/density/disposal remain framework-owned. Edit only if this story owns a real correction. |

### Existing Coverage

| Requirement | Status | Test |
|---|---|---|
| Contract version packing, descriptor metadata, context inputs, safe delegates | PROVEN | `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionViewOverrideContractsTests.cs` |
| `AddViewOverride` descriptor shape and self-registration | PROVEN | `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/ProjectionViewOverrideServiceCollectionExtensionsTests.cs` |
| Registry exact-role precedence, any-role fallback, duplicate startup hard-fail, idempotent re-registration, invalid component fallback | PROVEN | `ProjectionViewOverrideServiceCollectionExtensionsTests.cs` |
| Host passes context, refreshes context across renders, isolates render faults, redacts payload/raw exception, publishes once, recovers on descriptor change, avoids log spam on items-only churn, handles null context | PROVEN | `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionViewOverrideHostTests.cs` |
| Generated Counter view uses Level 4 replacement in the framework envelope and delegates fields safely | PROVEN | `CounterStoryVerificationTests.CounterProjectionView_Level4Replacement_RendersInsideFrameworkEnvelope_AndUsesSafeFieldDelegates` |
| Invalid Counter Level-4 component logs HFC1043 and renders generated default | PROVEN | `CounterStoryVerificationTests.CounterProjectionView_Level4InvalidComponent_LogsHfc1043_AndRendersGeneratedDefault` |
| Generator output injects view-override registry and host | PROVEN by snapshots/string pins | `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CounterProjectionApprovalTests.cs` |
| HFC1042-HFC1046 IDs/descriptors/docs registered | PROVEN as catalog metadata only | `DiagnosticDescriptorTests`, `DiagnosticRegistryTests`, `Hfc1026ReservationTests` |

### Known Gaps / Watch Items

- **L4-over-L2 same tuple precedence may need an explicit default-lane pin.** Existing code and Story 6.1
  contract prove the generator order, and Counter Level-4 proves replacement over generated body. Verify
  whether a test already proves Level 4 wins when a Level-2 template is registered simultaneously. If not,
  add that single pin.
- **Diagnostic phase mismatch is the main gap.** There is no `[ProjectionViewOverride]` marker attribute
  and no current SourceTools Level-4 marker parser/emitter equivalent to `[ProjectionTemplate]`. Treat
  HFC1042-HFC1046 build-time wording as unproven until default-lane build-diagnostic tests exist.
- **HFC1044 runtime behavior differs from older docs wording.** The live registry now hard-fails duplicate
  different-component registrations at construction rather than silently falling through to generated rendering.
  Preserve that stronger deterministic behavior and document it.
- **Accessibility analyzer is adjacent, not owned.** `CustomizationAccessibilityAnalyzer` and HFC1050-HFC1055
  are Story 6.4. This story may reference accessibility boundaries and HFC1046 disposition, but should not
  broaden static accessibility enforcement unless Product explicitly accepts scope expansion.
- **Historical labels are stale but useful.** Source/test comments call Level-4 work "Story 6-4". Under
  current planning, do not rename them for cosmetic reasons.
- **Latest external check (2026-06-05).** Microsoft Learn's ASP.NET Core Blazor .NET 10 dynamic component
  guidance supports rendering components by type with explicit parameters and warns against catch-all
  parameter surfaces; `FcProjectionViewOverrideHost` uses `RenderTreeBuilder.OpenComponent` plus an explicit
  `"Context"` attribute rather than a catch-all `DynamicComponent` bag. Microsoft Learn's Blazor error-boundary
  guidance supports custom `ErrorContent` and `Recover`, matching the host's narrow boundary and bounded
  recovery design.

### Project Structure Notes

- Contract artifact goes under `_bmad-output/contracts/`, not `docs/`, unless this story explicitly owns
  published diagnostic/doc corrections.
- Contracts surfaces stay under `src/Hexalith.FrontComposer.Contracts/Rendering/` and must remain
  netstandard2.0-clean except net10/Blazor types guarded with `#if NET10_0_OR_GREATER`.
- Runtime registry/host/DI changes stay under `src/Hexalith.FrontComposer.Shell/`.
- Generator precedence or context changes stay in `src/Hexalith.FrontComposer.SourceTools/Emitters/`.
  Do not hand-edit `obj/**/generated/HexalithFrontComposer/**`.
- Tests should stay beside existing lanes named above. New build-diagnostic tests, if any, belong in
  SourceTools tests and must be default-lane.

### Technical Constraints

- .NET 10, Roslyn 5.3.0, FluentUI v5 RC `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, xUnit v3 + Shouldly +
  NSubstitute + bUnit + Verify. Versions are centralized in `Directory.Packages.props`; do not bump them.
- `TreatWarningsAsErrors=true`; file-scoped namespaces; Allman braces; no copyright headers; no new
  third-party analyzer packages; no `.sln`; use `.slnx`.
- `ConfigureAwait(false)` on awaited calls outside Blazor renderer-context code.
- `SourceTools` references only Contracts. If adding compile-time Level-4 validation, keep IR pure/equatable
  and do not let `ISymbol` escape parse/analysis state.
- Do not touch `CanonicalSchemaMaterial`, schema fingerprints, MCP projection/resource URI grammar,
  MCP fail-closed gates, package versions, EventStore boundaries, or generated-output-path compatibility.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 6.3: Level-4 full-view overrides] - story statement and Epic ACs.
- [Source: _bmad-output/implementation-artifacts/6-1-level-2-projectiontemplate-overrides.md] - prior-story precedence and evidence hygiene constraints.
- [Source: _bmad-output/contracts/fc-cust-override-resolution-and-level2-template-contract-2026-06-05.md] - FC-CUST override-resolution precedence record.
- [Source: _bmad-output/implementation-artifacts/6-2-level-3-field-slot-overrides.md] - previous-story diagnostic-phase discipline and Level-3 composition constraints.
- [Source: _bmad-output/contracts/fc-cust-level3-field-slot-contract-2026-06-05.md] - Level-3 contract and non-goals.
- [Source: _bmad-output/project-context.md] - project rules, stack versions, generator/IR invariants, testing rules, and docs-vs-output rule.
- [Source: _bmad-output/project-docs/architecture.md#Runtime composition (Shell)] - runtime composition and Levels 2-4 customization statement.
- [Source: src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewOverrideDescriptor.cs] - descriptor metadata-only contract.
- [Source: src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewContext.cs] - per-render Level-4 context shape.
- [Source: src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionViewOverrideRegistry.cs] - resolution and cache-safety contract.
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/ProjectionViewOverrideServiceCollectionExtensions.cs] - public `AddViewOverride` seam.
- [Source: src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs] - resolution, HFC1043-HFC1045 logging, duplicate behavior.
- [Source: src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideDescriptorSource.cs] - descriptor-source defensive copy.
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs] - generated L4 -> L2 -> default precedence and context construction.
- [Source: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.cs] - runtime host, fallback, HFC2121 isolation.
- [Source: samples/Counter/Counter.Web/Program.cs] - live `AddViewOverride` sample registration.
- [Source: samples/Counter/Counter.Web/Components/Replacements/CounterFullViewReplacement.razor] - live accessible replacement component.
- [Source: tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionViewOverrideContractsTests.cs] - contract pins.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Extensions/ProjectionViewOverrideServiceCollectionExtensionsTests.cs] - registration/registry pins.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionViewOverrideHostTests.cs] - host isolation pins.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs] - generated Counter Level-4 pins.
- [Source: docs/how-to/customization-gradient-cookbook.md#Level 4: full projection-body replacement] - adopter guidance and boundaries.
- [Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/dynamiccomponent?view=aspnetcore-10.0] - current Blazor dynamic-component parameter guidance.
- [Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-10.0] - current Blazor error-boundary recovery guidance.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-05: Create-story activation resolved with no prepend/append steps; loaded `_bmad-output/project-context.md`, config, sprint status, Epic 6 planning context, previous Story 6.2, FC-CUST contracts, relevant Level-4 source/test files, git history, and current Blazor guidance.
- 2026-06-05: Validated the story against the create-story checklist focus areas: brownfield reuse, diagnostic-phase honesty, implementation anchors, architecture constraints, test lanes, and File List reconciliation gate.
- 2026-06-05: Dev-story activation resolved with no prepend/append steps; persistent project context loaded; existing `baseline_commit` preserved.
- 2026-06-05: Re-audited Contracts, Shell registry/DI/runtime host, SourceTools Razor emitter, Counter sample, diagnostic docs/catalog, and Story 6.1/6.2 FC-CUST contracts.
- 2026-06-05: Added gap-closing pins for Level-4-over-Level-2 runtime precedence, `ProjectionViewOverrideDescriptorSource` defensive copy, and HFC1045 minor-drift acceptance logging.
- 2026-06-05: Produced `_bmad-output/contracts/fc-cust-level4-full-view-override-contract-2026-06-05.md` with diagnostic phase disposition table and a Story 9-4/9-5 / Story 6.4 follow-up for catalog/build-time reconciliation.
- 2026-06-05: Validation: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` passed with 0 warnings / 0 errors.
- 2026-06-05: Validation caveat: exact solution VSTest commands with the required default-lane filter aborted before test execution with `System.Net.Sockets.SocketException (13): Permission denied`; CI remains the authoritative solution-level VSTest gate.
- 2026-06-05: Focused xUnit v3 in-process fallback passed: Contracts `ProjectionViewOverrideContractsTests` 4/4; Shell `ProjectionViewOverrideServiceCollectionExtensionsTests` 8/8; Shell Counter Level-4 methods 3/3; Shell `FcProjectionViewOverrideHostTests` 7/7; SourceTools `CounterProjectionApprovalTests`, `DiagnosticDescriptorTests`, and `Hfc1026ReservationTests` 26/26.
- 2026-06-05: Broad `CounterStoryVerificationTests` class run intentionally not used as gate because it reproduced two pre-existing snapshot/culture baseline failures outside Story 6.3; focused Story 6.3 Counter methods passed 3/3.
- 2026-06-05: `git diff --name-only` reconciliation completed; pre-existing unrelated `_bmad-output/story-automator/orchestration-1-20260604-140358.md` change excluded from this story File List.
- 2026-06-05: QA-generate-e2e-tests added `tests/e2e/specs/level-4-full-view-overrides.spec.ts` and `test:fc-level4`; TypeScript and Release build passed. Browser execution was attempted but Kestrel socket binding is blocked in this sandbox with `System.Net.Sockets.SocketException (13): Permission denied`.

### Completion Notes List

- Confirm-and-pin story completed with one contract artifact and three gap-closing tests; no production source behavior changed.
- AC1 confirmed against live contracts, registration/runtime registry, generated render wiring, Counter sample, and runtime host behavior.
- AC2 pinned with new default-lane test `CounterProjectionView_Level4Replacement_WinsWhenLevel2TemplateAlsoRegistered`; existing Level-4 envelope/delegation test remains green.
- AC3 recorded honestly: HFC1042 and HFC1046 are reserved/catalog-only today; HFC1043-HFC1045 are registry/startup-runtime dispositions; HFC2121 is runtime render-fault isolation. No final record claims all HFC1042-HFC1046 are proven build-time diagnostics.
- AC4 contract artifact created with registration, resolution, render, diagnostic, cache-safety, recursion-boundary, accessibility-boundary, non-goal, and owned-follow-up sections.
- AC5 verified: Release solution build clean; focused in-process tests green; exact solution VSTest locally socket-blocked; `.verified.txt`, pact, and `PublicAPI*.Shipped.txt` baselines unchanged.
- QA automation added a browser-level Story 6.3 E2E pin for the Counter sample Level-4 full-view replacement. The spec covers real command submission, replacement-body ownership, Level-2/DataGrid non-rendering, and explicit Level-3/generated field delegation.

### File List

- `_bmad-output/contracts/fc-cust-level4-full-view-override-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/6-3-level-4-full-view-overrides.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/ProjectionViewOverrideServiceCollectionExtensionsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs`
- `tests/e2e/package.json`
- `tests/e2e/specs/level-4-full-view-overrides.spec.ts`

### Change Log

- 2026-06-05: Moved story to in-progress, preserved baseline commit, and audited Level-4 implementation against AC1/AC2.
- 2026-06-05: Added gap-closing tests for Level-4-over-Level-2 precedence, descriptor-source defensive copy, and HFC1045 minor drift acceptance.
- 2026-06-05: Created the FC-CUST Level-4 full-view override contract artifact with truthful diagnostic disposition and follow-up ownership.
- 2026-06-05: Ran Release build and focused xUnit v3 in-process fallback validation; recorded local VSTest socket caveat.
- 2026-06-05: Reconciled File List against git reality and promoted story to review.
- 2026-06-05: Added Story 6.3 browser E2E automation and BMAD test summary; local browser execution remains socket-blocked before host startup.
- 2026-06-05 — Story-automator adversarial review (auto-fix). Validated AC1-AC5 against live source, generated emitter, and git reality; re-ran the changed test lanes in-process (8/8 registry incl. 2 new pins, 3/3 Counter Level-4, 7/7 host) and re-built the dependency chain at 0/0. No File List drift, no production source change. No CRITICAL/HIGH/MEDIUM blockers; promoted story review → done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-05 (story-automator autonomous review)
**Outcome:** Approve. AC1-AC5 validated against live source, the generated emitter, the FC-CUST contract, and git reality. Zero production source changed. No CRITICAL/HIGH/MEDIUM blockers; no auto-fixes were required.

### Scope verified

- **AC1 (Level-4 supplies the full projection body inside the framework envelope):** PROVEN. Live Contracts (`ProjectionViewOverrideDescriptor`/`Context`/`ContractVersion`/`IProjectionViewOverrideRegistry`), Shell registration/registry/host, and the generated `RazorEmitter` wiring all confirm the replacement is hosted through `FcProjectionViewOverrideHost<TProjection>` with a fresh per-render `ProjectionViewContext<TProjection>` while shell/grid-envelope/lifecycle stay framework-owned. `CounterProjectionView_Level4Replacement_RendersInsideFrameworkEnvelope_AndUsesSafeFieldDelegates` green.
- **AC2 (deterministic L4 → L2 → default precedence + DefaultBody recursion boundary):** PROVEN. `RazorEmitter.cs` builds `defaultBody` *before* override lookup (line 1283), resolves Level-4 (`ProjectionViewOverrideRegistry.Resolve`, line 1308) before Level-2 (`ProjectionTemplateRegistry.Resolve`, line 1337), and passes the pre-captured `defaultBody` into the context so `Context.DefaultBody` cannot re-enter the registry. New pin `CounterProjectionView_Level4Replacement_WinsWhenLevel2TemplateAlsoRegistered` proves L4 wins when an L2 template is registered for the same tuple (green).
- **AC3 (truthful HFC1042-HFC1046 + HFC2121 disposition):** PROVEN. Contract records HFC1042/HFC1046 as reserved/catalog-only, HFC1043-HFC1045 as registry/startup-runtime, and HFC2121 as runtime host isolation. Verified against `ProjectionViewOverrideRegistry.cs` (HFC1043 invalid-component warning, HFC1044 duplicate hard-fail, HFC1045 minor-drift Information "Override accepted") and `CustomizationAccessibilityAnalyzer.cs` (HFC1050-HFC1055 actually fire on `AddViewOverride`, confirming HFC1046 is adjacent/Story-6.4). No record falsely claims build-time SourceTools emission.
- **AC4 (FC-CUST Level-4 contract artifact):** PROVEN. `_bmad-output/contracts/fc-cust-level4-full-view-override-contract-2026-06-05.md` exists with registration, resolution, render (incl. recursion boundary), diagnostic-disposition, cache-safety, accessibility-boundary, and non-goal coverage. Every cited test method was confirmed to exist and pass; the cross-story catalog seam is pre-recorded with a Story 9-4/9-5 + 6.4 follow-up owner.
- **AC5 (behavior unchanged, evidence reconciled):** PROVEN. `git status` shows only test, e2e, and `_bmad-output` files touched — zero production source, `.verified.txt`, PublicAPI `*.Shipped.txt`, or pact baseline change. File List matches git reality exactly (orchestration log correctly excluded). The three gap-closing tests claimed match the three added in the diff exactly.

### Findings

- 🟢 **LOW (observation, no change) — Contract section headings.** AC4's task subtask names "accessibility boundary" and "recursion-boundary" as sections; the contract folds them into Diagnostic Disposition / Non-Goals and Render Contract respectively. All required content is present and accurate — this is an authoring choice, not a gap.
- 🟢 **LOW (observation, no change) — Minor-drift directionality.** `CustomizationContractVersion.Compare` accepts any minor difference as `source-compatible` regardless of direction (an override built against a *newer* minor than the installed framework is also accepted, pinned by the new HFC1045 test). Pre-existing shared Levels 2-4 behavior, out of scope for a confirm-and-pin story; flagged for the diagnostics owner only (same observation as the Story 6.2 review).
- ℹ️ **Improvement over prior stories — File List reconciliation gate passed.** Stories 5.5, 6.1, and 6.2 each required an auto-fix for undocumented e2e/test-summary deliverables. Story 6.3 reconciled all eight owned files correctly and excluded the unrelated `orchestration-1-20260604-140358.md` — no drift to fix.

### Verification

- Re-built the dependency chain (`Hexalith.FrontComposer.Shell.Tests` → Contracts/SourceTools/Shell/Counter samples) in Release: **0 warnings / 0 errors**.
- Re-ran the changed test lanes in-process (VSTest socket-blocked in sandbox, per the established fallback): `ProjectionViewOverrideServiceCollectionExtensionsTests` **8/8** (incl. `DescriptorSource_DefensiveCopiesInputList` and `Registry_MinorContractVersionDrift_LogsHfc1045Information_AndDescriptorIsAccepted`); Counter Level-4 methods **3/3**; `FcProjectionViewOverrideHostTests` **7/7**. `npm run typecheck` on the e2e specs passes.
- `git diff` confirms no `.verified.txt`, PublicAPI `*.Shipped.txt`, or pact baseline change and no production source change.

_Reviewer: Jérôme Piquot on 2026-06-05_
