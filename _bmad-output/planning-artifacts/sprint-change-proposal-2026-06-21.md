---
title: 'Sprint Change Proposal — Close the implementation-readiness confirmation debt'
date: '2026-06-21'
author: 'Administrator (correct-course, Developer role)'
trigger: 'Implementation Readiness Assessment 2026-06-21 — 🟡 NEEDS WORK (confirmation debt)'
mode: 'Incremental'
scope_classification: 'Minor (documentation/ratification reconciliation — no code change, no epic restructuring)'
status: 'approved-pending-final-signoff'
supersedes_open_items_in: '_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-21.md'
artifacts_changed:
  - '_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md'
  - '_bmad-output/contracts/fc-cmd-command-budget-contract-2026-06-04.md'
  - '_bmad-output/contracts/fc-cmd-retry-degraded-state-contract-2026-06-05.md'
  - '_bmad-output/planning-artifacts/epics.md'
  - '_bmad-output/implementation-artifacts/2-3-datagrid-filtering-status-and-empty-loading-states.md'
  - '_bmad-output/implementation-artifacts/6-1-level-2-projectiontemplate-overrides.md'
  - '_bmad-output/implementation-artifacts/1-6-theme-density-and-settings-persistence.md'
  - '_bmad-output/implementation-artifacts/3-4-command-lifecycle-ui.md'
  - '_bmad-output/implementation-artifacts/4-1-destructive-command-confirmation.md'
  - '_bmad-output/implementation-artifacts/4-2-unsaved-form-abandonment-guard.md'
---

# Sprint Change Proposal — Close the implementation-readiness confirmation debt

**Date:** 2026-06-21 · **Trigger:** Implementation Readiness Assessment (2026-06-21) ·
**Mode:** Incremental · **Scope:** Minor (documentation/ratification — no code change)

---

## Section 1 — Issue Summary

The 2026-06-21 whole-plan retrospective readiness audit graded the planning corpus
**🟡 NEEDS WORK** — *structurally excellent* (100% FR→epic traceability, no forward dependencies,
no technical-milestone epics, strong AC discipline) but held back by a single systemic pattern:
**confirmation debt**. Across the corpus, *recommended-and-shipped* decisions were allowed to close
on the AC2-style escape hatch *"confirmed **OR** escalated with an owner"* and were never formally
confirmed. Three such decisions were still open weeks later.

This proposal closes all three open decisions, fixes the process gap that produced them, and clears
the cheap traceability/staleness findings — moving the corpus from 🟡 to a clean **READY**.

**Material correction discovered during this pass.** The audit's #1 finding — *"AR8 numeric budgets
have no approved values (a real requirement gap)"* — is **stale**. The values exist and are
documented: `fc-cmd-command-budget-contract-2026-06-04.md` carries a full approved value table at
`status: confirm-stable`, and its own *Escalations* section reads *"None… the expected v1 defaults
are the final values."* AR8 is therefore the **same ratification debt** as FC-LYT, not a requirement
gap. This downgrades the only finding the audit flagged as a "real gap."

**Evidence base:** the readiness report; the three contract files; `epics.md`; the 40 story files.

---

## Section 2 — Impact Analysis

### Epic impact — none structural
Epics 1–7 are implemented and retro'd. **No epic is invalidated, resequenced, added, or removed.**
The Epic 3/4 shared-surface question is resolved by *accepting the documented split* (decision, not
rework). All impact is documentation/ratification on existing artifacts.

### Story impact — traceability tags only
Six done stories receive traceability tags (no behavioural change). Story 2.3 receives a clearly
dated **traceability addendum AC** (FR11 slow-query/max-items) flagged for implementation
verification rather than presented as an original acceptance gate.

### Artifact conflicts resolved
| Artifact | Conflict | Resolution |
|---|---|---|
| `fc-lyt-page-layout-2026-06-03.md` | `status: escalated`, 2 inputs unsigned | → `confirmed` (FullWidth + 75rem) |
| `fc-cmd-command-budget-contract-2026-06-04.md` | `confirm-stable`, never ratified | → `confirmed` (values unchanged) |
| `fc-cmd-retry-degraded-state-contract-2026-06-05.md` | "v1 implementation contract" | → `confirmed v1 contract` |
| `epics.md` AR8 line | stale "(none approved yet)" | → confirmed values + contract refs |
| `epics.md` UX-DR1–6 | `[inferred]`, unconfirmed | → confirmed; refreshed vs architecture §4 |
| `epics.md` UX-DR3 | stale vs architecture (hamburger/nav) | → refreshed; UX-DR8 added (account/security) |
| `epics.md` NFR13 | `[inferred]` | → confirmed |
| `epics.md` FR4/FR11/NFR11/UX-DR4 | sub-clause/cross-cutting traceability gaps | → tagged / made explicit |
| `epics.md` Epic 3/4 note | open consolidation offer | → split accepted, offer withdrawn |

### Technical impact — none
No source, test, build, or runtime change. `sprint-status.yaml` requires **no** update (no epic
added/removed/renumbered — checklist §6.4 = N/A). The published `docs/` site is untouched.

