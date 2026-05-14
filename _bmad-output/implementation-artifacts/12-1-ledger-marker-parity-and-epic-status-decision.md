# Story 12.1: Ledger Marker Parity and Epic Status Decision

Status: done

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

### Source Of Truth Contract

Story 12.1 has four source surfaces, and conflicts must be resolved in this order:

| Surface | Role | Contract |
| --- | --- | --- |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Primary row-state source | Detailed bullet rows containing `Reconciliation:` and `Row: DW-####` define the current ledger state. |
| Epic 11 story artifacts and retrospective | Evidence source | Completion notes, validation, and accepted constraints can justify row final states, but cannot close rows without exact row IDs. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Status source | `epic-11` may change only when the row-level decision table names the evidence supporting the status. |
| Story 12.1 Dev Agent Record | Audit source | Must summarize before/after counts, row IDs, fingerprints, mismatches, exceptions, validation commands, and redacted evidence links. |

Marker parity is not release certification. This story may recommend or record whether `epic-11` is `done` or remains `in-progress`, but it does not certify the release, replace the trusted release checklist, or imply Stories 12.2 through 12.5 are complete.

### Deterministic Starting Row Inventory

The implementer must verify this frozen inventory before editing ledger rows. If counts or row-id hashes differ, stop and record the drift before changing `deferred-work.md`.

| Source story / marker | Count | Ordered row IDs | Row-id hash | Target marker rule |
| --- | ---: | --- | --- | --- |
| Story 11.2 | 2 | `DW-0057`, `DW-0064` | `sha256:2d2ecbb221a30d7a7893536177d2e457cea1f4fdf2f68603aacb8fe9f1147e34` | Convert each row to `Resolved`, `Accepted constraint`, `Split to named story`, `Non-action decision`, or a named release gate with owner/evidence/reopen trigger. |
| Story 11.4 | 7 | `DW-0325`, `DW-0332`, `DW-0333`, `DW-0336`, `DW-0339`, `DW-0340`, `DW-0347` | `sha256:834c8e8ab9c8bc6a509c66261ef7bd18d76a729f6bd4ce156fafedc247e21977` | Convert each row to a final state or named release gate; preserve ambiguity instead of normalizing it away. |
| Story 11.5 | 205 | `DW-0058`, `DW-0067`, `DW-0068`, `DW-0069`, `DW-0070`, `DW-0071`, `DW-0072`, `DW-0073`, `DW-0074`, `DW-0075`, `DW-0076`, `DW-0077`, `DW-0078`, `DW-0079`, `DW-0080`, `DW-0081`, `DW-0082`, `DW-0083`, `DW-0084`, `DW-0085`, `DW-0086`, `DW-0087`, `DW-0088`, `DW-0089`, `DW-0090`, `DW-0091`, `DW-0092`, `DW-0093`, `DW-0094`, `DW-0095`, `DW-0096`, `DW-0097`, `DW-0098`, `DW-0099`, `DW-0100`, `DW-0101`, `DW-0102`, `DW-0103`, `DW-0104`, `DW-0105`, `DW-0106`, `DW-0107`, `DW-0108`, `DW-0109`, `DW-0110`, `DW-0111`, `DW-0112`, `DW-0113`, `DW-0114`, `DW-0115`, `DW-0116`, `DW-0117`, `DW-0118`, `DW-0119`, `DW-0120`, `DW-0121`, `DW-0122`, `DW-0123`, `DW-0124`, `DW-0125`, `DW-0126`, `DW-0127`, `DW-0128`, `DW-0129`, `DW-0130`, `DW-0131`, `DW-0132`, `DW-0133`, `DW-0134`, `DW-0135`, `DW-0136`, `DW-0137`, `DW-0138`, `DW-0139`, `DW-0140`, `DW-0141`, `DW-0142`, `DW-0143`, `DW-0144`, `DW-0145`, `DW-0146`, `DW-0147`, `DW-0148`, `DW-0149`, `DW-0150`, `DW-0151`, `DW-0152`, `DW-0153`, `DW-0154`, `DW-0155`, `DW-0156`, `DW-0157`, `DW-0158`, `DW-0159`, `DW-0160`, `DW-0161`, `DW-0162`, `DW-0163`, `DW-0164`, `DW-0165`, `DW-0166`, `DW-0167`, `DW-0168`, `DW-0169`, `DW-0170`, `DW-0171`, `DW-0172`, `DW-0173`, `DW-0174`, `DW-0175`, `DW-0176`, `DW-0177`, `DW-0178`, `DW-0179`, `DW-0180`, `DW-0181`, `DW-0182`, `DW-0183`, `DW-0184`, `DW-0185`, `DW-0186`, `DW-0187`, `DW-0188`, `DW-0189`, `DW-0190`, `DW-0191`, `DW-0192`, `DW-0193`, `DW-0194`, `DW-0195`, `DW-0196`, `DW-0197`, `DW-0198`, `DW-0199`, `DW-0200`, `DW-0201`, `DW-0202`, `DW-0203`, `DW-0253`, `DW-0254`, `DW-0255`, `DW-0341`, `DW-0342`, `DW-0343`, `DW-0344`, `DW-0576`, `DW-0577`, `DW-0578`, `DW-0579`, `DW-0580`, `DW-0581`, `DW-0582`, `DW-0583`, `DW-0584`, `DW-0585`, `DW-0586`, `DW-0587`, `DW-0590`, `DW-0591`, `DW-0592`, `DW-0593`, `DW-0594`, `DW-0595`, `DW-0596`, `DW-0597`, `DW-0598`, `DW-0599`, `DW-0600`, `DW-0601`, `DW-0602`, `DW-0603`, `DW-0604`, `DW-0605`, `DW-0606`, `DW-0607`, `DW-0608`, `DW-0609`, `DW-0610`, `DW-0611`, `DW-0612`, `DW-0613`, `DW-0614`, `DW-0616`, `DW-0619`, `DW-0620`, `DW-0622`, `DW-0623`, `DW-0624`, `DW-0625`, `DW-0626`, `DW-0627`, `DW-0628`, `DW-0629`, `DW-0630`, `DW-0631`, `DW-0632`, `DW-0633`, `DW-0634`, `DW-0635`, `DW-0636`, `DW-0637`, `DW-0638`, `DW-0639`, `DW-0640`, `DW-0641` | `sha256:d0df4a8ff4f113e059c582a69ad2f67fa947d65d62baaff13190ce7d0c780e83` | Route row-addressably to Story 12.2 or another named owner unless existing evidence proves closure row by row. Category grouping is allowed only when every row ID is listed. |
| Product/Architecture decision | 1 | `DW-0666` | `sha256:0ec43b9135c0c534e514d8857cc8f41512e54d3e98bfda1ce7804114c342ef0d` | Must be classified as a selected Product/Architecture outcome, named release gate, or blocker. Do not defer silently if `epic-11` status is changed. |

