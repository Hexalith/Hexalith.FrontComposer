# Story 6.1: Level 1 Annotation Overrides

Status: done

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

### Adopter Job To Preserve

Generated projection fields should carry common display intent from the domain model into the generated UI without requiring adopters to edit generated Razor, register runtime renderers, or create custom components. Level 1 is compile-time metadata only: annotations enrich SourceTools IR and the existing emitters choose the established rendering path.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A projection property has `[Display(Name = "Order Date")]` | The generated DataGrid renders without template edits | The column header uses `Order Date`; this explicit annotation wins over humanized property names and enum humanization. |
| AC2 | A projection property has `[Display(Description = "...")]` or `[Description("...")]` | The generated DataGrid renders | Existing header tooltip/help metadata still surfaces through the current accessible description path; `[Description]` wins over `Display.Description` when both are present. |
| AC3 | A projection property has `[ColumnPriority(1)]` | `FcColumnPrioritizer` activates for a projection with more than 15 columns | Lower priority numbers appear first in the default visible set; unannotated columns sort after explicit priorities; equal priorities remain declaration-order stable and keep HFC1028 Information behavior. |
| AC4 | A `DateTime`, `DateTimeOffset`, or nullable variant projection property has `[RelativeTime]` | The generated DataGrid cell renders using a deterministic clock seam | Values within the configured relative window render as fixed-width abbreviated relative text such as `3h ago`; values older than 7 days render with the existing absolute date format; UTC, Local, and Unspecified `DateTime.Kind` behavior is covered by tests. |
| AC5 | A nullable relative-time field has no value | The generated DataGrid cell renders | The cell renders the same null dash fallback used by existing date columns and emits no relative-time exception. |
| AC6 | A `decimal`, `double`, `float`, or nullable variant projection property has `[Currency]` | The generated DataGrid cell renders | The value is formatted with the current culture's standard currency format, remains right-aligned with `fc-col-numeric`, preserves null fallback behavior, and keeps sort/filter semantics on the raw numeric value. |
| AC7 | `[RelativeTime]` is applied to a non-DateTime-like field, `[Currency]` to a non-numeric field, or mutually exclusive format annotations are combined on the same field | The source generator runs | A stable warning diagnostic ID names the property, attribute, expected type family or exclusivity rule, actual type, fix, docs link, and fallback behavior; generated code still compiles, the column remains emitted, and the existing default formatter applies. |
| AC8 | A field uses supported Level 1 display metadata | The generated UI renders under localized/culture-sensitive contexts | The story preserves existing `DisplayAttribute` and shell localization behavior, currency uses `CultureInfo.CurrentCulture`, and no new custom field-resource system is introduced. |
| AC9 | A developer changes, removes, or changes constructor arguments for a Level 1 annotation while `dotnet watch` is running | The project rebuilds incrementally | The affected generated projection output changes and the running sample reflects the new header/priority/formatting without manual generated-file edits or runtime registration. |
| AC10 | Level 1 annotations are evaluated as a customization-gradient level | The app starts and renders generated views | No runtime override registry, custom component, slot registration, template file, custom format-provider registry, or DI-per-domain renderer is required. |
| AC11 | The Counter sample is used as a reference implementation | The sample domain is built and rendered | At least one simple metadata annotation and one format annotation demonstrate the adopter path, and tests cover the generated output. |
| AC12 | Level 1 annotation metadata crosses SourceTools boundaries | Parse, transform, and emit stages run | Header, description, priority, relative-time, and currency metadata survive through IR into generated output without tying the IR contract to a specific runtime component implementation. |

---

## Tasks / Subtasks

