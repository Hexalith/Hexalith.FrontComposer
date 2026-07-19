# Final Adversarial Review — GOV-1 Architecture

Date: 2026-07-19
Intent: final architecture gate
Authority assumption: all recorded choices were ratified by the user as Architect + Release Owner
Verdict: **FAIL — 0 Critical, 5 High findings**

The deterministic spine lint passes with zero findings. The bounded graph, canonical graph envelope,
base/before policy selection, build matrix, and manifest-v2 migration are internally enforceable. The
remaining High findings are all at the evaluator/release authorization seams.

## High findings

### H-1 — “Approved” immutable workflow/action commits have no enforceable trust root

**Evidence:** `ARCHITECTURE-SPINE.md` AD-12 lines 198-220 makes the active base/before policy authoritative
for identities, semantic profiles, argv, dispositions, and limits, but does not include trusted evaluator
coordinates. AD-13 lines 228-265 requires “approved” literal 40-hex reusable/action commits, while AD-14
lines 271-288 only seals and cross-checks the coordinates that actually ran. The PRD, FC-DEP-1, epics,
proposal, and story repeat “approved” without defining the machine-recognizable approval source.

**Adversarial construction:** candidate A changes the caller to a literal 40-hex commit selected by A.
CI records the actual caller/reusable/action coordinates; release confirms those coordinates equal the
literal references; the handoff, manifest, seal, and fallback digest all agree. Every stated equality
passes, but no independent authority ever proves that the selected evaluator commit was approved. A
second implementation can choose another literal 40-hex commit and also conform. Hashing proves identity,
not authorization.

**Required closure:** put exact allowed caller/reusable/action coordinates or their canonical closure
digest in an authority that the candidate cannot self-select—preferably the active base/before policy—and
apply the same delayed-activation/bootstrap rule as other trust changes. CI handoff and release must reject
any observed closure not equal to that active authority. Add a negative fixture where a well-formed,
fully sealed but unapproved 40-hex evaluator revision fails before handoff/publication.

### H-2 — The “every executed transitive action” closure is not deterministically computable as written

**Evidence:** AD-13 lines 254-265 requires every transitive action source and says Governance parses the
caller and reusable workflow blobs. AD-14 lines 277-288 defines `actions` as every **executed** external/
local action source. Static YAML parsing finds reachable `uses:` entries, not which conditional steps the
runner executed. Parsing only the workflow blobs also misses nested `uses:` in composite `action.yml`
files; current `Hexalith.Builds/Github/dapr-init/action.yml` itself invokes `dapr/setup-dapr` and
`nick-fields/retry`.

**Adversarial construction:** producer A records all statically reachable actions, including skipped
conditions and composite descendants. Producer B records only actions observed in the current path.
Both can plausibly satisfy “every executed action,” yet produce different evaluator and fallback digests.
A shallow parser can also omit composite descendants while still finding every `uses:` in the two
workflow files named by AD-13.

**Required closure:** choose one testable model. Either define a static transitive source closure over
exact workflow and composite-action metadata, independent of runtime conditions, or name an authenticated
runtime trace as the authority. For a static model, specify included `uses:` forms, local/external/nested
reusable resolution, condition handling, cycle detection, ordering, and depth/file/byte limits. Pin it with
a golden fixture containing a conditional step, a local composite action with external descendants, and a
cycle/limit negative.

### H-3 — Required reusable-workflow changes have no owned, accepted upstream delivery gate

**Evidence:** AD-13 requires the reusable release workflow to accept and consume the exact CI candidate,
expose/validate runtime `job.workflow_*` identity, use exact-SHA local actions, and participate in the
handoff/provenance contract. The GOV-1 story lines 107 and 118 require these results, while lines 205-209
forbid FrontComposer from changing Builds-owned source. Its only named upstream follow-up is BUILD-CAT-1.
The existing BUILD-REL-1 request has `accepted revision: pending` and does not yet include the complete
GOV-1 CI-handoff/exact-candidate/provenance contract. The currently selected `domain-release.yml` has no
required release-candidate input, and the current `domain-ci.yml` has no GOV-1 handoff/runtime-provenance
output.

