# Sprint Change Proposal: Row-Level New-Item Producer Follow-Up

Date: 2026-07-01
Trigger: `_bmad-output/implementation-artifacts/epic-2-retro-2026-06-04.md`
Mode: Batch
Status: approved/applied

## 1. Issue Summary

Epic 2 completed the read-only projection experience, but Story 2.6 intentionally deferred AC1(b),
"new-item indicator marks fresh rows", because the live projection nudge carries only projection type
and tenant id. The retro correctly identified that the producer belongs to command lifecycle rather
than the read-path nudge seam.

The planning issue is that the accepted deferral was routed to stale "Story 5-5 / Epic 3-5" wording.
Current sprint status defines Story 5.5 as MCP schema-fingerprint negotiation, and Epic 3 is already
done. Stories 3.3, 3.5, and 3.6 confirmed command identity, EventStore status polling, and budgets,
but each still leaves row-level `FcNewItemIndicator` producer wiring out of scope because the status
payload lacks precise row identity.

Evidence:

- Story 2.6 records AC1(b) as PO-accepted deferred work, not silently complete.
- `fc-tbl-table-api-contract-2026-06-04.md` keeps the `FcNewItemIndicator` component confirmed but
  the row-identity producer open.
- `fc-cmd-pending-identity-correlation-contract-2026-06-04.md` and Stories 3.5/3.6 state that row-level
  producer wiring is out of scope until a later payload includes `EntityKey` or equivalent row identity.
- Source search shows production code defines `NewItemIndicatorStateService` and tests render it through
  a test-only lane host, but no production generated grid or shell consumer renders it from a real
  producer payload.

## 2. Impact Analysis

Epic impact:

- Epic 2 remains done. Its live refresh and reconnect/reconciliation behavior is pinned; only the
  accepted-deferred AC1(b) follow-up needs a current backlog home.
- Epic 3 remains done. It supplied the necessary command identity/status foundations but did not and
  could not implement row marking without row identity.
- Epic 5 remains done and should not be used for this human-UI fresh-row follow-up.
- A small post-MVP follow-up epic is needed because all current epics are done.

Story impact:

- Story 2.6 needs wording cleanup only, replacing stale Story 5-5 follow-up references.
- Story 2.8 and the FC-TBL contract need the same follow-up reference cleanup.
- New backlog stories should capture the missing payload contract and the eventual producer/consumer
  wiring.

Artifact impact:

- No authored PRD exists. `epics.md` remains the requirements inventory and PRD proxy.
- Architecture is not contradicted, but should gain a short note that row-level fresh indicators require
  FC-NIP row identity and must not be produced from the current projection nudge alone.
- UX/docs should clarify that `FcNewItemIndicator` is a confirmed component, while automatic row marking
  is still a tracked follow-up.
- Sprint status should add backlog entries and an open action item instead of changing completed story
  statuses.

Technical impact:

- No rollback is useful.
- Implementation likely touches Shell pending-command state, generated grid/shell consumer rendering,
  SourceTools emitter output if the generated projection view owns the consumer, and focused Shell plus
  SourceTools tests.
- If EventStore must emit row identity, that dependency must be confirmed first and not inferred from
  status endpoint fields that do not currently carry it.

## 3. Recommended Approach

Selected path: Direct Adjustment with backlog reorganization.

Scope classification: Moderate.

Rationale:

- The issue is real but bounded. It does not invalidate the shipped read-only MVP or command lifecycle.
- Completed stories should not be reopened for retroactive implementation. The work needs a current,
  explicit backlog target.
- A two-story follow-up avoids fabricating row identity in the UI and keeps the producer contract honest:
  first confirm the payload, then wire the producer and generated/shell consumer.

Effort estimate: Medium. The contract story is small; implementation may become medium if SourceTools
generated markup and snapshots change.

Risk level: Medium. The main risk is false confidence from test-only lane rendering or imprecise row
identity. The proposal requires end-to-end producer payload evidence before marking the feature done.

Timeline impact: No impact to completed Epics 1-8. Adds a post-MVP backlog epic.

## 4. Detailed Change Proposals

### 4.1 `epics.md` - Add AR11 and Epic 9

Section: Additional Requirements

OLD:

```md
- AR10 (**Out of scope for v1 / fast-follow**): Do **not** build `<AuditTimeline>` or `<ConsequencePreview>` rich components now - approved fallbacks stand; track as fast-follow.
```

NEW:

```md
- AR10 (**Out of scope for v1 / fast-follow**): Do **not** build `<AuditTimeline>` or `<ConsequencePreview>` rich components now - approved fallbacks stand; track as fast-follow.
- AR11 (**FC-NIP**): Confirm and implement the row-level new-item producer contract for `FcNewItemIndicator`. The producer must come from command outcome context with precise row identity (`EntityKey` or an approved equivalent), not from the current projection nudge seam that carries only projection type and tenant id.
```

Rationale: Gives the accepted-deferred feature a current requirement key instead of stale Story 5-5 wording.

Section: Epic List

OLD:

