# Sprint Change Proposal - Initialize FrontComposer AppHost security through the EventStore Aspire extension

_Workflow: bmad-correct-course . Date: 2026-06-26 . Mode: Batch . Author: Administrator . Status: Approved and implemented_

> Trigger: `HexalithEventStoreSecurityExtensions to initialize the security service in aspire host`.
> The FrontComposer AppHost still hand-wires Keycloak and auth environment variables even though
> `Hexalith.EventStore.Aspire` now owns a reusable `HexalithEventStoreSecurityExtensions` API and the
> EventStore AppHost already uses it.

---

## Section 1 - Issue Summary

**Problem.** `src/Hexalith.FrontComposer.AppHost/Program.cs` creates the Keycloak resource directly
with `builder.AddKeycloak("keycloak", 8180)` and manually applies the JWT/OIDC/EventStore-client
environment variables to EventStore, Tenants, Admin.Server, Admin.UI, and Tenants.UI. This duplicates
the canonical EventStore Aspire security helper and leaves FrontComposer's host on a parallel local
security composition path.

**Why this matters.**

- The EventStore AppHost already initializes security with
  `HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();`.
- The shared extension carries behavior FrontComposer currently bypasses, including the canonical
  `security` resource, `EnableKeycloak=false` handling, optional persistent Keycloak reuse, and fixed
  persistent-port overrides through `KeycloakPersistent`, `KeycloakHttpPort`, and
  `KeycloakManagementPort`.
- Keeping duplicate env-var wiring in FrontComposer makes future changes to EventStore security easy
  to apply in one AppHost but miss in the other.

**Issue type:** technical duplication / local Aspire host composition drift discovered during
implementation. This is a deployment-composition correction, not a product-scope change.

**Evidence.**

| Location | Current behavior |
|---|---|
| `src/Hexalith.FrontComposer.AppHost/Program.cs` | Manually calls `builder.AddKeycloak("keycloak", 8180)`, builds `realmUrl`, and hand-sets `Authentication__JwtBearer__*`, `Authentication__OpenIdConnect__*`, and `EventStore__Authentication__*` variables. |
| `Hexalith.EventStore/src/Hexalith.EventStore.AppHost/Program.cs` | Uses `builder.AddHexalithEventStoreSecurity()` and chains `WithJwtBearerSecurity`, `WithEventStoreClientCredentials`, and related helpers. |
| `Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreSecurityExtensions.cs` | Provides the reusable AppHost security resource and all service wiring helpers FrontComposer needs. |

---

## Section 2 - Impact Analysis

### Epic Impact

No epic changes. This reinforces Epic 1 / FR10's bootstrap and deployment-composition expectations
without adding, removing, resequencing, or redefining any epic.

### Story Impact

No numbered story currently owns this AppHost cleanup. This proposal should be the change record for
the implementation. It is closest to prior security/AppHost correct-course work:

- `sprint-change-proposal-2026-06-14-shell-security-helper.md`
- `sprint-change-proposal-2026-06-09-shell-account-hamburger.md`
- `sprint-change-proposal-2026-06-07.md` for Aspire/Keycloak version alignment

### Artifact Conflicts

| Artifact | Impact | Action |
|---|---|---|
| PRD | N/A. The project explicitly has no authored PRD; `epics.md` is the planning source of record. | None |
| Epics | No scope change. FR10 is reinforced by cleaner AppHost composition. | No edit required |
| Architecture | No product architecture change. Optional note only if we want to document that the FrontComposer AppHost consumes EventStore's shared security helper. | Optional |
| AppHost code | Directly impacted. Replace local Keycloak/resource/env-var composition with `HexalithEventStoreSecurityExtensions`. | Edit |
| AppHost project file | `Aspire.Hosting.Keycloak` may no longer be directly used by FrontComposer AppHost code after the extension is adopted; EventStore.Aspire still owns that dependency. | Remove direct package reference if build stays clean |
| Realm import JSON | Reused by the shared extension through the same `./KeycloakRealms` default. | No edit |
| Tests / verification | No existing FrontComposer AppHost tests found. Add at least build verification; optionally add a source-level governance guard if this duplication is likely to regress. | Verify |

### Technical Impact

- `FrontComposer.AppHost` will initialize security through `builder.AddHexalithEventStoreSecurity()`.
- The visible Aspire resource name changes from `keycloak` to the extension's default `security`,
  aligning with EventStore AppHost. If preserving the old dashboard name is required, pass
  `new HexalithEventStoreSecurityOptions { ResourceName = "keycloak" }`, but the recommended path is
  to use the shared default.