- [x] T1. Lock current Level 1 metadata behavior as explicit story-owned coverage (AC1, AC2, AC3)
  - [x] Keep `System.ComponentModel.DataAnnotations.DisplayAttribute` as the standard label attribute. Do not add a FrontComposer-specific display-name attribute.
  - [x] Keep `System.ComponentModel.DescriptionAttribute` and `DisplayAttribute.Description` feeding `PropertyModel.Description`.
  - [x] Preserve current precedence: `[Description]` beats `Display.Description`; `Display.Name` beats `CamelCaseHumanizer`; raw property name is the final fallback.
  - [x] Preserve existing accessible description propagation through `HeaderTooltip` / generated description metadata; do not create tooltip-only behavior that bypasses the current accessibility path.
  - [x] Preserve `ColumnPriorityAttribute` semantics: any signed int is accepted, lower is earlier, null materializes as `int.MaxValue`, collisions emit HFC1028 Information and fall back to declaration order.
  - [x] Document that Story 6-1 preserves existing `DisplayAttribute` localization behavior but does not add a new field-display resource system.
  - [x] Add or tighten tests around parse, transform, and emit so these behaviors are visibly owned by Story 6-1.

- [x] T2. Add Contracts attributes for new format hints (AC4, AC6, AC7, AC10)
  - [x] Add `RelativeTimeAttribute` under `src/Hexalith.FrontComposer.Contracts/Attributes/`.
  - [x] Add `CurrencyAttribute` under the same namespace.
  - [x] Use `[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]` for both.
  - [x] Keep attributes dependency-free and trim-friendly. Constructors should only capture primitive/string values.
  - [x] For `[RelativeTime]`, default the relative window to 7 days; allow a bounded integer override only if it stays simple and testable.
  - [x] For `[Currency]`, default to the current culture standard currency format. Defer ISO currency-code conversion, custom currency providers, and arbitrary format strings unless already supported by existing code without new runtime services.

- [x] T3. Extend Parse-stage IR without breaking existing equality/caching contracts (AC4, AC6, AC7)
  - [x] Extend `PropertyModel` with a small format-hint value, enum, or record that can represent default, relative-time, and currency.
  - [x] Parse `[RelativeTime]` and `[Currency]` in `AttributeParser.ParseProperty`.
  - [x] Detect mutually exclusive Level 1 format annotations on the same property; emit one deterministic warning and fall back to the existing default formatter rather than letting declaration order choose a winner.
  - [x] Include the new field in `PropertyModel.Equals` and `GetHashCode`; update `DomainModelCacheEqualityTests` if needed.
  - [x] Emit warning diagnostics for incompatible type usage. Reserve stable SourceTools IDs in the HFC10xx range, documenting them in `DiagnosticDescriptors`, `FcDiagnosticIds` if needed, and analyzer release notes.
  - [x] Diagnostics must include field, attribute, expected type family, actual type, fix guidance, docs link, and fallback behavior.
  - [x] Warnings must be fail-soft: keep generated code compiling, keep the column emitted, and use the prior default format rather than dropping the field.

- [x] T4. Extend Transform-stage column metadata (AC4, AC6, AC7)
  - [x] Extend `ColumnModel` to carry the Level 1 format override.
  - [x] Make `RazorModelTransform.GetFormatHint` choose:
    - `[Currency]` -> currency format for numeric categories only.
    - `[RelativeTime]` -> relative-time render mode for DateTime-like categories only.
    - No annotation -> existing defaults (`N0`, `N2`, `d`, `t`, `Humanize:30`, etc.).
    - Conflicting annotations -> existing default after the parse warning; do not encode conflict-resolution policy in emitters.
  - [x] Preserve stable column sorting by priority and declaration order. Format overrides must not affect column ordering.
  - [x] Add tests proving explicit `Display.Name` still controls headers when a format annotation is also present.
  - [x] Keep IR names UI-agnostic (`DisplayFormat`, `Header`, `Description`, `Priority` style). Do not introduce `Component`, `Renderer`, or runtime override terminology into the Level 1 metadata contract.

- [x] T5. Emit currency formatting through the existing numeric column path (AC6)
  - [x] Reuse `ColumnEmitter.EmitNumericColumn` rather than creating a second numeric component.
  - [x] Format non-null values with the .NET currency standard format string and `CultureInfo.CurrentCulture`.
  - [x] Keep `Class = "fc-col-numeric"` so existing right-alignment and DataGrid styling apply.
  - [x] Preserve sort behavior over the underlying numeric property, not the formatted string.
  - [x] Add EN/FR culture tests where the existing test harness can switch `CurrentCulture` safely and restore it in `finally` or an equivalent fixture; include negative values, zero, nullable values, and supported floating-point/numeric variants.

