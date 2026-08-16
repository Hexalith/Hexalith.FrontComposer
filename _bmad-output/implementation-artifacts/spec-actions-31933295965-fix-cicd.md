---
title: 'Split Builds catalog gitlink from CI/CD execution SHA'
type: 'bugfix'
created: '2026-08-16'
status: 'done'
baseline_commit: '726cf20190429e1953e064b59ef8d23203029fa4'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release [run 31933295965](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31933295965/job/95131275213) failed `verify-source` because FrontComposer requires `references/Hexalith.Builds` gitlink (`58987900cff1e1f67c7f66966023789a104bc349`, audit-only) to equal approved execution SHA (`3f0e3595be693fce56a37648c0bd0f89390f5fd3`). Builds and sibling modules already allow those identities to differ; this extra equality fails on every catalog-only Builds bump. Dirty Builds also has unpublished EventStore `3.95.0` and Memories `2.21.3` catalog/audit edits.

**Approach:** Keep CI/Release execution pins on the last reviewed workflow commit. Drop gitlink==execution. After a human Builds catalog commit, advance only the FrontComposer gitlink so restore inherits the new versions.

## Boundaries & Constraints

**Always:** Keep `uses:@` == `builds-execution-sha` == `BUILDS_EXECUTION_SHA` == `HEXALITH_BUILDS_EXECUTION_SHA` == prepare/evidence `.hexalith/builds-execution` refs as identical lowercase 40-hex `3f0e3595be693fce56a37648c0bd0f89390f5fd3`. Catalog authority stays in Builds; FrontComposer `Directory.Packages.props` stays version-free. Record catalog gitlink and execution SHA as separate identities in release evidence. Retain historical `evaluator_authorizations`. Update root gitlinks with `git -c submodule.recurse=false submodule update --init`.

**Ask First:** Creating or pushing the Builds commit; moving any execution pin off `3f0e3595…`; advancing EventStore or Memories source gitlinks; changing Chatbot or other families; dropping historical evaluator rows.

**Never:** Re-pin execution coordinates to the catalog gitlink; add FrontComposer-local `PackageVersion` or `Hexalith*Version` overrides; land a dirty uncommitted Builds tree as the gitlink; edit nested submodules; recursive/remote submodule updates; weaken `uses:@` == `builds-execution-sha`; complete `spec-bump-latest-hexalith-nuget-packages-2.md` lockstep pin rewrite.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Catalog-only drift | Gitlink `58987900…` (or later catalog SHA), execution pins all `3f0e3595…` | `release_contract.py builds` and `CiGovernanceTests` pass | N/A |
| Pin mismatch | `uses:@` ≠ `builds-execution-sha` | Fail closed | `ContractError` |
| Catalog bump | Human Builds commit with EventStore `3.95.0` and Memories `2.21.3` | FrontComposer restore inherits those versions; other families unchanged | Builds validators reject unpublished, split, or downgraded families |
| Missing gitlink | `ls-tree` is not mode `160000` | Fail closed | Evidence/contract error; do not compare SHA to execution |

</frozen-after-approval>

## Code Map

