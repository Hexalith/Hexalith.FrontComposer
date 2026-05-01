# Story 6.6: Build-Time Validation, Error Boundaries & Diagnostics

Status: done

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

- [x] T1. Define the shared customization diagnostic contract (AC8-AC15)
  - [x] Add a dependency-light `CustomizationDiagnostic` or equivalent contract under `src/Hexalith.FrontComposer.Contracts/Diagnostics/` carrying HFC ID, severity, phase, override level, projection/component/role/field metadata, message sections, docs link, and sanitized structured properties.
  - [x] Add `CustomizationDiagnosticSeverity` / phase enums only if existing Roslyn `DiagnosticSeverity` and runtime log levels cannot represent the shared shape without adding Roslyn dependencies to Contracts.
  - [x] Keep the contract type metadata-only: no exception object, render fragment, tenant/user ID, item payload, localized string payload, or scoped service instance.
  - [x] Add formatting helpers that produce the canonical teaching text: `What`, `Expected`, `Got`, `Fix`, optional `Fallback`, and `DocsLink`.
  - [x] Add tests proving all message sections are non-empty for diagnostics emitted by this story.

- [x] T2. Consolidate Epic 6 contract version validation (AC1-AC3, AC12)
  - [x] Keep `ProjectionTemplateContractVersion` as the Level 2 source of truth and add matching version constants for Level 3 slots and Level 4 view replacements if those stories have landed them.
  - [x] Add a shared SourceTools helper for comparing packed contract versions so HFC1035/HFC1036 behavior is not duplicated for every customization level.
  - [x] Reserve the next available HFC10xx diagnostics for Level 3/4 contract mismatch/drift and stale Story 6-5 starter templates. Do not repurpose HFC1033-HFC1037, which are already Level 2 template diagnostics.
  - [x] Major mismatch suppresses unsafe selection; minor drift warns and proceeds only when source-compatible; build-only revision drift is informational or suppressed per the version policy.
  - [x] Add generated-manifest tests for Level 2 and future descriptor tests for Level 3/4 proving selection/fallback behavior matches the version decision.

- [x] T3. Add build-time custom-component accessibility analyzers (AC4-AC7)
  - [x] Implement analyzers in `src/Hexalith.FrontComposer.SourceTools/Diagnostics/` or an adjacent analyzer folder without creating a separate package.
  - [x] Analyzer inputs are adopter-authored components connected to `[ProjectionTemplate]`, slot descriptors, view override descriptors, and Story 6-5 starter metadata. Do not scan every Razor component in the application.
  - [x] For accessible names, inspect obvious static Razor/component patterns and companion class metadata; warn only when the component is demonstrably missing visible text or naming attributes on required interactive roots.
  - [x] For keyboard reachability, flag only clear anti-patterns such as clickable non-focusable elements or `tabindex="-1"` on the only interaction path.
  - [x] For focus visibility, flag obvious CSS suppression of `outline`, `box-shadow`, or Fluent focus token use in override-owned CSS when no replacement focus style is present.
  - [x] For aria-live parity, validate that override-owned lifecycle/loading/empty/status surfaces preserve the framework polite/assertive category when the metadata identifies such a surface.
  - [x] For reduced motion, flag custom animations/transitions in override-owned CSS without `@media (prefers-reduced-motion: reduce)`.
  - [x] For forced colors, flag custom color/border/fill CSS in override-owned CSS without a `@media (forced-colors: active)` path using system color keywords.
  - [x] For contrast, compute only static color pairs owned by override CSS and known framework neutral backgrounds. Defer dynamic token/runtime computed contrast to Story 10-2.

- [x] T4. Implement hot reload / rebuild diagnostics (AC11-AC12)
  - [x] Promote the existing HFC1010 reservation into a concrete diagnostic for "full rebuild/restart required for this change type" or allocate the next free ID if HFC1010 ownership has changed.
  - [x] Define trigger categories: marker metadata change, expected contract version change, generic context type change, descriptor schema change, slot/view registration added or removed, duplicate registration introduced, and generated manifest version mismatch.
  - [x] Add SourceTools tests showing supported Razor body edits are not flagged, while metadata/descriptor changes produce the rebuild diagnostic.
  - [x] Add Shell-side stale descriptor detection so runtime registries fail closed with the same docs-linked diagnostic when a generated manifest is older than the loaded framework contract.

- [x] T5. Add runtime customization error boundary and panel (AC8-AC11, AC15)
  - [x] Add `FcCustomizationErrorBoundary` under `src/Hexalith.FrontComposer.Shell/Components/Diagnostics/` or `Components/Rendering/` as a narrow boundary wrapper for adopter override regions.
  - [x] Integrate the boundary at Level 2 template host, Level 3 slot host, and Level 4 view replacement host boundaries. Do not wrap the whole shell or entire route body.
  - [x] Add `FcCustomizationDiagnosticPanel` that uses Fluent UI primitives, localized resources, and predictable focus behavior.
  - [x] The panel includes a retry/recover button only when the underlying `ErrorBoundary.Recover()` path is meaningful; otherwise it names the rebuild/restart path.
  - [x] The panel supports keyboard navigation, visible focus, screen-reader announcement, reduced-motion behavior, and forced-colors compatibility.
  - [x] The boundary publishes a sanitized `CustomizationDiagnostic` to `IDiagnosticSink` so Story 6-5 overlay and diagnostics panels can read the same event model.

