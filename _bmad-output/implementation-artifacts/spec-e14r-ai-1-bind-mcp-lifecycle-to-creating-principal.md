---
title: 'E14R-AI-1 Bind MCP lifecycle snapshots to their creating principal'
type: 'bugfix'
created: '2026-11-06'
status: 'done'
baseline_commit: '6059af7e68571af18846de831c8fe5884896425a'
route: 'dispatch'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** AC 14.2 #4 fails at HEAD (Epic 14 retro F-1). The singleton `FrontComposerMcpLifecycleStore` keys entries only by handle. `FrontComposerMcpLifecycleTracker.ReadCoreAsync` re-checks tool visibility, but not who created the entry. So any caller admitted to the same tool (another user, or another tenant) can read the snapshot through `frontcomposer.lifecycle.subscribe`.

**Approach:** Record the acknowledging context's `TenantId` and `UserId` on each lifecycle entry. The store returns a miss when the reader is a different principal, so the tracker sends the same hidden-unknown result it sends for a handle that was never issued.

## Boundaries & Constraints

**Always:** Compare principals ordinally on both `TenantId` and `UserId`. A cross-principal read must produce output byte-identical to a never-issued handle. The same principal must still be able to poll from later scopes. The lifetimes stay as they are: a singleton store and a scoped tracker. The public API surface does not change.

**Never:** No changes to the `MapFrontComposerMcp` authorization policy or API keys (E14R-AI-2/3). No changes to the PRD G-7/OI-2 rows or the 2026-09-26 security packet (E14R-AI-5). No changes to sprint-status action-item statuses. No new test identifiers containing underscores. No change to `FrontComposerMcpUlidFactory`.

**Decisions (delegated to the builder):**
- D1. Ownership is enforced inside the store. `TryReadSnapshot` takes the reader context and returns `false` on a mismatch, so the tracker's single miss path serves both cases.
- D2. The tracker resolves the reader context before it looks up the handle. A `FrontComposerMcpException` from `GetContext()` and a blank `TenantId` or `UserId` both produce hidden-unknown. The response therefore never depends on whether the handle exists.
- D3. If `TrackAcknowledged` meets an existing entry owned by another principal, it fails closed with `UnsupportedSchema`. It never appends transitions to that entry or returns its handle.
- D4. Sequential ULIDs within a millisecond stay. Handles are identifiers, not bearer capabilities. Once ownership is bound, guessing an adjacent handle returns only the absent-handle shape. Timing remains out of scope per AC 14.3 #2. The generator is unchanged.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Owner polls | tenant-a/user-a created the entry and reads it in a new scope, by correlationId or messageId | Snapshot success | N/A |
| Other user | tenant-a/user-b reads the handle | Bytes equal the never-issued handle | Hidden-unknown |
| Other tenant | tenant-b/user-b reads the handle | Bytes equal the never-issued handle | Hidden-unknown |
| Case variant | TENANT-A/user-a reads the handle | Same as the never-issued handle | Hidden-unknown |
| Blank or failing reader | GetContext returns a blank id or throws `FrontComposerMcpException` | Same as the never-issued handle | Hidden-unknown |
| Re-ack collision | The dispatcher returns a correlationId owned by another principal | No mutation and no handle returned | `UnsupportedSchema` |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs` -- `TrackAcknowledged` (:36), `TryReadSnapshot` (:110), `GetOrCreateEntry` (:165), `LifecycleEntry` (:289). These are internal, so their signatures can change.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs` -- `TrackAcknowledged` (:19, internal) and `ReadCoreAsync` (:38). The primary constructor is public, so resolve `IFrontComposerMcpAgentContextAccessor` through the injected `services` rather than adding a constructor parameter.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs:58,90` -- holds the `context` that injected TenantId and UserId into the command. Pass it to `TrackAcknowledged`.
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs:138-170` -- the lifecycle precheck already rejects callers without a context. Leave it unchanged.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Governance/McpSecurityGovernanceTests.cs:122` -- `SdkDispatch_UnregisteredResourceHasDistinctNotFoundError` is the pattern to follow: real `AddFrontComposerMcp`, an `McpServer` over pipes, and `JsonSerializer.Serialize(CallToolResult)` comparison.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs:829-925` and `McpLifecycleStoreDisposalTests.cs` -- direct store calls whose arguments must be updated.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs` -- Add a `FrontComposerMcpAgentContext creator` parameter to `TrackAcknowledged` and store `OwnerTenantId`/`OwnerUserId` on `LifecycleEntry`. Add a `FrontComposerMcpAgentContext reader` parameter to `TryReadSnapshot`, with an ordinal, non-blank `IsOwnedBy` check. Fail closed on a cross-owner existing entry (D1, D3).
- [x] `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs` -- Thread the creator context through `TrackAcknowledged`. In `ReadCoreAsync`, resolve the reader before lookup and return hidden-unknown when none can be resolved (D2).
- [x] `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpCommandInvoker.cs` -- Pass `context` to the tracker.
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/Governance/McpSecurityGovernanceTests.cs` -- Add an SDK-level Theory over `correlationId` and `messageId`. tenant-a/user-a creates the entry. tenant-a/user-b and tenant-b/user-b must each get serialized results equal to a never-issued handle, with no handles or identities in the output. tenant-a/user-a still gets the snapshot. Names must be underscore-free.
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs` -- Update the direct store calls. Add underscore-free store-level cases for the case-variant reader, the blank reader, and the re-ack collision.
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpLifecycleStoreDisposalTests.cs` -- Update the direct store calls.

