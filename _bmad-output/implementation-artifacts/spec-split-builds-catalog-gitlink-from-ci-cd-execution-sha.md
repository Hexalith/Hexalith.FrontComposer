---
title: 'Split Builds catalog gitlink from CI/CD execution SHA'
type: 'bugfix'
created: '2026-08-16'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'a5a013b84f5137fdccfad5f84600976557bffc3b'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer currently rejects the valid state where the `references/Hexalith.Builds` catalog gitlink (`7867d8fc7bcc3c906b16f0867f6555d8bec5432d`) differs from the reviewed CI/CD execution SHA (`3f0e3595be693fce56a37648c0bd0f89390f5fd3`). This makes catalog-only advances fail `verify-source` and Governance even though semantic dependency policy already evaluates the selected catalog by content.

**Approach:** Authenticate and report the catalog gitlink and execution SHA as separate identities. Keep all execution coordinates mutually identical and immutable while allowing any valid, semantically compatible catalog gitlink.

## Boundaries & Constraints

**Always:** Require the catalog identity to resolve from the exact candidate as a mode-`160000`, lowercase 40-hex gitlink and retain it in dependency-graph provenance. Keep `domain-ci.yml@`, `domain-release.yml@`, `BUILDS_EXECUTION_SHA`, `HEXALITH_BUILDS_EXECUTION_SHA`, `builds-execution-sha`, and Builds execution-checkout refs equal to the approved `3f0e3595…` execution SHA. Authenticate reusable workflow bytes at that execution SHA.

**Ask First:** Moving any execution pin, editing a root-declared submodule, changing evaluator authorizations, or changing workflow YAML requires explicit approval.

**Never:** Re-pin execution coordinates to the catalog gitlink; require catalog SHA equality with execution SHA; add FrontComposer-local package/version mirrors; weaken exact immutable pin checks; rewrite frozen historical specs to conceal the superseded rule.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Independent identities | Valid catalog gitlink differs from equal execution pins | Contract, evidence, and Governance pass; output/provenance preserve both identities | N/A |
| Execution drift | `uses:@`, input, environment, or execution checkout differs | Validation fails closed | Identify the mismatched execution coordinate |
| Missing catalog edge | Candidate lacks a valid mode-`160000` Builds gitlink | Validation fails closed | Report that the candidate gitlink cannot be resolved |

</frozen-after-approval>

## Code Map

- `eng/release_contract.py:320-329,388-407` -- `validate_builds_identity` and the `builds` command currently impose gitlink-to-execution equality; preserve independent shape checks and report both SHAs.
- `eng/release_evidence.py:792-845,1496-1513` -- source provenance preparation/live verification repeat the equality; graph evidence already seals the catalog while workflow provenance seals execution bytes.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:729-807` -- release coordinates are correctly lockstepped, but CI/evidence pins are incorrectly rebound through the gitlink.
- `tests/eng/test_release_contract.py:149-162` and `tests/eng/test_release_evidence_v2.py:339-608` -- fixture seams for divergent catalog, pin mismatch, missing gitlink, sealed execution identity, and live verification.
- `_bmad-output/planning-artifacts/architecture.md:244-249`, `_bmad-output/project-docs/deployment-guide.md:3-6,41-46`, `_bmad-output/project-context.md:263-271`, and `tests/README.md:185-190` -- living guidance still contains or insufficiently rejects the lockstep model.
- `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `.github/workflows/release-evidence.yml`, `eng/dependency-graph-policy.json`, and `references/Hexalith.Builds/**` -- read-only; current execution pins, evaluator authorizations, and catalog content are already correct.
- `_bmad-output/implementation-artifacts/spec-bump-latest-hexalith-nuget-packages-2.md` -- active frozen spec contradicts this correction; record a deferred close/rewrite instead of editing it.

## Tasks & Acceptance

