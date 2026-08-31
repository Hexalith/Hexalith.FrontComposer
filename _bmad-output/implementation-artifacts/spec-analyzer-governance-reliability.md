---
title: 'Harden analyzer governance reliability'
type: 'bugfix'
created: '2026-08-31'
status: 'done'
baseline_revision: 'd738598b96d1c153be498af5e13ea0cf115830f5'
baseline_commit: 'd738598b96d1c153be498af5e13ea0cf115830f5'
review_loop_iteration: 0
followup_review_recommended: true
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/.bmad-loop/runs/20260830-234810-e850/bundles/analyzer-governance-reliability/intent.md'
warnings: []
deferred:
  - summary: >-
      The packaged-analyzer test's legacy dotnet helper still drains stdout and stderr sequentially and does not terminate the child on cancellation.
    evidence: |-
      `RunDotnetAllowingFailureAsync` in `PackagedAnalyzerConsumerTests.cs` awaits stdout, then stderr, then `WaitForExitAsync` with the caller token. This pre-dates the Debug/Release matrix and can deadlock on a full stderr pipe or leave dotnet running when the test token is cancelled.
    location: >-
      tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs:315
    severity: medium
---

<intent-contract>

## Intent

**Problem:** Analyzer governance is correct but operationally fragile: overlapping rebuild proofs contend with live test outputs, a thirteen-project loop makes blocking lanes excessively slow, timeout teardown can lose output or leak children, packaged-consumer proof is Release-only, the CA1707 seal tracks irrelevant tokens and lines, and scalar MSBuild values are encoded as diagnostic IDs.

**Approach:** Consolidate expensive build evidence into one isolated blocking lane with shared results and reliable process cleanup; extend packaged-consumer proof across Debug and Release; and migrate the analyzer-policy contract to stable public-declaration inventory plus explicit scalar property values while preserving fail-closed parity.

## Boundaries & Constraints

**Always:** Preserve central `AnalysisMode=Recommended`, `TreatWarningsAsErrors=true`, built-in analyzers only, the exact two CA1707 suppression scopes, all 13 Story 11.22 project-membership obligations, zero-warning/zero-error build proof, Contracts/Schema/SourceTools TFM boundaries, and existing package/public/generated-output behavior. Keep `.bmad-loop` ledger and run bookkeeping read-only.

**Block If:** Completion would require an analyzer package, a broader suppression, weaker warnings policy, removal of a governed project, public/schema/generated-output baseline change, dependency/submodule update, or rewriting historical analyzer census evidence beyond the focused contract migration.

**Never:** Run heavy rebuild facts concurrently with solution test modules; cancel redirected pipe reads with the deadline token; treat cancellation as timeout; repack Debug artifacts for the packaged-consumer matrix; inventory locals, references, discards, private fields, comments, or source line numbers as CA1707 API evidence; or change the deferred-work ledger.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Heavy governance | normal CI | One isolated Shell lane performs two solution builds total and shares forced-Recommended evidence | Any missing project, nonzero exit, warning, or error fails closed |
| Child deadline/cancellation | slow or cancelled process | Kill tree, await exit, drain both pipes; deadline includes output, caller cancellation stays cancellation | Kill races do not mask the governing failure |
| Packaged consumer | shipped Release packages | Debug and Release consumers each generate, compile, and enforce Recommended/TWAE probes from configuration-isolated output | Any CA/ASP diagnostic or missing/wrong generated output fails |
| Identifier seal | tracked suppressed sources | Stable hash covers only CA1707-relevant public/protected declarations and exact source locations | Policy-scope drift or public underscore drift fails; local/line churn does not |
| Warning-control schema | scalar and list controls | Scalar MSBuild rows use `propertyValue`; diagnostic controls retain `diagnosticIds`; canonical parity keys are unchanged | Legacy, ambiguous, empty, or property-incompatible shapes fail closed |

</intent-contract>

## Code Map

- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:328,349,388,416,576,1311,1634,2074` -- inventory/schema/parity authority, heavy proofs, and current process runner.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTestGroup.cs:3` -- intra-module serialization; retain while moving destructive builds out of solution-wide lanes.
- `.github/workflows/quality.yml:79,121,253` -- canonical build and duplicate Governance/default execution; add one blocking heavy-build lane and exclusions.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:181` and `tests/README.md:19` -- executable lane seal and operator command documentation.
- `Hexalith.FrontComposer.slnx:4` -- read-only membership authority for the 13 recorded projects.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs:65,132,168` -- temp package-only consumer and Recommended/Default probes.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RenderTreeSequenceRewriterTests.cs:447` -- existing dual-configuration parse-safety reference; read-only unless regression evidence exposes a defect.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json:98,453` -- governed CA1707 seal and warning-control schema; not the deferred-work ledger.
- `.editorconfig:70` and `Directory.Build.props:28` -- read-only policy invariants.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/GovernanceProcessRunner.cs` and `GovernanceProcessRunnerTests.cs` -- extract the bounded child runner and prove successful, deadline, and caller-cancellation cleanup/output behavior.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs` -- cache canonical/forced solution evidence, assert all 13 `.slnx` members, consume the hardened runner, implement stable CA1707 public-declaration inventory, and enforce the migrated scalar/list schema with hostile mutations.
- [x] `.github/workflows/quality.yml`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`, and `tests/README.md` -- exclude a dedicated heavy trait from Governance/default solution lanes, run it once as a blocking Shell-only lane, and pin/document exact MTP filters and evidence.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs` -- keep Release package inputs but build/probe Debug and Release consumers with configuration-isolated generated sources.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- bump the additive schema version, migrate TWAE/AnalysisMode scalar rows to `propertyValue`, and reseal only the identifier fields required by the new algorithm; preserve canonical parity strings and history.

