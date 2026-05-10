# Sprint Change Proposal: Route Deferred Work to Backlog

Date: 2026-05-10
Project: Hexalith.FrontComposer
Prepared for: Jerome

## 1. Issue Summary

The implementation ledger contains a large set of deferred work items accumulated from code reviews, retrospectives, and story execution across Epics 1-10. Several entries are named and bounded, but many are still discoverable only by reading `_bmad-output/implementation-artifacts/deferred-work.md`.

Trigger: Jerome requested that deferred work be added to the backlog using the Correct Course workflow.

Evidence:

- `_bmad-output/implementation-artifacts/deferred-work.md` contains deferred review sections from 2026-04-14 through 2026-05-10.
- `_bmad-output/process-notes/story-creation-lessons.md` states that every deferral row must link to a story number; if no story exists yet, create a backlog entry before deferring.
- `_bmad-output/implementation-artifacts/epic-10-retro-2026-05-10.md` notes that no Epic 11 planning artifact existed and recommends deciding whether the next work is Epic 11 or release readiness.

## 2. Impact Analysis

Epic impact:

- Epics 1-10 remain complete as implementation tracks.
- A new Epic 11 is required to hold release-readiness hardening and deferred follow-up work.
- No PRD goal is invalidated; this is backlog organization and release-readiness hygiene.

Story impact:

- Seven backlog story buckets are added under Epic 11.
- Existing story files are not reopened.
- Future implementation can split any Epic 11 story if the bucket is too large for a dev-agent story.

Artifact conflicts:

- PRD: no change required.
- Architecture: no immediate change required.
- UX: no immediate change required.
- Epics: add Epic 11 and update index/list.
- Sprint status: add Epic 11 and Story 11.1-11.7 as `backlog`.
- Deferred ledger: add routing status so future reviewers can find the backlog home.

Technical impact:

- Documentation/backlog only; no production code or tests changed.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The issue is not a strategic pivot or failed technical approach; it is traceability debt.
- Grouping the ledger into release-readiness stories preserves completed epic history while making follow-up work actionable.
- A single new epic avoids reopening many completed epics and gives Product/Developer agents a clear place to prioritize the hardening tail.

Effort estimate: Low for backlog routing; Medium/High for eventual implementation of the routed stories.

Risk level: Low for this change; Medium if the ledger is not reconciled before release readiness.

## 4. Detailed Change Proposals

### Epics

OLD:

Epic planning ended at Epic 10, with deferred work scattered across the implementation ledger and retrospectives.

NEW:

Add `epic-11-deferred-hardening-release-readiness.md` with seven backlog stories:

- 11.1 Deferred Work Ledger Reconciliation and Ownership
- 11.2 Diagnostic Registry and Documentation Governance Follow-ups
- 11.3 CLI, Migration, and IDE Edge-Case Hardening
- 11.4 Drift Detection and Source Generator Coverage Hardening
- 11.5 MCP Schema Negotiation and Agent Contract Hardening
- 11.6 Shell UX, Accessibility, and Sample Coverage Follow-ups
- 11.7 EventStore Reliability and CI Governance Follow-ups

Rationale: Converts unowned deferred work into story-addressable backlog.

### Sprint Status

OLD:

`sprint-status.yaml` tracks Epics 1-10 only.

NEW:

Add Epic 11 and its seven stories with status `backlog`.

Rationale: Makes deferred work visible to the file-system tracking system.

### Deferred Ledger

OLD:

The ledger starts directly with individual deferred sections.

NEW:

Add a routing section mapping unresolved deferred work to Epic 11 story buckets.

Rationale: Keeps the original audit trail while giving future agents a current backlog destination.

## 5. Implementation Handoff

Change scope: Moderate backlog reorganization.

Handoff recipients:

- Product Owner: prioritize Epic 11 story order and split oversized buckets.
- Developer agent: implement selected Epic 11 stories and mark ledger items resolved/superseded with evidence.
- Test Architect: confirm release-readiness gates for Stories 11.2, 11.4, 11.5, and 11.7.

Success criteria:

- Epic 11 exists in planning artifacts.
- Epic 11 and Story 11.1-11.7 are present in sprint status as backlog.
- Deferred-work ledger has a routing section that points to Epic 11.
- No completed implementation story is reopened by this routing change.

## Checklist Summary

- [x] 1.1 Triggering issue identified: deferred ledger lacks backlog home.
- [x] 1.2 Core problem defined: traceability/backlog hygiene, not product scope invalidation.
- [x] 1.3 Evidence gathered from deferred ledger, process notes, and Epic 10 retrospective.
- [x] 2.1-2.5 Epic impact assessed: add Epic 11; no existing epic invalidated.
- [x] 3.1 PRD conflict checked: no PRD change required.
- [x] 3.2 Architecture conflict checked: no architecture change required now.
- [x] 3.3 UI/UX conflict checked: no UX spec change required now.
- [x] 3.4 Other artifacts checked: sprint status and deferred ledger require updates.
- [x] 4.1 Direct adjustment selected.
- [N/A] 4.2 Rollback not applicable.
- [N/A] 4.3 MVP review not applicable.
- [x] 4.4 Recommended path selected.
- [x] 5.1-5.5 Proposal and handoff defined.
- [x] 6.1-6.5 Backlog routing applied per user request.
