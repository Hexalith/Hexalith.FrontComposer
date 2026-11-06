---
title: 'E14R-AI-2 + E14R-AI-3 Non-substitutable MCP authentication and a working API-key path'
type: 'bugfix'
created: '2026-11-06'
status: 'done'
baseline_commit: '97f03dfeef5dd6b1c41829323fb07646c56d66ec'
route: 'dispatch'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:**
- *F-2.* `MapFrontComposerMcp` adds an empty `RequireAuthorization()`. Once a host appends any `AuthorizationPolicy`, ASP.NET drops the default authenticated-user policy, so anonymous requests reach MCP dispatch. The Counter `Test` host relies on exactly this: it appends a loopback policy, and its anonymous probes pass.
- *F-3.* `FrontComposerMcpOptions.ApiKeys` is matched only inside the request handler, after authorization has already run. An API-key-only host therefore gets 401, unless it writes its own scheme, which is what Counter did by duplicating the check.
- *Migration note.* v4.6.0 shipped these host-composition changes without one.
- *F-7.* The retro lists map-time edge cases that each need a decision.

**Approach:**
- Make the framework's authenticated-user requirement an explicit policy object. Appended host policies combine with it by union, so they cannot remove it.
- Ship a FrontComposer API-key authentication scheme, so the configured keys authenticate before authorization runs.
- Repair Counter, the e2e probes and the docs in the same change, and record a decision for each F-7 edge case.

## Boundaries & Constraints

**Always:**
- Anonymous `initialize` gets 401 on every mapped MCP endpoint, whatever the host appends.
- The host's `DefaultPolicy` still applies to a framework-only mapping.
- Counter in both `Test` and `Development` boots and serves MCP to the sample key.
- No response or log echoes a key.

**Never:**
- No changes to the PRD G-7, OI-2, OI-3, D-14 or FR-19 rows, or the 2026-09-26 security packet. These belong to E14R-AI-5, a held human decision.
- No sprint-status action-item status changes.
- No E14R-AI-4 audit hardening beyond the assertions this change needs.
- No E15R-AI-5 Shell release note; only link to it.

**Human decisions (2026-11-06):**
- H1. Give the Counter `Test` probes a principal:
  - Delete `CreateTestTransportPolicy`.
  - Add a second sample key, registered only in `Test`, mapped to a non-admitted identity.
  - The sanitized-failure probes use that key. Missing and invalid keys assert 401.
  - Reseal the analyzer inventory for the removed underscore test.
- H2. Ship the scheme: add public `AuthenticationBuilder.AddFrontComposerMcpApiKey(string authenticationScheme = FrontComposerMcpAuthenticationDefaults.ApiKeyScheme)` and retire `CounterMcpSampleAuthenticationHandler`.
- H3. Release as breaking (5.0.0). Use a `!` subject with a `BREAKING CHANGE:` footer, and add `docs/migrations/4.7-to-5.0.md`.

