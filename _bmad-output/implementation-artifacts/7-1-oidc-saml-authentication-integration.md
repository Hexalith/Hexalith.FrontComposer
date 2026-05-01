# Story 7.1: OIDC/SAML Authentication Integration

Status: done

> **Epic 7** - Authentication, Authorization & Multi-Tenancy. Covers **FR37**, prepares **FR35 / FR46**, and enforces **NFR20 / NFR21 / NFR102**. Applies lessons **L01**, **L03**, **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 7-1 gives FrontComposer a standard authentication bridge without creating a framework-owned login UI:

- Add a FrontComposer authentication registration layer that wires ASP.NET Core authentication state into existing framework seams: `IUserContextAccessor`, `IAuthRedirector`, and `EventStoreOptions.AccessTokenProvider`.
- Support one configured identity provider per deployment for v1, with explicit provider recipes for Keycloak, Microsoft Entra ID, Google, and GitHub.
- Treat OIDC as the primary path for Keycloak, Entra, and Google. GitHub sign-in uses GitHub's OAuth web application flow unless the adopter fronts GitHub through an OIDC broker such as Keycloak or Entra External ID. Do not pretend GitHub OAuth tokens are JWT bearer tokens for EventStore.
- Support SAML through ASP.NET Core authentication handler integration, with Sustainsys.Saml2 as the default documented OSS handler unless implementation research selects a better maintained package before coding starts. Do not implement custom SAML protocol parsing in FrontComposer.
- Extract only framework-required identifiers: `TenantId` and `UserId`. No profile PII, email display names, token bodies, or provider claims are stored, logged, cached, or sent to diagnostics.
- Preserve the existing fail-closed user/tenant contract: unauthenticated or claim-missing contexts produce null/empty `IUserContextAccessor` values and the EventStore clients refuse to send commands/queries.
- Prefer server/BFF cookie-backed token storage for Blazor Auto and Blazor Server. Standalone WASM remains supported through the standard `IAccessTokenProvider`-style adapter but the framework must not write raw JWTs to `localStorage`.
- Keep token storage and renewal host-auth owned. FrontComposer requests a per-operation token through `EventStoreOptions.AccessTokenProvider`; it does not cache, refresh, or serialize bearer tokens.
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
| AC2 | FrontComposer authentication is configured for SAML | The host starts | The framework accepts a maintained SAML2 authentication handler configuration and bridges its resulting `ClaimsPrincipal` through the same FrontComposer user-context and redirect seams as OIDC, without custom SAML assertion parsing in FrontComposer. |
| AC3 | Keycloak, Microsoft Entra ID, Google, or GitHub is selected | The provider recipe is configured | Keycloak, Entra, and Google use OIDC discovery/authorization-code flow; GitHub uses OAuth web flow or an adopter-supplied OIDC broker. The story documents this protocol distinction, required claim mapping keys, scheme/callback/logout settings, and tests the selected recipe behavior using fake/local handlers only. |
| AC4 | No provider is selected or more than one provider is selected | Options validation runs | Startup fails with a teaching diagnostic naming the expected single-provider v1 constraint and the fix. |
| AC5 | An unauthenticated user navigates to an authenticated FrontComposer route or a 401 response reaches `IAuthRedirector` | The auth flow triggers | The user is redirected/challenged through the configured provider scheme, not through a framework-owned login UI, and only local return paths are preserved through ASP.NET Core-safe redirect/challenge mechanisms. |
| AC6 | Authentication succeeds | The provider returns to the application callback | ASP.NET Core validates issuer/signature/expiry/audience through the configured handler and exposes an authenticated `ClaimsPrincipal` to FrontComposer. |
| AC7 | A validated principal contains configured tenant and user claims | FrontComposer reads authentication state | `IUserContextAccessor.TenantId` and `UserId` return normalized non-empty identifiers for framework operations using documented claim precedence. |
| AC8 | Tenant or user claim is missing, empty, whitespace, multi-valued, or contains `:` | FrontComposer reads authentication state | The accessor fails closed by returning unauthenticated/null-equivalent values or by surfacing a clear sanitized configuration diagnostic; commands, queries, subscriptions, and cache writes do not proceed with synthetic defaults, demo identities, or fallback tenants. |
| AC9 | EventStore-backed command, query, or SignalR subscription code needs an access token | The operation sends outbound traffic | `EventStoreOptions.AccessTokenProvider` supplies the current bearer token through the configured token relay path; command/query clients set `Authorization: Bearer <token>`, SignalR uses its access token callback, and none of those paths log or persist the token. |
| AC10 | Token acquisition fails, returns empty, or the session expired | A command/query/subscription tries to send | The framework surfaces re-authentication through `IAuthRedirector` or an explicit fail-fast auth exception. It does not retry with an old token, connect SignalR with a null token, or send anonymous requests when `RequireAccessToken` is true. |
| AC11 | Blazor Server or Blazor Auto is used | Authentication state is stored | Tokens are kept in server-side authentication/session mechanisms or BFF/cookie-backed flow. Raw JWTs are never written to browser `localStorage` by FrontComposer. |
| AC12 | Standalone Blazor WebAssembly is used | Authentication state is managed | FrontComposer consumes the platform auth/token abstraction supplied by the host and still avoids direct raw-token `localStorage` writes in framework code. |
| AC13 | Framework diagnostics, logs, or telemetry observe auth events | The app authenticates, redirects, or fails validation | Logs include provider kind, diagnostic ID, claim presence booleans, and sanitized categories only. They exclude access tokens, ID tokens, refresh tokens, authorization codes, SAML assertions, `Authorization` header values, `NameID`, subject identifiers, email, display name, raw claim values, tenant/user IDs, provider payloads, and raw claims JSON. |
| AC14 | Counter sample runs without real auth | The developer starts the sample | The sample keeps its existing demo `IUserContextAccessor` path by default, and any fake OIDC/test-auth fixture is opt-in, visibly named test/sample-only, excluded from production defaults, and covered by deterministic authenticated/unauthenticated smoke tests without external IdP credentials. |
| AC15 | Story 7-1 completes | A developer prepares Story 7-2 | The resulting seams make tenant propagation implementable without changing provider login code: `TenantId`, `UserId`, token relay, redirect, and fail-closed behavior are all covered by tests. Story 7-1 does not add tenant propagation, role mapping, authorization policies, or access-control UI. |
| AC16 | Generated UI, business code, EventStore clients, and Contracts consume authentication state | The auth bridge is configured | They depend only on ASP.NET Core authentication primitives and FrontComposer bridge interfaces; provider-specific OIDC/SAML/GitHub types remain contained in Shell registration/options/recipe code. |

