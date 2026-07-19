---
created: 2026-07-19
updated: 2026-07-19
story: GOV-1
owner: Product Owner + Architect + Developer + Release Owner
source_proposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19.md
decision: _bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md
status: ready-for-dev
scope: moderate
implementation_risk: high
priority: before Story 11.17d promotion and the next accepted governed release manifest
baseline_commit: e3e3dcf592fd7fa962c559e6e9fee034427cbe32
upstream_follow_up: BUILD-CAT-1
implementation_entry_gate: fc-dep-1-graph-boundary-ratification
---
# GOV-1: Validate Shared-Catalog Compatibility and Seal Dependency Provenance

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->
<!-- Type: cross-cutting governance and release-provenance correction. -->

## Story

As a framework maintainer and Release Owner,
I want compatibility validated from the catalogs selected by actual gitlinks while exact identities are sealed as provenance,
so that legitimate pointer advances remain reviewable and reproducible without false-red SHA pins.

## Why This Story Exists

The current Governance lane mixes two different responsibilities. It correctly checks semantic shared-catalog requirements, but it also hard-codes historical root, EventStore, Memories, and Parties `Hexalith.Builds` commit IDs. A compatible gitlink advance therefore fails before the catalog selected by that gitlink is evaluated. At the same time, release evidence does not seal the selected dependency graph or the raw shared-catalog bytes, so deleting the SHA assertions without adding provenance would weaken reproducibility.

GOV-1 separates those concerns:

- compatibility is determined from semantic package/import/ownership requirements in the catalog selected by each in-scope actual gitlink;
- exact repository, commit, path, and catalog-byte identities are recorded as deterministic provenance, diffed in CI, sealed in the governed release manifest, and verified again before and after publication;
- fingerprints remain evidence and drift detectors, never compatibility allowlists.

The creation baseline is `e3e3dcf592fd7fa962c559e6e9fee034427cbe32`. Despite its subject, that commit added planning artifacts and gitlink updates; it did not implement GOV-1.

## Implementation Entry Gate — Resolve Before Editing Production, Tests, Workflows, or Manifest Code

The approved artifacts disagree about the boundary of `hexalith.dependency-graph.v1`:

- the PRD/proposal language describes root and direct nested gitlinks;
- FC-DEP-1, the final Epic GOV wording, and architecture language require the complete reachable graph, keyed by `(repository identity, commit)` with cycle detection.

This is behaviorally material. A read-only census of the root plus each exact root-selected module's direct gitlinks is stable: 8 root edges plus 32 direct nested edges, 40 total, including 7 `Hexalith.Builds` selectors resolving to 5 distinct Builds commits. Independent full historical recursions produced different totals and exposed legacy/unmapped identities because older commit trees contain back-references and gitlinks that cannot be resolved from the current root identity map.

**Hard gate:** before Task 2 or any production/test/workflow/manifest edit, the Architect and Release Owner must ratify or amend FC-DEP-1 with the exact v1 boundary and canonicalization semantics.

Recommended v1 decision:

1. Bound the graph to FrontComposer's eight root gitlinks and the direct gitlinks contained in each exact root-selected module commit: depth 1-2, creation-time census exactly 40 edges.
2. Treat deeper historical back-references as outside v1, while retaining the generic cycle-safe model for a separately approved future schema.
3. If complete transitive historical recursion remains required, keep implementation blocked until legacy identity/object resolution, unresolved-edge policy, traversal/resource limits, and census reconciliation are approved and represented by fixtures.

Record the approved choice in FC-DEP-1 or a dated superseding decision artifact, update this story's counts/fixtures if necessary, and only then continue. Do not infer the boundary from the easiest implementation or silently choose between the conflicting approved texts.

## Acceptance Criteria

