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


Local committed precursor f75d774d281aa6d4764d9576597ff98ecfbaa3a7 has policy
SHA-256 1d55bd290472beb0b8ef5fc44437c0ecea57d8df49330cf02e9e69d9832255d1.
The 427-case focused suite passes (425 passed, two optional integrations skipped),
and the clean Shell build passes with zero warnings/errors. Ordinary committed
validation fails the independent Parties retained-catalog rule; infrastructure/CI
governance is 105/106 with the same sole failure. Actual remote-main PR diff,
local direct-parent push diff and actual-base acquire-catalogs each exit 1 at the
preserved old projection owner pin. The actual-base refusal is expressly
permitted for a precursor PR and remains failed evidence; the separate candidate
failure is not permitted, so no draft PR/push is made. Strict attribution for this
new continuation passes at the local commit; that does not alter concurrent
a2ed597bfbf3e178928144963ad8c9cb63077b3c's strict failure. Full exact commands,
source review and limits are in the new record; no remote success is inferred.


A later live-base observation supersedes the initial remote-main coordinate:
at 2026-10-30 09:34:14 UTC, remote main is
65cc17f69f4d42a5a7e1a5f145c5e1c3090c4b15 and original-workspace HEAD is
12dd17c92d023735ea77fe2840bee00581b851b9. These externally created commits
record Platform aa8c66edc782b1735539e60b2b949f7f4015e3e3 and are preserved.
The local precursor remains pinned to its reviewed initial b1/a8e tuple; it does
not include the newer main commit (merge base remains e7) or authorize its owner.
The live target-base diff exits 1 at aa8c66ed's old-policy owner-pin guard, before
candidate/ancestry checks. Carrying the proposal forward requires renewed tuple
and ancestry review as well as resolving the independent Parties candidate
failure; no silent rehash or rewrite is made. CI 41033896935 and Quality
41033896567 on new main are in_progress at 09:34:15 UTC, without inferred
conclusions. No precursor PR, push, merge, activation or approval is claimed.


Independent precursor review found six documentation/receipt gaps, all addressed
without policy/guard changes. Upstream candidate prerequisite DW-1988 is open
with owner roles and explicit repair/review/validation completion criteria. Exact
receipts are retained outside /tmp at
`/home/administrator/.local/state/hexalith/frontcomposer/story-15-5-policy-precursor-20261030/precursor-receipts.tar.gz`,
SHA-256 `2c91b227148d46b97a1b0c24af2554c67e4ff95dc49b0e63011b9029b36f844f`, with a verified per-member manifest and retrieval
instructions in the new precursor record. This is machine-local retention, not
hosted release evidence. Its final-results/ annex separately records final
commit/ownership/hygiene and committed-source/live-base results. The precursor
remains unpushed and in-review; all Story 15.5 and independent approval blockers
remain explicit. The historical qualification record is unchanged.


### Resumed review and external landing — 2026-10-30

Local main dd575b67c82c377bd17c34fb3d14366277d2ac62 contains precursor merge
92eff2e7b1b0f53631c53fa8284b466e202ae921 and delivery commits
f75d774d281aa6d4764d9576597ff98ecfbaa3a7 / e65502da0b755e2299a32fe6c112cdb9fddff0b2.
This later ancestry observation preserves earlier dated unlanded observations;
it establishes no protected owner review, successful activation or hosted PR.

Resumed independent review completed with one externally caused tuple-drift
handoff and a landing-observation correction. All 93 focused Python regressions
and strict/global artifact checks pass at inspected dd575 source; ordinary
committed-source validation and actual-parent diff fail the owner-pin guard.
Quality 41097179475 attempt 1 was still in progress at the recorded 17:17–17:19
UTC observation, with Gate 2c pending. No successful CI result is inferred.

