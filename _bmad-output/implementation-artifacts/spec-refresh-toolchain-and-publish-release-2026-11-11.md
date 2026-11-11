---
title: 'Refresh the release toolchain and publish FrontComposer'
type: 'chore'
created: '2026-11-11'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '92c29614fd180373656da907bbd4e78b06d59921'
context: []
---

<frozen-after-approval reason="user explicitly authorized all updates, pushes, release, and bypass of human validation">

## Intent

**Problem:** FrontComposer still pins SDK 10.0.403 and vulnerable Aspire 13.6.4 while current stable releases and the shared dependency catalog have advanced. Quality failures need diagnosis before publishing the next release.

**Approach:** Synchronize all root dependencies, adopt latest stable SDK 11.0.100 and compatible package versions, resolve CI failures, push validated changes, publish through the existing release workflow, and verify all eight NuGet packages.

## Boundaries & Constraints

**Always:** Preserve existing work and target frameworks net10.0/netstandard2.0. Keep NuGet version authority in Hexalith.Builds, documented compatibility holds, audit/analyzer gates, and immutable execution-policy closures. Update active mirrors, tests, locks, and fingerprints together. Use explicit root-only Git operations, validate exact Conventional Commit messages with pinned commitlint before commits. The user authorizes external publication and handling reviewer gates without another approval.

**Never:** Initialize nested submodules, use recursive/remote updates, fabricate manual evidence, suppress package vulnerabilities, replace release proofs with a manual tag, or edit old completed stories. Any additional CI issue discovered may be fixed within this release objective, preserving meaningful gates.

</frozen-after-approval>

## Code Map

- `global.json`, `.github/workflows/{quality,nightly,mutation-property-nightly,quarantine-governance-nightly,ide-parity-revalidation}.yml`: active SDK pins; quality also owns Aspire CLI and side-by-side source-resource SDK 10.0.302. Retain that compatibility SDK and install net10 runtime as needed for tests under SDK11.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`: AppHost SDK must become 13.6.5, matching selected catalog and fixing GHSA-f7vh-h5g3-7jrh.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`: toolchain pins, SDK feature-band tests and exact catalog expectations; update supported SDK band to [11.0.100,11.0.200).
- `jobs/ide-parity-version-revalidation.ps1`, `docs/ide-parity-matrix.{json,md}`, `docs/hot-reload-guide.md`, `tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityMatrixContractTests.cs`, `docs/validation/producer-fingerprints.json`: active SDK mirrors; preserve historical manual status.
- `references/Hexalith.Builds`: already fast-forwarded to remote main bc3efac849d4a17c3d00cba2eee0b54771ec524e, with latest compatible telemetry/test pins and Tenants7.0.7. All other eight roots are remote-equal and clean. Root `Directory.Packages.props` imports this catalog. `eng/dependency-graph-policy.json` selected package mirrors need synchronization to actual resolved catalog without changing execution authorizations.
- Both test workspace `package.json`/`package-lock.json`: Playwright1.64.1 becomes1.65.0, including core override; preserve Node24 type declarations. Root npm already current. `.config/dotnet-tools.json`: Stryker5.0.0 becomes5.1.0; DocFX2.81.0 remains current.
- `eng/dependency-governance.md`: update current projection and compatibility explanation. .NET11-only OpenIdConnect cannot replace10.0.15 on net10; retain compatible shared catalog versions.
- `.github/workflows/{ci,release,release-publish,release-evidence}.yml`, `.releaserc.json`, `eng/release_*.py`: existing exact-source release pipeline. Production pending deployment can be approved as authenticated reviewer jpiquot with explicit user authorization, preserving environment configuration.

## Tasks & Acceptance

**Execution:**
- [x] Update SDK, Aspire, active mirrors, governance assertions, IDE fingerprint, Stryker and both Playwright locks; synchronize selected-catalog policy expectations.
- [x] Run focused governance, dependency, release-parser, npm typecheck/browserless tests, and serialized .NET build/test verification. Diagnose and fix actual failures without weakening gates.
- [ ] Primary agent: independently inspect diff, review, commit and push only after all root dependency commits are remote-verifiable.
- [ ] Primary agent: resolve exact-source CI/Quality failures, dispatch release, handle authorized reviewer approval, and verify GitHub release and eight published NuGet versions.

**Acceptance Criteria:**
- Given latest registry metadata, when toolchain checks run, then SDK11.0.100 and Aspire13.6.5 resolve and compatible package pins and locks are current.
- Given existing target frameworks, when builds and affected tests run, then they pass with vulnerabilities and analyzers enabled; historical manual evidence stays truthful.
- Given synchronized main, when release runs, then CI passes and the eight declared packages are publicly retrievable at the release version, or exact external blockers are recorded.

## Implementation Notes

- Planning approval checkpoints are already waived by the user's explicit instruction. Baseline `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json` failed at restore with NU1903 for Aspire.Hosting.AppHost13.6.4. No AppHost was running beforehand.
- GitHub jobs/log API intermittently returns502/timeouts; primary agent is investigating separately. Implementation handoff covers local source updates and validation; primary agent owns remote operations.

## Spec Change Log

## Review Triage Log