**Acceptance Criteria:**
- Given the existing `CommandLifecycleTests`, `McpCommandToolAdapterTests` and `HostingTests` lifecycle cases, when they run after the change, then they pass unchanged in intent.
- Given a Release build of `Hexalith.FrontComposer.Mcp.Tests`, when the built executable runs unfiltered and with `-trait Category=Governance`, then there are zero failures.

## Implementation Notes

- What was built: `LifecycleEntry` stores `OwnerTenantId`/`OwnerUserId`. `IsOwnedBy` requires a non-blank, ordinal match on both, and `TryReadSnapshot` returns a miss when it fails (D1). A re-acknowledgement that hits a foreign-owned entry throws `UnsupportedSchema` before any subscription or mutation (D3). The tracker resolves the reader through the injected `services` (`TryResolveReader`) before it parses the handle (D2). The public constructor is unchanged.
- Added beyond the matrix, both fail-closed:
  - A creator with a blank tenant or user is refused before storage. Admission already rejects such callers (`FrontComposerMcpToolAdmissionService.cs:177-180`), so only a nondeterministic accessor could reach this.
  - The tracker also hides results from unauthenticated readers, mirroring that same admission check.
- New governance-test helpers live in `tests/.../Governance/McpAuditCommandService.cs` and `McpAuditLifecycleCommand.cs`, one type per file.
- Evidence:
  - The Release build gave 0 warnings and 0 errors.
  - `Hexalith.FrontComposer.Mcp.Tests` passed 460/460 unfiltered and 17/17 with `-trait Category=Governance`.
  - The 25 new cases all pass.
  - The retro probe, rebuilt against the fix, reports `snapshotReturned=False` for both foreign principals.
  - The new SDK and tracker cases fail against the baseline `6059af7e` `Mcp.dll`.
- Review pass 1 produced 16 findings: 6 patched, 10 rejected, none deferred. The patches:
  - Store docs now state identifier-only, ordinal matching and the message-id collision cause.
  - `FrontComposerMcpToolAdmissionService.ValidateContext` is now `internal static` and reused by `TryResolveReader`.
  - The SDK theory gains a `tenant-b`/`user-a` row and log-leak assertions through `McpAuditLoggerProvider`.
  - The blank-creator test now pins refusal before storage.
  - The D3 theory gains a `tenant-b`/`agent-a` row.
- Re-verification:
  - The Release build gave 0 warnings and 0 errors.
  - `Hexalith.FrontComposer.Mcp.Tests` passed 461/461 unfiltered and 17/17 with Governance.
  - The rebuilt probe reports `snapshotReturned=False` for both foreign principals and `True` for the owner.
  - Shell `AnalyzerPolicy_IdentifierInventory_MatchesSeal` passes, so no reseal is needed.

## Spec Change Log

## Review Triage Log

