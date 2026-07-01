# Sprint Change Proposal: Epic 8 Retro Follow-Through

Date: 2026-07-01
Trigger: `_bmad-output/implementation-artifacts/epic-8-retro-2026-06-25.md`
Mode: Batch
Status: approved
Approval: Administrator, 2026-07-01

> Note: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01.md` already exists for a
> separate Epic 2 follow-up, so this proposal uses a scoped file name and does not overwrite it.

## 1. Issue Summary

Epic 8 is complete and should not be reopened. All seven Epic 8 stories are marked done, and the
retrospective confirms that the shipped shell chrome, navigation rail, toolbar, density, and status
icon changes landed inside the Fluent v5 and Fluent 2-token constraints.

The course-correction issue is follow-through: the retrospective captured five open action items, but
they remain as broad owner rows rather than operational execution criteria. The repeated defects found
in Epic 8 reviews were not product-scope defects; they were process and evidence gaps:

- Fluent component styling was sometimes asserted from source text instead of proving attachment to
  rendered DOM.
- Story records and task wording drifted from actual implementation files and test ownership.
- Public reference docs were corrected, but their review metadata still does not reflect the Epic 8
  synchronization pass.
- The accent-as-thread guard exists, but future chrome stories need it named as a mandatory governance
  lane.
- Local browser and VSTest socket blockers were documented, but visual/browser evidence still needs
  named CI ownership rather than remaining a generic caveat.

Evidence reviewed:

- Epic 8 retrospective action items E8-AI-1 through E8-AI-5.
- `_bmad-output/implementation-artifacts/sprint-status.yaml`, which already lists the five Epic 8
  action items as open.
- Story 8.1, 8.4, 8.5, 8.6, and 8.7 review notes, which show the recurring rendered-DOM, stale task,
  and browser-evidence issues.
- `docs/reference/components/front-composer-shell.md`, `navigation.md`, and `page-toolbar.md`; the
  bodies are current, but shell/navigation front matter still says `reviewed: 2026-06-03`.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`, which contains the
  Story 8.2 accent-as-background guard.

## 2. Impact Analysis

Epic impact:

- Epic 8 remains done. No completed story status should change.
- No product epic should be added for this follow-through bundle. The work is process, documentation,
  governance, and CI evidence, not a user-facing feature.
- Avoid assigning this bundle to Epic 9 while the separate `sprint-change-proposal-2026-07-01.md`
  proposes Epic 9 for row-level new-item producer work.

Story impact:

- No Story 8.x implementation story is reopened.
- Future visual/chrome stories need a ready/review gate requiring rendered-DOM or computed-style proof.
- Future story reviews need a mechanical file-list and task-reconciliation check before promotion.

Artifact impact:

- No authored PRD exists. `epics.md` remains the requirements inventory and PRD proxy.
- `epics.md` does not need new product scope for this bundle.
- `sprint-status.yaml` should keep the existing action items but prefix them with stable IDs and clearer
  success criteria.
- `docs/reference/components/front-composer-shell.md` and `docs/reference/components/navigation.md`
  should update `reviewed` metadata to the Epic 8 synchronization date.
- A reusable visual-component evidence checklist should be added as an implementation artifact.
- Browser/visual evidence gaps should be tracked in a named evidence ledger or release-evidence row.

Technical impact:

- No code-path rollback is useful.
- The accent-as-background guard already exists; this proposal routes it into future story verification
  rather than inventing a second guard.
- CI evidence remains the right owner for Playwright/browser lanes when local socket restrictions block
  Kestrel or VSTest.

## 3. Recommended Approach

Selected path: Direct Adjustment through an Epic 8 retro follow-through bundle.

Scope classification: Moderate.

Rationale:

- The shipped Epic 8 behavior is sound, and the remaining risk is recurrence of review-time defects.
- The smallest useful change is to turn the five retro action items into explicit review, docs, and CI
  obligations.
- A new product epic would overstate the issue and collide with the separate proposed Epic 9 follow-up.
- Rollback and MVP review are not applicable.

Effort estimate: Low to Medium. Most changes are documentation/checklist work; the only automation risk
is the mechanical file-list reconciliation.

Risk level: Low. The main risk is process churn if the checklist becomes vague. Each proposed item is
therefore tied to a concrete artifact or command lane.

## 4. Detailed Change Proposals

### 4.1 `sprint-status.yaml` - Preserve Action Items, Add Stable IDs

Section: `action_items`

OLD:

```yaml
action_items:
  - epic: 8
    action: "Add a reusable visual-component checklist that requires rendered-DOM or computed-style proof for Fluent component layout/CSS changes"
    owner: "Test Architect"
    status: open
  - epic: 8
    action: "Keep shell/navigation reference docs synchronized with Story 8.3 and Story 8.5 public-surface changes"
    owner: "Technical Writer"
    status: open
  - epic: 8
    action: "Make changed-file and story-task reconciliation mechanical before story review completion"
    owner: "QA automation maintainer"
    status: open
  - epic: 8
    action: "Preserve the accent-as-thread guard as a mandatory Shell governance lane for future chrome work"
    owner: "Architect"
    status: open
  - epic: 8
    action: "Track browser/visual evidence gaps as named CI responsibilities rather than local unknowns"
    owner: "QA Engineer"
    status: open
```

