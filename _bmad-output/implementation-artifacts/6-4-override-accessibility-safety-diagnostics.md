---
baseline_commit: 9f7276bed4f6d34830b4a4523a50410cdd172ec3
---

# Story 6.4: Override-accessibility safety diagnostics

Status: done

<!-- Note: Created by BMAD create-story workflow and validated against the story-context checklist. -->

## Story

As an adopter developer,
I want overrides checked for accessibility regressions,
so that customization cannot silently break a11y.

## Acceptance Criteria

1. **HFC1050-HFC1055 build diagnostics are confirmed and pinned for custom overrides.**
   Given a statically inspectable Level 2, Level 3, or Level 4 custom override component,
   when it violates one of the FC-A11Y override rules,
   then the SourceTools analyzer reports the corresponding warning with the canonical teaching-message shape:
   HFC1050 missing accessible name, HFC1051 keyboard reachability blocked, HFC1052 suppressed focus visibility,
   HFC1053 missing `aria-live` parity, HFC1054 motion without reduced-motion fallback, and HFC1055 custom colors without forced-colors evidence.

2. **Analyzer scope and false-positive boundaries are documented and tested.**
   Given non-custom components, comments, safe named controls, and supported fallback markup,
   when the analyzer runs,
   then it does not report HFC1050-HFC1055 outside the customization surface, does not flag commented-out violations,
   preserves the existing bounded source scan, and keeps Level 2 `[ProjectionTemplate]` plus Level 3/4 registration-call discovery coverage.

3. **Development-only contract-mismatch diagnostics render through the existing panel.**
   Given `DEBUG` and `IHostEnvironment.IsDevelopment()`,
   when a Level 2/3/4 customization contract mismatch is recorded by the registry rejection log,
   then `FcCustomizationDiagnosticPanel` displays the sanitized mismatch with projection, component, role, field when applicable,
   expected/actual contract version details, fix guidance, and an HTTPS docs link.

4. **Production and Release builds do not expose dev-only mismatch UI.**
   Given Release builds or any non-Development environment,
   when a customization contract mismatch exists,
   then no development-only panel or dev-mode service is registered or rendered. Existing registry behavior remains unchanged:
   `LogAndSkip` logs and skips rejected descriptors; `FailClosedOnMajorMismatch` throws at startup.

5. **FC-CUST accessibility diagnostics contract is recorded and evidence is reconciled.**
   Given this story completes,
   when reviewing implementation evidence,
   then `_bmad-output/contracts/fc-cust-override-accessibility-diagnostics-contract-2026-06-05.md` records analyzer disposition, panel gating, non-goals, open items, source/test citations, and changed-file reconciliation.

## Tasks / Subtasks

- [x] **Task 1 - Re-audit HFC1050-HFC1055 against AC1 and AC2**
  - [x] Confirm `FcDiagnosticIds`, `DiagnosticDescriptors`, `AnalyzerReleases.Unshipped.md`, `docs/diagnostics/diagnostic-registry.json`, and `docs/diagnostics/HFC1050.md` through `HFC1055.md` agree on IDs, severity, docs links, and ownership.
  - [x] Confirm `CustomizationAccessibilityAnalyzer` reports every ID using the canonical `What/Expected/Got/Fix/Fallback/DocsLink` teaching shape.
  - [x] Confirm the analyzer reaches Level 2 `[ProjectionTemplate]` classes and Level 3/4 components referenced as the last type argument of `AddProjectionTemplate`, `AddSlotOverride`, and `AddViewOverride`.
  - [x] Record any catalog/front-matter drift honestly in the contract instead of silently overstating build-time behavior.

