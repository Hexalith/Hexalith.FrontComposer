# Story 2.1: Command Form Generation & Field Type Inference

Status: ready-for-dev

## Story

As a developer,
I want the source generator to produce form components from [Command]-annotated records with automatic field type inference and validation,
So that business users get correctly typed, validated input forms without manual component authoring.

---

## Critical Decisions (READ FIRST -- Do NOT Revisit)

These decisions are BINDING. Tasks reference them by number. If implementation uncovers a reason to change one, raise it before coding, not after.

| # | Decision | Rationale | See ADR |
|---|----------|-----------|---------|
| D1 | IR types are **sealed classes with manual `IEquatable<T>` and `GetHashCode()`** -- NOT `record` syntax | Matches existing `DomainModel`, `PropertyModel`, `ParseResult` pattern | ADR-009 |
| D2 | Diagnostic ID for "Command missing MessageId" is **HFC1006** -- NOT HFC1003 (already "Projection should be partial") | Avoid collision; update `AnalyzerReleases.Unshipped.md` | -- |
| D3 | Form wrapper is **standard Blazor `<EditForm>`** -- `<FluentEditForm>` does NOT exist in v5 | Fluent UI v5 API reality | -- |
| D4 | `bool` fields render as **`FluentSwitch`** -- NOT `FluentCheckbox` | UX spec "toggle style" = binary on/off | -- |
| D5 | **Form component dispatches Fluxor actions; stub only simulates HTTP.** `ICommandService.DispatchAsync` accepts an optional lifecycle callback. Stub invokes callback during its delays. Form's callback dispatches typed actions. | Decouples stub from Fluxor; supports real EventStore reuse | ADR-010 |
| D6 | `StubCommandService` throws `CommandRejectedException` on rejection. Form catches and dispatches `RejectedAction`. | Exception-based error path is explicit | ADR-010 |
| D7 | **`IStringLocalizer` is runtime-resolved** inside the generated component, NOT at source-gen time. Emitter generates a `ResolveLabel()` helper with null-safe lookup. | Generator cannot resolve runtime localization | -- |
| D8 | Lifecycle is **5 states**: Idle -> Submitting -> Acknowledged -> Syncing -> Confirmed/Rejected. Syncing is simulated by the stub via a second delay after HTTP returns. | AC5 requirement; matches real EventStore behavior | -- |
| D9 | **Form state is preserved on `Rejected`** -- model fields are NOT cleared. Only the lifecycle state resets to Idle when user dismisses. | UX spec "error recovery preserves intent" | -- |
| D10 | Pipeline entry point file is **`FrontComposerGenerator.cs`** -- `FrontComposerPipeline.cs` does NOT exist | Avoid fictitious file references | -- |
| D11 | v0.1 uses **standard Blazor `<DataAnnotationsValidator>`** -- NOT `Blazored.FluentValidation` | v5 compatibility unverified; reduces v0.1 critical-path risk | ADR-011 |
| D12 | `DomainManifest.Commands` uses **per-command registration with runtime aggregation** (Path A) -- NOT `Collect()`-based emission | Preserves incremental caching and 500ms NFR budget | ADR-012 |
| D13 | **Visual lifecycle feedback** (progress ring inside button, sync pulse, "Still syncing..." text) -- only the **progress ring during Submitting** is in scope for this story. Sync pulse, timeout text, and message bars are **Story 2-4 (FcLifecycleWrapper)**. This story dispatches the lifecycle state; Story 2-4 renders rich feedback. | Prevents scope creep | -- |
| D14 | `Fluxor.Feature.GetName()` uses **fully-qualified `{Namespace}.{TypeName}LifecycleState`** -- NOT `nameof({TypeName}LifecycleState)` | Avoid collisions when two commands have same short name in different namespaces | -- |
| D15 | Form component NEVER logs `_model` instance. Structured logging via `ILogger<{Command}Form>` logs **CorrelationId and MessageId only**. | PII/secret leakage prevention | -- |

---

## Architecture Decision Records

### ADR-009: Command IR Types Use Sealed Class Pattern
- **Status:** Accepted
- **Context:** Story 2-1 introduces `CommandModel`, `FormFieldModel`, `CommandFormModel`, `CommandFluxorModel`. Existing IR types (`DomainModel`, `PropertyModel`) use sealed classes with manual `IEquatable<T>` and `GetHashCode()`.
- **Decision:** All new command IR types follow the sealed class pattern with manual equality.
- **Consequences:** (+) Consistency with existing code. (+) Explicit control over equality semantics (critical for Roslyn incremental caching). (-) More verbose than records.
- **Rejected alternatives:** Records (breaks consistency; requires `IsExternalInit` polyfill verification across TFMs).

### ADR-010: Form Component Owns Lifecycle Action Dispatch
- **Status:** Accepted
- **Context:** `ICommandService` lives in Shell. Generated command actions (`{Command}Actions.SubmittedAction` etc.) are per-command. The stub cannot reference concrete action types. The lifecycle has 5 states; the stub simulates HTTP (Acknowledged), and Syncing/Confirmed represent SignalR catch-up.
- **Decision:** Update `ICommandService`:
  ```csharp
  Task<CommandResult> DispatchAsync<TCommand>(
      TCommand command,
      Action<CommandLifecycleState, string?>? onLifecycleChange = null,
      CancellationToken ct = default) where TCommand : class;
  ```
  Stub invokes `onLifecycleChange(CommandLifecycleState.Syncing, correlationId)` after the ack delay, then `onLifecycleChange(CommandLifecycleState.Confirmed, correlationId)` after the confirm delay. Form's callback dispatches typed Fluxor actions. On cancellation token trip (form disposed), stub stops invoking callbacks.
- **Consequences:** (+) Decouples stub from Fluxor. (+) Real EventStore service (Story 5-1) reuses the same callback for SignalR events. (-) Contract is slightly more complex than pure request/response.
- **Rejected alternatives:** Generator-emitted `ICommandLifecycleCallback<TCommand>` (interface explosion), reflection-based dispatch in stub (fragile, AOT-hostile), stub directly referencing generated action types (circular reference).

### ADR-011: v0.1 Uses DataAnnotationsValidator (FluentValidation Deferred)
- **Status:** Accepted
- **Context:** `Blazored.FluentValidation` compatibility with Fluent UI v5 is unverified as of this story. Adding it to v0.1's critical path creates cascading risk.
- **Decision:** v0.1 emits `<DataAnnotationsValidator />` inside the generated `<EditForm>`. Adopters use `[Required]`, `[Range]`, `[MaxLength]`, `[RegularExpression]` on command properties. FluentValidation integration is deferred to a later story.
- **Consequences:** (+) Zero external dependency risk. (+) Well-documented, stable Blazor pattern. (-) Adopters cannot use `AbstractValidator<T>` in v0.1. (-) AC4 wording adjusted.
- **Rejected alternatives:** Custom FluentValidation bridge (scope creep), skip validation entirely (violates AC4).

