# Story 6.6: Build-Time Validation, Error Boundaries & Diagnostics

Status: ready-for-dev

> **Epic 6** - Developer Customization Gradient. **FR43 / FR44 / FR45 / FR47 / UX-DR31 / UX-DR64 / NFR36 / NFR80 / NFR86** build-time validation, override fault isolation, and teaching diagnostics across customization Levels 1-4. Applies lessons **L01**, **L06**, **L07**, **L08**, **L10**, **L11**, **L13**, **L14**, and **L15**.

---

## Executive Summary

Story 6-6 makes the customization gradient trustworthy after Stories 6-1 through 6-5 add the customization surfaces:

- Add a shared customization diagnostic model so Level 2 templates, Level 3 slots, Level 4 replacements, and dev-mode starter templates use the same expected/got/fix/docs-link message shape.
- Extend build-time validation for override contract versions, stale starter templates, unsupported hot-reload edits, and custom-component accessibility obligations without changing the Level 1-4 public API shape.
- Add narrow runtime error boundaries around adopter-authored template/slot/view replacement components so a broken override degrades only the affected element and never crashes the FrontComposer shell.
- Add an in-place diagnostic panel for customization failures with a stable HFC runtime ID, sanitized context, docs link, localizable copy, retry/recover behavior, and focus-safe fallback rendering.
- Preserve the Story 6-5 overlay ownership boundary: 6-6 can feed diagnostic state to the overlay, but it does not add new overlay navigation, cookbook content, MCP exposure, or visual specimen CI.
- Convert the existing contract-version and diagnostic reservations into a coherent story-owned range plan for Epic 6, including release-note entries and tests that keep HFC IDs unique.
- Define an honest hot reload / rebuild diagnostic path. Razor body edits may hot reload; contract metadata, generic shape, marker changes, and registry changes require rebuild or restart messages instead of stale behavior.
- Keep the work CI-safe for a solo maintainer: targeted analyzers, targeted component tests, and deterministic fixtures replace broad cross-product accessibility matrices.

---

## Story

As a FrontComposer adopter,
I want customization errors to be caught at build time where possible, isolated at runtime when rendering fails, and explained with actionable diagnostics,
so that a broken override never crashes the shell and I can fix problems without asking for framework-maintainer help.

### Adopter Job To Preserve

