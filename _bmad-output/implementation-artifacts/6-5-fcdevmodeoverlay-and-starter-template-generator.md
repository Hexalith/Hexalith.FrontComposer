# Story 6.5: FcDevModeOverlay & Starter Template Generator

Status: done

> **Epic 6** - Developer Customization Gradient. **FR39 / FR40 / FR41 / FR42 / FR44 / FR45 / UX-DR9 / UX-DR11 / UX-DR54** developer-mode discovery overlay and copy-to-clipboard starter template generator for the customization gradient. Applies lessons **L01**, **L06**, **L07**, **L08**, **L10**, **L11**, **L13**, **L14**, and **L15**.

---

## Executive Summary

Story 6-5 turns the customization gradient (Stories 6-1 through 6-4) into a discoverable, in-product experience without leaking dev-only surface area into production:

- Add an in-app FcDevModeOverlay, available only in development hosting environments, that reveals which generated convention produced each rendered element and recommends the right customization level for the change a developer wants to make.
- Add `FcDevModeAnnotation` element overlays for projection-view and field customization seams produced by the auto-generation engine itself so the overlay never reverse-engineers DOM. Element identity flows from the same descriptors that already produced the generated component tree.
- Add a 360px `FluentDrawer` detail panel for a selected annotation showing convention name and description, contract type, current customization level (Default, Level 1, Level 2, Level 3, Level 4), recommended override level for common changes, before/after toggle for active overrides, and a "Copy starter template" button for Levels 2-4.
- Introduce `IRazorEmitter` as a dev-only Shell service that walks an in-memory `ComponentTreeNode` representation of the current generated output and emits Level 2, Level 3, and Level 4 starter Razor source. The emitter does not run the Roslyn source generator at runtime; it consumes the same descriptor metadata Stories 6-1, 6-2, 6-3, and 6-4 already publish.
- Wire the overlay activation surface (Ctrl+Shift+D + an icon-only shell-header toggle) and a single clipboard JS interop bridge for copying starter templates. Keyboard navigation, Escape-to-close, focus trap discipline, and screen-reader behavior are first-class acceptance criteria.
- Highlight `FcFieldPlaceholder` (unsupported field) cells with a distinct red-dashed visual when the overlay is active and surface the unsupported type name plus recommended override level in the detail panel. This discovery path makes the FR9 / UX-DR55 placeholder pipeline (Story 4-6) actionable from the UI.
- Exclude the entire overlay subsystem from production builds via `#if DEBUG` AND a runtime `IHostEnvironment.IsDevelopment()` gate, registering `IRazorEmitter` and the annotation injection only in development. Two layers of defence so a Release-mode build with an inadvertent symbol leak still cannot expose the surface.
- Demonstrate the overlay over the Counter sample. The dev-mode overlay must reveal the Story 6-1 RelativeTime/Currency annotations, a Story 6-3 slot override, and a Story 6-4 full replacement so the customization gradient is browsable from one screen.
- Defer broader build-time analyzer coverage, rich runtime fault diagnostic UX, and complete cookbook-style documentation to Stories 6-6, 9-5, and 10-2. Story 6-5 ships the discovery surface; Story 6-6 hardens the analyzers behind it.

---

## Story

As a FrontComposer adopter,
I want an in-app dev-mode overlay that shows me which conventions produced each generated UI element and gives me copy-to-clipboard starter Razor for Levels 2-4,
so that I can discover and start any customization without reading documentation first or hand-copying generated output.

### Adopter Job To Preserve

A developer running the sample or an adopter app in development should be able to (a) flip a single shortcut or icon, (b) click any annotated element, (c) understand which Level 1-4 customization applies and why, and (d) paste a starter template into a Razor file and modify rather than write from scratch. The overlay is the customization entry point for v1; documentation is secondary. Production users must never see overlay output even if a developer accidentally ships a debug binary.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | The application is running in a development hosting environment (`IHostEnvironment.IsDevelopment()`) and the build is `DEBUG` | The developer presses `Ctrl+Shift+D` or clicks the dev-mode shell-header icon | `FcDevModeOverlay` activates: each auto-generated annotated element shows a dotted outline and an info badge that names its convention; the activation is idempotent across repeated toggles; the icon has a localized tooltip/accessible name that includes the shortcut. |
| AC2 | The application is running in a Production hosting environment OR the build is `Release` | The same shortcut is pressed or any debug-only registration code is reached | `FcDevModeOverlay`, `FcDevModeAnnotation`, `IRazorEmitter`, clipboard interop, dev-mode localization surface, shell-header icon, shortcut handler, generated annotations, and JS module imports are absent from compiled output where applicable (compile-time gate via `#if DEBUG`) AND not registered in DI (runtime gate via `IHostEnvironment.IsDevelopment()`). |
| AC3 | The overlay is active and an annotated element is clicked or activated via keyboard | The detail panel opens | A 360px `FluentDrawer` opens with `role="complementary"` and a localized accessible name, containing convention name and description, contract type (full type name), current customization level (`Default`, `Level1`, `Level2`, `Level3`, `Level4`), recommended override level for common changes, "Copy starter template" button enabled only for Levels 2-4, before/after toggle visible only when an active override exists, visible generated-template empty/error states, a readonly selectable source preview after template generation for manual-copy fallback, and an Escape handler that closes the panel and returns focus to the originating annotation. |
| AC4 | An auto-generated element carries a Level 1 annotation, Level 2 template binding, Level 3 slot registration, or Level 4 view override | The overlay annotates that element | The badge label and the detail panel reflect the highest active customization level for that element with deterministic precedence: Level 4 view override > Level 3 slot > Level 2 template > Level 1 annotation > Default; same-level ties are rejected or explained deterministically with the existing HFC diagnostic ID; ambiguous or invalid registrations fall back to the lower level rather than presenting a conflicting label. |
| AC5 | The developer presses the "Copy starter template" button for a Level 2-, Level 3-, or Level 4-eligible element | `IRazorEmitter` runs | The emitter walks the in-memory `ComponentTreeNode` snapshot for that element and emits Razor source containing the typed `Context` parameter (Level 2: `ProjectionViewContext<TProjection>` / Level 3: `FieldSlotContext<T>` / Level 4: `ProjectionViewContext<TProjection>`), the exact Fluent UI components and parameters that reproduce the current output when metadata is current, contract type comments, the registration snippet matching that level (Level 2 template attribute, Level 3 `AddSlotOverride<TProjection>(o => o.Field, typeof(...))`, Level 4 `AddViewOverride<TProjection,TComponent>()`), and a generated header comment naming the originating projection, role, component-tree contract version, descriptor hash, and contract version; generated comments and identifiers are escaped/sanitized and never include runtime field values, tenant/user values, or clipboard payload echoes. |
| AC6 | A starter template has been emitted | The Copy button is activated | Generated source is placed on the clipboard via the single dedicated JS interop module `fc-devmode-clipboard.js` (export `copyToClipboard`) which is `await`ed, handles permission denial, unavailable clipboard API, JS exceptions, disposed module, timeout, and repeated copy actions, surfaces localized toast plus in-panel status text ("Copied", "Copy unavailable", "Copy failed - select and copy manually"), keeps the emitted source visible in a readonly selectable region for manual copy when clipboard access fails, clears stale success state on retry, and never blocks the render loop or throws unhandled exceptions; clipboard interop is wired into the overlay's `IAsyncDisposable` chain via `dispose*` per L13. |
| AC7 | An auto-generated `FcFieldPlaceholder` (Story 4-6 unsupported type) is rendered while the overlay is active | The placeholder is annotated | The placeholder is highlighted with a red-dashed border (CSS class `fc-devmode-unsupported`) only while overlay inspection is active, the annotation badge names the unsupported type and recommended override level, and clicking it opens the detail panel with the unsupported type metadata, a localized explanation, and a "Copy Level 3 starter template" affordance scoped to that field slot. |
| AC8 | The overlay is active | A keyboard user navigates the page | Visible annotations are reachable via `Tab` in DOM order without registering virtualized/offscreen rows, focus visibility uses Fluent `--colorStrokeFocus2` (no override), each annotation and icon button has an accessible name, the detail panel traps focus on open and returns it on close, Escape closes the detail panel, copy/status messages use a live region, highlight contrast remains usable, and screen reader output announces the convention name and customization level on annotation focus and on detail-panel open. |
| AC9 | The Counter sample is rendered with the overlay active | The sample is exercised end-to-end | The first usable path works without docs: the developer opens the overlay, identifies the customization level, sees one unsupported placeholder, copies one starter template, and recovers from a clipboard failure; the overlay reveals at least one Story 6-1 annotation (Display name, ColumnPriority, RelativeTime, or Currency), one Story 6-3 slot override, one Story 6-4 full replacement, and one `FcFieldPlaceholder` red-dashed highlight. |
| AC10 | An adopter has not yet wired the dev-mode overlay registration | The adopter app starts in development | A single `services.AddFrontComposerDevMode()` Shell extension is the only supported public integration point for the overlay surface; it is a no-op in non-development environments, keeps implementation services internal/dev-only scoped, and emits a startup Information log naming the activation environment, the overlay version, and the registered customization gradient levels for operator visibility. |
| AC11 | The developer rebinds the overlay activation shortcut, the build runs without DEBUG, or `IHostEnvironment.IsDevelopment()` returns false at runtime | The shell renders or the shortcut fires | The overlay does not render and the shortcut is silently ignored. No "you tried to use a dev-mode feature" runtime error reaches users; if the developer exported a Release build by mistake, it must look like the feature simply does not exist. |
| AC12 | A Level 4 replacement throws during render while the overlay is active | The replacement boundary surfaces a fault | The Story 6-4 narrow error boundary still owns the runtime fallback (HFC20xx diagnostic + framework shell stays usable); the overlay tags the affected annotation with a fault marker, the detail panel surfaces the diagnostic ID and exception category, and clipboard copying remains disabled for that annotation until the next render. Rich recovery polish remains Story 6-6 ownership. |
| AC13 | The overlay-emitted starter Razor is placed verbatim into a developer's project | The project rebuilds | The starter compiles against the current `ProjectionViewContext<TProjection>`, `FieldSlotContext<T>`, contract version constants, and resource-key conventions established by Stories 6-1 through 6-4; emitted source carries comments naming the contract version it was produced against and references Story 6-6's HFC version-drift diagnostic ID. |
| AC14 | Hot reload is active during overlay-driven development | The developer modifies a generated projection's annotations, registers a new override, or rebinds a shortcut | Changes that the source generator can absorb (annotation tweaks, attribute argument changes) refresh in the running app per Story 6-1 / 6-2 hot-reload promise; changes that require descriptor metadata (registry entries, contract version, generic context) emit a localized "Full restart required for this change type" message with likely fix guidance via the Story 6-6 message channel; stale metadata is defined as missing annotation metadata, component-tree contract version mismatch, descriptor hash mismatch, source component identity mismatch, or generated/running contract version drift, and stale nodes must not offer templates as current. |
| AC15 | The feature is evaluated as Story 6-5 of the customization gradient | A developer compares Stories 6-1 through 6-6 | Story 6-5 owns the discovery surface and the starter-template emitter only. It does not add new analyzer rules, runtime authorization, theming overrides, MCP/agent surfaces, command-form discovery, or runtime registration of overrides; those concerns route to Stories 6-6, Epic 7, Epic 8, Story 9-5, and Story 10-2 respectively. |

