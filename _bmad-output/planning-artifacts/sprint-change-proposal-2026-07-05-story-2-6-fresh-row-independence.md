---
title: Sprint Change Proposal - Story 2.6 Fresh-Row Independence Cleanup
status: approved
date: 2026-07-05
owner: Product Owner / Developer
trigger:
  report: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-05.md
  finding: Critical 1 - Story 2.6 has a forward dependency on Epic 9
mode: Batch
scope: Moderate
approval: approved 2026-07-05
---

# Sprint Change Proposal: Story 2.6 Fresh-Row Independence Cleanup

## 1. Issue Summary

The 2026-07-05 implementation-readiness assessment returned `NEEDS_WORK`.

The blocking issue is a planning artifact contradiction: Story 2.6 still says a SignalR projection
change causes the grid to update and marks fresh rows. The current PRD, UX, DataGrid reference, and
Epic 9 text all say automatic row-level fresh-item marking is owned by FC-NIP and must not be inferred
from projection nudges that lack row identity.

This makes Epic 2 appear dependent on Epic 9 behavior and breaks epic independence. Current
`sprint-status.yaml` records Story 2.6's fresh-row behavior as accepted-deferred and later normalized
to Epic 9 / FC-NIP, and it now records Story 9.2 as done on 2026-07-05. The plan text still needs to
match that reality so future readiness checks and story creation do not over-count Epic 2 scope.

Evidence reviewed:

- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-05.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md`
- `_bmad-output/planning-artifacts/ux-design.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/DESIGN.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/EXPERIENCE.md`
- `docs/reference/components/datagrid.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## 2. Impact Analysis

### Epic Impact

Epic 2 remains valid if Story 2.6 is scoped to live projection refresh, reconnect state, and missed-change
reconciliation. It must not claim row-level fresh-item producer behavior.

Epic 9 remains the FC-NIP owner. Story 9.1 records the row-identity producer contract decision; Story 9.2
owns producer and generated-grid consumer wiring. Since current sprint status marks 9.2 done, the correction
is an artifact-ownership cleanup, not a code dependency on unfinished Epic 9 work.

Epic 11 is not invalidated, but its implementation-order warning remains a planning hazard. The readiness
report's Epic 11 recommendations should be captured in the same cleanup batch before implementation story
selection continues.

No new epic is required and no completed epic needs rollback.

### Story Impact

Stories requiring direct wording cleanup:

- Story 2.6: remove fresh-row marking from Epic 2 acceptance criteria and point ownership to Epic 9 / Story 9.2.
- Story 2.3: replace stale `FcStatusBadge` status-pill wording with the approved UX-DR2 status-icon model.
- Story 3.6: replace "budgets are unset" re-decision wording with verification of the confirmed AR8 budgets.
- Story 4.3: replace "queue/block per contract" with the approved FC-CNC v1 behavior: block later local submits.
- Story 11.4: anchor security hardening to PRD NFR-5 and NFR-6.
- Story 11.6: remove duplicated wording.
- Stories 11.17, 11.18, 11.19: mark as split-before-dev or replace with smaller implementation stories before they are created for development.

### Artifact Conflicts

PRD:

- Root PRD is authoritative and has the stronger FR-24 release evidence requirement.
- Nested BMad PRD copy still has weaker FR-24 wording and D-6 release-evidence wording. It should be reconciled to the root PRD.

Epics:

- Story 2.6 still conflicts with PRD FR-13 / FR-26 and the Epic 9 source-of-record text.
- The formal FR Coverage Map stops at FR26 even though the canonical PRD has FR-27 through FR-29. Add an explicit PRD v1-readiness coverage addendum.

Architecture:

- No architecture rewrite is required. Architecture already supports the separation: EventStore command acceptance is not projection-confirmed success, and projection nudges are not row identity.

UX:

- UX already supports the correction. `EXPERIENCE.md` says FC-NIP fresh-row indicators remain blocked until row identity is confirmed and broad row marking or diff-based inference is not allowed.
- Navigation / module-tab / projection-flyout open questions should be resolved story-locally before route/navigation implementation work.

