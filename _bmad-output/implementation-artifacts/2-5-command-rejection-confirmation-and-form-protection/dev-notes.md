# Dev Notes

## Service Binding Reference

No new DI registrations beyond Task 1.4's extended validator. Everything else is already wired:

- `IDialogService` — registered via `services.AddFluentUIComponents()` (Story 1-8)
- `ILifecycleStateService` — Story 2-3 D12 Scoped
- `IOptionsMonitor<FcShellOptions>` — Story 2-4 (`.AddOptions<FcShellOptions>().BindConfiguration(...).ValidateDataAnnotations().ValidateOnStart()` + `IValidateOptions` via `AddSingleton`)
- `NavigationManager` — built-in
- `TimeProvider` — built-in on .NET 10 (abstracted for tests)

Validator extension (Task 1.4):

```csharp
// Task 1.4 — two additional cross-property invariants layered onto the existing validator.
services.AddSingleton<IValidateOptions<FcShellOptions>>(_ => new FcShellOptionsThresholdValidator());
```

`FcShellOptionsThresholdValidator.Validate(name, options)` returns `ValidateOptionsResult.Fail(...)` when any of the following hold:
1. Story 2-4's existing ordered-threshold chain (pulse < still-syncing < action-prompt).
2. `options.FormAbandonmentThresholdSeconds * 1000 <= options.StillSyncingThresholdMs` (abandonment must not fire before "Still syncing…" has had a chance to show).
3. `options.IdempotentInfoToastDurationMs > options.ConfirmedToastDurationMs` (Info must dismiss no later than Success for UX consistency).

## Rejection Copy Plumbing

Full data flow for rejection message rendering:

1. Adopter's `ICommandService` implementation throws `CommandRejectedException("Approval failed: insufficient inventory", "The order has been returned to Pending.")`.
2. Generated form's `OnValidSubmitAsync` catches: `Dispatcher.Dispatch(new {Command}Actions.RejectedAction(correlationId, ex.Message, ex.Resolution))` (existing code at `CommandFormEmitter.cs:314`).
3. Emitted Fluxor feature reducer projects `RejectedAction` into `{Command}LifecycleFeatureState { State = Rejected, CorrelationId, RejectionReason = Reason, RejectionResolution = Resolution }` (Task 0.1 verifies this path; Task 5.2b ensures it if absent).
4. Generated form's `BuildRenderTree` passes `RejectionMessage = BuildFcLifecycleRejectionCopy()` into `<FcLifecycleWrapper>` (Task 5.2). `BuildFcLifecycleRejectionCopy()` returns `$"{Reason}. {Resolution}"` (or the single non-empty clause, or null if both empty).
5. `ILifecycleStateService.Transition(correlationId, Rejected)` is called by the bridge — wrapper subscribes and receives the transition.
6. Wrapper's `ApplyTransition` case `Rejected`: sets `_state = _state with { Current = Rejected, RejectionMessage = RejectionMessage }` (parameter-supplied copy wins since it's recomputed on every render).
7. Razor template: `@if (_state.Current is CommandLifecycleState.Rejected) { <FluentMessageBar Intent=Error> @((string?)RejectionMessage ?? genericFallback) </FluentMessageBar> }`.
8. Blazor renders `RejectionMessage` as plain text — `<script>` tags from adversarial tenant payloads are HTML-encoded (2-4 D22 + 2-5 D14).

## Destructive Confirmation Flow

```
User clicks destructive trigger (CommandRendererEmitter emitted OnClick)
  ↓
await IDialogService.ShowDialogAsync<FcDestructiveConfirmationDialog>(parameters)
  ↓
Dialog renders (Cancel auto-focused via OnAfterRenderAsync → FocusAsync)
  ├── User presses Escape → OnCancel fires → dialogRef.Result cancelled
  ├── User presses Enter (Cancel focused) → OnCancel fires (D22)
  ├── User clicks Cancel → OnCancel fires
  └── User clicks Destructive (Danger) button → OnConfirm fires
       ↓
       await dialogRef.Result → result.Cancelled == false
       ↓
       _externalSubmit() called (Story 2-2 ADR-016)
       ↓
       Normal submit lifecycle: Submitting → Acknowledged → Syncing → Confirmed/Rejected
       (FcLifecycleWrapper takes over per Story 2-4)
```

