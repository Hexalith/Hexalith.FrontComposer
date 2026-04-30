# Story 7.1: OIDC/SAML Authentication Integration

Status: ready-for-dev

> **Epic 7** - Authentication, Authorization & Multi-Tenancy. Covers **FR37**, prepares **FR35 / FR46**, and enforces **NFR20 / NFR21 / NFR102**. Applies lessons **L01**, **L03**, **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 7-1 gives FrontComposer a standard authentication bridge without creating a framework-owned login UI:

- Add a FrontComposer authentication registration layer that wires ASP.NET Core authentication state into existing framework seams: `IUserContextAccessor`, `IAuthRedirector`, and `EventStoreOptions.AccessTokenProvider`.
- Support one configured identity provider per deployment for v1, with explicit provider recipes for Keycloak, Microsoft Entra ID, Google, and GitHub.
- Treat OIDC as the primary path for Keycloak, Entra, and Google. GitHub sign-in uses GitHub's OAuth web application flow unless the adopter fronts GitHub through an OIDC broker such as Keycloak or Entra External ID. Do not pretend GitHub OAuth tokens are JWT bearer tokens for EventStore.
- Support SAML through ASP.NET Core authentication handler integration, with Sustainsys.Saml2 as the default documented OSS handler unless implementation research selects a better maintained package before coding starts.
- Extract only framework-required identifiers: `TenantId` and `UserId`. No profile PII, email display names, token bodies, or provider claims are stored, logged, cached, or sent to diagnostics.
- Preserve the existing fail-closed user/tenant contract: unauthenticated or claim-missing contexts produce null/empty `IUserContextAccessor` values and the EventStore clients refuse to send commands/queries.
- Prefer server/BFF cookie-backed token storage for Blazor Auto and Blazor Server. Standalone WASM remains supported through the standard `IAccessTokenProvider`-style adapter but the framework must not write raw JWTs to `localStorage`.
- Keep Story 7-2 as the owner for full tenant propagation and isolation enforcement across every operation. Story 7-1 owns auth bootstrap, token validation, claim extraction, redirect, and bearer-token relay only.

---

## Story

As a developer,
I want to integrate standard identity providers via OIDC/SAML without building a custom authentication UI,
so that users authenticate with their existing corporate or social identity and I do not maintain auth UI code.

### Adopter Job To Preserve

