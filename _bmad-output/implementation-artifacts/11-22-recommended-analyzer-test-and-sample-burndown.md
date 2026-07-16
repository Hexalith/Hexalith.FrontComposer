---
created: 2026-07-16
updated: 2026-07-16
epic: 11
sourceDecision: _bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md
parentDecisionStory: 11.19d
dependsOn: 11.21
owner: Test Architect + Framework Maintainer
due: 2026-09-04
status: backlog
storyType: implementation-phase
approvalGate: separate-architecture-product-approval
---

# Story 11.22: Recommended Analyzer Test and Sample Burn-down

Status: backlog.

## Story

As a Test Architect and Framework Maintainer,
I want test and sample analyzer debt burned down without weakening intentional fixture semantics,
so that the complete repository can approach `Recommended` activation with trustworthy verification.

## Acceptance Criteria

1. Given the original census has 3,500 test findings and 203 sample findings, when this phase starts,
   then the baseline is regenerated after Stories 11.20 and 11.21 and every remaining diagnostic is
   assigned to a test project, sample project, diagnostic ID, and approved disposition.

2. Given underscore-separated three-part test names are required by repository convention, when
   CA1707 is handled, then test names remain readable and convention-compliant through Story 11.20's
   narrow approved mechanism; no mass rename or global CA1707 suppression is introduced.

3. Given tests intentionally exercise invalid code, module initializers, render-tree sequence numbers,
   asynchronous behavior, and generated fixtures, when a suppression remains necessary, then it is
   constrained to the smallest fixture/path/symbol and carries rationale, owner, review date, and a
   removal or revalidation trigger in the exception ledger.

4. Given generated Shell test specimens produced 302 census findings, when the generator fix from
   Story 11.21 is consumed, then test projects validate the corrected generated output and do not edit
   `obj/` or duplicate an emitter fix locally.

5. Given samples are adopter guidance, when sample findings are fixed, then samples continue to teach
   supported public APIs, generated artifact behavior, Fluent/MCP security boundaries, and package
   consumption without hiding genuine warnings.

6. Given the phase is complete, when validation runs, then all test/sample projects have zero
   actionable Recommended findings, default/Governance/Contract lanes pass with no skipped tests,
   Verify/PublicAPI/Pact/generated-output baselines change only intentionally, and normal Release stays
   0 warnings/0 errors.

## Tasks / Subtasks

- [ ] Regenerate the test/sample census after Stories 11.20 and 11.21.
- [ ] Apply the approved Naming policy without mass-renaming tests.
- [ ] Burn down non-Naming test diagnostics by project and defect class.
- [ ] Burn down sample diagnostics and validate generated consumers.
- [ ] Audit and narrow intentional fixture suppressions with ledger updates.
- [ ] Run focused, default, Governance, Contract, snapshot, and artifact validation.

## Dev Notes

### Original baseline

Shell.Tests 2,022; SourceTools.Tests 892; Mcp.Tests 305; Contracts.Tests 116; Cli.Tests 66;
Testing.Tests 64; Shell.Tests.Bench 25; Contracts.UI.Tests 10. Samples: Counter.Specimens.Domain 94;
Counter.Domain 79; IdeParityCounter 28; Counter.Web 2; Counter.Specimens 0.

The original test baseline is dominated by CA1707 (2,867 test findings). Story 11.20 owns the policy
disposition. This story owns remaining test/sample fixes and narrow fixture evidence, not the policy
decision itself.

### Validation

Run each changed test project, the full solution default lane with `DiffEngine_Disabled=true`, explicit
Governance and Contract lanes, and the forced current/candidate Release builds. The benchmark project's
existing warning policy must be reconciled with the final zero-warning activation gate rather than
silently ignored.

## References

- `_bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md`
- `_bmad-output/implementation-artifacts/11-20-recommended-analyzer-policy-and-exception-ledger.md`
- `_bmad-output/implementation-artifacts/11-21-recommended-analyzer-product-and-generator-burndown.md`

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

## Change Log

- 2026-07-16: Materialized approved staged-activation Phase 3 from Story 11.19d.
