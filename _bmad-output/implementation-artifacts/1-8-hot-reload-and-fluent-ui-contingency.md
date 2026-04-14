# Story 1.8: Hot Reload & Fluent UI Contingency

Status: ready-for-dev

## Story

As a developer,
I want domain attribute changes to trigger incremental source generator rebuilds with hot reload support, and a documented contingency plan for Fluent UI v5 GA migration,
so that my development inner loop is fast and I'm protected against upstream Fluent UI breaking changes.

## Acceptance Criteria

### AC1: Fast Incremental Rebuild on Domain Attribute Changes

> **Scope:** This AC covers the full end-to-end cycle (file save → incremental rebuild → browser update). See AC2 for generator-only performance.

> **Human-executed:** Task 1 requires running `dotnet watch` and observing browser behavior. The dev agent produces a measurement template and documentation stubs; a human validates by running the app.

**Given** a running Counter.Web application with `dotnet watch` in Blazor Server mode
**When** a developer adds or modifies a [Projection] property or attribute on a domain type
**Then** the source generator incrementally rebuilds only the affected domain assembly (not the full solution)
**And** the updated DataGrid reflects the change without full solution rebuild or manual application restart
**And** end-to-end latency (file save to browser update) is measured and recorded as a baseline
**And** if measured latency exceeds 2 seconds (NFR10), a note is added to `_bmad-output/implementation-artifacts/deferred-work.md` recording the measured value, environment, and proposed revised threshold

### AC2: Incremental Rebuild Performance Baseline

> **Scope:** This AC covers generator execution time only (CSharpGeneratorDriver). See AC1 for full end-to-end cycle.

**Given** a CSharpGeneratorDriver-based benchmark test exists for the source generator
**When** the test uses a two-pass incremental protocol (first pass seeds cache, second pass adds one property and measures the delta rebuild)
**Then** the measured incremental rebuild time per domain assembly is recorded as a baseline
**And** the test asserts incremental rebuild time is < 500ms (NFR8) with a documented machine spec
**And** the benchmark runs in CI (advisory mode, `continue-on-error: true`) to establish a regression baseline

### AC3: Fluent UI v5 Contingency Plan Documentation

**Given** Fluent UI Blazor v5 is pinned at an exact RC version in Directory.Packages.props
**When** the contingency plan document is reviewed
**Then** it contains all of the following sections:
- Version pin update procedure (step-by-step)
- Load-bearing API validation checklist (FluentLayout, DefaultValues, FluentDataGrid, FluentProviders, FluentNav, Epic 2 form components)
- Migration effort estimate (1-2 weeks budget for solo developer)
- Rollback procedure (revert to prior RC pin)
- Canary build workflow structure (canary-fluentui.yml, documented for W2/Epic 3 implementation)
- Note to subscribe to microsoft/fluentui-blazor releases for GA notification (human process step)

### AC4: Hot Reload Limitation Documentation and Diagnostic Reservation

**Given** a developer reads the hot reload guide (`docs/hot-reload-guide.md`)
**When** they consult the change-type support matrix
**Then** the documentation clearly categorizes which domain model changes support incremental rebuild vs. require full restart (minimum 8 change types with Yes/No and notes)
**And** the [BoundedContext] partial declaration edge case is documented as "Unverified — speculative, needs investigation" (not presented as confirmed behavior)
**And** the guide explicitly states the `.g.cs` Blazor hot reload limitation (requires rebuild, not true hot reload)
**And** a reserved diagnostic ID (HFC1010) exists as a comment in `DiagnosticDescriptors.cs` AND a corresponding row in `AnalyzerReleases.Unshipped.md`
**And** HFC1010 has no `DiagnosticDescriptor` field — only a comment documenting its intended purpose (analyzer implementation deferred — generators cannot detect diffs)

## Tasks / Subtasks

