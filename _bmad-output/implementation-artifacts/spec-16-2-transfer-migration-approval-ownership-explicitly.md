---
title: 'Story 16.2: Transfer Migration-Approval Ownership Explicitly'
type: 'chore'
created: '2026-11-16'
status: 'draft'
route: 'dispatch'
story_id: '16.2'
story_key: '16-2-a-evt-xfer-1-conditional-transfer-migration-approval-ownersh'
baseline_commit: '3e58556ba22e2c2a5513ad83dc00f0d899d4a140'
review_loop_iteration: 1
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-16-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 16.2 (EVT-XFER-1) runs only when no distinct EventStore maintainer can approve the migration. The original authority was confirmed available on 2026-11-15, so no transfer was ever needed. The story still has no owning record, though, and its sprint row stays `backlog`, which keeps Epic 16 from ever reaching `done`.

**Approach:** Close 16.2 under its fourth acceptance criterion. Write one dated not-invoked record that cites the existing confirmation receipt, and move the sprint row to `done`. Make no transfer, create no approval artifact, and leave G-3 unchanged.

## Boundaries & Constraints

**Always:** cite the existing `2026-11-15T21:49:02Z` confirmation instead of restating it as new authority. Write "Transfer: not invoked · Migration approval: not claimed · G-3: open" in the record. Keep CRLF line endings. Append only: earlier text in `epic-16-context.md` and the sprint comments stays byte-identical. State the reopen condition from the matrix below.

**Never:** name a receiving role or a delegate, invent a trigger, or record any approve or reject decision. Never touch the 16.1 or 16.3 specs, Tuple Records, `_bmad-output/planning-artifacts/**`, code, workflows, gitlinks or submodules. Never change the `16-3` or `epic-16` rows. Never present closing 16.2 as progress on G-3.

**Decisions (human, 2026-11-16):**
- *Close as not invoked.* The requesting user chose "Close as not invoked (Recommended)" in this session, before `2026-11-16T07:39:15Z`. The question restated that jpiquot holds the EventStore maintainer role and is available. Going forward, this choice replaces only the "tracking remains backlog" clause of `epics.md` § Approved Epic 16 disposition. That planning text stays as dated history.
- *Git state.* The pending 16.3 preparation was already committed as `3e58556ba22e2c2a5513ad83dc00f0d899d4a140`, so 16.2 starts on a clean tree.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Authority available | The 21:49:02Z confirmation stands | Not-invoked record is written; row becomes `done` | N/A |
| Authority later unavailable or moving | Occurs before any operative 16.3 receipt | Reopen: add a new dated 16.2 section, set the row back to `in-progress`, then run the full transfer (AC1–AC3) | A `done` row never authorizes a substitute EventStore receipt; AC3 fails closed |
| Tuple drift | A new 16.1 capture is recorded | No 16.2 change; this record covers the role, not a tuple | N/A |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/epics.md:4098-4126` -- Story 16.2 ACs. AC4 is the not-invoked branch: record it, and no extra approval artifact is needed. `:4593` holds the approved disposition "not invoked; tracking remains backlog". Read only.
- `_bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md:131-140` -- `## Human response and incident disposition — 2026-11-15`, items 1–2: the receipt to cite. Read only.
- `_bmad-output/implementation-artifacts/epic-16-context.md:72-76,128-134` -- the conditional assessment and the confirmation summary. Append target.
- `_bmad-output/implementation-artifacts/sprint-status.yaml:277` -- the row `16-2-a-evt-xfer-1-conditional-transfer-migration-approval-ownersh: backlog`. The trailing comment block ends with the 2026-11-15 lineage note. Append target.
- `eng/validate-story-artifacts.py` -- the story gate. It requires a `## File List`, which is why this record uses `route: dispatch`.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/spec-16-2-transfer-migration-approval-ownership-explicitly.md` -- append `## Not-Invoked Record — 2026-11-16` (the AC4 branch, a link to the receipt, the user's 2026-11-16 choice, the reopen condition and the status line) and a `## File List` -- this record is the story's only deliverable.
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- set the 16-2 row to the story's current state (`done` once reviewed), and append a dated comment saying 16.2 was not invoked, tracking is closed, nothing was transferred or approved, and G-3 is open -- this closes the backlog row.
- [ ] `_bmad-output/implementation-artifacts/epic-16-context.md` -- append a short dated `## Story 16.2 closure — 2026-11-16` paragraph -- this keeps the cached context accurate for 16.3 sessions.

