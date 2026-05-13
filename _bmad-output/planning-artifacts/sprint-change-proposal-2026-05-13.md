# Sprint Change Proposal: Epic 11 Evidence Alignment and Release Certification

Date: 2026-05-13
Project: Hexalith.FrontComposer
Prepared for: Jerome
Workflow: bmad-correct-course
Mode: Batch
Approval: Approved by Jerome on 2026-05-13
Implementation status: Applied

## 1. Issue Summary

Epic 11 completed all seven implementation stories and the retrospective is now done, but the retrospective identified a release-readiness mismatch: story status, ledger row state, and release evidence do not yet tell the same truth.

Trigger:

- `_bmad-output/implementation-artifacts/epic-11-retro-2026-05-13.md` found that all Epic 11 story rows are `done`, but `epic-11` remains `in-progress`.
- Direct ledger inspection found current reconciliation-owner markers after story completion:
  - Story 11.2: 2 current reconciliation-owner markers.
  - Story 11.4: 7 current reconciliation-owner markers.
  - Story 11.5: 205 current reconciliation-owner markers.
  - One `Needs Product/Architecture decision` marker.
- The retrospective also found release-readiness blockers that are not represented by a new planning artifact: provider-backed pending-command status, trusted release-context evidence, manual accessibility evidence, and stakeholder acceptance evidence.

Core problem:

Story completion has outpaced release-certification evidence. Epic 11 succeeded at routing and burning down much of the deferred-work ledger, but the project now needs an explicit release-certification correction so the remaining row-state and evidence gaps cannot hide behind `done` story statuses.

Issue type:

- Technical/process limitation discovered during retrospective.
- Release-readiness evidence gap.
- Backlog/status tracking mismatch.

## 2. Impact Analysis

### Epic Impact

Epic 11 can remain complete from a story-execution perspective, but it should not be marked `done` at the top level until ledger and release-readiness gates are reconciled.

The current epic plan ends at Epic 11. A new planning container is needed for release certification unless Jerome decides to keep this as a non-epic release checklist.

Recommended epic-level change:

- Add Epic 12: Release Certification and Evidence Alignment.
- Keep Epic 11 as `in-progress` until Epic 12 Story 12.1 confirms whether remaining markers are stale, accepted, split, or genuinely unresolved.
- Do not reopen Epics 1-10.
- Do not roll back Epic 11 stories.

### Story Impact

Proposed new stories:

1. Story 12.1: Ledger Marker Parity and Epic Status Decision
2. Story 12.2: MCP Ledger Closure and Contract Snapshot Decisions
3. Story 12.3: EventStore Pending-Command Provider Release Gate
4. Story 12.4: Trusted Release Evidence Dry Run
5. Story 12.5: Accessibility and Stakeholder Acceptance Evidence Pack

These stories should be backlog entries until explicitly created through the normal story workflow.

### Artifact Conflicts

PRD:

- No PRD goal is invalidated.
- The change supports existing v1 ship criteria: open-source NuGet/GitHub release, accessibility gates, manual screen-reader verification, signed releases, SBOM, EventStore readiness, and MCP agent correctness.

Architecture:

- No architecture reversal is required.
- Existing architecture already treats EventStore, MCP, tenant isolation, release automation, submodule governance, and Pact/provider evidence as load-bearing.
- Architecture may need a small release-certification note if the team wants the final gate to be architectural rather than purely backlog-driven.

UX:

- No UX redesign is required.
- Existing UX accessibility spec already states manual NVDA, JAWS, VoiceOver, and real-device verification blocks release branches.
- The correction should create evidence logs rather than change UI behavior.

Sprint Status:

- `epic-11` should remain `in-progress` until ledger parity and release-readiness policy are decided.
- Add `epic-12` and Story 12.1-12.5 as `backlog` if this proposal is approved.

Deferred Ledger:

- Must be reconciled so current `Reconciliation:` markers match story completion claims.
- Broad summary prose is not sufficient; current row markers are the source of truth.

## 3. Recommended Approach

Selected path: Direct Adjustment.

Scope classification: Moderate.

Rationale:

