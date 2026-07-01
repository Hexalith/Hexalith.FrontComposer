---
baseline_commit: 3a4eb5912fb9c5b315fdb9fe3f9e676165a00607
---

# Story 2.8: Confirm the FC-TBL table API contract

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> Brownfield reality - read this first. This is a CONFIRM-AND-PIN / CONTRACT-FREEZE story, not a
> DataGrid rebuild. Stories 2.1-2.7 already implemented and pinned the read-only projection grid:
> generated `FluentDataGrid<T>` views, filtering, status badges, empty/loading states, expand-in-row
> detail, wide-column prioritization, live updates, reconnect/reconciliation, and registry search.
> Story 1.0 explicitly escalated one remaining FC-TBL gap: the adopter-facing DataGrid sub-components
> are public CLR types but are not frozen in a Shell public API baseline. This story resolves that gap.
>
> Default expectation: small docs/test/package-boundary changes, not feature work. Do not restyle,
> rename, or rewire DataGrid behavior unless the public-surface audit proves a real contract gap.

## Story

As an adopter developer,
I want the table/column/filter API surface (`FC-TBL`) confirmed stable,
so that I can build on the DataGrid without breaking-change risk.

## Acceptance Criteria

**AC1 - FC-TBL contract is documented, reviewed, and has explicit disposition.**
**Given** the FC-TBL API surface exercised by the Story 1.0 spike,
**When** the dev agent audits the live source and writes the contract artifact,
**Then** `_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md` exists and marks every
column/filter/expand/status-notice surface as `confirmed-stable`, `internalized`, or `open`,
**And** every `open` item has an owner, reason, and follow-up story or backlog reference,
**And** the contract cites the source files and tests that prove the disposition.

**AC2 - Public surface is frozen or deliberately internalized.**
**Given** the Story 1.0 spike found 12 public Shell DataGrid sub-components with no Shell
`PublicAPI.Shipped.txt`,
**When** the package/API boundary is reviewed,
**Then** the dev agent either:
- adds an intentional Shell public API baseline and a package-boundary test for the confirmed public
  FC-TBL surface, or
- changes any not-for-adopter component to `internal` and updates tests/docs accordingly, or
- records a specific reason why the current package-validation mechanism is insufficient and escalates
  the owner/date in the FC-TBL contract.

The first option is preferred if the components remain public. Do not silently leave public
adopter-facing FC-TBL types outside an intentional baseline.

**AC3 - Existing DataGrid behavior remains unchanged and pinned.**
**Given** Stories 2.1-2.7 already pinned the grid behavior,
**When** this story completes,
**Then** all existing column/filter/expand/prioritizer/live-update/search pins still pass,
**And** any new tests are boundary/contract tests, not duplicate feature tests,
**And** generated snapshots or `.verified.txt` files are unchanged unless the dev agent made an
intentional, reviewed public-contract change.

## Tasks / Subtasks