- JWT bearer settings for EventStore, Tenants, and Admin.Server remain equivalent.
- EventStore client credentials for Admin.UI remain equivalent, with
  `EventStore__AdminServer__SwaggerUrl` still added by the FrontComposer host.
- Tenants.UI keeps both JWT bearer settings and interactive OIDC settings, now supplied through
  `WithJwtBearerSecurity(...)` and `WithOpenIdConnectSecurity(...)`.
- `EnableKeycloak=false` behavior remains: the extension returns `null`; the host still sets only the
  Admin.UI Swagger URL in the no-Keycloak branch.

---

## Section 3 - Recommended Approach

**Selected path: Option 1 - Direct Adjustment.** Effort **Low**, risk **Low-Medium**.

This is the smallest sustainable fix: keep the existing topology, but replace duplicate FrontComposer
security wiring with the shared EventStore Aspire helper. No rollback or MVP review is justified.

**Rejected alternatives.**

- **Rollback:** not applicable. There is no feature to undo; this is deduplication and alignment.
- **PRD/MVP review:** not applicable. No user-facing requirements or epic boundaries change.
- **Keep manual wiring:** rejected because it preserves two security composition paths that will drift.

**Primary risk.** The Aspire dashboard resource name changes from `keycloak` to `security`. Search did
not reveal a FrontComposer test depending on the `keycloak` resource name, but implementation should
still smoke-check the Aspire dashboard and any local scripts.

---

## Section 4 - Detailed Change Proposals

### 4a. `src/Hexalith.FrontComposer.AppHost/Program.cs`

**OLD:**

```csharp
IResourceBuilder<KeycloakResource>? keycloak = null;
ReferenceExpression? realmUrl = null;
if (!string.Equals(builder.Configuration["EnableKeycloak"], "false", StringComparison.OrdinalIgnoreCase)) {
    keycloak = builder.AddKeycloak("keycloak", 8180)
        .WithRealmImport("./KeycloakRealms");
    EndpointReference keycloakEndpoint = keycloak.GetEndpoint("http");
    realmUrl = ReferenceExpression.Create($"{keycloakEndpoint}/realms/hexalith");
}
```

Later, each secured resource manually calls `WithReference(keycloak)`, `WaitFor(keycloak)`, and
sets JWT/OIDC/EventStore authentication environment variables.

**NEW:**

```csharp
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();
```

Replace the manual wiring block with the shared helpers:

```csharp
if (security is not null) {
    _ = eventStore.WithJwtBearerSecurity(security);

    _ = tenants.WithJwtBearerSecurity(security);

    _ = adminServer.WithJwtBearerSecurity(security);

    _ = adminUI
        .WithEventStoreClientCredentials(security)
        .WithEnvironment("EventStore__AdminServer__SwaggerUrl", ReferenceExpression.Create($"{adminServerHttps}/swagger/index.html"));

    _ = tenantsUI
        .WithJwtBearerSecurity(security)
        .WithOpenIdConnectSecurity(
            security,
            "hexalith-tenants-ui",
            "tenants-ui-dev-secret");
}
else {
    _ = adminUI.WithEnvironment("EventStore__AdminServer__SwaggerUrl", ReferenceExpression.Create($"{adminServerHttps}/swagger/index.html"));
}
```

**Rationale:** Centralizes the security resource and environment contract in
`Hexalith.EventStore.Aspire`, matching the EventStore AppHost and reducing local duplication.

### 4b. `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`

**OLD:**

```xml
<PackageReference Include="Aspire.Hosting.AppHost" />
<PackageReference Include="Aspire.Hosting.Keycloak" />
```

**NEW:**

```xml
<PackageReference Include="Aspire.Hosting.AppHost" />
```

Keep the removal only if the AppHost builds clean. `Hexalith.EventStore.Aspire` still owns the
`Aspire.Hosting.Keycloak` package because the extension implementation uses `AddKeycloak` and
`KeycloakResource`.

### 4c. Optional source-level regression guard

If we want a cheap guard against reintroducing parallel security wiring, add a test that scans
`src/Hexalith.FrontComposer.AppHost/Program.cs` and asserts:

- it contains `AddHexalithEventStoreSecurity`;
- it does not contain `builder.AddKeycloak(`;
- it does not manually set `Authentication__JwtBearer__Authority` or
  `Authentication__OpenIdConnect__Authority`.

This is optional because there is no AppHost test project today, but it would be consistent with the
repo's governance-test style.

### 4d. Verification plan

Required:

```sh
dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj -c Release
```

Recommended smoke checks:

- Start `Hexalith.FrontComposer.AppHost` after the change and confirm the Aspire dashboard shows a
  running `security` resource when Keycloak is enabled.
