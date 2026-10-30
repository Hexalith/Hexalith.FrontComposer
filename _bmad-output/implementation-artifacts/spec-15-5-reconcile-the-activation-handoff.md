---
title: 'Story 15.5: Reconcile the Activation Handoff'
type: 'chore'
created: '2026-10-29'
status: 'done'
route: 'oneshot'
story_id: '15.5'
story_key: '15-5-i-rel-base-1-reconcile-the-published-compatibility-baseline'
baseline_commit: '7319f44bcf02b67d7ee09a315ba8aa9c7251b1f6'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-15-context.md'
---

<frozen-after-approval reason="bounded continuation of the requested story; existing approved intent preserved">

## Intent

**Problem:** Story 15.5 already selects published 4.7.1 and has a completed
later-base policy-activation record, but the release ledger ends with the
original refused transition. Its handoff needs to distinguish that historical
refusal, the subsequent activation, and the current checkout's verification.

**Approach:** Append a dated handoff to the existing release ledger linking the
completed implementation and activation. Check the latest published identity,
existing compatibility policy, and committed activation transition. Record
current CI and local graph limitations explicitly. Refresh the stale Epic 15
context through the existing workflow. Retain the baseline, dependencies,
configuration, historical evidence and separately owned approvals. Keep the
numbered story and REL-BASE-1 at review; this continuation records verification,
not maintainer acceptance or a repair of independently owned catalog/CI work.

</frozen-after-approval>

## Implementation Notes

- Investigation found no missing baseline implementation: the policy constant,
  XML baseline sites and Quality coordinates already select 4.7.1. No intent
  gaps, irreversible operations or new APIs. Footprint: the append-only release
  ledger, regenerated Epic 15 context, this continuation record, and review-driven
  handoffs for inherited CI failures in the existing deferred-work ledger.
- Reuse `eng/release_compatibility.py`, the eight-package
  `eng/release-package-inventory.json`, existing policy tests,
  `eng/dependency_graph.py` and `eng/validate-story-artifacts.py`. Add no scanner,
  test harness, reconstructed package, or committed evidence bundle.
- Original specs and their baselines remain unchanged. The previous Story 15.4
  continuity preserves publication validation, complete File Lists, catalog-owned
  Fluent selection and independent approval gates. The context refresh restores
  source-supported privacy and exact Fluent approval constraints.
- Fresh GitHub release metadata reports immutable v4.7.1, ID 426062933,
  published 2026-10-27T22:38:59Z. Its three recorded JSON asset digests still
  match the prior ledger citations. Fresh inventory-derived NuGet index/HEAD
  requests find all eight latest stable versions at 4.7.1 with HTTP 200.
  This is availability/metadata verification, not a new package-byte audit.
- `python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py`
  — exit 0, 60 passed in 4.455s. The existing seven passing real ApiCompat packs
  remain attributed to the approved advancement; no new pack run is claimed.
- `python3 eng/dependency_graph.py --root . diff --event push --event-base 0b469e03c5ae81cf6756f7cd8f2f78df7074514c --candidate cde40df71ebcef6c6c74c25a5a5644bebef3fc17`
  — exit 0. This actual committed descendant uses the already-landed prior
  policy; preserve the original earlier-base refusal. Current HEAD validation
  separately exits 1 because catalog object
  a9455fb3b57a65233020f4b3fc61426514521d7c is absent from the root Builds object
  store. No dependency/object acquisition or nested-submodule initialization
  is performed to mask the ordinary command's result.
- Current HEAD's exact CI/Quality attempt-1 reads report failure; Quality's
  Gate 4 is skipped. Record the bounded failures and links in the handoff.
  Do not substitute release-publication runs or historical packs for those gates.
- Logs/availability results stay in disposable local scratch:
  `/tmp/fc-15-5-handoff-20261029-w3jwgj9i/`. Final hygiene, ownership,
  documentation checks and independent review are recorded below.

## File List

- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`
- `_bmad-output/implementation-artifacts/epic-15-context.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md`

## Verification

- `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild`
  — exit 0, documentation validation passed. DocFX/snippet generation is
  unchanged; no fresh compilation or publication is claimed.
- `python3 eng/validate-story-artifacts.py` and
  `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md`
  — both exit 0 before review. Recheck after the documentation corrections.
- `git diff --check` — exit 0 before review; all changed Markdown uses CRLF.
  Ledger history is preserved as an unchanged prefix after repository newline
  normalization. No production/configuration file changed.
- `node node_modules/@commitlint/cli/cli.js --edit /tmp/fc-15-5-handoff-20261029-w3jwgj9i/commit-message.txt --verbose`
  — exit 0, zero problems/warnings. Installed CLI 21.2.2 matches package-lock.
  Successful output is `commitlint.log` in that local scratch directory.

### Read-only query reproduction

The GitHub connector's `github_fetch` was invoked with `url` set to each of:

- `https://api.github.com/repos/Hexalith/Hexalith.FrontComposer/releases/latest`
- `https://api.github.com/repos/Hexalith/Hexalith.FrontComposer/actions/runs?head_sha=7319f44bcf02b67d7ee09a315ba8aa9c7251b1f6&per_page=30`
- `https://api.github.com/repos/Hexalith/Hexalith.FrontComposer/actions/runs/40884129895/attempts/1/jobs?per_page=100`
- `https://api.github.com/repos/Hexalith/Hexalith.FrontComposer/actions/runs/40884129344/attempts/1/jobs?per_page=100`

`github_fetch_workflow_job_logs` used `repo_full_name=Hexalith/Hexalith.FrontComposer`
and `job_id=123483171783` / `123483170149`. These establish the failure details
and exact skipped Gate 4; `hosted-observations.json` retains bounded API fields
in local scratch. They do not establish CI for this later documentation commit.

The actual inventory-derived NuGet query was:

```bash
python3 - <<'PYQUERY'
import concurrent.futures, json, pathlib, urllib.request
root=pathlib.Path('.')
rows=[r for r in json.loads((root/'eng/release-package-inventory.json').read_text())['packages'] if r['packable']]
def check(row):
 name=row['package_id']; p=name.lower(); base=f'https://api.nuget.org/v3-flatcontainer/{p}'
 with urllib.request.urlopen(base+'/index.json',timeout=20) as response:
  versions=json.load(response)['versions']
 stable=[v for v in versions if '-' not in v]
 latest=max(stable,key=lambda v:tuple(map(int,v.split('.'))))
 url=f'{base}/4.7.1/{p}.4.7.1.nupkg'
 with urllib.request.urlopen(urllib.request.Request(url,method='HEAD'),timeout=20) as response:
  status=response.status
 return {'package':name,'latest_stable':latest,'baseline':'4.7.1','http_status':status,'index':base+'/index.json','endpoint':url}
with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
 result=list(pool.map(check,rows))
path=pathlib.Path('/tmp/fc-15-5-handoff-20261029-w3jwgj9i/nuget-availability.json')
path.write_text(json.dumps(result,indent=2)+'\n')
for r in result: print(r['package'],r['latest_stable'],r['http_status'])
assert all(r['latest_stable']=='4.7.1' and r['http_status']==200 for r in result)
PYQUERY
```

Result: exit 0; eight rows, all latest stable 4.7.1 / HTTP 200. The scratch
parent must exist before replay. This is the recorded one-off read-only query,
not a new compatibility scanner or a CI live-feed dependency.

## Review Triage Log

Blind Hunter completed independently; Edge Case Hunter and Verification Gap
were skipped by the oneshot route. All five findings were checked directly:

- BH-1 — medium / patch: the completion record still left checks/review pending.
  Added exact documentation, ownership, hygiene and commitlint results outside
  the frozen intent; final post-correction results follow.
- BH-2 — low / patch: publication/availability observations omitted query inputs.
  Added actual connector URLs/job IDs and the executed inventory-driven Python
  query. Earlier publication-byte audits remain separately attributed.
- BH-3 — medium / patch: current catalog/CI failures had no explicit handoff.
  Appended two focused deferred-work records, linked from the release ledger,
  with responsible maintainer roles, exact failures and closure criteria. Both
  are inherited failures outside this documentation continuation; no repair is
  claimed. Their original hosted attempts remain authoritative.
