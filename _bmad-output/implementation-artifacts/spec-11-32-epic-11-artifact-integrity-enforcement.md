---
title: 'Story 11.32: Epic 11 Artifact Integrity Enforcement'
type: 'bugfix'
created: '2026-09-22'
status: 'done'
route: 'dispatch'
story_id: '11.32'
baseline_commit: '00d4da4788692ee405a081884e78482a9556b0c3'
review_loop_iteration: 1
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
  - '{project-root}/docs/reference/story-artifact-validation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 11 can report completion while its queue contains nonimplementable parent stories, delivered child stories are absent, recorded revisions do not exist, done artifacts retain unresolved tasks or conflicting shadow statuses, and retrospective actions disagree with their evidence.

**Approach:** Reconcile the current Epic 11 artifacts against reachable repository history and accepted delivery evidence, then extend the existing story-artifact validator and its blocking test suite so each conflict fails with the exact story and cause.

## Boundaries & Constraints

**Always:** Preserve the 2026-09-10 retrospective as immutable historical evidence; restore queue ownership to the eleven materialized 11.17a-d, 11.18a-c, and 11.19a-d child artifacts; use reachable commits that actually contain each delivery; distinguish completed work, explicit deferral, superseded records, and genuinely open work; keep validation deterministic, standard-library-only, and active through the existing blocking CI test command.

**Never:** Put 11.17, 11.18, or 11.19 back into the implementable queue; mark unperformed review work as delivered; close E11R-AI-1 while its successor evidence remains in review; close E11R-AI-8 before Story 11.32 passes; add story-specific bypasses for known bad artifacts; change product runtime behavior, immutable release evidence, or the pinned CI entry point.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Consistent corpus | Child queue keys, artifacts, revisions, tasks, shadows, and E11R actions agree | Repository integrity validation passes | No notices conceal a conflict |
| Revision drift | A done story names a missing, non-commit, non-ancestor, or unrelated `final_revision` | Validation names the artifact and revision | Fail closed |
| Completion drift | A done artifact has an unresolved required task or contradictory Story shadow | Validation names the story, file, and conflict | Explicit delivered, deferred, superseded, or reopened disposition required |
| Queue drift | Parent key exists, materialized child is missing/mismatched, or E11R mapping/evidence/status is wrong | Validation names the exact key or action | Malformed or incomplete state fails closed |

</frozen-after-approval>

## Code Map

- `eng/validate-story-artifacts.py` -- preserve strict/legacy per-story behavior while adding reusable repository-integrity parsing and diagnostics for status, revisions, required tasks, child topology, shadows, and E11R actions.
- `eng/tests/test_validate_story_artifacts.py` -- add isolated negative fixtures for every matrix row plus one live-repository assertion; this suite already runs in blocking Gate 2b.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- replace three parent entries with the eleven historical child keys and reconcile E11R-AI-1..8 mappings, evidence, and supported status.
- `_bmad-output/implementation-artifacts/{11-6-testing-harness-failure-modes.md,11-17-mcp-runtime-split-and-benchmark-relocation.md,spec-11-15-storage-scope-and-snapshot-publisher-consolidation.md,spec-11-27-generated-command-route-acceptance-locator.md}` -- reconcile completion evidence, explicit deferrals, the superseded shadow, and any duplicated unresolved findings exposed by the new rule.
- `_bmad-output/implementation-artifacts/spec-11-7-command-projection-route-contract-implementation.md`, `spec-11-9-shell-layering-declaration-and-route-label-relocation.md`, and `spec-11-12-relocate-runtime-and-testing-owned-types-out-of-contracts.md` -- replace dead `final_revision` values with the reachable delivery commits `c5d39c43012e4c349b06e9accc9bf8418e85c18d`, `0b3fab3a11d812b3c47aa943134a7ba1ff5dd851`, and `ff166ac2134b13e839e6e1c9bbab35472ad09019`.
- `_bmad-output/planning-artifacts/epics.md` and `_bmad-output/implementation-artifacts/deferred-work.md` -- align current Epic 11 delivery prose and close DW-1958 and DW-1970 while leaving the rejected retrospective untouched.
- `docs/reference/story-artifact-validation.md` -- document repository-integrity coverage and the distinction between required work, review dispositions, and historical evidence.
- `.github/workflows/quality.yml` and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- existing blocking entry point and guard; inspect to prove no CI command change is needed.

## Tasks & Acceptance

