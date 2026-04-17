# Story 1.5: Source Generator - Transform & Emit Stages

Status: done

## Story

As a developer,
I want the source generator to transform parsed domain models into Blazor DataGrid components with field type inference, label resolution, and data formatting,
So that annotating a type with [Projection] produces a fully rendered, correctly formatted DataGrid at compile time.

## Acceptance Criteria

### AC1: Transform Stage Produces Output Models

**Given** a DomainModel IR from the Parse stage
**When** the Transform stage runs
**Then** it produces output models for: a Razor DataGrid component, Fluxor feature/actions/reducers, and a BoundedContext domain registration
**And** Fluxor types are emitted as fully-qualified name strings (no Fluxor dependency in SourceTools)

### AC2: Emit Stage Generates Named Files to Correct Output Path

**Given** output models from Transform
**When** the Emit stage runs
**Then** generated files use namespace-qualified hint names: `{Namespace}.{TypeName}.g.razor.cs`, `{Namespace}.{TypeName}Feature.g.cs`, `{Namespace}.{TypeName}Actions.g.cs`, `{Namespace}.{TypeName}Reducers.g.cs`, `{Namespace}.{TypeName}Registration.g.cs` (global-namespace types omit the prefix)
**And** all generated files go to `obj/{Config}/{TFM}/generated/HexalithFrontComposer/`
**And** namespaces match folder paths exactly

### AC3: DataGrid Renders Fields with Correct Type Inference & Formatting

**Given** a [Projection]-annotated type with various .NET property types
**When** the generated DataGrid renders
**Then** string fields render as text columns
**And** int/long/decimal fields render as right-aligned locale-formatted columns
**And** bool fields render as "Yes"/"No" text
**And** DateTime/DateTimeOffset fields render as short date per CultureInfo
**And** enum fields render as humanized labels (max 30 chars with ellipsis)
**And** null values render as em dash (\u2014) in all columns
**And** column headers use the label resolution chain: `[Display(Name)]` > humanized CamelCase > raw field name

### AC4: BoundedContext Domain Registration Generation

**Given** a type annotated with `[BoundedContext("Orders")]`
**When** the generator runs
**Then** it produces a domain registration grouping all projections under the "Orders" navigation section
**And** bounded context display labels use the `[BoundedContext(name, DisplayLabel = "...")]` optional parameter for domain-language overrides (e.g., `[BoundedContext("Orders", DisplayLabel = "Commandes")]`)

### AC5: Snapshot Testing with Semantic DOM Comparison

**Given** the Emit stage output
**When** snapshot tests run
**Then** golden output (`.approved.cs` / `.verified.txt`) matches expected generated source structure

## Tasks / Subtasks

