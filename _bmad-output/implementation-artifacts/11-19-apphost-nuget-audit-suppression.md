---
created: 2026-07-15
updated: 2026-07-15
epic: 11
childStory: 11.19b
parentStory: 11.19
owner: Developer + Security/Release Owner
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md
status: ready-for-dev
implementationGate: post-correction-readiness-pass
baseline_commit: c410e4d109ca266b65c5525afd3960af68e488e8
---

# Story 11.19b: AppHost NuGet Audit Suppression

Status: review.

## Story

As a release owner,
I want AppHost vulnerability-audit exceptions expressed per advisory,
so that new high/critical vulnerabilities cannot be hidden by a permanent warning-family suppression.

## Acceptance Criteria

1. Given `Hexalith.FrontComposer.AppHost.csproj` currently suppresses `NU1902;NU1903;NU1904` through
   `NoWarn`, when this story is complete, then that blanket family suppression is removed and no
   equivalent repository-, project-, or command-line suppression remains.

2. Given an active advisory is reported for the AppHost dependency graph, when Security/Release Owner
   reviews it, then each accepted exception is one `NuGetAuditSuppress` item whose `Include` is the
   exact advisory URL and whose adjacent rationale records affected package/version, applicability,
   owner, decision date, review/expiry date, and remediation link. If no advisory is accepted, the
   project contains no suppression item.

3. Given a new or changed advisory appears, when restore/audit runs, then it remains visible and fails
   according to repository warning policy unless that exact advisory receives its own reviewed item.
   Suppressing one advisory must not suppress another package sharing a warning code.

4. Given Governance runs, when it inspects AppHost and imported props/targets, then it rejects NU1901–
   NU1904 in `NoWarn`/`WarningsNotAsErrors`, rejects duplicate or non-advisory
   `NuGetAuditSuppress` entries, requires the rationale metadata, and proves the scan covers a non-empty
   AppHost project graph.

5. Given dependency remediation is available, when the affected package is upgraded or the advisory
   no longer applies, then the suppression and rationale are removed in the same change; the story does
   not authorize package upgrades beyond the minimum reviewed fix.

6. Given validation runs, when online audit is available, then Release restore/build and Governance
   pass with expected advisories only. If local network access is unavailable, captured CI audit output
   is the authority and the story cannot be marked done without that evidence.

## Tasks / Subtasks

- [x] Capture the effective AppHost dependency graph and online NuGet audit output.
- [x] Remove the blanket NU1902–NU1904 `NoWarn` entry.
- [x] Remediate dependencies where feasible; add only reviewed exact advisory suppressions when needed.
- [x] Add non-vacuous governance for warning-family and per-advisory policy.
- [x] Run online restore/audit, Release build, Governance, artifact, and file-integrity validation.

## Dev Notes

### Current State

`src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` currently contains:

```xml
<NoWarn>$(NoWarn);NU1902;NU1903;NU1904</NoWarn>
```

That suppresses every moderate/high/critical audit warning in the project, including future unrelated
advisories. The replacement is advisory-specific, not warning-code-specific.

### Files To Read Before Editing

- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`
- `Directory.Packages.props`, `src/Directory.Build.props`, and imported repository props/targets
- `obj/project.assets.json` only as generated evidence; never hand-edit it
- CI/Governance tests that pin NuGet audit and package policy

### Anti-Patterns

- Do not use `NuGetAudit=false`, `NoWarn`, `WarningsNotAsErrors`, a wildcard URL, or a family-wide
  conditional to obtain a green build.
- Do not invent advisory URLs from package names. Capture them from online NuGet audit output.
- Do not duplicate a suppression through imported files; NuGet can report NU1508 for duplicates.

### Technical Reference

NuGet supports one `NuGetAuditSuppress` item per advisory and describes suppression as a last resort:
https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages#excluding-advisories

### Validation Commands

```bash
dotnet restore src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj \
  -p:Configuration=Release
dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj \
  -c Release --no-restore -m:1 -p:MinVerVersionOverride=4.0.0
python3 eng/validate-story-artifacts.py --story \
  _bmad-output/implementation-artifacts/11-19-apphost-nuget-audit-suppression.md
