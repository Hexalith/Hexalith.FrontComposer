# 02-consolidate-apphost-sdk: Consolidate AppHost SDK format and clean package references

Update `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` from the legacy child SDK declaration to the Aspire 13+ root SDK declaration `Sdk="Aspire.AppHost.Sdk/13.4.6"`. Remove the redundant AppHost-level `Aspire.Hosting.AppHost` package reference because it is supplied by the AppHost SDK.

Review `Directory.Packages.props` and remove the root `Aspire.Hosting.AppHost` central package version only if no remaining root project references it. Preserve the existing `Aspire.Hosting.Keycloak` preview pin and repo comment; do not bump it independently.

**Done when**: the AppHost project uses `Sdk="Aspire.AppHost.Sdk/13.4.6"`, the redundant AppHost package reference is removed, package cleanup is confirmed, and affected project restore/build validation succeeds.

## Research findings

- Scenario targets Aspire `13.4.6` and keeps the root repository as the change boundary.
- AppHost currently uses the legacy child SDK form (`Microsoft.NET.Sdk` plus nested `Aspire.AppHost.Sdk` `13.4.6`) and directly references `Aspire.Hosting.AppHost`.
- The repository uses NuGet Central Package Management via `Directory.Packages.props`.
- Root-source search found no remaining root project references to `Aspire.Hosting.AppHost` outside the AppHost project, so the central `Aspire.Hosting.AppHost` version is orphaned after removing the AppHost package reference.
- `Aspire.Hosting.Keycloak` remains pinned to `13.4.6-preview.1.26319.6` with the existing comment preserved.
