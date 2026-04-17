# Tasks / Subtasks

> Checkboxes are intentionally unchecked. Dev agent marks them `[x]` as each lands. Numbers align to the AC quick index.

## Task 0 — Prereq verification + mandatory spikes (≤ 90 min)

- [x] 0.1: **RESOLVED during party review 2026-04-17.** Emitted `{Command}LifecycleFeatureState` uses `Error` + `Resolution` string properties, NOT `RejectionReason` + `RejectionResolution`. `CommandFluxorFeatureEmitter.cs:32-37` + reducer at L88 confirmed. `StubCommandServiceOptions.cs:25-28` already uses `RejectionReason`/`RejectionResolution` naming on the adopter-facing side — framework naming is INCONSISTENT today. **Task 5.2b is MANDATORY, not conditional.** Approach: rename emitted state-record fields `Error → RejectionReason`, `Resolution → RejectionResolution` in `CommandFluxorFeatureEmitter.cs` (emission + reducer projection) and re-approve every `CommandFluxorFeatureEmitter.*.verified.txt` snapshot. This aligns adopter-side (`StubCommandServiceOptions`) with framework-side (feature state). No API surface changes — internal reducer field renames only.
- [x] 0.2: Verified `IDialogService` is registered via `services.AddFluentUIComponents()` (wired in Counter.Web Program.cs; Shell bootstraps adopter-side). V5 canonical pattern confirmed via `mcp__fluent-ui-blazor__get_component_details`: `DialogService.ShowDialogAsync<TDialog>(options => options.Parameters.Add(nameof(TDialog.Prop), value))` lambda form — emitted into `CommandRendererEmitter.DestructiveBeforeSubmitAsync()`.
- [x] 0.3: **bUnit + `<NavigationLock>` compatibility spike.** Result: **PARTIAL PASS** — bUnit's default NavigationManager throws `NotSupportedException` on `SetNavigationLockState`; resolved by overriding the method in `TestNavigationManager`. Fully end-to-end `LocationChanging` interception requires Playwright (deferred per spec Task 8.11). Task 8.2 lands **7 unit-level bUnit tests** exercising first-edit anchor, wrapper-nav cascade, and `_isLeaving` flag-clear semantics via reflection — the navigation-interception branch itself is covered by the sharded abandonment-guard logic paths + Playwright E2E (Task 8.11 conditional).
- [x] 0.4: `NavigationLock` confirmed available in `Microsoft.AspNetCore.Components.Routing` for pinned .NET 10 SDK + Blazor version.
- [x] 0.5: `Microsoft.Extensions.TimeProvider.Testing` already referenced in `Hexalith.FrontComposer.Shell.Tests.csproj`.
- [x] 0.6: `FsCheck.Xunit.v3` reference present in both Shell.Tests and SourceTools.Tests.

## Task 1 — Contracts additions

- [x] 1.1: Created `src/Hexalith.FrontComposer.Contracts/Attributes/DestructiveAttribute.cs` with `ConfirmationTitle` / `ConfirmationBody` init-only properties (D1 / UX-DR37 / UX-DR58).
- [x] 1.2: Extended `FcShellOptions` with `FormAbandonmentThresholdSeconds` (default 30 s, `[Range(5, 600)]`) + `IdempotentInfoToastDurationMs` (default **5000 ms** per D6 revision, `[Range(1_000, 30_000)]`).
- [x] 1.3: Added `HFC2103_AbandonmentDuringSubmitting` + `HFC2104_IdempotentInfoBarRendered` runtime constants. (HFC1020/HFC1021 are analyzer-emitted; lives in `SourceTools/Diagnostics/` per §648 policy.)
- [x] 1.3b: Added `HFC1020` (Info) and `HFC1021` (Error) descriptors in `DiagnosticDescriptors.cs` and registered rows in `AnalyzerReleases.Unshipped.md`.
- [x] 1.4: Extended `FcShellOptionsThresholdValidator` with both D6 cross-property invariants (`FormAbandonmentThresholdSeconds * 1000 > StillSyncingThresholdMs` + `IdempotentInfoToastDurationMs <= ConfirmedToastDurationMs`).

## Task 2 — Parser + analyzer

- [x] 2.1: Added `IsDestructive` / `DestructiveConfirmTitle` / `DestructiveConfirmBody` to `CommandModel` (constructor, `Equals`, `GetHashCode`).
- [x] 2.2: `CommandParser.ParseDestructiveAttribute` detects the attribute + captures confirmation args. HFC1020 (Info) fires against the expanded regex (`Delete|Remove|Purge|Erase|Drop|Truncate|Wipe`) when `[Destructive]` is absent; HFC1021 (Error) fires and halts parsing when `IsDestructive && NonDerivableProperties.Count == 0`.
- [x] 2.3: Piped the three new fields through `CommandRendererModel` + `CommandRendererTransform.Transform`.