- [x] Task 1: Create Transform stage output models (AC: #1)
  - [x] 1.1 Create `Transforms/RazorModel.cs` -- output model for Razor DataGrid component generation. Fields: TypeName, Namespace, BoundedContext, Columns (EquatableArray\<ColumnModel\>). ColumnModel: PropertyName, Header, TypeCategory (Text/Numeric/Boolean/DateTime/Enum/Collection/Unsupported), FormatHint, IsNullable, BadgeMappings
  - [x] 1.2 Create `Transforms/FluxorModel.cs` -- output model for Fluxor feature/actions. Fields: TypeName, Namespace, StateName (`{TypeName}State`), FeatureName (`{TypeName}Feature`), InitialState (IsLoading=false, Items=null, Error=null)
  - [x] 1.3 Create `Transforms/RegistrationModel.cs` -- output model for domain registration. Fields: BoundedContext, Projections (list of type names), DisplayLabel (optional override)
  - [x] 1.4 All output models MUST implement `IEquatable<T>` with value-based equality (Roslyn incremental caching requirement). Use `EquatableArray<T>` for collections. Follow same pattern as DomainModel/PropertyModel in `Parsing/DomainModel.cs`

- [x] Task 2: Implement Transform pure functions (AC: #1, #3)
  - [x] 2.1 Create `Transforms/RazorModelTransform.cs` -- static pure function: `static RazorModel Transform(DomainModel model)`
    - Map each PropertyModel to ColumnModel using type inference rules:
      - `String` -> TypeCategory.Text, no format hint
      - `Int32`/`Int64` -> TypeCategory.Numeric, format hint `"N0"` (no decimals)
      - `Decimal`/`Double`/`Single` -> TypeCategory.Numeric, format hint `"N2"` (two decimals)
      - `Boolean` -> TypeCategory.Boolean, format hint "Yes/No"
      - `DateTime`/`DateTimeOffset`/`DateOnly` -> TypeCategory.DateTime, format hint "d" (short date)
      - `TimeOnly` -> TypeCategory.DateTime, format hint "t" (short time)
      - `Enum` -> TypeCategory.Enum, format hint "Humanize:30" (max 30 chars with ellipsis)
      - `Guid` -> TypeCategory.Text, format hint "Truncate:8" (8-char monospace, copy-on-click)
      - `Collection` -> TypeCategory.Collection, format hint "Count"
      - Unsupported (`IsUnsupported=true`) -> TypeCategory.Unsupported, skip column generation
    - Apply label resolution chain for column Header:
      1. `PropertyModel.DisplayName` (from `[Display(Name)]`)
      2. Humanize CamelCase (`OrderDate` -> `"Order Date"`)
      3. Raw property name (fallback)
    - Skip properties with `IsUnsupported=true`
  - [x] 2.2 Create `Transforms/FluxorModelTransform.cs` -- static pure function: `static FluxorModel Transform(DomainModel model)`
    - Derive state type name: `{TypeName}State`
    - Derive feature type name: `{TypeName}Feature`
    - Initial state: `IsLoading=false, Items=null, Error=null`
  - [x] 2.3 Create `Transforms/RegistrationModelTransform.cs` -- static pure function: `static RegistrationModel Transform(DomainModel model)`
    - Group by BoundedContext (defaults to Namespace last segment if no `[BoundedContext]`)
    - Include projection type info for registration
  - [x] 2.4 Create `Transforms/CamelCaseHumanizer.cs` -- static utility: `static string Humanize(string camelCase)` -- insert spaces before uppercase letters, handle acronyms. Rules:
    - Insert space before each uppercase letter preceded by a lowercase letter (`"OrderDate"` -> `"Order Date"`)
    - Keep consecutive uppercase letters together until a lowercase follows (`"XMLParser"` -> `"XML Parser"`)
    - Trailing uppercase suffix stays grouped (`"OrderID"` -> `"Order ID"`)
    - Only operates on ASCII A-Z transitions. Non-ASCII characters, digits, and underscores are preserved as-is
    - Empty string returns empty string, null returns null
    - All-caps input stays unchanged (`"ORDER"` -> `"ORDER"`)
    - Leading lowercase is capitalized (`"firstName"` -> `"First Name"`)
    - Digits are treated as non-letter boundaries (`"Order2Name"` -> `"Order2 Name"`)

- [x] Task 3: Implement Emit stage emitters (AC: #2, #3, #4)
  - [x] 3.1 Create `Emitters/RazorEmitter.cs` -- static function: `static string Emit(RazorModel model)` -- generates `{TypeName}.g.razor.cs`:
    - Header comment: `// Generated by FrontComposer source generator \n// Do not edit directly`
    - `#nullable enable` at top
    - Required using statements emitted at file scope:
      ```csharp
      using System;
      using System.Collections.Generic;
      using System.Globalization;
      using Fluxor;
      using Microsoft.AspNetCore.Components;
      using Microsoft.FluentUI.AspNetCore.Components;
      ```
    - Namespace: consumer's namespace (from DomainModel.Namespace)
    - Partial class inherits `ComponentBase`, implements `IDisposable`
    - `[Inject] private IState<{TypeName}State> {TypeName}State { get; set; } = default!;`
    - `OnInitialized()`: subscribe `StateChanged += OnStateChanged`
    - `OnStateChanged`: `InvokeAsync(StateHasChanged)`
    - `Dispose()`: unsubscribe `StateChanged -= OnStateChanged`
    - Render method emitting DataGrid column markup per ColumnModel. Example column pattern:
      ```csharp
      // Text column example:
      builder.OpenComponent<PropertyColumn<{TypeName}, string?>>(seq++);
      builder.AddAttribute(seq++, "Title", "{ColumnHeader}");
      builder.AddAttribute(seq++, "Property", (Func<{TypeName}, string?>)(x => x.{PropertyName} ?? "\u2014"));
      builder.CloseComponent();
      ```
    - Column rendering rules:
      - Text: `x.{PropertyName} ?? "\u2014"` (em dash for null)
      - Numeric: `x.{PropertyName}?.ToString("{FormatHint}", CultureInfo.CurrentCulture) ?? "\u2014"` with right-alignment via CSS class
      - Boolean: `x.{PropertyName} is null ? "\u2014" : x.{PropertyName}.Value ? "Yes" : "No"`
      - DateTime: `x.{PropertyName}?.ToString("d", CultureInfo.CurrentCulture) ?? "\u2014"`
      - Enum: humanize + truncate to 30 chars with ellipsis, null -> em dash
      - Guid: `x.{PropertyName}?.ToString("N")[..8] ?? "\u2014"`
      - Collection: `x.{PropertyName}?.Count.ToString() ?? "\u2014"` + " items" suffix
    - Three-state rendering pattern: Loading skeleton, Empty state, DataGrid
    - **BadgeMappings:** Present in ColumnModel but NOT rendered in this story. Reserved for Story 2.x badge rendering. Emit a `// TODO: Badge rendering (Story 2.x)` comment in generated code where badge columns would appear
  - [x] 3.2 Create `Emitters/FluxorFeatureEmitter.cs` -- static function: `static string Emit(FluxorModel model)` -- generates `{TypeName}Feature.g.cs`:
    - State record: `record {TypeName}State(bool IsLoading, IReadOnlyList<{TypeName}>? Items, string? Error)`
    - Emit `using System.Collections.Generic;` in generated file for `IReadOnlyList<T>`
    - Feature class: `class {TypeName}Feature : Feature<{TypeName}State>` with `GetName()` and `GetInitialState()`
    - All Fluxor types emitted as fully-qualified string references (`Fluxor.Feature<T>`, `Fluxor.FeatureStateAttribute`, etc.)
    - **CRITICAL:** SourceTools targets netstandard2.0 -- no Fluxor.dll reference. All Fluxor base types written as string identifiers
  - [x] 3.3 Create `Emitters/FluxorActionsEmitter.cs` -- static function: `static string Emit(FluxorModel model)` -- generates `{TypeName}Actions.g.cs`:
    - Emits projection-level data loading actions with matching reducer stubs:
      - `record {TypeName}LoadRequestedAction()` -- triggers data fetch
      - `record {TypeName}LoadedAction(IReadOnlyList<{TypeName}> Items)` -- data arrived
      - `record {TypeName}LoadFailedAction(string Error)` -- fetch failed
    - Emit corresponding static reducer class `{TypeName}Reducers` with `[ReducerMethod]` methods:
      - `LoadRequested` -> `state with { IsLoading = true, Error = null }`
      - `Loaded` -> `state with { IsLoading = false, Items = action.Items, Error = null }`
      - `LoadFailed` -> `state with { IsLoading = false, Error = action.Error }`
    - Action naming convention: always past-tense
    - Reducers follow static method pattern from Story 1.3 (`ThemeReducers` as reference)
    - All Fluxor types (`[ReducerMethod]`, `Feature<T>`) emitted as fully-qualified strings
    - **Effects are NOT generated in this story.** Actions/reducers handle state transitions but no Effect wires data loading (e.g., HTTP fetch, SignalR subscription). Effects come from Story 5.x (EventStore/SignalR integration) or are hand-written by the consumer. Generated code compiles and runs standalone -- state starts at initial values until an Effect or manual dispatch provides data.
  - [x] 3.4 Create `Emitters/RegistrationEmitter.cs` -- static function: `static string Emit(RegistrationModel model)` -- generates `{TypeName}Registration.g.cs`:
    - Emits a partial static class `{BoundedContext}DomainRegistration` with a registration method for this specific projection
    - Each projection contributes its own partial class member -- Roslyn merges partial classes at compile time
    - This avoids duplicate hint name crash when multiple projections share the same `[BoundedContext]`
    - Supports optional display label override
    - When no `[BoundedContext]`, uses namespace last segment as the class name
  - [x] 3.5 All emitted code must:
    - Use 4-space indentation
    - Include `#nullable enable` pragma
    - Include XML `/// <summary>` doc comments on public types
    - Be deterministic (same input -> same output byte-for-byte)
    - Parse successfully as valid C# (verify in tests)

- [x] Task 4: Wire Transform + Emit into FrontComposerGenerator (AC: #1, #2)
  - [x] 4.1 Replace placeholder in `FrontComposerGenerator.cs` lines 42-46:
    ```csharp
    if (result.Model is not null)
    {
        // Transform
        var razorModel = RazorModelTransform.Transform(result.Model);
        var fluxorModel = FluxorModelTransform.Transform(result.Model);
        var registrationModel = RegistrationModelTransform.Transform(result.Model);
        
        // Emit
        spc.AddSource($"{result.Model.TypeName}.g.razor.cs", RazorEmitter.Emit(razorModel));
        spc.AddSource($"{result.Model.TypeName}Feature.g.cs", FluxorFeatureEmitter.Emit(fluxorModel));
        spc.AddSource($"{result.Model.TypeName}Actions.g.cs", FluxorActionsEmitter.EmitActions(fluxorModel));
        spc.AddSource($"{result.Model.TypeName}Reducers.g.cs", FluxorActionsEmitter.EmitReducers(fluxorModel));
        // Each projection emits its own partial registration contribution
        // Unique hint name per type prevents duplicate key crash when multiple projections share a BoundedContext
        spc.AddSource($"{result.Model.TypeName}Registration.g.cs", 
            RegistrationEmitter.Emit(registrationModel));
    }
    ```
  - [x] 4.2 Add `using` statements for `Hexalith.FrontComposer.SourceTools.Transforms` and `Hexalith.FrontComposer.SourceTools.Emitters`
  - [x] 4.3 Add HFC1001 diagnostic descriptor to `DiagnosticDescriptors.cs` -- Warning: "No [Command] or [Projection] types found in compilation"

- [x] Task 5: Write Transform unit tests (AC: #1, #3, #5)
  - [x] 5.1 Create `tests/.../Transforms/RazorModelTransformTests.cs`:
    - Test type inference for ALL supported types (String, Int32, Int64, Decimal, Double, Single, Boolean, DateTime, DateTimeOffset, DateOnly, TimeOnly, Guid, Enum, Collection) -- 14 tests minimum
    - Test nullable variants set IsNullable flag correctly
    - Test label resolution: DisplayName takes priority over humanized name
    - Test humanized CamelCase: `"OrderDate"` -> `"Order Date"`, `"XMLParser"` -> `"XML Parser"`
    - Test unsupported properties are skipped (not included in columns)
  - [x] 5.2 Create `tests/.../Transforms/FluxorModelTransformTests.cs`:
    - Test state name derivation: `"OrderProjection"` -> `"OrderProjectionState"`
    - Test feature name derivation: `"OrderProjection"` -> `"OrderProjectionFeature"`
    - Test initial state values (IsLoading=false, Items=null, Error=null)
  - [x] 5.3 Create `tests/.../Transforms/RegistrationModelTransformTests.cs`:
    - Test BoundedContext extraction
    - Test fallback to namespace last segment when no BoundedContext
    - Test projection listing
  - [x] 5.4 Create `tests/.../Transforms/CamelCaseHumanizerTests.cs` (15+ cases):
    - `"OrderDate"` -> `"Order Date"` (basic camelCase)
    - `"XMLParser"` -> `"XML Parser"` (consecutive caps + lowercase)
    - `"OrderID"` -> `"Order ID"` (trailing acronym)
    - `"Id"` -> `"Id"` (single word, no change)
    - `"firstName"` -> `"First Name"` (leading lowercase capitalized)
    - `""` -> `""` (empty string)
    - `null` -> `null` (null input)
    - `"ORDER"` -> `"ORDER"` (all-caps unchanged)
    - `"Order2Name"` -> `"Order2 Name"` (digit boundary)
    - `"OrderIDs"` -> `"Order IDs"` (trailing acronym with plural)
    - `"HTMLToJSON"` -> `"HTML To JSON"` (multiple acronyms)
    - `"a"` -> `"A"` (single lowercase letter)
    - `"AB"` -> `"AB"` (two-char acronym)
    - `"getHTTPResponse"` -> `"Get HTTP Response"` (leading lower + acronym)
    - `"VeryLongEnumMemberNameThatExceedsThirtyCharacterLimit"` -> humanizes correctly (long input)
  - [x] 5.5 Additional edge case tests for RazorModelTransformTests:
    - Nullable `int?` property -> ColumnModel.IsNullable = true
    - Nullable `string?` (annotated) vs `string` (reference-type default) -> both set IsNullable correctly per IR
    - Mixed supported/unsupported: 5 supported + 2 unsupported properties -> exactly 5 columns in output
    - Label resolution fallback chain: test all 3 steps in priority order (DisplayName wins over humanized, humanized wins over raw)
    - Enum with format hint "Humanize:30" -> test that truncation hint is correctly set for enum columns
    - Null collection property -> ColumnModel.IsNullable = true, TypeCategory.Collection
    - No BoundedContext attribute -> RegistrationModelTransform defaults to namespace last segment
  - [x] 5.6 All transform tests use DomainModel IR directly (no compilation context needed) -- pure function testing, fast execution

- [x] Task 6: Write Emit snapshot tests (AC: #2, #5)
  - [x] 6.1 Create `tests/.../Emitters/RazorEmitterTests.cs`:
    - Snapshot test: basic projection with string, int, bool, DateTime properties
    - Snapshot test: projection with nullable properties (em dash rendering)
    - Snapshot test: projection with DisplayName overrides
    - Snapshot test: projection with enum and badge mappings
    - Snapshot test: projection with Guid truncation
    - All snapshots stored in `Snapshots/` folder as `.verified.txt` files using Verify framework
  - [x] 6.2 Create `tests/.../Emitters/FluxorEmitterTests.cs`:
    - Snapshot test: feature + state record generation
    - Verify fully-qualified Fluxor type references (string-based, not assembly refs)
  - [x] 6.3 Create `tests/.../Emitters/FluxorActionsEmitterTests.cs`:
    - Snapshot test: action records + reducer stubs generation
    - Verify past-tense naming convention on all actions
    - Verify reducer methods are static with `[ReducerMethod]` attribute (string-based)
    - Verify state transitions: LoadRequested->IsLoading=true, Loaded->IsLoading=false+Items, LoadFailed->Error
  - [x] 6.4 Create `tests/.../Emitters/RegistrationEmitterTests.cs`:
    - Snapshot test: single-projection registration
    - Snapshot test: bounded context display label
  - [x] 6.5 Verify all emitted code compiles: parse emitted strings with `CSharpSyntaxTree.ParseText()` and assert zero diagnostics
  - [x] 6.6 Semantic spot-check tests (beyond syntax validation):
    - Emitted DateTime format specifier `"d"` (short date) is lowercase, not `"D"` (long date)
    - Emitted numeric format `"N0"` for Int32 and `"N2"` for Decimal are correct
    - Emitted em dash character is `\u2014`, not hyphen or en dash
    - Snapshot folder convention: snapshots stored alongside test class per Verify convention (e.g., `Emitters/RazorEmitterTests.{TestName}.verified.txt`)

- [x] Task 7: Write end-to-end integration tests (AC: #1, #2, #3, #4)
  - [x] 7.1 Update existing `Integration/GeneratorDriverTests.cs`:
    - BasicProjection: verify generator now produces 5 files: `.g.razor.cs`, `Feature.g.cs`, `Actions.g.cs`, `Reducers.g.cs`, `Registration.g.cs`
    - AllFieldTypes: verify all 29 field types produce correct column type categories
    - Verify generated code compiles against output compilation (existing assertion pattern)
  - [x] 7.2 Add new integration test: BoundedContext grouping
    - Two projections with same `[BoundedContext("Orders")]` -> two separate registration files (`OrderProjectionRegistration.g.cs` + `OrderItemProjectionRegistration.g.cs`), both contributing to partial class `OrdersDomainRegistration`
    - Verify no duplicate hint name crash (Roslyn `ArgumentException`)
    - Verify both partial class contributions compile together into a single type
  - [x] 7.3 Add new integration test: no-output scenario
    - Type with only unsupported fields -> Razor component still generated but with zero DataGrid columns (renders empty state pattern)
    - Feature/Actions/Reducers still generated (state management is independent of column count)
    - Verify generated code compiles even with zero columns
  - [x] 7.4 Update `Caching/IncrementalCachingTests.cs` for incremental caching invalidation:
    - Existing tests expect zero generator output (Parse-only era) -- update to expect 5 generated files per projection
    - Add test: run generator twice with identical input -> verify output is byte-identical (determinism)
    - Add test: change one property on DomainModel (e.g., toggle IsNullable) -> verify output changes accordingly (not cached stale)
    - Add test: verify GetHashCode() differs when input models differ (IEquatable correctness for new output models)

- [x] Task 8: Update AnalyzerReleases.Unshipped.md and finalize (AC: #2)
  - [x] 8.1 Add HFC1001 entry to `AnalyzerReleases.Unshipped.md` following exact pattern of HFC1002-1005 entries (Rule ID, Category, Severity, Notes columns). Build will fail with RS2008 if this is missing.
  - [x] 8.2 Document new generated file outputs in release tracking
  - [x] 8.3 Run full test suite (existing 61 tests + all new) -- zero failures, zero regressions

### Review Findings

- [x] [Review][Patch] HFC1001 warning is never emitted because the generator only registers output for matched `[Projection]` symbols [`src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:25`]
- [x] [Review][Patch] Registration generation does not emit any `IFrontComposerRegistry` / `DomainManifest` wiring, so bounded-context registration is effectively a stub — **Deferred to Story 1-6 Task 0** [`src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs:37`]
- [x] [Review][Patch] `[BoundedContext(..., DisplayLabel = "...")]` is unsupported end-to-end because the attribute/parser/transform pipeline never carries a display label — **Fixed: Added DisplayLabel property to BoundedContextAttribute, DomainModel, AttributeParser extraction, and RegistrationModelTransform propagation** [`src/Hexalith.FrontComposer.SourceTools/Transforms/RegistrationModelTransform.cs:23`]
- [x] [Review][Patch] Enum columns render raw member names and truncation can exceed the specified 30-character maximum [`src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:78`]
- [x] [Review][Patch] Generated source keys and type references rely on the simple `TypeName`, which breaks nested projections and can collide for same-named projections in different namespaces — **Fixed: Hint names now use namespace-qualified prefix (Namespace.TypeName)** [`src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:53`]

## Definition of Done

- [x] All snapshot tests pass and golden `.verified.txt` files are committed
- [x] `CSharpSyntaxTree.ParseText()` verification passes for all emitted code (zero syntax errors)
- [x] Full test suite passes: existing 61 tests + all new tests, zero regressions
- [x] Zero `TreatWarningsAsErrors` violations
- [x] `AnalyzerReleases.Unshipped.md` entries added for any new HFC diagnostic IDs
- [x] Generated code is deterministic: same input produces byte-identical output across runs
- [x] No new dependencies added to SourceTools.csproj (remains netstandard2.0 with only Contracts + CodeAnalysis.CSharp)
- [x] ~~Registration wiring~~ — moved to Story 1-6 Task 0 (RegistrationEmitter enhancement)
- [x] Remaining review findings are resolved: bounded-context display-label support and collision-safe source naming

#### Review Round 2 (2026-04-14)

- [x] [Review][Defer] Two projections sharing BoundedContext but with different DisplayLabels — no conflict detection or diagnostic emitted — deferred, address when bounded-context aggregation is implemented
- [x] [Review][Resolved] AC2 deviation: hint names use `{Namespace}.{TypeName}` instead of spec's `{TypeName}` pattern — spec AC2 updated to reflect namespace-qualified naming
- [x] [Review][Patch] DisplayLabel containing newlines or unescaped control chars can break generated string literals — fixed: EscapeString now handles \n, \r, \t, \0 [`src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs:EscapeString`]
- [x] [Review][Patch] DisplayLabel with empty/whitespace string accepted silently — fixed: added IsNullOrWhiteSpace guard [`src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs:~214`]
- [x] [Review][Patch] DisplayLabel orphaned when BoundedContext name is null/invalid — fixed: displayLabel cleared on invalid name path [`src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs:~226`]
- [x] [Review][Patch] Missing integration test for global-namespace projection with qualified hint prefix — fixed: added RunGenerators_GlobalNamespaceProjection_HintNameHasNoNamespacePrefix [`tests/.../Integration/GeneratorDriverTests.cs`]
- [x] [Review][Patch] Verify snapshot files may be stale after BoundedContextDisplayLabel addition — verified: all 147 tests pass, snapshots current
- [x] [Review][Defer] BoundedContext name with invalid C# identifier chars produces uncompilable generated code — pre-existing [`src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs:37`]
- [x] [Review][Defer] Hint name sanitization for exotic namespace formats (global::, generics) — theoretical, pre-existing [`src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:83`]
- [x] [Review][Defer] XML doc comment escaping for special chars in BoundedContext name — pre-existing [`src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs:40`]
- [x] [Review][Defer] Incremental generator caching edge case when only DisplayLabel changes on a different partial declaration — speculative

## Dev Notes

### Architecture: Three-Stage Pipeline (ADR-004)

The source generator uses a three-stage pipeline within a single project (`SourceTools`):

```
Parse (Story 1.4 - DONE) -> Transform (THIS STORY) -> Emit (THIS STORY)
  INamedTypeSymbol          DomainModel IR           Output Models          Source Code Strings
       -> DomainModel IR    -> RazorModel             -> {TypeName}.g.razor.cs
                            -> FluxorModel            -> {TypeName}Feature.g.cs
                            -> FluxorModel            -> {TypeName}Actions.g.cs
                            -> FluxorModel            -> {TypeName}Reducers.g.cs
                            -> RegistrationModel      -> {TypeName}Registration.g.cs
```

**Parse stage (DONE):** `Parsing/AttributeParser.cs` produces `ParseResult` containing `DomainModel` IR.
**Transform stage (THIS):** Pure functions converting IR to surface-specific models.
**Emit stage (THIS):** String-based code generation from output models.

### Critical Constraints

1. **netstandard2.0 only** -- SourceTools targets netstandard2.0. No `Span<T>`, no `Range`, no default interface methods, no `HashCode.Combine`. Use same patterns as `EquatableArray<T>` vendor.

2. **ZERO Fluxor dependency** -- Generator emits Fluxor types as fully-qualified name strings. Generated code references `Fluxor.Feature<T>` etc. as text, compiling in the consumer project context that has the actual Fluxor package.

3. **ZERO Shell/Fluent UI dependency** -- SourceTools references only Contracts + Microsoft.CodeAnalysis.CSharp. No Shell, Fluxor, or Fluent UI assemblies.

4. **IEquatable<T> on all output models** -- Required for Roslyn incremental caching. If equality changes, cached pipeline steps invalidate. Follow exact pattern from `DomainModel.cs`.

5. **Deterministic output** -- Same DomainModel input must produce byte-identical source code output. No timestamps, no random values, no environment-dependent content.

6. **Emitted code targets consumer's TFM (net10.0)** -- While SourceTools itself is netstandard2.0, the *generated* source code compiles in the consumer project context (net10.0). This means C# 13 features like range operators (`[..8]` for Guid truncation) are safe in emitted strings. The SourceTools project just builds strings -- it doesn't compile them.

7. **Consumer must reference FluentUI + Fluxor** -- Generated Razor code emits `using Microsoft.FluentUI.AspNetCore.Components;` and `using Fluxor;`. These resolve at compile time in the consumer project. If consumer lacks these packages, generated code won't compile -- this is expected and by design (SourceTools stays dependency-free).

### Existing IR Types (DO NOT MODIFY)

The Parse stage produced these IR types in `Parsing/DomainModel.cs` -- consume them as-is:

```
DomainModel: TypeName, Namespace, BoundedContext?, ProjectionRole?, Properties (EquatableArray<PropertyModel>)
PropertyModel: Name, TypeName, IsNullable, IsUnsupported, DisplayName?, BadgeMappings (EquatableArray<BadgeMappingEntry>)
BadgeMappingEntry: EnumMemberName, Slot
ParseResult: Model?, Diagnostics (EquatableArray<DiagnosticInfo>)
```

**PropertyModel.TypeName values** (from FieldTypeMapper): `"String"`, `"Int32"`, `"Int64"`, `"Decimal"`, `"Double"`, `"Single"`, `"Boolean"`, `"DateTime"`, `"DateTimeOffset"`, `"DateOnly"`, `"TimeOnly"`, `"Guid"`, `"Enum"`, `"Collection"`.

### Fluxor Code Generation Patterns (from Story 1.3)

Generated projection state MUST follow ADR-008 shape adapted for list views (DataGrid renders multiple rows):
```csharp
// Projection list state: IsLoading, Items (nullable list), Error (nullable)
record {TypeName}State(bool IsLoading, IReadOnlyList<{TypeName}>? Items, string? Error);
```
**NOTE:** This differs from the command lifecycle state shape (`{TypeName}? Data` -- single item) documented in ADR-008. Projections use `IReadOnlyList<T>? Items` because DataGrid requires a collection data source. Command lifecycle states (Story 2.x) will use the single-item `Data` shape.

Generated feature:
```csharp
public class {TypeName}Feature : Feature<{TypeName}State>
{
    public override string GetName() => nameof({TypeName}State);
    protected override {TypeName}State GetInitialState() =>
        new(IsLoading: false, Items: null, Error: null);
}
```

Component subscription (Path B -- no FluxorComponent base class):
```csharp
public partial class {TypeName}View : ComponentBase, IDisposable
{
    [Inject] private IState<{TypeName}State> {TypeName}State { get; set; } = default!;

    protected override void OnInitialized()
    {
        {TypeName}State.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
        => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        {TypeName}State.StateChanged -= OnStateChanged;
    }
}
```

### Data Formatting Rules (UX-DR35)

| .NET Type | Column Behavior | Null Rendering |
|-----------|----------------|----------------|
| `string` | Text column | \u2014 (em dash) |
| `int`/`long`/`decimal` | Right-aligned, locale-formatted (N0/N2) | \u2014 |
| `bool` | "Yes"/"No" text | \u2014 |
| `DateTime`/`DateTimeOffset` | Short date per CultureInfo | \u2014 |
| `DateOnly` | Short date | \u2014 |
| `TimeOnly` | Short time | \u2014 |
| `enum` | Humanized label, max 30 chars + ellipsis | \u2014 |
| `Guid` | 8-char truncated monospace | \u2014 |
| `Collection` | Count (e.g., "3 items") | \u2014 |

**Null handling (UX-DR65):** NEVER "null", "N/A", or empty cells. Always em dash (\u2014).

### Label Resolution Chain (UX-DR21)

Column headers follow this priority:
1. `[Display(Name="...")]` attribute value (PropertyModel.DisplayName)
2. Humanized CamelCase (`OrderDate` -> `"Order Date"`)
3. Raw property name (final fallback)

### File Naming Convention

| Generated Element | File Pattern | Example |
|-------------------|-------------|---------|
| Razor partial | `{TypeName}.g.razor.cs` | `OrderProjection.g.razor.cs` |
| Fluxor feature+state | `{TypeName}Feature.g.cs` | `OrderProjectionFeature.g.cs` |
| Fluxor actions | `{TypeName}Actions.g.cs` | `OrderProjectionActions.g.cs` |
| Fluxor reducers | `{TypeName}Reducers.g.cs` | `OrderProjectionReducers.g.cs` |
| Domain registration (per-type partial) | `{TypeName}Registration.g.cs` | `OrderProjectionRegistration.g.cs` |

All generated files placed via `spc.AddSource(hintName, source)` -- Roslyn handles output path: `obj/{Config}/{TFM}/generated/HexalithFrontComposer/`. Note: Consumers may override via MSBuild `<GeneratedFilesDir>` property. Generator-produced namespaces assume default Roslyn output structure; namespace correctness under custom paths is the consumer's responsibility.

### No-Output Scenario (Zero Valid Columns)

If a `[Projection]`-annotated type has only unsupported fields (all columns skipped):
- Razor component IS still generated but renders the empty state pattern ("No data columns available")
- Fluxor Feature/State/Actions/Reducers ARE still generated (state management is independent of column count)
- No additional diagnostic emitted (HFC1002 already fires per unsupported field in Parse stage)
- Generated code must still compile and function correctly with zero columns

### Diagnostic IDs (HFC1000-1999)

| ID | Condition | Severity | Status |
|----|-----------|----------|--------|
| HFC1001 | No `[Command]` or `[Projection]` types found | Warning | **NEW in this story** |
| HFC1002 | Unsupported field type | Warning | Done (Story 1.4) |
| HFC1003 | Type should be partial | Warning | Done (Story 1.4) |
| HFC1004 | Unsupported type kind | Warning | Done (Story 1.4) |
| HFC1005 | Invalid attribute argument | Warning | Done (Story 1.4) |

### Project Structure Notes

**New directories to create:**
```
src/Hexalith.FrontComposer.SourceTools/
\u251c\u2500\u2500 Transforms/                    <-- NEW
\u2502   \u251c\u2500\u2500 RazorModel.cs              (output model)
\u2502   \u251c\u2500\u2500 FluxorModel.cs             (output model)
\u2502   \u251c\u2500\u2500 RegistrationModel.cs       (output model)
\u2502   \u251c\u2500\u2500 ColumnModel.cs             (sub-model for Razor columns)
\u2502   \u251c\u2500\u2500 RazorModelTransform.cs     (pure function)
\u2502   \u251c\u2500\u2500 FluxorModelTransform.cs    (pure function)
\u2502   \u251c\u2500\u2500 RegistrationModelTransform.cs (pure function)
\u2502   \u2514\u2500\u2500 CamelCaseHumanizer.cs      (utility)
\u251c\u2500\u2500 Emitters/                      <-- NEW
\u2502   \u251c\u2500\u2500 RazorEmitter.cs            (string source generation)
\u2502   \u251c\u2500\u2500 FluxorFeatureEmitter.cs    (string source generation)
\u2502   \u251c\u2500\u2500 FluxorActionsEmitter.cs    (string source generation)
\u2502   \u2514\u2500\u2500 RegistrationEmitter.cs     (string source generation)
\u251c\u2500\u2500 Parsing/                       (existing - DO NOT MODIFY)
\u2502   \u251c\u2500\u2500 AttributeParser.cs
\u2502   \u251c\u2500\u2500 DomainModel.cs
\u2502   \u251c\u2500\u2500 EquatableArray.cs
\u2502   \u2514\u2500\u2500 FieldTypeMapper.cs
\u251c\u2500\u2500 Diagnostics/                   (existing - ADD HFC1001)
\u2502   \u2514\u2500\u2500 DiagnosticDescriptors.cs
\u2514\u2500\u2500 FrontComposerGenerator.cs      (existing - MODIFY placeholder)

tests/Hexalith.FrontComposer.SourceTools.Tests/
\u251c\u2500\u2500 Transforms/                    <-- NEW
\u2502   \u251c\u2500\u2500 RazorModelTransformTests.cs
\u2502   \u251c\u2500\u2500 FluxorModelTransformTests.cs
\u2502   \u251c\u2500\u2500 RegistrationModelTransformTests.cs
\u2502   \u2514\u2500\u2500 CamelCaseHumanizerTests.cs
\u251c\u2500\u2500 Emitters/                      <-- NEW
\u2502   \u251c\u2500\u2500 RazorEmitterTests.cs
\u2502   \u251c\u2500\u2500 FluxorEmitterTests.cs
\u2502   \u251c\u2500\u2500 FluxorActionsEmitterTests.cs
\u2502   \u2514\u2500\u2500 RegistrationEmitterTests.cs
\u251c\u2500\u2500 Snapshots/                     (existing - ADD new .verified.txt files)
\u251c\u2500\u2500 Integration/                   (existing - UPDATE GeneratorDriverTests.cs)
\u2514\u2500\u2500 ...                            (existing tests unchanged)
```

**Alignment:** Mirrors source structure (`Transforms/` and `Emitters/` in both src and tests). Follows pattern established in Story 1.4 with `Parsing/`.

### Testing Framework & Conventions

- **xUnit v3** with `[Fact]` / `[Theory]` / `[InlineData]`
- **Shouldly** assertions: `.ShouldBe()`, `.ShouldNotBeNull()`, `.ShouldBeEmpty()`
- **Verify** (VerifyTests) for snapshot testing: `.verified.txt` files in `Snapshots/`
- **NSubstitute** for mocks (if needed -- likely not for pure function tests)
- **Test naming:** `{Method}_{Scenario}_{Expected}`
- **CancellationToken:** Use `TestContext.Current.CancellationToken` in async tests
- **CompilationHelper.cs** already exists for test reference assemblies

### Previous Story Intelligence (Story 1.4)

**Critical learnings to follow:**

1. **Build error RS2008:** AnalyzerReleases.Shipped.md / Unshipped.md required for release tracking. Already set up -- just add new entries for any new diagnostics.

2. **Build error RS1032:** Diagnostic message format must use `{0}` placeholder style (already working pattern in DiagnosticDescriptors.cs).

3. **xUnit1051:** All `RunGenerators()`, `GetDiagnostics()`, `ParseText()` calls must pass `TestContext.Current.CancellationToken`.

4. **Verify framework:** Use `Verify` (generic object serialization) for IR snapshots. For emitted source code in THIS story, `Verify.SourceGenerators` can now be used OR continue with `Verify` + string snapshots.

5. **EquatableArray<T>:** Already vendored in `Parsing/EquatableArray.cs`. Reuse for Transform output model collections. Do NOT create a second copy.

6. **TreatWarningsAsErrors=true:** Enforced globally. Any new warnings from emitted code must be handled. Test fixture projects may need `<NoWarn>` for HFC diagnostics.

7. **61 existing tests (all passing):** Do not break any. Run full test suite after changes.

8. **Incremental caching tests exist:** Existing caching tests in `Caching/IncrementalCachingTests.cs` will need updating since the generator now produces output (previously no output was produced because Transform+Emit didn't exist). **Task 7.4 covers this explicitly.**

### Git Intelligence

Recent commits (relevant patterns):
- `5bd730f`: Story 1.3 completed -- Fluxor state management foundation
- `4d3b726`: Sprint status tracking updates
- Story 1.4 is in review (uncommitted changes visible in `git status`)
- Project uses conventional commit messages
- Stories incrementally build on previous architecture

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-1-project-scaffolding-first-auto-generated-view.md#Story 1.5] -- Full story spec, BDD acceptance criteria
- [Source: _bmad-output/planning-artifacts/architecture.md#ADR-004] -- Three-stage pipeline architecture
- [Source: _bmad-output/planning-artifacts/architecture.md#ADR-008] -- Fluxor state shape convention
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR21] -- Label resolution chain
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR35] -- Data formatting rules
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR65] -- Null handling (em dash)
- [Source: _bmad-output/implementation-artifacts/1-4-source-generator-parse-stage.md] -- Parse stage implementation (predecessor)
- [Source: _bmad-output/implementation-artifacts/1-3-fluxor-state-management-foundation.md] -- Fluxor patterns to target
- [Source: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:42-46] -- Placeholder to replace
- [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs] -- IR types to consume
- [Source: src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs:12] -- HFC1001 reservation

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- CamelCaseHumanizer: Initial implementation split "OrderIDs" as "Order I Ds". Fixed by requiring 3+ consecutive uppercase chars before inserting acronym-break space.
- Integration tests: Generated Razor code references Fluxor/FluentUI/ASP.NET Core. Added `Fluxor.Blazor.Web`, `Microsoft.FluentUI.AspNetCore.Components`, and `FrameworkReference Microsoft.AspNetCore.App` to test project. Updated `CompilationHelper` with assembly references.
- Generated code compilation: Fixed missing `using System.Linq;` (for AsQueryable), non-nullable Guid `?.` operator, inline `Truncate` helper, `System.Linq.Enumerable.Count()` for collections, `RenderFragment` (not `RenderFragment<T>`) for ChildContent, and `System.Linq.Expressions.dll` reference for IQueryable.

### Completion Notes List

- Implementation tasks and automated tests were completed in a single session, but the story remains `in-progress` until the three open review findings above are resolved
- 139 SourceTools tests pass (78 new + 61 existing, zero regressions)
- 182 total solution tests pass (139 SourceTools + 34 Shell + 9 Contracts)
- Full solution build: 0 warnings, 0 errors
- Transform stage: 4 output models (RazorModel, FluxorModel, RegistrationModel, ColumnModel) + TypeCategory enum
- Transform functions: 3 transforms (Razor, Fluxor, Registration) + CamelCaseHumanizer utility
- Emit stage: 4 emitters (Razor, FluxorFeature, FluxorActions, Registration)
- Generator wiring: FrontComposerGenerator now produces 5 files per [Projection] type
- HFC1001 diagnostic added for "No [Command] or [Projection] types found"
- 10 snapshot .verified.txt files committed
- Deterministic output verified (same input -> byte-identical output)
- BoundedContext grouping verified (multiple projections sharing a BoundedContext produce separate partial class contributions, no hint name crash)
- Review findings resolved: DisplayLabel end-to-end support (attribute, parser, DomainModel, transform) and namespace-qualified hint names for collision safety
- Registration wiring deferred to Story 1-6 as documented in DoD
- 146 SourceTools tests pass (85 new + 61 existing, zero regressions)
- 189 total solution tests pass (146 SourceTools + 34 Shell + 9 Contracts)

### File List

**New files (src):**
- src/Hexalith.FrontComposer.SourceTools/Transforms/TypeCategory.cs
- src/Hexalith.FrontComposer.SourceTools/Transforms/ColumnModel.cs
- src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModel.cs
- src/Hexalith.FrontComposer.SourceTools/Transforms/FluxorModel.cs
- src/Hexalith.FrontComposer.SourceTools/Transforms/RegistrationModel.cs
- src/Hexalith.FrontComposer.SourceTools/Transforms/CamelCaseHumanizer.cs
- src/Hexalith.FrontComposer.SourceTools/Transforms/RazorModelTransform.cs
- src/Hexalith.FrontComposer.SourceTools/Transforms/FluxorModelTransform.cs
- src/Hexalith.FrontComposer.SourceTools/Transforms/RegistrationModelTransform.cs
- src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs
- src/Hexalith.FrontComposer.SourceTools/Emitters/FluxorFeatureEmitter.cs
- src/Hexalith.FrontComposer.SourceTools/Emitters/FluxorActionsEmitter.cs
- src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs

**Modified files (src):**
- src/Hexalith.FrontComposer.Contracts/Attributes/BoundedContextAttribute.cs
- src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs
- src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs
- src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs
- src/Hexalith.FrontComposer.SourceTools/Transforms/RegistrationModelTransform.cs
- src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs
- src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md

**New files (tests):**
- tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/CamelCaseHumanizerTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/RazorModelTransformTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/FluxorModelTransformTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/RegistrationModelTransformTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/FluxorEmitterTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/FluxorActionsEmitterTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RegistrationEmitterTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/*.verified.txt (10 snapshot files)

**Modified files (tests):**
- tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj
- tests/Hexalith.FrontComposer.SourceTools.Tests/CompilationHelper.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Caching/IncrementalCachingTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/AttributeParserTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/TestSources.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/RazorModelTransformTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/FluxorModelTransformTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/RegistrationModelTransformTests.cs

**Modified files (config):**
- _bmad-output/implementation-artifacts/sprint-status.yaml

### Change Log

- **2026-04-14:** Addressed code review findings - 3 items resolved (DisplayLabel end-to-end, collision-safe hint names, registration stub deferred to Story 1-6)
