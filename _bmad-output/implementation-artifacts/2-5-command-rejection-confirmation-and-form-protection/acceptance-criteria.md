# Acceptance Criteria

## AC1: Rejection → domain-specific message, no auto-dismiss, form input preserved

**Given** a command form wrapped in `FcLifecycleWrapper` via `CommandFormEmitter` (Story 2-4 ADR-020)
**And** the adopter's `ICommandService` throws `CommandRejectedException(reason: "Approval failed: insufficient inventory", resolution: "The order has been returned to Pending.")`
**When** the generated form's `OnValidSubmitAsync` catches the exception and dispatches `RejectedAction(correlationId, reason, resolution)`
**Then** the wrapper renders a `FluentMessageBar` (Intent=Error) with:
  - Title: `RejectionTitle` parameter if non-null (domain-language, e.g., "Approval failed"), else localized "Submission rejected"
  - Body: `"Approval failed: insufficient inventory. The order has been returned to Pending."` (period-space joined)
**And** when both `reason` and `resolution` are null/whitespace, the wrapper falls back to a localized generic message (`"The command was rejected. Please review your input and try again."`) so the user is never shown an empty MessageBar (graceful degradation — revised 2026-04-17 per code-review D2)
**And** the MessageBar has NO auto-dismiss (2-4 D17 regression — user dismisses via close button)
**And** `aria-live="assertive"` announces the rejection (2-4 AC7)
**And** the form's `_model` fields are UNCHANGED (user's input preserved — D5 regression test)
**And** the submit button re-enables for retry (2-4 AC7)
**And** `RejectionMessage` renders as plain text — `<script>` in Reason/Resolution is HTML-encoded (D14 XSS)

References: FR30, UX-DR46, NFR46, NFR47, NFR103, Decision **D4, D5, D14, D17**, Story 2-4 D22

## AC2: Idempotent outcome → Info MessageBar with auto-dismiss

**Given** the wrapper observes a `Confirmed` transition with `IdempotencyResolved == true`
**When** the transition arrives at any lifecycle phase (NoPulse/Pulse/StillSyncing/ActionPrompt)
**Then** a `FluentMessageBar` (Intent=Info) is rendered with:
  - Title: localized "Already confirmed"
  - Body: `IdempotentInfoMessage` parameter if non-null, else localized "This was already confirmed — no action needed." (D7 — safe under both cross-user and self-reconnect replay contexts)
