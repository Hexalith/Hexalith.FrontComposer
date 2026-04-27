# Story 6.4: Level 4 Full Component Replacement

Status: ready-for-dev

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
| AC8 | A valid replacement throws during render | The view renders | The fault is isolated to the affected replacement boundary; the shell, navigation, and sibling/next projection surfaces remain usable, the fallback UI includes a diagnostic ID, and sanitized logs include component type, projection type, role, and exception category without item payloads or generated field values. Rich recovery polish remains Story 6-6 ownership. |
| AC9 | A replacement component is evaluated against the custom-component accessibility contract | Build/startup validation and tests run | The six requirements are enforced, warned, or tested with explicit oracles: accessible name source, keyboard path into/out of the replacement, visible focus, aria-live category parity for lifecycle/loading/empty states, reduced-motion behavior, and forced-colors support. |
| AC10 | The developer edits replacement Razor markup during development | `dotnet watch` / hot reload is active | Razor body edits refresh where Blazor supports hot reload. Registration metadata, contract version, generic context, and component type changes are rebuild-triggering and must not be described as pure hot reload. |
| AC11 | The same projection renders under different tenants, users, cultures, densities, themes, read-only states, and item sets | The registry and replacement host execute repeatedly | Descriptor caching is allowed only for immutable registration metadata. `ProjectionViewContext<TProjection>`, item collections, `RenderContext`, localized strings, rendered fragments, scoped services, delegates, and per-render diagnostics are never cached across renders or users. |
| AC12 | The Counter sample is used as reference evidence | The sample renders | One projection uses a typed full replacement, one projection/role still falls back to generated output, one replacement demonstrates safe default-delegate fallback, lifecycle wrapper semantics are preserved, and automated tests cover accessibility, diagnostics, fallback, and context isolation. |
| AC13 | Story 6-5 later asks for a Level 4 starter template | It consumes 6-4 contracts | The 6-4 context exposes stable starter inputs: projection metadata, role, field/section descriptors, localized accessible names/labels/help text, current render flags, and safe default-render delegates where available. 6-4 does not implement overlay UI, drawer UX, or clipboard copy. |
| AC14 | The feature is evaluated as Level 4 in the customization gradient | A developer compares Levels 1-4 | Level 4 is full projection-view body replacement only. It does not add command-form replacement, shell replacement, runtime theming, DataGrid behavior rewrites, dev-mode overlay, or arbitrary CSS/design-token override APIs. |

---

## Tasks / Subtasks

- [ ] T1. Define the public Level 4 registration contract (AC1, AC5, AC7)
  - [ ] Add a typed registration API such as `AddViewOverride<TProjection,TComponent>(ProjectionRole? role = null)` over a typed registry/facade.
  - [ ] Keep the existing `IOverrideRegistry` string surface internal/provisional or test-only for Level 4; the only supported adopter registration path is typed/generic.
  - [ ] Store projection type, optional role, component type, expected contract version, diagnostic state, and registration source in immutable descriptors.
  - [ ] Reject duplicate exact `(projection, role)` replacements deterministically. Name both component types where possible.
  - [ ] Define role-specific precedence: role-specific Level 4, role-agnostic Level 4, generated customization pipeline.
  - [ ] Keep registry lookup bounded and descriptor-only. No runtime assembly scanning during render.

- [ ] T2. Introduce `ProjectionViewContext<TProjection>` (AC3, AC4, AC6, AC11, AC13)
  - [ ] Add the context under `src/Hexalith.FrontComposer.Contracts/Rendering/`.
  - [ ] Include `ProjectionType`, `BoundedContext`, `ProjectionRole`, `IReadOnlyList<TProjection> Items`, `RenderContext`, density/read-only/dev-mode accessors, generated field/section descriptors, lifecycle summary, and localization-safe labels/descriptions.
  - [ ] Expose optional default-render delegates only where the framework can prevent recursion. A default delegate for the same projection/role must bypass the selected Level 4 replacement and preserve lower-level recursion guards.
  - [ ] Define localized label/help-text fallback behavior. Sample replacements must consume context-provided strings and avoid hard-coded user-facing copy except test fixtures.
  - [ ] Keep the context immutable/read-only after construction.
  - [ ] Construct context per render from current state. Do not store tenant/user/item/culture payloads in registry descriptors or static fields.
  - [ ] Document that custom components must not mutate projection records, dispatch Fluxor state directly, or cache `RenderContext` across users.

