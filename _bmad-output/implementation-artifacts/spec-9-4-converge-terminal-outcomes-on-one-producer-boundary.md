---
title: 'Story 9.4: Converge terminal outcomes on one producer boundary'
type: 'feature'
created: '2026-08-12'
status: 'in-progress'
baseline_commit: '677b5e287bc0e60afc3fc6f27737ed8cb9697db8'
review_loop_iteration: 7
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

**Always:** Add public `CommandTargetAttribute(Type, CommandTargetResolutionMode, CommandTargetChangeKind)` (`ViewKey`/`ExpectedStatus` properties), `ICommandTargetIdentityProvider<T>.ResolveAsync(T, CancellationToken)`, and `CommandTargetIdentity(ViewKey, EntityKey, PriorStatus, ExpectedStatus)` in Contracts. Add `ICommandServiceWithLifecycleObservations.DispatchAsync<T>(T, Action<CommandLifecycleObservation>?, CancellationToken)`, where the observation carries state, MessageId, materiality, and time; retain the old interface. Generated descriptors stay pure/equatable. `CommandTargetSnapshot` adds declared projection/change kind and framework-owned scope/time; target failure affects FC-NIP only.

**Ask First:** Different public signatures, EventStore/dependency/package changes, or expansion into scope rendering (9.5), per-row first-wins (9.6), key preallocation (9.9), or live acceptance (9.8).

**Never:** Infer identity/materiality from undeclared rows, properties, routes, diffs, nudges, `AggregateId`, payloads, or lifecycle text; publish ineligible outcomes; edit `obj/`, submodules, fingerprints, or `CHANGELOG.md`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible terminal | Valid target + accepted ULID + confirmed/idempotent `Material` | Apply terminal state; publish once for declared view/entity | Duplicate never republishes or extends TTL |
| Early callback | Terminal observation precedes accepted association | Buffer then replay once after registration | Capacity equals `MaxPendingCommandEntries`; FIFO overflow is logged and suppressed |
| Ineligible | Bad provider/ID, timeout, conflict, delete, rejection, `NoOp`/`Unknown` | Lifecycle/dispatch continue; no indicator | Redacted diagnostic; no fallback |
| Invalid time/owner | Too old, >5s future, scope/circuit/pre-accept owner ends | Suppress and discard | Small future skew clamps; accepted navigation survives |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Contracts/{Attributes,Rendering,Lifecycle}/` -- public declaration/provider/observation types; XML-doc all.
- `src/Hexalith.FrontComposer.SourceTools/{Parsing,Transforms,Emitters}/` -- pure target IR, generated resolution/descriptor, resolver-only flow, provider isolation, and bounded execution.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/` -- snapshot association, `AssociateAccepted`, replay, and eligibility.
- `src/Hexalith.FrontComposer.Shell/{Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs,Services/StubCommandService.cs,Extensions/ServiceCollectionExtensions.cs,Options/FcShellOptions.cs}` -- typed materiality, DI, and `CommandTargetResolutionTimeoutMs` (500ms).
- `samples/Counter/**/*Command.cs` -- explicit `SameAsSource` + `Update` migration without invalidating the manually collected IDE-parity fixture.
- `tests/Hexalith.FrontComposer.{SourceTools,Shell}.Tests/`, `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` -- declarations, snapshots, replay, matrix, composition, and governance.
- `docs/reference/components/datagrid.md`, `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- adopter truth and final identifier-inventory reseal.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Contracts/`, `src/Hexalith.FrontComposer.SourceTools/`, `samples/` -- implement/document the approved APIs, pure descriptor pipeline, and declarations.
- [x] `src/Hexalith.FrontComposer.Shell/State/PendingCommands/`, EventStore/stub/options/DI files above -- replace ambient/direct mutation with capture, association, typed adapters, replay, and fail-closed eligibility.
- [x] `tests/`, `docs/`, analyzer ledger -- prove the matrix, refresh generated snapshots, preserve forbidden-source and IDE-evidence guards, document migration, and reseal last.

**Acceptance Criteria:**
- Given any terminal adapter, when observed, then only the resolver mutates terminal state/publishes and early input replays once.
- Given eligible material evidence, when in bounds, then the target publishes once; every other case stays lifecycle-correct and indicator-free.
- Given generated commands, when governance runs, then valid declarations compile, invalid declarations fail closed with HFC1005, undeclared commands remain lifecycle-neutral, and direct `ResolveTerminal`/forbidden inference is absent.

## Spec Change Log

