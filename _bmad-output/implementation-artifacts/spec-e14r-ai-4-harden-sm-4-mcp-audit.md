---
title: 'E14R-AI-4 Harden the SM-4 MCP leak and oracle audit'
type: 'chore'
created: '2026-11-06'
status: 'done'
baseline_commit: '67b9420987738712a2a73d218e103d8f390e1317'
route: 'dispatch'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 14 retro F-6 found that the named SM-4 audit (`McpSecurityGovernanceTests`) leaves AC 14.2 partly unproven:

- (a) Its hidden-tool case uses a gate that always throws, so no visible sibling exists. Suggestion and visible-list influence are tested only at unit level (`ToolAdmissionTests.cs:57-99`).
- (b) `HFC-MCP-UNKNOWN-TOOL` is never asserted anywhere in `tests/`.
- (c) `McpServer.Create` gets no logger factory, so SDK logging is disabled. The "no secret in logs" claim therefore covers only FrontComposer loggers, at Information level.
- (d) "Unauthenticated" and "tenant-less" are one synthetic context. The production accessor's `AuthFailed` and `TenantMissing` paths never run over the SDK.
- (e) No authenticated `tools/list` + `tools/call` goes through the mapped host.
- (g) The reused focused tests are not bound to the audit, and `AuthRedactionStressTests` was not rerun.

**Approach:** Extend the Governance-trait audit with SDK- and HTTP-level tests for each gap. Compare against a "same name, never registered" world, so that byte-identity proves there is no oracle. Pin the reused focused tests, so that deleting or skipping one fails the audit.

## Boundaries & Constraints

**Always:**
- Every new test carries `[Trait("Category", "Governance")]`, directly or through its class.
- New test identifiers contain no underscores, so the analyzer inventory seal does not move.
- Server-only values must never appear in any response or in any log category at Trace level: tenant ID, user ID, gate exception text, schema fingerprints and API keys.

**Never:**
- No production code changes. If a new test exposes a real defect, stop and report it instead of fixing it.
- No changes to the PRD G-7/OI-2 rows or the 2026-09-26 security packet (E14R-AI-5 is held).
- No sprint-status action-item changes.
- No duplicate of `DevelopmentEnvironmentInAnyCaseAdmitsAllowAllGates`, which already covers (f) since `67b94209`. Pin it instead.

