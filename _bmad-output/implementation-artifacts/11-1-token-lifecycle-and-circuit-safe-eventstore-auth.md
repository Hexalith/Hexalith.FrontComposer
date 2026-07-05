---
created: 2026-07-05
epic: 11
story: 1
story_key: 11-1-token-lifecycle-and-circuit-safe-eventstore-auth
source_epics: _bmad-output/planning-artifacts/epics.md
baseline_commit: 8db8b17b2576c6ec41a3a22b247327a4064d32f4
status: done
---

# Story 11.1: Token lifecycle and circuit-safe EventStore auth

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a FrontComposer operator,
I want EventStore auth tokens stored, expired, and evicted on sign-out, and acquired safely from an interactive Blazor circuit,
so the app does not silently lose EventStore connection whenever there is no `HttpContext`.

## Acceptance Criteria

1. Given `FrontComposerUserTokenStore`, when a token is stored, then its expiry is retained, expired entries are evicted, and the currently-dead `Remove` path is wired into the sign-out endpoint. (H2)

2. Given `FrontComposerAccessTokenProvider` runs inside an interactive Blazor circuit where `HttpContext` is null, when it acquires an EventStore token, then it falls back to the existing `CircuitServicesAccessor`/token-store seam used by sibling auth code, or, if no circuit-safe source is configured, fails fast at registration instead of throwing `HFC2013` only at read time. (H2, M1)

3. Given any token path, when token storage, sign-out eviction, circuit-context acquisition, logging, or exception handling runs, then no raw token is logged or exposed, and expired/sign-out eviction plus circuit-context acquisition are pinned by tests. Refines FR13; closes H2 and M1.

## Tasks / Subtasks

- [x] Audit the current auth relay and EventStore token path before editing. (AC: 1, 2, 3)
  - [x] Read `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs` completely, including `FrontComposerUserTokenStore`, `CircuitServicesAccessor`, `FrontComposerCircuitServicesHandler`, and `FrontComposerGatewayAuthorizationHandler`.
  - [x] Read `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAccessTokenProvider.cs` completely and preserve existing `HFC2013`/`HFC2014` behavior unless this story explicitly changes it.
  - [x] Read `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs`, `FrontComposerTokenRelayServiceExtensions.cs`, and `FrontComposerServerAuthenticationServiceExtensions.cs` completely before changing service registration, endpoint mapping, or token capture.
  - [x] Read the auth tests listed in "Current Files To Read Before Editing" and classify any existing dirty worktree paths before editing. Do not revert unrelated changes.

- [x] Make `FrontComposerUserTokenStore` expiry-aware and deterministic. (AC: 1, 3)
  - [x] Store token and absolute expiry together instead of storing only the raw bearer token string.
  - [x] Evict expired entries during `TryGet` and reject or immediately remove entries whose expiry is already elapsed when `Set` runs.
  - [x] Keep `Remove` idempotent and usable from endpoint code without requiring token material.
  - [x] Use an injectable deterministic time seam for tests, preferably the repo/runtime standard `TimeProvider` pattern if already available in this solution.
  - [x] Do not log raw token values, and do not place token values in exception messages, assertion output, or story evidence.

- [x] Capture token expiry at the OIDC/token-relay boundary. (AC: 1, 3)
  - [x] In `FrontComposerTokenRelayServiceExtensions`, update the `OnTokenValidated` token capture so it passes expiry into `FrontComposerUserTokenStore`.
  - [x] Prefer authoritative token metadata already available from OIDC/auth properties, such as `expires_at` or token endpoint `expires_in`; if neither exists, fail safely rather than storing an unbounded token.
  - [x] Preserve `SaveTokens = true`, existing user-id claim selection, `RequireHttpsMetadata`, and current SAML/OIDC/auth bridge behavior.

- [x] Wire sign-out eviction into the existing sign-out endpoint. (AC: 1, 3)
  - [x] In `MapHexalithFrontComposerAuthenticationEndpoints`, resolve the stable user id before `SignOutAsync` and call `FrontComposerUserTokenStore.Remove(userId)`.
  - [x] Preserve `returnUrl` sanitization, existing `SignOutAsync` scheme usage, and anonymous/missing-user behavior.
  - [x] Add endpoint coverage proving sign-out removes the stored token and does not throw for an anonymous or missing-user request.

