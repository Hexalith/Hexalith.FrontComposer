---
title: 'Story 13.3: Preserve Focus and Input Through Command Safety Outcomes'
type: 'feature'
created: '2026-09-28'
status: 'done'
route: 'dispatch'
review_loop_iteration: 3
baseline_commit: '52fa0739ab8dfb02e1b9d8b5b05d2fd318f220d8'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Generated command forms preserve core safety semantics but do not consistently preserve focus, entered values, accessible validation relationships, or a single truthful speech path when validation, rejection, authorization, confirmation, abandonment, or concurrent-submit outcomes occur.

**Approach:** Complete the canonical UX-B interaction contract around generated forms by adding linked focus-only validation summaries, mapped and unmapped rejection recovery, deterministic confirmation and abandonment focus return, fail-closed authorization focus, and an announced no-queue concurrency outcome while retaining the existing command lifecycle and authorization order.

## Boundaries & Constraints

**Always:** Implement VR-01–06, FM-05–09/11, OF-03/04, AM-18–20, and the assigned SS rows. Reuse FrontComposer and Fluent UI V5 controls, the existing focus helper, safe validation allowlist, lifecycle identity, admission gate, and Story 13.2 route-heading fallback. Preserve authorization before and after `BeforeSubmit` and at the service boundary, entered values, declared field order, one local advancing operation, and support-safe localized text.

**Never:** Queue or dispatch a blocked command, invent field errors, expose payloads/policy internals/backend metadata/stack traces/unrestricted PII, use alert/live semantics for focus-only summaries or headings, create nested modals, move AM-14/AM-26 ownership from Story 13.4, add Story 13.7 testing helpers, or claim OI-16, G-4, FLUENT-APP-1, or Product approval.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Validation | Client errors or safe mapped rejection | Complete linked summary renders before one focus move; stable error targets and values remain | Missing target advances to next invalid input; missing summary focuses first invalid input |
| Rejection/denial | Unmapped rejection or authorization denied | Rejection keeps lifecycle/recovery focus and AM-14 path; denial replaces form and focuses its heading | No fabricated errors; mapped rejection suppresses AM-14 |
| Confirmation | Destructive action | Cancel starts focused; modal is named/described/contained; confirm dispatches once | Escape/cancel dispatch nothing and restore invoker or route h1 |
| Abandonment | Edited for 30 seconds, then navigation | Silent in-flow warning focuses Stay; Stay/Escape preserves values and restores edited control | Removed origin falls back to form heading; only Leave discards |
| Concurrency | Another local command is active | Later submit stays focused, dispatch count remains one, AM-20 announces once | Optional View action focuses active lifecycle heading |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`, `Transforms/CommandFormTransform.cs`, and form models — emit Fluent editable inputs, one shared visible/programmatic container per declared field group in declared group/field order, Fluent-supported model validation association for split text bindings, stable validation targets, focused summaries, mapped-rejection state, and AM-20. Every submit event, including an invalid one, must check the active lifecycle/admission outcome before validation can move focus or publish feedback. Each editor's invalid state and its error/description relationship must reach the focusable control inside the Fluent editor's shadow root. Verify this in the Chromium accessibility tree: an invalid field's textbox reports `invalid` and a description containing its error. Host-element ARIA does not cross the shadow boundary. Prefer Fluent V5 field messaging (`Message`, `MessageState`, `MessageTemplate`, `ValidationFieldFor`); where 5.0.0-rc.5 still does not expose it, project the state onto the inner control from `fc-focus.js`. Each field shows its current error text exactly once, in Fluent's error styling, and keeps a stable error target for summary links. Field descriptions and group containers use Fluent-styled rendering, not unstyled raw HTML. Only a genuine denial or sign-in requirement replaces the form; infrastructure authorization failures (`MissingService`, `MissingPolicy`, `StaleTenantContext`, `HandlerFailed`, `Canceled`, `CatalogInconsistent`) keep the entered form and its retry warning. Unmapped rejection text never feeds a later client-validation summary.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` — connect destructive-dialog origin/return focus and authorization-denied heading behavior while retaining pre-dialog validation and duplicate-dialog prevention. Focus a denial heading only when the denial answers an operator submit/activation or replaces a form that contained focus; an initially denied renderer or a background authorization refresh renders its replacement without moving focus (FM-01, AM-26).
- Generated forms and zero-field inline renderers — AM-20 uses one always-mounted polite, atomic status node per form surface whose text is cleared and re-set for each blocked attempt, so every blocked attempt, including an identical repeat, announces exactly once. A zero-field inline renderer, whose form is hidden, owns that node visibly beside its trigger.
- `src/Hexalith.FrontComposer.Shell/Components/Forms/` and `wwwroot/js/fc-focus.js` — provide reusable validation-summary focus/link behavior, accessible destructive confirmation, and sequenced field-bound edited-control capture/return for the non-modal abandonment guard. Origin capture must ignore warning-action focus and stale completions from earlier field edits.
- `src/Hexalith.FrontComposer.Contracts/Communication/`, `Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs`, and `Shell/Services/Validation/ServerValidationApplicator.cs` — preserve support-safe field maps on rejected outcomes and distinguish mapped from unmapped recovery without weakening the allowlist.
- `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor{,.cs}` and pending-command admission types — expose a focusable active lifecycle/recovery target and mapped-rejection announcement suppression while preserving the authoritative operation identity. When the focused active-lifecycle heading unmounts because its command settles, move focus to the owning form's first editable control, or its submit control when none exists; never let it fall to `body`.
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources*.resx` — localize AM-18/19/20, summaries, recovery actions, confirmation, abandonment, and denial copy with EN/FR parity.
- `samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor.css` and `tests/e2e/specs/specimen-accessibility.spec.ts` with its visual baselines — retarget specimen overrides and contrast samples from the removed raw label and `fluent-message-bar` markup to the Fluent editor labels and denial card, keeping the CI a11y lane valid and the specimen readable.
- `tests/Hexalith.FrontComposer.{SourceTools,Shell}.Tests/` and `tests/e2e/specs/{destructive-command-confirmation,form-abandonment-guard,one-at-a-time-execution-policy,policy-gated-command-authorization,command-form-generation}.spec.ts` — pin generated markup, focus/speech counts, value preservation, lifecycle identity, no-dispatch outcomes, and browser containment/return behavior.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`, its transform/models, and validation/rejection runtime files in the Code Map — emit accessible Fluent controls whose invalid state and error/description relationships reach the focusable control, with each error shown once, complete linked summaries, deterministic fallbacks, one container per declared field group, correct model-field validation association, and safe mapped-rejection data while preserving values, lifecycle identity, and the form through infrastructure authorization failures.
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`, `src/Hexalith.FrontComposer.Shell/Components/Forms/`, lifecycle components, and `wwwroot/js/fc-focus.js` — add names/descriptions, ordered field-bound origin return, modal-only containment, silent Stay/Escape behavior, activation-owned denied-heading focus, settle-safe active-lifecycle focus, and recovery actions.
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`, `CommandRendererEmitter.cs`, pending-command admission types, and `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources*.resx` — check active-command blocking before both valid and invalid submit handling, retain attempted-submit focus, announce exact AM-20 once per blocked attempt through an always-mounted node (renderer-owned for zero-field inline commands), offer only a functional active-lifecycle action, and keep the original operation as the only advancing lifecycle.
- [x] `samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor.css` and `tests/e2e/specs/specimen-accessibility.spec.ts` — retarget specimen styling, contrast samples, and any affected visual baselines to the new Fluent editor and denial markup.
- [x] `tests/Hexalith.FrontComposer.{SourceTools,Shell}.Tests/` and the Story 13.3 files under `tests/e2e/specs/` in the Code Map — cover every matrix fallback, compiled/rendered field-group semantics, multi-field edited-origin ordering, real browser focus destinations, modal boundary wrapping/nested-modal refusal, and prove one speech path, one dispatch, input preservation, and redaction.