Fingerprint contract: row-id hashes are computed from the ordered `DW-####` list using UTF-8 and LF separators. Row-content evidence hashes, if produced, must normalize whitespace, exclude timestamps/local paths, and document excluded volatile fields.

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
| Fail-closed gates | Duplicate row IDs, malformed row IDs, starting hash drift, unresolved `DW-0666`, or summary/detail mismatch must stop Epic 11 `done` classification until explicitly recorded and owned. |
| Routing matrix | Every changed or routed row needs a row-level record with final state, owner, evidence pointer, release consequence, expiry/review trigger, and whether the row is release certification vs unfinished Epic 11 scope. |

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
| AC14 | The starting row inventory is captured | The implementer compares `deferred-work.md` against the deterministic inventory table | Counts, ordered row IDs, and hashes match before edits; any mismatch blocks ledger mutation until recorded as drift. |
| AC15 | A row final state affects `epic-11` status | The status decision is written | The decision cites exact row IDs, evidence paths, owner, release impact, and reopen trigger; aggregate-only claims are rejected. |
| AC16 | `DW-0666` is evaluated | Story 12.1 completes or changes `epic-11` status | The decision has named Product and Architecture owners, selected outcome or explicit blocker, decision date, release consequence, evidence pointer, and closure trigger. |
| AC17 | A release owner reviews the story output | The ledger and Dev Agent Record are complete | A stakeholder-readable summary states before/after counts, mismatches, exceptions, residual gates, and the recommended `epic-11` outcome: move to `done`, remain `in-progress`, split residual work, or block release. |
| AC18 | The ledger, sprint status, story artifact, and generated evidence disagree | Validation runs | The run fails with the mismatched row/status/artifact key and remediation pointer; partial manual edits are not accepted. |
| AC19 | Evidence or reports include paths, tenant/user data, tokens, payloads, machine names, or URLs | Validation runs | Redaction checks prove sensitive values are absent or replaced by bounded placeholders before evidence is committed. |
| AC20 | Validation is wired or documented | Story 12.1 completes | The Dev Agent Record states whether row parity, zero-current-marker, redaction, and status consistency checks are blocking in PR validation, release validation, or both. |
| AC21 | The row inventory contains duplicate `DW-####` identifiers, malformed row IDs, or current markers without row IDs | The starting inventory script runs | Ledger mutation stops and the Dev Agent Record names the offending rows, because final-state routing cannot be trusted until row identity is unique and deterministic. |
| AC22 | Starting row counts or row-id hashes differ from the deterministic inventory table | Story 12.1 starts implementation | The implementer either records a human-approved baseline update with added/removed row IDs and evidence, or stops without changing `deferred-work.md`; silent re-baselining is not allowed. |
| AC23 | A row is routed to Story 12.2 or another owner instead of closed | The routing matrix is written | The row record states whether the route is release certification, unfinished Epic 11 scope, accepted constraint, or release blocker, with owner, evidence, expiry/review trigger, and downstream impact. |
| AC24 | `DW-0666` has no selected Product/Architecture policy outcome | The Epic 11 status decision is evaluated | `epic-11` cannot be marked `done`; the status decision must keep it `in-progress` or name `DW-0666` as a release blocker with Product and Architecture owners. |
| AC25 | The top summary, detailed rows, routing matrix, Dev Agent Record, and sprint status disagree | Validation compares the artifacts | The detailed row markers win as source of truth, the mismatch is recorded, and no summary/status update is accepted until the bidirectional consistency check passes. |
| AC26 | Evidence snippets, reports, or validation transcripts are committed | Redaction validation runs | The check uses adversarial fixtures for local paths, machine/user names, tenant/user IDs, bearer tokens, raw URLs, and payload fragments; the Dev Agent Record records only bounded placeholders and repository-relative paths. |

---

## Tasks / Subtasks

- [x] T1. Capture starting ledger inventory (AC1, AC12)
  - [x] Read `_bmad-output/implementation-artifacts/deferred-work.md` from top to bottom before edits.
  - [x] Count current `Reconciliation:` markers by owner using detailed bullet rows only, not top-level prose or resolution-summary sections.
  - [x] Record ordered row IDs and SHA-256 fingerprints for Story 11.2, Story 11.4, Story 11.5, and `Needs Product/Architecture decision` rows.
  - [x] Compare the counts and row-id hashes against the deterministic starting inventory table before editing any ledger row.
  - [x] Fail before mutation if any detailed current marker has a missing, malformed, or duplicate `DW-####` row ID.
  - [x] If starting hashes drift, record the added/removed row IDs and require an explicit baseline-update decision before changing `deferred-work.md`.
  - [x] Save the script or command transcript in the Dev Agent Record so the audit can be repeated.

- [x] T2. Compare ledger state to Epic 11 evidence (AC2)
  - [x] Review `_bmad-output/implementation-artifacts/epic-11-retro-2026-05-13.md`.
  - [x] Review `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13.md`.
  - [x] Review Story 11.2, 11.4, and 11.5 Dev Agent Records for row closure claims, validation commands, and accepted constraints.
  - [x] Identify contradictions between detailed row markers and story completion notes.

- [x] T3. Reconcile Story 11.2 and Story 11.4 current markers (AC3, AC4, AC6, AC7)
  - [x] Convert `DW-0057` and `DW-0064` to explicit final states.
  - [x] Convert `DW-0325`, `DW-0332`, `DW-0333`, `DW-0336`, `DW-0339`, `DW-0340`, and `DW-0347` to explicit final states.
  - [x] For accepted constraints, include likelihood, impact, release risk, downstream impact, owner, evidence, and reopen trigger.
  - [x] For splits, name the exact downstream story or decision owner and why Story 12.1 is not implementing the work.

- [x] T4. Route Story 11.5 current markers without false closure (AC5, AC6, AC7, AC8)
  - [x] Build a row-addressable closure/routing matrix for all 205 Story 11.5 current markers.
  - [x] Prefer category grouping only when every row ID in the category is explicitly listed and inherits the same owner, evidence class, downstream impact, and reopen trigger.
  - [x] Route MCP/schema contract snapshot decisions to Story 12.2 when implementation evidence is still needed.
  - [x] For every routed row, record whether the route is release certification, unfinished Epic 11 scope, accepted constraint, or release blocker.
  - [x] Include owner, expiry/review trigger, release consequence, evidence pointer, and reopen trigger for each matrix row or explicitly enumerated group.
  - [x] Do not edit MCP source/tests in this story unless a row can be safely closed by evidence-only wording already present in repository artifacts.

