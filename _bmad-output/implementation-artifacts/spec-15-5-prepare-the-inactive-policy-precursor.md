---
title: 'Story 15.5: Prepare the Inactive Policy Precursor'
type: 'chore'
created: '2026-10-30'
status: 'in-review'
route: 'oneshot'
story_id: '15.5'
story_key: '15-5-i-rel-base-1-reconcile-the-published-compatibility-baseline'
baseline_commit: 'b1d89648ae6feed1c89e48c265de013e1507fa97'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-15-5-qualify-the-governance-transition.md'
---

<frozen-after-approval reason="user-authorized bounded inactive precursor and draft owner-review PR; expected actual-base governance refusal explicitly permitted">

## Intent

Prepare the smallest inactive AD-12/AD-12a projection-policy precursor for the exact committed source tuple, after reviewing Platform inventory, both helper sources, all root selectors and Builds.Tooling against existing authorization. Resolve the missing exact Builds 1884a88ab80d3d0ae80c09a5ed9dfe23867d9fb9 catalog by permitted metadata acquisition, retaining any blocker. Preserve commits, original story baselines, catalog authority, dependency selections, validation guards, unrelated workspace changes and every independent approval. Never initialize nested submodules.

Run focused regressions, ordinary committed-source validation, actual-base dependency diff and clean infrastructure/CI governance. Preserve the expected old-base refusal as failed evidence; candidate diagnostics grant no prior-base authorization. Record exact revisions, commands, results and blockers in this continuation, the handoff and ledgers. Once candidate validation and relevant regressions pass, push a draft precursor PR for owner review. Do not merge, bypass protection or claim activation. Activation needs a later real event after the reviewed precursor lands. Keep Story 15.5 open and a2ed597bfbf3e178928144963ad8c9cb63077b3c's strict attribution failure explicit.

</frozen-after-approval>

## Implementation Notes

- Starting HEAD b1d89648ae6feed1c89e48c265de013e1507fa97 includes the existing qualification commit. Remote main e7e892b6ed584501f6dd18e11acd299948c0113f is the real proposed PR base. Preserve both without rebasing or rewriting history.
- Original workspace has unrelated Platform checkout aa8c66edc782b1735539e60b2b949f7f4015e3e3. Work is isolated in a new worktree with standalone clones of only the nine root-declared dependencies, checked out at committed gitlinks. The proposal binds Platform a8e6276b512186b1e9e35160b8c150a50a17f789, not the dirty checkout.
- Intent gaps: none. The authorized draft PR is reversible; no deployment or irreversible data operation is needed. Footprint: four scalar changes in the existing projection row plus this record and append-only handoff/release/deferred-ledger updates. No implementation, workflow, trust, expectation, package, SDK, catalog or gitlink changes.
- Source investigation confirms only owner_commit, inventory_sha256, builds_commit and root_selectors_sha256 need a proposed replacement. All other projection fields and every policy section remain unchanged. Substantive review precedes those replacements; the computed identities describe a proposal for owner review.
- Receipt root: `/tmp/fc-15-5-policy-precursor-20261030-5womdkxt/`; exact argv, CWD, UTC time, result and logs are recorded in commands.jsonl. Clean npm install uses the existing lockfile; object and global NuGet caches are shared, so this is not a cold-network availability claim.

## File List

- `eng/dependency-graph-policy.json`
- `_bmad-output/implementation-artifacts/spec-15-5-prepare-the-inactive-policy-precursor.md`
- `_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md`
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`

## Proposed tuple and substantive source review

This is a proposal for owner review, not an authorization or activation receipt.
The actual PR base is e7e892b6ed584501f6dd18e11acd299948c0113f. Starting HEAD
b1d89648ae6feed1c89e48c265de013e1507fa97 and that base select the same nine root
gitlinks. Their prior policy SHA-256 remains
`e9ad463b10e144c9117378c92f518306e914680e6ac9a2cc2542a9e8a07b5bd6`.
The authorized tuple is reconstructed from historical activation
e04652d7f3fef2d3c2c6a77504a73b0c2f363a6e and checked against the existing policy.
The proposed committed policy SHA-256 after Git normalization is
`1d55bd290472beb0b8ef5fc44437c0ecea57d8df49330cf02e9e69d9832255d1`.

Exactly four projection fields change:

| Field | Currently authorized | Proposed |
| --- | --- | --- |
| `owner_commit` | `b742b1b7a496ac72d6961ca6db5213d3df42d234` | `a8e6276b512186b1e9e35160b8c150a50a17f789` |
| `inventory_sha256` | `7f4753f5bd787b2948eafb6da1b28b103246f3022b57710129cc61b714a58a29` | `2145b99e0580abdb05a812dc81ea009b4364d12990fe176a6d0dc5988efccedb` |
| `builds_commit` | `81225bcb6151c38ae891abd9110af17385e1b3b9` | `0b17f8022cdc23b8b242b000f5b2aa15580184ac` |
| `root_selectors_sha256` | `ddf83f55971c78a6d265f46b3bc9d6364c1acf85cfc88ee5c3e60818c13f81ef` | `b6534b18c7f4462366def97b1a0e5e79d1224e17c3543833de3042895dc4a40d` |

All other policy values remain identical: trust, profiles and exact semantic
expectations, limits, module dispositions, evaluators, release owners, approval
capability and all remaining projection fields. The owner stays evidence-only.
The unchanged consumer configuration seal is
`f5186d1b483c33e065ac542374a4b90c0da8257782ebc871d29aaf0ef3613762`.
The exact AppHost project, HexalithPlatformRoot binding, both Compile links and
root Builds.Tooling project reference remain sealed. No source selection changes.

Every root selector was reviewed against the authorized historical activation:

| Root path | Authorized | Proposed committed selection |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` | `70dbf73e80787299c832d30d911b16b105ab194d` |
| `references/Hexalith.Builds` | `81225bcb6151c38ae891abd9110af17385e1b3b9` | `0b17f8022cdc23b8b242b000f5b2aa15580184ac` |
| `references/Hexalith.Commons` | `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a` | `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a` |
| `references/Hexalith.EventStore` | `36c78f3435d2d492a73f1b0707cf865c8735d740` | `7b1df62102efb19da03553a890c62eb5ad6aeafe` |
| `references/Hexalith.Memories` | `72c36c7113c750c8b809f9d72b945a2a8c2d4555` | `9ea24458f949589bb8e59f9334161009487509d1` |
| `references/Hexalith.Parties` | `492a8cc1417c625cdfb7fd4c946686ef88adab9d` | `e2528a5d5853641f2b38e5e2e34bb02c0cc4be23` |
| `references/Hexalith.Platform` | `b742b1b7a496ac72d6961ca6db5213d3df42d234` | `a8e6276b512186b1e9e35160b8c150a50a17f789` |
| `references/Hexalith.PolymorphicSerializations` | `221ac4592ba908ae7661ba1fa9e2d1fcc23baee0` | `221ac4592ba908ae7661ba1fa9e2d1fcc23baee0` |
| `references/Hexalith.Tenants` | `7ddb864cda5e942925b056391617eae4895c1253` | `0d469d7c8fedc383f6258cbf2798e62cf9847304` |

