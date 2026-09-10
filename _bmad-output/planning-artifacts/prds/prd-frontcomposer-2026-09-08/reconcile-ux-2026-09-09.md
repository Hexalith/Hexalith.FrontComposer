# UX Reconciliation Extract — 2026-09-09

## Verdict

**PRD update required; no new product scope is implied.** The canonical PRD already carries the approved Module/Tab IA, lifecycle vocabulary, Fluent/FrontComposer policy, WCAG 2.2 AA target, and all six source journeys. The 2026-09-09 UX validation nevertheless exposes four high-impact product-contract gaps: deterministic announcements, accessible form errors, navigation/focus behavior, and complete state behavior. Apply these by amending existing stable IDs; do not renumber FR/NFR/UJ/SM identifiers. The remaining findings are UX-source or addendum corrections rather than new PRD requirements.

Source validation verdict: 0 critical, 8 high, 11 medium, 1 low; the three-file UX contract is visually coherent but thin/broken as a source-extractable implementation contract.

## Product-Level Deltas

| Priority | Delta to carry into the PRD | Existing IDs to amend | Done-ness / evidence consequence |
| --- | --- | --- | --- |
| High | **Deterministic state announcements.** Loading, stale, reconnecting/fallback, command progress/terminal states, blocked second submit, and fresh-row appearance must have a defined announcement outcome. Announce each meaningful transition once; deduplicate by operation/entity and state; coalesce rapid intermediate progress; never repeatedly announce polling/retry ticks; fresh-row expiry is silent. Progress and non-urgent outcomes use a polite status channel; a submit-blocking validation/rejection outcome uses the focused error summary/alert path. | FR-11, FR-12, FR-13, FR-15, FR-16; NFR-3, NFR-8, NFR-11; UJ-2, UJ-3; SM-6, SM-10 | Add e2e/bUnit evidence for event-to-announcement behavior, dedupe/coalescing, terminal announcements, blocked-submit feedback, and silent expiry. Do not leave SM-6 fully “met” until these paths are covered. |
| High | **Accessible validation and rejection.** Generated forms must associate field errors with their Fluent inputs, expose a summary linked to invalid controls, focus the summary after a failed submit, preserve useful input, and distinguish client validation from asynchronous server rejection. Server rejection remains a lifecycle outcome and must not be recast as field validation unless a safe field mapping exists. | FR-14, FR-15, FR-16, FR-22; NFR-3, NFR-6, NFR-11; UJ-3, UJ-6; SM-5, SM-6 | Test first-invalid navigation, summary-to-field links, input preservation, client/server distinction, support-safe copy, and keyboard-only recovery. |
| High | **Deterministic route, tab, palette, and dialog focus.** A successful client-side route activation focuses the route-level `h1`; keyboard tab selection retains focus on the active tab while the associated, labelled tabpanel changes; palette activation that navigates lands at the route heading; closing a palette/dialog returns focus to its invoker; failed navigation retains a usable focus location and announces failure. | FR-8, FR-10; NFR-3, NFR-11; UJ-2, UJ-3; SM-6 | Cover direct/deep-link navigation, tab changes, palette activation, close/cancel, and failed navigation with keyboard and screen-reader assertions. |
| High | **Complete, testable state treatments.** Every FR-11 and FR-15 state, plus blocked second submit and no-tenant/no-access outcomes, needs entry evidence, user-visible meaning, permitted actions, recovery/timeout, announcement, and terminal/non-terminal classification. This closes the current omissions around Submitting, Syncing, IdempotentConfirmed, NeedsReview, Warning, Degraded, and the blocked submit. | FR-11, FR-12, FR-15, FR-16, FR-30; NFR-3, NFR-8; UJ-2, UJ-3; SM-8, SM-10 | Use a state-by-surface acceptance matrix. Keep the detailed matrix outside the PRD, but make the requirement and evidence obligation explicit in these IDs. |
| Medium | **Measurable responsive accessibility and non-color state.** Generated shell/pages must preserve meaning and operation at 320 CSS-pixel reflow / 400% zoom, resilient text spacing, minimum WCAG 2.2 AA target sizing (subject to the standard's exceptions), and unobscured focus. Reduced-motion and forced-colors modes must retain navigation-current, focus, status, stale/reconnect, lifecycle, and fresh-row meaning without animation or color alone. | FR-8, FR-10, FR-11, FR-13; NFR-3, NFR-4, NFR-11; UJ-2; SM-6 | Add light/dark/forced-colors, reduced-motion, reflow/zoom, text-spacing, target-size, and focus-not-obscured coverage for changed UI surfaces. |
| Medium | **Component/document contract integrity.** Published and planning docs must use exact public FrontComposer identifiers or Fluent APIs that resolve against the selected catalog pin. `FcPageToolbar` is the product contract; its internal Fluent composition is implementation-owned unless deliberately made public. Journey supplements must cover each applicable UJ-1…UJ-6 or state why it is out of scope. | FR-23; NFR-4, NFR-11; UJ-5, UJ-6; SM-3, SM-6 | Validate documented component names against the pinned API and check UJ coverage during UX-document validation. This is documentation governance, not a request for new UI components. |

## Existing PRD Coverage That Should Be Preserved

- UJ-1…UJ-6 already preserve the six source journeys; the validation finding is missing traceability in the experience supplement, not a missing PRD journey.
- FR-8/FR-9/FR-10 already hold the approved shell, shortcuts, Module/Tab routes, rail modes, density, and localization outcomes.
- FR-11…FR-16 already hold the state names, reliability budgets, fresh-row scoping, and one-at-a-time command rule. Amend their behavioral precision without changing those decisions.
- NFR-3/NFR-4 already adopt WCAG 2.2 AA and the repository FrontComposer/Fluent V5 baseline. Preserve the ban on raw interactive controls, legacy tokens, and theme redefinition.
- SM-6 is the natural evidence home, but its current “met” state overstates the evidence after the accessibility review; mark it partial/unmet until the added paths pass.

## Addendum / UX-Source-Only Material

Keep the following out of the main PRD narrative:

- The full event-to-live-region matrix (role/politeness, copy intent, dedupe key, coalescing, silent transitions), focus-return matrix, and state-by-surface matrix belong in the addendum or UX behavior source.
- The exact reusable-component registry and visual rows belong in UX sources. Correct `FluentToolbar` / `FluentSearch` to the implementation-owned `FcPageToolbar` composition (`FluentStack role="toolbar"` plus `FluentTextInput` with search input type) rather than adding those nonexistent APIs to the PRD.
- Bind `--fc-color-accent` only as an alias of the active Fluent V5 accent role; keep contrast-role tables and forced-colors fallbacks in UX/addendum detail. Do not create an independent seed or palette.
- Replace vague navigation primitive names with the exact `FrontComposerNavigation` contract in UX sources. A comprehensive visual registry may say “inherits Fluent defaults” where no custom visual rule exists.
- Repair both supplements' dead source path and add a reconciliation date/revision marker. Remove the settled FC-IA-1 chronology from the active experience contract; retain only the decision/link in reconciliation history.
- Do not add a PRD traceability matrix: preserve UJ identifiers and make the UX supplement cite them directly.

## Conflicts With Logged Decisions

1. **D-8 / memlog “`ux-design.md` is sufficient” is overstated.** The format decision can remain (canonical UX source plus visual/behavioral supplements; no fourth standalone UX spec), but the artifacts are not handoff-ready while the high findings above remain. Amend D-8 to distinguish “sufficient artifact shape” from “validated completeness,” and track UX remediation as a new open item (next available OI ID) tied to Product re-approval rather than silently treating D-8 as closed.
2. **D-1 canonical-path decision conflicts with both supplements.** They still cite `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md`, which does not exist and contradicts the logged single-canonical-copy decision. Point them to `_bmad-output/planning-artifacts/prd.md` (the immutable old copy is under `archive/prds/...`). No PRD requirement change is needed.
3. **The memlog's claim that qualitative UX rules were fully lifted into FR-8…FR-11 is incomplete.** The new validation shows product behavior also belongs in FR-14…FR-16 and NFR-3/NFR-11. Preserve the earlier decisions; widen the mapping.
4. **No conflict with D-13's Fluent RC posture, but the UX supplement violates its executable meaning.** The selected pin remains catalog-owned; documentation must stop naming APIs absent from that pin.

## Recommended Register Changes

- Amend D-8 as described above; do not reverse the three-file authority chain.
- Add one UX-remediation open item covering the four high product-contract deltas plus the dead paths and unavailable component names. Owner: Product + UX; unblock condition: corrected UX sources, amended PRD IDs, and a rerun with no high findings. Tie it to G-4 re-approval unless Product explicitly promotes it to a readiness gate.
- Amend the §10 UX-risk entry: the 2026-09-09 review found the current contract thin/broken for downstream extraction; closure is the corrected UX contract plus SM-6 evidence, not merely the presence of `ux-design.md`.