- Rollback is not justified. The Epic 11 implementations added real validation and governance value.
- PRD MVP review is not needed. The core product goals remain valid.
- A direct backlog adjustment preserves completed work while making release certification explicit.
- Adding a release-certification epic or artifact is cleaner than silently converting remaining release gates into ad hoc tasks.

Risk:

- Current risk is medium if no correction is made, because the repo can appear release-ready while row-state and evidence gates still disagree.
- Proposed change risk is low to medium, because it is planning/status work first and only creates implementation stories for bounded evidence gaps.

## 4. Detailed Change Proposals

### Proposal A: Epics Index and Epic List

Files:

- `_bmad-output/planning-artifacts/epics/index.md`
- `_bmad-output/planning-artifacts/epics/epic-list.md`
- New file: `_bmad-output/planning-artifacts/epics/epic-12-release-certification-evidence-alignment.md`

OLD:

```markdown
- **v1 hardening / release readiness:** Epic 11 -- deferred review findings reconciled into owned backlog work before release certification
```

NEW:

```markdown
- **v1 hardening / release readiness:** Epic 11 -- deferred review findings reconciled into owned backlog work before release certification
- **v1 release certification:** Epic 12 -- ledger parity, provider-backed release gates, trusted release evidence, accessibility verification, and stakeholder acceptance aligned before v1 release
```

Rationale:

Epic 11 routed and implemented deferred hardening. Epic 12 should certify whether the evidence is coherent enough to release.

### Proposal B: New Epic 12 Planning Artifact

File:

- `_bmad-output/planning-artifacts/epics/epic-12-release-certification-evidence-alignment.md`

NEW:

```markdown
# Epic 12: Release Certification and Evidence Alignment

Release owners can certify v1 readiness from row-level ledger state, provider-backed runtime evidence, trusted release-context evidence, manual accessibility logs, and stakeholder acceptance rather than inferring readiness from completed story statuses.

### Story 12.1: Ledger Marker Parity and Epic Status Decision

As a release owner,
I want current deferred-work ledger markers reconciled with completed story evidence,
So that top-level epic status reflects the real release-readiness state.

Acceptance Criteria:

- Current `Reconciliation:` markers for Stories 11.2, 11.4, and 11.5 are audited.
- Each current marker is converted to `Resolved`, `Accepted constraint`, `Split to named story`, `Superseded`, `Non-action decision`, or a deliberately open release gate.
- The one `Needs Product/Architecture decision` row is surfaced with owner, decision, and evidence.
- `epic-11` is either marked `done` with rationale or deliberately left `in-progress` with named blocking rows.

### Story 12.2: MCP Ledger Closure and Contract Snapshot Decisions

As an agent integrator,
I want Story 11.5 ledger closure to match MCP contract evidence,
So that schema negotiation and agent contract readiness are not overstated.

Acceptance Criteria:

- Story 11.5 current owner markers are reconciled row by row or by an explicit row-scoped closure matrix.
- Accepted v1 constraints include owner, downstream impact, evidence, and reopen trigger.
- Any genuine unresolved MCP work becomes a named backlog item.

### Story 12.3: EventStore Pending-Command Provider Release Gate

As a release owner,
I want pending-command status provider readiness resolved,
So that command lifecycle confidence is based on provider-backed behavior.

Acceptance Criteria:

- The project decides whether to implement provider-backed `IPendingCommandStatusQuery` before v1 or accept a named release constraint.
- If implemented, status endpoint behavior is covered for 202, 200 terminal, 304, 429, 503, malformed, duplicate, stale, and provider-exception cases.
- If accepted, the release notes and docs state the limitation and reopen trigger.

### Story 12.4: Trusted Release Evidence Dry Run

As a maintainer,
I want release evidence proven in a trusted context,
So that signing, SBOM, checksums, symbols, attestations, package inventory, and publication ordering cannot produce a false release-ready record.

Acceptance Criteria:

- Trusted-context dry-run or equivalent release evidence is captured.
- No irreversible publish side effect occurs before blocking checks pass.
- Evidence is redacted and bounded.
- Any unavailable external credential or attestation path is recorded as an explicit release blocker or approved fallback.

### Story 12.5: Accessibility and Stakeholder Acceptance Evidence Pack

As a product and quality owner,
I want manual accessibility and stakeholder acceptance evidence captured,
So that release readiness includes the non-automated gates promised by the PRD and UX spec.

Acceptance Criteria:

- Manual screen-reader verification logs are prepared or explicitly deferred with release impact.
- Cross-AT, localization, RTL, and real-device scope are classified as v1 blocker, accepted v1 constraint, or post-v1 roadmap.
- Stakeholder acceptance status is recorded in repository artifacts.
```

