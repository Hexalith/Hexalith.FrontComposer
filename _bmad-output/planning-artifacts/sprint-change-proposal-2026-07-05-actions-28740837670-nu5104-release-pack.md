---
title: Sprint Change Proposal - Actions 28740837670 NU5104 Release Pack
date: 2026-07-05
status: implemented
approval: approved-by-administrator-2026-07-05
scope: minor
trigger:
  - https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/28740837670
---

# Sprint Change Proposal - Actions 28740837670 NU5104 Release Pack

## 1. Issue Summary

GitHub Actions run `28740837670` failed in the Release workflow during `Run semantic-release (live publish)`.
The release tests completed, but semantic-release attempted to pack version `1.0.0` and `dotnet pack`
failed on `Hexalith.FrontComposer.Contracts` with `NU5104`:

`A stable release of a package should not have a prerelease dependency`

The dependency named by NuGet was `Microsoft.FluentUI.AspNetCore.Components [5.0.0-rc.4-26180.1, )`.
The repository intentionally pins Fluent UI Blazor v5 RC because no stable v5 package is available yet,
and Story 11.11 is the approved package-boundary work that moves the net10 Fluent rendering surface out
of the `Contracts` kernel into `Contracts.UI`.

The run also reported a non-blocking Actions warning because `actions/attest-build-provenance@v2`
delegated to Node 20-based attestation actions. GitHub forced those actions onto Node 24, but the warning
would remain until the workflow uses a newer attestation action.

## 2. Impact Analysis

Epic impact: no epic scope or sequencing change is required. This is a release-packaging unblock for the
existing FR-24 release path, while the approved Epic 11 Contracts.UI split remains the long-term fix.

Story impact: no story inventory update is required. The change directly addresses the previously known
DW-0341 release-pack failure class without changing product behavior.

Artifact impact:

- `src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj`
- `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj`
- `src/Hexalith.FrontComposer.Schema/Hexalith.FrontComposer.Schema.csproj`
- `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`
- `.github/workflows/release.yml`

Technical impact: stable FrontComposer release packages can now pack while they intentionally depend on
the pinned prerelease Fluent UI v5 surface. `Testing` already used this package-level pattern for its
intentional prerelease dependencies. The new suppressions are scoped to the affected packable projects
and include comments tying them to Story 11.11 or upstream GA.

Release workflow impact: the attestation step now uses `actions/attest-build-provenance@v4`, which
preserves the existing `subject-path` input and delegates to the newer `actions/attest` v4 stack.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The CI/CD failure is isolated to NuGet package authoring warning `NU5104` being promoted to an error.
- The repository policy already requires Fluent UI v5 RC; downgrading to stable Fluent v4 would violate
  architecture and package-boundary tests.
- Converting the release branch to prerelease semantics would be a broader release-governance decision.
- Scoped project-level suppressions unblock the current release path without hiding the planned
  Contracts.UI boundary correction.
- The attestation warning can be corrected by bumping the action major while preserving the workflow
  input contract.

Effort: Low.
Risk: Low to medium. The package still exposes prerelease dependencies until Story 11.11 or Fluent UI v5 GA.
Timeline impact: immediate release-pack unblock.

## 4. Detailed Change Proposals

### Contracts Package

File: `src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj`

OLD:

```xml
<PackageId>Hexalith.FrontComposer.Contracts</PackageId>
<IsTrimmable
  Condition="$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'net8.0'))">true</IsTrimmable>
```

NEW:

```xml
<PackageId>Hexalith.FrontComposer.Contracts</PackageId>
<IsTrimmable
  Condition="$([MSBuild]::IsTargetFrameworkCompatible('$(TargetFramework)', 'net8.0'))">true</IsTrimmable>
<!-- NU5104: the net10.0 target still carries the pre-split Fluent UI v5 typography surface.
     Story 11.11 moves it to Contracts.UI; until then the netstandard2.0 kernel stays clean
     while release packing permits the documented net10.0 prerelease dependency. -->
<NoWarn>$(NoWarn);NU5104</NoWarn>
```

Rationale: the failing project is temporarily multi-targeted with a net10 Fluent rendering surface, while
the netstandard2.0 kernel remains clean.

### Transitive Contracts Consumers

Files:

- `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj`
- `src/Hexalith.FrontComposer.Schema/Hexalith.FrontComposer.Schema.csproj`

Change: add a scoped `NU5104` suppression with an explicit comment that the prerelease dependency flows
from the current `Contracts` net10 target until Story 11.11 completes `Contracts.UI`.

Rationale: after `Contracts` packed successfully, the same prerelease dependency surfaced while packing
`Mcp`; `Schema` has the same project-reference shape and is covered proactively.

### Shell Package

File: `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`

Change: add a scoped `NU5104` suppression with an explicit comment for the pinned Fluent UI v5 RC and
System.Reactive RC dependencies.

Rationale: Shell directly ships the Fluent v5 surface and `System.Reactive` RC dependency; the release
package should pack under the documented current dependency posture.

### Release Workflow Attestation Action

File: `.github/workflows/release.yml`

OLD:

```yaml
uses: actions/attest-build-provenance@v2
```

NEW:

```yaml
uses: actions/attest-build-provenance@v4
```

Rationale: the failed run reported a Node 20 deprecation warning from the v2 attestation stack. The v4
action keeps the workflow's `subject-path` input and uses the newer attestation action version.

## 5. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent.

Success criteria:

- The release package inventory packs at semantic-release's attempted version `1.0.0`.
- The produced output contains every expected `.nupkg` and `.snupkg`.
- No release workflow, semantic-release, or dependency-version downgrade is introduced.
- The attestation warning no longer uses the Node 20-based v2 action chain.
- Story 11.11 remains the long-term package-boundary correction.

Verification completed:

```sh
python3 eng/pack_release_packages.py --version 1.0.0 --output /tmp/fc-release-pack-after
```

Result: success. The script produced `.nupkg` and `.snupkg` artifacts for `Cli`, `Contracts`, `Mcp`,
`Schema`, `Shell`, `SourceTools`, and `Testing`.

## 6. Checklist Status

- [x] 1.1 Triggering issue identified: Release workflow run `28740837670`.
- [x] 1.2 Core problem defined: stable package pack blocked by intentional prerelease Fluent UI v5 dependency.
- [x] 1.3 Evidence gathered: GitHub Actions annotations and local reproduction both show `NU5104`.
- [x] 2.1 Current epic remains viable: FR-24 release path remains valid.
- [N/A] 2.2-2.5 Epic scope/order changes: no backlog restructure required.
- [x] 3.1 PRD conflicts checked: FR-24 is unblocked; no MVP scope change.
- [x] 3.2 Architecture conflicts checked: Fluent v5 policy and Contracts.UI split remain intact.
- [N/A] 3.3 UI/UX conflicts: no UI behavior changes.
- [x] 3.4 Secondary artifacts checked: package project files and release attestation action updated.
- [x] 4.1 Direct Adjustment selected.
- [N/A] 4.2 Rollback not useful.
- [N/A] 4.3 MVP review not required.
- [x] 5.1-5.5 Proposal and handoff documented.
- [x] 6.1-6.2 Proposal verified for consistency.
- [N/A] 6.3 Explicit approval: user requested direct fix.
- [N/A] 6.4 Sprint status update: no epic or story entries changed.
- [x] 6.5 Next steps and handoff plan defined.
