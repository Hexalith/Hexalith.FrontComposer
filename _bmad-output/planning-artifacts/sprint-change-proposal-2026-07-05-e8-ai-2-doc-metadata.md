# Sprint Change Proposal: E8-AI-2 Shell/Navigation Reference Metadata

Date: 2026-07-05
Trigger: E8-AI-2 from `_bmad-output/implementation-artifacts/sprint-status.yaml`
Mode: Batch
Status: approved and applied
Approval: Administrator requested the E8-AI-2 correction on 2026-07-05.

## 1. Issue Summary

Epic 8 changed two public component surfaces:

- Story 8.3 appended the `FrontComposerShell` `HeaderLogo` and `ShowDefaultHeaderLogo` parameters.
- Story 8.5 replaced the old full-nav plus `FcCollapsedNavRail` split with the unified
  `FrontComposerNavigation` rail and projection flyout.

The public reference page bodies already describe those changes and both pages already carry
`reviewed: 2026-06-25`, but the `ownerStory` metadata still pointed to the original Story 1.5 FC-DOC
authoring story. E8-AI-2 stayed open because public reference metadata and future doc-drift sweep
practice were not fully closed.

## 2. Impact Analysis

Epic impact:

- Epic 8 remains done. No implementation story is reopened.
- No PRD, architecture, or UX scope changes are required.

Story impact:

- Story 8.3 becomes the metadata owner for the shell page because it owns the latest material shell
  public parameter change.
- Story 8.5 becomes the metadata owner for the navigation page because it owns the current rail/flyout
  public behavior.

Artifact impact:

- `docs/reference/components/front-composer-shell.md`
- `docs/reference/components/navigation.md`
- `_bmad-output/implementation-artifacts/doc-drift-sweep-checklist.md`
- `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Technical impact:

- Documentation and BMAD artifact changes only. No runtime code, package, submodule, schema, generated
  output, or public API baseline changes.

## 3. Recommended Approach

Selected path: Direct Adjustment.

Rationale:

- The source and public docs are already behaviorally aligned.
- The remaining issue is metadata provenance plus a repeatable future sweep record.
- Rollback and MVP review do not apply.

Effort estimate: Low.
Risk level: Low.
Scope classification: Minor.

## 4. Detailed Change Proposals

### 4.1 FrontComposerShell Metadata

Artifact: `docs/reference/components/front-composer-shell.md`

OLD:

```yaml
ownerStory: 1-5-produce-the-fc-doc-component-documentation-contract
reviewed: 2026-06-25
```

NEW:

```yaml
ownerStory: 8-3-brand-logo-cell-in-header-start
reviewed: 2026-06-25
```

Rationale: the page's current public shell parameter surface includes Story 8.3's appended logo
parameters.

### 4.2 Navigation Metadata

Artifact: `docs/reference/components/navigation.md`

OLD:

```yaml
ownerStory: 1-5-produce-the-fc-doc-component-documentation-contract
reviewed: 2026-06-25
```

NEW:

```yaml
ownerStory: 8-5-icon-label-navigation-rail-and-projection-flyout
reviewed: 2026-06-25
```

Rationale: the page's current public navigation behavior is Story 8.5's unified rail and projection
flyout.

### 4.3 Future Doc-Drift Sweeps

Artifact: `_bmad-output/implementation-artifacts/doc-drift-sweep-checklist.md`

NEW:

- Add a reusable checklist for post-story and post-epic documentation drift sweeps.
- Require `ownerStory`, `reviewed`, current body text, no-update decisions, and validation evidence.
- Add a 2026-07-05 closure record for E8-AI-2.

Artifact: `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md`

NEW:

- Require the doc-drift sweep checklist before promotion when a story or review fix changes public
  component, route, CLI, diagnostic, or adopter-facing behavior.

### 4.4 Sprint Status

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
  - epic: 8
    action: "E8-AI-2: Synchronize shell/navigation public reference metadata with Story 8.3 and Story 8.5 and record future doc-drift sweeps"
    owner: "Technical Writer"
    status: open
```

NEW:

```yaml
  - epic: 8
    action: "E8-AI-2: Synchronize shell/navigation public reference metadata with Story 8.3 and Story 8.5 and record future doc-drift sweeps"
    owner: "Technical Writer"
    status: done
```

## 5. Checklist Progress

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Trigger identified | [x] | Trigger is E8-AI-2. |
| 1.2 Core problem defined | [x] | Metadata provenance and repeatable doc-drift sweep record were incomplete. |
| 1.3 Evidence gathered | [x] | Reviewed Epic 8 retro, Story 8.3, Story 8.5, existing docs, sprint status, and docs validation rules. |
| 2.1 Current epic assessed | [x] | Epic 8 remains done. |
| 2.2 Epic-level changes checked | [x] | No epic scope change required. |
| 2.3 Remaining epics reviewed | [x] | Future Epic 10/11 process rows remain independent. |
| 2.4 New epic need checked | [N/A] | No new epic needed. |
| 2.5 Priority/order checked | [x] | Close before future public component work uses stale metadata. |
| 3.1 PRD impact assessed | [N/A] | No PRD requirement changes. |
| 3.2 Architecture impact assessed | [N/A] | No architecture changes. |
| 3.3 UX impact assessed | [x] | Public component docs stay aligned with implemented shell/navigation behavior. |
| 3.4 Other artifacts assessed | [x] | Sprint status and review checklist updated. |
| 4.1 Direct adjustment evaluated | [x] | Viable, low effort, low risk. |
| 4.2 Rollback evaluated | [N/A] | No shipped implementation should be reverted. |
| 4.3 MVP review evaluated | [N/A] | MVP scope unaffected. |
| 4.4 Path selected | [x] | Direct Adjustment. |
| 5.1 Issue summary created | [x] | See Section 1. |
| 5.2 Impact documented | [x] | See Section 2. |
| 5.3 Recommended path documented | [x] | See Section 3. |
| 5.4 MVP/action plan documented | [x] | No MVP change; docs/process action only. |
| 5.5 Handoff plan established | [x] | Technical Writer owns metadata and future doc sweeps. |
| 6.1 Checklist reviewed | [x] | All applicable items covered. |
| 6.2 Proposal accuracy checked | [x] | Based on local artifacts on 2026-07-05. |
| 6.3 User approval | [x] | Administrator requested E8-AI-2 execution. |
| 6.4 Sprint status update | [x] | E8-AI-2 marked done. |
| 6.5 Handoff confirmation | [x] | Future sweeps routed through the doc-drift checklist. |

## 6. Implementation Handoff

Scope classification: Minor.

Route to: Technical Writer and Developer agent for direct documentation maintenance.

Success criteria:

- Shell and navigation reference pages carry Story 8.3 and Story 8.5 owner metadata respectively.
- Future public-surface work has a documented doc-drift sweep checklist.
- E8-AI-2 is marked done in sprint status.

## 7. Approval and Handoff Log

- 2026-07-05: Administrator requested E8-AI-2 execution.
- 2026-07-05: Proposal applied in-tree.
- Artifacts modified: shell/navigation component docs, sprint status, story review checklist, doc-drift
  sweep checklist.
