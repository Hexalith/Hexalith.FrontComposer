# Story 11.1: Deferred Work Ledger Reconciliation and Ownership

Status: ready-for-dev

> **Epic 11** - Deferred Hardening & Release Readiness. Converts the existing deferred-work ledger into owned, auditable backlog decisions without reopening completed Epics 1-10. Applies lessons **L06**, **L07**, **L08**, and especially **L10**.

---

## Executive Summary

Story 11-1 is the release-readiness cleanup pass for `_bmad-output/implementation-artifacts/deferred-work.md`.

The ledger already has a top-level Epic 11 routing section, but many older entries still rely on vague owners such as "future", "follow-up", "v1.x", an old completed story, or no owner at all. This story reconciles those entries so every unresolved deferred item has a concrete owner surface, every duplicate is cross-referenced, and every resolved or non-action item keeps evidence.

This story is process/documentation work. It should not implement the downstream fixes owned by Stories 11.2 through 11.7.

---

## Story

As a product owner,
I want the deferred-work ledger reconciled into owned backlog entries,
so that completed stories do not leave ambiguous future work behind.

### Release-Readiness Job To Preserve

A release owner should be able to scan the ledger and answer: "Is this deferred item still open, who owns the next decision, and where is the evidence if it was resolved, superseded, or accepted as non-action?"

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary artifact | Update `_bmad-output/implementation-artifacts/deferred-work.md`; keep original audit trail intact. |
| Scope | Reconcile ownership/status only. Do not fix the technical findings owned by Stories 11.2-11.7. |
| Owner contract | Every unresolved entry must point to a concrete active backlog story or explicitly named future decision surface. |
| Resolved contract | Resolved entries must keep a date, evidence path, and original review source. |
| Duplicate contract | Duplicate entries must be cross-referenced, not deleted, so audit history remains searchable. |
| Non-action contract | Accepted/no-action decisions must name the rationale, date, and decision owner. |
| Vague owner cleanup | Tighten "future", "follow-up", "v1.x", "later", and completed-story owners to Story 11.2-11.7 or a documented non-action decision. |
| Verification | Add a short reconciliation summary and a mechanical checklist or grep-based evidence that no vague unresolved owner remains. |
| Guardrail | Do not change production code unless a tiny local validation script is absolutely necessary and documented as support tooling. |