- [x] T6. Emit relative-time formatting deterministically (AC4, AC5)
  - [x] Prefer a small generated helper method or Shell helper that accepts the value and a `DateTimeOffset now` from an injected `TimeProvider`.
  - [x] Register or reuse `TimeProvider.System` in Shell service setup when missing; tests should inject a fake provider rather than relying on wall-clock time.
  - [x] Capture `now` once per generated render path before formatting rows so large virtualized grids do not show per-cell clock skew.
  - [x] Use fixed-width abbreviated labels for the relative window: examples such as `5m ago`, `3h ago`, `2d ago`. Keep copy concise and stable for DataGrid scanning.
  - [x] After 7 days, fall back to the same absolute date format used by unannotated DateTime columns.
  - [x] Respect nullable fallback with the existing dash representation.
  - [x] Cover `DateTimeOffset`, `DateTime`, nullable variants, UTC, Local, Unspecified, future timestamps, and boundary values with deterministic tests.
  - [x] Avoid per-cell timers in v1. Values may update on render/query refresh; continuous live ticking is out of scope.

- [x] T7. Preserve generated DataGrid and role-specific surfaces (AC1-AC7)
  - [x] Standard DataGrid, ActionQueue, StatusOverview, DetailRecord, and Timeline emit paths must agree on the same formatter when they render the annotated field.
  - [x] If a role-specific path cannot yet use the new formatter safely, document and test the intentional fallback rather than silently diverging.
  - [x] Do not alter badge mapping, unsupported placeholder emission, empty-state CTA behavior, or grouping semantics.

- [x] T8. Add Counter sample reference override (AC11)
  - [x] Add minimal Level 1 annotations to `samples/Counter/Counter.Domain/CounterProjection.cs`; recommended: `[Display(Name = "Last changed")]` plus `[RelativeTime]` on `LastUpdated`.
  - [x] Keep the sample small. Do not introduce Orders/TaskTracker solely for this story.
  - [x] Update snapshot or generated-output tests that assert Counter projection headers/formatting.
  - [x] Add a short comment only if needed to explain why the sample uses the annotation.

- [x] T9. Dev-loop and hot-reload evidence (AC9)
  - [x] Add focused tests proving generator output changes when an annotation is added, removed, or has a constructor argument changed between two compilations.
  - [x] If practical, add an integration note or test fixture documenting `dotnet watch` incremental rebuild behavior for attribute changes and the expected affected generated output count.
  - [x] Do not promise pure CLR/Razor hot reload for source-generator input changes. If a scenario needs restart/rebuild messaging, add it to Known Gaps with Story 6-6 ownership.

- [x] T10. Tests and verification (AC1-AC12)
  - [x] Contracts tests for `RelativeTimeAttribute`, `CurrencyAttribute`, and existing `ColumnPriorityAttribute` behavior.
  - [x] Parse tests for valid attributes, invalid-type warnings, diagnostic IDs/source spans, and precedence with `Display`/`Description`.
  - [x] Transform tests for `ColumnModel` metadata, display-name precedence, priority sorting, UI-agnostic IR naming, and fallback formatting.
  - [x] Emitter approval tests for currency and relative-time columns, including nullable cases.
  - [x] Shell/component tests for relative-time helper output and culture-sensitive currency formatting.
  - [x] Negative tests proving no runtime registry, template registration, DI-per-domain renderer, or custom component hook is introduced by Story 6-1.
  - [x] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [x] Targeted tests: Contracts attribute tests, SourceTools parse/transform/emit tests, and Shell formatting tests. Run full solution tests if the working tree is otherwise clean.

### Review Findings

_Code review run on 2026-04-29 over commit `248343a` (Blind Hunter + Edge Case Hunter + Acceptance Auditor). Triage: 1 decision-needed, 11 patches, 5 deferred, 7 dismissed._