- [x] **Task 1 - Re-audit the Story 1.0 FC-TBL spike finding (AC: #1, #2)**
  - [x] Read `_bmad-output/spike-notes/1-0-shell-integration-spike-2026-06-03.md`, especially API
        question 4 and finding F3. Confirm the 12 public DataGrid components still match the spike list:
        `FcColumnFilterCell`, `FcColumnPrioritizer`, `FcExpandInRowDetail`,
        `FcExpandedRowHiddenBanner`, `FcFilterEmptyState`, `FcFilterResetButton`, `FcFilterSummary`,
        `FcMaxItemsCapNotice`, `FcNewItemIndicator`, `FcProjectionGlobalSearch`,
        `FcSlowQueryNotice`, `FcStatusFilterChips`.
  - [x] Re-run the source audit with `rg`/reflection rather than trusting the note. Confirm public vs
        internal visibility, `[Parameter]` surface, and whether each component is intended for adopter
        direct use or generator/shell-only use.
  - [x] Re-confirm `tests/Hexalith.FrontComposer.Shell.Tests/Spike/Story10ShellIntegrationSpikeTests.cs`
        still compiles against the surface and pins `MaxVisibleColumns == 10` plus
        `FcExpandInRowDetail.PanelId` / `SuppressedAnnouncement`.

- [x] **Task 2 - Write the FC-TBL contract artifact (AC: #1)**
  - [x] Create `_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md`.
  - [x] Document these contract slices, each with source and test references:
        generated grid emission; column model/ordering; per-column filters; reserved filter keys and
        global search; status chips/badges; empty/loading/filter states; expand-in-row detail and
        filter-hidden announcement; wide-column prioritizer and hidden-column persistence; notices
        (`FcSlowQueryNotice`, `FcMaxItemsCapNotice`, `FcNewItemIndicator`); server/client
        virtualization actions; public API baseline policy.
  - [x] Mark each slice `confirmed-stable`, `internalized`, or `open`. `open` requires owner, reason,
        risk, and follow-up location. Do not use vague dispositions like "needs review".

- [x] **Task 3 - Freeze or deliberately narrow the public API surface (AC: #2)**
  - [x] Inspect package validation state: `Directory.Build.targets`, `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`,
        `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`, and
        `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`.
  - [x] If the 12 FC-TBL components remain public, add an intentional Shell public API baseline and a
        Shell package-boundary test modeled on `PackageBoundaryTests`, scoped to the Shell namespaces that
        are meant to be adopter-facing. Include exported types, public constructors, public parameters,
        and public helper records such as `ColumnDescriptor` / `ColumnVisibilityContext` if they remain
        public.
  - [x] If any component/helper is not meant to be adopter-facing, make the smallest visibility change
        (`internal` where compatible with Razor/component use), update tests, and record the change in the
        contract. Preserve `InternalsVisibleTo` discipline and do not expose internals just for tests.
  - [x] If a full Shell baseline is too broad for this story, add a focused FC-TBL boundary test that
        enumerates the confirmed `Hexalith.FrontComposer.Shell.Components.DataGrid` public surface and
        blocks accidental additions/removals. Record why this is an interim package-boundary decision.

- [x] **Task 4 - Verify behavior did not regress (AC: #3)**
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release` and require 0 warnings / 0 errors
        under TWAE. Use `-m:1 /nr:false` only if node reuse or parallelism flakes.
  - [x] Run `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
        If sandboxed VSTest hits the known local socket `SocketException (13): Permission denied`, use
        the xUnit v3 in-process runner per the Story 2.4-2.7 pattern and record that solution-level VSTest
        remains the CI gate.
  - [x] Run focused tests for changed areas: Shell package-boundary/API tests, Story 1.0 spike tests,
        DataGrid component tests, generated projection render tests, and SourceTools emitter/transform
        tests if generator files changed.
  - [x] Confirm `.verified.txt` snapshots are byte-for-byte unchanged unless the public contract changed
        intentionally and the contract artifact explains why.

- [x] **Task 5 - Honest record keeping (AC: #1, #2, #3)**
  - [x] Update the Dev Agent Record with the exact public-surface disposition, tests run, before/after
        failure counts, and whether this story made source changes or only docs/tests changes.
  - [x] Record any remaining FC-TBL open items in the contract artifact with owners and follow-up stories.
  - [x] Keep the File List complete and scan modified files for stray authoring/tool-call sentinels.

## Dev Notes

### Prior story intelligence

- Story 1.0 resolved the bootstrap/table spike and escalated F3: FC-TBL public surface is not frozen.
  Its permanent regression suite lives at `tests/Hexalith.FrontComposer.Shell.Tests/Spike/Story10ShellIntegrationSpikeTests.cs`.
- Story 2.3 confirmed filter components and generated status/empty/loading paths. Do not duplicate
  its feature tests unless a contract-boundary assertion is missing.
- Story 2.4 confirmed expand-in-row accessibility, including always-present `role="region"` panel,
  `PanelId`, and `SuppressedAnnouncement`.
- Story 2.5 confirmed `[ColumnPriority]`, HFC1028/HFC1029, >15-column `FcColumnPrioritizer` wrapping,
  and `MaxVisibleColumns=10`.
- Story 2.6 accepted deferral of true row-level new-item producer wiring to Epic 9 / FC-NIP Stories 9.1/9.2.
  For FC-TBL, `FcNewItemIndicator` may still be part of the displayed component contract, but do not
  claim row-identity production is complete.
- Story 2.7 completed command palette/global registry search with zero source change. `FcProjectionGlobalSearch`
  remains an in-grid row search component, not the registry-wide palette search.

### Existing FC-TBL surface to audit

| Slice | Current anchors | Notes |
|---|---|---|
| Generated grid envelope | `RazorEmitter.EmitGridEnvelopeOpen/Close`, `ProjectionRoleBodyEmitter.EmitStandardDataGrid` | Emits outer `data-fc-datagrid`, row count, notices, optional `FcColumnPrioritizer`, `FluentDataGrid<T>`, `Virtualize=true`, `DisplayMode=Table`, density-bound `ItemSize`, `OverscanCount=3`, `ItemKey`, optional `OnRowClick`/`RowClass`, and sibling detail panel. |
| Column emission | `ColumnEmitter`, `RazorModel`, `ColumnModel`, `RazorModelColumnPriorityOrderTests` | Text/numeric/bool/date/enum/collection/unsupported paths are generator-owned. Do not hand-edit generated output. |
| Filters/search/sort | `FilterActions.cs`, `ReservedFilterKeys.cs`, `FilterEffects.cs`, `FilterReducerTests`, `FcColumnFilterCellTests`, `FcDataGridInputPersistenceTests` | `ColumnFilterChangedAction` rejects reserved `__` column keys; status and global search are packed in reserved keys. |
| Navigation/persistence | `DataGridNavigationActions.cs`, `VirtualizationActions.cs`, `DataGridNavigationState.cs`, `DataGridNavigationEffects.cs` | Snapshot equality is structural; persistence is effect-owned to preserve Fluxor single-writer discipline. |
| Wide columns | `FcColumnPrioritizer.razor(.cs/.css)`, `ColumnDescriptor`, `ColumnVisibilityContext`, `ColumnVisibilityPersistenceEffect` | Public helper records/classes must be included in any Shell public API baseline if left public. |
| Expand-in-row | `FcExpandInRowDetail`, `FcExpandedRowHiddenBanner`, `ExpandedRowState`, `RazorEmitterExpandInRowTests`, `FcExpandInRowDetailTests` | Detail panel renders outside the virtualized grid to avoid variable-height rows breaking `Virtualize`. |
| Status and notices | `FcStatusFilterChips`, `FcStatusBadge`, `FcDesaturatedBadge`, `FcSlowQueryNotice`, `FcMaxItemsCapNotice`, `FcNewItemIndicator` | `FcNewItemIndicator` remains producer-deferred per Story 2.6; document that honestly. |
| Public API policy | `Directory.Build.targets`, `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`, `PackageBoundaryTests` | Only Testing has a committed `PublicAPI.Shipped.txt` today. Shell package validation is opt-in via `EnableFrontComposerPackageValidation=true`. |

### Project structure notes

- Contract artifact goes under `_bmad-output/contracts/`; do not put generated BMAD notes under `docs/`.
- Shell component code lives under `src/Hexalith.FrontComposer.Shell/Components/DataGrid/`.
- Public contracts for actions/snapshots live under `src/Hexalith.FrontComposer.Contracts/Rendering/`.
- Generator code lives under `src/Hexalith.FrontComposer.SourceTools/Emitters/` and
  `src/Hexalith.FrontComposer.SourceTools/Transforms/`.
- Tests should stay adjacent to existing coverage:
  `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/`,
  `tests/Hexalith.FrontComposer.Shell.Tests/Spike/`,
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/`,
  `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/`,
  or a focused Shell package-boundary test area if created.

### Technical constraints

- .NET 10, FluentUI v5 RC `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, xUnit v3, bUnit, Shouldly.
  Versions are pinned centrally; do not upgrade libraries in this story.
- Use solution-level build/test commands and set `DiffEngine_Disabled=true` for Verify tests.
- Keep `TreatWarningsAsErrors=true` clean. Use `ConfigureAwait(false)` on awaits unless Blazor UI
  code intentionally resumes the renderer context and the local pattern already uses `ConfigureAwait(true)`.
- Do not add `Version=` to project files. Do not add third-party analyzer or CLI packages.
- Do not edit generated `obj/**/generated/HexalithFrontComposer/**`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.8] - story statement and AC.
- [Source: _bmad-output/spike-notes/1-0-shell-integration-spike-2026-06-03.md#API questions] - FC-TBL public-surface gap F3.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Spike/Story10ShellIntegrationSpikeTests.cs] - spike regression pins for FC-TBL public components.
- [Source: _bmad-output/project-docs/component-inventory.md#DataGrid surface] - DataGrid component inventory.
- [Source: _bmad-output/project-docs/api-contracts.md#Source-generator contract] - projection attributes, generated outputs, diagnostics.
- [Source: _bmad-output/project-context.md#Testing Rules] - test commands and Verify discipline.
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs#EmitStandardDataGrid] - generated grid shape.
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs] - generated column shape.
- [Source: src/Hexalith.FrontComposer.Contracts/Rendering/FilterActions.cs] - filter/search/sort public actions.
- [Source: src/Hexalith.FrontComposer.Contracts/Rendering/DataGridNavigationActions.cs] - snapshot and navigation public actions.
- [Source: src/Hexalith.FrontComposer.Contracts/Rendering/VirtualizationActions.cs] - server/client virtualization public actions.
- [Source: Directory.Build.targets] - opt-in package validation policy.
- [Source: tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs] - existing public API baseline pattern.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-04: Story 1.0 spike Q4/F3 re-audited. The 12 listed DataGrid components remain public `ComponentBase` types. Reflection also confirmed `ColumnDescriptor` and `ColumnVisibilityContext` are public helper types exposed by the prioritizer child-fragment contract.
- 2026-06-04: Red phase for `FcTblPackageBoundaryTests`: focused boundary test failed against an intentionally empty `PublicAPI.FcTbl.Shipped.txt` baseline (1 failed / 0 passed), proving the guard detects the live public FC-TBL surface.
- 2026-06-04: Green phase: populated the focused Shell FC-TBL baseline and packed it from the Shell project. `FcTblPackageBoundaryTests` passed 1/1 via xUnit v3 in-process runner.
- 2026-06-04: Required solution-level VSTest command remained blocked locally by `System.Net.Sockets.SocketException (13): Permission denied` while opening the VSTest communication socket, even with `-m:1 /nr:false`. CI remains the official VSTest gate; local evidence uses the xUnit v3 in-process runner fallback.
- 2026-06-04: Validation completed. Release build passed with 0 warnings / 0 errors. Focused lanes passed: DataGrid namespace 60/60, Story 1.0 spike 8/8, generated render pins 8/8, relevant SourceTools DataGrid emitter/transform/diagnostic pins 32/32. Full fallback lanes reproduced known non-FC-TBL failures: Shell.Tests 1760 total / 9 failed, SourceTools.Tests 958 total / 3 failed, Cli.Tests 41 total / 3 failed, Testing.Tests 11 total / 2 failed; Contracts.Tests 159/159 and Mcp.Tests 291/291 passed.
- 2026-06-04: `git diff --name-only -- '*.verified.txt'` returned no changes. Stray authoring/tool-call sentinel scan over changed story-owned files returned no matches.
- 2026-06-04 (review): Adversarial review re-verified the contract end-to-end. Release build 0/0 TWAE. Focused lanes re-run via xUnit v3 in-process runner: `FcTblPackageBoundaryTests` 1/1 (live public surface byte-matches `PublicAPI.FcTbl.Shipped.txt`), Story 1.0 spike 8/8, DataGrid namespace 60/60. Contract factual claims confirmed against source: `ReservedFilterKeys.StatusKey="__status"`, `SearchKey="__search"`, `VirtualizationReservedKeys.HiddenColumnsKey="__hidden"`, `FcColumnPrioritizer.MaxVisibleColumns=10`; `BadgeSlot` has exactly the 6 slots the e2e spec asserts. No `src/**` `.cs`/`.razor`/`.css` behavior changes (only the Shell `.csproj` packaging entry). Findings: 0 critical, 0 high; 2 medium documentation-completeness issues (File List omitted the two `tests/e2e/` changes) — auto-fixed.

### Completion Notes List

- Story context engine analysis completed - comprehensive developer guide created.
- FC-TBL public surface disposition: 12 Story 1.0 DataGrid components remain public and confirmed stable; `ColumnDescriptor` and `ColumnVisibilityContext` remain public because `FcColumnPrioritizer.ChildContent` exposes them.
- Added a focused Shell FC-TBL public API baseline at `src/Hexalith.FrontComposer.Shell/PublicAPI.FcTbl.Shipped.txt`, packed from the Shell project and enforced by `FcTblPackageBoundaryTests`.
- Wrote `_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md` with source/test citations for every required slice. The only `open` item is row-identity producer wiring for `FcNewItemIndicator`, owned by FrontComposer Product/UX plus command lifecycle and followed up by Epic 9 / FC-NIP Stories 9.1/9.2.
- This story made no DataGrid behavior changes and no generated snapshot updates.
- Added `tests/e2e/specs/fc-tbl-contract.spec.ts`: a Playwright FC-TBL generated-table contract spec asserting `data-fc-datagrid` envelopes, generated `data-fc-field` column keys, the 6 `BadgeSlot` status chips (`role="group"`, `aria-pressed` toggle), and `role="region"` expand-in-row detail across the `type` and `data-formatting` specimens; wired via a new `test:fc-tbl` npm script in `tests/e2e/package.json`. This is a contract/boundary e2e test (not a duplicate feature test) and runs in the Playwright lane, not the `dotnet test` lanes.

### File List

- `_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md`
- `_bmad-output/implementation-artifacts/2-8-confirm-the-fc-tbl-table-api-contract.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`
- `src/Hexalith.FrontComposer.Shell/PublicAPI.FcTbl.Shipped.txt`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcTblPackageBoundaryTests.cs`
- `tests/e2e/specs/fc-tbl-contract.spec.ts`
- `tests/e2e/package.json`

### Change Log

- 2026-06-04: Confirmed and documented the FC-TBL table API contract, added a focused Shell FC-TBL public API baseline, and added the Shell boundary test that freezes the DataGrid namespace public surface.
- 2026-06-04 (review): Senior Developer Review (AI) auto-fix — File List corrected to include the previously undocumented `tests/e2e/specs/fc-tbl-contract.spec.ts` FC-TBL generated-table contract e2e spec and the `tests/e2e/package.json` `test:fc-tbl` script. No source/behavior changes. Status → done.
