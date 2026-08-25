---
title: 'Story 9.7: Add story-ID and commit-scope evidence'
type: 'chore'
created: '2026-08-25'
status: 'done'
baseline_commit: 'ceae00a4f9788222ed19153acfc05d68d0bc85d1'
story_id: '9.7'
review_loop_iteration: 4
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-retro-2026-08-11.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 9.7's strict evidence gate cannot attribute its own published delivery commit because that self-enforcing commit predates the exact `9.7` subject rule and also contains visible unrelated paths. Rewriting published history is forbidden.

**Approach:** Add one hard-authorized `bootstrap-owned` disposition for the exact Story 9.7 baseline/delivery tuple. It counts only File List paths as story-owned, keeps every unlisted path visible, and preserves ordinary classification and reconciliation semantics while report labels distinguish ownership-contributing listed paths (`owned`) from listed paths in non-owning classifications (`listed-unowned`).

## Boundaries & Constraints

**Always:** Authorize only story `9.7`, baseline `ceae00a4f9788222ed19153acfc05d68d0bc85d1`, and commit `fd04bdd97fbdd4976a0f213e46a316be199fd8a9`. Require the commit to be a non-merge whose sole parent is that baseline, not match `9.7`, and touch both listed guard paths `eng/validate-story-artifacts.py` and `eng/tests/test_validate_story_artifacts.py`. Accept at most one full-SHA `bootstrap-owned` declaration with a non-empty reason. Reconcile only listed paths; report all unlisted paths. Classify `2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06` as `shared` because its release-compatibility work later touched shared Story 9.7 paths.

**Ask First:** Any other bootstrap tuple, reusable authorization source, disposition kind, ownership rule, or commit/status semantic.

**Never:** Rewrite history, auto-detect bootstrap commits, accept a wildcard or movable ref, let story text authorize arbitrary bootstrap ownership, suppress unlisted paths, treat the whole bootstrap commit as story-owned, weaken ordinary unmapped/interleaved failures, or use path-level unrelated declarations as commit exceptions.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Authorized bootstrap | Exact story/baseline/SHA/parent and both listed guard paths | Listed paths reconcile as `bootstrap-owned`; unlisted paths remain reported | Pass |
| Wrong authorization | Any story, baseline, SHA, parent, merge shape, or matching subject differs | No ownership is granted | Fail closed |
| Invalid declaration | Multiple bootstrap rows, stale/short SHA, empty reason, or missing guard path | No ownership is granted | Fail closed |
| Later shared commit | Exact `2dcc43fe...` declaration with `shared` and reason | Commit remains visible but contributes no ownership | Pass |

</frozen-after-approval>

## Code Map

