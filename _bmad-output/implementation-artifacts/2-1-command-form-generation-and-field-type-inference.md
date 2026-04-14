# Story 2.1: Command Form Generation & Field Type Inference

Status: ready-for-dev

## Story

As a developer,
I want the source generator to produce form components from [Command]-annotated records with automatic field type inference and validation,
So that business users get correctly typed, validated input forms without manual component authoring.

## Acceptance Criteria

### AC1: Source Generator Emits Form Component

**Given** a record type annotated with `[Command]`
**When** the source generator runs
**Then** a Razor form component is emitted with input fields for each non-derivable property
**And** the generated file follows naming convention `{CommandName}Form.g.razor.cs`

### AC2: Field Type Inference Mapping

**Given** a generated command form with various property types
**When** the form renders
**Then** string properties render as `FluentTextInput`
**And** bool properties render as `FluentSwitch` (binary on/off UX per UX spec "toggle style")
**And** DateTime/DateOnly properties render as `FluentDatePicker<TValue>`
**And** enum properties render as `FluentSelect<TEnum, TEnum>` with humanized option labels (max 30 chars, truncated if longer)
**And** int/long properties render as `FluentTextInput` with `TextInputType.Number` (string-backing converter pattern)
**And** decimal/double/float properties render as `FluentTextInput` with `TextInputType.Number` + `InputMode.Decimal`
**And** Guid/ULID properties render as `FluentTextInput` with monospace CSS class
**And** unsupported types render as `FcFieldPlaceholder` with build-time warning HFC1002

> **CRITICAL v5 deviation from epics AC:** The epics reference v4 component names. Fluent UI Blazor v5 renamed/removed several form components. See Dev Notes for the complete v4-to-v5 mapping. The acceptance criteria above use the CORRECT v5 component names. The generated code MUST target v5 APIs.

### AC3: Label Resolution Chain & Accessibility

