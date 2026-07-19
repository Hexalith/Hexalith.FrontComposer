# Current-Fit Review, Pass 3 — GOV-1 Dependency Provenance

**Target:** `ARCHITECTURE-SPINE.md` and its two named companions
**Lens:** repository-fit of the revised release provenance, manifest, policy, semantic-profile, and
affected-module contracts
**Review date:** 2026-07-19
**Verdict:** **CHANGES REQUIRED.** The versioned/authenticated CI handoff, actual workflow coordinates,
transitive action pinning, v2 manifest shapes, machine-addressed bootstrap, closed policy/profile maps,
and exact argv/catalog mapping resolve the prior pass's release and schema findings. Two High defects
remain in the affected-module algorithm.

The AD-1 source conflict is intentionally excluded from this verdict and remains the sole ratification
gate.

## Critical

None.

## High

### H-1 — Catalog-only materialization cannot execute two current standalone build rows

AD-8 and the Closed Policy Seed materialize only
`references/Hexalith.Builds/Props/Directory.Packages.props` for an edge-bound build before running the
literal restore/build argv (`ARCHITECTURE-SPINE.md:149-152`, `:308-325`). That is insufficient for two
repositories in the closed current seed:

- exact Commons commit `ea1fc4551dcaf8ee63fd562d77dfe0f18c57a94c` unconditionally falls through
  to an import of `references/Hexalith.Builds/Hexalith.Build.props` when no sibling copy exists
  (`references/Hexalith.Commons/Directory.Build.props:7-9`, `:29-32`);
- exact PolymorphicSerializations commit `f977018abdd34de93c82ed050b746e4e30b0a960` has the same required import
  (`references/Hexalith.PolymorphicSerializations/Directory.Build.props:7-9`, `:15-18`).

Both selected Builds commits contain the catalog and `Hexalith.Build.props`, but an isolated owner
archive contains neither gitlink contents, and the prescribed catalog-only extraction supplies only the
first file. MSBuild therefore fails on the missing build-props import before the gate can prove catalog
compatibility. This is a false-red path in two mandatory `build` rows, not a future edge case.

**Required correction:** Define one safe, deterministic build-support materialization rule. For example,
archive the complete tree of the exact selected Builds commit from the approved object store into the
owner's listed gitlink path, without initializing a repository or traversing its gitlinks; reject unsafe
archive entries/collisions; then re-hash the catalog against the graph before invoking the static argv.
If a smaller closure is intended, enumerate every required Builds file and its verification rule rather
than specifying only the catalog.

### H-2 — Removing a depth-1 module schedules an impossible candidate-owner build

AD-8 maps every removed depth-2 edge to a build of its candidate owner, while a removed depth-1 edge
builds FrontComposer root (`ARCHITECTURE-SPINE.md:145-152`). Removing or replacing a root-selected
module removes that module's depth-2 edges from the candidate graph as a cascade. Those child removals
therefore schedule the old module as a candidate owner even though it has no candidate commit or
candidate Builds edge to materialize. The closed registry cannot satisfy that instruction, so a valid
root dependency removal/replacement is either unimplementable or produces divergent special cases.

**Required correction:** Close the cascade rule. A removed depth-1 edge should schedule the root proof
and suppress builds for depth-2 removals owned only by that removed/replaced edge. A removed depth-2
edge should schedule a candidate-owner build only when that canonical owner survives in the candidate
depth-1 graph; define the equivalent behavior for root repository replacement and pin the resulting
fixture cases.
