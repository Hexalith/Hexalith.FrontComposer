---
title: 'Story 9.5: Make indicator state observable and scope-safe'
type: 'feature'
created: '2026-08-16'
status: 'done'
baseline_commit: '9e212f17914a214717028f6047642083ea8012c9'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-context.md'
  - '{project-root}/_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** New-item state mutates without invalidating generated grids, so indicators appear or disappear only after unrelated renders. Scope is checked only by `Add`, allowing a previous tenant/user entry to remain readable before another producer mutation.

**Approach:** Add a backward-compatible, lane-scoped disposable notification seam, make scope validation and state mutation atomic, and have generated grid views subscribe, marshal rerenders through `InvokeAsync(StateHasChanged)`, and unsubscribe before teardown.

## Boundaries & Constraints

**Always:** Preserve the ten-second TTL, generation guard, scoped DI lifetime, Ordinal keys, and broad filter/re-query dismissal. Notify each affected view exactly once after an effective mutation and outside state locks; isolate subscriber faults. Enforce valid tenant/user scope before `Snapshot` returns and fail closed on invalid or throwing registered accessors. Generated grids guard before scheduling and inside the dispatcher callback, then unsubscribe before asynchronous disposal.

**Ask First:** A different public subscription shape, an incompatible package/API change, a new authentication or tenant transition signal, edits overlapping the user-owned Story 9.4 files, or changes to dependencies, submodules, package boundaries, or release policy.

**Never:** Change producer eligibility, target identity, terminal resolution, already-visible-row behavior, or `(ViewKey, EntityKey)` replacement/TTL semantics; those remain Story 9.4/9.6 concerns. Do not edit `FcNewItemIndicator` styling/localization, generated `obj/` output, EventStore, or unrelated dirty files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Add | Grid already rendered; entry added/replaced | Affected view receives one notification and renders automatically | Invalid keys retain current validation; disposed service no-ops |
| Dismiss | Materialization, filter/re-query, matching TTL, or non-empty clear removes entries | One notification per affected view; DOM removes indicators without manual render | Misses, empty clear, and stale timers emit none |
| Scope read | Tenant/user changes before another `Add` | Old timers/entries clear atomically before `Snapshot`; affected views notify once | Missing, whitespace, or throwing registered scope fails closed |
| Race/teardown | Timer, clear, subscription disposal, and grid disposal overlap | At most the winning effective mutation notifies; no deadlock or disposed-component render | Subscriber failures do not block healthy subscribers |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/INewItemIndicatorStateService.cs` -- add the compatible lane subscription contract with XML documentation.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs` -- atomically enforce scope for `Add`/`Snapshot`; capture affected views under `_gate`, dispose timers and publish once outside it.
- `src/Hexalith.FrontComposer.Shell/Services/SnapshotPublisher.cs` -- reuse its race-safe unsubscribe, replay-disabled fan-out, and subscriber isolation; do not duplicate publisher mechanics.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` -- grid-only subscription in `OnInitialized`, dedicated dispatcher callback, disposed guard, and unsubscribe-first `DisposeAsync`; leave non-grid strategies unchanged.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs` -- exact notification, scope, invalid-accessor, and timer/clear/dispose race matrix.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorLaneIntegrationTests.cs` -- replace forced rerenders with a subscribed/disposable consumer and automatic TTL/materialization/filter assertions.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs` -- render the real generated grid first, then prove automatic add/removal for every mutation without `cut.Render()`.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.cs` and the 12 affected grid `.verified.txt` files -- pin emitted subscribe/dispatch/dispose behavior; non-grid approvals remain byte-identical.
- `tests/Hexalith.FrontComposer.Shell.Tests/{State/PendingCommands/PendingCommandPublicCompatibilityTests.cs,Architecture/SecurityLoggingGovernanceTests.cs}` -- preserve package compatibility and deliberately refresh exact source-location evidence.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Shell/State/PendingCommands/{INewItemIndicatorStateService.cs,NewItemIndicatorStateService.cs}` -- implement compatible effective-mutation notifications, atomic fail-closed scope reads, subscriber isolation, and race-safe teardown while preserving Story 9.6 behavior.
- [x] `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.cs`, and affected grid approvals -- emit and pin grid-only subscription/disposal plumbing; refresh only intentional snapshots.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/{Components/DataGrid/FcNewItemIndicatorTests.cs,Components/DataGrid/FcNewItemIndicatorLaneIntegrationTests.cs,Generated/CounterStoryVerificationTests.cs,State/PendingCommands/PendingCommandPublicCompatibilityTests.cs,Architecture/SecurityLoggingGovernanceTests.cs}` -- prove notification counts, post-render DOM changes, scope/race safety, and compatibility without absorbing concurrent changes.

**Acceptance Criteria:**
- Given any effective add, materialization, filter/re-query dismissal, TTL expiry, clear, or scope-clear, when it completes, then each affected generated grid receives one notification and rerenders through `InvokeAsync(StateHasChanged)`.
- Given a generated grid rendered before each mutation, when the mutation occurs, then bUnit observes automatic DOM appearance/removal without calling `cut.Render()`.
- Given tenant/user change or invalid registered scope before another producer mutation, when state is read or rendered, then previous-scope entries cannot be returned or displayed.
- Given concurrent mutation and teardown, when operations settle, then notification delivery is bounded, fault-isolated, and cannot rerender a disposed component.

## Spec Change Log

## Design Notes

Use a view-key subscription over the shared publisher mechanics with replay disabled; initial render already reads `Snapshot`. A default inert subscription implementation preserves older custom interface implementations, while the built-in service provides observable behavior. Deduplicate affected view keys within one atomic operation. A constructor with no user accessor retains its existing unscoped test/adopter mode; a registered accessor is authoritative and fail-closed.

## Verification

**Commands:**
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:EnableFrontComposerPackageValidation=true && dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -p:EnableFrontComposerPackageValidation=true` -- clean Release compilation with warnings as errors.
- `python3 eng/pack_release_packages.py --version 4.0.0-ci.story9-5 --output /tmp/frontcomposer-story-9-5-release-pack` -- eight-package/API compatibility passes.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --no-restore --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- blocking test lane passes.
- `git diff --check` -- no whitespace errors.

