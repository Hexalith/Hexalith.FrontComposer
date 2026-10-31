---
title: 'Story 15.5: Prepare the Inactive Policy Precursor'
type: 'chore'
created: '2026-10-30'
status: 'done'
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
- 2026-10-31 round-3 resolution: resolves the seven open round-3 `[Review][Patch]` items as documentation only; no policy, dependency selection, guard, workflow, test or gitlink change. Before it, the user asked to fast-forward the nine root-declared dependency checkouts in the original workspace to their origin/main (non-recursive, no nested initialization, no gitlink committed). The resulting checkouts are Builds 585ffbce4452d995ef09b9f65d21b4ba343d4993, EventStore 61e6fa853d81cf822d67d6abfd6a130bdbb14ce2, Memories 00919bdd6fe97284da5f4f1657da4a5b336867cc, Parties 05aeee0d647a34eab112b6f8282a0810d35b7bf6, Platform 03a099bba258c37a8f16c8b2e842ae842b1ad6d6 and Tenants 9e5a991d0ba304bac7b1a5809cd71797b47d78ed; Commons, PolymorphicSerializations and AI.Tools were already current. These unstaged checkout moves are not part of this delivery; staging only the File List paths keeps them out, because committing them would be another unsealed seal transition. Every lane ran in an isolated clean clone at HEAD 188d8b733090a83108bd006aaa9fa71cd9a5a2d8's committed gitlinks. Two inline corrections (DW-1988 wording here and in the deferred ledger) follow the review's explicit instruction; the handoff and release ledger stay append-only and receive dated corrections instead. Surprise: external a55960f5441239410107bda3d547b381fdfa6f3a moved root Builds without resealing the root selectors, so ordinary validation and clean infrastructure governance fail at HEAD for a reason unrelated to this precursor.

## File List

- `eng/dependency-graph-policy.json`
- `_bmad-output/implementation-artifacts/spec-15-5-prepare-the-inactive-policy-precursor.md`
- `_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md`
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

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

