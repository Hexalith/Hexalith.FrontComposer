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
- [x] Primary agent: independently inspect diff, review, commit and push only after all root dependency commits are remote-verifiable.
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

- Merged runtime guard checks pass20/20. Genuine source closures: CI a11ce4e2f867cc4ab2a0c1833b2dae42a83efecd0131ee3bcc74456ae92b33ce (19sources); Release acb2623d2acb3e4096569f0cad8df11031fb4d786d5aa2b8dc0ff5ccb9dfb4fc (12sources); post-release c09ed6628d764df5854da0ee3eb3d16bb40d64c3331c20a60e9ad1f45bf68bd9 (17sources). Each blob was independently recomputed from committed exact source; all current caller hashes match, legacy rows preserved. The generated configuration projection was corrected from actual committed bytes; only the intended AppHost SDK configuration differed, and committed graph validation passed.

- Sealed dependency and workflow policy landed remotely at6d4c5ed1d4019e531222305fb2493b9751f78544. Exact committed graph validation passed. This documentation-only activation has that seal as its immediate parent and changes no selector, executable source, consumer configuration, or policy bytes; exact-source hosted CI and publication remain pending. No push reported bypassed branch-rule warnings.

- Release preparation first correctly refused5.0.0 because currentRelease still declaredv4.7. Semantic Release had independently planned5.0.0 from existing breaking history. Advanced the empty compatibility ledger to v5.0, retained baseline4.7.2 and all API-compatibility checks, updated two real-tree Python expectations and the exact diagnostic-ledger test while retaining synthetic4.7 fixtures.60 focused packer/prepublish tests, the diagnostic-ledger check, and affected SourceTools build pass.

- Hosted CI43104990202/43106141962 exposed a cold-cache NU1101 failure in the clean Testing consumer (five external Configuration/Authorization dependencies absent from fallback). The fixture now restores only external dependencies through approved repository feeds in a separate project before the unchanged isolated offline local-feed restore. Cold-cache regression starts with zero package entries and passes1/1;91external IDs are warmed, all four unique FrontComposer versions remain absent from fallback and resolve only through the local packed feed. Narrow Version5.0.0 build passes0warnings/errors. Evidence /tmp/frontcomposer-cold-cache-boundary-evidence.json.
- Complete local prepublication run on f74bf648 passed6468tests, eight package/symbol packs with API compatibility enabled, both consumer boundaries, SBOM and symbol checks. Manifest correctly refuses the local sentinel and missing authenticated CI/Release handoffs; no publication attempted.

- Hosted live-browser failure reproduced: SignalR.Common11 netstandard fallback lacks IInvocationBinder.GetTarget(ReadOnlySpan<byte>) required by Blazor10, so negotiation succeeds but the circuit closes before first interactive render. Owning Builds catalog0677db83636bca702d74c212c72f8b08a84616f3 retains Client/Common10.0.15, preserves existing Authorization10 ABI hold, keeps SDK11/Extensions11, and records latestStable11 honestly in audit. Central318-entry and audit checks pass; real rebuilt Chromium page transitions from interactive=false to true with zero missing-method exceptions. CI execution stays on independently authorized7d2019ad; only the selected catalog/source projection advances.

- Final compatible catalog Release build with explicit5.0.0 version passes0warnings/errors; full MCP531/531 and Testing185/185 pass, including external-only cache preparation. Browser interactivity was verified against the rebuilt compatible SignalR assets. Prepared earlier local artifacts remain non-publishable; production workflow must build/seal its own exact-source candidate.

- Compatible catalog/policy seal a715f2be4799daed6ecd7204d49480ed67aafe3a is remote and committed graph validation passes. This documentation-only activation preserves that exact graph and all executable workflow bytes; hosted CI/Quality must pass for this new source before release dispatch.

- Hosted CI43110622179 exposed duplicate parallel SourceTools builds: six analyzer edges explicitly set TargetFramework for a single-target project, producing a second global-property instance sharing obj/bin. Removed only redundant SetTargetFramework metadata and retained analyzer/reference/private flags. SDK11 evaluation now undefines TargetFramework consistently with ordinary references. Isolated clean parallel Release build with -warnaserror -m:4 passed0warnings/errors and compiled SourceTools exactly once; real generator and Debug/Release packaged-consumer tests passed41/41. Evidence /tmp/frontcomposer-analyzer-parallel-nu5mre0_/. Consumer configuration digest was recomputed from Git-filtered blobs to351f72ea6ec234036987941fda3001f9161cb40307ab4e8d2adfefaed19b5abd; execution closures are unchanged.

- Owning Builds CI43109751484 found six declaration failures because the selected catalog uses Tenants7.0.7 while retained release authority/fixtures stopped at7.0.6. Genuine GitHub release/tag and NuGet nuspec all identify7.0.7 atbcee4b4dbd886b7b718843cfb80419a92d33de15, with byte-identical declaration SHA2561588f3428ec29296c452fdd35bf95146142315aad3ad8ceb934e7a791f1fb522. Owning fix430784578957e0c10fef9368a0fadc35dc33eea9 adds that explicit authority and fixture, preserves prior releases/checks, and is pushed/remote-verified. Release build0warnings/errors,38declaration tests,52CI contracts,79source-CI scenarios,31-project authority validation,32validator scenarios and exact pinned commitlint pass. Root selector/source projection updated; package catalog bytes and immutable execution pins stay unchanged.

- Hosted a271eb0c retry CI43110622179 attempt2 passed build, consumer validation, unit tests and dependency governance, emitted authenticated dependency-release-handoff-43110622179-2. CodeQL and Commitlint passed; Quality Epic9 live acceptance and DEBUG lane passed. Remaining Quality lanes continue before the final exact-source push/release. Owning Parties CI43102711596 fails precompile on pre-existing mismatched nested Platform/Builds identities; the exact two-gitlink repair is awaiting explicit nested authorization required by AGENTS.md. No nested checkout was initialized or changed.