Start here: T1 inventory ledger entries -> T2 classify by status and owner -> T3 route unresolved entries to Stories 11.2-11.7 -> T4 cross-reference duplicates/resolved items -> T5 add reconciliation summary and validation evidence.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- |
| AC1 | `_bmad-output/implementation-artifacts/deferred-work.md` | Story 11.1 is complete | Every unresolved deferred item has one of: linked active backlog story, superseded/resolved marker with evidence, or documented non-action decision. |
| AC2 | A deferred item says "future", "follow-up", "later", "v1.x", or names only a completed story as owner | The ledger is reconciled | The entry is tightened to a concrete owner surface, normally Story 11.2-11.7, or recorded as a non-action decision with rationale. |
| AC3 | Duplicate deferred entries exist across repeated review passes | The ledger is reconciled | Duplicates are merged by cross-reference while preserving every original review source and date. |
| AC4 | An entry is already resolved or appears with a checkmark/resolved wording | The ledger is reconciled | The resolved state remains visible and includes date, evidence path or source file, and the original source context. |
| AC5 | An entry is intentionally accepted as no action | The ledger is reconciled | It records the decision owner, decision date, short rationale, and why no Story 11.x implementation is required. |
| AC6 | An unresolved entry belongs to diagnostic registry, docs stubs, HFCM, docs slug/schema/sample validation, or compatibility suppression policy | Routing is updated | It is owned by Story 11.2 unless explicitly resolved or accepted as non-action. |
| AC7 | An unresolved entry belongs to CLI migration, IDE parity, manifest parsing, sidecar/path normalization, write safety, or help/README behavior | Routing is updated | It is owned by Story 11.3 unless explicitly resolved or accepted as non-action. |
| AC8 | An unresolved entry belongs to drift detection, SourceTools generator behavior, metadata drift, deterministic output, or analyzer coverage | Routing is updated | It is owned by Story 11.4 unless explicitly resolved or accepted as non-action. |
| AC9 | An unresolved entry belongs to MCP schema negotiation, agent contract, skill corpus, fingerprint, tenant-scope, or schema rejection behavior | Routing is updated | It is owned by Story 11.5 unless explicitly resolved or accepted as non-action. |
| AC10 | An unresolved entry belongs to shell UX, accessibility, visual/localization/RTL behavior, sample coverage, command palette, customization-gradient UX, or generated UI polish | Routing is updated | It is owned by Story 11.6 unless explicitly resolved or accepted as non-action. |
| AC11 | An unresolved entry belongs to EventStore, SignalR/reconnection, realtime reliability, telemetry/exporter guidance, CI/release governance, or release-readiness blockers | Routing is updated | It is owned by Story 11.7 unless explicitly resolved or accepted as non-action. |
| AC12 | A deferred item could fit more than one Story 11.x bucket | Routing is updated | The primary owner is selected and secondary affected stories are listed as "Related", not as competing owners. |
| AC13 | A deferred item is too large for one Story 11.x bucket | Reconciliation completes | The ledger records a split recommendation for Product, but still names a current owner story so the item is not ownerless. |
| AC14 | Reconciliation edits the ledger | The diff is reviewed | It does not remove original review notes, source labels, or dates except to replace exact duplicate text with an explicit cross-reference. |
| AC15 | Validation is complete | The story moves to review | The Dev Agent Record lists the grep/query checks used to prove vague unresolved owners and duplicate unresolved entries were handled. |

---

## Tasks / Subtasks

- [ ] T1. Build the ledger inventory (AC1-AC3, AC14)
  - [ ] Read `_bmad-output/implementation-artifacts/deferred-work.md` from top to bottom.
  - [ ] Treat each bullet under a `## Deferred from:` section as a ledger item unless it is clearly explanatory text.
  - [ ] Capture item id, section heading, date, original source labels, current owner text, resolved marker, and evidence path if present.
  - [ ] Do not reorder the source sections unless a small summary table is added at the top.

- [ ] T2. Define the reconciliation markers in the ledger (AC1-AC5)
  - [ ] Add or update a short "Reconciliation Status" subsection near the top of `deferred-work.md`.
  - [ ] Define the accepted marker shapes for `Owner: Story 11.x`, `Related: Story 11.y`, `Resolved YYYY-MM-DD`, `Superseded by`, and `Non-action decision`.
  - [ ] Keep marker wording simple enough that future agents can grep it without a custom parser.

- [ ] T3. Route unresolved items to concrete Story 11 owners (AC2, AC6-AC13)
  - [ ] Route diagnostic registry/docs/HFCM/docs-site governance items to Story 11.2.
  - [ ] Route CLI, migration, IDE, manifest, sidecar, and help/README edge cases to Story 11.3.
  - [ ] Route drift detection, SourceTools, generator, metadata drift, deterministic output, and analyzer coverage items to Story 11.4.
  - [ ] Route MCP schema, agent contract, skill corpus, fingerprint, and schema rejection items to Story 11.5.
  - [ ] Route shell UX, accessibility, localization/RTL, visual, sample, command-palette, and customization-gradient UX items to Story 11.6.
  - [ ] Route EventStore, SignalR, realtime reliability, telemetry/exporter, CI/release governance, and release blockers to Story 11.7.
  - [ ] Where an item crosses buckets, record one `Owner:` and one or more `Related:` references.

- [ ] T4. Resolve duplicate, superseded, and no-action entries (AC3-AC5, AC14)
  - [ ] Cross-reference duplicates instead of deleting original source rows.
  - [ ] Preserve checked/resolved entries and add missing evidence paths where the ledger already names the implementation file or story.
  - [ ] Convert genuinely accepted edge cases to `Non-action decision` only when the rationale is explicit in the existing review note.
  - [ ] Do not silently close an item just because it looks low risk; if uncertain, keep it unresolved and route it to a Story 11 owner.

