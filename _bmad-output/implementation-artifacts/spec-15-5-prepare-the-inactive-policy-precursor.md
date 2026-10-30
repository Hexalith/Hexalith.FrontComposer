---
title: 'Story 15.5: Prepare the Inactive Policy Precursor'
type: 'chore'
created: '2026-10-30'
status: 'in-progress'
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
