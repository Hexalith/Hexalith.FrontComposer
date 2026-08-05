---
title: 'Scope SourceTools mutation away from full solution builds'
type: 'bugfix'
created: '2026-08-05'
status: 'done'
review_loop_iteration: 0
baseline_commit: '2f0791d6203a3331c746ec9694c04c49f80602e5'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Nightly Mutation run `30978026706` job `source-tools-mutation` fails because Stryker’s initial build targets `Hexalith.FrontComposer.slnx` as Debug and hits MSB4018 file locks on submodule DLLs such as `references/Hexalith.Tenants/.../Hexalith.Tenants.UI.dll`. Missing JSON reports then cascade into false “target drift” for every Parsing/Transforms file.

**Approach:** Make SourceTools mutation project-scoped (no full-umbrella solution build), build as Release to match the nightly pre-build, and make report validation skip target-drift when required segment reports are absent so a Stryker build failure does not invent drift noise.

## Boundaries & Constraints

**Always:** Keep mutate roots limited to SourceTools `Parsing/**` and `Transforms/**`; preserve happy-path break ≥80 and error-handling break ≥60; keep both JSON segment reports plus `eng/validate-stryker-reports.ps1` as the gate; leave CI/quality/release solution usage of `Hexalith.FrontComposer.slnx` unchanged; keep `Validate mutation reports` present in `mutation-property-nightly.yml`.

**Ask First:** Changing mutation thresholds or mutate roots; adding a new mutation-only `.slnx` instead of project-scoped configs; disabling or skipping the mutation job; any dependency/version bump.

**Never:** Silence drift while still requiring successful mutation evidence; build or mutate Tenants/Parties/AppHost for this gate; recursive nested submodule updates; alter lifecycle-property or LLM-benchmark jobs in this change.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Happy path | Project-scoped Stryker configs + SourceTools test graph only | Both segments emit JSON reports under `artifacts/mutation/{happy-path,error-handling}`; validator passes thresholds and coverage | N/A |
| Stryker build failure | Segment produces no JSON report | Validator fails with missing-report error(s) only — no target-drift storm | Exit nonzero; upload remaining artifacts |
| Config shape | Segment config omits umbrella `solution` | Validator accepts project + test-projects + required reporters/thresholds/mutate | Fail only on truly missing required fields |

</frozen-after-approval>

## Code Map

