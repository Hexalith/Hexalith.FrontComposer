---
created: 2026-07-11
epic: 11
story: 14
story_key: 11-14-update-architecture-context-ux-and-package-compat-docs
source_epics: _bmad-output/planning-artifacts/epics.md
baseline_commit: f1d8d73edc7fe69cf3cc3220ec5b29f144c55c37
review_patch_baseline_commit: cf3ff8ca
status: done
---

# Story 11.14: Update Architecture, Project Context, UX Trace, and Package Compatibility Docs

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a release owner,
I want the kernel split and query migration reflected in planning and published references,
so that adopters understand the new package boundaries and compatibility story.

## Acceptance Criteria

1. **Given** Stories 11.11-11.13 change package boundaries or public contracts, **when** documentation is updated, **then** `project-context.md`, architecture layer documentation, UX-DR1's home for `Typography`/`FcTypoToken`, release notes, and package-compat guidance reflect the approved shape.

2. **Given** docs are changed, **when** documentation validation runs, **then** generated planning docs remain under `_bmad-output/` and published docs under `docs/` are updated only where product references require it.

## Tasks / Subtasks

- [x] Reconcile the implemented 11.11-11.13 state before changing documentation. (AC: 1, 2)
  - [x] Read the Story 11.8 compatibility contract and the complete Story 11.11, 11.12, and 11.13 specs before editing; use the live source and package tests as current-state evidence rather than copying their planned wording.
  - [x] Confirm the final project/package names, target frameworks, project references, moved public types, retained kernel seams, and `QueryRequest` compatibility behavior against the current tree.
  - [x] Record the current status mismatch honestly: 11.11 is implemented but blocked by the Story-11.14-owned release inventory omission, 11.12 is done, and 11.13 is in review. Do not mark another story done or rewrite its review evidence outside its workflow.
  - [x] Preserve the existing unrelated modification to `_bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md`; do not absorb it into this story.

- [x] Repair the explicit release package inventory and its package-only evidence. (AC: 1)
  - [x] Add `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj` / `Hexalith.FrontComposer.Contracts.UI` to `eng/release-package-inventory.json` as `packable: true` with `symbol_required: true`, adjacent to Contracts. Keep semantic-release as the version source and add no inline package version.
  - [x] Update `CiGovernanceTests.PackageInventory_IsExplicitLockstepAndReviewable` to require `Hexalith.FrontComposer.Contracts.UI` explicitly while preserving dynamic discovery, unexpected-project rejection, symbols, and release-model assertions.
  - [x] Update `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` so its clean local-feed consumer packs/restores Contracts.UI before Shell and Testing and verifies the expected dependency/assets graph without repository-relative project references.
  - [x] Audit release-definition/package-set hash fixtures under `tests/ci-governance/fixtures/`; update only values that intentionally represent the live inventory hash, then reseal/recompute dependent synthetic fixture fields using the repository helper rather than hand-waving mismatches.
  - [x] Prove `eng/pack_release_packages.py` now includes Contracts.UI and produces its `.nupkg` and required `.snupkg`; do not publish packages from this story.

- [x] Update canonical planning and generated project knowledge to the observed package boundary. (AC: 1, 2)
  - [x] Update `_bmad-output/project-context.md`: remove future/planned split language; record the UI-clean dual-TFM Contracts kernel, packable net10 Contracts.UI package, packable netstandard SourceTools analyzer, current direct references, 11.12 ownership moves, composed query migration, public-API baselines, and actual package inventory. Reconcile stale package pins only from current centralized props.
  - [x] Update `_bmad-output/planning-artifacts/architecture.md`: make Layer 0A and dependency direction current, add the 11.12 ownership boundary and 11.13 `ProjectionQuery`/`QueryRequest` composition, and identify 11.14 as documentation/inventory evidence rather than future assembly implementation.
  - [x] Update `_bmad-output/project-docs/architecture.md`, `source-tree-analysis.md`, `component-inventory.md`, `data-models.md`, and `contribution-guide.md` wherever they still place UI/runtime/testing types in Contracts or describe the split as future. Update `project-overview.md`, `deployment-guide.md`, or `api-contracts.md` only if their current-state claims are demonstrably stale.
  - [x] Regenerate or synchronize `_bmad-output/implementation-artifacts/epic-11-context.md` if changed planning facts make its current-state summary stale. Keep completed story/proposal artifacts as provenance unless they incorrectly claim to be current guidance.
  - [x] Do not put generated planning/reference material under `docs/`; `_bmad-output/` remains the generated planning/knowledge home.

- [x] Update the UX-DR1 trace without changing visual behavior. (AC: 1)
  - [x] Update `_bmad-output/planning-artifacts/ux-design.md` so UX-DR1 names `Hexalith.FrontComposer.Contracts.UI` as the package/assembly home for `Typography`, `FcTypoToken`, and related Fluent mappings while retaining their existing public namespaces.
  - [x] Preserve the nine typography roles, `TypographyMappingVersion = "3.1.0"`, Fluent v5 text size/weight/tag semantics, density behavior, `--fc-spacing-unit`, and all accessibility/theme rules unless live 11.11 evidence proves a deliberate change.
  - [x] Update `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md` only where ownership/package trace is useful; record a no-update rationale for `ux-experience-2026-07-05.md` if its interaction behavior remains correct.

