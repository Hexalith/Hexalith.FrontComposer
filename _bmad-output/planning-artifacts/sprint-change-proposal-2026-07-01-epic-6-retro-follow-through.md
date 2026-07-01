---
title: 'Sprint Change Proposal - Epic 6 retrospective follow-through'
date: '2026-07-01T17:39:51+02:00'
author: 'Administrator (correct-course, Developer role)'
trigger: '_bmad-output/implementation-artifacts/epic-6-retro-2026-06-05.md'
mode: 'Batch (assumed; no live mode selection supplied)'
scope_classification: 'Moderate (workflow/tooling hardening; no product replan)'
status: 'approved-for-implementation'
approved_by: 'Administrator'
approved_at: '2026-07-01T17:43:56+02:00'
artifacts_reviewed:
  - '_bmad-output/planning-artifacts/epics.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01.md'
  - '_bmad-output/project-context.md'
  - '_bmad-output/project-docs/index.md'
  - '_bmad-output/project-docs/architecture.md'
  - '_bmad-output/project-docs/api-contracts.md'
  - '_bmad-output/project-docs/component-inventory.md'
  - '_bmad-output/project-docs/development-guide.md'
  - '_bmad-output/implementation-artifacts/epic-6-retro-2026-06-05.md'
  - '_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md'
  - '_bmad-output/implementation-artifacts/7-5-testing-library-bunit-host-and-deterministic-fakes.md'
  - '_bmad-output/implementation-artifacts/sprint-status.yaml'
  - '_bmad-output/contracts/fc-cli-inspect-contract-2026-06-05.md'
  - '_bmad-output/contracts/fc-diagnostics-catalog-contract-2026-06-05.md'
  - '_bmad-output/contracts/fc-drift-detection-baseline-contract-2026-06-05.md'
  - '_bmad-output/contracts/fc-cust-override-accessibility-diagnostics-contract-2026-06-05.md'
  - 'docs/diagnostics/README.md'
  - 'docs/reference/cli.md'
  - 'docs/how-to/test-generated-components.md'
---

# Sprint Change Proposal - Epic 6 retrospective follow-through

**Date:** 2026-07-01  
**Trigger:** Epic 6 retrospective, dated 2026-06-05  
**Mode:** Batch, because no live mode preference was supplied  
**Status:** Approved for implementation by Administrator on 2026-07-01

## Section 1 - Issue Summary

The Epic 6 retrospective identified one planning-relevant discovery for Epic 7 and five action items.
Current project state shows that the diagnostic and FC-CUST contract items were largely absorbed by
Epic 7, but the evidence-hygiene item is still unresolved.

The core issue is not that Epic 6 or Epic 7 needs to be reopened. The issue is that changed-file
evidence reconciliation remains manual even after repeated review-side corrections. Epic 6 action
`E6-AI-2` explicitly called for converting File List reconciliation into a generated changed-file
check. Epic 7 then confirmed that the same problem still occurred in Stories 7.3 and 7.5, and the
current `sprint-status.yaml` keeps only a broad Epic 8 action item for mechanical reconciliation.

Evidence:

- Epic 6 retro action `E6-AI-2`: "Convert File List reconciliation into a generated changed-file check."
- Epic 7 retro follow-through marks `E6-AI-2` as still in progress, with review-side File List
  omissions in Stories 7.3 and 7.5.
- `sprint-status.yaml` currently has an open Epic 8 action:
  `"Make changed-file and story-task reconciliation mechanical before story review completion"`.
- Story 6.1, 6.2, 6.4, 7.3, and 7.5 records all show review-side File List or evidence-count fixes.
- The diagnostic-phase concern from Epic 6 is already handled in Epic 7 contracts and docs:
  `frontcomposer inspect` preserves the HFC1038-HFC1046 phase boundary, the diagnostic catalog
  contract records known caveats, and architecture/API docs distinguish runtime/startup/reserved
  diagnostics from HFC1050-HFC1055 build analyzer warnings.

## Section 2 - Impact Analysis

### Epic Impact

No product epic is invalidated.