- BH-4 — medium / patch: context omitted Shell's DEBUG build gate and missing
  IHostEnvironment refusal. Confirmed in customization documentation and restored
  both conditions without changing runtime behavior.
- BH-5 — medium / patch: Fluent decision lacked its dated receipt and exact
  candidate/Builds binding. Restored the 2026-10-21 receipt link and the rule that
  equal versions across revisions transfer no approval evidence.


### Final local disposition

All five review corrections are complete. Post-correction documentation validation
and both global/focused artifact commands above exit 0; `git diff --check` exits
0. Both existing ledgers retain their complete historical prefixes; the owned
Markdown newline checks pass. No new tests were needed for these documentation
corrections, and the already-passing 60 policy tests were not rerun.

The continuation is done as a documentation handoff. Story 15.5 and REL-BASE-1
remain at review; both in-progress and final review workflow synchronizations
preserve their already-later/current review state without editing sprint status.
The current-source CI failures and skipped ApiCompat gate remain explicit. Two
inherited CI blockers are deferred to their responsible maintainer roles; no
baseline, runtime, dependency, policy or public API repair is claimed.


## Documented Unrelated Changes

- `.config/dotnet-tools.json` — concurrent DocFX 2.78.5 to 2.81.0 update in
  `9591708e442439dbf5b223685ac5f7fea5a58b80`, merged by
  `7e2702791451f3dd0e72e961d6993ed6f2650c61`; outside this handoff and preserved.

### Concurrent delivery during finalization

The final pre-commit guard detected that another session advanced main through
that DocFX merge and then committed all four handoff documents as
`a2ed597bfbf3e178928144963ad8c9cb63077b3c`. This session's prepared commit was
not executed. The landed content matches the reviewed worktree, and the
working tree was clean. Earlier source-specific observations above remain
attributed to invocation source 7319f44bcf02b67d7ee09a315ba8aa9c7251b1f6;
no graph/CI result is projected onto these later commits.

Global artifact validation still passes. The legacy focused check then failed
because the independently committed DocFX path was not declared; the unrelated
entry above repairs that path accounting without changing this story's File
List or original baseline. Separately,
`python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md --candidate a2ed597bfbf3e178928144963ad8c9cb63077b3c`
exits 1: external delivery a2ed597bfbf3e178928144963ad8c9cb63077b3c is
`story-id=no-match`, `disposition=unmapped`, and its four delivery paths are
`listed-unowned`. Exact output: `concurrent-delivery-strict.log` in local scratch.
No history rewrite, non-owning exemption, baseline movement or passing strict
classification is claimed. This is an additional review limitation, not an
accepted human disposition. This session's final follow-up records only this
concurrency/accounting result and uses the already-validated Story 15.5 message.


### Authorized CI-blocker repair continuation — 2026-10-29

The user subsequently authorized repair of the two deferred CI blockers. The
[new bounded repair](spec-15-5-repair-verification-blockers.md) records the exact
commands, counts and source attribution. This append preserves the original
frozen intent, baseline 7319f44bcf02b67d7ee09a315ba8aa9c7251b1f6, File List,
historical refusals and concurrency findings above.

Before changes, HEAD and hosted main both remained
2fcb1e9b5802ed32fc387680c6e42369130e0384 with a clean root worktree.
[CI 40921392978, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40921392978/attempts/1)
and
[Quality 40921392023, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40921392023/attempts/1)
reproduce the same two blockers; hosted Quality Gate 4 remains skipped.

The graph engine now has explicit `acquire-catalogs` preparation, invoked before
Quality governance. It reads bounded committed base/candidate inventories under
the active event-base policy, acquires only missing exact Builds catalog commits
from the policy identity's canonical remote, and verifies the requested objects
and catalog blobs. Graph/validate stay offline; no trust, projection or semantic
guard is weakened. Local acquisition of a9455fb3b57a65233020f4b3fc61426514521d7c
succeeded without checkout or nested initialization. Root Builds remains
at e38700b3a8de8ef57342688e9dfbbe3f573201ff with unchanged catalog bytes.
Testing's expectation alone now matches catalog-selected localization 10.0.14.

