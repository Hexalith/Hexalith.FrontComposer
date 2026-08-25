---
title: 'Story 9.7: Add story-ID and commit-scope evidence'
type: 'chore'
created: '2026-08-25'
status: 'done'
baseline_commit: 'ceae00a4f9788222ed19153acfc05d68d0bc85d1'
story_id: '9.7'
review_loop_iteration: 5
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-retro-2026-08-11.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 9.7's strict evidence gate cannot attribute its own published delivery commit because that self-enforcing commit predates the exact `9.7` subject rule and also contains visible unrelated paths. Rewriting published history is forbidden.

**Approach:** Add one hard-authorized `bootstrap-owned` disposition for the exact Story 9.7 baseline/delivery tuple. It counts only the code-bound File List path inventory as story-owned, keeps every unlisted path visible, and preserves ordinary classification and reconciliation semantics while report labels distinguish ownership-contributing listed paths (`owned`) from listed paths in non-owning classifications (`listed-unowned`).

## Boundaries & Constraints

**Always:** Authorize only story `9.7`, baseline `ceae00a4f9788222ed19153acfc05d68d0bc85d1`, and commit `fd04bdd97fbdd4976a0f213e46a316be199fd8a9`. Require the commit to be a non-merge whose sole parent is that baseline, not match `9.7`, and touch both listed guard paths `eng/validate-story-artifacts.py` and `eng/tests/test_validate_story_artifacts.py`. Accept at most one full-SHA `bootstrap-owned` declaration with a non-empty reason. Reconcile only the exact code-bound listed path inventory; report all unlisted paths. Permit multiple exactly declared, in-range full-SHA `shared` or `process` rows with non-empty reasons: they may classify an otherwise `unmapped` commit but must never suppress `interleaved`. Classify `2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06` and `c4df029050cb241f74cafd04a01f7718eae1ec0c` as `shared`.

**Ask First:** Any other bootstrap tuple, reusable authorization source, disposition kind, ownership rule, or commit/status semantic.

**Never:** Rewrite history, auto-detect bootstrap commits, accept a wildcard or movable ref, let story text authorize arbitrary bootstrap ownership, suppress unlisted paths, treat the whole bootstrap commit as story-owned, weaken ordinary unmapped/interleaved failures, or use path-level unrelated declarations as commit exceptions.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Authorized bootstrap | Exact story/baseline/SHA/parent and both listed guard paths | Listed paths reconcile as `bootstrap-owned`; unlisted paths remain reported | Pass |
| Wrong authorization | Any story, baseline, SHA, parent, merge shape, or matching subject differs | No ownership is granted | Fail closed |
| Invalid declaration | Multiple bootstrap rows, stale/short SHA, empty reason, or missing guard path | No ownership is granted | Fail closed |
| Declared shared commits | Exact in-range full-SHA declarations, including `2dcc43fe...` and `c4df0290...`, with `shared` and reasons | Commits remain visible but contribute no ownership; an interleaved story commit still fails | Pass |

</frozen-after-approval>

## Code Map