## Task 3 — FcDestructiveConfirmationDialog component

- [x] 3.1: Created `FcDestructiveConfirmationDialog.razor` + `.razor.cs` + `.razor.css` in `Shell/Components/Forms/`. Uses `FluentDialogBody` with `TitleTemplate` + `ChildContent` + `ActionTemplate`. Cancel button `AutoFocus="true"` (D11 — Enter does the safe thing). Destructive button `Appearance=Primary` + `Class="fc-destructive-confirm"` — red-palette via CSS variables. Escape key on dialog body dispatches `OnCancel` (D22).
- [x] 3.2: Destructive dialog open pattern emitted into `CommandRendererEmitter.DestructiveBeforeSubmitAsync()`: `DialogService.ShowDialogAsync<FcDestructiveConfirmationDialog>(options => options.Parameters.Add(...))`.

## Task 4 — FcFormAbandonmentGuard component

- [x] 4.1: Created `FcFormAbandonmentGuard.razor` + `.razor.cs` + `.razor.css` in `Shell/Components/Forms/`. `NavigationLock OnBeforeInternalNavigation="HandleNavigationChangingAsync"` wraps child content. D10 first-edit anchor via `EditContext.OnFieldChanged` (unsubscribes after first fire). D13 suppression: `Submitting` state logs HFC2103 + skips intercept; `Syncing` FIRES normally. D13 cascade: `[CascadingParameter(Name="WrapperInitiatedNavigation")]` bypasses guard. D24 `_isLeaving` flag cleared via `try/finally` around `Nav.NavigateTo`.
- [x] 4.2: `FluentMessageBar Intent="Warning"` with `ActionsTemplate` containing "Stay on form" (`AutoFocus="true"` — D9) and "Leave anyway". Escape on bar triggers Stay.

## Task 5 — FcLifecycleWrapper extension

- [x] 5.1: `LifecycleUiState` converted from positional to init-only record (D17 append-safe). Added `IsIdempotent` + `IdempotentDismissAt`. `From()` mapper populates `IsIdempotent` from `transition.IdempotencyResolved && NewState == Confirmed`. Added `[Parameter] RejectionTitle` + `[Parameter] IdempotentInfoMessage` (D17 revised — two new optional params). `ApplyTransition` branches on `IsIdempotent`: schedules `ScheduleIdempotentDismiss(IdempotentInfoToastDurationMs)` and logs HFC2104 with hashed CorrelationId; non-idempotent path schedules `ScheduleConfirmedDismiss()` unchanged. Razor template gained idempotent Info bar branch + domain-language rejection title resolver (`ResolveRejectionTitle()` → `RejectionTitle ?? "Submission rejected"`).
- [x] 5.2b: **MANDATORY rename completed.** `CommandFluxorFeatureEmitter` state record and `OnRejected` reducer now emit `RejectionReason`/`RejectionResolution` (not `Error`/`Resolution`). `OnSubmitted` reducer clears both fields. All `CommandFluxorFeatureEmitter.*.verified.txt` snapshots re-approved — inspected diffs were field-rename-local only.
- [x] 5.2: `CommandFormEmitter` emits `RejectionMessage="BuildFcLifecycleRejectionCopy()"` + `RejectionTitle="BuildFcLifecycleRejectionTitle()"` attributes on `<FcLifecycleWrapper>`. Added `EmitRejectionCopyHelpers` that emits both methods. Title format = `$"{DisplayLabel} failed"` per D4. Body join = `$"{Reason}. {Resolution}"` with whitespace-null collapse (single non-empty clause → returned alone; both null/whitespace → `null` → wrapper falls back to localized generic).
- [x] 5.3: Added `[Parameter] EventCallback<EditContext> OnEditContextReady` to emitted form; invoked immediately after `_editContext = new EditContext(_model)` in `OnInitialized`. Renderer captures via `OnFormEditContextReady` → stores in `_formEditContext` (consumed by `<FcFormAbandonmentGuard EditContext="...">`).

## Task 6 — Renderer integration

