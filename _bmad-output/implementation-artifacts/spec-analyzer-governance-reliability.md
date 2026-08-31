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
  - summary: >-
      The untraited hidden-control negative probe still runs two `--no-incremental` project rebuilds inside the solution-wide Governance lane.
    evidence: |-
      `AnalyzerPolicy_Story1122HiddenControlNegativeProbes_RemainClean` carries only `Category=Governance`, so `dotnet test Hexalith.FrontComposer.slnx --filter-trait "Category=Governance"` still rebuilds `Shell.Tests.csproj` and `Testing.Tests.csproj` non-incrementally while other solution test modules run. This predates the story and is the same contention class the `GovernanceBuild` trait isolated for the two solution builds; it was not reclassified here.
    location: >-
      tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:AnalyzerPolicy_Story1122HiddenControlNegativeProbes_RemainClean
    severity: medium
  - summary: >-
      `CiGovernanceTests.EventStoreRuntimeIdentityPinsOwnerApprovedTupleAndTruthfulDriftEvidence` fails on an EventStore gitlink that no longer matches its owner-approved pin.
    evidence: |-
      The fact expects `38967215e6c1b13e77f2b0006efd95d88d7ad7b8` but the gitlink is `1194dfe59bcbc9b235390d1e46a7dfe4ee115d94`. That gitlink is byte-identical at this story's baseline `d738598b` and at HEAD, and the story never touched the fact or its pin, so the red is a concurrent EventStore-pin drift owned outside this work.
    location: >-
      tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:3454
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

### 2026-08-31 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 13: (high 3, medium 5, low 5)
- defer: 2: (high 0, medium 2, low 0)
- reject: 19: (high 0, medium 3, low 16)
- addressed_findings:
  - `high` `patch` Closed the Release-membership fail-open: a `Build/@Solution` token is now parsed into its configuration half, so `*|Any CPU`, `Release|Any CPU`, and mixed-token spellings are Release disables, with a `Debug|*` non-finding case pinning the inverse.
  - `high` `patch` Made the MTP identity allowlist authenticate outcomes, not just names, so a skipped or non-executed heavy proof can no longer satisfy the gate at `dotnet test` exit 0; added passing/skipped TRX fixtures and two Python facts.
  - `high` `patch` Restored the argument-level guards the deleted thirteen-project loop carried: a cheap fact now proves the forced leg adds `-p:AnalysisMode=Recommended`, the canonical leg does not, and neither injects a warnings-weakening override.
  - `medium` `patch` Gave the whole-solution `--no-incremental` builds their own deadline instead of the 180s bound sized for single-project builds.
  - `medium` `patch` Pinned the `GovernanceBuild` trait set against the workflow `--expected-test` allowlist by reflection, so a renamed, added, or removed heavy fact fails at test time rather than at the CI evidence step.
  - `medium` `patch` Replaced the source-control shape check with an entryCount/paths/diagnosticIds agreement invariant that closes the empty-array hole in both directions while preserving the deliberate zero-entry `source-emitter-pragmas` census row.
  - `medium` `patch` Observed the abandoned drain task so a post-disposal `ExitCode` read cannot surface as an unobserved task exception, and recorded why the completion source exists.
  - `medium` `patch` Fixed the Windows long-running child, whose `param()` block cannot bind under `-Command`; the PID path now travels through the environment.
  - `low` `patch` Replaced the bare Windows early return in the pipe-holder proof with an explicit skip so it stops recording a pass it never earned.
  - `low` `patch` Anchored the synthetic identifier inventory to an absolute count so a regression that inventories nothing cannot satisfy the relative drift assertions.
  - `low` `patch` Scoped the per-test roll-up in the evidence payload to allowlisted lanes instead of dumping every identity the solution default lane ran.
  - `low` `patch` Made the EditorConfig diagnosticIds/property comparison `Ordinal`, matching the canonical parity key it feeds.
  - `low` `patch` Replaced the tautological packaged-consumer isolation assertion with a real per-leg freshness proof, asserted the generated root exists before enumerating it, and bound the consumer TFM to one constant.

