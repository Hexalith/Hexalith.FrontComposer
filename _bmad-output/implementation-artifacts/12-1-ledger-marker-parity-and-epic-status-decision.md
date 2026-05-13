# Story 12.1: Ledger Marker Parity and Epic Status Decision

Status: ready-for-dev

> **Epic 12** - Release Certification and Evidence Alignment. This story turns the Epic 11 retrospective finding into a row-addressable release-readiness decision. It applies lessons **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Epic 11 completed all seven implementation stories, but the release-readiness evidence does not yet match the detailed deferred-work ledger. The current sprint status keeps `epic-11` in progress because stale or unresolved `Reconciliation:` markers still name completed Story 11.x owners.

Story 12.1 is the narrow parity pass. It audits current deferred-work markers for Stories 11.2, 11.4, and 11.5, converts each remaining current marker to an explicit final state or named release gate, and records the decision for the top-level `epic-11` status. This story does not implement MCP contract work, EventStore provider behavior, trusted release publishing, or accessibility evidence packs; those remain with Stories 12.2 through 12.5.

---

## Story

As a release owner,
I want current deferred-work ledger markers reconciled with completed story evidence,
so that top-level epic status reflects the real release-readiness state.

### Release-Readiness Job To Preserve

A release owner should be able to inspect `_bmad-output/implementation-artifacts/deferred-work.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, and the Story 12.1 Dev Agent Record and know whether Epic 11 is truly done, still blocked by named row-level release gates, or done with explicitly accepted constraints.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary ledger input | `_bmad-output/implementation-artifacts/deferred-work.md` is the source of truth for current row state. Do not infer closure from story status alone. |
| Starting audit target | At story creation, current row markers are: Story 11.2 = 2 rows (`DW-0057`, `DW-0064`), Story 11.4 = 7 rows (`DW-0325`, `DW-0332`, `DW-0333`, `DW-0336`, `DW-0339`, `DW-0340`, `DW-0347`), Story 11.5 = 205 rows, plus `DW-0666` needing Product/Architecture decision. |
| Starting fingerprints | Story 11.2 row-id hash `sha256:2d2ecbb221a30d7a7893536177d2e457cea1f4fdf2f68603aacb8fe9f1147e34`; Story 11.4 row-id hash `sha256:834c8e8ab9c8bc6a509c66261ef7bd18d76a729f6bd4ce156fafedc247e21977`; Story 11.5 row-id hash `sha256:d0df4a8ff4f113e059c582a69ad2f67fa947d65d62baaff13190ce7d0c780e83`. |
| Final states | Every audited current marker becomes exactly one of: `Resolved`, `Accepted constraint`, `Split to named story`, `Superseded`, `Non-action decision`, or deliberately open release gate. |
| Story 11.5 boundary | Story 12.1 may route Story 11.5 MCP/schema rows to Story 12.2 through a row-addressable split matrix. Do not close MCP contract rows with a broad "Story 11.5 is done" claim. |
| Product/Architecture decision | `DW-0666` must be surfaced explicitly with owner, decision needed, evidence, downstream impact, and reopen/closure trigger. |
| Sprint status decision | Record whether `epic-11` moves to `done` or remains `in-progress`; if it remains open, name the blocking rows or release gates. |
| Scope guardrail | No production code, test code, docs-site generation, MCP implementation, EventStore provider implementation, release publishing, or accessibility verification unless needed only to write evidence/routing notes. |
| Validation | Re-run row-count and status-artifact consistency checks. For docs-only changes, `git diff --check` plus deterministic ledger scripts are sufficient unless implementation touches source or test files. |

Start here: T1 snapshot current ledger rows -> T2 compare against Epic 11 story evidence -> T3 classify Story 11.2 and 11.4 rows -> T4 produce row-addressable Story 11.5 routing to Story 12.2 or release gates -> T5 resolve `DW-0666` ownership -> T6 record Epic 11 status decision -> T7 validate counts and update Dev Agent Record.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | `_bmad-output/implementation-artifacts/deferred-work.md` contains current `Reconciliation:` markers for completed Story 11.x owners | Story 12.1 starts | The implementer records starting counts, ordered row IDs, and row-id fingerprints for Story 11.2, Story 11.4, Story 11.5, and Product/Architecture decision markers. |
| AC2 | Epic 11 story records and the retrospective contain completion evidence | The ledger audit compares current markers against story evidence | The comparison names which markers are stale, accepted constraints, split follow-ups, non-action decisions, or genuine open release gates. |
| AC3 | Current markers still name Story 11.2 | The rows are reconciled | `DW-0057` and `DW-0064` are converted to final states with evidence, owner, downstream impact, and reopen trigger; broad diagnostic-governance prose is not accepted as closure. |
| AC4 | Current markers still name Story 11.4 | The rows are reconciled | `DW-0325`, `DW-0332`, `DW-0333`, `DW-0336`, `DW-0339`, `DW-0340`, and `DW-0347` are converted to final states with evidence, owner, downstream impact, and reopen trigger. |
| AC5 | Current markers still name Story 11.5 | The rows are reconciled | All 205 Story 11.5 markers are either closed row by row or routed through an explicit row-addressable matrix to Story 12.2 or another named owner; no range-only or summary-only closure is accepted. |
| AC6 | A row is accepted as a v1 constraint | The ledger and story record are updated | The acceptance names owner, likelihood, impact, release risk, downstream consumer impact, evidence, expiry or review date, and reopen trigger. |
| AC7 | A row is split rather than fixed | The split is recorded | The destination is a named story, release gate, or architecture/product decision row; "future work", "Epic 12", or "later" alone is not sufficient. |
| AC8 | A row is a deliberately open release gate | The Epic 11 status decision is made | The gate names the blocking condition, owner, required evidence, release impact, and what event can close the gate. |
| AC9 | `DW-0666` remains a Product/Architecture decision marker | Story 12.1 completes | The row is surfaced as a named decision with policy options for UNC `//server/share` and drive-relative `C:foo` docs-slug handling, plus owner and evidence path. |
| AC10 | The ledger summary counts are stale or incomplete | The detailed rows are updated | The top-level reconciliation summary is updated so owner counts, release-certification routing, and detailed row markers agree. |
| AC11 | The implementation changes `_bmad-output/implementation-artifacts/sprint-status.yaml` | The Epic 11 decision is recorded | `epic-11` is either marked `done` with rationale or deliberately kept `in-progress` with named blocking rows/gates; Story 12.1 itself moves through the normal implementation status only when actual dev work occurs. |
| AC12 | Story 12.1 completes | Validation runs | A deterministic validation script reports zero current `Owner: Story 11.2`, `Owner: Story 11.4`, and stale `Owner: Story 11.5` markers unless open markers are explicitly named release gates; status-artifact consistency still has no drift. |
| AC13 | The story record is updated | The Dev Agent Record is completed | It lists changed files, validation commands and outcomes, final counts/fingerprints, deferred decisions, and whether `epic-11` was marked done or left in progress. |

