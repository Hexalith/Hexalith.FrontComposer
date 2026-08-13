---
title: 'Stop Release prepare-candidate from fail-closing on advisory Performance tests'
type: 'bugfix'
created: '2026-08-13'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'd4d7bfe3c51ed2e6f394b709315e04068ba72a9c'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release run [31723965027](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31723965027) failed `prepare-candidate` on two SourceTools wall-clock tests tagged `Category=Performance`. CI already keeps that trait advisory (Gate 3c `continue-on-error`); release `phase_tests()` only excludes `Quarantined`, so shared-runner noise (3086 ms vs 500 ms parse budget; p95 1384 ms vs 1000 ms drift cache-miss) blocks publication.

**Approach:** Make the release orchestrator's `dotnet test --filter` match the blocking CI default lane (Gate 3a). Leave Performance tests, NFR/IDE-MUST-006 budgets, and Gate 3c advisory mode unchanged. Pin the new filter pair in governance so a comment-only string cannot satisfy the contract.

## Boundaries & Constraints

**Always:** Keep prepare-candidate fail-closed for the default (non-advisory) set. Keep `TEST_PROJECTS`, `--configuration Release --no-build`, `DiffEngine_Disabled=true`, package count `8`, and freeze/publish fail-closed. Update the governance pin that currently requires `"--filter", "Category!=Quarantined",` so it requires the Gate 3a filter argument pair. Keep Performance tests in-repo and runnable via Gate 3c.

**Ask First:** Re-blocking Performance (or e2e-palette / NightlyProperty) on release; widening 500 ms / p95 2× budgets; quarantining the two tests; changing SourceTools parse/drift for speed; dispatching or authorizing a real release; touching secrets, `production`, or `NUGET_API_KEY`.

**Never:** Do not add `continue-on-error` to prepare-candidate. Do not skip tests without a trait filter. Do not weaken `TreatWarningsAsErrors`. Do not hand-edit generated code or `CanonicalSchemaMaterial`. Do not claim the two GHA timings are a generator regression requiring a perf story.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Prepare tests, Performance-tagged | SourceTools ParseStage / DriftBenchmark (`Category=Performance`) | Filter excludes them; prepare continues | N/A |
| Prepare tests, default-lane failure | Non-advisory test exits non-zero | `phase_tests` still fail-closed | `PhaseFailure` / exit 1 |
| Governance pin | Orchestrator `dotnet test` argv | Exact `"--filter", "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined",` | Test fails if only a comment matches or the old Quarantined-only pair remains |
| CI Gate 3c | `Category=Performance` | Remains advisory (`continue-on-error: true`) | Unchanged |

</frozen-after-approval>

## Code Map

- `eng/release_prepublish.py` L71–72 comment, L233–261 `phase_tests()` — L258 is `"--filter", "Category!=Quarantined",`. Change that pair (and the “Quarantined excluded” comment) to the Gate 3a string. Do not change `TEST_PROJECTS` or env scrubbing.
- `.github/workflows/quality.yml` L199–234 — Gate 3a filter (L222) is the source of truth; Gate 3c (L230–234) stays advisory. Read-only except as the string to copy.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` `BlockingTestLanes_ExcludeQuarantinedTestsWithoutSkippingGovernance` L141–165 — L153 already pins Gate 3a on quality.yml; L164 pins the old orchestrator pair. Update L164 (and the REL-3 comment) to the same Gate 3a pair. Do not touch L2144 (`ci_governance.py`).
- Failed tests (do not edit): `tests/Hexalith.FrontComposer.SourceTools.Tests/Performance/ParseStagePerformanceTests.cs` L11/L14–43; `tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/DriftBenchmarkTests.cs` L20–23/L64–101. Both are `Category=Performance`, wall-clock `Stopwatch`, no CI skip.
- `_bmad-output/project-context.md` L214–221 — documents CI advisory Performance; add that release prepare uses the Gate 3a filter (same exclusions).
- Read-only: `tests/eng/test_release_prepublish.py` (no filter pin); `tests/README.md` L35 already shows Gate 3a.

## Tasks & Acceptance

**Execution:**
- [x] `eng/release_prepublish.py` -- set `phase_tests()` `--filter` to the Gate 3a string and update the L71–72 comment -- release must not fail-close on advisory traits
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- pin the orchestrator `--filter` argument pair to that same Gate 3a string -- prevent comment-only or Quarantined-only regressions
- [x] `_bmad-output/project-context.md` -- record that release prepare uses the Gate 3a exclusions -- keep agent/release policy aligned

**Acceptance Criteria:**
- Given prepare-candidate `phase_tests()`, when SourceTools Performance tests would exceed wall-clock budgets, then they are excluded by filter and do not fail the job.
- Given a default-lane test failure, when `phase_tests()` runs, then prepare still exits non-zero.
- Given `CiGovernanceTests.BlockingTestLanes_ExcludeQuarantinedTestsWithoutSkippingGovernance`, when the orchestrator filter is stale or only present in a comment, then the test fails.
- Given this work alone, when Gate 3c and the two Performance test bodies are inspected, then they are unchanged and publication/secrets were not touched.

## Spec Change Log

## Design Notes

REL-2/REL-3 made release a fail-closed superset of CI minus only `Quarantined`. That predates Gate 3c staying advisory for runner-scheduler noise. Aligning release with Gate 3a restores the documented “Performance is not a unit gate” contract (`DriftBenchmarkTests` header; quality.yml Story 3-7 D6) without loosening NFR budgets. Copy the Gate 3a filter verbatim — do not invent a new exclusion set.

## Verification

**Commands:**
- `python3 -m py_compile eng/release_prepublish.py` -- expected: exit 0
- `python3 -m unittest tests/eng/test_release_prepublish.py` -- expected: OK
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~BlockingTestLanes_ExcludeQuarantinedTestsWithoutSkippingGovernance"` -- expected: passed
- `rg -n 'Category!=Quarantined' eng/release_prepublish.py tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- expected: orchestrator `dotnet test` argv is the Gate 3a pair; L2144 `ci_governance.py` pin unchanged

## Suggested Review Order

**Release filter**

- Entry point: prepare-candidate now uses the Gate 3a `--filter` instead of Quarantined-only.
  [`release_prepublish.py:259`](../../eng/release_prepublish.py#L259)

- Comment records the same Gate 3a exclusion set next to `TEST_PROJECTS`.
  [`release_prepublish.py:72`](../../eng/release_prepublish.py#L72)

**Governance pins**

- Pins the executable argv pair and rejects a leftover Quarantined-only invocation.
  [`CiGovernanceTests.cs:164`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L164)

- Keeps Gate 3c advisory (`continue-on-error`) as a non-blocking lane.
  [`CiGovernanceTests.cs:167`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L167)

**Policy**

- Documents that release prepare uses the same Gate 3a exclusions as CI.
  [`project-context.md:221`](../project-context.md#L221)

**Tests**

- Source-pins `phase_tests()` fail-closed `run()` plus the trailing-comma Gate 3a pair.
  [`test_release_prepublish.py:145`](../../tests/eng/test_release_prepublish.py#L145)
