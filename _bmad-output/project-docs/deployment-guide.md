# Hexalith.FrontComposer — Deployment / Release Guide

> **Generated:** 2026-06-02 · deep scan. **Updated 2026-07-13 (REL-2)** for the Tenants-aligned
> reusable CI/CD model. FrontComposer ships as **NuGet packages**, not a deployed service.
> "Deployment" here means the automated **semantic-release → NuGet** pipeline
> ([.releaserc.json](.releaserc.json)) driven by the shared reusable **Hexalith.Builds** workflows.

## What gets shipped

FrontComposer is a library/tooling product. Its release artifacts are **NuGet packages** (`.nupkg`) + **symbol packages** (`.snupkg`) published to nuget.org, plus a GitHub Release carrying those packages and a **release-evidence** bundle. The expected package set is pinned in [eng/release-package-inventory.json](eng/release-package-inventory.json) and verified during CI (consumer validation) and release (evidence).

The explicit package set contains `Cli`, `Contracts`, `Contracts.UI`, `Mcp`, `Schema`, `Shell`,
`SourceTools`, and `Testing`; every package requires a symbol package. `AppHost` and the combined `UI`
host are explicit non-package exceptions. The Contracts.UI/ownership split is binary-breaking from
`v1.12.0`, so the Release Owner approved `2.0.0`; the release commit range must carry a Conventional
Commit breaking-change signal. See the published 1.12-to-2.0 migration guide.

There are **no Dockerfiles / container images** for FrontComposer's own projects (unlike the `Hexalith.EventStore` submodule). The only orchestration is the local `samples/Counter` Aspire AppHost, which is a sample — not a deployed artifact.

## CI/CD model (REL-2, 2026-07-13) — one shared operating model

FrontComposer's primary CI/CD now delegates to the same reusable **Hexalith.Builds** workflows as
**Hexalith.Tenants**, so every Hexalith module shares one CI/CD operating model. The FR24 release
evidence obligations use **3-layer split-homing**, because the shared reusable `domain-release.yml`
publishes via semantic-release and exposes **no evidence hook** (and is a `@main` submodule this repo
must not edit):

| Layer | Home | Covers |
|---|---|---|
| **CI** | `ci.yml` → reusable `domain-ci.yml` with `run-consumer-validation: true` + `scripts/` | build + Tier 1 unit tests + coverage evidence; FR24 AC1 inventory + AC6 consumer-boundary validation |
| **Quality (supplemental)** | `quality.yml` | FrontComposer-only gates the reusable does not run — Gate 1 (Contracts netstandard2.0), Gate 2a (CLI smoke), Gate 2b (Governance), Gate 2c (Contract pacts + validators + stale-pact-diff), Gate 2d (docs), trait-filtered test lanes, quarantine/CI-duration telemetry, Playwright a11y/visual |
| **Release (publish)** | `release.yml` → reusable `domain-release.yml` via `workflow_run` | semantic-release pack + publish only |
| **Release evidence (FR24)** | `release-evidence.yml` (`workflow_run` on CI success) | AC2 signing, AC3 SBOM+checksums+sealed manifest, AC4–5 classify-release, AC8 evidence assets, AC9 attestation |

### CI workflows ([.github/workflows/](.github/workflows/))

| Workflow | Trigger | Purpose |
|---|---|---|
| `commitlint.yml` | PR **and** push to `main` | reusable `commitlint.yml@main` (Tenants parity; push guards direct-to-main commits) |
| `ci.yml` | push / PR to `main` | reusable `domain-ci.yml@main`: build (Release, `-warnaserror`) · `scripts/` consumer validation · Tier 1 unit tests + coverage |
| `quality.yml` | push / PR to `main` | supplemental FrontComposer gates (Gate 1/2a/2b/2c/2d, trait-filtered test lanes, telemetry, a11y/visual) — **CI-authoritative for these gates** |
| `release.yml` | `workflow_run` after CI success (push) | reusable `domain-release.yml@main`: semantic-release publish (no container images) |
| `release-evidence.yml` | `workflow_run` after CI success (push) | FR24 evidence bundle under **G1** (signing, SBOM, checksums, sealed manifest, classify-release, attestation, evidence assets) |
| `nightly.yml` | schedule | nightly checks (incl. skill-corpus prompt benchmark) |
| `ide-parity-revalidation.yml` | schedule / drift | revalidates `docs/ide-parity-matrix.json` against IDE behavior |
| `mutation-property-nightly.yml` | schedule | Stryker mutation + FsCheck property suites |
| `flaky-test-governance.yml` | schedule/governance | flaky-test tracking |
| `quarantine-governance-nightly.yml` | schedule | quarantined-test governance |

