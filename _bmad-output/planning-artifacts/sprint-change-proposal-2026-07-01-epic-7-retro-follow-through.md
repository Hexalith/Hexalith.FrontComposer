# Sprint Change Proposal: Epic 7 Tooling Follow-Through

Date: 2026-07-01
Trigger: `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md`
Mode: Batch
Status: approved
Approval: Administrator approved on 2026-07-01

## 1. Issue Summary

Epic 7 completed the authoring-tooling and drift-safety surface, but its retrospective recorded five
carry-forward actions that are not represented in the current open action item list. Current
`sprint-status.yaml` shows Epic 8 action items only, while the Epic 7 retro still names unresolved
process, documentation, testing, architecture, and security follow-through:

- `E7-AI-1`: make changed-file vs story File List reconciliation mechanical before review promotion.
- `E7-AI-2`: remove stale historical story ownership from adopter-facing CLI and Testing docs.
- `E7-AI-3`: treat CLI text output parity as contract coverage.
- `E7-AI-4`: keep HFCM9002 production emission as an explicit future design decision.
- `E7-AI-5`: keep Testing package evidence redaction in the default testing lane.

The direct problem is planning traceability: the work is known, but there is no current backlog home
or open action item set preserving it. A secondary issue is numbering collision. An existing proposed
change, `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01.md`, already reserves Epic
9 for row-level new-item producer work. This proposal therefore uses Epic 10 for the Epic 7 tooling
follow-through if that earlier proposal is approved. If the Epic 9 proposal is rejected before
implementation, the Product Owner may renumber this follow-through to the next available epic.

Evidence:

- Epic 7 retro action items list `E7-AI-1` through `E7-AI-5`.
- `sprint-status.yaml` currently lists only open Epic 8 action items.
- `fc-cli-migrate-contract-2026-06-05.md` and `src/Hexalith.FrontComposer.Cli/README.md` correctly
  state that HFCM9002 is synthetic-only today.
- `fc-testing-library-host-contract-2026-06-05.md`, `src/Hexalith.FrontComposer.Testing/README.md`,
  and `docs/how-to/test-generated-components.md` already contain the Story 7.5 redaction and
  not-modified testing contract, but the retro action should remain tracked as a regression guard.

## 2. Impact Analysis

Epic impact:

- Epic 7 remains done. No completed Epic 7 story should be reopened.
- Epic 8 remains done. Its open action item for mechanical reconciliation overlaps with `E7-AI-1`
  and should be broadened rather than duplicated.
- A post-MVP tooling-governance follow-up should be added as Epic 10 if Epic 9 is accepted first.

Story impact:

- Stories 7.1 through 7.5 remain done.
- New backlog stories are needed only for unimplemented follow-through work. Completed story evidence
  should be referenced, not rewritten.
- If Product decides not to pursue production HFCM9002 emission, Story 10.4 should explicitly remove
  adopter-facing promises instead of implementing a SourceTools emitter.

Artifact impact:

- No authored PRD exists for this repo. `epics.md` is the requirements inventory and PRD proxy.
- `epics.md` should gain a post-MVP authoring-tooling follow-through requirement and epic.
- `sprint-status.yaml` should gain backlog entries and restored Epic 7 open action items.
- Architecture and CLI contract docs need no immediate correction because they already preserve the
  synthetic-only HFCM9002 boundary. They should be updated only if Story 10.4 changes that decision.

Technical impact:

- No rollback is useful.
- Implementation is mostly process/test/docs tooling, with possible SourceTools work only if HFCM9002
  production emission is approved.
- The highest-risk implementation item is a false-positive file reconciliation check that blocks valid
  story work. Story 10.1 must compare against the story baseline commit and allow documented unrelated
  pre-existing workspace changes.

## 3. Recommended Approach

Selected path: Direct Adjustment with backlog reorganization.

Scope classification: Moderate.

Rationale:

- The shipped Epic 7 behavior is not invalidated.
- The missing work is cross-cutting enough that it should not be hidden as informal notes.
- Adding a focused post-MVP tooling-governance epic keeps action ownership explicit without reopening
  completed stories.
- The existing Epic 8 action item should be broadened to absorb the recurring reconciliation problem
  seen across Epics 6, 7, and 8.

Effort estimate: Medium. Stories 10.1, 10.2, 10.3, and 10.5 are small-to-medium governance or docs
work. Story 10.4 can become medium-to-large only if Product chooses production HFCM9002 emission.