- [ ] T5. Add validation evidence and handoff notes (AC1-AC15)
  - [ ] Add a compact reconciliation summary with counts by owner story and by state: unresolved, resolved, superseded, non-action.
  - [ ] Run focused searches for vague owner language (`future`, `follow-up`, `later`, `v1.x`) and verify remaining occurrences are either historical text with a new owner marker or accepted non-action decisions.
  - [ ] Run focused searches for duplicate ids and repeated resolved entries; document how each was handled.
  - [ ] Record validation commands in this story's Dev Agent Record.
  - [ ] Move Story 11.1 to `review` only after the ledger is reconciled and validation evidence is recorded.

---

## Dev Notes

### Current State

- Epic 11 was added by the 2026-05-10 sprint change proposal to route deferred work into release-readiness backlog stories.
- `_bmad-output/implementation-artifacts/deferred-work.md` already starts with a Backlog Routing Status mapping unresolved buckets to Stories 11.1-11.7.
- The detailed ledger still contains historical sections from 2026-04-14 through 2026-05-10. Many entries were written before Epic 11 existed, so old owner labels may name completed stories, "future", "v1.x", "follow-up", or broad concepts instead of current backlog owners.
- Several entries are already resolved with explicit 2026-05-10 notes. These should stay as evidence, not be reopened.

### Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Story 11.1 owns reconciliation and routing only, not implementation of deferred technical fixes. | Prevents scope creep and protects the single-purpose release-readiness backlog. Applies L06/L07. |
| D2 | Preserve historical review sections and source labels. | The ledger is an audit trail; deleting old rows would hide why a follow-up exists. |
| D3 | Every unresolved item must have one primary owner story. | Multiple owners create ambiguity; secondary impacts belong in `Related:` notes. |
| D4 | Resolved/superseded/non-action states require evidence or rationale. | Avoids false closure of release-readiness debt. |
| D5 | Vague owner language may remain only as historical quoted context if a concrete marker is added nearby. | Keeps old review wording intact while satisfying L10. |
| D6 | New backlog stories may be recommended but not created silently unless Product accepts the split. | Story 11.1 can identify oversized buckets, but backlog shape changes should be explicit. |

### Owner Routing Matrix

| Owner | Route deferred items about |
| --- | --- |
| Story 11.2 | Diagnostic registry, documentation governance, HFCM migration ids, diagnostic docs, schema validation, compatibility suppression, diagnostic prose/title quality. |
| Story 11.3 | CLI migration, IDE parity, strict manifest parsing, path normalization, sidecar behavior, write safety, README/help semantics. |
| Story 11.4 | Drift detection, SourceTools generator coverage, metadata drift tests, deterministic generated output, analyzer/diagnostic coverage gaps. |
| Story 11.5 | MCP schema negotiation, agent command/query contracts, skill corpus, fingerprints, schema rejection, tenant-scoped tool behavior. |
| Story 11.6 | Shell UX, accessibility, visual/localization/RTL checks, command palette, sample coverage, customization-gradient adopter guidance. |
| Story 11.7 | EventStore/realtime reliability, SignalR/reconnection, telemetry/exporter guidance, CI/release governance, release-readiness blockers. |

### Implementation Guidance

- Prefer small inline marker additions to each ledger row over rewriting the whole document.
- If a top summary table is added, it must be treated as an index. The canonical evidence remains in the historical row.
- Use exact story keys such as `11-2-diagnostic-registry-and-documentation-governance-follow-ups` where practical; short `Story 11.2` labels are acceptable in dense marker text if the routing section links the story names.
- Keep all source labels such as `Sources: blind`, `Sources: edge`, `Sources: auditor`, and original code path references.
- If an item already says "Owner: Story 9-x" and that story is done, add a Story 11.x owner marker rather than treating the old completed story as active ownership.
- Do not update production files just because ledger rows reference them. Those technical patches belong to later Epic 11 stories.

