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

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: all Testing tests pass with zero warnings.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Release -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: all MCP tests pass with zero warnings.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: zero warnings and errors.
- `git diff --check` -- expected: no whitespace errors.