An adopter should be able to point FrontComposer at a standard enterprise or social identity provider, challenge unauthenticated users through the provider's login page, and have FrontComposer's existing command/query/SignalR clients receive a validated user context and bearer token without leaking token contents or profile PII into framework state.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | FrontComposer authentication is configured for OIDC | The host starts | The framework registers an ASP.NET Core OpenID Connect challenge path, a cookie-backed sign-in/session scheme for server-side hosts, and the existing FrontComposer seams needed by Shell/EventStore. |
| AC2 | FrontComposer authentication is configured for SAML | The host starts | The framework accepts a SAML authentication handler configuration and bridges its resulting `ClaimsPrincipal` through the same FrontComposer user-context and redirect seams as OIDC. |
| AC3 | Keycloak, Microsoft Entra ID, Google, or GitHub is selected | The provider recipe is configured | Keycloak, Entra, and Google use OIDC discovery/authorization-code flow; GitHub uses OAuth web flow or an adopter-supplied OIDC broker. The story documents this protocol distinction and tests the selected recipe behavior. |
| AC4 | No provider is selected or more than one provider is selected | Options validation runs | Startup fails with a teaching diagnostic naming the expected single-provider v1 constraint and the fix. |
| AC5 | An unauthenticated user navigates to an authenticated FrontComposer route or a 401 response reaches `IAuthRedirector` | The auth flow triggers | The user is redirected/challenged through the configured provider, not through a framework-owned login UI. |
| AC6 | Authentication succeeds | The provider returns to the application callback | ASP.NET Core validates issuer/signature/expiry/audience through the configured handler and exposes an authenticated `ClaimsPrincipal` to FrontComposer. |
| AC7 | A validated principal contains configured tenant and user claims | FrontComposer reads authentication state | `IUserContextAccessor.TenantId` and `UserId` return normalized non-empty identifiers for framework operations. |
| AC8 | Tenant or user claim is missing, empty, whitespace, or contains `:` | FrontComposer reads authentication state | The accessor fails closed by returning unauthenticated/null-equivalent values or by surfacing a clear configuration diagnostic; commands, queries, subscriptions, and cache writes do not proceed with synthetic defaults. |
| AC9 | EventStore-backed command, query, or SignalR subscription code needs an access token | The operation sends outbound traffic | `EventStoreOptions.AccessTokenProvider` supplies a fresh bearer token through the configured token relay path, and the clients set `Authorization: Bearer <token>` without logging the token. |
| AC10 | Token acquisition fails, returns empty, or the session expired | A command/query/subscription tries to send | The framework surfaces re-authentication through `IAuthRedirector` or an explicit fail-fast auth exception. It does not retry with an old token or send anonymous requests when `RequireAccessToken` is true. |
| AC11 | Blazor Server or Blazor Auto is used | Authentication state is stored | Tokens are kept in server-side authentication/session mechanisms or BFF/cookie-backed flow. Raw JWTs are never written to browser `localStorage` by FrontComposer. |
| AC12 | Standalone Blazor WebAssembly is used | Authentication state is managed | FrontComposer consumes the platform auth/token abstraction supplied by the host and still avoids direct raw-token `localStorage` writes in framework code. |
| AC13 | Framework diagnostics, logs, or telemetry observe auth events | The app authenticates, redirects, or fails validation | Logs include provider kind, diagnostic ID, claim presence booleans, and sanitized categories only. They exclude tokens, email, display name, raw claim values, tenant/user IDs, and provider payloads. |
| AC14 | Counter sample runs without real auth | The developer starts the sample | The sample keeps its existing demo `IUserContextAccessor` path, but adds an opt-in fake OIDC/test-auth fixture or documented configuration proving the real authentication bridge without requiring external IdP credentials. |
| AC15 | Story 7-1 completes | A developer prepares Story 7-2 | The resulting seams make tenant propagation implementable without changing provider login code: `TenantId`, `UserId`, token relay, redirect, and fail-closed behavior are all covered by tests. |

---

## Tasks / Subtasks

- [ ] T1. Define FrontComposer authentication options and validation (AC1-AC4, AC13)
  - [ ] Add `FrontComposerAuthenticationOptions` under `src/Hexalith.FrontComposer.Shell/Options/` or `Services/Auth/` with provider kind, scheme names, tenant/user claim names, allowed issuer/audience settings, return URL behavior, and token relay settings.
  - [ ] Enforce exactly one provider per deployment for v1: OIDC, SAML, GitHub OAuth, or custom/brokered provider. More than one configured provider is a startup error.
  - [ ] Add options validation that fails on missing provider, missing tenant/user claim names, missing authority/metadata address, insecure HTTP authority outside development, and unsupported provider/protocol combinations.
  - [ ] Add HFC20xx Shell diagnostics for invalid auth configuration, claim extraction failure, token relay failure, and GitHub token-exchange requirement. Use existing `FcDiagnosticIds` patterns and do not reuse IDs.
  - [ ] Add tests for every validation branch, including provider-specific fix text and docs-link shape.

- [ ] T2. Add authentication registration extensions (AC1-AC5, AC11-AC12)
  - [ ] Add `AddHexalithFrontComposerAuthentication(...)` in `src/Hexalith.FrontComposer.Shell/Extensions/` that composes with `AddHexalithFrontComposer()` and `AddHexalithEventStore()`.
  - [ ] Register `IHttpContextAccessor` only where needed for server-side claim/token access; do not add HTTP pipeline dependencies to Contracts.
  - [ ] For OIDC, integrate with ASP.NET Core `AddAuthentication().AddCookie().AddOpenIdConnect(...)` or an equivalent authenticated BFF/cookie pattern. Keep provider UI external.
  - [ ] For SAML, expose a handler-configuration hook instead of hard-coding every SAML option in FrontComposer. If Sustainsys.Saml2 remains the selected package, add it as an optional documented package and pin it centrally when implementation starts.
  - [ ] For GitHub, support OAuth sign-in as an identity provider recipe but require an adopter token-exchange/broker path before treating any token as EventStore bearer JWT.
  - [ ] Add DI tests proving the extension replaces the default `NullUserContextAccessor` and `NoOpAuthRedirector` only when auth is configured.

