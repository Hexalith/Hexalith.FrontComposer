# Story 6.3: Level 3 Slot-Level Field Replacement

Status: review

> **Epic 6** - Developer Customization Gradient. **FR41 / FR43 / FR44 / FR45 / UX-DR54** slot-level customization for one projection field while the rest of the generated view remains framework-owned. Applies lessons **L01**, **L06**, **L07**, **L08**, **L10**, **L11**, and **L15**.

---

## Executive Summary

Story 6-3 makes Level 3 of the customization gradient usable without turning one field override into a full view rewrite:

- Add a refactor-safe public registration API such as `registry.AddSlotOverride<OrderProjection, Priority>(o => o.Priority, typeof(CustomPriorityRenderer))`.
- Introduce a typed `FieldSlotContext<TProjection,TField>` carrying the field value, generated field metadata, parent projection item, current `RenderContext`, role, density, read-only state, and framework render fallback.
- Resolve slot overrides at runtime through a typed registry keyed by projection type, role, and field identity. The key must come from the expression tree and generated metadata, not from string literals supplied by adopters.
- Integrate slot lookup at generated field-render boundaries only. All non-overridden fields continue using FrontComposer's existing auto-generation.
- Preserve Level 1 annotations and Level 2 templates. A slot override can be used inside default generated layouts and inside Level 2 generated field delegates.
- Keep lifecycle wrapper, tenant/user render context, shell density/theme, localization, unsupported placeholder rules, DataGrid sort/filter metadata, expand-in-row cleanup, and accessibility expectations intact.
- Demonstrate one Counter sample slot override that replaces a single field renderer and proves the adjacent fields remain generated.
- Leave full component replacement, dev-mode overlay UI, starter-template clipboard flow, and runtime error-boundary polish to later stories.

---

## Story

As a FrontComposer adopter,
I want to replace one generated projection field with a custom component while all other fields stay auto-generated,
so that I can fix one domain-specific rendering problem without owning the whole projection view.

### Adopter Job To Preserve

A backend-focused developer should be able to customize exactly one problematic field, keep compile-time safety on field identity, and still inherit the shell's navigation, lifecycle, accessibility, density, localization, and generated rendering conventions everywhere else.

The feature promise is deliberately narrow: replace a single generated field renderer for a known descriptor slot without taking ownership of the surrounding component, layout, lifecycle, navigation, authorization, localization, density, accessibility, or generated data conventions.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A developer registers a slot override with `AddSlotOverride<TProjection,TField>(Expression<Func<TProjection,TField>> field, Type componentType)` | The project compiles | The field selector is refactor-safe: renaming the property breaks compilation or analyzer validation instead of silently selecting the wrong field. |
| AC2 | The selector is not a direct property access, targets a nested member, calls a method, captures a variable, indexes a collection, or uses a computed expression | The override registration is validated | The framework emits a deterministic diagnostic naming the invalid selector, expected direct property expression, fix guidance, and docs link; the invalid override is not registered. |
| AC3 | A valid slot override exists for one projection field | The generated view renders | The custom component renders for that field only; all other fields render through the existing generated FrontComposer path. |
| AC4 | A custom slot component renders | It receives `FieldSlotContext<TProjection,TField>` | The context includes `Value`, `Parent`, `Field`, `RenderContext`, `ProjectionRole`, `DensityLevel`, `IsReadOnly`, `IsDevMode`, localization-safe labels/help text, and a `RenderDefault` fallback delegate. |
| AC5 | The slot component type is incompatible with the selected field context, lacks the required context parameter, is open generic without closed type information, or is not a Razor component | The override is validated or resolved | A deterministic HFC diagnostic follows the expected/got/fix/docs-link teaching shape; the generated field falls back to default rendering. |
| AC6 | A Level 3 override is registered | The app starts | Registration is runtime configuration, but the registry stores typed descriptors only. It must not register one renderer per domain type in DI or scan assemblies for slot attributes at render time. |
| AC7 | Generated DataGrid, DetailRecord, Timeline, ActionQueue, StatusOverview, Dashboard, and Level 2 template field delegates render an overridden field | The field is encountered | Each supported role uses the same slot lookup contract; unsupported roles produce an explicit diagnostic or documented fallback instead of silently ignoring the override. |
| AC8 | A slot override renders inside a DataGrid column | Sorting, filtering, virtualization, row keys, column priority, badge counts, empty states, unsupported placeholders, and expand-in-row cleanup execute | The slot changes cell content only; grid behavior still uses generated metadata and raw projection values. |
| AC9 | A Level 1 annotation and a Level 3 slot both apply to a field | The slot context is built | The context exposes the generated label, description, priority, badge/format hints, unsupported-type metadata, and localization-aware help text so the custom component can preserve framework semantics. |
| AC10 | A Level 2 template uses generated field delegates | One delegated field has a Level 3 override | The template still calls the field delegate, and the delegate resolves the slot override before falling back to generated default rendering. |
| AC11 | A slot override component throws during render | The generated view renders | Field renderer exceptions must not corrupt slot registry state or prevent generated fallback on the next render. Rich error-boundary UI, diagnostic surfacing, and recovery polish remain Story 6-6 ownership. |
| AC12 | The developer edits slot registration or slot component code during development | The host rebuilds or supports Blazor hot reload | CI must verify rebuild/startup behavior for adding, changing, and removing slot registration metadata. Local hot-reload smoke evidence for Razor body edits is supporting evidence only and must not become a flaky CI gate. |
| AC13 | Duplicate slot overrides are registered for the same projection, role, and field | The registry validates descriptors | Duplicate behavior is deterministic: either last registration is rejected with a diagnostic, or build/startup fails with an explicit HFC error. Silent last-writer-wins is forbidden. |
| AC14 | The same projection is rendered for different tenants, users, cultures, densities, and read-only states | Slot contexts are created repeatedly | The registry caches descriptors only; it must never cache `FieldSlotContext`, parent item values, rendered fragments, tenant/user data, culture-specific text, or default-render output across renders. Tenant, user, culture, request, density, and read-only state must not influence slot registration identity or component selection. |
| AC15 | The Counter sample is used as reference evidence | The sample renders with a slot override | One field uses a custom slot component, adjacent fields remain generated, Level 1 labels/formatting still flow into context, and targeted tests prove default fallback plus invalid-registration diagnostics. |
| AC16 | The story is evaluated as Level 3 in the customization gradient | A developer compares it to Levels 1, 2, and 4 | Level 3 is field-level replacement only. It does not add full component replacement, command-form customization, dev-mode overlay UI, starter-template generation, or public stringly override APIs. |

---

## Tasks / Subtasks

- [x] T1. Define the public Level 3 registration API (AC1, AC2, AC6, AC13)
  - [x] Add typed extension methods over the existing registry surface, for example `AddSlotOverride<TProjection,TField>(Expression<Func<TProjection,TField>> field, Type componentType, ProjectionRole? role = null)`.
  - [x] Accept only direct member access on the projection parameter, including nullable value fields. Reject nested members, method calls, conversions that hide invalid expressions, captured variables, indexers, and computed expressions.
  - [x] Normalize the field identity from Roslyn/generated metadata and expression-tree member info, not adopter strings.
  - [x] Define duplicate behavior as reject-with-diagnostic. Do not allow silent last-writer-wins.
  - [x] Keep the public API discoverable from IntelliSense and dependency-light for adopters.
  - [x] Avoid adding DI registrations per projection/field/component combination.

- [x] T2. Introduce typed slot contracts (AC4, AC5, AC9, AC14)
  - [x] Add `FieldSlotContext<TProjection,TField>` under `src/Hexalith.FrontComposer.Contracts/Rendering/`.
  - [x] Include:
    - `TField? Value`
    - `TProjection Parent`
    - `FieldDescriptor Field`
    - `RenderContext RenderContext`
    - `ProjectionRole? ProjectionRole`
    - `DensityLevel DensityLevel`
    - `bool IsReadOnly`
    - `bool IsDevMode`
    - `RenderFragment<FieldSlotContext<TProjection,TField>>? RenderDefault` or an equivalent default-render delegate.
  - [x] Keep the context immutable/read-only after construction.
  - [x] Define `RenderDefault` as a generated-default delegate that bypasses Level 3 lookup for the same `(projection, role, field)` so a slot cannot recursively render itself.
  - [x] Add a minimal component contract if needed, for example an interface or marker that requires a `Context` parameter. Do not create a broad renderer hierarchy.
  - [x] Ensure context values are constructed per render and are never stored in the registry.
  - [x] Provide XML docs warning slot authors not to mutate parent projection objects or cache render context across tenants/users.

- [x] T3. Add slot descriptor and registry implementation (AC3, AC5, AC6, AC13, AC14)
  - [x] Add immutable slot descriptors storing projection type, field name, field CLR type, optional role, component type, expected contract version, and diagnostic state.
  - [x] Add a typed registry facade, for example `IProjectionSlotRegistry`, backed by generated/runtime registration descriptors.
  - [x] If the existing `IOverrideRegistry` is reused internally, hide string keys behind typed extension methods and typed descriptor validation.
  - [x] Runtime lookup should be O(1) or bounded dictionary lookup by `(projectionType, role, fieldName)` with fallback to role-agnostic registration.
  - [x] Registry entries must be descriptor-only. Do not cache parent item, context, service provider scoped values, or rendered markup.
  - [x] Freeze descriptor collections before render-time use; concurrent renders must observe immutable snapshots and must not mutate registry dictionaries.
  - [x] Validate duplicate descriptors deterministically and name both registrations/components in diagnostics where possible.
  - [x] Do not scan loaded assemblies for slot components at render time.

- [x] T4. Validate component compatibility (AC5, AC11)
  - [x] Validate that the component type is a Blazor component with a required `Context` parameter compatible with `FieldSlotContext<TProjection,TField>`.
  - [x] Reject open generics unless the registry can close them deterministically from `TProjection` and `TField`.
  - [x] Reject mismatched `TField` types, including nullable/non-nullable mismatches that would produce invalid casts.
  - [x] Use deterministic HFC diagnostics with the standard message shape: What, Expected, Got, Fix, DocsLink.
  - [x] Define fallback behavior for invalid components: no slot selected, default generated field rendering used.
  - [x] Keep render exception isolation minimal and explicit in Story 6-3; defer rich diagnostic panels and cross-level error boundaries to Story 6-6.