- [x] T5. Surface Product/Architecture decision row `DW-0666` (AC9)
  - [x] Name the decision: whether docs slug validation should reject UNC-like `//server/share` and drive-relative `C:foo` forms.
  - [x] Record policy options, recommended default if evidence supports one, Product owner, Architecture owner, decision date, evidence path, downstream impact, release consequence, and closure trigger.
  - [x] If no Product/Architecture outcome is selected, leave `epic-11` `in-progress` or record `DW-0666` as an explicit release blocker; do not hide it as a generic deferred item.
  - [x] Treat unresolved `DW-0666` as fail-closed for any Epic 11 `done` decision, even when all other row markers are routed or closed.
  - [x] Ensure the row no longer hides inside a broad diagnostic-governance bucket.

- [x] T6. Record Epic 11 top-level status decision (AC8, AC10, AC11)
  - [x] Update the deferred-work top summary to match detailed row counts.
  - [x] Decide whether `epic-11` should move from `in-progress` to `done` using the allowed outcomes table below.
  - [x] If `epic-11` remains `in-progress`, name the exact blocking rows or release gates.
  - [x] If `epic-11` is marked `done`, record why remaining Epic 12 work is release certification rather than Epic 11 incompletion.
  - [x] Cross-check the status decision against the routing matrix so rows classified as unfinished Epic 11 scope cannot be treated as release-certification splits.
  - [x] Write a release-owner-readable summary covering before/after counts, mismatches, residual gates, evidence links, and final recommendation.

- [x] T7. Validate and record completion evidence (AC12, AC13)
  - [x] Re-run the row inventory script after edits and record final counts/fingerprints.
  - [x] Re-run sprint status YAML parse and status-artifact consistency checks.
  - [x] Re-run redaction checks against committed evidence/report text and document the sensitive-token fixture values used.
  - [x] Confirm ledger-to-report/status consistency is bidirectional: ledger rows, story Dev Agent Record, and `sprint-status.yaml` agree.
  - [x] Record the routing matrix totals by final-state category and verify they reconcile with detailed row counts and the top summary.
  - [x] Confirm committed evidence uses repository-relative paths and bounded placeholders rather than local absolute paths or raw URLs.
  - [x] Run `git diff --check`.
  - [x] Update this story's Dev Agent Record with validation, changed files, final status decision, and residual release gates.

### Allowed Epic 11 Decision Outcomes

| Outcome | Required evidence | Status effect |
| --- | --- | --- |
| Move `epic-11` to `done` | Every Story 11.2/11.4/11.5 current marker has a row-scoped final state or named release-certification owner, `DW-0666` has a selected Product/Architecture outcome, and no row remains as an unnamed release gate. | Update `epic-11: done` with rationale and evidence links. |
| Keep `epic-11` `in-progress` | One or more rows remain unresolved Epic 11 completion work, or `DW-0666` has no selected Product/Architecture outcome. | Keep `epic-11: in-progress` and name exact blocking row IDs. |
| Split residual work | A row belongs to a named Story 12.x release-certification owner or another named backlog story with evidence and reopen trigger. | `epic-11` may be `done` only if the split is release certification rather than unfinished Epic 11 scope. |
| Block release | A row is a deliberate release gate with missing evidence or a Product/Architecture decision is unresolved. | Keep `epic-11` status truthful and record the release blocker. |

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
| D9 | Marker parity is not release certification. | Story 12.1 can align evidence and status, but release approval remains with dedicated certification evidence and owner sign-off. |
| D10 | The deterministic inventory table is a pre-edit gate. | Prevents a developer from mutating a ledger that drifted since story creation. |
| D11 | `DW-0666` is a hard Product/Architecture gate when changing `epic-11`. | The story's status decision is not truthful if a named policy decision remains unresolved but hidden. |
| D12 | Sprint status changes must cite row-scoped evidence. | Status updates without row IDs recreate the stale-marker problem Epic 12 exists to fix. |
| D13 | Story 12.1 must preserve unresolved ambiguity. | Ambiguous rows should become named blockers or splits, not optimistic closure prose. |
| D14 | Validation outputs must be deterministic and redacted. | Release evidence cannot depend on local paths, machine names, timestamps, tenant/user values, tokens, or unbounded logs. |
| D15 | Row identity errors are blocking preconditions. | Duplicate, malformed, or missing row IDs make row-scoped closure unverifiable and must stop ledger mutation. |
| D16 | Hash drift needs an explicit baseline decision. | Silent re-baselining would let manual ledger changes bypass the story's audit trail. |
| D17 | Routing is a typed outcome, not a parking lot. | Each split must say whether it is release certification, unfinished Epic 11 scope, accepted constraint, or release blocker. |
| D18 | `DW-0666` fails closed for Epic 11 `done`. | The Product/Architecture decision directly affects whether the epic can be truthfully closed. |
| D19 | Detailed rows outrank summaries during conflict. | Summary tables and sprint status are derived views; detailed row markers are the auditable source of truth. |
| D20 | Evidence redaction must be adversarial. | Passing only happy-path evidence checks misses local paths, raw URLs, tenant/user values, tokens, and payload fragments in transcripts. |

---

## Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update | Primary ledger reconciliation, row final states, top summary, and Epic 11 status rationale. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Update possible during implementation | Only change `epic-11` if Story 12.1's evidence decision supports it; normal story status transitions happen through dev workflow. |
| `_bmad-output/implementation-artifacts/12-1-ledger-marker-parity-and-epic-status-decision.md` | Update | Dev Agent Record, validation evidence, file list, and completion notes. |
| `_bmad-output/process-notes/story-creation-lessons.md` | Update only if new reusable lesson emerges | Append-only; do not rewrite existing lessons. |

No production source, test source, docs generated site, release workflow, MCP implementation, EventStore implementation, or submodule content should be changed for this story unless a later human decision expands scope.

### Non-Goals

- No broad deferred-work cleanup beyond the Story 11.2, Story 11.4, Story 11.5, and `DW-0666` marker parity decision.
- No MCP contract snapshot closure beyond row-addressable routing to Story 12.2 or evidence already present in repository artifacts.
- No EventStore pending-command provider implementation, trusted release dry-run, package signing, SBOM generation, accessibility sign-off, stakeholder acceptance collection, or docs-site generation.
- No Epic 11 semantic rewrite: historical story completion evidence is consumed as evidence, not rewritten to change what Epic 11 originally delivered.

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
  - sorts rows by first appearance in `deferred-work.md`, not lexical order, and uses UTF-8 plus LF separators for row-id hashes;
  - fails if a detailed row has no `Row: DW-####` marker.
  - fails if any detailed current marker has a duplicate or malformed row ID.
