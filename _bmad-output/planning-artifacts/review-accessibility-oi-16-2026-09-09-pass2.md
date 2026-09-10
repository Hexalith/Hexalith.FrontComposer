# Accessibility Contract Review — OI-16 — Pass 2

## Reviewed revision

This is a clean second-pass review of the exact files below against FR-8, FR-10–FR-16, FR-22,
FR-23, NFR-3, SM-6, PRD Addendum §4, `docs/accessibility-verification/README.md`, and the
`bmad-ux` reviewer rubric.

| Artifact | SHA-256 |
| --- | --- |
| `ux-design.md` | `fcdd911c118f6831dc52064d5ed0497aa0da7b98241af64697b955b5a127d57c` |
| `ux-design-detailed-2026-07-05.md` | `4bb720364b69370d001a6ecdc151428813ea51bd05a47d1fcb06e538c7bc115c` |
| `ux-experience-2026-07-05.md` | `f611661cdb950ba6074c93dbbaded518602131001c1de3576bf1928260fcf8f6` |

Open implementation and evidence rows were not treated as document defects when they state a
deterministic acceptance outcome and an adequate evidence hook.

## Overall verdict

**Clean at the Critical/High document-contract threshold.** The revised chain provides deterministic
announcement dedupe and state-free coalescing groups, a single focus-only validation speech path,
complete overlay/focus and state-by-surface matrices, exact responsive/accessibility criteria, and an
accurate delivered-versus-open ledger. Four Medium clarifications and one Low evidence-trail update
remain, but none prevents independent implementation or testing of the PRD/Addendum §4 outcomes.

## Severity counts

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 4 |
| Low | 1 |

**Unresolved Critical/High count: 0 total — 0 Critical, 0 High.**

## Strengths

- The D-8 authority order, stable `UX-DR1`–`UX-DR8` identifiers, exact public component names, and
  pending approval posture are explicit (`ux-design.md:20-28,44-45,75-88`).
- The focused-summary contract now defines a single speech path with no alert/live-region duplication,
  and mapped/unmapped server rejection remains correctly distinct (`ux-design.md:134-139,170-171,
  185-194`).
- Event dedupe and cross-state coalescing now use separate keys, exact trailing-window behavior, stale
  result suppression, and 249/250 ms sequence evidence (`ux-design.md:141-149,151-183`;
  `ux-experience-2026-07-05.md:132-145`). FC-NIP announcement identity is tenant + user + entity
  across views, matching FR-13/Addendum §4.1 (`ux-design.md:173`; `prd.md:318-319`;
  `prd-addendum-2026-09-08.md:84`).
- Route, tab, palette, settings, destructive-confirmation, and abandonment focus destinations,
  containment, Escape behavior, and removed-invoker fallback are represented in UX-FM-1 and UX-OF-1
  (`ux-design.md:196-223`).
- UX-SS-1 now covers every addendum-mandated FR-11 and FR-15 state, blocked submit, no tenant, and no
  access, and also supplies rows for the remaining IA surfaces (`ux-design.md:225-280`;
  `ux-experience-2026-07-05.md:101-130`). The active and exhausted Degraded conditions are separated
  into non-terminal and terminal rows (`ux-design.md:255,257`).
- Reflow, actual 400% browser zoom, the four WCAG 1.4.12 spacing overrides, all five SC 2.5.8
  exception classes, the stricter unobscured-focus geometry floor, theme contrast, forced colors, and
  reduced motion have measurable outcomes and open evidence lanes (`ux-design.md:282-298`;
  `ux-design-detailed-2026-07-05.md:99-160,201-210`).
- The requirement ledger accurately separates retained runtime behavior from open implementation and
  evidence work, identifies the existing Axe lane as insufficient, and leaves FR-23 parity to OI-19
  (`ux-design.md:300-329`).
- The chain requires support-safe evidence and correctly defers manual AT/device completion and
  fail-closed release classification to the repository accessibility guidance
  (`ux-experience-2026-07-05.md:187-199`; `docs/accessibility-verification/README.md:9-72`).

## Findings

### Critical

None.

### High

