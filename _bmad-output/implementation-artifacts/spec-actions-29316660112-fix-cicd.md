---
title: 'Fix release run 29316660112 compatibility lifecycle'
type: 'bugfix'
created: '2026-07-14'
status: 'in-review'
review_loop_iteration: 1
baseline_commit: '6188288a0ccdf3394389019b732d630f25726925'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release job `87032154530` computed `2.1.0` and correctly stopped before packing because 109 approved 2.0 compatibility suppressions expire at `v2.1`. Published `2.0.4` now provides a clean baseline, while current `main` contains intentional post-2.0 public breaks (three Shell worker relocations and hydration-state consolidation), so releasing this code as a minor would violate SemVer.

**Approach:** Advance the compatibility lifecycle to a truthful `3.0` line: validate packages against published `2.0.4`, retire suppressions absorbed by that baseline, govern only exact breaks introduced after it, and require a valid Conventional Commit breaking signal so semantic-release selects `3.0.0`.

## Boundaries & Constraints

**Always:** Keep release/package validation blocking; preserve one-to-one JSON/XML suppression evidence; use `2.0.4` for every existing package baseline; retain exact `targetRelease: v3.0` evidence for intentional public breaks; deliver with a commit/PR title using `type!:` or a proper `BREAKING CHANGE:` footer.

**Ask First:** Restoring compatibility shims to ship `2.1`, changing any public API beyond the already-landed breaks, changing release triggers/publish policy, or rewriting published `main` history.

**Never:** Extend the expired 2.0 suppressions, relabel v3 breaks as v2.1, weaken ApiCompat/commitlint/semantic-release gates, accept unnecessary suppressions, hand-edit `CHANGELOG.md`, publish during verification, or modify submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Next major | `--version 3.0.0` with reviewed current ledger | Plan and eight-package pack use baseline `2.0.4` and exact v3 suppressions | Any untracked break or stale suppression blocks packing |
| Wrong minor | `--version 2.1.0` with v3 breaks present | Release plan is rejected before build/publish | Error identifies the pre-target/current-line mismatch |
| Next lifecycle boundary | `--version 3.1.0` | v3 suppressions are expired | Release remains blocked pending a fresh baseline review |

</frozen-after-approval>

## Code Map

- `Directory.Build.targets` and `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj` -- package-validation baseline policy.
- `src/Hexalith.FrontComposer.{Contracts,Shell}/CompatibilitySuppressions.xml` -- exact ApiCompat suppressions consumed during pack.
- `docs/diagnostics/{README.md,compatibility-suppressions.json}` -- documented baseline policy and governed release-line ledger mirrored one-to-one with XML.
- `eng/pack_release_packages.py` and `tests/eng/test_pack_release_packages.py` -- pre-publish release-line enforcement and focused tests.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`, `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`, and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- ledger and evaluated-baseline governance.
- `_bmad-output/implementation-artifacts/11-16-hydration-state-compatibility-evidence.md` -- consumer-facing break evidence whose stale 2.0 claim must name 3.0.

## Tasks & Acceptance

**Execution:**
- [x] `Directory.Build.targets`, `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`, and `docs/diagnostics/README.md` -- advance and document existing-package validation at `2.0.4` -- compare 3.0 only with the latest published 2.x surface.
- [x] `src/Hexalith.FrontComposer.Contracts/CompatibilitySuppressions.xml`, `src/Hexalith.FrontComposer.Shell/CompatibilitySuppressions.xml`, and `docs/diagnostics/compatibility-suppressions.json` -- remove obsolete 1.12-to-2.0 rows, generate/review exact 2.0.4-to-current breaks (including five CP0001 and fifteen CP0002 hydration diagnostics), set `currentRelease` to `v3.0`, and keep expiry at `v3.1` -- prevent stale or broadened exceptions.
- [x] `tests/eng/test_pack_release_packages.py`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`, `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`, and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` -- update happy/pre-target/expiry/current-line cases, evaluated baselines, and one-to-one evidence assertions -- lock the new lifecycle and reject `2.1`.
- [x] `_bmad-output/implementation-artifacts/{11-16-fatal-hydration-json-and-generated-literal-helper-consolidation.md,11-16-hydration-state-compatibility-evidence.md}` -- correct the release boundary and required Conventional Commit example to 3.0 -- keep adopter guidance truthful.

**Acceptance Criteria:**
- Given published `2.0.4` packages and current source, when the release inventory packs version `3.0.0`, then all eight `.nupkg` and `.snupkg` files pass package validation with no untracked break or unnecessary suppression.
- Given the proposed delivery subject, when commitlint evaluates it, then it is valid and its breaking marker causes semantic-release to select the 3.0 line.
- Given workflow and release configuration diffs, when scope is inspected, then triggers, permissions, reusable workflows, evidence gates, and publishing commands are unchanged.

## Spec Change Log

## Design Notes

The linked failure is the first visible row, not the whole defect. Renewing its expiry would next encounter three future-target rows and would conceal public breaks shipped after `2.0.4`. Rebaselining removes historical 2.0 noise while keeping the guard meaningful for the actual 3.0 delta.

## Verification

**Commands:**
- `python3 -m unittest tests/eng/test_pack_release_packages.py` -- expected: lifecycle plan tests pass, including 2.1 rejection and 3.1 expiry.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --filter FullyQualifiedName~CompatibilitySuppression` -- expected: ledger/XML governance passes.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --filter "Category=Governance"` -- expected: CI/release governance remains green.
- `python3 eng/pack_release_packages.py --version 3.0.0-ci.fix --output /tmp/frontcomposer-release-3.0` -- expected: eight package and eight symbol artifacts validate without publishing.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release -m:1 /nr:false` -- expected: zero warnings and errors.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: the complete blocking default lane passes.
- `printf '%s\n' 'fix!: advance compatibility baseline to 3.0' | npx commitlint --verbose` -- expected: zero errors and a valid breaking subject.

**Observed 2026-07-14:**

- Release lifecycle unit tests passed 4/4; exact suppression-policy tests passed 2/2; Contracts.UI baseline test passed 1/1; Shell Governance passed 143/143.
- The `3.0.0-ci.fix` dry pack produced and validated eight `.nupkg` plus eight `.snupkg` artifacts against published `2.0.4`; the ledger/XML parity check confirmed 20 hydration diagnostics plus three worker moves, all targeted at `v3.0` and expiring at `v3.1`.
- Release solution build completed with zero warnings/errors; the blocking default lane passed 4,100/4,100 tests; docs validation passed.
- Commitlint accepted `fix!: advance compatibility baseline to 3.0` with zero problems/warnings; workflow, release-configuration, and `CHANGELOG.md` diffs remained empty; `git diff --check` passed.
