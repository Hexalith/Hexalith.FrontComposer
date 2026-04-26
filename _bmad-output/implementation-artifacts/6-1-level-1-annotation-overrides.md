# Story 6.1: Level 1 Annotation Overrides

Status: ready-for-dev

> **Epic 6** - Developer Customization Gradient. **FR39 / FR44 / NFR84 / UX-DR54** annotation-level customization for generated projection fields. Applies lessons **L01**, **L06**, **L07**, **L08**, **L10**, **L11**, and **L15**.

---

## Executive Summary

Story 6-1 makes Level 1 of the customization gradient coherent and demonstrable:

- Treat existing `[Display(Name = ...)]`, `[Display(Description = ...)]`, `[Description]`, and `[ColumnPriority]` support as first-class Level 1 behavior, with regression tests proving it remains wired through Parse -> Transform -> Emit.
- Add explicit field-format annotations for the two gaps named by Epic 6: `[RelativeTime]` for DateTime-like projection fields and `[Currency]` for decimal-like projection fields.
- Keep Level 1 compile-time only. The source generator reads attributes, enriches IR, and emits deterministic Razor. No runtime override registry, component replacement, template registration, or DI-per-domain rendering is introduced here.
- Reuse the existing DataGrid and formatting path. `FcColumnPrioritizer`, `ColumnModel.Priority`, `HeaderTooltip`, resource-localized shell strings, and `CultureInfo.CurrentCulture` stay the integration points.
- Demonstrate one annotation override in the Counter sample so a developer can copy the pattern immediately.
- Make the hot-reload promise honest: attribute changes are generator inputs. The required dev-loop evidence is `dotnet watch` / incremental rebuild updating the running sample without hand-editing generated output. Pure Razor hot reload for generated `.g.cs` is not promised here; Story 6-6 owns explicit "restart/rebuild required" messaging for unsupported hot reload cases.

---

## Story

As a developer,
I want to override field rendering via declarative attributes without writing custom components,
so that I can adjust labels, formatting, column priority, and display hints with a one-line attribute change and immediate dev-loop feedback.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A projection property has `[Display(Name = "Order Date")]` | The generated DataGrid renders | The column header uses `Order Date`; this explicit name wins over humanized property names and enum humanization. |
| AC2 | A projection property has `[Display(Description = "...")]` or `[Description("...")]` | The generated DataGrid renders | Existing header tooltip/help emission still surfaces the description; `[Description]` wins over `Display.Description` when both are present. |
| AC3 | A projection property has `[ColumnPriority(1)]` | `FcColumnPrioritizer` activates for a projection with more than 15 columns | Lower priority numbers appear first in the default visible set; equal priorities remain declaration-order stable and keep HFC1028 Information behavior. |
| AC4 | A `DateTime` or `DateTimeOffset` projection property has `[RelativeTime]` | The generated DataGrid cell renders | Values within the configured relative window render as fixed-width abbreviated relative text such as `3h ago`; values older than 7 days render with the existing absolute date format. |
| AC5 | A nullable relative-time field has no value | The generated DataGrid cell renders | The cell renders the same null dash fallback used by existing date columns and emits no relative-time exception. |
| AC6 | A `decimal`, `double`, or `float` projection property has `[Currency]` | The generated DataGrid cell renders | The value is formatted with the current culture's currency format, remains right-aligned with `fc-col-numeric`, and preserves null fallback behavior. |
| AC7 | `[RelativeTime]` is applied to a non-DateTime-like field or `[Currency]` to a non-numeric field | The source generator runs | A warning diagnostic names the property, expected type family, actual type, fix, and docs link; the field falls back to the existing default formatter. |
| AC8 | A developer changes a Level 1 annotation while `dotnet watch` is running | The project rebuilds incrementally | The running sample reflects the new header/priority/formatting without manual generated-file edits or runtime registration. |
| AC9 | Level 1 annotations are evaluated as a customization-gradient level | The app starts and renders generated views | No runtime override registry, custom component, slot registration, or template file is required. |
| AC10 | The Counter sample is used as a reference implementation | The sample domain is built and rendered | At least one projection property demonstrates a Level 1 override, and tests cover the generated output. |

---

## Tasks / Subtasks