- Confirm `EnableKeycloak=false` still starts the stack and leaves Admin.UI with
  `EventStore__AdminServer__SwaggerUrl`.
- With Keycloak enabled, confirm Tenants.UI sign-in still uses the `hexalith-tenants-ui` client and
  EventStore/Tenants/Admin.Server accept JWT bearer discovery against the shared realm URL.
- If persistent reuse is enabled, confirm `KeycloakPersistent=true` and optional port overrides are
  honored by the FrontComposer AppHost, which is the behavior the shared extension adds.

---

## Section 5 - Implementation Handoff

**Scope classification: Minor.** One AppHost and one project file are affected. No PRD, epic,
story, UX, domain, schema, or public package API changes are required.

**Route to:** Developer agent for direct implementation after approval.

**Implementation tasks:**

1. Replace manual Keycloak initialization in `src/Hexalith.FrontComposer.AppHost/Program.cs` with
   `builder.AddHexalithEventStoreSecurity()`.
2. Replace manual JWT/OIDC/EventStore-client environment variable wiring with
   `WithJwtBearerSecurity`, `WithEventStoreClientCredentials`, and `WithOpenIdConnectSecurity`.
3. Remove the direct `Aspire.Hosting.Keycloak` package reference if the AppHost no longer needs it.
4. Run the Release AppHost build.
5. Perform the targeted Aspire smoke checks above if local runtime prerequisites are available.

**Success criteria:**

- `Program.cs` has a single security initialization path via `HexalithEventStoreSecurityExtensions`.
- The FrontComposer AppHost behavior remains equivalent for JWT bearer, Admin.UI credentials, and
  Tenants.UI OIDC sign-in.
- Shared extension features (`KeycloakPersistent`, fixed persistent ports, canonical `security`
  resource) work in FrontComposer AppHost.
- Release build passes with `TreatWarningsAsErrors=true`.

**Implementation evidence - completed 2026-06-26:**

- `src/Hexalith.FrontComposer.AppHost/Program.cs` now calls
  `builder.AddHexalithEventStoreSecurity()` and uses `WithJwtBearerSecurity`,
  `WithEventStoreClientCredentials`, and `WithOpenIdConnectSecurity`.
- The old local `builder.AddKeycloak(...)` path and manual
  `Authentication__JwtBearer__*` / `Authentication__OpenIdConnect__*` environment wiring were removed.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` no longer directly
  references `Aspire.Hosting.Keycloak`; `Hexalith.EventStore.Aspire` owns the Keycloak hosting
  dependency.
- Verification: `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj -c Release`
  passed with 0 warnings and 0 errors.
- Source scan verified no remaining `builder.AddKeycloak`, `Authentication__JwtBearer__Authority`, or
  `Authentication__OpenIdConnect__Authority` occurrences under `src/Hexalith.FrontComposer.AppHost`.

---

## Checklist Status - Change Navigation

- **1.1 Triggering story:** N/A. Direct AppHost composition correction requested by user.
- **1.2 Core problem:** Done. Duplicate/manual security initialization in FrontComposer AppHost.
- **1.3 Evidence:** Done. Program.cs manual wiring vs. EventStore shared extension and EventStore AppHost usage.
- **2.1 Current epic impact:** Done. No epic change.
- **2.2 Epic-level changes:** N/A.
- **2.3 Future epic review:** Done. No future epic invalidated.
- **2.4 New epic need:** N/A.
- **2.5 Epic order/priority:** N/A.
- **3.1 PRD conflict:** N/A. No authored PRD exists; `epics.md` is source of record.
- **3.2 Architecture conflict:** Done. No required architecture edit; optional note only.
- **3.3 UI/UX conflict:** N/A.
- **3.4 Other artifacts:** Done. AppHost project file and verification affected.
- **4.1 Direct Adjustment:** Viable, selected. Low effort, Low-Medium risk.
- **4.2 Rollback:** Not viable / not applicable.
- **4.3 PRD MVP Review:** Not viable / not applicable.
- **4.4 Recommended path:** Done. Direct Adjustment.
- **5.1 Issue summary:** Done.
- **5.2 Impact and artifact needs:** Done.
- **5.3 Path forward:** Done.
- **5.4 MVP impact/action plan:** Done. MVP unaffected.
- **5.5 Handoff plan:** Done. Developer agent after approval.
- **6.1 Checklist completion:** Done, with approval pending.
- **6.2 Proposal accuracy:** Done.
- **6.3 User approval:** Done. Approved by Administrator on 2026-06-26.
- **6.4 Sprint-status update:** N/A. No epic/story inventory changes.
- **6.5 Next steps/handoff:** Done. Routed to Developer agent and implemented directly.