```md
### Epic 8: Aspire-grade Visual Refresh  *(post-MVP chrome parity)*
...
```

NEW:

```md
### Epic 9: Fresh-Row Producer and Row Identity  *(post-MVP follow-up)*
An **operator** can see newly materialized projection rows marked after command outcomes, using a
framework-controlled row identity payload and the confirmed `FcNewItemIndicator` component.
**FRs covered:** FR13, FR14
**ARs:** AR11 (FC-NIP)
**Standalone:** post-MVP enhancement; builds on Epics 2 and 3, and does not reopen the projection nudge seam.
```

Rationale: All existing epics are done; the follow-up needs a backlog location that does not distort
completed Epic 3 or MCP Story 5.5.

### 4.2 `epics.md` - Add Stories 9.1 and 9.2

NEW:

```md
## Epic 9: Fresh-Row Producer and Row Identity

### Story 9.1: Confirm the FC-NIP row-identity producer contract

As a FrontComposer maintainer,
I want a confirmed row-identity payload contract for fresh-row indicators,
So that FrontComposer can mark newly materialized rows without guessing from projection nudges.

**Acceptance Criteria:**

**Given** a command outcome that can create or materially change a projection row,
**When** the producer contract is reviewed,
**Then** the contract identifies the exact payload fields required to call
`INewItemIndicatorStateService.Add(...)`: `ViewKey` or lane key, row `EntityKey`, command `MessageId`,
projection type, and any status-slot metadata needed to avoid ambiguity.

**Given** the current EventStore status endpoint and projection nudge contracts,
**When** they do not provide precise row identity,
**Then** the story records a blocking follow-up with owner/date instead of fabricating identity through
diffing or broad row marking.

**Given** the contract is confirmed,
**Then** `fc-tbl`, `fc-cmd`, and DataGrid documentation name FC-NIP as the owner of automatic row-level
fresh-item marking.

### Story 9.2: Wire `FcNewItemIndicator` producer and generated-grid consumer

As an operator,
I want rows created or materially changed by a confirmed command outcome to be marked as new,
So that live command results are discoverable in projection grids.

**Acceptance Criteria:**

**Given** the FC-NIP payload contract from Story 9.1,
**When** a command reaches the relevant terminal outcome,
**Then** the command outcome path calls `INewItemIndicatorStateService.Add(...)` with the confirmed
view/lane, `EntityKey`, `MessageId`, and timestamp.

**Given** a generated projection grid for that view/lane,
**When** `INewItemIndicatorStateService.Snapshot(viewKey)` contains entries,
**Then** the grid or shell-level grid wrapper renders `FcNewItemIndicator` with localized copy,
`role="status"`, and `aria-live="polite"` for the matching lane only.

**Given** the row materializes, the filter changes, the TTL expires, or tenant/user scope changes,
**Then** the indicator is dismissed through the existing state-service semantics.

**Given** SourceTools output changes,
**Then** generated Verify snapshots and FC-TBL public-surface tests are updated intentionally.
```

Rationale: Separates the missing contract from implementation. Story 9.2 cannot be done unless Story 9.1
proves exact row identity.

### 4.3 Story 2.6 - Normalize Deferral Target

Section: AC1 disposition

OLD:

```md
- **AC1(b) - "new-item indicator marks fresh rows": ACCEPTED-DEFERRED to Epic 3/5 - Story 5-5.**
```

NEW:

```md
- **AC1(b) - "new-item indicator marks fresh rows": ACCEPTED-DEFERRED to Epic 9 - FC-NIP Stories 9.1/9.2.**
```

Rationale: The deferral remains accepted, but Story 5.5 is now MCP schema negotiation and Epic 3 is complete.

### 4.4 FC-TBL Contract - Update Open Item Follow-Up

Artifact: `_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md`

OLD:

```md
Current planning places the command-lifecycle follow-up in Epic 3, especially Story 3.3
(FC-CMD identity/correlation) and Story 3.5 (EventStore status endpoint binding).
```

NEW:

```md
Stories 3.3 and 3.5 supplied the FC-CMD identity and EventStore status foundations but did not provide
row identity. The active follow-up is Epic 9 / FC-NIP: Story 9.1 confirms the row-identity payload and
Story 9.2 wires the producer plus generated-grid consumer.
```

Rationale: Keeps the contract honest after Epic 3 completion.

### 4.5 FC-CMD Contract - Name the Follow-Up

Artifact: `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md`

OLD:

```md
Row-level `FcNewItemIndicator` producer wiring is out of scope until a later command outcome payload
contains the producer identity needed to mark rows precisely.
```

NEW:

```md
Row-level `FcNewItemIndicator` producer wiring is out of scope for FC-CMD v1. Epic 9 / FC-NIP owns
the later command outcome payload: Story 9.1 confirms row identity, and Story 9.2 wires the producer.
```

Rationale: Converts an unnamed later payload into a trackable backlog item.

### 4.6 Architecture Note

Artifact: `_bmad-output/project-docs/architecture.md`

Section: Runtime composition / command lifecycle

NEW:

```md
Fresh-row indicators are not produced from the projection nudge seam. The current nudge carries only
projection type and tenant id, while `FcNewItemIndicator` requires row identity. FC-NIP owns the
post-MVP command outcome payload and producer wiring.
```

Rationale: Prevents future implementers from adding broad or fabricated row marking to the read path.

### 4.7 DataGrid Documentation Note

Artifact: `docs/reference/components/datagrid.md`

Section: Parameters / slots, after `FcNewItemIndicator`

NEW:

```md
`FcNewItemIndicator` is a confirmed component and state primitive. Automatic row-level producer wiring is
tracked separately by FC-NIP because the current projection nudge does not include row identity.
```

Rationale: Published docs should not imply that automatic fresh-row marking exists just because the
component is public.

### 4.8 Sprint Status Backlog Entries

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Section: `development_status`

NEW:

```yaml
  # Epic 9: Fresh-Row Producer and Row Identity (post-MVP follow-up)
  # Added 2026-07-01 via Correct Course (sprint-change-proposal-2026-07-01.md).
  epic-9: backlog
  9-1-confirm-the-fc-nip-row-identity-producer-contract: backlog
  9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer: backlog
```

Section: `action_items`

NEW:

```yaml
  - epic: 2
    action: "Normalize accepted-deferred new-item references to the Epic 9 FC-NIP follow-up"
    owner: "Product Owner / Developer"
    status: done
```

Rationale: Keeps sprint-status aligned without reopening done stories.

## 5. Checklist Progress

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story identified | [x] | Story 2.6 AC1(b), surfaced by Epic 2 retro. |
| 1.2 Core problem defined | [x] | Planning/reference mismatch plus missing row-identity payload. |
| 1.3 Evidence gathered | [x] | Retro, Story 2.6, FC-TBL/FC-CMD contracts, Stories 3.5/3.6, source search. |
| 2.1 Current epic assessed | [x] | Epic 2 remains valid; accepted-deferred item needs current backlog target. |
| 2.2 Epic-level change identified | [x] | Add Epic 9 follow-up; do not reopen Epic 2/3. |
| 2.3 Remaining epics reviewed | [x] | Epic 3 and 5 are complete; no current epic owns FC-NIP. |
| 2.4 New epic need checked | [x] | New post-MVP Epic 9 recommended. |
| 2.5 Priority/order checked | [x] | Backlog after Epic 8; Story 9.2 depends on 9.1. |
| 3.1 PRD impact assessed | [N/A] | No authored PRD; `epics.md` is the requirements inventory. |
| 3.2 Architecture impact assessed | [x] | Add note forbidding producer from current projection nudge. |
| 3.3 UX impact assessed | [x] | DataGrid docs should clarify component vs automatic producer. |
| 3.4 Other artifacts assessed | [x] | Contracts, sprint-status, docs, and story notes need reference cleanup. |
| 4.1 Direct adjustment evaluated | [x] | Viable; effort Medium, risk Medium. |
| 4.2 Rollback evaluated | [N/A] | No completed work should be reverted. |
| 4.3 MVP review evaluated | [N/A] | MVP is unaffected; this is post-MVP. |
| 4.4 Path selected | [x] | Direct Adjustment with backlog reorganization. |
| 5.1 Issue summary created | [x] | See Section 1. |
| 5.2 Impact documented | [x] | See Section 2. |
| 5.3 Recommended path documented | [x] | See Section 3. |
| 5.4 MVP/action plan documented | [x] | No MVP reduction; add Epic 9. |
| 5.5 Handoff plan established | [x] | PO/Developer for backlog, Architect/EventStore owner for 9.1, Developer for 9.2. |
| 6.1 Checklist reviewed | [x] | All applicable items covered. |
| 6.2 Proposal accuracy checked | [x] | Based on loaded local artifacts and source search. |
| 6.3 User approval | [x] | Approved by Administrator on 2026-07-01. |
| 6.4 Sprint status update | [x] | Backlog entries and action item applied after approval. |
| 6.5 Handoff confirmed | [x] | Handoff approved by Administrator on 2026-07-01. |

## 6. Implementation Handoff

Change scope: Moderate.

Route to:

- Product Owner / Developer: approve Epic 9 backlog entries and normalize stale references.
- Architect / EventStore integration owner: execute Story 9.1 and decide whether row identity is supplied
  by command result/status payload, generated command metadata, or another explicit contract.
- Developer agent: execute Story 9.2 only after Story 9.1 confirms exact row identity.
- Technical Writer: update DataGrid docs and contract references after backlog approval.

Success criteria:

- No artifact points to Story 5.5 as the fresh-row producer follow-up.
- FC-NIP has a backlog home with dependency ordering.
- No implementation claims automatic row-level fresh marking until a production producer and generated/shell
  consumer are wired and tested end-to-end.
- Projection nudge remains scoped to projection type and tenant id unless a separate contract explicitly
  changes it.

## 7. Approval Record

Approved by Administrator on 2026-07-01.

Approved path: Continue. Planning/documentation updates above were applied.
