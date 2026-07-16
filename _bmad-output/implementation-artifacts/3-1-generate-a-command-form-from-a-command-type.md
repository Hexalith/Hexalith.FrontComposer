---
baseline_commit: c166167a26424ce56ece76c48cfa54470d828ebf
---

# Story 3.1: Generate a command form from a `[Command]` type

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> Brownfield reality - read this first. The command generation pipeline already exists in source:
> `FrontComposerGenerator` discovers `[Command]`, parses to `CommandModel`, transforms to form,
> Fluxor, renderer, registration, MCP descriptor, and emits the command artifacts with the `.Command`
> hint segment. This is a CONFIRM-AND-PIN story with targeted fixes for verified gaps, not a rewrite.
>
> The epic says "6-7 generated files" but the live generator currently emits seven non-page command
> artifacts, plus an eighth `CommandPage.g.razor.cs` when density is `FullPage`: `Form`, `Actions`,
> `LifecycleFeature`, `Registration`, `Renderer`, `LastUsedSubscriber`, `LifecycleBridge`, and optional
> `Page`. The dev agent must reconcile this wording honestly in code/tests/docs instead of preserving
> an incorrect count.
>
> Critical gap found during story creation: generated command forms currently emit
> `Guid.NewGuid().ToString()` for `correlationId`. Project rules require `messageId` and
> `correlationId` to be ULIDs through `IUlidFactory`. Story 3.1 must remove the direct generated GUID
> correlation allocation or record a blocking owner decision; do not wait for Story 3.3 to fix a known
> generated-form contract violation. Story 3.3 still owns the broader FC-CMD pending identity,
> uniqueness scope, `alreadyApplied`, and reconciliation contract.

## Story

As an adopter developer,
I want a `[Command]`-annotated type to generate a complete command form and registration,
so that operators get a working submit form with no hand-written UI.

## Acceptance Criteria

**AC1 - Valid `[Command]` types generate the complete command artifact set and registration.**
**Given** a top-level `[Command]` type with a public parameterless constructor and a public
`MessageId` property,
**When** the project is built,
**Then** the generator emits the canonical command artifact set using the `.Command` hint segment:
`CommandForm.g.razor.cs`, `CommandActions.g.cs`, `CommandLifecycleFeature.g.cs`,
`CommandRegistration.g.cs`, `CommandRenderer.g.razor.cs`, `CommandLastUsedSubscriber.g.cs`,
`CommandLifecycleBridge.g.cs`, and `CommandPage.g.razor.cs` only when density is `FullPage`,
**And** the generated registration makes the command discoverable through the domain registry without
hand-written UI wiring,
**And** tests/documentation explicitly reconcile the epic wording "6-7 generated files" with the live
seven-plus-optional-page contract.

**AC2 - Shape diagnostics prevent uncompilable command forms.**
**Given** a `[Command]` missing a public parameterless constructor,
**When** the generator runs,
**Then** `HFC1009` is reported as an error and no uncompilable form is emitted.
**Given** a `[Command]` missing `MessageId`,
**When** the generator runs,
**Then** `HFC1006` is reported and the diagnostic explains that `MessageId` is required for
end-to-end correlation.

**AC3 - Unsupported fields degrade through `FcFieldPlaceholder` with a diagnostic.**
**Given** a `[Command]` with an unsupported non-derivable field type,
**When** parsed and rendered,
**Then** the field maps to `FormFieldTypeCategory.Placeholder`, the generated form renders
`FcFieldPlaceholder`, and `HFC1002` is reported,
**And** the diagnostic copy must be accurate for command forms. If the current HFC1002 text still says
only projection-slot override, update it to describe the command customization path or document the
chosen shared wording in the diagnostic catalog.