---

## Tasks / Subtasks

- [x] T1. Define FrontComposer authentication options and validation (AC1-AC4, AC13, AC16)
  - [x] Add `FrontComposerAuthenticationOptions` under `src/Hexalith.FrontComposer.Shell/Options/` or `Services/Auth/` with provider kind, scheme names, tenant/user claim names, allowed issuer/audience settings, return URL behavior, and token relay settings.
  - [x] Enforce exactly one provider per deployment for v1: OIDC, SAML, GitHub OAuth, or custom/brokered provider. More than one configured provider is a startup error.
  - [x] Add options validation that fails on missing provider, missing tenant/user claim names, missing authority/metadata address, insecure HTTP authority outside development, and unsupported provider/protocol combinations.
  - [x] Add HFC20xx Shell diagnostics for invalid auth configuration, claim extraction failure, token relay failure, and GitHub token-exchange requirement. Use existing `FcDiagnosticIds` patterns and do not reuse IDs.
  - [x] Add tests for every validation branch, including provider-specific fix text and docs-link shape.

- [x] T2. Add authentication registration extensions (AC1-AC5, AC11-AC12, AC16)
  - [x] Add `AddHexalithFrontComposerAuthentication(...)` in `src/Hexalith.FrontComposer.Shell/Extensions/` that composes with `AddHexalithFrontComposer()` and `AddHexalithEventStore()`.
  - [x] Register `IHttpContextAccessor` only where needed for server-side claim/token access; do not add HTTP pipeline dependencies to Contracts.
  - [x] For OIDC, integrate with ASP.NET Core `AddAuthentication().AddCookie().AddOpenIdConnect(...)` or an equivalent authenticated BFF/cookie pattern. Keep provider UI external.
  - [x] For SAML, expose a handler-configuration hook instead of hard-coding every SAML option in FrontComposer. If Sustainsys.Saml2 remains the selected package, add it as an optional documented package and pin it centrally when implementation starts.
  - [x] For GitHub, support OAuth sign-in as an identity provider recipe but require an adopter token-exchange/broker path before treating any token as EventStore bearer JWT.
  - [x] Add DI tests proving the extension replaces the default `NullUserContextAccessor` and `NoOpAuthRedirector` only when auth is configured.
  - [x] Add dependency-boundary tests or source checks proving generated UI, EventStore clients, and Contracts do not reference provider-specific OIDC/SAML/GitHub implementation types.

- [x] T3. Implement claims-based `IUserContextAccessor` adapters (AC6-AC8, AC13, AC15)
  - [x] Add a server-side `ClaimsPrincipalUserContextAccessor` that reads the authenticated principal from `IHttpContextAccessor` or `AuthenticationStateProvider` according to host mode.
  - [x] Add a WASM-friendly adapter seam that can read claims from the host authentication state without FrontComposer owning token storage.
  - [x] Normalize tenant/user claim values by trimming and rejecting null, empty, whitespace, multi-valued, and colon-containing values.
  - [x] Do not lowercase tenant IDs. Preserve existing storage-key precedent: tenants may be case-sensitive; user ID canonicalization beyond trimming is an explicit policy decision and must be documented if added.
  - [x] Return unauthenticated/null-equivalent values on missing context so existing fail-closed consumers continue to short-circuit.
  - [x] Add table-driven fake-principal tests for OIDC, SAML, and GitHub OAuth claim shapes, including `sub`, `nameidentifier`, `NameID`, `email`, configured tenant/user claim aliases, missing claims, multi-valued claims, whitespace, colon rejection, unauthenticated principal, and no leakage of raw claim values in logs/diagnostics.

- [x] T4. Bridge redirect and token relay seams (AC5, AC9-AC12)
  - [x] Implement an `IAuthRedirector` that triggers the configured challenge/sign-in route and preserves a sanitized return URL.
  - [x] Reject absolute or cross-origin return URLs; only local return paths are allowed.
  - [x] Implement a token provider bridge for `EventStoreOptions.AccessTokenProvider` that can retrieve current access tokens from the configured ASP.NET Core authentication session or host-supplied token accessor.
  - [x] Ensure command/query clients keep their existing `RequireAccessToken` fail-fast behavior when token acquisition fails.
  - [x] Add tests proving tokens are applied to command/query HTTP requests and SignalR access-token callback paths without logging token values.
  - [x] Add deterministic SignalR token-failure tests: null, empty, or thrown token acquisition prevents connection or produces the explicit authenticated failure path; no stale token reuse is allowed.
  - [x] Cover cancellation: if token acquisition observes cancellation, no redirect, cache mutation, or partial send occurs.

- [x] T5. Provider recipes and fixtures (AC3, AC6, AC9, AC14)
  - [x] Add provider recipe tests/fixtures for Keycloak OIDC discovery, Entra issuer/audience validation, Google OIDC standard claims, GitHub OAuth challenge/token-exchange requirement, and SAML handler bridging.
  - [x] Use fake handlers or local test doubles. Do not require live Keycloak, Entra, GitHub, Google, or SAML IdP credentials in CI.
  - [x] Name fake fixtures explicitly (`FakeOidc`, `FakeSaml`, `FakeGitHubOAuth`) and include expected positive/negative claim sets so CI cannot drift toward live-provider assumptions.
  - [x] Document required provider metadata: authority/metadata address, client ID, client secret storage expectations, redirect callback path, sign-out callback path, scopes, tenant claim, user claim, audience, and issuer.
  - [x] Ensure provider recipes include both development and production notes. Development may use user secrets or fake handlers; production must use secure secret storage owned by the host.
  - [x] Add sample `appsettings` snippets with placeholder values only. Never commit real secrets or tenant-specific examples.

