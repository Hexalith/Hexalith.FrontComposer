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
---

# Story 11.19b: AppHost NuGet Audit Suppression

Status: ready-for-dev.

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

- [ ] Capture the effective AppHost dependency graph and online NuGet audit output.
- [ ] Remove the blanket NU1902–NU1904 `NoWarn` entry.
- [ ] Remediate dependencies where feasible; add only reviewed exact advisory suppressions when needed.
- [ ] Add non-vacuous governance for warning-family and per-advisory policy.
- [ ] Run online restore/audit, Release build, Governance, artifact, and file-integrity validation.

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

### Debug Log References

### Completion Notes List

### File List

## Change Log

- 2026-07-15: Materialized approved 11.19b child from the live AppHost blanket audit suppression.
