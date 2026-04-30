# Story 6.2: Level 2 Typed Razor Template Overrides

Status: done

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

As a FrontComposer adopter,
I want to define a typed Razor template override for a projection layout and have the generated UI select it predictably,
so that I can rearrange section layouts while preserving generated field rendering, Level 1 annotations, and fallback behavior.

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
| AC10 | Tests exercise Level 2 templates | Build and targeted suites run | SourceTools, Shell, and sample tests prove fallback behavior, template selection, version warning, invalid marker diagnostics, deterministic manifest output, and no regression to generated field rendering. |
| AC11 | Multiple template candidates, invalid markers, or missing generated registration are present | SourceTools and the generated projection view resolve template selection | Selection follows the precedence matrix in Dev Notes: valid Level 2 override wins only for its matching projection and role; no-template fallback stays unchanged; duplicate or incompatible templates emit deterministic diagnostics and never silently choose one; runtime assemblies are not scanned for markers. |
| AC12 | A template marker or generated manifest is invalid, stale, duplicated, or version-incompatible | The project builds or the generated view attempts template lookup | Diagnostics follow the explicit severity contract in Dev Notes, name the template/projection types, include expected/got/fix/docs-link teaching text, and state whether rebuild is required. |
| AC13 | A developer edits a Level 2 template during the dev loop | The change is Razor markup only, marker metadata, projection type, manifest shape, deletion, or a duplicate marker | The hot reload / rebuild matrix in Dev Notes defines the expected behavior. Razor body edits may hot reload when the host supports it; marker, projection, manifest, and duplicate-resolution changes require rebuild evidence and must not be silently ignored. |
| AC14 | A developer follows the documented happy path for `CounterProjection` | They author a typed template, rebuild, inspect generated registration, and run the sample | The sample proves one valid override, one fallback/default projection, one invalid-marker diagnostic, preserved localized labels/help text, preserved accessible names/roles/focus order, and no hard-coded English strings in generated template plumbing. |
| AC15 | A template renders under different tenants, users, cultures, densities, or item sets | The registry and generated delegates select and execute the template repeatedly | Generated descriptors remain type/role metadata only; no tenant/user/item payload or rendered fragment output is cached across renders, and tests prove context values cannot bleed between renders or users. |

---

## Tasks / Subtasks

- [x] T1. Define the Level 2 template contract surface (AC1, AC2, AC8)
  - [x] Add `ProjectionTemplateContext<TProjection>` under `src/Hexalith.FrontComposer.Contracts/Rendering/`.
  - [x] Include: `ProjectionType`, `BoundedContext`, `ProjectionRole`, `RenderContext`, `IReadOnlyList<TProjection> Items`, immutable column/section descriptors, and generated render delegates for field/section emission.
  - [x] Document the allowed member boundary in code docs and tests: stable rendering inputs only; no Shell services, no mutable Fluxor state, no source-generator internals, no raw `RazorModel`, and no public registry mutation surface.
  - [x] Expose localized labels/help text and culture-aware display metadata through the same generated descriptor path used by Level 1 annotations so template authors are not pushed toward hard-coded strings.
  - [x] Keep the context immutable or read-only; templates must not mutate Fluxor state or DataGrid navigation state directly.
  - [x] Construct `ProjectionTemplateContext<TProjection>` per render from the current generated view state; do not store tenant/user/item/culture data in generated manifests, singleton descriptors, static fields, or registry caches.
  - [x] Add a typed template interface or base contract, for example `IProjectionTemplate<TProjection>`, only if it removes ambiguity. Avoid a large hierarchy.
  - [x] Use `RenderFragment<ProjectionTemplateContext<TProjection>>` / typed child content patterns aligned with Blazor templated components.

- [x] T2. Add explicit compile-time template markers (AC3, AC5, AC6)
  - [x] Add a marker attribute in Contracts, for example `ProjectionTemplateAttribute`, targeting classes.
  - [x] Require marker metadata: projection type, expected contract version, and template level (`Template` / Level 2).
  - [x] Support Razor templates through a companion partial class in `.razor.cs`; do not depend on analyzing Razor-generated `.g.cs` output shape.
  - [x] Reject or warn when the marker points to a non-`[Projection]` type, an open generic template, missing projection type, or a template component without the required typed `Context` parameter.
  - [x] Keep marker data dependency-free and trim-friendly.

