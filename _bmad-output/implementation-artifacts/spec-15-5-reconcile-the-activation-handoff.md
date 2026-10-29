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
