# Story 2.5: Command Rejection, Confirmation & Form Protection

Status: ready-for-dev

---

## Dev Agent Cheat Sheet (Read First — 2 pages)

> Amelia-facing terse summary. Authoritative spec is the full document below. Every line links to a section for detail.

**Goal:** Close Epic 2's UX by wiring **three pre- and post-submit protection surfaces** onto the lifecycle pipeline that Stories 2-1 → 2-4 already deliver: (1) **domain-specific rejection copy** (`"{Reason}. {Resolution}"` sourced from `CommandRejectedException` via Fluxor state) rendered by `FcLifecycleWrapper.RejectionMessage` with input preservation; (2) **idempotent-outcome UX** — `IdempotencyResolved==true` → `FluentMessageBar` (Info) with 3 s auto-dismiss copy "This was already confirmed — no action needed." (the copy deliberately spans both Story 2-4 Known-Gap G2 interpretations: cross-user confirm AND self-reconnect replay per 2-4 H-2); (3) **destructive command confirmation dialog** (`FluentDialog`, Cancel auto-focused) opt-in via new `[Destructive]` attribute with build-time `HFC1020` analyzer warning for Delete/Remove/Purge-named commands missing the attribute + `HFC1021` error for destructive-with-0-fields; (4) **full-page form abandonment protection** (`NavigationLock` + `LocationChanging` on `CommandRenderMode.FullPage` only; 30 s threshold on `FcShellOptions.FormAbandonmentThresholdSeconds`; inline `FluentMessageBar` Warning with Stay-on-form/Leave-anyway buttons — never a modal); (5) **button hierarchy enforcement** (Primary one-per-context, Secondary, Outline, Danger — all with domain-language labels + leading icons on Primary and row actions).

**Scope boundary:** Epic 2 happy path (stable connection). Error aggregation (UX-DR39 — >2 errors in 5 s window → aggregated bar "N commands failed. [Show details]") is **Story 10-x** (CI-gated UX polish). Bulk destructive confirmation ("Approve 12 orders?") is **v2**. MessageBar stacking-cap enforcement (max 3 visible) is deferred to Story 10-x. Real-time multi-user presence awareness is **v2**. Form abandonment on **compact inline** expand-in-row (2-4 field) forms is NOT shipped — only `FullPage` (epics.md §1055 is explicit; expand-in-row collapse is already handled by Story 2-2 AC9 Escape-to-collapse). Rejected MessageBar auto-dismiss remains **disabled** (Story 2-4 D17 invariant preserved — explicit user dismissal only).

**Binding contract with Story 2-4 (D1 wrapper parameter surface is APPEND-ONLY through v1):**
- 2-5 MAY ADD optional parameters to `FcLifecycleWrapper` (e.g., `IdempotentInfoMessage`) — MUST NOT remove, rename, or change types of `CorrelationId` / `ChildContent` / `RejectionMessage`. Breaking the contract cascades through every re-emitted form from `CommandFormEmitter` and re-invalidates Story 2-1 snapshot gates.
- 2-5 consumes 2-4's `RejectionMessage` parameter as-is; the wrapper already renders it as **plain text** (Story 2-4 D22 XSS invariant). 2-5 does NOT introduce `MarkupString`, a sanitizer, or "allow-rich-formatting" parameter — carries 2-4 D22 forward verbatim.
- 2-5 consumes 2-4's `IdempotencyResolved` transition flag (already surfaced via `HFC2101` log in 2-4) and branches rendering on it. HFC2101 remains logged as Information — 2-5 does NOT escalate it to Warning.
- 2-5 does NOT reshape `LifecycleThresholdTimer`, `LifecycleUiState`, or `FcLifecycleWrapper.razor.cs`'s transition state machine — changes are additive: new `CommandLifecycleState.IdempotentConfirmed` is NOT introduced (Story 2-4 ADR-017 explicitly keeps 5 states — the flag `IdempotencyResolved` is the sixth signal without a sixth state).

**Binding contract with Stories 2-1 / 2-2 / 2-3:**
- `CommandFormEmitter` (Story 2-1) already preserves `_model` on rejection (emitter L314 — `catch (CommandRejectedException ex) { Dispatcher.Dispatch(new RejectedAction(correlationId, ex.Message, ex.Resolution)); }` — NO `_model` reset; 2-5 ships a **regression test only** to lock this contract — D5). The emitter's validation-state notify (`_editContext?.NotifyValidationStateChanged();` at L316) is also preserved.
- `CommandRendererEmitter` (Story 2-2) exposes `OnCollapseRequested` callback + emits the `// Story 2-5 adds NavigateAwayRequested` TODO at L69. 2-5 **backfills that TODO** by adding an `OnNavigateAwayRequested` callback parameter **only on `CommandRenderMode.FullPage`** branch of `CommandRendererEmitter.EmitBuildRenderTree`. CompactInline + Inline paths are NOT modified in 2-5 — they already have Escape/close affordances.
- `CommandRendererEmitter`'s FullPage branch gains a wrapping `<FcFormAbandonmentGuard>` around the existing `<div style="max-width: ...">` container (after the breadcrumb `<nav>`, enclosing the `<CommandForm>` invocation). The guard component owns the `NavigationLock` + warning bar.
- `ICommandServiceWithLifecycle` + `CommandRejectedException` (Story 2-3) unchanged. 2-5 reads the rejection `Reason` + `Resolution` from the `{Command}LifecycleFeatureState` Rejected action payload; does **not** widen the service interface.
- Story 2-3 D19 single-writer invariant preserved — 2-5 never calls `ILifecycleStateService.Transition` (only the bridge does). No new writers.
- `CommandFormEmitter` is extended to pass `RejectionMessage` computed from `LifecycleState.Value.RejectionReason + ". " + LifecycleState.Value.RejectionResolution` into `FcLifecycleWrapper`. The Fluxor `{Command}LifecycleFeatureState` emitted by `CommandFluxorFeatureEmitter` (Story 2-3) already carries `RejectionReason` and `RejectionResolution` fields (per architecture.md §747 — `Rejected(CorrelationId, Reason, Resolution)` — **verify presence in Task 0**; if missing, scope expands to include adding those reducer projections).

**ADR-024 one-liner:** Destructive confirmation is a **pre-submit** dialog owned by `CommandRendererEmitter` (not the lifecycle wrapper). Wrapper remains post-submit only.
**ADR-025 one-liner:** Form abandonment uses Blazor's built-in `NavigationLock` + `LocationChanging` — NOT `IJSRuntime.beforeunload`. SSR-safe, no JS round-trip.
**ADR-026 one-liner:** Destructive is opt-in via `[Destructive]` attribute (NOT a name heuristic). Analyzer `HFC1020` warns on Delete/Remove/Purge-named commands missing the attribute.

**Files to create / extend:**

| Path | Action |
|---|---|
| `src/Hexalith.FrontComposer.Contracts/Attributes/DestructiveAttribute.cs` | Create — marks a command class as requiring confirmation (Task 1.1, D1) |
| `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs` | Modify — add `FormAbandonmentThresholdSeconds` + `IdempotentInfoToastDurationMs` (Task 1.2, D6, D10) |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | Modify — reserve `HFC1020`, `HFC1021`, `HFC2103` (Task 1.3) |
| `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` | Modify — register HFC1020 Warning + HFC1021 Error entries (Task 1.3b) |
| `src/Hexalith.FrontComposer.SourceTools/Diagnostics/FcDiagnosticDescriptors.cs` *(or existing diagnostic file)* | Modify — add descriptors for HFC1020 / HFC1021 (Task 1.3b) |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs` | Modify — add `IsDestructive` to `CommandModel`; add `DestructiveConfirmTitle` / `DestructiveConfirmBody` optional properties from attribute args (Task 2.1) |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs` | Modify — detect `[Destructive]`, emit `HFC1020` if command name ∈ {`Delete*`, `Remove*`, `Purge*`} and attribute absent; emit `HFC1021` if `IsDestructive && NonDerivableProperties.Count == 0` (Task 2.2) |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs` | Modify — pipe `IsDestructive` / `DestructiveConfirmTitle` / `DestructiveConfirmBody` through (Task 2.3) |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs` | Modify — propagate destructive fields from `CommandModel` → `CommandRendererModel` (Task 2.3) |
| `src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor` | Create — reusable `FluentDialog` with Cancel (auto-focus, Secondary) + destructive (Danger) buttons (Task 3.1, D11, D12) |
| `src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.cs` | Create — `[Parameter] Title`, `[Parameter] Body`, `[Parameter] DestructiveLabel`, `[Parameter] EventCallback OnConfirm`, `[Parameter] EventCallback OnCancel` (Task 3.1) |
| `src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor.css` | Create — minimal layout only; zero-override per UX-DR57 (Task 3.1) |
| `src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor` | Create — wraps full-page form children; installs `NavigationLock` + warning `FluentMessageBar` (Task 4.1, D8, D9) |
| `src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor.cs` | Create — tracks first-edit timestamp via `EditContext.OnFieldChanged`, consults `FcShellOptions.FormAbandonmentThresholdSeconds`, exposes `[Parameter] EditContext? EditContext`, `[Parameter] RenderFragment? ChildContent`, `[Parameter] EventCallback<LocationChangingContext> OnNavigateAway` (Task 4.1) |
| `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor` | Modify — add Info `FluentMessageBar` branch for `IdempotencyResolved` state (Task 5.1, D6, D7) |
| `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs` | Modify — extend `LifecycleUiState` (via new field `IsIdempotent`); schedule Info-dismiss at `IdempotentInfoToastDurationMs`; expose new **optional** parameter `IdempotentInfoMessage` honoring D17 append-only contract (Task 5.1) |
| `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/LifecycleUiState.cs` | Modify — add `IsIdempotent` bool to the record; update `From(...)` mapper to set it from `transition.IdempotencyResolved && NewState == Confirmed` (Task 5.1) |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Modify — in the `BuildRenderTree` wrapper-attribute block (L366), emit `builder.AddAttribute(seq++, "RejectionMessage", BuildRejectionMessage());` and emit a new `private string? BuildRejectionMessage() => LifecycleState.Value.State == CommandLifecycleState.Rejected ? BuildRejectionCopy(LifecycleState.Value.RejectionReason, LifecycleState.Value.RejectionResolution) : null;` helper. Verify emitted Fluxor state's projection includes `RejectionReason` + `RejectionResolution` fields (Task 5.2) |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFluxorFeatureEmitter.cs` | Verify/Modify — ensure feature state has `RejectionReason` + `RejectionResolution` string fields mapped from the Rejected action reducer. If absent, add reducer projection (Task 5.2b) |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` | Modify — for FullPage branch: wrap the inner content in `<FcFormAbandonmentGuard>` and pass `EditContext` (exposed by form via new parameter); for every branch where `model.IsDestructive == true`: emit destructive-dialog gating on submit (Task 6.1, D2, D11, D19) |
| `src/Hexalith.FrontComposer.Shell/_Imports.razor` | Modify — add `@using Hexalith.FrontComposer.Shell.Components.Forms` (Task 6.2) |
| `samples/Counter/Counter.Domain/` | Verify — Counter sample has no destructive commands currently (Increment is not destructive). No changes required beyond a new `ResetCommand` *optional demonstration*; documented as **optional in Task 7 only, NOT a story-blocker** if sample authoring time is tight |
| `samples/Counter/Counter.Web/appsettings.Development.json` | Modify — add `FormAbandonmentThresholdSeconds: 30` + `IdempotentInfoToastDurationMs: 3000` demo values (Task 7.1) |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Forms/FcDestructiveConfirmationDialogTests.cs` | Create — 6 bUnit tests: renders title/body, Cancel auto-focused, ESC closes via OnCancel, destructive button has Danger appearance, Enter on Cancel does NOT fire OnConfirm (auto-focus invariant), `DestructiveLabel` domain-language default (Task 8.1) |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Forms/FcFormAbandonmentGuardTests.cs` | Create — 7 bUnit tests with `FakeTimeProvider` and simulated `LocationChangingContext`: below-threshold does NOT block, above-threshold blocks, first-edit anchors timer, "Stay on form" cancels nav, "Leave anyway" proceeds, no warning while Submitting (D13), auto-focus on "Stay on form" (Task 8.2) |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperIdempotentTests.cs` | Create — 5 bUnit tests: `IdempotencyResolved==true` + Confirmed → Info MessageBar renders, dismisses after `IdempotentInfoToastDurationMs`, copy is `"This was already confirmed — no action needed."`, `aria-live="polite"`, XSS guard preserved for `IdempotentInfoMessage` parameter (Task 8.3) |
| `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/FcLifecycleWrapperRejectionTests.cs` | Create — 5 bUnit tests: rejection renders `"{Reason}. {Resolution}"`; plain-text encoding (D14 XSS); no auto-dismiss (2-4 D17 regression); `aria-live="assertive"`; generic fallback when both Reason + Resolution null (Task 8.4) |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserDestructiveTests.cs` | Create — 6 tests: `[Destructive]` sets `IsDestructive=true`; `DeleteOrderCommand` without attribute → HFC1020 Warning; `Delete0FieldCommand` with `[Destructive]` + 0 non-derivable fields → HFC1021 Error; `ResetCommand` without attribute → no diagnostic; optional `DestructiveConfirmTitle`/`DestructiveConfirmBody` captured from attribute args; analyzer release entries present (Task 8.5) |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitter.Destructive_*.verified.txt` | Create / re-approve — 3 new snapshots: destructive CompactInline, destructive FullPage, non-destructive FullPage (baseline) (Task 8.6) |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitter.*.verified.txt` | Re-approve — wrapper now receives `RejectionMessage` attribute with helper expression; inspect diffs before approval (Task 8.6) |
| `tests/Hexalith.FrontComposer.Shell.Tests/Options/FcShellOptionsValidationTests.cs` | Modify — add 2 tests: `FormAbandonmentThresholdSeconds` range [5, 600]; `IdempotentInfoToastDurationMs` range [1_000, 30_000] (Task 8.7) |
| `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererCompactInlineTests.cs` + `CommandRendererFullPageTests.cs` | Modify — add 1 assertion per file: non-destructive path does NOT render the destructive dialog; FullPage path renders `FcFormAbandonmentGuard` (Task 8.8) |

