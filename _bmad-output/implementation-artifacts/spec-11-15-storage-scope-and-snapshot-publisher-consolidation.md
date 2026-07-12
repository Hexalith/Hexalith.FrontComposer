---
title: 'Story 11.15: Storage scope and snapshot publisher consolidation'
type: 'refactor'
created: '2026-07-12'
status: 'blocked'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '_bmad-output/project-context.md'
  - '_bmad-output/implementation-artifacts/epic-11-context.md'
warnings:
  - multiple-goals
  - oversized
---

<intent-contract>

## Intent

**Problem:** Shell persistence repeats tenant/user scope resolution in six effects, while non-Fluxor state notification uses several hand-built containers with inconsistent replay, disposal, and subscriber-fault behavior. This duplication risks applying tenant hardening and subscription fixes unevenly.

**Approach:** Replace the six duplicated effect-local resolvers with one scoped Shell-internal resolver, and reuse one Shell-internal snapshot-publisher primitive across the notification sites selected by the unresolved M19 boundary. Preserve all public contracts, storage-key bytes, owner-specific state transitions, and circuit scoping.

## Boundaries & Constraints

**Always:** Resolve the current accessor on every operation; fail closed before storage I/O for missing, blank, or non-fatally throwing tenant/user access; preserve each feature's existing no-scope continuation and `HFC2105` support-safe logging; retain `StorageKeys`/`FrontComposerStorageKey` canonicalization and key shapes; keep publishers owned by scoped services; preserve atomic current/replay ordering, idempotent unsubscribe, and per-subscriber non-fatal fault isolation; retain projection reconnect and reconciliation epoch/dedup/telemetry policy in their owners.

**Block If:** The implementation boundary has not selected (1) whether storage consumers are only the six duplicated Fluxor effects or also LastUsed/readiness near-variants, and (2) whether snapshot consolidation covers only the two replayable twins or fully closes M19 by adapting BadgeCount away from Rx and/or other hot, keyed, or event-based publishers.

**Never:** Cache tenant/user identity across auth changes; return pre-escaped identities to existing key builders; expose raw IDs or exception details; add storage write sites; register a cross-circuit singleton publisher; change `IProjectionConnectionState`, `IReconnectionReconciliationState`, `IBadgeCountService`, `ICommandFeedbackPublisher`, or `ILifecycleStateService`; add replay to hot warning/badge streams; move owner-specific transition rules into the generic primitive.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Valid persisted scope | Current nonblank tenant and user | Existing storage key and feature-specific hydrate/persist behavior remain byte-for-byte equivalent | No error expected |
| Invalid persisted scope | Missing accessor, blank identity, or non-fatal accessor failure | No storage read/write/remove/enumeration; feature-specific fallback still occurs | Sanitized fail-closed diagnostic; fatal exceptions propagate |
| Snapshot subscription | Subscribe with replay enabled/disabled around concurrent publish | Current snapshot is coherent; replay never arrives stale after a fresher update | Subscriber faults do not stop healthy subscribers |
| Subscription disposal | Token disposed once or repeatedly, including near publish | No later callbacks after successful removal; disposal is idempotent | No error expected |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Shell/State/{Theme,Density,Navigation,DataGridNavigation,CapabilityDiscovery,CommandPalette}/*Effects.cs` -- six private `TryResolveScope` definitions and 16 invocations; Command Palette has the strongest current accessor-failure guard.
- `src/Hexalith.FrontComposer.Shell/State/StorageKeys.cs` -- existing key-family builder whose output must not change.
- `src/Hexalith.FrontComposer.Shell/Services/DerivedValues/LastUsedValueProvider.cs` -- materially different near-variant with one-per-circuit D31 behavior.
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionState.cs` -- replayable current-snapshot publisher with reconnect and sensitive-logging policy.
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationState.cs` -- replayable current-snapshot publisher with atomic epoch rules.
- `src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs` -- Rx-backed hot/no-replay public observable implicated by the originating M19 finding.
- `src/Hexalith.FrontComposer.Shell/Services/{Feedback,Lifecycle}` -- incompatible hot-only and correlation-keyed notification contracts requiring an explicit inclusion decision.
- `tests/Hexalith.FrontComposer.Shell.Tests/{State,Badges,Services,Architecture,Governance}` -- focused equivalence, lifetime, fault-isolation, and layering evidence.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.FrontComposer.Shell/Services/` -- add and directly test the selected internal scope resolver and snapshot primitive after the two inclusion decisions are recorded.
- `src/Hexalith.FrontComposer.Shell/State/` -- migrate the six unambiguous persisted effects, preserve DataGrid's pre/post-debounce re-resolution and every feature-specific fallback, then remove all six local resolver definitions.
- `src/Hexalith.FrontComposer.Shell/State/{ProjectionConnection,ReconnectionReconciliation}/` -- reuse the shared primitive without weakening reconnect, epoch, deduplication, logging, or telemetry semantics.
- `src/Hexalith.FrontComposer.Shell/{Badges,Services}/` and package metadata -- migrate only the additionally selected M19 notification sites while preserving their frozen public replay/completion shapes.
- `tests/Hexalith.FrontComposer.Shell.Tests/` -- add primitive matrices, former-owner equivalence tests, scoped-lifetime validation, and a source guard against renewed effect-local scope resolution.

**Acceptance Criteria:**
- Given the selected persisted-feature inventory, when scope-dependent effects execute, then one resolver uniformly fails closed and all existing successful storage behavior and key bytes remain unchanged.
- Given the selected notification inventory, when subscribers replay, publish, throw, unsubscribe, or race disposal, then the shared primitive supplies the common behavior once while each owner retains its distinct observable contract.
- Given the completed migration, when governance and story evidence are inspected, then the baseline `6 definitions / 16 invocations` is reconciled to `0 effect-local definitions / 16 shared invocations`, and exact before/after notification-container counts are documented.

## Spec Change Log

## Review Triage Log

## Design Notes

The current sources support multiple incompatible boundaries. The final story AC names “several hand-rolled snapshot pub/sub containers,” while the originating M19 finding also requires one non-Fluxor notification idiom and removal of the Shell's sole `System.Reactive` dependency. Only ProjectionConnection and ReconnectionReconciliation are true replayable snapshot twins; BadgeCount is hot/no-replay, Lifecycle is keyed, Feedback is hot-only, and other sites are plain events. Selecting a migration inventory changes observable behavior and package scope, so it cannot be inferred safely.

## Verification

**Commands:**
- `dotnet restore Hexalith.FrontComposer.slnx -p:NuGetAudit=false` -- expected: restore succeeds.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: zero warnings and errors.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` -- expected: all Shell tests pass.
- `git diff --check` -- expected: no whitespace errors.

## Auto Run Result

Status: blocked
Blocking condition: intent gap

Unanswered decisions:

1. Does “all Shell persisted features” mean the six duplicated Fluxor effects only, or must LastUsed and storage-readiness near-variants also use the resolver despite their distinct diagnostic and transition semantics?
2. Does Story 11.15 fully close M19 by replacing BadgeCount's Rx implementation, or does it consolidate only the two replayable snapshot twins?
3. Are hot/no-replay `CommandFeedbackPublisher`, correlation-keyed `LifecycleStateService`, and plain event publishers explicitly excluded, or must approved adapters migrate some/all of them?
