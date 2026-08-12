---
title: Sprint Change Proposal - Epic 9 Retrospective Rejection Remediation
date: 2026-08-12
mode: batch
status: approved-routed
approvedBy: Administrator
approvedDate: 2026-08-12
scope: moderate
trigger: _bmad-output/implementation-artifacts/epic-9-retro-2026-08-11.md
verdict: rejected
---

# Sprint Change Proposal: Epic 9 Retrospective Rejection Remediation

## 1. Issue Summary

The 2026-08-11 Epic 9 retrospective rejected the completed-delivery claim for **Fresh-Row Producer
and Row Identity**. Stories 9.1 and 9.2 remain valid historical records of the approved base decision
and the implementation attempt, but their `done` status does not prove the Epic 9 operator outcome.

Focused verification passed 108/108 tests. Those tests verify the producer, generated command,
indicator service, and generated grid in separate islands and preserve behavior that bypasses the
intended composition boundary. Current source and the retrospective establish five acceptance gaps:

1. Generated lifecycle callbacks call `PendingCommandState.ResolveTerminal(...)` directly, bypassing
   `IPendingCommandOutcomeResolver` and therefore indicator publication.
2. Generated target identity exists only in existing-row field-slot context. Standalone creates have
   no target row, while cross-row and status-move commands inherit the source row rather than an
   explicit target.
3. `INewItemIndicatorStateService` has no mutation notification. Generated grids therefore do not
   automatically re-render after add, expiry, dismissal, clear, or scope changes.
4. Tenant/user boundaries are checked only by `Add`; a scope change followed by `Snapshot` can expose
   an entry from the previous scope.
5. Duplicate observations are first-wins by `MessageId`, but distinct messages for the same
   `(ViewKey, EntityKey)` overwrite provenance and reset the expiry timer.

Live Aspire/browser verification did not run because shared build-output files were locked by other
processes. The unrelated Memories AppHost was correctly left untouched. This blocker does not weaken
the source-proven rejection and cannot be closed by substituting unit-test evidence.

The change is a failed implementation/composition approach against existing PRD FR-13 and FR-26. It
does not introduce a new product requirement or invalidate the read-only MVP.

## 2. Impact Analysis

### 2.1 Epic Impact

- Epic 9 cannot remain accepted as complete. It returns to `in-progress` when the remediation backlog
  is materialized.
- Stories 9.1 and 9.2 remain `done` as immutable historical delivery records. They are not reopened or
  rewritten to imply that their original review covered the newly discovered composition gaps.
- Six remediation stories, 9.3 through 9.8, map directly to retrospective actions E9-AI-1 through
  E9-AI-6.
- Epics 2 and 3 remain done. Their projection-refresh and command-lifecycle foundations remain valid;
  their completion does not satisfy Epic 9 composition acceptance.
- Epics 10 and 11 require no goal or sequencing change. Story 9.7 reuses the existing artifact
  validator/review-governance surface without reopening Epic 10.
- Story 11.24 remains independent. Epic 9 remediation does not depend on changing the EventStore
  runtime identity or editing a submodule.

### 2.2 Story Impact And Dependencies

| Story | Retrospective action | Purpose | Dependency |
| --- | --- | --- | --- |
| 9.3 | E9-AI-2 | Approve explicit command-to-target-row identity and material-change semantics. | First decision gate. |
| 9.4 | E9-AI-1 | Route every terminal path through the resolver and buffer early callbacks. | 9.3. |
| 9.5 | E9-AI-3 | Make indicator state observable and scope-safe. | 9.3. |
| 9.6 | E9-AI-4 | Enforce atomic first-wins per row. | 9.3. |
| 9.7 | E9-AI-5 | Add mechanical story-ID/commit-scope evidence. | May proceed alongside 9.3-9.6. |
| 9.8 | E9-AI-6 | Prove automated composition and live browser acceptance. | 9.3-9.7. |

### 2.3 Artifact Conflicts

- **PRD:** FR-13, FR-26, Epic 9 program status, D-4, and the open-question disposition currently claim
  complete delivery. Those claims conflict with the rejected retrospective.
- **Epics:** Epic 9 and Story 2.6 currently treat Story 9.2 as completed acceptance evidence. The epic
  needs active remediation children and corrected traceability.
- **Architecture:** existing layering remains valid, but it does not state the single terminal-owner,
  observable-state, proactive scope, explicit target, or atomic per-row invariants exposed by the
  rejection.