- `eng/validate-story-artifacts.py:306-348,510-549,621-678,726-758,830-1146,1188-1289,1399-1415` -- fail-closed CLI overrides and critical frontmatter scalars, prose-tolerant disposition grammar, explicit legacy bare-diff fallback, exact story-ID and canonical range evidence, hard-bound authorization, listed-path reconciliation, classification-aware labels, and repeated task-section extraction.
- `eng/tests/test_validate_story_artifacts.py:176-288,382-449,1067-1091,1359-1381,1409-1809` -- legacy fallback, duplicate-frontmatter, empty-override, repeated-heading, disposition-prose, trailing-slash, immutable-inventory, availability-guard, clean-clone CLI, canonical-artifact, and report-label regressions.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:141-169` and `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json:98-103` -- isolated blocking CI-pin fact and exact CA1707 identifier-inventory reseal.
- `.agents/skills/bmad-build/step-04-review.md:19-52` and `.claude/skills/bmad-build/step-04-review.md:19-52` -- synchronized reviewer-facing strict gate and one-time exception contract.
- `.agents/skills/bmad-build/spec-template.md:6` -- fail-closed `story_id` placeholder authoring rule.
- `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md:22-30` -- operator contract for commit dispositions and anti-bypass behavior.

## Tasks & Acceptance

**Execution:**
- [x] `eng/validate-story-artifacts.py`, `eng/tests/test_validate_story_artifacts.py`, workflow files, CI pin, and operator checklist -- preserve the delivered exact story-ID, canonical-ref, ancestry, per-commit path, merge, workspace, and File List evidence behavior.
- [x] `eng/validate-story-artifacts.py` -- add the hard-bound `bootstrap-owned` authorization, bind it to the canonical Story 9.7 artifact and immutable bootstrap-owned path set, reconcile only those authorized listed paths, and label listed paths as owned only for ownership-contributing classifications.
- [x] `eng/tests/test_validate_story_artifacts.py` -- prove the exact authorization succeeds and every artifact, tuple, topology, declaration, guard-path, and mutable-File-List deviation fails closed; prove shared/process/unmapped paths never receive an owned label and unlisted paths stay visible.
- [x] `.agents/skills/bmad-build/step-04-review.md` and `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md` -- document the one-time human-authorized recovery and prohibit routine substitution for correct commit attribution.
- [x] `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md` -- record both exact dispositions and refreshed verification evidence.

**Acceptance Criteria:**
- Given the exact authorized historical tuple and canonical Story 9.7 artifact, when strict validation runs to `HEAD`, then `fd04bdd9...` is reported as `story-id=no-match | disposition=bootstrap-owned`, only its listed paths are labeled `owned`, its unlisted paths remain visible as `unowned`, and `2dcc43fe...` is visible as `shared` with listed paths labeled `listed-unowned` rather than contributing ownership.
- Given any authorization or structural deviation, when validation runs, then it fails with actionable evidence and grants no bootstrap ownership.
- Given ordinary matching, unmapped, interleaved, `shared`, or `process` commits, when validation runs, then their existing classification and reconciliation semantics remain unchanged, and report labels remain classification-aware: listed paths are `owned` only for ownership-contributing classifications and `listed-unowned` otherwise.

### Review Findings

- [x] [Review][Patch] Extract the Story 9.7 CI-pin assertions into their own `[Fact]` and reseal the analyzer-policy identifier inventory [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:141]
- [x] [Review][Patch] Renegotiate the frozen Approach and AC 3 to admit classification-aware report labels, and record it in the Spec Change Log [_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:20]
- [x] [Review][Defer] Constrain `shared`/`process` disposition authorization the way review loop 3 constrained `bootstrap-owned` [eng/validate-story-artifacts.py:611] — deferred, pre-existing; they only withhold ownership and never grant it, so mutable story text cannot broaden attribution — tightening is a follow-up story
- [x] [Review][Patch] A documented-unrelated entry written with a trailing slash crashes the validator [eng/validate-story-artifacts.py:360]
- [x] [Review][Patch] The runtime `.claude/skills/bmad-build` mirror was not updated, so the strict gate is inert [.claude/skills/bmad-build/step-04-review.md:20]
- [x] [Review][Patch] The new blocking CI gate is non-hermetic and fails on any dirty working tree [eng/tests/test_validate_story_artifacts.py:1457]
- [x] [Review][Patch] The story-ID matcher's left guard is asymmetric, so version strings match the story [eng/validate-story-artifacts.py:790]
- [x] [Review][Patch] A deferred-work entry added by this story is contradicted by the same commit [_bmad-output/implementation-artifacts/deferred-work.md:2579]
- [x] [Review][Patch] Two paths inside the bootstrap commit are declared nowhere in the spec [_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:120]
- [x] [Review][Patch] Four load-bearing invariants are unpinned — deleting each keeps the suite green [eng/tests/test_validate_story_artifacts.py:1355]
- [x] [Review][Patch] History-coupled bootstrap tests have no availability guard [eng/tests/test_validate_story_artifacts.py:1453]
- [x] [Review][Patch] Verification and Test Evidence omit the Governance C# lane and record a stale candidate [_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:139]
- [x] [Review][Patch] The `story_id` template placeholder is an undocumented fail-closed trap [.agents/skills/bmad-build/spec-template.md:6]
- [x] [Review][Patch] A second recognized task heading is silently skipped, and the dispositions parser rejects prose [eng/validate-story-artifacts.py:1360]
- [x] [Review][Patch] `last_updated` drifted from the format the sprint tooling emits [_bmad-output/implementation-artifacts/sprint-status.yaml:44]
- [x] [Review][Patch] Code Map line anchors no longer resolve to the symbols they name [_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:47]
- [x] [Review][Defer] Merge commits contribute no path evidence [eng/validate-story-artifacts.py:953] — deferred, pre-existing
- [x] [Review][Defer] `--exclude` is applied to workspace paths but not committed paths [eng/validate-story-artifacts.py:1149] — deferred, pre-existing
- [x] [Review][Defer] An explicit `story_id` is never cross-checked against title, H1, or filename [eng/validate-story-artifacts.py:546] — deferred, pre-existing
- [x] [Review][Defer] Non-UTF-8 commit subjects raise a traceback instead of a validation failure [eng/validate-story-artifacts.py:911] — deferred, pre-existing
- [x] [Review][Defer] The baseline ref is not re-resolved after collection although the candidate is [eng/validate-story-artifacts.py:1078] — deferred, pre-existing
- [x] [Review][Defer] The bootstrap exception has no retirement path and pins the suite to this repository's history [eng/validate-story-artifacts.py:217] — deferred, pre-existing
- [x] [Review][Defer] Gate 2a pack baseline properties are not mirrored on the Gate 1 restore [.github/workflows/quality.yml:71] — deferred, pre-existing
- [x] [Review][Defer] `step-03-implement.md` never tells the implementer to put the story ID in commit subjects [.agents/skills/bmad-build/step-03-implement.md:1] — deferred, pre-existing
- [x] [Review][Patch] Legacy best-effort discovery passes `NO_VCS` or an invalid baseline to `git diff` and silently omits unstaged tracked changes [eng/validate-story-artifacts.py:726]
- [x] [Review][Patch] Duplicate `story_id` or `baseline_commit` frontmatter scalars silently replace the earlier security-critical value [eng/validate-story-artifacts.py:657]
- [x] [Review][Patch] An explicitly empty `--base` override silently falls back to story frontmatter [eng/validate-story-artifacts.py:312]

## Spec Change Log

- 2026-08-25, review loop 1 -- The strict gate exposed a self-bootstrap contradiction: the commit introducing exact Story 9.7 attribution could not satisfy a rule that did not yet exist, and published history cannot be rewritten. With explicit human approval, the frozen contract now permits one exact full-SHA `bootstrap-owned` tuple and the later release-compatibility commit is declared `shared`. This avoids the known-bad alternatives of history rewriting, generic exemptions, hidden interleaving, or whole-commit ownership. KEEP: canonical SHA/ancestry checks, exact ID boundaries, per-path visibility, separate workspace evidence, ordinary disposition semantics, existing CI enforcement, and the delivered validator behavior outside this exception.
- 2026-08-25, review loop 2 -- Adversarial review found the non-frozen Design Notes called every File List member `owned`, contradicting the approved rule that `shared` and `process` commits contribute no ownership. The report contract now labels a listed path `owned` only for `owned`, `interleaved`, or `bootstrap-owned` classifications and uses `listed-unowned` for listed paths in every non-owning classification. The authorization must also bind the canonical Story 9.7 artifact so a copied fixture cannot reuse it. This avoids misleading staging evidence and reusable recovery artifacts. KEEP: the exact code-bound story/baseline/SHA/sole-parent tuple, both listed and touched guard paths, fail-closed declaration handling, listed-only reconciliation, unlisted-path visibility, pure authorization tests, historical CLI integration, and operator anti-copy guidance.
- 2026-08-25, review loop 3 -- Adversarial review proved that binding `bootstrap-owned` to the canonical artifact still let later story text broaden historical ownership by adding either currently unowned bootstrap-commit path to the mutable File List. The non-frozen design now requires the exact twelve-path bootstrap-owned set to be code-bound and requires any changed intersection between the bootstrap commit and the File List to fail closed. This avoids converting the story artifact into an authorization source while still allowing unrelated future File List entries that the bootstrap commit never touched. KEEP: canonical artifact binding; the exact story/baseline/SHA/sole-parent/subject tuple; both guard paths; fail-closed malformed and multiple declarations; classification-aware `owned`, `listed-unowned`, and `unowned` labels; shared/process non-ownership; full per-path visibility; pure authorization tests; historical CLI integration; and operator anti-copy guidance.
- 2026-08-25, review loop 4 -- Human-approved review reconciliation amended the frozen Approach and third acceptance criterion to state the classification-aware report-label contract already introduced in loop 2. This avoids the contradictory reading that truthful `listed-unowned` labels violate an otherwise unchanged classification and reconciliation contract. The patch also makes the gate hermetic, mirrors it into the runtime Claude skill, closes parser and story-ID boundary gaps, pins the immutable authorization inventories, and records both intentionally unowned bootstrap paths. KEEP: the exact authorization tuple and twelve-path intersection; listed-only ownership; all-path visibility; ordinary classification and reconciliation behavior; the blocking CI command; fail-closed malformed declarations; and the loop-3 anti-broadening proof.
- 2026-08-25, review patch follow-up -- Review found three fail-open legacy/CLI parsing seams outside the frozen authorization contract: unusable baselines could hide unstaged tracked paths, duplicate critical frontmatter scalars overwrote their predecessors, and an empty explicit base was treated as absent. The patch now resolves a usable legacy baseline or reports and uses a bare-diff workspace fallback, rejects duplicate `story_id`/`baseline_commit` scalars, and rejects an explicitly empty `--base`. KEEP: strict candidate evidence, the approved bootstrap tuple and frozen block, ordinary valid-baseline discovery, staged/untracked discovery, and existing story-ID precedence.

## Commit Scope Dispositions

- `fd04bdd97fbdd4976a0f213e46a316be199fd8a9` | `bootstrap-owned` | self-enforcing Story 9.7 delivery predates its exact commit-ID gate; human-approved one-time recovery bound to the immutable baseline and guard paths
- `2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06` | `shared` | later release-compatibility work changed shared CI, governance, and deferred-work paths without belonging to Story 9.7
- `c4df029050cb241f74cafd04a01f7718eae1ec0c` | `shared` | earlier Fluent UI and test-stability work resealed the shared analyzer-policy inventory without belonging to Story 9.7

## Design Notes

`bootstrap-owned` is a code-authorized historical recovery, not a general third disposition. The story declaration and canonical artifact path must match the immutable authorization tuple; copying the text, editing frontmatter, or moving a baseline cannot create authority.

The bootstrap-owned set is also immutable and code-bound. The intersection of the bootstrap commit's touched paths and the canonical story File List must be exactly these twelve paths; adding either historically unowned path or removing an authorized path invalidates the declaration and grants no bootstrap ownership:

- `.agents/skills/bmad-build/spec-template.md`
- `.agents/skills/bmad-build/step-02-plan.md`
- `.agents/skills/bmad-build/step-04-review.md`
- `.agents/skills/bmad-build/step-05-present.md`
- `.github/workflows/quality.yml`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md`
- `eng/tests/test_validate_story_artifacts.py`
- `eng/validate-story-artifacts.py`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`

Future File List entries that the bootstrap commit did not touch do not change this intersection and therefore do not broaden or invalidate the historical authorization.

The bootstrap commit's two touched paths outside the immutable authorized set are `_bmad-output/implementation-artifacts/spec-actions-29316660112-fix-cicd.md` and `references/Hexalith.Tenants`. They are intentionally absent from the Story 9.7 File List, remain visible as `unowned`, and never contribute to reconciliation.

Path labels describe ownership, not mere File List membership. For `owned`, `interleaved`, and `bootstrap-owned` commits, listed paths are `owned` and other paths are `unowned`. For `shared`, `process`, `unmapped`, and `unrelated` commits, listed paths are `listed-unowned` and other paths are `unowned`. Reconciliation still admits only listed paths from ownership-contributing classifications.

## Verification

**Commands:**
- `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py` -- both modules compile.
- `python3 -m unittest eng.tests.test_validate_story_artifacts` -- all mandatory fixtures pass with no new skip.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore` -- the changed Governance project builds with zero warnings and errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.StoryArtifactValidatorGate_IsBlockingAndExact` and the corresponding `AnalyzerPolicyGovernanceTests.AnalyzerPolicy_IdentifierInventory_MatchesSeal` method -- the isolated CI pin and its exact analyzer inventory both pass.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests -parallel none` -- run the complete affected C# Governance class and record any unrelated repository-policy blocker separately from the focused Story 9.7 fact.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md --candidate HEAD` -- exact bootstrap/shared report passes and keeps unowned paths visible.
- `git diff --check` -- changed files are whitespace-clean.

