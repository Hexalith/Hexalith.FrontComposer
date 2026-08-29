---
title: 'Bump .NET SDK, Packages, and Root Submodules'
type: 'refactor'
created: '2026-08-29'
status: 'in-review'
baseline_commit: '85216682495f8cae26cd0883e2e84a538450af4a'
review_loop_iteration: 1
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer pins unavailable SDK `10.0.302`, Aspire `13.4.6`, stale npm dependencies, and eight stale governance values although NuGet is current; one root submodule also trails `main`.

**Approach:** Align active SDK/package mirrors, fast-forward root submodules, preserve central ownership and historical evidence, then validate .NET, release tooling, E2E, governance, and Aspire behavior.

## Boundaries & Constraints

**Always:** Re-resolve versions/tips before editing; pin SDK `10.0.400` and Aspire SDK/CLI `13.5.3`; keep NuGet authority in `Hexalith.Builds`; update npm manifests/locks together, including TypeScript 7 and conventional-changelog 10; retain latest Fluent V5 prerelease; preserve CRLF and package-family alignment.

**Ask First:** Editing shared submodule contents for a newly published NuGet candidate; advancing Builds execution SHA/closures; recapturing manual IDE evidence; accepting non-fast-forward movement or an undocumented version retention.

**Never:** Add root package-version metadata; rewrite `CHANGELOG.md`, dated scenarios/evidence, or completed stories; initialize nested submodules; use recursive/remote updates; downgrade stable to prerelease; commit, push, or publish.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| SDK | Installed `10.0.400`; repo asks for `10.0.302` | Active pins/docs/tests use `10.0.400` | Fail on active stale pins; retain history |
| Packages | NuGet current; npm/Aspire/policy lag | Update owned pins/locks; major npm behavior passes | Retain documented prerelease/unresolved packages |
| Submodules | Seven current; EventStore one commit behind | Root worktrees equal remote `main`; nested remain uninitialized | Halt on dirty/divergent/non-FF state |

</frozen-after-approval>

## Code Map