- [x] T6. Secure token and storage behavior (AC9-AC13)
  - [x] Audit framework code paths for raw token persistence; add a regression test that fails if FrontComposer writes access/refresh/ID token names or JWT-looking values to `IStorageService` or local-storage wrappers.
  - [x] Add a structured logging redaction test with JWT-like strings, email addresses, tenant IDs, and user IDs in exception messages and claims.
  - [x] Keep ETag/session persistence keyed by `IUserContextAccessor`; do not introduce alternate auth-specific cache keys.
  - [x] Ensure token refresh or re-auth errors cannot leave stale bearer tokens in `EventStoreOptions` or long-lived delegates.

- [x] T7. Counter sample and developer experience (AC5, AC14)
  - [x] Preserve `DemoUserContextAccessor` as the default Counter local-development path.
  - [x] Add an opt-in fake auth mode for Counter or a test-only sample host that demonstrates the registration extension and authenticated route behavior without external credentials.
  - [x] Add a short README section showing where adopters replace the demo stub with the auth bridge.
  - [x] Show the exact registration order with `AddHexalithFrontComposerQuickstart`, `AddHexalithFrontComposerAuthentication`, `AddHexalithEventStore`, and `services.Replace` patterns where needed.

- [x] T8. Tests and verification (AC1-AC16)
  - [x] Shell options validation tests for every invalid provider setup.
  - [x] Configuration/build-time tests proving zero providers and multiple providers fail before runtime traffic is accepted.
  - [x] DI registration tests proving default null/no-op seams are replaced only when configured.
  - [x] Claims adapter tests for OIDC, SAML, GitHub OAuth, unauthenticated, missing claim, colon claim, whitespace claim, and claim-alias behavior.
  - [x] Redirector tests for challenge route, local return URL, absolute URL rejection, and cancellation.
  - [x] Token relay tests for command, query, and SignalR subscription clients.
  - [x] Fail-closed tests proving unauthenticated or invalid-claim contexts do not dispatch command/query work, do not start SignalR with a null token, and do not write cache entries.
  - [x] Redaction tests proving no token/profile/claim payload leakage to logs, diagnostics, telemetry, cache, serialized client-visible state, or `IStorageService`.
  - [x] Counter/sample tests for demo mode preserved and fake-auth mode working.
  - [x] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [x] Targeted tests: `tests/Hexalith.FrontComposer.Shell.Tests` auth, EventStore, and sample-host lanes.

### Review Findings

> Code review run 2026-05-01 via `bmad-code-review` against commit 7296206 (32 files, 1,690 insertions, 77 deletions). Three adversarial layers raised 150 raw findings (Blind 60, Edge 64, Auditor 26). After dedup and bucketing: 5 decision-needed (all resolved), 30 patches (24 applied, 6 deferred or rolled into existing patches), 8 defers (entered in deferred-work.md), ~22 dismissed. Validation: `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` (0 warnings, 0 errors); `dotnet test Hexalith.FrontComposer.sln --no-build -p:UseSharedCompilation=false` => Contracts 148/0/0, Shell 1471/0/0 (+62 new tests), SourceTools 587/0/0, Bench 2/0/0; total 2208/0/0.

#### Decision-needed (resolved)

- [x] [Review][Decision] **DN1 — `AuthenticationStateUserContextAccessor` ships unregistered + sync-over-async deadlock risk** — **Resolution: option (a)** remove the file outright and defer the WASM accessor to Story 7-2 with explicit AC12 partial-credit acknowledgement. WASM hosts continue to use the host-supplied auth abstraction in v1 (which AC12 explicitly permits: "FrontComposer consumes the platform auth/token abstraction supplied by the host"). Story 7-2 owns the typed seam. Action: `git rm src/Hexalith.FrontComposer.Shell/Services/Auth/AuthenticationStateUserContextAccessor.cs`. Deferred entry: D9.
- [x] [Review][Decision] **DN2 — `EventStoreOptions.AccessTokenProvider ??=` silently no-ops when adopter pre-set a provider** — **Resolution: option (a)** always replace + log Information when an adopter-supplied delegate was previously present. Silent precedence inversion defeats the GitHub broker check (HFC2014) and the diagnostic path. Materialized as patch P32.
- [x] [Review][Decision] **DN3 — Multi-valued claim handling rejects entire extraction** — **Resolution: option (a)** keep strict rejection. Spec AC8 explicitly enumerates multi-valued as a fail-closed condition. Adopters with `groups`-style mappers should configure single-valued protocol mappers (Keycloak supports this via custom protocol mapper composition). No code change.
- [x] [Review][Decision] **DN4 — Cookie scheme `LoginPath`/`LogoutPath` route to nothing** — **Resolution: option (a)** ship a `MapHexalithFrontComposerAuthenticationEndpoints()` extension that turns the cookie redirect into a `ChallengeAsync` against the configured provider. Paths are also exposed on `FrontComposerAuthRedirectOptions` for adopters who want to override. Materialized as patch P33.
- [x] [Review][Decision] **DN5 — Force `EventStoreOptions.RequireAccessToken=true`?** — **Resolution: option (a)** force `RequireAccessToken=true` post-configure whenever the auth bridge is registered. Matches the persistent-feedback rule "auth-registered services must fail-closed on missing token". Adopters opt out by setting `RequireAccessToken=false` AFTER calling `AddHexalithFrontComposerAuthentication`. Materialized as patch P34.

#### Patches (apply after decisions resolved)

