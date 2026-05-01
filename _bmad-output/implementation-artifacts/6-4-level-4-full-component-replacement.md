# Story 6.4: Level 4 Full Component Replacement

Status: done

> **Epic 6** - Developer Customization Gradient. **FR42 / FR43 / FR44 / FR45 / FR47 / UX-DR31 / UX-DR54** full-view replacement for generated projection components while preserving FrontComposer shell, lifecycle, accessibility, diagnostics, and gradient inheritance. Applies lessons **L01**, **L06**, **L07**, **L08**, **L10**, **L11**, and **L15**.

---

## Executive Summary

Story 6-4 makes the top customization level usable without letting a full replacement become an escape hatch from framework guarantees:

- Add a typed Level 4 view override contract for replacing a generated projection view body with an adopter component.
- Keep the FrontComposer-owned shell wrapper outside the replacement: navigation, breadcrumbs, density, theme, loading/empty shells, lifecycle state, authorization boundary, telemetry context, and error isolation remain framework-owned.
- Register full replacements through typed descriptors, not stringly `IOverrideRegistry` calls exposed as the primary adopter API.
- Resolve Level 4 at the projection-view body boundary. A full replacement wins over Level 2 layout templates and Level 3 field slots for that projection/role, but the replacement receives generated metadata and optional default-render delegates so it can intentionally compose lower levels.
- Enforce the six custom-component accessibility requirements from the UX spec as build/startup diagnostics where feasible and as executable component tests for the sample path.
- Add build-time contract/version metadata so Story 6-6 can extend validation consistently across Levels 2-4.
- Demonstrate one Counter sample full replacement that preserves lifecycle wrapper integration and accessibility contracts.
- Defer dev-mode overlay starter generation, clipboard flow, rich analyzer coverage, and runtime diagnostic panel polish to Stories 6-5 and 6-6.

---

## Story

As a FrontComposer adopter,
I want to replace a generated projection component entirely with a custom implementation while the framework still owns lifecycle, accessibility, and shell integration,
so that complex views can be custom-built without losing the trust guarantees of the generated experience.

### Adopter Job To Preserve

A developer should be able to take full control over one projection's rendered body only when annotations, templates, and field slots are too small a tool. The adopter owns the custom body; FrontComposer still owns the shell contract around it. Level 4 must feel powerful, but it must not turn into "copy the generated component and quietly fork the framework."

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A developer registers a full view override for a projection type and optional role | The project builds and the app starts | The registration is typed by projection/component type, runtime-resolved from immutable descriptors, and does not require string projection names as the primary API. |
| AC2 | A projection has a valid Level 4 replacement | The generated view renders data | The replacement component renders instead of the default role body, Level 2 template, or Level 3 field-slot path for that projection/role. |
| AC3 | A full replacement renders | FrontComposer composes the generated view | The replacement owns only the projection-view body content region. The lifecycle wrapper, shell navigation, page title, breadcrumbs, theme, density, render context, loading shell, empty-state policy, authorization boundary, telemetry context, diagnostics hooks, disposal hooks, and error isolation remain framework-owned outside the replacement and cannot be suppressed by replacement markup. |
| AC4 | A replacement component receives its context | The component renders | It receives a fresh typed `ProjectionViewContext<TProjection>` or equivalent per render containing projection metadata, items, current `RenderContext`, role, lifecycle state, generated descriptors, localization-safe labels/help text with documented fallback behavior, and explicit default-render delegates where supported. |
| AC5 | A replacement is registered for one role only | The same projection renders in another role | Role-specific precedence applies: valid role-specific replacement wins for that role; otherwise a valid role-agnostic replacement wins; otherwise the existing generated path runs. |
| AC6 | A Level 4 replacement and Level 2/Level 3 overrides all exist for the same projection/role | The projection renders | Level 4 wins by default. Lower-level overrides are skipped unless invoked through named safe context/default delegates that bypass the active Level 4 descriptor, preserve Level 2/3 recursion guards, and do not bypass authorization or privileged framework actions. |
| AC7 | The replacement component is incompatible, open generic without deterministic closure, missing required context, not a Razor component, duplicated for the same projection/role, or version-incompatible | Validation runs | A deterministic HFC diagnostic names ID, severity, validation phase, target component/projection, expected/got/fix/docs link, and the invalid replacement is not silently selected. Ambiguous duplicate exact replacements are deterministic hard diagnostics/startup failures and neither candidate is selected. |
| AC8 | A valid replacement throws during render or disposal | The view renders or the component is torn down | The fault is isolated to the affected replacement boundary; the shell, navigation, and sibling/next projection surfaces remain usable, the fallback UI includes a diagnostic ID, disposal faults are logged without blocking shell cleanup, and sanitized logs include component type, projection type, role, and exception category without item payloads, generated field values, localized text, or raw exception messages. The boundary can recover when the selected descriptor or framework-owned context key changes; rich recovery polish remains Story 6-6 ownership. |
| AC9 | A replacement component is evaluated against the custom-component accessibility contract | Build/startup validation and tests run | The six requirements are enforced, warned, or tested with explicit oracles: accessible name source, keyboard path into/out of the replacement, visible focus, aria-live category parity for lifecycle/loading/empty states, reduced-motion behavior, and forced-colors support. |
| AC10 | The developer edits replacement Razor markup during development | `dotnet watch` / hot reload is active | Razor body edits refresh where Blazor supports hot reload. Registration metadata, contract version, generic context, and component type changes are rebuild-triggering and must not be described as pure hot reload. |
| AC11 | The same projection renders under different tenants, users, cultures, densities, themes, read-only states, and item sets | The registry and replacement host execute repeatedly | Descriptor caching is allowed only for immutable registration metadata. `ProjectionViewContext<TProjection>`, item collections, `RenderContext`, localized strings, rendered fragments, scoped services, delegates, and per-render diagnostics are never cached across renders or users. |
| AC12 | The Counter sample is used as reference evidence | The sample renders | One projection uses a typed full replacement, one projection/role still falls back to generated output, one replacement demonstrates safe default-delegate fallback, lifecycle wrapper semantics are preserved, and automated tests cover accessibility, diagnostics, fallback, and context isolation. |
| AC13 | Story 6-5 later asks for a Level 4 starter template | It consumes 6-4 contracts | The 6-4 context exposes stable starter inputs: projection metadata, role, field/section descriptors, localized accessible names/labels/help text, current render flags, and safe default-render delegates where available. 6-4 does not implement overlay UI, drawer UX, or clipboard copy. |
| AC14 | The feature is evaluated as Level 4 in the customization gradient | A developer compares Levels 1-4 | Level 4 is full projection-view body replacement only. It does not add command-form replacement, shell replacement, runtime theming, DataGrid behavior rewrites, dev-mode overlay, or arbitrary CSS/design-token override APIs. |

---

## Tasks / Subtasks

