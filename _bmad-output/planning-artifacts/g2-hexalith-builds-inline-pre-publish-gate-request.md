# Required upstream dependency (G2 / BUILD-REL-1): opt-in governed NuGet release contract for the FR24 pre-publication gate

- **Raised by:** REL-2 on 2026-07-13; made mandatory by REL-3 on 2026-07-15; extended with the
  common release-freeze gate by REL-4 on 2026-07-15 and the exact-candidate/evaluator-handoff contract
  by ratified GOV-1 on 2026-07-19
- **Target repository:** [Hexalith/Hexalith.Builds](https://github.com/Hexalith/Hexalith.Builds)
- **Target files:** `.github/workflows/domain-ci.yml`, `.github/workflows/domain-release.yml`, and
  exact local composite-action metadata used by those shared reusable workflows
- **Status:** issue filed; required/blocking; GOV-1 amendment acceptance and immutable revision pending
- **Suggested upstream story title:** "BUILD-REL-1: Add an opt-in governed NuGet release contract
  to Hexalith.Builds"
- **Upstream verification (2026-07-15):** a live search of Hexalith.Builds issues and pull requests
  found no matching issue or PR (only closed 2025 issues #1/#2 and dependabot/docs PRs); filing
  remained pending until 2026-07-18.
- **Upstream owner:** Release Owner (jpiquot) — filed under Release Owner directive on 2026-07-18
- **Issue/story URL:** <https://github.com/Hexalith/Hexalith.Builds/issues/17> (filed 2026-07-18 with
  the original release/freeze scope; the 2026-07-19 GOV-1 CI/exact-candidate/closure/handoff amendment
  below must be owner-accepted before the issue satisfies this dependency)
- **Accepted revision:** pending
- **Release impact:** blocks GOV-1 Tasks 4/5, story completion, release eligibility, unfreeze, and the
  next FrontComposer NuGet/GitHub release while the accepted immutable revision is pending. No local
  contingency is authorized without a new dated Architect + Release Owner decision.
- **This repo does NOT directly implement the shared change.** `references/Hexalith.Builds` is a shared
  `@main` submodule consumed by every Hexalith module and must not be edited or committed from
  FrontComposer. Filing, approval, implementation, and revision tracking are Release Owner plus
  Hexalith.Builds-owner actions.

## Why

The G1 posture approved on 2026-07-13 publishes through `domain-release.yml` and reconstructs evidence
after publication. Live v3.2.1 and v3.2.2 executions proved G1 insufficient: both evidence workflows
concluded successfully while packages were unsigned, manifests were invalid, readiness was blocked,
and `publish_authorized=false`. A reconstructed package cannot prove the bytes NuGet already received.

REL-3 therefore moves the complete FrontComposer-owned lifecycle into semantic-release preparation:
pack once, validate, sign/timestamp, verify, checksum, seal/verify the manifest, then run
`classify-release --require-publishable`. The shared workflow need not implement FrontComposer's evidence
logic, but it **must** make the signing contract available to the repository-owned semantic-release
lifecycle. G1 is no longer an approved fallback.

## Required change (backward-compatible for existing callers)

Signing-secret forwarding alone is **under-scoped** and is superseded by this revision. FrontComposer's
manifest validation (`eng/release_evidence.py`) requires every package to carry
`attestation_status=attested` with an attestation bundle (or a sealed Release Owner-approved
`approved-unsupported` fallback). Minting that attestation requires `actions/attest-build-provenance`
running inside the workflow that owns the candidate packages, with `id-token: write` and
`attestations: write` — workflow-level permissions and a lifecycle position that no forwarded secret
can provide. Secret forwarding cannot interleave a GitHub attestation between package preparation and
readiness classification.

The required upstream feature is an **opt-in governed mode** on `domain-release.yml` — or a sibling
governed release workflow — providing:

- an opt-in activation input (default off; existing callers unchanged);
- a protected release-environment/approval input applied to the governed release job;
- optional `workflow_call` secrets `NUGET_SIGNING_CERTIFICATE_BASE64` and
  `NUGET_SIGNING_CERTIFICATE_PASSWORD`;
- a configurable RFC 3161 timestamp-service input (default may remain the approved public timestamp
  authority);
- signing secrets scoped only to the governed release steps, never printed or persisted;
- `id-token: write` and `attestations: write` granted only where required by the governed mode,
  preserving minimum permissions elsewhere;
- a pre-publication candidate phase that knows the semantic-release version and produces the exact
  candidate packages before any publication side effect;
- `actions/attest-build-provenance` executed over those exact candidate packages;
- the resulting attestation-bundle path passed to the caller's manifest-finalization hook (e.g., an
  environment variable or output available to the semantic-release `prepareCmd` lifecycle);
