# Dev Notes

### Service Binding Reference (Task 4.3 registration)

```csharp
// Added to AddHexalithFrontComposer()
services.TryAddSingleton<IUlidFactory, UlidFactory>();
services.TryAddScoped<ILifecycleStateService, LifecycleStateService>();
services.TryAddScoped<LifecycleBridgeRegistry>();
```

### Files Touched Summary

**Contracts/Lifecycle/** (new):
- `ILifecycleStateService.cs`
- `CommandLifecycleTransition.cs`
- `IUlidFactory.cs`
- `LifecycleOptions.cs`
- *(cut per T1: `ICommandLifecycleAction.cs`. cut per T2: `ConnectionState.cs`.)*

**Shell/Services/Lifecycle/** (new):
- `LifecycleStateService.cs`
- `LifecycleBridgeRegistry.cs`
- `UlidFactory.cs`

**SourceTools/Emitters/** (new):
- `CommandLifecycleBridgeEmitter.cs`

**SourceTools/Emitters/** (modified):
- `CommandFluxorActionsEmitter.cs` — `ResetToIdleAction` gains `CorrelationId` parameter (marker interface application cut per T1)
- `CommandFormEmitter.cs` — inject `LifecycleBridgeRegistry`, call `Ensure<T>()` before first submit; `ResetToIdleAction` dispatch passes `_submittedCorrelationId`
- `CommandParser.cs` — reject generic `[Command]` types with HFC1017 (Hindsight H9 per T14)
- (No change needed to `CommandFluxorFeatureEmitter.cs`; the OnResetToIdle reducer already ignores any new param)

**Shell/Services/** (modified):
- `StubCommandService.cs` — inject `IUlidFactory`, replace Guid with ULID for MessageId
- `DerivedValues/SystemValueProvider.cs` — inject `IUlidFactory`, `"MessageId"` branch uses it

**Shell/Extensions/** (modified):
- `ServiceCollectionExtensions.cs` — new registrations + extend `AddHexalithDomain<T>()` to auto-register `*LifecycleBridge` types

**SourceTools/Diagnostics/** (modified):
- `DiagnosticDescriptors.cs` — reserve HFC1016; HFC2004/2005/2006 are runtime-only (logged, not emitted as diagnostics from the generator)

**SourceTools/** (modified):
- `FrontComposerGenerator.cs` — wire bridge emitter into the `[Command]` pipeline
- `AnalyzerReleases.Unshipped.md` — HFC1016 reservation (HFC2004/5/6 are runtime log codes, NOT analyzer-emitted — do NOT add to AnalyzerReleases)

**Directory.Packages.props** (modified):
- Add `NUlid 1.7.4`

**samples/Counter/Counter.Web/** (modified):
- `CounterProjectionEffects.cs` — optional debug log reading `ILifecycleStateService.GetState(correlationId)` at Confirmed dispatch (Task 8.1)
- `Program.cs` — no explicit edit typically required: `AddHexalithFrontComposer()` registers `IUlidFactory` automatically via Task 4.3. If Counter.Web constructs `StubCommandService` directly anywhere (inspect during dev), add explicit `IUlidFactory` passing. Flag here in case dev discovers such a site.

**tests/Hexalith.FrontComposer.Shell.Tests/** (new + modified):
- `Services/Lifecycle/UlidFactoryTests.cs` (new, 4 tests)
- `Services/Lifecycle/LifecycleStateServiceTests.cs` (new, 5+5+4+3 = 17 tests)
- `Services/Lifecycle/LifecycleStateMachinePropertyTests.cs` (new, 10 property tests)
- *(cut per T4 2026-04-16: `Services/Lifecycle/FakeTimeProvider.cs` — no time-dependent code to test)*
- `Services/Lifecycle/TestUlidFactory.cs` (new test helper)
- `Services/Lifecycle/LifecycleRegistrationTests.cs` (new, 3 DI tests)
- `Services/Lifecycle/LifecycleNoPersistenceTests.cs` (new, 1 test)
- `Components/CounterPageLifecycleE2ETests.cs` (new, 3 e2e tests)
- `Services/StubCommandServiceTests.cs` (modified — update constructor calls)

**tests/Hexalith.FrontComposer.SourceTools.Tests/** (new + modified):
- `Emitters/CommandLifecycleBridgeEmitterTests.cs` (new, 3+2 = 5 tests + 2 baselines)
- `Integration/CommandLifecycleBridgeIntegrationTest.cs` (new, 1 test)
- `Snapshots/LifecycleBridgeEmitterTests.Emit_IncrementCommand.verified.txt` (new baseline)
- `Snapshots/LifecycleBridgeEmitterTests.Emit_ConfigureCounterCommand.verified.txt` (new baseline)

### Naming Convention Reference

| Element | Pattern | Example |
|---|---|---|
| Generated lifecycle bridge | `{CommandTypeName}LifecycleBridge.g.cs` (Decision D16) | `IncrementCommandLifecycleBridge.g.cs` |
| Generator hint | `{QualifiedHintPrefix}.LifecycleBridge.g.cs` | `Counter.Domain.IncrementCommand.LifecycleBridge.g.cs` |
| Service implementation | `LifecycleStateService` | — |
| Registry | `LifecycleBridgeRegistry` (mirrors `LastUsedSubscriberRegistry`) | — |
| ULID factory | `UlidFactory` / `IUlidFactory` | — |
| Transition record | `CommandLifecycleTransition` | — |
| Action record (existing, only `ResetToIdleAction` extended) | `{CommandName}Actions.{Verb}Action` carrying `string CorrelationId` | `IncrementCommandActions.ConfirmedAction` |

### Testing Standards

- xUnit v3 (3.2.2), Verify.XunitV3, Shouldly, NSubstitute, bUnit 2.7.2, FsCheck.Xunit.v3 (inherited from 2-1/2-2)
- Property tests: 1000 iterations in CI, 10,000 in nightly (per architecture.md §1419)
- `TestContext.Current.CancellationToken` on all `RunGenerators`/`GetDiagnostics`/`ParseText` (xUnit1051)
- `TreatWarningsAsErrors=true` global
- `DiffEngine_Disabled: true` in CI
- **Test count budget (D17):** 42 new tests (11 service + 10 FsCheck + 5 bridge emitter + 4 ULID factory + 3 e2e + 3 DI + others). Cumulative target: ~505 tests.
- **Property test determinism:** each `[Property]` attribute uses a pinned seed (QuietOnSuccess = true; Seed is `Guid("D5F7-...")` derived from property name hash) so a shrunk case can be reproduced

### Build & CI

- Build race CS2012: `dotnet build` then `dotnet test --no-build` (inherited pattern)
- `AnalyzerReleases.Unshipped.md` update for HFC1016 only (2004/5/6 are runtime log codes per architecture.md §648 — code range 2000-2999 is assigned to Shell, and Shell-range diagnostics are runtime logs, not analyzer-emitted; skipping AnalyzerReleases entry is correct — see HFC2001 precedent which is also a runtime diagnostic)
- Roslyn 4.12.0 pinned (inherited)
- `NUlid 1.7.4` added to Directory.Packages.props; referenced from Shell only (Contracts stays dependency-free per architecture.md §1144)

### Previous Story Intelligence

**From Story 2-2 (immediate predecessor):**

- **Patterns that worked:** Hand-written service + per-command emitted subscriber (L05 — `LastUsedValueProvider` + `{Command}LastUsedSubscriber.g.cs`). **Story 2-3 reuses verbatim for the bridge.**
- **Lazy subscriber activation (D35 of 2-2):** idempotent `LastUsedSubscriberRegistry.Ensure<T>()` called at form submit time — reused as `LifecycleBridgeRegistry.Ensure<T>()`.
- **FsCheck property-based testing:** Story 2-2 Task 3.9 used FsCheck for storage-key roundtrip; Story 2-3 extends to state machine. FsCheck.Xunit.v3 package already pinned (inherited).
- **CorrelationId-keyed dict with bounded eviction (D38 of 2-2):** `ConcurrentDictionary<CorrelationId, PendingEntry>` with TTL+MaxInFlight — Story 2-3 reuses the pattern for `_entries` (TTL=5min grace, no MaxInFlight because lifecycle state is user-bounded by circuit scope).
- **Tenant/user fail-closed (D31 of 2-2):** applied to persisted services only; lifecycle state is ephemeral, so the D13 "not at this layer" decision is deliberate (L03 caveat).
- **Snapshot test re-approval pattern (Task 5.3 of 2-2):** regression-gate byte-identical assertion. Story 2-3 applies to `ResetToIdleAction` signature change and marker interface application.

**From Story 2-1:**

- `CommandFluxorActionsEmitter` emits 6 action records — don't break their existing shape, just add marker interface.
- `CommandFormEmitter.EmitSubmitMethod` dispatches 6 actions — augment the `ResetToIdleAction` dispatch with the newly-required CorrelationId parameter.
- Fluxor `IActionSubscriber.SubscribeToAction<TAction>` is the subscription primitive — same one used by Story 2-2's LastUsedSubscriber.
- Stub command service tests (7) use zero delays — Story 2-3 stub changes add `IUlidFactory` constructor param; update stub tests to pass `TestUlidFactory`.

### References

- [Source: _bmad-output/planning-artifacts/epics/epic-2-command-submission-lifecycle-feedback.md#Story 2.3 — AC source of truth]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#FR23, FR30, FR36 — functional requirements]
- [Source: _bmad-output/planning-artifacts/epics/requirements-inventory.md#UX-DR12 — ILifecycleStateService requirements]
- [Source: _bmad-output/planning-artifacts/prd/non-functional-requirements.md#NFR44, NFR45, NFR47 — reliability non-functionals]
- [Source: _bmad-output/planning-artifacts/prd/functional-requirements.md#FR23, FR30, FR36 — functional requirements text]
- [Source: _bmad-output/planning-artifacts/architecture.md#141-143 — ULID message IDs + ETag caching]
- [Source: _bmad-output/planning-artifacts/architecture.md#397 — Decision D2 Fluxor lifecycle (Fluxor CommandLifecycleState + FrontComposerLifecycleWrapper)]
- [Source: _bmad-output/planning-artifacts/architecture.md#461-464 — CommandLifecycleState feature per command type; Actions SubmittedAcknowledgedConfirmedRejected; reducer handling state transitions; effect wiring to ProjectionSubscriptionService]
- [Source: _bmad-output/planning-artifacts/architecture.md#536 — CommandLifecycleState is ephemeral; evicted on terminal state; NOT persisted to IStorageService]
- [Source: _bmad-output/planning-artifacts/architecture.md#574 — CorrelationId mismatch in lifecycle: ULID-based correlation from command MessageId; unit test asserting 1:1]
- [Source: _bmad-output/planning-artifacts/architecture.md#589 — CommandLifecycleState + wrapper (D2) implementation sequence]
- [Source: _bmad-output/planning-artifacts/architecture.md#648 — HFC diagnostic ID ranges]
- [Source: _bmad-output/planning-artifacts/architecture.md#741 — Fluxor action payload rule: always include CorrelationId]
- [Source: _bmad-output/planning-artifacts/architecture.md#756 — Structured logging convention (OpenTelemetry, include CommandType + TenantId + CorrelationId)]
- [Source: _bmad-output/planning-artifacts/architecture.md#1144 — Contracts must not reference other packages (dependency-free)]
- [Source: _bmad-output/planning-artifacts/architecture.md#1419 — FsCheck conventions (1000 CI / 10000 nightly)]
- [Source: _bmad-output/planning-artifacts/architecture.md#1436 — Idempotent handling via ULID message IDs with deterministic duplicate detection]
- [Source: _bmad-output/implementation-artifacts/2-1-command-form-generation-and-field-type-inference.md#ADR-010 — ICommandServiceWithLifecycle callback contract]
- [Source: _bmad-output/implementation-artifacts/2-1-command-form-generation-and-field-type-inference.md#Task 4 — Command Fluxor (6 action records + reducer with CorrelationId guard)]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes/critical-decisions-read-first-do-not-revisit.md#Decision D28 — hand-written generic service + emitted per-command typed subscriber]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes/critical-decisions-read-first-do-not-revisit.md#Decision D35 — idempotent + lazy subscriber registry]
- [Source: _bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes/critical-decisions-read-first-do-not-revisit.md#Decision D38 — ConcurrentDictionary<CorrelationId, PendingEntry> with bounded eviction]
- [Source: _bmad-output/implementation-artifacts/deferred-work.md — known deferrals (IdempotentConfirmed enum, HFC1008 analyzer)]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L03 — tenant/user isolation fail-closed]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L04 — generated name collision detection]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L05 — hand-written service + emitted per-type wiring]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L06 — defense-in-depth budget ≤25 decisions for feature story]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L07 — test count matrix scoring]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L09 — ADR rejected-alternatives discipline]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L10 — deferrals name story not epic]
- [Source: _bmad-output/process-notes/story-creation-lessons.md#L11 — cheat sheet for large stories]
- [Source: memory/feedback_no_manual_validation.md — automated E2E preference]
- [Source: memory/feedback_cross_story_contracts.md — explicit cross-story contracts (ADR-017 mirrors ADR-016)]
- [Source: memory/feedback_tenant_isolation_fail_closed.md — inherited context for D13 scoping decision]
- [Source: memory/feedback_defense_budget.md — ≤25 binding decisions (we're at 18 ✓)]
- [Source: NUlid 1.7.4 — https://github.com/RobThree/NUlid (MIT license; stable since 2019)]

### Project Structure Notes

- Alignment with architecture blueprint (architecture.md §852):
  - New `Shell/Services/Lifecycle/` folder — consistent with existing `Shell/Services/DerivedValues/` grouping (one service family per folder)
  - New `Contracts/Lifecycle/` additions — existing folder (`CommandLifecycleState.cs`, `ICommandLifecycleTracker.cs`) extended, no new folder
  - NUlid package in Shell only — preserves Contracts dependency-free invariant (architecture.md §1144)
  - No new test project — existing `Hexalith.FrontComposer.Shell.Tests` + `Hexalith.FrontComposer.SourceTools.Tests` absorb new tests
  - No new sample project — Counter.Web gets a dev-mode diagnostic panel in-place
- Detected conflicts or variances:
  - **`ResetToIdleAction` signature change** — Story 2-1 emitted `ResetToIdleAction()` (parameterless); Story 2-3 extends to `ResetToIdleAction(string CorrelationId)`. Blast radius: form emitter dispatch site + 1 reducer + 7 existing tests that construct the action. Each can be updated mechanically. Documented as the expected delta in Task 7.
  - **`StubCommandService` constructor expansion** — adds required `IUlidFactory` param. No external adopter uses `StubCommandService` directly; internal update only. 7 existing tests updated (Task 3.4).
  - **`ICommandLifecycleTracker` coexistence** — the existing provisional `ICommandLifecycleTracker` interface (`Contracts/Lifecycle/`) remains but is now superseded by `ILifecycleStateService` for all new code. Removing `ICommandLifecycleTracker` outright is out of scope for Story 2-3 (no consumer depends on it in-repo; it is a published public API in Contracts so removal requires a SemVer-major bump). Add a Known Gaps entry: "Remove `ICommandLifecycleTracker` — superseded by `ILifecycleStateService` — Story 9-x (deprecation cycle)."

---