- [x] T1. Define the public Level 4 registration contract (AC1, AC5, AC7)
  - [x] Add a typed registration API such as `AddViewOverride<TProjection,TComponent>(ProjectionRole? role = null)` over a typed registry/facade.
  - [x] Keep the existing `IOverrideRegistry` string surface internal/provisional or test-only for Level 4; the only supported adopter registration path is typed/generic.
  - [x] Store projection type, optional role, component type, expected contract version, diagnostic state, and registration source in immutable descriptors.
  - [x] Reject duplicate exact `(projection, role)` replacements deterministically. Name both component types where possible.
  - [x] Define role-specific precedence: role-specific Level 4, role-agnostic Level 4, generated customization pipeline.
  - [x] Keep registry lookup bounded and descriptor-only. No runtime assembly scanning during render.

- [x] T2. Introduce `ProjectionViewContext<TProjection>` (AC3, AC4, AC6, AC11, AC13)
  - [x] Add the context under `src/Hexalith.FrontComposer.Contracts/Rendering/`.
  - [x] Include `ProjectionType`, `BoundedContext`, `ProjectionRole`, `IReadOnlyList<TProjection> Items`, `RenderContext`, density/read-only/dev-mode accessors, generated field/section descriptors, lifecycle summary, and localization-safe labels/descriptions.
  - [x] Expose optional default-render delegates only where the framework can prevent recursion. A default delegate for the same projection/role must bypass the selected Level 4 replacement and preserve lower-level recursion guards.
  - [x] Define localized label/help-text fallback behavior. Sample replacements must consume context-provided strings and avoid hard-coded user-facing copy except test fixtures.
  - [x] Keep the context immutable/read-only after construction.
  - [x] Construct context per render from current state. Do not store tenant/user/item/culture payloads in registry descriptors or static fields.
  - [x] Document that custom components must not mutate projection records, dispatch Fluxor state directly, or cache `RenderContext` across users.

- [x] T3. Implement Level 4 registry and validation (AC1, AC5, AC7, AC11)
  - [x] Add a Shell service such as `IProjectionViewOverrideRegistry`.
  - [x] Register generated/runtime descriptors during app startup through a typed intake API.
  - [x] Validate component type compatibility: Blazor component, required `Context` parameter, compatible `ProjectionViewContext<TProjection>`, deterministic generic closure, and expected contract version.
  - [x] Keep validation deterministic across filesystem order, DI registration order, and parallel test execution.
  - [x] Ignore invalid descriptors after diagnostics and fall back to generated rendering unless the failure is an ambiguous duplicate. Ambiguous duplicates fail hard and select neither candidate.
  - [x] Avoid reflection scans during each render. Reflection/metadata inspection, if needed, happens during descriptor validation and is cached as descriptor metadata only.

- [x] T4. Integrate replacement selection into generated projection views (AC2, AC3, AC5, AC6)
  - [x] Extend `RazorEmitter` so generated views check Level 4 replacement descriptors at the body boundary after loading/empty shells are resolved and before default role body dispatch.
  - [x] Keep `FcProjectionSubtitle`, loading skeletons, empty placeholders, grid envelope ownership, navigation, density resolution, and lifecycle/disposal hooks framework-owned unless a specific owner is documented.
  - [x] When no replacement exists, preserve generated output behavior and snapshots where possible.
  - [x] If a replacement exists for a grid role, keep outer framework-owned containers that are required for scroll capture, reconciliation lanes, row-count banners, and density behavior. The replacement owns the body content, not the shell's state plumbing.
  - [x] Treat `ProjectionViewContext<TProjection>.Items` as the current framework-provided render/query window, not an implicit permission to fetch every projection row or bypass paging, virtualization, tenant, or authorization boundaries.
  - [x] Ensure Level 2 and Level 3 resolution do not also run accidentally under Level 4. They are available only through explicit context/default delegates.
  - [x] Assert that active Level 4 output contains no generated fields, sections, commands, or fallback body markup except through explicit default-render delegates.
  - [x] Add recursion guards for replacement components that ask for default rendering.

- [x] T5. Preserve lifecycle, shell, authorization, and telemetry boundaries (AC3, AC8)
  - [x] Ensure `FcLifecycleWrapper` and `ILifecycleStateService` integration remains outside the replacement so command lifecycle semantics do not depend on adopter markup.
  - [x] Preserve navigation routes, breadcrumbs, render context, density/theme cascades, and generated disposal cleanup.
  - [x] Preserve authorization boundaries already provided by the shell. Do not invent Epic 7 policy behavior in this story.
  - [x] Wrap replacement render faults in a narrow error boundary that renders a diagnostic fallback with HFC ID and keeps the rest of the shell usable.
  - [x] Key error-boundary recovery on the selected descriptor plus a framework-owned context generation value so a corrected replacement or changed context can recover without requiring a full shell reload.
  - [x] Contain replacement `IDisposable` / `IAsyncDisposable` faults during teardown; shell disposal, lifecycle cleanup, and sibling surfaces must continue.
  - [x] Log failures with component type, projection type, role, tenant/user redacted or hashed per existing telemetry policy, and exception category. Do not log item payloads, generated field values, localized user text, raw exception messages, or replacement render fragments.

- [x] T6. Enforce the custom-component accessibility contract (AC9)
  - [x] Validate or warn for missing accessible name via visible text or `aria-label` where static analysis can see it.
  - [x] Require keyboard reachability for interactive replacement surfaces; tests should assert focusable controls stay in DOM order and that focus can move into and out of the replacement.
  - [x] Forbid suppressing Fluent focus visibility in generated samples and component CSS; do not override `--colorStrokeFocus2`.
  - [x] Require lifecycle/loading/empty/status announcements to use the same polite/assertive categories as the framework.
  - [x] Require `prefers-reduced-motion` support for custom animations.
  - [x] Require forced-colors support through system color keywords where custom CSS is present.
  - [x] Defer broad Roslyn analyzer coverage and richer remediation UI to Story 6-6, but keep 6-4's sample/test path executable.

- [x] T7. Define diagnostics and version contract metadata (AC7, AC8, AC9, AC10)
  - [x] Reserve stable HFC10xx SourceTools diagnostics for invalid registrations, incompatible context, version drift, and accessibility warnings when discovered at build time.
  - [x] Reserve HFC20xx Shell diagnostics for runtime/startup validation and replacement render failures.
  - [x] Follow the teaching shape: What happened, Expected, Got, Fix, DocsLink.
  - [x] Before implementation closes, record a diagnostic oracle table in this story or test fixtures covering ID, severity, validation phase, assertion point, target component/projection, and fallback behavior for invalid registration, incompatible context, duplicate descriptor, version drift, accessibility warning, and render failure.
  - [x] Include contract version metadata shared with Stories 6-2 and 6-3 where practical.
  - [x] Diagnostic assertions must verify ID, severity, target component/projection, deterministic ordering, and expected/got/fix/docs-link content.