A subsequent external commit f69562a2a1b0f11295278c4e715bbf7ecf2999f7 changes
all nine root selectors. Its selected Platform eba576951e61e9763341463516247dcbf8fc0b90
still fails ordinary owner-pin validation. The unchanged inline-version predicate
independently rejects current Parties 8b0e23e437296691d0233fe86afc765063f9fef5's
root Directory.Packages.props. Current and parent nested Builds catalogs are
missing locally. The [resumed precursor record](spec-15-5-prepare-the-inactive-policy-precursor.md#resumed-verification-and-external-landing--2026-10-30)
records exact commands/revisions, review triage, distinct historical/current
Parties failures and the need for renewed source/selection authorization.
This run does not alter policy, dependencies, guards, Story 16.1 or sprint status,
and makes no commit or remote write. Story 15.5/REL-BASE-1 remain review; all
independent approvals and a2ed597bfbf3e178928144963ad8c9cb63077b3c's historical
strict failure retain their prior dispositions.


### Published 4.7.2 technical baseline continuation — 2026-10-31

The [new bounded advancement](spec-15-5-advance-the-published-baseline-to-4-7-2.md)
and [append-only ledger observation](rel-ai-1-release-evidence-ledger.md#story-155-published-472-baseline-advancement--2026-10-31)
select exactly published `4.7.2`, preserving this completed handoff, its frozen
intent, baseline, File List and every historical result above. Immutable v4.7.2
release `429411190` and all eight NuGet nuspecs identify source
`085026328a6ae6f9818b9529e7b79db374e1f055`.
[Release 41111778787/1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41111778787/attempts/1)
and independent
[Release Evidence 41115667970/1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41115667970/attempts/1)
are completed/success at that source; these are not candidate-source CI results.

The active baseline and focused candidate/binary expectations advance together;
only the two XML baseline inputs change the consumer-configuration seal. All
other policy values, root selectors, guards, contracts and the empty suppression
ledger remain intact. Local 202-case Python, 98 diagnostic, 107 infrastructure/CI,
four Contracts.UI and five Testing boundary results pass, as do all seven existing
Release Gate 4 ApiCompat packs against 4.7.2. Exact commands and remaining checks
are recorded in the new implementation artifact.

Starting HEAD `7e25d2b52a0becf265b7904cc157b42c7bff5b1d` and local unreferenced
technical candidate `67b483831d2104203379c19ae1f1559c4064e2ae` each pass ordinary
committed validation. Their actual-base transition remains exit 1 on the sealed
configuration guard; the previous actual-parent event remains exit 1 on Platform
`097d663f79f409f723fc1d2bd0609d46be4a417a`'s owner pin. Candidate diagnostics do
not authorize either event, approve the merged unreviewed precursor, or repair
historical compliance/attribution failures.

At the dated ledger observation, CI `41176684664` attempt 1 at starting HEAD is
completed/failure. Quality `41176684504` attempt 1 remains in_progress overall,
with failed catalog preparation and skipped infrastructure/ApiCompat in its
completed failed build job. The successful publication/verifier attempts and
local packs do not replace those source-specific hosted gates. Story 15.5 stays
in-progress; REL-BASE-1 remains review. Publication, owner, Fluent, Product and
EventStore approvals retain their independent dispositions. No push, merge,
publication, dependency update or approval is performed by this continuation.

The later local MCP boundary run also passes both cases, including cold/warm
4.7.2 validation and the unchanged missing-baseline refusal. This does not change
the failed actual-base transitions or any separately owned approval.

Full documentation validation subsequently passes, including eight compile
snippets and DocFX 2.81.0 metadata/site generation, after restoring the existing
pinned local tool manifest. Global/focused artifact checks and diff hygiene pass.
The new spec retains the initial missing-command failure, exact commands, source
coordinates and all unresolved transition/approval limits.


### Stable-source review completion — 2026-10-31

The new 4.7.2 successor's independent review is complete and all five accepted
corrections are resolved. A clean local candidate
`ccf8250a5404aea3fca835de74d0a30858610364`, parented by preserved external
4dfc5390, passes 419 focused tests, all seven ApiCompat packs, matching-source
Gate 4a adopter smoke, fresh-context Gate 2a CLI smoke, full documentation and
strict artifact ownership. The earlier shared-worktree package mismatch remains
failed history; the new pass uses freshly aligned package/source identities.
Only the existing configuration seal changes. Its direct-parent transition
still fails, the historical Platform refusal is retained, and Story 15.5 remains
in-progress. No precursor owner acceptance, publication or independent approval
is inferred. Exact commands and preserved prerequisite failures are in the new
implementation record; this completed historical handoff's frozen intent,
baseline, File List and earlier content remain unchanged.


The final build-workflow handoff marks only the bounded 4.7.2 successor done and
advances the encompassing Story 15.5 to review, an open state. This is a tracking
transition after local verification and independent implementation review; it
does not resolve the retained policy-transition failures or separately owned
approvals. No push, merge or activation is authorized by that handoff.


### Precursor round-3 review resolution — 2026-10-31

The [precursor's round-3 resolution](spec-15-5-prepare-the-inactive-policy-precursor.md#round-3-review-resolution--2026-10-31)
resolves its seven review patches as documentation only. Earlier observations in
this handoff stay as written; this entry corrects them where stated.

- **Landing.** The precursor reached main through direct merge
  92eff2e7b1b0f53631c53fa8284b466e202ae921, with no PR, while candidate validation
  was failing. That deviates from the frozen intent. The landed
  f75d774d281aa6d4764d9576597ff98ecfbaa3a7 policy is unreviewed, must not serve as
  a reviewed prior policy for activation, and its owner acceptance remains open.
- **Delivery.** The 2026-10-30 resumed entry above says that run made no commit or
  remote write. Commit e07e2f42605ffde307606f4ee027c382ef1e8aa4 then delivered it to
  origin/main and selected Platform a185bfaa5541f8de675fb82e4f553e1e49931201,
  whose owner pin ordinary validation refuses. CI 41099330117 failed, and
  Quality 41099329667 was cancelled after failing catalog preparation.
- **Superseded hosted observation.** Quality 41097179475 failed catalog preparation
  at 17:19:14Z, skipped Gate 2b–4a and ended cancelled at 17:20:39Z. Quality
  41097863471 and CI 41097864370 for f69562a2a1b0f11295278c4e715bbf7ecf2999f7
  also ended cancelled.
- **DW-1988 correction.** The entry above describes "distinct historical/current
  Parties failures". That is wrong: DW-1988's retained file is byte-identical at
  Parties 8b0e23e437296691d0233fe86afc765063f9fef5, and the root refusal masked it.
  Upstream Parties d651173dc4b9b121217a28a97b3090ddf9e60960 later cleared both,
  and fa30a34785b27a1a95903e35a7a1c1507426a5ab sealed the result. DW-1988's
  closure remains its owners' decision.
- **Current lanes.** At HEAD 188d8b733090a83108bd006aaa9fa71cd9a5a2d8,
  `acquire-catalogs` passes. Ordinary validation, the actual-parent push diff and
  clean infrastructure governance (108/109) fail on
  `consumer projection root source selectors changed`. Hosted failed-step logs
  show the same message. The cause is external commit
  a55960f5441239410107bda3d547b381fdfa6f3a, which moved root Builds and changed
  consumer configuration without renewing the Platform projection seals.
  `root_selectors_sha256`, `builds_commit` and `consumer_configuration_sha256`
  all drift; only the first is reported. It is deferred and not repaired here.
- **Open tracking.** Owner acceptance of the unreviewed precursor policy, and
  which policy may serve as the reviewed prior for activation, are now
  deferred items. So is strict attribution across sibling Story 15.5 specs.

Receipts are archived at
`/home/administrator/.local/state/hexalith/frontcomposer/story-15-5-policy-precursor-20261031/round3-receipts.tar.gz`
(SHA-256 `55b4d00df5d68170868d60734d929bbc87cbb858387e22625733723eb45c24e0`).
This is machine-local retention, not hosted evidence. The resolution changes no
policy, dependency, guard or gitlink and grants no activation or approval.
