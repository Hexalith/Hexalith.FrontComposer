# Approved canonical Platform provenance amendment

Status: **approved in chat on 2026-10-28; implementation verified in the working candidate; parent audit/review pending**. The owning specification records the explicit frozen-intent amendment. The current candidate's full disposable runtime passes; source-governance acceptance remains incomplete until the approved implementation and review are verified.

## Exact current boundary

The approved root dependency is `references/Hexalith.Platform` at `b742b1b7a496ac72d6961ca6db5213d3df42d234`. The AppHost consumes only these unchanged canonical files:

| Compile link | SHA-256 of the selected Git blob |
| --- | --- |
| `DaprSelfHostedMtls.cs` | `703072feb8b609c2661a1c7eaa955fbd2c9d838402520b4fc425bda2000ba1a8` |
| `DaprSelfHostedMtlsSubscriber.cs` | `55c280284d0a305b1d6b6667f31362b7c4e4a3cf39890b6175d913cd762bb9c6` |

The only technical source dependency consumed by those files is the existing root `Hexalith.Builds.Tooling` project at Builds `81ab049208678bd6616828824729c0ddac02f7d1`. Keep that current root selection; Platform's historical nested Builds selector is raw provenance, not permission to adopt its catalog or initialize its checkout.

[GOV-1](gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md) AD-1 fixes the complete depth-one/depth-two boundary; AD-4 fixes edge identity/order. AD-2/AD-10/AD-12 exact-object collection, isolated acquisition and delayed active-policy application remain binding. Identity is the trust key; paths are consumption/graph evidence, not repository trust authority. `eng/dependency_graph.py` currently expands every root dependency's `.gitmodules`; `_owner_edges` refuses every unknown identity. There is no existing leaf/source-projection exception. Actual `validate --commit c49412ed567ac47e10a1cceec100d2c41505cf45` therefore fails closed on `github.com/hexalith/hexalith.agents`. No nested source was initialized, acquired or executed.

## Raw canonical nested inventory — must remain visible

Selected `.gitmodules` SHA-256: `6da4f5297d79bfa54afcb8daacda70670af11868ba1738cc185f584db8eb6ab7`. All 17 depth-two mappings must remain in raw provenance and its canonical digest; seven identities are outside current global trust.

| Raw path | Exact Gitlink | Current global trust |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` | Existing |
| `references/Hexalith.Agents` | `703345f2e31148f44498e6673b00447c816f8dab` | Untrusted |
| `references/Hexalith.Builds` | `51a71cb1cfe860d5fc135eb3b8521fd80c413810` | Existing |
| `references/Hexalith.ChatBot` | `f568d43e99d2779e8d81bcd49552c7441d34de69` | Untrusted |
| `references/Hexalith.Commons` | `1663028fb6d9f46a0bf17919edeb21bb99be1587` | Existing |
| `references/Hexalith.Conversations` | `c71e6ba924f42e7f5da62116f9e3b4fd5d77227e` | Untrusted |
| `references/Hexalith.EventStore` | `eff7066ed5c8c7ec57e1b42862fddee479044134` | Existing |
| `references/Hexalith.Folders` | `4d166c77df9c46e0600e5a22bb097a7385eb75b7` | Untrusted |
| `references/Hexalith.FrontComposer` | `5688c3eba98567133285b0f51843d9daa8f24f0b` | Existing |
| `references/Hexalith.McpCli` | `8d4a8c07636c1ad065dfc8dc28dbfc956d5deb82` | Existing |
| `references/Hexalith.Memories` | `790670897c1fc684d41b980fc5e40934be796122` | Existing |
| `references/Hexalith.Parties` | `94f328fcf1a4c2a2b96dd962d89ad8a94d756203` | Existing |
| `references/Hexalith.PolymorphicSerializations` | `221ac4592ba908ae7661ba1fa9e2d1fcc23baee0` | Existing |
| `references/Hexalith.Projects` | `515d49c93fc0544b4f49c1662578f348781c0320` | Untrusted |
| `references/Hexalith.Tenants` | `1436044bf6452bbc5bdb2eaa8b585ab2ce9362ca` | Existing |
| `references/Hexalith.Timesheets` | `69f3622b17de70bf3e9043f96577f2b6d1a4cbd9` | Untrusted |
| `references/Hexalith.Works` | `15d14fd82a92bbd277fe9394cd0b77281750de20` | Untrusted |

## Smallest proposed contract change

Introduce a closed, separately validated **consumer source projection**, bound to the exact Platform owner identity/revision, both canonical source hashes, the actual existing root Builds source revision and the complete raw nested inventory/digest. Preserve the complete raw v1 depth-one/depth-two inventory, every edge identity/order and its canonical digest. The projection would explicitly distinguish the two consumed Compile links plus existing root Builds.Tooling from the unconsumed umbrella inventory; it must not turn the seven identities into globally trusted or materialized dependencies. All seven identities remain globally untrusted, with no acquisition or execution. Unknown identities outside the sealed context still fail before acquisition. Collection, consumer validation and delayed policy activation must agree; additional Compile/ProjectReference consumption fails closed. No nested/source acquisition or execution is proposed.

This needs an explicit amendment to the v1 graph/evidence/acquisition contract. It is not a config-only exception, a dropped-edge optimization, a broad identity allowlist or an authority granted by paths. Preserve identity as the trust key, delayed active-policy behavior, owner/pin drift refusal and complete raw provenance. No implementation of this new authorization/projection primitive is included in the current candidate.

Exact implementation/review surface if approved:

- `eng/dependency-graph-policy.json`: closed owner/revision/consumed-source inventory and activation data, preserving every current trusted identity and all package/catalog requirements except the two exact semantic expectation changes separately proposed below.
- `eng/dependency_graph.py`: closed projection parsing/validation plus collection, canonical evidence, acquisition and affected-proof handling; preserve the existing raw graph and global refusal contract.
- `tests/eng/test_dependency_graph.py`: exact positive projection and negative changed owner, pin, path, URL, nested target, inventory set, canonical-file hash or additional Compile/ProjectReference consumption; unknown-owner identities still refuse; no unconsumed source acquisition/execution; deterministic raw inventory/digests and delayed-policy/diff/envelope regressions.
- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md` and its sibling `.memlog.md`: propose the explicit contract decision and recorded limits; neither artifact is changed by this proposal.
- The owning GOV-1 contract and Story 13.6 spec/result: explicitly record the contract amendment and its independently verified limits before changing active authority.

