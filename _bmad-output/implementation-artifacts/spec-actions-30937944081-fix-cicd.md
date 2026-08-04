---
title: 'Keep release planning read-only'
type: 'bugfix'
created: '2026-08-04'
status: 'done'
review_loop_iteration: 0
baseline_commit: '936913b0989de5c6cd2ec5635353046725682688'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-align-production-release-with-tenants.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release run `30937944081`, job `92088773133`, fails in the unprotected `plan-release` job before computing a version. Semantic Release 25.0.5 performs a remote `git push --dry-run` authorization probe even with `dryRun: true`; the intentionally read-only GitHub Actions token receives HTTP 403, so candidate preparation and publication are skipped.

**Approach:** Preserve the read-only trust boundary and Semantic Release's version-selection behavior by giving the planner an ephemeral local bare Git remote made from the authenticated checkout. The external origin and GitHub token remain unavailable to planning, while the local remote satisfies Semantic Release's mandatory push probe without publication side effects.

## Boundaries & Constraints

**Always:** Keep `plan-release` at `contents: read`; derive the decision from the exact checked-out history and reachable release tags; keep stdout as one machine-readable JSON object and diagnostics on stderr; clean temporary Git data on success and failure; preserve the protected publisher as the only write-capable release path.

**Ask First:** Any dependency/version change, write permission outside the protected publisher, production dispatch/publication, modification of the version-bump rules, or change to the eight-package release contract.

**Never:** Grant `plan-release` `contents: write`; authenticate the planner against GitHub; contact or mutate `origin`; replace Semantic Release's commit analyzer with hand-written version logic; run prepare, publish, tag, changelog, or GitHub plugins while planning.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Releasable source | `main` checkout with a prior `v*` tag and a later `fix`/`feat` commit; origin is inaccessible | Analyze the local mirror and emit `release_required: true` with the Semantic Release version | Any attempted origin access makes the regression test fail |
| No release | Valid checkout containing only non-releasable commits after the last tag | Emit `release_required: false` and `version: null` | Do not enter protected jobs |
| Planner setup failure | Git clone/mirror or Semantic Release fails | Exit nonzero without a valid partial plan | Remove the temporary mirror in `finally` and retain diagnostics on stderr |

</frozen-after-approval>

## Code Map

- `.github/workflows/release.yml` -- read-only planning boundary and release-plan invocation.
- `eng/semantic-release-plan.mjs` -- programmatic Semantic Release planner that currently inherits the external origin.
- `tests/eng/test_release_contract.py` -- CI-authoritative negative contracts for the manual exact-source release boundary.
- `.releaserc.json` -- protected publisher configuration; must remain separate from planning.

## Tasks & Acceptance

**Execution:**
- [x] `eng/semantic-release-plan.mjs` -- create and clean an ephemeral local bare mirror, pass its file URL as `repositoryUrl`, and retain the existing analyzer/JSON contract so Semantic Release never probes GitHub during planning.
- [x] `.github/workflows/release.yml` -- remove the planner's unnecessary `GITHUB_TOKEN` exposure and describe installation without implying write authentication; retain `contents: read`.
- [x] `tests/eng/test_release_contract.py` -- add executable releasable/no-release fixtures with an inaccessible origin, assert temporary cleanup and exact JSON, and statically pin the tokenless read-only job boundary.

**Acceptance Criteria:**
- Given the exact run `30937944081` trust boundary, when release planning executes with no remote write credential, then it computes the next version successfully without contacting or mutating GitHub.
- Given any planner outcome, when the process exits, then no temporary mirror remains and no prepare/publish plugin or external write path has run.

## Spec Change Log

- 2026-08-04: Follow-up runs `30942027011` and `30942016617` confirmed the release planner fix reached `main`, then failed the shared dependency-governance contract because baseline commit `936913b0` advanced `references/Hexalith.Builds` to `824d7ef1` (`HexalithEventStoreVersion=3.91.0`) without synchronizing `frontcomposer-catalog-v1`. Updated the executable compatibility policy from `3.90.0` to the already-selected `3.91.0`; no gitlink or package dependency changed in this follow-up. Per AD-12 delayed activation, the policy-only correction landed separately as `30b4821e`; this graph-neutral evidence commit then exercises the corrected immutable base policy.
- 2026-08-04: Replacement Quality run `30943480472` passed every governance, contract, docs, and accessibility gate, then exposed a stale Def23 legacy-v2 fixture whose package row omitted the v2-only author-signing and timestamp members. Restored `verified` fixture values so the manifest is otherwise valid and the test isolates empty-fingerprint rejection; current v3 production manifests remain unsigned and do not carry these fields.
- 2026-08-04: Replacement run `30944713915` confirmed the fixture compiles but correctly rejected the changed C# identifier inventory. Resealed the governed CA1707 test inventory at its independently reported unchanged count `6236` and new line-sensitive SHA-256 `3e5cdc41d245d1f7bea5b2f25a5fe48fdefc1f24ce6728bf33f49f3bf615e990`.

