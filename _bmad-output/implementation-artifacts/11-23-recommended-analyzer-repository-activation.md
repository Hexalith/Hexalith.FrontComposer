---
created: 2026-07-16
updated: 2026-08-08
epic: 11
sourceDecision: _bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md
parentDecisionStory: 11.19d
dependsOn: 11.22
owner: Architect + Framework Maintainer + Release Owner
due: 2026-09-11
status: done
storyType: implementation-phase
approvalGate: separate-architecture-product-approval
approvalStatus: approved
approvedBy: Administrator
approvedOn: 2026-08-08
releaseGate: v1.0
---

# Story 11.23: Recommended Analyzer Repository Activation

Status: done.

Executable intent and completion evidence live in
`_bmad-output/implementation-artifacts/spec-11-23-recommended-analyzer-repository-activation.md`.

## Story

As an Architect, Framework Maintainer, and Release Owner,
I want the approved `AnalysisMode=Recommended` posture activated and governed repository-wide,
so that analyzer strictness becomes a durable v1.0 build invariant.

## Acceptance Criteria

1. Given Stories 11.20-11.22 are done and their current census contains zero actionable findings,
   when activation begins, then `AnalysisMode=Recommended` is declared in root
   `Directory.Build.props` without adding analyzer packages, changing `TreatWarningsAsErrors=true`,
   or introducing a global/category CA suppression.

2. Given netstandard2.0 compiler-host compatibility is explicit, when the property is evaluated across
   Contracts, Schema, and SourceTools, then their intended analyzer/TFM boundaries are preserved and
   documented; no net10-only analyzer dependency enters SourceTools or a netstandard2.0 target.

3. Given the benchmark project currently overrides warnings-as-errors, when the repository gate is
   finalized, then that exception is explicitly reconciled so the forced Release solution build emits
   zero warnings and zero errors rather than relying only on process exit success.

4. Given analyzer policy can regress through configuration drift, when Governance tests run, then they
   prove the central `Recommended` setting, built-in-analyzers-only rule, unchanged warnings-as-errors,
   absence of broad CA suppression, ledger/config parity, and forced candidate/current build parity.

5. Given activation can affect emitted and public surfaces, when validation runs, then default,
   Governance, Contract, package/PublicAPI, schema, generated-output, Verify, Pact, docs, and artifact
   lanes required by the changed surfaces pass with no unapproved baseline drift.

6. Given this is a v1.0 release gate, when Story 11.23 reaches review, then sprint/release status links
   the passing evidence, the Release Owner confirms the gate, and rollback requires a separately
   approved policy change that does not lower warnings-as-errors or hide diagnostics globally.

## Tasks / Subtasks

- [x] Verify Stories 11.20-11.22 are done and regenerate a zero-actionable-finding census.
- [x] Add `AnalysisMode=Recommended` to root `Directory.Build.props` with approved TFM boundaries.
- [x] Reconcile the benchmark warning-policy exception with the zero-warning Release gate.
- [x] Add durable analyzer-policy Governance tests.
- [x] Run the full forced Release, test, compatibility, docs, and artifact gates.
- [x] Record Release Owner evidence and update sprint/release traceability.

## Dev Notes

### Preconditions

Do not start implementation while any earlier phase is not done, any exception lacks the required
ledger fields, or a forced command-line Recommended build reports an actionable diagnostic.

### Rollback

An emergency rollback after activation is a separately approved build-policy change. It may revert the
root `Directory.Build.props` `AnalysisMode` declaration while remediation proceeds, but may not lower
`TreatWarningsAsErrors`, add a third-party analyzer, or add a blanket `NoWarn`/category suppression.

### Required validation

The final forced Release build must report exactly 0 warnings and 0 errors. Process exit code alone is
insufficient. Run the complete default lane with `DiffEngine_Disabled=true`, explicit Governance and
Contract lanes, package/public API/schema/generated-output compatibility, docs validation, and story
artifact validation.

## References

- `_bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md`
- `_bmad-output/implementation-artifacts/11-20-recommended-analyzer-policy-and-exception-ledger.md`
- `_bmad-output/implementation-artifacts/11-21-recommended-analyzer-product-and-generator-burndown.md`
- `_bmad-output/implementation-artifacts/11-22-recommended-analyzer-test-and-sample-burndown.md`

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

- 2026-08-08: Forced Recommended census at the approved implementation baseline completed with
  0 warnings / 0 errors before root activation. The activated Release build also completed with
  0 warnings / 0 errors.
- Root `Directory.Build.props` now declares `AnalysisMode=Recommended` beside unchanged
  `TreatWarningsAsErrors=true`. Contracts and Schema dual-TFM legs and the netstandard2.0 SourceTools
  host retain their existing boundaries.
- The Bench `TreatWarningsAsErrors=false` exception was removed after its census and activated Release
  evidence were both clean. The ledger records it as a fixed, no-remaining-control disposition.
- Governance now requires central Recommended, forbids any false TWAE declaration, proves representative
  effective properties (including Schema/SourceTools), and compares activated vs forced Recommended
  Release warning/error summaries.
- `docs/how-to/test-generated-components.md` renamed the CA1707-violating snippet method and added a
  namespace so the identifier inventory seal remains coherent under Recommended naming analysis.
- Release Owner evidence and AnalysisMode-only rollback posture are recorded in the canonical ledger.
- Architecture/Product approval is recorded via the frozen bmad-build specification approved 2026-08-08.

### File List

- `Directory.Build.props`
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs`
- `docs/how-to/test-generated-components.md`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md`
- `_bmad-output/implementation-artifacts/spec-11-23-recommended-analyzer-repository-activation.md`
- `_bmad-output/implementation-artifacts/11-23-recommended-analyzer-repository-activation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-08-08: Code-review patches — discrete Story 11.23 activation evidence errors, exact census/Release
  command seals, Schema/SourceTools effective-property probes, activated-vs-forced summary parity,
  AnalysisMode rollback comment, Bench fixed-disposition pointer, elevation-decision current posture
  refresh, and docs CA1707 snippet inventory note.
- 2026-08-08: Activated central Recommended analysis, removed the Bench warning-as-error exception,
  and recorded Release Owner evidence and the activated governance proof.
- 2026-08-07: Named root `Directory.Build.props` as the Phase 4 `AnalysisMode=Recommended` activation location (11.19d code-review patch).
- 2026-07-16: Materialized approved staged-activation Phase 4 from Story 11.19d.
