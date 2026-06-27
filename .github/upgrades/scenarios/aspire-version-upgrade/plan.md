# Aspire Version Upgrade Plan

## Overview

Upgrade the root Aspire setup to target Aspire 13.4.6 with a small, root-repository-focused scope. The AppHost SDK package version and AppHost target framework are already compliant (`Aspire.AppHost.Sdk` 13.4.6 and `net10.0`), so no TFM upgrade task is required. The plan focuses on aligning the Aspire CLI, consolidating the primary AppHost project to the Aspire 13+ SDK format, cleaning redundant package references, building the solution, and running a final validation gate.

Execution uses Automatic flow mode on working branch `aspire-version-upgrade` from source branch `main`, with branch sync by auto-merge and commits after each task. Referenced submodule projects and their Aspire configuration files are out of scope unless required to resolve root build or validation errors.

## Tasks

### 01-update-aspire-cli: Update Aspire CLI to 13.4.6

Verify the currently installed Aspire CLI version, update it from 13.4.3 to 13.4.6, and confirm the update before continuing. This is a tooling-only task; no source or project files should change.

**Done when**: `aspire --version` reports 13.4.6 and the CLI update result is recorded in the task progress details.

---

### 02-consolidate-apphost-sdk: Consolidate AppHost SDK format and clean package references

Update `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` from the legacy child SDK declaration to the Aspire 13+ root SDK declaration `Sdk="Aspire.AppHost.Sdk/13.4.6"`. Remove the redundant AppHost-level `Aspire.Hosting.AppHost` package reference because it is supplied by the AppHost SDK.

Review `Directory.Packages.props` and remove the root `Aspire.Hosting.AppHost` central package version only if no remaining root project references it. Preserve the existing `Aspire.Hosting.Keycloak` preview pin and repo comment; do not bump it independently.

**Done when**: the AppHost project uses `Sdk="Aspire.AppHost.Sdk/13.4.6"`, the redundant AppHost package reference is removed, package cleanup is confirmed, and affected project restore/build validation succeeds.

---

### 03-build-solution: Build the solution and fix introduced errors

Build `Hexalith.FrontComposer.slnx` after the AppHost SDK consolidation. Fix only errors introduced by the Aspire CLI/AppHost format/package cleanup changes.

Keep the scope root-repository focused; do not modify referenced submodule projects unless the build proves a root integration fix is impossible without doing so.

**Done when**: the solution build succeeds or any accepted limitation is documented with evidence that it was not introduced by the Aspire modernization work.

---

### 04-aspire-validation: Run the Aspire validation gate

Validate that root `aspire.config.json` still points to `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`. Run appropriate Aspire CLI validation or inspection commands against the root AppHost, and confirm Docker remains available before any runtime validation that requires containers.

**Done when**: Aspire CLI validation results are recorded, the AppHost configuration path is confirmed, and any runtime validation outcome or limitation is documented.