Emitted `CommandRendererEmitter` output for a destructive FullPage renderer (pseudocode illustrating the shape, **D24 validation gate + trigger-disable included**):

```csharp
private bool _dialogOpen;

private async Task OnSubmitClickAsync(MouseEventArgs _) {
    // D24 — validate BEFORE opening dialog; don't confirm an invalid-state destructive action.
    if (_formEditContext is not null && !_formEditContext.Validate()) {
        return; // FluentValidationSummary surfaces errors; dialog never opens
    }
    _dialogOpen = true;
    try {
        var parameters = new DialogParameters<FcDestructiveConfirmationParams> {
            Content = new FcDestructiveConfirmationParams {
                Title = DestructiveConfirmTitle ?? $"{DisplayLabel}?",
                Body = DestructiveConfirmBody ?? "This action cannot be undone.",
                DestructiveLabel = DisplayLabel
            }
        };
        var dialogRef = await DialogService.ShowDialogAsync<FcDestructiveConfirmationDialog>(parameters);
        var result = await dialogRef.Result;
        if (!result.Cancelled && _externalSubmit is not null) {
            _externalSubmit();
        }
    } finally {
        _dialogOpen = false; // Always clear — prevents trigger-lockout under async exceptions.
    }
}
// Submit trigger binds: Disabled="_dialogOpen" to block double-click dispatch during dialog lifetime.
```

(The exact `IDialogService` v5 invocation pattern must be verified via `mcp__fluent-ui-blazor__get_component_details` — the snippet above is illustrative.)

## v0.1 Environment Assumptions

- **JavaScript enabled.** `FluentDialog`'s focus-trap + ESC handling relies on JS. Hardened enterprise/government browser configurations that disable JS degrade the destructive-dialog safety posture — Tab escapes the modal, user can confirm without the Cancel auto-focus safety net. v0.1 accepts this: all BMAD planning assumes Blazor Server / WebAssembly / Auto, all of which require JS by design (Blazor itself won't run). Documented here so Story 7-x (enterprise adoption) has the assumption named explicitly.

## Natural Growth Path: `IDirtyStateTracker` (post-elicitation First Principles)

