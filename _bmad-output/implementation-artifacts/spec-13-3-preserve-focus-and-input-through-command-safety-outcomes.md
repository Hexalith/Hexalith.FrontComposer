---
title: 'Story 13.3: Preserve Focus and Input Through Command Safety Outcomes'
type: 'feature'
created: '2026-09-28'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 1
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

- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`, `Transforms/CommandFormTransform.cs`, and form models — emit Fluent editable inputs, one shared visible/programmatic container per declared field group in declared group/field order, Fluent-supported model validation association for split text bindings, stable validation targets, focused summaries, mapped-rejection state, and AM-20. Every submit event, including an invalid one, must check the active lifecycle/admission outcome before validation can move focus or publish feedback.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` — connect destructive-dialog origin/return focus and authorization-denied heading behavior while retaining pre-dialog validation and duplicate-dialog prevention.
- `src/Hexalith.FrontComposer.Shell/Components/Forms/` and `wwwroot/js/fc-focus.js` — provide reusable validation-summary focus/link behavior, accessible destructive confirmation, and sequenced field-bound edited-control capture/return for the non-modal abandonment guard. Origin capture must ignore warning-action focus and stale completions from earlier field edits.
- `src/Hexalith.FrontComposer.Contracts/Communication/`, `Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs`, and `Shell/Services/Validation/ServerValidationApplicator.cs` — preserve support-safe field maps on rejected outcomes and distinguish mapped from unmapped recovery without weakening the allowlist.
- `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor{,.cs}` and pending-command admission types — expose a focusable active lifecycle/recovery target and mapped-rejection announcement suppression while preserving the authoritative operation identity.
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources*.resx` — localize AM-18/19/20, summaries, recovery actions, confirmation, abandonment, and denial copy with EN/FR parity.
- `tests/Hexalith.FrontComposer.{SourceTools,Shell}.Tests/` and `tests/e2e/specs/{destructive-command-confirmation,form-abandonment-guard,one-at-a-time-execution-policy,policy-gated-command-authorization}.spec.ts` — pin generated markup, focus/speech counts, value preservation, lifecycle identity, no-dispatch outcomes, and browser containment/return behavior.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`, its transform/models, and validation/rejection runtime files in the Code Map — emit accessible Fluent controls, complete linked summaries, deterministic fallbacks, one container per declared field group, correct model-field validation association, and safe mapped-rejection data while preserving values and lifecycle identity.
- [ ] `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`, `src/Hexalith.FrontComposer.Shell/Components/Forms/`, lifecycle components, and `wwwroot/js/fc-focus.js` — add names/descriptions, ordered field-bound origin return, modal-only containment, silent Stay/Escape behavior, denied-heading focus, and recovery actions.
- [ ] `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`, pending-command admission types, and `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources*.resx` — check active-command blocking before both valid and invalid submit handling, retain attempted-submit focus, publish exact AM-20 once, offer only a functional active-lifecycle action, and keep the original operation as the only advancing lifecycle.
- [ ] `tests/Hexalith.FrontComposer.{SourceTools,Shell}.Tests/` and the four Story 13.3 files under `tests/e2e/specs/` — cover every matrix fallback, compiled/rendered field-group semantics, multi-field edited-origin ordering, real browser focus destinations, modal boundary wrapping/nested-modal refusal, and prove one speech path, one dispatch, input preservation, and redaction.

**Acceptance Criteria:**
- Given client or safely mapped server errors, when the outcome renders, then one non-live linked summary is focused after insertion, preserves values and declared relationships, and mapped rejection remains `Rejected` without AM-14 duplication.
- Given an unmapped rejection or authorization denial, when it renders, then no field error is invented, support-safe recovery remains keyboard reachable, and focus/announcement uses only its canonical path.
- Given confirmation, abandonment, or a blocked second submit, when the operator cancels, stays, leaves, confirms, or views the active command, then focus and dispatch follow the matrix exactly and no implicit or duplicate action occurs.

## Implementation Notes

**Review loop 1 KEEP instructions:** Preserve the working non-live linked validation summary and its missing-target/summary fallbacks; mapped-rejection lifecycle identity and AM-14 suppression; authorization-denial replacement/focus; destructive confirmation naming, Cancel-first behavior, origin/h1 return, and single dispatch; silent in-flow abandonment warning and explicit Leave; one polite atomic AM-20 region with attempted-submit focus; support-safe localized EN/FR copy; and the passing focused unit, integration, typecheck, and Story 13.3 Chromium verification structure. Re-derive these behaviors while correcting the loop-triggering contracts and all accepted patch findings in the triage log.

## Spec Change Log

- **Review loop 1 (2026-09-28):** ECH-05 showed invalid submits could bypass active-command blocking, BH-05 showed edited-origin capture could be overwritten by warning focus or stale interop completion, and BH-13 showed declared groups were emitted as repeated one-field groups. The Code Map and execution tasks now require pre-validation concurrency gating, sequenced field-bound origin capture, and one shared visible/programmatic container per declared group, plus explicit rendered/browser evidence. This avoids the known-bad states of validation focus replacing AM-20, Stay restoring to the warning action or an older field, and repeated labels without a real group. KEEP the complete validation/rejection, denial, confirmation, abandonment, concurrency, localization, and verification behavior listed in Implementation Notes.

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

## Design Notes

Treat focus as an outcome owned by the initiating control and operation. Reuse the existing overlay-origin helper for modal return, add an edited-control variant for the in-flow guard, and carry an explicit mapped-rejection flag so Story 13.4 can suppress AM-14 without guessing from rendered text.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release -m:1` — generator output and snapshots compile cleanly.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1` — shell and generated integration fixtures compile cleanly.
- Run the focused emitter, validation, form, lifecycle, authorization, admission, resource, Fluent-conformance, and analyzer-governance test classes from their built xUnit v3 assemblies — all pass.
- `npm --prefix tests/e2e run typecheck` and the four Story 13.3 Chromium specs against the Counter host — browser focus, containment, input, and dispatch assertions pass.
