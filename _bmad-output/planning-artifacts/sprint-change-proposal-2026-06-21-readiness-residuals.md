---
title: 'Sprint Change Proposal — Close the readiness residuals (#3 AC-text/DoD, #4 UX-DR traceability, #5 legacy ledger)'
date: '2026-06-21'
author: 'Administrator (correct-course, Developer role)'
trigger: 'Implementation Readiness Assessment 2026-06-21 (READY) — Recommended Next Steps #3, #4, #5'
mode: 'Batch'
scope_classification: 'Minor (planning-artifact reconciliation — no code, no epic restructuring, no test impact)'
status: 'approved'
supersedes_open_items_in: '_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-21.md'
companion_proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-21.md (confirmation-debt closure)'
residuals_covered: ['#3 align 9 stories'' AC text to the new DoD', '#4 backfill UX-DR3/UX-DR8 traceability', '#5 reconcile legacy Epic-11 / DW-0666 ledger cruft']
residuals_not_covered: ['#1 change-proposal sign-off (human decision — APPROVED by Administrator 2026-06-21)', '#2 FR11 slow-query/max-items implementation verification (out of reconciliation pass — subsequently VERIFIED & CLOSED 2026-06-21, see §5 Post-pass verification)']
artifacts_changed:
  - '_bmad-output/planning-artifacts/epics.md'
  - '_bmad-output/implementation-artifacts/deferred-work.md'
  - 'tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs (DW-0666 follow-on test pins)'
---

# Sprint Change Proposal — Close the readiness residuals (#3 / #4 / #5)

**Date:** 2026-06-21 · **Trigger:** Implementation Readiness Assessment (2026-06-21, ✅ READY) — *Recommended Next Steps* #3, #4, #5 ·
**Mode:** Batch · **Scope:** Minor (planning-artifact reconciliation — no code change)

