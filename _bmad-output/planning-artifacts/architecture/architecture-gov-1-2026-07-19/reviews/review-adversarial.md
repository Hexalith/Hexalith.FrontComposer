# Adversarial Divergence Review — GOV-1 Dependency Provenance

**Target:** `ARCHITECTURE-SPINE.md`
**Lens:** Independently implemented units that each claim conformance
**Date:** 2026-07-19
**Verdict:** **BLOCKED — the spine does not yet guarantee convergent or safe implementations.** The
bounded paradigm is sound, but two source-of-truth conflicts and six underspecified cross-unit seams
still allow materially different graph, CI, semantic, and release behavior.

## Critical

### C-1 — The graph boundary is precise but still non-authoritative

**Evidence:** AD-1 is tagged `[ASSUMPTION]` and selects depth 1–2
(`ARCHITECTURE-SPINE.md:46-53`), while Deferred explicitly leaves reconciliation with approved
requirements until later (`:241-242`). The story and FC-DEP-1 remain load-bearing sources named by
the spine, and they still contain complete-reachable language.

**Constructed divergence:**

- Collector A treats the spine as the latest architecture and emits the bounded 40-edge creation
  census.
- Collector B treats approved FC-DEP-1/epic acceptance text as authoritative and recursively walks
  every reachable `(repository, commit)` pair.

Both can defend conformance to a named source, but produce incompatible graphs, digests, CI diffs,
and release manifests for the same root commit. This is the exact divergence GOV-1's entry gate was
created to prevent.

**Disposition:** **Discuss/block, then autofix.** Obtain explicit Architect and Release Owner
ratification, mark AD-1 adopted, and amend or supersede FC-DEP-1 plus every normative
complete-reachable acceptance statement before this spine becomes final. If ratification is not
obtained, keep development blocked; Deferred is not a safe home for a conflicting normative source.

### C-2 — The policy has no immutable trust coordinate

**Evidence:** AD-3 and AD-12 require a FrontComposer-owned versioned policy
(`:63-71`, `:167-175`) but do not say *which commit's policy bytes* govern candidate collection,
base collection, command execution, release preparation, or historical live verification. AD-10's
"approved remotes" inherits the same ambiguity (`:149-157`). The policy combines data trust,
semantic profiles, and executable command argv in one file.

**Constructed divergence:**

- Unit A reads `eng/dependency-graph-policy.json` from the candidate merge revision. A pull request can
  therefore add a repository identity or change an argv entry before CI validates or executes it.
- Unit B reads the runner checkout's policy. Its result depends on whichever workflow revision happens
  to execute the job and can no longer reproduce a sealed historical graph.
- Unit C reads the base policy for PRs and the sealed-root policy for release verification. It rejects
  legitimate policy-plus-gitlink changes that A accepts.

All three satisfy "FrontComposer-owned versioned policy" as currently written. Only one can be the
intended trust model.

**Disposition:** **Discuss/block, then autofix.** Define an immutable policy coordinate for every
mode and separate non-executable graph/semantic policy from executable module-command policy if they
have different trust authorities. At minimum, state the exact object/revision used for base,
candidate, trusted-main release, and historical live verification; seal its schema and raw-byte hash;
define how an intentional policy change is approved; and ensure no untrusted candidate revision can
authorize command execution or a new remote.

## High

### H-1 — Pull-request base selection has two valid readings

**Evidence:** AD-8 says to use `github.event.pull_request.base.sha` as the base input but only
"record" its computed merge-base (`:122-129`). GOV-1 AC4 requires comparison of the explicit
merge-base graph with the candidate graph. The memlog also contains both a merge-base decision and a
later base-SHA formulation.

**Constructed divergence:** Unit A diffs `base.sha..github.sha`; Unit B computes
`merge-base(base.sha, github.sha)` and diffs `merge-base..github.sha`. Reruns, rebases, merge queues,
or a base branch that advances can make their evidence and affected-module sets differ.

**Disposition:** **Autofix.** Name exactly one `base_commit` algorithm and one `candidate_commit`
algorithm per GitHub event, define whether a missing/non-ancestor merge-base is full-affected or a hard
failure, and require those exact SHAs in graph evidence and every exact-commit build.

### H-2 — The supported-module build matrix is incorrectly deferred

