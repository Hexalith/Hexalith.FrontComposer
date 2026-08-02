---
title: 'Align Story 11.17 Shell catalog policy with the selected Builds revision'
type: 'bugfix'
created: '2026-08-01'
status: 'done'
baseline_commit: '628414061366d703e944131f00fc86197ffda718'
review_loop_iteration: 2
context:
  - '_bmad-output/implementation-artifacts/epic-11-context.md'
---

<frozen-after-approval reason="human-owned intent — renegotiated 2026-08-02 by Administrator (bmad-code-review group 4 Decision 2 option 1)">

## Intent

**Problem:** FrontComposer `HEAD` selects Hexalith.Builds commit
`e69891f67578c2f0dec1cd7d7eea113430d31077`, whose catalog advanced six Hexalith
version properties FrontComposer restores through selected `PackageVersion` rows
(notably `HexalithTenantsVersion` `5.3.0` → `5.4.0`), while the FrontComposer
semantic profile still expected the prior catalog values (Tenants at `5.3.0`,
and no pins for the other five consumed version properties). The mismatch makes
catalog Governance facts fail and blocks Story 11.17d promotion.

**Approach:** Align the FrontComposer-owned semantic policy
(`frontcomposer-catalog-v1`) with the already-selected catalog for every version
property FrontComposer actually consumes —
`HexalithCommonsVersion`, `HexalithEventStoreVersion`, `HexalithMemoriesVersion`,
`HexalithPartiesVersion`, `HexalithPolymorphicSerializationsVersion`, and
`HexalithTenantsVersion` — then harden required-property evaluation
(condition-awareness, diagnostics, fail-closed policy shape) and wire its suite
into CI, and rerun the exact focused and promotion lanes. Use the existing
fail-closed Governance facts as the regression coverage; they demonstrably fail
before this policy correction. Exclude with recorded reason:
`HexalithChatbotVersion` (no consumer in this repository) and
`HexalithFrontComposerVersion` (self-version / release-owned).

## Boundaries & Constraints

**Always:** Preserve the committed root dependency graph and all public/runtime behavior; keep the
policy semantic rather than introducing a commit or catalog-fingerprint allowlist; preserve the
unrelated untracked commitlint specification; record exact validation evidence in Story 11.17d;
pin exactly the six consumed version properties named above (not a blanket catalog fingerprint).

**Ask First:** Any dependency/gitlink movement, package-version change outside the selected-catalog
alignment for those six properties, test relaxation, public API/baseline change, or correction
broader than this diagnosed catalog mismatch and its authorized validator/CI hardening.

**Never:** Edit `references/Hexalith.Builds`, initialize nested submodules, change central package
versions, weaken or bypass Governance, modify `PublicAPI.FcTbl.Shipped.txt`, or claim Story 11.17d
promotion if any required lane fails.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Selected catalog matches policy | Root Builds commit exposes the six pinned FrontComposer version properties at their selected values | Dependency validation succeeds and retains all selector diagnostics | N/A |
| Selected catalog drifts again | Any pinned catalog property is absent, duplicated, or differs from policy | Existing Governance validation fails with owner path, exact commit, property, expected value, and actual value | Fail closed; do not promote the story |

</frozen-after-approval>

## Code Map

- `eng/dependency-graph-policy.json` -- authoritative semantic profile containing the stale expectation.
- `eng/dependency_graph.py` -- committed-object validator that compares selected catalog properties to policy.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- two existing current-revision Governance facts that were red on this mismatch.
- `tests/eng/test_dependency_graph.py` -- synthetic semantic-evaluation coverage, including compatible pointer advances and fail-closed mismatches.
- `_bmad-output/implementation-artifacts/epic-11-context.md` -- stale workflow cache regenerated from newer Epic 11 planning sources.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- review deferral for the pre-existing architecture-seed/policy drift.
- `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md` -- story status and exact promotion evidence.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- Story 11.17d sprint state synchronized only after all gates pass.

## Tasks & Acceptance

**Execution:**
- [x] `eng/dependency-graph-policy.json` -- align `frontcomposer-catalog-v1` `selected_catalog_required_properties` for the six FrontComposer-consumed version properties (including `HexalithTenantsVersion` `5.3.0` → `5.4.0`) so policy matches the selected catalog.
- [x] `eng/dependency_graph.py`, `tests/eng/test_dependency_graph.py`, and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- retain the existing regression assertions and add non-redundant required-property match, missing, duplicate, and mismatch coverage with actionable diagnostics.
- [x] `_bmad-output/implementation-artifacts/epic-11-context.md` and `_bmad-output/implementation-artifacts/deferred-work.md` -- correct the stale generated context and defer the pre-existing final architecture-seed drift to its owner.
- [x] `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md` and `_bmad-output/implementation-artifacts/sprint-status.yaml` -- append exact gate evidence and move Story 11.17d to `review` only if every required gate passes.

