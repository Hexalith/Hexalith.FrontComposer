---
created: 2026-07-15
updated: 2026-07-16
epic: 11
childStory: 11.19d
parentStory: 11.19
owner: Architect + Product Owner
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md
status: review
storyType: decision-record
implementationGate: post-correction-readiness-pass
baseline_commit: d9c19a4fb837357af10f6f1aa630232f670557c4
---

# Story 11.19d: Analyzer-Elevation Decision

Status: review.

## Story

As an Architect and Product Owner,
I want the repository's target `AnalysisMode=Recommended` posture and activation cost recorded,
so that analyzer strictness is an owned release decision rather than an accidental or indefinitely
deferred build change.

## Acceptance Criteria

1. Given the current .NET 10/Roslyn 5.6.0 repository and its effective analyzer configuration, when
   the decision work starts, then a Release diagnostic census records current `AnalysisMode`,
   `AnalysisLevel`, built-in analyzer severities, warning suppressions, project/category distribution,
   generated-code treatment, and the exact diagnostics newly produced by `Recommended`.

2. Given the census, when Architecture and Product decide the v1 posture, then a dated contract under
   `_bmad-output/contracts/` records `Recommended` as the target, whether activation is immediate or
   staged, the rationale, estimated burn-down, named owner(s), milestones/dates, release impact, and
   rollback/escalation rule. “Too many warnings” without an owned schedule is not a valid decision.

3. Given this is a decision story, when it is marked done, then it has not silently enabled
   `AnalysisMode`, changed severities, added analyzer packages, suppressed diagnostics, or performed
   broad code cleanup. Any selected implementation is materialized as one or more separately approved
   stories with package/defect-class scope and validation lanes.

4. Given staged activation is selected, when the contract is reviewed, then each phase names its
   projects/categories, diagnostic baseline, warning-reduction exit criterion, owner, due date, and
   release gate. Existing canonical NFR-1 warnings-as-errors remains unchanged.

5. Given immediate activation is selected, when follow-up stories are created, then they preserve the
   repository rule of built-in .NET/Roslyn analyzers only, use no global disable, and distinguish
   genuine fixes from narrow documented false-positive suppressions.

6. Given the decision artifact and census are complete, when docs/artifact validation and focused
   governance checks run, then they pass and sprint status links the decision plus any materialized
   follow-up. This decision story introduces no product-code, package, public API, UX, release workflow,
   generated output, or submodule change.

## Tasks / Subtasks

- [x] Capture the effective current analyzer configuration and a clean Release baseline.
- [x] Run a non-mutating `AnalysisMode=Recommended` diagnostic census by project/category/ID.
- [x] Estimate and group burn-down work without editing production code.
- [x] Record the signed Architecture/Product decision under `_bmad-output/contracts/`.
- [x] Materialize any approved implementation phases as new stories; do not implement them here.
- [x] Link sprint status and run documentation/artifact validation.

## Dev Notes

### Scope Boundary

No `AnalysisMode` property is currently declared in the repository. This story decides policy; it does
not make the build green under a new mode. Story 11.18 logging migrations and Story 11.19a/11.19b
enforcement fixes remain independent and must not be pulled into the decision census as cleanup work.

### Census Requirements

- Use the repository SDK `10.0.302`, Roslyn 5.6.0, Release configuration, and the normal centralized
  dependency graph.
- Compare the normal build with a command-line-only `-p:AnalysisMode=Recommended` run and preserve raw
  machine-readable diagnostics as bounded repository-relative evidence.
- Separate compiler, SDK built-in analyzer, source-generator, package audit, and project Governance
  diagnostics. Do not count generated code unless the effective policy analyzes it.
- Record suppressed IDs and why they would or would not remain permitted under the target posture.

### Decision Artifact Minimum Fields

Decision, status, date, approvers, target mode, activation strategy, diagnostic counts by project/ID,
owned phases, due dates, validation gates, release impact, rollback/escalation, and links to follow-up
stories.

### Anti-Patterns

- Do not add Sonar, StyleCop, Roslynator, or another analyzer package.
- Do not edit `Directory.Build.props`, `.editorconfig`, source, tests, or suppressions to improve the
  census result.
- Do not combine hundreds of unrelated findings into one future “fix all analyzers” story.

### Technical Reference

Official .NET code-analysis configuration defines `AnalysisMode` as the switch controlling which
built-in rule set is enabled: https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options#analysis-mode

### Validation Commands

```bash
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 -p:AnalysisMode=Recommended
python3 eng/validate-story-artifacts.py --story \
  _bmad-output/implementation-artifacts/11-19-analyzer-elevation-decision.md
```

## References

- `_bmad-output/planning-artifacts/epics.md` — 11.19d decision-child scope.
- `_bmad-output/planning-artifacts/prd.md` — canonical NFR-1 build strictness.
- `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` — policy-alignment finding.
- https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options

## Dev Agent Record

### Agent Model Used

### Debug Log References

