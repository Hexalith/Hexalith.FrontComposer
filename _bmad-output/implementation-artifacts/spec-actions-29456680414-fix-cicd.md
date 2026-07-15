---
title: 'Fix cold-cache MCP package-boundary validation in CI'
type: 'bugfix'
created: '2026-07-16'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'c410e4d109ca266b65c5525afd3960af68e488e8'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** CI run `29456680414` initially failed while NuGet indexed newly published EventStore and Memories packages. That propagation race has cleared and the follow-up main run now restores, builds, and validates consumers, but consistently fails Tier 1 because `McpRuntimePackageBoundaryTests` enables the published `3.0.0` ApiCompat baseline only after invoking `dotnet pack --no-restore`; a fresh runner therefore lacks the baseline package that the SDK expects in its global cache.

**Approach:** Make the MCP package-boundary test's nested pack acquire its configured baseline through the normal restore path before package validation. Preserve the production release validation policy and prove the test succeeds from an empty NuGet cache rather than relying on developer or runner cache state.

## Boundaries & Constraints

**Always:** Keep `EnableFrontComposerPackageValidation=true`, the published `3.0.0` baseline, package assembly byte comparison, exported-type checks, and embedded-resource checks blocking. Keep the change in the FrontComposer parent repository and preserve existing submodule worktrees.

**Ask First:** Any change to the package-validation baseline, release/package workflow, shared Hexalith.Builds workflow, package inventory, or submodule content/pointers beyond syncing the already-landed `c410e4d1` parent baseline.

**Never:** Disable or skip ApiCompat, copy a baseline package from a machine-specific cache, switch Release CI to source project references, roll dependency versions back, weaken the Tier 1 test, or modify/publish submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Cold runner | Published MCP `3.0.0` baseline is absent from the global NuGet cache | Nested pack restores the baseline, validates the review package, and inspects its assembly | Restore or validation failure remains a blocking test failure with subprocess output |
| Warm runner | Baseline already exists in the cache | The same pack and boundary assertions pass without behavior differences | Stale cache state must not bypass ApiCompat or assembly checks |
| Baseline unavailable | Configured baseline cannot be restored from package sources | Test fails closed before accepting the package | Preserve actionable restore/package-validation diagnostics |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs` -- owns the nested MCP pack and the package/runtime boundary assertions; its `--no-restore` assumption causes the cold-cache failure.
- `Directory.Build.targets` -- maps the opt-in validation flag to SDK package validation and the published `3.0.0` baseline; policy remains unchanged.
- `eng/pack_release_packages.py` -- production reference flow: build with validation enabled performs restore before a no-build pack.
- `.github/workflows/ci.yml` -- shared Tier 1 caller that exposes the clean-runner failure; workflow shape remains unchanged.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs` -- allow the nested pack to restore with its existing package-validation properties before validation -- remove the hidden dependency on a pre-populated global package cache while retaining every package-content assertion.

**Acceptance Criteria:**
- Given an empty NuGet package cache and published `Hexalith.FrontComposer.Mcp` version `3.0.0`, when the focused boundary test runs, then restore obtains the baseline and ApiCompat plus all assembly/resource assertions pass.
- Given the MCP Tier 1 project on the current main baseline, when its Release tests run in the shared-CI shape, then all tests pass without disabling validation or adding a workflow-only prefetch.
- Given the final diff, when scope is inspected, then package baseline policy, workflows, release scripts, dependency pins, and submodule pointers/content are unchanged.

## Spec Change Log

## Design Notes

The SDK materializes `PackageValidationBaselineVersion` as a package download during restore. The ordinary solution restore does not enable FrontComposer package validation, so the nested operation must participate in restore using the same properties it uses for pack. A self-contained test fixes local, CI, and future runner behavior; a workflow prefetch would only mask the test defect in one execution environment.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release -p:NuGetAudit=false -m:1 /nr:false` -- expected: clean Release build with zero warnings/errors.
- `dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~McpRuntimePackageBoundaryTests.PackagedRuntimeAssembly_MatchesInspectedReleaseBoundary"` -- expected: the test-owned cold cache restores the `3.0.0` baseline, a second warm-cache pack passes, and package-boundary assertions pass.
- `dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~McpRuntimePackageBoundaryTests.PackageValidation_MissingBaseline_FailsWithActionableRestoreDiagnostics"` -- expected: the unavailable-baseline case fails closed with actionable `NU1102` diagnostics and the contract test passes.
- `dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Release --no-restore` after the focused package test -- expected: the nested restore leaves shared project restore metadata intact.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release --no-build --no-restore` -- expected: complete MCP Tier 1 project passes.
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false` followed by `dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore -m:1 /nr:false` -- expected: CI-equivalent Release restore/build succeeds.
- `git diff --check` -- expected: no whitespace errors in tracked changes.
- `git status --short` plus baseline-scoped diff inspection -- expected: the approved MCP test/spec changes are identifiable separately; concurrent AppHost/audit-policy work is untouched, and package policy, workflows, dependency pins, release scripts, and submodules have no changes from this story.

**Observed 2026-07-16:** Release test-project build passed with zero warnings/errors; the focused package-boundary method passed 1/1 with isolated NuGet/build artifacts, restored the published `3.0.0` baseline into its cold cache, and repeated the pack against the warm cache; an immediate `--no-restore` rebuild passed, proving the nested restore did not contaminate shared project assets; the missing-baseline method passed 1/1 by verifying a nonzero nested-pack result with `NU1102`, package ID, and requested-version diagnostics; nested packs are bounded by a two-minute timeout with process-tree termination; the full MCP project passed 365/365; Release solution restore/build passed with zero warnings/errors; the scoped diff check passed. Concurrent AppHost/audit-policy worktree changes were preserved and excluded from this implementation.

## Suggested Review Order

**Cache-independent validation**

- Start here: proves validation restores a cold baseline and repeats from a warm cache.
  [`McpRuntimePackageBoundaryTests.cs:41`](../../tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs#L41)

- The helper isolates NuGet and MSBuild artifacts while preserving validation properties.
  [`McpRuntimePackageBoundaryTests.cs:112`](../../tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs#L112)

**Failure behavior**

- An unavailable baseline must fail closed with package-specific restore diagnostics.
  [`McpRuntimePackageBoundaryTests.cs:20`](../../tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs#L20)

- Timeout cancellation terminates the entire nested process tree.
  [`McpRuntimePackageBoundaryTests.cs:142`](../../tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs#L142)
