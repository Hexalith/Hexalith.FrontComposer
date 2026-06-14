# Sprint Change Proposal — Move shared security wiring out of Tenants into a FrontComposer Shell helper

_Workflow: bmad-correct-course · Date: 2026-06-14 · Mode: Batch · Author: Administrator_

> Direct follow-on to `sprint-change-proposal-2026-06-09-shell-account-hamburger.md`. That change
> moved the **sign in / out affordance** into the framework shell (`FcAccountMenu`) but left the
> **security wiring that makes it work** inside the **Tenants business module** host. This proposal
> relocates that generic technical code into framework-owned FrontComposer Shell helpers, so business
> (domain) modules carry only domain-specific security configuration.

---

## Section 1 — Issue Summary

**Problem.** Security/authentication **technical plumbing that is common to every FrontComposer
domain module** currently lives inside the `Hexalith.Tenants` business module
(`src/Hexalith.Tenants.UI/Program.cs` and `src/Hexalith.Tenants.UI/Services/Auth/TenantsTokenRelay.cs`).
This is a layering violation: a domain module should hold tenant-specific contracts/behaviors/flows,
not reusable framework infrastructure.

The Tenants repository's own `CLAUDE.md` states the rule being broken:
> *"Do not add boilerplate code that is common to domain modules here. … move the boilerplate into
> the appropriate technical module before consuming it. Typical homes for shared infrastructure …
> include `Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Commons` …"*

**Discovery.** Direct architectural review by the user during the account-menu/hamburger change:
*"le code gérant la sécurité devrait être dans un helper de frontcomposer car il s'agît de code
technique commun à tous les modules « domaine ». Les modules métier comme tenants ne devraient pas
contenir de code technique qui n'est pas spécifique aux tenants."*

**Issue type:** Architectural/layering correction (misplaced technical code) discovered during
implementation — a refactor, **not** a behavior change.

**Evidence — generic security code currently sitting in the Tenants module:**

| Location in `Hexalith.Tenants` | What it does | Tenant-specific? |
|---|---|---|
| `Program.cs`: `AddCascadingAuthenticationState()` + `Replace(AuthenticationStateProvider → ServerAuthenticationStateProvider)` | Flows the cookie-authenticated principal into interactive Server components, replacing the Shell's fail-closed `NullAuthenticationStateProvider` | ❌ No — pure Blazor-Server plumbing |
| `Services/Auth/TenantsTokenRelay.cs` — `TenantsUserTokenStore`, `CircuitServicesAccessor`, `TenantsCircuitServicesHandler`, `GatewayAuthorizationHandler`, `AddTenantsTokenRelay`, `AddGatewayAuthorization` | Circuit-safe capture + per-user OIDC bearer-token relay to an EventStore gateway | ❌ No — "Tenants" in name only; contains zero tenant logic |
| `Program.cs`: `Replace(IUserContextAccessor → ClaimsUserContextAccessor)` | Overrides the FC auth bridge's accessor; **hardcodes claim names** (`tenant_id`/`tenantId`/`tid`/`tenant`) that do **not** match the configured `eventstore:tenant` claim | ❌ No — and it is a **latent bug** (see Section 2 finding) |
| `Program.cs`: `authEnabled` OIDC-config probe | Generic "is an OIDC provider configured" boilerplate | ❌ No |
| `Program.cs`: `AddAuthorizationCore(GlobalAdministratorPolicy …)` | Authorization policy: role `GlobalAdministrator` + claim `eventstore:tenant=system`, key `TenantsFrontComposerRegistration.GlobalAdministratorPolicy` | ✅ **Yes** — policy *values/key* are domain config (stays) |

The framework side already owns the matching seams: `NullAuthenticationStateProvider` (fail-closed
default), `ClaimsPrincipalUserContextAccessor` (configurable superset), and
`AddHexalithFrontComposerAuthentication`. The Shell project carries
`FrameworkReference Microsoft.AspNetCore.App` and already references
`Microsoft.AspNetCore.Authentication.OpenIdConnect`, so it **can** host the relocated code with no new
dependencies and without violating the "dependency direction points down to Contracts" rule (NFR12).