**Acceptance Criteria:**
- Given the record, when it is compared with the cited receipt, then it links the existing 21:49:02Z confirmation and names no receiving role, delegate, trigger or decision.
- Given the diff, when it is inspected, then it touches only the three files above, every change except the one sprint-row value is an append, and the 16.1, 16.3, planning, code and gitlink paths are unchanged.
- Given `sprint-status.yaml`, when it is parsed, then `16-2` is `done`, `16-1` is `done`, `16-3` is `in-progress` and `epic-16` is `in-progress`.

## Spec Change Log

- **Iteration 1, 2026-11-16 (review loopback).** Trigger: review found three frozen-intent gaps, IG-1 to IG-3 in the Review Triage Log, and two spec defects outside the frozen block. BS-1: AC2 forbids setting the approved-change item EVT-XFER-1 to `not-required`. BS-2: AC2 forbids a dated reason comment directly above the 16-2 row. Amendments made at re-plan, after the human answers the gaps: the Tasks set EVT-XFER-1 to `not-required` and insert the reason comment above the row. AC2 then allows only four non-append changes: the 16-2 row value, the sync-managed `last_updated:` field, the EVT-XFER-1 status and that inserted comment. The orchestrator sets the final `done` row once review accepts the record. The record must also cite `prd.md:759` and `epics.md:4593` for the "distinct maintainer" reading, name OI-18, keep the AC3 fail-closed rule explicitly in force, and repeat the full trigger "unavailable or proposed to move" in every copy. Known-bad states avoided: a `done` story with EVT-XFER-1 still `open`; a row stuck at `review`; a record implying AC3 is dormant; a closure decided by an unnamed capacity; a baseline that stops being an ancestor of main. KEEP: the iteration-1 record structure (the cited receipt anchor and planning anchors, which all resolve; the status line; the paragraphs on what the record does not do, on reopening and on tuple drift), the CRLF handling, the append-only prefix checks and the scratch matrix-audit approach. Copies are kept in the session scratchpad folder `keep-16-2-iter1/`.

## Review Triage Log

