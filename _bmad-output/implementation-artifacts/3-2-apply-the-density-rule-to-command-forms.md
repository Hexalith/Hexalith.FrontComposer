---
baseline_commit: 8cfcc8033c9da19a3f8b05af7f16492fbf98b9f2
---

# Story 3.2: Apply the density rule to command forms

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> Brownfield reality - read this first. The density rule is already present in the live command
> pipeline: `CommandModel.ComputeDensity(int)` computes `Inline`, `CompactInline`, or `FullPage`;
> `CommandFormTransform` renders only `model.NonDerivableProperties`; `CommandRendererEmitter` maps
> density to the default render mode; and `FrontComposerGenerator` emits `CommandPage.g.razor.cs`
> only when density is `FullPage`. This is a confirm-and-pin story with targeted gap closure, not a
> rewrite of the parser/transform/emitter pipeline.
>
> Critical scope boundary: Story 3.3 owns the broader FC-CMD pending identity and correlation
> contract. Story 3.2 must prove derivable fields are excluded from form shape and generated output,
> but must not decide pending identity uniqueness, `alreadyApplied`, status reconciliation, or MCP
> command-tool derivable injection semantics beyond preserving the current generator contract.

## Story

As an operator,
I want command forms sized to their field count,
so that simple commands are inline and complex ones get a full page.

## Acceptance Criteria

**AC1 - Non-derivable field count selects the command density and page artifact exactly.**  
**Given** a `[Command]` type with a public parameterless constructor and `MessageId`,  
**When** the generator computes density from the command's non-derivable property count,  
**Then** `0` or `1` non-derivable fields produce `Inline`, `2`, `3`, or `4` produce `CompactInline`,
and `5` or more produce `FullPage`,  
**And** the generated artifact set includes `CommandPage.g.razor.cs` only for `FullPage`, never for
`Inline` or `CompactInline`.

**AC2 - Derivable fields are excluded from form shape and do not inflate density.**  
**Given** a command with derivable fields named `MessageId`, `CommandId`, `CorrelationId`,
`TenantId`, `UserId`, `Timestamp`, `CreatedAt`, `ModifiedAt`, or marked with `[DerivedFrom]`,  
**When** the parser, transforms, and emitters run,  
**Then** those properties are classified as derivable, excluded from generated form fields,
excluded from renderer density decisions, and not rendered as operator-editable inputs,  
**And** infrastructure-owned values continue to be filled by the existing submit/render/lifecycle
paths rather than by user form input.

**AC3 - Existing command render surfaces match density at runtime.**  
**Given** generated command renderers for inline, compact inline, and full-page densities,  
**When** rendered in the Shell test host,  
**Then** inline commands render as the button/popover path, compact inline commands render through
the expand-in-row card path, and full-page commands render through the routable full-page host with
breadcrumb/return behavior preserved.

**AC4 - Property-count diagnostics remain tied to non-derivable and total limits.**  
**Given** a command with more than `30` non-derivable properties,  
**When** parsed,  
**Then** `HFC1007` is reported as a warning.  
**Given** a command with more than `100` non-derivable properties,  
**When** parsed,  
**Then** `HFC1007` is reported as an error.  
**Given** a command with more than `200` total public properties,  
**When** parsed,  
**Then** `HFC1011` is reported as an error, even if many fields are derivable.

**AC5 - Story 3.1 command generation behavior does not regress.**  
**Given** Story 3.1 pinned the command artifact inventory, registration discovery, unsupported-field
placeholder behavior, lifecycle dispatch order, pending-command registration, and ULID
`correlationId` allocation,  
**When** this story completes,  
**Then** those tests and snapshots remain green,  
**And** any `.verified.txt` changes are intentional, minimal, and explained in the Dev Agent Record.

## Tasks / Subtasks