Sprint status:

- Do not change story statuses as part of this proposal. After approval and artifact edits, add a note/action record that Story 2.6 wording was normalized to match the accepted-deferred FC-NIP ownership.

### Technical Impact

No source code, generated code, package, database, or infrastructure change is required by this proposal. The output is planning-artifact cleanup plus a readiness rerun.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The problem is stale planning text, not a failed implementation approach.
- Rollback is not useful because current implementation/status already records the deferral and Story 9.2 completion.
- MVP scope does not need reduction because the PRD already assigns row-level fresh-item producer wiring to Epic 9 / FC-NIP.
- A single planning cleanup batch can remove the critical blocker and the readiness report's major stale-wording hazards.

Effort: Medium planning pass.

Risk: Low to medium. The main risk is traceability drift if only Story 2.6 is patched and the related PRD/UX/story wording remains stale.

Timeline impact: one artifact update pass plus one implementation-readiness rerun before marking the planning set implementation-ready.

## 4. Detailed Change Proposals

### 4.1 `epics.md` - Story 2.6 Acceptance Criteria

Story: 2.6 Live projection updates with reconnect & reconciliation

Section: Acceptance Criteria

OLD:

```markdown
**Given** an active projection subscription over SignalR,
**When** the backend emits a change,
**Then** the grid updates and a "new item" indicator marks fresh rows. *(FR13, FR14)*
```

NEW:

```markdown
**Given** an active projection subscription over SignalR,
**When** the backend emits a projection change,
**Then** the grid refreshes or reconciles the affected projection lane and surfaces read-path freshness
without marking individual rows as new. *(PRD FR-12, UX-DR5)*

**Given** automatic row-level fresh-item marking is required,
**When** a command outcome carries the confirmed FC-NIP row metadata,
**Then** Epic 9 / Story 9.2 owns producing and rendering `FcNewItemIndicator`; Story 2.6 does not infer
row identity from projection nudges. *(PRD FR-13, FR-26)*
```

Rationale: Epic 2 can be independently complete with live refresh and reconnect/reconciliation. Row-level
fresh marking belongs to FC-NIP and requires row identity, not a broad SignalR nudge.

### 4.2 `epics.md` - Epic 2 Summary

Section: Epic 2 summary

OLD:

```markdown
fed live from EventStore over SignalR/HTTP.
```

NEW:

```markdown
fed live from EventStore over SignalR/HTTP, with row-level fresh-item indicators delegated to Epic 9 / FC-NIP.
```

Rationale: Prevents the Epic 2 summary from implying row-level new-item producer scope.

### 4.3 `epics.md` - Story 2.3 Status Model

Story: 2.3 DataGrid filtering, status, and empty/loading states

Section: Acceptance Criteria

OLD:

```markdown
**Given** status-enum columns mapped via `[ProjectionBadge]`,
**When** rendered,
**Then** `FcStatusBadge`/`FcDesaturatedBadge` render with a mandatory `aria-label`. *(UX-DR2, NFR6)*
```

NEW:

```markdown
**Given** status-enum columns mapped via `[ProjectionBadge]`,
**When** rendered,
**Then** status members render as colored Fluent icons with hover and keyboard-focus tooltip labels plus
an always-present `aria-label`; numeric count slots remain `FluentBadge` / `FcDesaturatedBadge` pills.
*(UX-DR2, NFR6)*
```

Rationale: Aligns Story 2.3 with UX-DR2, Story 8.7, and the DataGrid reference.

### 4.4 `epics.md` - Story 3.6 Confirmed Budgets

Story: 3.6 Apply confirming-to-degraded and polling budgets

Section: Acceptance Criteria

OLD:

```markdown
**Given** the budgets are unset,
**When** Product/UX + EventStore review,
**Then** the threshold and polling budget values are decided and recorded (deterministic, testable via `FakeTimeProvider`). *(NFR10)*
```

NEW:

```markdown
**Given** the AR8 budgets confirmed on 2026-06-21,
**When** Product/UX and EventStore evidence is reviewed,
**Then** the implementation verifies the recorded values: confirming-to-degraded threshold `10_000` ms,
polling cadence `1_000` ms, polling max `120_000` ms, Epic 3 retry budget `0`, and Epic 4 retry
budget `1 x 250` ms, all deterministic and testable via `FakeTimeProvider`. *(NFR10)*
```

Rationale: The story should verify approved budgets, not reopen a closed decision.

### 4.5 `epics.md` - Story 4.3 FC-CNC Policy

Story: 4.3 One-at-a-time execution policy

Section: Acceptance Criteria

OLD:

```markdown
**Given** an in-flight command,
**When** I submit another,
**Then** the one-at-a-time policy applies the approved fallback (queue/block per contract) rather than racing. *(AR7)*
```

NEW:

```markdown
**Given** an in-flight command,
**When** I submit another local command,
**Then** FC-CNC v1 blocks the later local submit with support-safe feedback rather than queueing,
batching, or racing. *(AR7, PRD FR-16)*
```

Rationale: PRD and architecture state the v1 policy is block-later-local-submit. Queueing and batching are fast-follow/out-of-scope.

### 4.6 `epics.md` - Epic 11 Implementation Order

Section: Immediately before Epic 11 detailed stories

OLD:

```markdown
Suggested order: 11.0 -> 11.1 -> 11.2 -> 11.4 -> 11.3 -> 11.5 -> 11.6 -> 11.7 -> 11.9/11.15/11.16 -> 11.17/11.18/11.19 -> 11.8/11.11-11.14 last.
Story creation order is authoritative. Do not infer the next story from file order or numeric sort.
```

NEW:

```markdown
### Epic 11 Implementation Order

| Order | Story or group | Status / rule |
| --- | --- | --- |
| 1 | 11.0 route-contract decision | Done 2026-07-05; historical gate, not an implementation candidate. |
| 2 | 11.1 token lifecycle and circuit-safe auth | Next implementation candidate after gates. |
| 3 | 11.2 projection realtime resilience | Implement before lower-risk cleanup. |
| 4 | 11.4 security-validation hardening | Implement as three independently verifiable task groups. |
| 5 | 11.3 MCP cross-request lifecycle and operability | Implement after security validation setup. |
| 6 | 11.5 dead CSS and visual-conformance guards | Guard-first. |
| 7 | 11.6 Testing harness failure modes | Required for adopter failure-path testing. |
| 8 | 11.7 command/projection route implementation | Requires 11.0 done. |
| 9 | 11.9 / 11.15 / 11.16 consolidation stories | Lower-risk remediation group. |
| 10 | 11.17 / 11.18 / 11.19 | Split before ready-for-dev; do not implement as broad bundles. |
| 11 | 11.8 Contracts split decision | Done 2026-07-05; historical gate, not an implementation candidate. |
| 12 | 11.11 / 11.12 / 11.13 / 11.14 | Implement last; package-boundary and public-API evidence required. |

Story creation follows this table over heading order, numeric sort, or file order.
```

Rationale: Prevents human or automated next-story selection from following misleading heading order.

### 4.7 `epics.md` - Story 11.4 Security NFR Anchor

Story: 11.4 Security-validation hardening

Section: Closing trace sentence

OLD:

```markdown
*(Security hardening - no security NFR yet exists to anchor it (flagged for the requirements inventory); closes H7, H9, M11.)*
```

NEW:

```markdown
*(Anchored to PRD NFR-5 Security and NFR-6 Privacy/support safety; closes H7, H9, M11.)*
```

Rationale: The current PRD has explicit security and privacy/support-safety NFRs.

### 4.8 `epics.md` - Story 11.6 Repeated Wording

Story: 11.6 Testing harness failure modes

Section: Acceptance Criteria

OLD:

```markdown
**Given** the Counter sample's authorization-policy toggles,
**When** those scenarios are promoted into the Testing harness,
**Then** they are promoted into the harness, and the constructor `GetAwaiter().GetResult()` is replaced with an async factory.
```