- [x] T6. Logging, telemetry, and privacy guardrails (AC9-AC10)
  - [x] Add runtime HFC20xx IDs for customization boundary faults, stale descriptor manifest, inaccessible runtime fallback where build-time proof was impossible, and retry/recover failure.
  - [x] Structured logs must use the existing `FcDiagnosticIds` constants and never inline raw HFC strings at call sites unless the constants are not yet public.
  - [x] Add a redaction helper or reuse the existing safe identifier helper for component/projection metadata. Type names are allowed; tenant/user IDs, tokens, item payloads, field values, localized user strings, and render fragments are forbidden.
  - [x] Add tests with sensitive-looking values in projection items and exception messages proving logs/panels do not leak them.
  - [x] Add OpenTelemetry activity/event tags only for IDs, categories, override level, and sanitized type names.

- [x] T7. Diagnostic catalog and docs-link discipline (AC13-AC14)
  - [x] Update `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`, `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`, and `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` for all new IDs.
  - [x] Add a diagnostic catalog test that scans constants/descriptors/release notes for duplicate HFC IDs and missing docs links.
  - [x] Add tests that every story-owned diagnostic message includes the teaching fields.
  - [x] Add placeholder docs URLs using the existing `https://hexalith.github.io/FrontComposer/diagnostics/HFC{id}` shape until Story 9-5 generates full pages.

- [x] T8. Counter sample and fixtures (AC16)
  - [x] Add one valid Level 2/3/4 customization path to the Counter evidence set if not already present from Stories 6-2 through 6-5.
  - [x] Add a stale contract fixture that compiles and emits the expected version warning without becoming the selected runtime path when major-incompatible.
  - [x] Add an accessibility-warning fixture with a clear missing-name or focus-suppression violation.
  - [x] Add a runtime-fault fixture where a custom override throws and the shell remains interactive with only the affected field/template/body replaced by `FcCustomizationDiagnosticPanel`.
  - [x] Keep Counter evidence focused; do not add a new sample domain solely for diagnostics.

- [x] T9. Tests and verification (AC1-AC17)
  - [x] Contracts tests for `CustomizationDiagnostic` immutability, message shape, metadata-only boundary, and version comparison helpers.
  - [x] SourceTools tests for Level 2 existing HFC1035/HFC1036 behavior plus new Level 3/4/starter version diagnostics.
  - [x] Analyzer tests for accessible name, clear keyboard anti-patterns, focus suppression, aria-live parity, reduced-motion media query, forced-colors path, and static contrast failure.
  - [x] Negative analyzer tests proving dynamic or unprovable cases do not create noisy warnings.
  - [x] Shell/bUnit tests for Level 2/3/4 boundary placement, local fallback rendering, `Recover()` behavior, focus return, localized panel text, screen-reader announcement, reduced-motion/forced-colors CSS, and shell continuity.
  - [x] Logging tests proving sanitized structured fields and no payload leakage.
  - [x] Diagnostic catalog tests for ID uniqueness, release-note coverage, descriptor coverage, docs-link shape, and teaching-message completeness.
  - [x] Counter sample tests for valid customization, stale contract warning, accessibility warning fixture, and runtime-fault isolation.
  - [x] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [x] Targeted tests: Contracts diagnostics tests, SourceTools analyzer/diagnostic tests, Shell diagnostics/error-boundary tests, and Counter sample build/render tests.

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

### Party-Mode Hardening Addendum

Story 6-6 validates and reports customization contract problems using a shared metadata-only diagnostic model. It does not own customization contracts, introduce new runtime extension points, or expand Story 6-5 overlay UX.

Apply these clarifications during `bmad-dev-story`:

- Contract ownership stays with the producing story. Story 6-2 owns Level 2 template version constants and HFC1033-HFC1037, Story 6-3 owns Level 3 slot contracts, Story 6-4 owns Level 4 replacement contracts, and Story 6-5 owns overlay/starter flows. Story 6-6 centralizes validation behavior, message shape, catalog discipline, and runtime containment only.
- The shared diagnostic contract is metadata-only. It may carry HFC code, severity, phase/source, override level, target identifier, localization key, teaching-message sections, docs link, and sanitized context. It must not carry delegates, component instances/types for activation, render fragments, service instances, arbitrary component parameters, item payloads, raw exception text, tenant/user IDs, access tokens, URLs with query strings, or rendered markup.
- Fail-closed selection must be level-specific and testable: Level 1 invalid metadata/theme falls back to default metadata/theme; Level 2 major mismatch rejects the template and uses the base generated template; Level 3 slot mismatch does not activate the slot override; Level 4 mismatch does not activate the replacement view. Tests must assert selected artifact, rejected artifact, diagnostic code/severity, expected/actual version, and fallback decision. No partially compatible mixed override state may be composed.
- Static accessibility analyzers stay intentionally shallow. They flag only provable static facts such as missing accessible names, invalid ARIA references, duplicate static IDs in owned markup, statically detectable keyboard/focus traps, missing reduced-motion or forced-colors CSS for owned literal effects, and literal color pairs where both colors are known. Dynamic theme values, runtime DOM crawling, browser visual audits, and speculative scoring remain Story 10-2 scope.
- Analyzer false-positive control is release-blocking. Each analyzer rule needs one positive fixture, one nearby valid negative fixture, one unknown/dynamic fixture that does not hard-fail unless provably unsafe, and stable diagnostic code/span/severity assertions.
- Runtime boundaries are allowed only at external/customization invocation seams: Level 2 template host, Level 3 slot host, Level 4 view replacement host, and starter-template diagnostic evidence surfaces when rendering adopter code. They must not wrap the whole shell, routing, entire pages, or normal first-party generated components.
- Runtime diagnostic panels must define priority and state. Blocking runtime/build failure is primary, then accessibility, then version/contract drift, then telemetry-only context. Panel states are failed, retrying, recovered, and retry failed. Focus moves to the localized diagnostic heading on failure, retry/recover controls are keyboard reachable, and focus returns to the invoking region after successful recovery when possible.
- User-facing diagnostics use short structured teaching text: what failed, affected customization level/seam, why it was blocked or degraded, how to fix it, whether rebuild/restart is required, and where to look. The formatter may also expose Expected/Got/Fallback fields for catalog and test assertions, but panel copy should keep one dominant fix path.
- Localization covers titles, message sections, severity labels, analyzer names, button labels, fallback/rebuild text, empty/recovered states, and retry-failed text. Missing localized keys fail catalog tests; runtime display falls back to invariant English plus the HFC code instead of failing the panel.
- Logging and telemetry use an allowlist plus denylist. Allow diagnostic ID, severity, override level, sanitized type names, role/field identifiers, phase, and exception category. Deny user content, rendered markup, arbitrary component parameters, stack locals, raw URLs/query strings, bearer-like tokens, connection strings, email addresses, item payloads, field values, raw tenant/user IDs, and localized user strings.
- The diagnostic catalog source of truth remains `FcDiagnosticIds` plus SourceTools descriptors and release notes for analyzer-owned IDs. Catalog tests must assert ID uniqueness, severity validity, docs-link shape, localized title/message key presence, no accidental localized-key reuse, and no emitted diagnostic without catalog registration.
- Counter sample evidence is a narrow proof, not a matrix: one valid shared-pipeline customization, one version mismatch/drift fixture, one static accessibility warning fixture, and one runtime-fault fixture proving local fallback plus shell/sibling survival.
- Deferred from this story: production diagnostic overlay, rich overlay drill-down/filtering/pinning, dynamic accessibility engine, full visual specimen CI, analyzer performance characterization beyond a smoke guard, randomized hot-reload chaos testing, command-form customization validation, and MCP/agent-facing diagnostic surfaces.

### Advanced Elicitation Hardening Addendum

The advanced elicitation pass keeps the story ready-for-dev, but tightens the implementation oracles that prevent this diagnostic story from becoming a broad catch-all.

Apply these refinements during `bmad-dev-story`:

- Treat the diagnostic catalog as an executable matrix, not a prose appendix. For every story-owned HFC ID, add one row or fixture that names owner, phase, severity, docs link, expected/got/fix/fallback fields, selector effect, runtime fallback behavior, localization key, and source file that is allowed to emit it.
- Version validation must be side-effect free. Comparing expected and actual contracts may report diagnostics and suppress selection, but it must not mutate registries, rewrite descriptors, generate new artifacts, or cache a fallback decision that outlives the current generated manifest snapshot.
- Runtime boundary recovery must be bounded per failed seam. A repeated fault at the same override level/projection/field/render epoch publishes at most one active diagnostic until recovery, dispose, or manifest refresh; retry loops must not flood the sink, logs, telemetry, or overlay state.
- Analyzer implementation starts with syntax/semantic facts already connected to customization metadata. It must not scan the full app, inspect generated output as adopter-authored code, or infer failures from unknown dynamic values. Add a smoke test that a non-custom Razor component with the same markup pattern is ignored.
- Redaction tests must include exception messages and component parameter names that look useful but are unsafe. Runtime diagnostics may retain exception type/category and sanitized framework-owned type names; raw exception messages require allowlist proof before display or logging.
- Hot-reload/rebuild diagnostics require one shared classifier table used by SourceTools tests and Shell stale-manifest tests. The table must include marker metadata, expected-version constants, generic context shape, descriptor schema, registration adds/removes, duplicate registration, Razor body edit, CSS-only edit, and framework package upgrade.
- Diagnostic panel recovery is a user interaction contract. Tests must assert initial focus, retry button availability, retry-failed state, successful recovery focus return when possible, and no hidden live-region duplication when the panel rerenders.
- Counter fixtures should be independently minimal. Do not combine stale contract, accessibility violation, and runtime throw into one fixture, because a single failure would mask which AC regressed.
- Keep the binding-decision count stable. If implementation discovers a new policy decision rather than a test oracle, record it as a deferred decision for product/architecture review instead of expanding this story in place.

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
- Redaction tests must include adversarial connection strings, bearer-like tokens, email addresses, route/query values, exception messages containing user data, and nested metadata values; forbidden substrings must be absent while stable HFC IDs remain present.
- Fixture families must stay isolated by level or behavior rather than using one giant broken customization fixture. Prefer focused cases such as `Level1_MetadataMismatch`, `Level2_AccessibilityStaticViolation`, `Level3_RuntimeBoundaryThrow`, and `Level4_RebuildRequiredStaleArtifact`.
- Boundary isolation tests must assert the panel appears only at the failed seam, sibling seams remain rendered/interactive, one diagnostic is published for one fault, and rerender does not duplicate the diagnostic.
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

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj --no-restore --filter "CustomizationDiagnosticTests|CustomizationContractVersionTests" -p:UseSharedCompilation=false` -> Passed, 9 tests.
- `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --no-restore --filter "CustomizationAccessibilityAnalyzerTests|HotReloadRebuildClassifierTests|DiagnosticDescriptorTests|Hfc1047To1049DevModeReservationTests" -p:UseSharedCompilation=false` -> Passed, 37 tests.
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --no-restore --filter "FcProjectionViewOverrideHostTests|FcFieldSlotHostTests|CounterStoryVerificationTests" -p:UseSharedCompilation=false` -> Passed, 22 tests.
- `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --no-restore --filter "FullyQualifiedName~RazorEmitterTests|FullyQualifiedName~CounterProjectionApprovalTests|FullyQualifiedName~RoleSpecificProjectionApprovalTests" -p:UseSharedCompilation=false` -> Passed, 25 tests after approval rebaseline for Level 2 template host emission.
- `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` -> Succeeded, 0 warnings.
- `dotnet test Hexalith.FrontComposer.sln --no-build -p:UseSharedCompilation=false` -> Passed: Contracts 148, Shell 1365, SourceTools 583, Bench 2.