- Epic 6 remains done. Its FC-CUST precedence, Level 3/4 disposition, HFC1050-HFC1055 analyzer scope,
  and development-only mismatch panel rules are confirmed and documented.
- Epic 7 remains done. Stories 7.1 and 7.3 consumed the diagnostic phase/disposition learning from
  Epic 6; Story 7.4 preserved drift-pipeline isolation; Story 7.5 pinned the Testing library surface.
- Epic 8 is not blocked, but its existing open process action should be generalized because the defect
  predates Epic 8 and recurred across Epics 5, 6, and 7.

No epic should be removed, renumbered, or resequenced. No MVP scope change is required.

### Story Impact

Completed story statuses should not be reopened. The correct adjustment is to add a mechanical
pre-review gate that future story, QA, and story-automator review flows must run before a story can be
promoted.

Affected future workflows:

- Dev-story completion: File List and task/evidence reconciliation should be checked before moving to
  review.
- QA E2E generation: generated specs, fixtures, package files, sample-host changes, and test summaries
  should be listed or emitted in a machine-readable way.
- Story-automator review: approval/done promotion should fail if changed story-owned files are absent
  from the story File List.

### Artifact Conflicts

| Artifact | Current state | Required change |
|---|---|---|
| `_bmad-output/planning-artifacts/epics.md` | Epic 7 already captures inspect/catalog/drift/testing requirements. | No product-scope edit required. |
| `_bmad-output/project-docs/architecture.md` | Already records FC-CUST precedence and diagnostic phase boundaries. | No edit required. |
| `_bmad-output/project-docs/api-contracts.md` | Already distinguishes HFC1038-HFC1046 runtime/startup/reserved behavior from HFC1050-HFC1055 build warnings. | No edit required. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Open reconciliation action is scoped to Epic 8 only. | Broaden to process scope and explicitly close E6-AI-2 when implemented. |
| `_bmad/scripts` | No mechanical story artifact reconciliation script exists. | Add a validator script. |
| `.agents/skills/bmad-dev-story/SKILL.md` | Completion relies on manual File List reconciliation. | Require the validator before review promotion. |
| `.agents/skills/bmad-story-automator-review/*` | Review catches omissions after the fact. | Require the validator before approve/done. |
| `.agents/skills/bmad-qa-generate-e2e-tests/SKILL.md` | Summary generation does not require story File List/test-count synchronization. | Record generated files and run the validator when an active story exists. |

### Technical Impact

This is workflow/tooling work. It does not require SourceTools, Shell runtime, MCP, EventStore,
Tenants, schema fingerprint, package, public API, or submodule changes.

The validator should be repository-local, deterministic, and offline. It should avoid live package
feeds, browser execution, and recursive submodule traversal.

## Section 3 - Recommended Approach

**Recommended path: Direct Adjustment with moderate process handoff.**

Rollback is not useful because no completed implementation caused the defect. MVP review is not
needed because the read-only MVP and later epics remain valid. The sustainable fix is to convert the
manual evidence-hygiene rule into a small mechanical gate and wire it into the workflows that create
or approve story-owned files.

Effort estimate: low to medium.

Risk level: medium for workflow friction, low for product behavior. The main risk is false positives
when the validator cannot tell whether a changed file is story-owned or pre-existing. Mitigate that by
supporting explicit, story-local exclusions for pre-existing unrelated files and by treating submodule
paths as out of scope unless explicitly approved.

## Section 4 - Detailed Change Proposals

### 4.1 Sprint status action item

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`  
**Section:** `action_items`

OLD:

```yaml
  - epic: 8
    action: "Make changed-file and story-task reconciliation mechanical before story review completion"
    owner: "QA automation maintainer"
    status: open
```

NEW:

```yaml
  - epic: process
    action: "Make changed-file vs story File List, story-task, test-summary, generated-test, and sample-host reconciliation mechanical before story review completion; closes E6-AI-2 and carries Epic 7/Epic 8 recurrence evidence"
    owner: "QA automation maintainer"
    status: open