- BH1 — false: current ubuntu-latest images include SDK10.0.403 and its net10 runtimes; the unchanged primary CI and release jobs therefore retain a compatible runtime after setup-dotnet installs SDK11. Exact shared execution closures stay unchanged.
- BH2 — false: adopter proof intentionally selects .NET10 via its fixture global.json; the Ubuntu images already provide10.0.403, which satisfies latestFeature. The new dev-container compatibility installation will also preserve this local path.
- BH3 — high, patch: Testing PackageBoundaryTests contains stale Localization, Test SDK, and xUnit constants. Update all four exact expectations to the selected catalog and run its meaningful clean-consumer tests.
- BH4 — false as an unconditional pin-retention claim: the historical Firefox failure reproducer passes on the isolated1.65.0 browser, and the unchanged full three-browser Quality matrix remains required before release. Hosted matrix evidence is still pending; no publication claim is made.
- BH5 — high, patch: the declared IDE container lacks SDK11 and its post-create command uses forbidden recursive initialization and an obsolete .sln path. Add the exact SDK/compatible SDK through the official feature, retain root-only initialization, and restore the .slnx.
- BH6 — low, patch: the optional custom runtime installation directory is verified through PATH. Verify the dotnet executable in the requested directory to avoid a false failure for callers of that parameter.
- BH7 — false: every current SDK11 job installs the runtimes directly after SDK setup and before tests. Moving steps between jobs is a hypothetical future mutation, not a current reachable failure; the new guard catches missing installer steps without implementing a YAML scheduler.
- BH8 — medium, patch: the activation documentation still names an obsolete exact seal. Resolve the current policy-changing commit and document the separate activation push rather than instructing replay against the old seal.
- Edge-case reviewer: no findings. Verification-gap reviewer: no verification gaps.

## Verification

- `dotnet --version`; serialized builds and individual test modules; focused governance tests; npm clean installs/typecheck/browserless regressions; dependency-graph Python tests; `git -c core.whitespace=cr-at-eol diff --check`.
- Primary agent records CI/release URLs and verifies all package IDs in `tools/release-packages.json` against NuGet.

## Commit Scope Dispositions

- `c8abcf33705cb1ad5bcbe92e0ccb676d78062824` | `shared` | Concurrent maintainer runtime/SDK provisioning and lockfile updates preserved during rebase.

- `d5261cc4e1725869b8c457cf9a502cbaaa4879f2` | `shared` | Concurrent maintainer SDK11 pin, identical to the planned SDK change, fast-forwarded without losing local edits.

## Final Validation Record

- Concurrent upstream maintenance advanced seven roots after initial validation. All were clean fast-forwards to fetched remote main. Builds now selects compatible Microsoft.Extensions, SignalR client, TimeProvider.Testing, and System.Text.Json11.0.0; authentication/server packages remain10.0.15, Immutable10.0.15 retains its tested generator compatibility hold. Root policy and boundary expectations were refreshed before final serialized validation.
- All nested submodule status entries remain uninitialized. Default on-demand fetch attempted nested object fetching and failed with file-transport refusal; subsequent operations explicitly disable fetch.recurseSubmodules and submodule.recurse. No nested checkout was initialized or changed.
- Prior local evidence: Release build0warnings/errors, CLI157/157, focused Shell15/15, IDE4/4, package boundaries5/5, Python dependency/package191, Node265, legacy Node6, TypeScript and runtime smoke passed. The overlapping AppHost start failed MSB4018 on a Debug assembly lock; exact-path stop confirmed no running AppHost. Interrupted background test sessions are not counted as passes.

- Final catalog `bc3efac849d4a17c3d00cba2eee0b54771ec524e`: serialized Release solution build passed0warnings/errors. Individual Release modules passed Contracts227, Contracts.UI10, MCP485, Testing185, SourceTools1092, Shell3666 (5665 total, zero failures/skips); committed-source/governance/contract/performance lanes stay separate for their exact-source preconditions. SourceTools IDE4 and the toolchain guard also passed. The .NET11 Options11 test now awaits IAsyncStartupValidator.ValidateAsync; inspected exact package signatures before updating, and the full Shell lane passed.
- Review remediations passed: existing boundary class5/5 on prior selection, then complete Testing185/185 on final catalog; runtime custom-path success/missing/nonzero probes; fail-closed root-only devcontainer restore checks; activation-document parent/policy checks.

- Source AppHost validation exposed two Parties references with ExcludeAssets=all, forcing framework Logging.Abstractions10 against EventStore11. The owning-module fix retains compile assets in those two references, preserves its generator repair, passes serialized complete AppHost Debug build0warnings/errors, and is pushed/remote-verified at b6b4f8a5c83b77ab57a9b947436317162b4fb337. Its exact message passed owning pinned commitlint21.2.4; evidence /tmp/frontcomposer-parties-commitlint.log. Root selector policy refreshed after remote verification.

- Final source validation: complete serialized Debug AppHost graph passed0warnings/errors; isolated no-build Aspire start13.6.5 succeeded. Discovery reported31 Running/Healthy resources and the expected finished one-shot sample; frontcomposer-ui readiness passed. Exact-path Aspire stop completed for cleanup.

- Concurrent maintainer commit c8abcf33705cb1ad5bcbe92e0ccb676d78062824 added explicit net10 SDK companions, pinned initialization, publisher setup, and the official devcontainer lock. Rebased onto it, preserving its changes and combining both SDK/governance guards. Source-runtime package catalog is unchanged from bc3. Its shared workflow shorthand was valid newer Actions syntax but unsupported by the exact-source closure collector. Owning Builds fix7d2019adf4591a2c320a9288f76fc5f0f6905f65 uses an immutable remote action with byte-identical metadata/script; actionlint,8APTtests, pinned commitlint pass and remote main is verified. CI now uses this compatible exact workflow. New CI/Release/post-release authorizations are regenerated from combined committed sources rather than fabricating provenance.