- [ ] T3. Implement claims-based `IUserContextAccessor` adapters (AC6-AC8, AC13, AC15)
  - [ ] Add a server-side `ClaimsPrincipalUserContextAccessor` that reads the authenticated principal from `IHttpContextAccessor` or `AuthenticationStateProvider` according to host mode.
  - [ ] Add a WASM-friendly adapter seam that can read claims from the host authentication state without FrontComposer owning token storage.
  - [ ] Normalize tenant/user claim values by trimming and rejecting null, empty, whitespace, and colon-containing values.
  - [ ] Do not lowercase tenant IDs. Preserve existing storage-key precedent: tenants may be case-sensitive; user ID canonicalization beyond trimming is an explicit policy decision and must be documented if added.
  - [ ] Return unauthenticated/null-equivalent values on missing context so existing fail-closed consumers continue to short-circuit.
  - [ ] Add tests for claim aliases, missing claims, whitespace, colon rejection, unauthenticated principal, and no leakage of raw claim values in logs/diagnostics.

- [ ] T4. Bridge redirect and token relay seams (AC5, AC9-AC12)
  - [ ] Implement an `IAuthRedirector` that triggers the configured challenge/sign-in route and preserves a sanitized return URL.
  - [ ] Reject absolute or cross-origin return URLs; only local return paths are allowed.
  - [ ] Implement a token provider bridge for `EventStoreOptions.AccessTokenProvider` that can retrieve current access tokens from the configured ASP.NET Core authentication session or host-supplied token accessor.
  - [ ] Ensure command/query clients keep their existing `RequireAccessToken` fail-fast behavior when token acquisition fails.
  - [ ] Add tests proving tokens are applied to command/query HTTP requests and SignalR access-token callback paths without logging token values.
  - [ ] Cover cancellation: if token acquisition observes cancellation, no redirect, cache mutation, or partial send occurs.

- [ ] T5. Provider recipes and fixtures (AC3, AC6, AC9, AC14)
  - [ ] Add provider recipe tests/fixtures for Keycloak OIDC discovery, Entra issuer/audience validation, Google OIDC standard claims, GitHub OAuth challenge/token-exchange requirement, and SAML handler bridging.
  - [ ] Use fake handlers or local test doubles. Do not require live Keycloak, Entra, GitHub, Google, or SAML IdP credentials in CI.
  - [ ] Document required provider metadata: authority/metadata address, client ID, client secret storage expectations, redirect callback path, sign-out callback path, scopes, tenant claim, user claim, audience, and issuer.
  - [ ] Ensure provider recipes include both development and production notes. Development may use user secrets or fake handlers; production must use secure secret storage owned by the host.
  - [ ] Add sample `appsettings` snippets with placeholder values only. Never commit real secrets or tenant-specific examples.

- [ ] T6. Secure token and storage behavior (AC9-AC13)
  - [ ] Audit framework code paths for raw token persistence; add a regression test that fails if FrontComposer writes access/refresh/ID token names or JWT-looking values to `IStorageService` or local-storage wrappers.
  - [ ] Add a structured logging redaction test with JWT-like strings, email addresses, tenant IDs, and user IDs in exception messages and claims.
  - [ ] Keep ETag/session persistence keyed by `IUserContextAccessor`; do not introduce alternate auth-specific cache keys.
  - [ ] Ensure token refresh or re-auth errors cannot leave stale bearer tokens in `EventStoreOptions` or long-lived delegates.

