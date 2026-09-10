# GOV-1 split-publication delivery reconciliation — final recheck 2026-09-09

## Scope

This final recheck compares the amended GOV-1 architecture spine and memlog entries 120–131 with the
GOV-1 story, G2 / BUILD-REL-1 request, nonconformance register, current FrontComposer release
workflow/helpers at `053b2008307d4e476c0d4329e6c47763c301d43e`, and the exact pinned
`Hexalith.Builds` reusable at `4eb33928a1d8c7775f97221cf9edc171db0cb5f8` (workflow blob
`da67783aa419193e50fcc23b27e5caf752d7a3f2`). This verdict is bound to the reviewed
`ARCHITECTURE-SPINE.md` Git blob `f3a106c8e8221cfa45665e9f7a65a35c3810a023`. No source, workflow,
helper, spine, or register was changed by this reconciliation.

## Final verdict

**PASS — no remaining Critical or High delivery-reconciliation finding.**

The amended spine now preserves or explicitly supersedes every load-bearing delivery constraint found
in the declared sources, accurately distinguishes the desired split from brownfield reality, and
defers the complete implementation migration as discrete release-ineligibility gates.

This is an architecture reconciliation pass, not an implementation-conformance claim. The current
FrontComposer caller and pinned Builds reusable remain materially nonconforming until the Deferred
closure rows are implemented and closed.

## Resolution of initial findings

### DEL-SPLIT-1 — Resolved

Final authorization is assigned exclusively to the candidate-free protected publisher. AD-9 now says
builder checks are early denial only; after artifact authentication, pinned owner-controlled publisher
code obtains/verifies attestation or fallback, prepares and seals manifest v3, performs offline/live
verification and classification, and requires `publish_authorized=true` before NuGet/GitHub Release
mutation (`ARCHITECTURE-SPINE.md:245-254`). AD-19 repeats the boundary, ignores builder-produced
readiness claims, and returns the final manifest/attestation projections in AD-15
(`ARCHITECTURE-SPINE.md:699-719`).

### DEL-SPLIT-2 — Resolved

BUILD-REL-1 is now explicitly opt-in/default-off or a sibling reusable, with existing
`domain-release.yml` callers behaviorally unchanged until they select it. The builder uses
`submodules: false`, initializes only FrontComposer root-declared dependencies, and forbids recursive
or nested initialization (`ARCHITECTURE-SPINE.md:668-681`). The inherited BUILD-REL-1 invariant carries
the same rollout and initialization contract (`ARCHITECTURE-SPINE.md:72`).

### DEL-SPLIT-3 — Resolved

The spine records REL-5's dated supersession of stale author-signing/RFC-3161 requirements while
retaining mandatory non-signing provenance attestation or sealed approved-unsupported fallback
(`ARCHITECTURE-SPINE.md:72`; memlog entry 128). AD-15 closes the attestation projection and makes its
verification necessary for `compliant-candidate` (`ARCHITECTURE-SPINE.md:480-490,521-570`). AD-19
requires the protected publisher to mint/verify an attestation for every package digest or validate
the fallback, bind it into the final manifest/handoff, and preserve NuGet repository-signature
equivalence (`ARCHITECTURE-SPINE.md:707-719`).

### DEL-SPLIT-4 — Resolved as an explicit implementation handoff

The Deferred section now enumerates all eight closure bundles: owner-accepted split reusable plus
delayed activation; caller switch/exact topology; removal of `production` from candidate jobs;
publication-candidate production/authentication/hostile fixtures; handoff-v3/ledger migration;
candidate-free final classification/publication; pinned post-release helper execution; and duplicate
destination-name rejection. It keeps release ineligible until source owners reconcile and close them
(`ARCHITECTURE-SPINE.md:881-890`).

That accurately covers the current gaps: the caller still selects the legacy reusable path; the exact
pinned legacy and governed paths each combine candidate execution with protected/write authority;
FrontComposer's local candidate job still declares `production`; handoff code remains v1 with no
publication-candidate contract; and post-release verification still executes ambient/candidate helper
code. Those are implementation nonconformances, not remaining spine ambiguities.

## Additional high-risk consistency recheck

- The publication-candidate descriptor has a closed data-only release plan and no builder-produced
  final-manifest authority (`ARCHITECTURE-SPINE.md:683-697`).
- `publication_started` is now an owner-controlled marker set immediately before the first NuGet or
  GitHub Release mutation, not when the protected job or prerequisite attestation starts. This makes
  `rejected-before-publication` and `partial-publish-incident` disjoint and API-checkable
  (`ARCHITECTURE-SPINE.md:494-510`; memlog entry 131).
- Full FR-24 inventory/test/consumer/checksum/SBOM/symbol evidence, exact tag binding, GitHub raw-byte
  identity, and NuGet normalized-ZIP equivalence remain binding (`ARCHITECTURE-SPINE.md:69,675-719`).
- Split ownership is explicit: Builds owns reusable code and minimum permissions; FrontComposer owns
  schemas, exact output bytes, classification, invocation, publication, and verification; the Release
  Owner owns approval, credentials, exceptions, and containment (`ARCHITECTURE-SPINE.md:597-603`).
- The flow diagram now carries packages plus evidence from builder to publisher and does not depict a
  builder-produced final seal (`ARCHITECTURE-SPINE.md:832-843`).

## Remaining Critical/High findings

None.