**Evidence:** AD-8 makes affected builds release-blocking but says only "a static FrontComposer-owned
command map" (`:130-133`). AD-12 delegates it to a future policy (`:169-174`), and Deferred leaves the
actual module inventory and argv to implementation (`:237-238`).

**Constructed divergence:** Unit A supports all eight root modules; Unit B supports only modules that
currently have a known `.slnx`; Unit C silently maps an unsupported changed module to the FrontComposer
root build. Each runs every *supported* affected module once, yet their safety proof differs.

**Disposition:** **Autofix.** Move the v1 identity-to-build-target mapping and unsupported-module
behavior out of Deferred. The spine need not list every argv token, but it must require a complete
seeded inventory for all permitted depth-1 identities, exact candidate-commit materialization,
Release/NuGet mode, and fail-closed handling when an affected identity lacks a gate.

### H-3 — Semantic profile selection and equivalence are not a contract

**Evidence:** AD-6 requires an "applicable" package/import/marker contract and permits evaluation for
every selector "or equivalent owner contract profile" (`:97-108`). AD-12 says profiles live in JSON
but defines neither their schema nor the key by which an owner selects one (`:167-175`).

**Constructed divergence:** For two owners selecting the same Builds commit, Unit A evaluates the
union of their required packages/import rules, Unit B evaluates once using the first owner's profile,
and Unit C evaluates one catalog-global profile. Cache behavior can therefore change semantic truth,
not merely performance. They can also disagree over XML namespace handling, duplicate/conditional
items, version comparison, and optional-marker extraction.

**Disposition:** **Autofix.** Define a versioned semantic-profile schema, the exact selector key,
profile-equivalence key, XML comparison semantics, aggregation rule, and diagnostic contract. State
that catalog bytes may be loaded/hashed once but every distinct required profile must be evaluated;
cache keys must include the profile version/identity.

### H-4 — Graph JSON is deterministic only for the happy-path producer, not a closed verifier schema

**Evidence:** AD-4 and AD-5 define required fields and serialization (`:73-95`) but do not state
whether object members are exact or extensible, whether duplicate JSON names are rejected before
normal parsing, the allowed type/range for `catalog_contract_version`, or whether Builds-only fields
are forbidden on other edges. "Contains" and "additionally contain" admit both strict and permissive
readers.

**Constructed divergence:** Verifier A rejects unknown fields and duplicate object names. Verifier B
accepts extensions and uses last-name-wins parsing. Verifier C preserves unknown fields in digest
material while Verifier D discards them before recomputing. The same hostile manifest can pass one and
fail another, or yield different graph digests.

**Disposition:** **Autofix.** Publish an exact v1 JSON schema and validation order: duplicate-name
rejection at decode, exact allowed/required members per object/edge kind, scalar types (explicitly
excluding booleans as integers), marker grammar and bounds, extension policy, and whether digest
material is the validated original object or a normalized projection. Pin hostile golden fixtures,
not only one successful digest.

### H-5 — Affected-module selection is undefined for compound removals and exact materialization

**Evidence:** AD-8 maps each edge operation independently (`:126-133`) but does not define precedence
when a removed depth-1 module also produces removed depth-2 edges. It also says to build "the candidate
target/owner" without specifying how the exact target commit and its selected Builds catalog are
materialized. AD-10 covers acquisition, not the build workspace contract.

**Constructed divergence:** Removing a root module causes Unit A to build FrontComposer only; Unit B
also attempts to build every now-absent candidate owner from removed depth-2 edges; Unit C builds the
removed base module. On a changed module, one unit builds the ambient initialized submodule while
another archives the exact candidate gitlink and injects the selected catalog bytes.

**Disposition:** **Autofix.** Specify graph-change collapse/precedence, behavior for identities absent
from the candidate graph, exact module and Builds object coordinates, workspace construction, and the
proof command invariant. Affected selection should be a pure deterministic function with golden
compound-change fixtures.

### H-6 — Manifest integration and fallback migration are behavior, not plumbing

**Evidence:** AD-5 defines a graph envelope but not its exact location/name inside the existing
manifest (`:84-95`). AD-9 says the graph digest invalidates fallback approvals and rollout is atomic
(`:135-147`), but does not define the new manifest/helper schema version, backward-compatibility rule,
or exact input shape for the existing fallback digest.