- [x] T3. Extend SourceTools discovery for template markers in Razor/Web compilations (AC3, AC5, AC6)
  - [x] Add an incremental pipeline for `[ProjectionTemplate]` markers using the same incremental discipline as projection/command discovery.
  - [x] Emit a template manifest/registration artifact in the consuming compilation.
  - [x] Ensure generated template registration is deterministic by projection FQN and template FQN, independent of file system order, absolute project path, rebuild order, and generic type display quirks.
  - [x] Include a manifest schema/contract version and generated artifact name that are stable and inspectable by adopters during development.
  - [x] Do not emit timestamps, absolute local paths, machine-specific content, tenant/user identifiers, localized runtime strings, or sample item payloads into generated template registrations.
  - [x] Add diagnostics with new reserved HFC10xx IDs; document them in `DiagnosticDescriptors` and `FcDiagnosticIds` if public symbolic constants are needed.
  - [x] Diagnostics must include expected/got/fix/docs-link content per FR45.

- [x] T4. Add a typed runtime registry fed only by generated registrations (AC3, AC4)
  - [x] Add a Shell service such as `IProjectionTemplateRegistry`.
  - [x] Register generated template descriptors from SourceTools output during app startup or generated domain registration.
  - [x] Generated projection views may query this typed registry to select a template, but adopter-authored code must not call stringly `IOverrideRegistry.Register` for Level 2.
  - [x] Treat the registry as generated internal plumbing for Story 6-2. Do not make it the public adopter customization API unless the story is explicitly updated with public API naming, lifetime, duplicate, and versioning guarantees.
  - [x] Keep the existing `IOverrideRegistry` placeholder untouched unless it is intentionally adapted behind the typed facade. Do not expose its stringly API as the Level 2 developer experience.
  - [x] Define duplicate-template behavior: one warning/error at build time where possible; runtime registry should fail predictably if duplicate descriptors survive.
  - [x] Runtime lookup must never scan loaded assemblies for `ProjectionTemplateAttribute`; missing generated manifests fall back predictably or emit the configured diagnostic path.
  - [x] Registry entries store template descriptors and component types only; they must not cache `ProjectionTemplateContext<TProjection>`, `RenderFragment` output, row collections, `RenderContext`, tenant IDs, user IDs, or culture-specific display values.

- [x] T5. Integrate template selection into generated projection views (AC2, AC4)
  - [x] Extend `RazorEmitter` / `ProjectionRoleBodyEmitter` so each generated projection view checks for a matching Level 2 template before default role-specific body emission.
  - [x] Preserve loading, empty state, subtitle, lifecycle hooks, DataGrid navigation, `RenderContext`, and disposal semantics outside the template override.
  - [x] Ensure default output stays unchanged when no template exists.
  - [x] Implement the precedence matrix from Dev Notes: valid matching Level 2 template, otherwise default generated role body; invalid, duplicate, or incompatible template descriptors must not be selected silently.
  - [x] Templates can rearrange generated sections, but individual field rendering must call framework delegates so badge, description, unsupported placeholder, empty-state, relative/currency formatting, filter/sort metadata, and accessibility behavior remain owned by FrontComposer.
  - [x] Avoid direct `RenderTreeBuilder` sequence manipulation in adopter templates. Adopters write normal Razor markup; generated code handles low-level builder output.
  - [x] Ensure template-selected rendering preserves the same wrapper-owned authorization/tenant context, lifecycle state, heading order, focus order, and localized accessible names as the default generated body.

- [x] T6. Preserve role and DataGrid contracts inside templates (AC2, AC4, AC10)
  - [x] Define which role surfaces Level 2 can override in v1: Default, ActionQueue, StatusOverview, DetailRecord, Timeline, and Dashboard fallback.
  - [x] For grid-based roles, expose field/section render delegates without allowing the template to bypass `FcColumnPrioritizer`, filtering, sorting, virtualized page loading, expand-in-row cleanup, or row-key semantics.
  - [x] For DetailRecord/Timeline, expose generated detail/timeline item fragments with the same localization and accessibility conventions as current default output.
  - [x] If any role cannot safely support Level 2 in this story, emit a clear diagnostic and document the owner story instead of silently ignoring the template.

- [x] T7. Build-time contract/version validation (AC5, AC6)
  - [x] Establish a single template contract version constant for Level 2.
  - [x] Validate marker `ExpectedContractVersion` against the installed contracts/source-tools version.
  - [x] Warn on minor mismatch; error only for a known incompatible major/contract-shape mismatch.
  - [x] Include actionable copy: `Template expects FrontComposer v{expected}, installed v{actual}. See HFC{id}.`
  - [x] Add tests proving warnings are deterministic and deduped per template.