- [ ] T3. Implement Level 4 registry and validation (AC1, AC5, AC7, AC11)
  - [ ] Add a Shell service such as `IProjectionViewOverrideRegistry`.
  - [ ] Register generated/runtime descriptors during app startup through a typed intake API.
  - [ ] Validate component type compatibility: Blazor component, required `Context` parameter, compatible `ProjectionViewContext<TProjection>`, deterministic generic closure, and expected contract version.
  - [ ] Keep validation deterministic across filesystem order, DI registration order, and parallel test execution.
  - [ ] Ignore invalid descriptors after diagnostics and fall back to generated rendering unless the failure is an ambiguous duplicate. Ambiguous duplicates fail hard and select neither candidate.
  - [ ] Avoid reflection scans during each render. Reflection/metadata inspection, if needed, happens during descriptor validation and is cached as descriptor metadata only.

- [ ] T4. Integrate replacement selection into generated projection views (AC2, AC3, AC5, AC6)
  - [ ] Extend `RazorEmitter` so generated views check Level 4 replacement descriptors at the body boundary after loading/empty shells are resolved and before default role body dispatch.
  - [ ] Keep `FcProjectionSubtitle`, loading skeletons, empty placeholders, grid envelope ownership, navigation, density resolution, and lifecycle/disposal hooks framework-owned unless a specific owner is documented.
  - [ ] When no replacement exists, preserve generated output behavior and snapshots where possible.
  - [ ] If a replacement exists for a grid role, keep outer framework-owned containers that are required for scroll capture, reconciliation lanes, row-count banners, and density behavior. The replacement owns the body content, not the shell's state plumbing.
  - [ ] Ensure Level 2 and Level 3 resolution do not also run accidentally under Level 4. They are available only through explicit context/default delegates.
  - [ ] Assert that active Level 4 output contains no generated fields, sections, commands, or fallback body markup except through explicit default-render delegates.
  - [ ] Add recursion guards for replacement components that ask for default rendering.

- [ ] T5. Preserve lifecycle, shell, authorization, and telemetry boundaries (AC3, AC8)
  - [ ] Ensure `FcLifecycleWrapper` and `ILifecycleStateService` integration remains outside the replacement so command lifecycle semantics do not depend on adopter markup.
  - [ ] Preserve navigation routes, breadcrumbs, render context, density/theme cascades, and generated disposal cleanup.
  - [ ] Preserve authorization boundaries already provided by the shell. Do not invent Epic 7 policy behavior in this story.
  - [ ] Wrap replacement render faults in a narrow error boundary that renders a diagnostic fallback with HFC ID and keeps the rest of the shell usable.
  - [ ] Log failures with component type, projection type, role, tenant/user redacted or hashed per existing telemetry policy, and exception category. Do not log item payloads, generated field values, localized user text, or replacement render fragments.

- [ ] T6. Enforce the custom-component accessibility contract (AC9)
  - [ ] Validate or warn for missing accessible name via visible text or `aria-label` where static analysis can see it.
  - [ ] Require keyboard reachability for interactive replacement surfaces; tests should assert focusable controls stay in DOM order and that focus can move into and out of the replacement.
  - [ ] Forbid suppressing Fluent focus visibility in generated samples and component CSS; do not override `--colorStrokeFocus2`.
  - [ ] Require lifecycle/loading/empty/status announcements to use the same polite/assertive categories as the framework.
  - [ ] Require `prefers-reduced-motion` support for custom animations.
  - [ ] Require forced-colors support through system color keywords where custom CSS is present.
  - [ ] Defer broad Roslyn analyzer coverage and richer remediation UI to Story 6-6, but keep 6-4's sample/test path executable.