- **UX:** the experience supplement says FC-NIP wiring is complete. The observable behavior must require
  automatic post-render appearance and dismissal without manual render forcing.
- **FC-NIP contract:** the 2026-07-04 contract remains the approved base decision, but its ambient
  generated-row context is insufficient for standalone create and cross-row/status-move commands, and
  its producer-only first-wins statement did not establish atomic per-row behavior.
- **Sprint tracking:** `epic-9: done` conflicts with six open Epic 9 remediation actions.
- **Testing/process:** focused tests do not cross producer, generated-command, state-invalidation, and
  generated-grid boundaries. Commit history also lacks reliable story-scope isolation.

### 2.4 Technical Impact

Expected implementation surfaces are bounded to FrontComposer-owned Shell, SourceTools, tests, and
governance tooling:

- generated command lifecycle adapters and pending registration ordering;
- `IPendingCommandOutcomeResolver` and pending-command terminal ownership;
- explicit target identity/change-kind metadata;
- `INewItemIndicatorStateService` mutation notification, scope enforcement, and atomic add behavior;
- generated grid subscription/disposal and automatic render invalidation;
- composed bUnit/SourceTools tests, Verify snapshots where emitted output changes, and live Aspire/browser
  evidence;
- story artifact/commit-scope validation.

No dependency upgrade, schema-fingerprint change, package-boundary change, deployment/IaC change,
submodule edit, or EventStore contract change is authorized by this proposal.

## 3. Recommended Approach

### Selected Path

**Direct Adjustment with backlog reorganization.** Reopen Epic 9 at the epic level, preserve completed
story history, add Stories 9.3-9.8, and restore the completion claim only after composed and live
acceptance evidence passes.

### Alternatives Considered

| Option | Viability | Effort | Risk | Disposition |
| --- | --- | --- | --- | --- |
| Direct adjustment | Viable | Medium | Medium-High | Selected. It preserves valid partial infrastructure and fixes the actual seams. |
| Roll back Story 9.2 | Not useful | Medium-High | High | Rejected. It discards valid partial work without solving target identity or acceptance proof. |
| Reduce/defer the product goal | Possible only through a PRD scope change | Low implementation, high product cost | High | Rejected. The existing FR-13/FR-26 goal remains achievable and valuable. |

### Timeline Impact

The change adds one bounded remediation cycle plus live-environment verification. Story 9.3 is the
entry gate; Stories 9.4-9.6 follow it; Story 9.7 may proceed in parallel; Story 9.8 closes the epic.
Other work may continue, but Epic 9, FR-13, and FR-26 cannot return to complete before Story 9.8 passes.

## 4. Detailed Change Proposals

### 4.1 PRD Status And Requirement Truth

Artifact: `_bmad-output/planning-artifacts/prd.md`

Sections: §5.0 requirement status map, FR-13, FR-26, §8.2, D-4, and §12.1.

**OLD:**

```md
| FR-13 | Complete / release verification | Product + Architecture approved the FC-NIP payload source on 2026-07-05; Stories 9.1 and 9.2 are done and their contract/runtime evidence remains the release baseline. |
| FR-26 | Complete / release verification | Epic 9 is done; the approved FC-NIP payload and producer/consumer evidence remain the baseline. |
```

**NEW:**

```md
| FR-13 | Active remediation / release gate | The approved FC-NIP base source remains valid, but the rejected Epic 9 retrospective found terminal-path, target-identity, invalidation, scope, and first-wins composition gaps. Stories 9.3-9.8 must pass before completion is restored. |
| FR-26 | Active remediation / release gate | Epic 9 is in progress. Stories 9.1-9.2 remain historical delivery records; Stories 9.3-9.8 own accepted remediation and composed/live regression evidence. |
```

**OLD:**

```md
- Story 9.1 recorded the approved row identity payload source ...; completed Story 9.2 proves the producer/consumer wiring.
```

**NEW:**

```md
- Story 9.1 recorded the approved base payload source. The 2026-08-11 retrospective rejected Story 9.2 evidence as proof of composed behavior; Stories 9.3-9.8 must prove explicit target identity, one terminal producer boundary, observable scope-safe state, atomic per-row first-wins behavior, and composed/live acceptance.
```

**OLD:**

```md
FrontComposer must retain the completed row-level fresh-item producer/consumer wiring only through the approved FC-NIP payload source.
...
- Completed Story 9.2 evidence proves runtime metadata and producer/consumer behavior and remains a release regression gate.
```