**Impact:** local FrontComposer work can start, but GOV-1 cannot satisfy AC4/AC5 or unfreeze a governed
release without an external Hexalith.Builds revision. Different implementers can either violate the
repository ownership boundary, silently weaken runtime provenance, or leave the story permanently
incomplete.

**Required closure:** extend BUILD-REL-1 or create a named BUILD-GOV dependency with the exact
`workflow_call` inputs/outputs/artifact contract required by AD-13, an owner, issue URL, accepted 40-hex
revision, and explicit GOV-1 completion/release-unfreeze gate. If a FrontComposer-owned contingency is
allowed, record its authority, expiry, and migration trigger; otherwise state that Task 5 and story
completion are blocked until the upstream revision lands.

### H-4 — Normative sources contradict repository reality about the REL-4 authorization freeze

**Evidence:** PRD lines 373 and 529 and `epics.md` line 247 still say REL-4 is ready-for-dev and that
`release.yml` has no technical freeze. The repository already contains the REL-4 `freeze-guard` and
Release Owner variable check in `.github/workflows/release.yml` lines 38-83; sprint status places REL-4
in review. The GOV-1 spine/story correctly require preserving that freeze while mutable/exact-revision
seams remain nonconforming.

**Impact:** the canonical requirements simultaneously describe publication as merely administratively
blocked and technically blocked. Release readiness, allowed sequencing, and any claim that GOV-1 may
unfreeze publication therefore depend on which normative paragraph an implementer follows.

**Required closure:** reconcile PRD D-6/FR-24 and the epics truth-state to: the caller-side gate is
implemented and in review, live frozen-run evidence remains outstanding, and publication remains
unauthorized. Keep the distinction between “control exists in source,” “control is live-verified,” and
“Release Owner has enabled publication” explicit.

### H-5 — The Release → post-publication-verifier hop is not bound to the released CI candidate

**Evidence:** AD-13 precisely authenticates CI → Release, but there is no equivalent contract for
Release → `release-evidence.yml`; AD-9 only says post-publication live verification reconstructs the
sealed root. The GOV-1 story line 117 says to reconstruct from the “upstream release commit” without
defining which revision that means. GitHub documents that a `workflow_run` workflow uses the last commit
on the default branch for `GITHUB_SHA`/`GITHUB_REF`, not the original upstream workflow’s nested trigger
candidate. The current verifier checks out and resolves the release tag from
`github.event.workflow_run.head_sha` (`release-evidence.yml` lines 46-52 and 107-151), even though the
Release run may have published the earlier CI candidate passed under AD-13.

**Adversarial construction:** main advances after CI completes but before/during the Release run. Release
correctly publishes candidate C under AD-13; the Release workflow run is associated with later default-
branch SHA D. The post-publication workflow searches for a tag attached to D, finds none, and green-no-ops,
so C's published assets are never independently checked.

**Required closure:** define a versioned Release → verifier handoff authenticated by Release run ID and
attempt. It must carry the original CI candidate, release tag/version, manifest/release-asset identity,
and release evaluator coordinates, and be emitted even for failed/partial runs. The verifier must derive
the live root from the sealed manifest/handoff and must never treat the second-hop
`workflow_run.head_sha` as the published candidate. Pin/record the verifier workflow and its transitive
actions, and add a race fixture where default branch advances between CI, Release, and verification.
GitHub platform basis: <https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#workflow_run>.

## Gate disposition

Do not treat the architecture as implementation-final until H-1 through H-5 are either incorporated or
explicitly superseded by a newly ratified decision. Publication remains frozen; no finding requires
weakening the bounded graph or manifest-v2 decisions.
