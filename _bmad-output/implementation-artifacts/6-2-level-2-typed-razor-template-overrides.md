# Story 6.2: Level 2 Typed Razor Template Overrides

Status: ready-for-dev

> **Epic 6** - Developer Customization Gradient. **FR40 / FR43 / FR44 / UX-DR54** template-level customization for generated projection layouts. Applies lessons **L01**, **L06**, **L07**, **L08**, **L10**, **L11**, and **L15**.

---

## Executive Summary

Story 6-2 makes Level 2 of the customization gradient real without jumping to slot replacement or full component replacement:

- Introduce a typed projection template contract that lets an adopter rearrange section-level layout while still delegating individual field rendering to FrontComposer.
- Support Razor templates in the consuming Razor/Web project through an explicit compile-time marker on the template partial class or companion `.razor.cs` file. Do not rely on fragile parsing of Razor-generated `.g.cs` output.
- Generate a template manifest/registration artifact at compile time so adopters do not write runtime override registration code.
- Extend generated projection views to check the compile-time-backed template registry before falling back to the default role-specific layout.
- Preserve Level 1 annotations, field format hints, column descriptions, unsupported placeholders, empty states, filtering, sorting, virtualization, expand-in-row, and badge rendering inside template sections.
- Add build-time contract/version validation for template markers, with fail-soft warnings and explicit HFC diagnostics.
- Demonstrate one Counter sample template that rearranges projection layout but leaves field rendering to the framework.
- Keep `FcDevModeOverlay`, starter-template clipboard UI, slot overrides, full replacement, custom error boundaries, and visual specimen CI out of scope.

---

## Story

As a developer,
I want to override generated projection layout via typed Razor templates bound to domain model contracts,
so that I can rearrange section layouts and field groupings without replacing the whole component or reimplementing field rendering.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A developer creates a Razor component template for a projection view and marks its companion class with the Level 2 template contract | The project builds | A typed `ProjectionTemplateContext<TProjection>` parameter provides projection metadata, rows/items, render context, role, columns, and generated field/section render delegates. |
| AC2 | The template controls section-level layout | It renders a projection view | The template can reorder sections, group generated fields, and adjust visual hierarchy while individual field rendering still uses FrontComposer-generated delegates. |
| AC3 | A Level 2 template marker is present | SourceTools runs in the Razor/Web project compilation | A compile-time manifest/registration artifact is emitted; the adopter does not call `IOverrideRegistry.Register` manually. |
| AC4 | A projection view has a matching Level 2 template registration | The generated projection view renders data | The framework uses the template instead of the default layout; when no template exists, output remains byte-for-byte compatible with current default emission where possible. |
| AC5 | The template declares an expected FrontComposer contract version | The installed framework contract version differs | A build-time warning names the template, projection type, expected version, actual version, and docs-linked diagnostic ID. |
| AC6 | A template marker references a non-projection type, an incompatible template component, or a missing `Context` parameter | SourceTools validates the marker | A build-time warning or error follows the existing HFC teaching-message shape: expected, got, fix, docs link. |
| AC7 | A template is edited while hot reload / `dotnet watch` is active | The Razor file changes | The running app reflects Razor markup changes without application restart; changes to marker metadata are documented as rebuild-triggering generator inputs. |
| AC8 | Story 6-5 later requests a Level 2 starter template | It consumes the 6-2 contracts | The 6-2 contract exposes enough metadata for a starter generator to produce a typed `Context` parameter, current Fluent UI component shape, and contract comments, but 6-2 does not implement the overlay or clipboard flow. |
| AC9 | The Counter sample is used as reference evidence | The sample renders | At least one Level 2 template rearranges the Counter projection layout while preserving Level 1 annotation output from Story 6-1. |
| AC10 | Tests exercise Level 2 templates | Build and targeted suites run | SourceTools, Shell, and sample tests prove fallback behavior, template selection, version warning, invalid marker diagnostics, and no regression to generated field rendering. |

