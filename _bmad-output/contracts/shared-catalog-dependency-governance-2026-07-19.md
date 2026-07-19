---
id: FC-DEP-1
title: Shared Catalog Compatibility and Dependency Provenance
status: approved
date: 2026-07-19
amended: 2026-07-19
amendmentStatus: approved
previousStatus: approved
previousApprovedBy: Administrator
requiredApproval: Architect + Release Owner
approvedBy: Administrator (Architect + Release Owner)
ratified: 2026-07-19
owners:
  - Product Owner
  - Architect
  - Release Owner
implementationStory: GOV-1
upstreamFollowUp: BUILD-CAT-1
upstreamReleaseFollowUp: BUILD-REL-1 issue 17 (accepted immutable revision pending)
architectureSpine: _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
---

# FC-DEP-1: Shared Catalog Compatibility and Dependency Provenance

> **Approved amendment:** Administrator ratified the complete decision below as Architect and Release
> Owner on 2026-07-19. It supersedes the former unbounded complete-reachable interpretation of v1.

## Context

FrontComposer Governance tests hard-coded selected historical `Hexalith.Builds` commits. Legitimate
root or nested gitlink advances therefore failed until the expected SHA constants were mechanically
updated, even when every required package and build contract remained compatible. At the same time,
release evidence recorded the FrontComposer commit but not a defined, reproducible gitlink graph.

Compatibility and provenance are different concerns. A commit SHA precisely identifies inputs but
does not, by itself, state whether a catalog satisfies a consumer contract.

## Decision

1. **`[ADOPTED]` V1 boundary.** `hexalith.dependency-graph.v1` contains every gitlink at the
   explicit FrontComposer root commit (depth 1) and every gitlink in each exact root-selected
   repository commit (depth 2). Edges below depth 2 are outside v1. The creation-time 8 + 32 = 40
   census is evidence, not a fixed count. Complete historical traversal requires a new schema and
   separate approval.
2. Compatibility is validated from the shared catalog selected by each Builds gitlink inside that v1
   boundary. Governance loads `Props/Directory.Packages.props` from that exact commit and validates
   the applicable semantic package/import/marker contract.
3. Product Governance tests contain no expected 40-hex submodule SHA allowlist. A compatible catalog
   at a different commit passes; an incompatible catalog fails with owner path, actual commit, and
   semantic mismatch.
4. Root and in-boundary nested pointer changes produce a deterministic dependency-graph diff using the
   exact PR event-base/merge-revision or push before/current revision model in decision 11, and run the
   affected module's supported standalone restore/build gate.
5. The sealed release manifest records and verifies the complete defined v1 graph. Each edge includes
   `owner_repository`, `owner_commit`, `path`, `repository`, `commit`, and `depth`. Builds edges also
   include raw-byte `catalog_sha256` and nullable `catalog_contract_version`.
6. **`[ADOPTED]` Closed-world acquisition and resolution.** Repository resolution includes the
   explicit FrontComposer root identity and identities from its root `.gitmodules`. Graph collection
   reads exact committed objects, records edges before object-read/catalog-validation deduplication,
   rejects unknown/unsafe identities,
   and never clones candidate URLs, recursively initializes nested submodules, moves working-tree HEADs,
   or executes candidate-supplied commands. The collector is offline/object-only; CI may fetch exact
   objects from those approved remotes into isolated temporary bare stores. Missing objects after that
   bounded acquisition fail closed.
7. **`[ADOPTED]` Canonical graph.** Edges sort ordinally by
   `(depth, owner_repository, owner_commit, path, repository, commit)`. Strict
   lowercase 40-hex/64-hex values and normalized ASCII POSIX paths apply. `graph_digest` is SHA-256 over
   UTF-8 compact JSON of `{schema, root, edge_count, edges}` with `edge_count == len(edges)`,
   `ensure_ascii=true`, `allow_nan=false`, lexicographically sorted object keys, comma/colon separators,
   no BOM/trailing newline, and that edge order. The envelope, root, and both edge kinds have closed
   member sets; verification rejects missing/unknown members, duplicate JSON member names, boolean
   integers, and depths other than integer 1/2. The outer manifest seal binds the complete graph object.
   This is project canonicalization v1, not RFC 8785.
8. **`[ADOPTED]` Resource ceilings.** V1 fails closed above 4,096 edges, 1 MiB for any committed
   `.gitmodules` blob, 4 MiB for any catalog blob, or 64 MiB raw `ls-tree` output per owner commit.
   Ceilings are inclusive and measured before decoding/parsing; boundary fixtures are mandatory.
   Within depths 1-2, missing objects/mappings/catalogs, duplicates, malformed input, unknown identities,
   and unavailable commits fail closed; deeper edges are excluded by definition.
