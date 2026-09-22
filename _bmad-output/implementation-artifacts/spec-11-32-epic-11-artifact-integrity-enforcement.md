---
title: 'Story 11.32: Epic 11 Artifact Integrity Enforcement'
type: 'bugfix'
created: '2026-09-22'
status: 'ready-for-dev'
route: 'dispatch'
story_id: '11.32'
baseline_commit: '00d4da4788692ee405a081884e78482a9556b0c3'
review_loop_iteration: 0
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
- [ ] `eng/validate-story-artifacts.py` and `eng/tests/test_validate_story_artifacts.py` -- implement fail-closed repository integrity checks with exact diagnostics, negative fixtures, and a live-corpus regression while preserving strict/legacy per-story and sentinel semantics.
- [ ] `_bmad-output/implementation-artifacts/` -- correct the three revision hashes; check Story 11.6 tasks only where completion evidence exists; explicitly deliver, defer, dismiss, supersede, or reopen unresolved review/shadow records without pretending work ran.
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml`, `_bmad-output/planning-artifacts/epics.md`, and `deferred-work.md` -- materialize child queue state, align current planning status, reconcile E11R-AI-1..8 evidence/status, and close the owned stale-status debt.
- [ ] `docs/reference/story-artifact-validation.md` -- publish the enforced integrity contract and recovery guidance.

**Acceptance Criteria:**
- Given the reconciled repository, when the story-artifact validator suite runs, then the live Epic 11 corpus passes without exclusions or manual bypasses.
- Given a fixture containing any matrix conflict, when integrity validation runs, then it exits nonzero and identifies the exact story/action and contradiction.
- Given the final sprint state, when E11R-AI-1..8 are inspected, then every mapping and evidence path resolves, supported completed actions are closed, genuinely incomplete actions remain open, and E11R-AI-8 closes only with Story 11.32.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Keep the existing Gate 2b command unchanged by placing focused fixture coverage and the live-corpus assertion in `eng.tests.test_validate_story_artifacts`. Model nonimplementable parent/child topology explicitly; infer ordinary artifact facts from tracked files and fail on malformed input rather than depending on PyYAML or filename-only guesses.

The current sprint-plan generator derives the three nonimplementable parent headings and can report that wrong shape as synchronized. This story uses the blocking integrity validator as the post-generation guard; repairing the separate generator/decomposition parser is outside this artifact-validation change.

## Verification

**Commands:**
- `python3 -m unittest eng.tests.test_validate_story_artifacts` -- expected: focused fixtures and the live repository integrity assertion pass.
- `python3 eng/validate-story-artifacts.py` -- expected: global sentinel and repository integrity validation pass.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -parallel none -method Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.StoryArtifactValidatorGate_IsBlockingAndExact` -- expected: the blocking CI command remains pinned.
- `git diff --check` -- expected: no whitespace errors.