### Validation Strategy

Use documentation-focused validation rather than product test suites unless implementation unexpectedly adds tooling:

- Search for unresolved vague owner language: `future`, `follow-up`, `later`, `v1.x`, `TBD`, `ownerless`.
- Search for old completed-story owner labels such as `Owner: Story 9-`, `Owner: Story 8-`, and verify a Story 11.x owner marker exists nearby.
- Search for resolved entries without evidence: `Resolved` rows should include a date and source path/story reference.
- Search for duplicated ids: ids such as `DEF-9-4-C1` and known repeated review findings should cross-reference their canonical row.
- Review `git diff -- _bmad-output/implementation-artifacts/deferred-work.md` to ensure original evidence was preserved.

### Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update | Primary ledger reconciliation artifact. |
| `_bmad-output/implementation-artifacts/11-1-deferred-work-ledger-reconciliation-and-ownership.md` | Update | Record Dev Agent validation evidence during implementation. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Update | Move Story 11.1 to `review` only after implementation. |
| `_bmad-output/planning-artifacts/epics/epic-11-deferred-hardening-release-readiness.md` | Avoid unless needed | Only update if Product accepts a story split or routing correction. |
| `_bmad-output/process-notes/story-creation-lessons.md` | Optional | Add a new lesson only if reconciliation reveals a reusable process rule not covered by L10. |

### Project Structure Notes

- This is a file-system tracked BMAD project. Story status lives in `_bmad-output/implementation-artifacts/sprint-status.yaml`.
- Story artifacts live directly under `_bmad-output/implementation-artifacts` unless a folder-based story already exists.
- Completed Epics 1-10 should remain historical. Epic 11 is the active backlog owner for carry-forward work.
- Root-level submodules exist, but this story should not initialize or update submodules.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Deferred-work ledger | Stories 11.2-11.7 | Ledger rows must route unresolved technical work to the correct release-readiness story. |
| Story 11.1 | Product owner | Oversized or ambiguous buckets must surface as split recommendations instead of silent backlog changes. |
| Story 11.1 | Future dev/review agents | Resolved, superseded, and non-action decisions must preserve evidence and original review provenance. |
| Process lesson L10 | Story 11.1 | Every deferral needs a story-specific owner or an explicit non-action/resolved decision. |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Actual fixes for diagnostic/docs governance items. | Story 11.2 |
| Actual fixes for CLI/IDE edge cases. | Story 11.3 |
| Actual fixes for drift detection and SourceTools coverage gaps. | Story 11.4 |
| Actual fixes for MCP/agent contract hardening. | Story 11.5 |
| Actual fixes for shell UX/accessibility/sample follow-ups. | Story 11.6 |
| Actual fixes for EventStore/realtime/CI/release reliability follow-ups. | Story 11.7 |
| Additional Epic 11 splits if a bucket is too large after reconciliation. | Product owner decision after Story 11.1 inventory evidence |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-11-deferred-hardening-release-readiness.md#Story-11.1`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#Backlog-Routing-Status-2026-05-10`] - current Epic 11 bucket routing.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-10.md`] - Correct Course rationale for adding Epic 11 instead of reopening Epics 1-10.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - primary process lesson for deferral ownership.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope control for not implementing all downstream fixes here.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08--Party-review-vs-elicitation--different-roles`] - this story should still receive later party review and elicitation hardening.
- [Source: `_bmad-output/project-context.md`] - project rules for workflow, submodules, evidence, and documentation safety.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

- 2026-05-10: Story created via `/bmad-create-story 11-1-deferred-work-ledger-reconciliation-and-ownership` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### Change Log

- 2026-05-10: Created Story 11.1 and marked ready-for-dev.

### File List

- `_bmad-output/implementation-artifacts/11-1-deferred-work-ledger-reconciliation-and-ownership.md`