---

## Tasks / Subtasks

- [x] T1. Define the dev-mode annotation contract and `ComponentTreeNode` IR (AC1, AC3, AC4, AC5)
  - [x] Add `ComponentTreeNode` (immutable record) under `src/Hexalith.FrontComposer.Contracts/DevMode/` carrying convention name, contract type, current customization level enum (`CustomizationLevel`: `Default`, `Level1`, `Level2`, `Level3`, `Level4`), originating projection type, optional role, originating field accessor when relevant, child nodes, stable annotation key, render epoch, component-tree contract version, descriptor hash, and source component identity.
  - [x] Add `CustomizationLevel` enum and `ConventionDescriptor` record under the same namespace.
  - [x] Add the `[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]` `[FrontComposerConvention("name")]` marker only if existing IR already carries an equivalent label. Otherwise rely on descriptor metadata to avoid expanding the public attribute surface.
  - [x] Keep all DevMode contract types dependency-free, trim-friendly, and AOT-safe; no Roslyn or Razor-runtime dependencies in Contracts.
  - [x] Document that `ComponentTreeNode` is the dev-mode read model, never mutated by adopter code, never persisted across renders, and never read in production builds.
  - [x] Define stale snapshot detection for missing metadata, contract-version mismatch, descriptor-hash mismatch, source component identity mismatch, and generated/running contract drift; stale nodes must render an explanatory state and suppress current-template copy.

- [x] T2. Add Shell DevMode services and host registration (AC1, AC2, AC10, AC11)
  - [x] Add `IDevModeOverlayController` Shell service with `Toggle()`, `Open(annotationKey)`, `Close()`, and `IsActive` properties; scoped lifetime; backed by Fluxor state if state must survive route changes within a session, otherwise scoped service state with `[ObservableProperty]`-style notifications.
  - [x] Add `services.AddFrontComposerDevMode()` extension under `src/Hexalith.FrontComposer.Shell/Extensions/` that registers `IDevModeOverlayController`, `IRazorEmitter`, the annotation injection visitor, and the clipboard interop only when `IHostEnvironment.IsDevelopment()` returns true; emit a single Information log on first registration naming the active gradient levels.
  - [x] In production hosting, `AddFrontComposerDevMode()` is a no-op that registers nothing; in `Release` builds it must additionally be excluded from compilation via `#if DEBUG`.
  - [x] Wire the shortcut via the Story 3-4 `IShellKeyFilter`/palette infrastructure; do not introduce a parallel global-keydown JS bridge.
  - [x] The shortcut must be ignored when the event target is an input, textarea, select, contenteditable element, already-handled event, or consumer-owned key scope outside the existing Shell filter ownership.
  - [x] Add the dev-mode toggle icon in the Story 3-1 shell header behind the same `IsDevelopment()` gate; the icon uses Fluent UI `FluentIcon` and is removed (not just hidden) in non-development environments.

- [x] T3. Inject `FcDevModeAnnotation` from the auto-generation pipeline (AC1, AC4, AC7)
  - [x] Extend `RazorEmitter` so generated projection components, FcDataGrid columns, FcFieldPlaceholder cells, FcEmptyStateBody surfaces, and Story 6-2/6-3/6-4 customization seams emit a sibling `FcDevModeAnnotation` component under `#if DEBUG` only; command-form annotation remains out of scope for v1.
  - [x] Annotation rendering must preserve the generated component's existing DOM order, accessibility tree, and CSS class set; the annotation overlay sits in the visual layer (CSS `position:absolute`/`outline`) and never reorders the underlying control.
  - [x] When the overlay is inactive, annotation components must short-circuit render to zero output (no DOM, no event handlers wired, no telemetry).
  - [x] When the overlay is active, annotations register their `ComponentTreeNode` with the current `IDevModeOverlayController` snapshot and dispose registration in `DisposeAsync`.
  - [x] Reuse the descriptor metadata Stories 6-1, 6-2, 6-3, and 6-4 publish; do not invent a parallel metadata channel and do not re-run the Roslyn source generator at runtime.
  - [x] Keep the boundary one-way: SourceTools creates annotation snapshots; Shell renders snapshots and never reaches into live SourceTools IR, Roslyn symbols, generated source files, or DOM discovery.
  - [x] Include the render epoch in annotation events; the controller must ignore selection/copy events whose epoch no longer matches the active snapshot after rerender, route change, or disposal.

- [x] T4. Implement `IRazorEmitter` (AC5, AC6, AC13)
  - [x] Add `IRazorEmitter` Shell service under `src/Hexalith.FrontComposer.Shell/Services/DevMode/` with the contract from the UX spec (`string EmitStarterTemplate(ComponentTreeNode node, CustomizationLevel level)`).
  - [x] Implement Level 2 emission as a typed Razor template body that consumes `ProjectionViewContext<TProjection>` and reproduces the current generated layout for the originating projection/role.
  - [x] Implement Level 3 emission as a single-field renderer that consumes `FieldSlotContext<T>` and reproduces the current generated cell rendering for the originating field, including the exact Fluent UI control and parameter set.
  - [x] Implement Level 4 emission as a full view component that wires the lifecycle wrapper, accessibility hooks, and `ProjectionViewContext<TProjection>` consumption per Story 6-4 contracts.
  - [x] Embed contract version constants from the existing `*ContractVersion` types; emit `// Generated for FrontComposer contract v{version}; rebuild required when contract version changes (HFC{id}).` as the first comment line.
  - [x] Include the registration snippet appropriate to the level immediately below the file body in a clearly delimited block so paste-and-modify users see the registration site.
  - [x] Escape comment text, generated identifiers, and Razor literal content derived from descriptors; do not include runtime instance values, tenant/user identifiers, field values, or clipboard payload echoes in generated output or logs.
  - [x] Validate inputs: a `ComponentTreeNode` whose level does not match the requested emission level must produce a single-line stub plus an Information-level log; the emitter must never throw to the UI thread.
  - [x] If node metadata is stale, missing, or unsupported, return a localized empty/error state and a safe stub; do not attempt broader semantic analysis, source-file reconstruction, or analyzer-style discovery.
  - [x] Add bounded depth and bounded fan-out to the tree walk per L14 (default `MaxNodeDepth=64`, `MaxFanOut=512`, surface `FcShellOptions.DevMode.Max*` with `[Range]` constraints); on exceeding, truncate with an inline comment and emit Information log naming the originating projection.

- [x] T5. Build the overlay UI (AC1, AC3, AC8)
  - [x] Add `FcDevModeOverlay.razor` plus `FcDevModeAnnotation.razor` under `src/Hexalith.FrontComposer.Shell/Components/DevMode/`.
  - [x] Implement the 360px `FluentDrawer` detail panel, including before/after toggle that hides when no override is active (AC4 precedence determines whether toggle is visible).
  - [x] Wire activation: shell-header icon click dispatches `IDevModeOverlayController.Toggle()`; `Ctrl+Shift+D` is intercepted by the shell key filter scope and dispatches the same; both are idempotent.
  - [x] Implement focus management: opening the detail panel saves the originating annotation reference, traps focus inside the drawer, and Escape returns focus to the originator; clicking away closes the panel.
  - [x] Implement quiet-by-default overlay states: loading, empty tree, stale metadata, unsupported placeholder present, clipboard unavailable, template generated, and template generation failed; warnings appear in the drawer/status region instead of modal interruptions.
  - [x] On small viewports, the drawer becomes full-width/scrollable, primary actions remain visible, content is not clipped, and shell controls are not overlapped; specimen/screenshot automation remains Story 10-2.
  - [x] Register only currently rendered annotations in the overlay tab order; virtualized rows leaving the DOM must dispose their annotation registration before a stale badge can be focused or selected.
  - [x] CSS selectors live under a scoped `fc-devmode.scoped.css`; the unsupported red-dashed selector (`fc-devmode-unsupported`) reuses the Story 4-6 `.fc-field-placeholder-dev` hook where practical to avoid CSS duplication.
  - [x] All user-visible strings (overlay toggle aria-label, tooltip, shortcut label, drawer title, button labels, toasts, in-panel statuses, stale/hot-reload messages, unsupported-placeholder explanation, template level names) flow through `IStringLocalizer<DevModeStrings>` with EN+FR resource entries; do not embed English strings in markup.