---

## Tasks / Subtasks

- [ ] T1. Capture starting ledger inventory (AC1, AC12)
  - [ ] Read `_bmad-output/implementation-artifacts/deferred-work.md` from top to bottom before edits.
  - [ ] Count current `Reconciliation:` markers by owner using detailed bullet rows only, not top-level prose or resolution-summary sections.
  - [ ] Record ordered row IDs and SHA-256 fingerprints for Story 11.2, Story 11.4, Story 11.5, and `Needs Product/Architecture decision` rows.
  - [ ] Save the script or command transcript in the Dev Agent Record so the audit can be repeated.

- [ ] T2. Compare ledger state to Epic 11 evidence (AC2)
  - [ ] Review `_bmad-output/implementation-artifacts/epic-11-retro-2026-05-13.md`.
  - [ ] Review `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13.md`.
  - [ ] Review Story 11.2, 11.4, and 11.5 Dev Agent Records for row closure claims, validation commands, and accepted constraints.
  - [ ] Identify contradictions between detailed row markers and story completion notes.

- [ ] T3. Reconcile Story 11.2 and Story 11.4 current markers (AC3, AC4, AC6, AC7)
  - [ ] Convert `DW-0057` and `DW-0064` to explicit final states.
  - [ ] Convert `DW-0325`, `DW-0332`, `DW-0333`, `DW-0336`, `DW-0339`, `DW-0340`, and `DW-0347` to explicit final states.
  - [ ] For accepted constraints, include likelihood, impact, release risk, downstream impact, owner, evidence, and reopen trigger.
  - [ ] For splits, name the exact downstream story or decision owner and why Story 12.1 is not implementing the work.