| Iter | ID | Layer | Finding | Verdict | Route | Evidence |
|---|---|---|---|---|---|---|
| 1 | ECH-1 | edge-case | Reopen trigger "before any operative 16.3 receipt" misses FrontComposer/Release receipts turning operative before the EventStore one | medium | intent_gap IG-1 | Frozen matrix row 2. 16.3 needs three receipts per tuple. AC3 still fails closed, so the harm is a missing documented path, not an unsafe approval. |
| 1 | ECH-2 | edge-case | No reopen path for a later candidate after an earlier operative receipt; the tuple-drift row says 16.2 never changes | medium | intent_gap IG-1 | epics.md 16.3 AC3: drift needs new evidence and approval. Frozen matrix rows 2–3. |
| 1 | ECH-3 | edge-case | Sprint comment drops "proposed to" from the reopen trigger | low | patch (moot: loopback) | Iteration-1 comment said "becomes unavailable or moves". The AC1 trigger is "unavailable or proposed to move". |
| 1 | ECH-4 | edge-case | Record says the first three criteria "are not exercised", implying the AC3 fail-closed rule is dormant | medium | patch (moot: loopback) | AC3 governs any EVT-APP-1 evaluation without the original receipt, whether or not 16.2 was invoked. |
| 1 | ECH-5 | edge-case | Row `in-progress` while the spec is `in-review` | low | patch, DONE group (moot: loopback) | step-04 has no sync. Setting the final row value in this run resolves it. |
| 1 | ECH-6 | edge-case | Approved-change item EVT-XFER-1 stays `open` after closure | medium | bad_spec BS-1 | sprint-status.yaml:1224 `status: "open"`. Legend :40 defines `not-required`. Precedent ADOPT-APP-1 at :1228. AC2 as written forbids the change. |
| 1 | ECH-7 | edge-case | Append-only closure texts cannot be corrected if review rejects; future-tense "moves to done" goes stale | low | patch, DONE group (moot: loopback) | Nothing is committed before review accepts. The new texts stay editable until then. |
| 1 | ECH-8 | edge-case | AC2 claim contradicted by the `last_updated:` data-field change | low | rejected | The fix edits this build's spec. sync-sprint-status mandates the field update. The re-plan amends AC2 for BS-1/BS-2 anyway. |
| 1 | ECH-9 | edge-case | AC3 requires `done`; the row is `in-progress` and nothing owns the flip | medium | patch, DONE group (moot: loopback) | step-05 syncs only to `review` and stops at a later status. The 12.2 precedent set `done` explicitly on the user's choice. |
| 1 | VG-1 | verification-gap | Workflow leaves the row at `review` while AC3 needs `done`; the parity check covers Epic 11 only | medium | DONE group (moot: loopback) | step-05-present.md:17; eng/validate-story-artifacts.py:831. |
| 1 | VG-2 | verification-gap | AC2 literal mismatch (`last_updated:` field) | low | rejected | Same as ECH-8. |
| 1 | BH-1 | blind-hunter | EVT-XFER-1 item open; PRD OI-18 row and epics 16.2 section unaligned; record never names OI-18 | medium | bad_spec BS-1 (sprint item); patch, moot (OI-18 naming); rejected (planning part) | Planning part false: prd.md:759 and epics.md:4593 already record OI-18/16.2 as not invoked, and the frozen Never excludes planning edits. |
| 1 | BH-2 | blind-hunter | Intent, AC3, ticked task and Verification say `done`; the row is `in-progress`; closure texts are premature | medium | DONE group (moot: loopback) | Same root cause as ECH-9. |
| 1 | BH-3 | blind-hunter | Three lifecycle statuses; the validator compares spec and row only for Epic 11 | low | rejected | Pre-existing validator scope (validate-story-artifacts.py:831). Extending it is beyond a direct correction. The in-run state is workflow-designed. |
| 1 | BH-4 | blind-hunter | Tracking decision names no decider or capacity, yet overrides an approved PO/Architect/RO planning clause | medium | intent_gap IG-2 | epic-16-context.md:70 requires the identity and each capacity. The frozen Decision says only "the requesting user". The 12.2 precedent named "Product Owner". |
| 1 | BH-5 | blind-hunter | Reopen condition covers the wrong window and names no checker | medium | intent_gap IG-1 | Same root cause as ECH-1/ECH-2. |
| 1 | BH-6 | blind-hunter | "Distinct maintainer" trigger (epics.md:4100, :2887) not addressed when one person holds all three signer roles | medium | patch (moot: loopback) | Approved planning already resolved it: prd.md:759 and epics.md:4593 (separately confirmed EventStore authority, so not invoked). The record must cite it. |
| 1 | BH-7 | blind-hunter | Implementation Notes contradict each other (two stamps vs one) and AC2 is broken | low | rejected | The fix edits this build's spec. The later correction note supersedes the earlier ones, which stay as append-only history. |
| 1 | BH-8a | blind-hunter | Local main is ahead 1 / behind 1; integrating it rewrites baseline `3e58556b` | medium | intent_gap IG-3 | origin/main is `11c8377a858a756c4e4069bf08e62db1f58e1733`. A rebase changes 3e58556b's SHA, so baseline_commit stops being an ancestor of main and the baseline diff picks up foreign paths. |
| 1 | BH-8b | blind-hunter | A record commit on main breaks the coordination hold / restarts lineage | false | rejected | epic-16-context.md:175: the observed R-to-M change ended reliance on the attested hold. Design Notes disclose the record descendant. |
| 1 | BH-8c | blind-hunter | Context paragraph omits main drift to 11c8377a | false | rejected | The earlier main observations are dated. epic-16-context.md:98 requires a live re-read before decisions. Lineage is 16.3's scope. |
| 1 | BH-9 | blind-hunter | Closure comment sits ~1,050 lines below the row; the comment above the row still says "16.2/16.3 remain pending" | low | bad_spec BS-2 | 12.2 precedent: dated reason comment directly above its row (sprint-status.yaml:196). AC2 as written forbids the insertion. |
| 1 | BH-10 | blind-hunter | AC1 forbids naming the tracking decision the record must state | low | rejected | The fix edits this build's spec. |