- [x] **Task 2 - Add default-lane analyzer pins for all six diagnostics (AC: #1, #2)**
  - [x] Extend `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/CustomizationAccessibilityAnalyzerTests.cs`.
  - [x] Add positive tests for HFC1050, HFC1051, HFC1052, HFC1053, HFC1054, and HFC1055.
  - [x] Add at least one Level 3 `AddSlotOverride<...>` and one Level 4 `AddViewOverride<...>` registration-call test proving registration-discovered components are analyzed.
  - [x] Add negative controls for non-custom components, accessible named controls, commented-out violations, and valid reduced-motion/forced-colors fallback evidence.
  - [x] Keep tests in xUnit v3 + Shouldly; use `CompilationHelper.CreateCompilation()` and `Compilation.WithAnalyzers(...)` as the existing tests do.

- [x] **Task 3 - Fix only proven analyzer gaps (AC: #1, #2)**
  - [x] If a pin fails, adjust `CustomizationAccessibilityAnalyzer` conservatively. Do not add a new analyzer package or broad DOM/CSS parser.
  - [x] Preserve `EnableConcurrentExecution()`, generated-code exclusion, bounded source scanning, and comment stripping.
  - [x] Do not make the analyzer depend on `CompilationProvider`, generated output paths, runtime registries, tenant/user state, or localized strings.
  - [x] Keep emitted diagnostics as warnings. `TreatWarningsAsErrors=true` is the build-breaking policy.

- [x] **Task 4 - Implement and pin development-only contract-mismatch panel display (AC: #3, #4)**
  - [x] Reuse `FcCustomizationDiagnosticPanel`; do not create a second diagnostic panel UI.
  - [x] Convert `CustomizationContractRejection` records into sanitized `CustomizationDiagnostic` instances with the existing shared contract and canonical formatter.
  - [x] Render the panel only when both gates are true: `#if DEBUG` build and `IHostEnvironment.IsDevelopment()`.
  - [x] Pin that Development/DEBUG displays the mismatch details, expected/actual versions, safe docs link, and metadata rows.
  - [x] Pin that Production/Staging and Release paths do not register/render the mismatch panel.
  - [x] Preserve `CustomizationContractValidationGate` semantics: `LogAndSkip` remains log-and-skip, and `FailClosedOnMajorMismatch` remains startup fail-closed.

- [x] **Task 5 - Produce FC-CUST accessibility diagnostics contract (AC: #5)**
  - [x] Create `_bmad-output/contracts/fc-cust-override-accessibility-diagnostics-contract-2026-06-05.md`.
  - [x] Include diagnostic mapping HFC1050-HFC1055 to FC-A11Y primitive and WCAG intent.
  - [x] Include analyzer scope and known static-analysis limits: statically inspectable generated C# only, companion `.razor.css` limitations unless surfaced in source, conservative substring rules, bounded source length, and comment stripping.
  - [x] Include panel gating and redaction requirements.
  - [x] Include non-goals and any open follow-up with owner, reason, risk, and follow-up story.

- [x] **Task 6 - Verify no regression and reconcile evidence (AC: #1-#5)**
  - [x] Run `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false`.
  - [x] Run the focused SourceTools diagnostics tests and focused Shell panel/contract-mismatch tests.
  - [x] Attempt the solution-level default lane with `DiffEngine_Disabled=true` and the standard trait filter; if local VSTest is socket-blocked, record the exact caveat and use the established in-process focused fallback.
  - [x] Confirm no `.verified.txt`, pact, `PublicAPI*.Shipped.txt`, package-version, `CanonicalSchemaMaterial`, generated-output-path, MCP URI/security, or schema-fingerprint changes unless a deliberately documented contract change owns them.
  - [x] Reconcile the story File List against `git status --porcelain` before moving to review.

## Dev Notes

### Brownfield Reality

- The core analyzer already exists: `CustomizationAccessibilityAnalyzer` is a Roslyn `DiagnosticAnalyzer` with six supported descriptors and registration-call discovery for Level 3/4. Do not build a second analyzer pipeline.
- HFC1050-HFC1055 IDs and descriptors already exist, as do published diagnostic docs and diagnostic-registry entries.
- Existing analyzer test coverage is thin: today it pins HFC1050 positive/negative and non-custom exclusion only. The real story work is confirming/pinning all six diagnostics and Level 3/4 reach.
- `FcCustomizationDiagnosticPanel` already exists and is used by Level 2/3 render-fault hosts (HFC2115) and Level 4 render-fault host (HFC2121). Reuse it for contract mismatch display.
- `ICustomizationContractRejectionLog` already aggregates Major-mismatched descriptor rejections from Level 2/3/4 registries. `CustomizationContractValidationGate` reads it at host startup. The missing UI path is turning those records into dev-only panel diagnostics.
- Current source comments still use historical labels such as Story 6-6 and Story 6-4. Do not churn comments for cosmetic renumbering unless touching the line for a real change.

### Prior Story Intelligence

- Story 6.1 confirmed FC-CUST precedence: Level 4 view override -> Level 2 template -> generated default body; Level 3 composes inside whichever body renders.
- Story 6.2 recorded a build-vs-runtime diagnostic phase seam for HFC1038-HFC1041. Apply the same honesty here: distinguish build analyzer warnings (HFC1050-HFC1055) from runtime/startup contract mismatch panel display.
- Story 6.3 recorded HFC1046 as reserved/catalog-only and explicitly assigned accessibility enforcement to HFC1050-HFC1055 / Story 6.4. Do not make HFC1046 the active accessibility diagnostic unless Product/architecture explicitly changes the catalog model.
- Stories 6.1 and 6.2 both had File List drift caught in review. Story 6.3 passed that gate. Treat File List reconciliation as an acceptance gate.

### Architecture Guardrails

- Keep SourceTools analyzer changes in `src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationAccessibilityAnalyzer.cs` and descriptor tests under `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/`.
- Keep runtime panel and contract-mismatch display work in Shell. Expected source areas are:
  `src/Hexalith.FrontComposer.Shell/Components/Diagnostics/`,
  `src/Hexalith.FrontComposer.Shell/Components/DevMode/`,
  `src/Hexalith.FrontComposer.Shell/Services/Customization/`,
  `src/Hexalith.FrontComposer.Shell/Services/Diagnostics/`,
  and `src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs`.
- `AddFrontComposerDevMode` already gates registrations with `IHostEnvironment.IsDevelopment()` and `#if DEBUG`. Any new mismatch-panel service must follow the same two-gate rule.
- `FrontComposerShell` currently mounts dev overlay markup based on the DEBUG build symbol. Do not rely on that alone for contract-mismatch UI; pair DEBUG with the runtime development environment.
- `CustomizationDiagnostic` is intentionally metadata-only and sanitized. Do not include tenant/user IDs, item payloads, field values, exception objects, render fragments, scoped services, access tokens, or localized strings in diagnostic properties.
- Use `CustomizationDiagnosticFormatter` for canonical teaching text where publishing to `IDiagnosticSink`.
- Public docs under `docs/` are CI-gated DocFX content. Update only owned diagnostic docs if behavior or wording truly changes; generated/contract evidence belongs in `_bmad-output/`.

### Technical Constraints

- .NET 10 SDK pinned by `global.json`; Roslyn package pinned in the repo; do not bump dependencies.
- SourceTools must stay analyzer-safe: concurrent execution enabled, generated code excluded, no unbounded source scans, no broad semantic model walks after cheap syntax filtering.
- C# style: file-scoped namespaces, Allman braces, nullable-safe public boundaries, `ConfigureAwait(false)` on awaited calls outside Blazor renderer-context code, no copyright headers.
- Central package management only. Never add `Version=` to a project file.
- Keep `SourceTools` dependency direction down to `Contracts`; do not pull Shell, FluentUI, MCP, or runtime services into SourceTools.
- Existing official guidance still aligns with current patterns: Roslyn analyzers should call `EnableConcurrentExecution()` for concurrent analysis, and ASP.NET Core environment-specific behavior should be gated through `IHostEnvironment`/`IWebHostEnvironment` environment checks. Do not add custom environment string comparisons.

### Testing Requirements

- Focused analyzer tests should remain fast and deterministic through `CompilationHelper.CreateCompilation()`.
- Shell panel tests should use bUnit, loose JS interop if Fluent UI/panel focus is involved, `AddFluentUIComponents()`, and `AddLocalization()` as existing panel host tests do.
- Negative tests are as important as positive tests for this story: avoid making adopter ordinary components or comments noisy.
- Use solution-level test command as the official gate:
  `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false`
- If the local sandbox blocks VSTest sockets, record the failure and run focused xUnit v3 in-process fallback lanes, matching Stories 5.4-6.3.

### Non-Goals

- Do not change FC-CUST render precedence, registry resolution semantics, or contract-version comparison directionality unless a failing AC requires it.
- Do not change HFC1033-HFC1046 behavior except for documenting boundaries if needed.
- Do not touch `CanonicalSchemaMaterial`, schema fingerprints, MCP resource URI grammar, MCP security gates, EventStore boundaries, package versions, pacts, public API baselines, or generated output paths.
- Do not introduce a new dev-mode overlay framework, a second customization diagnostic panel, a third-party analyzer package, or a browser-only a11y test as the only proof for build diagnostics.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 6.4: Override-accessibility safety diagnostics] - story statement and ACs.
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3: Establish FC-A11Y accessibility primitives as a ready-gate] - HFC1050-HFC1055 as FC-A11Y enforcement.
- [Source: _bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md] - FC-A11Y primitive mapping and enforcement layers.
- [Source: _bmad-output/contracts/fc-cust-override-resolution-and-level2-template-contract-2026-06-05.md] - Level 2 contract and override precedence.
- [Source: _bmad-output/contracts/fc-cust-level3-field-slot-contract-2026-06-05.md] - Level 3 diagnostic-phase honesty and slot composition.
- [Source: _bmad-output/contracts/fc-cust-level4-full-view-override-contract-2026-06-05.md] - Level 4 HFC1046 boundary and Story 6.4 handoff.
- [Source: _bmad-output/project-context.md] - project rules, stack versions, testing rules, and no-scratch-docs rule.
- [Source: _bmad-output/project-docs/architecture.md#Runtime composition (Shell)] - customization levels and runtime composition.
- [Source: _bmad-output/project-docs/api-contracts.md#Diagnostics catalog] - HFC1050-HFC1055 catalog summary.
- [Source: src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationAccessibilityAnalyzer.cs] - HFC1050-HFC1055 emission and registration discovery.
- [Source: src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs] - descriptor definitions for HFC1050-HFC1055.
- [Source: src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs] - diagnostic ID constants.
- [Source: src/Hexalith.FrontComposer.Contracts/Diagnostics/CustomizationDiagnostic.cs] - sanitized diagnostic contract.
- [Source: src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationDiagnosticPanel.razor] - reusable panel UI and safe docs-link handling.
- [Source: src/Hexalith.FrontComposer.Shell/Services/Customization/CustomizationContractValidationGate.cs] - strict startup mismatch gate.
- [Source: src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs] - Level 2 rejection logging.
- [Source: src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs] - Level 3 rejection logging.
- [Source: src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs] - Level 4 rejection logging.
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs] - DEBUG + Development service-registration pattern.
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/CustomizationAccessibilityAnalyzerTests.cs] - existing analyzer tests to extend.
- [Source: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticCatalogTests.cs] - catalog uniqueness and docs-link discipline.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionTemplateHostTests.cs] - panel test setup pattern for Level 2 host faults.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcFieldSlotHostTests.cs] - panel test setup pattern for Level 3 host faults.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionViewOverrideHostTests.cs] - panel test setup pattern for Level 4 host faults.
- [Source: https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnostics.analysiscontext.enableconcurrentexecution?view=roslyn-dotnet-4.14.0] - official Roslyn concurrent analyzer guidance checked 2026-06-05.
- [Source: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-10.0] - official ASP.NET Core environment gating guidance checked 2026-06-05.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-05: Create-story activation resolved with no prepend/append steps; loaded `_bmad-output/project-context.md`, BMAD config, sprint status, Epic 6 planning text, prior Stories 6.1-6.3, FC-A11Y and FC-CUST contract artifacts, relevant SourceTools/Shell source and tests, and recent git history.
- 2026-06-05: Confirmed brownfield implementation exists for `CustomizationAccessibilityAnalyzer`, HFC1050-HFC1055 descriptors/docs/catalog entries, `CustomizationDiagnostic`, `FcCustomizationDiagnosticPanel`, `CustomizationContractRejectionLog`, and `CustomizationContractValidationGate`.
- 2026-06-05: Identified the main gaps for dev-story: analyzer tests cover only HFC1050 today, and contract-mismatch panel display is not yet proven as a DEBUG + Development UI path.
- 2026-06-05: Per create-story checklist, optimized the story toward reuse, diagnostic-phase honesty, exact file locations, gating requirements, and changed-file reconciliation.
- 2026-06-05: Dev-story activation resolved with no prepend/append steps; loaded BMAD config, root/submodule project-context files, story 6.4, sprint status, SourceTools analyzer/catalog/docs, Shell dev-mode/panel/registry services, and existing tests.
- 2026-06-05: Red-phase analyzer pins exposed a real Level 3/4 registration-discovery gap: registration-referenced components were collected in syntax actions but could be read by symbol actions before collection completed.
- 2026-06-05: Fixed registration-discovered analyzer coverage by moving registration-referenced component analysis to compilation-end while preserving attribute-based Level 2 symbol analysis, concurrent execution, generated-code exclusion, bounded source scanning, and comment stripping.
- 2026-06-05: Added development-only contract mismatch diagnostics provider registered only inside `AddFrontComposerDevMode` under DEBUG + Development and rendered through the existing `FcCustomizationDiagnosticPanel` when the optional provider exists.
- 2026-06-05: Required solution Release build passed 0/0. Required solution-level VSTest default lane was attempted and aborted before test execution by `System.Net.Sockets.SocketException (13): Permission denied`; focused in-process fallback lanes were used.
- 2026-06-05: Focused evidence: SourceTools analyzer lane passed 12/12 in Release; Shell Release provider/non-render lane passed 3/3; Shell Debug development/non-development lane passed 4/4.
- 2026-06-05: Reconciliation confirmed no `.verified.txt`, pact, `PublicAPI*.Shipped.txt`, package-version, `CanonicalSchemaMaterial`, generated-output-path, MCP URI/security, or schema-fingerprint changes. `_bmad-output/story-automator/orchestration-1-20260604-140358.md` was a pre-existing unrelated workspace change and is not story-owned.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story created as a confirm-and-pin plus targeted-gap story, not a greenfield rewrite.
- Web check completed against official Microsoft docs for Roslyn analyzer concurrency and ASP.NET Core environment gating; no package/version changes are required.
- HFC1050-HFC1055 are pinned across Level 2 `[ProjectionTemplate]`, Level 3 `AddSlotOverride`, Level 4 `AddViewOverride`, all six positive diagnostics, canonical teaching-message sections, non-custom exclusion, comment stripping, accessible-name safe controls, and reduced-motion/forced-colors fallback evidence.
- Registration-discovered analyzer coverage is fixed by analyzing collected registration references at compilation end, eliminating the symbol-action race without broad parser/dependency changes.
- Development-only contract mismatch display now converts sanitized registry rejections into `CustomizationDiagnostic` instances and renders them through `FcCustomizationDiagnosticPanel` only when the DEBUG build and Development environment gates are both satisfied.
- Release, Production, and Staging paths do not register/render the mismatch provider or panel path; `CustomizationContractValidationGate` behavior remains unchanged.
- FC-CUST override accessibility diagnostics contract artifact records analyzer disposition, static-analysis limits, panel gating/redaction requirements, non-goals, open catalog wording follow-ups, source/test citations, and changed-file reconciliation.