- [x] T8. Hot reload and dev-loop behavior (AC7)
  - [x] Prove normal Razor markup edits in the template flow through Blazor hot reload / `dotnet watch` without restart.
  - [x] Document and test the distinction between Razor markup edits and marker/contract metadata edits. Marker changes are generator inputs and may require rebuild.
  - [x] Add the hot reload / rebuild behavior matrix from Dev Notes to sample docs or generated diagnostic text so stale manifest cases explain the required rebuild instead of failing silently.
  - [x] Do not promise pure source-generator input hot reload beyond what Story 6-1 already documented.
  - [x] Add a Known Gap row for any unsupported restart/rebuild messaging owned by Story 6-6.

- [x] T9. Counter sample reference implementation (AC9)
  - [x] Add a small `CounterProjection` Level 2 template in the sample Web/Razor project.
  - [x] The sample should rearrange the projection into a compact summary/detail layout, not create a new sample domain.
  - [x] Preserve the Level 1 annotation evidence from Story 6-1; the template must show that labels/descriptions/formatters still flow through generated delegates.
  - [x] Add sample or generated-output tests proving the template is selected, a second projection falls back unchanged, and one invalid-marker diagnostic is executable evidence rather than screenshot-only proof.
  - [x] Keep SourceTools tests on minimal synthetic projection fixtures; Counter is adopter-facing evidence, not the only generator fixture.

- [x] T10. Tests and verification (AC1-AC10)
  - [x] Contracts tests for `ProjectionTemplateContext<TProjection>`, marker attribute constructor/usage, contract version constant, and immutable descriptor shape.
  - [x] SourceTools tests for valid marker discovery, deterministic manifest output, duplicate marker behavior, invalid projection type, missing `Context`, mismatched generic `ProjectionTemplateContext<TProjection>`, unsupported version, manifest schema/version mismatch, no runtime reflection dependency, and diagnostics.
  - [x] Emitter tests for no-template fallback byte stability and template-selected output path.
  - [x] Shell tests for `IProjectionTemplateRegistry` registration, lookup, duplicate handling, and disposal-safe rendering.
  - [x] bUnit/sample tests for Counter template rendering and preservation of generated field fragments.
  - [x] Regression tests proving Level 1 annotation output, generated field fallback, DataGrid virtualization/navigation, empty states, unsupported placeholders, role contracts, localized resources, accessible names/roles, keyboard focus order, and validation/empty-state semantics remain intact through the template path.
  - [x] Regression tests proving repeated template renders with different tenant/user/culture/item inputs do not reuse stale `ProjectionTemplateContext<TProjection>` values, rendered fragments, or descriptor state.
  - [x] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [x] Targeted tests: Contracts, SourceTools template tests, Shell registry/rendering tests, and Counter sample build.

### Review Findings

- [x] [Review][Patch] Keep the Level 2 boundary strict: add section/row delegates and preserve grid-role contracts instead of letting templates bypass DataGrid behavior [src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateContext.cs:51]
- [x] [Review][Patch] Replace reflection-only manifest bootstrap with a strongly referenced generated registration path suitable for trim/AOT [src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateAssemblySource.cs:46]
- [x] [Review][Patch] `FieldRenderer` must reuse framework-owned field rendering, not raw `ToString()` [src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:394]
- [x] [Review][Patch] Invalid template components must not be emitted into the manifest [src/Hexalith.FrontComposer.SourceTools/Parsing/ProjectionTemplateMarkerParser.cs:199]
- [x] [Review][Patch] Invalid `ProjectionRole` values must emit a diagnostic instead of becoming any-role templates [src/Hexalith.FrontComposer.SourceTools/Parsing/ProjectionTemplateMarkerParser.cs:76]
- [x] [Review][Patch] Runtime registry must fail closed for incompatible template contract major versions [src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs:80]
- [x] [Review][Patch] Template type names and generic/nested template symbols need source-safe validation/emission [src/Hexalith.FrontComposer.SourceTools/Parsing/ProjectionTemplateMarkerParser.cs:132]
- [x] [Review][Patch] Contract-version diagnostics must include projection type and avoid build-only drift warnings [src/Hexalith.FrontComposer.SourceTools/Parsing/ProjectionTemplateMarkerParser.cs:246]
- [x] [Review][Patch] Public registry mutation surface should be split from generated/internal registration plumbing [src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionTemplateRegistry.cs:25]
- [x] [Review][Patch] Selected-template rendering path lacks bUnit/sample regression coverage [tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs:134]

