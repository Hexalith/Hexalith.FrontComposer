# Reviewer Gate — current-tech / brownfield reality lens — 2026-09-09

**Subject:** `ARCHITECTURE-SPINE.md` SHA-256
`95cbe2f2754ad3c1d7c896e73822b49edb676c0f5921c4f05fded82519c6e6e8`

**Repository baseline:** FrontComposer HEAD
`053b2008307d4e476c0d4329e6c47763c301d43e`; release execution pin
`4eb33928a1d8c7775f97221cf9edc171db0cb5f8`; current Builds catalog gitlink
`a32cb422749352cce8dec948aa3e78c8f00eb4cf`.

## Verdict

**FAIL — one High current-platform feasibility defect remains.** The bounded-object graph, split
builder/publisher boundary, current Git/Python/.NET choices, immutable artifact transport, protected
environment, attestation, immutable GitHub Release, and NuGet repository-signature mechanisms all
exist and fit this public GitHub.com repository. However, AD-18 requires an exact environment
approval timestamp that GitHub's documented and observed read-only workflow-run approval response
does not expose. Because missing actor/environment state makes a governed publication
`non-compliant`, the specified ledger cannot be produced truthfully for a successful release.

The current production implementation remains materially different from the target spine, but this
is not an additional spine finding: the Adoption/Release Eligibility and Deferred sections accurately
halt releases and enumerate the migration bundles rather than claiming that the controls are already
live.

## Findings

### FIT-1 — High — `actors.environment.approved_at` has no authoritative API source

**Affected rules:** AD-18 lines 686–696; AD-15 lines 543–553.

AD-18 closes `actors.environment` as `{name, approver, approved_at}`, requires `approved_at` to be an
RFC 3339 UTC approval time, and makes missing actor/environment state `non-compliant`. GitHub's
read-only **Get the review history for a workflow run** endpoint returns the approving user, state,
comment, and environments, but its documented response has no approval timestamp. A live query of
FrontComposer Release run `34153315061` returned the same shape, including two approval records with
no event ID or timestamp. Environment `created_at`/`updated_at` describe the environment resource,
not the review. Deployment/status timestamps describe job state transitions and do not identify the
approval event or approver; treating them as `approved_at` would invent evidence.