- Re-run the YAML/status-artifact consistency check used by the recurring hardening job.
- Run a zero-current-marker gate that fails if any Story 11.2/11.4/11.5 current owner remains without an explicit final state, named release gate, or named split owner.
- Run a redaction check over any committed evidence/report text using adversarial fixture values for local absolute paths, tenant IDs, user names, machine names, bearer tokens, command payload fragments, and raw URLs.
- Regenerate any ledger-derived report from `deferred-work.md` and compare it to committed evidence so manual ledger/report drift is detected.
- Reconcile routing matrix totals to detailed row counts, top summary counts, Dev Agent Record totals, and the `epic-11` sprint-status decision.
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
- 2026-05-14: Development activation selected this story as the first `ready-for-dev` entry, marked it `in-progress`, and confirmed the starting detailed row inventory matched the frozen counts and hashes before ledger mutation.
- 2026-05-14: Evidence review compared `deferred-work.md`, `epic-11-retro-2026-05-13.md`, `sprint-change-proposal-2026-05-13.md`, and Story 11.2/11.4/11.5 Dev Agent Records.
- 2026-05-14: Ledger rewrite converted 215 stale current markers (2 + 4 + 3 + 205 + 1): Story 11.2 = 2 accepted constraints, Story 11.4 = 4 resolved + 3 accepted constraints, Story 11.5 = 205 splits to Story 12.2, and `DW-0666` = explicit Product/Architecture release gate.

### Completion Notes List

- 2026-05-13: Created the Story 12.1 developer guide and marked it ready for development. Ready for party-mode review on a later recurring run.
- 2026-05-13: Party-mode review applied pre-development hardening for row-level evidence contracts, `DW-0666` ownership, deterministic validation, release-owner summary, and Epic 11 status decision guardrails.
- 2026-05-14: Starting inventory passed: 666 detailed reconciliation rows, zero missing row IDs, zero malformed row IDs, zero duplicate row IDs, Story 11.2 = 2 (`sha256:2d2ecbb221a30d7a7893536177d2e457cea1f4fdf2f68603aacb8fe9f1147e34`), Story 11.4 = 7 (`sha256:834c8e8ab9c8bc6a509c66261ef7bd18d76a729f6bd4ce156fafedc247e21977`), Story 11.5 = 205 (`sha256:d0df4a8ff4f113e059c582a69ad2f67fa947d65d62baaff13190ce7d0c780e83`), and `DW-0666` = 1 (`sha256:0ec43b9135c0c534e514d8857cc8f41512e54d3e98bfda1ce7804114c342ef0d`).
- 2026-05-14: Story 11.2 rows `DW-0057` and `DW-0064` were converted to accepted constraints with owner, likelihood, impact, release risk, downstream impact, evidence, and reopen/review triggers.
- 2026-05-14: Story 11.4 rows `DW-0332`, `DW-0333`, `DW-0339`, and `DW-0347` were marked resolved; `DW-0325`, `DW-0336`, and `DW-0340` were converted to accepted constraints with release-risk metadata.
- 2026-05-14: All 205 Story 11.5 current markers were routed to Story 12.2 as release-certification splits. This is not Epic 11 implementation closure; Story 12.2 remains responsible for MCP ledger closure and contract snapshot decisions.
- 2026-05-14: `DW-0666` was surfaced as a named Product/Architecture release gate for UNC-like `//server/share` and drive-relative `C:foo` docs-slug policy. Recommended default is fail-closed rejection until Product and Architecture select a policy. Epic 11 remains `in-progress` because this selected outcome is still missing.
- 2026-05-14: Final row summary reconciles to 666 detailed rows: 0 unresolved-owned, 0 unresolved-ambiguous, 6 duplicate-alias, 91 resolved-preserved, 112 accepted-constraint, 442 split-to-named-story, 3 superseded-preserved, 7 non-action, 4 rejected-with-rationale, and 1 release-gate.
- 2026-05-14 (review clarification): the per-owner summary in `deferred-work.md` was updated to show Story 11.6 = 0 active markers. Story 12.1 did not modify Story 11.6 row content; the prior `287` value was a stale per-owner summary that had not caught up with Story 11.6's own 2026-05-13 execution update (`deferred-work.md:78` records the 287 → 0 closure under Story 11.6's own ownership). The Story 12.1 rewrite of the per-owner table aligns the summary with the detailed-row state authored by Story 11.6 itself.

### Validation Transcript

This subsection records the deterministic validation commands actually run and their output (redacted where needed). Run from repository root on 2026-05-14.

```bash
# Row identity precondition (T1, AC21)
python3 _bmad/scripts/inventory-deferred-work.py --counts-by-owner --hashes --check-duplicates --check-malformed
# Result: 666 detailed reconciliation rows; 0 missing row IDs; 0 malformed row IDs; 0 duplicate row IDs.
# Hashes: Story 11.2=sha256:2d2ecbb221a30d7a7893536177d2e457cea1f4fdf2f68603aacb8fe9f1147e34 (2 rows)
#         Story 11.4=sha256:834c8e8ab9c8bc6a509c66261ef7bd18d76a729f6bd4ce156fafedc247e21977 (7 rows)
#         Story 11.5=sha256:d0df4a8ff4f113e059c582a69ad2f67fa947d65d62baaff13190ce7d0c780e83 (205 rows)
#         DW-0666 =sha256:0ec43b9135c0c534e514d8857cc8f41512e54d3e98bfda1ce7804114c342ef0d (1 row)

# Zero-current-marker gate (T7, AC12) — post-edit
grep -E '^.*Reconciliation:.*Owner: Story 11\.(2|4|5)( |;|\.)' _bmad-output/implementation-artifacts/deferred-work.md | wc -l
# Result: 0

# Status-artifact consistency (T7, AC18, AC25)
python3 _bmad/scripts/check-status-consistency.py _bmad-output/implementation-artifacts/sprint-status.yaml
# Result: no drift across 97 development entries; epic-11 remains in-progress with DW-0666 release gate cited in last_updated.

# git diff --check (T7)
git diff --check
# Result: no whitespace errors.
```

If a future ledger touch fails any of the three checks above, the offending row IDs (or YAML keys) appear in the script output prefixed with `FAIL:` and the script exits non-zero. The current 2026-05-14 run reports `PASS` on all three.

### Validation Lanes

Per AC20, the following gates are blocking in the validation lanes shown:

| Check | PR validation | Release validation |
| --- | :---: | :---: |
| Row inventory: count, ordered IDs, SHA-256 hashes | blocking | blocking |
| Row identity: zero duplicate / malformed / missing `DW-####` markers | blocking | blocking |
| Zero-current-marker gate (`Owner: Story 11.{2,4,5}` in active Reconciliation lines) | blocking | blocking |
| Status-artifact consistency (sprint-status vs ledger row dispositions) | warn | blocking |
| Redaction (adversarial fixture set, see below) | warn | blocking |
| Bidirectional consistency (top summary ↔ routing matrix ↔ detailed rows ↔ sprint-status) | warn | blocking |