- [x] [Review][Patch] **P1 — `configure` callback invoked twice** [`Extensions/FrontComposerAuthenticationServiceExtensions.cs:24-32`] — Build options once via `OptionsBuilder` and re-read `IOptions<>` for handler wiring. Sources: B01+B05+E03+A11.
- [x] [Review][Patch] **P2 — `services.Replace` happens even when no provider configured** [`Extensions/FrontComposerAuthenticationServiceExtensions.cs:33-35`] — Guard with `if (setup.SelectedProviderKind != FrontComposerAuthenticationProviderKind.None)`. Sources: B04+E59.
- [x] [Review][Patch] **P3 — `FrontComposerReturnUrl.Sanitize` open-redirect hardening** [`Services/Auth/FrontComposerReturnUrl.cs:9-33`] — Cap input length (2048 chars), reject Unicode format chars (U+202E RTL, U+200E LRM, U+200F RLM, U+FEFF BOM, U+2028 LS, U+2029 PS), reject ` ` NBSP after `Trim`, reject `/@host/` userinfo-prefixed forms, increase double-decode loop bound to 8 with fixpoint termination. Sources: B10+B11+B12+E05+E07+E08+E09+E10+E37.
- [x] [Review][Patch] **P4 — `Sanitize` test coverage gaps** [`tests/.../FrontComposerAuthRedirectorTests.cs`] — Add cases for `data:`, `javascript:`, `JaVaScRiPt:`, `file:///`, ` `, `﻿` BOM, surrogate halves, IPv6 `[::1]`, IDN homograph, `/@evil`, length-cap, multi-fragment. Sources: E39+E55.
- [x] [Review][Patch] **P5 — `EventStoreAccessTokenGuard` exception type + HFC2013 logging** [`Infrastructure/EventStore/EventStoreAccessTokenGuard.cs:6-13`] — Throw `FrontComposerAuthenticationException(HFC2013, ...)` instead of bare `InvalidOperationException`; normalize whitespace token to null regardless of `requireAccessToken`; emit logger warning with HFC2013 prefix. Sources: B08+B54+E19+A19.
- [x] [Review][Patch] **P6 — `FrontComposerAccessTokenProvider` exception handling** [`Services/Auth/FrontComposerAccessTokenProvider.cs:35-58`] — Add `(string, string, Exception)` ctor on `FrontComposerAuthenticationException`; pass inner ex; replace `catch (Exception ex) when (ex is not OutOfMemoryException)` with explicit list (`HttpRequestException`, `TaskCanceledException`, `InvalidOperationException`); log HFC2013 on the empty-token branch. Sources: B17+B18+B34+E16+E31+E32+E47.
- [x] [Review][Patch] **P7 — Cache `Read()` result per scope** [`Services/Auth/ClaimsPrincipalUserContextAccessor.cs:15-17` + `AuthenticationStateUserContextAccessor.cs:15-17`] — `TenantId` and `UserId` getters each call `Read()` independently → 2× claim enumeration + 2× HFC2012 warn per render. Cache result in scoped field (lazy). Sources: B03+E48.
- [x] [Review][Patch] **P8 — Validator hardening (one consolidated patch)** [`Services/Auth/FrontComposerAuthenticationOptionsValidator.cs`]:
  - Reject duplicate aliases in `TenantClaimTypes`/`UserClaimTypes` (B21+E24)
  - Validate `Saml2.MetadataAddress` is HTTPS + absolute when `Saml2.Enabled` and `ConfigureHandler` is null; require `ConfigureHandler` non-null on enabled SAML (E23+E25)
  - Reject empty/whitespace `OpenIdConnect.Audience` (E61) and `TokenRelay.AccessTokenName` (E22)
  - Reject userinfo (`user:pass@`) in `Authority` (E33)
  - Reject conflicting `Authority` + `MetadataAddress` (different hosts) for OIDC (E56)
  - Validate `Scopes` contains `openid` when OIDC enabled; reject all-whitespace scopes (B26+B30+E62)
  - Validate `CallbackPath`/`SignedOutCallbackPath` start with `/` (B59)
  - Reject `signInScheme == challengeScheme` collisions (B56+E28)
  - Add `ArgumentException.ThrowIfNullOrWhiteSpace` to all recipe helpers (`UseGoogle`, `UseGitHubOAuth`, `UseKeycloak`, `UseMicrosoftEntraId`) for `clientId`/`clientSecret`/claim types (B24+E44).