This is not a missing implementation detail: the closed schema demands information the selected
platform interface does not provide. A conforming verifier would therefore reject every otherwise
successful governed release. GitHub's official response schema is visible in
[REST workflow-run review history](https://docs.github.com/en/rest/actions/workflow-runs#get-the-review-history-for-a-workflow-run).

**Disposition:** **discuss / amend the spine before implementation.** Choose an observable contract,
for example `(approver, approval_observed_at)` where the latter is explicitly the server timestamp of
the matching deployment transition, or require a separate owner-authored approval receipt whose API
object has an immutable server timestamp. Do not label a job/deployment timestamp as the exact review
time. Add a live-shape fixture containing repeated approvals and require an unambiguous mapping to the
single protected publisher.

### FIT-2 — Medium — the exact attestation action/permission contract is underspecified

**Affected rules:** AD-9 lines 256–265; AD-18 lines 674–684; AD-19 lines 743–756.

The intended binary attestation is feasible in this public repository: GitHub supports signing
multiple file subjects and returns a Sigstore `bundle-path`. The current generic
`actions/attest@v4` guidance also lists `artifact-metadata: write`, while AD-18's exhaustive caller
scope set omits it and sets unspecified permissions to `none`. GitHub's binary-attestation how-to
still shows the narrower `contents: read`, `id-token: write`, and `attestations: write` set, and
artifact-metadata storage records principally matter to linked/registry artifacts. That leaves two
plausible implementations with different permission behavior.

Primary references: [actions/attest](https://github.com/actions/attest) and
[GitHub binary attestation guidance](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations).

**Disposition:** **autofix in the spine.** Pin the exact owner-accepted attestation action and inputs.
If linked-artifact storage records are out of scope, explicitly disable them and fixture the current
minimal permission set; if they are required, add only `artifact-metadata: write` to the protected
attester and to the caller/reusable static assertions. The builder must still receive neither scope.

### FIT-3 — Medium — immutable Release publication needs an explicit draft sequence

**Affected rules:** AD-9 publication ordering; AD-19 lines 757–763.

Release immutability is enabled for `Hexalith/Hexalith.FrontComposer`, and the Release API exposes
both release-level `immutable` and asset SHA-256 fields. The platform freezes the tag and assets when
a release is published. GitHub consequently recommends **create draft → upload every asset → publish
the draft**. AD-19 fixes the successful end state but does not constrain this required publication
sequence. A new candidate-free publisher that creates a non-draft release before uploading assets
cannot complete once immutability applies; another implementation that follows the draft sequence
can. See [GitHub immutable releases](https://docs.github.com/en/code-security/concepts/supply-chain-security/immutable-releases)
and [managing releases](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository).

**Disposition:** **autofix in the spine.** Make draft creation the first GitHub mutation after
`publication_started`, upload the complete collision-checked authorized asset set, then publish once;
any abandoned draft or tag/package side effect remains a partial-publication incident. Require the
post-release verifier to check `draft == false`, `immutable == true`, the exact tag, and the complete
asset set.

### FIT-4 — Low — the reviewer projection assumes every reviewer has `login`

**Affected rule:** AD-18 lines 686–692.

The live production environment currently has one `User` reviewer, so `{type,id,login}` is
representable today. GitHub environments also support `Team` reviewers; their API objects expose a
team `slug`/`name`, not a user `login`. The closed projection would become unrepresentable if the
Release Owner control moves to a team.

**Disposition:** **autofix or defer with trigger.** Either constrain required reviewers to `User`, or
use a tagged identity projection such as `{type,id,login,slug}` with exactly one of `login`/`slug`
non-null. Revisit before adding a team reviewer.

## Confirmed current fit

- Local/runtime observations match the Stack table: Git `2.53.0`, Python `3.14.4`, .NET SDK
  `10.0.400`, and `global.json` pins the `10.0.400` feature-band floor with `latestPatch`.
- `actions/upload-artifact` v7.0.1 at `043fb46d…` is current, supports immutable unique artifacts,
  exposes an artifact SHA-256, and remains GitHub.com-only for v4+ semantics. The REST artifact object
  also exposes `digest`; the spine's coordinate + digest authentication is feasible. See the
  [official action README](https://github.com/actions/upload-artifact) and
  [Actions artifacts REST API](https://docs.github.com/en/rest/actions/artifacts).
- FrontComposer is public; artifact attestations are available. GitHub's current action returns a
  Sigstore bundle and supports multiple subjects, fitting the package-digest projection.
- Repository release immutability is live (`GET /immutable-releases` returned
  `{enabled:true,enforced_by_owner:false}`); release `v4.4.0` is non-draft, immutable, and exposes
  asset digests. The target end state is therefore supported.
- The live `production` environment has required reviewer `jpiquot`, `can_admins_bypass=false`, and
  one custom deployment branch policy `main`. The environment and branch-policy APIs require only
  Actions read for this public repository, matching the read-only verifier design. GitHub's
  [environment API](https://docs.github.com/en/rest/deployments/environments#get-an-environment)
  exposes the configured protection fields.
- Live `main` still has no ruleset/branch protection and no `CODEOWNERS`; the spine reports this
  brownfield limitation and makes sign-off implementation-ineligible until the no-bypass ruleset
  exists.
- The exact pinned Builds reusable and current FrontComposer caller still combine or misplace
  candidate execution and production authority, use the old governed/legacy modes, and lack the v3
  handoff/v4 manifest/ledger path. The spine neither ratifies that as conforming nor hides it: it
  requires a new owner-accepted opt-in/default-off split reusable, delayed activation, caller switch,
  candidate-free publisher, pinned post-release helper, schema migration, fixtures, source
  reconciliation, and the incident runbook before release eligibility.
- NuGet.org repository signing is compatible with retaining author-unsigned candidate packages and
  verifying a valid root `.signature.p7s` plus normalized equality of every other ZIP member; the raw
  candidate remains independently available as the immutable GitHub Release asset.

## Incomplete lower-severity checks

- No end-to-end split workflow exists yet, so runner-level behavior for the future exact reusable,
  attestation action, handoff archive, draft Release sequence, and hostile-candidate fixture could not
  be executed. These are already implementation gates, except for FIT-2/FIT-3's missing exact
  architecture choices.
- NuGet propagation latency and bounded retry behavior were not measured against a fresh publication.
  The spine's job timeout bounds the run, but a later implementation should fixture transient
  not-found versus terminal mismatch so eventual consistency does not create a false incident.
- The review did not exhaustively re-run all 190 governance tests; prior reviewers did so against the
  same source baseline. This lens used source inspection, exact pinned workflow bytes, live read-only
  GitHub API responses, and focused local version/canonicalization checks.

## Finding counts

Critical: **0** · High: **1** · Medium: **2** · Low: **1**.

