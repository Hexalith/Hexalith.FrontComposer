---
title: 'Reconcile AppHost Package Mode'
type: 'bugfix'
created: '2026-09-01'
status: 'blocked'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Release/package mode suppresses both the source and package references that the combined FrontComposer UI needs from Tenants and Parties, so the full AppHost build fails with 23 missing-namespace/symbol errors and no test constructs the real Aspire application model.

**Approach:** Restore the canonical package-only Release graph using package identities published and governed by the owning modules, then make the full AppHost build and model construction blocking validation paths without changing the approved topology.

## Boundaries & Constraints

**Always:** Keep Debug on root-declared source projects and Release on centrally versioned packages; preserve the current EventStore/admin/domain-module/DAPR/reference/wait topology; keep package versions catalog-owned; leave the deferred-work ledger and `references/Hexalith.*` contents unchanged without explicit cross-repository authority.

**Block If:** `Hexalith.Tenants.UI` or `Hexalith.Parties.UI` is unavailable from the configured package source, or the owning modules have not exposed an equivalent supported packageable composition seam.

**Never:** Enable missing package identities and call the resulting `NU1101` progress; silently use source-project fallbacks in Release package mode; copy module UI implementation into FrontComposer; weaken build/audit gates; redesign topology or container ownership.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Release package graph | Published owner-supported Tenants and Parties UI composition packages | AppHost and combined UI restore/build transitively with EventStore Aspire and module dependencies as packages | Fail on a missing identity or any external source-project edge |
| Aspire model | Package-mode AppHost with Keycloak disabled | Construct the model without starting resources and observe the approved resources, DAPR components, references, and waits | Fail on missing/extra semantic topology edges |
| Debug source graph | Root-declared submodules available | AppHost continues to build from source for local debugging | Fail on package fallback or nested-submodule use |

</intent-contract>

## Code Map

- `Directory.Build.props:12-26`, `deps.local.props:5-21`, `deps.nuget.props:3-10` -- canonical Debug/source and Release/package selectors; preserve.
- `src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj:17,22-41` -- default-false package gate creates the Release selection hole; its two host package identities cannot currently restore.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj:16-56` -- EventStore Aspire package/source switch and hosted-project build edges.
- `src/Hexalith.FrontComposer.AppHost/Program.cs:26-183` -- approved application model to preserve and observe through Aspire testing.
- `Hexalith.FrontComposer.slnx:29-49` -- stale Release exclusions prevent Gate 2 from validating AppHost/UI.
- `tests/Hexalith.FrontComposer.Shell.Tests/Integration/FrontComposerUiAppHostTests.cs` -- current source-text checks; candidate home for model assertions when paired with an aliased non-resource AppHost reference and `Aspire.Hosting.Testing`.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:761-790` and `Governance/AppHostNuGetAuditPolicyTests.cs:29-85` -- current shallow selection/audit checks; extend to evaluate the real Release graph.
- `references/Hexalith.Builds/Props/Directory.Packages.props:8,11-12,42,70-87` -- read-only central identities: EventStore Aspire `3.101.0`, Tenants UI `5.6.0`, Parties UI/portals `1.0.0`.
- `references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj:4` and `references/Hexalith.Tenants/tools/release-packages.json` -- read-only evidence that Tenants UI is non-packable and omitted from release inventory.
- `references/Hexalith.Parties/tools/release-packages.json` -- read-only evidence that Parties UI is omitted while AdminPortal and ConsumerPortal are published.

## Tasks & Acceptance

**Execution:**
- Owning Tenants/Parties repositories -- publish supported reusable UI composition assemblies under governed identities, or provide equivalent packageable seams -- unblock package-only consumption without copying host code.
- `src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj` -- after both identities are available, remove/derive the redundant package gate from canonical dependency mode -- make Release select exactly one dependency form.
- `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj` and `Integration/FrontComposerAppHostPackageModeTests.cs` -- reference the AppHost as a non-resource and construct the model with Keycloak disabled -- verify EventStore/admin/domain/DAPR/reference/wait semantics without starting containers.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AppHostNuGetAuditPolicyTests.cs`, `CiGovernanceTests.cs`, and `Hexalith.FrontComposer.slnx` -- make effective Release package selection and the full transitive AppHost build blocking governance evidence.

**Acceptance Criteria:**
- Given package-only Release mode, when the AppHost graph restores and builds transitively, then all required module identities resolve from the configured source, EventStore Aspire uses the imported catalog selection, and no external Hexalith source-project fallback enters the graph.
- Given the package-mode AppHost, when the focused Aspire test constructs its model without starting resources, then the approved project resources, `statestore`/`pubsub`, DAPR sidecars, references, and waits are observed.
- Given Debug mode, when the AppHost builds, then it continues to use only root-declared source projects and the approved topology is unchanged.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Release -m:1 /nr:false -p:NuGetAudit=false` -- expected after unblocking: zero warnings/errors with full project-reference traversal.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false -p:NuGetAudit=false` -- expected after unblocking: Gate 2-equivalent Release graph includes AppHost validation.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -class Hexalith.FrontComposer.Shell.Tests.Integration.FrontComposerAppHostPackageModeTests` -- expected after unblocking: package/model assertions pass.

## Auto Run Result

Status: blocked
Blocking condition: required Release identities `Hexalith.Tenants.UI/5.6.0` and `Hexalith.Parties.UI/1.0.0` are absent from nuget.org, while their owning release inventories do not publish them; resolving that ownership/package boundary requires explicit cross-repository work.

Evidence: the default Release AppHost build reproduced 23 `CS0234`/`CS0103` errors. Setting `FrontComposerUiUsePublishedModulePackages=true` instead failed restore with `NU1101` for both identities. No production or test files were changed, and the deferred-work ledger was not edited.
