# Accessibility Contract Review — OI-16 — 2026-09-09

## Overall verdict

**Changes required.** The repaired D-8 chain is substantially more complete, preserves its stable
identifiers, and accurately leaves OI-16, SM-6, OI-19, G-4, D-9, and Product approval open. However,
three High-severity contract defects still prevent a clean OI-16 document-quality result: announcement
coalescing and focused-alert behavior are not fully deterministic, `Degraded` has a contradictory
terminal classification, and the WCAG 2.2 SC 2.5.8 exceptions are incomplete because two distinct
exceptions are merged. Open implementation/evidence work is not counted as a document defect where
the acceptance contract and evidence hook are complete.

## Strengths

- The authority order, reconciliation marker, stable `UX-DR1`–`UX-DR8` identifiers, and non-approval
  posture are explicit (`ux-design.md:20-27,41-44`; `ux-design-detailed-2026-07-05.md:76-80`;
  `ux-experience-2026-07-05.md:22-26`).
- UX-AM-1, UX-VR-1, UX-FM-1, UX-SS-1, and UX-AE-1 provide useful stable joins from observable
  behavior to bUnit, fake-time, semantic-DOM, keyboard, and e2e evidence (`ux-design.md:128-236`).
- Client validation, safely mapped and unmapped server rejection, input preservation, summary-to-field
  navigation, blocked submission, and keyboard-only recovery are clearly distinguished
  (`ux-design.md:161-170`; `ux-experience-2026-07-05.md:133-142`).
- The reflow, 400% zoom, WCAG 1.4.12 spacing values, focus-obscuration floor, light/dark contrast,
  forced-colors fallbacks, and reduced-motion outcomes are stated as measurable behavior rather than
  visual preference (`ux-design.md:220-236`; `ux-design-detailed-2026-07-05.md:100-158,199-208`).
- The state matrix includes every named FR-11 state, every named FR-15 state, blocked submit, no tenant,
  and no access, with delivery/evidence status separated from the normative outcome
  (`ux-design.md:187-218`).
- Support-safe copy/evidence exclusions are explicit, and automated checks are correctly described as
  supporting rather than replacing manual assistive-technology/device evidence
  (`ux-design.md:122-126,222-224`; `ux-experience-2026-07-05.md:174-184`).
- The requirement ledger does not relabel historical partial evidence as an SM-6 pass and keeps the
  FR-23/OI-19 boundary explicit (`ux-design.md:238-267`).

## Findings

### Critical

None.

### High

- **H-01 — Announcement coalescing and focused-alert delivery are not deterministic enough to test.**
  AM-12 and the experience supplement require “rapid” intermediate progress to coalesce but define no
  time window, render-batch boundary, leading/trailing behavior, or expected message when several
  states arrive inside that boundary (`ux-design.md:148`; `ux-experience-2026-07-05.md:126-131`). In
  addition, the canonical definition requires a focusable summary “using alert semantics” while saying
  focus is the single announcement path (`ux-design.md:130-133,154-155`). A newly populated
  `role="alert"` and programmatic focus can create two assistive-technology announcements unless DOM
  creation, population, live attributes, and focus ordering are specified. *Fix:* define an exact
  coalescing duration or exact render/event-batch invariant, whether the first or last eligible message
  wins, and fake-time/count assertions. For validation/rejection, choose one observable speech path and
  specify the summary's role/live attributes plus insertion/population/focus sequence so the same text
  is announced exactly once in the required assistive-technology lane.

- **H-02 — `Degraded` contradicts the matrix's own terminal definition.** UX-SS-1 defines Terminal as a
  state that cannot advance automatically, but SS-24 labels `Degraded` a “Terminal landing” while
  polling may continue until 120,000 ms and later evidence may advance it automatically
  (`ux-design.md:187-190,217`). That makes terminal-announcement and first-terminal-wins assertions
  ambiguous. *Fix:* classify `Degraded` as non-terminal while polling continues and state what happens
  when the 120,000 ms budget expires, or split the active-degraded and polling-exhausted conditions into
  separately testable rows. Align AM-17's once-only rule with that classification.