- [ ] T7. Counter sample and developer experience (AC5, AC14)
  - [ ] Preserve `DemoUserContextAccessor` as the default Counter local-development path.
  - [ ] Add an opt-in fake auth mode for Counter or a test-only sample host that demonstrates the registration extension and authenticated route behavior without external credentials.
  - [ ] Add a short README section showing where adopters replace the demo stub with the auth bridge.
  - [ ] Show the exact registration order with `AddHexalithFrontComposerQuickstart`, `AddHexalithFrontComposerAuthentication`, `AddHexalithEventStore`, and `services.Replace` patterns where needed.

- [ ] T8. Tests and verification (AC1-AC15)
  - [ ] Shell options validation tests for every invalid provider setup.
  - [ ] DI registration tests proving default null/no-op seams are replaced only when configured.
  - [ ] Claims adapter tests for OIDC, SAML, GitHub OAuth, unauthenticated, missing claim, colon claim, whitespace claim, and claim-alias behavior.
  - [ ] Redirector tests for challenge route, local return URL, absolute URL rejection, and cancellation.
  - [ ] Token relay tests for command, query, and SignalR subscription clients.
  - [ ] Redaction tests proving no token/profile/claim payload leakage to logs, diagnostics, telemetry, cache, or `IStorageService`.
  - [ ] Counter/sample tests for demo mode preserved and fake-auth mode working.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [ ] Targeted tests: `tests/Hexalith.FrontComposer.Shell.Tests` auth, EventStore, and sample-host lanes.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Rendering/IUserContextAccessor.cs` | Flat `TenantId` / `UserId` abstraction. Null, empty, or whitespace means unauthenticated. | Do not change the interface shape. Existing consumers rely on `string.IsNullOrWhiteSpace` fail-closed behavior. |
| `src/Hexalith.FrontComposer.Shell/Services/Auth/NoOpAuthRedirector.cs` | Default `IAuthRedirector` throws on 401 so auth failures are never swallowed. | Auth bridge replaces this only when configured; default fail-fast stays. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptions.cs` | `RequireAccessToken = true`; `AccessTokenProvider` supplies bearer token per operation. | Keep per-operation token acquisition. Do not cache token strings in options or services. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreIdentity.cs` | Requires authenticated tenant context; rejects requested tenant mismatch and colon-containing segments. | Keep tenant mismatch fail-fast. Story 7-2 extends propagation; 7-1 must not loosen this. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs` | Reads optional command `TenantId`, requires matching authenticated tenant, applies bearer header, redacts identifiers in logs. | Do not log tokens, raw tenant/user, or command payload while adding auth. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs` | Requires user context, uses tenant/user for ETag cache keys, invokes `IAuthRedirector` on 401. | Keep cache correctness from server reconciliation; do not serve cross-tenant cache entries. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs` | SignalR access token callback has no cancellation token, a known framework limitation. | Do not block reconnect loops on long token refresh without tests and documented timeout behavior. |
| `samples/Counter/Counter.Web/Program.cs` | Replaces `IUserContextAccessor` with `DemoUserContextAccessor` for local demo behavior. | Preserve no-credential demo path. Real auth is opt-in. |

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 2-2 `IUserContextAccessor` | Story 7-1 auth bridge | 7-1 supplies a real claims-backed implementation without changing the flat `TenantId` / `UserId` interface. |
| Story 3-1 storage-key fail-closed migration | Story 7-1 claims handling | Missing tenant/user keeps persistence disabled rather than falling back to `"default"` or `"anonymous"`. |
| Story 5-1/5-2 EventStore clients | Story 7-1 token relay | `EventStoreOptions.AccessTokenProvider` remains the single bearer-token seam for commands, queries, and SignalR. |
| Story 5-2 `IAuthRedirector` | Story 7-1 redirect bridge | 7-1 replaces the fail-fast default with a configured challenge redirector. 401 handling stays centralized. |
| Story 7-2 tenant propagation | Story 7-1 claim extraction | 7-1 proves `TenantId` / `UserId` extraction; 7-2 owns propagation through all command/query/subscription/cache/MCP surfaces. |
| Story 7-3 authorization policies | Story 7-1 auth state | 7-1 supplies authenticated principals and role/policy claims; 7-3 owns `[RequiresPolicy]` and policy evaluation. |
| Story 9-5 docs site | Story 7-1 provider recipes | 7-1 creates concise recipe material and docs links; 9-5 turns them into Diataxis pages. |