**NEW:**

```md
FrontComposer must complete row-level fresh-item producer/consumer composition only through the approved and successor-amended FC-NIP payload contract.
...
- Story 9.2 is historical implementation evidence, not accepted composition evidence. Stories 9.3-9.8 are the release regression gate.
```

**OLD:**

```md
- **Epic 9:** done; FC-NIP decision and producer/consumer wiring are completed evidence.
```

**NEW:**

```md
- **Epic 9:** in progress after rejected retrospective acceptance; Stories 9.1-9.2 remain historical records and Stories 9.3-9.8 own remediation and final evidence.
```

**OLD:**

```md
| D-4 | FC-NIP row identity payload source | ... | Stories 9.1 and 9.2 are done; no remaining decision or implementation gate. |
```

**NEW:**

```md
| D-4 | FC-NIP row identity payload source | The 2026-07-05 pending-command metadata decision remains the base source for row-context commands. The 2026-08-11 retrospective proved it incomplete for standalone create, cross-row, delete/no-op, and status-move targets and for composed delivery. Story 9.3 owns the successor target-identity decision. | Stories 9.3-9.8 block FR-13/FR-26 completion and Epic 9 closure. |
```

Replace the §12.1 statement that FC-NIP is complete with an active-remediation statement linked to the
retrospective and Stories 9.3-9.8.

Rationale: restore truthful product/readiness state without changing the operator goal or MVP scope.

### 4.2 Epic 9 Status, Traceability, And Remediation Stories

Artifact: `_bmad-output/planning-artifacts/epics.md`

Sections: FR coverage map, Epic List, Story 2.6 historical dependency, detailed Epic 9.

**OLD:**

```md
| FR-13 | Epic 9: Stories 9.1 and 9.2; Story 2.6 preserves the ownership boundary |
| FR-26 | Epic 9: Story 9.2; Story 2.6 preserves the ownership boundary |
```

**NEW:**

```md
| FR-13 | Epic 9: historical Stories 9.1-9.2 plus remediation Stories 9.3-9.8; Story 2.6 preserves the ownership boundary |
| FR-26 | Epic 9: remediation Stories 9.3-9.8; Story 9.2 remains historical delivery evidence only |
```

Replace Epic 9 completion wording with:

```md
**Current state:** in progress after the 2026-08-11 retrospective rejected composed acceptance.
Stories 9.1 and 9.2 remain done historical records. Stories 9.3-9.8 own remediation; the epic closes
only after Story 9.8 records composed and live acceptance evidence.
```

Replace Story 2.6's claim that completed Story 9.2 supplied accepted producer/consumer evidence with a
historical note that the implementation was delivered but rejected as composition proof, and that
Stories 9.3-9.8 own the active dependency.

#### Story 9.3: Define Explicit Command Target Identity

**OLD:** No Story 9.3 exists.

**NEW:**

```md
### Story 9.3: Define explicit command target identity

As a FrontComposer maintainer,
I want every material command outcome to carry immutable target projection-row metadata,
So that fresh-row behavior works for create, same-row, cross-row, and status-move commands without ambient guesses.

**Acceptance Criteria:**

**Given** a command can create or materially change projection rows,
**When** Architect + Product approve the successor FC-NIP contract,
**Then** it names the authoritative source for projection type, view/lane, target `EntityKey`, material-change kind, prior status, expected status, and capture time.

**Given** a standalone create command has no existing row context,
**When** pending metadata is registered,
**Then** its target identity comes from an explicit framework-owned command-to-projection contract, not an existing-row cascade, EventStore `AggregateId`, projection nudge, visible-row diff, or untyped result payload.

**Given** a cross-row, status-move, delete, idempotent, rejected, or no-op outcome,
**When** target semantics are evaluated,
**Then** the intended target and indicator/no-indicator disposition are explicit and the source row is never silently reused as the target.

**Given** the decision is not approved,
**Then** Stories 9.4-9.6 remain blocked and no best-effort identity is implemented.
```

Rationale: closes E9-AI-2 and F9-02 before implementation choices harden another incomplete ambient seam.

#### Story 9.4: Converge Terminal Outcomes On One Producer Boundary

**OLD:** No Story 9.4 exists.

**NEW:**

