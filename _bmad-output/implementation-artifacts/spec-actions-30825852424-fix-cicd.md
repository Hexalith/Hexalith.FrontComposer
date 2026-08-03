---
title: 'Restore frozen Release Evidence runs'
type: 'bugfix'
created: '2026-08-03'
status: 'done'
review_loop_iteration: 0
baseline_commit: '6b342857191aed200b7608efb6758518872874da'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md'
  - '{project-root}/_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release Evidence run `30825852424` fails because it unconditionally requires a `release-verification-handoff`, although upstream Release run `30825825216` was intentionally frozen (`freeze-guard=success`, `release=skipped`) and therefore ran no producer. The later `always()` steps also lose forensic evidence by copying absent handoffs before creating metadata.

**Approach:** Authenticate the completed Release run and its job topology through the read-only Actions API before verification. Treat only a proven frozen/ineligible topology where no release job started as a non-attempt; retain mandatory handoff authentication and the existing verifier for every governed release attempt.

## Boundaries & Constraints

**Always:** Preserve the REL-4 fail-closed publication freeze, the `CI → Release → Release Evidence` chain, read-only permissions, all-conclusion observation, exact run ID/attempt checks, and independent verification for started release jobs. Seed metadata before fallible authentication and upload nonempty disposition/evidence artifacts on no-attempt and failure paths.

**Ask First:** Enabling publication, changing the handoff schemas or AD-13/AD-15 architecture, activating the pending AD-16 producer/evaluator integration, modifying the reusable Builds workflow or any submodule, or adding a different release authorization mechanism requires explicit approval.

**Never:** Do not inspect the mutable current repository variable to classify a historical run; fabricate or weaken handoffs; use the second-hop/default-branch SHA as a governed release candidate; green-no-op a started, failed, cancelled, partial, or ambiguous release attempt; add a fourth chained workflow; or grant write permissions.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Frozen | One successful `freeze-guard`, skipped `release`, no publish job started | Green no-attempt disposition with metadata; skip checkout, submodules, .NET, and handoff verification | Unexpected tag/publication evidence remains blocking |
| Ineligible | Guard and release jobs were both skipped by the caller conditions | Green distinct ineligible disposition with metadata | Do not label the run frozen or compliant |
| Governed attempt | Reusable `release / release` started with any conclusion | Run the existing verifier and require exactly one current Release handoff plus its CI handoff | Missing, duplicate, expired, malformed, stale, or mismatched evidence fails |
| Unexpected/API failure | Missing, duplicate, contradictory jobs or Actions API failure | Fail closed while retaining run/job metadata | Upload the forensic disposition artifact |
| Authenticated unpublished attempt | Valid handoffs declare `published=false`, null release/manifest, and empty assets | Existing no-publication verification completes green | Any omitted projection or observed side effect fails |

</frozen-after-approval>

## Code Map

- `.github/workflows/release-evidence.yml` -- currently authenticates handoffs before distinguishing an intentional non-attempt and seeds evidence too late.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- static and executable workflow-contract coverage; its tag-resolver tests start after the failing authentication boundary.
- `.github/workflows/release.yml` -- defines the expected frozen and invoked job shapes; behavior remains unchanged.
- `eng/dependency_handoff.py` -- authoritative governed-attempt handoff validator; remains unchanged.

## Tasks & Acceptance

**Execution:**
- [x] `.github/workflows/release-evidence.yml` -- add an authenticated, evidence-producing disposition boundary before the expensive verifier; bypass mandatory handoffs only when no publication job started, and retain the existing governed path unchanged.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- behaviorally cover frozen, ineligible, started, malformed, and API-failure topologies, plus early metadata/upload guarantees.

**Acceptance Criteria:**
- Given the exact topology from Release run `30825825216`, when Release Evidence classifies it, then the workflow records a frozen non-attempt and succeeds without requesting a handoff.
- Given `release / release` started, when its workflow completes with success, failure, or cancellation, then missing or invalid handoffs fail and cannot become a no-op.
- Given classification or authentication fails, when cleanup runs, then a nonempty metadata artifact is still uploaded and the workflow remains failed.
- Given the governed handoffs are valid, when verification runs, then the existing tag, immutable-release, manifest, NuGet-byte, signature, ledger, and incident checks remain effective.