- [x] [Review][Decision] F8 — `DateTimeKind.Unspecified` is silently treated as UTC in generated `FormatRelativeTime` — `RazorEmitter.cs:685` discard pattern coerces Unspecified to UTC. **Resolved → Option (a):** keep current "treat Unspecified as UTC" behavior. Rationale: the generated comparison frame is already UTC-canonical (`utcNow = now.ToUniversalTime()`); .NET server-side persistence (EF Core, `System.Text.Json` without `Z`) typically stores UTC instants with Kind=Unspecified; option (b) would silently flip behavior for adopters whose data is already UTC-normalized; option (c) imposes per-cell branching on the hot render path that contradicts AC4's expectation that all three Kinds render successfully. Doc note added inline above the helper in `RazorEmitter.EmitFormatters`; `Level1FormatRuntimeTests.RelativeTime_DateTimeUnspecifiedKind_TreatedAsUtc` pins the contract.

- [x] [Review][Patch] F1 — Generator silently accepts out-of-range `[RelativeTime(N)]` constructor argument [`src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs:572-575`] — the runtime `RelativeTimeAttribute` ctor enforces `1..365`, but `ParseDisplayFormat` reads `attr.ConstructorArguments[0].Value is int days` with no bounds check; `RelativeTimeWindowDays = ... ?? 7` only guards null, so `0`/negative/`>365` flow into IR and the emitted literal. With `0` or negative the comparison `distance > TimeSpan.FromDays(N)` is degenerate. Fix: emit HFC1032 + fall back to default 7 (matches D5 fail-soft policy). Bundles E4 (non-int constructor argument) and A9 (parse-stage validation untested).

- [x] [Review][Patch] F2 — Generated `FormatRelativeTime(DateTime, ...)` can throw `ArgumentOutOfRangeException`/`OverflowException` on `DateTime.MinValue` / `DateTime.MaxValue` and Local-kind extremes [`src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:683-695`] — `new DateTimeOffset(value)` for Local-kind extremes overflows the offset range, and `utcNow - timestamp` arithmetic on extremes throws. AC fail-soft contract requires column to keep rendering. Fix: wrap the conversion + subtraction in a guard (or pre-clamp) that falls back to absolute "d" format on overflow.

- [x] [Review][Patch] F6 — Misleading nested-ternary indentation in `RoleDateTimeExpression` [`src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs:411-419`] — visual structure does not match logical right-associative parsing; future "format hint" additions will be inserted at the wrong nesting level. Fix: extract to a `private static string BuildDateTimeExpression(...)` helper or use explicit parentheses.

- [x] [Review][Patch] F12 — `TestSources.cs` lost the leading 4-space class-member indent for `CounterProjectionSource` [`tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/TestSources.cs:333`] — only this constant lacks the 4-space indent that every other `internal const string` in the file uses. Cosmetic but breaks the file's consistent style.

- [x] [Review][Patch] F17 — Currency runtime culture-restoration tests are missing (T5 / Hardening Addendum bullet 3) [`tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/Level1FormatEmitterTests.cs`] — added tests only `ShouldContain("...ToString(\"C\", CultureInfo.CurrentCulture)")`. T5 mandates "EN/FR culture tests where the existing test harness can switch `CurrentCulture` safely and restore it in `finally` … include negative values, zero, nullable values, and supported floating-point/numeric variants." Add Shell or Contracts-level tests that actually run the emitted format string against `en-US` and `fr-FR` cultures with `try/finally` restoration.

- [x] [Review][Patch] F18 — Relative-time deterministic clock tests are missing for UTC / Local / Unspecified / future / boundary values (T6) — diff has zero tests that instantiate a fake `TimeProvider` and assert "5m ago" / "3h ago" / "2d ago" / future / 7-day-boundary outputs. Add a test class that hosts `FormatRelativeTime` (or its source-equivalent) and asserts each AC4/AC5 case deterministically. Coordinates with F8 decision (Unspecified semantics).