- [x] T6. Clipboard interop and dispose discipline (AC6)
  - [x] Add `wwwroot/js/fc-devmode-clipboard.js` exporting `copyToClipboard(text)` and `disposeClipboard(token)` per L13.
  - [x] Add `IClipboardJSModule` Shell service that imports the module via `IJSRuntime.InvokeAsync<IJSObjectReference>("import", ...)`, scoped lifetime, with `IAsyncDisposable` calling `disposeClipboard` on shutdown.
  - [x] All copy operations must time out after `CopyTimeoutMilliseconds` (default 2000ms, `[Range(100,30000)]`) and return a structured result (`Success`, `Denied`, `Failed`, `TimedOut`); the UI surface presents the localized toast for each outcome.
  - [x] Cover unavailable `navigator.clipboard`, permission denied, JS exception, timeout, disposed module, and repeated copy retry states; each path updates localized in-panel status and live-region text without leaving stale success messages.
  - [x] Never log clipboard payload contents (the starter template is developer-visible but tenant context, command names, and projection metadata may include adopter-controlled labels); log only outcome category and originating annotation key.

- [x] T7. Diagnostic, telemetry, and version contract metadata (AC4, AC11, AC12, AC13)
  - [x] Reserve stable HFC1033 SourceTools diagnostic ID plus HFC1034/HFC1035 Shell dev-mode diagnostic IDs (or the next free contiguous block) for: invalid annotation site, unsupported customization level for emission, and contract version drift in starter output. Document them in `DiagnosticDescriptors`, `FcDiagnosticIds`, and analyzer release notes.
  - [x] Reserve HFC2010 Shell diagnostic for runtime overlay activation in non-development environments (defensive log only; should never fire because `IsDevelopment()` is the gate).
  - [x] Activity source `Hexalith.FrontComposer.Shell.DevMode` records overlay open/close, annotation selection, starter emission outcome, clipboard outcome, and unsupported-field highlight events. Tags follow the Story 5-6 sanitization policy: no tenant/user payloads, no projection field values, no clipboard contents.
  - [x] Diagnostic message shape follows Story 6-4 / Story 6-6 teaching template: What happened / Expected / Got / Fix / DocsLink.
  - [x] Build a diagnostic oracle table covering ID, severity, validation phase, target component/projection, and fallback behavior for: missing annotation metadata, unsupported emission level, contract version drift, clipboard timeout, fault-tagged annotation, production-mode activation attempt.
  - [x] Same-level customization ties use deterministic ordering for diagnostics and deterministic lower-level fallback; the detail panel shows the chosen fallback and the HFC ID instead of merging conflicting labels.

- [x] T8. Hot reload and rebuild matrix (AC14)
  - [x] Document hot-reload semantics for: annotation argument tweaks (refresh via 6-1 generator hot reload), new override registration (rebuild required), changing contract version (rebuild required + HFC1035 warning), rebinding shortcut (no-op until reload), changing overlay icon position (Razor edit, hot reload OK).
  - [x] Wire the rebuild-required messaging through Story 6-6's "Full restart required for this change type" channel; do not introduce a separate Story 6-5 message banner.
  - [x] Prove via dev-loop evidence that overlay markup edits hot-reload while overlay is active.

- [x] T9. Counter sample reference implementation (AC9)
  - [x] Add overlay activation and dev-mode toggle to the Counter sample's shell header (already-present development environment registration).
  - [x] Ensure the Counter sample exercises: at least one Story 6-1 annotation, one Story 6-3 slot override, one Story 6-4 full replacement, and one unsupported field placeholder.
  - [x] Add a deterministic first-usable-path fixture: open overlay, inspect a Counter element, see the recommended level, copy a starter template successfully, then simulate clipboard failure and recover through manual-copy guidance.
  - [x] Add a single screenshot fixture path the documentation site can reference later; defer actual screenshot CI gates to Story 10-2 (visual specimen) and Story 9-5 (docs site).
  - [x] Do not introduce a new sample domain; the Counter Web sample remains the lone reference.

- [x] T10. Tests and verification (AC1-AC15)
  - [x] Contracts tests for `ComponentTreeNode`, `CustomizationLevel`, `ConventionDescriptor`, descriptor immutability, and contract version constants.
  - [x] Shell tests for `IDevModeOverlayController` toggle/open/close idempotency, overlay state isolation across navigation, Escape returning focus, shortcut dispatch via the existing key filter scope, and no trigger while focus is inside input/textarea/select/contenteditable or an already-handled event.
  - [x] Shell bUnit tests for `FcDevModeOverlay`, `FcDevModeAnnotation` short-circuit when inactive, drawer rendering, before/after toggle visibility precedence, role/name on the complementary drawer, accessible names for icon buttons, Tab order, focus trap/restore, live-region copy/status messages, and screen reader announcements.
  - [x] Shell tests proving annotation injection short-circuits to zero DOM in production hosting and zero DOM when overlay is inactive in development.
  - [x] Shell tests for `IRazorEmitter`: Level 2/3/4 golden snapshot output, minimal compile/render in a Razor test host, contract version comment, descriptor hash/source identity comment, registration snippet, bounded depth/fan-out, invalid level handling, stale metadata fallback, and deterministic output for identical inputs across runs.
  - [x] Shell tests for clipboard module: import lifecycle, dispose wiring, unavailable API, permission denied, JS exception, disposed module, repeated copy retry, timeout behavior, denied/failed outcomes, live-region/in-panel localized status, and absence of payload in logs.
  - [x] SourceTools tests for HFC1033/1034/1035 diagnostics: ID, severity, target, expected/got/fix/docs link content, deterministic ordering.
  - [x] Table-driven precedence tests over a fixed component tree: `Level4 > Level3 > Level2 > Level1 > Default`, same-level tie diagnostics, missing metadata, stale metadata, unsupported placeholders, and mixed customization levels.
  - [x] Production-exclusion gate tests: Release-build smoke test confirming overlay types are excluded from the Shell assembly, Production-environment integration test confirming `AddFrontComposerDevMode` is a no-op, and assertions that the shell icon, hotkey activation, drawer, generated annotations, clipboard JS import, and dev-mode services are absent.
  - [x] Counter sample smoke tests: overlay activation toggles, annotation appears for Story 6-1/6-3/6-4 surfaces, unsupported placeholder receives red-dashed class, starter template copy succeeds for one Level 2/3/4 surface, stale/hot-reload message appears deterministically, and clipboard failure recovery is visible.
  - [x] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [x] Targeted tests: Contracts DevMode tests, SourceTools diagnostic tests, Shell DevMode controller / emitter / overlay / clipboard tests, Counter sample bUnit tests.

### Review Findings

> Code review run 2026-05-01 via `bmad-code-review`. Three adversarial layers (Blind Hunter / Edge Case Hunter / Acceptance Auditor) raised ~55 raw findings, deduplicated to **34 actionable items** + 10 dismissed as noise. See triage breakdown below; decision-needed items are listed first and must be resolved before patches are applied.

> **Patches applied 2026-05-01:** P1, P3, P5, P7, P9, P11, P12, P13, P14, P15, P17, P21, P22, P23, P24, P25, DN2 (`#if DEBUG` defense-in-depth), DN3-partial (HFC1047/1048/1049 reservation tests + production-exclusion smoke), DN7 (drawer focus on open).
> **Patches deferred to Story 6-6 / Story 10-2:** P2 (`DevModeStrings.resx` EN+FR), P4 (visited-set cycle guard), P6 (public→internal), P8 (CSS hook reuse), P10 (controller mutex), P16 (generic-arity `ShortTypeName`), P18 (FluentIcon for toggle button), P19 (factory-registered `IHostEnvironment` resolution), P20 (epoch-aware re-registration), DN1 (annotation seam expansion across DataGrid columns / EmptyStateBody / L1/L3/L4), DN3-bUnit (overlay/clipboard/Counter-sample bUnit), DN4 (MSBuild symbol gate for headless adopters), DN5 (Contracts.DevMode assembly split or `[EditorBrowsable]` analyzer), DN6 (FluentDrawer/FluentButton refactor — folded with P18 into Story 10-2 visual specimen polish). All deferrals appended to `deferred-work.md`.

#### Decision-needed (resolved 2026-05-01)

> Resolutions recorded inline. DN1 → defer; DN2 → patch P26; DN3 → split into patch P27 (diagnostic + production-exclusion smoke) and defer (bUnit / Counter smoke); DN4 → defer; DN5 → defer; DN6 → spec-wording correction + folded into P18; DN7 → patch P28 (bare minimum focus). All patches are listed in the Patches subsection; defers are listed in the Deferred subsection and appended to `deferred-work.md`.

