# Adversarial Divergence Review — Pass 3

**Target:** Current `ARCHITECTURE-SPINE.md` and GOV-1 companions
**Date:** 2026-07-19
**Verdict:** **CHANGES REQUIRED.** No Critical findings remain. The revision closes the normative
profile contents, exact Release/NuGet argv, authenticated exact-run handoff, manifest member placement,
fallback formula, bootstrap/zero-before behavior, and immutable-action intent. Three High seams remain.
AD-1's approved-requirements conflict is treated as the intentional human ratification gate and is not
a finding.

## High

### H-1 — Catalog-only extraction cannot execute every matrix build

**Evidence:** AD-8 and the Closed Policy Seed materialize only
`Props/Directory.Packages.props` from the selected Builds commit
(`ARCHITECTURE-SPINE.md:149-152`, `:307-325`). That is enough for modules that consume only the shared
catalog, but it is not enough for every listed target. For example, the exact current Commons and
PolymorphicSerializations commits import
`references/Hexalith.Builds/Hexalith.Build.props` unconditionally; that file imports
`Props/Environment.Build.props` and uses additional Builds-owned configuration. An isolated archive of
either module plus one catalog blob therefore fails before it can provide the required standalone
proof. AD-8 also still assigns removed depth-2 edges to the candidate owner independently of a removed
depth-1 owner (`:145-147`), so a compound root removal can request an impossible build of an owner that
does not exist in the candidate graph.

**Constructed divergence:** Runner A follows the literal catalog-only rule and fails Commons/Poly with
a missing MSBuild import. Runner B archives the complete exact selected Builds commit into the gitlink
path and succeeds. On a removed root module, Runner C builds FrontComposer only while Runner D also
attempts the absent candidate owner because it processes each removed child edge independently.

**Required fix:** Materialize the deterministic build-input closure required by the target—safest is a
data-only archive of the complete exact selected Builds commit at the declared gitlink path—then verify
the catalog hash before restore; never initialize or execute the nested repository. Define compound
removal collapse explicitly: when a depth-1 owner is absent from candidate, its removed depth-2 edges do
not request an absent-owner build and the FrontComposer root proof owns that removal. Pin both behaviors
with matrix fixtures.

### H-2 — New sealed arrays and scalar fields are not canonically typed or ordered

**Evidence:** AD-14 fixes the exact placement and member sets of `dependency_policy` and
`workflow_provenance`, but says only that `actions` is "explicitly sorted" without naming its sort key
(`:246-264`). The handoff similarly fixes member sets while leaving `run_id`, `run_attempt`, and other
scalar types/normalization unstated (`:232-237`). AD-5 supplies strict types and an ordinal tuple for the
graph, but those rules do not define these new v2/handoff fields.

**Constructed divergence:** Producer A orders actions by `(repository,path,commit)`; Producer B orders
by `(path,repository,commit)`. One serializes `run_id`/`run_attempt` as JSON integers while another
preserves GitHub's values as strings. Each emits the exact named members and an explicitly sorted list,
yet the raw handoff SHA, outer manifest seal, and offline verification result differ.

**Required fix:** Give every handoff and workflow-provenance scalar an exact JSON type and grammar,
including positive integer/string policy for run identifiers and attempts. Sort `actions` by one named
ordinal tuple, reject duplicates by one named identity key, and apply exact repository/path/commit/hash
normalization. Add golden byte fixtures for the handoff, workflow provenance, and outer seal.

### H-3 — Exact-run authentication does not yet prove the active policy bytes

**Evidence:** AD-13 strongly authenticates the Actions run and artifact and requires candidate equality
before accepting graph/policy coordinates (`:214-237`). It then says release "reuses" the coordinates
(`:227-228`), but neither AD-9 live verification (`:154-172`) nor AD-13 explicitly requires loading
`eng/dependency-graph-policy.json` from the recorded policy commit and matching its raw SHA-256 to both
the handoff and manifest before graph reconstruction and semantic acceptance. Exact-run provenance
authenticates who emitted the claim; it does not independently prove the claimed blob coordinate.

**Constructed divergence:** Verifier A trusts the successful CI artifact's policy hash as supplied.
Verifier B reads the recorded commit's policy object, hashes its raw bytes, parses the expected schema,
and compares handoff/manifest coordinates before use. A malformed or buggy handoff can therefore be
sealed and accepted by A but rejected by B, even though both use the exact triggering run.

**Required fix:** Make live release verification read the exact policy blob at the recorded
repository/commit/path, reject missing or duplicate policy objects, verify raw SHA-256 and schema
against both handoff and manifest, and use those verified bytes for graph/semantic reconstruction.
Policy mismatch must fail before preparation, fallback, classification, or publication.

## Gate Recommendation

Keep the spine draft until H-1 through H-3 are folded into enforceable rules. These are deterministic
autofixes, not new product decisions. Afterward, the only remaining architecture blocker should be the
intentional AD-1 Architect + Release Owner ratification gate.