Platform retains all 17 mappings; 16 selectors differ. Its .gitmodules raw
SHA-256 remains `6da4f5297d79bfa54afcb8daacda70670af11868ba1738cc185f584db8eb6ab7`.
Both source blobs are byte-identical across the authorized and proposed owners:
DaprSelfHostedMtls.cs is
`703072feb8b609c2661a1c7eaa955fbd2c9d838402520b4fc425bda2000ba1a8`;
DaprSelfHostedMtlsSubscriber.cs is
`55c280284d0a305b1d6b6667f31362b7c4e4a3cf39890b6175d913cd762bb9c6`.
The seven inventory-only identities remain Agents, Chatbot, Conversations,
Folders, Projects, Timesheets and Works. Their selectors are recorded as raw
metadata; none is trusted, resolved, acquired, initialized or executed.

| Platform inventory path | Authorized owner selection | Proposed owner selection |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` | `70dbf73e80787299c832d30d911b16b105ab194d` |
| `references/Hexalith.Agents` | `703345f2e31148f44498e6673b00447c816f8dab` | `dd4aa1dbcfa5bce9c39567012b339fc6470cc690` |
| `references/Hexalith.Builds` | `51a71cb1cfe860d5fc135eb3b8521fd80c413810` | `1884a88ab80d3d0ae80c09a5ed9dfe23867d9fb9` |
| `references/Hexalith.ChatBot` | `f568d43e99d2779e8d81bcd49552c7441d34de69` | `03ed05ba73fddba39c0535a6d7b836682e1a8d14` |
| `references/Hexalith.Commons` | `1663028fb6d9f46a0bf17919edeb21bb99be1587` | `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a` |
| `references/Hexalith.Conversations` | `c71e6ba924f42e7f5da62116f9e3b4fd5d77227e` | `6a6ed23dd3fdc05eba3c4d87ff1e7105d606135b` |
| `references/Hexalith.EventStore` | `eff7066ed5c8c7ec57e1b42862fddee479044134` | `28ccd32c95ab521755d12deee5d7ad16927e7c92` |
| `references/Hexalith.Folders` | `4d166c77df9c46e0600e5a22bb097a7385eb75b7` | `7e31dc1e8c0aaed1f1da41c0a614640beffecff0` |
| `references/Hexalith.FrontComposer` | `5688c3eba98567133285b0f51843d9daa8f24f0b` | `f3fef9f2dad49638521d7bc2fd3bf43c8056b516` |
| `references/Hexalith.McpCli` | `8d4a8c07636c1ad065dfc8dc28dbfc956d5deb82` | `c05335fc2ed90901bcaeef82a67263fb2d1c8703` |
| `references/Hexalith.Memories` | `790670897c1fc684d41b980fc5e40934be796122` | `fc8b46f07f77b73c67cab6687feb07e0e600d585` |
| `references/Hexalith.Parties` | `94f328fcf1a4c2a2b96dd962d89ad8a94d756203` | `1d84af20837f388c1605085c54109bd4b537a998` |
| `references/Hexalith.PolymorphicSerializations` | `221ac4592ba908ae7661ba1fa9e2d1fcc23baee0` | `221ac4592ba908ae7661ba1fa9e2d1fcc23baee0` |
| `references/Hexalith.Projects` | `515d49c93fc0544b4f49c1662578f348781c0320` | `29a61cb079328e6dffdafb77ff117f89d1930a1c` |
| `references/Hexalith.Tenants` | `1436044bf6452bbc5bdb2eaa8b585ab2ce9362ca` | `4fa78baa94f939a2b752bce52d0bac77214ddd83` |
| `references/Hexalith.Timesheets` | `69f3622b17de70bf3e9043f96577f2b6d1a4cbd9` | `7a8a89ca727f44b7b1fa6a58a79d8050f853af67` |
| `references/Hexalith.Works` | `15d14fd82a92bbd277fe9394cd0b77281750de20` | `c3bb3e680723e8030dc0363a6e2f38ac22610e60` |

The actual root Builds.Tooling source change is substantive: 25 paths,
1438 insertions and 161 deletions. Reviewed code adds CompositionAttachAdmission,
CompositionAttachRequest, attachment documents/store/models and bounded hold
admission; changes CompositionEngine and ModuleCommandExecutionService cleanup,
withdrawal and failure-retention behavior; introduces checked Linux fsync through
CompositionDurableFlush; and changes resource claims, native test outcome/report
handling and source mapping's empty AdditionalProperties handling. Admission
checks runner-issued identity, exact composition/mode and retry lineage, rejecting
hosted/developer/CI owners. These are source observations, not behavior acceptance.

The helper's directly referenced CompositionWorkspace, CompositionPortAllocator,
CompositionRunPorts, CompositionDaprSidecarPorts and CompositionToolchainPins,
plus the Builds.Tooling project file, have no diff. This narrows direct helper
exposure but supplies no equivalence or owner adoption approval for the assembly.
No changed dependency implementation was executed for this comparison.

Reproduction commands (exit 0):

- Root: `git diff --raw e04652d7f3fef2d3c2c6a77504a73b0c2f363a6e b1d89648ae6feed1c89e48c265de013e1507fa97 -- references`.
- Platform: `git diff b742b1b7a496ac72d6961ca6db5213d3df42d234 a8e6276b512186b1e9e35160b8c150a50a17f789 -- .gitmodules DaprSelfHostedMtls.cs DaprSelfHostedMtlsSubscriber.cs` (empty), plus `git diff --raw b742b1b7a496ac72d6961ca6db5213d3df42d234 a8e6276b512186b1e9e35160b8c150a50a17f789 -- references`.
- Builds: `git diff --stat 81225bcb6151c38ae891abd9110af17385e1b3b9 0b17f8022cdc23b8b242b000f5b2aa15580184ac -- src/libraries/Hexalith.Builds.Tooling`.
- Clean root: `python3 /tmp/fc-15-5-policy-precursor-20261030-5womdkxt/inspect-source-tuple.py` asserts historical seals and records raw proposed metadata; its successful reporting exit is not graph acceptance.

## Exact catalog acquisition and first independent candidate blocker

Before acquisition, `git cat-file -e 1884a88ab80d3d0ae80c09a5ed9dfe23867d9fb9^{commit}`
in the isolated root Builds store exits 128, missing locally. The exact command
`git -c submodule.recurse=false fetch --no-recurse-submodules --no-tags https://github.com/hexalith/hexalith.builds.git 1884a88ab80d3d0ae80c09a5ed9dfe23867d9fb9`
exits 0. This is the explicitly requested, AD-12a/AD-10 trusted canonical Builds
metadata acquisition, independent of candidate projection authority. It changes
only the isolated object store and FETCH_HEAD, never a gitlink or working tree;
no nested repository or candidate executable is materialized. The ordinary
active-base acquire-catalogs guard is preserved and will be tested separately.