Risk level: Medium. The main risks are over-broad automation blocking legitimate work, and accidentally
documenting HFCM9002 as production behavior before a real SourceTools emitter exists.

Timeline impact: No impact to completed Epics 1-8. Adds a post-MVP backlog epic and restored open
retro action items.

## 4. Detailed Change Proposals

### 4.1 `epics.md` - Add Tooling Follow-Through Requirement

Section: Additional Requirements

OLD:

```md
- AR10 (**Out of scope for v1 / fast-follow**): Do **not** build `<AuditTimeline>` or `<ConsequencePreview>` rich components now - approved fallbacks stand; track as fast-follow.
```

NEW:

```md
- AR10 (**Out of scope for v1 / fast-follow**): Do **not** build `<AuditTimeline>` or `<ConsequencePreview>` rich components now - approved fallbacks stand; track as fast-follow.
- AR12 (**FC-TOOL-GOV**): Preserve Epic 7 authoring-tooling follow-through as explicit backlog work: mechanical story evidence reconciliation, adopter-facing historical-label cleanup, CLI text/JSON parity coverage, HFCM9002 production-emission decisioning, and default-lane Testing redaction coverage. Use AR12 if the existing FC-NIP/AR11 proposal is accepted first; otherwise renumber to the next available AR.
```

Rationale: Keeps the Epic 7 retro actions visible in the requirements inventory without conflicting
with the already proposed FC-NIP/AR11 change.

### 4.2 `epics.md` - Add Epic 10

Section: Epic List

NEW:

```md
### Epic 10: Tooling Governance Follow-Through *(post-MVP quality hardening)*
An **adopter developer** can trust FrontComposer's authoring-tooling evidence because story file
lists are mechanically reconciled, CLI text output is covered like JSON output, migration sidecar
promises stay honest, and Testing package evidence remains redacted by default.
**FRs covered:** FR20, FR21, FR22
**ARs:** AR12 (FC-TOOL-GOV)
**Standalone:** post-MVP quality hardening; builds on Epic 7 and does not reopen completed stories.
```

Rationale: All current root epics are done, and the existing July 1 proposal already uses Epic 9.

### 4.3 `epics.md` - Add Stories 10.1 Through 10.5

NEW:

```md
## Epic 10: Tooling Governance Follow-Through

### Story 10.1: Mechanical story evidence reconciliation

As a QA automation maintainer,
I want changed-file, story File List, and task-completion reconciliation to run before review promotion,
So that story review no longer discovers omitted story-owned files or stale completion claims.

**Acceptance Criteria:**

**Given** a story has a `baseline_commit`,
**When** the reconciliation check runs,
**Then** it compares story-owned changed files against the story File List and reports omitted,
extra, or undocumented files before the story can move to review.

**Given** a workspace has pre-existing unrelated changes,
**When** they predate the story baseline or are explicitly documented as unrelated,
**Then** the check reports them separately without forcing the story to claim ownership.

**Given** story tasks are marked complete,
**When** the check runs,
**Then** it verifies task claims against changed files, test summaries, or explicit documented blockers.

### Story 10.2: Adopter-facing historical-label cleanup

As a technical writer,
I want adopter-facing CLI, diagnostics, and Testing docs free of stale historical story ownership labels,
So that adopters are not sent to obsolete Story 9 provenance when Epic 7 owns the current contract.

**Acceptance Criteria:**

**Given** CLI, migration, diagnostics, Testing README, and published how-to docs,
**When** they describe current Epic 7 behavior,
**Then** adopter-facing text names the current contract or feature, not stale historical story ownership.

**Given** source comments or generated diagnostic registry metadata retain old Story 9 labels as
provenance,
**When** they are not adopter-facing and do not misstate current ownership,
**Then** they may remain documented as brownfield provenance.

### Story 10.3: CLI text-output parity guard

As a Test Architect,
I want text output covered at the same behavioral boundary as JSON output for CLI commands,
So that summaries, filtering, and budgets cannot drift between machine and human output.

**Acceptance Criteria:**

**Given** a CLI command has JSON summary, filtering, fail-flag, or diff-budget behavior,
**When** tests are added or changed,
**Then** text-output pins cover the same shared behavior unless the story explicitly documents why text
does not expose that field.

**Given** a migration or inspect output budget changes,
**When** JSON caps are updated,
**Then** text output caps and omitted-budget markers are updated and tested intentionally.

### Story 10.4: HFCM9002 production-emission decision

As a Product Owner and Architect,
I want an explicit decision on production HFCM9002 migration sidecar emission,
So that adopter docs either promise a real SourceTools emitter or clearly keep HFCM9002 synthetic-only.

**Acceptance Criteria:**

**Given** the current CLI migrate contract,
**When** Product and Architecture review HFCM9002,
**Then** they choose one of two paths: implement a SourceTools production sidecar emitter with tests, or
remove/de-emphasize adopter-facing promises beyond synthetic/manual sidecar evidence.

**Given** production emission is approved,
**Then** SourceTools emits the sidecar, CLI migrate reads it, docs describe it, and tests prove path
safety, redaction, and text/JSON output parity.

**Given** production emission is not approved,
**Then** CLI README and contract docs keep the synthetic-only boundary prominent.

### Story 10.5: Testing evidence redaction default-lane guard

As a developer,
I want Testing package evidence redaction to stay in the default lane,
So that assertion helpers cannot leak tenant, user, token, secret, password, oversized, or
punctuation-heavy secret values.

**Acceptance Criteria:**

**Given** Testing package evidence formatters or fakes change,
**When** the default Testing lane runs,
**Then** it includes redaction cases for tenant/user IDs, token/secret/password keys, oversized payloads,
and punctuation-heavy string secret values.

**Given** a new public Testing helper emits evidence,
**Then** `PublicAPI.Shipped.txt`, README guidance, and redaction tests are updated intentionally.
```