- [ ] T1. Lock current Level 1 metadata behavior as explicit story-owned coverage (AC1, AC2, AC3)
  - [ ] Keep `System.ComponentModel.DataAnnotations.DisplayAttribute` as the standard label attribute. Do not add a FrontComposer-specific display-name attribute.
  - [ ] Keep `System.ComponentModel.DescriptionAttribute` and `DisplayAttribute.Description` feeding `PropertyModel.Description`.
  - [ ] Preserve current precedence: `[Description]` beats `Display.Description`; `Display.Name` beats `CamelCaseHumanizer`; raw property name is the final fallback.
  - [ ] Preserve `ColumnPriorityAttribute` semantics: any signed int is accepted, lower is earlier, null materializes as `int.MaxValue`, collisions emit HFC1028 Information and fall back to declaration order.
  - [ ] Add or tighten tests around parse, transform, and emit so these behaviors are visibly owned by Story 6-1.

- [ ] T2. Add Contracts attributes for new format hints (AC4, AC6, AC7, AC9)
  - [ ] Add `RelativeTimeAttribute` under `src/Hexalith.FrontComposer.Contracts/Attributes/`.
  - [ ] Add `CurrencyAttribute` under the same namespace.
  - [ ] Use `[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]` for both.
  - [ ] Keep attributes dependency-free and trim-friendly. Constructors should only capture primitive/string values.
  - [ ] For `[RelativeTime]`, default the relative window to 7 days; allow a bounded integer override only if it stays simple and testable.
  - [ ] For `[Currency]`, default to the current culture currency format. Optional ISO currency-code support is allowed only if it does not require new package dependencies or a runtime service.

- [ ] T3. Extend Parse-stage IR without breaking existing equality/caching contracts (AC4, AC6, AC7)
  - [ ] Extend `PropertyModel` with a small format-hint value, enum, or record that can represent default, relative-time, and currency.
  - [ ] Parse `[RelativeTime]` and `[Currency]` in `AttributeParser.ParseProperty`.
  - [ ] Include the new field in `PropertyModel.Equals` and `GetHashCode`; update `DomainModelCacheEqualityTests` if needed.
  - [ ] Emit warning diagnostics for incompatible type usage. Reserve new SourceTools IDs in the HFC10xx range, documenting them in `DiagnosticDescriptors`, `FcDiagnosticIds` if needed, and analyzer release notes.
  - [ ] Warnings must be fail-soft: keep generating the column with the prior default format rather than dropping the field.

- [ ] T4. Extend Transform-stage column metadata (AC4, AC6, AC7)
  - [ ] Extend `ColumnModel` to carry the Level 1 format override.
  - [ ] Make `RazorModelTransform.GetFormatHint` choose:
    - `[Currency]` -> currency format for numeric categories only.
    - `[RelativeTime]` -> relative-time render mode for DateTime-like categories only.
    - No annotation -> existing defaults (`N0`, `N2`, `d`, `t`, `Humanize:30`, etc.).
  - [ ] Preserve stable column sorting by priority and declaration order. Format overrides must not affect column ordering.
  - [ ] Add tests proving explicit `Display.Name` still controls headers when a format annotation is also present.

- [ ] T5. Emit currency formatting through the existing numeric column path (AC6)
  - [ ] Reuse `ColumnEmitter.EmitNumericColumn` rather than creating a second numeric component.
  - [ ] Format non-null values with the .NET currency standard format string and `CultureInfo.CurrentCulture`.
  - [ ] Keep `Class = "fc-col-numeric"` so existing right-alignment and DataGrid styling apply.
  - [ ] Preserve sort behavior over the underlying numeric property, not the formatted string.
  - [ ] Add EN/FR culture tests where the existing test harness can switch `CurrentCulture` safely.

- [ ] T6. Emit relative-time formatting deterministically (AC4, AC5)
  - [ ] Prefer a small generated helper method or Shell helper that accepts the value and a `DateTimeOffset now` from an injected `TimeProvider`.
  - [ ] Register or reuse `TimeProvider.System` in Shell service setup when missing; tests should inject a fake provider rather than relying on wall-clock time.
  - [ ] Use fixed-width abbreviated labels for the relative window: examples such as `5m ago`, `3h ago`, `2d ago`. Keep copy concise and stable for DataGrid scanning.
  - [ ] After 7 days, fall back to the same absolute date format used by unannotated DateTime columns.
  - [ ] Respect nullable fallback with the existing dash representation.
  - [ ] Avoid per-cell timers in v1. Values may update on render/query refresh; continuous live ticking is out of scope.

- [ ] T7. Preserve generated DataGrid and role-specific surfaces (AC1-AC7)
  - [ ] Standard DataGrid, ActionQueue, StatusOverview, DetailRecord, and Timeline emit paths must agree on the same formatter when they render the annotated field.
  - [ ] If a role-specific path cannot yet use the new formatter safely, document and test the intentional fallback rather than silently diverging.
  - [ ] Do not alter badge mapping, unsupported placeholder emission, empty-state CTA behavior, or grouping semantics.

