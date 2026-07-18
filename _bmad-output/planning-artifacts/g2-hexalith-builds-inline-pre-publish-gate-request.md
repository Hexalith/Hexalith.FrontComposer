# Required upstream dependency (G2 / BUILD-REL-1): opt-in governed NuGet release contract for the FR24 pre-publication gate

- **Raised by:** REL-2 on 2026-07-13; made mandatory by REL-3 on 2026-07-15; extended with the
  common release-freeze gate (second required item) by REL-4 on 2026-07-15
- **Target repository:** [Hexalith/Hexalith.Builds](https://github.com/Hexalith/Hexalith.Builds)
- **Target file:** `.github/workflows/domain-release.yml` (shared reusable workflow)
- **Status:** required/blocking — owner-approved issue or story must be filed before implementation
- **Suggested upstream story title:** "BUILD-REL-1: Add an opt-in governed NuGet release contract
  to Hexalith.Builds"
- **Upstream verification (2026-07-15):** a live search of Hexalith.Builds issues and pull requests
  found no matching issue or PR (only closed 2025 issues #1/#2 and dependabot/docs PRs); filing
  remained pending until 2026-07-18.
- **Upstream owner:** Release Owner (jpiquot) — filed under Release Owner directive on 2026-07-18
- **Issue/story URL:** <https://github.com/Hexalith/Hexalith.Builds/issues/17> (filed 2026-07-18;
  full two-item scope: opt-in governed release contract + common release-freeze gate)
- **Accepted revision:** pending
- **Release impact:** blocks the next FrontComposer NuGet or GitHub package release unless the Release
  Owner explicitly approves the bounded FrontComposer-owned gated-workflow contingency
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
    uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main
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

## Bounded contingency

If the shared contract cannot land before a required release, stop and obtain explicit Release Owner
approval for a thin FrontComposer-owned gated release workflow implementing the same exact-artifact
invariant. Record scope, approver, expiry/reopen trigger, and the migration back to Hexalith.Builds.
This is a bounded contingency, not permission to resume G1 or create a permanent release fork.