A developer should be able to customize generated UI at Levels 1-4 and still trust the framework to say: this contract is stale, this component is inaccessible, this edit requires rebuild, or this runtime failure is isolated here. The framework must teach the fix without logging sensitive domain values or forcing the developer to inspect generated code internals.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A Level 2 template, Level 3 slot, Level 4 view replacement, or Story 6-5 starter template declares an expected FrontComposer contract version | The project builds | Build-time validation compares the expected contract with the installed contract and emits deterministic HFC diagnostics for incompatible major mismatch, compatible minor drift, and stale starter output. |
| AC2 | A contract major mismatch is detected | SourceTools reports the diagnostic | The affected override is not selected silently; Level 2/3/4 resolution falls back to the lower generated path where safe, or fails closed with a clear startup diagnostic for ambiguous unsafe registration. |
| AC3 | A minor/build contract drift is detected | SourceTools reports the diagnostic | The override may still be selected when the contract is source-compatible, but the warning names expected version, actual version, target projection/component, fix, docs link, and rebuild guidance. |
| AC4 | A custom template, slot, or view replacement component can be inspected statically | The build runs | Roslyn analyzers check the six custom-component accessibility requirements from UX-DR31 where feasible: accessible name, keyboard reachability, focus visibility, aria-live parity, reduced motion, and forced-colors support. |
| AC5 | The analyzer cannot prove an accessibility property statically | It evaluates an override | It emits no speculative high-severity diagnostic. Instead, the story requires executable bUnit/sample tests for the gap and documents the limitation in the diagnostic oracle. |
| AC6 | A missing accessible name is detected on an interactive custom override root or required control | The build runs with normal severity | A warning includes WCAG name/role/value context, user scenario, expected/got/fix/docs-link text, and can be promoted to error by `TreatWarningsAsErrors`. |
| AC7 | A custom accent color or override-owned CSS color pair is configured in a statically inspectable way | The analyzer can compute contrast against the framework Light and Dark neutral backgrounds | It emits a warning when contrast fails WCAG AA thresholds: 4.5:1 normal text and 3:1 large text/UI components. Dynamic theme values are deferred to Story 10-2 visual/accessibility specimen tests, not guessed. |
| AC8 | A Level 2, Level 3, or Level 4 override throws during render | The generated view renders | A narrow `FcCustomizationErrorBoundary` catches the fault, renders a local diagnostic panel in the affected component/field/body region, and leaves the shell, navigation, sibling projections, and surrounding generated view usable. |
| AC9 | The runtime diagnostic panel renders | A developer inspects the fault | The panel includes localized description, HFC runtime ID, override level, projection/component name, exception category, docs link, retry/recover action when supported, and no tenant/user/item payload or field value leakage. |
| AC10 | A runtime override fault is logged | Structured logging runs | The log includes diagnostic ID, override level, projection type, component type, role/field when applicable, exception type/category, and sanitized message. It excludes item payloads, generated field values, localized user strings, render fragments, tenant IDs, access tokens, and raw user IDs. |
| AC11 | An error boundary recovers after the developer fixes Razor markup during development | The component re-renders or the developer activates retry | The boundary can recover without a full page refresh when Blazor supports the edit; if metadata changed, the panel points to the rebuild/restart diagnostic instead of pretending hot reload is enough. |
| AC12 | A developer changes marker metadata, contract version constants, generic context type, registration API calls, or descriptor shape during hot reload | The running app cannot safely reflect the change | SourceTools or Shell surfaces "Full rebuild/restart required for this change type" with HFC1010-style diagnostic text and no stale descriptor selection. |
| AC13 | A diagnostic message is produced by SourceTools or Shell for customization/generation errors | Tests inspect the message | It follows the teaching shape: What happened, Expected, Got, Fix, Fallback when relevant, DocsLink. Diagnostics missing one of these fields fail tests unless explicitly exempted in the oracle. |
| AC14 | A new HFC diagnostic ID is added in this story | The build/test suite runs | The ID appears in `FcDiagnosticIds`, `DiagnosticDescriptors` when SourceTools-owned, `AnalyzerReleases.Unshipped.md` when analyzer-owned, and the diagnostic catalog uniqueness test. IDs are never reused or silently renumbered. |
| AC15 | The Story 6-5 dev-mode overlay is active and an override is faulted or stale | The annotation/detail panel reads diagnostic state | The overlay may display the same diagnostic ID and summary from the shared model, but 6-6 does not add new overlay UX flows beyond feeding diagnostic state. |
| AC16 | Counter sample exercises Levels 2-4 customization evidence | Validation runs | The sample includes one valid path, one contract-drift fixture, one accessibility warning fixture, and one runtime-fault fixture proving shell continuity and local diagnostic fallback. |
| AC17 | The story is evaluated as the Epic 6 closer | A developer compares it to Stories 6-1 through 6-5 | Story 6-6 hardens validation and diagnostics only. It does not add new override levels, command-form customization, runtime authorization policy behavior, cookbook docs, visual specimen CI, MCP/agent surfaces, or production dev-mode overlay exposure. |

---

## Tasks / Subtasks