### Provider Strategy

| Provider | v1 protocol path | Notes |
| --- | --- | --- |
| Keycloak | OIDC or SAML | Prefer OIDC discovery for new deployments. SAML remains available for enterprise IdP parity. |
| Microsoft Entra ID | OIDC | Use ASP.NET Core OIDC / Microsoft Identity Web-compatible configuration. Validate issuer and audience explicitly. |
| Google | OIDC | Use Google OpenID Connect discovery/claims. Tenant claim must come from adopter mapping because Google accounts do not naturally define FrontComposer tenant IDs. |
| GitHub | OAuth web flow or OIDC broker | GitHub's app user sign-in path is OAuth web flow. If EventStore requires a JWT, the adopter must exchange/broker GitHub identity into an app JWT before `AccessTokenProvider` returns it. |
| Generic enterprise SAML | SAML handler | Use ASP.NET Core authentication scheme integration. FrontComposer consumes the resulting `ClaimsPrincipal`, not raw SAML assertions. |

### Binding Decisions

| ID | Decision | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | Story 7-1 uses ASP.NET Core authentication handlers and ships no login UI. | The PRD explicitly forbids custom auth UI and provider UI belongs to the IdP. | Build framework-owned login pages or account management screens. |
| D2 | One provider per deployment in v1. | Matches Epic 7 AC and keeps auth state, redirect, and token relay testable for a solo maintainer. | Simultaneous Keycloak + Entra + GitHub selection in v1. |
| D3 | `IUserContextAccessor` stays flat and claim-backed. | Existing Shell/EventStore services already consume this abstraction. | Add a new `ITenantContext` hierarchy or pass `ClaimsPrincipal` through framework services. |
| D4 | GitHub support is honest OAuth support unless brokered to OIDC/JWT. | GitHub OAuth app tokens are not the same as OIDC/JWT bearer tokens for EventStore. | Treat GitHub OAuth access tokens as framework JWTs. |
| D5 | Token storage is host-auth owned, not FrontComposer-owned. | Prevents raw JWT `localStorage` writes and keeps security posture aligned with Microsoft guidance. | Store access/refresh tokens in `IStorageService` for convenience. |
| D6 | Missing or invalid tenant/user claims fail closed. | L03 tenant/user isolation is already established across storage, derived values, and EventStore. | Fall back to `"default"` tenant or `"anonymous"` user in production. |
| D7 | SAML support is handler-based and claim-normalized. | SAML assertions are protocol payloads; FrontComposer only needs a validated principal. | Parse SAML assertions directly in framework code. |
| D8 | Provider recipe tests use fake/local handlers, not live IdP CI. | Live IdP credentials make CI brittle and leak-prone. | Require Keycloak/Entra/GitHub/Google test tenants for every PR. |
| D9 | Auth diagnostics log categories and presence booleans only. | Auth failures are adjacent to tokens and PII. Logs must not create a new leak surface. | Log full claim sets or exception messages for debugging convenience. |
| D10 | Story 7-1 does not implement authorization policies. | `[RequiresPolicy]` is Story 7-3; auth state is the prerequisite. | Add policy evaluation and button hiding in this story. |

### Library / Framework Requirements

