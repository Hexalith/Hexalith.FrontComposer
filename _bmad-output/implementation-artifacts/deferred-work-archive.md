### DW-667: Follow-up review still recommended for 11-2-projection-realtime-resilience after the review budget was exhausted
origin: review-budget-followup
source_spec: `spec-11-2-projection-realtime-resilience.md`
severity: low
reason: Review budget (3 cycles) was exhausted with the story finalized (status: done, verify green) while the review pass kept recommending an independent follow-up. The work was committed by bmad-loop run 20260706-075033-1582; this entry preserves the lingering follow-up recommendation for a deliberate later review.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: commit 9ad4312f performed the requested independent follow-up review and implemented its realtime, fallback, pending-driver, factory, and teardown findings; spec-dw-667-followup-review-projection-realtime-resilience.md records followup_review_recommended: false.

### DW-668: Follow-up review still recommended for 11-4-security-validation-hardening after the review budget was exhausted
origin: review-budget-followup
source_spec: `spec-11-4-security-validation-hardening.md`
severity: low
reason: Review budget (3 cycles) was exhausted with the story finalized (status: done, verify green) while the review pass kept recommending an independent follow-up. The work was committed by bmad-loop run 20260706-191144-ea78; this entry preserves the lingering follow-up recommendation for a deliberate later review.
status: done 2026-08-28
archived: 2026-09-18
resolution: resolved by sweep bundle dw-dw-668-followup
resolution-undo: a384ba36c0f4d3e4f1a6ac701a4f87e221fd0b122814a65da769de121260277e 2026-08-28 7374617475733a206f70656e

### DW-671: FC-NIP contract guards duplicate roughly forty literal fragments and two full markdown tables across the C# and Playwright suites with no shared source of truth.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-3-define-explicit-command-target-identity.md (2026-08-12)"), 2026-08-27
location: AssertTableRows
source_spec: `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
reason: summary: FC-NIP contract guards duplicate roughly forty literal fragments and two full markdown tables across the C# and Playwright suites with no shared source of truth. evidence: `AssertTableRows` / `parseTableRows` demand cell-by-cell equality of the Immutable Target Snapshot and Complete Outcome Disposition Matrix, transcribed by hand into both languages; a single contract typo breaks both suites and the copies will drift. Pre-existing duplication pattern that Story 9.3 extended rather than introduced.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit d928f5f7 implements the shared FC-NIP manifest and its C# and TypeScript contract guards

### DW-672: FC-NIP guard file and class names still say "row identity" although both now primarily guard command target identity.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-3-define-explicit-command-target-identity.md (2026-08-12)"), 2026-08-27
location: tests/e2e/specs/fc-nip-row-identity-contract.spec.ts
source_spec: `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
reason: summary: FC-NIP guard file and class names still say "row identity" although both now primarily guard command target identity. evidence: `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` and `FcNipRowIdentityProducerContractTests` were retargeted in place, and the base contract header still reads "Story 9.1 - Confirm the FC-NIP row-identity producer contract". Renaming touches governance identifiers and the analyzer identifier inventory, so it belongs in an owned story.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit d928f5f7 renames the C# class and Playwright spec around command-target identity

### DW-673: Reconcile the dual dating of the FC-NIP base decision across the contract set.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-3-define-explicit-command-target-identity.md (2026-08-12)"), 2026-08-27
location: prd.md
source_spec: `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
reason: summary: Reconcile the dual dating of the FC-NIP base decision across the contract set. evidence: The base file is named and dated `2026-07-04`, but the successor contract, `prd.md`, and the Story 9.3 spec all cite "the 2026-07-05 row-context decision" — the `Decision update:` date inside the 07-04 file. No document explains the relationship, so the set appears to reference a contract that does not exist. Second-pass code review, 2026-08-12.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit d928f5f7 reconciles the 2026-07-04 record and 2026-07-05 approval/update chronology

### DW-674: Unify the clock-abstraction naming in the successor contract.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-3-define-explicit-command-target-identity.md (2026-08-12)"), 2026-08-27
location: CapturedAt
source_spec: `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
reason: summary: Unify the clock-abstraction naming in the successor contract. evidence: `CapturedAt` is sourced from "FrontComposer `TimeProvider`" while the `ObservedAt` fallback is "the Shell `TimeProvider`", with no statement that these name the same seam. Cosmetic but load-bearing for a contract whose whole point is that the two timestamps stay distinct. Second-pass code review, 2026-08-12.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit d928f5f7 names the common FrontComposer Shell TimeProvider seam

### DW-675: Specify redaction, log category/level, and a suppression-rate signal for the FC-NIP target-failure diagnostic.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-3-define-explicit-command-target-identity.md (2026-08-12)"), 2026-08-27
location: EntityKey
source_spec: `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
reason: summary: Specify redaction, log category/level, and a suppression-rate signal for the FC-NIP target-failure diagnostic. evidence: The contract emits a bounded diagnostic carrying `EntityKey`, `PriorStatus`, and `ExpectedStatus` — business data — with no redaction rule, and requires no suppression-rate observability. A fail-closed implementation suppressing 100% of indicators in production would be indistinguishable from a working one. Belongs with the Story 9.4 implementation. Second-pass code review, 2026-08-12.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit d928f5f7 adds redacted events 5912/5913 and the suppression-rate contract

### DW-676: Record the retirement path for the gap-evidence prohibitions pinned on `EventStorePendingCommandStatusQuery.cs`.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-3-define-explicit-command-target-identity.md (2026-08-12)"), 2026-08-27
location: EventStorePendingCommandStatusQuery.cs
source_spec: `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
reason: summary: Record the retirement path for the gap-evidence prohibitions pinned on `EventStorePendingCommandStatusQuery.cs`. evidence: The newly broadened `ShouldNotContain("EntityKey:")` makes the earlier `ShouldNotContain("EntityKey: status.AggregateId")` dead, and the broad form will block Story 9.4, which is expected to add target identity to that exact file. Nothing documents when or how these assertions retire. Owned by Story 9.4. Second-pass code review, 2026-08-12.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs:1

### DW-677: RESOLVED 2026-08-12 — implement the FC-NIP opt-in migration in Story 9.4 (build-time diagnostic plus sample migration).

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-3-define-explicit-command-target-identity.md (2026-08-12)"), 2026-08-27
location: samples/Counter
source_spec: `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
reason: summary: RESOLVED 2026-08-12 — implement the FC-NIP opt-in migration in Story 9.4 (build-time diagnostic plus sample migration). evidence: Decided at the Story 9.3 code-review decision gate. FC-NIP stays opt-in per command; the historical row cascade is NOT promoted into an implicit `SameAsSource` declaration, because that would reintroduce the ambient source-row placement the frozen Never-list forbids. Story 9.4 owns (a) a build-time SourceTools diagnostic when a command renders from a generated projection row without a target declaration — allocate the next free id in `FcDiagnosticIds`, `HFC1070` is currently highest — and (b) migrating `samples/Counter`, `samples/Counter.Specimens.Domain`, and `samples/IdeParityCounter` `[Command]` types to explicit declarations. Adopter release notes must state that fresh-row indicators now require a declaration. Recorded in the successor contract's "Migration From The Historical Row Cascade" section; remaining work is Story 9.4 implementation, not an open decision.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: samples/Counter/Counter.Specimens.Domain/PurgeSpecimenRecordCommand.cs:1

### DW-678: Decide whether one command may resolve more than one FC-NIP target.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-3-define-explicit-command-target-identity.md (2026-08-12)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
reason: summary: Decide whether one command may resolve more than one FC-NIP target. evidence: The successor contract states "One command resolves at most one target; multi-target commands require a separate decision", and the spec's frozen Ask First repeats it. No story owns that decision, so a command that materially changes several rows has no defined indicator disposition. Contract-declared deferral filed by the second-pass code review, 2026-08-12.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-680: Approve the public API shape for the command target-identity surface before Story 9.4 publishes it.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-3-define-explicit-command-target-identity.md (2026-08-12)"), 2026-08-27
location: PublicAPI.Sh
source_spec: `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
reason: summary: Approve the public API shape for the command target-identity surface before Story 9.4 publishes it. evidence: The successor contract defers "Any public API shape" to explicit human approval, but the declaration attribute is authored by adopters and `ICommandTargetIdentityProvider<TCommand>` is implemented by them, so both become public surface the moment Story 9.4 ships them. Requires a decision plus a `PublicAPI.Shipped.txt` baseline plan. Contract-declared deferral filed by the second-pass code review, 2026-08-12.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md:1

### DW-681: Re-pin the projection-nudge-seam prohibition in the synchronized-truth guard.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-3-define-explicit-command-target-identity.md (2026-08-12)"), 2026-08-27
location: _bmad-output/project-docs/architecture.md
source_spec: `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
reason: summary: Re-pin the projection-nudge-seam prohibition in the synchronized-truth guard. evidence: "Fresh-row indicators are not produced from the projection nudge seam" still exists in `_bmad-output/project-docs/architecture.md`, but the deleted `FcNipContractReferences_WhenAuthored_NameEpicNineOwnershipInDocs` was its only assertion and the replacement `SynchronizedTruth_WhenReviewed_ResolvesDecisionAndKeepsCompositionOpen` does not re-pin it. Second-pass code review, 2026-08-12.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/project-docs/architecture.md:1

### DW-682: Dual full-solution Release builds in `AnalyzerPolicy_ActivatedReleaseBuild_MatchesForcedRecommendedCandidate` can contend when Governance facts run in parallel.

origin: migrated from legacy ledger ("Deferred from: code review of spec-11-23-recommended-analyzer-repository-activation.md (2026-08-08)"), 2026-08-27
location: AnalyzerPolicy_ActivatedReleaseBuild_MatchesForcedRecommendedCandidate
source_spec: `_bmad-output/implementation-artifacts/spec-11-23-recommended-analyzer-repository-activation.md`
reason: summary: Dual full-solution Release builds in `AnalyzerPolicy_ActivatedReleaseBuild_MatchesForcedRecommendedCandidate` can contend when Governance facts run in parallel. evidence: The new parity fact runs two solution builds against shared outputs; sibling Governance rebuild gates already deferred the same contention class under Story 11.22.
status: done 2026-08-31
archived: 2026-09-18
resolution: resolved by sweep bundle dw-analyzer-governance-reliability
resolution-undo: e4596f494f9c9989040773d97a6329e9d10b7fd5193cf008204a784d3bd533f7 2026-08-31 7374617475733a206f70656e

### DW-683: `CommandFormEmitterTests` admission dispose ordering still anchors with `IndexOf("try")` / `IndexOf("finally")` after the submitted-log call site; a larger token containing those substrings could mis-order the assert (pre-existing; this chunk only reordered the greater-than checks).

origin: migrated from legacy ledger ("Deferred from: code review of spec-11-22-recommended-analyzer-test-and-sample-burn-down.md chunk 4 (2026-08-08)"), 2026-08-27
location: CommandFormEmitterTests
reason: `CommandFormEmitterTests` admission dispose ordering still anchors with `IndexOf("try")` / `IndexOf("finally")` after the submitted-log call site; a larger token containing those substrings could mis-order the assert (pre-existing; this chunk only reordered the greater-than checks).
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: commit 8b38d7bb replaces substring ordering with Roslyn syntax-node ownership assertions in tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs:648.

### DW-684: `BadgeCountServiceTests` / `NavigationEffectsLastActiveRouteTests` CA1859 helpers now return concrete `ServiceProvider` but call sites still do not dispose the built providers (same lifetime gap existed when typed as `IServiceProvider`).

origin: migrated from legacy ledger ("Deferred from: code review of spec-11-22-recommended-analyzer-test-and-sample-burn-down.md chunk 3 (2026-08-08)"), 2026-08-27
location: BadgeCountServiceTests
reason: `BadgeCountServiceTests` / `NavigationEffectsLastActiveRouteTests` CA1859 helpers now return concrete `ServiceProvider` but call sites still do not dispose the built providers (same lifetime gap existed when typed as `IServiceProvider`).
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: Commit 9c9ec260; tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs:64 and tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsLastActiveRouteTests.cs:104 now own and dispose their ServiceProvider instances.

### DW-685: `ETagCacheService.EnsurePersistedLruSeededAsync` re-checks `_disposed` after acquiring `_lruSeedGate` but Dispose can still land after that re-check and before/during `TrySeedPersistedLruAsync` / `_lruSeeded` write; residual race beyond the wait-gate fix from the 11.21 review closeout.

origin: migrated from legacy ledger ("Deferred from: code review of spec-11-22-recommended-analyzer-test-and-sample-burn-down.md chunk 2 (2026-08-08)"), 2026-08-27
location: ETagCacheService.EnsurePersistedLruSeededAsync
reason: `ETagCacheService.EnsurePersistedLruSeededAsync` re-checks `_disposed` after acquiring `_lruSeedGate` but Dispose can still land after that re-check and before/during `TrySeedPersistedLruAsync` / `_lruSeeded` write; residual race beyond the wait-gate fix from the 11.21 review closeout.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: commit 9ad4312f resolved the ETag seed/dispose race; src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:68 leaves the managed semaphore undisposed and ETagCacheServiceTests.cs:288 proves owner and waiter both settle.

### DW-686: `FrontComposerMcpLog` applies the CA1873 local-binding pattern on some paths only; sibling helpers can still pass category formatting straight into `Log*` — complete when the next MCP logging burn-down touches those sites.

origin: migrated from legacy ledger ("Deferred from: code review of spec-11-22-recommended-analyzer-test-and-sample-burn-down.md chunk 2 (2026-08-08)"), 2026-08-27
location: FrontComposerMcpLog
reason: `FrontComposerMcpLog` applies the CA1873 local-binding pattern on some paths only; sibling helpers can still pass category formatting straight into `Log*` — complete when the next MCP logging burn-down touches those sites.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: commit 8cabbf54 applies the deferred CA1873 local-binding pattern across src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs.

### DW-687: `RenderTreeSequenceRewriter.StartsArgumentList` now skips whitespace before `(` / the identifier but still ignores comments/trivia (e.g. `( /*x*/ ++seq)`), so the OrFail prefilter can miss some fail-safe leftovers.

origin: migrated from legacy ledger ("Deferred from: code review of spec-11-22-recommended-analyzer-test-and-sample-burn-down.md chunk 2 (2026-08-08)"), 2026-08-27
location: RenderTreeSequenceRewriter.StartsArgumentList
reason: `RenderTreeSequenceRewriter.StartsArgumentList` now skips whitespace before `(` / the identifier but still ignores comments/trivia (e.g. `( /*x*/ ++seq)`), so the OrFail prefilter can miss some fail-safe leftovers.
status: done 2026-09-06
archived: 2026-09-18
resolution: resolved by sweep bundle dw-source-generator-boundary-hardening
resolution-undo: bf3c48abeb67a658f5f9561c364f753c4213486b2740060223149ab0b51d6cd8 2026-09-06 7374617475733a206f70656e

### DW-688: `GeneratedLogMethodEmitter.ValidateArguments` rejects null/whitespace `methodName`/`eventName` but still accepts non-empty invalid C# identifiers that would fail to compile in generated output.

origin: migrated from legacy ledger ("Deferred from: code review of spec-11-22-recommended-analyzer-test-and-sample-burn-down.md chunk 2 (2026-08-08)"), 2026-08-27
location: GeneratedLogMethodEmitter.ValidateArguments
reason: `GeneratedLogMethodEmitter.ValidateArguments` rejects null/whitespace `methodName`/`eventName` but still accepts non-empty invalid C# identifiers that would fail to compile in generated output.
status: done 2026-09-06
archived: 2026-09-18
resolution: resolved by sweep bundle dw-source-generator-boundary-hardening
resolution-undo: bf3c48abeb67a658f5f9561c364f753c4213486b2740060223149ab0b51d6cd8 2026-09-06 7374617475733a206f70656e

### DW-689: Thirteen-project Recommended Governance rebuild gate (`AnalyzerPolicy_Story1122RecordedProjects_RemainRecommendedClean`) runs sequential 180s-bounded builds and slows every Governance lane; intentional executable gate — optimize later (caching, narrower trait, or shared binary log reuse).

origin: migrated from legacy ledger ("Deferred from: code review of spec-11-22-recommended-analyzer-test-and-sample-burn-down.md (2026-08-08)"), 2026-08-27
location: AnalyzerPolicy_Story1122RecordedProjects_RemainRecommendedClean
reason: Thirteen-project Recommended Governance rebuild gate (`AnalyzerPolicy_Story1122RecordedProjects_RemainRecommendedClean`) runs sequential 180s-bounded builds and slows every Governance lane; intentional executable gate — optimize later (caching, narrower trait, or shared binary log reuse).
status: done 2026-08-31
archived: 2026-09-18
resolution: resolved by sweep bundle dw-analyzer-governance-reliability
resolution-undo: e4596f494f9c9989040773d97a6329e9d10b7fd5193cf008204a784d3bd533f7 2026-08-31 7374617475733a206f70656e

### DW-690: `RunDotnetResultAsync` kills on timeout then still awaits stdout/stderr tasks that may cancel uncleanly; pre-existing helper shared by several governance proofs — harden when the next timeout-related flake appears.

origin: migrated from legacy ledger ("Deferred from: code review of spec-11-22-recommended-analyzer-test-and-sample-burn-down.md (2026-08-08)"), 2026-08-27
location: RunDotnetResultAsync
reason: `RunDotnetResultAsync` kills on timeout then still awaits stdout/stderr tasks that may cancel uncleanly; pre-existing helper shared by several governance proofs — harden when the next timeout-related flake appears.
status: done 2026-08-31
archived: 2026-09-18
resolution: resolved by sweep bundle dw-analyzer-governance-reliability
resolution-undo: e4596f494f9c9989040773d97a6329e9d10b7fd5193cf008204a784d3bd533f7 2026-08-31 7374617475733a206f70656e

### DW-691: Shell.Tests ASP0006 `NoWarn` comment claims exactly 17 hand-authored fixture sites but nothing asserts that count; debt growth under the same control would stay silent until Story 11.22.

origin: migrated from legacy ledger ("Deferred from: code review of 11-21-recommended-analyzer-product-and-generator-burndown.md chunk 4b (2026-08-08)"), 2026-08-27
location: NoWarn
reason: ~~Shell.Tests ASP0006 `NoWarn` comment claims exactly 17 hand-authored fixture sites but nothing asserts that count; debt growth under the same control would stay silent until Story 11.22.~~ Resolved by Story 11.22: project `NoWarn` removed, sites fixed to literals, and Governance re-executes the ASP0006 negative-control probe.
status: done 2026-08-08
archived: 2026-09-18
resolution: Resolved by Story 11.22: project `NoWarn` removed, sites fixed to literals, and Governance re-executes the ASP0006 negative-control probe

### DW-692: Diagnostic EventId/level/exception inventory (73 / 56 Information / 17 Debug / 20 exceptions) is hard-coded independently in `SecurityLoggingGovernanceTests` and `FrontComposerDiagnosticLogTests`, so the two guards can drift.

origin: migrated from legacy ledger ("Deferred from: code review of 11-21-recommended-analyzer-product-and-generator-burndown.md chunk 4b (2026-08-08)"), 2026-08-27
location: SecurityLoggingGovernanceTests
reason: Diagnostic EventId/level/exception inventory (73 / 56 Information / 17 Debug / 20 exceptions) is hard-coded independently in `SecurityLoggingGovernanceTests` and `FrontComposerDiagnosticLogTests`, so the two guards can drift.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: commit 8cabbf54 introduces shared FrontComposerDiagnosticLogInventoryAssertions used by both logging-governance suites.

### DW-693: `McpLifecycleStoreDisposalTests` only exercises `TryReadSnapshot` after dispose; `AcknowledgeAsync` / `TryRecordObservedTransition` share `ThrowIfDisposed` but are not pinned.

origin: migrated from legacy ledger ("Deferred from: code review of 11-21-recommended-analyzer-product-and-generator-burndown.md chunk 4b (2026-08-08)"), 2026-08-27
location: McpLifecycleStoreDisposalTests
reason: `McpLifecycleStoreDisposalTests` only exercises `TryReadSnapshot` after dispose; `AcknowledgeAsync` / `TryRecordObservedTransition` share `ThrowIfDisposed` but are not pinned.
status: done 2026-09-06
archived: 2026-09-18
resolution: resolved by sweep bundle dw-mcp-lifecycle-disposal-coverage
resolution-undo: 63e0e03ab983b5d73f2c9ee9b02946a52f6b486418a55809a80a3f7006c12e51 2026-09-06 7374617475733a206f70656e

### DW-694: Badge/Shortcut helpers gained `IsEnabled` stubs for Information-level HFC21xx asserts; other Shell `Substitute.For<ILogger<T>>()` factories that omit `IsEnabled` remain a latent false-negative risk now that wrappers short-circuit.

origin: migrated from legacy ledger ("Deferred from: code review of 11-21-recommended-analyzer-product-and-generator-burndown.md chunk 4b (2026-08-08)"), 2026-08-27
location: IsEnabled
reason: Badge/Shortcut helpers gained `IsEnabled` stubs for Information-level HFC21xx asserts; other Shell `Substitute.For<ILogger<T>>()` factories that omit `IsEnabled` remain a latent false-negative risk now that wrappers short-circuit.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: commit 8cabbf54 replaces Shell logger substitutes with EnabledLoggerSubstitute and adds focused enabled-level regression coverage.

### DW-695: Canonical fingerprint golden pins document-level `Metadata` order and empty nested maps but never varies `Collections` order or non-empty `EnumValues`.

origin: migrated from legacy ledger ("Deferred from: code review of 11-21-recommended-analyzer-product-and-generator-burndown.md chunk 4b (2026-08-08)"), 2026-08-27
location: Metadata
reason: Canonical fingerprint golden pins document-level `Metadata` order and empty nested maps but never varies `Collections` order or non-empty `EnumValues`.
status: done 2026-09-06
archived: 2026-09-18
resolution: resolved by sweep bundle dw-canonical-fingerprint-vector-coverage
resolution-undo: 8c9ea2f3d13723111a0a8fec1236d149c43fd650c3069b4a9b45f8eb9f918262 2026-09-06 7374617475733a206f70656e

### DW-696: `SchemaMigrationDeltaPathTruncationTests` drives only `RemovedField` through `TruncatePath`, hard-codes `MaxPathLength = 256`, and never compares against a Substring-based oracle or additional surrogate cut shapes.

origin: migrated from legacy ledger ("Deferred from: code review of 11-21-recommended-analyzer-product-and-generator-burndown.md chunk 4a (2026-08-08)"), 2026-08-27
location: SchemaMigrationDeltaPathTruncationTests
reason: `SchemaMigrationDeltaPathTruncationTests` drives only `RemovedField` through `TruncatePath`, hard-codes `MaxPathLength = 256`, and never compares against a Substring-based oracle or additional surrogate cut shapes.
status: done 2026-09-06
archived: 2026-09-18
resolution: resolved by sweep bundle dw-source-generator-regression-coverage
resolution-undo: f1bc87dff38c0e8658a9a139676fcf5e81e444b417b6e298ec46b26c1b69e0fe 2026-09-06 7374617475733a206f70656e

### DW-697: `GeneratedRenderTreeText.MaskSequenceArguments` has no dedicated unit tests (including the claimed `seq++` leave-unmasked behavior).

origin: migrated from legacy ledger ("Deferred from: code review of 11-21-recommended-analyzer-product-and-generator-burndown.md chunk 4a (2026-08-08)"), 2026-08-27
location: GeneratedRenderTreeText.MaskSequenceArguments
reason: `GeneratedRenderTreeText.MaskSequenceArguments` has no dedicated unit tests (including the claimed `seq++` leave-unmasked behavior).
status: done 2026-09-06
archived: 2026-09-18
resolution: resolved by sweep bundle dw-source-generator-regression-coverage
resolution-undo: f1bc87dff38c0e8658a9a139676fcf5e81e444b417b6e298ec46b26c1b69e0fe 2026-09-06 7374617475733a206f70656e

### DW-698: `RazorEmitterBadgeColumnTests` / `RazorEmitterExpandInRowTests` never call `ShouldUseLiteralRenderTreeSequences`; literal sequencing is only indirectly pinned via snapshots / negative `int seq = 800` checks.

origin: migrated from legacy ledger ("Deferred from: code review of 11-21-recommended-analyzer-product-and-generator-burndown.md chunk 4a (2026-08-08)"), 2026-08-27
location: RazorEmitterBadgeColumnTests
reason: `RazorEmitterBadgeColumnTests` / `RazorEmitterExpandInRowTests` never call `ShouldUseLiteralRenderTreeSequences`; literal sequencing is only indirectly pinned via snapshots / negative `int seq = 800` checks.
status: done 2026-09-06
archived: 2026-09-18
resolution: resolved by sweep bundle dw-source-generator-regression-coverage
resolution-undo: f1bc87dff38c0e8658a9a139676fcf5e81e444b417b6e298ec46b26c1b69e0fe 2026-09-06 7374617475733a206f70656e

### DW-699: `PackagedAnalyzerConsumerTests` builds the temp consumer Release-only; rewriter unit tests already stress DEBUG vs Release parse safety, but the packaged Recommended gate does not.

origin: migrated from legacy ledger ("Deferred from: code review of 11-21-recommended-analyzer-product-and-generator-burndown.md chunk 4a (2026-08-08)"), 2026-08-27
location: PackagedAnalyzerConsumerTests
reason: `PackagedAnalyzerConsumerTests` builds the temp consumer Release-only; rewriter unit tests already stress DEBUG vs Release parse safety, but the packaged Recommended gate does not.
status: done 2026-08-31
archived: 2026-09-18
resolution: resolved by sweep bundle dw-analyzer-governance-reliability
resolution-undo: e4596f494f9c9989040773d97a6329e9d10b7fd5193cf008204a784d3bd533f7 2026-08-31 7374617475733a206f70656e

### DW-700: `RazorEmitter.Truncate` uses `AsSpan(0, maxLength - 1)` (CA1845); `maxLength < 1` on a non-empty value still throws. Sole production call site passes `30`; same failure shape existed with `Substring`.

origin: migrated from legacy ledger ("Deferred from: code review of 11-21-recommended-analyzer-product-and-generator-burndown.md chunk 3 (2026-08-08)"), 2026-08-27
location: RazorEmitter.Truncate
reason: `RazorEmitter.Truncate` uses `AsSpan(0, maxLength - 1)` (CA1845); `maxLength < 1` on a non-empty value still throws. Sole production call site passes `30`; same failure shape existed with `Substring`.
status: done 2026-09-06
archived: 2026-09-18
resolution: resolved by sweep bundle dw-source-generator-boundary-hardening
resolution-undo: bf3c48abeb67a658f5f9561c364f753c4213486b2740060223149ab0b51d6cd8 2026-09-06 7374617475733a206f70656e

### DW-701: RazorEmitter grid `DisposeAsync` / non-grid `Dispose` call `SuppressFinalize` (CA1816) but have no `_disposed` early-return, unlike the form emitter. Teardown is mostly naturally idempotent (null refs / catch); repeat dispose remains weaker than the form path.

origin: migrated from legacy ledger ("Deferred from: code review of 11-21-recommended-analyzer-product-and-generator-burndown.md chunk 3 (2026-08-08)"), 2026-08-27
location: DisposeAsync
reason: RazorEmitter grid `DisposeAsync` / non-grid `Dispose` call `SuppressFinalize` (CA1816) but have no `_disposed` early-return, unlike the form emitter. Teardown is mostly naturally idempotent (null refs / catch); repeat dispose remains weaker than the form path.
status: done 2026-09-06
archived: 2026-09-18
resolution: resolved by sweep bundle dw-source-generator-boundary-hardening
resolution-undo: bf3c48abeb67a658f5f9561c364f753c4213486b2740060223149ab0b51d6cd8 2026-09-06 7374617475733a206f70656e

### DW-706: Identifier inventory algorithm hashes every underscore-containing C# token under `tests/` (including locals/discards), so routine non-CA1707 test edits force ledger reseals; narrow the sealed token set only under an explicit follow-up that preserves fail-closed CA1707 scope drift detection.

origin: migrated from legacy ledger ("Deferred from: code review of 11-20-recommended-analyzer-policy-and-exception-ledger.md (2026-08-08)"), 2026-08-27
location: tests/**
reason: Identifier inventory algorithm hashes every underscore-containing C# token under `tests/**` (including locals/discards), so routine non-CA1707 test edits force ledger reseals; narrow the sealed token set only under an explicit follow-up that preserves fail-closed CA1707 scope drift detection.
status: done 2026-08-31
archived: 2026-09-18
resolution: resolved by sweep bundle dw-analyzer-governance-reliability
resolution-undo: e4596f494f9c9989040773d97a6329e9d10b7fd5193cf008204a784d3bd533f7 2026-08-31 7374617475733a206f70656e

### DW-707: Ledger `warningControls` for `TreatWarningsAsErrors` encode the boolean MSBuild value in `diagnosticIds` (`["true"]` / `["false"]`); schema clarity can improve later without changing the current governance parity keys.

origin: migrated from legacy ledger ("Deferred from: code review of 11-20-recommended-analyzer-policy-and-exception-ledger.md (2026-08-08)"), 2026-08-27
location: TreatWarningsAsErrors
reason: Ledger `warningControls` for `TreatWarningsAsErrors` encode the boolean MSBuild value in `diagnosticIds` (`["true"]` / `["false"]`); schema clarity can improve later without changing the current governance parity keys.
status: done 2026-08-31
archived: 2026-09-18
resolution: resolved by sweep bundle dw-analyzer-governance-reliability
resolution-undo: e4596f494f9c9989040773d97a6329e9d10b7fd5193cf008204a784d3bd533f7 2026-08-31 7374617475733a206f70656e

### DW-708: `Hexalith.FrontComposer.SourceTools.Tests.Docs.FcDocComponentDocumentationContractTests.EveryComponentPageLinksAtLeastOnePublishedAccessibilityDiagnostic` for `docs/reference/components/settings.md`.

origin: migrated from legacy ledger ("Broad Suite Pre-Existing Failures (2026-07-03)"), 2026-08-27
location: docs/reference/components/settings.md
reason: `Hexalith.FrontComposer.SourceTools.Tests.Docs.FcDocComponentDocumentationContractTests.EveryComponentPageLinksAtLeastOnePublishedAccessibilityDiagnostic` for `docs/reference/components/settings.md`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: docs/reference/components/settings.md:1

### DW-709: `Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.HexalithDependencyMode_DefaultsToProjectReferencesForDebugAndPackagesForRelease`.

origin: migrated from legacy ledger ("Broad Suite Pre-Existing Failures (2026-07-03)"), 2026-08-27
location: Hexalith.FrontComposer.Sh
reason: `Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.HexalithDependencyMode_DefaultsToProjectReferencesForDebugAndPackagesForRelease`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-8-retro-follow-through.md:41

### DW-710: `Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.PackageInventory_IsExplicitLockstepAndReviewable`.

origin: migrated from legacy ledger ("Broad Suite Pre-Existing Failures (2026-07-03)"), 2026-08-27
location: Hexalith.FrontComposer.Sh
reason: `Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.PackageInventory_IsExplicitLockstepAndReviewable`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/BoundaryTests.cs:25

### DW-711: `Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.ReleaseWorkflow_AddsSbomSigningAttestationAndManifestGatesAfterBlockingTests`.

origin: migrated from legacy ledger ("Broad Suite Pre-Existing Failures (2026-07-03)"), 2026-08-27
location: Hexalith.FrontComposer.Sh
reason: `Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.ReleaseWorkflow_AddsSbomSigningAttestationAndManifestGatesAfterBlockingTests`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/BoundaryTests.cs:25

### DW-712: `Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.NightlyBenchmarkWorkflow_UsesEmbeddedPromptContractAndReadOnlyEvidence`.

origin: migrated from legacy ledger ("Broad Suite Pre-Existing Failures (2026-07-03)"), 2026-08-27
location: Hexalith.FrontComposer.Sh
reason: `Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.NightlyBenchmarkWorkflow_UsesEmbeddedPromptContractAndReadOnlyEvidence`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-8-retro-follow-through.md:41

### DW-713: `Hexalith.FrontComposer.Shell.Tests.Governance.Story12_4_RedPhaseDefTests.Story12_4_Def14_AttestBuildProvenanceStep_IsWiredInReleaseWorkflow`.

origin: migrated from legacy ledger ("Broad Suite Pre-Existing Failures (2026-07-03)"), 2026-08-27
location: Hexalith.FrontComposer.Sh
reason: `Hexalith.FrontComposer.Shell.Tests.Governance.Story12_4_RedPhaseDefTests.Story12_4_Def14_AttestBuildProvenanceStep_IsWiredInReleaseWorkflow`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-8-retro-follow-through.md:41

### DW-734: Example block payload duplicates the title rather than showing a real emitted message [all 106 stubs] — chunk-C synthesizer limitation. Real `What/Expected/Got/Fix/DocsLink` example authoring belongs in the Story 9-5 prose pass per `messageTemplatePolicy`. Sources: blind. Reconciliation: Row: DW-0003; Rejected with rationale 2026-05-11; Evidence: Story 11.2 prioritized HFC0001 and HFC1601 example authoring per AC26; full-corpus example rewrite is beyond Story 11.2 budget and requires a Product/Architecture decision on per-diagnostic example sourcing (Story 9-5 prose pass).

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — chunk C (2026-05-10)"), 2026-08-27
location: What/Expected/Got/Fix/DocsLink
reason: **DEF-9-4-C3 — Example block payload duplicates the title rather than showing a real emitted message** [all 106 stubs] — chunk-C synthesizer limitation. Real `What/Expected/Got/Fix/DocsLink` example authoring belongs in the Story 9-5 prose pass per `messageTemplatePolicy`. Sources: blind. Reconciliation: Row: DW-0003; Rejected with rationale 2026-05-11; Evidence: Story 11.2 prioritized HFC0001 and HFC1601 example authoring per AC26; full-corpus example rewrite is beyond Story 11.2 budget and requires a Product/Architecture decision on per-diagnostic example sourcing (Story 9-5 prose pass).
status: done 2026-08-28
archived: 2026-09-18
decision: 2026-08-28 Close as accepted — Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.
resolution: closed by human decision: Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.
decision: 2026-08-28 Close as accepted — Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.

### DW-735: `docsSlug` relative vs canonical help-link absolute (`hexalith.github.io`) [all 106 stubs] — host migration would require 106-file edit. Architectural choice: derive body link from registry top-level `canonicalHelpLinkFormat` or document hardcoded host. Story 9-5 / docs-host strategy. Sources: blind. Reconciliation: Row: DW-0004; Resolved 2026-05-11; Evidence: DiagnosticDescriptors.CanonicalHelpLinkFormat plus registry canonicalHelpLinkFormat validation.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — chunk C (2026-05-10)"), 2026-08-27
location: hexalith.github.io
reason: **DEF-9-4-C4 — `docsSlug` relative vs canonical help-link absolute (`hexalith.github.io`)** [all 106 stubs] — host migration would require 106-file edit. Architectural choice: derive body link from registry top-level `canonicalHelpLinkFormat` or document hardcoded host. Story 9-5 / docs-host strategy. Sources: blind. Reconciliation: Row: DW-0004; Resolved 2026-05-11; Evidence: DiagnosticDescriptors.CanonicalHelpLinkFormat plus registry canonicalHelpLinkFormat validation.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: hexalith.github.io: DiagnosticDescriptors.CanonicalHelpLinkFormat plus registry canonicalHelpLinkFormat validation.
decision: 2026-08-27 Implement change — Implement the behavior requested by DW-735, update affected contracts and consumers, and add focused regression evidence.

### DW-736: Mass-applied `introducedIn: 0.1.0` for all stubs even where registry post-dates 0.1.0 [all 106 stubs] — Story 9-2 migration tooling and Story 9-1 drift may rely on accurate `introducedIn` for "what's new since v X" semantics. Re-derive per-entry from registry. Authoring + data accuracy task. Sources: blind. Reconciliation: Row: DW-0005; Resolved 2026-05-11; Evidence: registry/stub introducedIn parity remains enforced by DiagnosticRegistryTests and docs validation.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — chunk C (2026-05-10)"), 2026-08-27
location: n/a
reason: **DEF-9-4-C5 — Mass-applied `introducedIn: 0.1.0` for all stubs even where registry post-dates 0.1.0** [all 106 stubs] — Story 9-2 migration tooling and Story 9-1 drift may rely on accurate `introducedIn` for "what's new since v X" semantics. Re-derive per-entry from registry. Authoring + data accuracy task. Sources: blind. Reconciliation: Row: DW-0005; Resolved 2026-05-11; Evidence: registry/stub introducedIn parity remains enforced by DiagnosticRegistryTests and docs validation.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: n/a: registry/stub introducedIn parity remains enforced by DiagnosticRegistryTests and docs validation.

### DW-737: HFC1601 sub-allocation (1500-id gap) and Shell range non-contiguous sub-bands [registry + README] — HFC1601 lives in a 530-id gap inside 1000–1999; Shell stubs occupy two clusters HFC2004–2019 and HFC2100–2121. Sub-band convention not documented. Governance work. Sources: blind. Reconciliation: Row: DW-0006; Resolved 2026-05-11; Evidence: structured externalBoundaries and allowedExceptions.crossPackageRange for HFC1601 in diagnostic-registry.json.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — chunk C (2026-05-10)"), 2026-08-27
location: diagnostic-registry.json
reason: **DEF-9-4-C6 — HFC1601 sub-allocation (1500-id gap) and Shell range non-contiguous sub-bands** [registry + README] — HFC1601 lives in a 530-id gap inside 1000–1999; Shell stubs occupy two clusters HFC2004–2019 and HFC2100–2121. Sub-band convention not documented. Governance work. Sources: blind. Reconciliation: Row: DW-0006; Resolved 2026-05-11; Evidence: structured externalBoundaries and allowedExceptions.crossPackageRange for HFC1601 in diagnostic-registry.json.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: diagnostic-registry.json: structured externalBoundaries and allowedExceptions.crossPackageRange for HFC1601 in diagnostic-registry.json.

### DW-738: README `schemaVersion: 1.0` lock claim but stub front-matter has no schema-version field [`docs/diagnostics/README.md`, all 106 stubs] — fail-closed schema discipline cannot be enforced at the stub layer as currently shaped. Adding stub-side `schemaVersion` is a registry-format change. Sources: blind. Reconciliation: Row: DW-0007; Rejected with rationale 2026-05-11; Evidence: README keeps stub front matter bounded and registry schemaVersion remains the single fail-closed schema contract; adding a per-stub schemaVersion field is a registry-format change beyond Story 11.2 budget.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — chunk C (2026-05-10)"), 2026-08-27
location: docs/diagnostics/README.md
reason: **DEF-9-4-C7 — README `schemaVersion: 1.0` lock claim but stub front-matter has no schema-version field** [`docs/diagnostics/README.md`, all 106 stubs] — fail-closed schema discipline cannot be enforced at the stub layer as currently shaped. Adding stub-side `schemaVersion` is a registry-format change. Sources: blind. Reconciliation: Row: DW-0007; Rejected with rationale 2026-05-11; Evidence: README keeps stub front matter bounded and registry schemaVersion remains the single fail-closed schema contract; adding a per-stub schemaVersion field is a registry-format change beyond Story 11.2 budget.
status: done 2026-08-28
archived: 2026-09-18
decision: 2026-08-28 Close as accepted — Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.
resolution: closed by human decision: Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.
decision: 2026-08-28 Close as accepted — Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.

### DW-739: `storyOwner` is a free-form slug with no controlled vocabulary [all stubs] — after chunk-C P1 normalises `storyOwner` against registry, a follow-up enforcement could compare values against `sprint-status.yaml` story keys. Sources: blind. Reconciliation: Row: DW-0008; Rejected with rationale 2026-05-11; Evidence: storyOwner parity remains registry-backed; controlled-vocabulary expansion is beyond Story 11.2 budget and requires a Product/Architecture decision on cross-file vocabulary enforcement.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — chunk C (2026-05-10)"), 2026-08-27
location: sprint-status.yaml
reason: **DEF-9-4-C8 — `storyOwner` is a free-form slug with no controlled vocabulary** [all stubs] — after chunk-C P1 normalises `storyOwner` against registry, a follow-up enforcement could compare values against `sprint-status.yaml` story keys. Sources: blind. Reconciliation: Row: DW-0008; Rejected with rationale 2026-05-11; Evidence: storyOwner parity remains registry-backed; controlled-vocabulary expansion is beyond Story 11.2 budget and requires a Product/Architecture decision on cross-file vocabulary enforcement.
status: done 2026-08-28
archived: 2026-09-18
decision: 2026-08-28 Close as accepted — Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.
resolution: closed by human decision: Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.
decision: 2026-08-28 Close as accepted — Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.

### DW-740: `samples/*.json` not deeply audited for forbidden tokens [`docs/diagnostics/samples/*.json`] — README requires samples avoid timestamps, absolute paths, machine names, SDK banners, live feed URLs. Add a separate scan / fixture-schema test. Chunk B/C fixture-schema work; partly addressed in chunk-A patch P10 (registry-drift-report path redaction). Sources: edge. Reconciliation: Row: DW-0009; Resolved 2026-05-11; Evidence: DriftSampleReports_AreNormalizedAndCommitted validates findings shape and forbidden evidence tokens.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — chunk C (2026-05-10)"), 2026-08-27
location: samples/*.json
reason: **DEF-9-4-C9 — `samples/*.json` not deeply audited for forbidden tokens** [`docs/diagnostics/samples/*.json`] — README requires samples avoid timestamps, absolute paths, machine names, SDK banners, live feed URLs. Add a separate scan / fixture-schema test. Chunk B/C fixture-schema work; partly addressed in chunk-A patch P10 (registry-drift-report path redaction). Sources: edge. Reconciliation: Row: DW-0009; Resolved 2026-05-11; Evidence: DriftSampleReports_AreNormalizedAndCommitted validates findings shape and forbidden evidence tokens.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: samples/*.json: DriftSampleReports_AreNormalizedAndCommitted validates findings shape and forbidden evidence tokens.

### DW-741: HFCM* migration analyzer rows are unregistered and unstubbed [`src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` + missing registry/stubs] — 6 IDs (`HFCM0000`, `HFCM0001`, `HFCM0002`, `HFCM0004`, `HFCM9001`, `HFCM9002`) live in the SourceTools unshipped file with no registry row or docs stub. Owner: Story 9-2 (migration tooling). Story 9-2 to add a registry-exempt declaration and tighten the orphan check to `^HFC[0-9]` when it registers the first concrete HFCM* migration entry. Related: DEF-9-4-A14 (RS2002 retained for HFCM* CLI-emitted migration ids). Sources: edge. Reconciliation: Row: DW-0010; Resolved 2026-05-11; Evidence: docs/diagnostics/migration-findings.json owns HFCM rows and SourceTools RS2002 suppression was removed.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — chunk C (2026-05-10)"), 2026-08-27
location: src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md
reason: **DEF-9-4-C10 — HFCM* migration analyzer rows are unregistered and unstubbed** [`src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md` + missing registry/stubs] — 6 IDs (`HFCM0000`, `HFCM0001`, `HFCM0002`, `HFCM0004`, `HFCM9001`, `HFCM9002`) live in the SourceTools unshipped file with no registry row or docs stub. **Owner: Story 9-2** (migration tooling). Story 9-2 to add a registry-exempt declaration and tighten the orphan check to `^HFC[0-9]` when it registers the first concrete HFCM* migration entry. Related: DEF-9-4-A14 (RS2002 retained for HFCM* CLI-emitted migration ids). Sources: edge. Reconciliation: Row: DW-0010; Resolved 2026-05-11; Evidence: docs/diagnostics/migration-findings.json owns HFCM rows and SourceTools RS2002 suppression was removed.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md: docs/diagnostics/migration-findings.json owns HFCM rows and SourceTools RS2002 suppression was removed.

### DW-743: `CONTRIBUTING.md` `Debugger.Launch()` guidance has no enforcement [`CONTRIBUTING.md:12-14`] — Documentation-only safeguard against generator-host `Debugger.Launch()` leaks; no analyzer, pre-commit hook, or CI grep gates removal. Add a CI grep gate later if a leak ships. Sources: blind. Reconciliation: Row: DW-0012; Resolved 2026-05-12; Evidence: CONTRIBUTING.md and ProductionSource_ForbidsUnconditionalDebuggerLaunch; User-visible behavior: production source forbids `Debugger.Launch()` prompts.

origin: migrated from legacy ledger ("Deferred from: code review of 9-3-ide-parity-and-developer-experience (2026-05-09)"), 2026-08-27
location: CONTRIBUTING.md
reason: **DEF-9-3-2 — `CONTRIBUTING.md` `Debugger.Launch()` guidance has no enforcement** [`CONTRIBUTING.md:12-14`] — Documentation-only safeguard against generator-host `Debugger.Launch()` leaks; no analyzer, pre-commit hook, or CI grep gates removal. Add a CI grep gate later if a leak ships. Sources: blind. Reconciliation: Row: DW-0012; Resolved 2026-05-12; Evidence: CONTRIBUTING.md and ProductionSource_ForbidsUnconditionalDebuggerLaunch; User-visible behavior: production source forbids `Debugger.Launch()` prompts.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: CONTRIBUTING.md: CONTRIBUTING.md and ProductionSource_ForbidsUnconditionalDebuggerLaunch

### DW-744: PowerShell file-write race for `$OutPath` [`jobs/ide-parity-version-revalidation.ps1`] — Not an issue under serial release-gate execution; revisit if the script is invoked in parallel. Sources: edge. Reconciliation: Row: DW-0013; Resolved 2026-05-12; Evidence: jobs/ide-parity-version-revalidation.ps1 and docs/reference/ide-parity.md; User-visible behavior: dry-run artifacts are repository-bounded and written through same-directory temp files.

origin: migrated from legacy ledger ("Deferred from: code review of 9-3-ide-parity-and-developer-experience (2026-05-09)"), 2026-08-27
location: ide-parity-version-revalidation.ps1
reason: **DEF-9-3-3 — PowerShell file-write race for `$OutPath`** [`jobs/ide-parity-version-revalidation.ps1`] — Not an issue under serial release-gate execution; revisit if the script is invoked in parallel. Sources: edge. Reconciliation: Row: DW-0013; Resolved 2026-05-12; Evidence: jobs/ide-parity-version-revalidation.ps1 and docs/reference/ide-parity.md; User-visible behavior: dry-run artifacts are repository-bounded and written through same-directory temp files.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: ide-parity-version-revalidation.ps1: jobs/ide-parity-version-revalidation.ps1 and docs/reference/ide-parity.md

### DW-745: `RepositoryRoot` walk via symlinked `AppContext.BaseDirectory` [`tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs:2048-2059`] — Walks ancestors looking for `.sln`; if `AppContext.BaseDirectory` is symlinked or extracted from an artifact bundle, walk may overshoot or follow a wrong link. Not a realistic CI path today. Sources: edge. Reconciliation: Row: DW-0014; Resolved 2026-05-12; Evidence: IdeParityRepositoryRoot now resolves link targets before ancestor walk; User-visible behavior: IDE parity tests anchor to the real checkout when the test base directory is symlinked.

origin: migrated from legacy ledger ("Deferred from: code review of 9-3-ide-parity-and-developer-experience (2026-05-09)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs:2048-2059
reason: **DEF-9-3-4 — `RepositoryRoot` walk via symlinked `AppContext.BaseDirectory`** [`tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs:2048-2059`] — Walks ancestors looking for `.sln`; if `AppContext.BaseDirectory` is symlinked or extracted from an artifact bundle, walk may overshoot or follow a wrong link. Not a realistic CI path today. Sources: edge. Reconciliation: Row: DW-0014; Resolved 2026-05-12; Evidence: IdeParityRepositoryRoot now resolves link targets before ancestor walk; User-visible behavior: IDE parity tests anchor to the real checkout when the test base directory is symlinked.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs:2048-2059: IdeParityRepositoryRoot now resolves link targets before ancestor walk

### DW-746: Manifest extra-field tolerance / duplicate JSON keys [`tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs`] — `System.Text.Json` silently keeps the last value for duplicate keys and ignores unknown fields; tampered manifests with duplicate keys would pass schema tests. Would require explicit allowlist or strict-schema parser. Low practical risk. Sources: edge. Reconciliation: Row: DW-0015; Resolved 2026-05-12; Evidence: StrictJsonValidation_RejectsDuplicateKeysUnknownFieldsAndTrailingCommas; User-visible behavior: matrix and evidence manifests reject duplicate keys and unknown fields.

origin: migrated from legacy ledger ("Deferred from: code review of 9-3-ide-parity-and-developer-experience (2026-05-09)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs
reason: **DEF-9-3-5 — Manifest extra-field tolerance / duplicate JSON keys** [`tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs`] — `System.Text.Json` silently keeps the last value for duplicate keys and ignores unknown fields; tampered manifests with duplicate keys would pass schema tests. Would require explicit allowlist or strict-schema parser. Low practical risk. Sources: edge. Reconciliation: Row: DW-0015; Resolved 2026-05-12; Evidence: StrictJsonValidation_RejectsDuplicateKeysUnknownFieldsAndTrailingCommas; User-visible behavior: matrix and evidence manifests reject duplicate keys and unknown fields.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs: StrictJsonValidation_RejectsDuplicateKeysUnknownFieldsAndTrailingCommas

### DW-747: UTF-8 BOM / trailing-comma read tolerance for manifest JSON [`tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs`] — A manifest with a trailing comma raises `JsonException` without a friendly hint; current strict behavior is the correct fail-closed default for tamper detection. Sources: edge. Reconciliation: Row: DW-0016; Resolved 2026-05-12; Evidence: StrictJsonValidation_RejectsDuplicateKeysUnknownFieldsAndTrailingCommas and docs/reference/ide-parity.md; User-visible behavior: trailing commas stay fail-closed through strict parsing.

origin: migrated from legacy ledger ("Deferred from: code review of 9-3-ide-parity-and-developer-experience (2026-05-09)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs
reason: **DEF-9-3-6 — UTF-8 BOM / trailing-comma read tolerance for manifest JSON** [`tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs`] — A manifest with a trailing comma raises `JsonException` without a friendly hint; current strict behavior is the correct fail-closed default for tamper detection. Sources: edge. Reconciliation: Row: DW-0016; Resolved 2026-05-12; Evidence: StrictJsonValidation_RejectsDuplicateKeysUnknownFieldsAndTrailingCommas and docs/reference/ide-parity.md; User-visible behavior: trailing commas stay fail-closed through strict parsing.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs: StrictJsonValidation_RejectsDuplicateKeysUnknownFieldsAndTrailingCommas and docs/reference/ide-parity.md

### DW-748: README "JSON path schema" framing in change-log overstated [`src/Hexalith.FrontComposer.Cli/README.md`] — README adds path-relativity + glob notes (P-16/P-22) but no actual schema, exit-code table, or field listing. Story 9-5 owns final docs; Story 9-2 wording in change-log inflates scope. Sources: auditor. Reconciliation: Row: DW-0017; Resolved 2026-05-12; Evidence: src/Hexalith.FrontComposer.Cli/README.md and docs/reference/cli.md; User-visible behavior: README/reference list exit codes, JSON fields, path relativity, and diff limits.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — third pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/README.md
reason: **DEF-9-2-18 — README "JSON path schema" framing in change-log overstated** [`src/Hexalith.FrontComposer.Cli/README.md`] — README adds path-relativity + glob notes (P-16/P-22) but no actual schema, exit-code table, or field listing. Story 9-5 owns final docs; Story 9-2 wording in change-log inflates scope. Sources: auditor. Reconciliation: Row: DW-0017; Resolved 2026-05-12; Evidence: src/Hexalith.FrontComposer.Cli/README.md and docs/reference/cli.md; User-visible behavior: README/reference list exit codes, JSON fields, path relativity, and diff limits.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/README.md: src/Hexalith.FrontComposer.Cli/README.md and docs/reference/cli.md

### DW-752: `SourceFile.DetectEncoding` strict UTF-8 fallback breaks legitimate Latin-1 files [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`] — Intentional fail-closed per second-pass P-encoding; flagged for completeness. Sources: blind. Reconciliation: Row: DW-0021; Resolved 2026-05-12; Evidence: SourceFile_ReadAsyncRejectsInvalidUtf8 and CLI docs; User-visible behavior: unsupported encodings fail closed with sanitized guidance.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — third pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs
reason: **DEF-9-2-22 — `SourceFile.DetectEncoding` strict UTF-8 fallback breaks legitimate Latin-1 files** [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`] — Intentional fail-closed per second-pass P-encoding; flagged for completeness. Sources: blind. Reconciliation: Row: DW-0021; Resolved 2026-05-12; Evidence: SourceFile_ReadAsyncRejectsInvalidUtf8 and CLI docs; User-visible behavior: unsupported encodings fail closed with sanitized guidance.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs: SourceFile_ReadAsyncRejectsInvalidUtf8 and CLI docs

### DW-754: `MigrationCatalog.BuildEdges` throws from a static field initializer [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:87-108`] — Defensive guard; only one edge currently and a duplicate would also fail tests at first instantiation. Sources: blind+edge. Reconciliation: Row: DW-0023; Resolved 2026-05-12; Evidence: MigrationCatalog.BuildEdges explicit duplicate-edge validation; User-visible behavior: duplicate migration edges fail with named edge rather than latent `SingleOrDefault`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — third pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:87-108
reason: **DEF-9-2-24 — `MigrationCatalog.BuildEdges` throws from a static field initializer** [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:87-108`] — Defensive guard; only one edge currently and a duplicate would also fail tests at first instantiation. Sources: blind+edge. Reconciliation: Row: DW-0023; Resolved 2026-05-12; Evidence: MigrationCatalog.BuildEdges explicit duplicate-edge validation; User-visible behavior: duplicate migration edges fail with named edge rather than latent `SingleOrDefault`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:87-108: MigrationCatalog.BuildEdges explicit duplicate-edge validation

### DW-755: ✅ DEF-9-2-25 — Resolved 2026-05-10 — Apply IOException during write leaves partial file [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`] — `SourceFile.WriteAsync` now writes through a same-directory temp file and replaces the target only after the temp write completes; README documents the apply-write behavior. Sources: edge. Reconciliation: Row: DW-0024; Resolved 2026-05-10; Evidence: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; Original review source/date preserved.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — third pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs
reason: ✅ **DEF-9-2-25 — Resolved 2026-05-10 — Apply IOException during write leaves partial file** [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`] — `SourceFile.WriteAsync` now writes through a same-directory temp file and replaces the target only after the temp write completes; README documents the apply-write behavior. Sources: edge. Reconciliation: Row: DW-0024; Resolved 2026-05-10; Evidence: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; Original review source/date preserved.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:1

### DW-756: `PathUtilities.Canonical` drive-root edge case (empty `Path.GetFileName`) [`src/Hexalith.FrontComposer.Cli/PathUtilities.cs`] — Drive roots are not valid Compile Include targets. Sources: edge. Reconciliation: Row: DW-0025; Resolved 2026-05-12; Evidence: PathUtilities.Canonical catches invalid path shapes and selection rejects non-project roots; User-visible behavior: drive roots are refused as unsupported project/source paths.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — third pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/PathUtilities.cs
reason: **DEF-9-2-26 — `PathUtilities.Canonical` drive-root edge case (empty `Path.GetFileName`)** [`src/Hexalith.FrontComposer.Cli/PathUtilities.cs`] — Drive roots are not valid Compile Include targets. Sources: edge. Reconciliation: Row: DW-0025; Resolved 2026-05-12; Evidence: PathUtilities.Canonical catches invalid path shapes and selection rejects non-project roots; User-visible behavior: drive roots are refused as unsupported project/source paths.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/PathUtilities.cs: PathUtilities.Canonical catches invalid path shapes and selection rejects non-project roots

### DW-757: `SourceFile.ReadAsync` OOM on a multi-GB `.cs` file [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`] — No realistic adopter `.cs` source approaches OOM thresholds. Sources: edge. Reconciliation: Row: DW-0026; Resolved 2026-05-12; Evidence: SourceFile.MaxSupportedBytes and SourceFile_ReadAsyncRejectsExcessiveFileSizeBeforeDecoding; User-visible behavior: files over 16 MiB fail closed before decoding.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — third pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs
reason: **DEF-9-2-27 — `SourceFile.ReadAsync` OOM on a multi-GB `.cs` file** [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`] — No realistic adopter `.cs` source approaches OOM thresholds. Sources: edge. Reconciliation: Row: DW-0026; Resolved 2026-05-12; Evidence: SourceFile.MaxSupportedBytes and SourceFile_ReadAsyncRejectsExcessiveFileSizeBeforeDecoding; User-visible behavior: files over 16 MiB fail closed before decoding.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs: SourceFile.MaxSupportedBytes and SourceFile_ReadAsyncRejectsExcessiveFileSizeBeforeDecoding

### DW-758: `ToolPackagingSmokeTests.FindOnPath` PATHEXT casing (`extension.ToLowerInvariant()`) [`tests/Hexalith.FrontComposer.Cli.Tests/ToolPackagingSmokeTests.cs:118`] — Works on Windows due to case-insensitive filesystem; minor. Sources: blind+edge. Reconciliation: Row: DW-0027; Resolved 2026-05-12; Evidence: ToolPackagingSmokeTests preserves PATHEXT casing and focused CLI tests passed; User-visible behavior: packaging smoke lookup respects platform path extension casing.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — third pass (2026-05-09)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Cli.Tests/ToolPackagingSmokeTests.cs:118
reason: **DEF-9-2-28 — `ToolPackagingSmokeTests.FindOnPath` PATHEXT casing (`extension.ToLowerInvariant()`)** [`tests/Hexalith.FrontComposer.Cli.Tests/ToolPackagingSmokeTests.cs:118`] — Works on Windows due to case-insensitive filesystem; minor. Sources: blind+edge. Reconciliation: Row: DW-0027; Resolved 2026-05-12; Evidence: ToolPackagingSmokeTests preserves PATHEXT casing and focused CLI tests passed; User-visible behavior: packaging smoke lookup respects platform path extension casing.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Cli.Tests/ToolPackagingSmokeTests.cs:118: ToolPackagingSmokeTests preserves PATHEXT casing and focused CLI tests passed

### DW-759: `MigrationDiagnosticSidecarReader.NormalizePath` does not handle drive-relative paths like `C:foo.cs` [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`] — Degrades to `RedactedPathSentinel` and silently drops; lookup misses. Sources: edge. Reconciliation: Row: DW-0028; Resolved 2026-05-12; Evidence: Migrate_SidecarHostilePathsSurfaceManualOnlySentinel; User-visible behavior: drive-relative, traversal, and URI sidecar paths surface sentinel manual-only entries.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — third pass (2026-05-09)"), 2026-08-27
location: foo.cs
reason: **DEF-9-2-29 — `MigrationDiagnosticSidecarReader.NormalizePath` does not handle drive-relative paths like `C:foo.cs`** [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`] — Degrades to `RedactedPathSentinel` and silently drops; lookup misses. Sources: edge. Reconciliation: Row: DW-0028; Resolved 2026-05-12; Evidence: Migrate_SidecarHostilePathsSurfaceManualOnlySentinel; User-visible behavior: drive-relative, traversal, and URI sidecar paths surface sentinel manual-only entries.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: foo.cs: Migrate_SidecarHostilePathsSurfaceManualOnlySentinel

### DW-760: `--project` / `ProjectSelection` does not canonicalize through symlinks/junctions before downstream use [`src/Hexalith.FrontComposer.Cli/ProjectSelection.cs:14-19`] — Minor; downstream `WriteSafetyPolicy` re-canonicalizes. Sources: edge. Reconciliation: Row: DW-0029; Resolved 2026-05-12; Evidence: ProjectSelection canonicalizes explicit project and solution paths; User-visible behavior: selected project path is canonical before downstream trust decisions.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — second pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/ProjectSelection.cs:14-19
reason: **DEF-9-2-7 — `--project` / `ProjectSelection` does not canonicalize through symlinks/junctions before downstream use** [`src/Hexalith.FrontComposer.Cli/ProjectSelection.cs:14-19`] — Minor; downstream `WriteSafetyPolicy` re-canonicalizes. Sources: edge. Reconciliation: Row: DW-0029; Resolved 2026-05-12; Evidence: ProjectSelection canonicalizes explicit project and solution paths; User-visible behavior: selected project path is canonical before downstream trust decisions.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/ProjectSelection.cs:14-19: ProjectSelection canonicalizes explicit project and solution paths

### DW-761: `.sln` parser does not robustly parse VS-format quoted paths with escaped quotes [`src/Hexalith.FrontComposer.Cli/ProjectSelection.cs:1382-1390`] — Works on every test fixture; defer until a real adopter `.sln` breaks. Sources: edge. Reconciliation: Row: DW-0030; Resolved 2026-05-12; Evidence: ProjectSelection_ReadsQuotedSolutionProjectPathsDeterministically; User-visible behavior: quoted `.sln` project paths are parsed deterministically.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — second pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/ProjectSelection.cs:1382-1390
reason: **DEF-9-2-8 — `.sln` parser does not robustly parse VS-format quoted paths with escaped quotes** [`src/Hexalith.FrontComposer.Cli/ProjectSelection.cs:1382-1390`] — Works on every test fixture; defer until a real adopter `.sln` breaks. Sources: edge. Reconciliation: Row: DW-0030; Resolved 2026-05-12; Evidence: ProjectSelection_ReadsQuotedSolutionProjectPathsDeterministically; User-visible behavior: quoted `.sln` project paths are parsed deterministically.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/ProjectSelection.cs:1382-1390: ProjectSelection_ReadsQuotedSolutionProjectPathsDeterministically

### DW-762: `.slnx` and `.fsproj` not supported [`src/Hexalith.FrontComposer.Cli/ProjectSelection.cs`] — First-pass patch claim was partial; only the CSV-split brittleness was fixed. Owner: Story 9-3 IDE parity. Sources: auditor. Reconciliation: Row: DW-0031; Resolved 2026-05-12; Evidence: ProjectSelection_RejectsUnsupportedExplicitProjectFormats, ProjectSelection_RejectsUnsupportedSolutionFormats, ProjectSelection_RejectsUnsupportedSolutionProjectTypes; User-visible behavior: `.slnx` and `.fsproj` fail closed with sanitized guidance.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — second pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/ProjectSelection.cs
reason: **DEF-9-2-9 — `.slnx` and `.fsproj` not supported** [`src/Hexalith.FrontComposer.Cli/ProjectSelection.cs`] — First-pass patch claim was partial; only the CSV-split brittleness was fixed. **Owner:** Story 9-3 IDE parity. Sources: auditor. Reconciliation: Row: DW-0031; Resolved 2026-05-12; Evidence: ProjectSelection_RejectsUnsupportedExplicitProjectFormats, ProjectSelection_RejectsUnsupportedSolutionFormats, ProjectSelection_RejectsUnsupportedSolutionProjectTypes; User-visible behavior: `.slnx` and `.fsproj` fail closed with sanitized guidance.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/ProjectSelection.cs: ProjectSelection_RejectsUnsupportedExplicitProjectFormats, ProjectSelection_RejectsUnsupportedSolutionFormats, ProjectSelection_RejectsUnsupportedSolutionProjectTypes

### DW-765: ✅ DEF-9-2-12 — Resolved 2026-05-10 — No atomic temp+rename write [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`] — Duplicate of DEF-9-2-2; `SourceFile.WriteAsync` now writes via a same-directory temp file before replacing the target. Sources: edge. Reconciliation: Row: DW-0034; Duplicate of DEF-9-2-2; Evidence: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; Alias preserves original source row.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — second pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs
reason: ✅ **DEF-9-2-12 — Resolved 2026-05-10 — No atomic temp+rename write** [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`] — Duplicate of DEF-9-2-2; `SourceFile.WriteAsync` now writes via a same-directory temp file before replacing the target. Sources: edge. Reconciliation: Row: DW-0034; Duplicate of DEF-9-2-2; Evidence: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; Alias preserves original source row.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:1

### DW-766: `MefHostServices` composition exception not surfaced cleanly [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:566-568`] — Only triggers on a misconfigured deploy. Catch `CompositionException` and surface a clear "Workspaces assemblies failed to load" error. Sources: edge. Reconciliation: Row: DW-0035; Resolved 2026-05-12; Evidence: MigrationPlanner catches workspace composition/load failures; User-visible behavior: workspace assembly failures report bounded sanitized guidance.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — second pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:566-568
reason: **DEF-9-2-13 — `MefHostServices` composition exception not surfaced cleanly** [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:566-568`] — Only triggers on a misconfigured deploy. Catch `CompositionException` and surface a clear "Workspaces assemblies failed to load" error. Sources: edge. Reconciliation: Row: DW-0035; Resolved 2026-05-12; Evidence: MigrationPlanner catches workspace composition/load failures; User-visible behavior: workspace assembly failures report bounded sanitized guidance.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:566-568: MigrationPlanner catches workspace composition/load failures

### DW-767: ✅ DEF-9-2-14 — Resolved 2026-05-10 — `ProjectDocumentLoader.Load` does not evaluate `<Import>` items via MSBuild [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`, `src/Hexalith.FrontComposer.Cli/README.md`] — The limitation is now documented and `frontcomposer migrate` emits a warning when the selected `.csproj` has top-level `<Import>` elements. Full MSBuild evaluation remains out of scope. Sources: edge. Reconciliation: Row: DW-0036; Resolved 2026-05-10; Evidence: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; Original review source/date preserved.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — second pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs
reason: ✅ **DEF-9-2-14 — Resolved 2026-05-10 — `ProjectDocumentLoader.Load` does not evaluate `<Import>` items via MSBuild** [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`, `src/Hexalith.FrontComposer.Cli/README.md`] — The limitation is now documented and `frontcomposer migrate` emits a warning when the selected `.csproj` has top-level `<Import>` elements. Full MSBuild evaluation remains out of scope. Sources: edge. Reconciliation: Row: DW-0036; Resolved 2026-05-10; Evidence: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; Original review source/date preserved.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:1

### DW-768: `.gitmodules` parser does not unescape `\"`, `\\`, or single-quoted paths [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:1198`] — Git itself only emits double-quoted output; edge case for hand-written files. Sources: edge. Reconciliation: Row: DW-0037; Resolved 2026-05-12; Evidence: Migrate_ParsesSingleQuotedGitmodulesSubmodulePaths and SubmoduleBoundaryReader.UnquoteGitConfigValue; User-visible behavior: hand-written single/double quoted submodule paths remain excluded from writes.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — second pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:1198
reason: **DEF-9-2-15 — `.gitmodules` parser does not unescape `\"`, `\\`, or single-quoted paths** [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:1198`] — Git itself only emits double-quoted output; edge case for hand-written files. Sources: edge. Reconciliation: Row: DW-0037; Resolved 2026-05-12; Evidence: Migrate_ParsesSingleQuotedGitmodulesSubmodulePaths and SubmoduleBoundaryReader.UnquoteGitConfigValue; User-visible behavior: hand-written single/double quoted submodule paths remain excluded from writes.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:1198: Migrate_ParsesSingleQuotedGitmodulesSubmodulePaths and SubmoduleBoundaryReader.UnquoteGitConfigValue

### DW-769: `PathUtilities.Canonical` `catch` is too narrow [`src/Hexalith.FrontComposer.Cli/PathUtilities.cs:45-48`] — Does not catch `PathTooLongException`, `ArgumentException`, `NotSupportedException`. Hardening; rare on Windows long-path scenarios. Sources: edge. Reconciliation: Row: DW-0038; Resolved 2026-05-12; Evidence: PathUtilities.Canonical catches ArgumentException, PathTooLongException, NotSupportedException, IOException, and UnauthorizedAccessException; User-visible behavior: odd path shapes fail closed instead of crashing.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — second pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/PathUtilities.cs:45-48
reason: **DEF-9-2-16 — `PathUtilities.Canonical` `catch` is too narrow** [`src/Hexalith.FrontComposer.Cli/PathUtilities.cs:45-48`] — Does not catch `PathTooLongException`, `ArgumentException`, `NotSupportedException`. Hardening; rare on Windows long-path scenarios. Sources: edge. Reconciliation: Row: DW-0038; Resolved 2026-05-12; Evidence: PathUtilities.Canonical catches ArgumentException, PathTooLongException, NotSupportedException, IOException, and UnauthorizedAccessException; User-visible behavior: odd path shapes fail closed instead of crashing.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/PathUtilities.cs:45-48: PathUtilities.Canonical catches ArgumentException, PathTooLongException, NotSupportedException, IOException, and UnauthorizedAccessException

### DW-770: Ctrl+C double-press does not force-exit [`src/Hexalith.FrontComposer.Cli/Program.cs`] — Second press should restore the default handler. UX polish. Sources: blind+edge. Reconciliation: Row: DW-0039; Resolved 2026-05-12; Evidence: Program.cs cancelPresses handler and docs/reference/cli.md; User-visible behavior: first Ctrl+C cancels, second Ctrl+C returns control to the OS default.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools — second pass (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/Program.cs
reason: **DEF-9-2-17 — Ctrl+C double-press does not force-exit** [`src/Hexalith.FrontComposer.Cli/Program.cs`] — Second press should restore the default handler. UX polish. Sources: blind+edge. Reconciliation: Row: DW-0039; Resolved 2026-05-12; Evidence: Program.cs cancelPresses handler and docs/reference/cli.md; User-visible behavior: first Ctrl+C cancels, second Ctrl+C returns control to the OS default.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/Program.cs: Program.cs cancelPresses handler and docs/reference/cli.md

### DW-771: `fail-on-warning` vs `fail-on-error` precedence undocumented [`src/Hexalith.FrontComposer.Cli/InspectCommand.cs:678-685`] — Both flags are honored but the help text and JSON contract do not document precedence. Add to README and JSON `applied` payload. Sources: blind. Reconciliation: Row: DW-0040; Resolved 2026-05-12; Evidence: CliApplication help, src/Hexalith.FrontComposer.Cli/README.md, docs/reference/cli.md; User-visible behavior: `--fail-on-warning` is documented as stricter than `--fail-on-error`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/InspectCommand.cs:678-685
reason: **DEF-9-2-1 — `fail-on-warning` vs `fail-on-error` precedence undocumented** [`src/Hexalith.FrontComposer.Cli/InspectCommand.cs:678-685`] — Both flags are honored but the help text and JSON contract do not document precedence. Add to README and JSON `applied` payload. Sources: blind. Reconciliation: Row: DW-0040; Resolved 2026-05-12; Evidence: CliApplication help, src/Hexalith.FrontComposer.Cli/README.md, docs/reference/cli.md; User-visible behavior: `--fail-on-warning` is documented as stricter than `--fail-on-error`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/InspectCommand.cs:678-685: CliApplication help, src/Hexalith.FrontComposer.Cli/README.md, docs/reference/cli.md

### DW-772: ✅ DEF-9-2-2 — Resolved 2026-05-10 — Apply does not write to temp + atomic rename [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`] — `SourceFile.WriteAsync` now writes to a same-directory `*.tmp` file, then replaces the target with `File.Move(..., overwrite: true)` and best-effort temp cleanup. Sources: edge. Reconciliation: Row: DW-0041; Resolved 2026-05-10; Evidence: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; Original review source/date preserved.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs
reason: ✅ **DEF-9-2-2 — Resolved 2026-05-10 — Apply does not write to temp + atomic rename** [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs`] — `SourceFile.WriteAsync` now writes to a same-directory `*.tmp` file, then replaces the target with `File.Move(..., overwrite: true)` and best-effort temp cleanup. Sources: edge. Reconciliation: Row: DW-0041; Resolved 2026-05-10; Evidence: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs; Original review source/date preserved.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:1

### DW-773: ✅ DEF-9-2-3 — Resolved 2026-05-10 — Race between `Directory.Exists` and `EnumerateFiles` returns generic IO error [`src/Hexalith.FrontComposer.Cli/InspectCommand.cs`] — generated-output enumeration now catches `DirectoryNotFoundException`, `IOException`, and `UnauthorizedAccessException` and returns `GeneratedOutputUnavailable` with retry/`--build` guidance. Sources: edge. Reconciliation: Row: DW-0042; Resolved 2026-05-10; Evidence: src/Hexalith.FrontComposer.Cli/InspectCommand.cs; Original review source/date preserved.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/InspectCommand.cs
reason: ✅ **DEF-9-2-3 — Resolved 2026-05-10 — Race between `Directory.Exists` and `EnumerateFiles` returns generic IO error** [`src/Hexalith.FrontComposer.Cli/InspectCommand.cs`] — generated-output enumeration now catches `DirectoryNotFoundException`, `IOException`, and `UnauthorizedAccessException` and returns `GeneratedOutputUnavailable` with retry/`--build` guidance. Sources: edge. Reconciliation: Row: DW-0042; Resolved 2026-05-10; Evidence: src/Hexalith.FrontComposer.Cli/InspectCommand.cs; Original review source/date preserved.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/InspectCommand.cs:1

### DW-774: ✅ DEF-9-2-4 — Resolved 2026-05-10 — `ProjectLooksFrontComposerAnnotated` does not catch `IOException` [`src/Hexalith.FrontComposer.Cli/InspectCommand.cs`] — the annotation probe now wraps recursive source enumeration and degrades to "no annotations" on directory/file access failures. Sources: edge. Reconciliation: Row: DW-0043; Resolved 2026-05-10; Evidence: src/Hexalith.FrontComposer.Cli/InspectCommand.cs; Original review source/date preserved.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/InspectCommand.cs
reason: ✅ **DEF-9-2-4 — Resolved 2026-05-10 — `ProjectLooksFrontComposerAnnotated` does not catch `IOException`** [`src/Hexalith.FrontComposer.Cli/InspectCommand.cs`] — the annotation probe now wraps recursive source enumeration and degrades to "no annotations" on directory/file access failures. Sources: edge. Reconciliation: Row: DW-0043; Resolved 2026-05-10; Evidence: src/Hexalith.FrontComposer.Cli/InspectCommand.cs; Original review source/date preserved.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/InspectCommand.cs:1

### DW-775: `MigrationCatalog.Resolve` uses `SingleOrDefault` [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:88`] — Throws `InvalidOperationException` if a future contributor adds a duplicate `(from,to)` edge. Switch to `FirstOrDefault` plus a startup uniqueness assertion. Sources: edge. Reconciliation: Row: DW-0044; Resolved 2026-05-12; Evidence: MigrationCatalog.BuildEdges duplicate-edge validation; User-visible behavior: duplicate catalog edges fail with named edge during catalog initialization.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:88
reason: **DEF-9-2-5 — `MigrationCatalog.Resolve` uses `SingleOrDefault`** [`src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:88`] — Throws `InvalidOperationException` if a future contributor adds a duplicate `(from,to)` edge. Switch to `FirstOrDefault` plus a startup uniqueness assertion. Sources: edge. Reconciliation: Row: DW-0044; Resolved 2026-05-12; Evidence: MigrationCatalog.BuildEdges duplicate-edge validation; User-visible behavior: duplicate catalog edges fail with named edge during catalog initialization.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/MigrationCommand.cs:88: MigrationCatalog.BuildEdges duplicate-edge validation

### DW-776: ✅ DEF-9-2-6 — Resolved 2026-05-10 — `--build` hint not always emitted in inspect error messages [`src/Hexalith.FrontComposer.Cli/InspectCommand.cs`] — both annotated and no-obvious-annotation generated-output-missing branches now suggest `dotnet build` / `--build`; covered by CLI tests. Sources: blind. Reconciliation: Row: DW-0045; Resolved 2026-05-10; Evidence: src/Hexalith.FrontComposer.Cli/InspectCommand.cs; Original review source/date preserved.

origin: migrated from legacy ledger ("Deferred from: code review of 9-2-cli-inspection-and-migration-tools (2026-05-09)"), 2026-08-27
location: src/Hexalith.FrontComposer.Cli/InspectCommand.cs
reason: ✅ **DEF-9-2-6 — Resolved 2026-05-10 — `--build` hint not always emitted in inspect error messages** [`src/Hexalith.FrontComposer.Cli/InspectCommand.cs`] — both annotated and no-obvious-annotation generated-output-missing branches now suggest `dotnet build` / `--build`; covered by CLI tests. Sources: blind. Reconciliation: Row: DW-0045; Resolved 2026-05-10; Evidence: src/Hexalith.FrontComposer.Cli/InspectCommand.cs; Original review source/date preserved.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Cli/InspectCommand.cs:1

### DW-777: AC14 PublishAot=true alone does not fire HFC1070 [`src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:116`] — Production gates the HFC1070 emit on `optionsResult.Options.PublishTrimmed` only. AC14 reads "trim-enabled OR native-AOT host", so a project with `PublishAot=true` and `PublishTrimmed=false` (uncommon but valid for some AOT scenarios) silently skips the advisory. The chunk-C test theory was reduced to the `PublishTrimmed`-only matrix; restore the `(false, true)` case once production also gates on `PublishAot`. Owner: Story 9-1 follow-up production patch. Sources: edge. Reconciliation: Row: DW-0046; Resolved 2026-05-12; Evidence: `DriftOptions.TrimOrAotAdvisoryEnabled`, `FrontComposerGenerator`, `TrimAotReflectionCatalogDiagnosticTests.TrimOrAotEnabled_AndNoOverrideEvidence_EmitsHfc1070`; User-visible behavior: HFC1070 now fires for `PublishAot=true && PublishTrimmed=false` when contracts are present and no adopter catalog override is observable.

origin: migrated from legacy ledger ("Deferred from: code review of 9-1-build-time-drift-detection chunk C (2026-05-07)"), 2026-08-27
location: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:116
reason: **DEF-9-1C-2 — AC14 PublishAot=true alone does not fire HFC1070** [`src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:116`] — Production gates the HFC1070 emit on `optionsResult.Options.PublishTrimmed` only. AC14 reads "trim-enabled OR native-AOT host", so a project with `PublishAot=true` and `PublishTrimmed=false` (uncommon but valid for some AOT scenarios) silently skips the advisory. The chunk-C test theory was reduced to the `PublishTrimmed`-only matrix; restore the `(false, true)` case once production also gates on `PublishAot`. **Owner:** Story 9-1 follow-up production patch. Sources: edge. Reconciliation: Row: DW-0046; Resolved 2026-05-12; Evidence: `DriftOptions.TrimOrAotAdvisoryEnabled`, `FrontComposerGenerator`, `TrimAotReflectionCatalogDiagnosticTests.TrimOrAotEnabled_AndNoOverrideEvidence_EmitsHfc1070`; User-visible behavior: HFC1070 now fires for `PublishAot=true && PublishTrimmed=false` when contracts are present and no adopter catalog override is observable.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs:116: DriftOptions.TrimOrAotAdvisoryEnabled, FrontComposerGenerator, TrimAotReflectionCatalogDiagnosticTests.TrimOrAotEnabled_AndNoOverrideEvidence_EmitsHfc1070

### DW-778: AC11 perf coverage out of chunk C scope [`tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Benchmarks/IncrementalRebuildBenchmarkTests.cs` or sibling] — AC11 mandates median/p95 < 500 ms with warmup excluded across cache-hit and cache-miss paths. None of the six chunk-C files (Regression, Incremental, TrimAot, Seam, Diagnostics/DriftDiagnosticCatalogTests) carry perf assertions. Per spec T7, perf tests live in `Drift/Benchmarks/` (chunk A or pre-existing). Owner: Confirm AC11 coverage exists in `Drift/Benchmarks/IncrementalRebuildBenchmarkTests.cs` or schedule a follow-up perf-test pass. Sources: auditor. Reconciliation: Row: DW-0047; Resolved 2026-05-12; Evidence: `tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/DriftBenchmarkTests.cs`, command `dotnet test ... --filter "Category=Performance|FullyQualifiedName~DriftBenchmark"` passed; User-visible behavior: cache-hit and cache-miss drift benchmark tests now run under `Category=Performance` with drift detection enabled and median/p95 assertions.

origin: migrated from legacy ledger ("Deferred from: code review of 9-1-build-time-drift-detection chunk C (2026-05-07)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Benchmarks/IncrementalRebuildBenchmarkTests.cs
reason: **DEF-9-1C-1 — AC11 perf coverage out of chunk C scope** [`tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Benchmarks/IncrementalRebuildBenchmarkTests.cs` or sibling] — AC11 mandates median/p95 < 500 ms with warmup excluded across cache-hit and cache-miss paths. None of the six chunk-C files (Regression, Incremental, TrimAot, Seam, Diagnostics/DriftDiagnosticCatalogTests) carry perf assertions. Per spec T7, perf tests live in `Drift/Benchmarks/` (chunk A or pre-existing). **Owner:** Confirm AC11 coverage exists in `Drift/Benchmarks/IncrementalRebuildBenchmarkTests.cs` or schedule a follow-up perf-test pass. Sources: auditor. Reconciliation: Row: DW-0047; Resolved 2026-05-12; Evidence: `tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/DriftBenchmarkTests.cs`, command `dotnet test ... --filter "Category=Performance|FullyQualifiedName~DriftBenchmark"` passed; User-visible behavior: cache-hit and cache-miss drift benchmark tests now run under `Category=Performance` with drift detection enabled and median/p95 assertions.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Benchmarks/IncrementalRebuildBenchmarkTests.cs: tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/DriftBenchmarkTests.cs, command dotnet test ... --filter "Category=Performance|FullyQualifiedName~DriftBenchmark" passed

### DW-779: Order-dependence test concatenation `Id+"|"+Message` could mask non-Id/Message diff [`tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftBaselineTrustFailureTests.cs:136-138`] — Pre-existing pattern reinforced (not introduced) by chunk-B fixture renames. Order-comparison tests concatenate diagnostic Id+Message with `|` and ordinal-sort; tests cannot detect a regression where two diagnostics share Id+Message but differ on severity, location, or properties bag. Switch to structural comparison on the full diagnostic shape. Owner: v1.x test-rigor pass. Sources: blind. Reconciliation: Row: DW-0048; Resolved 2026-05-12; Evidence: `DriftBaselineTrustFailureTests.DiagnosticShape` compares ID, severity, message, location path, and full property bag.

origin: migrated from legacy ledger ("Deferred from: code review of 9-1-build-time-drift-detection chunk B (2026-05-07)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftBaselineTrustFailureTests.cs:136-138
reason: **DEF-9-1B-1 — Order-dependence test concatenation `Id+"|"+Message` could mask non-Id/Message diff** [`tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftBaselineTrustFailureTests.cs:136-138`] — Pre-existing pattern reinforced (not introduced) by chunk-B fixture renames. Order-comparison tests concatenate diagnostic Id+Message with `|` and ordinal-sort; tests cannot detect a regression where two diagnostics share Id+Message but differ on severity, location, or properties bag. Switch to structural comparison on the full diagnostic shape. **Owner:** v1.x test-rigor pass. Sources: blind. Reconciliation: Row: DW-0048; Resolved 2026-05-12; Evidence: `DriftBaselineTrustFailureTests.DiagnosticShape` compares ID, severity, message, location path, and full property bag.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftBaselineTrustFailureTests.cs:136-138: DriftBaselineTrustFailureTests.DiagnosticShape compares ID, severity, message, location path, and full property bag.

### DW-780: `baselinePath.ShouldNotContain(":")` over-eager [`tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticContractTests.cs:93`] — A repo-relative path containing a legitimate colon (e.g. URL-fragment-style baseline names) would fail the assertion. Intent is "no Windows drive letter", but match is colon-anywhere. Replace with a more specific check (`!Regex.IsMatch(path, @"^[A-Za-z]:[\\/]")`). Owner: v1.x test-rigor pass. Sources: edge. Reconciliation: Row: DW-0049; Resolved 2026-05-12; Evidence: `DriftDiagnosticContractTests.WindowsDriveRootedPathCheck_AllowsBenignColonOnly` and updated `BaselinePathProperty_IsRepoRelativeForwardSlash_OrOutsideProjectSentinel`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-1-build-time-drift-detection chunk B (2026-05-07)"), 2026-08-27
location: baselinePath.Sh
reason: **DEF-9-1B-2 — `baselinePath.ShouldNotContain(":")` over-eager** [`tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticContractTests.cs:93`] — A repo-relative path containing a legitimate colon (e.g. URL-fragment-style baseline names) would fail the assertion. Intent is "no Windows drive letter", but match is colon-anywhere. Replace with a more specific check (`!Regex.IsMatch(path, @"^[A-Za-z]:[\\/]")`). **Owner:** v1.x test-rigor pass. Sources: edge. Reconciliation: Row: DW-0049; Resolved 2026-05-12; Evidence: `DriftDiagnosticContractTests.WindowsDriveRootedPathCheck_AllowsBenignColonOnly` and updated `BaselinePathProperty_IsRepoRelativeForwardSlash_OrOutsideProjectSentinel`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: baselinePath.Sh: DriftDiagnosticContractTests.WindowsDriveRootedPathCheck_AllowsBenignColonOnly and updated BaselinePathProperty_IsRepoRelativeForwardSlash_OrOutsideProjectSentinel.

### DW-783: AC7 ProjectionBadge metadata-drift test [`tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs`] — Attempted during chunk-B patch application; the test source needs `[ProjectionBadge(BadgeSlot.<Slot>)]` (enum-arg syntax — string literal does not compile) and an enum projection property of a type whose members are fully annotated (otherwise HFC1025 partial-coverage warning fires and the diagnostic surface is mixed). Production `BuildBadgeSignature` runs over annotated enum fields and writes `Slot=Member` pairs ordinally; baseline-side `badgeSignature` field is a comma-joined string. Test scenario needs to: (a) declare `[ProjectionBadge(BadgeSlot.Danger)] New, [ProjectionBadge(BadgeSlot.Success)] Done`; (b) set baseline `badgeSignature` to a different mapping; (c) assert one HFC1066 with `kind="ProjectionBadge"`. Owner: Add when next AC7 coverage pass lands. Sources: auditor. Reconciliation: Row: DW-0052; Resolved 2026-05-12; Evidence: `DriftClassifierMetadataTests.ProjectionBadgeMappingChange_EmitsSingleMetadataDiagnostic`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-1-build-time-drift-detection chunk B (2026-05-07)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs
reason: **DEF-9-1B-5 — AC7 ProjectionBadge metadata-drift test** [`tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs`] — Attempted during chunk-B patch application; the test source needs `[ProjectionBadge(BadgeSlot.<Slot>)]` (enum-arg syntax — string literal does not compile) and an enum projection property of a type whose members are fully annotated (otherwise HFC1025 partial-coverage warning fires and the diagnostic surface is mixed). Production `BuildBadgeSignature` runs over annotated enum fields and writes `Slot=Member` pairs ordinally; baseline-side `badgeSignature` field is a comma-joined string. Test scenario needs to: (a) declare `[ProjectionBadge(BadgeSlot.Danger)] New, [ProjectionBadge(BadgeSlot.Success)] Done`; (b) set baseline `badgeSignature` to a different mapping; (c) assert one HFC1066 with `kind="ProjectionBadge"`. **Owner:** Add when next AC7 coverage pass lands. Sources: auditor. Reconciliation: Row: DW-0052; Resolved 2026-05-12; Evidence: `DriftClassifierMetadataTests.ProjectionBadgeMappingChange_EmitsSingleMetadataDiagnostic`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs: DriftClassifierMetadataTests.ProjectionBadgeMappingChange_EmitsSingleMetadataDiagnostic.

### DW-784: AC7 Destructive metadata-drift test [`tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs`] — Attempted during chunk-B patch application with `[Destructive]` source attribute and `"destructive": false` baseline. Production `CompareContractMetadata` does emit `if (baseline.Destructive != current.Destructive)` for both projections and commands, but the synthetic test fixture did not surface the diagnostic — investigation needed into whether the command snapshot path correctly flows `model.IsDestructive` (bool) into `DriftCurrentContract.Destructive` (bool?) for synthetic test compilations, or whether the `[Destructive]` attribute requires additional surface area (e.g., a `[Description]` attribute alongside) to be picked up by CommandParser in the test compilation context. Owner: Reproduce by adding a Destructive integration test against a real (non-synthetic) command compilation, then port the working pattern back to the chunk-B test. Sources: auditor. Reconciliation: Row: DW-0053; Resolved 2026-05-12; Evidence: `DriftClassifierMetadataTests.CommandContractMetadataChange_EmitsSingleMetadataDiagnostic` (`Destructive` row).

origin: migrated from legacy ledger ("Deferred from: code review of 9-1-build-time-drift-detection chunk B (2026-05-07)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs
reason: **DEF-9-1B-6 — AC7 Destructive metadata-drift test** [`tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs`] — Attempted during chunk-B patch application with `[Destructive]` source attribute and `"destructive": false` baseline. Production `CompareContractMetadata` does emit `if (baseline.Destructive != current.Destructive)` for both projections and commands, but the synthetic test fixture did not surface the diagnostic — investigation needed into whether the command snapshot path correctly flows `model.IsDestructive` (bool) into `DriftCurrentContract.Destructive` (bool?) for synthetic test compilations, or whether the `[Destructive]` attribute requires additional surface area (e.g., a `[Description]` attribute alongside) to be picked up by CommandParser in the test compilation context. **Owner:** Reproduce by adding a Destructive integration test against a real (non-synthetic) command compilation, then port the working pattern back to the chunk-B test. Sources: auditor. Reconciliation: Row: DW-0053; Resolved 2026-05-12; Evidence: `DriftClassifierMetadataTests.CommandContractMetadataChange_EmitsSingleMetadataDiagnostic` (`Destructive` row).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs: DriftClassifierMetadataTests.CommandContractMetadataChange_EmitsSingleMetadataDiagnostic (Destructive row).

### DW-785: AC7 ProjectionRole/Currency/ProjectionEmptyStateCta/Icon metadata-drift coverage [`tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs`] — Four AC7 metadata categories patched in Chunk A (P6) remain without dedicated chunk-B fact tests. Production code paths exist (`AddIfChanged("ProjectionRole", ...)`, `AddIfChanged("Icon", ...)`, `AddIfChanged("ProjectionEmptyStateCta", ...)`, `AddMetadataIfChanged("DisplayFormat", ...)` for currency-driven displayFormat). Each needs: (a) source-side attribute on a representative property/contract; (b) baseline JSON value differing from source; (c) assertion that exactly one HFC1066 fires with the matching `kind` token. Owner: Add alongside DEF-9-1B-5/-6 in the next AC7 coverage pass. Sources: auditor. Reconciliation: Row: DW-0054; Resolved 2026-05-12; Evidence: `DriftClassifierMetadataTests.ProjectionContractMetadataChange_EmitsSingleMetadataDiagnostic`, `CurrencyDisplayFormatChange_EmitsSingleMetadataDiagnostic`, and `CommandContractMetadataChange_EmitsSingleMetadataDiagnostic`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-1-build-time-drift-detection chunk B (2026-05-07)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs
reason: **DEF-9-1B-7 — AC7 ProjectionRole/Currency/ProjectionEmptyStateCta/Icon metadata-drift coverage** [`tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs`] — Four AC7 metadata categories patched in Chunk A (P6) remain without dedicated chunk-B fact tests. Production code paths exist (`AddIfChanged("ProjectionRole", ...)`, `AddIfChanged("Icon", ...)`, `AddIfChanged("ProjectionEmptyStateCta", ...)`, `AddMetadataIfChanged("DisplayFormat", ...)` for currency-driven displayFormat). Each needs: (a) source-side attribute on a representative property/contract; (b) baseline JSON value differing from source; (c) assertion that exactly one HFC1066 fires with the matching `kind` token. **Owner:** Add alongside DEF-9-1B-5/-6 in the next AC7 coverage pass. Sources: auditor. Reconciliation: Row: DW-0054; Resolved 2026-05-12; Evidence: `DriftClassifierMetadataTests.ProjectionContractMetadataChange_EmitsSingleMetadataDiagnostic`, `CurrencyDisplayFormatChange_EmitsSingleMetadataDiagnostic`, and `CommandContractMetadataChange_EmitsSingleMetadataDiagnostic`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs: DriftClassifierMetadataTests.ProjectionContractMetadataChange_EmitsSingleMetadataDiagnostic, CurrencyDisplayFormatChange_EmitsSingleMetadataDiagnostic, and CommandContractMetadataChange_EmitsSingleMetadataDiagnostic.

### DW-792: Load-phase diagnostic count is unbounded [`src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs:566-682`] — `MaxDiagnostics` only caps the comparison phase; hostile/buggy baselines can flood the build with HFC1059–HFC1064. Cap load-phase separately. Source: blind. Reconciliation: Row: DW-0061; Resolved 2026-05-12; Evidence: `DriftBaselineLoader.CapLoadDiagnostics` and `DriftBaselineTrustFailureTests.LoadPhaseDiagnostics_AreCappedAndTruncated`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-1-build-time-drift-detection chunk A (2026-05-07)"), 2026-08-27
location: src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs:566-682
reason: **DEF-9-1A-7 — Load-phase diagnostic count is unbounded** [`src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs:566-682`] — `MaxDiagnostics` only caps the comparison phase; hostile/buggy baselines can flood the build with HFC1059–HFC1064. Cap load-phase separately. Source: blind. Reconciliation: Row: DW-0061; Resolved 2026-05-12; Evidence: `DriftBaselineLoader.CapLoadDiagnostics` and `DriftBaselineTrustFailureTests.LoadPhaseDiagnostics_AreCappedAndTruncated`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs:566-682: DriftBaselineLoader.CapLoadDiagnostics and DriftBaselineTrustFailureTests.LoadPhaseDiagnostics_AreCappedAndTruncated.

### DW-794: `DriftBaselineInput.GetHashCode` uses randomized `string.GetHashCode` [`src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs:470-474`] — Hash is not stable across processes; Roslyn's incremental pipeline uses `Equals` so impact is minor, but `StringComparer.Ordinal.GetHashCode` would be deterministic. Source: auditor. Reconciliation: Row: DW-0063; Resolved 2026-05-12; Evidence: `DriftBaselineInput.GetHashCode` now uses `StringComparer.Ordinal.GetHashCode`.

origin: migrated from legacy ledger ("Deferred from: code review of 9-1-build-time-drift-detection chunk A (2026-05-07)"), 2026-08-27
location: src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs:470-474
reason: **DEF-9-1A-9 — `DriftBaselineInput.GetHashCode` uses randomized `string.GetHashCode`** [`src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs:470-474`] — Hash is not stable across processes; Roslyn's incremental pipeline uses `Equals` so impact is minor, but `StringComparer.Ordinal.GetHashCode` would be deterministic. Source: auditor. Reconciliation: Row: DW-0063; Resolved 2026-05-12; Evidence: `DriftBaselineInput.GetHashCode` now uses `StringComparer.Ordinal.GetHashCode`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Drift/DriftDetection.cs:470-474: DriftBaselineInput.GetHashCode now uses StringComparer.Ordinal.GetHashCode.

### DW-802: `Negotiate_TrustingCallerBool_DoesNotOverrideAnalyzerDecision` reflection helper does not meaningfully exercise the legacy bool [`tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationSnapshotInputTests.cs:1581-1599`] — Test sets up Breaking snapshots so result is always Incompatible regardless of bool. To pin AC6, needs `HasCompatibleAdditiveDrift = true` AND snapshots null. Owner: Restructure when D5/AC6 revalidation lands. Sources: auditor. Reconciliation: Row: DW-0071; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate chunk 4 (2026-05-07)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationSnapshotInputTests.cs:1581-1599
reason: **DEF-CK4-5 — `Negotiate_TrustingCallerBool_DoesNotOverrideAnalyzerDecision` reflection helper does not meaningfully exercise the legacy bool** [`tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationSnapshotInputTests.cs:1581-1599`] — Test sets up Breaking snapshots so result is always Incompatible regardless of bool. To pin AC6, needs `HasCompatibleAdditiveDrift = true` AND snapshots null. **Owner:** Restructure when D5/AC6 revalidation lands. Sources: auditor. Reconciliation: Row: DW-0071; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationSnapshotInputTests.cs:1581

### DW-803: `Catalog_ShipsExactlyTheStoryT8Set` does not fail-loud on missing `fixtureId` [`tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/SchemaFixtureCatalogTests.cs:2061-2070`] — Fixture without `fixtureId` is silently excluded from equality check. Defensive only. Owner: v1.x fixture-catalog hardening. Sources: blind. Reconciliation: Row: DW-0072; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.4 SourceTools/schema-fingerprint follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.4 SourceTools/schema-fingerprint follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate chunk 4 (2026-05-07)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/SchemaFixtureCatalogTests.cs:2061-2070
reason: **DEF-CK4-6 — `Catalog_ShipsExactlyTheStoryT8Set` does not fail-loud on missing `fixtureId`** [`tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/SchemaFixtureCatalogTests.cs:2061-2070`] — Fixture without `fixtureId` is silently excluded from equality check. Defensive only. **Owner:** v1.x fixture-catalog hardening. Sources: blind. Reconciliation: Row: DW-0072; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.4 SourceTools/schema-fingerprint follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.4 SourceTools/schema-fingerprint follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/SchemaFixtureCatalogTests.cs:45-61 fails loudly when fixtureId is missing via TryGetProperty(...).ShouldBeTrue; commit 7e3c160fe72eb1f1a90caa0846e941985d0f0f1e contains the guard.

### DW-805: `SchemaContractFamilyNamesTests.Canonical_DistinctValuesPerFamily` passes vacuously for single-member enum [`tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/SchemaContractFamilyNamesTests.cs:1972-1981`] — `HashSet.Add` returning `true` is trivial for a single-member enum. Owner: Becomes meaningful when second contract family is added. Sources: blind. Reconciliation: Row: DW-0074; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.4 SourceTools/schema-fingerprint follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.4 SourceTools/schema-fingerprint follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate chunk 4 (2026-05-07)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/SchemaContractFamilyNamesTests.cs:1972-1981
reason: **DEF-CK4-8 — `SchemaContractFamilyNamesTests.Canonical_DistinctValuesPerFamily` passes vacuously for single-member enum** [`tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/SchemaContractFamilyNamesTests.cs:1972-1981`] — `HashSet.Add` returning `true` is trivial for a single-member enum. **Owner:** Becomes meaningful when second contract family is added. Sources: blind. Reconciliation: Row: DW-0074; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.4 SourceTools/schema-fingerprint follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.4 SourceTools/schema-fingerprint follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Schema/SchemaContractFamilyNames.cs:8-14 now maps seven schema families, so Canonical_DistinctValuesPerFamily is no longer a vacuous single-member test.

### DW-813: Coverage gaps in `AuthContextAccessor` parser tests (cache-hit, sentinel re-throw, multi-valued StringValues, uppercase hex rejection, null/empty hint) [`tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs`] — Five distinct coverage gaps exposed by chunk-2 memoization rewrite. All work in production but trust-boundary parser is high-priority for fail-closed regressions. Owner: v1.x fail-closed coverage hardening. Sources: edge. Reconciliation: Row: DW-0082; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate chunk 4 (2026-05-07)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs
reason: **DEF-CK4-16 — Coverage gaps in `AuthContextAccessor` parser tests (cache-hit, sentinel re-throw, multi-valued StringValues, uppercase hex rejection, null/empty hint)** [`tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs`] — Five distinct coverage gaps exposed by chunk-2 memoization rewrite. All work in production but trust-boundary parser is high-priority for fail-closed regressions. **Owner:** v1.x fail-closed coverage hardening. Sources: edge. Reconciliation: Row: DW-0082; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs:1

### DW-815: `SourceTools.csproj` ProjectReference to `Schema.csproj` lacks `<PrivateAssets="all" />` [`src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj:18`] — D9 mandates the reference (both `.Mcp` and `.SourceTools` reference Schema) but no SourceTools source file currently consumes a Schema type. With `IsRoslynComponent=true` + `EnforceExtendedAnalyzerRules=true`, the project reference may flow `Hexalith.FrontComposer.Schema.dll` into the analyzer's runtime closure, increasing the analyzer footprint shipped to consuming projects. The `Microsoft.CodeAnalysis.CSharp` PackageReference correctly carries `PrivateAssets="all"`; the new ProjectReference does not. Owner: audit when first analyzer-side delta computation lands (or sooner if analyzer-host validation fails RS1036/RS1038); decide whether `<PrivateAssets="all">` is required for the Schema reference. Sources: blind+edge. Reconciliation: Row: DW-0084; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.4 SourceTools/schema-fingerprint follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.4 SourceTools/schema-fingerprint follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate chunk 3 (2026-05-07)"), 2026-08-27
location: SourceTools.cs
reason: **DEF-CK3-2 — `SourceTools.csproj` ProjectReference to `Schema.csproj` lacks `<PrivateAssets="all" />`** [`src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj:18`] — D9 mandates the reference (both `.Mcp` and `.SourceTools` reference Schema) but no SourceTools source file currently consumes a Schema type. With `IsRoslynComponent=true` + `EnforceExtendedAnalyzerRules=true`, the project reference may flow `Hexalith.FrontComposer.Schema.dll` into the analyzer's runtime closure, increasing the analyzer footprint shipped to consuming projects. The `Microsoft.CodeAnalysis.CSharp` PackageReference correctly carries `PrivateAssets="all"`; the new ProjectReference does not. **Owner:** audit when first analyzer-side delta computation lands (or sooner if analyzer-host validation fails RS1036/RS1038); decide whether `<PrivateAssets="all">` is required for the Schema reference. Sources: blind+edge. Reconciliation: Row: DW-0084; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.4 SourceTools/schema-fingerprint follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.4 SourceTools/schema-fingerprint follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj:12-15 has no Schema ProjectReference; commit 914ae542c8b44b53f644e87d770838a31520699f removed it.
decision: 2026-08-31 Implement change — Implement the bounded change described by DW-815, update affected contracts and consumers, and add focused regression evidence.
decision: 2026-08-31 Implement change — Implement the bounded change described by DW-815, update affected contracts and consumers, and add focused regression evidence.

### DW-818: `ExpectedStateLine` pin lacks a runtime emission cross-check [`tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaFingerprintCrossPackageTests.cs:27`] — The pinned alphabetical line catches catalog drift but won't catch a coincidental match between a re-ordered source enum and the alphabetical pin. The State enum-values cell is not backed by a CLR enum, so the catalog is the source of truth. Add a runtime emission cross-check that asserts the catalog matches what the production MCP server actually emits in `McpLifecycleResult` payloads. Owner: v1.x cross-check hardening. Sources: blind+edge. Reconciliation: Row: DW-0087; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate Group C (2026-05-05)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaFingerprintCrossPackageTests.cs:27
reason: **DEF-C3 — `ExpectedStateLine` pin lacks a runtime emission cross-check** [`tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaFingerprintCrossPackageTests.cs:27`] — The pinned alphabetical line catches catalog drift but won't catch a coincidental match between a re-ordered source enum and the alphabetical pin. The State enum-values cell is not backed by a CLR enum, so the catalog is the source of truth. Add a runtime emission cross-check that asserts the catalog matches what the production MCP server actually emits in `McpLifecycleResult` payloads. **Owner:** v1.x cross-check hardening. Sources: blind+edge. Reconciliation: Row: DW-0087; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaFingerprintCrossPackageTests.cs:27

### DW-821: AC5 server-side revalidation on `CompatibleAdditive` [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`, `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderSchemaGateTests.cs`] — AC5 requires re-running validation/defaulting/bounds/auth/sanitization on `CompatibleAdditive` admit before any side effect. Current admission gate returns the negotiation result but does not invoke the post-admission revalidation hook. The `RevalidationCount >= 1` test assertion was downgraded to `Skip = "AC5: revalidation pending follow-up"`. Follow-up must thread a revalidation step into the projection reader and command invoker pipelines. Owner: Follow-up story (Epic 8). Sources: blind+auditor. Reconciliation: Row: DW-0090; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate (2026-05-05)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs
reason: **DEF-D5 — AC5 server-side revalidation on `CompatibleAdditive`** [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`, `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderSchemaGateTests.cs`] — AC5 requires re-running validation/defaulting/bounds/auth/sanitization on `CompatibleAdditive` admit before any side effect. Current admission gate returns the negotiation result but does not invoke the post-admission revalidation hook. The `RevalidationCount >= 1` test assertion was downgraded to `Skip = "AC5: revalidation pending follow-up"`. Follow-up must thread a revalidation step into the projection reader and command invoker pipelines. **Owner:** Follow-up story (Epic 8). Sources: blind+auditor. Reconciliation: Row: DW-0090; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs:1

### DW-823: Tool admission `Reject` loses original tool reference [`src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs:746-760`] — `Reject` returns `Tool: null` but preserves the user-supplied `requestedName`; logs/observability cannot identify which tool category was rejected. Pre-existing; not regressed by 8-6a. Owner: v1.x telemetry hardening. Sources: blind. Reconciliation: Row: DW-0092; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate (2026-05-05)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs:746-760
reason: **DEF-2 — Tool admission `Reject` loses original tool reference** [`src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs:746-760`] — `Reject` returns `Tool: null` but preserves the user-supplied `requestedName`; logs/observability cannot identify which tool category was rejected. Pre-existing; not regressed by 8-6a. **Owner:** v1.x telemetry hardening. Sources: blind. Reconciliation: Row: DW-0092; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs:746

### DW-826: Manifest aggregator dedup/null-handling [`src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs:14-19`] — May be intentional aggregator behavior; verify against emitter expectation before adding `.Distinct()` or null-filtering. Owner: Story 8-6 design verification. Sources: edge. Reconciliation: Row: DW-0095; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate (2026-05-05)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs:14-19
reason: **DEF-5 — Manifest aggregator dedup/null-handling** [`src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs:14-19`] — May be intentional aggregator behavior; verify against emitter expectation before adding `.Distinct()` or null-filtering. **Owner:** Story 8-6 design verification. Sources: edge. Reconciliation: Row: DW-0095; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs:13-17,28-43 filters nulls, tuple-deduplicates manifests, rejects mixed null/non-null fingerprints, and rejects mixed algorithms; commits b115e111 and 0246689d.

### DW-831: SkillResourceReadResult `IsSuccess` + `Category` dual source of truth [`src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs:1140-1150`] — Privatize ctor and expose `Success`/`Failure` factories to prevent invalid states. Owner: Story 8-5 follow-up. Sources: blind. Reconciliation: Row: DW-0100; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-5-skill-corpus-and-build-time-agent-support (2026-05-04)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs:1140-1150
reason: **DEF-2 — SkillResourceReadResult `IsSuccess` + `Category` dual source of truth** [`src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs:1140-1150`] — Privatize ctor and expose `Success`/`Failure` factories to prevent invalid states. **Owner:** Story 8-5 follow-up. Sources: blind. Reconciliation: Row: DW-0100; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:1154

### DW-834: LowerIdPattern accepts numeric-only IDs and has no length cap [`src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs:1051-1052`] — `^[a-z0-9]+(?:-[a-z0-9]+)*$` permits `1234` or 1000-char IDs; no observed abuse path today. Owner: v1.x identifier policy. Sources: edge. Reconciliation: Row: DW-0103; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-5-skill-corpus-and-build-time-agent-support (2026-05-04)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs:1051-1052
reason: **DEF-5 — LowerIdPattern accepts numeric-only IDs and has no length cap** [`src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs:1051-1052`] — `^[a-z0-9]+(?:-[a-z0-9]+)*$` permits `1234` or 1000-char IDs; no observed abuse path today. **Owner:** v1.x identifier policy. Sources: edge. Reconciliation: Row: DW-0103; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: commit a7e94471; src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusParser.cs:12,157-161,507-509 caps IDs at 128 characters and requires a lowercase-letter prefix.

### DW-837: T1 "owning story/follow-up" per-file metadata — Rolled into P-43 aggregate manifest (DN-8 resolved → derived aggregate at runtime). Owner: Story 8-5 P-43. Sources: auditor. Reconciliation: Row: DW-0106; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-5-skill-corpus-and-build-time-agent-support (2026-05-04)"), 2026-08-27
location: n/a
reason: **DEF-8 — T1 "owning story/follow-up" per-file metadata** — Rolled into P-43 aggregate manifest (DN-8 resolved → derived aggregate at runtime). **Owner:** Story 8-5 P-43. Sources: auditor. Reconciliation: Row: DW-0106; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusAggregateManifestBuilder.cs:10 derives the aggregate manifest and lines 19-21 carry each resource's OwningStory.

### DW-841: Schema fingerprint emission and schema-failure categories present in 8-4a's path-filtered diff [`src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs`, `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpFailureCategory.cs`, `src/Hexalith.FrontComposer.Contracts/Mcp/McpResourceDescriptor.cs`] — Coalesced PR #9 included Story 8-6 changes; per scope guardrails this work belongs to Story 8-6 (`review`). Owner: Story 8-6 review. Sources: auditor. Reconciliation: Row: DW-0110; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.4 SourceTools/schema-fingerprint follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.4 SourceTools/schema-fingerprint follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-4a-projection-rendering-sanitized-taxonomy-and-snapshot pass 2 (2026-05-04)"), 2026-08-27
location: src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs
reason: **R2-D3 — Schema fingerprint emission and schema-failure categories present in 8-4a's path-filtered diff** [`src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs`, `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpFailureCategory.cs`, `src/Hexalith.FrontComposer.Contracts/Mcp/McpResourceDescriptor.cs`] — Coalesced PR #9 included Story 8-6 changes; per scope guardrails this work belongs to Story 8-6 (`review`). **Owner:** Story 8-6 review. Sources: auditor. Reconciliation: Row: DW-0110; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.4 SourceTools/schema-fingerprint follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.4 SourceTools/schema-fingerprint follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Emitters/McpManifestEmitter.cs:1

### DW-842: Skill corpus resource provider registered in `AddFrontComposerMcp` [`src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:47-48,73`] — Same coalesced-PR pattern as R2-D3; Story 8-5 territory. Owner: Story 8-5 review. Sources: auditor. Reconciliation: Row: DW-0111; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-4a-projection-rendering-sanitized-taxonomy-and-snapshot pass 2 (2026-05-04)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:47-48
reason: **R2-D4 — Skill corpus resource provider registered in `AddFrontComposerMcp`** [`src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:47-48,73`] — Same coalesced-PR pattern as R2-D3; Story 8-5 territory. **Owner:** Story 8-5 review. Sources: auditor. Reconciliation: Row: DW-0111; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:47

### DW-851: `FormatEnumerable` hardcoded 5-element cap ignores options [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:316-327`] — Magic number unrelated to configured cell budget; no option to tune. Less impactful once F21 (arrays should fall through to unsupported placeholder per Formatting Matrix) is fixed via a Patch. Owner: Options-pattern cleanup. Sources: edge+blind. Reconciliation: Row: DW-0120; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-4-projection-rendering-for-agents (2026-05-03)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:316-327
reason: **DF8.4-2 — `FormatEnumerable` hardcoded 5-element cap ignores options** [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:316-327`] — Magic number unrelated to configured cell budget; no option to tune. Less impactful once F21 (arrays should fall through to unsupported placeholder per Formatting Matrix) is fixed via a Patch. **Owner:** Options-pattern cleanup. Sources: edge+blind. Reconciliation: Row: DW-0120; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:474-480 no longer enumerates or caps five elements; collection values return the canonical unsupported placeholder; commit a776288c0a3661f66c3f840b68c0a6119e40e7df introduced that behavior.

### DW-853: `MaxFieldsPerResource = 0` and `MaxRowsPerResource = 0` silently fall back to 1 via `Math.Max(1, ...)` clamp [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs` validator] — Validator covers new projection bounds (cell, doc, suggestions, status groups, timeline entries) but not pre-existing per-resource bounds; operator setting 0 expects a hard fail. Owner: Options-pattern hardening (paired with F25 cell-min validator patch). Sources: auditor. Reconciliation: Row: DW-0122; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-4-projection-rendering-for-agents (2026-05-03)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs
reason: **DF8.4-4 — `MaxFieldsPerResource = 0` and `MaxRowsPerResource = 0` silently fall back to 1 via `Math.Max(1, ...)` clamp** [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs` validator] — Validator covers new projection bounds (cell, doc, suggestions, status groups, timeline entries) but not pre-existing per-resource bounds; operator setting 0 expects a hard fail. **Owner:** Options-pattern hardening (paired with F25 cell-min validator patch). Sources: auditor. Reconciliation: Row: DW-0122; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:219-221 rejects non-positive MaxRowsPerResource and MaxFieldsPerResource.

### DW-859: `IUlidFactory` lives in `Hexalith.FrontComposer.Contracts` and is registered by `AddFrontComposerMcp` [`src/Hexalith.FrontComposer.Contracts/Lifecycle/IUlidFactory.cs`] — Architectural coupling: a non-MCP host that uses Contracts could see MCP's singleton via `TryAddSingleton` semantics. Re-shape only if Contracts surface widens further. Owner: Cross-Story Contract review (Story 8-1). Sources: auditor. Reconciliation: Row: DW-0128; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-3-two-call-lifecycle-and-agent-command-semantics Round 3 (2026-05-03)"), 2026-08-27
location: src/Hexalith.FrontComposer.Contracts/Lifecycle/IUlidFactory.cs
reason: **DF8.3-R3-4 — `IUlidFactory` lives in `Hexalith.FrontComposer.Contracts` and is registered by `AddFrontComposerMcp`** [`src/Hexalith.FrontComposer.Contracts/Lifecycle/IUlidFactory.cs`] — Architectural coupling: a non-MCP host that uses Contracts could see MCP's singleton via `TryAddSingleton` semantics. Re-shape only if Contracts surface widens further. **Owner:** Cross-Story Contract review (Story 8-1). Sources: auditor. Reconciliation: Row: DW-0128; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current verified behavior as intentional and close the deferred row.
decision: 2026-08-27 Accept current behavior — Record the current verified behavior as intentional and close the deferred row.

### DW-860: `_byMessage` collision is silently dropped on `TryAdd` [`Invocation/FrontComposerMcpLifecycleTracker.cs:194`] — Mitigated by ULID factory collision resistance under correct config. Promote to fail-closed only if external messageId injection becomes a supported flow. Owner: Defer; promote alongside MCP messageId-injection feature work. Sources: blind+edge. Reconciliation: Row: DW-0129; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-3-two-call-lifecycle-and-agent-command-semantics Round 3 (2026-05-03)"), 2026-08-27
location: FrontComposerMcpLifecycleTracker.cs:194
reason: **DF8.3-R3-5 — `_byMessage` collision is silently dropped on `TryAdd`** [`Invocation/FrontComposerMcpLifecycleTracker.cs:194`] — Mitigated by ULID factory collision resistance under correct config. Promote to fail-closed only if external messageId injection becomes a supported flow. **Owner:** Defer; promote alongside MCP messageId-injection feature work. Sources: blind+edge. Reconciliation: Row: DW-0129; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs:210-214 handles message-key collision by removing the correlation, disposing the new entry, and throwing fail-closed FrontComposerMcpException; commit 238aaa37.

### DW-874: Out-of-scope submodule pointer changes in `Hexalith.EventStore` and `Hexalith.Tenants` — Working-tree carries unrelated submodule pointer drifts (release tags, doc updates) not declared in the Story 7-3 File List. Should be reverted or committed under a separate change. Owner: Pre-merge cleanup. Sources: A. Reconciliation: Row: DW-0143; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.4 SourceTools/schema-fingerprint follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.4 SourceTools/schema-fingerprint follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 7-3-command-authorization-policies (2026-05-01)"), 2026-08-27
location: Hexalith.EventStore
reason: **DF1 — Out-of-scope submodule pointer changes in `Hexalith.EventStore` and `Hexalith.Tenants`** — Working-tree carries unrelated submodule pointer drifts (release tags, doc updates) not declared in the Story 7-3 File List. Should be reverted or committed under a separate change. **Owner:** Pre-merge cleanup. Sources: A. Reconciliation: Row: DW-0143; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.4 SourceTools/schema-fingerprint follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.4 SourceTools/schema-fingerprint follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 1cc9c277 records both Hexalith.EventStore and Hexalith.Tenants gitlink updates; the current tracked paths are clean

### DW-880: Decision cache across executable boundaries — Explicitly deferred per Advanced Elicitation Hardening (Deferred Decisions). v1 must use fresh executable checks; bounded short-lived cache deferred to performance follow-up. Sources: spec. Reconciliation: Row: DW-0149; Final classification 2026-05-14: split-to-named-story; Target owner: Story 10.6 benchmark/release guard follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 10.6 benchmark/release guard follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 7-3-command-authorization-policies (2026-05-01)"), 2026-08-27
location: n/a
reason: **DF7 — Decision cache across executable boundaries** — Explicitly deferred per Advanced Elicitation Hardening (Deferred Decisions). v1 must use fresh executable checks; bounded short-lived cache deferred to performance follow-up. Sources: spec. Reconciliation: Row: DW-0149; Final classification 2026-05-14: split-to-named-story; Target owner: Story 10.6 benchmark/release guard follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 10.6 benchmark/release guard follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-894: `AuthorizingCommandServiceDecorator` (DN2 re-defer) [needs new file under `src/Hexalith.FrontComposer.Shell/Services/Authorization/` + DI registration in `ServiceCollectionExtensions.AddHexalithFrontComposer*` + 4-6 tests under `tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/`] — Wraps `ICommandService.DispatchAsync`. Reads `DomainManifest.CommandPolicies` keyed by `command.GetType().FullName`. Pass-through unprotected commands. Use `SourceSurface.DirectDispatch`. On deny, throws `UnauthorizedAccessException` (or returns failed-result type matching `ICommandService` contract — design call needed). Critical for AC2 boundary completeness. Owner: Dedicated `/bmad-dev-story 7-3` follow-up session. Sources: A1 / Pass-2 DN2. Reconciliation: Row: DW-0163; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 7-3-command-authorization-policies (Pass 3, 2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Services/Authorization/
reason: **DF14 — `AuthorizingCommandServiceDecorator` (DN2 re-defer)** [needs new file under `src/Hexalith.FrontComposer.Shell/Services/Authorization/` + DI registration in `ServiceCollectionExtensions.AddHexalithFrontComposer*` + 4-6 tests under `tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/`] — Wraps `ICommandService.DispatchAsync`. Reads `DomainManifest.CommandPolicies` keyed by `command.GetType().FullName`. Pass-through unprotected commands. Use `SourceSurface.DirectDispatch`. On deny, throws `UnauthorizedAccessException` (or returns failed-result type matching `ICommandService` contract — design call needed). Critical for AC2 boundary completeness. **Owner:** Dedicated `/bmad-dev-story 7-3` follow-up session. Sources: A1 / Pass-2 DN2. Reconciliation: Row: DW-0163; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandDispatchAuthorizationGate.cs:1

### DW-895: `CommandRendererEmitter` evaluator wiring (DN3 re-defer) [needs source-generator emitter rewrite + 7+ verified.txt rebaselines + bUnit fixtures under `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Authorization/`] — Inject `ICommandAuthorizationEvaluator` into rendered components. Render-time probe sets cached flag; submit-time check stays authoritative. Inline / compact-inline / full-page trigger `Disabled` binding consults the flag. Emit appropriate `SourceSurface.{Inline,CompactInline,FullPage}Action`. Substantial scope; chains naturally with DN4. Owner: Dedicated dev session. Sources: A2 / Pass-2 DN3. Reconciliation: Row: DW-0164; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 7-3-command-authorization-policies (Pass 3, 2026-05-02)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Generated/Authorization/
reason: **DF15 — `CommandRendererEmitter` evaluator wiring (DN3 re-defer)** [needs source-generator emitter rewrite + 7+ verified.txt rebaselines + bUnit fixtures under `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Authorization/`] — Inject `ICommandAuthorizationEvaluator` into rendered components. Render-time probe sets cached flag; submit-time check stays authoritative. Inline / compact-inline / full-page trigger `Disabled` binding consults the flag. Emit appropriate `SourceSurface.{Inline,CompactInline,FullPage}Action`. Substantial scope; chains naturally with DN4. **Owner:** Dedicated dev session. Sources: A2 / Pass-2 DN3. Reconciliation: Row: DW-0164; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:1602

### DW-896: `CapabilityDiscoveryEffects` filter (DN4 re-defer) [needs effect change + 4-6 tests] — Mirror `CommandPaletteEffects.CanSurfaceCommandAsync` shape. Emit `SourceSurface.HomeCapability`. Owner: Same dev session as DN3. Sources: A3 / Pass-2 DN4. Reconciliation: Row: DW-0165; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 7-3-command-authorization-policies (Pass 3, 2026-05-02)"), 2026-08-27
location: CapabilityDiscoveryEffects
reason: **DF16 — `CapabilityDiscoveryEffects` filter (DN4 re-defer)** [needs effect change + 4-6 tests] — Mirror `CommandPaletteEffects.CanSurfaceCommandAsync` shape. Emit `SourceSurface.HomeCapability`. **Owner:** Same dev session as DN3. Sources: A3 / Pass-2 DN4. Reconciliation: Row: DW-0165; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/11-15-storage-scope-and-snapshot-publisher-consolidation.md:42

### DW-897: `FcAuthorizedCommandRegion` Shell component (DN5 re-defer) [needs new `.razor` + `.razor.cs` + `.razor.css` + replace `<AuthorizeView Policy="@policy">` in `FcProjectionEmptyPlaceholder.razor`] — Wraps `ICommandAuthorizationEvaluator` with `Authorized`/`NotAuthorized`/`Pending` render fragments. Eliminates the resource-shape divergence between CTA and form/palette/home (AC12 mandates "same policy semantics as command renderers"). Owner: Dedicated dev session. Sources: A4 / Pass-2 DN5. Reconciliation: Row: DW-0166; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 7-3-command-authorization-policies (Pass 3, 2026-05-02)"), 2026-08-27
location: .razor.cs
reason: **DF17 — `FcAuthorizedCommandRegion` Shell component (DN5 re-defer)** [needs new `.razor` + `.razor.cs` + `.razor.css` + replace `<AuthorizeView Policy="@policy">` in `FcProjectionEmptyPlaceholder.razor`] — Wraps `ICommandAuthorizationEvaluator` with `Authorized`/`NotAuthorized`/`Pending` render fragments. Eliminates the resource-shape divergence between CTA and form/palette/home (AC12 mandates "same policy semantics as command renderers"). **Owner:** Dedicated dev session. Sources: A4 / Pass-2 DN5. Reconciliation: Row: DW-0166; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionEmptyPlaceholder.razor:1

### DW-898: Generated/Authorization bUnit gating suite (P3 re-defer) [needs DF15/DN3 to land first; bUnit fixtures must exercise the actual generated trigger gating] — Tests under `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Authorization/*.cs` proving inline / compact / full-page / CTA / palette / home surfaces are non-executable for unauthorized users with zero side effects (no `SubmittedAction` dispatch, no pending registration, no `ICommandService.DispatchAsync` call). Owner: Chained after DF15. Sources: A6 / Pass-2 P3. Reconciliation: Row: DW-0167; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 7-3-command-authorization-policies (Pass 3, 2026-05-02)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Generated/Authorization/*.cs
reason: **DF18 — Generated/Authorization bUnit gating suite (P3 re-defer)** [needs DF15/DN3 to land first; bUnit fixtures must exercise the actual generated trigger gating] — Tests under `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Authorization/*.cs` proving inline / compact / full-page / CTA / palette / home surfaces are non-executable for unauthorized users with zero side effects (no `SubmittedAction` dispatch, no pending registration, no `ICommandService.DispatchAsync` call). **Owner:** Chained after DF15. Sources: A6 / Pass-2 P3. Reconciliation: Row: DW-0167; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:1623

### DW-899: Full failure-matrix coverage (P4 re-defer) [needs DF15/DN3 to land first] — Tenant-switch-after-render, sign-out-after-render, destructive-dialog-delay (between auth#1 and auth#2), palette-route-opened-after-auth-change scenarios. Owner: Chained after DF15. Sources: A7 / Pass-2 P4. Reconciliation: Row: DW-0168; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 7-3-command-authorization-policies (Pass 3, 2026-05-02)"), 2026-08-27
location: n/a
reason: **DF19 — Full failure-matrix coverage (P4 re-defer)** [needs DF15/DN3 to land first] — Tenant-switch-after-render, sign-out-after-render, destructive-dialog-delay (between auth#1 and auth#2), palette-route-opened-after-auth-change scenarios. **Owner:** Chained after DF15. Sources: A7 / Pass-2 P4. Reconciliation: Row: DW-0168; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:1630

### DW-909: Build-time HFC for missing host policy catalog reference [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/`] — Task T2 sub-bullet `[x]` was over-claimed; runtime startup validator covers AC7 alternatively. A build-time variant would require host-supplied assembly attribute (e.g., `[assembly: FrontComposerKnownPolicy("OrderApprover")]`) or generated host metadata seam. Owner: Out of scope Pass 3; Story 9-1 (Build-Time Drift Detection). Sources: A14 / A22. Reconciliation: Row: DW-0178; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.4 SourceTools/schema-fingerprint follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.4 SourceTools/schema-fingerprint follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 7-3-command-authorization-policies (Pass 3, 2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.SourceTools/Diagnostics/
reason: **DF29 — Build-time HFC for missing host policy catalog reference** [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/`] — Task T2 sub-bullet `[x]` was over-claimed; runtime startup validator covers AC7 alternatively. A build-time variant would require host-supplied assembly attribute (e.g., `[assembly: FrontComposerKnownPolicy("OrderApprover")]`) or generated host metadata seam. **Owner:** Out of scope Pass 3; Story 9-1 (Build-Time Drift Detection). Sources: A14 / A22. Reconciliation: Row: DW-0178; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.4 SourceTools/schema-fingerprint follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.4 SourceTools/schema-fingerprint follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-28
archived: 2026-09-18
decision: 2026-08-28 Close as accepted — Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.
resolution: closed by human decision: Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.
decision: 2026-08-28 Close as accepted — Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.

### DW-923: Cached entry with control-character ETag silently filtered, never removed from LRU [`EventStoreQueryClient.cs:121-124`] — Orphan entry takes quota; never participates in `If-None-Match`. Owner: ETag cache hygiene backlog. Sources: E29. Reconciliation: Row: DW-0192; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 7-3-command-authorization-policies (Pass 3, 2026-05-02)"), 2026-08-27
location: EventStoreQueryClient.cs:121-124
reason: **D12 — Cached entry with control-character ETag silently filtered, never removed from LRU** [`EventStoreQueryClient.cs:121-124`] — Orphan entry takes quota; never participates in `If-None-Match`. **Owner:** ETag cache hygiene backlog. Sources: E29. Reconciliation: Row: DW-0192; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:143-153 removes a cached control-character ETag with CancellationToken.None; commit 652c68f3ae944af997c04fe5ec3d56a3caf2908b introduced the eviction.

### DW-924: `EventStoreIdentity.RequireUserContext(snapshot)` re-runs `RequireValidSegment` on already-validated fields [`EventStoreIdentity.cs:13-18`] — Dead defensive code; remove with D3. Sources: B26. Reconciliation: Row: DW-0193; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 7-3-command-authorization-policies (Pass 3, 2026-05-02)"), 2026-08-27
location: EventStoreIdentity.cs:13-18
reason: **D13 — `EventStoreIdentity.RequireUserContext(snapshot)` re-runs `RequireValidSegment` on already-validated fields** [`EventStoreIdentity.cs:13-18`] — Dead defensive code; remove with D3. Sources: B26. Reconciliation: Row: DW-0193; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreIdentity.cs:13-18 returns the already-validated snapshot fields and documents removal of redundant validation.

### DW-934: WASM-friendly `IUserContextAccessor` adapter (resolves DN1) [`src/Hexalith.FrontComposer.Shell/Services/Auth/`] — `AuthenticationStateUserContextAccessor.cs` was removed in this story (it was unregistered, dangerous via `.GetAwaiter().GetResult()` deadlock surface, and its WASM seam responsibility is out of Story 7-1 scope). Standalone WASM hosts in v1 supply their own `IUserContextAccessor` per AC12 ("FrontComposer consumes the platform auth/token abstraction supplied by the host"). Owner: Story 7-2 (tenant context propagation) — that story is the natural place to introduce a typed WASM accessor with a deadlock-safe path (cache scope-local `AuthenticationState`). Reconciliation: Row: DW-0203; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 7-1-oidc-saml-authentication-integration (2026-05-01)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Services/Auth/
reason: **D9 — WASM-friendly `IUserContextAccessor` adapter (resolves DN1)** [`src/Hexalith.FrontComposer.Shell/Services/Auth/`] — `AuthenticationStateUserContextAccessor.cs` was removed in this story (it was unregistered, dangerous via `.GetAwaiter().GetResult()` deadlock surface, and its WASM seam responsibility is out of Story 7-1 scope). Standalone WASM hosts in v1 supply their own `IUserContextAccessor` per AC12 ("FrontComposer consumes the platform auth/token abstraction supplied by the host"). **Owner:** Story 7-2 (tenant context propagation) — that story is the natural place to introduce a typed WASM accessor with a deadlock-safe path (cache scope-local `AuthenticationState`). Reconciliation: Row: DW-0203; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-935: `FcProjectionTemplateHost.OpenComponent` non-generic overload requires `DynamicallyAccessedMembers` / generic-arity for AOT/trim [`src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs`] — `OpenComponent(0, Descriptor.TemplateType)` non-generic overload triggers trimming warnings without DAM annotations. Owner: Epic 9-4 (AOT/trimming sweep) — that story owns the project-wide annotation pass. Reconciliation: Row: DW-0204; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC5-AC13, AC26-AC29, AC33-AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs.

origin: migrated from legacy ledger ("Deferred from: code review of 6-6-build-time-validation-error-boundaries-and-diagnostics (2026-05-01)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs
reason: **D1 — `FcProjectionTemplateHost.OpenComponent` non-generic overload requires `DynamicallyAccessedMembers` / generic-arity for AOT/trim** [`src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs`] — `OpenComponent(0, Descriptor.TemplateType)` non-generic overload triggers trimming warnings without DAM annotations. **Owner:** Epic 9-4 (AOT/trimming sweep) — that story owns the project-wide annotation pass. Reconciliation: Row: DW-0204; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC5-AC13, AC26-AC29, AC33-AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionTemplateHost.cs.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateDescriptor.cs:35-47 propagates DynamicallyAccessedMembers(All) on TemplateType into FcProjectionTemplateHost.OpenComponent; commit 6a9fd9b7c44b35d65d7386b841747538e0fde849 added the trim metadata.

### DW-938: HFC1010 hot-reload classifier kept as scaffold-only; T4 effectively downgraded [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationHotReloadClassifier.cs`] — `Classify()` is a public static helper exercised only by `HotReloadRebuildClassifierTests.cs`; no SourceTools generator step or Shell startup hook calls it. Wiring requires either an `IIncrementalGenerator` step that compares prior-vs-current marker metadata, or a Shell-side descriptor-manifest gate. AnalyzerReleases row already says "Reserved — full restart required (not yet implemented)." Owner: Story 9-1 (build-time drift detection) — natural home for the incremental-generator wiring; Shell-side gate is a separate task tracked under Epic 6 retro. Reconciliation: Row: DW-0207; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2, Story 11.4; Evidence: src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationHotReloadClassifier.cs.

origin: migrated from legacy ledger ("Deferred from: code review of 6-6-build-time-validation-error-boundaries-and-diagnostics (2026-05-01)"), 2026-08-27
location: src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationHotReloadClassifier.cs
reason: **D4 — HFC1010 hot-reload classifier kept as scaffold-only; T4 effectively downgraded** [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationHotReloadClassifier.cs`] — `Classify()` is a public static helper exercised only by `HotReloadRebuildClassifierTests.cs`; no SourceTools generator step or Shell startup hook calls it. Wiring requires either an `IIncrementalGenerator` step that compares prior-vs-current marker metadata, or a Shell-side descriptor-manifest gate. AnalyzerReleases row already says "Reserved — full restart required (not yet implemented)." **Owner:** Story 9-1 (build-time drift detection) — natural home for the incremental-generator wiring; Shell-side gate is a separate task tracked under Epic 6 retro. Reconciliation: Row: DW-0207; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2, Story 11.4; Evidence: src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationHotReloadClassifier.cs.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationHotReloadClassifier.cs:1

### DW-953: Sentinel `RenderContext` synthesis in generated `RenderSlotField` (initial GD-P2 fail-closed attempt reverted) [`src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` helper body around `:518`] — Initial fail-closed patch removed the `RenderContext ?? new RenderContext(TenantId: string.Empty, ...)` synthesis on memory-rule grounds. Full-solution testing surfaced that NO production wiring (samples / Shell / Counter.Web) provides a cascading `RenderContext` ancestor; the sentinel is the canonical "anonymous preview" default. Removing it bypassed every slot in Counter sample tests (`FcFieldSlotHost.BuildRenderTree` GB-P1 null guard fired on every render). Reverted; resolution: keep sentinel + document in code. The memory rule `feedback_tenant_isolation_fail_closed` is about persistence-scope keys, not render-time UI state. Owner: Story 7-2 (tenant context propagation and isolation) — that story is the right surface to introduce a strict cascading-context check for slots when tenant isolation matters at the rendering boundary. Reconciliation: Row: DW-0222; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC5-AC13, AC26-AC29, AC33-AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement Group D (2026-05-01)"), 2026-08-27
location: src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs
reason: **GD-D3 — Sentinel `RenderContext` synthesis in generated `RenderSlotField` (initial GD-P2 fail-closed attempt reverted)** [`src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` helper body around `:518`] — Initial fail-closed patch removed the `RenderContext ?? new RenderContext(TenantId: string.Empty, ...)` synthesis on memory-rule grounds. Full-solution testing surfaced that NO production wiring (samples / Shell / Counter.Web) provides a cascading `RenderContext` ancestor; the sentinel is the canonical "anonymous preview" default. Removing it bypassed every slot in Counter sample tests (`FcFieldSlotHost.BuildRenderTree` GB-P1 null guard fired on every render). Reverted; resolution: keep sentinel + document in code. The memory rule `feedback_tenant_isolation_fail_closed` is about persistence-scope keys, not render-time UI state. **Owner: Story 7-2** (tenant context propagation and isolation) — that story is the right surface to introduce a strict cascading-context check for slots when tenant isolation matters at the rendering boundary. Reconciliation: Row: DW-0222; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC5-AC13, AC26-AC29, AC33-AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-978: Query response `MaxResponseBytes` guard remains unimplemented [`src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`] — Story 5-6 added telemetry around query outcomes and preserved Story 5-2 no-churn semantics, but did not add a response-size rejection path because that would change the HTTP/query classification contract and requires an option default, migration guidance, and AOT/governance review. Owner: Story 9-4 diagnostic/governance/AOT cleanup. Remaining risk: a hostile or buggy EventStore endpoint can return an oversized 200 OK body before JSON parsing fails; telemetry must continue to avoid logging response bodies or raw exception messages. Reconciliation: Row: DW-0247; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.7; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.7.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.7; Evidence: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs.

origin: migrated from legacy ledger ("Deferred from: 5-6-build-time-infrastructure-enforcement-and-observability (2026-04-26 bmad-dev-story)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs
reason: **5-6-DF1 — Query response `MaxResponseBytes` guard remains unimplemented** [`src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`] — Story 5-6 added telemetry around query outcomes and preserved Story 5-2 no-churn semantics, but did not add a response-size rejection path because that would change the HTTP/query classification contract and requires an option default, migration guidance, and AOT/governance review. **Owner: Story 9-4 diagnostic/governance/AOT cleanup.** Remaining risk: a hostile or buggy EventStore endpoint can return an oversized 200 OK body before JSON parsing fails; telemetry must continue to avoid logging response bodies or raw exception messages. Reconciliation: Row: DW-0247; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.7; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.7.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.7; Evidence: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:1

### DW-980: Query response body parsed without `MaxResponseBytes` cap [`src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:79-80`] — `JsonDocument.ParseAsync(stream, ...)` will buffer a hostile or buggy server's multi-GB response. Spec only enforces request body limit (D12). Add `MaxResponseBytes` option in Story 5-2 (response handling) or Story 5-6 (governance/observability). Reconciliation: Row: DW-0249; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: EventStoreClientTests.QueryClient_RejectsOversizedResponseBody_BeforeParsing; Residual release-gate risk: none.; Evidence: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:79-80.

origin: migrated from legacy ledger ("Deferred from: code review of 5-1-eventstore-service-abstractions (2026-04-25 bmad-code-review)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:79-80
reason: **DF2 — Query response body parsed without `MaxResponseBytes` cap** [`src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:79-80`] — `JsonDocument.ParseAsync(stream, ...)` will buffer a hostile or buggy server's multi-GB response. Spec only enforces request body limit (D12). Add `MaxResponseBytes` option in Story 5-2 (response handling) or Story 5-6 (governance/observability). Reconciliation: Row: DW-0249; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: EventStoreClientTests.QueryClient_RejectsOversizedResponseBody_BeforeParsing; Residual release-gate risk: none.; Evidence: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:79-80.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs:79

### DW-981: `ContractsAssembly_DoesNotReferenceInfrastructurePackages` substring match against `"Hosting"` and `"EventStore"` [`tests/Hexalith.FrontComposer.Contracts.Tests/Communication/EventStoreContractTests.cs:54-62`] — could false-positive on benign assembly names like `EventStoreHelpers.Abstractions`. Tighten to an exact deny-list when Story 9-1 (drift detection) lands. Reconciliation: Row: DW-0250; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: EventStoreContractTests.ContractsAssembly_DoesNotReferenceInfrastructurePackages; Residual release-gate risk: none.; Related: Story 11.4; Evidence: tests/Hexalith.FrontComposer.Contracts.Tests/Communication/EventStoreContractTests.cs:54-62.

origin: migrated from legacy ledger ("Deferred from: code review of 5-1-eventstore-service-abstractions (2026-04-25 bmad-code-review)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Contracts.Tests/Communication/EventStoreContractTests.cs:54-62
reason: **DF3 — `ContractsAssembly_DoesNotReferenceInfrastructurePackages` substring match against `"Hosting"` and `"EventStore"`** [`tests/Hexalith.FrontComposer.Contracts.Tests/Communication/EventStoreContractTests.cs:54-62`] — could false-positive on benign assembly names like `EventStoreHelpers.Abstractions`. Tighten to an exact deny-list when Story 9-1 (drift detection) lands. Reconciliation: Row: DW-0250; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: EventStoreContractTests.ContractsAssembly_DoesNotReferenceInfrastructurePackages; Residual release-gate risk: none.; Related: Story 11.4; Evidence: tests/Hexalith.FrontComposer.Contracts.Tests/Communication/EventStoreContractTests.cs:54-62.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Contracts.Tests/Communication/EventStoreContractTests.cs:54

### DW-984: `catch when ex is not OutOfMemoryException && ex is not StackOverflowException` SOE filter is cargo-cult

origin: migrated from legacy ledger ("Deferred from: code review of 3-7-command-palette-e2e-and-scorer-bench (2026-04-25 bmad-code-review)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs
reason: **`catch when ex is not OutOfMemoryException && ex is not StackOverflowException` SOE filter is cargo-cult** [`src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs` multiple sites: 909, 960, 979, 1049, 1118, 1157, 1208] — `StackOverflowException` is uncatchable in managed code since .NET 2.0; adding it to the filter signals misunderstanding of CSEs. Pre-existing pattern from Story 3-4 Pass-3; tracked for Epic 9-4 governance analyzer cleanup. Reconciliation: Row: DW-0253; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.6 Shell/sample/accessibility follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.6 Shell/sample/accessibility follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:194 and sibling catches use !ExceptionGuard.IsFatal(ex); commit db9ba9ee4 replaced the cargo-cult exception filter.

### DW-992: `PaletteResultsComputedAction.Results` Query field invariant fragile under future canonicalisation

origin: migrated from legacy ledger ("Deferred from: code review of 3-4-fccommandpalette-and-keyboard-shortcuts — Pass 6 / Chunk 2 (2026-04-25 bmad-code-review)"), 2026-08-27
location: CommandPaletteActions.cs
reason: **`PaletteResultsComputedAction.Results` Query field invariant fragile under future canonicalisation** [`CommandPaletteActions.cs`] — Reducer's stale-query guard requires `state.Query == action.Query`; doc-only tightening for v1.x. Reconciliation: Row: DW-0261; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC18, AC21-AC22, AC37; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: CommandPaletteActions.cs.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteActions.cs:34-40 preserves the original query invariant alongside computed results.

### DW-999: `OnParametersSet` sync-only override on `FcProjectionEmptyPlaceholder`

origin: migrated from legacy ledger ("Deferred from: code review of 4-6-empty-states-field-descriptions-and-unsupported-types — Pass 3 (2026-04-25 bmad-code-review)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionEmptyPlaceholder.razor.cs:107-117
reason: **`OnParametersSet` sync-only override on `FcProjectionEmptyPlaceholder`** [`src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionEmptyPlaceholder.razor.cs:107-117`] — Future async-friendly resolver becomes a breaking interface change. Architectural concern, not a current bug. Reconciliation: Row: DW-0268; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionEmptyPlaceholder.razor.cs:107-117.
status: done 2026-08-28
archived: 2026-09-18
decision: 2026-08-28 Accept current behavior — Record the current behavior as intentional and close DW-999 with its verified rationale.
resolution: closed by human decision: Record the current behavior as intentional and close DW-999 with its verified rationale.
decision: 2026-08-28 Accept current behavior — Record the current behavior as intentional and close DW-999 with its verified rationale.

### DW-1001: `RegisterDomain` not thread-safe with resolver's `_registry.GetManifests()` enumeration

origin: migrated from legacy ledger ("Deferred from: code review of 4-6-empty-states-field-descriptions-and-unsupported-types — Pass 3 (2026-04-25 bmad-code-review)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:13
reason: **`RegisterDomain` not thread-safe with resolver's `_registry.GetManifests()` enumeration** [`src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:13, 93-107`] — Raw `List<T>` returned by `GetManifests`; concurrent `RegisterDomain` (e.g., add-on registering from a hosted service) could throw `InvalidOperationException("Collection was modified")` during resolver enumeration. Pre-existing risk amplified by frequent registry walks under Story 4-6. Reconciliation: Row: DW-0270; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC14-AC16, AC30; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:13, 93-107.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:130 snapshots manifests under the registry lock before enumeration.

### DW-1007: Snapshot files lost UTF-8 BOM after regeneration

origin: migrated from legacy ledger ("Deferred from: code review of 4-6-empty-states-field-descriptions-and-unsupported-types (2026-04-25)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/
reason: **Snapshot files lost UTF-8 BOM after regeneration** [multiple `.verified.txt` under `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/`] — environment/tooling regression (text-editor or `dotnet test` regeneration encoding); not a Story 4-6 logic defect. Investigate the regeneration toolchain or accept the new BOM-less convention for verified.txt files. Reconciliation: Row: DW-0276; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC12, AC23, AC29, AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: section: code review of 4-6-empty-states-field-descriptions-and-unsupported-types (2026-04-25).
status: done 2026-08-28
archived: 2026-09-18
decision: 2026-08-28 Accept current behavior — Record the current behavior as intentional and close DW-1007 with its verified rationale.
resolution: closed by human decision: Record the current behavior as intentional and close DW-1007 with its verified rationale.
decision: 2026-08-28 Accept current behavior — Record the current behavior as intentional and close DW-1007 with its verified rationale.

### DW-1011: Duplicate `AppInitializedAction` on Blazor Server prerender + interactive double-fire

origin: migrated from legacy ledger ("Deferred from: code review of 3-5-home-directory-badge-counts-and-new-capability-discovery.md (2026-04-22)"), 2026-08-27
location: 3-5-home-directory-badge-counts-and-new-capability-discovery.md
reason: **Duplicate `AppInitializedAction` on Blazor Server prerender + interactive double-fire** — `CapabilityDiscoveryEffects.HandleAppInitialized` runs hydrate + seed twice. Covered by Story 3-6 `StorageReadyAction` + ScopeFlipObserverEffect (ADR-049). Reconciliation: Row: DW-0280; Superseded by Story 3.6 ScopeFlipObserverEffect 2026-05-13; Disposition: superseded; Validation: existing Story 3.6 persistence/scope-flip tests; Residual release-gate risk: none.; Evidence: section: code review of 3-5-home-directory-badge-counts-and-new-capability-discovery.md (2026-04-22).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2414

### DW-1012: Post-auth scope-flip (anonymous hydrate → authenticated persist) rehydrate missing

origin: migrated from legacy ledger ("Deferred from: code review of 3-5-home-directory-badge-counts-and-new-capability-discovery.md (2026-04-22)"), 2026-08-27
location: 3-5-home-directory-badge-counts-and-new-capability-discovery.md
reason: **Post-auth scope-flip (anonymous hydrate → authenticated persist) rehydrate missing** — user logs in after anonymous hydrate; persist writes post-login scope without rehydrating. Covered by Story 3-6 ScopeFlipObserverEffect. Reconciliation: Row: DW-0281; Superseded by Story 3.6 ScopeFlipObserverEffect 2026-05-13; Disposition: superseded; Validation: existing Story 3.6 persistence/scope-flip tests; Residual release-gate risk: none.; Evidence: section: code review of 3-5-home-directory-badge-counts-and-new-capability-discovery.md (2026-04-22).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2421

### DW-1022: AC7 behavioural test: home card `@onclick` / `@onkeydown` silently dropped by Razor compiler

origin: migrated from legacy ledger ("Deferred from: code review of 3-5-home-directory-badge-counts-and-new-capability-discovery.md (2026-04-22)"), 2026-08-27
location: FcHomeCard.razor
reason: ~~**AC7 behavioural test: home card `@onclick` / `@onkeydown` silently dropped by Razor compiler**~~ — **RESOLVED in the same review session.** Extracted `FcHomeCard` child component (`Components/Home/FcHomeCard.razor` + `.razor.cs`); `@onclick` / `@onkeydown` now emit as proper Blazor event bindings. Behavioural test `ActivatesCardViaEnterKey_DispatchesVisitedAction_ThenNavigates` passes. Reconciliation: Row: DW-0291; Resolved 2026-04-22; Evidence: section: code review of 3-5-home-directory-badge-counts-and-new-capability-discovery.md (2026-04-22); Original review source/date preserved.
status: done 2026-04-22
archived: 2026-09-18
resolution: **RESOLVED in the same review session.** Extracted `FcHomeCard` child component (`Components/Home/FcHomeCard.razor` + `.razor.cs`); `@onclick` / `@onkeydown` now emit as proper Blazor event bindings. Behavioural test `ActivatesCardViaEnterKey_DispatchesVisitedAction_ThenNavigates` passes. Reconciliation: Row: DW-0291; Resolved 2026-04-22; Evidence: section: code review of 3-5-home-directory-badge-counts-and-new-capability-discovery.md (2026-04-22); Original review source/date preserved

### DW-1023: Invalid ARIA listbox structure

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 1 review (2026-04-21 pass 5)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor:25-64
reason: **Invalid ARIA listbox structure** — `<ul role="listbox">` contains `<li role="none">` wrapping `<h4>` + nested `<ul role="group">`. WAI-ARIA 1.2 prefers direct `<li role="option">` or direct `<li role="group">` children. Deferred to Story 10-2 a11y pipeline alongside forced-colors / RTL verification. `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor:25-64` Reconciliation: Row: DW-0292; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC18, AC21-AC22, AC37; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 1 review (2026-04-21 pass 5).
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor:26-35 nests role=option elements under a labelled role=group within the listbox.

### DW-1024: Task 10.7 bUnit test matrix still at 5 of 9+ shipped

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 1 review (2026-04-21 pass 5)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs
reason: **Task 10.7 bUnit test matrix still at 5 of 9+ shipped** — carried forward from Pass 2/3/4 F-03. Missing: `ArrowDownDispatchesSelectionMoved`, `ArrowUpDispatchesSelectionMoved`, `EnterDispatchesActivation`, `EscapeClosesPalette`, `AriaLiveAnnouncesNoMatchesForEmptyResults`, `FocusManagement_ArrowsKeepFocusOnSearchInput`, `FocusManagement_EscapeRestoresFocusToInvoker`, `FocusManagement_ActivateSentinelDoesNotClosePalette`, `PaletteDismissPaths_AllDispatchPaletteClosedAction`. Landing with the Aspire MCP Playwright matrix follow-up per DN3. `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs` Reconciliation: Row: DW-0293; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.5; AC coverage: AC18, AC21-AC22, AC37; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.5.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.5; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 1 review (2026-04-21 pass 5).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs:1

### DW-1026: `NavigationManager.ToAbsoluteUri(targetUrl)` throws on malformed `targetUrl`

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 1 review (2026-04-21 pass 5)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs:422
reason: **`NavigationManager.ToAbsoluteUri(targetUrl)` throws on malformed `targetUrl`** — RouteUrls come from the trusted registry; not reachable with realistic data. Revisit if adopter-supplied URLs flow into this path. `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs:422` Reconciliation: Row: DW-0295; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 1 review (2026-04-21 pass 5).
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: CommandPaletteEffects now validates the target with IsInternalRoute and catches navigation invalid-operation failures; the former ToAbsoluteUri call is gone.

### DW-1027: `StubBadgeService` test stub comparer semantics diverge from production `IBadgeCountService` contract

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 1 review (2026-04-21 pass 5)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPaletteResultListTests.cs
reason: **`StubBadgeService` test stub comparer semantics diverge from production `IBadgeCountService` contract** — production implementation lands with Story 3-5; test stub can be aligned then. `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPaletteResultListTests.cs:~1293` Reconciliation: Row: DW-0296; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 1 review (2026-04-21 pass 5).
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPaletteResultListTests.cs:246-249 builds the stub dictionary with EqualityComparer<Type>.Default, matching production semantics.

### DW-1028: Badge `Counts` dictionary concurrent-mutation read race during `OnNext` re-render

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 1 review (2026-04-21 pass 5)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor:51
reason: **Badge `Counts` dictionary concurrent-mutation read race during `OnNext` re-render** — `IBadgeCountService` contract in Contracts does not mandate immutable-snapshot semantics; Story 3-5 picks the concrete implementation and can guarantee either (a) immutable snapshot on `CountChanged` push, or (b) `ConcurrentDictionary` read safety. `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor:51` Reconciliation: Row: DW-0297; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 1 review (2026-04-21 pass 5).
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs:68,104 exposes an atomically replaced ImmutableDictionary snapshot.

### DW-1031: `NavigateHomeAsync` `JSDisconnectedException` on dead circuit

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 3 re-review (2026-04-21 pass 4)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **`NavigateHomeAsync` `JSDisconnectedException` on dead circuit** — Blazor error boundary + the HFC2109 handler-fault catch already absorb; additional `try/catch` is defensive noise. Reconciliation: Row: DW-0300; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 3 re-review (2026-04-21 pass 4).
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:214-231 invokes the shortcut handler and awaits it inside one try/catch, so a synchronous NavigateTo/JSDisconnectedException is contained and logged.

### DW-1032: `OpenSettingsAsync` synchronous throw

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 3 re-review (2026-04-21 pass 4)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **`OpenSettingsAsync` synchronous throw** — `FcSettingsDialogLauncher` internal wrapping covers the common cases; out-of-scope for chunk-3 hardening. Reconciliation: Row: DW-0301; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 3 re-review (2026-04-21 pass 4).
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:214-231 catches failures from both synchronous OpenSettings invocation and the returned asynchronous operation.

### DW-1039: `CommandRouteBuilder.BuildRoute` URL-reserved characters in segment input

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 3 re-review (2026-04-21 pass 4)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **`CommandRouteBuilder.BuildRoute` URL-reserved characters in segment input** — impossible per C# identifier rules; PascalCase type names cannot contain `/ ? # &` etc. Reconciliation: Row: DW-0308; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 3 re-review (2026-04-21 pass 4).
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Contracts/Navigation/GeneratedCommandRoute.cs:41-68 sanitizes and validates route segments before construction.

### DW-1040: `_disposed` race in `RegistrationDisposable.Dispose` when owner already disposed

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 3 re-review (2026-04-21 pass 4)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **`_disposed` race in `RegistrationDisposable.Dispose` when owner already disposed** — `TryGetValue` on cleared `ConcurrentDictionary` is benign (returns `false`); no functional impact. The race exists but has no adverse consequence. Revisit only if lifecycle diagnostics need stricter contracts. Reconciliation: Row: DW-0309; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts — Chunk 3 re-review (2026-04-21 pass 4).
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:289-295 makes RegistrationDisposable.Dispose idempotent with Interlocked.Exchange.

### DW-1042: AC2/AC5/D11 bUnit matrix deferred

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs
reason: **AC2/AC5/D11 bUnit matrix deferred** — `FcCommandPaletteTests` ships 5 tests; the spec named ~13 component-level tests covering arrow-key navigation dispatch, enter/escape activation, focus management (F1–F5), live-region "no matches" text, and `PaletteDismissPaths_AllDispatchPaletteClosedAction` Theory. Reducer-level coverage is in place. Deferred per DAR Review Findings as a follow-up coverage pass. `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs` Reconciliation: Row: DW-0311; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs:1

### DW-1043: `ShortcutService.Dispose` race with pending chord timer

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs
reason: **`ShortcutService.Dispose` race with pending chord timer** — `_chordTimer` callback can take `_chordSync` lock after `Dispose` returned; functionally harmless because the generation guard absorbs any late timer fire, but the race window exists. `FakeTimeProvider` in tests never exercises the real threading path. Revisit only if real threaded TimeProvider is added. `src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:~3410-3440` Reconciliation: Row: DW-0312; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21).
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs:139-179 and :194-204 serialize timer callbacks and disposal through _chordSync while the disposed flag prevents post-dispose timer allocation.

### DW-1044: Prerender-only scope resolution retry

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs
reason: **Prerender-only scope resolution retry** — if `IUserContextAccessor` returns no tenant/user on the prerender pass (server-side circuit pre-sign-in), `CommandPaletteEffects.HandleAppInitialized` silently skips hydrate. A later `PaletteScopeChangedAction` only re-runs hydrate if the reducer has cleared the ring buffer — fail-closed is the correct floor today. Story 3-6 (session-persistence / context-restoration) will introduce a proper `StorageReady` event that re-fires hydrate on late scope resolution. `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:~112-155` Reconciliation: Row: DW-0313; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21).
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:161-168 retries hydration on StorageReadyAction, and lines 720-735 rehydrate after a scope change.

### DW-1045: `BoundedContextRouteParser` returns null for bare `/domain/{bc}` landing

origin: migrated from legacy ledger ("Deferred from: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextRouteParser.cs
reason: **`BoundedContextRouteParser` returns null for bare `/domain/{bc}` landing** — 2-segment domain path resolves to `null`, meaning the contextual-bonus cannot fire on a bare BC landing page. By design until bounded-context landing pages exist; projection/command resolution requires the 3rd segment. Revisit if Epic 3 adds `/domain/{bc}` landing routes. `src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextRouteParser.cs:~44-49` Reconciliation: Row: DW-0314; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21).
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/State/Navigation/BoundedContextRouteParser.cs:38-42 explicitly accepts two-segment /domain/{bc} landing routes.

### DW-1048: Drawer-open state not reconciled on Tablet→Desktop viewport transitions

origin: migrated from legacy ledger ("Deferred from: code review of story 3-2-sidebar-navigation-and-responsive-behavior (2026-04-19)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor.cs
reason: **Drawer-open state not reconciled on Tablet→Desktop viewport transitions** — the drawer UI hides because `FcHamburgerToggle.IsVisible` flips false on the transition, but no effect clears the in-flight drawer-open state. Cosmetic; no incorrect data exposed to the user. Revisit if UX receives reports of "ghost drawer" on rapid resize. `src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor.cs` Reconciliation: Row: DW-0317; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-2-sidebar-navigation-and-responsive-behavior (2026-04-19).
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Components/Layout/FcHamburgerToggle.razor:10-16 documents the superseding tier-specific design, and lines 22-40 render mutually exclusive Desktop versus responsive drawer branches so the drawer instance is removed on Tablet-to-Desktop.

### DW-1049: `InvalidTierIsIgnored` test relies on bUnit loose-JS coercion

origin: migrated from legacy ledger ("Deferred from: code review of story 3-2-sidebar-navigation-and-responsive-behavior (2026-04-19)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcLayoutBreakpointWatcherTests.cs
reason: **`InvalidTierIsIgnored` test relies on bUnit loose-JS coercion** — `subscribe` mock returns `true` which bUnit coerces (and the subsequent cast-to-`IJSObjectReference` exception is silently swallowed by the C# catch block). The passing assertion (`CurrentViewport == Desktop` after a `999` dispatch) is therefore a side-effect of the failed subscribe path rather than a clean assertion of the invalid-tier-rejection guard. Test-quality improvement only. `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcLayoutBreakpointWatcherTests.cs` Reconciliation: Row: DW-0318; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 3-2-sidebar-navigation-and-responsive-behavior (2026-04-19).
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcLayoutBreakpointWatcherTests.cs:117-128 invokes OnViewportTierChangedAsync(999) directly and asserts the state remains Desktop, independent of JS coercion.

### DW-1052: Destructive renderer emitter snapshot coverage

origin: migrated from legacy ledger ("Deferred from: code review of story 2-5 full adversarial pass (2026-04-17)"), 2026-08-27
location: tests/.../CommandRendererEmitterTests.cs
reason: **Destructive renderer emitter snapshot coverage** — `CommandRendererEmitterTests` never passes `isDestructive: true`; `DestructiveBeforeSubmitAsync`, `_dialogOpen`, and `IDialogService` injection have no snapshot. Authoring the snapshots requires a destructive command test model + 8+ rendering permutations. Significant scoping work; track as a dedicated snapshot-hardening pass. `tests/.../CommandRendererEmitterTests.cs` Reconciliation: Row: DW-0321; Split 2026-05-12; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC18, AC21-AC22, AC37; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: Story 11.4 added destructive metadata drift coverage (`DriftClassifierMetadataTests.CommandContractMetadataChange_EmitsSingleMetadataDiagnostic`) but did not broaden renderer snapshot approval corpus; Release risk: low for drift release-readiness, medium for renderer visual regression; Reopen trigger: Story 11.6 snapshot/accessibility coverage pass.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs:167 exercises isDestructive:true and lines 175-191 assert the destructive dialog/emitter paths.

### DW-1060: `OnFirstEdit` re-mount arm gap

origin: migrated from legacy ledger ("Deferred from: code review of story 2-5 full adversarial pass (2026-04-17)"), 2026-08-27
location: FcFormAbandonmentGuard.razor.cs
reason: **`OnFirstEdit` re-mount arm gap** — guard unsubscribes `OnFieldChanged` after first fire; if the same `EditContext` reference is reused across mounts the guard never re-arms. Emitter creates a fresh `EditContext` in `OnInitialized` making exact reuse unlikely in practice. `FcFormAbandonmentGuard.razor.cs` Reconciliation: Row: DW-0329; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC18, AC21-AC22, AC37; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 2-5 full adversarial pass (2026-04-17).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor.cs:78 re-subscribes whenever the prior subscription was cleared; a remount also creates a fresh component instance.

### DW-1061: Planning monolith paths and external bookmarks

origin: migrated from legacy ledger ("Deferred from: code review of tasks-subtasks.md (story 2-5) (2026-04-17)"), 2026-08-27
location: _bmad-output/planning-artifacts/prd.md
reason: **Planning monolith paths and external bookmarks** — Replacing single-file `_bmad-output/planning-artifacts/prd.md`, `epics.md`, `ux-design-specification.md`, and research monoliths with sharded folders can break external deep links and old citations until referrers are updated. Tracked as deferral from review; no runtime code change required. Reconciliation: Row: DW-0330; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of tasks-subtasks.md (story 2-5) (2026-04-17).
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current verified behavior as intentional and close the deferred row.
decision: 2026-08-27 Accept current behavior — Record the current verified behavior as intentional and close the deferred row.

### DW-1063: DisplayLabel unsupported end-to-end

origin: migrated from legacy ledger ("Deferred from: code review of story files 1-3/1-4/1-5/1-6/1-7 (2026-04-14)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-4-drift-detection-and-source-generator-coverage-hardening.md
reason: **DisplayLabel unsupported end-to-end** — `BoundedContextAttribute` has no `DisplayLabel` property; parser, IR, and transform all lack support. Story 1-5 review finding acknowledged. No current story owns the fix. Reconciliation: Row: DW-0332; Resolved 2026-05-14; Decision owner: FrontComposer SourceTools maintainers; Evidence: `BoundedContextAttribute.DisplayLabel`, `AttributeParser`, `CommandParser`, `DomainModel`, `RegistrationModelTransform`, `RegistrationEmitter`, `AttributeParserTests.Parse_DisplayLabelProjection_ExtractsDisplayLabel`, `RegistrationModelTransformTests.Transform_DisplayLabel_PropagatedFromDomainModel`, `GeneratorDriverTests.RunGenerators_DisplayLabel_PropagatedToRegistration`, and `_bmad-output/implementation-artifacts/11-4-drift-detection-and-source-generator-coverage-hardening.md`; Downstream impact: generated registration metadata now carries DisplayLabel; Reopen trigger: DisplayLabel propagation regresses in parser, transform, or emitter tests; Previous owner was Story 11.4.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2779

### DW-1064: Namespace-collision-safe source naming

origin: migrated from legacy ledger ("Deferred from: code review of story files 1-3/1-4/1-5/1-6/1-7 (2026-04-14)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-4-drift-detection-and-source-generator-coverage-hardening.md
reason: **Namespace-collision-safe source naming** — Generated source keys rely on simple TypeName, which breaks for same-named projections in different namespaces. Story 1-5 review finding acknowledged. No current story addresses this. Reconciliation: Row: DW-0333; Resolved 2026-05-14; Decision owner: FrontComposer SourceTools maintainers; Evidence: `FrontComposerGenerator.GetQualifiedHintPrefix`, command `.Command` hint suffix, `GeneratorDriverTests.RunGenerators_SameNameDifferentNamespace_NoHintNameCollision`, `GeneratorDriverTests.RunGenerators_GlobalNamespaceProjection_HintNameHasNoNamespacePrefix`, and `_bmad-output/implementation-artifacts/11-4-drift-detection-and-source-generator-coverage-hardening.md`; Downstream impact: same-simple-name projections no longer collide in generated source hints; Reopen trigger: generated hint names lose namespace qualification or same-name fixture coverage; Previous owner was Story 11.4.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2786

### DW-1065: Generated Fluxor actions missing CorrelationId

origin: migrated from legacy ledger ("Deferred from: code review of story files 1-3/1-4/1-5/1-6/1-7 (2026-04-14)"), 2026-08-27
location: FluxorActionsEmitter.cs
reason: ~~**Generated Fluxor actions missing CorrelationId**~~ — RESOLVED 2026-04-14 in Story 2-1 Task 0.5. `FluxorActionsEmitter.cs` now emits `CorrelationId` as the leading parameter on `LoadRequestedAction`, `LoadedAction`, and `LoadFailedAction`. Snapshot (`FluxorActionsEmitterTests.Actions_Snapshot.verified.txt`) re-approved; `CounterStoryVerificationTests` updated to pass a generated CorrelationId to each dispatch. Reconciliation: Row: DW-0334; Resolved 2026-04-14; Evidence: section: code review of story files 1-3/1-4/1-5/1-6/1-7 (2026-04-14); Original review source/date preserved.
status: done 2026-04-14
archived: 2026-09-18
resolution: RESOLVED 2026-04-14 in Story 2-1 Task 0.5. `FluxorActionsEmitter.cs` now emits `CorrelationId` as the leading parameter on `LoadRequestedAction`, `LoadedAction`, and `LoadFailedAction`. Snapshot (`FluxorActionsEmitterTests.Actions_Snapshot.verified.txt`) re-approved; `CounterStoryVerificationTests` updated to pass a generated CorrelationId to each dispatch. Reconciliation: Row: DW-0334; Resolved 2026-04-14; Evidence: section: code review of story files 1-3/1-4/1-5/1-6/1-7 (2026-04-14); Original review source/date preserved

### DW-1066: Release workflow NuGet push ordering risk

origin: migrated from legacy ledger ("Deferred from: code review of story files 1-3/1-4/1-5/1-6/1-7 (2026-04-14)"), 2026-08-27
location: n/a
reason: **Release workflow NuGet push ordering risk** — In semantic-release plugin chain, `@semantic-release/exec` (NuGet push) runs before `@semantic-release/github` (GitHub Release). If GitHub Release fails, packages are already on NuGet with no rollback. Matches the EventStore pattern — a known inherited risk. Reconciliation: Row: DW-0335; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: CiGovernanceTests.ReleaseWorkflow_AddsSbomSigningAttestationAndManifestGatesAfterBlockingTests; Residual release-gate risk: partial-publish reconciliation remains explicit evidence, not silent pass.; Evidence: section: code review of story files 1-3/1-4/1-5/1-6/1-7 (2026-04-14).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2801

### DW-1068: BoundedContext name with invalid C# identifier chars

origin: migrated from legacy ledger ("Deferred from: code review of story 1-5 round 2 (2026-04-14)"), 2026-08-27
location: RegistrationEmitter
reason: **BoundedContext name with invalid C# identifier chars** — Names like `"My Orders"` or `"Order-Management"` produce invalid class names in `RegistrationEmitter`. Pre-existing; no sanitization or diagnostic exists. Reconciliation: Row: DW-0337; Split to Story 11.4 SourceTools identifier diagnostics 2026-05-13; Disposition: split-to-named-story; Reason: generator malformed-name diagnostics are SourceTools scope; Residual release-gate risk: medium for exotic adopters.; Evidence: section: code review of story 1-5 round 2 (2026-04-14).
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs:36-42 derives the generated class identifier from model.TypeName, while lines 63-65 emit BoundedContext only as an escaped string; commit e378cef841962717ef55960ae7f1fdf583fe8670 introduced the type-derived registration class name.

### DW-1070: XML doc comment escaping

origin: migrated from legacy ledger ("Deferred from: code review of story 1-5 round 2 (2026-04-14)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-4-drift-detection-and-source-generator-coverage-hardening.md
reason: **XML doc comment escaping** — BoundedContext name with `<`, `>`, or `&` chars is placed into XML doc comments without escaping, potentially breaking doc tooling. Pre-existing across all emitters. Reconciliation: Row: DW-0339; Resolved 2026-05-14; Decision owner: FrontComposer SourceTools maintainers; Evidence: `RegistrationEmitter.XmlEscape`, `GeneratorDriverTests.RunGenerators_BoundedContextXmlCharacters_EscapesRegistrationXmlDoc`, and `_bmad-output/implementation-artifacts/11-4-drift-detection-and-source-generator-coverage-hardening.md`; Downstream impact: generated registration XML documentation remains valid for XML-sensitive bounded-context metadata; Reopen trigger: generated XML doc output reintroduces unescaped bounded-context values; Previous owner was Story 11.4.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2829

### DW-1071: Incremental generator caching edge case

origin: migrated from legacy ledger ("Deferred from: code review of story 1-5 round 2 (2026-04-14)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-4-drift-detection-and-source-generator-coverage-hardening.md
reason: **Incremental generator caching edge case** — Changing only `DisplayLabel` on `[BoundedContext]` in a separate partial declaration may not trigger re-generation if `[Projection]` is on a different partial. Speculative; needs investigation. Reconciliation: Row: DW-0340; Accepted constraint 2026-05-14; Decision owner: FrontComposer SourceTools maintainers; Evidence: DisplayLabel participates in `DomainModel`, `RegistrationModel`, equality/hash paths, focused SourceTools regression coverage, and `_bmad-output/implementation-artifacts/11-4-drift-detection-and-source-generator-coverage-hardening.md`; Likelihood: low; Impact: medium if a partial metadata-only edit ever goes stale; Release risk: low until a partial-only DisplayLabel edit regression is observed; Downstream impact: hot reload/incremental-edit scenarios only; Review trigger: incremental cache test exposes stale registration output for partial metadata-only edits; Previous owner was Story 11.4.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs:87,107 and 306,328 include BoundedContextDisplayLabel in equality and hashing for both incremental models.

### DW-1073: Shallow clone + 3-level submodule nesting may cause CI failures

origin: migrated from legacy ledger ("Deferred from: code review of 1-7-ci-pipeline-and-semantic-release.md (2026-04-14)"), 2026-08-27
location: n/a
reason: **Shallow clone + 3-level submodule nesting may cause CI failures** — FrontComposer → EventStore → Tenants chain with `fetch-depth: 1` + `submodules: recursive` is fragile when gitlinks point to non-HEAD commits in deeply nested submodules. Pre-existing architecture. Reconciliation: Row: DW-0342; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2850

### DW-1074: CI and Release race on push to main

origin: migrated from legacy ledger ("Deferred from: code review of 1-7-ci-pipeline-and-semantic-release.md (2026-04-14)"), 2026-08-27
location: n/a
reason: **CI and Release race on push to main** — Both workflows trigger on `push: branches: [main]`. CI has `cancel-in-progress: true`, Release has `false`. When advisory mode is removed in Epic 2, a fast-follow merge could ship un-CI-validated code. Needs `workflow_run` or `needs:` dependency. Reconciliation: Row: DW-0343; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2857

### DW-1075: @semantic-release/git push may fail with persist-credentials: false

origin: migrated from legacy ledger ("Deferred from: code review of 1-7-ci-pipeline-and-semantic-release.md (2026-04-14)"), 2026-08-27
location: n/a
reason: **@semantic-release/git push may fail with persist-credentials: false** — Release checkout uses `persist-credentials: false` with manual remote URL token injection. If `@semantic-release/git` uses a different push mechanism, the CHANGELOG commit push will fail. Will surface on first release attempt. Reconciliation: Row: DW-0344; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2864

### DW-1077: HFC1010 analyzer implementation

origin: migrated from legacy ledger ("Deferred from: Story 1-8 (Hot Reload & Fluent UI Contingency) — 2026-04-14"), 2026-08-27
location: DiagnosticDescriptors.cs
reason: **HFC1010 analyzer implementation** — HFC1010 is reserved as a comment in `DiagnosticDescriptors.cs` and a table row in `AnalyzerReleases.Unshipped.md` (Story 1.8 AC4). No `DiagnosticDescriptor` field because the check requires diffing the previous compilation against the current one, which is an analyzer concern, not a generator one. Until implemented, `RS2002` is suppressed in `Hexalith.FrontComposer.SourceTools.csproj`. Reconciliation: Row: DW-0346; Split 2026-05-12; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2, Story 11.4; Evidence: `CustomizationHotReloadClassifierTests`, `DriftDiagnosticCatalogTests` HFC1010 allocation guard, and SourceTools regression passed; Release risk: low for drift/generator hardening because HFC1010 wiring is hot-reload/governance scope; Reopen trigger: Story 11.6 or Story 11.2 wires classifier emission or changes HFC1010 registry policy.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs:1

### DW-1078: RS2002 guard test

origin: migrated from legacy ledger ("Deferred from: Story 1-8 (Hot Reload & Fluent UI Contingency) — 2026-04-14"), 2026-08-27
location: Hexalith.FrontComposer.SourceTools.cs
reason: **RS2002 guard test** — `NoWarn;RS2002` is project-wide in `Hexalith.FrontComposer.SourceTools.csproj`, which silences RS2002 for any _future_ DiagnosticDescriptor added without an `AnalyzerReleases.Unshipped.md` entry. Add a unit test that enumerates all `DiagnosticDescriptor` fields via reflection and asserts each diagnostic ID has a matching row in `AnalyzerReleases.Shipped.md` + `AnalyzerReleases.Unshipped.md`. Retires the blanket suppression once implemented. Reconciliation: Row: DW-0347; Resolved 2026-05-14; Decision owner: FrontComposer SourceTools maintainers; Evidence: `DiagnosticRegistryTests.HfcmMigrationFindings_AreCliGovernedNotRoslynReleaseRows`, descriptor release-row guard tests, `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj` no longer containing `RS2002`, and `_bmad-output/implementation-artifacts/11-4-drift-detection-and-source-generator-coverage-hardening.md`; Downstream impact: future real DiagnosticDescriptor IDs are release-row guarded; Reopen trigger: project-wide RS2002 suppression returns or descriptor release-row parity tests are removed; Previous owner was Story 11.4.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj:1

### DW-1080: AC3 density and ARIA live regions

origin: migrated from legacy ledger ("Deferred from: code review of story 2-1-command-form-generation-and-field-type-inference.md (2026-04-14)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **AC3 density and ARIA live regions** — Story 2.1 AC3 calls for Comfortable density defaults and `aria-describedby` / `aria-live="polite"` on validation messaging. The generated form sets wrapper `aria-label`, per-field labels, and inline `Message`/`MessageState` for numeric parse failures, but does not explicitly set density or region-level `aria-live`. Deferred until someone verifies Fluent UI v5 `FluentTextInput` / `FluentValidationSummary` / `EditForm` defaults (or adds targeted attributes + a bUnit/axe assertion). Reconciliation: Row: DW-0349; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC18, AC21-AC22, AC37; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 2-1-command-form-generation-and-field-type-inference.md (2026-04-14).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2899

### DW-1081: CI: isolate performance tests when advisory mode ends

origin: migrated from legacy ledger ("Deferred from: code review of story 1-8 — third review (2026-04-14)"), 2026-08-27
location: .github/workflows/ci.yml
reason: **CI: isolate performance tests when advisory mode ends** — Today `continue-on-error: true` on the whole `build-and-test` job makes benchmark regressions and functional failures equally non-blocking. When Epic 2 removes that flag, add a dedicated test step filtered with `--filter Category=Performance` and its own `continue-on-error: true` so only perf flakes stay advisory. Documented inline in `.github/workflows/ci.yml` (Gate 3 comment); tracked here so Epic 2 CI work does not lose the intent. Reconciliation: Row: DW-0350; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: CiGovernanceTests.QuarantineLane_IsWarningOnlyAndPublishesBoundedEvidence and BlockingTestLanes_ExcludeQuarantinedTestsWithoutSkippingGovernance; Residual release-gate risk: none.; Evidence: section: code review of story 1-8 — third review (2026-04-14).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-ci-package-boundary-fluent-pin.md:54

### DW-1082: AC3 accessibility verification — axe-core + manual smoke

origin: migrated from legacy ledger ("Deferred from: code review of story 2-1 (2026-04-15)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **AC3 accessibility verification — axe-core + manual smoke** — Task 9.3/9.4 (manual Counter.Web smoke test of the full 5-state lifecycle, axe-core scan for zero serious/critical violations) were not executed in the 2026-04-14 implementation pass. Gate the follow-up behind the Aspire MCP + Chrome MCP automation path so it stays automated (per user preference). Coverage for AC3 remains unverified end-to-end. Reconciliation: Row: DW-0351; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.5; AC coverage: AC17-AC20, AC35; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.5.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.5; Evidence: section: code review of story 2-1 (2026-04-15).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2913

### DW-1086: Parent-driven `OnParametersSet` on generated form

origin: migrated from legacy ledger ("Deferred from: code review of story 2-1 (2026-04-15)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Parent-driven `OnParametersSet` on generated form** — When a parent passes a changing `InitialValue`, the emitted form does not re-initialize `_model`. v0.1 has no documented parent-driven reinit requirement; revisit if adopters need it. Reconciliation: Row: DW-0355; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC12, AC23, AC29, AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 2-1 (2026-04-15).
status: done 2026-08-28
archived: 2026-09-18
decision: 2026-08-28 Accept current behavior — Record the current behavior as intentional and close DW-1086 with its verified rationale.
resolution: closed by human decision: Record the current behavior as intentional and close DW-1086 with its verified rationale.
decision: 2026-08-28 Accept current behavior — Record the current behavior as intentional and close DW-1086 with its verified rationale.

### DW-1088: Dead `NumberStyles_Any` helper property

origin: migrated from legacy ledger ("Deferred from: code review of story 2-1 (2026-04-15)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Dead `NumberStyles_Any` helper property** — Emitted per numeric field but never referenced. Code-gen cleanup only. Reconciliation: Row: DW-0357; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 2-1 (2026-04-15).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 8eef1b6a

### DW-1092: HFC1015 analyzer emission

origin: migrated from legacy ledger ("Deferred from: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **HFC1015 analyzer emission** — Story 2-2 ships runtime `ILogger.LogWarning` only; compile-time analyzer reporting of RenderMode/density mismatch is Epic 9 scope (Story 9.4). Reconciliation: Row: DW-0361; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC12, AC23, AC29, AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: section: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2983

### DW-1093: Destructive command Danger-appearance + confirmation dialog UX

origin: migrated from legacy ledger ("Deferred from: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Destructive command Danger-appearance + confirmation dialog UX** — Story 2-5 owns. Story 2-2 renderer never emits `Appearance.Danger`. Reconciliation: Row: DW-0362; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2990

### DW-1094: 30-second form abandonment warning (UX-DR38)

origin: migrated from legacy ledger ("Deferred from: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **30-second form abandonment warning (UX-DR38)** — Story 2-5 owns. Story 2-2's FullPage renderer has no abandonment guard. Reconciliation: Row: DW-0363; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:2997

### DW-1095: DataGridNavigationState effects (persistence, hydration, beforeunload flush, capture-side DataGrid wiring)

origin: migrated from legacy ledger ("Deferred from: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **DataGridNavigationState effects (persistence, hydration, beforeunload flush, capture-side DataGrid wiring)** — Story 4.3 owns. Story 2-2 ships reducers only (Decision D30); `RestoreGridStateAction` dispatch from FullPage page is a no-op on empty state. Reconciliation: Row: DW-0364; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:3004

### DW-1096: Full-featured FluentUI v5 chrome (FluentButton appearance literals, FluentPopover, FluentBreadcrumb, leading icons)

origin: migrated from legacy ledger ("Deferred from: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Full-featured FluentUI v5 chrome (FluentButton appearance literals, FluentPopover, FluentBreadcrumb, leading icons)** — Story 2-2 MVP emits plain HTML for the renderer chrome to keep the generator vocabulary domain-pure (Counter.Domain does not carry FluentUI primitives beyond what Story 2-1 already needed). Adopter overrides via partial classes + Story 3.1 (shell layout) will re-enable Fluent primitives end-to-end. Reconciliation: Row: DW-0365; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC17-AC20, AC35; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-1097: IExpandInRowJSModule scoped cache

origin: migrated from legacy ledger ("Deferred from: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **IExpandInRowJSModule scoped cache** — Decision D25's scoped-Lazy cache was not wired into the generated renderer (the renderer imports the module inline per instance). The Shell-side `IExpandInRowJSModule` + `ExpandInRowJSModule` class remain registered for adopters that want to preload via hand-written code; migrating generated renderers to consume this service requires moving the interface into Contracts (adds ASP.NET ElementReference dependency) — deferred. Reconciliation: Row: DW-0366; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC12, AC23, AC29, AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcExpandInRowDetail.razor.cs:35 injects IExpandInRowJSModule and line 81 initializes it; generated snapshots contain FcExpandInRowDetail.

### DW-1098: Story 2-1 CommandFormEmitter .verified.txt snapshots

origin: migrated from legacy ledger ("Deferred from: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Story 2-1 CommandFormEmitter .verified.txt snapshots** — Task 5.2 and Task 5.3 called for "12 existing .verified.txt snapshots" to be re-approved; those files don't exist in the repo. Regression coverage runs through existing `CommandFormTransformTests` (3 tests updated for D23 label) and generator integration tests. True byte-snapshot coverage for CommandFormEmitter is deferred to whenever `.verified.txt` snapshots are introduced for the Form pipeline. Reconciliation: Row: DW-0367; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC12, AC23, AC29, AC34; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt:1 and CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt:1 are byte snapshots for the form pipeline.

### DW-1099: HFC1015 renumber — Story spec used HFC1008 for "RenderMode incompatible with density". Story 2-1 already owns HFC1008 for `[Flags]`-enum properties; density-mismatch renumbered to HFC1015. Story file updated globally. Reconciliation: Row: DW-0368; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: section: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15.

origin: migrated from legacy ledger ("Deferred from: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **HFC1015 renumber** — Story spec used HFC1008 for "RenderMode incompatible with density". Story 2-1 already owns HFC1008 for `[Flags]`-enum properties; density-mismatch renumbered to HFC1015. Story file updated globally. Reconciliation: Row: DW-0368; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: section: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:3032

### DW-1100: Story 2-2 Task 10 (37 bUnit tests), Task 11 (10 emitter snapshot/parseability/determinism/contract tests), Task 12 (3 axe-core scans)

origin: migrated from legacy ledger ("Deferred from: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Story 2-2 Task 10 (37 bUnit tests), Task 11 (10 emitter snapshot/parseability/determinism/contract tests), Task 12 (3 axe-core scans)** — deferred to Session C continuation of Story 2-2 dev. Story 2-2 stays `in-progress` until all three ship. Current cumulative coverage: 330/330 tests green (Contracts 12, Shell 82, SourceTools 236). **Updated 2026-04-16 (Session D):** Tasks 10.1–10.5 + 11.1–11.4 + 12.1 landed (30 net-new tests, cumulative 410 green). Spec'd 121 net-new not reached because several scenarios depend on infrastructure not in MVP — listed below. Reconciliation: Row: DW-0369; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC18, AC21-AC22, AC37; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: section: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:3039

### DW-1101: Task 13.3 automated E2E via Aspire MCP + Claude browser

origin: migrated from legacy ledger ("Deferred from: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15"), 2026-08-27
location: 2-2-e2e-results.json
reason: **Task 13.3 automated E2E via Aspire MCP + Claude browser** — single authoritative E2E path not yet executed in this dev session (required 10 scenarios + machine-readable `2-2-e2e-results.json` artifact). Deferred to Session C alongside bUnit/axe tests. **Still deferred after Session D** — needs a dedicated Counter-sample-up-via-Aspire pass. Reconciliation: Row: DW-0370; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.5; AC coverage: AC17-AC20, AC35; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.5.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.5; Evidence: section: Story 2-2 (Action Density Rules & Rendering Modes) — 2026-04-15.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/2-2-e2e-results.json:1

### DW-1103: FluentUI v5 RC2 satellite icons not shipped — every renderer falls back to a null `Icon` at runtime

origin: migrated from legacy ledger ("Deferred from: Story 2-2 Session D (2026-04-16) — bUnit-vs-spec gap"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **FluentUI v5 RC2 satellite icons not shipped — every renderer falls back to a null `Icon` at runtime** (`Renderer_Inline_LeadingIconPresent`, `Renderer_FullPage_LeadingIconPresent`, `Renderer_IconFallback_*` for the actual icon-render path). The renderer's `TryResolveIcon` now uses the v5 nested-`Icons+Variant+SizeNN+Name` type path (Session-D patch), so when v5 GA ships the satellite icons or when adopters reference `Microsoft.FluentUI.AspNetCore.Components.Icons.Regular` the icons will resolve. The warning-on-bad-name path (`IconFallback...AndLogs`) was retained because it is observable without the icon types being loadable. Reconciliation: Row: DW-0372; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC18, AC21-AC22, AC37; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: Story 2-2 Session D (2026-04-16) — bUnit-vs-spec gap.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:3060

### DW-1105: Renderer Escape handler for CompactInline (`Renderer_CompactInline_EscapeInvokesOnCollapseRequested`)

origin: migrated from legacy ledger ("Deferred from: Story 2-2 Session D (2026-04-16) — bUnit-vs-spec gap"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Renderer Escape handler for CompactInline (`Renderer_CompactInline_EscapeInvokesOnCollapseRequested`)** — the renderer wires the JS Escape handler only on the Inline popover wrapper. Bringing CompactInline collapse under the same handler needs a `OnCollapseRequested` event-callback hookup + DataGrid-row coordination; deferred until Story 4.5 (`expand-in-row-detail`). Reconciliation: Row: DW-0374; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: Story 2-2 Session D (2026-04-16) — bUnit-vs-spec gap.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:3074

### DW-1107: Manual keyboard walk-through (Task 12.2)

origin: migrated from legacy ledger ("Deferred from: Story 2-2 Session D (2026-04-16) — bUnit-vs-spec gap"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Manual keyboard walk-through (Task 12.2)** + **axe-core scan on Counter (Task 13.5)** — both deferred together with Task 13.3 to the Counter-sample E2E pass; bUnit-side keyboard surface is covered by `KeyboardTabOrderTests` and a11y surface by `AxeCoreA11yTests`. Reconciliation: Row: DW-0376; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC17-AC20, AC35; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: Story 2-2 Session D (2026-04-16) — bUnit-vs-spec gap.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:3088

### DW-1111: Derived record shadows base `[DerivedFrom]`

origin: migrated from legacy ledger ("Deferred from: code review of story 2-1 (2026-04-16)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Derived record shadows base `[DerivedFrom]`** — Property walk in `CommandParser` uses `seenNames.Add` (first-declared wins); `AttributeParser.ParsePropertyForCommand` reads attributes only off the most-derived symbol. A derived record that re-declares the same property name silently drops base-declared `[DerivedFrom]` attributes. Revisit when a real derivation chain with attribute inheritance lands. Reconciliation: Row: DW-0380; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC12, AC23, AC29, AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 2-1 (2026-04-16).
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Parsers/CommandParser.cs:491-504 walks overridden and shadowed base properties when resolving derived metadata.

### DW-1113: Nested command types (`Outer.InnerCommand`) emission

origin: migrated from legacy ledger ("Deferred from: code review of story 2-1 (2026-04-16)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Nested command types (`Outer.InnerCommand`) emission** — Hint-prefix / namespace emission hasn't been audited for nested `[Command]` types. Counter sample doesn't exercise nesting. Add an HFC1004-style diagnostic if nesting is unsupported, or prove correctness with a test. Reconciliation: Row: DW-0382; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC17-AC20, AC35; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: section: code review of story 2-1 (2026-04-16).
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs:87-102 emits HFC1014 and halts parsing for nested commands; tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandDensityTests.cs:157-174 pins it.
decision: 2026-09-06 Implement the change — Implement the behavior described by DW-1113 at its recorded touchpoint, update affected contracts and consumers, and add focused regression evidence.
decision: 2026-09-06 Implement the change — Implement the behavior described by DW-1113 at its recorded touchpoint, update affected contracts and consumers, and add focused regression evidence.

### DW-1115: W1 [HIGH] `DataGridNavigationReducers.Cap` static mutable cross-tenant leak

origin: migrated from legacy ledger ("Deferred from: code review of story 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group A (Contracts) chunk"), 2026-08-27
location: DataGridNavigationReducers.cs:23
severity: high
reason: **W1 [HIGH] `DataGridNavigationReducers.Cap` static mutable cross-tenant leak** — `Shell/State/DataGridNavigation/DataGridNavigationReducers.cs:23` declares `Cap` as `public static int { get; set; }`. Two tenants in the same process configure different `FcShellOptions.DataGridNavCap`; whichever `PostConfigure` runs last wins for everyone. Aligns with the project memory "Per-user persistence services must fail-closed on missing tenant/user." Fix: move `Cap` into per-circuit state or read it from `IOptions<FcShellOptions>` inside the reducer. Reconciliation: Row: DW-0384; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group A (Contracts) chunk.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationReducers.cs:23

### DW-1116: W2 [MED] `DataGridNavigationReducers.ReduceCapture` cap=0 destroys all snapshots silently

origin: migrated from legacy ledger ("Deferred from: code review of story 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group A (Contracts) chunk"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
severity: medium
reason: **W2 [MED] `DataGridNavigationReducers.ReduceCapture` cap=0 destroys all snapshots silently** — `while (next.Count > cap)` loop drains the entire dictionary on every Capture if `Cap ≤ 0`. Consequence of P4 (no range guard on `DataGridNavCap`). Once P4 lands at the contract layer, this becomes unreachable; until then, add a defensive `if (cap <= 0) return state;` in the reducer. Reconciliation: Row: DW-0385; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group A (Contracts) chunk.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:3152

### DW-1117: W3 [LOW] LRU tie-breaking nondeterministic on equal `CapturedAt`

origin: migrated from legacy ledger ("Deferred from: code review of story 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group A (Contracts) chunk"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
severity: low
reason: **W3 [LOW] LRU tie-breaking nondeterministic on equal `CapturedAt`** — `ImmutableDictionary` enumeration order is unspecified, so eviction picks an arbitrary entry on ties. Cross-platform flaky-test risk. Fix: use `<=` with a tiebreaker (e.g., key ordinal) or capture `Stopwatch.GetTimestamp()` for sub-tick uniqueness. Reconciliation: Row: DW-0386; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: section: code review of story 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group A (Contracts) chunk.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:3160

### DW-1118: W4 [LOW] LRU eviction is O(N²) per overflow capture

origin: migrated from legacy ledger ("Deferred from: code review of story 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group A (Contracts) chunk"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
severity: low
reason: **W4 [LOW] LRU eviction is O(N²) per overflow capture** — full scan + immutable rebuild per call. Fine for default Cap=50 but pathological for bulk replay (Fluxor effect re-hydrate, server restore). Fix: maintain a parallel min-heap or a `ImmutableSortedDictionary<DateTimeOffset, string>` index. Reconciliation: Row: DW-0387; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group A (Contracts) chunk.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs:323-340 uses ConcurrentQueue dequeue eviction rather than repeated full scans and rebuilds.

### DW-1120: W6 [LOW] `System.Collections.Immutable` `<PackageReference>` lacks explicit `Version`

origin: migrated from legacy ledger ("Deferred from: code review of story 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group A (Contracts) chunk"), 2026-08-27
location: Hexalith.FrontComposer.Contracts.cs
severity: low
reason: **W6 [LOW] `System.Collections.Immutable` `<PackageReference>` lacks explicit `Version`** — `Hexalith.FrontComposer.Contracts.csproj:6-8` adds the package only for netstandard2.0 with no version pin, relying on Central Package Management in `Directory.Packages.props`. Already in effect per other commits, so deterministic now; revisit if CPM is ever disabled or the package is added in a project without CPM. Reconciliation: Row: DW-0389; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: section: code review of story 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group A (Contracts) chunk.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: Directory.Packages.props:11 imports the centralized catalog, and references/Hexalith.Builds/Props/Directory.Packages.props:299 pins System.Collections.Immutable 10.0.11.

### DW-1123: `netstandard2.0` compile-path depends on an `IsExternalInit` polyfill for `init` setters

origin: migrated from legacy ledger ("Deferred from: code review of story 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group A (Contracts) second pass"), 2026-08-27
location: Hexalith.FrontComposer.Contracts.cs
reason: **`netstandard2.0` compile-path depends on an `IsExternalInit` polyfill for `init` setters** — build is green, but the `Hexalith.FrontComposer.Contracts.csproj:6-8` only declares `System.Collections.Immutable` + `System.ComponentModel.Annotations`. Confirm the shim source (inherited from `Directory.Build.props`? `PolySharp`?) and pin explicitly before publishing the Contracts package externally; consumers that disable Central Package Management otherwise break. Reconciliation: Row: DW-0392; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC5-AC13, AC26-AC29, AC33-AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of story 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group A (Contracts) second pass.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Contracts/Internals/IsExternalInit.cs:10

### DW-1128: [HIGH] `LastUsedSubscriberEmitter` does not pass `CancellationToken` to `RecordAsync`

origin: migrated from legacy ledger ("Deferred from: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group B (Shell + Tests + Counter sample) chunk"), 2026-08-27
location: src/Hexalith.FrontComposer.SourceTools/Emitters/LastUsedSubscriberEmitter.cs:93
severity: high
reason: **[HIGH] `LastUsedSubscriberEmitter` does not pass `CancellationToken` to `RecordAsync`** — `src/Hexalith.FrontComposer.SourceTools/Emitters/LastUsedSubscriberEmitter.cs:93` emits `await _recorder.RecordAsync<TCommand>(command).ConfigureAwait(false);` with no token argument, binding to the Group B–added optional parameter as `CancellationToken.None`. CT plumbing from Group B is inert in production until the emitter passes a meaningful token (e.g. from the subscriber's scoped `CancellationToken` / circuit-teardown linked source). **Defer target:** SourceTools group code review for story 2-2. **Scope:** update the emitter to thread a `CancellationToken` into `RecordAsync`, re-approve any affected emitter snapshot tests. Reconciliation: Row: DW-0397; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC17-AC20, AC35; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: section: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group B (Shell + Tests + Counter sample) chunk.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Emitters/LastUsedSubscriberEmitter.cs:98

### DW-1129: [MED] Icon resolution uses reflection + assembly probing

origin: migrated from legacy ledger ("Deferred from: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group C (SourceTools layer) chunk"), 2026-08-27
location: CommandRendererEmitter.cs:833-864
severity: medium
reason: **[MED] Icon resolution uses reflection + assembly probing** — `CommandRendererEmitter.cs:833-864` builds an assembly-qualified name via string concatenation and resolves with `Type.GetType` + `Activator.CreateInstance`. AOT-hostile under trimmed WASM; documented FluentUI v5 RC2 workaround but no Known Gaps entry exists. **Defer target:** Epic 9 AOT pass; add Known Gaps entry. Reconciliation: Row: DW-0398; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC12, AC23, AC29, AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: section: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group C (SourceTools layer) chunk.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs:411-420 resolves icons through the static FcFluentIcons catalog without reflection or assembly probing.

### DW-1131: [MED] `ClosePopoverAsync` delegates scroll+focus ordering to Shell `fc-expandinrow.js`

origin: migrated from legacy ledger ("Deferred from: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group C (SourceTools layer) chunk"), 2026-08-27
location: fc-expandinrow.js
severity: medium
reason: **[MED] `ClosePopoverAsync` delegates scroll+focus ordering to Shell `fc-expandinrow.js`** — AC9 scroll-then-focus contract is enforced in Shell JS, not the emitter. **Defer target:** Group D (Shell JS) review to verify the helper preserves scroll-then-focus order and handles hidden-element edge cases. Reconciliation: Row: DW-0400; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC12, AC23, AC29, AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: section: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group C (SourceTools layer) chunk.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-expandinrow.js:15-16 performs scrollIntoView before focus.

### DW-1134: [LOW] `EscapeString` helper diverges between `CommandPageEmitter` and `CommandFormEmitter`

origin: migrated from legacy ledger ("Deferred from: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group C (SourceTools layer) chunk"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
severity: low
reason: **[LOW] `EscapeString` helper diverges between `CommandPageEmitter` and `CommandFormEmitter`** — consistency refactor. **Defer target:** next emitter cleanup pass. Reconciliation: Row: DW-0403; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC12, AC23, AC29, AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: section: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group C (SourceTools layer) chunk.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandPageEmitter.cs:115

### DW-1135: [LOW] HFC1016 diagnostic added outside the spec-declared "4 new diagnostics" set

origin: migrated from legacy ledger ("Deferred from: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group C (SourceTools layer) chunk"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
severity: low
reason: **[LOW] HFC1016 diagnostic added outside the spec-declared "4 new diagnostics" set** — defensible defense-in-depth against init-only derivable records but a spec-surface scope expansion. **Defer target:** spec patch to enumerate HFC1016; no code change required. Reconciliation: Row: DW-0404; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC12, AC23, AC29, AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2, Story 11.4; Evidence: section: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group C (SourceTools layer) chunk.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/3-1-generate-a-command-form-from-a-command-type.md:190-192 now explicitly enumerates HFC1016 in the parser contract.
decision: 2026-08-28 Implement requested change — Implement DW-1135 at its recorded touchpoint, update affected contracts and consumers, and add focused regression evidence.
decision: 2026-08-28 Implement requested change — Implement DW-1135 at its recorded touchpoint, update affected contracts and consumers, and add focused regression evidence.

### DW-1137: [HIGH → deferred] `TrySetPropertyValue` compile-time switch refactor (D9)

origin: migrated from legacy ledger ("Deferred from: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group C (SourceTools layer) chunk"), 2026-08-27
location: CommandRendererEmitter.cs:138-182
severity: high
reason: **[HIGH → deferred] `TrySetPropertyValue` compile-time switch refactor (D9)** — `CommandRendererEmitter.cs:138-182` uses runtime `PropertyInfo.GetProperty` + `Convert.ChangeType` per derivable-field pre-fill. Spec Task 4.3 L712 mandates a compile-time per-property switch for AOT/trim safety. Interim patches applied: narrowed `catch` to `InvalidCastException | FormatException | OverflowException | ArgumentException` + `CurrentCulture` alignment with form-numeric binding. **Defer target:** dedicated SourceTools refactor task — augment `CommandRendererModel` with `EquatableArray<PropertyModel>` DerivableProperties (name + fully-qualified type), update `CommandRendererTransform`, rewrite `TrySetPropertyValue` as typed switch, regenerate all 8 renderer verified snapshots. Reconciliation: Row: DW-0406; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC18, AC21-AC22, AC37; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: section: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group C (SourceTools layer) chunk.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs:343-349 emits a typed property-name switch; CommandRendererEmitterTests.cs:148-154 reject reflection.

### DW-1138: [MED → deferred] `RefreshDerivedValuesBeforeSubmitAsync` writes `_prefilledModel` not form `_model` (P30)

origin: migrated from legacy ledger ("Deferred from: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group C (SourceTools layer) chunk"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
severity: medium
reason: **[MED → deferred] `RefreshDerivedValuesBeforeSubmitAsync` writes `_prefilledModel` not form `_model` (P30)** — pre-submit refresh mutates the renderer prefill while the form has already copied to its own `_model`. Correct fix requires the renderer to pass a model-mutation delegate into the form's `BeforeSubmit`, which is an architectural change best made alongside Story 2-3's lifecycle-state work. Interim workaround: `InitialValue` on `OnInitialized` carries derived values into the form's initial `_model`, so steady-state values are correct on first submit. Reconciliation: Row: DW-0407; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC12, AC23, AC29, AC34; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: section: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group C (SourceTools layer) chunk.
status: done 2026-08-28
archived: 2026-09-18
decision: 2026-08-28 Accept current behavior — Record the current behavior as intentional and close DW-1138 with its verified rationale.
resolution: closed by human decision: Record the current behavior as intentional and close DW-1138 with its verified rationale.
decision: 2026-08-28 Accept current behavior — Record the current behavior as intentional and close DW-1138 with its verified rationale.

### DW-1143: [LOW] `LastUsedSubscriberRegistry` scope-resolution ordering

origin: migrated from legacy ledger ("Deferred from: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group D (Shell services + Fluxor state + JS module) chunk"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
severity: low
reason: **[LOW] `LastUsedSubscriberRegistry` scope-resolution ordering** — subscriber resolved in registry's own scope rather than caller's; cross-scope leak possible if a scoped subscriber ever gains scoped deps. **Defer target:** Epic 9 DI hygiene pass. Reconciliation: Row: DW-0412; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group D (Shell services + Fluxor state + JS module) chunk.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:277-279 registers both the concrete registry and interface as Scoped; src/Hexalith.FrontComposer.Shell/Services/LastUsedSubscriberRegistry.cs:20,37 resolves subscribers from that same scope.

### DW-1144: [LOW] `FrontComposerStorageKey.TryParse` returns URL-encoded segments — naming footgun

origin: migrated from legacy ledger ("Deferred from: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group D (Shell services + Fluxor state + JS module) chunk"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
severity: low
reason: **[LOW] `FrontComposerStorageKey.TryParse` returns URL-encoded segments — naming footgun** — `Tenant`/`User` fields on parse result are canonicalized, not original. **Defer target:** API-clarity polish pass. Reconciliation: Row: DW-0413; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group D (Shell services + Fluxor state + JS module) chunk.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/FrontComposerStorageKey.cs:89-107 explicitly names the returned encoded segments TenantCanon/UserCanon and documents canonical round-trip semantics.
decision: 2026-08-27 Implement change — Implement the behavior requested by DW-1144, update affected contracts and consumers, and add focused regression evidence.

### DW-1146: [LOW] `DevDiagnosticEvent.Message` verbatim forward to `ILogger`

origin: migrated from legacy ledger ("Deferred from: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group D (Shell services + Fluxor state + JS module) chunk"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
severity: low
reason: **[LOW] `DevDiagnosticEvent.Message` verbatim forward to `ILogger`** — structured logging mitigates; raw-text sinks could be tricked by CRLF injection. Parallels Group C DEF9 `NavigateToReturnPath`. **Defer target:** Epic 9 log-audit pass. Reconciliation: Row: DW-0415; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group D (Shell services + Fluxor state + JS module) chunk.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerWarningLog.cs:459

### DW-1147: [LOW] Document `focusTriggerElementById` in D11's module contract (from P60)

origin: migrated from legacy ledger ("Deferred from: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group D (Shell services + Fluxor state + JS module) chunk"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
severity: low
reason: **[LOW] Document `focusTriggerElementById` in D11's module contract (from P60)** — JS module currently exports two functions: `initializeExpandInRow` (documented in D11) and `focusTriggerElementById` (used by `CommandRendererEmitter.ClosePopoverAsync` per Group C DEF4 cross-reference). The second function is undocumented in D11's module-contract table. Spec-only patch: add `focusTriggerElementById(elementId: string): void` with the scroll-then-focus ordering guarantee. **Defer target:** spec update on D11 module contract, no code change. Reconciliation: Row: DW-0416; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC12, AC23, AC29, AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 2-2-action-density-rules-and-rendering-modes (2026-04-16) — Group D (Shell services + Fluxor state + JS module) chunk.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.UI/wwwroot/js/fc-expandinrow.js:8-17 documents and implements the focus and scroll behavior in the module contract.

### DW-1148: HFC2102 reserved but unused

origin: migrated from legacy ledger ("Deferred from: code review of story 2-4-fclifecyclewrapper-visual-lifecycle-feedback/index.md (2026-04-17)"), 2026-08-27
location: index.md
reason: ~~**HFC2102 reserved but unused**~~ — **Resolved 2026-04-17:** `FcLifecycleWrapper.OnPhaseChangedFromTimer` logs `LogDebug` with `HFC2102_ThresholdTimerOffUiThread` before `InvokeAsync` (documents thread-pool → Blazor marshal). Reconciliation: Row: DW-0417; Resolved 2026-04-17; Evidence: section: code review of story 2-4-fclifecyclewrapper-visual-lifecycle-feedback/index.md (2026-04-17); Original review source/date preserved.
status: done 2026-04-17
archived: 2026-09-18
resolution: **Resolved 2026-04-17:** `FcLifecycleWrapper.OnPhaseChangedFromTimer` logs `LogDebug` with `HFC2102_ThresholdTimerOffUiThread` before `InvokeAsync` (documents thread-pool → Blazor marshal). Reconciliation: Row: DW-0417; Resolved 2026-04-17; Evidence: section: code review of story 2-4-fclifecyclewrapper-visual-lifecycle-feedback/index.md (2026-04-17); Original review source/date preserved

### DW-1149: Story task checkboxes vs delivered work

origin: migrated from legacy ledger ("Deferred from: code review of story 2-4-fclifecyclewrapper-visual-lifecycle-feedback/index.md (2026-04-17)"), 2026-08-27
location: index.md
reason: ~~**Story task checkboxes vs delivered work**~~ — **Resolved 2026-04-17:** Note under story Tasks / Subtasks clarifies checkboxes are historical spec authoring; shipped scope is in Dev Agent Record / Completion Notes. Reconciliation: Row: DW-0418; Resolved 2026-04-17; Evidence: section: code review of story 2-4-fclifecyclewrapper-visual-lifecycle-feedback/index.md (2026-04-17); Original review source/date preserved.
status: done 2026-04-17
archived: 2026-09-18
resolution: **Resolved 2026-04-17:** Note under story Tasks / Subtasks clarifies checkboxes are historical spec authoring; shipped scope is in Dev Agent Record / Completion Notes. Reconciliation: Row: DW-0418; Resolved 2026-04-17; Evidence: section: code review of story 2-4-fclifecyclewrapper-visual-lifecycle-feedback/index.md (2026-04-17); Original review source/date preserved

### DW-1150: FsCheck property suite (Task 5.2b)

origin: migrated from legacy ledger ("Deferred from: code review of story 2-4-fclifecyclewrapper-visual-lifecycle-feedback/index.md (2026-04-17)"), 2026-08-27
location: tests/.../LifecycleThresholdTimerPropertyTests.cs
reason: ~~**FsCheck property suite (Task 5.2b)**~~ — **Resolved 2026-04-17:** `tests/.../LifecycleThresholdTimerPropertyTests.cs` — three properties (monotonic phase under random advances; final anchor vs fresh timer; pure elapsed buckets vs `CurrentPhase`). Reconciliation: Row: DW-0419; Resolved 2026-04-17; Evidence: section: code review of story 2-4-fclifecyclewrapper-visual-lifecycle-feedback/index.md (2026-04-17); Original review source/date preserved.
status: done 2026-04-17
archived: 2026-09-18
resolution: **Resolved 2026-04-17:** `tests/.../LifecycleThresholdTimerPropertyTests.cs` — three properties (monotonic phase under random advances; final anchor vs fresh timer; pure elapsed buckets vs `CurrentPhase`). Reconciliation: Row: DW-0419; Resolved 2026-04-17; Evidence: section: code review of story 2-4-fclifecyclewrapper-visual-lifecycle-feedback/index.md (2026-04-17); Original review source/date preserved

### DW-1151: Task 4 ASCII flowchart vs Decision D10

origin: migrated from legacy ledger ("Deferred from: code review of 2-3-command-lifecycle-state-management/index.md (2026-04-16)"), 2026-08-27
location: index.md
reason: ~~**Task 4 ASCII flowchart vs Decision D10**~~ — **Resolved 2026-04-16** in `2-3-command-lifecycle-state-management/index.md`: DUPLICATE cross-CorrelationId branch now matches detection-only / fresh entry; terminal retention line no longer references dropped grace window. Reconciliation: Row: DW-0420; Resolved 2026-04-16; Evidence: section: code review of 2-3-command-lifecycle-state-management/index.md (2026-04-16); Original review source/date preserved.
status: done 2026-04-16
archived: 2026-09-18
resolution: **Resolved 2026-04-16** in `2-3-command-lifecycle-state-management/index.md`: DUPLICATE cross-CorrelationId branch now matches detection-only / fresh entry; terminal retention line no longer references dropped grace window. Reconciliation: Row: DW-0420; Resolved 2026-04-16; Evidence: section: code review of 2-3-command-lifecycle-state-management/index.md (2026-04-16); Original review source/date preserved

### DW-1154: Type specimen view + Playwright screenshot diffing + `axe-core` CI

origin: migrated from legacy ledger ("Deferred from: story 3-1-shell-layout-theme-and-typography (2026-04-18)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Type specimen view + Playwright screenshot diffing + `axe-core` CI** — Story 3-1 ships the 9 `Typography` constants + mapping version pin but not the visual regression tooling. **Defer target:** Story 10-2 (accessibility CI gates). Reconciliation: Row: DW-0423; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.7; AC coverage: AC18, AC21-AC22, AC37; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.7.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.7; Evidence: section: story 3-1-shell-layout-theme-and-typography (2026-04-18).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:3447

### DW-1157: Playwright E2E smoke (Task 10.9)

origin: migrated from legacy ledger ("Deferred from: story 3-1-shell-layout-theme-and-typography (2026-04-18)"), 2026-08-27
location: ShellThemeToggleE2ETests.cs
reason: **Playwright E2E smoke (Task 10.9)** — Spec called for `ShellThemeToggleE2ETests.cs` Playwright theme-toggle + localStorage persistence + `prefers-color-scheme` emulation + `--fc-color-success` computed-style check. Conditional on Aspire MCP availability per `feedback_no_manual_validation.md`; the granular bUnit + options-validation + scope tests cover the AC matrix, but browser-level scoped-CSS resolution is only exercised manually in dev. **Defer target:** Story 10-2 (accessibility CI gates); add to Aspire MCP browser harness when the Counter.Web Aspire AppHost adds a Playwright step. Reconciliation: Row: DW-0426; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.5; AC coverage: AC17-AC20, AC35; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.5.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.5, Story 11.7; Evidence: section: story 3-1-shell-layout-theme-and-typography (2026-04-18).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/e2e/specs/settings-persistence.spec.ts:159

### DW-1159: `FrontComposerShellParameterSurfaceTests` snapshot + full bUnit render tests (Task 10.1)

origin: migrated from legacy ledger ("Deferred from: story 3-1-shell-layout-theme-and-typography (2026-04-18)"), 2026-08-27
location: FrontComposerShellParameterSurfaceTests
reason: ~~**`FrontComposerShellParameterSurfaceTests` snapshot + full bUnit render tests (Task 10.1)**~~ — **Resolved 2026-04-18 during Story 3-1 review-fix automation.** Added `FrontComposerShellTests`, `FrontComposerShellParameterSurfaceTests`, `FcThemeToggleTests`, `FcSystemThemeWatcherTests`, `LayoutComponentTestBase`, and `SlotMappingRegressionTests` to the shell test project. Reconciliation: Row: DW-0428; Resolved 2026-04-18; Evidence: section: story 3-1-shell-layout-theme-and-typography (2026-04-18); Original review source/date preserved.
status: done 2026-04-18
archived: 2026-09-18
resolution: **Resolved 2026-04-18 during Story 3-1 review-fix automation.** Added `FrontComposerShellTests`, `FrontComposerShellParameterSurfaceTests`, `FcThemeToggleTests`, `FcSystemThemeWatcherTests`, `LayoutComponentTestBase`, and `SlotMappingRegressionTests` to the shell test project. Reconciliation: Row: DW-0428; Resolved 2026-04-18; Evidence: section: story 3-1-shell-layout-theme-and-typography (2026-04-18); Original review source/date preserved

### DW-1161: Palette bUnit test matrix short (5 of 9)

origin: migrated from legacy ledger ("Deferred from: code review of 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21 pass 3)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs
reason: **Palette bUnit test matrix short (5 of 9)** — `ArrowDownDispatchesSelectionMoved`, `ArrowUpDispatchesSelectionMoved`, `EnterDispatchesActivation`, `EscapeClosesPalette`, `AriaLiveAnnouncesNoMatchesForEmptyResults`, `FocusManagement_ArrowsKeepFocusOnSearchInput`, `FocusManagement_EscapeRestoresFocusToInvoker`, `FocusManagement_ActivateSentinelDoesNotClosePalette`, `PaletteDismissPaths_AllDispatchPaletteClosedAction` remain open. **Defer target:** follow-up coverage pass paired with Aspire MCP palette verification. `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs` Reconciliation: Row: DW-0430; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.5; AC coverage: AC18, AC21-AC22, AC37; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.5.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.5; Evidence: section: code review of 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21 pass 3).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs:1

### DW-1164: Chord fallthrough re-evaluation test gap

origin: migrated from legacy ledger ("Deferred from: code review of 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21 pass 3)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Chord fallthrough re-evaluation test gap** — `ShortcutService` has a branch where the second key of a broken chord falls through to `IsChordPrefix` fresh-start path; no test proves the behaviour. **Defer target:** Task 10.1 follow-up coverage pass with FocusManagement tests. Reconciliation: Row: DW-0433; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21 pass 3).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs:283-297 pins repeated-prefix chord reevaluation; commit 6fbe67bcc7cd79ebac15691c92ab3d0026b59530.

### DW-1165: Modifier-bearing chord second-key test gap

origin: migrated from legacy ledger ("Deferred from: code review of 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21 pass 3)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **Modifier-bearing chord second-key test gap** — pressing `g` then `Ctrl+Z` (not-registered) clears pending; no test asserts the sequence. Same follow-up bucket. Reconciliation: Row: DW-0434; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 3-4-fccommandpalette-and-keyboard-shortcuts (2026-04-21 pass 3).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs:299-317 pins modifier-bearing second-key fallthrough; commit 6fbe67bcc7cd79ebac15691c92ab3d0026b59530.

### DW-1170: `HydrationState=Hydrated` on hydrate error paths (F-EH-009)

origin: migrated from legacy ledger ("Deferred from: code review of 3-6-session-persistence-and-context-restoration (2026-04-22 fresh pass)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: **`HydrationState=Hydrated` on hydrate error paths (F-EH-009)** — Transient JS-interop errors during prerender (e.g. `LocalStorageService.GetKeysAsync` throwing `InvalidOperationException` before JS is available) currently dispatch `*HydratedCompletedAction` → reducer sets `HydrationState = Hydrated` → subsequent `StorageReadyAction` re-hydrate gate short-circuits. The `ReduceNavigationHydratedCompleted` XML doc explicitly states "Called on BOTH happy path AND fail-closed path" — this is load-bearing design, not a bug. To retry on transient errors we'd need a `Failed` state (or a `Reset-to-Idle` action on specific exception types) which is a behavioral spec change. **Defer target:** dedicated design story under Epic 5 (reliable real-time experience) or Epic 9 (observability) — add telemetry first to measure how often the prerender-then-auth path actually hits this in the wild. Reconciliation: Row: DW-0439; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.7; AC coverage: AC1-AC4, AC24-AC25, AC36; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.7.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.7; Evidence: section: code review of 3-6-session-persistence-and-context-restoration (2026-04-22 fresh pass).
status: done 2026-08-31
archived: 2026-09-18
resolution: closed by human decision: The current behavior is deliberate, documented, and safe to retain.
decision: 2026-08-31 Close with current behavior — The current behavior is deliberate, documented, and safe to retain.

### DW-1175: Information-level logging on every projection-connection-state transition floods telemetry on flapping connections

origin: migrated from legacy ledger ("Deferred from: code review of 5-3-signalr-connection-and-disconnection-handling (2026-04-26)"), 2026-08-27
location: ProjectionConnectionState.cs:100-104
reason: **Information-level logging on every projection-connection-state transition floods telemetry on flapping connections** — A 30s SignalR reconnect cycle writes 3+ lines per cycle plus one `RejoinFailed` per active group. Rate-limiting / sampling / structured trace correlation belongs to Story 5-6 build-time infrastructure enforcement and observability. Source: `ProjectionConnectionState.cs:100-104` (`ProjectionConnectionStateService.Apply` log call). Reconciliation: Row: DW-0444; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: ProjectionConnectionTelemetryTests reconnect rate-limit coverage; Residual release-gate risk: none.; Evidence: source labels: `ProjectionConnectionState.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:3595

### DW-1176: SignalR factory wrapper tests for `Reconnecting`/`Reconnected`/`Closed` event publication, `WithAutomaticReconnect()` policy wiring, distinct initial-start-failure surface, and `AccessTokenProvider` per-call observation — T9.1 unfilled. Likely lands as `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/SignalRProjectionHubConnectionFactoryTests.cs` with a HubConnection fake. Reconciliation: Row: DW-0445; Split to SignalR factory wrapper test story 2026-05-13; Disposition: split-to-named-story; Reason: production wrapper needs a dedicated seam/fake around HubConnectionBuilder; Residual release-gate risk: medium; Reopen trigger: a regression in `WithAutomaticReconnect`/`Reconnecting`/`Reconnected`/`Closed` event publication or `AccessTokenProvider` callback observation is observed in production logs, or a dedicated SignalR factory wrapper test story is scheduled (added by Story 11.7 code review P-11); Evidence: section: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26).

origin: migrated from legacy ledger ("Deferred from: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/SignalRProjectionHubConnectionFactoryTests.cs
reason: **P3 — SignalR factory wrapper tests for `Reconnecting`/`Reconnected`/`Closed` event publication, `WithAutomaticReconnect()` policy wiring, distinct initial-start-failure surface, and `AccessTokenProvider` per-call observation** — T9.1 unfilled. Likely lands as `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/SignalRProjectionHubConnectionFactoryTests.cs` with a HubConnection fake. Reconciliation: Row: DW-0445; Split to SignalR factory wrapper test story 2026-05-13; Disposition: split-to-named-story; Reason: production wrapper needs a dedicated seam/fake around HubConnectionBuilder; Residual release-gate risk: medium; Reopen trigger: a regression in `WithAutomaticReconnect`/`Reconnecting`/`Reconnected`/`Closed` event publication or `AccessTokenProvider` callback observation is observed in production logs, or a dedicated SignalR factory wrapper test story is scheduled (added by Story 11.7 code review P-11); Evidence: section: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26).
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: Commit 9ad4312f; tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/SignalRProjectionHubConnectionFactoryTests.cs:13-48 and ProjectionSubscriptionServiceTests.cs:111-118,203-214,558-572 cover factory retry/token/phase plus reconnect, closed, and initial-start behavior.

### DW-1178: bUnit negative assertions for `FcProjectionConnectionStatus` — additive: no `.fluent-overlay`/`.fluent-dialog` rendered, no focus migration into the indicator, no `NavigationManager.NavigateTo` invoked. Reconciliation: Row: DW-0447; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: FcProjectionConnectionStatusTests negative overlay/focus/navigation assertions; Residual release-gate risk: none.; Related: Story 11.6; Evidence: section: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26).

origin: migrated from legacy ledger ("Deferred from: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26)"), 2026-08-27
location: FcProjectionConnectionStatus
reason: **P15 — bUnit negative assertions for `FcProjectionConnectionStatus`** — additive: no `.fluent-overlay`/`.fluent-dialog` rendered, no focus migration into the indicator, no `NavigationManager.NavigateTo` invoked. Reconciliation: Row: DW-0447; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: FcProjectionConnectionStatusTests negative overlay/focus/navigation assertions; Residual release-gate risk: none.; Related: Story 11.6; Evidence: section: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/State/ReconnectionReconciliation/ReconnectReconcileStatusIntegrationTests.cs:29

### DW-1179: Race-staged tests for duplicate Subscribe/Unsubscribe during reconnect, dispose-suppresses-callbacks — `FakeProjectionHubConnection` would need a `BlockUntil(...)` primitive to deterministically stage the race. Reconciliation: Row: DW-0448; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: ProjectionSubscriptionServiceFaultTests and FaultInjectingProjectionHubConnectionTests deterministic reconnect race coverage; Residual release-gate risk: low.; Related: Story 11.4; Evidence: section: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26).

origin: migrated from legacy ledger ("Deferred from: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26)"), 2026-08-27
location: FakeProjectionHubConnection
reason: **P17 — Race-staged tests for duplicate Subscribe/Unsubscribe during reconnect, dispose-suppresses-callbacks** — `FakeProjectionHubConnection` would need a `BlockUntil(...)` primitive to deterministically stage the race. Reconciliation: Row: DW-0448; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: ProjectionSubscriptionServiceFaultTests and FaultInjectingProjectionHubConnectionTests deterministic reconnect race coverage; Residual release-gate risk: low.; Related: Story 11.4; Evidence: section: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs:32

### DW-1180: Fallback ETag-validator-usage / cleanup-on-reconnect / 429-503 visible-data-preserved tests — feasible now that DN1 driver is wired, but artificial without a real lane producer; should land alongside the first lane-registration callsite (DataGrid/count surface). Reconciliation: Row: DW-0449; Split to visible-lane registration story 2026-05-13; Disposition: split-to-named-story; Reason: fallback ETag/429/503 tests need real DataGrid/count lane producers; Residual release-gate risk: medium; Reopen trigger: first DataGrid/count `RegisterLane` callsite is wired (DW-0450 / DW-0460), or fallback ETag/429/503 behavior is observed regressing in production (added by Story 11.7 code review P-11); Evidence: section: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26).

origin: migrated from legacy ledger ("Deferred from: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26)"), 2026-08-27
location: RegisterLane
reason: **P18 — Fallback ETag-validator-usage / cleanup-on-reconnect / 429-503 visible-data-preserved tests** — feasible now that DN1 driver is wired, but artificial without a real lane producer; should land alongside the first lane-registration callsite (DataGrid/count surface). Reconciliation: Row: DW-0449; Split to visible-lane registration story 2026-05-13; Disposition: split-to-named-story; Reason: fallback ETag/429/503 tests need real DataGrid/count lane producers; Residual release-gate risk: medium; Reopen trigger: first DataGrid/count `RegisterLane` callsite is wired (DW-0450 / DW-0460), or fallback ETag/429/503 behavior is observed regressing in production (added by Story 11.7 code review P-11); Evidence: section: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26).
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:1497 and src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs:268 register real visible lanes; ProjectionFallbackRefreshSchedulerTests.cs:127 and :221 cover reconciliation and ETag comparison.

### DW-1181: Lane registration callsites for `IProjectionFallbackRefreshScheduler.RegisterLane` are not yet wired in DataGrid/count consumers. The driver fires `TriggerFallbackOnceAsync` while disconnected, but with zero registered lanes the sweep is a no-op. The first consumer-facing pass that mounts `RegisterLane` from the DataGrid/count surface closes AC7 fully end-to-end. Owner: Story 5-4 reconnection sweep work or a dedicated 5-3 follow-up. Reconciliation: Row: DW-0450; Split to visible-lane registration story 2026-05-13; Disposition: split-to-named-story; Reason: generated DataGrid and badge/count RegisterLane callsites are not wired in this story; Residual release-gate risk: medium; Reopen trigger: visible-lane generator wiring story (see DW-0460) lands, or a regression where a reconnect sweep finds zero registered lanes is observed (added by Story 11.7 code review P-11); Evidence: section: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26).

origin: migrated from legacy ledger ("Deferred from: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26)"), 2026-08-27
location: IProjectionFallbackRefreshScheduler.RegisterLane
reason: **G53-3 — Lane registration callsites for `IProjectionFallbackRefreshScheduler.RegisterLane` are not yet wired in DataGrid/count consumers.** The driver fires `TriggerFallbackOnceAsync` while disconnected, but with zero registered lanes the sweep is a no-op. The first consumer-facing pass that mounts `RegisterLane` from the DataGrid/count surface closes AC7 fully end-to-end. Owner: Story 5-4 reconnection sweep work or a dedicated 5-3 follow-up. Reconciliation: Row: DW-0450; Split to visible-lane registration story 2026-05-13; Disposition: split-to-named-story; Reason: generated DataGrid and badge/count RegisterLane callsites are not wired in this story; Residual release-gate risk: medium; Reopen trigger: visible-lane generator wiring story (see DW-0460) lands, or a regression where a reconnect sweep finds zero registered lanes is observed (added by Story 11.7 code review P-11); Evidence: section: code review of 5-3-signalr-connection-and-disconnection-handling Pass-1 (2026-04-26).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:3637

### DW-1185: `LoadPageFailedAction` reducer's TCS-resolution behavior on schema mismatch is unverified [`LoadPageEffects.cs:127`] — Need to confirm whether `ReduceLoadPageFailed` resolves `PendingCompletionsByKey[viewKey]` TCS on schema-mismatch dispatch. If not, callers awaiting the TCS may hang. Defer pending a reducer-side audit; if confirmed buggy, file as patch in next pass. Reconciliation: Row: DW-0454; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: LoadPageTCSLifecycleTests.SchemaMismatchFailure_PropagatesViaTrySetException_AndRemovesPendingCompletion; Residual release-gate risk: none.; Evidence: LoadPageEffects.cs:127.

origin: migrated from legacy ledger ("Deferred from: code review of 5-4-reconnection-reconciliation-and-batched-updates (2026-04-26 bmad-code-review Pass-1)"), 2026-08-27
location: LoadPageEffects.cs:127
reason: **W4 — `LoadPageFailedAction` reducer's TCS-resolution behavior on schema mismatch is unverified** [`LoadPageEffects.cs:127`] — Need to confirm whether `ReduceLoadPageFailed` resolves `PendingCompletionsByKey[viewKey]` TCS on schema-mismatch dispatch. If not, callers awaiting the TCS may hang. Defer pending a reducer-side audit; if confirmed buggy, file as patch in next pass. Reconciliation: Row: DW-0454; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: LoadPageTCSLifecycleTests.SchemaMismatchFailure_PropagatesViaTrySetException_AndRemovesPendingCompletion; Residual release-gate risk: none.; Evidence: LoadPageEffects.cs:127.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadPageEffects.cs:127

### DW-1189: `ReconciliationSweepState` markers unbounded if no scheduled `ClearExpiredReconciliationSweepsAction` dispatch [`ReconciliationSweepState.cs`] — `ReduceMark` accepts arbitrary view keys with no cap; only an external dispatch of `ClearExpiredReconciliationSweepsAction` evicts. Memory leak in long-lived circuits if the clear timer is missing. Gated on DN1 (sweep-wiring decision); a periodic clear effect should land alongside the marker dispatcher. Reconciliation: Row: DW-0458; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: ReconciliationSweepReducersTests.MarkReconciliationSweepAction_CapsMarkersWhenClearSchedulingFallsBehind; Residual release-gate risk: none.; Evidence: ReconciliationSweepState.cs.

origin: migrated from legacy ledger ("Deferred from: code review of 5-4-reconnection-reconciliation-and-batched-updates (2026-04-26 bmad-code-review Pass-1)"), 2026-08-27
location: ReconciliationSweepState.cs
reason: **W8 — `ReconciliationSweepState` markers unbounded if no scheduled `ClearExpiredReconciliationSweepsAction` dispatch** [`ReconciliationSweepState.cs`] — `ReduceMark` accepts arbitrary view keys with no cap; only an external dispatch of `ClearExpiredReconciliationSweepsAction` evicts. Memory leak in long-lived circuits if the clear timer is missing. **Gated on DN1** (sweep-wiring decision); a periodic clear effect should land alongside the marker dispatcher. Reconciliation: Row: DW-0458; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: ReconciliationSweepReducersTests.MarkReconciliationSweepAction_CapsMarkersWhenClearSchedulingFallsBehind; Residual release-gate risk: none.; Evidence: ReconciliationSweepState.cs.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepState.cs:1

### DW-1191: Visible-lane RegisterLane callsites [DataGrid emit path + BadgeCountService] — The DataGrid view-host is source-generator emitted into adopter-namespace `.razor.g.cs` files. Wiring `IProjectionFallbackRefreshScheduler.RegisterLane`/`UnregisterLane` callsites requires emit-path changes in `Hexalith.FrontComposer.SourceTools` plus a parallel registration in `BadgeCountService`. Story 5-4 ships the coordinator + scheduler + sweep dispatch + family cache invalidation + schema-mismatch fallback fully functional, but the source-generator-emit lift warrants its own story. AC2/AC4/AC5 explicit narrowing for 5-4: visible-lane reconciliation does NOT auto-fire on reconnect for DataGrid or badge-count surfaces; existing Story 5-2 query/cache path keeps these surfaces loading correctly on reconnect. Defer target: Story 5-5 visible-lane wiring or a dedicated wiring story; supersedes the existing `G53-3` known-gap from the 5-3 review. Reconciliation: Row: DW-0460; Split to Story 11.4 visible-lane generator wiring 2026-05-13; Disposition: split-to-named-story; Reason: requires source-generator DataGrid emit changes and BadgeCountService registration; Residual release-gate risk: medium; Reopen trigger: Story 11.4 (or successor) lands SourceTools emit-path changes for DataGrid `RegisterLane`/`UnregisterLane`, or BadgeCountService gains visible-lane registration (added by Story 11.7 code review P-11); Related: Story 11.4; Evidence: section: code review of 5-4-reconnection-reconciliation-and-batched-updates (2026-04-26 bmad-code-review Pass-1).

origin: migrated from legacy ledger ("Deferred from: code review of 5-4-reconnection-reconciliation-and-batched-updates (2026-04-26 bmad-code-review Pass-1)"), 2026-08-27
location: .razor.g.cs
reason: **DN3 — Visible-lane RegisterLane callsites** [DataGrid emit path + BadgeCountService] — The DataGrid view-host is source-generator emitted into adopter-namespace `.razor.g.cs` files. Wiring `IProjectionFallbackRefreshScheduler.RegisterLane`/`UnregisterLane` callsites requires emit-path changes in `Hexalith.FrontComposer.SourceTools` plus a parallel registration in `BadgeCountService`. Story 5-4 ships the coordinator + scheduler + sweep dispatch + family cache invalidation + schema-mismatch fallback fully functional, but the source-generator-emit lift warrants its own story. **AC2/AC4/AC5 explicit narrowing for 5-4:** visible-lane reconciliation does NOT auto-fire on reconnect for DataGrid or badge-count surfaces; existing Story 5-2 query/cache path keeps these surfaces loading correctly on reconnect. **Defer target:** Story 5-5 visible-lane wiring or a dedicated wiring story; supersedes the existing `G53-3` known-gap from the 5-3 review. Reconciliation: Row: DW-0460; Split to Story 11.4 visible-lane generator wiring 2026-05-13; Disposition: split-to-named-story; Reason: requires source-generator DataGrid emit changes and BadgeCountService registration; Residual release-gate risk: medium; Reopen trigger: Story 11.4 (or successor) lands SourceTools emit-path changes for DataGrid `RegisterLane`/`UnregisterLane`, or BadgeCountService gains visible-lane registration (added by Story 11.7 code review P-11); Related: Story 11.4; Evidence: section: code review of 5-4-reconnection-reconciliation-and-batched-updates (2026-04-26 bmad-code-review Pass-1).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.cs:301

### DW-1192: ETag/304/429/503 polling parity [`PendingCommandPollingCoordinator.cs`] — Real `IPendingCommandStatusQuery` not registered in this story (DN4 governs); ETag plumbing has no callsite to exercise. Move with the real status query implementation. Reconciliation: Row: DW-0461; Final classification 2026-05-15: accepted-constraint; Constraint: `PENDING-STATUS-NULL-PROVIDER-V1`; Decision owner: Shell/EventStore integration owner; Trigger watcher: Release owner role; Likelihood: low; Impact: low; Release risk: medium; Reason: repository evidence still has only the `IPendingCommandStatusQuery` seam/null provider and no stable EventStore status endpoint URL, schema, validator, retry, or reconnect-epoch contract; Final outcome: named accepted v1 constraint, not provider-backed release-ready; User/operator impact: command lifecycle relies on live nudges, reconnect reconciliation, and bounded fallback polling, not direct provider-backed status polling; Agent impact: agents must not claim provider-backed pending-command readiness for v1; Release/package impact: release notes must carry this constraint before command lifecycle readiness is promoted; Expiry/revalidation trigger: 2026-06-30 or EventStore publishes stable status-resource metadata, whichever comes first; Reopen event: any provider-backed readiness claim, status-resource metadata consumption, or EventStore endpoint promotion; Required artifact: `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md`; Evidence: `ServiceCollectionExtensions` registers `NullPendingCommandStatusQuery`, `AddHexalithEventStore` does not replace it, and focused pending-command/EventStore validation in Story 12.3.

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-command-idempotency-and-optimistic-updates (2026-04-26)"), 2026-08-27
location: PendingCommandPollingCoordinator.cs
reason: **D1 — ETag/304/429/503 polling parity** [`PendingCommandPollingCoordinator.cs`] — Real `IPendingCommandStatusQuery` not registered in this story (DN4 governs); ETag plumbing has no callsite to exercise. Move with the real status query implementation. Reconciliation: Row: DW-0461; Final classification 2026-05-15: accepted-constraint; Constraint: `PENDING-STATUS-NULL-PROVIDER-V1`; Decision owner: Shell/EventStore integration owner; Trigger watcher: Release owner role; Likelihood: low; Impact: low; Release risk: medium; Reason: repository evidence still has only the `IPendingCommandStatusQuery` seam/null provider and no stable EventStore status endpoint URL, schema, validator, retry, or reconnect-epoch contract; Final outcome: named accepted v1 constraint, not provider-backed release-ready; User/operator impact: command lifecycle relies on live nudges, reconnect reconciliation, and bounded fallback polling, not direct provider-backed status polling; Agent impact: agents must not claim provider-backed pending-command readiness for v1; Release/package impact: release notes must carry this constraint before command lifecycle readiness is promoted; Expiry/revalidation trigger: 2026-06-30 or EventStore publishes stable status-resource metadata, whichever comes first; Reopen event: any provider-backed readiness claim, status-resource metadata consumption, or EventStore endpoint promotion; Required artifact: `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md`; Evidence: `ServiceCollectionExtensions` registers `NullPendingCommandStatusQuery`, `AddHexalithEventStore` does not replace it, and focused pending-command/EventStore validation in Story 12.3.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs:1

### DW-1193: Three overlapping status enums [`OptimisticBadgeState/PendingCommandStatus/PendingCommandTerminalOutcome`] — Refactor opportunity, not a defect. Track as Story 9-4 (governance) follow-up. Reconciliation: Row: DW-0462; Split to Story 9.4 enum/status governance 2026-05-13; Disposition: split-to-named-story; Reason: enum consolidation is governance/refactor work; Residual release-gate risk: low.; Evidence: OptimisticBadgeState/PendingCommandStatus/PendingCommandTerminalOutcome.

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-command-idempotency-and-optimistic-updates (2026-04-26)"), 2026-08-27
location: OptimisticBadgeState/PendingCommandStatus/PendingCommandTerminalOutcome
reason: **D2 — Three overlapping status enums** [`OptimisticBadgeState/PendingCommandStatus/PendingCommandTerminalOutcome`] — Refactor opportunity, not a defect. Track as Story 9-4 (governance) follow-up. Reconciliation: Row: DW-0462; Split to Story 9.4 enum/status governance 2026-05-13; Disposition: split-to-named-story; Reason: enum consolidation is governance/refactor work; Residual release-gate risk: low.; Evidence: OptimisticBadgeState/PendingCommandStatus/PendingCommandTerminalOutcome.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-1195: Resolve-before-Register grace window [`PendingCommandStateService.cs:980-985`] — Buffering design needs cross-team alignment; current behavior (drop unknown observation) is documented per spec. Reconciliation: Row: DW-0464; Accepted constraint 2026-05-13; Disposition: accepted-with-risk; Risk: likelihood medium, impact low; Release risk: unknown observations before registration are intentionally dropped; Downstream impact: rare status race; Owner: pending-command owner; Review by: 2026-06-30; Reopen trigger: provider-backed status arrives before registration in production; Validation: PendingCommandStateService behavior review.; Evidence: PendingCommandStateService.cs:980-985.

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-command-idempotency-and-optimistic-updates (2026-04-26)"), 2026-08-27
location: PendingCommandStateService.cs:980-985
reason: **D4 — Resolve-before-Register grace window** [`PendingCommandStateService.cs:980-985`] — Buffering design needs cross-team alignment; current behavior (drop unknown observation) is documented per spec. Reconciliation: Row: DW-0464; Accepted constraint 2026-05-13; Disposition: accepted-with-risk; Risk: likelihood medium, impact low; Release risk: unknown observations before registration are intentionally dropped; Downstream impact: rare status race; Owner: pending-command owner; Review by: 2026-06-30; Reopen trigger: provider-backed status arrives before registration in production; Validation: PendingCommandStateService behavior review.; Evidence: PendingCommandStateService.cs:980-985.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current verified behavior as intentional and close the deferred row.
decision: 2026-08-27 Accept current behavior — Record the current verified behavior as intentional and close the deferred row.

### DW-1197: Long-running Confirming has no escalation [`FcDesaturatedBadge.razor.cs`] — UX-DR concern (StillSyncing escalation) needs broader UX alignment; Story 5-5 budget exhausted. Reconciliation: Row: DW-0466; Split to Product/UX long-running confirmation escalation 2026-05-13; Disposition: split-to-named-story; Reason: UX escalation copy/timing needs product decision; Residual release-gate risk: low.; Evidence: FcDesaturatedBadge.razor.cs.

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-command-idempotency-and-optimistic-updates (2026-04-26)"), 2026-08-27
location: FcDesaturatedBadge.razor.cs
reason: **D6 — Long-running Confirming has no escalation** [`FcDesaturatedBadge.razor.cs`] — UX-DR concern (StillSyncing escalation) needs broader UX alignment; Story 5-5 budget exhausted. Reconciliation: Row: DW-0466; Split to Product/UX long-running confirmation escalation 2026-05-13; Disposition: split-to-named-story; Reason: UX escalation copy/timing needs product decision; Residual release-gate risk: low.; Evidence: FcDesaturatedBadge.razor.cs.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor:22
decision: 2026-08-27 Implement change — Implement the behavior requested by DW-1197, update affected contracts and consumers, and add focused regression evidence.

### DW-1199: Counter golden snapshot SVG markup change [`tests/.../Generated/CounterStoryVerificationTests.*.verified.txt`] — Verify whether the change is from a Fluent UI version bump or an unintended regression in render output for existing components; not introduced by Story 5-5 logic. Reconciliation: Row: DW-0468; Split to Story 11.6 Counter specimen snapshot review 2026-05-13; Disposition: split-to-named-story; Reason: visual/generated snapshot ownership is Shell UX/sample scope; Residual release-gate risk: low.; Related: Story 11.6; Evidence: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.*.verified.txt.

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-command-idempotency-and-optimistic-updates (2026-04-26)"), 2026-08-27
location: tests/.../Generated/CounterStoryVerificationTests.*.verified.txt
reason: **D8 — Counter golden snapshot SVG markup change** [`tests/.../Generated/CounterStoryVerificationTests.*.verified.txt`] — Verify whether the change is from a Fluent UI version bump or an unintended regression in render output for existing components; not introduced by Story 5-5 logic. Reconciliation: Row: DW-0468; Split to Story 11.6 Counter specimen snapshot review 2026-05-13; Disposition: split-to-named-story; Reason: visual/generated snapshot ownership is Shell UX/sample scope; Residual release-gate risk: low.; Related: Story 11.6; Evidence: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.*.verified.txt.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md:29 identifies the Fluent catalog change as the snapshot cause, and line 219 records the drift reconciliation.

### DW-1200: ETag/304/429/503 polling parity [`State/PendingCommands/PendingCommandPollingCoordinator.cs`] — Continuation of Pass-1 D1. Real `IPendingCommandStatusQuery` not registered; ETag plumbing has no callsite. Lifts when DN4-style real provider ships. Reconciliation: Row: DW-0469; Final classification 2026-05-15: superseded-preserved; Superseded by DW-0461; Decision owner: Shell/EventStore integration owner; Constraint: `PENDING-STATUS-NULL-PROVIDER-V1`; Validation: same pending-status provider split evidence; Release risk: inherited by DW-0461 accepted v1 constraint; Reopen event: only through DW-0461 if provider-backed readiness is claimed; Coverage rationale: if DW-0461 closes by provider implementation, DW-0469 must be re-checked for ETag/304/429/503 polling parity coverage; if DW-0461 closes by permanent constraint, DW-0469 must be explicitly re-routed to an ETag/retry-parity backlog row before EventStore status work resumes (DW-0461's null-provider framing does not strictly imply DW-0469's retry/cache hygiene scope); Evidence: State/PendingCommands/PendingCommandPollingCoordinator.cs and Story 12.3 release decision table.

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-command-idempotency-and-optimistic-updates Pass 2 (2026-04-26)"), 2026-08-27
location: PendingCommandPollingCoordinator.cs
reason: **P2-D1 — ETag/304/429/503 polling parity** [`State/PendingCommands/PendingCommandPollingCoordinator.cs`] — Continuation of Pass-1 D1. Real `IPendingCommandStatusQuery` not registered; ETag plumbing has no callsite. Lifts when DN4-style real provider ships. Reconciliation: Row: DW-0469; Final classification 2026-05-15: superseded-preserved; Superseded by DW-0461; Decision owner: Shell/EventStore integration owner; Constraint: `PENDING-STATUS-NULL-PROVIDER-V1`; Validation: same pending-status provider split evidence; Release risk: inherited by DW-0461 accepted v1 constraint; Reopen event: only through DW-0461 if provider-backed readiness is claimed; Coverage rationale: if DW-0461 closes by provider implementation, DW-0469 must be re-checked for ETag/304/429/503 polling parity coverage; if DW-0461 closes by permanent constraint, DW-0469 must be explicitly re-routed to an ETag/retry-parity backlog row before EventStore status work resumes (DW-0461's null-provider framing does not strictly imply DW-0469's retry/cache hygiene scope); Evidence: State/PendingCommands/PendingCommandPollingCoordinator.cs and Story 12.3 release decision table.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs:1

### DW-1201: `OptionsValidator` invariant comment is mis-justified [`Options/FcShellOptionsThresholdValidator.cs:532-538`] — Cap counts only pending entries; validator's stated reasoning is incorrect but the threshold rule itself is harmless. Comment-only cleanup. Reconciliation: Row: DW-0470; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: FcShellOptionsThresholdValidator comment corrected; Residual release-gate risk: none.; Evidence: Options/FcShellOptionsThresholdValidator.cs:532-538.

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-command-idempotency-and-optimistic-updates Pass 2 (2026-04-26)"), 2026-08-27
location: FcShellOptionsThresholdValidator.cs:532-538
reason: **P2-D2 — `OptionsValidator` invariant comment is mis-justified** [`Options/FcShellOptionsThresholdValidator.cs:532-538`] — Cap counts only pending entries; validator's stated reasoning is incorrect but the threshold rule itself is harmless. Comment-only cleanup. Reconciliation: Row: DW-0470; Resolved 2026-05-13; Disposition: fixed-in-11.7; Validation: FcShellOptionsThresholdValidator comment corrected; Residual release-gate risk: none.; Evidence: Options/FcShellOptionsThresholdValidator.cs:532-538.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Options/FcShellOptionsThresholdValidator.cs:532

### DW-1210: `DisplayName` empty `CommandTypeName` from direct `PendingCommandEntry` instantiation [`Components/EventStore/FcPendingCommandSummary.razor.cs:106-109`] — `Register` validates; only direct record construction bypasses. Reconciliation: Row: DW-0479; Accepted constraint 2026-05-13; Disposition: accepted-with-risk; Risk: likelihood low, impact low; Release risk: Register validates command type names; Downstream impact: direct record construction only; Owner: pending-command owner; Review by: 2026-06-30; Reopen trigger: public API starts accepting direct entries; Validation: PendingCommandRegistration validation review.; Evidence: Components/EventStore/FcPendingCommandSummary.razor.cs:106-109.

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-command-idempotency-and-optimistic-updates Pass 2 (2026-04-26)"), 2026-08-27
location: FcPendingCommandSummary.razor.cs:106-109
reason: **P2-D11 — `DisplayName` empty `CommandTypeName` from direct `PendingCommandEntry` instantiation** [`Components/EventStore/FcPendingCommandSummary.razor.cs:106-109`] — `Register` validates; only direct record construction bypasses. Reconciliation: Row: DW-0479; Accepted constraint 2026-05-13; Disposition: accepted-with-risk; Risk: likelihood low, impact low; Release risk: Register validates command type names; Downstream impact: direct record construction only; Owner: pending-command owner; Review by: 2026-06-30; Reopen trigger: public API starts accepting direct entries; Validation: PendingCommandRegistration validation review.; Evidence: Components/EventStore/FcPendingCommandSummary.razor.cs:106-109.
status: done 2026-08-28
archived: 2026-09-18
decision: 2026-08-28 Close as accepted — Accept the current verified behavior and close the deferred row without implementation.
resolution: closed by human decision: Accept the current verified behavior and close the deferred row without implementation.
decision: 2026-08-28 Close as accepted — Accept the current verified behavior and close the deferred row without implementation.

### DW-1211: Generated `nameof(commandFqn)` fallback returns simple name when `typeof(...).FullName` is null [`SourceTools/Emitters/CommandFormEmitter.cs:353`] — Open-generic edge case; current adopters do not hit this. Reconciliation: Row: DW-0480; Split to Story 11.4 SourceTools command-name fallback 2026-05-13; Disposition: split-to-named-story; Reason: open-generic generated fallback is source-generator scope; Residual release-gate risk: low.; Related: Story 11.4; Evidence: SourceTools/Emitters/CommandFormEmitter.cs:353.

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-command-idempotency-and-optimistic-updates Pass 2 (2026-04-26)"), 2026-08-27
location: CommandFormEmitter.cs:353
reason: **P2-D12 — Generated `nameof(commandFqn)` fallback returns simple name when `typeof(...).FullName` is null** [`SourceTools/Emitters/CommandFormEmitter.cs:353`] — Open-generic edge case; current adopters do not hit this. Reconciliation: Row: DW-0480; Split to Story 11.4 SourceTools command-name fallback 2026-05-13; Disposition: split-to-named-story; Reason: open-generic generated fallback is source-generator scope; Residual release-gate risk: low.; Related: Story 11.4; Evidence: SourceTools/Emitters/CommandFormEmitter.cs:353.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs:105-120 rejects generic command types with HFC1017, so the open-generic FullName-null fallback is unreachable.

### DW-1214: Catch filter `ex is not OutOfMemoryException` does not mention `StackOverflowException`/`AccessViolationException` [`State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs`, `Infrastructure/EventStore/ProjectionSubscriptionService.cs`] — Superseded by Story 11.16's explicit project-wide classifier decision. Reconciliation: Row: DW-0483; Accepted constraint 2026-05-13; Disposition: resolved-superseded 2026-07-13; Resolution: `Services/ExceptionGuard.IsFatal` is the single four-type taxonomy and all 35 catch filters delegate to it with zero local classifiers or ad-hoc fatal lists; Cancellation policy remains call-site-specific; Validation: focused Shell classifier, governance, authorization, and owner lane 182/182 passed; Evidence: `src/Hexalith.FrontComposer.Shell/Services/ExceptionGuard.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/FatalExceptionGuardGovernanceTests.cs`.

origin: migrated from legacy ledger ("Deferred from: code review of 5-5-command-idempotency-and-optimistic-updates Pass 2 (2026-04-26)"), 2026-08-27
location: ReconnectionReconciliationCoordinator.cs
reason: **P2-D15 — Catch filter `ex is not OutOfMemoryException` does not mention `StackOverflowException`/`AccessViolationException`** [`State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs`, `Infrastructure/EventStore/ProjectionSubscriptionService.cs`] — Superseded by Story 11.16's explicit project-wide classifier decision. Reconciliation: Row: DW-0483; Accepted constraint 2026-05-13; Disposition: resolved-superseded 2026-07-13; Resolution: `Services/ExceptionGuard.IsFatal` is the single four-type taxonomy and all 35 catch filters delegate to it with zero local classifiers or ad-hoc fatal lists; Cancellation policy remains call-site-specific; Validation: focused Shell classifier, governance, authorization, and owner lane 182/182 passed; Evidence: `src/Hexalith.FrontComposer.Shell/Services/ExceptionGuard.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/FatalExceptionGuardGovernanceTests.cs`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/ExceptionGuard.cs:12 and src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs:81 now use the centralized four-type fatal-exception classifier.

### DW-1221: Group B carry-forward: `ProjectionSlotRegistry.IsCompatibleComponent` `GetProperty("Context", ...)` throws `AmbiguousMatchException` (uncaught) on shadowed inherited `Context` properties. Reconciliation: Row: DW-0490; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30).

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: B2 — Group B carry-forward: `ProjectionSlotRegistry.IsCompatibleComponent` `GetProperty("Context", ...)` throws `AmbiguousMatchException` (uncaught) on shadowed inherited `Context` properties. Reconciliation: Row: DW-0490; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30).
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:208-220 catches AmbiguousMatchException from shadowed Context properties and rejects fail-soft.

### DW-1222: Group B carry-forward: contract-version check accepts negative `ContractVersion` integers and surfaces them as version mismatch instead of a more specific "invalid version" diagnostic. Reconciliation: Row: DW-0491; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30).

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: B3 — Group B carry-forward: contract-version check accepts negative `ContractVersion` integers and surfaces them as version mismatch instead of a more specific "invalid version" diagnostic. Reconciliation: Row: DW-0491; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30).
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:85-93 rejects non-positive contract versions with a dedicated invalid-version diagnostic.

### DW-1224: Group B carry-forward: `Descriptors` materializes a list per access and lacks documented immutable-snapshot semantics under concurrent registration. Reconciliation: Row: DW-0493; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30).

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: B5 — Group B carry-forward: `Descriptors` materializes a list per access and lacks documented immutable-snapshot semantics under concurrent registration. Reconciliation: Row: DW-0493; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30).
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:45-59 materializes one ReadOnlyCollection descriptor snapshot after construction.

### DW-1226: Group B carry-forward: identical descriptors emitted by two sources still trigger HFC1040 because dedupe runs only on key equality, not full descriptor equality. Reconciliation: Row: DW-0495; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: section: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30).

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: B7 — Group B carry-forward: identical descriptors emitted by two sources still trigger HFC1040 because dedupe runs only on key equality, not full descriptor equality. Reconciliation: Row: DW-0495; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: section: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30).
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:154-160 deduplicates fully equal descriptors before marking a key ambiguous.

### DW-1227: Group B carry-forward: descriptor `FieldType` is trusted at registration; nothing checks it against the actual `ProjectionType.GetProperty(FieldName).PropertyType`, so a hand-built descriptor can register with a wrong type and crash at render. Reconciliation: Row: DW-0496; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30).

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: B8 — Group B carry-forward: descriptor `FieldType` is trusted at registration; nothing checks it against the actual `ProjectionType.GetProperty(FieldName).PropertyType`, so a hand-built descriptor can register with a wrong type and crash at render. Reconciliation: Row: DW-0496; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: section: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30).
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Components/Rendering/FcFieldSlotHost.cs:106-118 validates descriptor FieldType against TField and falls back safely.

### DW-1228: Group B carry-forward: HFC1040 log path uses `enum.ToString()` per duplicate hit, allocating in a hot diagnostic path. Reconciliation: Row: DW-0497; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC17-AC20, AC35; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: section: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30).

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30)"), 2026-08-27
location: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md
reason: B9 — Group B carry-forward: HFC1040 log path uses `enum.ToString()` per duplicate hit, allocating in a hot diagnostic path. Reconciliation: Row: DW-0497; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC17-AC20, AC35; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: section: code review of 6-3-level-3-slot-level-field-replacement, Group A — Contracts (2026-04-30).
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:163-169 passes ProjectionRole directly to the generated logger; the explicit enum.ToString allocation is gone.

### DW-1235: Reflection-based `Context` property lookup with virtual/override base properties [`ProjectionSlotRegistry.cs:303-305`] — Blazor convention is `[Parameter]` on the most-derived property; corner case. Reconciliation: Row: DW-0504; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: ProjectionSlotRegistry.cs:303-305.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group B — Shell Runtime (2026-04-30)"), 2026-08-27
location: ProjectionSlotRegistry.cs:303-305
reason: GB-D7 — Reflection-based `Context` property lookup with virtual/override base properties [`ProjectionSlotRegistry.cs:303-305`] — Blazor convention is `[Parameter]` on the most-derived property; corner case. Reconciliation: Row: DW-0504; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: ProjectionSlotRegistry.cs:303-305.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:208-243 handles inherited or shadowed Context lookup and validates the resulting public Parameter property.

### DW-1249: Stale `Major` rollback diagnostic does not include the descriptor's effective major in HFC1041 message [`ProjectionSlotRegistry.cs:240-246`] — cosmetic; HFC1041 reports `Expected major` and the descriptor's full version is in `{ContractVersion}`. Reconciliation: Row: DW-0518; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC14-AC16, AC30; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: ProjectionSlotRegistry.cs:240-246.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group B — Shell Runtime (2026-04-30)"), 2026-08-27
location: ProjectionSlotRegistry.cs:240-246
reason: GB-D21 — Stale `Major` rollback diagnostic does not include the descriptor's effective major in HFC1041 message [`ProjectionSlotRegistry.cs:240-246`] — cosmetic; HFC1041 reports `Expected major` and the descriptor's full version is in `{ContractVersion}`. Reconciliation: Row: DW-0518; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC14-AC16, AC30; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: ProjectionSlotRegistry.cs:240-246.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:99-110 logs both expected and actual effective major, minor, and build values.

### DW-1262: HFC1040 log path uses `enum.ToString()` per duplicate hit (Group A B9 carry-forward) [`ProjectionSlotRegistry.cs:273`] — duplicate-registration path fires once per process at startup; allocation is not a hot path. Reconciliation: Row: DW-0531; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: ProjectionSlotRegistry.cs:273.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group B — Shell Runtime (2026-04-30)"), 2026-08-27
location: ProjectionSlotRegistry.cs:273
reason: GB-D34 — HFC1040 log path uses `enum.ToString()` per duplicate hit (Group A B9 carry-forward) [`ProjectionSlotRegistry.cs:273`] — duplicate-registration path fires once per process at startup; allocation is not a hot path. Reconciliation: Row: DW-0531; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.2; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.2.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2; Evidence: ProjectionSlotRegistry.cs:273.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs:163-169 no longer calls ToString on the role at the duplicate-log callsite.

### DW-1267: `__FrontComposerProjectionTemplatesRegistration.Descriptors` compile dependency on at least one `[ProjectionTemplate]` marker; removing all markers breaks `Program.cs` compile [`samples/Counter/Counter.Web/Program.cs:34`] — Story 6-2 carry-forward; generator should emit empty descriptor array when no markers exist for graceful empty-manifest fallback. Reconciliation: Row: DW-0536; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: samples/Counter/Counter.Web/Program.cs:34.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group C — Counter Sample (2026-04-30)"), 2026-08-27
location: Program.cs
reason: GC-D4 — `__FrontComposerProjectionTemplatesRegistration.Descriptors` compile dependency on at least one `[ProjectionTemplate]` marker; removing all markers breaks `Program.cs` compile [`samples/Counter/Counter.Web/Program.cs:34`] — Story 6-2 carry-forward; generator should emit empty descriptor array when no markers exist for graceful empty-manifest fallback. Reconciliation: Row: DW-0536; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: samples/Counter/Counter.Web/Program.cs:34.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionTemplateManifestEmitter.cs:113-137 always emits the registration type and an empty descriptor array when no markers exist.

### DW-1277: Recursion-guard (RenderDefault same-field bypass) test not in Counter slot [`samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor`] — covered at Shell layer in `FcFieldSlotHostTests.RenderDefault fallback path` (Group B GB-P2); spec D15 same-field bypass discipline lives at the Shell host, not in the sample. Reconciliation: Row: DW-0546; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC17-AC20, AC35; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group C — Counter Sample (2026-04-30)"), 2026-08-27
location: samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor
reason: GC-D14 — Recursion-guard (RenderDefault same-field bypass) test not in Counter slot [`samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor`] — covered at Shell layer in `FcFieldSlotHostTests.RenderDefault fallback path` (Group B GB-P2); spec D15 same-field bypass discipline lives at the Shell host, not in the sample. Reconciliation: Row: DW-0546; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC17-AC20, AC35; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcFieldSlotHostTests.cs:64-82 directly exercises the RenderDefault fallback path at the Shell host boundary.

### DW-1279: Virtualization-style row-reuse / `@key` integration test for Counter sample (Testing Standards line 420 + GB-P16) [`tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs`] — Counter sample renders one row; emitter `@key` wiring proof belongs to Group D SourceTools snapshot tests. Reconciliation: Row: DW-0548; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC17-AC20, AC35; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group C — Counter Sample (2026-04-30)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs
reason: GC-D16 — Virtualization-style row-reuse / `@key` integration test for Counter sample (Testing Standards line 420 + GB-P16) [`tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs`] — Counter sample renders one row; emitter `@key` wiring proof belongs to Group D SourceTools snapshot tests. Reconciliation: Row: DW-0548; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC17-AC20, AC35; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterVirtualizationTests.cs:91-106 pins generated ItemKey identity and fallback behavior at the SourceTools row-reuse layer.

### DW-1281: `ProjectionTemplateContractVersion.Current` is a moving target pinned at compile [`samples/Counter/Counter.Web/Components/Templates/CounterCardLayoutTemplate.razor.cs:17`] — ContractVersion management belongs to Story 6-6 build-time analyzer enforcement. Reconciliation: Row: DW-0550; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: samples/Counter/Counter.Web/Components/Templates/CounterCardLayoutTemplate.razor.cs:17.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group C — Counter Sample (2026-04-30)"), 2026-08-27
location: samples/Counter/Counter.Web/Components/Templates/CounterCardLayoutTemplate.razor.cs:17
reason: GC-D18 — `ProjectionTemplateContractVersion.Current` is a moving target pinned at compile [`samples/Counter/Counter.Web/Components/Templates/CounterCardLayoutTemplate.razor.cs:17`] — ContractVersion management belongs to Story 6-6 build-time analyzer enforcement. Reconciliation: Row: DW-0550; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC14-AC16, AC30; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: samples/Counter/Counter.Web/Components/Templates/CounterCardLayoutTemplate.razor.cs:17.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Parsers/ProjectionTemplateMarkerParser.cs:331-373 enforces HFC1035/HFC1036 contract-version drift, with tests/Hexalith.FrontComposer.SourceTools.Tests/Parsers/ProjectionTemplateMarkerTests.cs:188-245 covering it.

### DW-1283: Test base uses `AddSingleton` (not `TryAddSingleton`/`TryAddEnumerable`) for `IProjectionSlotRegistry` and `IProjectionTemplateRegistry` [`tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs:137-143`] — production uses `TryAddSingleton`; tests intentionally use `AddSingleton` so the test wins ordering. Document the intentional asymmetry. Reconciliation: Row: DW-0552; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC14-AC16, AC30; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs:137-143.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group C — Counter Sample (2026-04-30)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs:137-143
reason: GC-D20 — Test base uses `AddSingleton` (not `TryAddSingleton`/`TryAddEnumerable`) for `IProjectionSlotRegistry` and `IProjectionTemplateRegistry` [`tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs:137-143`] — production uses `TryAddSingleton`; tests intentionally use `AddSingleton` so the test wins ordering. Document the intentional asymmetry. Reconciliation: Row: DW-0552; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC14-AC16, AC30; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs:137-143.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Generated/GeneratedComponentTestBase.cs:141-149 documents the empty test registries and intentional descriptor-source override setup immediately above the AddSingleton registrations.

### DW-1284: `aria-label` on non-interactive `<span>` has variable screen-reader support (NVDA/JAWS) [`samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor:4`] — accessibility refinement; out of Story 6-3 contract scope. Reconciliation: Row: DW-0553; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC18, AC35; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Counter slot now uses deterministic aria-labelledby instead of aria-label on a non-interactive span.; Validation/evidence: samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor; tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor:4.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group C — Counter Sample (2026-04-30)"), 2026-08-27
location: samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor:4
reason: GC-D21 — `aria-label` on non-interactive `<span>` has variable screen-reader support (NVDA/JAWS) [`samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor:4`] — accessibility refinement; out of Story 6-3 contract scope. Reconciliation: Row: DW-0553; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC18, AC35; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Counter slot now uses deterministic aria-labelledby instead of aria-label on a non-interactive span.; Validation/evidence: samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor; tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor:4.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: samples/Counter/Counter.Web/Components/Slots/CounterCountSlot.razor:4

### DW-1287: `Guid.NewGuid().ToString()` correlation IDs in test reduce reproducibility [`tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs:200,237`] — bUnit test convention; minor diagnostic polish. Reconciliation: Row: DW-0556; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC17, AC20, AC35; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Counter sample verification correlation IDs are deterministic.; Validation/evidence: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs:200,237.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group C — Counter Sample (2026-04-30)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs:200
reason: GC-D24 — `Guid.NewGuid().ToString()` correlation IDs in test reduce reproducibility [`tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs:200,237`] — bUnit test convention; minor diagnostic polish. Reconciliation: Row: DW-0556; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC17, AC20, AC35; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Counter sample verification correlation IDs are deterministic.; Validation/evidence: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs:200,237.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 076d69ef

### DW-1288: `csproj` analyzer ref omits `PrivateAssets="all"`; analyzer may flow into runtime publish output [`samples/Counter/Counter.Web/Counter.Web.csproj:11-15`] — existing pattern in `Counter.Domain.csproj`; sample build hygiene improvement. Reconciliation: Row: DW-0557; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC20; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Counter.Web analyzer project reference now uses PrivateAssets discipline.; Validation/evidence: samples/Counter/Counter.Web/Counter.Web.csproj; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: samples/Counter/Counter.Web/Counter.Web.csproj:11-15.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group C — Counter Sample (2026-04-30)"), 2026-08-27
location: samples/Counter/Counter.Web/Counter.Web.csproj:11-15
reason: GC-D25 — `csproj` analyzer ref omits `PrivateAssets="all"`; analyzer may flow into runtime publish output [`samples/Counter/Counter.Web/Counter.Web.csproj:11-15`] — existing pattern in `Counter.Domain.csproj`; sample build hygiene improvement. Reconciliation: Row: DW-0557; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC20; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Counter.Web analyzer project reference now uses PrivateAssets discipline.; Validation/evidence: samples/Counter/Counter.Web/Counter.Web.csproj; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: samples/Counter/Counter.Web/Counter.Web.csproj:11-15.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: samples/Counter/Counter.Web/Counter.Web.csproj:11

### DW-1291: `EmitDetailDescription` emits `Typography.Caption` which is a `Hexalith.FrontComposer.Contracts.Rendering.FcTypoToken` while `FluentLabel.Typography` is the FluentUI Blazor enum, plus the generator omits the FQN namespace [`src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs:838`] — surfaced during Group C apply when adding `[Display(Description=...)]` to `CounterProjection.Count` triggered CS0103 in `Counter.Domain.CounterProjection.g.razor.cs(879,54)`. Carry forward to Story 4-6 emitter follow-up; Story 6-3 sample rolled back the description annotation to keep build green. Reconciliation: Row: DW-0560; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC5-AC13, AC26-AC29, AC33-AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs:838.

origin: migrated from legacy ledger ("Deferred from: code review of 6-3-level-3-slot-level-field-replacement, Group C — Counter Sample (2026-04-30)"), 2026-08-27
location: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs:838
reason: GC-D28 — `EmitDetailDescription` emits `Typography.Caption` which is a `Hexalith.FrontComposer.Contracts.Rendering.FcTypoToken` while `FluentLabel.Typography` is the FluentUI Blazor enum, plus the generator omits the FQN namespace [`src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs:838`] — surfaced during Group C apply when adding `[Display(Description=...)]` to `CounterProjection.Count` triggered CS0103 in `Counter.Domain.CounterProjection.g.razor.cs(879,54)`. Carry forward to Story 4-6 emitter follow-up; Story 6-3 sample rolled back the description annotation to keep build green. Reconciliation: Row: DW-0560; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.4; AC coverage: AC5-AC13, AC26-AC29, AC33-AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.4.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.4; Evidence: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs:838.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Emitters/ProjectionRoleBodyEmitter.cs:838

### DW-1294: `DevModeStrings.resx` + `DevModeStrings.fr.resx` missing → all overlay strings fall back to hardcoded English. Add 16 EN keys + 16 FR keys with NBSP-respecting French translations. [`src/Hexalith.FrontComposer.Shell/Resources/DevMode/`] Reconciliation: Row: DW-0563; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC7, AC22; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Dev-mode overlay/toggle/drawer/copy/starter strings now have EN/FR resources with parity tests.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Resources/DevMode/DevModeStrings.resx; src/Hexalith.FrontComposer.Shell/Resources/DevMode/DevModeStrings.fr.resx; tests/Hexalith.FrontComposer.Shell.Tests/Resources/DevModeStringsTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Resources/DevMode/.

origin: migrated from legacy ledger ("Patches deferred from Story 6-5 review (2026-05-01)"), 2026-08-27
location: DevModeStrings.resx
reason: P2 — `DevModeStrings.resx` + `DevModeStrings.fr.resx` missing → all overlay strings fall back to hardcoded English. Add 16 EN keys + 16 FR keys with NBSP-respecting French translations. [`src/Hexalith.FrontComposer.Shell/Resources/DevMode/`] Reconciliation: Row: DW-0563; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC7, AC22; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Dev-mode overlay/toggle/drawer/copy/starter strings now have EN/FR resources with parity tests.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Resources/DevMode/DevModeStrings.resx; src/Hexalith.FrontComposer.Shell/Resources/DevMode/DevModeStrings.fr.resx; tests/Hexalith.FrontComposer.Shell.Tests/Resources/DevModeStringsTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Resources/DevMode/.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit bd56dc75

### DW-1295: `RazorEmitter.AppendNode` lacks visited-set guard for cyclic `ComponentTreeNode.Children`. Theoretical today (generator can't produce cycles); harden once adopter-fed trees from MCP/test fixtures land in Story 6-6. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs`] Reconciliation: Row: DW-0564; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC10, AC29, AC34; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Starter-template component-tree emission now detects repeated references while preserving depth and fan-out bounds.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs; tests/Hexalith.FrontComposer.Shell.Tests/Services/DevMode/RazorEmitterTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.5; Evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs.

origin: migrated from legacy ledger ("Patches deferred from Story 6-5 review (2026-05-01)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs
reason: P4 — `RazorEmitter.AppendNode` lacks visited-set guard for cyclic `ComponentTreeNode.Children`. Theoretical today (generator can't produce cycles); harden once adopter-fed trees from MCP/test fixtures land in Story 6-6. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs`] Reconciliation: Row: DW-0564; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC10, AC29, AC34; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Starter-template component-tree emission now detects repeated references while preserving depth and fan-out bounds.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs; tests/Hexalith.FrontComposer.Shell.Tests/Services/DevMode/RazorEmitterTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.5; Evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs:1

### DW-1296: Make `DevModeOverlayController`, `RazorEmitter`, `ClipboardJSModule`, `DevModeAnnotationSnapshotVisitor`, `DevModeRegistrationLogger` `internal sealed class` (interfaces stay public). Mass refactor across Shell tests; defer to a focused PR. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/`] Reconciliation: Row: DW-0565; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC5-AC13, AC26-AC29, AC33-AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/.

origin: migrated from legacy ledger ("Patches deferred from Story 6-5 review (2026-05-01)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Services/DevMode/
reason: P6 — Make `DevModeOverlayController`, `RazorEmitter`, `ClipboardJSModule`, `DevModeAnnotationSnapshotVisitor`, `DevModeRegistrationLogger` `internal sealed class` (interfaces stay public). Mass refactor across Shell tests; defer to a focused PR. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/`] Reconciliation: Row: DW-0565; Final classification 2026-05-13: split-to-named-story; Decision owner: Story 11.3; AC coverage: AC5-AC13, AC26-AC29, AC33-AC34; Score: impact=variable; risk=variable; cost=medium/high; adjacency=split; Rationale: Outside Story 11.6 bounded Shell/sample release-readiness scope; routed to Story 11.3.; Validation/evidence: not impacted in Story 11.6; historical source row preserved; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/.
status: done 2026-08-29
archived: 2026-09-18
decision: 2026-08-29 Preserve public surface — Treat concrete classes as supported API.
resolution: closed by human decision: Treat concrete classes as supported API.
decision: 2026-08-29 Preserve public surface — Treat concrete classes as supported API.

### DW-1297: Reuse Story 4-6 `.fc-field-placeholder-dev` hook from `FcFieldPlaceholder.razor.css` instead of duplicating the red-dashed outline rule in `FcDevModeAnnotation.razor.css`. Cosmetic CSS cleanup. [`src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor.css`] Reconciliation: Row: DW-0566; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC5-AC13, AC26-AC29, AC33-AC34; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor.css.

origin: migrated from legacy ledger ("Patches deferred from Story 6-5 review (2026-05-01)"), 2026-08-27
location: FcFieldPlaceholder.razor.cs
reason: P8 — Reuse Story 4-6 `.fc-field-placeholder-dev` hook from `FcFieldPlaceholder.razor.css` instead of duplicating the red-dashed outline rule in `FcDevModeAnnotation.razor.css`. Cosmetic CSS cleanup. [`src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor.css`] Reconciliation: Row: DW-0566; Final classification 2026-05-13: accepted-with-risk; Decision owner: Story 11.6 release owner; AC coverage: AC5-AC13, AC26-AC29, AC33-AC34; Score: impact=low/medium; risk=low; cost=medium/high; adjacency=accepted; Rationale: Low release-readiness risk or existing lower-level evidence is sufficient for this release pass.; Validation/evidence: focused Story 11.6 Shell/Counter validation plus historical source row; revisit on matching regression or adopter request; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor.css.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor.css:12-19 and FcFieldPlaceholder.razor.css:7-9 contain distinct rules; the duplicated red-dashed outline is gone.

### DW-1298: Wrap `DevModeOverlayController.SelectedAnnotationKey` / `SelectedNode` mutations in a single private lock. Theoretical race (Blazor scoped service is single-threaded); harden when JS-interop continuations introduce a second SynchronizationContext. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs`] Reconciliation: Row: DW-0567; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC9, AC13; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Overlay selection state is serialized behind a private lock and same-key re-registration refreshes the selected node.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs; tests/Hexalith.FrontComposer.Shell.Tests/Services/DevMode/DevModeOverlayControllerTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs.

origin: migrated from legacy ledger ("Patches deferred from Story 6-5 review (2026-05-01)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs
reason: P10 — Wrap `DevModeOverlayController.SelectedAnnotationKey` / `SelectedNode` mutations in a single private lock. Theoretical race (Blazor scoped service is single-threaded); harden when JS-interop continuations introduce a second SynchronizationContext. [`src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs`] Reconciliation: Row: DW-0567; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC9, AC13; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Overlay selection state is serialized behind a private lock and same-key re-registration refreshes the selected node.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs; tests/Hexalith.FrontComposer.Shell.Tests/Services/DevMode/DevModeOverlayControllerTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs:1

### DW-1299: Generic-arity-aware `ShortTypeName` for `OriginatingProjectionTypeName="Acme.Domain.Generic`1[[…]]"` to avoid identifier collision between arity-1 and arity-2 of the same name (e.g. `GenericProjection_Of_Customer_Template`). [`src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs`] Reconciliation: Row: DW-0568; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC11, AC29; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Starter component names now include CLR generic arity and argument names to avoid simple-name collisions.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs; tests/Hexalith.FrontComposer.Shell.Tests/Services/DevMode/RazorEmitterTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs.

origin: migrated from legacy ledger ("Patches deferred from Story 6-5 review (2026-05-01)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs
reason: P16 — Generic-arity-aware `ShortTypeName` for `OriginatingProjectionTypeName="Acme.Domain.Generic`1[[…]]"` to avoid identifier collision between arity-1 and arity-2 of the same name (e.g. `GenericProjection_Of_Customer_Template`). [`src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs`] Reconciliation: Row: DW-0568; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC11, AC29; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Starter component names now include CLR generic arity and argument names to avoid simple-name collisions.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs; tests/Hexalith.FrontComposer.Shell.Tests/Services/DevMode/RazorEmitterTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs:1

### DW-1300: P18 / DN6 — Replace `FcDevModeToggleButton`'s literal `i` content with a `FluentIcon` from `FcFluentIcons` (requires a new dev-mode SVG icon path). Story 10-2 visual specimen will exercise the rendered icon. [`src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor`, `src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs`] Reconciliation: Row: DW-0569; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC6; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: The dev-mode toggle now renders a FrontComposer Fluent icon instead of a literal i placeholder.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor; src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs; tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/FcDevModeToggleButtonTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor.

origin: migrated from legacy ledger ("Patches deferred from Story 6-5 review (2026-05-01)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor
reason: P18 / DN6 — Replace `FcDevModeToggleButton`'s literal `i` content with a `FluentIcon` from `FcFluentIcons` (requires a new dev-mode SVG icon path). Story 10-2 visual specimen will exercise the rendered icon. [`src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor`, `src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs`] Reconciliation: Row: DW-0569; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC6; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: The dev-mode toggle now renders a FrontComposer Fluent icon instead of a literal i placeholder.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor; src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs; tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/FcDevModeToggleButtonTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeToggleButton.razor:1

### DW-1301: Resolve factory-registered `IHostEnvironment` in `AddFrontComposerDevMode()` no-arg overload. Currently only `ImplementationInstance` is matched; factory/singleton paths fail closed (no overlay) silently. Adopters with non-standard hosts must call the explicit overload. [`src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs`] Reconciliation: Row: DW-0570; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC8, AC28; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: The no-arg dev-mode registration path can resolve factory-registered IHostEnvironment descriptors.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs; tests/Hexalith.FrontComposer.Shell.Tests/Extensions/AddFrontComposerDevModeExtensionsTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs.

origin: migrated from legacy ledger ("Patches deferred from Story 6-5 review (2026-05-01)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs
reason: P19 — Resolve factory-registered `IHostEnvironment` in `AddFrontComposerDevMode()` no-arg overload. Currently only `ImplementationInstance` is matched; factory/singleton paths fail closed (no overlay) silently. Adopters with non-standard hosts must call the explicit overload. [`src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs`] Reconciliation: Row: DW-0570; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC8, AC28; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: The no-arg dev-mode registration path can resolve factory-registered IHostEnvironment descriptors.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs; tests/Hexalith.FrontComposer.Shell.Tests/Extensions/AddFrontComposerDevModeExtensionsTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Evidence: src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs:1

### DW-1302: `FcDevModeAnnotation.OnParametersSet` epoch-aware re-registration so selection persists when parent re-renders with same `AnnotationKey` but new `RenderEpoch`; emit HFC1049 to indicate drift. [`src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor`] Reconciliation: Row: DW-0571; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC13; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Same-key/new-epoch annotation re-registration refreshes selected metadata instead of leaving stale starter state selected.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs; tests/Hexalith.FrontComposer.Shell.Tests/Services/DevMode/DevModeOverlayControllerTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2, Story 11.4; Evidence: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor.

origin: migrated from legacy ledger ("Patches deferred from Story 6-5 review (2026-05-01)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor
reason: P20 — `FcDevModeAnnotation.OnParametersSet` epoch-aware re-registration so selection persists when parent re-renders with same `AnnotationKey` but new `RenderEpoch`; emit HFC1049 to indicate drift. [`src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor`] Reconciliation: Row: DW-0571; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC13; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Same-key/new-epoch annotation re-registration refreshes selected metadata instead of leaving stale starter state selected.; Validation/evidence: src/Hexalith.FrontComposer.Shell/Services/DevMode/DevModeOverlayController.cs; tests/Hexalith.FrontComposer.Shell.Tests/Services/DevMode/DevModeOverlayControllerTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.2, Story 11.4; Evidence: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Components/DevMode/FcDevModeAnnotation.razor:1

### DW-1304: DN3 (bUnit / Counter sample bUnit) — bUnit overlay/annotation tests, ClipboardJSModule tests, Counter sample smoke tests covering overlay activation / annotation appearance / red-dashed class / starter copy / stale message / clipboard recovery. Coupled to DN1 + Story 10-2 a11y CI gate. [`tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/`] Reconciliation: Row: DW-0573; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC7, AC10, AC11, AC13, AC17, AC18, AC33, AC35; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Focused bUnit/unit/Counter evidence now covers the implemented dev-mode and sample release-readiness fixes.; Validation/evidence: tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/FcDevModeToggleButtonTests.cs; tests/Hexalith.FrontComposer.Shell.Tests/Services/DevMode/RazorEmitterTests.cs; tests/Hexalith.FrontComposer.Shell.Tests/Resources/DevModeStringsTests.cs; tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.7; Evidence: tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/.

origin: migrated from legacy ledger ("Patches deferred from Story 6-5 review (2026-05-01)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/
reason: DN3 (bUnit / Counter sample bUnit) — bUnit overlay/annotation tests, ClipboardJSModule tests, Counter sample smoke tests covering overlay activation / annotation appearance / red-dashed class / starter copy / stale message / clipboard recovery. Coupled to DN1 + Story 10-2 a11y CI gate. [`tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/`] Reconciliation: Row: DW-0573; Final classification 2026-05-13: fixed-in-11.6; Decision owner: Story 11.6; AC coverage: AC7, AC10, AC11, AC13, AC17, AC18, AC33, AC35; Score: impact=high; risk=medium; cost=low/medium; adjacency=direct; Rationale: Focused bUnit/unit/Counter evidence now covers the implemented dev-mode and sample release-readiness fixes.; Validation/evidence: tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/FcDevModeToggleButtonTests.cs; tests/Hexalith.FrontComposer.Shell.Tests/Services/DevMode/RazorEmitterTests.cs; tests/Hexalith.FrontComposer.Shell.Tests/Resources/DevModeStringsTests.cs; tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs; Matrix: _bmad-output/implementation-artifacts/11-6-row-evidence-matrix.md; Previous owner was Story 11.6; Related: Story 11.7; Evidence: tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Components/DevMode/FcDevModeToggleButtonTests.cs:1

### DW-1309: Markdown rich rendering features (role-specific tables, status cards, timelines, empty-state suggestions). Owner: Story 8-4. [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`] Reconciliation: Row: DW-0578; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-1-mcp-server-and-typed-tool-exposure (2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs
reason: Markdown rich rendering features (role-specific tables, status cards, timelines, empty-state suggestions). **Owner:** Story 8-4. [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`] Reconciliation: Row: DW-0578; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs:145

### DW-1310: Schema fingerprints / version negotiation — `McpManifest.SchemaVersion` is a static constant string. Owner: Story 8-6. [`src/Hexalith.FrontComposer.Contracts/Mcp/McpManifest.cs`] Reconciliation: Row: DW-0579; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-1-mcp-server-and-typed-tool-exposure (2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.Contracts/Mcp/McpManifest.cs
reason: Schema fingerprints / version negotiation — `McpManifest.SchemaVersion` is a static constant string. **Owner:** Story 8-6. [`src/Hexalith.FrontComposer.Contracts/Mcp/McpManifest.cs`] Reconciliation: Row: DW-0579; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Contracts/Mcp/McpManifest.cs:1

### DW-1311: Two-call lifecycle subscription tool — out of scope per spec D7. Owner: Story 8-3. [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`] Reconciliation: Row: DW-0580; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-1-mcp-server-and-typed-tool-exposure (2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs
reason: Two-call lifecycle subscription tool — out of scope per spec D7. **Owner:** Story 8-3. [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`] Reconciliation: Row: DW-0580; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current verified behavior as intentional and close the deferred row.
decision: 2026-08-27 Accept current behavior — Record the current verified behavior as intentional and close the deferred row.

### DW-1312: Tenant-scoped tool listing / closest-match suggestions — out of scope per spec D6. Owner: Story 8-2. [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs:264-268`] Reconciliation: Row: DW-0581; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.2 diagnostic/docs governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.2 diagnostic/docs governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-1-mcp-server-and-typed-tool-exposure (2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs:264-268
reason: Tenant-scoped tool listing / closest-match suggestions — out of scope per spec D6. **Owner:** Story 8-2. [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs:264-268`] Reconciliation: Row: DW-0581; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.2 diagnostic/docs governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.2 diagnostic/docs governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current verified behavior as intentional and close the deferred row.
decision: 2026-08-27 Accept current behavior — Record the current verified behavior as intentional and close the deferred row.

### DW-1313: `ApiKeys` plaintext storage in options — current `IOptions`-bound config is the standard ASP.NET Core pattern; rotation/secret-store integration is a security follow-up. Owner: Epic 7 security follow-up. [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs:375`] Reconciliation: Row: DW-0582; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-1-mcp-server-and-typed-tool-exposure (2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs:375
reason: `ApiKeys` plaintext storage in options — current `IOptions`-bound config is the standard ASP.NET Core pattern; rotation/secret-store integration is a security follow-up. **Owner:** Epic 7 security follow-up. [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs:375`] Reconciliation: Row: DW-0582; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-09-06
archived: 2026-09-18
decision: 2026-09-06 Use secure configuration — Retain IOptions and require adopters to source values from a secure ASP.NET Core configuration provider.
resolution: closed by human decision: Retain IOptions and require adopters to source values from a secure ASP.NET Core configuration provider.
decision: 2026-09-06 Use secure configuration — Retain IOptions and require adopters to source values from a secure ASP.NET Core configuration provider.

### DW-1314: Skill corpus and build-time agent support resources. Owner: Story 8-5. [`src/Hexalith.FrontComposer.Mcp/`] Reconciliation: Row: DW-0583; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-1-mcp-server-and-typed-tool-exposure (2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/
reason: Skill corpus and build-time agent support resources. **Owner:** Story 8-5. [`src/Hexalith.FrontComposer.Mcp/`] Reconciliation: Row: DW-0583; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Skills/FrontComposerSkillResourceProvider.cs:29

### DW-1315: `BuildServiceProvider` probe pattern in `AddFrontComposerMcp` — pre-existing pattern from Story 8-1; the diff expands its use but does not introduce it. Owner: Story 8-1 follow-up / Epic 9 host hardening. [`src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:37`] Reconciliation: Row: DW-0584; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-2-hallucination-rejection-and-tenant-scoped-tools (2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:37
reason: `BuildServiceProvider` probe pattern in `AddFrontComposerMcp` — pre-existing pattern from Story 8-1; the diff expands its use but does not introduce it. **Owner:** Story 8-1 follow-up / Epic 9 host hardening. [`src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:37`] Reconciliation: Row: DW-0584; Final classification 2026-05-14: split-to-named-story; Target owner: Story 11.7 EventStore/release-governance follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 11.7 EventStore/release-governance follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:37

### DW-1316: `NormalizeForMatching` discards confusable / non-ASCII forms silently rather than producing a documented "unsupported" suggestion category — intentional per spec T2; a future story can route them to a dedicated suggestion path. Owner: Post-v1 benchmark-driven follow-up. [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:230-250`] Reconciliation: Row: DW-0585; Final classification 2026-05-14: split-to-named-story; Target owner: Story 10.6 benchmark/release guard follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 10.6 benchmark/release guard follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-2-hallucination-rejection-and-tenant-scoped-tools (2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:230-250
reason: `NormalizeForMatching` discards confusable / non-ASCII forms silently rather than producing a documented "unsupported" suggestion category — intentional per spec T2; a future story can route them to a dedicated suggestion path. **Owner:** Post-v1 benchmark-driven follow-up. [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:230-250`] Reconciliation: Row: DW-0585; Final classification 2026-05-14: split-to-named-story; Target owner: Story 10.6 benchmark/release guard follow-up; Decision owner: Story 12.2 release certification; Rationale: row is adjacent to MCP certification or non-runtime scope and is not required to block MCP v1 release after Story 11.5 evidence; Downstream MCP impact: none or contract-adjacent as recorded in the Story 12.2 release-owner summary; Close trigger: Story 10.6 benchmark/release guard follow-up lands or explicitly accepts the row with its own evidence; Evidence: Story 11.5 row-scoped matrix, Story 12.1 routing update, and Story 12.2 inventory/validation; Previous owner was Story 11.5.
status: done 2026-09-06
archived: 2026-09-18
resolution: closed by human decision: The current silent, fail-closed behavior remains safe and contract-compatible.
decision: 2026-09-06 Preserve no suggestion — The current silent, fail-closed behavior remains safe and contract-compatible.

### DW-1319: Whitespace-trimmed canonical name cannot be invoked — intentional per D4 ("similar names are suggestions, never aliases"); leading-space form correctly returns `UnknownTool` with a canonical suggestion. Owner: None — by design. [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:76-77`] Reconciliation: Row: DW-0588; Non-action decision 2026-05-11; Decision owner: Story 11.1 reconciliation; Rationale: existing row records no active fix required; Evidence: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:76-77.

origin: migrated from legacy ledger ("Deferred from: code review of 8-2-hallucination-rejection-and-tenant-scoped-tools (2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:76-77
reason: Whitespace-trimmed canonical name cannot be invoked — intentional per D4 ("similar names are suggestions, never aliases"); leading-space form correctly returns `UnknownTool` with a canonical suggestion. **Owner:** None — by design. [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:76-77`] Reconciliation: Row: DW-0588; Non-action decision 2026-05-11; Decision owner: Story 11.1 reconciliation; Rationale: existing row records no active fix required; Evidence: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:76-77.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:82-106 uses ordinal exact matching on the original requested name and only returns normalized near-matches as suggestions, so whitespace variants cannot execute as aliases.

### DW-1320: Zero-width or RTL marker characters in `requestedName` bypass `IsNullOrWhiteSpace` — `NormalizeForMatching` marks them unsupported and returns `null` suggestion; no execution occurs. Owner: None — defense-in-depth covered. [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:69-83`] Reconciliation: Row: DW-0589; Non-action decision 2026-05-11; Decision owner: Story 11.1 reconciliation; Rationale: existing row records no active fix required; Evidence: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:69-83.

origin: migrated from legacy ledger ("Deferred from: code review of 8-2-hallucination-rejection-and-tenant-scoped-tools (2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:69-83
reason: Zero-width or RTL marker characters in `requestedName` bypass `IsNullOrWhiteSpace` — `NormalizeForMatching` marks them unsupported and returns `null` suggestion; no execution occurs. **Owner:** None — defense-in-depth covered. [`src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:69-83`] Reconciliation: Row: DW-0589; Non-action decision 2026-05-11; Decision owner: Story 11.1 reconciliation; Rationale: existing row records no active fix required; Evidence: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:69-83.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:154-174 marks control/non-ASCII forms unsupported and lines 235-238 suppress suggestions; ResolveAsync lines 98-106 never dispatches them.

### DW-1324: Package-boundary / SDK-adapter-boundary tests for AC14 — assert `Contracts` and `SourceTools` public surfaces are SDK-DTO-free; pin `FrontComposerMcpProtocolMapper` output via snapshot. Owner: Story 8-6 schema-versioning surface discipline. [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpProtocolMapper.cs`] Reconciliation: Row: DW-0593; Final classification 2026-05-14: accepted-constraint; Decision owner: Story 11.2 diagnostic/docs governance owner; Likelihood: low; Impact: low to medium; Release risk: non-blocking for v1 with documented trigger; Downstream impact: agent/adopter behavior remains stable for v1; Evidence: Story 11.5 D11/DN9/DN14/DN15 notes, row-scoped matrix, and Story 12.2 release-owner summary; Expiry/revalidation trigger: public MCP category/key changes, descriptor-registry mutability, build-time corpus signing/baseline materialization, or a consumer parsing diagnostic polish strings as contract input; Release-note requirement: required only if public machine keys/categories or corpus/fingerprint publication semantics change; Regression guard: Story11_5ResolutionTests, AggregateManifestIntegrityTests, SchemaNegotiationPrecedenceMatrixTests, AuthContextAccessorTests, and diagnostic docs governance tests as applicable; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-2-hallucination-rejection-and-tenant-scoped-tools (2026-05-02)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/FrontComposerMcpProtocolMapper.cs
reason: Package-boundary / SDK-adapter-boundary tests for AC14 — assert `Contracts` and `SourceTools` public surfaces are SDK-DTO-free; pin `FrontComposerMcpProtocolMapper` output via snapshot. **Owner:** Story 8-6 schema-versioning surface discipline. [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpProtocolMapper.cs`] Reconciliation: Row: DW-0593; Final classification 2026-05-14: accepted-constraint; Decision owner: Story 11.2 diagnostic/docs governance owner; Likelihood: low; Impact: low to medium; Release risk: non-blocking for v1 with documented trigger; Downstream impact: agent/adopter behavior remains stable for v1; Evidence: Story 11.5 D11/DN9/DN14/DN15 notes, row-scoped matrix, and Story 12.2 release-owner summary; Expiry/revalidation trigger: public MCP category/key changes, descriptor-registry mutability, build-time corpus signing/baseline materialization, or a consumer parsing diagnostic polish strings as contract input; Release-note requirement: required only if public machine keys/categories or corpus/fingerprint publication semantics change; Regression guard: Story11_5ResolutionTests, AggregateManifestIntegrityTests, SchemaNegotiationPrecedenceMatrixTests, AuthContextAccessorTests, and diagnostic docs governance tests as applicable; Previous owner was Story 11.5.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/BoundaryTests.cs:7 and :29 assert Contracts and SourceTools are MCP-SDK-free, while ToolAdmissionTests.cs:356 pins the protocol-mapper output shape.
decision: 2026-08-28 Implement change — Implement the requested change at src/Hexalith.FrontComposer.Mcp/FrontComposerMcpProtocolMapper.cs, update affected contracts and consumers, and add focused regression evidence.
decision: 2026-08-28 Implement change — Implement the requested change at src/Hexalith.FrontComposer.Mcp/FrontComposerMcpProtocolMapper.cs, update affected contracts and consumers, and add focused regression evidence.

### DW-1326: `MaxProjectionStatusGroups`/`MaxFieldsPerResource`/`MaxRowsPerResource` rendering paths use `Math.Max(1, …)` to guard `0`, masking a missing validator floor. Owner: options-pattern hardening. [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs`] Reconciliation: Row: DW-0595; Final classification 2026-05-14: accepted-constraint; Decision owner: Story 11.2 diagnostic/docs governance owner; Likelihood: low; Impact: low to medium; Release risk: non-blocking for v1 with documented trigger; Downstream impact: agent/adopter behavior remains stable for v1; Evidence: Story 11.5 D11/DN9/DN14/DN15 notes, row-scoped matrix, and Story 12.2 release-owner summary; Expiry/revalidation trigger: public MCP category/key changes, descriptor-registry mutability, build-time corpus signing/baseline materialization, or a consumer parsing diagnostic polish strings as contract input; Release-note requirement: required only if public machine keys/categories or corpus/fingerprint publication semantics change; Regression guard: Story11_5ResolutionTests, AggregateManifestIntegrityTests, SchemaNegotiationPrecedenceMatrixTests, AuthContextAccessorTests, and diagnostic docs governance tests as applicable; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-4-projection-rendering-for-agents re-review (2026-05-04)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs
reason: `MaxProjectionStatusGroups`/`MaxFieldsPerResource`/`MaxRowsPerResource` rendering paths use `Math.Max(1, …)` to guard `0`, masking a missing validator floor. **Owner:** options-pattern hardening. [`src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs`] Reconciliation: Row: DW-0595; Final classification 2026-05-14: accepted-constraint; Decision owner: Story 11.2 diagnostic/docs governance owner; Likelihood: low; Impact: low to medium; Release risk: non-blocking for v1 with documented trigger; Downstream impact: agent/adopter behavior remains stable for v1; Evidence: Story 11.5 D11/DN9/DN14/DN15 notes, row-scoped matrix, and Story 12.2 release-owner summary; Expiry/revalidation trigger: public MCP category/key changes, descriptor-registry mutability, build-time corpus signing/baseline materialization, or a consumer parsing diagnostic polish strings as contract input; Release-note requirement: required only if public machine keys/categories or corpus/fingerprint publication semantics change; Regression guard: Story11_5ResolutionTests, AggregateManifestIntegrityTests, SchemaNegotiationPrecedenceMatrixTests, AuthContextAccessorTests, and diagnostic docs governance tests as applicable; Previous owner was Story 11.5.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:219-231 rejects non-positive MaxRowsPerResource, MaxFieldsPerResource, and MaxProjectionStatusGroups; commits e2a81d177 and 7a69cdf92 introduced the guards.

### DW-1334: Adding positional record parameters with defaults to `McpResourceDescriptor` / `McpParameterDescriptor` is source-compatible but binary-breaking if `Contracts` is ever published as a stable NuGet package (`IsPackable` not currently set). Owner: revisit before Contracts ships as stable NuGet. [`src/Hexalith.FrontComposer.Contracts/Mcp/McpResourceDescriptor.cs`, `src/Hexalith.FrontComposer.Contracts/Mcp/McpParameterDescriptor.cs`] Reconciliation: Row: DW-0603; Final classification 2026-05-14: accepted-constraint; Decision owner: Story 11.2 diagnostic/docs governance owner; Likelihood: low; Impact: low to medium; Release risk: non-blocking for v1 with documented trigger; Downstream impact: agent/adopter behavior remains stable for v1; Evidence: Story 11.5 D11/DN9/DN14/DN15 notes, row-scoped matrix, and Story 12.2 release-owner summary; Expiry/revalidation trigger: public MCP category/key changes, descriptor-registry mutability, build-time corpus signing/baseline materialization, or a consumer parsing diagnostic polish strings as contract input; Release-note requirement: required only if public machine keys/categories or corpus/fingerprint publication semantics change; Regression guard: Story11_5ResolutionTests, AggregateManifestIntegrityTests, SchemaNegotiationPrecedenceMatrixTests, AuthContextAccessorTests, and diagnostic docs governance tests as applicable; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-4-projection-rendering-for-agents re-review (2026-05-04)"), 2026-08-27
location: src/Hexalith.FrontComposer.Contracts/Mcp/McpResourceDescriptor.cs
reason: Adding positional record parameters with defaults to `McpResourceDescriptor` / `McpParameterDescriptor` is source-compatible but binary-breaking if `Contracts` is ever published as a stable NuGet package (`IsPackable` not currently set). **Owner:** revisit before Contracts ships as stable NuGet. [`src/Hexalith.FrontComposer.Contracts/Mcp/McpResourceDescriptor.cs`, `src/Hexalith.FrontComposer.Contracts/Mcp/McpParameterDescriptor.cs`] Reconciliation: Row: DW-0603; Final classification 2026-05-14: accepted-constraint; Decision owner: Story 11.2 diagnostic/docs governance owner; Likelihood: low; Impact: low to medium; Release risk: non-blocking for v1 with documented trigger; Downstream impact: agent/adopter behavior remains stable for v1; Evidence: Story 11.5 D11/DN9/DN14/DN15 notes, row-scoped matrix, and Story 12.2 release-owner summary; Expiry/revalidation trigger: public MCP category/key changes, descriptor-registry mutability, build-time corpus signing/baseline materialization, or a consumer parsing diagnostic polish strings as contract input; Release-note requirement: required only if public machine keys/categories or corpus/fingerprint publication semantics change; Regression guard: Story11_5ResolutionTests, AggregateManifestIntegrityTests, SchemaNegotiationPrecedenceMatrixTests, AuthContextAccessorTests, and diagnostic docs governance tests as applicable; Previous owner was Story 11.5.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj:4 is packable and Directory.Build.targets:8 enables package validation for packable projects.
decision: 2026-08-27 Implement change — Implement the behavior requested by DW-1334, update affected contracts and consumers, and add focused regression evidence.

### DW-1341: Lost invariant comment on truncation marker — `// marker reflects FULL aggregate, not the bounded subset` removed at `src/Hexalith.FrontComposer.Schema/Diagnostics/SchemaMigrationDeltaAnalyzer.cs:138`; restore in a doc-only follow-up so future refactorers don't recompute aggregate post-truncation. Owner: Doc-only follow-up. Reconciliation: Row: DW-0610; Final classification 2026-05-14: accepted-constraint; Decision owner: Story 11.2 diagnostic/docs governance owner; Likelihood: low; Impact: low to medium; Release risk: non-blocking for v1 with documented trigger; Downstream impact: agent/adopter behavior remains stable for v1; Evidence: Story 11.5 D11/DN9/DN14/DN15 notes, row-scoped matrix, and Story 12.2 release-owner summary; Expiry/revalidation trigger: public MCP category/key changes, descriptor-registry mutability, build-time corpus signing/baseline materialization, or a consumer parsing diagnostic polish strings as contract input; Release-note requirement: required only if public machine keys/categories or corpus/fingerprint publication semantics change; Regression guard: Story11_5ResolutionTests, AggregateManifestIntegrityTests, SchemaNegotiationPrecedenceMatrixTests, AuthContextAccessorTests, and diagnostic docs governance tests as applicable; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate Group B re-review (2026-05-05)"), 2026-08-27
location: src/Hexalith.FrontComposer.Schema/Diagnostics/SchemaMigrationDeltaAnalyzer.cs:138
reason: Lost invariant comment on truncation marker — `// marker reflects FULL aggregate, not the bounded subset` removed at `src/Hexalith.FrontComposer.Schema/Diagnostics/SchemaMigrationDeltaAnalyzer.cs:138`; restore in a doc-only follow-up so future refactorers don't recompute aggregate post-truncation. **Owner:** Doc-only follow-up. Reconciliation: Row: DW-0610; Final classification 2026-05-14: accepted-constraint; Decision owner: Story 11.2 diagnostic/docs governance owner; Likelihood: low; Impact: low to medium; Release risk: non-blocking for v1 with documented trigger; Downstream impact: agent/adopter behavior remains stable for v1; Evidence: Story 11.5 D11/DN9/DN14/DN15 notes, row-scoped matrix, and Story 12.2 release-owner summary; Expiry/revalidation trigger: public MCP category/key changes, descriptor-registry mutability, build-time corpus signing/baseline materialization, or a consumer parsing diagnostic polish strings as contract input; Release-note requirement: required only if public machine keys/categories or corpus/fingerprint publication semantics change; Regression guard: Story11_5ResolutionTests, AggregateManifestIntegrityTests, SchemaNegotiationPrecedenceMatrixTests, AuthContextAccessorTests, and diagnostic docs governance tests as applicable; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Schema/Diagnostics/SchemaMigrationDeltaAnalyzer.cs:192

### DW-1345: `SchemaContractFamilyNames` switch lacks attribute-based exhaustiveness — covered functionally by the build-time exhaustiveness test in P-46. Owner: v2.x compiler-enforced exhaustiveness if Roslyn analyzer ships. Reconciliation: Row: DW-0614; Final classification 2026-05-14: accepted-constraint; Decision owner: Story 11.2 diagnostic/docs governance owner; Likelihood: low; Impact: low to medium; Release risk: non-blocking for v1 with documented trigger; Downstream impact: agent/adopter behavior remains stable for v1; Evidence: Story 11.5 D11/DN9/DN14/DN15 notes, row-scoped matrix, and Story 12.2 release-owner summary; Expiry/revalidation trigger: public MCP category/key changes, descriptor-registry mutability, build-time corpus signing/baseline materialization, or a consumer parsing diagnostic polish strings as contract input; Release-note requirement: required only if public machine keys/categories or corpus/fingerprint publication semantics change; Regression guard: Story11_5ResolutionTests, AggregateManifestIntegrityTests, SchemaNegotiationPrecedenceMatrixTests, AuthContextAccessorTests, and diagnostic docs governance tests as applicable; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate Group B re-review (2026-05-05)"), 2026-08-27
location: SchemaContractFamilyNames
reason: `SchemaContractFamilyNames` switch lacks attribute-based exhaustiveness — covered functionally by the build-time exhaustiveness test in P-46. **Owner:** v2.x compiler-enforced exhaustiveness if Roslyn analyzer ships. Reconciliation: Row: DW-0614; Final classification 2026-05-14: accepted-constraint; Decision owner: Story 11.2 diagnostic/docs governance owner; Likelihood: low; Impact: low to medium; Release risk: non-blocking for v1 with documented trigger; Downstream impact: agent/adopter behavior remains stable for v1; Evidence: Story 11.5 D11/DN9/DN14/DN15 notes, row-scoped matrix, and Story 12.2 release-owner summary; Expiry/revalidation trigger: public MCP category/key changes, descriptor-registry mutability, build-time corpus signing/baseline materialization, or a consumer parsing diagnostic polish strings as contract input; Release-note requirement: required only if public machine keys/categories or corpus/fingerprint publication semantics change; Regression guard: Story11_5ResolutionTests, AggregateManifestIntegrityTests, SchemaNegotiationPrecedenceMatrixTests, AuthContextAccessorTests, and diagnostic docs governance tests as applicable; Previous owner was Story 11.5.
status: done 2026-09-06
archived: 2026-09-18
decision: 2026-09-06 Close accepted constraint — The build-time exhaustiveness test is the approved v1 protection and no analyzer story exists.
resolution: closed by human decision: The build-time exhaustiveness test is the approved v1 protection and no analyzer story exists.
decision: 2026-09-06 Close accepted constraint — The build-time exhaustiveness test is the approved v1 protection and no analyzer story exists.

### DW-1351: `BuildStructuredFailure` enumeration oracle (`isHiddenEquivalent: false` for schema failures) — distinguishes "schema-mismatched" from "tool-unknown" payloads. Defense-in-depth follow-up; not exploitable in current threat model. Owner: Defense-in-depth hardening. [`src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiationRuntimeGate.cs:884-892`] Reconciliation: Row: DW-0620; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate Group A re-review (2026-05-05)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiationRuntimeGate.cs:884-892
reason: `BuildStructuredFailure` enumeration oracle (`isHiddenEquivalent: false` for schema failures) — distinguishes "schema-mismatched" from "tool-unknown" payloads. Defense-in-depth follow-up; not exploitable in current threat model. **Owner:** Defense-in-depth hardening. [`src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiationRuntimeGate.cs:884-892`] Reconciliation: Row: DW-0620; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-09-06
archived: 2026-09-18
decision: 2026-09-06 Accept current behavior — Retain the current behavior as an explicit accepted constraint and record its rationale.
resolution: closed by human decision: Retain the current behavior as an explicit accepted constraint and record its rationale.
decision: 2026-09-06 Accept current behavior — Retain the current behavior as an explicit accepted constraint and record its rationale.

### DW-1352: `McpLifecycleResult` model added but unused within Group A — used by SourceTools T6 reflection elsewhere in the story; not actually dead. Owner: None — verified non-dead. [`src/Hexalith.FrontComposer.Mcp/Invocation/McpLifecycleModels.cs:454-458`] Reconciliation: Row: DW-0621; Non-action decision 2026-05-11; Decision owner: Story 11.1 reconciliation; Rationale: existing row records no active fix required; Evidence: src/Hexalith.FrontComposer.Mcp/Invocation/McpLifecycleModels.cs:454-458.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate Group A re-review (2026-05-05)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Invocation/McpLifecycleModels.cs:454-458
reason: `McpLifecycleResult` model added but unused within Group A — used by SourceTools T6 reflection elsewhere in the story; not actually dead. **Owner:** None — verified non-dead. [`src/Hexalith.FrontComposer.Mcp/Invocation/McpLifecycleModels.cs:454-458`] Reconciliation: Row: DW-0621; Non-action decision 2026-05-11; Decision owner: Story 11.1 reconciliation; Rationale: existing row records no active fix required; Evidence: src/Hexalith.FrontComposer.Mcp/Invocation/McpLifecycleModels.cs:454-458.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaFingerprintCrossPackageTests.cs:23 reflects over McpLifecycleResult, proving the model is consumed and not dead.

### DW-1360: Low-budget edge cases — `maxDeltaCount=2` with marker drops all real Breaking deltas (only marker + Truncated survive); `maxDeltaCount=N+1` (1 over budget) drops 1 real delta to make room for `Truncated`. Both documented behaviors but not pinned by tests. Linked to `MaxDeltaCount` magic-number defer above. Owner: parameterized truncation tests at boundary budgets. Reconciliation: Row: DW-0629; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate Chunk 1 re-review (2026-05-06)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj
reason: Low-budget edge cases — `maxDeltaCount=2` with marker drops all real Breaking deltas (only marker + Truncated survive); `maxDeltaCount=N+1` (1 over budget) drops 1 real delta to make room for `Truncated`. Both documented behaviors but not pinned by tests. Linked to `MaxDeltaCount` magic-number defer above. **Owner:** parameterized truncation tests at boundary budgets. Reconciliation: Row: DW-0629; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaTruncationTests.cs:44

### DW-1365: `FrontComposerMcpRuntimeManifestAggregator` cross-algorithm aggregation

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate Chunk 2 re-review (2026-05-07)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs:14-30
reason: **`FrontComposerMcpRuntimeManifestAggregator` cross-algorithm aggregation**: dedup tuple `(AlgorithmId, Value)` allows mixing `Sha256SourceToolsBlobV1` and `Sha256CanonicalJsonV1` fingerprints into one document. Defense-in-depth follow-up; no current call site mixes algorithms. **Owner:** reject mixed-algorithm corpora as `SchemaIntegrityMismatch` or document strict same-algorithm contract. [`src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs:14-30`] Reconciliation: Row: DW-0634; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs:14

### DW-1366: `McpToolResolutionResult.Reject(name, category, catalog)` drops `Tool` reference

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate Chunk 2 re-review (2026-05-07)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs:38-52
reason: **`McpToolResolutionResult.Reject(name, category, catalog)` drops `Tool` reference**: schema-rejection path loses descriptor correlation that exists for unknown-tool rejections. **Owner:** diagnostic-payload threading follow-up; preserve `Tool` on schema-rejection path or thread descriptor name into structured-failure payload. [`src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs:38-52`] Reconciliation: Row: DW-0635; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs:38

### DW-1367: Two public constructors on DI-resolved `FrontComposerMcpDescriptorRegistry`

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate Chunk 2 re-review (2026-05-07)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs:23-29
reason: **Two public constructors on DI-resolved `FrontComposerMcpDescriptorRegistry`**: `: this(options, corpusProviders: null)` legacy ctor + new corpus-aware ctor. DI containers may pick the legacy one and silently drop corpus support. Once AC8 corpus runtime aggregate ships, consolidate to the corpus-aware ctor only. **Owner:** linked to C2 follow-up. [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs:23-29`] Reconciliation: Row: DW-0636; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs:136 pins DI selection of the corpus-aware constructor and verifies the provider is invoked.

### DW-1368: `hashesMatch = string.Equals(client.Value, server.Value)` ignores `AlgorithmId`

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate Chunk 2 re-review (2026-05-07)"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs:150
reason: **`hashesMatch = string.Equals(client.Value, server.Value)` ignores `AlgorithmId`** (Group D H1): two fingerprints from different algorithms sharing the same encoded string would trip Exact. Now mitigated by C3 (single hex wire form) but defensive hardening is good hygiene. **Owner:** add `string.Equals(client.AlgorithmId, server.AlgorithmId, Ordinal)` to the predicate. [`src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs:150`] Reconciliation: Row: DW-0637; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs:150

### DW-1369: `CompatibleAdditive`/`CompatibleWarning` allow side effects but bypass argument-shape revalidation against baseline

origin: migrated from legacy ledger ("Deferred from: code review of 8-6a-schema-negotiation-runtime-gate Chunk 2 re-review (2026-05-07)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj
reason: **`CompatibleAdditive`/`CompatibleWarning` allow side effects but bypass argument-shape revalidation against baseline** (Group D H2): `ValidateArguments` only checks against current `descriptor.Parameters` — no baseline cross-check, so `EnumChanged` (CompatibleWarning) lets requests through to validation that may reject the client's old-enum value with `ValidationFailed`, losing the schema-drift signal in the agent response. **Owner:** AC5 revalidation follow-up (already linked to D5). Reconciliation: Row: DW-0638; Final classification 2026-05-14: resolved; Decision owner: Story 12.2 release certification; Evidence: Story 11.5 source/tests and Story 12.2 MCP validation (`dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release`, 291 passed); Negative cases covered: negotiation rejection, hidden/unknown precedence, tenant-gated admission, schema/fingerprint mismatch, descriptor stripping, redaction, culture-invariant public categories, memoized deterministic retry, and zero side effects before admission; Downstream impact: MCP v1 contract evidence closed; Release-note requirement: none unless public machine keys change; Regression guard: focused MCP tests named in Story 11.5 Dev Agent Record; Previous owner was Story 11.5.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj:1

### DW-1373: `compatibility-suppressions.json` per-row schema not enforced [`docs/diagnostics/compatibility-suppressions.json`] — file ships with `"suppressions": []`; no per-row schema guard for AC14's required fields (package, TFM, oldSignature, newState, hfcId, targetRelease, reviewerRationale). Belongs in chunk B test work. Sources: edge+auditor. Reconciliation: Row: DW-0642; Resolved 2026-05-11; Evidence: ValidateCompatibilitySuppressionsJson and suppression-scope fixtures.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: compatibility-suppressions.json
reason: **DEF-9-4-A1 — `compatibility-suppressions.json` per-row schema not enforced** [`docs/diagnostics/compatibility-suppressions.json`] — file ships with `"suppressions": []`; no per-row schema guard for AC14's required fields (package, TFM, oldSignature, newState, hfcId, targetRelease, reviewerRationale). Belongs in chunk B test work. Sources: edge+auditor. Reconciliation: Row: DW-0642; Resolved 2026-05-11; Evidence: ValidateCompatibilitySuppressionsJson and suppression-scope fixtures.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 059c6346

### DW-1374: `ValidateRegistryJson` does not `yield break` after `unsupported-schema` [`tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:293-316`] — on a true 2.0-shaped payload the iterator throws NRE before yielding the named category, breaking the "fail-closed with named category" contract. Chunk B fix. Sources: edge. Reconciliation: Row: DW-0643; Resolved 2026-05-11; Evidence: RegistryValidator_UnsupportedSchemaShortCircuitsBeforeNestedRows.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:293-316
reason: **DEF-9-4-A2 — `ValidateRegistryJson` does not `yield break` after `unsupported-schema`** [`tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:293-316`] — on a true 2.0-shaped payload the iterator throws NRE before yielding the named category, breaking the "fail-closed with named category" contract. Chunk B fix. Sources: edge. Reconciliation: Row: DW-0643; Resolved 2026-05-11; Evidence: RegistryValidator_UnsupportedSchemaShortCircuitsBeforeNestedRows.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:293

### DW-1375: `externalBoundaries` vs `ranges` overlap policy not encoded [`docs/diagnostics/diagnostic-registry.json`] — `Hexalith.EventStore` is both an external boundary and a range owner; no test asserts mutual exclusion. Chunk B constraint. Sources: edge. Reconciliation: Row: DW-0644; Resolved 2026-05-11; Evidence: structured externalBoundaries rangePolicy validation.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: docs/diagnostics/diagnostic-registry.json
reason: **DEF-9-4-A3 — `externalBoundaries` vs `ranges` overlap policy not encoded** [`docs/diagnostics/diagnostic-registry.json`] — `Hexalith.EventStore` is both an external boundary and a range owner; no test asserts mutual exclusion. Chunk B constraint. Sources: edge. Reconciliation: Row: DW-0644; Resolved 2026-05-11; Evidence: structured externalBoundaries rangePolicy validation.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 059c6346

### DW-1376: `externalBoundaries` array has no schema/provenance [`docs/diagnostics/diagnostic-registry.json:84-86`] — static list with no comment/owner/update-policy field. Chunk B schema work. Sources: blind. Reconciliation: Row: DW-0645; Resolved 2026-05-11; Evidence: externalBoundaries package/owner/rangePolicy/provenance/updatePolicy/rationale schema.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: docs/diagnostics/diagnostic-registry.json:84-86
reason: **DEF-9-4-A4 — `externalBoundaries` array has no schema/provenance** [`docs/diagnostics/diagnostic-registry.json:84-86`] — static list with no comment/owner/update-policy field. Chunk B schema work. Sources: blind. Reconciliation: Row: DW-0645; Resolved 2026-05-11; Evidence: externalBoundaries package/owner/rangePolicy/provenance/updatePolicy/rationale schema.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 059c6346

### DW-1377: `relatedIds` is empty for every entry [`docs/diagnostics/diagnostic-registry.json`] — schema seam present but unused; logical relationships (e.g., HFC0001 ↔ replacement, HFC1056/57 ↔ authorization siblings) not encoded. Authoring task. Sources: blind. Reconciliation: Row: DW-0646; Resolved 2026-05-11; Evidence: relatedIds populated for HFC0001/HFC4001, HFC1037/HFC1040/HFC1044/HFC1601, and HFC1056/HFC1057 with reciprocal tests.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: docs/diagnostics/diagnostic-registry.json
reason: **DEF-9-4-A5 — `relatedIds` is empty for every entry** [`docs/diagnostics/diagnostic-registry.json`] — schema seam present but unused; logical relationships (e.g., HFC0001 ↔ replacement, HFC1056/57 ↔ authorization siblings) not encoded. Authoring task. Sources: blind. Reconciliation: Row: DW-0646; Resolved 2026-05-11; Evidence: relatedIds populated for HFC0001/HFC4001, HFC1037/HFC1040/HFC1044/HFC1601, and HFC1056/HFC1057 with reciprocal tests.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: docs/diagnostics/diagnostic-registry.json:1

### DW-1378: Drift-sample timestamp guard blocks only the current year [`tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:285`] — fragile across year boundaries; should match `\b(19|20)\d{2}\b`. Chunk B test. Sources: edge. Reconciliation: Row: DW-0647; Resolved 2026-05-11; Evidence: YearLiteralRegex guard in DriftSampleReports_AreNormalizedAndCommitted.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:285
reason: **DEF-9-4-A6 — Drift-sample timestamp guard blocks only the current year** [`tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:285`] — fragile across year boundaries; should match `\b(19|20)\d{2}\b`. Chunk B test. Sources: edge. Reconciliation: Row: DW-0647; Resolved 2026-05-11; Evidence: YearLiteralRegex guard in DriftSampleReports_AreNormalizedAndCommitted.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:285

### DW-1379: `DocsSlugValidation` does not block LRM/RLM (U+200E/U+200F) or NFC-normalize [`tests/.../DiagnosticRegistryTests.cs:318-319`] — extend `IsZeroWidth`; ideally whitelist `^diagnostics/HFC[0-9]{4}$`. Chunk B. Sources: edge. Reconciliation: Row: DW-0648; Resolved 2026-05-11; Evidence: DocsSlugValidation_DistinguishesUnsafeCanonicalizationFailures covers LRM/RLM, bidi, NFC, malformed and double-encoded input.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: tests/.../DiagnosticRegistryTests.cs:318-319
reason: **DEF-9-4-A7 — `DocsSlugValidation` does not block LRM/RLM (U+200E/U+200F) or NFC-normalize** [`tests/.../DiagnosticRegistryTests.cs:318-319`] — extend `IsZeroWidth`; ideally whitelist `^diagnostics/HFC[0-9]{4}$`. Chunk B. Sources: edge. Reconciliation: Row: DW-0648; Resolved 2026-05-11; Evidence: DocsSlugValidation_DistinguishesUnsafeCanonicalizationFailures covers LRM/RLM, bidi, NFC, malformed and double-encoded input.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:318

### DW-1380: Sample drift JSON not validated against an actual schema [`tests/.../DiagnosticRegistryTests.cs:265-291`] — only top-level keys are asserted; `findings[]` shape unvalidated, so producer-side renames go undetected. Chunk B fixture-schema work. Sources: edge. Reconciliation: Row: DW-0649; Resolved 2026-05-11; Evidence: sample findings schema validation in DiagnosticRegistryTests.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: tests/.../DiagnosticRegistryTests.cs:265-291
reason: **DEF-9-4-A8 — Sample drift JSON not validated against an actual schema** [`tests/.../DiagnosticRegistryTests.cs:265-291`] — only top-level keys are asserted; `findings[]` shape unvalidated, so producer-side renames go undetected. Chunk B fixture-schema work. Sources: edge. Reconciliation: Row: DW-0649; Resolved 2026-05-11; Evidence: sample findings schema validation in DiagnosticRegistryTests.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 059c6346

### DW-1381: `Hexalith.Tenants` boundary has no range entry; latent collision risk — present as boundary but no range owner; first Tenants-owned diagnostic would have to either pick a different owner or break the boundary. Chunk B constraint. Sources: edge. Reconciliation: Row: DW-0650; Resolved 2026-05-11; Evidence: Hexalith.Tenants no-range-reserved boundary policy in diagnostic-registry.json and README.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: diagnostic-registry.json
reason: **DEF-9-4-A9 — `Hexalith.Tenants` boundary has no range entry; latent collision risk** — present as boundary but no range owner; first Tenants-owned diagnostic would have to either pick a different owner or break the boundary. Chunk B constraint. Sources: edge. Reconciliation: Row: DW-0650; Resolved 2026-05-11; Evidence: Hexalith.Tenants no-range-reserved boundary policy in diagnostic-registry.json and README.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: docs/diagnostics/diagnostic-registry.json:1

### DW-1382: `SourceFiles()` enumeration is filesystem-order dependent [`tests/.../DiagnosticRegistryTests.cs:343-350`] — non-deterministic error reporting across platforms. Add `OrderBy(p => p, Ordinal)` before iteration. Chunk B test. Sources: edge. Reconciliation: Row: DW-0651; Resolved 2026-05-11; Evidence: EnumerateOwnedFiles uses ordinal OrderBy and governance tests passed.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: tests/.../DiagnosticRegistryTests.cs:343-350
reason: **DEF-9-4-A10 — `SourceFiles()` enumeration is filesystem-order dependent** [`tests/.../DiagnosticRegistryTests.cs:343-350`] — non-deterministic error reporting across platforms. Add `OrderBy(p => p, Ordinal)` before iteration. Chunk B test. Sources: edge. Reconciliation: Row: DW-0651; Resolved 2026-05-11; Evidence: EnumerateOwnedFiles uses ordinal OrderBy and governance tests passed.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:343

### DW-1383: `DocsLinkPrefix` constant duplicates `canonicalHelpLinkFormat` (two sources of truth) [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`] — analyzer descriptor should derive from registry's `canonicalHelpLinkFormat` instead of hardcoding the host. Larger refactor; deferred. Sources: auditor. Reconciliation: Row: DW-0652; Resolved 2026-05-11; Evidence: DiagnosticDescriptors.CanonicalHelpLinkFormat is the descriptor helper source and descriptor HelpLinkUri parity is asserted.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs
reason: **DEF-9-4-A11 — `DocsLinkPrefix` constant duplicates `canonicalHelpLinkFormat` (two sources of truth)** [`src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`] — analyzer descriptor should derive from registry's `canonicalHelpLinkFormat` instead of hardcoding the host. Larger refactor; deferred. Sources: auditor. Reconciliation: Row: DW-0652; Resolved 2026-05-11; Evidence: DiagnosticDescriptors.CanonicalHelpLinkFormat is the descriptor helper source and descriptor HelpLinkUri parity is asserted.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs:1

### DW-1384: Story 9-5 docs-root containment policy not encoded as a JSON-schema constraint on `docsSlug` [`docs/diagnostics/diagnostic-registry.json`] — README narrates the constraint but no validator-enforced schema; chunk B/C work. Sources: auditor. Reconciliation: Row: DW-0653; Resolved 2026-05-11; Evidence: IsCanonicalDocsSlug rejects encoded traversal, rooted paths, mixed separators, query/fragment, format chars, non-NFC, and double encoding.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: docs/diagnostics/diagnostic-registry.json
reason: **DEF-9-4-A12 — Story 9-5 docs-root containment policy not encoded as a JSON-schema constraint on `docsSlug`** [`docs/diagnostics/diagnostic-registry.json`] — README narrates the constraint but no validator-enforced schema; chunk B/C work. Sources: auditor. Reconciliation: Row: DW-0653; Resolved 2026-05-11; Evidence: IsCanonicalDocsSlug rejects encoded traversal, rooted paths, mixed separators, query/fragment, format chars, non-NFC, and double encoding.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: docs/diagnostics/diagnostic-registry.json:1

### DW-1385: `Directory.Build.props` `EnablePackageValidation` block is dead code at evaluation time [`Directory.Build.props:7-13`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:248`] — `Condition="'$(IsPackable)' == 'true'"` evaluates before the csproj sets `IsPackable`, so the block never fires. Chunk-A revert kept the literal text to satisfy the chunk-B test contract; the proper fix is to move the block into a new `Directory.Build.targets` (imported after csproj) and update `PackableProjects_UsePackageValidationBaselinePolicy` to inspect that file plus an opt-in `EnableFrontComposerPackageValidation` switch. Sources: edge+auditor. Reconciliation: Row: DW-0654; Resolved 2026-05-11; Evidence: package-validation properties moved to Directory.Build.targets behind EnableFrontComposerPackageValidation and tests updated.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: Directory.Build.props
reason: **DEF-9-4-A13 — `Directory.Build.props` `EnablePackageValidation` block is dead code at evaluation time** [`Directory.Build.props:7-13`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:248`] — `Condition="'$(IsPackable)' == 'true'"` evaluates before the csproj sets `IsPackable`, so the block never fires. Chunk-A revert kept the literal text to satisfy the chunk-B test contract; the proper fix is to move the block into a new `Directory.Build.targets` (imported after csproj) and update `PackableProjects_UsePackageValidationBaselinePolicy` to inspect that file plus an opt-in `EnableFrontComposerPackageValidation` switch. Sources: edge+auditor. Reconciliation: Row: DW-0654; Resolved 2026-05-11; Evidence: package-validation properties moved to Directory.Build.targets behind EnableFrontComposerPackageValidation and tests updated.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 059c6346

### DW-1386: HFC1601 ownerPackage / emit-location inconsistency formalization [`docs/diagnostics/diagnostic-registry.json` HFC1601 entry] — registry has `ownerPackage: "SourceTools"` (matches numeric range) but the production descriptor / runtime emission lives in the Shell project (`FrontComposerRegistry.ValidateManifests`, `CustomizationContractValidationGate`). Chunk-A revert kept ownerPackage=SourceTools to satisfy `RegistryContract_IsVersionedSortedUniqueAndRangeOwned`; lifecycleNote now documents the inconsistency. Chunk-B follow-up: introduce a `crossPackageRangeException` field + test guard, OR add an emit-location-vs-range check that allows registered exceptions. Sources: blind+auditor. Reconciliation: Row: DW-0655; Resolved 2026-05-11; Evidence: allowedExceptions.crossPackageRange contains only HFC1601 with owner/consumer/rationale/help-link/story provenance.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: docs/diagnostics/diagnostic-registry.json
reason: **DEF-9-4-A14 — HFC1601 ownerPackage / emit-location inconsistency formalization** [`docs/diagnostics/diagnostic-registry.json` HFC1601 entry] — registry has `ownerPackage: "SourceTools"` (matches numeric range) but the production descriptor / runtime emission lives in the Shell project (`FrontComposerRegistry.ValidateManifests`, `CustomizationContractValidationGate`). Chunk-A revert kept ownerPackage=SourceTools to satisfy `RegistryContract_IsVersionedSortedUniqueAndRangeOwned`; lifecycleNote now documents the inconsistency. Chunk-B follow-up: introduce a `crossPackageRangeException` field + test guard, OR add an emit-location-vs-range check that allows registered exceptions. Sources: blind+auditor. Reconciliation: Row: DW-0655; Resolved 2026-05-11; Evidence: allowedExceptions.crossPackageRange contains only HFC1601 with owner/consumer/rationale/help-link/story provenance.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: docs/diagnostics/diagnostic-registry.json:1

### DW-1387: HFC1040 severity asymmetry vs HFC1037 / HFC1044 [`docs/diagnostics/diagnostic-registry.json`] — L3 slot override duplicate is `Warning`/`allowed-with-rationale`; L2 (HFC1037) and L4 (HFC1044) are `Error`/`discouraged-error`. Asymmetry may be intentional (L3 is permissive override) or a copy-paste regression. Confirm via chunk-B fixture-driven severity-pinning test; if intentional, document the design rationale in story-creation-lessons or registry lifecycleNote. Sources: blind. Reconciliation: Row: DW-0656; Resolved 2026-05-11; Evidence: HFC1040 lifecycleNote documents intentional L3 warning asymmetry and severity matrix is pinned.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: docs/diagnostics/diagnostic-registry.json
reason: **DEF-9-4-A15 — HFC1040 severity asymmetry vs HFC1037 / HFC1044** [`docs/diagnostics/diagnostic-registry.json`] — L3 slot override duplicate is `Warning`/`allowed-with-rationale`; L2 (HFC1037) and L4 (HFC1044) are `Error`/`discouraged-error`. Asymmetry may be intentional (L3 is permissive override) or a copy-paste regression. Confirm via chunk-B fixture-driven severity-pinning test; if intentional, document the design rationale in story-creation-lessons or registry lifecycleNote. Sources: blind. Reconciliation: Row: DW-0656; Resolved 2026-05-11; Evidence: HFC1040 lifecycleNote documents intentional L3 warning asymmetry and severity matrix is pinned.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: docs/diagnostics/diagnostic-registry.json:1

### DW-1388: Per-diagnostic title authoring (~140 entries) [`docs/diagnostics/diagnostic-registry.json` + 106 docs stubs] — many entries use synthesized `HFCxxxx Title Words` form; chunk-A applied only the mechanical "Git Hub"→"GitHub" fix to keep the docs-stub title-equality test passing. Full per-diagnostic title authoring (replacing redundant prefix with natural-language descriptions) is Story 9-5 / chunk-C work and requires concurrent docs-stub updates. Sources: auditor. Reconciliation: Row: DW-0657; Rejected with rationale 2026-05-11; Evidence: Story 11.2 prioritized HFC0001 and HFC1601 title/prose cleanup per AC26; full-corpus rewrite is beyond Story 11.2 budget and requires a Product/Architecture decision (Story 9-5 prose pass candidate).

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: docs/diagnostics/diagnostic-registry.json
reason: **DEF-9-4-A16 — Per-diagnostic title authoring (~140 entries)** [`docs/diagnostics/diagnostic-registry.json` + 106 docs stubs] — many entries use synthesized `HFCxxxx Title Words` form; chunk-A applied only the mechanical "Git Hub"→"GitHub" fix to keep the docs-stub title-equality test passing. Full per-diagnostic title authoring (replacing redundant prefix with natural-language descriptions) is Story 9-5 / chunk-C work and requires concurrent docs-stub updates. Sources: auditor. Reconciliation: Row: DW-0657; Rejected with rationale 2026-05-11; Evidence: Story 11.2 prioritized HFC0001 and HFC1601 title/prose cleanup per AC26; full-corpus rewrite is beyond Story 11.2 budget and requires a Product/Architecture decision (Story 9-5 prose pass candidate).
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-1389: Sample drift evidence covers only 4 of ~10 AC20 failure categories [`docs/diagnostics/samples/`] — chunk-A samples cover compatibility-binary-break, docs-stub-missing, registry-out-of-range-id, release-row-missing. Missing: unsupported-schema, reserved/retired misuse, invalid lifecycle transition, encoded docs-root escape, unsafe generated front matter, duplicate ID. Each missing fixture also needs a corresponding chunk-B test asserting the validator emits the named category. Sources: auditor. Reconciliation: Row: DW-0658; Resolved 2026-05-11; Evidence: samples now cover unsupported schema, reserved/retired misuse, invalid lifecycle, encoded docs-root escape, unsafe front matter, duplicate ID, suppression scope, HFCM governance, and existing drift categories.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: docs/diagnostics/samples/
reason: **DEF-9-4-A17 — Sample drift evidence covers only 4 of ~10 AC20 failure categories** [`docs/diagnostics/samples/`] — chunk-A samples cover compatibility-binary-break, docs-stub-missing, registry-out-of-range-id, release-row-missing. Missing: unsupported-schema, reserved/retired misuse, invalid lifecycle transition, encoded docs-root escape, unsafe generated front matter, duplicate ID. Each missing fixture also needs a corresponding chunk-B test asserting the validator emits the named category. Sources: auditor. Reconciliation: Row: DW-0658; Resolved 2026-05-11; Evidence: samples now cover unsupported schema, reserved/retired misuse, invalid lifecycle, encoded docs-root escape, unsafe front matter, duplicate ID, suppression scope, HFCM governance, and existing drift categories.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 059c6346

### DW-1390: Story 9-2 HFCM* migration ids in `AnalyzerReleases.Unshipped.md` trip RS2002 [`src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`, `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj`] — HFCM0000-HFCM9002 are CLI-emitted migration findings (no Roslyn `DiagnosticDescriptor`). They live in the analyzer-releases file so the registry/release-row contract covers them, but Roslyn's release-tracking analyzer cannot find a backing descriptor and emits RS2002. Chunk-A kept the project-wide `<NoWarn>$(NoWarn);RS2002</NoWarn>` (with updated comment) until Story 9-2's migration release-row file is relocated to a CLI-specific artifact. Sources: build verification. Reconciliation: Row: DW-0659; Resolved 2026-05-11; Evidence: HFCM rows moved to docs/diagnostics/migration-findings.json and RS2002 removed from SourceTools project.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-diagnostic-id-system-and-deprecation-policy — Chunk A (governance core) (2026-05-10)"), 2026-08-27
location: AnalyzerReleases.Unshipped.md
reason: **DEF-9-4-HFCM — Story 9-2 HFCM* migration ids in `AnalyzerReleases.Unshipped.md` trip RS2002** [`src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`, `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj`] — HFCM0000-HFCM9002 are CLI-emitted migration findings (no Roslyn `DiagnosticDescriptor`). They live in the analyzer-releases file so the registry/release-row contract covers them, but Roslyn's release-tracking analyzer cannot find a backing descriptor and emits RS2002. Chunk-A kept the project-wide `<NoWarn>$(NoWarn);RS2002</NoWarn>` (with updated comment) until Story 9-2's migration release-row file is relocated to a CLI-specific artifact. Sources: build verification. Reconciliation: Row: DW-0659; Resolved 2026-05-11; Evidence: HFCM rows moved to docs/diagnostics/migration-findings.json and RS2002 removed from SourceTools project.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md:1

### DW-1391: Public surface drift — `DiagnosticDescriptors.DocsLinkPrefix` was renamed to public `CanonicalHelpLinkFormat`; revisit when `EnableFrontComposerPackageValidation` ships against the 0.1.0 baseline. (blind)

origin: migrated from legacy ledger ("Deferred from: code review of story-11-2-diagnostic-registry-and-documentation-governance-follow-ups (2026-05-11)"), 2026-08-27
location: DiagnosticDescriptors.DocsLinkPrefix
reason: Public surface drift — `DiagnosticDescriptors.DocsLinkPrefix` was renamed to public `CanonicalHelpLinkFormat`; revisit when `EnableFrontComposerPackageValidation` ships against the 0.1.0 baseline. (blind)
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: docs/diagnostics/README.md:7 and DiagnosticRegistryTests.cs:945-963 show package validation against baseline 4.1.1 and reject the former baseline values.
decision: 2026-08-27 Keep open
decision: 2026-08-27 Keep open

### DW-1394: HFC1601 `lifecycleNote` no longer references the specific Shell runtime emit class; if those classes are renamed, the structured `allowedExceptions.crossPackageRange` row still passes but traceability anchor is lost. Prose judgment call. (blind+edge)

origin: migrated from legacy ledger ("Deferred from: code review of story-11-2-diagnostic-registry-and-documentation-governance-follow-ups (2026-05-11)"), 2026-08-27
location: allowedExceptions.crossPackageRange
reason: HFC1601 `lifecycleNote` no longer references the specific Shell runtime emit class; if those classes are renamed, the structured `allowedExceptions.crossPackageRange` row still passes but traceability anchor is lost. Prose judgment call. (blind+edge)
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-1397: `introducedIn: "0.2.0"` semantic correctness — six HFCM rows in `docs/diagnostics/migration-findings.json` use the literal `"0.2.0"`; resolve as a documentation decision whether this represents tool version (Story 9-2 CLI) or FrontComposer release. Reconciliation: Row: DW-0660; Resolved 2026-05-12; Evidence: docs/diagnostics/README.md; User-visible behavior: `introducedIn` records the FrontComposer CLI/tooling release that introduced the CLI-emitted finding, not the first product release containing the migrated API. (blind)

origin: migrated from legacy ledger ("Deferred from: code review of story-11-2-diagnostic-registry-and-documentation-governance-follow-ups — pass 2 (2026-05-11)"), 2026-08-27
location: docs/diagnostics/migration-findings.json
reason: `introducedIn: "0.2.0"` semantic correctness — six HFCM rows in `docs/diagnostics/migration-findings.json` use the literal `"0.2.0"`; resolve as a documentation decision whether this represents tool version (Story 9-2 CLI) or FrontComposer release. Reconciliation: Row: DW-0660; Resolved 2026-05-12; Evidence: docs/diagnostics/README.md; User-visible behavior: `introducedIn` records the FrontComposer CLI/tooling release that introduced the CLI-emitted finding, not the first product release containing the migrated API. (blind)
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: docs/diagnostics/migration-findings.json:1

### DW-1398: All `approvedOn` rows stamped with the same `2026-05-11` date — audit-trail design choice (single batch approval vs per-row sign-off). Validator doesn't enforce per-row distinct dates. Reconciliation: Row: DW-0661; Non-action decision 2026-05-11; Decision owner: Story 11.2 review pass 2; Rationale: batch approval is acceptable for the initial HFCM CLI migration cohort; Evidence: `docs/diagnostics/migration-findings.json`. (blind)

origin: migrated from legacy ledger ("Deferred from: code review of story-11-2-diagnostic-registry-and-documentation-governance-follow-ups — pass 2 (2026-05-11)"), 2026-08-27
location: docs/diagnostics/migration-findings.json
reason: All `approvedOn` rows stamped with the same `2026-05-11` date — audit-trail design choice (single batch approval vs per-row sign-off). Validator doesn't enforce per-row distinct dates. Reconciliation: Row: DW-0661; Non-action decision 2026-05-11; Decision owner: Story 11.2 review pass 2; Rationale: batch approval is acceptable for the initial HFCM CLI migration cohort; Evidence: `docs/diagnostics/migration-findings.json`. (blind)
status: done 2026-08-28
archived: 2026-09-18
decision: 2026-08-28 Close as accepted — Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.
resolution: closed by human decision: Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.
decision: 2026-08-28 Close as accepted — Retain the current behavior as an explicit accepted constraint with the ledger evidence preserved.

### DW-1399: `Random rng = new(20260511)` literal seed pattern in `RegistryValidator_DeterministicUnderShuffledInput` — cosmetic; conventional dated seed. Re-evaluate after fixing the shuffle algorithm (Pass-2 patch on `tests/.../DiagnosticRegistryTests.cs:629-650`). Reconciliation: Row: DW-0662; Non-action decision 2026-05-11; Decision owner: Story 11.2 review pass 2; Rationale: deterministic seed pattern recognised; no correctness impact once Fisher-Yates lands; Evidence: `tests/.../DiagnosticRegistryTests.cs:637`. (blind)

origin: migrated from legacy ledger ("Deferred from: code review of story-11-2-diagnostic-registry-and-documentation-governance-follow-ups — pass 2 (2026-05-11)"), 2026-08-27
location: tests/.../DiagnosticRegistryTests.cs:629-650
reason: `Random rng = new(20260511)` literal seed pattern in `RegistryValidator_DeterministicUnderShuffledInput` — cosmetic; conventional dated seed. Re-evaluate after fixing the shuffle algorithm (Pass-2 patch on `tests/.../DiagnosticRegistryTests.cs:629-650`). Reconciliation: Row: DW-0662; Non-action decision 2026-05-11; Decision owner: Story 11.2 review pass 2; Rationale: deterministic seed pattern recognised; no correctness impact once Fisher-Yates lands; Evidence: `tests/.../DiagnosticRegistryTests.cs:637`. (blind)
status: done 2026-09-06
archived: 2026-09-18
decision: 2026-09-06 Close cosmetic item — The deterministic seed has no correctness or maintenance impact.
resolution: closed by human decision: The deterministic seed has no correctness or maintenance impact.
decision: 2026-09-06 Close cosmetic item — The deterministic seed has no correctness or maintenance impact.

### DW-1400: `_bmad-output` Path.Combine case-asymmetry between Windows (case-insensitive) and Linux CI (case-sensitive) — repo policy is lowercase; rename unlikely. Reconciliation: Row: DW-0663; Non-action decision 2026-05-11; Decision owner: Story 11.2 review pass 2; Rationale: cross-platform exposure is theoretical until anyone renames the directory; Evidence: `tests/.../DiagnosticRegistryTests.cs:659`. (blind)

origin: migrated from legacy ledger ("Deferred from: code review of story-11-2-diagnostic-registry-and-documentation-governance-follow-ups — pass 2 (2026-05-11)"), 2026-08-27
location: tests/.../DiagnosticRegistryTests.cs:659
reason: `_bmad-output` Path.Combine case-asymmetry between Windows (case-insensitive) and Linux CI (case-sensitive) — repo policy is lowercase; rename unlikely. Reconciliation: Row: DW-0663; Non-action decision 2026-05-11; Decision owner: Story 11.2 review pass 2; Rationale: cross-platform exposure is theoretical until anyone renames the directory; Evidence: `tests/.../DiagnosticRegistryTests.cs:659`. (blind)
status: done 2026-09-06
archived: 2026-09-18
decision: 2026-09-06 Close under policy — The repository mandates lowercase _bmad-output and no rename scenario exists.
resolution: closed by human decision: The repository mandates lowercase _bmad-output and no rename scenario exists.
decision: 2026-09-06 Close under policy — The repository mandates lowercase _bmad-output and no rename scenario exists.

### DW-1402: `HfcmIdShapeRegex` vs `SampleFindingIdShapeRegex` divergence — intentional split (HFCM-specific for migration-findings, broader sample regex for placeholder evidence). Confusion risk acknowledged. Reconciliation: Row: DW-0665; Non-action decision 2026-05-11; Decision owner: Story 11.2 review pass 2; Rationale: split is intentional per AC15; document split in `docs/diagnostics/README.md` if Story 11.3 expands HFCM scope; Evidence: `tests/.../DiagnosticRegistryTests.cs:474-475,966`. (edge)

origin: migrated from legacy ledger ("Deferred from: code review of story-11-2-diagnostic-registry-and-documentation-governance-follow-ups — pass 2 (2026-05-11)"), 2026-08-27
location: docs/diagnostics/README.md
reason: `HfcmIdShapeRegex` vs `SampleFindingIdShapeRegex` divergence — intentional split (HFCM-specific for migration-findings, broader sample regex for placeholder evidence). Confusion risk acknowledged. Reconciliation: Row: DW-0665; Non-action decision 2026-05-11; Decision owner: Story 11.2 review pass 2; Rationale: split is intentional per AC15; document split in `docs/diagnostics/README.md` if Story 11.3 expands HFCM scope; Evidence: `tests/.../DiagnosticRegistryTests.cs:474-475,966`. (edge)
status: done 2026-09-06
archived: 2026-09-18
decision: 2026-09-06 Close intentional split — The two validators intentionally accept different evidence domains.
resolution: closed by human decision: The two validators intentionally accept different evidence domains.
decision: 2026-09-06 Close intentional split — The two validators intentionally accept different evidence domains.

### DW-1403: `IsRootedSlug` UNC `//server/share` and drive-relative `C:foo` semantics — boundary policy decision (should drive-relative paths fail like absolute paths, or be accepted). Reconciliation: Row: DW-0666; Release gate 2026-05-14; Gate: docs-slug UNC and drive-relative policy decision; Product owner: Product Owner role; Architecture owner: Architect role; Decision date target: TBD on next Product/Architecture review trigger; Policy options: reject UNC-like `//server/share` and drive-relative `C:foo` as rooted/hostile slug forms, accept only after explicit docs-slug normalization rationale, or split platform-specific handling with test-backed exceptions; Recommended default: reject both forms fail-closed until Product and Architecture select otherwise; Evidence: `tests/.../DiagnosticRegistryTests.cs:929-945`, `_bmad-output/implementation-artifacts/11-2-diagnostic-registry-and-documentation-governance-follow-ups.md`, and `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13.md`; Downstream impact: diagnostic docs slug validation and canonical help-link safety; Release consequence: Epic 11 remains in-progress and v1 release certification is blocked for this policy until selected; Closure trigger: Product and Architecture record the selected docs-slug policy and matching tests/evidence; Previous ambiguous marker is preserved by row history. Final classification 2026-06-21: resolved; Decision owner: Administrator (Product + Architecture owner); Selected policy: option (a) — reject UNC (`//server/share`) and drive-relative (`C:`) slug forms fail-closed as rooted/hostile (accept-with-rationale and split-per-platform declined — no off-site docs-hosting driver); Already-enforced: `IsRootedSlug` classifies both shapes as rooted and the `== "diagnostics/{id}"` exact-match gate rejects them again; Pinned: `DiagnosticRegistryTests.cs` `DocsSlugValidation_DistinguishesUnsafeCanonicalizationFailures` UNC + drive-relative `invalid-slug` cases (26/26 green 2026-06-21); Release consequence cleared: docs-slug policy selected → the legacy "Epic 11 in-progress" gate is closed and does not affect Epics 1–7 (independently done).

origin: migrated from legacy ledger ("Deferred from: code review of story-11-2-diagnostic-registry-and-documentation-governance-follow-ups — pass 2 (2026-05-11)"), 2026-08-27
location: tests/.../DiagnosticRegistryTests.cs:929-945
reason: `IsRootedSlug` UNC `//server/share` and drive-relative `C:foo` semantics — boundary policy decision (should drive-relative paths fail like absolute paths, or be accepted). Reconciliation: Row: DW-0666; Release gate 2026-05-14; Gate: docs-slug UNC and drive-relative policy decision; Product owner: Product Owner role; Architecture owner: Architect role; Decision date target: TBD on next Product/Architecture review trigger; Policy options: reject UNC-like `//server/share` and drive-relative `C:foo` as rooted/hostile slug forms, accept only after explicit docs-slug normalization rationale, or split platform-specific handling with test-backed exceptions; Recommended default: reject both forms fail-closed until Product and Architecture select otherwise; Evidence: `tests/.../DiagnosticRegistryTests.cs:929-945`, `_bmad-output/implementation-artifacts/11-2-diagnostic-registry-and-documentation-governance-follow-ups.md`, and `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-13.md`; Downstream impact: diagnostic docs slug validation and canonical help-link safety; Release consequence: Epic 11 remains in-progress and v1 release certification is blocked for this policy until selected; Closure trigger: Product and Architecture record the selected docs-slug policy and matching tests/evidence; Previous ambiguous marker is preserved by row history. Final classification 2026-06-21: resolved; Decision owner: Administrator (Product + Architecture owner); Selected policy: option (a) — reject UNC (`//server/share`) and drive-relative (`C:`) slug forms fail-closed as rooted/hostile (accept-with-rationale and split-per-platform declined — no off-site docs-hosting driver); Already-enforced: `IsRootedSlug` classifies both shapes as rooted and the `== "diagnostics/{id}"` exact-match gate rejects them again; Pinned: `DiagnosticRegistryTests.cs` `DocsSlugValidation_DistinguishesUnsafeCanonicalizationFailures` UNC + drive-relative `invalid-slug` cases (26/26 green 2026-06-21); Release consequence cleared: docs-slug policy selected → the legacy "Epic 11 in-progress" gate is closed and does not affect Epics 1–7 (independently done).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:929

### DW-1404: Rows DW-0071, DW-0090, DW-0638 — compatible-additive command admission now revalidates current server `DataAnnotations` before dispatch; skipped DEF-D5 command test was restored. Evidence: `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerSchemaGateTests.cs`, focused MCP validation command.

origin: migrated from legacy ledger ("Fixed in Story 11.5"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs
reason: Rows DW-0071, DW-0090, DW-0638 — compatible-additive command admission now revalidates current server `DataAnnotations` before dispatch; skipped DEF-D5 command test was restored. Evidence: `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerSchemaGateTests.cs`, focused MCP validation command.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:1

### DW-1405: Rows DW-0634, DW-0637 — fingerprint equality now includes algorithm and value; mixed-algorithm runtime aggregate input fails closed as `SchemaIntegrityMismatch`. Evidence: `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs`, `src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationTests.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs`.

origin: migrated from legacy ledger ("Fixed in Story 11.5"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs
reason: Rows DW-0634, DW-0637 — fingerprint equality now includes algorithm and value; mixed-algorithm runtime aggregate input fails closed as `SchemaIntegrityMismatch`. Evidence: `src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs`, `src/Hexalith.FrontComposer.Mcp/Schema/FrontComposerMcpRuntimeManifestAggregator.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaNegotiationTests.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/Schema/SchemaNegotiation.cs:1

### DW-1406: Rows DW-0635, DW-0092 — schema-rejection tool resolution preserves the resolved descriptor internally while public responses continue to use sanitized schema categories. Evidence: `src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs`, `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionSchemaGateTests.cs`.

origin: migrated from legacy ledger ("Fixed in Story 11.5"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs
reason: Rows DW-0635, DW-0092 — schema-rejection tool resolution preserves the resolved descriptor internally while public responses continue to use sanitized schema categories. Evidence: `src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs`, `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionSchemaGateTests.cs`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Mcp/McpToolResolutionResult.cs:1

### DW-1407: Rows DW-0636, DW-0633 — production DI constructor selection is pinned through a corpus-provider invocation test; runtime corpus aggregate remains an explicit v1 release constraint until build-time corpus signing/baseline materialization exists. Evidence: `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs`; owner: Story 11.5; revalidation trigger: build-time corpus signing or generated baseline materialization work.

origin: migrated from legacy ledger ("Fixed in Story 11.5"), 2026-08-27
location: tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs
reason: Rows DW-0636, DW-0633 — production DI constructor selection is pinned through a corpus-provider invocation test; runtime corpus aggregate remains an explicit v1 release constraint until build-time corpus signing/baseline materialization exists. Evidence: `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs`; owner: Story 11.5; revalidation trigger: build-time corpus signing or generated baseline materialization work.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs:136-182 pins corpus-aware DI selection, every registered provider, and the zero-provider path.

### DW-1408: Row DW-0082 — header parser cache, sentinel rethrow, multi-value rejection, empty no-op, uppercase rejection, unsupported algorithm, short, oversized, and malformed values are covered. Evidence: `tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs`.

origin: migrated from legacy ledger ("Fixed in Story 11.5"), 2026-08-27
location: tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs
reason: Row DW-0082 — header parser cache, sentinel rethrow, multi-value rejection, empty no-op, uppercase rejection, unsupported algorithm, short, oversized, and malformed values are covered. Evidence: `tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs:1

### DW-1409: Story 8.5 skill-resource invalid-state rows — `SkillResourceReadResult` construction is factory-only and pinned by success/failure state matrix tests. Evidence: `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs`.

origin: migrated from legacy ledger ("Fixed in Story 11.5"), 2026-08-27
location: src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs
reason: Story 8.5 skill-resource invalid-state rows — `SkillResourceReadResult` construction is factory-only and pinned by success/failure state matrix tests. Evidence: `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpus.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillResourceTests.cs:1

### DW-1414: Category E — Schema negotiator design rows fixed or pinned by Story 11.5

origin: migrated from legacy ledger ("Accepted or Split by Story 11.5 — row-scoped closure matrix (revised 2026-05-12 to address DN16/DN22)"), 2026-08-27
location: MessageKey
reason: **Category E — Schema negotiator design rows fixed or pinned by Story 11.5**     - Rows: DW-0634, DW-0635, DW-0636, DW-0637, DW-0638, DW-0639, DW-0640, DW-0641.     - Disposition/date/rationale: resolved 2026-05-12 in Story 11.5 source/tests. These rows cover cross-algorithm aggregation, descriptor stripping on rejection, DI constructor risk, algorithm-aware fingerprint equality, compatible-additive/warning bypass, obsolete bool cleanup, strict schema failure mapping, and agent-category wire-format pins.     - Original owner: Story 11.5. Revalidation trigger: any public `MessageKey`, `AgentCategory`, `decisionKind`, URI category, lifecycle category, or fingerprint equality behavior changes. Downstream impact: v1 agents keep current machine keys/categories. Regression guard/evidence: `SchemaNegotiationTests.Negotiate_SameValueDifferentSupportedAlgorithm_IsNotExact`, `AggregateManifestIntegrityTests.Aggregator_MixedFingerprintAlgorithms_FailsClosed`, `ToolAdmissionSchemaGateTests`, `CommandInvokerSchemaGateTests`, `Story11_5ResolutionTests`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/Schema/Story11_5ResolutionTests.cs:208 plus AggregateManifestIntegrityTests and ToolAdmissionSchemaGateTests pin the Category-E behavior summarized by this row.

### DW-1415: Constraint: Manifests whose `Fingerprint` is null are accepted (the per-manifest integrity loop in `FrontComposerMcpDescriptorRegistry.ValidateAggregateIntegrity` continues to the next manifest rather than failing closed). The runtime corpus aggregate (`FrontComposerMcpRuntimeManifestAggregator.Compute(manifests, corpusFingerprints)`) is computed but the result is not yet plumbed into a production agent-facing fingerprint header. This is a deliberate v1 choice — the build-time emitter only sees per-manifest content, and hosts that ship no skill corpus must not fail-closed at registration.

origin: migrated from legacy ledger ("D11 missing-claimed-fingerprint / corpus runtime aggregate — v1 release constraint (revised 2026-05-12 to address DN14)"), 2026-08-27
location: Fingerprint
reason: **Constraint:** Manifests whose `Fingerprint` is null are accepted (the per-manifest integrity loop in `FrontComposerMcpDescriptorRegistry.ValidateAggregateIntegrity` continues to the next manifest rather than failing closed). The runtime corpus aggregate (`FrontComposerMcpRuntimeManifestAggregator.Compute(manifests, corpusFingerprints)`) is computed but the result is not yet plumbed into a production agent-facing fingerprint header. This is a deliberate v1 choice — the build-time emitter only sees per-manifest content, and hosts that ship no skill corpus must not fail-closed at registration.
status: done 2026-08-27
archived: 2026-09-18
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-1419: Telemetry/evidence path: `FrontComposerMcpDescriptorRegistry` ctor invokes every registered `ISkillCorpusFingerprintProvider` exactly once at host startup; pinned by `AggregateManifestIntegrityTests.DescriptorRegistry_DiConstruction_InvokesAllRegisteredCorpusProviders` (multi-provider) and `DescriptorRegistry_DiConstruction_ZeroProviders_DoesNotFailClosed` (zero-provider). Per-manifest integrity bypass is exercised by `AggregateManifestIntegrityTests.DescriptorRegistry_LoadingTamperedAggregate_FailsClosed_WithIntegrityMismatch` (which proves manifests with claimed fingerprints still fail closed on tamper).

origin: migrated from legacy ledger ("D11 missing-claimed-fingerprint / corpus runtime aggregate — v1 release constraint (revised 2026-05-12 to address DN14)"), 2026-08-27
location: FrontComposerMcpDescriptorRegistry
reason: **Telemetry/evidence path:** `FrontComposerMcpDescriptorRegistry` ctor invokes every registered `ISkillCorpusFingerprintProvider` exactly once at host startup; pinned by `AggregateManifestIntegrityTests.DescriptorRegistry_DiConstruction_InvokesAllRegisteredCorpusProviders` (multi-provider) and `DescriptorRegistry_DiConstruction_ZeroProviders_DoesNotFailClosed` (zero-provider). Per-manifest integrity bypass is exercised by `AggregateManifestIntegrityTests.DescriptorRegistry_LoadingTamperedAggregate_FailsClosed_WithIntegrityMismatch` (which proves manifests **with** claimed fingerprints still fail closed on tamper).
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/Schema/AggregateManifestIntegrityTests.cs:21,149,170 exercises tampered-aggregate failure and both multi-provider and zero-provider DI construction.

### DW-1420: Regression guard: the three DI-composition tests above plus the new `Story11_5ResolutionTests.DN10_FourWayFingerprintConflict_FailsClosed_WithoutDowngrade` (four-source disagreement still fails closed) and `Story11_5ResolutionTests.DN8_SentinelRedaction_*` (no corpus-side leak).

origin: migrated from legacy ledger ("D11 missing-claimed-fingerprint / corpus runtime aggregate — v1 release constraint (revised 2026-05-12 to address DN14)"), 2026-08-27
location: Story11_5ResolutionTests.DN10_FourWayFingerprintConflict_FailsClosed_WithoutDowngrade
reason: **Regression guard:** the three DI-composition tests above plus the new `Story11_5ResolutionTests.DN10_FourWayFingerprintConflict_FailsClosed_WithoutDowngrade` (four-source disagreement still fails closed) and `Story11_5ResolutionTests.DN8_SentinelRedaction_*` (no corpus-side leak).
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/Schema/Story11_5ResolutionTests.cs:56,156,208 pins sentinel redaction and four-way fingerprint-conflict fail-closed behavior.

### DW-1426: `ReconciliationSweepReducer` expired-marker comment vs code mismatch: Comment says "skip markers already expired at insertion" but no pre-insertion expiry guard exists. Pre-existing accuracy issue in `ReconciliationSweepState.cs`. Owner: next sweep reducer work item.

origin: migrated from legacy ledger ("Deferred from: code review of 11-6-shell-ux-accessibility-and-sample-coverage-follow-ups (2026-05-13)"), 2026-08-27
location: ReconciliationSweepState.cs
reason: **W6 — `ReconciliationSweepReducer` expired-marker comment vs code mismatch:** Comment says "skip markers already expired at insertion" but no pre-insertion expiry guard exists. Pre-existing accuracy issue in `ReconciliationSweepState.cs`. Owner: next sweep reducer work item.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconciliationSweepState.cs:1

### DW-1428: `DevModeOverlayController.Register` `_nodes` write outside `_selectionLock`: Dictionary write and selection-refresh lock are not atomic. Design intent for Blazor scoped single-threaded circuit model; not a production correctness defect today. Owner: future thread-safety hardening if multi-threaded prerender is targeted.

origin: migrated from legacy ledger ("Deferred from: code review of 11-6-shell-ux-accessibility-and-sample-coverage-follow-ups (2026-05-13)"), 2026-08-27
location: DevModeOverlayController.Register
reason: **W8 — `DevModeOverlayController.Register` `_nodes` write outside `_selectionLock`:** Dictionary write and selection-refresh lock are not atomic. Design intent for Blazor scoped single-threaded circuit model; not a production correctness defect today. Owner: future thread-safety hardening if multi-threaded prerender is targeted.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:5366

### DW-1429: `TryGetClrGenericStarterName` regex misses nested types and assembly-qualified tokens: `[A-Za-z0-9_.]` excludes `+`, `-`, `=`, `,` from CLR generic argument syntax; nested types fall back to `{baseName}_Arity{n}`. Consistent with accepted constraints in Story 11.6. Owner: starter-template generator hardening.

origin: migrated from legacy ledger ("Deferred from: code review of 11-6-shell-ux-accessibility-and-sample-coverage-follow-ups (2026-05-13)"), 2026-08-27
location: TryGetClrGenericStarterName
reason: **W9 — `TryGetClrGenericStarterName` regex misses nested types and assembly-qualified tokens:** `[A-Za-z0-9_.]` excludes `+`, `-`, `=`, `,` from CLR generic argument syntax; nested types fall back to `{baseName}_Arity{n}`. Consistent with accepted constraints in Story 11.6. Owner: starter-template generator hardening.
status: done 2026-09-06
archived: 2026-09-18
decision: 2026-09-06 Close accepted fallback — The Arity fallback is approved behavior for unsupported CLR token shapes.
resolution: closed by human decision: The Arity fallback is approved behavior for unsupported CLR token shapes.
decision: 2026-09-06 Close accepted fallback — The Arity fallback is approved behavior for unsupported CLR token shapes.

### DW-1431: Pre-image Reconciliation Summary bucket totals did not reconcile: Old bucket counts `unresolved-owned 637 + unresolved-ambiguous 0 + duplicate-alias 6 + resolved-preserved 13 + superseded-preserved 0 + non-action 2 + rejected-with-rationale 4 + split-parent 0 = 662`, but the pre-image total line claimed `659`. Story 12.1 rewrote the summary; the table-authoritative totals reconcile as `unresolved-owned 0 + unresolved-ambiguous 0 + duplicate-alias 6 + resolved-preserved 119 + accepted-constraint 149 + split-to-named-story 377 + superseded-preserved 3 + non-action 7 + rejected-with-rationale 4 + release-gate 1 = 666`. (Earlier 12.1 hand-rolled `91+112+442` figures were superseded by the live bucket table after Story 12.2/12.3/12.5 reclassifications; treat the bucket table as the source of truth.) The 3-row historical drift versus the 659 pre-image total was inherited, not introduced. Owner: deferred-work ledger maintainer (one-time audit on next ledger touch).

origin: migrated from legacy ledger ("Deferred from: code review of 12-1-ledger-marker-parity-and-epic-status-decision (2026-05-14)"), 2026-08-27
location: n/a
reason: **W1 — Pre-image Reconciliation Summary bucket totals did not reconcile:** Old bucket counts `unresolved-owned 637 + unresolved-ambiguous 0 + duplicate-alias 6 + resolved-preserved 13 + superseded-preserved 0 + non-action 2 + rejected-with-rationale 4 + split-parent 0 = 662`, but the pre-image total line claimed `659`. Story 12.1 rewrote the summary; the table-authoritative totals reconcile as `unresolved-owned 0 + unresolved-ambiguous 0 + duplicate-alias 6 + resolved-preserved 119 + accepted-constraint 149 + split-to-named-story 377 + superseded-preserved 3 + non-action 7 + rejected-with-rationale 4 + release-gate 1 = 666`. (Earlier 12.1 hand-rolled `91+112+442` figures were superseded by the live bucket table after Story 12.2/12.3/12.5 reclassifications; treat the bucket table as the source of truth.) The 3-row historical drift versus the 659 pre-image total was inherited, not introduced. Owner: deferred-work ledger maintainer (one-time audit on next ledger touch).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit dd74efc4 replaced the legacy reconciliation summary and its inconsistent pre-image totals with canonical DW blocks.

### DW-1432: Pre-image per-owner table claimed Story 11.5 = 204 but detailed-row count was 205: Story 12.1's deterministic starting inventory consumed the correct count of 205 (matching the frozen `sha256:d0df4a8…780e83` fingerprint). The old per-owner summary table was stale by one row. Now corrected to 0 (after splits). Owner: deferred-work ledger maintainer.

origin: migrated from legacy ledger ("Deferred from: code review of 12-1-ledger-marker-parity-and-epic-status-decision (2026-05-14)"), 2026-08-27
location: n/a
reason: **W2 — Pre-image per-owner table claimed Story 11.5 = 204 but detailed-row count was 205:** Story 12.1's deterministic starting inventory consumed the correct count of 205 (matching the frozen `sha256:d0df4a8…780e83` fingerprint). The old per-owner summary table was stale by one row. Now corrected to 0 (after splits). Owner: deferred-work ledger maintainer.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit dd74efc4 removed the stale legacy per-owner summary while migrating the ledger to canonical DW blocks.

### DW-1433: `epic-11-retrospective: done` while `epic-11: in-progress` is a pre-existing internal inconsistency: `sprint-status.yaml:147` holds `epic-11: in-progress` (correctly, per `DW-0666` release gate); `sprint-status.yaml:155` holds `epic-11-retrospective: done`. A retrospective marked done while the epic remains open implies either the retrospective predated the `DW-0666` gate or the epic should already be closed. Story 12.1 did not modify the retrospective status. Owner: sprint-status / Epic 11 retrospective owner.

origin: migrated from legacy ledger ("Deferred from: code review of 12-1-ledger-marker-parity-and-epic-status-decision (2026-05-14)"), 2026-08-27
location: sprint-status.yaml:147
reason: **W3 — `epic-11-retrospective: done` while `epic-11: in-progress` is a pre-existing internal inconsistency:** `sprint-status.yaml:147` holds `epic-11: in-progress` (correctly, per `DW-0666` release gate); `sprint-status.yaml:155` holds `epic-11-retrospective: done`. A retrospective marked done while the epic remains open implies either the retrospective predated the `DW-0666` gate or the epic should already be closed. Story 12.1 did not modify the retrospective status. Owner: sprint-status / Epic 11 retrospective owner.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/sprint-status.yaml:143 keeps epic-11 in-progress and line 168 marks epic-11-retrospective optional, removing the reported contradiction.
decision: 2026-08-27 Implement change — Implement the behavior requested by DW-1433, update affected contracts and consumers, and add focused regression evidence.

### DW-1434: Bucket vocabulary aliases are undocumented in `deferred-work.md`: Strict-vocabulary counts (`resolved=70`, `accepted-constraint=50`, `fixed-in-11.6=11`, `fixed-in-11.7=10`, `accepted-with-risk=62`) reconcile to the summary buckets (`resolved-preserved=91`, `accepted-constraint=112`) only via undocumented aliases (`resolved-preserved = resolved + fixed-in-11.6 + fixed-in-11.7`; `accepted-constraint = strict-accepted + accepted-with-risk`). The Reconciliation Status frontmatter enumerates only the strict tokens. Externally replayable verification requires the alias spec. Owner: deferred-work ledger maintainer (add a vocabulary mapping subsection on next ledger touch).

origin: migrated from legacy ledger ("Deferred from: code review of 12-1-ledger-marker-parity-and-epic-status-decision (2026-05-14)"), 2026-08-27
location: deferred-work.md
reason: **W4 — Bucket vocabulary aliases are undocumented in `deferred-work.md`:** Strict-vocabulary counts (`resolved=70`, `accepted-constraint=50`, `fixed-in-11.6=11`, `fixed-in-11.7=10`, `accepted-with-risk=62`) reconcile to the summary buckets (`resolved-preserved=91`, `accepted-constraint=112`) only via undocumented aliases (`resolved-preserved = resolved + fixed-in-11.6 + fixed-in-11.7`; `accepted-constraint = strict-accepted + accepted-with-risk`). The Reconciliation Status frontmatter enumerates only the strict tokens. Externally replayable verification requires the alias spec. Owner: deferred-work ledger maintainer (add a vocabulary mapping subsection on next ledger touch).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit dd74efc4 removed the legacy bucket-alias summary vocabulary; the current ledger is status-driven DW blocks.

### DW-1435: `W1`–`W9` review-finding bullets in `## Deferred from: code review …` sections lack `DW-####` Reconciliation markers: Bullets at `deferred-work.md:1118-1126` (11-6 review) and `:1130` (11-7 review) carry `Owner: …` lines but no `Reconciliation: Row: DW-####` marker. T1's "Fail before mutation if any detailed current marker has a missing, malformed, or duplicate `DW-####` row ID" was satisfied by Story 12.1 only because the inventory script scans rows that already carry `DW-####` and skips bullets that don't — i.e., the script does not fail-close on missing IDs as T1 specified. Pre-existing inventory-script gap, not introduced by this commit. Owner: deferred-work inventory-script maintainer.

origin: migrated from legacy ledger ("Deferred from: code review of 12-1-ledger-marker-parity-and-epic-status-decision (2026-05-14)"), 2026-08-27
location: deferred-work.md:1118-1126
reason: **W5 — `W1`–`W9` review-finding bullets in `## Deferred from: code review …` sections lack `DW-####` Reconciliation markers:** Bullets at `deferred-work.md:1118-1126` (11-6 review) and `:1130` (11-7 review) carry `Owner: …` lines but no `Reconciliation: Row: DW-####` marker. T1's "Fail before mutation if any detailed current marker has a missing, malformed, or duplicate `DW-####` row ID" was satisfied by Story 12.1 only because the inventory script scans rows that already carry `DW-####` and skips bullets that don't — i.e., the script does not fail-close on missing IDs as T1 specified. Pre-existing inventory-script gap, not introduced by this commit. Owner: deferred-work inventory-script maintainer.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit dd74efc4

### DW-1443: AC28 / T7.5: adversarial MCP redaction fixtures referenced but not produced: Dev Agent Record claims a textual scan over evidence; AC28/T7.5 read "fixtures". No fixture artifact added in the diff. Documented scan remains as interim evidence. Owner: Story 12.4 (`12-4-trusted-release-evidence-dry-run`), the natural home for release-evidence fixture work.

origin: migrated from legacy ledger ("Deferred from: code review of 12-2-mcp-ledger-closure-and-contract-snapshot-decisions (2026-05-15)"), 2026-08-27
location: n/a
reason: **W8 — AC28 / T7.5: adversarial MCP redaction fixtures referenced but not produced:** Dev Agent Record claims a textual scan over evidence; AC28/T7.5 read "fixtures". No fixture artifact added in the diff. Documented scan remains as interim evidence. Owner: Story 12.4 (`12-4-trusted-release-evidence-dry-run`), the natural home for release-evidence fixture work.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:5471

### DW-1445: Ledger total drifted 659→666 in this commit without per-row reconciliation: The bucket-table total in `deferred-work.md` moved from 659 to 666 across Story 12.1/12.2 fan-out; Story 12.3 inherits but does not own the drift. Pre-existing Story 12.1/12.2 ledger churn, not introduced by the pending-command release-gate work. Owner: Story 12.1 / Story 12.2 follow-up audit.

origin: migrated from legacy ledger ("Deferred from: code review of 12-3-eventstore-pending-command-provider-release-gate (2026-05-15)"), 2026-08-27
location: deferred-work.md
reason: **W1 — Ledger total drifted 659→666 in this commit without per-row reconciliation:** The bucket-table total in `deferred-work.md` moved from 659 to 666 across Story 12.1/12.2 fan-out; Story 12.3 inherits but does not own the drift. Pre-existing Story 12.1/12.2 ledger churn, not introduced by the pending-command release-gate work. Owner: Story 12.1 / Story 12.2 follow-up audit.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit dd74efc4 removed the legacy 659/666 bucket-total summary whose drift this row reported.

### DW-1447: `epic-11-retrospective: done` while `epic-11: in-progress` remains unresolved: Pre-existing sprint-status inconsistency acknowledged in Story 12.1's W3 review note. Story 12.3 did not touch the retrospective status. Owner: sprint-status / Epic 11 retrospective owner.

origin: migrated from legacy ledger ("Deferred from: code review of 12-3-eventstore-pending-command-provider-release-gate (2026-05-15)"), 2026-08-27
location: n/a
reason: **W3 — `epic-11-retrospective: done` while `epic-11: in-progress` remains unresolved:** Pre-existing sprint-status inconsistency acknowledged in Story 12.1's W3 review note. Story 12.3 did not touch the retrospective status. Owner: sprint-status / Epic 11 retrospective owner.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/sprint-status.yaml:143 and :168 record epic-11 in-progress and retrospective optional, removing the reported contradiction.

### DW-1448: Inventory script silently skips bullets lacking `DW-####` markers: Pre-existing inventory-script gap from Story 12.1's W5 review note. Story 12.3's "Row inventory/hash | Passed" claim inherits the gap. Owner: deferred-work inventory-script maintainer.

origin: migrated from legacy ledger ("Deferred from: code review of 12-3-eventstore-pending-command-provider-release-gate (2026-05-15)"), 2026-08-27
location: DW-####
reason: **W4 — Inventory script silently skips bullets lacking `DW-####` markers:** Pre-existing inventory-script gap from Story 12.1's W5 review note. Story 12.3's "Row inventory/hash | Passed" claim inherits the gap. Owner: deferred-work inventory-script maintainer.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit dd74efc4 migrated legacy bullets into explicit DW blocks, resolving the missing-marker inventory gap.

### DW-1453: Smart-quote / NBSP-hyphen / en-dash variants evade trigger scanner: Trigger phrases like `provider‑backed pending‑command` (NBSP hyphen U+2011) or `provider–backed` (en-dash) are visually identical to a human reviewer but use non-ASCII codepoints, so `Contains(trigger, OrdinalIgnoreCase)` does not match. Deferred: low real-world likelihood for release-note authoring; add Unicode normalisation only when an actual evasion appears. Owner: pending-status governance test maintainer. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:24-28`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-3-1-pending-status-reopen-governance-test (2026-05-16)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:24-28
reason: **CR-12-3-1-D1 — Smart-quote / NBSP-hyphen / en-dash variants evade trigger scanner:** Trigger phrases like `provider‑backed pending‑command` (NBSP hyphen U+2011) or `provider–backed` (en-dash) are visually identical to a human reviewer but use non-ASCII codepoints, so `Contains(trigger, OrdinalIgnoreCase)` does not match. Deferred: low real-world likelihood for release-note authoring; add Unicode normalisation only when an actual evasion appears. Owner: pending-status governance test maintainer. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:24-28`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 537cbd4c replaced the trigger-scanning governance implementation with the provider-backed EventStore status endpoint.

### DW-1454: `EventStoreOptions` reflection misses fields and explicit interface implementations: `GetProperties(BindingFlags.Instance | BindingFlags.Public)` does not see public fields, indexers, or interface members implemented explicitly (non-public on the type). Deferred: broader reflection design (fields/non-public/indexers); today the type uses plain auto-properties. Owner: pending-status governance test maintainer. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:65-68`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-3-1-pending-status-reopen-governance-test (2026-05-16)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:65-68
reason: **CR-12-3-1-D2 — `EventStoreOptions` reflection misses fields and explicit interface implementations:** `GetProperties(BindingFlags.Instance | BindingFlags.Public)` does not see public fields, indexers, or interface members implemented explicitly (non-public on the type). Deferred: broader reflection design (fields/non-public/indexers); today the type uses plain auto-properties. Owner: pending-status governance test maintainer. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:65-68`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 537cbd4c replaced the reflection-based provider governance implementation.

### DW-1455: Per-file and global `MaxDiagnostics` caps stack into one opaque suppression line: Each file scan caps at `MaxDiagnostics`, then `FormatDiagnostics` caps again across files, so multi-file violations collapse to a single `additional diagnostics suppressed: N` with no file paths for the suppressed entries. Deferred: per-file bounded-diagnostics intent is satisfied; the multi-file UX is a nice-to-have. Owner: pending-status governance test maintainer. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:341-371,538-546`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-3-1-pending-status-reopen-governance-test (2026-05-16)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:341-371
reason: **CR-12-3-1-D3 — Per-file and global `MaxDiagnostics` caps stack into one opaque suppression line:** Each file scan caps at `MaxDiagnostics`, then `FormatDiagnostics` caps again across files, so multi-file violations collapse to a single `additional diagnostics suppressed: N` with no file paths for the suppressed entries. Deferred: per-file bounded-diagnostics intent is satisfied; the multi-file UX is a nice-to-have. Owner: pending-status governance test maintainer. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:341-371,538-546`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 537cbd4c removed the obsolete MaxDiagnostics trigger scanner.

### DW-1456: Suppression count mixes allowed (Constraint Metadata) hits with real violations: When `violations.Count == MaxDiagnostics`, `CountReleaseNoteTriggerHits` re-scans without applying the metadata allowance, so the suppression delta includes hits that were explicitly allowed. Deferred: only misleads when a giant allowed metadata block coexists with exactly `MaxDiagnostics` real violations in the same file. Owner: pending-status governance test maintainer. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:363-368,373-374`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-3-1-pending-status-reopen-governance-test (2026-05-16)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:363-368
reason: **CR-12-3-1-D4 — Suppression count mixes allowed (Constraint Metadata) hits with real violations:** When `violations.Count == MaxDiagnostics`, `CountReleaseNoteTriggerHits` re-scans without applying the metadata allowance, so the suppression delta includes hits that were explicitly allowed. Deferred: only misleads when a giant allowed metadata block coexists with exactly `MaxDiagnostics` real violations in the same file. Owner: pending-status governance test maintainer. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:363-368,373-374`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 537cbd4c removed the obsolete release-note trigger counting path.

### DW-1457: Singleton-instance Null provider diagnostic does not name the descriptor shape as the root cause: A `Singleton(new NullPendingCommandStatusQuery())` registration emits `must use ImplementationType NullPendingCommandStatusQuery` even though the runtime type is correct; the real root cause is "descriptor uses an instance, implementation cannot be inferred from the descriptor". Deferred: diagnostic clarity nit; the underlying detection (two violations: lifetime + impl-type) is correct. Owner: pending-status governance test maintainer. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:511-521`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-3-1-pending-status-reopen-governance-test (2026-05-16)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:511-521
reason: **CR-12-3-1-D5 — Singleton-instance Null provider diagnostic does not name the descriptor shape as the root cause:** A `Singleton(new NullPendingCommandStatusQuery())` registration emits `must use ImplementationType NullPendingCommandStatusQuery` even though the runtime type is correct; the real root cause is "descriptor uses an instance, implementation cannot be inferred from the descriptor". Deferred: diagnostic clarity nit; the underlying detection (two violations: lifetime + impl-type) is correct. Owner: pending-status governance test maintainer. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs:511-521`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 537cbd4c replaced the descriptor-shape governance check with the real provider integration.

### DW-1458: Spec AC3 "12 non-blank lines" vs implementation "12 data rows" Constraint Metadata budget conflict: The story AC3 caps the Constraint Metadata allowance at 12 non-blank lines, but the approved `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md` has a 14-data-row metadata table (header + separator + 14 data = ~16 non-blank lines). The implementation deliberately relaxed the cap to "12 data rows; header/separator/prose excluded" so the existing approved release note passes; switching to the spec-literal counter would push the `Agent impact` trigger line outside the window and break the running guard. Reopen trigger: spec AC3 wording is revised to match the implementation, OR the approved release note is restructured to fit a 12-non-blank-line cap. Owner: pending-status governance test maintainer + spec author. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs` `BuildConstraintMetadataWindow` `tableMode` branch; `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md:11-28`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-3-1-pending-status-reopen-governance-test (2026-05-16)"), 2026-08-27
location: _bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md
reason: **CR-12-3-1-D6 — Spec AC3 "12 non-blank lines" vs implementation "12 data rows" Constraint Metadata budget conflict:** The story AC3 caps the Constraint Metadata allowance at 12 non-blank lines, but the approved `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md` has a 14-data-row metadata table (header + separator + 14 data = ~16 non-blank lines). The implementation deliberately relaxed the cap to "12 data rows; header/separator/prose excluded" so the existing approved release note passes; switching to the spec-literal counter would push the `Agent impact` trigger line outside the window and break the running guard. Reopen trigger: spec AC3 wording is revised to match the implementation, OR the approved release note is restructured to fit a 12-non-blank-line cap. Owner: pending-status governance test maintainer + spec author. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs` `BuildConstraintMetadataWindow` `tableMode` branch; `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md:11-28`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 537cbd4c removed the obsolete Constraint Metadata window and release-note artifact.
decision: 2026-08-27 Implement change — Implement the behavior requested by DW-1458, update affected contracts and consumers, and add focused regression evidence.

### DW-1460: `safe_run_attempt` accepts float and truncates via `int(1.9) == 1`: Asymmetric with `"1.9"` string rejection — a JSON producer that sends a float `run_attempt` is silently truncated to an integer instead of being rejected or surfacing a typed diagnostic. Deferred: very low likelihood in practice (CI emits integer `GITHUB_RUN_ATTEMPT`, well-formed JSON; env vars are strings caught by the try/except). Pick up if a producer or fixture is found that sends float `run_attempt` values. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:184-191`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run (2026-05-16)"), 2026-08-27
location: eng/release_evidence.py:184-191
reason: **CR-12-4-Def2 — `safe_run_attempt` accepts float and truncates via `int(1.9) == 1`:** Asymmetric with `"1.9"` string rejection — a JSON producer that sends a float `run_attempt` is silently truncated to an integer instead of being rejected or surfacing a typed diagnostic. Deferred: very low likelihood in practice (CI emits integer `GITHUB_RUN_ATTEMPT`, well-formed JSON; env vars are strings caught by the try/except). Pick up if a producer or fixture is found that sends float `run_attempt` values. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:184-191`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:184

### DW-1463: `nextRelease.version` provenance is not bound into the sealed manifest: `args.version` is echoed into manifest rows but the semantic-release commit SHA + computed version isn't recorded as a fingerprintable provenance field. AC30/D15 require binding "version inputs." Deferred: defensible because per-row `Version` drift is already inventory-rejected at `eng/release_evidence.py inventory`. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py` `prepare_manifest`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run (2026-05-17)"), 2026-08-27
location: eng/release_evidence.py
reason: **CR-12-4-Def5 — `nextRelease.version` provenance is not bound into the sealed manifest:** `args.version` is echoed into manifest rows but the semantic-release commit SHA + computed version isn't recorded as a fingerprintable provenance field. AC30/D15 require binding "version inputs." Deferred: defensible because per-row `Version` drift is already inventory-rejected at `eng/release_evidence.py inventory`. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py` `prepare_manifest`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:1

### DW-1464: Gate-ordering test asserts existence, not full phase-specific incident ordering: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` ~L512-518 enforces `verify-manifest < classify-release < push` via `IndexOf`, but does not assert that the symbol-push and package-push partial-publish incidents appear in the failure branch of the *immediately preceding* push, nor that post-seal mutation positioning is correct. Deferred: contract is partially proven; tighten when cross-story 11.7 ordering surfaces a real regression. Owner: release governance test maintainer. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` `ReleaseRcOrdersChecksBeforePublish`-style assertions.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run (2026-05-17)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs
reason: **CR-12-4-Def6 — Gate-ordering test asserts existence, not full phase-specific incident ordering:** `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` ~L512-518 enforces `verify-manifest < classify-release < push` via `IndexOf`, but does not assert that the symbol-push and package-push partial-publish incidents appear in the failure branch of the *immediately preceding* push, nor that post-seal mutation positioning is correct. Deferred: contract is partially proven; tighten when cross-story 11.7 ordering surfaces a real regression. Owner: release governance test maintainer. Evidence: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` `ReleaseRcOrdersChecksBeforePublish`-style assertions.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:1

### DW-1465: Fixture digest `48ada263...` for `approved-fallback` positive case is precomputed by hand: When `release-manifest-valid.json` fingerprints regenerate, the operator must manually recompute the hash. No regen script exists. Deferred: low real-world likelihood until the next manifest change requires it; pick up alongside Def9 (operator `fingerprint-digest` helper). Owner: release-evidence helper maintainer. Evidence: `tests/ci-governance/fixtures/release-readiness-cases.json` `approved-fallback` case.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run round 4 (2026-05-17)"), 2026-08-27
location: release-manifest-valid.json
reason: **CR-12-4-Def7 — Fixture digest `48ada263...` for `approved-fallback` positive case is precomputed by hand:** When `release-manifest-valid.json` fingerprints regenerate, the operator must manually recompute the hash. No regen script exists. Deferred: low real-world likelihood until the next manifest change requires it; pick up alongside Def9 (operator `fingerprint-digest` helper). Owner: release-evidence helper maintainer. Evidence: `tests/ci-governance/fixtures/release-readiness-cases.json` `approved-fallback` case.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: tests/ci-governance/fixtures/release-manifest-valid.json:1

### DW-1467: Operator-facing `fingerprint-digest` helper subcommand absent: Operators computing fallback `approved_against_fingerprints_sha256` by hand can produce serialization mismatches versus `canonical_sha256` (separator order, key sort order). Deferred: add a CLI subcommand emitting the canonical hex when the first fallback approval is needed in production. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:117-119` `canonical_sha256`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run round 4 (2026-05-17)"), 2026-08-27
location: eng/release_evidence.py:117-119
reason: **CR-12-4-Def9 — Operator-facing `fingerprint-digest` helper subcommand absent:** Operators computing fallback `approved_against_fingerprints_sha256` by hand can produce serialization mismatches versus `canonical_sha256` (separator order, key sort order). Deferred: add a CLI subcommand emitting the canonical hex when the first fallback approval is needed in production. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:117-119` `canonical_sha256`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:117

### DW-1471: `parse_strict_bool` does not reject `bool` instances for non-boolean callers: Related to `safe_run_attempt` (P76) — Python's bool-is-int conflation means a JSON `true`/`false` could pass through fields expecting integer text. Deferred: latent footgun in current callsites but no observed defect; pick up alongside P76. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:177-180` `parse_strict_bool`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run round 4 (2026-05-17)"), 2026-08-27
location: eng/release_evidence.py:177-180
reason: **CR-12-4-Def13 — `parse_strict_bool` does not reject `bool` instances for non-boolean callers:** Related to `safe_run_attempt` (P76) — Python's bool-is-int conflation means a JSON `true`/`false` could pass through fields expecting integer text. Deferred: latent footgun in current callsites but no observed defect; pick up alongside P76. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:177-180` `parse_strict_bool`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:177

### DW-1472: attestation bundle integration still incomplete: The release workflow now has a blocking `actions/attest-build-provenance@v4` step before `npx semantic-release`, but AC9's full "attestations generated then verified" path remains structurally incomplete because the round-3 `gh attestation verify` step inside `.releaserc.json` `prepareCmd` runs at the same scope as `dotnet nuget sign` and cannot have a workflow step interleaved. Closing the remaining gap requires moving `dotnet nuget sign` (and `dotnet build`/`dotnet pack`/`dotnet CycloneDX`) OUT of `prepareCmd` into workflow-level steps that run BEFORE `npx semantic-release`, plus an earlier `npx semantic-release --dry-run` step that captures `RELEASE_NEXT_VERSION` via stdout-grep so the build/pack/sign steps know which version to produce. Deferred from round-4 patching: structural workflow rewrite that exceeds patch-session scope; production releases stay on `approved-unsupported` fallback (current behavior) until this lands. Owner: release-evidence/workflow maintainer. Evidence: `.releaserc.json:12` prepareCmd, `.github/workflows/release.yml` `Attest release evidence provenance` and `Run semantic-release` steps. Reconciliation: Superseded 2026-07-18 by REL-3 — the workflow-interleaving design this entry prescribed is replaced by the AC18 model: attestation over the exact signed candidates belongs to the upstream BUILD-REL-1 governed contract (candidate phase in `domain-release.yml`), `eng/release_prepublish.py prepare` binds `RELEASE_ATTESTATION_STATUS`/`RELEASE_ATTESTATION_BUNDLE` into the sealed manifest, and `classify-release --require-publishable` fails closed unless the status is `attested` or a sealed owner-approved fallback. Residual dependency tracked in `g2-hexalith-builds-inline-pre-publish-gate-request.md` and REL-5; reopen trigger: the upstream contract lands with a different attestation handoff shape.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run round 4 (2026-05-17)"), 2026-08-27
location: .releaserc.json
reason: **CR-12-4-Def14 — attestation bundle integration still incomplete:** The release workflow now has a blocking `actions/attest-build-provenance@v4` step before `npx semantic-release`, but AC9's full "attestations generated then verified" path remains structurally incomplete because the round-3 `gh attestation verify` step inside `.releaserc.json` `prepareCmd` runs at the same scope as `dotnet nuget sign` and cannot have a workflow step interleaved. Closing the remaining gap requires moving `dotnet nuget sign` (and `dotnet build`/`dotnet pack`/`dotnet CycloneDX`) OUT of `prepareCmd` into workflow-level steps that run BEFORE `npx semantic-release`, plus an earlier `npx semantic-release --dry-run` step that captures `RELEASE_NEXT_VERSION` via stdout-grep so the build/pack/sign steps know which version to produce. Deferred from round-4 patching: structural workflow rewrite that exceeds patch-session scope; production releases stay on `approved-unsupported` fallback (current behavior) until this lands. Owner: release-evidence/workflow maintainer. Evidence: `.releaserc.json:12` prepareCmd, `.github/workflows/release.yml` `Attest release evidence provenance` and `Run semantic-release` steps. Reconciliation: Superseded 2026-07-18 by REL-3 — the workflow-interleaving design this entry prescribed is replaced by the AC18 model: attestation over the exact signed candidates belongs to the upstream BUILD-REL-1 governed contract (candidate phase in `domain-release.yml`), `eng/release_prepublish.py prepare` binds `RELEASE_ATTESTATION_STATUS`/`RELEASE_ATTESTATION_BUNDLE` into the sealed manifest, and `classify-release --require-publishable` fails closed unless the status is `attested` or a sealed owner-approved fallback. Residual dependency tracked in `g2-hexalith-builds-inline-pre-publish-gate-request.md` and REL-5; reopen trigger: the upstream contract lands with a different attestation handoff shape.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: .github/workflows/release.yml:307-321 delegates release to the immutable Builds workflow; references/Hexalith.Builds/.github/workflows/domain-release.yml:988-1004 mints and verifies the candidate attestation before publication.
decision: 2026-08-27 Implement the change — Implement the behavior requested by DW-1472, update affected contracts and consumers, and add focused regression evidence.
decision: 2026-08-27 Implement the change — Implement the behavior requested by DW-1472, update affected contracts and consumers, and add focused regression evidence.

### DW-1473: `release-budget` step records metrics on every workflow event without an `if:` guard: The step at `.github/workflows/release.yml:241-243` runs on push, dispatch, and any future trigger; minor API/quota cost on non-publish triggers but no correctness issue. Deferred: cosmetic optimization; pick up when workflow trigger surface expands beyond push/dispatch. Owner: release-evidence/workflow maintainer. Evidence: `.github/workflows/release.yml:241-243`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run round 5 (2026-05-18)"), 2026-08-27
location: .github/workflows/release.yml:241-243
reason: **CR-12-4-Def15 — `release-budget` step records metrics on every workflow event without an `if:` guard:** The step at `.github/workflows/release.yml:241-243` runs on push, dispatch, and any future trigger; minor API/quota cost on non-publish triggers but no correctness issue. Deferred: cosmetic optimization; pick up when workflow trigger surface expands beyond push/dispatch. Owner: release-evidence/workflow maintainer. Evidence: `.github/workflows/release.yml:241-243`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:5681

### DW-1481: Empty/truncated existing `partial-publish-incident.json` evades P155 overwrite protection: P155 prevents overwriting a real incident with the placeholder, but a 0-byte or corrupted file passes the `failed_phase != "none"` check trivially. Threat model: operator/attacker truncating the file mid-investigation; low likelihood. Pick up when forensic-integrity hardening is prioritized. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:1917-1930`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run round 8 verification (2026-05-19)"), 2026-08-27
location: partial-publish-incident.json
reason: **CR-12-4-Def38 — Empty/truncated existing `partial-publish-incident.json` evades P155 overwrite protection:** P155 prevents overwriting a real incident with the placeholder, but a 0-byte or corrupted file passes the `failed_phase != "none"` check trivially. Threat model: operator/attacker truncating the file mid-investigation; low likelihood. Pick up when forensic-integrity hardening is prioritized. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:1917-1930`.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:3543-3555 refuses to overwrite an unreadable existing partial-publish incident.

### DW-1486: `fallback-digest` subcommand without `release-package-inventory.json` produces misleading digest: P163 added the subcommand; missing inventory now silently yields a digest derived from `{"package_set": "missing"}`. Operators paste this into `vars.RELEASE_ATTESTATION_FALLBACK_FINGERPRINTS_SHA256` and silently mismatch later. Pick up when first production fallback approval is needed. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:2007-2008`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run round 8 verification (2026-05-19)"), 2026-08-27
location: release-package-inventory.json
reason: **CR-12-4-Def43 — `fallback-digest` subcommand without `release-package-inventory.json` produces misleading digest:** P163 added the subcommand; missing inventory now silently yields a digest derived from `{"package_set": "missing"}`. Operators paste this into `vars.RELEASE_ATTESTATION_FALLBACK_FINGERPRINTS_SHA256` and silently mismatch later. Pick up when first production fallback approval is needed. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:2007-2008`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release-package-inventory.json:1

### DW-1489: Checksum glob crashes with `ValueError` on symlink chains crossing the root: `p.relative_to(root)` raises when symlink target escapes root. Defense-in-depth; pick up alongside Def40. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:1546-1547`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run round 8 verification (2026-05-19)"), 2026-08-27
location: eng/release_evidence.py:1546-1547
reason: **CR-12-4-Def46 — Checksum glob crashes with `ValueError` on symlink chains crossing the root:** `p.relative_to(root)` raises when symlink target escapes root. Defense-in-depth; pick up alongside Def40. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:1546-1547`.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:3018-3021 keeps glob results lexical beneath root and calls relative_to(root) without resolving escaping symlink targets, eliminating the recorded ValueError path.

### DW-1491: Symbol-push success after package-push success leaves forensic-state gap: `partial-publish-incident phase=symbol-push` does not flag the prior-success state; operators must read `prior-release.json` to know packages already shipped. Real-world impact: monitoring/incident triage UX, not publish-authorization. Owner: release-evidence helper maintainer. Evidence: `.releaserc.json:12` symbol-push branch.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run round 8 verification (2026-05-19)"), 2026-08-27
location: prior-release.json
reason: **CR-12-4-Def48 — Symbol-push success after package-push success leaves forensic-state gap:** `partial-publish-incident phase=symbol-push` does not flag the prior-success state; operators must read `prior-release.json` to know packages already shipped. Real-world impact: monitoring/incident triage UX, not publish-authorization. Owner: release-evidence helper maintainer. Evidence: `.releaserc.json:12` symbol-push branch.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: eng/release_prepublish.py:855-874 records the failed phase and exact successful-push count; eng/release_evidence.py:3612-3621 emits typed incident state.

### DW-1492: `gh api --paginate` lacks per-request timeout: Rate-limited probe could stall for minutes. Release-budget already tracks total minutes; pick up if a real GitHub-API stall extends past the workflow timeout. Owner: release-evidence/workflow maintainer. Evidence: `.github/workflows/release.yml:226-231`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run round 8 verification (2026-05-19)"), 2026-08-27
location: .github/workflows/release.yml:226-231
reason: **CR-12-4-Def49 — `gh api --paginate` lacks per-request timeout:** Rate-limited probe could stall for minutes. Release-budget already tracks total minutes; pick up if a real GitHub-API stall extends past the workflow timeout. Owner: release-evidence/workflow maintainer. Evidence: `.github/workflows/release.yml:226-231`.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: .github/workflows/release.yml:21-23 bounds verify-source to 10 minutes and :64-70 uses finite per_page=100 API requests; the former unbounded gh api --paginate probe no longer exists.

### DW-1499: AC10 `Record attestation fallback evidence` step runs in dry-run mode: Always writes `attestation-unavailable.md` for both dry-run and live; cosmetic only because dry-run cannot publish. Pick up if dry-run is exercised against an attested-supported repo. Owner: release-evidence/workflow maintainer. Evidence: `.github/workflows/release.yml:411-417`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run round 8 verification (2026-05-19)"), 2026-08-27
location: attestation-unavailable.md
reason: **CR-12-4-Def56 — AC10 `Record attestation fallback evidence` step runs in dry-run mode:** Always writes `attestation-unavailable.md` for both dry-run and live; cosmetic only because dry-run cannot publish. Pick up if dry-run is exercised against an attested-supported repo. Owner: release-evidence/workflow maintainer. Evidence: `.github/workflows/release.yml:411-417`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit ef2823ba removed the old attestation-unavailable dry-run step; .github/workflows/release.yml:406 now uses the AD-15 verification handoff.

### DW-1501: `next_owner_action` for `local-candidate` returns generic message: Companion to Def16 (developer-local case). Dry-run dispatch is the primary supported usage and gets misleading guidance ("resolve blocking release gates" instead of "dry-run dispatches are never publish-authorized; toggle dry_run=false and approve"). Pick up alongside dry-run UX hardening. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:1382`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run round 8 verification (2026-05-19)"), 2026-08-27
location: eng/release_evidence.py:1382
reason: **CR-12-4-Def58 — `next_owner_action` for `local-candidate` returns generic message:** Companion to Def16 (developer-local case). Dry-run dispatch is the primary supported usage and gets misleading guidance ("resolve blocking release gates" instead of "dry-run dispatches are never publish-authorized; toggle dry_run=false and approve"). Pick up alongside dry-run UX hardening. Owner: release-evidence helper maintainer. Evidence: `eng/release_evidence.py:1382`.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:2858-2861 now emits dry-run-specific next_owner_action guidance; introduced by commit 471c7fcc.

### DW-1502: Hexalith.EventStore submodule pointer change in commit 3f707c2 falls outside Story 12.5 documentation scope: Acceptance Auditor A-01 noted the commit modifies a submodule pointer not named in the story's File List or Implementation Plan (`docs/...` only). The change is already on `main` and subsequent commits (e.g., aa9ad94 `feat: Update submodule references and enhance release evidence handling`) also touched submodules. Defer: pre-existing scope-bundling pattern; pick up as a process improvement for split-commits/scope-aware staging in future story implementations. Owner: story-creation/dev-agent maintainer. Evidence: commit `3f707c2`, story File List at `_bmad-output/implementation-artifacts/12-5-accessibility-and-stakeholder-acceptance-evidence-pack.md:348-354`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-5-accessibility-and-stakeholder-acceptance-evidence-pack (2026-05-19)"), 2026-08-27
location: docs/
reason: **CR-12-5-Def01 — Hexalith.EventStore submodule pointer change in commit 3f707c2 falls outside Story 12.5 documentation scope:** Acceptance Auditor A-01 noted the commit modifies a submodule pointer not named in the story's File List or Implementation Plan (`docs/...` only). The change is already on `main` and subsequent commits (e.g., aa9ad94 `feat: Update submodule references and enhance release evidence handling`) also touched submodules. Defer: pre-existing scope-bundling pattern; pick up as a process improvement for split-commits/scope-aware staging in future story implementations. Owner: story-creation/dev-agent maintainer. Evidence: commit `3f707c2`, story File List at `_bmad-output/implementation-artifacts/12-5-accessibility-and-stakeholder-acceptance-evidence-pack.md:348-354`.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: Commit fd04bdd9 added strict story-ID and commit-scope validation; eng/validate-story-artifacts.py:1733-1757 now fails interleaved and unmapped commits.

### DW-1509: Cross-pack precedence rule when multiple `release-candidate-*` packs exist: Edge Case Hunter EC-10, EC-67 — README at lines 69-73 defines classifications but no rule designates a canonical pack when multiple dated packs coexist; the `evidence_pack_version` has no `supersedes:` chain. Pick up when the second pack is created. Owner: accessibility-evidence-pack maintainer. Evidence: `docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:14`, `docs/accessibility-verification/README.md:69-73`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-5-accessibility-and-stakeholder-acceptance-evidence-pack (2026-05-19)"), 2026-08-27
location: docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:14
reason: **CR-12-5-Def08 — Cross-pack precedence rule when multiple `release-candidate-*` packs exist:** Edge Case Hunter EC-10, EC-67 — README at lines 69-73 defines classifications but no rule designates a canonical pack when multiple dated packs coexist; the `evidence_pack_version` has no `supersedes:` chain. Pick up when the second pack is created. Owner: accessibility-evidence-pack maintainer. Evidence: `docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:14`, `docs/accessibility-verification/README.md:69-73`.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: Commit 4a038668; docs/accessibility-verification/README.md now defines canonical cross-pack precedence and contradiction handling.

### DW-1517: Internal markdown in Evidence Manifest may be misread as external evidence: Edge Case Hunter EC-57 — the manifest lists three "Repository markdown" rows; automation parsing the manifest may treat them as external artifacts requiring retention/redaction infrastructure. Pick up when an external artifact is first added or when manifest schema gains an `is_external` flag. Owner: accessibility-evidence-pack maintainer. Evidence: `docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:137-143`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-5-accessibility-and-stakeholder-acceptance-evidence-pack (2026-05-19)"), 2026-08-27
location: docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:137-143
reason: **CR-12-5-Def16 — Internal markdown in Evidence Manifest may be misread as external evidence:** Edge Case Hunter EC-57 — the manifest lists three "Repository markdown" rows; automation parsing the manifest may treat them as external artifacts requiring retention/redaction infrastructure. Pick up when an external artifact is first added or when manifest schema gains an `is_external` flag. Owner: accessibility-evidence-pack maintainer. Evidence: `docs/accessibility-verification/release-candidate-2026-05-15-evidence-pack.md:137-143`.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: Commit 4a038668; the release-candidate accessibility evidence pack now labels all repository-markdown artifacts as internal.

### DW-1522: `find ... | sed 's#^#- #'` in summary step would mishandle filenames containing `#`: Blind Hunter BH-019 — operationally extremely rare. Pick up alongside step-summary hardening. Owner: workflow maintainer. Evidence: `.github/workflows/release.yml:616-621`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: .github/workflows/release.yml:616-621
reason: **CR-12-4-Def63 — `find ... | sed 's#^#- #'` in summary step would mishandle filenames containing `#`:** Blind Hunter BH-019 — operationally extremely rare. Pick up alongside step-summary hardening. Owner: workflow maintainer. Evidence: `.github/workflows/release.yml:616-621`.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: Commit ef2823ba removed the find-to-sed release summary path; current .github/workflows/release.yml contains no such pipeline.

### DW-1523: `RELEASE_DRY_RUN` shell-level vocabulary (`false|0|no`) wider than Python `parse_strict_bool` ({true,false} only): Blind Hunter BH-023/BH-041 — maintenance hazard if the vocabulary in three workflow steps drifts. Pick up alongside boolean-domain consolidation. Owner: workflow/helper maintainer. Evidence: `.github/workflows/release.yml:445-448, 469-472, 484-487` + `.releaserc.json:12`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: .github/workflows/release.yml:445-448
reason: **CR-12-4-Def64 — `RELEASE_DRY_RUN` shell-level vocabulary (`false|0|no`) wider than Python `parse_strict_bool` ({true,false} only):** Blind Hunter BH-023/BH-041 — maintenance hazard if the vocabulary in three workflow steps drifts. Pick up alongside boolean-domain consolidation. Owner: workflow/helper maintainer. Evidence: `.github/workflows/release.yml:445-448, 469-472, 484-487` + `.releaserc.json:12`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:6031

### DW-1525: `evidence_section` only top-level type check; nested members (e.g., `attestation.fallback`) not type-checked: Blind Hunter BH-033 + Edge Case Hunter EC-21/EC-37 — defense-in-depth. Pick up alongside structured-evidence validation pass. Owner: release-evidence maintainer. Evidence: `eng/release_evidence.py:1601-1606`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: eng/release_evidence.py:1601-1606
reason: **CR-12-4-Def66 — `evidence_section` only top-level type check; nested members (e.g., `attestation.fallback`) not type-checked:** Blind Hunter BH-033 + Edge Case Hunter EC-21/EC-37 — defense-in-depth. Pick up alongside structured-evidence validation pass. Owner: release-evidence maintainer. Evidence: `eng/release_evidence.py:1601-1606`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:1601

### DW-1529: `RELEASE_ATTESTATION_FALLBACK_*` env vars `${{ vars.X || '' }}` swallow typos: Blind Hunter BH-043 — diagnostic clarity regression. Pick up alongside CI input validation. Owner: workflow maintainer. Evidence: `.github/workflows/release.yml:19-22`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: .github/workflows/release.yml:19-22
reason: **CR-12-4-Def70 — `RELEASE_ATTESTATION_FALLBACK_*` env vars `${{ vars.X || '' }}` swallow typos:** Blind Hunter BH-043 — diagnostic clarity regression. Pick up alongside CI input validation. Owner: workflow maintainer. Evidence: `.github/workflows/release.yml:19-22`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:6073

### DW-1531: `helper_version.content_sha256` field has no SHA256 hex-format validation in `manifest_diagnostics`: Edge Case Hunter EC-5 — null/empty content_sha256 emits one drift message but no `malformed` typed reason. Pick up alongside fingerprint-shape validation. Owner: release-evidence maintainer. Evidence: `eng/release_evidence.py:815-823`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: eng/release_evidence.py:815-823
reason: **CR-12-4-Def72 — `helper_version.content_sha256` field has no SHA256 hex-format validation in `manifest_diagnostics`:** Edge Case Hunter EC-5 — null/empty content_sha256 emits one drift message but no `malformed` typed reason. Pick up alongside fingerprint-shape validation. Owner: release-evidence maintainer. Evidence: `eng/release_evidence.py:815-823`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:815

### DW-1532: `__version__ = "1.0.0"` constant has no semver-format assertion at import time: Edge Case Hunter EC-6 — operator setting `__version__='wip'` loses operational signal but doesn't break flow. Pick up alongside CI helper-import smoke test. Owner: release-evidence maintainer. Evidence: `eng/release_evidence.py:27`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: eng/release_evidence.py:27
reason: **CR-12-4-Def73 — `__version__ = "1.0.0"` constant has no semver-format assertion at import time:** Edge Case Hunter EC-6 — operator setting `__version__='wip'` loses operational signal but doesn't break flow. Pick up alongside CI helper-import smoke test. Owner: release-evidence maintainer. Evidence: `eng/release_evidence.py:27`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:27

### DW-1535: `CURRENT_BRANCH` fallback to `github.event.repository.default_branch` could be empty for some event payloads: Edge Case Hunter EC-18 — `workflow_dispatch` always carries repository. Pick up if trigger surface expands. Owner: workflow maintainer. Evidence: `.github/workflows/release.yml:194`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: .github/workflows/release.yml:194
reason: **CR-12-4-Def76 — `CURRENT_BRANCH` fallback to `github.event.repository.default_branch` could be empty for some event payloads:** Edge Case Hunter EC-18 — `workflow_dispatch` always carries repository. Pick up if trigger surface expands. Owner: workflow maintainer. Evidence: `.github/workflows/release.yml:194`.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: Commit ef2823ba removed CURRENT_BRANCH/default_branch fallback; .github/workflows/release.yml:7-8 now has only workflow_dispatch.

### DW-1537: `safe_run_attempt` loses sub-integer provenance on float-string input `'1.0'`: Edge Case Hunter EC-22 — niche. Pick up alongside typed-CLI hardening. Owner: release-evidence maintainer. Evidence: `eng/release_evidence.py:2058-2126`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: eng/release_evidence.py:2058-2126
reason: **CR-12-4-Def78 — `safe_run_attempt` loses sub-integer provenance on float-string input `'1.0'`:** Edge Case Hunter EC-22 — niche. Pick up alongside typed-CLI hardening. Owner: release-evidence maintainer. Evidence: `eng/release_evidence.py:2058-2126`.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:2085-2089 rejects every floating-point run_attempt, including 1.0.

### DW-1538: publishCmd `jq --arg run_id` `tonumber` parse-error fail-closed but diagnostic is opaque: Edge Case Hunter EC-23 — pick up alongside CI diagnostic prose review. Owner: workflow maintainer. Evidence: `.releaserc.json:642`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: .releaserc.json:642
reason: **CR-12-4-Def79 — publishCmd `jq --arg run_id` `tonumber` parse-error fail-closed but diagnostic is opaque:** Edge Case Hunter EC-23 — pick up alongside CI diagnostic prose review. Owner: workflow maintainer. Evidence: `.releaserc.json:642`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:6136

### DW-1539: publishCmd `trap 'rm -f ...' EXIT` install timing — early `set -e` exit before trap leaves stderr files: Edge Case Hunter EC-24 — pick up alongside checksums-glob audit. Owner: workflow maintainer. Evidence: `.releaserc.json:642`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: .releaserc.json:642
reason: **CR-12-4-Def80 — publishCmd `trap 'rm -f ...' EXIT` install timing — early `set -e` exit before trap leaves stderr files:** Edge Case Hunter EC-24 — pick up alongside checksums-glob audit. Owner: workflow maintainer. Evidence: `.releaserc.json:642`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:6143

### DW-1540: Shell variable expansion 64KB limit on `prior_release_response` capture: Edge Case Hunter EC-25 — real-world response sizes well under limit. Pick up if response-size limit raised. Owner: workflow maintainer. Evidence: `.releaserc.json:642`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: .releaserc.json:642
reason: **CR-12-4-Def81 — Shell variable expansion 64KB limit on `prior_release_response` capture:** Edge Case Hunter EC-25 — real-world response sizes well under limit. Pick up if response-size limit raised. Owner: workflow maintainer. Evidence: `.releaserc.json:642`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:6150

### DW-1541: `release_budget` writes evidence row without sealed manifest when called pre-manifest-creation: Edge Case Hunter EC-28 — pick up alongside budget-history corruption audit. Owner: release-evidence maintainer. Evidence: `.github/workflows/release.yml:580-584`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: .github/workflows/release.yml:580-584
reason: **CR-12-4-Def82 — `release_budget` writes evidence row without sealed manifest when called pre-manifest-creation:** Edge Case Hunter EC-28 — pick up alongside budget-history corruption audit. Owner: release-evidence maintainer. Evidence: `.github/workflows/release.yml:580-584`.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: commit ef2823ba removed the release-budget workflow step; .github/workflows/release.yml:292-293 now uses release_prepublish prepare and bundle.

### DW-1544: Heredoc `${#random_hex}` length check is bash-specific; no explicit `shell: bash` directive: Edge Case Hunter EC-41 — ubuntu-latest is bash by default. Pick up if runner image changes. Owner: workflow maintainer. Evidence: `.github/workflows/release.yml:163-171`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 9)"), 2026-08-27
location: .github/workflows/release.yml:163-171
reason: **CR-12-4-Def85 — Heredoc `${#random_hex}` length check is bash-specific; no explicit `shell: bash` directive:** Edge Case Hunter EC-41 — ubuntu-latest is bash by default. Pick up if runner image changes. Owner: workflow maintainer. Evidence: `.github/workflows/release.yml:163-171`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:6178

### DW-1547: jq `display_title` word-boundary regex substitutes `${nextRelease.version}` without escaping dots: Blind Hunter BH-F8 + BH-F22 — resulting regex matches `v1X2Y3` literal substrings as well as `v1.2.3`. SemVer cannot produce `1X2Y3` versions, so real-world collision is impossible; defense-in-depth concern. Pick up alongside P210 follow-up if version schema expands to allow non-numeric segments. Owner: workflow maintainer. Evidence: `.releaserc.json:12` same_version_runs filter.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 10)"), 2026-08-27
location: .releaserc.json:12
reason: **CR-12-4-Def88 — jq `display_title` word-boundary regex substitutes `${nextRelease.version}` without escaping dots:** Blind Hunter BH-F8 + BH-F22 — resulting regex matches `v1X2Y3` literal substrings as well as `v1.2.3`. SemVer cannot produce `1X2Y3` versions, so real-world collision is impossible; defense-in-depth concern. Pick up alongside P210 follow-up if version schema expands to allow non-numeric segments. Owner: workflow maintainer. Evidence: `.releaserc.json:12` same_version_runs filter.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:6199

### DW-1548: Dry-run vs live publish step env-block duplication: Blind Hunter BH-F18 — a future env-var added to the live step but not the dry-run step silently desyncs classifier inputs. Pick up alongside workflow refactor to lift shared env into job-level `env:` block. Owner: workflow maintainer. Evidence: `.github/workflows/release.yml:208-211, 226-233`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 10)"), 2026-08-27
location: .github/workflows/release.yml:208-211
reason: **CR-12-4-Def89 — Dry-run vs live publish step env-block duplication:** Blind Hunter BH-F18 — a future env-var added to the live step but not the dry-run step silently desyncs classifier inputs. Pick up alongside workflow refactor to lift shared env into job-level `env:` block. Owner: workflow maintainer. Evidence: `.github/workflows/release.yml:208-211, 226-233`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:6206

### DW-1553: `release_budget` step records `tag="<github.ref_name>"` (e.g., `"main"`) when no sealed-manifest produced before the step runs: Edge Case Hunter EC-14 — P192 fixed the success-path; failed-run records keep the misleading branch-name-as-tag value. Pick up alongside P239 / budget-skipped-marker. Owner: release-evidence maintainer. Evidence: `.github/workflows/release.yml:609-630` + `eng/release_evidence.py:2007-2008`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 10)"), 2026-08-27
location: .github/workflows/release.yml:609-630
reason: **CR-12-4-Def94 — `release_budget` step records `tag="<github.ref_name>"` (e.g., `"main"`) when no sealed-manifest produced before the step runs:** Edge Case Hunter EC-14 — P192 fixed the success-path; failed-run records keep the misleading branch-name-as-tag value. Pick up alongside P239 / budget-skipped-marker. Owner: release-evidence maintainer. Evidence: `.github/workflows/release.yml:609-630` + `eng/release_evidence.py:2007-2008`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:2007

### DW-1554: `approved_at` newly required in `fallback_complete`; pre-existing fallback approvals without `approved_at` are silently invalidated with no migration path: Blind Hunter BH-F16 — intentional fail-closed per P191; operators with active fallback approvals will see surprise blocking on first run after upgrade. Workflow env defaults to empty string. Pick up alongside CI input migration documentation; emit a more specific blocking diagnostic naming `vars.RELEASE_ATTESTATION_FALLBACK_APPROVED_AT`. Owner: release-evidence + workflow maintainers. Evidence: `eng/release_evidence.py:1500` + `.github/workflows/release.yml:18-19`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-19, round 10)"), 2026-08-27
location: eng/release_evidence.py:1500
reason: **CR-12-4-Def95 — `approved_at` newly required in `fallback_complete`; pre-existing fallback approvals without `approved_at` are silently invalidated with no migration path:** Blind Hunter BH-F16 — intentional fail-closed per P191; operators with active fallback approvals will see surprise blocking on first run after upgrade. Workflow env defaults to empty string. Pick up alongside CI input migration documentation; emit a more specific blocking diagnostic naming `vars.RELEASE_ATTESTATION_FALLBACK_APPROVED_AT`. Owner: release-evidence + workflow maintainers. Evidence: `eng/release_evidence.py:1500` + `.github/workflows/release.yml:18-19`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:1500

### DW-1558: P240 rate-limit branch and transient-error branch both emit operator messages on rate-limit hit: Edge Case Hunter EH-R10-6 — when rate-limit is detected, the subsequent `[ $same_version_rc -ne 0 ]` branch also fires with empty stderr → confusing forensic message. Fail-closed direction is correct; stylistic. Pick up alongside P254 detector rewrite. Owner: workflow maintainer. Evidence: `.releaserc.json:12`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-20, round 11)"), 2026-08-27
location: .releaserc.json:12
reason: **CR-12-4-Def99 — P240 rate-limit branch and transient-error branch both emit operator messages on rate-limit hit:** Edge Case Hunter EH-R10-6 — when rate-limit is detected, the subsequent `[ $same_version_rc -ne 0 ]` branch also fires with empty stderr → confusing forensic message. Fail-closed direction is correct; stylistic. Pick up alongside P254 detector rewrite. Owner: workflow maintainer. Evidence: `.releaserc.json:12`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:6276

### DW-1559: Three release-evidence JSON files with overlapping names (`partial-publish-placeholder.json`, `partial-publish-incident.json`, `prior-release.json`): Round-12 inline review. The placeholder is written pre-publish with `phase=none classification=none`; the incident file is rewritten on push failure; `prior-release.json` records either no-prior-release or actual prior-release API response. A consumer parsing the artifact bundle must read all three to determine "did a real partial publish happen". Pick up alongside any future "single typed release-state artifact" consolidation. Owner: release-evidence maintainer. Evidence: `.releaserc.json:11` prepareCmd + `.github/workflows/release.yml` publishCmd.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-20, round 12, Group A only)"), 2026-08-27
location: partial-publish-placeholder.json
reason: **CR-12-4-Def100 — Three release-evidence JSON files with overlapping names (`partial-publish-placeholder.json`, `partial-publish-incident.json`, `prior-release.json`):** Round-12 inline review. The placeholder is written pre-publish with `phase=none classification=none`; the incident file is rewritten on push failure; `prior-release.json` records either no-prior-release or actual prior-release API response. A consumer parsing the artifact bundle must read all three to determine "did a real partial publish happen". Pick up alongside any future "single typed release-state artifact" consolidation. Owner: release-evidence maintainer. Evidence: `.releaserc.json:11` prepareCmd + `.github/workflows/release.yml` publishCmd.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: eng/release_prepublish.py:426-434 uses one canonical partial-publish-incident.json, while eng/release_evidence.py:3598-3615 gives placeholder and real incidents distinct typed contracts.
decision: 2026-08-28 Implement change — Implement the requested change at partial-publish-placeholder.json, update affected contracts and consumers, and add focused regression evidence.
decision: 2026-08-28 Implement change — Implement the requested change at partial-publish-placeholder.json, update affected contracts and consumers, and add focused regression evidence.

### DW-1560: Inline Python heredocs in `release.yml` (Seed step, concurrency-guard, release-budget-skipped marker) duplicate JSON-emission logic that lives in `eng/release_evidence.py`: Round-12 inline review. The inline scripts ARE fingerprinted indirectly via `release.yml` in `RELEASE_DEFINITION_FILES`, so drift is caught. But their typed decision contracts (`frontcomposer.release-run-metadata.v1`, `frontcomposer.release-budget-skipped.v1`) are not enumerated alongside the helper's contracts; a future audit tool listing all release decision contracts would need to scan both files. Pick up alongside a release-evidence subcommand consolidation (e.g., move emitters into `eng/release_evidence.py` as `seed-run-metadata` / `release-budget-skipped` subcommands). Owner: release-evidence maintainer. Evidence: `.github/workflows/release.yml:121-157` (Seed), `:236-433` (concurrency-guard), `:651-672` (budget-skipped emitter).

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-20, round 12, Group A only)"), 2026-08-27
location: release.yml
reason: **CR-12-4-Def101 — Inline Python heredocs in `release.yml` (Seed step, concurrency-guard, release-budget-skipped marker) duplicate JSON-emission logic that lives in `eng/release_evidence.py`:** Round-12 inline review. The inline scripts ARE fingerprinted indirectly via `release.yml` in `RELEASE_DEFINITION_FILES`, so drift is caught. But their typed decision contracts (`frontcomposer.release-run-metadata.v1`, `frontcomposer.release-budget-skipped.v1`) are not enumerated alongside the helper's contracts; a future audit tool listing all release decision contracts would need to scan both files. Pick up alongside a release-evidence subcommand consolidation (e.g., move emitters into `eng/release_evidence.py` as `seed-run-metadata` / `release-budget-skipped` subcommands). Owner: release-evidence maintainer. Evidence: `.github/workflows/release.yml:121-157` (Seed), `:236-433` (concurrency-guard), `:651-672` (budget-skipped emitter).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:1

### DW-1562: CR-12-4-D27 → follow-up story: extract `.releaserc.json` `prepareCmd` + `publishCmd` to dedicated bash scripts:

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-20, round 12, Group A only)"), 2026-08-27
location: .releaserc.json
reason: **CR-12-4-D27 → follow-up story: extract `.releaserc.json` `prepareCmd` + `publishCmd` to dedicated bash scripts:** Round-12 inline review. Best-practice implementation plan: (1) create `eng/release/prepareCmd.sh` and `eng/release/publishCmd.sh` preserving the current unsigned-release env-var contract (`RELEASE_DRY_RUN`, `RELEASE_CONCURRENT_SAME_VERSION`, `RELEASE_OWNER_APPROVED`, `RELEASE_APPROVER`, `RELEASE_ATTESTATION_STATUS`, `RELEASE_ATTESTATION_FALLBACK_*`, `RELEASE_FROM_FORK`, `RELEASE_APPROVAL_MECHANISM`, `GITHUB_REPOSITORY`, `GITHUB_RUN_ID`, `GITHUB_RUN_ATTEMPT`, `GITHUB_REF`, `GITHUB_REF_PROTECTED`, `GITHUB_EVENT_NAME`, semantic-release `${nextRelease.version}` template interpolation handled by passing version as `$1`), (2) reference from `.releaserc.json` via `bash eng/release/prepareCmd.sh "${nextRelease.version}"` and `bash eng/release/publishCmd.sh "${nextRelease.version}"`, (3) add both scripts to `RELEASE_DEFINITION_FILES` AND `FALLBACK_INVALIDATION_FILES` in `eng/release_evidence.py`, (4) bump `__version__` (deliberately invalidates active fallback approvals per AC34/D19), (5) update fingerprint baselines in `tests/Hexalith.FrontComposer.Testing.Tests/fixtures/release-manifest-valid.json` + any other fixture pinning `release_definition_fingerprints`, (6) update CiGovernanceTests assertions that reference `.releaserc.json` shape (line counts, embedded commands), (7) update story-spec references in `12-4-trusted-release-evidence-dry-run.md` AC table where publishCmd structure is named, (8) shellcheck the new scripts. The former signing-certificate and timestamper inputs were retired from this follow-up on 2026-08-04. Round-11 P254 already hardened the inline jq rate-limit detection so the audit-hygiene cost is acceptable through v1. Owner: release-evidence + workflow maintainers. Evidence: `.releaserc.json:11` (publishCmd), `:10` (prepareCmd).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:1

### DW-1563: No fixture pins the round-10 P243 invariant `approved_at >= expires_at`: Round-13 Edge Case Hunter EH-R13-2 — the two complete-fallback fixtures use `approved_at: "2026-05-19"` and `expires_at: "2026-12-31"` (approved before expiry). No fixture demonstrates the gate firing on `0001-01-01T00:00:00Z`-style operator footgun. Tight branch, exploitation requires repo-vars write access, regression-pin only. Pick up alongside any future fallback-validation refactor. Owner: governance-tests maintainer. Evidence: `eng/release_evidence.py:1420-1421`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-20, round 13, Group B fixtures)"), 2026-08-27
location: eng/release_evidence.py:1420-1421
reason: **CR-12-4-Def102 — No fixture pins the round-10 P243 invariant `approved_at >= expires_at`:** Round-13 Edge Case Hunter EH-R13-2 — the two complete-fallback fixtures use `approved_at: "2026-05-19"` and `expires_at: "2026-12-31"` (approved before expiry). No fixture demonstrates the gate firing on `0001-01-01T00:00:00Z`-style operator footgun. Tight branch, exploitation requires repo-vars write access, regression-pin only. Pick up alongside any future fallback-validation refactor. Owner: governance-tests maintainer. Evidence: `eng/release_evidence.py:1420-1421`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:1420

### DW-1564: No fixture pins the round-11 P259 365-day `approved_at` boundary: Round-13 Edge Case Hunter EH-R13-3 — P259 tightened `>` to `>=` to close the exact-365-day off-by-one; all complete-fallback fixtures use `approved_at: "2026-05-19"` (~1 day old). A regression flipping `>=` back to `>` would not be caught. Regression-pin only. Pick up alongside any future fallback-validation refactor. Owner: governance-tests maintainer. Evidence: `eng/release_evidence.py:1426`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-20, round 13, Group B fixtures)"), 2026-08-27
location: eng/release_evidence.py:1426
reason: **CR-12-4-Def103 — No fixture pins the round-11 P259 365-day `approved_at` boundary:** Round-13 Edge Case Hunter EH-R13-3 — P259 tightened `>` to `>=` to close the exact-365-day off-by-one; all complete-fallback fixtures use `approved_at: "2026-05-19"` (~1 day old). A regression flipping `>=` back to `>` would not be caught. Regression-pin only. Pick up alongside any future fallback-validation refactor. Owner: governance-tests maintainer. Evidence: `eng/release_evidence.py:1426`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:1426

### DW-1565: No fixture exercises `partial_publish_state="recovered"` or `"full"`: Round-13 Edge Case Hunter EH-R13-7 — `_PARTIAL_PUBLISH_STATES = {"full", "none", "partial", "recovered"}`. Fixtures cover only `"none"` (base + most cases) and `"partial"` (`partial-publish-state-rerun-attempt1`). Downstream routes any non-`"none"` to `rerun-review`, so a refactor that accidentally treats `"recovered"` as `"none"` (skipping owner review) would slip past. Regression-pin gap, low practical risk. Pick up alongside next concurrency / rerun fixture pass. Owner: governance-tests maintainer. Evidence: `eng/release_evidence.py:1259, 1267`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-20, round 13, Group B fixtures)"), 2026-08-27
location: eng/release_evidence.py:1259
reason: **CR-12-4-Def104 — No fixture exercises `partial_publish_state="recovered"` or `"full"`:** Round-13 Edge Case Hunter EH-R13-7 — `_PARTIAL_PUBLISH_STATES = {"full", "none", "partial", "recovered"}`. Fixtures cover only `"none"` (base + most cases) and `"partial"` (`partial-publish-state-rerun-attempt1`). Downstream routes any non-`"none"` to `rerun-review`, so a refactor that accidentally treats `"recovered"` as `"none"` (skipping owner review) would slip past. Regression-pin gap, low practical risk. Pick up alongside next concurrency / rerun fixture pass. Owner: governance-tests maintainer. Evidence: `eng/release_evidence.py:1259, 1267`.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:1259

### DW-1566: Asymmetric stringly-typed boolean coverage: Round-13 Blind Hunter B10 — `string-false-approval` and `concurrent-same-version-string-input` are present, no `string-true-approval` or `string-false-concurrent` counterpart. Only the "should-be-false-but-coerced-truthy" and "should-be-true-but-string" axes are pinned for one field each. A consumer that accepts any non-empty string as truthy (e.g. `"false"` parsed as truthy) would pass the existing rows but no fixture catches the inverse coercion. Pick up alongside next typed-parsing hardening pass. Owner: governance-tests maintainer. Evidence: `tests/ci-governance/fixtures/release-readiness-cases.json:189-198, 559-568`.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-20, round 13, Group B fixtures)"), 2026-08-27
location: tests/ci-governance/fixtures/release-readiness-cases.json:189-198
reason: **CR-12-4-Def105 — Asymmetric stringly-typed boolean coverage:** Round-13 Blind Hunter B10 — `string-false-approval` and `concurrent-same-version-string-input` are present, no `string-true-approval` or `string-false-concurrent` counterpart. Only the "should-be-false-but-coerced-truthy" and "should-be-true-but-string" axes are pinned for one field each. A consumer that accepts any non-empty string as truthy (e.g. `"false"` parsed as truthy) would pass the existing rows but no fixture catches the inverse coercion. Pick up alongside next typed-parsing hardening pass. Owner: governance-tests maintainer. Evidence: `tests/ci-governance/fixtures/release-readiness-cases.json:189-198, 559-568`.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: tests/ci-governance/fixtures/release-readiness-cases.json:913-928 now covers string true approval and string false concurrency; introduced by 9a2c6b6f.

### DW-1567: AC29 categories not covered by helper-side `DANGEROUS_EVIDENCE_PATTERNS`: credentialed URLs and signing-material markers: Round-13 discovered during P269 (D28 patch) application. The current pattern list at `eng/release_evidence.py:213-223` covers Bearer/token-prefix shapes, Windows paths, Unix paths, tenant/user identifiers, and workflow commands — 4 of 9 AC29 categories shape-wise (raw log fragment, env dump, username catch indirectly via embedded credentials). Two categories are NOT caught by any current pattern: (a) credentialed URLs like `https://user:pass@host/`, and (b) signing-material markers like `-----BEGIN CERTIFICATE-----` / `-----BEGIN PRIVATE KEY-----` / `-----BEGIN RSA PRIVATE KEY-----`. Adding fixtures for these would assert blocked outcomes the helper does not produce. Pick up alongside any future redaction-pattern-extension pass. Owner: release-evidence maintainer. Evidence: `eng/release_evidence.py:213-223` (`DANGEROUS_EVIDENCE_PATTERNS`) + AC29 enumeration in Story 12.4 spec.

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-20, round 13, Group B fixtures)"), 2026-08-27
location: eng/release_evidence.py:213-223
reason: **CR-12-4-Def106 — AC29 categories not covered by helper-side `DANGEROUS_EVIDENCE_PATTERNS`: credentialed URLs and signing-material markers:** Round-13 discovered during P269 (D28 patch) application. The current pattern list at `eng/release_evidence.py:213-223` covers Bearer/token-prefix shapes, Windows paths, Unix paths, tenant/user identifiers, and workflow commands — 4 of 9 AC29 categories shape-wise (raw log fragment, env dump, username catch indirectly via embedded credentials). Two categories are NOT caught by any current pattern: (a) credentialed URLs like `https://user:pass@host/`, and (b) signing-material markers like `-----BEGIN CERTIFICATE-----` / `-----BEGIN PRIVATE KEY-----` / `-----BEGIN RSA PRIVATE KEY-----`. Adding fixtures for these would assert blocked outcomes the helper does not produce. Pick up alongside any future redaction-pattern-extension pass. Owner: release-evidence maintainer. Evidence: `eng/release_evidence.py:213-223` (`DANGEROUS_EVIDENCE_PATTERNS`) + AC29 enumeration in Story 12.4 spec.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:213

### DW-1568: AC34 changed-affected-artifact trigger not enforced by `fallback_complete`: Round-13 discovered during P270 (D29 patch) application. `attestation.fallback.affected_artifact` is required (presence + non-empty per `fallback_complete:1382, 1393`) but its value is not cross-checked against `manifest.packages[*].package_id` or `artifact_path`. A hand-crafted fallback approving artifact X while the manifest ships artifact Y would currently classify `fallback-approved`. AC34 enumerates "changed affected artifacts" as a distinct invalidation trigger. Pick up alongside the helper-side AC34 hardening pass (related: ship together with Def106 redaction extensions for a single typed-classifier-hardening PR). Owner: release-evidence maintainer. Evidence: `eng/release_evidence.py:1365-1478` (`fallback_complete`).

origin: migrated from legacy ledger ("Deferred from: code review of 12-4-trusted-release-evidence-dry-run.md (2026-05-20, round 13, Group B fixtures)"), 2026-08-27
location: eng/release_evidence.py:1365-1478
reason: **CR-12-4-Def107 — AC34 changed-affected-artifact trigger not enforced by `fallback_complete`:** Round-13 discovered during P270 (D29 patch) application. `attestation.fallback.affected_artifact` is required (presence + non-empty per `fallback_complete:1382, 1393`) but its value is not cross-checked against `manifest.packages[*].package_id` or `artifact_path`. A hand-crafted fallback approving artifact X while the manifest ships artifact Y would currently classify `fallback-approved`. AC34 enumerates "changed affected artifacts" as a distinct invalidation trigger. Pick up alongside the helper-side AC34 hardening pass (related: ship together with Def106 redaction extensions for a single typed-classifier-hardening PR). Owner: release-evidence maintainer. Evidence: `eng/release_evidence.py:1365-1478` (`fallback_complete`).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/release_evidence.py:1365

### DW-1572: New-outside-filter indicator may not render promptly: `NewItemIndicatorStateService.Add` raises no change notification, so a new badge only appears on the next grid render; usually masked because the confirming lifecycle transition dispatches a Fluxor action that re-renders. Latent. Owner: Shell state maintainer. Evidence: `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:182`.

origin: migrated from legacy ledger ("Deferred from: code review of story-9.2 (2026-07-05)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:182
reason: **CR-9-2-Def04 — New-outside-filter indicator may not render promptly:** `NewItemIndicatorStateService.Add` raises no change notification, so a new badge only appears on the next grid render; usually masked because the confirming lifecycle transition dispatches a Fluxor action that re-renders. Latent. Owner: Shell state maintainer. Evidence: `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:182`.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs:43-61,145 publishes Add mutations through SnapshotPublisher; commit fb04b428.

### DW-1573: Producer covers only the polling/status-query path: `LiveNudgeRefresh`/`ReconnectReconciliation` observation sources are not currently wired to construct `PendingCommandOutcomeObservation`s, so the FC-NIP producer covers the real EventStore-confirmation path exactly as the Implementation Gate directs. No current gap; a future nudge-driven terminal resolution would need explicit wiring. Owner: Shell state maintainer. Evidence: `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs` (sole resolver caller).

origin: migrated from legacy ledger ("Deferred from: code review of story-9.2 (2026-07-05)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs
reason: **CR-9-2-Def05 — Producer covers only the polling/status-query path:** `LiveNudgeRefresh`/`ReconnectReconciliation` observation sources are not currently wired to construct `PendingCommandOutcomeObservation`s, so the FC-NIP producer covers the real EventStore-confirmation path exactly as the Implementation Gate directs. No current gap; a future nudge-driven terminal resolution would need explicit wiring. Owner: Shell state maintainer. Evidence: `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs` (sole resolver caller).
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:320-332 polls pending outcomes on live nudges, while :461-481 invokes reconciliation after reconnect.

### DW-1586: Triage the repository's open low-severity undici Dependabot alert.

origin: migrated from legacy ledger ("Deferred from: actions-29110799882-enable-dependency-graph (2026-07-10)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-actions-29110799882-enable-dependency-graph.md`
reason: summary: Triage the repository's open low-severity undici Dependabot alert. evidence: The alert pre-existed this repair and remains below the dependency-review workflow's configured high-severity failure threshold.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: commit 79d74ba1 updates package-lock.json:6561 to undici 7.29.0.
decision: 2026-08-27 Implement change — Implement the behavior requested by DW-1586, update affected contracts and consumers, and add focused regression evidence.

### DW-1588: Command bounded-context fallback now resolves to the literal `Default` while the projection fallback still uses the namespace-last segment, so an un-annotated projection and its create-command co-located in one namespace land in different bounded contexts and the empty-state CTA can silently stop resolving.

origin: migrated from legacy ledger ("Deferred from: spec-11-7-command-projection-route-contract-implementation (2026-07-11) — follow-up review"), 2026-08-27
location: RegistrationModelTransform.TransformCommand
source_spec: `_bmad-output/implementation-artifacts/spec-11-7-command-projection-route-contract-implementation.md`
reason: summary: Command bounded-context fallback now resolves to the literal `Default` while the projection fallback still uses the namespace-last segment, so an un-annotated projection and its create-command co-located in one namespace land in different bounded contexts and the empty-state CTA can silently stop resolving. evidence: `RegistrationModelTransform.TransformCommand` returns `"Default"` for a command with no `[BoundedContext]`; `RegistrationModelTransform.Transform` (projections) still returns `GetNamespaceLastSegment(namespace)`. `EmptyStateCtaResolver.ResolveByBoundedContext` only matches commands whose manifest `BoundedContext` equals the projection's, so an un-annotated co-located pair no longer matches and the "Send your first ..." CTA drops with no diagnostic. The `Default` rule is mandated by the intent (I/O matrix "Missing BC → /commands/Default/{TypeName}", AC route↔metadata coherence) and projections cannot be symmetrized to `Default` ("Never change projection routes"), so the asymmetry is an accepted consequence rather than a spec defect; impact is limited to adopters relying on implicit namespace inference to link a projection and command instead of the documented explicit `[BoundedContext]` mechanism (as the Counter sample uses). No test exercises the divergence.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-1591: Strengthen the pre-existing Contracts.UI package dependency proof so broken dependency metadata cannot false-pass.

origin: migrated from legacy ledger ("Deferred from: code review of 11-14-update-architecture-context-ux-and-package-compat-docs.md (2026-07-11)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs:79-90
source_spec: `_bmad-output/implementation-artifacts/11-14-update-architecture-context-ux-and-package-compat-docs.md`
reason: summary: Strengthen the pre-existing Contracts.UI package dependency proof so broken dependency metadata cannot false-pass. evidence: `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs:79-90` searches the raw nuspec for a package-ID substring and directly references Fluent in the consumer; parse exact dependency IDs/versions and let the package supply Fluent transitively.
status: done 2026-08-29
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs:84-99,114-138 parses exact nuspec dependencies and proves a transitive clean consumer

### DW-1593: Resolved 2026-08-04 — reconcile the generated project index with the live unsigned-package release posture.

origin: migrated from legacy ledger ("Deferred from: code review of 11-14-update-architecture-context-ux-and-package-compat-docs.md (2026-07-11)"), 2026-08-27
location: _bmad-output/project-docs/index.md
source_spec: `_bmad-output/implementation-artifacts/11-14-update-architecture-context-ux-and-package-compat-docs.md`
reason: summary: Resolved 2026-08-04 — reconcile the generated project index with the live unsigned-package release posture. evidence: `_bmad-output/project-docs/index.md` and `project-scan-report.json` now describe exact-SHA operator dispatch, unsigned candidates, and NuGet.org repository-signature verification.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/project-docs/index.md:1

### DW-1594: Make clean package-consumer fixtures honor NuGet's effective global packages folder.

origin: migrated from legacy ledger ("Deferred from: code review of 11-14-update-architecture-context-ux-and-package-compat-docs.md (2026-07-11)"), 2026-08-27
location: ~/.nuget/packages
source_spec: `_bmad-output/implementation-artifacts/spec-actions-29182697666-fix-cicd.md`
reason: summary: Make clean package-consumer fixtures honor NuGet's effective global packages folder. evidence: The pre-existing fixtures construct `~/.nuget/packages` directly, so environments using `NUGET_PACKAGES` can fail despite already containing every restored dependency in their configured global folder.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs:270-278 honors NUGET_PACKAGES and falls back to the user package folder; commit 18642609.

### DW-1595: Clean up temporary clean-consumer package, project, and cache directories after package-boundary tests.

origin: migrated from legacy ledger ("Deferred from: code review of 11-14-update-architecture-context-ux-and-package-compat-docs.md (2026-07-11)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-actions-29182697666-fix-cicd.md`
reason: summary: Clean up temporary clean-consumer package, project, and cache directories after package-boundary tests. evidence: The pre-existing package-consumer tests allocate unique `/tmp` directories on every run and never remove them, causing repeated local or CI executions to accumulate package payloads and generated build outputs.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs:29-44,62-119 uses unique consumer directories and removes them in finally blocks.

### DW-1600: Complete the one-type-per-file split for the retained projection fallback contracts and scheduler helper types under Story 11.17.

origin: migrated from legacy ledger ("Deferred from: code review of 11-11-create-contracts-ui-assembly-and-migrate-blazor-rendering-surface (2026-07-12)"), 2026-08-27
location: src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshContracts.cs
source_spec: `_bmad-output/implementation-artifacts/spec-11-9-shell-layering-declaration-and-route-label-relocation.md`
reason: summary: Complete the one-type-per-file split for the retained projection fallback contracts and scheduler helper types under Story 11.17. evidence: `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionFallbackRefreshContracts.cs` retains the pre-existing public contract bundle and the Infrastructure scheduler retains nested helper types; Story 11.9 explicitly excludes the separately planned mechanical split.
status: done 2026-08-29
archived: 2026-09-18
resolution: already resolved: commit 5092b041 deleted ProjectionFallbackRefreshContracts.cs and split its declarations into same-named one-type files

### DW-1607: A genuinely-timestamped package can be scored `timestamp_status="missing"` when a verbose cert chain pushes the `Timestamp:` line outside the 80-line scan window.

origin: migrated from legacy ledger ("Deferred from: code review of rel-2-align-frontcomposer-cicd-with-tenants (2026-07-13)"), 2026-08-27
location: eng/release_evidence.py:642
source_spec: `_bmad-output/implementation-artifacts/rel-2-align-frontcomposer-cicd-with-tenants.md`
reason: summary: A genuinely-timestamped package can be scored `timestamp_status="missing"` when a verbose cert chain pushes the `Timestamp:` line outside the 80-line scan window. evidence: `_timestamp_verified_in_region` truncates each package's `dotnet nuget verify -v normal` region to the last `_TIMESTAMP_BLOCK_MAX_LINES = 80` lines before scanning for the timestamp confirmation (`eng/release_evidence.py:642,663-676`). If cert-chain output pushes the `Timestamp:` line more than 80 lines above the package's `Successfully verified` line, the real timestamp falls outside the window → `timestamp_status="missing"` → `prepare-manifest` fails closed on a genuinely-timestamped package. Deferred: unlikely with normal `-v normal` output (per-package blocks are typically well under 80 lines), but a fragility to harden. Evidence: `eng/release_evidence.py:642,669`.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: commit 936913b0 removed parse_signing_verification and its bounded Timestamp scan when author-signing parsing was retired.

### DW-1656: Replace maintained per-project benchmark commands with the repository's focused direct-executable pattern and always disable DiffEngine.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-mcp-runtime-split-and-benchmark-relocation (2026-07-16)"), 2026-08-27
location: .github/workflows/nightly.yml:49-53
source_spec: `_bmad-output/implementation-artifacts/11-17-mcp-runtime-split-and-benchmark-relocation.md`
reason: summary: Replace maintained per-project benchmark commands with the repository's focused direct-executable pattern and always disable DiffEngine. evidence: `.github/workflows/nightly.yml:49-53` and `tests/README.md:105-106` retain the pre-existing project-level `dotnet test --filter` pattern; the README command also omits the repository-required `DiffEngine_Disabled=true` environment setting. The Story 11.17c change only retargeted those commands from MCP.Tests to the Bench project.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: Commit d08d1dec; .github/workflows/nightly.yml:53-57 and tests/README.md:105-116 now use focused project-level MTP execution with DiffEngine_Disabled=true.

### DW-1668: Verify JSON-formatted parse failures for Hexalith.Builds command applications.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-mcp-runtime-split-and-benchmark-relocation — chunk 1 (2026-07-17)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-actions-29585546315-fix-cicd-2.md`
reason: summary: Verify JSON-formatted parse failures for Hexalith.Builds command applications. evidence: The updated Hexalith.Builds submodule's parse-valid command tests do not send malformed arguments with `--output json`, so a regression of the HXC001 JSON parse-failure contract would be undetected.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: references/Hexalith.Builds/test/Hexalith.Builds.Module.Tests/ModuleCommandApplicationTests.cs:88-147 exercises malformed or blank command arguments with --output json and asserts HXC001 JSON output; commit 345e0ce.

### DW-1669: Add symlink-containment coverage for module manifest paths.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-mcp-runtime-split-and-benchmark-relocation — chunk 1 (2026-07-17)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-actions-29585546315-fix-cicd-2.md`
reason: summary: Add symlink-containment coverage for module manifest paths. evidence: The updated Hexalith.Builds submodule tests lexical traversal but does not exercise an in-repository symlink resolving outside the repository root.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: references/Hexalith.Builds/test/Hexalith.Builds.Module.Tests/ManifestValidationTests.cs:118-145 contains LoadManifestWithEscapingDescriptorSymlinkFailsClosed; commit 345e0ce.

### DW-1674: Remove the release-path-dead `eng/pack_release_packages.py` and port its unit coverage to the live `scripts/pack-release-packages.py` packer.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-mcp-runtime-split-and-benchmark-relocation — chunk 1 (2026-07-17)"), 2026-08-27
location: eng/pack_release_packages.py
source_spec: `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`
reason: summary: Remove the release-path-dead `eng/pack_release_packages.py` and port its unit coverage to the live `scripts/pack-release-packages.py` packer. evidence: REL-3 replaced the `.releaserc.json` prepareCmd with `eng/release_prepublish.py` (which packs via `scripts/pack-release-packages.py`), leaving `eng/pack_release_packages.py` outside every release path while `quality.yml` still runs its green unit lane (`tests/eng/test_pack_release_packages.py`) and the live scripts packer has no unit tests of its own.
status: done 2026-08-29
archived: 2026-09-18
resolution: already resolved: commit 2dcc43fe deleted eng/pack_release_packages.py and ported its tests to scripts/pack-release-packages.py

### DW-1676: Unpublished `Hexalith.Tenants.Client 3.15.1` (NU1102) blocks the non-publishing chain's SBOM phase; the catalog-pin facet was resolved by owner directive.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-mcp-runtime-split-and-benchmark-relocation — chunk 1 (2026-07-17)"), 2026-08-27
location: test/consumer
source_spec: `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`
reason: summary: Unpublished `Hexalith.Tenants.Client 3.15.1` (NU1102) blocks the non-publishing chain's SBOM phase; the catalog-pin facet was resolved by owner directive. evidence: 2026-07-18 (updated same day): the Release Owner directed the governance expectation to `System.Reactive 7.0.0` and the nested-gitlink pins were realigned (EventStore 08b57086, Memories/Parties 041897f0) — all 99 governance tests and the chain's build/pack/test/consumer phases are green. The remaining blocker is `dotnet CycloneDX` over the slnx: it restores every solution project individually (including the Debug-only UI/AppHost/Parties graph), which fails NU1102 on `Hexalith.Tenants.Client >= 3.15.1` (nuget.org nearest 3.2.17) — the same in-flight gitlink-bump regression tracked by the Release Owner's parallel session. When 3.15.1 publishes (or the pin is corrected), re-run `prepare --non-publishing` for the fully green chain; consider whether the FR24 SBOM should be scoped to the packable Release graph instead of the whole slnx.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: references/Hexalith.Builds/Props/Directory.Packages.props:11 selects published Hexalith.Tenants.Client 5.4.1 and the unavailable 3.15.1 pin is absent; the root Builds gitlink is c8837217.

### DW-1682: Normalize the authoritative Builds package catalog to the repository's required UTF-8 BOM and CRLF format.

origin: migrated from legacy ledger ("Deferred from: code review of spec-move-parties-package-versions-to-hexalith-builds.md (2026-07-19)"), 2026-08-27
location: InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog
source_spec: `_bmad-output/implementation-artifacts/spec-move-parties-package-versions-to-hexalith-builds.md`
reason: summary: Normalize the authoritative Builds package catalog to the repository's required UTF-8 BOM and CRLF format. evidence: `InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog` encounters 18 bare-LF line endings in the pre-existing `c177c66` catalog before reaching its package-ownership assertions; this story does not modify that catalog.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: references/Hexalith.Builds/Props/Directory.Packages.props:1 is currently UTF-8 with BOM and CRLF line endings; the repaired catalog is present at the pinned Builds revision.

### DW-1684: Reconcile the published Hexalith.Builds catalog-format commit with mandatory Conventional Commit history.

origin: migrated from legacy ledger ("Deferred from: code review of spec-actions-29681767891-fix-cicd.md (2026-07-19)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-actions-29681767891-fix-cicd.md`
reason: summary: Reconcile the published Hexalith.Builds catalog-format commit with mandatory Conventional Commit history. evidence: An external process published Builds commit `deb76e983434335c990b0a1f676b8887d643a274` with subject `Refactor code structure for improved readability and maintainability`; commitlint reports `type-empty` and `subject-empty`. The no-push workflow cannot rewrite published history, and a force push requires separate human authorization.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current verified behavior as intentional and close the deferred row.
decision: 2026-08-27 Accept current behavior — Record the current verified behavior as intentional and close the deferred row.

### DW-1685: Bind shared-catalog governance bytes to the same Builds commit recorded by the FrontComposer gitlink.

origin: migrated from legacy ledger ("Deferred from: code review of spec-actions-29681767891-fix-cicd.md (2026-07-19)"), 2026-08-27
location: InfrastructureGovernanceTests
source_spec: `_bmad-output/implementation-artifacts/spec-actions-29681767891-fix-cicd.md`
reason: summary: Bind shared-catalog governance bytes to the same Builds commit recorded by the FrontComposer gitlink. evidence: `InfrastructureGovernanceTests` reads the catalog bytes from the Builds working tree but reads the expected root Builds SHA from the FrontComposer index. A dirty Builds checkout can therefore validate one commit's bytes while asserting another commit's gitlink; fresh CI checkouts are unaffected, but local fail-closed governance is not identity-bound.
status: done 2026-08-29
archived: 2026-09-18
resolution: already resolved: commit e3e3dcf5 introduced the committed-object graph engine; eng/dependency_graph.py:207 reads the selected catalog blob with git cat-file

### DW-1686: Reconcile FrontComposer's root and EventStore nested Builds gitlinks with their approved governance expectations, then rerun the complete Shell Governance lane before completing Story 11.17d.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split.md — evidence/status chunk (2026-07-19)"), 2026-08-27
location: references/Hexalith.Builds
source_spec: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md`
reason: summary: Reconcile FrontComposer's root and EventStore nested Builds gitlinks with their approved governance expectations, then rerun the complete Shell Governance lane before completing Story 11.17d. evidence: On clean commit `6a4350ec`, the Release Shell.Tests build completed with zero warnings/errors, but the direct Governance lane passed 186/188. `InfrastructureGovernanceTests.PartiesPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` expects root `references/Hexalith.Builds` commit `deb76e98` while the root gitlink is `4bbe7c04`; `CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog` expects EventStore's nested Builds gitlink `c177c66a` while it is `4bbe7c04`. Both mismatches are FrontComposer-tracked and were introduced outside Story 11.17d by concurrent commit `6a4350ec`. Administrator selected keeping the story in progress until the mismatches are reconciled and the complete lane passes on the exact promotion revision.
status: done 2026-08-29
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/11-17-shell-bundle-split.md:89 records the root/EventStore Builds-gitlink mismatch as resolved with governance evidence

### DW-1691: The declaration guard pins type-level identity only (Path|Identity|Kind|Accessibility|Modifiers) and not member-level identity, so a future move that reordered a record's parameters, renumbered an enum member, dropped a base type/interface, changed an attribute, generic constraint, or nullability would pass the guard green.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split (2026-07-21)"), 2026-08-27
location: ShellTypeOrganizationGovernanceTests.DeclarationPin
source_spec: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md`
reason: summary: The declaration guard pins type-level identity only (Path|Identity|Kind|Accessibility|Modifiers) and not member-level identity, so a future move that reordered a record's parameters, renumbered an enum member, dropped a base type/interface, changed an attribute, generic constraint, or nullability would pass the guard green. evidence: `ShellTypeOrganizationGovernanceTests.DeclarationPin` and `ShellTypeOrganizationGovernanceTests.RuntimeKind` assert kind/accessibility/modifiers/top-level/assembly only. This meets AC4 as written (AC4 scopes the pins to type identities/kinds/modifiers/accessibility/top-level assembly ownership) and member-level preservation of the current 111 targets was manually verified during this review. Reopen trigger: AC4 is extended to member-level identity, or a member-level drift regression is observed.
status: done 2026-08-28
archived: 2026-09-18
decision: 2026-08-28 Accept current behavior — Record the current behavior as intentional and close DW-1691 with its verified rationale.
resolution: closed by human decision: Record the current behavior as intentional and close DW-1691 with its verified rationale.
decision: 2026-08-28 Accept current behavior — Record the current behavior as intentional and close DW-1691 with its verified rationale.

### DW-1693: Whole-project direct-`Log*` count pins in `SecurityLoggingGovernanceTests` (`sites.Length == 208`, `117` 11.18b + `91` 11.18c, plus a 45-file `ExpectedDirectCallCounts` map) are brittle — any unrelated logging edit in any of ~45 Shell files breaks the test, training maintainers to "just bump the number", and a careless bump could mask a genuinely new direct call.

origin: migrated from legacy ledger ("Deferred from: code review of 11-18-fail-closed-security-log-sites (2026-07-21)"), 2026-08-27
location: SecurityLoggingGovernanceTests.cs:51-103
source_spec: `_bmad-output/implementation-artifacts/11-18-fail-closed-security-log-sites.md`
reason: summary: Whole-project direct-`Log*` count pins in `SecurityLoggingGovernanceTests` (`sites.Length == 208`, `117` 11.18b + `91` 11.18c, plus a 45-file `ExpectedDirectCallCounts` map) are brittle — any unrelated logging edit in any of ~45 Shell files breaks the test, training maintainers to "just bump the number", and a careless bump could mask a genuinely new direct call. evidence: `SecurityLoggingGovernanceTests.cs:51-103,118,136-137` (rev 3356ae7e). The actual security guarantee is the separate `sites.Where(SecuritySourcePaths...).ShouldBeEmpty()` assertion, which is correct and sufficient; the global count pins add churn without security signal. Intentional per AC1 ("the denominator is never silently reduced") so retained by design, but flagged as a maintainability trade-off. Reopen trigger: the count pins are relaxed to per-security-file assertions, or a count-bump is observed masking a real regression.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs:242-266 asserts zero direct calls and scoped ownership without brittle whole-project count pins.
decision: 2026-08-28 Implement requested change — Implement DW-1693 at its recorded touchpoint, update affected contracts and consumers, and add focused regression evidence.
decision: 2026-08-28 Implement requested change — Implement DW-1693 at its recorded touchpoint, update affected contracts and consumers, and add focused regression evidence.

### DW-1696: Refresh generated project-context dependency facts that no longer match the selected shared catalog.

origin: migrated from legacy ledger ("Deferred from: code review of run-current-tests-and-fix-failures (2026-07-31)"), 2026-08-27
location: _bmad-output/project-context.md
source_spec: `_bmad-output/implementation-artifacts/spec-run-all-tests-fix-failures-2.md`
reason: summary: Refresh generated project-context dependency facts that no longer match the selected shared catalog. evidence: `_bmad-output/project-context.md` still lists FsCheck.Xunit.v3 3.3.3 and Verify/Verify.XunitV3 31.22.0, while baseline Builds catalog `b529b665` selects 3.3.4 and 31.27.0. The drift pre-dated this repair's policy edit and requires a separately owned project-context regeneration.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: _bmad-output/project-context.md:50-51 now matches references/Hexalith.Builds/Props/Directory.Packages.props:158,314-315 for FsCheck.Xunit.v3 3.4.0 and Verify 32.0.0; context update commit 1610415e.

### DW-1698: Add invalid-manifest fixtures for EventStore release-manifest normalization and containment guards.

origin: migrated from legacy ledger ("Deferred from: code review of run-current-tests-and-fix-failures (2026-07-31)"), 2026-08-27
location: release_package_contract.py
source_spec: `_bmad-output/implementation-artifacts/spec-run-all-tests-fix-failures-2.md`
reason: summary: Add invalid-manifest fixtures for EventStore release-manifest normalization and containment guards. evidence: The concurrently advanced EventStore checkout's `tools/release_package_contract.py` rejects malformed, duplicate, foreign, traversal, non-normalized, and out-of-root entries, while current tests exercise the checked-in valid manifest only. This submodule work was unrelated to and preserved by the FrontComposer repair.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: references/Hexalith.EventStore/tests/Hexalith.EventStore.Contracts.Tests/Packaging/ReleasePackageManifestTests.cs:172-183 exercises malformed, duplicate, foreign, traversal, non-normalized, out-of-root, and missing manifest entries.

### DW-1701: Reconcile contradictory SDK, REL-1, and REL-4 truth-state records introduced after Story 11.18b.

origin: migrated from legacy ledger ("Deferred from: code review of 11-18-warning-and-above-log-sites.md (2026-07-31)"), 2026-08-27
location: _bmad-output/implementation-artifacts/sprint-status.yaml:381
source_spec: `_bmad-output/implementation-artifacts/sprint-status.yaml`
reason: summary: Reconcile contradictory SDK, REL-1, and REL-4 truth-state records introduced after Story 11.18b. evidence: The current baseline-to-HEAD status file says the expected and reported SDK are both `10.0.302` while calling that a blocker, reopens REL-1 as backlog after recording it closed as superseded, and still says REL-4 enforcement is pending after later entries record implementation and review (`_bmad-output/implementation-artifacts/sprint-status.yaml:381,524-525,621-623`). Git blame places these edits in later non-11.18b commits, so their owning status/release work must reconcile them separately.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: Commit 7d85692399f5886b7e7812255b89c98830d2b135 removed the contradictory SDK blocker and stale REL-1/REL-4 truth-state rows from _bmad-output/implementation-artifacts/sprint-status.yaml; the current tracker no longer contains the cited contradictions.
decision: 2026-08-27 Implement change — Implement the behavior requested by DW-1701, update affected contracts and consumers, and add focused regression evidence.

### DW-1703: Bind the analyzer-policy identifier inventory to a committed revision and assert the delta, instead of hashing working-tree line numbers.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split.md — promotion delta (2026-08-01)"), 2026-08-27
location: tests/**/*.cs
source_spec: `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
reason: summary: Bind the analyzer-policy identifier inventory to a committed revision and assert the delta, instead of hashing working-tree line numbers. evidence: `AnalyzerPolicyGovernanceTests.ValidateIdentifierInventory` recomputes `count` and `sha256` over `path:line:token` from the working tree using the same function that produced the pinned values, so the gate detects only "the ledger is stale" and every drift is resolvable by pasting back the value the failure message prints. Because the hash encodes one-based line numbers, any line-shifting edit anywhere under `tests/**/*.cs` breaks the seal without changing the token count. Reproduced at clean HEAD `04057737`: the test fails with `count=6207` — exactly the sealed count — and `sha256=d31b7ad221d52e8964841b4b64af010216bba90c35fd0aeba0627e139901c648` against the sealed `3ca33cb725c9e1512a1c1b9ef40a4fc4421d0b370476a77e85771059d6edce04`, caused purely by the line shift committed in `04057737`. Owner: GOV-1 / Story 11.19. **Correction (group 4, 2026-08-02):** absorbing *foreign* identifier drift remains on Story 11.17d's Never-List; re-sealing for this story's *own* added test identifiers is in scope under the Administrator's 2026-08-01 ruling (Never-List text reconciled in group 4). Reopen trigger: the inventory is computed from a committed revision, or a delta-attribution assertion (added tokens must lie in approved test projects) is added.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: Commit 1610415e; tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:2038-2048 derives a semantic declaration inventory instead of path-and-line token hashing.

### DW-1706: The Story 11.17d-only GOV-1 promotion waiver exists only as prose inside a `waiver:` string, so any consumer reading the action item's structured fields still sees an unmet gate blocking the promotion that already happened.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split.md — promotion-delta re-review (2026-08-01)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/sprint-status.yaml`
reason: summary: The Story 11.17d-only GOV-1 promotion waiver exists only as prose inside a `waiver:` string, so any consumer reading the action item's structured fields still sees an unmet gate blocking the promotion that already happened. evidence: **Reconciled 2026-08-02 by approved GOV-1 course correction.** Story 11.17d completed on 2026-08-02 and is not reopened. The action's structured `due` field now contains only the unwaived condition, `before the next accepted governed release manifest`; the dated waiver is retained as history instead of competing current state. No schema extension was necessary. Reopen trigger: a future action requires a simultaneous partial waiver that cannot be represented by correcting its remaining due condition.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/sprint-status.yaml:260 limits the live GOV-1 due condition to before the next accepted governed release manifest, while line 273 retains the Story 11.17d waiver as historical context.
decision: 2026-08-27 Implement change — Implement the behavior requested by DW-1706, update affected contracts and consumers, and add focused regression evidence.

### DW-1707: Execute the story-artifact validator's own test suite in CI, so a regression to the repository-wide evidence gate cannot ship unnoticed.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split.md — promotion-delta commit re-review (2026-08-01)"), 2026-08-27
location: eng/tests/test_validate_story_artifacts.py
source_spec: `eng/validate-story-artifacts.py`
reason: summary: Execute the story-artifact validator's own test suite in CI, so a regression to the repository-wide evidence gate cannot ship unnoticed. evidence: `eng/tests/test_validate_story_artifacts.py` is referenced by no workflow, script, or MSBuild target. The only Python unittest invocation in CI runs `tests/eng/test_pack_release_packages.py` — a different file in a different directory — and a search across `.github` for `eng/tests` or `test_validate_story` returns nothing. Measured consequence: inserting `return True` at the top of `mention_is_not_an_output_path`, which exempts every backticked path in every checked task in every story, passes both `ci.yml` and `quality.yml` untouched; the suite that catches it runs only when a human or agent types the command. Pre-existing: both the validator and its suite were added by Story 10.1, long before this story. Owner: repository tooling, not a Shell organization story. Reopen trigger: a story is promoted on artifact-validation evidence after the gate has silently regressed, or the quality workflow gains a Python unit-test stage.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/tests/test_validate_story_artifacts.py:1

### DW-1713: Reconciled — remove the former GOV-1 Closed Policy Seed drift and establish one executable policy authority.

origin: migrated from legacy ledger ("Deferred from: spec-11-17-align-shell-catalog-policy-with-builds review (2026-08-01)"), 2026-08-27
location: eng/dependency-graph-policy.json
source_spec: `_bmad-output/implementation-artifacts/spec-11-17-align-shell-catalog-policy-with-builds.md`
reason: summary: Reconciled — remove the former GOV-1 Closed Policy Seed drift and establish one executable policy authority. evidence: **Reconciled 2026-08-02 by approved GOV-1 course correction.** The spine no longer duplicates volatile owner/profile/value, module-disposition, limit, or evaluator-authorization rows. `eng/dependency-graph-policy.json` is the sole executable authority; architecture retains the closed schema, complete-coverage invariant, delayed activation rule, and fail-closed trust boundary. Handoffs and manifest v2 must seal the policy repository/path/schema/revision/digest. Reopen trigger: architecture prose again duplicates an executable policy value or a Governance check permits a second executable profile seed.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/dependency-graph-policy.json:1

### DW-1714: Restore the REL-6 three-way lockstep between the `release.yml` reusable-workflow pin and the root `references/Hexalith.Builds` gitlink.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split (2026-08-01, catalog-policy promotion delta)"), 2026-08-27
location: release.yml
source_spec: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md`
reason: summary: Restore the REL-6 three-way lockstep between the `release.yml` reusable-workflow pin and the root `references/Hexalith.Builds` gitlink. evidence: `.github/workflows/release.yml:92,95` pin `domain-release.yml@79f82acc` and pass the matching `builds-execution-sha`, while the root Builds gitlink advanced to `e69891f6` in commit `62841406`. `CiGovernanceTests` (`tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:497-507`) asserts only `uses:@sha == builds-execution-sha`, so the Governance lane's green result does not cover the gitlink leg. The reusable job's own `job.workflow_sha == builds-execution-sha` check still passes, so this is latent drift rather than a startup failure. Arises from the committed range, not the reviewed worktree delta. Reopen trigger: the next Builds gitlink bump, or any release attempt.
status: done 2026-08-28
archived: 2026-09-18
decision: 2026-08-28 Close as accepted — Accept the current verified behavior and close the deferred row without implementation.
resolution: closed by human decision: Accept the current verified behavior and close the deferred row without implementation.
decision: 2026-08-28 Close as accepted — Accept the current verified behavior and close the deferred row without implementation.

### DW-1715: Bind the policy's blessed `HexalithTenantsVersion` to the Tenants gitlink or to an actual FrontComposer consumer.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split (2026-08-01, catalog-policy promotion delta)"), 2026-08-27
location: eng/dependency-graph-policy.json:33
source_spec: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md`
reason: summary: Bind the policy's blessed `HexalithTenantsVersion` to the Tenants gitlink or to an actual FrontComposer consumer. evidence: `grep -rn "HexalithTenantsVersion" src/ tests/ eng/ scripts/ .github/` returns exactly one hit — the policy row at `eng/dependency-graph-policy.json:33`. The `hexalith.tenants` edge maps to `shared-catalog-baseline-v1`, whose required properties and packages are both empty, so the Tenants edge is provenance-only. Source mode resolves Tenants through the `references/Hexalith.Tenants` gitlink and package mode through the catalog property; nothing asserts the two denote the same release, so a source/package skew that still compiles is invisible. Reopen trigger: a Tenants gitlink move that is not accompanied by a catalog bump, or the first source/package behavioural divergence.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: Commit 54edc44f801e94fcf71e8589c54b181c43488a71 removed the stale exact HexalithTenantsVersion mirror. eng/dependency-graph-policy.json:59-67 now requires the property name's presence while selected_catalog_required_properties is deliberately empty; _bmad-output/project-context.md:264-275 documents the value-independent contract.
decision: 2026-08-28 Implement change — Implement the requested change at eng/dependency-graph-policy.json:33, update affected contracts and consumers, and add focused regression evidence.
decision: 2026-08-28 Implement change — Implement the requested change at eng/dependency-graph-policy.json:33, update affected contracts and consumers, and add focused regression evidence.

### DW-1716: Give the C# Governance facts a signal for what a profile enforced, not only that its edge was visited.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split (2026-08-01, catalog-policy promotion delta)"), 2026-08-27
location: eng/dependency_graph.py:591
source_spec: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md`
reason: summary: Give the C# Governance facts a signal for what a profile enforced, not only that its edge was visited. evidence: `eng/dependency_graph.py:591` appends `validated {edge_context} under profile {profile_name}` once per edge regardless of how many checks ran inside the profile, and `InfrastructureGovernanceTests` asserts on those strings plus `selectors_validated`. Measured during this review: deleting the whole required-property loop leaves `validate` at `ok: true, selectors_validated: 7` with all profile diagnostics intact and both Governance facts green. Every future profile check inherits the same blind spot. Reopen trigger: a new profile check is added, or the dependency-graph suite is wired into CI (which would supply the missing signal by another route).
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: The row's alternate reopening condition is satisfied: .github/workflows/quality.yml:157-166 runs the dependency-graph semantic suites in blocking Gate 2b. tests/eng/test_dependency_graph.py:529-554 covers required-property match/missing/duplicate/mismatch and :1880-1934 pins the landed profile contract.

### DW-1720: Restore a lane that exercises CI's own `dotnet test` invocation path, or record the reduction in guarantee.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split (2026-08-01, catalog-policy promotion delta)"), 2026-08-27
location: Hexalith.FrontComposer.slnx
source_spec: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md`
reason: summary: Restore a lane that exercises CI's own `dotnet test` invocation path, or record the reduction in guarantee. evidence: The story's documented commands replaced two solution-level `dotnet test Hexalith.FrontComposer.slnx` lanes with eight direct xUnit v3 runner invocations. The direct runners bypass VSTest discovery, RunSettings, and MSBuild-supplied properties, which is the path the reusable `domain-ci` workflow drives. A discovery-only regression — a project missing from the `.slnx` Release configuration, an `IsTestProject` break — is therefore unobservable locally and first surfaces in CI. The swap is repository-compliant (VSTest/Playwright sockets are blocked in this environment), so this is a disclosed-reduction item rather than a defect. Reopen trigger: a CI-only test-discovery failure, or the socket restriction being lifted.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: Commit d08d1dec; .github/workflows/quality.yml:121-149 and :271-284 execute solution/project MTP paths and verify their TRX output.
decision: 2026-08-27 Implement change — Implement the behavior requested by DW-1720, update affected contracts and consumers, and add focused regression evidence.

### DW-1721: Deferred tasks can self-exempt with bare fabricated basenames — existence check only runs when `"/"` is in the path.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split (2026-08-02, group 3 catalog-policy/eng/CI)"), 2026-08-27
location: eng/validate-story-artifacts.py
source_spec: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md`
reason: summary: Deferred tasks can self-exempt with bare fabricated basenames — existence check only runs when `"/"` is in the path. evidence: `eng/validate-story-artifacts.py` deferred existence check requires `"/"` in path; bare phantoms never enter `task_paths`. Pre-existing; surfaced again in group 3. Reopen trigger: bare-basename deferred phantoms are included in existence checks, or the validator documents that bare basenames are intentionally non-evidence.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: eng/validate-story-artifacts.py:2551-2569 explicitly documents bare, tree-absent basenames as hypothetical non-output evidence and makes creation claims strict; :2593-2602 implements that exact documented boundary.

### DW-1723: Dead policy surfaces (`module_build_registry`, `evaluator_authorizations`, contract-tree `resource_limits`) are unused while comments advertise broader Task/AD scope.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split (2026-08-02, group 3 catalog-policy/eng/CI)"), 2026-08-27
location: eng/dependency-graph-policy.json
source_spec: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md`
reason: summary: Dead policy surfaces (`module_build_registry`, `evaluator_authorizations`, contract-tree `resource_limits`) are unused while comments advertise broader Task/AD scope. evidence: `eng/dependency-graph-policy.json` scaffolding fields. Reopen trigger: GOV-1 activates those surfaces or the comments are narrowed to the implemented scope.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: The formerly dead policy fields are active: eng/dependency_graph.py:337,381,605,734,877,935,987 consumes resource_limits and module_build_registry; eng/workflow_source_closure.py:177-185,1068-1100 consumes limits and evaluator_authorizations; eng/dependency_handoff.py:103,571 enforces evaluator authorization.
decision: 2026-08-27 Implement change — Implement the behavior requested by DW-1723, update affected contracts and consumers, and add focused regression evidence.

### DW-1724: Shallow CI `fetch-depth: 1` plus multi-pin Builds blob resolution through one FrontComposer Builds object store can under-validate non-HEAD pins when objects are missing.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split (2026-08-02, group 3 catalog-policy/eng/CI)"), 2026-08-27
location: .github/workflows/quality.yml
source_spec: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md`
reason: summary: Shallow CI `fetch-depth: 1` plus multi-pin Builds blob resolution through one FrontComposer Builds object store can under-validate non-HEAD pins when objects are missing. evidence: `.github/workflows/quality.yml` shallow clone + latent multi-pin risk. Pre-existing. Reopen trigger: fetch depth covers required Builds objects, or multi-pin resolution fails closed when a pin blob is absent.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: Commit 8a6a6cb322e978b361170eb7a0a2cc689a4b8b8c changed the primary checkout to complete history for GOV-1. Current .github/workflows/quality.yml:37-43,510-520,602-607 uses fetch-depth: 0 at all three checkout sites.

### DW-1725: `baseline_commit` remains `0a84e818` despite repeated foreign-absorption and ~40–122 commit staleness warnings across review chunks.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split (2026-08-02, group 4 story artifacts/docs)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md`
reason: summary: `baseline_commit` remains `0a84e818` despite repeated foreign-absorption and ~40–122 commit staleness warnings across review chunks. evidence: Frontmatter still pins `0a84e818` while review text and validator File List class repeatedly note that diffs absorb concurrent Story 11.18 and other foreign work. Re-baselining is a process action, not a docs-only patch. Reopen trigger: a dated Administrator decision to move `baseline_commit` past the contaminating range, or the next promotion measurement that requires a clean identity census. resolution: Closed 2026-08-02 chunk C — Administrator Decision 1 updated frontmatter `baseline_commit` to `32db5c3460a4aa0ae6382ae9db36fa42e512ffd3` (post-11.18 tip / latest 111-target touch).
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/11-17-shell-bundle-split.md:2 pins baseline_commit 32db5c3460a4aa0ae6382ae9db36fa42e512ffd3, replacing stale 0a84e818.

### DW-1726: Committed catalog pins are mutation-blind to Governance/synthetic suites — emptying `selected_catalog_required_*` still passes shape checks and profile-visit diagnostics.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split (2026-08-02, group 4 story artifacts/docs)"), 2026-08-27
location: deferred-work.md
source_spec: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md`
reason: summary: Committed catalog pins are mutation-blind to Governance/synthetic suites — emptying `selected_catalog_required_*` still passes shape checks and profile-visit diagnostics. evidence: Group 3 deferred this as "already recorded", but no prior `deferred-work.md` entry matched. Live facts assert profile visit / shape, not pin contents. Owner: GOV-1 / dependency-graph maintainers. Reopen trigger: Governance or synthetic suite asserts non-empty pin contents against the selected catalog, or the profile schema gains a fail-closed content seal.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: tests/eng/test_dependency_graph.py:1880-1901 pins the exact closed FrontComposer profile shape and nonempty required-name set; :1924-1934 rejects removal of every required module property; :2060-2076 rejects mutation of exact required packages. Emptying the selected-catalog contract no longer passes.

### DW-1728: Story-artifact unit suite still unwired from Gate 2b while promotion cites its green counts.

origin: migrated from legacy ledger ("Deferred from: code review of 11-17-shell-bundle-split (2026-08-02, AC6 promotion chunk C)"), 2026-08-27
location: quality.yml
source_spec: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md`
reason: summary: Story-artifact unit suite still unwired from Gate 2b while promotion cites its green counts. evidence: Already recorded in group 3; resurfaced because Chunk C adds suite coverage and cites 51/49/2 as promotion evidence while `quality.yml` Gate 2b still runs only dependency-graph / pack-release. Reopen trigger: `eng/tests/test_validate_story_artifacts.py` is wired into CI, or promotion evidence stops citing the unwired suite.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/11-17-shell-bundle-split.md:1

### DW-1731: CA1707 EditorConfig suppressions for `tests/.cs` and `FcDiagnosticIds.cs` appear in the 11.19a baseline-to-HEAD delta but were landed later in `67154049` (analyzer-policy / Story 11.20 surface), not the CS1591 realignment commit.

origin: migrated from legacy ledger ("Deferred from: code review of 11-19-doc-comment-enforcement-realignment (2026-08-02)"), 2026-08-27
location: tests/**.cs
source_spec: `_bmad-output/implementation-artifacts/11-19-doc-comment-enforcement-realignment.md`
reason: summary: CA1707 EditorConfig suppressions for `tests/**.cs` and `FcDiagnosticIds.cs` appear in the 11.19a baseline-to-HEAD delta but were landed later in `67154049` (analyzer-policy / Story 11.20 surface), not the CS1591 realignment commit. evidence: `.editorconfig` CA1707 blocks between CS1591 default and freeze scopes. Reopen trigger: 11.20/AnalyzerPolicyGovernance owns or rejects those suppressions explicitly.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/11-20-recommended-analyzer-policy-and-exception-ledger.md:130 explicitly owns the exact CA1707 test and FcDiagnosticIds scopes.

### DW-1742: AD-13/AD-15 production wiring landed 2026-08-08 (`create-ci`/`create-release`/`verify-ci`/`verify-release` in `ci.yml`/`release.yml`/`release-evidence.yml`); sealed handoff emission and release eligibility remain blocked until AD-16 supplies an owner-accepted immutable Builds revision and non-empty `evaluator_authorizations`.

origin: migrated from legacy ledger ("Deferred from: GOV-1 story reconciliation adversarial review (2026-08-04)"), 2026-08-27
location: ci.yml
source_spec: `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md`
reason: summary: AD-13/AD-15 production wiring landed 2026-08-08 (`create-ci`/`create-release`/`verify-ci`/`verify-release` in `ci.yml`/`release.yml`/`release-evidence.yml`); sealed handoff emission and release eligibility remain blocked until AD-16 supplies an owner-accepted immutable Builds revision and non-empty `evaluator_authorizations`. evidence: Option (b) from the 2026-08-04 entry was implemented: workflows now call `create_ci_handoff`/`create_release_handoff`. With empty `evaluator_authorizations` and `domain-ci.yml@main`, `create-ci`/`create-release` exit 2 (deferred diagnostic) and do not upload sealed handoffs; `release.yml` fails closed without an AD-13 artifact. Reopen trigger: Release Owner reopens Hexalith.Builds issue 17 (or successor), records a qualifying 40-hex revision, FrontComposer pins CI/release reusable+action closures, populates `eng/dependency-graph-policy.json` `evaluator_authorizations`, and proves end-to-end sealed handoff emission.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 7b3d8f3b

### DW-1743: Restore Nightly Quality LLM benchmark budget gate (missing .github/benchmark-budget.json) from actions run 30978011661

origin: migrated from legacy ledger ("Deferred from: GOV-1 story reconciliation adversarial review (2026-08-04)"), 2026-08-27
location: .github/benchmark-budget.json
source_spec: none
reason: summary: Restore Nightly Quality LLM benchmark budget gate (missing .github/benchmark-budget.json) from actions run 30978011661 evidence: Split from dual-CI fix intent; user chose [S] to tackle SourceTools mutation failure first and defer the LLM budget gate
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 3405a91d

### DW-1746: After BUILD-REL-1 merges, bump domain-ci.yml self-pins of Builds composites from baseline 824d7ef to the merged commit that contains the changed dapr-init/governed-provenance bytes.

origin: migrated from legacy ledger ("BUILD-REL-1 review deferrals (2026-08-05)"), 2026-08-27
location: domain-ci.yml
source_spec: `_bmad-output/implementation-artifacts/spec-build-rel-1-governed-nuget-release-contract.md`
reason: summary: After BUILD-REL-1 merges, bump domain-ci.yml self-pins of Builds composites from baseline 824d7ef to the merged commit that contains the changed dapr-init/governed-provenance bytes. evidence: Pins necessarily target the pre-change commit while this PR modifies those composites; consumers otherwise execute pre-change action bytes until a follow-up pin bump.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: BUILD-REL-1 has landed and the current Builds workflow no longer uses baseline 824d7ef: references/Hexalith.Builds/.github/workflows/domain-ci.yml:318,397,480,498,554,572 pins initialize-build/dapr-init to 5c3ff35c590cfae9f3a9784b75d08dd065c55cef with BUILD-REL-1 provenance.

### DW-1750: Online audit evidence is dated 2026-07-16 with no fresh re-baseline before done — discharged 2026-08-07: `dotnet list src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj package --include-transitive --vulnerable` reported no vulnerable packages against nuget.org at done transition.

origin: migrated from legacy ledger ("Deferred from: code review of 11-19-apphost-nuget-audit-suppression.md (2026-08-07)"), 2026-08-27
location: src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj
reason: Online audit evidence is dated 2026-07-16 with no fresh re-baseline before done — **discharged 2026-08-07**: `dotnet list src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj package --include-transitive --vulnerable` reported no vulnerable packages against nuget.org at done transition.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 84273bac

### DW-1756: `docs/diagnostics/HFC1002.md` still tells readers to prefer `.editorconfig` or a pragma and does not document the property-level `[SuppressMessage]` mechanism the samples now depend on.

origin: migrated from legacy ledger ("Deferred from: build review of 11-20-recommended-analyzer-policy-and-exception-ledger.md (2026-08-07)"), 2026-08-27
location: docs/diagnostics/HFC1002.md
source_spec: `_bmad-output/implementation-artifacts/11-20-recommended-analyzer-policy-and-exception-ledger.md`
reason: summary: `docs/diagnostics/HFC1002.md` still tells readers to prefer `.editorconfig` or a pragma and does not document the property-level `[SuppressMessage]` mechanism the samples now depend on. evidence: HFC1002 is generator-reported, so `.editorconfig` severity does not apply to it. `docs/` is the published CI-gated DocFX site (Gate 2d), so the edit belongs in a story that owns a docs change.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: docs/diagnostics/HFC1002.md:46-53 documents the broad-suppression escape hatch and its governance requirements.

### DW-1758: Ledger `diagnosticIds` is overloaded to carry MSBuild property values, so the root-CA guard runs over a field with two incompatible meanings.

origin: migrated from legacy ledger ("Deferred from: build review of 11-20-recommended-analyzer-policy-and-exception-ledger.md (2026-08-07)"), 2026-08-27
location: analyzer-policy-exception-ledger-v1.json
source_spec: `_bmad-output/implementation-artifacts/11-20-recommended-analyzer-policy-and-exception-ledger.md`
reason: summary: Ledger `diagnosticIds` is overloaded to carry MSBuild property values, so the root-CA guard runs over a field with two incompatible meanings. evidence: `analyzer-policy-exception-ledger-v1.json` encodes `"property": "TreatWarningsAsErrors", "diagnosticIds": ["true"]`. Splitting into `diagnosticIds` and `propertyValue` is a schema change requiring a coordinated reseal.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: Commit 1610415e; _bmad-output/contracts/analyzer-policy-exception-ledger-v1.json:453-470 represents scalar MSBuild settings as propertyValue, guarded by AnalyzerPolicyGovernanceTests.cs:804-878.
decision: 2026-08-27 Implement requested change — Implement the behavior requested by DW-1758, update affected contracts and consumers, and add focused regression evidence.

### DW-1761: The three compile specimens omit `--no-restore`, carry no timeouts, and use inconsistent flag sets, although Gate 2b runs them inside a `--no-build` test job.

origin: migrated from legacy ledger ("Deferred from: build review of 11-20-recommended-analyzer-policy-and-exception-ledger.md (2026-08-07)"), 2026-08-27
location: quality.yml
source_spec: `_bmad-output/implementation-artifacts/11-20-recommended-analyzer-policy-and-exception-ledger.md`
reason: summary: The three compile specimens omit `--no-restore`, carry no timeouts, and use inconsistent flag sets, although Gate 2b runs them inside a `--no-build` test job. evidence: `quality.yml` Gate 2b runs `dotnet test … --no-build --filter "Category=Governance"`; the first synthetic build needs NuGet feed access from the test process, so an offline agent reports a network failure as an analyzer-policy failure.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: Commit 1610415e; tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:2253-2299 applies --no-restore --no-incremental -m:1 /nr:false to every compile specimen through the bounded runner.

### DW-1771: `SemaphoreSlim.Dispose()` in `ETagCacheService` can strand a caller already awaiting `WaitAsync` rather than faulting it.

origin: migrated from legacy ledger ("Deferred from: build review of 11-20-recommended-analyzer-policy-and-exception-ledger.md (2026-08-07)"), 2026-08-27
location: ETagCacheService.cs:73-79
source_spec: `_bmad-output/implementation-artifacts/11-21-recommended-analyzer-product-and-generator-burndown.md`
reason: summary: `SemaphoreSlim.Dispose()` in `ETagCacheService` can strand a caller already awaiting `WaitAsync` rather than faulting it. evidence: `ETagCacheService.cs:73-79,324-356`. The added `catch (ObjectDisposedException)` covers only an already-disposed gate at call time; `SemaphoreSlim.Dispose` is documented as unsafe with pending waiters, so a seed in flight at circuit teardown is not guaranteed to observe the exception. A `CancellationTokenSource` cancelled in `Dispose()` and linked into `WaitAsync` would fault queued waiters deterministically. Story 11.21 added dispose tests but did not change the synchronisation primitive.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: commit 9ad4312f; src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:68-77 intentionally leaves the managed semaphore undisposed so queued waiters settle safely.

### DW-1774: `Counter.Web` and `Counter.Specimens` had their ASP0006 `NoWarn` removed but are not part of any asserted zero-ASP0006 consumer set, so a regression reaching only those consumers would be ungated.

origin: migrated from legacy ledger ("Deferred from: build review of 11-20-recommended-analyzer-policy-and-exception-ledger.md (2026-08-07)"), 2026-08-27
location: Counter.Web
source_spec: `_bmad-output/implementation-artifacts/11-21-recommended-analyzer-product-and-generator-burndown.md`
reason: summary: `Counter.Web` and `Counter.Specimens` had their ASP0006 `NoWarn` removed but are not part of any asserted zero-ASP0006 consumer set, so a regression reaching only those consumers would be ungated. evidence: `PackagedAnalyzerConsumerTests` asserts over its own generated temp consumer; the negative control for these two projects was run manually during Story 11.21 and is not encoded as a test.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: Commit 1610415e; tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:68-83 includes Counter.Specimens and Counter.Web in the strict set, with warning-free builds enforced at :539-552.

### DW-1780: Rename artifact file away from caller-enforcement title if desired (`rel-4-enforce-temporary-release-freeze.md` vs Builds-pin supersession).

origin: migrated from legacy ledger ("Deferred from: REL-4 supersession token split [S] (2026-08-09)"), 2026-08-27
location: rel-4-enforce-temporary-release-freeze.md
source_spec: `_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md`
reason: summary: Rename artifact file away from caller-enforcement title if desired (`rel-4-enforce-temporary-release-freeze.md` vs Builds-pin supersession). evidence: Review blind-hunter 2026-08-09; cosmetic path naming, not an acceptance failure.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current verified behavior as intentional and close the deferred row.
decision: 2026-08-27 Accept current behavior — Record the current verified behavior as intentional and close the deferred row.

### DW-1781: Maintain the full historical REL-3 epic body (amended ACs, Implementation Record, 2026-07-18 review findings) as an active implementer surface.

origin: migrated from legacy ledger ("Deferred from: REL-3 residual token split [S] (2026-08-09)"), 2026-08-27
location: rel-3-enforce-fr24-pre-publish-and-reconcile-releases.history.md
source_spec: `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`
reason: summary: Maintain the full historical REL-3 epic body (amended ACs, Implementation Record, 2026-07-18 review findings) as an active implementer surface. evidence: Split under token gate [S]; archived to `rel-3-enforce-fr24-pre-publish-and-reconcile-releases.history.md` so the active plan stays T8-only.
status: done 2026-08-27
archived: 2026-09-18
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-1783: GOV-1 / BUILD-REL-1 accepted immutable Builds revision and evaluator authorization closure.

origin: migrated from legacy ledger ("Deferred from: REL-3 residual token split [S] (2026-08-09)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`
reason: summary: GOV-1 / BUILD-REL-1 accepted immutable Builds revision and evaluator authorization closure. evidence: Split from residual review plan; upstream acceptance is a transferred GOV-1 condition, not a T8 workflow patch.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 7b3d8f3b

### DW-1787: EventStore and Release owners publish a retrievable replacement source/package identity with exact hashes and a matching Builds catalog commit.

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: 999.1.20-proof.fa2d1c9910f8
source_spec: `_bmad-output/implementation-artifacts/spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md`
reason: summary: EventStore and Release owners publish a retrievable replacement source/package identity with exact hashes and a matching Builds catalog commit. evidence: Story 1.20's approved `999.1.20-proof.fa2d1c9910f8` archives are unrecoverable, current source/package identities differ, and no FrontComposer-scoped replacement authority exists.
status: done 2026-08-29
archived: 2026-09-18
resolution: already resolved: commit 16996eb5 added the retrievable owner-approved EventStore bb94d93e / 3.91.1 tuple, exact hashes, and Builds a8a50859 identity

### DW-1788: EventStore supplies a real-loopback Pact provider verifier and separately reconciles the 19 FrontComposer consumer interactions with the provider wire contract.

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md`
reason: summary: EventStore supplies a real-loopback Pact provider verifier and separately reconciles the 19 FrontComposer consumer interactions with the provider wire contract. evidence: Neither approved nor current EventStore source contains the required provider-test project, while committed pacts conflict with real query envelopes, ETags, headers, and provider-state inputs.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: Post-baseline commits cd1a01d3 and e29b37f8 supplied and refreshed the real-loopback provider proof. _bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/provider-verification.json:2-22 reports live-compatibility, passed, 19 requested/reported interactions, 19 setup/teardown events, complete Kestrel loopback execution, and clean shutdown.
decision: 2026-08-29 Approve reconciliation — Reconcile consumers, pacts, adapters, and loopback/AppHost evidence against the approved provider contract.
decision: 2026-08-29 Approve reconciliation — Reconcile consumers, pacts, adapters, and loopback/AppHost evidence against the approved provider contract.

### DW-1792: Bind package-audit provenance to an existing Git revision whose catalog blob matches the audited selections.

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-bump-eventstore-package-to-3-93-0.md`
reason: summary: Bind package-audit provenance to an existing Git revision whose catalog blob matches the audited selections. evidence: The existing validator accepts any 40-character lowercase hexadecimal value and does not verify that `generatedFromRevision` exists or contains the audited catalog.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: commit bc31f369; references/Hexalith.Builds/Tools/validate-package-version-audit.ps1:1280 verifies the revision, ancestry, and exact catalog blob.

### DW-1794: Add a durable Release-mode assertion for the exact EventStore Aspire package identity and absence of its source-project edge.

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: Hexalith.EventStore.Aspire/3.93.0
source_spec: `_bmad-output/implementation-artifacts/spec-bump-eventstore-package-to-3-93-0.md`
reason: summary: Add a durable Release-mode assertion for the exact EventStore Aspire package identity and absence of its source-project edge. evidence: This bump proved `Hexalith.EventStore.Aspire/3.93.0` through an isolated restore and asset inspection, but the existing CI governance tests do not assert the resolved package version.
status: done 2026-08-29
archived: 2026-09-18
resolution: already resolved: commit 16996eb5 added exact Release EventStore package-identity and no-source-project-edge governance evidence

### DW-1797: Fix Hexalith.Memories integration-fast failures in AccessTelemetry Aspire Dapr clock sidecar startup and OpenBao sidecar secret readiness (CI run 31719611315).

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: n/a
source_spec: none
reason: summary: Fix Hexalith.Memories integration-fast failures in AccessTelemetry Aspire Dapr clock sidecar startup and OpenBao sidecar secret readiness (CI run 31719611315). evidence: Added from https://github.com/Hexalith/Hexalith.Memories/actions/runs/31719611315 during FrontComposer CA1707 seal planning; independently shippable in Hexalith.Memories and out of scope for FrontComposer ledger reseal.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: references/Hexalith.Memories commits f3227b1b and 1d9e9c89 restored AccessTelemetry coverage and stabilized OpenBao integration-fast readiness.

### DW-1798: Make FC-NIP Playwright contract guards cross-platform by fixing PLAYWRIGHT_SKIP_WEBSERVER env syntax that fails on Windows runners.

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: n/a
source_spec: none
reason: summary: Make FC-NIP Playwright contract guards cross-platform by fixing PLAYWRIGHT_SKIP_WEBSERVER env syntax that fails on Windows runners. evidence: Split from CI run 31715693323 because the accessibility-visual Windows npm failure is independently shippable from the Linux Gate 2b CA1707 identifier-inventory seal drift.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: Commit d08d1dec; tests/e2e/package.json:31-36 uses cross-env for PLAYWRIGHT_SKIP_WEBSERVER and the dependency is locked.

### DW-1800: Quality Gate 2b on current main fails CentralPackageVersions catalog inheritance and ReleaseWorkflow Builds gitlink SHA checks.

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds
source_spec: `_bmad-output/implementation-artifacts/spec-actions-31779965137-fix-cicd.md`
reason: summary: Quality Gate 2b on current main fails CentralPackageVersions catalog inheritance and ReleaseWorkflow Builds gitlink SHA checks. evidence: Quality run 31781816546 on `4ccd7727` failed `InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` and `CiGovernanceTests.ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate`; those Facts are outside this inventory-seal confirm.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: Current focused execution passes both DW-1800 facts defined in tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs:36-55 and CiGovernanceTests.cs:1157.

### DW-1801: Re-dispatch Release on a later release-ready main tip after remaining Gate 2b failures are fixed.

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: release.yml
source_spec: `_bmad-output/implementation-artifacts/spec-actions-31779965137-fix-cicd.md`
reason: summary: Re-dispatch Release on a later release-ready main tip after remaining Gate 2b failures are fixed. evidence: Failed Release 31779965137 targeted ancestor SHA `d31679c1` and cannot turn green; `release.yml` prepare requires live main, and current tip still fails other Gate 2b Facts.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: Tag v4.2.0 at commit df689935 and later tag v4.3.0 at commit 155f25aa prove Release was re-dispatched successfully on later release-ready main tips.

### DW-1802: Document that the first push/PR after a semantic-policy change still evaluates under the event-base exact-pin policy (AD-10 delayed activation).

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: System.CommandLine
source_spec: `_bmad-output/implementation-artifacts/spec-actions-31783283241-fix-cicd.md`
reason: summary: Document that the first push/PR after a semantic-policy change still evaluates under the event-base exact-pin policy (AD-10 delayed activation). evidence: `diff` loads policy at event-base; current main already fails EventStore `System.CommandLine` `2.0.10` vs `2.0.11`, so the landing change can stay red once until the new presence-only policy is the push base.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: _bmad-output/project-context.md:281-286 explicitly documents event-base delayed activation and the two-phase preauthorization policy; commits 54edc44f and dfbe9978.

### DW-1803: Amend FC-DEP-1 / architecture spine so sibling catalogs are presence-only instead of exact required-package version fail-closed.

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-actions-31783283241-fix-cicd.md`
reason: summary: Amend FC-DEP-1 / architecture spine so sibling catalogs are presence-only instead of exact required-package version fail-closed. evidence: Planning architecture still says a changed required package version fails closed for every selected catalog; only project-context was updated.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: _bmad-output/project-docs/architecture.md:363-367 now defines sibling catalogs as presence-only rather than exact-version fail-closed.
decision: 2026-08-27 Implement change — Implement the behavior requested by DW-1803, update affected contracts and consumers, and add focused regression evidence.

### DW-1805: Align reusable Release workflow pins with current Builds HEAD `606d9f1` (tests still expect `99d5a46`).

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: CiGovernanceTests.ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate
source_spec: `_bmad-output/implementation-artifacts/spec-actions-29643539939-fix-cicd.md`
reason: summary: Align reusable Release workflow pins with current Builds HEAD `606d9f1` (tests still expect `99d5a46`). evidence: `CiGovernanceTests.ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` fails on later main; this restore-close did not change workflows or gitlinks.
status: done 2026-08-29
archived: 2026-09-18
resolution: already resolved: commit 16996eb5 separated the catalog gitlink from the immutable Builds execution coordinate while retaining execution-coordinate lockstep

### DW-1806: Reseal the analyzer-policy identifier inventory for later-main drift (`6824` / `af8a8d24…` vs sealed `6820` / `6c099739…`).

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: AnalyzerPolicyGovernanceTests.AnalyzerPolicy_IdentifierInventory_MatchesSeal
source_spec: `_bmad-output/implementation-artifacts/spec-actions-29643539939-fix-cicd.md`
reason: summary: Reseal the analyzer-policy identifier inventory for later-main drift (`6824` / `af8a8d24…` vs sealed `6820` / `6c099739…`). evidence: `AnalyzerPolicyGovernanceTests.AnalyzerPolicy_IdentifierInventory_MatchesSeal` fails on later main; the July `6188` ledger refresh in this spec is historical and was not re-opened.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 9e212f17

### DW-1807: RESOLVED 2026-08-14 — Refresh Builds audit selectedVersion rows for the four Dependabot-advanced external packages so validate-package-version-audit.ps1 can pass.

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: validate-package-version-audit.ps1
source_spec: `_bmad-output/implementation-artifacts/spec-bump-latest-hexalith-nuget-packages.md`
reason: summary: RESOLVED 2026-08-14 — Refresh Builds audit selectedVersion rows for the four Dependabot-advanced external packages so validate-package-version-audit.ps1 can pass. evidence: Builds `58987900cff1e1f67c7f66966023789a104bc349` sets Roslynator, SonarAnalyzer, and System.CommandLine audit floors to the already-landed catalog pins; `validate-package-version-audit.ps1` now passes.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: Hexalith.Builds commit 5898790

### DW-1808: RESOLVED 2026-08-14 — Land evaluator_authorizations for 3f0e359 before the workflow pin-move commit so AD-13 create-ci is authorized on the pin push.

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-bump-latest-hexalith-nuget-packages.md`
reason: summary: RESOLVED 2026-08-14 — Land evaluator_authorizations for 3f0e359 before the workflow pin-move commit so AD-13 create-ci is authorized on the pin push. evidence: Combined pin+policy commit `e787690f` is already on origin/main, so that push could only soft-defer. The follow-up commit keeps workflow blobs unchanged while `e787690f` is the push base; `draft-evaluator` for ci/release/post_release reports `authorized_draft: true` against that policy.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 019ea221

### DW-1810: Close or rewrite leftover `spec-bump-latest-hexalith-nuget-packages-2.md`, which still requires moving every execution pin onto the catalog gitlink.

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: spec-bump-latest-hexalith-nuget-packages-2.md
source_spec: `_bmad-output/implementation-artifacts/spec-split-builds-catalog-gitlink-from-ci-cd-execution-sha.md`
reason: summary: Close or rewrite leftover `spec-bump-latest-hexalith-nuget-packages-2.md`, which still requires moving every execution pin onto the catalog gitlink. evidence: That in-progress spec's frozen boundaries still bind catalog gitlink and execution SHA; this correction forbids completing that lockstep rewrite, but the leftover spec can restore it if resumed.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-1811: Reconcile the production Release handoff/evaluator path with the v3 source-provenance manifest schema and its living documentation.

origin: migrated from legacy ledger ("Deferred from: Story 11.24 owner decision (2026-08-10)"), 2026-08-27
location: .github/workflows/release.yml
source_spec: `_bmad-output/implementation-artifacts/spec-split-builds-catalog-gitlink-from-ci-cd-execution-sha.md`
reason: summary: Reconcile the production Release handoff/evaluator path with the v3 source-provenance manifest schema and its living documentation. evidence: `.github/workflows/release.yml` supplies `DEPENDENCY_RELEASE_HANDOFF` plus `RELEASE_EVALUATOR`, so `eng/release_evidence.py` builds evaluator-style provenance while labeling the manifest `hexalith.release-evidence.v3`; the v3 validator expects source-proof provenance and the current focused success fixture exercises only `--source-proof`.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: Commits df689935, 10d61c4f, and c8b31f12; eng/release_evidence.py:33-65 now projects recovered post-release CI handoff into the v3 exact-source schema.

### DW-1815: Add executable commitlint boundary coverage proving 200-character header, body, and footer lines pass while 201-character lines fail.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-converge-terminal-outcomes-on-one-producer-boundary (2026-08-16)"), 2026-08-27
location: commitlint.config.mjs
source_spec: `_bmad-output/implementation-artifacts/spec-9-4-converge-terminal-outcomes-on-one-producer-boundary.md`
reason: summary: Add executable commitlint boundary coverage proving 200-character header, body, and footer lines pass while 201-character lines fail. evidence: `commitlint.config.mjs` sets all three limits to 200, but no repository test invokes the pinned CLI at both boundary values.
status: done 2026-08-29
archived: 2026-09-18
resolution: already resolved: commit b8c8f01a added executable pinned-commitlint 200/201 boundary cases for header, body, and footer lines

### DW-1816: Make the nightly LLM benchmark gate reject missing, failed, zero-run, or otherwise invalid benchmark results before accepting candidate evidence.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-converge-terminal-outcomes-on-one-producer-boundary (2026-08-16)"), 2026-08-27
location: eng/llm_benchmark.py
source_spec: `_bmad-output/implementation-artifacts/spec-9-4-converge-terminal-outcomes-on-one-producer-boundary.md`
reason: summary: Make the nightly LLM benchmark gate reject missing, failed, zero-run, or otherwise invalid benchmark results before accepting candidate evidence. evidence: Review of `eng/llm_benchmark.py` and `eng/release_prepublish.py` found that artifact presence can satisfy the handoff without proving a successful non-empty benchmark run.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/llm_benchmark.py:1

### DW-1826: Replace substring-based trigger detection with exact structured matching so incidental prose cannot satisfy trigger expectations.

origin: migrated from legacy ledger ("Deferred from: code review of 9-4-converge-terminal-outcomes-on-one-producer-boundary (2026-08-16)"), 2026-08-27
location: .agents/skills/bmad-eval-runner/scripts/run_triggers.py
source_spec: `_bmad-output/implementation-artifacts/spec-9-4-converge-terminal-outcomes-on-one-producer-boundary.md`
reason: summary: Replace substring-based trigger detection with exact structured matching so incidental prose cannot satisfy trigger expectations. evidence: `.agents/skills/bmad-eval-runner/scripts/run_triggers.py` can classify a query as triggered when the skill name appears only as an unrelated substring.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: .agents/skills/bmad-eval-runner/scripts/run_triggers.py:209-258 now evaluates trigger queries through the intended runner seam.

### DW-1838: Bind package-audit provenance to the catalog commit it claims to describe and distinguish incremental family refreshes from complete live snapshots.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-6-enforce-atomic-per-row-first-wins.md (2026-08-18)"), 2026-08-27
location: validate-package-version-audit.ps1
source_spec: `_bmad-output/implementation-artifacts/spec-bump-eventstore-to-3-97-0.md`
reason: summary: Bind package-audit provenance to the catalog commit it claims to describe and distinguish incremental family refreshes from complete live snapshots. evidence: `validate-package-version-audit.ps1` accepts any lowercase 40-character `generatedFromRevision` without proving that the commit exists or that its catalog blob matches the audited selections, while the audit exposes only artifact-wide timestamp and revision fields for incrementally preserved package decisions.
status: done 2026-09-01
archived: 2026-09-18
resolution: resolved by sweep bundle dw-package-audit-provenance-bom
resolution-undo: 7f557be52a6c44515f315eb53777d6fb25470b175455ff165942faa55a32b52e 2026-09-01 7374617475733a206f70656e

### DW-1839: Add a Builds-owned validation gate that enforces the required UTF-8 BOM on the central package catalog.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-6-enforce-atomic-per-row-first-wins.md (2026-08-18)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-bump-eventstore-to-3-97-0.md`
reason: summary: Add a Builds-owned validation gate that enforces the required UTF-8 BOM on the central package catalog. evidence: `.gitattributes` enforces CRLF but not the BOM, so pushed catalog commit `761dc0187ef60599f12310fef2411dbaf0206742` removed the marker without a Builds gate failing; this bump repaired the committed bytes but not the systemic regression path.
status: done 2026-09-01
archived: 2026-09-18
resolution: resolved by sweep bundle dw-package-audit-provenance-bom
resolution-undo: 7f557be52a6c44515f315eb53777d6fb25470b175455ff165942faa55a32b52e 2026-09-01 7374617475733a206f70656e

### DW-1841: Add package-mode coverage that constructs and verifies the FrontComposer Aspire application model.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-6-enforce-atomic-per-row-first-wins.md (2026-08-18)"), 2026-08-27
location: Hexalith.EventStore.Aspire/3.97.0
source_spec: `_bmad-output/implementation-artifacts/spec-bump-eventstore-to-3-97-0.md`
reason: summary: Add package-mode coverage that constructs and verifies the FrontComposer Aspire application model. evidence: Current checks restore and compile `Hexalith.EventStore.Aspire/3.97.0` but do not execute its topology extensions and assert the EventStore, admin, domain-module, DAPR component, reference, and wait-edge model produced for FrontComposer.
status: done 2026-09-06
archived: 2026-09-18
resolution: already resolved: Commit 16996eb5 added the package-mode AppHost runtime-evidence model and tests; eng/eventstore_runtime_evidence.py:1370-1450 now validates the exact Release AppHost restore/package graph and the authenticated smoke constructs the topology.

### DW-1847: Reconcile the checked-in Fluent UI rc.5 catalog with stale Shell/UI expectations and generated rendering tests.

origin: migrated from legacy ledger ("Deferred from: code review of spec-fix-current-release-compatibility-gates (2026-08-25)"), 2026-08-27
location: 5.0.0-rc.5-26219.1
source_spec: `_bmad-output/implementation-artifacts/spec-fix-current-release-compatibility-gates.md`
reason: summary: Reconcile the checked-in Fluent UI rc.5 catalog with stale Shell/UI expectations and generated rendering tests. evidence: The non-publishing candidate built and packed successfully, then the unchanged full Shell suite failed 16 of 2,648 tests against the already-selected `5.0.0-rc.5-26219.1`, including the rc.4 conformance pin, generated rendering assertions, and analyzer inventory seal.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: Commit 79d74ba14730501fe835cb5f57e5bfc7faf9ad04 reconciled the rc.5 expectations and generated visual snapshots. Current Contracts.UI and Testing package-boundary tests pin 5.0.0-rc.5-26219.1 at PackageBoundaryTests.cs:15 and :13 respectively; no rc.4 expectation remains in tests or src.

### DW-1850: Apply `--exclude` patterns to committed paths, not only workspace paths.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-7-add-story-id-and-commit-scope-evidence (2026-08-25)"), 2026-08-27
location: docs/_site
source_spec: `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
reason: summary: Apply `--exclude` patterns to committed paths, not only workspace paths. evidence: `collect_reconciled_changed_files` filters workspace paths through `is_excluded`, while `unowned_paths` in `collect_commit_scope_evidence` is computed with no exclude filtering. A story-matching commit that contains a default-excluded path (build output, `docs/_site`) is classified `interleaved` and hard-fails, diverging from the documented `--exclude` contract and from legacy mode.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: docs/_site/xrefmap.yml:1

### DW-1851: Cross-check an explicit `story_id` against the title, H1, and filename identities.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-7-add-story-id-and-commit-scope-evidence (2026-08-25)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
reason: summary: Cross-check an explicit `story_id` against the title, H1, and filename identities. evidence: `extract_story_id` returns immediately when frontmatter carries a value, so conflicting legacy identities are detected only when frontmatter is absent. A mistyped or copy-pasted `story_id` silently redirects the entire ownership gate with no conflict reported.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:1

### DW-1852: Harden commit-log decoding and report rendering against non-UTF-8 bytes.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-7-add-story-id-and-commit-scope-evidence (2026-08-25)"), 2026-08-27
location: UnicodeDecodeError
source_spec: `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
reason: summary: Harden commit-log decoding and report rendering against non-UTF-8 bytes. evidence: `run_git_checked` reads the commit log with `text=True`, so a non-UTF-8 subject raises an uncaught `UnicodeDecodeError` rather than a validation failure. `decode_nul_paths` uses `errors="surrogateescape"` and those strings reach `print()` in `format_commit_scope_evidence`, where a strict stdout handler raises `UnicodeEncodeError`. Path parsing was hardened; log decoding and rendering were not.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:1

### DW-1858: Detect concurrent workspace changes while strict story evidence is being collected.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-7-add-story-id-and-commit-scope-evidence (2026-08-25)"), 2026-08-27
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
reason: summary: Detect concurrent workspace changes while strict story evidence is being collected. evidence: Candidate movement is checked after collection, but workspace porcelain is sampled only once. A file staged, modified, created, or resolved after that snapshot can be absent from the final report even though the report describes current workspace state.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:1

### DW-1860: Remove stale non-package files before sealing a release candidate directory.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-7-add-story-id-and-commit-scope-evidence (2026-08-25)"), 2026-08-27
location: scripts/pack-release-packages.py
source_spec: `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
reason: summary: Remove stale non-package files before sealing a release candidate directory. evidence: `scripts/pack-release-packages.py` deletes only root-level `*.nupkg` and `*.snupkg`, while `eng/release_prepublish.py` seals every remaining file recursively. A reused output directory can therefore carry stale logs or metadata into release evidence.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: scripts/pack-release-packages.py:1

### DW-1864: Refresh authoritative FrontComposer context documents to the selected Fluent UI rc.5 pin.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-7-add-story-id-and-commit-scope-evidence (2026-08-25)"), 2026-08-27
location: _bmad-output/project-context.md
source_spec: `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
reason: summary: Refresh authoritative FrontComposer context documents to the selected Fluent UI rc.5 pin. evidence: The selected Builds catalog is `5.0.0-rc.5-26219.1`, but `_bmad-output/project-context.md` and `_bmad-output/project-docs/project-overview.md` still call rc.4 the exact authoritative pin, directing future work toward stale component behavior.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: Commit 79d74ba14730501fe835cb5f57e5bfc7faf9ad04 refreshed both authoritative context surfaces. _bmad-output/project-context.md:39-42 and _bmad-output/project-docs/project-overview.md:38-45 now identify Fluent UI 5.0.0-rc.5-26219.1 as the exact selected pin.

### DW-1869: Verify that each candidate package's primary DLL has the expected assembly identity.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-7-add-story-id-and-commit-scope-evidence (2026-08-25, loop 5)"), 2026-08-27
location: eng/verify-candidate-packages.cs
source_spec: `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
reason: summary: Verify that each candidate package's primary DLL has the expected assembly identity. evidence: `eng/verify-candidate-packages.cs` selects entries by the expected DLL filename and checks their versions, but it does not assert that `AssemblyName.GetAssemblyName(...).Name` matches the package identity. A substituted, correctly named assembly with matching version metadata can therefore pass this release-compatibility verifier. Owned by the release-compatibility work declared `shared`, not by Story 9.7.
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/verify-candidate-packages.cs:1

### DW-1873: Gate story completion on the mechanical commit-scope report by CI machinery, not workflow convention.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-7-add-story-id-and-commit-scope-evidence (2026-08-25, loop 6)"), 2026-08-27
location: .github/workflows/
source_spec: `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
reason: summary: Gate story completion on the mechanical commit-scope report by CI machinery, not workflow convention. evidence: `grep -rn "validate-story-artifacts" .github/workflows/` returns nothing. `quality.yml` Gate 2b runs `python3 -m unittest eng.tests.test_validate_story_artifacts`, and `CiGovernanceTests.StoryArtifactValidatorGate_IsBlockingAndExact` pins exactly that command, so CI proves only that the validator is unbroken. The strict gate itself runs only when an agent follows `step-04-review.md`. Epic 9 retro action F9-06/E9-AI-5 asked to gate story completion on the report. A CI job needs a branch-to-spec resolution convention that does not exist (`fix/9-7-story-scope-evidence` maps to `spec-9-7-...` only by coincidence) plus a skip path for non-story branches that would reintroduce skippability. Human decision 2026-08-25: accept workflow enforcement for 9.7, state the boundary in Design Notes, and design CI enforcement in a successor story.
status: done 2026-08-31
archived: 2026-09-18
decision: 2026-08-31 Tracked mapping — Add a versioned branch/change-to-spec mapping contract, fail closed on ambiguous story branches, and run the validator in CI with focused governance tests.
resolution: closed by human decision: Retain step-04 workflow enforcement as the documented completion boundary and close CI automation as intentionally out of scope.
decision: 2026-08-31 Keep agent gate — Retain step-04 workflow enforcement as the documented completion boundary and close CI automation as intentionally out of scope.

### DW-1874: Consider a `misattributed` disposition kind for subject-only false story matches.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-7-add-story-id-and-commit-scope-evidence (2026-08-25, loop 6)"), 2026-08-27
location: story_id_pattern("9.7")
source_spec: `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
reason: summary: Consider a `misattributed` disposition kind for subject-only false story matches. evidence: `story_id_pattern("9.7")` matches any bare occurrence of the canonical ID, so `Revert "fix(9.7): ..."`, `backport of 9.7`, or `see 9.7 for context` makes an unrelated commit story-matching. Because `if matches and unowned_paths` precedes `elif disposition` and the frozen Boundaries forbid a disposition from suppressing `interleaved`, such a commit hard-fails the gate with no escape short of rewriting a published subject. The trigger is often outside the author control (`git revert` and the GitHub revert button generate the subject). Human decision 2026-08-25: document the trap rather than reopen the frozen block, since it has not yet fired and loop-3 anti-broadening work depends on `interleaved` staying unsuppressable.
status: done 2026-08-27
archived: 2026-09-18
resolution: closed by human decision: Record the current behavior as intentional and close the deferred row with its verified rationale.
decision: 2026-08-27 Accept current behavior — Record the current behavior as intentional and close the deferred row with its verified rationale.

### DW-1880: Derive the bootstrap story path inside the immutable path set from its constant.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-7-add-story-id-and-commit-scope-evidence (2026-08-25, loop 8)"), 2026-08-27
location: eng/validate-story-artifacts.py:253
source_spec: `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
reason: summary: Derive the bootstrap story path inside the immutable path set from its constant. evidence: `BOOTSTRAP_OWNED_STORY_PATH` is repeated as a raw string literal inside `BOOTSTRAP_OWNED_PATHS` while the two guard paths correctly use their constants; the copies can drift. `eng/validate-story-artifacts.py:253`
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: eng/validate-story-artifacts.py:253

### DW-1890: Retire ledger entries that later loops resolved.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-7-add-story-id-and-commit-scope-evidence (2026-08-25, loop 9)"), 2026-08-27
location: _bmad-output/implementation-artifacts/deferred-work.md
source_spec: `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
reason: summary: Retire ledger entries that later loops resolved. evidence: Entries filed from loop-4 review remain `status: open` although later loops implemented them, so `bmad-loop-sweep` re-triages resolved work every run. Retiring them requires verifying each entry against current source, which is the sweep's own job rather than a review patch inside this story. `_bmad-output/implementation-artifacts/deferred-work.md`
status: done 2026-08-27
archived: 2026-09-18
resolution: already resolved: commit 38af1160 verified current source and closed stale open ledger entries with concrete file/commit resolutions, implementing the retirement requested by this meta-row.

### DW-1896: `ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` (`tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:812`) has been red in every Story 9.7 iteration's Test Evidence, holding the blocking Governance lane at 66/67. It asserts every release Builds coordinate equals the `references/Hexalith.Builds` gitlink resolved by `git ls-tree HEAD`, currently `4eb33928a1d8c7775f97221cf9edc171db0cb5f8` against `22a578b576a515d2af214fe81859447fffc97981`. Under the REL-6 lockstep decision the required lockstep is two-way only (`uses:@<sha>` == `builds-execution-sha`) and the gitlink is deliberately independent because it selects catalog content rather than the release tool, so the assertion may encode the wrong invariant. Owner: Release Owner. Deferred 2026-08-26 (review loop 10): filed for ownership; Story 9.7 changes neither coordinate.

origin: migrated from legacy ledger ("Deferred from: code review of spec-9-7-add-story-id-and-commit-scope-evidence (2026-08-26)"), 2026-08-27
location: tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:812
reason: `ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` (`tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:812`) has been red in every Story 9.7 iteration's Test Evidence, holding the blocking Governance lane at 66/67. It asserts every release Builds coordinate equals the `references/Hexalith.Builds` gitlink resolved by `git ls-tree HEAD`, currently `4eb33928a1d8c7775f97221cf9edc171db0cb5f8` against `22a578b576a515d2af214fe81859447fffc97981`. Under the REL-6 lockstep decision the required lockstep is two-way only (`uses:@<sha>` == `builds-execution-sha`) and the gitlink is deliberately independent because it selects catalog content rather than the release tool, so the assertion may encode the wrong invariant. Owner: Release Owner. Deferred 2026-08-26 (review loop 10): filed for ownership; Story 9.7 changes neither coordinate.
status: done 2026-08-29
archived: 2026-09-18
resolution: already resolved: commit 16996eb5 removed the erroneous release-execution-equals-catalog-gitlink assertion and pins only the approved execution-coordinate lockstep
decision: 2026-08-27 Implement the change — Implement the behavior requested by DW-1896, update affected contracts and consumers, and add focused regression evidence.

### DW-1897: Bind package-audit revision provenance to the exact committed catalog identity.

origin: migrated from legacy ledger ("spec-bump-eventstore-package-to-3-98-0.md"), 2026-08-28
location: references/Hexalith.Builds/Tools/package-version-audit.json:4
source_spec: `_bmad-output/implementation-artifacts/spec-bump-eventstore-package-to-3-98-0.md`
reason: The generated audit records parent Builds revision `8f255570b2df14603a943e8d7ee0c5d3f0b025fc` while its catalog hash binds candidate bytes first committed in `c8837217e6c07f7e12ccf3e3b5e86c5bc83ceade`; validators accept this convention, but the revision alone cannot reproduce the audited catalog.
status: done 2026-09-01
archived: 2026-09-18
resolution: resolved by sweep bundle dw-package-audit-provenance-bom
resolution-undo: 7f557be52a6c44515f315eb53777d6fb25470b175455ff165942faa55a32b52e 2026-09-01 7374617475733a206f70656e

### DW-1898: Prevent catalog-wide audit-history growth for a single-family selector change.

origin: migrated from legacy ledger ("spec-bump-eventstore-package-to-3-98-0.md"), 2026-08-28
location: references/Hexalith.Builds/Tools/audit-central-package-versions.ps1:917
source_spec: `_bmad-output/implementation-artifacts/spec-bump-eventstore-package-to-3-98-0.md`
reason: Refreshing EventStore from `3.97.0` to `3.98.0` caused the generated audit to append preserved history across 140 families and 285 packages, producing 13,241 insertions and 1,447 deletions because preservation is coupled to the complete catalog hash.
status: done 2026-09-01
archived: 2026-09-18
resolution: resolved by sweep bundle dw-package-audit-provenance-bom
resolution-undo: 7f557be52a6c44515f315eb53777d6fb25470b175455ff165942faa55a32b52e 2026-09-01 7374617475733a206f70656e

### DW-1899: The checked-in central package graph blocks the repository's normal .NET test and solution-build lanes before tests execute.
origin: spec-deferred 0a702c5a81ac
location: references/Hexalith.Builds/Props/Directory.Packages.props:318
source_spec: `spec-dw-668-followup-review-11-4-security-validation-hardening.md`
severity: medium
reason: Exact focused-test and solution-build commands fail NU1107 because xunit.v3 4.0.0 resolves xunit.v3.common 4.0.0 while xunit.v3.extensibility.core remains pinned to 3.2.2. Validation-only overlays proved this change, but the unchanged blocking workflow cannot reach those assertions.
status: done 2026-08-28
archived: 2026-09-18
resolution: already resolved: Parent commit 45967719 advances Builds to 569a6e9; references/Hexalith.Builds/Props/Directory.Packages.props:318-320 now aligns xunit.v3, xunit.v3.assert, and xunit.v3.extensibility.core at 3.2.2.

### DW-1900: The blocking Playwright workflow does not execute the settings-persistence Unicode storage-key regression.
origin: spec-deferred fbe23dae8228
location: .github/workflows/quality.yml:481
source_spec: `spec-dw-668-followup-review-11-4-security-validation-hardening.md`
severity: low
reason: The focused serverless Playwright regression passes 1/1, but the blocking workflow typechecks and selects other specs; reverting the helper casing could therefore escape that CI lane.
status: done 2026-09-02
archived: 2026-09-18
resolution: resolved by sweep bundle dw-settings-persistence-ci
resolution-undo: e45ef9c996eefc0ef4c289927a06915fef408c7039d05a09954fbca459ceb28d 2026-09-02 7374617475733a206f70656e

### DW-1901: Reconcile the preserved EventStore provider and live AppHost compatibility failures in separately approved pact/API work.
origin: spec-deferred dbc4b5f12f66
location: _bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/
source_spec: `spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md`
severity: high
reason: The complete 19-interaction provider report truthfully records 16 contract failures and a runtime identity mismatch, while the AppHost smoke records failed runtime observations. The frozen intent explicitly makes these outcomes non-authorizing and routes reconciliation to separate work.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/provider-verification.json:1-12 records finalVerdict passed with all 19 interactions passing.
decision: 2026-08-29 Approve reconciliation — Reconcile consumers, pacts, adapters, and loopback/AppHost evidence against the approved provider contract.
decision: 2026-08-29 Approve reconciliation — Reconcile consumers, pacts, adapters, and loopback/AppHost evidence against the approved provider contract.

### DW-1903: Regain the newer Builds catalog once a newer EventStore runtime identity is owner-approved, and re-assess the package pins this story had to move backwards.
origin: spec-deferred b2312b2406e4
location: eng/dependency-graph-policy.json
source_spec: `spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md`
severity: high
reason: Selecting the owner-approved Builds catalog `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` is required by the frozen intent, but it moves `eng/dependency-graph-policy.json` backwards from the previously selected `449d3643`: ModelContextProtocol.AspNetCore 2.2.0 -> 1.4.1 (major), Verify and Verify.XunitV3 32.0.0 -> 31.27.0, FsCheck.Xunit.v3 3.4.0 -> 3.3.4, Microsoft.Extensions.Localization and System.Collections.Immutable 10.0.11 -> 10.0.10, Microsoft.NET.Test.Sdk 18.9.0 -> 18.8.1, and Fluent UI v5 rc.5 -> rc.4. The Fluent step also regenerated two verified DataGrid snapshots that lost the `display-mode` and `cell-type` attributes and moved `col-justify` onto a class, which is an accessibility-observable DOM change. Nothing in this repository records a forward path back to the newer catalog.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: commit 58197d7c; references/Hexalith.Builds/Props/Directory.Packages.props:8 and :158-315 now carry EventStore 3.102.0 and the restored newer dependency catalog.

### DW-1905: EventStore and Builds gitlink updates currently sit on `fix/cicd-mtp-release` mixed with concurrent CI/MTP files.

origin: migrated from legacy ledger ("spec-bump-eventstore-to-3-100-0.md"), 2026-08-30
location: references/Hexalith.EventStore; references/Hexalith.Builds
source_spec: `_bmad-output/implementation-artifacts/spec-bump-eventstore-to-3-100-0.md`
reason: FrontComposer HEAD remained `f84b68b4e147238f28ca70219f19233d4b4b64d1` on `fix/cicd-mtp-release` with many documented-unrelated CI/MTP paths dirty, so committing the EventStore and Builds gitlink updates there would mix scopes.
status: done 2026-08-31
archived: 2026-09-18
resolution: already resolved: commit 14e2e0d7 integrated the EventStore and Builds 3.100.0 gitlink updates on main, eliminating the mixed fix/cicd-mtp-release branch state.

### DW-1906: Builds `test-package-version-audit-validator.ps1` Git-shim PID check can fail then pass.

origin: migrated from legacy ledger ("spec-bump-eventstore-to-3-100-0.md"), 2026-08-30
location: references/Hexalith.Builds/Tools/test-package-version-audit-validator.ps1
source_spec: `_bmad-output/implementation-artifacts/spec-bump-eventstore-to-3-100-0.md`
reason: The validator failed twice with `The non-terminating Git shim did not record both owned process IDs.` before passing all 66 scenarios on isolated retry; the scripts were not edited by this EventStore bump, so the intermittent Git-shim PID capture remains deferred.
status: done 2026-09-01
archived: 2026-09-18
resolution: resolved by sweep bundle dw-package-audit-provenance-bom
resolution-undo: 7f557be52a6c44515f315eb53777d6fb25470b175455ff165942faa55a32b52e 2026-09-01 7374617475733a206f70656e

### DW-1909: `CiGovernanceTests.EventStoreRuntimeIdentityPinsOwnerApprovedTupleAndTruthfulDriftEvidence` fails on an EventStore gitlink that no longer matches its owner-approved pin.
origin: spec-deferred f37d4440d25e
location: tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:3454
source_spec: `spec-analyzer-governance-reliability.md`
severity: medium
reason: The fact expects `38967215e6c1b13e77f2b0006efd95d88d7ad7b8` but the gitlink is `1194dfe59bcbc9b235390d1e46a7dfe4ee115d94`. That gitlink is byte-identical at this story's baseline `d738598b` and at HEAD, and the story never touched the fact or its pin, so the red is a concurrent EventStore-pin drift owned outside this work.
status: done 2026-09-01
archived: 2026-09-18
resolution: already resolved: Post-baseline commit e29b37f8 reconciled the governance identity. tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:3420-3422 declares current EventStore d6b8d2e5c1763713a126ff627822ead738e0f642 and :3514-3526 asserts both gitlink and checkout match it; git ls-tree HEAD reports that exact gitlink.

### DW-1917: The checked-in audit artifact is not re-derivable by any shipped code path.
origin: spec-deferred 518d2d0f7d27
location: references/Hexalith.Builds/Tools/package-version-audit.json
source_spec: `spec-package-audit-provenance-bom.md`
severity: medium
reason: Tools/package-version-audit.json is snapshot.mode incremental with 4 refreshed and 137 preserved families whose origins were back-filled from the previous v1 audit's global revision and time. That back-fill can only happen on an incremental run over a v1 prior, which the generator now rejects with "incremental refresh requires a schemaVersion 2 prior audit"; a complete refresh would instead stamp all 141 origins with the current revision and time. The artifact is forward-refreshable but cannot be reproduced.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: references/Hexalith.Builds/Tools/package-version-audit.json:2-10 is now a schema-v2 complete snapshot with all 141 families refreshed and none preserved, making the checked-in artifact re-derivable; commit 7e84ff1.

### DW-1929: Gate 2c authenticated AppHost smoke still fails (`apphost.start.failed`) after EventStore 3.102.0 identity refresh.

origin: migrated from legacy ledger (""), 2026-09-05
location: _bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/apphost-smoke.json
source_spec: `_bmad-output/implementation-artifacts/spec-bump-latest-submodules-and-hexalith-packages.md`
reason: `apphost-smoke.json` records the correct SHAs and version but `finalVerdict=failed`; `validate-contract-artifacts.ps1 -RequireProviderVerification` rejects the non-passing smoke. The prior capture was already failed while the provider live-compatibility lane passes, so this needs a clean Aspire AppHost start after the historical Shell NuGet-versus-project CS1704 or file-lock blocker is cleared.
status: done 2026-09-05
archived: 2026-09-18
resolution: already resolved: _bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/apphost-smoke.json:1-10 records finalVerdict passed; commit bf507eaa recaptured the authenticated smoke at current provenance.

