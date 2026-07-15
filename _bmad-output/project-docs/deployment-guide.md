# Hexalith.FrontComposer — Deployment / Release Guide

> **Generated:** 2026-06-02 · deep scan. **Updated 2026-07-15 (REL-3 course correction)** after live
> v3.2.1/v3.2.2 evidence proved the G1 post-publication workflow is not an FR24 publication gate.
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
**Hexalith.Tenants**, so every Hexalith module shares one CI/CD operating model. REL-2's current G1
implementation uses 3-layer split-homing because the shared reusable `domain-release.yml` exposes no
evidence hook. This table describes the **current implementation**, not an FR24-compliant target. The
Release Owner has frozen publish-capable releases until REL-3 moves authorization ahead of publication:

| Layer | Home | Covers |
|---|---|---|
| **CI** | `ci.yml` → reusable `domain-ci.yml` with `run-consumer-validation: true` + `scripts/` | build + Tier 1 unit tests + coverage evidence; FR24 AC1 inventory + AC6 consumer-boundary validation |
| **Quality (supplemental)** | `quality.yml` | FrontComposer-only gates the reusable does not run — Gate 1 (Contracts netstandard2.0), Gate 2a (CLI smoke), Gate 2b (Governance), Gate 2c (Contract pacts + validators + stale-pact-diff), Gate 2d (docs), trait-filtered test lanes, quarantine/CI-duration telemetry, Playwright a11y/visual |
| **Release (publish)** | `release.yml` → reusable `domain-release.yml` via `workflow_run` | semantic-release pack + publish only |
| **Release evidence (current G1)** | `release-evidence.yml` (`workflow_run` after `Release`) | post-publication reconstructed evidence and diagnostics; cannot authorize or prove already-published bytes |

### Release freeze control (REL-4, approved 2026-07-15)

The freeze above is technically enforced by a fail-closed publish gate in `release.yml`
(implemented by REL-4; approved by
`sprint-change-proposal-2026-07-15-release-freeze-enforcement.md`):

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
| `release.yml` | `workflow_run` after CI success (push) | reusable `domain-release.yml@main`: semantic-release publish (no container images) |
| `release-evidence.yml` | `workflow_run` after `Release` completes | current G1 post-publication reconstructed evidence; REL-3 target is downloaded NuGet/GitHub verification only |
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

### Current publish path (`release.yml` → reusable `domain-release.yml`) — not FR24 compliant

`release.yml` runs from **`workflow_run` after a successful CI push** (guarded
`conclusion == 'success' && event == 'push'`, so PR/scheduled CI runs never release, and a failed CI
can never publish). It delegates to `domain-release.yml@main` with `test-projects: ''` (CI already
gated the same head — the release path does not duplicate test compute). The reusable runs
`npm ci` → `npm audit signatures` → build → `npx semantic-release`, which drives this repo's
`.releaserc.json`:

- **`prepareCmd`:** `python3 eng/pack_release_packages.py --version ${nextRelease.version} --output ./nupkgs` — packs the inventory package set (`.nupkg` + `.snupkg`).
- **`publishCmd`:** `dotnet nuget push ./nupkgs/*.nupkg` then `./nupkgs/*.snupkg` to nuget.org with `$NUGET_API_KEY --skip-duplicate`.

This path pushes unsigned `nupkgs/*.nupkg` before any FR24 manifest/readiness decision. It is retained
here as an accurate description of the current code, not as release authorization.

### Current G1 release evidence (`release-evidence.yml`) — diagnostic only

Re-homed from the bespoke `release.yml` (REL-1) into a supplemental workflow because the reusable
`domain-release.yml` has no evidence hook. It runs from `workflow_run` **after Release completes**,
resolves the release tag, and over a deterministic **re-pack** of the inventory produces:

1. `release_evidence.py test-results` (`release-evidence/test-results.json`) — release tests re-run excluding `Category=Quarantined`.
2. `release_evidence.py inventory` (`package-inventory.json`) → `actions/attest-build-provenance@v4` over the re-packed `.nupkg` (AC9).
3. CycloneDX **SBOM** (`sbom.json`), then — when the signing cert secret is provisioned — the re-packed `.nupkg` are signed into **`nupkgs-signed/`** (`dotnet nuget sign`, RFC 3161 timestamp) and verified (`dotnet nuget verify --all -v normal`); the path-sanitized transcript `signing-verification.txt` feeds `prepare-manifest`, so each package's `signing_status`/`timestamp_status` (**both blocking checks**) become `verified`. When absent, a **blocking readiness reason** is recorded (AC2). See [Provisioning the signing certificate](#provisioning-the-signing-certificate).
4. **LLM benchmark candidate evidence** (`llm_benchmark.py` validate-prompt-set → budget-status → run-benchmark, offline / **no provider spend**) → `benchmark-summary.json`, whose hash is bound into the manifest (a required field; the pass/fail benchmark *gate* remains a nightly concern).
5. `checksums` (over `nupkgs-signed/`) → `prepare-manifest` (`--signing-verification`, `--benchmark-summary-hash`) → `seal-manifest` → `verify-manifest` — the sealed manifest binds commit SHA / tag / run-id / workflow-ref / package-set fingerprint / version / sbom hash / benchmark summary hash.
6. `classify-release` **without** `--require-publishable` (G1 = advisory for the release that just published).
7. `gh release upload <tag> release-evidence/*.json` + `upload-artifact` archives `release-evidence/**` (30-day retention).

