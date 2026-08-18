---
title: 'Story 9.4: Converge terminal outcomes on one producer boundary'
type: 'feature'
created: '2026-08-12'
status: 'done'
baseline_commit: '677b5e287bc0e60afc3fc6f27737ed8cb9697db8'
review_loop_iteration: 8
review_cap_override: 'Human-authorized on 2026-08-14 for iteration 8 and the completion loops required by this decision.'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-context.md'
  - '{project-root}/_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Generated callbacks bypass `IPendingCommandOutcomeResolver`, race registration, and polling discards materiality. Ambient row identity remains untrustworthy.

**Approach:** Publish Story 9.3's target API, capture before dispatch, associate the accepted ULID, and route every adapter through one bounded resolver.

## Boundaries & Constraints

**Always:** Add public `CommandTargetAttribute(Type, CommandTargetResolutionMode, CommandTargetChangeKind)` (`ViewKey`/`ExpectedStatus` properties), `ICommandTargetIdentityProvider<T>.ResolveAsync(T, CancellationToken)`, and `CommandTargetIdentity(ViewKey, EntityKey, PriorStatus, ExpectedStatus)` in Contracts. Add `ICommandServiceWithLifecycleObservations.DispatchAsync<T>(T, Action<CommandLifecycleObservation>?, CancellationToken)`, where the observation carries state, MessageId, materiality, and time; retain the old interface. Generated descriptors stay pure/equatable. `CommandTargetSnapshot` adds declared projection/change kind and framework-owned scope/time; target failure affects FC-NIP only. Terminal truth is first-wins and never rolls back: a lifecycle-transition failure retains bounded idempotent convergence work, retried from stored truth without transport re-query or indicator republication. Duplicate observations may trigger convergence but never change the terminal outcome. Indicator publication/non-publication is decided at most once per MessageId.

**Ask First:** Different public signatures, EventStore/dependency/package changes, or expansion into scope rendering (9.5), per-row first-wins (9.6), key preallocation (9.9), or live acceptance (9.8).

**Never:** Infer identity/materiality from undeclared rows, properties, routes, diffs, nudges, `AggregateId`, payloads, or lifecycle text; publish ineligible outcomes; edit `obj/`, submodules, fingerprints, or `CHANGELOG.md`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible terminal | Valid target + accepted ULID + confirmed/idempotent `Material` | Apply terminal state; publish once for declared view/entity | Duplicate never republishes or extends TTL |
| Early callback | Terminal observation precedes accepted association | Buffer then replay once after registration | Capacity equals `MaxPendingCommandEntries`; FIFO overflow is logged and suppressed |
| Ineligible | Bad provider/ID, timeout, conflict, delete, rejection, `NoOp`/`Unknown` | Lifecycle/dispatch continue; no indicator | Redacted diagnostic; no fallback |
| Invalid time/owner | Too old, >5s future, scope/circuit/pre-accept owner ends | Suppress and discard | Small future skew clamps; accepted navigation survives |
| Lifecycle delivery failure | First terminal is committed but `ILifecycleStateService.Transition` fails or does not converge | Preserve terminal truth and the one-time indicator decision; retry stored terminal state locally | Bounded FIFO/deadline; no status query, outcome mutation, or publication retry |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Contracts/{Attributes,Rendering,Lifecycle}/` -- public declaration/provider/observation types; XML-doc all.
- `src/Hexalith.FrontComposer.SourceTools/{Parsing,Transforms,Emitters}/` -- pure target IR; canonical-view validation; monotonic acknowledgement; fail-safe admission cleanup; provider isolation and bounded execution.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/` -- exception-safe association/replay, first-wins state, at-most-once indicator decisions, and bounded lifecycle convergence before transport polling.
- `src/Hexalith.FrontComposer.Shell/{Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs,Services/StubCommandService.cs,Extensions/ServiceCollectionExtensions.cs,Options/FcShellOptions.cs}` -- typed materiality, DI, and `CommandTargetResolutionTimeoutMs` (500ms).
- `samples/Counter/**/*Command.cs` -- explicit `SameAsSource` + `Update` migration without invalidating the manually collected IDE-parity fixture.
- `tests/Hexalith.FrontComposer.{SourceTools,Shell}.Tests/`, `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` -- declarations, snapshots, replay, matrix, composition, and governance.
- `docs/reference/components/datagrid.md`, `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- adopter truth and final identifier-inventory reseal.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Contracts/`, `src/Hexalith.FrontComposer.SourceTools/`, `samples/` -- retain the approved APIs/declarations; reject noncanonical fixed view keys; make acknowledgement monotonic; contain cleanup/setup failures while preserving the hard dispatch deadline.
- [x] `src/Hexalith.FrontComposer.Shell/State/PendingCommands/`, EventStore/stub/options/DI files above -- retain first terminal truth, add bounded lifecycle convergence and one-time indicator decisions, reconcile post-commit association exceptions, validate delegated MessageIds, and make typed/legacy callbacks fail loudly or close safely.
- [x] `tests/`, `docs/`, snapshots, analyzer ledger -- prove every matrix/review case, preserve IDE/fingerprint evidence, refresh generated snapshots intentionally, and reseal identifier inventory last.

