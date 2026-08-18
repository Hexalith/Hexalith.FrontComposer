---
title: 'Story 9.6: Enforce atomic per-row first-wins'
type: 'feature'
created: '2026-08-18'
status: 'done'
baseline_commit: 'b8c8f01acdfa8160e5fc050bfadf72da922fe950'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-context.md'
  - '{project-root}/_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `NewItemIndicatorStateService.Add` is unconditionally last-wins: a second confirmed command targeting an already-active `(ViewKey, EntityKey)` overwrites `MessageId` and `CreatedAt`, disposes the incumbent timer, and installs a fresh ten-second timer. An operator therefore sees a row's fresh-row provenance silently replaced and its lifetime extended by unrelated later commands.

**Approach:** Make the first eligible publication own the row: inside the existing state-service lock, an `Add` for an already-active row is suppressed rather than applied, leaving the incumbent entry, timer, and generation untouched. The row re-opens only when the active entry leaves `_entries`.

## Boundaries & Constraints

**Always:** Decide the winner inside the existing `_gate` critical section — the resolver publishes outside its own lock, so decision order and `Add` arrival order can invert. A suppressed `Add` must leave the incumbent entry, its `MessageId`, `CreatedAt`, timer, and generation byte-identical; must dispose any timer it speculatively created; and must not add its view key to `affectedViewKeys`. Apply the scope boundary before the occupancy test, so a tenant/user transition still clears the row first. Preserve the ten-second TTL, the generation guard, Ordinal keys, scoped DI lifetime, and every existing dismissal path.

**Ask First:** Any new or changed member on `INewItemIndicatorStateService`, `IPendingCommandOutcomeResolver`, or `NewItemIndicatorEntry`; any change to `PendingCommandOutcomeResolver`'s publication or dedup design; a TTL value change; edits to dependencies, submodules, package boundaries, or release policy.

**Never:** Change producer eligibility, target identity, terminal materiality, or terminal resolution — those are Story 9.3/9.4 and are already correct. Do not re-key `_indicatorDecisions` by `(ViewKey, EntityKey)`; it is permanent for the circuit and has no notion of expiry or dismissal, so that would permanently block a legitimate later entry. Do not widen or hold the resolver lock across publication. Do not edit `FcNewItemIndicator` styling/localization, `RazorEmitter`, generated `obj/` output, EventStore, or the dirty `references/Hexalith.Memories` gitlink.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Distinct message, active row | Entry for `(v1,e1)` active; `Add` with a different `MessageId` for `(v1,e1)` | Incumbent `MessageId`/`CreatedAt`/expiry survive unchanged; no notification for that view | Speculative timer disposed; non-fatal disposal faults swallowed as today |
| Concurrent publication | Two distinct message IDs `Add` the same row simultaneously | Exactly one entry, one timer/provenance pair, one notification; winner is whichever enters the lock first | Neither call throws; no deadlock |
| Row re-opened | Active entry removed by TTL expiry, materialization, filter/re-query, `Clear`, or scope transition | A later `Add` for that row is accepted normally and starts a fresh ten-second lifetime | Unchanged validation and fail-closed scope behavior |
| Duplicate same message | Same `MessageId` observed twice | Already suppressed at the resolver; no second `Add` reaches the state service | Unchanged (`DuplicateIgnored` + `_indicatorDecisions`) |
| Distinct rows | Two message IDs targeting different `(ViewKey, EntityKey)` | Both publish independently, as today | N/A |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs:122-132` -- the sole change site. `_entries.Remove(key, out existing)` + `_entries[key] = new TrackedEntry(...)` is the unconditional overwrite; replace with an occupancy test. The surrounding `lock (_gate)` (`:80`), the `installed` flag and its `finally { if (!installed) DisposeTimer(timer); }` (`:112-140`), and `ApplyScopeBoundaryLocked` (`:116`, must stay ahead of the test) already provide everything needed — no new synchronization primitive.
- `…/NewItemIndicatorStateService.cs:100,359,364,373-392` -- `Interlocked.Increment(ref _generationCounter)` runs before the occupancy test, so a suppressed `Add` may burn a generation number: harmless, the counter is monotonic and the incumbent's stored generation is untouched. `OnTimerFired`'s generation guard is already correct (a stale timer cannot evict a newer entry) — read-only. The existing `FrontComposerHotPathLog` call sites and `_logger` field (`:18`, `NullLogger` default at `:36`) are the seam for the suppression event.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerHotPathLog.cs:386-396,869-871` -- `NewItemMetadataIncomplete` (EventId 5759) is the exact template: public wrapper guards `IsEnabled`, digests identifiers via `DigestIdentifier`, then calls the private partial. Next free EventId is **5784**.
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs:307` -- pins hot-path IDs to `Enumerable.Range(5700, 84)`; a new event requires 84→85.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerHotPathLogTests.cs:204-207` -- pins count `84`, the same range, and the ordered `ExpectedEventNames`/`ExpectedLevels` arrays. Both files must be updated in lockstep or the Governance lane fails.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs:130` -- `State_ReplacedEntry_KeepsTheNewGenerationAndLifetime` **asserts the opposite semantics** (last-wins, TTL reset to t+15s). Must be rewritten, not merely supplemented. In-file helpers to reuse: `CreateScopedState` (`:293`), `RecordingTimerTimeProvider`/`RecordingTimer` with `DisposeCount` (`:298`, `:318`), and the racing-tasks precedent `State_TimerClearAndUnsubscribeRace_BoundsDeliveryAndLeavesNoEntry` (`:246`).
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/SnapshotPublisherTests.cs:165-206` -- repo reference pattern for true interleaving races: `Barrier`, two `IsBackground` threads, 1000 iterations, invariant that holds on every legal interleaving.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandOutcomeResolverTests.cs:82,554,586,933` -- read-only evidence that duplicate-same-`MessageId` and gate-release behavior are already covered by Story 9.4; every existing multi-message test deliberately uses distinct entity keys, so the same-row collision is untested.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:18,387,435` -- read-only: `_indicatorDecisions` is `MessageId`-keyed and permanent; `Add` is called outside `_gate`. Explains why the fix cannot live here.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs` -- suppress `Add` when the row is already active, preserving the incumbent entry/timer/generation, disposing the speculative timer, and emitting no notification for that view -- makes first-wins atomic under the one lock that owns the active-entry set.
- [x] `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerHotPathLog.cs` -- add EventId 5784 `NewItemIndicatorSuppressed` (Debug) with digested `MessageId`, `ViewKey`, and `EntityKey`, following the 5759 wrapper pattern -- makes a suppressed publication diagnosable in production without logging raw identifiers.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/{Architecture/SecurityLoggingGovernanceTests.cs,Infrastructure/Telemetry/FrontComposerHotPathLogTests.cs}` -- extend the pinned event range to 85 and append the new name/level -- both are contiguity gates that fail closed on any new event.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs` -- rewrite `State_ReplacedEntry_KeepsTheNewGenerationAndLifetime` to assert first-wins, and add the I/O matrix cases: suppressed-`Add` timer disposal, notification silence, row re-opening after each removal path, and a concurrent two-message race -- the existing test currently pins the behavior this story reverses.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandOutcomeResolverTests.cs` -- add one composed case where two distinct confirmed message IDs resolve to the same `(ViewKey, EntityKey)` against the real state service and only the first indicator survives -- proves the guard holds through the actual producer boundary, not just at the service API.