## Suggested Review Order

**Observable state boundary**

- Start with lane-scoped subscriptions, atomic scope enforcement, and effective mutation fan-out.
  [`NewItemIndicatorStateService.cs:43`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs#L43)

- See the compatible default interface seam retained for existing custom implementations.
  [`INewItemIndicatorStateService.cs:15`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/INewItemIndicatorStateService.cs#L15)

- Review fail-closed snapshots before previous-scope entries can escape.
  [`NewItemIndicatorStateService.cs:152`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs#L152)

- Verify cleanup failures cannot suppress committed notifications or remaining timer disposal.
  [`NewItemIndicatorStateService.cs:346`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs#L346)

**Generated-grid lifecycle**

- Follow grid-only subscription wiring from component initialization.
  [`RazorEmitter.cs:1048`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs#L1048)

- Inspect dispatcher-marshaled rerenders with guards before and inside scheduling.
  [`RazorEmitter.cs:1072`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs#L1072)

- Confirm unsubscribe occurs before asynchronous component teardown.
  [`RazorEmitter.cs:1200`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs#L1200)

**Acceptance and compatibility evidence**

- Exercise every post-render mutation against the actual generated grid.
  [`CounterStoryVerificationTests.cs:145`](../../tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs#L145)

- Prove generated-grid subscription disposal exactly once at runtime.
  [`CounterStoryVerificationTests.cs:254`](../../tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs#L254)

- Pin add-first scope transitions and deduplicated cross-lane notification behavior.
  [`FcNewItemIndicatorTests.cs:195`](../../tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs#L195)

- Preserve notification delivery when custom timer cleanup fails nonfatally.
  [`FcNewItemIndicatorTests.cs:79`](../../tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs#L79)

- Confirm legacy interface implementations inherit the inert compatible subscription default.
  [`PendingCommandPublicCompatibilityTests.cs:174`](../../tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPublicCompatibilityTests.cs#L174)

- Pin generated source shape and keep non-grid strategies unsubscribed.
  [`RazorEmitterTests.cs:84`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.cs#L84)