---

## Section 2 — Impact Analysis

### Epic Impact — none
FrontComposer Epics 1–7 are DONE. This is Shell-territory technical hygiene touching **FR10** (DI
bootstrap path) and the auth bridge surface; it **modifies, adds, removes, or resequences no epic**.

### Story Impact
- No formal story (architectural correction). Recorded here. Touches the same surface as Epic 1
  Story 1.1 (bootstrap) and the auth-bridge work; the helper becomes part of the documented
  Quickstart → Domain → EventStore bootstrap story.

### Key finding (defect, not just hygiene)
`ClaimsUserContextAccessor` in Tenants resolves `TenantId` from claim names
`tenant_id`/`tenantId`/`tid`/`tenant`, but the Keycloak setup emits the tenant under
`eventstore:tenant` (configured in `AddHexalithFrontComposerAuthentication(... tenantClaimType:
"eventstore:tenant" ...)`). Because Tenants `Replace`s the FC accessor with this one **unconditionally
at the end of `Program.cs`**, the correct, configured `ClaimsPrincipalUserContextAccessor` is shadowed
by one that cannot see the real tenant claim. **Removing the Tenants override and relying on the FC
accessor both removes the misplaced code AND fixes the claim mismatch.** This must be verified against
any code that reads `IUserContextAccessor.TenantId` server-side.

### Artifact Conflicts
| Artifact | Impact | Action |
|---|---|---|
| **PRD** | N/A — none exists | none |
| **Architecture** (`_bmad-output/project-docs/architecture.md`) | §4 governance note (prior proposal) should record that server-auth-state + token-relay are now framework-owned helpers | ✏️ edit |
| **`epics.md` / Story 1.1** | Bootstrap path gains optional server-security helpers; reinforce, don't alter | ✏️ note |
| **Shell public API** | New public extension methods on `Hexalith.FrontComposer.Shell` | ⚠️ update `PublicAPI*.Shipped.txt` **if** this surface is baseline-tracked |
| **`docs/` (FC-DOC, Gate 2d)** | New helpers need a doc page per the FC-DOC contract | ✏️ add |
| **Tenants composition/unit tests** | Tests asserting `AddTenantsTokenRelay`/`ClaimsUserContextAccessor` registrations | ✏️ update |
| **Shell tests** | New DI-registration + relay-handler tests | ➕ add |
| **Memory `project_host_auth_state_provider.md`** | The manual host wiring it documents is superseded by the helper | ✏️ update after implementation |

### Technical Impact
- **FrontComposer Shell** gains framework-owned, render-mode-explicit security helpers. No new package
  references (uses existing `Microsoft.AspNetCore.App` framework ref + OIDC package).
- **Tenants** shrinks to domain-specific security *configuration* (which provider, which claim names,
  which policy, which gateways to authorize) calling framework helpers.
- **Cross-repo:** spans the FrontComposer repo + the `Hexalith.Tenants` submodule, and—per the prior
  proposal's pattern—may require propagation to the **Parties** repo's submodule pointers.
- **Behavior:** intended to be **behavior-preserving** for Tenants (minus the claim-mismatch fix,
  which is a correction). No epic/MVP/contract change.

---

## Section 3 — Recommended Approach

**Option 1 — Direct Adjustment (refactor-in-place across repos): ✅ Selected.** Effort **Medium**,
risk **Low–Medium**.

- **Option 2 — Rollback:** rejected. The account-menu feature is wanted and green; we are relocating
  its support code, not reverting it.
- **Option 3 — PRD/MVP review:** N/A. MVP and epics untouched.

**Rationale:** Putting shared security plumbing in the framework is exactly what both repos' guidance
mandates (Tenants `CLAUDE.md` boundary; FrontComposer as the "technical module"). It removes
duplication for every current and future domain module, fixes the latent claim-mismatch bug, and keeps
the auth affordance (already framework-owned) consistent with the wiring that powers it.