Rationale:

This keeps the next work narrow: evidence alignment and release certification, not another broad hardening sweep.

### Proposal C: Sprint Status

File:

- `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
  # Epic 11: Deferred Hardening & Release Readiness
  epic-11: in-progress
  11-1-deferred-work-ledger-reconciliation-and-ownership: done
  11-2-diagnostic-registry-and-documentation-governance-follow-ups: done
  11-3-cli-migration-and-ide-edge-case-hardening: done
  11-4-drift-detection-and-source-generator-coverage-hardening: done
  11-5-mcp-schema-negotiation-and-agent-contract-hardening: done
  11-6-shell-ux-accessibility-and-sample-coverage-follow-ups: done
  11-7-eventstore-reliability-and-ci-governance-follow-ups: done
  epic-11-retrospective: done
```

NEW:

```yaml
  # Epic 11: Deferred Hardening & Release Readiness
  epic-11: in-progress
  11-1-deferred-work-ledger-reconciliation-and-ownership: done
  11-2-diagnostic-registry-and-documentation-governance-follow-ups: done
  11-3-cli-migration-and-ide-edge-case-hardening: done
  11-4-drift-detection-and-source-generator-coverage-hardening: done
  11-5-mcp-schema-negotiation-and-agent-contract-hardening: done
  11-6-shell-ux-accessibility-and-sample-coverage-follow-ups: done
  11-7-eventstore-reliability-and-ci-governance-follow-ups: done
  epic-11-retrospective: done

  # Epic 12: Release Certification and Evidence Alignment
  epic-12: backlog
  12-1-ledger-marker-parity-and-epic-status-decision: backlog
  12-2-mcp-ledger-closure-and-contract-snapshot-decisions: backlog
  12-3-eventstore-pending-command-provider-release-gate: backlog
  12-4-trusted-release-evidence-dry-run: backlog
  12-5-accessibility-and-stakeholder-acceptance-evidence-pack: backlog
  epic-12-retrospective: optional
```

Rationale:

Sprint status should make the release-certification path visible before any agent starts additional work.

### Proposal D: Deferred Work Ledger

File:

- `_bmad-output/implementation-artifacts/deferred-work.md`

OLD:

Current row markers include active Story 11.2, 11.4, and 11.5 reconciliation owners after those stories are marked `done`.

NEW:

Add a top-level section:

```markdown
## Release Certification Routing Status (2026-05-13)

Epic 12 owns release-certification evidence alignment after Epic 11 story completion.

- Story 12.1 owns ledger marker parity and the Epic 11 top-level status decision.
- Story 12.2 owns MCP/Story 11.5 row-state reconciliation and contract snapshot decisions.
- Story 12.3 owns provider-backed pending-command status release gating.
- Story 12.4 owns trusted release evidence dry-run validation.
- Story 12.5 owns manual accessibility and stakeholder acceptance evidence.
```

Rationale:

This prevents old `Owner: Story 11.x` markers from being confused with current release-certification ownership.

### Proposal E: PRD, Architecture, and UX

PRD:

- No immediate text change required.
- Existing success criteria and NFRs already require release, accessibility, EventStore, MCP, and evidence gates.

Architecture:

- Optional small note under Release Automation or API Boundaries:

```markdown
Release certification requires row-level deferred-work parity, provider-backed EventStore gate decisions, trusted release-context evidence, and manual acceptance evidence before v1 release status is claimed.
```

UX:

- No design change required.
- Accessibility verification logs should be produced under the docs/accessibility verification path defined by the UX spec.