- [x] T5. Integrate slot lookup into generated field emission (AC3, AC7, AC8, AC9)
  - [x] Add a slot-resolution helper emitted by `RazorEmitter` or shared Shell code that receives the parent item, field metadata, current render context, role, and default-render fragment.
  - [x] Integrate at the field boundary in `ColumnEmitter`, `ProjectionRoleBodyEmitter`, and any shared field/section delegates used by Level 2 templates.
  - [x] Preserve existing `PropertyColumn` sort/filter expressions or generated metadata so a custom cell renderer does not change DataGrid behavior.
  - [x] For DataGrid cells, render the slot through a `TemplateColumn` or equivalent only when a slot is actually present; no-slot output should remain as close to existing output as possible.
  - [x] Preserve deterministic slot host keys for virtualized rows, using generated row identity plus field identity where the host needs `@key`; never key by localized label text or rendered value.
  - [x] For non-grid roles, wrap exactly the generated field content, not the entire card/row/timeline item.
  - [x] Preserve unsupported placeholder behavior: a slot can replace the field renderer, but invalid/missing slot must still show `FcFieldPlaceholder` for unsupported types.
  - [x] Preserve badge, relative-time, currency, label, description, priority, grouping, empty-state, and accessibility metadata in default rendering and context.

- [x] T6. Preserve Level 2 template composition (AC10)
  - [x] Ensure Level 2 field delegates call the same field-render helper used by default generated layouts.
  - [x] A template author should not need separate slot lookup code.
  - [x] Template field delegates should pass the same `FieldSlotContext` metadata that default views pass.
  - [x] Template default delegates used by `RenderDefault` must bypass the active slot descriptor for the same field to prevent Level 2 + Level 3 recursion.
  - [x] Add tests proving a Level 2 template still renders a Level 3 slot for one field and default generated delegates for adjacent fields.

- [x] T7. Role and precedence matrix (AC7, AC13, AC16)
  - [x] Define slot matching precedence:
    1. Valid role-specific slot for `(projection, role, field)`.
    2. Valid role-agnostic slot for `(projection, field)`.
    3. Generated default field renderer.
  - [x] Duplicate exact matches are diagnostics/errors, not precedence.
  - [x] Invalid component descriptors are ignored after diagnostics and do not block fallback.
  - [x] Role-specific unsupported behavior must be explicit. If Timeline or Dashboard cannot safely host a slot in this story, record the limitation with a named owner instead of silently ignoring the slot.
  - [x] Keep Level 4 full replacement out of precedence. Level 4 will wrap or bypass field-level logic in Story 6-4.

- [x] T8. Hot reload and dev-loop evidence (AC12, AC15)
  - [x] Prove Razor markup edits in the custom slot component refresh under `dotnet watch` where Blazor hot reload supports the edit.
  - [x] Document that registration expression changes, component type changes, generic context changes, and contract version changes are rebuild-triggering metadata changes.
  - [x] Add a small generated or sample note that points unsupported hot-reload cases to Story 6-6 rebuild/restart diagnostics.
  - [x] Do not promise source-generator input hot reload beyond the repo's architecture constraint.

- [x] T9. Counter sample reference implementation (AC15)
  - [x] Add one custom slot component in the Counter sample, recommended for a single display field that benefits from a richer visual treatment.
  - [x] Register the slot through the typed lambda API.
  - [x] Keep the sample small: one slot override, one default adjacent field, and one invalid-registration test fixture.
  - [x] Preserve Level 1 annotation evidence from Story 6-1 and Level 2 template compatibility from Story 6-2 where available.
  - [x] Do not add a new Orders sample solely for this story.

- [x] T10. Diagnostics, docs links, and contract versioning (AC2, AC5, AC13)
  - [x] Reserve stable HFC10xx SourceTools/build-time diagnostics for invalid selectors, incompatible component context, duplicate slots, and version drift.
  - [x] Reserve Shell HFC20xx runtime diagnostics only if startup/runtime descriptor validation owns an error.
  - [x] Include contract version metadata so Story 6-6 can enforce override compatibility consistently across Levels 2-4.
  - [x] Diagnostics must include enough context for self-service: projection type, field name when resolvable, component type, expected context, actual context, fix, and docs link.

- [x] T11. Tests and verification (AC1-AC16)
  - [x] Contracts tests for `FieldSlotContext<TProjection,TField>`, descriptor immutability, and registration extension shape.
  - [x] Expression validation tests for direct property, inherited property, interface-implemented property, shadowed-property disambiguation, nullable property, nested member rejection, method-call rejection, captured-variable rejection, indexer rejection, and conversion handling.
  - [x] Registry tests for role-specific precedence, role-agnostic fallback, duplicate rejection, invalid descriptor fallback, descriptor-only caching, immutable snapshot reads under concurrent render, and no render-context cache bleed.
  - [x] SourceTools/emitter tests proving default output remains unchanged when no slots exist and slot output is used only for the overridden field.
  - [x] DataGrid tests proving sort/filter/virtualization/row key/priority metadata remain generated when a slot renders cell content.
  - [x] Level 2 integration tests proving template field delegates honor Level 3 slots.
  - [x] bUnit tests for custom slot component context values, default fallback delegate without slot recursion, deterministic slot host keys under row reuse, accessibility label preservation, and render exception isolation.
  - [x] Counter sample build/render tests for one valid slot and one invalid-registration diagnostic.
  - [x] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [x] Targeted tests: Contracts, SourceTools slot parser/emitter tests, Shell registry/rendering tests, and Counter sample tests.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Registration/IOverrideRegistry.cs` | Placeholder string-based registry with `Register(string projectionType, string overrideType, Type implementationType)` and `Resolve`. | Do not expose this stringly API as the Level 3 adopter path. Hide it behind typed extensions or a typed registry facade. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/FieldDescriptor.cs` | Immutable field metadata with name, type, display name, format, order, read-only flag, and hints. | Reuse and extend only if needed. Slot context must carry generated metadata rather than making components rediscover labels. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/RenderContext.cs` | Immutable render context with tenant, user, render mode, density, read-only, and non-positional `IsDevMode`. | Flow this context into every slot render; do not cache it in registry descriptors. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/IRenderer.cs` | Provisional renderer abstraction. | Do not redesign the renderer stack for Level 3. Add the smallest slot-specific contract needed. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` | Unsupported-field diagnostic already points developers toward Story 6-3 slot replacement for custom rendering. | Keep HFC1002 fail-soft behavior and make slot replacement the escape path, not a reason to drop unsupported fields. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/ColumnModel.cs` | Carries generated field metadata for headers, type category, format hints, nullable state, badge mappings, priority, field group, description, and unsupported type. | Slot integration must preserve this metadata for default rendering and context. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs` | Emits PropertyColumn/TemplateColumn fragments for generated DataGrid fields, including badge and unsupported placeholder behavior. | Integrate slots at the cell content boundary without breaking sort/filter expressions or column descriptors. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs` | Owns role-specific Default, ActionQueue, StatusOverview, DetailRecord, Timeline, and Dashboard body emission. | Reuse role emission helpers. Do not fork separate slot-aware role renderers. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` | Emits generated component scaffolding, render context cascading parameter, density handling, lifecycle hooks, page loading, and strategy dispatch. | Add slot services/helpers here only where they are shared by generated field boundaries. |
| `tests/Hexalith.FrontComposer.Shell.Tests/FrontComposerTestBase.cs` | Already registers a fake `IOverrideRegistry`. | Update test base to include typed slot registry/facade without breaking existing tests. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 6-3 decision |
| --- | --- | --- | --- |
| Level 1 field metadata | Story 6-1 | Slot context and default renderer | Expose generated labels, descriptions, format hints, priority, and hints through `FieldSlotContext`. |
| Level 2 field delegates | Story 6-2 | Slot rendering | Delegates call the same field-render helper so templates automatically honor slots. |
| Unsupported placeholder path | Story 4-6 | Slot escape hatch | Unsupported fields remain visible by default; valid slots may replace the placeholder for that field only. |
| DataGrid behavior | Stories 4-3 to 4-5 | Slot DataGrid cells | Slot content cannot own sort/filter/virtualization/row-key behavior. Generated metadata remains authoritative. |
| Render context | Contracts/Shell | Slot components | Tenant/user/density/read-only/dev-mode flow per render; descriptors never cache render context. |
| Runtime diagnostics and error boundaries | Story 6-6 | Slot failures | 6-3 defines minimal fallback/isolation; 6-6 owns rich diagnostic panels and analyzer enforcement. |
| Dev-mode overlay/starter generation | Story 6-5 | Slot discovery | 6-3 exposes enough contract metadata for starter generation but does not implement overlay UI. |
| Full replacement | Story 6-4 | Override precedence | Level 4 is out of scope and must not be simulated by broad field slots. |

### Slot Contract Shape

Recommended API shape:

```csharp
services.AddFrontComposerOverrides(registry =>
{
    registry.AddSlotOverride<CounterProjection, int>(
        projection => projection.Count,
        typeof(CounterCountSlot));
});
```

Recommended component shape:

```razor
@using Hexalith.FrontComposer.Contracts.Rendering

<FluentBadge Appearance="Appearance.Accent">
    @Context.Value
</FluentBadge>