- [ ] T1. Define the shared customization diagnostic contract (AC8-AC15)
  - [ ] Add a dependency-light `CustomizationDiagnostic` or equivalent contract under `src/Hexalith.FrontComposer.Contracts/Diagnostics/` carrying HFC ID, severity, phase, override level, projection/component/role/field metadata, message sections, docs link, and sanitized structured properties.
  - [ ] Add `CustomizationDiagnosticSeverity` / phase enums only if existing Roslyn `DiagnosticSeverity` and runtime log levels cannot represent the shared shape without adding Roslyn dependencies to Contracts.
  - [ ] Keep the contract type metadata-only: no exception object, render fragment, tenant/user ID, item payload, localized string payload, or scoped service instance.
  - [ ] Add formatting helpers that produce the canonical teaching text: `What`, `Expected`, `Got`, `Fix`, optional `Fallback`, and `DocsLink`.
  - [ ] Add tests proving all message sections are non-empty for diagnostics emitted by this story.

- [ ] T2. Consolidate Epic 6 contract version validation (AC1-AC3, AC12)
  - [ ] Keep `ProjectionTemplateContractVersion` as the Level 2 source of truth and add matching version constants for Level 3 slots and Level 4 view replacements if those stories have landed them.
  - [ ] Add a shared SourceTools helper for comparing packed contract versions so HFC1035/HFC1036 behavior is not duplicated for every customization level.
  - [ ] Reserve the next available HFC10xx diagnostics for Level 3/4 contract mismatch/drift and stale Story 6-5 starter templates. Do not repurpose HFC1033-HFC1037, which are already Level 2 template diagnostics.
  - [ ] Major mismatch suppresses unsafe selection; minor drift warns and proceeds only when source-compatible; build-only revision drift is informational or suppressed per the version policy.
  - [ ] Add generated-manifest tests for Level 2 and future descriptor tests for Level 3/4 proving selection/fallback behavior matches the version decision.

- [ ] T3. Add build-time custom-component accessibility analyzers (AC4-AC7)
  - [ ] Implement analyzers in `src/Hexalith.FrontComposer.SourceTools/Diagnostics/` or an adjacent analyzer folder without creating a separate package.
  - [ ] Analyzer inputs are adopter-authored components connected to `[ProjectionTemplate]`, slot descriptors, view override descriptors, and Story 6-5 starter metadata. Do not scan every Razor component in the application.
  - [ ] For accessible names, inspect obvious static Razor/component patterns and companion class metadata; warn only when the component is demonstrably missing visible text or naming attributes on required interactive roots.
  - [ ] For keyboard reachability, flag only clear anti-patterns such as clickable non-focusable elements or `tabindex="-1"` on the only interaction path.
  - [ ] For focus visibility, flag obvious CSS suppression of `outline`, `box-shadow`, or Fluent focus token use in override-owned CSS when no replacement focus style is present.
  - [ ] For aria-live parity, validate that override-owned lifecycle/loading/empty/status surfaces preserve the framework polite/assertive category when the metadata identifies such a surface.
  - [ ] For reduced motion, flag custom animations/transitions in override-owned CSS without `@media (prefers-reduced-motion: reduce)`.
  - [ ] For forced colors, flag custom color/border/fill CSS in override-owned CSS without a `@media (forced-colors: active)` path using system color keywords.
  - [ ] For contrast, compute only static color pairs owned by override CSS and known framework neutral backgrounds. Defer dynamic token/runtime computed contrast to Story 10-2.

- [ ] T4. Implement hot reload / rebuild diagnostics (AC11-AC12)
  - [ ] Promote the existing HFC1010 reservation into a concrete diagnostic for "full rebuild/restart required for this change type" or allocate the next free ID if HFC1010 ownership has changed.
  - [ ] Define trigger categories: marker metadata change, expected contract version change, generic context type change, descriptor schema change, slot/view registration added or removed, duplicate registration introduced, and generated manifest version mismatch.
  - [ ] Add SourceTools tests showing supported Razor body edits are not flagged, while metadata/descriptor changes produce the rebuild diagnostic.
  - [ ] Add Shell-side stale descriptor detection so runtime registries fail closed with the same docs-linked diagnostic when a generated manifest is older than the loaded framework contract.