**Acceptance Criteria:**
- Given the unchanged root gitlink selects Builds `e69891f6...`, when dependency validation evaluates the FrontComposer profile, then it accepts `HexalithTenantsVersion=5.4.0` and validates every required Builds selector.
- Given the policy correction, when focused dependency tests, Shell organization/ownership tests, Release restore/build, broad Shell non-Contract, solution default, Governance, package, and artifact lanes run, then all pass with zero unauthorized baseline or dependency changes.
- Given any required validation failure, when completion is assessed, then Story 11.17d remains `in-progress` with the exact blocker recorded.

## Spec Change Log

- 2026-08-01 (initial): approved bounded remediation — align the FrontComposer semantic
  `HexalithTenantsVersion` expectation with the already-selected Builds catalog and rerun the promotion
  lanes.
- 2026-08-01 (quick-dev adversarial review, scope grew after approval): added required-property
  match/missing/duplicate/mismatch coverage and expected/observed diagnostics to `eng/dependency_graph.py`;
  regenerated `_bmad-output/implementation-artifacts/epic-11-context.md`; replaced the story's
  solution-level test commands with direct per-project runs; recorded the GOV-1 architecture-seed
  deferral. These exceeded the frozen Intent's "align the policy, then rerun the lanes" and were recorded
  only as checked tasks in the non-frozen sections.
- 2026-08-01 (`bmad-code-review`): four adversarial layers over the promotion delta. Under the recorded
  AC5/Never-List scope amendment, `frontcomposer-catalog-v1` was extended from one pinned version
  property to the six FrontComposer consumes; the required-property check became condition-aware
  (canonical `'$(X)' == ''` self-default accepted, other conditions and `Choose` branches rejected);
  `load_policy` now fails closed on an unknown, non-object, or non-string profile shape; and
  `tests/eng/test_dependency_graph.py` was wired into Gate 2b and pinned in `CiGovernanceTests`,
  having previously run in no workflow. Suite 28 → 38 tests.
- 2026-08-02 (`bmad-code-review` group 4): Administrator renegotiated the frozen Intent in-place to
  the six-property consumed-version scope (Decision 2 option 1). Companion remains `done` for its
  bounded remediation; parent Story 11.17d was then returned to `in-progress` after group 2/3/4
  re-reviews with AC6 open at the recorded 102/111 body census.
- 2026-08-02 (`bmad-code-review` chunk C): parent formally re-baselined `baseline_commit` to
  `32db5c3460a4aa0ae6382ae9db36fa42e512ffd3` and closed AC6 at **111/111** against that baseline.
  Parent promotion evidence records default **4,211/4,211** and Governance **375/375**.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/eng -p 'test_dependency_graph.py' -v` -- expected: all dependency-graph unit tests pass.
- `python3 eng/dependency_graph.py --root . validate --commit 628414061366d703e944131f00fc86197ffda718` -- expected: `ok: true` and all 7 selectors validated. **Caveat (2026-08-01 code review):** `load_policy` reads `eng/dependency-graph-policy.json` from the **working tree**, not from the named commit, so this command reproduces `ok: true` only where the policy edit is present. On a clean checkout of `62841406` it fails, which is why the promotion must be measured at a revision that commits the policy.
- `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -class Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests -parallel none` -- expected: all facts pass, including both previously red catalog facts.
- The exact serialized restore/build, direct focused, direct eight-project default/Governance, package, artifact, and `git diff --check` command blocks in `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md` -- expected: 0 build warnings/errors; after the 2026-08-02 AC6 re-baseline completion pass, default **4,211/4,211** and Governance **375/375** (earlier companion measurement at `65e7d8fb` was 4,206/4,206 and 370/370).

## Suggested Review Order

**Semantic catalog contract**

- Start with the single selected-catalog expectation that unblocks the promotion lane.
  [`dependency-graph-policy.json:33`](../../eng/dependency-graph-policy.json#L33)

- Missing and duplicate properties now report expected and observed values fail-closed.
  [`dependency_graph.py:571`](../../eng/dependency_graph.py#L571)

**Regression coverage**

- Four synthetic cases mutation-pin property match, missing, duplicate, and mismatch behavior.
  [`test_dependency_graph.py:495`](../../tests/eng/test_dependency_graph.py#L495) — the earlier `:470`
  citation pointed at the closing line of the pre-existing required-*package* test, not at these cases.
  Condition-awareness cases begin at
  [`test_dependency_graph.py:526`](../../tests/eng/test_dependency_graph.py#L526), and the policy-shape
  fail-closed pins are in `PolicyShapeTests`.

**Planning context and deferral**

- Regenerated orientation now includes materialized children without embedding story-level acceptance detail.
  [`epic-11-context.md:31`](epic-11-context.md#L31)

- Implemented semantic validation is distinguished from still-gated GOV-1 workflow architecture.
  [`epic-11-context.md:80`](epic-11-context.md#L80)

- Pre-existing Closed Policy Seed drift remains explicitly owned by Architecture.
  [`deferred-work.md:1933`](deferred-work.md#L1933)

**Promotion evidence**

- Exact candidate results and unrelated-work classification are reconciled in the story record.
  [`11-17-shell-bundle-split.md:416`](11-17-shell-bundle-split.md#L416)

- Sprint state records the final direct-project validation totals and bounded scope.
  [`sprint-status.yaml:468`](sprint-status.yaml#L468)