### ADR-012: DomainManifest.Commands Uses Per-Type Registration
- **Status:** Accepted
- **Context:** Cross-type aggregation via `IncrementalValuesProvider.Collect()` defeats Roslyn incremental caching and risks the 500ms NFR budget.
- **Decision:** Each `[Command]` emits its own `{Command}Registration.g.cs` with a per-command manifest entry. `FrontComposerRegistry.RegisterDomain()` merges manifests by BoundedContext at runtime (existing pattern).
- **Consequences:** (+) Preserves per-type incremental caching. (+) Parallels projection pattern. (-) More generated files. (+) Deterministic emission ordering.
- **Rejected alternatives:** `Collect()`-based aggregation (NFR violation on large domains), bounded-context index files (deferred to v0.3 as optimization if measured need arises).

---

## Lifecycle Sequence Diagram (Reference)

Canonical click -> Confirmed sequence. All tasks must align with this.

```
USER              FORM                     DISPATCHER              STUB                        CALLBACK
 |                 |                            |                    |                            |
 |-- click -->     |                            |                    |                            |
 |                 |-- Dispatch Submitted ----->|                    |                            |
 |                 |   (CorrelationId, cmd)     |                    |                            |
 |                 |-- InvokeAsync(StateHasChanged)                  |                            |
 |                 |   (button disabled, ring shown)                 |                            |
 |                 |-- await DispatchAsync ------------------------> |                            |
 |                 |   (cmd, onLifecycleChange, ct)                  |                            |
 |                 |                            |                    |-- delay(AckMs) ------->    |
 |                 |                            |                    |-- return CommandResult --> |
 |                 |<-------------- CommandResult (MessageId) ------ |                            |
 |                 |-- Dispatch Acknowledged -->|                    |                            |
 |                 |   (CorrelationId, MessageId)                    |                            |
 |                 |                            |                    |-- delay(SyncMs) ------>    |
 |                 |                            |                    |-- onLifecycleChange(Syncing)->|
 |                 |<----------- callback invoked (Syncing, CorrelationId) ---------------------- |
 |                 |-- Dispatch Syncing ------->|                    |                            |
 |                 |                            |                    |-- delay(ConfirmMs) --->    |
 |                 |                            |                    |-- onLifecycleChange(Confirmed)->|
 |                 |<----------- callback invoked (Confirmed, CorrelationId) ------------------- |
 |                 |-- Dispatch Confirmed ----->|                    |                            |
 |                 |                            |                    |                            |
 |                 |   [User dismisses / timer / terminal ack]       |                            |
 |                 |-- Dispatch ResetToIdle ---> (Story 2-4 concern) |                            |

ERROR PATH:
 |                 |-- await DispatchAsync ------------------------> |                            |
 |                 |                                                 |-- throw CommandRejectedException
 |                 |<-------- CommandRejectedException ------------- |                            |
 |                 |-- Dispatch Rejected ------>|                    |                            |
 |                 |   (CorrelationId, Reason, Resolution)           |                            |
 |                 |   MODEL FIELDS PRESERVED (Decision D9)          |                            |

CANCELLATION (form disposed mid-submit):
 |                 | [Dispose() -> ct.Cancel()]                      |                            |
 |                 |                                                 |-- ct.ThrowIfCancellationRequested
 |                 |   [No further callbacks fired]                  |                            |
```

Task 3A.2 pseudocode MUST match this sequence. Task 5.1 stub implementation MUST match this sequence. Task 7.3 Counter sample wiring MUST match this sequence.

---

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
**And** bool properties render as `FluentSwitch` (Decision D4)
**And** DateTime/DateOnly properties render as `FluentDatePicker<TValue>`
**And** enum properties render as `FluentSelect<TEnum, TEnum>` with humanized option labels truncated to 30 chars
**And** int/long properties render as `FluentTextInput` with `TextInputType.Number` (string-backing converter)
**And** decimal/double/float properties render as `FluentTextInput` with `TextInputType.Number` + `InputMode.Decimal`
**And** Guid/ULID properties render as `FluentTextInput` with monospace CSS class
**And** unsupported types render as `FcFieldPlaceholder` with build-time warning HFC1002

### AC3: Label Resolution Chain & Accessibility

**Given** a generated command form
**When** field labels render
**Then** the label resolution chain applies: `[Display(Name)]` > `IStringLocalizer<TCommand>` (runtime) > humanized CamelCase > raw field name (Decision D7)
**And** every field has an associated `<label>` element
**And** required fields are visually marked via `Required` property
**And** validation messages use `aria-describedby` and `aria-live="polite"`
**And** `FcFieldPlaceholder` is focusable in tab order with `role="status"` and `aria-label`
**And** form wrapper is `max-width: 720px; margin: 0 auto` per UX spec (Form Layout section)
**And** form defaults to Comfortable density per UX spec (Density Strategy section)

### AC4: DataAnnotations Validation Integration

**Given** DataAnnotations attributes on command properties (e.g., `[Required]`, `[Range]`, `[MaxLength]`)
**When** the form is submitted with invalid input
**Then** validation messages appear inline via each component's `Message`/`MessageState` properties (Fluent UI v5 IFluentField pattern)
**And** the form does not submit until validation passes
**And** `EditContext` is wired via standard Blazor `<EditForm>` + `<DataAnnotationsValidator />` (Decision D11)
**And** numeric string-to-number conversion failures surface as inline `MessageState.Error` messages

### AC5: Stub ICommandService with Full 5-State Lifecycle

**Given** the v0.1 milestone scope (Epics 1-2 only)
**When** command forms submit
**Then** `StubCommandService : ICommandService` simulates full 5-state lifecycle (Idle -> Submitting -> Acknowledged -> Syncing -> Confirmed / Rejected) via the lifecycle callback (Decision D5, D8)
**And** button shows `FluentProgressRing` during `Submitting` state (Decision D13 -- other visual feedback is Story 2-4)
**And** the form component (not the stub) translates callbacks into typed Fluxor action dispatches
**And** on `Rejected`, form field values are preserved (Decision D9)
**And** form dispose cancels in-flight callbacks via `CancellationToken`
**And** stub is replaceable with real EventStore dispatcher (Story 5.1) without code changes
**And** Counter sample demonstrates the full lifecycle against the stub

**References:** FR1, UX-DR22, UX-DR21, UX-DR3, NFR30, UX spec Form Layout section, UX spec Error Recovery section

---

## Tasks / Subtasks

### Task 0: Prerequisites (Blocking for this story) (AC: all)

- [ ] 0.1: Verify Fluent UI Blazor version compatibility. If `5.0.0.26098` GA is available on NuGet, update `Directory.Packages.props` from `5.0.0-rc.2-26098.1`. Otherwise stay on RC2.
- [ ] 0.2: Add `FsCheck.Xunit.v3` to `Directory.Packages.props` for property-based tests in Task 8.7. Do NOT add `Blazored.FluentValidation` (Decision D11).
- [ ] 0.3: Create `DerivedFromAttribute` in `Contracts/Attributes/`:
  ```csharp
  // Required output (binding contract):
  [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
  public sealed class DerivedFromAttribute : Attribute
  {
      public DerivedFromAttribute(DerivedFromSource source) { Source = source; }
      public DerivedFromSource Source { get; }
  }
  public enum DerivedFromSource { Context, User, Timestamp, MessageId }
  ```