- [x] Publish adopter-facing package and query migration guidance. (AC: 1, 2)
  - [x] Add a complete versioned migration page under `docs/migrations/` for the release-owner-approved edge (expected `1.12` to `2.0`, subject to the explicit version decision below). It must satisfy the repository's required headings: Affected Versions, Why This Changed, Old Code, New Code, Analyzer And Code Fix, and Skill Corpus Evidence.
  - [x] Include a complete old-to-new ownership table covering all 25 Story 11.12 moves plus the 11.11/11.13 surfaces. Name every identity and destination: unchanged rendering/shortcut FQNs now supplied by Contracts.UI; `InMemoryStorageService` in Testing; `FcShellOptions`, `FcShellDevModeOptions`, `CustomizationContractValidationMode`, and `InlinePopoverRegistry` in Shell; all 18 DataGrid actions and both expanded-row actions listed in the ownership map below; and `ProjectionQuery` composed through `QueryRequest.Create`.
  - [x] State compatibility precisely: adding the Contracts.UI reference preserves source namespaces for moved UI types, but their assembly identity move without type forwarding is binary-breaking; 11.12 namespace/assembly moves require source edits; HFC0001/CS0618 shims retain the v1.12 flattened query constructor/properties/deconstruction throughout 2.x with removal targeted for 3.0; direct JSON stays flat with no `criteria` member.
  - [x] State the non-changes explicitly: no incidental change to `IQueryService`, Testing callback signatures, EventStore body/headers/cache behavior, MCP output/descriptors, schema fingerprints, CLI JSON, generated routes/output paths, or Pact wire shape.
  - [x] Update `docs/migrations/index.md`, `docs/toc.yml`, and relevant how-to/index links; update `docs/concepts/source-generation-and-mcp-split.md` with the kernel/UI/analyzer/runtime dependency direction.
  - [x] Update `docs/skills/frontcomposer/setup/package-and-hosting.md` so agent setup chooses Contracts, Contracts.UI, Shell, Testing, SourceTools, and MCP by responsibility while preserving the MCP fail-closed gate instructions and skill-corpus front-matter/section contract.
  - [x] Update `docs/reference/api/index.md` if it does not expose the now-generated Contracts.UI API area. Do not hand-edit generated `docs/reference/api/*.yml` or `docs/_site/**`; regenerate API metadata through the documented DocFX flow.
  - [x] Run the doc-drift sweep and record updated pages plus explicit no-update decisions for plausible but already-correct tutorials, references, contracts, and UX artifacts.

- [x] Resolve and evidence the release-version/release-note posture. (AC: 1)
  - [x] Reconcile the approved plan's stale “pre-v1.0” assumption with the actual latest stable tag and changelog (`v1.12.0`). Do not publish or describe the no-forwarder assembly moves as an ordinary backward-compatible minor release.
  - [x] Record Release Owner approval for the major-version posture (expected `2.0.0`) or an explicit documented alternative as a dated `approved` contract under `_bmad-output/contracts/` (either a signed amendment to the 11.8 compatibility plan or a dedicated release-version decision). Block release completion if that owner decision is absent; the dev agent must not self-approve it. The implementation/release commit range must contain a valid Conventional Commit breaking-change signal (`!` or `BREAKING CHANGE:`) so semantic-release can generate truthful release notes.
  - [x] Keep `CHANGELOG.md` semantic-release-owned. Do not hand-author a fake released section; supply durable checked-in migration/package guidance and verify the semantic-release dry-run classifies the change and generated notes as breaking.
  - [x] Document the intentional prerelease-dependency posture while Fluent UI v5 and bUnit remain prerelease. Do not present NU5104 suppression as proof that stable-on-prerelease dependencies are risk-free.
  - [x] Replace the stale shared `FrontComposerPackageValidationBaselineVersion` default of `0.1.0` with evidence against the actual latest stable baseline `1.12.0` for existing packages. Give first-release Contracts.UI a narrow project-specific no-baseline path with a named removal trigger after its first major release; do not disable validation globally. Update governance/package tests so completion fails if existing packages still validate against `0.1.0`, Contracts.UI tries to download a nonexistent baseline, or an intentional compatibility suppression is broad/untracked.

- [x] Validate package, docs, compatibility, and story evidence before review. (AC: 1, 2)
  - [x] Run the release inventory command and focused `CiGovernanceTests`; require a valid inventory that includes Contracts.UI.
  - [x] Run Release builds and focused tests for Contracts, Contracts.UI, SourceTools, Shell, and Testing. Reuse existing package/public-API/clean-consumer tests; do not duplicate 11.11-11.13 behavior tests.
  - [x] Pack the full explicit inventory at one non-published test version and build a clean external consumer that exercises kernel-only, UI, Shell/Testing, and canonical `ProjectionQuery` usage.
  - [x] Run `pwsh ./eng/validate-docs.ps1` with DocFX, metadata, snippets, links, diagnostic registry, and warnings-as-errors enabled. Verify generated API ownership shows Contracts.UI for typography/rendering/shortcut types and the canonical query surface.
  - [x] Run each affected/default test project individually with `DiffEngine_Disabled=true`, then run the Release `.slnx` build, `git diff --check`, and the story artifact validator. Use the `.slnx` for restore/build only, not solution-level `dotnet test`.