- [x] [Review][Patch] **P9 — OIDC `SaveTokens=true` default** [`Extensions/FrontComposerAuthenticationServiceExtensions.cs:58-77`] — Today `oidc.SaveTokens` is left at framework default (false); `FrontComposerAccessTokenProvider.ReadHttpContextTokenAsync` then always returns null. Set `oidc.SaveTokens = true` unless adopter overrode. Sources: A13+E26.
- [x] [Review][Patch] **P10 — Cookie scheme secure defaults** [`Extensions/FrontComposerAuthenticationServiceExtensions.cs:53-56`] — Set `cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always` outside Development; `cookie.Cookie.SameSite = SameSiteMode.Lax`; `cookie.Cookie.HttpOnly = true`; `cookie.SlidingExpiration = false` (or expose options). Sources: B31+A14.
- [x] [Review][Patch] **P11 — Counter fake-auth production guard + smoke tests** [`samples/Counter/Counter.Web/Program.cs:60-64` + new test file] — Refuse to register `CounterFakeAuthUserContextAccessor` when `IHostEnvironment.IsDevelopment()` is false (throw `InvalidOperationException` with explicit message) AND emit `LogCritical` startup banner when fake auth is active. Add deterministic smoke test verifying demo-mode-preserved + fake-auth-on toggle behavior. Sources: B40+B41+E49+E50+A07+A08.
- [x] [Review][Patch] **P12 — README null-forgiving on secret access** [`samples/Counter/Counter.Web/README.md:498-503`] — Replace `builder.Configuration["Auth:ClientSecret"]!` with `?? throw new InvalidOperationException("Auth:ClientSecret is required")`. Sources: B49.
- [x] [Review][Patch] **P13 — `CapturingLogger` captures log level** [`tests/.../Services/Auth/CapturingLogger.cs:1-20`] — Today only the message is captured; tests cannot verify HFC2012 was logged at Warning. Capture `(LogLevel, string)` tuples. Sources: B50.
- [x] [Review][Patch] **P14 — Provider recipe tests for Entra/Google/GitHub/SAML** [`tests/.../FrontComposerAuthenticationOptionsTests.cs`] — Today only Keycloak recipe-shape test exists. Add `Recipe_MicrosoftEntraId_*`, `Recipe_Google_*`, `Recipe_GitHubOAuth_*`, `Recipe_Saml2_*` covering tenant/user claim defaults, single-provider invariant, and broker requirement (GitHub). Sources: A03+A04+B43.
- [x] [Review][Patch] **P15 — OIDC `ValidIssuer` wiring** [`Options/FrontComposerAuthenticationOptions.cs` + `Extensions/...`] — Add `string? ValidIssuer { get; set; }` on `FrontComposerOpenIdConnectOptions`; wire to `oidc.TokenValidationParameters.ValidIssuer`; require non-empty for Entra recipe. Sources: A05.
- [x] [Review][Patch] **P16 — Logging redaction stress test** [`tests/.../Services/Auth/`] — Add a test that injects JWT-shaped (`eyJ...`), email-shaped (`alice@evil.test`), and tenant-shaped values into claims and exception messages; assert no occurrence in `CapturingLogger.Messages` or `Exception.Message`. Sources: A06+B47.
- [x] [Review][Patch] **P17 — Validator teaching diagnostic shape** [`Services/Auth/FrontComposerAuthenticationOptionsValidator.cs:101-105`] — Add `Got=<sanitized current state>` segment alongside `Expected=...`; emit per-cause docs anchor (e.g., `#oidc-https`, `#github-broker`). Sources: A18+A21.
- [x] [Review][Patch] **P18 — `AuthBoundaryTests` Linux-CI safety + tighter exemptions** [`tests/.../Architecture/AuthBoundaryTests.cs`] — Use `OrdinalIgnoreCase` for path matching (Windows-developed, Linux-CI run); strip line-comments before scanning for OIDC type names; convert per-file allow-list to per-literal (`access_token` only, not whole-file pass) for the token-string boundary check. Sources: B35+B36+A25+E51.
- [x] [Review][Patch] **P19 — Recipe helpers must reset sibling providers** [`Options/FrontComposerAuthenticationOptions.cs:35-93`] — Calling `UseKeycloak` then `UseGitHubOAuth` produces double-enabled config that validator then rejects. Each recipe helper should set `OpenIdConnect.Enabled=false; Saml2.Enabled=false; GitHubOAuth.Enabled=false; CustomBrokered.Enabled=false` before enabling its own. Sources: A22.
- [x] [Review][Patch] **P20 — Negative DI test (extension not called → defaults preserved)** [`tests/.../Extensions/FrontComposerAuthenticationServiceExtensionsTests.cs`] — Add a test that builds DI WITHOUT calling `AddHexalithFrontComposerAuthentication` and asserts `IUserContextAccessor` resolves to `NullUserContextAccessor`, `IAuthRedirector` resolves to `NoOpAuthRedirector`. Sources: A10.
- [x] [Review][Patch] **P21 — `FrontComposerAuthRedirector.RedirectAsync` test coverage** [`tests/.../FrontComposerAuthRedirectorTests.cs`] — Today only `Sanitize` is tested. Add tests for null-HttpContext branch, `ChallengeAsync` invocation with the configured challenge scheme, returnUrl propagation, and pre-call cancellation. Sources: A09+E17+E18+E55.
- [x] [Review][Patch] **P22 — Validator runs eagerly before handler registration** [`Extensions/FrontComposerAuthenticationServiceExtensions.cs:24-32`] — Today `AddAuthentication`/`AddCookie`/`AddOpenIdConnect` are called against an unvalidated `setup` instance; `ValidateOnStart` fires later. Run `FrontComposerAuthenticationOptionsValidator.Validate(setup)` synchronously inside the extension and throw before invoking `AddAuthenticationHandlers`. Sources: E34.
- [x] [Review][Patch] **P23 — Test brittleness: replace `result.Failures.Single()`/`ShouldContain(literal)` with predicate-based assertions** [`tests/.../FrontComposerAuthenticationOptionsTests.cs`] — Use `result.Failures.ShouldContain(f => f.StartsWith("HFC2011"))` instead of `result.Failures.Single().ShouldContain("...")`. Sources: B23+B42+E52+E53.
- [x] [Review][Patch] **P24 — `AnalyzerReleases.Unshipped.md` column alignment** [`src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md:545-552`] — Normalize column widths (HFC201x rows are misaligned vs older rows). Sources: B32.
- [x] [Review][Patch] **P25 — GitHub principal-OK + bearer-relay-fails fixture** [`tests/.../FrontComposerAccessTokenProviderTests.cs`] — Today only the broker-required exception is tested. Add a test that builds a GitHub-shaped principal, asserts `IUserContextAccessor.TenantId/UserId` succeed, and asserts `GetAccessTokenAsync` throws HFC2014 when no broker/`HostAccessTokenProvider` is configured. Sources: A17.
- [x] [Review][Patch] **P26 — `HttpContext is null` test for `FrontComposerAccessTokenProvider`** [`tests/.../FrontComposerAccessTokenProviderTests.cs`] — Add a test where `IHttpContextAccessor.HttpContext == null` (background scope / SignalR reconnect after request end) and assert HFC2013 is thrown with sanitized message + structured logger entry. Sources: E54+E42.
- [x] [Review][Patch] **P27 — SAML fake-handler bridging fixture** [`tests/.../Extensions/`] — Build a stub `IAuthenticationHandler` that emits a synthetic SAML-shaped principal and assert it flows through `ClaimsPrincipalUserContextAccessor` to `TenantId`/`UserId`. Sources: A16.
- [x] [Review][Patch] **P28 — Diagnostic message HFC double-prefix** [`Services/Auth/FrontComposerAccessTokenProvider.cs:25-27, 36-38, 54-56` + `FrontComposerAuthRedirector.cs:18`] — Messages built as `$"{HFC...}: ..."` produce `HFC20XX: HFC20XX: ...` once structured logger adds `{DiagnosticId}` template. Drop the literal prefix from the message; let logger template emit it. Sources: A21.
- [x] [Review][Patch] **P29 — Fail-closed cache-write smoke test** [`tests/.../`] — Add a test proving an unauthenticated principal results in zero EventStore cache writes and zero `IStorageService` writes. Sources: A23.
- [x] [Review][Patch] **P30 — Validator branch-coverage gap tests** [`tests/.../FrontComposerAuthenticationOptionsTests.cs`] — Cover missing OIDC `Authority`+`MetadataAddress`, non-`code` `ResponseType`, SAML `MetadataAddress`/`ConfigureHandler` shape, GitHub missing `ClientSecret`, `MetadataAddress` HTTPS validation. Sources: A24.
- [x] [Review][Patch] **P31 — Remove `AuthenticationStateUserContextAccessor.cs`** [`src/Hexalith.FrontComposer.Shell/Services/Auth/AuthenticationStateUserContextAccessor.cs`] — Resolves DN1.
- [x] [Review][Patch] **P32 — Always replace `EventStoreOptions.AccessTokenProvider`** [`Extensions/FrontComposerAuthenticationServiceExtensions.cs`] — Resolves DN2; logs Information when overriding a pre-set adopter delegate.
- [x] [Review][Patch] **P33 — `MapHexalithFrontComposerAuthenticationEndpoints` extension** [`Extensions/FrontComposerAuthenticationServiceExtensions.cs`] — Resolves DN4; cookie-redirect login/sign-out paths map to `ChallengeAsync`/`SignOutAsync` against the configured provider, avoiding the 404 trap.
- [x] [Review][Patch] **P34 — Force `EventStoreOptions.RequireAccessToken=true` post-configure** [`Extensions/FrontComposerAuthenticationServiceExtensions.cs`] — Resolves DN5; auth-registered services fail-closed on missing token.