### File List

- `_bmad-output/contracts/fc-cust-override-accessibility-diagnostics-contract-2026-06-05.md`
- `_bmad-output/implementation-artifacts/6-4-override-accessibility-safety-diagnostics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationAccessibilityAnalyzer.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Diagnostics/CustomizationContractMismatchDiagnosticProvider.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Diagnostics/ICustomizationContractMismatchDiagnosticProvider.cs`
- `samples/Counter/Counter.Web/Program.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/CustomizationAccessibilityAnalyzerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/AddFrontComposerDevModeExtensionsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Diagnostics/CustomizationContractMismatchDiagnosticProviderTests.cs`
- `tests/e2e/package.json`
- `tests/e2e/specs/override-accessibility-safety-diagnostics.spec.ts`

### Change Log

- 2026-06-05: Implemented Story 6.4 override accessibility diagnostic pins, fixed registration-discovered analyzer coverage, added DEBUG+Development contract mismatch panel adapter, created FC-CUST contract artifact, and moved story to review.
- 2026-06-05: Senior Developer Review (AI, story-automator auto-fix). Verified all ACs by building and running tests: Release build 0W/0E; SourceTools analyzer 12/12 (Release); Shell mismatch lane 6/6 (Release) and 8/8 (Debug). Auto-fixed one MEDIUM File List drift (added `samples/Counter/Counter.Web/Program.cs`, `tests/e2e/package.json`, `tests/e2e/specs/override-accessibility-safety-diagnostics.spec.ts`, `_bmad-output/implementation-artifacts/tests/test-summary.md`). No CRITICAL/HIGH issues. Status review -> done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-05 (story-automator adversarial review, auto-fix mode)
**Outcome:** Approve (auto-fixed). 0 CRITICAL, 0 HIGH, 1 MEDIUM (fixed), 2 LOW (accepted/noted).

