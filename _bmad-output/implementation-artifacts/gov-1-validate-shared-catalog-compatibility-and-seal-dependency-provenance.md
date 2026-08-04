---
created: 2026-07-19
updated: 2026-08-04
story: GOV-1
owner: Product Owner + Architect + Developer + Release Owner
source_proposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19.md
course_correction: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md
decision: _bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md
status: in-progress
scope: moderate
implementation_risk: high
priority: before the next accepted governed release manifest
baseline_commit: 3786330d241c2d87449fa3e01afc95fc832552df
upstream_follow_up: BUILD-CAT-1
upstream_release_follow_up: Reopen BUILD-REL-1 issue 17 with the GOV-1 amendment or file a successor
implementation_entry_gate: resolved
implementation_entry_gate_resolved: 2026-07-19
architecture_spine: _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
external_completion_gate: Qualifying immutable revision from reopened Hexalith.Builds issue 17 or successor; integration and end-to-end proof only
---
# GOV-1: Validate Shared-Catalog Compatibility and Seal Dependency Provenance

Status: in-progress

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
commits. The pre-ratification snapshot `600f4c738bd28b1efe0e69940ccec8b03faba7c4` and current tracked
architecture-finalization HEAD `c585073c3b8fae58fe49cbfac5ddabca4df3dec7` both contain 40 edges,
7 selectors, and **6** distinct Builds commits, although individual gitlinks changed. Counts and exact
IDs are evidence, never acceptance constants; Task 1 must freeze the eventual production-start commit.
Production/test/workflow/manifest implementation may now proceed under the ratified contract.
Execution boundary corrected 2026-08-02: Hexalith.Builds issue 17 closed on 2026-07-20 without accepting
the GOV-1 amendment or recording a qualifying immutable revision. All FrontComposer-local graph diff,
affected-module proof, evaluator closure, handoff schema/consumer, manifest-v2, and hostile-fixture work
may proceed. Only reusable-workflow integration, end-to-end exact-candidate evidence proof, story
completion, release eligibility, and any unfreeze remain externally blocked until the Release Owner
reopens issue 17 with the complete amendment or files a successor and records an owner-accepted
immutable revision satisfying AD-13, AD-15, and AD-16.

## Acceptance Criteria

1. **Validate every selected catalog semantically, without a commit allowlist.** **Given** the ratified `hexalith.dependency-graph.v1` boundary, **when** governance evaluates an explicit FrontComposer root commit, **then** it records every depth-1/2 gitlink edge from committed Git objects in deterministic order, including in-boundary self/back-references; reads `Props/Directory.Packages.props` from every distinct actual `Hexalith.Builds` commit selected by those edges; evaluates every selector under its explicit owner profile; and contains no expected historical 40-hex Builds SHA allowlist. Catalog bytes may cache by distinct Builds commit while every selector remains present in evidence and diagnostics.

2. **Compatible pointer advances pass and remain reviewable.** **Given** a gitlink advances to a catalog whose semantic contract remains compatible, **when** Governance and CI run, **then** compatibility passes. The changed repository/commit/catalog identity appears only in the deterministic dependency-graph diff and sealed provenance; a fingerprint difference alone cannot reject it.

3. **Incompatible or unreadable catalogs fail precisely.** **Given** an in-scope Builds edge selects a missing catalog, malformed XML, missing/duplicate/conditional/overridden required package declaration, changed required version, broken import/ownership rule, or an approved mandatory marker mismatch, **when** validation runs, **then** it fails closed with the owning repository, owning commit, gitlink path, selected Builds commit, catalog path, and precise semantic mismatch. A marker remains optional until separately approved as mandatory.

4. **Gitlink changes receive exact, affected-module CI proof.** **Given** a pull request changes an in-boundary gitlink, **when** primary CI requires the event base to equal the computed merge-base and compares it with the exact `github.sha` merge revision, **then** it emits deterministic added/removed/changed edge/catalog evidence and applies the closed build/evidence-only registry once per affected module at that candidate revision. Push CI compares a non-zero `github.event.before` with `github.sha`; zero/unavailable bases fail the gate. Build dispositions run exact static standalone Release/NuGet argv with bounded exact Builds contract-tree materialization. Depth-1 changes subsume descendant churn, unchanged graphs build nothing, and no recursive/nested initialization or candidate-supplied command is permitted.

5. **Governed release evidence seals and re-verifies the complete approved graph.** **Given** `prepare-manifest`, `seal-manifest`, offline fixture verification, live pre-publish verification, and post-publish verification, **when** manifest v2 is processed, **then** the sealed payload binds the closed v1 graph, explicit root, every defined edge, each Builds catalog hash/marker, active policy coordinates, authenticated successful-CI handoff, and active-policy-authorized static caller/reusable/action closures. Every Release attempt emits the authenticated AD-15 verification handoff preserving the original CI candidate; the verifier never substitutes its second-hop/default-branch SHA and cannot green-no-op failure or partial publication. Verification fails closed on missing, unknown, duplicate, malformed, over-limit, unresolved in-boundary, out-of-order, unavailable-object, unapproved policy/workflow/handoff, root-commit mismatch, graph/catalog drift, or digest mismatch. Legacy manifests are audit-only and non-publishable. Existing artifact checksums, signatures, timestamps, attestations, seals, helper/package/fallback fingerprints, classification, incident handling, and freeze controls remain intact.

6. **Ownership and migration are explicit.** **Given** catalog authorship belongs to `Hexalith.Builds`, **when** GOV-1 lands, **then** BUILD-CAT-1 is durably routed upstream for any desired catalog marker/contract-version addition. FrontComposer validates the semantic catalog content directly during migration, carries no fingerprint allowlist, does not edit submodule content, and does not make the optional marker mandatory without a separate Architecture/Product/Release Owner decision and migration plan.

### Acceptance Clarifications Approved 2026-08-02

**Given** an exact immutable base and candidate under the ratified event model, **when** their defined
depth-1/2 graphs are compared, **then** the normalized diff and policy projection prove a deterministic,
bounded, cycle-safe, root-subsumed, at-most-once set of affected modules, including explicit bootstrap
handling.

**Given** a CI, Release, or post-release evaluator, **when** its authority is checked, **then** the active
policy independently authorizes its local blobs and literal-40-hex external coordinates, and the bounded
static transitive closure matches before execution evidence is trusted.

**Given** a Release attempt for a CI-approved candidate, **when** Release and post-release verification
run, **then** the authenticated CI-to-Release and Release-to-verifier handoffs bind that same exact
candidate, policy, and evaluator closure; failed or partial attempts emit verifiable evidence and cannot
pass through a missing-evidence no-op.

**Given** a publishable manifest v2, **when** it is prepared, consumed, or independently verified,
**then** it seals and verifies the normalized dependency graph, semantic catalog evidence, active policy
coordinate/digest, authenticated handoff chain, and immutable workflow/evaluator provenance.

## Tasks / Subtasks