**Execution:**
- [x] `eng/validate-story-artifacts.py` and `eng/tests/test_validate_story_artifacts.py` -- implement fail-closed repository integrity checks with exact diagnostics, negative fixtures, and a live-corpus regression while preserving strict/legacy per-story and sentinel semantics.
- [x] `_bmad-output/implementation-artifacts/` -- correct the three revision hashes; check Story 11.6 tasks only where completion evidence exists; explicitly deliver, defer, dismiss, supersede, or reopen unresolved review/shadow records without pretending work ran.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/planning-artifacts/epics.md`, and `deferred-work.md` -- materialize child queue state, align current planning status, reconcile E11R-AI-1..8 evidence/status, and close the owned stale-status debt.
- [x] `docs/reference/story-artifact-validation.md` -- publish the enforced integrity contract and recovery guidance.

**Acceptance Criteria:**
- Given the reconciled repository, when the story-artifact validator suite runs, then the live Epic 11 corpus passes without exclusions or manual bypasses.
- Given a fixture containing any matrix conflict, when integrity validation runs, then it exits nonzero and identifies the exact story/action and contradiction.
- Given the final sprint state, when E11R-AI-1..8 are inspected, then every mapping and evidence path resolves, supported completed actions are closed, genuinely incomplete actions remain open, and E11R-AI-8 closes only with Story 11.32.

## Implementation Notes

- Review loop 1 must re-derive the repository-integrity pass from the strengthened contract below. Do not carry forward first-pass shortcuts that activate only when a required ledger already exists or infer required topology only from surviving artifacts.
- Preserve the existing standard-library-only command, strict/legacy per-story behavior, authoring-sentinel behavior, exact diagnostics, isolated temporary-Git fixture style, historical revision/queue/E11R reconciliation, immutable retrospective, and unchanged Gate 2b entry point.

## Spec Change Log

- **2026-09-22 — Review loop 1.** Triggering findings: the first pass could be disabled by deleting required ledgers, derived required children only from surviving files, ignored malformed or ambiguous artifact metadata, accepted weak revision/evidence relationships, and lacked negative fixtures for several implemented branches. Amended the non-frozen Design Notes and verification contract to require independent corpus manifests, mandatory inputs, strict identity/status/supersession/action parsing, exact queue/planning agreement, stronger provenance association, and explicit negative coverage. Known-bad state avoided: a green repository-integrity gate after the conflicting or missing evidence it is meant to guard has disappeared. **KEEP:** the standard-library-only integration in the existing validator; preserved strict/legacy and sentinel semantics; exact story/action diagnostics; isolated temporary-Git tests plus a live-corpus assertion; the verified child-queue, historical revision, planning, deferred-work, and E11R reconciliations; the immutable retrospective and unchanged Gate 2b command.

## Review Triage Log

| ID | Verdict | Evidence |
|---|---|---|
| BH-01 | medium | The live spec is `in-review` while its sprint and planning rows remain `in-progress`, and no validator path compares an ordinary story artifact status with either ledger. |
| BH-02 | high | `main()` invokes repository integrity only when `sprint-status.yaml` already exists, so deleting that ledger disables the entire integrity pass and leaves only sentinel scanning. |
| BH-03 | medium | `validate_planning_statuses()` returns no failures when `epics.md` is absent, erasing every planning/sprint reconciliation check. |
| BH-04 | high | Child expectations are derived only from surviving child artifacts; removing a delivered child artifact and its queue row creates no expectation and passes. |
| BH-05 | high | Planning reconciliation silently continues when a story has zero or multiple queue candidates, so deleting an ordinary story row or adding a decoy row is not a failure. |
| BH-06 | high | Repository artifact statuses are neither required nor allow-listed; an absent or misspelled status bypasses done-artifact validation and can make retrospective evidence appear complete. |
| BH-07 | high | `parse_repository_story_artifact()` discards both frontmatter and semantic-scan failures, so malformed structure can hide metadata and tasks from the blocking pass. |
| BH-08 | medium | The parser selects the first explicit Story identity and does not compare title, H1, and filename identities, allowing evidence attribution to drift. |
| BH-09 | medium | Duplicate active artifacts are rejected only when their statuses differ; two active `done` records pass without a supersession declaration. |
| BH-10 | medium | A superseded record is accepted when `superseded_by` names any existing file; story identity, self-reference, active status, and artifact location are not checked. |
| BH-11 | medium | An unchecked review row containing a disposition label is excluded from failures, even though the documented contract requires dispositioned rows to be checked. |
| BH-12 | low | A `### Review Findings` section remains active across a peer `###` heading because exit is limited to level 2 or above, so unrelated unchecked lists can be misclassified. |
| BH-13 | high | `final_revision` relevance passes solely on a Story ID in the commit subject even when the commit changes no delivery or artifact path, contradicting the delivery-provenance requirement. |
| BH-14 | medium | Retrospective evidence is checked only for path existence; the shared generic evidence file in the fixture proves unrelated evidence can close an action. |
| BH-15 | medium | Retrospective `epic` is ignored, duplicate scalar fields use first-wins parsing, and `closed` is checked only for non-emptiness, allowing contradictory or malformed action metadata. |
| VG-01 | medium | Pre-verified gap: fixtures never create a planning artifact or assert a planning/sprint mismatch, so deleting `validate_planning_statuses()` leaves all focused tests green. |
| VG-02 | low | Pre-verified gap: revision tests cover four 40-character object shapes but never exercise the canonical-length/case guard, so that guard can regress unnoticed. |
| VG-03 | medium | Pre-verified gap: the queue test covers a parent and a missing child row but never an existing child whose queue status disagrees with its artifact. |
| VG-04 | medium | Pre-verified gap: no fixture enters the superseded-artifact branch, so missing and nonresolving successor diagnostics are unpinned. |
| VG-05 | medium | An independently executed fixture with `- [ ] [Review][Defer]` exits successfully, confirming the unchecked-disposition bypass. |
| VG-06 | medium | An independently executed fixture with two explicit active `done` artifacts exits successfully, confirming same-status duplicates are not rejected. |
| VG-07 | high | Deleting an entire child family from the fixture while retaining its queue keys exits successfully because topology is inferred only from remaining child artifacts. |
| EC-01 | high | `extract_frontmatter()` and `scan_semantic_lines()` expose failures, but repository parsing ignores them; malformed artifacts can therefore disappear from semantic validation. |
| EC-02 | medium | Multiple explicit identity matches are collected but only the first is used, so contradictory title and H1 identities pass. |
| EC-03 | high | Missing or unsupported artifact statuses are accepted and alter completion and retrospective-state inference. |
| EC-04 | medium | Supersession validates only that a target file exists, so self-links and different-story successors remove contradictory evidence from reconciliation. |
| EC-05 | medium | Same-status duplicate active artifacts bypass the `len(statuses) < 2` guard despite the documented one-active-record contract. |
| EC-06 | maybe-false | The claimed unchecked delivery behind a checked Patch row cannot be determined from the current artifact schema; settling it requires a defined machine-readable link from each patch row to delivery evidence. |
| EC-07 | low | `git diff-tree` omits `--root`, so a root delivery commit that changes the artifact but omits the Story ID in its subject is rejected as unrelated; the condition is rare but the plumbing behavior is real. |
| EC-08 | medium | Child-parent rejection checks only keys beginning `11-17-`/`11-18-`/`11-19-`; a bare materialized parent key such as `11-17` survives. |
| EC-09 | medium | A missing planning artifact returns success rather than an exact fail-closed diagnostic. |
| EC-10 | high | A planning Story with no matching sprint candidate is silently skipped, allowing ledger omission to pass. |
| EC-11 | medium | Sprint lifecycle values are normalized but never allow-listed, so unsupported values can participate in comparisons and pass. |
| EC-12 | medium | Retrospective scalar parsing uses `setdefault`, silently retaining the first of repeated integrity-bearing fields instead of reporting ambiguity. |
| EC-13 | high | The promised any-conflict failure is violated when a child artifact and its queue row vanish together because no independent child manifest remains. |
| EC-14 | low | The diff repairs six revision fields while the execution task names three; this is a real documentation mismatch, but its only correction edits this build's spec and is therefore rejected by review policy. |
| BH-16 | medium | An isolated fixture with either blank or malformed present `story_id` metadata returned success because repository parsing ignored the invalid scalar and inferred identity from title/H1; patch the parser to reject an invalid present identity field. |
| BH-17 | medium | An isolated artifact with consecutive `Status: done` and `Status: review` declarations returned success because status scanning stopped at the first body declaration; patch collection to compare every pre-H2 status shadow. |
| BH-18 | medium | Changing only the title identity to malformed `Story 11.25.1` returned success because the invalid identity-bearing title was ignored while the H1 won; patch identity parsing to fail malformed Story-bearing title/H1 metadata. |
| BH-19 | medium | A second active artifact named `weird-history.md` with the accepted space-delimited `Story 11.25 Fixture` title/H1 returned success because candidate discovery depended on a colon or queue-shaped filename; patch discovery to include every parsed Epic 11 document identity. |
| BH-20 | medium | A done artifact carrying `superseded_by` returned success because that field is validated only in the superseded branch; patch non-superseded records to reject contradictory successor metadata. |
| BH-21 | low | A synthetic `11-99-phantom` queue row is indeed ignored, but the current contract intentionally reconciles active artifacts to queue rows rather than asserting the inverse, and a universal inverse would also need a policy for decision-only Story 11.8. The unlikely case and nontrivial policy change make this rejected low finding. |
| BH-22 | medium | Isolated `11.17` and zero-padded parent-equivalent queue keys bypass the hyphen-only topology matcher, despite the contract rejecting bare parent rows; patch canonical Epic 11 child/parent key validation. |
| BH-23 | medium | Replacing the Story 11.28 planning heading colon with a dash made the entry disappear and returned success; patch Story-like heading parsing to reject malformed maintained planning identities. |
| BH-24 | medium | A fixture with Story 11.28 consistently in review while E11R-AI-4 remained done returned success, allowing action closure before implementation; patch E11R-AI-2..7 status agreement in both directions. |
| BH-25 | medium | Deleting the Story 11.25 successor and closing E11R-AI-1 returned success because successor status is checked only when the file exists; patch missing successor evidence to fail and preserve the open action. |
| BH-26 | medium | Removing an action's evidence field while replacing its historical `ref` with the active artifact returned success because both fields are concatenated; patch evidence validation to require and inspect the evidence field independently. |
| BH-27 | medium | Appending `-NOT-A-PATH` after a matching `.md` evidence scalar returned success because an unbounded substring was truncated at `.md`; patch evidence extraction to parse complete path list-item scalars. |
| BH-28 | medium | Moving a matching artifact path under a sibling `notes:` field returned success because evidence mode survived an unrecognized four-space key; patch the evidence block to end at every sibling field. |
| EC-15 | medium | This independently duplicates BH-17: conflicting pre-H2 body statuses are reachable and the second is currently ignored, so the same status-collection patch covers it. |
| EC-16 | medium | This independently duplicates BH-19: a valid space-delimited Epic 11 identity under a nonmatching slug is not selected as a candidate, so the discovery patch covers it. |
| EC-17 | high | A queue-matched `spec-11-24-story.md` whose title/H1 identify Story 12.24 returned success because the non-Epic document identity is filtered before filename comparison; patch filename/document conflict detection before Epic filtering. |
| EC-18 | medium | This independently duplicates BH-20: active successor metadata is demonstrably accepted and contradicts lifecycle state, so the non-superseded guard covers it. |
| EC-19 | medium | `git diff-tree -r` does not expose merge-parent changes for a merge commit, so a valid merge revision can be rejected as pathless; patch changed-path inspection to include merge-parent diffs and add a focused merge fixture. |
| EC-20 | medium | A planning status such as `done_foo` returned success because the status regex accepted only its valid prefix; patch parsing to validate the complete explicit scalar. |
| EC-21 | medium | Pre-verified and independently reproduced: an extra planning child such as 11.18d passes without artifact or queue evidence; patch planning validation to reject children outside the fixed manifest. |
| EC-22 | medium | Adding an unquoted duplicate E11R-AI-4 block returned success because action starts recognize quoted IDs only; patch action discovery to parse quoted and unquoted canonical IDs before uniqueness checks. |
| EC-23 | medium | This independently duplicates BH-24: done E11R-AI-2..7 actions are not rejected when their implementation story regresses to review, so bilateral status agreement is required. |
| EC-24 | medium | This independently duplicates BH-25: absent Story 11.25 successor evidence currently removes the E11R-AI-1 closure guard, so missing evidence must fail closed. |
| EC-25 | false | The exact claim is disproved by the review-loop contract: presence of the maintained planning root intentionally activates integrity checks, and conditioning activation on surviving Epic 11 text would let deletion of `epics.md` disable its own required-file validation. |
| EC-26 | medium | This claim duplicates BH-19/EC-16 and the isolated space-delimited duplicate returned success; candidate discovery must use every valid Epic 11 identity form it already parses. |
| VG-08 | high | Pre-verified gap: deleting the sprint ledger, planning directory, and all top-level Story 11 artifacts together while leaving the maintained implementation root returns no failures; patch activation to key off the maintained root and add the combined-removal fixture. |
| VG-09 | medium | Pre-verified gap: an unexpected planning-only child 11.18d produces no failure; patch the fixed-manifest inverse check and pin its exact diagnostic. |
| VG-10 | medium | Pre-verified gap: the missing-child test removes artifact and queue together but never asserts the independently implemented missing-queue-key branch; add a queue-only deletion case so that gate cannot regress silently. |
| VG-11 | medium | Pre-verified gap: repository fixtures exercise unchecked review findings but not an unchecked required task, so removing the task-heading arm leaves the suite green; add an unchecked Tasks case. |
| VG-12 | medium | Pre-verified gap: E11R-AI-8 has only a passing open/in-progress state, so its early-closure branch can disappear unnoticed; add both forbidden early closure and allowed completed-story transition cases. |
| VG-13 | medium | Pre-verified gap: the one-hop supersession-chain rejection has no fixture, so its branch can regress while all tests remain green; add a superseded-to-superseded chain case. |