- [ ] T5. Add runtime customization error boundary and panel (AC8-AC11, AC15)
  - [ ] Add `FcCustomizationErrorBoundary` under `src/Hexalith.FrontComposer.Shell/Components/Diagnostics/` or `Components/Rendering/` as a narrow boundary wrapper for adopter override regions.
  - [ ] Integrate the boundary at Level 2 template host, Level 3 slot host, and Level 4 view replacement host boundaries. Do not wrap the whole shell or entire route body.
  - [ ] Add `FcCustomizationDiagnosticPanel` that uses Fluent UI primitives, localized resources, and predictable focus behavior.
  - [ ] The panel includes a retry/recover button only when the underlying `ErrorBoundary.Recover()` path is meaningful; otherwise it names the rebuild/restart path.
  - [ ] The panel supports keyboard navigation, visible focus, screen-reader announcement, reduced-motion behavior, and forced-colors compatibility.
  - [ ] The boundary publishes a sanitized `CustomizationDiagnostic` to `IDiagnosticSink` so Story 6-5 overlay and diagnostics panels can read the same event model.

- [ ] T6. Logging, telemetry, and privacy guardrails (AC9-AC10)
  - [ ] Add runtime HFC20xx IDs for customization boundary faults, stale descriptor manifest, inaccessible runtime fallback where build-time proof was impossible, and retry/recover failure.
  - [ ] Structured logs must use the existing `FcDiagnosticIds` constants and never inline raw HFC strings at call sites unless the constants are not yet public.
  - [ ] Add a redaction helper or reuse the existing safe identifier helper for component/projection metadata. Type names are allowed; tenant/user IDs, tokens, item payloads, field values, localized user strings, and render fragments are forbidden.
  - [ ] Add tests with sensitive-looking values in projection items and exception messages proving logs/panels do not leak them.
  - [ ] Add OpenTelemetry activity/event tags only for IDs, categories, override level, and sanitized type names.

- [ ] T7. Diagnostic catalog and docs-link discipline (AC13-AC14)
  - [ ] Update `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`, `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`, and `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` for all new IDs.
  - [ ] Add a diagnostic catalog test that scans constants/descriptors/release notes for duplicate HFC IDs and missing docs links.
  - [ ] Add tests that every story-owned diagnostic message includes the teaching fields.
  - [ ] Add placeholder docs URLs using the existing `https://hexalith.github.io/FrontComposer/diagnostics/HFC{id}` shape until Story 9-5 generates full pages.

- [ ] T8. Counter sample and fixtures (AC16)
  - [ ] Add one valid Level 2/3/4 customization path to the Counter evidence set if not already present from Stories 6-2 through 6-5.
  - [ ] Add a stale contract fixture that compiles and emits the expected version warning without becoming the selected runtime path when major-incompatible.
  - [ ] Add an accessibility-warning fixture with a clear missing-name or focus-suppression violation.
  - [ ] Add a runtime-fault fixture where a custom override throws and the shell remains interactive with only the affected field/template/body replaced by `FcCustomizationDiagnosticPanel`.
  - [ ] Keep Counter evidence focused; do not add a new sample domain solely for diagnostics.

