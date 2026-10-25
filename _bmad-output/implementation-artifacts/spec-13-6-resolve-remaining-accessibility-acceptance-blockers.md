---
title: 'Story 13.6: Resolve remaining accessibility acceptance blockers'
type: 'chore'
created: '2026-10-25'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'cfa9febc2942666265f23f87b26112c846f625ad'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 13.6's local automated work is implemented and all seven manual results are Owner-reported Pass. Hosted CI cannot fetch the selected Parties revision, and the manual records lack required execution details.

**Approach:** Publish the already validated dependency revision with explicit authorization, rerun affected hosted verification, and reconcile human-supplied details into existing records. Preserve unresolved platform and approval gates.

## Boundaries & Constraints

**Always:** Preserve frozen intent, reported Pass results, test attribution and baselines. Record commands/counts, full commit IDs and CI links; raw output stays in scratch or CI artifacts.

**Never:** Invent observations or approvals; force-push; merge unrelated changes; update dependencies; initialize nested submodules; weaken tests. No new source bug was demonstrated; R8-B1 stays deferred.

## Approved decisions — 2026-10-25

The user instructed “apply recommended” after reviewing the publication recommendation and remaining manual qualification. This authorizes publishing the existing Parties commit `d915a5893715328ef5bdf6a740888b6d212257ca` to `refs/heads/fix/story-13-6-probe-archive` on its existing origin, and rerunning failed FrontComposer CI/Quality jobs for `cfa9febc2942666265f23f87b26112c846f625ad`. This explicit authorization overrides the generic Build workflow prohibition on remote operations for these actions only. It does not authorize changing either main branch, new commits, dependency updates or publication of documentation changes.

No additional manual execution facts were supplied. Preserve all seven Owner-reported Pass results and retain their incomplete formal qualification while addressing hosted verification. Keep the parent story and sprint in-progress.

</frozen-after-approval>

## Code Map

Initial investigation, before the authorized publication and reruns; current outcomes are recorded below.

- `references/Hexalith.Parties`: selected commit contains the approved four-path probe archival correction. Remote main is `986a369ec26f6c5194593eb7bce12955eda54149`, ten commits beyond the selected commit's parent `1ef274ea0471643d83f924147e85ac8326cf6a47`; local origin/main is stale. Proposed remote branch is absent.
- `.github/workflows/ci.yml`, `.github/workflows/quality.yml`: reuse hosted lanes. Current [CI](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145439) and [Quality build job](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/job/121219420393) fail before tests: submodule initialization exits 128, `upload-pack: not our ref d915a5893715328ef5bdf6a740888b6d212257ca`. Other Quality jobs were running at inspection.
- `eng/dependency_graph.py`: current committed-policy validation passes; no policy exception is needed.
- `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md`: authoritative parent; A1–A6 complete, aggregate acceptance open.
- `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`: update existing MAN-136 records and hosted results. Eighteen detailed observations remain open.
- `docs/accessibility-verification/manual-log-template.md`: authoritative required fields for completed manual records.

Current root: `cfa9febc2942666265f23f87b26112c846f625ad`. Subsequent changes after the implementation affect documentation only. Prior results: 1,061 browser passes/four skips, 1,411 SourceTools and 3,785 Shell passes, retaining original attribution.

## Tasks & Acceptance

**Execution:**

- [x] `references/Hexalith.Parties` — after authorization, recheck the target remote ref, publish the exact existing commit with an explicit refspec, then verify availability. If the ref exists at another commit, stop; never overwrite. No branch switch or parent gitlink change is needed.
- [x] `.github/workflows/ci.yml`, `.github/workflows/quality.yml` — rerun failed hosted jobs on the same candidate; diagnose subsequent failures before proposing changes.
- [x] `_bmad-output/implementation-artifacts/13-6-accessibility-result.md` — record hosted outcomes and supplied manual facts, retaining unsupported fields and independent blockers.
- [x] `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml` — reconcile supported progress; keep Story 13.6 in-progress until original criteria are satisfied.

**Acceptance Criteria:**

- Given authorized publication, when hosted checkout runs, then it fetches the exact selected object without changing dependency selection.
- Given hosted attempts, when results are recorded, then each claim names its actual commit/run and distinguishes failures/skips from passes.
- Given manual Pass reports, when records become completed, then required factual fields are supplied by the human; otherwise qualification stays blocked.
- Given any unresolved original gate, when tracking is reconciled, then Story 13.6 remains incomplete.

## Implementation Notes