```md
### Story 9.4: Converge terminal outcomes on one producer boundary

As an operator,
I want every confirmed command path to use the same terminal-outcome boundary,
So that callback and polling confirmations produce identical pending and fresh-row behavior.

**Acceptance Criteria:**

**Given** generated lifecycle callbacks, EventStore polling, reconnect reconciliation, or any other terminal adapter,
**When** a terminal observation arrives,
**Then** it routes through `IPendingCommandOutcomeResolver`; generated adapters do not call `PendingCommandState.ResolveTerminal(...)` directly.

**Given** a lifecycle callback arrives before accepted pending registration is durable,
**When** registration completes,
**Then** the bounded callback is buffered and replayed exactly once through the resolver, preserving cancellation, disposal, and `MessageId` matching.

**Given** the stub callback path and EventStore status path confirm the same contract,
**When** composed tests run,
**Then** each produces one eligible lane entry and duplicate terminal observations do not create another publication.

**Given** SourceTools emits terminal adapters,
**When** Governance scans generated output,
**Then** direct terminal mutation outside the approved owner boundary fails the test.
```

Rationale: closes E9-AI-1 and F9-01.

#### Story 9.5: Make Indicator State Observable And Scope-Safe

**OLD:** No Story 9.5 exists.

**NEW:**

```md
### Story 9.5: Make indicator state observable and scope-safe

As an operator,
I want fresh-row indicators to appear and disappear immediately in my active scope,
So that the UI never waits for an unrelated render or exposes a previous tenant/user entry.

**Acceptance Criteria:**

**Given** add, materialization, filter/re-query dismissal, TTL expiry, explicit clear, or scope transition mutates indicator state,
**When** the mutation completes,
**Then** generated grids receive one change notification, marshal rendering through `InvokeAsync(StateHasChanged)`, and unsubscribe/dispose safely.

**Given** a generated grid rendered before the mutation,
**When** each mutation scenario occurs,
**Then** bUnit proves automatic DOM appearance/removal without calling `cut.Render()` manually.

**Given** tenant or user scope changes before another producer mutation,
**When** state is read or rendered,
**Then** previous-scope entries are cleared or rejected before `Snapshot` returns and cannot render.

**Given** concurrent timer, clear, and disposal operations,
**When** tests run,
**Then** notification delivery remains race-safe, bounded, and free of disposed-component callbacks.
```

Rationale: closes E9-AI-3, F9-03, and F9-04.

#### Story 9.6: Enforce Atomic Per-Row First-Wins

**OLD:** No Story 9.6 exists.

**NEW:**

```md
### Story 9.6: Enforce atomic per-row first-wins

As an operator,
I want one stable fresh-row indication for each row,
So that later confirmations cannot replace its provenance or extend its lifetime unexpectedly.

**Acceptance Criteria:**

**Given** duplicate terminal observations for one `MessageId`,
**When** they reach the producer boundary,
**Then** only the first eligible observation can publish.

**Given** distinct confirmed message IDs target the same `(ViewKey, EntityKey)` while an entry is active,
**When** publication races or occurs sequentially,
**Then** the first entry, `MessageId`, `CreatedAt`, and original expiry win atomically; later attempts do not replace data or reset TTL.

**Given** the first entry expires or is dismissed,
**When** a later material command targets the row,
**Then** a new entry may be accepted under a newly defined active-entry window.

**Given** concurrent publication tests,
**Then** one active entry and one original timer/provenance pair are observed.
```

Rationale: closes E9-AI-4 and F9-05.

#### Story 9.7: Add Story-ID And Commit-Scope Evidence

**OLD:** No Story 9.7 exists.

**NEW:**

```md
### Story 9.7: Add story-ID and commit-scope evidence

As a QA automation maintainer,
I want review completion to prove which commits and files belong to a story,
So that future epic evidence is auditable, bisectable, and isolated from unrelated work.

**Acceptance Criteria:**

**Given** a story baseline and candidate head,
**When** artifact validation runs before review completion,
**Then** it reports every non-merge commit, story-ID match, changed path, File List disposition, and unrelated/interleaved commit.

**Given** implementation, review, or done-transition commits do not map to the story,
**When** the report is evaluated,
**Then** review completion fails until scope is corrected or an explicit shared/process disposition is recorded; published history is not rewritten.

**Given** pre-existing unrelated workspace changes,
**When** validation runs,
**Then** they remain separately reported and are not forced into story ownership.

**Given** the validator changes,
**Then** fixture coverage includes subject-less, wrong-story, shared-process, merge, and interleaved ranges.
```