## 5. Checklist Summary

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Trigger story identified | [x] Done | Trigger came from Epic 11 retrospective and Story 11.5 ledger-state mismatch. |
| 1.2 Core problem defined | [x] Done | Release-readiness evidence and row state do not fully match story status. |
| 1.3 Evidence gathered | [x] Done | Retro, sprint status, ledger marker counts, PRD, architecture, UX spec, and previous change proposal reviewed. |
| 2.1 Current epic evaluated | [x] Done | Epic 11 stories are done, but top-level epic should remain in progress until evidence alignment is resolved. |
| 2.2 Epic changes determined | [x] Done | Add Epic 12 or equivalent release-certification planning artifact. |
| 2.3 Remaining epics reviewed | [x] Done | No future epic exists after Epic 11. |
| 2.4 New epic needed | [!] Action-needed | Recommend Epic 12 to avoid implicit release-certification work. |
| 2.5 Priority/order reviewed | [x] Done | Release certification should happen before v1 release claims. |
| 3.1 PRD conflicts checked | [x] Done | No conflict; change supports existing PRD gates. |
| 3.2 Architecture conflicts checked | [x] Done | No reversal; optional release-certification note recommended. |
| 3.3 UX conflicts checked | [x] Done | No redesign; manual accessibility logs remain required for release. |
| 3.4 Other artifacts checked | [x] Done | Sprint status and deferred ledger require updates after approval. |
| 4.1 Direct adjustment | [x] Viable | Best path. |
| 4.2 Rollback | [N/A] Skip | No completed work should be reverted. |
| 4.3 PRD MVP review | [N/A] Skip | MVP remains valid. |
| 4.4 Recommended path selected | [x] Done | Moderate direct adjustment. |
| 5.1 Issue summary | [x] Done | Included above. |
| 5.2 Impact and artifact needs | [x] Done | Included above. |
| 5.3 Path forward | [x] Done | Add release-certification planning container. |
| 5.4 MVP impact/action plan | [x] Done | No MVP reduction; add evidence alignment work. |
| 5.5 Handoff plan | [x] Done | See below. |
| 6.1 Checklist completion | [x] Done | Applicable items addressed. |
| 6.2 Proposal accuracy | [x] Done | Based on repository artifacts inspected on 2026-05-13. |
| 6.3 User approval | [x] Done | Jerome approved the proposal on 2026-05-13. |
| 6.4 Sprint status update | [x] Done | Epic 12 and Story 12.1-12.5 backlog entries added to sprint status. |
| 6.5 Handoff confirmation | [x] Done | Handoff is routed to Product, Architecture, Developer, Test Architect, and Product Owner roles as listed below. |

## 6. Implementation Handoff

Change scope: Moderate backlog reorganization and release-certification planning.

Recommended routing:

- John (Product Manager): approve Epic 12 as a planning container or choose a non-epic release checklist instead.
- Winston (Architect): decide whether remaining ledger markers represent architecture decisions, accepted constraints, or required implementation.
- Amelia (Developer): implement approved planning artifact edits, sprint-status updates, and ledger routing section.
- Murat (Test Architect): define release-gate evidence acceptance for EventStore provider status, trusted release dry run, and manual accessibility evidence.
- Alice (Product Owner): confirm stakeholder acceptance evidence requirements and whether any accepted constraints need release-note language.

Success criteria:

- A release-certification planning artifact exists.
- Sprint status exposes the release-certification work as backlog or an approved equivalent.
- Current deferred-work reconciliation markers no longer contradict completed Story 11.x status.
- Provider-backed pending-command status has an explicit release decision.
- Trusted release evidence path is proven or blocked with named rationale.
- Manual accessibility and stakeholder acceptance evidence are visible before v1 release readiness is claimed.

## 7. Approval Request

Recommended approval decision:

Approve this proposal as a moderate direct adjustment and create Epic 12: Release Certification and Evidence Alignment.

If approved, the next implementation step is to update:

- `_bmad-output/planning-artifacts/epics/index.md`
- `_bmad-output/planning-artifacts/epics/epic-list.md`
- `_bmad-output/planning-artifacts/epics/epic-12-release-certification-evidence-alignment.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`

No production code changes are proposed by this course correction.