- [ ] Task 1: Manual hot reload validation with Counter sample (AC: #1) **[HUMAN-EXECUTED]**
  - [ ] 1.1 Run Counter.Web with `dotnet watch` in Blazor Server mode; add a property to CounterProjection; verify DataGrid column appears after incremental rebuild
  - [ ] 1.2 Modify [Display(Name=...)] attribute; verify label updates
  - [ ] 1.3 Measure and record end-to-end latency (file save → browser update) with timestamps; if > 2s, add note to `_bmad-output/implementation-artifacts/deferred-work.md` with measured value, environment (OS, .NET SDK version, machine), and proposed revised threshold
  - [ ] 1.4 Verify Fluxor store re-initializes correctly after `dotnet watch` rebuild (counter state starts at zero, store is functional). Note: Fluxor state is NOT preserved across `dotnet watch` restarts — server restart clears Blazor Server DI container. Document this limitation in `docs/hot-reload-guide.md`.
  - **Dev agent role:** Produce measurement template stub in `docs/hot-reload-guide.md` with placeholder for human-recorded values. If agent cannot run `dotnet watch`, mark Task 1 subtasks as `TODO: human validation` and proceed to Tasks 2-4.

- [ ] Task 2: Add incremental rebuild benchmark test (AC: #2)
  - [ ] 2.1 Create `tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/IncrementalRebuildBenchmarkTests.cs` — **two-pass incremental protocol:** first `driver.RunGenerators(compilation)` seeds the cache; then mutate source to add one property to a [Projection] type, create compilation C2, and time only `driver.RunGenerators(C2)`. Assert delta < 500ms. Follow xUnit v3 patterns. Reference `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/IncrementalCachingTests.cs` and `ParseStagePerformanceTests.cs` as templates.
  - [ ] 2.2 Document baseline machine spec (CPU, RAM, OS) in test file comments. Add `[Trait("Category", "Performance")]` for CI filtering.
  - [ ] 2.3 Add test for invalid/partial syntax tolerance — e.g., `[Projection] public partial class Bad { public string X { get }`. **Expected contract:** (a) zero generated files for the malformed type, (b) Roslyn-level compile diagnostic (not generator), (c) no unhandled exception from generator pipeline.

- [ ] Task 3: Reserve HFC1010 diagnostic and write hot reload docs (AC: #4)
  - [ ] 3.1 Two files to update:
    - `DiagnosticDescriptors.cs`: Add as a **comment** (NOT a `DiagnosticDescriptor` field). Verbatim: `// HFC1010 reserved — "Full restart required for this change type" (not yet implemented; requires analyzer, not generator — see docs/hot-reload-guide.md)`
    - `AnalyzerReleases.Unshipped.md`: Add as a **table row** (NOT a comment). Use the existing table format: `HFC1010 | HexalithFrontComposer | Info | Reserved — full restart required (not yet implemented)`
  - [ ] 3.2 Create `docs/hot-reload-guide.md` with: change-type support matrix (copy Dev Notes table, minimum 8 rows with Yes/No and notes per category), `.g.cs` limitation explanation, Fluxor state non-preservation note, measurement template for human validation, and version-scope header: "Validated against .NET 10 SDK. Verify on .NET 11+ — the .g.cs limitation may be resolved."
  - [ ] 3.3 In the change-type matrix, mark the [BoundedContext] partial declaration row as "Unverified — speculative, needs investigation" (not "Yes")

- [ ] Task 4: Create Fluent UI v5 contingency plan (AC: #3)
  - [ ] 4.1 Create `docs/fluent-ui-v5-contingency.md` with all sections required by AC3. Validate that rollback commands work: test `dotnet add package Microsoft.FluentUI.AspNetCore.Components --version "5.0.0-rc.2-26098.1"` resolves correctly and document exact quoting/escaping.
  - [ ] 4.2 In the canary workflow section, note that the `dotnet nuget search --format json | jq` pattern needs a fallback/validation step before W2 implementation (fragile output format)

**DoD for documentation outputs:** Each doc must contain all sections listed in its AC. Contingency plan must be actionable by a developer who has never read the architecture doc. Hot reload guide must include the complete change-type matrix with Yes/No and notes per category. The `docs/` directory must be created at the project root if it doesn't exist.

## Dev Notes

### Critical Architecture Constraint: Source Generator Output and Hot Reload

**The most important thing to understand:** Blazor hot reload does NOT pick up changes to source generator `.g.cs` output files. This is a fundamental .NET/Roslyn limitation, not a FrontComposer bug.

The developer workflow is:
1. Developer modifies domain type (e.g., adds property to CounterProjection)
2. `dotnet watch` detects file change and triggers incremental rebuild
3. Source generator runs (Parse -> Transform -> Emit pipeline)
4. New `.g.cs` files are written to `obj/`
5. `dotnet watch` detects the rebuilt assembly and applies changes
6. Browser reflects update

**This is NOT true hot reload** (which skips rebuild). It is a fast incremental rebuild cycle via `dotnet watch`. The < 2 second NFR10 target covers the full cycle: file save -> incremental rebuild -> browser update.

### Key File Paths

- Generator entry point: `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`
- Diagnostics: `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`
- Analyzer releases: `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`
- Counter domain: `samples/Counter/Counter.Domain/CounterProjection.cs`
- Counter web: `samples/Counter/Counter.Web/`
- Fluent UI pin: `Directory.Packages.props` (line 15)
- Benchmark template: `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/IncrementalCachingTests.cs`
- Performance template: `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/ParseStagePerformanceTests.cs`

### Hot Reload Change Categories

| Change Type | Hot Reload? | Notes |
|---|---|---|
| Add/remove property on [Projection] type | Yes (via dotnet watch rebuild) | Incremental generator re-runs < 500ms |
| Change [Display(Name=...)] attribute | Yes (via dotnet watch rebuild) | Label resolution chain updates |
| Change [BoundedContext] attribute parameter | **Unverified** | Expected to work via dotnet watch rebuild (nav grouping updates), but if [Projection] and [BoundedContext] are on separate partial declarations, the change may not trigger re-generation. Speculative — needs investigation (see deferred-work.md). |
| Change property type (e.g., int → long) | **No** — requires full restart | Type change may break Fluxor state deserialization |
| Add nullability (int → int?) | **No** — requires full restart | Nullable wrapper changes field type mapping |
| Change generic type parameters | **No** — requires full restart | Roslyn limitation on generic type hot reload |
| Add new [Projection] attribute to unannotated type | Yes (via dotnet watch rebuild) | New type enters ForAttributeWithMetadataName pipeline |
| Modify non-generated .razor files | **Yes** — true Blazor hot reload | No rebuild needed for hand-written Razor |

### Diagnostic Reservation Guide

HFC1010 is **reserved as comment + table row** (no `DiagnosticDescriptor` field). Generators see current state, not diffs — detecting "what changed" requires an analyzer, which is out of scope. See Task 3.1 for exact formats for both files.

### Fluent UI v5 Contingency Plan — Key Facts

**Current pin:** `5.0.0-rc.2-26098.1` in `Directory.Packages.props` (line 15)

**RC2 new features (relevant to contingency):**
- AutoComplete component (new)
- Toast component (new, replaces removed IToastService)
- Theme API + Theme Designer
- DataGrid pinned columns
- **MCP Server migration service** — can assist component-by-component migration from v4 to v5

**Load-bearing APIs to validate on version bump:**
1. `FluentLayout` + `FluentLayoutItem` — shell layout (used in Counter.Web MainLayout.razor)
2. `DefaultValues` — application-wide component defaults (e.g., button appearance)
3. `FluentDataGrid` — primary projection rendering (HTML `<table>` in v5, improved a11y)
4. `<FluentProviders />` — service provider registration (replaces v4's individual providers)
5. `FluentNav` — sidebar navigation (renamed from FluentNavMenu in v5)
6. Form components for Epic 2: `FluentTextField`, `FluentCheckbox`, `FluentDatePicker`, `FluentSelect`, `FluentNumberField`

**v4 → v5 breaking changes to document:**
- `FluentNavMenu` → `FluentNav` (renamed)
- `IToastService` → removed (use `FluentMessageBar` or new Toast component in RC2)
- `SelectedOptions` → `SelectedItems` (binding change)
- `FluentDesignTheme` → CSS custom properties (theming change)
- `<FluentDesignSystemProvider>` → `<FluentProviders />` (simplified)
- Property and attribute name changes to align with Fluent UI React v9

**Human process step (ADR-003 requirement):** Subscribe to `microsoft/fluentui-blazor` GitHub releases for GA notification. This is a manual step — add a reminder in the contingency plan document.

**Canary workflow skeleton (for contingency doc — implementation deferred to W2/Epic 3):**
```yaml
# .github/workflows/canary-fluentui.yml — Weekly Monday 6 AM UTC
# Steps: checkout (submodules: recursive) → setup-dotnet 10.0.x
#   → override Fluent UI version to latest pre-release in Directory.Packages.props
#   → dotnet build → dotnet test --no-build
#   → on failure: create GitHub issue with label 'canary-failure'
# NOTE: version detection via `dotnet nuget search --format json | jq` is fragile;
#   W2 implementation must add a validation step or fallback.
```

### Implementation Environment Notes

- **.NET 10 hot reload regression:** `dotnet watch` is slower with Blazor Server than .NET 8. The < 2s NFR10 target was calibrated against .NET 8 behavior — measurement on .NET 10 drives a data-informed decision.
- **xUnit v3 (3.2.2)** with `Verify.XunitV3` — diverges from architecture doc (which says v2). Use xUnit v3 patterns for all new tests.
- **Test baseline:** 202 tests (Contracts: 9, Shell: 43, SourceTools: 150). Full build+test ~6s locally.
- **CI is advisory mode:** `continue-on-error: true` on build-and-test job during Epic 1.
- **Build race (CS2012):** Always use `--no-build` after a separate `dotnet build` step.
- **Snapshot testing:** `.verified.txt` files use LF line endings; set `DiffEngine_Disabled: true` in CI.

### Source Documents

Architecture ADR-003 (Fluent UI contingency), ADR-004 (IR pipeline), epics.md (Story 1.8), prd.md (FR70, NFR8, NFR10), ux-design-specification.md (UX-DR61), deferred-work.md (caching edge case).

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### Change Log

### File List