**Design note (render mode):** `ServerAuthenticationStateProvider` and `CircuitHandler` are
Blazor-**Server** concepts. The helpers are therefore exposed as an **explicit, Server-scoped opt-in**
(`…ServerAuthenticationState` / token relay), **not** folded unconditionally into the render-mode-
agnostic `AddHexalithFrontComposerAuthentication`. This keeps the auth bridge usable by non-Server
adopters while giving Server hosts a one-call path.

---

## Section 4 — Detailed Change Proposals

### 4a. FrontComposer Shell — new framework-owned helpers (`Hexalith.FrontComposer.Shell`)

**1. Server authentication-state helper** — new file
`Extensions/FrontComposerServerAuthenticationServiceExtensions.cs`:

```csharp
namespace Hexalith.FrontComposer.Shell.Extensions;

public static class FrontComposerServerAuthenticationServiceExtensions {
    /// <summary>
    /// Flows the cookie-authenticated principal into interactive Server components, replacing the
    /// fail-closed <c>NullAuthenticationStateProvider</c> the Quickstart registers. Call from a
    /// Blazor Server host once interactive OIDC sign-in is wired.
    /// </summary>
    public static IServiceCollection AddHexalithFrontComposerServerAuthenticationState(
        this IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.AddCascadingAuthenticationState();
        _ = services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>());
        return services;
    }
}
```

**2. Generic token-relay infrastructure** — move `TenantsTokenRelay.cs` into the Shell, renamed
framework-neutrally (new file `Services/Auth/FrontComposerTokenRelay.cs` + extensions):

| Tenants (remove) | FrontComposer Shell (add) |
|---|---|
| `TenantsUserTokenStore` | `FrontComposerUserTokenStore` |
| `CircuitServicesAccessor` | `CircuitServicesAccessor` (unchanged name; generic) |
| `TenantsCircuitServicesHandler` | `FrontComposerCircuitServicesHandler` |
| `GatewayAuthorizationHandler` | `FrontComposerGatewayAuthorizationHandler` |
| `AddTenantsTokenRelay()` | `AddHexalithFrontComposerTokenRelay()` |
| `IHttpClientBuilder.AddGatewayAuthorization()` | `IHttpClientBuilder.AddFrontComposerGatewayAuthorization()` |

Behavior preserved. Two refinements while moving:
- The hardcoded OIDC scheme literal `"Hexalith.FrontComposer.Oidc"` should bind to the existing
  `FrontComposerOpenIdConnectOptions.ChallengeScheme` constant rather than a duplicated string.
- `RequireHttpsMetadata = false` (local-dev affordance) should be driven by host environment / an
  option, not unconditionally forced, so production hosts keep HTTPS metadata enforcement. (Confirm
  desired default during implementation.)

**3. (Optional convenience umbrella)** — `AddHexalithFrontComposerServerSecurity(configure)` that
calls `AddHexalithFrontComposerAuthentication` + `AddHexalithFrontComposerServerAuthenticationState` +
`AddHexalithFrontComposerTokenRelay` in the correct order. Decide during implementation whether to
ship this now or keep the three granular calls.

**4. Tests** (`tests/Hexalith.FrontComposer.Shell.Tests`):
- DI-registration tests for each new helper (provider replaced, cascading state added, relay services
  present).
- A `FrontComposerGatewayAuthorizationHandler` test (attaches bearer when a token exists; leaves
  anonymous requests untouched).
- Keep the in-flight `FcAccountMenuAuthenticatedTests.cs` / `FcAccountMenu.razor.css` (already on disk).

### 4b. Tenants submodule — consume the helpers, delete the moved code

**`src/Hexalith.Tenants.UI/Program.cs`** — the `if (authEnabled)` block reduces to:

```csharp
if (authEnabled) {
    _ = builder.Services.AddHexalithFrontComposerAuthentication(o => o.UseKeycloak(
        oidcAuthority!,
        builder.Configuration["Authentication:OpenIdConnect:ClientId"]!,
        builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]!,
        tenantClaimType: "eventstore:tenant",
        userClaimType: "sub"));
    _ = builder.Services.AddHexalithFrontComposerServerAuthenticationState(); // was: AddCascadingAuthenticationState + Replace<…ServerAuthenticationStateProvider>
    _ = builder.Services.AddHexalithFrontComposerTokenRelay();                // was: AddTenantsTokenRelay
}
```

- **Delete** `src/Hexalith.Tenants.UI/Services/Auth/TenantsTokenRelay.cs`.
- **Delete** `src/Hexalith.Tenants.UI/Services/ClaimsUserContextAccessor.cs` and **remove** the
  `builder.Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor, ClaimsUserContextAccessor>())`
  line — rely on the FC bridge's configured `ClaimsPrincipalUserContextAccessor` (this also fixes the
  `eventstore:tenant` claim mismatch). **Verify** any server-side `IUserContextAccessor.TenantId`
  reader still resolves correctly.
- Gateway clients keep their authorization, switching `.AddGatewayAuthorization()` →
  `.AddFrontComposerGatewayAuthorization()`.

**Stays in Tenants (genuinely domain-specific):** the `authEnabled` config probe, the Keycloak
provider choice + claim-type mapping, the `GlobalAdministratorPolicy` (`AddAuthorizationCore`), and the
tenant gateway registrations.

**Tenants tests:** update `TenantsUiCompositionTests.cs` (and any test referencing
`AddTenantsTokenRelay` / `ClaimsUserContextAccessor`) to assert the new framework helpers instead.

### 4c. Documentation
- `architecture.md` §4 — extend the prior governance note: server auth-state + token relay are
  framework-owned helpers; domain hosts call them.
- `docs/` — FC-DOC page(s) for the new public helpers (Gate 2d `validate-docs.ps1`).
- Update `PublicAPI*.Shipped.txt` **iff** the Shell surface is baseline-tracked.

### 4d. Verification — IMPLEMENTED 2026-06-14
- **FrontComposer Shell:** Debug **and** Release builds clean (`TreatWarningsAsErrors`, 0 warnings).
  New helper tests green: `FrontComposerServerSecurityServiceExtensionsTests` **5/5**. Auth /
  extensions / account-menu lane **196/196** (no regressions; includes the 5 new tests).
- **Tenants.UI:** builds clean (0 warnings); full UI suite **670/670** green
  (`DiffEngine_Disabled=true`).
- **Files added (FrontComposer):** `Extensions/FrontComposerServerAuthenticationServiceExtensions.cs`
  (`AddHexalithFrontComposerServerAuthenticationState` + umbrella
  `AddHexalithFrontComposerServerSecurity`), `Extensions/FrontComposerTokenRelayServiceExtensions.cs`
  (`AddHexalithFrontComposerTokenRelay` + `AddFrontComposerGatewayAuthorization`),
  `Services/Auth/FrontComposerTokenRelay.cs` (relay infra),
  `tests/.../Extensions/FrontComposerServerSecurityServiceExtensionsTests.cs`.
- **Files changed (Tenants):** `Program.cs` (umbrella call + `AddFrontComposerGatewayAuthorization`,
  dead usings removed, `IUserContextAccessor` override removed); **deleted**
  `Services/Auth/TenantsTokenRelay.cs` and `Services/ClaimsUserContextAccessor.cs`.
- **Docs:** `architecture.md` §4 governance note updated.
- **Decisions applied:** umbrella helper shipped; relay `RequireHttpsMetadata` now gated on
  `IHostEnvironment.IsDevelopment()` (production keeps HTTPS-metadata enforcement).
- **Live-visual: OPEN** — start `Hexalith.FrontComposer.AppHost`, sign in, confirm header avatar shows
  the user, `IUserContextAccessor.TenantId` resolves the real `eventstore:tenant`, gateway calls carry
  the bearer token. (A leftover `tenants-ui` / `Counter.Web` instance was stopped to release build
  locks during implementation.)