- `global.json:3`, `.github/workflows/{quality,nightly,mutation-property-nightly,quarantine-governance-nightly}.yml` -- active SDK pins; `quality.yml:581` also owns Aspire CLI.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj:1` -- CPM-exempt SDK; must match catalog/exception `13.5.3`.
- `Directory.Packages.props`, `references/Hexalith.Builds/Props/Directory.Packages.props` -- read-only import/catalog; live audit found all 33 direct NuGet dependencies current.
- `eng/dependency-graph-policy.json:68-83` -- eight stale exact selected-catalog mirrors: FsCheck, Roslyn Workspaces, Localization, TimeProvider.Testing, MCP, Immutable, Verify, and Verify.XunitV3.
- Root/two E2E `package.json`/locks and `CiGovernanceTests.cs:920-945` -- npm authority, aligned Playwright/Axe, and release-parser gate.
- IDE parity matrix/job/test plus `_bmad-output/project-context.md` and current project docs -- current mirrors; historical evidence is read-only.
- `references/Hexalith.EventStore` -- current `62d28510...`, discovered remote `2aa94e80...`; all other root gitlinks were current during planning.

## Tasks & Acceptance

**Execution:**
- [x] Re-query official registries and eight remote refs; halt on unsafe movement.
- [x] Update SDK/Aspire/current mirrors, npm manifests/locks, and eight policy rows without duplicating NuGet authority.
- [x] Fast-forward changed roots only; run .NET, package, parser, E2E, governance, and Aspire checks, recording the commit-bound gate limitation while uncommitted.

**Review remediation:**
- [x] Add the explicit side-by-side `10.0.302` source-resource SDK exception while retaining `10.0.400` as the active/default SDK, and bind it with workflow governance and contributor documentation.
- [x] Make the IDE matrix fail closed on its historical `10.0.302` evidence baseline until manual `10.0.400` revalidation occurs; mechanically refresh only the producer fingerprint.
- [x] Refresh exact catalog expectations, the current project scan, Node `>=24.10.0` policy/locks, SDK-band validation, semantic-release coverage, and workflow pin extraction.
- [x] Make legacy scenario failures exit nonzero, add an executable propagation test, and exercise the real legacy runner without retaining regenerated historical result/evidence files.
- [x] Validate the EventStore and Tenants tips in their owning repositories, resolve the latest-tip source-routing blocker through the authorized Tenants upstream patch, run the primary Playwright/Axe lane and final live topology, and stop the exact FrontComposer AppHost.
- [x] Reconcile the complete story-owned File List and correct the mixed `ab51ef0d...` commit disposition without changing frozen intent or other process dispositions.

**Acceptance Criteria:**
- Given the root, when CLI/restore/build/AppHost checks run, then SDK `10.0.400` and Aspire `13.5.3` resolve warning-free.
- Given live registries, when freshness/behavior checks run, then NuGet remains latest-compatible, npm direct packages are latest stable, and major upgrades pass parser/typecheck gates.
- Given catalog and remotes, when governance/status run, then eight policy rows match without changing Builds execution pins, root gitlinks are latest fast-forward tips, and nested submodules remain uninitialized.

## Spec Change Log

- 2026-08-29: Implemented the approved SDK, Aspire, npm, policy-mirror, current-documentation, and root-submodule refresh; completed the verification matrix below.
- 2026-08-29: Applied review-loop remediation for the source-resource SDK exception, pending IDE evidence contract, exact catalogs, Node floor, workflow/parser governance, legacy runner propagation, primary accessibility lane, and owning-submodule validation; recorded the final latest-tip AppHost incompatibility without altering submodule content.
- 2026-08-29: After explicit user authorization for shared Builds/Tenants upstream work, adopted Builds `244ea890...`, pushed the focused Tenants source-routing fix at `635c3374...`, and closed the AppHost blocker with a warning-free dual-SDK build plus a `16/16` Running/Healthy Aspire proof.

## File List

- `.github/workflows/ide-parity-revalidation.yml`
- `.github/workflows/mutation-property-nightly.yml`
- `.github/workflows/nightly.yml`
- `.github/workflows/quality.yml`
- `.github/workflows/quarantine-governance-nightly.yml`
- `_bmad-output/implementation-artifacts/spec-bump-dotnet-sdk-packages-and-submodules.md`
- `_bmad-output/project-context.md`
- `_bmad-output/project-docs/architecture.md`
- `_bmad-output/project-docs/contribution-guide.md`
- `_bmad-output/project-docs/development-guide.md`
- `_bmad-output/project-docs/index.md`
- `_bmad-output/project-docs/project-overview.md`
- `_bmad-output/project-docs/project-scan-report.json`
- `_bmad-output/project-docs/source-tree-analysis.md`
- `docs/hot-reload-guide.md`
- `docs/ide-parity-matrix.json`
- `docs/ide-parity-matrix.md`
- `docs/validation/producer-fingerprints.json`
- `eng/dependency-graph-policy.json`
- `eng/run-epic9-live-proof.sh`
- `global.json`
- `jobs/ide-parity-version-revalidation.ps1`
- `package-lock.json`
- `package.json`
- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.Memories`
- `references/Hexalith.Tenants`
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`
- `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/package-lock.json`
- `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/package.json`
- `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/run-story-2-2-e2e.cjs`
- `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/run-story-2-2-e2e.test.cjs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`
- `tests/e2e/package-lock.json`
- `tests/e2e/package.json`
- `tests/e2e/scripts/run-epic9-live-proof.test.mjs`
- `tests/e2e/specs/specimen-accessibility.spec.ts`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-dark-comfortable-chromium-linux.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-dark-compact-chromium-linux.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-dark-roomy-chromium-linux.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-light-comfortable-chromium-linux.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-light-compact-chromium-linux.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-light-roomy-chromium-linux.png`

## Commit Scope Dispositions

- `299e0c88fef1c555526c701992845123e75889a1` | `process` | Concurrent deferred-work decision capture created after this spec's baseline.
- `09de050f7b57e4b4c1648710453639fd11d210a0` | `process` | Concurrent deferred-work decision capture created after this spec's baseline.
- `fd924d5064c460ee6a8fb666a6122305c30a089e` | `process` | Concurrent deferred-work decision capture created after this spec's baseline.
- `0441f7e00adf850160ae1688a4eb2b0f4a73570f` | `process` | Concurrent deferred-work decision capture created after this spec's baseline.
- `ab51ef0d1c0807f70c402bd47ee74620a24cc7e4` | `shared` | Mixed concurrent sweep bookkeeping with incidental capture of the then-untracked draft story spec; the sweep bookkeeping remains outside this spec while the captured draft is shared story/process history.

## Documented Unrelated Workspace State