NEW:

```yaml
action_items:
  - epic: 8
    action: "E8-AI-1: Add a reusable visual-component checklist that requires rendered-DOM or computed-style proof for Fluent component layout/CSS changes before review promotion"
    owner: "Test Architect"
    status: open
  - epic: 8
    action: "E8-AI-2: Synchronize shell/navigation public reference metadata with Story 8.3 and Story 8.5 and record future doc-drift sweeps"
    owner: "Technical Writer"
    status: open
  - epic: 8
    action: "E8-AI-3: Make changed-file and story-task reconciliation mechanical before story review completion"
    owner: "QA automation maintainer"
    status: open
  - epic: 8
    action: "E8-AI-4: Require the Shell accent-as-thread governance guard in every future chrome/story verification record"
    owner: "Architect"
    status: open
  - epic: 8
    action: "E8-AI-5: Track browser/visual evidence gaps as named CI responsibilities with owner, lane, and closure evidence"
    owner: "QA Engineer"
    status: open
```

Rationale: The existing rows are valid; stable IDs make follow-up auditable without adding a product epic.

### 4.2 Add Visual Component Evidence Checklist

Artifact: `_bmad-output/implementation-artifacts/visual-component-evidence-checklist.md`

NEW:

```md
# Visual Component Evidence Checklist

Use this checklist for any story that changes Fluent component layout, scoped CSS, generated UI markup,
hover/focus/touch behavior, or visual baselines.

| Item | Required evidence |
| --- | --- |
| Rendered DOM attachment | bUnit or browser evidence showing the styled node actually exists in rendered markup. |
| Scoped CSS reachability | Proof that CSS isolation selectors reach a real node, or use inline/component parameters when no scoped node exists. |
| Fluent web-component targeting | Proof against actual Fluent v5 light DOM/parts before using `::part()` or tag selectors. |
| Computed-style or behavior proof | Browser/computed-style evidence when source inspection cannot prove the visual result. |
| Accessibility interaction | Keyboard focus, hover, and touch paths named when tooltip or icon-only UI changes. |
| Visual/browser lane ownership | If local Playwright/Kestrel is blocked, record the CI lane, owner, and expected artifact path. |
| Snapshot/baseline intent | Verify snapshots and visual baselines are either unchanged by evidence or intentionally updated. |
```

Rationale: Stories 8.4 and 8.6 both showed that source-string tests can pass while visual styling is
dead. This checklist turns the lesson into a ready/review gate.

### 4.3 Add Story Review Reconciliation Checklist

Artifact: `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md`

NEW:

```md
# Story Review Reconciliation Checklist

Before moving a story from review to done:

1. Compare changed files against the story File List.
   - `git status --short`
   - `git diff --name-only <story-baseline>...HEAD` when a baseline commit is recorded
2. Compare completed task names against actual touched tests and implementation files.
3. Identify generated, QA, e2e, and documentation files separately.
4. Explicitly exclude unrelated dirty files with a short reason.
5. Record test-count deltas and any pre-existing failing lanes.
6. Do not promote the story until File List, task claims, and verification evidence agree.
```

Rationale: Epic 8 reviews still found stale task names and omitted/incorrect evidence. This should be a
mechanical review step, not reviewer memory.

### 4.4 Synchronize Public Docs Review Metadata

Artifact: `docs/reference/components/front-composer-shell.md`

OLD:

```yaml
reviewed: 2026-06-03
```

NEW:

```yaml
reviewed: 2026-06-25
```

Artifact: `docs/reference/components/navigation.md`

OLD:

```yaml
reviewed: 2026-06-03
```

NEW:

```yaml
reviewed: 2026-06-25
```

Rationale: The document bodies already describe the appended `HeaderLogo` /
`ShowDefaultHeaderLogo` shell surface and the unified navigation rail. The metadata should reflect the
Epic 8 documentation sweep. `docs/reference/components/page-toolbar.md` already has
`reviewed: 2026-06-25`.

### 4.5 Name Accent Guard as Mandatory Chrome Verification

Artifact: `tests/README.md`

Section: CI Quarantine Governance, after the main blocking lane command.

NEW:

```md
For Shell chrome or visual stories, the verification record must name the Shell Fluent governance lane,
including `FluentConformanceTests.Shell_chrome_styles_never_use_accent_as_surface_background`, so the
accent-as-thread rule remains mandatory evidence rather than an implicit background guard.
```

Rationale: Story 8.2 already implemented the guard. The process change is to make future chrome stories
name it explicitly.

### 4.6 Track Browser and Visual Evidence as Named CI Responsibilities

Artifact: `docs/accessibility-verification/README.md`

Section: after "Visual Baseline Changes".

NEW:

```md
## Named CI Evidence Follow-Ups

When local browser execution is blocked, the story or retrospective must name the CI owner, lane, and
artifact path expected to close the evidence gap. A generic "Playwright is socket-blocked locally" note
is not enough for promotion when the story changes visual, hover/focus, touch, or screenshot-baseline
behavior.
```