---

## Tasks / Subtasks

- [ ] T1. Define the Level 2 template contract surface (AC1, AC2, AC8)
  - [ ] Add `ProjectionTemplateContext<TProjection>` under `src/Hexalith.FrontComposer.Contracts/Rendering/`.
  - [ ] Include: `ProjectionType`, `BoundedContext`, `ProjectionRole`, `RenderContext`, `IReadOnlyList<TProjection> Items`, immutable column/section descriptors, and generated render delegates for field/section emission.
  - [ ] Keep the context immutable or read-only; templates must not mutate Fluxor state or DataGrid navigation state directly.
  - [ ] Add a typed template interface or base contract, for example `IProjectionTemplate<TProjection>`, only if it removes ambiguity. Avoid a large hierarchy.
  - [ ] Use `RenderFragment<ProjectionTemplateContext<TProjection>>` / typed child content patterns aligned with Blazor templated components.

- [ ] T2. Add explicit compile-time template markers (AC3, AC5, AC6)
  - [ ] Add a marker attribute in Contracts, for example `ProjectionTemplateAttribute`, targeting classes.
  - [ ] Require marker metadata: projection type, expected contract version, and template level (`Template` / Level 2).
  - [ ] Support Razor templates through a companion partial class in `.razor.cs`; do not depend on analyzing Razor-generated `.g.cs` output shape.
  - [ ] Reject or warn when the marker points to a non-`[Projection]` type, an open generic template, missing projection type, or a template component without the required typed `Context` parameter.
  - [ ] Keep marker data dependency-free and trim-friendly.

- [ ] T3. Extend SourceTools discovery for template markers in Razor/Web compilations (AC3, AC5, AC6)
  - [ ] Add an incremental pipeline for `[ProjectionTemplate]` markers using the same incremental discipline as projection/command discovery.
  - [ ] Emit a template manifest/registration artifact in the consuming compilation.
  - [ ] Ensure generated template registration is deterministic by projection FQN and template FQN.
  - [ ] Add diagnostics with new reserved HFC10xx IDs; document them in `DiagnosticDescriptors` and `FcDiagnosticIds` if public symbolic constants are needed.
  - [ ] Diagnostics must include expected/got/fix/docs-link content per FR45.

- [ ] T4. Add a typed runtime registry fed only by generated registrations (AC3, AC4)
  - [ ] Add a Shell service such as `IProjectionTemplateRegistry`.
  - [ ] Register generated template descriptors from SourceTools output during app startup or generated domain registration.
  - [ ] Generated projection views may query this typed registry to select a template, but adopter-authored code must not call stringly `IOverrideRegistry.Register` for Level 2.
  - [ ] Keep the existing `IOverrideRegistry` placeholder untouched unless it is intentionally adapted behind the typed facade. Do not expose its stringly API as the Level 2 developer experience.
  - [ ] Define duplicate-template behavior: one warning/error at build time where possible; runtime registry should fail predictably if duplicate descriptors survive.

- [ ] T5. Integrate template selection into generated projection views (AC2, AC4)
  - [ ] Extend `RazorEmitter` / `ProjectionRoleBodyEmitter` so each generated projection view checks for a matching Level 2 template before default role-specific body emission.
  - [ ] Preserve loading, empty state, subtitle, lifecycle hooks, DataGrid navigation, `RenderContext`, and disposal semantics outside the template override.
  - [ ] Ensure default output stays unchanged when no template exists.
  - [ ] Templates can rearrange generated sections, but individual field rendering must call framework delegates so badge, description, unsupported placeholder, empty-state, relative/currency formatting, filter/sort metadata, and accessibility behavior remain owned by FrontComposer.
  - [ ] Avoid direct `RenderTreeBuilder` sequence manipulation in adopter templates. Adopters write normal Razor markup; generated code handles low-level builder output.