- [ ] 0.4: Create `CommandRejectedException` in `Contracts/Communication/`:
  ```csharp
  // Required output:
  public class CommandRejectedException : Exception
  {
      public string Resolution { get; }
      public CommandRejectedException(string reason, string resolution) : base(reason)
      { Resolution = resolution; }
  }
  ```
- [ ] 0.5: **Update `ICommandService` contract in `Contracts/Communication/`** to accept lifecycle callback (Decision D5, ADR-010):
  ```csharp
  // Required output (binding contract):
  public interface ICommandService
  {
      Task<CommandResult> DispatchAsync<TCommand>(
          TCommand command,
          Action<CommandLifecycleState, string?>? onLifecycleChange = null,
          CancellationToken cancellationToken = default) where TCommand : class;
  }
  ```

### Task 0.5: Epic 1 Cleanup (Can ship as separate PR) (AC: 1)

> **Rationale for separate PR:** The CorrelationId retrofit is a breaking change to Epic 1 generated code with large blast radius. Ship independently so review and regression scope stays contained.

- [ ] 0.5.1: Add `CorrelationId` to all generated Fluxor actions (ADR-008 gap from deferred-work.md). **BLAST RADIUS:**
  - `FluxorActionsEmitter.cs` record signatures (`LoadRequestedAction`, `LoadedAction`, `LoadFailedAction`)
  - All existing `.verified.txt` snapshots must be re-approved
  - `RazorEmitter.cs` dispatcher calls must include CorrelationId
  - Reducer tests may break
  - Estimated: 4+ hours
- [ ] 0.5.2: Add migration note to `deferred-work.md` marking CorrelationId gap RESOLVED with a pointer to the retrofit commit. Update `CHANGELOG.md` if one exists, otherwise add a "Breaking Changes" note to story 0.5 Completion Notes.

### Task 1: Command IR Model & Parser (AC: 1) (See Decision D1, D2)

- [ ] 1.1: Add `CommandModel` IR type to `SourceTools/Parsing/DomainModel.cs`. **Required output** (sealed class with manual `IEquatable<T>` -- Decision D1):
  ```csharp
  // Example structure -- match exact field set:
  public sealed class CommandModel : IEquatable<CommandModel>
  {
      public string TypeName { get; }
      public string Namespace { get; }
      public string BoundedContext { get; }
      public string BoundedContextDisplayLabel { get; }
      public EquatableArray<PropertyModel> Properties { get; }
      public EquatableArray<PropertyModel> DerivableProperties { get; }
      public EquatableArray<PropertyModel> NonDerivableProperties { get; }
      // + ctor, Equals, GetHashCode
  }
  ```
  **Equality MUST cover EVERY field** (pre-mortem A prevention): TypeName, Namespace, BoundedContext, BoundedContextDisplayLabel, Properties, DerivableProperties, NonDerivableProperties. Hash code same.
- [ ] 1.2: Add `CommandParseResult` sealed class to `DomainModel.cs` following existing `ParseResult` pattern.
- [ ] 1.3: Add `ParseCommand()` method to `AttributeParser` (or create `CommandParser.cs`):
  - Support BOTH record positional parameter syntax AND property syntax
  - Include inherited properties from base types (e.g., `MessageId` inherited from a base record)
  - Map each property via existing `FieldTypeMapper`
  - Classify properties as derivable/non-derivable:
    - Derivable: has `[DerivedFrom]` OR name matches `MessageId`, `CommandId`, `CorrelationId`, `TenantId`, `UserId`, `Timestamp`, `CreatedAt`, `ModifiedAt`
    - Non-derivable: all others
  - Emit HFC1002 for unsupported types (already defined)
  - Emit **HFC1006** (NEW, Decision D2) for missing `MessageId`
  - Emit **HFC1007** (NEW) warning when command has > 30 non-derivable properties; hard error at > 100 (DoS mitigation, red team defense)
  - Resolve BoundedContext via `[BoundedContext]` on type or containing class/namespace
  - Resolve DisplayName via `[Display(Name="...")]`
  - **All string values passed to emitter MUST go through `EscapeString`** helper (red team defense vs injection via `DisplayName="foo\"; System.IO.File.Delete(...)"`)
  - Update `AnalyzerReleases.Unshipped.md` with HFC1006 and HFC1007 entries