- [x] [Review][Patch] F19 — `ColumnPriority` regression coverage promised by T1/T10 is absent [`tests/Hexalith.FrontComposer.Contracts.Tests/Attributes/Level1FormatAttributeTests.cs`] — story-owned coverage for `ColumnPriorityAttribute` constructor semantics, lower-first ordering, null materializing as `int.MaxValue`, and HFC1028 collision handling was promised but not added. Add the regression tests in the new `Level1FormatAttributeTests` (or a sibling file) so T1's "lock current Level 1 metadata behavior" pledge is enforceable.

- [x] [Review][Patch] F20 — `DomainModelCacheEqualityTests` not updated for `DisplayFormat` / `RelativeTimeWindowDays` (T3) [`tests/Hexalith.FrontComposer.SourceTools.Tests/Incremental/DomainModelCacheEqualityTests.cs`] — the new IR fields are in `PropertyModel.Equals`/`GetHashCode` but no cache-invalidation test guards them. Add cache-equality cases that assert two `PropertyModel` instances differing only in `DisplayFormat` or `RelativeTimeWindowDays` are NOT equal and have differing hashes.

- [x] [Review][Patch] F21 — Two-compilation dev-loop tests for annotation add/remove/constructor-argument-change (T9 / AC9) — story marks T9 `[x]` but no test runs the generator twice to assert generated output changes. Add an incremental-rebuild test that compiles a projection without `[RelativeTime]`, captures emitted source, recompiles with `[RelativeTime(14)]`, and asserts the literal window changed. Repeat for add/remove.

- [x] [Review][Patch] F23 — Cross-role formatter agreement asserted but not tested (T7) — `Level1FormatEmitterTests` only covers the standard DataGrid path. T7 requires "Standard DataGrid, ActionQueue, StatusOverview, DetailRecord, and Timeline emit paths must agree on the same formatter when they render the annotated field." Add at least one `[Currency]` and one `[RelativeTime]` test per role to assert the formatter expression appears in each rendered body, or document the intentional fallback if a role cannot consume it.

- [x] [Review][Patch] F24 — `[Description]` precedence over `Display.Description`, and `Display.Name` over `CamelCaseHumanizer`, are not story-owned (T1 / T10) [`tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/Level1FormatAnnotationParserTests.cs`] — T1 explicitly re-classifies these as Story-6-1-owned coverage. Add the precedence assertions in Story 6-1's parser test file.

- [x] [Review][Defer] F7 — `Location.None` fallback emits HFC1032 with line/column 0 [`src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs:648`] — pre-existing pattern across multiple parser methods; not introduced by this story.

- [x] [Review][Defer] F13 — Mutually-exclusive conflict diagnostic short-circuits per-attribute type-incompat diagnostics [`src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs:586-597`] — two-pass authoring loop is acceptable and the deterministic conflict warning is the higher-value signal.

- [x] [Review][Defer] F16 — `(int)Math.Floor` boundary at exact `1d`/`1h` could flicker on floating-point edge [`src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:713`] — theoretical; address with integer-Ticks comparison only if a real flicker is reported.

- [x] [Review][Defer] F22 — "No runtime registry, template registration, DI-per-domain renderer, custom component hook" negative tests not added (T10) — low ROI; the absence is verifiable via static code review and bounded by Level 2-4 stories that would have to add such surfaces.

- [x] [Review][Defer] F26 — Counter sample integration test only string-greps for `FormatRelativeTime` and the verified.txt snapshot still shows absolute date (AC11 / T8) — adding a render-time assertion requires injecting a near-now `TimeProvider` into the bUnit context and updating verified.txt; defer in favor of source-level coverage.

_Dismissed (7) — recorded for traceability:_ F3 (sort uses raw `BuildSortExpression`, not Property), F4 (relative-time emits the same attribute set as default DateTime path), F5 (`DateOnly` exclusion is intentional per Type Compatibility Matrix), F9 (suffix allocation cosmetic), F10 (Party-Mode Review section IS populated), F11 (`CurrencyAttribute` empty marker is intentional per scope guardrails), F15 (Invariant digits inside the relative window are intentional fixed-width labels per Hardening Addendum).

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

### Advanced Elicitation Hardening Addendum