@code {
    [Parameter, EditorRequired]
    public FieldSlotContext<CounterProjection, int> Context { get; set; } = default!;
}
```

The exact names may change during implementation, but the invariants do not:

- field identity is typed and expression-based;
- component context is typed;
- default rendering remains callable;
- registry descriptors are cacheable, contexts/rendered fragments are not;
- invalid slots fail soft to generated rendering unless the failure is a duplicate/ambiguous registration that must be fixed.

### Contract Ownership Boundary

Contracts owns `FieldSlotContext<TProjection,TField>`, immutable descriptor/version shapes, and typed registration extension signatures. Shell owns descriptor storage, duplicate validation, component compatibility checks, slot lookup, render host behavior, diagnostics routing, and fallback to generated rendering. SourceTools owns generated field-boundary calls and no-slot baseline preservation. Counter sample code must consume only public APIs and must not introduce sample-specific seams back into the core contract.

### Component Compatibility Contract

`componentType` must be a Razor component assignable to the repository's component contract and must expose the agreed `Context` parameter compatible with `FieldSlotContext<TProjection,TField>`. Validation must happen before render use when the descriptor is registered or resolved; incompatible component types, missing context parameters, open generics that cannot be closed deterministically from `TProjection` and `TField`, and nullable/non-nullable field mismatches emit deterministic diagnostics and fall back to generated rendering. Duplicate exact valid registrations remain deterministic errors.

### Adopter Proof

The Counter sample is an integration proof, not the design driver. It must replace exactly one generated field renderer and demonstrate that sibling fields, generated validation/navigation flow where present, localization labels, density, accessibility attributes, role behavior, and default fallback remain generated-owned.

### Selector Validation Matrix

Selector validation is part of the public contract, not an emitter convenience. The `field` expression must reduce to direct member access on the `TProjection` parameter, for example `x => x.Priority`. Conversions may be stripped only when the remaining node is still a direct projection member known to generated projection metadata. Nested paths, conversions that hide invalid expressions, method calls, indexers, computed expressions, captured values, and member access on anything other than the projection parameter are invalid and must produce deterministic diagnostics.

| Selector | Expected behavior |
| --- | --- |
| `x => x.Priority` | Valid direct property selector. |
| `x => x.NullableScore` | Valid direct nullable property selector. |
| `x => (object)x.Priority` | Valid only if implementation strips compiler conversion and still resolves direct property metadata. |
| `x => x.Customer.Name` | Invalid nested member; diagnostic with fix to target a projection-level field. |
| `x => x.Priority.ToString()` | Invalid method call; diagnostic. |
| `x => x.Items[0]` | Invalid indexer; diagnostic. |
| `x => localProjection.Priority` | Invalid captured variable; diagnostic. |
| `x => x.Priority + 1` | Invalid computed expression; diagnostic. |
| `x => x.GetPriority()` | Invalid method call; diagnostic. |

### Slot Matching Precedence

| Situation | Result |
| --- | --- |
| Valid role-specific slot for `(projection, role, field)` | Use role-specific slot. |
| No role-specific slot, valid role-agnostic slot for `(projection, field)` | Use role-agnostic slot. |
| Both role-specific and role-agnostic slots exist for the same projection field | Role-specific wins for that role; both registrations are allowed because their keys differ. |
| No valid slot | Use generated default field renderer. |
| Duplicate exact slot descriptors | Emit deterministic diagnostic/error; do not pick one silently. |
| Invalid component descriptor | Emit diagnostic and use generated default renderer. |
| Slot render throws | Prevent registry-state corruption and allow generated fallback on the next render; fallback/error-boundary UI polish is deferred to Story 6-6. |
| Full view override exists in future Story 6-4 | Story 6-4 defines final precedence; 6-3 must not pre-empt it. |

### Render Pipeline Order

Field rendering resolves in this order:

1. Establish generated field metadata and role/DataGrid behavior first. Column identity, sort/filter metadata, virtualization behavior, row keys, priority, descriptors, unsupported-placeholder decisions, and generated lifecycle remain framework-owned.
2. Invoke the shared field-render helper at the field boundary. The helper checks a Level 3 slot descriptor for `(projection, role, field)` and falls back to `(projection, field)` when no role-specific descriptor exists.
3. If a valid slot is selected, build a fresh `FieldSlotContext<TProjection,TField>` for the current render and pass the default-render delegate to the slot.
4. `RenderDefault` invokes the same generated or Level 2 field delegate that would have run without the slot and must not re-enter Level 3 slot lookup for the same field.
5. If a host component needs `@key`, key the slot boundary from generated row identity plus canonical field identity. Do not key slot components by localized label text, formatted values, or component type alone.
6. If no valid slot is selected, render the generated default field path unchanged.

### Role Hosting Matrix

| Role or surface | Story 6-3 expectation |
| --- | --- |
| DataGrid | Slot may replace cell content only; grid semantics remain generated. |
| DetailRecord | Slot may replace individual field content inside the generated detail surface. |
| ActionQueue | Slot may replace field content without taking command lifecycle or navigation ownership. |
| StatusOverview | Slot may replace field content while preserving generated badge/status metadata. |
| Dashboard | Slot may replace field content only where the generated field boundary exists; unsupported dashboard placements must emit a diagnostic or documented fallback. |
| Timeline | Slot support must be explicit; if the current Timeline renderer has no safe field boundary, emit a diagnostic or documented fallback rather than silently ignoring the slot. |
| Level 2 template field delegates | Delegates call the same shared helper, so templates automatically honor Level 3 slots. |

### Render Context And Cache Safety

The slot registry is descriptor-only and scoped to the FrontComposer configuration/rendering service instance. It must not use static mutable process state for registered slot descriptors, and it must not register per-tenant or per-user descriptors. Runtime render context values influence the per-render `FieldSlotContext`; they do not influence descriptor identity or component selection.

Registry construction may validate and aggregate descriptors during app startup or generated configuration wiring, but the render-time registry view must be immutable. Concurrent renders should read the same frozen snapshot without locks around per-field lookup and without any mutation caused by component resolution.

Descriptor caching is allowed:

- projection type;
- field name and field CLR type;
- optional role;
- component type;
- expected contract version;
- generated `FieldDescriptor` metadata that is type-level and culture-neutral.

Caching is forbidden:

- `FieldSlotContext<TProjection,TField>`;
- parent projection item;
- field value;
- tenant ID or user ID;
- `RenderContext`;
- `CultureInfo.CurrentCulture` output;
- localized strings resolved at render time;
- `RenderFragment` output;
- service-provider scoped values.

### Accessibility And Localization Requirements

- Slot components inherit the custom-component accessibility contract from UX guidance: accessible name, keyboard reachability, visible focus, live-region politeness where applicable, reduced-motion respect, and forced-colors support.
- The story must expose generated labels/help text in context so components do not hard-code English labels.
- A slot replacing a badge-like field must preserve visible label text and equivalent accessible name.
- A slot replacing an unsupported placeholder must either render equivalent field/type/fix guidance or deliberately call `RenderDefault`.
- Build-time analyzer enforcement of the full accessibility contract is Story 6-6, but Story 6-3 tests should catch the obvious regressions in sample and helper components.

### Hot Reload / Rebuild Matrix

CI must validate the deterministic rebuild/startup path for slot registration metadata changes. Razor body hot reload is useful developer evidence but remains a local/manual smoke check unless the repository already has a stable hot-reload harness.

| Change | Expected dev-loop behavior |
| --- | --- |
| Slot component Razor body markup | Local hot reload smoke evidence where Blazor supports the edit; not a blocking CI gate. |
| Slot component CSS isolation | Local hot reload or browser refresh per host tooling; not a blocking CI gate. |
| Registration expression changes field | Rebuild required; expression metadata changed. |
| Registration component type changes | Rebuild required; descriptor changed. |
| Context generic type changes | Rebuild required; component contract changed. |
| Duplicate registration added/removed | Rebuild/startup validation required. |
| Framework slot contract version changes | Rebuild required; Story 6-6 owns richer version diagnostics. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | Slot registration uses typed lambda expressions, not strings or `nameof`. | Refactor safety is the core FR41 value. | `Register("Priority")`; `[ProjectionFieldSlot(nameof(Priority))]` as the primary API. |
| D2 | Level 3 is runtime registration with typed descriptors. | Adopters can wire custom components without regenerating domain assemblies, while generated views still resolve slots predictably. | Source-generator-only slot discovery; reflection scan at render time. |
| D3 | The registry caches descriptors only. | Prevents tenant/user/culture/item bleed while keeping lookup cheap. | Cache contexts or rendered fragments; build per-item descriptor dictionaries. |
| D4 | Field slots replace content at the field boundary only. | Keeps lifecycle, shell layout, DataGrid behavior, and role structures framework-owned. | Replace whole DataGrid rows/cards; allow slot to own view lifecycle. |
| D5 | Generated default renderer remains the fallback and is exposed through context. | Lets slot authors wrap or augment default rendering instead of duplicating it. | Force slot authors to recreate all default rendering manually. |
| D6 | Invalid slots fail soft to generated rendering except duplicate ambiguous registrations. | A bad customization should not erase the field; duplicates are ambiguous and require correction. | Throw on every invalid slot; silently ignore all invalid slots. |
| D7 | Level 2 delegates automatically honor Level 3 slots. | Template authors should not learn a second API to make slots work. | Require templates to call the registry manually. |
| D8 | DataGrid sort/filter/virtualization remain generated metadata behavior. | Slot content should not break DataGrid mechanics. | Let slot component own sort/filter expressions. |
| D9 | Story 6-3 does not implement dev-mode overlay starter generation. | Story 6-5 owns discovery UI and copy-to-clipboard flows. | Pull overlay and starter template generator into slot story. |
| D10 | Story 6-3 does not implement full component replacement. | Full replacement has broader lifecycle/accessibility contract and belongs to Story 6-4. | Use field slots as a backdoor for replacing whole views. |
| D11 | Duplicate exact slots are deterministic diagnostics/errors. | Silent precedence would make startup order a hidden policy. | Last registration wins; first registration wins silently. |
| D12 | Slot components use typed `Context` parameter rather than broad service injection. | Keeps the component contract visible and testable. | Require slot components to inject registry/render services. |
| D13 | Direct property selectors are the only supported v1 selector shape. | Keeps expression parsing robust and explainable. | Support nested paths and computed fields in v1. |
| D14 | Unsupported fields remain visible without a slot. | Story 4-6 no-silent-omission rule stays binding. | Hide unsupported fields until a slot exists. |
| D15 | `RenderDefault` bypasses Level 3 lookup for the active field. | Prevents a slot from recursively resolving itself when wrapping generated output. | Let fallback call the public field helper and rely on caller discipline. |
| D16 | Render-time slot lookup reads an immutable descriptor snapshot. | Avoids cross-render races and keeps the registry descriptor-only under concurrent Blazor rendering. | Mutate dictionaries during render; rebuild the registry per row. |
| D17 | Slot host keys are generated from row identity plus canonical field identity when keys are required. | Prevents virtualized row reuse from leaking component state between fields or rows. | Key by localized labels, formatted values, or component type alone. |

### Library / Framework Requirements

- Target the repository's current .NET 10 / Blazor / Fluent UI Blazor / Fluxor / Roslyn SourceTools stack.
- Use `Expression<Func<TProjection,TField>>` for refactor-safe selectors.
- Use Blazor component parameters and `RenderFragment`/`RenderFragment<T>` patterns for context and default rendering.
- Use `DynamicComponent` only if the implementation needs type-based component rendering; keep parameter dictionaries small and deterministic.
- Preserve nullable-reference annotations and trim-friendly contracts.
- Do not add a third-party component composition library.

External references checked on 2026-04-26:

- Microsoft Learn: Blazor templated components: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/templated-components?view=aspnetcore-10.0
- Microsoft Learn: Razor component generic type support: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/generic-type-support?view=aspnetcore-10.0
- Microsoft Learn: ASP.NET Core Hot Reload: https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload
- Microsoft Learn: C# expression trees: https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Rendering/FieldSlotContext.cs` | Typed field-level context passed to custom slot components. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotDescriptor.cs` | Immutable descriptor for slot override metadata. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotContractVersion.cs` | Single source of slot contract version truth. |
| `src/Hexalith.FrontComposer.Contracts/Registration/IOverrideRegistry.cs` | Existing placeholder may remain, but typed extension/facade should hide string keys. |
| `src/Hexalith.FrontComposer.Contracts/Registration/ProjectionSlotRegistryExtensions.cs` | Refactor-safe `AddSlotOverride` API. |
| `src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/*` | Runtime registry, descriptor validation, and lookup. |
| `src/Hexalith.FrontComposer.Shell/Components/Rendering/*Slot*` | Shared slot host/helper component if needed. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs` | DataGrid field-boundary integration. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs` | Role field-boundary integration. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` | Shared helper/service injection and render-context plumbing. |
| `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` | HFC diagnostics for slot selectors, components, duplicates, and version drift. |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | Public constants if diagnostics are referenced outside SourceTools. |
| `samples/Counter/Counter.Web/Components/Slots/*` | Counter Level 3 slot reference component. |
| `samples/Counter/Counter.Web/*` or generated registration hook | Counter slot registration. |
| `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/*Slot*Tests.cs` | Context and descriptor tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/*` | Registry/lookup/validation tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/*Slot*Tests.cs` | Generated field integration tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/*Slot*Tests.cs` | bUnit slot rendering tests. |

### Testing Standards

- Release-blocking P0 coverage: selector validation, typed component compatibility, duplicate detection, descriptor-only cache isolation, no-slot emitter baseline preservation, DataGrid behavior preservation, Level 2 delegate compatibility, and render-exception isolation to the extent owned by Story 6-3.
- Keep the matrix CI-safe. Each added automated test must map to one AC and one risk; broad role/culture/density permutations need an explicit risk justification instead of exhaustive combinatorics.
- Required gate evidence: contracts tests, registry/cache isolation tests, deterministic diagnostics tests, no-slot emitter baseline tests, DataGrid preservation tests, Level 2 integration tests, bUnit render-isolation tests, targeted Counter sample build/render tests, and `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
- Supporting evidence: Counter sample screenshots/manual notes and local hot-reload smoke notes. Supporting evidence cannot substitute for automated proof of behavioral contracts.
- Test direct property expression parsing exhaustively; expression parser bugs are high-risk because they defeat the refactor-safety promise.
- Include selector fixtures for inherited members, explicit interface implementations, and shadowed properties so the canonical metadata key cannot drift from the expression member.
- Keep registry tests independent of render output. Prove descriptor lookup and duplicate behavior before component tests.
- Add a recursion guard test where a custom slot calls `RenderDefault`; the assertion should prove generated fallback renders once and does not re-enter slot resolution for the same field.
- Test repeated renders with different `RenderContext`, culture, density, read-only state, and parent item values to catch stale context caching.
- Add a virtualization-style row reuse test for deterministic slot host keys when parent identity and field identity change.
- For DataGrid, assert behavior metadata remains generated: sort expressions, filter support, priority descriptors, row keys, and virtualization plumbing.
- For accessibility, assert the Counter slot preserves accessible name/visible text and does not suppress focus styles where inspectable.
- For localization, assert slot context labels/descriptions come from generated metadata and not hard-coded sample strings.
- Diagnostic assertions must verify stable diagnostic ID, severity, target member/component, and deterministic ordering. Avoid brittle full-message assertions except for the critical expected/got/fix/docs-link teaching shape.
- Use targeted tests first. Run full build with warnings as errors before closure.

### Scope Guardrails

Do not implement these in Story 6-3:

- Level 4 full component replacement.
- Dev-mode overlay, annotation outlines, detail drawer, or clipboard starter generation.
- Command-form slot replacement.
- Runtime reflection scanning for all override components.
- Public stringly override registration as the primary API.
- New DataGrid sorting/filtering abstractions owned by slot components.
- Per-tenant or per-user registry entries.
- Caching rendered fragments or `FieldSlotContext`.
- Rich runtime diagnostic panels and full override error boundaries beyond minimal field isolation.
- A new sample domain solely for slots.
- EventStore, SignalR, ETag cache, command idempotency, or observability changes.

### Non-Goals With Owning Stories

| Non-goal | Owner |
| --- | --- |
| Full component replacement and final Level 4 precedence over field slots. | Story 6-4 |
| Dev-mode overlay, annotation outlines, detail drawer, before/after toggle, and starter-template clipboard generation. | Story 6-5 |
| Rich runtime error-boundary UI, diagnostic surfacing, recovery polish, and complete custom-component accessibility analyzer enforcement. | Story 6-6 |
| Slot cookbook examples across Levels 1-4 and unsupported-field replacement. | Story 9-5 |
| Component test host utilities for adopter slot components. | Story 10-1 |
| Visual specimen coverage across theme, density, accessibility, and forced-colors modes. | Story 10-2 |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Rich dev-mode discovery of field slots, "copy starter component", and before/after toggle. | Story 6-5 |
| Full replacement precedence between view overrides and field slots. | Story 6-4 |
| Build-time analyzer enforcement of the complete custom-component accessibility contract. | Story 6-6 |
| User-facing runtime diagnostic panel for failing slot components. | Story 6-6 |
| Slot cookbook examples across Levels 1-4 and unsupported-field replacement. | Story 9-5 |
| Component test host utilities specifically for adopter slot components. | Story 10-1 |
| Visual specimen coverage for custom slots across theme/density/accessibility modes. | Story 10-2 |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-6-developer-customization-gradient.md#Story-6.3`] - story statement, ACs, FR41, FR44, UX-DR54.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md`] - FR41 slot-level lambda override, FR43 validation, FR44 hot reload, FR45 teaching diagnostics, FR47 error isolation.
- [Source: `_bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md`] - generic lambda selector and `FieldSlotContext<T>` direction.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/user-journey-flows.md`] - Level 3 overlay/starter expectations and inheritance principle.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md`] - custom component accessibility contract and zero-override visual boundary.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Source-Generator-as-Infrastructure`] - generator diagnostics, incremental rebuild, and hot reload limitations.
- [Source: `_bmad-output/implementation-artifacts/6-1-level-1-annotation-overrides.md`] - Level 1 metadata and format behavior to expose in slot context.
- [Source: `_bmad-output/implementation-artifacts/6-2-level-2-typed-razor-template-overrides.md`] - Level 2 template delegates and render-context/cache safety rules.
- [Source: `_bmad-output/implementation-artifacts/4-3-datagrid-filtering-sorting-and-search.md`] - DataGrid filtering/sorting contracts.
- [Source: `_bmad-output/implementation-artifacts/4-4-virtual-scrolling-and-column-prioritization.md`] - virtualization, priority, and row-key contracts.
- [Source: `_bmad-output/implementation-artifacts/4-5-expand-in-row-detail-and-progressive-disclosure.md`] - detail/expand-in-row lifecycle constraints.
- [Source: `_bmad-output/implementation-artifacts/4-6-empty-states-field-descriptions-and-unsupported-types.md`] - unsupported placeholder and no-silent-drop discipline.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review and elicitation remain separate hardening passes.
- [Source: `src/Hexalith.FrontComposer.Contracts/Registration/IOverrideRegistry.cs`] - existing placeholder registry.
- [Source: `src/Hexalith.FrontComposer.Contracts/Rendering/FieldDescriptor.cs`] - generated field metadata contract.
- [Source: `src/Hexalith.FrontComposer.Contracts/Rendering/RenderContext.cs`] - tenant/user/density/read-only/dev-mode context.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs`] - unsupported field diagnostic already pointing toward slots.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Transforms/ColumnModel.cs`] - generated field metadata used by emitters.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs`] - DataGrid field emission integration point.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs`] - role field emission integration point.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`] - generated component context/service plumbing.
- [Source: Microsoft Learn: Blazor templated components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/templated-components?view=aspnetcore-10.0) - typed `RenderFragment<T>` patterns.
- [Source: Microsoft Learn: Razor component generic type support](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/generic-type-support?view=aspnetcore-10.0) - generic component constraints.
- [Source: Microsoft Learn: ASP.NET Core Hot Reload](https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload) - hot reload behavior and limits.
- [Source: Microsoft Learn: C# expression trees](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/) - selector expression parsing basis.

---

## Party-Mode Review

- Date/time: 2026-04-26T12:10:28.5115364+02:00
- Selected story key: `6-3-level-3-slot-level-field-replacement`
- Command/skill invocation used: `/bmad-party-mode 6-3-level-3-slot-level-field-replacement; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Test Architect)
- Findings summary: The review found the story direction sound but identified contract ambiguity around selector grammar, Level 2/Level 3 render pipeline order, role-specific precedence, component compatibility validation, descriptor registry scoping, exception-isolation boundaries, hot-reload evidence, and CI-safe test prioritization.
- Changes applied: Narrowed the adopter promise to one known descriptor slot; clarified selector grammar; added component compatibility and contract ownership boundaries; defined render pipeline order and role hosting expectations; strengthened descriptor-only cache/registry scoping; narrowed render exception and hot-reload expectations; added release-blocking P0 test priorities, diagnostic assertion rules, required/supporting evidence, and named non-goal owners.
- Findings deferred: Full Level 4 precedence remains Story 6-4; dev overlay and starter generation remain Story 6-5; rich runtime error-boundary UI, diagnostic surfacing, recovery polish, and complete accessibility analyzer enforcement remain Story 6-6; cookbook documentation remains Story 9-5; adopter slot component test host utilities remain Story 10-1; visual specimen coverage remains Story 10-2.
- Final recommendation: ready-for-dev

---

## Advanced Elicitation

- Date/time: 2026-04-26T13:12:25.8327176+02:00
- Selected story key: `6-3-level-3-slot-level-field-replacement`
- Command/skill invocation used: `/bmad-advanced-elicitation 6-3-level-3-slot-level-field-replacement`
- Batch 1 method names: Pre-mortem Analysis; Red Team vs Blue Team; Failure Mode Analysis; Security Audit Personas; Self-Consistency Validation.
- Reshuffled Batch 2 method names: First Principles Analysis; Comparative Analysis Matrix; Occam's Razor Application; Hindsight Reflection; User Persona Focus Group.
- Findings summary: The elicitation found the story ready but still vulnerable to implementation traps around `RenderDefault` recursion, virtualized row component-state leakage, concurrent registry mutation, and selector canonicalization for inherited/interface/shadowed members.
- Changes applied: Added explicit `RenderDefault` same-field lookup bypass requirements; required immutable render-time descriptor snapshots; added deterministic slot host key guidance for virtualized rows; expanded selector, registry, bUnit, Level 2, and DataGrid test expectations to cover those risks; added binding decisions D15-D17.
- Findings deferred: No new product or architecture deferrals were introduced. Existing deferrals remain with their named owners: Level 4 precedence in Story 6-4, dev overlay/starter generation in Story 6-5, rich error-boundary/accessibility analyzer polish in Story 6-6, cookbook material in Story 9-5, adopter test-host utilities in Story 10-1, and visual specimens in Story 10-2.
- Final recommendation: ready-for-dev

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Started story 6-3 via `bmad-dev-story`; updated sprint status from `ready-for-dev` to `in-progress`.
- Added RED tests for slot contracts and Shell registry before implementation; confirmed missing contract/registry surface failed initially.
- Validation:
  - `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false -nr:false -m:1` - passed, 0 warnings.
  - `dotnet test tests\Hexalith.FrontComposer.Contracts.Tests\Hexalith.FrontComposer.Contracts.Tests.csproj --filter FullyQualifiedName~ProjectionSlotContractsTests --no-build` - passed, 11 tests.
  - `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~ProjectionSlotRegistryTests|FullyQualifiedName~CounterProjectionView_Level3Slot" --no-build` - passed, 8 tests.
  - `dotnet test tests\Hexalith.FrontComposer.SourceTools.Tests\Hexalith.FrontComposer.SourceTools.Tests.csproj` - passed, 556 tests.
  - `dotnet test Hexalith.FrontComposer.sln --no-build -nr:false -m:1 /p:UseSharedCompilation=false` - passed: SourceTools 556, Contracts 122, Shell 1312, Bench 2.

### Completion Notes List

- Added public typed Level 3 slot contracts: selector parsing, descriptor identity, contract version, immutable `FieldSlotContext<TProjection,TField>`, and `IProjectionSlotRegistry`.
- Added Shell slot registration and descriptor-only registry with role-specific precedence, role-agnostic fallback, invalid component/version fallback, and deterministic duplicate ambiguity.
- Added shared `FcFieldSlotHost<TProjection,TField>` using fresh per-render context and generated default fallback to avoid same-field recursion.
- Integrated slot lookup into generated DataGrid, DetailRecord, Timeline, and Level 2 field delegates. DataGrid keeps `PropertyColumn` default output when no slot is registered and switches to `TemplateColumn` only for registered slot fields.
- Reserved HFC1038-HFC1041 descriptors/constants and release-table rows for invalid selectors, invalid slot components, duplicate slots, and incompatible slot contract versions.
- Added Counter sample `CounterCountSlot` and typed registration; bUnit verifies one field is replaced while adjacent fields remain generated.
- Rebaselined SourceTools snapshots for the intentional Level 2/Level 3 generated plumbing and updated strategy assertions for slot-routed Timeline fields.
- Hot reload metadata changes remain rebuild/startup validated by build/test gates; rich dev-loop overlay/rebuild diagnostics remain deferred to Story 6-5/6-6 as scoped by the story.

### File List

- `_bmad-output/implementation-artifacts/6-3-level-3-slot-level-field-replacement.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor`
- `samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor.css`
- `samples/Counter/Counter.Web/Program.cs`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/FieldSlotContext.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionSlotRegistry.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotContractVersion.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotDescriptor.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotFieldIdentity.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotSelector.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotSelectorException.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/ProjectionSlotServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotDescriptorSource.cs`
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs`
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionSlotContractsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticDescriptorTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1026ReservationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/FcFieldPlaceholderColumnEmissionTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterStrategyDispatchTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.*.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/*.verified.txt`

### Change Log

- 2026-04-30: Implemented Level 3 slot contracts, Shell registry/host, generated field-boundary integration, Counter sample slot, diagnostics reservations, and regression coverage.
- 2026-04-30: Applied Group A code-review patches (P1–P15). `ProjectionSlotSelector` now strips boxing and lifted-nullable conversion chains and references `FcDiagnosticIds.HFC1038_*`; `ProjectionSlotDescriptor` validates non-null/non-empty references; `IProjectionSlotRegistry` documents thread-safety and immutable-snapshot contract; `ProjectionSlotContractVersion.Current` is derived from `Major/Minor/Build`; `FieldSlotContext` and `ProjectionSlotDescriptor` import `ProjectionRole` via `using`; `ProjectionSlotSelectorException` adds canonical CA1032 constructors; `FieldDescriptor` adds `Description` and `RazorEmitter`/`ColumnEmitter`/`ProjectionRoleBodyEmitter` flow `ColumnModel.Description` into slot context (resolves AC9 description/help-text gap from F1); HFC1039 wording tightened. Test additions: 7 new Group A contract assertions covering inherited / explicit-interface / shadowed-property selectors, lifted-nullable and `(object)x.Priority` boxing, descriptor null-input guards, and per-case rejection-reason assertions (Contracts grew from 122 → 129 tests). Snapshots re-baselined for the additive `Description` parameter. Validation: `dotnet build Hexalith.FrontComposer.sln /p:TreatWarningsAsErrors=true /p:UseSharedCompilation=false` clean (0 warnings); `dotnet test Hexalith.FrontComposer.sln --no-build` => Contracts 129/0/0, Shell 1312/0/0, SourceTools 556/0/0, Bench 2/0/0.
- 2026-04-30: Applied Group B code-review patches (GB-P1–GB-P18). `FcFieldSlotHost` logs HFC2120 on null `Parent`/`Field`/`RenderContext` (GB-P1), exposes `[EditorRequired]` on those parameters (GB-P11), accepts an optional `Key` parameter that flows through `RenderTreeBuilder.SetKey` for virtualized-row anchoring per D17 (GB-P16), and re-validates `descriptor.FieldType == typeof(TField)` before `OpenComponent` to log HFC1039 + fall back to `RenderDefault` on hand-built descriptor drift (GB-P14). `ProjectionSlotRegistry` now catches `AmbiguousMatchException` from `GetProperty("Context")` shadowed-`new` cases (GB-P3), distinguishes invalid (≤ 0) from incompatible-major contract versions in HFC1041 (GB-P5), exposes `Descriptors` as a `ReadOnlyCollection` snapshot frozen post-construction (GB-P6), rejects abstract / interface component types up front (GB-P12), and validates that the `Context` property has a public setter (GB-P13). The IL2075 trim suppression now justifies itself via the descriptor-side `[DynamicallyAccessedMembers(All)]` on `ProjectionSlotDescriptor.ComponentType` so trim metadata flows from registration through the host (GB-P4 / GB-P18). `ProjectionSlotDescriptorSource` defensive-copies the supplied list and rejects null elements with an indexed `ArgumentException` (GB-P7). `ProjectionSlotServiceCollectionExtensions.AddSlotOverride` gains a typed `<TProjection,TField,TComponent : IComponent>` overload so component-type mismatches fail at compile time (GB-P10), self-registers `IProjectionSlotRegistry` via `TryAddSingleton` so adopters who only call `AddSlotOverride` still get a working registry (GB-P15), and documents the registration contract in XML doc remarks. New runtime diagnostic `HFC2120_ProjectionSlotHostMissingParameter` reserved in `FcDiagnosticIds`. Test additions (GB-P2 / GB-P8 / GB-P9 / GB-P17): 11 new `ProjectionSlotRegistryTests` (constructor / Resolve null guards, HFC1039/1040/1041 emission verification via list-backed `ILogger`, abstract / interface / get-only-Context fixtures), 5 new `FcFieldSlotHostTests` bUnit cases (descriptor-resolved render, RenderDefault fallback, null-Parent + null-Field log assertions, FieldType vs `TField` mismatch HFC1039 + fallback), and 8 new `ProjectionSlotServiceCollectionExtensionsTests` (typed/non-typed overload behaviour, descriptor shape, registry self-registration, idempotent identical re-registration, fail-closed on different-component duplicates, invalid-selector throw at call site). Validation: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` clean (0 warnings); `dotnet test Hexalith.FrontComposer.sln --no-build -p:UseSharedCompilation=false` => Contracts 129/0/0, Shell 1335/0/0 (was 1312, +23 new), SourceTools 556/0/0, Bench 2/0/0.

### Review Findings

#### Group A — Contracts (2026-04-30)

Three-layer adversarial review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) on chunked diff `2305f8c~1..2305f8c` filtered to `Hexalith.FrontComposer.Contracts/**/*Slot*`, `FieldSlotContext.cs`, `FcDiagnosticIds.cs`, and `ProjectionSlotContractsTests.cs` (~530 LOC). Diff cached at `_bmad-output/implementation-artifacts/review-6-3-groupA/group-a-diff.patch`.

##### Decision-needed (resolved)

- [x] [Review][Decision] **F1 — resolved (option a) → P15** `FieldDescriptor` carries no `Description`/help-text channel; AC9 promises localization-aware help text. Resolution: add `Description` to the runtime `FieldDescriptor` record and flow Story 4-6's `ColumnModel.Description` through `RazorEmitter` so slot context fulfills AC9 verbatim. Tracked as patch P15.

##### Patch

- [x] [Review][Patch] **P1 — `StripBoxingConversion` strips one Convert level only; Nullable<T> chains and lifted conversions throw** [`src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotSelector.cs:54`] — `x => x.NullablePriority` typed as `Expression<Func<T, object?>>` produces `Convert(Convert(member, typeof(int?)), typeof(object))` and `(object)x.Priority` similarly nests; current code rejects both. Loop the strip until non-`Convert/ConvertChecked` is reached, and also strip Convert when `unary.Type == Nullable.GetUnderlyingType(unary.Operand.Type) ?? unary.Operand.Type`.
- [x] [Review][Patch] **P2 — Diagnostic ID hard-coded as string instead of constant reference** [`src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotSelector.cs:8`] — replace `private const string DiagnosticId = "HFC1038"` with `FcDiagnosticIds.HFC1038_ProjectionSlotSelectorInvalid`. Eliminates two-source drift.
- [x] [Review][Patch] **P3 — `ProjectionSlotDescriptor` accepts null `Type` arguments at runtime** [`src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotDescriptor.cs:18`] — positional record params are non-nullable references but C# does not enforce at runtime; add explicit `ArgumentNullException.ThrowIfNull` guards via primary-constructor body or replace with a record-with-validating-ctor pattern.
- [x] [Review][Patch] **P4 — `IProjectionSlotRegistry` lacks documented thread-safety and immutable-snapshot guarantee** [`src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionSlotRegistry.cs:14`] — Blazor renders concurrently across circuits and D16 mandates immutable render-time snapshots. Add XML doc remarks: `Resolve` must be safe under concurrent reads; `Descriptors` must return an immutable snapshot.
- [x] [Review][Patch] **P5 — `ProjectionSlotContractVersion.Current` packing range is unguarded** [`src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotContractVersion.cs:7`] — `Minor=1000` would silently masquerade as the next major. Add a static-ctor or compile-time `Debug.Assert(Minor < 1000 && Build < 1000)` (or replace literals with `checked(Major*1_000_000 + Minor*1_000 + Build)` style).
- [x] [Review][Patch] **P6 — `ProjectionSlotContractVersion` constants are independent literals; bumping `Major` without `Current` silently bypasses HFC1041** [`src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotContractVersion.cs:7`] — derive `Current` from `Major*1_000_000 + Minor*1_000 + Build` (or compute Major/Minor/Build from `Current`). Single source of truth.
- [x] [Review][Patch] **P7 — `ProjectionRole` is fully qualified inline in two files but imported via `using` in a third** [`src/Hexalith.FrontComposer.Contracts/Rendering/FieldSlotContext.cs:108,141`, `ProjectionSlotDescriptor.cs:236`] — add `using Hexalith.FrontComposer.Contracts.Attributes;` and remove FQN. Cosmetic but reduces copy-paste hazards.
- [x] [Review][Patch] **P8 — `ProjectionSlotSelectorException` lacks canonical (message, innerException) constructor (CA1032)** [`src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotSelectorException.cs:9`] — add `(string message, Exception innerException, string? paramName = "field")`. Allows wrapping future expression-compilation failures.
- [x] [Review][Patch] **P9 — `InvalidSelectors` theory data references `CapturedField` declared after the method** [`tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionSlotContractsTests.cs:163`] — move the `private static readonly int CapturedField = 7;` declaration above `InvalidSelectors()` so reordering cannot introduce silent regressions; theory-data initialization order is currently relying on file-positional luck.
- [x] [Review][Patch] **P10 — `SlotSelector_RejectsNonDirectPropertyExpressions` asserts only generic substrings; per-case rejection reasons are not validated** [`tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionSlotContractsTests.cs:139`] — split into per-case named theories or include per-case `expectedReason` data so a regression that conflates "method call" vs "indexer" diagnostics fails. Aligns with spec testing standards "deterministic ordering" rule.
- [x] [Review][Patch] **P11 — Test fixture omits `Hints` assertion** [`tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionSlotContractsTests.cs:391`] — `FieldSlotContext_ExposesSuppliedInputs` builds with `Hints: null`. AC9 promises hints flow through context. Set a non-null `RenderHints` instance in the fixture and assert it via `context.Field.Hints.ShouldBe(...)`.
- [x] [Review][Patch] **P12 — Inherited / explicit-interface / shadowed property fixtures missing from selector tests** [`tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionSlotContractsTests.cs:485-516`] — Testing Standards §"Include selector fixtures for inherited members, explicit interface implementations, and shadowed properties so the canonical metadata key cannot drift". Add positive theories asserting `property.Name` is the canonical name in each shape.
- [x] [Review][Patch] **P13 — `(object)x.Priority` boxing-conversion has no positive regression test** [`tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/ProjectionSlotContractsTests.cs`] — Selector matrix promises this is "valid only if implementation strips compiler conversion". After P1, add `ProjectionSlotSelector.Parse<P>(x => (object)x.Priority).ShouldBe(("Priority", typeof(int)))`.
- [x] [Review][Patch] **P14 — HFC1039 reservation wording is ambiguous about the component contract** [`src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs:56`] — tighten `"compatible Blazor component"` to `"Blazor component (IComponent / ComponentBase) with a public [Parameter] Context property of FieldSlotContext<TProjection,TField>"`. Clarifies AC5 expectations for adopters reading the diagnostic table.
- [x] [Review][Patch] **P15 — Add `Description` to `FieldDescriptor` to fulfill AC9 help-text promise** [`src/Hexalith.FrontComposer.Contracts/Rendering/FieldDescriptor.cs`, `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`] — append `string? Description = null` to the `FieldDescriptor` record, populate it from `ColumnModel.Description` in `RazorEmitter`, and re-baseline affected verified-txt snapshots. Resolves F1 (AC9 description/help-text channel).

##### Defer

- [x] [Review][Defer] **D1 — Selector parser `parameter != lambda.Parameters[0]` reference equality rejects rewritten lambdas** [`src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotSelector.cs:36`] — defer; D13 limits v1 to direct property selectors authored at the call site, expression-tree wrapping is not a v1 use case.
- [x] [Review][Defer] **D2 — Same-typed positional ctor on `FieldSlotContext<T,T>` allows silent `(value, parent)` swap** [`src/Hexalith.FrontComposer.Contracts/Rendering/FieldSlotContext.cs:21`] — defer; the slot host (Shell) constructs context, not adopter code, and slots over self-referential identity fields are unusual; revisit if a builder/factory is added in 6-5.
- [x] [Review][Defer] **D3 — `ArgumentNullException.ThrowIfNull(parent)` boxes value-type projections and rejects null-class previews** [`src/Hexalith.FrontComposer.Contracts/Rendering/FieldSlotContext.cs:30`] — defer; spec implies `TProjection` is always a class instance during render, and unsupported/empty rows go through `FcFieldPlaceholder`/`FcEmptyState`, not slot context.
- [x] [Review][Defer] **D4 — `Parse<TProjection,TField>` and `Parse<TProjection>` overloads can compile-collide on `object`-typed properties** [`src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotSelector.cs:24`] — defer; collision is compile-time and adopter sees CS0121, recoverable by named generic args; removing one overload would break an ergonomic API.
- [x] [Review][Defer] **D5 — `DocsLink` URL hard-coded in selector exception** [`src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotSelector.cs:9`] — defer to Story 6-6 (build-time validation, error boundaries & diagnostics) which owns canonical HFC docs URL governance.
- [x] [Review][Defer] **B1–B9 — Group B Shell-registry findings surfaced by Edge Case Hunter** — `Resolve` case-sensitivity, `IsCompatibleComponent` `AmbiguousMatchException` for inherited `Context`, negative-`ContractVersion` handling, `projectionType` open-generic/interface validation, `Descriptors` snapshot semantics, ambiguous-flag-never-recovers behavior, identical-descriptor dedupe before HFC1040, descriptor `FieldType` vs actual property mismatch, and `HFC1040` `enum.ToString()` allocation. Carry forward to Group B (Shell runtime) review chunk.

##### Dismissed (12)

- `#if NET10_0_OR_GREATER` guard asymmetry — intentional: descriptor/registry types must compile in `netstandard2.0` so SourceTools (analyzer) can reference them; only `FieldSlotContext` requires `RenderFragment<T>` and is correctly net10-only.
- `ProjectionSlotDescriptor` does not validate `ContractVersion` / component compatibility — intentional separation: descriptor is a data record; validation lives in Shell `ProjectionSlotRegistry` (HFC1039/1040/1041 paths).
- `Resolve` null-return ambiguity (no slot vs duplicate vs version mismatch) — intentional per D6 fail-soft to default; ambiguity diagnostics flow through `ILogger` (HFC1040/1041) separately.
- HFC1033–HFC1037 reservations included in this PR — those are Story 6-2 IDs already reviewed and marked done; out of Story 6-3 scope.
- `FieldSlotContext` exposes `IsReadOnly`/`IsDevMode` already reachable via `RenderContext` — intentional per AC4 contract shape (slots may diverge from render-context defaults if shell adjusts).
- `ProjectionSlotSelectorException` `Message` includes BCL `(Parameter 'field')` suffix — BCL-standard `ArgumentException` formatting; the diagnostic teaching content (`HFC1038`/Expected/Got/Fix/Docs) is at the start of the message and assertions hit it.
- Test `RenderFragment<...> fallback = static _ => static _ => { }` does not exercise the fragment — D15 (`RenderDefault` bypass) is verified at Shell level (`FcFieldSlotHost`); the contract-layer test only validates plumbing/identity.
- `FieldSlotContext` properties not declared `init` — getter-only is already immutable; `init` adds nothing for ctor-only construction.
- `TField?` cannot represent "no value" for non-nullable structs — by design; adopters select the `Nullable<T>` property explicitly when null distinction matters.
- Story 6-2 diagnostic IDs flagged as collision risk — verified non-colliding in `FcDiagnosticIds`.

