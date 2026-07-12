# Implementation Prompt: FC-NIP Continuation and Boundary Verification

Use this prompt from the root of `Hexalith.FrontComposer`.

## Role

Act as a senior FrontComposer and EventStore maintainer. Work from current repository evidence rather
than the historical blocked state in early Story 9.2 notes.

Before changing anything:

1. Read `AGENTS.md` and `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`.
2. Read `_bmad-output/project-context.md`.
3. Read the complete files:
   - `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`
   - `_bmad-output/implementation-artifacts/9-1-confirm-the-fc-nip-row-identity-producer-contract.md`
   - `_bmad-output/implementation-artifacts/9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer.md`
   - `_bmad-output/implementation-artifacts/sprint-status.yaml`
4. Inspect the current implementation and tests before proposing changes.

Do not initialize nested submodules. Do not modify a `references/Hexalith.*` submodule or move a
submodule pointer without explicit approval.

## Current Decision

Treat these statements as the approved starting point:

- Epic 9 Stories 9.1 and 9.2 are implemented and marked `done`.
- FC-NIP row identity comes from FrontComposer-owned pending-command metadata populated by generated
  grid/command runtime context.
- EventStore command status remains a lifecycle/status and trusted-observation-time source keyed by
  `MessageId`.
- EventStore `AggregateId`, projection nudges, opaque detail metadata, and untyped `ResultPayload`
  are not universal FrontComposer row identity.
- No EventStore code change is required to continue with the current generated-grid command flow.
- `epic-9: in-progress` in sprint status is tracking drift because both Epic 9 stories are done.

## Existing Submodule Story Routing

Do not create a duplicate EventStore story before reconciling these existing backlog entries in
`references/Hexalith.EventStore`:

1. **Story 4.1: Event Identity And Duplicate Result Fidelity**
   - Sprint key: `4-1-event-identity-and-duplicate-result-fidelity`
   - Current status: `backlog`
   - Story file: not created
   - Relevant scope: preserve event count, typed/domain result payload, backpressure fields,
     acceptance/error state, and correlation information when returning a duplicate command result.
   - FC-NIP relationship: adjacent duplicate-result correctness only; it does not define generated
     grid row identity.

2. **Story 4.2: Resume And Idempotency Integrity**
   - Sprint key: `4-2-resume-and-idempotency-integrity`
   - Current status: `backlog`
   - Story file: not created
   - Relevant scope: match resume state by `MessageId`, `CausationId`, and `CommandType`; validate
     tenant access before idempotency reads; preserve retryability; key command status/archive by
     `{tenant}:{messageId}` while treating correlation id as an indexed field.
   - FC-NIP relationship: this is the existing home for EventStore command-status identity
     hardening. It strengthens the `MessageId` lifecycle boundary used by FrontComposer but does not
     replace `PendingCommandRowIdentity` or supply FrontComposer lane/entity metadata.

The existing EventStore **Story 2.6: Generated Command-Status Location Policy** is already `done` and
must not be recreated. It owns the absolute, gateway-authoritative, fail-closed status `Location`
policy, not FC-NIP row identity.

No matching unfinished FC-NIP, `FcNewItemIndicator`, `PendingCommandRowIdentity`, or generated-grid
row-identity story was found in the root-declared Tenants, Memories, or Parties submodules.

If EventStore work is authorized, follow the existing Epic 4 ordering: create/implement Story 4.1
when duplicate-result fidelity is in scope, then create/implement Story 4.2 for status/resume identity
hardening. Do not broaden either story with FrontComposer presentation concepts.

## Objective

Verify that the completed FC-NIP path remains correct on the current branch, then make only the
smallest evidence-backed FrontComposer changes needed to close residual behavior gaps. Do not reopen
the EventStore boundary without a concrete unsupported use case.

The verified path must remain:

```text
generated grid row context
  -> PendingCommandRowIdentity
  -> generated command form
  -> PendingCommandRegistration / PendingCommandEntry
  -> terminal PendingCommandOutcomeObservation resolved by MessageId
  -> INewItemIndicatorStateService.Add(...)
  -> generated grid Snapshot(viewKey)
  -> FcNewItemIndicator
```

## Required Audit

Verify these production files and their focused tests:

- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRowIdentity.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`

Prove all of the following:

1. Row metadata is supplied only by framework-controlled runtime context.
2. Terminal outcomes resolve by `MessageId` first.
3. Missing or ambiguous row identity causes no indicator mutation.
4. Duplicate terminal observations do not re-add an indicator or reset its expiration.
5. `DateTimeOffset.MinValue` is not accepted as a trusted observation timestamp.
6. Generated grids render indicators for an exact view key only.
7. Materialization, re-query/filter changes, expiration, and tenant/user transitions dismiss entries.
8. Generated code, snapshots, public API baselines, and documentation agree with the implementation.

## FrontComposer Hardening Scope

Address a hardening item only when its current behavior is reproduced by a focused failing test.

### 1. Already-materialized row behavior

Reproduce the review finding where a confirmed or idempotently confirmed outcome arrives after the
target row is already visible. If reproduced, make dismissal level-triggered so an indicator is not
rendered for a row already present in the current materialized item set. Preserve stable row-key
semantics and do not infer identity by diffing unrelated rows.

Acceptance evidence:

- a focused generated-grid test starts with the target row materialized;
- the terminal outcome does not leave a visible fresh-row indicator;
- a genuinely non-materialized target still produces the indicator.

### 2. Indicator change notification

Reproduce the finding where `Add(...)` does not cause the owning grid to render until another state
transition occurs. If reproduced, use the existing scoped/component state pattern to notify the grid
without introducing a global singleton or violating Fluxor single-writer discipline.

If the clean solution changes a public interface, update the applicable shipped public API baseline
and package-boundary tests intentionally. Do not add a public event merely to make a test convenient.

### 3. Generated cascade allocation

Measure or otherwise demonstrate the per-cell `CascadingValue<PendingCommandRowIdentity?>`
allocation concern before refactoring it. A safe change must preserve correct row identity under
FluentDataGrid virtualization and component reuse. Do not set `IsFixed="true"` on a cascade whose
value can change when a virtualized cell instance is recycled.

If no safe per-row seam exists, leave production code unchanged and record the finding as accepted
performance debt with concrete evidence. Do not perform a structural emitter rewrite without the
corresponding Verify snapshot lane.

### 4. Dependency-governance guard

Review the Story 9.2 change that replaced an exact `Hexalith.EventStore.Aspire` version assertion
with a non-empty presence assertion. If version alignment is still an invariant, replace the
hard-coded patch pin with a comparison against the repository's authoritative centralized version
source. Keep the assertion non-vacuous. Treat this as a separate governance change if it is not
caused by FC-NIP behavior.

## EventStore Decision Gate

Do not change EventStore for the current grid-originated command path.

An EventStore change may be proposed only if a concrete required command flow cannot originate from
a generated grid or otherwise supply `PendingCommandRowIdentity` through FrontComposer-controlled
runtime context. If that case exists:

1. Document the unsupported flow and why FrontComposer cannot own the mapping.
2. Produce an ADR or contract proposal before code.
3. Define a bounded, typed, versioned command-outcome target contract; do not reuse untyped
   `ResultPayload` and do not reinterpret `AggregateId` globally.
4. Keep EventStore UI-agnostic. It may describe domain/projection target identity, but it must not
   know FrontComposer component names, route layout, or presentation-only lane keys.
5. Specify compatibility, serialization, authorization, tenant isolation, redaction, and unknown
   field behavior.
6. Obtain explicit approval before editing the EventStore submodule or changing its pointer.

If no such unsupported flow is proven, record: `EventStore change: not required`.

## Non-Negotiable Constraints

- Never infer fresh rows from projection nudges or visible-row diffs.
- Never mark every row in a projection or lane as new.
- Never treat EventStore `AggregateId` as a universal generated-grid `EntityKey`.
- Never smuggle FC-NIP identity through optional untyped `ResultPayload`.
- Never add EventStore references to Contracts or SourceTools.
- Never hand-edit generated files under `obj/**/generated/HexalithFrontComposer/`.
- Preserve SourceTools' pure equatable IR and netstandard-compatible dependency boundary.
- Use FrontComposer/Fluent UI v5 components and Fluent 2 tokens for UI changes.
- Use `DiffEngine_Disabled=true` for Verify-backed tests.
- Preserve intentional snapshots and public API baselines; update them only for owned changes.

## Verification

Run the most focused red/green tests for every reproduced issue, followed by the relevant Shell and
SourceTools lanes. Then run the repository's filtered solution regression lane with
`DiffEngine_Disabled=true`.

At minimum, cover:

- pending-command outcome resolution and duplicate observations;
- missing and ambiguous row metadata;
- EventStore status mapping without `AggregateId` promotion;
- generated command runtime row-context capture;
- generated-grid lane isolation, rendering, and dismissal;
- affected Verify snapshots;
- FC-TBL package-boundary/public API evidence when applicable;
- story-artifact reconciliation for every changed file.

If an environment prevents a required lane, record the exact command, blocker, focused fallback
evidence, and CI authority. Do not claim an unexecuted lane passed.

## Deliverables

Return:

1. A current-state verdict: already satisfied, FrontComposer changes required, or an approved
   EventStore contract is required.
2. Evidence for every reproduced or dismissed residual finding.
3. The minimal implementation and tests for verified gaps.
4. An explicit statement that EventStore was unchanged, or the approved ADR/contract and separate
   submodule change evidence.
5. Reconciled story/file/test evidence.
6. A recommendation to correct the Epic 9 parent status separately from code changes.

Do not mark work complete while a required test is failing or while the implementation depends on
unproven row identity.
