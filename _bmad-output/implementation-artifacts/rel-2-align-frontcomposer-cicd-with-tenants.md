---
created: 2026-07-09
updated: 2026-07-13
owner: Release Owner + Developer + QA/Test Architect
baseline_commit: d05d723d0732ef062c9a1b18be9af971136c3086
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-09-tenants-cicd-alignment.md
fr24Proposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-13-rel-ai-1-fr24-rehome-into-rel-2.md
status: review
approval: approved-by-administrator-2026-07-09
fr24Approval: approved-by-administrator-2026-07-13
fr24Owner: true
gatingDecision: "G1 now (post-publish evidence + next-release fail-closed); G2 (Hexalith.Builds inline pre-publish gate) flagged as a durable upstream follow-up. Approved 2026-07-13."
scope: moderate
---

# REL-2: Align FrontComposer CI/CD With Tenants Reusable Workflows

Status: review.

Approval: approved by Administrator on 2026-07-09. FR24 fold-in approved 2026-07-13.

**FR24 scope (added 2026-07-13):** REL-2 now also OWNS the FR24 release evidence gate (formerly REL-1,
closed as superseded). Because the shared reusable `domain-release.yml` provides no evidence hook and is
a non-editable submodule, FR24 uses 3-layer **split-homing**: (1) package inventory + consumer validation
in shared CI via `domain-ci.yml` `run-consumer-validation: true` + new FrontComposer `scripts/`; (2) the
pristine reusable `domain-release.yml` for publish; (3) a **new supplemental `.github/workflows/release-evidence.yml`**
(`workflow_run` on CI success) reusing `eng/release_evidence.py` for signing, SBOM, checksums, manifest,
`classify-release`, and evidence assets. Gating posture **G1** (post-publish + next-release fail-closed);
**G2** (Hexalith.Builds inline gate) is a flagged durable follow-up. `REL-AI-1` stays open until the
Release Owner records evidence paths.

## Story

As a Release Owner and FrontComposer maintainer,
I want FrontComposer primary CI/CD to use the same reusable Hexalith.Builds workflows as Hexalith.Tenants,
so that Hexalith modules share one CI/CD operating model.

## Context