- `eng/validate-story-artifacts.py:311-367,515-714,762-799,852-1190,1234-1361,1467-1511` -- fail-closed CLI overrides and all duplicate frontmatter scalars, inline-comment parsing, prose-tolerant disposition grammar, explicit legacy bare-diff fallback, exact story-ID and canonical range evidence, hard-bound authorization, matching-first classification, control-safe report values, listed-path reconciliation, classification-aware labels, and nested task-section extraction with an explicit review-finding exclusion.
- `eng/tests/test_validate_story_artifacts.py:104-141,230-368,478-645,1239-1574,1823-2271` -- workflow mirror and documentation contracts, legacy fallback, duplicate-frontmatter, empty-override, nested-task/review-heading boundaries, disposition-prose, matching classification, terminal-control rendering, strict invocation, interleaving, merge, immutable-inventory, required-history, clean-clone CLI, canonical-artifact, and report-label regressions.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:141-169` and `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json:98-103` -- isolated blocking CI-pin fact and exact CA1707 identifier-inventory reseal.
- `.agents/skills/bmad-build/step-04-review.md:19-52` and `.claude/skills/bmad-build/step-04-review.md:19-52` -- synchronized reviewer-facing strict gate and one-time exception contract.
- `.agents/skills/bmad-build/spec-template.md:6,74-79` and `docs/reference/story-artifact-validation.md:13-62` -- fail-closed `story_id` authoring, reusable disposition scaffold, and the operator-facing classification/path-label grammar.
- `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md:22-30` -- operator contract for commit dispositions and anti-bypass behavior.

## Tasks & Acceptance

**Execution:**
- [x] `eng/validate-story-artifacts.py`, `eng/tests/test_validate_story_artifacts.py`, workflow files, CI pin, and operator checklist -- preserve the delivered exact story-ID, canonical-ref, ancestry, per-commit path, merge, workspace, and File List evidence behavior.
- [x] `eng/validate-story-artifacts.py` -- add the hard-bound `bootstrap-owned` authorization, bind it to the canonical Story 9.7 artifact and immutable bootstrap-owned path set, reconcile only those authorized listed paths, and label listed paths as owned only for ownership-contributing classifications.
- [x] `eng/tests/test_validate_story_artifacts.py` -- prove the exact authorization succeeds and every artifact, tuple, topology, declaration, guard-path, and mutable-File-List deviation fails closed; prove shared/process/unmapped paths never receive an owned label and unlisted paths stay visible.
- [x] `.agents/skills/bmad-build/step-04-review.md` and `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md` -- document the one-time human-authorized recovery and prohibit routine substitution for correct commit attribution.
- [x] `_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md` -- record all exact dispositions and refreshed verification evidence.

**Acceptance Criteria:**
- Given the exact authorized historical tuple and canonical Story 9.7 artifact, when strict validation runs to `HEAD`, then `fd04bdd9...` is reported as `story-id=no-match | disposition=bootstrap-owned`, only its listed paths are labeled `owned`, its unlisted paths remain visible as `unowned`, and `2dcc43fe...` is visible as `shared` with listed paths labeled `listed-unowned` rather than contributing ownership.
- Given any authorization or structural deviation, when validation runs, then it fails with actionable evidence and grants no bootstrap ownership.
- Given ordinary matching, unmapped, interleaved, `shared`, or `process` commits, when validation runs, then their existing classification and reconciliation semantics remain unchanged, and report labels remain classification-aware: listed paths are `owned` only for ownership-contributing classifications and `listed-unowned` otherwise.

### Review Findings

- [x] [Review][Patch] Extract the Story 9.7 CI-pin assertions into their own `[Fact]` and reseal the analyzer-policy identifier inventory [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:141]
- [x] [Review][Patch] Renegotiate the frozen Approach and AC 3 to admit classification-aware report labels, and record it in the Spec Change Log [_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:20]
- [x] [Review][Defer] Constrain `shared`/`process` disposition authorization the way review loop 3 constrained `bootstrap-owned` [eng/validate-story-artifacts.py:631] — deferred, pre-existing; they only withhold ownership and never grant it, so mutable story text cannot broaden attribution — tightening is a follow-up story
- [x] [Review][Patch] A documented-unrelated entry written with a trailing slash crashes the validator [eng/validate-story-artifacts.py:731]
- [x] [Review][Patch] The runtime `.claude/skills/bmad-build` mirror was not updated, so the strict gate is inert [.claude/skills/bmad-build/step-04-review.md:20]
- [x] [Review][Patch] The new blocking CI gate is non-hermetic and fails on any dirty working tree [eng/tests/test_validate_story_artifacts.py:2028]
- [x] [Review][Patch] The story-ID matcher's left guard is asymmetric, so version strings match the story [eng/validate-story-artifacts.py:866]
- [x] [Review][Patch] A deferred-work entry added by this story is contradicted by the same commit [_bmad-output/implementation-artifacts/deferred-work.md:2579]
- [x] [Review][Patch] Two paths inside the bootstrap commit are declared nowhere in the spec [_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:138]
- [x] [Review][Patch] Four load-bearing invariants are unpinned — deleting each keeps the suite green [eng/tests/test_validate_story_artifacts.py:1826]
- [x] [Review][Patch] History-coupled bootstrap tests have no availability guard [eng/tests/test_validate_story_artifacts.py:1832]
- [x] [Review][Patch] Verification and Test Evidence omit the Governance C# lane and record a stale candidate [_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:160]
- [x] [Review][Patch] The `story_id` template placeholder is an undocumented fail-closed trap [.agents/skills/bmad-build/spec-template.md:6]
- [x] [Review][Patch] A second recognized task heading is silently skipped, and the dispositions parser rejects prose [eng/validate-story-artifacts.py:1467]
- [x] [Review][Patch] `last_updated` drifted from the format the sprint tooling emits [_bmad-output/implementation-artifacts/sprint-status.yaml:44]
- [x] [Review][Patch] Code Map line anchors no longer resolve to the symbols they name [_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:42]
- [x] [Review][Defer] Merge commits contribute no path evidence [eng/validate-story-artifacts.py:1049] — deferred, pre-existing
- [x] [Review][Defer] `--exclude` is applied to workspace paths but not committed paths [eng/validate-story-artifacts.py:1250] — deferred, pre-existing
- [x] [Review][Defer] An explicit `story_id` is never cross-checked against title, H1, or filename [eng/validate-story-artifacts.py:562] — deferred, pre-existing
- [x] [Review][Defer] Non-UTF-8 commit subjects raise a traceback instead of a validation failure [eng/validate-story-artifacts.py:1004] — deferred, pre-existing
- [x] [Review][Defer] The baseline ref is not re-resolved after collection although the candidate is [eng/validate-story-artifacts.py:1171] — deferred, pre-existing
- [x] [Review][Defer] The bootstrap exception has no retirement path and pins the suite to this repository's history [eng/validate-story-artifacts.py:219] — deferred, pre-existing
- [x] [Review][Defer] Gate 2a pack baseline properties are not mirrored on the Gate 1 restore [.github/workflows/quality.yml:71] — deferred, pre-existing
- [x] [Review][Defer] `step-03-implement.md` never tells the implementer to put the story ID in commit subjects [.agents/skills/bmad-build/step-03-implement.md:1] — deferred, pre-existing
- [x] [Review][Patch] Legacy best-effort discovery passes `NO_VCS` or an invalid baseline to `git diff` and silently omits unstaged tracked changes [eng/validate-story-artifacts.py:762]
- [x] [Review][Patch] Duplicate `story_id` or `baseline_commit` frontmatter scalars silently replace the earlier security-critical value [eng/validate-story-artifacts.py:667]
- [x] [Review][Patch] An explicitly empty `--base` override silently falls back to story frontmatter [eng/validate-story-artifacts.py:319]

- [x] [Review][Patch] RESOLVED 2026-08-25 (loop 5): add `references/Hexalith.EventStore` and `references/Hexalith.Tenants` to the File List so `5817f191` classifies `owned` instead of `interleaved`, and delete the now-false `## Documented Unrelated Workspace State` entry for `references/Hexalith.EventStore` (adding it to the File List claims it). Record the reversal in the loop-5 Spec Change Log entry, including that this absorbs a pre-existing workspace change, which the Epic 9 constraint discourages -- accepted by human decision. Original finding: Story 9.7's own strict gate exits 1 at HEAD because `5817f191` commits declared-unrelated submodule pointers under a `fix(9.7):` subject — `references/Hexalith.EventStore` is declared in `## Documented Unrelated Workspace State` as "Story 9.7 neither modifies nor claims it", `references/Hexalith.Tenants` is declared nowhere, and the authored rule in `step-05-present.md` forbids staging a Documented-Unrelated path. The in-range merge of PR #97 means the history is published, so the frozen Never blocks amending. Human call needed between: add both paths to the File List (contradicting the unrelated declaration), declare a disposition on `5817f191` (which relies on the bypass below), re-land the submodule bump under a non-9.7 subject, or renegotiate the frozen block. [_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:174]
- [x] [Review][Patch] RESOLVED 2026-08-25 (loop 5): close the bypass with an asymmetry rather than the "touches no File List path" rule, which would invalidate the story's own `2dcc43fe` row (it touches three listed paths). A `shared`/`process` disposition MAY suppress `unmapped` (subject does not claim this story) but MUST NOT suppress `interleaved` (subject claims this story but touches unowned paths); the `interleaved` check at `eng/validate-story-artifacts.py:1130` now runs before matching ownership at `:1137` and non-matching disposition handling at `:1140`, and regressions prove a `fix(9.7)` commit touching an unlisted path still fails while a matching listed-only commit stays owned. Both existing shared rows stay valid because neither subject matches `9.7`. Original finding: a mutable `shared`/`process` disposition suppressed an `interleaved`/`unmapped` hard failure, which the frozen Never forbids ("weaken ordinary unmapped/interleaved failures"). Reproduced before the patch: a `fix(9.7)` commit touching an unlisted path reported `interleaved` + exit 1, then reported `disposition=shared` with the path merely labelled `unowned` and no failure once one row was added to story text. The recorded 2026-08-25 deferral rationale ("they only withhold ownership and never grant it") covered ownership broadening but not hard-failure suppression, so the behavior was re-decided. [eng/validate-story-artifacts.py:1130]
- [x] [Review][Patch] RESOLVED 2026-08-25 (loop 5): keep `c4df0290...` and renegotiate the frozen block -- amend the frozen Always and the I/O matrix to admit multiple exactly-declared, reasoned `shared` rows rather than the single enumerated commit, correct the Execution task's "both exact dispositions" wording, and add the loop-5 Spec Change Log entry that loops 1-4 each received and this change did not. Original finding: a third `shared` disposition (`c4df0290...`) exceeds the frozen Always, which authorizes classifying exactly `2dcc43fe...`, and the I/O matrix's single "Later shared commit" row and the Execution task's "record both exact dispositions"; no Spec Change Log entry records the renegotiation — it appears only in Test Evidence prose. [_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:107]
- [x] [Review][Patch] The historical CLI test pins immutable range `ceae00a4..2dcc43fe` while reading the mutable live story artifact, so the blocking Gate 2b lane is red at HEAD (`Ran 92 tests ... FAILED (failures=1)`); bisected green at `a229be7e`, red at `d4385378`, which added the `c4df0290` disposition and two File List entries absent from that range [eng/tests/test_validate_story_artifacts.py:2028]
- [x] [Review][Patch] Three of the four changed `.agents/skills/bmad-build` files were never mirrored into the runtime `.claude` copy, so the `story_id` template field, the "persist canonical story_id" planning rule, and the entire step-05 completion gate and staging discipline are inert — `.claude/skills/bmad-build/step-05-present.md` contains zero occurrences of `validate-story-artifacts`; this is the same defect class already patched once for `step-04-review.md` [.claude/skills/bmad-build/step-05-present.md:51]
- [x] [Review][Patch] The ownership-contributing classification filter is unpinned — deleting the `if commit.classification in OWNERSHIP_CONTRIBUTING_CLASSIFICATIONS` line leaves the full 92-test suite with only the one pre-existing failure, so the loop-2/loop-3 anti-broadening invariant can be silently lost [eng/validate-story-artifacts.py:1253]
- [x] [Review][Patch] `extract_frontmatter` does not strip trailing YAML comments from `story_id`, so a correctly-substituted value that keeps the template's own hint comment fails with `invalid explicit story_id: expected exactly two numeric segments`, and the message never mentions the comment [eng/validate-story-artifacts.py:692]
- [x] [Review][Patch] Verification and Test Evidence record results that do not reproduce at HEAD — the "92 tests / 2 optional skips" claim has one failure, and the strict gate cited as passing against `a229be7e` exits 1 at the delivered HEAD; no evidence exists for `d4385378` or `5817f191` [_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:172]
- [x] [Review][Patch] Duplicate non-critical frontmatter scalars still silently overwrite, and `title` feeds legacy story-ID resolution yet is absent from `CRITICAL_FRONTMATTER_SCALAR_KEYS` [eng/validate-story-artifacts.py:51]
- [x] [Review][Patch] `extract_checked_tasks` closes a task section only on `## `, so every `### Review Findings` checked item is extracted as a task requiring evidence — currently benign only because each bullet happens to carry a bracketed path [eng/validate-story-artifacts.py:1467]
- [x] [Review][Patch] `CHECKED_TASK_HEADINGS` is a closed two-spelling set with no fail-closed path when a story contains `- [x]` lines but no recognized heading matched, so checked-task evidence validation is silently skipped [eng/validate-story-artifacts.py:53]
- [x] [Review][Patch] Strict mode reports a raw `git failure while resolving baseline: ref is empty` for a missing or empty `baseline_commit`, and has no code guard for `NO_VCS` (legacy mode has one; strict mode relies on prose only) [eng/validate-story-artifacts.py:961]
- [x] [Review][Patch] `test_malformed_stale_or_empty_dispositions_fail_closed` accepts `"malformed" in stderr or "stale" in stderr` for all six sub-cases, so an empty-reason regression still passes because the fixture SHAs are also stale [eng/tests/test_validate_story_artifacts.py:1239]
- [x] [Review][Patch] Four deferred-work entries added by this story are filed under the Story 9.6 heading rather than the new Story 9.7 heading, and the merge-resolution entry is duplicated verbatim [_bmad-output/implementation-artifacts/deferred-work.md:2588]
- [x] [Review][Patch] The `last_updated` finding was resolved backwards -- fixed during this review: restored to the emitter's `%m-%d-%Y %H:%M` format — the prior `08-18-2026 09:45` matched the emitter's `DATE_FORMAT = "%m-%d-%Y %H:%M"`, and the patch changed it to an ISO value that only matches the tolerated fallback [_bmad-output/implementation-artifacts/sprint-status.yaml:44]
- [x] [Review][Patch] Review Findings line anchors are stale although the anchor-drift finding is marked patched — only Code Map and Suggested Review Order were refreshed; nine cited anchors resolve elsewhere (`:611`->621, `:790`->838, `:911`->780, `:953`->1003, `:1078`->1126, `:1149`->1061, `:1360`->1399, `:546`->552, `:312`->324) [_bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md:376]
- [x] [Review][Patch] Untested seams and test hygiene: `--candidate requires --story` survives deletion, the bootstrap-on-merge branch is never exercised end-to-end, `BOOTSTRAP_HISTORY_AVAILABLE` has no CI assertion that history is present, two tests bundle three scenarios without `subTest`, and `setUp` re-executes the validator module for each of 14 tests [eng/tests/test_validate_story_artifacts.py:44]
- [x] [Review][Patch] The new report vocabulary (`owned`/`listed-unowned`/`unowned`/`interleaved`/`unmapped`/`shared`/`process`/`bootstrap-owned`) and the `## Commit Scope Dispositions` grammar are defined only inside this one story artifact — no `docs/` entry and no `spec-template.md` scaffold [.agents/skills/bmad-build/spec-template.md:6]
- [x] [Review][Defer] `4.1.1` is hard-coded in three unlinked places (`quality.yml`, `eng/release_compatibility.py`, three `CiGovernanceTests.cs` regexes) with no single source of truth [.github/workflows/quality.yml:82] -- deferred, owned by the release-compatibility spec declared `shared`
- [x] [Review][Defer] Strict mode spawns one `git diff-tree` per non-merge commit and prints an unbounded report on every run including failures, with no `--quiet` or `--limit` [eng/validate-story-artifacts.py:1079] -- deferred, pre-existing
- [x] [Review][Patch] Closing task extraction on every nested heading lets checked execution work under ordinary `### Implementation` subsections evade evidence validation; retain nested task collection through the next same-or-higher-level section while excluding `### Review Findings` and its descendants [eng/validate-story-artifacts.py:1467]
- [x] [Review][Patch] A `shared`/`process` declaration can downgrade a story-matching, listed-only commit from `owned`; preserve interleaved precedence and apply non-owning dispositions only when the subject does not match the story [eng/validate-story-artifacts.py:1130]
- [x] [Review][Patch] Raw terminal controls in commit subjects, disposition reasons, and Git paths can spoof report lines or terminal state; quote only control-bearing values through one deterministic rendering boundary while leaving ordinary output unchanged [eng/validate-story-artifacts.py:1260]