Local repair results after review: 32 projection/acquisition/workflow regressions
passed; the broader Python dependency/guard suite ran 427 cases (425 passed,
2 skipped); 60 release
policy regressions passed; Testing and Contracts.UI boundary classes passed
5/5 and 4/4, including real clean consumers. The existing seven-library Quality
ApiCompat lane passed locally against 4.7.1. CLI packing and all eight package
boundary validations passed. Both additional PackageReference-only consumers
restored and built with zero warnings/errors. These are fresh local results for
the repair worktree rooted at 2fcb1e9b5802ed32fc387680c6e42369130e0384.

The missing-object repair exposes a further existing fail-closed condition:
ordinary committed-source validate and actual-parent push diff now both exit 1,
`consumer projection root source selectors changed`. The selected current Builds
and root selector tuple disagree with the existing projection seal. That seal
and all selections are preserved. The infrastructure/CI governance run exits 1:
106 total, 104 passed, 2 failed. One is this selector guard; the other is a local
pre-existing `tests/e2e/node_modules/node_modules` self-link causing
`Too many levels of symbolic links`. No full-governance success is claimed.

The concurrent a2ed597bfbf3e178928144963ad8c9cb63077b3c strict command above was
rerun and still exits 1 with `story-id=no-match`, `disposition=unmapped` and the
four `listed-unowned` paths. Neither its disposition nor either original story
baseline is changed. Fresh local successes do not establish passing hosted CI,
close either deferred blocker's hosted closure conditions, or grant any
independent approval. Story 15.5 and REL-BASE-1 remain at review.


### Clean governance and source-authority continuation — 2026-10-30

The [new qualification record](spec-15-5-qualify-the-governance-transition.md)
continues this repair without changing its frozen intent, baseline, historical
results or completed bounded-repair disposition. It records every exact command,
source tuple, first/final environment result and remaining prerequisite.

HEAD/hosted main initially matched f3fef9f2dad49638521d7bc2fd3bf43c8056b516.
While this task was read-only, an external commit
e7e892b6ed584501f6dd18e11acd299948c0113f recorded the existing Platform pointer
and landed locally/remotely. Clean qualification is pinned to that source and its
actual parent f3fef9f2dad49638521d7bc2fd3bf43c8056b516. This task made no change
to dependency selections or unrelated workspace content.

The active policy still authorizes Platform b742b1b7a496ac72d6961ca6db5213d3df42d234,
root Builds 81225bcb6151c38ae891abd9110af17385e1b3b9 and root selector digest
ddf83f55971c78a6d265f46b3bc9d6364c1acf85cfc88ee5c3e60818c13f81ef. The exact
current tuple selects Platform a8e6276b512186b1e9e35160b8c150a50a17f789 and Builds
0b17f8022cdc23b8b242b000f5b2aa15580184ac, root selector digest
b6534b18c7f4462366def97b1a0e5e79d1224e17c3543833de3042895dc4a40d. Helper bytes,
configuration and .gitmodules still match, but the inventory and root tooling
source differ. No hashes were refreshed. AD-12 requires a reviewed inactive
precursor to land before a later event can use that prior policy; candidate
self-authorization and a scratch base remain forbidden. The ordinary current
validate, actual-parent diff and catalog preparation all exit 1 on the owner pin.

Clean checkout plus lockfile-only npm installation avoids the recorded self-link:
infrastructure/CI governance is 105/106, with the projection guard the sole failure.
The 427-case Python suite passes with two skips, 60 release regressions pass,
Testing/Contracts.UI boundary classes pass 5/5 and 4/4, all seven existing ApiCompat
library packs pass against 4.7.1, all eight package validations pass, and both
additional fresh package-only consumers restore/build. No new suppression, guard
change or nested initialization occurs. Exact logs are local scratch in
`/tmp/fc-15-5-governance-20261030-zqwjdxk5/`.