**Corrected interpretation (approved 2026-07-15):** G1 is not a publication gate. It runs after NuGet
and GitHub side effects, signs a reconstruction rather than the published packages, invokes
`classify-release` without `--require-publishable`, and its unsigned `require_or_record` path can finish
green with an invalid manifest and blocked readiness. The phrase “fails closed on the next release” is
withdrawn: the current release workflow contains no check that consumes the prior result. The evidence
is useful for diagnosis and historical reconciliation only. The required Hexalith.Builds dependency is
tracked in
[g2-hexalith-builds-inline-pre-publish-gate-request.md](../planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md). It is **not** implemented in this repo.

### REL-3 target release path

The publish freeze clears only when the implemented path is:

```text
Pack once → validate inventory/tests/consumers → generate symbols/SBOM
→ sign + RFC 3161 timestamp exact .nupkg → verify → checksum all artifacts/evidence
→ seal + verify manifest → classify-release --require-publishable
→ publish those same authorized bytes → verify downloaded NuGet/GitHub bytes
```

The shared Hexalith.Builds workflow must forward signing credentials and timestamp configuration to
semantic-release. FrontComposer's repository-owned `prepareCmd` performs authorization; `publishCmd`
re-verifies and pushes only manifest-authorized paths. The GitHub plugin attaches the durable evidence
bundle during initial release creation. The supplemental workflow no longer repacks or signs; it
downloads and verifies what NuGet and GitHub actually expose.

### FR24 status — why `REL-AI-1` stays open

| Target | AC | Status |
|---|---|---|
| Package-consumer validation (Contracts-only vs Shell/UI boundaries) | AC1/AC6 | **Implemented** — `scripts/` trio, run by `domain-ci.yml` `run-consumer-validation: true` |
| SBOM + checksums + manifest diagnostics | AC3/AC4–5/AC8 | **G1 diagnostic only** — produced over a re-pack after publication; v3.2.1/v3.2.2 manifests are invalid |
| Certificate signing + RFC 3161 timestamp | AC2 | **Not satisfied** — current secrets affect only the post-publication re-pack; published v3.2.1/v3.2.2 packages are unsigned |
| Attestation | AC9 | **Not sufficient for FR24** — current subject is the reconstructed package set, not exact published bytes |
| Exact-artifact pre-publish gate (`--require-publishable`) | REL-3 | **Required/blocking** — implement before the next release; Hexalith.Builds signing forwarding is mandatory unless the bounded contingency is approved |
| Durable initial GitHub Release evidence | AC8/AC12 | **Not satisfied** — immutable v3.2.1/v3.2.2 releases have package assets only; evidence remains a 30-day Actions artifact |
| Release Owner closure evidence | REL-AI-1 | **Pending** — only a real release authorized before publication and verified after publication can close the action |

Affected releases and their authoritative run/evidence links are recorded in the
[REL-AI-1 release-evidence ledger](../implementation-artifacts/rel-ai-1-release-evidence-ledger.md).

## Required release environment

| Variable | Scope | Purpose |
|---|---|---|
| `NUGET_API_KEY` | secret (forwarded to `domain-release.yml`) | nuget.org push |
| `GITHUB_TOKEN` | provided | GitHub Release + evidence asset upload |
| `NUGET_SIGNING_CERTIFICATE_BASE64` / `NUGET_SIGNING_CERTIFICATE_PASSWORD` | secret (required for FrontComposer release) | REL-3 target: forwarded by the reusable workflow to semantic-release and used to sign the exact publishable packages; current G1 uses them only on a re-pack |
| `NUGET_SIGNING_TIMESTAMPER` | reusable-workflow input / repository configuration (required) | approved RFC 3161 timestamp authority for the exact publishable packages |

### Non-publishing signing validation

The current G1 workflow signs a **re-pack** and therefore cannot clear FR24, even when its signing step
passes. The following self-signed chain recipe is retained only for local/non-publishing validation of
the mechanics. It is not approval for a production author identity. The Release Owner must select and
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