#### Group B — Shell Runtime (2026-04-30)

Three-layer adversarial review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) on chunked diff `2305f8c~1..2305f8c` filtered to `Hexalith.FrontComposer.Shell/Services/ProjectionSlots/**`, `Components/Rendering/FcFieldSlotHost.cs`, `Extensions/ProjectionSlotServiceCollectionExtensions.cs`, the Story 6-3 lines of `Extensions/ServiceCollectionExtensions.cs` (TryAddSingleton<IProjectionSlotRegistry>), and `tests/.../Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs` (~470 LOC actionable + 128 LOC tests). Diff cached at `_bmad-output/implementation-artifacts/review-6-3-groupB-diff.txt` and `review-6-3-groupB-svc-diff.txt`.

Triage: 0 decision-needed + 18 patches + 35 defers (incl. B1–B9 Group A carry-forward) + 6 dismissed.

##### Patch

- [x] [Review][Patch] **GB-P1 — `FcFieldSlotHost.BuildRenderTree` silently returns when `Parent`, `Field`, or `RenderContext` is null** [`src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs:58-60`] — Field renders nothing with no log/diagnostic. A bug in the generated emitter that fails to wire `RenderContext` produces invisibly blank cells. Log a warning (HFC2010 candidate) naming the missing parameter and invoke `RenderDefault` if available before returning. (Sources: F-002 + E-017 + A-010.)
- [x] [Review][Patch] **GB-P2 — Add bUnit tests for `FcFieldSlotHost`** [`tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcFieldSlotHostTests.cs` — to be created] — T11 spec explicitly requires bUnit tests for "custom slot component context values, default fallback delegate without slot recursion, deterministic slot host keys under row reuse, accessibility label preservation, and render exception isolation". The Group B diff ships zero render tests for the host. Add tests covering: descriptor-resolved render path, RenderDefault fallback path, null-parameter early-return + diagnostic, ambiguous-descriptor → null → fallback, descriptor.FieldType vs typeof(TField) mismatch (after GB-P14), GB-P16 @key plumbing. (Sources: F-028 + E-039 + A-016.)
- [x] [Review][Patch] **GB-P3 — `IsCompatibleComponent` does not catch `AmbiguousMatchException` from `GetProperty("Context")`** [`src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:303-305`] — Components that shadow a base `Context` property via `new` cause `Type.GetProperty("Context", ...)` to throw `AmbiguousMatchException` uncaught, propagating out of the registry constructor and crashing app startup. AC11 says "Field renderer exceptions must not corrupt slot registry state". Wrap the `GetProperty` call in try/catch and convert to HFC1039 fail-soft. (Sources: A-002 + E-004 + B2.)
- [x] [Review][Patch] **GB-P4 — `[UnconditionalSuppressMessage(IL2072)]` on `BuildRenderTree` is misplaced** [`src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs:51-54`] — `RenderTreeBuilder.OpenComponent(int, Type)` does not require `DynamicallyAccessedMembers` annotations; the actual reflection happens in `ProjectionSlotRegistry.IsCompatibleComponent`. Move the suppression there (or add the equivalent) and replace the host's suppression with a `[DynamicDependency]` or `[DynamicallyAccessedMembers(PublicProperties)]` if needed for trim-safe `OpenComponent` activation. (Source: F-007.)
- [x] [Review][Patch] **GB-P5 — `IsSupportedContractVersion` accepts negative and zero `ContractVersion` and surfaces them as version-mismatch instead of "invalid version"** [`src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:281-282`] — `contractVersion / 1_000_000 == Major` returns `false` for `-1`, `0`, and any non-Major-aligned value, all reported via the same HFC1041 message. Add an explicit invalid-version diagnostic for `contractVersion <= 0` distinct from "incompatible major" so adopters see a more actionable error. (Sources: F-014 + E-007.)
- [x] [Review][Patch] **GB-P6 — `Descriptors` getter materializes a fresh `List<>` per access; not a frozen immutable snapshot** [`src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:218-230`] — `IProjectionSlotRegistry` Group A P4 documents an immutable-snapshot contract under concurrent reads. Cache the result once after constructor completes (or return `ImmutableArray<ProjectionSlotDescriptor>`) and refresh only when `_entries` mutates. Today the property allocates per call and a caller can downcast `IReadOnlyCollection<>` to `List<>` and mutate. (Sources: F-015 + E-009 + A-004.)
- [x] [Review][Patch] **GB-P7 — `ProjectionSlotDescriptorSource` stores caller's `IReadOnlyList<>` reference and does not validate non-null elements** [`src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotDescriptorSource.cs:9-12`] — Caller passing a `List<>` cast can mutate post-construction. A null entry in the list propagates to `Register(null)` and crashes the registry constructor. Defensive copy via `[..descriptors]` (or `descriptors.ToArray()`) and reject null elements deterministically (skip + log, or throw with `paramName` + index). (Sources: F-020 + F-021 + E-042.)
- [x] [Review][Patch] **GB-P8 — Tests do not cover null/empty argument guards on `Resolve` or null `IEnumerable<sources>` ctor argument** [`tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs`] — `ArgumentNullException.ThrowIfNull(projectionType)`, `ArgumentException.ThrowIfNullOrWhiteSpace(fieldName)`, and `ArgumentNullException.ThrowIfNull(sources)` are uncovered. Add three tests proving the guards fire. (Sources: F-024 + E-035 + E-036.)
- [x] [Review][Patch] **GB-P9 — Tests do not cover HFC1041 contract-version mismatch path; HFC1039/HFC1040 diagnostic emission unverified** [`tests/Hexalith.FrontComposer.Shell.Tests/Services/ProjectionSlots/ProjectionSlotRegistryTests.cs`] — `NullLogger` discards all output; the version-mismatch and ambiguous/invalid-component log paths have no test. Replace `NullLogger` with a capturing test logger (e.g., `XunitLogger` + `ITestOutputHelper` or a list-backed `ILogger` fake) and assert HFC1039/HFC1040/HFC1041 events fire with the expected projection/field/component placeholders. Add HFC1041 test by registering a descriptor whose `ContractVersion` is one major below `Major`. (Sources: F-025 + E-033.)
- [x] [Review][Patch] **GB-P10 — Add typed `AddSlotOverride<TProjection,TField,TComponent>()` overload with `where TComponent : IComponent` constraint** [`src/Hexalith.FrontComposer.Shell/Extensions/ProjectionSlotServiceCollectionExtensions.cs:118`] — Current signature accepts `Type componentType` raw; a typo registering the Context type instead of the component type only fails at app startup with a runtime warning. Add a generic overload that catches the mistake at compile time. Keep the `Type`-taking overload for codegen scenarios. (Sources: F-032 + E-014 + A-008.)
- [x] [Review][Patch] **GB-P11 — `FcFieldSlotHost` Parameter properties lack `[EditorRequired]`** [`src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs:27-44`] — `Parent`, `Field`, `RenderContext` are required for the component to function (the early return at line 58-60 proves it) but lack `[EditorRequired]`, so the Razor compiler does not warn callers who omit them. Combined with GB-P1, missing parameters cause invisible bugs. Add `[EditorRequired]` to those three. (Source: F-033.)
- [x] [Review][Patch] **GB-P12 — `IsCompatibleComponent` does not reject abstract classes or interface types** [`src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:288-322`] — `typeof(IComponent)` itself satisfies `IsAssignableFrom`; an abstract `ComponentBase` subclass passes the check; both fail at Blazor activation with opaque errors. Add `if (componentType.IsAbstract) reason = "Component type is abstract."; if (componentType.IsInterface) reason = "Component type is an interface.";` ahead of the IComponent check. (Sources: E-015 + E-030 + A-014.)
- [x] [Review][Patch] **GB-P13 — `IsCompatibleComponent` does not validate that the `Context` property has a public setter** [`src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:303-322`] — A component declaring `public FieldSlotContext<...> Context { get; }` (no setter) passes the [Parameter] + type checks but fails at Blazor parameter setting with a confusing error. Add `if (!contextProperty.CanWrite || contextProperty.SetMethod is null or { IsPublic: false }) reason = "Context property has no public setter.";`. (Source: E-025.)
- [x] [Review][Patch] **GB-P14 — `FcFieldSlotHost` does not re-validate `descriptor.FieldType == typeof(TField)` before `OpenComponent`** [`src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs:73-85`] — Adopter (or hand-built descriptor) passing wrong `FieldType` registers because `IsCompatibleComponent` checks the descriptor's `FieldType` against itself, not against the actual property type or the host's `TField`. At render the `Context` parameter cast throws `InvalidCastException`. Add `if (descriptor.FieldType != typeof(TField)) { /* log HFC1039 + RenderDefault fallback */ return; }` between the Resolve call and the OpenComponent. (Sources: B8 + E-012 + E-019 + A-013.)
- [x] [Review][Patch] **GB-P15 — `AddSlotOverride` XML doc does not mention that `IProjectionSlotRegistry` requires a separate registration call** [`src/Hexalith.FrontComposer.Shell/Extensions/ProjectionSlotServiceCollectionExtensions.cs:107-138`] — Adopter who calls only `AddSlotOverride` without the framework's bootstrap extension (which registers `IProjectionSlotRegistry`) sees a runtime DI resolution failure with no slot-aware diagnostic. Add a `<remarks>` section to the XML doc pointing to the bootstrap call. Optionally call `services.TryAddSingleton<IProjectionSlotRegistry, ProjectionSlotRegistry>()` inside `AddSlotOverride` so the API is self-sufficient. (Source: A-009.)
- [x] [Review][Patch] **GB-P16 — `FcFieldSlotHost` lacks `Key` parameter / `builder.SetKey` plumbing for virtualized rows** [`src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs:83-85`] — Spec D17 requires "slot host keys generated from row identity plus canonical field identity when keys are required" to prevent virtualized row reuse from leaking slot component state. The host has no `Key` parameter and never calls `builder.SetKey(...)`. Add an optional `[Parameter] public object? Key { get; set; }` and call `builder.SetKey(Key)` after `OpenComponent` (and on the default branch if a default `<Component>` is opened). Generated emitter (Group D) wiring is separate. (Sources: A-011 + D17.)
- [x] [Review][Patch] **GB-P17 — Tests do not exercise the `AddSlotOverride` extension** [`tests/Hexalith.FrontComposer.Shell.Tests/Extensions/ProjectionSlotServiceCollectionExtensionsTests.cs` — to be created] — `ProjectionSlotRegistryTests.cs` constructs descriptors directly and bypasses the public API. AC1 promises refactor-safe registration via `AddSlotOverride<TProjection,TField>(...)`. Add tests proving: a single call registers exactly one descriptor source; the resulting registry resolves the slot; an invalid selector at the call site throws via `ProjectionSlotSelector.Parse`; duplicate calls with identical descriptors are deduped via record equality (no HFC1040). (Source: A-015.)
- [x] [Review][Patch] **GB-P18 — Slot `ComponentType` metadata not preserved for trim/AOT despite `[UnconditionalSuppressMessage]`** [`src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs:51-54`, `ProjectionSlotRegistry.cs:284-287`] — Suppressing the analyzer silences the warning but does not preserve metadata under aggressive trimming. Adopter components in trimmed AOT may fail to bind `Context` at runtime. Add `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | PublicConstructors)]` to `ProjectionSlotDescriptor.ComponentType` (Contracts) so the trim analyzer roots the metadata at the descriptor boundary; this is a source-of-truth fix more durable than `[DynamicDependency]` at every call site. (Source: E-024.)

