# Hexalith.FrontComposer — Deployment / Release Guide

> **Generated:** 2026-06-02 · deep scan. **Updated 2026-07-18 (REL-4/REL-3 implementation)** after live
> v3.2.1/v3.2.2 evidence proved the G1 post-publication workflow is not an FR24 publication gate.
> The REL-4 freeze guard is now implemented in `release.yml` (first live frozen-run verification
> pending CI).
> FrontComposer ships as **NuGet packages**, not a deployed service.
> "Deployment" here means the automated **semantic-release → NuGet** pipeline
> ([.releaserc.json](.releaserc.json)) driven by the shared reusable **Hexalith.Builds** workflows.

## What gets shipped

FrontComposer is a library/tooling product. Its release artifacts are **NuGet packages** (`.nupkg`) +
**symbol packages** (`.snupkg`) published to nuget.org. A compliant release must also carry its durable
**release-evidence** bundle on the initial GitHub Release. The expected package set is pinned in
[eng/release-package-inventory.json](eng/release-package-inventory.json) and must be validated against
the exact artifacts intended for publication.

The explicit package set contains `Cli`, `Contracts`, `Contracts.UI`, `Mcp`, `Schema`, `Shell`,
`SourceTools`, and `Testing`; every package requires a symbol package. `AppHost` and the combined `UI`
host are explicit non-package exceptions. The Contracts.UI/ownership split is binary-breaking from
`v1.12.0`, so the Release Owner approved `2.0.0`; the release commit range must carry a Conventional
Commit breaking-change signal. See the published 1.12-to-2.0 migration guide.

There are **no Dockerfiles / container images** for FrontComposer's own projects (unlike the `Hexalith.EventStore` submodule). The only orchestration is the local `samples/Counter` Aspire AppHost, which is a sample — not a deployed artifact.

## CI/CD model and current release freeze (REL-3, 2026-07-15)

FrontComposer's primary CI/CD now delegates to the same reusable **Hexalith.Builds** workflows as
**Hexalith.Tenants**, so every Hexalith module shares one CI/CD operating model. REL-3 (2026-07-18)
moved FR24 authorization ahead of publication: the exact-artifact chain runs inside semantic-release's
`prepareCmd`/`publishCmd`, and the supplemental workflow only verifies published bytes. Publication
remains frozen (REL-4 gate) until a real release proves the chain end-to-end and the Release Owner
enables it (REL-5):

| Layer | Home | Covers |
|---|---|---|
| **CI** | `ci.yml` → reusable `domain-ci.yml` with `run-consumer-validation: true` + `scripts/` | build + Tier 1 unit tests + coverage evidence; FR24 AC1 inventory + AC6 consumer-boundary validation |
| **Quality (supplemental)** | `quality.yml` | FrontComposer-only gates the reusable does not run — Gate 1 (Contracts netstandard2.0), Gate 2a (CLI smoke), Gate 2b (Governance), Gate 2c (Contract pacts + validators + stale-pact-diff), Gate 2d (docs), trait-filtered test lanes, quarantine/CI-duration telemetry, Playwright a11y/visual |
| **Release (publish)** | `release.yml` → `freeze-guard` job → reusable `domain-release.yml` via `workflow_run` | REL-4 fail-closed freeze gate (default frozen), then semantic-release pack + publish |
| **Release evidence (REL-3)** | `release-evidence.yml` (`workflow_run` after `Release`, any conclusion) | independent post-publication verification: downloads NuGet/GitHub bytes, compares against the sealed manifest, fails on divergence; authorization happens pre-publication in `release_prepublish.py` |

### Release freeze control (REL-4 implemented 2026-07-18; live CI verification pending)

`REL-4`'s fail-closed release-freeze control is implemented: `release.yml` now contains the
`freeze-guard` job gating the `release` job, so publication is technically disabled by default.
Governance tests `ReleaseWorkflow_PublishFreezeGate_IsFailClosedByDefault` and
`Workflows_HaveNoPublishPathOutsideGatedReleaseWorkflow` pin the gate. The first CI-authoritative
frozen Release run URL (freeze-guard success, release-job skip, no publication side effect) is
recorded in the REL-4 story after the next push-CI success on `main`. The active runbook:

- A `freeze-guard` job evaluates the repository/organization Actions variable
  **`HEXALITH_RELEASE_PUBLISH_ENABLED`** with an **exact POSIX string comparison in bash**
  (`[ "$VALUE" = "true" ]`). The `release` job runs only when the guard's `publish-enabled` output
  is `true`, in addition to the existing CI-success + push-event conditions.
- **Publication is disabled by default.** A missing variable, any value other than the exact string
  `true` (including `True`, `TRUE`, `1`, `yes`, padded whitespace), or a failed/skipped guard keeps
  the freeze. The exact match happens in bash because GitHub-expression `==` is case-insensitive.
- **What a frozen run looks like:** the Release run concludes **green**; the `freeze-guard` emits a
  `::notice` and step-summary line stating the freeze; the `release` job is **skipped**; no
  semantic-release, NuGet, tag, changelog, or GitHub Release side effect occurs.
- **Custody:** only the Release Owner changes the variable (repository/organization settings). Do
  not set it to `true` before REL-3's exact-artifact gate passes on a real release.
- **Shadowing hazard:** repo-level variables override org-level ones — an org-level `true` leaks
  into repos with no repo-level value, so FrontComposer must carry an explicit repo-level value
  whenever an org-level value exists.
- **Residual risk:** a human with `NUGET_API_KEY` custody running semantic-release locally bypasses
  any workflow control; API-key custody remains a Release Owner responsibility under FR24.
- **Removal/re-scope:** only when REL-3 is operational with passing real-release evidence; the
  variable then remains as the standing Release Owner freeze switch. Governance tests
  (`CiGovernanceTests`) pin the gate wiring, the exact-match comparison, the single publish path,
  and the removal-condition marker.
- The same gate contract is a required common Hexalith.Builds item so all Hexalith modules share
  the identical default-frozen control (see
  `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md`).

### CI workflows ([.github/workflows/](.github/workflows/))

| Workflow | Trigger | Purpose |
|---|---|---|
| `commitlint.yml` | PR **and** push to `main` | reusable `commitlint.yml@main` (Tenants parity; push guards direct-to-main commits) |
| `ci.yml` | push / PR to `main` | reusable `domain-ci.yml@main`: build (Release, `-warnaserror`) · `scripts/` consumer validation · Tier 1 unit tests + coverage |
| `quality.yml` | push / PR to `main` | supplemental FrontComposer gates (Gate 1/2a/2b/2c/2d, trait-filtered test lanes, telemetry, a11y/visual) — **CI-authoritative for these gates** |
| `release.yml` | `workflow_run` after CI success (push) | REL-4 `freeze-guard` (default frozen) → reusable `domain-release.yml@main`: semantic-release publish (no container images) |
| `release-evidence.yml` | `workflow_run` after `Release` completes (any conclusion) | REL-3 independent verification: downloaded NuGet/GitHub byte comparison against the sealed manifest |
| `nightly.yml` | schedule | nightly checks (incl. skill-corpus prompt benchmark) |
| `ide-parity-revalidation.yml` | schedule / drift | revalidates `docs/ide-parity-matrix.json` against IDE behavior |
| `mutation-property-nightly.yml` | schedule | Stryker mutation + FsCheck property suites |
| `flaky-test-governance.yml` | schedule/governance | flaky-test tracking |
| `quarantine-governance-nightly.yml` | schedule | quarantined-test governance |

Every workflow uses **root-declared submodule init only** (`submodules: false` +
`initialize-build@main`); never recursive nested submodules.

CI build/test commands are the source of truth for local dev — see [development-guide.md](./development-guide.md).

## Versioning — semantic-release + Conventional Commits

The current configuration automatically invokes semantic-release from qualifying commits on `main`
([.releaserc.json](.releaserc.json)); branch `main`, tag format `v${version}`. **Governance freeze:** do
not run a publish-capable release until REL-3's pre-publication gate is implemented and the Release
Owner authorizes it.

| Commit type | Version bump |
|---|---|
| `feat:` | minor |
| `fix:` / `perf:` | patch |
| `feat!:` or `BREAKING CHANGE:` footer | major |
| `docs:` / `refactor:` / `test:` / `chore:` / `build:` / `ci:` / `style:` | none |

Plugins: `commit-analyzer` → `release-notes-generator` → `changelog` → `exec` (pack/publish) → `github` (release + assets) → `git` (commits `CHANGELOG.md` with `chore(release): ${version} [skip ci]`).