- [x] T8. Hot reload and rebuild matrix (AC10, AC13)
  - [x] Prove Razor markup edits in the replacement component can refresh under `dotnet watch` where Blazor supports the edit.
  - [x] Document rebuild-required changes: registration added/removed, projection type changed, component type changed, generic context changed, contract version changed, duplicate registration introduced, and metadata descriptor changed.
  - [x] Do not promise pure source-generator input hot reload beyond the architecture constraint.
  - [x] Leave user-facing rebuild/restart messaging polish to Story 6-6.

- [x] T9. Counter sample reference implementation (AC12)
  - [x] Add one small full view replacement in the Counter sample.
  - [x] Use Fluent UI primitives and FrontComposer tokens; do not introduce custom design-system CSS.
  - [x] Preserve lifecycle wrapper semantics and accessible names in the replacement.
  - [x] Include a fallback/generated projection or role path in the same sample evidence.
  - [x] Add one invalid-registration fixture for deterministic diagnostics.
  - [x] Keep the sample focused. Do not add a new Orders sample solely for Level 4.

- [x] T10. Tests and verification (AC1-AC14)
  - [x] Contracts tests for `ProjectionViewContext<TProjection>`, descriptor immutability, version constants, and registration extension shape.
  - [x] Registry tests for role-specific precedence, role-agnostic fallback, duplicate rejection, invalid descriptor fallback, immutable snapshot lookup, and no context/cache bleed.
  - [x] SourceTools/emitter tests proving no-replacement fallback remains unchanged and replacement selection happens before Level 2/3 paths.
  - [x] Shell/bUnit tests proving context values, lifecycle wrapper preservation, error boundary isolation, default-render recursion guard, and density/theme/render-context propagation.
  - [x] Shell/bUnit tests proving authorization boundary preservation, breadcrumbs/navigation shell preservation, loading/empty shell preservation, disposal cleanup, telemetry context propagation, and that render/disposal exceptions leave sibling/next projection surfaces usable.
  - [x] Shell/bUnit tests proving error-boundary recovery when descriptor/context generation changes and proving repeated failures do not log item payloads, localized text, raw exception messages, or render fragments.
  - [x] Shell/SourceTools tests proving Level 4 receives only the framework-owned current render/query window and does not introduce an all-data fetch path that bypasses paging, virtualization, tenant, or authorization boundaries.
  - [x] Accessibility tests for accessible names, keyboard reachability, focus visibility, reduced motion, forced-colors CSS, and live-region category preservation in the sample replacement.
  - [x] Targeted repeated-render tests changing two tenants, two users, two cultures, one theme/density variation, read-only state, item list, and scoped-service instances to prove no stale context/render output/delegate/service is reused without full cross-product expansion.
  - [x] Counter sample tests for valid typed replacement, generated fallback role, safe default-delegate fallback, invalid-registration diagnostic, lifecycle/accessibility preservation, localization-safe labels, and context isolation.
  - [x] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [x] Targeted tests: Contracts, SourceTools replacement/diagnostic tests, Shell registry/rendering tests, and Counter sample build/render tests.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Registration/IOverrideRegistry.cs` | Provisional string-based registry with `Register(string projectionType, string overrideType, Type implementationType)` and `Resolve`. | Do not expose this as the Level 4 primary API. Hide it behind typed descriptors/facade if reused. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/IRenderer.cs` | Stable provisional renderer abstraction returning typed outputs such as `RenderFragment`. | Do not redesign the renderer stack. Add only the Level 4 context/descriptor contracts needed. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/RenderContext.cs` | Immutable tenant/user/render-mode/density/read-only context with non-positional `IsDevMode`. | Flow it into `ProjectionViewContext<TProjection>` per render. Do not cache it in descriptors. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` | Owns generated projection scaffolding, injections, lifecycle hooks, loading/empty shells, grid envelopes, and strategy dispatch. | Integrate Level 4 selection at the body boundary without duplicating all strategy emitters. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs` | Owns Default, ActionQueue, StatusOverview, DetailRecord, Timeline, Dashboard data-state body emission. | Default rendering delegates must reuse this path and bypass Level 4 recursion. |
| `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs` | Scoped lifecycle state bus with telemetry, duplicate-message handling, and subscriber isolation. | Level 4 must not make custom components own lifecycle state machine semantics. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/*` | Current worktree contains telemetry instrumentation from Story 5-6. | Replacement diagnostics should follow the sanitized logging/activity pattern and avoid PII. |
| `_bmad-output/implementation-artifacts/6-2-level-2-typed-razor-template-overrides.md` | Defines Level 2 typed template contracts, descriptor-only cache safety, and no runtime reflection scanning. | Level 4 precedence must not break Level 2 fallback or cache-safety rules. |
| `_bmad-output/implementation-artifacts/6-3-level-3-slot-level-field-replacement.md` | Defines Level 3 field-slot contracts, role precedence, `RenderDefault` recursion guard, and descriptor-only lookup. | Level 4 wins by default but may expose explicit default delegates that compose lower levels safely. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 6-4 decision |
| --- | --- | --- | --- |
| Level 1 metadata | Story 6-1 | Full replacement context | Expose labels, descriptions, priority, format hints, and unsupported metadata through generated descriptors. |
| Level 2 templates | Story 6-2 | Level 4 precedence | Level 4 wins over templates for matching projection/role unless explicit default delegates are invoked. |
| Level 3 slots | Story 6-3 | Level 4 precedence/default render | Level 4 wins over slots by default; default delegates may call the generated pipeline with recursion guards. |
| Lifecycle wrapper | Stories 2-3, 2-4, 5-5 | Full replacement host | Wrapper and lifecycle state bus remain framework-owned outside replacement body. |
| DataGrid/grid envelope | Stories 4-3, 4-4, 4-5, 5-4 | Replacement host | Framework-owned grid state, scroll capture, reconciliation, and disposal stay outside or are explicitly delegated. |
| Unsupported placeholder/empty states | Story 4-6 | Replacement context/fallback | Full replacement may opt into default render delegates, but invalid replacement must not silently omit unsupported fields. |
| Error isolation | FR47 / Story 6-6 | Level 4 host | 6-4 provides narrow boundary and diagnostic ID; 6-6 owns richer analyzer/panel polish. |
| Dev overlay/starter generator | Story 6-5 | Level 4 starter template | 6-4 exposes metadata; 6-5 owns UI and clipboard generation. |

### Replacement Precedence Matrix

| Inputs | Selected path |
| --- | --- |
| Valid role-specific Level 4 replacement for `(projection, role)` | Level 4 replacement. |
| No role-specific Level 4, valid role-agnostic Level 4 for projection | Level 4 replacement. |
| Invalid Level 4 descriptor plus valid lower-level customizations | Diagnostic, ignore invalid Level 4, continue to Level 2/3/generated path. |
| Duplicate exact Level 4 replacements | Diagnostic error/startup failure; do not select either silently. |
| No valid Level 4, valid Level 2 template | Level 2 template path. |
| No valid Level 4/2, valid Level 3 slot | Generated role body with slot at field boundary. |
| No valid customization | Existing generated role body. |