- Published only existing Parties `d915a5893715328ef5bdf6a740888b6d212257ca` to `refs/heads/fix/story-13-6-probe-archive` after confirming the ref was absent. Exact ref lookup and a fresh isolated remote object fetch pass. The existing full message passes owning commitlint 21.2.2 with zero problems/warnings. Neither main branch, any gitlink, source, workflow, dependency selection nor baseline was changed.
- Requested failed-job reruns for the exact FrontComposer candidate `cfa9febc2942666265f23f87b26112c846f625ad`. CI attempt 2 succeeds, including 157 Cli.Tests and dependency governance; the other three configured Tier 1 project runs are not evidenced by the log/single-TRX artifact. Quality now passes dependency checkout but has independent documentation-governance, migration-wording and invalid-bearer-response failures. Firefox succeeds (505 passed / 22 skipped); Quality completes failure. The failed governance lane is 537 passed / 1 failed, Chromium is 525 passed / 1 failed / 1 skipped, and the main live proof passes one browser/138 tooling tests before four later rejection probes return status 000. These results do not close aggregate acceptance. The [hosted result](13-6-accessibility-result.md#hosted-dependency-publication-and-verification--2026-10-25) records actual attempts, job links, counts and diagnoses, distinguishing original successful jobs carried into the rerun from fresh executions.
- Reconciled the existing result, parent spec and sprint comments. All seven manual results remain Owner-reported Pass, formal qualification stays blocked, and all eighteen detailed observations remain open because no new human execution facts were supplied. Original acceptance tasks, R8-B1 and independent gates stay open; parent story and sprint remain in-progress.
- Raw outputs stay in `/tmp/fc-13-6-hosted-9cc_j2d7/` and linked CI artifacts. No new commit or documentation publication occurred.

## Verification

- `python3 eng/dependency_graph.py --root . validate --commit cfa9febc2942666265f23f87b26112c846f625ad` — investigation passed: 51 edges, seven semantic selectors.
- Existing Parties commit message validated with its pinned commitlint 21.2.2: zero problems/warnings. Successful log retained at `/tmp/fc-13-6-publication-check-j754dfym/commitlint.log`; no commit created or changed.
- `git ls-remote origin refs/heads/fix/story-13-6-probe-archive` from Parties — passes after publication: exact `d915a5893715328ef5bdf6a740888b6d212257ca`. A fresh isolated bare-store remote fetch independently retrieves that object. Neither main branch moved.
- `gh run rerun 40210145439 --failed --repo Hexalith/Hexalith.FrontComposer` and `gh run rerun 40210145120 --failed --repo Hexalith/Hexalith.FrontComposer` — both exit 0; GitHub run/job API confirms attempt 2 on exact `cfa9febc2942666265f23f87b26112c846f625ad`, CI success and Quality failure. [CI attempt 2](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145439/attempts/2); [Quality attempt 2](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40210145120/attempts/2). Exact job attribution, commands and separate failure diagnoses are in the existing result.
- `python3 eng/validate-story-artifacts.py` and `git diff --check` — pass. The committed dependency validation also reruns successfully: 51 edges/seven selectors. Frozen intent, seven manual records, eighteen unchecked observations, 69 PNG/Verify baselines and eight root gitlinks are preserved.

- Browserless migration-wording reproduction: 0 passed / 1 failed / 0 skipped / 0 flaky, zero retries, same exact error as both hosted Chromium attempts. Pinned DocFX matcher probe returns False/exit 0 when installed and exits 1 with an empty cache; the missing-prerequisite explanation remains a supported inference because hosted validation masks the underlying exception. Full commands and raw diagnostics remain in the result/scratch.

## Review Triage Log

Three independent layers completed. Edge Case Hunter returned no findings; Verification Gap Reviewer found no verification gaps. Each Blind Hunter finding was verified before routing; all fixes are direct additions to the result record, with no runtime or test changes.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-1 remaining blocker handoff | low | patch | The result diagnoses the missing three-project CI execution and three failed Quality jobs, but lacks an explicit next-action/closure handoff. Add stable local references and mark ownership unassigned rather than inventing a person or assignment. |
| BH-2 published-ref retention | medium | patch | The selected Parties object is published only on the dedicated branch and remains outside remote main; deleting that reference can recreate the demonstrated fetch failure. Record the dependency on retaining this ref and the maintainer assignment still needed. |
| BH-3 publication verification commands | low | patch | Exact commitlint and isolated-fetch procedures are asserted but omitted from the command record. Preserve the actual commands/cwds and exits without performing another push. |
| BH-4 DocFX probe reconstruction | low | patch | The external probe exists and demonstrates installed-versus-empty cache behavior, but the committed record omits its exact inputs and reconstruction. Add a bounded recipe tied to the tracked function and actual commands; retain the supported-inference qualification. |
| BH-5 browser skip attribution | low | patch | Both job logs enumerate 22 skipped tests; the result currently retains totals only. Add their grouped source identities, reasons and acceptance limits, without promoting skipped checks to passes. |
| BH-6 attempt-specific inspection | low | patch | The recorded general run API commands return the latest attempt and can drift after another rerun. Add executed attempt-2 inspection queries or explicitly identified immutable-attempt response references while retaining original command history. |

All six review corrections are applied in the existing result: a bounded blocker/retention handoff, exact publication-check commands, a reconstructible DocFX diagnostic, grouped skip attribution, and fixed attempt-2 references. Parent checks pass for recipe syntax, story-artifact validation and whitespace; original result contents, parent frozen intent, source/tests/workflows, all 69 baselines and eight root gitlinks remain unchanged. No workflows or publication actions were repeated during review. The approval closes this bounded publication/rerun/reconciliation work only; Story 13.6 acceptance remains in-progress.

This follow-up spec is done. The original Story 13.6 spec and sprint remain in-progress as explicitly approved; the generic workflow's sprint-review transition does not apply to this bounded completion. No local commit is created because the approved decisions explicitly exclude new commits and documentation publication. The only remote write was the authorized exact Parties branch publication, followed by the authorized CI/Quality rerun requests.