## The release pipeline — behavior

### Publish path (`release.yml` → freeze-guard → reusable `domain-release.yml`) — REL-3 pre-publication gate

`release.yml` runs from **`workflow_run` after a successful CI push** (guarded
`conclusion == 'success' && event == 'push'`, so PR/scheduled CI runs never release, and a failed CI
can never publish). The REL-4 `freeze-guard` job then gates the `release` job (default frozen; see
above). When enabled, it delegates to `domain-release.yml@main` with `test-projects: ''` (CI already
gated the same head). The reusable runs `npm ci` → `npm audit signatures` → build →
`npx semantic-release`, which drives this repo's `.releaserc.json`:

- **`prepareCmd`:** `python3 eng/release_prepublish.py prepare --version ${nextRelease.version}` —
  the repository-owned FR24 exact-artifact chain, fail-closed at every phase **before any
  publication side effect**: pack once (`scripts/pack-release-packages.py`) → inventory validation →
  7-project release test lane → package-consumer validation (Contracts-only + Shell/UI boundaries)
  → SBOM + required-symbol check → sign + RFC 3161 timestamp the **exact** candidate `.nupkg` into
  `nupkgs-signed/` and verify (`dotnet nuget verify --all`; **missing credentials, unsigned
  packages, invalid chains, or missing timestamps abort — no record-and-proceed**) → benchmark
  candidate evidence → checksums → `prepare-manifest` (attestation binding per AC18) →
  `seal-manifest` → `verify-manifest` → `classify-release --require-publishable` (only
  `classification=ready` + `publish_authorized=true` lets semantic-release continue).
- **`publishCmd`:** `python3 eng/release_prepublish.py publish --version ${nextRelease.version}` —
  re-verifies the sealed manifest and readiness immediately before pushing, audits every
  manifest row (path shape + exact sha256), then pushes **only the manifest-authorized signed
  bytes** (`nupkgs-signed/*.nupkg` + matching `nupkgs/*.snupkg`) with no `--skip-duplicate`; any
  divergence writes a typed `partial-publish-incident.json` and fails the release (AC14).
- **`@semantic-release/github`:** attaches the signed packages, symbols, and the full
  `release-evidence/` chain as **durable assets at initial release creation** (AC12).

Attestation (AC18): provenance attestation over the exact signed candidates requires the upstream
BUILD-REL-1 governed contract (candidate phase + `attest-build-provenance` in the shared workflow);
until it lands, classification fails closed unless the Release Owner seals the bounded
`approved-unsupported` fallback record — so the pipeline cannot silently publish unattested bytes.

### Independent post-publication verification (`release-evidence.yml`)

Refactored by REL-3 from the withdrawn G1 reconstructed-evidence model into **download-and-compare
verification only** — it never rebuilds, repacks, re-signs, or attests. It runs from `workflow_run`
when **Release completes with any conclusion** (success, failure, or cancellation — AC19):

1. Resolves the release tag (parent-aware; a run with no tag and no side effects is a clean green
   no-op — the expected shape for frozen REL-4 runs and non-releasing pushes).
2. Downloads the GitHub Release assets; **missing durable evidence assets on the release itself
   fail the run** (a 30-day Actions artifact alone is non-compliant).
3. Verifies the sealed manifest over the downloaded bytes, enforces the retroactive-authorization
   ban (the downloaded `release-readiness.json` must already show `publish_authorized=true` — the
   verifier never runs `classify-release`), downloads the same versions from nuget.org, and
   three-way compares sha256 (NuGet bytes ⇄ GitHub assets ⇄ sealed manifest).
4. `dotnet nuget verify --all` over the downloaded NuGet bytes with the public trust bundle only.
5. Any divergence (packages missing on one side, altered assets, unsigned bytes) writes a typed
   partial-publication incident and fails; no compliant ledger disposition is possible until
   owner-led reconciliation (AC14/AC19).
6. Emits `ledger-record.json` — the machine-readable disposition proposal the Release Owner uses to
   update the [REL-AI-1 ledger](../implementation-artifacts/rel-ai-1-release-evidence-ledger.md)
   (the workflow never commits to the repository).

### Required artifact invariant (implemented)