- [ ] T9. Tests and verification (AC1-AC17)
  - [ ] Contracts tests for `CustomizationDiagnostic` immutability, message shape, metadata-only boundary, and version comparison helpers.
  - [ ] SourceTools tests for Level 2 existing HFC1035/HFC1036 behavior plus new Level 3/4/starter version diagnostics.
  - [ ] Analyzer tests for accessible name, clear keyboard anti-patterns, focus suppression, aria-live parity, reduced-motion media query, forced-colors path, and static contrast failure.
  - [ ] Negative analyzer tests proving dynamic or unprovable cases do not create noisy warnings.
  - [ ] Shell/bUnit tests for Level 2/3/4 boundary placement, local fallback rendering, `Recover()` behavior, focus return, localized panel text, screen-reader announcement, reduced-motion/forced-colors CSS, and shell continuity.
  - [ ] Logging tests proving sanitized structured fields and no payload leakage.
  - [ ] Diagnostic catalog tests for ID uniqueness, release-note coverage, descriptor coverage, docs-link shape, and teaching-message completeness.
  - [ ] Counter sample tests for valid customization, stale contract warning, accessibility warning fixture, and runtime-fault isolation.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [ ] Targeted tests: Contracts diagnostics tests, SourceTools analyzer/diagnostic tests, Shell diagnostics/error-boundary tests, and Counter sample build/render tests.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | Public HFC constants already include SourceTools IDs through HFC1037 and runtime log IDs through HFC2114. | Do not renumber existing IDs; add new IDs with clear ownership and XML docs. |
| `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` | SourceTools descriptors exist for generator diagnostics, including Level 2 template HFC1033-HFC1037. | Reuse the category, severity, and descriptor pattern; avoid ad hoc diagnostics. |
| `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` | Release-note table tracks analyzer/generator IDs. | Add every new SourceTools diagnostic; runtime-only Shell logs do not need analyzer release entries. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateContractVersion.cs` | Level 2 contract version source of truth, currently 1.0.0 packed as `Current`. | Keep Level 2 behavior stable; add shared comparison helpers rather than changing the existing constant shape. |
| `src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs` | Descriptor-only registry; duplicate runtime descriptors become ambiguous and resolve to null. | Keep descriptor-only caching and no assembly scans; add stale/major-mismatch diagnostics without caching render state. |
| `src/Hexalith.FrontComposer.Shell/Services/InMemoryDiagnosticSink.cs` | Existing scoped dev diagnostic sink stores bounded recent events. | Reuse or adapt for customization diagnostics; keep bounded memory behavior per L14. |
| `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` | Central source-output registration and diagnostic descriptor lookup. | Keep incremental discipline and deterministic diagnostic reporting. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` | Generated projection integration point for template selection and future slot/view hosts. | Add boundary hooks at override seams only; preserve no-customization fallback output. |

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 6-2 Level 2 templates | 6-6 version/analyzer/boundary validation | HFC1033-HFC1037 remain Level 2-owned; 6-6 extends validation via shared helpers and runtime stale-manifest diagnostics. |
| Story 6-3 Level 3 slots | 6-6 analyzers/error boundary | Slot descriptors and `FieldSlotContext` receive version and accessibility validation; slot host gets narrow boundary. |
| Story 6-4 Level 4 replacements | 6-6 analyzers/error boundary | View replacement descriptors and `ProjectionViewContext` receive version/accessibility validation; view host gets narrow boundary. |
| Story 6-5 dev-mode overlay | 6-6 diagnostic state | Overlay can consume shared diagnostic summaries; 6-6 does not own overlay flows or clipboard UI. |
| Story 9-5 docs site | 6-6 docs links | 6-6 uses stable docs-link placeholders; 9-5 turns them into full Diataxis pages. |
| Story 10-2 visual/accessibility gates | 6-6 analyzers | Static analyzers cover provable cases; visual/specimen CI owns dynamic color, forced-colors, and full browser behavior. |

### Diagnostic Reservation Plan

Use the next free contiguous IDs at implementation time. Do not assume this table is exhaustive if later stories have already allocated IDs before 6-6 starts.

| Proposed ID | Owner | Severity | Trigger |
| --- | --- | --- | --- |
| HFC1010 | SourceTools | Info/Warning | Full rebuild/restart required for unsupported hot-reload metadata change. Reserved earlier; activate if still free. |
| HFC1038-HFC1042 | SourceTools | Warning/Error | Level 3/4 contract mismatch/drift, stale starter template, duplicate/invalid descriptor compatibility where not already owned. |
| HFC1043-HFC1048 | SourceTools | Warning | Accessibility analyzer findings: accessible name, keyboard path, focus suppression, aria-live parity, reduced motion, forced colors/static contrast. |
| HFC2115-HFC2119 | Shell runtime | Warning/Information | Customization boundary fault, stale manifest at runtime, diagnostic panel recovery failure, suppressed activation outside safe host, redaction fallback. |