---

## Section 3 — Recommended Approach

**Selected path: Option 1 — Direct Adjustment.** Effort **Low**, risk **Low**.

Rollback (Option 2) is rejected — the code is correct and shipped; there is nothing to revert.
MVP review (Option 3) is rejected — scope is unchanged. This is pure documentation reconciliation
plus three ratifications the user (as Product/UX owner) made live on 2026-06-21:

1. **AR8** — ratify the existing documented v1 budgets as final.
2. **FC-LYT** — confirm `FullWidth` default + `75rem` constrained max-measure.
3. **UX-DRs** — confirm UX-DR1–6 (+ NFR13) as-is and refresh them against architecture §4.

…and one structural decision: **accept the Epic 3/4 risk-boundary split** (both epics shipped/retro'd;
retro-consolidating done work is churn with no benefit).

The **root-cause process fix** amends the contract-confirmation Definition-of-Done so
"escalate-with-owner" can no longer silently count as Done.

---

## Section 4 — Detailed Change Proposals (all applied)

### Group 1 — Three ratifications (decisions 1 & 2)
- **FC-LYT** `fc-lyt-page-layout-2026-06-03.md`: front-matter `status: escalated → confirmed`,
  owner stamped `Administrator, 2026-06-21`; Confirmation section rewritten to record both sign-offs
  (FullWidth default ✅, 75rem max-measure ✅).
- **AR8 budget** `fc-cmd-command-budget-contract-2026-06-04.md`: `Status: confirm-stable → confirmed
  (Product/UX + EventStore ratified 2026-06-21)`. Value table unchanged.
- **AR8 retry** `fc-cmd-retry-degraded-state-contract-2026-06-05.md`: `Status: v1 implementation
  contract → confirmed v1 contract (ratified 2026-06-21)`.

### Group 2 — epics.md requirement updates (decisions 1, 3, 4)
- **AR8 stale text** → confirmed values + contract references.
- **UX-DR section header** → blanket `[inferred]` caveat replaced with "Confirmed 2026-06-21".
- **UX-DR1, 2, 4, 5, 6** → `[inferred]` tag dropped.
- **UX-DR3** → refreshed: always-visible Desktop hamburger (supersedes "D9"), single-active-nav
  (`NavLinkMatch.Prefix`).
- **UX-DR7** → confirmed stamp (FullWidth + 75rem).
- **UX-DR8 (new)** → `FcAccountMenu` account control + framework-owned server security.
- **NFR13** → `[inferred]` → confirmed.
- **Epic 3/4 note** → split accepted (2026-06-21), consolidation offer withdrawn.

### Group 3 — Traceability cleanups
- **FR4** template-manifest emission → tagged on S6.1 AC1 (epics.md + `6-1-*.md`).
- **FR11** slow-query/max-items notices → new AC on S2.3 (epics.md clean AC + `2-3-*.md` dated
  traceability addendum flagged "verify implementation").
- **NFR11** telemetry → cross-cutting ownership made explicit (`FrontComposerActivitySource`, Shell +
  MCP paths).
- **UX-DR4** four untagged components → tagged on S1.6, S3.4, S4.1, S4.2 (epics.md + each story file).

### Group 4 — Process-gap fix (root cause)
- **Contract-confirmation Definition-of-Done** amendment added to `epics.md`: stories 1.2, 1.3, 1.4,
  1.5, 2.8, 3.3, 3.5, 3.6, 4.3 MUST NOT reach Done on "escalated with an owner" alone — Done now
  requires either **confirmed** or a **tracked, dated, owned blocking follow-up**.

---

## Section 5 — Implementation Handoff

**Scope classification: Minor.** Direct implementation by the Developer agent — **already applied**
in this correct-course pass. No PO backlog reorganization and no PM/Architect replan required.

### Deliverables produced
- This Sprint Change Proposal (`sprint-change-proposal-2026-06-21.md`).
- 10 artifact edits applied (front-matter list above).

### Success criteria
- ✅ All three open decisions closed with a named owner (Administrator) and date (2026-06-21).
- ✅ AR8 / FC-LYT / UX-DR / NFR13 statuses reflect "confirmed" in their source artifacts.
- ✅ FR4 / FR11 / NFR11 / UX-DR4 traceability gaps tagged.
- ✅ Epic 3/4 split formally accepted.
- ✅ Process Definition-of-Done amended to prevent recurrence.

### Residual follow-up (one tracked item)
- **FR11 slow-query/max-items notices** — the new S2.3 AC is a *traceability addendum*. Confirm the
  shipped grid surfaces actually render these notices; if absent, raise a small follow-up story.
  Owner: FrontComposer Shell. (This is the only item not provably closed by existing tests.)

### Routing
- **Developer agent:** changes applied; verify the FR11 residual above.
- **No further handoff** to PO / PM / Architect.

---

**Outcome:** With these changes the readiness corpus moves from 🟡 NEEDS WORK to **READY** — the
confirmation debt and its enabling process gap are closed, and the one finding the audit called a
"real gap" (AR8) was corrected to a ratification it now records as confirmed.
