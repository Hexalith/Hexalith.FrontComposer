---
title: 'FrontComposer UI for Tenants and Parties Modules'
type: 'feature'
created: '2026-07-03'
status: 'done'
baseline_commit: '7b4235c4b87eae253e2a7061cb05ee4579c38e24'
context:
  - references/Hexalith.AI.Tools/hexalith-ux-instructions.md
  - _bmad-output/project-context.md
  - references/Hexalith.Tenants/_bmad-output/project-context.md
  - references/Hexalith.Parties/_bmad-output/project-context.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer currently launches the Tenants UI as the primary module UI, while Parties remains available only through its own standalone host. There is no repo-owned `frontcomposer-ui` host that presents Tenants and Parties modules together through one FrontComposer shell.

**Approach:** Add a `Hexalith.FrontComposer.UI` Blazor Server host, modelled on `Hexalith.Tenants.UI`, that registers both Tenants and Parties UI modules into one FrontComposer shell. Parties must use the same Tenants-style host composition pattern: FrontComposer quickstart, Fluent UI V5, server-side OIDC/circuit auth, bearer token relay to gateway clients, and AppHost environment wiring.

## Boundaries & Constraints

**Always:** Use FrontComposer and Fluent UI V5 components; no raw interactive controls or theme redefinition. Keep the new host in the FrontComposer repo unless a submodule compatibility change is unavoidable. Reference root-declared submodules under `references/` only; do not initialize nested submodules. Preserve Tenants UI behavior, including tenant/global administrator navigation, EventStore command gateway wiring, tenant query gateway wiring, and optional Memories search degradation. Preserve Parties admin/consumer route authorization, party binding fail-closed behavior, and support-safe UI states.

**Ask First:** Any change to Tenants or Parties domain contracts, event schemas, public API routes, Keycloak realm imports, DAPR access-control policy, or persistent state/projection behavior. Any decision to remove the existing `tenants-ui` AppHost resource instead of adding `frontcomposer-ui` alongside it.

