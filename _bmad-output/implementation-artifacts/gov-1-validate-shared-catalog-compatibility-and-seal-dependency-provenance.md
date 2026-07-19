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
implementation_entry_gate: resolved
implementation_entry_gate_resolved: 2026-07-19
architecture_spine: _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
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

## Implementation Entry Gate — Resolved 2026-07-19

Administrator explicitly ratified FC-DEP-1 and the focused architecture spine as Architect and Release
Owner. `hexalith.dependency-graph.v1` is the complete defined depth-1/2 graph: every gitlink at the
explicit FrontComposer commit plus every direct gitlink in each exact root-selected commit. In-boundary
self/back-reference edges are recorded; deeper historical edges are excluded and require a separately
approved schema.

The ratified contract also fixes repository normalization, edge identity/order, closed canonical JSON,
resource ceilings, immutable base/before policy activation, exact CI revisions, affected-module
dispositions, manifest v2, authenticated CI handoff, and immutable workflow provenance. The governing
sources are:

- `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md`;
- `_bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`.

Creation-time evidence remains 40 edges (8 root + 32 direct), 7 Builds selectors, and 5 distinct Builds
commits. At implementation-start `600f4c738bd28b1efe0e69940ccec8b03faba7c4`, the same 40 edges and
7 selectors resolve to **6** distinct Builds commits. Counts are evidence, never acceptance constants.
Production/test/workflow/manifest implementation may now proceed under the ratified contract.

## Acceptance Criteria

1. **Validate every selected catalog semantically, without a commit allowlist.** **Given** the ratified `hexalith.dependency-graph.v1` boundary, **when** governance evaluates an explicit FrontComposer root commit, **then** it records every depth-1/2 gitlink edge from committed Git objects in deterministic order, including in-boundary self/back-references; reads `Props/Directory.Packages.props` from every distinct actual `Hexalith.Builds` commit selected by those edges; evaluates every selector under its explicit owner profile; and contains no expected historical 40-hex Builds SHA allowlist. Catalog bytes may cache by distinct Builds commit while every selector remains present in evidence and diagnostics.

2. **Compatible pointer advances pass and remain reviewable.** **Given** a gitlink advances to a catalog whose semantic contract remains compatible, **when** Governance and CI run, **then** compatibility passes. The changed repository/commit/catalog identity appears only in the deterministic dependency-graph diff and sealed provenance; a fingerprint difference alone cannot reject it.

3. **Incompatible or unreadable catalogs fail precisely.** **Given** an in-scope Builds edge selects a missing catalog, malformed XML, missing/duplicate/conditional/overridden required package declaration, changed required version, broken import/ownership rule, or an approved mandatory marker mismatch, **when** validation runs, **then** it fails closed with the owning repository, owning commit, gitlink path, selected Builds commit, catalog path, and precise semantic mismatch. A marker remains optional until separately approved as mandatory.

4. **Gitlink changes receive exact, affected-module CI proof.** **Given** a pull request changes an in-boundary gitlink, **when** primary CI requires the event base to equal the computed merge-base and compares it with the exact `github.sha` merge revision, **then** it emits deterministic added/removed/changed edge/catalog evidence and applies the closed build/evidence-only registry once per affected module at that candidate revision. Push CI compares a non-zero `github.event.before` with `github.sha`; zero/unavailable bases fail the gate. Build dispositions run exact static standalone Release/NuGet argv with bounded exact Builds contract-tree materialization. Depth-1 changes subsume descendant churn, unchanged graphs build nothing, and no recursive/nested initialization or candidate-supplied command is permitted.

5. **Governed release evidence seals and re-verifies the complete approved graph.** **Given** `prepare-manifest`, `seal-manifest`, offline fixture verification, live pre-publish verification, and post-publish verification, **when** manifest v2 is processed, **then** the sealed payload binds the closed v1 graph, explicit root, every defined edge, each Builds catalog hash/marker, active policy coordinates, authenticated successful-CI handoff, and immutable caller/reusable/action workflow definitions. Verification fails closed on missing, unknown, duplicate, malformed, over-limit, unresolved in-boundary, out-of-order, unavailable-object, policy/workflow/handoff mismatch, root-commit mismatch, graph/catalog drift, or digest mismatch. Legacy manifests are audit-only and non-publishable. Existing artifact checksums, signatures, timestamps, attestations, seals, helper/package/fallback fingerprints, classification, incident handling, and freeze controls remain intact.