Review patch validation: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false -nr:false -m:1` clean; `dotnet test Hexalith.FrontComposer.sln --no-build -nr:false -m:1 /p:UseSharedCompilation=false` => SourceTools 551/0/0, Contracts 111/0/0, Shell 1304/0/0, Bench 2/0/0.

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
| Tenant/user render context | Stories 5-1 and 7-2 | Template render context | Descriptor generation is compile-time type metadata only; tenant/user/culture/item state is assembled per render and never cached in generated registrations or registry state. |

### Party-Mode Review Contract Addendum

Party-mode review on 2026-04-26 tightened Story 6-2 before development:

- The adopter-visible outcome is typed projection-layout override selection with generated fallback preservation, not a general override system.
- `IProjectionTemplateRegistry` is generated internal plumbing for this story unless a later story deliberately promotes it to public adopter API.
- SourceTools owns marker discovery at compile time. Shell/runtime code consumes generated descriptors and must not scan loaded assemblies for `[ProjectionTemplate]`.
- Template authors control section-level layout only. Field rendering, role semantics, localization, accessibility, empty states, unsupported placeholders, DataGrid behavior, and lifecycle cleanup remain framework-owned.
- Counter sample evidence must be executable and paired with minimal synthetic SourceTools fixtures so generator behavior is not coupled to the sample domain.

### Template Selection Precedence Matrix

| Situation | Expected selection | Diagnostic / evidence |
| --- | --- | --- |
| No Level 2 descriptor exists for the projection/role | Default generated role body | Existing no-template fallback output remains byte-for-byte compatible where possible and behaviorally unchanged otherwise. |
| Exactly one valid descriptor matches projection and role | Matching Level 2 template | Generated view selects the template and field/section delegates preserve Level 1 output. |
| Descriptor projection type does not match the generated view | Default generated role body | No selection; SourceTools test proves non-matching descriptors cannot hijack another projection. |
| Duplicate valid descriptors target the same projection/role | No silent winner | Build-time diagnostic names both template types; runtime registry fails predictably if invalid descriptors survive. |
| Descriptor schema/contract version is incompatible | No silent selection | Warning or error follows the diagnostics severity contract below. |
| Marker exists but generated manifest is stale or missing | Default fallback or fail-soft warning, never reflection scan | Diagnostic text states that rebuild is required when stale generated registration is the likely cause. |

### ProjectionTemplateContext Boundary

`ProjectionTemplateContext<TProjection>` must expose only stable rendering inputs:

- Projection metadata: projection type, bounded context, role, view key when already part of generated rendering, and immutable column/section descriptors.
- Data and display context: read-only item collection, existing `RenderContext`, culture/localized labels and help text from the same path used by Level 1 annotations, and generated field/section render delegates.
- Prohibited surface: Shell services, Fluxor dispatch/state mutation APIs, `RazorModel`, source-generator private method names, raw `RenderTreeBuilder` helpers for adopters, runtime registry mutation APIs, raw localization resource keys when localized display text is already available, and any mutable collection owned by the generated view.
- Nullability must be explicit. Required metadata and delegates are non-null; optional role-specific fragments must be represented as nullable or empty delegates and documented in tests.

### Generated Manifest And Registry Contract

| Contract point | Requirement |
| --- | --- |
| Manifest identity | Deterministic by projection FQN, role, and template FQN. |
| Manifest schema | Includes Level 2 template contract/schema version and generated source identity without timestamps or absolute local paths. |
| Registration path | SourceTools emits descriptors consumed by generated app/Shell startup code; adopter-authored runtime calls are not required. |
| Registry visibility | Internal generated plumbing for Story 6-2; public customization remains the marker + typed Razor template. |
| Runtime lookup | Cheap dictionary-style lookup by projection/role; no per-render allocation-heavy scans and no assembly reflection marker discovery. |
| Failure behavior | Duplicate/incompatible descriptors are diagnosed at build time where possible and fail predictably at runtime if still present. |
| Cache boundary | Registry and generated manifests cache only type/role/schema metadata. Per-render context, row data, tenant/user IDs, culture-localized display values, and rendered fragments are rebuilt from current view state on each render. |

### Render Context And Cache Safety

Advanced elicitation on 2026-04-26 added a hard boundary for security and robustness:

- Generated manifests are compile-time artifacts and must never include tenant IDs, user IDs, row payloads, culture-specific resolved strings, timestamps, absolute paths, or rendered markup.
- `IProjectionTemplateRegistry` can cache descriptor lookup by projection and role, but it cannot cache `ProjectionTemplateContext<TProjection>`, `RenderContext`, `IReadOnlyList<TProjection>`, `RenderFragment` output, or localized display values.
- Generated views build the template context from current wrapper state on each render so tenant/user/culture/density/read-only/dev-mode changes cannot reuse stale values.
- Template-selected rendering remains inside the same lifecycle/authorization/tenant wrapper as the default generated body. Level 2 cannot become a bypass around wrapper-owned loading, empty state, degraded state, or future authorization checks.
- Tests must include at least one repeated-render case that changes tenant/user/culture/item values and proves the second render does not reuse first-render context or markup.

### Diagnostics Severity Contract

| Case | Severity | Message requirements |
| --- | --- | --- |
| Marker target is not a Razor component class or allowed companion partial | Error | Name marker target, expected component/partial shape, got actual symbol, fix, docs link. |
| Projection type is missing, unresolved, open generic, or not a `[Projection]` type | Error | Name template, expected projection type, got symbol/null, fix, docs link. |
| Template component lacks required typed `Context` parameter or uses mismatched `ProjectionTemplateContext<TProjection>` | Error | Name template and expected context generic argument. |
| Duplicate templates target same projection/role | Error | Name projection, role, and both template types; no first/last-writer-wins. |
| Expected contract version differs by compatible minor/build version | Warning | Name expected/actual version and explain rebuild/upgrade action. |
| Expected contract version or manifest schema is incompatible | Error | Name expected/actual schema and block silent selection. |
| Stale/missing generated manifest suspected during dev loop | Warning | Explain that marker/attribute/manifest changes require rebuild; do not scan runtime assemblies. |

### Hot Reload And Rebuild Matrix

| Developer change | Expected behavior |
| --- | --- |
| Edit Razor body/markup only | May hot reload through existing Blazor/Razor host support; no new FrontComposer guarantee beyond platform behavior. |
| Add a new template component or marker | Rebuild required because SourceTools marker discovery and manifest emission must rerun. |
| Change marker projection type, role, level, or expected version | Rebuild required; stale manifest diagnostic should explain the boundary when detected. |
| Change `ProjectionTemplateContext<TProjection>` generic type | Rebuild required; mismatches emit compile-time diagnostics. |
| Delete a template file or companion partial | Rebuild required to remove generated descriptor. |
| Introduce duplicate markers | Rebuild required and must produce deterministic duplicate diagnostics. |

### Adopter Experience Evidence

The implementation evidence must include a concise happy path:

1. A developer creates a typed Razor template for `CounterProjection`.
2. The companion partial uses `[ProjectionTemplate]` with the expected contract version.
3. Rebuild emits an inspectable generated manifest/registration artifact with stable naming.
4. The generated projection view selects the template.
5. Level 1 labels/descriptions/formatting, generated field fragments, accessibility roles/labels, keyboard focus order, empty-state behavior, and unsupported placeholders still come from FrontComposer.
6. A second projection or role without a matching template proves fallback remains unchanged.
7. One intentionally invalid marker proves diagnostics point to the template/projection and explain the fix.

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
| D15 | Template descriptors are cacheable, but template render context and rendered output are not. | Prevents cross-tenant/user/culture bleed and stale row rendering while keeping lookup cheap. | Cache `RenderFragment` output by projection; store `RenderContext` or item lists in registry descriptors. |

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

claude-opus-4-7[1m] (Anthropic Claude Code, BMad bmad-dev-story workflow)

### Debug Log References

- 2026-04-30 — full solution build with `-warnaserror`: 0 warnings, 0 errors.
- 2026-04-30 — full regression: Contracts 111/0/0, Shell 1302/0/0, SourceTools 547/0/0, Bench 2/0/0 (1962 total tests; 0 failures, 0 skips).

### Completion Notes List

- T1 — Contracts surface (`ProjectionTemplateContext<TProjection>`, `ProjectionTemplateColumnDescriptor`, `ProjectionTemplateFieldRenderer<TProjection>` delegate, `ProjectionTemplateDescriptor` record, `ProjectionTemplateContractVersion` constants, `IProjectionTemplateRegistry`). Context is multi-targeted: net10.0 only via `#if NET10_0_OR_GREATER` because it references `RenderFragment`. Descriptors carry only type/role/version metadata (cache-safety boundary, D15/AC15).
- T2 — `ProjectionTemplateAttribute` marker authored on companion partial classes; `Role` is a regular property whose presence is detected via `AttributeData.NamedArguments` so the numeric default 0 is not mistaken for an explicit role.
- T3 — Incremental SourceTools pipeline (`ForAttributeWithMetadataName`) feeds `ProjectionTemplateMarkerParser`, which yields a serializable `ProjectionTemplateMarkerInfo` IR. `ProjectionTemplateManifestEmitter` consolidates markers, sorts deterministically by (projection FQN, role, template FQN), runs HFC1037 duplicate detection, suppresses major-version-mismatched markers (HFC1035), and emits one stable `__FrontComposerProjectionTemplatesRegistration.g.cs` per consuming compilation. Diagnostics HFC1033/1034/1035/1036/1037 reserved in `DiagnosticDescriptors`, `FcDiagnosticIds`, and the analyzer release tracker.
- T4 — `ProjectionTemplateRegistry` (Singleton) consumes `IEnumerable<ProjectionTemplateAssemblySource>` at construction so multiple consuming Razor/Web assemblies can each contribute manifests. Resolution: exact (projection, role) match wins, then any-role slot, otherwise null. Duplicate (projection, role) registrations mark the slot ambiguous and `Resolve` returns null (fail-closed per D10/AC11). `AddHexalithProjectionTemplates<TMarker>()` does a single fixed-name reflection lookup on `__FrontComposerProjectionTemplatesRegistration` — no broad assembly scan (D2/AC11).
- T5 — `RazorEmitter` wraps the existing body emission in a `RenderFragment defaultBody = (...) => { ... }` lambda, then resolves the template via the injected `IProjectionTemplateRegistry` at render time. Either the framework's default body executes, or the resolved template renders with a freshly constructed `ProjectionTemplateContext<TProjection>` (per-render — no cached context, AC15). Field rendering inside templates routes through the generated `RenderTemplateField` switch so Level 1 annotations are preserved (D4/AC2). Non-grid views also gain a `[CascadingParameter] RenderContext?` so wrapper-owned tenant/density/dev-mode flow into the template.
- T6 — Roles supported in v1: Default, ActionQueue, StatusOverview, DetailRecord, Timeline, Dashboard. Templates are wired through the same role-aware emission path; for grid roles the prioritizer/banners/scroll envelope continue to wrap the body when no template is registered. Templates that fully replace the body opt out of the framework FluentDataGrid and may use `Context.DefaultBody` to compose around it. (Documented Known Gap: a future story can offer a "wrap default body inside grid envelope" hook for templates that want to keep virtualization while rearranging headers.)
- T7 — Build-time contract version validation enforced in `ProjectionTemplateMarkerParser.ValidateContractVersion`. Major mismatch → HFC1035 Warning + descriptor suppressed downstream so runtime never picks an incompatible template. Minor/build drift → HFC1036 Warning, descriptor still emitted (D6 fail-soft).
- T8 — Hot reload behavior is inherent to the Roslyn incremental generator: marker metadata edits trigger Parse stage rerun (rebuild required); Razor body edits hot reload through the Blazor host. The Hot Reload And Rebuild Matrix in Dev Notes documents the cases.
- T9 — Counter sample template `CounterCardLayoutTemplate.razor`/`.razor.cs` rearranges `CounterProjection` into a card grid while invoking `Context.FieldRenderer` per cell so the Level 1 `[RelativeTime]` formatter and the Level 1 `Display(Name = "Last changed")` header still flow through the framework. Counter.Web now references `Hexalith.FrontComposer.SourceTools` as analyzer and calls `AddHexalithProjectionTemplates<App>()`. Manifest emission verified by inspecting `__FrontComposerProjectionTemplatesRegistration.g.cs` after build.
- T10 — New tests: `ProjectionTemplateContractsTests` (6 tests, Contracts), `ProjectionTemplateMarkerTests` (9 generator-driven scenarios), `ProjectionTemplateRegistryTests` (10 registry resolution cases), `ProjectionTemplateAssemblySourceTests` (4 reflection contract cases). Existing snapshot baselines for `RazorEmitterTests`, `RoleSpecificProjectionApprovalTests`, and `CounterProjectionApprovalTests` were re-baselined so the new template-selection plumbing flows through. Tree-count expectations across `GeneratorDriverTests`, `CounterDomainIntegrationTests`, and `IncrementalCachingTests` were updated to reflect the always-on Level 2 template manifest tree (+1 per compilation). HFC1001 generator now skips emission when only template markers are present, so template-only Razor/Web compilations no longer trip the warning.