- [ ] T4. Route Story 11.5 current markers without false closure (AC5, AC6, AC7, AC8)
  - [ ] Build a row-addressable closure/routing matrix for all 205 Story 11.5 current markers.
  - [ ] Prefer category grouping only when every row ID in the category is explicitly listed and inherits the same owner, evidence class, downstream impact, and reopen trigger.
  - [ ] Route MCP/schema contract snapshot decisions to Story 12.2 when implementation evidence is still needed.
  - [ ] Do not edit MCP source/tests in this story unless a row can be safely closed by evidence-only wording already present in repository artifacts.

- [ ] T5. Surface Product/Architecture decision row `DW-0666` (AC9)
  - [ ] Name the decision: whether docs slug validation should reject UNC-like `//server/share` and drive-relative `C:foo` forms.
  - [ ] Record policy options, recommended default if evidence supports one, owner, evidence path, downstream impact, and closure trigger.
  - [ ] Ensure the row no longer hides inside a broad diagnostic-governance bucket.

- [ ] T6. Record Epic 11 top-level status decision (AC8, AC10, AC11)
  - [ ] Update the deferred-work top summary to match detailed row counts.
  - [ ] Decide whether `epic-11` should move from `in-progress` to `done`.
  - [ ] If `epic-11` remains `in-progress`, name the exact blocking rows or release gates.
  - [ ] If `epic-11` is marked `done`, record why remaining Epic 12 work is release certification rather than Epic 11 incompletion.

- [ ] T7. Validate and record completion evidence (AC12, AC13)
  - [ ] Re-run the row inventory script after edits and record final counts/fingerprints.
  - [ ] Re-run sprint status YAML parse and status-artifact consistency checks.
  - [ ] Run `git diff --check`.
  - [ ] Update this story's Dev Agent Record with validation, changed files, final status decision, and residual release gates.

---

## Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Detailed `Reconciliation:` markers are the source of truth, not story status. | The Epic 11 retrospective found completed story statuses can hide stale row state. |
| D2 | Story 12.1 is an evidence-alignment story, not a new implementation sweep. | Keeps release certification bounded and prevents another broad hardening epic from forming implicitly. |
| D3 | Row-addressable routing is required even when category grouping is used. | Story 11.5 previously showed that range-only closure can diverge from detailed row markers. |
| D4 | Accepted constraints are release decisions and must carry full metadata. | Owner, risk, downstream impact, evidence, and reopen trigger make a constraint auditable. |
| D5 | Story 11.5 MCP contract closure belongs to Story 12.2 unless existing evidence already proves closure. | Story 12.2 exists specifically to reconcile MCP ledger closure and contract snapshot decisions. |
| D6 | `epic-11` status must communicate readiness truth, not bookkeeping neatness. | Moving the epic to done is valid only if remaining work is clearly release certification rather than unresolved Epic 11 scope. |
| D7 | Product/Architecture decisions must be explicit rows, not hidden in prose. | `DW-0666` requires a named policy decision before docs-slug closure is honest. |
| D8 | No recursive nested submodule commands are needed for this story. | The work is repository artifact reconciliation; root-level submodule policy remains unchanged. |

---

## Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update | Primary ledger reconciliation, row final states, top summary, and Epic 11 status rationale. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Update possible during implementation | Only change `epic-11` if Story 12.1's evidence decision supports it; normal story status transitions happen through dev workflow. |
| `_bmad-output/implementation-artifacts/12-1-ledger-marker-parity-and-epic-status-decision.md` | Update | Dev Agent Record, validation evidence, file list, and completion notes. |
| `_bmad-output/process-notes/story-creation-lessons.md` | Update only if new reusable lesson emerges | Append-only; do not rewrite existing lessons. |

No production source, test source, docs generated site, release workflow, MCP implementation, EventStore implementation, or submodule content should be changed for this story unless a later human decision expands scope.

---

## Project Structure Notes

- Implementation artifacts live under `_bmad-output/implementation-artifacts`; planning artifacts live under `_bmad-output/planning-artifacts`.
- Keep row IDs stable. Do not delete historical deferred-work bullets; append or replace only the current `Reconciliation:` marker text needed to express the final state.
- Use repository-relative evidence paths. Do not paste local absolute paths, raw logs, secrets, tokens, tenant/user values, command payloads, or unbounded output into the ledger.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`. Do not initialize or update nested submodules.

---

## Testing Strategy

- Use a deterministic row-inventory script that:
  - parses only detailed bullet rows containing `Reconciliation:`;
  - counts current `Owner: Story 11.2`, `Owner: Story 11.4`, `Owner: Story 11.5`, and `Needs Product/Architecture decision`;
  - emits ordered row IDs and SHA-256 hashes;
  - fails if a detailed row has no `Row: DW-####` marker.
