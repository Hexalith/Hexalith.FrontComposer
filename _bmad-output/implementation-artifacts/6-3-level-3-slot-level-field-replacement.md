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