1. **Validate every selected catalog semantically, without a commit allowlist.** **Given** an approved `hexalith.dependency-graph.v1` boundary, **when** governance evaluates an explicit FrontComposer root commit, **then** it walks every in-scope gitlink edge from committed Git objects with deterministic ordering and cycle handling; reads `Props/Directory.Packages.props` from every distinct actual `Hexalith.Builds` commit selected by those edges; validates the applicable package, import, ownership, and optional-marker contract; and contains no expected historical 40-hex Builds SHA allowlist. Each distinct Builds commit is validated once while every selecting edge remains present in evidence.

2. **Compatible pointer advances pass and remain reviewable.** **Given** a gitlink advances to a catalog whose semantic contract remains compatible, **when** Governance and CI run, **then** compatibility passes. The changed repository/commit/catalog identity appears only in the deterministic dependency-graph diff and sealed provenance; a fingerprint difference alone cannot reject it.

3. **Incompatible or unreadable catalogs fail precisely.** **Given** an in-scope Builds edge selects a missing catalog, malformed XML, missing/duplicate/conditional/overridden required package declaration, changed required version, broken import/ownership rule, or an approved mandatory marker mismatch, **when** validation runs, **then** it fails closed with the owning repository, owning commit, gitlink path, selected Builds commit, catalog path, and precise semantic mismatch. A marker remains optional until separately approved as mandatory.

4. **Gitlink changes receive exact, affected-module CI proof.** **Given** a pull request changes a root or in-scope nested gitlink, **when** primary CI compares the explicit merge-base graph with the explicit candidate graph, **then** it emits deterministic added/removed/changed edge and catalog evidence and restores/builds every affected supported module once from its exact candidate commit in standalone Release/NuGet mode. The gate is release-blocking, uses a static approved module-command map, initializes no recursive or nested submodules, executes no candidate-supplied command, and does no redundant module build for an unchanged graph.

5. **Governed release evidence seals and re-verifies the complete approved graph.** **Given** `prepare-manifest`, `seal-manifest`, offline fixture verification, live pre-publish verification, and post-publish verification, **when** the manifest is processed, **then** the sealed payload binds the v1 schema, explicit root repository/commit, every defined edge, each selected Builds commit, raw catalog SHA-256, deterministic graph digest, and any approved semantic-contract version. Verification fails closed on missing, extra, duplicate, malformed, nonterminating, unresolved, out-of-order, unavailable-object, root-commit mismatch, live graph drift, raw catalog drift, or graph-digest mismatch. Existing artifact checksums, signatures, timestamps, attestations, seals, helper/package/fallback fingerprints, classification, incident handling, and freeze controls remain intact.

6. **Ownership and migration are explicit.** **Given** catalog authorship belongs to `Hexalith.Builds`, **when** GOV-1 lands, **then** BUILD-CAT-1 is durably routed upstream for any desired catalog marker/contract-version addition. FrontComposer validates the semantic catalog content directly during migration, carries no fingerprint allowlist, does not edit submodule content, and does not make the optional marker mandatory without a separate Architecture/Product/Release Owner decision and migration plan.

## Tasks / Subtasks