The approved Correct Course proposal
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-09-tenants-cicd-alignment.md`
compares FrontComposer's bespoke CI/CD with the Tenants submodule baseline:

- Tenants CI delegates to `Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main`.
- Tenants release runs from `workflow_run` after a successful `CI` push to `main` and delegates to
  `Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main`.
- FrontComposer currently has bespoke CI/release workflows and governance tests that pin that model.

REL-1 remains closed/superseded for FR24 evidence obligations, and its July 5 auto-publish-from-`main`
release-model decision is superseded by the approved July 9 Tenants alignment proposal.

## Acceptance Criteria

1. Given `.github/workflows/commitlint.yml`, when workflow triggers are inspected, then it matches
   Tenants by running on PRs and pushes to `main`.

2. Given `.github/workflows/ci.yml`, when workflow structure is inspected, then the primary CI job uses
   `Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main` with FrontComposer-specific test
   project inputs and root-only submodule initialization.

3. Given package-consumer validation is enabled, when shared CI runs, then FrontComposer package
   pack/validate/consumer scripts are available and pass.

4. Given FrontComposer Verify snapshots, when tests run in shared CI, then `DiffEngine_Disabled=true`
   is applied so CI cannot hang on diff tooling.

5. Given `.github/workflows/release.yml`, when workflow structure is inspected, then release runs through
   `workflow_run` after successful `CI` push events and delegates to
   `Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main`.

6. Given the final packable set (8 packages incl. `Contracts.UI` @ 2.0.0), when shared CI runs with
   `run-consumer-validation: true`, then FrontComposer `scripts/` pack/validate/consumer wrappers
   validate the inventory and prove the documented Contracts-only vs Shell/UI consumer boundaries
   (FR24 AC1 + AC6) before release. A Contracts-only consumer must NOT inherit Blazor/Fluent runtime
   deps (kernel-split invariant); a Shell/UI consumer references Contracts + Contracts.UI + Shell.

7. Given governance tests run, when they inspect CI/CD workflows, then they assert the Tenants-aligned
   `workflow_run` + reusable `domain-release.yml` model AND the re-homed FR24 supplement, and no longer
   assert the superseded auto-publish/advisory model. Specifically flip
   `ReleaseWorkflow_PublishesFromMainWithoutManualDispatchGates`,
   `ReleaseWorkflow_RunsAutomaticPackageReleaseAfterBlockingTests`, and
   `ReleaseWorkflow_ProducesAdvisoryFr24EvidenceBundleWithoutGating`, and update
   `SemanticReleasePack_EnablesEvaluatedPackageValidationAgainst112Baseline` from the 1.12.0 baseline to
   the 2.0.0 / `Contracts.UI` reality. **The migration will break MORE than these four named tests — see
   Dev Notes §7 for the full breakage list; the completion gate is the whole Governance lane green, not
   just these four.**

8. Given FrontComposer-only quality gates are still required by PRD NFR-11, when primary CI/CD is
   simplified, then those gates are either moved to shared reusable workflow support or retained as
   explicitly supplemental quality workflows with CI authority documented.

### FR24 Release Evidence Gate (added 2026-07-13; ACs re-homed from REL-1)

9. Given a release runs (`workflow_run` after CI success), when the supplemental
   `.github/workflows/release-evidence.yml` executes, then it reuses `eng/release_evidence.py` to produce
   SBOM, checksums, test-results, package inventory, and a sealed+verified release manifest bound to
   commit SHA / tag / run-id / workflow-ref / package-set fingerprint / version, runs `classify-release`,
   and attaches the evidence bundle as GitHub Release assets (FR24 AC3 + AC8). Keeps `submodules: false`
   root-only init; no recursive submodule updates.

10. Given signing is provisioned, when packages are produced, then `.nupkg` are signed and verified
    (`dotnet nuget verify --all`, RFC 3161 timestamp) and unsigned/timestamp-missing packages are
    recorded as a blocking readiness reason (FR24 AC2).

11. Given the reusable `domain-release.yml` publishes without an inline hook, when release readiness is
    enforced, then the **G1** model applies: publish proceeds under the reusable workflow; the evidence
    workflow runs `classify-release` and **fails closed on the next release** if the prior evidence is
    missing/invalid (FR24 AC4–5). **G2** (an opt-in inline pre-publish gate upstreamed into Hexalith.Builds
    `domain-release.yml`) is tracked as a separate owner-approved dependency, not implemented in this repo.

12. Given Release Owner review, when evidence paths are recorded for every FR24 artifact under the G1
    model, then `REL-AI-1` may move to `done`; otherwise it stays `open` with the blocking gap.

---

## Dev Notes

> This is a **brownfield CI/CD migration**. The heavy risk is not writing new YAML — it is (a) not losing
> FrontComposer-specific required gates the reusable workflows do not run, and (b) reconciling every
> governance test that pins the current inline shape. Read §7 and §8 before touching any `.yml`.

### 1. Current state — what exists today (the thing you are migrating FROM)

**`.github/workflows/ci.yml`** (bespoke, ~497 lines, `on: push[main] + pull_request[main]`). Three jobs:
- `commitlint` (ubuntu, no submodules) — duplicates the standalone `commitlint.yml`; validates PR title, PR commit range, and last-main commit via `npx commitlint`.
- `build-and-test` (ubuntu, `submodules: false` + `Hexalith/Hexalith.Builds/Github/initialize-build@main`, `initialize-dotnet@main` `dotnet-version: 10.0.301`). Gate steps, in order:
  - **Gate 1** — Contracts `-f netstandard2.0` isolation build.
  - **Gate 2** — full `.slnx` Release build.
  - **Gate 2a** — CLI tool package smoke (packs `Cli` @ `0.0.0-ci`, installs as local tool, runs `frontcomposer --help/inspect/migrate`).
  - **Gate 2b** — Governance tests: `DiffEngine_Disabled: true`, `dotnet test …slnx --filter "Category=Governance"`. **Blocking.**
  - **Gate 2c** — Contract pacts: `--filter "Category=Contract"`, `./eng/validate-contract-artifacts.ps1`, and a **stale-pact-diff guard** (`git diff --exit-code -- tests/…/Pact`). Uploads `contract-artifacts`.
  - **Gate 2d** — Docs validation: `pwsh ./eng/validate-docs.ps1`.
  - **Gate 3a** — Unit + bUnit default lane (`DiffEngine_Disabled: true`, `--filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`, coverage collect). **Blocking.**
  - **Gate 3b/3c/3d** — palette / performance / quarantine lanes, all `continue-on-error: true` (advisory).
  - Quarantine evidence + CI-duration evidence via `.github/scripts/ci_governance.py` (`summarize-quarantine`, `duration-monitor`).
- `accessibility-visual` (**windows-latest**, ~25 min) — Playwright a11y/visual: builds `samples/Counter/Counter.Web`, `npm run test:a11y` with `Hexalith__FrontComposer__Specimens__Enabled: true`, plus visual-governance + a11y-artifact validators.

**`.github/workflows/release.yml`** (bespoke, ~269 lines, `on: push[main]` **only** — auto-publish). Job `release` (ubuntu, `submodules: false` + root-only init): seed `release-evidence/run-metadata.json` → build Contracts ns2.0 prereq → **run release tests per-project** (`DiffEngine_Disabled: true`, 7 test csprojs, `--filter "Category!=Quarantined"`) → `release_evidence.py test-results` → install CycloneDX → `release_evidence.py inventory` → `actions/attest-build-provenance@v4` over `package-inventory.json` → **`npx semantic-release` (live, no dry-run)** → upload packages → **FR24 advisory evidence layer** (SBOM via CycloneDX over `.slnx`; `checksums` → `prepare-manifest` → `seal-manifest` → `verify-manifest`; `classify-release` **advisory, no `--require-publishable`**; attach to GitHub Release; upload `release-evidence/**`). Every FR24 step is best-effort `if: always()` + `|| echo "::warning::…"`. **No signing. No dry-run toggle. `RELEASE_DRY_RUN` deliberately absent.**

**`.releaserc.json`** (semantic-release): `branches:["main"]`, `tagFormat:"v${version}"`; `@semantic-release/exec` `prepareCmd: "rm -rf ./nupkgs && python3 eng/pack_release_packages.py --version ${nextRelease.version} --output ./nupkgs"`; `publishCmd: "dotnet nuget push ./nupkgs/*.nupkg … && dotnet nuget push ./nupkgs/*.snupkg …"`; plus `@semantic-release/github` + `@semantic-release/git`.

**`.github/workflows/commitlint.yml`** — **PR-only** (`on: pull_request[main]`, no push); already a thin caller of `Hexalith/Hexalith.Builds/.github/workflows/commitlint.yml@main`. `codeql.yml` and `dependency-review.yml` are already reusable-workflow callers too. **So the only bespoke workflows to migrate are `ci.yml` and `release.yml`; `commitlint.yml` only needs its trigger widened to add `push[main]`.**

**`eng/`** — `release_evidence.py` (12 subcommands, see §5), `pack_release_packages.py` (reads `eng/release-package-inventory.json`, filters `packable==true`), `release-package-inventory.json`, `validate-contract-artifacts.ps1`, `validate-docs.ps1`, `validate-story-artifacts.py`. **There is NO `sign` and NO `sbom` and NO `consumer-validation` subcommand.** **`scripts/` does NOT exist.** `release-evidence/` exists with only `run-metadata.json` checked in.

**`Directory.Build.targets`** — `EnableFrontComposerPackageValidation` default **false** (opt-in); `FrontComposerPackageValidationBaselineVersion` default **1.12.0**; `Contracts.UI.csproj` overrides that baseline to **2.0.0**. Package validation is **not** currently enabled in the pipeline. `DiffEngine_Disabled` is **not** in props — it is a per-step workflow env only. `global.json` pins SDK **10.0.301**.

### 2. Target state — the Tenants baseline (the thing you are migrating TO)

Copy the shape from `references/Hexalith.Tenants/` (submodule, initialized). **Do not edit the submodule** — read it as a template only.

**Tenants `ci.yml`** (`references/Hexalith.Tenants/.github/workflows/ci.yml`):
```yaml
name: CI
on:
  push: { branches: [main] }
  pull_request: { branches: [main] }
  schedule: [ { cron: '0 2 * * *' } ]   # nightly performance tier
concurrency: { group: ci-${{ github.ref }}, cancel-in-progress: true }
permissions: { contents: read }
jobs:
  ci:
    uses: Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main
    with:
      solution: Hexalith.Tenants.slnx
      run-consumer-validation: true
      run-coverage-gate: true
      unit-test-projects: |
        tests/…Contracts.Tests
        tests/…Client.Tests
        …
      integration-test-projects: |
        tests/…Server.Tests
      aspire-test-project: tests/…IntegrationTests
      coverage-minimum-line: 80
      coverage-required-branch: 100
      coverage-line-scope: | …
      coverage-isolation-targets: | …
```

**Tenants `release.yml`** (`references/Hexalith.Tenants/.github/workflows/release.yml`):
```yaml
name: Release
on:
  workflow_run: { workflows: [CI], types: [completed], branches: [main] }
concurrency: { group: release-${{ github.ref }}, cancel-in-progress: false }
permissions: { contents: read }
jobs:
  release:
    if: >-
      github.event.workflow_run.conclusion == 'success' &&
      github.event.workflow_run.event == 'push'
    permissions: { contents: write, issues: write, pull-requests: write }
    uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main
    with:
      solution: Hexalith.Tenants.slnx
      publish-containers: true        # FrontComposer: OMIT — no containers to publish
      container-projects: | …          # FrontComposer: OMIT
    secrets:
      NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
      HEXALITH_ZOT_USERNAME: ${{ secrets.HEXALITH_ZOT_USERNAME }}   # FrontComposer: OMIT (no containers)
      HEXALITH_ZOT_API_KEY: ${{ secrets.HEXALITH_ZOT_API_KEY }}     # FrontComposer: OMIT
```
The `workflow_run.workflows: [CI]` name must match the caller CI workflow's `name: CI`. The `event == 'push'` guard stops nightly `schedule` CI runs from releasing. `cancel-in-progress: false` on releases.

**Tenants `commitlint.yml`** — `on: pull_request[main] + push[main]`; thin `uses: …/commitlint.yml@main`. (FrontComposer only needs to add `push[main]` to its existing caller.)

**Tenants `scripts/`** (`references/Hexalith.Tenants/scripts/` — the copy source for the 3 consumer-validation scripts):
- `pack-release-packages.py <output_dir> <version>` — positional args; deletes stale `*.nupkg`/`*.snupkg`, `dotnet pack <proj> --no-build -c Release -o <output_dir> -p:Version=<version> /m:1 /nr:false` per project in a `PACKAGE_PROJECTS` list.
- `validate-nuget-packages.py <package_dir>` — asserts package count == `EXPECTED_PACKAGE_IDS`, each `.nupkg` has readme + license, and each package's nuspec `<dependency>` set **exactly equals** `EXPECTED_DEPENDENCIES[id]` (the layering contract), with `FORBIDDEN_DEPENDENCY_IDS`/`FORBIDDEN_DEPENDENCY_FRAGMENTS` (no `.Tests`/`.Sample`/`.AppHost`/host deps). All packages must share one version.
- `validate-consumer-package-references.py <package_dir>` — generates **throwaway `PackageReference`-only** consumer projects (an `assert_package_only()` forbids `ProjectReference`), `dotnet restore` + `dotnet build -c Release -warnaserror -p:WarningsNotAsErrors=NU1603`, runs the test-consumer assembly. `PACKAGE_IDS` is the subset that gets a real consumer smoke build (Tenants: Contracts/Client/Server/Testing; Aspire excluded as AppHost-shaped).

**Reusable input contract you inherit** (from `references/Hexalith.Builds/.github/workflows/`):
- `domain-ci.yml` inputs: `solution` (req), `dotnet-global-json` (`global.json`), `unit-test-projects`, `integration-test-projects`, `aspire-test-project`, `run-consumer-validation` (bool, default false), `run-coverage-gate` (bool, default false), `coverage-*`, timeouts. **`run-consumer-validation: true` runs exactly `python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test` then `validate-nuget-packages.py ./nupkgs` then `validate-consumer-package-references.py ./nupkgs`** — the script paths and the positional `./nupkgs 0.0.0-ci-test` signature are hard-coded in the reusable. Root-only submodule init inside the reusable. **It does NOT set `DiffEngine_Disabled` and does NOT run Governance/Contract/docs/a11y/CLI-smoke — only unit/integration/aspire tests + optional consumer validation + optional coverage.**
- `domain-release.yml` inputs: `solution` (req), `test-projects` (leave EMPTY — CI already gated the head), `publish-containers` (default false), `container-projects`. Secrets: `NUGET_API_KEY` (req), `HEXALITH_ZOT_*` (optional). It runs `npm ci` → `npm audit signatures` → build `-warnaserror` → **`npx semantic-release`**. **There is NO evidence/SBOM/signing hook; the only injection points are `publish-containers` and the caller's own `.releaserc.json`.** This is why FR24 evidence CANNOT live in the release path and MUST go in a separate supplemental workflow.

### 3. The 3-layer FR24 split-homing architecture (approved 2026-07-13)

| Layer | Home | FR24 coverage | Submodule change? |
| --- | --- | --- | --- |
| **CI** | `domain-ci.yml` with `run-consumer-validation: true` + new FrontComposer `scripts/` | AC1 inventory, AC6 package-consumer validation | No |
| **Release (publish)** | pristine reusable `domain-release.yml` via `workflow_run` | none — publish only | No |
| **FR24 evidence** | **new supplemental `.github/workflows/release-evidence.yml`** (`workflow_run` on CI success), reusing `eng/release_evidence.py` | AC2 signing, AC3 SBOM+checksums+manifest, AC4–5 `classify-release`, AC8 evidence assets | No |

Rationale: the reusable `domain-release.yml` is a `@main` shared submodule with no evidence hook and cannot
be edited here. The supplemental `release-evidence.yml` is exactly the "supplemental FrontComposer quality
workflow" escape hatch the 07-09 proposal AC8 sanctioned; it reuses the already-proven 170 KB evidence tool.

### 4. Gating decision — G1 now / G2 flagged (approved)

- **G1 (in scope now):** publish proceeds under the reusable workflow; `release-evidence.yml` produces the
  full bundle, runs `classify-release`, attaches assets, and **fails closed on the NEXT release** if prior
  evidence is missing/invalid. Weaker (a bad release can publish once) but auditable and shippable with no
  submodule change.
- **G2 (flagged follow-up, NOT in this repo):** upstream opt-in `pre-publish-command`/signing/SBOM/classify
  inputs into Hexalith.Builds `domain-release.yml` so `classify-release --require-publishable` blocks
  **before** `dotnet nuget push`. This is a **separate owner-approved Hexalith.Builds story** (Proposal E).
  Do NOT edit `references/Hexalith.Builds` here. A story task only raises the upstream request.
- **G3 (rejected):** FrontComposer keeps a thin owned gated release job — defeats Tenants alignment.

### 5. `eng/release_evidence.py` — the evidence engine (reuse, do not rebuild)

12 subcommands: `inventory`, `checksums`, `test-results`, `verify-manifest`, `seal-manifest`,
`prepare-manifest`, `release-budget`, `path-check`, `partial-publish-incident`, `classify-release`,
`classify-fixtures`, `fallback-digest`. **No `sbom`, no `sign`, no `consumer-validation`.** The
`ReleaseEvidenceScript_*` / `ReleaseBudget*` governance tests already exercise this tool thoroughly against
fixtures in `tests/ci-governance/fixtures/` — it is **proven**, just **not wired into the live path as a
gate**. The `release-evidence.yml` chain to build (same as REL-1's advisory layer, but re-homed and G1-gated):
`inventory` → `checksums` → `test-results` → `prepare-manifest` → `seal-manifest` → `verify-manifest` →
`classify-release` → SBOM (CycloneDX over `.slnx`) → sign + verify (`dotnet nuget sign` / `dotnet nuget verify --all`,
only when the signing cert secret is provisioned) → upload evidence bundle + `gh release upload`. The sealed
manifest binds commit SHA / tag / run-id / workflow-ref / package-set fingerprint / version. `classify-release`
runs WITHOUT `--require-publishable` at publish time (G1 = advisory that release), but the NEXT run's
`test-results`/`inventory`/manifest checks fail closed if the prior evidence was missing/invalid.

### 6. Packages, inventory, and consumer boundaries (the 2.0 split landed)

`eng/release-package-inventory.json` (10 entries) is final after `b6e985f4 refactor!: govern the FrontComposer 2.0 package split`:
**8 packable** (`packable:true, symbol_required:true`): `Cli`, `Contracts`, `Contracts.UI`, `Mcp`, `Schema`,
`Shell`, `SourceTools`, `Testing`. **2 non-packable** (`packable:false, exception:…`): `AppHost` (Aspire exe),
`UI` (container host). `Contracts.UI` ships at a **2.0.0** public-API baseline; base packages remain 1.12.0.

**Consumer-boundary invariant to enforce (AC6):** a Contracts-only consumer must NOT inherit Blazor/Fluent
runtime deps (the kernel-split invariant — `Contracts` targets `net10.0;netstandard2.0` and is UI-clean); a
Shell/UI consumer references `Contracts` + `Contracts.UI` + `Shell`. Model FrontComposer's
`validate-nuget-packages.py` `EXPECTED_DEPENDENCIES` and `validate-consumer-package-references.py`
generated consumers on those two documented boundaries (see Decisions D3).

### 7. Governance test reconciliation — THE hard part (AC7)

File: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
(class `CiGovernanceTests`, class-level `[Trait("Category","Governance")]`). Tests parse workflow YAML as
**plain text** (`File.ReadAllText` + `ShouldContain`/`ShouldNotContain`/regex/`IndexOf`), not as parsed YAML.
There are currently **42** `[Fact]/[Theory]` in this class (was 41 at REL-1 — one was added since; re-baseline
the expected count before claiming a pass).

**The 4 named tests to flip/update (AC7):**
1. `ReleaseWorkflow_PublishesFromMainWithoutManualDispatchGates` (line ~864) — currently asserts
   `on: push[main]` regex and `ShouldNotContain("workflow_run:")`, plus inline step names "Run semantic-release",
   "Validate package inventory before release". **Flip to:** assert `release.yml` uses `on: workflow_run`
   (workflows `[CI]`, `event=='push'` guard) delegating to `domain-release.yml@main`; drop the inline-step
   and `on: push` assertions.
2. `ReleaseWorkflow_RunsAutomaticPackageReleaseAfterBlockingTests` (line ~296) — hard-codes `push:`, inline
   job/step names, six inline test-csproj paths, `submodules: false`, `Initialize build submodules`,
   `global-json-file: global.json`, `attest-build-provenance` ordering. **Flip to:** assert the reusable-caller
   shape (a `uses: …/domain-release.yml@main` job; CI is the upstream gate; no duplicated in-release test job).
3. `ReleaseWorkflow_ProducesAdvisoryFr24EvidenceBundleWithoutGating` (line ~902) — asserts ALL FR24 evidence
   commands live in `release.yml` with an `IndexOf(evidence) < IndexOf("Run semantic-release")` ordering.
   **Re-point to** `release-evidence.yml`: require the FR24 evidence chain there and assert the **G1** posture
   (next-release fail-closed; still no `workflow_dispatch`/`RELEASE_DRY_RUN` gated-dispatch model). The
   same-file ordering assertion must be removed (evidence and publish are now two files).
4. `SemanticReleasePack_EnablesEvaluatedPackageValidationAgainst112Baseline` (line ~419) — workflow-agnostic;
   shells `dotnet msbuild Contracts.csproj -getProperty:… -p:EnableFrontComposerPackageValidation=true` and
   asserts `PackageValidationBaselineVersion == "1.12.0"`. **Update to the 2.0.0 / Contracts.UI reality** and
   the final 8-package inventory (see Decisions D5 — the base is still 1.12.0, only Contracts.UI is 2.0.0, so
   decide precisely what "update to 2.0" means: re-point to `Contracts.UI.csproj` @ 2.0.0 and/or assert the
   8-package inventory).

**⚠ Migration breaks MORE than the 4 named tests.** Because these tests string-match the *inline*
`ci.yml`/`release.yml` shape, converting them to thin reusable callers WILL also fail at least:
- `ReleaseWorkflow_BuildsContractsNetStandard20BeforeContractTests` (asserts the inline "Build Contracts test
  prerequisite (netstandard2.0)" step in `release.yml`).
- `BlockingTestLanes_ExcludeQuarantinedTestsWithoutSkippingGovernance` (asserts `release.yml` contains
  `--filter "Category!=Quarantined"` and `ShouldNotContain("continue-on-error: true")` — the per-project
  release test loop is being removed).
- `BuildAndTestJob_IsBlockingAndHasGovernanceTelemetryGate`, `Gate2bGovernanceStep_IsNotMarkedAdvisory`,
  `QuarantineLane_IsWarningOnlyAndPublishesBoundedEvidence`,
  `Workflow_DoesNotUsePathFiltersThatCanSkipFrameworkGovernance` — all read the inline `ci.yml` gates that
  are being relocated to supplemental workflows.
- `Workflows_UseRootLevelSubmodulesOnly` enumerates **ALL** `.github/workflows/*.yml` — the new
  `release-evidence.yml` (and any new supplemental quality workflow) MUST use `submodules: false` +
  `initialize-build@main`, or this test fails.

**Completion gate:** run the FULL Governance lane and reconcile EVERY failure. For each broken test decide:
re-point (gate moved to a supplemental workflow file — update the path/assertions), flip (model changed —
invert the assertion), or preserve (gate kept — assertion still valid). Do not stop at the four named ones.

**How to run Governance tests locally** (VSTest/`dotnet test` sockets are blocked in this sandbox —
`SocketException (13): Permission denied`): build the test project per-project, then run the built xUnit v3
executable directly.
```bash
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -m:1 /nr:false
./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests \
  -class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests -notrait Category=Quarantined
```
Several Governance tests shell out to `python3` and `dotnet msbuild` (30 s timeout) — run from repo root with
`python3` and the .NET SDK on PATH. Direct-runner passes are advisory local evidence; **CI is authoritative**
for actual workflow execution. `.slnx` restore fails locally on de-initialized nested submodules — build
per-project, never via the solution restore.

Fixtures these tests depend on live in `tests/ci-governance/fixtures/` (e.g. `release-readiness-cases.json`
with 27 named cases, `release-manifest-valid/invalid.json`, `release-budget-three-breaches.json`) and
`release-evidence/run-metadata.json`. If you change evidence shapes, keep the fixtures in sync.

### 8. Where the FrontComposer-only gates go (AC8 / PRD NFR-11)

The reusable `domain-ci.yml` runs ONLY unit/integration/aspire tests + optional consumer validation +
optional coverage. It does NOT run: Gate 1 Contracts-ns2.0 isolation build, Gate 2a CLI tool smoke,
Gate 2b Governance, Gate 2c Contract pacts + `validate-contract-artifacts.ps1` + stale-pact-diff, Gate 2d
Docs (`validate-docs.ps1`), quarantine + CI-duration evidence (`ci_governance.py`), or the Playwright
a11y/visual job. **These must be RETAINED as explicitly-named supplemental FrontComposer quality
workflow(s)** (the AC8 escape hatch) — e.g. keep the FrontComposer-specific gates in a `quality.yml`
(or split governance/contract/docs/a11y/cli-smoke), running on `push[main] + pull_request[main]`, each
documented as CI-authoritative. Governance tests that assert those gates get re-pointed to the supplemental
file(s). Do NOT silently drop any gate — PRD NFR-11 requires the v1.0 gate to include the default lane
(`DiffEngine_Disabled=true`), Governance, Contract, snapshots, PublicAPI baselines, Pact checks, property
tests where configured, docs validation, and e2e a11y/visual for the changed surface.

### 9. Decisions resolved (intent-gaps the dev must NOT re-open)

- **D1 — `DiffEngine_Disabled` in the shared CI lane (AC4).** The snapshot-heavy unit/bUnit tests run under
  `domain-ci.yml` `unit-test-projects`, where the removed inline `DiffEngine_Disabled: true` step no longer
  protects them and there is NO reusable input to set it. **Resolution:** make snapshot-diff-launching
  impossible at the repo level — add a `ModuleInitializer` in the test host that calls
  `DiffEngine.DiffRunner.Disabled = true;` (or `Verify`'s `VerifierSettings`/`DisableDiff` equivalent),
  committed so it holds under the reusable, the supplemental quality workflows, AND local runs. DiffEngine
  also auto-disables when it detects CI (`GITHUB_ACTIONS`/`CI`), but do NOT rely on that alone — the AC wants
  it "applied", and local reusable-equivalent runs may not set those. Keep the explicit
  `DiffEngine_Disabled: true` env on every `dotnet test` step in the **supplemental quality workflows** you
  own (Governance/Contract/docs lanes), exactly as today.
- **D2 — Consumer-validation script path/signature.** `domain-ci.yml` invokes `scripts/pack-release-packages.py`,
  `scripts/validate-nuget-packages.py`, `scripts/validate-consumer-package-references.py` with the packer
  called as `… ./nupkgs 0.0.0-ci-test` (positional). Create these three files under a NEW `scripts/` dir,
  mirroring the Tenants signatures. The existing `eng/pack_release_packages.py` (flag-based `--version/--output`,
  used by `.releaserc.json`) stays for the release path; the new `scripts/pack-release-packages.py` is the
  CI-time packer with the positional signature the reusable expects. **Prefer reading the single source of
  truth `eng/release-package-inventory.json`** (filter `packable==true`) inside `scripts/pack-release-packages.py`
  rather than hardcoding a `PACKAGE_PROJECTS` list, to avoid drift — this is a FrontComposer improvement over
  Tenants' hardcoded constants. `validate-nuget-packages.py` may keep its expected-id/dependency constants
  but the id set must equal the 8 packable ids.
- **D3 — FrontComposer consumer boundaries.** Generate at least two throwaway `PackageReference`-only
  consumers: (a) **Contracts-only** (references `Hexalith.FrontComposer.Contracts` only; asserts NO
  Blazor/`Microsoft.FluentUI.*`/`Fluxor` transitive dep resolves — kernel-split invariant) and (b) **Shell/UI**
  (references `Contracts` + `Contracts.UI` + `Shell`; compiles the documented bootstrap surface). Decide
  per-package whether `Cli` (tool, no refs), `Mcp`, `Schema`, `SourceTools` (Roslyn analyzer, `PrivateAssets`),
  `Testing` get a consumer smoke build or metadata-only checks — mirror Tenants' pattern (Testing gets a
  test-consumer; analyzer/tool packages are metadata-only in `validate-nuget-packages.py`).
- **D4 — `.releaserc.json` under the reusable.** `domain-release.yml` runs `npx semantic-release`, which reads
  FrontComposer's `.releaserc.json`; the existing `prepareCmd` (`eng/pack_release_packages.py`) + `publishCmd`
  (`dotnet nuget push`) still apply. Keep them. Reconcile governance test #1/#2 assertions on `.releaserc.json`
  content. Optionally (parity with Tenants, not required by ACs) add `validate-nuget-packages.py` /
  `validate-consumer-package-references.py` to `prepareCmd` as a belt-and-suspenders release-time check.
- **D5 — `…Against112Baseline` update.** Base `FrontComposerPackageValidationBaselineVersion` is still
  `1.12.0`; only `Contracts.UI.csproj` overrides to `2.0.0`. "Update to the 2.0.0 / Contracts.UI reality"
  means: re-point (and likely rename) the test so it proves the `Contracts.UI` @ 2.0.0 baseline is evaluated
  and the final 8-package inventory holds — NOT that every package jumped to 2.0. Keep a base-1.12.0 assertion
  if the base default is unchanged. Confirm with the inventory JSON before asserting.
- **D6 — REL-AI-1 stays open.** Do NOT mark `REL-AI-1` done from this story. AC12/AC8 require the Release Owner
  to run a real release and record evidence paths (CI-authoritative). Keep `REL-AI-1` open with
  `implementation_story: REL-2`.
- **D7 — G2 is out of scope here.** Only a task that *raises the upstream Hexalith.Builds request* belongs in
  this repo. Never edit `references/Hexalith.Builds` (or any `references/Hexalith.*` submodule) files.

### 10. Previous story intelligence — REL-1 (`rel-1-release-evidence-gate-before-v1-rc.md`, status `superseded`)

- REL-1 shipped the **advisory FR24 evidence layer inside `release.yml`** (best-effort, non-gating) + the
  requiring test `ReleaseWorkflow_ProducesAdvisoryFr24EvidenceBundleWithoutGating` + a deployment-guide
  reconciliation. **That advisory layer is DELETED when REL-2 adopts the reusable `domain-release.yml`** and
  re-homed into `release-evidence.yml`.
- REL-1 wired these `release_evidence.py` subcommands (reuse them in `release-evidence.yml`): `test-results`,
  `inventory`, `checksums`, `prepare-manifest`, `seal-manifest`, `verify-manifest`, `classify-release`
  (advisory, no `--require-publishable`). It did NOT use `partial-publish-incident`, `release-budget`,
  `path-check`, `fallback-digest`, `classify-fixtures` (available; classify-fixtures is test-only).
- REL-1 **deferred → transferred to REL-2:** AC2 signing (needs code-signing cert + secrets — Release Owner),
  AC4–5 gating, AC6 consumer-validation (NO tooling existed — build the `scripts/`), AC8 evidence-recording
  (Release Owner real release). AC1/AC3/AC7 were implemented advisory.
- REL-1 verified `CiGovernanceTests` = 41/41 via the **direct xUnit v3 runner** (VSTest sockets blocked);
  build per-project (`-c Release -m:1 /nr:false`), NOT via `.slnx` restore (fails on de-init nested submodules).
  Now 42 tests — re-baseline.
- **A fully gated release workflow exists in git history at `ef2823ba~1`** ("streamlined to the minimal
  auto-publish path on 2026-07-03"). Mine it for the gated shape rather than rebuild from scratch.
- Signing self-flags as unproven and CI-authoritative; the SBOM step warns the CycloneDX-over-`.slnx`
  invocation "may be absent (refine …)". Treat SBOM + signing as verify-in-Actions, not locally.
- `baseline_commit` staleness: `eng/validate-story-artifacts.py` fails once `main` advances past the frontmatter
  baseline. REL-2's baseline (`d05d723d…`) is HEAD now; on completion reconcile with `--base <HEAD>` +
  a "Documented Unrelated Changes" note (see the dev-story validator memory).

### 11. Git intelligence (recent CI/CD-relevant commits)

- `b6e985f4 refactor!: govern the FrontComposer 2.0 package split` — landed the 8-package inventory incl.
  `Contracts.UI` @ 2.0.0 (the trigger that makes the `…Against112Baseline` test stale and re-fires consumer validation).
- `17921001 feat: enhance release process with Python script for packaging` / `ef2823ba refactor: streamline
  release process` — introduced `pack_release_packages.py` / streamlined the gated release into auto-publish
  (the gated shape is at `ef2823ba~1`).
- `e36b96a4 feat: … enhance CI governance tests (#49)` / `9160e0c1 Refactor release governance … for FR-24` —
  the current `CiGovernanceTests` + FR24 governance shape.
- `9ca87724` pinned SDK 10.0.301 across workflows. `ed34cde3 ci: harden release failure evidence`.

### Project Structure Notes

- **Submodules (hard invariant):** root-declared `references/…` only; `submodules: false` on every checkout +
  `Hexalith/Hexalith.Builds/Github/initialize-build@main`. NEVER recursive; NEVER edit a `references/Hexalith.*`
  submodule file. The reusables already honor this; any NEW workflow you add must too
  (`Workflows_UseRootLevelSubmodulesOnly` enforces it across all workflow files).
- **New paths this story creates:** `scripts/pack-release-packages.py`, `scripts/validate-nuget-packages.py`,
  `scripts/validate-consumer-package-references.py`, `.github/workflows/release-evidence.yml`, and (per D1/D8)
  a supplemental FrontComposer quality workflow if you extract the FrontComposer-only gates out of `ci.yml`.
- **`.slnx` only**, central package management, TWAE everywhere — a new Python `scripts/` dir does not touch
  MSBuild, but any csproj generated by the consumer harness must build `-warnaserror` clean
  (`-p:WarningsNotAsErrors=NU1603` is the sanctioned CI-pack-version carve-out).
- **Docs:** any reconciliation of the release/deployment model goes to `_bmad-output/project-docs/deployment-guide.md`
  (NOT the published `docs/` DocFX site).

### References

- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-09-tenants-cicd-alignment.md] — alignment rationale, ACs 1–5, NFR-11/12 impact.
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-13-rel-ai-1-fr24-rehome-into-rel-2.md] — 3-layer split-homing, G1/G2, governance-test flip list, Proposals A–F.
- [Source: _bmad-output/implementation-artifacts/rel-1-release-evidence-gate-before-v1-rc.md#Acceptance-Criteria] — FR24 AC1–AC8 canonical text; deferred/transferred ACs.
- [Source: references/Hexalith.Tenants/.github/workflows/ci.yml, release.yml, commitlint.yml] — copy-from templates.
- [Source: references/Hexalith.Tenants/scripts/{pack-release-packages,validate-nuget-packages,validate-consumer-package-references}.py] — script templates.
- [Source: references/Hexalith.Builds/.github/workflows/domain-ci.yml, domain-release.yml] — reusable input contracts; no evidence hook.
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs] — the 42 governance tests; the 4 named + broader breakage set.
- [Source: eng/release_evidence.py] — 12 subcommands; [Source: eng/release-package-inventory.json] — 8 packable + 2 non-packable.
- [Source: _bmad-output/project-context.md] — TWAE, submodule, `.slnx`, DiffEngine, testing, release rules.

---

## Tasks / Subtasks

- [x] **T1 — Widen `commitlint.yml` trigger (AC1).**
  - [x] Add `push: { branches: [main] }` alongside the existing `pull_request: { branches: [main] }` in
        `.github/workflows/commitlint.yml` (Tenants parity). Keep the `uses: …/commitlint.yml@main` body.
- [x] **T2 — Create the `scripts/` consumer-validation trio (AC3, AC6; FR24 AC1+AC6).**
  - [x] `scripts/pack-release-packages.py <output_dir> <version>` — positional signature; reads
        `eng/release-package-inventory.json` (filter `packable==true`) and `dotnet pack … --no-build -c Release
        -o <output_dir> --include-symbols -p:Version=<version> -p:SymbolPackageFormat=snupkg /m:1 /nr:false` (D2).
  - [x] `scripts/validate-nuget-packages.py <package_dir>` — asserts the 8 packable ids (read from the
        inventory, not hardcoded — D2 anti-drift improvement), license metadata + readme integrity when present,
        single shared version, forbidden-fragment guard, and the **kernel-split invariant** (Contracts declares no
        Blazor/Fluent/Fluxor dep). Note: readme is optional (FrontComposer ships one only on Cli/Testing); exact
        per-package dependency-set equality was replaced by the invariant checks (see Completion Notes).
  - [x] `scripts/validate-consumer-package-references.py <package_dir>` — generates Contracts-only + Shell/UI
        `PackageReference`-only consumers (D3); `restore` + `build -warnaserror -p:WarningsNotAsErrors=NU1603`;
        asserts kernel-split (Contracts-only output pulls no Blazor/Fluent/Fluxor assembly).
  - [x] Sanity-ran each script locally against a scratchpad nupkgs directory produced at version 0.0.0-ci-test
        (per-project build first): pack → 8 nupkg + 8 snupkg; validate-nuget → 8/8 + kernel-split held;
        validate-consumer → both consumers build 0/0, Contracts-only proven UI-clean.
- [x] **T3 — Migrate `ci.yml` to the reusable `domain-ci.yml` (AC2, AC3, AC4).**
  - [x] Replaced the bespoke primary CI job with `uses: Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main`,
        `solution: Hexalith.FrontComposer.slnx`, `run-consumer-validation: true`, and the 5 trait-clean
        `unit-test-projects` (root-only init is inside the reusable; FrontComposer has no Dapr/Aspire tier).
  - [x] Applied the D1 `DiffEngine` repo-level disable (`DiffEngineModuleInitializer` in Shell.Tests +
        SourceTools.Tests — the two Verify-using assemblies) so no lane can launch a diff tool.
- [x] **T4 — Retain FrontComposer-only gates as supplemental quality workflow(s) (AC8; PRD NFR-11).**
  - [x] Moved Gate 1 (Contracts ns2.0), Gate 2 (solution build), Gate 2a (CLI smoke), Gate 2b (Governance),
        Gate 2c (Contract pacts + `validate-contract-artifacts.ps1` + stale-pact-diff), Gate 2d (Docs), the
        trait-filtered Gate 3 lanes, quarantine + CI-duration evidence, and the Playwright a11y/visual job into
        the new `quality.yml` on `push[main] + pull_request[main]`, each test step keeping `DiffEngine_Disabled: true`
        and root-only submodule init. CI authority documented in the header + deployment-guide.
- [x] **T5 — Migrate `release.yml` to the reusable `domain-release.yml` (AC5).**
  - [x] Replaced with an `on: workflow_run: { workflows: [CI], types: [completed], branches: [main] }` caller
        gated `if: conclusion=='success' && event=='push'`, `uses: …/domain-release.yml@main`,
        `solution: Hexalith.FrontComposer.slnx`, `test-projects: ''`, no containers. Forwards `secrets: NUGET_API_KEY`.
  - [x] Deleted the inline FR24 advisory evidence layer (moved to T6). Kept `.releaserc.json` pack/publish (D4).
- [x] **T6 — Add supplemental `.github/workflows/release-evidence.yml` (AC9, AC10, AC11; FR24 AC2/AC3/AC4-5/AC8).**
  - [x] `on: workflow_run: { workflows: [CI], types: [completed], branches: [main] }`, guarded
        `conclusion=='success' && event=='push'`, `submodules: false` + `initialize-build@main`.
  - [x] Chains the reused eng/release_evidence.py subcommands: test-results → inventory → attest-build-provenance → SBOM
        (CycloneDX over `.slnx`) → sign+verify (`dotnet nuget sign`/`verify --all`, RFC 3161 — secret-gated; else
        records a blocking readiness reason) → `checksums` → `prepare-manifest` → `seal-manifest` →
        `verify-manifest` → `classify-release` → upload bundle + `gh release upload` assets, over a deterministic
        re-pack of the inventory.
  - [x] Implements **G1**: publish is not blocked inline; core evidence steps gate (fail closed) and
        `classify-release` runs without the publishable pre-gate. No `RELEASE_DRY_RUN`/gated-dispatch reintroduced.
- [x] **T7 — Flip/update governance tests + reconcile the whole Governance lane (AC7).**
  - [x] Flipped the 3 model-guard tests + updated the baseline test (§7 items 1–4) and re-pointed the 6 gate-moved
        tests to `quality.yml`/`commitlint.yml`/`release-evidence.yml`; removed 2 orphaned private helpers.
  - [x] Ran the FULL Governance lane via the direct xUnit v3 runner: `CiGovernanceTests` **44/44** (42 methods +
        2 Theory rows) and the default-lane `Story12_4_RedPhaseDefTests` **11/11** (its 2 `Def14` tests re-pointed
        to `release-evidence.yml`). Re-baselined at 42 `[Fact]/[Theory]` methods.
  - [x] The tests/ci-governance/fixtures directory is unchanged (no evidence-shape change).
- [x] **T8 — Raise the G2 upstream request (AC11).**
  - [x] Authored `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` (Proposal E)
        for the Release Owner to file against Hexalith.Builds. Did NOT edit `references/Hexalith.Builds`.
- [x] **T9 — Docs + status reconciliation (AC12).**
  - [x] Rewrote `_bmad-output/project-docs/deployment-guide.md` to the reusable + 3-layer split-homing + G1 reality
        (removed the superseded auto-publish/advisory description). Did NOT touch published `docs/`.
  - [x] Keep `REL-AI-1` open (`implementation_story: REL-2`); recorded the GitHub-Actions-only gaps
        (live publish, real SBOM, signing when cert provisioned, evidence-path recording) in the deployment guide.
- [x] **T10 — Verify locally + record CI-only gaps.**
  - [x] Built per-project `-c Release` (Shell.Tests, SourceTools.Tests, Cli, Mcp, Schema — all 0 warnings/0 errors);
        ran Governance via the direct xUnit v3 runner; all 5 workflow YAMLs parse. Reconciled
        `validate-story-artifacts.py` with `--base <HEAD>` + Documented Unrelated Changes.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8 (bmad-dev-story)

### Debug Log References

- Local validation environment note: solution-level `dotnet restore`/VSTest is blocked in the sandbox
  (nested-submodule restore + socket `Permission denied`). Followed the fallback ladder: built each project
  per-project `-c Release -m:1 /nr:false -p:NuGetAudit=false`, and ran Governance tests via the built xUnit v3
  executable directly (`-class …`). CI remains authoritative for actual workflow execution.
- `CiGovernanceTests` = **44/44** via the direct runner (42 `[Fact]/[Theory]` methods; the
  `…FailsClosedOnTrxNonFailedCounters` Theory expands to 2 rows). `Story12_4_RedPhaseDefTests` = **11/11**.
  `SourceTools.Tests` Governance subset = 132/0 (confirms its `DiffEngineModuleInitializer` loads).
- scripts trio sanity-run against `0.0.0-ci-test`: pack → 8 `.nupkg` + 8 `.snupkg`; `validate-nuget-packages` →
  8/8, kernel-split held (Contracts deps = `System.Collections.Immutable`, `System.Text.Json`,
  `System.Threading.Tasks.Extensions`); `validate-consumer-package-references` → Contracts-only + Shell/UI both
  build 0/0, Contracts-only output UI-clean.

### Completion Notes List

- **CI/CD model migrated to Tenants parity via 3-layer split-homing.** `commitlint.yml` widened to PR + push;
  `ci.yml` → reusable `domain-ci.yml` (`run-consumer-validation: true` + 5 trait-clean `unit-test-projects`);
  new `quality.yml` retains every FrontComposer-only gate (Gate 1/2/2a/2b/2c/2d, trait-filtered Gate 3 lanes,
  quarantine/CI-duration telemetry, a11y/visual) and is CI-authoritative for them; `release.yml` → reusable
  `domain-release.yml` via `workflow_run` after CI success; new `release-evidence.yml` hosts the FR24 evidence
  bundle under **G1** (post-publish + next-release fail-closed; core evidence steps gate, `classify-release`
  advisory-at-publish).
- **Test-lane split (D1 rationale):** the reusable `domain-ci.yml` runs whole projects with no `--filter`, so
  only the 5 trait-clean projects (Cli/Contracts/Contracts.UI/Mcp/Testing) go there. Shell.Tests (Governance/
  Contract/**Quarantined** lanes) and SourceTools.Tests (**Performance**/MutationErrorHandling) keep their
  trait-filtered gates in `quality.yml` so advisory/quarantine traits never enter the blocking lane. The
  `DiffEngineModuleInitializer` in both Verify-using assemblies satisfies AC4 at the repo level regardless of
  which lane runs them.
