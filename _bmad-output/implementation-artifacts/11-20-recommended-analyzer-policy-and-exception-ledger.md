---
created: 2026-07-16
updated: 2026-07-16
epic: 11
sourceDecision: _bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md
parentDecisionStory: 11.19d
owner: Architect + Framework Maintainer
due: 2026-07-24
status: backlog
storyType: implementation-phase
approvalGate: separate-architecture-product-approval
---

# Story 11.20: Recommended Analyzer Policy and Exception Ledger

Status: backlog.

## Story

As an Architect and Framework Maintainer,
I want every current analyzer suppression and Naming diagnostic classified into a narrow exception or
an actionable fix,
so that `AnalysisMode=Recommended` can be adopted without breaking public compatibility or hiding
findings globally.

## Acceptance Criteria

1. Given the Story 11.19d census, when the policy audit runs, then all 2,958 Naming findings and every
   effective `NoWarn`, warning-as-error, and individual `.editorconfig` severity are represented in a
   versioned ledger with scope, rationale, owner, review date, and removal or revalidation trigger.

2. Given CA1707 conflicts with the repository's required underscore-separated test names and existing
   public `FcDiagnosticIds` constant names, when dispositions are recorded, then test naming and public
   compatibility are preserved through the narrowest supported per-path, per-symbol, or source-level
   mechanism; repository-wide or category-wide CA suppression is forbidden.

3. Given a Naming finding is not covered by an approved compatibility or test-convention exception,
   when the scoped candidate lane runs, then the finding is fixed or placed in a separately approved,
   owner-bound defect story; no open item is hidden by the ledger.

4. Given current compiler, SDK, package, FrontComposer, IDE, and CA suppressions have different owners,
   when the audit completes, then each is classified by source and the decision explicitly states
   whether it remains, narrows, moves, or becomes a fix. Story 11.19a documentation policy and package
   audit policy remain independent.

5. Given the no-third-party-analyzer policy, when Governance checks run, then they prove no analyzer
   package was added, no CA category was globally disabled, `TreatWarningsAsErrors=true` remains the
   canonical policy, and the ledger matches effective build configuration.

6. Given this phase changes policy boundaries rather than product behavior, when validation completes,
   then the normal forced Release build remains 0 warnings/0 errors, focused analyzer-policy tests pass,
   the default solution lane passes, and public API/schema/generated-output baselines change only when
   explicitly approved by this story.

## Tasks / Subtasks

- [ ] Reproduce the 4,070-diagnostic census and isolate the 2,958 Naming findings.
- [ ] Audit all effective warning controls and produce the exception ledger.
- [ ] Classify CA1707/CA1711 occurrences as compatibility exceptions or genuine fixes.
- [ ] Implement only approved narrow policy mechanisms and required genuine Naming fixes.
- [ ] Add analyzer-policy Governance coverage preventing global CA suppression and analyzer packages.
- [ ] Run forced current/candidate builds, focused tests, default tests, and artifact validation.

## Dev Notes

### Scope

This story owns analyzer-policy classification and narrow exception mechanics. It does not enable
repository-wide `AnalysisMode=Recommended` and does not perform performance, globalization,
maintainability, usage, reliability, or design burn-down except where a Naming fix cannot be separated.

CA1707 baseline by major surface: Shell.Tests 1,660; SourceTools.Tests 665; Mcp.Tests 281;
Contracts.Tests 109; Contracts net10.0 89; Cli.Tests 64; Testing.Tests 57; Bench 21;
Contracts.UI.Tests 10. The exact source of truth is the compressed census binary log.

### Prohibited shortcuts

- No `dotnet_analyzer_diagnostic.category-*.severity = none`.
- No repository-wide `dotnet_diagnostic.CA1707.severity = none`.
- No blanket CA IDs in root `NoWarn`.
- No third-party analyzer package.
- No public diagnostic-constant rename without explicit compatibility design and API evidence.

### Validation lanes

```bash
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore --no-incremental -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore --no-incremental -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 -p:AnalysisMode=Recommended
DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --no-restore \
  --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"
```

## References

- `_bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md`
- `_bmad-output/contracts/analyzer-elevation-recommended-census-2026-07-16.binlog.gz`
- `_bmad-output/implementation-artifacts/11-19-doc-comment-enforcement-realignment.md`

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

## Change Log

- 2026-07-16: Materialized approved staged-activation Phase 1 from Story 11.19d.