**Constructed divergence:** Unit A adds top-level `dependency_graph` and folds
`graph_digest` into the existing fallback definition map. Unit B adds
`provenance.dependency_graph`, relies on the policy file fingerprint to invalidate fallbacks, and
continues accepting legacy manifests offline. Unit C rejects every pre-v2 manifest immediately. Their
seals, fixtures, approval invalidation, and historical verification are mutually incompatible.

**Disposition:** **Autofix.** Fix the exact manifest member path, manifest/helper version transition,
legacy acceptance window (normally fail closed for governed release), seal material, and the exact
fallback digest key/value contribution. State whether historical post-publication verification uses
the helper/policy sealed with that manifest or the current verifier under an explicit compatibility
matrix.

## Medium

### M-1 — The machine-result and failure-ownership seam is prose-only

**Evidence:** AD-6 makes C# Governance consume a Python "machine result" (`:106-108`), while the only
error convention is a human diagnostic tuple (`:186`). AD-9 collapses all failures to blocked/invalid
evidence (`:139-146`). No output envelope, stable error code, exit-code taxonomy, aggregation ordering,
or remediation owner is fixed.

**Constructed divergence:** One consumer parses stderr strings, another parses a partial JSON document,
and a third treats exit code 1 as semantic incompatibility but exit code 2 as infrastructure failure.
They can report different owners and permit different retry/fallback behavior for the same fault.

**Disposition:** **Autofix.** Define a versioned success/failure JSON envelope and stable categories
for policy/trust, object acquisition, graph structure, semantic catalog, affected build, manifest drift,
and internal failure. Bind each category to its failing gate and remediation owner. All categories must
remain release-blocking; retries or approved fallbacks must be explicitly enumerated.

### M-2 — Per-object ceilings still permit unbounded aggregate cost

**Evidence:** AD-7 sets 4,096 edges and per-owner/per-blob limits (`:110-120`) but no total tree bytes,
total catalog bytes, owner/object count independent of edges, subprocess timeout, or diagnostic output
limit. At the allowed maxima, aggregate object processing can be hundreds of GiB.

**Constructed divergence:** Unit A enforces only the written per-object limits and exhausts a runner;
Unit B adds an undocumented 128 MiB total cap and rejects a graph Unit A accepts; Unit C times out Git
after an implementation-selected duration. Their accepted graph set differs.

**Disposition:** **Discuss, then autofix.** Add total byte/object/process/output budgets and deterministic
timeout classification, or justify a bounded acquisition model that provides the equivalent guarantee.
Pin inclusive boundary behavior in fixtures.

### M-3 — Base-only identities and remote acquisition are not fully defined

**Evidence:** AD-3 validates base and candidate mappings against one policy (`:67-71`); AD-10 requires
object stores including removed-base edges (`:153-156`). It does not say whether an identity removed
from candidate `.gitmodules` remains resolvable from base policy/data, which URL coordinate is allowed,
or how redirect/host/protocol changes are handled by acquisition.

**Constructed divergence:** One collector cannot reconstruct removed edges because it uses only
candidate mappings; another accepts the base URL even when policy has changed; another uses a current
remote and may fetch different authority than was sealed.

**Disposition:** **Autofix.** Define independent base/candidate mapping inputs under the pinned trust
policy, immutable identity-to-approved-fetch coordinates, redirect/protocol rules, and the exact
fail-closed result when a removed object cannot be acquired.

## Coverage Summary

| Attack surface | Result |
| --- | --- |
| Boundary | Critical source-of-truth conflict remains open. |
| Trust policy | Critical revision/authority ambiguity permits candidate-controlled trust or irreproducible verification. |
| Semantic profiles | High divergence in profile selection, equivalence, and XML semantics. |
| Graph/digest schema | High divergence between strict and permissive verifiers. |
| Base/candidate diff | High ambiguity between event base and computed merge-base. |
| Affected builds | High ambiguity in supported inventory, removals, and exact workspace materialization. |
| Manifest/fallback | High ambiguity in schema transition and invalidation formula. |
| Failure ownership | Medium interoperability and remediation ambiguity. |

## Gate Recommendation

Do not finalize the spine or release GOV-1 implementation from this draft. Resolve C-1 and C-2 first;
then fold H-1 through H-6 into enforceable AD rules or a versioned policy/schema artifact whose exact
trust coordinate is itself governed. M-1 and M-3 are clear fixes; M-2 needs one explicit operational
budget decision. After amendment, rerun lint and the full reviewer gate against the redistilled spine.