- **`validate-nuget-packages.py` — invariant validator (D2 latitude).** Rather than hardcode exact per-package
  dependency sets (brittle, undeterminable without packing, and prone to drift), it reads the packable id set
  from the inventory and enforces: 8 ids, license present, readme integrity when declared, single version,
  forbidden-fragment guard, and the **kernel-split invariant** (Contracts pulls no Blazor/Fluent/Fluxor). This
  is the sanctioned "FrontComposer improvement over Tenants' hardcoded constants". Empirically confirmed:
  FrontComposer packages ship `license=MIT` on all 8 but a README only on Cli/Testing — so the Tenants-style
  mandatory-readme check would have failed real CI; readme is therefore optional here.
- **Governance-test reconciliation (AC7) — the whole lane, not just the 4 named.** Flipped `Publishes…` →
  `RunsViaWorkflowRunAfterCiSuccess`, `RunsAutomaticPackageRelease…` → `DelegatesToReusableDomainReleaseAfterCiGate`,
  `ProducesAdvisoryFr24Evidence…` → `ReleaseEvidenceWorkflow_ProducesFr24EvidenceBundleUnderG1`, and updated
  `…Against112Baseline` → `EvaluatesBaseAndContractsUiPackageValidationBaselines` (D5: base stays 1.12.0, Contracts.UI
  evaluates 2.0.0). Re-pointed the 6 gate-moved tests (`CommitlintJob…`, `BuildAndTestJob…`, `Gate2b…`,
  `BlockingTestLanes…`, `QuarantineLane…`, and the ns2.0 build → `QualityWorkflow_BuildsContractsNetStandard20InIsolation`)
  to `quality.yml`/`commitlint.yml`/`release-evidence.yml`. Removed 2 now-orphaned private helpers
  (`ExtractJobBlock`, `ExtractRunScriptLines`) to avoid a dead-code TWAE break. **Discovered a broader breakage:**
  `Story12_4_RedPhaseDefTests` is no longer quarantined (runs in the default blocking lane) and two `Def14` tests
  pinned the old `release.yml` attestation shape — re-homed the attestation (`attest-build-provenance` +
  `attestations: write`/`id-token: write`) into `release-evidence.yml` (preserving FR24 AC9) and re-pointed both
  tests there.