- Re-run the YAML/status-artifact consistency check used by the recurring hardening job.
- Run `git diff --check` before review.
- Run broader `dotnet test` only if implementation touches source, test, workflow, or validation script code. For ledger/status-only edits, command evidence plus deterministic row scripts are the relevant validation.

---

## Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 11.1 ledger reconciliation | Story 12.1 | Row IDs and `Reconciliation:` markers define the current deferred-work state machine. |
| Stories 11.2, 11.4, 11.5 | Story 12.1 | Completion evidence must be compared against the still-current ledger markers before Epic 11 status can be decided. |
| Story 12.1 | Story 12.2 | MCP/Story 11.5 rows that still require contract snapshot decisions must be routed row-addressably, not left under completed Story 11.5 ownership. |
| Story 12.1 | Stories 12.3-12.5 | Provider-backed EventStore, trusted release evidence, accessibility, and stakeholder acceptance gates remain separate release-certification work. |
| Sprint status | Release owner | `epic-11` top-level status must match the recorded ledger/evidence decision. |

---

## Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| MCP ledger closure and contract snapshot decisions beyond evidence routing. | Story 12.2 |
| Provider-backed pending-command status release gate. | Story 12.3 |
| Trusted release-context signing, SBOM, attestation, package inventory, and dry-run evidence. | Story 12.4 |
| Manual accessibility, localization/RTL/AT, stakeholder acceptance evidence. | Story 12.5 |
| Docs slug UNC and drive-relative policy if `DW-0666` needs product/architecture sign-off before closure. | Jerome, Product Owner, and Architect decision row |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-12-release-certification-evidence-alignment.md#Story-12.1`] - story statement, acceptance criteria, and Epic 12 release-certification scope.
- [Source: `_bmad-output/implementation-artifacts/epic-11-retro-2026-05-13.md`] - retrospective findings, current marker counts, and Epic 11 status caution.
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13.md`] - approved course correction and Epic 12 creation rationale.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md`] - row-level deferred-work ledger and current `Reconciliation:` marker source of truth.
- [Source: `_bmad-output/implementation-artifacts/11-2-diagnostic-registry-and-documentation-governance-follow-ups.md`] - diagnostic governance completion evidence and unresolved decisions.
- [Source: `_bmad-output/implementation-artifacts/11-4-drift-detection-and-source-generator-coverage-hardening.md`] - SourceTools drift/generator completion evidence and accepted constraints.
- [Source: `_bmad-output/implementation-artifacts/11-5-mcp-schema-negotiation-and-agent-contract-hardening.md`] - MCP schema negotiation completion evidence and row-scoped closure notes.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope and decision budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L07--Test-count-inflation-is-a-cost`] - test-scope budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08--Party-review-vs-elicitation--different-roles`] - later party review and elicitation sequencing.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - named owner requirement.
- [Source: `_bmad-output/project-context.md`] - project rules for evidence, diagnostics, release governance, redaction, and submodules.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-13: Story created via `/bmad-create-story 12-1-ledger-marker-parity-and-epic-status-decision` during recurring pre-dev hardening job.
- 2026-05-13: Pre-creation audit parsed `sprint-status.yaml`, confirmed status-artifact consistency had no drift across 97 development entries, found ready buffer count 0, and selected the first backlog story `12-1-ledger-marker-parity-and-epic-status-decision`.
- 2026-05-13: Starting detailed ledger-marker audit found Story 11.2 = 2 rows, Story 11.4 = 7 rows, Story 11.5 = 205 rows, and Product/Architecture decision = 1 row (`DW-0666`).

### Completion Notes List

- 2026-05-13: Created the Story 12.1 developer guide and marked it ready for development. Ready for party-mode review on a later recurring run.

### Change Log

- 2026-05-13: Created Story 12.1 and marked ready-for-dev.

### File List

- `_bmad-output/implementation-artifacts/12-1-ledger-marker-parity-and-epic-status-decision.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