- [ ] **Task 1 — Ratify the v1 graph contract and freeze implementation evidence (AC: #1, #4, #5, #6)**
  - [ ] Obtain Architect + Release Owner approval for the exact graph boundary in the **Implementation Entry Gate**. Amend FC-DEP-1 or add a dated superseding decision with depth/boundary, repository identity normalization, edge uniqueness, visited/cycle behavior, unresolved-edge handling, array ordering, raw-byte hashing, schema evolution, and resource limits.
  - [ ] Re-run the graph census from `baseline_commit` and the implementation-start commit. Record root edge count, direct nested count, maximum depth, repository/commit pairs, every Builds selector, distinct Builds commits, and unresolved/legacy identities. If the chosen boundary changes the creation-time 40-edge/7-selector/5-catalog census, update the story and fixtures before implementation.
  - [ ] Record implementation-start `HEAD`, working-tree paths, root gitlinks, and all in-scope commit objects. Preserve unrelated work and do not initialize/update submodules to manufacture missing history.
  - [ ] Keep BUILD-CAT-1 open and upstream-owned. Record the upstream issue/decision evidence in `_bmad-output/implementation-artifacts/deferred-work.md` without editing `references/Hexalith.Builds`.

- [ ] **Task 2 — Add one reusable committed-object dependency-graph engine (AC: #1, #2, #3, #4, #5)**
  - [ ] Add `eng/dependency_graph.py` as a standard-library-only collect/canonicalize/validate/diff engine used by CI and release evidence. Do not create a parallel manifest implementation.
  - [ ] Accept an explicit root repository identity and 40-hex commit. Read trees with `git ls-tree -r -z --full-tree`, committed `.gitmodules` with `git config --blob <commit>:.gitmodules`, and catalogs with exact `<commit>:<path>` object reads. Never derive release evidence from the ambient index, working-tree nested HEADs, or a mutable submodule checkout.
  - [ ] Resolve only repository identities already declared by the FrontComposer root. Normalize approved GitHub SSH/HTTPS forms to a canonical lowercase identity, strip terminal `.git`/slash, and reject credentials, control characters, absolute/backslash/dot-segment paths, path traversal, or unknown identities. Never clone or execute from a candidate URL.
  - [ ] Record an edge before applying the visited check. Treat different commits of the same repository as distinct. Use `(owner_repository, owner_commit, path)` for edge uniqueness and `(Builds repository, Builds commit)` for catalog-validation deduplication; retain every selector edge.
  - [ ] Emit deterministic machine JSON with top-level `schema: hexalith.dependency-graph.v1`, `root: { repository, commit }`, and explicitly sorted edges. Each edge contains at least `owner_repository`, `owner_commit`, `path`, `repository`, `commit`, and `depth`; Builds edges additionally contain `catalog_sha256` and the approved marker/absence representation. Use strict lowercase 40-hex Git IDs, lowercase 64-hex SHA-256 values, normalized POSIX relative paths, and an explicit stable array sort key.
  - [ ] Hash the raw Git blob bytes so BOM/EOL/comments are sealed; parse those same bytes for semantics. Do not label Python's existing compact `json.dumps(..., sort_keys=True)` output as RFC 8785 canonical JSON. Preserve or explicitly version the existing seal formula unless a separate decision adopts JCS.
  - [ ] Add deterministic diagnostics and nonzero exits for missing objects, missing/duplicate `.gitmodules` mappings, malformed URLs/paths/IDs, duplicate edges, cycles/nontermination, unresolved repositories, missing catalogs, and inconsistent graph input.

- [ ] **Task 3 — Replace historical SHA assertions with selected-catalog semantic governance (AC: #1, #2, #3, #6)**
  - [ ] Update `InfrastructureGovernanceTests.cs` so the catalog governance tests use the committed-object engine/equivalent exact owner-commit blob reads and delete `rootBuildsCommit`, `eventStoreBuildsCommit`, `memoriesBuildsCommit`, `partiesBuildsCommit`, and the ambient-index `ReadGitlinkCommit` compatibility path.
  - [ ] Preserve the existing semantic contract: FrontComposer's root remains an import shim; required central package identities and versions remain authoritative and unconditional; invalid Include/Update/Exclude/Remove/conditions still fail; EventStore/Memories inheritance and Parties' three guarded imports, central-package properties, no inline versions, and no MinVer ownership remain enforced against the applicable selected catalog.
  - [ ] Validate each distinct selected Builds catalog and report all selecting owners. Keep the existing root catalog BOM/CRLF policy as a separate repository-format assertion unless the entry-gate decision explicitly promotes that formatting requirement to every catalog's semantic contract.
  - [ ] Add synthetic positives for a compatible commit advance and multiple selectors of one catalog. Add negatives for every AC3 mismatch, unknown identity, malformed `.gitmodules`, duplicate edge, path escape, unavailable commit/blob, and conflicting Builds commits. Assert messages include owner repository/commit/path and selected catalog commit/path.
  - [ ] Never replace the SHA list with a raw-catalog SHA-256 allowlist or an accepted-commit table. The exact IDs belong in produced evidence and fixtures only.

- [ ] **Task 4 — Add release-blocking graph diff and affected-module gates (AC: #2, #4)**
  - [ ] Update `.github/workflows/ci.yml` so primary CI selects one explicit PR candidate SHA and its exact merge-base/base SHA, collects both graphs, diffs logical edges by `(owner_repository, path, target_repository)`, and publishes deterministic JSON/text evidence. Do not mix the GitHub PR merge commit with head-state submodule data.
  - [ ] For added/removed/changed edges, map changes to supported owners through a static FrontComposer-owned allowlist. A changed root module builds that module once; a changed nested Builds selector builds its owning module once; unchanged graphs build nothing extra.
  - [ ] Materialize each affected exact commit in a temporary archive/worktree and the exact selected Builds catalog without moving the shared nested submodule checkout. Run `dotnet restore <Module>.slnx -p:Configuration=Release -p:UseNuGetDeps=true` and the Release build with `--no-restore`. No recursive init, candidate-supplied script/command, or arbitrary repository URL is permitted.
  - [ ] Update `.github/workflows/quality.yml` only for supplemental helper/Governance coverage and required exact history/object fetch. Preserve Gate 2b and the root-only submodule policy; `fetch-depth: 1` must not be the only source for merge-base or candidate-object proof.
  - [ ] Extend `CiGovernanceTests.cs` to pin explicit base/candidate selection, the release-blocking dependency relationship, deterministic evidence, static module commands, no recursive submodule operations, no arbitrary command execution, and the unchanged-graph no-op path.

- [ ] **Task 5 — Extend the existing sealed release manifest (AC: #5, #6)**
  - [ ] Extend `eng/release_evidence.py`; do not add a separate release-manifest tool. `prepare_manifest` must collect the graph for `args.commit_sha`, reject the local sentinel for release evidence, persist schema/root/edges/catalog hashes/digest, and include the graph digest in fallback invalidation.
  - [ ] Treat the graph addition as a manifest schema change and version it deliberately (expected major helper/schema bump unless compatibility code proves otherwise). Bind any new helper/config file into `RELEASE_DEFINITION_FILES` and `FALLBACK_INVALIDATION_FILES`.
  - [ ] Make `verify-manifest --no-root` enforce schema, types, strict IDs/hashes/paths, uniqueness, completeness, explicit ordering, edge count, and graph digest without consulting a checkout. Make live `--root` verification recompute the exact graph from the sealed root commit and compare every edge/catalog byte hash before publish or post-publish acceptance.
  - [ ] Preserve `eng/release_prepublish.py` ordering: prepare -> seal -> live verify -> classify, plus pre-push verification. Preserve pack-once artifacts, symbol checksums, immutability probe, signing/timestamp/attestation, approval fallback, classification, and incident behavior.
  - [ ] Update `.github/workflows/release-evidence.yml` to reconstruct and report the exact graph schema/count/digest from the upstream release commit. It remains read-only: no prepare, reseal, classification rewrite, or publication. Preserve exact upstream SHA checkout, full required history, and root-only submodule initialization.
  - [ ] Keep `.github/workflows/release.yml`, the reusable release workflow boundary, the REL-4 freeze, and `.releaserc.json` publication ownership unchanged unless minimal plumbing is strictly required. The existing `release-evidence/*.json` asset wildcard already covers the enhanced manifest/evidence.

- [ ] **Task 6 — Add fixtures, regression proof, documentation, and durable handoff (AC: all)**
  - [ ] Add `tests/eng/test_dependency_graph.py` with synthetic local Git repositories for deterministic collection/diff, compatible pointer advance, multiple Builds versions/selectors, exact-byte hashing, cycles, duplicates, missing mappings/objects/catalogs, malformed inputs, unsafe URLs/paths, stable ordering, and chosen-boundary behavior.
  - [ ] Update `ReleaseModelGovernanceTests.cs`, `Story12_4_RedPhaseDefTests.cs`, `tests/ci-governance/stage_release_state.py`, `release-manifest-valid.json`, the invalid manifest fixture, and `release-readiness-cases.json` for the versioned graph schema and both offline/live failures. Reseal synthetic fixtures only through the actual helper.
  - [ ] Update `tests/README.md` with focused local commands and the distinction between semantic compatibility, graph provenance, offline structural verification, and live drift verification.
  - [ ] Reconcile `_bmad-output/project-docs/deployment-guide.md`, `_bmad-output/project-docs/architecture.md`, and `_bmad-output/project-context.md` only where the landed boundary/tooling changes durable contributor or Release Owner behavior. Do not rewrite unrelated planning history.
  - [ ] Re-run the complete Governance lane on the exact Story 11.17d promotion revision. GOV-1 removes its current false-red pointer blocker, but Story 11.17d remains separately owned and cannot be promoted on stale or partial evidence.
  - [ ] Record exact commands/results, chosen graph boundary and census, schema/count/digest, changed-path ledger, fixture reseals, root gitlink audit, no-recursion scan, and `git diff --check` in the Dev Agent Record before review.

## Dev Notes

### Current State and UPDATE/NEW Map

Every UPDATE file below was inspected during creation. Treat this table as implementation routing, not permission to change every row.

| Path | Current state | Required GOV-1 direction |
|---|---|---|
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` | Correct semantic catalog checks plus four historical Builds SHA constants; `ReadGitlinkCommit` reads the ambient index. | Remove compatibility SHA pins; validate every approved selected catalog from exact committed objects; preserve semantic package/import/ownership checks and actionable diagnostics. |
| `eng/release_evidence.py` | Version 1.2.0 release-evidence helper; manifest has no dependency graph. | Extend prepare/seal/offline verify/live verify/diagnostics with the versioned graph and digest; retain all existing artifact and authorization safeguards. |
| `.github/workflows/ci.yml` | Primary reusable domain CI; Release is triggered from this workflow's conclusion. | Add the release-blocking graph-diff/affected-module job here. |
| `.github/workflows/quality.yml` | Supplemental FrontComposer gates, root-only init, shallow checkout. | Add supplemental graph/helper coverage and sufficient exact-object history without recursive init. |
| `.github/workflows/release-evidence.yml` | Read-only post-release evidence on the exact upstream SHA. | Recompute and report graph schema/count/digest; preserve no-mutation responsibilities. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` | Pins CI/release workflow contracts. | Pin explicit revisions, release-blocking dependency, safe static module gates, deterministic evidence, and no recursion. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/ReleaseModelGovernanceTests.cs` | Pins release manifest and helper behavior. | Add graph schema/digest/offline/live/fallback regression assertions. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/Story12_4_RedPhaseDefTests.cs` | Cross-checks release red-phase fixtures/contracts. | Reconcile only assertions affected by the manifest schema. |
| `tests/ci-governance/stage_release_state.py` and `tests/ci-governance/fixtures/*` | Stages sealed synthetic readiness cases. | Generate valid v1 graph evidence and negative structural/drift cases through production helpers. |
| `eng/release_prepublish.py` | Correct prepare/seal/live-verify/classify and pre-push ordering. | Preserve; update only if the enhanced helper requires plumbing. |
| `.github/workflows/release.yml`, `.releaserc.json` | Frozen single publication path and evidence asset wildcard. | Preserve unless minimal graph plumbing is required; do not reopen REL-3/REL-4 policy. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Durable external-work ledger. | Route BUILD-CAT-1 with owner, evidence, and reopen trigger. |

Expected NEW files:

- `eng/dependency_graph.py` — one reusable stdlib committed-object graph engine.
- `tests/eng/test_dependency_graph.py` — synthetic Git graph/semantic/safety tests.
- Optionally `eng/dependency-module-gates.json` if the static supported-module command map is made declarative. If added, bind it into release definition and fallback fingerprints and reject candidate-owned commands.

Do not create a second manifest tool or add a third-party parsing/canonicalization dependency. Python 3.14.4, Git 2.53.0, and .NET SDK 10.0.302 are available at story creation.

### Creation-Time Catalog Evidence — Provenance, Not an Allowlist

The stable bounded census found 7 Builds selector edges resolving to 5 distinct catalog commits. FrontComposer, Tenants, and Memories currently select one Builds commit; EventStore, Parties, Commons, and PolymorphicSerializations select four other commits. None of the five catalogs exposes a contract-version marker. Their exact commits and raw SHA-256 values are useful fixture/baseline evidence only; no value may become an accepted-compatibility list.

Raw bytes are the provenance unit because normalization would erase BOM/EOL/comment changes. Semantic XML evaluation must use those same bytes. The current root test's BOM/CRLF assertion remains a local format policy unless separately generalized.

### Graph and Git Safety Requirements

- The explicit root commit is authoritative. Ambient `HEAD`, `git ls-files --stage`, nested working-tree HEADs, and initialized submodule contents are not release evidence.
- Use Git plumbing through argv-based subprocess calls, never shell interpolation. `.gitmodules` is untrusted candidate input.
- Root `.gitmodules` supplies the only permitted repository identity map. Do not clone arbitrary URLs or discover nested repositories from the network.
- Strictly validate repository identity, Git IDs, SHA-256 values, paths, object availability, uniqueness, depth, ordering, and the ratified boundary before emitting a graph.
- A visited set prevents recursion; it must not suppress an edge from evidence. Repository identity plus commit, not repository name alone, is the traversal identity.
- Sort arrays explicitly. `sort_keys=True` sorts object keys, not list elements.
- Keep compatibility and provenance distinct: XML semantics can pass while commit/hash evidence changes; a matching fingerprint cannot rescue an invalid semantic contract.

### Semantic Catalog Contract to Preserve

The current Governance tests define the migration contract. Preserve at least:

- the FrontComposer root `Directory.Packages.props` remains an import shim with no owned `PackageVersion` items;
- shared package rows are single, unconditional authoritative declarations without incompatible `Update`, `Exclude`, `Remove`, conditional shadowing, or inline consumer overrides;
- the currently governed package/version set remains exact, including Tenants, BenchmarkDotNet, FsCheck.xUnit v3, Roslyn Workspaces, localization/time-provider testing, MCP, NUlid, PactNet, immutable/annotations/reactive/task-extension packages, and Verify;
- EventStore and Memories inherit their governed shared rows without local overrides;
- Parties retains its exact three guarded import paths, central package properties, inherited package ownership, and absence of MinVer ownership/inline versions;
- forbidden provider/infrastructure package scans remain independent and green.

Do not attempt to emulate complete MSBuild/NuGet evaluation in ad-hoc XML code. The semantic checks protect the approved invariants; the affected module's standalone Release restore/build is executable proof that the selected catalog actually evaluates.

### CI Revision and Module-Gate Requirements

GitHub pull-request workflows distinguish head and merge commits. Select and document one candidate model, feed the same exact candidate to graph collection and module builds, and compare it with its exact base/merge-base. Evidence must name both full SHAs.

The supported module command map is code/config owned by FrontComposer. Never run a path, shell fragment, workflow, or build command obtained from `.gitmodules` or a candidate module. Temporary exact-commit materialization must not move shared submodule checkouts or run recursive initialization. Release restore/build uses `UseNuGetDeps=true`; it does not depend on source-project wiring hidden in the umbrella checkout.

### Release Manifest and Seal Requirements

- `prepare_manifest` uses the requested commit SHA. The local sentinel is not acceptable for a governed release graph.
- Offline `--no-root` verification always enforces structural/schema/order/digest rules. Live verification additionally resolves the sealed root commit, reconstructs the graph, re-hashes raw catalog blobs, and rejects drift.
- Add the graph digest to fallback invalidation so an approval cannot survive a dependency-pointer change.
- Post-publication evidence/ledger records the schema, edge count, and digest alongside the existing package/artifact evidence.
- Preserve the current seal formula unless explicitly versioned. RFC 8785 is a reference point, not an adopted claim.
- Preserve REL-3 exact-artifact enforcement and REL-4's default-frozen publication gate. This story adds provenance; it does not authorize publishing.

### Scope Boundaries and Never-List

- No runtime or public API behavior, schema, generated output, package inventory, UX, route, copy, CSS, JS, accessibility, localization, telemetry, or feature change.
- No dependency/package/action version upgrade. Keep existing SHA-pinned GitHub Actions; the mutable `Hexalith.Builds` `initialize-build@main` action is not dependency provenance.
- No root/submodule `.gitmodules` or gitlink change as part of the implementation. No file under `references/**` is an implementation target.
- No recursive/remote submodule update and no nested submodule initialization.
- No REL-3 signing/artifact-scope redesign, REL-4 freeze redesign, REL-5 operational authorization, or actual publication.
- BUILD-CAT-1 remains an external `Hexalith.Builds` responsibility.

### Testing Requirements

Run focused helper tests first, then the repository-authoritative Governance lane:

```bash
python3 -m unittest tests/eng/test_dependency_graph.py
python3 -m py_compile eng/dependency_graph.py eng/release_evidence.py eng/release_prepublish.py

DiffEngine_Disabled=true dotnet test \
  tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj \
  --configuration Release --filter "Category=Governance"

DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx \
  --configuration Release --filter "Category=Governance"

python3 eng/release_evidence.py verify-manifest \
  --manifest tests/ci-governance/fixtures/release-manifest-valid.json --no-root

python3 eng/release_evidence.py classify-fixtures \
  --fixtures tests/ci-governance/fixtures/release-readiness-cases.json \
  --output /tmp/frontcomposer-release-readiness.json

python3 eng/validate-story-artifacts.py
git diff --check
rg -n "submodule update.*--recursive|submodule foreach.*--recursive|--recurse-submodules" \
  .github eng tests
```

For every affected supported module, use the exact static module solution/path and exact candidate commit:

```bash
dotnet restore <Module>.slnx -p:Configuration=Release -p:UseNuGetDeps=true
dotnet build <Module>.slnx --configuration Release --no-restore -p:UseNuGetDeps=true
```

Also prove:

- compatible pointer advance passes semantics while diff/provenance changes;
- semantic mismatch fails even when graph shape is valid;
- missing/extra/duplicate/malformed/nonterminating/unresolved/out-of-order cases fail offline;
- live root/commit/edge/catalog-byte/digest drift fails;
- unchanged graph produces no affected-module build;
- multiple owners selecting one Builds commit validate it once and report every selector;
- no release/publish command is executed.

If an authoritative broad gate is environmentally blocked, record the exact command and result separately from focused proof. Do not weaken or relabel the gate.

### Previous-Story Intelligence

- Story 11.17d is mechanically implemented but remains `in-progress` because its exact promotion revision must pass the complete Governance lane. Its evidence reproduced the historical Builds SHA false-red. GOV-1 owns that governance correction; do not absorb Shell split scope or mark 11.17d complete from a GOV-1-focused run.
- REL-3 already owns the exact-artifact prepare/seal/live-verify/classify seam. Extend that seam rather than creating a new release path.
- Recent pointer-reconciliation commits demonstrate why commit identities are provenance rather than semantic compatibility requirements. Do not infer implementation from commit subjects; inspect the actual diff.
- Preserve user work, root-only dependency initialization, warnings-as-errors, CRLF/final-newline policy, and the solution-level Governance command from project context.

### Project Structure Notes

- Primary implementation: `eng/dependency_graph.py`, `eng/release_evidence.py`, `InfrastructureGovernanceTests.cs`, and primary/supplemental/release-evidence workflows.
- Primary regression areas: `tests/eng/`, Shell Governance tests, and `tests/ci-governance/fixtures/`.
- Durable documentation is limited to the graph/release contributor contract and BUILD-CAT-1 handoff.
- Creation-time artifact changes are only this story file and the surgical sprint-status story transition. The implementation File List must replace the initial list below with its exact owned union before review.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Epic Governance and final GOV-1 acceptance criteria]
- [Source: `_bmad-output/planning-artifacts/prd.md` — FR-24, NFR-12, NFR-13, SM-2/SM-2a, D-11]
- [Source: `_bmad-output/planning-artifacts/prd-addendum-2026-07-05.md`]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — dependency-graph and release-provenance invariants]
- [Source: `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md` — FC-DEP-1]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19.md`]
- [Source: `_bmad-output/implementation-artifacts/11-17-shell-bundle-split.md` — current Governance blocker]
- [Source: `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`]
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs`]
- [Source: `eng/release_evidence.py`; `eng/release_prepublish.py`]
- [Source: `.github/workflows/ci.yml`; `.github/workflows/quality.yml`; `.github/workflows/release-evidence.yml`; `.github/workflows/release.yml`]
- [Git `ls-tree`](https://git-scm.com/docs/git-ls-tree)
- [Git config `--blob`](https://git-scm.com/docs/git-config#Documentation/git-config.txt---blobltblobgt)
- [Git revisions](https://git-scm.com/docs/gitrevisions)
- [Git submodule configuration](https://git-scm.com/docs/gitmodules)
- [Git diff options](https://git-scm.com/docs/diff-options)
- [GitHub Actions pull-request SHA semantics](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows)
- [NuGet Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [NuGet/MSBuild restore targets](https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets)
- [MSBuild imports](https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-use-the-same-target-in-multiple-project-files)
- [MSBuild evaluation and build process](https://learn.microsoft.com/en-us/visualstudio/msbuild/build-process-overview)
- [RFC 8785 JSON Canonicalization Scheme](https://www.rfc-editor.org/rfc/rfc8785.html)
- [FIPS 180-4 Secure Hash Standard](https://csrc.nist.gov/pubs/fips/180-4/upd1/final)
- [Python `hashlib`](https://docs.python.org/3/library/hashlib.html)
- [SLSA build provenance v1.2](https://slsa.dev/spec/v1.2/build-provenance)
- [GitHub artifact attestations](https://docs.github.com/en/actions/concepts/security/artifact-attestations)

## Dev Agent Record

### Agent Model Used

OpenAI Codex (GPT-5)

### Debug Log References

- 2026-07-19: Loaded the Hexalith baseline, create-story workflow/customization/config/template/checklist, project context, complete PRD/addendum, architecture, Epic GOV, UX artifacts (N/A for this non-UI story), proposal, FC-DEP-1, sprint status, previous story/release evidence, live implementation/tests/workflows, repository configuration, git history/status, and official Git/GitHub/NuGet/MSBuild/provenance guidance.
- 2026-07-19: Confirmed the story key was `backlog`, the separate GOV-1 action item was `open`, no canonical GOV-1 story existed, and the creation baseline worktree was clean.
- 2026-07-19: Confirmed `e3e3dcf5` changed planning artifacts/gitlinks only; no GOV-1 implementation exists despite the commit subject.
- 2026-07-19: Bounded root+direct census found 40 edges (8 root + 32 direct), 7 Builds selectors, and 5 distinct Builds commits. Full historical recursion exposed unresolved legacy identities and non-reconciled census totals; the conflict is preserved as a hard entry gate rather than guessed away.
- 2026-07-19: Create-story checklist review added the graph-boundary entry gate, exact object-reading/canonicalization/safety rules, semantic-vs-provenance separation, affected-module execution constraints, manifest offline/live failure modes, previous-story handoff, official references, and focused/broad validation commands.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- GOV-1 is ready for development only after the FC-DEP-1 graph-boundary entry gate is explicitly ratified.
- Sprint tracking moves the story row to `ready-for-dev`; the separate cross-cutting action remains `open` until implementation and accepted evidence complete.

### File List

- `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md` (NEW — create-story artifact)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (UPDATE — GOV-1 story row only; action item remains open)