- [x] Make EventStore access token acquisition circuit-safe. (AC: 2, 3)
  - [x] Extend `FrontComposerAccessTokenProvider` so the no-host-provider path can read from `HttpContext` when present and from the existing circuit service/token-store seam when `HttpContext` is null.
  - [x] Resolve the circuit user through `CircuitServicesAccessor` and `AuthenticationStateProvider`, matching the pattern in `FrontComposerGatewayAuthorizationHandler`/`ServerCircuitUserContextAccessor`.
  - [x] Preserve the configured host provider as the highest-priority source and preserve the GitHub OAuth no-broker `HFC2014` guard.
  - [x] If the auth bridge is configured in a mode that cannot provide a circuit-safe token source, fail fast during registration/startup with a sanitized diagnostic instead of letting normal interactive EventStore reads fail later because `HttpContext` is null.
  - [x] Keep EventStore HTTP and SignalR consumers using `EventStoreOptions.AccessTokenProvider`; do not bypass the centralized access-token provider with ad hoc token reads.

- [x] Preserve security, privacy, architecture, and existing bridge behavior. (AC: 1, 2, 3)
  - [x] Preserve sanitized `HFC2013` logging and exception behavior for empty/missing EventStore access tokens.
  - [x] Preserve `EventStoreOptions.RequireAccessToken = true` and the existing provider-replacement warning behavior.
  - [x] Do not add token storage to browser storage, URL/query strings, generated snapshots, contracts, story evidence, or logs.
  - [x] Do not change generated command routes, MCP lifecycles, projection realtime resilience, Contracts kernel split work, SourceTools, submodules, package versions, or generated output for this story.

- [x] Add focused tests and run validation. (AC: 1, 2, 3)
  - [x] Add token-store tests for storing with expiry, returning unexpired tokens, evicting expired entries, rejecting already-expired entries, and idempotent `Remove`.
  - [x] Add sign-out endpoint tests proving `Remove` is called for the current signed-in user and no raw token appears in logs or responses.
  - [x] Add access-token-provider tests for `HttpContext` saved-token path, interactive-circuit fallback, no-circuit-source fail-fast/diagnostic behavior, GitHub `HFC2014`, cancellation, empty token logging, and sanitized wrapping of provider exceptions.
  - [x] Add or extend redaction stress tests so representative bearer/JWT-like token fixtures do not appear in logs, exceptions, or test output.
  - [x] Run focused auth/Shell tests and the story artifact validator before review.
  - [x] Attempt the standard filtered solution lane with `DiffEngine_Disabled=true`; if locally blocked, record the exact command, exact blocker, whether the blocker occurred before test execution, focused fallback result, and CI authority.

### Review Findings

- [x] [Review][Patch] OIDC base-auth registration can still start without a circuit token source — AC2 requires fail-fast registration when no circuit-safe source is configured. AddHexalithFrontComposerAuthentication wires EventStoreOptions.AccessTokenProvider and registers only FrontComposerUserTokenStore/CircuitServicesAccessor, while FrontComposerCircuitServicesHandler and OIDC token capture are only added by AddHexalithFrontComposerTokenRelay/AddHexalithFrontComposerServerSecurity. Decision: patch with fail-fast validation rather than auto-composing token relay from the render-mode-agnostic auth bridge.
- [x] [Review][Patch] SignalR reconnect token acquisition still depends on ambient circuit context — AC2 covers circuit-safe EventStore token acquisition, but SignalR invokes EventStoreOptions.AccessTokenProvider later through SignalRProjectionHubConnectionFactory, after FrontComposerCircuitServicesHandler has cleared CircuitServicesAccessor.Services. Decision: patch by capturing the authenticated user/token lookup context when the projection subscription is created, so SignalR reconnects do not depend on ambient circuit state.
- [x] [Review][Patch] Expired-token eviction can remove a fresh replacement token [src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs:42]
- [x] [Review][Patch] Sign-out eviction happens before sign-out success is known [src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs:126]
- [x] [Review][Defer] Token relay hook still targets the hard-coded default OIDC scheme [src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerTokenRelayServiceExtensions.cs:47] — deferred, pre-existing

## Dev Notes

### Story Context