```text
Pack once → validate inventory/tests/consumers → generate symbols/SBOM
→ sign + RFC 3161 timestamp exact .nupkg → verify → attest (or sealed owner fallback)
→ checksum all artifacts/evidence → seal + verify manifest
→ classify-release --require-publishable → publish those same authorized bytes
→ verify downloaded NuGet/GitHub bytes
```

The publish freeze clears only when this path proves itself on a real release: the upstream
BUILD-REL-1 governed contract (or the owner-approved bounded contingency) supplies signing
secrets/timestamp input/attestation to semantic-release, the Release Owner enables
`HEXALITH_RELEASE_PUBLISH_ENABLED`, and downloaded-byte verification passes (REL-5 owns that
enablement and REL-AI-1 closure).

### FR24 status — why `REL-AI-1` stays open

| Target | AC | Status |
|---|---|---|
| Package-consumer validation (Contracts-only vs Shell/UI boundaries) | AC1/AC6 | **Implemented** — `scripts/` trio, run by `domain-ci.yml` `run-consumer-validation: true` and re-run against the exact candidates in `release_prepublish.py prepare` |
| SBOM + checksums + sealed/verified manifest | AC3/AC4–5/AC8 | **Implemented pre-publication** (`release_prepublish.py prepare`); v3.2.1/v3.2.2 manifests remain invalid historical records |
| Certificate signing + RFC 3161 timestamp of the exact publishable bytes | AC2 | **Implemented fail-closed in `prepare`**; blocked until REL-5 provisions the production signing identity (published v3.2.1/v3.2.2 packages remain unsigned) |
| Attestation over exact signed candidates | AC9/AC18 | **Enforced at classification** — requires the upstream BUILD-REL-1 governed contract or the sealed owner-approved fallback; fails closed otherwise |
| Exact-artifact pre-publish gate (`--require-publishable`) | REL-3 | **Implemented** — `prepareCmd` ends in `classify-release --require-publishable`; `publishCmd` re-verifies before pushing |
| Durable initial GitHub Release evidence | AC8/AC12 | **Implemented** — `@semantic-release/github` attaches packages, symbols, and the evidence chain at initial release creation; the verifier fails when they are absent |
| Published-byte verification | AC13/AC19 | **Implemented** — `release-evidence.yml` downloads NuGet + GitHub bytes and compares against the sealed manifest on every Release completion |
| Release Owner closure evidence | REL-AI-1 | **Pending (REL-5)** — only a real release authorized before publication and verified after publication can close the action; no such release exists yet |

Affected releases and their authoritative run/evidence links are recorded in the
[REL-AI-1 release-evidence ledger](../implementation-artifacts/rel-ai-1-release-evidence-ledger.md).

## Required release environment

| Variable | Scope | Purpose |
|---|---|---|
| `NUGET_API_KEY` | secret (forwarded to `domain-release.yml`) | nuget.org push |
| `GITHUB_TOKEN` | provided | GitHub Release + evidence asset upload |
| `NUGET_SIGNING_CERTIFICATE_BASE64` / `NUGET_SIGNING_CERTIFICATE_PASSWORD` | secret (required for FrontComposer release) | consumed by `release_prepublish.py prepare` to sign the exact publishable packages; must be forwarded to semantic-release by the upstream BUILD-REL-1 governed contract (absent credentials abort preparation) |
| `NUGET_SIGNING_TIMESTAMPER` | reusable-workflow input / repository configuration (required) | approved RFC 3161 timestamp authority for the exact publishable packages |

### Non-publishing signing validation

The pre-publication chain signs the exact candidates fail-closed; the recipe below exists so the
mechanics can be validated locally (`release_prepublish.py prepare --non-publishing`) without a
production identity. It is not approval for a production author identity. The Release Owner must select and
approve the production signing identity, trust model, storage/rotation plan, and timestamp authority.
A bare self-signed leaf does not work with `dotnet nuget sign` (`NU3018 InvalidBasicConstraints`).

> **Why a chain + why the PFX must embed the root:** on Linux, `dotnet nuget sign`/`verify` trust
> code-signing roots **only** via the SDK's own bundle `…/sdk/<ver>/trustedroots/codesignctl.pem`,
> not the OS trust store, `SSL_CERT_FILE`, or the .NET user cert stores. The workflow recovers the
> issuing CA from the PFX chain (`openssl pkcs12 -cacerts`) and appends it to that bundle **before
> signing** — so the PFX must be exported **with** the root (`-certfile root.crt`), or the run fails
> with a clear error.