**Acceptance Criteria:**
- Given client or safely mapped server errors, when the outcome renders, then one non-live linked summary is focused after insertion, preserves values and declared relationships, and mapped rejection remains `Rejected` without AM-14 duplication.
- Given an unmapped rejection or authorization denial, when it renders, then no field error is invented, support-safe recovery remains keyboard reachable, and focus/announcement uses only its canonical path.
- Given confirmation, abandonment, or a blocked second submit, when the operator cancels, stays, leaves, confirms, or views the active command, then focus and dispatch follow the matrix exactly and no implicit or duplicate action occurs.

## Implementation Notes

**Review loop 1 KEEP instructions:** Preserve the working non-live linked validation summary and its missing-target/summary fallbacks; mapped-rejection lifecycle identity and AM-14 suppression; authorization-denial replacement/focus; destructive confirmation naming, Cancel-first behavior, origin/h1 return, and single dispatch; silent in-flow abandonment warning and explicit Leave; one polite atomic AM-20 region with attempted-submit focus; support-safe localized EN/FR copy; and the passing focused unit, integration, typecheck, and Story 13.3 Chromium verification structure. Re-derive these behaviors while correcting the loop-triggering contracts and all accepted patch findings in the triage log.

**Review loop 2 KEEP instructions:** Keep everything in the loop 1 list, plus these loop 2 results:

- one `OnSubmit` path that checks lifecycle and admission before validation
- sequenced, field-bound edited-origin capture per guard root, with the form-heading fallback
- one `fieldset`/`legend` per declared group, with description and error `aria-describedby`
- `FluentTextInput` editors with `ValidationFieldFor` on split numeric bindings
- counter-based per-instance form DOM ids (ULID policy)
- same-page fragment href plus programmatic focus on summary links, with the Web using
- `focusFirstEditableWithin` preferring generated editors over the hidden antiforgery input
- `containDialogFocus` boundary wrapping, and the expiring overlay-intent reservation for destructive dialogs
- the View-active-command availability check and withdrawal
- `HasMappedFieldErrors` through Fluxor, and `CommandRejectedException.FromProblem`
- CRLF line endings (LF for `.verified.txt`), the analyzer inventory reseal, and the dedicated rejection-host Chromium test

Also apply every iteration-2 `patch` row in the triage log.

**Review loop 3 KEEP instructions:** Keep the loop 1 and loop 2 lists, plus the loop 2 re-derivation results:

- `FcCommandBlockedOutcome`: one always-mounted status node per form; clear, wait, then re-set on each attempt; serialized attempts; renderer-owned inline variant for zero-field commands; View availability check and withdrawal
- activation-owned denial focus through `captureFocusBeforeReplacement`/`focusReplacementHeading`, with no focus on an initial denial
- form retention for transient authorization failures (including `Pending` at submit)
- dispatcher-marshalled summary show/hide
- rendered-order summary descriptors
- guard capture only when the edited field changes
- the restored `preserveExisting` overlay branch
- `preventDefault` on summary links only after scripted focus is proven, and the native fallback otherwise
- unmapped-rejection text kept out of summaries
- the specimen CSS and a11y sample retargeting
- the `OnConfirmed` dispatcher marshalling
- the settled-admission wait in `CommandTargetGeneratedFormTests`
- all loop 2 tests

Also apply every iteration-3 `patch` row in the triage log.

**Loop 2 verification blocker (2026-09-29):** The CSS and a11y samples were retargeted, but the six `specimen-accessibility` visual baselines (linux and win32) were not regenerated. The CI lane renders them from the Counter Test-environment host, and that host cannot boot: `MapFrontComposerMcp` throws outside Development (pre-existing since `e5666650`), and the spec forbids editing tracked host code to work around it. Regenerate them with `npm --prefix tests/e2e run test:visual:update` once that host is healthy. For the same reason, the allowed-policy Story 4.4 test and the dark-specimen contrast test were verified only up to their Development-environment limits.

## Spec Change Log

- **Review loop 1 (2026-09-28):** ECH-05 showed invalid submits could bypass active-command blocking, BH-05 showed edited-origin capture could be overwritten by warning focus or stale interop completion, and BH-13 showed declared groups were emitted as repeated one-field groups. The Code Map and execution tasks now require pre-validation concurrency gating, sequenced field-bound origin capture, and one shared visible/programmatic container per declared group, plus explicit rendered/browser evidence. This avoids the known-bad states of validation focus replacing AM-20, Stay restoring to the warning action or an older field, and repeated labels without a real group. KEEP the complete validation/rejection, denial, confirmation, abandonment, concurrency, localization, and verification behavior listed in Implementation Notes.
- **Review loop 2 (2026-09-29):**
  - **Findings:**
    - BH2-04/VG2-11/E2-05: initially denied renderers and background authorization refreshes steal focus from the route heading.
    - BH2-06/E2-12: the AM-20 region is inserted together with its text and stays silent on repeat attempts.
    - VG2-13: zero-field inline renderers hide AM-20 inside their `display:none` form.
    - VG2-08: the CI a11y spec and the specimen styling target removed markup.
    - BH2-05: infrastructure authorization failures replace the form with no retry path.
  - **Amended:** the Code Map, tasks, and verification now require:
    - denial focus owned by an operator activation
    - an always-mounted, per-attempt AM-20 node, owned by the renderer for zero-field inline commands
    - retargeted specimen styling and a11y samples
    - form retention for infrastructure authorization failures
  - **Known-bad states avoided:** focus leaving the route `h1` on page load; an unannounced or once-only AM-20; an invisible blocked outcome; a red a11y lane; a stuck "Please retry" panel.
  - **KEEP:** the loop 1 and loop 2 lists in Implementation Notes.
- **Review loop 3 (2026-09-29):**
  - **Findings:**
    - BH3-08/BH3-09, confirmed by a Chromium accessibility-tree probe: every generated textbox reports `invalid=false` and no description even when invalid. Host `aria-describedby`/`aria-invalid` never cross the Fluent shadow boundary, Fluent's own message duplicates the error text ("Invalid number format." twice), and the raw description, error, and group markup is unstyled.
    - BH3-04/E3-02: focus falls to `body` when the focused active-lifecycle heading unmounts on settle.
  - **Amended:** the Code Map, tasks, and verification now require invalid state and error/description relationships on the focusable inner control, verified in the accessibility tree; each error shown once in Fluent styling; Fluent-styled descriptions and groups; and a settle-safe focus fallback to the owning form.
  - **Known-bad states avoided:** screen readers hearing valid, undescribed fields; duplicated visible errors; unstyled adopter forms; focus lost to `body` after "View active command".
  - **KEEP:** the loop 1, loop 2, and loop 3 lists in Implementation Notes.