**Acceptance Criteria:**
- Given any terminal adapter, when observed, then only the resolver mutates terminal state/publishes and early input replays once.
- Given eligible material evidence, when in bounds, then the target publishes once; every other case stays lifecycle-correct and indicator-free.
- Given generated commands, when governance runs, then valid declarations compile, invalid declarations fail closed with HFC1005, undeclared commands remain lifecycle-neutral, and direct `ResolveTerminal`/forbidden inference is absent.
- Given a committed terminal whose lifecycle delivery fails, when a duplicate arrives or polling runs, then stored terminal truth converges idempotently without querying status transport, altering the outcome, or repeating the indicator decision.

### Review Findings

- [x] [Review][Patch] Check `!string.IsNullOrWhiteSpace(result.MessageId)` before marking accepted result in `LegacyLifecycleObservationCommandServiceAdapter` [`src/Hexalith.FrontComposer.Shell/Services/LegacyLifecycleObservationCommandServiceAdapter.cs:203`]
- [x] [Review][Patch] Add XML documentation comments to `PendingCommandOutcomeRegistrationMarker` class and properties [`src/Hexalith.FrontComposer.Shell/Extensions/PendingCommandOutcomeRegistrationMarker.cs:5`]
- [x] [Review][Patch] Evicting entries from _indicatorDecisions permits duplicate indicator publication on subsequent observations [`src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:378-388`]
- [x] [Review][Patch] Deferring _indicatorDecisions.Add below eligibility guards prevents recording non-publication decisions for non-material/unconfirmed outcomes [`src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:359-371`]
- [x] [Review][Patch] Premature _indicatorDecisions.Add before timestamp resolution and indicator state Add execution [`src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:383`]
- [x] [Review][Patch] Missing unit test coverage for _indicatorDecisions capacity bounding and eviction [`tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandOutcomeResolverTests.cs`]
- [x] [Review][Patch] Missing unit test coverage for AggregateException unwrapping in CommandServiceExtensions.IsFatal [`tests/Hexalith.FrontComposer.Contracts.Tests/Communication/CommandServiceExtensionsTests.cs`]
- [x] [Review][Patch] Missing executable commitlint boundary coverage for 200-character line length limits [`commitlint.config.mjs:7-9`]


## Spec Change Log