None.

### Medium

#### M-01 — Keyboard-shortcut overlay origin is not a reproducible focus target

**Evidence/location:** UX-OF-1 names a `Ctrl+K` or `Ctrl+,` “owner” as the invoker, while FM-04/FM-05
require return to the invoker (`ux-design.md:203-204,218-223`). Neither file defines whether “owner” is
the shell, the active element when the chord fires, or another stable control. Addendum §4.2 requires
palette/dialog close to return to its invoker (`prd-addendum-2026-09-08.md:90-94`).

**Remediation:** Define shortcut invocation as capturing the connected, enabled
`document.activeElement` before opening. Return there on close; when it is unavailable, use the existing
route-`h1` fallback. Name the stable selector/handle and test shortcut opening from shell navigation,
page content, and a removed-origin case.

#### M-02 — Several expanded state rows do not follow the matrix's terminal definition

**Evidence/location:** Terminal is defined as unable to advance automatically without new user input,
context, or permission (`ux-design.md:227-228`). SS-39 and SS-43–SS-45 explicitly require a user choice
but are classified Non-terminal; SS-41 is a settled zero-result query requiring a new edit, yet is also
Non-terminal; SS-35 is only conditionally classified with no alternate classification
(`ux-design.md:266,270,272,274-276`). The addendum requires a terminal/non-terminal classification for
each state row (`prd-addendum-2026-09-08.md:98`).

**Remediation:** State the subject of classification consistently—query/activation attempt, local
operation, or overlay session—and then reclassify these rows. If “waiting for a user decision” is meant
to be Non-terminal, amend the definition explicitly and apply it consistently; remove conditional
classification by naming both offline conditions or selecting one deterministic class.

#### M-03 — The target-size evidence scope can omit shell and lifecycle targets

**Evidence/location:** UX-AE-1 globally applies every evidence row to changed shell and lifecycle
surfaces, but AE-04's test setup enumerates rail, tabs, toolbar, grid, forms, palette, and dialogs—not
shell/account controls or lifecycle/recovery controls (`ux-design.md:282-293`). UX-RF-1's shell target
cell names only hamburger/navigation actions even though FR-8 requires the account menu
(`ux-design-detailed-2026-07-05.md:146-153`; `prd.md:249-258`).

**Remediation:** Say “every pointer target on every changed surface” and make the inventory explicitly
include skip/account/menu controls, Home cards/CTAs, lifecycle/recovery actions, and all current listed
families. Preserve the corrected five-exception recording and geometry rules.

#### M-04 — `Polite combobox status` has no defined semantic channel

**Evidence/location:** UX-AM-1 defines `Polite status` as one shared `role="status"`/
`aria-live="polite"` channel per surface, but AM-28 introduces a different `Polite combobox status`
without defining role, live attributes, ownership, or its relationship to the shared surface channel
(`ux-design.md:134-139,180`). This leaves no deterministic way to prevent a Fluent combobox's native
result-count speech from duplicating AM-28.

**Remediation:** Define AM-28's exact rendered channel and whether it replaces or reuses the shared
surface status. Require one owner for zero-result speech, suppression of any duplicate native/custom
message, and a screen-reader DOM plus fake-time message-count assertion.

### Low

#### L-01 — The OI-16 trail does not yet name the pass-two review

**Evidence/location:** UX-OI16-1 lists the first follow-up accessibility report but not this pass-two
artifact (`ux-design.md:315-329`). This is expected pre-review bookkeeping, but the evidence trail will
be incomplete after this file is created if left unchanged.

**Remediation:** In the eventual evidence-trail/validation update, retain the failed first-pass result,
add this pass-two filename and exact reviewed digests, and record its 0 Critical / 0 High result. Do not
mark OI-16 or SM-6 complete based on document review alone.

## Gate statement

This pass has **0 unresolved Critical and 0 unresolved High findings** and is clean at the OI-16
document-contract review threshold. That result does not establish the still-open SM-6 implementation
and evidence work: OI-16 remains open, G-4 remains open, OI-19 remains independent, and D-9 re-approval
remains pending. This report is not Product approval.