NEW:

```markdown
**Given** the Counter sample's authorization-policy toggles,
**When** those scenarios are promoted into the Testing harness,
**Then** the harness exposes equivalent configurable authorization-policy states, and the constructor
`GetAwaiter().GetResult()` is replaced with an async factory.
```

Rationale: Removes duplicated wording and makes the expected harness outcome concrete.

### 4.9 `epics.md` - Stories 11.17, 11.18, 11.19 Split-Before-Dev Marker

Stories: 11.17, 11.18, 11.19

Section: Story headings or leading notes

NEW NOTE:

```markdown
> Split-before-dev: this section is a decomposition parent. Do not move it to ready-for-dev until it is
> split by package or defect class into independently reviewable implementation stories with their own
> validation lanes.
```

Suggested child split:

- 11.17a CLI mechanical split.
- 11.17b SourceTools drift split.
- 11.17c skill-corpus/runtime split.
- 11.17d Shell bundle split.
- 11.18a Shell warning-and-above logging.
- 11.18b MCP fail-closed logging.
- 11.18c remaining hot-path logging.
- 11.19a CS1591 enforcement.
- 11.19b NuGet audit policy.
- 11.19c localization/accessibility fixes.
- 11.19d diagnostic constant rename.
- 11.19e analyzer-mode decision.

Rationale: The readiness report found these stories too broad for reliable implementation as written.

### 4.10 `epics.md` - PRD V1 Coverage Addendum

Section: After the existing FR Coverage Map

NEW:

```markdown
### PRD V1 Readiness Coverage Addendum

The legacy FR Coverage Map above is retained for brownfield continuity. The canonical PRD adds explicit
v1-readiness requirements FR-27 through FR-29:

- PRD FR-27 (tooling-governance follow-through): Epic 10.
- PRD FR-28 (Epic 11 decision gates): Story 11.0 route-contract decision and Story 11.8 Contracts split decision.
- PRD FR-29 (architecture-review release risks): Epic 11 implementation stories 11.1 through 11.19, with Stories 11.11 through 11.14 deliberately last.
```

Rationale: Makes FR-27 through FR-29 discoverable for readiness traceability without renumbering the legacy map.

### 4.11 Nested PRD Copy - FR-24 Reconciliation

Artifact: `_bmad-output/planning-artifacts/prds/prd-frontcomposer-2026-07-05/prd.md`

Section: FR-24 requirement and D-6 release-evidence gate

OLD:

```markdown
FrontComposer must release the expected NuGet package set through semantic-release with signed packages, symbols, SBOM, evidence chain, and GitHub Release assets.
```

NEW:

```markdown
FrontComposer must release the expected NuGet package set through semantic-release with signed packages,
symbols, SBOM, checksums, sealed release manifest/evidence chain, GitHub Release assets, and
package-consumer validation evidence.
```

Also mirror the root PRD D-6 wording:

```markdown
Resolved: FR-24 is release governance and is tracked by `sprint-status.yaml` action `REL-AI-1`;
approved 2026-07-05 to route focused release-governance story `REL-1` because release
workflow/governance-test/package-consumer validation changes are required before RC.
```

Rationale: Readiness workflows intentionally used both PRD source sets. They must not disagree on release evidence.

### 4.12 UX / Route Open Questions

Artifact: `_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-07-05/EXPERIENCE.md`

Proposed handling:

- Do not block this Story 2.6 cleanup on the UX open questions.
- For navigation and route stories, require a story-local decision on module-tab route encoding and projection-flyout behavior before implementation.
- Keep the FC-NIP sentence unchanged: "broad row marking or diff-based inference is not allowed."

Rationale: The UX open questions affect route/navigation work, not the Story 2.6 fresh-row ownership fix.

## 5. Implementation Handoff

Scope classification: Moderate.

Handoff recipients:

- Product Owner: approve the proposal and confirm the root PRD remains authoritative.
- Developer / planning maintainer: apply the artifact edits above to `epics.md` and the nested PRD copy.
- Release Owner: keep `REL-AI-1` open until evidence paths or an approved fallback are recorded.
- Story creation workflow owner: follow the Epic 11 implementation-order table over file order.

Implementation sequence after approval:

1. Patch `epics.md` Story 2.6, Story 2.3, Story 3.6, Story 4.3, Story 11.4, Story 11.6, Epic 11 order, split-before-dev markers, and PRD coverage addendum.
2. Patch the nested PRD copy's FR-24 and D-6 wording to mirror root PRD strength.
3. Add a sprint-status note only if the team wants a durable record of the Story 2.6 wording normalization; do not change completed story statuses.
4. Rerun implementation readiness against the root and nested PRD/UX source sets.

Success criteria:

- Story 2.6 no longer claims automatic row-level fresh marking inside Epic 2.
- Epic 9 / Story 9.2 remains the exclusive owner of automatic `FcNewItemIndicator` producer/consumer behavior.
- Story 2.3, 3.6, 4.3, 11.4, and 11.6 no longer carry stale contract wording.
- Epic 11 story selection is unambiguous and does not follow heading order by accident.
- Nested PRD FR-24 matches the stronger root PRD release-evidence requirement.
- Readiness rerun no longer reports the Story 2.6 forward-dependency critical violation.

## 6. Checklist Execution Summary

| Checklist item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story identified | [x] | Story 2.6 fresh-row acceptance criteria. |
| 1.2 Core problem defined | [x] | Stale planning text creates a false forward dependency from Epic 2 to Epic 9. |
| 1.3 Evidence gathered | [x] | Readiness report, epics, PRD, UX, DataGrid docs, sprint status. |
| 2.1 Current epic evaluated | [x] | Epic 2 remains independently complete after Story 2.6 wording is narrowed. |
| 2.2 Epic-level changes identified | [x] | Modify Epic 2 story wording; no new epic. |
| 2.3 Remaining epics reviewed | [x] | Epic 9 ownership stands; Epic 11 order cleanup carried forward. |
| 2.4 New/obsolete epic checked | [x] | No new epic or obsolete epic. |
| 2.5 Epic order checked | [x] | Epic 11 order table recommended; no Epic 2/Epic 9 resequence. |
| 3.1 PRD conflicts checked | [x] | Root/nested FR-24 mismatch needs reconciliation. |
| 3.2 Architecture conflicts checked | [x] | No architecture rewrite required. |
| 3.3 UX conflicts checked | [x] | UX supports no broad row marking; route questions are separate. |
| 3.4 Other artifacts checked | [x] | Sprint status should get a note only after approval; readiness rerun required. |
| 4.1 Direct Adjustment | [x] | Viable; selected. |
| 4.2 Potential Rollback | [N/A] | Rollback would not fix stale planning text. |
| 4.3 PRD MVP Review | [N/A] | MVP remains achievable; no scope reduction. |
| 4.4 Recommended path selected | [x] | Direct Adjustment. |
| 5.1 Issue summary | [x] | Captured above. |
| 5.2 Impact and artifact needs | [x] | Captured above. |
| 5.3 Path rationale | [x] | Captured above. |
| 5.4 PRD MVP/action plan | [x] | No MVP reduction; artifact cleanup and readiness rerun. |
| 5.5 Handoff plan | [x] | Product Owner, Developer/planning maintainer, Release Owner, story workflow owner. |
| 6.1 Checklist reviewed | [x] | No unrecorded blocker remains. |
| 6.2 Proposal accuracy checked | [x] | Cross-checked against current artifacts. |
| 6.3 User approval | [x] | Approved by user on 2026-07-05. |
| 6.4 Sprint status update | [N/A] | No epic/story status mutation proposed before approval. |
| 6.5 Next steps and handoff | [x] | Proceed with approved artifact cleanup and readiness rerun. |

## 7. Approval

Approved by user on 2026-07-05. Proceed with implementation handoff and the approved artifact cleanup.