- semantic-release publishing the already-authorized artifacts without rebuilding or repacking;
- durable failure and partial-publication evidence surfaced by the governed mode;
- backward compatibility for every existing Hexalith module caller (all new inputs unset → current
  behavior);
- root-only, non-recursive submodule initialization preserved.

FrontComposer's `.releaserc.json` continues to own the semantic-release `prepareCmd`/`publishCmd`
evidence lifecycle and consumes `${nextRelease.version}`; the shared workflow provides the governed
execution context (secrets, permissions, environment, candidate phase, attestation) — not
FrontComposer's evidence logic.

### Caller shape once G2 lands (FrontComposer)

```yaml
jobs:
  release:
    uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@<accepted-40-hex-revision>
    with:
      solution: Hexalith.FrontComposer.slnx
      test-projects: ''
      governed-release: true
      release-environment: production-release
      nuget-signing-timestamper: https://<approved-rfc3161-authority>
    secrets:
      NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
      NUGET_SIGNING_CERTIFICATE_BASE64: ${{ secrets.NUGET_SIGNING_CERTIFICATE_BASE64 }}
      NUGET_SIGNING_CERTIFICATE_PASSWORD: ${{ secrets.NUGET_SIGNING_CERTIFICATE_PASSWORD }}
```

FrontComposer then runs its pack-once evidence lifecycle from semantic-release `prepareCmd`, re-verifies
authorization in `publishCmd`, and uses `release-evidence.yml` only to verify downloaded NuGet/GitHub
assets and update the historical ledger.

## Constraints

- Must be **opt-in and backward-compatible** — every current caller (Tenants, Parties, EventStore, …)
  keeps working unchanged with the new inputs unset.
- Signing secrets are scoped only to the semantic-release step and must not be printed or persisted.
- Must not weaken the existing `submodules: false` + root-only init invariant.
- Must preserve minimum workflow permissions.
- Owner-approved: do not modify the shared submodule from FrontComposer or merge upstream without the
  Hexalith.Builds owner and Release Owner workflow.
- Filing the issue/story does not clear the FrontComposer release freeze; the accepted revision must be
  integrated and REL-3's gate must pass.

## Second required item — common release-freeze gate (all Hexalith modules)

Added 2026-07-15 by the approved
`sprint-change-proposal-2026-07-15-release-freeze-enforcement.md` (REL-4), on the Administrator's
directive that the freeze mechanism be common to all Hexalith modules. Every module publishing
through `domain-release.yml` shares the same unguarded `npx semantic-release` exposure FrontComposer
hit with v3.2.1/v3.2.2.

Required contract:

- `domain-release.yml` must refuse to run its Semantic Release step unless the **calling
  repository's** `HEXALITH_RELEASE_PUBLISH_ENABLED` configuration variable is exactly the string
  `true`, evaluated with an exact (case-sensitive, untrimmed) comparison in a shell step — not a
  GitHub-expression `==`, which compares case-insensitively.
- Missing or malformed values freeze publication with an explicit notice; the run concludes green
  (skip-not-fail) so frozen modules do not accumulate red runs.
- Called reusable workflows resolve `vars` from the caller's repository/organization, which is what
  makes one shared gate per-module controllable. The Hexalith.Builds owner must verify this
  resolution behavior on the current GitHub Actions platform during implementation. Fallback shape
  if verification fails: a required `publish-enabled` boolean input defaulting to `false`, computed
  by each caller from its own variable.
