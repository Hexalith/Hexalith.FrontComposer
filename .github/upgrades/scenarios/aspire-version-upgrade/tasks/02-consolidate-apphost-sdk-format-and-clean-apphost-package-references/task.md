# 02-consolidate-apphost-sdk-format-and-clean-apphost-package-references: Consolidate AppHost SDK format and clean AppHost package references

- Update `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` from the legacy child SDK declaration to `Sdk="Aspire.AppHost.Sdk/13.4.6"` on the root `<Project>` element.
- Remove the redundant AppHost-level `Aspire.Hosting.AppHost` package reference because it is supplied by the AppHost SDK.
- Review `Directory.Packages.props` and remove the root `Aspire.Hosting.AppHost` central package version only if no remaining root project references it.
- Preserve the existing `Aspire.Hosting.Keycloak` preview pin and repo comment; do not bump it independently.
- Commit the task result after the project and package cleanup is complete.