##### Defer

- [x] [Review][Defer] **GB-D1 — `RenderDefault?.Invoke(context)(builder)` does not null-check the inner `RenderFragment` return** [`FcFieldSlotHost.cs:79`] — defer; spec-compliant authors don't return null `RenderFragment`s, low real-world risk. (Sources: F-003 + E-040 + A-012.)
- [x] [Review][Defer] **GB-D2 — Sequence-number stability across descriptor-present vs RenderDefault branches** [`FcFieldSlotHost.cs:79,83-85`] — defer; Blazor diff handles distinct subtree branches via the parent component boundary; current 0/1 sequence numbers are conventional. (Sources: F-004 + E-020.)
- [x] [Review][Defer] **GB-D3 — `IsReadOnly = RenderContext.IsReadOnly || Field.IsReadOnly` OR-combine cannot express explicitly editable field in read-only context** [`FcFieldSlotHost.cs:69`] — defer; intentional asymmetry per FieldDescriptor semantics; document if needed. (Source: F-006.)
- [x] [Review][Defer] **GB-D4 — Constructor enumerates `IEnumerable<ProjectionSlotDescriptorSource>` once at startup; post-startup registrations are invisible** [`ProjectionSlotRegistry.cs:194-198`] — defer; Story 6-3 explicitly scopes registration to startup wiring; runtime add/remove is anti-pattern (see Group A defer D5). (Source: F-009.)
- [x] [Review][Defer] **GB-D5 — `Resolve` does not log telemetry on null-return / ambiguous-flag fall-through** [`ProjectionSlotRegistry.cs:202-216`] — defer to Story 6-6 (rich runtime diagnostics) per spec lines 461-462. (Source: F-010.)
- [x] [Review][Defer] **GB-D6 — `RegistryKey` uses `Type` reference equality across AssemblyLoadContexts** [`ProjectionSlotRegistry.cs:325`] — defer; theoretical Blazor scenario, type-identity stability is a .NET runtime invariant. (Source: F-013.)
- [x] [Review][Defer] **GB-D7 — Reflection-based Context lookup with virtual/override base properties** [`ProjectionSlotRegistry.cs:303-305`] — defer; Blazor convention is to declare `[Parameter]` on the most-derived property; corner case. (Source: F-017.)
- [x] [Review][Defer] **GB-D8 — Strict `propertyType != expectedContextType` rejects covariant/wrapper context types** [`ProjectionSlotRegistry.cs:311-314`] — defer; spec D12 requires exact typed Context, intentional. Closed-generic `OpenGenericSlot<int>` rejection message is cosmetic. (Sources: F-018 + E-001.)
- [x] [Review][Defer] **GB-D9 — `ContainsGenericParameters` vs `IsGenericTypeDefinition` for open-generic detection** [`ProjectionSlotRegistry.cs:290`] — defer; both reject the same set of relevant cases for IComponent types. (Source: F-019.)
- [x] [Review][Defer] **GB-D10 — `ProjectionRole?` not validated via `Enum.IsDefined`** [`ProjectionSlotServiceCollectionExtensions.cs:122`] — defer; defensible — adopter passing an undefined enum is a rare misuse; runtime resolve simply finds no entry. (Source: F-022.)
- [x] [Review][Defer] **GB-D11 — Cross-field exact-role + role-agnostic coexistence not exercised by tests** [`ProjectionSlotRegistryTests.cs`] — defer; covered indirectly by `Resolve_DifferentField_ReturnsNull`. (Sources: F-026 + E-038.)
- [x] [Review][Defer] **GB-D12 — Concurrent registration / Resolve+Register interleaving not tested** [`ProjectionSlotRegistryTests.cs`] — defer; constructor-only registration model means concurrent registration is not a current path; concurrent Resolve is `ConcurrentDictionary`-safe by .NET contract. (Source: F-027.)
- [x] [Review][Defer] **GB-D13 — HFC1039 message has `{Projection}` substituted twice** [`ProjectionSlotRegistry.cs:251-257`] — defer; cosmetic; ordering verified by inspection. (Source: F-029.)
- [x] [Review][Defer] **GB-D14 — `descriptor.ProjectionType.FullName` is null for arrays/pointers/by-ref types** [`ProjectionSlotRegistry.cs:240-246`] — defer; theoretical; projection types are records/classes by AC convention. (Source: F-030.)
- [x] [Review][Defer] **GB-D15 — `FieldSlotContext<,>` allocated per render** [`FcFieldSlotHost.cs:62-71`] — defer; spec mandates per-render context construction (D16, AC14, "context constructed per render and never stored"); allocation is intentional. (Source: F-031.)
- [x] [Review][Defer] **GB-D16 — `Descriptors` property hides ambiguous entries from dev-mode introspection** [`ProjectionSlotRegistry.cs:219-230`] — defer to Story 6-5 dev-mode overlay UX; ambiguity is logged via HFC1040 at registration. (Source: F-034.)
- [x] [Review][Defer] **GB-D17 — `AddOrUpdate` factory may execute multiple times under contention, duplicating HFC1040 logs** [`ProjectionSlotRegistry.cs:262-278`] — defer; constructor-only registration so contention is process-rare; if it ever fires twice it is benign log noise. (Source: E-002.)
- [x] [Review][Defer] **GB-D18 — `RegistryKey.FieldName` is ordinal case-sensitive** [`ProjectionSlotRegistry.cs:325`] — defer; Group A B1 carry-forward. Generated `Field.Name` from generator metadata is canonical exact-case; risk only on hand-built descriptors which are out of v1 scope. (Sources: E-003 + B1.)
- [x] [Review][Defer] **GB-D19 — `MakeGenericType` reflection exceptions if `FieldSlotContext<,>` ever gains generic constraints** [`ProjectionSlotRegistry.cs:300-302`] — defer; theoretical; `FieldSlotContext<,>` has no constraints today. (Source: E-005.)
- [x] [Review][Defer] **GB-D20 — `Resolve` does not validate `projectionType` against open-generic / interface / abstract types** [`ProjectionSlotRegistry.cs:202-216`] — defer; Group A B4 carry-forward. `FcFieldSlotHost<TProjection,TField>` always passes a closed generic; risk is only on hand-rolled hosts. (Sources: E-006 + B4 + A-003.)
- [x] [Review][Defer] **GB-D21 — Stale `Major` rollback diagnostic does not include the descriptor's effective major** [`ProjectionSlotRegistry.cs:240-246`] — defer; cosmetic; HFC1041 reports `Expected major {ExpectedMajor}` and the descriptor's full version is in `{ContractVersion}`. (Source: E-008.)
- [x] [Review][Defer] **GB-D22 — Once `Ambiguous` flag is set, removing one duplicate source does not restore the slot** [`ProjectionSlotRegistry.cs:262-278`] — defer; Group A B6 carry-forward. Constructor-only registration model; recovery requires process restart with corrected DI. (Sources: E-010 + B6 + A-005.)
- [x] [Review][Defer] **GB-D23 — Identical descriptors emitted by two sources rely on record equality for dedupe** [`ProjectionSlotRegistry.cs:266`] — defer; Group A B7 carry-forward. Works for default record equality; subclass/derived-record adopters are out of v1 scope. (Sources: E-011 + B7.)
- [x] [Review][Defer] **GB-D24 — `AddSlotOverride` does not check that the property selected by `field` exists / is readable on `TProjection`** [`ProjectionSlotServiceCollectionExtensions.cs:126`] — defer; `ProjectionSlotSelector.Parse` already validates expression shape; existence is intrinsic to the typed selector. (Source: E-016.)
- [x] [Review][Defer] **GB-D25 — Struct projections with `default(TProjection)` parent slip past `Parent is null` check** [`FcFieldSlotHost.cs:58`] — defer; spec D3/Group A defer D3 scope class projections only. (Source: E-018.)
- [x] [Review][Defer] **GB-D26 — Non-public `Context` properties rejected by `BindingFlags.Public`** [`ProjectionSlotRegistry.cs:303-305`] — defer; Blazor convention is public parameters; non-public is unusual. (Source: E-026.)
- [x] [Review][Defer] **GB-D27 — `Nullable<TField>` default-value handling on null `Value` for value-type `TField`** [`FcFieldSlotHost.cs:31-32, 56-65`] — defer; struct projections out of v1 scope. (Source: E-027.)
- [x] [Review][Defer] **GB-D28 — Source iteration order is DI registration order — non-deterministic across modules** [`ProjectionSlotRegistry.cs:194-198`] — defer; both ordering-dependent registrations trigger HFC1040 fail-closed, so observable behavior is order-independent. (Source: E-028.)
- [x] [Review][Defer] **GB-D29 — Custom `IEnumerable<ProjectionSlotDescriptorSource>` deferred query throwing during enumeration** [`ProjectionSlotRegistry.cs:194`] — defer; pathological IEnumerable not supported by DI conventions. (Source: E-029.)
- [x] [Review][Defer] **GB-D30 — Test does not assert which validation gate rejects `OpenGenericSlot<>`** [`ProjectionSlotRegistryTests.cs:67-73`] — defer; refactor risk only; current test passes whether the open-generic check or the closed-generic context-type check rejects it. (Source: E-032.)
- [x] [Review][Defer] **GB-D31 — `ProjectionSlotDescriptor` record equality includes `ContractVersion`; identical-component re-registration with bumped version triggers false HFC1040** [`ProjectionSlotRegistry.cs:266`] — defer; bumping `ContractVersion` mid-process is a non-scenario; if it occurs, HFC1040 + a follow-on HFC1041 will fire correctly. (Source: A-006.)
- [x] [Review][Defer] **GB-D32 — `AddSlotOverride` registers a separate `ProjectionSlotDescriptorSource` singleton per call rather than aggregating** [`ProjectionSlotServiceCollectionExtensions.cs:135`] — defer; descriptors are aggregation containers, not renderers — AC6 applies to renderer DI registrations, not configuration data. Aggregating via `ConfigureOptions<>` would be cleaner but is design polish, not a spec violation. Sources: A-007 + F-008 + E-013.
- [x] [Review][Defer] **GB-D33 — `IProjectionSlotRegistry.Resolve` accepts caller-supplied `fieldName` strings** [`ProjectionSlotRegistry.cs:202`] — defer; D1 typed-key principle covers the registration side via `AddSlotOverride`'s `Expression<Func<TProjection,TField>>`. The resolution side passes `Field.Name` from generated metadata, which is canonical. Adopter code reaching the resolver directly would regress refactor-safety, but that is not a current path. (Source: A-017.)
- [x] [Review][Defer] **GB-D34 — HFC1040 log path uses `enum.ToString()` per duplicate hit** [`ProjectionSlotRegistry.cs:273`] — defer; Group A B9 carry-forward. Duplicate-registration path fires once per process at startup; allocation is not a hot path. (Source: B9.)
- [x] [Review][Defer] **GB-D35 — Role-specific Ambiguous entry does not fall through to role-agnostic on Resolve** [`ProjectionSlotRegistry.cs:202-216`] — defer; current behavior is spec-compliant per D6/D11 ("duplicate exact slots are deterministic diagnostics/errors") — the duplicate must be visible to the adopter, not silently papered over by role-agnostic. (Sources: E-031 + E-034.)

##### Dismissed (6)

- `BuildRenderTree` does not catch slot component construction/render exceptions — intentional per spec D6/AC11 (Story 6-6 owns rich error boundary). The host's lack of catch matches "minimal explicit isolation" guidance; Blazor surrounds the render with its own error boundary. The registry constructor's reflection paths (covered by GB-P3) are the true AC11 surface.
- `ConcurrentDictionary` storage with constructor-only init is a code smell — consistent with D16 spec ("render-time slot lookup reads an immutable descriptor snapshot"); concurrent reads are safe; post-construction writes are not a current scope.
- Slot component receives only `Context`, ignoring other [Parameter] declarations — intentional per D12 (typed `Context` parameter is the contract).
- No `[CascadingParameter]` boundary established by `FcFieldSlotHost` — `OpenComponent` does not interrupt cascading values; works as-is.
- `FcFieldSlotHost` recursion guard via the host name itself — defended by accident (host has no `Context` property so `IsCompatibleComponent` rejects it); intentional via spec D15 same-field bypass discipline at the contract layer.
- A-018 cross-reference to A-002 — duplicate evidence; not a separate finding.
