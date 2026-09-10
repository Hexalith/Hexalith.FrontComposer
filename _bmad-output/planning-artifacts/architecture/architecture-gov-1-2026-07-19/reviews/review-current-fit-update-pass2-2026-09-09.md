# Reviewer Gate — current-technology / reality-check lens — pass 2 — 2026-09-09

**Subject:** `ARCHITECTURE-SPINE.md` SHA-256
`9dc9ded5670839f24a49ebc4499b6086ddf2590215a178f28311399a01244e5a`

**Repository baseline:** FrontComposer HEAD
`053b2008307d4e476c0d4329e6c47763c301d43e`; public GitHub.com repository.

## Verdict

**FAIL — one Critical liveness defect and one High platform contradiction remain.** Most of the
revised mechanism is implementable on the current platform: the pinned attestation action and its
minimal permissions are exact, direct reusable-workflow identity is available in OIDC/certificate
claims, a raw Actions artifact is a digest-bearing ZIP after the documented redirect, immutable
product and incident-evidence Releases are supported, and the NuGet normalization/signature rule
matches both current documentation and a live package. However, the fallback-approval PR can never
both bind an already-existing CI run and leave that run's candidate at live `main`; and AD-12
literally forbids following the redirect that GitHub's only raw artifact download endpoint always
returns.

The repository is not release-eligible today for additional, already-disclosed brownfield reasons:
the emergency-stop variable is live `true`, while `main` has neither a ruleset nor branch
protection. Those observations confirm the spine's Adoption/Deferred blockers and are not counted as
new architecture findings below.

## Findings

### FIT2-1 — Critical — fallback approval PR has no reachable valid state

**Affected rules:** AD-9 lines 271–300; AD-13 lines 413–433.

The fallback approval payload binds an exact `candidate` and an exact CI `run_id/run_attempt`, so it
cannot be authored until after that candidate has been pushed to `main` and CI has allocated the run
coordinates. AD-9 then requires the payload file and byte-identical approving review to merge through
protected `main`. That merge necessarily advances live `main` beyond the bound candidate. AD-13
requires the subsequently dispatched Release SHA, authenticated CI candidate, and a fresh API read of
live `main` all to be equal. The original candidate is therefore ineligible after the approval merge.
Putting the approval into a later candidate only repeats the cycle: that later candidate needs its own
future CI coordinates and another merge. No branch-protection or merge strategy supplies a fixed
point, and pre-authoring the record is impossible because GitHub allocates the run ID after the push.

The failure is fail-closed, but it removes the only publication path when the active policy truthfully
sets `attestation_capability: unsupported`; the fallback mechanism specified by the architecture is
therefore not implementable.

