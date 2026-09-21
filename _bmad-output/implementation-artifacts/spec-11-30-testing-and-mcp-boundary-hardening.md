---
title: 'Story 11.30: Testing and MCP Boundary Hardening'
type: 'bugfix'
created: '2026-09-20'
status: 'done'
baseline_commit: 'aaa916bbfea86a58d8bed005e80ae2419458571d'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Testing fake emits non-ULID lifecycle handles, lets evidence serialization override configured outcomes, and misses common credential keys. MCP also accepts 26-character Crockford strings outside the 128-bit ULID range.

**Approach:** Emit deterministic canonical Testing ULIDs, make evidence capture bounded and non-fatal with broader redaction, and share exact ULID validation across MCP identity boundaries.

## Boundaries & Constraints

**Always:** Preserve per-host determinism, distinct message/correlation IDs, lifecycle ordering, configured outcomes, bounded evidence, uppercase-only MCP handles, opaque read failures, and valid wire/API behavior. Parse with `NUlid.Ulid.TryParse` plus exact canonical round-trip; use stable payload-free failure markers.

**Never:** Emit GUIDs; normalize malformed MCP input; expose payloads, credentials, exception text, or rejected handles; weaken fail-closed behavior; change public APIs, fingerprints, generated output, route/visual debt, or unrelated deferred work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Testing identity | Same sequence in fresh hosts | Reproducible, distinct canonical IDs; later dispatches remain unique | Exhaustion fails instead of wrapping |
| Serialization failure | Cyclic, unsupported, or throwing payload | Outcome is unchanged; evidence has a bounded unavailable marker | Fatal exceptions propagate; details are omitted |
| Credential key | Any case/nesting of `Authorization`, `ApiKey`, `Cookie`, `PrivateKey`, or `ConnectionString` | Entire value is `<redacted>`; benign fields remain | Redact before truncation |
| MCP canonical maximum | `7ZZZZZZZZZZZZZZZZZZZZZZZZZ` | Accepted wherever a canonical lifecycle handle is allowed | N/A |
| MCP overflow | 26 Crockford characters beginning `8`–`Z` from any ingress | Rejected before side effects, storage, output, or lookup | Fail closed; reads stay opaque |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Testing/TestCommandService.cs:64-132` and new `DeterministicTestUlid.cs` -- replace `test-*` handles while preserving outcomes, callbacks, and evidence.
- `src/Hexalith.FrontComposer.Testing/Evidence.cs:50-130` and `README.md:5-13` -- harden/document the existing redactor and serialization boundary.
- `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj` and new `FrontComposerMcpUlid.cs` -- add centrally versioned `NUlid` and one exact validator.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:97-108,416-449` -- validate factory and dispatcher IDs, including no-tracker output.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs:50-65,159-179` -- reuse validation for acknowledgements, transitions, storage, and reads.
- `tests/Hexalith.FrontComposer.Testing.Tests/{FrontComposerTestHostTests,TestingFailureModeTests}.cs` -- repeatability, serialization, outcome, and credential regressions.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/{CommandInvokerTests,CommandLifecycleTests}.cs` -- maximum/overflow regressions with no-dispatch/no-echo assertions.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- reseal only changed test identifiers.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Testing/DeterministicTestUlid.cs`, `TestCommandService.cs`, `README.md` -- emit/document distinct deterministic ULIDs without public-surface change.
- [x] `src/Hexalith.FrontComposer.Testing/Evidence.cs` -- convert non-fatal serializer failures to a safe marker and broaden key redaction.
- [x] `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj`, `FrontComposerMcpUlid.cs`, `Invocation/FrontComposerMcpCommandInvoker.cs`, `Invocation/FrontComposerMcpLifecycleStore.cs` -- centralize exact NUlid validation across all identified paths.
- [x] `tests/Hexalith.FrontComposer.Testing.Tests` and `tests/Hexalith.FrontComposer.Mcp.Tests` -- cover every matrix row and preserve lifecycle, opacity, cancellation, redaction, and outcome contracts.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- reseal the exact changed identifier inventory without policy relaxation.

**Acceptance Criteria:**
- Given fresh Testing hosts and every configured outcome, when commands dispatch, then IDs are deterministic canonical ULIDs and evidence failures neither replace outcomes nor expose secrets.
- Given canonical-maximum and overflow fixtures at each MCP identity boundary, when allocation, acknowledgement, transition recording, or lifecycle reading occurs, then valid handles retain existing behavior and overflow handles fail closed before side effects or disclosure.
- Given the completed change, when the focused Testing and MCP suites, public/package checks, analyzer governance, and Release solution build run, then all pass with zero warnings and no unrelated contract drift.

### Review Findings

- [x] [Review][Decision] Credential-key matching over-redacts benign compound names — Decision (2026-09-21): Option 1 Keep Contains. Over-redaction of compound names is accepted fail-closed behavior for test evidence; equality would regress `AccessToken`/`PasswordHash`. No matcher change.

- [x] [Review][Patch] Exact NUlid round-trip is unpinned at factory and dispatcher identity checks [tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs:184]
- [x] [Review][Patch] Production MCP ULID factory is never asserted against the new canonical gate [src/Hexalith.FrontComposer.Mcp/FrontComposerMcpUlidFactory.cs:15]
- [x] [Review][Patch] Overflow lifecycle reads are only covered on correlationId [tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs:445]

- [x] [Review][Defer] Pending, observed, and replayed transition message IDs are not bound to the owning entry [src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs:51] — deferred: pre-existing; already recorded in deferred-work.md from the prior 11.30 review
- [x] [Review][Defer] Correlation/message maps are not one-to-one [src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs:165] — deferred: pre-existing; already recorded in deferred-work.md from the prior 11.30 review
- [x] [Review][Defer] Pre-canceled TrackAcknowledged still mutates before observing the token [src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs:47] — deferred: pre-existing; already recorded in deferred-work.md from the prior 11.30 review
- [x] [Review][Defer] Invalid public evidence options can still throw after serialization [src/Hexalith.FrontComposer.Testing/Evidence.cs:86] — deferred: pre-existing; already recorded in deferred-work.md from the prior 11.30 review
- [x] [Review][Defer] Evidence serialization allocates the full payload before the output cap [src/Hexalith.FrontComposer.Testing/Evidence.cs:74] — deferred: pre-existing; already recorded in deferred-work.md from the prior 11.30 review
- [x] [Review][Defer] AggregateException wrapping cancel or fatal serializer errors is unverified [src/Hexalith.FrontComposer.Testing/Evidence.cs:79] — deferred: unverified medium; settle by throwing AggregateException (inner OperationCanceledException or OutOfMemoryException) from a serialized getter and asserting rethrow versus the unavailable marker


- [x] [Review][Patch] Empty or whitespace dispatcher and pending-transition IDs are untested after NormalizeIdentifier removal [tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs:787]
- [x] [Review][Patch] deferred-work still records AggregateException unwrap as an open unverified gap after MustPropagate and tests landed [_bmad-output/implementation-artifacts/deferred-work.md:9746]

- [x] [Review][Defer] Prior 11.30 lifecycle-identity, cancellation-order, and evidence-bounds deferrals were reconfirmed this review [src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs:51] — deferred: pre-existing; already recorded in deferred-work.md from prior 11.30 reviews
- [x] [Review][Defer] Baseline-to-HEAD range includes concurrent release-policy, workflow, 4.5.1-complete, and EventStore/Tenants gitlink work [.github/workflows/release.yml:1] — deferred: concurrent work outside Story 11.30; already recorded as BH2-1/BH2-2

Rejected:
- rejected — credential-key `Contains` over-redaction: Decision (2026-09-21) Option 1 Keep Contains; fail-closed test evidence is the intended policy.
- false — spec `status: done` vs sprint `review`: the prescribed fix edits the spec under review.
- false — empty or whitespace dispatcher IDs now fail closed: `null` still aliases to `messageId`; non-canonical strings were never valid handles, and fail-closed matches frozen Never-normalize.
- false — SchemaGate/SpecGap `message-a` fakes will fail the MCP suite: those tests never complete a successful dispatch after this change (`Amount=200` fails `[Range]` before dispatch; SpecGap cases reject at admission/validation).
- false — DeterministicTestUlid lacks a production NUlid reference / per-outcome round-trip: every outcome shares `Create()`, and success/repeatability tests already pin NUlid exact round-trip; the test project already references NUlid.
- false — stale Code Map / empty Implementation Notes / incomplete Verification list: the prescribed fix edits the spec under review.
- false — BH-13 line endings remain wrong: `git ls-files --eol` on the touched files is `i/lf w/crlf` under `eol=crlf`, which is the required storage/checkout pair.
- false — TrackAcknowledged materializes pending transitions before storage: validating overflow IDs before `GetOrCreateEntry` is the required fail-closed-before-side-effects behavior.
- false — `pendingTransitions` can be null: production always passes a materialized list; no production caller can supply null.
- false — undefined lifecycle enum values can reach snapshots: production `LifecycleStateService.IsValidTransition` drops unknown `to` values (`_ => false`), and `TryRecordObservedTransition` has no production caller.
- false — MCP probe accepts `7ZZZZZZZZZZZZZZZZZZZZZZZZZ` without NUlid parse of the original handle: the I/O matrix requires that canonical-maximum fixture, and NUlid 1.7.3 cannot parse timestamps past DateTimeOffset.MaxValue; factory tests already pin exact round-trip on produced IDs.
- false — canonical-maximum fixtures reuse one string for message and correlation: there is only one 128-bit maximum encoding, and distinctness is pinned by the production factory test.
- false — Rejected/Timeout/StallAtSyncing paths never assert ULID shape: `DeterministicTestUlid.Create*` runs before outcome branching, and Success/repeatability tests already pin that encoder.
- false — null dispatcher correlation is omitted on the no-tracker fallback: that path emits raw `CommandResult` fields and has always omitted a missing correlation; lifecycle aliasing is tested on `TrackAcknowledged`.
- false — analyzer inventory reseal includes a concurrent CiGovernanceTests identifier: the seal is a whole-inventory snapshot required for AnalyzerPolicyGovernanceTests on this tree, and the evidence text records the concurrent delta without relaxing policy.
- false — leftover `partial` on FrontComposerMcpLifecycleStore / TryReadSnapshot opaque miss for null or whitespace: `partial` with one part is legal leftover after regex removal; opaque miss matches specified read-failure behavior, and `ReadAsync` already rejects non-canonical handles before storage lookup.
- false — spec status, Code Map, Implementation Notes, and Verification command list are stale: the prescribed fix edits the spec under review.
- low — no focused IsCanonical suite for Crockford lookalikes or hyphenated factory/ack forms: `ReadAsync_MalformedLifecycleHandle_FailsAsHiddenUnknownWithoutStoreLookup` already covers empty, lowercase, padded, hyphenated, and overflow handles, and factory/dispatcher theories cover overflow plus lowercase.
- low — lifecycle-enabled InvokeAsync lacks its own overflow dispatcher/callback test: no-tracker `InvokeAsync` and `TrackAcknowledged` independently pin fail-closed before storage.
- low — Testing copies Shell ExceptionGuard instead of sharing it: `RedactedEvidenceFormatter_FatalSerializationFailure_Propagates` already pins all four CLR fatal types; InternalsVisibleTo/sharing is a larger boundary change than the drift risk.
- low — deferred-work 11.30 rows use absolute paths and a looser schema: rewriting historical ledger entries is not a direct correction of this change.
- low — README omits sequence exhaustion and AssertExactRoundTripMismatchWhenParseable skips non-lowercase overflow: exhaustion is an internal test-host path already covered by `TestCommandService_ExhaustedIdentitySequence_FailsInsteadOfWrapping`; overflow fail-closed is already asserted.
- low — TryRecordObservedTransition returns true after a discarded Observe: pre-existing, internal, and currently has no production caller.

## Implementation Notes

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
|---|---|---|---|
| VG-1 subscription replay ULID guard lacks a direct regression | medium | patch | `LifecycleEntry.Observe` is a new production replay boundary, while the overflow tests stop in outer validation and the test lifecycle service never replays on subscription. |
| VG-2 three declared fatal exception arms lack behavioral coverage | medium | patch | Only `OutOfMemoryException` exercises `IsFatal`; removing any of the other three arms would leave the focused suite green. |
| EC-1 evidence serialization swallows cancellation | medium | patch | `catch (Exception ex) when (!IsFatal(ex))` includes `OperationCanceledException`, so a throwing payload can replace cancellation with the unavailable marker and continue dispatch. |
| EC-2 dispatcher overflow can be detected only after dispatch | false | reject | A dispatcher-returned identifier does not exist until dispatch completes; the new check runs immediately afterward and before MCP tracking, storage, or output, while factory identifiers are validated before dispatch. |
| EC-3 separator-bearing credential keys are not redacted | medium | patch | Substring matching `apikey`, `privatekey`, and `connectionstring` does not match common `api_key`, `private-key`, or `connection_string` spellings, despite the README's broader API-key/private-key wording. |
| EC-4 pending transitions may carry another canonical message ID | medium | defer | This was already accepted by the prior `NormalizeIdentifier(transitionMessageId) ?? messageId` path; exact canonical validation does not establish entry ownership. |
| EC-5 observed transitions may carry another canonical message ID | medium | defer | The pre-change `Observe` path also appended any normalized canonical message ID; no equality check binds it to `LifecycleEntry.MessageId`. |
| EC-6 a reused correlation ID is not checked against the new message or descriptor | medium | defer | `GetOrCreateEntry` returns the existing entry solely by correlation ID, so a later acknowledgement can report identifiers that do not describe the stored entry; this behavior predates the story. |
| EC-7 undefined lifecycle enum values can reach snapshots | false | reject | There is no production caller of `TryRecordObservedTransition`, and production subscription replay comes from `LifecycleStateService`, whose transition validation prevents undefined states from becoming replayable entries. |
| BH-1 the lifecycle JSON schema admits overflow-shaped strings | false | reject | The unchanged schema is a broad syntactic Crockford-shape gate, not the exhaustive semantic validator; the frozen intent explicitly preserves schema fingerprints while runtime validation supplies the stricter 128-bit check. |
| BH-2 pre-canceled tracking can mutate state before checking cancellation | medium | defer | With no pending transitions, the existing `TrackAcknowledged` path creates/subscribes/transitions without observing the token; the current ULID work did not introduce that ordering. |
| BH-3 lifecycle callback capture is unbounded | medium | defer | The invoker's unbounded callback queue and first full array copy predate this story; the new validation copy adds pressure but the required ingress cap is an existing lifecycle design issue. |
| BH-4 correlation/message dictionaries do not enforce cross-map uniqueness | medium | defer | A correlation equal to another entry's message can make lookup order return the wrong snapshot; the two-map insertion design is unchanged by this story. |
| BH-5 correlation reuse returns an entry with a different message | medium | defer | This is the same pre-existing one-to-one binding defect verified for EC-6. |
| BH-6 evidence serialization allocates before applying the output cap | medium | defer | Full `JsonNode` and JSON string construction before truncation is pre-existing; solving capture-memory bounds requires a streaming/depth/size policy beyond this serializer-failure fix. |
| BH-7 invalid public evidence options can still replace outcomes | medium | defer | Empty tenant/user IDs and negative payload limits can throw after serialization, but those public options and their lack of validation predate this story and require a configuration policy decision. |
| BH-8 the new unavailable marker is post-processed | medium | patch | Configured identifier replacement or a small payload cap can alter/truncate `<serialization-unavailable>`, violating the frozen stable-marker requirement. |
| BH-9 separator-bearing credential keys leak | medium | patch | Verified with the same `IsSensitiveKey` substring behavior as EC-3; both share one normalization fix. |
| BH-10 additional runtime exceptions should be classified fatal | false | reject | The reviewer did not establish that the named exceptions are process-fatal or reachable from this serialization path; the explicit four-type policy satisfies the captured requirement, with its declared arms covered by VG-2. |
| BH-11 spec and sprint review states disagree | false | reject | `in-review` is the build workflow's current spec state while sprint tracking remains `in-progress` until successful presentation synchronizes it to `review`; the mismatch is transient by design. |
| BH-12 the spec lists expected rather than actual verification results | low | reject | The observed run has current passing evidence, but the proposed correction edits this build's spec and review rules require rejecting such findings; presentation owns the final evidence record. |
| BH-13 changed text files have mixed or LF-only working-tree endings | low | patch | `.gitattributes` requires CRLF and `git ls-files --eol` confirms mixed/LF working copies on the changed files; normalization is a direct formatting correction. |
| BH-14 Testing tests rely on a transitive NUlid reference | low | patch | New test code directly compiles against `NUlid`, but the test project has no direct centrally versioned package reference; adding one is a direct dependency declaration. |
| BH2-1 release policy and workflow activation share one commit | medium | defer | Commit `81bae1a6a92314e8390db698a7fdbbea716199f8` contains both the policy row and workflow activation even though the separate release spec requires a policy-only push first; this is concurrent release work, not Story 11.30. |
| BH2-2 status commit also advances EventStore and Tenants gitlinks | medium | defer | Commit `b2a007f8d5e1f307f3a5b0a5ec286d73420b127b` changes both gitlinks in a status-only commit without Story 11.30 ownership or dependency-governance evidence; the Release build passes, but commit scope and compatibility provenance remain unresolved. |
| BH2-3 pending transitions may carry another canonical message ID | medium | defer | carried: The pre-change normalization path accepted any canonical transition message ID, so ownership binding remains pre-existing deferred work. |
| BH2-4 observed or replayed transitions may carry another canonical message ID | medium | defer | carried: The pre-change observation path also accepted any normalized message ID; exact validation does not establish entry ownership. |
| BH2-5 correlation reuse can return an entry with a different message or descriptor | medium | defer | carried: `GetOrCreateEntry` still resolves solely by correlation ID, matching the previously verified pre-existing defect. |
| BH2-6 correlation and message maps permit cross-map collisions | medium | defer | carried: The unchanged two-map insertion design can make lookup order resolve the wrong entry. |
| BH2-7 pre-canceled tracking mutates before observing cancellation | medium | defer | carried: With no pending transitions, the existing path creates, subscribes, and transitions without checking the token. |
| BH2-8 lifecycle callback capture remains unbounded | medium | defer | carried: The unbounded callback queue and materialization predate this story; history limits do not bound ingress memory. |
| BH2-9 dispatcher-returned overflow is detected after dispatch | false | reject | carried: A dispatcher-returned identifier cannot be validated before dispatch produces it; validation occurs immediately afterward and before MCP tracking, storage, or output. |
| BH2-10 evidence allocates before applying the output cap | medium | defer | carried: Full `JsonNode` and JSON-string materialization before truncation is the previously verified pre-existing memory-bound gap. |
| BH2-11 invalid evidence options can still replace outcomes | medium | defer | carried: Empty configured identifiers and negative limits are pre-existing public-option validation gaps requiring a policy decision. |
| BH2-12 aggregate-wrapped cancellation or fatal serialization failures are swallowed | medium | patch | `JsonSerializer` propagates direct getter exceptions, while the new catch classifies only the outer `AggregateException`; an aggregate containing cancellation or a fatal exception therefore returns the unavailable marker instead of propagating. |
| BH2-13 fatal ULID-factory exceptions are converted to unsupported-schema failures | medium | defer | `NewCanonicalUlid` already caught every non-cancellation exception at the baseline, including process-fatal exceptions; this is real but pre-existing MCP factory behavior outside Story 11.30. |
| BH2-14 TryRecordObservedTransition can report a discarded transition as recorded | low | defer | The internal method returns `true` after `Observe`, while terminal-state guards can discard the transition; the behavior predates Story 11.30 and currently has no production caller. |
| BH2-15 specs list expected rather than recorded verification results | low | reject | carried: Story verification evidence is finalized by the presentation step, and the unrelated release spec remains `in-progress`; editing either spec to satisfy this finding is not a code correction for this review. |
| EC2-1 aggregate-wrapped cancellation or fatal serialization failures are swallowed | medium | patch | The new catch filter checks only the outer exception, so an `AggregateException` containing `OperationCanceledException` or a fatal exception is converted to the unavailable marker. |
| EC2-2 pending transition message IDs are not bound to the entry | medium | defer | carried: Canonicality replaced normalization, but the pre-existing path never required equality with the owning message ID. |
| EC2-3 observed transition message IDs are not bound to the entry | medium | defer | carried: The pre-existing observation path likewise lacked message ownership binding. |
| EC2-4 reused or cross-map-colliding lifecycle identities can resolve another command | medium | defer | carried: The unchanged dictionary design and correlation-only reuse check are the previously verified one-to-one identity defect. |
| EC2-5 pre-canceled TrackAcknowledged mutates state | medium | defer | carried: Cancellation is still observed only inside the pending-transition loop after the pre-existing mutations. |
| EC2-6 undefined lifecycle states can reach snapshots | false | reject | carried: Production replay is constrained by `LifecycleStateService.IsValidTransition`, and `TryRecordObservedTransition` has no production caller. |
| EC2-7 TryRecordObservedTransition returns success for a discarded terminal transition | low | defer | `LifecycleEntry.Observe` silently rejects a different post-terminal state, but the internal wrapper still returns `true`; this pre-existing contract mismatch has no production caller. |
| VG-3 null dispatcher correlation fallback lacks MCP lifecycle coverage | medium | patch | The optional-correlation contract is used by `StubCommandService`, but every current MCP lifecycle fake supplies a correlation; a focused lifecycle invocation test must pin fallback to the message ID and readable snapshot behavior. |
| BH3-1 pending transitions may carry another canonical message ID | medium | defer | carried: The pre-change normalization path accepted any canonical transition message ID, so ownership binding remains pre-existing deferred work. |
| BH3-2 observed or replayed transitions may carry another canonical message ID | medium | defer | carried: The pre-change observation path also accepted any normalized message ID; exact validation does not establish entry ownership. |
| BH3-3 correlation reuse can return an entry with a different message or descriptor | medium | defer | carried: `GetOrCreateEntry` still resolves solely by correlation ID, matching the previously verified pre-existing defect. |
| BH3-4 correlation and message maps permit cross-map collisions | medium | defer | carried: The unchanged two-map insertion design can make lookup order resolve the wrong entry. |
| BH3-5 pre-canceled tracking mutates before observing cancellation | medium | defer | carried: With no pending transitions, the existing path creates, subscribes, and transitions without checking the token. |
| BH3-6 lifecycle callback capture remains unbounded | medium | defer | carried: The unbounded callback queue and materialization predate this story; history limits do not bound ingress memory. |
| BH3-7 evidence allocates before applying the output cap | medium | defer | carried: Full `JsonNode` and JSON-string materialization before truncation is the previously verified pre-existing memory-bound gap. |
| BH3-8 invalid evidence options can still replace outcomes | medium | defer | carried: Empty configured identifiers and negative limits are pre-existing public-option validation gaps requiring a policy decision. |
| BH3-9 JSON escaping can bypass configured identifier replacement | medium | defer | `RedactConfiguredValues` still runs after JSON encoding, so configured tenant or user identifiers containing quotes, backslashes, or control characters can survive as escaped text. This behavior predates Story 11.30 and requires structural configured-value redaction. |
| BH3-10 fatal ULID-factory exceptions are converted to unsupported-schema failures | medium | defer | carried: `NewCanonicalUlid` already caught every non-cancellation exception at the baseline, including process-fatal exceptions. |
| BH3-11 TryRecordObservedTransition can report a discarded transition as recorded | low | defer | carried: The internal method returns `true` after `LifecycleEntry.Observe`, even when terminal-state guards discard the transition; the behavior predates Story 11.30 and currently has no production caller. |
| BH3-12 release policy and workflow activation share one commit | medium | defer | carried: Concurrent release-governance commit `81bae1a6a92314e8390db698a7fdbbea716199f8` combined authorization and activation outside Story 11.30. |
| BH3-13 status work also advanced EventStore and Tenants gitlinks | medium | defer | carried: Commit `b2a007f8d5e1f307f3a5b0a5ec286d73420b127b` advanced both gitlinks without Story 11.30 ownership or compatibility evidence. |
| BH3-14 deferred-work source paths are absolute | false | reject | The attended-build append format explicitly records `{spec_file}`, which is the absolute runtime path, as provenance rather than as a portable Markdown link; no consumer behavior was shown to resolve or fail on these values. |
| EC3-1 pending transitions may carry another canonical message ID | medium | defer | carried: Canonicality replaced normalization, but the pre-existing path never required equality with the owning message ID. |
| EC3-2 observed transitions may carry another canonical message ID | medium | defer | carried: The pre-existing observation path likewise lacked message ownership binding. |
| EC3-3 reused correlations can retain an older message or descriptor | medium | defer | carried: The unchanged correlation-only lookup is the previously verified one-to-one identity defect. |
| EC3-4 opposite-map identity collisions can resolve another command | medium | defer | carried: The unchanged two-map insertion design permits ambiguous cross-map handles. |
| EC3-5 pre-canceled TrackAcknowledged mutates state | medium | defer | carried: Cancellation is still observed only inside the pending-transition loop after the pre-existing mutations. |
| EC3-6 lifecycle callback capture remains unbounded | medium | defer | carried: The unbounded callback queue and materialization predate this story; history limits do not bound ingress memory. |
| EC3-7 TryRecordObservedTransition returns success for a discarded terminal transition | low | defer | carried: `LifecycleEntry.Observe` can silently reject a different post-terminal state while the internal wrapper still returns `true`; no production caller exists. |
| EC3-8 evidence allocates before applying the output cap | medium | defer | carried: Full `JsonNode` and JSON-string materialization before truncation is the previously verified pre-existing memory-bound gap. |
| EC3-9 invalid evidence options can still replace outcomes | medium | defer | carried: Empty configured identifiers and negative limits are pre-existing public-option validation gaps requiring a policy decision. |
| EC3-10 dispatcher-returned overflow is detected after dispatch | false | reject | carried: A dispatcher-returned identifier cannot be validated before dispatch produces it; validation occurs immediately afterward and before MCP tracking, storage, or output. |
| VG4-1 NuGet publication does not consume the selected authority posture | medium | defer | The verification-gap review found that the caller exports the authority posture through the pinned reusable workflow, but FrontComposer's NuGet publisher never reads it; shared Hexalith.Builds enforcement and an integration regression are required. This concurrent release work is outside Story 11.30. |
| VG4-2 subscription replay correlation ownership guard lacks a regression | medium | patch | The replay fixture covers an overflow message with the owning correlation, while direct overflow lookup stops before `LifecycleEntry.Observe`; removing only the new correlation-equality guard leaves every existing test green. |

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: all Testing tests pass with zero warnings.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Release -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: all MCP tests pass with zero warnings.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: zero warnings and errors.
- `git diff --check` -- expected: no whitespace errors.