**AC quick index (details in Acceptance Criteria section below):**

| AC | One-liner | Task(s) |
|---|---|---|
| AC1 | Domain-specific rejection copy `"{Reason}. {Resolution}"` rendered in wrapper Danger MessageBar; no auto-dismiss (2-4 D17 invariant preserved); `aria-live="assertive"`; form input preserved | 5.2, 8.4 |
| AC2 | Idempotent outcome (IdempotencyResolved==true + Confirmed) → `FluentMessageBar` (Info), copy `"This was already confirmed — no action needed."`, auto-dismiss at `IdempotentInfoToastDurationMs` (default 3000 ms), `aria-live="polite"`; also hardened against XSS via plain-text rendering for `[Parameter] IdempotentInfoMessage` | 5.1, 8.3 |
| AC3 | `[Destructive]` attribute on command class → renderer gates submit behind `FcDestructiveConfirmationDialog` (Cancel auto-focused Secondary, action Danger, title = `{DisplayLabel}?`, body = "This action cannot be undone.") — ESC closes | 3.1, 6.1, 8.1 |
| AC4 | Destructive commands MUST NOT render as `Inline` (0-field button) — analyzer `HFC1021` Error at parse time; destructive commands MUST render expand-in-row or full-page | 2.2, 8.5 |
| AC5 | Non-destructive commands (Approve/Create/Update) show **no** confirmation dialog (wrapper + in-button spinner cover feedback per 2-4) | 8.8 |
| AC6 | FullPage form active for > `FormAbandonmentThresholdSeconds` (default 30 s from **first field edit**, not from mount) → attempted nav intercepted; `FluentMessageBar` (Warning) appears at form top with "Stay on form" (Primary, auto-focus) + "Leave anyway" (Secondary); `NavigationLock` is released on confirmed leave; CompactInline + Inline are NOT guarded | 4.1, 6.1, 8.2 |
| AC7 | Button hierarchy enforced in emitted renderers: Primary one-per-context (submit), Secondary for Cancel/supporting, Outline for row-inline action trigger, Danger only for destructive action button inside dialog; ALL labels humanized via `ResolveLabel` chain (`[Display(Name)]` → IStringLocalizer → humanized → raw); Primary and DataGrid row actions include `IconStart` | 6.1, 8.8 |
| AC8 | `HFC1020` Warning on Delete/Remove/Purge-named commands missing `[Destructive]` — build does NOT fail unless `TreatWarningsAsErrors=true` (it is, per Story 1-7); diagnostic message guides adopter to add the attribute or rename the command | 2.2, 8.5 |
| AC9 | Error recovery fit-for-purpose (NFR103): rejection copy explains what failed + why + what happened to data, and the user can retry (submit button re-enables per 2-4 AC7) or modify input without re-typing anything (D5 preservation contract) | 5.2, 8.4 |

**Scope guardrails (do NOT implement — see Known Gaps):**
- Error aggregation (`>2 errors in 5 s window`) → **Story 10-x** (UX CI polish)
- Bulk destructive confirm ("Approve 12 orders?") → **v2**
- MessageBar stacking cap (max 3 visible) → **Story 10-x**
- Multi-user presence indicators ("also viewing: user") → **v2**
- Form abandonment on **CompactInline** expand-in-row → **NOT shipping** (epics §1055 explicitly scopes to full-page; Story 2-2 Escape-to-collapse suffices)
- SignalR disconnect UX during Rejected → **Story 5-3** (connection-aware lifecycle)
- Custom destructive dialog content-slot override (adopter-provided Razor fragment) → **Epic 6** (customization gradient)
- Visual regression baselines for destructive dialog + abandonment bar → **Story 10-2**
- Cross-user vs self-reconnect idempotent disambiguation (Story 2-4 G2 + 5-4) → copy deliberately safe under both; **Story 5-4** may later extend `CommandLifecycleTransition` with a source flag if UX demands differentiation
- Destructive-attribute parameterisation beyond `Title` / `Body` (e.g., typed data-preview fragment) → **Epic 6**
- `[Destructive]` adopter analyzer escape hatch (`SuppressHFC1020` attribute) → **Story 9-4** (diagnostic ID system and deprecation policy)

**4 new diagnostic codes reserved:**
- **HFC1020 Warning (analyzer-emitted)** — command named `Delete*` / `Remove*` / `Purge*` lacks `[Destructive]` attribute. Auto-fix: add attribute OR rename command to non-destructive verb.
- **HFC1021 Error (analyzer-emitted)** — `[Destructive]` command has zero non-derivable fields (would render as `Inline` 0-field button, but destructive commands MUST NOT render inline per UX-DR36 + epics §1048).
- **HFC2103 Warning (runtime log)** — `FcFormAbandonmentGuard` detected `LocationChanging` while the lifecycle service reports `Submitting` — warning logged and the guard yields to the submission (no warning bar shown); in practice this is rare because `NavigationLock` isn't normally armed during submit (D13).
- **HFC2104 Information (runtime log)** — `FcLifecycleWrapper` rendered the idempotent Info bar; logged to allow adopters to measure frequency and calibrate copy. Uses correlation-ID hashing (Story 2-4 Red Team RT-4).

**Test expectation: ~36 new tests, cumulative ~542.** Breakdown at Task 8. Per L06 budget (≤25 decisions for feature story, we land at **23**), tests scale with decisions at ~1.6/decision — tighter than Story 2-4's ~2.0/decision because destructive-dialog + abandonment-guard are lean standalone components and the wrapper changes are additive.

**Start here:** Task 0 (verify Fluxor feature state exposes `RejectionReason` + `RejectionResolution`; if absent, scope a reducer-projection extension in Task 5.2b) → Task 1 (Contracts: attribute + options + diagnostic IDs) → Task 2 (Parser + analyzer) → Task 3 (Dialog component) → Task 4 (Abandonment guard component) → Task 5 (Wrapper extension) → Task 6 (Emitter integration + renderer wrap) → Task 7 (Counter sample config + optional destructive demo) → Task 8 (tests: bUnit + snapshot + parser).

**The 23 Decisions and 3 ADRs below are BINDING. Do not revisit without raising first.**

---

## Story

As a business user,
I want domain-specific rejection messages that tell me what went wrong, destructive action confirmation dialogs that prevent accidents, and form abandonment protection that saves my work,
so that I never lose data to accidental navigation, never misunderstand an error, and never accidentally destroy something.

---

## Critical Decisions (READ FIRST — Do NOT Revisit)