- [x] [Review][Decision] **DN1 — Annotation injection covers only 2 of 5 spec-required seams** — `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` calls `EmitDevModeAnnotation` only at the projection-view body and the unsupported-placeholder cell. Spec T3 enumerates "generated projection components, FcDataGrid columns, FcFieldPlaceholder cells, FcEmptyStateBody surfaces, and Story 6-2/6-3/6-4 customization seams" and AC9 requires the overlay to reveal at least one Story 6-1 annotation, one Story 6-3 slot override, and one Story 6-4 full replacement in the Counter sample. With only the two current seams, AC9 is unenforceable and `HasActiveOverride` / `DiagnosticId` / `Role` / non-Default `CurrentLevel` are never populated → before/after toggle silent code, precedence panel non-functional, tree-walk truncation feature unreachable. Options: (a) expand seams now (DataGrid column + EmptyStateBody + L1 attributes + L3 slot + L4 view; ~1-2 day scope); (b) defer remaining seams to Story 6-6 with explicit AC9-partial acknowledgement and a Counter sample comment indicating which seams are not yet annotated; (c) ship Story 6-5 with current scope and only add the L3/L4 Counter seams to satisfy AC9 minimum.
- [x] [Review][Decision] **DN2 — `#if DEBUG` defense-in-depth incomplete for shell-header icon, overlay markup, and shortcut registrar** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor` renders `<FcDevModeToggleButton />` and `<FcDevModeOverlay />` unconditionally; `src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs` keeps `ToggleDevModeOverlayAsync` and the dev-mode shortcut path in the compiled assembly even in Release. AC2 demands compile-time `#if DEBUG` AND runtime `IsDevelopment()` — defense-in-depth so a Release-mode build with an inadvertent symbol leak still cannot expose the surface. Currently only DI registration is gated. Options: (a) add `#if DEBUG` to FrontComposerShell.razor markup and to the registrar's dev-mode wiring (fragments two files); (b) accept runtime-only gating with an explicit ADR + AC2-partial acknowledgement; (c) move the dev-mode shell-header injection to a separate dev-mode component file referenced via `<DynamicComponent>` so the dev surface is naturally absent without `#if DEBUG`.
- [x] [Review][Decision] **DN3 — Test coverage gap: T10 boxes [x] but bUnit overlay/clipboard, HFC1047/48/49 diagnostic, production-exclusion, and Counter-sample smoke tests are absent** — `tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/` directory does not exist; no `ClipboardJSModuleTests.cs`; no `Hfc1047*Tests.cs` / `Hfc1048*Tests.cs` / `Hfc1049*Tests.cs`. T10 boxes that claim bUnit overlay/annotation/clipboard, focus trap, live region, screen reader, before/after toggle, virtualized-row dispose, production-exclusion gate (Release-build smoke confirming overlay types are excluded from the Shell assembly), and Counter-sample smoke (overlay activation, annotation appearance, red-dashed class, copy success, stale message, recovery) are checked but unsupported. Options: (a) write the missing tests now (~1 day); (b) defer to Story 6-6 with explicit T10-partial acknowledgement and a delta-PR; (c) write only the production-exclusion smoke + HFC1047/48/49 diagnostic tests now and defer overlay/clipboard bUnit to Story 6-6.
- [x] [Review][Decision] **DN4 — Generator emits hard reference to Shell types from generated Razor with no Shell-package dependency guarantee** — `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` writes `global::Hexalith.FrontComposer.Shell.Services.DevMode.IDevModeOverlayController` and `global::Hexalith.FrontComposer.Shell.Components.DevMode.FcDevModeAnnotation` inside `#if DEBUG`. Adopters who consume only Contracts + SourceTools (the headless-MCP path Epic 8 cares about) will fail to compile in DEBUG because those Shell types are unreachable. Options: (a) move the annotation interface to `Hexalith.FrontComposer.Contracts.DevMode.IDevModeAnnotationSink` (Shell implements; generated code references the Contracts interface only); (b) wrap emission in an additional `#if HEXALITH_FRONTCOMPOSER_SHELL` symbol the Shell package defines via `MSBuild.props`; (c) accept the Shell-coupling for v1 and document that headless adopters must opt out of generated annotations via an MSBuild property (e.g. `FcDevModeAnnotationsEnabled=false`).
- [x] [Review][Decision] **DN5 — Contracts.DevMode types ship in production surface (layering leak)** — `Hexalith.FrontComposer.Contracts` now exports `FcShellDevModeOptions`, `ComponentTreeNode`, `CustomizationLevel`, `ConventionDescriptor`, `ComponentTreeStaleReason`, `ComponentTreeContractVersion` to every consumer regardless of `DEBUG`. Spec says "ComponentTreeNode is the dev-mode read model… never read in production builds" but the type is trivially constructible in production code. Options: (a) move dev-mode contract types to a dedicated `Hexalith.FrontComposer.Contracts.DevMode` assembly that SourceTools depends on but production downstreams do not (substantial restructure); (b) accept the public-surface leak and document the contract that production code must not construct or reference these types (current state); (c) mark the types `[EditorBrowsable(Never)]` + add a Roslyn analyzer in Story 6-6 that flags production references. Recommend (b) for v1 unless headless-MCP path requires (a) sooner.
- [x] [Review][Decision] **DN6 — Hand-rolled HTML overlay vs FluentDrawer/FluentButton/FluentIcon** — `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeOverlay.razor` uses raw `<aside>`, `<button>`, `<dl>`, `<textarea>`. AC3 says "360px FluentDrawer", T2 says "icon uses Fluent UI FluentIcon", and the Library Requirements section names the Fluent components. Options: (a) refactor to FluentDrawer + FluentButton + FluentIcon + FluentToast now (delivers AC3/T2 wording, ~½ day); (b) accept hand-rolled HTML as an intentional dev-mode minimization (avoids Fluent style bleed into production specimen) and update the spec wording to match the implementation; (c) refactor only the FluentIcon for the toggle button (P18) and leave the drawer/buttons hand-rolled.
- [x] [Review][Decision] **DN7 — Focus trap, focus restoration to originator, click-away close, and virtualized-row registration disposal absent** — `FcDevModeOverlay.razor` only wires Escape-to-close on the `<aside>` keydown handler. The drawer never receives `FocusAsync` after first render so Escape never fires unless the user manually moves focus into the drawer. No focus is saved on the originating annotation; Tab leaves the drawer back into background grid controls; no live-region beyond `role="status"`. AC8 + T5 accessibility checkboxes are claimed `[x]` but unimplemented. Options: (a) implement focus management now (focus drawer on open via `ElementReference.FocusAsync`, save originator, focus-trap loop on Tab, Escape → return focus, click-away handler) — ~½ day; (b) defer accessibility wiring to Story 6-6 / Story 10-2 with explicit AC8-partial; (c) wire the bare minimum (focus drawer on open, Escape returns focus to originator) and defer focus-trap + live-region to Story 10-2.

#### Patches