The fetched commit's raw Props/Directory.Packages.props SHA-256 is
`fa3df877f1304db565456dd6c79da7d13ad2cff600fea94331ea093523568a10`, identical to
root Builds 0b17f8022cdc23b8b242b000f5b2aa15580184ac's catalog. Equal bytes supply
metadata identity, not transferable source, Fluent or publication approval. The
historical authorized Platform nested Builds 51a71cb1cfe860d5fc135eb3b8521fd80c413810
catalog remains `95622edbc970beea37bf22c2fbd5bfe43cf6e69f2087a8a0f6fac0e0b3097ef6`.
The specific missing-object obligation is resolved locally; cold hosted execution
has not yet acquired or accepted the proposed inventory under prior authority.

`python3 eng/dependency_graph.py --root . --policy eng/dependency-graph-policy.json validate --commit b1d89648ae6feed1c89e48c265de013e1507fa97`
exits 1. With the proposed policy explicit, collection/projection checks succeed,
then Parties e2528a5d5853641f2b38e5e2e34bb02c0cc4be23 fails the unchanged
parties-catalog-v1 no_inline_versions_in_tracked_files check. Its retained
`_bmad-output/implementation-artifacts/tests/five-reconciliation-and-migration-2026-10-28/retained-source-mapping/pinned-tree/Props/Directory.Packages.props`
contains 307 direct PackageVersion rows (30085 bytes; SHA-256
`48b3abca2eb67bc09bde33f3f0203e10fbca810614d0582852be4ab54294dc9d`), introduced
by 9332b8a3310d8ba719dfd47af825682058d4f493. The scanner deliberately covers
every tracked matching .csproj/.props/.targets blob; import participation is not
an exemption. The first exact refusal is `must inherit every package version
from the pinned Builds catalog`. Later semantic results are not inferred.

This cannot be fixed by acquiring metadata or changing a projection seal. A
Parties content repair plus a new selection, or a narrowed guard/profile, exceeds
this task's preservation constraints. It remains an upstream content prerequisite.
Only the expected actual-base governance refusal was exempted for the precursor
PR. This independent candidate failure is not exempted: the draft PR remains
blocked until the stated candidate-validation prerequisite can pass. No remote
write, merge, protection bypass, activation or independent approval is authorized
by these diagnostics. Story 15.5 and REL-BASE-1 remain in review.

## Committed-source verification at f75d774d281aa6d4764d9576597ff98ecfbaa3a7

