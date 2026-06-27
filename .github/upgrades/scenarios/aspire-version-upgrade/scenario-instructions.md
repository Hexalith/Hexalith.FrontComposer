# Aspire Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Aspire Version**: 13.4.6
- **Target Framework Requirement**: net10.0
- **Upgrade Scope**: Assess the existing Aspire solution, update any non-current Aspire references if found, consolidate project format if needed, and validate the solution.

## Source Control
- **Source Branch**: main
- **Working Branch**: aspire-version-upgrade
- **Commit Strategy**: After Each Task
- **Branch Sync**: Auto (Merge)

## Strategy and Execution Constraints

- **Selected Strategy**: Small root-focused Aspire 13.4.6 alignment; update tooling, consolidate AppHost SDK format, clean redundant AppHost package metadata, then build and validate.
- **No TFM Upgrade**: Do not create a target framework upgrade task because the primary AppHost already targets `net10.0`.
- **Package Cleanup Boundary**: Remove `Aspire.Hosting.AppHost` only where redundant; preserve the `Aspire.Hosting.Keycloak` preview pin and existing package-version intent.
- **Scope Constraint**: Keep changes in the root repository (`src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`, `Directory.Packages.props`, `aspire.config.json`, and generated upgrade artifacts). Treat referenced submodules as out of scope unless required to resolve root build/validation failures.
- **Validation Constraint**: Build before runtime validation. Use Aspire CLI inspection/validation only after CLI version alignment and build success. Confirm Docker availability before container-dependent AppHost validation.
- **Source Control Constraint**: Use working branch `aspire-version-upgrade`, sync from `main` by merge, and commit after each completed task.