- [ ] T6. Preserve role and DataGrid contracts inside templates (AC2, AC4, AC10)
  - [ ] Define which role surfaces Level 2 can override in v1: Default, ActionQueue, StatusOverview, DetailRecord, Timeline, and Dashboard fallback.
  - [ ] For grid-based roles, expose field/section render delegates without allowing the template to bypass `FcColumnPrioritizer`, filtering, sorting, virtualized page loading, expand-in-row cleanup, or row-key semantics.
  - [ ] For DetailRecord/Timeline, expose generated detail/timeline item fragments with the same localization and accessibility conventions as current default output.
  - [ ] If any role cannot safely support Level 2 in this story, emit a clear diagnostic and document the owner story instead of silently ignoring the template.

- [ ] T7. Build-time contract/version validation (AC5, AC6)
  - [ ] Establish a single template contract version constant for Level 2.
  - [ ] Validate marker `ExpectedContractVersion` against the installed contracts/source-tools version.
  - [ ] Warn on minor mismatch; error only for a known incompatible major/contract-shape mismatch.
  - [ ] Include actionable copy: `Template expects FrontComposer v{expected}, installed v{actual}. See HFC{id}.`
  - [ ] Add tests proving warnings are deterministic and deduped per template.

- [ ] T8. Hot reload and dev-loop behavior (AC7)
  - [ ] Prove normal Razor markup edits in the template flow through Blazor hot reload / `dotnet watch` without restart.
  - [ ] Document and test the distinction between Razor markup edits and marker/contract metadata edits. Marker changes are generator inputs and may require rebuild.
  - [ ] Do not promise pure source-generator input hot reload beyond what Story 6-1 already documented.
  - [ ] Add a Known Gap row for any unsupported restart/rebuild messaging owned by Story 6-6.

- [ ] T9. Counter sample reference implementation (AC9)
  - [ ] Add a small `CounterProjection` Level 2 template in the sample Web/Razor project.
  - [ ] The sample should rearrange the projection into a compact summary/detail layout, not create a new sample domain.
  - [ ] Preserve the Level 1 annotation evidence from Story 6-1; the template must show that labels/descriptions/formatters still flow through generated delegates.
  - [ ] Add sample or generated-output tests proving the template is selected.