Advanced elicitation on 2026-04-26 applied two robustness batches after the party-mode review. These clarifications are binding for `bmad-dev-story` and are meant to harden edge cases without expanding Level 1 beyond compile-time metadata:

- Treat mutually exclusive format annotations as invalid metadata, not precedence. A property with both `[RelativeTime]` and `[Currency]` must emit one deterministic warning and fall back to the existing default formatter.
- Keep time deterministic at the render boundary. Generated code or the Shell helper should capture one `TimeProvider` value per render path and pass it into per-cell formatting.
- Keep culture-sensitive tests isolated. Currency tests that mutate `CultureInfo.CurrentCulture` must restore the original culture and avoid leaking process-wide state into unrelated tests.
- Keep diagnostics teachable and bounded. Invalid-type and conflicting-annotation warnings should share the same message shape: what happened, expected family or exclusivity rule, actual metadata/type, fix, docs link, and fail-soft fallback.
- Keep adopter evidence minimal. The Counter sample demonstrates the happy path; invalid metadata and conflict behavior belong in focused SourceTools tests, not in sample code.

### Type Compatibility Matrix

| Attribute | Valid source types | Invalid-source behavior | Explicitly out of scope |
| --- | --- | --- | --- |
| `[Display(Name = ...)]` | Any projection property | Existing display metadata fallback applies. | New FrontComposer-specific display-name attribute. |
| `[Display(Description = ...)]` / `[Description(...)]` | Any projection property | Existing description fallback applies. | New tooltip-only description surface or custom field-resource system. |
| `[ColumnPriority(...)]` | Any projection property | Existing HFC1028 collision behavior remains Information-level and stable. | Replacing priority with `DisplayAttribute.Order`. |
| `[RelativeTime]` | `DateTime`, `DateTimeOffset`, and nullable variants | Stable warning diagnostic; generated code compiles; column remains emitted with existing date/time formatting. | Per-cell live ticking, custom grammar/localized relative phrases, runtime date formatter registry. |
| `[Currency]` | `decimal`, `double`, `float`, and nullable variants | Stable warning diagnostic; generated code compiles; column remains emitted with existing numeric formatting. | Currency conversion, ISO currency-provider services, arbitrary runtime format providers. |
| `[RelativeTime]` + `[Currency]` on one property | None; they are mutually exclusive format hints. | Stable warning diagnostic; generated code compiles; column remains emitted with the existing default formatter for the source type. | Declaration-order precedence or combining date and numeric render modes. |

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
| D11 | Mutually exclusive format annotations warn and fall back instead of choosing precedence. | Attribute order should not create hidden rendering policy, and fail-soft output preserves the adopter's generated UI. | Last attribute wins; first attribute wins; build error. |
| D12 | Relative-time formatting captures `now` once per generated render path. | Large virtualized grids should not show inconsistent labels solely because cells formatted at slightly different instants. | Call `TimeProvider.GetUtcNow()` per cell; add live ticking timers. |

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
- For conflicting annotations: one field with both `[RelativeTime]` and `[Currency]` must produce one deterministic warning, keep generated output compiling, and use the pre-existing default formatter.
- For invalid attributes: assert warning ID, message includes What/Expected/Got/Fix/DocsLink, and generated output still exists.
- Approval snapshots should only change where the annotation changes emitted output.
- If full solution tests are expensive because unrelated work is dirty, run targeted Contracts/SourceTools/Shell tests and document the limitation.

### Scope Guardrails

Do not implement these in Story 6-1:

- Runtime override registry, DI-per-domain rendering, or generated component lookup by domain.
- Level 2 typed Razor templates.
- Level 3 slot-level field replacement or `IOverrideRegistry`.
- Level 4 full component replacement.
- `FcDevModeOverlay`, starter template generation, or clipboard support.
- Build-time version compatibility validation for override contracts.
- Runtime error boundaries for faulty overrides.
- A custom localization/resource system for field display names.
- Per-cell live ticking timers for relative time.
- Currency conversion, ISO currency-provider services, or arbitrary runtime format-provider registries.
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

GPT-5 Codex

### Debug Log References