**Acceptance Criteria:**
- Given the Quality workflow, when blocking test lanes run, then destructive analyzer builds execute once outside concurrent solution modules and produce authenticated nonzero MTP evidence.
- Given all 13 Story 11.22 paths, when membership and forced Recommended evidence are checked, then every path remains an enabled Release solution member and the shared build is 0 warnings/0 errors.
- Given scalar/list schema mutations or CA1707 scope/public-symbol drift, when Governance validates the contract, then it rejects invalid shapes/drift while routine local-token and line-only edits leave the identifier seal unchanged.
- Given the packaged analyzer, when the focused fact runs, then both Debug and Release generated consumers pass clean-build/output checks and fail/pass the CA1822 Recommended/Default contrast respectively.

## Spec Change Log

## Review Triage Log

### 2026-08-31 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 18: (high 9, medium 6, low 3)
- defer: 1: (high 0, medium 1, low 0)
- reject: 5: (high 0, medium 1, low 4)
- addressed_findings:
  - `high` `patch` Resealed the identifier inventory against a temporary candidate index containing every new tracked test file.
  - `medium` `patch` Excluded `GovernanceBuild` from release prepublication and pinned the exclusion in Python and C# governance tests.
  - `medium` `patch` Added the durable heavy-lane trait, filter, and exact-evidence rules to project context.
  - `low` `patch` Corrected the focused Governance command in `tests/README.md` to exclude the live build proofs.
  - `high` `patch` Extended MTP evidence validation and the Quality workflow to authenticate exactly the two intended fully-qualified test identities.
  - `medium` `patch` Sealed the heavy execution/evidence step names against duplicates and rejected conditional execution.
  - `high` `patch` Added a five-second secondary cleanup bound spanning process exit and both redirected pipe drains.
  - `high` `patch` Arbitrated deadline/cancellation races in favor of the caller token while preserving cancellation diagnostics.
  - `medium` `patch` Asserted captured stdout and stderr on caller cancellation.
  - `medium` `patch` Synchronized cancellation with child readiness instead of a scheduling-sensitive timer.
  - `low` `patch` Added a nonzero-exit runner case that proves both redirected pipes are still drained.
  - `high` `patch` Inventoried underscore-bearing identifiers in every qualified namespace segment and added hostile dotted-namespace evidence.
  - `medium` `patch` Added a protected-only declaration rename check to the CA1707 inventory proof.
  - `high` `patch` Rejected empty and whitespace-only diagnostic identifiers for diagnostic-control shapes.
  - `high` `patch` Rejected warning-control fields that are not allowed by the selected source/property shape.
  - `high` `patch` Required EditorConfig diagnostic identifiers to match the diagnostic encoded in the property name.
  - `high` `patch` Treated wildcard `*|*` solution disables as disabling every Release configuration.
  - `low` `patch` Added a temporary-candidate-index whitespace/seal verification path so untracked reviewed files are not omitted.

## Design Notes

The heavy trait is additive to class-level `Governance`: existing lanes explicitly exclude it, while a Shell-project-only blocking step selects it. Cache failures as evidence rather than retrying. Canonicalize scalar rows back to their existing `msbuild|path|property|value` keys so schema clarity does not alter policy parity.

## File List

- `.github/workflows/quality.yml`
- `.github/scripts/ci_governance.py`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/implementation-artifacts/spec-analyzer-governance-reliability.md`
- `_bmad-output/project-context.md`
- `eng/release_prepublish.py`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/GovernanceProcessRunner.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/GovernanceProcessRunnerTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs`
- `tests/README.md`
- `tests/eng/test_ci_governance.py`
- `tests/eng/test_release_prepublish.py`

## Verification