## Spec Change Log

- 2026-08-25, review loop 1 -- The strict gate exposed a self-bootstrap contradiction: the commit introducing exact Story 9.7 attribution could not satisfy a rule that did not yet exist, and published history cannot be rewritten. With explicit human approval, the frozen contract now permits one exact full-SHA `bootstrap-owned` tuple and the later release-compatibility commit is declared `shared`. This avoids the known-bad alternatives of history rewriting, generic exemptions, hidden interleaving, or whole-commit ownership. KEEP: canonical SHA/ancestry checks, exact ID boundaries, per-path visibility, separate workspace evidence, ordinary disposition semantics, existing CI enforcement, and the delivered validator behavior outside this exception.
- 2026-08-25, review loop 2 -- Adversarial review found the non-frozen Design Notes called every File List member `owned`, contradicting the approved rule that `shared` and `process` commits contribute no ownership. The report contract now labels a listed path `owned` only for `owned`, `interleaved`, or `bootstrap-owned` classifications and uses `listed-unowned` for listed paths in every non-owning classification. The authorization must also bind the canonical Story 9.7 artifact so a copied fixture cannot reuse it. This avoids misleading staging evidence and reusable recovery artifacts. KEEP: the exact code-bound story/baseline/SHA/sole-parent tuple, both listed and touched guard paths, fail-closed declaration handling, listed-only reconciliation, unlisted-path visibility, pure authorization tests, historical CLI integration, and operator anti-copy guidance.
- 2026-08-25, review loop 3 -- Adversarial review proved that binding `bootstrap-owned` to the canonical artifact still let later story text broaden historical ownership by adding either currently unowned bootstrap-commit path to the mutable File List. The non-frozen design now requires the exact twelve-path bootstrap-owned set to be code-bound and requires any changed intersection between the bootstrap commit and the File List to fail closed. This avoids converting the story artifact into an authorization source while still allowing unrelated future File List entries that the bootstrap commit never touched. KEEP: canonical artifact binding; the exact story/baseline/SHA/sole-parent/subject tuple; both guard paths; fail-closed malformed and multiple declarations; classification-aware `owned`, `listed-unowned`, and `unowned` labels; shared/process non-ownership; full per-path visibility; pure authorization tests; historical CLI integration; and operator anti-copy guidance.
- 2026-08-25, review loop 4 -- Human-approved review reconciliation amended the frozen Approach and third acceptance criterion to state the classification-aware report-label contract already introduced in loop 2. This avoids the contradictory reading that truthful `listed-unowned` labels violate an otherwise unchanged classification and reconciliation contract. The patch also makes the gate hermetic, mirrors it into the runtime Claude skill, closes parser and story-ID boundary gaps, pins the immutable authorization inventories, and records both intentionally unowned bootstrap paths. KEEP: the exact authorization tuple and twelve-path intersection; listed-only ownership; all-path visibility; ordinary classification and reconciliation behavior; the blocking CI command; fail-closed malformed declarations; and the loop-3 anti-broadening proof.
- 2026-08-25, review patch follow-up -- Review found three fail-open legacy/CLI parsing seams outside the frozen authorization contract: unusable baselines could hide unstaged tracked paths, duplicate critical frontmatter scalars overwrote their predecessors, and an empty explicit base was treated as absent. The patch now resolves a usable legacy baseline or reports and uses a bare-diff workspace fallback, rejects duplicate `story_id`/`baseline_commit` scalars, and rejects an explicitly empty `--base`. KEEP: strict candidate evidence, the approved bootstrap tuple and frozen block, ordinary valid-baseline discovery, staged/untracked discovery, and existing story-ID precedence.
- 2026-08-25, review loop 5 -- Human-approved reconciliation reverses the prior unrelated classification for `references/Hexalith.EventStore`, claims both that path and `references/Hexalith.Tenants` for the published `5817f191...` Story 9.7 commit, and expands the exact code-bound bootstrap-owned inventory from twelve to thirteen paths because the bootstrap commit also touched Tenants. This intentionally absorbs a pre-existing workspace change despite the Epic 9 isolation preference. The frozen contract now admits multiple exact, reasoned `shared`/`process` rows while forbidding either kind from suppressing `interleaved`; current-artifact tests validate the current range instead of combining mutable metadata with an old candidate. The patch also synchronizes runtime workflow mirrors, documents the report grammar, rejects every duplicate frontmatter scalar and inline-comment ambiguity, bounds checked-task extraction, and pins strict CLI/history/filter seams. KEEP: the exact bootstrap story/baseline/SHA/topology tuple, code-bound path inventory, listed-only reconciliation, all-path visibility, classification-aware labels, canonical refs, workspace evidence, and blocking CI command.
- 2026-08-25, review loop 5 patch follow-up -- Review found that the first heading-boundary fix excluded legitimate nested execution tasks, that matching listed-only commits could still be downgraded by non-owning dispositions, and that control-bearing Git/report values were emitted raw. The patch now keeps task collection active across nested execution subsections while excluding Review Findings, gives all matching listed-only commits `owned` precedence over `shared`/`process`, and deterministically quotes control-bearing subjects, disposition reasons, and Git paths. KEEP: interleaved precedence, non-matching shared/process behavior, ordinary report text, repeated recognized task sections, checked Review Findings exclusion, and the complete exact bootstrap authorization contract.