- `dotnet test tests\Hexalith.FrontComposer.Contracts.Tests\Hexalith.FrontComposer.Contracts.Tests.csproj --filter FullyQualifiedName~Level1FormatAttributeTests --no-restore /p:UseSharedCompilation=false`
- `dotnet test tests\Hexalith.FrontComposer.SourceTools.Tests\Hexalith.FrontComposer.SourceTools.Tests.csproj --filter "FullyQualifiedName~Level1Format" --no-restore /p:UseSharedCompilation=false`
- `dotnet test tests\Hexalith.FrontComposer.SourceTools.Tests\Hexalith.FrontComposer.SourceTools.Tests.csproj --filter "FullyQualifiedName~CounterDomainIntegrationTests|FullyQualifiedName~CounterProjectionApprovalTests" --no-restore /p:UseSharedCompilation=false`
- `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --filter FullyQualifiedName~CounterProjectionView_LoadedState_RendersColumnsAndFormatting --no-restore /p:UseSharedCompilation=false`
- `dotnet test tests\Hexalith.FrontComposer.SourceTools.Tests\Hexalith.FrontComposer.SourceTools.Tests.csproj --filter FullyQualifiedName~AttributeParserTests --no-restore /p:UseSharedCompilation=false`
- `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`
- `dotnet test Hexalith.FrontComposer.sln --no-build /p:UseSharedCompilation=false`

### Completion Notes List

- Added dependency-free `[RelativeTime]` and `[Currency]` Contracts attributes with constructor and usage tests.
- Extended SourceTools parse, transform, diagnostics, and emitted Razor metadata for Level 1 format hints while preserving existing display, description, priority, sort, filter, and fail-soft fallback behavior.
- Added HFC1032 for invalid or conflicting Level 1 format annotations with teachable warning messages and generated-code fallback.
- Emitted currency through the existing numeric column path with `CultureInfo.CurrentCulture` and relative time through deterministic generated helpers that capture `TimeProvider.GetUtcNow()` once per render path.
- Updated the Counter sample and generated-output tests to demonstrate `[Display(Name = "Last changed")]` plus `[RelativeTime]`.
- Full solution validation passed: Contracts 99/0/0, SourceTools 505/0/0, Shell 1258/0/3, Bench 2/0/0.

### Party-Mode Review