- `.github/workflows/mutation-property-nightly.yml` -- `source-tools-mutation` job; restore/build Release SourceTools.Tests then runs both Stryker segments and `eng/validate-stryker-reports.ps1`.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-happy-path.json` -- currently `"solution": "Hexalith.FrontComposer.slnx"`; mutate Parsing/Transforms; break 80.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-error-handling.json` -- same solution pin; break 60; `Category=MutationErrorHandling`.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json` -- approved roots = SourceTools Parsing + Transforms only.
- `eng/validate-stryker-reports.ps1` -- requires `solution` today (line ~176); on missing JSON uses `continue` without clearing `$hasAllRequiredReports`, so drift still runs (line ~197–308).
- `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj` -- refs Contracts only (no Tenants).
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj` -- refs SourceTools, Contracts, Contracts.UI, Schema, Mcp, Shell (no Tenants/Parties).
- `docs/how-to/mutation-and-property-quality-gates.md` -- local Stryker commands; update only if config invocation shape changes.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- asserts mutation nightly still contains `Validate mutation reports`; does not pin Stryker `solution`.
- Failed evidence: run `30978026706` artifact `mutation-validation-errors.txt` (missing reports + full target-drift list).

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-happy-path.json` and `stryker-error-handling.json` -- remove umbrella `solution`; keep `project` + `test-projects`; set `"configuration": "Release"`; optionally lower `concurrency` to 2 if Stryker warns at 4 -- stop compiling Tenants/Parties during mutation.
- [x] `eng/validate-stryker-reports.ps1` -- stop requiring `solution`; when a required segment JSON report is missing in CI mode, set `$hasAllRequiredReports = $false` before continuing so target-drift is skipped; refresh stale tool version text if it still says 4.14.1 while `.config/dotnet-tools.json` pins a newer `dotnet-stryker`.
- [x] `docs/how-to/mutation-and-property-quality-gates.md` -- note project-scoped Release mutation (no full-slnx Stryker build) if the documented local flow would otherwise mislead.
- [x] `tests/eng` or focused governance coverage -- add/adjust a cheap assertion that mutation Stryker configs are project-scoped (no `Hexalith.FrontComposer.slnx` solution pin) and that missing-report validation does not emit target-drift; keep `CiGovernanceTests` mutation step name green.

**Acceptance Criteria:**
- Given the SourceTools mutation job on the fixed configs, when Stryker starts, then it builds the SourceTools project graph only (not `references/Hexalith.Tenants` / Parties UI projects) under Release.
- Given both segment JSON reports exist under `artifacts/mutation`, when `eng/validate-stryker-reports.ps1` runs, then it passes without missing-report or fabricated target-drift failures for approved Parsing/Transforms roots.
- Given a missing segment JSON report, when validation runs without `-AllowMissingReports`, then it fails on missing report(s) and does not list every target file as drift.
- Given `CiGovernanceTests` mutation boundary checks, when run, then `Validate mutation reports` remains required in `mutation-property-nightly.yml`.

## Spec Change Log

- 2026-08-05: Approved implementation completed. Removed the umbrella `Hexalith.FrontComposer.slnx` `solution` pin from both Stryker segment configs, pinned `"configuration": "Release"`, stopped requiring `solution` in `eng/validate-stryker-reports.ps1`, fixed the missing-report branch to clear `$hasAllRequiredReports` (so a Stryker build failure no longer fabricates target-drift), made the job-summary tool-version line read `.config/dotnet-tools.json` dynamically instead of a hardcoded stale string, documented the project-scoped Release mutation build in the how-to guide, and added two focused `CiGovernanceTests` cases covering the config shape and the missing-report/no-drift behavior. Left `concurrency: 4` unchanged (no observed Stryker warning to justify lowering it; the optional expensive Stryker smoke run was not executed).
- 2026-08-05: Adversarial review hardening. Closed two more drift-storm leaks by clearing `$hasAllRequiredReports` on a missing segment config file and on a malformed mutation JSON report, not just a missing report. Hardened `$StrykerToolVersion` resolution with try/catch and a null/blank coalesce so a malformed or incomplete `.config/dotnet-tools.json` always falls back to `"unknown"` instead of aborting the script. Added `configuration` to the required-fields list and a new failure when a config still sets `solution`, so both a dropped-Release regression and a reintroduced umbrella `.slnx` build are caught; discovered and fixed a latent `Set-StrictMode -Version Latest` crash this exposed (direct dot-access to a genuinely absent JSON property throws instead of returning `$null`) by adding a `Get-PSObjectPropertyValue` helper and routing every `stryker-config` field read through it. Extended `CiGovernanceTests` to assert `test-projects` on both configs, replaced the brittle `ShouldNotContain("\"solution\"")` script-text assertion (now a false positive against the new forbid-solution message) with a behavioral test that runs the script against a mutated config missing `configuration` and reintroducing `solution`, and renamed the missing-report test to `ValidateStrykerReportsScript_SkipsTargetDriftWhenAReportIsMissing` for clarity.

## Design Notes

Stryker.NET treats `solution` as optional for SDK-style projects; when present it builds that entire solution before mutating `project`. FrontComposer’s umbrella `.slnx` intentionally includes submodule UI projects for Debug navigation, which is the wrong build surface for a SourceTools-only mutation gate. Project-scoped configs align the build graph with the existing test `ProjectReference` closure (Contracts/UI/Schema/Mcp/Shell) and avoid the MSB4018 lock class seen on Tenants.UI.

## Verification

**Commands:**
- `pwsh ./eng/validate-stryker-reports.ps1 -AllowMissingReports` -- expected: config-shape validation passes without requiring umbrella `solution`.
- Focused governance/unit test for the new config/missing-report contracts -- expected: pass.
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~E2EGovernanceAndStoryTenFourBoundariesRemainExplicit" -c Release` -- expected: pass.
- Optional smoke (expensive): one Stryker segment with the updated happy-path config and confirm no Tenants.UI path appears in the build log -- expected: JSON report emitted under `artifacts/mutation/happy-path`.
- `git diff --check` -- expected: no whitespace errors.