[CI 41023803195 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41023803195/attempts/1)
passes its build/test job but fails dependency governance; [Quality 41023802429
attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41023802429/attempts/1)
fails catalog preparation. Both reach actual-base Platform 2cbb8a68d277bdd55e321972b8aa11ef015fc54e's
owner-pin refusal. Hosted infrastructure and ApiCompat remain skipped. These are
existing push runs of e7e892b6ed584501f6dd18e11acd299948c0113f, not PR results.

The relevant local gates remain failed, so no PR/push/merge is made under the
user's passing-gates prerequisite. Story 15.5 and REL-BASE-1 remain at review;
all independent approvals retain their dispositions. Strict attribution for
concurrent a2ed597bfbf3e178928144963ad8c9cb63077b3c was rerun and remains exit 1:
story-id=no-match, disposition=unmapped, four listed-unowned paths. No exemption
or reinterpretation is introduced.

Review-qualified observation at 2026-10-30 08:20:37 UTC: CI 41023803195
attempt 1 is completed/failure; Quality 41023802429 attempt 1 remains in_progress
without an overall conclusion, while its build-and-test job 123957050415 is
completed/failure with infrastructure and ApiCompat skipped. The current Platform
also selects nested Builds 1884a88ab80d3d0ae80c09a5ed9dfe23867d9fb9, whose catalog
object is absent locally; its digest is unavailable and no remote absence is
inferred. This remains an exact-metadata prerequisite after authorized policy
activation. The qualification record now contains full diagnostic code, actual-base
coordinates, named skip reasons and a verified archive outside temporary storage
with its checksum/retrieval path. No failed gate, old attribution failure or
independent approval is reclassified.


### Story 15.5 inactive precursor preparation — 2026-10-30

The [new bounded precursor](spec-15-5-prepare-the-inactive-policy-precursor.md)
uses starting HEAD b1d89648ae6feed1c89e48c265de013e1507fa97 and real remote-main
base e7e892b6ed584501f6dd18e11acd299948c0113f. The existing qualification commit
and unrelated dirty Platform checkout aa8c66edc782b1735539e60b2b949f7f4015e3e3
are preserved. Only four fields of the Platform projection are proposed: owner
and raw inventory for a8e6276b512186b1e9e35160b8c150a50a17f789, actual root Builds
0b17f8022cdc23b8b242b000f5b2aa15580184ac and the sealed nine-root selector tuple.
The supporting record compares all root and Platform inventory selectors, both
unchanged helper blobs, unchanged consumer configuration and substantive
25-file Builds.Tooling changes. No dependency selection, catalog, trust,
expectation, evaluator or validation guard changes.

Exact Builds 1884a88ab80d3d0ae80c09a5ed9dfe23867d9fb9 was acquired into an
isolated root Builds object store from the canonical URL with no recursive fetch,
checkout or nested initialization. Its raw catalog SHA-256 is
fa3df877f1304db565456dd6c79da7d13ad2cff600fea94331ea093523568a10. This resolves
the historical locally-missing metadata observation; it grants no projection,
source, catalog-adoption or runtime authority.

Explicit candidate-policy validation now reaches an independent Parties
committed-source refusal: at e2528a5d5853641f2b38e5e2e34bb02c0cc4be23, the
retained-source-mapping/pinned-tree/Props/Directory.Packages.props artifact
contains 307 direct PackageVersion rows, violating the existing all-tracked-file
inheritance guard. The full path, exact blob hash and command are in the new
record. No fixture exception or policy weakening is introduced. The user's
exception permits only the expected actual-base refusal; candidate validation
still blocks a draft PR. No activation or remote publication is claimed.

CI 41023803195 attempt 1 and Quality 41023802429 attempt 1 on
e7e892b6ed584501f6dd18e11acd299948c0113f are both completed/failure at the
2026-10-30 09:23:49 UTC pre-edit observation. CI's build/test job passed, but
its dependency gate failed; Quality failed catalog preparation and skipped
infrastructure/ApiCompat. These existing push results are not precursor PR
verification. All independent approvals and Story 15.5/REL-BASE-1 remain at their
previous dispositions. Concurrent a2ed597bfbf3e178928144963ad8c9cb63077b3c's
strict no-match/unmapped/four-listed-unowned failure remains explicit and open.