> **Relationship to the earlier proposal.** The 2026-06-21 confirmation-debt proposal
> (`sprint-change-proposal-2026-06-21.md`) moved the corpus from 🟡 NEEDS WORK to ✅ READY by closing the
> three open decisions and amending the contract-confirmation Definition-of-Done. The follow-up READY
> readiness report then listed **5 non-blocking minor cleanups**. This proposal closes the three that are
> pure planning-artifact reconciliation (**#3, #4, #5** — exactly correct-course's remit), batched into
> one pass. The other two are out of scope here and noted in §5.

---

## Section 1 — Issue Summary

The readiness re-audit (`implementation-readiness-report-2026-06-21.md`) graded the corpus **READY** with
**5 minor, non-blocking** residuals. Three are planning-artifact reconciliations:

- **#3 — Soft AC wording persists.** The global Contract-confirmation Definition-of-Done (DoD) amendment
  (2026-06-21) retired the *"confirmed **OR** escalated with an owner"* escape hatch, but the **per-story
  AC text** was never rewritten. A future reader looking at a single story's AC could **re-learn the
  retired escape hatch** without seeing the global amendment.
- **#4 — UX-DR3 / UX-DR8 lack numbered-story traceability.** Both shipped via sprint-change-proposals
  (account-hamburger 2026-06-09, security-helper 2026-06-14, nav-single-active 2026-06-19) and are
  architected (§4) and Governance-guarded, but neither is linked to a numbered Epic story — a
  *traceability* gap, not a capability gap.
- **#5 — Legacy decomposition cruft in `deferred-work.md`.** The ledger is written against a **superseded
  Epic 11 / 12 + Story 11.x / 12.x** decomposition that no longer maps to the active 7-epic plan, and
  `DW-0666` dangles a *"Epic 11 remains in-progress"* gate against an all-`done` 7-epic sprint-status.

**Evidence base:** the READY readiness report (§3–§5 + Recommended Next Steps), `epics.md`, the current
`sprint-status.yaml` (Epics 1–7 only, all `done`), and `deferred-work.md`.

---

## Section 2 — Impact Analysis

### Epic impact — none
No epic is added, removed, resequenced, or invalidated. All seven epics remain `done`.

### Story impact — wording/traceability only
- **#3:** five story ACs in `epics.md` (1.2, 1.4, 2.8, 3.3, 3.5) receive an inline DoD pointer. **No
  acceptance behaviour changes** — the gate is unchanged; only the retired escape hatch is annotated.
- **#4:** a requirement-level traceability note is added for UX-DR3/UX-DR8. **No new story is created**
  (change-proposal-of-record is explicitly accepted).
- **#5:** a reconciliation note is added to `deferred-work.md`. **No ledger row disposition changes.**

### Material finding during the pass — only 5 of 9 stories needed an edit
The DoD amendment lists **nine** contract-confirmation stories (1.2, 1.3, 1.4, 1.5, 2.8, 3.3, 3.5, 3.6,
4.3). Inspecting the **canonical** `epics.md` ACs showed only **five** still carry the literal
escape-hatch clause. The other four already read DoD-clean in `epics.md`:

| Story | Canonical `epics.md` AC reads | Action |
|---|---|---|
| 1.2 | "…confirmed (or the open question is **escalated with an owner**)…" | ✏️ annotated |
| 1.4 | "…confirmed or the boundary question is **escalated with an owner**." | ✏️ annotated |
| 2.8 | "…confirmed-stable (or open items **escalated with owners**)…" | ✏️ annotated |
| 3.3 | "…are each decided or **escalated with an owner**." | ✏️ annotated |
| 3.5 | "…confirm-stable or the gap is **escalated with an owner**." | ✏️ annotated |
| 1.3 | enforcement-mechanism wording (no escape hatch in `epics.md`) | ✅ already clean |
| 1.5 | "…or a **tracked gap with an owner**." (already DoD-shaped) | ✅ already clean |
| 3.6 | "…the threshold and polling budget values are **decided and recorded**…" | ✅ already clean |
| 4.3 | "…**confirmed as the v1 contract** and batching recorded as fast-follow." | ✅ already clean |

The four already-clean stories' historical *per-story implementation files* still contain
escalate-with-owner narrative, but those are **Dev Agent Records of what happened** (escalate-with-owner
*was* the method at the time) — rewriting completed execution history is out of scope and would falsify
the record. The forward-looking canonical reference (`epics.md`) is what the finding targets, and all
nine stories are covered by the global DoD amendment that lists them by number.

### Artifact conflicts resolved
| Artifact | Conflict | Resolution |
|---|---|---|
| `epics.md` S1.2/1.4/2.8/3.3/3.5 ACs | retired escape-hatch wording still present | → annotated with DoD pointer (gate unchanged) |
| `epics.md` UX-DR section | UX-DR3 refinements + UX-DR8 had no story link | → traceability note; change-proposal-of-record accepted |
| `deferred-work.md` | legacy Epic 11/12 numbering + dangling `DW-0666` gate | → reconciliation note mapping legacy→7-epic + `DW-0666` clarified as a tracked release-gate |

### Technical impact — none
No source, test, build, snapshot, baseline, or `sprint-status.yaml` change. `docs/` untouched.

---

## Section 3 — Recommended Approach

**Selected path: Option 1 — Direct Adjustment.** Effort **Low**, risk **Low**. Rollback and MVP-review
are both N/A (nothing built, scope unchanged). This is documentation reconciliation only, batched into a
single pass per the user's direction.

Two deliberate non-actions, each the correct conservative call:
1. **No story-file history rewrites** (#3) — completed Dev Agent Records are preserved as audit truth.
2. **No `DW-0666` policy decision** (#5) — the docs-slug UNC/drive-relative policy is a genuine Product +
   Architecture decision. This pass *records* `DW-0666` as a tracked/dated/owned blocking follow-up
   (exactly the DoD-compliant disposition) and **decouples** it from the obsolete "Epic 11" gate; it does
   **not** fabricate a closure.

---

## Section 4 — Detailed Change Proposals (all applied)

### #3 — Align story AC text to the DoD (`epics.md`, 5 edits)
Each of S1.2 / S1.4 / S2.8 / S3.3 / S3.5 had its escape-hatch clause rewritten from a bare
*"escalated with an owner"* to an explicit DoD-tied form, e.g.:

> **OLD (S1.4):** *…it is confirmed or the boundary question is escalated with an owner.*
> **NEW (S1.4):** *…it is confirmed — or, per the Contract-confirmation Definition-of-Done (2026-06-21),
> the boundary question is recorded as a tracked, dated, owned blocking follow-up ("escalated with an
> owner" alone is **not** Done).*

The other four DoD-listed stories (1.3, 1.5, 3.6, 4.3) were verified already-clean in `epics.md` (see §2
table) and are covered by the global DoD amendment that enumerates all nine by number.

### #4 — Backfill UX-DR3 / UX-DR8 traceability (`epics.md`, 1 insertion)
A `🔗 UX-DR story-traceability note (added 2026-06-21)` was inserted after the UX-DR list, **accepting
change-proposal-of-record** (no synthetic backfill story):
- **UX-DR3** — always-visible Desktop hamburger (supersedes "D9") + single-active-nav shipped via
  `…-2026-06-09-shell-account-hamburger` and `…-2026-06-19-nav-single-active-item`; base
  rail/breakpoint behaviour stays traced to **Story 2.2** (AC `*(UX-DR3)*`).
- **UX-DR8** — `FcAccountMenu` + framework server security shipped via
  `…-2026-06-09-shell-account-hamburger` and `…-2026-06-14-shell-security-helper`; **no dedicated story**
  — this note is its sole story-level traceability link.

### #5 — Reconcile legacy Epic-11 / DW-0666 ledger cruft (`deferred-work.md`, 1 insertion)
A `## Legacy Epic 11 / 12 Numbering Reconciliation (2026-06-21)` section was inserted at the top of the
ledger (ahead of every Epic-11 reference), recording:
- The `Epic 11/12` + `Story 11.x/12.x` numbering is a **superseded decomposition**; the active plan and
  current `sprint-status.yaml` track **only Epics 1–7, all `done`**, with **no `epic-11`/`epic-12`
  markers** — so the `W3` references to `sprint-status.yaml:147`/`:155` are **stale line references**, not
  the current status.
- `DW-0666` is **not** an open epic — it is a **standalone Product + Architecture docs-slug release-gate**,
  recorded (per the DoD) as an open/tracked/dated/owned blocking follow-up with a stated default and
  closure trigger, **decoupled** from the all-`done` 7-epic status.

---

## Section 5 — Implementation Handoff

**Scope classification: Minor.** Direct implementation by the Developer agent — **already applied** in
this batch pass. No PO/PM/Architect handoff.

### Deliverables produced
- This proposal (`sprint-change-proposal-2026-06-21-readiness-residuals.md`).
- 6 artifact edits applied: 5 AC annotations + 1 UX-DR note in `epics.md`; 1 reconciliation note in
  `deferred-work.md`.

### Success criteria
- ✅ **#3** — every `epics.md` contract-confirmation AC that carried the retired escape hatch now points
  at the DoD; the four already-clean stories are documented as such.
- ✅ **#4** — UX-DR3 refinements and UX-DR8 carry an explicit, dated traceability record.
- ✅ **#5** — the legacy Epic 11/12 numbering is mapped to the current 7-epic structure and `DW-0666` is
  clarified as a tracked release-gate, removing the dangling "Epic 11 in-progress" confusion.

### Residuals deliberately NOT closed here
- **#1 — Sign off the change proposal.** `sprint-change-proposal-2026-06-21.md` is
  `status: approved-pending-final-signoff`. This is a **human governance act** by the Product/UX owner
  (Administrator) and is **left to the user** — flip its front-matter to `approved` to formally retire the
  confirmation debt. *(Not actioned in this pass to avoid fabricating a sign-off.)*
- **#2 — Verify the FR11 slow-query / max-items notice.** An **implementation-verification** task (does the
  shipped grid render the notice?), not a planning-artifact reconciliation — outside the reconciliation
  pass itself, but **verified on request** (see Post-pass verification below). Owner: FrontComposer Shell.

### Routing
- **Developer agent:** changes applied; nothing further.
- **User (Administrator):** action residual #1 sign-off. *(Residual #2 verified — see below.)*

---

## Post-pass verification (2026-06-21) — residual #2 (FR11) is CLOSED

At the user's request, the FR11 *slow-query / max-items notices* sub-clause was verified directly against
the shipped Shell source. **The readiness report's premise was incorrect** — it called this "the single
requirement clause not provably closed by an existing test"; in fact it is implemented, generator-wired,
and test-covered:

- **Components exist** — `FcSlowQueryNotice` (auto-dismissing `FluentMessageBar Intent="Info"`, visible
  when `LoadedPageState.LastElapsedMsByKey[ViewKey] > FcShellOptions.SlowQueryThresholdMs`, default
  2_000 ms, auto-dismiss after 5 s) and `FcMaxItemsCapNotice` (non-dismissing info bar, visible when
  `ItemsCount >= FcShellOptions.MaxUnfilteredItems`, default 10_000, **and** no real filter active).
- **Wired into every generated grid** — `RazorEmitter.EmitGridEnvelopeOpen` (Story 4-4 T2.4)
  **unconditionally emits both components above the grid** in each `[Projection]` view, passing
  `ViewKey` / `ItemsCount` (= `state.Items.Count`) / `AnyRealFilterActive`. Not orphaned components.
- **Configurable + validated** — `FcShellOptions.SlowQueryThresholdMs` (range 500–30_000) and
  `MaxUnfilteredItems` (range 100–1_000_000).
- **Tests pass (10/10, run 2026-06-21):**
  - `RazorEmitterBannersTests` (SourceTools, Story 4-4 T5.4) — **3/3**: asserts the emitter outputs both
    components + their `ViewKey`/`ItemsCount`/`AnyRealFilterActive` attributes.
  - `FcSlowQueryNoticeTests` + `FcMaxItemsCapNoticeTests` (Shell bUnit) — **7/7**: threshold-crossing
    visibility and boundary cases (9_999 hidden / 10_000 + 10_001 shown / 15_000-with-filter hidden).

**Conclusion:** residual #2 is closed. With it and #1's sign-off (now approved), **all five residuals from
the READY readiness report are resolved** and the planning corpus is fully clean.

---

## Post-pass follow-on (2026-06-21) — `DW-0666` decided & closed

Following the #5 reconciliation, the user authorized closing the one genuinely-open ledger item:
**`DW-0666`** — the docs-slug UNC / drive-relative normalization policy that kept the legacy "Epic 11"
gate dangling. Decision recorded in `deferred-work.md`:

- **Decision (Administrator, as Product + Architecture owner):** **option (a) — reject UNC
  (`//server/share`) and drive-relative (`C:…`) slug forms fail-closed** as rooted/hostile
  (accept-with-rationale and split-per-platform declined; no off-site docs-hosting driver).
- **Already-enforced (confirm-and-pin, not a behaviour change)** — `IsRootedSlug` classifies both shapes
  as rooted (`slug[0]=='/'` for UNC; `slug[1]==':'` for drive-relative) and the `== "diagnostics/{id}"`
  exact-match gate rejects them a second time.
- **Pinned** — added `//server/share/diagnostics/HFC1058` and `C:diagnostics/HFC1058` → `invalid-slug`
  cases to `DocsSlugValidation_DistinguishesUnsafeCanonicalizationFailures` (**26/26 green**, run 2026-06-21).
- **Ledger reconciled** — top-of-file note flipped to RESOLVED; a `Final classification 2026-06-21:
  resolved` marker appended to the `DW-0666` row; Reconciliation Summary reclassified
  (`release-gate` 1 → 0, `resolved-preserved` 119 → 120, total still 666). **No live release-gate rows
  remain.**

This is a **`tests`-only** code change (1 file, 2 `InlineData` lines) plus ledger reconciliation — no
`src/` or runtime-behaviour change.

---

**Outcome:** The three planning-artifact residuals from the READY readiness report are closed. The
retired escape hatch can no longer be re-learned from a single story's AC, UX-DR3/UX-DR8 have an explicit
traceability record, and the legacy Epic-11 / `DW-0666` ledger cruft is reconciled against the live 7-epic
plan. Follow-on at the user's request: residual #1 sign-off **approved**, residual #2 (FR11) **verified
by passing tests**, and the last open ledger item **`DW-0666` decided and closed** — the readiness corpus
now has **no open items**.