6. **Ownership and migration are explicit.** **Given** catalog authorship belongs to `Hexalith.Builds`, **when** GOV-1 lands, **then** BUILD-CAT-1 is durably routed upstream for any desired catalog marker/contract-version addition. FrontComposer validates the semantic catalog content directly during migration, carries no fingerprint allowlist, does not edit submodule content, and does not make the optional marker mandatory without a separate Architecture/Product/Release Owner decision and migration plan.

## Tasks / Subtasks

- [ ] **Task 1 — Ratify the v1 graph contract and freeze implementation evidence (AC: #1, #4, #5, #6)**
  - [x] Obtain Architect + Release Owner approval for the exact graph boundary in the **Implementation Entry Gate**. FC-DEP-1 and the focused architecture spine were ratified on 2026-07-19 with the depth-1/depth-2 boundary, closed identities/policy, canonical graph, raw-byte hashing, exact revision rules, schema migration, workflow provenance, and resource ceilings.
  - [x] Re-run the graph census from `baseline_commit` and the implementation-start commit. The creation baseline has 40 edges, 7 Builds selectors, and 5 distinct Builds commits; implementation-start `600f4c738bd28b1efe0e69940ccec8b03faba7c4` has 40 edges, 7 selectors, and 6 distinct Builds commits. These are evidence, never acceptance constants.
  - [ ] Record implementation-start `HEAD`, working-tree paths, root gitlinks, and all in-scope commit objects. Preserve unrelated work and do not initialize/update submodules to manufacture missing history.
  - [ ] Keep BUILD-CAT-1 open and upstream-owned. Record the upstream issue/decision evidence in `_bmad-output/implementation-artifacts/deferred-work.md` without editing `references/Hexalith.Builds`.

- [ ] **Task 2 — Add one reusable committed-object dependency-graph engine (AC: #1, #2, #3, #4, #5)**
  - [ ] Add `eng/dependency_graph.py` as a standard-library-only collect/canonicalize/validate/diff engine used by CI and release evidence. Do not create a parallel manifest implementation.
  - [ ] Add required `eng/dependency-graph-policy.json` with schema `hexalith.dependency-graph-policy.v1` as the single FrontComposer-owned source for trusted identities/paths, semantic profiles, static module argv, evidence-only dispositions, and v1 resource ceilings. Enforce base-policy activation and the one-time approved bootstrap defined by AD-12; a candidate policy cannot authorize itself.
  - [ ] Accept an explicit root repository identity and 40-hex commit. Read trees with `git ls-tree -r -z --full-tree`, committed `.gitmodules` with `git config --blob <commit>:.gitmodules`, and catalogs with exact `<commit>:<path>` object reads. Never derive release evidence from the ambient index, working-tree nested HEADs, or a mutable submodule checkout.
  - [ ] Resolve only repository identities already declared by the FrontComposer root. Normalize approved GitHub SSH/HTTPS forms to a canonical lowercase identity, strip terminal `.git`/slash, and reject credentials, control characters, absolute/backslash/dot-segment paths, path traversal, or unknown identities. Never clone or execute from a candidate URL.
  - [ ] Collect exactly depth 1 root gitlinks and depth 2 direct gitlinks from each exact depth-1 owner commit. Record self/back edges normally; never traverse deeper. Treat different commits of the same repository as distinct. Use `(owner_repository, owner_commit, path)` for edge uniqueness and cache raw Builds blob reads by `(Builds repository, Builds commit)` while evaluating every selector against its explicit semantic profile.
  - [ ] Emit duplicate-member-free deterministic JSON with exactly `{schema, root, edge_count, edges, graph_digest}`. `schema` is `hexalith.dependency-graph.v1`; `edge_count == len(edges)`; root and edge member sets, strict lowercase IDs/hashes, nullable catalog marker, normalized POSIX paths, ordinal edge ordering, project canonical bytes, and the golden digest follow AD-4/AD-5 exactly.
  - [ ] Hash the raw Git blob bytes so BOM/EOL/comments are sealed; parse those same bytes for semantics. Do not label Python's existing compact `json.dumps(..., sort_keys=True)` output as RFC 8785 canonical JSON. Preserve or explicitly version the existing seal formula unless a separate decision adopts JCS.
  - [ ] Add deterministic diagnostics and nonzero exits for missing objects, missing/duplicate `.gitmodules` mappings, malformed URLs/paths/IDs, duplicate edges, unresolved repositories, missing catalogs, inconsistent graph input, and every AD-7 ceiling. Enforce 4,096 edges, 64 MiB `ls-tree` bytes per owner commit, 1 MiB per `.gitmodules` blob, and 4 MiB per catalog blob before decoding/parsing.

- [ ] **Task 3 — Replace historical SHA assertions with selected-catalog semantic governance (AC: #1, #2, #3, #6)**
  - [ ] Update `InfrastructureGovernanceTests.cs` so the catalog governance tests use the committed-object engine/equivalent exact owner-commit blob reads and delete `rootBuildsCommit`, `eventStoreBuildsCommit`, `memoriesBuildsCommit`, `partiesBuildsCommit`, and the ambient-index `ReadGitlinkCommit` compatibility path.
  - [ ] Preserve the existing semantic contract: FrontComposer's root remains an import shim; required central package identities and versions remain authoritative and unconditional; invalid Include/Update/Exclude/Remove/conditions still fail; EventStore/Memories inheritance and Parties' three guarded imports, central-package properties, no inline versions, and no MinVer ownership remain enforced against the applicable selected catalog.
  - [ ] Cache each distinct selected Builds blob/hash, evaluate every selector edge through its explicit policy profile, and report every selecting owner. Keep the existing root catalog BOM/CRLF policy as a separate repository-format assertion unless a later approved policy revision promotes it to every catalog's semantic contract.
  - [ ] Add synthetic positives for a compatible commit advance and multiple selectors of one catalog. Add negatives for every AC3 mismatch, unknown identity, malformed `.gitmodules`, duplicate edge, path escape, unavailable commit/blob, and conflicting Builds commits. Assert messages include owner repository/commit/path and selected catalog commit/path.
  - [ ] Never replace the SHA list with a raw-catalog SHA-256 allowlist or an accepted-commit table. The exact IDs belong in produced evidence and fixtures only.

- [ ] **Task 4 — Add release-blocking graph diff and affected-module gates (AC: #2, #4)**
  - [ ] Update `.github/workflows/ci.yml` so pull-request CI uses `github.event.pull_request.base.sha` as `event_base`, `github.sha` as the exact candidate merge revision, and requires `git merge-base event_base github.sha == event_base`. Push CI compares non-zero `github.event.before` with `github.sha`; zero/unavailable bases take the fail-closed full-affected diagnostic path and are never release-eligible. Record all exact revisions, collect both graphs, diff logical edges by `(owner_repository, path, repository)`, and publish deterministic evidence.
  - [ ] Apply the AD-8 cascade before the policy registry: classify depth-1 added/changed/removed edges first and subsume their descendant churn; then classify remaining depth-2 changes. Deduplicate affected modules by canonical identity. Commands and evidence-only dispositions come only from the active closed policy; unchanged graphs build nothing extra.
  - [ ] Materialize each affected exact owner commit in isolation plus the complete bounded regular-file Builds contract tree at the listed gitlink path. Enforce 16,384 files, 16 MiB per blob, and 256 MiB total before extraction; reject symlinks, gitlinks, special modes, unsafe paths, and graph/catalog hash drift. Run the exact static standalone Release/NuGet argv from policy. No recursive init, candidate-supplied script/command, mutable checkout, or arbitrary repository URL is permitted.
  - [ ] Pin the primary CI reusable workflow and every transitive action source to approved immutable 40-hex commits, record/validate actual caller/reusable/action coordinates, and emit exactly one authenticated `dependency-release-handoff` artifact conforming to `hexalith.dependency-release-handoff.v1` for successful eligible `push` runs.
  - [ ] Update `.github/workflows/quality.yml` only for supplemental helper/Governance coverage and required exact history/object fetch. Preserve Gate 2b and the root-only submodule policy; `fetch-depth: 1` must not be the only source for merge-base or candidate-object proof.
  - [ ] Extend `CiGovernanceTests.cs` to pin explicit base/candidate selection, the release-blocking dependency relationship, deterministic evidence, static module commands, no recursive submodule operations, no arbitrary command execution, and the unchanged-graph no-op path.

- [ ] **Task 5 — Extend the existing sealed release manifest (AC: #5, #6)**
  - [ ] Extend `eng/release_evidence.py`; do not add a separate release-manifest tool. `prepare_manifest` must collect the graph for `args.commit_sha`, reject the local sentinel, and emit `hexalith.release-evidence.v2` with the complete AD-5 `dependency_graph`, closed `dependency_policy`, and AD-14 `workflow_provenance` objects.
  - [ ] Bind the policy, graph helper, CI workflow, versioned handoff contract, and immutable workflow/action definition coordinates into `RELEASE_DEFINITION_FILES` and `FALLBACK_INVALIDATION_FILES`. Implement the exact v2 fallback formula over definition, package set, graph digest, policy SHA-256, and workflow definition digest.
  - [ ] Make `verify-manifest --no-root` enforce schema, types, strict IDs/hashes/paths, uniqueness, completeness, explicit ordering, edge count, and graph digest without consulting a checkout. Make live `--root` verification recompute the exact graph from the sealed root commit and compare every edge/catalog byte hash before publish or post-publish acceptance.
  - [ ] Reject duplicate/unknown v2 members and any graph, policy, handoff projection, CI-only evaluator digest, raw handoff hash, release evaluator, combined workflow-definition digest, or exact-candidate inconsistency. Legacy manifests are accepted only by explicit audit diagnostics and are never publishable, fallback-eligible, resealed, or upgraded in place.
  - [ ] Preserve `eng/release_prepublish.py` ordering: prepare -> seal -> live verify -> classify, plus pre-push verification. Preserve pack-once artifacts, symbol checksums, immutability probe, signing/timestamp/attestation, approval fallback, classification, and incident behavior.
  - [ ] Update `.github/workflows/release-evidence.yml` to reconstruct and report the exact graph, policy, handoff, and workflow provenance from the upstream release commit. It remains read-only: no prepare, reseal, classification rewrite, or publication. Preserve exact upstream SHA checkout, full required history, and root-only submodule initialization.
  - [ ] Update `.github/workflows/release.yml` and the reusable release workflow seam so the caller passes `workflow_run.head_sha` plus the triggering run ID, verifies the named successful-CI handoff through read-only Actions APIs, reloads the recorded policy from that exact commit, and consumes that same SHA everywhere. Pin reusable workflows and all transitive actions to immutable 40-hex commits. Preserve the REL-4 freeze and `.releaserc.json` publication ownership; current mutable `@main` seams cannot authorize publication.

- [ ] **Task 6 — Add fixtures, regression proof, documentation, and durable handoff (AC: all)**
  - [ ] Add `tests/eng/test_dependency_graph.py` with synthetic local Git repositories for deterministic collection/diff, compatible pointer advance, multiple Builds versions/selectors, exact-byte hashing, self/back edges, depth-2 boundary exclusion, duplicates, missing mappings/objects/catalogs, malformed inputs, unsafe URLs/paths, stable ordering, every resource boundary, and full contract-tree extraction limits.
  - [ ] Update `ReleaseModelGovernanceTests.cs`, `Story12_4_RedPhaseDefTests.cs`, `tests/ci-governance/stage_release_state.py`, `release-manifest-valid.json`, the invalid manifest fixture, and `release-readiness-cases.json` for the versioned graph schema and both offline/live failures. Reseal synthetic fixtures only through the actual helper.
  - [ ] Add policy activation/bootstrap, depth-1 cascade collapse, zero/unavailable push base, exact handoff authentication, immutable workflow/action closure, legacy-manifest audit-only, and graph/policy/workflow fallback invalidation fixtures.
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
| `.github/workflows/ci.yml` | Primary reusable domain CI; Release is triggered from this workflow's conclusion. Current reusable/transitive `@main` references are mutable. | Add release-blocking graph diff, exact revision handling, affected-module proof, immutable reusable/action provenance, and the authenticated release handoff here. |
| `.github/workflows/quality.yml` | Supplemental FrontComposer gates, root-only init, shallow checkout. | Add supplemental graph/helper coverage and sufficient exact-object history without recursive init. |
| `.github/workflows/release-evidence.yml` | Read-only post-release evidence on the exact upstream SHA. | Recompute and report graph schema/count/digest; preserve no-mutation responsibilities. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` | Pins CI/release workflow contracts. | Pin explicit revisions, release-blocking dependency, safe static module gates, deterministic evidence, and no recursion. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/ReleaseModelGovernanceTests.cs` | Pins release manifest and helper behavior. | Add graph schema/digest/offline/live/fallback regression assertions. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/Story12_4_RedPhaseDefTests.cs` | Cross-checks release red-phase fixtures/contracts. | Reconcile only assertions affected by the manifest schema. |
| `tests/ci-governance/stage_release_state.py` and `tests/ci-governance/fixtures/*` | Stages sealed synthetic readiness cases. | Generate valid v1 graph evidence and negative structural/drift cases through production helpers. |
| `eng/release_prepublish.py` | Correct prepare/seal/live-verify/classify and pre-push ordering. | Preserve; update only if the enhanced helper requires plumbing. |
| `.github/workflows/release.yml`, `.releaserc.json` | Frozen single publication path and evidence asset wildcard; the workflow currently lacks the ratified exact-CI-revision/handoff seam and immutable reusable/action closure. | Add the required exact-SHA input, authenticated handoff, policy reload, and immutable workflow/action pins while preserving the REL-3/REL-4 publication policy and evidence asset ownership. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Durable external-work ledger. | Route BUILD-CAT-1 with owner, evidence, and reopen trigger. |

Expected NEW files:

- `eng/dependency_graph.py` — one reusable stdlib committed-object graph engine.
- `eng/dependency-graph-policy.json` — required versioned trust, semantic-profile, static-command, disposition, and resource-limit policy.
- `tests/eng/test_dependency_graph.py` — synthetic Git graph/semantic/safety tests.

Do not create a second manifest tool or add a third-party parsing/canonicalization dependency. Python 3.14.4, Git 2.53.0, and .NET SDK 10.0.302 are available at story creation.

### Creation-Time and Implementation-Start Evidence — Provenance, Not an Allowlist

At creation baseline `e3e3dcf5`, the bounded v1 census found 40 edges, 7 Builds selector edges, and 5 distinct catalog commits. At implementation-start `600f4c738bd28b1efe0e69940ccec8b03faba7c4`, it still finds 40 edges and 7 selectors but 6 distinct Builds commits. None of the creation-time catalogs exposes a contract-version marker. All exact commits, counts, and raw SHA-256 values are fixture/baseline evidence only; no value may become an acceptance constant or compatibility allowlist.

Raw bytes are the provenance unit because normalization would erase BOM/EOL/comment changes. Semantic XML evaluation must use those same bytes. The current root test's BOM/CRLF assertion remains a local format policy unless separately generalized.

### Graph and Git Safety Requirements

- The explicit root commit is authoritative. Ambient `HEAD`, `git ls-files --stage`, nested working-tree HEADs, and initialized submodule contents are not release evidence.
- Use Git plumbing through argv-based subprocess calls, never shell interpolation. `.gitmodules` is untrusted candidate input.
- Root `.gitmodules` supplies the only permitted repository identity map. Do not clone arbitrary URLs or discover nested repositories from the network.
- Strictly validate repository identity, Git IDs, SHA-256 values, paths, object availability, uniqueness, depth, ordering, and the ratified boundary before emitting a graph.
- Collection is a fixed depth-1/depth-2 projection, not recursive traversal. Record self/back edges normally, cache repeated object reads safely, and exclude deeper edges by boundary rather than reporting them unresolved.
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

For pull requests, use `github.event.pull_request.base.sha` as `event_base` and `github.sha` as the exact candidate merge revision. Require `git merge-base event_base github.sha == event_base`; otherwise fail closed. For pushes, compare non-zero `github.event.before` with `github.sha`; zero/unavailable bases run full-affected diagnostics but fail the gate and cannot produce release-eligible evidence. Graph collection, materialization, module builds, and evidence must consume these same exact revisions.

The supported module command map is the closed active policy owned by FrontComposer. Never run a path, shell fragment, workflow, or build command obtained from `.gitmodules` or a candidate module. Temporary exact-commit materialization must not move shared submodule checkouts or run recursive initialization. Materialize the complete bounded regular-file Builds contract tree at the selected gitlink path, verify its catalog hash, and run exact static standalone Release/NuGet argv with `UseNuGetDeps=true`.

### Release Manifest and Seal Requirements

- `prepare_manifest` uses the requested commit SHA. The local sentinel is not acceptable for a governed release graph.
- Offline `--no-root` verification always enforces structural/schema/order/digest rules. Live verification additionally resolves the sealed root commit, reconstructs the graph, re-hashes raw catalog blobs, and rejects drift.
- V2 fallback invalidation binds the graph digest, active policy SHA-256, and trusted CI/release workflow definition digest so approval cannot survive dependency, trust-policy, or evaluator drift.
- Post-publication evidence/ledger records graph schema/count/digest, policy coordinates, authenticated CI handoff, and immutable workflow provenance alongside existing package/artifact evidence.
- Preserve the current seal formula unless explicitly versioned. RFC 8785 is a reference point, not an adopted claim.
- Preserve REL-3 exact-artifact enforcement and REL-4's default-frozen publication gate. This story adds provenance; it does not authorize publishing.

### Scope Boundaries and Never-List

- No runtime or public API behavior, schema, generated output, package inventory, UX, route, copy, CSS, JS, accessibility, localization, telemetry, or feature change.
- No unrelated dependency or package upgrade. Pinning the CI/release reusable workflows and every transitive action source to approved immutable 40-hex commits, including replacing mutable Builds `@main` references, is required GOV-1 provenance work; any Builds-owned source change is routed upstream.
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
- missing/extra/duplicate/malformed/over-limit/unresolved/out-of-order cases fail offline, while self/back edges remain valid within the fixed boundary;
- live root/commit/edge/catalog-byte/policy/handoff/workflow/digest drift fails;
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
- 2026-07-19: Administrator ratified FC-DEP-1 and the focused architecture spine as Architect + Release Owner. The approved v1 boundary is exactly root gitlinks plus direct gitlinks in each root-selected commit; deeper history is out of scope.
- 2026-07-19: Implementation-start `600f4c738bd28b1efe0e69940ccec8b03faba7c4` census remains 40 edges and 7 Builds selectors but resolves to 6 distinct Builds commits. Source artifacts were reconciled to manifest v2, closed policy, exact CI/release revision handoff, immutable workflow/action provenance, and adopted resource ceilings.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- GOV-1's FC-DEP-1 architecture entry gate was ratified on 2026-07-19. Development may proceed against the final spine after Task 1 freezes the exact implementation-start object ledger and records the BUILD-CAT-1 handoff.
- Sprint tracking moves the story row to `ready-for-dev`; the separate cross-cutting action remains `open` until implementation and accepted evidence complete.

### File List

- `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md` (NEW — create-story artifact)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (UPDATE — GOV-1 story row only; action item remains open)
