# Hexalith.FrontComposer — Deployment / Release Guide

> **Generated:** 2026-06-02 · deep scan. FrontComposer ships as **NuGet packages**, not a deployed service. "Deployment" here means the automated **semantic-release → NuGet** pipeline ([.releaserc.json](.releaserc.json)) plus the CI workflows.

## What gets shipped

FrontComposer is a library/tooling product. Its release artifacts are **signed NuGet packages** (`.nupkg`) + **symbol packages** (`.snupkg`) published to nuget.org, plus a GitHub Release carrying signed packages and a full **release-evidence** bundle. The expected package set is pinned in [eng/release-package-inventory.json](eng/release-package-inventory.json) and verified during release.

There are **no Dockerfiles / container images** for FrontComposer's own projects (unlike the `Hexalith.EventStore` submodule). The only orchestration is the local `samples/Counter` Aspire AppHost, which is a sample — not a deployed artifact.

## CI workflows ([.github/workflows/](.github/workflows/))

| Workflow | Trigger | Purpose |
|---|---|---|
| `ci.yml` | push / PR to `main` | commitlint · build (Gate 1 netstandard2.0 Contracts, Gate 2 solution) · CLI pack+smoke · Governance & Contract tests · docs validation · unit+bUnit (coverage) · a11y/visual (Playwright) |
| `release.yml` | push to `main` (+ manual) | semantic-release pipeline (below) |
| `nightly.yml` | schedule | nightly checks (incl. skill-corpus prompt benchmark) |
| `ide-parity-revalidation.yml` | schedule / drift | revalidates `docs/ide-parity-matrix.json` against IDE behavior |
| `mutation-property-nightly.yml` | schedule | Stryker mutation + FsCheck property suites |
| `flaky-test-governance.yml` | schedule/governance | flaky-test tracking |
| `quarantine-governance-nightly.yml` | schedule | quarantined-test governance |

CI build/test commands are the source of truth for local dev — see [development-guide.md](./development-guide.md).

## Versioning — semantic-release + Conventional Commits

Releases are fully automated from commit messages on `main` ([.releaserc.json](.releaserc.json)). Branch: `main`; tag format `v${version}`.

| Commit type | Version bump |
|---|---|
| `feat:` | minor |
| `fix:` / `perf:` | patch |
| `feat!:` or `BREAKING CHANGE:` footer | major |
| `docs:` / `refactor:` / `test:` / `chore:` / `build:` / `ci:` / `style:` | none |

Plugins: `commit-analyzer` → `release-notes-generator` → `changelog` → `exec` (build/pack/sign/publish) → `github` (release + assets) → `git` (commits `CHANGELOG.md` with `chore(release): ${version} [skip ci]`).

## The release pipeline (`@semantic-release/exec`)

### `prepareCmd` (build → sign → evidence)

1. `dotnet build -c Release -p:Version=<next>` then `dotnet pack --no-build -c Release --include-symbols --include-source -o ./nupkgs`.
2. **SBOM:** `dotnet CycloneDX Hexalith.FrontComposer.slnx -o ./release-evidence/sbom --json`.
3. **Package inventory:** `python eng/release_evidence.py inventory --expected eng/release-package-inventory.json` (verifies the exact set of packages produced).
4. **Sign:** `dotnet nuget sign ./nupkgs/*.nupkg` with `$NUGET_SIGNING_CERTIFICATE_PATH` / `…_PASSWORD` / `…_TIMESTAMPER` → `./nupkgs-signed`, then `dotnet nuget verify`.
5. **Attestation (optional):** when `RELEASE_ATTESTATION_STATUS=attested`, `gh attestation verify … --signer-workflow Hexalith/Hexalith.FrontComposer/.github/workflows/release.yml`.
6. **Evidence chain:** release-budget snapshot → `checksums` → `prepare-manifest` → `seal-manifest` → partial-publish placeholders → `verify-manifest`. All written under `release-evidence/`.

### `publishCmd` (guarded publish)

1. **Dry-run guard:** `RELEASE_DRY_RUN` defaults to **true**; a dry run runs `classify-release --dry-run-clean-exit` and **halts before any publish side effect** (defense-in-depth so `@semantic-release/github`/`git` never fire on a dry run).
2. **Concurrency probes:** queries GitHub for a prior `v<next>` tag and other in-flight `release.yml` runs targeting the same version; fails closed on ambiguity (`RELEASE_CONCURRENT_SAME_VERSION=true`).
3. **Readiness classification:** `classify-release --require-publishable` gates on manifest, test-results, ref protection, fork status, owner approval, attestation status.
4. **Publish:** `dotnet nuget push ./nupkgs-signed/*.nupkg` then `./nupkgs/*.snupkg` to `https://api.nuget.org/v3/index.json` with `$NUGET_API_KEY --skip-duplicate`. A push failure records a `partial-publish-incident` and exits non-zero.

### GitHub Release assets (`@semantic-release/github`)

Signed `.nupkg` + `.snupkg`, `checksums.json`, `sealed-manifest.json`, `release-verification.json`, `release-readiness.json`, `test-results.json`, release-budget summaries, `signing-verification.txt`, SBOM JSON, attestation bundle, partial-publish records, prior-release probe.

## Required release environment

| Variable | Purpose |
|---|---|
| `NUGET_API_KEY` | nuget.org push |
| `NUGET_SIGNING_CERTIFICATE_PATH` / `_PASSWORD` / `NUGET_SIGNING_TIMESTAMPER` | package signing |
| `RELEASE_DRY_RUN` | `true` (default) blocks publish; set `false` to actually publish |
| `RELEASE_ATTESTATION_STATUS` (+ fallback approval vars) | controls GitHub attestation gate |
| `GITHUB_REPOSITORY`, `GITHUB_RUN_ID`, `GITHUB_REF`, `GITHUB_REF_PROTECTED`, `RELEASE_FROM_FORK`, `RELEASE_OWNER_APPROVED`, … | classification inputs |

## Consuming the packages

Downstream apps add the FrontComposer NuGet packages, annotate domain types with `[Projection]`/`[Command]`, and call `AddHexalithFrontComposerQuickstart()` + `AddHexalithDomain<TMarker>()` + `AddHexalithEventStore(...)` (see [architecture.md](./architecture.md) §4). To stand up the MCP endpoint, register the fail-closed gates and call `AddFrontComposerMcp(...)` + `MapFrontComposerMcp()` (see [api-contracts.md](./api-contracts.md) §2).

## Pre-release checklist

1. Conventional-commit messages on `main` (drives the version bump).
2. Green CI: build Gates 1–2, Governance + Contract tests, docs validation, unit+bUnit lane.
3. No stale pact diff; public-API baseline updated intentionally if the Testing surface changed.
4. `eng/release-package-inventory.json` matches the packages you intend to ship.
5. Signing cert + NuGet key configured; decide `RELEASE_DRY_RUN` (`true` to rehearse).