- **Review iteration 1 (2026-08-13):** The verification-gap review found that the solution-level `dotnet test` command contradicted the repository baseline requiring test projects to run individually. The blocking lane below now enumerates every test project separately. Avoid the known-bad state where analyzer-governance tests require product-test serialization solely to keep a solution-wide test host alive. **KEEP:** the approved Contracts shapes; pure/equatable target IR; pre-dispatch capture and post-accept association; resolver-only terminal mutation/publication; bounded first-terminal replay and fail-closed eligibility; typed EventStore/stub/auth adapters; sample migration; docs, generated snapshots, IDE hashes, analyzer-ledger reseal, and matrix/governance coverage.
- **Review iteration 2 (2026-08-13):** The adversarial review proved that setting `EnableFrontComposerPackageValidation=true` on `dotnet build` does not execute SDK package ApiCompat; the blocking lane now runs the repository's eight-package release packer. The same review found that mutating `samples/IdeParityCounter` invalidates manually collected IDE evidence that this story cannot truthfully recollect, so that fixture and its manifests remain unchanged while the Counter sample carries the explicit migration. **KEEP:** the approved Contracts shapes; pure/equatable target IR; pre-dispatch capture and post-accept association; resolver-only terminal mutation/publication; bounded first-terminal replay and fail-closed eligibility; typed EventStore/stub/auth adapters; Counter sample migration; docs, generated snapshots, analyzer-ledger reseal, and matrix/governance coverage.
- **Review iteration 3 (2026-08-13, human decision):** The review found that the proposed undeclared-command diagnostic conflicted with retained manual IDE evidence and required a producer-fingerprint reseal forbidden by the frozen boundary. The human chose policy B: remove that diagnostic, its registry/docs/tests and producer reseal from Story 9.4; preserve explicit HFC1005 target validation and the byte-identical IDE fixture/evidence. The human also explicitly approved `IPendingCommandOutcomeCoordinator.BufferBeforeAccepted(ownerId, observation)` so pre-accept callbacks use an owner-scoped buffer-only boundary. **KEEP:** the package-compatible public API; generated target isolation and hard deadline; owner-scoped pre-accept buffering; resolver-only terminal mutation/publication; bounded FIFO/first-terminal behavior; typed adapters; Counter-only sample migration; and executable matrix coverage.
- **Review iteration 4 (2026-08-13):** The verification review found that repository-specific project context and blocking CI define solution-level testing, overriding the shared baseline's per-project default. The blocking lane is restored to the authoritative solution command; the individual loop remains supplemental evidence. Functional findings in the same pass require owner-specific discard, polling-to-Fluxor convergence, scope-safe accepted association, lazy provider construction, frozen provider dispatch, and DI/lifecycle hardening. The human explicitly approved the additive `IPendingCommandOutcomeCoordinator.DiscardBufferedByOwner(string ownerId)` boundary while retaining message-based cleanup for compatibility. **KEEP:** the human-approved diagnostic exclusion and preserved IDE/fingerprint artifacts, plus every prior package/API and producer-boundary invariant.
- **Review iteration 5 (2026-08-13):** The final immutable-diff review found a projection/view-key integrity gap, pre-9.4 SourceTools constructor ABI drift, correlation-wide lifecycle feedback suppression, init-only provider-clone rejection, process-wide provider-worker poisoning, and resolver-only DI activation/split risks. Corrections restore the old public constructors, validate targets against the declared projection's canonical view, suppress only the exact forwarded lifecycle state, scope the provider worker per circuit, adapt legacy resolver-only registrations, and pin framework polling to the coordinator alias. **KEEP:** all human decisions and public coordinator additions from iterations 3–4.
- **Review iteration 6 (2026-08-13):** The corrected-diff review found that the legacy resolver adapter delegated terminal application but skipped shared eligible-indicator publication, and that a purge specimen had been mislabeled as an update. The adapter now applies the same publication guard after delegation, and the purge command remains undeclared until a delete-capable provider exists. **KEEP:** all iteration-5 compatibility, canonical-target, lifecycle, bulkhead, and DI corrections.
- **Review iteration 7 (2026-08-13):** The next immutable-diff review found that legacy lifecycle clock/observer failures could escape into an otherwise accepted dispatch, and that generated form disposal reset an already-associated command to Idle before polling or reconnect terminal convergence. The legacy adapter now isolates observation delivery while completing terminal bookkeeping, and accepted disposal preserves resolver-owned lifecycle state. **KEEP:** all iteration-6 publication, purge, compatibility, target-integrity, and human-approved boundary decisions.
- **Review iteration 8 (2026-08-14, human decision and cap override):** Review proved that a thrown lifecycle transition committed terminal truth but removed the entry from polling, leaving no convergence route. The human authorized continuation beyond the review cap and approved immutable first-terminal truth plus bounded, idempotent local lifecycle convergence without transport re-query or indicator retry. The re-derivation also closes monotonic acknowledgement, post-commit association recovery, admission cleanup, typed-callback loss, canonical-view validation, delegated MessageId trust, legacy callback lifetime/clock, and provider-worker setup gaps. Avoid rollback/reopening, mutable duplicate outcomes, repeated publication decisions, or weakening the provider deadline. **KEEP:** every prior public API/ABI, human decision, IDE/fingerprint constraint, resolver-only mutation/publication boundary, and package-compatible adapter path.

## Design Notes