## Review Triage Log

| Iteration | Layer | Finding | Verdict | Evidence | Route |
|---:|---|---|---|---|---|
| 1 | verification-gap | VG-01: Modal-containment E2E never crosses either focus boundary. | medium | The test moves only between adjacent Cancel and Confirm controls, so a broken wrap at the last/first control remains undetected. | patch |
| 1 | verification-gap | VG-02: Nested-modal fail-closed behavior executes only through an interop mock. | medium | No browser test places focus inside a real Fluent dialog before attempting the destructive action. | patch |
| 1 | verification-gap | VG-03: Generated validation summary and linked-field focus are not observed in a browser. | medium | Existing browser coverage checks visible copy or synthetic fallback markup, while component coverage only observes mocked interop calls. | patch |
| 1 | verification-gap | VG-04: Unmapped-rejection “Edit and retry” focus is verified only as an interop call. | medium | No browser assertion proves the real Fluent editor receives focus. | patch |
| 1 | verification-gap | VG-05: Most-recently-edited abandonment return is tested with only one field. | medium | Reverting to first-field capture would leave the current same-field unit and browser assertions green. | patch |
| 1 | verification-gap | VG-06: Annotated field semantics are protected only by transform/emitted-source assertions. | medium | No compiled rendered fixture proves the generated group and description relationships survive component rendering. | patch |
| 1 | edge-case-hunter | ECH-01: Guid and TimeOnly fields reach the string-only text-input emitter. | medium | The path is real, but the baseline native-input emitter already assigned strings to these typed properties; Story 13.3 did not originate the unsupported typed-adapter gap. | defer |
| 1 | edge-case-hunter | ECH-02: Nullable Boolean fields use non-nullable switch expressions. | medium | The new expression exposes the mismatch, but the baseline already bound nullable Boolean values to a non-nullable Fluent switch contract. | defer |
| 1 | edge-case-hunter | ECH-03: Nullable enum fields use non-nullable select expressions. | medium | The new expression exposes the mismatch, but the baseline already bound nullable enum values to a non-nullable Fluent select contract. | defer |
| 1 | edge-case-hunter | ECH-04: Hidden-field descriptors can produce links without rendered controls. | maybe-false | The implementation deliberately supports missing targets and advances through later invalid controls; an actual filtered form rejected solely on omitted fields is needed to establish whether the residual no-target case violates intended filtered-form behavior. | defer |
| 1 | edge-case-hunter | ECH-05: An invalid later submit bypasses the concurrency outcome. | medium | `EditForm.OnInvalidSubmit` calls the summary directly, before lifecycle/admission checks, so it can move focus and omit AM-20 while another command is active. | bad_spec |
| 1 | edge-case-hunter | ECH-06: “View active command” can be inert when no lifecycle heading is mounted. | medium | The action is always rendered while `focusActiveLifecycle` only searches the current document and returns false without fallback. | patch |
| 1 | edge-case-hunter | ECH-07: Unmapped form-level rejection messages are retained but not immediately rendered. | low | The extra messages are hidden when no safe field maps, but the canonical lifecycle reason, resolution, and recovery action remain visible; expanding the presentation surface is disproportionate for this supplemental-copy loss. | reject |
| 1 | edge-case-hunter | ECH-08: Authorization-denial heading IDs collide across two renderer instances of the same command. | medium | The generated ID contains only the command type, and `getElementById` can therefore focus the other instance. | patch |
| 1 | edge-case-hunter | ECH-09: Two destructive renderers can capture an opening intent before either dialog receives focus. | medium | `captureOverlayOrigin` checks current dialog focus but not an existing open intent, while `_dialogOpen` is only per renderer. | patch |
| 1 | edge-case-hunter | ECH-10: A validation link has no native fragment fallback after interop failure. | low | `preventDefault` is unconditional, so the catch comment claiming a native fallback is inaccurate. | patch |
| 1 | edge-case-hunter | ECH-11: Destructive-dialog focus-module disposal can rethrow after handled interop failure. | low | `DisposeAsync` runs unguarded in `finally`, outside the existing JS exception catches. | patch |
| 1 | edge-case-hunter | ECH-12: Lifecycle recovery focus-module disposal can rethrow after handled interop failure. | low | `DisposeAsync` runs unguarded in `finally`, outside the existing JS exception catches. | patch |
| 1 | edge-case-hunter | ECH-13: Validation-summary focus-module disposal can rethrow after handled interop failure. | low | Both validation focus paths dispose outside the existing JS exception catches. | patch |
| 1 | edge-case-hunter | ECH-14: Empty validation-message lists suppress the detail fallback. | false | The framework classifier removes empty lists, and even a manually constructed public payload still renders the exception reason/resolution through the lifecycle rejection; the claimed message-less outcome does not occur. | reject |
| 1 | edge-case-hunter | ECH-15: One-error summaries use plural grammar. | low | Both locales always format the plural resource regardless of count. | patch |
| 1 | blind-hunter | BH-01: Filtered/hidden mapped fields can lack rendered link targets. | maybe-false | Missing targets are an explicit matrix case and the focus helper advances to the next invalid control; a real all-hidden rejection and its intended filtered-form treatment are required to settle the remaining edge. | defer |
| 1 | blind-hunter | BH-02: Unmapped/global rejection messages are stored but not shown in the mapped summary. | low | The canonical unmapped lifecycle reason and resolution remain visible and keyboard reachable; adding a second detailed presentation channel is disproportionate absent evidence that supplemental global copy is required. | reject |
| 1 | blind-hunter | BH-03: Numeric Fluent inputs do not identify the model validation field when their value expression targets a backing string. | medium | Fluent UI V5 exposes `ValidationFieldFor` specifically for split text/model binding; without it, model validation state is not associated with the numeric control. | patch |
| 1 | blind-hunter | BH-04: Validation anchors always cancel their native fragment action. | low | The JS failure path catches the error but cannot restore the already-prevented default action. | patch |
| 1 | blind-hunter | BH-05: Edited-origin capture can complete out of order or capture the warning action. | medium | Fire-and-forget imports can overlap, and `captureEditedOrigin` prefers the current active editable selector—which includes buttons—over the field named by the event. | bad_spec |
| 1 | blind-hunter | BH-06: Escape is handled only while Stay has focus. | medium | The key handler is attached to the Stay button, so Escape from Leave does not execute the required stay behavior. | patch |
| 1 | blind-hunter | BH-07: Abandonment warning IDs collide across guard instances. | medium | Title, description, and Stay IDs are fixed and focus uses a global ID lookup. | patch |
| 1 | blind-hunter | BH-08: Authorization-denial heading IDs collide across same-command renderer instances. | medium | The generated ID is type-derived without an instance suffix. | patch |
| 1 | blind-hunter | BH-09: Destructive confirmation has a pre-focus two-renderer modal race. | medium | An existing overlay-open intent is overwritten until focus enters the first dialog. | patch |
| 1 | blind-hunter | BH-10: Initial destructive focus interop failures escape the submit handler. | medium | The import/capture path has no JS, disconnect, or prerender catch and runs before admission/lifecycle dispatch. | patch |
| 1 | blind-hunter | BH-11: “View active command” silently fails when the active lifecycle is off-route or unmounted. | medium | The action ignores the Boolean result of its document-only lookup. | patch |
| 1 | blind-hunter | BH-12: The concurrency card can remain after the original command settles. | low | The card truthfully records why that attempt did not run; stale action-target risk is already captured by BH-11, and adding cross-form completion tracking is disproportionate here. | reject |
| 1 | blind-hunter | BH-13: Fields sharing a declared group are emitted as separate one-field groups. | medium | VR-01 requires one visible/programmatic group identity and preserved group order; repeated per-field `role=group` wrappers provide neither a shared group container nor a visible group label. | bad_spec |
| 1 | blind-hunter | BH-14: One-error summaries use plural grammar. | low | The localized template is always plural. | patch |
| 1 | blind-hunter | BH-15: The modal containment E2E does not test boundary wrapping. | medium | It tabs only between adjacent actions, matching VG-01. | patch |
| 1 | blind-hunter | BH-16: Two top-level test types violate a repository one-type-per-file rule. | false | No tracked repository instruction or analyzer establishes that rule, and the test file compiles cleanly. | reject |
| 1 | blind-hunter | BH-17: Synthesized rejection details and `Problem.RejectionDetails` can disagree. | low | The public exception exposes synthesized `Details` while retaining a payload whose optional detail member is null, creating a divergent metadata view for future consumers. | patch |
| 1 | blind-hunter | BH-18: Changed text files do not follow the repository CRLF policy. | low | `.gitattributes` and `.editorconfig` require CRLF, and file inspection confirms LF-only/mixed changed files. | patch |
| 2 | blind-hunter | BH2-01: Admission is acquired before authorization and held across the destructive dialog. | low | The native modal makes the page inert during confirmation, and admission-before-auth only changes which blocking message a denied operator sees while another command is active; the fix restructures the gate. | reject |
| 2 | blind-hunter | BH2-02: Re-pressing a form's own in-flight submit shows AM-20 and republishes the warning. | false | The matrix requires AM-20 for any later submit while a local command is active and the copy is accurate; `ICommandFeedbackPublisher` has no shipping subscriber, so repeated publication is inert. | reject |
| 2 | blind-hunter | BH2-03: Stale comments describe removed lifecycle gating and the old destructive-cancel catch. | low | Emitter comments still say the submit button is lifecycle-disabled and that cancel reaches ResetToIdle; a maintainer following them could reintroduce the AM-20 focus loss. | patch |
| 2 | blind-hunter | BH2-04: Presentation-time authorization denial moves focus on load and on every refresh. | medium | A 2026-09-29 Chromium run with two denied specimen renderers left focus on the Allowed renderer's denial heading at page load; the renderer sets `_authorizationFocusPending` on every refresh, contrary to FM-01 and AM-26's activation-attempt rule. | bad_spec |
| 2 | blind-hunter | BH2-05: Infrastructure authorization failures replace the whole form with no retry path. | medium | `SetAuthorizationWarning` sets `_authorizationDenied` for MissingService, MissingPolicy, StaleTenantContext, HandlerFailed, Canceled, and CatalogInconsistent, whose copy says "Please retry", so a submit-time transient failure hides the inputs until an unrelated allowed refresh. A persisting no-policy 403 is VR-04 fail-closed behavior. | bad_spec |
| 2 | blind-hunter | BH2-06: The AM-20 status region is created together with its text, and identical repeats change nothing. | medium | The `role=status` section mounts only when blocked, a pattern many screen readers do not announce, and a repeat attempt re-renders identical text; UX-AM-1 requires one polite channel announcing once per blocked attempt. | bad_spec |
| 2 | blind-hunter | BH2-07: `FcValidationSummary` mutates render state off the renderer dispatcher. | medium | The rejection and server-validation catches run after `ConfigureAwait(false)` awaits and call `ShowAndFocusAsync`/`Hide`, which rebuild `_entries` while a Fluxor-triggered render can concurrently run `OnParametersSet` → `RebuildEntries`. | patch |
| 2 | blind-hunter | BH2-08: Group members declared apart render out of summary order. | low | `EmitFields` hoists later group members to the first member's position while `BuildValidationFields` keeps declaration order, so summary order and the next-target fallback diverge from DOM order. | patch |
| 2 | blind-hunter | BH2-09: Group legends and field descriptions are not localized. | false | Both come from explicit adopter attributes and follow the same precedence as explicit `Display(Name)` labels, which the baseline already emits literally; shell copy keeps EN/FR parity. | reject |
| 2 | blind-hunter | BH2-10: Every keystroke triggers edited-origin JS interop round-trips. | low | The guard captures on every `OnFieldChanged` (import, invoke, dispose) with `Immediate` Fluent inputs; capturing only when the edited field changes keeps the latest-origin contract. | patch |
| 2 | blind-hunter | BH2-11: Generated editors misreport or omit `aria-invalid`. | medium | Numeric editors derive `aria-invalid` from the parse error only, so a Required, Range, or mapped error reads `false`, and no other editor emits it; the Fluent 5.0.0-rc.5 library has no `aria-invalid` handling, contrary to VR-01. | patch |
| 2 | blind-hunter | BH2-12: Dialog naming uses a cross-shadow idref, draft `aria-description`, and no retry. | low | `aria-label` supplies the name and the Chromium run verifies the accessible name and description; name computation ignores an unresolved idref, and no missing-host timing was observed. | reject |
| 2 | blind-hunter | BH2-13: The destructive dialog uses fixed shared IDs. | low | The single modal slot allows one dialog at a time; an overlap needs a reopen during a close animation, and instance IDs add plumbing. | reject |
| 2 | blind-hunter | BH2-14: Refused confirmations are silent, modal detection is broad, and the 5 s intent can expire early. | low | Refusal requires focus inside a modal, and a native modal makes the page inert; inline popovers are not dialogs (the `fluent-popover-b` host has no role); an un-upgraded `fluent-dialog` exists only before the library loads. | reject |
| 2 | blind-hunter | BH2-15: `captureOverlayOrigin(preserveExisting)` changed behavior for existing shell callers. | low | Tracker intents without `createdAt` are now overwritten unless a modal is open, resetting `moved`; restoring the previous preserve branch for `preserveExisting` callers is a direct correction. | patch |
| 2 | blind-hunter | BH2-16: Summary links add history entries, and the click-handler test is vacuous. | low | Native fragment navigation plus focus was the loop 1 ECH-10/BH-04 trade-off, and the Chromium run shows focus lands on the field; `HasAttribute("@onclick")` can never fail and should assert the rendered Blazor click handler. | patch |
| 2 | blind-hunter | BH2-17: Browser evidence is missing for mapped rejection and weak for nested-modal refusal. | low | A generated-form bUnit test covers mapped rejection with lifecycle identity, the host has no field-map rejection mode, and refusal is also covered by the JS reservation e2e and the bUnit fail-closed test. | reject |
| 2 | blind-hunter | BH2-18: The rejection e2e spawns its own Development host. | low | It is a serial, Chromium-only describe with one test, and it exists because the shared Test host cannot boot (pre-existing `MapFrontComposerMcp` gate). | reject |
| 2 | blind-hunter | BH2-19: `CommandAlreadyInProgress*` resources are orphaned and FR typography is mixed. | low | No code references the three keys in either culture after the AM-20 copy change; deleting them is direct. | patch |
| 2 | blind-hunter | BH2-20: The denial heading has two ID schemes. | false | The renderer heading (presentation-time trigger replacement) and the form heading (submit-time replacement) are different surfaces, and the ULID policy passed with the renderer GUID. | reject |
| 2 | blind-hunter | BH2-21: The spec, sprint status, and epic context disagree. | false | Step 5 syncs sprint status by design, and the epic-context wording change arrived in the auto-commit, outside shipped code. | reject |
| 2 | verification-gap | VG2-01: No CI workflow runs the four Story 13.3 Playwright specs. | medium | Pre-existing: these specs never ran in CI, and the shared Test host throws in `MapFrontComposerMcp` outside Development since `e5666650`; tracked with the 2026-09-27 Story 13.2 browser-lane entry. | defer |
| 2 | verification-gap | VG2-02: Server `CommandValidationException` form-level errors now render only through the summary, and nothing tests that path. | medium | Filed evidence: no generated-form test throws `CommandValidationException`, so removing the summary call leaves global errors rendered nowhere. | patch |
| 2 | verification-gap | VG2-03: Only a source token covers the dispatch-time `Forbidden` replacement. | medium | Filed evidence: `SetAuthorizationWarning` also emits the only asserted token. | patch |
| 2 | verification-gap | VG2-04: Form-level denial heading focus is never asserted. | medium | Filed evidence: only `-submit` focus requests are asserted. | patch |
| 2 | verification-gap | VG2-05: A same-form resubmit during its own command has no runtime test. | medium | Filed evidence: every runtime concurrency test uses two forms. | patch |
| 2 | verification-gap | VG2-06: Summary rebuild on validation-state change is untested. | medium | Filed evidence: every summary test shows the summary once and never changes messages. | patch |
| 2 | verification-gap | VG2-07: The changed `preserveExisting` overlay-origin mode has no test. | low | Same defect as BH2-15; the restoring patch adds a focused check. | patch |
| 2 | verification-gap | VG2-08: The CI a11y spec and the specimen styling target removed markup. | medium | `specimen-accessibility.spec.ts:166-172` samples `.fc-command-field-label` and the policy `fluent-message-bar`, neither of which renders any more, and `FrontComposerTypeSpecimen.razor.css:136-157` styles the same removed markup; the Code Map never listed these consumers. | bad_spec |
| 2 | verification-gap | VG2-09: `command-form-generation.spec.ts:76` becomes a strict-mode violation. | medium | Parse errors now render in the field error node and as the summary link "Initial Value: Invalid number format.", so the unscoped `getByText` matches twice. | patch |
| 2 | verification-gap | VG2-10: Stale unmapped rejection text reappears in a later client summary. | medium | The rejection catch stores unmapped messages in `_serverFormLevelErrors`, which is cleared only after validation passes, so the next client-invalid submit lists them under "Validation failed". | patch |
| 2 | verification-gap | VG2-11: Presentation-time denial takes focus on page load. | medium | Same root cause as BH2-04. | bad_spec |
| 2 | verification-gap | VG2-12: The abandonment guard adds JS round-trips per keystroke. | low | Same root cause as BH2-10. | patch |
| 2 | verification-gap | VG2-13: Zero-field inline commands hide the AM-20 outcome. | medium | Their form renders inside `display:none` (`CommandRendererEmitter.cs:898-906`), so the blocked card, its status region, and the `-submit` focus target cannot be perceived, and no shell subscriber shows the published warning. | bad_spec |
| 2 | edge-case-hunter | E2-01: The rejection's store clear wipes a parse error typed while the command is in flight. | low | It needs an invalid number typed during the in-flight window, followed by a rejection; the fix needs a separate parse-error store. | reject |
| 2 | edge-case-hunter | E2-02: A later client summary lists stale rejection text. | medium | Same root cause as VG2-10. | patch |
| 2 | edge-case-hunter | E2-03: An unchanged resubmit after a mapped rejection is blocked and relabeled as client validation. | low | `EditContext.Validate` counts every store, so mapped messages block an unchanged resubmit exactly as baseline server validation did; clearing them needs store-origin tracking. | reject |
| 2 | edge-case-hunter | E2-04: A server 403 permanently replaces a form that has no policy. | false | A fail-closed replacement that persists until the surface is reopened is VR-04 behavior, and no in-panel retry is owed for a server denial. | reject |
| 2 | edge-case-hunter | E2-05: Denial focus fires on initial load and on every authorization refresh. | medium | Same root cause as BH2-04. | bad_spec |
| 2 | edge-case-hunter | E2-06: A refused confirmation is silent, and dialog-hosted destructive commands never run. | low | Same as BH2-14; the intent excludes nested modals. | reject |
| 2 | edge-case-hunter | E2-07: An un-upgraded `fluent-dialog` keeps a leftover intent live forever. | low | Same as BH2-14. | reject |
| 2 | edge-case-hunter | E2-08: Summary descriptors can target fields hidden by `DerivableFieldsHidden`/`ShowFieldsOnly`. | maybe-false | carried ECH-04/BH-01: missing targets advance to the next invalid control; settling it still needs a real filtered-form rejection. | defer |
| 2 | edge-case-hunter | E2-09: A group declared on non-adjacent properties renders in a different order than the summary lists it. | low | Same root cause as BH2-08. | patch |
| 2 | edge-case-hunter | E2-10: Guid and TimeOnly fields bind to the string-only text editor. | medium | carried ECH-01: a pre-existing typed-adapter gap. | defer |
| 2 | edge-case-hunter | E2-11: A numeric field that fails Required or Range reads `aria-invalid="false"`. | medium | Same root cause as BH2-11. | patch |
| 2 | edge-case-hunter | E2-12: The first blocked outcome may go unannounced, and repeats are silent. | medium | Same root cause as BH2-06. | bad_spec |
| 2 | edge-case-hunter | E2-13: Clicking a summary link pushes a history entry. | low | Same as BH2-16. | patch |
| 2 | edge-case-hunter | E2-14: A null `DisplayLabel` reads "Command command status". | low | Only generated forms mount `FcLifecycleWrapper`, and they always pass `DisplayLabel`; the awkward fallback needs a direct adopter mount and a new resource. | reject |
| 2 | edge-case-hunter | E2-15: `labelDialog` has no retry when the host attaches late. | low | Same as BH2-12. | reject |
| 2 | edge-case-hunter | E2-16: Three `CommandAlreadyInProgress*` resources are orphaned. | low | Same as BH2-19. | patch |
| 2 | edge-case-hunter | E2-17: Numeric format errors are no longer announced as they are typed. | false | AM-18 suppresses individual error insertion before the complete summary; a per-keystroke `role=alert` would add a second speech path. | reject |
| 2 | edge-case-hunter | E2-18: Authorization denial offers no keyboard recovery. | false | VR-04 recovery is returning to the prior surface, or re-authenticating when offered; shell navigation, breadcrumbs, and browser Back stay keyboard reachable. BH2-05 covers the transient-failure case. | reject |
| 2 | edge-case-hunter | E2-19: Admission runs before authorization and is held during confirmation. | low | Same as BH2-01. | reject |
| 3 | blind-hunter | BH3-01: The replacement-focus capture is awaited before `Ready=false` and before the refresh-sequence stamp. | medium | In both emitters `CaptureFocusBeforeReplacementAsync` runs first, so a stale "allowed" trigger stays interactive for a JS round trip, sequence stamps follow JS completion order, and the Pass-3 comment is now false; set Ready and stamp first. | patch |
| 3 | blind-hunter | BH3-02: The replacement check does not confirm that the replaced surface held focus. | low | The capture records any focused element on the page, so a shell re-render that removes an unrelated focused control lets the denial heading take focus; focus had already fallen to `body`, so the outcome is a usable target, and scoping needs a root per renderer mode. | reject |
| 3 | blind-hunter | BH3-03: The form-level background-denial focus path has only source-token coverage. | medium | Same gap as VG3-02. | patch |
| 3 | blind-hunter | BH3-04: Focus falls to `body` when the focused active-lifecycle heading unmounts on settle. | medium | The heading renders only while Submitting, Acknowledged, or Syncing; the e2e re-focuses the attempted submit by hand after confirmation, so the loss is hidden, and the spec named no settle destination. | bad_spec |
| 3 | blind-hunter | BH3-05: A blocked attempt always moves focus to the submit button. | medium | `FcCommandBlockedOutcome` focuses `AttemptedControlId` (`-submit`) even when Enter was pressed in a field, pulling focus off the attempted control, contrary to FM-09; keep focus when it is still inside the form. | patch |
| 3 | blind-hunter | BH3-06: Zero-field inline commands still hide their scope-unavailable and submit-time denial outcomes. | medium | Only AM-20 moved to the renderer; the baseline already rendered every other zero-field form outcome inside the `display:none` form. | defer |
| 3 | blind-hunter | BH3-07: "View active command" counts a hidden lifecycle as available. | medium | `hasActiveLifecycle`/`focusActiveLifecycle` check `isConnected` but not rendering, so a lifecycle inside a hidden zero-field form or closed popover offers an action that only fails when clicked; add the `getClientRects()` check. | patch |
| 3 | blind-hunter | BH3-08: Field ARIA relationships never reach the focusable inner control. | high | A Chromium accessibility-tree probe on `/commands/Counter/ConfigureCounterCommand` after a failed submit found every textbox at `invalid=false` with no description, although Name and Initial Value were invalid. Host `aria-describedby`/`aria-invalid` do not cross the `delegatesFocus` shadow root, so VR-01 fails for assistive technology. | bad_spec |
| 3 | blind-hunter | BH3-09: The new field markup is unstyled for adopters, and errors duplicate Fluent's own message. | medium | The same probe showed "Invalid number format." twice in one field (Fluent's message plus the custom error node); raw `fieldset`, description, and error nodes have Shell styling nowhere. | bad_spec |
| 3 | blind-hunter | BH3-10: `Pending` joined the infrastructure-failure reasons without a test. | low | Keeping the form with retry copy for a still-pending check at submit fits the "only a genuine denial replaces the form" rule, but `TransientAuthorizationFailures` omits it. | patch |
| 3 | blind-hunter | BH3-11: The specimen task is ticked while its visual baselines are stale. | low | The Implementation Notes record the regeneration blocker: the Test host cannot boot because of the pre-existing `MapFrontComposerMcp` gate. | defer |
| 3 | blind-hunter | BH3-12: Server validation summaries carry the client-validation kind. | low | Only a data attribute differs, and a new enum member adds public surface for a consumer that does not exist yet. | reject |
| 3 | blind-hunter | BH3-13: The new resx entries have no translator comments. | low | The removed entries carried "Do not mention queuing or retry", and the new `{0}` templates are undocumented; adding comments is direct. | patch |
| 3 | blind-hunter | BH3-14: The guard's captured field name is never cleared when the EditContext changes. | low | A swapped EditContext skips recapture of the same field name; clear it in `OnParametersSet` when the context changes (a replaced editor already falls back to the form heading by design). | patch |
| 3 | blind-hunter | BH3-15: Two comments are stale. | low | `ServerValidationApplicator` still says there is no result type, and the guard's D9 comment ignores the added scripted Stay focus. | patch |
| 3 | blind-hunter | BH3-16: Scripted focus is marked available without proof. | low | Link failures still restore the native fallback through `FocusFieldAsync`'s catch; stronger proof would need a new return channel. | reject |
| 3 | blind-hunter | BH3-17: Summary links repeat the field label. | low | Default DataAnnotations messages already name the field, so every entry reads "Record Id: The Record Id field is required."; omit the prefix when the message already contains the label. | patch |
| 3 | verification-gap | VG3-01: No browser test executes `captureFocusBeforeReplacement`/`focusReplacementHeading`. | medium | Filed evidence: bUnit mocks both to `true` and no e2e imports them. | patch |
| 3 | verification-gap | VG3-02: A standalone form's background-replacement focus has no runtime test. | medium | Filed evidence: only the renderer variant is rendered through a background refresh. | patch |
| 3 | verification-gap | VG3-03: Clearing an earlier AM-20 on an admitted attempt is never asserted. | medium | Filed evidence: the resubmit tests check dispatch counts only. | patch |
| 3 | verification-gap | VG3-04: The VG2-10 runtime test cannot observe the reset it names. | medium | Filed evidence: an unmapped rejection never populates `_serverFormLevelErrors`; a server-validation global error followed by a client-invalid resubmit is needed. | patch |
| 3 | verification-gap | VG3-05: Default destructive-confirmation copy is checked only as emitted source. | medium | Filed evidence: both runtime destructive fixtures supply explicit copy. | patch |
| 3 | verification-gap | VG3-06: The in-flight authorization window reopened. | medium | Same root cause as BH3-01. | patch |
| 3 | edge-case-hunter | E3-01: A faulted presentation task would poison every later blocked attempt. | false | Every interop helper catches its failure types and `IsCurrent` short-circuits after disposal, so no path faults the chained task. | reject |
| 3 | edge-case-hunter | E3-02: Focus drops to `body` after "View active command" when the command settles. | medium | Same root cause as BH3-04. | bad_spec |
| 3 | edge-case-hunter | E3-03: `labelDialog` has no retry when the host attaches late. | low | carried BH2-12/E2-15: `aria-label` names the dialog and no late-host timing was observed. | reject |
| 3 | edge-case-hunter | E3-04: A no-policy server 403 replaces the form for good. | false | carried E2-04: persistent fail-closed replacement is VR-04 behavior. | reject |
| 3 | edge-case-hunter | E3-05: Summary descriptors can target fields hidden by `DerivableFieldsHidden`/`ShowFieldsOnly`. | maybe-false | carried ECH-04/BH-01/E2-08. | defer |
| 3 | edge-case-hunter | E3-06: A rejection mapped only to fields hidden on this surface still counts as mapped. | maybe-false | `HasMappedFieldErrors` counts allowlisted fields whether or not they render, so AM-14 is suppressed while the summary links to an absent control; if real it would be medium. Settle it by rejecting a hidden field through a real `ShowFieldsOnly` popover. | defer |
| 3 | edge-case-hunter | E3-07: The admission reservation spans authorization and the destructive dialog. | low | carried BH2-01/E2-19: the native modal makes the page inert, and the message ordering is otherwise harmless. | reject |
| 3 | edge-case-hunter | E3-08: A refused destructive confirmation is silent. | low | carried BH2-14/E2-06. | reject |
| 3 | edge-case-hunter | E3-09: The guard's origin capture goes stale after an EditContext swap. | low | Same root cause as BH3-14. | patch |
| 3 | edge-case-hunter | E3-10: A JS interop timeout (`TaskCanceledException`) can escape `OnAfterRenderAsync`. | low | The Shell convention catches `JSDisconnectedException`/`JSException`/`InvalidOperationException` in 14 files and `TaskCanceledException` in none of the pre-existing ones; it needs a 60 s client stall and a codebase-wide policy change. | reject |
| 3 | edge-case-hunter | E3-11: Enter in a field moves focus to the submit button on a blocked attempt. | medium | Same root cause as BH3-05. | patch |
| 3 | edge-case-hunter | E3-12: Submit stays enabled on a stale allowed state during the capture round trip. | medium | Same root cause as BH3-01. | patch |
| 3 | edge-case-hunter | E3-13: An empty 400 validation payload produces no summary. | low | The baseline also rendered nothing for an empty server-validation payload, and adding a generic message adds a branch for a malformed server response. | reject |
| 3 | edge-case-hunter | E3-14: `CommandTargetGeneratedFormTests.cs:863` asserts that retired copy is absent. | low | "Command already in progress" no longer exists, so the assertion cannot fail; assert that the current AM-20 copy is absent instead. | patch |
| 3 | edge-case-hunter | E3-15: The FullPage denial card lost its max-width wrapper. | low | The removed FullPage branch wrapped the card in `max-width: opts.FullPageFormMaxWidth`; restoring the wrapper is direct. | patch |
| 3 | edge-case-hunter | E3-16: The renderer's presentation gate still replaces the form on a transient authorization failure. | medium | `AuthorizationTriggerDisabled()` is `!Ready || !Allowed`, and the baseline already replaced FullPage/CompactInline forms on any non-allowed presentation decision; the loop 2 rule covered the form's own submit path. | defer |
| 3 | edge-case-hunter | E3-17: "View active command" is offered for a hidden lifecycle. | medium | Same root cause as BH3-07. | patch |
| 4 | blind-hunter | BH4-01: Nullable `bool`/enum command properties generate code that does not compile. | medium | carried ECH-02/ECH-03: the transform maps `Boolean`/`Enum` (nullable included) to Switch/Select, and the emitted `Expression<Func<bool>>`/`Func<TEnum>` casts over a nullable member raise CS0266; loop 1 routed this defect to defer. | defer |
| 4 | blind-hunter | BH4-02: A blocked zero-field inline attempt can move focus to another row's trigger. | medium | `_triggerButtonId` is `fc-trigger-<FQN>` for every instance and sits outside the form, so `focusAttemptedControl` focuses the first `getElementById` match instead of the pressed trigger. | patch |
| 4 | blind-hunter | BH4-03: Summary link text can lose its field name. | medium | `LinkText` uses a substring test, so the label "Id" matches "Invalid number format." and the link no longer names its field; match the label on word boundaries. | patch |
| 4 | blind-hunter | BH4-04: The scope-unavailable blocked path always pulls focus to the submit button. | low | `PresentBlockedSubmissionAsync` calls `focusElementById(-submit)` even when Enter came from a field, unlike the AM-20 path's `focusAttemptedControl` rule; switching the call is direct. | patch |
| 4 | blind-hunter | BH4-05: `FcValidationFieldDescriptor.ErrorId` is unused, and the JS comment says descriptors name it. | low | Links target editor ids; the comment in `fc-focus.js` misstates which id descriptors carry, so correct the comment. | patch |
| 4 | blind-hunter | BH4-06: The unmapped rejection branch and rejection titles keep English-only labels and copy. | low | Pre-existing Story 5 rejection labels, fallback text, and `ResolveRejectionTitle` predate this story; the new mapped card uses localized labels. | defer |
| 4 | blind-hunter | BH4-07: English numeric parse errors appear under French summaries. | low | The hard-coded "Invalid number format." predates this story; the summary now also lists it. | defer |
| 4 | blind-hunter | BH4-08: The French destructive title uses a regular space before "?". | low | `FcShellResources.fr.resx` uses a no-break space before double punctuation elsewhere, so the "?" can wrap alone in the dialog title. | patch |
| 4 | blind-hunter | BH4-09: The denial, mapped-rejection, and abandonment cards use raw `section`/`h2`/`p` and custom flex. | low | These are text and layout wrappers inside `FluentCard`, not controls, and they inherit theme typography; converting every surface is more than a direct correction. | reject |
| 4 | blind-hunter | BH4-10: Only one component catches `TaskCanceledException` from JS interop. | low | carried E3-10: a codebase-wide policy change for a 60 s client stall. | reject |
| 4 | blind-hunter | BH4-11: The accessibility-tree evidence is duplicated, Chromium-gated by `if`, and limited to text inputs. | low | Helper duplication and the `if` gate are test hygiene; non-text editor coverage is taken by VG4-10. | reject |
| 4 | blind-hunter | BH4-12: The browser focus proofs use synthetic DOM fixtures. | low | The Counter host cannot revoke a policy or hide a lifecycle mid-session, and bUnit covers the generated wiring. | reject |
| 4 | blind-hunter | BH4-13: The Acceptance Criteria were not updated for the bad_spec loops. | false | Fixing it means editing this build's spec, which triage rejects. | reject |
| 4 | blind-hunter | BH4-14: A loop 2 KEEP bullet reads as truncated. | false | "with the Web using" names the `Microsoft.AspNetCore.Components.Web` using, and fixing it would mean editing this build's spec. | reject |
| 4 | blind-hunter | BH4-15: The specimen task is ticked although the visual baselines are stale. | low | carried BH3-11: regeneration is blocked by the pre-existing Test-host `MapFrontComposerMcp` failure and is recorded in the Implementation Notes. | defer |
| 4 | blind-hunter | BH4-16: Misleading names: `EmitAuthorizationMessageBar` emits a card, and `DefaultCopyDestructiveCommand.cs` declares `ArchiveWidgetCommand`. | low | Renaming the private emitter method and the test file is direct; marking the public `ResultUnmappedMessages` `[Obsolete]` would trip the diagnostic deprecation policy and is out of scope. | patch |
| 4 | verification-gap | VG4-01: No test covers the active-lifecycle heading markup that `fc-focus.js` selects. | medium | Filed evidence: every C# test mocks `hasActiveLifecycle`/`focusActiveLifecycle`, and no Lifecycle test asserts the heading. | patch |
| 4 | verification-gap | VG4-02: The numeric parse error reaching the EditContext has no runtime test. | medium | Filed evidence: no Shell test mentions "Invalid number format" or `ParseError`. | patch |
| 4 | verification-gap | VG4-03: No runtime test covers a mapped rejection's form-level messages. | medium | Filed evidence: `MappedRejectingCommandService` has empty `GlobalErrors`. | patch |
| 4 | verification-gap | VG4-04: The summary's unlinked EditContext messages are never exercised. | medium | Filed evidence: every summary test uses messages on fields that have descriptors. | patch |
| 4 | verification-gap | VG4-05: No test withdraws the zero-field inline blocked outcome. | medium | Filed evidence: the zero-field test covers only the present path. | patch |
| 4 | verification-gap | VG4-06: Abandonment Escape is tested by calling the handler through reflection. | medium | Filed evidence: moving `@onkeydown` back to Stay keeps the test green. | patch |
| 4 | verification-gap | VG4-07: Stay focus on a re-shown warning is untested. | medium | Filed evidence: no guard test asserts `focusElementById` with the Stay id. | patch |
| 4 | verification-gap | VG4-08: Per-instance form and denial-heading ids are never compared across two instances. | medium | Filed evidence: only the guard has a uniqueness test. | patch |
| 4 | verification-gap | VG4-09: No test covers hiding a group fieldset when all its members are hidden. | low | Filed disposition: only an all-derivable group reaches it, and no sample has one. | defer |
| 4 | verification-gap | VG4-10: Switch, date-picker, and enum-select editors have no rendered Story 13.3 field-contract coverage. | medium | Filed evidence: no Shell fixture has a bool, date, or enum property. | patch |
| 4 | verification-gap | VG4-11: The loop 3 JS behaviors are verified only by Playwright specs that CI never runs. | medium | carried VG2-01: blocked by the pre-existing Test-host `MapFrontComposerMcp` failure. | defer |
| 4 | verification-gap | VG4-12: `LinkText` drops the label on substring matches. | medium | Same root cause as BH4-03. | patch |
| 4 | edge-case-hunter | E4-01: A no-policy dispatch 403 replaces the form for good. | false | carried E2-04: fail-closed VR-04 replacement. | reject |
| 4 | edge-case-hunter | E4-02: A parse error typed while a command is in flight is erased by the rejection store clear. | low | carried E2-01. | reject |
| 4 | edge-case-hunter | E4-03: An unchanged resubmit after a mapped rejection is blocked. | low | carried E2-03. | reject |
| 4 | edge-case-hunter | E4-04: Summary descriptors can target hidden fields. | maybe-false | carried ECH-04/BH-01/E2-08. | defer |
| 4 | edge-case-hunter | E4-05: An empty 400 validation payload gives no feedback. | low | carried E3-13. | reject |
| 4 | edge-case-hunter | E4-06: A cancelled link click does nothing when `focusValidationTarget` finds no target. | low | The function already falls back to the next linked, first linked, and first invalid control, and it returns false only when nothing focusable exists, where the native fragment would fail too. | reject |
| 4 | edge-case-hunter | E4-07: `LinkText` matches a label inside an unrelated word. | medium | Same root cause as BH4-03. | patch |
| 4 | edge-case-hunter | E4-08: Numeric keystrokes alternate the guard's captured field name. | low | A split binding's `FieldChanged(form, "_XString")` alternates with `NotifyClientFieldChanged("X")`, so each keystroke re-imports the module; ignore identifiers whose model is not the EditContext model. | patch |
| 4 | edge-case-hunter | E4-09: A JS interop timeout can fault the circuit. | low | carried E3-10. | reject |
| 4 | edge-case-hunter | E4-10: The blocked outcome stays visible after the blocking command settles. | low | carried BH-12. | reject |
| 4 | edge-case-hunter | E4-11: A superseded renderer refresh can leave `_authorizationReplacementArmed` set. | low | The persistent flag survives the stale-sequence return, so a later unrelated background denial can use it; a per-refresh local, as the form uses, is direct. | patch |
| 4 | edge-case-hunter | E4-12: A refused destructive confirmation is silent. | low | carried BH2-14/E3-08. | reject |
| 4 | edge-case-hunter | E4-13: A non-modal open dialog keeps overlay intents live. | low | carried BH2-14/E2-07. | reject |
| 4 | edge-case-hunter | E4-14: The settle watcher ignores a focusout with a null `relatedTarget`. | low | Finishing on every focusout could stop the watcher before the heading's own unmount, which the Chromium settle test relies on; the scenario (clicking a blank area, then the command settles) is narrow. | reject |
| 4 | edge-case-hunter | E4-15: Stay with no captured origin can jump to a page `h1` outside an inline form. | low | The abandonment guard wraps full-page command forms, whose marked form heading is the FM-11 fallback. | reject |
| 4 | edge-case-hunter | E4-16: Duplicate normalized keys overcount `MappedFieldCount`, and blank messages count as mapped. | low | Only `> 0` is consumed, and blank server messages are a malformed payload. | reject |
| 4 | edge-case-hunter | E4-17: Unmapped 409 global messages are never shown. | low | carried ECH-07/BH-02: the canonical lifecycle reason and resolution remain visible. | reject |
| 4 | edge-case-hunter | E4-18: A background denial can clear a pending operator-submit denial focus. | low | `SetAuthorizationWarning(operatorActivation: false)` overwrites `_authorizationFocusPending` before the submit denial renders; OR-assigning keeps the operator's focus. | patch |
| 4 | edge-case-hunter | E4-19: The renderer presentation gate replaces the form on transient failures. | medium | carried E3-16. | defer |
| 4 | edge-case-hunter | E4-20: "View active command" stays offered after the lifecycle settles. | low | carried BH-12: it is withdrawn, with focus returned, on activation. | reject |

## Design Notes

Treat focus as an outcome owned by the initiating control and operation. Reuse the existing overlay-origin helper for modal return, add an edited-control variant for the in-flow guard, and carry an explicit mapped-rejection flag so Story 13.4 can suppress AM-14 without guessing from rendered text.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release -m:1` — generator output and snapshots compile cleanly.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1` — shell and generated integration fixtures compile cleanly.
- Run the focused emitter, validation, form, lifecycle, authorization, admission, resource, Fluent-conformance, and analyzer-governance test classes from their built xUnit v3 assemblies — all pass.
- `npm --prefix tests/e2e run typecheck`, the Story 13.3 Chromium specs in the Code Map, and `npm --prefix tests/e2e run test:a11y` against the Counter host — browser focus, containment, input, dispatch, contrast, and visual assertions pass, including Chromium accessibility-tree assertions that an invalid generated editor exposes `invalid` and its error as its description, and that each error is visible exactly once. The Test-environment host throws in `MapFrontComposerMcp` (pre-existing since `e5666650`). If it cannot boot, record that blocker and run against a Development specimen host without editing tracked host code, reporting any failures that environment causes.