## Commit Scope Dispositions

- `fd04bdd97fbdd4976a0f213e46a316be199fd8a9` | `bootstrap-owned` | self-enforcing Story 9.7 delivery predates its exact commit-ID gate; human-approved one-time recovery bound to the immutable baseline and guard paths
- `2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06` | `shared` | later release-compatibility work changed shared CI, governance, and deferred-work paths without belonging to Story 9.7
- `c4df029050cb241f74cafd04a01f7718eae1ec0c` | `shared` | earlier Fluent UI and test-stability work resealed the shared analyzer-policy inventory without belonging to Story 9.7
- `f37124607b0f4c54c8be3f2fd0223fadb89d7e5c` | `shared` | earlier dependency maintenance touched the later-claimed EventStore pointer without belonging to Story 9.7
- `0bfb143e6d52cf83abcbd893e7f1c679f17d598b` | `shared` | earlier EventStore dependency maintenance touched the later-claimed pointer without belonging to Story 9.7
- `cdfff09daef6266ab538d6f745fd257b573351de` | `shared` | earlier dependency maintenance touched the later-claimed EventStore pointer without belonging to Story 9.7

## Design Notes

`bootstrap-owned` is a code-authorized historical recovery, not a general third disposition. The story declaration and canonical artifact path must match the immutable authorization tuple; copying the text, editing frontmatter, or moving a baseline cannot create authority.