## Spec Change Log

- 2026-08-03: Approved implementation completed. Added attempt-scoped Release run/job authentication, frozen/ineligible/governed dispositions, early forensic metadata, fail-closed ambiguity/API handling, and executable workflow topology coverage.
- 2026-08-03: Matrix audit closed the Frozen-row side-effect branch. Added a read-only, no-checkout diagnostic tag/GitHub Release probe that blocks observed publication evidence and API ambiguity/failure without changing governed candidate sourcing.
- 2026-08-03: Adversarial review hardening added bounded API calls, exact freeze-step and handoff-attempt binding, paginated artifact enumeration, immediate authenticated-evidence retention, first-parent tag matching, draft-release blocking, and mutation-resistant fixtures for identity mismatches and historical release state. The pre-existing governed resolver's all-parent match was deferred separately.

## Design Notes

Classify from the triggering run's immutable run/job records, not the current freeze variable, which may change after completion. A skipped caller job is named `release`; an invoked reusable job is reported as `release / release`. This transitional boundary does not replace the blocked AD-16 producer integration: it only distinguishes "no release job executed" from a governed attempt.

## Verification

**Commands:**
- `actionlint .github/workflows/release-evidence.yml .github/workflows/release.yml .github/workflows/ci.yml` -- expected: all workflow syntax and expressions pass.
- `python3 -m unittest tests/eng/test_dependency_handoff.py` -- expected: governed handoff contracts remain green.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release -p:NuGetAudit=false` -- expected: zero warnings and errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests` -- expected: all CI/release workflow governance tests pass.
- `git diff --check` -- expected: no whitespace errors.

**Results (2026-08-03):**
- `actionlint .github/workflows/release-evidence.yml .github/workflows/release.yml .github/workflows/ci.yml` -- passed with no diagnostics.
- `python3 -m unittest tests/eng/test_dependency_handoff.py` -- 5/5 passed.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release -p:NuGetAudit=false` -- succeeded with 0 warnings and 0 errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests` -- 62/62 passed, including seventeen executable cases covering clean frozen/ineligible dispositions with unrelated history, direct/first-parent tag evidence, release-only and draft-release evidence, probe API failure/ambiguity, started success/failure/cancellation, malformed/side-effect topology, exact run/attempt mismatches, jobs API failure, and run API failure.
- `git diff --check` -- passed.
- Frozen approval block SHA-256 remained `52f11e9ce3cb19ae912b5e53705469c9633434ac795ee2ef4984bd012f1e0b09`; CRLF line endings were preserved in all changed files.

## Suggested Review Order

**Run disposition boundary**

- Authenticate immutable run topology before deciding whether any release attempt occurred.
  [`release-evidence.yml:103`](../../.github/workflows/release-evidence.yml#L103)

- Probe only release-shaped tags and releases under bounded, fail-closed API budgets.
  [`release-evidence.yml:319`](../../.github/workflows/release-evidence.yml#L319)

**Governed attempt preservation**

- Bind the mandatory handoff to the exact authenticated run attempt before checkout.
  [`release-evidence.yml:556`](../../.github/workflows/release-evidence.yml#L556)

- Authenticate the original CI candidate and retain evidence immediately after validation.
  [`release-evidence.yml:636`](../../.github/workflows/release-evidence.yml#L636)

**Forensics and verification**

- Always upload seeded or authenticated evidence, including failed classification paths.
  [`release-evidence.yml:1160`](../../.github/workflows/release-evidence.yml#L1160)

- Pin step ordering, conditional gates, pagination, and exact-attempt binding statically.
  [`CiGovernanceTests.cs:2468`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L2468)

- Execute seventeen topology and publication fixtures against extracted workflow scripts.
  [`CiGovernanceTests.cs:2539`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L2539)

**Deferred peripheral**

- Record the pre-existing governed resolver's all-parent match for focused follow-up.
  [`deferred-work.md:2027`](deferred-work.md#L2027)