Epic 11 is architecture-review remediation for post-MVP release hardening. Story 11.1 is the first implementation story after the completed Story 11.0 route-contract decision gate, and the sprint tracker marks Story 11.1 as the next ready-for-dev candidate in the authoritative order. [Source: `_bmad-output/planning-artifacts/epics.md:1393`; `_bmad-output/planning-artifacts/epics.md:1422`; `_bmad-output/implementation-artifacts/sprint-status.yaml:355`]

The story exists to close two architecture review findings:

- H2: `FrontComposerUserTokenStore` stores raw bearer tokens in a singleton `ConcurrentDictionary` keyed by user id, has no expiry, and `Remove` currently has no caller. [Source: `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md:40`]
- M1: `FrontComposerAccessTokenProvider` reads only `IHttpContextAccessor` in its fallback path and throws `HFC2013` when `HttpContext` is null during normal interactive circuit activity. Sibling seams already use `CircuitServicesAccessor`. [Source: `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md:75`]

Do not reopen Epics 1-8. Do not change the Story 11.0 route contract. Story 11.7 owns generated command route implementation, and Stories 11.11-11.14 own the Contracts kernel split implementation later in the Epic 11 order. [Source: `_bmad-output/planning-artifacts/epics.md:1393`; `_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md`; `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`]

### Current Implementation Facts