Rationale: closes E9-AI-5 and F9-06 without rewriting published history.

#### Story 9.8: Prove Composed And Live Epic 9 Acceptance

**OLD:** No Story 9.8 exists.

**NEW:**

```md
### Story 9.8: Prove composed and live Epic 9 acceptance

As an operator and release owner,
I want generated create/update paths proven through a running FrontComposer system,
So that Epic 9 closes on observable behavior rather than isolated implementation tests.

**Acceptance Criteria:**

**Given** Stories 9.3-9.7 are done,
**When** automated composition tests run,
**Then** standalone create, row-context update, cross-row/status move, callback confirmation, and status polling traverse generated command, pending registration, resolver, indicator service, and generated grid boundaries with the intended indicator/no-indicator result.

**Given** a grid is already rendered,
**When** add, materialization, filter/re-query, TTL, clear, or tenant/user transition occurs,
**Then** the DOM updates automatically, remains lane/scoped, preserves first-wins provenance, and retains accessible localized `role="status"`/`aria-live="polite"` behavior.

**Given** the FrontComposer AppHost can build without shared-output locks,
**When** the browser acceptance run executes,
**Then** a durable command log and browser artifact record the repaired scenarios against a running system without stopping unrelated AppHosts.

**Given** live verification is environment-blocked,
**Then** the exact command and blocker are recorded and this story, Epic 9, FR-13, and FR-26 remain open; passing unit lanes are not substituted.
```

Rationale: closes E9-AI-6 and the composition-proof gap.

### 4.3 Architecture Invariants

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Section: add `## FC-NIP Composition Invariants` after `## Key Invariants`.

**OLD:** No explicit FC-NIP composition section exists.

**NEW:**

```md
## FC-NIP Composition Invariants

- `IPendingCommandOutcomeResolver` is the single owner of terminal pending-command application and eligible fresh-row publication. Generated callbacks and infrastructure adapters emit observations; they do not mutate terminal pending state directly.
- Command target metadata is immutable and explicit. It identifies projection, view/lane, target row, material-change kind, and status movement independently of the UI surface that launched the command.
- Projection nudges, visible-row diffs, EventStore `AggregateId`, and untyped result payloads are not universal row identity.
- Indicator state is observable. Every effective add/dismiss/expiry/clear/scope mutation invalidates subscribed generated consumers; subscriptions are scoped and disposed.
- Tenant/user scope is enforced before state is read or rendered, not only on the next producer add.
- Active indicator identity is `(ViewKey, EntityKey)` and uses atomic first-wins semantics across duplicate and distinct message IDs; later attempts do not replace provenance or extend expiry.
```

Update the detailed project architecture runtime-composition note to reference these invariants and the
successor Story 9.3 contract.

Rationale: adds the missing architecture spine without changing layers, technology, dependency direction,
or EventStore ownership.

### 4.4 UX Behavior And Acceptance

Artifacts:

- `_bmad-output/planning-artifacts/ux-design.md`
- `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md`

Section: UX-DR5 and Brownfield Reconciliation.

Add to UX-DR5:

```md
Fresh-row indicators must appear and disappear from an already-rendered generated grid when their state changes; they must not depend on an unrelated projection/Fluxor render. Indicator state is lane-, tenant-, and user-scoped, uses useful non-noisy live announcements, and clears before a previous scope can render.
```

**OLD:**

```md
- FC-NIP row identity and producer/consumer wiring are complete under Stories 9.1-9.2; broad row marking or diff-based inference remains forbidden.
```

**NEW:**

```md
- Stories 9.1-9.2 retain the FC-NIP base decision and historical implementation, but the 2026-08-11 retrospective rejected composed acceptance. Stories 9.3-9.8 own target identity, terminal-path convergence, observable scope-safe state, atomic per-row first-wins behavior, and composed/live evidence. Broad row marking and diff-based inference remain forbidden.
```

Flow 2 remains the intended user journey; Story 9.8 becomes its acceptance proof. No layout, styling,
component-library, or accessibility-floor change is proposed.

Rationale: corrects completion truth and makes automatic render behavior an explicit UX outcome.

### 4.5 FC-NIP Contract And DataGrid Documentation

Artifacts:

- `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`
- `docs/reference/components/datagrid.md`

**OLD contract status:**

```md
Status: approved payload source for Story 9.2 implementation
```