## Design Notes

Semantic Release's core verifies push authorization before commit analysis even in dry-run mode. A local bare mirror preserves its native branch, tag, and commit analysis while converting that mandatory probe into an isolated local no-op; granting remote write permission would violate the approved unprotected/protected release split.

## Verification

**Commands:**
- `actionlint .github/workflows/release.yml` -- expected: workflow syntax and expressions pass.
- `python3 -m unittest tests/eng/test_release_contract.py` -- expected: planner fixtures and release boundary contracts pass.
- `node eng/semantic-release-plan.mjs` -- expected: one valid plan object on stdout under the current checkout.
- `git diff --check` -- expected: no whitespace errors.

**Results (2026-08-04):**
- `actionlint .github/workflows/release.yml` -- passed (exit 0, no diagnostics).
- `python3 -m unittest -v tests/eng/test_release_contract.py` -- passed: 16 tests in 3.041s; detached-HEAD patch/minor/major/no-release planning, shallow-history rejection, pre- and post-clone cleanup, structured Trace2 evidence, and the tokenless/read-only boundary each ran and reported `ok`.
- `node eng/semantic-release-plan.mjs` -- passed (exit 0); stdout plan was `{"release_required":true,"version":"4.1.0"}` and Semantic Release diagnostics remained on stderr.
- `git diff --check` -- passed (exit 0, no diagnostics).

**Follow-up verification:**

- Run `30942027011`, job `92103414883` artifact `dependency-graph-evidence-30942027011-1` -- identified the stale policy pin: expected EventStore `3.90.0`, selected Builds catalog provided `3.91.0`.
- Run `30942016617`, job `92102569671` -- both failing infrastructure-governance tests reported the same stale policy pin against tracked Builds commit `824d7ef1`.
- `python3 -m unittest -v tests.eng.test_dependency_graph` -- passed: 68 tests in 6.533s.
- Focused `dotnet test` for `CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog` and `PartiesPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` -- passed: 2/2 tests in 4.7082s.
- Exact Def23 test -- passed: 1/1 after rebuilding the Release test assembly.
- Complete `Story12_4_RedPhaseDefTests` class -- passed: 11/11 in 5.370s.
- `python3 -m unittest -v tests.eng.test_release_evidence_v2` -- passed: 8/8 tests in 5.094s.
- Focused analyzer-policy governance plus Def23 fixture run -- passed: 2/2 tests in 17.6445s.

## Suggested Review Order

**Read-only planning boundary**

- Scrub ambient GitHub credentials and reject incomplete histories before analysis.
  [`semantic-release-plan.mjs:12`](../../eng/semantic-release-plan.mjs#L12)

- Mirror the authenticated checkout locally and bind Semantic Release to its file URL.
  [`semantic-release-plan.mjs:34`](../../eng/semantic-release-plan.mjs#L34)

- Preserve primary failures while enforcing temporary-mirror cleanup.
  [`semantic-release-plan.mjs:69`](../../eng/semantic-release-plan.mjs#L69)

- Keep the unprotected workflow job tokenless with read-only contents permission.
  [`release.yml:144`](../../.github/workflows/release.yml#L144)

**Behavioral proof**

- Exercise detached exact-SHA patch and no-release decisions against an inaccessible origin.
  [`test_release_contract.py:236`](../../tests/eng/test_release_contract.py#L236)

- Pin minor, major, and shallow-history behavior to native Semantic Release rules.
  [`test_release_contract.py:252`](../../tests/eng/test_release_contract.py#L252)

- Prove populated mirrors are removed after a post-clone failure.
  [`test_release_contract.py:331`](../../tests/eng/test_release_contract.py#L331)

- Statically seal effective permissions, credential scrubbing, mirror binding, and plugin allowlisting.
  [`test_release_contract.py:375`](../../tests/eng/test_release_contract.py#L375)