- `eng/release_contract.py:320-329,388-407` -- `validate_builds_identity` currently `selected_gitlink != approved`; CLI `builds` still reads gitlink via `git ls-tree`. Drop equality; keep `uses:@` and `builds-execution-sha` == `--approved`. Still require a real `160000` gitlink.
- `tests/eng/test_release_contract.py:149-162` -- `test_builds_identity_rejects_mismatched_workflow_input_or_gitlink`; the `(workflow, "b"*40)` case must become a pass.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:733-807` -- keep all *release* coords equal to `domain-release.yml@`; **rebind** `domain-ci.yml@` and evidence `ref` to that execution SHA, not `buildsGitlinkSha`. Delete `buildsGitlinkSha.ShouldBe(approvedBuildsSha)`.
- `eng/release_evidence.py:802-817,1496-1505` -- `_source_workflow_provenance` rejects gitlink ≠ `builds_execution_sha`; keep gitlink parse (mode `160000`) and load reusable bytes at **execution** SHA; do not require SHA equality. Diagnostic path `1504-1505` same.
- `.github/workflows/release.yml:17,233,286,321,329`, `ci.yml:25`, `release-evidence.yml:233` -- **read-only** execution pins; leave `3f0e3595…`.
- `references/Hexalith.Builds/Props/Directory.Packages.props:8,10,40-52,67-69` and `Tools/package-version-audit.json` -- dirty EventStore `3.94.1`→`3.95.0`, Memories `2.21.1`→`2.21.3` (13+3 rows). Parent gitlink still `58987900…`.
- `Directory.Packages.props:1-14` -- version-free import; do not add versions.
- `references/Hexalith.Builds/.github/workflows/domain-release.md:344-348` -- **read-only** Builds contract: caller gitlink need not equal executed release-tool SHA.
- `_bmad-output/planning-artifacts/architecture.md:244-249` -- FR-24 still requires gitlink == execution; rewrite to split catalog vs execution.
- `tests/README.md:189` -- same third-leg wording.

## Tasks & Acceptance

**Execution:**
- [x] `eng/release_contract.py` -- require a `160000` Builds gitlink and `uses:@` == `builds-execution-sha` == `--approved`; stop requiring gitlink SHA == approved -- this is the failing `verify-source` check.
- [x] `tests/eng/test_release_contract.py` -- rename/retarget the identity test so equal pins with a different gitlink pass, and pin mismatch still fails -- covers the I/O happy path and pin-mismatch row.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- bind CI/evidence Builds refs to the release execution SHA; remove gitlink==execution -- prevents the next catalog bump from failing Governance.
- [x] `eng/release_evidence.py` -- authenticate reusable `domain-release.yml` bytes at the execution SHA; keep gitlink as catalog identity without equality -- production prep must not reintroduce the third leg.
- [x] `_bmad-output/planning-artifacts/architecture.md` and `tests/README.md` -- document catalog gitlink vs execution pin as independent, matching Builds `domain-release.md` -- stop future specs from restoring lockstep.
- [x] HALT for a human-created or human-approved Builds commit of the dirty catalog/audit, then `git -c submodule.recurse=false submodule update --init references/Hexalith.Builds` to that exact SHA -- FrontComposer CI reads the gitlink, not a dirty tree. Working tree gitlink is `7867d8fc7bcc3c906b16f0867f6555d8bec5432d` (already on Builds `origin/main`; no duplicate local commit).
- [x] Confirm root `Directory.Packages.props` and `selected_catalog_required_properties` stay version-free -- a compatible gitlink advance must not grow local mirrors.

**Acceptance Criteria:**
- Given gitlink ≠ `3f0e3595…` and all in-scope execution pins still `3f0e3595…`, when `verify-source` / `release_contract.py builds` / `CiGovernanceTests` run, then they pass.
- Given `uses:@` and `builds-execution-sha` differ, when the same checks run, then they fail closed.
- Given the human Builds catalog commit is the FrontComposer gitlink, when restore runs, then EventStore is `3.95.0`, Memories is `2.21.3`, and no local version override exists.

## Spec Change Log

- 2026-08-16 -- Implementation dropped gitlink==execution in contract, evidence, and Governance; execution pins remain `3f0e3595…`. The FrontComposer catalog gitlink was advanced to `7867d8fc7bcc3c906b16f0867f6555d8bec5432d` (already on Builds `origin/main`); execution pins were not rewritten.

## Design Notes

Builds `domain-release.md` already says the caller gitlink is an independent development dependency. Tenants/Parties/Commons/EventStore already run with gitlink ≠ execution. FrontComposer `spec-actions-29801109766-fix-cicd.md` removed the third leg; later lockstep specs put it back. Do not auto-move `uses:@` from gitlink — GitHub requires a static SHA literal.

GitHub reusable `uses:` cannot interpolate the gitlink. Execution pins move only when Builds workflow/action bytes are intentionally re-reviewed (Ask First).

## Verification

**Commands:**
- `python3 -m unittest tests/eng/test_release_contract.py tests/eng/test_release_evidence_v2.py -v` -- expected: identity tests pass with diverged gitlink; pin mismatch still fails.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate|FullyQualifiedName~CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds"` -- expected: green.
- `python3 eng/release_contract.py builds --root . --commit "$(git rev-parse HEAD)" --approved 3f0e3595be693fce56a37648c0bd0f89390f5fd3` -- expected: pass while gitlink is not that SHA.
- After the human Builds commit and gitlink advance, from `references/Hexalith.Builds`: `pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1`, `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1`, `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` -- expected: EventStore `3.95.0` / Memories `2.21.3` aligned; downgrade still rejected.
- `DiffEngine_Disabled=true dotnet restore Hexalith.FrontComposer.slnx` then `DiffEngine_Disabled=true dotnet build Hexalith.FrontComposer.slnx --configuration Release` -- expected: restore uses the new family versions; warning-free.

## Suggested Review Order

**Contract split**

- Drop gitlink==execution; keep `uses:@` == `builds-execution-sha` == approved.
  [`release_contract.py:320`](../../eng/release_contract.py#L320)

- CLI still requires a real `160000` gitlink, then validates pins only.
  [`release_contract.py:400`](../../eng/release_contract.py#L400)

**Evidence provenance**

- Parse catalog gitlink as `160000` + 40-hex; load reusable bytes at execution SHA.
  [`release_evidence.py:809`](../../eng/release_evidence.py#L809)

- Live verify-manifest uses the same split, so catalog-ahead gitlinks stay diagnostic-free.
  [`release_evidence.py:1504`](../../eng/release_evidence.py#L1504)

**Governance**

- Bind CI/evidence Builds refs to the release execution SHA, not the gitlink.
  [`CiGovernanceTests.cs:761`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L761)

**Catalog gitlink**

- Inherit EventStore `3.95.0` / Memories `2.21.3` from Builds `7867d8fc` without moving pins.
  [`Directory.Packages.props:8`](../../references/Hexalith.Builds/Props/Directory.Packages.props#L8)

**Docs**

- FR-24 now treats catalog gitlink as independent of execution.
  [`architecture.md:246`](../planning-artifacts/architecture.md#L246)

- Operator runbook no longer requires gitlink == `3f0e3595…`.
  [`deployment-guide.md:5`](../project-docs/deployment-guide.md#L5)

**Tests**

- Diverged gitlink passes; pin mismatch still fails closed.
  [`test_release_contract.py:149`](../../tests/eng/test_release_contract.py#L149)

- Live v3 verify-manifest covers catalog gitlink ahead of execution SHA.
  [`test_release_evidence_v2.py:365`](../../tests/eng/test_release_evidence_v2.py#L365)