**Results (2026-08-05):**
- `pwsh ./eng/validate-stryker-reports.ps1 -AllowMissingReports` -- passed; config-shape validation succeeded without an umbrella `solution`, and the job summary reported `dotnet-stryker 4.16.0` (read live from `.config/dotnet-tools.json` instead of the stale hardcoded `4.14.1`).
- Focused governance coverage `CiGovernanceTests.SourceToolsMutationConfigs_AreProjectScopedAndReleaseBuilt` and `CiGovernanceTests.ValidateStrykerReportsScript_DoesNotRequireSolutionAndSkipsDriftWhenAReportIsMissing` -- both pass; the latter drives the real script against a fixture report root with only the happy-path segment present and asserts the failure is scoped to "Missing JSON mutation report for segment 'error-handling'" with no "Target drift:" text.
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~E2EGovernanceAndStoryTenFourBoundariesRemainExplicit" -c Release` -- 1/1 passed.
- Full `CiGovernanceTests` class (Release) -- 60/60 passed (58 pre-existing + 2 new).
- Optional expensive Stryker smoke run was not executed (long-running, environment-specific per the how-to doc); config-shape and missing-report behavior were verified instead as the spec allows.
- `git diff --check` -- no whitespace errors on any changed file; CRLF line endings were preserved in `eng/validate-stryker-reports.ps1`, both Stryker JSON configs, and `CiGovernanceTests.cs`.

**Results (2026-08-05, adversarial review round):**
- `pwsh ./eng/validate-stryker-reports.ps1 -AllowMissingReports` -- passed against the real configs.
- Manual regression probes against temp copies (not committed): reintroducing `solution` + dropping `configuration` on the happy-path config both failed with the new dedicated messages; a missing segment config file, a missing JSON report, and a malformed JSON report each now fail with only their own message and never a `Target drift:` storm; a malformed `.config/dotnet-tools.json` and one missing the `dotnet-stryker` entry both fell back to `dotnet-stryker unknown` instead of aborting.
- `CiGovernanceTests.SourceToolsMutationConfigs_AreProjectScopedAndReleaseBuilt`, `CiGovernanceTests.ValidateStrykerReportsScript_RejectsReintroducedSolutionAndMissingConfiguration`, `CiGovernanceTests.ValidateStrykerReportsScript_SkipsTargetDriftWhenAReportIsMissing` -- all pass.
- Full `CiGovernanceTests` class (Release) -- 61/61 passed.
- `git diff --check` -- no whitespace errors; CRLF preserved in `eng/validate-stryker-reports.ps1` and `CiGovernanceTests.cs`; `.config/dotnet-tools.json` and the Stryker JSON configs used for manual regression probes were restored byte-for-byte (verified via `git diff --stat`).

## Suggested Review Order

**Project-scoped Stryker configs**

- Drop the umbrella `solution` pin and build the SourceTools project graph as `Release`.
  [`stryker-happy-path.json:4`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-happy-path.json#L4)
- Mirror the same project-scoped, Release-configured shape for the error-handling segment.
  [`stryker-error-handling.json:4`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-error-handling.json#L4)

**Validator fix**

- Require `configuration` and forbid a reintroduced `solution` in the config-shape check.
  [`validate-stryker-reports.ps1:202`](../../eng/validate-stryker-reports.ps1#L202)
- Defensive `stryker-config` property lookup that fails cleanly instead of crashing under `Set-StrictMode -Version Latest` when a field is genuinely absent.
  [`validate-stryker-reports.ps1:49`](../../eng/validate-stryker-reports.ps1#L49)
- Clear `$hasAllRequiredReports` on every drift-storm trigger — missing segment config, missing report, and malformed report JSON — not just `-AllowMissingReports`.
  [`validate-stryker-reports.ps1:194`](../../eng/validate-stryker-reports.ps1#L194), [`:232`](../../eng/validate-stryker-reports.ps1#L232), [`:253`](../../eng/validate-stryker-reports.ps1#L253)
- Read the Stryker tool version live from `.config/dotnet-tools.json` with a safe fallback to `"unknown"`.
  [`validate-stryker-reports.ps1:15`](../../eng/validate-stryker-reports.ps1#L15)

**Governance coverage**

- Assert both segment configs stay project-scoped, Release-built, and reference the SourceTools.Tests project.
  [`CiGovernanceTests.cs:1975`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L1975)
- Drive the real script against a mutated config to prove the missing-`configuration`/reintroduced-`solution` failures fire.
  [`CiGovernanceTests.cs:1998`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L1998)
- Drive the real script against a missing-report fixture and assert no target-drift storm.
  [`CiGovernanceTests.cs:2065`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L2065)

**Docs**

- Note the project-scoped Release mutation build so the local flow doesn't imply a full-`.slnx` Stryker build.
  [`mutation-and-property-quality-gates.md:33`](../../docs/how-to/mutation-and-property-quality-gates.md#L33)