### 2026-08-31 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 15: (high 1, medium 4, low 10)
- defer: 0
- reject: 23: (high 0, medium 3, low 20)
- addressed_findings:
  - `high` `patch` Bound each cached solution build to the argument vector that produced it: the two adjacent `Lazy` initializers differ by one token, and nothing stopped a slip from making `MatchesForcedRecommendedCandidate` compare a build against itself and pass.
  - `medium` `patch` Sized the heavy-build deadline to the CI job it runs in: 900s per build across two builds could never fire inside `build-and-test`'s `timeout-minutes: 20`, so the fail-closed timeout was unreachable and an undiagnosable job kill would win.
  - `medium` `patch` Took the shared build cache off `TestContext.Current.CancellationToken`, which donated whichever heavy fact touched the `Lazy` first its token to the other and would cache a cancelled task permanently.
  - `medium` `patch` Closed the editorconfig warning-control fail-open: a property that is not `dotnet_diagnostic.<id>.severity` skipped the diagnosticIds agreement check entirely and accepted any payload.
  - `medium` `patch` Pinned `GovernanceBuild` to the one project the heavy lane runs; the trait is excluded solution-wide but selected only in Shell.Tests, and the reflection pin sees only that assembly, so the trait elsewhere would execute in no lane at all.
  - `low` `patch` Scoped the identity-allowlist regex to the evidence step and normalized reflection's nested-type `+` to the `.` spelling MTP and the workflow use.
  - `low` `patch` Removed the unreachable `SymbolKind.Namespace` branch from the CA1707 predicate; namespaces are inventoried by their own syntax branch, which returns before any symbol is resolved.
  - `low` `patch` Stopped the unexpected-field sweep from firing on an unresolved shape, where it reported every legitimately required field as unexpected and buried the real diagnostic.
  - `low` `patch` Gave the deadline runner test the same tolerant PID poll its sibling cancellation test uses, instead of a strict existence assertion that races a slow child.
  - `low` `patch` Guarded the temporary-root deletion so a surviving child's handle cannot replace the assertion message that reports it.
  - `low` `patch` Widened the detached pipe-holder bound from 8s to 20s: 500ms plus the 5s cleanup grace left 2.5s of slack, which is a flake rather than a stronger claim about a 60s sleep.
  - `low` `patch` Fixed the Windows long-running child, where cmd reads `^>` as a literal `>` and turned the redirection into a bad ping argument.
  - `low` `patch` Removed a stray double blank line in the packaged-consumer matrix that `git diff --check` does not catch.
  - `low` `patch` Added the modified `CiGovernanceTests` class to the Verification commands; it was changed by this story but no listed lane ran it.
  - `low` `patch` Corrected the stale identifier-seal figures recorded in the Verification results, which named a candidate-index run rather than the committed seal.

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
- `tests/ci-governance/fixtures/mtp-quarantine/heavy-pass/module-heavy.trx`
- `tests/ci-governance/fixtures/mtp-quarantine/heavy-skipped/module-heavy.trx`
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
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests` -- expected: every workflow/orchestrator pin passes; the EventStore gitlink fact is the one recorded concurrent red.
- Temporary candidate-index inventory/whitespace checks -- expected: the final tracked `tests/**` file set matches the CA1707 seal and every tracked/untracked reviewed file is whitespace-clean without touching the real index.

**Results (2026-08-31, follow-up review pass):**
- Release solution build passed with 0 warnings and 0 errors (27.76s cold, 8.11s incremental after the final patch).
- Full `AnalyzerPolicyGovernanceTests` direct lane passed 8/8 in 56.005s, including both heavy solution builds under the new 420s per-build deadline.
- `GovernanceProcessRunnerTests` passed 5/5 in 9.211s after the widened detached-pipe bound.
- `CiGovernanceTests` ran 80 tests with 1 failure: only `EventStoreRuntimeIdentityPinsOwnerApprovedTupleAndTruthfulDriftEvidence`, the concurrent EventStore gitlink drift already recorded in `deferred`. Both new `GovernanceBuild` pins passed.
- Isolated `GovernanceBuild` MTP lane passed 2/2 in 27.914s; `validate-mtp-evidence` returned `ok: true` against exactly the two allowlisted identities.
- Packaged analyzer consumer matrix passed 1/1 in 10.768s across the Debug and Release legs.
- `python3 -m unittest tests/eng/test_ci_governance.py tests/eng/test_release_prepublish.py` passed 23/23 in 2.357s.
- The CA1707 seal was resealed to 3,277 declarations, SHA-256 `94794758035e3c26ba7cbef15a7d700ce981e823ae6d86ae209f82add88127e3`, for the one new underscore-bearing public test method; `AnalyzerPolicy_IdentifierInventory_MatchesSeal` then passed.
- `git diff --check` reported no whitespace errors in tracked source; `.bmad-loop` and the deferred-work ledger were not modified.

## Documented Unrelated Changes

- `_bmad-output/implementation-artifacts/deferred-work.md` - orchestrator-owned sweep ledger bookkeeping written outside this story; its entries and statuses are not story output and are never edited here.

## Auto Run Result

Status: done

### Summary

Follow-up review pass over the analyzer-governance reliability change (baseline `d738598b`, delivered by `1610415e` and `d0dbc2ce`). No intent gap and no spec defect: the implementation still matches the frozen contract. Fifteen patches were applied. The one high-severity finding is a verification hole this story opened: moving the two solution builds into adjacent static `Lazy` fields left nothing binding either cached result to the command that produced it, so a one-token slip between the two initializers would have made the forced-vs-activated comparison a self-comparison that passes. Four medium patches close a deadline that could never fire inside its own CI job, a shared cache that inherited one test's cancellation token, an editorconfig warning-control shape that failed open on an unrecognized property, and a heavy trait that would have silently executed in no lane at all if applied outside Shell.Tests.

### Files Changed

- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs` — cached builds now carry their command line and are asserted per leg; the shared build runs under `CancellationToken.None`; the per-build deadline is 420s; unrecognized editorconfig properties are rejected; the unexpected-field sweep no longer fires on an unresolved shape; the dead namespace branch is gone.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` — the identity-allowlist pin is scoped to its evidence step and normalizes nested-type spelling; a new source-level fact pins `GovernanceBuild` to the Shell.Tests project.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/GovernanceProcessRunnerTests.cs` — tolerant PID poll in the deadline test, guarded temporary-root cleanup, a realistic detached-pipe bound, and a working Windows redirection.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs` — removed a stray double blank line.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` — resealed the CA1707 test inventory to 3,277 declarations for the one new public test method.
- `_bmad-output/implementation-artifacts/spec-analyzer-governance-reliability.md` — triage log, the added `CiGovernanceTests` verification lane, refreshed results, and this section.

### Review Findings

- Applied 15 patches: 1 high, 4 medium, 10 low.
- Deferred 0 new findings; the three existing `deferred` entries were left exactly as recorded.
- Rejected 23 findings. The substantive rejections: argparse's `append` action copies its default (not the classic mutable-default bug); the duplicate-identity path in the evidence gate fails closed, not open, and both pinned proofs are `[Fact]`s; the outcome check is already constrained by the identity equality that precedes it; the synthetic inventory compilation is declaration-based, so missing metadata references cannot move the seal; and excluding `GovernanceBuild` from release prepublication is correct rather than a hole — a destructive `--no-incremental` solution rebuild in the middle of a `--no-build` artifact validation would invalidate the very artifacts being validated, and Gate 2b remains blocking on the same commit.
- Follow-up review recommendation: `true`; a high-severity patch was applied and the score is `3 x 4 + 10 = 22`.

### Verification

All commands in `## Verification` were re-run after the patches; results are recorded there. Summary: Release solution build 0 warnings / 0 errors; `AnalyzerPolicyGovernanceTests` 8/8; `GovernanceProcessRunnerTests` 5/5; isolated `GovernanceBuild` MTP lane 2/2 with `ok: true` evidence; packaged consumer matrix 1/1; Python governance/release suites 23/23; `CiGovernanceTests` 79/80 with the single failure being the already-deferred EventStore gitlink drift.

### Residual Risks

- The per-build deadline moved from 900s to 420s so it can fire inside `build-and-test`'s 20-minute budget. That is roughly 20x the ~28s local build; a pathologically slow runner would now fail closed with a timeout rather than be killed by the job, which is the intended trade but is a tighter bound than before.
- `release_prepublish.py` no longer runs the forced-Recommended solution build proof, because the heavy facts are excluded there and no replacement lane can run them without rebuilding over the artifacts under validation. The proof remains blocking in CI Gate 2b on the same commit, so this is redundancy lost rather than coverage lost — but the release orchestrator is now dependent on CI for that evidence.
- The Windows branches of the process-runner tests remain corrected by inspection only; CI runs Linux, so they are still unexercised.
- The CA1707 seal is now 3,277 declarations; any further public underscore-bearing test declaration requires an intentional reseal, which is the designed cost of the contract.