The three protection surfaces in 2-5 — rejection input preservation (D5), destructive confirmation (D2), form abandonment (D10) — all answer the same underlying question: *"what's at risk of loss if this action completes?"* In v0.1 they share no abstraction (L07 — premature unification costs more than it saves when only one surface consumes "dirty state" today). **However**, if Story 2-2 CompactInline abandonment protection (Sally review #5 → deferred to G4) ever lands, OR Epic 4 introduces DataGrid row-level dirty state, the clean unifier is:

```csharp
public interface IDirtyStateTracker {
    bool IsDirty { get; }
    DateTimeOffset? FirstDirtyAt { get; }
    event Action<bool>? DirtyStateChanged;
}
```

`FcFormAbandonmentGuard`'s field-edit subscription becomes a generic `IDirtyStateTracker` consumer; D10's first-edit-anchor semantics become a special case of "dirty-state-tracker with a single dirty transition." **Name this in Dev Notes for the v2 refactor** — zero v0.1 cost, keeps the abstraction-path discoverable when extension demand arrives.

## Abandonment Guard Lifecycle

- Mount: `OnInitialized` — subscribe to `EditContext.OnFieldChanged` only if `EditContext` non-null. Guard does NOT start a timer; timer is implicit via `TimeProvider.GetUtcNow() - _firstEditAt` computed on every `LocationChanging`.
- First edit: `_firstEditAt = Time.GetUtcNow()` + unsubscribe from OnFieldChanged (we only care about the FIRST edit).
- Navigation attempt: `HandleNavigationChanging(ctx)` executes through the decision tree in Task 4.1.
- Circuit teardown: `Dispose` — unsubscribe if still subscribed, release `NavigationLock` via component disposal (Blazor handles lock lifecycle automatically when `<NavigationLock>` leaves the render tree).

## Append-Only Parameter Surface Verification

Story 2-4 D1 append-only contract for `FcLifecycleWrapper`:
- `CorrelationId` (EditorRequired, string) — UNCHANGED.
- `ChildContent` (RenderFragment?) — UNCHANGED.
- `RejectionMessage` (string?) — UNCHANGED. 2-5 POPULATES this from the emitter (new wiring, same param).
- **NEW: `IdempotentInfoMessage` (string?, optional)** — append-only extension per D17.

Snapshot test `FcLifecycleWrapperParameterSurfaceTests.cs` asserts the five expected `[Parameter]` / `[EditorRequired]` annotations are present with exactly the expected names + types; a re-approved baseline prevents accidental removal or retyping during Story 2-5 or later.

## Fluent UI v5 Component Reference

Verify the following v5 components + parameters are used (no v4 naming regressions — Story 2-4 D8 precedent):
- `FluentDialog` — used via `IDialogService.ShowDialogAsync` (NOT inline `<FluentDialog>`).
- `FluentMessageBar` — `Intent`, `Title`, `AllowDismiss`, `ActionsTemplate` (rather than deprecated v4 parameters).
- `FluentButton` — `Appearance`, `Color`, `IconStart` (v5 names).
- `IDialogService.ShowDialogAsync<TDialog>` — verify parameter shape via `mcp__fluent-ui-blazor__get_component_details IDialogService` or `DialogService` at Task 0.2 if unsure.

`IToastService` is DEPRECATED in v5 (UX spec §492) — do NOT inject or reference. All surfaces in 2-5 use `FluentMessageBar` (inline) or `FluentDialog` (modal).

## Files Touched Summary

**Contracts/** (modified):
- `Attributes/DestructiveAttribute.cs` (NEW)
- `FcShellOptions.cs` (2 new properties)
- `Diagnostics/FcDiagnosticIds.cs` (4 new codes: HFC1020, HFC1021, HFC2103, HFC2104)

**SourceTools/** (modified):
- `Parsing/DomainModel.cs` — `CommandModel` gains 3 fields
- `Parsing/CommandParser.cs` — `[Destructive]` detection + HFC1020/HFC1021 emission
- `Transforms/CommandRendererModel.cs` — 3 new fields
- `Transforms/CommandRendererTransform.cs` — propagation
- `Emitters/CommandRendererEmitter.cs` — destructive gating + FullPage abandonment wrap
- `Emitters/CommandFormEmitter.cs` — `RejectionMessage` attribute wiring + helper method
- `Emitters/CommandFluxorFeatureEmitter.cs` — conditional on Task 0.1
- `AnalyzerReleases.Unshipped.md` — HFC1020 + HFC1021 rows
- `Diagnostics/*` descriptors — HFC1020 + HFC1021 descriptors

**Shell/** (new/modified):
- `Components/Forms/FcDestructiveConfirmationDialog.razor[.cs][.css]` (NEW trio)
- `Components/Forms/FcFormAbandonmentGuard.razor[.cs][.css]` (NEW trio)
- `Components/Lifecycle/FcLifecycleWrapper.razor` — idempotent Info bar branch
- `Components/Lifecycle/FcLifecycleWrapper.razor.cs` — `IdempotentInfoMessage` param + `ScheduleIdempotentDismiss` + HFC2104 log
- `Components/Lifecycle/LifecycleUiState.cs` — `IsIdempotent` field + mapper
- `Options/FcShellOptionsThresholdValidator.cs` — 2 new cross-property invariants
- `_Imports.razor` — add `@using Hexalith.FrontComposer.Shell.Components.Forms`

**samples/Counter/Counter.Web/** (modified):
- `appsettings.Development.json` — 2 new settings

**samples/Counter/Counter.Domain/** (OPTIONAL):
- `ResetCountCommand.cs` — demo destructive command (deferrable per Task 7.2)

**tests/Hexalith.FrontComposer.Shell.Tests/** (new/modified):
- `Components/Forms/FcDestructiveConfirmationDialogTests.cs` (6 tests)
- `Components/Forms/FcFormAbandonmentGuardTests.cs` (7 tests)
- `Components/Lifecycle/FcLifecycleWrapperIdempotentTests.cs` (5 tests)
- `Components/Lifecycle/FcLifecycleWrapperRejectionTests.cs` (5 tests)
- `Components/Lifecycle/FcLifecycleWrapperParameterSurfaceTests.cs` (1 test — new — locks down D17)
- `Options/FcShellOptionsValidationTests.cs` (2 new tests)
- `Generated/CommandRendererCompactInlineTests.cs` + `CommandRendererFullPageTests.cs` — modified assertions

**tests/Hexalith.FrontComposer.SourceTools.Tests/** (new/modified):
- `Parsing/CommandParserDestructiveTests.cs` (6 tests)
- `Emitters/CommandFormEmitterInputPreservationTests.cs` (1 regression test per D5)
- `Emitters/Snapshots/` — 3 new + 2 re-approved verified.txt files

## Naming Convention Reference

| Element | Pattern | Example |
|---|---|---|
| Destructive attribute | `DestructiveAttribute` / `[Destructive]` | — |
| Destructive dialog | `FcDestructiveConfirmationDialog` | — |
| Abandonment guard | `FcFormAbandonmentGuard` | — |
| New Options | `FormAbandonmentThresholdSeconds`, `IdempotentInfoToastDurationMs` | — |
| Destructive confirm params record | `FcDestructiveConfirmationParams` (NOT `FcDestructiveConfirmationDialogParameters` — keep terse) | — |
| Analyzer diagnostic IDs | `HFC1020`, `HFC1021` | `CommandParser` emits |
| Runtime diagnostic IDs | `HFC2103`, `HFC2104` | `ILogger` references `FcDiagnosticIds.HFC2xxx_*` constant |
| Wrapper new parameter | `IdempotentInfoMessage` (append-only D17) | `[Parameter] public string? IdempotentInfoMessage` |
| CSS class prefix | `fc-destructive-dialog-*`, `fc-form-abandonment-*` | — |

## Testing Standards

- xUnit v3 (3.2.2), Verify.XunitV3, Shouldly, NSubstitute, bUnit 2.7.2 — inherited from 2-1/2-2/2-3/2-4.
- `FakeTimeProvider` via `Microsoft.Extensions.TimeProvider.Testing` — reuse Story 2-4's package.
- `TestContext.Current.CancellationToken` on async tests (xUnit1051).
- `TreatWarningsAsErrors=true` global.
- `DiffEngine_Disabled: true` in CI.
- **Test count budget (L07):** **~36 new tests** (6 dialog + 7 abandonment guard + 5 idempotent wrapper + 5 rejection wrapper + 1 parameter surface lockdown + 6 parser + 1 regression + 2 options + ~3 renderer assertion updates). Cumulative target **~542**. L07 cost-benefit: tight coverage of the net-new components (dialog + guard) and a single regression test per invariant being relied upon. 23 decisions / 36 tests ≈ 1.6/decision — tighter than 2-4's 2.0/decision because the lifecycle-wrapper extension reuses established infrastructure.

## Build & CI

- Build race CS2012: `dotnet build` then `dotnet test --no-build` (inherited pattern)
- `AnalyzerReleases.Unshipped.md` MUST include HFC1020 (Warning) + HFC1021 (Error) rows — otherwise build fails with RS2008
- Roslyn 4.12.0 pinned (inherited)
- `Microsoft.FluentUI.AspNetCore.Components` stays at `5.0.0-rc.2-26098.1` — do NOT bump in this story
- No new CI jobs — everything rides the existing `dotnet test` pass. E2E latency gate from 2-4's Task 6 remains pre-existing.

## Previous Story Intelligence

**From Story 2-4 (immediate predecessor):**

- **Append-only parameter contract (D1)** — `FcLifecycleWrapper`'s three parameters are locked; 2-5 adds ONE new optional `IdempotentInfoMessage` per D17. A dedicated `FcLifecycleWrapperParameterSurfaceTests.cs` snapshot locks this down.
- **XSS invariant (D22)** — all adopter-supplied strings (Reason, Resolution, IdempotentInfoMessage) render as plain text. 2-5 adds regression tests to `FcLifecycleWrapperRejectionTests.cs` and `FcLifecycleWrapperIdempotentTests.cs`.
- **IdempotencyResolved flag (Known Gap G2 + Hindsight H-2)** — 2-4 logs HFC2101 when observed. 2-5 consumes the signal into the Info MessageBar. Copy is safe under both cross-user and self-reconnect contexts (D7).
- **HFC2xxx runtime-only logging precedent (architecture.md §648 + 2-3 HFC2004-7 + 2-4 HFC2100-2)** — 2-5's HFC2103/HFC2104 are runtime-logged, NOT analyzer-emitted — no `AnalyzerReleases.Unshipped.md` entry for those two.
- **FcShellOptions growth risk (2-4 demoted-ADR Dev Note)** — 2-5 adds 2 more properties → 10 total. Trigger for splitting into `FcLifecycleOptions` + `FcFormOptions` + `FcGridOptions` is **≥ 12 properties** OR a third cross-concern. 2-5 stays below the trigger; split remains Story 9-2.
- **Abandonment + ADR-022 page-reload cross-story coupling (2-4 Known Gap G11)** — Sally's recommended fix (a) was "sequence 2-5 to ship in the same release as 2-4." 2-5's release alongside or immediately-after 2-4 closes the regression window where "Start over" silently loses typed data. Release manager decision; spec documents the coupling.

**From Story 2-3:**

- **Single-writer invariant (D19)** — `ILifecycleStateService.Transition` is written only by the bridge. 2-5 never transitions lifecycle state; all new UX is in render-only surfaces or pre-submit gates.
- **RejectedAction payload contract (architecture.md §747)** — `{Command}Actions.RejectedAction(CorrelationId, Reason, Resolution)` is what the form dispatches; the reducer should project into feature state. Task 0.1 verifies the state-projection path exists end-to-end.

**From Story 2-2:**

- **ADR-016 renderer/form split** — form owns `<EditForm>`, renderer owns chrome. 2-5 adds the destructive dialog + abandonment guard to the RENDERER side (chrome), not the form. Confirmation gates submit-trigger invocation, abandonment wraps form children in FullPage mode.
- **`OnCollapseRequested` callback precedent** — Task 2-2 ADR-016 D11 documented the "Story 2-5 adds NavigateAwayRequested" TODO at `CommandRendererEmitter.cs:69`. 2-5 now backfills the contract.

**From Story 2-1:**

- **Input preservation on rejection (emitter L312-318)** — `_model` is NOT reset in the `catch` block. 2-5 locks this with `CommandFormEmitterInputPreservationTests.cs`.
- **`ResolveLabel` chain precedent** — `[Display(Name)]` → IStringLocalizer → humanized → raw. 2-5's `DisplayLabel` usage for destructive-button labels inherits this chain unchanged.

## Lessons Ledger Citations (from `_bmad-output/process-notes/story-creation-lessons.md`)

- **L01 Cross-story contract clarity** — Binding contracts with Stories 2-1 / 2-2 / 2-3 / 2-4 are explicitly enumerated in the cheat sheet. ADR-024/025/026 document the append-only + renderer-level ownership decisions.
- **L04 Generated name collision detection** — `FcDestructiveConfirmationDialog` + `FcFormAbandonmentGuard` are hand-written in a new `Components/Forms/` subfolder — zero collision risk with existing `Components/Lifecycle/` or `Components/Rendering/` files.
- **L05 Hand-written service + emitted per-type wiring** — Dialog + Guard are hand-written; emission of destructive-gating + abandonment-wrap per command is done by modifying `CommandRendererEmitter` (NOT by emitting per-command subclasses). Matches 2-4 L05 pattern.
- **L06 Defense-in-depth budget** — 24 Critical Decisions after elicitation (D24 added for validation-before-dialog gate + Leave-anyway flag try/finally) — still under the ≤25 feature-story cap. No Occam trim required.
- **L07 Test cost-benefit** — 36 tests / 23 decisions ≈ 1.6/decision, tighter than 2-4's 2.0/decision. Lean surface because most wiring re-uses existing infrastructure (FakeTimeProvider, IDialogService, NavigationLock built-in).
- **L08 Party review vs. elicitation** — Party review (Winston / Amelia / Sally / Murat) ran 2026-04-17 and surfaced field-naming bug (Task 0.1 blocker — Fluxor state uses `Error`/`Resolution`, NOT `RejectionReason`/`RejectionResolution`), fire-and-forget double-dispatch risk, Leave-anyway race, EditContext-exposure punt, rejection title coldness, idempotent 3s-SR-cutoff, CompactInline orphan, ActionPrompt-edge in D13, property-based testing gaps, and bUnit NavigationLock compat risk. Advanced elicitation (Pre-mortem / Red Team / Chaos / Hindsight / First Principles) ran immediately after and surfaced: P0-A idempotent-bar flood coupling with Story 5-4 (G13); P0-B validation-before-dialog fix (D24); P0-C HFC1020 adoption-blocker severity (D20 revised to Info); Red Team Attack-1 Story 6-3 XSS inheritance (ADR-024 Consequences); Red Team Attack-2 expanded HFC1020 regex; Red Team Attack-3 `_isLeaving` flag try/finally (D24); Chaos-1 JS-enabled assumption documented; Chaos-2 clock-skew (G14); Hindsight Retrospect-1 MCP agent destructive semantics (G15); First Principles IDirtyStateTracker growth path documented. Party caught coupling + architecture; elicitation caught security + edge cases — L08 validated in practice.
- **L09 ADR rejected-alternatives discipline** — ADR-024 cites 3, ADR-025 cites 2, ADR-026 cites 3. All ≥2 satisfied. ADR-024 surfaced the wrapper-owned alternative that nearly won (would have compromised 2-4 D1 append-only contract).
- **L10 Deferrals name a story** — All 16 Known Gaps cite specific owning stories (Story 10-x, Story 5-4, Story 6-3, Story 9-4, Story 4-5, Story 3-1/3-2, Story 8-3, v2, Out-of-scope).
- **L11 Dev Agent Cheat Sheet** — Present. Feature story with three distinct UX surfaces (rejection, confirmation, abandonment) + cross-story bindings warrants fast-path entry despite being under the 30-decision threshold.

## References

- [Source: _bmad-output/planning-artifacts/epics/epic-2-command-submission-lifecycle-feedback.md#Story 2.5 — AC source of truth, §1021-1078]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR36 — button hierarchy, §349]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR37 — destructive confirmation dialog, §350]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR38 — form abandonment protection, §351]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR39 — notification patterns, §352]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR46 — domain-specific error messages + input preservation, §359]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR58 — confirmation pattern rules, §371]
- [Source: _bmad-output/planning-artifacts/prd/functional-requirements.md#FR30 — exactly-one user-visible outcome, §1248]
- [Source: _bmad-output/planning-artifacts/prd/non-functional-requirements.md#NFR46 — domain-specific rejection messages, §149]
- [Source: _bmad-output/planning-artifacts/prd/non-functional-requirements.md#NFR47 — zero silent failures, §150]
- [Source: _bmad-output/planning-artifacts/prd/non-functional-requirements.md#NFR103 — technical, precise, concise, confident messages, §206]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/ux-consistency-patterns.md#Button hierarchy, §2217-2243]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/ux-consistency-patterns.md#Confirmation patterns, §2305-2352]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/ux-consistency-patterns.md#Form abandonment protection, §2318-2331]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/ux-consistency-patterns.md#Confirmation dialog pattern (destructive actions), §2333-2352]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/ux-consistency-patterns.md#Notification & MessageBar patterns, §2354-2376]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/user-journey-flows.md#Journey 5 — form rejection + input preserved, §1548-1562]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/user-journey-flows.md#Journey 6 — rejection + idempotent outcome, §1566-1632]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/index.md#Error recovery preserves intent (principle 4), §1674]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/index.md#Input preservation on rejection (principle), §1663]
- [Source: _bmad-output/planning-artifacts/ux-design-specification/ux-consistency-patterns.md#FluentMessageBar severity mapping, §2365-2370]
- [Source: _bmad-output/planning-artifacts/architecture.md#648 — HFC diagnostic ID ranges; 1xxx analyzer-emitted, 2xxx runtime-logged]
- [Source: _bmad-output/planning-artifacts/architecture.md#747 — Rejected action carries Reason + Resolution]
- [Source: _bmad-output/planning-artifacts/architecture.md#1144 — Contracts dependency-free (DestructiveAttribute + FcShellOptions extensions respect this)]
- [Source: _bmad-output/implementation-artifacts/2-4-fclifecyclewrapper-visual-lifecycle-feedback/critical-decisions-read-first-do-not-revisit.md#Decision D1 — wrapper append-only parameter surface]
- [Source: _bmad-output/implementation-artifacts/2-4-fclifecyclewrapper-visual-lifecycle-feedback/critical-decisions-read-first-do-not-revisit.md#Decision D17 — Rejected no-auto-dismiss]
- [Source: _bmad-output/implementation-artifacts/2-4-fclifecyclewrapper-visual-lifecycle-feedback/critical-decisions-read-first-do-not-revisit.md#Decision D22 — XSS plain-text rendering invariant]
- [Source: _bmad-output/implementation-artifacts/2-4-fclifecyclewrapper-visual-lifecycle-feedback/known-gaps-explicit-not-bugs.md#Known Gap G2 + Hindsight H-2 — IdempotencyResolved cross-user-vs-self-reconnect disambiguation]
- [Source: _bmad-output/implementation-artifacts/2-4-fclifecyclewrapper-visual-lifecycle-feedback/known-gaps-explicit-not-bugs.md#Known Gap G11 — abandonment sequencing with ADR-022 page-reload]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management/critical-decisions-read-first-do-not-revisit.md#Decision D19 — single-writer invariant]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management/dev-notes.md#Patch P-11 — ResetToIdleAction CorrelationId plumbing]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes/architecture-decision-records.md#ADR-016 — renderer/form split]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes/tasks-subtasks.md#OnCollapseRequested + Story 2-5 NavigateAwayRequested TODO — CommandRendererEmitter.cs:69]
- [Source: _bmad-output/implementation-artifacts/2-1-command-form-generation-and-field-type-inference.md#Emitter L312-318 — input preservation on rejection]
- [Source: _bmad-output/implementation-artifacts/2-1-command-form-generation-and-field-type-inference.md#ResolveLabel chain — domain-language labels]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md — running list of known deferrals]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L01-L11 — all lessons applied]
- [Source: memory/feedback_no_manual_validation.md — automated bUnit + FakeTimeProvider preferred over manual validation]
- [Source: memory/feedback_cross_story_contracts.md — explicit cross-story contracts per ADR-016 canonical example; ADR-024/025/026 here mirror the pattern]
- [Source: memory/feedback_tenant_isolation_fail_closed.md — D18 inherits from 2-3 D13 / 2-4 D13 (ephemeral, no persisted data)]
- [Source: memory/feedback_defense_budget.md — 23 decisions, under the ≤25 feature-story cap]
- [Source: src/Hexalith.FrontComposer.Contracts/Communication/CommandRejectedException.cs — Reason + Resolution surface]
- [Source: src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs — existing thresholds extended in Task 1.2]
- [Source: src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor[.cs][.css] — base for extensions in Task 5]
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs — L312-318 rejection catch, L366 wrapper attribute block]
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs — L69 Story 2-5 TODO, FullPage branch for abandonment wrap]
- [Source: src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs + DomainModel.cs — destructive parsing extension in Task 2]
- [Source: src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md — HFC1020/HFC1021 entry target]

## Project Structure Notes

- **Alignment with architecture blueprint** (architecture.md §920-935, §648):
  - New `Shell/Components/Forms/` subfolder for `FcDestructiveConfirmationDialog` + `FcFormAbandonmentGuard` — consistent with existing `Shell/Components/Lifecycle/` and `Shell/Components/Rendering/` organization. The `Forms/` folder groups form-adjacent cross-cutting UX components.
  - `DestructiveAttribute` lives in `Contracts/Attributes/` alongside `CommandAttribute`, `ProjectionAttribute`, `BoundedContextAttribute`, `IconAttribute`, etc. — consistent adopter-facing attribute placement.
  - `FcShellOptions` extension stays in Contracts (architecture.md §1144 dependency-free invariant preserved — the two new `int` properties pull no new namespace).
  - HFC1020 (analyzer Warning) and HFC1021 (analyzer Error) land in the `HFC1xxx` range per §648 diagnostic-ID policy (`1xxx` = analyzer-emitted, `2xxx` = runtime-logged). HFC2103/HFC2104 land in the `HFC2xxx` range.
  - No Contracts → Shell reverse reference — both `Forms/` components live in Shell. The emitter's emitted `.g.cs` references `Hexalith.FrontComposer.Shell.Components.Forms` via emitted `using` directive (Task 6.1), same pattern as Story 2-4 D21 for `Lifecycle` components.
- **Fluent UI `Fc` prefix convention** honored — `FcDestructiveConfirmationDialog`, `FcFormAbandonmentGuard`.
- **Append-only `FcShellOptions` extension honoured** — 2 properties added to existing class, no new options type created. Crosses the 10-property threshold; split trigger remains 12 properties or 3rd cross-concern (Story 9-2 owns split).

---