#### Deferred (pre-existing or out of scope)

- [x] [Review][Defer] **D1 — `ProjectionSubscriptionService` `IOptionsMonitor` support for `EventStoreOptions`** [`src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs:60-67`] — `current = options.Value` snapshot at construction freezes `AccessTokenProvider`/`RequireAccessToken`. Hot-reload would be useful but spec doesn't mandate it. **Owner:** Story 7-2 (tenant context propagation revisits scoped options) or Epic 9 backlog. Sources: B07+A20+E21+E64+B37.
- [x] [Review][Defer] **D2 — Double token fetch on Subscribe** [`Infrastructure/EventStore/ProjectionSubscriptionService.cs:369-379`] — Pre-flight + StartAsync each call the token provider. Performance optimization, not correctness. **Owner:** Epic 9 backlog. Sources: B09+E20.
- [x] [Review][Defer] **D3 — SignalR auto-reconnect token re-pre-flight** [`Infrastructure/EventStore/ProjectionSubscriptionService.cs:88-90, 304-352`] — Reconnect path doesn't pre-flight a fresh token; SignalR's accessTokenFactory handles re-fetch. Defensible but worth flagging. **Owner:** Epic 5/9 SignalR hardening backlog. Sources: E38+E63.
- [x] [Review][Defer] **D4 — `https://hexalith.dev/frontcomposer/authentication` docs URL is aspirational** [`Services/Auth/FrontComposerAuthenticationOptionsValidator.cs:101-105`] — Validator embeds a URL pointing to a page that does not exist. **Owner:** Story 9-5 (Diataxis docs site) — natural place to publish the auth bridge docs page. Sources: A26.
- [x] [Review][Defer] **D5 — Lessons L01/L03/L06/L07/L08/L10 traceability annotations** [`tests/.../`] — Spec references lessons but tests are not annotated with `[Trait("Lesson", "Lxx")]`. Process polish only. **Owner:** Future test-governance pass. Sources: B53.
- [x] [Review][Defer] **D6 — `FrontComposerAccessTokenProvider` Singleton lifetime closes off per-tenant token brokers** [`Extensions/FrontComposerAuthenticationServiceExtensions.cs:36`] — Singleton is fine for v1 single-provider. **Owner:** Story 7-2 (multi-tenant scoping may want Scoped). Sources: B19.
- [x] [Review][Defer] **D7 — `FrontComposerAuthRedirector` CSRF state for brokered providers** [`Services/Auth/FrontComposerAuthRedirector.cs:14-24`] — `ChallengeAsync` uses ASP.NET defaults for state/nonce; SAML/custom-brokered providers may need explicit state. **Owner:** Future per-provider hardening when SAML adoption surfaces. Sources: B58.
- [x] [Review][Defer] **D8 — Internal/public symmetry of `SelectedProviderKind`** [`Options/FrontComposerAuthenticationOptions.cs:858-885`] — Internal accessor used through public DI registration. Cosmetic. **Owner:** Future API surface review. Sources: B45.

#### Dismissed (~22 findings)

Noise / intentional-by-design / handled elsewhere: B15 (alias names in logs are admin-config), B20 (optional `IHostEnvironment` is intentional), B33 (XML doc lint not enforced), B46 (`IList<string>` mutation caught at validation), B55 (using-order speculative), B60 (diagnostic constant naming style cosmetic), E06 (sanitize edge already correct), E14 (case-sensitive alias values intentional per spec), E45 (GitHub recipe broker-config-required is by design), E46 (no min-length is intentional), E57 (claim-mapper compatibility is fail-closed by design), E58 (OCE propagation already correct), E60 (fake-auth identity sharing is sample-only — out of scope per `feedback_memory_rule_scope_check`), B39 (alias `|` separator log-parse — admin-config so low impact), and 7 other low-impact cosmetic/redundant findings collapsed into the patch clusters above.

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

### Architecture Contracts

- Shell owns provider registration, challenge/redirect wiring, claims-backed user context adapters, token-provider bridge registration, sanitized diagnostics, and sample/test fake-auth wiring.
- EventStore clients consume only `EventStoreOptions.AccessTokenProvider`, `IUserContextAccessor`, and `IAuthRedirector`; they do not own token storage, refresh, provider SDKs, or authentication session state.
- Contracts remains free of ASP.NET Core, OAuth/OIDC, SAML, GitHub, and provider-specific package references unless a future story deliberately changes the public abstraction boundary.
- Generated UI and business-facing components consume authenticated state only through FrontComposer bridge abstractions; provider-specific types stay inside Shell registration/options/recipe code.
- Fail-closed means unauthenticated, missing-claim, malformed-claim, multi-valued-claim, or token-unavailable cases do not dispatch backend command/query work, do not start SignalR with a null/stale token, do not write cache entries, and never synthesize `"default"`, `"anonymous"`, demo, or fallback identities.