Keys trim once and compare Ordinal; snapshot equality excludes `CapturedAt`. Missing observation time uses shell time; ≤5s future skew clamps, larger skew suppresses; maximum age reuses `MaxPendingCommandPollingDurationMs`. The first terminal observation wins even if non-material. Buffer and lifecycle-convergence capacity reuse `MaxPendingCommandEntries` with FIFO eviction; convergence uses the polling duration and per-tick budget, runs before transport polling, and derives only from the stored terminal entry. A transition counts as converged only when lifecycle state and MessageId match after the attempt. Pre-accept observations are keyed by canonical MessageId, local correlation owner, and captured scope; general resolution never buffers an unknown ID. Scope/circuit and pre-accept disposal clear state; accepted navigation does not. Undeclared commands remain lifecycle-neutral and indicator-ineligible without a migration diagnostic. Provider clones isolate adopter resolution and become dispatch input when available; if the first clone itself misses the hard deadline, target resolution fails closed but the original command dispatches, as required by the FC-NIP-only failure rule.

## Verification

**Commands:**
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:EnableFrontComposerPackageValidation=true && dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -p:EnableFrontComposerPackageValidation=true` -- clean package/sample compilation.
- `python3 eng/pack_release_packages.py --version 4.0.0-ci.story9-4 --output /tmp/frontcomposer-story-9-4-release-pack` -- all eight release packages and symbols build, pack, and pass SDK package/API compatibility against the configured baseline.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --no-restore --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- the repository-authoritative solution-level blocking lane passes.
- `pwsh ./eng/validate-docs.ps1 && (cd tests/e2e && npm run test:fc-nip && npm run test:fc-diagnostics)` -- documentation and browserless contract guards pass.
- `git diff --check` -- no whitespace errors.

## Suggested Review Order

**Terminal truth and convergence**

- First-terminal storage anchors bounded, idempotent lifecycle convergence without transport replay.
  [`PendingCommandStateService.cs:141`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs#L141)

- Per-tick snapshotting prevents one failing transition from starving status transport.
  [`PendingCommandStateService.cs:252`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs#L252)

- Polling spends its shared budget on local convergence before transport queries.
  [`PendingCommandPollingCoordinator.cs:34`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs#L34)

**Resolution and publication**

- Association reconciles committed registrations and replays buffered terminal evidence safely.
  [`PendingCommandOutcomeResolver.cs:141`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs#L141)

- Message-scoped decisions make publication and suppression equally at-most-once.
  [`PendingCommandOutcomeResolver.cs:354`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs#L354)

**Generated and adapter boundaries**

- Fixed destination declarations now require exact non-null runtime identity evidence.
  [`CommandFormEmitter.cs:711`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs#L711)

- Generated cleanup survives coordinator and dispatcher failures without retaining ownership.
  [`CommandFormEmitter.cs:920`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs#L920)

- Parser rejects fixed view keys that disagree with the declared projection.
  [`CommandParser.cs:399`](../../src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs#L399)

- Legacy callbacks match accepted identity, detach cancellation, and expire on a bounded timer.
  [`LegacyLifecycleObservationCommandServiceAdapter.cs:32`](../../src/Hexalith.FrontComposer.Shell/Services/LegacyLifecycleObservationCommandServiceAdapter.cs#L32)

- Lifecycle bridge subscription claims close synchronous replay and partial-construction races.
  [`CommandLifecycleBridgeEmitter.cs:130`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/CommandLifecycleBridgeEmitter.cs#L130)

**Composition safeguards**

- Admission-gate topology remains exactly one non-keyed scoped circuit boundary.
  [`ServiceCollectionExtensions.cs:587`](../../src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs#L587)

**Focused proof**

- Capacity and saturated-clock tests pin bounded convergence edge behavior.
  [`PendingCommandStateServiceTests.cs:224`](../../tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandStateServiceTests.cs#L224)

- Fairness and expiry tests prove convergence cannot starve status transport.
  [`PendingCommandPollingCoordinatorTests.cs:137`](../../tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingCoordinatorTests.cs#L137)

- Mismatched pre-accept terminals cannot suppress the canonical retained callback.
  [`LegacyLifecycleObservationCommandServiceAdapterTests.cs:112`](../../tests/Hexalith.FrontComposer.Shell.Tests/Services/LegacyLifecycleObservationCommandServiceAdapterTests.cs#L112)

- Runtime reducer proof preserves Syncing when acknowledgement arrives afterward.
  [`CommandTargetGeneratedFormTests.cs:256`](../../tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandTargetGeneratedFormTests.cs#L256)

- Global-namespace execution proves canonical projection identity without textual ambiguity.
  [`GeneratorDriverTests.cs:383`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs#L383)

## Suggested Review Order

**Fair and scope-safe convergence**

- Reserve transport capacity before bounded lifecycle convergence consumes the shared polling budget.
  [`PendingCommandPollingCoordinator.cs:34`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs#L34)

- Clear prior-scope state atomically, then deliver fail-safe lifecycle notifications outside the gate.
  [`PendingCommandStateService.cs:633`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs#L633)

**Accepted producer boundaries**

- Preserve accepted EventStore truth when observation timestamps or callbacks fail nonfatally.
  [`EventStoreCommandClient.cs:180`](../../src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs#L180)

- Validate accepted identities and isolate every retained-callback cleanup operation independently.
  [`LegacyLifecycleObservationCommandServiceAdapter.cs:32`](../../src/Hexalith.FrontComposer.Shell/Services/LegacyLifecycleObservationCommandServiceAdapter.cs#L32)

- Treat nonfatal timer setup failure as closed observation lifetime, never failed acceptance.
  [`LegacyLifecycleObservationCommandServiceAdapter.cs:243`](../../src/Hexalith.FrontComposer.Shell/Services/LegacyLifecycleObservationCommandServiceAdapter.cs#L243)

- Continue generated stale-subscription cleanup while preserving fatal exception propagation.
  [`CommandLifecycleBridgeEmitter.cs:212`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/CommandLifecycleBridgeEmitter.cs#L212)

**Focused proof**

- Minimum-budget saturation proves lifecycle convergence cannot starve status transport.
  [`PendingCommandPollingCoordinatorTests.cs:183`](../../tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingCoordinatorTests.cs#L183)

- Reentrant scope transition proves new-scope registrations survive the atomic clear.
  [`PendingCommandStateServiceTests.cs:433`](../../tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandStateServiceTests.cs#L433)

- Delete-provider coverage pins target association without eligible indicator publication.
  [`CommandTargetGeneratedFormTests.cs:380`](../../tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandTargetGeneratedFormTests.cs#L380)

- Browser contract pins reservation, convergence, polling, and transport ordering.
  [`fc-nip-row-identity-contract.spec.ts:268`](../../tests/e2e/specs/fc-nip-row-identity-contract.spec.ts#L268)

## Suggested Review Order

**At-most-once indicator decisions**

- Resolve records the MessageId decision under the lock, then publishes after release.
  [`PendingCommandOutcomeResolver.cs:231`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs#L231)

- Publication and suppression both stamp the same MessageId so duplicates cannot retry Add.
  [`PendingCommandOutcomeResolver.cs:419`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs#L419)

- Scope loss still drops buffered terminals, but never forgets an indicator decision.
  [`PendingCommandOutcomeResolver.cs:482`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs#L482)

- Indicator Add runs only after `_gate` is released, so subscriber render cannot re-enter.
  [`PendingCommandOutcomeResolver.cs:430`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs#L430)

**Accepted dispatch and convergence**

- Transport Accepted keeps Syncing when association fails, so polling can still converge.
  [`CommandFormEmitter.cs:1277`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs#L1277)

- Lifecycle convergence treats cancellation as abort, not a failed retry.
  [`PendingCommandStateService.cs:505`](../../src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs#L505)

- Pre-accept overflow stays FIFO-capped and now logs the suppressed oldest terminal.
  [`LegacyLifecycleObservationCommandServiceAdapter.cs:166`](../../src/Hexalith.FrontComposer.Shell/Services/LegacyLifecycleObservationCommandServiceAdapter.cs#L166)

**Generated cleanup isolation**

- Form cleanup unwraps AggregateException so fatal inners still propagate.
  [`CommandFormEmitter.cs:940`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs#L940)

- Bridge Dispose isolates non-fatal subscription faults and continues the rest.
  [`CommandLifecycleBridgeEmitter.cs:255`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/CommandLifecycleBridgeEmitter.cs#L255)

**Focused proof**

- Capacity overflow retains the oldest MessageId and never republishes it.
  [`PendingCommandOutcomeResolverTests.cs:554`](../../tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandOutcomeResolverTests.cs#L554)

- Browser contract forbids `_indicatorDecisions.Remove` and pins `RecordIndicatorDecision`.
  [`fc-nip-row-identity-contract.spec.ts:322`](../../tests/e2e/specs/fc-nip-row-identity-contract.spec.ts#L322)