**Acceptance Criteria:**
- Given an active entry for a row, when a later distinct material command targets that same `(ViewKey, EntityKey)`, then the first `MessageId`, `CreatedAt`, provenance, and original expiry instant all win and the TTL is not reset or extended.
- Given the active entry is removed by expiry, materialization, filter/re-query, `Clear`, or a scope transition, when a later material command targets that row, then a new entry is accepted and starts a fresh ten-second lifetime.
- Given concurrent publication of two distinct message IDs for one row, when both settle, then exactly one active entry and one original timer/provenance pair are observed.
- Given the whole blocking test lane, when it runs, then no existing indicator, resolver, scope, notification, or generated-grid behavior regresses.


### Review Findings

- [x] [Review][Decision] Submodule pointer updates in commit e014386c (Hexalith.Builds and Hexalith.Memories) -- Human decision: Kept in branch feat/9-6-atomic-per-row-first-wins as intentional updates.
- [x] [Review][Patch] Reseal CA1707 test underscore identifier inventory in _bmad-output/contracts/analyzer-policy-exception-ledger-v1.json (6994 tokens, sha256 83022848dd488ece6bb4f97a92f46ad25ef21a085285277555430c0dfa99aed5) [`_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`] -- verified with AnalyzerPolicy_IdentifierInventory_MatchesSeal
- [x] [Review][Patch] Suppression log call site verified by nothing; all tests used `NullLogger` [`src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs`] -- covered with `CapturingLogger<T>`; mutation-verified
- [x] [Review][Patch] Event 5784 could not identify the row; `EntityKey` missing from the payload [`src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerHotPathLog.cs`]
- [x] [Review][Patch] Race-test thread bodies had no try/catch; a throw terminated the test host [`tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs`]
- [x] [Review][Patch] Race test asserted on the first `Join` while the second thread was still live, disposing the `Barrier` underneath it [`tests/.../FcNewItemIndicatorTests.cs`]
- [x] [Review][Patch] `IndicatorDecisionCount.ShouldBe(2)` did not prove the second observation reached `Add` [`tests/.../PendingCommandOutcomeResolverTests.cs`]
- [x] [Review][Patch] Newly reachable generation-mismatch path (armed speculative timer after swallowed disposal fault) was untested [`tests/.../FcNewItemIndicatorTests.cs`]
- [x] [Review][Patch] Double hash lookup and two flags for one fact; rotting comment; `Add` contract undocumented [`src/.../NewItemIndicatorStateService.cs`, `src/.../INewItemIndicatorStateService.cs`]
- [x] [Review][Patch] Re-open test asserted the fresh lifetime only for the TTL path; timer double used the real system clock [`tests/.../FcNewItemIndicatorTests.cs`]
- [x] [Review][Defer] Speculative timer created before the occupancy test -- frozen I/O matrix expects it; recorded in deferred-work
- [x] [Review][Defer] Stale last-wins prose in `fc-nip-row-identity-producer-contract-2026-07-04.md` (historical record; human decision)
- [x] [Review][Defer] Published DataGrid reference page does not document first-wins/suppression/re-open
- [x] [Review][Defer] No lane- or component-level coverage of a suppressed publication