- Date: 2026-04-26T08:44:07+02:00
- Selected story key: 6-1-level-1-annotation-overrides
- Command/skill invocation used: `/bmad-party-mode 6-1-level-1-annotation-overrides; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - Keep the compile-time Level 1 boundary explicit: no runtime registry, template registration, custom components, custom format-provider registry, or DI-per-domain renderer.
  - Make `RelativeTime` and `Currency` contracts testable with explicit valid type families, nullable behavior, deterministic diagnostics, and fail-soft fallback requirements.
  - Preserve accessibility/localization expectations by using the current description metadata path and `CultureInfo.CurrentCulture`, while deferring any new field-resource system.
  - Add parse -> IR -> transform -> emit evidence so metadata survival is not only asserted through final markup snapshots.
  - Tighten dev-loop evidence to cover add/remove/constructor-argument annotation changes and affected generated output.
- Changes applied:
  - Added adopter job framing and expanded ACs from AC1-AC10 to AC1-AC12.
  - Added deterministic fallback/diagnostic requirements, culture/time boundaries, and accessibility/localization preservation language.
  - Added a type compatibility matrix and Level 1 non-goals for runtime extension points and currency-provider scope.
  - Tightened tasks for diagnostics, IR naming, Counter sample evidence, incremental rebuild checks, and negative tests.
- Findings deferred:
  - Robustness/security edge cases remain for the later advanced-elicitation pass per L08.
  - Pure source-generator hot reload messaging remains owned by Story 6-6.
  - Custom localized relative-time grammar, currency conversion, ISO provider services, Level 2+ templates/slots/replacements, and field-resource systems remain out of scope.
- Final recommendation: ready-for-dev

### Advanced Elicitation

- Date: 2026-04-26T09:32:37+02:00
- Selected story key: 6-1-level-1-annotation-overrides
- Command/skill invocation used: `/bmad-advanced-elicitation 6-1-level-1-annotation-overrides`
- Batch 1 method names: Red Team vs Blue Team; Security Audit Personas; Failure Mode Analysis; Pre-mortem Analysis; Self-Consistency Validation
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; First Principles Analysis; Socratic Questioning; Occam's Razor Application; Comparative Analysis Matrix
- Findings summary:
  - The story already preserved the Level 1 compile-time boundary, but conflicting format annotations needed explicit fail-soft behavior so declaration order cannot become hidden policy.
  - Relative-time formatting was deterministic in tests, but large rendered grids still needed a one-`now` capture rule to avoid per-cell clock skew.
  - Currency behavior correctly uses `CultureInfo.CurrentCulture`, but the test strategy needed explicit culture restoration to avoid cross-test leakage.
  - Invalid metadata diagnostics needed one shared teachable message shape across wrong-type and conflicting-annotation cases.
  - Counter sample evidence should stay happy-path only; negative/conflict behavior belongs in focused SourceTools tests.
- Changes applied:
  - Expanded AC7 to cover mutually exclusive format annotations.
  - Added the Advanced Elicitation Hardening Addendum.
  - Added a conflict row to the Type Compatibility Matrix and binding decisions D11-D12.
  - Tightened T3, T4, T5, T6, and Testing Standards for annotation conflicts, render-time clock capture, and culture isolation.
- Findings deferred:
  - No product-scope, architecture-policy, or cross-story contract changes were applied.
  - Pure source-generator hot reload messaging remains owned by Story 6-6.
  - Localized relative-time grammar, currency conversion/provider services, runtime registries, Level 2+ templates, and sample-domain expansion remain out of scope.
- Final recommendation: ready-for-dev

### File List

- `_bmad-output/implementation-artifacts/6-1-level-1-annotation-overrides.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `samples/Counter/Counter.Domain/CounterProjection.cs`
- `src/Hexalith.FrontComposer.Contracts/Attributes/CurrencyAttribute.cs`
- `src/Hexalith.FrontComposer.Contracts/Attributes/RelativeTimeAttribute.cs`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/ColumnEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`
- `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs`
- `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/ColumnModel.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModelTransform.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Attributes/Level1FormatAttributeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.CounterProjectionView_LoadedState_RendersColumnsAndFormatting.verified.txt`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/Level1FormatEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/CounterDomainIntegrationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/Level1FormatAnnotationParserTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/TestSources.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/Level1FormatTransformTests.cs`

### Change Log

- 2026-04-29: Implemented Story 6-1 Level 1 annotation overrides and moved story to review after full solution validation.
- 2026-04-30: Code review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) on commit `248343a`. Triage: 1 decision-needed, 11 patches, 5 deferred, 7 dismissed. F8 resolved to keep Unspecified-as-UTC with documentation + deterministic tests. All 11 patches applied: F1 (HFC1032 for out-of-range / non-int `[RelativeTime(N)]` window with default fallback), F2 (try/catch around `FormatRelativeTime` Local-kind extreme conversion + delta arithmetic — fail-soft to absolute "d"), F6 (extracted nested DateTime ternary into `BuildDateTimeExpression` helper), F12 (TestSources class-member indent), F17 (Currency culture-restoration tests EN/FR with try/finally), F18 (relative-time deterministic tests for UTC/Local/Unspecified/future/boundary via Level1FormatRuntimeTests), F19 (story-owned ColumnPriority regression in Contracts + transform tests), F20 (DomainModelCacheEqualityTests for DisplayFormat + RelativeTimeWindowDays), F21 (two-compilation dev-loop tests proving annotation add/remove/argument-change invalidates PropertyModel), F23 (cross-role formatter agreement tests + documented Timeline-omits-Currency intentional fallback), F24 (Display.Name-over-humanizer + Description precedence tests). Validation: `dotnet build` clean (0 warnings); `dotnet test Hexalith.FrontComposer.sln` => Contracts 105/0/0, SourceTools 538/0/0, Shell 1289/0/0, Bench 2/0/0. Story status review → done.