```

Rationale: The issue is no longer Epic 8-specific. It is a cross-epic process defect first carried
forward by Epic 6 and confirmed again in Epic 7.

### 4.2 New story artifact validator

**Artifact:** `_bmad/scripts/verify_story_artifacts.py`  
**Section:** new file

OLD:

```text
No repository-local validator exists for story File List and changed-file reconciliation.
```

NEW:

```text
Add a deterministic validator that:

1. Accepts --story <path> and optional --baseline <commit>.
2. Reads the story's baseline_commit front matter when --baseline is omitted.
3. Computes changed files from git diff/status without recursing into nested submodules.
4. Parses the story's "### File List" section.
5. Fails when story-owned changed files are absent from the File List.
6. Includes QA-generated E2E specs, fixtures, helper files, package files, sample-host files,
   contract files, test summaries, and the story file itself.
7. Supports explicit story-local exclusions for pre-existing unrelated files, requiring each
   exclusion to name the path and reason.
8. Checks that completed tasks mentioning generated artifacts have matching File List entries or
   an explicit "not changed" note.
9. Emits text and JSON output suitable for story-automator logs.
10. Exits non-zero on omissions, malformed File List sections, or undocumented exclusions.
```

Rationale: This directly implements `E6-AI-2` without changing product behavior.

### 4.3 Dev-story completion gate

**Artifact:** `.agents/skills/bmad-dev-story/SKILL.md`  
**Section:** completion sequence before moving a story to review

OLD:

```text
Manual instruction: reconcile the File List against git status before moving to review.
```

NEW:

```text
Before moving a story to review, run:

python3 {project-root}/_bmad/scripts/verify_story_artifacts.py --story {story_file}

If the validator fails, update only the allowed story sections:
baseline_commit, Tasks/Subtasks checkboxes, Dev Agent Record, File List, Change Log, and Status.
Do not promote the story until the validator passes or every exclusion is explicitly documented.
```

Rationale: Dev-story is where the first reconciliation claim is usually made, so it should not rely
on memory.

### 4.4 Story-automator review gate

**Artifact:** `.agents/skills/bmad-story-automator-review/workflow.yaml`,
`.agents/skills/bmad-story-automator-review/instructions.xml`, and
`.agents/skills/bmad-story-automator-review/checklist.md`  
**Section:** pre-approval validation

OLD:

```text
Review manually compares the story File List with git reality and auto-fixes omissions when found.
```

NEW:

```text
Run the story artifact validator before approving or moving a story to done.
If automatic fixes are allowed, update the File List and review notes, then rerun the validator.
If automatic fixes are not allowed or the omission is ambiguous, hold the story in review.
```

Rationale: Review should still inspect the result, but the mechanical inventory should be generated
first.

### 4.5 QA E2E generation handoff

**Artifact:** `.agents/skills/bmad-qa-generate-e2e-tests/SKILL.md`  
**Section:** Step 5 - Create Summary

OLD:

```markdown
Output markdown summary:

...

Save summary to: `{default_output_file}`
```

NEW:

```markdown
Output markdown summary and record every created or modified test/support file.
If an active story file is available, update or emit enough information to update:
- generated test files
- generated helper/fixture/package files
- generated sample-host files
- generated test summary file
- test-count changes

Then run:

python3 {project-root}/_bmad/scripts/verify_story_artifacts.py --story <active-story>