**AC4 - Generated command submit forms comply with current lifecycle and ID rules.**
**Given** a generated form submission,
**When** the form allocates `correlationId`, dispatches lifecycle actions, registers pending command
state, and forwards lifecycle changes,
**Then** the generated source uses the existing `IUlidFactory` abstraction for generated
`correlationId` values, not direct `Guid.NewGuid()` calls,
**And** it preserves the existing action order and guards: `SubmittedAction` before dispatch,
accepted-result pending registration before `AcknowledgedAction`, `Syncing`/`Confirmed` callback
transitions, `RejectedAction` for rejection, and `ResetToIdleAction` for validation/cancellation.

**AC5 - Existing renderer, density, authorization, lifecycle, and pending-command behavior does not regress.**
**Given** Epic 2 already pinned command palette/navigation and the command runtime surface already has
renderer/lifecycle/pending-command tests,
**When** this story completes,
**Then** inline, compact-inline, full-page, authorization-gated, lifecycle-wrapper, pending-command,
and registry discovery tests still pass,
**And** no generated snapshots or `.verified.txt` files change unless the change is intentional and
explained in the Dev Agent Record.

## Tasks / Subtasks

- [x] **Task 1 - Re-audit the live command generation pipeline before editing (AC: #1, #5)**
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` command output block
        and confirm the exact emitted hint names and conditional `CommandPage` behavior.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs`,
        `Transforms/CommandFormTransform.cs`, `Transforms/FormFieldModel.cs`,
        `Transforms/CommandFluxorTransform.cs`, `Transforms/CommandRendererTransform.cs`, and
        `Transforms/RegistrationModelTransform.cs` before changing generator behavior.
  - [x] Read command emitters before modifying output:
        `CommandFormEmitter.cs`, `CommandFluxorActionsEmitter.cs`,
        `CommandFluxorFeatureEmitter.cs`, `CommandRendererEmitter.cs`, `CommandPageEmitter.cs`,
        `LastUsedSubscriberEmitter.cs`, `CommandLifecycleBridgeEmitter.cs`, and
        `RegistrationEmitter.cs`.
  - [x] Confirm whether the story needs source changes or only tests/docs plus the direct GUID fix.
        Do not rewrite the parser/transform/emitter pipeline if the audit proves the behavior exists.

- [x] **Task 2 - Pin the generated artifact inventory end-to-end (AC: #1)**
  - [x] Add or update a generator integration test that runs `FrontComposerGenerator` against one
        inline command and one full-page command and asserts the generated tree file names exactly.
        Expected non-page command set is seven files; expected full-page set is eight files including
        `CommandPage.g.razor.cs`.
  - [x] Assert every command artifact uses the `.Command` hint segment so command/projection dual
        annotation cannot collide.
  - [x] Assert generated registration exposes the command to the registry path used by
        `AddHexalithDomain<TMarker>()`; reuse existing `RegistrationEmitterTests` or
        `FrontComposerRegistryTests` patterns rather than inventing a second registry abstraction.
  - [x] Update `_bmad-output/project-docs/api-contracts.md` or a focused contract artifact only if
        the live artifact count wording is wrong; do not edit published `docs/` unless the docs gate
        requires a user-facing correction.

- [x] **Task 3 - Confirm diagnostic gates and unsupported-field behavior (AC: #2, #3)**
  - [x] Verify parser tests already cover `HFC1006`, `HFC1009`, and command unsupported-field
        `HFC1002`; add missing generator-level tests if parser-only coverage is insufficient.
  - [x] Verify `AttributeParser.ParsePropertyForCommand(...)` reports `HFC1002` for unsupported
        command fields and sets `PropertyModel.IsUnsupported`.
  - [x] Verify `CommandFormTransform` maps unsupported command fields to
        `FormFieldTypeCategory.Placeholder`.
  - [x] Add or update emitter/rendered tests proving the generated command form renders
        `FcFieldPlaceholder` with the field name/type and keeps the rest of the form usable.
  - [x] Check `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`,
        `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`,
        `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`, and
        `_bmad-output/project-docs/api-contracts.md` for HFC1002 wording. Fix command-inaccurate copy
        if it would mislead adopter developers.

- [x] **Task 4 - Replace generated direct GUID correlation allocation (AC: #4)**
  - [x] In `CommandFormEmitter.cs`, replace generated `Guid.NewGuid().ToString()` correlation
        allocation with generated injection/use of `IUlidFactory.NewUlid()`. Preserve existing
        service lifetimes and the existing Shell registration of `IUlidFactory`.
  - [x] Update `CommandFormEmitterTests` and any `.verified.txt` snapshots that intentionally pin
        the generated source. The expected generated source must no longer contain direct
        `Guid.NewGuid()` for command `correlationId`.
  - [x] Keep Story 3.3 scope intact: do not decide pending identity uniqueness, `alreadyApplied`,
        status endpoint reconciliation, or FC-CMD contract text here.
  - [x] If any direct GUID remains in generated command form output, record it as a blocker with
        owner/date in the Dev Agent Record; do not mark AC4 complete.

- [x] **Task 5 - Preserve lifecycle, pending-command, and renderer behavior (AC: #4, #5)**
  - [x] Confirm `CommandFormEmitter` still ensures `LifecycleBridgeRegistry.Ensure<TBridge>()` and
        `LastUsedSubscriberRegistry.Ensure<TSubscriber>()` before `SubmittedAction`.
  - [x] Confirm accepted command results still call `PendingCommandState.Register(...)` before
        `AcknowledgedAction`, and invalid/conflicting pending registration still skips duplicate
        acknowledgement as currently implemented.
  - [x] Re-run or add focused tests around:
        `CommandLifecycleBridgeIntegrationTest`,
        `LifecycleBridgeEmitterTests`,
        `CommandRendererInlineTests`,
        `CommandRendererCompactInlineTests`,
        `CommandRendererFullPageTests`,
        `FcLifecycleWrapperTests`,
        `PendingCommandStateServiceTests`, and
        `PendingCommandOutcomeResolverTests`.
  - [x] Do not wire `FcNewItemIndicator` producer from projection nudges. Epic 2 established that
        row identity is absent from the read-path nudge; producer wiring belongs to the command
        outcome path in Epic 3 follow-up stories.

- [x] **Task 6 - Verify build and record evidence honestly (AC: #1-#5)**
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release` and require 0 warnings / 0 errors
        under `TreatWarningsAsErrors`.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
        If local VSTest is still socket-blocked with `SocketException (13): Permission denied`, use
        the existing xUnit v3 in-process fallback pattern and record CI as the authoritative
        solution-level VSTest gate.
  - [x] Run focused SourceTools and Shell lanes for changed areas. At minimum include parser,
        transform, emitter, generated renderer, lifecycle bridge, pending-command, and registry tests.
  - [x] Check `git diff --name-only -- '*.verified.txt'` and record whether snapshots changed.
  - [x] Keep the File List complete, including story file, sprint status, generated snapshots, and
        any docs/test files changed by QA automation.

## Dev Notes

### Existing command pipeline anchors

- `FrontComposerGenerator` already uses `ForAttributeWithMetadataName` for
  `Hexalith.FrontComposer.Contracts.Attributes.CommandAttribute`, collects `CommandParseResult`, emits
  a compilation-level MCP manifest when commands/projections exist, and emits per-command sources in
  the command `RegisterSourceOutput` block. [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs]
- Current generated command artifact set is:
  `CommandForm.g.razor.cs`, `CommandActions.g.cs`, `CommandLifecycleFeature.g.cs`,
  `CommandRegistration.g.cs`, `CommandRenderer.g.razor.cs`, `CommandLastUsedSubscriber.g.cs`,
  `CommandLifecycleBridge.g.cs`, plus `CommandPage.g.razor.cs` when density is `FullPage`.
  [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs]
- `CommandParser` enforces top-level non-generic commands, public parameterless constructor
  (`HFC1009`), `MessageId` presence (`HFC1006`), non-derivable property writability (`HFC1016`),
  property-count limits (`HFC1007`/`HFC1011`), `[Destructive]`, and `[RequiresPolicy]`.
  [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs]
- `AttributeParser.ParsePropertyForCommand(...)` currently shares the `HFC1002` unsupported-field
  path, including `[Flags]` command enums routed to placeholder through `HFC1008` plus unsupported
  metadata. Verify the diagnostic text before declaring AC3 complete. [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs]
- `CommandFormTransform` excludes `model.NonDerivableProperties` only, maps supported primitive
  categories to form fields, maps unsupported fields to `Placeholder`, strips trailing "Command" from
  button labels, and preserves explicit display labels. [Source: src/Hexalith.FrontComposer.SourceTools/Transforms/CommandFormTransform.cs]

### Current tests to reuse

- Parser coverage exists in `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs`
  for `HFC1006` and unsupported command fields; verify `HFC1009` coverage and add if missing.
- Transform coverage exists in `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/CommandFormTransformTests.cs`
  for field categories, derivable-property exclusion, placeholder mapping, and labels.
- Emitter coverage exists in `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`,
  `CommandRendererEmitterTests.cs`, `CommandFluxorEmitterTests.cs`, and
  `LifecycleBridgeEmitterTests.cs`. Snapshot changes must be intentional and recorded.
- Rendered command coverage exists under `tests/Hexalith.FrontComposer.Shell.Tests/Generated/` for
  inline, compact-inline, full-page, lifecycle wrapper, return-path, authorization, JS focus, and
  derivable-field behavior.
- Pending/lifecycle surfaces exist under `src/Hexalith.FrontComposer.Shell/State/PendingCommands/`,
  `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/`, and
  `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/`.

### Architecture and constraints

- SourceTools must remain a pure Roslyn incremental generator: parse -> pure IR -> transform -> emit.
  Do not let `ISymbol` escape parse-stage models; keep `EquatableArray<T>` and structural equality
  complete for cache correctness. [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Generated code is not hand-edited. Change the generator, transforms, or annotated test fixtures.
  Output path `obj/{Config}/{TFM}/generated/HexalithFrontComposer/` is public contract. [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Dependency direction stays downward to `Contracts`. `SourceTools` references only `Contracts`; do
  not pull Shell, FluentUI, or net10-only dependencies into SourceTools. [Source: _bmad-output/project-context.md#Code Quality & Style Rules]
- Generated forms may reference Shell runtime types in emitted source, but the generator project must
  not reference Shell. This is the existing pattern in `CommandFormEmitter` and projection emitters.
- Use the custom `FcFluentIcons` path where icons are involved. Do not add FluentUI icons NuGet.
- All package versions are centrally pinned; do not add `Version=` attributes to `.csproj` files and
  do not upgrade packages for this story. Local pins verified during story creation include FluentUI
  Blazor `5.0.0-rc.3-26138.1`, Fluxor.Blazor.Web `6.9.0`, xUnit v3 `3.2.2`, Verify.XunitV3
  `31.19.0`, Roslyn `5.3.0`, and NUlid `1.7.3`. NuGet/Microsoft Learn checks on 2026-06-04 confirm
  these pins are current enough for this story; the implementation must follow repo pins, not chase
  newer prereleases. [Source: Directory.Packages.props] [Source: global.json]
- `global.json` pins SDK `10.0.302` with `rollForward: latestPatch`; Microsoft Learn documents that
  `global.json` selects the SDK independently from target runtime and `rollForward` controls acceptable
  SDK fallback. [Source: https://learn.microsoft.com/en-us/dotnet/core/tools/global-json]

### Previous story and retro intelligence

- Epic 2 completed the read-only projection MVP and FC-TBL contract. Do not reopen DataGrid behavior
  while doing command form work. [Source: _bmad-output/implementation-artifacts/epic-2-retro-2026-06-04.md#Next Epic Preparation]
- Story 2.6 accepted deferral of row-level `FcNewItemIndicator` producer wiring because the read-path
  nudge carries only projection type and tenant id. Epic 3 must use the command outcome path for row
  identity; Story 3.1 should not add row identity to the SignalR nudge seam. [Source: _bmad-output/implementation-artifacts/2-6-live-projection-updates-with-reconnect-reconciliation.md]
- Story 2.8 confirmed the FC-TBL table API and created a focused Shell FC-TBL public API baseline.
  Command work must keep FC-TBL tests and snapshots unchanged unless intentionally impacted, which is
  not expected here. [Source: _bmad-output/implementation-artifacts/2-8-confirm-the-fc-tbl-table-api-contract.md]
- Epic 2 retro explicitly says current planning keys override stale source comments that refer to
  command lifecycle as Story 5-5. Normalize new command-lifecycle references to Epic 3 story keys.
  [Source: _bmad-output/implementation-artifacts/epic-2-retro-2026-06-04.md#Action Items]

### Project Structure Notes

- Expected production files, if source changes are needed:
  `src/Hexalith.FrontComposer.SourceTools/Parsing/`,
  `src/Hexalith.FrontComposer.SourceTools/Transforms/`,
  `src/Hexalith.FrontComposer.SourceTools/Emitters/`,
  `src/Hexalith.FrontComposer.SourceTools/Diagnostics/`,
  and possibly `src/Hexalith.FrontComposer.Contracts/Diagnostics/`.
- Expected test files:
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/`,
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/`,
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/`,
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/`,
  and `tests/Hexalith.FrontComposer.Shell.Tests/Generated/`.
- Documentation updates, if needed, belong under `_bmad-output/project-docs/` or a focused
  `_bmad-output/contracts/` artifact. Do not use published `docs/` as scratch space.
- Leave submodules alone. `Hexalith.EventStore/` and `Hexalith.Tenants/` project-context files were
  loaded as persistent facts, but this story should not modify submodule files.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.1] - story statement and initial ACs.
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 3] - command authoring/lifecycle scope.
- [Source: _bmad-output/project-context.md#Technology Stack & Versions] - pinned versions and package rules.
- [Source: _bmad-output/project-context.md#Source-Generator Rules] - generator invariants.
- [Source: _bmad-output/project-context.md#Testing Rules] - solution-level test command and Verify discipline.
- [Source: _bmad-output/project-docs/architecture.md#The generation pipeline] - generated command artifact contract.
- [Source: _bmad-output/project-docs/api-contracts.md#Source-generator contract] - attribute, output, density, diagnostic catalog.
- [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs] - live command generation output.
- [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs] - command parser diagnostics and derivable-field classification.
- [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs] - unsupported field diagnostic path.
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs] - generated command form behavior and direct GUID gap.
- [Source: src/Hexalith.FrontComposer.Contracts/Lifecycle/IUlidFactory.cs] - ULID abstraction.
- [Source: src/Hexalith.FrontComposer.Shell/Services/Lifecycle/UlidFactory.cs] - Shell ULID implementation.
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs] - parser diagnostic coverage.
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs] - form emitter coverage and snapshots.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererTestFixtures.cs] - generated command fixtures.
- [Source: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components] - FluentUI Blazor package latest prerelease pin check.
- [Source: https://www.nuget.org/packages/Fluxor.Blazor.Web/6.9.0] - Fluxor Blazor net10 compatibility check.
- [Source: https://www.nuget.org/packages/NUlid] - NUlid package and ULID characteristics.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-04: Audited command generator/parser/transform/emitter path before edits. Confirmed the
  live command output block already emits seven non-page `.Command` artifacts and only emits
  `CommandPage.g.razor.cs` for `FullPage` density.
- 2026-06-04: Release build: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`
  passed with 0 warnings and 0 errors.
- 2026-06-04: Required solution VSTest command with default trait exclusions failed before test
  execution because VSTest could not open its local communication socket:
  `System.Net.Sockets.SocketException (13): Permission denied` at `TcpListener` startup. CI remains
  the authoritative VSTest gate for the solution-level command.
- 2026-06-04: xUnit v3 in-process fallback evidence:
  SourceTools focused command lane 143/143 passed; `CommandLifecycleBridgeIntegrationTest` 1/1
  passed; Shell focused command renderer/lifecycle/pending lane 68/68 passed after excluding the
  known pre-existing `CommandRendererFullPageTests.Renderer_FullPage_UsesQueryFallbacksWhenPageContextIsEmpty`
  baseline failure.
- 2026-06-04: xUnit v3 in-process default-lane evidence with trait exclusions: Contracts 159/159
  passed, MCP 291/291 passed, SourceTools 970 total / 3 known non-story failures, CLI 41 total / 3
  known non-story failures, Testing 11 total / 2 known package-boundary failures, Shell 1760 total /
  9 known non-story failures, Bench 0 tests after exclusions.
- 2026-06-04: `git diff --name-only -- '*.verified.txt'` shows only the two intentional command form
  snapshots updated for the generated `IUlidFactory` injection and `UlidFactory.NewUlid()` call.

### Implementation Plan

- Keep the existing parse -> transform -> emit pipeline intact.
- Add generator-level inventory and diagnostic pins around the current command artifact contract.
- Replace only generated command-form correlation allocation with the existing `IUlidFactory`
  abstraction, preserving lifecycle ordering and Story 3.3 scope.
- Use shared HFC1002 wording that is accurate for both projection and command generated UI.

### Completion Notes List

- Story context engine analysis completed - comprehensive developer guide created.
- Story generated in confirm-and-pin mode because the live command generator already emits the
  command form, Fluxor, renderer, registration, last-used subscriber, lifecycle bridge, and optional
  full-page route artifacts.
- Critical implementation guardrail recorded: generated command forms must stop allocating command
  `correlationId` with direct `Guid.NewGuid()` and use `IUlidFactory.NewUlid()` instead.
- Confirmed and pinned exact command generated artifact inventory: seven non-page `.Command` hint
  segment files and the optional eighth `CommandPage.g.razor.cs` for `FullPage` density.
- Added generator-level pins for command registry registration, HFC1009/HFC1006 shape diagnostics,
  and HFC1002 unsupported command fields rendering through `FcFieldPlaceholder`.
- Replaced generated command-form `correlationId` allocation with injected `IUlidFactory.NewUlid()`;
  no direct generated command-form `Guid.NewGuid().ToString()` allocation remains.
- Updated HFC1002 wording and `_bmad-output/project-docs/api-contracts.md` so unsupported field
  guidance and command artifact counts are accurate for command forms.
- Preserved lifecycle/pending behavior: lifecycle bridge and last-used subscriber are ensured before
  `SubmittedAction`; accepted pending registration remains before `AcknowledgedAction`; invalid or
  conflicting pending registrations still skip duplicate acknowledgement.
- Did not wire `FcNewItemIndicator` from projection nudges and did not decide Story 3.3 pending
  identity, `alreadyApplied`, status endpoint reconciliation, or FC-CMD contract scope.

### File List

- `_bmad-output/implementation-artifacts/3-1-generate-a-command-form-from-a-command-type.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/project-docs/api-contracts.md`
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs`
- `tests/e2e/specs/command-form-generation.spec.ts`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`

### Change Log

- 2026-06-04: Story 3.1 implemented in confirm-and-pin mode; command artifact inventory, command
  diagnostics, unsupported-field placeholder behavior, registry discovery, lifecycle ordering, and
  generated ULID correlation allocation are pinned. Status moved to review.
- 2026-06-04: Senior Developer Review (AI) completed. 0 Critical / 0 High; 2 Medium File List
  completeness gaps auto-fixed (added `tests/e2e/specs/command-form-generation.spec.ts` and
  `_bmad-output/implementation-artifacts/tests/test-summary.md`). Status moved to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot
**Date:** 2026-06-04
**Outcome:** Approve (after auto-fix of 2 Medium documentation gaps)

### Acceptance Criteria validation

- **AC1 (IMPLEMENTED):** Command artifact inventory pinned end-to-end. `GeneratorDriverTests.RunGenerators_CommandArtifactInventory_UsesCommandHintSegmentAndOptionalPage` asserts the seven non-page `.Command` artifacts (and the optional eighth `CommandPage.g.razor.cs` for FullPage density via `PlaceOrderCommand`). `RunGenerators_CommandRegistration_ExposesCommandToDomainRegistry` confirms registry discovery. `api-contracts.md` reconciles the "6-7 generated files" epic wording with the live 7-plus-optional-page contract.
- **AC2 (IMPLEMENTED):** `RunGenerators_CommandShapeDiagnostics_AreReportedAtGeneratorLevel` confirms `HFC1009` is reported at Error severity and `HFC1006` explains end-to-end correlation; the uncompilable form is not emitted.
- **AC3 (IMPLEMENTED):** `RunGenerators_UnsupportedCommandField_ReportsHfc1002AndEmitsPlaceholderForm` confirms placeholder mapping, `FcFieldPlaceholder` rendering, and command-accurate `HFC1002` copy (no stale `[ProjectionFieldSlot]`-only wording). Diagnostic text updated in `DiagnosticDescriptors.cs`, `AttributeParser.cs`, `AnalyzerReleases.Unshipped.md`, and `api-contracts.md`.
- **AC4 (IMPLEMENTED):** `CommandFormEmitter` now injects `IUlidFactory` and allocates `correlationId` via `UlidFactory.NewUlid()`; no direct generated `Guid.NewGuid().ToString()` remains. Verified type-safe: `NewUlid()` returns `string`, `CorrelationId` is "string, not Guid" (Decision D1) throughout, no downstream `Guid.Parse` on correlation. `IUlidFactory` is registered (`TryAddSingleton`) in Shell `ServiceCollectionExtensions`. Action order/guards preserved (`SubmittedAction` after bridge/subscriber ensure; pending registration before `AcknowledgedAction`; Syncing/Confirmed/Rejected/ResetToIdle transitions intact).
- **AC5 (IMPLEMENTED):** Build 0 warnings / 0 errors (Release, TWAE). SourceTools suite 973/976 — the 3 failures (`IdeParity` Linux case-sensitivity, `datagrid.md` docs contract, Story 1.12 ledger `deferred-work.md`) are pre-existing and unrelated to command generation. Only the two intentional `CommandFormEmitter*.verified.txt` snapshots changed (ULID injection + call), confirmed via `git diff -- '*.verified.txt'`.

### Findings

- **MEDIUM (fixed):** `tests/e2e/specs/command-form-generation.spec.ts` — new QA-automation test absent from File List. Verified valid against live code (anchors `.fc-command-form`/aria-labels, `fc-confirmed` testid, "Submitting…", "Invalid number format.", and sample `BatchIncrementCommand`/`IncrementCommand`/`ConfigureCounterCommand` all exist). Added to File List.
- **MEDIUM (fixed):** `_bmad-output/implementation-artifacts/tests/test-summary.md` modified but not listed. Added to File List.
- **LOW (cleared, no change):** `Emit_DoesNotLogModel` assertion rewritten. Investigated: the new check correctly enforces Decision D15 (no `Logger` line references `_model`); the previous `"_model,\n"` check passed only by `\r\n`/`\n` line-ending accident. The `_model` at the call site is the legitimate `CommandService` submission, not logging. No defect, no masking.

### Notes

- Story 3.3 scope correctly untouched (no pending-identity uniqueness, `alreadyApplied`, status reconciliation, or FC-CMD contract text decided here).
- `FcNewItemIndicator` producer wiring correctly not added (Epic 2 / Story 2.6 deferral respected).
- Story-automator harness bookkeeping files (`orchestration-*.md`, `preflight-selection-latest.json`) intentionally excluded from the File List as non-implementation artifacts.
