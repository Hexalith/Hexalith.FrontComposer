---
created: 2026-07-15
updated: 2026-07-15
epic: 11
childStory: 11.19d
parentStory: 11.19
owner: Architect + Product Owner
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md
status: ready-for-dev
storyType: decision-record
implementationGate: post-correction-readiness-pass
---

# Story 11.19d: Analyzer-Elevation Decision

Status: ready-for-dev.

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

- [ ] Capture the effective current analyzer configuration and a clean Release baseline.
- [ ] Run a non-mutating `AnalysisMode=Recommended` diagnostic census by project/category/ID.
- [ ] Estimate and group burn-down work without editing production code.
- [ ] Record the signed Architecture/Product decision under `_bmad-output/contracts/`.
- [ ] Materialize any approved implementation phases as new stories; do not implement them here.
- [ ] Link sprint status and run documentation/artifact validation.

## Dev Notes

### Scope Boundary

No `AnalysisMode` property is currently declared in the repository. This story decides policy; it does
not make the build green under a new mode. Story 11.18 logging migrations and Story 11.19a/11.19b
enforcement fixes remain independent and must not be pulled into the decision census as cleanup work.

### Census Requirements

- Use the repository SDK `10.0.301`, Roslyn 5.6.0, Release configuration, and the normal centralized
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

### Completion Notes List

### File List

## Change Log

- 2026-07-15: Materialized approved 11.19d decision child with a non-mutating Recommended-mode census contract.
