# Adversarial Divergence Review — Pass 2

**Target:** `ARCHITECTURE-SPINE.md` and its GOV-1 companions
**Date:** 2026-07-19
**Verdict:** **CHANGES REQUIRED.** The revision closes the exact PR base, graph JSON, registry
membership, exact release commit, and legacy-manifest disposition findings. Four high-impact seams and
three medium residuals still permit independently conforming implementations to disagree. AD-1's
approved-source conflict is treated as the intentional human ratification gate and is not counted as a
review defect.

## High

### H-1 — Named semantic profiles still have no fixed semantics

**Evidence:** AD-6 and the Closed Policy Seed now require exactly one named profile per selector owner
(`ARCHITECTURE-SPINE.md:105-121`, `:236-252`), which closes profile *selection*. However, the seed says
implementation may "refine package assertions inside a named profile" (`:238-240`) and no companion
defines the exact rule set or a closed rule schema for `frontcomposer-catalog-v1`,
`eventstore-catalog-v1`, `memories-catalog-v1`, `parties-catalog-v1`, or
`shared-catalog-baseline-v1`.

**Constructed divergence:** Engine A makes `shared-catalog-baseline-v1` validate only well-formed XML;
Engine B requires the full current centrally governed package/version set. Engine C treats an import
condition as equivalent after whitespace normalization; Engine D requires the exact guarded expression.
All resolve each owner to the same profile name and remain defensible under "may refine," but report
different compatibility for the same catalog bytes.

**Disposition:** **Autofix.** Remove the refinement escape hatch. Define a closed versioned profile-rule
schema and exact seed content, or bind each profile to an immutable existing Governance contract whose
assertions are exhaustively enumerated. Catalog bytes may be cached by Builds commit, but evaluation
must be keyed by exact profile ID plus profile-content digest. Unknown rule kinds and incomplete
profiles must fail closed.

### H-2 — The explicit build matrix still does not implement the required proof

**Evidence:** The identity/solution inventory is now closed (`:254-272`), resolving the prior
"supported module" ambiguity. Its literal argv nevertheless restores without
`-p:Configuration=Release -p:UseNuGetDeps=true` and builds without `-p:UseNuGetDeps=true`
(`:254-257`), contradicting GOV-1's standalone Release/NuGet proof. The spine requires an isolated
checkout of the exact module commit but does not define how the exact selected Builds catalog is
materialized without nested initialization. AD-8 also retains independent handling of removed
depth-1 and depth-2 edges (`:139-149`), so a compound root removal can still request builds of owners
absent from the candidate graph.

**Constructed divergence:** Runner A restores in the SDK's default configuration and accidentally
uses source-project dependency wiring. Runner B supplies Release/NuGet properties. Runner C builds an
ambient initialized module; Runner D archives the candidate module but cannot resolve its Builds
imports. For a removed root module, one builds FrontComposer only while another attempts the removed
owner's now-impossible candidate build.

**Disposition:** **Autofix.** Pin the literal restore/build argv required by the story, including
`UseNuGetDeps=true` on both commands and Release configuration on restore. Define data-only
materialization of the exact selected Builds commit into the expected path, with no nested init or
candidate execution. Define compound-removal collapse: once a depth-1 owner is absent from candidate,
its removed depth-2 edges do not request an absent-owner build; the FrontComposer root proof owns that
removal. Add golden affected-set and argv fixtures.

### H-3 — CI-to-release policy coordinates have no authenticated transport contract

**Evidence:** AD-13 correctly pins `workflow_run.head_sha`, the reusable workflow revision, and the
release commit (`:201-212`). It says CI "supplies" the AD-12 policy coordinates and release reuses them
(`:209-210`), but `workflow_run` carries only run metadata; the spine does not define an input/artifact
schema, provenance check, or association among policy coordinates, triggering run ID, CI head SHA, and
graph digest.

**Constructed divergence:** Release A recomputes policy selection from the candidate commit, activating
a policy change too early. Release B downloads the newest artifact with a matching name, which can be
from another run. Release C trusts string inputs without reloading and hashing the exact policy object.
Release D downloads exact-run evidence and verifies the policy commit/blob/hash. All can claim to reuse
"CI-supplied coordinates," but only D preserves the intended trust chain.

**Disposition:** **Autofix.** Specify a closed CI handoff envelope tied to
`workflow_run.id` and `workflow_run.head_sha`, containing policy repository/commit/schema/raw SHA-256,
base/candidate/graph digests, and the CI workflow identity. Require exact-run artifact retrieval,
closed-schema verification, policy blob re-hash from its recorded commit, and equality with the sealed
manifest before preparation or classification. Any missing/mismatched handoff stays frozen and
non-publishable.

### H-4 — The graph is closed, but its v2 manifest container and fallback formula are not

**Evidence:** AD-5 now closes the complete dependency-graph JSON shape and duplicate-member behavior
(`:85-103`); that prior finding is resolved. AD-14 also makes legacy manifests audit-only,
non-publishable, fallback-ineligible, and immutable (`:214-222`); migration disposition is resolved.
But AD-14 names only `manifest_schema`, "the complete AD-5 graph," and "AD-12 policy coordinates"
without fixing the graph member path, exact policy-coordinate object, or allowed new member sets.
AD-9 says the graph digest invalidates fallback approvals (`:151-165`) without fixing its exact key and
canonical input in the existing fallback formula.

