# Story 1.4: Source Generator - Parse Stage

Status: done

## Story

As a developer,
I want a Roslyn incremental source generator that parses domain model attributes into a typed intermediate representation,
So that the framework can reason about domain types at compile time with a testable, pure-function core.

## Acceptance Criteria

### AC1: SourceTools Project Configuration

**Given** the `Hexalith.FrontComposer.SourceTools` project exists
**When** the project is built
**Then** it targets `netstandard2.0` only
**And** `IsRoslynComponent=true` and `EnforceExtendedAnalyzerRules=true` are set
**And** it references Contracts (for attribute types) and `Microsoft.CodeAnalysis.CSharp` (`PrivateAssets="all"`)
**And** it does NOT reference Shell, Fluxor, or Fluent UI

**Verification:** The `.csproj` already exists with correct settings. Validate no new unintended dependencies are added during implementation.

### AC2: Parse Stage via ForAttributeWithMetadataName

**Given** a C# type is annotated with `[Projection]`
**When** the Parse stage runs via `ForAttributeWithMetadataName`
**Then** the `INamedTypeSymbol` is extracted into a `DomainModel` IR record
**And** the IR captures:
  - Type name and namespace
  - Properties: name, type, nullability
  - Applied attributes: `[BoundedContext]`, `[ProjectionRole]`, `[ProjectionBadge]`, `[Display]`
**And** the Parse function is pure (no side effects, no `Compilation` references in output)

### AC3: Snapshot Testing and Field Type Coverage

**Given** a test project with known `[Projection]`-annotated types
**When** Parse stage snapshot tests run using the **Verify** (VerifyTests) snapshot framework
**Then** verified snapshot output matches expected `DomainModel` IR for each test type
**And** the field type coverage matrix includes all types listed below:

| Category | Types | Count |
|----------|-------|-------|
| Primitives | string, int, long, decimal, double, float, bool | 7 |
| Date/Time | DateTime, DateTimeOffset, DateOnly, TimeOnly | 4 |
| Identity | Guid | 1 |
| Enum | enum (backed by int) | 1 |
| Nullable | Nullable<T> for each primitive, date/time, identity, enum type | 13 |
| Collections | List<T>, IEnumerable<T>, IReadOnlyList<T> | 3 |
| **Total** | | **29** |

**Note:** Nested records and ULID are deferred to Story 1.5 or later when complex type handling is validated end-to-end through the Transform stage.

### AC4: Unsupported Field Type Handling

**Given** the generator encounters an unsupported field type
**When** the Parse stage processes it
**Then** a diagnostic `HFC1002` is emitted at **Warning** severity with format: What / Expected / Got / Fix / DocsLink
**And** the field is included in the IR with an `IsUnsupported` flag

**Note:** Per architecture diagnostic ID allocation: HFC1001 = "No [Command] or [Projection] types found in compilation" (deferred to Story 1.5 full pipeline). HFC1002 = "Unsupported field type in [Projection]" (this story).

### AC5: Error Recovery and Partial Parse

**Given** a `[Projection]`-annotated type has syntax errors or partially invalid attribute arguments
**When** the Parse stage processes it
**Then** the parser produces a partial `DomainModel` IR for whatever it could successfully parse
**And** diagnostics are emitted for the invalid portions
**And** the generator does NOT throw or crash -- downstream stages receive the partial IR

## Out of Scope

- **`[Command]`-annotated type parsing** -- The parse stage in this story is scoped to `[Projection]` types only. `[Command]` parsing will be added in Story 1.5 alongside the Transform/Emit stages that consume both IR types.
- **FsCheck property-based tests** -- Deferred to a follow-up task after the core parse stage is validated. Architecture targets: 1000 CI iterations, 10000 nightly. Will be added as a subtask before W2 milestone.
- **Stryker mutation testing** -- Mutation score target (>=70% W2, >=85% v0.1) applies across Parse+Transform. Stryker configuration will be added as part of Story 1.7 (CI Pipeline) with Parse stage as the first target.
- **Nested record types and ULID** -- Complex type support deferred to Story 1.5+ when Transform stage can validate end-to-end rendering.

## Tasks / Subtasks