- `FrontComposerUserTokenStore` currently lives in `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs` with a `ConcurrentDictionary<string, string>`, `Set(string userId, string accessToken)`, `TryGet(string userId, out string accessToken)`, and `Remove(string userId)`. It stores raw token text only and has no expiry check. [Source: `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs:17`]
- `CircuitServicesAccessor` is an `AsyncLocal<IServiceProvider?>` seam, and `FrontComposerCircuitServicesHandler` publishes circuit services during inbound circuit activity. This is the local seam to reuse for interactive circuit token fallback. [Source: `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs:37`; `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs:54`]
- `FrontComposerGatewayAuthorizationHandler` already resolves a user id first from `HttpContext.User` and then from circuit `AuthenticationStateProvider`; it attaches a bearer token when `FrontComposerUserTokenStore.TryGet` succeeds. Preserve this sibling behavior while improving expiry semantics. [Source: `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs:78`]
- `FrontComposerAccessTokenProvider` currently injects `IHttpContextAccessor`, `IOptions<FrontComposerAuthenticationOptions>`, and `ILogger`, chooses an explicitly configured host provider first, and otherwise calls `ReadHttpContextTokenAsync`. [Source: `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAccessTokenProvider.cs:11`; `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAccessTokenProvider.cs:31`]
- `ReadHttpContextTokenAsync` throws sanitized `HFC2013` when `HttpContext` is null. That is the M1 runtime failure to eliminate for normal interactive EventStore reads. [Source: `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAccessTokenProvider.cs:73`]
- `AddHexalithFrontComposerAuthentication` replaces auth seams, registers singleton `FrontComposerAccessTokenProvider`, sets `EventStoreOptions.AccessTokenProvider = tokenProvider.GetAccessTokenAsync`, and sets `RequireAccessToken = true`. Preserve this central bridge and replacement warning behavior. [Source: `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs:64`]
- `MapHexalithFrontComposerAuthenticationEndpoints` maps `/authentication/sign-out` and currently calls only `SignOutAsync`; this is the endpoint where token-store `Remove` must be wired. [Source: `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs:100`]
- `AddFrontComposerTokenRelay` registers `FrontComposerUserTokenStore`, `CircuitServicesAccessor`, `FrontComposerCircuitServicesHandler`, and `FrontComposerGatewayAuthorizationHandler`; its OIDC `OnTokenValidated` captures `TokenEndpointResponse?.AccessToken` and currently calls `tokenStore.Set(userId, token)` without expiry. [Source: `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerTokenRelayServiceExtensions.cs:30`; `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerTokenRelayServiceExtensions.cs:42`]
- `ServerCircuitUserContextAccessor` already demonstrates the fallback pattern from `HttpContext.User` to circuit `AuthenticationStateProvider` through `CircuitServicesAccessor`. Reuse that shape rather than inventing a parallel circuit abstraction. [Source: `src/Hexalith.FrontComposer.Shell/Services/Auth/ServerCircuitUserContextAccessor.cs`]
- `EventStoreHttp` and `SignalRProjectionHubConnectionFactory` consume `EventStoreOptions.AccessTokenProvider` for EventStore HTTP and SignalR calls. Fix the provider seam, not each consumer separately. [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreHttp.cs`; `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs`]

### Current Files To Read Before Editing

Read each likely UPDATE file completely before changing it:

- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs` - current token store, circuit service seam, circuit handler, and gateway auth handler.
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAccessTokenProvider.cs` - EventStore access-token provider and `HFC2013`/`HFC2014` behavior.
- `src/Hexalith.FrontComposer.Shell/Services/Auth/ServerCircuitUserContextAccessor.cs` - existing circuit fallback pattern.
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs` - auth bridge registration and sign-out endpoint.
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerTokenRelayServiceExtensions.cs` - token relay registration and OIDC token capture.
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerServerAuthenticationServiceExtensions.cs` - server security composition with token relay.
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthenticationOptions.cs` - auth and token relay options.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreAccessTokenGuard.cs` - EventStore missing-token guard.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreHttp.cs` - EventStore HTTP bearer-token application.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs` - EventStore SignalR access-token provider flow.
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAccessTokenProviderTests.cs` - current provider coverage, including the test that currently expects `HFC2013` when `HttpContext` is absent.
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerServerSecurityServiceExtensionsTests.cs` - token relay and gateway handler tests; likely location for token-store/sign-out additions unless a dedicated auth test file is clearer.
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerAuthenticationServiceExtensionsTests.cs` - auth bridge/EventStore provider registration tests.
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/AuthRedactionStressTests.cs` - redaction stress tests for logs/exceptions.
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/AuthBoundaryTests.cs` - auth boundary and token-storage architecture checks.

### Architecture Compliance

- Keep the fix inside the Shell/auth/EventStore integration layer. Do not add .NET 10, Blazor, auth, EventStore, SignalR, UI, or testing runtime types to the `Contracts` kernel. Story 11.11+ owns the Contracts UI split. [Source: `_bmad-output/project-context.md`; `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`]
- Preserve the existing EventStore bearer-token option path. The command/status contract says FrontComposer must not introduce tenant/user storage or cross-circuit replay in pending state for EventStore status work. [Source: `_bmad-output/contracts/fc-cmd-eventstore-status-endpoint-contract-2026-06-04.md`]
- Follow existing service lifetimes. `FrontComposerUserTokenStore` and `CircuitServicesAccessor` are singleton seams today; if the implementation changes lifetimes, prove there is no cross-user/cross-circuit leakage and preserve circuit safety.
- Follow repository style: .NET 10, nullable enabled, `TreatWarningsAsErrors=true`, `ConfigureAwait(false)`, one C# type per file for new top-level types, central package management, `.slnx` solution commands, and no generated output churn. [Source: `_bmad-output/project-context.md`]
- For UI-visible or support-facing errors, keep copy support-safe and avoid exposing tokens, raw metadata, stack traces, or framework implementation details. [Source: `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md`]

### Anti-Patterns To Avoid

- Do not make `FrontComposerAccessTokenProvider` depend only on `IHttpContextAccessor` for interactive circuit reads.
- Do not store bearer tokens without expiry, with process-lifetime retention, or in browser-visible storage.
- Do not log token text, JWT-like payloads, authentication properties containing access tokens, or raw exception strings that may contain token material.
- Do not silently return an empty access token when `RequireAccessToken` is true.
- Do not add broad auth abstractions or cross-module refactors. Story 11.17 owns mechanical type/file decomposition later.
- Do not use `git submodule update --recursive` or `--remote`, and do not modify `references/Hexalith.*` submodules for this story.

### Testing Requirements

- Minimum focused lane should include the Shell auth tests touched by this story. Prefer targeted `dotnet test` filters first, then run the Shell test project if feasible.
- Required story artifact gate before review:
  `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/11-1-token-lifecycle-and-circuit-safe-eventstore-auth.md`
- Required broad lane when feasible:
  `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`
- If local VSTest sockets, restore, vulnerability feed access, or environment permissions block validation, record the exact command and exact failure. Do not mark validation complete without focused fallback evidence.

### Latest Technical Information

- Microsoft Learn guidance for ASP.NET Core Blazor states that `IHttpContextAccessor` should generally be avoided with interactive rendering because `HttpContext` can be null during interactive rendering and Blazor apps run outside the normal ASP.NET Core request pipeline. This directly supports Story 11.1's circuit-safe fallback requirement. Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/httpcontext?view=aspnetcore-10.0
- Microsoft Learn's Blazor security guidance allows server-side token access through `HttpContext` for static SSR/prerendering scenarios, but warns that tokens are not updated if a user authenticates after the circuit is established and calls out the `AsyncLocal` behavior. Preserve the existing `HttpContext` saved-token path, but do not rely on it as the only interactive source. Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/security/additional-scenarios?view=aspnetcore-10.0
- Microsoft Learn's SignalR configuration guidance documents the .NET client's `AccessTokenProvider` as the supported way to supply bearer tokens for `WithUrl` connections. Keep EventStore SignalR flowing through `EventStoreOptions.AccessTokenProvider`. Source: https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration?view=aspnetcore-10.0
- Microsoft Learn's Blazor fundamentals guidance shows `CircuitHandler` as an appropriate way to capture circuit/user state from `AuthenticationStateProvider` into services. That matches the existing `CircuitServicesAccessor` seam in this codebase. Source: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr?view=aspnetcore-10.0

### Previous Story Intelligence

Story 11.0 is complete and selected `/commands/{BoundedContext}/{CommandTypeName}` as the canonical generated command route family. This story must not change route activation or command route implementation. [Source: `_bmad-output/implementation-artifacts/11-0-command-projection-route-contract-decision-gate.md`; `_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md`]

The pre-existing `bmad-dev-auto` attempt for Story 11.1 is blocked because implementation work was attempted on `main` with a dirty worktree. Treat that as workflow evidence only; it does not block create-story, and this story file deliberately records the current unrelated dirty paths. [Source: `_bmad-output/implementation-artifacts/bmad-dev-auto-result-11-1-token-lifecycle-and-circuit-safe-eventstore-auth.md`; `git status --short`]

### Git Intelligence

Current worktree at story creation has the following unrelated dirty paths:

- `references/Hexalith.Memories` - modified submodule pointer or nested state; do not alter without explicit approval.
- `_bmad-output/implementation-artifacts/bmad-dev-auto-result-11-1-token-lifecycle-and-circuit-safe-eventstore-auth.md` - pre-existing untracked automation result.
- `_bmad-output/implementation-artifacts/epic-11-context.md` - pre-existing untracked Epic 11 context artifact.

Do not revert or include these paths in story-owned implementation changes unless the user explicitly redirects the work.

## Documented Unrelated Changes

These paths were dirty before Story 11.1 creation and are unrelated to the create-story artifact work. They are intentionally not listed as story-owned implementation files.

- `references/Hexalith.Memories` - Pre-existing unrelated submodule workspace state; do not modify without explicit approval.
- `_bmad-output/implementation-artifacts/bmad-dev-auto-result-11-1-token-lifecycle-and-circuit-safe-eventstore-auth.md` - Pre-existing unrelated automation result.
- `_bmad-output/implementation-artifacts/epic-11-context.md` - Pre-existing unrelated Epic 11 context artifact.

### Project Structure Notes

- Story file location: `_bmad-output/implementation-artifacts/11-1-token-lifecycle-and-circuit-safe-eventstore-auth.md`.
- Sprint-status key: `11-1-token-lifecycle-and-circuit-safe-eventstore-auth`.
- Primary implementation area: `src/Hexalith.FrontComposer.Shell/Services/Auth/` and `src/Hexalith.FrontComposer.Shell/Extensions/`.
- Primary test area: `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/` and `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/`.
- Avoid `docs/_site/**`, `obj/**`, submodules, generated output, package version files, route contract artifacts, MCP artifacts, and Contracts split files for this story.

### References

- Source: `_bmad-output/planning-artifacts/epics.md` - Epic 11 source of record, Story 11.1 acceptance criteria, and authoritative implementation order.
- Source: `_bmad-output/planning-artifacts/prd.md` - FR12, FR29, NFRs, runtime stack, EventStore/Tenants external system context.
- Source: `_bmad-output/planning-artifacts/prd-addendum-2026-07-05.md` - post-readiness remediation context.
- Source: `_bmad-output/planning-artifacts/architecture.md` - Epic 11 technical spine and Shell/auth boundaries.
- Source: `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` - H2 and M1 findings this story closes.
- Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md` - original corrective Story 11.1 framing.
- Source: `_bmad-output/planning-artifacts/ux-design.md`, `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md`, and `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md` - support-safe error/state copy constraints.
- Source: `_bmad-output/implementation-artifacts/11-0-command-projection-route-contract-decision-gate.md` - completed prior story and current Epic 11 unblocker.
- Source: `_bmad-output/contracts/fc-route-generated-command-route-contract-2026-07-05.md` - route contract not owned by this story.
- Source: `_bmad-output/contracts/fc-cmd-eventstore-status-endpoint-contract-2026-06-04.md` - EventStore auth/status contract constraints.
- Source: `_bmad-output/project-context.md` - project stack, coding, testing, docs, and submodule rules.
- Source: `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs` - current token store/circuit/gateway implementation.
- Source: `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAccessTokenProvider.cs` - current EventStore token acquisition fallback.
- Source: `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs` - auth bridge registration and sign-out endpoint.
- Source: `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerTokenRelayServiceExtensions.cs` - OIDC token relay capture.
- Source: `src/Hexalith.FrontComposer.Shell/Services/Auth/ServerCircuitUserContextAccessor.cs` - existing circuit user fallback pattern.
- Source: `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAccessTokenProviderTests.cs` - current provider tests.
- Source: `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerServerSecurityServiceExtensionsTests.cs` - token relay/gateway tests.
- Source: `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerAuthenticationServiceExtensionsTests.cs` - auth bridge/EventStore provider tests.
- Source: `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/AuthRedactionStressTests.cs` - no-token-leak stress coverage.
- Source: `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/AuthBoundaryTests.cs` - auth boundary governance.
- Source: Microsoft Learn Blazor `HttpContext` guidance - https://learn.microsoft.com/en-us/aspnet/core/blazor/components/httpcontext?view=aspnetcore-10.0
- Source: Microsoft Learn Blazor security additional scenarios - https://learn.microsoft.com/en-us/aspnet/core/blazor/security/additional-scenarios?view=aspnetcore-10.0
- Source: Microsoft Learn SignalR configuration - https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration?view=aspnetcore-10.0
- Source: Microsoft Learn Blazor SignalR fundamentals - https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr?view=aspnetcore-10.0

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-05: Create-story analysis loaded root Hexalith LLM instructions, BMAD create-story workflow/config/discovery/template/checklist, project context, sprint status, Epic 11 source, PRD/addendum, architecture, UX artifacts, architecture quality review findings, sprint change proposal, Story 11.0, FC-ROUTE contract, EventStore status contract, live Shell auth/EventStore source, auth tests, current git status, and Microsoft Learn Blazor/SignalR guidance.
- 2026-07-05: Confirmed Story 11.1 had no existing story file and sprint status was `backlog` before story creation.
- 2026-07-05: Validated story context against the create-story checklist by adding concrete UPDATE files, current implementation facts, Microsoft Learn circuit/AuthContext evidence, acceptance-test requirements, architecture constraints, anti-patterns, previous-story intelligence, and current dirty-worktree classification.
- 2026-07-05: Dev-story activation loaded BMAD workflow customization, root and submodule project context files, complete Story 11.1, sprint status, required Shell auth/EventStore source files, required auth tests, and current dirty-worktree state. Existing unrelated dirty paths preserved: `references/Hexalith.Memories`, `_bmad-output/implementation-artifacts/bmad-dev-auto-result-11-1-token-lifecycle-and-circuit-safe-eventstore-auth.md`, and `_bmad-output/implementation-artifacts/epic-11-context.md`.
- 2026-07-05: Red phase added focused tests for expiry-aware token storage, OIDC expiry capture, sign-out eviction, circuit fallback, stable subject-key lookup, no-source fail-fast validation, and token redaction. Initial focused auth run failed before production changes on missing `FrontComposerUserTokenStore` expiry APIs and new access-provider constructor/source behavior, as expected.
- 2026-07-05: Green/refactor phase implemented expiry-aware `FrontComposerUserTokenStore`, OIDC `expires_at`/`expires_in` capture, sign-out token eviction, circuit-safe EventStore token fallback through `CircuitServicesAccessor` + `AuthenticationStateProvider`, stable `sub`/NameIdentifier token-store lookup, and fail-fast HFC2013 validation for provider modes without a brokered source.
- 2026-07-05: Validation passed: focused Release auth/governance lane 71/71; Shell default Release lane 2061/2061; standard filtered solution lane passed with Contracts 177, CLI 67, MCP 358, SourceTools 1045, Shell 2063, Testing 30, and Bench reporting no matching non-performance tests; Release solution build passed 0 warnings / 0 errors.
- 2026-07-05: Code review applied four patches: fail-fast OIDC base-auth validation without circuit-safe relay, SignalR reconnect token-provider capture, exact stale-token cleanup, and post-success sign-out token eviction. Review validation passed focused Shell review lane 81/81, full Shell project lane 2074/2074, and the standard filtered solution lane with Contracts 177, CLI 67, MCP 358, Shell 2069, SourceTools 1045, Testing 30, and Bench reporting no matching non-performance tests.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-07-05.
- Story status set to `ready-for-dev`.
- Sprint status updated so Epic 11 is `in-progress` and Story 11.1 is `ready-for-dev`.
- No source code was changed by story creation.
- Implemented expiry-aware per-user token retention using `TimeProvider`, evicting expired entries during reads and rejecting already-expired writes without logging token material.
- Captured OIDC token expiry from saved `expires_at` metadata or token endpoint `expires_in`; tokens without expiry are not stored unbounded.
- Wired sign-out endpoint eviction for the stable authenticated user id while preserving return-url sanitization, existing sign-out scheme behavior, and anonymous sign-out behavior.
- Extended EventStore access-token acquisition to preserve host-provider priority, keep `HttpContext.GetTokenAsync` for saved-token paths, and fall back to the circuit token store when `HttpContext` is absent.
- Added fail-fast HFC2013 option validation for SAML/custom/GitHub-allowed modes without a brokered host access-token provider, and preserved HFC2014 for GitHub OAuth without a broker.
- Added focused tests for token expiry, sign-out eviction, circuit fallback, stable subject-key lookup, fail-fast validation, and JWT-shaped token redaction.
- Story moved to `review` after focused, Shell, solution, Release build, and story-artifact validation passed.
- Code review patches resolved the remaining high/medium review findings and kept the hard-coded OIDC token-relay scheme as deferred pre-existing work.
- Story moved to `done` after focused review tests, the full Shell test project, the standard filtered solution lane, and story artifact validation passed.

### File List

- `_bmad-output/implementation-artifacts/11-1-token-lifecycle-and-circuit-safe-eventstore-auth.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerServerAuthenticationServiceExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerTokenRelayServiceExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthenticationOptions.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAccessTokenProvider.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAuthenticationOptionsValidator.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerTokenRelay.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerAuthenticationServiceExtensionsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerServerSecurityServiceExtensionsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/AuthRedactionStressTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAccessTokenProviderTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAuthenticationOptionsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerUserTokenStoreTests.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`

### Change Log

- 2026-07-05: Created Story 11.1 handoff artifact and moved sprint tracker state from `backlog` to `ready-for-dev`.
- 2026-07-05: Implemented token expiry, sign-out eviction, circuit-safe EventStore auth fallback, sanitized fail-fast validation, and focused auth/redaction tests; moved story to review.
- 2026-07-05: Applied code-review patches for fail-fast relay validation, SignalR reconnect token capture, stale-token cleanup safety, and sign-out eviction ordering; moved story to done.

## Auto Run Result

Status: blocked
Blocking condition: version-control sanity check failed: current branch is `main` for implementation work and the working tree is dirty with `_bmad-output/implementation-artifacts/sprint-status.yaml`, `references/Hexalith.Memories`, `_bmad-output/implementation-artifacts/11-1-token-lifecycle-and-circuit-safe-eventstore-auth.md`, `_bmad-output/implementation-artifacts/bmad-dev-auto-result-11-1-token-lifecycle-and-circuit-safe-eventstore-auth.md`, and `_bmad-output/implementation-artifacts/epic-11-context.md`.