Save summary to: `{default_output_file}`
```

Rationale: Multiple review notes show QA-created E2E specs, package files, and summaries were the
common source of post-dev File List drift.

### 4.6 PRD and epic text disposition

**Artifact:** `_bmad-output/planning-artifacts/epics.md`  
**Section:** Epic 7 and Requirements Inventory

OLD:

```text
Epic 7 already covers inspect, migrate, diagnostic catalog surfacing, drift detection, and Testing
library support.
```

NEW:

```text
No text change proposed.
```

Rationale: The PRD-equivalent requirements inventory and Epic 7 stories already absorbed the Epic 6
diagnostic phase/disposition constraint. The remaining change is workflow/tooling, not product scope.

## Section 5 - Implementation Handoff

**Scope classification:** Moderate.

This does not require PM/Architect replanning. It does require Developer plus QA automation
coordination because the durable fix changes BMAD workflow behavior.

### Handoff recipients

- Developer agent: implement `_bmad/scripts/verify_story_artifacts.py`, wire it into dev-story and
  story-automator review, and update `sprint-status.yaml` after approval.
- QA automation maintainer: update E2E generation so generated files and counts flow into the story
  record or the validator input.
- Test Architect: define the validator's own focused tests and false-positive cases.
- Technical Writer: no immediate published-docs update required; document the workflow behavior only
  if the team wants it in contributor docs.

### Success criteria

- `E6-AI-2` can be marked done only after a validator exists and is wired into story review promotion.
- A future story cannot be promoted while story-owned changed files are absent from the File List.
- QA-generated E2E specs, fixtures, package files, sample-host changes, and summaries cannot be
  omitted silently.
- Pre-existing unrelated workspace changes require explicit path/reason exclusions.
- Diagnostic phase/disposition behavior remains unchanged.
- No product epics, completed story statuses, public APIs, package versions, schema fingerprints, or
  submodules are changed.

## Section 6 - Checklist Progress

| Checklist item | Status | Notes |
|---|---|---|
| 1.1 Triggering story identified | [x] | Epic 6 retrospective; downstream evidence in Stories 7.3 and 7.5. |
| 1.2 Core problem defined | [x] | Process/tooling gap: File List reconciliation remains manual. |
| 1.3 Evidence gathered | [x] | Epic 6 retro, Epic 7 retro, sprint-status action item, story review corrections. |
| 2.1 Current epic assessed | [x] | Epic 6 remains complete. |
| 2.2 Epic-level changes determined | [x] | No product epic changes; process action should be broadened. |
| 2.3 Future epics reviewed | [x] | Epic 7 consumed diagnostic lesson; Epic 8 carries current open process item. |
| 2.4 New/obsolete epics checked | [x] | No new product epic needed. |
| 2.5 Epic order/priority checked | [x] | No resequencing needed. |
| 3.1 PRD conflicts checked | [x] | PRD-equivalent `epics.md` remains valid. |
| 3.2 Architecture conflicts checked | [x] | Architecture already records diagnostic phase boundaries. |
| 3.3 UI/UX conflicts checked | [N/A] | No UI behavior change. |
| 3.4 Other artifacts assessed | [x] | Sprint-status and BMAD skills need workflow/tooling edits after approval. |
| 4.1 Direct adjustment evaluated | [x] | Viable; low-medium effort, low product risk. |
| 4.2 Rollback evaluated | [x] | Not viable; no completed implementation should be reverted. |
| 4.3 MVP review evaluated | [x] | Not needed. |
| 4.4 Recommended path selected | [x] | Direct adjustment with moderate process handoff. |
| 5.1 Issue summary created | [x] | Section 1. |
| 5.2 Impact documented | [x] | Section 2. |
| 5.3 Path forward documented | [x] | Section 3. |
| 5.4 MVP/action plan documented | [x] | No MVP reduction; workflow/tooling action plan. |
| 5.5 Handoff plan established | [x] | Section 5. |
| 6.1 Checklist reviewed | [x] | This table. |
| 6.2 Proposal accuracy checked | [x] | Cross-checked against current artifacts. |
| 6.3 Explicit user approval | [x] | Approved by Administrator on 2026-07-01. |
| 6.4 Sprint-status update | [!] | Approved as an implementation task; no sprint-status edit applied during proposal finalization. |
| 6.5 Next steps/handoff | [x] | Developer + QA automation + Test Architect. |

## Section 7 - Approval and Routing

Administrator approved this proposal for implementation on 2026-07-01 at 17:43:56+02:00.

Route this moderate-scope change to:

- Developer agent: implement `_bmad/scripts/verify_story_artifacts.py`, wire it into dev-story and
  story-automator review, and update `sprint-status.yaml`.
- QA automation maintainer: update E2E generation so generated files and counts flow into the story
  record or validator input.
- Test Architect: define validator test coverage and false-positive cases.

Implementation must preserve the proposal's non-goals: no product epic changes, completed story
status changes, public API changes, package changes, schema fingerprint changes, or submodule edits.