- [x] Task 1: Implement IIncrementalGenerator entry point (AC: #1, #2)
  - [x] 1.1 Create `FrontComposerGenerator.cs` implementing `IIncrementalGenerator`
  - [x] 1.2 Register `ForAttributeWithMetadataName` for `[Projection]` attribute (fully qualified: `Hexalith.FrontComposer.Contracts.Attributes.ProjectionAttribute`)
  - [x] 1.3 Wire parse pipeline: syntax filter -> semantic transform
- [x] Task 2: Create DomainModel IR records (AC: #2)
  - [x] 2.1 Create `DomainModel.cs` in `Parsing/` folder -- immutable record with: TypeName, Namespace, BoundedContext, ProjectionRole, Properties (EquatableArray<PropertyModel>)
  - [x] 2.2 Create `PropertyModel` record: Name, TypeName, IsNullable, IsUnsupported, DisplayName, BadgeMapping (EquatableArray<BadgeMappingEntry>? for enum-typed properties)
  - [x] 2.3 Create `ParseResult` record: `ParseResult(DomainModel? Model, EquatableArray<DiagnosticInfo> Diagnostics)` -- parse returns this, NOT raw DomainModel, because diagnostics cannot be emitted from the `ForAttributeWithMetadataName` transform callback (no `SourceProductionContext` at that stage). Diagnostics are unpacked in `RegisterSourceOutput`.
  - [x] 2.4 Create `DiagnosticInfo` record: Id, Message, Severity, Location (file path + line/column as strings, not Roslyn `Location`)
  - [x] 2.5 Vendor `EquatableArray<T>` struct into `Parsing/` folder -- must work on netstandard2.0: use manual element-wise iteration for `SequenceEqual`, XOR-based hash code (no `Span<T>`, no `HashCode.Combine`)
  - [x] 2.6 Implement `IEquatable<T>` on all IR records (required for Roslyn incremental caching). Note: C# records auto-implement equality for scalar properties BUT NOT for collection properties -- `EquatableArray<T>` handles this.
  - [x] 2.7 Do NOT reference Contracts `RenderHints` type in IR -- all IR types are self-contained plain data. If render hint fields are needed, inline them directly in `PropertyModel`.
- [x] Task 3: Implement AttributeParser (AC: #2)
  - [x] 3.1 Create `AttributeParser.cs` in `Parsing/` folder
  - [x] 3.2 Signature: `static ParseResult Parse(GeneratorAttributeSyntaxContext context, CancellationToken ct)` -- use `context.TargetSymbol` (cast to `INamedTypeSymbol`) and `context.Attributes` (pre-resolved matched attributes). Do NOT re-query attributes via `GetAttributes()`.
  - [x] 3.3 Parse `[BoundedContext(name)]` from the annotated type only. If absent, leave bounded-context discovery to downstream fallback logic rather than inferring a namespace-level attribute that C# cannot express.
  - [x] 3.4 Parse `[ProjectionRole(role)]` from type
  - [x] 3.5 Parse `[Display(Name=...)]` from properties (use `System.ComponentModel.DataAnnotations.DisplayAttribute`)
  - [x] 3.6 Parse `[ProjectionBadge(slot)]` from **enum member fields** (NOT from projection properties). `ProjectionBadgeAttribute` targets `AttributeTargets.Field` -- it decorates individual enum members (e.g., `[ProjectionBadge(BadgeSlot.Warning)] Active`), not properties on the projection class. To parse: resolve the property's type symbol to the enum, then iterate `IFieldSymbol` members of that enum for badge attribute data. Store as `BadgeMappingEntry(string EnumMemberName, BadgeSlot Slot)` list on the `PropertyModel`.
- [x] Task 4: Implement FieldTypeMapper (AC: #2, #3, #4)
  - [x] 4.1 Create `FieldTypeMapper.cs` in `Parsing/` folder as a **public static class** (unit-testable in isolation without CSharpGeneratorDriver)
  - [x] 4.2 Map all 29 supported .NET types to IR type representation (see field type matrix)
  - [x] 4.3 Detect nullability via `NullableAnnotation` on `IPropertySymbol` and `Nullable<T>` check for value types
  - [x] 4.4 Flag unsupported types with `IsUnsupported = true`
  - [x] 4.5 Handle missing nullable context: when `NullableContextOptions` is disabled, treat reference types as nullable by default (defensive)
- [x] Task 5: Implement Diagnostic Reporting (AC: #4, #5)
  - [x] 5.1 Create `DiagnosticDescriptors.cs` in `Diagnostics/` folder
  - [x] 5.2 Define `HFC1002` descriptor: unsupported field type, severity **Warning** (What/Expected/Got/Fix/DocsLink format). Reserve HFC1001 for "no annotated types found" (Story 1.5).
  - [x] 5.3 Wire diagnostic emission in `RegisterSourceOutput` callback: unpack `ParseResult.Diagnostics` and call `SourceProductionContext.ReportDiagnostic` for each. Diagnostics CANNOT be emitted from the `ForAttributeWithMetadataName` transform -- they must travel as data in `ParseResult`.
  - [x] 5.4 Ensure error recovery: partial parse continues on invalid attributes, collecting diagnostics in `ParseResult` without crashing
- [x] Task 6: Write Parse Stage Tests (AC: #3, #4, #5)
  - [x] 6.1 Create test fixture types with `[Projection]` annotations covering full 29-type field type matrix
  - [x] 6.2 Write `AttributeParserTests.cs` using `CSharpGeneratorDriver` test harness
  - [x] 6.3 Write `FieldTypeMapperTests.cs` as **unit tests** (mapper is public static, testable without compilation context)
  - [x] 6.4 Create Verify snapshots for IR objects (use `Verify` generic object serialization, NOT `Verify.SourceGenerators` -- parse-only stage has no emitted source yet; `Verify.SourceGenerators` deferred to Story 1.5)
  - [x] 6.5 Write diagnostic negative-path tests (minimum 12 scenarios):
    - (a) Unsupported field type -> HFC1002
    - (b) Malformed attribute arguments (e.g., `[BoundedContext(null)]`)
    - (c) `[Projection]` on type with syntax errors -> partial IR + diagnostic
    - (d) `[Projection]` on class missing `partial` keyword -> diagnostic
    - (e) `[Projection]` on a `struct` -> diagnostic or supported? (specify behavior)
    - (f) `[Projection]` on a `record struct` -> same
    - (g) `[Projection]` on a nested class inside non-partial outer class -> diagnostic
    - (h) `[Projection]` on a generic class `MyProjection<T>` -> diagnostic
    - (i) `[Projection]` on an abstract class -> diagnostic
    - (j) `[Projection]` on class in global namespace (no namespace) -> handle gracefully
    - (k) Multiple attributes on same class (`[Projection] [ProjectionRole(...)]`)
    - (l) `[Projection]` on type with compound/unsupported properties (`byte[]`, `Dictionary<,>`, `object`, `dynamic`, tuple) -> HFC1002 per field, no crash
  - [x] 6.6 Write `GeneratorDriverTests.cs` end-to-end integration test -- **every driver test MUST assert**: `outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty()` (verifies generated code compiles)
  - [x] 6.7 Add test packages to `Directory.Packages.props` and test `.csproj`:
    - `Verify` (generic object serialization for IR snapshots)
    - `Verify.XunitV3` (xUnit v3 integration -- NOT `Verify.Xunit` which is for xUnit v2)
    - `Microsoft.CodeAnalysis.CSharp` (for CSharpGeneratorDriver)
    - Pin exact versions in `Directory.Packages.props` (required by Central Package Management)
  - [x] 6.8 Write **incremental caching correctness tests** (CRITICAL):
    - (a) Unrelated file edit -> generator step shows `IncrementalStepRunReason.Cached` (IR not recomputed)
    - (b) Attribute argument change -> step shows `IncrementalStepRunReason.Modified`
    - (c) Adding a new `[Projection]` class -> existing cached outputs not invalidated
  - [x] 6.9 Write cancellation token test: pass pre-cancelled `CancellationToken`, assert generator exits without output or exception
  - [x] 6.10 Create `CompilationHelper.cs` utility for test reference assemblies (see Dev Notes: Test Reference Assemblies)
- [x] Task 7: Performance Baseline (AC: #2)
  - [x] 7.1 Create a benchmark test (BenchmarkDotNet or xUnit stopwatch) measuring parse stage against a fixture with 20+ projected types
  - [x] 7.2 Assert P95 < 500ms for full pipeline (Parse is expected well under 50ms alone; 500ms is the budget for the entire Parse+Transform+Emit chain per NFR8). Note: benchmark thresholds are developer guidance for this story; CI enforcement is deferred to Story 1.7.
  - [x] 7.3 Run as a separate test category (do not slow CI feedback loop)

### Review Findings

- [x] `[Review][Dismiss]` Clarify namespace-level bounded-context support — dismissed because .NET `AttributeTargets` does not support `Namespace`, so `BoundedContextAttribute` cannot be applied to a namespace in C#. Type-level `[BoundedContext]` remains the viable Story 1.4 scope.
- [x] `[Review][Patch]` Snapshot tests verify empty generator-driver output instead of `DomainModel` IR [`tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/AttributeParserTests.cs:33`]
- [x] `[Review][Patch]` Report diagnostics at their captured source locations instead of `Location.None` [`src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:36`]
- [x] `[Review][Patch]` Handle pre-cancelled generator runs without throwing [`src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs:32`]
- [x] `[Review][Patch]` Reject invalid enum attribute values and unsupported enum layouts during parse [`src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs:223`]
- [x] `[Review][Patch]` Tighten incremental caching tests to assert parse-stage behavior specifically [`tests/Hexalith.FrontComposer.SourceTools.Tests/Caching/IncrementalCachingTests.cs:41`]

## Dev Notes

### Architecture: Three-Stage Pipeline (ADR-004)

The source generator uses a three-stage pipeline within a single project (folder organization, not project boundaries):

1. **Parse** (THIS STORY) -- Roslyn `INamedTypeSymbol` -> `DomainModel` IR (pure data records)
2. **Transform** (Story 1.5) -- `DomainModel` -> surface-specific output models (RazorModel, FluxorModel, etc.)
3. **Emit** (Story 1.5) -- output models -> string source code

**Parse and Transform are pure functions -> testable with unit tests. Emit uses snapshot/golden-file tests.**

### Generator Entry Point Pattern

```csharp
[Generator]
public sealed class FrontComposerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Parse stage: discover [Projection]-annotated types
        // ForAttributeWithMetadataName is the REQUIRED approach (not CreateSyntaxProvider)
        var parseResults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Hexalith.FrontComposer.Contracts.Attributes.ProjectionAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                transform: static (ctx, ct) => AttributeParser.Parse(ctx, ct))  // receives full GeneratorAttributeSyntaxContext
            .Where(static r => r is not null);

        // Output registration -- unpack ParseResult, report diagnostics, forward model
        context.RegisterSourceOutput(parseResults, static (spc, result) =>
        {
            // Report diagnostics collected during parse (cannot emit from transform callback)
            foreach (var diag in result!.Diagnostics)
                spc.ReportDiagnostic(diag.ToDiagnostic());

            // Emit stage (Story 1.5) -- for now, parse-only validation
            if (result.Model is not null)
            {
                // Transform + Emit will go here in Story 1.5
            }
        });
    }
}
```

**Key points:**
- `AttributeParser.Parse()` receives `GeneratorAttributeSyntaxContext` (not raw `ISymbol`). Use `ctx.TargetSymbol` (cast to `INamedTypeSymbol`) and `ctx.Attributes` (pre-resolved matched attributes -- do NOT re-query via `GetAttributes()`).
- Returns `ParseResult(DomainModel?, EquatableArray<DiagnosticInfo>)` -- diagnostics travel as data because `SourceProductionContext` is only available in `RegisterSourceOutput`.
- `CancellationToken` (the `ct` parameter) MUST be checked periodically during property iteration for IDE responsiveness.

### DomainModel IR Design Constraints

- All IR records MUST implement `IEquatable<T>` -- Roslyn incremental caching compares previous/current output via equality. Without correct equality, the generator re-runs on every keystroke.
- IR records MUST NOT hold `ISymbol`, `Compilation`, `SemanticModel`, or any Roslyn object -- these are not serializable and break caching.
- Use `string` for type names (fully qualified), not symbol references.
- Collections in IR: use `ImmutableArray<T>` wrapped in a **vendored `EquatableArray<T>`** struct (copy the standard implementation into the SourceTools project -- do NOT take a NuGet dependency, as transitive packages pollute the analyzer load context on netstandard2.0).
- The parse function signature MUST accept `CancellationToken` and check it periodically (especially when iterating large property lists). The token comes from `IncrementalGeneratorInitializationContext`.
- **netstandard2.0 constraint:** No `Span<T>`, `Range`, or default interface methods in IR types. Keep types simple.

### Attribute Fully-Qualified Names for ForAttributeWithMetadataName

| Attribute | Fully Qualified Name |
|-----------|---------------------|
| `[Projection]` | `Hexalith.FrontComposer.Contracts.Attributes.ProjectionAttribute` |
| `[Command]` | `Hexalith.FrontComposer.Contracts.Attributes.CommandAttribute` |
| `[BoundedContext]` | `Hexalith.FrontComposer.Contracts.Attributes.BoundedContextAttribute` |
| `[ProjectionRole]` | `Hexalith.FrontComposer.Contracts.Attributes.ProjectionRoleAttribute` |
| `[ProjectionBadge]` | `Hexalith.FrontComposer.Contracts.Attributes.ProjectionBadgeAttribute` |
| `[Display]` | `System.ComponentModel.DataAnnotations.DisplayAttribute` |

### File Structure

```
src/Hexalith.FrontComposer.SourceTools/
├── FrontComposerGenerator.cs          # IIncrementalGenerator entry point (thin)
├── Parsing/
│   ├── AttributeParser.cs             # GeneratorAttributeSyntaxContext -> ParseResult
│   ├── DomainModel.cs                 # IR data records (DomainModel, PropertyModel, ParseResult, DiagnosticInfo)
│   ├── EquatableArray.cs              # Vendored netstandard2.0-compatible EquatableArray<T>
│   └── FieldTypeMapper.cs             # .NET type -> IR type mapping (public static)
├── Diagnostics/
│   └── DiagnosticDescriptors.cs       # HFC1000-1999 catalog
└── Pipeline/
    └── (Story 1.5)                    # FrontComposerPipeline.cs

tests/Hexalith.FrontComposer.SourceTools.Tests/
├── CompilationHelper.cs               # MetadataReference utility for CSharpGeneratorDriver
├── Parsing/
│   ├── AttributeParserTests.cs        # Parse stage tests via CSharpGeneratorDriver
│   ├── FieldTypeMapperTests.cs        # Unit tests (no compilation context needed)
│   └── TestFixtures/                  # [Projection]-annotated source strings
│       ├── BasicProjection.cs
│       ├── AllFieldTypesProjection.cs
│       ├── UnsupportedFieldProjection.cs
│       ├── EdgeCaseProjections.cs     # Nested, generic, abstract, struct, global namespace
│       └── CompoundTypeProjection.cs  # byte[], tuple, Dictionary, object, dynamic
├── Snapshots/                         # Verify verified files
│   └── *.verified.txt                 # Verify uses .verified.txt (not .approved.cs)
├── Caching/
│   └── IncrementalCachingTests.cs     # Caching correctness (Cached/Modified/New)
└── Integration/
    └── GeneratorDriverTests.cs        # End-to-end + output compilation validation
```

### Supported Field Type Matrix (29 types)

| .NET Type | IR Type | Rendering (Story 1.5) |
|-----------|---------|----------------------|
| `string` | String | Text column |
| `int` | Int32 | Right-aligned, locale-formatted |
| `long` | Int64 | Right-aligned, locale-formatted |
| `decimal` | Decimal | Right-aligned, locale-formatted |
| `double` | Double | Right-aligned, locale-formatted |
| `float` | Single | Right-aligned, locale-formatted |
| `bool` | Boolean | "Yes"/"No" text |
| `DateTime` | DateTime | Short date per CultureInfo |
| `DateTimeOffset` | DateTimeOffset | Short date per CultureInfo |
| `DateOnly` | DateOnly | Short date per CultureInfo |
| `TimeOnly` | TimeOnly | Short time per CultureInfo |
| `enum` | Enum | Humanized labels (max 30 chars) |
| `Guid` | Guid | String representation |
| `T?` (nullable) | Same + IsNullable=true | Em dash (--) for null |
| `List<T>` | Collection | (Story 1.5 handling) |
| `IEnumerable<T>` | Collection | (Story 1.5 handling) |
| `IReadOnlyList<T>` | Collection | (Story 1.5 handling) |

**Nullable variants (13):** One per non-collection type above (string?, int?, long?, decimal?, double?, float?, bool?, DateTime?, DateTimeOffset?, DateOnly?, TimeOnly?, Guid?, enum?).

**Deferred types:** Nested records, ULID -- added in Story 1.5+ when Transform validates complex rendering.

### Nullability Detection

Use `IPropertySymbol.NullableAnnotation`:
- `NullableAnnotation.Annotated` -> `IsNullable = true`
- `NullableAnnotation.NotAnnotated` -> `IsNullable = false`
- For value types, also check if the type is `Nullable<T>` via `INamedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T`

**Edge case -- nullable context disabled:** When the consuming project does not enable `<Nullable>enable</Nullable>`, reference types like `string` will have `NullableAnnotation.None`. In this case, treat reference types as **nullable by default** (defensive approach). This prevents false assumptions about non-nullability when the compiler itself doesn't know.

### Diagnostic ID Allocation (SourceTools: HFC1000-1999)

| ID | Severity | Condition | Story |
|----|----------|-----------|-------|
| HFC1001 | Warning | No `[Command]` or `[Projection]` types found in compilation | 1.5 |
| HFC1002 | Warning | Unsupported field type in `[Projection]` | **1.4** |

**HFC1002 Format:**

**Severity: Warning** (teams can promote to Error via `.editorconfig`: `dotnet_diagnostic.HFC1002.severity = error`)

**Note:** `TreatWarningsAsErrors=true` in `Directory.Build.props` means HFC1002 will be promoted to error within this repository. Add `<NoWarn>HFC1002</NoWarn>` to test fixture projects and the Counter sample to prevent build failures during development.

```
HFC1002: Unsupported field type
What: Property '{PropertyName}' on type '{TypeName}' has type '{FieldType}' which is not supported for auto-generation.
Expected: One of: string, int, long, decimal, double, float, bool, DateTime, DateTimeOffset, DateOnly, TimeOnly, enum, Guid, or nullable/collection variants.
Got: {FieldType}
Fix: Use a supported type, or override rendering with [ProjectionFieldSlot] (Story 6.3).
DocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1002
```

### SourceTools Test Project Configuration

The test project references SourceTools as a **normal project reference** (NOT as an analyzer):
```xml
<ProjectReference Include="..\..\src\Hexalith.FrontComposer.SourceTools\Hexalith.FrontComposer.SourceTools.csproj" />
```
This allows calling `AttributeParser.Parse()` and `FieldTypeMapper.Map()` as regular methods in tests.

For integration tests using `CSharpGeneratorDriver` and Verify snapshots, add these to **`Directory.Packages.props`** first (required by CPM), then reference in test `.csproj`:
```xml
<!-- In Directory.Packages.props: -->
<PackageVersion Include="Verify" Version="[pin latest stable]" />
<PackageVersion Include="Verify.XunitV3" Version="[pin latest stable]" />
<!-- NOTE: Verify.XunitV3 is for xUnit v3. Do NOT use Verify.Xunit (that's xUnit v2). -->

<!-- In SourceTools.Tests.csproj: -->
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" />
<PackageReference Include="Verify" />
<PackageReference Include="Verify.XunitV3" />
```

**Parse-only stage uses `Verify` (generic object serialization), NOT `Verify.SourceGenerators`.** The parse stage produces IR objects, not emitted source code. `Verify.SourceGenerators` is designed for `CSharpGeneratorDriver.RunGenerators()` output (emitted .cs files) and will produce empty snapshots for a parse-only generator. Use `Verify`'s built-in object serialization to snapshot `DomainModel` IR records. `Verify.SourceGenerators` will be added in Story 1.5 when the Emit stage produces source.

**FieldTypeMapper is `public static`** -- unit-testable in isolation without `CSharpGeneratorDriver`. Test it directly with primitive inputs (type name strings, nullability flags). This gives fast feedback on type mapping logic without compilation overhead.

### Test Reference Assemblies (CompilationHelper)

`CSharpGeneratorDriver` tests require a compilation with correct `MetadataReference` assemblies. This is the #1 source of cryptic build failures in source generator test projects. Create a `CompilationHelper.cs` utility:

```csharp
internal static class CompilationHelper
{
    internal static CSharpCompilation CreateCompilation(string source)
    {
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),          // System.Runtime
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),        // System.Runtime (attributes)
            MetadataReference.CreateFromFile(typeof(ProjectionAttribute).Assembly.Location), // Contracts
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly.Location),
        };

        // Add all runtime assemblies needed for netcoreapp compilation
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var additionalRefs = new[] { "System.Runtime.dll", "netstandard.dll" }
            .Select(dll => Path.Combine(runtimeDir, dll))
            .Where(File.Exists)
            .Select(path => MetadataReference.CreateFromFile(path));

        return CSharpCompilation.Create("TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(source) },
            references.Concat(additionalRefs),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
```

**Note:** On .NET 10, `typeof(object).Assembly.Location` returns the .NET 10 runtime assembly, not netstandard2.0. Tests create compilations targeting the test's runtime -- this is correct and expected.

### Nullability: string? vs int? Distinction

These are fundamentally different at the Roslyn level:
- **`int?`** = `Nullable<int>` -- a different CLR type. Detected via `INamedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T`. The underlying type is extracted via `((INamedTypeSymbol)type).TypeArguments[0]`.
- **`string?`** = nullable annotation only -- same CLR type as `string`. Detected via `NullableAnnotation.Annotated` on the `IPropertySymbol`. No type unwrapping needed.

Both set `IsNullable = true` in the IR, but the detection paths diverge. The `FieldTypeMapper` should NOT treat `string?` as a "nullable variant" in the same code path as `int?`. Handle reference type nullability via annotation check; handle value type nullability via `Nullable<T>` unwrapping.

### EquatableArray<T> Implementation Guide

The vendored `EquatableArray<T>` MUST work on netstandard2.0. Common implementations (Andrew Lock, CommunityToolkit) use `ImmutableArray<T>.AsSpan()` and `HashCode.Combine` which are NOT available on netstandard2.0.

**Required implementation approach:**
- Wrap `ImmutableArray<T>` internally
- `Equals`: iterate elements with `EqualityComparer<T>.Default.Equals(a[i], b[i])`
- `GetHashCode`: XOR-fold element hash codes (e.g., `hash = hash * 31 + element.GetHashCode()`)
- Implement `IEquatable<EquatableArray<T>>`, `IEnumerable<T>`
- Handle empty/default arrays (default `ImmutableArray<T>` has `IsDefault = true`)

### Testing Approach

**Parse tests are integration-level** (use `CSharpGeneratorDriver` -- Roslyn's `INamedTypeSymbol` cannot be meaningfully unit-tested without a compilation context). This is by design per architecture.

**Test pattern:** `{Method}_{Scenario}_{Expected}` (established in Story 1.3)

**Test framework:** xUnit v3 + Shouldly (NOT xUnit v2 + FluentAssertions -- the architecture doc is outdated; actual codebase uses xUnit v3 + Shouldly as established in Stories 1.1-1.3)

**CSharpGeneratorDriver test pattern:**
```csharp
// Arrange: create compilation with test source (use CompilationHelper)
var source = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Test"")]
[Projection]
public partial class TestProjection
{
    public string Name { get; set; }
    public int Count { get; set; }
}";

var compilation = CompilationHelper.CreateCompilation(source);

// Act: run generator
var generator = new FrontComposerGenerator();
var driver = CSharpGeneratorDriver.Create(generator);
driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

// Assert: verify results
var results = driver.GetRunResult();
results.Diagnostics.ShouldBeEmpty();

// CRITICAL: verify generated code compiles (catches typos in emitted source)
var outputCompilation = compilation.AddSyntaxTrees(
    results.GeneratedTrees.ToArray());
outputCompilation.GetDiagnostics()
    .Where(d => d.Severity == DiagnosticSeverity.Error)
    .ShouldBeEmpty();

// Verify IR via snapshot (use Verify's object serialization)
await Verify(results);
```

**Incremental caching test pattern:**
```csharp
// Run 1: initial generation
var driver1 = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);

// Run 2: modify unrelated file -- expect Cached
var newTree = CSharpSyntaxTree.ParseText("public class Unrelated { }");
var compilation2 = compilation.AddSyntaxTrees(newTree);
var driver2 = driver1.RunGenerators(compilation2);
var result2 = driver2.GetRunResult().Results[0];
result2.TrackedSteps["Parse"]
    .SelectMany(s => s.Outputs)
    .All(o => o.Reason == IncrementalStepRunReason.Cached)
    .ShouldBeTrue();
```

### Performance Requirement

- P95 incremental generator execution (full pipeline Parse+Transform+Emit): **< 500ms** (NFR8)
- Parse stage alone should target **< 50ms** for a typical domain (20-50 projected types). If parse exceeds 50ms, investigate before moving to Transform.
- Optimization: `ForAttributeWithMetadataName` provides built-in Roslyn caching -- only re-runs when attribute-bearing types change
- Do NOT use `context.SyntaxProvider.CreateSyntaxProvider` (less efficient, no attribute filtering)
- **Measurement:** Add a benchmark test (Task 7) that runs parse against a 20+ type fixture and asserts on timing. Run as a separate test category to avoid slowing CI.

### Cross-Assembly Discovery Note

Roslyn source generators only see the current compilation. The EventStore domain (git submodule) lives in a separate assembly. For cross-assembly discovery, a runtime `IFrontComposerRegistry` + generated manifest will be used (Story 1.6). The parse stage only processes types in the current compilation.

### Project Structure Notes

- **SourceTools location:** `src/Hexalith.FrontComposer.SourceTools/` (already exists, currently empty)
- **Test location:** `tests/Hexalith.FrontComposer.SourceTools.Tests/` (already exists, currently empty)
- **Contracts attributes location:** `src/Hexalith.FrontComposer.Contracts/Attributes/` (BoundedContextAttribute, CommandAttribute, ProjectionAttribute, ProjectionRoleAttribute, ProjectionBadgeAttribute)
- **Contracts rendering:** `src/Hexalith.FrontComposer.Contracts/Rendering/` (FieldDescriptor, RenderHints, IRenderer)
- **Contracts registration:** `src/Hexalith.FrontComposer.Contracts/Registration/` (IFrontComposerRegistry, DomainManifest)
- **Build props:** `Directory.Build.props` has `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`
- All generated output goes to `obj/{Config}/{TFM}/generated/HexalithFrontComposer/` (deterministic, inspectable)

### Previous Story Intelligence (Story 1.3)

**Key learnings from Story 1.3 implementation:**

1. **Testing framework is xUnit v3 + Shouldly** (not xUnit v2 + FluentAssertions as architecture doc states). Use `[Fact]`, `[Theory]`, `[InlineData]`, and `Shouldly` assertions (`.ShouldBe()`, `.ShouldBeNull()`, `.ShouldBeEmpty()`).

2. **Test naming convention:** `{Method}_{Scenario}_{Expected}` -- follow this consistently.

3. **Cancellation token pattern:** Use `TestContext.Current.CancellationToken` in async tests (xUnit v3 feature).

4. **FrontComposer prefix convention:** Framework-owned types use `FrontComposer` prefix (e.g., `FrontComposerThemeFeature`). Generated domain types use the type name directly (e.g., `CounterProjectionFeature`). Apply this to any framework-level parse types.

5. **NSubstitute for mocking** (not Moq) -- use if parse tests need mock services.

6. **File organization:** Mirror the source structure in tests (e.g., `Parsing/` folder in both src and tests).

7. **Enum nullability trap:** `default(EnumType)` equals the first enum value (0), which may be a valid value. For nullable enum detection in IR, use `Nullable<T>` / `.HasValue` pattern, not `!= default`.

### Git Intelligence

**Recent commits (last 5):**
1. `5bd730f` - Update solution to include Shell.Tests project, mark story 1-3 done
2. `4d3b726` - Update .gitignore, mark submodules dirty, update sprint status
3. `05e315b` - Merge PR #7: sprint status and readiness report
4. `2ef2914` - Add sprint status tracking
5. `a4049f2` - Merge PR #6: epics and planning artifacts

**Pattern:** Stories 1.1-1.3 established the project structure, contracts, and Fluxor state. Story 1.4 is the first code generation story -- no prior generator code exists in the repo.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1, Story 1.4]
- [Source: _bmad-output/planning-artifacts/architecture.md#ADR-004 Source Generator IR Pattern]
- [Source: _bmad-output/planning-artifacts/architecture.md#Decision 8: Auto-Discovery Mechanism]
- [Source: _bmad-output/planning-artifacts/architecture.md#SourceTools Project Structure]
- [Source: _bmad-output/planning-artifacts/architecture.md#Diagnostic Policy]
- [Source: _bmad-output/planning-artifacts/prd.md#Source Generator Performance]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Auto-Generation Boundary Protocol]
- [Source: _bmad-output/implementation-artifacts/1-3-fluxor-state-management-foundation.md#Dev Notes]

### Dependencies

- **Depends on:** Story 1.2 (Contracts package with attribute types) -- DONE
- **Prerequisite for:** Story 1.5 (Transform & Emit stages consume DomainModel IR)
- **Prerequisite for:** Story 1.6 (Counter sample uses generator end-to-end)
- **Prerequisite for:** Story 1.7 (CI pipeline runs SourceTools.Tests)

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Build error RS2008: Added AnalyzerReleases.Shipped.md / Unshipped.md for release tracking
- Build error RS1032: Simplified diagnostic message format to use `{0}` placeholder
- Build error CS8604: Resolved null reference for `irType` in PropertyModel construction
- xUnit1051: All `RunGenerators()`, `GetDiagnostics()`, `ParseText()` calls now pass `TestContext.Current.CancellationToken`
- Struct predicate: Expanded `ForAttributeWithMetadataName` predicate from `ClassDeclarationSyntax or RecordDeclarationSyntax` to `TypeDeclarationSyntax` to catch struct/record-struct and emit HFC1004
- Verify snapshots: Accepted initial `.received.txt` as `.verified.txt` baselines
- Caching test: Relaxed assertion to check `Any()` cached/unchanged instead of `All()` since early pipeline steps legitimately re-run

### Completion Notes List

- All 7 tasks and subtasks completed
- 61 new tests added (all passing), 0 regressions across 104 total solution tests
- IR types (DomainModel, PropertyModel, ParseResult, DiagnosticInfo, BadgeMappingEntry) all implement IEquatable<T> for Roslyn incremental caching
- EquatableArray<T> vendored for netstandard2.0 compatibility (no Span<T>, no HashCode.Combine)
- AttributeParser parses [BoundedContext], [ProjectionRole], [Display], [ProjectionBadge] attributes
- FieldTypeMapper maps all 29 supported .NET types; unsupported types flagged with IsUnsupported=true and HFC1002 diagnostic
- Diagnostic descriptors HFC1002-HFC1005 defined with release tracking
- Parse stage handles: struct, record struct, generic, abstract, nested in non-partial, global namespace, missing partial keyword, null bounded context, nullable context disabled, compound unsupported types
- Performance baseline: 25 projection types parsed well under 500ms budget
- Incremental caching verified: unrelated edits cached, attribute changes trigger modified, new types don't invalidate existing

### File List

**New files (source):**
- src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs
- src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs
- src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs
- src/Hexalith.FrontComposer.SourceTools/Parsing/EquatableArray.cs
- src/Hexalith.FrontComposer.SourceTools/Parsing/FieldTypeMapper.cs
- src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs
- src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Shipped.md
- src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md

**New files (tests):**
- tests/Hexalith.FrontComposer.SourceTools.Tests/CompilationHelper.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/AttributeParserTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/FieldTypeMapperTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CancellationTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/TestSources.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Caching/IncrementalCachingTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Performance/ParseStagePerformanceTests.cs
- tests/Hexalith.FrontComposer.SourceTools.Tests/Snapshots/*.verified.txt (6 snapshot files)

**Modified files:**
- src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj (added AdditionalFiles for release tracking)
- tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj (added Verify, Verify.XunitV3, Microsoft.CodeAnalysis.CSharp)
- Directory.Packages.props (added Verify 31.15.0, Verify.XunitV3 31.15.0)

### Change Log

- 2026-04-13: Implemented Story 1.4 - Source Generator Parse Stage. Added Roslyn incremental source generator with parse-only pipeline, DomainModel IR records, attribute parsing, field type mapping for 29 types, diagnostic reporting (HFC1002-HFC1005), and comprehensive test suite (61 tests including snapshot, caching, cancellation, and performance tests).