- [x] Reconcile the story File List, documentation drift sweep, release evidence, and exact blockers against the actual diff before moving to review.

### Review Findings

- [x] [Review][Patch] [High] Allow no migration diagnostic for manual package moves and preserve each exact ApiCompat CP identity — remove the unrelated query-only `HFC0001` claim from package/type/member breaks, record `CP0001`/`CP0002`/`CP0008` in the governed ledger, and reconcile that identity with each XML suppression. Evidence: `docs/diagnostics/compatibility-suppressions.json` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`.
- [x] [Review][Patch] [High] Retain the flattened `QueryRequest` compatibility shims throughout 2.0 and advance their documented removal target to 3.0.0. Evidence: `src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs`, `_bmad-output/contracts/fc-2-0-release-version-decision-2026-07-11.md`, and `docs/migrations/1.12-to-2.0.md`.
- [x] [Review][Patch] [High] Enable the 1.12 package-validation gate in the semantic-release pack path and verify evaluated MSBuild behavior. Evidence: `eng/pack_release_packages.py`.
- [x] [Review][Patch] [Medium] Enforce compatibility-suppression expiry against the current release, not only against each row's target release. Evidence: `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`.
- [x] [Review][Patch] [High] Correct suppression-ledger destinations that falsely claim Story 11.12 namespaces were preserved. Evidence: `docs/diagnostics/compatibility-suppressions.json`.
- [x] [Review][Patch] [Medium] Replace the non-compiling three-argument `LoadPageAction` examples with valid migration code. Evidence: `docs/migrations/1.12-to-2.0.md`.
- [x] [Review][Patch] [Medium] Make the Testing clean consumer local-feed/offline-only as required by the story. Evidence: `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`.
- [x] [Review][Defer] [Low] Contracts.UI dependency proof can false-pass broken dependency metadata because it uses substring nuspec checks and directly references Fluent [`tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs:79`] — deferred, pre-existing
- [x] [Review][Defer] [Medium] Generated project guidance still recommends forbidden solution-level test execution [`_bmad-output/project-docs/development-guide.md:56`] — deferred, pre-existing
- [x] [Review][Defer] [Medium] Generated project index still claims NuGet packages are signed while the live release guide says they are unsigned [`_bmad-output/project-docs/index.md:31`] — deferred, pre-existing

## Dev Notes

### Story Context and Implementation Gate

Epic 11 closes architecture-review release risks without reopening completed product epics. Story 11.8 approved a UI-clean Contracts kernel plus a net10-only Contracts.UI package. Stories 11.11-11.13 implement the package split, runtime/testing ownership cleanup, and composed query migration; Story 11.14 publishes the truthful compatibility story and repairs the explicit release inventory. [Source: `_bmad-output/planning-artifacts/epics.md` — Epic 11 and Story 11.14; `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`]

The story is context-ready, but implementation must start by reconciling actual source/package evidence:

- Story 11.11's spec is blocked because the new packable Contracts.UI project is absent from `eng/release-package-inventory.json`. The implementation is already present in commit `4d24036d`; do not recreate the project or move the types again.
- Story 11.12 is done and intentionally moved 25 runtime/testing-owned public identities without upward type forwarding or duplicate compatibility types.
- Story 11.13 is in review and retains the v1.12 flat `QueryRequest` source/JSON surface as HFC0001/CS0618 shims while repository code uses `ProjectionQuery`.
- The inventory repair is part of this story and is required to unblock Story 11.11, Story 11.5's broad gate, release packing, and Testing's clean packed consumer.

Do not claim the package-boundary implementation set complete from sprint labels alone. Completion requires live inventory, package, consumer, public-API, query-wire, docs, and version evidence.

### Brownfield Package and Contract Snapshot

| Surface | Current implemented ownership | Must remain true |
| --- | --- | --- |
| Contracts | `Hexalith.FrontComposer.Contracts`, `net10.0;netstandard2.0` | Both faces remain UI-clean; no Contracts.UI, ASP.NET Components, Fluent, Shell, or Testing dependency. Retains wire/attribute/schema/diagnostic contracts plus UI-neutral seams such as `IStorageService`, `IInlinePopover`, and `GridViewSnapshot`. |
| Contracts.UI | `Hexalith.FrontComposer.Contracts.UI`, packable `net10.0` | References Contracts, `Microsoft.AspNetCore.App`, and Fluent UI. Owns the moved typography, render-fragment projection contexts/delegates/descriptors, and keyboard-event shortcut surface under unchanged `Hexalith.FrontComposer.Contracts.*` namespaces. |
| SourceTools | `Hexalith.FrontComposer.SourceTools`, packable `netstandard2.0` analyzer | References and embeds only SourceTools + Contracts; generated consumer code may require Contracts.UI, but the analyzer must not. |
| Shell | `Hexalith.FrontComposer.Shell`, `net10.0` | References Contracts + Contracts.UI; owns shell options, `InlinePopoverRegistry`, DataGrid actions, and expanded-row actions. |
| Testing | `Hexalith.FrontComposer.Testing`, packable `net10.0` | References Contracts + Shell; owns `InMemoryStorageService` and its public API baseline. |
| Schema / MCP / CLI | Kernel consumers | Schema references Contracts; MCP references Contracts + Schema; CLI has no project references. Do not add Contracts.UI without separately approved evidence. |
| Query contracts | `ProjectionQuery` + `QueryRequest` in Contracts | Criteria are composed, legacy flattened source remains throughout 2.x with removal targeted for 3.0, and JSON/EventStore/MCP/Testing outward behavior stays compatible. |

The Contracts.UI `PublicAPI.Shipped.txt`, Contracts kernel ownership/package tests, SourceTools packaged-analyzer test, Testing public API/package test, and Story 11.13 query compatibility tests already exist. Extend only where the Story 11.14 inventory or documentation boundary exposes a real gap.

### Complete Story 11.12 Ownership Map

The migration guide must enumerate these identities explicitly; a generic “actions moved to Shell” sentence is insufficient compatibility guidance:

| Old owner | New owner | Identities |
| --- | --- | --- |
| `Hexalith.FrontComposer.Contracts.Storage` | `Hexalith.FrontComposer.Testing` | `InMemoryStorageService` |
| `Hexalith.FrontComposer.Contracts` | `Hexalith.FrontComposer.Shell.Options` | `FcShellOptions`, `FcShellDevModeOptions`, `CustomizationContractValidationMode` |
| `Hexalith.FrontComposer.Contracts.Rendering` | `Hexalith.FrontComposer.Shell.Services` | `InlinePopoverRegistry` |
| `Hexalith.FrontComposer.Contracts.Rendering` | `Hexalith.FrontComposer.Shell.State.DataGridNavigation` | `CaptureGridStateAction`, `RestoreGridStateAction`, `ClearGridStateAction`, `PruneExpiredAction`, `ColumnFilterChangedAction`, `StatusFilterToggledAction`, `GlobalSearchChangedAction`, `SortChangedAction`, `FiltersResetAction`, `LoadPageAction`, `LoadPageSucceededAction`, `LoadPageNotModifiedAction`, `LoadPageFailedAction`, `LoadPageCancelledAction`, `ClearPendingPagesAction`, `ColumnVisibilityChangedAction`, `ResetColumnVisibilityAction`, `ScrollCapturedAction` |
| `Hexalith.FrontComposer.Contracts.Rendering` | `Hexalith.FrontComposer.Shell.State.ExpandedRow` | `ExpandRowAction`, `CollapseRowAction` |

Retained inward seams are not moves: `IStorageService`, `IInlinePopover`, and `GridViewSnapshot` remain in Contracts. The migration guide must not tell adopters to replace or remove those interfaces/data contracts.

### Release and Compatibility Guardrails

- The repository is already tagged `v1.12.0`; “pre-v1.0 break” is stale planning language, not an acceptable release rationale.
- Moving public types to another assembly without `TypeForwardedTo` is binary-breaking. Type forwarding is intentionally unavailable here because it would make Contracts depend upward on Contracts.UI and restore the Fluent dependency that the split removes.
- Rendering/shortcut source retains the old namespaces, so adopters generally add the Contracts.UI package reference without namespace changes. The 11.12 owner moves do require new namespaces/package references.
- `QueryRequest` is different: it deliberately retains legacy signatures and exact flat JSON while HFC0001 guides source migration to `ProjectionQuery`; do not imply those shims were removed by the package split.
- Stable packages depending on prerelease Fluent/bUnit packages intentionally trigger NU5104. Existing scoped suppressions do not remove the dependency-resolution risk; the release notes/package guide must describe the posture.
- Do not change product behavior, type namespaces, package IDs, schema canonicalization, generated snapshots, Pacts, public API baselines, or diagnostic identity merely to simplify documentation.

### Current Files To Read Before Editing

Read every likely UPDATE file completely before editing it:

- `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`
- `_bmad-output/implementation-artifacts/spec-11-11-create-contracts-ui-assembly-and-migrate-blazor-rendering-surface.md`
- `_bmad-output/implementation-artifacts/spec-11-12-relocate-runtime-and-testing-owned-types-out-of-contracts.md`
- `_bmad-output/implementation-artifacts/spec-11-13-decompose-queryrequest-through-hfc0001-migration-path.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/project-context.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/ux-design.md`
- `_bmad-output/project-docs/{architecture,source-tree-analysis,component-inventory,data-models,contribution-guide,project-overview,deployment-guide,api-contracts}.md`
- `src/Hexalith.FrontComposer.{Contracts,Contracts.UI,SourceTools,Shell,Testing,Mcp,Schema}/*.csproj`
- `src/Hexalith.FrontComposer.Contracts.UI/PublicAPI.Shipped.txt`
- `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`
- `eng/release-package-inventory.json`
- `eng/pack_release_packages.py`
- `eng/release_evidence.py`
- `Directory.Build.targets`
- `.releaserc.json`
- `.github/workflows/release.yml`
- `CHANGELOG.md`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`
- `tests/ci-governance/fixtures/release-readiness-cases.json`
- `tests/ci-governance/fixtures/release-manifest-valid.json`
- `docs/docfx.json`
- `docs/toc.yml`
- `docs/concepts/source-generation-and-mcp-split.md`
- `docs/how-to/migration-guides.md`
- `docs/migrations/index.md`
- `docs/skills/frontcomposer/setup/package-and-hosting.md`
- `docs/reference/api/index.md`
- `docs/diagnostics/HFC0001.md`

### What Each Primary UPDATE Must Preserve

- `_bmad-output/project-context.md`: update only stale current-state facts; preserve generator IR, schema, MCP security, UX, testing, Git/submodule, and release guardrails.
- Architecture docs: change planned/future package wording to observed/current dependency direction; preserve all runtime, security, schema, generated-output, Fluxor, EventStore, and UI governance invariants.
- UX docs: change ownership trace, not typography semantics, density behavior, theme rules, or accessibility behavior.
- Release inventory/governance: add the missing package without weakening unexpected-project, symbols, drift, manifest, or publish gates.
- Testing package consumer: add Contracts.UI to the local feed/dependency proof without falling back to project references or online package resolution.
- Published docs: explain adopter actions and compatibility; avoid internal story chronology except where it clarifies deprecation ownership.
- `CHANGELOG.md`: preserve semantic-release ownership; generated release notes must come from correct commit classification.

### Architecture Compliance

- Use `Hexalith.FrontComposer.slnx`; never create or use a legacy `.sln`.
- Keep centralized dependency versions; do not add `Version=` to project references or package references.
- Do not add new product C# types for this documentation/inventory story unless a focused validation helper is genuinely required; one C# type per file still applies.
- Keep Contracts dual-targeted and UI-clean, Contracts.UI net10-only, and SourceTools netstandard2.0/kernel-only.
- Do not modify root-declared submodules. The stale Tenants consumer is external fallout and requires separately authorized submodule work.
- Keep generated planning artifacts under `_bmad-output/`; keep published product documentation under `docs/`; never edit `docs/_site/**`, `bin/**`, `obj/**`, or generated analyzer output.

### Library and Framework Requirements

- Use the repository's actual centralized pins at implementation time. At story creation, `global.json` pins .NET SDK `10.0.301`; centralized props pin Roslyn `5.6.0`, Fluent UI Blazor v5 `5.0.0-rc.4-26180.1`, xUnit v3 `3.2.2`, and bUnit `2.8.4-preview`.
- This story does not authorize dependency upgrades. Version drift discovered while updating project context must be reported or corrected to the current centralized value, not solved by bumping packages.
- DocFX is warnings-as-errors and already includes the Contracts.UI assembly in `docs/docfx.json`; regenerate metadata/site output through validation rather than editing generated API YAML.

### Testing Requirements

Minimum focused commands (confirm exact CLI syntax against the current repo before running):

```bash
python3 eng/release_evidence.py inventory --root . --expected eng/release-package-inventory.json --output /tmp/frontcomposer-package-inventory.json
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~CiGovernanceTests"
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj -c Release --no-restore
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.UI.Tests/Hexalith.FrontComposer.Contracts.UI.Tests.csproj -c Release --no-restore
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Release --no-restore
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj -c Release --no-restore
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --no-restore
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release --no-restore
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"
pwsh ./eng/validate-docs.ps1
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 /nr:false
python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/11-14-update-architecture-context-ux-and-package-compat-docs.md
git diff --check
```

Also run the repository's semantic-release dry-run and full inventory pack flow using a non-published test version after confirming the current scripts/options. Record exact outputs; never invoke a live publish.

If restore, vulnerability feeds, DocFX, package consumers, VSTest/MTP, or external feeds block validation, record the exact command, exact blocker, whether tests executed, focused fallback evidence, and CI authority. A focused pass does not convert a blocked required lane into a pass.

### Previous Story Intelligence

Story 11.13 established the compatibility contract that Story 11.14 must explain:

- `ProjectionQuery` owns projection type, paging, column/status filters, search, and ordering.
- `QueryRequest` owns tenant, EventStore routing, validators, ETags, and cache metadata.
- Legacy construction, properties, 19-value deconstruction, record copy/equality behavior, and flat JSON remain synchronized through HFC0001/CS0618 migration shims.
- `IQueryService.QueryAsync`, Testing callbacks, EventStore body/headers/cache keys, MCP output, Pacts, and generated output remain unchanged.
- Broad release/package migration documentation is explicitly assigned to Story 11.14.

Story 11.12 review also teaches two important constraints: do not introduce upward type forwarding or duplicate old/new runtime identities, and keep the File List/package-consumer evidence mechanically reconciled.

### Git Intelligence

- `4d24036d` implemented the Contracts.UI split, exact public-API baseline, clean package consumers, explicit UI references, DocFX metadata, solution membership, and release test lane. Extend its package evidence; do not repeat its moves.
- `5bec9d48` implemented composed query compatibility and HFC0001 documentation/tests. Preserve its exact flat JSON and diagnostic contract.
- `f1d8d73e` records the current Story 11.5 broad-gate failure caused by the missing Contracts.UI inventory row.
- Recent commit subjects include casing/spacing that does not satisfy this repository's Conventional Commit rules. Do not copy them. A breaking release must use a valid lowercase Conventional Commit subject and explicit breaking signal.

### Latest Technical Information

- Microsoft package validation can compare a package against a previously released stable baseline and detects breaking API/TFM changes. Intentional breaks should be captured with narrowly reviewed suppressions and the baseline advanced after the new major release. [Source: https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/baseline-version-validator]
- NuGet package compatibility treats assemblies, target frameworks, and dependencies as public contract. Binary-breaking API changes may be acceptable across major versions but must be deliberate and validated. [Source: https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/nuget-package-compatibility-rules]
- Microsoft library guidance distinguishes source, behavior, and binary breaks; moving/removing public assembly APIs without compatibility support is binary-breaking, and `ObsoleteAttribute` is the normal source migration signal where a shim can remain. [Source: https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/breaking-changes]
- NuGet NU5104 warns when a stable package depends on a prerelease package; the official remedies are a prerelease package version or stable dependencies. Existing scoped suppression is an explicit project risk decision, not a compatibility guarantee. [Source: https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu5104]
- DocFX metadata/build supports warnings-as-errors; the repository already enables it, so missing/broken API ownership or links must fail validation. [Source: https://dotnet.github.io/docfx/reference/docfx-cli-reference/docfx-metadata.html]

### Documented Unrelated Changes

- `_bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md` - pre-existing user-owned Story 11.5 rework evidence; preserve and exclude from Story 11.14's File List.

### Project Structure Notes

- Story file: `_bmad-output/implementation-artifacts/11-14-update-architecture-context-ux-and-package-compat-docs.md`.
- Sprint key: `11-14-update-architecture-context-ux-and-package-compat-docs`.
- Generated planning/knowledge: `_bmad-output/**` only.
- Published adopter docs: `docs/**`, excluding generated `docs/_site/**` and hand edits to generated API YAML.
- Release inventory and governance: `eng/release-package-inventory.json`, `eng/*.py`, `.github/workflows/release.yml`, `.releaserc.json`, and `tests/**/Governance`/package-boundary tests.
- Production product behavior is out of scope. Any required product-code change beyond a focused release/test helper is an intent gap and must be escalated rather than silently added.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Epic 11 implementation order, Stories 11.8 and 11.11-11.14]
- [Source: `_bmad-output/planning-artifacts/prd.md` — FR-23, FR-25, FR-28, FR-29, NFR-12, D-5]
- [Source: `_bmad-output/planning-artifacts/prd-addendum-2026-07-05.md` — planning/project-doc source inventory]
- [Source: `_bmad-output/planning-artifacts/architecture.md` — layer model and current/future split language]
- [Source: `_bmad-output/planning-artifacts/ux-design.md` — UX-DR1]
- [Source: `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md` — typography and density semantics]
- [Source: `_bmad-output/implementation-artifacts/epic-11-context.md` — package-boundary technical decisions and dependencies]
- [Source: `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md` — approved moves, packages, compatibility posture, and done criteria]
- [Source: `_bmad-output/implementation-artifacts/spec-11-11-create-contracts-ui-assembly-and-migrate-blazor-rendering-surface.md` — implemented split and inventory blocker]
- [Source: `_bmad-output/implementation-artifacts/spec-11-12-relocate-runtime-and-testing-owned-types-out-of-contracts.md` — moved identities and review lessons]
- [Source: `_bmad-output/implementation-artifacts/spec-11-13-decompose-queryrequest-through-hfc0001-migration-path.md` — composed-query compatibility contract]
- [Source: `_bmad-output/implementation-artifacts/doc-drift-sweep-checklist.md` — required documentation reconciliation]
- [Source: `_bmad-output/implementation-artifacts/story-review-reconciliation-checklist.md` — File List and test evidence rules]
- [Source: `_bmad-output/project-context.md` — current project rules and stale facts owned by this story]
- [Source: `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` — repository-wide implementation rules]
- [Source: `references/Hexalith.AI.Tools/hexalith-ux-instructions.md` — Fluent UI/UX invariants]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-12: Review-patch verification passed after the Administrator-approved lifecycle resolution. The compatibility ledger now uses schema `2.0`, reconciles all 113 exact CP0001/CP0002/CP0008 XML identities, and permits same-FQN namespace preservation only for Contracts-to-Contracts.UI moves. The shared semantic-release execution plan rejects the pre-target `1.12` line, rejects expiry at `2.1`, rejects a mismatched current release line, and forwards `EnableFrontComposerPackageValidation=true` to all 16 planned build/pack commands. Focused verification passed: 4 Python execution-plan tests, 17 `QueryRequestTests`, 98 SourceTools diagnostic/deprecation tests (including the isolated 4/4 deprecation lane after excluding the in-process runner's Contracts copy), 43 CI governance tests, zero-warning Release builds, DocFX/docs validation, the actual eight-package/eight-symbol pack at `2.0.0-review.followup`, and `git diff --check`.
- 2026-07-11: Dev-story implementation confirmed the live Contracts/Contracts.UI/SourceTools/Shell/Testing ownership and composed `ProjectionQuery`/`QueryRequest` compatibility against source, project references, and public-API baselines. Story 11.11 remains blocked by its Story-11.14-owned inventory omission; 11.12 is done; 11.13 remains in review.
- 2026-07-11: Added the missing Contracts.UI release-inventory row, explicit governance coverage, Testing clean-consumer coverage, and a narrow first-release package-validation no-baseline path while advancing existing-package validation to `1.12.0`. Resealed affected synthetic release fixtures through `eng/release_evidence.py`.
- 2026-07-11: Red-phase inventory/baseline guards failed for the intended omissions, then passed after implementation. The explicit inventory validated; the full non-published `2.0.0-story.11.14` pack produced eight `.nupkg` and eight required `.snupkg` files including Contracts.UI; the clean Testing consumer restored Contracts, Contracts.UI, Shell, and Testing from packed packages without project assets.
- 2026-07-11: HALT gate reached before versioned migration/release-note documentation: no dated `approved` Release Owner decision for the post-`v1.12.0` breaking assembly moves exists under `_bmad-output/contracts/`. The dev agent did not self-approve the expected `2.0.0` posture.
- 2026-07-11: Administrator, acting as Release Owner, approved `2.0.0`; the decision is recorded in `_bmad-output/contracts/fc-2-0-release-version-decision-2026-07-11.md`. The versioned migration and package guidance were then published without editing semantic-release-owned `CHANGELOG.md`.
- 2026-07-11: Package validation compared supported existing libraries with `1.12.0`. Contracts and Shell intentional assembly-binding breaks are limited to 113 exact ApiCompat rows mirrored one-for-one in the governed suppression ledger; Contracts.UI uses the named first-release no-baseline path. The .NET SDK's documented `PackAsTool` exception leaves CLI compatibility covered by inventory, symbol, install, and output gates.
- 2026-07-11: Final validation passed: 3,990 tests across seven serialized projects, Release solution build with 0 warnings/errors, docs validation, inventory verification, clean package consumer, eight-package/eight-symbol pack at `2.0.0-story.11.14.final`, and `git diff --check`.
- 2026-07-11: `semantic-release 25.0.5 --dry-run --no-ci` with the repository analyzer/notes plugins classified the breaking commit range as `major`, selected `2.0.0`, and generated a BREAKING CHANGES section; no tag, changelog, package publish, or GitHub release was created.
- 2026-07-11: Create-story analysis loaded the BMAD workflow/customization/config, repository instructions, persistent project contexts, complete story source section, PRD/addendum, architecture, UX artifacts, Epic 11 context, Story 11.8 compatibility contract, Story 11.11-11.13 specs, release/package projects, inventory and governance tests, published migration/DocFX structure, recent Git history, and official .NET/NuGet/DocFX guidance.
- 2026-07-11: Confirmed no canonical Story 11.14 file existed; sprint status key was `backlog`; Epic 11 was already `in-progress`.
- 2026-07-11: Confirmed the live Contracts.UI project is packable and implemented, but the release inventory omits it. This is the exact blocking condition recorded by Story 11.11 and reproduced by inventory/governance analysis.
- 2026-07-11: Confirmed latest stable tag/changelog is `v1.12.0`, invalidating the planning assumption that the no-forwarder assembly moves are pre-v1.0.
- 2026-07-11: Parallel artifact, architecture/code, history/package, and official-guidance analyses were consolidated into this implementation guide; no subagent edited files.

### Documented Unrelated Changes

- `references/Hexalith.EventStore` — pre-existing root-submodule pointer drift committed after the story baseline and before dev-story began; preserved without submodule edits.
- `references/Hexalith.Memories` — pre-existing root-submodule pointer drift committed after the story baseline and before dev-story began; preserved without submodule edits.

### Completion Notes List

- Reconciled the live Story 11.11-11.13 package/query state across planning, generated knowledge, published documentation, and UX-DR1 ownership trace. Interaction, typography mapping `3.1.0`, accessibility, density, wire, schema, CLI, and generated-route behavior remain unchanged.
- Added Contracts.UI to the explicit eight-package release inventory, resealed affected fixtures with the repository helper, and extended clean-consumer evidence across Contracts, Contracts.UI, Shell, and Testing.
- Published the approved `1.12` to `2.0` migration, exact ownership map, composed-query/HFC0001 guidance, prerelease dependency risk, package responsibility guide, and API navigation.
- Advanced supported library package validation to the real `1.12.0` baseline, retained a narrow Contracts.UI first-release exception, and checked in exact Contracts/Shell compatibility suppressions with one-to-one governance evidence.
- Preserved `CHANGELOG.md` ownership and proved the actual Conventional Commit range produces a semantic-release major `2.0.0` with breaking release notes.
- All required package, consumer, test, build, docs, story, diff, and release-classification gates passed.

### File List

- `Directory.Build.targets`
- `_bmad-output/contracts/fc-2-0-release-version-decision-2026-07-11.md`
- `_bmad-output/implementation-artifacts/11-14-update-architecture-context-ux-and-package-compat-docs.md`
- `_bmad-output/implementation-artifacts/epic-11-context.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md`
- `_bmad-output/planning-artifacts/ux-design.md`
- `_bmad-output/project-context.md`
- `_bmad-output/project-docs/architecture.md`
- `_bmad-output/project-docs/component-inventory.md`
- `_bmad-output/project-docs/contribution-guide.md`
- `_bmad-output/project-docs/data-models.md`
- `_bmad-output/project-docs/deployment-guide.md`
- `_bmad-output/project-docs/development-guide.md`
- `_bmad-output/project-docs/index.md`
- `_bmad-output/project-docs/project-overview.md`
- `_bmad-output/project-docs/source-tree-analysis.md`
- `docs/concepts/source-generation-and-mcp-split.md`
- `docs/diagnostics/README.md`
- `docs/diagnostics/compatibility-suppressions.json`
- `docs/diagnostics/diagnostic-registry.json`
- `docs/diagnostics/HFC0001.md`
- `docs/how-to/migration-guides.md`
- `docs/migrations/1.12-to-2.0.md`
- `docs/migrations/index.md`
- `docs/reference/api/index.md`
- `docs/skills/frontcomposer/setup/package-and-hosting.md`
- `docs/toc.yml`
- `docs/validation/producer-fingerprints.json`
- `eng/pack_release_packages.py`
- `eng/release-package-inventory.json`
- `src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj`
- `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`
- `src/Hexalith.FrontComposer.Contracts/CompatibilitySuppressions.xml`
- `src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs`
- `src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj`
- `src/Hexalith.FrontComposer.Shell/CompatibilitySuppressions.xml`
- `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`
- `src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj`
- `tests/eng/test_pack_release_packages.py`
- `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/QueryRequestTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/QueryRequestDeprecationTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`
- `tests/ci-governance/fixtures/release-manifest-valid.json`
- `tests/ci-governance/fixtures/release-readiness-cases.json`
- `_bmad-output/implementation-artifacts/deferred-work.md` — named exception: review-workflow deferred-item triage is pre-existing/root-owned and intentionally excluded from this patch implementation.
- `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs:79` — named exception: review-location evidence for the deferred pre-existing Contracts.UI finding resolves through the listed test file.
- `_bmad-output/project-docs/development-guide.md:56` — named exception: review-location evidence for the deferred pre-existing generated-guidance finding resolves through the listed document.
- `Hexalith.FrontComposer.Contracts.UI` — named exception: package-ID evidence resolves through the inventory, project, tests, and migration entries above.
- `CiGovernanceTests.PackageInventory_IsExplicitLockstepAndReviewable` — named exception: test-method evidence is in the listed `CiGovernanceTests.cs` file.
- `tests/ci-governance/fixtures/` — named exception: directory evidence is the two listed, resealed fixture files.
- `_bmad-output/project-docs/api-contracts.md` — named exception: reviewed and intentionally unchanged because its current API claims remain correct.
- `_bmad-output/planning-artifacts/ux-experience-2026-07-05.md` — named exception: reviewed and intentionally unchanged because interaction behavior did not change.
- `docs/migrations/` — named exception: directory evidence is the listed `1.12-to-2.0.md` page and migration indexes.
- `QueryRequest.Create` — named exception: API evidence is documented and tested through the listed migration/context artifacts.
- `v1.12.0` — named exception: latest-stable evidence from Git/tag history is recorded in the completion log and version decision.
- `1.12` — named exception: migration-edge evidence is the listed `1.12-to-2.0.md` page.
- `2.0` — named exception: migration-edge evidence is the listed `1.12-to-2.0.md` page.
- `2.0.0` — named exception: approved-version evidence is the listed release decision and semantic-release dry run.
- `_bmad-output/contracts/` — named exception: directory evidence is the listed approved release-version decision.
- `CHANGELOG.md` — named exception: reviewed and intentionally unchanged because semantic-release owns it.
- `0.1.0` — named exception: stale-baseline removal is evidenced by the listed targets and governance test.
- `1.12.0` — named exception: current-baseline evidence is in the listed targets, suppression ledger, tests, and migration.
- `.slnx` — named exception: the unchanged `Hexalith.FrontComposer.slnx` was used for the successful Release build and not solution-level test execution.

### Change Log

- 2026-07-12: Applied approved review patches, retained `QueryRequest` shims throughout 2.x with removal targeted for 3.0, and hardened compatibility-suppression release enforcement.
- 2026-07-11: Implemented Story 11.14, recorded Release Owner approval for 2.0.0, completed compatibility/package/documentation evidence, and moved the story to review.

## Suggested Review Order

**Release enforcement**

- Start with the shared plan that validates release lines before executing package commands.
  [`pack_release_packages.py:29`](../../eng/pack_release_packages.py#L29)

- Review the versioned ledger schema and exact CP identity evidence.
  [`compatibility-suppressions.json:1`](../../docs/diagnostics/compatibility-suppressions.json#L1)

- Confirm every planned command forwards package validation and rejects stale suppressions.
  [`test_pack_release_packages.py:19`](../../tests/eng/test_pack_release_packages.py#L19)

**Query migration lifecycle**

- Verify the retained compatibility surface now advertises the approved 3.0 removal.
  [`QueryRequest.cs:12`](../../src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs#L12)

- Confirm the Release Owner amendment preserves shims throughout the 2.x line.
  [`fc-2-0-release-version-decision-2026-07-11.md:22`](../contracts/fc-2-0-release-version-decision-2026-07-11.md#L22)

- Read the corrected adopter examples and complete ownership map.
  [`1.12-to-2.0.md:53`](../../docs/migrations/1.12-to-2.0.md#L53)

**Verification boundaries**

- Inspect exact ledger-to-XML reconciliation and destination parsing.
  [`DiagnosticRegistryTests.cs:643`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs#L643)

- Check deterministic TFM selection for isolated deprecation compilation.
  [`QueryRequestDeprecationTests.cs:94`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/QueryRequestDeprecationTests.cs#L94)

- Confirm the clean consumer restores exclusively from local packages and cache.
  [`PackageBoundaryTests.cs:63`](../../tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs#L63)