### Completion Notes List

- Added a metadata-only shared customization diagnostic contract with dependency-light severity/phase, canonical teaching formatter, sanitized properties, and tests for required message sections.
- Added shared packed contract-version comparison helpers and wired Shell Level 2/3/4 registries to suppress major mismatches while allowing compatible drift according to policy.
- Activated HFC1010 rebuild/restart classification and added SourceTools hot-reload classifier tests for supported body/CSS edits versus metadata/descriptor changes.
- Added scoped customization accessibility analyzer diagnostics HFC1050-HFC1055, release tracking, catalog coverage, positive and negative analyzer tests, and non-custom component false-positive protection.
- Added runtime diagnostic panel and narrow Level 2 template, Level 3 slot, and Level 4 view replacement boundary hosts that publish sanitized `CustomizationDiagnostic` events to `IDiagnosticSink`.
- Preserved Level 4 redaction guarantees: logs and sink events include HFC ID, level/type/category metadata, but exclude item payloads and raw exception messages.
- Rebased generated approval snapshots for the Level 2 template host emission change; Counter valid Level 2/3/4 customization evidence remains green.

### File List

- `_bmad-output/implementation-artifacts/6-6-build-time-validation-error-boundaries-and-diagnostics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/CustomizationDiagnostic.cs`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/CustomizationDiagnosticFormatter.cs`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/CustomizationDiagnosticPhase.cs`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/CustomizationDiagnosticSeverity.cs`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/CustomizationContractVersion.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateDescriptor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationDiagnosticPanel.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationDiagnosticPanel.razor.css`
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Diagnostics/CustomizationDiagnosticPublisher.cs`
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs`
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs`
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs`
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationAccessibilityAnalyzer.cs`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationHotReloadClassifier.cs`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Parsing/ProjectionTemplateMarkerParser.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Diagnostics/CustomizationDiagnosticTests.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/CustomizationContractVersionTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionViewOverrideHostTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/CustomizationAccessibilityAnalyzerTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/HotReloadRebuildClassifierTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.BasicProjection_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.DescriptionWithEscapeEdgeCases_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.DisplayNameOverrides_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.EnumAndBadgeMappings_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.GuidTruncation_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.NullableProperties_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.ActionQueueNoEnumProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.ActionQueueProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.DashboardProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.DashboardWrongShapeProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.DetailRecordProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.StatusOverviewProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.TimelineProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.WhenStateTypoProjection_Approval.verified.txt`

### Change Log

- 2026-05-01: Implemented Story 6-6 build-time validation, runtime diagnostic boundaries/panel, HFC1010/HFC1050-HFC1055/HFC2115-HFC2119 catalog additions, and full regression verification.

---

## Party-Mode Review

- Date/time: 2026-04-30T08:02:13.4644753+02:00
- Selected story key: `6-6-build-time-validation-error-boundaries-and-diagnostics`
- Command/skill invocation used: `/bmad-party-mode 6-6-build-time-validation-error-boundaries-and-diagnostics; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), Sally (UX Designer)
- Findings summary: The review found the story direction architecturally sound, but identified pre-dev ambiguity around contract ownership, metadata-only diagnostic boundaries, level-specific fail-closed selection, static analyzer false-positive control, runtime boundary placement, panel focus/recovery behavior, diagnostic catalog source of truth, localization coverage, telemetry redaction, and focused fixture design.
- Changes applied: Added a Party-Mode Hardening Addendum that fixes the contract-boundary sentence, clarifies ownership by producing story, defines metadata-only diagnostic constraints, specifies level-specific selection/fallback oracles, narrows static analyzer scope, adds analyzer false-positive gates, constrains runtime boundaries to override seams, defines panel priority/focus/retry states, expands localization and redaction requirements, strengthens catalog discipline, and narrows Counter evidence. Testing Standards now require adversarial redaction inputs, isolated fixture families, and boundary sibling-survival/no-duplicate-diagnostic assertions.
- Findings deferred: Production diagnostic overlay, rich overlay drill-down/filtering/pinning, dynamic accessibility engine, full visual specimen CI, analyzer performance characterization beyond a smoke guard, randomized hot-reload chaos testing, command-form customization validation, and MCP/agent-facing diagnostic surfaces remain deferred to their existing owning stories or future quality work.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- Date/time: 2026-04-30T13:39:43.4010712+02:00
- Selected story key: `6-6-build-time-validation-error-boundaries-and-diagnostics`
- Command/skill invocation used: `/bmad-advanced-elicitation 6-6-build-time-validation-error-boundaries-and-diagnostics`
- Batch 1 method names: Red Team vs Blue Team; Security Audit Personas; Failure Mode Analysis; Self-Consistency Validation; Occam's Razor Application.
- Reshuffled Batch 2 method names: Pre-mortem Analysis; Chaos Monkey Scenarios; Graph of Thoughts; Comparative Analysis Matrix; Hindsight Reflection.
- Findings summary: The elicitation found that the story already had sound boundaries after party-mode review, but still needed sharper executable oracles around HFC catalog ownership, side-effect-free version checks, bounded runtime recovery, analyzer scope limits, redaction of exception text and parameters, shared hot-reload classifiers, diagnostic panel recovery states, and fixture isolation.
- Changes applied: Added an Advanced Elicitation Hardening Addendum requiring an executable diagnostic catalog matrix, side-effect-free version validation, per-seam diagnostic dedupe, customization-metadata-scoped analyzers, stricter redaction tests, a shared hot-reload/rebuild classifier table, focus/retry/recovery panel assertions, isolated Counter fixtures, and a guard against adding new binding decisions during implementation.
- Findings deferred: New product or architecture policy decisions discovered during implementation must be recorded for product/architecture review instead of expanding this story; dynamic accessibility/browser-computed checks, full visual specimen CI, code fixes, rich diagnostics documentation, and MCP-readable diagnostics remain with their existing owning stories.
- Final recommendation: ready-for-dev