Superseded in part on 2026-10-31: the round-3 review resolution below corrects
this section's delivery, hosted, nested-catalog, Parties and receipt statements.

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
Builds catalog`. This root refusal masks, rather than replaces, DW-1988's
retained-file refusal: the identical retained blob is still present at
8b0e23e437296691d0233fe86afc765063f9fef5 and is rejected too (corrected
2026-10-31; see the round-3 resolution below).
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
tracked-version refusals — including DW-1988's retained
`_bmad-output/implementation-artifacts/tests/five-reconciliation-and-migration-2026-10-28/retained-source-mapping/pinned-tree/Props/Directory.Packages.props` —
and separately authorize the resulting root selection.
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

## Code review round 3 — 2026-10-30

Follow-up review of the fix delta dd575b67c82c377bd17c34fb3d14366277d2ac62..e07e2f42605ffde307606f4ee027c382ef1e8aa4
(25.051 kB; Blind Hunter floor min(floor(sqrt(25.051) + 1), 10) = 6). Four
independent reviewers ran at the parent model capability: Blind Hunter, Edge
Case Hunter, Verification Gap and Acceptance Auditor. Every finding was
re-verified against the repository, hosted runs and the retained /tmp receipts
before triage.

### Review Findings

The seven `[Review][Patch]` items are resolved by the round-3 review resolution
(2026-10-31) below.

- [x] [Review][Patch] Record the precursor's direct-merge landing as unreviewed [spec-15-5-prepare-the-inactive-policy-precursor.md:381] — human decision 2026-10-30: state the frozen-intent deviation; the landed policy is unreviewed and must not serve as a reviewed prior policy for activation; owner acceptance stays a separate open item. 92eff2e7b1b0f53631c53fa8284b466e202ae921 (`build: merge fix/story-15-5-policy-precursor into main`) is on origin/main; `gh pr list --repo Hexalith/Hexalith.FrontComposer --head fix/story-15-5-policy-precursor --state all` returns none; candidate validation was failing when it merged. The frozen intent requires a draft precursor PR for owner review, forbids merging or bypassing protection, and needs a later real event after the reviewed precursor lands. The records call this an external landing / ancestry observation without recording the deviation or whether the owner's direct merge counts as owner review. The f75d774d281aa6d4764d9576597ff98ecfbaa3a7 policy is now main's committed policy.
- [x] [Review][Patch] Records are stale at their own delivery commit [spec-15-5-prepare-the-inactive-policy-precursor.md:419] — they state no Git mutation, no commit and no push/remote write (this record, the handoff and the release ledger) and name Platform eba576951e61e9763341463516247dcbf8fc0b90 as the current selection, but e07e2f42605ffde307606f4ee027c382ef1e8aa4 delivers them to origin/main and selects Platform a185bfaa5541f8de675fb82e4f553e1e49931201; `python3 eng/dependency_graph.py --root . validate --commit e07e2f42605ffde307606f4ee027c382ef1e8aa4` refuses that owner pin. Append a dated delivery observation to this record, the handoff and the release ledger.
- [x] [Review][Patch] DW-1988's retained-file refusal is still current, not historical [spec-15-5-prepare-the-inactive-policy-precursor.md:439] — retained-source-mapping/pinned-tree/Props/Directory.Packages.props blob dae4eaa21e728002a0304104327e5f4de8fff7c3 (SHA-256 48b3abca2eb67bc09bde33f3f0203e10fbca810614d0582852be4ab54294dc9d, 307 PackageVersion rows) is byte-identical at selected Parties 8b0e23e437296691d0233fe86afc765063f9fef5. `assert_no_inline_versions` raises on the first rejected file and the root Directory.Packages.props sorts first, masking it. Correct the historical wording in all four records and name the retained file in the repair plan.
- [x] [Review][Patch] Resumed receipts are /tmp-only and several new results lack exact commands [spec-15-5-prepare-the-inactive-policy-precursor.md:426] — the f69562a2 raw-diagnostic parameters, the corrected Parties predicate invocation, the nested Builds `fatal: not a tree object` probe and the stale three-scalar preview exist only in /tmp/fc-15-5-resume-review-dqivd22x/, regressing accepted patches BH-1/BH-2. Archive them persistently with a checksum and member manifest, and record the exact invocations.
- [x] [Review][Patch] Intent-required lanes are missing from the resumed lane table [spec-15-5-prepare-the-inactive-policy-precursor.md:390] — no row, result or blocker for clean InfrastructureGovernance/CiGovernance, the actual-parent diff of the current tuple or acquire-catalogs; only validate ran for f69562a2a1b0f11295278c4e715bbf7ecf2999f7. Run them at the current committed source and record exact results or blockers.
- [x] [Review][Patch] Hosted Quality observation superseded before delivery [spec-15-5-prepare-the-inactive-policy-precursor.md:407] — run 41097179475 failed "Prepare exact governance catalog objects" at 17:19:14Z, skipped every Gate 2b–4a step and ended cancelled at 17:20:39Z, before e07e2f42 was committed (17:27:48Z); f69562a2's Quality 41097863471 and CI 41097864370 also ended cancelled. Append the final hosted state.
- [x] [Review][Patch] New deferred block has no section heading [deferred-work.md:11066] — it follows DW-1988 directly without a `## Deferred from:` heading, so the sweep migration's section context attributes it to the 2026-10-29 full-history review. Insert a dated heading above it. (The block stays visible as its own legacy item: the bmad-loop parser ends DW entries at a flat block.)
- [x] [Review][Defer] Story-labelled delivery commit carries an unowned Platform gitlink [commit e07e2f42605ffde307606f4ee027c382ef1e8aa4] — deferred (human decision 2026-10-30): pushed history cannot be rewritten and the validator has no per-commit exception outside Story 13.7; an authorized exception needs its own guard-change story. e07e2f42605ffde307606f4ee027c382ef1e8aa4 (`fix: story 15.5`, on origin/main) also moves references/Hexalith.Platform eba576951e61e9763341463516247dcbf8fc0b90 → a185bfaa5541f8de675fb82e4f553e1e49931201. `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-prepare-the-inactive-policy-precursor.md --candidate e07e2f42605ffde307606f4ee027c382ef1e8aa4` exits 1: `interleaved story commit ... touches unowned paths: references/Hexalith.Platform`. `--unrelated` does not suppress it, and the historical-owned recovery is hardcoded to two Story 13.7 commits. Strict attribution therefore fails for every candidate at or after e07e2f42; the recorded exit-0 strict check ran at f69562a2a1b0f11295278c4e715bbf7ecf2999f7 and does not cover the delivered commit.
- [x] [Review][Defer] Nine selector bumps have no executed Gate 2b–4a evidence [references/* at f69562a2a1b0f11295278c4e715bbf7ecf2999f7 and e07e2f42605ffde307606f4ee027c382ef1e8aa4] — deferred: pre-existing/external; Quality's catalog preparation fails first on the owner-pin guard, so Gate 2c provider verification, Gate 3a and Gate 4 ApiCompat are skipped for the bumped tuple. Closing it needs separately authorized seal renewal or gate reordering.
- [x] [Review][Defer] Story 16.1 findings BH-R4–BH-R10 were rejected without a ledger pointer [spec-15-5-prepare-the-inactive-policy-precursor.md:373] — deferred: their fixes belong to the independently owned, in-progress Story 16.1 spec, which still has no File List; recorded for its owner.

#### Rejected

- Landing commit 92eff2e7 broke main's ordinary validation — false: pre-merge main 0873b5fdd9bcba876638a1dd198b616358023102 already fails `validate` on the same Platform deee5d2f8633ea77527c3151f52a56bf4f2656e4 under the old seal. The governing-policy concern is carried by the merge decision above.
- BH-R3's rejection rationale contradicts this run's spec edits; the final-results annex lacks a tracked checksum — low, carried: repeats round-2 rejected BH-R3.
- No authorization path for acquiring the missing nested Builds catalogs — false: the deferred entry conditions acquisition on permitted metadata, and the exact authorized fetch form is tracked in this record's catalog-acquisition section.
- `review_loop_iteration` stays 0 after a resumed review — false: bmad-build step-04 increments it only before intent_gap/bad_spec loopbacks, not for code-review rounds.


## Round-3 review resolution — 2026-10-31

This continuation resolves the seven round-3 `[Review][Patch]` items above.
Starting HEAD and origin/main are 188d8b733090a83108bd006aaa9fa71cd9a5a2d8,
actual parent a55960f5441239410107bda3d547b381fdfa6f3a. Every lane ran from an
isolated clean clone of that HEAD with standalone clones of only the nine
root-declared dependencies at its committed gitlinks; no nested repository was
initialized. Earlier dated observations stay as written apart from the two
DW-1988 wording corrections named below.

### Unreviewed direct-merge landing

The precursor deviated from its frozen intent. That intent required a draft
precursor PR for owner review once candidate validation passed, and forbade
merging or bypassing protection. Instead, branch `fix/story-15-5-policy-precursor`
landed on main through direct merge commit
92eff2e7b1b0f53631c53fa8284b466e202ae921 (`build: merge
fix/story-15-5-policy-precursor into main`, 2026-10-30T15:34:17Z), while ordinary
candidate validation was failing. At 2026-10-31T16:03:01Z, and again in the
archived receipts at 16:03:31–16:03:32Z,
`gh pr list --repo Hexalith/Hexalith.FrontComposer --head fix/story-15-5-policy-precursor --state all --json number,state,title`
returns `[]` and `gh api repos/Hexalith/Hexalith.FrontComposer/commits/92eff2e7b1b0f53631c53fa8284b466e202ae921/pulls --jq length`
returns `0`: no PR ever carried the precursor.

Per the 2026-10-30 human decision, the landed f75d774d281aa6d4764d9576597ff98ecfbaa3a7
policy is unreviewed. It must not serve as a reviewed prior policy for any
activation event, and the direct merge is not recorded as owner review. Owner
acceptance of that policy remains a separate open item. Later policy commits
(3725144b19cf3453fe1f607f9711c1d33743cc60 through
126c43f670cfa3a36f31e9281271f54d8779b322) have since replaced the f75 projection
row. Their review status belongs to their own records; this record grants them
nothing.

### Delivery observation for the resumed records

The 2026-10-30 resumed records were stale at their own delivery commit. They state
that the resumed run made no Git mutation, commit or remote write, and they name
Platform eba576951e61e9763341463516247dcbf8fc0b90 as the current selection. Commit
e07e2f42605ffde307606f4ee027c382ef1e8aa4 (`fix: story 15.5`,
2026-10-30T17:27:48Z, parent f69562a2a1b0f11295278c4e715bbf7ecf2999f7) then
delivered this record, the handoff, the release ledger and the deferred ledger to
origin/main. The same commit moved references/Hexalith.Platform to
a185bfaa5541f8de675fb82e4f553e1e49931201; its strict-attribution consequence stays
deferred as recorded above.

`python3 eng/dependency_graph.py --root . validate --commit e07e2f42605ffde307606f4ee027c382ef1e8aa4`
exits 1: `github.com/hexalith/hexalith.platform@a185bfaa5541f8de675fb82e4f553e1e49931201: consumer source projection owner pin changed`.
Hosted, both attempt 1:

- [CI 41099330117](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41099330117) — completed/failure. Dependency-governance job 124216571609 failed "Collect exact graph diff and affected-module proof" at 17:39:59Z.
- [Quality 41099329667](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41099329667) — completed/cancelled at 17:59:56Z. Build-and-test job 124214027080 failed "Prepare exact governance catalog objects" at 17:40:37Z; Gate 1, 2 and 2a passed, every Gate 2b–4a step was skipped.

### Final hosted state of the superseded observations

The 17:17–17:19 UTC in-progress observation above was superseded before delivery:

- [Quality 41097179475](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41097179475) (dd575b67c82c377bd17c34fb3d14366277d2ac62, push, attempt 1) — build-and-test job 124205767715 failed "Prepare exact governance catalog objects" at 17:19:14Z and skipped every Gate 2b–4a step, including all Gate 2c steps; the run ended completed/cancelled at 17:20:39Z, before e07e2f42 was committed.
- [Quality 41097863471](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41097863471) (f69562a2a1b0f11295278c4e715bbf7ecf2999f7, attempt 1) — build-and-test job 124208616695 failed the same step at 17:27:10Z, skipping Gate 2b–4a; completed/cancelled at 17:31:25Z.
- [CI 41097864370](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41097864370) (same source, attempt 1) — all four jobs, including dependency-governance 124213535116, were cancelled at 17:30:25Z; completed/cancelled at 17:30:27Z.

None of these runs supplies a governance verdict for its source.

### DW-1988 correction

The 2026-10-30 records called DW-1988's retained-file refusal historical at
Parties 8b0e23e437296691d0233fe86afc765063f9fef5. That was wrong. The retained
`_bmad-output/implementation-artifacts/tests/five-reconciliation-and-migration-2026-10-28/retained-source-mapping/pinned-tree/Props/Directory.Packages.props`
is still there as blob dae4eaa21e728002a0304104327e5f4de8fff7c3 (SHA-256
`48b3abca2eb67bc09bde33f3f0203e10fbca810614d0582852be4ab54294dc9d`, 307
PackageVersion rows). `assert_no_inline_versions` raises on the first rejected
tracked file, and the root Directory.Packages.props sorts first, so the root
refusal masked it. The archived `round3-resolution-20261031/parties-predicate.py`
runs the unchanged predicate twice. First it runs once over every tracked file,
exactly as validation does. Then it runs once per tracked file, with the file
listing narrowed to that path, so every rule applies and no earlier file masks a
later one. Run from the clean clone's root:

- `python3 parties-predicate.py e07e2f42605ffde307606f4ee027c382ef1e8aa4 e2528a5d5853641f2b38e5e2e34bb02c0cc4be23 8b0e23e437296691d0233fe86afc765063f9fef5` — exit 0 (diagnostic; receipt `parties-predicate-e07-policy-v2.log`). Under parties-catalog-v1 (.csproj/.props/.targets), e2528a5d's 48 tracked files reject only the retained file. 8b0e23e4's 50 files reject both the root file and the retained file.
- `python3 parties-predicate.py 188d8b733090a83108bd006aaa9fa71cd9a5a2d8 9e29b203e1c5af9d92ef8172a507843e6cb0e068` — exit 0 (`parties-predicate-head-policy-v2.log`); none of HEAD's selected Parties' 53 files is rejected.
- The earlier `parties-predicate-*-policy.log` receipts came from a first script version. Its per-file list re-implemented only the ItemGroup/PackageVersion rule, so the v2 runs supersede them; they reported the same files.

Upstream state since then: Parties d651173dc4b9b121217a28a97b3090ddf9e60960
(`fix: inherit package versions from the shared catalog`, 2026-10-30T18:08:01Z)
removed the root Contracts.UI fallback. It also renamed the retained file to
`…/Directory.Packages.props.snapshot` with the same blob, which puts it outside
the scanned extensions. FrontComposer fa30a34785b27a1a95903e35a7a1c1507426a5ab
sealed Parties 6b8f0f8637c9a3bc7596794e2f819e5015e40e8e, a descendant of that
fix. Ordinary validation at 126c43f670cfa3a36f31e9281271f54d8779b322 validates
Parties 9e29b203e1c5af9d92ef8172a507843e6cb0e068 under parties-catalog-v1.
Whether renaming the retained file's extension satisfies DW-1988 is for its
owners to decide; this record does not close DW-1988. The four records
are corrected in place here and in the deferred ledger, and by dated corrections
in the handoff and release ledger.

### Persistent receipts and exact invocations

The 2026-10-30 resume receipts and this resolution's receipts are archived
outside temporary storage:

- Archive: `/home/administrator/.local/state/hexalith/frontcomposer/story-15-5-policy-precursor-20261031/round3-receipts.tar.gz`.
- SHA-256: `55b4d00df5d68170868d60734d929bbc87cbb858387e22625733723eb45c24e0`.
- 66 members plus `receipt-members.json`, which lists each member's byte count and SHA-256; every hash was verified against extracted bytes. `resume-review-20261030/` is a byte copy of `/tmp/fc-15-5-resume-review-dqivd22x/`. `round3-resolution-20261031/` holds `commands.jsonl` (argv, CWD, UTC start/end, exit), every lane log, the hosted-API and failed-step-log receipts, `run.py`, `parties-predicate.py`, `projection-field-drift.py` and `setup-clean-checkout.sh`.
- Verify with `sha256sum <archive>`; list with `tar -tzf <archive>`; extract with `tar -xzf <archive> -C <existing-directory>`.

Exact invocations of the 2026-10-30 resume results:

- f69562a2 raw diagnostic: `python3 /tmp/fc-15-5-resume-review-dqivd22x/inspect-latest-source-tuple.py`, run from the workspace root `/home/administrator/projects/hexalith/frontcomposer`. That CWD is evident from the store paths in its log. The retained script fixes source f69562a2a1b0f11295278c4e715bbf7ecf2999f7, authorized-root example b1d89648ae6feed1c89e48c265de013e1507fa97 and base dd575b67c82c377bd17c34fb3d14366277d2ac62, and writes `source-tuple-latest.json`. The original argv was not logged; this invocation is reconstructed from the retained script and log. To rerun it, extract the archive and run `resume-review-20261030/inspect-latest-source-tuple.py` from a FrontComposer root whose root stores hold those revisions. First point its hard-coded `scratch` path at an existing directory.
- Corrected Parties predicate: the 2026-10-30 invocation was not retained, only its JSON results. It is replaced by the archived `round3-resolution-20261031/parties-predicate.py` v2 runs above. They reproduce both earlier first refusals and add the per-file view. Run the extracted script by path, with the CWD set to a FrontComposer root (here the clean clone created by `setup-clean-checkout.sh`); its exact argv and CWD are in `commands.jsonl`.
- Nested Builds probe: the 2026-10-30 `fatal: not a tree object` came from `git ls-tree <commit> -- Props/Directory.Packages.props` (`dependency_graph.read_blob`) in the workspace's references/Hexalith.Builds store, as logged in `inspect-latest-source-tuple.log`. Rerun on 2026-10-31 in the clean clone's Builds store, `git ls-tree cc0db51b800be1d0d5badc3780395cfb5953d0f5 -- Props/Directory.Packages.props` and the same for 69701dfcf54cb9c69fc6cb0c51b031af8d218142 each exit 0 with blob 52c61818bc0a0f62489cb8bf91de143d73a7d07e. Both commits are now on Builds origin/main. Their availability today is no evidence about the earlier observation time.
- Stale three-scalar preview: `proposed-current-platform-reseal.diff` (owner 9e01ffacc45cb6a5ee12853d44f4b7f6e1f4084c) is archived as stale and was never applied.

### Intent-required lanes at the current committed source

All commands ran from the clean clone's root, created by `setup-clean-checkout.sh`
at the ephemeral session scratch path
`/tmp/claude-1000/-home-administrator-projects-hexalith-frontcomposer/9ed344d0-14bf-42c2-b4db-4061a520f965/scratchpad/fc155/checkout`.
The clone is clean in its working trees and gitlink selections, but not in its
objects. `git clone --shared` borrows the original workspace's object stores
through alternates. Those stores had fetched every root dependency's origin/main
earlier that day, so the `acquire-catalogs` pass is not a cold-network
acquisition claim. Global NuGet and npm caches are shared too.

| Lane | Command | Result at 188d8b733090a83108bd006aaa9fa71cd9a5a2d8 | Blocker |
| --- | --- | --- | --- |
| Ordinary committed source | `python3 eng/dependency_graph.py --root . validate --commit 188d8b733090a83108bd006aaa9fa71cd9a5a2d8` | Failed, exit 1 | Collection: `consumer projection root source selectors changed` |
| Actual-parent transition | `python3 eng/dependency_graph.py --root . diff --event push --event-base a55960f5441239410107bda3d547b381fdfa6f3a --candidate 188d8b733090a83108bd006aaa9fa71cd9a5a2d8` | Failed, exit 1 | Same refusal; validation of base a55960f5 itself exits 1, so the diff stops before candidate semantics |
| Exact catalog preparation | `python3 eng/dependency_graph.py --root . acquire-catalogs --event-base a55960f5441239410107bda3d547b381fdfa6f3a --candidate 188d8b733090a83108bd006aaa9fa71cd9a5a2d8` | Passed, exit 0; seven exact catalog commits prepared in the clone's root Builds store | None |
| Clean infrastructure/CI governance | `npm ci --ignore-scripts --no-audit --no-fund` (exit 0, 352 packages); `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --nologo -m:1 -nodeReuse:false --disable-build-servers -p:NuGetAudit=false` (exit 0, zero warnings/errors); `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests -class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests` | Failed, exit 1: 109 total, 108 passed, one failed, zero skipped/errors | Sole failure `CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds`, on the same root-selector refusal |

The blocker is external to this precursor. Ordinary validation passes, with
eight Builds selectors validated, at 34d0afde14c43b6079208c47f95297d0cf99b51d
and at the last policy seal, 126c43f670cfa3a36f31e9281271f54d8779b322. Commit
a55960f5441239410107bda3d547b381fdfa6f3a (`fix(build): resolve reviewed workspace
dependencies`, 2026-10-31T15:22:25Z) then moved root Builds
a22b55f0a4f6d2aaa3a5ccc505896064ac595364 → bb1b71ebc2b966b50b974e624e55cf540ad8a29c.
It also changed `Directory.Packages.props` and `deps.local.props`, and renewed
no seal.

A raw comparison against the Platform projection row (receipt
`projection-field-drift.log`) shows three sealed fields drift at a55960f5 and
HEAD. In `evaluate_consumer_projections` order they are `root_selectors_sha256`,
`builds_commit` (still a22b55f0) and `consumer_configuration_sha256`. All three
match at 126c43f6. Only the first refusal is reported because the evaluation
stops there, so the renewal must cover all three fields.

Hosted failed-step logs (receipts `hosted-log-failed-<run>.log`) show the same
refusal:

- CI [41214552453](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41214552453) (a55960f5) and [41215250922](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41215250922) (HEAD) failed dependency-governance with `consumer projection root source selectors changed`.
- Quality [41215250572](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41215250572) (HEAD) failed "Gate 2b: Infrastructure governance and telemetry contracts" in job 124578671464 and skipped the remaining Gate 2b–4a steps. Its log reports one Shell.Tests error, `CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds`, with the same message.
- Quality [41214551987](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41214551987) (a55960f5) has the same Gate 2b refusal and ended cancelled.

Renewing those seals is its own seal transition and is not attempted here; a
deferred entry records it. No current infrastructure, ApiCompat, hosted or
activation success is claimed. This resolution grants no publication, owner,
Fluent, Product, EventStore or GOV-1 approval. Story 16.1, every original
baseline and every independent approval keep their recorded dispositions.

### Round-3 resolution checks

Final run in the original workspace after every edit, including the review
patches and the sprint-status synchronization:

- `python3 eng/validate-story-artifacts.py` — exit 0, `Story artifact validation passed.`
- `git diff --check` — exit 0. All five edited files remain CRLF-only.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-prepare-the-inactive-policy-precursor.md --candidate 188d8b733090a83108bd006aaa9fa71cd9a5a2d8 --unrelated references/Hexalith.Builds --unrelated references/Hexalith.EventStore --unrelated references/Hexalith.Memories --unrelated references/Hexalith.Parties --unrelated references/Hexalith.Platform --unrelated references/Hexalith.Tenants --reason 'User asked to fast-forward the root-declared submodule checkouts to origin/main before this run; no gitlink is committed.'` — exit 1. Four Story 15.5 commits are `interleaved`. e07e2f42605ffde307606f4ee027c382ef1e8aa4 carries the already-deferred unowned Platform gitlink. fc17308d969129edc461c451e16dedacf777b2b0 (`docs: record story 15.5 as in progress`) touches this spec and `sprint-status.yaml`. It was interleaved in the first run and became `owned` once the status sync added `sprint-status.yaml` to this File List. decbb195f1c010460530fde6b662a6118a83ee7c, eb1f0f7158b254dfacf460b2e30199ac8912944f and 188d8b733090a83108bd006aaa9fa71cd9a5a2d8 deliver the sibling 4.7.2 spec and its review. The existing high deferral covers only e07e2f42. No deferral covered these sibling-spec interleavings, because the validator attributes by story ID rather than by spec; a new deferred entry records that. The six documented-unrelated dependency checkouts are reported as such.


## Review Triage Log — round-3 resolution, 2026-10-31

The oneshot route ran Blind Hunter only; Edge Case Hunter and Verification Gap
were skipped. One context-free reviewer at the parent model capability reviewed
the changed worktree: 47.875 kB of diff, floor min(floor(sqrt(47.9) + 1), 10) = 7.
It returned 14 findings, each checked against the code, receipts and Git history.

- BH-31-1 — medium / patch: the reseal scope was understated. `projection-field-drift.log` shows that `root_selectors_sha256`, `builds_commit` and `consumer_configuration_sha256` all drift at a55960f5 and HEAD and all match at 126c43f6; `evaluate_consumer_projections` reports only the first. All four records now name the three fields.
- BH-31-2 — false / low / reject: the requested DW id, owner and completion criteria for the new high entry are false findings. The workflow prescribes the flat `source_spec`/`summary`/`evidence` defer format, and the sweep migration assigns DW coordinates. The malformed `validate --commit` command was real (low) and is patched to `--commit <sha>`. A cross-link inside DW-1988 is rejected (low): it would rewrite an old entry, and the new entry already names DW-1988.
- BH-31-3 — low / reject: the resumed deferred entry's nested-catalog, Parties and tuple statements were true when observed. Only its DW-1988 sentence was wrong when written, and that sentence is corrected. The sweep re-verifies entries against current code, and the new entries record the overtaken state.
- BH-31-4 — low / patch: added a superseded-in-part pointer at the top of the resumed section and a resolution pointer under the round-3 findings.
- BH-31-5 — low / patch: the landing check now cites both the manual observation (16:03:01Z) and the archived receipts (16:03:31–16:03:32Z).
- BH-31-6 — low / patch: "eight selectors" now reads "eight Builds selectors validated". `selectors_validated` counts Builds-selector edges, not the nine depth-1 root selectors.
- BH-31-7 — medium / patch + defer: fc17308d is now described correctly (this spec plus `sprint-status.yaml`). The claim that the existing deferral covered sibling commits is withdrawn, and a new deferred entry records sibling-spec strict attribution.
- BH-31-8 — medium / patch: v1's per-file list re-implemented one rule. v2 runs the unchanged predicate once per tracked file and reproduces the same results; its receipts supersede v1's.
- BH-31-9 — low / patch: the hosted cause was correct but uncited. Archived failed-step logs show the same message; HEAD's Gate 2b reports one Shell.Tests error.
- BH-31-10 — medium / patch: the record now says the `--shared` clone borrows the original object stores (fetched that day), so `acquire-catalogs` is not a cold-network claim. It also records the ephemeral clone path and the shared NuGet/npm caches.
- BH-31-11 — low / patch: the fast-forwarded checkout SHAs are recorded. The delivery stages only File List paths, so no gitlink move is committed.
- BH-31-12 — medium / defer: owner acceptance of the unreviewed precursor policy, and which policy may serve as the reviewed prior for activation, are now a deferred entry. Settling them needs the owner/protection process.
- BH-31-13 — low / patch at finalize: this spec moves to `done`, because its bounded patches are resolved and its remaining blockers are external and deferred. Sprint-status gets a dated comment replacing the now-false seven-open-patches reason.
- BH-31-14 — low / patch: the record now gives the archived member paths and the CWD for both diagnostics; "closed" is corrected to "resolves" in the handoff.

The 2026-10-30 note that the done step is not reached while required gates fail
applied to that run's own candidate gates. The precursor proposal has since been
overtaken: it landed unreviewed and was re-sealed by later commits. The gates
that still fail belong to the external a55960f5 seal gap. Story 15.5 stays open.