```bash
# 1. self-signed ROOT CA (CA:TRUE, keyCertSign)
openssl req -newkey rsa:3072 -nodes -keyout root.key -out root.csr \
  -subj "/CN=Hexalith FrontComposer Release Evidence Root"
openssl x509 -req -in root.csr -signkey root.key -days 3650 -sha256 -out root.crt \
  -extfile <(printf 'basicConstraints=critical,CA:TRUE\nkeyUsage=critical,keyCertSign,cRLSign\nsubjectKeyIdentifier=hash\n')

# 2. code-signing LEAF issued by the root (CA:FALSE, EKU=codeSigning)
openssl req -newkey rsa:3072 -nodes -keyout leaf.key -out leaf.csr \
  -subj "/CN=Hexalith FrontComposer Release Evidence"
openssl x509 -req -in leaf.csr -CA root.crt -CAkey root.key -CAcreateserial -days 825 -sha256 -out leaf.crt \
  -extfile <(printf 'basicConstraints=critical,CA:FALSE\nkeyUsage=critical,digitalSignature\nextendedKeyUsage=critical,codeSigning\nsubjectKeyIdentifier=hash\nauthorityKeyIdentifier=keyid,issuer\n')

# 3. PFX = leaf key + leaf cert + root chain  (the -certfile root.crt is REQUIRED)
openssl pkcs12 -export -out signing-cert.pfx -inkey leaf.key -in leaf.crt -certfile root.crt \
  -passout pass:'CHOOSE_A_PASSWORD'

# 4. provision the two secrets (base64 has NO newlines: -w0)
base64 -w0 signing-cert.pfx | gh secret set NUGET_SIGNING_CERTIFICATE_BASE64 --repo <org>/<repo>
gh secret set NUGET_SIGNING_CERTIFICATE_PASSWORD --repo <org>/<repo> --body 'CHOOSE_A_PASSWORD'
rm -f signing-cert.pfx leaf.key root.key   # keep root.key offline if you plan to re-issue the leaf
```

In REL-3, equivalent validation must happen before publication against the exact candidate paths. A
self-signed test chain does not authorize a production release. Use the Release Owner-approved public
or organizational signing service/certificate for published packages.

## Consuming the packages

Downstream apps add the FrontComposer NuGet packages, annotate domain types with `[Projection]`/`[Command]`, and call `AddHexalithFrontComposerQuickstart()` + `AddHexalithDomain<TMarker>()` + `AddHexalithEventStore(...)` (see [architecture.md](./architecture.md) §4). To stand up the MCP endpoint, register the fail-closed gates and call `AddFrontComposerMcp(...)` + `MapFrontComposerMcp()` (see [api-contracts.md](./api-contracts.md) §2).

UI/rendering adopters reference `Hexalith.FrontComposer.Contracts.UI`; kernel-only contract or analyzer
consumers do not. The `scripts/validate-consumer-package-references.py` CI check proves both boundaries:
a **Contracts-only** consumer never inherits Blazor/Fluent/Fluxor runtime deps (kernel-split invariant),
while a **Shell/UI** consumer composes the full bootstrap surface.

## Pre-release checklist

1. Conventional-commit messages on `main` (drives the version bump).
2. Green CI: `ci.yml` (build + consumer validation + unit tests) **and** `quality.yml` (Gates 1–2, Governance + Contract, docs, unit+bUnit, a11y/visual).
3. No stale pact diff; public-API baseline updated intentionally if the Testing surface changed.
4. `eng/release-package-inventory.json` matches the packages you intend to ship.
5. Confirm REL-3 is implemented and the Hexalith.Builds signing contract (or explicitly approved bounded contingency) is recorded. Until then, **stop: publication is frozen**.
6. Provision the Release Owner-approved signing identity, password, and RFC 3161 timestamp authority; prove secret redaction and non-publishing fail-closed paths.
7. Require the pre-publication bundle to report a valid sealed manifest, `classification=ready`, and `publish_authorized=true` for the exact candidate paths.
8. Release Owner authorizes the real release; NuGet and the initial GitHub Release receive only those paths plus durable evidence.
9. Download NuGet/GitHub assets, verify signatures and hashes against the sealed manifest, record any partial-publication incident, and update the historical ledger.
10. Close `REL-AI-1` only when all exact-artifact and durable-evidence criteria pass.