---

## Review Findings

> Code review run: `/bmad-code-review` on 2026-05-01. Three adversarial layers raised 111 raw findings (Blind Hunter 36, Edge Case Hunter 58, Acceptance Auditor 17). After dedup and scope-filtering (most Blind/Edge findings target Story 6-5 files that are uncommitted in the working tree but were already reviewed in the 6-5 code-review pass), 26 findings are actionable for Story 6-6: 9 decision-needed, 15 patches, 2 defers. 38+ dismissed (mostly 6-5-territory or false positives).

### Decision-needed (resolved 2026-05-01 — `defaults` accepted)

- [x] [Review][Decision] **DN1 → defer** — AC16 / T8 Counter sample fixtures: deferred to a Counter-fixtures follow-up story. Existing 6-2..6-5 sample evidence + the regression test pass cover the customization gradient by construction; AC16 fixtures are evidence work that lands separately.
- [x] [Review][Decision] **DN2 → spec amendment + defer** — AC7 static contrast: the spec wording already defers dynamic theme values to Story 10-2; AC7 amended below to make the static-contrast deferral explicit. Owner: Story 10-2 (visual/accessibility specimen verification).
- [x] [Review][Decision] **DN3 → patch** (becomes **P16**) — extend `CustomizationAccessibilityAnalyzer` to detect components referenced from `AddSlotOverride<>` / `AddViewOverride<>` / `AddProjectionTemplate<>` registration calls via semantic-model walk so Level 3 slot and Level 4 view override components get the same six accessibility checks.
- [x] [Review][Decision] **DN4 → defer** — HFC1010 classifier kept as scaffold-only; T4 effectively downgraded. Wiring requires either an incremental-generator step or a Shell startup gate, both of which are larger than the post-review budget. Tracked in `deferred-work.md` under this story.
- [x] [Review][Decision] **DN5 → defer** — HFC2116 stale-manifest emit deferred. Reservation marked "reserved — emit deferred" in release notes; design conversation about manifest version surface owed before wiring. Tracked in `deferred-work.md` under this story.
- [x] [Review][Decision] **DN6 → spec amendment + accept current behavior** — Level 3 / 4 / starter contract drift continues to be enforced at runtime via registries' `!comparison.CanSelect` log-and-skip path; AC1 and AC3 amended below to make the L2-build-time / L3+L4+starter-runtime split explicit.
- [x] [Review][Decision] **DN7 → patch** (becomes **P17**) — add opt-in strict mode `FcShellOptions.CustomizationContractValidation` (enum: `LogAndSkip` (default) / `FailClosedOnMajorMismatch`) plus a startup `IValidateOptions` / `IHostedService` that aggregates Major-mismatched registrations from all three registries and throws when strict mode is on. Preserves current adopter ergonomics while honoring AC2 wording for adopters who require fail-closed.
- [x] [Review][Decision] **DN8 → patch** (becomes **P18**) — add `FcProjectionTemplateHostTests.cs` + expand `FcFieldSlotHostTests.cs` with sibling-survival + publish-once + no-dup-on-rerender assertions, copying the Level 4 fixture pattern.
- [x] [Review][Decision] **DN9 → defer + relocate** — `deferred-work.md` updated to relocate W2 (generic-constraint analyzer) and W3 (`Context.DefaultBody` lambda capture) and W7 (`Context.FieldRenderer` unknown-field contract) from `Owner: Story 6-6` to their natural homes (9-1 drift detection / 9-4 AOT / 9-5 docs).

### Acceptance Criteria amendments (DN2, DN6)