Every workflow uses **root-declared submodule init only** (`submodules: false` +
`initialize-build@main`); never recursive nested submodules.

CI build/test commands are the source of truth for local dev — see [development-guide.md](./development-guide.md).

## Versioning — semantic-release + Conventional Commits

Releases are fully automated from commit messages on `main` ([.releaserc.json](.releaserc.json)). Branch: `main`; tag format `v${version}`.

| Commit type | Version bump |
|---|---|
| `feat:` | minor |
| `fix:` / `perf:` | patch |
| `feat!:` or `BREAKING CHANGE:` footer | major |
| `docs:` / `refactor:` / `test:` / `chore:` / `build:` / `ci:` / `style:` | none |

Plugins: `commit-analyzer` → `release-notes-generator` → `changelog` → `exec` (pack/publish) → `github` (release + assets) → `git` (commits `CHANGELOG.md` with `chore(release): ${version} [skip ci]`).

## The release pipeline — behavior

### Publish (`release.yml` → reusable `domain-release.yml`)

`release.yml` runs from **`workflow_run` after a successful CI push** (guarded
`conclusion == 'success' && event == 'push'`, so PR/scheduled CI runs never release, and a failed CI
can never publish). It delegates to `domain-release.yml@main` with `test-projects: ''` (CI already
gated the same head — the release path does not duplicate test compute). The reusable runs
`npm ci` → `npm audit signatures` → build → `npx semantic-release`, which drives this repo's
`.releaserc.json`:

- **`prepareCmd`:** `python3 eng/pack_release_packages.py --version ${nextRelease.version} --output ./nupkgs` — packs the inventory package set (`.nupkg` + `.snupkg`).
- **`publishCmd`:** `dotnet nuget push ./nupkgs/*.nupkg` then `./nupkgs/*.snupkg` to nuget.org with `$NUGET_API_KEY --skip-duplicate`.

### FR24 release evidence (`release-evidence.yml`, G1)

Re-homed from the bespoke `release.yml` (REL-1) into a supplemental workflow because the reusable
`domain-release.yml` has no evidence hook. It runs from `workflow_run` after CI success, resolves the
release tag pointing at the commit, and over a **deterministic re-pack** of the inventory produces:

1. `release_evidence.py test-results` (`release-evidence/test-results.json`) — release tests re-run excluding `Category=Quarantined`.
2. `release_evidence.py inventory` (`package-inventory.json`) → `actions/attest-build-provenance@v4` over the re-packed `.nupkg` (AC9).
3. CycloneDX **SBOM** (`sbom.json`), then — when the signing cert secret is provisioned — the re-packed `.nupkg` are signed into **`nupkgs-signed/`** (`dotnet nuget sign`, RFC 3161 timestamp) and verified (`dotnet nuget verify --all -v normal`); the path-sanitized transcript `signing-verification.txt` feeds `prepare-manifest`, so each package's `signing_status`/`timestamp_status` (**both blocking checks**) become `verified`. When absent, a **blocking readiness reason** is recorded (AC2). See [Provisioning the signing certificate](#provisioning-the-signing-certificate).
4. **LLM benchmark candidate evidence** (`llm_benchmark.py` validate-prompt-set → budget-status → run-benchmark, offline / **no provider spend**) → `benchmark-summary.json`, whose hash is bound into the manifest (a required field; the pass/fail benchmark *gate* remains a nightly concern).
5. `checksums` (over `nupkgs-signed/`) → `prepare-manifest` (`--signing-verification`, `--benchmark-summary-hash`) → `seal-manifest` → `verify-manifest` — the sealed manifest binds commit SHA / tag / run-id / workflow-ref / package-set fingerprint / version / sbom hash / benchmark summary hash.
6. `classify-release` **without** `--require-publishable` (G1 = advisory for the release that just published).
7. `gh release upload <tag> release-evidence/*.json` + `upload-artifact` archives `release-evidence/**` (30-day retention).