**Never:** Do not hand-edit generated code, add package versions to `.csproj`, use `.sln`, use recursive submodule updates, or replace module UI pages with copied/ad hoc versions. Do not expose JWTs, access tokens, raw EventStore payloads, cursor internals, stack traces, party identifiers in error copy, or internal correlation details.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Combined host boot | AppHost starts EventStore, Tenants, Parties, and `frontcomposer-ui` with Keycloak enabled | One UI host opens with FrontComposer navigation for Tenants and Parties routes; authenticated gateway calls relay the signed-in user's access token | Missing optional Memories search degrades Tenants search only; missing Parties base URL leaves Parties clients disabled until configured |
| No OIDC provider | `Authentication:OpenIdConnect:*` is absent | Host still boots for degraded/test mode; authorization policies resolve and protected routes fail closed | Unauthenticated protected routes challenge or show not-authorized state without leaking internals |
| Parties consumer binding absent | Consumer role user has no single `party_id` plus tenant claim | Consumer self-service routes remain fail-closed or redirect to the no-binding state | No fallback party id, tenant id, or claim payload is rendered |
| Gateway configuration absent | EventStore, Tenants, or Parties base URLs are missing | Host composes inert/unavailable gateway services using existing module degraded behavior | UI shows module-owned unavailable/degraded states rather than throwing during startup |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Program.cs` -- golden host pattern for FrontComposer quickstart, server security, EventStore/Tenants gateway clients, and Memories search.
- `references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/*` -- reusable Tenants route components and shell registration assembly.
- `references/Hexalith.Parties/src/Hexalith.Parties.UI/Program.cs` -- source of Parties module service registrations, route authorization, admin/consumer portals, and freshness services.
- `references/Hexalith.Parties/src/Hexalith.Parties.AdminPortal` and `references/Hexalith.Parties/src/Hexalith.Parties.ConsumerPortal` -- routeable Parties UI module assemblies to expose from the combined host.
- `src/Hexalith.FrontComposer.AppHost/Program.cs` -- Aspire composition that must add Parties runtime and `frontcomposer-ui` resource wiring.
- `src/Hexalith.FrontComposer.AppHost/ProjectMetadataPaths.cs` and sibling metadata files -- path-only resource metadata wrappers for root-declared submodule projects.
- `Hexalith.FrontComposer.slnx`, `deps.local.props`, `Directory.Packages.props` -- solution/project-reference/package-version boundaries for the new host.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj` -- create a non-packable Web SDK host referencing FrontComposer Shell, Tenants UI/client dependencies, and Parties adopter-facing UI/client projects -- provides the combined executable UI surface.
- [x] `src/Hexalith.FrontComposer.UI/Program.cs` -- implement Tenants-style service composition for both modules, including Fluent UI, FrontComposer quickstart scans, `AddHexalithDomain` for Tenants and Parties, server security/token relay, Tenants gateways, Parties auth/claims/self-scope/admin/freshness/client registrations, and degraded fallbacks.
- [x] `src/Hexalith.FrontComposer.UI/Components/*` -- add App/Routes/Layout/imports for a single FrontComposer shell that routes Tenants and Parties assemblies, uses `AuthorizeRouteView`, and preserves focus-on-navigate.
- [x] `src/Hexalith.FrontComposer.AppHost/*` -- add Parties and `frontcomposer-ui` project metadata/resources, EventStore domain-service routing for `party`, SignalR hub URL, module base URLs, tenant config, and OIDC client settings while keeping existing `tenants-ui` unless the user approves removal.
- [x] `deps.local.props` and `Hexalith.FrontComposer.slnx` -- add root Parties path/property and include the new UI project without turning submodule projects into default Release solution builds.
- [x] Focused tests or build guards -- add/adjust minimal verification that the combined host registers both module route assemblies and AppHost metadata paths resolve.

**Acceptance Criteria:**
- Given the root submodules are initialized, when `dotnet build src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj -c Debug` runs, then the combined host builds without warnings.
- Given the AppHost builds, when its resource graph is inspected or launched, then `frontcomposer-ui` has references/waits for EventStore, Tenants, and Parties and receives `EventStore`, `Tenants`, `Parties`, and OIDC environment settings.
- Given OIDC is configured, when a user signs in through `frontcomposer-ui`, then Tenants and Parties gateway HTTP clients use the FrontComposer bearer-token relay pattern.
- Given OIDC is not configured, when the host starts, then authorization policies still resolve and protected module routes fail closed rather than crashing startup.

## Spec Change Log

## Design Notes

The key design choice is a new host instead of editing `Hexalith.Tenants.UI` or `Hexalith.Parties.UI` into a shared executable. Tenants is the golden pattern because it already uses the current FrontComposer server security helper and token relay; Parties should be integrated by applying that same host-level pattern around its module services and route assemblies.

Keep the existing module pages as the source of truth. If a referenced Web SDK UI assembly cannot be consumed cleanly as a route/component assembly, prefer extracting only the minimum reusable registration seam in the owning submodule over copying pages into FrontComposer.

## Verification

**Commands:**
- `dotnet build src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj -c Debug` -- passed with 0 warnings and 0 errors.
- `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj -c Debug` -- passed with 0 warnings and 0 errors.
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~FrontComposerAuthenticationOptionsTests|FullyQualifiedName~AuthBoundaryTests.ProviderSpecificAuthenticationTypes_DoNotLeakOutsideShellAuthArea"` -- passed; covers Shell-owned role-claim option plumbing and the provider-specific auth boundary.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- failed on pre-existing governance/doc checks unrelated to this change: missing accessibility diagnostic link in `docs/reference/components/settings.md`, release/nightly workflow `submodules: true` expectations, release provenance attestation expectation, dependency-mode governance lookup, and package-inventory lockstep check.

## Suggested Review Order

**Combined Host Composition**

- Entry point composes Tenants and Parties under one FrontComposer shell.
  [`Program.cs:22`](../../src/Hexalith.FrontComposer.UI/Program.cs#L22)

- Shell-owned security keeps role claims without leaking provider option types.
  [`Program.cs:36`](../../src/Hexalith.FrontComposer.UI/Program.cs#L36)

- Route table exposes Tenants plus Parties admin and consumer assemblies.
  [`Routes.razor:31`](../../src/Hexalith.FrontComposer.UI/Components/Routes.razor#L31)

- Host document loads module styles and root script shims.
  [`App.razor:7`](../../src/Hexalith.FrontComposer.UI/Components/App.razor#L7)

**Reusable Module Seams**

- Tenants standalone registration is reusable by the combined host.
  [`TenantsUiServiceCollectionExtensions.cs:27`](../../references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs#L27)

- Parties now follows the same host-level module-registration pattern.
  [`PartiesUiServiceCollectionExtensions.cs:28`](../../references/Hexalith.Parties/src/Hexalith.Parties.UI/Extensions/PartiesUiServiceCollectionExtensions.cs#L28)

- Shell option carries role-claim mapping inside the auth boundary.
  [`FrontComposerAuthenticationOptions.cs:38`](../../src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthenticationOptions.cs#L38)

- OIDC handler applies role-claim mapping only inside Shell auth code.
  [`FrontComposerAuthenticationServiceExtensions.cs:276`](../../src/Hexalith.FrontComposer.Shell/Extensions/FrontComposerAuthenticationServiceExtensions.cs#L276)

**Build And Static Assets**

- Project paths flow into submodules without nested submodule initialization.
  [`Hexalith.FrontComposer.UI.csproj:9`](../../src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj#L9)

- Referenced Web hosts keep styles but not duplicate root assets.
  [`Hexalith.FrontComposer.UI.csproj:36`](../../src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj#L36)

- Parties submodule now recognizes root-declared sibling references.
  [`Directory.Build.props:4`](../../references/Hexalith.Parties/Directory.Build.props#L4)

**Aspire Wiring**

- EventStore routes wildcard party domain traffic to the Parties service.
  [`Program.cs:26`](../../src/Hexalith.FrontComposer.AppHost/Program.cs#L26)

- Parties runs as a domain module and depends on Tenants.
  [`Program.cs:62`](../../src/Hexalith.FrontComposer.AppHost/Program.cs#L62)

- `frontcomposer-ui` references EventStore, Tenants, and Parties.
  [`Program.cs:119`](../../src/Hexalith.FrontComposer.AppHost/Program.cs#L119)

- Parties metadata resolves the root-declared submodule project.
  [`HexalithParties.cs:4`](../../src/Hexalith.FrontComposer.AppHost/HexalithParties.cs#L4)

**Verification**

- Test covers the new Shell role-claim option.
  [`FrontComposerAuthenticationOptionsTests.cs:292`](../../tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/FrontComposerAuthenticationOptionsTests.cs#L292)