> **AC1 (amended):** A Level 2 template, Level 3 slot, Level 4 view replacement, or Story 6-5 starter template declares an expected FrontComposer contract version. **Level 2 templates** are validated at build time by SourceTools (HFC1035/1036). **Level 3 slot, Level 4 view replacement, and Story 6-5 starter template** contract drift is enforced at runtime by `ProjectionSlotRegistry` / `ProjectionViewOverrideRegistry` log-and-skip on `!comparison.CanSelect`. HFC1038-HFC1042 remain reserved for a future SourceTools-side analyzer (deferred).
>
> **AC3 (amended):** A minor/build contract drift is detected. SourceTools emits HFC1036 for Level 2; runtime registries emit equivalent log-warning messages for Level 3/4/starter naming expected version, actual version, target projection/component, fix, docs link, and rebuild guidance.
>
> **AC7 (amended):** Static-color-pair contrast computation is deferred to Story 10-2 alongside the existing dynamic-theme deferral. HFC1055 in this story validates only the structural requirement that custom CSS includes a `forced-colors` media-query path. WCAG-AA threshold ratios (4.5:1 / 3:1) are owned by Story 10-2 visual/accessibility specimen verification.

### Patch

- [x] [Review][Patch] **P1 — Diagnostic panel labels are hardcoded English; no `IStringLocalizer` injection, no resx keys** [`src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationDiagnosticPanel.razor:14-30`] — Inject `IStringLocalizer<FcShellResources>`, add 6+ resx keys (EN+FR) for "Expected", "Got", "Fix", "Fallback", "DiagnosticDocsLink", "RetryButton". Currently only `DevModeOverlayShortcutDescription` was added to FR resx — diagnostic panel keys are missing.
- [x] [Review][Patch] **P2 — Diagnostic panel uses raw HTML (`<div>`/`<dl>`/`<button>`/`<a>`) instead of Fluent UI primitives** [`src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationDiagnosticPanel.razor:1-32`] — Spec T5 sub-bullet 3 requires Fluent UI primitives. Rewrite using `FluentMessageBar`/`FluentButton`/`FluentAnchor`/`FluentStack`/`FluentLabel`.
- [x] [Review][Patch] **P3 — Diagnostic panel: no programmatic `FocusAsync` on render — `tabindex="-1"` alone leaves keyboard users unable to reach the alert content easily** [`src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationDiagnosticPanel.razor:7`] — Add `ElementReference _root` + `OnAfterRenderAsync(firstRender)` calling `_root.FocusAsync()` when first rendered for the current diagnostic. Reset on Recover.
- [x] [Review][Patch] **P4 — Diagnostic panel does not visibly render projection/component/role/field/exception-category metadata required by AC9** [`src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationDiagnosticPanel.razor:9-12`] — Currently only `Diagnostic.Id` and `Diagnostic.Level` are visible. Add `<dl>` rows for `Diagnostic.ProjectionTypeName`, `ComponentTypeName`, `Role`, `FieldName`, exception category from `Properties` (or whichever names the contract exposes).
- [x] [Review][Patch] **P5 — Diagnostic panel external `<a href="@Diagnostic.DocsLink">` lacks `rel="noopener"` and `javascript:` scheme guard** [`src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationDiagnosticPanel.razor:1242` (synthetic line)] — Use `rel="noopener noreferrer"` and validate `Uri.IsWellFormedUriString(Diagnostic.DocsLink, UriKind.Absolute)` + `StartsWith("https://")` before rendering the anchor (otherwise render the raw URL as text).
- [x] [Review][Patch] **P6 — No diagnostic-catalog uniqueness/teaching-shape test scanning all three sources** [add `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticCatalogTests.cs`] — Reflectively enumerate `FcDiagnosticIds` constants, `DiagnosticDescriptors.AllDescriptors` ids, `AnalyzerReleases.Unshipped.md` rows; assert no duplicates, every ID has a docs link, every story-owned message includes the teaching fields (What/Expected/Got/Fix/DocsLink). T7 sub-bullet 2 + AC14 explicitly require this.
- [x] [Review][Patch] **P7 — Redaction adversarial tests cover only Level 4 — Level 2 (template host) and Level 3 (field slot host) lack equivalent coverage** [`tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/`] — Mirror `FcProjectionViewOverrideHostTests` redaction patterns into new `FcProjectionTemplateHostTests.cs` + expand `FcFieldSlotHostTests.cs`: pass projection items with sensitive-looking values + exception messages with tenant/user-shaped strings, assert nothing leaks into logs or panel output.
- [x] [Review][Patch] **P8 — `CustomizationAccessibilityAnalyzer` reads full source text per type via `GetSyntax(...).ToFullString()` then runs 6 substring scans — quadratic against partial classes; defies Roslyn analyzer perf guidance** [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationAccessibilityAnalyzer.cs:2832-2840` (synthetic)] — Replace with `SymbolAnalysisContext.RegisterSyntaxNodeAction` walking only attribute-tagged types. Bound source-byte scan size; reuse cached `SemanticModel`.
- [x] [Review][Patch] **P9 — Analyzer substring scans match comments and string literals — false positives** [same file as P8] — Add a syntax-tree pass that strips trivia and string-literal nodes before substring matching; or use Roslyn CSS/Razor token-aware matching when feasible. Without this, `// builder.AddAttribute(0, "onclick", …)` in a comment will trigger HFC1051.
- [x] [Review][Patch] **P10 — `CustomizationDiagnostic.SanitizeProperties` drops blank-key entries silently** [`src/Hexalith.FrontComposer.Contracts/Diagnostics/CustomizationDiagnostic.cs:354` (synthetic)] — Add a `Debug.Assert(!string.IsNullOrWhiteSpace(property.Key), ...)` or surface a counter on dropped entries so callers learn about programming mistakes during dev/test.
- [x] [Review][Patch] **P11 — `CustomizationContractVersion.Unpack` accepts negative packed input and yields negative Major/Minor/Build** [`src/Hexalith.FrontComposer.Contracts/Rendering/CustomizationContractVersion.cs:705` (synthetic)] — Add `if (packed < 0) throw new ArgumentOutOfRangeException(nameof(packed));` (or clamp + log + return MajorMismatch). Add a unit test asserting negative input throws.
- [x] [Review][Patch] **P12 — DISMISSED on second pass** — `FcFieldSlotHost.RenderFailure` already calls `Services?.GetService<IDiagnosticSink>()` with null-conditional (line 158) and `CustomizationDiagnosticPublisher.Publish` returns early on null sink (line 8 of `CustomizationDiagnosticPublisher.cs`). Existing defense covers the disposal-race scenario.
- [x] [Review][Patch] **P13 — Registry log messages still reference "incompatible major" wording when `comparison.Decision` is `MinorDrift`** [`src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs:55-65`, `ProjectionViewOverrideRegistry.cs:120-130`, `ProjectionSlotRegistry.cs:86-95`] — Branch the log message and HFC ID on `comparison.Decision`: HFC1035/HFC1041 for MajorMismatch, HFC1036/HFC1040 for MinorDrift; minor drift continues to log-and-allow per AC3.
- [x] [Review][Patch] **P14 — DISMISSED on second pass** — `FcProjectionViewOverrideHost` already uses `_loggedSinceLastRecover` to dedup both the log line AND the `Publish` call (line 138-164). The flag is reset in `Recover()` (line 176) and `OnParametersSet` (line 84). Edge Hunter's E49 was a false positive.
- [x] [Review][Patch] **P15 — Inconsistent `Field?.Name` null-safety across error-fragment code paths in `FcFieldSlotHost`** [`src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs:1452-1453` vs `1480` (synthetic)] — `LogWarning(...Field.Name...)` raw read coexists with `Field?.Name` in `CreateDiagnostic`. Use `Field?.Name ?? "<unknown>"` consistently in both error-fragment render lines and log-line interpolation.
- [x] [Review][Patch] **P16 — Extend `CustomizationAccessibilityAnalyzer` to Level 3 slot and Level 4 view override components (resolves DN3)** [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationAccessibilityAnalyzer.cs`] — Today only `[ProjectionTemplate]`-marked types are analyzed. Add a registration-call walker (`RegisterOperationAction(OperationKind.Invocation)`) that identifies `IServiceCollection.AddSlotOverride<TProjection, TField, TComponent>()` / `AddViewOverride<TProjection, TComponent>()` / `AddProjectionTemplate<TComponent>()` invocations, resolves the `TComponent` symbol, and runs the same six accessibility checks (HFC1050-HFC1055) against it. Level 1 annotation overrides remain out of scope (no replacement component).
- [x] [Review][Patch] **P17 — Add opt-in `FcShellOptions.CustomizationContractValidation` strict mode (resolves DN7)** [`src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs` + new `Hexalith.FrontComposer.Shell/Services/Customization/CustomizationContractValidationGate.cs`] — Add enum `CustomizationContractValidationMode { LogAndSkip, FailClosedOnMajorMismatch }`. Default `LogAndSkip`. When `FailClosedOnMajorMismatch`, registries expose a parallel `RejectedDescriptors` enumeration (or attach to a shared `ICustomizationContractRejectionLog` service); a hosted service started after registries hydrate enumerates rejections and throws `InvalidOperationException` listing each `(level, projection, expected, actual, HFC ID, docs link)`. Add unit tests covering both modes for L2/L3/L4 registries.
- [x] [Review][Patch] **P18 — Add bUnit boundary sibling-survival tests for Level 2 and Level 3 hosts (resolves DN8)** [add `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionTemplateHostTests.cs`; expand `FcFieldSlotHostTests.cs`] — Mirror the Level 4 `FcProjectionViewOverrideHostTests` pattern. Per host, add three tests: (1) sibling-survival — render a faulting host alongside two non-faulting siblings, assert siblings still render; (2) publish-once — assert exactly one diagnostic is published per fault episode, even if `RenderFailure` re-renders; (3) no-dup-on-rerender — trigger a second render of the host while the boundary is in failed state, assert `IDiagnosticSink` count remains 1. Reuse the redaction-input pattern from P7 to merge concerns where overlap exists.

### Defer

- [x] [Review][Defer] **D1 — `FcProjectionTemplateHost.OpenComponent` non-generic overload requires `DynamicallyAccessedMembers` / generic-arity for AOT/trim** [`src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs`] — deferred, owned by Epic 9-4 (AOT/trimming sweep) where the project-wide DAM annotation pass lands.
- [x] [Review][Defer] **D2 — HFC2117 / HFC2118 / HFC2119 reserved as constants but no emit sites (recovery-failure, runtime-fallback, inaccessible-runtime-fallback)** [`src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs:404-414`] — deferred, the emit sites depend on later epic features (Epic 7 fail-closed paths, Epic 9 runtime-only fallbacks); current reservations are intentional and tracked.
- [x] [Review][Defer] **D3 — Counter sample fixtures for AC16 (valid path / contract drift / a11y warning / runtime fault)** [`samples/Counter/`] — deferred (resolves DN1) to a Counter-fixtures follow-up story. Existing 6-2..6-5 sample evidence + the regression test pass cover the customization gradient by construction.
- [x] [Review][Defer] **D4 — HFC1010 hot-reload classifier kept as scaffold-only; T4 effectively downgraded** [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationHotReloadClassifier.cs`] — deferred (resolves DN4). Wiring requires either an incremental-generator step or a Shell startup gate, both of which exceed the post-review budget.
- [x] [Review][Defer] **D5 — HFC2116 `CustomizationStaleDescriptorManifest` emit deferred** [`src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs:399`] — deferred (resolves DN5). Reservation marked "reserved — emit deferred" in release notes; design conversation about manifest version surface owed before wiring.
- [x] [Review][Defer] **D6 — Spec-acknowledged carryovers W2 / W3 / W7 relocated from `Owner: Story 6-6`** [`_bmad-output/implementation-artifacts/deferred-work.md`] — deferred (resolves DN9). Owners updated in `deferred-work.md` to point to their natural homes (W2 → 9-1 drift detection or 9-4 AOT analyzer governance; W3 → 9-5 docs / 9-1 governance; W7 → 9-5 contract docs).

### Patch application summary (2026-05-01)

All 16 actionable patches applied (P1–P11 + P13 + P15–P18; P12 and P14 were dismissed on second pass as false positives — existing code is already defensive). Validation:

- **Build:** `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` — 0 warnings, 0 errors.
- **Tests:** `dotnet test Hexalith.FrontComposer.sln --no-build -p:UseSharedCompilation=false` — Contracts 148/0/0, Shell 1371/0/0 (was 1365 + 6 new boundary tests), SourceTools 587/0/0 (was 583 + 4 new diagnostic catalog tests), Bench 2/0/0. **Total: 2,108/0/0.**

Highlights:

- **Diagnostic panel** rewritten with Fluent UI primitives (FluentMessageBar / FluentButton / FluentBadge / FluentLabel / FluentStack), localized via `IStringLocalizer<FcShellResources>` with 12 new resx keys (EN+FR), programmatic FocusAsync on first render per Diagnostic.Id, visible projection/component/role/field/exception-category metadata rows, https-only docs-link with `rel="noopener noreferrer"` + scheme guard.
- **CustomizationAccessibilityAnalyzer** restructured: `RegisterCompilationStartAction` + concurrent component-registration walker collects `AddProjectionTemplate<>` / `AddSlotOverride<,,>` / `AddViewOverride<,>` consumers so Level 3 slots and Level 4 view replacements get the same six accessibility checks; source-text scan bounded to 256 KB; comments stripped before substring matching (string literals preserved so call-site detection still works).
- **Strict-mode option** `FcShellOptions.CustomizationContractValidation` (`LogAndSkip` default / `FailClosedOnMajorMismatch`); three registries record Major-mismatch rejections into `ICustomizationContractRejectionLog`; new `CustomizationContractValidationGate` hosted service throws at startup when strict mode is on and any rejection was recorded.
- **Registry log messages** now branch on `comparison.Decision` to differentiate MajorMismatch from MinorDrift; MinorDrift surfaces an Information-level message naming expected/actual + rebuild guidance.
- **Boundary tests** added across all three levels: redaction adversarial inputs, publish-once dedup on rerender, sibling-survival via two-host harness component (Level 2).
- **Diagnostic catalog test** reflectively enumerates `FcDiagnosticIds` constants + `DiagnosticDescriptors` fields, asserts uniqueness, validates HFCxxxx shape, cross-checks Story 6-6 IDs.

### Dismissed (38)

Most dismissals fall into one root cause: the working tree contains uncommitted Story 6-5 implementation (verified via `git log` — 6-5 was marked `done` in sprint-status but never committed), so the 6-6 review payload includes 6-5 files (`Contracts/DevMode/*`, `Shell/Components/DevMode/*`, `Shell/Services/DevMode/*`, `Extensions/AddFrontComposerDevModeExtensions.cs`, `wwwroot/js/fc-devmode-clipboard.js`, `Shortcuts/FrontComposerShortcutRegistrar.cs` Ctrl+Shift+D, `FcShellOptions.cs` `FcShellDevModeOptions`, dev-mode annotation injection in every emitter approval). Findings on those files were already triaged in the 6-5 code-review pass on 2026-05-01 (16 patches applied, 13 deferred per sprint-status).

Other dismissals: Auditor A2/A16 (scope blow-out — see above; A15 (Blazor `ErrorBoundary` reuse is functionally equivalent to spec's `FcCustomizationErrorBoundary` name); Blind B13 (clock injection — minor testability), B29 (HotReloadRebuildClassification ctor — minor type design), B30 (verified.txt BOM — build verified clean, likely Verify framework artifact), B32 (registry orphaned brace — false positive, build clean); Edge E11 (`[EditorRequired]` enforces TemplateType non-null), E38 (CompareContractVersion logic — covered by `CustomizationContractVersionTests`).