- [x] **Task 1 - Re-audit the live density path before editing (AC: #1-#5)**
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs` and verify
        `CommandModel.ComputeDensity(int)` still implements `<=1`, `2..4`, `>=5`.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs` and verify the
        well-known derivable-name set, `[DerivedFrom]` classifier, `HFC1007`, and `HFC1011`.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandFormTransform.cs` and
        `CommandRendererTransform.cs` to confirm only non-derivable properties feed fields and
        renderer density metadata.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` and confirm the
        conditional `CommandPage.g.razor.cs` emission remains gated by `CommandDensity.FullPage`.
  - [x] Read `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`,
        `CommandPageEmitter.cs`, and `CommandFormEmitter.cs` before changing output behavior.

- [x] **Task 2 - Pin exact density thresholds at parser/model and generator levels (AC: #1)**
  - [x] Add or update a focused `CommandModel.ComputeDensity` boundary test covering counts
        `0`, `1`, `2`, `4`, and `5`.
  - [x] Add or update generator integration fixtures for one-field, two-field, four-field, and
        five-field commands; assert the generated renderer source logs/sets the expected default
        density and that `CommandPage.g.razor.cs` appears only for the five-field command.
  - [x] Reuse `GeneratorDriverTests`, `CommandTestSources`, and `CommandRendererEmitterTests`
        patterns rather than creating a parallel generator test harness.
  - [x] Do not change the density thresholds unless an owner explicitly changes FR3; if a threshold
        mismatch is found, fix the implementation to match the spec rather than rewriting the story.

- [x] **Task 3 - Prove derivable fields do not affect fields, density, or rendered input (AC: #2)**
  - [x] Extend parser coverage so every well-known derivable field plus `[DerivedFrom]` is proven
        excluded from `NonDerivableProperties`.
  - [x] Add a generator-level test where a command has four user-editable fields plus multiple
        derivable fields; assert density remains `CompactInline` and no `CommandPage.g.razor.cs` is
        emitted.
  - [x] Add a generator-level test where a command has five user-editable fields plus derivable
        fields; assert density is `FullPage` and the page artifact is emitted.
  - [x] Add or update form-emitter/rendered coverage proving derivable properties are not emitted as
        editable labels/inputs while non-derivable fields still render in declaration order.
  - [x] Preserve the Story 3.1 `IUlidFactory.NewUlid()` generated `correlationId` path; do not
        reintroduce `Guid.NewGuid()` anywhere in generated command form output.

- [x] **Task 4 - Close diagnostic coverage for count limits (AC: #4)**
  - [x] Keep existing `HFC1007` warning and error tests for `CommandParser.NonDerivableWarningThreshold`
        and `NonDerivableErrorThreshold`.
  - [x] Add missing parser and generator-level coverage for `HFC1011` total-public-property hard
        limit, including a fixture that proves total count is not confused with density count.
  - [x] Confirm `DiagnosticDescriptors.CommandTooManyTotalProperties`, `FcDiagnosticIds`, and
        `AnalyzerReleases.Unshipped.md` remain consistent; update only if the live catalog is wrong.
  - [x] Do not add new diagnostics for density. This story pins existing `HFC1007`, `HFC1011`, and
        existing runtime `HFC1015` override warnings.

- [x] **Task 5 - Re-prove Shell runtime density behavior without reopening unrelated UX (AC: #3, #5)**
  - [x] Re-run or extend `RenderModeOverrideTests` for default inline, compact inline, and full-page
        renderer shape.
  - [x] Re-run or extend `CommandRendererInlineTests`, `CommandRendererCompactInlineTests`, and
        `CommandRendererFullPageTests` for button/popover, expand-in-row card, and full-page route
        behavior.
  - [x] Preserve full-page return-path and breadcrumb behavior from `CommandRendererFullPageTests`.
  - [x] Do not reopen FC-TBL, projection DataGrid density, command palette search, or row-level
        `FcNewItemIndicator` producer wiring. Epic 2 explicitly carries row identity producer work
        into current Epic 3 lifecycle/status stories, not this density story.

- [x] **Task 6 - Verify build/tests and record evidence honestly (AC: #1-#5)**
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release` and require 0 warnings / 0 errors.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
        If local VSTest is still socket-blocked with `SocketException (13): Permission denied`, use
        the existing xUnit v3 in-process fallback pattern and record CI as the authoritative
        solution-level VSTest gate.
  - [x] Run focused SourceTools and Shell lanes for changed areas. At minimum include
        `CommandParserTests`, `CommandFormTransformTests`, `CommandRendererEmitterTests`,
        `GeneratorDriverTests`, `RenderModeOverrideTests`, `CommandRendererInlineTests`,
        `CommandRendererCompactInlineTests`, and `CommandRendererFullPageTests`.
  - [x] Check `git diff --name-only -- '*.verified.txt'` and record whether snapshots changed.
  - [x] Keep the File List complete, including this story file, sprint status, generated snapshots,
        and any tests/docs changed by QA automation.

## Dev Notes

### Existing density pipeline anchors

- `CommandModel.ComputeDensity(int)` is the single density threshold function: non-derivable count
  `<=1` returns `Inline`, `<=4` returns `CompactInline`, and anything higher returns `FullPage`.
  `CommandModel` computes `Density` in its constructor from `nonDerivableProperties.Count`.
  [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs]
- `CommandParser` classifies derivable fields by exact well-known names:
  `MessageId`, `CommandId`, `CorrelationId`, `TenantId`, `UserId`, `Timestamp`, `CreatedAt`,
  `ModifiedAt`, plus properties carrying `[DerivedFrom]`. The parser also owns
  `NonDerivableWarningThreshold = 30`, `NonDerivableErrorThreshold = 100`, and
  `TotalPropertyHardLimit = 200`. [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs]
- `CommandFormTransform` iterates `model.NonDerivableProperties` only. If a derivable field appears
  in generated form output, the bug is upstream classification or an emitter/regression, not a reason
  to special-case field names in the form emitter. [Source: src/Hexalith.FrontComposer.SourceTools/Transforms/CommandFormTransform.cs]
- `CommandRendererTransform` passes `model.Density`, non-derivable property names, derivable property
  names, and the sanitized full-page route into `CommandRendererModel`. [Source: src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs]
- `FrontComposerGenerator` always emits the seven non-page command artifacts and emits
  `CommandPage.g.razor.cs` only when `result.Model.Density == CommandDensity.FullPage`.
  [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs]
- `CommandRendererEmitter` maps `CommandDensity` to `CommandRenderMode`, logs density in generated
  renderers, blocks incompatible inline overrides with `HFC1015`, and uses the field count to decide
  whether inline rendering can be used safely. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs]
- `CommandPageEmitter` is a route host only. Do not move density decisions into it; it should remain
  emitted only by the generator for full-page commands. [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandPageEmitter.cs]

### Current tests to reuse

- Parser coverage already proves common derivable classification, `[DerivedFrom]`, missing
  `MessageId`, unsupported fields, and `HFC1007` warning/error thresholds in
  `CommandParserTests`. Add the missing total-property `HFC1011` and exact density-boundary pins
  there or in an adjacent model test. [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs]
- `CommandFormTransformTests` already proves derivable fields are skipped and zero non-derivable
  fields produce an empty field array. Extend, do not duplicate, that coverage.
  [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/CommandFormTransformTests.cs]
- `GeneratorDriverTests` already pins command artifact inventory, command hint segments, generator
  diagnostics, unsupported-field placeholder output, and the full-page page artifact for a five-field
  command. Add exact `1/2/4/5` threshold fixtures here if generator-level pins are missing.
  [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs]
- `CommandRendererEmitterTests` already has approval snapshots for one-field inline, two-field
  compact inline, four-field compact boundary, and five-field full-page boundary. Snapshot churn is
  expected only when an emitted contract intentionally changes. [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs]
- Shell rendered fixtures already include `ZeroFieldInlineCommand`, `OneFieldInlineCommand`,
  `TwoFieldCompactCommand`, `CompactCommandWithDerivableField`, and `FiveFieldFullPageCommand`.
  Reuse them for bUnit runtime density proof. [Source: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererTestFixtures.cs]
- Runtime pins already exist in `RenderModeOverrideTests`, `CommandRendererInlineTests`,
  `CommandRendererCompactInlineTests`, and `CommandRendererFullPageTests`. Prefer extending these over
  adding a new test namespace. [Source: tests/Hexalith.FrontComposer.Shell.Tests/Generated/RenderModeOverrideTests.cs]

### Architecture and constraints

- SourceTools must remain a pure Roslyn incremental generator: parse -> pure IR -> transform -> emit.
  No `ISymbol` may escape parse-stage models; keep `EquatableArray<T>` and structural equality
  complete for cache correctness. [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Generated code is not hand-edited. Change the generator, parser, transforms, emitters, or test
  fixtures. Output under `obj/{Config}/{TFM}/generated/HexalithFrontComposer/` is a public contract.
  [Source: _bmad-output/project-context.md#Source-Generator Rules]
- Dependency direction stays down to `Contracts`. `SourceTools` references only `Contracts`; do not
  pull Shell, FluentUI, or net10-only dependencies into `SourceTools`. Generated source may reference
  Shell runtime types by fully qualified name because that is the existing emitter pattern.
  [Source: _bmad-output/project-context.md#Code Quality & Style Rules]
- Do not add or upgrade packages for this story. Relevant repo pins are `.NET SDK 10.0.300`,
  Roslyn `5.3.0`, FluentUI Blazor `5.0.0-rc.3-26138.1`, Fluxor.Blazor.Web `6.9.0`, NUlid `1.7.3`,
  xUnit v3 `3.2.2`, and Verify.XunitV3 `31.19.0`. [Source: Directory.Packages.props] [Source: global.json]
- Official NuGet checks on 2026-06-04 confirm the story should follow repo pins rather than chase
  unrelated upgrades: Fluxor.Blazor.Web `6.9.0` lists net10.0 compatibility, NUlid `1.7.3` remains
  the pinned ULID package, Roslyn `5.3.0` is published, and the FluentUI package page lists the
  repo-pinned v5 RC alongside the current 4.x stable line. [Source: https://www.nuget.org/packages/Fluxor.Blazor.Web]
  [Source: https://www.nuget.org/packages/NUlid] [Source: https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp]
  [Source: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components]
- Testing rules are solution-level by default and require `DiffEngine_Disabled=true` for Verify
  snapshot tests. If the local VSTest socket restriction recurs, use the established xUnit v3
  in-process fallback and record the limitation. [Source: _bmad-output/project-context.md#Testing Rules]

### Previous story and retro intelligence

- Story 3.1 confirmed the live command generator emits seven non-page command artifacts plus an
  optional `CommandPage` for `FullPage`, fixed generated `correlationId` allocation to use
  `IUlidFactory.NewUlid()`, and pinned command registration/diagnostics/placeholder/lifecycle
  behavior. Story 3.2 must preserve those contracts. [Source: _bmad-output/implementation-artifacts/3-1-generate-a-command-form-from-a-command-type.md]
- The latest commit `8cfcc80 feat(story-3.1): Generate a command form from a Command type` touched
  `CommandFormEmitter`, diagnostics, `AttributeParser`, generator tests, command form snapshots,
  `_bmad-output/project-docs/api-contracts.md`, and e2e command-form generation evidence. Treat
  those changes as the current baseline rather than undoing them. [Source: git log/show 2026-06-04]
- Epic 2 retro says current planning keys override stale comments that refer to command lifecycle as
  Story 5-5. New command-lifecycle follow-up references should use current Epic 3 story keys:
  Story 3.3 for FC-CMD identity/correlation and Story 3.5 for status endpoint / pending-command
  resolution. [Source: _bmad-output/implementation-artifacts/epic-2-retro-2026-06-04.md#Next Epic Preparation]
- Story 2.6 accepted deferral of row-level `FcNewItemIndicator` producer wiring because the live
  projection nudge carries only projection type and tenant id. Density work must not add row identity
  to the projection nudge seam. [Source: _bmad-output/implementation-artifacts/2-6-live-projection-updates-with-reconnect-reconciliation.md]
- Story 2.8 confirmed FC-TBL as stable. Do not reopen DataGrid or public FC-TBL API baselines while
  pinning command density. [Source: _bmad-output/implementation-artifacts/2-8-confirm-the-fc-tbl-table-api-contract.md]

### Project Structure Notes

- Expected production files if source changes are genuinely needed:
  `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs`,
  `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs`,
  `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandFormTransform.cs`,
  `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs`,
  `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`,
  `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandPageEmitter.cs`, and
  `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`.
- Expected test files:
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs`,
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/CommandTestSources.cs`,
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/CommandFormTransformTests.cs`,
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs`,
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs`, and
  `tests/Hexalith.FrontComposer.Shell.Tests/Generated/*CommandRenderer*Tests.cs`.
- Documentation updates, if needed, belong under `_bmad-output/project-docs/` or a focused
  `_bmad-output/contracts/` artifact. Do not use published `docs/` as scratch space.
- Leave submodules alone. This story should not modify `Hexalith.EventStore/`, `Hexalith.Tenants/`,
  or `Hexalith.Commons/`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.2] - story statement and initial ACs.
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 3] - command authoring/lifecycle scope.
- [Source: _bmad-output/project-context.md#Technology Stack & Versions] - pinned versions and package rules.
- [Source: _bmad-output/project-context.md#Source-Generator Rules] - generator invariants.
- [Source: _bmad-output/project-context.md#Testing Rules] - solution-level test command and Verify discipline.
- [Source: _bmad-output/project-docs/architecture.md#The generation pipeline] - command density and page artifact contract.
- [Source: _bmad-output/project-docs/api-contracts.md#Source-generator contract] - generated output, density, and diagnostic catalog.
- [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs] - `CommandDensity` and `CommandModel.ComputeDensity`.
- [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs] - derivable classification and count diagnostics.
- [Source: src/Hexalith.FrontComposer.SourceTools/Transforms/CommandFormTransform.cs] - non-derivable-only form fields.
- [Source: src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs] - renderer density model.
- [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs] - conditional `CommandPage` emission.
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs] - density to render-mode output.
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandPageEmitter.cs] - full-page route host output.
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs] - parser coverage.
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/CommandFormTransformTests.cs] - transform coverage.
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs] - generator integration coverage.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Generated/RenderModeOverrideTests.cs] - default runtime density coverage.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-04: Create-story workflow loaded project context, sprint status, epics, architecture/API
  docs, previous Story 3.1, recent git history, live source files, and current tests.
- 2026-06-04: Discovered story is a confirm-and-pin density story. Live implementation already
  computes density and conditionally emits `CommandPage`; remaining risk is missing boundary and
  derivable-field generator/runtime pins plus `HFC1011` coverage.
- 2026-06-04: Official NuGet/package check completed for relevant pinned packages. No package
  updates are required or desired for this story.
- 2026-06-04: Re-audited the live density path before editing. `CommandModel.ComputeDensity`
  remains `<=1` Inline, `<=4` CompactInline, and `>=5` FullPage; parser derivable classification,
  form/renderer transforms, renderer/page/form emitters, and generator page gating match the story.
- 2026-06-04: Added SourceTools density, derivable-field, form-emitter, generator-artifact, and
  `HFC1011` total-property coverage. No production source changes were required.
- 2026-06-04: Added Shell runtime coverage for the four-field compact boundary and reran inline,
  compact inline, and full-page renderer lanes.
- 2026-06-04: Validation evidence: changed SourceTools test project Release build passed 0 warnings /
  0 errors; changed Shell test project Release build passed 0 warnings / 0 errors. Exact solution
  build command is locally blocked by NuGet audit network access (`NU1900` against
  `api.nuget.org`) during restore in this sandbox.
- 2026-06-04: Validation evidence: exact solution VSTest command is locally blocked before test
  execution by MSBuild/VSTest socket restrictions (`SocketException (13): Permission denied`).
  xUnit v3 in-process fallback: SourceTools focused lane 116/116 passed; Shell focused lane 36/36
  passed after excluding the known pre-existing full-page query-fallback baseline failure.
- 2026-06-04: Broader in-process default lanes reproduced known non-story baselines:
  SourceTools.Tests 985 total / 3 known failures; Shell.Tests 1761 total / 9 known failures.
  `git diff --name-only -- '*.verified.txt'` returned no changes.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story validates as ready-for-dev after checklist review: ACs are explicit, tasks map to ACs,
  implementation files are identified, existing tests are cited, and scope exclusions prevent
  command lifecycle/FC-TBL/new-item regressions.
- Confirm-and-pin implementation completed with no production source changes. Density thresholds,
  derivable-field exclusion, conditional full-page page emission, HFC1007/HFC1011 diagnostics, and
  Shell runtime render surfaces are pinned by focused tests.
- Story 3.1 command behavior was preserved: generated command form output still uses
  `IUlidFactory.NewUlid()` and no `Guid.NewGuid()` command-form correlation path was introduced.
- Local exact solution build/test gates remain environment-blocked by NuGet audit network access and
  VSTest/MSBuild socket restrictions; focused in-process story evidence is green and CI remains the
  authoritative solution-level gate.

### File List

- `_bmad-output/implementation-artifacts/3-2-apply-the-density-rule-to-command-forms.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandDensityTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/CommandTestSources.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererTestFixtures.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/RenderModeOverrideTests.cs`
- `tests/e2e/specs/command-form-generation.spec.ts`

### Change Log

- 2026-06-04: Added confirm-and-pin test coverage for command density thresholds, derivable-field
  exclusion, HFC1011 total-property diagnostics, and Shell runtime compact-boundary behavior; moved
  story to review with environment caveats recorded.
- 2026-06-04: Story-automator adversarial review. Re-verified all 5 ACs against live source and ran
  the new pins in-process: SourceTools test project built 0/0, 15 new density/derivable/HFC1011/
  form-emitter cases passed; Shell test project built 0/0, the new four-field compact runtime pin
  passed. 0 Critical / 0 High. 1 Medium auto-fixed: File List was missing the changed e2e spec
  `tests/e2e/specs/command-form-generation.spec.ts` (added). Status moved review -> done.

## Senior Developer Review (AI)

- **Reviewer:** Jérôme Piquot — 2026-06-04 (story-automator-review)
- **Outcome:** Approve. Status review -> done.
- **AC1 (density thresholds + page artifact):** IMPLEMENTED. `CommandModel.ComputeDensity`
  (`src/.../Parsing/DomainModel.cs:194`) returns Inline `<=1`, CompactInline `2..4`, FullPage `>=5`;
  `FrontComposerGenerator` gates `CommandPage.g.razor.cs` on `Density == FullPage`. Pinned by the new
  `RunGenerators_CommandDensityThresholds_*` theory (1/2/4/5) — verified passing.
- **AC2 (derivable exclusion):** IMPLEMENTED. `CommandParser.IsDerivableProperty`
  (`src/.../Parsing/CommandParser.cs:389`) classifies the well-known name set plus `[DerivedFrom]`
  (walking overrides/shadows); form/renderer transforms use `NonDerivableProperties` only. Pinned by
  the parser, generator, and form-emitter derivable tests — verified passing.
- **AC3 (runtime render surfaces):** IMPLEMENTED. New `FourFieldCompactCommand` fixture + bUnit
  `Renderer_DefaultMode_MatchesDensityForFourFieldCompactCommand` (expand-in-row, no breadcrumb)
  passes; inline/compact/full-page lanes preserved.
- **AC4 (count diagnostics):** IMPLEMENTED. HFC1007 warn `>30`/error `>100` and HFC1011 total `>200`
  (`CommandParser.cs:286,319`). New HFC1011 tests prove total-limit is independent of the density
  count and does not trigger HFC1007 — verified passing.
- **AC5 (no Story 3.1 regression):** HELD. No production source changed; `git diff '*.verified.txt'`
  is empty; generated `correlationId` path is untouched.
- **Findings:** [Medium] File List omitted the changed e2e spec — auto-fixed by adding it.
- **Note:** The e2e spec references demo artifacts (`ConfigureCounterCommand`, `BatchIncrementCommand`,
  `IncrementCommand`, the `/counter` page sections and `Configure Counter` link) that all exist in
  `samples/Counter`; the spec runs under Playwright, not the in-process lane.