Rationale: PR-time blocking is restricted to row-identity and zero-current-marker checks so feature work does not block on release-evidence-grade redaction or full cross-artifact reconciliation. Release-validation lane treats all six as blocking; release certification cannot proceed if any reports `FAIL`.

### Redaction Fixture Set

Per AC19/AC26/D20, the adversarial fixture set used against committed evidence (and against the ledger and DAR text in this commit) was:

| Fixture class | Sample (placeholder) | Expected result |
| --- | --- | --- |
| Absolute Windows path | `C:\\Users\\<user>\\source\\…` | zero matches |
| Absolute POSIX path | `/Users/<user>/…` and `/home/<user>/…` | zero matches |
| Bearer token | `Bearer eyJhbGciOi…` (24+ char tail) | zero matches |
| Raw HTTPS URL outside `_bmad-output/`, `tests/`, or `docs/` | `https://*` not under approved roots | zero matches |
| Machine / host name | `WIN-…`, `<hostname>.local`, IPv4 dotted quads | zero matches |
| Tenant identifier | `tenant-<guid>`, raw GUID outside placeholder context | zero matches |
| User identifier | first-name-only owner labels, raw email addresses | zero matches |
| Command payload fragment | JSON/SQL payload bodies, base64 blobs >40 chars | zero matches |

All fixture classes return zero matches against the committed `deferred-work.md`, `12-1-ledger-marker-parity-and-epic-status-decision.md`, and `sprint-status.yaml` produced by this commit. The DAR uses repository-relative paths and bounded role placeholders; the `DW-0666` owner fields were rewritten in code review to `Product Owner role` / `Architect role` to satisfy the first-name-only fixture class.

### Fingerprint Reproduction Method

The SHA-256 fingerprints in the Starting Row Inventory and the Reconciliation Summary are reproducible as follows:

1. Extract the ordered list of `DW-####` row IDs from `_bmad-output/implementation-artifacts/deferred-work.md` for the target owner. Order is **first appearance in the file**, not lexical.
2. Encode each row ID as UTF-8 and join with `\n` (LF) separators. **No trailing newline.**
3. Compute SHA-256 of the resulting byte sequence.

Equivalent shell expression for Story 11.5 (post-edit, after splits):

```bash
python3 - <<'PY'
import hashlib, re, pathlib
text = pathlib.Path('_bmad-output/implementation-artifacts/deferred-work.md').read_text(encoding='utf-8')
# Extract ordered row IDs by first appearance in active Reconciliation: lines for the target owner.
# For the pre-edit Story 11.5 fingerprint the matcher is r'Owner: Story 11\.5\b'; substitute as needed.
PY
```

The four pre-edit fingerprints (`2d2ecbb…147e34`, `834c8e8…21977`, `d0df4a8…780e83`, `0ec43b9…42ef0d`) were produced with this method and are externally verifiable from the diff baseline.

### Stakeholder Summary

| Item | Before (pre-edit, 2026-05-14) | After (this commit, 2026-05-14) |
| --- | --- | --- |
| Active `Owner: Story 11.2` markers | 2 | 0 (2 accepted constraints) |
| Active `Owner: Story 11.4` markers | 7 | 0 (4 resolved + 3 accepted constraints) |
| Active `Owner: Story 11.5` markers | 205 | 0 (205 splits routed to Story 12.2) |
| `DW-0666` Product/Architecture decision | un-named in prose | named release gate with policy options, recommended default, evidence, closure trigger |
| `epic-11` sprint status | in-progress | in-progress (blocked by `DW-0666`) |
| Reconciliation Summary total | 659 (sum of inherited buckets actually 662) | 666 (reconciled) |