**Given** a generated command form
**When** field labels render
**Then** the label resolution chain applies: `[Display(Name)]` > `IStringLocalizer<TCommand>` (runtime) > humanized CamelCase > raw field name
**And** every field has an associated `<label>` element (via FluentUI v5's built-in `Label` property)
**And** required fields are visually marked (via FluentUI v5's `Required` property)
**And** validation messages use `aria-describedby` and `aria-live="polite"` for accessibility
**And** `FcFieldPlaceholder` is focusable in tab order with `role="status"` and `aria-label`
**And** form wrapper has `max-width: 720px; margin: 0 auto` per UX spec layout constraint
**And** form defaults to Comfortable density per UX spec density table

### AC4: FluentValidation Integration

**Given** FluentValidation rules exist for the command type
**When** the form is submitted with invalid input
**Then** validation messages appear inline via each component's `Message`/`MessageState` properties (Fluent UI v5 IFluentField pattern)
**And** the form does not submit until validation passes
**And** `EditContext` is wired via standard Blazor `<EditForm>` + `FluentValidationValidator` from `Blazored.FluentValidation`
**And** numeric string-to-number conversion failures surface as inline `MessageState.Error` messages

> **CRITICAL v5 deviation:** `FluentValidationMessage<T>` was REMOVED in v5. Validation messages are now displayed via the `Message`, `MessageState`, and `MessageCondition` properties built into every input component (IFluentField interface). The `<FluentEditForm>` component does NOT exist in v5 -- use standard Blazor `<EditForm>` wrapping Fluent UI input components.

### AC5: Stub ICommandService for v0.1

**Given** the v0.1 milestone scope (Epics 1-2 only, EventStore abstractions not yet available)
**When** command forms submit
**Then** a `StubCommandService : ICommandService` is used that simulates the command lifecycle (Idle -> Submitting -> Acknowledged -> Syncing -> Confirmed) with configurable delays **including an explicit Syncing transition between Acknowledged and Confirmed**
**And** the form component (not the stub) translates `CommandResult` into typed Fluxor action dispatches for the specific command type
**And** on a `Rejected` state, form field values are preserved (never cleared) per UX spec "error recovery preserves intent"
**And** the stub is replaceable with the real EventStore dispatcher (Story 5.1) without code changes
**And** the Counter sample demonstrates the full lifecycle against the stub

**References:** FR1, UX-DR22, UX-DR21 (label resolution), UX-DR3 (field placeholder), NFR30 (accessibility labels), UX spec form layout (720px), UX spec error recovery

---

## Tasks / Subtasks

### Task 0: Prerequisite Fixes from Epic 1 Deferred Work (AC: all)

- [ ] 0.1: Add `CorrelationId` to all generated Fluxor actions (ADR-008 gap from deferred-work.md). **BLAST RADIUS WARNING:** This touches `FluxorActionsEmitter.cs` AND updates existing `LoadRequestedAction`, `LoadedAction`, `LoadFailedAction` record signatures. Expected impact:
  - All existing `.verified.txt` snapshot files must be re-approved
  - `FluxorFeatureEmitter.cs` reducer references to actions may break
  - `RazorEmitter.cs` dispatcher calls (e.g., `Dispatcher.Dispatch(new LoadRequestedAction())`) must include CorrelationId
  - All existing reducer tests may break
  - Estimated effort: 4+ hours
- [ ] 0.2: Verify Fluent UI Blazor version compatibility. The MCP docs report `5.0.0.26098` as the released GA version. The project pins `5.0.0-rc.2-26098.1`. Check NuGet; if GA is available, update `Directory.Packages.props` to the stable version.
- [ ] 0.3: Add `FluentValidation` and `FluentValidation.AspNetCore` packages to `Directory.Packages.props`. Also evaluate `Blazored.FluentValidation` for `EditContext` integration.
  - **COMPATIBILITY CHECK REQUIRED:** Verify `Blazored.FluentValidation` supports Fluent UI Blazor v5. If not compatible or unmaintained, fall back to standard `DataAnnotationsValidator` + per-field `Message`/`MessageState` binding. Document the decision in the story before proceeding.
- [ ] 0.4: Create `DerivedFromAttribute` in `Contracts/Attributes/` to mark properties as derivable from context:
  ```csharp
  [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
  public sealed class DerivedFromAttribute : Attribute
  {
      public DerivedFromAttribute(DerivedFromSource source) { Source = source; }
      public DerivedFromSource Source { get; }
  }

  public enum DerivedFromSource { Context, User, Timestamp, MessageId }
  ```

### Task 1: Command IR Model & Parser (AC: 1)

- [ ] 1.1: Create `CommandModel` IR type in `SourceTools/Parsing/DomainModel.cs` as a **sealed class with manual `IEquatable<T>` and `GetHashCode()`** (matches existing IR pattern -- do NOT use `record` syntax):
  ```csharp
  public sealed class CommandModel : IEquatable<CommandModel>
  {
      public string TypeName { get; }
      public string Namespace { get; }
      public string BoundedContext { get; }
      public string BoundedContextDisplayLabel { get; }
      public EquatableArray<PropertyModel> Properties { get; }              // all fields
      public EquatableArray<PropertyModel> DerivableProperties { get; }     // auto-filled
      public EquatableArray<PropertyModel> NonDerivableProperties { get; } // user must fill
      // + ctor, Equals, GetHashCode
  }
  ```
  Reuse existing `PropertyModel` from `DomainModel.cs` (its `BadgeMappings` field stays empty for commands -- minor tech debt, tolerable for v0.1).

- [ ] 1.2: Create `CommandParseResult` sealed class (same pattern as existing `ParseResult`) in `DomainModel.cs`.

- [ ] 1.3: Add `ParseCommand()` method to `AttributeParser` (or create `CommandParser.cs`). Logic:
  - Extract `[Command]`-annotated type metadata via `INamedTypeSymbol`
  - Support BOTH record positional parameter syntax AND property syntax:
    - `record IncrementCommand(int Amount, string MessageId)` -- positional
    - `record IncrementCommand { public int Amount { get; init; } }` -- property syntax
  - Include **inherited properties from base types** (e.g., if `SubmitOrderCommand : BaseCommand` and `BaseCommand` has `MessageId`, include it)
  - Map each property using existing `FieldTypeMapper`
  - Classify properties as derivable/non-derivable:
    - Derivable if: has `[DerivedFrom]` attribute, OR name matches well-known derivable keys: `MessageId`, `CommandId`, `CorrelationId`, `TenantId`, `UserId`, `Timestamp`, `CreatedAt`, `ModifiedAt`
    - All others: non-derivable (user must provide)
  - Emit HFC1002 for unsupported types (already defined in DiagnosticDescriptors)
  - Emit **HFC1006** (NEW -- NOT HFC1003 which is already "Projection should be partial") if `[Command]` is missing required `MessageId` property
  - Resolve BoundedContext: check `[BoundedContext]` on the command type, then on its containing namespace/assembly
  - Respect label resolution: check for `[Display(Name="...")]` on properties, store in `PropertyModel.DisplayName`
  - **Subtask 1.3.a:** Update `AnalyzerReleases.Unshipped.md` with the new HFC1006 entry (mandatory per RS2008)

- [ ] 1.4: Wire `FrontComposerGenerator.cs` to call `ParseCommand()` for `[Command]`-annotated types via `ForAttributeWithMetadataName("Hexalith.FrontComposer.Contracts.Attributes.CommandAttribute")`. **Replace** the existing `commandMatches` boolean provider (which currently only counts types for HFC1001) with a full `IncrementalValuesProvider<CommandParseResult>`.

- [ ] 1.5: Unit tests for `ParseCommand` -- pure function tests with synthetic `INamedTypeSymbol` or `CSharpCompilation`. **Coverage matrix (explicit list):**

  | # | Type | # | Type | # | Type |
  |---|------|---|------|---|------|
  | 1 | `string` | 11 | `Guid` | 21 | `long?` |
  | 2 | `int` | 12 | `MyEnum` | 22 | `decimal?` |
  | 3 | `long` | 13 | `string?` | 23 | `double?` |
  | 4 | `decimal` | 14 | `int?` | 24 | `float?` |
  | 5 | `double` | 15 | `DateTime?` | 25 | `bool?` |
  | 6 | `float` | 16 | `DateOnly?` | 26 | `Guid?` |
  | 7 | `bool` | 17 | `TimeOnly?` | 27 | `MyEnum?` |
  | 8 | `DateTime` | 18 | `DateTimeOffset?` | 28 | `List<T>` (nested record, single level) |
  | 9 | `DateOnly` | 19 | `DateTimeOffset` | 29 | `IReadOnlyList<T>` |
  | 10 | `TimeOnly` | 20 | `int?` | | |

  Additional parser tests:
  - Derivable field classification (MessageId, TenantId, UserId, Timestamp, CorrelationId, CommandId, CreatedAt, ModifiedAt, `[DerivedFrom]`)
  - DisplayName from `[Display(Name)]` attribute
  - HFC1002 for unsupported types
  - HFC1006 for missing MessageId
  - Empty command (0 properties)
  - Record positional parameters
  - Record with property syntax
  - Inherited property from base record (MessageId on base, not on derived)
  - Empty enum (no members)
  - `[Flags]` enum (emit as unsupported OR document as single-select -- decide)
  - Nested enum (defined inside command class)

### Task 2: Command Form Transform (AC: 1, 2)

- [ ] 2.1: Create `FormFieldModel` in `SourceTools/Transforms/` as a **sealed class with manual IEquatable<T>**:
  ```csharp
  public sealed class FormFieldModel : IEquatable<FormFieldModel>
  {
      public string PropertyName { get; }
      public string DisplayLabel { get; }      // static chain result (DisplayName or humanized)
      public string TypeCategory { get; }      // TextInput, NumberInput, DecimalInput, DatePicker, Switch, Select, Placeholder, MonospaceText
      public string DotNetType { get; }        // e.g. "int", "DateTime", "MyEnum"
      public string FullyQualifiedType { get; } // for enum Items and generic type params
      public bool IsNullable { get; }
      public bool IsRequired { get; }
      public bool IsUnsupported { get; }
      public EquatableArray<string> EnumValues { get; } // humanized enum member names for Select
      // + ctor, Equals, GetHashCode
  }
  ```

- [ ] 2.2: Create `CommandFormModel` in `SourceTools/Transforms/` (sealed class):
  ```csharp
  public sealed class CommandFormModel : IEquatable<CommandFormModel>
  {
      public string TypeName { get; }
      public string Namespace { get; }
      public string FormClassName { get; }     // {CommandName}Form
      public string ButtonLabel { get; }       // "Send {Humanized CommandName}"
      public string CommandTypeFqn { get; }    // fully qualified command type
      public int NonDerivableFieldCount { get; }
      public EquatableArray<FormFieldModel> Fields { get; }
      public EquatableArray<FormFieldModel> DerivableFields { get; }
      // + ctor, Equals, GetHashCode
  }
  ```

- [ ] 2.3: Create `CommandFormTransform.cs` -- static pure function `CommandFormModel Transform(CommandModel model)`:
  - Map each property to `FormFieldModel` with correct `TypeCategory`:
    - `string` -> `TextInput`
    - `int`, `long` -> `NumberInput` (generates `TextInputType.Number`)
    - `decimal`, `double`, `float` -> `DecimalInput` (generates `TextInputType.Number` + `InputMode.Decimal`)
    - `bool` -> `Switch` (FluentSwitch for binary on/off UX per AC2)
    - `DateTime`, `DateTimeOffset`, `DateOnly` -> `DatePicker`
    - `TimeOnly` -> `TextInput` with `TextInputType.Time` hint (no FluentTimePicker in v5)
    - `enum` -> `Select` (populate `EnumValues` with humanized names, truncated to 30 chars)
    - `Guid`, ULID -> `MonospaceText` (generates `FluentTextInput` with CSS monospace class)
    - Unsupported -> `Placeholder`
  - Apply STATIC label resolution: `DisplayName ?? HumanizeCamelCase(PropertyName) ?? PropertyName`
    - Note: `IStringLocalizer` is NOT resolved at source generation time. It's resolved at RENDER time by the generated component (see Task 3.1).
  - Generate button label: `"Send {HumanizeCamelCase(CommandTypeName)}"`
  - Truncate enum humanized names at 30 chars per UX spec

- [ ] 2.4: Unit tests for `CommandFormTransform` -- pure function tests:
  - Each TypeCategory mapping (all 29 coverage matrix types)
  - Label resolution chain (with/without [Display])
  - Button label generation
  - Enum value humanization with 30-char truncation
  - Mixed derivable/non-derivable field separation
  - `IEquatable<T>` correctness: same input -> equal instances, different input -> unequal (3+ tests per model type)

### Task 3A: Command Form Emitter -- Core Structure (AC: 1)

- [ ] 3A.1: Create `CommandFormEmitter.cs` in `SourceTools/Emitters/`. Emits a `{CommandName}Form.g.razor.cs` partial class inheriting `ComponentBase, IDisposable`.

- [ ] 3A.2: Emitted class structure (pseudocode -- emitter produces C# `BuildRenderTree` calls, NOT Razor template syntax):
  ```csharp
  public partial class {CommandName}Form : ComponentBase, IDisposable
  {
      [Parameter] public {CommandTypeFqn}? InitialValue { get; set; }
      [Inject] private Fluxor.IState<{CommandName}LifecycleState> LifecycleState { get; set; } = default!;
      [Inject] private Fluxor.IDispatcher Dispatcher { get; set; } = default!;
      [Inject] private Hexalith.FrontComposer.Contracts.Communication.ICommandService CommandService { get; set; } = default!;
      [Inject(Key = null)] private Microsoft.Extensions.Localization.IStringLocalizer<{CommandTypeFqn}>? Localizer { get; set; }

      private {CommandTypeFqn} _model = new();
      private EditContext? _editContext;

      protected override void OnInitialized()
      {
          _model = InitialValue ?? new();
          _editContext = new EditContext(_model);
          LifecycleState.StateChanged += OnStateChanged;
      }

      private void OnStateChanged(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

      private string ResolveLabel(string propertyName, string staticLabel)
          => Localizer?[propertyName].Value ?? staticLabel;

      private async Task OnValidSubmitAsync()
      {
          var correlationId = Guid.NewGuid().ToString();
          Dispatcher.Dispatch(new {CommandName}Actions.SubmittedAction(correlationId, _model));
          var result = await CommandService.DispatchAsync(_model);
          Dispatcher.Dispatch(new {CommandName}Actions.AcknowledgedAction(correlationId, result.MessageId));
          // Syncing/Confirmed/Rejected dispatched by stub via its own mechanism (Task 5)
      }

      public void Dispose()
      {
          LifecycleState.StateChanged -= OnStateChanged;
      }

      protected override void BuildRenderTree(RenderTreeBuilder __builder) { /* see Task 3B */ }
  }
  ```

- [ ] 3A.3: Form-level accessibility and layout (emitted in `BuildRenderTree` via `RenderTreeBuilder`):
  - Wrap form in a `<div style="max-width: 720px; margin: 0 auto;">` per UX spec layout
  - Use standard Blazor `<EditForm>` (NOT `<FluentEditForm>` -- does not exist in v5) with `EditContext="_editContext"` and `OnValidSubmit="OnValidSubmitAsync"`
  - Add `<FluentValidationValidator />` (from Blazored.FluentValidation) OR `<DataAnnotationsValidator />` fallback based on Task 0.3 decision
  - Form element attributes: `aria-label="Send {CommandName} command form"`
  - Emit `seq++` pattern in `BuildRenderTree` with `#pragma warning disable ASP0006` / `#pragma warning restore ASP0006` OR use explicit sequence constants

### Task 3B: Command Form Emitter -- Field Rendering (AC: 2)

- [ ] 3B.1: Emitter must generate `builder.OpenComponent<T>()` / `builder.AddAttribute()` / `builder.CloseComponent()` calls for each field. **Emission patterns** (as they appear in emitted C# code, NOT Razor):

  | Field Type | Emitted C# (in BuildRenderTree) |
  |------------|----------------------------------|
  | `string` | `builder.OpenComponent<FluentTextInput>(seq++); builder.AddAttribute(seq++, "Value", _model.{Prop}); builder.AddAttribute(seq++, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => _model.{Prop} = v ?? string.Empty)); builder.AddAttribute(seq++, "Label", ResolveLabel("{Prop}", "{StaticLabel}")); builder.AddAttribute(seq++, "Required", {IsRequired}); builder.CloseComponent();` |
  | `int`, `long` | Same as above but with backing string field `_{prop}String` + parse-failure detection + `TextInputType="TextInputType.Number"`. See 3B.2 below. |
  | `decimal`, `double`, `float` | Same as numeric but add `InputMode="TextInputMode.Decimal"` AND `TextInputType="TextInputType.Number"`. |
  | `bool` | `builder.OpenComponent<FluentSwitch>(seq++); builder.AddAttribute(seq++, "Value", _model.{Prop}); builder.AddAttribute(seq++, "ValueChanged", EventCallback.Factory.Create<bool>(this, v => _model.{Prop} = v)); builder.AddAttribute(seq++, "Label", ResolveLabel(...)); builder.CloseComponent();` |
  | `DateTime` | `builder.OpenComponent<FluentDatePicker<DateTime>>(seq++); ... builder.CloseComponent();` |
  | `DateTime?` | `builder.OpenComponent<FluentDatePicker<DateTime?>>(seq++); ... builder.CloseComponent();` |
  | `DateOnly` / `DateOnly?` | Same pattern with `DateOnly` / `DateOnly?` |
  | `TimeOnly` | `FluentTextInput` with `TextInputType="TextInputType.Time"` |
  | `enum` | `builder.OpenComponent<FluentSelect<{EnumFqn}, {EnumFqn}>>(seq++); builder.AddAttribute(seq++, "Items", global::System.Enum.GetValues<{EnumFqn}>()); builder.AddAttribute(seq++, "OptionText", (Func<{EnumFqn}, string>)(e => TruncateAt30(Humanize(e.ToString())))); ... ` |
  | `Guid`, ULID | `FluentTextInput` + `builder.AddAttribute(seq++, "Class", "fc-monospace");` |
  | Unsupported | `builder.OpenComponent<FcFieldPlaceholder>(seq++); builder.AddAttribute(seq++, "FieldName", "{Prop}"); builder.AddAttribute(seq++, "TypeName", "{DotNetType}"); builder.CloseComponent();` |

- [ ] 3B.2: **Numeric string-backing converter with parse-failure feedback.** For each numeric field, emit:
  ```csharp
  private string? _{prop}String;
  private string? _{prop}ParseError;

  private void On{Prop}Changed(string? value)
  {
      _{prop}String = value;
      if (string.IsNullOrWhiteSpace(value))
      {
          _model.{Prop} = default;
          _{prop}ParseError = null;
          return;
      }
      if ({TypeName}.TryParse(value, System.Globalization.CultureInfo.CurrentCulture, out var parsed))
      {
          _model.{Prop} = parsed;
          _{prop}ParseError = null;
      }
      else
      {
          _{prop}ParseError = "Invalid number format.";
      }
  }
  ```
  And bind: `builder.AddAttribute(seq++, "Message", _{prop}ParseError);` `builder.AddAttribute(seq++, "MessageState", _{prop}ParseError != null ? Microsoft.FluentUI.AspNetCore.Components.MessageState.Error : Microsoft.FluentUI.AspNetCore.Components.MessageState.None);`

- [ ] 3B.3: Submit button emission:
  ```csharp
  builder.OpenComponent<FluentButton>(seq++);
  builder.AddAttribute(seq++, "Type", ButtonType.Submit);
  builder.AddAttribute(seq++, "Appearance", Appearance.Primary);
  builder.AddAttribute(seq++, "Disabled", LifecycleState.Value.State != CommandLifecycleState.Idle);
  builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(__b => __b.AddContent(seq++, "{ButtonLabel}")));
  builder.CloseComponent();
  ```

### Task 3C: Validation Wiring (AC: 4)

- [ ] 3C.1: Based on Task 0.3 decision, wire validation:
  - **IF `Blazored.FluentValidation` is v5-compatible:** Emit `<FluentValidationValidator />` inside `<EditForm>` wrapper.
  - **IF NOT:** Emit `<DataAnnotationsValidator />` and document that FluentValidation rules will not be auto-applied. The adopter must use DataAnnotations attributes on commands until Blazored catches up.

- [ ] 3C.2: Per-field validation message binding. Every field (except unsupported placeholders) must bind:
  - `Message` = `_editContext.GetValidationMessages(() => _model.{Prop}).FirstOrDefault() ?? _{prop}ParseError`
  - `MessageState` = `Error` if message exists, `None` otherwise
  - `aria-describedby` linking to a hidden message span

- [ ] 3C.3: Form-level `<FluentValidationSummary />` at the top of the form for cross-field errors (confirmed exists in v5).

### Task 3D: FcFieldPlaceholder Component (AC: 2, 3)

- [ ] 3D.1: Create `FcFieldPlaceholder.razor` in `Shell/Components/Rendering/` (directory must be created):
  ```razor
  @namespace Hexalith.FrontComposer.Shell.Components.Rendering
  @inject NavigationManager Nav

  <FluentCard role="status"
              aria-label="@($"{FieldName} requires custom renderer")"
              tabindex="0"
              class="fc-field-placeholder @(IsDevMode ? "fc-field-placeholder-dev" : "")"
              Style="border: 2px dashed var(--neutral-stroke-rest); padding: 12px;">
      <FluentIcon Value="@(new Icons.Regular.Size20.Warning())" Color="Color.Warning" />
      <span><strong>@FieldName</strong> (@TypeName) -- This field requires a custom renderer.</span>
      <FluentAnchor Href="https://hexalith.dev/docs/customization-gradient" Appearance="Appearance.Hypertext">
          Learn how to customize this field
      </FluentAnchor>
  </FluentCard>

  @code {
      [Parameter, EditorRequired] public string FieldName { get; set; } = default!;
      [Parameter, EditorRequired] public string TypeName { get; set; } = default!;
      [Parameter] public bool IsDevMode { get; set; } = false;
  }
  ```
  Dev-mode style adds a red-dashed border (CSS class `fc-field-placeholder-dev`). Accessibility: `role="status"`, `aria-label`, `tabindex="0"` for tab-order continuity per UX spec.

- [ ] 3D.2: Add CSS for `.fc-field-placeholder-dev` and `.fc-monospace` classes in a shared Shell stylesheet (or inline `<style>` in `FrontComposerShell.razor`).

### Task 3E: Emitter Tests -- Snapshot Coverage (AC: 1, 2)

- [ ] 3E.1: Snapshot tests for `CommandFormEmitter` -- golden file `.verified.txt` comparisons in `SourceTools.Tests/Emitters/Snapshots/`:
  1. Single string field command
  2. Multi-field command (string, int, bool, DateTime, enum) -- "typical" command
  3. **Kitchen sink: all 29 field types in one command** (parallel to `AllFieldTypesProjection` pattern used for projection tests)
  4. Command with 0 non-derivable fields (button-only form)
  5. Command with unsupported type (triggers FcFieldPlaceholder)
  6. Command with `[Display(Name)]` overrides on fields
  7. Command with nullable types (DateTime?, int?, enum?)
  8. Command with numeric fields (verifies string-backing converter snapshot)
  9. Command with ALL unsupported fields (all placeholders + submit button)
  10. Command with `[Flags]` enum (verifies the decision made in Task 1.5)
  11. Command with record positional parameters
  12. Command with inherited property from base record

- [ ] 3E.2: Parseability test: `EmittedCode_ParsesAsValidCSharp` -- every emitted `.g.razor.cs` file must parse without syntax errors via `CSharpSyntaxTree.ParseText()`. Follow existing pattern from `RazorEmitterTests.cs`.

- [ ] 3E.3: Determinism test: run the emitter twice on identical input, assert byte-identical output.

### Task 4: Command Fluxor Actions & State (AC: 1, 5)

- [ ] 4.1: Create `CommandFluxorTransform.cs` in `SourceTools/Transforms/` (sealed class `CommandFluxorModel`). Transform: `CommandFluxorModel Transform(CommandModel model)` -- purely derives names.

- [ ] 4.2: Create `CommandFluxorActionsEmitter.cs` in `SourceTools/Emitters/`. **Design decision:** Commands use a nested-class wrapper pattern (new convention for commands, not for projections):
  ```csharp
  // {CommandName}Actions.g.cs
  public static class {CommandName}Actions
  {
      public sealed record SubmittedAction(string CorrelationId, {CommandFqn} Command);
      public sealed record AcknowledgedAction(string CorrelationId, string MessageId);
      public sealed record SyncingAction(string CorrelationId);
      public sealed record ConfirmedAction(string CorrelationId);
      public sealed record RejectedAction(string CorrelationId, string Reason, string Resolution);
  }
  ```
  Per architecture naming: actions always past-tense, always include CorrelationId (ADR-008).

- [ ] 4.3: Create `CommandFluxorFeatureEmitter.cs` in `SourceTools/Emitters/`. Emits:
  ```csharp
  public sealed record {CommandName}LifecycleState(
      Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState State,
      string? CorrelationId,
      string? MessageId,
      string? Error,
      string? Resolution);

  public class {CommandName}LifecycleFeature : Fluxor.Feature<{CommandName}LifecycleState>
  {
      public override string GetName() => nameof({CommandName}LifecycleState); // use nameof per existing pattern
      protected override {CommandName}LifecycleState GetInitialState()
          => new(Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState.Idle,
                 null, null, null, null);
  }

  public static class {CommandName}Reducers
  {
      [Fluxor.ReducerMethod]
      public static {CommandName}LifecycleState OnSubmitted(
          {CommandName}LifecycleState state, {CommandName}Actions.SubmittedAction action)
          => state with { State = CommandLifecycleState.Submitting, CorrelationId = action.CorrelationId };

      [Fluxor.ReducerMethod]
      public static {CommandName}LifecycleState OnAcknowledged(
          {CommandName}LifecycleState state, {CommandName}Actions.AcknowledgedAction action)
          => state with { State = CommandLifecycleState.Acknowledged, MessageId = action.MessageId };

      [Fluxor.ReducerMethod]
      public static {CommandName}LifecycleState OnSyncing(
          {CommandName}LifecycleState state, {CommandName}Actions.SyncingAction action)
          => state with { State = CommandLifecycleState.Syncing };

      [Fluxor.ReducerMethod]
      public static {CommandName}LifecycleState OnConfirmed(
          {CommandName}LifecycleState state, {CommandName}Actions.ConfirmedAction action)
          => state with { State = CommandLifecycleState.Confirmed };

      [Fluxor.ReducerMethod]
      public static {CommandName}LifecycleState OnRejected(
          {CommandName}LifecycleState state, {CommandName}Actions.RejectedAction action)
          => state with { State = CommandLifecycleState.Rejected, Error = action.Reason, Resolution = action.Resolution };
          // NOTE: Rejected state does NOT clear form (form state is on the form component, not in Fluxor)
  }
  ```
  Note: `CommandLifecycleState` is **ephemeral** per architecture -- not persisted to `IStorageService`. Evicted on terminal state is handled by the subscriber (form component) resetting to Idle after Confirmed/Rejected is acknowledged by the user.

- [ ] 4.4: Unit tests: snapshot tests for generated Fluxor types, state transition correctness, `IEquatable<T>` correctness for `CommandFluxorModel`.

### Task 5: Stub ICommandService Implementation (AC: 5)

- [ ] 5.1: Create `StubCommandService.cs` in `Shell/Services/` (directory must be created). **The stub does NOT dispatch Fluxor actions** -- it only simulates the HTTP round-trip. The form component owns action dispatching.
  ```csharp
  public class StubCommandService : ICommandService
  {
      private readonly StubCommandServiceOptions _options;

      public StubCommandService(StubCommandServiceOptions options) { _options = options; }

      public async Task<CommandResult> DispatchAsync<TCommand>(
          TCommand command, CancellationToken ct = default) where TCommand : class
      {
          // Simulate network round-trip to EventStore
          await Task.Delay(_options.AcknowledgeDelayMs, ct);
          var messageId = Guid.NewGuid().ToString();
          if (_options.SimulateRejection)
          {
              throw new CommandRejectedException(
                  _options.RejectionReason ?? "Simulated rejection",
                  _options.RejectionResolution ?? "Adjust input and retry");
          }
          return new CommandResult(messageId, "acknowledged");
      }
  }
  ```
  The form component (Task 3A.2) then dispatches the `Syncing` and `Confirmed` actions after the acknowledged result returns, simulating the SignalR catch-up phase with another configurable delay.

- [ ] 5.2: Create `StubCommandServiceOptions.cs`:
  ```csharp
  public sealed record StubCommandServiceOptions(
      int AcknowledgeDelayMs = 100,
      int SyncingDelayMs = 100,
      int ConfirmDelayMs = 200,
      bool SimulateRejection = false,
      string? RejectionReason = null,
      string? RejectionResolution = null);
  ```

- [ ] 5.3: Register in DI via `AddHexalithFrontComposer()` as `services.AddScoped<ICommandService, StubCommandService>()` with `services.TryAddSingleton(new StubCommandServiceOptions())`. Must be replaceable -- when EventStore package is available (Story 5.1), its `AddHexalithEventStore()` call overrides with real implementation.

- [ ] 5.4: Create `CommandRejectedException` in `Contracts/Communication/`:
  ```csharp
  public class CommandRejectedException : Exception
  {
      public string Resolution { get; }
      public CommandRejectedException(string reason, string resolution) : base(reason)
      {
          Resolution = resolution;
      }
  }
  ```

- [ ] 5.5: Unit tests for `StubCommandService`:
  - Verify acknowledgment returns `CommandResult` with non-null `MessageId`
  - Verify rejection throws `CommandRejectedException`
  - Verify cancellation token is respected during delay
  - Verify configurable delays are honored
  - **IMPORTANT:** Tests use `AcknowledgeDelayMs = 0` etc. to avoid flakiness under CI load. Real delays are only for Counter sample smoke test.

### Task 6: Wire Generator Pipeline (AC: 1)

- [ ] 6.1: Update `FrontComposerGenerator.cs` (NOT `FrontComposerPipeline.cs` -- that file does NOT exist; the generator logic lives in `FrontComposerGenerator.cs`):
  - Replace the existing `commandMatches` boolean `IncrementalValuesProvider<bool>` with a full `IncrementalValuesProvider<CommandParseResult>`
  - Add `RegisterSourceOutput` calls for: command form `.g.razor.cs`, command Fluxor feature `.g.cs`, command Fluxor actions `.g.cs`
  - Ensure both projection and command pipelines coexist without interference

- [ ] 6.2: Update `RegistrationEmitter.cs` to populate `DomainManifest.Commands` list. **Architectural concern:** The current per-type emission pipeline makes cross-type aggregation (collecting all commands in a bounded context into one manifest) non-trivial. Resolution strategy:
  - Option A (RECOMMENDED for v0.1): Emit per-command registration entries that the runtime `FrontComposerRegistry.RegisterDomain()` aggregates at startup (current pattern for projections).
  - Option B: Use `IncrementalValuesProvider.Collect()` to gather all commands into a single array before emission, then emit one aggregated registration file per bounded context. This is a larger refactor.
  - Decide and document the choice in the commit message and in the story's Completion Notes.

- [ ] 6.3: Integration test in `SourceTools.Tests/Integration/`: full generator driver test with a `[Command]`-annotated record alongside a `[Projection]`-annotated record in the same namespace. Verify all output files are generated and compile without errors.

- [ ] 6.4: Integration test: command and projection with same `TypeName` in different namespaces (collision resilience -- should use namespace-qualified hint names per Story 1-5 learnings).

### Task 7: Counter Sample Integration (AC: 5)

- [ ] 7.1: After generator changes, `IncrementCommand` should auto-generate:
  - `IncrementCommandForm.g.razor.cs`
  - `IncrementCommandLifecycleFeature.g.cs`
  - `IncrementCommandActions.g.cs`
  Verify these compile.

- [ ] 7.2: Integrate the command form into the existing Counter page alongside the DataGrid. Form has one field (`Amount` -- int) plus the "Send Increment Counter" button.

- [ ] 7.3: Wire `StubCommandService` in Counter.Web's `Program.cs`. Demonstrate full lifecycle: click submit -> button disabled + progress ring -> acknowledged -> syncing -> confirmed -> reset to Idle.

- [ ] 7.4: Wire the form dispatch handler to call `Dispatcher.Dispatch(new IncrementCommandActions.SyncingAction(correlationId))` and `...ConfirmedAction(correlationId)` after configurable delays, simulating SignalR callback.

- [ ] 7.5: Smoke test: run `dotnet watch` on Counter.Web, modify `IncrementCommand` (add a property), verify the form updates via incremental rebuild (< 500ms generator budget per NFR).

### Task 8: Test Infrastructure & bUnit Coverage (AC: 3, 4)

- [ ] 8.1: Extend `GeneratedComponentTestBase.cs` in `Shell.Tests` (or create if it doesn't exist -- verify first):
  - Register `ICommandService` mock via `Services.AddSingleton(Substitute.For<ICommandService>())`
  - Register a test `IValidator<T>` via NSubstitute when needed
  - Register `IStringLocalizer<T>` returning keys as values (pass-through localizer)
  - Keep `JSInterop.Mode = JSRuntimeMode.Loose`

- [ ] 8.2: Create `IncrementCommandValidator : AbstractValidator<IncrementCommand>` in Counter.Domain. Rule: `Amount` must be > 0. Register in Counter.Web `Program.cs`.

- [ ] 8.3: bUnit tests in `Shell.Tests/Components/CommandFormRenderTests.cs` -- **explicit test list** (10 tests minimum):
  1. `Form_RendersWithSingleStringField_ShowsFluentTextInput`
  2. `Form_RendersWithNumericField_ShowsTextInputWithNumberType`
  3. `Form_RendersWithBoolField_ShowsFluentSwitch`
  4. `Form_RendersWithEnumField_ShowsFluentSelect`
  5. `Form_RendersWithDateField_ShowsFluentDatePicker`
  6. `Form_RendersWithUnsupportedType_ShowsFcFieldPlaceholder`
  7. `Form_SubmitButton_DisabledDuringSubmittingState`
  8. `Form_OnValidSubmit_DispatchesSubmittedAction`
  9. `Form_WithInvalidFluentValidationRule_ShowsInlineError`
  10. `Form_OnRejected_PreservesFieldValues` (UX requirement)

- [ ] 8.4: Numeric converter edge case tests in `Shell.Tests/Components/NumericConverterTests.cs`:
  1. Empty string -> `_{prop}ParseError == null`, model value is default
  2. Whitespace -> same as empty
  3. Valid integer -> model updated, no error
  4. Overflow (`int.MaxValue + 1`) -> parse error shown
  5. Negative number -> accepted
  6. Leading zeros -> accepted
  7. Non-numeric ("abc") -> parse error shown
  8. Locale-sensitive decimal separator (invariant vs fr-FR culture) -> parsed correctly
  9. Null input -> same as empty

- [ ] 8.5: Form abandonment dirty-state tracking infrastructure (forward dependency on Story 2-2). Add a `bool IsDirty` property to the form component, updated on any field change. Story 2-2 will use this for the 30-second abandonment warning. This prevents retrofitting later.

- [ ] 8.6: FcFieldPlaceholder accessibility tests in `Shell.Tests/Components/FcFieldPlaceholderTests.cs`:
  - Has `role="status"` attribute
  - Has `aria-label` with field name
  - Has `tabindex="0"` (focusable in tab order)
  - Has `FluentAnchor` documentation link
  - Dev-mode prop adds `fc-field-placeholder-dev` CSS class

- [ ] 8.7: Property-based tests (FsCheck) -- add `FsCheck.Xunit.v3` to Directory.Packages.props and add tests:
  1. `CamelCaseHumanizer_RoundtripProperty` -- for any valid C# identifier, humanized output is non-null, non-empty, no leading/trailing whitespace
  2. `NumericConverter_IntRoundtrip` -- for any `int`, `int.TryParse(value.ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture, out var parsed) && parsed == value` holds
  3. `CommandModel_Equality_Symmetric` -- for any two `CommandModel` instances, `a.Equals(b) == b.Equals(a)` and equal instances have equal hash codes

### Task 9: Validation & Final QA (AC: all)

- [ ] 9.1: Run full test suite: expect ~80-95 new tests (up from current 202 baseline). Verify all pass.
- [ ] 9.2: Verify `dotnet build --configuration Release` succeeds with no warnings (TreatWarningsAsErrors=true).
- [ ] 9.3: Manual smoke test on Counter.Web: submit form with valid input, verify full lifecycle animation; submit with invalid input, verify error message; try parse-failure ("abc" in Amount), verify inline error.
- [ ] 9.4: axe-core accessibility scan (if available) on Counter.Web command form page. Goal: zero serious/critical violations.
- [ ] 9.5: Update `deferred-work.md` -- mark CorrelationId gap and DomainManifest.Commands gap as resolved.

---

## Dev Notes

### CRITICAL DECISIONS (made during review -- do NOT revisit)

1. **IR types: sealed class with manual `IEquatable<T>`** -- matches existing `DomainModel`, `PropertyModel`, `ParseResult` pattern. Do NOT use `record` syntax.
2. **Diagnostic ID for missing MessageId: HFC1006** -- NOT HFC1003 (which is already "Projection should be partial"). Update `AnalyzerReleases.Unshipped.md`.
3. **Form wrapper: standard Blazor `<EditForm>`** -- NOT `<FluentEditForm>` (does not exist in v5).
4. **bool fields render as `FluentSwitch`** -- binary on/off UX per spec "toggle style," not `FluentCheckbox` (which is tri-state opt-in UX).
5. **Form component dispatches Fluxor actions, not the stub** -- stub only simulates HTTP. Form handler translates `CommandResult` into typed `SubmittedAction`/`AcknowledgedAction`/`SyncingAction`/`ConfirmedAction`/`RejectedAction` dispatches.
6. **StubCommandService throws `CommandRejectedException`** on rejection. Form handler catches and dispatches `RejectedAction` with reason and resolution.
7. **IStringLocalizer is runtime-resolved** (inside the generated component), not source-gen-time. Generator emits a `ResolveLabel()` helper that does `Localizer?[propertyName].Value ?? staticLabel`.
8. **5-state lifecycle INCLUDES Syncing** -- Idle -> Submitting -> Acknowledged -> **Syncing** -> Confirmed/Rejected. Stub simulates Syncing via a second delay after the HTTP call returns (implemented in form component, not stub).
9. **Form state preservation on Rejected** -- model fields are NOT cleared on rejection. Only Fluxor state resets on user dismiss.
10. **File paths: `FrontComposerGenerator.cs`** is the pipeline entry point, NOT `FrontComposerPipeline.cs` (does not exist).

### CRITICAL: Fluent UI Blazor v5 Breaking Changes for Form Components

| Epics/UX Reference (v4) | Actual v5 Component | Breaking Change |
|--------------------------|---------------------|-----------------|
| `FluentTextField` | `FluentTextInput` | Renamed; string-based binding |
| `FluentNumberField` | `FluentTextInput` + `TextInputType.Number` | REMOVED; no native numeric binding |
| `FluentCheckbox` | `FluentSwitch` for command booleans | Checkbox vs Switch -- use Switch for binary UX |
| `FluentDatePicker` | `FluentDatePicker<TValue>` | Now generic; must specify TValue |
| `FluentSelect` | `FluentSelect<TOption, TValue>` | Now requires TWO type parameters |
| `FluentValidationMessage` | IFluentField `Message`/`MessageState` properties | REMOVED entirely |
| `FluentEditForm` | Standard Blazor `<EditForm>` | Does NOT exist in v5 |

**Numeric field pattern:** `FluentTextInput` is string-based (`Value` is `string?`). Emitter generates per-field backing `string?` + parse-failure detection + inline error message via `MessageState.Error`.

**Validation pattern:** All v5 inputs implement `IFluentField` with built-in `Message`, `MessageState`, `MessageCondition`. Use these instead of removed `FluentValidationMessage<T>`. Plus form-level `<FluentValidationSummary />` for cross-field errors.

### Architecture Constraints (MUST FOLLOW)

1. **SourceTools (`netstandard2.0`) must NEVER reference Fluxor, FluentUI, or Shell.** All external types emitted as fully-qualified name strings in generated code.
2. **Three-stage pipeline:** Parse -> Transform -> Emit. Parse and Transform are pure functions. Emit produces string source code. (ADR-004)
3. **IEquatable<T> on all IR/output models** (sealed class pattern) -- required for Roslyn incremental caching.
4. **Deterministic output** -- same input must produce byte-identical source code.
5. **All generated files** end in `.g.cs` or `.g.razor.cs`, live in `obj/` not `src/`.
6. **Hint names must be namespace-qualified:** `{Namespace}.{TypeName}.g.razor.cs` (learned from Story 1-5 review).
7. **Fluxor actions: past-tense naming, always include CorrelationId** (ADR-008).
8. **No FluxorComponent base class** -- use `IState<T>` inject + explicit subscribe/dispose.
9. **AnalyzerReleases.Unshipped.md** must be updated for any new diagnostic (RS2008 build error).
10. **CommandLifecycleState is ephemeral** -- not persisted to IStorageService. Evicted on terminal state.
11. **`#pragma warning disable ASP0006`** around `BuildRenderTree` sequence increments in emitted code, OR use explicit sequence constants.

### Existing Code to REUSE (Do NOT Reinvent)

- `FieldTypeMapper.cs` -- already maps all .NET types
- `PropertyModel` -- has Name, TypeName, IsNullable, IsUnsupported, DisplayName, BadgeMappings (leave empty for commands)
- `CamelCaseHumanizer` -- for labels and button text
- `EquatableArray<T>` -- already vendored
- `ICommandService` + `CommandResult` -- contracts already exist
- `CommandLifecycleState` enum -- Idle, Submitting, Acknowledged, Syncing, Confirmed, Rejected
- `ICommandLifecycleTracker` -- interface exists (not needed for this story)
- `DiagnosticDescriptors.cs` -- HFC1002 already defined. Add HFC1006 for missing MessageId.
- `FrontComposerRegistry` -- has `DomainManifest.Commands` list (currently always empty)
- Existing pattern: `RazorEmitterTests.EmittedCode_ParsesAsValidCSharp` for parse-check tests

### Files That Must Be Created (Directories Too)

Directories to create:
- `src/Hexalith.FrontComposer.Shell/Services/`
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/` (if not present)

New files (counting across all tasks):
- Contracts: `DerivedFromAttribute.cs`, `CommandRejectedException.cs`
- SourceTools/Parsing: extend `DomainModel.cs` with `CommandModel`, `CommandParseResult`; extend `AttributeParser.cs` with `ParseCommand()`
- SourceTools/Transforms: `CommandFormModel.cs`, `FormFieldModel.cs`, `CommandFormTransform.cs`, `CommandFluxorModel.cs`, `CommandFluxorTransform.cs`
- SourceTools/Emitters: `CommandFormEmitter.cs`, `CommandFluxorActionsEmitter.cs`, `CommandFluxorFeatureEmitter.cs`
- SourceTools/Diagnostics: update `DiagnosticDescriptors.cs` with HFC1006
- Shell/Services: `StubCommandService.cs`, `StubCommandServiceOptions.cs`
- Shell/Components/Rendering: `FcFieldPlaceholder.razor`
- Directory.Packages.props: add FluentValidation, Blazored.FluentValidation (pending compat check), FsCheck.Xunit.v3
- AnalyzerReleases.Unshipped.md: HFC1006 entry
- Counter.Domain: `IncrementCommandValidator.cs`

### Naming Convention Reference

| Element | Pattern | Example |
|---------|---------|---------|
| Generated form partial | `{CommandName}Form.g.razor.cs` | `IncrementCommandForm.g.razor.cs` |
| Generated Fluxor feature | `{CommandName}LifecycleFeature.g.cs` | `IncrementCommandLifecycleFeature.g.cs` |
| Generated Fluxor actions | `{CommandName}Actions.g.cs` | `IncrementCommandActions.g.cs` |
| Fluxor state record | `{CommandName}LifecycleState` | `IncrementCommandLifecycleState` |
| Action wrapper class (new convention for commands) | `{CommandName}Actions` (static class) | `IncrementCommandActions` |
| Action records (nested) | `{CommandName}Actions.{Verb}Action` | `IncrementCommandActions.SubmittedAction` |
| Button label | `"Send {Humanized CommandName}"` | `"Send Increment Counter"` |
| Feature name | `nameof({CommandName}LifecycleState)` | `"IncrementCommandLifecycleState"` |

### Fluxor State Shape (Command Lifecycle vs. Projection)

**Projection (existing):**
```csharp
record {Type}State(bool IsLoading, IReadOnlyList<{Type}>? Items, string? Error)
```

**Command Lifecycle (NEW -- this story):**
```csharp
record {CommandName}LifecycleState(
    CommandLifecycleState State,
    string? CorrelationId,
    string? MessageId,
    string? Error,
    string? Resolution)
```

Command state is **ephemeral** (not persisted), scoped per-command-type. On terminal state, user dismissal resets to Idle -- form state (model values) is NOT reset; the user can retry without re-entering data.

### Testing Standards

- **xUnit v3** (3.2.2) with `Verify.XunitV3` for snapshots, `Shouldly` for assertions, `NSubstitute` for mocks, `FsCheck.Xunit.v3` for property-based tests (NEW)
- **bUnit** 2.7.2 for rendered Blazor components
- **Parse/Transform:** pure function tests (90% of tests). Fast, no compilation context needed.
- **Emit:** snapshot/golden-file tests via `.verified.txt` files (10% of tests).
- **bUnit:** for rendered form component tests against Fluent UI v5 (10+ tests per story 2-1).
- **Test naming:** `{Method}_{Scenario}_{Expected}` -- pick one per project, don't mix.
- **Test builder pattern** for domain models.
- **Stryker targets:** Parse and Transform only. Emit excluded (snapshot tests cover it).
- **CancellationToken:** All `RunGenerators()`, `GetDiagnostics()`, `ParseText()` calls MUST pass `TestContext.Current.CancellationToken` (xUnit v3 requirement -- xUnit1051 warning).
- **`TreatWarningsAsErrors=true`** is enforced globally.
- **`DiffEngine_Disabled: true`** must be set in CI or snapshot mismatches hang the runner.
- **StubCommandService unit tests: use delay = 0** to avoid flaky tests under CI load. Real delays only for Counter sample smoke test.
- **Expected test count after this story:** ~80-95 new tests (up from 202 baseline). Target total: ~282-297.

### Build & CI Notes

- **Build race (CS2012):** Always `dotnet build` first, then `dotnet test --no-build`. Recurring issue from Epic 1.
- **`AnalyzerReleases.Unshipped.md`:** Update for HFC1006 or RS2008 build error.
- **Roslyn pinned at 4.12.0 exactly** -- 5.x breaks IDE analyzer load context.
- **ASP0006 in .NET 10:** `seq++` in RenderTreeBuilder triggers error with TreatWarningsAsErrors. Suppress via `#pragma warning disable ASP0006` in EMITTED code OR use explicit sequence constants.
- **Snapshot normalization regex** for Blazor IDs/`blazor:` attributes may need extension for new dynamic attributes (`aria-describedby`, label `for`).

### Previous Story Intelligence (from Epic 1)

**Patterns that WORKED:**
- Three-stage pipeline (Parse -> Transform -> Emit) with pure functions
- Snapshot testing with Verify framework
- `IEquatable<T>` on all models (sealed class pattern) for incremental caching
- Namespace-qualified hint names for generated files
- Label resolution chain with `[Display(Name)]` > humanized CamelCase
- `CSharpSyntaxTree.ParseText()` verification for all emitted code (zero syntax errors)

**Pitfalls to AVOID:**
- DO NOT create a second `EquatableArray<T>` -- reuse existing one
- DO NOT call `services.AddFluxor()` twice -- use the existing single-scan pattern
- DO NOT use `GetTypes()` for assembly scanning -- use `GetExportedTypes()`
- DO NOT reference FluentUI v4 APIs -- v5 renames and removes many components
- DO NOT use `record` for IR types -- sealed class with manual IEquatable<T> matches existing code
- DO NOT dispatch Fluxor actions from the stub service -- form component owns typed action dispatch
- DO NOT reuse HFC1003 for new diagnostic -- it's already taken

### Project Structure Notes

```
src/Hexalith.FrontComposer.Contracts/
  Attributes/
    DerivedFromAttribute.cs                 # NEW
  Communication/
    CommandRejectedException.cs             # NEW

src/Hexalith.FrontComposer.SourceTools/
  Parsing/
    DomainModel.cs                          # EXTEND: CommandModel, CommandParseResult (sealed classes)
    AttributeParser.cs                      # EXTEND: ParseCommand() method
  Transforms/
    CommandFormModel.cs                     # NEW: FormFieldModel, CommandFormModel
    CommandFormTransform.cs                 # NEW
    CommandFluxorModel.cs                   # NEW
    CommandFluxorTransform.cs               # NEW
  Emitters/
    CommandFormEmitter.cs                   # NEW
    CommandFluxorActionsEmitter.cs          # NEW
    CommandFluxorFeatureEmitter.cs          # NEW
    FluxorActionsEmitter.cs                 # MODIFY: add CorrelationId to projection actions
    RegistrationEmitter.cs                  # MODIFY: populate DomainManifest.Commands
  Diagnostics/
    DiagnosticDescriptors.cs                # ADD HFC1006
  FrontComposerGenerator.cs                 # MODIFY: replace boolean command provider with full parse pipeline
  AnalyzerReleases.Unshipped.md             # ADD HFC1006 entry

src/Hexalith.FrontComposer.Shell/
  Services/                                  # NEW DIRECTORY
    StubCommandService.cs                   # NEW
    StubCommandServiceOptions.cs            # NEW
  Components/Rendering/                     # NEW DIRECTORY
    FcFieldPlaceholder.razor                # NEW

tests/Hexalith.FrontComposer.SourceTools.Tests/
  Parsing/
    CommandParserTests.cs                   # NEW (~40 tests incl. equality)
  Transforms/
    CommandFormTransformTests.cs            # NEW (~20 tests)
    CommandFluxorTransformTests.cs          # NEW (~10 tests)
  Emitters/
    CommandFormEmitterTests.cs              # NEW (~12 snapshot tests)
    CommandFluxorActionsEmitterTests.cs     # NEW (~6 tests)
    CommandFluxorFeatureEmitterTests.cs     # NEW (~6 tests)
    Snapshots/                              # NEW .verified.txt files
  Integration/
    CommandGeneratorIsolatedTests.cs        # NEW
    CommandProjectionMixedTests.cs          # NEW

tests/Hexalith.FrontComposer.Shell.Tests/
  Services/
    StubCommandServiceTests.cs              # NEW (~5 tests)
  Components/
    FcFieldPlaceholderTests.cs              # NEW (~6 tests)
    CommandFormRenderTests.cs               # NEW (~10 bUnit tests)
    NumericConverterTests.cs                # NEW (~9 edge case tests)

Directory.Packages.props                     # ADD: FluentValidation, Blazored.FluentValidation (pending), FsCheck.Xunit.v3

samples/Counter/Counter.Domain/
  IncrementCommandValidator.cs              # NEW

samples/Counter/Counter.Web/
  Program.cs                                 # MODIFY: register StubCommandService + validator
```

### References

- [Source: _bmad-output/planning-artifacts/epics.md -- Epic 2, Story 2.1]
- [Source: _bmad-output/planning-artifacts/architecture.md -- ADR-004, ADR-008, Fluxor naming, SourceTools structure, Package boundaries]
- [Source: _bmad-output/planning-artifacts/prd.md -- FR1, FR6, FR8, FR9, Success Criteria, NFRs]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md -- Data Formatting table, Form layout (720px max-width, Comfortable density), Action density, Label resolution, IFluentField, Error recovery preserves intent]
- [Source: _bmad-output/implementation-artifacts/1-5-source-generator-transform-and-emit-stages.md -- Transform/Emit patterns, CorrelationId gap, sealed class IR pattern]
- [Source: _bmad-output/implementation-artifacts/1-6-counter-sample-domain-and-aspire-topology.md -- Fluxor single-scan, assembly scanning]
- [Source: _bmad-output/implementation-artifacts/1-7-ci-pipeline-and-semantic-release.md -- CI conventions, build race]
- [Source: _bmad-output/implementation-artifacts/1-8-hot-reload-and-fluent-ui-contingency.md -- Fluent UI v5 RC2 status, hot reload limitations]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md -- CorrelationId gap, manifest Commands empty]
- [Source: Fluent UI Blazor v5 MCP documentation -- component API research, migration guides]
- [Source: Party mode review feedback from Winston (architect), Amelia (dev), Sally (UX), Murat (test architect)]

---

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