### Auth Flow Sequence

1. An unauthenticated request reaches an authenticated FrontComposer route or a 401 reaches `IAuthRedirector`.
2. `IAuthRedirector` issues a challenge against the configured ASP.NET Core authentication scheme and preserves only a local return path.
3. The external provider handles login and returns through the configured callback.
4. ASP.NET Core authentication middleware validates the protocol payload and exposes an authenticated `ClaimsPrincipal`.
5. FrontComposer extracts normalized `TenantId` and `UserId` through the configured claim map and returns null-equivalent values on invalid input.
6. EventStore command/query/SignalR paths request a per-operation token through `EventStoreOptions.AccessTokenProvider`; host-auth code owns storage and renewal.

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
| D11 | Provider-specific dependencies remain contained in Shell registration/options/recipe code. | Keeps Contracts, generated UI, and EventStore clients stable and testable across OIDC, SAML, GitHub OAuth, and brokered providers. | Let generated UI or EventStore reference provider-specific SDK types directly. |
| D12 | Token relay is opt-in per configured downstream client and host-auth owns token storage/renewal. | Prevents FrontComposer from becoming an identity platform or stale-token cache while still supporting EventStore bearer authentication. | Cache tokens in framework services or globally forward any available provider token. |

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

### Advanced Elicitation Hardening Addendum

The advanced elicitation pass keeps the story ready-for-dev, but tightens security and determinism around the auth bridge so implementation does not silently become an identity platform.

Apply these refinements during `bmad-dev-story`:

- Treat claim extraction as an executable provider matrix, not prose. For each fake OIDC, fake SAML, fake GitHub OAuth, and custom/brokered fixture, record inbound claim type, configured alias, precedence result, normalization result, invalid-value result, diagnostic ID, and whether command/query/SignalR/cache work is allowed. Include duplicate alias, multi-valued claim, whitespace, colon, missing tenant, missing user, unauthenticated principal, case-sensitive tenant, and unknown extra claim rows.
- Claim precedence must be deterministic and conflict-aware. If two configured aliases both produce non-empty values for the same framework identifier and the values differ after trimming, fail closed with a sanitized configuration/claim diagnostic instead of choosing one by enumeration order. Tests must prove raw values are not logged while still naming the conflicting claim aliases.
- Redirect safety needs a table-driven oracle. Cover local absolute paths, app-relative paths, path-base prefixed paths, query-only return paths if supported, absolute URLs, scheme-relative URLs, encoded backslashes, encoded CR/LF, double-encoded external URLs, fragments, empty return paths, and malformed URI input. Rejected cases challenge to the configured provider without preserving the unsafe return value.
- Token relay is a per-operation lifecycle contract. A token acquisition attempt starts only when a command, query, or SignalR connection attempt needs it; cancellation, session expiry, empty token, thrown provider exception, or sign-out during acquisition must end in a single explicit auth failure path with no header set, no cache mutation, no retry with a previously observed token, and no background refresh owned by FrontComposer.
- Avoid long-lived token capture. `AccessTokenProvider` delegates and SignalR callbacks may close over host-auth services or accessors, but must not close over token strings, claims payloads, `ClaimsPrincipal` snapshots, authorization-code values, SAML assertions, or mutable request-scoped objects that can outlive the request/circuit.
- SAML and GitHub recipes require protocol honesty tests. SAML fixtures prove FrontComposer consumes only handler-produced claims and never parses assertions. GitHub fixtures prove OAuth sign-in can authenticate the user context but cannot satisfy EventStore bearer-token relay unless a broker/custom access-token provider is explicitly configured.
- Logout and auth-state change behavior must fail closed. After sign-out, expired session detection, or host-auth state change to unauthenticated, new command/query/SignalR attempts must not reuse old tenant/user/token data, and cache writes must remain disabled until a fresh valid principal and token are available.
- Redaction coverage must include high-temptation debug data: `Authorization` headers, ID/access/refresh token names, JWT-like strings, authorization codes, SAML XML fragments, `NameID`, `sub`, email/display names, tenant/user IDs, raw claim JSON, exception messages containing claim values, callback URLs with query strings, and provider metadata payloads. Allowed telemetry remains provider kind, scheme name, diagnostic ID, phase, and claim-presence booleans.
- Provider-specific package boundaries are release-blocking. Add a source/dependency guard that allows OIDC/SAML/GitHub implementation references only in Shell auth registration/options/recipe/test fixture namespaces. Contracts, SourceTools, generated UI, EventStore clients, and business-facing components must depend only on ASP.NET Core primitives and FrontComposer bridge interfaces.
- Keep the implementation budget stable. If coding uncovers requirements for multi-IdP routing, tenant membership validation, account provisioning, role mapping, token exchange service design, provider certification, or custom renewal UX, record a deferred decision with an owning story instead of expanding 7-1.

### Scope Guardrails

Do not implement these in Story 7-1:

- Multi-provider selection in one deployment.
- User/account management UI, profile screens, password reset UI, or provider login UI.
- `[RequiresPolicy]` attribute behavior, command policy checks, or button hiding. Story 7-3 owns this.
- Full tenant propagation to command envelopes, query parameters, SignalR groups, ETag keys, and MCP enumeration. Story 7-2 owns this.
- MCP tenant-scoped tools. Epic 8 owns this.
- Per-tenant IdP routing or dynamic tenant resolution at challenge time.
- Account linking, user provisioning, tenant membership validation, role mapping, profile normalization, or provider discovery.
- Token refresh UI, custom token renewal policy, or client-side token persistence strategy beyond consuming host/platform auth abstractions.
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

## Party-Mode Review

