# Story 6.3: Level 3 Slot-Level Field Replacement

Status: ready-for-dev

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
| AC11 | A slot override component throws during render | The generated view renders | The failure is isolated to the affected field as far as Story 6-3 can support; the story records remaining runtime error-boundary polish as Story 6-6 ownership. No exception may corrupt unrelated field output or cached registry state. |
| AC12 | The developer edits the custom slot component Razor markup under `dotnet watch` | The host supports Blazor hot reload | The field rendering updates without application restart; changes to registration expressions or component type metadata require rebuild and must be documented. |
| AC13 | Duplicate slot overrides are registered for the same projection, role, and field | The registry validates descriptors | Duplicate behavior is deterministic: either last registration is rejected with a diagnostic, or build/startup fails with an explicit HFC error. Silent last-writer-wins is forbidden. |
| AC14 | The same projection is rendered for different tenants, users, cultures, densities, and read-only states | Slot contexts are created repeatedly | The registry caches descriptors only; it must never cache `FieldSlotContext`, parent item values, rendered fragments, tenant/user data, culture-specific text, or default-render output across renders. |
| AC15 | The Counter sample is used as reference evidence | The sample renders with a slot override | One field uses a custom slot component, adjacent fields remain generated, Level 1 labels/formatting still flow into context, and targeted tests prove default fallback plus invalid-registration diagnostics. |
| AC16 | The story is evaluated as Level 3 in the customization gradient | A developer compares it to Levels 1, 2, and 4 | Level 3 is field-level replacement only. It does not add full component replacement, command-form customization, dev-mode overlay UI, starter-template generation, or public stringly override APIs. |

---

## Tasks / Subtasks

- [ ] T1. Define the public Level 3 registration API (AC1, AC2, AC6, AC13)
  - [ ] Add typed extension methods over the existing registry surface, for example `AddSlotOverride<TProjection,TField>(Expression<Func<TProjection,TField>> field, Type componentType, ProjectionRole? role = null)`.
  - [ ] Accept only direct member access on the projection parameter, including nullable value fields. Reject nested members, method calls, conversions that hide invalid expressions, captured variables, indexers, and computed expressions.
  - [ ] Normalize the field identity from Roslyn/generated metadata and expression-tree member info, not adopter strings.
  - [ ] Define duplicate behavior as reject-with-diagnostic. Do not allow silent last-writer-wins.
  - [ ] Keep the public API discoverable from IntelliSense and dependency-light for adopters.
  - [ ] Avoid adding DI registrations per projection/field/component combination.

- [ ] T2. Introduce typed slot contracts (AC4, AC5, AC9, AC14)
  - [ ] Add `FieldSlotContext<TProjection,TField>` under `src/Hexalith.FrontComposer.Contracts/Rendering/`.
  - [ ] Include:
    - `TField? Value`
    - `TProjection Parent`
    - `FieldDescriptor Field`
    - `RenderContext RenderContext`
    - `ProjectionRole? ProjectionRole`
    - `DensityLevel DensityLevel`
    - `bool IsReadOnly`
    - `bool IsDevMode`
    - `RenderFragment<FieldSlotContext<TProjection,TField>>? RenderDefault` or an equivalent default-render delegate.
  - [ ] Keep the context immutable/read-only after construction.
  - [ ] Add a minimal component contract if needed, for example an interface or marker that requires a `Context` parameter. Do not create a broad renderer hierarchy.
  - [ ] Ensure context values are constructed per render and are never stored in the registry.
  - [ ] Provide XML docs warning slot authors not to mutate parent projection objects or cache render context across tenants/users.