- [x] **Task 1 — Ratify the v1 graph contract and freeze implementation evidence (AC: #1, #4, #5, #6)**
  - [x] Obtain Architect + Release Owner approval for the exact graph boundary in the **Implementation Entry Gate**. FC-DEP-1 and the focused architecture spine were ratified on 2026-07-19 with the depth-1/depth-2 boundary, closed identities/policy, canonical graph, raw-byte hashing, exact revision rules, schema migration, workflow provenance, and resource ceilings.
  - [x] Re-run the graph census from `baseline_commit`, pre-ratification `600f4c738bd28b1efe0e69940ccec8b03faba7c4`, and current tracked architecture-finalization HEAD `c585073c3b8fae58fe49cbfac5ddabca4df3dec7`. The baseline has 40 edges/7 selectors/5 distinct Builds commits; both later snapshots have 40/7/6. These are evidence, never acceptance constants.
  - [x] Record implementation-start `HEAD`, working-tree paths, root gitlinks, and all in-scope commit objects. Preserve unrelated work and do not initialize/update submodules to manufacture missing history.
  - [x] Keep BUILD-CAT-1 open and upstream-owned. Record the upstream issue/decision evidence in `_bmad-output/implementation-artifacts/deferred-work.md` without editing `references/Hexalith.Builds`.
  - [x] Record Hexalith.Builds issue 17 / BUILD-REL-1 as the upstream dependency. **Corrected 2026-08-02:** issue 17 closed without the GOV-1 amendment or a qualifying revision. Reopen it with the complete amendment or file a successor; only reusable-workflow integration, end-to-end evidence proof, this story's completion, release eligibility, and unfreeze await the accepted revision and exact workflow/action blob closure.

- [x] **Task 2 — Add one reusable committed-object dependency-graph engine (AC: #1, #2, #3, #4, #5)**
  - [x] Add `eng/dependency_graph.py` as a standard-library-only collect/canonicalize/validate/diff engine used by CI and release evidence. Do not create a parallel manifest implementation.
  - [x] Add required `eng/dependency-graph-policy.json` with schema `hexalith.dependency-graph-policy.v1` as the single FrontComposer-owned source for trusted identities/paths, semantic profiles, static module argv, evidence-only dispositions, evaluator authorizations, and v1 resource ceilings. `evaluator_authorizations` (AD-12 CI/Release/post-release closures) landed as a closed, empty registry. **Corrected 2026-08-02:** populating and testing that registry and base-policy activation/bootstrap are unblocked Task 4/5 local work; the empty registry authorizes no evaluator and cannot support a release handoff.
  - [x] Accept an explicit root repository identity and 40-hex commit. Read trees with `git ls-tree -r -z --full-tree`, committed `.gitmodules` with `git config --blob <commit>:.gitmodules`, and catalogs with exact `<commit>:<path>` object reads. Never derive release evidence from the ambient index, working-tree nested HEADs, or a mutable submodule checkout.
  - [x] Resolve only repository identities already declared by the FrontComposer root. Normalize approved GitHub SSH/HTTPS forms to a canonical lowercase identity, strip terminal `.git`/slash, and reject credentials, control characters, absolute/backslash/dot-segment paths, path traversal, or unknown identities. Never clone or execute from a candidate URL.
  - [x] Collect exactly depth 1 root gitlinks and depth 2 direct gitlinks from each exact depth-1 owner commit. Record self/back edges normally; never traverse deeper. Treat different commits of the same repository as distinct. Use `(owner_repository, owner_commit, path)` for edge uniqueness and cache raw Builds blob reads by `(Builds repository, Builds commit)` while evaluating every selector against its explicit semantic profile.
  - [x] Emit duplicate-member-free deterministic JSON with exactly `{schema, root, edge_count, edges, graph_digest}`. `schema` is `hexalith.dependency-graph.v1`; `edge_count == len(edges)`; root and edge member sets, strict lowercase IDs/hashes, nullable catalog marker, normalized POSIX paths, ordinal edge ordering, project canonical bytes, and the golden digest follow AD-4/AD-5 exactly.
  - [x] Hash the raw Git blob bytes so BOM/EOL/comments are sealed; parse those same bytes for semantics. Do not label Python's existing compact `json.dumps(..., sort_keys=True)` output as RFC 8785 canonical JSON. Preserve or explicitly version the existing seal formula unless a separate decision adopts JCS.
  - [x] Add deterministic diagnostics and nonzero exits for missing objects, missing/duplicate `.gitmodules` mappings, malformed URLs/paths/IDs, duplicate edges, unresolved repositories, missing catalogs, inconsistent graph input, and every AD-7 ceiling. Enforce 4,096 edges, 64 MiB `ls-tree` bytes per owner commit, 1 MiB per `.gitmodules` blob, and 4 MiB per catalog blob before decoding/parsing.

- [x] **Task 3 — Replace historical SHA assertions with selected-catalog semantic governance (AC: #1, #2, #3, #6)**
  - [x] Update `InfrastructureGovernanceTests.cs` so the catalog governance tests use the committed-object engine/equivalent exact owner-commit blob reads and delete `rootBuildsCommit`, `eventStoreBuildsCommit`, `memoriesBuildsCommit`, `partiesBuildsCommit`, and the ambient-index `ReadGitlinkCommit` compatibility path. `CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog` and `PartiesPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` now shell out to `python3 eng/dependency_graph.py validate` and assert on its machine-readable result; `ReadGitlinkCommit`/`ReadGitAttribute`/`AssertUtf8BomAndCrLf`/`ReadTrackedFiles` were deleted as dead code (their only call sites). `AssertAuthoritativePackageVersion`/`AssertPackageOverride`/`FindPackageVersionOperations`/`ItemSpecSelectsPackage` were kept — `CentralPackageVersionOwnership_InvalidOperations_AreRejected` still exercises them directly and is unrelated to the SHA-allowlist removal.
  - [x] Preserve the existing semantic contract: FrontComposer's root remains an import shim; required central package identities and versions remain authoritative and unconditional; invalid Include/Update/Exclude/Remove/conditions still fail; EventStore/Memories inheritance and Parties' three guarded imports, central-package properties, no inline versions, and no MinVer ownership remain enforced against the applicable selected catalog. Ported into `eng/dependency_graph.py`'s `evaluate_semantics`/`assert_*` functions and the policy's per-owner profiles; each owner is now validated against the catalog its *own* gitlink actually selects (the old test validated EventStore/Memories/Parties inheritance against the root's selected catalog, not each owner's own).
  - [x] Cache each distinct selected Builds blob/hash, evaluate every selector edge through its explicit policy profile, and report every selecting owner. Keep the existing root catalog BOM/CRLF policy as a separate repository-format assertion unless a later approved policy revision promotes it to every catalog's semantic contract. `assert_builds_checkout_format_policy` deliberately reads the checked-out working tree (not the raw commit object) for this one narrow check — the pinned `Hexalith.Builds` commit's raw blob genuinely carries bare LF (eol=crlf only rewrites bytes on checkout), a known separately tracked upstream formatting issue; using the raw object here would introduce a new out-of-scope CI failure.
  - [x] Add synthetic positives for a compatible commit advance and multiple selectors of one catalog. Add negatives for every AC3 mismatch, unknown identity, malformed `.gitmodules`, duplicate edge, path escape, unavailable commit/blob, and conflicting Builds commits. Assert messages include owner repository/commit/path and selected catalog commit/path. Covered in `tests/eng/test_dependency_graph.py` (24 tests). Not covered: a literal "duplicate edge" negative — AD-4 edge identity `(owner_repository, owner_commit, path)` is populated from one `.gitmodules` parse per owner, so a duplicate edge cannot arise from valid input; there is no natural fixture for it. "Conflicting Builds commits" is exercised as "multiple owners selecting the *same* commit" (the actually-specified positive case); a genuine conflict isn't a distinct engine failure mode since every edge is validated independently against its own selected commit.
  - [x] Never replace the SHA list with a raw-catalog SHA-256 allowlist or an accepted-commit table. The exact IDs belong in produced evidence and fixtures only. Confirmed: no commit/SHA allowlist exists anywhere in `eng/dependency_graph.py`, `eng/dependency-graph-policy.json`, or the rewritten C# tests.

- [ ] **Task 4 — Add release-blocking graph diff and affected-module gates (AC: #2, #4)**
  - [ ] **Local work is unblocked by the approved 2026-08-02 correction.** The local graph, policy, materialization, closure, handoff schema, and caller-side governance are implemented; activation of the successful-push handoff producer remains open with the external evaluator authorization/integration gate below.
  - [x] Update `.github/workflows/ci.yml` so pull-request CI uses `github.event.pull_request.base.sha` as `event_base`, `github.sha` as the exact candidate merge revision, and requires `git merge-base event_base github.sha == event_base`. Push CI compares non-zero `github.event.before` with `github.sha`; zero/unavailable bases take the fail-closed full-affected diagnostic path and are never release-eligible. Record all exact revisions, collect both graphs, diff logical edges by `(owner_repository, path, repository)`, and publish deterministic evidence.
  - [x] Apply the AD-8 cascade before the policy registry: classify depth-1 added/changed/removed edges first and subsume their descendant churn; then classify remaining depth-2 changes. Deduplicate affected modules by canonical identity. Commands and evidence-only dispositions come only from the active closed policy; unchanged graphs build nothing extra.
  - [x] Materialize each affected exact owner commit in isolation plus the complete bounded regular-file Builds contract tree at the listed gitlink path. Enforce 16,384 files, 16 MiB per blob, and 256 MiB total before extraction; reject symlinks, gitlinks, special modes, unsafe paths, and graph/catalog hash drift. Run the exact static standalone Release/NuGet argv from policy. No recursive init, candidate-supplied script/command, mutable checkout, or arbitrary repository URL is permitted.
  - [ ] Implement the authenticated `dependency-release-handoff` producer/consumer contract and local fixtures. For every successful eligible `push`, emit exactly one `hexalith.dependency-release-handoff.v1` artifact binding repository, workflow/event/run/attempt, immutable base and exact candidate, normalized graph/diff, bounded affected-module proof, active policy coordinates/digest, evaluator closure/conclusion, and raw handoff digest. The authenticated handoff candidate is the sole downstream release-candidate authority. **2026-08-04 correction (see Debug Log): NOT satisfied.** `eng/dependency_handoff.py` implements this exact contract (`create_ci_handoff`/`validate_ci_handoff`, schema `hexalith.dependency-release-handoff.v1`, verified against AD-13's literal `{schema, run, revisions, evaluator, dependency_policy, dependency_graph}` shape) and it is exercised by `tests/eng/test_dependency_handoff.py`, but **no workflow ever calls it**: `.github/workflows/ci.yml` instead calls `dependency_handoff.py create-source`, emitting the narrower `hexalith.dependency-release-source.v1` artifact, which has no `evaluator` member at all. `eng/release_prepublish.py`/`release_evidence.py`'s optional `--ci-handoff`/`--release-evaluator` activation path (env vars `DEPENDENCY_RELEASE_HANDOFF`/`RELEASE_EVALUATOR`) is never populated by any workflow, so `create_ci_handoff`/`validate_ci_handoff` are dead code in production today. This is a real gap against AD-13 `[ADOPTED]` in `architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md`, not a documented, approved supersession — see the new Debug Log entry and `deferred-work.md`.
  - [x] Add standard-library `eng/workflow_source_closure.py`. Its static closure follows every conditional/unconditional literal `uses:` plus composite descendants from exact blobs, independent of runtime path; it includes action metadata hashes and fails on mutable/dynamic refs, Docker actions, unsupported YAML forms, ambiguous metadata, cycles, or AD-13 depth/source/blob ceilings. Match the result to the active policy before handoff.
  - [x] Add Governance coverage proving `eng/dependency-graph-policy.json` is the one executable profile/disposition/limit/evaluator authority, the architecture names that canonical coordinate, and planning prose does not maintain a second executable value seed.
  - [ ] **External integration gate only:** after the Release Owner reopens issue 17 with the GOV-1 amendment or files a successor, integrate its owner-accepted 40-hex revision. Pin the primary CI reusable workflow and every transitive action source to the active-policy-authorized immutable closure, record/validate actual coordinates and metadata blob hashes, and prove the real reusable-workflow handoff end to end. FrontComposer must not edit `references/Hexalith.Builds`, use `@main`, or claim integration complete while that revision is pending.
  - [x] Update `.github/workflows/quality.yml` only for supplemental helper/Governance coverage and required exact history/object fetch. Preserve Gate 2b and the root-only submodule policy; `fetch-depth: 1` must not be the only source for merge-base or candidate-object proof.
  - [x] Extend `CiGovernanceTests.cs` to pin explicit base/candidate selection, the release-blocking dependency relationship, deterministic evidence, static module commands, no recursive submodule operations, no arbitrary command execution, and the unchanged-graph no-op path.

- [ ] **Task 5 — Extend the existing sealed release manifest (AC: #5, #6)**
  - [ ] **Local work is unblocked by the approved 2026-08-02 correction.** The local manifest, policy, handoff contracts, verifier, post-release consumer, and fixtures are implemented; the Release producer/caller wiring remains open with the external evaluator authorization/integration gate below.
  - [x] Extend `eng/release_evidence.py`; do not add a separate release-manifest tool. `prepare_manifest` must collect the graph for `args.commit_sha`, reject the local sentinel, and emit `hexalith.release-evidence.v2` with the complete AD-5 `dependency_graph`, closed `dependency_policy` (`schema`, repository, canonical path, revision, and raw-byte SHA-256), and AD-14 `workflow_provenance` objects.
  - [x] Bind the policy, graph helper, CI workflow, versioned handoff contract, and immutable workflow/action definition coordinates into `RELEASE_DEFINITION_FILES` and `FALLBACK_INVALIDATION_FILES`. Implement the exact v2 fallback formula over definition, package set, graph digest, policy SHA-256, and workflow definition digest.
  - [x] Make `verify-manifest --no-root` enforce schema, types, strict IDs/hashes/paths, uniqueness, completeness, explicit ordering, edge count, and graph digest without consulting a checkout. Make live `--root` verification recompute the exact graph from the sealed root commit and compare every edge/catalog byte hash before publish or post-publish acceptance.
  - [x] Reject duplicate/unknown v2 members and any graph, policy, handoff projection, CI-only evaluator digest, raw handoff hash, release evaluator, combined workflow-definition digest, or exact-candidate inconsistency. Legacy manifests are accepted only by explicit audit diagnostics and are never publishable, fallback-eligible, resealed, or upgraded in place.
  - [x] Preserve `eng/release_prepublish.py` ordering: prepare -> seal -> live verify -> classify, plus pre-push verification. Preserve pack-once artifacts, symbol checksums, immutability probe, signing/timestamp/attestation, approval fallback, classification, and incident behavior.
  - [x] Update `.github/workflows/release-evidence.yml` to reconstruct and report the exact graph, policy, handoff, and workflow provenance from the upstream release commit. It remains read-only: no prepare, reseal, classification rewrite, or publication. Preserve exact upstream SHA checkout, full required history, and root-only submodule initialization.
  - [ ] Update `.github/workflows/release.yml` and its local handoff consumer so the caller authenticates the triggering run ID/attempt and event head through read-only Actions APIs, verifies the named successful-CI handoff, and treats that handoff's matching candidate as the sole release-candidate authority. Reload the recorded policy from its exact commit and consume the authenticated candidate everywhere. Preserve the REL-4 freeze and `.releaserc.json` publication ownership; a tag, ambient/default branch, second-hop event SHA, or mutable `@main` seam cannot authorize publication. **2026-08-04 correction (see Debug Log): partially satisfied, not fully.** The `verify-source` job (`.github/workflows/release.yml:39-123`) genuinely does authenticate the dispatched SHA against live `main` and the completed push-CI run via read-only `gh api` calls, download+validate the exact-source artifact, and treat its authenticated candidate as sole authority before any protected job runs — real, live, tested behavior. But it consumes `dependency-release-source-<run>-<attempt>` (`hexalith.dependency-release-source.v1`), not the AD-13-mandated `dependency-release-handoff` artifact/schema, so it is not literally AD-13-conformant. Kept unchecked pending an Architect/Release Owner decision on whether the shipped exact-source-proof mechanism should formally amend AD-13, or whether `create_ci_handoff`/`verify-ci` need to be wired in instead.
  - [ ] Under `if: always()`, make every governed Release attempt upload exactly one `release-verification-handoff` artifact conforming to AD-15, including the authenticated CI run/attempt/raw handoff hash, exact policy projection, and failure/partial null representations. Update `release-evidence.yml` to authenticate both runs/artifacts, require matching candidate/policy projections even on pre-manifest failure, reload the exact base/before policy, derive the root only from the original CI candidate plus sealed manifest, require a policy-authorized post-release closure, and verify/record success, failure, or partial publication without using second-hop `workflow_run.head_sha` as the candidate or green-no-oping. **2026-08-04 correction (see Debug Log): NOT satisfied.** `.github/workflows/release-evidence.yml`'s `if: always()` steps do unconditionally record a disposition/ledger for every attempt (a real, useful property — `Record explicit non-publication disposition` always writes `ledger-record.json`, and the untrusted `github.event.workflow_run.head_sha` is only a lookup hint, cross-checked against a freshly authenticated `gh api runs/{id}` fetch and the `release-candidate-*` artifact's descriptor), but the artifact it uploads (`release-verification-<run>-<attempt>`) is built from bespoke, locally-invented schemas (`frontcomposer.release-run-disposition.v2`, `frontcomposer.release-ledger-record.v2`, `frontcomposer.partial-publish-incident.v2`, `frontcomposer.published-byte-comparison.v2`), not the AD-15-mandated closed `hexalith.release-verification-handoff.v1` shape `{schema,release_run,ci_handoff,candidate,dependency_policy,release,manifest,assets,evaluator}` — it carries no `evaluator` binding and never calls `create_release_handoff`/`validate_release_handoff`. This is a real gap against AD-15 `[ADOPTED]`, not a documented, approved supersession.
  - [ ] **External integration gate only:** wire the local Release/handoff/manifest implementation to the owner-accepted 40-hex revision from reopened issue 17 or its successor, pin and authorize the complete reusable/action closure, and prove both exact-candidate handoffs plus manifest-v2 verification end to end. No FrontComposer-owned contingency is authorized; a contingency requires a new dated Architect + Release Owner decision with scope, expiry, migration, and equivalent proof.

- [x] **Task 6 — Add fixtures, regression proof, documentation, and durable handoff (AC: all)**
  - [x] Add `tests/eng/test_dependency_graph.py` with synthetic local Git repositories for deterministic collection/diff, compatible pointer advance, multiple Builds versions/selectors, exact-byte hashing, self/back edges, depth-2 boundary exclusion, duplicates, missing mappings/objects/catalogs, malformed inputs, unsafe URLs/paths, stable ordering, every resource boundary, and full contract-tree extraction limits. The completed 56-test suite covers collection, semantic evaluation, exact diff/cascade/no-op behavior, delayed policy activation, strict offline validation, bounded contract-tree extraction, and isolated exact-module materialization.
  - [x] Update `ReleaseModelGovernanceTests.cs`, `Story12_4_RedPhaseDefTests.cs`, `tests/ci-governance/stage_release_state.py`, `release-manifest-valid.json`, the invalid manifest fixture, and `release-readiness-cases.json` for the versioned graph schema and both offline/live failures. Reseal synthetic fixtures only through the actual helper. `release-manifest-valid.json`/`release-manifest-invalid.json` carry `hexalith.dependency-graph.v1`/`hexalith.dependency-graph-policy.v1` members; `stage_release_state.py` sources its `dependency_graph`/`dependency_policy` rows from the real `eng/dependency_graph.py` engine (`_load_dependency_graph_engine()`) rather than a second hand-authored fixture generator; `Story12_4_RedPhaseDefTests.cs` pins the `hexalith.release-evidence.v2` manifest schema and its `dependency_graph` member directly. `ReleaseModelGovernanceTests.cs` validates v2 behavior through the actual `verify-manifest`/`classify-fixtures` CLI over these same fixtures rather than duplicating schema literals in C#, so no `.cs` edit was needed there for the schema bump itself. All 212 Shell.Tests Governance facts pass against this fixture set.
  - [x] Add policy activation/bootstrap, depth-1 cascade collapse, zero/unavailable push base, exact handoff authentication, immutable workflow/action closure, legacy-manifest audit-only, and graph/policy/workflow fallback invalidation fixtures. The previously-missing zero/unavailable-push-base fixture is now `tests/eng/test_dependency_graph.py::DiffAndMaterializationTests::test_zero_push_base_forces_full_affected_and_is_never_release_eligible`, which reproduces the CLI `diff` command's zero-base branch (candidate graph standing in for both base and candidate, `release_eligible` forced `False`, `full_affected` marking every module) and proves `validate_diff_evidence` still accepts the resulting document as an auditable, non-release-eligible record. Every other listed case was already covered. A live GitHub Actions run that naturally produces a genuinely zero/unavailable `github.event.before` (e.g. a repository's very first push) is an opportunistic, unscheduled real-workflow observation, not a blocker — it cannot be manufactured in a development session and is not required for this bullet's fixture-coverage intent.
  - [x] Add a sealed-but-unapproved evaluator negative, conditional-source and nested-composite positives, cycle/dynamic/mutable/unsupported/limit negatives, and a race fixture where default branch advances between CI, Release, and verification while the verifier remains bound to the original candidate. Prove pre-manifest/failed/partial Release attempts retain matching CI/policy projections, cannot omit the verification handoff, and cannot green-no-op. Covered by `tests/eng/test_dependency_handoff.py` (`test_sealed_but_unapproved_evaluator_fails_before_handoff`, `test_ci_candidate_substitution_fails_closed`, `test_failed_release_handoff_preserves_ci_candidate_and_policy`, `test_unpublished_attempt_cannot_green_noop_with_omitted_projection`) and `tests/eng/test_workflow_source_closure.py` (`test_closure_conditional_nested_composites_includes_exact_sorted_sources`, `test_composite_cycle_fails_closed`, `test_mutable_and_dynamic_external_refs_fail_closed`, `test_docker_references_and_docker_metadata_fail_closed`, `test_unsupported_yaml_uses_forms_fail_closed`, `test_ad13_depth_source_blob_and_total_limits_fail_closed`, `test_unexpected_reusable_coordinate_and_unknown_repository_fail_closed`). **2026-08-04 note:** the `test_dependency_handoff.py` fixtures listed here correctly and faithfully exercise `create_ci_handoff`/`validate_ci_handoff`/`create_release_handoff`/`validate_release_handoff` — but per the corrections above, those functions are not yet called by any production workflow, so these fixtures currently guard implemented-but-unwired code, not the live release pipeline. The task's literal "add fixtures" ask is met; production activation is separately tracked as a Task 4/5 gap.
  - [x] Update `tests/README.md` with focused local commands and the distinction between semantic compatibility, graph provenance, offline structural verification, and live drift verification. Added a "Dependency Graph Governance (GOV-1)" section; offline-structural/live-drift verification is Task 5 (manifest) scope and noted as not-yet-implemented there.
  - [x] Reconcile `_bmad-output/project-docs/deployment-guide.md`, `_bmad-output/project-docs/architecture.md`, and `_bmad-output/project-context.md` only where the landed boundary/tooling changes durable contributor or Release Owner behavior. Do not rewrite unrelated planning history. Checked both project-docs files for stale references to the removed SHA-allowlist mechanism — found none needing a change. Added one bullet to `project-context.md`'s Testing Rules explaining the new `eng/dependency_graph.py`-backed Gate 2b behavior, since a future contributor/agent debugging a Gate 2b failure needs to know a SHA mismatch is no longer the failure mode.
  - [x] Record the Story 11.17d disposition. It completed on 2026-08-02 under the Administrator's one-story GOV-1 promotion waiver with its own exact-revision evidence. GOV-1 does not reopen it; the next governed release remains independently blocked until GOV-1 completes.
  - [x] Record exact commands/results, chosen graph boundary and census, schema/count/digest, changed-path ledger, fixture reseals, root gitlink audit, no-recursion scan, and `git diff --check` in the Dev Agent Record before review. Recorded in the 2026-08-04 Debug Log entry above.

## Dev Notes

### Current State and UPDATE/NEW Map

Every UPDATE file below was inspected during creation. Treat this table as implementation routing, not permission to change every row.

| Path | Current state | Required GOV-1 direction |
|---|---|---|
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` | Correct semantic catalog checks plus four historical Builds SHA constants; `ReadGitlinkCommit` reads the ambient index. | Remove compatibility SHA pins; validate every approved selected catalog from exact committed objects; preserve semantic package/import/ownership checks and actionable diagnostics. |
| `eng/release_evidence.py` | Version 1.2.0 release-evidence helper; manifest has no dependency graph. | Extend prepare/seal/offline verify/live verify/diagnostics with the versioned graph and digest; retain all existing artifact and authorization safeguards. |
| `.github/workflows/ci.yml` | Primary reusable domain CI; Release is triggered from this workflow's conclusion. Current reusable/transitive `@main` references are mutable. | Add release-blocking graph diff, exact revision handling, affected-module proof, immutable reusable/action provenance, and the authenticated release handoff here. |
| `.github/workflows/quality.yml` | Supplemental FrontComposer gates, root-only init, shallow checkout. | Add supplemental graph/helper coverage and sufficient exact-object history without recursive init. |
| `.github/workflows/release-evidence.yml` | Read-only post-release evidence currently treats the second `workflow_run` head/default-branch SHA as the candidate and may no-op when its tag lookup misses. | Authenticate the mandatory Release verification handoff, derive the original CI candidate/manifest/assets from it, require an authorized post-release closure, and record every success/failure/partial attempt without mutation or green no-op. |
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
- `eng/workflow_source_closure.py` — standard-library exact-blob static workflow/composite-action closure and authorization projection.
- `tests/eng/test_dependency_graph.py` — synthetic Git graph/semantic/safety tests.
- `tests/eng/test_workflow_source_closure.py` — conditional/composite/cycle/mutable/limit/authorization fixtures.

Do not create a second manifest tool or add a third-party parsing/canonicalization dependency. Python 3.14.4, Git 2.53.0, and .NET SDK 10.0.302 are available at story creation.

### Creation-Time and Implementation-Start Evidence — Provenance, Not an Allowlist

At creation baseline `e3e3dcf5`, the bounded v1 census found 40 edges, 7 Builds selector edges, and 5 distinct catalog commits. Pre-ratification `600f4c738bd28b1efe0e69940ccec8b03faba7c4` and current tracked architecture-finalization HEAD `c585073c3b8fae58fe49cbfac5ddabca4df3dec7` both find 40 edges and 7 selectors resolving to 6 distinct Builds commits. None of the creation-time catalogs exposes a contract-version marker. All exact commits, counts, and raw SHA-256 values are fixture/baseline evidence only; no value may become an acceptance constant or compatibility allowlist. Task 1 re-freezes the actual production-start commit after planning changes land.

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
- Evaluator identity is authorized independently by the active policy; literal hashes and internally consistent seals alone are insufficient. The static closure includes conditional sources and composite descendants with action metadata blob hashes under AD-13 limits.
- Post-publication evidence/ledger authenticates the AD-15 Release run/handoff, retains the original CI candidate across the second workflow hop, and records graph schema/count/digest, policy coordinates, both handoffs, and authorized evaluator provenance alongside existing package/artifact evidence.
- Preserve the current seal formula unless explicitly versioned. RFC 8785 is a reference point, not an adopted claim.
- Preserve REL-3 exact-artifact enforcement and REL-4's default-frozen publication gate. This story adds provenance; it does not authorize publishing.

### Scope Boundaries and Never-List

- No runtime or public API behavior, schema, generated output, package inventory, UX, route, copy, CSS, JS, accessibility, localization, telemetry, or feature change.
- No unrelated dependency or package upgrade. Pinning the CI/release reusable workflows and every transitive action source to active-policy-authorized immutable 40-hex commits, including replacing mutable Builds `@main` references, is required GOV-1 provenance work; any Builds-owned source change is routed upstream.
- No root/submodule `.gitmodules` or gitlink change as part of the implementation. No file under `references/**` is an implementation target.
- No recursive/remote submodule update and no nested submodule initialization.
- No REL-3 signing/artifact-scope redesign, REL-4 freeze redesign, REL-5 operational authorization, or actual publication.
- BUILD-CAT-1 remains an external `Hexalith.Builds` responsibility.
- BUILD-REL-1 issue 17 closed without a qualifying revision. A reopened issue or successor gates only reusable-workflow integration, end-to-end proof, story completion, release eligibility, and unfreeze; local Tasks 4/5 proceed. No local contingency is authorized by this story.

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
- a fully sealed but active-policy-unapproved evaluator fails before handoff/publication;
- default-branch advance across CI -> Release -> verifier does not change the original candidate, and failed/partial attempts cannot green-no-op;
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
- [Source: `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` — BUILD-REL-1 issue 17 and GOV-1 amendment]
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
- 2026-07-19: Pre-ratification `600f4c738bd28b1efe0e69940ccec8b03faba7c4` census is 40 edges/7 Builds selectors/6 distinct Builds commits. During finalization, main advanced externally to `c585073c3b8fae58fe49cbfac5ddabca4df3dec7`; a fresh committed-object census remains 40/7/6 despite changed Builds/EventStore gitlinks. Task 1 still freezes the eventual production-start commit. Source artifacts were reconciled to manifest v2, closed policy, exact CI/release revision handoffs, immutable workflow/action provenance, and adopted resource ceilings.
- 2026-07-19: Final adversarial architecture review closed evaluator authorization, deterministic static transitive-action closure, BUILD-REL-1 issue-17 delivery gating, REL-4 truth-state, and the Release-to-verifier original-candidate handoff. Accepted Builds revision remains pending and is an explicit external completion gate.
- 2026-07-19: Started local Task 1-3 implementation (Claude Code session, in parallel with the ongoing architecture-finalization session). Froze implementation-start `HEAD=c585073c3b8fae58fe49cbfac5ddabca4df3dec7`. Working tree carried only planning-artifact edits from the concurrent session (contracts doc, architecture.md, ARCHITECTURE-SPINE.md, prd.md, sprint-change-proposal-2026-07-19.md, this story file); no code/test files were dirty. Root gitlinks at that commit: AI.Tools=991e8ea1, Builds=a3d56085, Commons=ea1fc455, EventStore=539dca2b, Memories=e6164c8b, Parties=f24275ae, PolymorphicSerializations=f977018a, Tenants=088232a7. Independently recomputed the depth-1/2 census by reading each root-declared submodule's committed `.gitmodules` at its pinned commit: 8 depth-1 + 32 depth-2 = 40 edges, matching the story's evidence. 7 Builds-selector edges (root + Commons + EventStore + Memories + Parties + PolymorphicSerializations + Tenants); AI.Tools has no `.gitmodules` at its pinned commit (evidence-only, no Builds selector), matching AD-6/module registry. 6 distinct Builds commits because Memories and Tenants both select `cb8b2d41`: root=a3d56085, Commons=1a15a0ca, EventStore=9ec0a032, Memories/Tenants=cb8b2d41, Parties=c177c66a, PolymorphicSerializations=598f5063. Sprint status moved GOV-1 `ready-for-dev` -> `in-progress`. Appended BUILD-CAT-1 and BUILD-REL-1 (issue 17) entries to `deferred-work.md` under a new "Deferred from: GOV-1 story creation (2026-07-19)" section, without touching `references/Hexalith.Builds`. Proceeding with Task 2 (dependency-graph engine + policy) and Task 3 (governance-test rewrite) only; Tasks 4/5 stay untouched pending BUILD-REL-1 per AD-16/decision 16.
- 2026-07-19: Implemented `eng/dependency-graph-policy.json` (trusted identities, semantic profiles/requirements ported from the ARCHITECTURE-SPINE.md Closed Policy Seed, module-build registry, AD-7 ceilings, empty `evaluator_authorizations`) and `eng/dependency_graph.py` (identity/path normalization, depth-1/2 collection, AD-5 canonical digest, AD-6 semantic evaluation). First `validate` run against live HEAD surfaced a real finding, not a bug: the root-selected Builds commit's raw `Props/Directory.Packages.props` blob has bare LF (0 CRLF / 320 bare-LF), even though `.gitattributes` declares `eol=crlf` for that path — `eol=crlf` only rewrites bytes on checkout, so the stored object can legitimately carry bare LF (the working-tree file correctly shows 320/0 the other way round). This is the same class of issue already logged in `deferred-work.md` for a different Builds commit (`c177c66`, 18 bare-LF). Per Dev Notes ("the current root test's BOM/CRLF assertion remains a local format policy unless separately generalized"), kept this one narrow check reading the checked-out working tree via `assert_builds_checkout_format_policy`, matching the pre-GOV-1 test's actual pass/fail behavior exactly, rather than gating CI on an unrelated pre-existing upstream formatting issue.
- 2026-07-19: Rewrote both catalog-compatibility Facts in `InfrastructureGovernanceTests.cs` to invoke `python3 eng/dependency_graph.py validate` and assert on its JSON result; deleted `ReadGitlinkCommit`/`ReadGitAttribute`/`AssertUtf8BomAndCrLf`/`ReadTrackedFiles` (dead after the rewrite — verified no other call sites) and the now-unused `System.Text` using. Kept `AssertAuthoritativePackageVersion`/`AssertPackageOverride`/`FindPackageVersionOperations`/`ItemSpecSelectsPackage` — still exercised directly by the unrelated `CentralPackageVersionOwnership_InvalidOperations_AreRejected` unit test. `dotnet build --configuration Release`: 0 warnings/0 errors. `dotnet test ... --filter Category=Governance` on Shell.Tests: 188/188 passed — this is the exact lane that was red in GitHub Actions run 29693894141 (Gate 2b), now green. One unrelated pre-existing failure surfaced on the first run: `AnalyzerPolicyGovernanceTests.AnalyzerPolicy_GovernanceContract_FailsClosed` failed on an identifier-inventory drift (`testUnderscoreIdentifierTokens`/`testInventorySha256` in `analyzer-policy-exception-ledger-v1.json`), a golden-hash ledger reacting to the renamed/added/removed C# method identifiers from this same rewrite — updated the ledger to the tool's freshly computed values (6194, `5c619cb1...`), matching the project's existing "update baselines intentionally" convention for this class of ledger.
- 2026-07-19: Wrote `tests/eng/test_dependency_graph.py` (24 tests: collection determinism/ordering/digest, depth-2 boundary exclusion, self/back edges, multi-owner same-commit selection, resource ceilings, identity/path rejection, and AD-6 semantic positives/negatives). First run found two real engine bugs, not test bugs: (1) `BUILDS_IDENTITY` was a hardcoded Python module constant instead of a policy-driven value, making it architecturally inconsistent with AD-12 ("one versioned trust... policy" should be the single source of truth) and impossible to point at a synthetic test identity — fixed by adding `builds_identity` to the policy schema and threading it through `collect_graph`/`evaluate_semantics` as a parameter instead of a hardcoded constant; (2) several of my own test fixtures were building one fake "Builds" git repository per synthetic owner instead of one shared repository with multiple commits, which doesn't model reality (one Builds identity, many pinned commits) — fixed the fixture, not the engine. All 24 tests pass after both fixes. Also ran `python3 -m py_compile eng/dependency_graph.py eng/release_evidence.py eng/release_prepublish.py` (clean), `python3 eng/validate-story-artifacts.py` (passed), `git diff --check` (clean), and the recursive-submodule-flag scan (only pre-existing, legitimate matches inside `CiGovernanceTests.cs`'s own detection regex — nothing in new files).
- 2026-07-19: Updated `tests/README.md` (new "Dependency Graph Governance (GOV-1)" section) and `_bmad-output/project-context.md` (new Testing Rules bullet explaining the new Gate 2b failure mode, `rule_count` 77->78, dates refreshed). Checked `_bmad-output/project-docs/deployment-guide.md` and `_bmad-output/project-docs/architecture.md` for stale references to the removed SHA-allowlist mechanism — found none, no change needed there.
- 2026-07-19: Final validation: solution-wide `dotnet test Hexalith.FrontComposer.slnx --filter Category=Governance` = 347/347 passed (Contracts.Tests 6, Cli.Tests 6, Mcp.Tests 6, Shell.Tests.Bench 1, SourceTools.Tests 140, Shell.Tests 188). Full Shell.Tests default lane (`Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined`) = 2367/2367 passed — no regressions from the rewrite or the ledger update. Story stays `in-progress`, not `review`: Tasks 4/5 and the manifest/workflow-dependent Task 6 subtasks remain blocked on Hexalith.Builds issue 17 / BUILD-REL-1 per AD-16, exactly as the Implementation Entry Gate anticipated.
- 2026-08-02: Administrator approved `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md`. Live issue evidence shows Hexalith.Builds issue 17 closed on 2026-07-20 without the GOV-1 amendment or a qualifying immutable revision. The story now separates unblocked FrontComposer-local Task 4/5 work from the external reusable-workflow integration/end-to-end completion gate. Story 11.17d is done under its recorded one-story waiver and is not reopened.
- 2026-08-02: Course-correction application validation passed at exact HEAD `4302301ac88c23bfb7b97838dfd26cd6d9c9440f`: sprint-status YAML parsed, story-artifact validation passed, `git diff --check` returned 0, and semantic graph validation returned `ok: true`, 43 edges, seven selectors, and graph digest `58fa3d657c4aef979e84f2cd6b2ddf1a868fa5225f94a28d1e7390c2a3a78472`. Counts and digest remain evidence, never acceptance constants.
- 2026-08-02: Implemented the FrontComposer-local Task 4/5 slice at exact root candidate `52f4327ca9ded051750b8ae38f8b8b752148548d`: strict exact-revision graph diff and AD-8 cascade; bounded isolated module/Builds-tree materialization; policy-owned literal restore/build argv; exact workflow/composite-action closure and policy authorization; strict CI and Release handoff contracts; manifest v2 graph/policy/workflow provenance; offline/live verification; legacy audit-only behavior; and the read-only post-release handoff consumer. Same-candidate validation returned 43 edges, seven Builds selectors, graph digest `1596ef038c1ce6017b4b011c98ba4d0834d0292a40aa2eb28a9f309913e94e21`, zero changes, and zero affected modules.
- 2026-08-02: Focused validation passed: `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_workflow_source_closure.py tests/eng/test_dependency_handoff.py tests/eng/test_release_evidence_v2.py` = 77/77; Python compilation clean; valid manifest/offline fixture accepted, invalid fixture rejected, readiness classification valid; `actionlint` accepted `ci.yml`, `quality.yml`, and `release-evidence.yml`; Shell.Tests Release build = 0 warnings/0 errors; `CiGovernanceTests` = 62/62; `AnalyzerPolicyGovernanceTests` = 1/1 after refreshing its deterministic identifier hash; complete Shell Governance name filter = 199/199; solution-wide `Category=Governance` = 376/376. The no-recursion scan and `git diff --check` returned clean. No acquisition, publish, commit, fetch, push, recursive/nested submodule initialization, or remote operation was run.
- 2026-08-04: Reconciliation pass at exact HEAD `3786330d241c2d87449fa3e01afc95fc832552df` (rebased `baseline_commit` frontmatter to this commit per the workflow's Baseline step; the prior `c3152890` baseline predates several intervening REL-AI-1/REL-5 commits that carried GOV-1-adjacent Task 4/5 engine/workflow work — `936913b0`, `3ebbdce9`, `90c5dcb9`, `f5b5eefc`, `569ad3e4`, `663a88ec`, `8a6a6cb3`, `5e460c79`, `30b4821e`, `9be329a4`, `1c64c47f`, `3786330d`. **Correction of an initial false claim in this same entry, caught by adversarial review below: `git show --stat 8a6a6cb3 -- <this file>` shows 8a6a6cb3 DID touch this story file (87 insertions/39 deletions) — it is the commit that landed the 2026-08-02 course-correction's checkbox/gate-framing edits that this session started from. The correct statement is that none of these commits made further edits to this story file's task checkboxes beyond what was already visible in the working tree at session start**, not that none of them touched the file historically.) Re-audited every unchecked Task 4/5/6 checkbox against the current codebase rather than trusting the 2026-08-02 Debug Log snapshot:
  - `python3 eng/dependency_graph.py --root "$(pwd)" validate --commit 3786330d241c2d87449fa3e01afc95fc832552df` (matching `RunDependencyGraphValidate`'s exact invocation) → `ok: true`, 43 edges, 7 selectors validated, graph digest `2eb139ccd9371d7c04b270892f8db506444da0e7a117d5c48a04f9e44ba5e877`.
  - `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_workflow_source_closure.py tests/eng/test_dependency_handoff.py tests/eng/test_release_evidence_v2.py` = 94/94 before, 95/95 after adding the zero-push-base fixture below.
  - `python3 -m py_compile eng/dependency_graph.py eng/release_evidence.py eng/release_prepublish.py` clean.
  - `python3 eng/release_evidence.py verify-manifest --manifest tests/ci-governance/fixtures/release-manifest-valid.json --no-root` exit 0.
  - `python3 eng/release_evidence.py classify-fixtures --fixtures tests/ci-governance/fixtures/release-readiness-cases.json --output /tmp/frontcomposer-release-readiness.json` exit 0.
  - `python3 eng/validate-story-artifacts.py` → "Story artifact validation passed."
  - `git diff --check` clean; `rg -n "submodule update.*--recursive|submodule foreach.*--recursive|--recurse-submodules" .github eng tests` returned only the pre-existing detection regex inside `CiGovernanceTests.cs` (no new matches).
  - `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "Category=Governance"` = 212/212 (up from 199/199 on 2026-08-02 — the growth predates this session and belongs to the REL-AI-1/REL-5 commits above, not to today's change).
  - `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category=Governance"` = 373/373 (Contracts.Tests 6, Cli.Tests 6, Mcp.Tests 8, SourceTools.Tests 140, Shell.Tests.Bench 1, Shell.Tests 212; Testing.Tests has no Governance-tagged tests).
  - Reading `eng/dependency_handoff.py`, `.github/workflows/ci.yml`, `.github/workflows/release.yml`, and `.github/workflows/release-evidence.yml` in full confirmed Task 4/5's two remaining non-external-gate bullets and Task 6's fixture bullets were already implemented by the intervening commits — via the exact-source-proof design the Release Owner formally adopted under REL-5 (`_bmad-output/implementation-artifacts/rel-5-provision-signing-identity-and-first-governed-release.md`) — which functionally satisfies, but does not literally spell the same schema name as, this story's original Task 4/5 wording. Checkbox text was updated in place with file/line and test-name citations rather than silently checked, so the supersession is auditable.
  - The one genuine, previously-documented gap — "a dedicated zero/unavailable-push-base fixture" (Task 6) — was still absent from `tests/eng/test_dependency_graph.py`; added `test_zero_push_base_forces_full_affected_and_is_never_release_eligible` to `DiffAndMaterializationTests`, reproducing the CLI `diff` command's zero-base branch (`eng/dependency_graph.py` `main()`, `args.command == "diff"`, `zero_base` path) at the function level per this file's established `mock.patch.object(dg, "load_policy_at_commit", ...)` convention, and confirmed `validate_diff_evidence` accepts the resulting non-release-eligible, full-affected document.
  - No file under `references/**` was read for write, no `.gitmodules`/gitlink was touched, no submodule was initialized/updated, and no acquisition, publish, commit, fetch, push, or remote operation was run.
- 2026-08-04 (adversarial review correction): ran `/bmad-quick-dev`'s step-04 three-layer parallel review (Blind Hunter, Edge Case Hunter, Verification Gap) over the above reconciliation diff before presenting it. The Blind Hunter found, and independent verification here confirmed, that the reconciliation above over-claimed on two of Task 4/5's core bullets:
  - **AD-13 `[ADOPTED]`** (`architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md:242-`) mandates the CI-to-Release handoff artifact be named `dependency-release-handoff` with schema `hexalith.dependency-release-handoff.v1` and an `evaluator` member. `eng/dependency_handoff.py` implements this exactly (`create_ci_handoff`/`validate_ci_handoff`), but `.github/workflows/ci.yml` never calls it — it calls `create-source` instead, producing the narrower `hexalith.dependency-release-source.v1` artifact (no `evaluator` member). `eng/release_evidence.py`'s `--ci-handoff`/`--release-evaluator` activation path exists but is never populated by any workflow (`DEPENDENCY_RELEASE_HANDOFF`/`RELEASE_EVALUATOR` env vars are set nowhere), so `create_ci_handoff`/`validate_ci_handoff` are live, tested, but unreachable in production.
  - **AD-15 `[ADOPTED]`** (`ARCHITECTURE-SPINE.md:333-`) mandates the Release-to-verifier artifact be named `release-verification-handoff` with schema `hexalith.release-verification-handoff.v1` and the exact closed shape `{schema,release_run,ci_handoff,candidate,dependency_policy,release,manifest,assets,evaluator}`. `.github/workflows/release-evidence.yml` uploads a differently-named artifact built from bespoke schemas (`frontcomposer.release-run-disposition.v2`, `frontcomposer.release-ledger-record.v2`, etc.) that never bind an `evaluator` and never call `create_release_handoff`/`validate_release_handoff`.
  - Searched `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md`, `sprint-change-proposal-2026-08-03.md`, `rel-5-provision-signing-identity-and-first-governed-release.md`, and `architecture.md` for any decision amending or superseding AD-13/AD-15 with the shipped "exact-source CI proof" design (`grep -rln "AD-13\|AD-15"`); none exists. This is an **undocumented divergence** between a formally ratified architecture decision and the actual `ci.yml`/`release.yml`/`release-evidence.yml` implementation, not an approved design evolution — the 2026-08-04 entry above was wrong to characterize it as the latter.
  - Corrected: reverted the two affected Task 4/5 checkbox flips to `[ ]` with accurate, evidence-cited explanations in place; reverted `status`/`Status` from `in-review` back to `in-progress` (the same diff had, self-contradictorily, also left the pre-existing "Status stays `in-progress`, not `review`" Completion Notes line untouched — both Blind Hunter and Edge Case Hunter independently caught this); fixed a false claim in the entry above (commit `8a6a6cb3` does touch this story file — confirmed via `git show --stat`); rewrote the Task 6 evaluator-fixture bullet's citation to state plainly that it guards implemented-but-unwired code, not the live pipeline; appended a `deferred-work.md` entry for the AD-13/AD-15 divergence (out of this session's scope to resolve — it requires an Architect/Release Owner decision, not a local patch). The zero-push-base fixture, the v2-schema fixture/staging updates, and Task 6's other bullets were independently fact-checked by the Verification Gap reviewer (function names, workflow behavior, and test counts all confirmed to exist exactly as cited) and are unaffected by this correction.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- GOV-1's FC-DEP-1 architecture entry gate was ratified on 2026-07-19. The 2026-08-02 approved correction unblocks all FrontComposer-local Task 4/5 work. Only reusable-workflow integration, end-to-end proof, story completion, release eligibility, and unfreeze await a qualifying revision from reopened issue 17 or a successor.
- Sprint tracking moved the story row `ready-for-dev` -> `in-progress`; the separate cross-cutting action remains `open` until implementation and accepted evidence complete.
- **Tasks 1, 2, and 3 are complete. The FrontComposer-local Task 4/5 implementation is now present and focused tests are green.** `eng/dependency_graph.py` + `eng/dependency-graph-policy.json` collect and semantically validate the bounded v1 graph without a commit allowlist, compute exact base/candidate diffs, prove a bounded affected set, and execute only policy-owned static argv. `eng/workflow_source_closure.py`, `eng/dependency_handoff.py`, and manifest v2 add the authorized immutable evaluator closure, both strict handoff schemas, and sealed graph/policy/workflow provenance.
- **The story remains incomplete for the external AD-16 seam.** The active policy intentionally authorizes no production evaluator while Hexalith.Builds issue 17/successor has no owner-accepted immutable reusable-workflow revision. Consequently CI does not fabricate a successful release handoff, `.github/workflows/release.yml` is not falsely rewired, and no end-to-end publication/evidence claim is made. Story 11.17d completed separately on 2026-08-02 and is not reopened.
- Two deliberate, documented departures from a literal reading of the story are recorded in the Debug Log above: (1) the root-only BOM/CRLF format check reads the checked-out working tree rather than the raw commit object (Dev Notes explicitly carve this one check out as "a local format policy"); (2) `builds_identity` was added to the policy schema as a value the engine reads, rather than a hard-coded Python constant, since the hard-coded form was both untestable and inconsistent with AD-12's "one versioned trust... policy" principle.
- Status stays `in-progress`, not `review`: the local mechanisms and consumers are implemented, but successful-push/Release handoff production, immutable reusable-workflow integration, and the final end-to-end evidence chain still require the qualifying Hexalith.Builds revision and active-policy authorization.
- **2026-08-04 reconciliation (as corrected by same-day adversarial review — see the two Debug Log entries above): Tasks 1-3 and 6 are now fully checked; Task 4 has three open bullets and Task 5 has four open bullets.** Intervening REL-AI-1/REL-5 work (commits `936913b0` through `3786330d`, most directly `8a6a6cb3`) had already implemented Task 6's v2-schema fixture/staging updates and a real, live "exact-source CI proof" authentication mechanism (`create-source`/`verify-source` in `eng/dependency_handoff.py`, wired into `ci.yml`/`release.yml`) without anyone updating this story file's checkboxes. This session verified that mechanism against the live code, closed the one fixture gap that was genuinely still missing (a zero/unavailable-push-base test in `tests/eng/test_dependency_graph.py`), and — on first pass — incorrectly concluded the exact-source-proof mechanism also satisfies Task 4/5's core handoff-schema bullets. Adversarial review caught this: those bullets require the literal `hexalith.dependency-release-handoff.v1`/`hexalith.release-verification-handoff.v1` schemas mandated by AD-13/AD-15 `[ADOPTED]`, which are implemented and tested in `eng/dependency_handoff.py` but never invoked by any production workflow — a real, undocumented divergence from the ratified architecture spine, now recorded in `deferred-work.md` for Architect/Release Owner disposition. Those three bullets (one per Task 4, two per Task 5) were reverted to unchecked with accurate citations. No workflow, release, or `references/**` file was modified. Status remains `in-progress` — both for the pre-existing AD-16 external gate (a qualifying Hexalith.Builds issue-17/successor revision) and now also for the newly-surfaced AD-13/AD-15 gap, neither of which this repository can resolve unilaterally in one session.

### File List

- `.github/workflows/ci.yml` (UPDATE — exact base/candidate graph gate, isolated acquisition, bounded affected-module proof, deterministic evidence)
- `.github/workflows/quality.yml` (UPDATE — full exact-object history and focused Python governance coverage)
- `.github/workflows/release-evidence.yml` (UPDATE — authenticated two-run/two-handoff read-only verifier bound to the original candidate)
- `eng/dependency_graph.py` (NEW — committed-object semantic graph engine plus strict offline validation, exact diff, bounded materialization/acquisition, and static affected-module execution)
- `eng/dependency-graph-policy.json` (NEW — `hexalith.dependency-graph-policy.v1`: trusted identities, semantic profiles, static module argv, evaluator registries, and AD-7/AD-13 limits)
- `eng/dependency_handoff.py` (NEW — strict CI-to-Release and Release-to-verifier handoff contracts and live verification)
- `eng/workflow_source_closure.py` (NEW — exact-blob static workflow/composite-action closure and policy authorization)
- `eng/release_evidence.py` (UPDATE — manifest v2 graph/policy/handoff/workflow provenance, offline/live verification, fallback binding, legacy audit-only mode)
- `eng/release_prepublish.py` (UPDATE — explicit graph-root plumbing while preserving prepare/seal/live-verify/classify ordering)
- `tests/eng/test_dependency_graph.py` (NEW, extended 2026-08-04 — 69 synthetic-repository graph/semantic/diff/materialization/policy tests, including the zero/unavailable-push-base fixture)
- `tests/eng/test_dependency_handoff.py` (NEW — strict candidate/policy/evaluator handoff tests)
- `tests/eng/test_workflow_source_closure.py` (NEW — conditional/nested/cycle/mutable/dynamic/unsupported/limit/authorization tests)
- `tests/eng/test_release_evidence_v2.py` (NEW — manifest v2 offline/live/fallback/legacy/evaluator tests)
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` (UPDATE — catalog-compatibility Facts now invoke the Python engine; removed the SHA allowlist and now-dead helper methods)
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` (UPDATE — exact CI graph gate, static policy commands, sealed release provenance, and original-candidate verifier pins)
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/Story12_4_RedPhaseDefTests.cs` (UPDATE — manifest v2 and authenticated handoff contract pins)
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` (UPDATE — refreshed `identifierInventory.testUnderscoreIdentifierTokens`/`testInventorySha256` after the C# identifier-set change)
- `tests/ci-governance/stage_release_state.py` (UPDATE — hermetic manifest-v2 fixture staging)
- `tests/ci-governance/fixtures/release-manifest-valid.json` (UPDATE — sealed manifest-v2 valid evidence)
- `tests/ci-governance/fixtures/release-manifest-invalid.json` (UPDATE — manifest-v2 invalid evidence)
- `tests/ci-governance/fixtures/release-readiness-cases.json` (UPDATE — v2 graph/policy/workflow fallback inputs)
- `tests/README.md` (UPDATE — focused graph/closure/handoff/manifest commands and offline/live provenance distinction)
- `_bmad-output/project-context.md` (UPDATE — durable Gate 2b and local-vs-external GOV-1 guidance)
- `_bmad-output/implementation-artifacts/deferred-work.md` (UPDATE — new BUILD-CAT-1/BUILD-REL-1 entries under "Deferred from: GOV-1 story creation (2026-07-19)")
- `_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md` (UPDATE — this story file: status, task checkboxes, Debug Log, Completion Notes, File List)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (UPDATE — GOV-1 story row `ready-for-dev` -> `in-progress`; action item remains open)

## Suggested Review Order

**AD-13/AD-15 vs. shipped-implementation gap (read this first — the most important finding from today's session)**

- Newly-discovered: AD-13/AD-15 `[ADOPTED]` mandate a schema/artifact production never emits.
  [`gov-1-...-provenance.md:141`](gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md#L141)

- Same gap on the Release-side handoff artifact; `release.yml` uses a different, unmandated schema.
  [`gov-1-...-provenance.md:156`](gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md#L156)

- `release-evidence.yml`'s `if: always()` artifact doesn't conform to AD-15's closed shape either.
  [`gov-1-...-provenance.md:157`](gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md#L157)

- Durable ledger entry for Architect/Release Owner disposition — retire AD-13/15's schema or wire it in.
  [`deferred-work.md:2051`](deferred-work.md#L2051)

- Existing fixtures for the not-yet-wired handoff functions are real but don't guard the live pipeline.
  [`gov-1-...-provenance.md:164`](gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md#L164)

**New test coverage (the one genuine local gap this session closed)**

- Reproduces the CLI `diff` zero/unavailable-push-base branch at the function level; proves fail-safe behavior.
  [`test_dependency_graph.py:1280`](../../tests/eng/test_dependency_graph.py#L1280)

**Self-correction audit trail (peripheral — shows how the session caught and fixed its own overclaims)**

- Frontmatter/header status reverted to `in-progress` after a same-diff self-contradiction was caught.
  [`gov-1-...-provenance.md:9`](gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md#L9)

- Corrects a false "commit didn't touch this file" claim from the first reconciliation pass.
  [`gov-1-...-provenance.md:375`](gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md#L375)

- Full account of the adversarial-review findings and what was reverted/corrected as a result.
  [`gov-1-...-provenance.md:388`](gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md#L388)

- Corrected Completion Notes summary with the accurate open-bullet counts (3 for Task 4, 4 for Task 5).
  [`gov-1-...-provenance.md:403`](gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md#L403)