| Iter | Source | Finding | Verdict | Evidence | Route |
|------|--------|---------|---------|----------|-------|
| 1 | blind | Blank-creator guard runs after dispatch; the invoker should validate its second `GetContext()` before injection | low | Pre-existing: the invoker's second context was never validated. Admission validates the first call, and the shipped HTTP accessor is deterministic within a request, so only a nondeterministic custom accessor reaches this. The fix would add a guard. | reject |
| 1 | blind + edge | A D3 collision after a successful dispatch surfaces as `UnsupportedSchema` and a schema-failure log | low | Confirmed: the invoker catch maps it through `SchemaNegotiationRuntimeGate.ToStructuredFailure`. This is the same pre-existing mapping as the message-collision and non-canonical-handle paths, and frozen D3 chose this category. It is reachable only when a dispatcher returns a correlation it did not mint. A distinct category would add public surface. | reject |
| 1 | blind | The D3 test's message-collision arm exercises the pre-existing `_byMessage` path, and the `<exception>` doc omits that cause | low | The arm is valid no-mutation coverage of a foreign principal. The doc really does omit "message id already mapped to another entry"; that is a direct doc fix. | patch |
| 1 | blind | The usable-principal rule is duplicated in admission `ValidateContext`, the tracker `TryResolveReader`, and the store | low | The tracker copies `FrontComposerMcpToolAdmissionService.ValidateContext` (`:176-183`). If admission tightens, the owner path gets `AuthFailed` while a never-issued handle gets hidden-unknown. Reusing the method removes the drift. The store's identifier-only check is intentional. | patch |
| 1 | blind + edge (x2) | `TryResolveReader` catches only `FrontComposerMcpException`, so other accessor exceptions give `DownstreamFailed`, contradicting D2 | false | D2 names only `FrontComposerMcpException` and blank ids. The reader is resolved before lookup, so the response does not depend on the handle and there is no oracle. The SDK precheck (`FrontComposerMcpServiceCollectionExtensions.cs:155`) maps these exceptions to `AuthFailed` before the tracker. The untested null-accessor and null-context branches are trivial. | reject |
| 1 | blind | The new SDK test drops the log-leak assertions of the `SdkDispatch` pattern | low | No new log carries identities, but AC 14.2 #4 isolation is unpinned in logs. Adding `McpAuditLoggerProvider` capture and asserts is a small change. | patch |
| 1 | blind | Cross-principal reads leave no operator trace | low | This is an observability enhancement outside the intent. It would need a new `LoggerMessage` event id. | reject |
| 1 | blind + edge | Lookup ignores the handle kind, so a cross-dictionary alias makes the owner's `messageId` poll miss; the D3 guard misses cross-dictionary collisions | low | Needs a dispatcher returning a correlation equal to another entry's minted messageId. The invoker mints distinct ULIDs. The owner can still poll by `correlationId`. The pre-existing aliasing used to leak the foreign snapshot and now fails closed. The fix would add guards and signatures. | reject |
| 1 | blind | D3 stops protecting after terminal-retention eviction | false | Once an entry is evicted its snapshot is gone. A later entry under the same correlation holds only the new acker's own transitions, so nothing from the old owner can be read. | reject |
| 1 | blind | No same-user, other-tenant row (`tenant-b`/`user-a`) | low | A tenant-skipping comparison is caught only by the case-variant row. Adding the row is a direct test change. | patch |
| 1 | blind | Store XML docs claim "only the same principal can read", but the store ignores `IsAuthenticated` | low | `IsOwnedBy` matches tenant and user identifiers only, and the tracker enforces authentication. This is a direct doc correction. | patch |
| 1 | verification-gap | `...WithoutIdentifiedCreatorFailsClosedBeforeStorage` passes even with the guard deleted | low | Pre-verified: without the guard, a blank-owned entry is stored, then the D3 check throws, and every assertion still holds. A follow-up owner acknowledgement of the same handles pins the guard. | patch |

## Verification

**Commands:**
- `timeout 900 dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Release -nodeReuse:false --disable-build-servers` -- expected: 0 warnings, 0 errors
- `tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests` -- expected: all pass
- `tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests -trait Category=Governance` -- expected: all pass
- The original probe, `/tmp/claude-1000/-home-administrator-projects-hexalith-frontcomposer/6895d03e-97e1-4a2b-a586-cc15eb68a6ce/scratchpad/lifecycle-probe`, rebuilt against the fix -- expected: `snapshotReturned=False` for both foreign principals
