---
title: 'Story 13.3: Preserve Focus and Input Through Command Safety Outcomes'
type: 'feature'
created: '2026-09-28'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
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

- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`, `Transforms/CommandFormTransform.cs`, and form models — emit Fluent editable inputs, declared grouping/description metadata, stable validation targets, focused summaries, mapped-rejection state, AM-20, and active-lifecycle focus without changing submit/auth/admission ordering.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` — connect destructive-dialog origin/return focus and authorization-denied heading behavior while retaining pre-dialog validation and duplicate-dialog prevention.
- `src/Hexalith.FrontComposer.Shell/Components/Forms/` and `wwwroot/js/fc-focus.js` — provide reusable validation-summary focus/link behavior, accessible destructive confirmation, and edited-control capture/return for the non-modal abandonment guard.
- `src/Hexalith.FrontComposer.Contracts/Communication/`, `Shell/Infrastructure/EventStore/EventStoreResponseClassifier.cs`, and `Shell/Services/Validation/ServerValidationApplicator.cs` — preserve support-safe field maps on rejected outcomes and distinguish mapped from unmapped recovery without weakening the allowlist.
- `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor{,.cs}` and pending-command admission types — expose a focusable active lifecycle/recovery target and mapped-rejection announcement suppression while preserving the authoritative operation identity.
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources*.resx` — localize AM-18/19/20, summaries, recovery actions, confirmation, abandonment, and denial copy with EN/FR parity.
- `tests/Hexalith.FrontComposer.{SourceTools,Shell}.Tests/` and `tests/e2e/specs/{destructive-command-confirmation,form-abandonment-guard,one-at-a-time-execution-policy,policy-gated-command-authorization}.spec.ts` — pin generated markup, focus/speech counts, value preservation, lifecycle identity, no-dispatch outcomes, and browser containment/return behavior.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`, its transform/models, and validation/rejection runtime files in the Code Map — emit accessible Fluent controls, complete linked summaries, deterministic fallbacks, and safe mapped-rejection data while preserving values and lifecycle identity.
- [ ] `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`, `src/Hexalith.FrontComposer.Shell/Components/Forms/`, lifecycle components, and `wwwroot/js/fc-focus.js` — add names/descriptions, origin return, modal-only containment, silent Stay/Escape behavior, denied-heading focus, and recovery actions.
- [ ] `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`, pending-command admission types, and `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources*.resx` — retain attempted-submit focus, publish exact AM-20 once, offer active-lifecycle focus, and keep the original operation as the only advancing lifecycle.
- [ ] `tests/Hexalith.FrontComposer.{SourceTools,Shell}.Tests/` and the four Story 13.3 files under `tests/e2e/specs/` — cover every matrix fallback and prove one speech path, one dispatch, input preservation, and redaction.

**Acceptance Criteria:**
- Given client or safely mapped server errors, when the outcome renders, then one non-live linked summary is focused after insertion, preserves values and declared relationships, and mapped rejection remains `Rejected` without AM-14 duplication.
- Given an unmapped rejection or authorization denial, when it renders, then no field error is invented, support-safe recovery remains keyboard reachable, and focus/announcement uses only its canonical path.
- Given confirmation, abandonment, or a blocked second submit, when the operator cancels, stays, leaves, confirms, or views the active command, then focus and dispatch follow the matrix exactly and no implicit or duplicate action occurs.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Treat focus as an outcome owned by the initiating control and operation. Reuse the existing overlay-origin helper for modal return, add an edited-control variant for the in-flow guard, and carry an explicit mapped-rejection flag so Story 13.4 can suppress AM-14 without guessing from rendered text.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release -m:1` — generator output and snapshots compile cleanly.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1` — shell and generated integration fixtures compile cleanly.
- Run the focused emitter, validation, form, lifecycle, authorization, admission, resource, Fluent-conformance, and analyzer-governance test classes from their built xUnit v3 assemblies — all pass.
- `npm --prefix tests/e2e run typecheck` and the four Story 13.3 Chromium specs against the Counter host — browser focus, containment, input, and dispatch assertions pass.