### Accessibility Contract For Level 4

| Requirement | Story 6-4 expectation | Deferred owner |
| --- | --- | --- |
| Accessible name | Sample and tests prove visible text or `aria-label`; diagnostics warn when static validation can detect absence. | Story 6-6 for broad analyzer coverage. |
| Keyboard reachability | Replacement host and sample controls stay in DOM order; tests cover focusable controls. | Story 10-2 for full specimen/E2E matrix. |
| Focus visibility | Sample CSS does not suppress Fluent focus ring or `--colorStrokeFocus2`. | Story 6-6 analyzer where feasible. |
| State announcements | Custom lifecycle/status changes use framework polite/assertive categories. | Story 6-6 for complete remediation UI. |
| Reduced motion | Any custom animation uses `prefers-reduced-motion`. | Story 10-2 visual/accessibility specimen expansion. |
| Forced colors | Custom CSS uses system colors in forced-colors mode. | Story 10-2 for CI specimen coverage. |

### Diagnostic Oracle (T7)

| ID | Severity | Validation phase | Assertion point | Target component / projection | Fallback behavior |
| --- | --- | --- | --- | --- | --- |
| HFC1042 | Error | Build (reserved for SourceTools) | `DiagnosticDescriptorTests.AllDescriptorsRegistered` + `AnalyzerReleases.Unshipped.md` | A Level 4 view override targets a non-projection / generic / abstract type | Reserved — emits no runtime fault today; analyzer activation deferred to Story 6-6. |
| HFC1043 | Warning | Startup (registry constructor) | `ProjectionViewOverrideRegistry.Register` → `IsCompatibleComponent` returns false | Replacement component lacks `[Parameter] Context` of `ProjectionViewContext<TProjection>`, is open-generic, abstract, or interface | Descriptor ignored; Level 2/3/generated path runs. Counter sample test `CounterProjectionView_Level4InvalidComponent_LogsHfc1043_AndRendersGeneratedDefault` pins the fallback. |
| HFC1044 | Error | Startup (registry constructor) | `ProjectionViewOverrideRegistry` constructor throws `InvalidOperationException` after the source loop | Two registrations for the same `(projection, role)` tuple with different component types | Hard fail: registry is not constructed; DI activation throws. Idempotent re-registration of the same component type is a no-op. `Registry_DuplicateDifferentComponent_FailsHardOnConstruction` pins the throw. |
| HFC1045 | Warning (incompatible major) / Information (drift within major) | Startup (registry constructor) | `IsCompatibleContractVersion` mismatch → Warning + descriptor ignored; equal major but different packed value → Information log, descriptor accepted | Contract version drift across `ProjectionViewOverrideContractVersion` major / minor / build | Warning path: descriptor ignored; lower-level cascade runs. Information path: descriptor accepted, drift observable in logs. |
| HFC1046 | Warning | Build (reserved) | `DiagnosticDescriptorTests.AllDescriptorsRegistered` | Custom-component accessibility analyzer for missing accessible name / focus suppression / reduced-motion / forced-colors | Reserved — broad analyzer activation deferred to Story 6-6 per T6. Sample-path executable evidence covers AC9 today. |
| HFC2121 | Warning | Render (host error boundary) | `FcProjectionViewOverrideHost<TProjection>.RenderFailure` formats and emits the diagnostic | Replacement throws during `BuildRenderTree` / `OnInitialized` / `OnParametersSet` / `OnAfterRender` / async lifecycle | Boundary catches; sanitized log includes projection / component / role / exception category / hashed tenant + user / consecutive-failure counter / circuit-open state. Items, field values, raw exception text, localized strings, and render fragments are intentionally omitted. After 3 consecutive failures the boundary opens until descriptor or `RenderContext` changes. `Render_ThrowingReplacement_IsolatesFault_AndRendersDiagnosticFallback` and `Render_PersistentlyThrowingReplacement_DoesNotLogPerItemsTick` pin the contract. |

### Hot Reload / Rebuild Matrix

| Change | Expected behavior |
| --- | --- |
| Replacement Razor markup/body edit | Hot reload where Blazor supports the edit; supporting evidence, not sole CI gate. |
| Registration added/removed | Rebuild required; descriptor set changed. |
| Component type changed | Rebuild/startup validation required. |
| `ProjectionViewContext<TProjection>` generic type changed | Rebuild required; contract changed. |
| Expected contract version changed | Rebuild required; validation warning/error expected. |
| Duplicate registration introduced | Rebuild/startup validation required and deterministic diagnostic emitted. |
| Accessibility-affecting markup changed | Hot reload may show visual change; automated tests still own release evidence. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | Level 4 uses typed view override descriptors, not public stringly registry calls. | Full replacement is high blast-radius and needs refactor-safe contracts. | `Register("Projection", "view", typeof(...))` as the adopter API; runtime assembly scan. |
| D2 | Level 4 resolves at the projection body boundary after framework loading/empty/shell scaffolding. | Preserves shell trust while giving adopters full control of data body rendering. | Replace the entire generated component including shell wrappers; replace only individual fields. |
| D3 | Level 4 wins over Levels 2 and 3 by default. | Full replacement is explicitly the top of the gradient. | Let templates and slots still run implicitly under replacement. |
| D4 | Default-render delegates must bypass the active Level 4 descriptor for the same projection/role. | Prevents infinite recursion when replacement wraps generated output. | Call public render path and rely on component author discipline. |
| D5 | Registry caches descriptors only. | Prevents tenant/user/culture/item/render-fragment bleed. | Cache `ProjectionViewContext`, item lists, or rendered fragments. |
| D6 | Invalid replacements fail soft to generated lower-level paths except ambiguous duplicates. | Bad customization should not erase the view; duplicates are hidden policy and must be fixed. | Throw on every invalid component; silently ignore duplicates. |
| D7 | Accessibility is a contract, not optional guidance. | Full replacement has enough power to break WCAG guarantees. | Treat custom component accessibility as adopter-only responsibility. |
| D8 | Story 6-4 provides narrow render-fault isolation; Story 6-6 owns rich diagnostics. | FR47 needs immediate shell protection, but analyzer/panel polish is a separate story. | Pull all error-boundary and analyzer work into 6-4. |
| D9 | Counter remains the required sample. | Keeps solo-maintainer scope controlled and consistent with prior Epic 6 evidence. | Add a new Orders/Operations sample solely for Level 4. |
| D10 | Full replacement is projection-view body replacement only. | Command forms, shell replacement, and theming would expand the story beyond Epic 6.4. | Add command-form replacement and runtime shell override APIs here. |
| D11 | Framework-owned grid envelopes stay outside replacement when needed for state/reconciliation. | Prevents full replacements from accidentally breaking virtualization, scroll capture, and reconnect behavior. | Let replacement own all DataGrid infrastructure. |
| D12 | Hot reload promise is honest: Razor body edits may hot reload; descriptor metadata changes rebuild. | Matches Blazor/source-generator boundaries and avoids false dev-loop expectations. | Promise no-rebuild behavior for registration/generic changes. |
| D13 | Error-boundary recovery and replacement disposal faults are part of the Level 4 v1 contract. | A full replacement can fail after initial render; the shell must remain usable and recover when framework-owned selection/context changes. | Leave recovery/disposal behavior entirely to Story 6-6; require a full page reload after any replacement fault. |
| D14 | Level 4 context passes the current framework-owned render/query window, not an unbounded data access grant. | Preserves paging, virtualization, tenant, authorization, and performance contracts from earlier stories. | Pass all projection rows to replacement components; let replacements query backing stores directly from the view body. |

