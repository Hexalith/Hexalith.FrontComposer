---
baseline_commit: 76fc6da7e66adb79c789d67bd3cd47460441a9c3
---

# Story 2.1: Render a projection from a `[Projection]` type

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield reality — read this FIRST (this is a CONFIRM-AND-PIN / VERIFY story, not build-from-scratch).**
> Unlike a green-field generator story, the entire `[Projection]` generation pipeline **already
> exists, emits, and is tested** in this repo at baseline `76fc6da`. The generator wires a full
> Parse → Transform → Emit chain for projections (`FrontComposerGenerator.cs:177-207`): it emits the
> **5 files** (`{T}.g.razor.cs` view + `Feature`/`Actions`/`Reducers`/`Registration`), dispatches the
> **6 `ProjectionRole` strategies** (Default / ActionQueue / StatusOverview / DetailRecord / Timeline /
> Dashboard→fallback), parses & applies the Level-1 formats (`[RelativeTime]`, `[Currency]`), badge
> slots, empty-state CTA, and **already reports HFC1003** (`[Projection]` not `partial`) at
> `AttributeParser.cs:166`. There are existing generator-driver tests (`GeneratorDriverTests`),
> role-approval snapshots (`RoleSpecificProjectionApprovalTests`), and bUnit render tests
> (`CounterStoryVerificationTests`).
>
> **So this story's job is to (1) VERIFY each of the three ACs actually holds end-to-end against
> `src/` at this commit, (2) CLOSE any coverage gap with a focused regression pin (especially the
> AC2 render-time behaviours and the AC3 *build-fails-under-TWAE* end-to-end assertion), and (3) make
> the verification durable** so Epic 2's grid/filter/expand stories build on a pinned projection
> baseline. **Default to ZERO `src/` change.** If a real `src/` gap is found, fix the generator (never
> hand-edit generated output) and update affected `.verified.txt` snapshots **intentionally**. If a
> behaviour is already correct, pin it — do not "improve" or restyle emitted output (that churns every
> downstream snapshot for no AC reason).

## Story

As an adopter developer,
I want a `[Projection]`-annotated `partial` type to generate a complete projection view,
so that operators get a working read-model page with no hand-written UI.

## Acceptance Criteria

**AC1 — 5 generated files appear under the public generated-output path; the view dispatches Loading / Empty / Data states per the role. *(FR1, NFR7)***
**Given** a `partial` class annotated `[Projection]` with a `[ProjectionRole]`,
**When** the project builds,
**Then** the **5 generated files** appear (`{T}.g.razor.cs`, `{T}Feature.g.cs`, `{T}Actions.g.cs`, `{T}Reducers.g.cs`, `{T}Registration.g.cs`) — discoverable under the **public** `GeneratedOutputPathContract` path `obj/{Config}/{TFM}/generated/HexalithFrontComposer/{Type}.g.razor.cs`, validated in **Debug *and* Release** (NFR7),
**And** the emitted view **dispatches Loading / Empty / Data** states according to the declared `ProjectionRole` strategy.

**AC2 — Role strategy, empty-state CTA, and Level-1 display formats apply when rendered. *(FR5, UX-DR1)***
**Given** a projection that declares `[ProjectionRole(..., WhenState=...)]`, `[ProjectionEmptyStateCta(commandType)]`, and badge/format attributes (`[ProjectionBadge]`, `[RelativeTime]`, `[Currency]`),
**When** the generated view renders,
**Then** the **role strategy** governs layout (e.g. ActionQueue filters by the `WhenState` enum member; Timeline orders chronologically; StatusOverview aggregates; DetailRecord renders a single record),
**And** the **empty-state CTA** renders its configured command call-to-action in the Empty state,
**And** the **Level-1 display formats** apply (`[RelativeTime]` renders relative, `[Currency]` renders currency-formatted), with each `[ProjectionBadge]` slot carrying a mandatory `aria-label` (FC-A11Y).

**AC3 — A non-`partial` `[Projection]` reports HFC1003 and the build fails under TWAE. *(FR6, NFR1)***
**Given** a `[Projection]` type that is **not** `partial`,
**When** the project builds,
**Then** **HFC1003** is reported,
**And** because `TreatWarningsAsErrors=true` (TWAE) is on everywhere, the HFC1003 *Warning* is promoted to a build-breaking **error** — the build fails rather than silently generating against an unusable type.