- `.bmad-loop/decisions.json` - Changed only by the exact concurrent process commits declared above; outside this dependency refresh.
- `_bmad-output/implementation-artifacts/deferred-work.md` - Changed only by the exact concurrent process commits declared above; outside this dependency refresh.
- `_bmad-output/implementation-artifacts/bmad-build-auto-result-spec-11-23-recommended-analyze-01.md` - Concurrent untracked output from another workflow; preserved without modification.

## Design Notes

The Builds catalog gitlink and the approved CI/CD execution SHA are deliberately independent. This refresh repairs catalog mirrors but must leave `4eb33928...` workflow execution pins and evaluator closures untouched.

## Verification

**Commands:**
- `dotnet --version`; restore/build solution Release and AppHost Debug -- expected: `10.0.400`, warning-free.
- Root/E2E `npm ci`, E2E typecheck, and focused release-parser tests -- expected: reproducible locks and unchanged behavior.
- Dependency-graph unit tests and focused governance/IDE-parity tests -- expected: worktree-safe checks pass; report commit-object limitation separately.
- `aspire start --non-interactive`, `aspire describe`, `aspire stop`; submodule status and `git diff --check` -- expected: healthy topology, root-only gitlinks, no nested initialization/whitespace errors.

## Results

- **Versions and package authority:** `dotnet --version` returned `10.0.400`; Aspire CLI/AppHost resolved `13.5.3`. Official registry re-query confirmed the selected Builds NuGet catalog remains current-compatible and every declared npm dependency is at the latest stable version. Builds advanced to upstream `244ea890...` (`v4.26.0`); its live audit resolves 286 catalog entries, including latest-stable `Microsoft.Testing.Extensions.CodeCoverage` `18.10.0`, which FrontComposer does not consume. The eight selected-catalog policy mirrors still match Builds, while every approved `4eb33928...` execution pin/closure remains unchanged. Exact test mirrors now assert Fluent UI `5.0.0-rc.5-26219.1`, Localization `10.0.11`, and Microsoft.NET.Test.Sdk `18.9.0` with their coupled selected values.
- **Toolchain and clean installs:** `quality.yml` keeps the active/default SDK on `10.0.400` and installs one explicitly named side-by-side `10.0.302` source-resource compatibility SDK; governance scans all workflow YAML quoting forms, permits only that single exception, and extracts exactly one Aspire CLI install at `13.5.3`. Root and both E2E manifests require Node `>=24.10.0`; all three lock-only regenerations and all three clean `npm ci` runs passed with zero audit vulnerabilities. The current project scan was refreshed to the selected active package/toolchain baseline and its JSON validation passed.
- **Focused .NET behavior:** the direct xUnit v3 executable runs passed toolchain/SDK-threshold/semantic-release/Fluent governance `10/10`, IDE parity `5/5`, packaged analyzer `1/1`, Contracts.UI package boundary `1/1`, and Testing package boundaries `3/3`. The SDK threshold proves `10.0.302` and malformed/prerelease input fail, `10.0.400` and `10.0.499` pass, and the exclusive `10.0.500` ceiling fails. Semantic-release notes contained breaking, ordinary fix, and ordinary feature sections. The existing `dotnet test <project>` entry point remains a CI risk under SDK `10.0.400`: Microsoft.Testing.Platform rejects the legacy `VSTest` target before discovery, so the executable runners were used for focused evidence.
- **IDE evidence and documentation:** the active matrix pin is `10.0.400`, but `lastValidated` remains `2026-05-09` and all twelve untouched evidence manifests remain bound to historical baseline `10.0.302`. The explicit `revalidation-pending` contract and tamper tests fail closed instead of claiming manual revalidation. Only `docs/validation/producer-fingerprints.json` was mechanically refreshed (`5de4c6945e6a29c96c5f16d2fc00e4197896d82213f1d320876f500ef9340535`). `pwsh ./eng/validate-docs.ps1` passed.
- **Primary Playwright/Axe lane:** clean install and TypeScript 7 typecheck passed. Six Linux visual baselines were deliberately refreshed for Fluent RC5/Chromium after inspection, and the visual setup now waits for custom elements, fonts, and stable layout height. Final `npm run test:a11y` passed `22/22` in `14.8s`, including all Axe checks.
- **Legacy runner:** the new executable failure-propagation test passed `2/2`. The real `npm run story2.2:e2e` exited `1` as required: S1/S2 timed out waiting for `section.inline-section`, S4 timed out waiting for `section.command-section .fc-expand-in-row`, S6 passed, and all three Axe scenarios passed with zero serious/critical findings. Generated legacy result/evidence files were restored immediately because frozen historical evidence is not story-owned.
- **Owning-submodule validation:** Builds `244ea890...` passed its Release build with 0 warnings/errors plus package-audit `286` packages/`141` families/`1` source, generator fixtures `55`, audit-validator fixtures `60`, central-catalog `286` plus fixtures `14`, authoritative-catalog `50`/`3`, and workflow-platform `27` validations. EventStore `84c2ddae...` passed its package-mode Release solution build with 0 warnings/errors; changed tests passed parity closure `161/161`, smoke capture `9/9`, new Docker port resolution `4/4`, and all newly relevant OCI dispatcher cases. Tenants branch `fix/frontcomposer-source-routing` is pushed at `635c337409150d98bd5807e27e064e0caa094cf7`; its standalone Release build passed with 0 warnings/errors, focused routing behavior passed `3/3`, and UI `2556/2556`, Contracts `125/125`, and Server `747/747` tests passed. Memories `7b3f29ce...` passed its exact-SDK `10.0.302` Server.Tests Debug build with 0 warnings/errors plus its focused Story 24.6/24.7 lane `95/95`. All owning worktrees remained clean and all nested submodules remained uninitialized; broad tests that require nested modules were not treated as executable under the root policy.
- **Resolved Builds audit blocker:** upstream Builds `244ea890...` already binds the checked-in audit to the selected catalog bytes, so the deterministic audit validator now passes. No redundant Builds change, commit, or branch was created.
- **Current EventStore upstream blockers:** at clean tip `84c2ddae...`, Corrective OCI tests passed `36/37`; the sole fixture failure requires immutable Builds commit `22a578b...`, which is absent from the intentionally root-only initialized Builds object database. OQ8 tests passed `316/318`; two deterministic cases still expect a `rev-parse` failure message although the updated validator now fails safely earlier at `merge-base --is-ancestor`. Focused new cases and the checked-in direct OQ8 validator pass. These shared-tip test/evidence issues do not change FrontComposer package selection and were not patched inside the submodule.
- **Final latest-tip AppHost proof:** the authorized Tenants patch makes its non-packable UI/UI.Tests prefer a complete available FrontComposer source graph while preserving explicit `HexalithFrontComposerFromSource=false` package validation and leaving Commons/EventStore/Memories routing unchanged. The Epic 9 live-proof harness now exports the narrow `HexalithFrontComposerFromSource=true` input, with fake-lifecycle regression coverage, so NuGet restore, referenced-project builds, and Aspire children select the same FrontComposer graph without enabling every shared repository's source dependencies. The final isolated dual-SDK (`10.0.302` + `10.0.400`) serial AppHost build passed with 0 warnings/errors. Aspire `13.5.3` then reached `16/16` resources Running/Healthy, including Keycloak, Parties, Tenants, EventStore, and the dependency-closed FrontComposer UI. The exact AppHost was stopped and final `aspire ps --format Json` returned `[]`.
- **Submodule movement and commit-object limitation:** all captured movements were clean fast-forwards or an explicitly authorized pushed fix. Builds reached remote `244ea890...`, EventStore reached `84c2ddae...`, Memories reached `7b3f29ce...`, and Tenants reached pushed branch commit `635c3374...`; the other four root gitlinks remained remote-equal and clean at the final snapshot. `python3 eng/dependency_graph.py validate --commit ab51ef0d1c0807f70c402bd47ee74620a24cc7e4` passed all seven selectors, but necessarily validates the committed graph and reports old committed gitlinks; final worktree gitlinks cannot be graph-validated until a future commit object exists.
- **Residual source-resource SDK constraint:** the remote-equal Commons, Memories, Parties, and PolymorphicSerializations roots still pin SDK `10.0.302`. A clean runner must install it side-by-side with active/default `10.0.400` for source-resource processes until those upstream repositories align; governance documents and permits only this explicit compatibility exception. No shared root content was edited.
- **Hygiene:** release/history/manual IDE evidence surfaces were not rewritten, submodule worktrees are clean, all nested submodules remain uninitialized, and the temporary dual-SDK install was removed. The only shared-repository commit/push was the explicitly authorized Tenants fix above; no Builds or FrontComposer commit, FrontComposer push, publish, PR, merge, or nested-submodule operation was performed.