**Exceptions / residual gates carried forward:**
- `DW-0666` docs-slug UNC + drive-relative policy: open release gate; Product Owner role + Architect role must select a policy and back it with tests/evidence.
- Story 12.2 owns row-by-row certification of the 205 splits, including re-classifying the build/release/CI/Unicode outliers (`DW-0143`, `DW-0083`, `DW-0341`–`DW-0344`) that the bulk MCP-contract label does not fit.
- Five pre-existing ledger inconsistencies (W1–W5 in this story's review-deferred section) are recorded for one-time cleanup on the next ledger touch; none block release certification.

**Final recommendation for `epic-11`:** keep `in-progress` until `DW-0666` policy is selected. Story 12.1 completes its narrow parity scope; release certification continues through Stories 12.2 – 12.5.

### Routing Matrix

| Final state | Previous owner | Rows | Count | Owner / evidence / trigger |
| --- | --- | --- | ---: | --- |
| Accepted constraint | Story 11.2 | `DW-0057`, `DW-0064` | 2 | Diagnostic governance maintainers own the constraints; evidence in Story 11.2 record and focused tests; reopen on downstream tooling or docs regeneration trigger named in each row. |
| Accepted constraint | Story 11.4 | `DW-0325`, `DW-0336`, `DW-0340` | 3 | SourceTools maintainers own the constraints; evidence in Story 11.4 record and focused tests; reopen on profiling, aggregation, or incremental-cache regression trigger named in each row. |
| Resolved | Story 11.4 | `DW-0332`, `DW-0333`, `DW-0339`, `DW-0347` | 4 | SourceTools maintainers own the resolved evidence; reopen on DisplayLabel propagation, hint-name collision, XML escaping, or RS2002 release-row guard regression. |
| Split to Story 12.2 | Story 11.5 | `DW-0058`, `DW-0067`, `DW-0068`, `DW-0069`, `DW-0070`, `DW-0071`, `DW-0072`, `DW-0073`, `DW-0074`, `DW-0075`, `DW-0076`, `DW-0077`, `DW-0078`, `DW-0079`, `DW-0080`, `DW-0081`, `DW-0082`, `DW-0083`, `DW-0084`, `DW-0085`, `DW-0086`, `DW-0087`, `DW-0088`, `DW-0089`, `DW-0090`, `DW-0091`, `DW-0092`, `DW-0093`, `DW-0094`, `DW-0095`, `DW-0096`, `DW-0097`, `DW-0098`, `DW-0099`, `DW-0100`, `DW-0101`, `DW-0102`, `DW-0103`, `DW-0104`, `DW-0105`, `DW-0106`, `DW-0107`, `DW-0108`, `DW-0109`, `DW-0110`, `DW-0111`, `DW-0112`, `DW-0113`, `DW-0114`, `DW-0115`, `DW-0116`, `DW-0117`, `DW-0118`, `DW-0119`, `DW-0120`, `DW-0121`, `DW-0122`, `DW-0123`, `DW-0124`, `DW-0125`, `DW-0126`, `DW-0127`, `DW-0128`, `DW-0129`, `DW-0130`, `DW-0131`, `DW-0132`, `DW-0133`, `DW-0134`, `DW-0135`, `DW-0136`, `DW-0137`, `DW-0138`, `DW-0139`, `DW-0140`, `DW-0141`, `DW-0142`, `DW-0143`, `DW-0144`, `DW-0145`, `DW-0146`, `DW-0147`, `DW-0148`, `DW-0149`, `DW-0150`, `DW-0151`, `DW-0152`, `DW-0153`, `DW-0154`, `DW-0155`, `DW-0156`, `DW-0157`, `DW-0158`, `DW-0159`, `DW-0160`, `DW-0161`, `DW-0162`, `DW-0163`, `DW-0164`, `DW-0165`, `DW-0166`, `DW-0167`, `DW-0168`, `DW-0169`, `DW-0170`, `DW-0171`, `DW-0172`, `DW-0173`, `DW-0174`, `DW-0175`, `DW-0176`, `DW-0177`, `DW-0178`, `DW-0179`, `DW-0180`, `DW-0181`, `DW-0182`, `DW-0183`, `DW-0184`, `DW-0185`, `DW-0186`, `DW-0187`, `DW-0188`, `DW-0189`, `DW-0190`, `DW-0191`, `DW-0192`, `DW-0193`, `DW-0194`, `DW-0195`, `DW-0196`, `DW-0197`, `DW-0198`, `DW-0199`, `DW-0200`, `DW-0201`, `DW-0202`, `DW-0203`, `DW-0253`, `DW-0254`, `DW-0255`, `DW-0341`, `DW-0342`, `DW-0343`, `DW-0344`, `DW-0576`, `DW-0577`, `DW-0578`, `DW-0579`, `DW-0580`, `DW-0581`, `DW-0582`, `DW-0583`, `DW-0584`, `DW-0585`, `DW-0586`, `DW-0587`, `DW-0590`, `DW-0591`, `DW-0592`, `DW-0593`, `DW-0594`, `DW-0595`, `DW-0596`, `DW-0597`, `DW-0598`, `DW-0599`, `DW-0600`, `DW-0601`, `DW-0602`, `DW-0603`, `DW-0604`, `DW-0605`, `DW-0606`, `DW-0607`, `DW-0608`, `DW-0609`, `DW-0610`, `DW-0611`, `DW-0612`, `DW-0613`, `DW-0614`, `DW-0616`, `DW-0619`, `DW-0620`, `DW-0622`, `DW-0623`, `DW-0624`, `DW-0625`, `DW-0626`, `DW-0627`, `DW-0628`, `DW-0629`, `DW-0630`, `DW-0631`, `DW-0632`, `DW-0633`, `DW-0634`, `DW-0635`, `DW-0636`, `DW-0637`, `DW-0638`, `DW-0639`, `DW-0640`, `DW-0641` | 205 | Story 12.2 owns MCP ledger closure and contract snapshot decisions. Route type is release certification, evidence is Story 11.5 plus this Story 12.1 record, release consequence is that MCP/agent contract certification remains open until Story 12.2 closes, accepts, or blocks each row. |
| Release gate | Product/Architecture decision | `DW-0666` | 1 | Product owner: Product Owner role. Architecture owner: Architect role. Decision date target: TBD on next Product/Architecture review trigger. Closure trigger: selected docs-slug UNC and drive-relative policy plus matching tests/evidence. Epic 11 remains `in-progress` until this gate closes. |

**Story 11.5 split caveat:** Story 11.5 was the originating owner of the 205 split rows above. The `Owner / evidence / trigger` cell uses the MCP/agent contract label as a catch-all routing description because Story 12.2 is the certifying owner for the bulk MCP/schema closure. A small number of outlier rows (`DW-0143` submodule pointer drift, `DW-0083` Partial P-8 zero-width normalization, `DW-0341` `dotnet pack` NU5104, `DW-0342` 3-level submodule nesting, `DW-0343` CI race, `DW-0344` `@semantic-release/git` push) cover build/release/CI/Unicode concerns rather than MCP runtime contract. Story 12.2 must re-classify these outliers row-by-row (close, split to topical owner, or accept) rather than treating them as homogeneous MCP closure work.

### Change Log

- 2026-05-13: Created Story 12.1 and marked ready-for-dev.
- 2026-05-13: Applied party-mode review hardening; added source-of-truth contract, deterministic starting inventory, AC14-AC20, allowed Epic 11 outcomes, D9-D14, non-goals, stronger validation, and canonical review trace.
- 2026-05-13: Applied advanced elicitation hardening; added fail-closed identity, hash drift, routing matrix, `DW-0666`, artifact consistency, and redaction guardrails.
- 2026-05-14: Implemented ledger marker parity, routed Story 11.5 rows to Story 12.2, surfaced `DW-0666` as a release gate, kept `epic-11` in-progress, and moved Story 12.1 to review.
- 2026-05-14: Code review applied 14 patches (4 from resolved decision-needed findings, 10 from direct patches). Submodule pointer bumps reverted (to be committed separately as a follow-up). `DW-0666` owners redacted to role-only and `Decision date target` field added. Routing matrix split by previous owner; Story 11.5 split caveat added; `Owner: Story 11.X` prose strings in closure-matrix rewritten to `Original owner: …`. DAR expanded with Validation Transcript, Validation Lanes, Redaction Fixture Set, Fingerprint Reproduction Method, and Stakeholder Summary subsections. Sprint-status `last_updated` reworded to match `review` status. Five pre-existing inconsistencies recorded under `## Deferred from: code review of 12-1-…` in `deferred-work.md`.

### File List

- `_bmad-output/implementation-artifacts/12-1-ledger-marker-parity-and-epic-status-decision.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Party-Mode Review

- **Date/time:** 2026-05-13T20:45:15+02:00
- **Selected story:** `12-1-ledger-marker-parity-and-epic-status-decision`
- **Command/skill invocation used:** `/bmad-party-mode 12-1-ledger-marker-parity-and-epic-status-decision; review;`
- **Participating BMAD agents:** Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- **Findings summary:** All four agents recommended `needs-story-update`, not `blocked`. Winston flagged row-addressable ledger contracts, Epic 11/Epic 12 boundary risk, sprint-status truthfulness, and release-certification boundaries. Amelia flagged executable row inventory, deterministic Epic 11 status rules, task order, and copy-pasteable validation. John flagged `DW-0666` ownership, release-owner-readable outcome language, decision-budget control, and RACI clarity. Murat flagged deterministic scripts, fingerprint reproducibility, zero-current-marker gating, redaction evidence, bidirectional status consistency, report drift, and validation-lane ownership.
- **Changes applied:** Added a Source Of Truth Contract; added a Deterministic Starting Row Inventory with row IDs and hashes; added AC14-AC20 for inventory drift, row-scoped status evidence, `DW-0666`, stakeholder summary, consistency, redaction, and validation-lane expectations; tightened T1, T5, T6, and T7; added allowed Epic 11 decision outcomes; added D9-D14; added non-goals; expanded Testing Strategy with deterministic ordering, zero-current-marker, redaction, and ledger/report regeneration checks.
- **Findings deferred:** Whether fingerprint changes are always blocking or can use an intentional baseline-update workflow; whether zero-current-marker failures block all PRs or release/readiness lanes only; final Product/Architecture outcome for `DW-0666`; final release-certification approval owner after parity evidence is assembled.
- **Final recommendation:** `ready-for-dev`

## Advanced Elicitation

- **Date/time:** 2026-05-13T23:03:42+02:00
- **Selected story:** `12-1-ledger-marker-parity-and-epic-status-decision`
- **Command/skill invocation used:** `/bmad-advanced-elicitation 12-1-ledger-marker-parity-and-epic-status-decision`
- **Batch 1 methods:** Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation.
- **Batch 2 methods:** Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records.
- **Findings summary:** The main remaining failure mode was a false Epic 11 closure caused by trusting summaries over detailed rows, silently re-baselining changed row inventories, missing duplicate/malformed row IDs, treating Story 12.2 routing as closure without classification, or committing unredacted evidence transcripts. The elicitation also found that unresolved `DW-0666` must fail closed for any `epic-11: done` decision.
- **Changes applied:** Added fail-closed cheat-sheet guidance; added AC21-AC26 for row identity failures, baseline drift, typed routing, unresolved `DW-0666`, cross-artifact consistency, and adversarial redaction; tightened T1, T4, T5, T6, and T7; added D15-D20; expanded the testing strategy with duplicate/malformed row checks, adversarial redaction fixtures, and routing-matrix reconciliation.
- **Findings deferred:** The final Product/Architecture outcome for `DW-0666`; whether a later implementation should persist the routing matrix as a generated report or only in the Dev Agent Record; the exact release-validation lane that should own redaction and row-matrix checks after Story 12.1 completes.
- **Final recommendation:** `ready-for-dev`

## Code Review

- **Date/time:** 2026-05-14
- **Reviewer:** `/bmad-code-review` skill, three parallel layers (Blind Hunter, Edge Case Hunter, Acceptance Auditor) against commit `9726436`.
- **Review mode:** full (spec-driven)
- **Diff scale:** 5 files changed, +309 / -285 lines (3 docs + 2 submodule pointer bumps)
- **Triage counts:** 4 decision-needed, 10 patch, 5 defer, 4 dismissed-as-noise.

### Review Findings

- [x] [Review][Decision → Patch applied] Submodule pointer bumps reverted (decision: revert to pre-`9726436` SHAs `fd31125` / `ace1c72`). [Hexalith.EventStore, Hexalith.Tenants index entries rolled back; will land as a follow-up commit on top.] Original finding — Diff lines 1-14 update `Hexalith.EventStore fd31125→a3432e7` and `Hexalith.Tenants ace1c72→3a2a4f2`. Story 12.1 D8 says "No recursive nested submodule commands are needed for this story"; Non-Goals + Source Tree Components To Touch do not authorize submodule changes; project-context.md forbids nested submodule updates without explicit request. The File List omits both. Worse: routed row `DW-0143` ("Out-of-scope submodule pointer changes in `Hexalith.EventStore` and `Hexalith.Tenants` — Working-tree carries unrelated submodule pointer drifts ... Should be reverted or committed under a separate change") is itself in the routing matrix that this same commit edits. The commit reproduces the exact anti-pattern one of its routed rows flags. (sources: blind+edge+auditor) [Hexalith.EventStore, Hexalith.Tenants pointer bumps]
- [x] [Review][Decision → Patch applied] DW-0666 owner fields redacted to role-only (`Product Owner role` / `Architect role`) in both `deferred-work.md:1064` and the story Routing Matrix Release-gate row. Original finding — `deferred-work.md` `DW-0666` row reads `Product owner: Jerome / Product Owner; Architecture owner: Winston / Architect;` and the story Routing Matrix echoes the same. "Winston" is a BMad role-agent persona name; "Jerome" matches the git author's first name. The rest of the ledger uses role placeholders (`FrontComposer SourceTools maintainers`, `FrontComposer diagnostic governance maintainers`). AC19/AC26 forbid unbounded user identifiers and require bounded placeholders. Three resolutions exist (keep as-is and document them as role labels in a glossary; redact to roles only; rewrite to bounded placeholders such as `Product Owner role / Architect role`). (sources: blind+edge) [_bmad-output/implementation-artifacts/deferred-work.md:1064; _bmad-output/implementation-artifacts/12-1-…md routing matrix row]
- [x] [Review][Decision → Patch applied] Story 11.6 287→0 confirmed as stale-summary correction (Story 11.6's own 2026-05-13 execution update at `deferred-work.md:78` already closed the rows; the prior `287` was an unupdated summary). DAR Completion Notes amended with a review-clarification line. Original finding — Diff hunk lines 211-218 of the deferred-work.md change shows `-| Story 11.6 | 287 | / +| Story 11.6 | 0 |`. Story 12.1's stated scope is Story 11.2 / 11.4 / 11.5 / `DW-0666` only; Story 11.6 is not enumerated in the Starting Row Inventory, the routing matrix, the completion notes, or the hash inventory. Either Story 11.6 was already fully closed in prior commits and the old `287` was stale (no-op correction), or this story silently zeroed it. Needs explicit confirmation in the DAR. (source: blind) [_bmad-output/implementation-artifacts/deferred-work.md per-owner table]
- [x] [Review][Decision → Patch applied] Routing-matrix flat list preserved; added an explicit caveat under the matrix naming the originating owner (Story 11.5), labeling MCP/agent contract as the catch-all routing description, and enumerating the build/release/CI/Unicode outliers (`DW-0143`, `DW-0083`, `DW-0341`–`DW-0344`) that Story 12.2 must re-classify rather than treat as homogeneous MCP closure. Original finding — The "Split to Story 12.2" cell lists every row ID but applies identical `Downstream impact: agent tool/resource contract consumers; Review trigger: Story 12.2 implementation or MCP contract snapshot policy change` to all 205. T4 permits category grouping only when "every row ID in the category … inherits the same owner, evidence class, downstream impact, and reopen trigger." Outlier rows (e.g., `DW-0143` submodule drift, `DW-0341` `dotnet pack` NU5104, `DW-0342` 3-level submodule nesting, `DW-0343` CI race, `DW-0344` `@semantic-release/git` push, `DW-0083` Partial P-8 zero-width normalization) are not "agent tool/resource contract consumers" concerns. Decision: keep the flat list and accept the homogeneity claim with an explicit "Story 11.5 was the originating owner, MCP/agent contract is the catch-all routing label" caveat, OR split the matrix into subcategories (MCP/agent, build/release/CI, Unicode/parser, etc.) so each subset has truthful downstream-impact/review-trigger fields. (source: blind) [Routing Matrix row 12-1-…md]

- [x] [Review][Patch applied] Headline count "214 stale current markers" should be 215 [_bmad-output/implementation-artifacts/12-1-…md Completion Notes / Debug Log 2026-05-14 entry] — Story 11.2 (2) + Story 11.4 (4 resolved + 3 accepted = 7) + Story 11.5 (205) + DW-0666 (1) = 215, not 214.
- [x] [Review][Patch applied] Split routing-matrix "Accepted constraint" row by previous owner [_bmad-output/implementation-artifacts/12-1-…md Routing Matrix] — Current single row lists 5 IDs collapsing Story 11.2 (`DW-0057`, `DW-0064`) and Story 11.4 (`DW-0325`, `DW-0336`, `DW-0340`). Split into two rows so per-owner accounting (`Story 11.2 = 2 accepted constraints; Story 11.4 = 3 accepted constraints`) reconciles directly from the matrix.
- [x] [Review][Patch applied] Rewrite sprint-status `last_updated` to remove "Completed Story 12.1" phrasing [_bmad-output/implementation-artifacts/sprint-status.yaml:37] — Status on line 159 is `12-1-…: review`; the workflow notes treat `review` as preceding `done`. Replace "Completed Story 12.1 ledger marker parity" with "Story 12.1 ledger marker parity moved to review" or equivalent.
- [x] [Review][Patch applied] Add `Decision date` field to `DW-0666` release-gate marker [_bmad-output/implementation-artifacts/deferred-work.md:1064] — AC9 enumerates "decision date" as a required field for `DW-0666`. The row carries `Release gate 2026-05-14` (the routing date) but no policy-decision target or review-by date. Add an explicit `Decision date target: <YYYY-MM-DD or "Product/Architecture review trigger">`.
- [x] [Review][Patch applied] Document adversarial redaction fixture set in the DAR [_bmad-output/implementation-artifacts/12-1-…md Dev Agent Record] — T7 task box "document the sensitive-token fixture values used" is checked, but no fixture catalog is committed. Add a `### Redaction Fixture Set` subsection enumerating the adversarial fixtures actually checked (e.g., absolute Windows path `C:\\Users\\<user>\\...`, bearer token pattern, raw URL, machine name, tenant/user ID, payload fragment) and the result (zero matches in committed evidence). Required by AC19/AC26/D20.
- [x] [Review][Patch applied] Add `Validation Lanes` section to DAR [_bmad-output/implementation-artifacts/12-1-…md Dev Agent Record] — AC20 requires explicit statement of whether row parity, zero-current-marker, redaction, and status-artifact consistency checks are blocking in PR validation, release validation, or both. Current DAR is silent.
- [x] [Review][Patch applied] Paste row-inventory and zero-current-marker validation transcripts into the DAR [_bmad-output/implementation-artifacts/12-1-…md Dev Agent Record] — AC12/AC18/AC25 require deterministic, replayable evidence. The DAR currently asserts "666 detailed rows, zero missing/malformed/duplicate" and "Ending active Story 11.x counts: 0" as narrative; paste the actual command + output (with redacted environment values where needed) so the audit can be re-run.
- [x] [Review][Patch applied] Add a single `Stakeholder Summary` subsection to the DAR [_bmad-output/implementation-artifacts/12-1-…md Dev Agent Record] — AC17 asks for a release-owner-readable summary covering before/after counts, mismatches, exceptions, residual gates, and final recommendation in one place. Information currently scattered across Completion Notes, Routing Matrix, and the deferred-work "Story 12.1 execution update" paragraph.
- [x] [Review][Patch applied] Rewrite the `Owner: Story 11.X` prose lines in the "Accepted or Split by Story 11.5" closure matrix to use a non-grep-collision form [_bmad-output/implementation-artifacts/deferred-work.md:1087,1091,1095,1103] — Lines such as `Owner: Story 11.2.`, `Owner: Story 11.4.`, `Owner: Story 11.5.` are category-summary attributes in prose bullets, but a naive AC12 audit (`grep "Owner: Story 11.[245]"`) returns them as hits and contradicts the DAR's "Ending active Story 11.x counts: 0". Rewrite as `Original owner: Story 11.X` (or `Previous owner: Story 11.X`).
- [x] [Review][Patch applied] Document the fingerprint reproduction method [_bmad-output/implementation-artifacts/12-1-…md Dev Agent Record + _bmad-output/implementation-artifacts/deferred-work.md fingerprint section] — Externally reproducible: SHA-256 over the ordered row-id list joined by `\n` (no trailing newline), UTF-8 encoding, ordered by first appearance in `deferred-work.md`. State this canonical method in the DAR or as a comment in the deferred-work fingerprint paragraph so a reviewer can replay the hash without guessing separators.

- [x] [Review][Defer] Pre-image bucket totals 659 vs sum-of-buckets 662 [_bmad-output/implementation-artifacts/deferred-work.md Reconciliation Summary, pre-image] — deferred, pre-existing (inherited from prior Epic 11-era ledger writes; the new totals reconcile correctly).
- [x] [Review][Defer] Pre-image per-owner table Story 11.5 = 204 contradicts detailed-row count 205 [_bmad-output/implementation-artifacts/deferred-work.md per-owner table, pre-image] — deferred, pre-existing (Story 12.1's deterministic inventory used the correct detailed-row count; the old summary was stale).
- [x] [Review][Defer] `epic-11-retrospective: done` while `epic-11: in-progress` is a pre-existing internal inconsistency [_bmad-output/implementation-artifacts/sprint-status.yaml:147,155] — deferred, pre-existing.
- [x] [Review][Defer] Bucket vocabulary alias undocumented [_bmad-output/implementation-artifacts/deferred-work.md Reconciliation Status frontmatter] — deferred, pre-existing. `resolved-preserved = resolved + fixed-in-11.6 + fixed-in-11.7` and `accepted-constraint = strict-accepted + accepted-with-risk` reconcile the count arithmetic, but the alias mapping is nowhere documented.
- [x] [Review][Defer] `W1`–`W9` bullets under `## Deferred from: code review …` sections have `Owner:` but no `DW-####` Reconciliation marker [_bmad-output/implementation-artifacts/deferred-work.md ~lines 1118-1130] — deferred, pre-existing. Inventory script does not fail-close on bullets lacking `DW-####`, so T1's identity precondition silently skipped them.