- **REL-AI-1 stays open (D6).** AC12/AC8 require the Release Owner to run a real release, verify SBOM/signing/live
  publish in Actions, and record evidence paths — none of which can be verified locally. **G2 is out of scope
  here (D7):** only the upstream request doc was authored; `references/Hexalith.Builds` was not edited.
- **CI-only verification gaps (recorded):** live `semantic-release` publish, real CycloneDX SBOM over `.slnx`,
  package signing (needs `NUGET_SIGNING_CERTIFICATE_*` secrets — unsigned runs record a blocking readiness reason),
  the reusable-workflow execution itself, and the Playwright a11y/visual lane — all are GitHub-Actions-authoritative.

### File List

Story-owned changes:

- `.github/workflows/commitlint.yml` (modify — add push[main] trigger; T1)
- `.github/workflows/ci.yml` (modify — reusable `domain-ci.yml` caller; T3)
- `.github/workflows/quality.yml` (new — supplemental FrontComposer gates; T4)
- `.github/workflows/release.yml` (modify — reusable `domain-release.yml` caller via workflow_run; T5)
- `.github/workflows/release-evidence.yml` (new — FR24 evidence, G1, + attestation; T6)
- `scripts/pack-release-packages.py` (new — T2)
- `scripts/validate-nuget-packages.py` (new — T2)
- `scripts/validate-consumer-package-references.py` (new — T2)
- `tests/Hexalith.FrontComposer.Shell.Tests/DiffEngineModuleInitializer.cs` (new — D1/T3)
- `tests/Hexalith.FrontComposer.SourceTools.Tests/DiffEngineModuleInitializer.cs` (new — D1/T3)
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` (modify — flip/re-point 10 tests, remove 2 orphaned helpers; T7)
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/Story12_4_RedPhaseDefTests.cs` (modify — re-point 2 Def14 tests to release-evidence.yml; T7)
- `_bmad-output/project-docs/deployment-guide.md` (modify — reusable + split-homing + G1 reconciliation; T9)
- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` (new — G2 upstream request; T8)
- `_bmad-output/implementation-artifacts/rel-2-align-frontcomposer-cicd-with-tenants.md` (this story — status/record)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modify — rel-2 → in-progress → review)

Documented Unrelated Changes (dirty at session start, not owned by this dev-story):

- `_bmad-output/implementation-artifacts/rel-1-release-evidence-gate-before-v1-rc.md`,
  `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/prd.md`,
  `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-13-rel-ai-1-fr24-rehome-into-rel-2.md`
  (planning artifacts from the FR24 re-home correct-course), and the pre-existing submodule pointer edits
  `references/Hexalith.Builds`, `references/Hexalith.EventStore` (out of scope per Implementation Notes).

## Implementation Notes

- Do not initialize nested submodules. CI changes must keep root-declared submodule initialization only.
- Do not drop FR24 evidence obligations. If the reusable release path cannot host them in this story,
  keep `REL-AI-1` open with explicit owner/date/reopen criteria.
- Do not treat a direct Tenants file copy as sufficient if FrontComposer-specific required gates become
  untracked (see Dev Notes §8).
- Existing modified submodule pointers are out of scope unless the implementation story explicitly owns
  them.
- **Do not add FR24 evidence into the shared reusable `domain-release.yml`** — it is a `@main` submodule
  consumed by every Hexalith module and must not be edited from this repo. FR24 evidence lives in the
  FrontComposer-owned supplemental `release-evidence.yml`. The only path to inline pre-publish gating (G2)
  is a separate, owner-approved Hexalith.Builds change.
- The reusable `domain-release.yml` publishes via `npx semantic-release` with no evidence hook — hence the
  G1 post-publish + next-release fail-closed model. Do not assume inline gating exists.

## Change Log

- 2026-07-13: **Implemented (dev-story).** Migrated FrontComposer primary CI/CD to the Tenants-aligned reusable
  Hexalith.Builds workflows via 3-layer split-homing: `commitlint.yml` widened to PR+push; `ci.yml` →
  `domain-ci.yml` (consumer validation + trait-clean unit tests); new `quality.yml` retains all FrontComposer-only
  gates (CI-authoritative, PRD NFR-11); `release.yml` → `domain-release.yml` via `workflow_run`; new
  `release-evidence.yml` hosts FR24 evidence + signing + attestation under G1. Added the `scripts/`
  consumer-validation trio (inventory-driven pack, kernel-split-aware validators), the `DiffEngineModuleInitializer`
  (D1), the G2 upstream request doc, and rewrote the deployment guide. Reconciled the whole Governance lane
  (`CiGovernanceTests` 44/44, `Story12_4_RedPhaseDefTests` 11/11) including re-homing the FR24 attestation into
  `release-evidence.yml`. `REL-AI-1` stays open (Release-Owner real-release evidence). Status
  `ready-for-dev` → `review`.
- 2026-07-13: Expanded into a comprehensive dev-ready story (context engine pass). Added Dev Notes
  (current-vs-target state, 3-layer split-homing, reusable input contracts, Tenants templates, governance-test
  flip spec incl. the broader breakage list, FR24 evidence chain, resolved decisions D1–D7, REL-1 intelligence,
  git intelligence), detailed Tasks/Subtasks with AC mapping, File List, and `baseline_commit`. Status
  `backlog` → `ready-for-dev`. ACs 1–12 preserved; AC7 annotated with the full-Governance-lane completion gate.
- 2026-07-13: Expanded to OWN the FR24 release evidence gate (re-homed from the now-superseded REL-1) per
  approved Correct Course proposal `sprint-change-proposal-2026-07-13-rel-ai-1-fr24-rehome-into-rel-2.md`.
  Added FR24 ACs 6–12, the 3-layer split-homing architecture, the G1-now/G2-flagged gating decision, and
  FR24 evidence tasks. `REL-AI-1` remains open under REL-2.
- 2026-07-09: Created from approved Correct Course proposal. Story is backlog and ready for Release
  Owner / Developer / QA routing.