- **Review iteration 1 (2026-08-13):** The verification-gap review found that the solution-level `dotnet test` command contradicted the repository baseline requiring test projects to run individually. The blocking lane below now enumerates every test project separately. Avoid the known-bad state where analyzer-governance tests require product-test serialization solely to keep a solution-wide test host alive. **KEEP:** the approved Contracts shapes; pure/equatable target IR; pre-dispatch capture and post-accept association; resolver-only terminal mutation/publication; bounded first-terminal replay and fail-closed eligibility; typed EventStore/stub/auth adapters; sample migration; docs, generated snapshots, IDE hashes, analyzer-ledger reseal, and matrix/governance coverage.
- **Review iteration 2 (2026-08-13):** The adversarial review proved that setting `EnableFrontComposerPackageValidation=true` on `dotnet build` does not execute SDK package ApiCompat; the blocking lane now runs the repository's eight-package release packer. The same review found that mutating `samples/IdeParityCounter` invalidates manually collected IDE evidence that this story cannot truthfully recollect, so that fixture and its manifests remain unchanged while the Counter sample carries the explicit migration. **KEEP:** the approved Contracts shapes; pure/equatable target IR; pre-dispatch capture and post-accept association; resolver-only terminal mutation/publication; bounded first-terminal replay and fail-closed eligibility; typed EventStore/stub/auth adapters; Counter sample migration; docs, generated snapshots, analyzer-ledger reseal, and matrix/governance coverage.
- **Review iteration 3 (2026-08-13, human decision):** The review found that the proposed undeclared-command diagnostic conflicted with retained manual IDE evidence and required a producer-fingerprint reseal forbidden by the frozen boundary. The human chose policy B: remove that diagnostic, its registry/docs/tests and producer reseal from Story 9.4; preserve explicit HFC1005 target validation and the byte-identical IDE fixture/evidence. The human also explicitly approved `IPendingCommandOutcomeCoordinator.BufferBeforeAccepted(ownerId, observation)` so pre-accept callbacks use an owner-scoped buffer-only boundary. **KEEP:** the package-compatible public API; generated target isolation and hard deadline; owner-scoped pre-accept buffering; resolver-only terminal mutation/publication; bounded FIFO/first-terminal behavior; typed adapters; Counter-only sample migration; and executable matrix coverage.
- **Review iteration 4 (2026-08-13):** The verification review found that repository-specific project context and blocking CI define solution-level testing, overriding the shared baseline's per-project default. The blocking lane is restored to the authoritative solution command; the individual loop remains supplemental evidence. Functional findings in the same pass require owner-specific discard, polling-to-Fluxor convergence, scope-safe accepted association, lazy provider construction, frozen provider dispatch, and DI/lifecycle hardening. The human explicitly approved the additive `IPendingCommandOutcomeCoordinator.DiscardBufferedByOwner(string ownerId)` boundary while retaining message-based cleanup for compatibility. **KEEP:** the human-approved diagnostic exclusion and preserved IDE/fingerprint artifacts, plus every prior package/API and producer-boundary invariant.
- **Review iteration 5 (2026-08-13):** The final immutable-diff review found a projection/view-key integrity gap, pre-9.4 SourceTools constructor ABI drift, correlation-wide lifecycle feedback suppression, init-only provider-clone rejection, process-wide provider-worker poisoning, and resolver-only DI activation/split risks. Corrections restore the old public constructors, validate targets against the declared projection's canonical view, suppress only the exact forwarded lifecycle state, scope the provider worker per circuit, adapt legacy resolver-only registrations, and pin framework polling to the coordinator alias. **KEEP:** all human decisions and public coordinator additions from iterations 3–4.
- **Review iteration 6 (2026-08-13):** The corrected-diff review found that the legacy resolver adapter delegated terminal application but skipped shared eligible-indicator publication, and that a purge specimen had been mislabeled as an update. The adapter now applies the same publication guard after delegation, and the purge command remains undeclared until a delete-capable provider exists. **KEEP:** all iteration-5 compatibility, canonical-target, lifecycle, bulkhead, and DI corrections.
- **Review iteration 7 (2026-08-13):** The next immutable-diff review found that legacy lifecycle clock/observer failures could escape into an otherwise accepted dispatch, and that generated form disposal reset an already-associated command to Idle before polling or reconnect terminal convergence. The legacy adapter now isolates observation delivery while completing terminal bookkeeping, and accepted disposal preserves resolver-owned lifecycle state. **KEEP:** all iteration-6 publication, purge, compatibility, target-integrity, and human-approved boundary decisions.

## Design Notes

Keys trim once and compare Ordinal; snapshot equality excludes `CapturedAt`. Missing observation time uses shell time; ≤5s future skew clamps, larger skew suppresses; maximum age reuses `MaxPendingCommandPollingDurationMs`. The first terminal observation wins even if non-material. Buffer capacity reuses `MaxPendingCommandEntries` with FIFO eviction. Pre-accept observations are keyed by canonical MessageId, local correlation owner, and captured scope; general resolution never buffers an unknown ID. Scope/circuit and pre-accept disposal clear state; accepted navigation does not. Undeclared commands remain lifecycle-neutral and indicator-ineligible without a migration diagnostic.

## Verification

**Commands:**
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:EnableFrontComposerPackageValidation=true && dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -p:EnableFrontComposerPackageValidation=true` -- clean package/sample compilation.
- `python3 eng/pack_release_packages.py --version 4.0.0-ci.story9-4 --output /tmp/frontcomposer-story-9-4-release-pack` -- all eight release packages and symbols build, pack, and pass SDK package/API compatibility against the configured baseline.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --no-restore --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- the repository-authoritative solution-level blocking lane passes.
- `pwsh ./eng/validate-docs.ps1 && (cd tests/e2e && npm run test:fc-nip && npm run test:fc-diagnostics)` -- documentation and browserless contract guards pass.
- `git diff --check` -- no whitespace errors.