- [ ] T10. Tests and verification (AC1-AC10)
  - [ ] Contracts tests for `ProjectionTemplateContext<TProjection>`, marker attribute constructor/usage, contract version constant, and immutable descriptor shape.
  - [ ] SourceTools tests for valid marker discovery, deterministic manifest output, duplicate marker behavior, invalid projection type, missing `Context`, version mismatch, and diagnostics.
  - [ ] Emitter tests for no-template fallback byte stability and template-selected output path.
  - [ ] Shell tests for `IProjectionTemplateRegistry` registration, lookup, duplicate handling, and disposal-safe rendering.
  - [ ] bUnit/sample tests for Counter template rendering and preservation of generated field fragments.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [ ] Targeted tests: Contracts, SourceTools template tests, Shell registry/rendering tests, and Counter sample build.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Registration/IOverrideRegistry.cs` | Placeholder string-based override registry with `Register(string projectionType, string overrideType, Type implementationType)`. | Do not make this the public Level 2 API. If reused internally, hide it behind typed contracts and generated registration. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/IRenderer.cs` | Provisional generic renderer contract returning typed outputs such as `RenderFragment`. | Do not redesign the rendering abstraction for this story. Add the smallest template-specific context needed. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/RenderContext.cs` | Immutable record carrying tenant, user, render mode, density, read-only, and dev-mode flag. | Flow it through template context so templates inherit shell behavior. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionContext.cs` | Row-level context for derivable command fields. | Keep it separate from template context; template context is layout-level, not command derivation state. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModel.cs` | Transform-stage projection view metadata: type, namespace, bounded context, columns, strategy, labels, empty-state CTA. | Extend only as needed for template descriptors. Avoid stuffing runtime services into transform models. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModelTransform.cs` | Maps `DomainModel` to `RazorModel`, preserves stable priority sorting, emits role and column diagnostics. | Template support must not reorder columns or alter role fallback diagnostics when no template exists. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` | Emits generated projection component scaffolding, injections, lifecycle hooks, formatting helpers, and strategy dispatch. | Integrate template selection at the body boundary, not by duplicating all strategy emitters. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs` | Owns Default, ActionQueue, StatusOverview, DetailRecord, Timeline, Dashboard fallback body emission. | Field/section delegates should reuse this logic rather than introduce parallel rendering. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs` | Owns generated field/column rendering including badge, unsupported placeholder, numeric/date/enum behavior. | Templates must call generated delegates that still route through this emitter. |
| `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` | Registers Shell services, but has no concrete override registry implementation today. | Add template registry registration here if Shell owns the runtime lookup. Respect scoped/singleton lifetime rules. |
| `samples/Counter/Counter.Domain/CounterProjection.cs` | Minimal projection used by generated view tests. Story 6-1 adds Level 1 annotation evidence. | Keep it readable; do not add an Orders/TaskTracker sample just for Level 2. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 6-2 decision |
| --- | --- | --- | --- |
| Level 1 annotations and format hints | Story 6-1 | Level 2 template context and render delegates | Templates inherit generated field behavior; they do not reimplement labels/formatting. |
| Role-specific projection bodies | Stories 4-1 through 4-6 | Template render delegates | Section/field fragments reuse existing emitters and role semantics. |
| DataGrid virtualization and navigation | Stories 4-3, 4-4, 4-5 | Grid templates | Templates cannot bypass page loading, row-key, expanded-row cleanup, or persistence contracts. |
| Empty states and unsupported placeholders | Story 4-6 | Template-selected views | Loading/empty/unsupported behavior remains framework-owned. |
| Source-generator rebuild honesty | Architecture + Story 6-1 | Hot reload AC | Razor markup hot reload is supported; marker metadata changes are generator inputs and may require rebuild. |
| Starter template generator | Story 6-5 | Level 2 contracts | 6-2 supplies descriptors/context shape; 6-5 builds overlay UI and clipboard copy. |
| Build-time diagnostics | Architecture + prior HFC10xx IDs | Template validation | Reserve new HFC10xx diagnostics with teaching messages and docs links. |
| Future Level 3 slots | Story 6-3 | Templates | Level 2 must not add slot-level lambda registration or field component replacement. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | Level 2 templates are Razor components plus an explicit marker on a companion partial class. | Razor markup stays natural, while SourceTools gets stable user-authored syntax to inspect. | Parse `.razor.g.cs`; require only runtime DI registration. |
| D2 | Generated compile-time registration feeds a typed template registry. | Adopters do not write imperative override registration, but generated views can still select templates at runtime. | Stringly `IOverrideRegistry` as public API; reflection scan on every startup. |
| D3 | Template context owns layout metadata and generated render delegates, not raw emitter internals. | Keeps templates stable across emitter refactors and prevents adopter dependence on generated private methods. | Expose `RazorModel` directly; expose `RenderTreeBuilder` helpers to adopters. |
| D4 | Field rendering remains framework-owned inside Level 2. | Prevents duplicate badge/format/filter/placeholder/accessibility implementations. | Let templates render fields manually from `TProjection` only. |
| D5 | No-template fallback must remain behaviorally unchanged. | Level 2 cannot regress existing adopters. | Always wrap views in a template host even when no template exists. |
| D6 | Version validation is warning-first except for known incompatible contract shape. | Keeps adoption fail-soft while still surfacing drift. | Block all mismatches; silently accept all mismatches. |
| D7 | Hot reload promise is split: Razor template markup is hot reloadable; marker metadata is a rebuild input. | Honest with Blazor/Razor and source-generator boundaries. | Promise in-process hot reload for generator input changes. |
| D8 | The Counter sample remains the only required reference implementation. | Solo-maintainer scope control and continuity with Story 6-1. | Add a new Orders/TaskTracker sample in this story. |
| D9 | Story 6-2 defines the starter-template contract seam but not `FcDevModeOverlay` or clipboard generation. | Story 6-5 owns overlay-driven starter generation. | Pull overlay and `IRazorEmitter` UI into 6-2. |
| D10 | Duplicate templates must be deterministic and visible. | Two templates for one projection would make output order-dependent. | Last writer wins; first writer wins silently. |
| D11 | Templates are scoped to projection views, not commands. | Epic 6.2 only covers projection layout. Command-form template customization stays backlog/future. | Apply Level 2 to command forms in the same story. |
| D12 | Accessibility contracts stay inherited from generated fragments where possible. | Avoids asking adopters to recreate field ARIA labels and descriptions for simple layout changes. | Make templates fully responsible for all accessible names. |
| D13 | Template registry lookup must be cheap and bounded. | Generated views render often; lookup cannot allocate heavily or scan assemblies. | Reflection discovery per render; unbounded dictionary growth. |
| D14 | Contracts stay dependency-light; Shell owns Blazor-specific runtime helpers where needed. | Domain assemblies should not gain avoidable dependency weight. | Put Shell services in Contracts; put marker attributes in Shell. |

### Library / Framework Requirements

- Target the repository's current .NET 10 / Blazor / Fluent UI Blazor / Fluxor / Roslyn SourceTools stack.
- Use Blazor templated component patterns: `RenderFragment` and `RenderFragment<TValue>` with explicit `Context` naming for readability.
- Use Razor generic type support where it improves type safety, but avoid generic type names likely to collide in cascaded type-parameter flows.
- Use `[Parameter]` and `[EditorRequired]` for required template context parameters where the component contract is a Razor component.
- Keep SourceTools incremental. New marker discovery should use Roslyn incremental APIs and stable equality inputs.
- Preserve nullable-reference annotations and trim compatibility.
- Do not add a new third-party templating or UI library.

External references checked on 2026-04-26:

- Microsoft Learn: Blazor templated components: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/templated-components?view=aspnetcore-10.0
- Microsoft Learn: Razor component generic type support: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/generic-type-support?view=aspnetcore-10.0
- Microsoft Learn: ASP.NET Core Hot Reload: https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionTemplateAttribute.cs` | Compile-time marker for Level 2 templates. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateContext.cs` | Typed context passed to Level 2 templates. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateDescriptor.cs` | Immutable descriptor for generated template registrations. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateContractVersion.cs` | Single source of template contract version truth. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` or adjacent template parser | Marker validation and projection linkage. |
| `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` | New HFC diagnostics for template validation/version drift. |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | Public constants for any new IDs used outside SourceTools. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/*Template*Emitter.cs` | Generated template manifest/registration output. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` | Projection view template-selection integration. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs` | Reusable field/section delegate emission as needed. |
| `src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/*` | Typed runtime registry and generated-registration intake. |
| `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` | DI registration for template registry. |
| `samples/Counter/Counter.Web/Components/Templates/*` | Counter Level 2 template reference implementation. |
| `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/*Template*Tests.cs` | Contract/attribute tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/*Template*Tests.cs` | Marker, manifest, diagnostic, and emitter tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/*Template*Tests.cs` | Registry/rendering tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Generated/*Template*Tests.cs` | Counter/sample template-selection tests if generated components are consumed there. |

### Testing Standards

- Keep parser/transform tests deterministic and free of wall-clock or filesystem order assumptions.
- Add no-template fallback tests before adding positive template tests; fallback is the regression rail.
- Test duplicate markers in stable order and assert the diagnostic names both template types.
- Test invalid projection reference, missing required context parameter, generic template misuse, version mismatch, and valid template selection.
- bUnit tests should prove templates get generated field fragments, not hand-rendered duplicate field behavior.
- For accessibility, assert generated fragments retain existing `aria-label`, tooltip/description, placeholder, and badge visible-label behavior in the template path.
- Use targeted tests first; run full build with warnings as errors before closure.

### Scope Guardrails

Do not implement these in Story 6-2:

- Level 3 slot-level field replacement or lambda-based field override registration.
- Level 4 full component replacement.
- `FcDevModeOverlay`, `FcDevModeAnnotation`, starter-template drawer UI, or clipboard copy.
- `IRazorEmitter` production overlay integration.
- Command-form templates.
- Runtime reflection scanning of all assemblies for templates on every startup.
- A new sample domain.
- Manual adopter calls to stringly `IOverrideRegistry.Register` for Level 2.
- Custom Fluent UI wrappers that duplicate existing DataGrid, badge, placeholder, empty-state, or expand-in-row components.
- EventStore, SignalR, ETag cache, command idempotency, or observability changes.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Overlay-driven "Copy starter template" UI and clipboard flow. | Story 6-5 |
| Slot-level replacement for one field inside a template section. | Story 6-3 |
| Full component replacement with lifecycle wrapper preservation. | Story 6-4 |
| User-facing rebuild/restart diagnostic for unsupported hot reload metadata edits. | Story 6-6 |
| Template contract migration docs and cookbook examples across Levels 1-4. | Story 9-5 |
| Visual specimen coverage for adopter-authored templates across themes/densities. | Story 10-2 |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-6-developer-customization-gradient.md#Story-6.2`] - story statement, ACs, FR40, FR43, FR44, UX-DR54.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#Developer-Customization--Override-System`] - FR40 typed Razor templates, FR43 validation, FR44 hot reload, FR45 teaching diagnostics.
- [Source: `_bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md#API-Surface`] - customization gradient examples and solo-maintainer constraints.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#FcDevModeOverlay`] - future overlay/starter path, explicitly out of scope here.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#FcStarterTemplateGenerator`] - Level 2 starter template metadata requirements for Story 6-5.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Source-Generator-as-Infrastructure`] - generator diagnostics, incremental source generation, hot reload limitation, and package graph.
- [Source: `_bmad-output/implementation-artifacts/6-1-level-1-annotation-overrides.md`] - Level 1 format/annotation behavior to preserve inside templates.
- [Source: `_bmad-output/implementation-artifacts/4-4-virtual-scrolling-and-column-prioritization.md`] - DataGrid priority/virtualization contract.
- [Source: `_bmad-output/implementation-artifacts/4-5-expand-in-row-detail-and-progressive-disclosure.md`] - expand-in-row lifecycle and cleanup contract.
- [Source: `_bmad-output/implementation-artifacts/4-6-empty-states-field-descriptions-and-unsupported-types.md`] - field descriptions, empty states, unsupported placeholder discipline.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review and elicitation remain separate hardening passes.
- [Source: `src/Hexalith.FrontComposer.Contracts/Registration/IOverrideRegistry.cs`] - existing placeholder registry not suitable as public Level 2 API.
- [Source: `src/Hexalith.FrontComposer.Contracts/Rendering/IRenderer.cs`] - provisional renderer contract.
- [Source: `src/Hexalith.FrontComposer.Contracts/Rendering/RenderContext.cs`] - render context to flow into templates.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModel.cs`] - transform metadata available to template context/registration.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`] - generated projection view integration point.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs`] - role-specific body emission to reuse.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs`] - generated field/column rendering to preserve.
- [Source: `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`] - Shell DI integration point.
- [Source: `samples/Counter/Counter.Domain/CounterProjection.cs`] - sample projection used for Level 2 reference.
- [Source: Microsoft Learn: Blazor templated components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/templated-components?view=aspnetcore-10.0) - `RenderFragment<TValue>` and template `Context` pattern.
- [Source: Microsoft Learn: Razor generic type support](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/generic-type-support?view=aspnetcore-10.0) - generic Razor component and cascading type-parameter constraints.
- [Source: Microsoft Learn: ASP.NET Core Hot Reload](https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload) - hot reload behavior for Razor component edits.

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