- Date/time: 2026-04-30T08:10:59.3981177+02:00
- Selected story key: `7-1-oidc-saml-authentication-integration`
- Command/skill invocation used: `/bmad-party-mode 7-1-oidc-saml-authentication-integration; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Test Architect), John (Product Manager)
- Findings summary: The review found the story directionally sound but too broad and under-specified for a security-sensitive auth bridge. The main risks were provider-specific coupling leaking outside Shell, unclear token relay ownership, ambiguous claims failure behavior, non-deterministic token/redaction/fake-provider test oracles, and scope bleed into Story 7-2 tenant propagation, Story 7-3 authorization, production IdP certification, or account-management UX.
- Changes applied: Hardened AC2-AC16 around handler-only SAML, fake/local provider fixtures, safe challenge return paths, documented claim precedence, multi-valued claim rejection, no fallback/demo identities, command/query/SignalR token relay, stale/null token failure behavior, expanded redaction prohibitions, sample-only fake auth, Story 7-2/7-3 boundaries, and provider-specific dependency containment. Added Shell/EventStore/Contracts architecture contracts, an auth flow sequence, binding decisions for provider dependency containment and host-owned token storage/renewal, deterministic fake fixture requirements, fail-closed tests, SignalR token-failure tests, and stricter scope guardrails.
- Findings deferred: Multi-provider selection, account linking, provider discovery, user provisioning, tenant membership validation, role mapping, authorization policies, access-control UI, full tenant propagation, custom token renewal UI/policy, GitHub-to-EventStore token strategy beyond broker guidance, live IdP certification, full Diataxis provider cookbook, browser matrix/E2E login flow coverage, MCP tenant-scoped tooling, and production IdP onboarding templates remain with their owning future stories or manual integration lanes.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- Date/time: 2026-04-30T14:02:43.5175163+02:00
- Selected story key: `7-1-oidc-saml-authentication-integration`
- Command/skill invocation used: `/bmad-advanced-elicitation 7-1-oidc-saml-authentication-integration`
- Batch 1 method names: Security Audit Personas; Red Team vs Blue Team; Failure Mode Analysis; Pre-mortem Analysis; Self-Consistency Validation.
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; Graph of Thoughts; Comparative Analysis Matrix; Occam's Razor Application; Challenge from Critical Perspective.
- Findings summary: The elicitation found that party-mode review had already fixed the main scope boundaries, but implementation still needed sharper executable oracles for claim precedence conflicts, redirect normalization, per-operation token lifecycle, long-lived token capture, protocol-honest SAML/GitHub recipes, logout/auth-state invalidation, redaction stress cases, and provider package boundaries.
- Changes applied: Added an Advanced Elicitation Hardening Addendum requiring provider claim matrices, deterministic conflict handling, table-driven redirect safety tests, fail-closed token relay lifecycle rules, no token/principal/assertion capture in long-lived delegates, SAML/GitHub protocol honesty tests, sign-out/session-expiry fail-closed checks, expanded redaction vectors, package-boundary guards, and explicit deferral of new product or architecture decisions.
- Findings deferred: Multi-IdP routing, tenant membership validation, account provisioning, role mapping, token exchange service design, provider certification, custom token renewal UX, and any new auth architecture policy remain deferred to owning future stories or product/architecture review.
- Final recommendation: ready-for-dev

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --no-restore --filter "FullyQualifiedName~Auth|FullyQualifiedName~FrontComposerAuthentication" -p:UseSharedCompilation=false` (red phase: expected missing auth bridge types)
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~Auth|FullyQualifiedName~FrontComposerAuthentication" -p:UseSharedCompilation=false` (44/0/0)
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~Auth|FullyQualifiedName~FrontComposerAuthentication|FullyQualifiedName~ProjectionSubscriptionServiceTests|FullyQualifiedName~Counter" -p:UseSharedCompilation=false` (80/0/0)
- `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` (0 warnings)
- `dotnet test Hexalith.FrontComposer.sln --no-build -p:UseSharedCompilation=false` (Contracts 148/0/0, Shell 1409/0/0, SourceTools 587/0/0, Bench 2/0/0)

### Completion Notes List

- Added `FrontComposerAuthenticationOptions` with OIDC, SAML2 handler-hook, GitHub OAuth, and custom/brokered provider recipes, validation enforcing the single-provider v1 constraint, HTTPS metadata outside Development, claim alias presence, protocol honesty, and GitHub broker requirements.
- Added Shell auth registration through `AddHexalithFrontComposerAuthentication`, keeping provider-specific types inside Shell auth code while replacing `IUserContextAccessor` and `IAuthRedirector` only when auth is configured.
- Added claims-backed server and authentication-state user context adapters with trim-only normalization, case-preserving tenant IDs, multi-valued/empty/colon/conflicting-alias fail-closed behavior, and sanitized HFC2012 diagnostics.
- Added safe return URL sanitization and a configured challenge redirector that preserves only local paths.
- Added per-operation access token relay through `FrontComposerAccessTokenProvider`, EventStore options wiring, and SignalR subscription preflight/wrapper guards so missing, empty, or canceled required tokens prevent connection/startup work.
- Added HFC2011-HFC2014 runtime diagnostic reservations and release notes.
- Preserved Counter's default credential-free demo accessor and added an opt-in sample-only fake-auth accessor plus README registration guidance.
- Added auth options, claims, redirect, token relay, DI, SignalR token-failure, storage no-token-write, and provider-boundary tests using fake/local handlers and source guards only.

### File List

- `Directory.Packages.props`
- `_bmad-output/implementation-artifacts/7-1-oidc-saml-authentication-integration.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `samples/Counter/Counter.Web/CounterFakeAuthUserContextAccessor.cs`
- `samples/Counter/Counter.Web/Program.cs`
- `samples/Counter/Counter.Web/README.md`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`
- `src/Hexalith.FrontComposer.Shell/AnalyzerReleases.Unshipped.md`
- `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`
- `src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreAccessTokenGuard.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthenticationOptions.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Auth/AuthenticationStateUserContextAccessor.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Auth/ClaimsPrincipalUserContextAccessor.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAccessTokenProvider.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAuthRedirector.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAuthenticationException.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAuthenticationOptionsValidator.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerClaimExtractor.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerReturnUrl.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/AuthBoundaryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Extensions/FrontComposerAuthenticationServiceExtensionsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/CapturingLogger.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/ClaimsPrincipalUserContextAccessorTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAccessTokenProviderTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAuthRedirectorTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAuthenticationOptionsTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/TestHostEnvironment.cs`