- Target the repository's current .NET 10 / Blazor Auto / ASP.NET Core authentication stack.
- Use ASP.NET Core authentication/authorization primitives already available through the Shell project's `Microsoft.AspNetCore.App` framework reference where possible.
- Add external provider packages only when required and pin them in `Directory.Packages.props`.
- Do not add ASP.NET Core, OAuth/OIDC, SAML, or provider-specific dependencies to `Hexalith.FrontComposer.Contracts`.
- Use Blazor `AuthenticationStateProvider` and ASP.NET Core `ClaimsPrincipal` patterns rather than custom principal models.
- Use authorization code flow for OIDC. Do not enable implicit or hybrid flows for server-side/BFF paths.
- Keep provider secrets in host configuration/user secrets/secure deployment secret stores. Do not place secrets in sample JSON.

External references checked on 2026-04-30:

- Microsoft Learn: Secure an ASP.NET Core Blazor Web App with OpenID Connect: https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-10.0
- Microsoft Learn: Secure ASP.NET Core Blazor WebAssembly: https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/?view=aspnetcore-10.0
- Microsoft Learn: Configure OpenID Connect Web authentication in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication?view=aspnetcore-10.0
- Keycloak: Securing applications and services with OpenID Connect: https://www.keycloak.org/securing-apps/oidc-layers
- GitHub Docs: Authorizing OAuth apps: https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps
- Google Identity: OpenID Connect: https://developers.google.com/identity/openid-connect/openid-connect
- Sustainsys.Saml2: ASP.NET Core handler: https://saml2.sustainsys.com/en/v2/asp.net-core.html

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthenticationOptions.cs` | Provider, claim, scheme, redirect, and token relay options. |
| `src/Hexalith.FrontComposer.Shell/Services/Auth/ClaimsPrincipalUserContextAccessor.cs` | Server-side claims-backed `IUserContextAccessor`. |
| `src/Hexalith.FrontComposer.Shell/Services/Auth/AuthenticationStateUserContextAccessor.cs` | Blazor authentication-state adapter if needed for Auto/WASM paths. |
| `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAuthRedirector.cs` | Configured challenge/sign-in redirector with local return URL validation. |
| `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAccessTokenProvider.cs` | Host-auth token relay implementation for `EventStoreOptions.AccessTokenProvider`. |
| `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs` | `AddHexalithFrontComposerAuthentication` registration extension. |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | New HFC20xx auth diagnostic constants if shared constants are needed. |
| `src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md` | Runtime/Shell diagnostic release notes if project policy requires them. |
| `samples/Counter/Counter.Web/README.md` or existing docs | Demo-auth vs real-auth setup notes. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/*` | Options, claims, redirect, token, and redaction tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/*` | Token relay tests at command/query/SignalR seams. |

### Testing Standards

- P0 coverage: options validation, claims extraction, fail-closed missing tenant/user, token relay, redirect, no raw token persistence, no PII/token logging.
- Provider recipe coverage must use fake/test handlers. Live IdP tests are explicitly out of scope for normal CI.
- All auth exceptions and diagnostics must follow the teaching shape: what happened, expected, got, fix, docs link.
- Security negative tests are required: raw JWT-like values, emails, tenant IDs, user IDs, access tokens, and provider payloads must not appear in logs, telemetry, diagnostics, or cache.
- Existing EventStore auth tests must continue to pass with `RequireAccessToken=false` lanes used for non-auth tests.
- The Counter demo must still start without external credentials.

### Scope Guardrails

Do not implement these in Story 7-1:

- Multi-provider selection in one deployment.
- User/account management UI, profile screens, password reset UI, or provider login UI.
- `[RequiresPolicy]` attribute behavior, command policy checks, or button hiding. Story 7-3 owns this.
- Full tenant propagation to command envelopes, query parameters, SignalR groups, ETag keys, and MCP enumeration. Story 7-2 owns this.
- MCP tenant-scoped tools. Epic 8 owns this.
- Per-tenant IdP routing or dynamic tenant resolution at challenge time.
- Vertical-specific auth policies, audit logging, consent, or regulated-data classification.
- Storing raw access tokens, refresh tokens, ID tokens, or SAML assertions in `IStorageService`, localStorage wrappers, diagnostics, or logs.
- Live provider containers/services in default CI.

### Non-Goals With Owning Stories

| Non-goal | Owner |
| --- | --- |
| Tenant propagation and cross-tenant isolation enforcement across all operations. | Story 7-2 |
| Declarative command authorization via `[RequiresPolicy]`. | Story 7-3 |
| MCP tool enumeration scoped by tenant and authorization. | Epic 8 |
| Full documentation pages and provider cookbook. | Story 9-5 |
| Auth-specific migration/codefix tooling. | Story 9-2 / Story 9-4 |
| Browser matrix/E2E visual coverage for login flows. | Story 10-2 or dedicated auth E2E follow-up |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| GitHub OAuth to EventStore JWT token exchange recipe depends on adopter/broker architecture. | Story 9-5 docs or adopter integration guide |
| Full multi-IdP support with runtime provider selection. | v1.x follow-up |
| Policy-based command authorization and UI gating. | Story 7-3 |
| Tenant propagation into every command/query/subscription/cache/MCP surface. | Story 7-2 |
| Live provider smoke tests against real Keycloak/Entra/GitHub/Google tenants. | Optional nightly/manual integration lane |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-7-authentication-authorization-multi-tenancy.md#Story-7.1`] - story statement, provider list, JWT/token/PII constraints.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR37`] - OIDC/SAML provider integration without custom auth UI.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#Security-&-Data-Handling`] - zero PII, tenant isolation, JWT bearer propagation.
- [Source: `_bmad-output/planning-artifacts/prd/domain-specific-requirements.md`] - horizontal framework boundary and no custom auth UI commitment.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication/Authorization`] - ASP.NET Core auth as v1 cross-cutting concern.
- [Source: `_bmad-output/implementation-artifacts/2-2-action-density-rules-and-rendering-modes/`] - `IUserContextAccessor` fail-closed precedent.
- [Source: `_bmad-output/implementation-artifacts/3-1-shell-layout-theme-and-typography/`] - storage-key migration away from default tenant/user fallbacks.
- [Source: `_bmad-output/implementation-artifacts/5-2-http-response-handling-and-etag-caching.md`] - `IAuthRedirector`, 401 handling, ETag/cache response matrix.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md`] - Epic 7 email canonicalization and auth-related follow-up notes.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L03`] - tenant/user isolation fail-closed.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06`] - defense-in-depth budget.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review then elicitation sequencing.
- [Source: `src/Hexalith.FrontComposer.Contracts/Rendering/IUserContextAccessor.cs`] - existing flat user context contract.
- [Source: `src/Hexalith.FrontComposer.Contracts/Communication/IAuthRedirector.cs`] - 401 redirect seam.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptions.cs`] - bearer token provider seam.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreIdentity.cs`] - tenant validation and non-colon segment guard.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs`] - command bearer-token application.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`] - query bearer-token application, redirect, cache keying.
- [Source: Microsoft Learn: Secure an ASP.NET Core Blazor Web App with OpenID Connect](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-10.0) - Blazor OIDC/BFF, auth state, redirect/callback, token validation guidance.
- [Source: Microsoft Learn: Secure ASP.NET Core Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/?view=aspnetcore-10.0) - WASM OIDC/PKCE and token handling constraints.
- [Source: Microsoft Learn: Configure OpenID Connect Web authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication?view=aspnetcore-10.0) - ASP.NET Core OIDC handler setup.
- [Source: Keycloak OIDC docs](https://www.keycloak.org/securing-apps/oidc-layers) - Keycloak OIDC endpoints and discovery behavior.
- [Source: GitHub OAuth app docs](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps) - GitHub web application OAuth flow.
- [Source: Google OpenID Connect docs](https://developers.google.com/identity/openid-connect/openid-connect) - Google OIDC claims and discovery behavior.
- [Source: Sustainsys.Saml2 ASP.NET Core handler docs](https://saml2.sustainsys.com/en/v2/asp.net-core.html) - SAML handler integration pattern.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

(to be filled in by dev agent)

### File List

(to be filled in by dev agent)