**Constructed divergence:** Producer A emits top-level `dependency_graph` and
`dependency_graph_policy`; Producer B nests both under `provenance`. Verifier C recomputes fallback
approval over `{"definition": ..., "package_set": ..., "dependency_graph": digest}`; Verifier D adds
the digest to the existing fingerprints map. Their graph bytes can be identical while seals,
fallback approvals, and offline fixtures remain incompatible.

**Disposition:** **Autofix.** Fix exact v2 member names and closed object shapes for the graph and
policy coordinate. Define the exact canonical fallback input and key placement, including legacy
fallback rejection before digest comparison. State that the outer seal hashes the validated original
v2 payload minus only `seal`, and add producer/verifier golden fixtures for manifest, seal, and fallback
digest bytes.

## Medium

### M-1 — Bootstrap and zero-base policy selection remain incomplete

**Evidence:** AD-12 now immutably selects `event_base` for PRs and non-zero `before` for pushes and
delays policy activation (`:185-199`); ordinary runs are convergent. The bootstrap permits a candidate
policy when no base policy exists, but does not define how "dependency graph is unchanged" is computed
without the missing policy, how approval evidence is authenticated, or the exact bootstrap policy
schema/digest expected from the Closed Policy Seed. AD-8 sends a zero/unavailable push base to a
full-affected path (`:143-144`), while AD-12 provides no active policy for that path.

**Constructed divergence:** Bootstrap A uses candidate policy to compare both graphs; Bootstrap B
compares raw gitlink tuples under the hard-coded spine seed; Bootstrap C refuses to run. On a created
branch/tag push, one runner uses candidate policy and another fails before full-affected builds.

**Disposition:** **Autofix.** Define bootstrap graph equality independently of candidate trust, bind the
candidate policy to the exact closed seed and authenticated Architect/Release Owner approval record,
and state precisely when that candidate policy may be used for the bootstrap run. For zero-base pushes,
either define a trusted policy coordinate or fail the workflow before collection; "full affected" alone
cannot supply trust policy.

### M-2 — Approved fetch coordinates are still implicit

**Evidence:** AD-10 says acquisition uses root-declared approved remotes (`:167-175`), while AD-3 treats
candidate `.gitmodules` as untrusted and AD-12's policy is described as identity/path mappings, not an
exact identity-to-fetch-coordinate registry (`:185-191`). Neither the Closed Policy Seed nor companions
list immutable approved fetch URLs or redirect behavior.

**Constructed divergence:** Acquirer A fetches from the base `.gitmodules` URL; Acquirer B reconstructs
`https://github.com/{identity}.git`; Acquirer C follows redirects. They can contact different authorities
or reconstruct base-only objects differently while accepting the same canonical identity.

**Disposition:** **Autofix.** Make approved fetch coordinates an explicit policy field, pin protocol,
host, owner/repository, redirect prohibition, and base-only resolution. Acquisition must never derive a
network endpoint from candidate graph data.

### M-3 — The Python machine-result and failure taxonomy remain undefined

**Evidence:** AD-6 still makes C# Governance consume a Python "machine result" (`:114-116`), while the
only cross-consumer error contract is an actionable prose diagnostic (`:233`). There is no closed
success/error envelope, stable error code set, exit-code mapping, or remediation ownership.

**Constructed divergence:** One C# test parses stdout JSON, another parses diagnostic strings, and CI
distinguishes infrastructure from semantic failure by implementation-selected exit codes. They can
aggregate selector failures differently and route the same fault to different owners.

**Disposition:** **Autofix.** Define a versioned closed result envelope, deterministic diagnostic order,
stable failure categories, exit-code mapping, and owning gate/remediation role. Every category remains
release-blocking unless a separately enumerated fallback says otherwise.

## Prior-Finding Closure Matrix

| Prior concern | Pass-2 result |
| --- | --- |
| AD-1 approved-source conflict | Intentional ratification gate; excluded from defect verdict. |
| Immutable policy selection | Closed for ordinary non-zero-base PR/push runs; bootstrap and zero-base paths remain. |
| Exact PR diff base | Closed: event base, merge revision, equality check, and diff boundary are explicit. |
| Supported-module inventory | Closed; execution/materialization and compound-removal rules remain. |
| Semantic profile selection | Owner mapping closed; profile contents remain open. |
| Graph/digest JSON | Closed, including duplicate names and exact members. |
| Release exact commit/ref | Exact commit and reusable-workflow ref closed; policy handoff transport remains. |
| Legacy manifest migration | Closed: audit-only, non-publishable, no fallback, no reseal. |
| Manifest/fallback integration | Still open at the v2 container and canonical fallback-input seam. |
| Failure ownership | Still open. |

## Gate Recommendation

Retain draft status. Apply H-1 through H-4 before requesting human ratification: they are clear
convergence fixes, not product choices. M-1 and M-2 must also be fixed before the bootstrap CI can be
implemented safely. M-3 can be a compact additional AD or closed engine-result schema, but should not
be deferred because C#, CI, and release tooling are independent consumers.