9. Hexalith.Builds owns BUILD-CAT-1: introduce a semantic catalog-contract version and canonicalization
   rules. Until supported gitlinks migrate, FrontComposer validates semantic contents directly and
   records fingerprints only as provenance. Making the version marker mandatory requires separate
   approval.
10. **`[ADOPTED]` One policy owner.** A versioned `eng/dependency-graph-policy.json` owns the
    trusted identity/path set, semantic owner profiles, affected-module argv, evaluator authorizations,
    and v1 limits. Committed
    base/candidate `.gitmodules` are untrusted graph data. PR evaluation uses the exact base-commit
    policy and push evaluation uses the exact non-zero before-commit policy for both graphs; evidence
    records its commit and raw SHA-256. A candidate policy change activates only as a later change's
    base policy. The initial bootstrap requires an unchanged dependency graph, frozen publication, and
    Architect + Release Owner approval of the exact policy digest, enforced by the Release Owner-
    controlled `HEXALITH_DEPENDENCY_POLICY_BOOTSTRAP_SHA256` repository variable. Once the base contains
    policy, bootstrap is permanently unavailable. A zero/unavailable push-before may emit diagnostic/
    full-affected evidence, but the gate fails and is not release-eligible.
    Python owns semantic catalog
    evaluation; C# Governance consumes the machine result rather than duplicating policy. The policy is
    bound into release-definition and fallback-invalidation fingerprints. CI, Release, and post-release
    evaluator closures must project exactly one policy authorization; each authorization fixes the local
    caller blob hash, immutable reusable workflow coordinates/blob, static transitive action coordinates/
    blobs, and canonical closure digest. Candidate changes pre-authorize a future closure and switch only
    from a later base; a self-recorded or sealed but unapproved closure fails closed.
11. **`[ADOPTED]` Git format and CI revisions.** V1 accepts Git SHA-1 object format only. Pull
    requests use `github.event.pull_request.base.sha` plus `github.sha` (the primary-CI merge revision)
    and require the computed merge-base to equal that event base; a mismatch fails closed. Pushes use
    `github.event.before` plus `github.sha`, with a zero/unavailable base taking the full-affected
    fail-closed path. Collection and builds use the same candidate revision.
12. **`[ADOPTED]` Closed profile/build registries.** Every Builds-selector owner maps to exactly one
    named semantic profile and every governed target maps to an exact standalone build argv or explicit
    evidence-only disposition. The seed registry covers FrontComposer and all eight root-declared
    identities; AI.Tools is evidence-only because its seed commit has no solution/build surface. There
    is no implicit default. The focused architecture spine contains the exact identity/profile and
    identity/solution/catalog-binding matrices; missing or candidate-added entries fail closed under
    decision 10. Build rows run exact static Release/NuGet restore/build argv in an isolated exact-commit
    checkout; edge-bound Builds regular-file contract trees are bounded-materialized from the selected
    candidate commit, their catalog re-hashed against the graph, and never initialized as nested
    repositories. Materialization rejects unsafe modes/paths and is capped at 16,384 files, 16 MiB per
    blob, and 256 MiB total.
    Depth-1 additions/changes build the candidate target and removals build FrontComposer; those changes
    subsume their descendant depth-2 diff. Only remaining depth-2 changes build a candidate owner, with
    an absent owner collapsing to FrontComposer. Every module is scheduled at most once.
13. The release caller passes `github.event.workflow_run.head_sha` as a required exact commit; the
    reusable workflow checks out and propagates that commit through preparation, sealing, verification,
    fallback, and publication. Its Hexalith.Builds workflow reference is an active-policy-authorized immutable 40-hex
    commit and is sealed with the caller workflow hash and CI-selected policy coordinates. The caller
    passes the triggering CI run ID and fetches the single versioned dependency-release handoff through
    the read-only Actions API only after repository/workflow/event/branch/conclusion/run/head metadata
    match and the recorded candidate equals the event head SHA. Actual caller/reusable workflow refs and
    SHAs must match sealed coordinates, and release reloads/hashes/parses the policy blob at its recorded
    FrontComposer commit. Primary CI and release reusable workflows are 40-hex pinned and their
    transitive sources sealed. Every transitive non-local action is 40-hex pinned; Builds local actions
    come from an exact reusable-workflow SHA checkout, never `@main`. The handoff evaluator digest is
    canonical SHA-256 over its exact caller/reusable/action sources. Manifest CI provenance must equal
    those sources, project the authenticated run, and bind the raw handoff JSON SHA-256; offline
    verification recomputes both CI-only and combined CI/release definition digests. The current mutable
    CI/release `@main` calls and missing release exact-ref input are non-conforming, so REL-4 remains
    frozen until this seam and its tests exist.
    The action list is a deterministic static closure, independent of conditions, that recursively reads
    exact caller/reusable/composite metadata blobs. Dynamic/mutable references, Docker actions,
    ambiguous/unsupported metadata syntax, cycles, or the AD-13 closure ceilings fail closed; action
    evidence includes raw metadata blob SHA-256.