- [ ] T7. Define diagnostics and version contract metadata (AC7, AC8, AC9, AC10)
  - [ ] Reserve stable HFC10xx SourceTools diagnostics for invalid registrations, incompatible context, version drift, and accessibility warnings when discovered at build time.
  - [ ] Reserve HFC20xx Shell diagnostics for runtime/startup validation and replacement render failures.
  - [ ] Follow the teaching shape: What happened, Expected, Got, Fix, DocsLink.
  - [ ] Before implementation closes, record a diagnostic oracle table in this story or test fixtures covering ID, severity, validation phase, assertion point, target component/projection, and fallback behavior for invalid registration, incompatible context, duplicate descriptor, version drift, accessibility warning, and render failure.
  - [ ] Include contract version metadata shared with Stories 6-2 and 6-3 where practical.
  - [ ] Diagnostic assertions must verify ID, severity, target component/projection, deterministic ordering, and expected/got/fix/docs-link content.

- [ ] T8. Hot reload and rebuild matrix (AC10, AC13)
  - [ ] Prove Razor markup edits in the replacement component can refresh under `dotnet watch` where Blazor supports the edit.
  - [ ] Document rebuild-required changes: registration added/removed, projection type changed, component type changed, generic context changed, contract version changed, duplicate registration introduced, and metadata descriptor changed.
  - [ ] Do not promise pure source-generator input hot reload beyond the architecture constraint.
  - [ ] Leave user-facing rebuild/restart messaging polish to Story 6-6.

- [ ] T9. Counter sample reference implementation (AC12)
  - [ ] Add one small full view replacement in the Counter sample.
  - [ ] Use Fluent UI primitives and FrontComposer tokens; do not introduce custom design-system CSS.
  - [ ] Preserve lifecycle wrapper semantics and accessible names in the replacement.
  - [ ] Include a fallback/generated projection or role path in the same sample evidence.
  - [ ] Add one invalid-registration fixture for deterministic diagnostics.
  - [ ] Keep the sample focused. Do not add a new Orders sample solely for Level 4.

- [ ] T10. Tests and verification (AC1-AC14)
  - [ ] Contracts tests for `ProjectionViewContext<TProjection>`, descriptor immutability, version constants, and registration extension shape.
  - [ ] Registry tests for role-specific precedence, role-agnostic fallback, duplicate rejection, invalid descriptor fallback, immutable snapshot lookup, and no context/cache bleed.
  - [ ] SourceTools/emitter tests proving no-replacement fallback remains unchanged and replacement selection happens before Level 2/3 paths.
  - [ ] Shell/bUnit tests proving context values, lifecycle wrapper preservation, error boundary isolation, default-render recursion guard, and density/theme/render-context propagation.
  - [ ] Shell/bUnit tests proving authorization boundary preservation, breadcrumbs/navigation shell preservation, loading/empty shell preservation, disposal cleanup, telemetry context propagation, and that render exceptions leave sibling/next projection surfaces usable.
  - [ ] Accessibility tests for accessible names, keyboard reachability, focus visibility, reduced motion, forced-colors CSS, and live-region category preservation in the sample replacement.
  - [ ] Targeted repeated-render tests changing two tenants, two users, two cultures, one theme/density variation, read-only state, item list, and scoped-service instances to prove no stale context/render output/delegate/service is reused without full cross-product expansion.
  - [ ] Counter sample tests for valid typed replacement, generated fallback role, safe default-delegate fallback, invalid-registration diagnostic, lifecycle/accessibility preservation, localization-safe labels, and context isolation.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [ ] Targeted tests: Contracts, SourceTools replacement/diagnostic tests, Shell registry/rendering tests, and Counter sample build/render tests.

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

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

(to be filled in by dev agent)

### File List

(to be filled in by dev agent)