## Design Notes

Keep the existing Gate 2b command unchanged by placing focused fixture coverage and the live-corpus assertion in `eng.tests.test_validate_story_artifacts`. Model nonimplementable parent/child topology explicitly; infer ordinary artifact facts from tracked files and fail on malformed input rather than depending on PyYAML or filename-only guesses.

The current sprint-plan generator derives the three nonimplementable parent headings and can report that wrong shape as synchronized. This story uses the blocking integrity validator as the post-generation guard; repairing the separate generator/decomposition parser is outside this artifact-validation change.

Review-loop contract:

- Activate repository-integrity validation when either maintained `_bmad-output` planning or implementation artifact root is present. Once activated, missing `implementation-artifacts`, `sprint-status.yaml`, `planning-artifacts`, or `epics.md` is an exact failure; existence of a required file must never be the condition that enables its own validation. Temporary sentinel/per-story fixtures with no maintained artifact root retain their existing behavior.
- Carry an independent expected manifest for the eleven materialized children: 11.17a-d, 11.18a-c, and 11.19a-d with their canonical artifact/queue keys. Missing artifacts, missing rows, bare or suffixed parent rows, extra/mismatched child rows, and artifact/queue status disagreement all fail even when both sides of a missing pair disappear.
- Require every active Epic 11 story artifact to have one allowed lifecycle status. Propagate frontmatter and semantic Markdown parse failures. Reject conflicting Story identities across title, H1, and filename-derived identity. Reject every duplicate active record, including same-status duplicates.
- A superseded record must name a different repository-relative file under `implementation-artifacts`; the target must resolve to a non-superseded active artifact for the same Story identity. Self-links, generic evidence files, missing targets, and different-story targets fail.
- Treat every unchecked required or review checkbox as unresolved, including rows already labeled Defer, Dismiss, Supersede, or Reopen. End review-section scope at a peer or higher heading, not only at level 2. A checked Patch row remains the explicit artifact assertion that delivery evidence exists; do not invent an unverifiable per-row evidence schema.
- Parse `development_status` fail closed: duplicate/malformed keys and unsupported values fail. Each active explicit Epic 11 artifact maps to exactly one numeric Story queue entry (with child filenames owning child rows), and its normalized status agrees. Explicit maintained planning statuses must have exactly one matching queue entry and agree; zero or multiple candidates fail.
- Validate `final_revision` as a canonical lowercase 40-character commit that exists, is a commit, and is an ancestor of `HEAD`; include root commits in changed-path inspection. A Story ID in the subject alone is insufficient: relevance must also be supported by a changed non-artifact delivery path named by the artifact's Code Map/File List, or another equally deterministic artifact-owned delivery-path declaration. Artifact-only bookkeeping commits do not establish delivery.
- Parse E11R-AI-1..8 with unique integrity-bearing fields, `epic: 11`, the exact implementation-story mapping, supported open/done status, and an ISO `YYYY-MM-DD` closed date only when done. Evidence must resolve and include an active artifact whose Story identity matches `implementation_story`; a generic existing path alone is insufficient. Keep E11R-AI-1 open while its active successor is in review and E11R-AI-8 open until Story 11.32 passes review.
- Add isolated negative tests for missing required ledgers; planning/sprint mismatch and zero/multiple candidates; missing entire child families, bare parents, missing children, and child-status mismatch; noncanonical revisions and root-commit changed paths; malformed metadata, conflicting identities, missing/invalid statuses, and same-status duplicates; missing/self/different-story supersession targets; unchecked disposition rows and peer-heading boundaries; duplicate/malformed E11R fields and unrelated evidence. Every diagnostic must name the exact story/action plus the contradiction. Keep the consistent fixture and live repository assertion.

## Verification

**Commands:**
- `python3 -m unittest eng.tests.test_validate_story_artifacts` -- expected: focused fixtures and the live repository integrity assertion pass.
- `python3 eng/validate-story-artifacts.py` -- expected: global sentinel and repository integrity validation pass.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -parallel none -method Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.StoryArtifactValidatorGate_IsBlockingAndExact` -- expected: the blocking CI command remains pinned.
- `git diff --check` -- expected: no whitespace errors.

**Actual (2026-09-22):** all four commands passed; the Python suite ran 204 tests
with two skips, and the pinned governance method ran one test with no failures.