These decisions are BINDING. Tasks reference them by number. If implementation uncovers a reason to change one, raise it before coding, not after.

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | **Destructive classification is opt-in via a new `[Destructive]` attribute on the command class.** NOT a name heuristic, NOT runtime reflection. Attribute signature: `public sealed class DestructiveAttribute : Attribute { public string? ConfirmationTitle { get; init; } public string? ConfirmationBody { get; init; } }`. Both properties are optional; when null the renderer falls back to the humanized command `DisplayLabel + "?"` for title and a localized `"This action cannot be undone."` for body. | Name heuristics false-positive (`RemoveOutlierCommand` might be reversible) and false-negative (`ArchiveCommand` may be destructive for certain tenants). Opt-in is the safe default. Analyzer `HFC1020` catches the common miss by pattern-matching command names (see D20) — a soft warning, not a block. |
| D2 | **Destructive confirmation dialog is a pre-submit gate emitted by `CommandRendererEmitter`, NOT a post-submit surface of `FcLifecycleWrapper`.** The renderer wraps the form's submit trigger with a dialog-open callback. `_externalSubmit` (the form-registered submit delegate from Story 2-2 ADR-016) is only invoked after the user confirms the dialog. FcLifecycleWrapper remains strictly post-submit — its job is to render Submitting/Syncing/Confirmed/Rejected outcomes, not intercept them. | Matches the renderer's Story 2-2 ownership of chrome around the form (ADR-016). Keeps the wrapper's 5-phase timer untouched and append-only per 2-4 D1. Intercepting submit inside the wrapper would require the wrapper to introspect intent, break the renderer/form split, and leak confirmation semantics into every post-submit render. |
| D3 | **`IdempotencyResolved` signal is consumed by `FcLifecycleWrapper`** (NOT by a sixth lifecycle state nor by a separate component). Wrapper extends `LifecycleUiState` with an additive `bool IsIdempotent` field populated from `transition.IdempotencyResolved && transition.NewState == CommandLifecycleState.Confirmed`. A `FluentMessageBar` (Info intent) branch in the Razor template renders when `_state.IsIdempotent`. Dismiss is scheduled via the same `ITimer`-via-`TimeProvider` pattern Story 2-4 D5 established, just with a different duration (`FcShellOptions.IdempotentInfoToastDurationMs`, default 3000 ms). | Preserves 2-3 ADR-017's explicit "5 states is the contract" invariant. Avoids a new API surface (no `IdempotentConfirmed` enum value), avoids a new component, reuses the proven `ScheduleConfirmedDismiss` pattern from 2-4. Leverages the signal Story 2-4 already logs (HFC2101). |
| D4 | **Rejection copy format is `$"{Reason}. {Resolution}"` (with period + space join), sourced from the Fluxor state's `RejectionReason` + `RejectionResolution` fields populated by the reducer from `RejectedAction(CorrelationId, Reason, Resolution)`.** Empty/whitespace Resolution → copy is just `Reason`. Null Reason → wrapper falls back to the localized generic from Story 2-4 AC7. Join is concatenation in C#, never Razor markup (D14 XSS invariant). Locale-aware period-vs-full-stop rendering is out of v0.1 scope (English + French share the period glyph; logged as a future concern, not a Known Gap). | Matches epics §1031 format `"[What failed]: [Why]. [What happened to the data]."` — our implementation splits: `{Reason}` carries "What failed: Why" (one clause, dev shapes it), `{Resolution}` carries "What happened to the data" (second clause). Adopters already produce `CommandRejectedException(reason, resolution)` at the service layer (architecture.md §747 + `CommandRejectedException.cs`). No new API. |
| D5 | **Input preservation on rejection is an existing contract tested by regression, not new code.** Story 2-1 `CommandFormEmitter` already does NOT reset `_model` in the `catch (CommandRejectedException)` branch (verified at L312-318 of `CommandFormEmitter.cs`). 2-5 ships ONE explicit regression test in `CommandFormEmitterInputPreservationTests.cs` that verifies the emitted code has no `_model = new()` after rejection. | Locking down a working behaviour with a test is cheaper than re-implementing it and risks less regression. The UX-DR46 invariant is behavioural, not structural — a test is the right enforcement vehicle. |
| D6 | **Three thresholds live on `FcShellOptions`** (extending Story 2-4's four — NOT creating a new options type): `FormAbandonmentThresholdSeconds` (default 30, range [5, 600], unit: seconds — NOTE the unit differs from 2-4's millisecond thresholds because the epic-spec uses "30 seconds" verbatim in §1050; exposing the human-readable unit reduces adopter mis-configuration), `IdempotentInfoToastDurationMs` (default 3000, range [1_000, 30_000] ms). Cross-property validator (2-4 ADR-023's `FcShellOptionsThresholdValidator`) is extended with two additional invariants: `FormAbandonmentThresholdSeconds * 1000 > StillSyncingThresholdMs` (abandonment must not fire before user could have seen sync status) and `IdempotentInfoToastDurationMs <= ConfirmedToastDurationMs` (idempotent Info dismisses no later than Confirmed Success for UX consistency). | Single source of truth for shell config (2-4 demoted ADR-023 to a Dev Note, keeping 8-property cap until 12; this adds 2 more → 10, still under the demote-trigger threshold). Seconds-unit for abandonment matches epics §1055 and UX spec §2330 verbatim; forces the dev to see the magnitude. Cross-property validator follows 2-4's precedent. |
| D7 | **Idempotent copy is "This was already confirmed — no action needed." (English; localized in resource file).** Deliberately generic to serve both Story 2-4 Known-Gap G2 interpretation axes (H-2): **cross-user context** ("another user already approved it") AND **self-reconnect replay context** ("your own Confirmed is being replayed"). 2-5 does NOT differentiate; if Story 5-4 later adds a `ReplaySource` field to `CommandLifecycleTransition` distinguishing cross-user from self-reconnect, 2-5's copy remains safe under both (no regression required), and 5-4 can OPTIONALLY refine the copy at that time. The current copy reads correctly whether a coworker, a reconnect, or a duplicate retry triggered the idempotent path. | Story 2-4 H-2 Pre-mortem explicitly warned that rendering "already approved by another user" under a self-reconnect replay would be user-hostile. Our copy is the minimal safe intersection. The epics §1039 illustrative phrasing ("This order was already approved (by another user). No action needed.") is ASPIRATIONAL adopter-wording guidance, not framework copy — adopters may override via the `IdempotentInfoMessage` parameter when they have enough context to safely attribute the outcome. |
| D8 | **Form abandonment uses Blazor 8+ built-in `NavigationLock` + `LocationChanging` — NOT `IJSRuntime.beforeunload`.** `NavigationLock` is wrapped inside `FcFormAbandonmentGuard` and arms whenever `(UtcNow - firstEditAt) >= FormAbandonmentThresholdSeconds`. The `LocationChanging` callback shows the inline `FluentMessageBar` (Warning) + calls `ctx.PreventNavigation()`. On "Leave anyway" the guard calls `NavigationLock.Confirm()` (or removes the lock and re-navigates via `NavigationManager.NavigateTo(target)`). | Blazor 8+ `NavigationLock` is SSR-safe, works identically under Blazor Server / WebAssembly / Auto, and needs no JS interop (no CSP wrinkle, no pre-render race). `beforeunload` shows a browser-native dialog with non-customizable copy — violates UX-DR38 + UX-DR57 (zero-override, framework-chosen copy). |
| D9 | **Abandonment warning is an inline `FluentMessageBar` at the form top, NOT a modal dialog.** Two action buttons: "Stay on form" (Primary appearance, auto-focused via `@ref` + `FocusAsync` in `OnAfterRenderAsync`) + "Leave anyway" (Secondary). The bar is rendered above the form's existing content (before the `<EditForm>` but inside `FcFormAbandonmentGuard`'s wrapper div). Escape triggers "Stay on form" (preserve work by default). | Modal would yank the user out of the form context they're trying to preserve — the bar leaves the form visible behind it. Inline matches UX spec §2320-2330 ASCII exactly. Auto-focus on "Stay" (not "Leave") follows the Cancel-auto-focus rationale from destructive dialog (D11) — Enter does the safe thing. |
| D10 | **Abandonment timer anchors on `EditContext.OnFieldChanged` first-fire, NOT on form mount.** If the user opens a full-page form but never types anything, no abandonment protection fires even after 30 s elapse — they have nothing to lose. The guard subscribes via `EditContext.OnFieldChanged += OnFirstEdit` where `OnFirstEdit` captures `_firstEditAt = Time.GetUtcNow()` and unsubscribes. Uses `TimeProvider` for `FakeTimeProvider` deterministic tests (inherits 2-4 D5 pattern). | UX spec §2313 explicitly says "below 30 seconds, the user likely just glanced" — we go one step further: "no input = no abandonment protection," which is strictly safer than time-only (false-positives zero, false-negatives only when a user types within the first milliseconds and regrets instantly — acceptable trade). |
| D11 | **`FcDestructiveConfirmationDialog` uses `FluentDialog` (v5)** opened via `IDialogService.ShowDialogAsync<FcDestructiveConfirmationDialog>(parameters)` pattern. Cancel button is `Appearance=Secondary` with `data-autofocus="true"` + explicit `OnAfterRenderAsync → Cancel.FocusAsync()` fallback. Destructive action button is `Appearance=Accent` with `Color=ButtonColor.Error` (Fluent UI v5 danger slot). Escape dispatches the `OnCancel` callback. `aria-label` on the dialog matches the action name. | FluentDialog (v5) provides the standard focus-trap + ESC handling, matching UX spec §2333 destructive-pattern ASCII. `IDialogService` is registered via `services.AddFluentUIComponents()` (already wired by Story 1-8). Using `IDialogService` over inline `<FluentDialog>` avoids DOM-nesting pitfalls and lets the dialog sit in the portal layer for z-index sanity. |
| D12 | **Destructive dialog title defaults to `{DisplayLabel}?` and body defaults to the localized `"This action cannot be undone. [BoundedContextLabel] {entity} will be permanently affected."` when `[Destructive(ConfirmationTitle, ConfirmationBody)]` properties are null.** Adopters customize per-command via attribute args; the fallback preserves domain-language consistency (humanized via Story 2-1's `ResolveLabel` chain — no hardcoded "Delete"). Destructive button label is `DisplayLabel` verbatim (e.g., "Delete Order", NOT "OK" / "Submit"). | UX spec §2350 mandates domain-language label on destructive button. The default-body entity-name inclusion makes adopter-light configurations feel intentional; `[Destructive]` alone produces a coherent dialog without forcing the adopter to write the copy. |
| D13 | **Abandonment protection is SUPPRESSED while the form is in Submitting/Syncing state.** `FcFormAbandonmentGuard` injects `ILifecycleStateService` and queries the bound correlation ID's current state on every `LocationChanging` call. If state ∈ `{Submitting, Syncing}`, the guard logs `HFC2103` (Warning) and **skips the warning bar** (does NOT `PreventNavigation`) — the user's navigation proceeds and the lifecycle wrapper's own Still-syncing / action-prompt handles the delay. | The abandonment bar and the lifecycle wrapper competing for the same user-attention layer would be a UX disaster. Submitting is a high-salience state (spinner + disabled submit + live-region announce); stacking another modal warning on top is redundant and user-hostile. |
| D14 | **Rejection `Reason` + `Resolution` and `IdempotentInfoMessage` are rendered as PLAIN TEXT, NEVER `MarkupString`.** Carries forward Story 2-4 D22 XSS invariant verbatim. The regression test in `FcLifecycleWrapperRejectionTests.cs` asserts `RejectionMessage = "<script>alert(1)</script>"` produces markup containing `&lt;script&gt;` (escaped). Same test applied to `IdempotentInfoMessage` parameter. Story 2-5 does NOT introduce any sanitizer library; if adopters need rich-formatted rejection copy, that is Epic 6 customization gradient territory with explicit opt-in + sanitizer. | XSS in a per-submission status surface is the highest-severity injection vector we can imagine — every tenant's rejection paths round-trip through this UI. Plain-text default + explicit opt-in (Epic 6) is the OWASP-correct posture. Tax: zero. |
| D15 | **Error aggregation (UX-DR39 ">2 errors in 5 s → aggregated bar") is deferred to Story 10-x.** v0.1 ships single-bar-per-rejection; stacking happens at the Fluent-UI-MessageBar default up to the stack-cap (max 3). The aggregation heuristic + "Show details" expansion UX are CI-gated polish work. | L07 test cost-benefit applied — aggregation requires a timer + aggregated-bar component + 8+ tests; value is low until adopters actually hit rapid-rejection scenarios. Defer with a specific owning story per L10. |
| D16 | **Bulk destructive confirmation ("Approve 12 orders?") is v2.** Epic 2 is per-command; Epic 4+5's DataGrid selection + batch operations introduce the bulk surface, at which point the destructive-dialog component is reused with a count prefix. v0.1 ships single-command confirmation only. | UX spec §2314 explicitly scopes bulk to v2. Epic 2 ships per-entity commands; bulk requires selection state + batch dispatcher work that doesn't exist in v0.1. |
| D17 | **`FcLifecycleWrapper` parameter surface append-only per 2-4 D1.** 2-5 ADDS ONE optional parameter: `[Parameter] public string? IdempotentInfoMessage { get; set; }` (null → default localized copy). Does NOT rename, remove, or retype `CorrelationId` / `ChildContent` / `RejectionMessage`. Tests assert no breaking snapshot diffs on the three existing parameters. | 2-4's append-only stability contract is load-bearing for 2-1 `CommandFormEmitter` snapshot regression — breaking it cascades through every emitted form. |
| D18 | **Tenant/user isolation (L03) is INHERITED, not enforced in 2-5 components.** `FcDestructiveConfirmationDialog`, `FcFormAbandonmentGuard`, and the wrapper's idempotent branch have no persisted state, no `IStorageService` writes, no tenant/user-keyed dictionary. Abandonment timer is a per-circuit component instance; the destructive dialog is modal + transient; the wrapper extensions remain circuit-scoped per 2-3 D13. No `IUserContextAccessor` / `ITenantContextAccessor` dependency added. | Same rationale as 2-3 D13 / 2-4 D13 — L03 applies to persistence services. 2-5 is pure UI wiring over existing circuit-scoped state. |
| D19 | **`CommandRendererEmitter` FullPage branch is the ONLY renderer path that wraps in `FcFormAbandonmentGuard`.** CompactInline (expand-in-row 2-4 field forms) already has Escape-to-collapse (Story 2-2 AC9) which is the appropriate "abandon" gesture — no abandonment bar. Inline (0-1 field button/popover) abandons by closing the popover or clicking away — no bar. 0-field dispatch commands bypass the form path entirely and have no state to protect. | Narrowest surface that implements UX-DR38 without stacking competing abandonment UX on top of Story 2-2's existing lightweight collapse affordances. Matches epics §1055 "full-page form" scope verbatim. |
| D20 | **`HFC1020` Warning fires on commands whose `TypeName` matches regex `^(Delete\|Remove\|Purge)[A-Z]\|^(Delete\|Remove\|Purge)Command$`** AND that LACK the `[Destructive]` attribute. Message: `"Command '{TypeName}' appears destructive by name but is missing [Destructive] attribute. Add [Destructive] or rename."` Adopter-facing fix: add the attribute (respecting intent) OR rename to a non-destructive verb (`Archive`, `Deactivate`, `Close` — which have distinct semantics). No analyzer escape-hatch in v0.1 (if adopter has a `RemoveFilterCommand` that is genuinely non-destructive, they rename to `ClearFilterCommand`; Epic 9's diagnostic suppression policy lands the `SuppressHFC1020` attribute if real demand emerges). | Name-heuristic analyzer is a guardrail, not a gate — adopter intent wins via explicit annotation. Build-warning-as-error (Story 1-7 `TreatWarningsAsErrors=true`) means this DOES block builds in adopter code — intentional: a mis-classified destructive command is a production-incident-class bug. |
| D21 | **Fluxor feature state's `RejectionReason` + `RejectionResolution` are verified present in Task 0 and — if missing — added via reducer projection in Task 5.2b as an ADDITIVE change** (no API surface widening, pure internal reducer wiring). The emitter generator `CommandFluxorFeatureEmitter` already reduces `RejectedAction(CorrelationId, Reason, Resolution)` into state; the generated record shape is auditable in `Counter.Domain`'s generated output. If the two string properties are present: no generator change required. If absent: add them to the emitted state type + reducer. | De-risks the critical path. Either state the emitter is in today is cheap to resolve; Task 0 verifies before any downstream work. |
| D22 | **Destructive dialog dismissal via ESC dispatches `OnCancel` (NOT `OnConfirm`).** Safest keyboard path — Enter on Cancel (auto-focused) also does the safe thing. No global keyboard shortcut registered for the dialog (UX-DR67 framework-shortcut owner is `IShortcutService`; dialog-local keybinds stay in the browser-native `<dialog>` semantics that `FluentDialog` exposes). | Cancel-auto-focus + ESC-to-cancel is the defense-in-depth pattern from UX spec §2349-2352. The user must actively cross the hazard to confirm. |
| D23 | **TestContext.Current.CancellationToken on all async bUnit tests.** xUnit1051 rule. Inherited convention across all prior stories. | Inherited. |

---

## Architecture Decision Records

### ADR-024: Destructive Confirmation Is Renderer-Level, Pre-Submit — Lifecycle Wrapper Remains Post-Submit Only

- **Status:** Accepted
- **Context:** Destructive-action confirmation dialog (epics §1042-1048, UX-DR37, UX-DR58) must intercept submit BEFORE the command dispatches. Three plausible ownership splits:
  1. **Wrapper-owned** — `FcLifecycleWrapper` intercepts its own `ChildContent`'s submit event.
  2. **Renderer-owned** — `CommandRendererEmitter` gates the submit trigger (the button's `OnClick` / `_externalSubmit` invocation) behind dialog confirmation.
  3. **Form-owned** — `CommandFormEmitter` inserts a dialog before `OnValidSubmitAsync`.
- **Decision:** Take option 2 (renderer-owned). `CommandRendererEmitter` emits the destructive-dialog open call, gates `_externalSubmit` on the dialog's `OnConfirm`, and leaves the form + wrapper untouched.
- **Consequences:** (+) `FcLifecycleWrapper`'s append-only parameter surface (2-4 D1) is preserved — no new intercept-hooks, no pre-submit logic. (+) Renderer is already the owner of submit-trigger chrome (button, popover, breadcrumb — ADR-016); extending it to "gate submit behind a dialog" is a natural expansion of that ownership. (+) Non-destructive commands emit zero confirmation plumbing — flat runtime cost for the 99 %. (-) Adds a renderer-level code path gated on `model.IsDestructive`; snapshot tests gain 3 new approved baselines (Task 8.6). (-) Inline (0-field) destructive commands CANNOT render — blocked at parse time via HFC1021 (D19).
- **Rejected alternatives:**
  - **Wrapper-owned (option 1)** — would require `FcLifecycleWrapper` to introspect submit intent (new intercept param + dialog callback). Breaks 2-4 D1 append-only contract (we'd need a NON-optional new parameter to distinguish destructive vs non-destructive).
  - **Form-owned (option 3)** — `CommandFormEmitter` already knows `NonDerivableProperties` but not destructive intent (it receives a `CommandFormModel`, not a `CommandRendererModel`). Widening the form model to carry destructive metadata couples two emitters that Story 2-2 ADR-016 deliberately decoupled.
  - **Shell-level middleware (cascade)** — service-locator hack, same anti-pattern Story 2-4 ADR-020 rejected for wrapper placement.

### ADR-025: `NavigationLock` + `LocationChanging` For Form Abandonment — NOT `IJSRuntime.beforeunload`

- **Status:** Accepted
- **Context:** Full-page form abandonment protection (epics §1050-1055, UX-DR38) can be implemented at two layers:
  1. **Browser-native `beforeunload`** via `IJSRuntime.InvokeAsync("eval", "window.onbeforeunload = ...")`.
  2. **Blazor 8+ `NavigationLock` + `LocationChanging` handler** — built-in component + event from `Microsoft.AspNetCore.Components.Routing`.
- **Decision:** Take option 2. `FcFormAbandonmentGuard` renders a `<NavigationLock OnBeforeInternalNavigation="OnNavigationChanging" />` and handles the `LocationChangingContext` to show the warning bar and call `ctx.PreventNavigation()` when the threshold has elapsed. On "Leave anyway" the guard sets `_isLeaving = true` and re-invokes `NavigationManager.NavigateTo(_pendingTarget)` which, because the lock is no longer active (or it checks the flag), completes the navigation.
- **Consequences:** (+) SSR-safe, no JS interop, no CSP/script-src policy wrinkle. (+) Works identically on Blazor Server, WebAssembly, Auto. (+) Copy is framework-controlled (UX-DR38 + UX-DR57 zero-override invariants satisfied). (+) Testable with bUnit's `TestContext.Services.AddSingleton<NavigationManager>` and simulated `LocationChangingContext`. (-) Only intercepts INTERNAL navigation (SPA nav). External `window.close` / browser-back IS NOT blocked (browser owns that). Acceptable: epics §1051 scopes "breadcrumb, sidebar, command palette" — all internal. Browser-close is out of framework control. (-) Requires `FcFormAbandonmentGuard` to coordinate with `IDisposable` on Blazor circuit teardown to release the lock.
- **Rejected alternatives:**
  - **beforeunload (option 1)** — browser-native dialog is non-customizable ("Leave site? Changes you made may not be saved"), violates UX-DR38 (must show framework-controlled copy with "Stay on form" / "Leave anyway"). Also requires JS interop which is not available during pre-render (SSR phase).
  - **Custom JS + CSP-safe inline handler** — moves copy-control to JS, loses localization parity with other framework UI, and introduces a JS module surface that Epic 6 customization gradient would have to overlay.

### ADR-026: Destructive Is Opt-In Via `[Destructive]` Attribute — NOT Name Heuristic

- **Status:** Accepted
- **Context:** epics §1042 says commands are "annotated OR identified as destructive (Delete, Remove, Purge)". Two interpretation axes:
  1. **Name heuristic** — `DeleteOrderCommand` is destructive because its name starts with `Delete`.
  2. **Attribute opt-in** — only `[Destructive]`-annotated commands trigger confirmation, regardless of name.
  3. **Hybrid** — name heuristic flags + opt-in required via attribute; analyzer warning bridges them.
- **Decision:** Take option 3 (hybrid). `[Destructive]` attribute is the ONLY runtime-honored classification signal. Name heuristic fires an analyzer warning (`HFC1020`) at build time on `Delete*`/`Remove*`/`Purge*` commands missing the attribute — a guardrail, not a gate.
- **Consequences:** (+) Explicit adopter intent (no false-positive `RemoveOutlierCommand` false-confirmation noise). (+) Guardrail catches the common miss (developer adds `DeletePartyCommand` and forgets the attribute). (+) Under `TreatWarningsAsErrors` (Story 1-7), the warning IS effectively blocking — adopter must consciously annotate or rename. (-) Two-step authoring (attribute + class name) for the happy path. Mitigation: `[Destructive]` is a one-liner above the record declaration. (-) Analyzer consumes 2 diagnostic IDs (HFC1020 Warning + HFC1021 Error).
- **Rejected alternatives:**
  - **Name-only (option 1)** — false-positive risk is non-trivial (`RemoveFilterCommand` is non-destructive). Adding a name-heuristic escape hatch (`[NotDestructive]` attribute) inverts the opt-in/opt-out semantics and reads badly.
  - **Attribute-only, no heuristic (option 2)** — common dev mistake (forgetting the attribute on a clearly-destructive command) ships as a production bug with no compile-time signal. Unacceptable for a framework that invests heavily in build-time analyzers (Story 9 is the Diagnostic ID System).
  - **Runtime command-name inspection** — reflection in hot path, AOT-hostile, L05 violation.

---

## Acceptance Criteria

### AC1: Rejection → domain-specific message, no auto-dismiss, form input preserved

**Given** a command form wrapped in `FcLifecycleWrapper` via `CommandFormEmitter` (Story 2-4 ADR-020)
**And** the adopter's `ICommandService` throws `CommandRejectedException(reason: "Approval failed: insufficient inventory", resolution: "The order has been returned to Pending.")`
**When** the generated form's `OnValidSubmitAsync` catches the exception and dispatches `RejectedAction(correlationId, reason, resolution)`
**Then** the wrapper renders a `FluentMessageBar` (Intent=Error) with:
  - Title: localized "Submission rejected"
  - Body: `"Approval failed: insufficient inventory. The order has been returned to Pending."` (period-space joined)
**And** the MessageBar has NO auto-dismiss (2-4 D17 regression — user dismisses via close button)
**And** `aria-live="assertive"` announces the rejection (2-4 AC7)
**And** the form's `_model` fields are UNCHANGED (user's input preserved — D5 regression test)
**And** the submit button re-enables for retry (2-4 AC7)
**And** `RejectionMessage` renders as plain text — `<script>` in Reason/Resolution is HTML-encoded (D14 XSS)

References: FR30, UX-DR46, NFR46, NFR47, NFR103, Decision **D4, D5, D14, D17**, Story 2-4 D22

### AC2: Idempotent outcome → Info MessageBar with auto-dismiss

**Given** the wrapper observes a `Confirmed` transition with `IdempotencyResolved == true`
**When** the transition arrives at any lifecycle phase (NoPulse/Pulse/StillSyncing/ActionPrompt)
**Then** a `FluentMessageBar` (Intent=Info) is rendered with:
  - Title: localized "Already confirmed"
  - Body: `IdempotentInfoMessage` parameter if non-null, else localized "This was already confirmed — no action needed." (D7 — safe under both cross-user and self-reconnect replay contexts)
**And** the bar auto-dismisses after `FcShellOptions.IdempotentInfoToastDurationMs` (default 3000 ms) from the Confirmed `LastTransitionAt`
**And** `aria-live="polite"` announces the outcome
**And** `HFC2104` is logged at Information level with hashed correlation ID
**And** the Success `FluentMessageBar` (non-idempotent Confirmed path, 2-4 AC6) is NOT rendered concurrently — the Info bar replaces it for the idempotent case
**And** `IdempotentInfoMessage` renders as plain text (D14 XSS)

References: FR30, UX-DR46, Decision **D3, D6, D7, D14, D17**, Story 2-4 Known Gap G2, Story 5-4 (future replay-source disambiguation)

### AC3: Destructive command → pre-submit confirmation dialog

**Given** a `[Destructive]` command class (e.g., `[Destructive] public sealed record DeleteOrderCommand(Guid OrderId) { }`)
**When** the generated renderer emits the submit trigger
**Then** clicking the trigger opens an `FcDestructiveConfirmationDialog` (via `IDialogService.ShowDialogAsync`)
**And** the dialog renders with:
  - Title: `ConfirmationTitle` if set, else `$"{DisplayLabel}?"` (e.g., "Delete Order?")
  - Body: `ConfirmationBody` if set, else localized "This action cannot be undone."
  - Cancel button: `Appearance=Secondary`, `data-autofocus="true"`, programmatically focused in `OnAfterRenderAsync`
  - Destructive action button: `Appearance=Accent` with Fluent UI v5 danger color slot, label = `DisplayLabel` (domain-language, e.g., "Delete Order")
**And** pressing Escape dispatches `OnCancel` (D22)
**And** `Enter` on the auto-focused Cancel button does NOT fire `OnConfirm`
**And** dismissal without confirmation ABANDONS the submit (form `_model` unchanged, lifecycle never transitions from Idle)
**And** confirmation invokes the form's `_externalSubmit` (Story 2-2 ADR-016) — lifecycle proceeds normally from there

References: FR30, UX-DR36, UX-DR37, UX-DR58, Decision **D1, D2, D11, D12, D22**, ADR-024

### AC4: Destructive commands never render as `Inline` (0-field button)

**Given** a command class annotated `[Destructive]` with zero non-derivable properties (would classify as `CommandDensity.Inline`)
**When** the source generator runs on the project
**Then** diagnostic `HFC1021` is emitted at Error severity against the command class declaration
**And** the message reads `"Destructive command '{TypeName}' must have at least one non-derivable property (destructive commands cannot render as inline buttons)."`
**And** the project build FAILS
**And** no renderer `.g.cs` is emitted for the offending command (or, if emitted, the destructive dialog is rendered in CompactInline minimum — verify parse-stage halt is cleanest — Task 2.2 decides)

References: UX-DR36 ("Danger never inline on DataGrid rows"), epics §1048, Decision **D1, D20**, ADR-026

### AC5: Non-destructive commands show NO confirmation dialog

**Given** a non-destructive command (e.g., `ApproveCommand`, `CreateOrderCommand`, `UpdateProfileCommand` — no `[Destructive]`)
**When** the user submits
**Then** no `FcDestructiveConfirmationDialog` opens
**And** the lifecycle wrapper provides feedback per Story 2-4 AC1-9
**And** the emitted renderer's rendered markup contains no `FcDestructiveConfirmationDialog` reference (snapshot tests verify — Task 8.6)

References: UX-DR58, epics §1057-1059, Decision **D2**, Story 2-4 AC1

### AC6: Full-page form active >30 s (first-edit anchored) → abandonment protection

**Given** a `CommandRenderMode.FullPage` generated renderer (command with 5+ non-derivable fields)
**And** `FcShellOptions.FormAbandonmentThresholdSeconds = 30` (default)
**When** the user mounts the form and types in ANY field
**Then** `FcFormAbandonmentGuard` starts its timer anchored on `EditContext.OnFieldChanged` first-fire (D10)
**When** the user then clicks breadcrumb / sidebar / command-palette navigation after ≥ 30 s elapsed
**Then** `NavigationLock` fires `LocationChanging`, the guard calls `ctx.PreventNavigation()`, and a `FluentMessageBar` (Intent=Warning) renders at the top of the form:
  - Body: "You have unsaved input."
  - Action buttons: "Stay on form" (Primary, auto-focused) + "Leave anyway" (Secondary)
**And** Escape on the warning bar triggers "Stay on form" (D9)
**And** clicking "Leave anyway" removes the lock and re-invokes `NavigationManager.NavigateTo(pendingTarget)`
**And** `CompactInline` + `Inline` renderers do NOT render the guard (snapshot tests verify — Task 8.8, D19)
**And** below 30 s (first edit to nav-attempt interval) the guard does NOT intercept
**And** if the user mounts the form and never edits, no interception occurs regardless of mount-duration
**And** the warning is SUPPRESSED when `ILifecycleStateService.GetState(correlationId)` returns Submitting/Syncing — logged `HFC2103` Warning (D13)

References: FR30, UX-DR38, UX-DR58, epics §1050-1055, UX spec §2318-2331, Decision **D6, D8, D9, D10, D13, D19**, ADR-025

### AC7: Button hierarchy enforced in emitted renderers

**Given** any command rendered via `CommandRendererEmitter`
**When** the renderer emits its button chrome
**Then** the submit button appearance is `ButtonAppearance.Accent` (Primary slot) in CompactInline + FullPage modes, `ButtonAppearance.Outline` in Inline 0-field, following UX spec §2221-2226
**And** all button labels are humanized via Story 2-1's `ResolveLabel` chain (`[Display(Name)]` → IStringLocalizer → humanized CamelCase → raw) — NEVER hardcoded "OK"/"Submit"
**And** Cancel buttons inside popover / dialog use `ButtonAppearance.Neutral` (Secondary)
**And** Destructive action buttons inside `FcDestructiveConfirmationDialog` use `ButtonAppearance.Accent + Color=Error` (Danger slot) with domain-language label (e.g., "Delete Order")
**And** Primary buttons + DataGrid row actions include `IconStart` parameter bound to `ResolveIcon()` — other button appearances omit it unless shell-header (out of 2-5 scope)
**And** the destructive dialog's Cancel button uses `data-autofocus` so Enter does the safe thing (D11)

References: FR30, UX-DR36, epics §1061-1068, Decision **D11, D12**

### AC8: Build-time analyzer warning on destructive-pattern-name commands missing `[Destructive]`

**Given** a command class named `DeleteOrderCommand`, `RemoveCartItemCommand`, or `PurgeLogsCommand` (matches `^(Delete|Remove|Purge)[A-Z]` or `^(Delete|Remove|Purge)Command$`)
**And** the class does NOT have `[Destructive]` applied
**When** the source generator's parse stage runs
**Then** `HFC1020` diagnostic is emitted at Warning severity against the class declaration
**And** the message reads `"Command '{TypeName}' appears destructive by name but is missing [Destructive] attribute. Add [Destructive] or rename the command."`
**And** under Story 1-7's `TreatWarningsAsErrors=true` the build FAILS — adopter must add the attribute or rename
**And** `AnalyzerReleases.Unshipped.md` has the HFC1020 Warning + HFC1021 Error entries registered

References: UX-DR36, UX-DR37, NFR47, Decision **D1, D20**, ADR-026, architecture.md §648 (diagnostic policy)

### AC9: Error recovery supports retry and input modification without re-typing

**Given** a rejected command where the rejection message explains failure cause + data state
**When** the user reads the Danger MessageBar
**Then** the submit button is re-enabled (2-4 AC7 regression preserved)
**And** the form's field values are preserved (D5 — `_model` not reset)
**And** the user can modify one field and re-submit without losing the others
**And** the recovery path requires zero external documentation — the MessageBar copy guides next action (NFR103)
**And** a re-submit generates a new `CorrelationId` via the `ResetToIdleAction` path (Story 2-3 patch P-11) so the rejected bar doesn't linger past the new submission

References: FR30, UX-DR46, NFR46, NFR47, NFR103, Decision **D4, D5**, Story 2-3 patch P-11

---

## Tasks / Subtasks

> Checkboxes are intentionally unchecked. Dev agent marks them `[x]` as each lands. Numbers align to the AC quick index.

### Task 0 — Prereq verification (≤ 15 min)

- [ ] 0.1: Verify the emitted `{Command}LifecycleFeatureState` record (produced by `CommandFluxorFeatureEmitter`) exposes `RejectionReason` + `RejectionResolution` as `string?` properties populated by the `Rejected` action reducer. Inspect `samples/Counter/Counter.Domain/generated/*LifecycleFeatureState.g.cs` and `CommandFluxorFeatureEmitter.cs`. If present → mark 5.2b skipped. If ABSENT → retain 5.2b as an in-scope emitter extension (additive reducer projection).
- [ ] 0.2: Verify `IDialogService` is registered via `services.AddFluentUIComponents()` in `ServiceCollectionExtensions.cs` (Story 1-8 should have wired this). If absent, this is a one-line add in Task 3.2.
- [ ] 0.3: Verify `NavigationLock` is available in `Microsoft.AspNetCore.Components.Routing` for the pinned .NET 10 SDK + Blazor version. If not (e.g., Blazor Server downgrade), adjust Task 4.1 approach to `INavigationInterception` fallback.
- [ ] 0.4: Confirm `Microsoft.Extensions.TimeProvider.Testing` is already referenced in `Hexalith.FrontComposer.Shell.Tests.csproj` (Story 2-4 added this). If absent (accidentally rolled back), add per Story 2-4 Task 0.3.

### Task 1 — Contracts additions

- [ ] 1.1: Create `src/Hexalith.FrontComposer.Contracts/Attributes/DestructiveAttribute.cs`. Signature:
  ```csharp
  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
  public sealed class DestructiveAttribute : Attribute {
      public string? ConfirmationTitle { get; init; }
      public string? ConfirmationBody { get; init; }
  }
  ```
  XMLdocs cite D1, UX-DR37, UX-DR58.
- [ ] 1.2: Extend `FcShellOptions` with two new properties per D6:
  - `FormAbandonmentThresholdSeconds` (int, default 30, `[Range(5, 600)]`, unit=seconds per D6)
  - `IdempotentInfoToastDurationMs` (int, default 3000, `[Range(1_000, 30_000)]`)
- [ ] 1.3: Extend `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` with:
  - `public const string HFC1020_DestructiveNamePatternMissingAttribute = "HFC1020";`
  - `public const string HFC1021_DestructiveCommandHasZeroFields = "HFC1021";`
  - `public const string HFC2103_AbandonmentDuringSubmitting = "HFC2103";`
  - `public const string HFC2104_IdempotentInfoBarRendered = "HFC2104";`
- [ ] 1.3b: Add HFC1020 (Warning) and HFC1021 (Error) entries to `AnalyzerReleases.Unshipped.md`. Add descriptors to wherever HFC10xx descriptors live in `SourceTools/Diagnostics/` (inspect existing HFC1002, HFC1013 for format precedent).
- [ ] 1.4: Extend `FcShellOptionsThresholdValidator` (lives in `Shell/Options/` per 2-4 Task 3.2) with D6's two cross-property invariants:
  - `opts.FormAbandonmentThresholdSeconds * 1000 > opts.StillSyncingThresholdMs`
  - `opts.IdempotentInfoToastDurationMs <= opts.ConfirmedToastDurationMs`

### Task 2 — Parser + analyzer

- [ ] 2.1: Extend `CommandModel` in `DomainModel.cs` with three new properties:
  - `bool IsDestructive`
  - `string? DestructiveConfirmTitle`
  - `string? DestructiveConfirmBody`
  Update constructor, `Equals`, `GetHashCode`. Propagate through `CommandRendererTransform` (Task 2.3).
- [ ] 2.2: Extend `CommandParser.cs` parse stage:
  - Detect `DestructiveAttributeName = "Hexalith.FrontComposer.Contracts.Attributes.DestructiveAttribute"`; if present, capture `ConfirmationTitle`/`ConfirmationBody` named args.
  - Name-pattern regex: `^(Delete|Remove|Purge)(?:[A-Z]|Command$)`. If the pattern matches AND `[Destructive]` absent → emit HFC1020 Warning.
  - If `IsDestructive && NonDerivableProperties.Count == 0` → emit HFC1021 Error AND SKIP renderer emission for this command (return early from the transform pipeline for this IR entry so no invalid Inline-destructive `.g.cs` is emitted).
  - All diagnostics use `filePath` + `linePos` from the type symbol's declaration syntax per existing pattern.
- [ ] 2.3: Extend `CommandRendererModel` + `CommandRendererTransform` to pipe `IsDestructive` / `DestructiveConfirmTitle` / `DestructiveConfirmBody` from `CommandModel` → `CommandRendererModel`. Update `Equals`/`GetHashCode` on `CommandRendererModel`.

### Task 3 — FcDestructiveConfirmationDialog component

- [ ] 3.1: Create `src/Hexalith.FrontComposer.Shell/Components/Forms/FcDestructiveConfirmationDialog.razor` + `.razor.cs` + `.razor.css` per D11, D12, AC3, D22. Parameters: `Title`, `Body`, `DestructiveLabel`, `OnConfirm` (EventCallback), `OnCancel` (EventCallback). Auto-focus Cancel via `@ref + OnAfterRenderAsync → FocusAsync()`. Handle `OnKeyDown` on dialog content for Escape → `OnCancel`. Use `FluentDialog` v5 (verify via `mcp__fluent-ui-blazor__get_component_details` if unsure of v5 param shape; parameters to look for: `Modal`, `TrapFocus`, `OnDismiss`).
- [ ] 3.2: Register via `IDialogService` (already registered per Task 0.2). Dialog is shown via `DialogService.ShowDialogAsync<FcDestructiveConfirmationDialog>(new DialogParameters<FcDestructiveConfirmationParams>(...))` — document the calling pattern that `CommandRendererEmitter` will emit (Task 6.1).

### Task 4 — FcFormAbandonmentGuard component

- [ ] 4.1: Create `src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor` + `.razor.cs` + `.razor.css` per D8, D9, D10, D13, AC6.
  - Parameters: `EditContext? EditContext` (required), `string CorrelationId` (for D13 state query), `RenderFragment? ChildContent`.
  - Inject `ILifecycleStateService`, `IOptionsMonitor<FcShellOptions>`, `NavigationManager`, `ILogger<FcFormAbandonmentGuard>`, `TimeProvider`.
  - On `OnParametersSet`: if EditContext non-null and first-time, subscribe `EditContext.OnFieldChanged += OnFirstEdit` where `OnFirstEdit` sets `_firstEditAt = Time.GetUtcNow()` and immediately unsubscribes.
  - Render: `<NavigationLock OnBeforeInternalNavigation="HandleNavigationChanging" />` first, then warning bar (conditional on `_showingWarning`), then `@ChildContent`.
  - `HandleNavigationChanging(LocationChangingContext ctx)`:
    1. If `_firstEditAt is null` → return (no edit yet, no protection).
    2. If `(Time.GetUtcNow() - _firstEditAt.Value).TotalSeconds < opts.FormAbandonmentThresholdSeconds` → return.
    3. Query lifecycle state: if Submitting/Syncing → log HFC2103 and return (D13).
    4. Set `_pendingTarget = ctx.TargetLocation`, `_showingWarning = true`, call `ctx.PreventNavigation()`, `StateHasChanged`.
  - "Stay on form" button: `_showingWarning = false` + `StateHasChanged` + focus-return to the form's first field.
  - "Leave anyway" button: `_showingWarning = false` + `NavigationManager.NavigateTo(_pendingTarget)` — the lock, on the next call, will see `_isLeaving = true` and bypass the intercept (or we can dispose the lock transiently; pick the pattern that tests cleanest in bUnit).
  - Dispose: unsubscribe from EditContext, release the lock.
- [ ] 4.2: Warning MessageBar markup: `<FluentMessageBar Intent="MessageBarIntent.Warning" AllowDismiss="false">` with body + `<ActionsTemplate>` containing both buttons. "Stay on form" has `@ref + OnAfterRenderAsync → FocusAsync()` auto-focus, Appearance=Accent (Primary). "Leave anyway" Appearance=Neutral (Secondary). Handle `OnKeyDown` on the bar for Escape → Stay (D9).

### Task 5 — FcLifecycleWrapper extension

- [ ] 5.1: Extend `LifecycleUiState.cs` — add `bool IsIdempotent` field + optional `DateTimeOffset? IdempotentDismissAt`. Update `LifecycleUiState.From(transition, phase, rejectionMessage)` to set `IsIdempotent = transition.IdempotencyResolved && transition.NewState == CommandLifecycleState.Confirmed`. Update `FcLifecycleWrapper.razor.cs` `ApplyTransition` method: when `transition.NewState == Confirmed && transition.IdempotencyResolved`, call a new `ScheduleIdempotentDismiss()` (symmetric to `ScheduleConfirmedDismiss`) using `IdempotentInfoToastDurationMs`, and set `next.IsIdempotent = true`.
  - Add `[Parameter] public string? IdempotentInfoMessage { get; set; }` (D17 — ONE new optional parameter; verify by snapshot diff no existing parameter changes).
  - Update `FcLifecycleWrapper.razor` to branch: when `_state.Current is Confirmed && _state.IsIdempotent` → render `FluentMessageBar` (Intent=Info) with body `IdempotentInfoMessage ?? LocalizedIdempotentCopy` — DO NOT ALSO render the Success MessageBar in this case.
  - Log `HFC2104` Information with hashed CorrelationId in the idempotent branch.
- [ ] 5.2: Extend `CommandFormEmitter.cs` to pass a runtime-computed `RejectionMessage` attribute into `<FcLifecycleWrapper>`:
  - Around L366 (after `builder.AddAttribute(seq++, "CorrelationId", _submittedCorrelationId);`) add:
    ```csharp
    _ = sb.AppendLine("        builder.AddAttribute(seq++, \"RejectionMessage\", BuildFcLifecycleRejectionCopy());");
    ```
  - Emit a helper method:
    ```csharp
    _ = sb.AppendLine("    private string? BuildFcLifecycleRejectionCopy()");
    _ = sb.AppendLine("    {");
    _ = sb.AppendLine("        if (LifecycleState.Value.State != CommandLifecycleState.Rejected) return null;");
    _ = sb.AppendLine("        var reason = LifecycleState.Value.RejectionReason;");
    _ = sb.AppendLine("        var resolution = LifecycleState.Value.RejectionResolution;");
    _ = sb.AppendLine("        if (string.IsNullOrWhiteSpace(reason) && string.IsNullOrWhiteSpace(resolution)) return null;");
    _ = sb.AppendLine("        if (string.IsNullOrWhiteSpace(resolution)) return reason;");
    _ = sb.AppendLine("        if (string.IsNullOrWhiteSpace(reason)) return resolution;");
    _ = sb.AppendLine("        return reason + \". \" + resolution;");
    _ = sb.AppendLine("    }");
    ```
  - Plain-text join; wrapper-side rendering stays plain-text (D14).
- [ ] 5.2b: *(CONDITIONAL on Task 0.1 finding)* if `{Command}LifecycleFeatureState` is missing `RejectionReason`/`RejectionResolution`, extend `CommandFluxorFeatureEmitter.cs` to project them from `RejectedAction` into state. Re-approve affected snapshots.

### Task 6 — Renderer integration

- [ ] 6.1: Extend `CommandRendererEmitter.cs`:
  - Add `using Hexalith.FrontComposer.Shell.Components.Forms;` to the emitted using-block (symmetrical to Story 2-4 D21).
  - For EVERY branch (Inline, CompactInline, FullPage), if `model.IsDestructive`: wrap the submit-trigger's `OnClick` in a dialog-open path:
    ```csharp
    // Pseudocode — actual emission is inside the RenderTreeBuilder stream
    OnClick = async _ => {
        var parameters = new DialogParameters { Title = "...", Body = "...", DestructiveLabel = "..." };
        var dialogRef = await DialogService.ShowDialogAsync<FcDestructiveConfirmationDialog>(parameters);
        var result = await dialogRef.Result;
        if (!result.Cancelled && _externalSubmit is not null) _externalSubmit();
    };
    ```
    Inject `IDialogService` in the renderer partial class (add `[Inject]` line in the emitted header).
  - FullPage branch ONLY (D19): wrap the `<CommandForm>` invocation in `<FcFormAbandonmentGuard CorrelationId="@_submittedCorrelationId" EditContext="@_formEditContext">`. Requires exposing the form's `EditContext` to the renderer — add a new `[Parameter] public EditContext? ExposedEditContext { get; set; }` on the form + `EventCallback<EditContext> OnEditContextReady` that the renderer consumes. *(If exposing the EditContext proves intrusive, fall back to letting the guard subscribe to `NavigationManager.LocationChanging` without field-edit tracking and use mount-time as anchor — accept reduced precision; file as a Known Gap.)*
  - Non-destructive branches emit identical markup to Story 2-2's output — verify via snapshot diff.
- [ ] 6.2: Add `@using Hexalith.FrontComposer.Shell.Components.Forms` to `src/Hexalith.FrontComposer.Shell/_Imports.razor` — harmless since `_Imports.razor` scopes Razor files inside Shell, and the dialog/guard live in Shell.

### Task 7 — Counter sample integration (optional destructive demo, mandatory config)

- [ ] 7.1: Extend `samples/Counter/Counter.Web/appsettings.Development.json`:
  ```json
  "Hexalith": { "Shell": {
    "SyncPulseThresholdMs": 300,
    "StillSyncingThresholdMs": 2000,
    "TimeoutActionThresholdMs": 10000,
    "ConfirmedToastDurationMs": 5000,
    "FormAbandonmentThresholdSeconds": 30,
    "IdempotentInfoToastDurationMs": 3000
  } }
  ```
- [ ] 7.2: *(OPTIONAL, not blocker)* Add `[Destructive] public sealed record ResetCountCommand(Guid CounterId) : ICommand { }` to `samples/Counter/Counter.Domain` for manual verification of the destructive dialog. Increment demonstrates non-destructive + lifecycle; Reset demonstrates destructive + dialog. If authoring time is tight, skip — the dialog component is tested in bUnit without needing a sample command.

### Task 8 — Tests

- [ ] 8.1: `FcDestructiveConfirmationDialogTests.cs` (6 bUnit tests per AC3 / D11 / D12 / D22). Verify title/body rendering, Cancel auto-focused, ESC dispatches OnCancel, destructive button appearance + label, Enter on Cancel does NOT fire OnConfirm.
- [ ] 8.2: `FcFormAbandonmentGuardTests.cs` (7 bUnit tests with `FakeTimeProvider` per AC6 / D8-10 / D13). Verify below-threshold passes through, above-threshold blocks, first-edit anchors timer, Stay cancels nav, Leave proceeds, Submitting state suppresses warning, Stay auto-focused.
- [ ] 8.3: `FcLifecycleWrapperIdempotentTests.cs` (5 bUnit tests per AC2 / D3 / D6 / D7 / D14).
- [ ] 8.4: `FcLifecycleWrapperRejectionTests.cs` (5 bUnit tests per AC1 / D4 / D5 / D14 / D17). Include XSS regression (`<script>` in Reason → encoded); include 2-4 D17 no-auto-dismiss regression.
- [ ] 8.5: `CommandParserDestructiveTests.cs` (6 parser tests per AC4 / AC8 / D1 / D20). Include the analyzer release registration assertion.
- [ ] 8.6: Snapshot regen:
  - 2 `CommandFormEmitter.*.verified.txt` re-approvals (wrapper now receives `RejectionMessage` attribute + helper method emitted — inspect diffs; they should be LOCAL to the wrapper attribute line + the new helper method body).
  - 3 new `CommandRendererEmitter.Destructive_*.verified.txt`: destructive CompactInline, destructive FullPage, non-destructive FullPage (baseline).
- [ ] 8.7: Extend `FcShellOptionsValidationTests.cs` — 2 new tests for D6's two cross-property invariants + range validation on the two new int properties.
- [ ] 8.8: Modify `CommandRendererCompactInlineTests.cs` + `CommandRendererFullPageTests.cs` to:
  - Assert non-destructive path does NOT reference `FcDestructiveConfirmationDialog` in generated markup.
  - Assert FullPage path DOES render `FcFormAbandonmentGuard` wrapper.
- [ ] 8.9: Add `CommandFormEmitterInputPreservationTests.cs` (1 regression test per D5): scan emitted code for the rejection `catch` block and assert NO `_model =` assignment appears after the `Dispatcher.Dispatch(new RejectedAction(...))` line.

### Task 9 — Regression + zero-warning gate

- [ ] 9.1: `dotnet build` — expect zero warnings under `TreatWarningsAsErrors=true`.
- [ ] 9.2: `dotnet test --no-build` — expect all Shell.Tests + SourceTools.Tests green, ~36 new tests, cumulative ~542.
- [ ] 9.3: Verify `AnalyzerReleases.Unshipped.md` has HFC1020 + HFC1021 rows; no unreleased descriptors.
- [ ] 9.4: Manually launch Counter sample (`aspnet run --project samples/Counter/Counter.AppHost`), navigate to an Increment command in the UI, submit, verify happy-path lifecycle still works (regression). If Task 7.2's ResetCommand was added, verify the dialog renders. If not, skip this step.

---

## Known Gaps (Explicit, Not Bugs)

Per lesson **L10** (deferrals name a story, not an epic), every gap below has an owning story number.

| # | Gap | Why deferred | Owning story |
|---|---|---|---|
| G1 | **Error aggregation** — UX-DR39 specifies `>2 error bars in 5 s → "N commands failed. [Show details]"` aggregation. v0.1 ships single-bar-per-rejection; aggregation is CI-gated polish. | L07 test cost-benefit (8+ tests, timer, aggregated component) doesn't pay off until adopters hit rapid-rejection in production. | **Story 10-x** (Epic 10 UX polish / or a new 10-7 if needed) |
| G2 | **MessageBar stacking cap (max 3 visible)** — UX spec §2372 mandates oldest-auto-dismissible-removed when 4th arrives. v0.1 relies on FluentMessageBar's default stacking (no cap enforcement at framework level). | Same — rapid-stack scenarios are rare in the v0.1 Counter sample and early adopter projects. | **Story 10-x** |
| G3 | **Bulk destructive confirmation ("Approve 12 orders?")** — UX spec §2314 explicitly v2. Requires DataGrid multi-select + batch dispatcher that doesn't exist in v0.1. | v2 feature. The `FcDestructiveConfirmationDialog` is designed to be reused with a count-prefix string at that time — no v2 design risk. | **v2** |
| G4 | **Form abandonment on CompactInline expand-in-row** — only FullPage is guarded in v0.1 (D19). CompactInline has Escape-to-collapse (Story 2-2 AC9) as its abandon gesture. | epics §1055 explicitly scopes abandonment to full-page forms; extending to CompactInline would add guard instances on every DataGrid row, amplifying NavigationLock load under scroll. | **v2 / Epic 4** (if DataGrid-row-level dirty-state becomes a common UX need) |
| G5 | **Cross-user vs self-reconnect idempotent disambiguation** — Story 2-4 G2 + H-2 warned that "already approved by another user" under self-reconnect replay is wrong. 2-5's copy is deliberately safe under both (D7); 5-4 may later refine. | Safe copy for v0.1 is the minimum intersection. 5-4 may extend `CommandLifecycleTransition` with a `ReplaySource` enum (CrossUser / SelfReconnect / Duplicate) and adopt per-source copy at that time. | **Story 5-4** (reconnection reconciliation) |
| G6 | **Visual regression baselines** for destructive dialog + abandonment bar (per-theme × per-density) | A11y + visual-regression CI infrastructure lands in Story 10-2. | **Story 10-2** (Accessibility CI gates & visual specimen verification) |
| G7 | **Custom destructive dialog content-slot override** — adopters might want a typed data-preview inside the dialog body (e.g., a table of the items about to be deleted). v0.1 accepts only string `ConfirmationBody`. | Epic 6 customization gradient (Level 3 slot) is the right surface for rich adopter content — outside Epic 2 scope. | **Story 6-3** (Level 3 slot-level field replacement) |
| G8 | **`[Destructive]` analyzer escape hatch (`SuppressHFC1020`)** — if adopter legitimately has a `RemoveFilterCommand` that is non-destructive, v0.1's only recourse is rename. Epic 9 adds diagnostic suppression. | L07 — real demand hasn't materialized; renaming is the cleaner fix in early v0.1 adoption. | **Story 9-4** (Diagnostic ID System and deprecation policy) |
| G9 | **Browser-close `beforeunload` protection** — `NavigationLock` only intercepts internal SPA nav. Closing the tab / typing a new URL bypasses it. | Browser-native `beforeunload` dialog is non-customizable (UX-DR38 + UX-DR57 violation). Accepting this gap is deliberate — the adopter's IT browser policies + user training cover the rare case. | **Out-of-scope** — not fixable without JS interop and opinionated adopter config |
| G10 | **Locale-aware period rendering (e.g., Chinese full-stop "。")** for rejection copy join | v0.1 supports English + French, which share the `.` glyph. Adding locale-aware terminal-punctuation-aware join requires `CultureInfo.NumberFormat.NumberDecimalSeparator` equivalent for sentence terminators (which doesn't exist in BCL) or a lookup table. | **Story 9-5** (Diataxis documentation site — documents localization nuances) |
| G11 | **Test coverage for FcDestructiveConfirmationDialog under `IDialogService.ShowDialogAsync` vs. direct component render** — bUnit can render the dialog directly but doesn't exercise the real `IDialogService` portal path fully. | Tier 1 visual-regression + integration-test coverage of `IDialogService` lands in Story 10-2 + 10-1 (adopter test host). 2-5 ships component-level bUnit coverage. | **Story 10-2** + **Story 10-1** |
| G12 | **Destructive dialog width / responsive behaviour on <768 px (phone tier)** | Phone-tier DataGrid pattern (UX-DR62) is Story 3+ scope. FluentDialog has defaults that work acceptably but aren't calibrated per UX-DR62 tap-target rules. | **Story 3-x** (responsive + touch target work) |

---

## Dev Notes

### Service Binding Reference

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

### Rejection Copy Plumbing

Full data flow for rejection message rendering:

1. Adopter's `ICommandService` implementation throws `CommandRejectedException("Approval failed: insufficient inventory", "The order has been returned to Pending.")`.
2. Generated form's `OnValidSubmitAsync` catches: `Dispatcher.Dispatch(new {Command}Actions.RejectedAction(correlationId, ex.Message, ex.Resolution))` (existing code at `CommandFormEmitter.cs:314`).
3. Emitted Fluxor feature reducer projects `RejectedAction` into `{Command}LifecycleFeatureState { State = Rejected, CorrelationId, RejectionReason = Reason, RejectionResolution = Resolution }` (Task 0.1 verifies this path; Task 5.2b ensures it if absent).
4. Generated form's `BuildRenderTree` passes `RejectionMessage = BuildFcLifecycleRejectionCopy()` into `<FcLifecycleWrapper>` (Task 5.2). `BuildFcLifecycleRejectionCopy()` returns `$"{Reason}. {Resolution}"` (or the single non-empty clause, or null if both empty).
5. `ILifecycleStateService.Transition(correlationId, Rejected)` is called by the bridge — wrapper subscribes and receives the transition.
6. Wrapper's `ApplyTransition` case `Rejected`: sets `_state = _state with { Current = Rejected, RejectionMessage = RejectionMessage }` (parameter-supplied copy wins since it's recomputed on every render).
7. Razor template: `@if (_state.Current is CommandLifecycleState.Rejected) { <FluentMessageBar Intent=Error> @((string?)RejectionMessage ?? genericFallback) </FluentMessageBar> }`.
8. Blazor renders `RejectionMessage` as plain text — `<script>` tags from adversarial tenant payloads are HTML-encoded (2-4 D22 + 2-5 D14).

### Destructive Confirmation Flow

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

Emitted `CommandRendererEmitter` output for a destructive FullPage renderer (pseudocode illustrating the shape):

```csharp
private async Task OnSubmitClickAsync(MouseEventArgs _) {
    var parameters = new DialogParameters<FcDestructiveConfirmationParams> {
        Content = new FcDestructiveConfirmationParams {
            Title = {DestructiveConfirmTitle ?? $"{DisplayLabel}?"},
            Body = {DestructiveConfirmBody ?? "This action cannot be undone."},
            DestructiveLabel = "{DisplayLabel}"
        }
    };
    var dialogRef = await DialogService.ShowDialogAsync<FcDestructiveConfirmationDialog>(parameters);
    var result = await dialogRef.Result;
    if (!result.Cancelled && _externalSubmit is not null) {
        _externalSubmit();
    }
}
```

(The exact `IDialogService` v5 invocation pattern must be verified via `mcp__fluent-ui-blazor__get_component_details` — the snippet above is illustrative.)

### Abandonment Guard Lifecycle

- Mount: `OnInitialized` — subscribe to `EditContext.OnFieldChanged` only if `EditContext` non-null. Guard does NOT start a timer; timer is implicit via `TimeProvider.GetUtcNow() - _firstEditAt` computed on every `LocationChanging`.
- First edit: `_firstEditAt = Time.GetUtcNow()` + unsubscribe from OnFieldChanged (we only care about the FIRST edit).
- Navigation attempt: `HandleNavigationChanging(ctx)` executes through the decision tree in Task 4.1.
- Circuit teardown: `Dispose` — unsubscribe if still subscribed, release `NavigationLock` via component disposal (Blazor handles lock lifecycle automatically when `<NavigationLock>` leaves the render tree).

### Append-Only Parameter Surface Verification

Story 2-4 D1 append-only contract for `FcLifecycleWrapper`:
- `CorrelationId` (EditorRequired, string) — UNCHANGED.
- `ChildContent` (RenderFragment?) — UNCHANGED.
- `RejectionMessage` (string?) — UNCHANGED. 2-5 POPULATES this from the emitter (new wiring, same param).
- **NEW: `IdempotentInfoMessage` (string?, optional)** — append-only extension per D17.

Snapshot test `FcLifecycleWrapperParameterSurfaceTests.cs` asserts the five expected `[Parameter]` / `[EditorRequired]` annotations are present with exactly the expected names + types; a re-approved baseline prevents accidental removal or retyping during Story 2-5 or later.

### Fluent UI v5 Component Reference

Verify the following v5 components + parameters are used (no v4 naming regressions — Story 2-4 D8 precedent):
- `FluentDialog` — used via `IDialogService.ShowDialogAsync` (NOT inline `<FluentDialog>`).
- `FluentMessageBar` — `Intent`, `Title`, `AllowDismiss`, `ActionsTemplate` (rather than deprecated v4 parameters).
- `FluentButton` — `Appearance`, `Color`, `IconStart` (v5 names).
- `IDialogService.ShowDialogAsync<TDialog>` — verify parameter shape via `mcp__fluent-ui-blazor__get_component_details IDialogService` or `DialogService` at Task 0.2 if unsure.

`IToastService` is DEPRECATED in v5 (UX spec §492) — do NOT inject or reference. All surfaces in 2-5 use `FluentMessageBar` (inline) or `FluentDialog` (modal).

### Files Touched Summary

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

### Naming Convention Reference

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

### Testing Standards

- xUnit v3 (3.2.2), Verify.XunitV3, Shouldly, NSubstitute, bUnit 2.7.2 — inherited from 2-1/2-2/2-3/2-4.
- `FakeTimeProvider` via `Microsoft.Extensions.TimeProvider.Testing` — reuse Story 2-4's package.
- `TestContext.Current.CancellationToken` on async tests (xUnit1051).
- `TreatWarningsAsErrors=true` global.
- `DiffEngine_Disabled: true` in CI.
- **Test count budget (L07):** **~36 new tests** (6 dialog + 7 abandonment guard + 5 idempotent wrapper + 5 rejection wrapper + 1 parameter surface lockdown + 6 parser + 1 regression + 2 options + ~3 renderer assertion updates). Cumulative target **~542**. L07 cost-benefit: tight coverage of the net-new components (dialog + guard) and a single regression test per invariant being relied upon. 23 decisions / 36 tests ≈ 1.6/decision — tighter than 2-4's 2.0/decision because the lifecycle-wrapper extension reuses established infrastructure.

### Build & CI

- Build race CS2012: `dotnet build` then `dotnet test --no-build` (inherited pattern)
- `AnalyzerReleases.Unshipped.md` MUST include HFC1020 (Warning) + HFC1021 (Error) rows — otherwise build fails with RS2008
- Roslyn 4.12.0 pinned (inherited)
- `Microsoft.FluentUI.AspNetCore.Components` stays at `5.0.0-rc.2-26098.1` — do NOT bump in this story
- No new CI jobs — everything rides the existing `dotnet test` pass. E2E latency gate from 2-4's Task 6 remains pre-existing.

### Previous Story Intelligence

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

### Lessons Ledger Citations (from `_bmad-output/process-notes/story-creation-lessons.md`)

- **L01 Cross-story contract clarity** — Binding contracts with Stories 2-1 / 2-2 / 2-3 / 2-4 are explicitly enumerated in the cheat sheet. ADR-024/025/026 document the append-only + renderer-level ownership decisions.
- **L04 Generated name collision detection** — `FcDestructiveConfirmationDialog` + `FcFormAbandonmentGuard` are hand-written in a new `Components/Forms/` subfolder — zero collision risk with existing `Components/Lifecycle/` or `Components/Rendering/` files.
- **L05 Hand-written service + emitted per-type wiring** — Dialog + Guard are hand-written; emission of destructive-gating + abandonment-wrap per command is done by modifying `CommandRendererEmitter` (NOT by emitting per-command subclasses). Matches 2-4 L05 pattern.
- **L06 Defense-in-depth budget** — 23 Critical Decisions — under the ≤25 feature-story cap. No Occam trim required.
- **L07 Test cost-benefit** — 36 tests / 23 decisions ≈ 1.6/decision, tighter than 2-4's 2.0/decision. Lean surface because most wiring re-uses existing infrastructure (FakeTimeProvider, IDialogService, NavigationLock built-in).
- **L08 Party review vs. elicitation** — no party review + elicitation passes run on this story yet (drafted as initial specification; review rounds may surface additional decisions). Cheat sheet flagged the explicit scope guardrails to preempt common elicitation gaps (error aggregation, bulk, visual regression baselines — all deferred to named stories per L10).
- **L09 ADR rejected-alternatives discipline** — ADR-024 cites 3, ADR-025 cites 2, ADR-026 cites 3. All ≥2 satisfied. ADR-024 surfaced the wrapper-owned alternative that nearly won (would have compromised 2-4 D1 append-only contract).
- **L10 Deferrals name a story** — All 12 Known Gaps cite specific owning stories (Story 10-x, Story 5-4, Story 6-3, Story 9-4, Story 3-x, v2, Out-of-scope).
- **L11 Dev Agent Cheat Sheet** — Present. Feature story with three distinct UX surfaces (rejection, confirmation, abandonment) + cross-story bindings warrants fast-path entry despite being under the 30-decision threshold.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.5 — AC source of truth, §1021-1078]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-DR36 — button hierarchy, §349]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-DR37 — destructive confirmation dialog, §350]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-DR38 — form abandonment protection, §351]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-DR39 — notification patterns, §352]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-DR46 — domain-specific error messages + input preservation, §359]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-DR58 — confirmation pattern rules, §371]
- [Source: _bmad-output/planning-artifacts/prd.md#FR30 — exactly-one user-visible outcome, §1248]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR46 — domain-specific rejection messages, §149]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR47 — zero silent failures, §150]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR103 — technical, precise, concise, confident messages, §206]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Button hierarchy, §2217-2243]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Confirmation patterns, §2305-2352]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Form abandonment protection, §2318-2331]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Confirmation dialog pattern (destructive actions), §2333-2352]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Notification & MessageBar patterns, §2354-2376]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Journey 5 — form rejection + input preserved, §1548-1562]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Journey 6 — rejection + idempotent outcome, §1566-1632]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Error recovery preserves intent (principle 4), §1674]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Input preservation on rejection (principle), §1663]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#FluentMessageBar severity mapping, §2365-2370]
- [Source: _bmad-output/planning-artifacts/architecture.md#648 — HFC diagnostic ID ranges; 1xxx analyzer-emitted, 2xxx runtime-logged]
- [Source: _bmad-output/planning-artifacts/architecture.md#747 — Rejected action carries Reason + Resolution]
- [Source: _bmad-output/planning-artifacts/architecture.md#1144 — Contracts dependency-free (DestructiveAttribute + FcShellOptions extensions respect this)]
- [Source: _bmad-output/implementation-artifacts/2-4-fclifecyclewrapper-visual-lifecycle-feedback.md#Decision D1 — wrapper append-only parameter surface]
- [Source: _bmad-output/implementation-artifacts/2-4-fclifecyclewrapper-visual-lifecycle-feedback.md#Decision D17 — Rejected no-auto-dismiss]
- [Source: _bmad-output/implementation-artifacts/2-4-fclifecyclewrapper-visual-lifecycle-feedback.md#Decision D22 — XSS plain-text rendering invariant]
- [Source: _bmad-output/implementation-artifacts/2-4-fclifecyclewrapper-visual-lifecycle-feedback.md#Known Gap G2 + Hindsight H-2 — IdempotencyResolved cross-user-vs-self-reconnect disambiguation]
- [Source: _bmad-output/implementation-artifacts/2-4-fclifecyclewrapper-visual-lifecycle-feedback.md#Known Gap G11 — abandonment sequencing with ADR-022 page-reload]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management.md#Decision D19 — single-writer invariant]
- [Source: _bmad-output/implementation-artifacts/2-3-command-lifecycle-state-management.md#Patch P-11 — ResetToIdleAction CorrelationId plumbing]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes.md#ADR-016 — renderer/form split]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes.md#OnCollapseRequested + Story 2-5 NavigateAwayRequested TODO — CommandRendererEmitter.cs:69]
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

### Project Structure Notes

- **Alignment with architecture blueprint** (architecture.md §920-935, §648):
  - New `Shell/Components/Forms/` subfolder for `FcDestructiveConfirmationDialog` + `FcFormAbandonmentGuard` — consistent with existing `Shell/Components/Lifecycle/` and `Shell/Components/Rendering/` organization. The `Forms/` folder groups form-adjacent cross-cutting UX components.
  - `DestructiveAttribute` lives in `Contracts/Attributes/` alongside `CommandAttribute`, `ProjectionAttribute`, `BoundedContextAttribute`, `IconAttribute`, etc. — consistent adopter-facing attribute placement.
  - `FcShellOptions` extension stays in Contracts (architecture.md §1144 dependency-free invariant preserved — the two new `int` properties pull no new namespace).
  - HFC1020 (analyzer Warning) and HFC1021 (analyzer Error) land in the `HFC1xxx` range per §648 diagnostic-ID policy (`1xxx` = analyzer-emitted, `2xxx` = runtime-logged). HFC2103/HFC2104 land in the `HFC2xxx` range.
  - No Contracts → Shell reverse reference — both `Forms/` components live in Shell. The emitter's emitted `.g.cs` references `Hexalith.FrontComposer.Shell.Components.Forms` via emitted `using` directive (Task 6.1), same pattern as Story 2-4 D21 for `Lifecycle` components.
- **Fluent UI `Fc` prefix convention** honored — `FcDestructiveConfirmationDialog`, `FcFormAbandonmentGuard`.
- **Append-only `FcShellOptions` extension honoured** — 2 properties added to existing class, no new options type created. Crosses the 10-property threshold; split trigger remains 12 properties or 3rd cross-concern (Story 9-2 owns split).

---

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List

### Change Log

| Date | Change | By |
|---|---|---|
| 2026-04-17 | Story drafted — initial comprehensive spec with 23 decisions + 3 ADRs. Ready for Amelia/Winston/Murat review rounds before coding. | bmad-create-story |

### Review Findings