- [ ] T3. Add slot descriptor and registry implementation (AC3, AC5, AC6, AC13, AC14)
  - [ ] Add immutable slot descriptors storing projection type, field name, field CLR type, optional role, component type, expected contract version, and diagnostic state.
  - [ ] Add a typed registry facade, for example `IProjectionSlotRegistry`, backed by generated/runtime registration descriptors.
  - [ ] If the existing `IOverrideRegistry` is reused internally, hide string keys behind typed extension methods and typed descriptor validation.
  - [ ] Runtime lookup should be O(1) or bounded dictionary lookup by `(projectionType, role, fieldName)` with fallback to role-agnostic registration.
  - [ ] Registry entries must be descriptor-only. Do not cache parent item, context, service provider scoped values, or rendered markup.
  - [ ] Validate duplicate descriptors deterministically and name both registrations/components in diagnostics where possible.
  - [ ] Do not scan loaded assemblies for slot components at render time.

- [ ] T4. Validate component compatibility (AC5, AC11)
  - [ ] Validate that the component type is a Blazor component with a required `Context` parameter compatible with `FieldSlotContext<TProjection,TField>`.
  - [ ] Reject open generics unless the registry can close them deterministically from `TProjection` and `TField`.
  - [ ] Reject mismatched `TField` types, including nullable/non-nullable mismatches that would produce invalid casts.
  - [ ] Use deterministic HFC diagnostics with the standard message shape: What, Expected, Got, Fix, DocsLink.
  - [ ] Define fallback behavior for invalid components: no slot selected, default generated field rendering used.
  - [ ] Keep render exception isolation minimal and explicit in Story 6-3; defer rich diagnostic panels and cross-level error boundaries to Story 6-6.

- [ ] T5. Integrate slot lookup into generated field emission (AC3, AC7, AC8, AC9)
  - [ ] Add a slot-resolution helper emitted by `RazorEmitter` or shared Shell code that receives the parent item, field metadata, current render context, role, and default-render fragment.
  - [ ] Integrate at the field boundary in `ColumnEmitter`, `ProjectionRoleBodyEmitter`, and any shared field/section delegates used by Level 2 templates.
  - [ ] Preserve existing `PropertyColumn` sort/filter expressions or generated metadata so a custom cell renderer does not change DataGrid behavior.
  - [ ] For DataGrid cells, render the slot through a `TemplateColumn` or equivalent only when a slot is actually present; no-slot output should remain as close to existing output as possible.
  - [ ] For non-grid roles, wrap exactly the generated field content, not the entire card/row/timeline item.
  - [ ] Preserve unsupported placeholder behavior: a slot can replace the field renderer, but invalid/missing slot must still show `FcFieldPlaceholder` for unsupported types.
  - [ ] Preserve badge, relative-time, currency, label, description, priority, grouping, empty-state, and accessibility metadata in default rendering and context.

- [ ] T6. Preserve Level 2 template composition (AC10)
  - [ ] Ensure Level 2 field delegates call the same field-render helper used by default generated layouts.
  - [ ] A template author should not need separate slot lookup code.
  - [ ] Template field delegates should pass the same `FieldSlotContext` metadata that default views pass.
  - [ ] Add tests proving a Level 2 template still renders a Level 3 slot for one field and default generated delegates for adjacent fields.

- [ ] T7. Role and precedence matrix (AC7, AC13, AC16)
  - [ ] Define slot matching precedence:
    1. Valid role-specific slot for `(projection, role, field)`.
    2. Valid role-agnostic slot for `(projection, field)`.
    3. Generated default field renderer.
  - [ ] Duplicate exact matches are diagnostics/errors, not precedence.
  - [ ] Invalid component descriptors are ignored after diagnostics and do not block fallback.
  - [ ] Role-specific unsupported behavior must be explicit. If Timeline or Dashboard cannot safely host a slot in this story, record the limitation with a named owner instead of silently ignoring the slot.
  - [ ] Keep Level 4 full replacement out of precedence. Level 4 will wrap or bypass field-level logic in Story 6-4.

- [ ] T8. Hot reload and dev-loop evidence (AC12, AC15)
  - [ ] Prove Razor markup edits in the custom slot component refresh under `dotnet watch` where Blazor hot reload supports the edit.
  - [ ] Document that registration expression changes, component type changes, generic context changes, and contract version changes are rebuild-triggering metadata changes.
  - [ ] Add a small generated or sample note that points unsupported hot-reload cases to Story 6-6 rebuild/restart diagnostics.
  - [ ] Do not promise source-generator input hot reload beyond the repo's architecture constraint.