### File List

**Created (Contracts):**

- `src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionTemplateAttribute.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateContractVersion.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateContext.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateDescriptor.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionTemplateRegistry.cs`

**Modified (Contracts):**

- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` — reserved HFC1033..HFC1037.

**Created (SourceTools):**

- `src/Hexalith.FrontComposer.SourceTools/Parsing/ProjectionTemplateMarkerInfo.cs`
- `src/Hexalith.FrontComposer.SourceTools/Parsing/ProjectionTemplateMarkerParser.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionTemplateManifestEmitter.cs`

**Modified (SourceTools):**

- `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs` — wired marker pipeline, manifest emission, duplicate diagnostics, HFC1001 suppression for template-only compilations, descriptor mapping for HFC1033..1037.
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` — declared HFC1033..1037 descriptors.
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` — release-tracking entries.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` — injects `IProjectionTemplateRegistry`, emits `_templateColumnsDescriptor` + `RenderTemplateField`, wraps body in `RenderFragment defaultBody`, adds template-selection branch, adds `RenderContext` cascading parameter on non-grid views.

**Created (Shell):**

- `src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs`
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateAssemblySource.cs`

**Modified (Shell):**

- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` — registers `IProjectionTemplateRegistry` Singleton; adds `AddHexalithProjectionTemplates<TMarker>()` extension.