## Test Evidence

- Pre-change baseline: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 72 tests with 2 existing optional `ReviewVerifierTests` skips.
- Iteration-1 known-bad evidence: the focused suite passed 76 tests with the same 2 optional skips, and the live strict report passed, but review proved its report still mislabeled listed paths in the `shared` commit as `owned` and allowed a copied fixture path to reuse the hard-coded tuple. Iteration 2 must replace this evidence rather than treating it as acceptance proof.
- Iteration-2 known-bad evidence: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 76 tests with the same 2 optional `ReviewVerifierTests` skips, and the live strict report passed with truthful classification labels. Review nevertheless proved that mutating the canonical File List could broaden bootstrap ownership, so this evidence cannot satisfy iteration 3.
- Iteration-3 evidence: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 83 tests with the same 2 optional `ReviewVerifierTests` skips. The suite includes the exact historical CLI report, copied-artifact rejection, pure tuple/topology/declaration/guard checks, independent declared-versus-resolved baseline deviations that prove fail-closed `unmapped`/`listed-unowned` behavior with no ownership, a canonical-metadata regression that adds a historically unowned bootstrap path to the File List and likewise proves no ownership, and an end-to-end regression proving checked tasks under the exact `## Tasks & Acceptance` heading are extracted and evidence-validated. `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md --candidate HEAD` passed against canonical candidate `f35523436db525197dcc223ddbe8aa0db97bbdf3`; it reported the bootstrap commit's twelve authorized paths as `owned`, both other paths as `unowned`, and the shared commit's three listed paths as `listed-unowned` while preserving every other path as `unowned`.
- Iteration-4 evidence: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 89 tests with the same 2 optional `ReviewVerifierTests` skips and no history skip in this full checkout. New regressions pin the four closed inventories, repeated task headings, disposition prose, normalized trailing-slash classifications, version-token boundaries, clean-clone historical execution, and history availability guards. The Shell.Tests Release project built with 0 warnings / 0 errors; `StoryArtifactValidatorGate_IsBlockingAndExact` and `AnalyzerPolicy_IdentifierInventory_MatchesSeal` each passed 1/1 after the exact 6,997-token hash reseal. The full affected `CiGovernanceTests` class ran 67 tests: 66 passed and the pre-existing `ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` failed because the committed Builds gitlink is `22a578b576a515d2af214fe81859447fffc97981` while unchanged release coordinates pin `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`; Story 9.7 changes neither surface. The live strict gate passed against canonical candidate `a229be7e7e8ea8af43c537a9ed24f13175cc1416`: the bootstrap commit's twelve authorized paths remained `owned`, its two now-declared outside paths remained `unowned`, the release-compatibility paths remained `listed-unowned`, and the earlier analyzer-ledger commit remained visible as an explicitly reasoned `shared` commit. The only unrelated workspace state was the pre-existing maintainer-owned `references/Hexalith.EventStore` pointer, which remained visible and unclaimed.
- Iteration-4 review-patch evidence: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py` passed, the three focused regressions passed 3/3, and `python3 -m unittest eng.tests.test_validate_story_artifacts` passed 92 tests with the same 2 optional `ReviewVerifierTests` skips. The added subcases prove missing, `NO_VCS`, and unresolvable legacy baselines all use an explicit bare-diff fallback that rediscovers unstaged `README.md`; duplicate `story_id` and duplicate `baseline_commit` each fail closed; and an explicitly empty `--base` is rejected without a traceback. The strict Story 9.7 gate passed against canonical candidate `a229be7e7e8ea8af43c537a9ed24f13175cc1416`, preserving the exact bootstrap/shared labels and the visible documented unrelated `references/Hexalith.EventStore` workspace pointer.
- Matrix audit: `python3 -m unittest -v eng.tests.test_validate_story_artifacts.BootstrapOwnedAuthorizationTests` passed 14/14 with no skips. The exact tuple and canonical historical CLI tests cover the authorized-bootstrap and later-shared rows; the authorization-dimension and immutable-intersection tests cover wrong authorization; and the duplicate-declaration parser test plus the authorization-dimension cases cover invalid declarations including stale/short SHA, empty reason, missing guard paths, topology, and File List deviations.

## File List

- `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md`
- `eng/validate-story-artifacts.py`
- `eng/tests/test_validate_story_artifacts.py`
- `.agents/skills/bmad-build/spec-template.md`
- `.agents/skills/bmad-build/step-02-plan.md`
- `.agents/skills/bmad-build/step-04-review.md`
- `.agents/skills/bmad-build/step-05-present.md`
- `.github/workflows/quality.yml`
- `.claude/skills/bmad-build/step-04-review.md`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`