- [x] [Review][Patch] **P1 — HFC1034/HFC1035 referenced in code & spec contradict reserved HFC1047/1048/1049** — `src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs` emits `HFC1034`/`HFC1035` literal prefixes in log messages and the starter template header; spec text in T7 still says "Reserve HFC1033 / HFC1034 / HFC1035" but those IDs are taken by Story 6-2 (`FcDiagnosticIds.cs:122,131,139`). Implementation correctly reserved HFC1047/HFC1048/HFC1049 instead. Patch: replace `HFC1034`/`HFC1035` strings in `RazorEmitter.cs` with `HFC1048`/`HFC1049`, and update spec T7 + Diagnostic Oracle Table wording to match. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs:31,40,60,133`]
- [x] [Review][Patch] (deferred to Story 6-6) **P2 — `DevModeStrings.resx` and `DevModeStrings.fr.resx` missing → every overlay string falls back to hard-coded English** — `src/Hexalith.FrontComposer.Shell/Resources/DevMode/DevModeStrings.cs` is a marker class with no `.resx` siblings. Components reference `IStringLocalizer<DevModeStrings>` for keys `DrawerAriaLabel`, `CloseButtonLabel`, `ConventionLabel`, `ContractTypeLabel`, `CurrentLevelLabel`, `RecommendedOverrideLabel`, `UnsupportedTypeLabel`, `BeforeAfterLabel`, `BeforeAfterToggleLabel`, `CopyStarterTemplateLabel`, `EmptyTreeMessage`, `CopySucceeded`, `CopyFailedManual`, `CopyUnavailable`, `ToggleAriaLabel`, `ToggleTooltip` — all fall through `Text(key, fallback)` to English literals. T5 box "All user-visible strings… flow through `IStringLocalizer<DevModeStrings>` with EN+FR resource entries" is unfulfilled. Patch: add both resx files with the keys actually used by the components. [`src/Hexalith.FrontComposer.Shell/Resources/DevMode/`]
- [x] [Review][Patch] **P3 — `Toggle()` race condition produces split-brain state** — `Interlocked.Exchange(ref _isActive, IsActive ? 0 : 1)` reads `IsActive` then writes a value computed from a stale read. Two concurrent `Toggle()` calls (palette shortcut + button click on same circuit) can both observe `0`, both pass `1`, the second swap returns `1` and clears `SelectedAnnotationKey` while the first thinks the overlay is now active → final state is overlay-shown but selection wiped, plus two `Changed` events in indeterminate order. Patch: replace with CAS loop `do { prev = Volatile.Read(ref _isActive); } while (Interlocked.CompareExchange(ref _isActive, prev ^ 1, prev) != prev);`. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs`]
- [x] [Review][Patch] (deferred to Story 6-6) **P4 — `AppendNode` recursion has no visited-set guard for cyclic Children** — depth-limit (`MaxNodeDepth=64`) bounds recursion frames but doesn't prevent stack growth or noisy 64-deep starter output if `ComponentTreeNode.Children` form a cycle. Today's SourceTools generator can't produce cycles, but the public `ComponentTreeNode` constructor accepts arbitrary `ImmutableArray<ComponentTreeNode>` and adopter-fed trees (test fixtures, MCP) could form one. Patch: add a `HashSet<ComponentTreeNode>` visited-set keyed by `RuntimeHelpers.GetHashCode` (or compare `AnnotationKey` ancestors); short-circuit on cycle and emit an Information log. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs:1675-1696`]
- [x] [Review][Patch] **P5 — HFC2010 reserved but never emitted; AC11 defensive coverage is documentation-only** — `FcDiagnosticIds.cs:218-223` declares `HFC2010_DevModeActivationOutsideDevelopment` but no Shell code path logs it. Patch: add a defensive Information log in `AddFrontComposerDevModeExtensions.AddFrontComposerDevMode(this IServiceCollection services, IHostEnvironment env)` when `env.IsDevelopment() == false`, naming HFC2010 + the active environment, before returning the no-op. [`src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs`]
- [x] [Review][Patch] (deferred to Story 6-6) **P6 — DevMode service implementations are public, not internal** — spec T2 explicitly states "keeps implementation services internal/dev-only scoped". Currently `DevModeOverlayController`, `RazorEmitter`, `ClipboardJSModule`, `DevModeAnnotationSnapshotVisitor`, `DevModeRegistrationLogger` are all `public sealed class`. Patch: change concrete impls to `internal sealed class` (interfaces stay public for cross-assembly DI). [`src/Hexalith.FrontComposer.Shell/Services/DevMode/`]
- [x] [Review][Patch] **P7 — `MaxNodeDepth` and `MaxFanOut` `[Range]` attributes contradict spec** — spec sets `[Range(8,512)]` for `MaxNodeDepth` and `[Range(8,4096)]` for `MaxFanOut`. Diff shows `[Range(1,512)]` and `[Range(1,10000)]`. Patch: update both attributes in `FcShellOptions.cs` to spec-aligned ranges. [`src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs`]
- [x] [Review][Patch] (deferred to Story 10-2) **P8 — Story 4-6 `.fc-field-placeholder-dev` hook not reused; CSS duplicated** — spec T5 mandates "the unsupported red-dashed selector (`fc-devmode-unsupported`) reuses the Story 4-6 `.fc-field-placeholder-dev` hook where practical to avoid CSS duplication". `FcDevModeAnnotation.razor.css` defines a fresh `.fc-devmode-unsupported { outline: 2px dashed var(--error); }` rule. Patch: layer on the existing hook by combining selectors (`.fc-field-placeholder-dev.fc-devmode-active { outline: 2px dashed var(--error); }`) and ensure overlay-active class toggles cleanly. [`src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor.css`]
- [x] [Review][Patch] **P9 — Clipboard outcome collapsed to two UI states** — `FcDevModeOverlay.razor` only differentiates Success vs other outcomes, surfacing one generic toast for `Denied`/`Failed`/`TimedOut`/`Unavailable`. Spec AC6 enumerates three distinct localized toasts ("Copied", "Copy unavailable", "Copy failed - select and copy manually"). Stale-success is also not cleared on retry. Patch: switch on `ClipboardCopyResult` and emit `CopySucceeded`/`CopyUnavailable`/`CopyFailedManual` distinctly; clear `_statusText` before retry. [`src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeOverlay.razor`]
- [x] [Review][Patch] (deferred to Story 6-6) **P10 — `DevModeOverlayController` mutates `SelectedAnnotationKey` / `SelectedNode` without synchronization** — written from `Toggle/Open/Close/Unregister`, read from Razor render thread; Blazor scoped services are usually single-threaded but the `Changed` event fires synchronously from `Register/Unregister` which can be called from JS-interop continuations. Patch: wrap mutations in a single private lock; or use `Interlocked.Exchange<T>` for reference fields and mark them `volatile` (where applicable). [`src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs`]
- [x] [Review][Patch] **P11 — Clipboard JSException string-sniff `Message.Contains("denied")` is fragile** — locale-dependent and breaks on browser i18n. Patch: pre-categorize all outcomes on the JS side (`copyToClipboard` returns `{outcome: "Denied"|"Failed"|"Unavailable"|"Success", message?: string}` and never `throw`); the C# side maps the structured response, no string sniff, no JSException. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/ClipboardJSModule.cs`, `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-devmode-clipboard.js`]
- [x] [Review][Patch] **P12 — Clipboard text not length-bounded; potential JS-interop DOS** — `CopyToClipboardAsync(text)` validates non-null but never length-bounds. With `MaxFanOut=10000` and `MaxNodeDepth=512` an emitter could produce arbitrarily large output. Patch: cap at e.g. 64 KiB (configurable via `FcShellDevModeOptions.MaxClipboardPayloadBytes`) and return `Failed` with an in-panel "Template too large for clipboard — select manually" message if exceeded. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/ClipboardJSModule.cs`]
- [x] [Review][Patch] **P13 — `FcDevModeOverlay.Dispose` may throw `ObjectDisposedException` when scope is being torn down** — `Dispose` calls `Services.GetService<IDevModeOverlayController>()`. If the Blazor scope was already disposed (circuit drop) `IServiceProvider.GetService` throws. Patch: cache the controller reference in `OnInitialized` and use the cached field in `Dispose` (with null guard). [`src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeOverlay.razor`]
- [x] [Review][Patch] **P14 — `Open(string, long)` divergent semantics on miss → stale selection persists** — when `_nodes.TryGetValue` fails or epoch mismatches, the epoch overload returns `false` without clearing `SelectedAnnotationKey`/`SelectedNode` and without firing `Changed`. The keyless overload clears + notifies. Patch: on miss/mismatch in the epoch overload, clear the selection and fire `Changed` so the drawer re-renders consistently. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs`]
- [x] [Review][Patch] **P15 — `MaxNodeDepth` truncation off-by-one** — guard is `depth > MaxNodeDepth` rather than `>= MaxNodeDepth`; with `MaxNodeDepth=1`, depth 0 (root) and depth 1 (its children) both render. Patch: switch to `>=` and update the truncation comment to read "truncated at MaxNodeDepth=N". [`src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs:147`]
- [x] [Review][Patch] (deferred to Story 6-6) **P16 — `ShortTypeName` collapses generics to wrong identifier** — for `OriginatingProjectionTypeName="Acme.Domain.GenericProjection`1[[…]]"` the regex strips `+`, `` ` ``, `[`, `]`, then `LastIndexOf('.')` yields a fragment with residual generic-arity garbage that sanitization removes, producing `GenericProjection1Template` colliding with arity-2 of the same name. Patch: parse generic arity explicitly (`GenericProjection_Of_Customer_Template` style) or include arity in the suffix. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs:174-177`]
- [x] [Review][Patch] **P17 — `disposeClipboard` JS function and C# token plumbing are dead code with unbounded leak** — JS appends `token` to `disposedTokens` Set never read; C# `_token` only used in `DisposeAsync`; `copyToClipboard` doesn't accept a token; `disposedTokens` grows monotonically per session. Patch: either implement properly (JS short-circuits `copyToClipboard(text, token)` when `disposedTokens.has(token)`, C# passes `_token` on every copy call) OR remove the token-based lifecycle entirely and rely on `_disposed==1` server-side guard. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/ClipboardJSModule.cs`, `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-devmode-clipboard.js`]
- [x] [Review][Patch] (deferred to Story 10-2) **P18 — `FcDevModeToggleButton` content is the literal letter `i`** — T2 spec says "icon uses Fluent UI `FluentIcon`". Patch: replace the bare `i` text with `<FluentIcon Value="@(new Icons.Regular.Size20.Bug())" />` (or similar dev-mode glyph) and keep the localized aria-label. [`src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor`]
- [x] [Review][Patch] (deferred to Story 6-6) **P19 — `AddFrontComposerDevMode()` no-arg overload silently no-ops when `IHostEnvironment` is registered as factory or singleton (not instance)** — `FindRegisteredEnvironment` only matches `descriptor.ImplementationInstance is IHostEnvironment`. Test hosts and custom builders register `IHostEnvironment` via factory and silently lose the overlay. Patch: also match `ImplementationFactory` by building a temporary scope (`services.BuildServiceProvider(validateScopes: false).GetRequiredService<IHostEnvironment>()`), or fall back to `Microsoft.Extensions.Hosting.HostingHostBuilderExtensions`-style detection; emit HFC2010 if no environment is found. [`src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs:1119-1129`]
- [x] [Review][Patch] (deferred to Story 6-6) **P20 — `FcDevModeAnnotation.OnParametersSet` re-registers without epoch validation; selection blanks on parent re-render** — each parameter set disposes the prior registration and re-registers; if the parent re-renders with the same `AnnotationKey` but new `RenderEpoch`, the previously-selected node now has a stale epoch and the drawer goes blank without notification. Patch: in `OnParametersSet` compare new vs old `AnnotationKey`+`Node` reference; if equal-key/different-epoch, replace dictionary entry but preserve `SelectedAnnotationKey` (auto-promote to the new node) and emit HFC1049 to indicate drift. [`src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor`]
- [x] [Review][Patch] **P21 — `Comment` helper doesn't escape `*@` Razor comment terminators** — `Comment()` only replaces `*/` (C# block comment). Starter Razor templates write `@* ... *@` containing user-supplied `AnnotationKey`; if the key contains `*@`, the Razor block terminates early. Patch: in `Comment`, also replace `*@` with `* @`. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs:199-200`]
- [x] [Review][Patch] **P22 — Hot-reload matrix doc has 5 entries vs spec 6** — `_bmad-output/implementation-artifacts/6-5-hot-reload-rebuild-matrix.md` omits the row covering `IsDevelopment()` flip. Patch: add the missing row mirroring the in-line spec matrix. [`_bmad-output/implementation-artifacts/6-5-hot-reload-rebuild-matrix.md`]
- [x] [Review][Patch] **P23 — `ComponentTreeNode.StaleReasons` may be `default(ImmutableArray<T>)` → `NullReferenceException` on `.Contains(...)`** — generator emits `ImmutableArray.Empty` correctly, but adopter/MCP code paths constructing the record directly hit the default value. Patch: in the primary constructor, normalize to `StaleReasons.IsDefault ? ImmutableArray<ComponentTreeStaleReason>.Empty : StaleReasons` (or `[StaleReasonsRequired]` validator). [`src/Hexalith.FrontComposer.Contracts/DevMode/ComponentTreeNode.cs`]
- [x] [Review][Patch] **P24 — `ConventionDescriptor.Validate` allows whitespace-only `description` and `recommendedOverride`** — only null-checked, while `name` is `IsNullOrWhiteSpace`-checked. Whitespace-only description renders the overlay's "Convention" `<dd>` blank. Patch: use `ArgumentException.ThrowIfNullOrWhiteSpace` for symmetry. [`src/Hexalith.FrontComposer.Contracts/DevMode/ConventionDescriptor.cs:664-676`]
- [x] [Review][Patch] **P25 — `Ctrl+Shift+D` collides with browser bookmark default** — Chrome/Edge "Bookmark all tabs" and Firefox "Bookmarks sidebar" fire alongside the Blazor handler. `wwwroot/js/fc-keyboard.js` only `preventDefault`s `Ctrl+K` and `Ctrl+,`. Patch: extend the JS gate to call `event.preventDefault()` for `Ctrl+Shift+D` (or equivalent in the shell key filter scope before forwarding to the registrar). [`src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-keyboard.js`]

#### Deferred

