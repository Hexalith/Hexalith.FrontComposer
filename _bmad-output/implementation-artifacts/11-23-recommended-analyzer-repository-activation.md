---
created: 2026-07-16
updated: 2026-07-16
epic: 11
sourceDecision: _bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md
parentDecisionStory: 11.19d
dependsOn: 11.22
owner: Architect + Framework Maintainer + Release Owner
due: 2026-09-11
status: backlog
storyType: implementation-phase
approvalGate: separate-architecture-product-approval
releaseGate: v1.0
---

# Story 11.23: Recommended Analyzer Repository Activation

Status: backlog.

## Story

As an Architect, Framework Maintainer, and Release Owner,
I want the approved `AnalysisMode=Recommended` posture activated and governed repository-wide,
so that analyzer strictness becomes a durable v1.0 build invariant.

## Acceptance Criteria

1. Given Stories 11.20-11.22 are done and their current census contains zero actionable findings,
   when activation begins, then `AnalysisMode=Recommended` is declared in the approved central build
   location without adding analyzer packages, changing `TreatWarningsAsErrors=true`, or introducing a
   global/category CA suppression.

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

- [ ] Verify Stories 11.20-11.22 are done and regenerate a zero-actionable-finding census.
- [ ] Add the central `AnalysisMode=Recommended` setting with approved TFM boundaries.
- [ ] Reconcile the benchmark warning-policy exception with the zero-warning Release gate.
- [ ] Add durable analyzer-policy Governance tests.
- [ ] Run the full forced Release, test, compatibility, docs, and artifact gates.
- [ ] Record Release Owner evidence and update sprint/release traceability.

## Dev Notes

### Preconditions

Do not start implementation while any earlier phase is not done, any exception lacks the required
ledger fields, or a forced command-line Recommended build reports an actionable diagnostic.

### Rollback

An emergency rollback after activation is a separately approved build-policy change. It may revert the
central `AnalysisMode` declaration while remediation proceeds, but may not lower
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

### File List

## Change Log

- 2026-07-16: Materialized approved staged-activation Phase 4 from Story 11.19d.