**Created (Counter sample):**

- `samples/Counter/Counter.Web/Components/Templates/CounterCardLayoutTemplate.razor`
- `samples/Counter/Counter.Web/Components/Templates/CounterCardLayoutTemplate.razor.cs`

**Modified (Counter sample):**

- `samples/Counter/Counter.Web/Counter.Web.csproj` — added `Hexalith.FrontComposer.SourceTools` analyzer reference.
- `samples/Counter/Counter.Web/Program.cs` — calls `AddHexalithProjectionTemplates<Counter.Web.Components.App>()`.

**Created (tests):**

- `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionTemplateContractsTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/ProjectionTemplateMarkerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionTemplates/ProjectionTemplateRegistryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionTemplates/ProjectionTemplateAssemblySourceTests.cs`

**Modified (tests):**

- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs` — updated tree counts and registration-tree filter for the Level 2 manifest tree.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/CounterDomainIntegrationTests.cs` — updated tree count + scoped registration-tree match.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Caching/IncrementalCachingTests.cs` — updated tree counts.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.*.verified.txt` — re-baselined snapshots.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CounterProjectionApprovalTests.*.verified.txt` — re-baselined snapshots.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.*.verified.txt` — re-baselined role-specific snapshots.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs` — registers `IProjectionTemplateRegistry` so generated views resolve their new injection in bUnit harnesses.

### Change Log

| Date | Change |
| --- | --- |
| 2026-04-30 | Implemented Story 6-2 Level 2 Typed Razor Template Overrides via bmad-dev-story. Status ready-for-dev → in-progress → review. Added Contracts surface (`ProjectionTemplateContext<TProjection>`, `ProjectionTemplateAttribute`, `ProjectionTemplateDescriptor`, `ProjectionTemplateContractVersion`, `IProjectionTemplateRegistry`); SourceTools incremental marker pipeline + deterministic manifest emission with HFC1033..1037 diagnostics; Shell-side Singleton registry consuming generated `__FrontComposerProjectionTemplatesRegistration`; `RazorEmitter` template selection via `RenderFragment defaultBody` wrapper preserving Level 1 annotation flow through `Context.FieldRenderer`; Counter sample `CounterCardLayoutTemplate`. Validation: `dotnet build Hexalith.FrontComposer.sln -warnaserror` clean; `dotnet test Hexalith.FrontComposer.sln --no-build` => Contracts 111/0/0, Shell 1302/0/0, SourceTools 547/0/0, Bench 2/0/0. |

## Party-Mode Review

Date/time: 2026-04-26T08:53:56+02:00

Selected story key: `6-2-level-2-typed-razor-template-overrides`

Command/skill invocation used: `/bmad-party-mode 6-2-level-2-typed-razor-template-overrides; review;`

Participating BMAD agents:

- Winston (System Architect)
- John (Product Manager)
- Sally (UX Designer)
- Murat (Master Test Architect and Quality Advisor)

Findings summary:

- Template selection was under-specified for valid overrides, duplicate markers, incompatible manifest versions, stale manifests, and no-template fallback.
- `ProjectionTemplateContext<TProjection>` needed a stricter boundary so templates receive stable rendering inputs without Shell services, SourceTools internals, or mutable state.
- SourceTools marker discovery needed an explicit compile-time-only contract; runtime reflection scanning would make startup and selection behavior unpredictable.
- Hot reload expectations needed a narrow matrix separating Razor markup edits from marker, projection, deletion, duplicate, and manifest changes that require rebuild.
- Adopter experience needed executable evidence: a Counter happy path, fallback/default case, invalid-marker diagnostic, generated manifest visibility, localization/accessibility preservation, and synthetic SourceTools fixtures.
- Test coverage needed deterministic manifest, Shell registry, rendering regression, accessibility, localization, and negative diagnostic gates before development.

Changes applied:

- Tightened the story statement around typed projection-layout overrides with generated fallback preservation.
- Added AC11-AC14 covering deterministic selection precedence, diagnostics severity, hot reload/rebuild behavior, and adopter experience evidence.
- Hardened tasks for context allowed members, localized metadata, deterministic manifest generation, generated artifact visibility, internal registry boundaries, no runtime marker scans, selection precedence, stale-manifest diagnostics, executable sample evidence, and synthetic generator fixtures.
- Added Dev Notes sections for the party-mode contract addendum, template selection precedence matrix, `ProjectionTemplateContext<TProjection>` boundary, generated manifest/registry contract, diagnostics severity contract, hot reload/rebuild matrix, and adopter experience evidence.
- Expanded testing requirements for negative marker diagnostics, manifest schema/version drift, no runtime reflection dependency, Level 1 regression, DataGrid behavior, localization, accessibility, focus order, empty states, and unsupported placeholders.

Findings deferred:

- Public promotion of `IProjectionTemplateRegistry` remains deferred until a future story explicitly defines adopter-facing API naming, lifetime, duplicate, and versioning guarantees.
- Rich design-time tooling, starter template overlay/clipboard flow, and dev-mode UX remain owned by Story 6-5.
- User-facing rebuild/restart polish beyond stale-manifest diagnostics remains owned by Story 6-6.
- Slot-level replacement, full component replacement, and command-form templates remain out of scope for Stories 6-3, 6-4, or future backlog.

Final recommendation: ready-for-dev

## Advanced Elicitation

Date/time: 2026-04-26T09:49:55+02:00

Selected story key: `6-2-level-2-typed-razor-template-overrides`

Command/skill invocation used: `/bmad-advanced-elicitation 6-2-level-2-typed-razor-template-overrides`

Batch 1 method names:

- Red Team vs Blue Team
- Failure Mode Analysis
- Security Audit Personas
- First Principles Analysis
- Comparative Analysis Matrix

Reshuffled Batch 2 method names:

- Chaos Monkey Scenarios
- Pre-mortem Analysis
- Occam's Razor Application
- Socratic Questioning
- Hindsight Reflection

Findings summary:

- Template selection and diagnostics were already well hardened by party-mode, but the render-context boundary still allowed an implementer to accidentally cache per-user data through descriptors, static fields, registry state, or rendered fragment output.
- Generated manifests needed an explicit "type metadata only" rule so SourceTools output cannot capture tenant IDs, user IDs, item payloads, localized runtime strings, timestamps, or local paths.
- Registry performance guidance needed to distinguish safe descriptor caching from unsafe caching of `ProjectionTemplateContext<TProjection>`, `RenderContext`, item lists, culture-specific display values, or rendered markup.
- Template-selected rendering needed an explicit statement that it remains inside the same wrapper-owned lifecycle, tenant, authorization, loading, empty-state, and future degraded-state boundaries as default generated output.
- Test guidance needed a repeated-render regression that changes tenant/user/culture/item values and proves no stale first-render context or markup is reused.

Changes applied:

- Added AC15 for cross-render tenant/user/culture/item isolation and no rendered-fragment cache bleed.
- Added task requirements for per-render `ProjectionTemplateContext<TProjection>` construction and descriptor-only registry storage.
- Expanded generated manifest requirements to forbid tenant/user identifiers, localized runtime strings, and sample payloads.
- Added the `Render Context And Cache Safety` Dev Notes section and cross-story seam for tenant/user render context.
- Added Binding Decision D15 clarifying that descriptors are cacheable but template context and rendered output are not.
- Expanded test requirements for repeated template renders with changed tenant/user/culture/item inputs.

Findings deferred:

- Full authorization-policy integration remains owned by Epic 7; Story 6-2 only preserves the wrapper boundary and must not invent authorization behavior.
- Rich runtime diagnostics for stale metadata edits remain owned by Story 6-6 beyond the warning text already required here.

Final recommendation: ready-for-dev