**Execution:**
- [x] `eng/release_contract.py` and `tests/eng/test_release_contract.py` -- validate catalog shape independently, keep exact execution-pin equality, emit both identities, and cover divergence, malformed/missing gitlink, and pin mismatch.
- [x] `eng/release_evidence.py` and `tests/eng/test_release_evidence_v2.py` -- seal/verify catalog through the graph and reusable bytes through execution provenance; add divergent-catalog prepare/live fixtures without weakening wrong-execution failures.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- retain gitlink mode/shape validation and compare every CI/CD execution coordinate to the approved execution SHA, never to the catalog SHA.
- [x] Living architecture, deployment, project context, and test guidance -- document the two independent identities and current catalog companion versions without altering historical frozen records.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- append the required close/rewrite follow-up for the contradictory in-progress package-bump spec.

**Acceptance Criteria:**
- Given current `HEAD`, when the release contract, provenance, and focused Governance checks run, then catalog `7867d8fc…` and execution `3f0e3595…` are both authenticated and the checks pass.
- Given a future compatible catalog-only gitlink advance, when dependency and release validation run, then no execution pin or evaluator authorization rewrite is required.
- Given any execution-coordinate mismatch or missing catalog gitlink, when validation runs, then it fails closed without substituting one identity for the other.

## Spec Change Log

## Design Notes

The dependency graph is the authority for the catalog commit and raw catalog bytes. The active evaluator registry and literal workflow coordinates are the authority for CI/CD execution source. Equality within each contract remains mandatory; equality across the two contracts is permitted but never required. An unmerged local precedent at `7ac58171d4fa18d7178ff206cfde098495b002cd` is read-only implementation evidence and must be reconciled with current `main`, not cherry-picked blindly.

## Verification

**Commands:**
- `python3 -m unittest tests/eng/test_release_contract.py tests/eng/test_release_evidence_v2.py -v` -- expected: divergent catalog and negative identity fixtures pass.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "FullyQualifiedName~ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate|FullyQualifiedName~CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds"` -- expected: focused Governance checks pass.
- `python3 eng/release_contract.py builds --root . --commit "$(git rev-parse HEAD)" --approved 3f0e3595be693fce56a37648c0bd0f89390f5fd3` -- expected: success output contains distinct catalog and execution SHAs.
- `git diff --check` -- expected: no whitespace errors.

## Suggested Review Order

**Identity contract**

- Separate catalog validation from the immutable execution-pin pair at the release gate.
  [`release_contract.py:320`](../../eng/release_contract.py#L320)

- Report both authenticated identities so downstream evidence cannot conflate them.
  [`release_contract.py:411`](../../eng/release_contract.py#L411)

**Provenance boundary**

- Resolve execution bytes from the dedicated checkout with fail-closed object verification.
  [`release_evidence.py:814`](../../eng/release_evidence.py#L814)

- Bind source provenance to execution bytes while catalog identity remains graph-owned.
  [`release_evidence.py:831`](../../eng/release_evidence.py#L831)

- Apply the same execution-source rule during live manifest verification.
  [`release_evidence.py:1546`](../../eng/release_evidence.py#L1546)

**Governance and guidance**

- Enforce one approved execution SHA while independently validating the catalog gitlink shape.
  [`CiGovernanceTests.cs:729`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L729)

- Define catalog and execution as independent architectural contracts.
  [`architecture.md:244`](../planning-artifacts/architecture.md#L244)

- Give release operators the current independent identities and catalog companions.
  [`deployment-guide.md:5`](../project-docs/deployment-guide.md#L5)

- Preserve execution lockstep without forcing catalog-only advances to rewrite workflow pins.
  [`project-context.md:263`](../project-context.md#L263)

**Regression evidence and follow-up**

- Prove divergent identities pass while malformed or mismatched coordinates fail.
  [`test_release_contract.py:149`](../../tests/eng/test_release_contract.py#L149)

- Prove preparation and live verification hash execution bytes, not catalog workflow bytes.
  [`test_release_evidence_v2.py:387`](../../tests/eng/test_release_evidence_v2.py#L387)

- Preserve review-discovered pre-existing release-provenance work for focused correction.
  [`deferred-work.md:2431`](deferred-work.md#L2431)