The bootstrap-owned set is also immutable and code-bound. After the loop-5 human scope reversal, the intersection of the bootstrap commit's touched paths and the canonical story File List must be exactly these thirteen paths; adding the remaining historically unowned path or removing an authorized path invalidates the declaration and grants no bootstrap ownership:

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
- `references/Hexalith.Tenants`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`

Future File List entries that the bootstrap commit did not touch do not change this intersection and therefore do not broaden or invalidate the historical authorization.

The bootstrap commit's one touched path outside the immutable authorized set is `_bmad-output/implementation-artifacts/spec-actions-29316660112-fix-cicd.md`. It is intentionally absent from the Story 9.7 File List, remains visible as `unowned`, and never contributes to reconciliation.

Path labels describe ownership, not mere File List membership. For `owned`, `interleaved`, and `bootstrap-owned` commits, listed paths are `owned` and other paths are `unowned`. For `shared`, `process`, `unmapped`, and `unrelated` commits, listed paths are `listed-unowned` and other paths are `unowned`. Reconciliation still admits only listed paths from ownership-contributing classifications.

## Verification

**Commands:**
- `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py` -- both modules compile.
- `python3 -m unittest eng.tests.test_validate_story_artifacts` -- all mandatory fixtures pass with no new skip.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore` -- the changed Governance project builds with zero warnings and errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.StoryArtifactValidatorGate_IsBlockingAndExact` and the corresponding `AnalyzerPolicyGovernanceTests.AnalyzerPolicy_IdentifierInventory_MatchesSeal` method -- the isolated CI pin and its exact analyzer inventory both pass.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests -parallel none` -- run the complete affected C# Governance class and record any unrelated repository-policy blocker separately from the focused Story 9.7 fact.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md --candidate HEAD` -- exact bootstrap/shared report passes and keeps unowned paths visible.
- `pwsh ./eng/validate-docs.ps1` -- the reusable story-validation reference and navigation entry satisfy DocFX validation.
- `git diff --check` -- changed files are whitespace-clean.