### Verification performed (claims re-run, not trusted)

- `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` → **0 warnings / 0 errors** (confirms `TreatWarningsAsErrors` gate; new analyzer does not fire on repo source).
- SourceTools analyzer lane (Release) → **12/12** — six positive HFC1050-HFC1055 pins with canonical What/Expected/Got/Fix/Fallback/DocsLink shape, plus negatives (non-custom, commented-out, accessible-name, reduced-motion/forced-colors fallback) and Level 3 `AddSlotOverride` / Level 4 `AddViewOverride` registration-discovery pins. (AC1, AC2)
- Shell mismatch lane (Release) → **6/6**, (Debug) → **8/8** — provider conversion, DEBUG+Development panel render with projection/component/role/field/version/docs-link rows, DEBUG+non-Development suppression, Release no-render, and DEBUG-vs-Release registration gating. (AC3, AC4)
- AC validation: all five ACs IMPLEMENTED; all six tasks marked `[x]` confirmed against source/tests. Registry/validation-gate semantics (`LogAndSkip`, `FailClosedOnMajorMismatch`) untouched — verified by absence of diffs to those files.

### Findings

- **MEDIUM (FIXED) — File List drift / inaccurate reconciliation claim.** Completion Notes and the sprint-status comment claimed "File List reconciled", but four story-6.4-owned changed files were absent from the File List: `samples/Counter/Counter.Web/Program.cs` (the `Hexalith:FrontComposer:E2E:SeedContractMismatch` seed that drives the AC3/AC4 e2e), `tests/e2e/package.json` (`test:fc-a11y-diagnostics` script), `tests/e2e/specs/override-accessibility-safety-diagnostics.spec.ts` (new AC3/AC4 browser spec), and `_bmad-output/implementation-artifacts/tests/test-summary.md` (story-6.4 test evidence). This is the exact gate flagged in Prior Story Intelligence (6.1/6.2 drifted). Added all four to the File List. (`_bmad-output/story-automator/orchestration-1-20260604-140358.md` remains correctly excluded as a pre-existing unrelated workspace change.)
- **LOW (accepted) — Level 3/4 diagnostics now report at compilation-end, not live in the IDE.** The fix moved registration-discovered analysis from a `SymbolAction` to a `RegisterCompilationEndAction` to eliminate a genuine read-before-collection race (`AnalyzeRegisteredTypes`, `CustomizationAccessibilityAnalyzer.cs:130`). Correct trade-off: AC1 targets *build* diagnostics, the prior live path was racy, and Level 2 `[ProjectionTemplate]` analysis remains a live `SymbolAction`. Not reverted (reverting reintroduces the race). Static-analysis limits are recorded in the FC-CUST contract.
- **LOW (noted) — `ContractMismatchDiagnostics` getter rebuilds the diagnostic list per render** (`FrontComposerShell.razor.cs:260`): each access re-resolves the optional provider and re-iterates the rejection log. Dev-only surface, bounded by rejection count, and the rejection log is effectively static after startup hydration. Left as-is to avoid adding caching/staleness risk to a dev-only path.