```

## References

- `_bmad-output/planning-artifacts/epics.md` — 11.19b child scope.
- `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` — audit-policy finding.
- https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1508

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Implementation Plan

- Capture the effective AppHost audit configuration, online vulnerability result, and a non-empty dependency graph before changing policy.
- Add red Governance coverage for blanket warning-family suppressions and malformed/duplicate advisory exceptions, then remove the AppHost `NoWarn` entry.
- Keep the project suppression-free when the online audit reports no accepted advisory; do not change package versions without an identified minimum remediation.
- Run focused Governance, Release restore/build, broad regression, artifact, and changed-file integrity gates before review promotion.

### Debug Log References

- 2026-07-16: Effective Release properties were `NoWarn=;0419;1570;1572;1573;1574;1734;NU1902;NU1903;NU1904`, empty `WarningsNotAsErrors`, `NuGetAudit=true`, `NuGetAuditMode=all`, and `TreatWarningsAsErrors=true`.
- 2026-07-16: The required pre-change `aspire start` baseline reached online restore but could not start resources because package-only dependencies `Hexalith.EventStore` 3.67.1 and `Hexalith.Memories` 2.6.17 were not yet published; NuGet reported nearest versions 3.67.0 and 2.6.16.
- 2026-07-16: Release/source-override online restore with command-line `NoWarn` cleared passed. The generated AppHost assets graph contained 81 package nodes plus the EventStore Aspire project node; `dotnet list package --include-transitive --vulnerable` queried nuget.org and reported no vulnerable packages.
- 2026-07-16: Governance RED was reproduced: 1/4 failed because effective AppHost `NoWarn` contained `NU1902`; after the blanket entry was removed, the focused policy lane passed 4/4.
- 2026-07-16: The configured Release/source-override regression lane passed 4,136/4,136 after the policy change (Contracts 209, Contracts.UI 10, CLI 73, MCP 364, SourceTools 1,088, Shell 2,334, Testing 57, benchmark discovery 1).
- 2026-07-16: Exact online AppHost Release restore passed without audit warnings. The AppHost-only Release compile passed with `BuildProjectReferences=false` (0 warnings, 0 errors); the repository's broader combined-UI Release graph remains blocked before AppHost compilation by pre-existing unpublished UI module packages, while its source fallback reaches three pre-existing HFC0001 violations in the Parties submodule.
- 2026-07-16: The full Governance lane passed 319/319 (including the four new audit-policy cases).
- 2026-07-16: Final baseline regression passed 4,136/4,136 after a clean build, excluding only the unrelated concurrent `PackageValidation_MissingBaseline_FailsWithActionableRestoreDiagnostics` test. Including that new test concurrently contaminates shared package-validation assets with its intentional `9999.0.0-frontcomposer-missing-baseline` value and causes the independent SourceTools pack test to fail; both the test and its companion artifact remain outside this story.
- 2026-07-16: Story artifact validation passed with all Story 11.19b files accounted for and the two concurrent files classified as unrelated.

### Completion Notes List

- Captured a non-empty effective AppHost dependency graph and online audit baseline before implementation; no advisory currently requires an accepted exception.
- Removed the AppHost `NU1902;NU1903;NU1904` warning-family suppression; no package version or advisory suppression was added because the online audit is clean.
- Added non-vacuous Governance coverage for effective/imported warning policy, non-empty project graph, exact unique advisory URLs, complete rationale metadata, dates, remediation links, and synthetic fail-closed cases.
- Verified the online Release audit, focused AppHost Release compile, full configured regression lane, and Governance lane. The existing combined-UI Release graph blocker is documented separately and is outside this story's changed files.
- Confirmed file-list integrity against baseline commit `c410e4d109ca266b65c5525afd3960af68e488e8`; Story 11.19b is ready for review.

### File List

- `_bmad-output/implementation-artifacts/11-19-apphost-nuget-audit-suppression.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AppHostNuGetAuditPolicyTests.cs`

### Documented Unrelated Changes

- `_bmad-output/implementation-artifacts/spec-actions-29456680414-fix-cicd.md` — unrelated concurrent CI-fix artifact created after the Story 11.19b baseline.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs` — unrelated concurrent MCP package-boundary fix created after the Story 11.19b baseline.

## Change Log

- 2026-07-15: Materialized approved 11.19b child from the live AppHost blanket audit suppression.
- 2026-07-16: Removed the audit warning-family suppression, added fail-closed AppHost audit governance, captured online audit/build evidence, and promoted the story to review.