**NEW contract status and amendment:**

```md
Status: approved base decision; delivery completion rejected 2026-08-11
Successor gate: Story 9.3 explicit command target identity contract
```

Add an amendment preserving the 2026-07-05 decision while stating:

- ambient generated-row context covers only commands launched from an existing row and cannot define
  standalone create or cross-row targets;
- target projection/lane/entity/change-kind/status metadata must be explicit and captured before async
  dispatch or virtualized-row reuse;
- all terminal paths must converge on the resolver;
- observable scope-safe state and atomic per-row first-wins behavior are contract requirements;
- Stories 9.3-9.8 supersede Story 9.2 as delivery-completion evidence.

**OLD DataGrid note:**

```md
Automatic row-level producer wiring is tracked separately by Epic 9 / FC-NIP because the current projection nudge does not include row identity.
```

**NEW DataGrid note:**

```md
Automatic row-level producer wiring is under active Epic 9 remediation. Projection nudges remain insufficient row identity. Stories 9.3-9.8 require explicit target metadata, one terminal producer boundary, automatic generated-grid invalidation, scope-safe dismissal, atomic per-row first-wins behavior, and composed/live acceptance before the feature is documented as complete.
```

Rationale: preserve historical decisions while preventing adopter documentation from implying accepted
end-to-end behavior.

### 4.6 Sprint Status And Action Mapping

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Section: `development_status`.

**OLD:**

```yaml
  epic-9: done
  9-1-fc-nip-row-identity-producer-decision-record: done
  9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer: done
  epic-9-retrospective: done
```

**NEW:**

```yaml
  epic-9: in-progress
  9-1-fc-nip-row-identity-producer-decision-record: done
  9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer: done
  9-3-define-explicit-command-target-identity: backlog
  9-4-converge-terminal-outcomes-on-one-producer-boundary: backlog
  9-5-make-indicator-state-observable-and-scope-safe: backlog
  9-6-enforce-atomic-per-row-first-wins: backlog
  9-7-add-story-id-and-commit-scope-evidence: backlog
  9-8-prove-composed-and-live-epic-9-acceptance: backlog
  epic-9-retrospective: done
```

Keep E9-AI-1 through E9-AI-6 open and add their corresponding `implementation_story` values. Close an
action only when its story success criteria and evidence are complete. Story 9.8 is the only action
authorized to return Epic 9 to `done` after all prerequisites pass.

Rationale: reconciles the rejected verdict with sprint truth without reopening completed story records.

### 4.7 Testing And Process Evidence

No production test is removed or weakened. Implementation stories must add:

- a rendered row-context command test that submits through the real generated stub callback path;
- a standalone generated create-command composition test with explicit target identity;
- an EventStore-status polling composition test over the same resolver boundary;
- post-initial-render bUnit tests for add, materialization, filter/re-query, TTL, clear, and scope transition,
  with no manual `cut.Render()` call;
- duplicate-message and distinct-message/same-row sequential and concurrency tests;
- SourceTools output guards prohibiting direct terminal mutation in generated adapters;
- intentional Verify snapshot updates where emitted output changes;
- a mechanical commit-scope report with hostile fixture coverage;
- a live FrontComposer AppHost/browser acceptance record after shared build locks are cleared.

The existing 108/108 focused pass remains historical evidence. It is not the successor acceptance gate.

### 4.8 Explicitly Unchanged Artifacts And Boundaries

- Story files 9.1 and 9.2 remain unchanged historical records.
- The Epic 9 retrospective remains unchanged evidence.
- The projection nudge contract remains unchanged and is not a row-identity producer.
- EventStore status remains lifecycle/status by `MessageId`; no EventStore or other submodule edit is
  authorized.
- Existing accepted UX decisions from the Story 9.2 review—already-visible/idempotent indicators may
  linger up to the TTL and broad re-query dismissal—remain unchanged unless new Product/UX evidence
  separately reopens them.
- F9-07 per-cell cascade allocation remains deferred performance debt and is not acceptance-blocking.

## 5. Implementation Handoff

### Scope Classification

**Moderate.** Backlog reorganization and coordinated Product/Architecture, Developer, and QA work are
required, but there is no fundamental product replan or platform replacement.

### Recipients And Responsibilities