- [ ] T8. Add Counter sample reference override (AC8, AC10)
  - [ ] Add a minimal Level 1 annotation to `samples/Counter/Counter.Domain/CounterProjection.cs`; recommended: `[Display(Name = "Last changed")]` or `[RelativeTime]` on `LastUpdated`.
  - [ ] Keep the sample small. Do not introduce Orders/TaskTracker solely for this story.
  - [ ] Update snapshot or generated-output tests that assert Counter projection headers/formatting.
  - [ ] Add a short comment only if needed to explain why the sample uses the annotation.

- [ ] T9. Dev-loop and hot-reload evidence (AC8)
  - [ ] Add a focused test proving generator output changes when an annotation changes between two compilations.
  - [ ] If practical, add an integration note or test fixture documenting `dotnet watch` incremental rebuild behavior for attribute changes.
  - [ ] Do not promise pure CLR/Razor hot reload for source-generator input changes. If a scenario needs restart/rebuild messaging, add it to Known Gaps with Story 6-6 ownership.

- [ ] T10. Tests and verification (AC1-AC10)
  - [ ] Contracts tests for `RelativeTimeAttribute`, `CurrencyAttribute`, and existing `ColumnPriorityAttribute` behavior.
  - [ ] Parse tests for valid attributes, invalid-type warnings, and precedence with `Display`/`Description`.
  - [ ] Transform tests for `ColumnModel` metadata, display-name precedence, priority sorting, and fallback formatting.
  - [ ] Emitter approval tests for currency and relative-time columns, including nullable cases.
  - [ ] Shell/component tests for relative-time helper output and culture-sensitive currency formatting.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [ ] Targeted tests: Contracts attribute tests, SourceTools parse/transform/emit tests, and Shell formatting tests. Run full solution tests if the working tree is otherwise clean.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Attributes/ColumnPriorityAttribute.cs` | Already shipped by Story 4-4; property-level attribute, any `int`, lower value first, unannotated sorts after explicit values. | Do not change constructor semantics or HFC1028 behavior. |
| `src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionAttribute.cs` | Explicitly tells adopters to use BCL `DisplayAttribute` for display metadata. | Keep BCL metadata as the primary Level 1 label/description path. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` | Parses `DisplayAttribute`, `DescriptionAttribute`, `ColumnPriorityAttribute`, `ProjectionFieldGroupAttribute`, empty-state CTA, badges, roles, and unsupported field diagnostics. | Extend this parser; do not add a second metadata scanner. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs` | `PropertyModel` carries display name, description, priority, field group, enum mappings, unsupported FQN, and equality/cache data. | Add format metadata carefully and update equality/hash coverage. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModelTransform.cs` | Resolves headers, format hints, type categories, role strategy, priority sorting, and HFC1029 activation. | Extend `GetFormatHint`/column metadata without reordering unrelated columns. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/ColumnModel.cs` | Carries the DataGrid column metadata consumed by emitters and prioritizer descriptors. | Preserve `Priority`, `FieldGroup`, `Description`, filter support, and equality shape. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs` | Emits text, numeric, boolean, DateTime, enum, collection, and unsupported DataGrid columns. Numeric columns already use `CultureInfo.CurrentCulture`; date columns use standard absolute formats. | Reuse these paths; do not introduce parallel DataGrid components. |
| `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.*` | Uses `ColumnDescriptor(Key, Header, Priority)` and generator-provided sorted column metadata. | Story 6-1 should not change the prioritizer UI; it only proves annotation priority still drives it. |
| `samples/Counter/Counter.Domain/CounterProjection.cs` | Minimal sample projection with `Id`, `Count`, and `LastUpdated`. | Keep it readable as the first adopter sample. |

### Cross-Story Contract Table