Rationale: Converts the Epic 7 retro action items into implementable backlog items.

### 4.4 `sprint-status.yaml` - Add Backlog Entries

Section: `development_status`

OLD:

```yaml
  epic-8-retrospective: done
```

NEW:

```yaml
  epic-8-retrospective: done

  # Epic 10: Tooling Governance Follow-Through (post-MVP quality hardening)
  # Numbered Epic 10 to avoid colliding with the existing proposed Epic 9 FC-NIP change.
  epic-10: backlog
  10-1-mechanical-story-evidence-reconciliation: backlog
  10-2-adopter-facing-historical-label-cleanup: backlog
  10-3-cli-text-output-parity-guard: backlog
  10-4-hfcm9002-production-emission-decision: backlog
  10-5-testing-evidence-redaction-default-lane-guard: backlog
  epic-10-retrospective: optional
```

Rationale: Adds a backlog home without changing completed Epic 7 or Epic 8 statuses.

### 4.5 `sprint-status.yaml` - Broaden Existing Reconciliation Action

Section: `action_items`

OLD:

```yaml
  - epic: 8
    action: "Make changed-file and story-task reconciliation mechanical before story review completion"
    owner: "QA automation maintainer"
    status: open
```

NEW:

```yaml
  - epic: 10
    action: "Make changed-file, story File List, and story-task reconciliation mechanical before story review completion; covers Epic 7 E7-AI-1 and Epic 8 follow-through"
    owner: "QA automation maintainer"
    status: open
```

Rationale: The same recurring process defect appears in multiple retrospectives. One broader action is
clearer than parallel open items.

### 4.6 `sprint-status.yaml` - Restore Remaining Epic 7 Action Items

Section: `action_items`

NEW:

```yaml
  - epic: 10
    action: "Audit adopter-facing CLI, diagnostics, and Testing docs for stale historical story ownership labels; retain provenance-only labels only where they do not confuse current ownership"
    owner: "Technical Writer"
    status: open
  - epic: 10
    action: "Add CLI text-output parity coverage guidance for summaries, filtering, and diff budgets on future CLI stories"
    owner: "Test Architect"
    status: open
  - epic: 10
    action: "Decide HFCM9002 production sidecar emission: either implement and pin a SourceTools emitter or keep/remove adopter-facing production promises"
    owner: "Architect + Product Owner"
    status: open
  - epic: 10
    action: "Keep Testing package evidence redaction cases in the default lane for tenant, user, token, secret, password, oversized, and punctuation-heavy values"
    owner: "Developer"
    status: open
```

Rationale: Restores the non-overlapping Epic 7 retrospective actions to active tracking.

### 4.7 Contract and Architecture Documents

No immediate contract or architecture edits are required by this approved proposal.

If Story 10.4 chooses production HFCM9002 emission, update:

- `_bmad-output/contracts/fc-cli-migrate-contract-2026-06-05.md`
- `_bmad-output/contracts/fc-diagnostics-catalog-contract-2026-06-05.md`
- `src/Hexalith.FrontComposer.Cli/README.md`
- `_bmad-output/project-docs/architecture.md`
- published migration/diagnostics docs under `docs/`

