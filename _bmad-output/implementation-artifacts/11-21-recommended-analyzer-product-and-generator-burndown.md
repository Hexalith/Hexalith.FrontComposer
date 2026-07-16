---
created: 2026-07-16
updated: 2026-07-16
epic: 11
sourceDecision: _bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md
parentDecisionStory: 11.19d
dependsOn: 11.20
owner: Framework Maintainer + SourceTools Maintainer
due: 2026-08-14
status: backlog
storyType: implementation-phase
approvalGate: separate-architecture-product-approval
---

# Story 11.21: Recommended Analyzer Product and Generator Burn-down

Status: backlog.

## Story

As a Framework and SourceTools Maintainer,
I want product-source and generator-emission findings fixed by defect class,
so that every shipped package and generated consumer can build cleanly under the approved
`Recommended` policy.

## Acceptance Criteria

1. Given Story 11.20's approved exception ledger, when product projects are built with command-line
   `AnalysisMode=Recommended` and unchanged warnings-as-errors, then all 367 census findings in shipped
   FrontComposer source are fixed or covered by a pre-approved narrow compatibility exception.

2. Given the census reports 503 diagnostics in SourceTools output, when generator findings are
   remediated, then fixes are made in emitters or annotated source, never under `obj/`, and generated
   sample/test consumers prove the emitted code no longer introduces actionable Recommended findings.

3. Given CA1848 and CA1873 account for 566 findings, including 405 generated findings, when logging
   work is performed, then it follows the source-generated `LoggerMessage` convention and does not
   reopen completed Story 11.18 scopes except through explicit, non-overlapping defect ownership.

4. Given remaining product findings span Performance, Globalization, Usage, Maintainability,
   Reliability, and Design categories, when changes are grouped, then each change maps to a named
   diagnostic/package scope and preserves public API, schema fingerprints, wire formats, command
   lifecycle, MCP fail-closed behavior, and generated artifact inventory.

5. Given netstandard2.0 compiler-host compatibility is load-bearing, when Contracts, Schema, and
   SourceTools are validated, then their existing TFM/analyzer boundaries remain explicit and the
   Contracts/SourceTools netstandard2.0 gate passes.

6. Given product/generator burn-down is complete, when validation runs, then owned product projects
   and generated sample consumers build under Recommended with zero actionable warnings, normal Release
   remains 0 warnings/0 errors, focused package/generator tests and default/Governance/Contract lanes
   pass, and all intentional baseline changes are documented.

## Tasks / Subtasks

- [ ] Rebase the product/generated baseline on the approved Story 11.20 ledger.
- [ ] Burn down product findings by package and diagnostic category.
- [ ] Fix generated-code findings in SourceTools emitters with red-green-refactor tests.
- [ ] Validate samples and generated test specimens without editing generated output.
- [ ] Run package/public-API/schema/generated-output compatibility lanes required by changed surfaces.
- [ ] Run full Release/default/Governance/Contract/artifact validation.

## Dev Notes

### Census scope

Product source has 367 findings: Shell 217; Contracts net10.0 119; Mcp 24; Schema net10.0 3;
Contracts.UI 2; Cli 1; Testing 1. The non-test generated subset is Counter.Domain 79,
Counter.Specimens.Domain 94, and IdeParityCounter 28. Shell.Tests has another 302 generated findings;
the emitter fix belongs here while consumer validation is shared with Story 11.22.

### Boundaries

- Do not hand-edit generated files.
- Do not change `CanonicalSchemaMaterial` or fingerprint algorithms.
- Do not add package versions to project files or add analyzer packages.
- Keep each C# type in its own file when new types are genuinely required.
- Preserve fail-closed MCP gates and source-generator pure-IR equality contracts.

### Validation lanes

Run forced Release builds with and without command-line `AnalysisMode=Recommended`, focused tests for
every changed package/emitter, generated-output and snapshot parity, package/public-API/schema lanes,
the default solution lane, and artifact validation. Use `DiffEngine_Disabled=true` for tests.

## References

- `_bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md`
- `_bmad-output/implementation-artifacts/11-20-recommended-analyzer-policy-and-exception-ledger.md`
- `_bmad-output/implementation-artifacts/11-18-hot-path-log-sites.md`
- `_bmad-output/implementation-artifacts/11-18-warning-and-above-log-sites.md`

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

## Change Log

- 2026-07-16: Materialized approved staged-activation Phase 2 from Story 11.19d.