- [ ] T9. Counter sample reference implementation (AC15)
  - [ ] Add one custom slot component in the Counter sample, recommended for a single display field that benefits from a richer visual treatment.
  - [ ] Register the slot through the typed lambda API.
  - [ ] Keep the sample small: one slot override, one default adjacent field, and one invalid-registration test fixture.
  - [ ] Preserve Level 1 annotation evidence from Story 6-1 and Level 2 template compatibility from Story 6-2 where available.
  - [ ] Do not add a new Orders sample solely for this story.

- [ ] T10. Diagnostics, docs links, and contract versioning (AC2, AC5, AC13)
  - [ ] Reserve stable HFC10xx SourceTools/build-time diagnostics for invalid selectors, incompatible component context, duplicate slots, and version drift.
  - [ ] Reserve Shell HFC20xx runtime diagnostics only if startup/runtime descriptor validation owns an error.
  - [ ] Include contract version metadata so Story 6-6 can enforce override compatibility consistently across Levels 2-4.
  - [ ] Diagnostics must include enough context for self-service: projection type, field name when resolvable, component type, expected context, actual context, fix, and docs link.

- [ ] T11. Tests and verification (AC1-AC16)
  - [ ] Contracts tests for `FieldSlotContext<TProjection,TField>`, descriptor immutability, and registration extension shape.
  - [ ] Expression validation tests for direct property, nullable property, nested member rejection, method-call rejection, captured-variable rejection, indexer rejection, and conversion handling.
  - [ ] Registry tests for role-specific precedence, role-agnostic fallback, duplicate rejection, invalid descriptor fallback, descriptor-only caching, and no render-context cache bleed.
  - [ ] SourceTools/emitter tests proving default output remains unchanged when no slots exist and slot output is used only for the overridden field.
  - [ ] DataGrid tests proving sort/filter/virtualization/row key/priority metadata remain generated when a slot renders cell content.
  - [ ] Level 2 integration tests proving template field delegates honor Level 3 slots.
  - [ ] bUnit tests for custom slot component context values, default fallback delegate, accessibility label preservation, and render exception isolation.
  - [ ] Counter sample build/render tests for one valid slot and one invalid-registration diagnostic.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [ ] Targeted tests: Contracts, SourceTools slot parser/emitter tests, Shell registry/rendering tests, and Counter sample tests.

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

### Selector Validation Matrix

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
| No valid slot | Use generated default field renderer. |
| Duplicate exact slot descriptors | Emit deterministic diagnostic/error; do not pick one silently. |
| Invalid component descriptor | Emit diagnostic and use generated default renderer. |
| Slot render throws | Isolate to field where possible; fallback/error-boundary polish deferred to Story 6-6. |
| Full view override exists in future Story 6-4 | Story 6-4 defines final precedence; 6-3 must not pre-empt it. |

### Render Context And Cache Safety

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

| Change | Expected dev-loop behavior |
| --- | --- |
| Slot component Razor body markup | Hot reload where Blazor supports the edit. |
| Slot component CSS isolation | Hot reload or browser refresh per host tooling; no framework restart requirement beyond Blazor behavior. |
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

- Test direct property expression parsing exhaustively; expression parser bugs are high-risk because they defeat the refactor-safety promise.
- Keep registry tests independent of render output. Prove descriptor lookup and duplicate behavior before component tests.
- Test repeated renders with different `RenderContext`, culture, density, read-only state, and parent item values to catch stale context caching.
- For DataGrid, assert behavior metadata remains generated: sort expressions, filter support, priority descriptors, row keys, and virtualization plumbing.
- For accessibility, assert the Counter slot preserves accessible name/visible text and does not suppress focus styles where inspectable.
- For localization, assert slot context labels/descriptions come from generated metadata and not hard-coded sample strings.
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

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

(to be filled in by dev agent)

### File List

(to be filled in by dev agent)