| Seam | Producer | Consumer | Story 6-1 decision |
| --- | --- | --- | --- |
| BCL display metadata | Stories 1-4 parser/transform pipeline | Level 1 labels/descriptions | Reuse `[Display]` and `[Description]`; no custom display attribute. |
| Column priority | Story 4-4 | Level 1 annotation contract | Treat existing `[ColumnPriority]` as Level 1; add regression ownership, not a new implementation. |
| Field descriptions | Story 4-6 | Header tooltip/help | Preserve description propagation and precedence while adding format hints. |
| DataGrid column emission | Stories 4-1 through 4-6 | Relative/currency formatting | Extend `ColumnEmitter`; do not fork role-specific emitters. |
| Localization/culture | Shell resources + current culture | Currency and relative-time text | Currency follows `CultureInfo.CurrentCulture`; relative-time abbreviations must be concise and testable. |
| Hot reload / generator rebuild | Architecture source-generator constraint | Developer dev-loop | `dotnet watch` incremental rebuild is the evidence path; Story 6-6 owns unsupported hot reload messaging. |
| Future Levels 2-4 | Epic 6 Stories 6-2 to 6-4 | Override gradient | Level 1 cannot introduce runtime registry or custom component contracts. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | BCL `[Display]` remains the label source; no FrontComposer display-name attribute. | Existing code and .NET metadata already support `Name`, `Description`, `GroupName`, and resources. | Add `[FcDisplayName]`; infer labels from resource files first. |
| D2 | `[Description]` keeps precedence over `Display.Description`. | Story 4-6 already resolved this and it matches the more direct property-description intent. | Last attribute wins; duplicate tooltip content. |
| D3 | `[ColumnPriority]` is reclassified as Level 1 rather than rebuilt. | It already ships in Contracts/Parse/Transform/Prioritizer. | Create `[DisplayOrder]`; use `DisplayAttribute.Order` as the only priority input. |
| D4 | New `[RelativeTime]` and `[Currency]` attributes live in Contracts. | Domain assemblies need dependency-free annotations; SourceTools can read them at compile time. | Put annotations in Shell; use runtime options only. |
| D5 | Invalid format annotations warn and fall back. | Annotation mistakes should teach without breaking unrelated generated UI. | Error and block build; silently ignore. |
| D6 | Relative time does not continuously tick per cell. | Per-cell timers would be expensive and unnecessary for DataGrid scanning. | One timer per row/cell; SignalR-driven live ticking. |
| D7 | Currency formatting uses current culture by default. | Epic AC asks for locale-formatted currency symbol and existing numeric path already uses `CultureInfo.CurrentCulture`. | Hard-code USD/EUR; require resource keys per field. |
| D8 | Sort/filter semantics stay based on raw field values. | Formatting should not corrupt DataGrid sorting or filtering behavior. | Sort formatted strings; disable sorting for formatted fields. |
| D9 | Attribute changes are generator input changes. | Current architecture states generated `.g.cs` does not have pure Razor hot reload semantics. | Promise in-process hot reload for source-generator input changes. |
| D10 | Sample evidence stays in Counter. | Solo-maintainer filter: avoid adding a new sample domain just to show one attribute. | Create a full Orders sample in this story. |

### Library / Framework Requirements

- Target current repo stack and package lines: .NET 10, Blazor, Fluent UI Blazor, Fluxor, Roslyn SourceTools, xUnit, bUnit, Shouldly, NSubstitute, Verify.
- Use `System.ComponentModel.DataAnnotations.DisplayAttribute` and `System.ComponentModel.DescriptionAttribute` for existing BCL metadata.
- Use `AttributeUsageAttribute` correctly for new property-level attributes.
- Use standard .NET numeric currency formatting (`C`/`C2` as chosen by tests) with `CultureInfo.CurrentCulture`.
- Use standard .NET date/time absolute formatting for the fallback path.
- Use `TimeProvider` or an injectable clock seam for relative-time tests. Avoid `DateTimeOffset.UtcNow` directly in code that must be deterministic.

External references checked on 2026-04-26:

- Microsoft Learn: `DisplayAttribute`: https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.displayattribute
- Microsoft Learn: `DescriptionAttribute`: https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.descriptionattribute.description
- Microsoft Learn: writing custom attributes: https://learn.microsoft.com/en-us/dotnet/standard/attributes/writing-custom-attributes
- Microsoft Learn: standard numeric format strings: https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings
- Microsoft Learn: standard date and time format strings: https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings
- Microsoft Learn: ASP.NET Core Hot Reload: https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Attributes/RelativeTimeAttribute.cs` | Compile-time Level 1 date/time formatting annotation. |
| `src/Hexalith.FrontComposer.Contracts/Attributes/CurrencyAttribute.cs` | Compile-time Level 1 numeric currency formatting annotation. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs` | Parse new attributes and invalid-type diagnostics. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs` | Carry format metadata through `PropertyModel`. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/ColumnModel.cs` | Carry format metadata to emitters. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModelTransform.cs` | Resolve format override precedence. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs` | Emit currency and relative-time rendering. |
| `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` | Add warning descriptors for incompatible annotations if new IDs are reserved. |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | Add public diagnostic constants if the repo pattern requires it for new IDs. |
| `tests/Hexalith.FrontComposer.Contracts.Tests/Attributes/*` | Attribute constructor/usage tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/*` | Parse and warning tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/*` | Format precedence and metadata tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/*` | Approval/snapshot coverage for generated columns. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/*` or adjacent helper tests | Relative-time/culture formatting behavior if helper lives in Shell. |
| `samples/Counter/Counter.Domain/CounterProjection.cs` | Minimal reference annotation. |

### Testing Standards

- Keep SourceTools parse/transform tests pure and deterministic. Avoid tests that depend on wall-clock time.
- Use culture scopes that restore the original culture in `finally` or test fixtures.
- Keep relative-time thresholds explicit. Boundary cases: just under 1 minute, minutes, hours, days, exactly 7 days, older than 7 days, future timestamps, nullable.
- For currency: decimal, nullable decimal, double/float if supported, and invalid string/DateTime cases.
- For invalid attributes: assert warning ID, message includes What/Expected/Got/Fix/DocsLink, and generated output still exists.
- Approval snapshots should only change where the annotation changes emitted output.
- If full solution tests are expensive because unrelated work is dirty, run targeted Contracts/SourceTools/Shell tests and document the limitation.

### Scope Guardrails

Do not implement these in Story 6-1:

- Level 2 typed Razor templates.
- Level 3 slot-level field replacement or `IOverrideRegistry`.
- Level 4 full component replacement.
- `FcDevModeOverlay`, starter template generation, or clipboard support.
- Build-time version compatibility validation for override contracts.
- Runtime error boundaries for faulty overrides.
- A custom localization/resource system for field display names.
- Per-cell live ticking timers for relative time.
- A new sample domain solely for customization.
- Changes to EventStore, SignalR, ETag cache, command lifecycle, or reconnect behavior.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Pure hot reload messaging for source-generator input changes that require rebuild/restart. | Story 6-6 |
| Template-level override starter code that shows the same field formatting in a custom layout. | Story 6-2 / 6-5 |
| Slot-level replacement cookbook for fields that outgrow `[RelativeTime]` or `[Currency]`. | Story 6-3 / Story 9-5 |
| Dev-mode overlay discovery of the active Level 1 annotation and recommended next level. | Story 6-5 |
| Public customization cookbook page showing the same problem solved at Levels 1-4. | Story 9-5 |
| Advanced localized relative-time grammar beyond compact fixed-width abbreviations. | Story 9-5 or localization backlog |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-6-developer-customization-gradient.md#Story-6.1`] - story statement, baseline ACs, FR39, UX-DR54, NFR84.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#Developer-Customization--Override-System`] - FR39 annotation-level override, FR44 hot reload, FR45 diagnostics.
- [Source: `_bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md#API-Surface`] - Layer 1 attribute-driven surface and customization-gradient examples.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#FcColumnPrioritizer`] - priority-driven DataGrid column visibility.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md#FcDevModeOverlay`] - future discovery path, explicitly out of scope here.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Source-Generator-as-Infrastructure`] - generator input changes, IR extraction, diagnostics, and incremental performance constraints.
- [Source: `_bmad-output/implementation-artifacts/4-4-virtual-scrolling-and-column-prioritization.md`] - existing `[ColumnPriority]` semantics and prioritizer contract.
- [Source: `_bmad-output/implementation-artifacts/4-6-empty-states-field-descriptions-and-unsupported-types.md`] - description propagation, unsupported placeholder discipline, and no silent omission rule.
- [Source: `_bmad-output/implementation-artifacts/5-7-signalr-fault-injection-test-harness.md`] - recent story pattern for test-support scoping and solo-maintainer guardrails.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review and elicitation remain separate hardening passes.
- [Source: `src/Hexalith.FrontComposer.Contracts/Attributes/ColumnPriorityAttribute.cs`] - existing Level 1 priority attribute.
- [Source: `src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionAttribute.cs`] - BCL display metadata guidance.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs`] - current metadata parser.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs`] - `PropertyModel` IR and equality/cache surface.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModelTransform.cs`] - current header, type category, format hint, and priority sorting logic.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs`] - current DataGrid column emission.
- [Source: `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor.cs`] - column descriptor and prioritizer behavior.
- [Source: `samples/Counter/Counter.Domain/CounterProjection.cs`] - reference sample projection to annotate.
- [Source: Microsoft Learn: `DisplayAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.displayattribute) - BCL display metadata.
- [Source: Microsoft Learn: `DescriptionAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.descriptionattribute.description) - BCL description metadata.
- [Source: Microsoft Learn: writing custom attributes](https://learn.microsoft.com/en-us/dotnet/standard/attributes/writing-custom-attributes) - attribute declaration and `AttributeUsageAttribute`.
- [Source: Microsoft Learn: standard numeric format strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings) - currency format behavior.
- [Source: Microsoft Learn: ASP.NET Core Hot Reload](https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload) - hot reload scope and limitations.

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