## Design Notes

Committing this record adds another record descendant on main. It is not a tested tuple and needs no capture of its own. Main had already drifted past M through `11c8377a858a756c4e4069bf08e62db1f58e1733`; that drift belongs to 16.3's lineage work and is not handled here.

## Verification

**Commands:**
- `run-with-temp python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-2-transfer-migration-approval-ownership-explicitly.md` -- expected: "Story artifact validation passed."
- `git diff --check` -- expected: clean.
- `yaml.safe_load` of `sprint-status.yaml` -- expected: the row values from the ACs.
- `file` on the three files -- expected: CRLF line terminators.

## Implementation Notes

- The sprint row follows the story's lifecycle: `in-progress` during implementation, then `done` once review accepts the record below (matrix row 1; third acceptance criterion).
- The workflow's sprint sync also refreshed the two `last_updated` stamps in the `sprint-status.yaml` header. They are tracking bookkeeping, not record or lineage text; every other change in that file is an append or the one row value.
- Verification, 2026-11-16: the story validator above and the repository-integrity run of the same script both print "Story artifact validation passed."; `git diff --check` is clean; `yaml.safe_load` gives `16-1` `done`, `16-2` `in-progress` (until review), `16-3` `in-progress` and `epic-16` `in-progress`; `file` reports CRLF for all three files, with no bare LF. Earlier bytes of `epic-16-context.md` and `sprint-status.yaml` are an unchanged prefix of the new files, apart from the row value and the two header stamps. All five relative links in the new text resolve to an existing heading. The 16.1 and 16.3 specs, `_bmad-output/planning-artifacts/`, `src/`, `tests/`, `eng/`, `.github/` and `references/` are unchanged against the baseline.
- Step-03 verification correction, 2026-11-16: the header comment `# last_updated:` was restored to its baseline `2026-11-15`, because the frozen Always rule keeps sprint comments byte-identical. Only the sync-managed `last_updated:` data field and the 16-2 row value differ in the pre-existing part of `sprint-status.yaml`.
- Matrix audit, 2026-11-16: this story is documentation only, and Epic 16's evidence policy forbids adding a validator, so no committed test exists. A read-only scratch assertion script, not committed, checked matrix row 1 (the record, the cited receipt, the status line, no invented role, the row lifecycle), row 2 (the reopen and fail-closed rule) and row 3 (tuple drift needs no change), plus the three-file scope and append-only prefixes. Overall: PASS. The `16-2` row stays `in-progress` until review accepts the record.
- Review iteration 1 loopback (intent_gap), 2026-11-16: reverted the deliverable (the Not-Invoked Record and File List sections, the task ticks, the `epic-16-context.md` append and the sprint comment block). The `16-2` row stays `in-progress` from the step-03 sync, as does the `last_updated:` field. Waiting on IG-1 to IG-3 before re-planning.