**Builder decisions:**
- D1. `MapFrontComposerMcp` attaches one policy object built at map time: `IAuthorizationPolicyProvider.GetDefaultPolicyAsync()` combined with `RequireAuthenticatedUser()`. It replaces the empty `RequireAuthorization()`.
- D2. If `IAuthorizationPolicyProvider` or `IAuthenticationSchemeProvider` is not registered, mapping throws an `InvalidOperationException` naming the missing call. This check runs after the gate checks (F-7 #4).
- D3. The Development check uses `IHostEnvironment.IsDevelopment()`. Case sensitivity gives no protection, because whoever sets the environment name chooses its case (F-7 #1).
- D4. Gates are resolved in `CreateAsyncScope()`, which is disposed synchronously through `DisposeAsync()`. Gate constructors that need an active request remain a documented limitation (F-7 #2).
- D5. Decorator-wrapped `AllowAll*` gates and the SDK `MapMcp`/stdio/`McpServer.Create` bypass remain host responsibility under FR-19 and residual 4. They are documented, with no code change (F-7 #3, #5).
- D6. API-key handler outcomes:

  | Request | Result |
  |---|---|
  | No header | `NoResult` |
  | Multi-valued, blank or unmatched header | `Fail` with a fixed message; no key echoed |
  | Matched key | Identity built as listed below |

  A matched key produces a `ClaimsIdentity` with `TenantId` and `UserId` claims. Its authentication type is the scheme name. Matching goes through `FrontComposerMcpApiKeyCredentialStore`, reading the header named by `ApiKeyHeaderName`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Appended policy | The host appends an always-true assertion policy; the request is anonymous | 401 | N/A |
| Stricter default | The host `DefaultPolicy` requires a claim; the caller is authenticated without it; framework-only mapping | 403 | N/A |
| API-key only | The host uses `AddFrontComposerMcpApiKey()` as its default scheme | Valid key: 200. Missing or invalid key: 401, body free of the key | N/A |
| No auth services | Gates pass; there is no `AddAuthorization` or `AddAuthentication` | Mapping throws | Message names the missing call |
| Lowercase env | `EnvironmentName = "development"` with `AllowAll*` gates | Mapping succeeds | N/A |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpEndpointRouteBuilderExtensions.cs:21-34` -- the sites for D1 through D4.
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpApiKeyCredentialStore.cs` -- `internal` hashed, fixed-time matcher. Reuse it; do not reimplement it.
- `src/Hexalith.FrontComposer.Mcp/HttpFrontComposerMcpAgentContextAccessor.cs:82-96` -- in-handler API-key path. Leave it unchanged; it agrees with the scheme.
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpOptions.cs:18-20` -- `ApiKeyHeaderName` and `ApiKeys`.
- `samples/Counter/Counter.Web/Program.cs:188-237`, plus `CounterMcpSampleAuthenticationHandler.cs` and `CounterMcpSampleAccessPolicy.cs` -- the host. `IsAdmitted` checks the `TenantId`/`UserId` claims.
- `tests/Hexalith.FrontComposer.Shell.Tests/Samples/CounterMcpSampleAccessTests.cs:61-75` -- the loopback-policy test, which this change removes.
- `tests/e2e/specs/mcp-fail-closed-security.spec.ts:38-143`, `tests/e2e/helpers/mcp-client.ts:43-116` -- the probes. The client throws on a non-200 response, and its key defaults to `FC_E2E_MCP_API_KEY`.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Governance/McpSecurityGovernanceTests.cs:68-120` -- the HTTP host pattern. Lines 96-97 assert only metadata. `HostingTests.cs:204` maps without auth services.
- `docs/skills/frontcomposer/setup/package-and-hosting.md` -- an embedded skill resource. Keep its frontmatter and section markers.
- `docs/migrations/4.5-to-4.6.md`, `index.md`, `docs/toc.yml` -- the migration page contract.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpEndpointRouteBuilderExtensions.cs` -- Implement D1 through D4. Add XML docs explaining that the requirement cannot be removed by appended policies, and naming `AllowAnonymous()` as a host-owned bypass.
- [x] `src/Hexalith.FrontComposer.Mcp/Extensions/` + `src/Hexalith.FrontComposer.Mcp/` -- Add public `FrontComposerMcpAuthenticationDefaults` (the `ApiKeyScheme` constant), the public `AddFrontComposerMcpApiKey` extension, and an internal handler per D6. Use one file per type.
- [x] `samples/Counter/Counter.Web/*` -- Switch to `AddFrontComposerMcpApiKey()` and delete `CounterMcpSampleAuthenticationHandler.cs`. Remove `CreateTestTransportPolicy` and its `Test` mapping. Register a second key, in `Test` only, mapped to a non-admitted identity.
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/Governance/McpSecurityGovernanceTests.cs` (+ helpers) -- Add HTTP tests for every matrix row. Replace the metadata-only assertion with checks on the effective outcome. Use underscore-free names.
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/HostingTests.cs` and other mapping tests -- Register auth services where D2 now requires them.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Samples/CounterMcpSampleAccessTests.cs` + the analyzer seal -- Remove the loopback test and reseal the identifier inventory with the printed values.
- [x] `tests/e2e/specs/mcp-fail-closed-security.spec.ts` -- Missing or invalid keys on the `Test` host: assert 401 and no key echo. Move the sanitized-failure probes to the non-admitted key.
- [x] `docs/skills/frontcomposer/setup/package-and-hosting.md` -- Document the non-removable requirement, `AddFrontComposerMcpApiKey`, the D4 and D5 limits, and `AllowAnonymous()` as an explicit host opt-out.
- [x] `docs/migrations/4.5-to-4.6.md` -- Add an "MCP host composition" section covering the three v4.6.0 changes and their workarounds.
- [x] `docs/migrations/4.7-to-5.0.md` + `index.md` + `docs/toc.yml` -- Add the 5.0 page: the non-substitutable policy, `AddFrontComposerMcpApiKey`, D2 and D3, and old and new host code. Link E15R-AI-5 as pending.

**Acceptance Criteria:**
- Given Counter.Web in `Test` and in `Development`, when each host boots and receives the sample key, then `tools/list` returns the admitted inventory, and a missing or invalid key gets 401 without echoing the key.
- Given the non-admitted `Test` key, when it calls `tools/list`, `tools/call` and `resources/read`, then it gets the empty or sanitized shapes the e2e spec asserts.
- Given the existing Mcp and Shell lifecycle, admission and sample tests, when this change runs, then they pass.

## Implementation Notes

- D1-D4 live in `FrontComposerMcpEndpointRouteBuilderExtensions.ValidateHostComposition`: one `CreateAsyncScope()` (disposed via `DisposeAsync()`) resolves the gates outside `IsDevelopment()`, then the auth-service check (one message naming every missing call), then `GetDefaultPolicyAsync()` combined with `RequireAuthenticatedUser()`. The resource-collection materialization still runs first, so the `HostingTests` URI-collision test throws before D2; auth services were registered there anyway.
- D6 handler `FrontComposerMcpApiKeyAuthenticationHandler` uses the fixed `FailureMessage` "The FrontComposer MCP API key was rejected." and trims the matched TenantId/UserId exactly as `HttpFrontComposerMcpAgentContextAccessor.Create` does. `AddFrontComposerMcpApiKey` also `TryAddSingleton`s the credential store so the scheme resolves when `AddAuthentication` runs before `AddFrontComposerMcp`.
- H1 non-admitted Test key: `counter-e2e-mcp-non-admitted-key` -> `probe-tenant`/`probe-user` (`CounterMcpSampleAccessPolicy.NonAdmitted*`). Through it `tools/list` returns only `frontcomposer.lifecycle.subscribe` (the lifecycle tool is always appended), so the e2e spec asserts exactly that list.
- Matrix coverage (`McpSecurityGovernanceTests`): appended policy (object and named) -> 401 on every mapped endpoint; stricter default -> 401/403/200; API-key only -> 200 vs 401 for invalid/blank/multi-valued/unmatched with no key in bodies or Trace-level logs; no auth services (3 combinations) -> throw naming the missing call; `development`/`DEVELOPMENT`/`Development` -> mapping succeeds; plus a D4 async-only-disposable gate dependency. A mutation run against the baseline mapping failed 7 of these (the named-policy row passes on baseline by design: ASP.NET only drops the default for policy objects).
- Analyzer seal: removing `TestTransportPolicy_AdmitsOnlyLoopbackAddresses` and renaming `ApiKey_TakesPrecedence_OverIdentityClaims` to `ApiKeyWithConflictingIdentityClaimsFailsClosed` moved the tracked inventory 3586 -> 3584 and `70665957...899530` -> `1f49019f...89670b5`; HEAD matched the old seal.
- Review fix: after a key match, `HttpFrontComposerMcpAgentContextAccessor` throws `AuthFailed` when `HttpContext.User` carries a non-empty `TenantClaimTypes`/`UserClaimTypes` claim that differs from the key identity (mixed-scheme principals). This reverses the Story 8-1 "API key takes precedence" assertion, which now expects `AuthFailed`. New test files carry no public underscore declarations, so committing them does not move the seal.
- `FrontComposerMcpEndpointRouteBuilderExtensions` gained a type summary, so it left `docs/validation/api-summary-baseline.txt`. The 5.0 guide uses `fromVersion` 4.7.6 (latest published tag).
- Review pass 1: 23 triage rows. 15 were patched: the accessor conflict rule, the handler remark, the parity and conflict tests, the permissive-default and mixed-scheme governance tests, `ResponseHeadersRead`, the docs corrections and the e2e key cleanup. 8 were rejected: 5 low and 3 false. Nothing was deferred.
- Re-verification after the patches:
  - Release builds of Mcp.Tests, Counter.Web and Shell.Tests: 0 warnings, 0 errors.
  - Mcp.Tests: 490/490 unfiltered and 31/31 Governance.
  - Shell `CounterMcpSampleAccessTests` and `AnalyzerPolicyGovernanceTests`: 15/15.
  - Counter.Web `Test` gave no key / invalid / sample / non-admitted = 401/401/200/200. `Development` gave 401/401/200/401. No key appeared in a body or host log.
  - MCP e2e on chromium: 12/12. The implementer ran 51/51 across all three browsers.
  - `eng/validate-docs.ps1`: passed.

## Spec Change Log

- 2026-11-06, review pass 1.
  - *Trigger:* blind and edge-case reviewers.
    - `HttpFrontComposerMcpAgentContextAccessor` resolves the API-key header before it looks at the authorized principal.
    - So in a mixed-scheme host, a JWT can satisfy the host policy while MCP dispatches as a different API-key identity.
    - The header-first behaviour predates this change. This change exposed it by publishing the mixed-scheme recipe and claiming the scheme and the accessor "always agree".
  - *Amended:* the Code Map line "Leave it unchanged" is superseded. After a key match, the accessor must fail with `AuthFailed` when any claim of a configured tenant or user claim type on `HttpContext.User` conflicts with the matched identity.
  - *Known-bad state avoided:* an MCP dispatch identity that the host authorization policy never evaluated.
  - *How it was applied:* as a review patch, not a full loopback, because the amendment is additive and fails closed.
  - *KEEP:* everything else in the implemented diff, including D1-D6, H1-H3, the tests, the docs and the seal.

## Review Triage Log

| Iter | Source | Finding | Verdict | Evidence | Route |
|------|--------|---------|---------|----------|-------|
| 1 | blind + edge | The accessor dispatches as the API-key identity even when other authenticated credentials carry a conflicting tenant or user | medium | `HttpFrontComposerMcpAgentContextAccessor.cs:84-97` reads the header before `http.User`. With the DefaultPolicy-schemes recipe the PolicyEvaluator merges principals, so a JWT can satisfy `RequireClaim` while the key's tenant is used. The behaviour predates this change; the change exposes it and claims the two "always agree". | patch (see Change Log) |
| 1 | blind | `SchemeAndInHandlerAccessorResolveTheSameCaller` cannot detect a divergence | low | The accessor reads only the header, so both sides match the same store. It needs a conflicting-principal case. | patch |
| 1 | verification-gap | No test for a permissive host `DefaultPolicy`, so removing `RequireAuthenticatedUser()` would go unnoticed | low | Pre-verified: every HTTP test's default policy already denies anonymous callers. | patch |
| 1 | verification-gap | Copying the default policy's schemes into a mixed-scheme host is untested | low | Pre-verified: the API-key scheme is the default scheme in every test. | patch |
| 1 | verification-gap | The padded-identity parity between handler and accessor trimming is untested | low | Pre-verified: no test identity contains whitespace. | patch |
| 1 | verification-gap + blind + edge | D2 checks only that services are registered; with no default challenge scheme every request still gets a 500 | low | True, but a map-time scheme check would wrongly reject hosts whose appended policy names its schemes after mapping. Document the limit instead. | patch (docs) |
| 1 | blind + edge | The docs say anonymous requests "always get 401", but a cookie or OIDC challenge redirects | low | `package-and-hosting.md:47` and `4.7-to-5.0.md` row 1. The governance cookie test has to override the redirect to get a 401. | patch (docs) |
| 1 | blind + edge | "`AllowAnonymous()` is the only way to drop the requirement" is false | low | Host `IAuthorizationHandler`s, `IPolicyEvaluator`, `IAuthorizationMiddlewareResultHandler`, and conventions that strip metadata can also bypass it. | patch (docs) |
| 1 | blind | Making the API-key scheme the app-wide default widens what agent keys unlock | low | Docs-only: the safe pattern is an API-key-scoped policy appended to the MCP endpoints, which 5.0's union semantics now allow. Counter has no other authenticated surface. | patch (docs + test) |
| 1 | blind | The identity lacks Name/NameIdentifier claims and ignores the configured claim types | low | Frozen D6 specifies the `TenantId`/`UserId` claims, and the accessor's defaults include them. Adding claims is an enhancement. | reject |
| 1 | blind | `AddFrontComposerMcpApiKey` has no `configureOptions`; adding it later would break binaries | false | A `(string, Action<AuthenticationSchemeOptions>)` overload can be added later without a binary break. | reject |
| 1 | blind | An anonymous SSE GET admitted by a future regression would hang the HTTP probe | low | `McpHttpTestHost` uses the default `ResponseContentRead`. `ResponseHeadersRead` is a direct fix. | patch |
| 1 | blind + edge | The e2e `FC_E2E_MCP_NON_ADMITTED_API_KEY` override is a trap, and the unauthenticated helper hard-codes the sample key | low | Counter registers a compiled constant, so overriding the key yields a 401. Removing the override is a direct deletion. | patch |
| 1 | blind | The 4.5-to-4.6 guide contradicts itself: the corpus is "unchanged" while v4.6.0 changed the hosting skill, and the intro addresses Shell hosts only | low | `e5666650` changed `setup/package-and-hosting.md`. Direct doc correction. | patch (docs) |
| 1 | blind | The 4.6/4.7 workaround warns about nothing, though the old Counter handler compared keys ordinally rather than in fixed time | low | The deleted handler used `string.Equals(..., Ordinal)`. A one-sentence warning fixes it. | patch (docs) |
| 1 | blind | In the 4.7-to-5.0 samples, "local network" should be loopback, and the `using` lines are missing | low | `IPAddress.IsLoopback` admits only the same machine. Direct correction. | patch (docs) |
| 1 | blind | "Default policy read once" and "`AllowAnonymous` still skips" are untested | low | One is ASP.NET semantics; the other needs a contrived late configuration. Unlikely to regress. | reject |
| 1 | blind + edge | A throw from `DisposeAsync` in `finally` masks the original startup error | low | It needs a throwing scoped disposer during map-time rejection. The fix adds a guard. | reject |
| 1 | blind | The 5.0 guide links an internal `_bmad-output` retrospective | low | Published docs should not depend on internal planning files. Direct link removal. | patch (docs) |
| 1 | blind + edge | The HTTP "multi-valued" probe sends one comma-joined header line | low | Real, but the `Count != 1` branch is covered at unit level and the HTTP row still pins rejection of the joined form. Relabel only. | reject |
| 1 | blind | The padded-key unit case pins a rejection Kestrel never produces | false | Handler-level behaviour for non-Kestrel callers is still a contract; nothing is wrong. | reject |
| 1 | edge | A provider that returns null from `GetDefaultPolicyAsync` causes a bare `ArgumentNullException` | false | `IAuthorizationPolicyProvider.GetDefaultPolicyAsync` is declared non-nullable, so this is a host contract violation. | reject |
| 1 | edge | A null or blank matched identity throws an NRE or authenticates empty claims | low | `AddFrontComposerMcp`'s options validation rejects blank identities. This needs the scheme without the MCP registration. The fix adds a guard. | reject |

## Verification

**Commands:**
- `timeout 900 dotnet build Hexalith.FrontComposer.slnx -c Release -nodeReuse:false --disable-build-servers` -- expected: 0 warnings, 0 errors
- The built `Hexalith.FrontComposer.Mcp.Tests` executable, unfiltered and with `-trait Category=Governance` -- expected: all pass
- The built `Hexalith.FrontComposer.Shell.Tests` executable with `-class` `CounterMcpSampleAccessTests` and `AnalyzerPolicyGovernanceTests` -- expected: all pass
- Boot Counter.Web (Release) with `ASPNETCORE_ENVIRONMENT=Test`, then with `Development`, and POST `initialize` to `/mcp` with no key, an invalid key and the sample key -- expected: 401, 401, 200
- `tests/e2e`: `npx playwright test specs/mcp-*.spec.ts` (Test host, `webServer.env` mirrored) -- expected: all pass
- `pwsh eng/validate-docs.ps1`, run twice, because the first run on a cold workspace gives false failures -- expected: pass