---

## Section 5 — Implementation Handoff

**Scope classification: Moderate** — framework Shell + submodule + likely Parties propagation; no
epic/PRD/architecture-pattern change, MVP unaffected.

**Action plan (sequenced — note: opposite order from the 06-09 proposal, because Tenants now consumes
new FrontComposer API):**

1. **FrontComposer repo** — add the helpers + Shell tests + docs on a `feat/` branch → PR → merge.
   Suggested commit: `feat(shell): add server auth-state and token-relay helpers`. Verify Release
   build + default lane green. (If consumed via NuGet, this must publish before step 2; on the local
   `deps.local.props` ProjectReference path the submodule sees it immediately.)
2. **Tenants submodule** (`/frontcomposer/Hexalith.Tenants`) — delete `TenantsTokenRelay.cs` +
   `ClaimsUserContextAccessor.cs`, simplify `Program.cs`, update tests, on a `feat/` branch → PR →
   merge. Because the net effect removes infra and fixes the claim bug, classify carefully:
   `refactor(ui): use FrontComposer server-security helpers` (+ a `fix:` note for the tenant-claim
   resolution if user-visible). Then bump the `/frontcomposer` Tenants submodule pointer.
3. **Propagate to Parties** (`/parties`) — bump `/parties/Hexalith.FrontComposer` and
   `/parties/Hexalith.Tenants` pointers after steps 1–2 merge.
4. **Live-visual verification** under `Hexalith.FrontComposer.AppHost`, then re-run Parties AppHost.

**Recipients:**
- **Developer** — steps 1–2 (FrontComposer + Tenants), step 4 (verification).
- **Developer / `/parties` owner** — step 3.

**Constraints (project-context + CLAUDE.md):** no direct commits to `main` (feature branch + PR);
Conventional Commits (**this is a `refactor`/`fix`, not a `feat` for the Tenants side — don't trigger a
false minor bump for a move**); submodule edits require explicit approval (**granted via this
proposal**); never `--init --recursive`; `.slnx` only; `TreatWarningsAsErrors`.

**Success criteria:**
- No generic security/auth infrastructure remains in `Hexalith.Tenants.UI`; only domain-specific
  security *configuration* stays.
- FrontComposer Shell owns server auth-state + token-relay helpers, documented and tested.
- `IUserContextAccessor.TenantId` resolves the real `eventstore:tenant` claim (bug fixed).
- All configured tests green in each repo's CI lane; live `/tenants` sign-in works end-to-end.

---

## Checklist Status (Change Navigation)
- **§1 Trigger & Context:** ✅ Done (1.1 no formal story / architectural correction · 1.2 layering
  violation, refactor · 1.3 evidence table + Tenants `CLAUDE.md` rule + claim-mismatch finding)
- **§2 Epic Impact:** ✅ N/A (no epic changes)
- **§3 Artifact Conflicts:** 3.1 N/A (no PRD) · 3.2 ✅ architecture note + Shell public-API/docs ·
  3.3 N/A (no UX doc; no UI change) · 3.4 ✅ tests + docs + memory
- **§4 Path Forward:** ✅ Option 1 (Direct Adjustment / refactor across repos)
- **§5 Proposal Components:** ✅ this document
- **§6 Final Review/Handoff:** ✅ approved (Approve + implement now). 6.4 sprint-status.yaml: **N/A**
  (no epic add/remove/renumber).

## Implementation status (2026-06-14)
- ✅ Code implemented + tests green in both repos (see §4d). Builds clean Debug + Release (TWAE).
- ⏳ **Not yet committed** (working-tree changes only) — branch/commit/PR per the action plan when ready.
- ⏳ Parties submodule-pointer propagation (action-plan step 3).
- ⏳ Live-visual verification under the FrontComposer AppHost (action-plan step 4).