**Commands:**
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: 0 warnings and 0 errors.
- `DiffEngine_Disabled=true dotnet test --project tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-build --filter-trait "Category=GovernanceBuild" --results-directory ./TestResults/governance-build --report-xunit-trx` -- expected: isolated heavy proofs pass with nonzero evidence.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests` -- expected: schema, inventory, parity, timeout, and build proofs pass.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -parallel none -method Hexalith.FrontComposer.SourceTools.Tests.Integration.PackagedAnalyzerConsumerTests.PackagedAnalyzer_ContractsOnlyPayload_GeneratedShellConsumerCompiles` -- expected: Debug and Release package-consumer matrix passes.
- `git diff --check` -- expected: no whitespace errors; no `.bmad-loop` ledger changes.
- `python3 -m unittest tests/eng/test_ci_governance.py tests/eng/test_release_prepublish.py` -- expected: exact-identity evidence and release filter tests pass.
- Temporary candidate-index inventory/whitespace checks -- expected: the final tracked `tests/**` file set matches the CA1707 seal and every tracked/untracked reviewed file is whitespace-clean without touching the real index.

**Results (2026-08-31):**
- Final Release solution build passed in 19.30s with 0 warnings and 0 errors; the focused Shell test-project rebuild also passed with 0 warnings and 0 errors.
- Isolated `GovernanceBuild` MTP lane passed 2/2 in 56.307s; the evidence validator accepted one TRX containing exactly the two allowlisted identities.
- Full `AnalyzerPolicyGovernanceTests` direct lane passed 7/7 in 92.524s against a temporary final-candidate index.
- `GovernanceProcessRunnerTests` passed 5/5 in 9.152s, including caller cancellation and a detached pipe-holder cleanup bound.
- Packaged analyzer consumer matrix passed 1/1 in 14.092s, exercising clean Debug and Release generated output plus both CA1822 contrasts from Release-packed inputs.
- Focused CI governance command/evidence facts passed 2/2; the final Python governance/release suite passed 21/21 in 2.930s.
- The candidate-index CA1707 seal passed at 3,274 declarations with SHA-256 `45a91360dadfaae47984ff873d27fed8810d3c9ad97cadfade10b11156cf5e7c`; `.bmad-loop` remained unchanged.

## Auto Run Result

### Summary

Resolved DW-682, DW-689, DW-690, DW-699, DW-706, and DW-707 as one analyzer-governance reliability change: expensive build proofs are serialized in one authenticated lane and share results, process teardown is bounded and cancellation-safe, packaged consumers run a Debug/Release matrix from Release packages, and the analyzer-policy contract uses semantic declaration seals plus explicit scalar property values.

### Files Changed

- `.github/scripts/ci_governance.py` — validates an exact repeatable test-identity allowlist in MTP TRX evidence.
- `.github/workflows/quality.yml` — isolates the two heavy analyzer build proofs and authenticates their identities.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` — migrates schema 1.1 scalar values and reseals semantic CA1707 declarations.
- `_bmad-output/implementation-artifacts/spec-analyzer-governance-reliability.md` — records scope, review triage, verification, and terminal result.
- `_bmad-output/project-context.md` — documents the durable `GovernanceBuild` lane and release exclusions.
- `eng/release_prepublish.py` — keeps heavy build proofs out of candidate-preparation test runs.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs` — shares build evidence, replaces per-project rebuilds with sealed solution membership, and hardens schema/inventory validation.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` — seals lane uniqueness, filters, exact identities, and release adoption.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/GovernanceProcessRunner.cs` — provides bounded kill/exit/drain behavior with cancellation diagnostics.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/GovernanceProcessRunnerTests.cs` — covers success, nonzero exit, timeout, cancellation, child cleanup, and stuck pipe ownership.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs` — builds and probes Debug and Release consumers from one Release package set.
- `tests/README.md` — documents default, Governance, and isolated heavy-lane filters.
- `tests/eng/test_ci_governance.py` — proves exact TRX identity validation fails closed.
- `tests/eng/test_release_prepublish.py` — pins the `GovernanceBuild` release exclusion.

### Review Findings

- Applied 18 patches: 9 high, 6 medium, and 3 low severity.
- Deferred 1 pre-existing medium-severity packaged-consumer process-helper issue in this spec's `deferred` frontmatter.
- Rejected 5 findings as speculative, redundant, or outside any observable contract.
- Follow-up review recommendation: `true`; patch score is `3 × 6 + 3 = 21`, and high-severity patches were applied.

### Verification

- Release solution and focused project builds: passed, 0 warnings and 0 errors.
- Analyzer governance class: 7/7 passed against the final candidate index.
- Isolated heavy lane: 2/2 passed; one TRX contained exactly both required identities.
- Process runner: 5/5 passed; packaged consumer matrix: 1/1 passed.
- Python governance/release tests: 21/21 passed; focused CI governance facts: 2/2 passed.
- Candidate-index identifier seal: 3,274 declarations and the recorded SHA-256 matched.

### Residual Risks

The packaged-consumer test retains its older sequential redirected-stream helper; the verified matrix passes, but that helper should receive a separate bounded process-runner migration. No deferred-work ledger or `.bmad-loop` run bookkeeping was edited.
