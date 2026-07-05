# Hexalith.FrontComposer — Deployment / Release Guide

> **Generated:** 2026-06-02 · deep scan. FrontComposer ships as **NuGet packages**, not a deployed service. "Deployment" here means the automated **semantic-release → NuGet** pipeline ([.releaserc.json](.releaserc.json)) plus the CI workflows.

## What gets shipped

FrontComposer is a library/tooling product. Its release artifacts are **NuGet packages** (`.nupkg`) + **symbol packages** (`.snupkg`) published to nuget.org, plus a GitHub Release carrying those packages and an advisory **release-evidence** bundle. The expected package set is pinned in [eng/release-package-inventory.json](eng/release-package-inventory.json) and verified during release.

> **Current vs FR24 target (REL-1, 2026-07-05).** The live pipeline uses the deliberate 2026-07-03
> **auto-publish-from-`main`** model. REL-1 added an **advisory FR24 evidence layer** (test-results,
> inventory, SBOM, checksums, sealed manifest, advisory readiness) in [.github/workflows/release.yml](.github/workflows/release.yml)
> **without** re-adding approval gates or dry-run. **Deferred FR24 targets (why `REL-AI-1` stays open):**
> certificate **signing + RFC 3161 timestamp** (AC2), **publish gating** via `classify-release
> --require-publishable` / dry-run / owner approval (AC4–5), and **package-consumer validation** (AC6).
> Packages are currently published **unsigned**; provenance is via GitHub build-provenance attestation.

There are **no Dockerfiles / container images** for FrontComposer's own projects (unlike the `Hexalith.EventStore` submodule). The only orchestration is the local `samples/Counter` Aspire AppHost, which is a sample — not a deployed artifact.

## CI workflows ([.github/workflows/](.github/workflows/))

| Workflow | Trigger | Purpose |
|---|---|---|
| `ci.yml` | push / PR to `main` | commitlint · build (Gate 1 netstandard2.0 Contracts, Gate 2 solution) · CLI pack+smoke · Governance & Contract tests · docs validation · unit+bUnit (coverage) · a11y/visual (Playwright) |
| `release.yml` | push to `main` | semantic-release auto-publish + advisory FR24 evidence layer (below) |
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

Plugins: `commit-analyzer` → `release-notes-generator` → `changelog` → `exec` (pack/publish) → `github` (release + assets) → `git` (commits `CHANGELOG.md` with `chore(release): ${version} [skip ci]`).

## The release pipeline — current behavior (auto-publish + advisory evidence)

### semantic-release (`@semantic-release/exec`, [.releaserc.json](.releaserc.json))

- **`prepareCmd`:** `python3 eng/pack_release_packages.py --version ${nextRelease.version} --output ./nupkgs` — packs the inventory package set (`.nupkg` + `.snupkg`). No signing step today.
- **`publishCmd`:** `dotnet nuget push ./nupkgs/*.nupkg` then `./nupkgs/*.snupkg` to `https://api.nuget.org/v3/index.json` with `$NUGET_API_KEY --skip-duplicate`. Publish runs automatically on push to `main`; there is no dry-run or approval gate.

### FR24 evidence layer ([.github/workflows/release.yml](.github/workflows/release.yml), REL-1)

Orchestrated in the workflow around the unchanged publish, all **advisory / best-effort** (evidence failures never gate or block the auto-publish):

1. **Before publish:** run tests per project → `release_evidence.py test-results` (`release-evidence/test-results.json`); `release_evidence.py inventory` (`package-inventory.json`); `attest-build-provenance` over the inventory.
2. **After publish** (over the produced `./nupkgs`): CycloneDX **SBOM** (`sbom.json`) → `release_evidence.py checksums` (`checksums.json`) → `prepare-manifest` → `seal-manifest` (`sealed-manifest.json`) → `verify-manifest` (`manifest-verification.json`) → **advisory** `classify-release` **without** `--require-publishable` (`release-readiness.json`).
3. **Archive:** `gh release upload <tag> release-evidence/*.json` attaches the bundle to the GitHub Release; `upload-artifact` archives `release-evidence/**` (30-day retention, `if-no-files-found: error`).

### Deferred FR24 targets — why `REL-AI-1` stays open

| Target | AC | Status |
|---|---|---|
| Certificate signing + RFC 3161 timestamp (`dotnet nuget sign` / `verify --all`) | AC2 | **Deferred** — needs a code-signing cert + secrets (Release Owner) |
| Publish gating: `classify-release --require-publishable`, dry-run default, owner approval, partial-publish incident | AC4–5 | **Out of scope** — owner chose to keep the 2026-07-03 auto-publish model |
| Package-consumer validation (clean consumer restores local packages) | AC6 | **Not yet implemented** — follow-up |
| Release Owner records evidence paths from a real run | AC8 | **Pending** — the evidence layer must run in CI once and its paths be recorded |

The evidence tooling (`eng/release_evidence.py`) is complete and covers signing/gating/manifest classification; those axes are simply **not wired into the live pipeline** under the current model.

## Required release environment (current)

| Variable | Purpose |
|---|---|
| `NUGET_API_KEY` | nuget.org push |
| `GITHUB_TOKEN` | GitHub Release + evidence asset upload |

Signing (`NUGET_SIGNING_CERTIFICATE_*`), dry-run/approval (`RELEASE_DRY_RUN`, `RELEASE_OWNER_APPROVED`), and attestation-gate variables are part of the **deferred** FR24 gate and are not consumed by the current pipeline.

## Consuming the packages

Downstream apps add the FrontComposer NuGet packages, annotate domain types with `[Projection]`/`[Command]`, and call `AddHexalithFrontComposerQuickstart()` + `AddHexalithDomain<TMarker>()` + `AddHexalithEventStore(...)` (see [architecture.md](./architecture.md) §4). To stand up the MCP endpoint, register the fail-closed gates and call `AddFrontComposerMcp(...)` + `MapFrontComposerMcp()` (see [api-contracts.md](./api-contracts.md) §2).

## Pre-release checklist

1. Conventional-commit messages on `main` (drives the version bump).
2. Green CI: build Gates 1–2, Governance + Contract tests, docs validation, unit+bUnit lane.
3. No stale pact diff; public-API baseline updated intentionally if the Testing surface changed.
4. `eng/release-package-inventory.json` matches the packages you intend to ship.
5. `NUGET_API_KEY` configured. Publish is automatic on merge to `main` — there is no dry-run rehearsal in the current model (see deferred FR24 targets).