- 2026-07-16: Captured SDK 10.0.302 / MSBuild 18.6.4 and a clean normal Release solution build (0 warnings, 0 errors) with the story-prescribed non-audit build command.
- 2026-07-16: Effective root posture is undeclared `AnalysisMode`, `AnalysisLevel=latest`, `EnableNETAnalyzers=true`, `TreatWarningsAsErrors=true`, and explicit CA1062/CA1822/CA2007 warnings plus CA1014 suppression. Source projects also inherit documentation-warning suppressions 0419/1570/1572/1573/1574/1734; project-local suppressions are included in the decision census.
- 2026-07-16: Default regression lane passed 4,149/4,150 on its first serialized run; the sole failure was a two-minute cancellation inside the MCP package-boundary test's nested `dotnet pack`. Its exact direct xUnit rerun passed 1/1 in 9.0 seconds, confirming no functional regression.
- 2026-07-16: A forced candidate build with unchanged warnings-as-errors failed at 120 diagnostics before dependency failure stopped full enumeration. A diagnostic-only candidate build with command-line `TreatWarningsAsErrors=false` enumerated 4,070 unique Recommended findings across 19 affected Release projects/TFMs: Naming 2,958; Performance 772; Globalization 228; Usage 49; Maintainability 46; Reliability 12; Design 5. Tests account for 3,500 findings, samples 203, and product source 367. The effective candidate policy reports 503 diagnostics in SourceTools-generated files, so they remain included in the census.
- 2026-07-16: Restored the normal analyzer posture, rebuilt Release clean (0 warnings, 0 errors), and reran the complete default regression lane successfully (4,150/4,150).
- 2026-07-16: Burn-down grouping: Naming 2,958 (principally CA1707 conflicts with required underscore test names and existing public diagnostic-constant names); logging CA1848/CA1873 566 (405 generated); remaining Performance 206; Globalization 228; Design/Maintainability/Usage/Reliability 112. Estimated four staged phases: policy/exception audit (3-5 engineer-days), production and generator fixes (8-12), test/sample fixes (8-12), final activation/governance (2-3); total 21-32 engineer-days before contingency.
- 2026-07-16: Recorded the signed FC-ANALYZERS-RECOMMENDED contract selecting staged activation, naming Architecture/Product/implementation/release owners, setting 2026-07-24 through 2026-09-11 milestones, and defining zero-warning exits plus rollback/escalation rules. Story and repository artifact validation both pass.
- 2026-07-16: Materialized Stories 11.20-11.23 as separate approval-gated backlog phases with non-overlapping policy, product/generator, test/sample, and final activation scopes. No phase implementation was performed; artifact validation remains green.
- 2026-07-16: Sprint status links the decision and Stories 11.20-11.23. Story/repository artifact validation, published-docs validation, and the focused Governance lane pass (322 Governance tests; projects without matching traits reported no matching test, not failure).
- 2026-07-16: Final completion gate passed: all tasks checked, full default regression 4,150/4,150, forced normal Release 0 warnings/0 errors, 322 focused Governance tests, docs validation, artifact validation, gzip evidence integrity, File List reconciliation, and whitespace checks.

### Completion Notes List

- Effective current analyzer configuration and the clean Release baseline are captured without changing build policy, severities, suppressions, packages, source, tests, or generated output.
- The non-mutating Recommended census is captured by category, diagnostic ID, and project; compressed current/strict/census MSBuild binary logs preserve raw machine-readable evidence.
- Burn-down is partitioned by defect class and validation boundary; no source, test, suppression, package, severity, or generated-output edit was made during estimation.
- Architecture/Product approved `Recommended` as the target through four staged, release-gated phases; immediate activation was rejected on the measured 4,070-diagnostic cost.
- Follow-up implementation is materialized as four independently approval-gated backlog stories; Story 11.19d introduces no analyzer-policy or product-code change.
- Sprint traceability, documentation validation, artifact validation, focused Governance coverage, the clean Release build, and the 4,150-test default regression lane are green.
- Definition of Done is satisfied and the story is ready for independent code review; follow-up phases remain backlog and unimplemented.

### File List

- `_bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md`
- `_bmad-output/contracts/analyzer-elevation-current-2026-07-16.binlog.gz`
- `_bmad-output/contracts/analyzer-elevation-recommended-census-2026-07-16.binlog.gz`
- `_bmad-output/contracts/analyzer-elevation-recommended-strict-2026-07-16.binlog.gz`
- `_bmad-output/implementation-artifacts/11-20-recommended-analyzer-policy-and-exception-ledger.md`
- `_bmad-output/implementation-artifacts/11-21-recommended-analyzer-product-and-generator-burndown.md`
- `_bmad-output/implementation-artifacts/11-22-recommended-analyzer-test-and-sample-burndown.md`
- `_bmad-output/implementation-artifacts/11-23-recommended-analyzer-repository-activation.md`
- `_bmad-output/implementation-artifacts/11-19-analyzer-elevation-decision.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-07-15: Materialized approved 11.19d decision child with a non-mutating Recommended-mode census contract.
- 2026-07-16: Completed the analyzer census and approved staged-activation decision; added raw binary-log evidence and four separately gated implementation stories without changing analyzer policy or product code.
