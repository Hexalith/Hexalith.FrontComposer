# Aspire Upgrade Assessment

## Summary

| Area | Current | Target | Status |
|---|---:|---:|---|
| Aspire AppHost SDK | 13.4.6 | 13.4.6 | Already latest stable |
| Required AppHost TFM | net10.0 | net10.0 | Already compliant |
| Aspire CLI | 13.4.3 | 13.4.6 | Update recommended |
| Container runtime | Docker 29.4.3 | Available | OK |

The main AppHost is already using the latest stable Aspire AppHost SDK version published on NuGet (`13.4.6`). The remaining modernization work is format/validation focused: consolidate the AppHost SDK declaration to the Aspire 13+ project format, remove the redundant AppHost package reference, update the Aspire CLI, and validate the solution.

## Scope

- Solution: `Hexalith.FrontComposer.slnx`
- Primary AppHost: `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`
- Config: `aspire.config.json`
- Target version: `13.4.6`
- Required TFM: `net10.0`

## Current Aspire Version Detection

Signals found:

1. `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`
   - Uses legacy child SDK element: `<Sdk Name="Aspire.AppHost.Sdk" Version="13.4.6" />`
   - Target framework: `net10.0`
2. `Directory.Packages.props`
   - `Aspire.Hosting.AppHost` version: `13.4.6`
   - `Aspire.Hosting.Keycloak` version: `13.4.6-preview.1.26319.6`
3. NuGet query for `Aspire.AppHost.Sdk`
   - Latest stable: `13.4.6`
4. Aspire CLI
   - Installed: `13.4.3`
   - CLI reports an update is available.

Current version band: **13.4.x**.

## Component Inventory

### AppHost

- Path: `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`
- Format: `project-legacy` for Aspire 13+ because it uses `Sdk="Microsoft.NET.Sdk"` plus a child `Aspire.AppHost.Sdk` element.
- Consolidation needed: **Yes** — use `Sdk="Aspire.AppHost.Sdk/13.4.6"` on `<Project>`.
- Redundant package reference: **Yes** — `Aspire.Hosting.AppHost` is included by the AppHost SDK and can be removed from the AppHost project.

Registered AppHost resources in `src/Hexalith.FrontComposer.AppHost/Program.cs`:

- `eventstore`
- `eventstore-admin`
- `eventstore-admin-ui`
- `tenants`
- `sample`
- `tenants-ui`
- `counter-web`
- Keycloak-backed security resource via `AddHexalithEventStoreSecurity()` when enabled
- DAPR topology through Hexalith EventStore Aspire extension methods

### ServiceDefaults

Service defaults are provided by referenced platform projects:

- `references/Hexalith.Commons/src/libraries/Hexalith.Commons.ServiceDefaults/Hexalith.Commons.ServiceDefaults.csproj`
  - Contains `<IsAspireSharedProject>true</IsAspireSharedProject>`
  - References `Microsoft.Extensions.ServiceDiscovery`

### Aspire Packages

Root repository package versions:

- `Aspire.Hosting.AppHost` = `13.4.6`
- `Aspire.Hosting.Keycloak` = `13.4.6-preview.1.26319.6`

Notes:

- `Aspire.Hosting.Keycloak` is intentionally pinned to a preview build with an existing repo comment stating it should not be bumped independently from sibling AppHosts.
- `Microsoft.Extensions.ServiceDiscovery` is current at `10.7.0` in referenced platform repositories.
- Some referenced submodule projects use `Aspire.Hosting` and other Aspire hosting packages. Those are separate root-declared submodule repositories and should not be changed in this root upgrade unless explicitly required.

### Configuration Files

- Root `aspire.config.json` exists and points to the primary AppHost:
  - `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`
- No root `.aspire/settings.json` or root `apphost.run.json` migration is required.
- Referenced submodules contain their own Aspire config files and are out of scope for the root AppHost format update.

## TFM Requirements

- Target Aspire 13.4.6 requires `net10.0`.
- Primary AppHost already targets `net10.0`.
- Most solution projects target `net10.0`; source-tooling projects that intentionally target `netstandard2.0` are not AppHost projects and do not need TFM changes for this upgrade.

Projects needing TFM upgrade: **0**.

## Breaking Change Scan

Target version equals current version (`13.4.6` → `13.4.6`), so no version-transition API transforms are required.

Automated breaking-change patterns detected in root AppHost scope: **0**.

Advisory patterns:

- `BeforeResourceStartedEvent` appears in referenced submodule AppHost/test code (`references/Hexalith.Memories/...`). This is outside the root AppHost scope and is already on Aspire 13.4.6, so it is recorded as informational only.
- No root-scope `DefaultAzureCredential`, `WithPublishingCallback`, `IDistributedApplicationLifecycleHook`, `AddNpmApp`, `AddNodeApp`, `WithSecretBuildArg`, or `AddAzureAIFoundry` usage was found.

## Environment

- Aspire CLI: `13.4.3`; update recommended to align with target `13.4.6`.
- Docker: `29.4.3`; container runtime is available.
- Aspire agent/skills: repository contains Aspire skill files under `.github/skills/aspire` and `.claude/skills/aspire`.

## Recommended Work

1. Update Aspire CLI to `13.4.6`.
2. Consolidate `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` to `Sdk="Aspire.AppHost.Sdk/13.4.6"`.
3. Remove the redundant `Aspire.Hosting.AppHost` package reference from the AppHost project.
4. Remove the now-unused root `Aspire.Hosting.AppHost` central package version if no remaining root project references it.
5. Build the solution and fix any errors introduced by the AppHost format consolidation.
6. Validate the AppHost with Aspire CLI commands if the build succeeds.

## Assessment Checklist

- [x] Current Aspire version determined precisely
- [x] Target version confirmed
- [x] Version transitions identified
- [x] AppHost format identified
- [x] ServiceDefaults presence checked
- [x] Aspire packages inventoried
- [x] Package renames identified
- [x] TFM upgrade requirement determined
- [x] Breaking change patterns scanned and counted
- [x] Advisory items identified
- [x] Tooling status checked