- Rollout is deliberately fail-closed for the whole ecosystem: when the gate lands, every consuming
  module is frozen until its owner sets the variable. The owner sets `true` on modules that should
  keep publishing at rollout time; FrontComposer's stays non-`true` until REL-3 completes.
- Documented hazard: repository-level variables shadow organization-level ones; an org-level `true`
  leaks into repos with no repo-level value. Frozen repos must carry an explicit repo-level value
  whenever an org-level value exists.

This item does not gate FrontComposer's own freeze — the REL-4 caller-side `freeze-guard` in
FrontComposer's `release.yml` enforces it immediately and remains as defense-in-depth. It is
independent of, though filed alongside, the signing-contract item above; the signing item remains
blocking for REL-3, while this item is required for ecosystem coverage.

## GOV-1 amendment — exact candidate and evaluator handoffs

Added 2026-07-19 by ratified FC-DEP-1 / architecture AD-13, AD-15, and AD-16. Issue 17 is not accepted
for GOV-1 until the Hexalith.Builds owner supplies one immutable 40-hex revision implementing all of
the following backward-compatible governed-mode contracts:

- `domain-ci.yml` accepts required governed inputs for the exact candidate SHA, active policy
  repository/commit/SHA-256, and expected evaluator-authorization digest; validates its actual
  `job.workflow_ref/job.workflow_sha`; evaluates the bounded static workflow/composite-action closure;
  and outputs the exact reusable/action provenance needed by the caller to create the single
  `dependency-release-handoff` artifact (`hexalith.dependency-release-handoff.v1`).
- `domain-release.yml` accepts required governed inputs `release-commit` (the authenticated
  `workflow_run.head_sha`), triggering CI run ID, active policy coordinates, handoff artifact name, and
  expected Release evaluator-authorization digest. Every checkout, prepare, seal, live verify,
  fallback, classify, and publish operation consumes `release-commit`; it validates actual reusable/
  action coordinates and never selects default-branch HEAD.
- The caller-side Release run uploads under `if: always()` exactly one
  `release-verification-handoff` artifact containing
  `hexalith.release-verification-handoff.v1`: authenticated CI repository/workflow/run ID/attempt/raw
  handoff hash, exact active-policy projection, original candidate, Release run ID/attempt/conclusion,
  version/tag/GitHub Release identity, sealed-manifest path/hash/seal, sorted asset name/hash/size rows,
  and authorized Release evaluator coordinates/digest. The post-release verifier re-downloads both
  handoffs and requires matching policy/candidate projections. Pre-manifest, failed, or partial runs
  preserve the CI/policy projection and use the closed null/empty representation for unavailable release
  fields rather than omitting the artifact.
- Every reusable workflow/action reference is literal-40-hex-pinned. Local composite actions are loaded
  from the exact reusable-workflow commit. Their complete static transitive `uses:` closure, including
  conditional entries and composite descendants, matches the active FrontComposer policy
  authorization and the handoff/manifest projection.
- The accepted revision is recorded here with the exact `domain-ci.yml`, `domain-release.yml`, and
  composite-action blob SHA-256 values before FrontComposer integration.

**GOV-1 completion gate:** while **Accepted revision** remains `pending`, FrontComposer may implement
local graph, semantic, policy, and fixture work, but GOV-1 Tasks 4/5, story completion, release
eligibility, and any REL-4 unfreeze remain blocked. FrontComposer must not edit the Builds submodule.
The bounded contingency below is not approved by GOV-1; using it requires a new dated Architect +
Release Owner decision with scope, expiry, migration trigger, and equivalent proofs.

## Bounded contingency

If the shared contract cannot land before a required release, stop. A thin FrontComposer-owned gated
workflow is permitted only by a new dated **Architect + Release Owner** decision that records equivalent
graph/policy/evaluator/two-handoff/exact-artifact proofs, scope, approvers, expiry/reopen trigger, and
migration back to Hexalith.Builds. No such contingency is authorized by GOV-1; this section is an
escalation path, not permission to resume G1 or create a permanent release fork.
