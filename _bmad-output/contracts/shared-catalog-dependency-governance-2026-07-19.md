---
id: FC-DEP-1
title: Shared Catalog Compatibility and Dependency Provenance
status: approved-base-with-open-adoption-gates
date: 2026-07-19
amended: 2026-09-09
amendmentStatus: architecture-adopted-product-and-release-acceptance-pending
previousStatus: approved
previousApprovedBy: Administrator
requiredApproval: Architect + Release Owner
approvedBy: Administrator (Architect + Release Owner)
ratified: 2026-07-19
latestArchitectureAdoption: Administrator (2026-09-09)
latestRequiredAcceptance: Product Owner + Release Owner
owners:
  - Product Owner
  - Architect
  - Release Owner
implementationStory: GOV-1
upstreamFollowUp: BUILD-CAT-1
upstreamReleaseFollowUp: BUILD-REL-1 issue 17 (owner-accepted immutable revision a8a50859 recorded 2026-08-08; pins advance through the spine AD-16 lineage)
architectureSpine: _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
---

# FC-DEP-1: Shared Catalog Compatibility and Dependency Provenance

> **Approved amendment:** Administrator ratified the complete decision below as Architect and Release
> Owner on 2026-07-19. It supersedes the former unbounded complete-reachable interpretation of v1.
> The finalized 2026-09-09 GOV-1 spine is now authoritative for AD-1 through AD-19, whose IDs remain
> stable. Administrator adopted that architecture direction; Product Owner and Release Owner acceptance
> of the new publication boundary and halt posture remains open and is not inferred from the 2026-07-19
> approval.

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
    base policy. The one-time v1 bootstrap was consumed when the first policy landed on 2026-07-19; base-policy
    absence is permanently fail-closed and no bootstrap mode is reachable. A zero/unavailable push-before may emit diagnostic/
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
    is no implicit default. The active policy `eng/dependency-graph-policy.json` contains the exact identity/profile and
    identity/solution/catalog-binding matrices; the spine defines their closed schema and coverage
    invariants only. Missing or candidate-added entries fail closed under decision 10. Build rows run exact static Release/NuGet restore/build argv in an isolated exact-commit
    checkout; edge-bound Builds regular-file contract trees are bounded-materialized from the selected
    candidate commit, their catalog re-hashed against the graph, and never initialized as nested
    repositories. Materialization rejects unsafe modes/paths and is capped at 16,384 files, 16 MiB per
    blob, and 256 MiB total.
    Depth-1 additions/changes build the candidate target and removals build FrontComposer; those changes
    subsume their descendant depth-2 diff. Only remaining depth-2 changes build a candidate owner, with
    an absent owner collapsing to FrontComposer. Every module is scheduled at most once.
13. **`[ADOPTED AD-13]` Exact candidate and evaluator authority.** Release is operator
    `workflow_dispatch` on `refs/heads/main`. The dispatched SHA equals the live `main` ref and is the
    head of exactly one completed successful push run of both `ci.yml` and `quality.yml`, selected
    through read-only Actions APIs. The run/attempt-named CI handoff is independently authenticated and
    its candidate is the sole release authority. The active-policy CI and Release evaluator closures use
    literal immutable coordinates; mutable references, self-authorized rows, ambiguous closure sources,
    or a candidate/default-branch substitution fail closed. Only the AD-19 secretless builder may
    execute the authenticated candidate; the protected publisher receives it solely as authenticated
    publication-candidate data.
14. **`[ADOPTED AD-14]` One-way manifest migration.** The lineage is
    `hexalith.release-evidence.v1` (legacy) → `v2` → `v3` → `v4`. Preparation, sealing,
    classification, fallback, and publication accept only v4. V2/v3 are byte-preserved audit evidence,
    never produced, resealed, upgraded, or made fallback-eligible for a new publication. The v4 seal
    covers every top-level member except `seal`, including the complete graph, policy, selected quality
    run, attestation-or-fallback projection, and workflow provenance.
15. **`[ADOPTED AD-15]` Release-to-verifier handoff, attempt truth, and durable evidence.** Every
    authenticated Release run uploads exactly one `hexalith.release-verification-handoff.v3` artifact
    under `if: always()`, or the narrowly valid deferred sentinel when no CI handoff was authenticated.
    It preserves the original candidate, quality run, CI handoff, policy, publication-candidate
    coordinates, release state, denial reason, final manifest v4, attestation/fallback, authorized asset
    inventory, and Release evaluator. The post-release verifier independently authenticates the CI and
    Release artifacts and its own pinned `post_release` closure; it never substitutes later branch code
    or candidate identity. `frontcomposer.release-ledger-record.v2` assigns exactly one fail-closed
    disposition per attempt, appends rather than replaces observations, and never permits a later green
    observation or owner sign-off to erase an incident. Incomplete publication is preserved through the
    separately authorized incident-recovery path and immutable reserved-namespace evidence Release.