14. **`[ADOPTED]` One-way manifest migration.** GOV-1 introduces required
    top-level `manifest_schema: hexalith.release-evidence.v2`, complete graph, closed policy coordinates,
    and closed caller/reusable/CI/action workflow provenance. The outer seal covers all top-level members
    except `seal`; the fallback digest covers its existing definition/package-set inputs plus graph
    digest, policy SHA-256, and the canonical trusted workflow-definition digest.
    Legacy manifests are audit-only and non-publishable, cannot satisfy fallback, and are never upgraded
    or resealed in place. Historical ledger bytes remain unchanged; current fixtures migrate atomically.
15. **`[ADOPTED]` Release-to-verifier handoff.** Every governed Release attempt uploads under
    `if: always()` one versioned verification handoff authenticated by Release run ID/attempt. It binds
    the original authenticated CI run ID/attempt/raw handoff hash, exact active-policy projection,
    candidate, conclusion, version/tag/GitHub Release identity, manifest path/hash/seal, exact asset
    name/hash/size rows, and authorized Release evaluator. The post-release verifier re-downloads and
    authenticates both handoffs and requires their candidate/policy projection to agree even when
    manifest creation failed. It uses
    this handoff and sealed manifest as its only candidate authority, never the second-hop
    `workflow_run.head_sha`/default-branch SHA. It authenticates its own policy-authorized static closure,
    verifies published bytes or records failure/partial incident state, and cannot green-no-op.
16. **`[ADOPTED]` External workflow completion gate.** Hexalith.Builds issue 17 / BUILD-REL-1 must
    deliver the exact CI/release inputs, outputs, runtime identity checks, static closure, exact-candidate
    and both handoff contracts at an owner-accepted immutable 40-hex revision. The accepted revision is
    currently pending. FrontComposer may proceed with local graph/policy work, but GOV-1 Tasks 4/5,
    story completion, release eligibility, and REL-4 unfreeze remain blocked until that revision and
    workflow/action blobs are recorded in the active policy. No local contingency is authorized without
    a new dated Architect + Release Owner decision.

## Consequences

- Pointer advances stop causing false-red compatibility failures solely because their SHA changed.
- Governance broadens from a historical subset to every selected Builds catalog in the complete
  defined v1 graph.
- Release evidence becomes reproducible from explicit dependency identities rather than relying on
  implicit Git checkout state.
- The release manifest schema, producer, verifier, fixtures, fallback invalidation, and
  post-publication verifier must change atomically.
- CI gains targeted graph-diff and affected-module cost only when pointers change.
- Trust/profile/command expansion takes two changes: land reviewed inactive policy, then use it from a
  later base revision.
- Publication stays frozen until release is wired to the exact CI-tested commit and immutable reusable
  workflow provenance.
- Literal hashes prove identity only; the immutable base/before policy is the independent authorization
  root for CI, Release, and post-release static evaluator closures.
- Post-release verification remains bound to the original CI candidate across both workflow hops and
  records failed/partial attempts instead of treating an absent default-branch tag as a no-op.
- BUILD-REL-1 issue 17's accepted immutable revision is a GOV-1 completion/unfreeze prerequisite.
- Deeper historical back-references are deliberately excluded from v1; a transitive schema must first
  resolve legacy identities, traversal budgets, unresolved-edge policy, and migration fixtures.
- BUILD-CAT-1 is external coordination and does not authorize editing `references/Hexalith.Builds`
  from the FrontComposer repository.

## Rejected Alternatives

- Keep exact SHA constants: conflates identity with compatibility and requires mechanical churn.
- Remove submodule governance: fails closed neither on incompatible catalogs nor unreviewed drift.
- Use an exact fingerprint allowlist: replaces one identity allowlist with another.
- Roll back current pointers: restores historical identity without proving compatibility.
- Traverse all historical back-references in v1: produces non-reconciled censuses and unresolved legacy
  identities without adding compatibility value to the selected root/direct module build graph.

## Verification

- A different compatible Builds commit passes semantic Governance.
- A catalog with a missing or wrong required value fails with actionable edge diagnostics.
- A pointer change emits the graph diff and runs the affected-module gate.
- Manifest verification fails for missing, duplicate, malformed, over-limit, unresolved in-boundary,
  out-of-order, or drifted graph evidence.
- Duplicate JSON member names and unknown schema members fail before digest acceptance.
- A PR that changes policy and relies on that change for its candidate graph fails; a later PR may use
  the landed policy revision.
- Release fails unless checkout, evidence, and publication all use the triggering successful CI head SHA
  and sealed immutable reusable-workflow identity.
- Legacy manifests are diagnosable but never publishable or fallback-eligible.
- Pre- and post-publication verification bind the same sealed graph.
- No recursive submodule initialization command is introduced.