### Library / Framework Requirements

- Target the repository's current .NET 10 / Blazor / Fluent UI Blazor / Fluxor / Roslyn SourceTools stack.
- Use normal Razor component patterns for replacement components: `[Parameter]`, `[EditorRequired]`, `RenderFragment`, and `DynamicComponent` only when type-based rendering is necessary.
- Use built-in Blazor error-boundary patterns for narrow runtime isolation where they fit the host shape.
- Use Fluent UI Blazor primitives and tokens. Do not add a parallel UI library or custom design-token system.
- Preserve nullable-reference annotations, trim compatibility, and descriptor immutability.
- Keep Contracts dependency-light; Shell owns Blazor-specific runtime hosting details where possible.

External references checked on 2026-04-26:

- Microsoft Learn: Dynamically-rendered ASP.NET Core Razor components: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/dynamiccomponent?view=aspnetcore-10.0
- Microsoft Learn: Handle errors in ASP.NET Core Blazor apps: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-10.0
- Microsoft Learn: ASP.NET Core Blazor CSS isolation: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation?view=aspnetcore-10.0
- Microsoft Learn: ASP.NET Core Hot Reload: https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewContext.cs` | Typed context passed to Level 4 replacement components. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewOverrideDescriptor.cs` | Immutable descriptor for full replacement metadata. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewOverrideContractVersion.cs` | Single source of Level 4 contract version truth. |
| `src/Hexalith.FrontComposer.Contracts/Registration/ProjectionViewOverrideRegistryExtensions.cs` | Typed `AddViewOverride<TProjection,TComponent>` API. |
| `src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/*` | Runtime registry, descriptor validation, lookup, and diagnostics. |
| `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.*` | Replacement host/error boundary/context construction if a host component is needed. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` | Replacement selection at generated body boundary. |
| `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` | Build-time diagnostics for invalid Level 4 metadata. |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | Public diagnostic constants if referenced outside SourceTools. |
| `samples/Counter/Counter.Web/Components/Replacements/*` | Counter Level 4 reference replacement. |
| `samples/Counter/Counter.Web/*` or generated registration hook | Counter replacement registration. |
| `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/*ViewOverride*Tests.cs` | Context/descriptor/registration tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionViewOverrides/*` | Registry, validation, and cache-isolation tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/*ViewOverride*Tests.cs` | bUnit host, error boundary, lifecycle, accessibility tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/*ViewOverride*Tests.cs` | Generated replacement selection and no-replacement fallback tests. |

### Testing Standards

- Release-blocking P0 coverage: typed registration, role precedence, duplicate rejection, component compatibility, no-replacement fallback, replacement selection before Level 2/3, context isolation, render-fault isolation, and accessibility sample path.
- Keep test matrices bounded. Cross-product tenant/user/culture/density/theme permutations require risk justification; targeted repeated-render tests are preferred.
- Diagnostics must assert stable ID, severity, target, deterministic ordering, and expected/got/fix/docs-link shape.
- Registry tests should not depend on render output. Prove descriptor behavior first, then component rendering.
- bUnit tests should prove the host passes fresh context on repeated renders and does not cache rendered fragments.
- Error-boundary tests should prove shell/navigation wrapper remains renderable after replacement fault.
- Accessibility tests should include at least accessible name, keyboard focus path, focus style non-suppression where inspectable, reduced-motion CSS, and forced-colors CSS for the sample component.
- Emitter tests must pin that no-replacement output remains unchanged where possible.
- Run targeted Contracts, SourceTools, and Shell tests before closure; run full solution build with warnings as errors before story completion.

### Scope Guardrails

Do not implement these in Story 6-4:

- Dev-mode overlay, annotation outlines, detail drawer, before/after toggle, or clipboard starter generation.
- Full command-form replacement.
- Shell replacement, navigation replacement, or runtime theme/design-token override APIs.
- Runtime reflection scanning for all replacement components on every render.
- Public stringly override registration as the primary API.
- New DataGrid sorting/filtering/virtualization abstractions owned by replacement components.
- Per-tenant or per-user replacement registrations.
- Caching rendered fragments or `ProjectionViewContext<TProjection>`.
- Broad Roslyn analyzer implementation for every accessibility rule.
- Rich diagnostic panel/retry UI beyond the narrow FR47 fallback.
- A new sample domain solely for Level 4.
- EventStore, SignalR, ETag cache, command idempotency, or observability changes.

### Non-Goals With Owning Stories

| Non-goal | Owner |
| --- | --- |
| Dev-mode discovery UI and "copy full replacement starter" clipboard flow. | Story 6-5 |
| Rich runtime diagnostic panel, retry/recover UX, and complete override error-boundary polish. | Story 6-6 |
| Broad Roslyn analyzer enforcement for all custom-component accessibility rules. | Story 6-6 |
| Full customization cookbook showing one problem solved across Levels 1-4. | Story 9-5 |
| Component test host utilities for adopter-authored replacement components. | Story 10-1 |
| Visual specimen coverage across theme, density, language direction, forced colors, and motion preferences. | Story 10-2 |
| Authorization policy behavior beyond preserving shell boundaries. | Epic 7 |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Overlay-driven Level 4 starter template generation and before/after comparison. | Story 6-5 |
| Complete accessibility analyzer coverage for custom components. | Story 6-6 |
| Rich user-facing diagnostic panel for failing replacement components. | Story 6-6 |
| Cookbook examples comparing annotation, template, slot, and full replacement. | Story 9-5 |
| Adopter test host utilities for replacement component authors. | Story 10-1 |
| Visual/accessibility specimen coverage for full replacements. | Story 10-2 |

---

## Advanced Elicitation

- Date/time: 2026-04-30T08:25:22.7061492+02:00
- Selected story key: `6-4-level-4-full-component-replacement`
- Command/skill invocation used: `/bmad-advanced-elicitation 6-4-level-4-full-component-replacement`
- Batch 1 method names: Pre-mortem Analysis; Red Team vs Blue Team; Failure Mode Analysis; Security Audit Personas; Self-Consistency Validation.
- Reshuffled Batch 2 method names: First Principles Analysis; Comparative Analysis Matrix; Occam's Razor Application; Hindsight Reflection; User Persona Focus Group.
- Findings summary: The elicitation found the story ready, with remaining implementation traps around render-fault recovery, disposal-time failures, over-broad item access, and leak-prone diagnostics. The core design still holds: Level 4 replaces the projection body while FrontComposer owns shell, lifecycle, authorization, telemetry, loading/empty policy, and diagnostics.
- Changes applied: Hardened AC8 for disposal faults, sanitized logging, and recovery after selected descriptor/context changes; clarified T4 that `ProjectionViewContext<TProjection>.Items` is the current framework-owned render/query window, not an all-data fetch permission; expanded T5 with error-boundary recovery keys and disposal-fault containment; expanded T10 with recovery, redaction, disposal, and bounded-data-access tests; added binding decisions D13 and D14.
- Findings deferred: Rich diagnostic panel UX, retry/recover controls, broad accessibility analyzer coverage, and visual specimen matrix remain with Stories 6-6 and 10-2. No new product or architecture decisions were deferred.
- Final recommendation: ready-for-dev

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-6-developer-customization-gradient.md#Story-6.4`] - story statement, ACs, FR42, FR44, UX-DR31, UX-DR54.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#Developer-Customization--Override-System`] - FR42 full replacement, FR43 version validation, FR44 hot reload, FR45 diagnostics, FR47 error isolation.
- [Source: `_bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md#API-Surface`] - customization gradient contracts and solo-maintainer scope filter.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#FcLifecycleWrapper`] - lifecycle wrapper remains framework-owned.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#FcStarterTemplateGenerator`] - future Level 4 starter-template metadata needs.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md#Accessibility-Considerations`] - six-rule custom-component accessibility contract.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Source-Generator-as-Infrastructure`] - source generator, diagnostics, and hot reload constraints.
- [Source: `_bmad-output/implementation-artifacts/6-1-level-1-annotation-overrides.md`] - Level 1 metadata and compile-time boundary.
- [Source: `_bmad-output/implementation-artifacts/6-2-level-2-typed-razor-template-overrides.md`] - Level 2 template descriptors, render-context/cache safety, and no runtime scans.
- [Source: `_bmad-output/implementation-artifacts/6-3-level-3-slot-level-field-replacement.md`] - Level 3 slot precedence, `RenderDefault` recursion guard, and descriptor-only registry.
- [Source: `_bmad-output/implementation-artifacts/4-4-virtual-scrolling-and-column-prioritization.md`] - DataGrid virtualization and density contract.
- [Source: `_bmad-output/implementation-artifacts/4-5-expand-in-row-detail-and-progressive-disclosure.md`] - expand-in-row lifecycle/disposal contract.
- [Source: `_bmad-output/implementation-artifacts/4-6-empty-states-field-descriptions-and-unsupported-types.md`] - empty state, unsupported placeholder, and no-silent-omission discipline.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review and advanced elicitation remain separate hardening passes.
- [Source: `src/Hexalith.FrontComposer.Contracts/Registration/IOverrideRegistry.cs`] - existing provisional registry.
- [Source: `src/Hexalith.FrontComposer.Contracts/Rendering/IRenderer.cs`] - renderer abstraction.
- [Source: `src/Hexalith.FrontComposer.Contracts/Rendering/RenderContext.cs`] - render context contract.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`] - generated projection integration point.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs`] - role body emission to reuse for default delegates.
- [Source: `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs`] - lifecycle state bus and telemetry/logging shape.
- [Source: Microsoft Learn: Dynamically-rendered Razor components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/dynamiccomponent?view=aspnetcore-10.0) - component-by-type rendering and parameter passing.
- [Source: Microsoft Learn: Blazor error handling](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-10.0) - error-boundary behavior.
- [Source: Microsoft Learn: Blazor CSS isolation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation?view=aspnetcore-10.0) - component CSS scoping and build-time selector rewriting.
- [Source: Microsoft Learn: ASP.NET Core Hot Reload](https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload) - hot reload behavior and limits.

---

## Party-Mode Review

Date/time: 2026-04-27T02:10:08+02:00

Selected story: `6-4-level-4-full-component-replacement`

Command/skill invocation used: `/bmad-party-mode 6-4-level-4-full-component-replacement; review;`

Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), Sally (UX Designer)

Findings summary:

- Architecture: Level 4 boundary ownership needed tighter wording so replacement code cannot suppress shell, lifecycle, auth, telemetry, loading/empty/error policy, or framework-owned grid/state envelopes.
- Implementation: typed public registration, Level 2/3 skip behavior, default-render delegate recursion bypass, context immutability, and sanitized error logging needed executable acceptance hooks.
- Test strategy: AC7/AC8/AC9/AC11/AC12 needed sharper diagnostic, render-fault, accessibility, repeated-render, and Counter sample oracles without creating a combinatorial matrix.
- UX/accessibility: localized labels/help text, keyboard path, focus visibility, aria-live parity, reduced motion, forced colors, and safe default-state behavior needed to be testable before development.

Changes applied:

- Hardened AC3, AC4, AC6, AC7, AC8, AC9, AC11, AC12, and AC13 for body-only replacement, per-render context, non-recursive default delegates, deterministic duplicate diagnostics, sanitized render-fault logging, explicit accessibility oracles, descriptor-only caching, Counter fallback evidence, and stable Story 6-5 starter metadata.
- Hardened T1-T7 and T10 for typed-only public API, localized context strings, duplicate hard-fail behavior, no generated body markup under active Level 4 except safe delegates, telemetry redaction, focus/live-region/reduced-motion/forced-colors assertions, diagnostic oracle table, shell-preservation tests, repeated-render isolation, and Counter sample evidence.

Findings deferred:

- Exact contract-version representation (integer, semantic version, or marker interface) remains an implementation decision constrained by T7.
- Exact default-render delegate granularity (whole body, section, or field) remains an implementation decision constrained by AC6 and T2.
- Overlay UI, starter drawer UX, clipboard flow, rich diagnostics panel, broad accessibility analyzer coverage, and visual specimen coverage remain owned by Stories 6-5, 6-6, and 10-2.

Final recommendation: `ready-for-dev`

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests\Hexalith.FrontComposer.Contracts.Tests\Hexalith.FrontComposer.Contracts.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet test tests\Hexalith.FrontComposer.SourceTools.Tests\Hexalith.FrontComposer.SourceTools.Tests.csproj --no-restore -p:UseSharedCompilation=false`
- `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false`
- `dotnet test Hexalith.FrontComposer.sln --no-build -p:UseSharedCompilation=false`

### Completion Notes List

- Added typed Level 4 view override contracts, descriptor/version metadata, and per-render `ProjectionViewContext<TProjection>` with safe default-render delegates.
- Added Shell registration/registry validation, role-specific precedence, duplicate fail-closed behavior, and a narrow error-boundary host with sanitized HFC2121 fallback logging.
- Integrated Level 4 selection into generated projection bodies ahead of Level 2 templates and Level 3 slots while preserving framework-owned loading/empty shells and grid envelopes.
- Added Counter sample full-view replacement and bUnit evidence for valid replacement, invalid fallback diagnostics, and generated fallback behavior.
- Reserved HFC1042-HFC1046 and HFC2121 diagnostics, updated SourceTools snapshots, and rebaselined generated approvals for the new Level 4 branch.
- Verification passed: full solution build with warnings as errors and full solution tests.

### File List

- `_bmad-output/implementation-artifacts/6-4-level-4-full-component-replacement.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `samples/Counter/Counter.Web/Components/Replacements/CounterFullViewReplacement.razor`
- `samples/Counter/Counter.Web/Program.cs`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionViewOverrideRegistry.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewContext.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewOverrideContractVersion.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewOverrideDescriptor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/ProjectionViewOverrideServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideDescriptorSource.cs`
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs`
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionViewOverrideContractsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionViewOverrideHostTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/ProjectionViewOverrideServiceCollectionExtensionsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticDescriptorTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1026ReservationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CounterProjectionApprovalTests.cs`
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

- 2026-05-01: Implemented Story 6.4 Level 4 full component replacement and moved story to review after full build/test verification.
- 2026-05-01: bmad-code-review pass — 49 raw findings across 3 layers (Acceptance Auditor 16, Blind Hunter 15, Edge Case Hunter 18) triaged to 6 decision-needed, 15 patches, 10 deferred, 5 dismissed. Findings appended below.
- 2026-05-01: All 6 decision-needed resolved (DN1 hard-fail registry construction; DN2 drop Items from recovery key + circuit breaker after 3 consecutive failures; DN3 keep fail-closed-on-exact + code comment; DN4 keep role-agnostic Counter registration since CounterProjection has only Default role + acknowledge test-fixture fallback evidence; DN5 hashed tenant/user via SHA256-truncated 8-hex in HFC2121; DN6 lifecycleState derived from `state.IsLoading` and items count, label localization deferred to 6-5/6-6) and all 15 patches applied. Validation: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` clean (0 warnings); `dotnet test Hexalith.FrontComposer.sln` => Contracts 133/0/0 (+1 P15 starter-shape), Shell 1352/0/0 (+6 net for new DN2/P1/P2/P3/P7/P8 + idempotent re-reg + duplicate hard-fail), SourceTools 563/0/0 (14 generator approval snapshots rebaselined for DN6), Bench 2/0/0. Story status review -> done.

### Review Findings

- [x] [Review][Decision] DN1 — Ambiguous duplicate Level 4 registrations fail-soft (HFC1044 Warning + Resolve→null), but spec AC7/D6 mandates "deterministic hard diagnostics/startup failures". Current code: `ProjectionViewOverrideRegistry.cs:118-126` logs Warning; test `Registry_DuplicateDifferentComponent_FailsClosedForExactTuple` asserts soft fail. Compare Story 6-2 HFC1037 which is `DiagnosticSeverity.Error`. Options: (a) escalate HFC1044 to Error and throw `InvalidOperationException` from registry constructor (spec-faithful); (b) ratify the soft-fail design and update AC7/D6 in the story.
- [x] [Review][Decision] DN2 — `OnParametersSet` calls `_boundary.Recover()` whenever `Items` reference changes (`FcProjectionViewOverrideHost.cs:34-45`). Fluxor produces a fresh `IReadOnlyList` per state update even when contents are unchanged, so a deterministically-failing replacement floods HFC2121 logs on every unrelated state tick (sort flip, polling, ETag refresh). Options: (a) compare items by sequence-equal/version token; (b) add a "stuck after N consecutive failures" guard; (c) introduce a framework-owned context-generation token used as the only recovery key (T5 wording suggests this).
- [x] [Review][Decision] DN3 — Ambiguous `(P, RoleX)` entry suppresses an otherwise-valid `(P, role=null)` fallback because `Resolve` returns `null` from the early-return on the ambiguous-exact branch (`ProjectionViewOverrideRegistry.cs:60-67`). Spec D6 is silent on cascade behavior. Options: (a) keep current "fail-closed-on-exact" (safest, surprising), (b) fall through to the role-agnostic descriptor when only the role-specific tuple is ambiguous, (c) emit an additional HFC explaining the suppression so adopters can find it.
- [x] [Review][Decision] DN4 — Counter sample registers Level 4 role-agnostic (`Program.cs` `AddViewOverride<CounterProjection, CounterFullViewReplacement>()`), so every Counter render hits the replacement; the "fallback role" evidence lives only in `CounterStoryVerificationTests.cs:104-147` which builds its own `services` without `AddViewOverride`. Spec AC12 says "one projection/role still falls back to generated output". Options: (a) restrict the live registration to a specific role, (b) accept test-only fallback evidence and update AC12 to acknowledge.
- [x] [Review][Decision] DN5 — `RenderFailure` log (`FcProjectionViewOverrideHost.cs:71-77`) drops tenant/user entirely; spec T5 says "tenant/user redacted or hashed per existing telemetry policy". Stricter is safer, but loses operator-correlation. Options: (a) include hashed tenant/user fields in HFC2121 log + update test, (b) ratify drop-entirely behavior in spec.
- [x] [Review][Decision] DN6 — `lifecycleState: "Loaded"` is an emit-time literal in `RazorEmitter.cs:1226`; `entityLabel`/`entityPluralLabel` are non-localized English literals at lines 1227-1228. The Counter sample renders both into user-visible regions including `<div aria-live="polite">@Context.LifecycleState</div>`. Spec T2 requires "localization-safe labels/descriptions" and "lifecycle summary"; AC9 requires aria-live category parity. Options: (a) plumb `state` into a derived expression (Loading/Empty/Loaded) and route labels through `IStringLocalizer` now, (b) defer localization to Story 6-5/6-6 with explicit AC carve-out, (c) hybrid — derive lifecycle from state now (cheap), defer label localization.
- [x] [Review][Patch] P1 — `Render_ThrowingReplacement_IsolatesFault` test asserts `_logger.Entries.ShouldAllBe(e => e.Exception == null)` but `LogWarning(...)` is called with no exception arg, so the assertion always passes structurally [tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionViewOverrideHostTests.cs:~1332]
- [x] [Review][Patch] P2 — `Render_ValidReplacement_PassesFreshContext` renders once with a one-item context and asserts `count="1"` (tautological); add repeated-render isolation test rotating tenant/user/culture/density/items per T10 [FcProjectionViewOverrideHostTests.cs:~1305-1316]
- [x] [Review][Patch] P3 — `OnParametersSet` reads `Context.Items` / `Context.RenderContext` unconditionally while `BuildRenderTree` short-circuits on null Context — null-deref on second render if Context becomes null [src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.cs:34-45 vs :51]
- [x] [Review][Patch] P4 — Replacement `IDisposable`/`IAsyncDisposable` faults are not contained: ErrorBoundary covers render-pipeline only, host does not implement `DisposeAsync` or wrap inner-component teardown. Spec T5/AC8 require sibling-surface continuation on disposal faults [src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.cs]
- [x] [Review][Patch] P5 — Replacement that internally re-mounts the typed generated body (`<CounterProjectionView />`) re-enters the registry → infinite Level 4 selection. `Context.DefaultBody` is correctly guarded but bare component re-mount is not. Need a cascading "already-inside-host-for-(P,role)" flag [src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionViewOverrideHost.cs:62-66]
- [x] [Review][Patch] P6 — `sectionRenderer` lambda emits an empty fragment for any name except `"Body"` while `Context.Sections` advertises the full section list. Either filter `Sections` to `["Body"]` until full dispatch is wired, or dispatch each section name to its emitted renderer [src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:1230]
- [x] [Review][Patch] P7 — bUnit test for error-boundary recovery on descriptor or RenderContext change is missing; AC8 recovery semantics are unproven by automation [tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionViewOverrideHostTests.cs]
- [x] [Review][Patch] P8 — Add a11y assertions covering keyboard reachability, focus visibility (no `--colorStrokeFocus2` override), `prefers-reduced-motion`, and `forced-colors` for the Counter sample replacement (vacuous "no custom CSS found" guards are acceptable but must exist). T10 a11y subtask currently lacks these oracles [tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs]
- [x] [Review][Patch] P9 — Add the diagnostic oracle table required by T7: ID, severity, validation phase, assertion point, target component/projection, fallback behavior — for HFC1042-1046 + HFC2121. Either as a markdown table in this story or as a structured fixture [story file or tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/]
- [x] [Review][Patch] P10 — `descriptor.RegistrationSource` is collected via `[CallerMemberName]` but never appears in any HFC1043/1044/1045 log message. Diagnostics should interpolate it so adopters can locate the originating call site [src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:78-105,118-124]
- [x] [Review][Patch] P11 — Idempotent re-registration of the same component from two different startup helpers fails-closed because `RegistrationSource` is part of record equality. Either exclude `RegistrationSource` from the `existing.Descriptor == descriptor` check, or compare on `(ProjectionType, Role, ComponentType, ContractVersion)` [src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:114; ProjectionViewOverrideDescriptor.cs:22-30]
- [x] [Review][Patch] P12 — Minor/Build version drift within accepted Major is silently accepted; emit Info-level log when `descriptor.ContractVersion != ProjectionViewOverrideContractVersion.Current` so drift is observable per T7 [src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:129-130]
- [x] [Review][Patch] P13 — HFC1044 "Got: {Existing} and {New}" message ordering follows DI enumeration order, breaking T3 "deterministic across DI registration order". Sort the duplicate pair by `ComponentType.FullName` before logging [src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:118-124]
- [x] [Review][Patch] P14 — Registry constructor only null-checks the enumerable, not its elements; null `ProjectionViewOverrideDescriptorSource` from a misconfigured DI registration NREs in `foreach` [src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:30-38]
- [x] [Review][Patch] P15 — Add a Contracts test pinning the AC13 "stable starter inputs" surface as a single shape (Columns[*].Description, Sections, EntityLabel/PluralLabel, RenderContext, the four delegates simultaneously reachable on a representative projection) so silent trims fail before Story 6-5 picks the contract up [tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionViewOverrideContractsTests.cs]
- [x] [Review][Defer] W1 — AC10 hot-reload subtasks are box-checked without proof artefacts. Add a Hot-Reload Verification note to Dev Agent Record listing exercised edit categories — supporting evidence per spec, not a gating CI step [story file Dev Agent Record] — deferred, supporting-evidence
- [x] [Review][Defer] W2 — Generic-closure compile-time enforcement (TComponent constrained to `ProjectionViewContext<TProjection>`) — Story 6-6 analyzer territory [src/Hexalith.FrontComposer.Shell/Extensions/ProjectionViewOverrideServiceCollectionExtensions.cs:28-48] — deferred, owner: Story 6-6
- [x] [Review][Defer] W3 — `defaultBody` lambda captures local render snapshot; replacement that stores `Context` across renders and later invokes `Context.DefaultBody` reads stale state. Doc warning + future analyzer [src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:1190-1197] — deferred, owner: Story 6-6
- [x] [Review][Defer] W4 — `IProjectionViewOverrideRegistry.Descriptors` exposes only valid non-ambiguous entries; rejected/ambiguous descriptors are visible only via logs. Dev panel needs a structured rejected-list [src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:40-50] — deferred, owner: Story 6-5/6-6 dev overlay
- [x] [Review][Defer] W5 — Cross-story contract version sharing helper (Stories 6-2 / 6-3 / 6-4 each have parallel packed-version constants). Extract `PackedContractVersion` shared helper [src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewOverrideContractVersion.cs] — deferred, refactor
- [x] [Review][Defer] W6 — Sibling-projection-surface usability after Level 4 fault is plausible by ErrorBoundary construction but not test-exercised. Add bUnit harness rendering Counter+throwing-Level4 alongside StatusProjection [tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/] — deferred, low ROI today
- [x] [Review][Defer] W7 — `Context.FieldRenderer` contract for unknown field names is unspecified — no test, no docs [src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:1232; samples/Counter/Counter.Web/Components/Replacements/CounterFullViewReplacement.razor:18-22] — deferred, doc clarification
- [x] [Review][Defer] W8 — `MakeGenericType(descriptor.ProjectionType)` in registry validation has no try/catch; a constraint-violating projection type would take down DI startup. `ProjectionViewContext<TProjection>` has no generic constraint today, so unreachable [src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:1019] — deferred, currently unreachable
- [x] [Review][Defer] W9 — Counter `InvalidViewMissingContext` fixture lives in test code only, not the live sample. T9 wording is fuzzy [tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs:404-446] — deferred, test-fixture covers diagnostic
- [x] [Review][Defer] W10 — `IsCompatibleContractVersion` accepts structurally-malformed packed versions (e.g., Minor > 1000) because divisor-only check ignores Minor budget. Hardening only [src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs:989-990] — deferred, internal-pack only

**Dismissed (5):** HFC1042/HFC1046 reserved-but-unused (T6 explicitly defers analyzer to Story 6-6); `_templateColumnsDescriptor` snapshot reference for non-grid views (Blind couldn't verify; full test suite passes); override selected before loading shells (Blind admitted couldn't verify control flow); Logger null-guard (DI guarantees logger; bUnit instantiation without DI is contrived); `EntityLabel` `ThrowIfNullOrWhiteSpace` fragility (defensive coding; emitter never produces empty).