**Disposition:** **discuss / amend before implementation.** Keep the fallback exceptional and
same-candidate-bound, but obtain authority inside the same operator-dispatched Release run instead of
through a commit that advances `main`. One implementable shape is a closed fallback request in
`workflow_dispatch` inputs plus a byte-exact canonical approval JSON in the `production` deployment
approval comment for that same Release run. Authenticate the approver, `approved` state, environment,
comment, and run through the Actions review-history API; bind the current Release run and the already
selected CI handoff; use a verifier observation time because the response exposes no review timestamp;
and accept repeated API rows only when their projected bytes are identical. A live read of run
`34153315061` returned two byte-equivalent records with only `user`, `state`, `comment`, and
`environments`, matching GitHub's documented response shape. See GitHub's
[workflow-run review-history API](https://docs.github.com/en/rest/actions/workflow-runs#get-the-review-history-for-a-workflow-run)
and [deployment-review behavior](https://docs.github.com/en/actions/how-tos/managing-workflow-runs-and-deployments/managing-deployments/reviewing-deployments).

### FIT2-2 — High — AD-12's redirect ban prohibits the required raw artifact acquisition

**Affected rules:** AD-7 lines 207–218; AD-12 lines 381–391; AD-19 lines 865–868.

AD-7 and AD-19 require the protected publisher to retrieve the raw artifact ZIP through the
authenticated Actions REST archive endpoint. GitHub documents that endpoint as returning HTTP 302 to
a one-minute archive URL; the initial API response does not contain the ZIP. AD-12 simultaneously
forbids the protected closure from downloading a “redirect target.” Read literally and as a static
control, conforming code cannot follow the only supported transport and therefore cannot obtain the
publication candidate. This is not hypothetical: artifact `10046042003` returned metadata digest
`sha256:1e549aabe0506b01629433ad61c6ab14a5506f2ba943ed828fc487f51fe17c79`, and hashing the byte stream
obtained by following its archive endpoint produced the same digest. The mechanism works only by
violating the blanket wording. GitHub documents the 302, one-minute expiry, ZIP-only format, and
Actions-read permission in the
[Actions artifacts REST API](https://docs.github.com/en/rest/actions/artifacts#download-an-artifact).

**Disposition:** **autofix the spine.** Preserve the executable-content redirect ban, but add one
closed data-only exception for the authenticated artifact endpoint: accept exactly one HTTPS redirect
to GitHub's issued short-lived archive URL, never forward the API authorization header to a different
host, apply the streaming raw-byte cap while following it, and authenticate the completed ZIP against
the API digest before parsing. Continue to forbid redirected scripts, actions, installers,
configuration, packages used as executable input, and every redirect outside this transport.

## Confirmed current fit

- **Attestation action and permissions.** Tag `actions/attest@v4.2.0` resolves to the spine's exact
  `f7c74d28b9d84cb8768d0b8ca14a4bac6ef463e6`. Its `action.yml` uses Node 24 and exposes
  `subject-checksums`, `push-to-registry`, `create-storage-record`, `show-summary`, and `bundle-path`.
  The selected `push-to-registry: false` and `create-storage-record: false` path does not create a
  linked-artifact storage record, so `contents: read`, `id-token: write`, and `attestations: write`
  are sufficient and the explicit `artifact-metadata: write` prohibition is consistent. Primary
  sources: [pinned action commit](https://github.com/actions/attest/commit/f7c74d28b9d84cb8768d0b8ca14a4bac6ef463e6),
  [actions/attest](https://github.com/actions/attest), and GitHub's
  [binary attestation guidance](https://docs.github.com/en/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations).
- **Verifier and reusable identity.** Current `gh attestation verify` supports offline `--bundle`,
  exact repository, reusable signer workflow/digest, source ref/digest, predicate-type and
  self-hosted-runner denial, with JSON output containing the verified certificate and statement.
  GitHub explicitly identifies the called reusable as the signer, and reusable jobs expose
  `job_workflow_ref`/`job_workflow_sha`, run/attempt, source, runner, and environment claims. The
  spine correctly treats signed predicate content as signer-controlled and relies on verified
  certificate/result fields for platform identity. See the
  [CLI verifier reference](https://cli.github.com/manual/gh_attestation_verify),
  [OIDC reusable-workflow guidance](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-with-reusable-workflows),
  and [OIDC claim reference](https://docs.github.com/en/actions/reference/security/oidc).
- **Privilege split.** GitHub documents that permissions can only be maintained or reduced through a
  reusable-workflow chain, and that a callee job's environment supplies its environment secret rather
  than an `on.workflow_call` secret. AD-18/AD-19's caller ceiling, environment-scoped NuGet secret,
  ban on `secrets: inherit`, and direct protected publisher are feasible. See
  [reusable workflows](https://docs.github.com/en/actions/how-tos/reuse-automations/reuse-workflows).
- **Immutable Releases and incident evidence.** The live immutable-release setting is enabled, and
  release `v4.4.0` reports `immutable: true` with SHA-256 asset digests. GitHub locks the tag and assets
  at publication and recommends draft → attach all assets → publish, exactly matching AD-19. The same
  mechanism supports the separate incident-evidence prerelease; its title/notes/prerelease flag remain
  mutable platform metadata, but the evidence assets and tag are immutable, which is the contract's
  durable boundary. See [immutable releases](https://docs.github.com/en/code-security/concepts/supply-chain-security/immutable-releases).
- **NuGet version and signing semantics.** NuGet normalization removes leading zeroes, a zero fourth
  component, and build metadata, so AD-19's strict three-component lowercase subset plus normalized
  equality avoids repository identity aliases. In a live comparison for
  `Hexalith.FrontComposer.Schema` `4.4.0`, the GitHub asset was author-unsigned, the NuGet-served package
  verified successfully as repository-signed, the served archive added only root `.signature.p7s`,
  and every other member name and uncompressed byte sequence was identical. This validates the exact
  permitted-difference rule for the current feed/package. See NuGet's
  [version normalization](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning) and
  [signed-package reference](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference).
- **Ledger flow.** The two-PR ledger sign-off has no candidate-currentness loop because it follows the
  historical observation and authenticates each merged head/review independently. GitHub pull-request
  review objects expose review ID, user, state, body, commit ID, and `submitted_at`, so that projection
  is representable. It remains correctly implementation-ineligible until protected main exists. See
  the [pull-request review API](https://docs.github.com/en/rest/pulls/reviews).

## Confirmed brownfield blockers (not new findings)

- `HEXALITH_RELEASE_PUBLISH_ENABLED` returned literal `true`, created/updated
  `2026-08-06T13:31:29Z`; Adoption correctly calls this a Critical operational blocker and requires
  authenticated observation of literal `false` before release eligibility.
- `GET /rulesets` returned an empty array and `main.protected` returned `false`; the protected-main PR
  predicates used by fallback and ledger sign-off cannot run today. The spine already defers them and
  blocks the implementation gate, so this is honest implementation drift rather than another
  architecture defect.
- The live `production` environment has one required user reviewer, custom branch policy `main`, and
  `prevent_self_review: false`. The spine records the one-owner interim condition and requires
  self-review prevention when a second Release Owner exists.

## Incomplete lower-severity checks

- No `split-publication-v1` implementation or incident-preservation workflow exists yet, so the exact
  end-to-end runner behavior, hostile-candidate test, and immutable incident Release could not be
  exercised. The spine already makes these implementation-gate requirements.
- A fresh NuGet publication was not performed, so indexing/repository-signing propagation and bounded
  retry behavior were not timed. This does not invalidate the byte/signature rule verified against the
  existing `4.4.0` package.
- The pinned future owner verifier action does not yet exist, so its bundle-path fixtures and exact
  parser behavior remain implementation evidence rather than current repository evidence.

## Method and evidence

- Read-only repository/API inspection plus local hashing/parsing; no source, spine, memlog, repository
  setting, Release, package, branch, or submodule was changed.
- `sha256sum ARCHITECTURE-SPINE.md` bound this report to the subject hash above.
- `uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace ...` returned
  `ok: true`, zero deterministic findings.
- Relevant API reads covered the publish-gate variable, `production` environment and branch policy,
  workflow-run approvals, rulesets/main protection, artifact metadata/download bytes, immutable-release
  setting, release `v4.4.0`, and the pinned `actions/attest` ref/content.

## Finding counts

Critical: **1** · High: **1** · Medium: **0** · Low: **0**.