Initial Epic 8 rows to track:

```md
| ID | Source | Lane | Owner | Closure evidence |
| --- | --- | --- | --- | --- |
| E8-CI-1 | Story 8.1 review | Windows visual baseline update lane | QA Engineer | Updated win32 visual baselines or explicit non-update decision. |
| E8-CI-2 | Story 8.3 review | Shell chrome Playwright lane | QA Engineer | Browser assertion for custom-logo non-decorative branch or documented supersession by bUnit pin. |
| E8-CI-3 | Story 8.7 review | Status icon tooltip/touch browser lane | QA Engineer | Playwright browser run with the hasTouch-scoped test passing. |
```

Rationale: Epic 8 handled local blockers honestly, but evidence closure needs named ownership.

## 5. Checklist Progress

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Trigger identified | [x] | Trigger is the Epic 8 retrospective, especially action items E8-AI-1 through E8-AI-5. |
| 1.2 Core problem defined | [x] | Process/evidence follow-through after completed visual work; not a product defect. |
| 1.3 Evidence gathered | [x] | Retro, sprint-status action items, story review notes, docs, and governance tests reviewed. |
| 2.1 Current epic assessed | [x] | Epic 8 remains done; no story should be reopened. |
| 2.2 Epic-level changes checked | [x] | No product epic change recommended. Use action-item bundle. |
| 2.3 Remaining epics reviewed | [x] | No defined future epic is invalidated; separate proposed Epic 9 should not be reused here. |
| 2.4 New epic need checked | [x] | New product epic is not needed. |
| 2.5 Priority/order checked | [x] | Execute before the next visual/chrome story. |
| 3.1 PRD impact assessed | [N/A] | No authored PRD; `epics.md` is the requirements inventory and is unchanged. |
| 3.2 Architecture impact assessed | [x] | Accent-as-thread architecture rule already exists; process must require its guard lane. |
| 3.3 UX impact assessed | [x] | Public docs are current in body; shell/navigation reviewed metadata needs update. |
| 3.4 Other artifacts assessed | [x] | Sprint status, tests README, accessibility evidence docs, and new checklists proposed. |
| 4.1 Direct adjustment evaluated | [x] | Viable; effort Low-Medium, risk Low. |
| 4.2 Rollback evaluated | [N/A] | No completed work should be reverted. |
| 4.3 MVP review evaluated | [N/A] | MVP and product scope are unaffected. |
| 4.4 Path selected | [x] | Direct Adjustment via retro follow-through bundle. |
| 5.1 Issue summary created | [x] | See Section 1. |
| 5.2 Impact documented | [x] | See Section 2. |
| 5.3 Recommended path documented | [x] | See Section 3. |
| 5.4 MVP/action plan documented | [x] | No MVP change; process/docs/CI action plan only. |
| 5.5 Handoff plan established | [x] | Test Architect, Technical Writer, QA automation maintainer, Architect, and QA Engineer. |
| 6.1 Checklist reviewed | [x] | All applicable items covered. |
| 6.2 Proposal accuracy checked | [x] | Based on local artifacts available on 2026-07-01. |
| 6.3 User approval | [x] | Approved by Administrator on 2026-07-01. |
| 6.4 Sprint status update | [x] | Approved E8-AI action IDs and success criteria applied. |
| 6.5 Handoff confirmation | [x] | Routed to Test Architect, Technical Writer, QA automation maintainer, Architect, and QA Engineer. |

## 6. Implementation Handoff

Scope classification: Moderate.

Routing:

- Test Architect: Own E8-AI-1 and approve the visual-component evidence checklist.
- Technical Writer: Own E8-AI-2 and apply the two reviewed-date metadata updates.
- QA automation maintainer: Own E8-AI-3 and turn the reconciliation checklist into a repeatable review
  preflight.
- Architect: Own E8-AI-4 and keep the accent-as-thread governance lane mandatory for future Shell chrome
  work.
- QA Engineer: Own E8-AI-5 and close the named CI evidence rows or record explicit non-update decisions.

Success criteria:

- `sprint-status.yaml` action items carry stable E8-AI IDs.
- Visual/chrome story reviews include rendered-DOM or computed-style evidence when styling Fluent
  components.
- Story review promotion includes changed-file and task reconciliation evidence.
- Shell/navigation public docs show the Epic 8 review date.
- Future Shell chrome stories name the accent-as-background guard lane.
- Browser/visual blockers have owner, CI lane, and closure artifact instead of generic local-blocker text.

Approval received and approved implementation updates applied on 2026-07-01.

## 7. Approval and Handoff Log

- 2026-07-01: Approved by Administrator.
- Scope classification: Moderate.
- Artifacts updated from this proposal: `sprint-status.yaml`, shell/navigation public docs metadata,
  visual and story-review checklists, `tests/README.md`, and `docs/accessibility-verification/README.md`.
- Routed to: Test Architect, Technical Writer, QA automation maintainer, Architect, and QA Engineer.