## Tasks / Subtasks

> ⚠️ **Verification-first.** Every task starts by confirming the current behaviour against `src/`
> before writing anything. Most subtasks below should resolve to "already true → add/confirm the pin";
> only open a `src/` change if a genuine AC gap is proven. Record what you found (true/false + the
> evidence) in the Dev Agent Record so the review can audit it.

- [x] **Task 1 — Verify AC1: 5-file emission + public output path + Loading/Empty/Data dispatch (AC: #1)** — ✅ all sub-concerns ALREADY PINNED; no gap, no change.
  - [x] 5-file emission confirmed against `FrontComposerGenerator.cs:200-205`. `GeneratorDriverTests.RunGenerators_BasicProjection_Produces6Files` already asserts **7** trees (`:23`) **and** all five projection hint names individually (`:27-31`: `.g.razor.cs`, `Feature.g.cs`, `Actions.g.cs`, `Reducers.g.cs`, `Registration.g.cs`). No assertion missing.
  - [x] **Public path (NFR7):** governed by `GeneratedOutputPathContract` (`Template` + `BuildProjectRelativePath`). Already pinned in **both Debug and Release** via `IdeParityConformanceUtilityTests.GeneratedOutputPathContract_BuildsPublicForwardSlashPath` (`[InlineData("Debug",…)]` + `[InlineData("Release",…)]`) using the contract API (no hardcoded path). Release coverage present.
  - [x] **Loading/Empty/Data dispatch:** all three states pinned — Loading via `FcProjectionLoadingSkeletonTests` (`role="status"`/`aria-busy="true"`), Empty via `FcProjectionEmptyPlaceholderTests`, Data via `CounterStoryVerificationTests` (columns/formatting). No gap.

- [x] **Task 2 — Verify AC2: role strategy + empty-state CTA + Level-1 formats + badge a11y (AC: #2)** — ✅ verified; 2 implicit-snapshot invariants made explicit (no `src/` change).
  - [x] **Role strategy:** all 6 strategies (`ProjectionRenderStrategy.cs`: Default/ActionQueue/StatusOverview/DetailRecord/Timeline/Dashboard) dispatch + render via the 5 happy-path `RoleSpecificProjectionApprovalTests` snapshots. **Positive WhenState pin ADDED** — `ActionQueueProjection_EmitsWhenStateFilter_ForNamedMembers` asserts the emitted view filters `state.Items.Where(x => x.Status.ToString() == "Pending" || …== "Submitted")` and emits `WhenState="Pending,Submitted"` (was only implicit inside the approval snapshot; the negative HFC1022 typo pin already existed).
  - [x] **Empty-state CTA:** ALREADY PINNED — `[ProjectionEmptyStateCta(commandType)]` parses (`AttributeParser.ParseProjectionEmptyStateCta` → `DomainModel.EmptyStateCtaCommandTypeName`) and `FcProjectionEmptyPlaceholderTests.CtaRendersWhenResolverFindsCommandAndUserAuthenticated` asserts the CTA renders in the Empty body (text + href). No gap.
  - [x] **Level-1 formats:** ALREADY PINNED — `CounterStoryVerificationTests.CounterProjectionView_LoadedState_RendersColumnsAndFormatting` asserts rendered `[Currency]` (`"1,234"`) and `[RelativeTime]` (`"04/14/2026"` deterministic out-of-window fallback). No gap.
  - [x] **Badge a11y (FC-A11Y ready-gate):** **emit pin ADDED** — `ActionQueueProjection_RendersBadgeColumn_ThroughFcStatusBadge_WithAccessibleColumnHeader` asserts the `[ProjectionBadge]`-mapped enum column emits through `FcStatusBadge` carrying `ColumnHeader="Status"` — the parameter `FcStatusBadgeTests.AriaLabelCombinesColumnHeaderAndLabelInEnglish` already pins into the mandatory `aria-label="Status: …"`. Composition now explicitly pinned end-to-end. No filter UI built (2.3 scope).

- [x] **Task 3 — Verify AC3: non-`partial` → HFC1003 → build fails under TWAE (AC: #3)** — ✅ parse pin confirmed; TWAE-promotion gap CLOSED.
  - [x] **Parse-level** pin confirmed: `AttributeParserTests.Parse_NonPartialProjection_EmitsHFC1003()` (fixture `TestSources.NonPartialProjection`) backed by report site `AttributeParser.cs:166`. Descriptor mapped through `FrontComposerGenerator.GetDescriptor(":344")` → `DiagnosticDescriptors.ProjectionShouldBePartial`.
  - [x] **End-to-end TWAE gap CLOSED** — new `Hfc1003TreatWarningsAsErrorsTests` (4 tests): (a) descriptor id/`Warning`/on-by-default/category; (b) generator emits HFC1003 as `Warning`; (c) **`NonPartialProjection_Hfc1003_IsPromotedToError_UnderTreatWarningsAsErrors`** drives the generator on a `WithGeneralDiagnosticOption(ReportDiagnostic.Error)` (=TWAE) compilation and asserts the reported HFC1003 severity is **Error** — i.e. the build breaks; (d) governance read pinning the config chain.
  - [x] Confirmed HFC1003 **absent** from `src/Directory.Build.props` NoWarn (`0419;1570;1572;1573;1574;1591;1734` only) and **not** downgraded in `.editorconfig`; `TreatWarningsAsErrors=true` set repo-wide (`Directory.Build.props:19`). All three pinned by the governance test.

- [x] **Task 4 — Run the build + test lanes; re-prove the pre-existing baseline (DoD)**
  - [x] `dotnet build Hexalith.FrontComposer.slnx -c Release` → **0 warnings / 0 errors** under TWAE.
  - [x] `DiffEngine_Disabled=true dotnet test … --filter "Category!=Performance&…"` run; everything this story touches is green (6 new pins pass).
  - [x] **Re-proved the standing 13-failure baseline** — exactly **8 Shell + 3 SourceTools + 2 Cli = 13**, matching the Epic-1 retro record. All are pre-existing/environmental (e.g. `Story112` needs missing `deferred-work.md`; `EvidencePathNormalization` is Linux case-sensitivity; Shell bUnit/FluentUI render timing). Shell + Cli assemblies were **never modified** by this story; SourceTools.Tests went 944→950 (+6 new pins, all green) with the **same** 3 pre-existing failures. None new, none misattributed.
  - [x] **No projection emitter behaviour changed → ZERO `.verified.txt` snapshot edits.** (Confirm-and-pin: default zero `src/` change held.)

- [x] **Task 5 — Honest record-keeping (retro AI-1 / AI-2)**
  - [x] **File List accuracy (retro AI-1):** complete File List + before→after counts recorded in Dev Agent Record below.
  - [x] **No authoring sentinels (retro AI-2):** scanned new/modified test files + this story file — no stray `</content>` / `</invoke>` / tool-call tags.

## Dev Notes

### What already exists vs. what this story does

| Concern | State today (`76fc6da`) | This story |
|---|---|---|
| Projection parse → IR | **Exists** — `AttributeParser.Parse` → `DomainModel`/`PropertyModel` (pure, `EquatableArray`, no `ISymbol`) | Verify; no change expected |
| 5-file emission wired | **Exists** — `FrontComposerGenerator.cs:200-205` | Pin all 5 hint names in `GeneratorDriverTests` |
| ProjectionRole dispatch (6 strategies) | **Exists** — `Transforms/ProjectionRenderStrategy.cs` + `RazorEmitter` switch; `RoleSpecificProjectionApprovalTests` snapshots | Confirm positive WhenState pin |
| Loading/Empty/Data states | **Exists** — `RazorEmitter` view body; bUnit pins in `CounterStoryVerificationTests` | Confirm all 3 states pinned |
| `[RelativeTime]`/`[Currency]` formats | **Exists** — `AttributeParser.ParseDisplayFormat` → `PropertyModel` → render | Confirm rendered-output assertion |
| `[ProjectionEmptyStateCta]` | **Exists** — parsed to `DomainModel.EmptyStateCtaCommandTypeName` | Confirm Empty-state CTA render pin |
| `[ProjectionBadge]` slots + `aria-label` | **Exists** — `PropertyModel.BadgeMappings` → `FcStatusBadge`/`FcDesaturatedBadge` | Confirm accessible-name pin |
| HFC1003 (non-`partial`) | **Exists & reports** — `AttributeParser.cs:166`; parse-test `Parse_NonPartialProjection_EmitsHFC1003` | Add **TWAE build-break** end-to-end assertion |
| Public output path (NFR7) | **Exists** — `Conformance/GeneratedOutputPathContract.cs` | Confirm Debug **and** Release path validation |

> **Key judgment for the dev agent:** the deliverable here is **confidence + durable pins**, not new
> generator features. If you find yourself rewriting `RazorEmitter`, stop — re-read the AC and confirm
> you've found a *genuine* gap, not a style preference. The Epic-1 retro §5 flags exactly this
> ("copy-a-pattern-without-the-difference") as the rising Epic-2 risk now that we touch generation/render.

### Exact anchors (read these before touching anything)

- **Generator pipeline / 5-file emission** — `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:177-207` (projection `RegisterSourceOutput`; `AddSource` for the 5 files at `:200-205`; HFC1001 "no types found" at `:143-148`).
- **Parser + HFC1003 / HFC1002 / Level-1 formats** — `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` (HFC1003 not-partial at **`:166`**; HFC1002 unsupported field type ≈`:505`; `ParseDisplayFormat` for `[RelativeTime]`/`[Currency]`; `[ProjectionBadge]`/`[ProjectionEmptyStateCta]`/`[ColumnPriority]`/`[ProjectionFieldGroup]` parsing).
- **IR models** — `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs` (`DomainModel`, `PropertyModel`, `BadgeMappingEntry`, `ParseResult`, `DiagnosticInfo`). **Invariant:** pure & fully equatable, `EquatableArray<T>`, **no `ISymbol` escapes parse** — if you must touch a model, hand-write `Equals`/`GetHashCode` and keep it Roslyn-free or you silently break incremental caching.
- **Transform / role + diagnostics** — `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModelTransform.cs` (HFC1023 Dashboard-fallback, HFC1025 badge coverage, HFC1027 collection-not-filterable, HFC1029 >15 columns, HFC1031 field-group-on-Timeline) and `Transforms/ProjectionRenderStrategy.cs` (6-strategy enum).
- **View emitter** — `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` (view class; Loading/Empty/Data body; role-dispatch switch). Siblings: `FluxorFeatureEmitter.cs`, `FluxorActionsEmitter.cs` (`EmitActions`/`EmitReducers`), `RegistrationEmitter.cs`.
- **Public output path** — `src/Hexalith.FrontComposer.Contracts/Conformance/GeneratedOutputPathContract.cs` (`Template`, `BuildProjectRelativePath`). NFR7: validated Debug **and** Release — never hardcode the path.
- **Diagnostic descriptors** — `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` (HFC1003 descriptor + default `Warning` severity). Symbolic ids in `Hexalith.FrontComposer.Contracts` `FcDiagnosticIds`.

### Test anchors (where pins live / go)

- **Generator-driver integration** — `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs` (`RunGenerators_BasicProjection_Produces6Files` asserts **7** trees = 5 + 2 manifests; `.g.razor.cs` name `TestDomain.CounterProjection.g.razor.cs`; HFC1001 cases).
- **Compilation entry point** — `tests/Hexalith.FrontComposer.SourceTools.Tests/CompilationHelper.cs` (`CreateCompilation(...)` ≈`:66-86`, `ParseProjection(...)` ≈`:88-92`). All generator tests drive `FrontComposerGenerator` via `CSharpGeneratorDriver.RunGenerators()`.
- **Parse-stage pins** — `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/AttributeParserTests.cs` (`Parse_NonPartialProjection_EmitsHFC1003`, `Parse_UnsupportedFieldType_EmitsHFC1002`, `Parse_AllFieldTypesProjection_Covers29Types`) + `Parsing/ProjectionRoleAttributeParserTests.cs`; fixtures in `Parsing/TestFixtures/TestSources.cs`.
- **Role + emitter snapshots** — `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.cs` (+ 8 `.verified.txt`, fixtures `RoleSpecificTestSources.cs`); `Emitters/RazorEmitterTests.cs`, `Emitters/RegistrationEmitterTests.cs`; per-code `Diagnostics/Hfc102{5,7,8,9},Hfc103{0,1}DiagnosticTests.cs`.
- **bUnit render pins** — `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs` (extends `GeneratedComponentTestBase`; `CounterProjectionView_LoadedState_RendersColumnsAndFormatting`, `StatusProjectionView_NullAndBooleanValues_RenderSnapshot` + 2 `.verified.txt`); rendering scaffolds in `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/` (`FcProjectionLoadingSkeletonTests`, `FcProjectionEmptyPlaceholderTests`, `FcProjectionSubtitleTests`, `FcProjectionRoutesTests`).
- **Fixtures / specimens** — `tests/.../Parsing/TestFixtures/TestSources.cs`, `Emitters/RoleSpecificProjections/RoleSpecificTestSources.cs`, and the Specimens host `samples/Counter/Counter.Specimens.Domain/SpecimenStatusProjection.cs` / `SpecimenFormattingProjection.cs` (`Hexalith__FrontComposer__Specimens__Enabled=true`).

### Project-context guardrails that apply here (non-negotiable)

- **Never hand-edit generated code** (`obj/**/generated/HexalithFrontComposer/`) — that path is a public contract. Fix the **generator** or the annotated type, never the output.
- **No `ISymbol` may escape the parse stage**; keep IR pure & `EquatableArray`-based — a missing equality field silently breaks incremental caching.
- **Diagnostics travel as `DiagnosticInfo` data**, converted to Roslyn `Diagnostic` **only inside `RegisterSourceOutput`**. Don't create a Roslyn `Diagnostic` in parse/transform.
- **`SourceTools` references only `Contracts`** (netstandard2.0-clean) — never add a net10-only dep into `SourceTools` or the netstandard2.0 face of `Contracts`.
- **C# house style:** file-scoped namespaces, Allman braces, `_camelCase` private fields, `Async` suffix, **`ConfigureAwait(false)` on every await** (CA2007 → build error via TWAE), `ArgumentNullException.ThrowIfNull` at public boundaries, **no copyright/license headers** (this repo has none), **CRLF**, 4-space indent, final newline.
- **Tests:** xUnit **v3** + **Shouldly** (`ShouldBe`/`ShouldThrow`, never raw `Assert.*`) + **Verify.XunitV3** (NOT `Verify.Xunit`); plural `{Class}Tests.cs`; three-part `Subject_Scenario_Expectation` method names; **solution-level** `dotnet test` + trait filters (not per-project); `.verified.txt` updated **intentionally** and committed.
- **Build discipline:** `.slnx` only; `TreatWarningsAsErrors=true` — fix warnings, don't blanket-suppress; built-in analyzers only (no Sonar/StyleCop/Roslynator); centralized versions in `Directory.Packages.props` (never add `Version=` to a `.csproj`).
- **Commits/branches:** Conventional Commits; this work is `test:`/`fix:` shaped (verification + pins), **not `feat:`** unless a genuine new generator capability is added (a false `feat:` triggers a minor bump + NuGet publish). Already on a feature branch (`feat/story-1-2-fc-lyt-page-layout`); do **not** commit to `main`.

### Epic-1 dependencies & their state (from the Epic-1 retro §6)

| Epic-2 needs | From Epic 1 | State at kickoff |
|---|---|---|
| Page layout for projection pages | Story 1.2 (FC-LYT) | ⚠️ **escalated** — `FullWidth` is default + shipped (right for DataGrid-dense projection pages); `--fc-page-max-inline-size=75rem` themeable. Projection pages get FullWidth with no opt-in → **zero action needed here**. |
| A11y ready-gate every story must pass | Story 1.3 (FC-A11Y) | ⚠️ **escalated**; primitive set shipped & pinned. Layer-1 (shell-frame) + Layer-2 (HFC1050–1055 override) green; **Layer-3 e2e axe lane is CI-only** (Playwright unsupported on this host + Node <24). For projection output: the badge `aria-label` is the in-scope a11y pin (AC2); the e2e grid-a11y depth is Story 2.4's. |
| Localized chrome around grids | Story 1.4 (FC-L10N) | ⚠️ **escalated**; parity tests green. Not in this story's path. |
| DataGrid-surface component doc | Story 1.5 (FC-DOC) | DataGrid doc is a **tracked gap owned by Story 2.8** — do **not** author `docs/` here. |
| FC-TBL table/column/filter API | Story 1.0 spike | ✅ surface pinned; **Story 2.8** formally confirms. This story renders into the grid; it does not confirm FC-TBL. |

> **Scope boundary:** this story is "**a projection renders from its `[Projection]` type**". It is
> *not* the DataGrid filtering/status/empty story (2.3), the expand-in-row a11y story (2.4), column
> prioritization (2.5), live updates (2.6), or FC-TBL confirmation (2.8). Stay inside AC1–AC3; resist
> scope creep into sibling stories that touch the same `Shell/Components/DataGrid/` surface.

### Why this is confirm-and-pin, and what "done" looks like

Per `epics.md`'s source caveat, FrontComposer is a **brownfield codify** project — most FR capability
is *already built*; the epics confirm + pin it. Story 2.1 is the projection-render anchor for Epic 2:
the generator already emits the 5 files, dispatches roles, applies formats, and reports HFC1003.
**Done = each of AC1/AC2/AC3 is proven true against `src/` at `76fc6da` and carries a durable
regression pin**, the Release build is 0/0 under TWAE, the default test lane is green, the standing
13-failure baseline is re-proved pre-existing, and the File List + counts are accurate. Genuine `src/`
change is the exception, not the expectation — and if made, it lands in the generator (never the
output) with intentional snapshot updates.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.1] — story statement, ACs (FR1, FR5, FR6, NFR1, NFR7, UX-DR1)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2] — Epic 2 scope & FR/UX-DR coverage
- [Source: _bmad-output/project-docs/api-contracts.md#1] — attribute→output contract; HFC1001–HFC1070 catalog (HFC1002/1003/1022–1032)
- [Source: _bmad-output/project-docs/architecture.md#3] — the generation pipeline (Parse→IR→Transform→Emit), 5-file projection output
- [Source: _bmad-output/project-docs/data-models.md#1] — registration model (`DomainManifest` → `IFrontComposerRegistry`)
- [Source: _bmad-output/project-docs/component-inventory.md#A] — `Fc*` projection/grid components (`FcStatusBadge`, `FcProjectionLoadingSkeleton`, `FcProjectionEmptyPlaceholder`)
- [Source: _bmad-output/project-context.md] — generator rules (no hand-edit output, no `ISymbol` escape, diagnostics-as-data), TWAE, test discipline, `DiffEngine_Disabled=true`
- [Source: _bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md] — FullWidth default for DataGrid-dense projection pages
- [Source: _bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md] — the FC-A11Y ready-gate (badge `aria-label` Layer-1; e2e Layer-3 CI-only)
- [Source: _bmad-output/implementation-artifacts/epic-1-retro-2026-06-03.md#3,#6,#7] — recurring File-List/sentinel taxes (AI-1/AI-2), 13-failure baseline, Epic-2 dependency states
- [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:177-207] — projection pipeline + 5-file `AddSource`
- [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs:166] — HFC1003 report site
- [Source: src/Hexalith.FrontComposer.Contracts/Conformance/GeneratedOutputPathContract.cs] — public output-path contract (NFR7)
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs] — 5-file emission pin
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs] — bUnit render/format pins

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context) — bmad-dev-story workflow.

### Debug Log References

- Release build: `dotnet build Hexalith.FrontComposer.slnx -c Release` → `0 Warning(s) / 0 Error(s)` under TWAE.
- Default lane: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
- Targeted new-pin run: 6 passed / 0 failed (4 `Hfc1003TreatWarningsAsErrorsTests` + 2 `RoleSpecificProjectionApprovalTests` AC2 facts).

### Completion Notes List

**Nature of the work.** Confirm-and-pin / verify story. The entire `[Projection]` pipeline already
exists and is tested at baseline `76fc6da`. Outcome: **ZERO `src/` change, ZERO `.verified.txt`
snapshot edits** — every AC was verified true against `src/`, and the two genuine *durability* gaps
were closed with focused, deterministic test pins.

**Per-AC verdict:**
- **AC1 — already fully pinned (no change).** 5-file emission (`GeneratorDriverTests`, all 5 hint names
  + 7-tree count), public output path in **Debug *and* Release** (`IdeParityConformanceUtilityTests`,
  via `GeneratedOutputPathContract` API — not hardcoded), and Loading/Empty/Data dispatch
  (`FcProjectionLoadingSkeletonTests` / `FcProjectionEmptyPlaceholderTests` / `CounterStoryVerificationTests`).
- **AC2 — verified; 2 implicit invariants made explicit.** Role strategy (6/6), empty-state CTA, and
  Level-1 `[Currency]`/`[RelativeTime]` formats were already pinned. The **positive WhenState filter**
  and the **badge→`FcStatusBadge` `ColumnHeader`(aria-label)** wiring were previously only *implicit*
  inside the 1100-line `ActionQueueProjection_Approval` snapshot; added two named, self-documenting
  emit assertions so a careless snapshot re-accept can no longer silently drop either invariant.
- **AC3 — TWAE-promotion gap closed.** The parse pin proved the diagnostic is *produced*; the new
  `Hfc1003TreatWarningsAsErrorsTests` proves the behaviour the AC asserts — HFC1003 is an on-by-default
  `Warning` that, under a TWAE compilation (`GeneralDiagnosticOption=Error`), surfaces as **Error**
  (build break), and that no `NoWarn`/`.editorconfig` rule downgrades it.

**Test counts (before → after):**
- `Hexalith.FrontComposer.SourceTools.Tests`: **944 → 953** (+9 new; **950 pass / 3 pre-existing fail**). Re-proved by the review run on 2026-06-04: `Failed: 3, Passed: 950, Total: 953`. The +9 = 4 `Hfc1003TreatWarningsAsErrorsTests` (dev-story) + 5 facts added to `RoleSpecificProjectionApprovalTests` (2 by dev-story: WhenState + badge; 3 by the subsequent QA `generate-e2e-tests` pass: Timeline / StatusOverview / DetailRecord role-layout pins).
- All other test assemblies: unchanged (not touched by this story).

> **Record-keeping correction (review, 2026-06-04):** the original dev-story note said "944 → 950 (+6); +2 named facts". That was accurate at dev-story time but went stale when the QA `generate-e2e-tests` pass added 3 more AC2 role-layout pins to the same file. Counts and File List below now reflect the actual git tree.

**Standing 13-failure baseline re-proved pre-existing** (8 Shell + 3 SourceTools + 2 Cli), matching the
Epic-1 retro record. Shell + Cli assemblies were never modified here; SourceTools.Tests kept the same 3
pre-existing/environmental failures (`DiagnosticRegistryTests.Story112_LedgerRowsMapToOneOfThreeFinalStates`
— missing `deferred-work.md`; `IdeParityConformanceUtilityTests.EvidencePathNormalization_HonorsCaseSensitiveFlagOnLinux`
— Linux case-sensitivity; `CommandFormEmitterTests.Emit_DoesNotLogModelInstance`). None new, none misattributed.

### File List

_Test-only additions/edits — no `src/` or generated-output changes attributable to this story (see out-of-scope note below)._

- **Added** `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1003TreatWarningsAsErrorsTests.cs` (AC3: 4 pins — descriptor, emit-as-Warning, TWAE→Error promotion, NoWarn/.editorconfig governance).
- **Modified** `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.cs` (AC2: **+5 named facts** — `ActionQueueProjection_EmitsWhenStateFilter_ForNamedMembers` (positive WhenState pin) + `ActionQueueProjection_RendersBadgeColumn_ThroughFcStatusBadge_WithAccessibleColumnHeader` (badge→`FcStatusBadge` accessible-`ColumnHeader` pin), both by dev-story; **plus 3 role-layout pins added by the QA `generate-e2e-tests` pass** — `TimelineProjection_EmitsChronologicalOrdering_OnTimestampProperty`, `StatusOverviewProjection_EmitsAggregation_GroupedByStatusWithCount`, `DetailRecordProjection_EmitsSingleRecordLayout_NotGrid`).
- **Added** `_bmad-output/implementation-artifacts/tests/2-1-test-summary.md` (QA `generate-e2e-tests` summary; AC coverage map + validation run 953/950/3).
- **Modified** `_bmad-output/implementation-artifacts/2-1-render-a-projection-from-a-projection-type.md` (Status, task checkboxes, Dev Agent Record, File List, Change Log — permitted sections only).
- **Modified** `_bmad-output/implementation-artifacts/sprint-status.yaml` (2-1 → in-progress → review → done; move-log comments).

**Out-of-scope working-tree change (NOT part of story 2.1):**

- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` is modified in the working tree — an XML doc-comment on `HeaderEnd` referencing **Story 3-4 D18** (default right-side actions: `FcPaletteTriggerButton` + `FcSettingsButton`). It is **not attributable to this story** and is intentionally **excluded** from this File List. The review confirmed it is a comment-only change (no behaviour change) and left it untouched rather than reverting another story's work. Surfaced here for transparency per the retro AI-1 File-List-accuracy gate.

### Change Log

| Date | Change |
|---|---|
| 2026-06-04 | Story 2.1 dev-story (confirm-and-pin). Verified AC1/AC2/AC3 against `src/` at `76fc6da`. ZERO `src/`/snapshot change. Added AC3 TWAE-promotion pins (`Hfc1003TreatWarningsAsErrorsTests`) and AC2 positive-WhenState + badge-aria-label emit pins (`RoleSpecificProjectionApprovalTests`). Release build 0/0 under TWAE; default lane green; standing 13-failure baseline re-proved pre-existing. Status → review. |
| 2026-06-04 | Adversarial code review (story-automator). Re-proved Release build **0/0** under TWAE and `SourceTools.Tests` **953 total / 950 pass / 3 pre-existing fail**; all 9 new pins green. AC1/AC2/AC3 confirmed IMPLEMENTED. Corrected record-keeping (retro AI-1): test counts 944→**953** (+9, not +6), `RoleSpecificProjectionApprovalTests` is **+5 facts** (not +2, after QA added 3 role-layout pins), `2-1-test-summary.md` added to File List, and the out-of-scope `FrontComposerShell.razor.cs` working-tree doc-comment change (Story 3-4) surfaced + explicitly excluded. No CRITICAL issues. Status → done. |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-04 · **Outcome:** ✅ Approve (record-keeping corrected; no code changes required)

**Independently re-verified (not trusted from the story):**
- `dotnet build Hexalith.FrontComposer.slnx -c Release` → **0 Warning / 0 Error** under repo-wide TWAE (AC3 promotion chain intact).
- Default lane on `SourceTools.Tests` → **Failed: 3, Passed: 950, Total: 953**; the 3 failures are the exact pre-existing/environmental standing baseline (`CommandFormEmitterTests.Emit_DoesNotLogModelInstance`, `IdeParityConformanceUtilityTests.EvidencePathNormalization_HonorsCaseSensitiveFlagOnLinux`, `DiagnosticRegistryTests.Story112_LedgerRowsMapToOneOfThreeFinalStates`). None new, none attributable to this story.
- Targeted run of the two touched classes → **20 passed / 0 failed** (16 `RoleSpecificProjectionApprovalTests` incl. all 5 new facts + 4 `Hfc1003TreatWarningsAsErrorsTests`).

**AC verdicts:** AC1 IMPLEMENTED · AC2 IMPLEMENTED (4/6 role layouts now have named pins; Default/Dashboard covered by snapshot + HFC1023) · AC3 IMPLEMENTED (parse pin + generator-driver TWAE→Error promotion pin).

**Test-quality check:** new pins use real Shouldly `ShouldContain` assertions on generated source (no placeholders), three-part names, order-independent, semantic markers (no hardcoded paths/timing). No authoring sentinels found in the new/modified files.

**Findings & dispositions:**
1. *(High → fixed)* Story File List + counts were stale vs git (claimed +2/944→950; actual +5/944→953, plus undocumented `2-1-test-summary.md`). The retro AI-1 File-List-accuracy gate was violated. **Corrected** in Dev Agent Record + File List + Change Log above.
2. *(Medium → surfaced, not reverted)* `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs` carries an out-of-scope doc-comment change (Story 3-4) in the working tree, contradicting the "ZERO `src/` change" claim. Comment-only, no behaviour change; **left untouched** (reverting would destroy another story's work) and **explicitly excluded** in the File List.
3. *(Low → noted)* `_bmad-output/project-docs/{architecture,component-inventory}.md` drift in the tree is outside code-review scope (excluded folder); no source impact.

No CRITICAL issues remained after the fixes, so the story advances to **done**.
