---
title: 'Align production release with the Tenants exact-source model'
type: 'refactor'
created: '2026-08-04'
status: 'complete'
review_loop_iteration: 1
baseline_commit: 'd5591583cd6671b25875d511870955cde10929ae'
context:
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/project-docs/deployment-guide.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer auto-chains Release from CI, is frozen behind a repository variable, has stale catalog governance, no Builds package manifest, and cannot prove that an approved release published the exact dispatched source. Its stricter signing/evidence path must remain effective while the obsolete AD-16 handoff design is retired.

**Approach:** Adopt Tenants' manual, current-green-main preflight and pinned production reusable release, with a production-protected pack-once preparation handoff for FrontComposer's signed evidence-bearing bytes. Authenticate exact CI/release API data, publish only the sealed candidate, verify the resulting non-draft tag and bytes, and represent no-release or partial outcomes honestly.

## Boundaries & Constraints

**Always:** Use `workflow_dispatch`, literal `release-production` concurrency, current `refs/heads/main`, one unambiguous successful completed push-CI run for the dispatched 40-hex SHA, Builds commit `a53166539bf4441d5e33d04281b14c2d59e950c3` for both reusable identity fields, `environment-name: production`, the exact eight-package manifest, `publish-containers: false`, and read-only permissions before the protected jobs. Preserve pack-once signing/timestamping, SBOM, checksums, sealed manifest/readiness, immutable-byte publication, NuGet/GitHub byte comparison, and partial-publication incident evidence. Keep CRLF except LF-only shell files.

**Ask First:** Any real dispatch/publication; any GitHub secret, variable, environment-rule, dependency, submodule, branch, commit, or remote mutation; any need to use a Builds revision other than the selected gitlink.

**Never:** Automatic release triggers, the freeze variable, mutable publication references, parallel publishers, recursive/nested submodule operations, fabricated CI/release handoffs, skipped duplicate pushes, write permissions in unprotected jobs, container publication, or a green "published" disposition without an exact release tag and verified bytes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Publish | current main, exact successful push CI, releasable commits, production approval | sealed eight-package candidate reaches the pinned reusable publisher; a non-draft tag resolves to the dispatched SHA | post-publication byte verification must pass |
| No release | exact green source but no releasable commits | explicit no-release summary; no protected publication claim | verifier records non-published, not success-as-release |
| Rejected source | wrong ref/SHA, stale main, absent/failed/ambiguous CI or malformed API | stop before production | fail closed with actionable diagnostic |
| Drift/partial | manifest count/IDs, Builds identities, candidate hashes, publication set, or tag SHA differ | no publication when detected pre-push; incident evidence after any side effect | workflow fails and retains forensic artifacts |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml` -- manual source gate, protected preparation/reusable publication, and exact-tag verification.
- `.github/workflows/ci.yml` -- blocking dependency governance and honest exact-source proof; remove pending AD-16 fiction.
- `.github/workflows/release-evidence.yml` -- read-only outcome classification and published-byte/incident verification.
- `.releaserc.json`, `eng/release_prepublish.py`, `eng/release_evidence.py`, `eng/dependency_handoff.py` -- no-release planning, prepared-candidate handoff, approval evidence, manifest/provenance, exact-byte publisher.
- `tools/release-packages.json`, `eng/release-package-inventory.json` -- Builds-schema inventory and richer FrontComposer inventory kept in exact parity.
- `eng/dependency-graph-policy.json` -- selected catalog semantic contract.
- `tests/eng/`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/` -- executable negative and architecture contracts.

## Tasks & Acceptance

**Execution:**
- [x] `eng/dependency-graph-policy.json`, `tests/eng/test_dependency_graph.py` -- audit every governed property against the selected catalog, synchronize the EventStore value to `3.90.0`, and mutation-test every governed value so semantic enforcement remains fail-closed.
- [x] `.github/workflows/release.yml`, `tools/release-packages.json` -- implement exact-source dispatch, strict API/schema/count checks, explicit no-release planning, protected signed candidate preparation, pinned reusable publication inputs, and exact non-draft tag/SHA verification.
- [x] `.releaserc.json`, `eng/release_prepublish.py`, `eng/release_evidence.py`, `eng/dependency_handoff.py` -- consume the authenticated prepared candidate without repacking, replace the unfulfilled AD-16 evaluator handoffs with a closed exact-CI-source/candidate proof, record production-environment approval, remove the changelog release commit, and retain manifest, incident, and byte-integrity gates.
- [x] `.github/workflows/ci.yml`, `.github/workflows/release-evidence.yml` -- emit/consume only truthful proof, classify preflight/no-releasable/governed attempts distinctly, and require forensic verification for every attempted or partial publication.
- [x] `tests/eng/`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`, `ReleaseModelGovernanceTests.cs` -- cover stale main, missing/failed/duplicate CI, malformed API, manifest drift, Builds mismatch, missing publication, tag/SHA mismatch, partial state, and the single protected publisher.
- [x] `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/project-docs/deployment-guide.md`, `_bmad-output/project-context.md` -- replace workflow-run/freeze/AD-16/changelog claims with the deployed operator model and list required production environment protection and signing/NuGet/timestamp configuration without changing settings.

**Acceptance Criteria:**
- Given exact current green main and approval, when releasable commits exist, then the pinned reusable job publishes exactly eight sealed NuGet package/symbol pairs and the non-draft release tag peels to the dispatched SHA.
- Given a normal push/PR, stale/invalid dispatch, failed or ambiguous CI, no releasable commits, or inconsistent publication, when workflows finish, then no false publication claim occurs and every failure/no-release/incident disposition is explicit and fail-closed.

## Spec Change Log

- 2026-08-04: Implemented and reviewed the approved operator-controlled production release architecture; all specified tasks completed.

## Design Notes

The selected reusable workflow does not expose FrontComposer signing secrets and validates its package count only for container releases. Candidate preparation therefore occurs in a read-only `production` job that maps the existing environment secrets, computes the same Semantic Release version, packs/signs/seals once, and uploads a run/attempt-bound artifact. The reusable job recomputes the version, downloads exactly that artifact, verifies source/version/manifest hashes and count, then publishes it. This is one publication path; it neither weakens signing nor requires an upstream Builds edit.

## Verification

**Commands:**
- `actionlint` on every changed workflow; `python3 -m unittest` for dependency graph, workflow/provenance, release, and mutation contracts.
- Release-build affected test projects with warnings as errors, then invoke built xUnit v3 assemblies directly with focused class filters; never solution-level `dotnet test`.
- Validate `tools/release-packages.json` against the selected Builds contract and richer inventory; run `git diff --check` and the recursive-submodule prohibition scan.