**Gating posture G1 (approved 2026-07-13):** publish proceeds under the reusable workflow; the evidence
workflow's core steps (test-results, inventory, verify-manifest, classify-release) are **real gating
steps** (not best-effort), so missing/invalid evidence fails the workflow and **fails closed on the next
release**. **G2** — an opt-in inline pre-publish gate (`classify-release --require-publishable` before
`dotnet nuget push`) — requires upstreaming new inputs into Hexalith.Builds `domain-release.yml` and is a
**separate owner-approved follow-up**, tracked in
[g2-hexalith-builds-inline-pre-publish-gate-request.md](../planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md). It is **not** implemented in this repo.

### FR24 status — why `REL-AI-1` stays open

| Target | AC | Status |
|---|---|---|
| Package-consumer validation (Contracts-only vs Shell/UI boundaries) | AC1/AC6 | **Implemented** — `scripts/` trio, run by `domain-ci.yml` `run-consumer-validation: true` |
| SBOM + checksums + sealed manifest + classify-release + evidence assets | AC3/AC4–5/AC8 | **Implemented (G1)** in `release-evidence.yml` |
| Certificate signing + RFC 3161 timestamp | AC2 | **Wired end-to-end, secret-gated** — provision a code-signing **chain** as `NUGET_SIGNING_CERTIFICATE_BASE64`/`_PASSWORD` ([recipe below](#provisioning-the-signing-certificate)) to activate; signing/timestamp then populate the sealed manifest's blocking `signing_status`/`timestamp_status`. Unsigned runs record a blocking readiness reason |
| Attestation generated before readiness claimed | AC9 | **Implemented** — `attest-build-provenance` over re-packed `.nupkg` |
| Inline pre-publish gate (`--require-publishable` before push) | — (G2) | **Deferred** — upstream Hexalith.Builds follow-up |
| Release Owner records evidence paths from a real run | AC8/AC12 | **Pending** — a real release must run `release-evidence.yml` once and its paths be recorded; `REL-AI-1` stays open until then |

## Required release environment

| Variable | Scope | Purpose |
|---|---|---|
| `NUGET_API_KEY` | secret (forwarded to `domain-release.yml`) | nuget.org push |
| `GITHUB_TOKEN` | provided | GitHub Release + evidence asset upload |
| `NUGET_SIGNING_CERTIFICATE_BASE64` / `NUGET_SIGNING_CERTIFICATE_PASSWORD` | secret (optional) | package signing in `release-evidence.yml`; when absent, signing is recorded as a blocking readiness reason |
| `NUGET_SIGNING_TIMESTAMPER` | var (optional) | RFC 3161 timestamp authority (default `http://timestamp.digicert.com`) |

### Provisioning the signing certificate

The FR24 evidence gate signs a **re-pack** of the release packages (it does not sign what
`domain-release.yml` publishes to nuget.org — that is the separate G2 track). To clear the AC2
blocking readiness reason cheapest, provision a **self-signed code-signing chain** — a self-signed
**root CA plus a leaf it issues**. A bare self-signed *leaf* will not work: `dotnet nuget sign`
rejects it (`NU3018 InvalidBasicConstraints`) because it builds and validates a chain to a CA.

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

On the next tagged release, `release-evidence.yml` signs into `nupkgs-signed/`, verifies with an
RFC 3161 timestamp, and records `signing-readiness.json` as `signed:true, verified:true`. This is an
**internal evidence** signature — the root is not publicly trusted, so it is not for author-signing on
nuget.org. For publicly-trusted publishing use Azure Trusted/Artifact Signing or a CA cert (G2 track).

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
5. `NUGET_API_KEY` configured. Publish is automatic after CI success on `main` (via `workflow_run`); `release-evidence.yml` produces the G1 evidence bundle. To sign, provision the `NUGET_SIGNING_CERTIFICATE_*` secrets.
6. After a real release, the Release Owner records the `release-evidence.yml` evidence paths so `REL-AI-1` can close.