- **H-03 — The target-size exception contract merges two different WCAG 2.2 SC 2.5.8 exceptions.**
  AE-04 and UX-RF-1 list an undefined “equivalent-spacing” exception
  (`ux-design.md:231`; `ux-design-detailed-2026-07-05.md:156-158`). SC 2.5.8 has distinct **Spacing**
  and **Equivalent** exceptions; merging them permits inconsistent geometry decisions and omits the
  measurable spacing condition. *Fix:* enumerate Inline, Spacing, Equivalent, User Agent Control, and
  Essential separately. Define Spacing with the required 24 CSS-pixel diameter-circle non-intersection
  measurement, define Equivalent as a separate conforming control providing the same function, and
  require evidence to record the target, exception, measurement or equivalent control, and rationale.

### Medium

- **M-01 — No-tenant/no-access announcement rows lack a complete announcement join.** SS-05 and SS-06
  say only “once per failed/denied activation,” without an AM identifier, exact channel, dedupe identity,
  silent behavior, or announcement-count evidence (`ux-design.md:198-199`). VR-04 adds “Polite” for
  authorization denial while also moving focus to the replacement heading, leaving the single-speech-
  path rule unresolved (`ux-design.md:168,185`). *Fix:* append stable AM rows for no tenant and no
  access, or declare a focus-only path. In either case specify exact safe message, channel, dedupe key,
  focus/live-region interaction, silent repeats, and bUnit/e2e count assertion.

- **M-02 — Dialog opening focus and the missing-invoker fallback are under-specified.** The component
  rows require trapping and return behavior, but UX-FM-1 defines an opening destination only for the
  palette; settings and confirmation/abandonment dialogs have no general opening-focus row
  (`ux-design.md:172-185`; `ux-experience-2026-07-05.md:87-90,159-170`). “Nearest enabled logical
  predecessor” is not an independently reproducible fallback algorithm. *Fix:* append focus rows for
  settings, destructive confirmation, and abandonment opening, including the safe initial target,
  trap/cycle/Escape behavior, and exact fallback order when the invoker is removed or disabled. Define
  the predecessor in DOM/component terms or fall directly to the route `h1`.

- **M-03 — Filter-no-results has two observable messages under one underspecified matrix reference.**
  AM-02 specifies “No {item label} available,” SS-15 cites “AM-02 with filter context,” and the behavior
  supplement specifies “No {items} match these filters” (`ux-design.md:138,208`;
  `ux-experience-2026-07-05.md:70-74`). *Fix:* append a dedicated announcement ID for filter-no-results,
  or make AM-02 contain explicit unfiltered and filtered branches with their exact message, identity,
  dedupe, silent-repeat, and recovery-action rules.

- **M-04 — The behavior supplement overstates the scope of UX-SS-1.** It says every Information
  Architecture surface lands on a state row, but its IA includes module-tab, command-form, and
  palette/dialog states that do not have canonical SS rows (`ux-experience-2026-07-05.md:42-51,101-122`;
  `ux-design.md:187-218`). The addendum-mandated FR-11, FR-15, blocked, no-tenant, and no-access coverage
  is present, so this is not a missing mandatory-state High finding. *Fix:* narrow the claim to the
  addendum-mandated state families, or append rows for every IA state family and retain the existing FM,
  VR, and AM joins.

### Low

- **L-01 — Two OI-16 evidence-trail dispositions still read as pending source edits.** The updated
  supplements carry the reconciliation revision, but their ledger rows say reconciliation is
  “required by this update” (`ux-design.md:258-259`; both supplement front matters at lines 1-18).
  *Fix:* record both supplements as repaired and awaiting clean follow-up review, without changing the
  open status of OI-16, SM-6, OI-19, G-4, D-9, or Product approval.

## Severity summary

- Critical: 0
- High: 3
- Medium: 4
- Low: 1

**Gate result:** not clean; three High findings remain. This review does not close OI-16, SM-6, G-4,
or OI-19 and is not Product approval.