## Test Evidence

- Pre-change baseline: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 72 tests with 2 existing optional `ReviewVerifierTests` skips.
- Iteration-1 known-bad evidence: the focused suite passed 76 tests with the same 2 optional skips, and the live strict report passed, but review proved its report still mislabeled listed paths in the `shared` commit as `owned` and allowed a copied fixture path to reuse the hard-coded tuple. Iteration 2 must replace this evidence rather than treating it as acceptance proof.
- Iteration-2 known-bad evidence: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 76 tests with the same 2 optional `ReviewVerifierTests` skips, and the live strict report passed with truthful classification labels. Review nevertheless proved that mutating the canonical File List could broaden bootstrap ownership, so this evidence cannot satisfy iteration 3.
- Iteration-3 evidence: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 83 tests with the same 2 optional `ReviewVerifierTests` skips. The suite includes the exact historical CLI report, copied-artifact rejection, pure tuple/topology/declaration/guard checks, independent declared-versus-resolved baseline deviations that prove fail-closed `unmapped`/`listed-unowned` behavior with no ownership, a canonical-metadata regression that adds a historically unowned bootstrap path to the File List and likewise proves no ownership, and an end-to-end regression proving checked tasks under the exact `## Tasks & Acceptance` heading are extracted and evidence-validated. `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-7-add-story-id-and-commit-scope-evidence.md --candidate HEAD` passed against canonical candidate `f35523436db525197dcc223ddbe8aa0db97bbdf3`; it reported the bootstrap commit's twelve authorized paths as `owned`, both other paths as `unowned`, and the shared commit's three listed paths as `listed-unowned` while preserving every other path as `unowned`.
- Iteration-4 evidence: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 89 tests with the same 2 optional `ReviewVerifierTests` skips and no history skip in this full checkout. New regressions pin the four closed inventories, repeated task headings, disposition prose, normalized trailing-slash classifications, version-token boundaries, clean-clone historical execution, and history availability guards. The Shell.Tests Release project built with 0 warnings / 0 errors; `StoryArtifactValidatorGate_IsBlockingAndExact` and `AnalyzerPolicy_IdentifierInventory_MatchesSeal` each passed 1/1 after the exact 6,997-token hash reseal. The full affected `CiGovernanceTests` class ran 67 tests: 66 passed and the pre-existing `ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` failed because the committed Builds gitlink is `22a578b576a515d2af214fe81859447fffc97981` while unchanged release coordinates pin `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`; Story 9.7 changes neither surface. The live strict gate passed against canonical candidate `a229be7e7e8ea8af43c537a9ed24f13175cc1416`: the bootstrap commit's twelve authorized paths remained `owned`, its two now-declared outside paths remained `unowned`, the release-compatibility paths remained `listed-unowned`, and the earlier analyzer-ledger commit remained visible as an explicitly reasoned `shared` commit. The only unrelated workspace state was the pre-existing maintainer-owned `references/Hexalith.EventStore` pointer, which remained visible and unclaimed.
- Iteration-4 review-patch evidence: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py` passed, the three focused regressions passed 3/3, and `python3 -m unittest eng.tests.test_validate_story_artifacts` passed 92 tests with the same 2 optional `ReviewVerifierTests` skips. The added subcases prove missing, `NO_VCS`, and unresolvable legacy baselines all use an explicit bare-diff fallback that rediscovers unstaged `README.md`; duplicate `story_id` and duplicate `baseline_commit` each fail closed; and an explicitly empty `--base` is rejected without a traceback. The strict Story 9.7 gate passed against canonical candidate `a229be7e7e8ea8af43c537a9ed24f13175cc1416`, preserving the exact bootstrap/shared labels and the visible documented unrelated `references/Hexalith.EventStore` workspace pointer.
- Matrix audit: `python3 -m unittest -v eng.tests.test_validate_story_artifacts.BootstrapOwnedAuthorizationTests` passed 14/14 with no skips. The exact tuple and canonical historical CLI tests cover the authorized-bootstrap and later-shared rows; the authorization-dimension and immutable-intersection tests cover wrong authorization; and the duplicate-declaration parser test plus the authorization-dimension cases cover invalid declarations including stale/short SHA, empty reason, missing guard paths, topology, and File List deviations.
- Iteration-5 evidence: `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 103 tests with the same 2 optional `ReviewVerifierTests` skips. The 16-test `BootstrapOwnedAuthorizationTests` matrix passed with no skips and now requires the repository's bootstrap history, pins the ownership-contributing classification filter, exercises bootstrap rejection on a merge, and validates the current canonical artifact rather than combining mutable story metadata with a historical candidate. The live strict gate passed against canonical candidate `88bae03fb6daac6b6433c8c38e7e87d6ff882fb2`: `fd04bdd9...` reports thirteen exact authorized paths as `owned` and the spec-actions path as `unowned`; all five exact non-owning declarations remain visible with listed paths as `listed-unowned`; and `5817f191...` reports both EventStore and Tenants as `owned`. The Shell.Tests Release project built with 0 warnings / 0 errors; `StoryArtifactValidatorGate_IsBlockingAndExact` and `AnalyzerPolicy_IdentifierInventory_MatchesSeal` each passed 1/1. The full affected `CiGovernanceTests` class again ran 67 tests: 66 passed and only the pre-existing `ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` failed because unchanged release coordinates pin `4eb33928a1d8c7775f97221cf9edc171db0cb5f8` while the approved Builds gitlink differs. `pwsh ./eng/validate-docs.ps1` passed after adding the reusable story-validation reference.
- Iteration-5 review-patch follow-up evidence: the seven focused review regressions passed 7/7, covering nested execution-task validation, checked Review Findings exclusion, interleaved precedence, both matching-commit disposition kinds, and subject/reason/path control escaping. `python3 -m py_compile eng/validate-story-artifacts.py eng/tests/test_validate_story_artifacts.py && python3 -m unittest eng.tests.test_validate_story_artifacts` passed 108 tests with the same 2 optional `ReviewVerifierTests` skips; the 16-test `BootstrapOwnedAuthorizationTests` matrix again passed with no skips. The strict gate passed against canonical candidate `88bae03fb6daac6b6433c8c38e7e87d6ff882fb2` with the exact bootstrap/shared/owned labels and no unresolved workspace state. The Shell.Tests Release project built with 0 warnings / 0 errors, and the two focused Governance facts each passed 1/1. The complete `CiGovernanceTests` class remained 66/67 solely because the pre-existing `ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` assertion compares the unchanged `4eb33928...` release coordinates with the different approved Builds gitlink. `pwsh ./eng/validate-docs.ps1` and `git diff --check` both passed. The story remains `in-review`.

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
- `.claude/skills/bmad-build/spec-template.md`
- `.claude/skills/bmad-build/step-02-plan.md`
- `.claude/skills/bmad-build/step-04-review.md`
- `.claude/skills/bmad-build/step-05-present.md`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `docs/reference/index.md`
- `docs/reference/story-artifact-validation.md`
- `references/Hexalith.EventStore`
- `references/Hexalith.Tenants`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`

## Suggested Review Order

**Exact bootstrap boundary**

- Start with the immutable authorization surface defining exactly what historical recovery may own.
  [`validate-story-artifacts.py:219`](../../eng/validate-story-artifacts.py#L219)

- Verify every declared and resolved tuple dimension fails closed.
  [`validate-story-artifacts.py:879`](../../eng/validate-story-artifacts.py#L879)

- Confirm canonical range evidence authorizes before classifying every non-merge commit.
  [`validate-story-artifacts.py:961`](../../eng/validate-story-artifacts.py#L961)

**Parser and fallback hardening**

- Reject empty base overrides before they can silently select story frontmatter.
  [`validate-story-artifacts.py:319`](../../eng/validate-story-artifacts.py#L319)

- Reject every duplicate frontmatter scalar and prevent downstream use.
  [`validate-story-artifacts.py:667`](../../eng/validate-story-artifacts.py#L667)

- Recover legacy discovery through explicit bare-diff fallback for unusable baselines.
  [`validate-story-artifacts.py:762`](../../eng/validate-story-artifacts.py#L762)

- Preserve exact story-token boundaries across dotted versions and embedded identifiers.
  [`validate-story-artifacts.py:866`](../../eng/validate-story-artifacts.py#L866)

**Ownership and reporting semantics**

- Preserve matching story commits as owned before consulting non-owning dispositions.
  [`validate-story-artifacts.py:1130`](../../eng/validate-story-artifacts.py#L1130)

- Reconciliation admits only ownership-contributing commit classifications.
  [`validate-story-artifacts.py:1234`](../../eng/validate-story-artifacts.py#L1234)

- Report labels distinguish ownership from mere File List membership.
  [`validate-story-artifacts.py:1260`](../../eng/validate-story-artifacts.py#L1260)

- Escape control-bearing report values without changing ordinary human-readable output.
  [`validate-story-artifacts.py:1345`](../../eng/validate-story-artifacts.py#L1345)

- Validate nested execution tasks while excluding reviewer bookkeeping subsections.
  [`validate-story-artifacts.py:1467`](../../eng/validate-story-artifacts.py#L1467)

**Workflow enforcement**

- Keep runtime Claude review behavior synchronized with the blocking strict gate.
  [`step-04-review.md:19`](../../.claude/skills/bmad-build/step-04-review.md#L19)

- Make unresolved story-ID template placeholders explicitly fail closed.
  [`spec-template.md:6`](../../.agents/skills/bmad-build/spec-template.md#L6)

- Document classifications, path labels, and disposition grammar for contributors.
  [`story-artifact-validation.md:13`](../../docs/reference/story-artifact-validation.md#L13)

- Pin the exact blocking CI command independently from release-policy assertions.
  [`CiGovernanceTests.cs:141`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs#L141)

**Regression proof**

- Prove all unusable baseline forms still discover unstaged tracked work.
  [`test_validate_story_artifacts.py:230`](../../eng/tests/test_validate_story_artifacts.py#L230)

- Prove all duplicate scalar keys, inline comments, and empty base overrides behave fail-closed.
  [`test_validate_story_artifacts.py:275`](../../eng/tests/test_validate_story_artifacts.py#L275)

- Prove nested tasks cannot hide behind review-only subsections.
  [`test_validate_story_artifacts.py:546`](../../eng/tests/test_validate_story_artifacts.py#L546)

- Pin matching-commit ownership and terminal-safe report rendering.
  [`test_validate_story_artifacts.py:1359`](../../eng/tests/test_validate_story_artifacts.py#L1359)

- Exercise the exact historical report with truthful owned and unowned labels.
  [`test_validate_story_artifacts.py:2028`](../../eng/tests/test_validate_story_artifacts.py#L2028)

- Prove mutable canonical metadata cannot broaden historical bootstrap ownership.
  [`test_validate_story_artifacts.py:2198`](../../eng/tests/test_validate_story_artifacts.py#L2198)