- [ ] 1.4: Wire `FrontComposerGenerator.cs` (Decision D10) via `ForAttributeWithMetadataName("Hexalith.FrontComposer.Contracts.Attributes.CommandAttribute")`. Replace existing `commandMatches` boolean provider with full `IncrementalValuesProvider<CommandParseResult>`.
- [ ] 1.5: Unit tests for `ParseCommand` -- **exactly 52 tests**:
  - **29 field-type coverage tests** (full matrix: 7 primitives, 4 date/time, 2 identity, 1 enum, 13 nullable, 1 nested record, 1 collection)
  - **9 derivable classification tests** (each derivable key: MessageId, CommandId, CorrelationId, TenantId, UserId, Timestamp, CreatedAt, ModifiedAt, `[DerivedFrom]`)
  - **3 edge case tests**: empty command (0 properties), record positional params, record property syntax
  - **2 inheritance tests**: MessageId on base record, property on base record
  - **3 enum tests**: empty enum (no members), `[Flags]` enum (decide unsupported vs single-select), nested enum (defined inside command class)
  - **3 diagnostic tests**: HFC1002 for unsupported, HFC1006 for missing MessageId, HFC1007 for > 30 properties
  - **2 security tests**: DisplayName with embedded `"` / `\` / `\n` is escaped (red team defense); command type name colliding with `System` namespace is rejected
  - **1 IEquatable test** covering every CommandModel field

### Task 2: Command Form Transform (AC: 1, 2)

- [ ] 2.1: Create `FormFieldModel` in `SourceTools/Transforms/` (sealed class with manual IEquatable, Decision D1).
- [ ] 2.2: Create `CommandFormModel` in `SourceTools/Transforms/` (sealed class).
- [ ] 2.3: Create `CommandFormTransform.cs` -- static pure function `CommandFormModel Transform(CommandModel model)`:
  - Map each property to `FormFieldModel` with correct `TypeCategory`:
    - `string` -> `TextInput`
    - `int`, `long` -> `NumberInput`
    - `decimal`, `double`, `float` -> `DecimalInput`
    - `bool` -> `Switch` (Decision D4)
    - `DateTime`, `DateTimeOffset`, `DateOnly` -> `DatePicker`
    - `TimeOnly` -> `TextInput` with `TextInputType.Time` hint
    - `enum` -> `Select` (humanized names, truncated to 30 chars)
    - `Guid`, ULID -> `MonospaceText`
    - Unsupported -> `Placeholder`
  - Apply static label resolution: `DisplayName ?? HumanizeCamelCase(PropertyName) ?? PropertyName`
  - Generate button label: `"Send {HumanizeCamelCase(CommandTypeName)}"`
  - All string emissions pass through `EscapeString`
- [ ] 2.4: Unit tests for `CommandFormTransform` -- **exactly 38 tests**:
  - **10 TypeCategory mapping tests** (one per category)
  - **4 label resolution tests** (with Display, without, humanized, raw)
  - **2 button label tests** (simple name, multi-word name)
  - **3 enum humanization tests** (short names, > 30 chars truncation, `[Flags]`)
  - **3 derivable/non-derivable separation tests**
  - **16 IEquatable tests** (3 FormFieldModel + 3 CommandFormModel + reflexive/symmetric/transitive + hash consistency across varied inputs)

### Task 3A: Command Form Emitter -- Core Structure (AC: 1) (See Decisions D3, D5, D7, D13, D15)

- [ ] 3A.1: Create `CommandFormEmitter.cs` in `SourceTools/Emitters/`. Emits `{CommandName}Form.g.razor.cs` partial class inheriting `ComponentBase, IDisposable`.

- [ ] 3A.2: Emitted class structure. **Required output (binding contract -- matches Lifecycle Sequence Diagram):**
  ```csharp
  // Example emitted code:
  public partial class {CommandName}Form : ComponentBase, IDisposable
  {
      [Parameter] public {CommandTypeFqn}? InitialValue { get; set; }
      [Inject] private Fluxor.IState<{CommandName}LifecycleState> LifecycleState { get; set; } = default!;
      [Inject] private Fluxor.IDispatcher Dispatcher { get; set; } = default!;
      [Inject] private Hexalith.FrontComposer.Contracts.Communication.ICommandService CommandService { get; set; } = default!;
      [Inject(Key = null)] private Microsoft.Extensions.Localization.IStringLocalizer<{CommandTypeFqn}>? Localizer { get; set; }
      [Inject] private ILogger<{CommandName}Form>? Logger { get; set; }

      private {CommandTypeFqn} _model = new();
      private EditContext? _editContext;
      private CancellationTokenSource? _cts;

      protected override void OnInitialized()
      {
          _model = InitialValue ?? new();
          _editContext = new EditContext(_model);
          LifecycleState.StateChanged += OnStateChanged;
      }

      private void OnStateChanged(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

      // Decision D7 -- runtime label resolution with null safety (pre-mortem C defense):
      private string ResolveLabel(string propertyName, string staticLabel)
      {
          try
          {
              var localized = Localizer?[propertyName];
              if (localized is not null && !localized.ResourceNotFound && !string.IsNullOrEmpty(localized.Value))
                  return localized.Value;
          }
          catch { /* localizer failure is non-fatal, fall through */ }
          return staticLabel;
      }

      private async Task OnValidSubmitAsync()
      {
          _cts?.Cancel();
          _cts = new CancellationTokenSource();
          var correlationId = Guid.NewGuid().ToString();

          // 1. Submitted -- dispatch BEFORE await so button disables immediately (reverse engineering step back 3):
          Dispatcher.Dispatch(new {CommandName}Actions.SubmittedAction(correlationId, _model));
          await InvokeAsync(StateHasChanged);

          // NOTE: NEVER log _model (Decision D15). Log CorrelationId only.
          Logger?.LogInformation("Command submitted. CorrelationId={CorrelationId}", correlationId);

          try
          {
              // 2. DispatchAsync with callback for Syncing / Confirmed (ADR-010):
              var result = await CommandService.DispatchAsync(
                  _model,
                  onLifecycleChange: (state, _) =>
                  {
                      switch (state)
                      {
                          case CommandLifecycleState.Syncing:
                              Dispatcher.Dispatch(new {CommandName}Actions.SyncingAction(correlationId));
                              break;
                          case CommandLifecycleState.Confirmed:
                              Dispatcher.Dispatch(new {CommandName}Actions.ConfirmedAction(correlationId));
                              break;
                      }
                  },
                  cancellationToken: _cts.Token);

              // 3. Acknowledged -- dispatched after HTTP returns:
              Dispatcher.Dispatch(new {CommandName}Actions.AcknowledgedAction(correlationId, result.MessageId));
              // Syncing and Confirmed dispatched via the callback above.
          }
          catch (CommandRejectedException ex)
          {
              // 4. Rejected -- form field values NOT cleared (Decision D9):
              Dispatcher.Dispatch(new {CommandName}Actions.RejectedAction(correlationId, ex.Message, ex.Resolution));
              Logger?.LogWarning("Command rejected. CorrelationId={CorrelationId} Reason={Reason}", correlationId, ex.Message);
          }
          catch (OperationCanceledException) { /* form disposed; ignore */ }
      }

      public void Dispose()
      {
          _cts?.Cancel();
          _cts?.Dispose();
          LifecycleState.StateChanged -= OnStateChanged;
      }
  }
  ```

- [ ] 3A.3: Form-level layout and accessibility (Decision D3, AC3):
  - Wrap in `<div style="max-width: 720px; margin: 0 auto;">`
  - Use standard Blazor `<EditForm>` (Decision D3) with `EditContext="_editContext"` and `OnValidSubmit="OnValidSubmitAsync"`
  - Emit `<DataAnnotationsValidator />` (Decision D11)
  - Form `aria-label="Send {CommandName} command form"`
  - Wrap `seq++` pattern with `#pragma warning disable ASP0006` / `#pragma warning restore ASP0006`

### Task 3B: Command Form Emitter -- Field Rendering (AC: 2)

- [ ] 3B.1: Emit `builder.OpenComponent<T>()` / `AddAttribute()` / `CloseComponent()` calls for each field. **Required emission table** (BuildRenderTree C#, NOT Razor):

  | Field Type | Emitted Pattern |
  |------------|-----------------|
  | `string` | `OpenComponent<FluentTextInput>` + `AddAttribute("Value", _model.{Prop})` + `AddAttribute("ValueChanged", EventCallback.Factory.Create<string?>(this, v => _model.{Prop} = v ?? ""))` + `AddAttribute("Label", ResolveLabel(...))` + `AddAttribute("Required", {IsRequired})` + `CloseComponent()` |
  | `int`, `long` | Same pattern + `AddAttribute("TextInputType", TextInputType.Number)` + string-backing converter (3B.2) |
  | `decimal`, `double`, `float` | Same + `AddAttribute("TextInputType", TextInputType.Number)` + `AddAttribute("InputMode", TextInputMode.Decimal)` + string-backing converter |
  | `bool` | `OpenComponent<FluentSwitch>` (Decision D4) + `AddAttribute("Value", _model.{Prop})` + `AddAttribute("ValueChanged", ...)` + `AddAttribute("Label", ...)` |
  | `DateTime` | `OpenComponent<FluentDatePicker<DateTime>>` + standard bindings |
  | `DateTime?` | `OpenComponent<FluentDatePicker<DateTime?>>` + standard bindings |
  | `DateOnly` / `DateOnly?` | `OpenComponent<FluentDatePicker<DateOnly>>` / `<DateOnly?>` |
  | `TimeOnly` | `OpenComponent<FluentTextInput>` + `AddAttribute("TextInputType", TextInputType.Time)` + `AddAttribute("Placeholder", "HH:mm")` |
  | `enum` | `OpenComponent<FluentSelect<{EnumFqn}, {EnumFqn}>>` + `AddAttribute("Items", System.Enum.GetValues<{EnumFqn}>())` + `AddAttribute("OptionText", (Func<{EnumFqn}, string>)(e => TruncateAt30(Humanize(e.ToString()))))` |
  | `Guid`, ULID | `OpenComponent<FluentTextInput>` + `AddAttribute("Class", "fc-monospace")` |
  | Unsupported | `OpenComponent<FcFieldPlaceholder>` + `AddAttribute("FieldName", "{Prop}")` + `AddAttribute("TypeName", "{DotNetType}")` |

- [ ] 3B.2: Numeric string-backing converter with parse-failure feedback. **Required output:**
  ```csharp
  // Example per-numeric-field backing fields:
  private string? _{prop}String;
  private string? _{prop}ParseError;

  private void On{Prop}Changed(string? value)
  {
      _{prop}String = value;
      if (string.IsNullOrWhiteSpace(value)) { _model.{Prop} = default; _{prop}ParseError = null; return; }
      if ({TypeName}.TryParse(value, System.Globalization.CultureInfo.CurrentCulture, out var parsed))
      { _model.{Prop} = parsed; _{prop}ParseError = null; }
      else { _{prop}ParseError = "Invalid number format."; }
  }
  ```
  Bind `Message` and `MessageState` to `_{prop}ParseError`.

- [ ] 3B.3: **Submit button with lifecycle progress ring (Decision D13, reverse engineering step 2).** Required output:
  ```csharp
  // Example:
  builder.OpenComponent<FluentButton>(seq++);
  builder.AddAttribute(seq++, "Type", ButtonType.Submit);
  builder.AddAttribute(seq++, "Appearance", Appearance.Primary);
  builder.AddAttribute(seq++, "Disabled", LifecycleState.Value.State != CommandLifecycleState.Idle);
  builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(__b =>
  {
      int cseq = 0;
      if (LifecycleState.Value.State == CommandLifecycleState.Submitting)
      {
          __b.OpenComponent<FluentProgressRing>(cseq++);
          __b.AddAttribute(cseq++, "Style", "width: 20px; height: 20px; margin-right: 8px;");
          __b.CloseComponent();
      }
      __b.AddContent(cseq++, "{ButtonLabel}");
  }));
  builder.CloseComponent();
  ```

### Task 3C: Validation Wiring (AC: 4) (See Decision D11)

- [ ] 3C.1: Emit `<DataAnnotationsValidator />` inside `<EditForm>` (Decision D11).
- [ ] 3C.2: Per-field validation binding. Each non-placeholder field binds:
  - `Message` = `_editContext.GetValidationMessages(() => _model.{Prop}).FirstOrDefault() ?? _{prop}ParseError`
  - `MessageState` = `Error` when message non-null, else `None`
- [ ] 3C.3: Emit `<FluentValidationSummary Model="_model" />` at form top for cross-field errors.

### Task 3D: FcFieldPlaceholder Component (AC: 2, 3)

- [ ] 3D.1: Create `Shell/Components/Rendering/FcFieldPlaceholder.razor` (directory must be created). **Required output:**
  ```razor
  @namespace Hexalith.FrontComposer.Shell.Components.Rendering

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
- [ ] 3D.2: Add CSS for `.fc-field-placeholder-dev` (red dashed border) and `.fc-monospace` (monospace font family) to a shared stylesheet in Shell.

### Task 3E: Emitter Snapshot Coverage (AC: 1, 2)

- [ ] 3E.1: Snapshot tests (`.verified.txt`) -- **exactly 12 tests**:
  1. Single string field command
  2. Multi-field typical (string, int, bool, DateTime, enum)
  3. **Kitchen sink: all 29 field types** (parallel to `AllFieldTypesProjection`)
  4. Zero non-derivable fields (button-only form)
  5. Unsupported type triggers placeholder
  6. `[Display(Name)]` overrides
  7. All nullable types
  8. Numeric fields (verifies string-backing converter snapshot)
  9. All-unsupported fields
  10. `[Flags]` enum (verifies Task 1.5 decision)
  11. Record positional parameters
  12. Inherited property from base record

- [ ] 3E.2: Parseability test: every emitted `.g.razor.cs` parses via `CSharpSyntaxTree.ParseText()` with zero errors.
- [ ] 3E.3: Determinism test: run emitter twice on identical input, assert byte-identical output.

### Task 4: Command Fluxor Actions & State (AC: 1, 5) (See Decision D14)

- [ ] 4.1: Create `CommandFluxorTransform.cs` (sealed class `CommandFluxorModel`). Transform derives names from `CommandModel`.

- [ ] 4.2: Create `CommandFluxorActionsEmitter.cs`. **Required output** (nested wrapper class is new convention for commands):
  ```csharp
  public static class {CommandName}Actions
  {
      public sealed record SubmittedAction(string CorrelationId, {CommandFqn} Command);
      public sealed record AcknowledgedAction(string CorrelationId, string MessageId);
      public sealed record SyncingAction(string CorrelationId);
      public sealed record ConfirmedAction(string CorrelationId);
      public sealed record RejectedAction(string CorrelationId, string Reason, string Resolution);
  }
  ```

- [ ] 4.3: Create `CommandFluxorFeatureEmitter.cs`. **Required output:**
  ```csharp
  public sealed record {CommandName}LifecycleState(
      Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState State,
      string? CorrelationId,
      string? MessageId,
      string? Error,
      string? Resolution);

  public class {CommandName}LifecycleFeature : Fluxor.Feature<{CommandName}LifecycleState>
  {
      // Decision D14 -- fully qualified name prevents collisions:
      public override string GetName() => "{Namespace}.{CommandName}LifecycleState";
      protected override {CommandName}LifecycleState GetInitialState()
          => new(CommandLifecycleState.Idle, null, null, null, null);
  }

  public static class {CommandName}Reducers
  {
      [Fluxor.ReducerMethod] public static {CommandName}LifecycleState OnSubmitted(
          {CommandName}LifecycleState state, {CommandName}Actions.SubmittedAction action)
          => state with { State = CommandLifecycleState.Submitting, CorrelationId = action.CorrelationId };

      [Fluxor.ReducerMethod] public static {CommandName}LifecycleState OnAcknowledged(
          {CommandName}LifecycleState state, {CommandName}Actions.AcknowledgedAction action)
          => state with { State = CommandLifecycleState.Acknowledged, MessageId = action.MessageId };

      [Fluxor.ReducerMethod] public static {CommandName}LifecycleState OnSyncing(
          {CommandName}LifecycleState state, {CommandName}Actions.SyncingAction action)
          => state with { State = CommandLifecycleState.Syncing };

      [Fluxor.ReducerMethod] public static {CommandName}LifecycleState OnConfirmed(
          {CommandName}LifecycleState state, {CommandName}Actions.ConfirmedAction action)
          => state with { State = CommandLifecycleState.Confirmed };

      [Fluxor.ReducerMethod] public static {CommandName}LifecycleState OnRejected(
          {CommandName}LifecycleState state, {CommandName}Actions.RejectedAction action)
          => state with { State = CommandLifecycleState.Rejected, Error = action.Reason, Resolution = action.Resolution };
  }
  ```

- [ ] 4.4: Unit tests -- **exactly 12 tests**: 5 snapshot tests (actions, state record, feature class, reducers, registration), 5 state transition tests (one per reducer), 2 IEquatable tests for CommandFluxorModel. Plus 1 collision test: two commands with same `TypeName` in different namespaces produce distinct `GetName()` values (Decision D14, chaos #3 defense).

### Task 5: Stub ICommandService Implementation (AC: 5) (See Decisions D5, D6, D8, ADR-010)

- [ ] 5.1: Create `StubCommandService.cs` in `Shell/Services/` (directory must be created). **Required output** (matches Lifecycle Sequence Diagram):
  ```csharp
  public class StubCommandService : ICommandService
  {
      private readonly IOptionsSnapshot<StubCommandServiceOptions> _options;

      public StubCommandService(IOptionsSnapshot<StubCommandServiceOptions> options)
      { _options = options; }

      public async Task<CommandResult> DispatchAsync<TCommand>(
          TCommand command,
          Action<CommandLifecycleState, string?>? onLifecycleChange = null,
          CancellationToken ct = default) where TCommand : class
      {
          var opts = _options.Value;
          await Task.Delay(opts.AcknowledgeDelayMs, ct);
          var messageId = Guid.NewGuid().ToString();
          if (opts.SimulateRejection)
          {
              throw new CommandRejectedException(
                  opts.RejectionReason ?? "Simulated rejection",
                  opts.RejectionResolution ?? "Adjust input and retry");
          }
          // Post-ack: simulate SignalR catch-up via callback
          _ = Task.Run(async () =>
          {
              try
              {
                  await Task.Delay(opts.SyncingDelayMs, ct);
                  onLifecycleChange?.Invoke(CommandLifecycleState.Syncing, messageId);
                  await Task.Delay(opts.ConfirmDelayMs, ct);
                  onLifecycleChange?.Invoke(CommandLifecycleState.Confirmed, messageId);
              }
              catch (OperationCanceledException) { /* form disposed */ }
          }, ct);
          return new CommandResult(messageId, "acknowledged");
      }
  }
  ```

- [ ] 5.2: Create `StubCommandServiceOptions.cs`:
  ```csharp
  public sealed record StubCommandServiceOptions
  {
      public int AcknowledgeDelayMs { get; init; } = 100;
      public int SyncingDelayMs { get; init; } = 100;
      public int ConfirmDelayMs { get; init; } = 200;
      public bool SimulateRejection { get; init; } = false;
      public string? RejectionReason { get; init; }
      public string? RejectionResolution { get; init; }
  }
  ```
  **DI lifetime (pre-mortem B defense):** Register as `Scoped` via `services.Configure<StubCommandServiceOptions>(...)` OR `services.AddScoped<IOptionsSnapshot<StubCommandServiceOptions>, ...>()` equivalent. NEVER `Singleton` -- options are per-scope/per-circuit.

- [ ] 5.3: Register in `AddHexalithFrontComposer()`: `services.AddScoped<ICommandService, StubCommandService>()` + default options via `services.Configure<StubCommandServiceOptions>(_ => { })`. Replaceable by Story 5.1's `AddHexalithEventStore()`.

- [ ] 5.4: Unit tests for `StubCommandService` -- **exactly 7 tests**:
  1. Acknowledgment returns `CommandResult` with non-null `MessageId`
  2. Syncing callback fires after ack
  3. Confirmed callback fires after Syncing
  4. Rejection throws `CommandRejectedException`
  5. Rejection callbacks never fire after exception
  6. Cancellation during delay stops callback invocation
  7. Delays honored from options
  **All tests use `AcknowledgeDelayMs = 0, SyncingDelayMs = 0, ConfirmDelayMs = 0`** to prevent flakiness under CI load. Only Counter sample smoke test uses real delays.

### Task 6: Wire Generator Pipeline (AC: 1) (See Decisions D10, D12, ADR-012)

- [ ] 6.1: Update `FrontComposerGenerator.cs` (Decision D10):
  - Replace `commandMatches` boolean `IncrementalValuesProvider<bool>` with full `IncrementalValuesProvider<CommandParseResult>`
  - Add `RegisterSourceOutput` calls for: form `.g.razor.cs`, Fluxor feature `.g.cs`, Fluxor actions `.g.cs`, command registration `.g.cs`
  - Ensure projection and command pipelines coexist
  - **Clarification (reverse engineering step 5):** Generator runs in the project containing the `[Command]`-annotated types (e.g., `Counter.Domain`). Generated Razor partials compile fine because `Counter.Web` references `Counter.Domain` and the partials live in `obj/.../generated/` under that domain project. `Counter.Web` must have `<AddRazorSupportForMvc>false</AddRazorSupportForMvc>` disabled (or simply reference the domain project). Verify Counter.Web `@using Counter.Domain.*` namespaces are added to `_Imports.razor`.

- [ ] 6.2: Update `RegistrationEmitter.cs` per Decision D12 (ADR-012). Each `[Command]` type emits its own registration entry contributing to `DomainManifest.Commands`. Runtime aggregation in `FrontComposerRegistry.RegisterDomain()`. No `Collect()`.

- [ ] 6.3: Integration test: generator driver with `[Command]`-annotated record + `[Projection]`-annotated record in same namespace. Assert all output files generated and compile.

- [ ] 6.4: Integration test: two commands with same `TypeName` in different namespaces (collision resilience -- namespace-qualified hint names; Decision D14 collision test).

### Task 7: Counter Sample Integration (AC: 5) (See Decision D13)

- [ ] 7.1: After generator changes, `IncrementCommand` auto-generates: `IncrementCommandForm.g.razor.cs`, `IncrementCommandLifecycleFeature.g.cs`, `IncrementCommandActions.g.cs`, registration. Verify compilation.

- [ ] 7.2: Integrate form into Counter page (reverse engineering step 4). **Required:**
  - Add `@using Counter.Domain` to `Counter.Web/_Imports.razor`
  - Place `<IncrementCommandForm />` component in `CounterPage.razor` above the DataGrid
  - Layout: form in a column, DataGrid below it. Expected markup:
    ```razor
    <div class="page-container">
        <section class="command-section">
            <IncrementCommandForm />
        </section>
        <section class="data-section">
            <CounterProjectionView />
        </section>
    </div>
    ```

- [ ] 7.3: Wire `StubCommandService` in Counter.Web's `Program.cs`:
  ```csharp
  services.Configure<StubCommandServiceOptions>(o =>
  {
      o.AcknowledgeDelayMs = 150;
      o.SyncingDelayMs = 150;
      o.ConfirmDelayMs = 200;
  });
  ```
  **IMPORTANT (reverse engineering step 1):** On `ConfirmedAction`, also dispatch `CounterProjectionActions.LoadRequestedAction` to re-query the projection, simulating SignalR catch-up. Without this, the demo shows "submitted" but the count never changes. Implementation: add a dedicated `CounterProjectionEffects.cs` that listens for `IncrementCommandActions.ConfirmedAction` and dispatches `LoadRequestedAction`. (This effect is sample code, not generated.)

- [ ] 7.4: Smoke test: `dotnet watch` on Counter.Web, modify `IncrementCommand` (add property), verify form updates via incremental rebuild within 500ms budget (NFR10).

### Task 8: Test Infrastructure & bUnit Coverage (AC: 3, 4) (Runs BEFORE Task 3)

> **Reorder rationale (hindsight 1):** The dev needs the test base before writing the form emitter. Completing 8.1 before Task 3 enables TDD-style iteration on the emitter.

- [ ] 8.1: Extend (or create) `GeneratedComponentTestBase.cs` in `Shell.Tests`:
  - Register `ICommandService` mock via `Services.AddSingleton(Substitute.For<ICommandService>())` (swapped per-test for real validator scenarios)
  - Register pass-through `IStringLocalizer<T>` (returns key as value)
  - Register `IDispatcher` from Fluxor test harness
  - Register `IOptionsSnapshot<StubCommandServiceOptions>` with zero delays
  - Keep `JSInterop.Mode = JSRuntimeMode.Loose`

- [ ] 8.2: Create `IncrementCommandValidator` via DataAnnotations (Decision D11) -- e.g., `[Range(1, int.MaxValue)]` on `Amount`. Register in Counter.Web. (Not using `AbstractValidator<T>` per ADR-011.)

- [ ] 8.3: bUnit tests in `Shell.Tests/Components/CommandFormRenderTests.cs` -- **exactly 10 tests**:
  1. `Form_RendersWithSingleStringField_ShowsFluentTextInput`
  2. `Form_RendersWithNumericField_ShowsTextInputWithNumberType`
  3. `Form_RendersWithBoolField_ShowsFluentSwitch`
  4. `Form_RendersWithEnumField_ShowsFluentSelect`
  5. `Form_RendersWithDateField_ShowsFluentDatePicker`
  6. `Form_RendersWithUnsupportedType_ShowsFcFieldPlaceholder`
  7. `Form_SubmitButton_DisabledDuringSubmittingState`
  8. `Form_OnValidSubmit_DispatchesSubmittedAction`
  9. `Form_WithInvalidDataAnnotation_ShowsInlineError` (uses **real** `[Range]` attribute + `DataAnnotationsValidator`, NOT mock validator -- red team E defense)
  10. `Form_OnRejected_PreservesFieldValues`

- [ ] 8.4: Numeric converter edge case tests in `NumericConverterTests.cs` -- **exactly 9 tests**: empty string, whitespace, valid integer, overflow (`int.MaxValue + 1`), negative, leading zeros, non-numeric "abc", locale decimal separator (invariant vs fr-FR), null input.

- [ ] 8.5: Form dirty-state tracking for forward compatibility with Story 2-2 (form abandonment protection): emit `bool IsDirty` property updated on any field change. Story 2-2 will use for 30-second warning.

- [ ] 8.6: `FcFieldPlaceholderTests.cs` -- **exactly 5 tests**: `role="status"`, `aria-label`, `tabindex="0"`, documentation `FluentAnchor`, dev-mode CSS class.

- [ ] 8.7: Property-based tests (FsCheck) -- **exactly 3 properties**:
  1. CamelCaseHumanizer roundtrip: for any valid identifier, output is non-null, non-empty, no leading/trailing whitespace
  2. Numeric converter int roundtrip: for any `int`, `int.TryParse(v.ToString(CI), CI, out var p) && p == v`
  3. CommandModel equality symmetry: for any two instances, `a.Equals(b) == b.Equals(a)` and equal instances have equal hash codes

### Task 9: Final Integration & QA (AC: all)

- [ ] 9.0: **End-to-end integration smoke test** (hindsight 3) in `SourceTools.Tests/Integration/EndToEndSmokeTest.cs`:
  - Compile mini project with a `[Command]`-annotated record via `CSharpCompilation`
  - Run `CSharpGeneratorDriver` with the generator
  - Use bUnit to instantiate the generated form
  - Assert it renders without exception
  - Single test; catches regressions across parse/transform/emit/runtime seams

- [ ] 9.1: Run full test suite. **Exact expected total new tests: 140 = 52 (1.5) + 38 (2.4) + 12 (3E) + 12 (4.4) + 7 (5.4) + 2 (6.3-6.4) + 1 (9.0) + 10 (8.3) + 9 (8.4) + 5 (8.6) + 3 (8.7) - overlap adjustments.** Target total solution count: 202 + 140 = ~342.
- [ ] 9.2: Verify `dotnet build --configuration Release` succeeds with no warnings (TreatWarningsAsErrors=true).
- [ ] 9.3: Manual smoke test Counter.Web: valid submit shows full 5-state lifecycle including progress ring + DataGrid refresh; invalid submit shows inline error; "abc" in Amount shows parse error.
- [ ] 9.4: axe-core accessibility scan on Counter.Web command form -- zero serious/critical violations.
- [ ] 9.5: Update `deferred-work.md`: mark CorrelationId gap and DomainManifest.Commands gap as RESOLVED.

---

## Dev Notes

### Fluent UI Blazor v5 Breaking Changes Reference

| Epics/UX Reference (v4) | Actual v5 Component | Breaking Change |
|--------------------------|---------------------|-----------------|
| `FluentTextField` | `FluentTextInput` | Renamed; string-based binding |
| `FluentNumberField` | `FluentTextInput` + `TextInputType.Number` | REMOVED; no native numeric binding |
| `FluentCheckbox` | `FluentSwitch` for command booleans | Switch for binary on/off UX (Decision D4) |
| `FluentDatePicker` | `FluentDatePicker<TValue>` | Now generic |
| `FluentSelect` | `FluentSelect<TOption, TValue>` | Now requires TWO type parameters |
| `FluentValidationMessage` | IFluentField `Message`/`MessageState` | REMOVED entirely |
| `FluentEditForm` | Standard Blazor `<EditForm>` | Does NOT exist in v5 (Decision D3) |

### Architecture Constraints (MUST FOLLOW)

1. **SourceTools (`netstandard2.0`) must NEVER reference Fluxor, FluentUI, or Shell.** All external types emitted as fully-qualified name strings.
2. **Three-stage pipeline:** Parse -> Transform -> Emit. Parse and Transform are pure functions. (ADR-004)
3. **IEquatable<T> on all IR/output models** (sealed class pattern -- Decision D1, ADR-009) -- required for Roslyn incremental caching.
4. **Deterministic output** -- same input must produce byte-identical source code.
5. **All generated files** end in `.g.cs` or `.g.razor.cs`, live in `obj/` not `src/`.
6. **Hint names namespace-qualified:** `{Namespace}.{TypeName}.g.razor.cs`.
7. **Fluxor actions past-tense, always include CorrelationId** (ADR-008, fixed in Task 0.5).
8. **No FluxorComponent base class** -- `IState<T>` inject + explicit subscribe/dispose.
9. **AnalyzerReleases.Unshipped.md** must be updated for HFC1006 and HFC1007 (RS2008).
10. **CommandLifecycleState is ephemeral** -- not persisted.
11. **`#pragma warning disable ASP0006`** around `BuildRenderTree` `seq++` in emitted code.

### Existing Code to REUSE (Do NOT Reinvent)

- `FieldTypeMapper.cs` -- maps all .NET types
- `PropertyModel` -- has Name, TypeName, IsNullable, IsUnsupported, DisplayName, BadgeMappings (empty for commands)
- `CamelCaseHumanizer` -- for labels
- `EquatableArray<T>` -- reuse, don't duplicate
- `ICommandService` + `CommandResult` + `CommandLifecycleState` -- extend contract per ADR-010
- `ICommandLifecycleTracker` -- not needed for this story
- `DiagnosticDescriptors.cs` -- HFC1002 defined; add HFC1006, HFC1007
- `FrontComposerRegistry` -- runtime manifest aggregation
- `EscapeString` helper from Story 1-5 -- mandatory for DisplayName / BoundedContext string emission
- `CounterProjectionActions.LoadRequestedAction` -- existing; Task 7.3 dispatches on Confirmed

### Files That Must Be Created

Directories:
- `src/Hexalith.FrontComposer.Shell/Services/`
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/` (if not present)

New files summarized:
- Contracts: `DerivedFromAttribute.cs`, `CommandRejectedException.cs`; modify `ICommandService.cs`
- SourceTools/Parsing: extend `DomainModel.cs` (CommandModel, CommandParseResult), extend `AttributeParser.cs` (ParseCommand)
- SourceTools/Transforms: `CommandFormModel.cs` (FormFieldModel + CommandFormModel), `CommandFormTransform.cs`, `CommandFluxorModel.cs`, `CommandFluxorTransform.cs`
- SourceTools/Emitters: `CommandFormEmitter.cs`, `CommandFluxorActionsEmitter.cs`, `CommandFluxorFeatureEmitter.cs`
- SourceTools/Diagnostics: update `DiagnosticDescriptors.cs` (HFC1006, HFC1007)
- SourceTools: update `AnalyzerReleases.Unshipped.md`
- Shell/Services: `StubCommandService.cs`, `StubCommandServiceOptions.cs`
- Shell/Components/Rendering: `FcFieldPlaceholder.razor` + CSS
- Directory.Packages.props: add `FsCheck.Xunit.v3`
- Counter.Domain: `IncrementCommandValidator.cs` (DataAnnotations), `_Imports.razor` updates
- Counter.Web: `Program.cs` wiring, `CounterPage.razor` form placement, `CounterProjectionEffects.cs`
- deferred-work.md: resolution notes

### Naming Convention Reference

| Element | Pattern | Example |
|---------|---------|---------|
| Generated form partial | `{CommandName}Form.g.razor.cs` | `IncrementCommandForm.g.razor.cs` |
| Generated Fluxor feature | `{CommandName}LifecycleFeature.g.cs` | |
| Generated Fluxor actions | `{CommandName}Actions.g.cs` | |
| Fluxor state record | `{CommandName}LifecycleState` | |
| Action wrapper class | `{CommandName}Actions` (static) | |
| Action records (nested) | `{CommandName}Actions.{Verb}Action` | `IncrementCommandActions.SubmittedAction` |
| Feature `GetName()` | `"{Namespace}.{CommandName}LifecycleState"` | `"Counter.Domain.IncrementCommandLifecycleState"` (Decision D14) |
| Button label | `"Send {Humanized CommandName}"` | `"Send Increment Counter"` |

### DI Scope Contract

| Service | Lifetime | Rationale |
|---------|----------|-----------|
| `ICommandService` (StubCommandService) | Scoped | Per-circuit in Blazor Server, per-user in WASM |
| `StubCommandServiceOptions` (via `IOptionsSnapshot`) | Scoped | Per-scope mutation without cross-tenant bleed (pre-mortem B defense) |
| `IState<{Command}LifecycleState>` | Scoped (Fluxor default) | Per-circuit state |
| `IStringLocalizer<T>` | Scoped | ASP.NET Core default |

### Testing Standards

- xUnit v3 (3.2.2), Verify.XunitV3, Shouldly, NSubstitute, bUnit 2.7.2, FsCheck.Xunit.v3 (new)
- Parse/Transform: pure function tests (90% of new tests)
- Emit: snapshot/golden-file (.verified.txt)
- bUnit: rendered component tests
- `TestContext.Current.CancellationToken` on all `RunGenerators/GetDiagnostics/ParseText` (xUnit1051)
- `TreatWarningsAsErrors=true` global
- `DiffEngine_Disabled: true` in CI
- StubCommandService unit tests use zero delays
- **Expected new test count: 140.** Target total: ~342.

### Build & CI

- Build race CS2012: `dotnet build` then `dotnet test --no-build`
- `AnalyzerReleases.Unshipped.md` update for HFC1006, HFC1007
- Roslyn 4.12.0 pinned
- ASP0006 suppression in emitted code via `#pragma warning disable ASP0006`

### Previous Story Intelligence (from Epic 1)

**Patterns that worked:** Three-stage pipeline, snapshot testing, sealed class IR with manual IEquatable, namespace-qualified hint names, label resolution chain.

**Pitfalls to avoid:**
- DO NOT create a second `EquatableArray<T>`
- DO NOT call `services.AddFluxor()` twice
- DO NOT use `GetTypes()` -- use `GetExportedTypes()`
- DO NOT reference FluentUI v4 APIs
- DO NOT use `record` for IR types (Decision D1)
- DO NOT dispatch Fluxor actions from the stub (Decision D5)
- DO NOT reuse HFC1003 (Decision D2)
- DO NOT log `_model` in form components (Decision D15)

### References

- [Source: _bmad-output/planning-artifacts/epics.md -- Epic 2, Story 2.1]
- [Source: _bmad-output/planning-artifacts/architecture.md -- ADR-004, ADR-008, Fluxor naming, SourceTools structure, Package boundaries]
- [Source: _bmad-output/planning-artifacts/prd.md -- FR1, FR6, FR8, FR9, NFRs]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md -- Data Formatting section, Form Layout section, Density Strategy section, Label Resolution, IFluentField, Error Recovery section]
- [Source: _bmad-output/implementation-artifacts/1-5-source-generator-transform-and-emit-stages.md -- Transform/Emit patterns, CorrelationId gap, sealed class IR pattern, EscapeString helper]
- [Source: _bmad-output/implementation-artifacts/1-6-counter-sample-domain-and-aspire-topology.md -- Fluxor single-scan, assembly scanning]
- [Source: _bmad-output/implementation-artifacts/1-7-ci-pipeline-and-semantic-release.md -- CI conventions]
- [Source: _bmad-output/implementation-artifacts/1-8-hot-reload-and-fluent-ui-contingency.md -- Fluent UI v5 RC2, hot reload]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md -- CorrelationId gap, manifest Commands empty]
- [Source: Fluent UI Blazor v5 MCP documentation -- component API research, migration guides]
- [Source: Party mode review feedback from Winston, Amelia, Sally, Murat]
- [Source: Advanced elicitation findings -- Pre-mortem, 5 Whys, Tree of Thoughts, Chaos Monkey, Hindsight, Red Team, Feynman, ADRs, Reverse Engineering, Meta-Prompting]

---

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