## Documented Unrelated Workspace State

- `references/Hexalith.EventStore` - pre-existing maintainer-owned submodule pointer change; Story 9.7 neither modifies nor claims it.

## Suggested Review Order

**Exact bootstrap boundary**

- Start with the immutable authorization surface defining exactly what historical recovery may own.
  [`validate-story-artifacts.py:231`](../../eng/validate-story-artifacts.py#L231)

- Verify every declared and resolved tuple dimension fails closed.
  [`validate-story-artifacts.py:843`](../../eng/validate-story-artifacts.py#L843)

- Confirm canonical range evidence authorizes before classifying every non-merge commit.
  [`validate-story-artifacts.py:925`](../../eng/validate-story-artifacts.py#L925)

**Parser and fallback hardening**

- Reject empty base overrides before they can silently select story frontmatter.
  [`validate-story-artifacts.py:324`](../../eng/validate-story-artifacts.py#L324)

- Reject duplicate critical frontmatter scalars and prevent downstream use.
  [`validate-story-artifacts.py:657`](../../eng/validate-story-artifacts.py#L657)

- Recover legacy discovery through explicit bare-diff fallback for unusable baselines.
  [`validate-story-artifacts.py:726`](../../eng/validate-story-artifacts.py#L726)

- Preserve exact story-token boundaries across dotted versions and embedded identifiers.
  [`validate-story-artifacts.py:830`](../../eng/validate-story-artifacts.py#L830)

**Ownership and reporting semantics**

- Reconciliation admits only ownership-contributing commit classifications.
  [`validate-story-artifacts.py:1188`](../../eng/validate-story-artifacts.py#L1188)

- Report labels distinguish ownership from mere File List membership.
  [`validate-story-artifacts.py:1214`](../../eng/validate-story-artifacts.py#L1214)

- Every recognized task section participates in checked-task evidence validation.
  [`validate-story-artifacts.py:1399`](../../eng/validate-story-artifacts.py#L1399)

**Workflow enforcement**

- Keep runtime Claude review behavior synchronized with the blocking strict gate.
  [`step-04-review.md:19`](../../.claude/skills/bmad-build/step-04-review.md#L19)

- Make unresolved story-ID template placeholders explicitly fail closed.
  [`spec-template.md:6`](../../.agents/skills/bmad-build/spec-template.md#L6)

- Pin the exact blocking CI command independently from release-policy assertions.
  [`CiGovernanceTests.cs:141`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L141)

**Regression proof**

- Prove all unusable baseline forms still discover unstaged tracked work.
  [`test_validate_story_artifacts.py:176`](../../eng/tests/test_validate_story_artifacts.py#L176)

- Prove duplicate critical keys and empty base overrides fail closed.
  [`test_validate_story_artifacts.py:221`](../../eng/tests/test_validate_story_artifacts.py#L221)

- Exercise the exact historical report with truthful owned and unowned labels.
  [`test_validate_story_artifacts.py:1583`](../../eng/tests/test_validate_story_artifacts.py#L1583)

- Prove mutable canonical metadata cannot broaden historical bootstrap ownership.
  [`test_validate_story_artifacts.py:1729`](../../eng/tests/test_validate_story_artifacts.py#L1729)