### Hot Reload / Rebuild Matrix

| Change | Expected behavior |
| --- | --- |
| Razor body edit in template/slot/view component | Hot reload where Blazor supports it; boundary can recover if the component renders successfully after edit. |
| Marker attribute added/removed/changed | Rebuild required; HFC1010-style message names marker metadata change. |
| Expected contract version changed | Rebuild required; SourceTools emits version drift/mismatch after rebuild. |
| Generic `ProjectionTemplateContext<T>`, `FieldSlotContext<T>`, or `ProjectionViewContext<T>` shape changed | Rebuild required; major mismatch suppresses unsafe selection. |
| Slot/view registration added, removed, or duplicated | Rebuild/startup validation required; duplicate selection is never silent. |
| CSS-only focus/reduced-motion/forced-colors fix | Hot reload may apply locally; analyzer/build evidence still required before completion. |
| Framework package upgraded under existing starter templates | Build emits drift/mismatch warning and the dev panel points to regenerate starter templates. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | Story 6-6 centralizes diagnostic shape but does not renumber existing HFC IDs. | Existing story traces and tests already cite HFC1033-HFC1037; renumbering destroys trust. | Reassign all Epic 6 IDs into a fresh contiguous range. |
| D2 | Runtime error boundaries are narrow and placed at override seams. | Broad boundaries hide unrelated failures and can make the whole route look broken. | Wrap the entire shell or route body in one customization boundary. |
| D3 | Static accessibility analyzers emit warnings only for provable issues. | Noisy analyzers get suppressed; Story 10-2 owns dynamic/browser verification. | Warn on every unprovable accessibility case. |
| D4 | Contract major mismatch suppresses unsafe selection; minor drift warns and may proceed. | Preserves safety without blocking additive upgrades. | Block all version drift; accept all drift silently. |
| D5 | Runtime diagnostics log type metadata and categories, not payloads. | Customization failures happen near domain data; logs must not leak tenant or item content. | Log exception.ToString plus render context for convenience. |
| D6 | HFC1010-style rebuild messaging is explicit and user-facing to developers. | Hot reload limits are a dev-loop product surface, not tribal knowledge. | Let stale manifests fail later at runtime. |
| D7 | Story 6-6 feeds diagnostic state to Story 6-5 overlay but does not expand overlay UX. | Keeps the L06 budget focused on validation/isolation. | Add a new diagnostics drawer and workflow in this story. |
| D8 | Counter remains the required sample evidence. | Continuity with prior Epic 6 stories and solo-maintainer scope control. | Add a new diagnostics sample domain. |
| D9 | Docs links use stable placeholders until Story 9-5 generates real pages. | The diagnostic contract needs URLs now; prose docs are a separate documentation story. | Omit docs links until DocFX exists. |
| D10 | Diagnostic catalog tests are release-blocking for this story. | HFC ID drift is hard to discover manually and undermines FR45. | Rely on reviewer discipline. |

### Library / Framework Requirements

- Target the repository's current .NET 10 / Blazor / Fluent UI Blazor / Fluxor / Roslyn SourceTools stack.
- Use Roslyn analyzers for build-time diagnostics; keep SourceTools incremental and deterministic.
- Use Blazor `ErrorBoundary` patterns for runtime isolation, but place boundaries inside interactive override hosts rather than only in static layout.
- Use Fluent UI primitives and existing localization/resource patterns for diagnostic panels.
- Use WCAG 2.2 AA thresholds for contrast and component naming references while preserving the project's WCAG 2.1 AA baseline commitment.
- Keep Contracts dependency-light; do not add ASP.NET Core, Roslyn, or Fluent UI dependencies to Contracts for diagnostic model types.

