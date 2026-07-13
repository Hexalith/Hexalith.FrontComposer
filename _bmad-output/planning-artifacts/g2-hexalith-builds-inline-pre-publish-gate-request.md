# Upstream request (G2): opt-in inline pre-publish gate in Hexalith.Builds `domain-release.yml`

- **Raised by:** REL-2 (Align FrontComposer CI/CD With Tenants), 2026-07-13
- **Target repository:** [Hexalith/Hexalith.Builds](https://github.com/Hexalith/Hexalith.Builds)
- **Target file:** `.github/workflows/domain-release.yml` (shared reusable workflow)
- **Status:** proposed — **owner approval required** before any Hexalith.Builds change
- **This repo does NOT implement G2.** Per REL-2 decision D7, `references/Hexalith.Builds` is a shared
  `@main` submodule consumed by every Hexalith module and must never be edited from FrontComposer.
  This document is the artifact that raises the request; filing the issue/PR upstream is a
  Release-Owner action.

## Why

FrontComposer's FR24 release evidence runs under the **G1** posture (approved 2026-07-13): publish
proceeds under the reusable `domain-release.yml`; the supplemental `.github/workflows/release-evidence.yml`
produces the evidence bundle **after** publish and **fails closed on the next release** if prior evidence
is missing/invalid. G1 is auditable and shippable with **no submodule change**, but it has one gap: a bad
release can publish **once** before the next run catches it, because the reusable `domain-release.yml`
exposes no hook to run `classify-release --require-publishable` **before** `dotnet nuget push`.

**G2** closes that gap by upstreaming an opt-in pre-publish gate into the shared reusable so
`classify-release --require-publishable` (and optional signing/SBOM) can block **before** any
irreversible publish side effect — for every Hexalith module, not just FrontComposer.

## Requested change (opt-in, backward-compatible)

Add optional inputs to `domain-release.yml` (all default off/empty, so existing callers are unaffected):

| Input | Type | Default | Behavior |
|---|---|---|---|
| `pre-publish-command` | string | `''` | Shell command run **after build, before `npx semantic-release`**. Non-zero exit **fails the release before publish**. |
| `pre-publish-working-directory` | string | `.` | Working directory for `pre-publish-command`. |

Optional (nice-to-have, can be a follow-up): `sbom`, `sign`, and `classify` boolean inputs that wire the
CycloneDX/`dotnet nuget sign`/`classify-release --require-publishable` steps directly, so callers do not
have to re-implement them in a `pre-publish-command`.

### Caller shape once G2 lands (FrontComposer)

```yaml
jobs:
  release:
    uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main
    with:
      solution: Hexalith.FrontComposer.slnx
      test-projects: ''
      pre-publish-command: >-
        python3 scripts/pack-release-packages.py ./nupkgs ${{ '${VERSION}' }} &&
        python3 eng/release_evidence.py classify-release --require-publishable ...
    secrets:
      NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
```

When G2 is available, FrontComposer would move the `classify-release --require-publishable` gate into
`pre-publish-command` and drop the "advisory at publish" caveat from `release-evidence.yml`, upgrading
the posture from **G1 (post-publish + next-release fail-closed)** to **G2 (inline pre-publish gate)**.

## Constraints

- Must be **opt-in and backward-compatible** — every current caller (Tenants, Parties, EventStore, …)
  keeps working unchanged with the new inputs unset.
- Must not weaken the existing `submodules: false` + root-only init invariant.
- Owner-approved: this is a shared-infra change; do not merge without Release-Owner sign-off.

## Rejected alternative (G3)

Keep a thin owned gated release job in FrontComposer instead of the reusable — **rejected**, because it
defeats the REL-2 goal of one shared CI/CD operating model across Hexalith modules.