## Spec Change Log

## Design Notes

The active-entry window is exactly membership in `_entries`. Every removal path already funnels through the lock — `OnTimerFired` (`:373`), `DismissMaterialized` (`:237`), `DismissForFilterChange` (`:208`), `Clear` (`:261`), `ApplyScopeBoundaryLocked` (`:290`) — so no separate "window" bookkeeping is required, and AC-2 falls out of the same test.

`PendingCommandOutcomeResolver` is deliberately untouched. It still burns each `MessageId` in `_indicatorDecisions` even when the state service suppresses the `Add`, which is correct: the FC-NIP contract requires that the same accepted `MessageId` never republish, including after its indicator is dismissed or expires.

Story 9.4 already satisfies the epic's duplicate-observation requirement through the `DuplicateIgnored` status gate and the `_indicatorDecisions` set, with regression tests at `PendingCommandOutcomeResolverTests.cs:82,396,755`. Verify it; do not re-implement it.

No generated-output change is expected — consumers only `Subscribe`/`Snapshot`/`DismissMaterialized`/`DismissForFilterChange`. If a `.verified.txt` grid approval moves, stop and investigate rather than accepting it.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug` -- clean compile with `TreatWarningsAsErrors`.
- `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests -class Hexalith.FrontComposer.Shell.Tests.Components.DataGrid.FcNewItemIndicatorTests` -- focused first-wins suite green (VSTest sockets are blocked in this environment; invoke the built xUnit v3 runner directly).
- Same runner with `-class` for `…State.PendingCommands.PendingCommandOutcomeResolverTests`, `…Architecture.SecurityLoggingGovernanceTests`, and `…Infrastructure.Telemetry.FrontComposerHotPathLogTests` -- resolver and both event-id contiguity gates green.
- `dotnet build Hexalith.FrontComposer.slnx -c Release` then `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- full blocking lane; record any environment blocker separately rather than weakening the gate.
- `git diff --check` -- no whitespace errors.

## Suggested Review Order

**The first-wins decision**

- Start here: one atomic `TryAdd` under `_gate` is the whole behavior change.
  [`NewItemIndicatorStateService.cs:128`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs#L128)

- The contract adopters must read: active window, re-open conditions, and why arrival order wins over `CreatedAt`.
  [`INewItemIndicatorStateService.cs:22`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/INewItemIndicatorStateService.cs#L22)

- Confirm the scope boundary still runs ahead of the occupancy test, so a tenant switch clears first.
  [`NewItemIndicatorStateService.cs:118`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs#L118)

- The suppressed call's only observable trace, emitted outside the lock.
  [`NewItemIndicatorStateService.cs:151`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs#L151)

**Suppression telemetry**

- Wrapper digests all three identifiers before they reach the log.
  [`FrontComposerHotPathLog.cs:551`](../../src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerHotPathLog.cs#L551)

- Event 5784 carries the full `(ViewKey, EntityKey)` row identity, not just the view.
  [`FrontComposerHotPathLog.cs:999`](../../src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerHotPathLog.cs#L999)

**Behavioral proof**

- The reversal itself: incumbent provenance survives and the original expiry still governs.
  [`FcNewItemIndicatorTests.cs:133`](../../tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs#L133)

- Composed proof through the real producer boundary, with two distinct confirmed message IDs.
  [`PendingCommandOutcomeResolverTests.cs:106`](../../tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandOutcomeResolverTests.cs#L106)

- True race: Barrier, two threads, 1000 iterations, invariant holds for either winner.
  [`FcNewItemIndicatorTests.cs:319`](../../tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs#L319)

- A swallowed disposal fault leaves a newer-generation timer armed; the guard must ignore its late fire.
  [`FcNewItemIndicatorTests.cs:163`](../../tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs#L163)

- Every removal path re-opens the row with a full fresh ten-second window.
  [`FcNewItemIndicatorTests.cs:243`](../../tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs#L243)

- Scope transition clears before the occupancy test, so the same row re-opens.
  [`FcNewItemIndicatorTests.cs:292`](../../tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs#L292)

- Suppression is pinned by its diagnostic; deleting the log block fails this test.
  [`FcNewItemIndicatorTests.cs:203`](../../tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs#L203)

- Regression guard: distinct rows still publish independently.
  [`FcNewItemIndicatorTests.cs:227`](../../tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs#L227)

**Contiguity gates**

- Both pin the hot-path event range; they fail closed on any new event id.
  [`SecurityLoggingGovernanceTests.cs:307`](../../tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs#L307)

- Count, range, ordered names, and ordered levels all move together.
  [`FrontComposerHotPathLogTests.cs:208`](../../tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerHotPathLogTests.cs#L208)