External references checked on 2026-04-30:

- Microsoft Learn: Blazor error handling / ErrorBoundary: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-10.0
- Microsoft Learn: ASP.NET Core Hot Reload: https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload?view=aspnetcore-10.0
- Microsoft Learn: Roslyn analyzer overview: https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview?view=vs-2022
- NuGet analyzer packaging conventions: https://learn.microsoft.com/nuget/schema/analyzers-conventions
- W3C WCAG 2.2: https://www.w3.org/TR/wcag/
- W3C ARIA14 accessible name technique: https://www.w3.org/WAI/WCAG22/Techniques/aria/ARIA14.html

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/CustomizationDiagnostic.cs` | Shared metadata-only diagnostic contract. |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/CustomizationDiagnosticFormatter.cs` | Teaching-message formatter/helper. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/*ContractVersion.cs` | Level 3/4 version constants if missing after their implementations land. |
| `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` | New HFC10xx descriptors. |
| `src/Hexalith.FrontComposer.SourceTools/Diagnostics/*Accessibility*Analyzer.cs` | Static custom-component accessibility analyzers. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/*ContractVersion*` | Shared contract-version comparison helper. |
| `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` | Analyzer release-note entries. |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | New runtime/source constants. |
| `src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationErrorBoundary.razor` | Narrow boundary wrapper. |
| `src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationDiagnosticPanel.razor` | Localized fault panel. |
| `src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationDiagnosticPanel.razor.css` | Focus/reduced-motion/forced-colors CSS. |
| `src/Hexalith.FrontComposer.Shell/Services/Diagnostics/*` | Diagnostic publishing/redaction helpers if existing sink needs adapters. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` | Boundary/diagnostic integration at override seams. |
| `src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/*` | Runtime stale-manifest diagnostics for Level 2. |
| `samples/Counter/Counter.Web/Components/Diagnostics/*` | Counter diagnostic fixtures. |
| `tests/Hexalith.FrontComposer.Contracts.Tests/Diagnostics/*` | Diagnostic model/version tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/*` | Analyzer, version, catalog tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Diagnostics/*` | Boundary/panel bUnit tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Services/Diagnostics/*` | Redaction/logging tests. |

### Testing Standards

- Release-blocking P0 coverage: version mismatch/drift, stale restart diagnostic, runtime boundary isolation, panel accessibility, log redaction, diagnostic catalog uniqueness, and teaching-message completeness.
- Analyzer tests must include both positive and negative cases. A noisy false-positive analyzer is a product bug.
- Runtime boundary tests must prove sibling generated content and shell navigation remain usable after a failing override renders.
- Use bUnit for panel and boundary behavior; use SourceTools `CSharpGeneratorDriver`/analyzer test harnesses for build-time diagnostics.
- Do not require Playwright or full visual specimen coverage in this story. Story 10-2 owns browser matrix and screenshot baselines.
- Run targeted Contracts, SourceTools, Shell, and Counter tests before closure; run full solution build with warnings as errors.

### Scope Guardrails

Do not implement these in Story 6-6:

- New customization levels or new public override APIs beyond diagnostics/version helpers.
- Command-form customization.
- Authorization policy behavior for commands or projections (Epic 7).
- Story 6-5 overlay UX expansion, before/after flow, or clipboard generation changes except diagnostic-state input.
- Full DocFX diagnostic documentation pages or cookbook prose (Story 9-5).
- Visual specimen CI, Playwright accessibility matrix, or screenshot baseline gates (Story 10-2).
- MCP/agent diagnostic exposure (Epic 8).
- Runtime theme editor, arbitrary design token customization, or dynamic contrast engine.
- Per-tenant/per-user diagnostic policy configuration.
- Logging raw exception text when it contains adopter payloads.

### Non-Goals With Owning Stories

| Non-goal | Owner |
| --- | --- |
| Full diagnostics documentation site and customization cookbook. | Story 9-5 |
| Visual/accessibility specimen CI for override surfaces. | Story 10-2 |
| Adopter test host utilities for customization components. | Story 10-1 |
| Authorization policy integration. | Epic 7 |
| MCP/agent-facing diagnostic resources. | Epic 8 |
| Migration CLI/code fixes for future breaking changes. | Story 9-4 / Story 9-2 |
| Production dev-mode overlay exposure. | Never; explicitly forbidden by Story 6-5. |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Dynamic contrast and browser-computed accessibility states. | Story 10-2 |
| Complete Diataxis diagnostic docs pages for every HFC ID. | Story 9-5 |
| Code fixes for stale contract markers and migration edits. | Story 9-4 / Story 9-2 |
| Component test host APIs for adopter-authored overrides. | Story 10-1 |
| MCP-readable diagnostics and skill-corpus update path. | Epic 8 |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-6-developer-customization-gradient.md#Story-6.6`] - story statement, ACs, FR43/FR44/FR45/FR47.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md`] - Developer Customization & Override System requirements.
- [Source: `_bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md`] - diagnostic teaching shape, package boundaries, hot reload commitment, solo-maintainer sustainability filter.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md`] - WCAG baseline, contrast thresholds, custom override analyzer expectation, diagnostic quality gates.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md`] - FcDevModeOverlay, FcStarterTemplateGenerator, accessibility contract, error isolation context.
- [Source: `_bmad-output/planning-artifacts/architecture.md`] - diagnostic ID policy, circuit-breaker error boundary, generator diagnostics, hot reload limits, error handling principles.
- [Source: `_bmad-output/implementation-artifacts/6-2-level-2-typed-razor-template-overrides.md`] - Level 2 descriptors, HFC1033-HFC1037, template context and registry contracts.
- [Source: `_bmad-output/implementation-artifacts/6-3-level-3-slot-level-field-replacement.md`] - Level 3 slot context, descriptor-only registry, render-default recursion guard, rich diagnostics deferral.
- [Source: `_bmad-output/implementation-artifacts/6-4-level-4-full-component-replacement.md`] - Level 4 view replacement, narrow error boundary, accessibility contract, Story 6-6 deferrals.
- [Source: `_bmad-output/implementation-artifacts/6-5-fcdevmodeoverlay-and-starter-template-generator.md`] - overlay/starter ownership boundary and diagnostic-state handoff.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06`] - defense-in-depth budget.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review before advanced elicitation.
- [Source: `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`] - current HFC constants and runtime log ID pattern.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`] - SourceTools descriptor pattern.
- [Source: `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`] - analyzer release tracking.
- [Source: `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateContractVersion.cs`] - current Level 2 version contract.
- [Source: `src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs`] - descriptor-only runtime registry and duplicate handling.
- [Source: Microsoft Learn: Blazor error handling / ErrorBoundary](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-10.0) - boundary placement and recovery behavior.
- [Source: Microsoft Learn: ASP.NET Core Hot Reload](https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload?view=aspnetcore-10.0) - hot reload support and unsupported edits.
- [Source: Microsoft Learn: Roslyn analyzers overview](https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview?view=vs-2022) - analyzer behavior in IDE/build.
- [Source: NuGet analyzer conventions](https://learn.microsoft.com/nuget/schema/analyzers-conventions) - analyzer packaging/discovery.
- [Source: W3C WCAG 2.2](https://www.w3.org/TR/wcag/) - contrast and UI component accessibility requirements.
- [Source: W3C ARIA14](https://www.w3.org/WAI/WCAG22/Techniques/aria/ARIA14.html) - accessible name technique when visible text cannot be used.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

(to be filled in by dev agent)

### File List

(to be filled in by dev agent)