**And** the bar auto-dismisses after `FcShellOptions.IdempotentInfoToastDurationMs` (default **5000 ms** per Sally #2) from the Confirmed `LastTransitionAt`
**And** `aria-live="polite"` announces the outcome — when `IdempotentInfoMessage` is non-null the live region announces that custom copy, otherwise the localized "Already confirmed" (revised 2026-04-17 per code-review P12 — announcement matches visible body)
**And** `HFC2104` is logged at Information level with a redacted correlation ID (first 8 chars + ellipsis — not a cryptographic hash)
**And** the Success `FluentMessageBar` (non-idempotent Confirmed path, 2-4 AC6) is NOT rendered concurrently — the Info bar replaces it for the idempotent case
**And** `IdempotentInfoMessage` renders as plain text (D14 XSS)

References: FR30, UX-DR46, Decision **D3, D6, D7, D14, D17**, Story 2-4 Known Gap G2, Story 5-4 (future replay-source disambiguation)

## AC3: Destructive command → pre-submit confirmation dialog

**Given** a `[Destructive]` command class (e.g., `[Destructive] public sealed record DeleteOrderCommand(Guid OrderId) { }`)
**When** the generated renderer emits the submit trigger
**Then** clicking the trigger opens an `FcDestructiveConfirmationDialog` (via `IDialogService.ShowDialogAsync`)
**And** the dialog renders with:
  - Title: `ConfirmationTitle` if set, else `$"{DisplayLabel}?"` (e.g., "Delete Order?")
  - Body: `ConfirmationBody` if set, else localized "This action cannot be undone."
  - Cancel button: `Appearance=Outline` (secondary-like in Fluent UI v5 rc.2 — no `Secondary` enum value), `AutoFocus="true"` which renders the standard HTML `autofocus` attribute; Fluent's dialog focus manager places focus on this button after dialog paint
  - Destructive action button: `Appearance=Primary` with `Class="fc-destructive-confirm"` applying the danger color palette via CSS variables (Fluent UI v5 rc.2 has no `Accent`/`Color=Error` slot — the shell owns the danger styling), label = `DisplayLabel` (domain-language, e.g., "Delete Order")
**And** pressing Escape dispatches `OnCancel` (D22)
**And** `Enter` on the auto-focused Cancel button does NOT fire `OnConfirm`
**And** dismissal without confirmation ABANDONS the submit (form `_model` unchanged, lifecycle never transitions from Idle)
**And** confirmation invokes the form's `_externalSubmit` (Story 2-2 ADR-016) — lifecycle proceeds normally from there

> **API surface note (2026-04-17):** `Microsoft.FluentUI.AspNetCore.Components` **5.0.0-rc.2** only exposes `ButtonAppearance` ∈ {Default, Outline, Primary, Subtle, Transparent}. "Secondary"/"Accent"/"Neutral" in the original spec prose are Fluent v4 names that were not carried forward to v5 rc.2. The mapping above is canonical for this story; revisit if Fluent v5 GA reintroduces a Danger/Accent slot.

References: FR30, UX-DR36, UX-DR37, UX-DR58, Decision **D1, D2, D11, D12, D22**, ADR-024

## AC4: Destructive commands never render as `Inline` (0-field button)

**Given** a command class annotated `[Destructive]` with zero non-derivable properties (would classify as `CommandDensity.Inline`)
**When** the source generator runs on the project
**Then** diagnostic `HFC1021` is emitted at Error severity against the command class declaration
**And** the message reads `"Destructive command '{TypeName}' must have at least one non-derivable property (destructive commands cannot render as inline buttons)."`
**And** the project build FAILS
**And** no renderer `.g.cs` is emitted for the offending command (or, if emitted, the destructive dialog is rendered in CompactInline minimum — verify parse-stage halt is cleanest — Task 2.2 decides)

References: UX-DR36 ("Danger never inline on DataGrid rows"), epics §1048, Decision **D1, D20**, ADR-026

## AC5: Non-destructive commands show NO confirmation dialog

**Given** a non-destructive command (e.g., `ApproveCommand`, `CreateOrderCommand`, `UpdateProfileCommand` — no `[Destructive]`)
**When** the user submits
**Then** no `FcDestructiveConfirmationDialog` opens
**And** the lifecycle wrapper provides feedback per Story 2-4 AC1-9
**And** the emitted renderer's rendered markup contains no `FcDestructiveConfirmationDialog` reference (snapshot tests verify — Task 8.6)

References: UX-DR58, epics §1057-1059, Decision **D2**, Story 2-4 AC1

## AC6: Full-page form active >30 s (first-edit anchored) → abandonment protection

**Given** a `CommandRenderMode.FullPage` generated renderer (command with 5+ non-derivable fields)
**And** `FcShellOptions.FormAbandonmentThresholdSeconds = 30` (default)
**When** the user mounts the form and types in ANY field
**Then** `FcFormAbandonmentGuard` starts its timer anchored on `EditContext.OnFieldChanged` first-fire (D10)
**When** the user then clicks breadcrumb / sidebar / command-palette navigation after ≥ 30 s elapsed
**Then** `NavigationLock` fires `LocationChanging`, the guard calls `ctx.PreventNavigation()`, and a `FluentMessageBar` (Intent=Warning) renders at the top of the form:
  - Title: "You have unsaved input." (rendered via `FluentMessageBar.Title`; supplementary body "Leaving now discards what you've entered." rendered via `ChildContent` — reconciled with the shipped Fluent v5 rc.2 MessageBar API 2026-04-17)
  - Action buttons: "Stay on form" (`Appearance=Primary`, auto-focused via `AutoFocus="true"`) + "Leave anyway" (`Appearance=Outline` — secondary-like per Fluent v5 rc.2 API surface; see AC3 API surface note)
**And** Escape on the warning bar triggers "Stay on form" (D9)
**And** clicking "Leave anyway" removes the lock and re-invokes `NavigationManager.NavigateTo(pendingTarget)`
**And** `CompactInline` + `Inline` renderers do NOT render the guard (snapshot tests verify — Task 8.8, D19)
**And** below 30 s (first edit to nav-attempt interval) the guard does NOT intercept
**And** if the user mounts the form and never edits, no interception occurs regardless of mount-duration
**And** the warning is SUPPRESSED **only** when `ILifecycleStateService.GetState(correlationId)` returns `Submitting` — logged `HFC2103` at Information severity (D13 revised 2026-04-17 per Sally #6 ActionPrompt edge)
**And** when the lifecycle state is `Syncing` (including `Syncing` with `TimerPhase=ActionPrompt`) the guard FIRES normally — external nav during long-running syncs still protects unsaved edits
**And** when the wrapper's own Start-over button initiates navigation, the `[CascadingValue] WrapperInitiatedNavigation` flag suppresses the warning (wrapper-legitimate nav never competes with abandonment UX)

References: FR30, UX-DR38, UX-DR58, epics §1050-1055, UX spec §2318-2331, Decision **D6, D8, D9, D10, D13, D19**, ADR-025

## AC7: Button hierarchy enforced in emitted renderers

**Given** any command rendered via `CommandRendererEmitter`
**When** the renderer emits its button chrome
**Then** the submit button appearance is `ButtonAppearance.Primary` in CompactInline + FullPage modes, `ButtonAppearance.Outline` in Inline 0-field, following UX spec §2221-2226 (per Fluent UI v5 rc.2 API — see AC3 API surface note; v4 "Accent" ≈ v5 "Primary")
**And** all button labels are humanized via Story 2-1's `ResolveLabel` chain (`[Display(Name)]` → IStringLocalizer → humanized CamelCase → raw) — NEVER hardcoded "OK"/"Submit"
**And** Cancel buttons inside popover / dialog use `ButtonAppearance.Outline` (secondary-like in v5 rc.2 — v4 "Neutral" has no v5 equivalent)
**And** Destructive action buttons inside `FcDestructiveConfirmationDialog` use `ButtonAppearance.Primary` + `Class="fc-destructive-confirm"` for the danger CSS palette (v5 rc.2 has no Accent/Color=Error slot) with domain-language label (e.g., "Delete Order")
**And** Primary buttons *emitted by `CommandRendererEmitter`* include `IconStart` bound to `ResolveIcon()` — shell-level components such as `FcFormAbandonmentGuard` ("Stay on form") are **explicitly out of scope** for this IconStart requirement because they have no command metadata to resolve an icon from (clarified 2026-04-17 per code-review D3)
**And** the destructive dialog's Cancel button uses Fluent's `AutoFocus="true"` property (renders the HTML `autofocus` attribute) so Enter does the safe thing (D11; replaces earlier `data-autofocus` prose — Fluent v5's `FluentButton.AutoFocus` is the canonical mechanism)

References: FR30, UX-DR36, epics §1061-1068, Decision **D11, D12**

## AC8: Build-time analyzer hint on destructive-pattern-name commands missing `[Destructive]`

**Given** a command class whose type name matches the expanded pattern `^(Delete|Remove|Purge|Erase|Drop|Truncate|Wipe)[A-Z]` or `^(Delete|Remove|Purge|Erase|Drop|Truncate|Wipe)Command$` (e.g., `DeleteOrderCommand`, `RemoveCartItemCommand`, `PurgeLogsCommand`, `EraseUserCommand`, `WipeCacheCommand`)
**And** the class does NOT have `[Destructive]` applied
**When** the source generator's parse stage runs
**Then** `HFC1020` diagnostic is emitted at **Info severity** against the class declaration (per D20 — deliberate v0.1 gradient; Warning promotion + `[SuppressHFC1020]` escape hatch ship in Story 9-4)
**And** the message reads `"Command '{TypeName}' appears destructive by name but is missing [Destructive] attribute. Add [Destructive] or rename the command."`
**And** under Story 1-7's `TreatWarningsAsErrors=true` the build DOES NOT fail on HFC1020 in v0.1 — Info diagnostics are surfaced in IDE + build output without halting CI, preventing Day-1 adoption blockers for codebases with pre-existing non-destructive `Remove*`/`Delete*` commands (elicitation Pre-mortem P0-C)
**And** `AnalyzerReleases.Unshipped.md` has the HFC1020 Info + HFC1021 Error entries registered

References: UX-DR36, UX-DR37, NFR47, Decision **D1, D20**, ADR-026, architecture.md §648 (diagnostic policy), Story 9-4 (Warning promotion + suppression escape hatch)

## AC9: Error recovery supports retry and input modification without re-typing

**Given** a rejected command where the rejection message explains failure cause + data state
**When** the user reads the Danger MessageBar
**Then** the submit button is re-enabled (2-4 AC7 regression preserved)
**And** the form's field values are preserved (D5 — `_model` not reset)
**And** the user can modify one field and re-submit without losing the others
**And** the recovery path requires zero external documentation — the MessageBar copy guides next action (NFR103)
**And** a re-submit generates a new `CorrelationId` via `Guid.NewGuid()` at the top of `OnValidSubmitAsync` — this IS the P-11 pattern (each submit acquires a fresh CorrelationId, and the separate `ResetToIdleAction` dispatch in the `catch (OperationCanceledException)` path handles the dialog-cancel case). The rejected bar does not linger past the new submission because the wrapper binds to whichever CorrelationId the form is currently tracking (clarified 2026-04-17 per code-review D4)

References: FR30, UX-DR46, NFR46, NFR47, NFR103, Decision **D4, D5**, Story 2-3 patch P-11

---