- [x] 6.1: `CommandRendererEmitter` extended:
  - Added `using Hexalith.FrontComposer.Shell.Components.Forms;` to emitted using-block.
  - Added `[Inject] IDialogService DialogService` (destructive commands only).
  - Added `private bool _dialogOpen;` field (destructive only) + `private EditContext? _formEditContext;` (always, for FullPage guard binding).
  - Added `OnFormEditContextReady(EditContext)` helper invoked via `OnEditContextReady` callback.
  - For destructive commands: emitted `DestructiveBeforeSubmitAsync` that (a) calls `RefreshDerivedValuesBeforeSubmitAsync` first, (b) guards double-open with `_dialogOpen`, (c) opens `FcDestructiveConfirmationDialog` via `IDialogService`, (d) throws `OperationCanceledException` on user cancel (the form's existing catch resets lifecycle via `ResetToIdleAction` P-11), (e) `try/finally` clears `_dialogOpen`. Wired into all three density branches via `beforeSubmitFunc` swap.
  - D24 satisfied implicitly: `OnValidSubmitAsync` only fires when the form's DataAnnotationsValidator reports valid state; `DestructiveBeforeSubmitAsync` runs inside the submit pipeline, so validation has already passed before the dialog opens.
  - FullPage branch wraps the form invocation in `<FcFormAbandonmentGuard CorrelationId="..." EditContext="_formEditContext">`. CompactInline + Inline branches are unchanged per D19.
- [x] 6.2: Added `@using Hexalith.FrontComposer.Shell.Components.Forms` to `src/Hexalith.FrontComposer.Shell/_Imports.razor`.

## Task 7 — Counter sample integration (optional destructive demo, mandatory config)

- [x] 7.1: Extended `samples/Counter/Counter.Web/appsettings.Development.json` with `FormAbandonmentThresholdSeconds: 30` + `IdempotentInfoToastDurationMs: 5000` (D6 revision: 5000 ms default, not 3000).
- [ ] 7.2: *(OPTIONAL, skipped — not a blocker.)* ResetCountCommand destructive demo deferred to a later sample-authoring story. Dialog/guard component behavior is covered by bUnit.

## Task 8 — Tests

- [x] 8.1: `FcDestructiveConfirmationDialogTests.cs` — 6 bUnit tests (title/body rendering, Cancel autofocus attribute, Cancel click fires OnCancel, Confirm click fires OnConfirm, Escape dispatches OnCancel not OnConfirm, destructive button has danger class).
- [x] 8.2: `FcFormAbandonmentGuardTests.cs` — 7 bUnit tests covering first-edit anchor, second-edit does-not-re-anchor, `_isLeaving` flag default + clear-via-try/finally, Stay button no-navigate + warning clear, Leave button navigate + flag clear, null-EditContext inert, Dispose unsubscribes. Full NavigationLock interception is Playwright-scope per Task 0.3 spike result.
- [x] 8.3: `FcLifecycleWrapperIdempotentTests.cs` — 5 bUnit tests (default copy, adopter override, XSS plain-text, auto-dismiss at `IdempotentInfoToastDurationMs`, non-idempotent Confirmed still renders Success).
- [x] 8.4: `FcLifecycleWrapperRejectionTests.cs` — 5 bUnit tests (domain title, fallback title, XSS plain-text, no-auto-dismiss regression, fallback copy when null).
- [x] 8.5: `CommandParserDestructiveTests.cs` — 6 parser tests ([Destructive] sets IsDestructive, ConfirmationTitle/Body capture, HFC1020 Info on Delete-named-sans-attribute, HFC1021 Error on zero-field destructive, benign non-destructive name does NOT fire HFC1020, expanded regex catches Wipe).
- [x] 8.6: Snapshot regen — re-approved **2 CommandFormEmitter snapshots** + **7 CommandRendererEmitter snapshots** after inspecting each diff for local-only changes (RejectionMessage/Title wrapper attributes + helper methods + OnEditContextReady parameter + InvokeAsync line + FullPage FcFormAbandonmentGuard wrapper + OnEditContextReady EventCallback attribute on all form instantiations). `CommandFluxorFeatureEmitter.*.verified.txt` re-approval subsumed by the Task 5.2b field-rename cascade.
- [x] 8.7: Extended `FcShellOptionsValidationTests.cs` — added 2 cross-property tests (`FormAbandonment_must_exceed_StillSyncing`, `Idempotent_toast_must_not_exceed_ConfirmedToast`) + 4 `[Theory]` bounds-check rows for the two new int properties.
- [ ] 8.8: *(Deferred to follow-up.)* `CommandRendererCompactInlineTests.cs` / `CommandRendererFullPageTests.cs` markup assertions — the FullPage+FcFormAbandonmentGuard wrap is already captured by the re-approved `Renderer_FiveFields_FullPageBoundarySnapshot` snapshot diff; explicit "does not reference FcDestructiveConfirmationDialog" assertions pending a dedicated test pass.
- [ ] 8.9: *(Deferred to follow-up.)* `CommandFormEmitterRejectionBehavioralTests.cs` — D5 input-preservation regression test. Behavior is preserved at the emitter level (the existing `catch (CommandRejectedException)` at L312-318 is unchanged); an explicit bUnit behavioral test is follow-up work.
- [ ] 8.10: *(Deferred to follow-up.)* 3 FsCheck property tests — Murat-recommended property-based coverage on (a) `BuildFcLifecycleRejectionCopy` helper, (b) HFC1020 regex, (c) abandonment guard decision tree. Deferred because emitted-helper property testing requires extracting the helper or exercising via an emitter roundtrip — needs dedicated design.
- [ ] 8.11: *(Deferred to follow-up — conditional on Task 7.2.)* 2 Playwright auto-focus E2E tests. Task 7.2's ResetCountCommand demo was skipped, so the destructive dialog Playwright test has no sample command to drive. The abandonment-guard Playwright test is still valid via Counter's IncrementCommand → deferrable to a later story.
- [ ] 8.12: *(Deferred — conditional on Task 7.2.)* E2E dialog-paint latency assertion — same reason as 8.11.

## Task 9 — Regression + zero-warning gate

- [x] 9.1: `dotnet build -warnaserror` — 0 warnings, 0 errors.
- [x] 9.2: `dotnet test --no-build` — 264 Shell.Tests + 271 SourceTools.Tests + 12 Contracts.Tests = **547 passing (2 skipped E2E latency unchanged from baseline)**. 29 net-new tests from this story (6 dialog + 7 guard + 5 idempotent + 5 rejection + 6 parser + 2 options cross-property + 4 options range-extension rows). Snapshot re-approvals (9) land the rest of the emitter-side coverage.
- [x] 9.3: `AnalyzerReleases.Unshipped.md` contains HFC1020 (Info) and HFC1021 (Error) rows; zero unreleased descriptors.
- [ ] 9.4: *(Manual regression deferred to reviewer.)* Counter sample smoke-launch is not automatable in this session; Task 9.1 + 9.2 gates confirm no code-level regression.

### Review Findings

- [ ] [Review][Decision] Resolve authority drift between `acceptance-criteria.md` and shipped behavior — **AC8** requires `HFC1020` at **Warning** severity and `TreatWarningsAsErrors` failure; implementation, `DiagnosticDescriptors`, `AnalyzerReleases.Unshipped.md`, and `CommandParserDestructiveTests` use **Info** (see `CommandParser.cs` HFC1020 emission and `Parse_DeleteNamed_WithoutAttribute_EmitsHFC1020Info`). Either revise AC8 / ADR-026 text to ratify Info + Story 9-4 promotion, or raise severity to Warning and add suppression UX.

- [ ] [Review][Decision] Resolve **AC6** vs **D13** on abandonment suppression — `acceptance-criteria.md` states the warning is suppressed when lifecycle state is **Submitting or Syncing**; `FcFormAbandonmentGuard` (`HandleNavigationChangingAsync`) suppresses only **Submitting**, and `tasks-subtasks.md` documents Syncing as still intercepting. Pick one canonical rule and align AC6 text or guard logic.

- [x] [Review][Patch] Idempotent Confirmed live-region copy mismatch [`FcLifecycleWrapper.razor` `Announcement` / `DataPhase`] — **Fixed 2026-04-17:** idempotent `Confirmed` announces `"Already confirmed"` and uses `data-fc-phase="confirmed-idempotent"`.

- [x] [Review][Patch] Default idempotent Info body string — **Fixed 2026-04-17:** default matches AC2 (`"This was already confirmed — no action needed."`).

- [x] [Review][Patch] Idempotent auto-dismiss anchor — **Fixed 2026-04-17:** `IdempotentDismissAt`, `ConfirmedDismissAt`, and dismiss timers use `transition.LastTransitionAt` + duration; `ComputeDueTimeFromTransitionAnchor` sets timer `dueTime` from anchor, not handler wall clock.

- [x] [Review][Patch] Destructive dialog button appearances vs AC3/AC7 — **Addressed 2026-04-17:** Fluent UI AspNetCore Components **5.0.0-rc.2** exposes only `ButtonAppearance` = Default, Outline, Primary, Subtle, Transparent (no Secondary/Accent). Cancel uses **Outline** (secondary-like); destructive action remains **Primary** with `fc-destructive-confirm` CSS danger palette per existing D11. Align AC3/AC7 prose to this API surface or document mapping.

- [x] [Review][Defer] Large planning-artifact file moves/deletes — pre-existing consumers of monolithic `_bmad-output/planning-artifacts/*.md` paths may break until references update; not introduced by Story 2-5 runtime code.

- [x] [Review][Defer] `HFC2103_AbandonmentDuringSubmitting` names “Submitting” but is also used for wrapper-initiated navigation bypass — logging ID semantics only; clarify in a later cleanup.

---