This local commit is a direct child of the preserved starting HEAD
b1d89648ae6feed1c89e48c265de013e1507fa97. It is not pushed or landed. Its policy
SHA-256 is `1d55bd290472beb0b8ef5fc44437c0ecea57d8df49330cf02e9e69d9832255d1`.
All commands below run from the isolated clean root unless otherwise stated.
Full argv/results are in commands.jsonl under the receipt root above.

- `npm ci --ignore-scripts --no-audit --no-fund` — exit 0, 352 packages from the existing lockfile. No original npm directory is reused or modified.
- `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_workflow_source_closure.py tests/eng/test_dependency_handoff.py tests/eng/test_release_contract.py tests/eng/test_release_evidence_v2.py tests/eng/test_validate_contract_artifacts.py eng.tests.test_validate_story_artifacts` — exit 0, 427 run, 425 passed, 2 skipped, 234.799s runner time. Includes closed projection shapes, inventory-only trust, root and configuration drift, forged evidence, missing historical catalogs, candidate self-authorization, zero-base acquisition/execution refusal, actual workflow preparation, handoff and release checks.
- `python3 eng/dependency_graph.py --root . --policy eng/dependency-graph-policy.json graph --commit b1d89648ae6feed1c89e48c265de013e1507fa97` — exit 0, 69 raw edges and eight Builds selectors, with projection checks enforced by the collector. This explicit working-policy diagnostic authenticates the described inventory/catalog metadata and bindings; it is not semantic validation or prior-base authority.
- `python3 eng/dependency_graph.py --root . validate --commit f75d774d281aa6d4764d9576597ff98ecfbaa3a7` — exit 1, ordinary committed candidate-policy validation reaches the Parties retained-catalog inheritance refusal above. No --policy override is used here. Candidate validation has not passed.
- `python3 eng/dependency_graph.py --root . diff --event pull_request --event-base e7e892b6ed584501f6dd18e11acd299948c0113f --candidate f75d774d281aa6d4764d9576597ff98ecfbaa3a7` — exit 1, real remote-main base's existing policy refuses Platform a8e6276b512186b1e9e35160b8c150a50a17f789: `consumer source projection owner pin changed`. This is the explicitly permitted expected actual-base refusal, preserved as failed evidence, not waived validation.
- `python3 eng/dependency_graph.py --root . diff --event push --event-base b1d89648ae6feed1c89e48c265de013e1507fa97 --candidate f75d774d281aa6d4764d9576597ff98ecfbaa3a7` — exit 1, same refusal under the local commit's exact direct-parent policy. This local transition check is not a hosted push event or activation claim.
- `python3 eng/dependency_graph.py --root . acquire-catalogs --event-base e7e892b6ed584501f6dd18e11acd299948c0113f --candidate f75d774d281aa6d4764d9576597ff98ecfbaa3a7` — exit 1 at the unchanged actual-base owner pin, even after the explicitly requested exact metadata fetch. Candidate policy never authorizes this preparation command.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --nologo -m:1 -p:NuGetAudit=false` — exit 0, zero warnings/errors. This builds the package-based Release governance harness, not the changed Builds.Tooling source or a runtime topology.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-prepare-the-inactive-policy-precursor.md --candidate f75d774d281aa6d4764d9576597ff98ecfbaa3a7` — exit 0, story-id=match, disposition=owned, exactly five owned paths. This continuation uses its own baseline; no historical attribution is repaired.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md --candidate a2ed597bfbf3e178928144963ad8c9cb63077b3c` — exit 1, strict story-id=no-match, disposition=unmapped and the same four listed-unowned delivery paths. Rerun from the original workspace also reports its preserved unrelated dirty Platform pointer; no disposition or exemption is added.
- `node node_modules/@commitlint/cli/cli.js --edit /tmp/fc-15-5-policy-precursor-20261030-5womdkxt/precursor-commit-message.txt --verbose` — exit 0, pinned commitlint 21.2.2, zero problems/warnings before the local commit. `git diff --check` and `bash eng/check-committed-files.sh --staged` pass, five staged files.

The two optional skips are ReviewVerifierTests.test_artifact_validation_failure_prevents_done_review_completion
and ReviewVerifierTests.test_incomplete_review_reports_workflow_not_complete:
`bmad-story-automator skill is not installed`. Neither integration is claimed
passed. Earlier `gh run view --json ...attempt...` probes exited 1 because this
installed gh has no attempt JSON field; corrected gh api calls at 09:30:29–30 UTC
confirm both existing runs completed/failure, attempt 1. Failed probe logs remain.

No new package, clean-consumer, ApiCompat, DocFX, runtime or hosted PR success is
claimed. Those earlier qualification results retain their original revisions.
The draft remains conditional on actual candidate validation and relevant
regressions passing, separately from the permitted actual-base refusal. A later
real event can select a reviewed, landed precursor as its prior policy; a local
commit or diagnostic cannot do so. Upstream content repair would change the tuple
and require renewed review before updating this proposal. Do not silently rehash.

### Clean infrastructure/CI result

`dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests -class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests`
— exit 1 at committed source f75d774d281aa6d4764d9576597ff98ecfbaa3a7;
106 total, 105 passed, one failed, zero errors/skips/unrun. The sole failure is
CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds,
on the Parties retained-catalog inheritance refusal. The unchanged
GovernanceBuildTrait_IsDeclaredOnlyInTheProjectTheHeavyLaneRuns passes in the
clean npm environment; the original self-link remains untouched. Log:
clean-infrastructure-ci-governance.log. This is a failed candidate governance gate,
not the separately permitted actual-base refusal, so draft-PR creation is blocked.

All original Story 15.5 baselines remain unchanged: published-baseline
2c8e204c9f1ca5c2c44d70005d481dd70f89f3fb, activation handoff
7319f44bcf02b67d7ee09a315ba8aa9c7251b1f6, repair
2fcb1e9b5802ed32fc387680c6e42369130e0384 and qualification
e7e892b6ed584501f6dd18e11acd299948c0113f. Sprint and REL-BASE-1 remain review.
Release Owner, publication, Fluent, Product, EventStore and GOV-1 approvals retain
their independent previously recorded dispositions. No source adoption approval
is created by the agent's comparison, test execution or local commit.

## External source movement and live-base disposition

At 2026-10-30 09:34:14 UTC, fresh read-only history and ls-remote record an
external advance. Original-workspace HEAD is now
12dd17c92d023735ea77fe2840bee00581b851b9, whose exact parents are
b1d89648ae6feed1c89e48c265de013e1507fa97 and
65cc17f69f4d42a5a7e1a5f145c5e1c3090c4b15. Remote main is the latter commit,
whose parent is e7e892b6ed584501f6dd18e11acd299948c0113f. This task created
neither external commit and did not stage, merge, push or change the original
workspace. The originally dirty Platform checkout is unchanged at
aa8c66edc782b1735539e60b2b949f7f4015e3e3; the external commit now records it,
so the original workspace is clean rather than still showing that dirty pointer.

The external delta versus the initial committed source is solely Platform's
root gitlink a8e6276b512186b1e9e35160b8c150a50a17f789 to
aa8c66edc782b1735539e60b2b949f7f4015e3e3. It is preserved in the original
workspace and remote main. The isolated precursor still describes its exact
reviewed starting tuple at b1d89648ae6feed1c89e48c265de013e1507fa97; it does
not adopt or authorize the externally advanced tuple. References above to e7 as
the real remote-main/PR base are timestamped initial-preflight observations;
e7 is no longer live main. No PR exists, so none of these local probes claims an
authenticated hosted event. Both the independent candidate refusal and renewed
source/ancestry review block carrying this local proposal to a future PR.

`python3 eng/dependency_graph.py --root . diff --event pull_request --event-base 65cc17f69f4d42a5a7e1a5f145c5e1c3090c4b15 --candidate f75d774d281aa6d4764d9576597ff98ecfbaa3a7`
— exit 1, live target-base policy refuses Platform
aa8c66edc782b1735539e60b2b949f7f4015e3e3's owner pin. The check stops before
candidate or ancestry evaluation; it proves neither. `git merge-base 65cc17f69f4d42a5a7e1a5f145c5e1c3090c4b15 f75d774d281aa6d4764d9576597ff98ecfbaa3a7`
returns e7e892b6ed584501f6dd18e11acd299948c0113f (exit 0), so this precursor
does not include the new main commit. Do not rebase/rewrite historical commits,
switch the proposed owner or refresh seals without substantive renewed review.

At 09:34:15 UTC, `gh run list --repo Hexalith/Hexalith.FrontComposer --commit 65cc17f69f4d42a5a7e1a5f145c5e1c3090c4b15 --limit 10 --json databaseId,name,status,conclusion,headSha,event,url`
reports CI 41033896935 and Quality 41033896567 in_progress with no conclusion;
Commitlint 41033896747 passed and CodeQL 41033896900 is in_progress. No result is
inferred for unfinished work. Those are external push runs, not precursor PR
runs. The existing initial-source CI/Quality failures remain historical failures.

### Reproduction and source-attribution clarification

The complete raw tuple diagnostic is already preserved in the historical
[qualification record](spec-15-5-qualify-the-governance-transition.md#reproduce-the-raw-tuple-inspection).
Reproduce this continuation's exact inspect-source-tuple.py from that code block
by changing only `commit` from e7e892b6ed584501f6dd18e11acd299948c0113f to
b1d89648ae6feed1c89e48c265de013e1507fa97, and `base` from
f3fef9f2dad49638521d7bc2fd3bf43c8056b516 to
e7e892b6ed584501f6dd18e11acd299948c0113f. Run from a checkout with the exact
root object stores and the independently acquired trusted catalog metadata.
The historical spec remains unmodified; no unavailable scratch-only source is
required to reconstruct the diagnostic. The exact executed copy is retained
with the receipts below. It uses raw object helpers and never authorizes sources.

The npm install, 427-case fixture regression suite, explicit working-policy
diagnostics and Shell build ran with HEAD b1d89648ae6feed1c89e48c265de013e1507fa97
and the four proposed working-policy replacements. They are pre-commit evidence;
the suite is not a successful validation of the real candidate's catalogs.
Ordinary validate, both initial-base diffs, initial-base acquire-catalogs, strict
new-continuation attribution and the clean infrastructure/CI test execution
ran after the actual f75d774d281aa6d4764d9576597ff98ecfbaa3a7 commit, using its
committed policy without an override where specified. Later documentation-only
additions are separately attributed by the final strict check; the f75 receipt
does not cover their unstaged content. No f75 hash is assigned to a pre-commit
working-tree state.

### Source review reproduction and retained receipts

The complete reviewed source delta is reproduced from the root Builds repository
with `git diff --no-ext-diff --no-renames 81225bcb6151c38ae891abd9110af17385e1b3b9 0b17f8022cdc23b8b242b000f5b2aa15580184ac -- src/libraries/Hexalith.Builds.Tooling`
(exit 0; tooling-source-diff.log). In that immutable diff, Runtime/CompositionAttachAdmission.cs
and CompositionAttachRequest.cs contain owner/mode/hold admission; CompositionEngine.cs
and ModuleCommandExecutionService.cs contain withdrawal, retention and cleanup
changes; CompositionDurableFlush.cs and the document/native snapshot callers
contain checked fsync before rename; CompositionResourceClaimStore.cs contains
empty-resource verification; TestEnvironment/TestResultWriter.cs and native
report/execution files contain attachment-aware results; and
Workspace/SourceMappingReconciliationTask.cs changes empty-property splitting.
These exact source locations support the earlier code-reading observations.
Neither the stat nor this review is runtime or owner-adoption acceptance.

A verified machine-local receipt snapshot is retained outside temporary storage:

- Archive: `/home/administrator/.local/state/hexalith/frontcomposer/story-15-5-policy-precursor-20261030/precursor-receipts.tar.gz`.
- SHA-256: `2c91b227148d46b97a1b0c24af2554c67e4ff95dc49b0e63011b9029b36f844f`.
- 76 members include exact command argv/CWD/UTC/result logs, raw tuple JSON, full tooling diff, executed diagnostic/setup/preservation scripts, messages and receipt-members.json. The latter gives each other member's byte count and SHA-256; all hashes were verified against extracted member bytes.
- Verify with `sha256sum /home/administrator/.local/state/hexalith/frontcomposer/story-15-5-policy-precursor-20261030/precursor-receipts.tar.gz`; list/retrieve with `tar -tzf /home/administrator/.local/state/hexalith/frontcomposer/story-15-5-policy-precursor-20261030/precursor-receipts.tar.gz` and `tar -xzf /home/administrator/.local/state/hexalith/frontcomposer/story-15-5-policy-precursor-20261030/precursor-receipts.tar.gz -C <existing-local-receipt-directory>`.

This snapshot ends after the full source-diff/preservation checks and precedes
final documentation finalization. A final-results/ annex under the same persistent
directory retains exact final commit, ownership, whitespace, ordinary validation,
live-base refusal and Git-state checks. The final commit cannot embed its own
identifier. This local retention is not hosted release evidence or a remote
access guarantee; another machine can reproduce from the tracked diagnostic
block and exact canonical objects, or must explicitly obtain the local archive.
No receipt archive, manifest or raw-output packet is committed to the repository.

The preservation script confirms exactly four changed policy scalars, all other
policy values identical, unchanged proposed root gitlinks, complete historical
document prefixes, unchanged original baseline files and no initialized nested
Git entries in either workspace. SDK 10.0.403, runtime 10.0.14, Node v26.4.0,
Python 3.14.4 and pinned commitlint 21.2.2 were used. Global NuGet and Git object
caches are shared; no cold-cache qualification is inferred.

## Review Triage Log

Independent Blind Hunter: 32.551 kB changed content; finding floor
min(floor(sqrt(32.551)+1),10)=6. Every finding was checked against source/receipts.

- BH-1 — medium / patch: /tmp-only receipts could disappear. Added the verified external archive, exact checksum, per-member manifest and retrieval instructions. Explicitly retain the machine-local/hosted-evidence distinction.
- BH-2 — medium / patch: the diagnostic reproduction depended on a scratch-only script. Linked the historical tracked complete implementation and specified both exact revision substitutions; retained the executed copy in the archive.
- BH-3 — medium / patch: diff --stat alone did not substantiate behavioral source observations. Recorded the full immutable source-diff command/log and the precise implementation files supporting each code-reading conclusion; runtime/owner acceptance remains unclaimed.
- BH-4 — medium / patch: the committed-verification heading grouped pre-commit checks. Added explicit pre-commit versus post-commit source attribution and the relation to unchanged committed policy/source bytes; no pre-commit run is attributed to f75.
- BH-5 — medium / patch: earlier strict/hygiene receipts omitted later documentation additions. Finalization runs fresh ownership and whitespace checks after all additions, records the exact resulting commit in the persistent final-results annex, and leaves the old snapshot receipts untouched.
- BH-6 — medium / patch: the new Parties blocker lacked a stable actionable ledger coordinate. Added open DW-1988 with owner roles, exact evidence and completion criteria; prior entries remain append-only history. Completion requires separately authorized upstream content/selection work and renewed exact tuple validation, never an exemption or inferred approval.

This continuation stays in-review: the initial bounded proposal is prepared
locally, but the independent candidate failure and externally advanced tuple
remain blocking. No review finding authorizes a guard or scope change. The
conditional draft PR has not been created, and Story 15.5 remains open.


### Resumed review — 2026-10-30

Step 4 reviewed the complete baseline-to-workspace diff at inspected source
dd575b67c82c377bd17c34fb3d14366277d2ac62: 85.028 kB; Blind Hunter floor
min(floor(sqrt(85.028) + 1), 10) = 10. Three independent context-free reviewers
ran at the parent model capability. Edge Case Hunter returned no findings.
Verification Gap returned no gaps after tracing ConsumerProjectionTests, ordinary
InfrastructureGovernance validation and Quality's existing invocation. The diff
also included independently owned pushall and Story 16.1 commits; inclusion does
not transfer their ownership to this continuation. The human explicitly allowed
continuation while preserving those changes.

Every Blind Hunter finding was checked separately:

- BH-R1 — high / defer: ordinary validation at dd575b67c82c377bd17c34fb3d14366277d2ac62 reproduces the owner-pin refusal: Platform 9e01ffacc45cb6a5ee12853d44f4b7f6e1f4084c differs from this precursor's a8e6276b512186b1e9e35160b8c150a50a17f789 seal. This external selection movement postdates the precursor. Renewed tuple review and a separately authorized successor are required; no seal is refreshed here. A new deferred entry records the latest subsequent movement too.
- BH-R2 — medium / patch: the handoff and release ledger lacked a later ancestry observation of the precursor merge. Append that observation and the failed current gate to both records, retaining every earlier dated unpushed/unlanded observation. Ancestry proves inclusion; it supplies no protected-review, hosted-PR or successful-activation receipt.
- BH-R3 — low / reject: the suggested final-annex checksum would edit this spec and create another local evidence identity without establishing hosted authority. The existing archive/annex already disclaim that authority. Fresh strict verification below checks the committed delivery range independently; no new evidence archive is introduced.
- BH-R4 — medium / reject outside intent: Story 16.1 lacks a File List; its legacy validator command reproduces that failure. The user explicitly requested preservation of that independently owned spec. This review neither repairs it nor adds its paths to 15.5 ownership.
- BH-R5 — medium / reject outside intent: Story 16.1 omits --candidate and therefore does not request strict attribution, as the validator reference confirms. Correcting the independently owned spec is excluded by the preservation instruction.
- BH-R6 — low / reject outside intent: Story 16.1's first execution bullet omits its frozen human decision's current-selection comparison. That explicit decision remains authoritative. Aligning its task text would edit the preserved spec.
- BH-R7 — false: Story 16.1 explicitly distinguishes the Quality-tested tuple SHA from the later record commit and requires release comparison with that tested tuple. No record descendant is claimed to have been tested merely because it records a result; no immutability exception is introduced.
- BH-R8 — medium / reject outside intent: Story 16.1 names two commit paths in its frozen approach and three in its later task list. Reconciling its authorized scope belongs to its owner; this run preserves that spec.
- BH-R9 — low / reject outside intent: Story 16.1 uses both overall-passing-run and individual-required-step wording; its frozen rule requires individual-step inspection, and no tuple record exists. Clarifying its independently owned plan would edit a preserved spec rather than repair this precursor.
- BH-R10 — medium / reject outside intent: Story 16.1's planned record lacks an explicit run-attempt/job binding, which matters across reruns. No tuple record exists yet. That future requirement belongs to the preserved spec's owner; this run records its own hosted observation with attempt and job identities below.

## Resumed verification and external landing — 2026-10-30

Local main at dd575b67c82c377bd17c34fb3d14366277d2ac62 includes merge
92eff2e7b1b0f53631c53fa8284b466e202ae921 and precursor delivery commits
f75d774d281aa6d4764d9576597ff98ecfbaa3a7 and
e65502da0b755e2299a32fe6c112cdb9fddff0b2. Earlier non-landing observations
remain dated history. Inclusion supplies no independent owner approval.
This resumed run performs no Git mutation or remote write.

| Lane | Required command | Local result | Blocker timing | Fallback evidence | CI authority |
| --- | --- | --- | --- | --- | --- |
| Projection regressions | `python3 -m unittest tests.eng.test_dependency_graph.ConsumerProjectionTests tests.eng.test_dependency_graph.QualityCatalogPreparationWorkflowTests tests.eng.test_dependency_graph.ApprovedProjectionExpectationTests` | Passed: 33 run, zero failures/skips, 14.751s, at dd575b67c82c377bd17c34fb3d14366277d2ac62 | None | None needed | Local fixture evidence only |
| Compatibility policy | `python3 -m unittest tests.eng.test_pack_release_packages tests.eng.test_release_prepublish` | Passed: 60 run, zero failures/skips, 1.830s, at the same source | None | None needed | Local regression evidence only |
| Global integrity | `python3 eng/validate-story-artifacts.py` | Passed, exit 0 before record updates; final rerun follows below | None | None needed | Local artifact evidence only |
| Strict ownership | `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-prepare-the-inactive-policy-precursor.md --candidate dd575b67c82c377bd17c34fb3d14366277d2ac62 --unrelated references/Hexalith.Platform --reason 'User authorized preserving the existing Platform workspace changes while continuing Story 15.5.'` | Passed, exit 0; both 15.5 delivery commits owned; interleaved commits unrelated | None | None needed | No semantic or approval authority |
| Committed source | `python3 eng/dependency_graph.py --root . validate --commit dd575b67c82c377bd17c34fb3d14366277d2ac62` | Failed, exit 1 | Collection, before semantics: selected Platform 9e01ffacc45cb6a5ee12853d44f4b7f6e1f4084c, `consumer source projection owner pin changed` | Raw diagnostic is not a passing substitute | No hosted acceptance inferred |
| Actual-parent transition | `python3 eng/dependency_graph.py --root . diff --event push --event-base 2255fb8a7fc135a04769c7b9df0f14de3796fb19 --candidate dd575b67c82c377bd17c34fb3d14366277d2ac62` | Failed, exit 1 | Base collection: Platform 8ff6db8feb5b3165bdaaf90899778199823bc59e, same owner-pin refusal | No candidate-policy override or self-comparison | Local parent/candidate reproduction only |

Raw inspection reuses the tracked diagnostic in the qualification record with
source dd575b67c82c377bd17c34fb3d14366277d2ac62, authorized-root example
b1d89648ae6feed1c89e48c265de013e1507fa97 and parent
2255fb8a7fc135a04769c7b9df0f14de3796fb19. It reports metadata only, never
authorization. The two helper hashes and consumer-configuration seal match the
policy, but all 17 Platform gitlinks have changed from the a8e owner. A three-scalar
preview was prepared in scratch only; it is not applied or accepted.

Hosted observation at 2026-10-30 17:17–17:19 UTC: Quality
[41097179475](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41097179475)
on dd575b67c82c377bd17c34fb3d14366277d2ac62 is push attempt 1, in_progress,
with no conclusion. Attempt-specific job 124205767715 has catalog preparation
and all four Gate 2c steps pending; epic9-live-acceptance job 124205767773 is
also in_progress. Read with `gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/41097179475`
and `gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/41097179475/attempts/1/jobs --paginate`.
These timestamped observations claim no later result or successful runtime gate.

### Subsequent external nine-selector advance

Before these append-only notes were written, external commit
f69562a2a1b0f11295278c4e715bbf7ecf2999f7 advanced all nine root gitlinks.
It is preserved, and no dd575 result is relabeled as verification of that tuple.
The exact selected Platform is now eba576951e61e9763341463516247dcbf8fc0b90,
root Builds 1160a3a7ddc0f4c6927456b5735a2963261016a0, and Parties
8b0e23e437296691d0233fe86afc765063f9fef5. The prior three-scalar preview is
therefore stale even as a proposal; it must not be applied to this new tuple.

`python3 eng/dependency_graph.py --root . validate --commit f69562a2a1b0f11295278c4e715bbf7ecf2999f7`
exits 1 at the unchanged projection owner-pin guard, before semantics. The raw
diagnostic exits 0 as a metadata reporter: root selector digest
43ac5d825fb2b2d8fdb5cf74d3c4f47ad5d0db9c3711f7018900a85832bab747,
Platform inventory digest 2bec644d9c20bcb8bbb68bbac9a61c191e05f9363123c6e90eb11d41ca0e6c69,
and unchanged helper hashes. The new Platform selects nested Builds
cc0db51b800be1d0d5badc3780395cfb5953d0f5; that object and the parent's nested
69701dfcf54cb9c69fc6cb0c51b031af8d218142 are absent in the inspected local
Builds store (`fatal: not a tree object`). The old 1884a88ab80d3d0ae80c09a5ed9dfe23867d9fb9
object is now available there after the external dependency advance; its catalog
hash still matches fa3df877f1304db565456dd6c79da7d13ad2cff600fea94331ea093523568a10.
No fetch or nested-submodule initialization was performed by this run.

The existing Parties `assert_no_inline_versions` predicate, invoked with the
unchanged policy's selected profile/extensions/limits against exact committed
8b0e23e437296691d0233fe86afc765063f9fef5, independently rejects its root
Directory.Packages.props: `must inherit every package version from the pinned
Builds catalog`. This is a new current-source diagnostic, distinct from DW-1988's
historical retained-file refusal at e2528a5d5853641f2b38e5e2e34bb02c0cc4be23.
Neither reports that ordinary validation passed the Platform guard or that a full
candidate graph was accepted. Earlier manual predicate probes failed on incorrect
call arguments/policy lookup before evaluation; only the corrected existing
predicate supplies this refusal evidence.

Source/ancestry renewal must cover all nine new selectors and changed Builds.Tooling,
exact current/parent catalogs and Parties owner content. The frozen preservation
constraints do not authorize silently adopting that tuple, changing a dependency
or weakening an owner guard. No current infrastructure, ApiCompat or hosted-CI
success is claimed. All original baselines, historical failures, independent
approvals, Story 16.1 and sprint status are preserved. This continuation remains
in-review; the workflow's done/commit step is not reached while required gates fail.
Commands, raw diagnostics and the stale unapplied preview remain local in
`/tmp/fc-15-5-resume-review-dqivd22x/`; no evidence packet is committed.


### Final resumed checks

The current Parties root refusal is the local Contracts.UI fallback ItemGroup:
_PartiesContractsUiCatalogVersion plus a conditional PackageVersion for
Hexalith.FrontComposer.Contracts.UI. The guard deliberately rejects any locally
declared PackageVersion, including this conditional family-version fallback.
An upstream repair must confirm the exact selected catalog supplies that package,
remove the local fallback through the owning repository, address any further
tracked-version refusals and separately authorize the resulting root selection.
No scanner exemption or semantic-profile relaxation is proposed.

After all resumed append-only updates:

- `python3 eng/validate-story-artifacts.py` — exit 0.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-prepare-the-inactive-policy-precursor.md --candidate f69562a2a1b0f11295278c4e715bbf7ecf2999f7` — exit 0, both 15.5 delivery commits owned; external selector/skill/Story 16.1 commits remain unrelated; four owned unstaged documentation paths, no staged/untracked/unresolved paths.
- `git diff --check` — exit 0.
- SHA-256 comparisons with the pre-review snapshot confirm Story 16.1 and sprint-status are byte-identical. Policy is unchanged; the four updated documents preserve their entire pre-review content as a prefix and retain CRLF. External dependency commits are preserved rather than included in this run's changes.

This run leaves four append-only documentation edits for review. No commit is
created because required candidate/governance prerequisites remain failed and
this spec must not be promoted to done. Completing the successor requires a
human amendment permitting renewed current-tuple qualification and the necessary
Parties content/dependency-selection repair; the preservation constraints and
all independent approvals otherwise remain in force.