| Recipient | Responsibility |
| --- | --- |
| Product Owner + Architect | Approve Story 9.3 target/change semantics and PRD/architecture truth-state corrections. |
| Shell + SourceTools maintainers | Implement Stories 9.4-9.6 without dependency inversion or generated-file hand edits. |
| QA automation maintainer | Implement Story 9.7 and keep false positives as validator fixes rather than bypasses. |
| QA Engineer | Implement Story 9.8 automated composition and live browser acceptance evidence. |
| Product Owner / Developer | Apply approved planning, contract, documentation, and sprint-status edits; keep action/story mapping synchronized. |
| Release Owner | Treat FR-13/FR-26 and Epic 9 as open until Story 9.8 evidence passes. |

### Success Criteria

1. Every generated/runtime terminal adapter routes through `IPendingCommandOutcomeResolver`; no direct
   generated terminal mutation remains.
2. Standalone create, same-row, cross-row, status-move, delete/no-op, rejected, and idempotent outcomes
   have explicit target and materiality dispositions.
3. Indicator mutations automatically update already-rendered generated grids and never expose a prior
   tenant/user scope.
4. Active `(ViewKey, EntityKey)` entries are atomic first-wins across duplicate and distinct messages,
   preserving original provenance and expiry.
5. Automated composition tests cross generated command, pending registration, resolver, state service,
   and generated grid boundaries.
6. A durable live AppHost/browser record proves create/update production, lane-scoped rendering,
   dismissal, scope clearing, first-wins, and the fixed callback path.
7. Commit-scope evidence maps implementation, review, and completion commits to the correct story.
8. PRD, epics, UX, architecture, FC-NIP contract, DataGrid docs, sprint status, and action items agree on
   the same truth state.

## 6. Change Navigation Checklist

| Item | Status | Finding |
| --- | --- | --- |
| 1.1-1.3 Trigger/context | [x] | Story 9.2 retrospective rejection; F9-01-F9-05 and 108/108 isolated-test evidence. |
| 2.1-2.5 Epic impact | [x] | Reopen Epic 9 only; preserve 9.1/9.2; add 9.3-9.8; no new epic. |
| 3.1 PRD | [!] | Correct false completion and add release gate. |
| 3.2 Architecture | [!] | Add target, terminal-owner, observable-state, scope, and first-wins invariants. |
| 3.3 UX | [!] | Require automatic scoped appearance/removal; correct completion wording. |
| 3.4 Other artifacts | [!] | Epics, sprint status, contract, docs, tests, and process need updates. |
| 4.1 Direct adjustment | [x] | Viable; Medium effort, Medium-High risk. |
| 4.2 Rollback | [N/A] | Discards valid partial work and does not solve the gaps. |
| 4.3 MVP review | [N/A] | MVP remains intact; v1-readiness truth changes. |
| 4.4 Recommended path | [x] | Direct Adjustment with backlog reorganization. |
| 5.1-5.5 Proposal components | [x] | Issue, impact, path, action plan, and handoff are defined. |
| 6.1 Checklist review | [x] | All applicable findings are represented. |
| 6.2 Proposal accuracy | [x] | Administrator reviewed the complete batch proposal and chose Continue on 2026-08-12. |
| 6.3 User approval | [x] | Administrator explicitly approved the proposal on 2026-08-12. |
| 6.4 Sprint status update | [x] | Epic 9 is in progress; Stories 9.3-9.8 are backlog; E9-AI-1 through E9-AI-6 map to their implementation stories. |
| 6.5 Handoff confirmation | [x] | Moderate change routed to Product Owner / Developer with Architect and QA responsibilities defined in Section 5. |

## 7. Approval Record

Status: **Approved and routed 2026-08-12.**

Review gate: **Completed 2026-08-12; Administrator chose Continue without requested edits.**

Approval gate: **Completed 2026-08-12; Administrator explicitly approved implementation.**

Applied course-correction artifacts:

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/project-docs/architecture.md`
- `_bmad-output/planning-artifacts/ux-design.md`
- `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md`
- `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`
- `docs/reference/components/datagrid.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Handoff: **Moderate scope routed to Product Owner / Developer.** Product + Architecture own Story 9.3;
Shell + SourceTools own Stories 9.4-9.6; QA automation owns Story 9.7; QA owns Story 9.8 composed/live
acceptance. Epic 9, FR-13, and FR-26 remain open until Story 9.8 passes.

No production source, test, staging, commit, push, or submodule change was performed by this workflow.
The unrelated `references/Hexalith.EventStore` gitlink change present in the working tree was left
untouched.