16. **`[ADOPTED AD-16]` Owner-accepted Builds lineage.** The 2026-08-08 owner-accepted revision
    `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` roots the accepted BUILD-REL-1 lineage; it is a
    predecessor, not evidence that the split reusable exists. CI and Release pins may differ but must be
    in that lineage and present in the active policy through delayed activation. A new immutable Builds
    revision implementing AD-19 must be accepted into the lineage before FrontComposer selects it. No
    integration, completion claim, release eligibility, or local contingency exists outside this rule.
17. **`[ADOPTED AD-17]` Manifest v4 provenance.** Manifest v4 binds the complete dependency graph,
    active policy, authenticated CI handoff hash and run, selected quality run, exact Release
    caller/reusable/builds-execution coordinate, and byte-identical attestation-or-fallback projection.
    Its definition digest binds CI and Release evaluator provenance while keeping quality as denial-only
    evidence and actor/approval state in the ledger.
18. **`[ADOPTED AD-18]` Least privilege and run-bound recovery.** Candidate-controlled code, graph
    gates, and the builder have no environment, publication secret, OIDC/attestation permission, or
    write scope. Only candidate-free pinned owner code in the protected publisher can hold product
    publication authority. The fallback is valid only for an explicitly unsupported attestation
    capability and one authenticated Release run/attempt; expiry, retry, candidate, CI, policy,
    workflow, graph, or package-set drift invalidates it. Incident recovery is a separate protected,
    candidate-free, reserved-namespace evidence writer with no product-publication credential.
19. **`[ADOPTED AD-19]` Split publication and implementation gate.** `split-publication-v1` contains
    exactly one secretless `build-publication-candidate` role and one protected candidate-free
    `publish-publication-candidate` role in the selected immutable reusable. The builder produces the
    closed `hexalith.publication-candidate.v1` archive and can only deny early. The publisher
    authenticates every byte as data, mints/verifies attestation or validates the run-bound fallback,
    prepares and seals manifest v4, completes offline/live classification, and alone may publish. The
    GOV-1 split implementation gate is the exact canonical conformance packet, authenticated review,
    and approval-projection protocol in the spine; all required register rows must be closed and any
    stale/mixed/unavailable evidence or unprotected history fails the gate.

## Unresolved Owner Decisions

- Product Owner and Release Owner acceptance of AD-19, the production-release halt, and the current
  no-exception posture remains open under PRD D-16/G-8.
- Release Owner or repository-administrator action to set `HEXALITH_RELEASE_PUBLISH_ENABLED` to literal
  `false` and preserve an authenticated API observation remains open. The value is deny-only.
- Hexalith.Builds owner and Release Owner acceptance of the immutable split-reusable successor entering
  AD-16 lineage remains open.
- Product Owner and Release Owner approval of a measurable incident acknowledgement/containment target
  and the `docs/release-incident-response.md` runbook remains open.
- Release Owner confirmation of the no-bypass protected-main ruleset required by the conformance and
  ledger approval protocols remains open.
- No bounded risk exception is authorized. Any future exception requires a separate dated Product
  Owner + Release Owner + Architect decision with scope, expiry, evidence, compensating controls, and a
  revocation trigger.

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
- Production publication remains halted until the split reusable, owner decisions, incident runbook,
  and GOV-1 split implementation gate close. `HEXALITH_RELEASE_PUBLISH_ENABLED=false` is the required
  deny-only emergency stop during the halt and can never authorize publication.
- Literal hashes prove identity only; the immutable base/before policy is the independent authorization
  root for CI, Release, and post-release static evaluator closures.
- Post-release verification remains bound to the original CI candidate across both workflow hops and
  records failed/partial attempts instead of treating an absent default-branch tag as a no-op.
- Candidate construction and protected publication are distinct roles; builder output cannot become a
  final seal, classification, authorization, or executable publisher input.
- New publications use release-verification handoff v3 and manifest v4. Run-bound fallback evidence,
  append-only attempt observations, and immutable incident preservation remain part of the contract.
- BUILD-REL-1 issue 17's owner-accepted revision `a8a50859` roots the AD-16 lineage; every CI/release
  pin must lie in it.
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
- Release fails unless the exact split topology keeps candidate execution secretless and makes the
  candidate-free protected publisher the sole final classifier and publication actor.
- Legacy manifests are diagnosable but never publishable, resealable, or fallback-eligible.
- Fallback authorization fails on any run-attempt, candidate, policy, graph, workflow, package-set, or
  expiry mismatch and cannot carry across a retry.
- Every attempt maps to one closed ledger disposition; incident observations are append-only and
  incomplete publication is durably preserved before retry.
- The GOV-1 split implementation gate fails on an open nonconformance row, stale spine hash, mixed or
  unavailable evidence, unprotected/direct-push history, or missing authenticated approval projection.
- Pre- and post-publication verification bind the same sealed graph.
- No recursive submodule initialization command is introduced.