Keep AppHost/authentication/runtime behavior, current root Gitlinks, SDK/package/catalog pins and Release/package selection unchanged. No further full runtime is proposed merely for bookkeeping; any implementation that changes consumed code must receive its own appropriate verification.

## Separate preserved catalog gate

Read-only comparison at selected Builds `81ab049208678bd6616828824729c0ddac02f7d1` finds `Microsoft.Extensions.Localization` and `System.Collections.Immutable` at `10.0.14`, while the preserved FrontComposer policy requires `10.0.13`. Actual graph validation stops earlier at the untrusted identity; this is a separate measured row comparison, not a later successful CLI stage. No version alignment or new trust has been applied. Synthetic semantic fixtures normalize their own catalog rows to the unchanged policy so their intended assertions remain isolated; 109 tests pass.

The [owning result](13-6-accessibility-result.md) and [owning spec](spec-13-6-qualify-full-topology-readiness.md) retain full runtime evidence, failed attempts, original baseline and independent remaining gates.

Authoritative contract artifacts for that future decision are the [GOV-1 spine](../planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md) and its [memory log](../planning-artifacts/architecture/architecture-gov-1-2026-07-19/.memlog.md). Their current rules remain unchanged.

## Separately proposed semantic expectation alignment — unapplied

The combined approval proposal includes only these additional expectation edits in `eng/dependency-graph-policy.json`, matching the already-selected Builds `81ab049208678bd6616828824729c0ddac02f7d1` catalog:

| Exact JSON location | Current expectation | Proposed expectation |
| --- | --- | --- |
| `profiles.frontcomposer-catalog-v1.selected_catalog_required_packages.Microsoft.Extensions.Localization` | `10.0.13` | `10.0.14` |
| `profiles.frontcomposer-catalog-v1.selected_catalog_required_packages.System.Collections.Immutable` | `10.0.13` | `10.0.14` |

These are policy expectations only. Do not change an actual package, catalog, SDK, Gitlink, production setting, any other expectation or other approved baseline. The current candidate keeps both expectations at 10.0.13 and therefore does not claim this gate is resolved. Keep this proposal separate from consumer-projection identity/inventory evidence.

Existing evidence supports the actual selected 81ab inputs: the Debug AppHost/Shell builds are warning/error free; 222 owned assembly references match exact identities/versions; the full c2tg0uhi disposable proof passes 10/9 resources, real mTLS boundaries, bootstrap/status/fresh membership, 3/3 authenticated navigation, four 401 routes and unchanged live proof 1/1. That evidence does not self-authorize a policy change.

If approved, verify exact selected-catalog compatibility at both changed rows, negative older/wrong/missing package versions and unchanged remaining expectations, plus the complete source-projection/semantic/envelope/acquisition regressions. Apply GOV-1 AD-12 delayed activation: collection, consumer validation and policy activation must agree on the approved contract and active policy revision; a candidate cannot authorize its own changed expectations or trust. No policy alignment or new authorization is implemented in this handoff.


## Approval record — 2026-10-28

The user explicitly approved this combined bounded amendment in chat. Earlier proposal-only/unapplied statements describe the pre-implementation state; they do not require another approval. Implement the exact source-projection contract and the two semantic expectation edits above, preserving delayed activation, raw inventory, global refusal, all actual pins and runtime behavior. The owning spec is the implementation source of truth; no commit/push/publication is authorized.


## Implementation disposition — 2026-10-28

The approved changes are now implemented in `eng/dependency_graph.py`, its policy and regression tests, the GOV-1 spine/memory log and owning contract. Earlier proposed/unapplied statements above are historical. The new closed projection retains raw v1 edge shapes/all 17 Platform mappings and their digest, seals owner/commit/two canonical source hashes and exact consumer/root Builds source, enforces an evidence-only owner, materializes only two source blobs and refuses changed/extra consumption before execution. Automatic `.user`/`.rsp` inputs are included. All seven inventory identities remain globally untrusted, with no acquisition/materialization/execution; existing trusted exact nested Builds metadata/catalog remains required. Only the two approved semantic expectations changed 13→14; no actual version/pin/runtime changes occurred.

Engine 126/126, handoff 11/11 and release-evidence 15/15 checks pass. Actual committed blobs verify raw 69/all 17, both canonical hashes and the nested 51 catalog digest while binding helper source to root 81ab; current working configuration and exact 14 expectations pass component diagnostics. Strict c494 committed-object/configuration and old-active-policy checks correctly refuse: the working candidate cannot activate its own policy under AD-12. Owner landing and a later base remain required for active acceptance; no commit/push/publication or synthetic actual-root acceptance was created. Full commands, logs, expected refusals, input attribution and remaining parent audit/review are appended to the [owning spec](spec-13-6-qualify-full-topology-readiness.md) and [result](13-6-accessibility-result.md). The successful c2tg0uhi runtime is unchanged and not rerun.