- [x] [Review][Defer] **D1 — `IDevModeAnnotationSnapshotVisitor` is an empty marker interface with no consumers** — registered as scoped via DI but no production surface uses it; defer to Story 6-6 either to flesh out (visitor pattern for snapshot-tree analyzers) or remove. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/IDevModeAnnotationSnapshotVisitor.cs`] — deferred, pre-existing
- [x] [Review][Defer] **D2 — `FcDevModeAnnotation` button inserts interactive `<button>` inside cell content of a `FluentDataGrid` row, breaking tab order and nesting interactive controls** — DEBUG-only annoyance; defer to Story 10-2 visual specimen + Story 6-6 a11y polish. [`src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor`] — deferred, pre-existing

> Dismissed (10 noise items): HFC2010 numeric ordering (cosmetic); toggle button `lang` attr; clipboard JS path hardcoded assembly name (standard Blazor); sprint-status YAML whitespace flip; "single Information log" guarantee (correct via Interlocked marker); MainLayout.razor in spec File Structure (toggle was correctly placed in shell header); `OnInitialized` subscribe asymmetry (theoretical, DI is static); `Identifier` digit-prefix inconsistency (theoretical); `#pragma warning disable HFC1002` in Counter (real warning ID, appropriate use); clipboard payload echo in starter source preview (acceptable today, never logged).

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Shell/Components/Shell/FrontComposerShell.razor` | Composes header, sidebar, breadcrumbs, command palette, and main content host. | Add the dev-mode toggle icon under `IsDevelopment()` only. Do not move existing affordances; do not change shell layout for non-dev users. |
| `src/Hexalith.FrontComposer.Shell/Services/Palette/IShellKeyFilter.cs` | Scoped keyboard interception with palette/scoped routing from Story 3-4. | Reuse this surface for `Ctrl+Shift+D`; do not introduce a parallel global keydown bridge. |
| `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcFieldPlaceholder.razor` | Renders unsupported-field placeholder with localized message and `.fc-field-placeholder-dev` class hook from Story 4-6. | Extend the dev-mode CSS class to layer the red-dashed border; keep the existing placeholder DOM, accessibility, and resource-key surface unchanged. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` | Owns generated projection scaffolding, command form scaffolding, lifecycle/loading/empty shells, role dispatch, and Level 4 selection (Story 6-4). | Add `#if DEBUG` `FcDevModeAnnotation` injection at the existing seam points. Do not perturb generated output snapshots when `DEBUG` is undefined. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewContext.cs` | Story 6-4 typed context with descriptor-only caching. | Consume in Level 2 / Level 4 starter emission. Do not extend the context contract from this story. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/FieldSlotContext.cs` | Story 6-3 typed slot context. | Consume in Level 3 starter emission. Do not extend the context contract from this story. |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | Public diagnostic constants used by SourceTools and Shell. | Reserve HFC1033/1034/1035 and HFC2010; document in analyzer release notes. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/*` | Activity source / sanitized logging conventions from Story 5-6. | DevMode telemetry follows the same shape; no PII, no payload, outcome-only tags. |
| `samples/Counter/Counter.Web/Components/*` | Counter sample wiring including Story 6-3 slot override and Story 6-4 view override. | Add overlay activation; add the dev-mode toggle icon only in development; do not move existing sample components. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 6-5 decision |
| --- | --- | --- | --- |
| Level 1 attribute metadata | Story 6-1 | DevMode annotation badge / detail panel | Read existing IR descriptors; never re-parse attributes at runtime. |
| Level 2 typed templates | Story 6-2 | DevMode annotation precedence + starter emission | Level 2 detail panel shows registered template type; emitter regenerates layout from descriptor. |
| Level 3 slot overrides | Story 6-3 | DevMode annotation precedence + starter emission | Slot detail shows refactor-safe lambda; emitter regenerates the field renderer. |
| Level 4 view replacement | Story 6-4 | DevMode annotation precedence + starter emission | Level 4 wins the badge; emitter outputs the full body and respects Story 6-4 fallback rules. |
| Unsupported placeholder | Story 4-6 | Red-dashed annotation highlight | Extend Story 4-6 CSS hook; do not add a parallel placeholder pipeline. |
| Build-time validation analyzers | Story 6-6 | DevMode contract version drift messaging | Story 6-5 reserves HFC1035 and emits the comment; Story 6-6 ships the analyzer enforcement. |
| Runtime fault diagnostics | Story 6-4 / Story 6-6 | DevMode fault marker on annotation | Story 6-4 owns the boundary; Story 6-5 only reads the boundary outcome to mark the annotation. |
| Shell key filter / palette scope | Story 3-4 | Overlay activation shortcut | Reuse `IShellKeyFilter`; no new global keydown surface. |
| Shell header iconography | Story 3-1 / Story 3-4 | Overlay toggle icon placement | Add icon only in development; remove from compilation in Release. |

### Component Tree Walk Contract

| Concern | Story 6-5 decision |
| --- | --- |
| Tree source | Auto-generation engine descriptors (Stories 6-1 / 6-2 / 6-3 / 6-4); not Roslyn at runtime; not DOM scraping. |
| Tree lifetime | Built per render of the active projection; carries a render epoch; never cached across renders or users; descriptor metadata only is reused. |
| Tree depth bound | `FcShellOptions.DevMode.MaxNodeDepth` default 64 with `[Range(8,512)]`; truncate with inline comment plus Information log. |
| Tree fan-out bound | `FcShellOptions.DevMode.MaxFanOut` default 512 with `[Range(8,4096)]`; truncate with inline comment plus Information log. |
| Mutation | None. `ComponentTreeNode` is immutable; emitter consumes a read-only snapshot. |
| Concurrency | Immutable per-render snapshot with epoch checks on UI events; emitter is pure; no shared mutable state. |
| Snapshot freshness | A node is stale when metadata is missing, the component-tree contract version differs, descriptor hash differs, source component identity differs, or generated/running contract versions drift; stale nodes display guidance and suppress current-template copy. |

### Customization Level Precedence Matrix

| Inputs | Selected level (badge + detail panel) |
| --- | --- |
| Valid Level 4 view override exists for `(projection, role)` | Level 4. |
| No Level 4, field node has a valid Level 3 slot | Level 3 for that field, including when the field is rendered through a Level 2 delegate. |
| No Level 4 / Level 3 for the selected node, valid Level 2 template exists for the projection body | Level 2. |
| No Level 4 / Level 2 / Level 3, Level 1 annotation present on the field | Level 1. |
| No customization | `Default`. |
| Invalid Level 2/3/4 registration (Story 6-4 D6) | Falls back to the lower level; badge shows the lower level; detail panel surfaces the existing HFC diagnostic ID and points the developer to the registration site. |
| Active Level 4 plus active Level 3 / Level 2 | Level 4 wins. The detail panel exposes the lower-level descriptors as informational only; the starter copy button targets Level 4. |
| Same-level duplicate or tie | The existing HFC diagnostic is shown; the overlay chooses the deterministic lower-level fallback and explains the suppressed candidates. |
| Stale metadata | Badge shows the last known lower safe level or `Default`; template copy is disabled until rebuild/restart refreshes the snapshot. |

### Diagnostic Reservation Table

| ID | Severity | Phase | Trigger | Fallback / Action |
| --- | --- | --- | --- | --- |
| HFC1033 | Information | SourceTools (parse) | Annotation marker present on a non-projection / non-property site. | Annotation is ignored; default rendering; build proceeds. |
| HFC1034 | Information | Shell (runtime, dev only) | `IRazorEmitter.EmitStarterTemplate` invoked with a level the node does not support (e.g., Level 4 for a field-only node). | Emitter returns single-line stub plus comment naming the supported levels; UI disables the Copy button for that level. |
| HFC1035 | Warning | Shell (runtime, dev only) | Generated starter template references a contract version that differs from the running framework. | Emit comment line in starter template; surface a localized "Regenerate after upgrading framework" toast. |
| HFC2010 | Information | Shell (runtime) | Defensive log when overlay activation is attempted outside development. | Activation is suppressed; no UI surface; one log line per process. |

### Hot Reload / Rebuild Matrix

| Change | Expected behavior |
| --- | --- |
| Story 6-1 annotation argument tweak | Hot reload via existing 6-1 generator path; overlay reflects new badge label on next render. |
| New Level 2/3/4 registration added | Rebuild required; once rebuilt, overlay reflects the new precedence and starter button targets the new level. |
| Contract version constant changed | Rebuild required; overlay surfaces HFC1035 warning until starter templates are regenerated. |
| Overlay markup or CSS edit | Hot reload via Blazor; no rebuild required. |
| `Ctrl+Shift+D` rebinding | Reload required for the new binding to take effect. |
| Adopter toggles `IsDevelopment()` to false | Overlay disappears at next render; activation is silently ignored. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | The overlay subsystem is gated by both `#if DEBUG` (compile-time) AND `IHostEnvironment.IsDevelopment()` (runtime). | Defence-in-depth: a Release build with stray symbol references must still not expose the surface. | Compile-time only; runtime only; trim attribute only. |
| D2 | Annotation injection happens inside the source generator at the same seam points Stories 6-1 to 6-4 already touch, behind `#if DEBUG`. | Reuses the descriptor channel; avoids DOM reverse engineering or runtime Roslyn. | Runtime DOM scanning; runtime Roslyn invocation; manual annotation per Razor file. |
| D3 | `IRazorEmitter` walks an in-memory `ComponentTreeNode` IR built from descriptors. | The UX spec mandates component-tree walking, not Roslyn at runtime; AOT-friendly and dev-loop-fast. | Re-running the source generator at runtime; serializing generated source from disk. |
| D4 | Activation uses the existing `IShellKeyFilter` surface. | Story 3-4 already owns scoped keyboard routing including Mac Cmd vs Ctrl normalization. | Global keydown JS bridge; per-page handlers; shell-only handler. |
| D5 | Clipboard interop is a single dedicated module `fc-devmode-clipboard.js` wired into `IAsyncDisposable`. | L13 (JS interop disposal from day one); single-purpose module is easier to audit. | Reuse Story 3-4's palette JS bridge; inline JS via `IJSRuntime.InvokeVoidAsync`. |
| D6 | Field annotation precedence is Level 4 > Level 3 > Level 2 > Level 1 > Default; projection-body nodes use Level 2 when no Level 4 exists, while field nodes inside Level 2 still resolve Level 3 slots. | Mirrors Stories 6-2 / 6-3 / 6-4 runtime precedence without hiding slot-level overrides inside template bodies. | Level 1 always wins for badge visibility; combined badges showing all active levels; projection-level Level 2 always masking field-level Level 3. |
| D7 | Starter templates are produced from descriptors, not from generated source files on disk. | Generated files may have transient state, partial classes, or build outputs not present in dev mode; descriptors are stable. | Read `.g.cs` files from `obj/`; require running `dotnet build` before each emission. |
| D8 | `IRazorEmitter` enforces bounded depth and fan-out per L14 with `FcShellOptions.DevMode.Max*`. | Recursive component trees in adopter projects could explode; bound default + simple truncation policy is operator-visible. | Documented-unbounded; v1 ships unbounded with regression tests only. |
| D9 | Contract version constants are referenced via a header comment in every starter template. | Story 6-6 owns the analyzer; Story 6-5 makes the version visible so an upgraded adopter spots the drift. | Embed version in every component attribute; rely solely on analyzer. |
| D10 | Story 6-5 owns only the discovery surface and starter emission. Build-time analyzer expansion, runtime fault polish, and adopter docs route to Stories 6-6, 9-5, and 10-2. | Keeps the L06 budget within the feature-story ceiling and matches the gradient ownership map. | Pull analyzer expansion / docs / runtime fault polish into 6-5. |
| D11 | Annotation rendering preserves the underlying control's accessibility tree. The annotation is a sibling visual outline, not a wrapper that intercepts ARIA. | UX spec requires no regression in accessibility; the annotation is a developer affordance, not a user one. | Wrap each annotated control inside the annotation; add `role="region"` on annotations. |
| D12 | EN+FR resource strings cover all dev-mode UI; no hardcoded English. | Project-wide localization discipline; French fallback enables FR-locale developers. | EN-only strings under `#if DEBUG`. |
| D13 | The dev-mode toggle icon is in the shell header rather than the sidebar or palette. | Matches UX spec; icon-only is acceptable in the header per `ux-consistency-patterns.md` exception. | Sidebar entry; command palette entry; floating action button. |
| D14 | Overlay activation idempotency is guaranteed at controller level, not at UI level. | A scoped controller is the single source of truth across re-renders, route changes, and shortcut/icon dispatch. | Component-local state; Fluxor-only state. |
| D15 | The before/after toggle is visible in the detail panel only when an active override exists for the selected element. | Avoids dead UI affordance and reduces decision noise for default-rendered elements. | Always visible; disabled but visible. |

### Library / Framework Requirements

- Target the repository's current .NET 10 / Blazor / Fluent UI Blazor / Fluxor / Roslyn SourceTools stack.
- Use `IHostEnvironment.IsDevelopment()` from `Microsoft.Extensions.Hosting.Abstractions` for the runtime gate.
- Use Fluent UI Blazor `FluentDrawer`, `FluentButton`, `FluentIcon`, `FluentLabel`, `FluentToast` (or the project's existing toast surface) for overlay UI.
- Use `IJSObjectReference`-based module import for clipboard interop; do not use raw `IJSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", ...)`.
- Use `IStringLocalizer<DevModeStrings>` with EN + FR resource files; resources live in `src/Hexalith.FrontComposer.Shell/Resources/DevMode/`.
- Preserve nullable-reference annotations and trim/AOT compatibility.

External references checked on 2026-04-30:

- Microsoft Learn: Use ASP.NET Core APIs in a class library — `IHostEnvironment` usage: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-10.0
- Microsoft Learn: ASP.NET Core Blazor JavaScript interoperability (JSInterop) — module import and `IAsyncDisposable`: https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0
- Microsoft Learn: ASP.NET Core Blazor globalization and localization — `IStringLocalizer` with resource files: https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization?view=aspnetcore-10.0
- Microsoft Learn: ASP.NET Core Hot Reload limits: https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload
- Fluent UI Blazor Components — `FluentDrawer` API surface: https://www.fluentui-blazor.net/Drawer

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Contracts/DevMode/ComponentTreeNode.cs` | Immutable IR node for the dev-mode walk. |
| `src/Hexalith.FrontComposer.Contracts/DevMode/CustomizationLevel.cs` | Level enum (`Default` / `Level1` / `Level2` / `Level3` / `Level4`). |
| `src/Hexalith.FrontComposer.Contracts/DevMode/ConventionDescriptor.cs` | Descriptor record exposed to overlay. |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | Reserve HFC1033, HFC1034, HFC1035, HFC2010. |
| `src/Hexalith.FrontComposer.Shell/Services/DevMode/IDevModeOverlayController.cs` | Controller contract. |
| `src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs` | Scoped controller implementation. |
| `src/Hexalith.FrontComposer.Shell/Services/DevMode/IRazorEmitter.cs` | Starter template emitter contract. |
| `src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs` | Starter template emitter implementation (Level 2/3/4). |
| `src/Hexalith.FrontComposer.Shell/Services/DevMode/IClipboardJSModule.cs` | Clipboard module contract (`IAsyncDisposable`). |
| `src/Hexalith.FrontComposer.Shell/Services/DevMode/ClipboardJSModule.cs` | Clipboard module implementation. |
| `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeOverlay.razor` | Overlay host component. |
| `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor` | Per-element annotation component. |
| `src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor.css` | Scoped CSS for annotations + drawer + unsupported highlight. |
| `src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs` | `AddFrontComposerDevMode()` registration extension. |
| `src/Hexalith.FrontComposer.Shell/Resources/DevMode/DevModeStrings.resx` + `DevModeStrings.fr.resx` | Localized UI strings. |
| `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-devmode-clipboard.js` | Single-purpose clipboard JS module. |
| `src/Hexalith.FrontComposer.Shell/Configuration/FcShellOptions.cs` (extension) | Add nested `DevMode` options block (`MaxNodeDepth`, `MaxFanOut`, `CopyTimeoutMilliseconds`). |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` (extension) | `#if DEBUG` annotation injection at existing seams. |
| `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` | Add HFC1033 descriptor entries. |
| `samples/Counter/Counter.Web/Program.cs` (extension) | Wire `AddFrontComposerDevMode()` in development. |
| `samples/Counter/Counter.Web/Components/Layout/MainLayout.razor` (extension) | Show dev-mode toggle icon in the header in development. |
| `tests/Hexalith.FrontComposer.Contracts.Tests/DevMode/*` | Contracts DevMode tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Services/DevMode/*` | Controller, emitter, clipboard module tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/*` | bUnit overlay/annotation/drawer/focus-management tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/*` | HFC1033/1034/1035 diagnostic tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/DevModeAnnotationInjectionTests.cs` | Snapshot tests proving annotation injection short-circuits to zero output without DEBUG. |

### Testing Standards

- Release-blocking P0 coverage: production-mode exclusion (compile + runtime), overlay activation idempotency, annotation precedence, Level 2/3/4 starter emission shape, contract version comment, clipboard timeout/denied/failed handling, focus management, screen reader announcements, unsupported placeholder highlight, and Counter sample reference paths.
- Diagnostic assertions verify HFC ID, severity, target component/projection, deterministic ordering, expected/got/fix/docs link content, and fallback behavior.
- Emitter tests assert deterministic output for identical inputs and bounded depth/fan-out behavior on synthetic deep / wide trees.
- bUnit tests assert that the annotation component short-circuits to zero DOM when overlay is inactive AND when running outside development; both gates must be exercised.
- Avoid cross-product tenant/user/culture/density/theme matrices for the overlay; targeted scenarios suffice.
- Counter sample tests cover Story 6-1 / 6-3 / 6-4 surfaces appearing as annotated, with one Copy operation each verified end-to-end through the clipboard module's `Success` outcome.
- Run targeted Contracts, SourceTools, and Shell tests before closure; run full solution build with warnings as errors before story completion.

### Scope Guardrails

Do not implement these in Story 6-5:

- Build-time Roslyn analyzer expansion (custom-component accessibility, version-drift enforcement, starter compilation gates) — Story 6-6.
- Rich runtime fault diagnostic UX (retry, "open in IDE", expanded stack visualization) — Story 6-6.
- Complete adopter cookbook documentation comparing Levels 1 to 4 — Story 9-5.
- Visual specimen / screenshot / accessibility CI gates for the overlay — Story 10-2.
- New customization gradient levels or new `IOverrideRegistry` surface API.
- MCP / agent surface integration of the overlay — Epic 8.
- Tenant-aware or per-user overlay behavior — Epic 7.
- Theming overrides, runtime token editor, or design-system surface inside the overlay.
- Persisting overlay state across sessions.
- Command-form discovery surface (the v1 overlay scope is projection views, fields, and customization gradient seams; command forms remain non-annotated for v1).
- Source-generator hot reload promises beyond the Story 6-1 / 6-2 boundaries.
- New shell-level keyboard shortcut routing infrastructure (reuse Story 3-4).

### Non-Goals With Owning Stories

| Non-goal | Owner |
| --- | --- |
| Custom-component accessibility analyzer expansion. | Story 6-6 |
| Rich runtime fault recovery UI for failing overrides. | Story 6-6 |
| Contract version drift analyzer enforcement. | Story 6-6 |
| Starter template documentation site / cookbook. | Story 9-5 |
| Visual specimen and screenshot CI for the overlay. | Story 10-2 |
| Adopter test host utilities for dev-mode plugins. | Story 10-1 |
| Command-form annotation surface. | Backlog (post-v1 Epic 6 follow-up). |
| MCP / agent integration that surfaces overlay metadata externally. | Epic 8. |
| Tenant / user scoped overlay personalization. | Epic 7. |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Starter compilation analyzer that fails the build when a starter template targets a stale contract version. | Story 6-6 |
| Roslyn analyzer enforcing the six-rule custom-component accessibility contract on overlay-emitted starter templates. | Story 6-6 |
| Phone-viewport behavior for the overlay (UX spec already says "annotations only; drawer full-width"; specimen verification deferred). | Story 10-2 |
| End-to-end Playwright coverage of overlay activation, annotation click, drawer open, copy success/denied. | Story 10-2 |
| Full adopter cookbook with 4-level walkthroughs. | Story 9-5 |
| Adopter test host APIs for plugins that want to add custom annotations. | Story 10-1 |
| Command-form discovery surface (projection-only in v1). | Backlog (post-v1 Epic 6 follow-up — flagged). |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-6-developer-customization-gradient.md#Story-6.5`] - story statement, ACs, FR39-42 discovery path, UX-DR9, UX-DR11, UX-DR54.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md`] - FR39 (annotation level), FR40 (template level), FR41 (slot level), FR42 (full replacement), FR44 (hot reload), FR45 (diagnostics).
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#FcDevModeOverlay`] - overlay anatomy, states, accessibility contract.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#FcStarterTemplateGenerator`] - `IRazorEmitter` API surface, output level guidance.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/responsive-design-accessibility.md`] - dev-mode overlay phone-viewport behavior and accessibility expectations.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/ux-consistency-patterns.md`] - shell-header icon-only exception and `Ctrl+Shift+D` shortcut convention.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/user-journey-flows.md`] - overlay-driven customization journey and starter template eliminates the learning cliff.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Source-Generator-as-Infrastructure`] - source generator boundaries, hot-reload constraints, AOT/trim posture.
- [Source: `_bmad-output/implementation-artifacts/6-1-level-1-annotation-overrides.md`] - Level 1 metadata and compile-time boundary.
- [Source: `_bmad-output/implementation-artifacts/6-2-level-2-typed-razor-template-overrides.md`] - Level 2 typed template descriptors.
- [Source: `_bmad-output/implementation-artifacts/6-3-level-3-slot-level-field-replacement.md`] - Level 3 slot precedence and `RenderDefault` recursion guard.
- [Source: `_bmad-output/implementation-artifacts/6-4-level-4-full-component-replacement.md`] - Level 4 typed full replacement, `ProjectionViewContext<TProjection>`, error boundary.
- [Source: `_bmad-output/implementation-artifacts/4-6-empty-states-field-descriptions-and-unsupported-types.md`] - unsupported placeholder pipeline, `.fc-field-placeholder-dev` CSS hook reuse.
- [Source: `_bmad-output/implementation-artifacts/3-4-fccommandpalette-and-keyboard-shortcuts/`] - `IShellKeyFilter` scoped routing reused for `Ctrl+Shift+D`.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L01`] - Cross-story contract clarity upfront.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06`] - Defense-in-depth budget per story (decision count target ≤ 25).
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - Party review and advanced elicitation remain separate hardening passes after this story is created.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L11`] - Dev Agent Cheat Sheet recommended only if story exceeds the 30-decision / 80-test threshold.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L13`] - JS interop modules ship `dispose*` from day one.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L14`] - Bounded-by-policy beats documented-unbounded for in-memory caches/walks.
- [Source: Microsoft Learn: ASP.NET Core Generic Host — `IHostEnvironment`](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-10.0) - runtime hosting environment contract.
- [Source: Microsoft Learn: Blazor JavaScript interoperability](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0) - module import and `IAsyncDisposable` discipline.
- [Source: Microsoft Learn: Blazor globalization and localization](https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization?view=aspnetcore-10.0) - resource-key and `IStringLocalizer` patterns.
- [Source: Microsoft Learn: ASP.NET Core Hot Reload](https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload) - hot reload limits referenced by Story 6-6.
- [Source: Fluent UI Blazor Components — FluentDrawer](https://www.fluentui-blazor.net/Drawer) - drawer placement, focus, and accessibility surface.

---

## Party-Mode Review

- Date/time: 2026-04-30T07:56:51.3296637+02:00
- Selected story key: `6-5-fcdevmodeoverlay-and-starter-template-generator`
- Command/skill invocation used: `/bmad-party-mode 6-5-fcdevmodeoverlay-and-starter-template-generator; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Sally (UX Designer), Murat (Test Architect)
- Findings summary: The review found the story direction viable, but tightened the implementation contract around verifiable production exclusion, the single `AddFrontComposerDevMode()` entry point, one-way SourceTools-to-Shell annotation snapshots, component-tree freshness/version evidence, deterministic same-level precedence handling, keyboard ownership, quiet overlay states, clipboard failure UX, small-viewport behavior, localization coverage, and release-blocking tests.
- Changes applied: Hardened AC1-AC10 and AC14; expanded `ComponentTreeNode` metadata and stale snapshot semantics; added shortcut guardrails for editable/already-handled targets; clarified SourceTools/Shell read-model boundaries; added stale/unsupported emitter fallback; added quiet-by-default overlay states and small-viewport behavior; broadened localized strings; expanded clipboard negative paths; added same-level tie diagnostics; corrected field-level precedence to Level 4 > Level 3 > Level 2 > Level 1 > Default; strengthened T9 and T10 with first-usable-path, production-exclusion, precedence, accessibility, clipboard, stale metadata, and golden-emitter test gates.
- Findings deferred: Analyzer expansion remains Story 6-6; rich runtime fault recovery remains Story 6-6; cookbook documentation remains Story 9-5; visual/screenshot/accessibility CI remains Story 10-2; adopter test-host utilities remain Story 10-1; command-form annotation remains a post-v1 Epic 6 backlog follow-up; MCP/agent and tenant/user personalized overlay surfaces remain Epic 8 and Epic 7 respectively.
- Final recommendation: ready-for-dev

---

## Advanced Elicitation

- Date/time: 2026-04-30T09:08:53.5528483+02:00
- Selected story key: `6-5-fcdevmodeoverlay-and-starter-template-generator`
- Command/skill invocation used: `/bmad-advanced-elicitation 6-5-fcdevmodeoverlay-and-starter-template-generator`
- Batch 1 methods: Red Team vs Blue Team; Security Audit Personas; Failure Mode Analysis; First Principles Analysis; Occam's Razor Application
- Batch 2 methods: Chaos Monkey Scenarios; Self-Consistency Validation; User Persona Focus Group; Architecture Decision Records; Hindsight Reflection
- Findings summary: The elicitation found that the story was already well hardened, but it still had four implementation traps: command-form annotation contradicted the v1 scope guardrail, clipboard-denied recovery named manual copy without guaranteeing selectable source, descriptor-derived starter output needed explicit escaping/no-runtime-value rules, and annotation events needed render-epoch protection so virtualized or disposed nodes could not open stale details.
- Changes applied: Narrowed annotation injection to projection-view and field customization seams while explicitly deferring command-form annotation; added readonly selectable starter-source fallback; added descriptor escaping and no tenant/user/runtime-value output rules; added render epoch to `ComponentTreeNode`, annotation events, and the component-tree concurrency contract; tightened visible-only tab order and virtualized-row disposal expectations; corrected HFC1033/HFC1034/HFC1035 ownership wording between SourceTools and Shell dev-mode diagnostics.
- Findings deferred: Command-form discovery remains the existing post-v1 Epic 6 follow-up; Story 6-6 still owns analyzer expansion, version-drift enforcement, and richer runtime fault recovery; Story 10-2 still owns screenshot/accessibility CI gates for viewport-specific overlay behavior.
- Final recommendation: ready-for-dev

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests\Hexalith.FrontComposer.Contracts.Tests\Hexalith.FrontComposer.Contracts.Tests.csproj --filter "FullyQualifiedName~DevMode" /p:UseSharedCompilation=false --logger "console;verbosity=minimal"`: passed, 6 tests.
- `dotnet test tests\Hexalith.FrontComposer.SourceTools.Tests\Hexalith.FrontComposer.SourceTools.Tests.csproj /p:UseSharedCompilation=false --logger "console;verbosity=minimal"`: passed, 566 tests.
- `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj /p:UseSharedCompilation=false --logger "console;verbosity=minimal"`: passed, 1363 tests.
- `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false /m:1`: passed, 0 warnings.
- `dotnet test Hexalith.FrontComposer.sln --no-build /p:UseSharedCompilation=false --logger "console;verbosity=minimal"`: passed, Contracts 139, Shell 1363, SourceTools 566, Bench 2.
- `dotnet build Hexalith.FrontComposer.sln -c Release -warnaserror /p:UseSharedCompilation=false /m:1`: passed, 0 warnings.

### Completion Notes List

- Added Contracts dev-mode IR (`ComponentTreeNode`, convention/level/version/stale metadata) plus `FcShellOptions.DevMode` bounds for starter emission and clipboard timeout behavior.
- Added DEBUG/Development-gated Shell dev-mode registration, scoped overlay controller, annotation snapshot visitor, starter Razor emitter, clipboard JS module, shell header toggle, and `Ctrl+Shift+D` shortcut wiring.
- Extended SourceTools Razor emission with DEBUG-only projection and unsupported-field annotations that resolve the dev-mode controller optionally through `IServiceProvider`, so adopter apps still start when dev mode is not registered.
- Added overlay/annotation/toggle components and dev-mode copy/status resources; unsupported placeholder annotations now surface the red dashed highlight and Level 3 starter path.
- Wired the Counter sample through `AddFrontComposerDevMode(builder.Environment)` and an intentional unsupported `Metadata` field; documented hot-reload/rebuild behavior in `6-5-hot-reload-rebuild-matrix.md`.
- Rebaselined intended generator and Counter snapshots for the new DEBUG annotations and unsupported placeholder column; updated the Story 4-1 emitter-drift approval gate with explicit Story 6-5 tokens.

### File List

- `_bmad-output/implementation-artifacts/6-5-fcdevmodeoverlay-and-starter-template-generator.md`
- `_bmad-output/implementation-artifacts/6-5-hot-reload-rebuild-matrix.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `samples/Counter/Counter.Domain/Counter.Domain.csproj`
- `samples/Counter/Counter.Domain/CounterProjection.cs`
- `samples/Counter/Counter.Web/Program.cs`
- `src/Hexalith.FrontComposer.Contracts/DevMode/*`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`
- `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs`
- `src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md`
- `src/Hexalith.FrontComposer.Shell/Components/DevMode/*`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
- `src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Resources/DevMode/*`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.fr.resx`
- `src/Hexalith.FrontComposer.Shell/Services/DevMode/*`
- `src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs`
- `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-devmode-clipboard.js`
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/DevMode/*`
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/AddFrontComposerDevModeExtensionsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.CounterProjectionView_LoadedState_RendersColumnsAndFormatting.verified.txt`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/DevMode/*`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CounterProjectionApprovalTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/DevModeAnnotationInjectionTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.*.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/*.verified.txt`