**Decisions (builder):**
- D1. Oracle proof: a request against "registered but hidden" must be byte-identical to the same request against a manifest that never registered that name, with the same caller and the same visible sibling. This holds whatever suggestion the name produces.
- D2. If SDK Trace logs echo *client-supplied* request content, do not weaken the FrontComposer-category assertions. Assert that server-only values are absent from every category. Record the SDK's wire-trace echo of a client's own input as a residual in Implementation Notes: it is not a cross-principal disclosure.
- D3. Pins use reflection over the `Mcp.Tests` assembly: the named method exists, carries `Fact` or `Theory`, has an empty `Skip`, and has no `Category=Quarantined` trait. `AuthRedactionStressTests` lives in Shell.Tests, so it is pinned by a source scan through the existing `LocateRepositoryRoot` pattern: the named methods are present and there is no `Skip =`. It is also run as verification evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Gate-hidden vs never registered | Admitted caller; `Billing.HiddenCommand.Execute` gate-false vs unregistered | Serialized `CallToolResult` identical in both worlds. It has `unknown_tool`, `HFC-MCP-UNKNOWN-TOOL` and "Request failed.", and `visibleTools` contains the sibling `Billing.PayInvoiceCommand.Execute`. | N/A |
| Policy-denied vs never registered | Same caller; a policy-gated command denied by `IFrontComposerMcpCommandPolicyGate` | Identical across worlds; same assertions | N/A |
| Near-match | A typo of the hidden name | Identical across worlds; the suggestion is never the hidden name | N/A |
| Tenant-less | Real `HttpFrontComposerMcpAgentContextAccessor`; authenticated `HttpContext.User` with no tenant claim → `TenantMissing` | `tools/list` is empty, and `tools/call` is identical across worlds | N/A |
| Unauthenticated | Real accessor; no `HttpContext` → `AuthFailed` | Same as tenant-less, with no `auth_failed` or `tenant_missing` text | N/A |
| Mapped host | Production host using `AddFrontComposerMcpApiKey()`, restrictive gates that admit the key identity, and a recording dispatcher; SDK HTTP client with the key | `tools/list` contains the tool. `tools/call` dispatches once with the key's `TenantId`/`UserId` and canonical ULID `MessageId`/`CorrelationId`, which equal the acknowledgement's handles. | N/A |
| Pin removed | A pinned focused test is deleted, renamed or skipped | The pin test fails and names it | N/A |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.FrontComposer.Mcp.Tests/Governance/McpSecurityGovernanceTests.cs:~300` -- `SdkDispatch_UnregisteredResourceHasDistinctNotFoundError`. This is the pattern for pipes + `McpServer.Create`. Pass `loggerFactory:` with Trace capture. Do not rename existing tests.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Governance/McpHttpTestHost.cs`, `McpAuditLoggerProvider.cs`, `McpAuditCommandService.cs`, `McpAuditLifecycleCommand.cs` -- reuse these. Add a recording dispatcher only if the existing one cannot capture injected values. One type per file.
- `src/Hexalith.FrontComposer.Mcp/HttpFrontComposerMcpAgentContextAccessor.cs` -- internal and registered by `AddFrontComposerMcp`. Drive it through a registered `IHttpContextAccessor` whose `HttpContext` is null or carries a chosen `User`. `AddHttpContextAccessor` is `TryAdd`, so register yours first.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpToolAdmissionService.cs:126-148` -- builds the hidden-unknown shape and `docsCode`.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ToolAdmissionTests.cs` (`HiddenTool_AndAbsentTool_ProduceStructurallyIdenticalUnknownEnvelope`, `PolicyHiddenNearMatch_DoesNotInfluenceSuggestionOrVisibleList`, `TenantHiddenNearMatch_DoesNotInfluenceSuggestionOrVisibleList`), `Invocation/ProjectionReaderTaxonomyTests.cs` (all 7), `Schema/SchemaNegotiationPrecedenceMatrixTests.cs` (all 4), and the Governance tests from `97f03dfe`/`67b94209` -- these are the pin targets.
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/AuthRedactionStressTests.cs` -- 4 methods; the source pin target.
- `Skills/SkillTypeOrganizationGovernanceTests.cs:338` -- the `LocateRepositoryRoot` pattern.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/Governance/McpSecurityGovernanceTests.cs` (+ helpers) -- Add SDK oracle tests for the first five matrix rows (D1), using a Trace `loggerFactory` passed to `McpServer.Create`. Assert `HFC-MCP-UNKNOWN-TOOL`. Assert server-only values are absent from all captured logs (D2).
- [x] same -- Add the mapped-host authenticated list + invoke test (matrix row 6) over the SDK HTTP client transport.
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/Governance/` (new file) -- Add a Governance pin test per D3. Its pin list contains the Code Map targets plus `DevelopmentEnvironmentInAnyCaseAdmitsAllowAllGates`. Each failure message names the missing or skipped test.

**Acceptance Criteria:**
- Given the Release build, when the `Mcp.Tests` executable runs unfiltered and with `-trait Category=Governance`, then everything passes and the new tests are in the Governance run.
- Given Shell.Tests in Release, when it runs `-class Hexalith.FrontComposer.Shell.Tests.Services.Auth.AuthRedactionStressTests` and the analyzer seal test, then both pass.
- Given a pinned method name temporarily changed locally, when the pin test runs, then it fails naming that method. Revert the change after checking.

## Implementation Notes

- What was built (tests only; `git diff 67b94209 -- src/` is empty):
  - `SdkUnknownToolAnswerIsIdenticalWhetherTheNameIsHiddenOrNeverRegistered` (8 rows) covers matrix rows 1-3. Each row runs two SDK pipe worlds that differ only in whether the manifest registers the hidden name. The caller, gates, visible sibling, and request are the same, and the production `HttpFrontComposerMcpAgentContextAccessor` reads an admitted `HttpContext.User`. `tools/list` and `tools/call` must serialize byte-identically through `McpJsonUtilities.DefaultOptions`. The answer must carry `unknown_tool`, `HFC-MCP-UNKNOWN-TOOL`, "Request failed.", and `visibleTools == [Billing.PayInvoiceCommand.Execute]`, and must never dispatch. Hiding is by a gate returning false, a throwing gate, or a denying `IFrontComposerMcpCommandPolicyGate`. The near-match rows use a case variant, a one-letter typo, and `"Billing"`. `"Billing"` ties the hidden name with the sibling at score 92, so the ordinal tie-break would suggest the hidden name if it reached the candidates; the pinned suggestion is the sibling.
  - `SdkCallerWithoutUsableIdentityCannotTellARegisteredToolFromAnAbsentOne` (2 rows) covers rows 4-5 through the production accessor, with a registered `IHttpContextAccessor` substitute. The substitute returns a tenant-less authenticated user, or null. Across both worlds, the hidden, sibling, absent, and lifecycle tools all return one identical bare "Request failed." answer, and `tools/list` is empty. FrontComposer logs prove that the `TenantMissing` or `AuthFailed` path ran in tools/list, the invoker, and the lifecycle precheck.
  - `MappedHostListsAndInvokesTheToolAsTheApiKeyIdentity` covers row 6. It runs a Production host with `AddFrontComposerMcpApiKey()`, a restrictive `McpAuditIdentityTenantToolGate` that admits only the key identity, and `McpRecordingAuditCommandService`. An SDK `HttpClientTransport` (StreamableHttp) sends the key header. The test asserts one dispatch with the key's tenant and user, and canonical, distinct ULID handles that equal the acknowledgement's.
  - `McpSecurityAuditPinGovernanceTests` (D3) pins 33 `Mcp.Tests` methods by string name, so a rename fails the pin instead of breaking the build. The pins cover the Code Map targets, every `McpSecurityGovernanceTests` method, and three API-key identity tests in `ApiKeyAuthenticationSchemeTests` and `AuthContextAccessorTests`.
    - A pin fails when the method is missing or not public. It fails when the declaring type is not publicly visible, or is abstract and not sealed. It fails when the method is not a `Fact`/`Theory`, or carries a `Skip`, an `Explicit`, `SkipExceptions`, or a skipped or explicit data row.
    - A pin also fails on a `Category` trait that the blocking lanes exclude: `Quarantined`, `GovernanceBuild`, `Performance`, `NightlyProperty` or `e2e-palette`. Traits are read from the method, class or assembly, and matching ignores case.
    - Roslyn locates each pinned method's source under `tests/Hexalith.FrontComposer.Mcp.Tests` and flags `Assert.Skip`, `Assert.SkipWhen` and `Assert.SkipUnless` calls in its body.
    - A Roslyn source pin covers the 4 `AuthRedactionStressTests` methods. Each must be declared and be a `Fact`/`Theory`. The file must have no `Skip =` or `Explicit =`, no non-blocking `Category` trait on the class or a pinned method, and no in-body `Assert.Skip*`.
    - Non-vacuity probes run against a nested non-public fixture that xUnit never discovers: a skipped fact, an explicit fact, `SkipExceptions`, a skipped data row, a lowercase `quarantined` class trait, and an in-body `Assert.Skip`. A mutated `AuthRedactionStressTests` source adds a skip, a rename, a `Performance` class trait and an in-body `Assert.Skip`. Every probe must be reported, naming its method.
  - The tenant gate records every protocol name it evaluates and denies, and the policy gate records every policy it evaluates. In the registered world the hidden tool must be evaluated and denied by its gate. For the policy row, the tenant gate admits the tool and the policy gate evaluates `AuditRestrictedPolicy`. The never-registered world must evaluate neither.
  - Server-only values asserted absent from every response and every log category are the tenant ID, the user ID, the gate exception marker, both descriptor fingerprints, the configured API key and the policy name.
  - `MappedHostListsAndInvokesTheToolAsTheApiKeyIdentity` also sends a `tools/call` that supplies `TenantId`, `UserId`, `MessageId` and `CorrelationId` itself. It must be refused without dispatch and echo none of those values. The app is stopped before the captured logs are read.
  - `SdkDispatch_UnregisteredResourceHasDistinctNotFoundError` and `SdkLifecycleSubscribeAnswersForeignPrincipalsAsNeverIssuedHandle` now pass a Trace logger factory that captures every category to `McpServer.Create` and `StreamServerTransport`.
    - Client-supplied content stays asserted on FrontComposer categories only (D2): the hidden tool name in one test, and the lifecycle handles the client sent and received in the other. Tenant, user and gate exception text are asserted absent from every category.
    - The resource test's admitted identity moved from `tenant-a`/`agent-a` to the audit's distinctive values. SDK Trace logs the skill-corpus `resources/list` response, and its `security-tenant-and-policy-boundaries` entry contains the substring `tenant-a`.
- Helper changes:
  - `McpAuditLogger` now records the level, the structured state values and the scope state. Non-string collection values are rendered element by element.
  - `McpHttpTestHost.CreateBuilder` adds a provider-specific Trace filter, so host configuration cannot lower capture.
  - New one-type files: `McpSdkPipeSession` (pipes + `McpServer.Create(..., loggerFactory, ...)` + a Trace-logging `StreamServerTransport`), `McpAuditIdentityTenantToolGate`, `McpAuditDenyingCommandPolicyGate` and `McpRecordingAuditCommandService`. The existing `McpAuditCommandService` cannot record injected values.
- Residual (D2), confirmed by dumping the captured logs: SDK categories echo client-supplied request content.
  - `ModelContextProtocol.Server.StreamServerTransport` at Trace logs every received message verbatim, including the tool name and arguments.
  - `ModelContextProtocol.Server.McpServer` at Information logs `"{ToolName}" completed. IsError = {IsError}.` with the client-supplied name.
  - `McpServer` at Trace logs every sent response. These carry only what the client receives, including the lifecycle handles returned to their owner and the framework-global skill corpus resource list.
  - A client that types a hidden tool's exact name therefore sees it echoed in SDK logs. This is the client's own input, not a cross-principal disclosure. The FrontComposer-category assertions are unweakened and still forbid both the hidden name and the requested name. Every category is also asserted free of the hidden name whenever the client did not send it.
- Evidence:
  - The Release build of `Mcp.Tests` gave 0 warnings and 0 errors.
  - The executable passed 503/503 unfiltered and 44/44 with `-trait Category=Governance`, and all 13 new cases appear in the Governance run.
  - Shell.Tests (Release) passed `AuthRedactionStressTests` 10/10 and `AnalyzerPolicy_IdentifierInventory_MatchesSeal` 1/1, so no reseal is needed.
  - Pin probes, each reverted with `git checkout`:
    - Renaming `TenantHiddenNearMatch_DoesNotInfluenceSuggestionOrVisibleList` failed the pin, naming it ("... is pinned by the SM-4 MCP audit, but it no longer exists").
    - Adding `Skip` to one `SchemaNegotiationPrecedenceMatrixTests` method and `Category=Quarantined` to another failed the pin, naming both.
- Review pass 1 produced 24 triage rows: 12 patched, 12 rejected (3 of them false), none deferred. No production defect was found.
  - Each suspected oracle is blocked by the admission order:
    - Schema negotiation runs only for an exact visible match.
    - Admission runs before argument validation.
  - The patches:
    - 33 pins now cover every audit method and the API-key tests.
    - Trait matching ignores case. Discoverability and `SkipExceptions` are checked. A probe fixture covers every reflection branch.
    - The source pin checks all non-blocking categories, and in-body `Assert.Skip*` calls are caught in all pinned methods.
    - The tenant and policy gates prove they were evaluated, and `AuditPolicyName` is treated as server-only.
    - The mapped host stops before its logs are read, and a spoofed derivable-argument call is refused.
    - The two existing SDK tests now run with a Trace logger factory. Their admitted identity moved off `tenant-a`, because the skill corpus legitimately contains that substring.
    - Collection values in captured logs are now rendered.
- Re-verification:
  - The Release build of Mcp.Tests gave 0 warnings and 0 errors.
  - `Mcp.Tests` passed 503/503 unfiltered and 44/44 with Governance.
  - Shell `AuthRedactionStressTests` passed 10/10 and the analyzer seal test passed. There are no `src/` changes.

## Spec Change Log

## Review Triage Log

| Iter | Source | Finding | Verdict | Evidence | Route |
|------|--------|---------|---------|----------|-------|
| 1 | blind + verification-gap | The pin list omits the new F-6 audit tests, the older SM-4 tests, and the API-key leak and conflict tests | medium | The three new methods, `SdkDispatch_*`, `ProductionMapping_*` and the API-key tests have no pin, so the main deliverable could be skipped silently. Adding the strings is a direct fix. | patch |
| 1 | verification-gap + edge + blind | The source pin flags only the literal `Quarantined`, not the other non-blocking categories | low | Pre-verified: a `Performance` class trait takes it out of Gate 3a while the pin stays green. | patch |
| 1 | verification-gap + edge + blind | Neither pin catches an in-body `Assert.Skip`/`SkipWhen`/`SkipUnless`, or `SkipExceptions` | low | Pre-verified: the repo already uses dynamic skips (`LiveTwoTenantProductionAdapterTests.cs:49`). | patch |
| 1 | verification-gap + blind | The reflection pin's skip, explicit, data-row and category branches have no automated self-probe | low | Pre-verified: deleting the Skip branch leaves the pin green. | patch |
| 1 | edge + blind | Trait matching is case-sensitive | low | xUnit trait filtering is case-insensitive. Using `OrdinalIgnoreCase` is a direct fix. | patch |
| 1 | edge + blind | The pins never check discoverability (public, non-abstract) | low | An `internal` class hides from discovery while the pin passes. A one-line check fixes it. | patch |
| 1 | edge + blind | Nothing proves the gate-false and policy rows actually evaluated the hidden descriptor | low | A gate that is never consulted would pass vacuously. Recording the evaluations is small. | patch |
| 1 | edge | `AuditPolicyName` is missing from the server-only values | low | The policy name is a server-only value. Direct addition. | patch |
| 1 | edge | The mapped-host test reads its logs while the host is still running | low | Late request-completion or shutdown entries are never scanned. Stop the host first. | patch |
| 1 | blind | The mapped-host handle check holds by construction, and spoofed derivable arguments are untested | low | The stub echoes the command's IDs. A spoof attempt (client `TenantId`/`MessageId`) is the missing AC 14.1 #4 proof. | patch |
| 1 | blind | F-6(c) is closed only for the new tests; `SdkDispatch_*` and `SdkLifecycleSubscribe*` still pass no logger factory | low | F-6(c) names `SdkDispatch_*` explicitly. Pass a Trace factory and apply the D2 split. | patch |
| 1 | blind + edge | `McpAuditLogger` renders collection values as type names | low | `Append(value.Value)` prints `System.String[]`. Enumerating them is a direct fix. | patch |
| 1 | blind | The oracle compares re-serialized client models, not wire bytes | low | It matches the frozen row "Serialized `CallToolResult` identical". The server emits only mapped known properties. | reject |
| 1 | blind | The D2 carve-out skips the all-category check for the hidden name; compare counts across the two worlds | low | Tools are handler-based, with no registration dump. The fix adds cross-world counting logic. | reject |
| 1 | blind + edge | `SkipTestWithoutData`, `TheoryDataRow` skips in MemberData, and pruned data rows go undetected | low | Rare. Enumerating member data adds complexity. | reject |
| 1 | edge | Traits from `CollectionDefinition` or `IDataAttribute.Traits`, `#if` wrapping, and partial declarations go undetected | low | Rare test-authoring shapes. The fix adds complexity. | reject |
| 1 | blind + edge | `NonBlockingCategories` is hand-copied rather than parsed from `quality.yml` | low | It drifts only when the workflow changes. Parsing adds complexity. | reject |
| 1 | blind | The source regexes scan comments and strings | low | That can only cause loud false failures, never silent passes. | reject |
| 1 | blind | The oracle is never exercised over the mapped HTTP host | low | The answer is built by the same JSON-RPC path; HTTP framing does not depend on visibility. | reject |
| 1 | blind | Only the null-`HttpContext` AuthFailed branch runs over the SDK | false | An unauthenticated `DefaultHttpContext` takes the same `IsAuthenticated != true` branch (`HttpFrontComposerMcpAgentContextAccessor.cs`). The API-key branches are unit-tested. | reject |
| 1 | blind | A hidden tool plus a fingerprint header might give a schema answer (an existence oracle) | false | Schema negotiation runs only for an exact visible match (`FrontComposerMcpToolAdmissionService.cs:82-102`). Hidden names never reach it. | reject |
| 1 | blind | Ill-typed arguments to a hidden name could differ from an absent one | false | `InvokeAsync` resolves admission before `ValidateArguments` (`FrontComposerMcpCommandInvoker.cs:43-66`). | reject |
| 1 | blind + edge | `McpSdkPipeSession` disposal is not exception-safe | low | It needs a throwing dispose; the fix adds try/finally scaffolding. | reject |
| 1 | edge | A skip match in a data-source property is attributed to the class | low | The failure still fires loudly. | reject |

## Verification

**Commands:**
- `timeout 900 dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Release -nodeReuse:false --disable-build-servers` -- expected: 0 warnings, 0 errors
- `tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests` unfiltered and with `-trait Category=Governance` -- expected: all pass
- the built `Hexalith.FrontComposer.Shell.Tests` executable with `-class Hexalith.FrontComposer.Shell.Tests.Services.Auth.AuthRedactionStressTests` and `-method "*AnalyzerPolicy_IdentifierInventory_MatchesSeal*"` -- expected: all pass