If Story 10.4 rejects production emission, update only adopter-facing wording that could be read as a
production promise. Keep the current synthetic-only wording in the CLI README and CLI migrate contract.

## 5. Implementation Handoff

Scope classification: Moderate.

Handoff recipients:

- Product Owner: use the approved Epic 10 numbering for story creation and keep it sequenced after the
  existing Epic 9 FC-NIP work.
- QA automation maintainer: implement Story 10.1.
- Technical Writer: implement Story 10.2 and any Story 10.4 doc outcome.
- Test Architect: implement Story 10.3 and define default-lane evidence for Story 10.5.
- Architect + Product Owner: decide Story 10.4.
- Developer agent: implement approved SourceTools/CLI/Testing code changes after Product/Architecture
  decisions are recorded.

Success criteria:

- `sprint-status.yaml` contains the restored Epic 7 follow-through actions.
- Story reconciliation fails before review when story-owned changed files or task claims are stale.
- CLI human text output receives parity pins whenever machine JSON behavior changes.
- HFCM9002 is either implemented as a production SourceTools sidecar path or kept explicitly
  synthetic/manual-only in adopter-facing docs.
- Testing package evidence redaction cannot regress without default-lane test failure.

## 6. Checklist Results

| Checklist item | Status | Notes |
|---|---|---|
| 1.1 Triggering story identified | [x] | Trigger is Epic 7 retrospective, covering Stories 7.1-7.5 follow-through. |
| 1.2 Core problem defined | [x] | Known retrospective actions lack current backlog/action-item traceability. |
| 1.3 Evidence gathered | [x] | Retro, sprint status, contracts, READMEs, docs, and epics were checked. |
| 2.1 Current epic evaluated | [x] | Epic 7 remains complete; no reopening recommended. |
| 2.2 Epic-level changes identified | [x] | Add post-MVP Epic 10 or renumber to next available if Epic 9 is rejected. |
| 2.3 Remaining epics reviewed | [x] | Epic 8 done; one open action overlaps E7-AI-1. |
| 2.4 New epic need checked | [x] | New tooling-governance follow-up recommended. |
| 2.5 Priority/order checked | [x] | Backlog after pending Epic 9 proposal to avoid numbering collision. |
| 3.1 PRD conflicts checked | [x] | No authored PRD; `epics.md` acts as requirements inventory and PRD proxy. |
| 3.2 Architecture conflicts checked | [x] | No immediate architecture conflict; update only if HFCM9002 decision changes. |
| 3.3 UI/UX conflicts checked | [N/A] | No user-facing UI behavior change proposed. |
| 3.4 Other artifacts checked | [x] | Sprint status, contracts, CLI README, Testing README, and how-to docs reviewed. |
| 4.1 Direct Adjustment evaluated | [x] | Viable; effort Medium, risk Medium. |
| 4.2 Rollback evaluated | [N/A] | No rollback would help. |
| 4.3 MVP review evaluated | [N/A] | MVP scope unaffected. |
| 4.4 Recommended path selected | [x] | Direct Adjustment with backlog reorganization. |
| 5.1 Issue summary created | [x] | Included above. |
| 5.2 Impact documented | [x] | Included above. |
| 5.3 Path forward documented | [x] | Included above. |
| 5.4 MVP/action plan documented | [x] | No MVP reduction; add Epic 10/action items. |
| 5.5 Handoff plan established | [x] | Included above. |
| 6.1 Checklist completion reviewed | [x] | Approval received and follow-through edits applied. |
| 6.2 Proposal accuracy checked | [x] | Cross-checked against current artifacts. |
| 6.3 Explicit user approval | [x] | Administrator approved on 2026-07-01. |
| 6.4 Sprint status update | [x] | Epic 10 backlog entries and action items applied to `sprint-status.yaml`. |
| 6.5 Next steps and handoff | [x] | Moderate-scope handoff routed to PO, QA automation, Technical Writer, Test Architect, Architect, and Developer. |

## 7. Approval and Routing

Approved by Administrator on 2026-07-01.

Change scope: Moderate.

Artifacts updated:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Routed to:

- Product Owner for Epic 10 sequencing and story creation.
- QA automation maintainer for Story 10.1.
- Technical Writer for Story 10.2 and Story 10.4 documentation outcomes.
- Test Architect for Story 10.3 and Story 10.5 lane definition.
- Architect + Product Owner for Story 10.4 decisioning.
- Developer agent for approved implementation stories.
