---
created: 2026-07-12
epic: 11
story: 11
story_key: 11-11-create-contracts-ui-assembly-and-migrate-blazor-rendering-surface
source_epics: _bmad-output/planning-artifacts/epics.md
baseline_commit: 359bbf834798bea62824d1a4a00509a7d9f8228e
implementation_commit: 4d24036d6c59fd53a76761ffdef8c797509f4008
governance_commit: b6e985f40ec697dd1927dabf14152c318cd931d9
status: done
---

# Story 11.11: Create Contracts.UI Assembly and Migrate Blazor Rendering Surface

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an adopter developer (Hexalith.Tenants first),
I want the net10/Blazor rendering surface split out of the netstandard Contracts kernel,
so that referencing Contracts stops inheriting the pinned Fluent RC.

## Acceptance Criteria

1. **Given** the approved Story 11.8 decision, **when** the net10/Blazor surface is moved, **then** `Typography`/`FcTypoToken`, `RenderFragment` contexts, and `KeyboardEventArgs` members live in a net10-only Contracts.UI assembly or approved equivalent. *(H11)*

2. **Given** a consumer references only the Contracts kernel, **when** package and project-reference validation runs, **then** it no longer inherits the pinned Fluent RC through Contracts.

3. **Given** public surfaces move, **when** package boundary tests run, **then** public API baselines and docs are updated intentionally.

4. **Given** SourceTools and repository-owned generated/UI consumers, **when** analyzer, package, and compile validation runs, **then** SourceTools references and embeds only the Contracts kernel, generated UI compiles through explicit Contracts.UI references, moved public FQNs remain source-compatible, and routes, generated text, schema/wire contracts, and snapshots have no unrelated drift.

## Tasks / Subtasks

- [x] Reconcile the brownfield implementation before editing. (AC: 1, 2, 3, 4)
  - [x] Read this story, Epic 11 Story 11.11, the Story 11.8 compatibility plan, the dedicated Story 11.11 spec, the 1.12-to-2.0 migration guide, Story 11.14, and every file under "Current Files To Read Before Editing" completely.
  - [x] Confirm commits `4d24036d` and `b6e985f4` are ancestors of the working baseline and inspect current code rather than replaying their file operations.
  - [x] Treat the old spec's `blocked` inventory result, the old dev-auto branch-mismatch result, and `epic-11-context.md`'s Story-11.14 blocker sentence as historical. Story 11.14 has resolved the release-inventory blocker.
  - [x] Do not recreate Contracts.UI, move already-moved types, add duplicate same-FQN types, or mark historical checks as freshly passing without rerunning them.
  - [x] Re-run `git status --short --branch` before edits. Preserve the current unrelated user changes to agent instructions, Story 11.5/deferred-work/sprint artifacts, e2e evidence, root submodule pointers, and the untracked continuation prompt.

- [x] Confirm and pin the Contracts/Contracts.UI ownership boundary. (AC: 1, 2, 3)
  - [x] Prove `Hexalith.FrontComposer.Contracts` remains packable and targets `net10.0;netstandard2.0`, with neither face referencing Contracts.UI, ASP.NET Components, Fluent UI, Shell, runtime implementations, or test fakes.
  - [x] Prove `Hexalith.FrontComposer.Contracts.UI` remains a packable, trimmable, net10-only SDK library that references Contracts downward, `Microsoft.AspNetCore.App`, and the centrally pinned Fluent UI package.
  - [x] Confirm every approved rendering/shortcut type exists exactly once in Contracts.UI, retains its `Hexalith.FrontComposer.Contracts.Rendering` or `.Shortcuts` namespace, and is absent from the Contracts assembly.
  - [x] Keep kernel descriptors, registries, contract-version constants, `RenderContext`, `FieldDescriptor`, `ContractsMetadata.TypographyMappingVersion`, and `ShortcutRegistration` in Contracts.
  - [x] Preserve the net10 trim annotations on kernel descriptors. Do not remove `#if NET10_0_OR_GREATER` guards merely because the Blazor/Fluent UI guards were removed.
  - [x] Preserve one top-level C# type/delegate per file, file-scoped namespaces, XML documentation on public API, CRLF, nullable annotations, and warning-free builds.

- [x] Preserve moved behavior and explicit consumer references. (AC: 1, 4)
  - [x] Keep the nine typography role mappings and `TypographyMappingVersion = "3.1.0"` unchanged; this story changes ownership, not visual semantics.
  - [x] Preserve `FieldSlotContext`, `ProjectionTemplateContext`, and `ProjectionViewContext` validation, render-delegate, fallback, density, read-only, and development-mode behavior.
  - [x] Preserve `ShortcutBinding` normalization and dispatch behavior, including canonical modifier ordering, strict two-key chords, rejection of empty/duplicate/unknown modifiers, and `KeyboardEventArgs` mapping.
  - [x] Confirm Shell directly references Contracts and Contracts.UI, and authored/generated UI consumers in Counter, IdeParityCounter, Shell tests, and SourceTools tests reference Contracts.UI explicitly rather than relying on accidental transitivity.
  - [x] Do not add Contracts.UI, ASP.NET Components, or Fluent references to SourceTools, MCP, Schema, or CLI. SourceTools must remain a netstandard2.0 analyzer referencing and embedding only Contracts.
  - [x] Preserve unchanged generated FQNs. Generated-consumer compilation may receive Contracts.UI metadata; the generator project and analyzer payload may not.

- [x] Strengthen package and public-API evidence rather than trusting substring checks. (AC: 2, 3, 4)
  - [x] Keep `src/Hexalith.FrontComposer.Contracts.UI/PublicAPI.Shipped.txt` as the intentional exact API baseline and update it only for approved public changes.
  - [x] Close the deferred package-proof gap: parse the packed nuspec dependency groups and assert exact dependency IDs/version ranges for Contracts and Fluent UI instead of searching the raw XML for substrings.
  - [x] Make the clean Contracts.UI consumer rely on the Contracts.UI package to supply Fluent transitively; remove its direct Fluent `PackageReference` so malformed package metadata cannot false-pass.
  - [x] Keep separate clean kernel-only consumers for netstandard2.0 and net10 and assert their `project.assets.json`, nuspec, and assembly references contain no Contracts.UI, ASP.NET Components, or Fluent dependency.
  - [x] Prove the package includes the intentional public-API baseline and the release inventory still lists Contracts.UI as packable with symbols required.

- [x] Resolve the post-first-release package-validation trigger honestly. (AC: 3)
  - [x] Verify whether `Hexalith.FrontComposer.Contracts.UI` version `2.0.0` is available from the configured release package source; a Git tag alone is not sufficient publication evidence.
  - [x] If the package is published, remove `FrontComposerPackageValidationSkipBaseline`, set the Contracts.UI baseline to `2.0.0` through the existing package-validation policy, and update the focused governance tests that currently require the first-release exception.
  - [x] If the package is not published, retain the narrow exception, record the exact release blocker and owner/date, and do not claim post-release baseline validation passed.
  - [x] Do not disable package validation globally, generate broad suppressions, change the `1.12.0` baseline for existing packages, publish a package, or edit `CHANGELOG.md` manually.

- [x] Reconfirm SourceTools, generated consumers, documentation, and release integration. (AC: 3, 4)
  - [x] Prove SourceTools' package/analyzer payload contains only SourceTools and Contracts, while a packaged generated consumer compiles with an explicit Contracts.UI reference and unchanged generated source.
  - [x] Confirm solution membership, DocFX API metadata, release test-project enumeration, and `eng/release-package-inventory.json` still include Contracts.UI and its tests in their approved locations.
  - [x] Confirm `docs/migrations/1.12-to-2.0.md` accurately states that namespaces remain unchanged, adopters must add Contracts.UI and rebuild, and assembly identity moved without type forwarding as an intentional v2 binary break.
  - [x] Preserve Story 11.12 runtime/testing ownership, Story 11.13 `ProjectionQuery`/HFC0001 compatibility, and Story 11.14 architecture/UX/release documentation. Update only demonstrably stale current guidance owned by this story.
  - [x] Do not change schema fingerprints, canonical JSON, MCP descriptors/output, CLI JSON, EventStore request/response behavior, generated routes/output paths, Pact wire shapes, or unrelated Verify snapshots.

- [x] Produce fresh, reviewable evidence and reconcile the implementation record. (AC: 1, 2, 3, 4)
  - [x] Restore/build the `.slnx` in Release with warnings as errors and explicitly build Contracts for netstandard2.0 before ownership/package tests that inspect its output.
  - [x] Run Contracts, Contracts.UI, SourceTools, and Shell test projects individually with `DiffEngine_Disabled=true`; run relevant Testing/sample/IDE-parity builds when dependency changes touch them.
  - [x] Run public API, exact nuspec/assets, clean-consumer, analyzer-payload/generated-consumer, release inventory, package pack, DocFX/docs, story artifact, and whitespace gates. Do not publish packages.
  - [x] Use the `.slnx` for restore/build only. Do not substitute a solution-level `dotnet test` for the repository-required per-project lanes.
- [x] Record exact current counts and blockers. Historical 11.14 counts are useful baseline evidence but are not a fresh pass.
- [x] Reconcile the final implementation diff, story File List, current-state documentation, and any accepted deferral before moving this story to review.

### Review Findings

- [x] [Review][Decision] Separate or explicitly accept the out-of-scope review range — Resolved 2026-07-12: user accepted the bundled history as intentional; no paths were reverted and the finding was dismissed.
- [x] [Review][Patch] Make the clean Contracts.UI consumer prove the package-supplied ASP.NET framework reference [tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs:80]
- [x] [Review][Patch] Validate dependency asset exclusions and runtime loading in the packed-package proof [tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs:86]
- [x] [Review][Defer] Move synthetic pulse insertion inside the cleanup scope [tests/e2e/specs/specimen-accessibility.spec.ts:342] — deferred, pre-existing

## Dev Notes

### Story Context

Epic 11 is architecture-review release-risk remediation, not new product scope. Story 11.8 approved separating the Blazor/Fluent rendering surface from the Contracts kernel so kernel and analyzer consumers no longer inherit the pinned Fluent prerelease. Story 11.11 owns the Contracts.UI assembly and moved UI-facing contracts; Story 11.12 owns runtime/testing relocations, Story 11.13 owns query decomposition, and Story 11.14 owns broad compatibility documentation and release inventory. [Source: `_bmad-output/planning-artifacts/epics.md` lines 1393-1441, 1628-1730; `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md` lines 17-94]

The canonical source story is intentionally concise. The dedicated spec adds the implementation boundary: Contracts stays UI-clean on `net10.0;netstandard2.0`; Contracts.UI is packable/net10-only; SourceTools stays netstandard2.0/Contracts-only; moved namespaces and behavior remain stable; schema, MCP, CLI, EventStore, generated route/output, and wire shapes do not drift. [Source: `_bmad-output/implementation-artifacts/spec-11-11-create-contracts-ui-assembly-and-migrate-blazor-rendering-surface.md` lines 19-65]

### Brownfield Reality and Status Reconciliation

At story creation on 2026-07-12, the implementation is already present and released in the repository:

- Commit `4d24036d` created Contracts.UI, moved the rendering/shortcut surface, added explicit consumers, relocated tests, and added solution/DocFX/release integration.
- Commit `b6e985f4` added the release inventory/governance and recorded the intentional 2.0 breaking posture.
- HEAD `359bbf83` is tagged `v2.0.0`; `main` equals `origin/main`.
- Story 11.14 is done and resolved the original missing-inventory blocker.
- Sprint tracking still labels Story 11.11 `backlog`, no canonical `11-11-*.md` story existed, the dedicated spec still says `blocked`, and the old dev-auto result records an obsolete Story-11.7 branch mismatch.

Therefore, implementation should start with confirmation and current-state reconciliation. Do not recreate the assembly or duplicate types. Patch only a verified gap, then produce fresh evidence. The known current gaps are the weak nuspec dependency proof and the first-release package-validation exception whose removal trigger may now have fired.

### Approved Dependency Direction

```text
Contracts.UI (net10.0)
  -> Contracts (net10.0;netstandard2.0, UI-clean)
  -> Microsoft.AspNetCore.App
  -> Microsoft.FluentUI.AspNetCore.Components

Shell
  -> Contracts
  -> Contracts.UI

Testing
  -> Contracts
  -> Shell -> Contracts.UI

SourceTools (netstandard2.0 analyzer)
  -> Contracts only

Schema -> Contracts only
MCP -> Contracts + Schema
CLI -> no FrontComposer project references
```

Never add an upward Contracts-to-Contracts.UI reference or a `TypeForwardedTo` shim. Type forwarding would require Contracts to reference the UI assembly and recreate the transitive UI/Fluent leak. Source compatibility is preserved by unchanged namespaces plus an explicit Contracts.UI reference and rebuild; binary compatibility is intentionally broken and carried by v2.0. [Source: `docs/migrations/1.12-to-2.0.md`; `_bmad-output/contracts/fc-2-0-release-version-decision-2026-07-11.md`]

### Approved Moved Surface

The following types belong in `src/Hexalith.FrontComposer.Contracts.UI/` while retaining their existing public namespaces:

- Rendering: `Typography`, `FcTypoToken`, `TypographyStyle`, `FieldSlotContext<TProjection,TField>`, `ProjectionTemplateContext<TProjection>`, `ProjectionTemplateSectionRenderer`, `ProjectionTemplateRowRenderer<TProjection>`, `ProjectionTemplateFieldRenderer<TProjection>`, `ProjectionTemplateColumnDescriptor`, `ProjectionTemplateSectionDescriptor`, and `ProjectionViewContext<TProjection>`.
- Shortcuts: `IShortcutService` and `ShortcutBinding`.

Retain UI-neutral seams in Contracts: `FieldDescriptor`, `RenderContext`, projection slot/template/view descriptors and contract versions, `IProjectionSlotRegistry`, `IProjectionTemplateRegistry`, `IProjectionViewOverrideRegistry`, `ShortcutRegistration`, and typography mapping-version metadata.

### Current State of Existing Files

- `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj` currently targets net10, is packable/trimmable, references Contracts + ASP.NET shared framework + Fluent, packages its public API baseline, and carries the first-release validation exception.
- `src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj` currently targets `net10.0;netstandard2.0` and has no UI references. Preserve the conditional netstandard support packages and conditional trimmability.
- `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj` currently targets netstandard2.0, references Contracts only, and embeds SourceTools + Contracts in the analyzer package. Do not add Contracts.UI.
- `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj` currently references both Contracts and Contracts.UI and owns the Fluent runtime dependency.
- `IProjectionSlotRegistry.cs` and `IProjectionViewOverrideRegistry.cs` are kernel registry seams whose cross-assembly documentation was adjusted without changing dependency direction.
- Sample/test project files carry explicit Contracts.UI references where authored or generated UI consumes moved types.
- `CompilationHelper.cs` adds Contracts.UI only to generated-consumer compilation metadata. `PackagedAnalyzerConsumerTests.cs` proves Contracts.UI/Shell are absent from the analyzer payload while generated UI compiles in the consumer.
- Contracts tests retain kernel descriptor/version assertions and prove moved types are absent. Contracts.UI tests own moved behavior and API/package assertions.
- `Hexalith.FrontComposer.slnx`, `docs/docfx.json`, `.github/workflows/release.yml`, and `eng/release-package-inventory.json` register the project, API assembly, test lane, package, and symbols.

### Current Files To Read Before Editing

Read every relevant UPDATE file completely before changing it:

- `Hexalith.FrontComposer.slnx`
- `Directory.Build.targets`
- `eng/release-package-inventory.json`
- `docs/docfx.json`
- `.github/workflows/release.yml`
- `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`
- `src/Hexalith.FrontComposer.Contracts.UI/PublicAPI.Shipped.txt`
- all files under `src/Hexalith.FrontComposer.Contracts.UI/Rendering/` and `Shortcuts/`
- `src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj`
- `src/Hexalith.FrontComposer.Contracts/ContractsMetadata.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/FieldDescriptor.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/RenderContext.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionSlotRegistry.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionTemplateRegistry.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/IProjectionViewOverrideRegistry.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotDescriptor.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionTemplateDescriptor.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionViewOverrideDescriptor.cs`
- `src/Hexalith.FrontComposer.Contracts/Shortcuts/ShortcutRegistration.cs`
- `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj`
- `src/Hexalith.FrontComposer.Schema/Hexalith.FrontComposer.Schema.csproj`
- `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj`
- `src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj`
- `src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj`
- all five explicit sample consumer project files under `samples/Counter/` and `samples/IdeParityCounter/`
- `tests/Hexalith.FrontComposer.Contracts.UI.Tests/Hexalith.FrontComposer.Contracts.UI.Tests.csproj`
- `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`
- all four files under `tests/Hexalith.FrontComposer.Contracts.UI.Tests/Rendering/`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Architecture/ContractsKernelOwnershipTests.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Architecture/ContractsPackageBoundaryTests.cs`
- the four corresponding rendering test files under `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/CompilationHelper.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj`
- `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutBindingNormalizeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `docs/migrations/1.12-to-2.0.md`
- `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md`
- `_bmad-output/contracts/fc-2-0-release-version-decision-2026-07-11.md`
- `_bmad-output/implementation-artifacts/11-14-update-architecture-context-ux-and-package-compat-docs.md`

### Architecture Compliance and Anti-Patterns

- Use the `.slnx` only. Keep centralized package versions; do not add `Version=` to project files.
- Keep dependency direction pointing down to Contracts. Do not introduce cycles or accidental transitivity.
- Do not change the exact Fluent pin as part of this boundary reconciliation. `FcTypoToken` exposes Fluent types publicly, so a version change is API-sensitive and needs its own evidence.
- Do not hand-edit generated output, `obj/**`, API YAML, release notes, or sibling submodules.
- Do not move Story 11.12 runtime/testing types back into Contracts or change Story 11.13's HFC0001/flat-JSON compatibility surface.
- Do not use the old `pre-v1.0` wording as current release truth. The approved migration is v1.12 to v2.0.
- Do not add a raw Blazor/Fluent dependency to kernel-only consumers to make a test pass.
- Do not weaken public API, exact dependency, inventory, or package validation gates to accept current output.

### Testing Requirements

Prepare Release outputs first:

```bash
dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 /nr:false
dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -f netstandard2.0 -c Release --no-restore -m:1 /nr:false
```

Run affected test projects individually:

```bash
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj -c Release --no-restore -m:1 /nr:false
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.UI.Tests/Hexalith.FrontComposer.Contracts.UI.Tests.csproj -c Release --no-restore -m:1 /nr:false
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --no-restore -m:1 /nr:false
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore -m:1 /nr:false
```

If Microsoft.Testing.Platform/VSTest sockets block execution, build the project and run its xUnit v3 executable directly with single-dash filters as needed; record the exact blocked command and fallback evidence.

Run inventory, docs, artifact, and whitespace gates:

```bash
python3 eng/release_evidence.py inventory --root . --expected eng/release-package-inventory.json --output /tmp/frontcomposer-package-inventory.json
pwsh ./eng/validate-docs.ps1
python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/11-11-create-contracts-ui-assembly-and-migrate-blazor-rendering-surface.md
git diff --check
```

Pack the explicit eight-package inventory at a non-published test version and compile clean kernel-only/UI/generated consumers. Never publish from a story workflow.

### Latest Technical Information

- Microsoft documents `net10.0` and `netstandard2.0` as supported SDK-style TFMs and uses `TargetFrameworks` for multi-targeted libraries. Preserve both kernel targets rather than collapsing Contracts to one TFM. Source: https://learn.microsoft.com/en-us/dotnet/standard/frameworks
- Microsoft documents `<FrameworkReference Include="Microsoft.AspNetCore.App" />` as the correct way for an SDK class library to consume ASP.NET Core shared-framework APIs. Contracts.UI already follows this pattern. Source: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/target-aspnetcore?view=aspnetcore-10.0
- NuGet's SDK pack targets generate TFM-specific library folders and dependency groups; validate exact dependency groups rather than substring presence. Source: https://learn.microsoft.com/en-us/nuget/create-packages/supporting-multiple-target-frameworks
- PackageReference restore produces the full transitive dependency closure. A clean Contracts.UI consumer that directly references Fluent cannot prove that Contracts.UI declared Fluent correctly. Source: https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files
- .NET package validation checks breaking changes, runtime/framework consistency, and applicability holes; baseline validation is the supported post-release mechanism. Source: https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview
- Microsoft's baseline-validator guidance says that after a new major release, advance the validation baseline to that release and review/remove temporary compatibility suppressions. Apply this only after confirming the Contracts.UI 2.0.0 package is available. Source: https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/baseline-version-validator
- The current NuGet gallery lists `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.4-26180.1` as the latest v5 prerelease, matching the repository pin at story creation. This story does not authorize a version bump. Source: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components

### Previous Story Intelligence

The formal previous canonical Epic 11 story file is Story 11.6, not the numerically closer specs. Story 11.6's transferable lessons are:

- Reconfirm brownfield state and read UPDATE files before editing.
- Treat publishable public API and clean-consumer evidence as first-class acceptance evidence.
- Add direct surface tests, not only host-level or substring assertions.
- Preserve unrelated dirty work and record exact environmental blockers with focused fallbacks.
- Reconcile the story File List mechanically before review.

Story 11.14 is the more relevant later evidence source: it resolved the inventory blocker, recorded v2 compatibility, and deferred the weak Contracts.UI dependency proof. Reuse its facts, not its old test counts.

### Git Intelligence

- `4d24036d` implemented the Contracts.UI split across 48 files (+896/-807).
- `b6e985f4` added the breaking-change signal, release inventory, package validation/governance, migration docs, and 2.0 compatibility evidence.
- `3eafa3fa` pinned clean-consumer Fluent v5 dependencies; its direct Fluent reference is now the package-proof blind spot this story must close.
- `f32a9862` made Contracts netstandard2.0 output an explicit release-test prerequisite.
- `359bbf83` is the current `v2.0.0` release commit at story creation.

Fresh read-only checks run during story creation at HEAD: Contracts.UI tests passed 10/10; Contracts tests passed 200/200; release inventory validation returned `status: valid`; `git diff --check` exited successfully. These establish the creation baseline only. The dev agent must rerun affected evidence after any change.

### Documented Unrelated Changes

The following paths were already modified or untracked before Story 11.11 creation and are not Story 11.11 deliverables. Preserve them and exclude them from Story 11.11 completion claims unless their owners explicitly transfer them into scope:

- `AGENTS.md`
- `CLAUDE.md`
- `_bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/prompt-fc-nip-continuation-2026-07-12.md`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.Memories`
- `references/Hexalith.PolymorphicSerializations`
- `references/Hexalith.Tenants`
- `tests/e2e/specs/specimen-accessibility.spec.ts`

### Project Structure Notes

- Canonical story file: `_bmad-output/implementation-artifacts/11-11-create-contracts-ui-assembly-and-migrate-blazor-rendering-surface.md`.
- Primary implementation area: `src/Hexalith.FrontComposer.Contracts.UI/` plus the kernel boundary in `src/Hexalith.FrontComposer.Contracts/`.
- Primary test areas: `tests/Hexalith.FrontComposer.Contracts.UI.Tests/`, `tests/Hexalith.FrontComposer.Contracts.Tests/`, `tests/Hexalith.FrontComposer.SourceTools.Tests/`, and focused Shell shortcut/governance tests.
- Release/package integration: `Directory.Build.targets`, `eng/release-package-inventory.json`, `.github/workflows/release.yml`, `docs/docfx.json`, and clean-consumer tests.
- Do not edit sibling submodules or generated build output for this story.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 11 objective, implementation order, Story 11.8 gate, Story 11.11 foundation, and sibling ownership.
- `_bmad-output/planning-artifacts/prd.md` - dependency direction, release-readiness risk, public surface, and D-5 split decision.
- `_bmad-output/planning-artifacts/architecture.md` - current Layer 0/0A/SourceTools/consumer shape and invariants.
- `_bmad-output/planning-artifacts/ux-design.md` and `ux-design-detailed-2026-07-05.md` - Contracts.UI typography ownership and unchanged UX semantics.
- `_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md` - approved target shape and compatibility constraints.
- `_bmad-output/contracts/fc-2-0-release-version-decision-2026-07-11.md` - approved v1.12-to-v2 breaking posture and post-first-release validation trigger.
- `_bmad-output/implementation-artifacts/spec-11-11-create-contracts-ui-assembly-and-migrate-blazor-rendering-surface.md` - detailed intent, code map, original verification plan, and historical blocker.
- `_bmad-output/implementation-artifacts/11-14-update-architecture-context-ux-and-package-compat-docs.md` - resolved inventory/docs/release evidence and deferred package-proof finding.
- `_bmad-output/implementation-artifacts/11-6-testing-harness-failure-modes.md` - formal previous-story workflow and public-package evidence lessons.
- `_bmad-output/project-context.md` - current stack, coding, package, testing, and workflow rules.
- `references/Hexalith.AI.Tools/hexalith-llm-instructions.md` and `hexalith-ux-instructions.md` - repository-wide code, package, testing, and UI rules.
- Microsoft and NuGet documentation URLs listed under Latest Technical Information.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-12: Create-story analysis loaded repository instructions, resolved workflow/configuration, persistent project-context facts, complete sprint tracking, Epic 11/PRD/architecture/UX sources, Story 11.8/11.11/11.14 contracts and specs, formal previous Story 11.6, current implementation/tests/package files, Git history, current dirty state, and current official platform/package guidance.
- 2026-07-12: Confirmed no canonical Story 11.11 file existed; sprint tracking remained `backlog` despite the landed implementation and v2.0 release.
- 2026-07-12: Parallel artifact, architecture/code, and history/live-state analyses agreed this must be a confirm-and-pin/status-reconciliation story.
- 2026-07-12: Read-only baseline evidence passed: Contracts.UI 10/10, Contracts 200/200, release inventory valid, and whitespace diff check successful.
- 2026-07-12: Dev-story reconciliation confirmed `4d24036d` and `b6e985f4` are ancestors of `359bbf83`, preserved every documented unrelated working-tree change, and treated the old blocked spec/dev-auto/context claims as historical.
- 2026-07-12: RED evidence failed exactly on the expired Contracts.UI first-release exception in both the UI package boundary lane (3 passed, 1 failed) and SourceTools package-policy governance lane (1 failed).
- 2026-07-12: NuGet.org exact search confirmed `Hexalith.FrontComposer.Contracts.UI` `2.0.0` is published. Removed the no-baseline marker, set the project baseline to `2.0.0`, and passed a direct build/pack with package validation enabled against that release.
- 2026-07-12: Replaced raw nuspec substring checks with exact net10 dependency ID/version assertions and removed the clean UI consumer's direct Fluent reference. The UI package boundary lane passed 4/4 and full Contracts.UI passed 10/10.
- 2026-07-12: Full SourceTools initially exposed a prerelease-version fixture flake (`review.05934810`, invalid leading-zero numeric identifier). Added an alphabetic prefix across the three Story 11.11 package consumers; the packaged analyzer lane then passed and full SourceTools passed 1,069/1,069.
- 2026-07-12: Final evidence passed: Release solution and explicit netstandard2.0 Contracts builds with 0 warnings/errors; Contracts 200/200; Contracts.UI 10/10; SourceTools 1,069/1,069; Shell 2,218/2,218; inventory valid; DocFX/docs valid; `git diff --check`; direct Contracts.UI 2.0.0 baseline validation; and eight non-published `.nupkg` plus eight `.snupkg` artifacts.
- 2026-07-12: Aspire baseline was attempted before edits and stopped cleanly after startup failed on pre-existing external/runtime state: a locked Tenants.UI output and duplicate Shell assembly identities (`1.12.0.0` package plus local `1.0.0.0`). Focused Story 11.11 validation is unaffected.

### Implementation Plan

- Confirm the already-landed ownership split and explicit dependency graph before changing anything.
- Use red tests to close the exact nuspec/transitive-consumer proof gap and resolve the first-release validation trigger from current publication evidence.
- Rerun package/API/analyzer/generated-consumer/runtime/docs/release evidence and patch only verified regressions.
- Reconcile the story record and sprint state without absorbing unrelated work.

### Completion Notes List

- Confirmed the existing Contracts/Contracts.UI ownership split, unchanged moved FQNs/behavior, kernel trim metadata, explicit UI consumers, Contracts-only SourceTools payload, and release/DocFX integration without replaying the landed assembly move.
- Strengthened Contracts.UI package evidence to parse the exact `net10.0` dependency group and require only the matching Contracts version plus Fluent `5.0.0-rc.4-26180.1`; the clean consumer now proves Fluent transitivity through Contracts.UI.
- Advanced Contracts.UI package validation from the expired first-release exception to its published `2.0.0` baseline and updated focused governance plus current diagnostics/project guidance.
- Hardened three package-consumer fixtures against invalid all-numeric leading-zero prerelease identifiers.
- Preserved the intentional public API baseline, typography mapping `3.1.0`, schema/wire/generated/Pact/Verify surfaces, Story 11.12/11.13 ownership and compatibility, and all unrelated user changes.
- Completed all acceptance criteria and fresh review evidence; story is ready for code review.

### Change Log

- 2026-07-12: Created the missing canonical Story 11.11 artifact as a brownfield confirm-and-pin/reconciliation story and moved sprint tracking from `backlog` to `ready-for-dev`.
- 2026-07-12: Strengthened exact Contracts.UI package dependency/transitive-consumer evidence, advanced package validation to published baseline `2.0.0`, hardened package fixture versions, refreshed current guidance, and moved the story to review.

### File List

Story 11.11 implementation changes:

- `_bmad-output/implementation-artifacts/11-11-create-contracts-ui-assembly-and-migrate-blazor-rendering-surface.md`
- `_bmad-output/implementation-artifacts/epic-11-context.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/project-context.md`
- `docs/diagnostics/README.md`
- `docs/diagnostics/compatibility-suppressions.json`
- `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Architecture/ContractsPackageBoundaryTests.cs`
- `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs`

Reviewed unchanged/evidence-only named exceptions:

- `Hexalith.FrontComposer.Contracts` — named exception: ownership evidence resolves through the unchanged Contracts project and the listed package/ownership tests.
- `net10.0;netstandard2.0` — named exception: target-framework evidence resolves through the unchanged Contracts project and the successful explicit netstandard2.0 build.
- `src/Hexalith.FrontComposer.Contracts.UI/PublicAPI.Shipped.txt` — named exception: exact public API baseline reviewed and intentionally unchanged; full Contracts.UI 10/10 validates it.
- `2.0.0` — named exception: publication and baseline evidence resolves through the configured NuGet.org exact search, the changed Contracts.UI project/tests, and the successful direct package-validation pack.
- `eng/release-package-inventory.json` — named exception: reviewed and intentionally unchanged because Story 11.14 already lists Contracts.UI as packable with symbols; the fresh inventory gate is valid.
- `docs/migrations/1.12-to-2.0.md` — named exception: reviewed and intentionally unchanged because its package/namespace/binary-break guidance remains accurate.
- `.slnx` — named exception: the unchanged `Hexalith.FrontComposer.slnx` was used only for the successful Release restore/build; all tests ran per project.

Pre-existing unrelated working-tree paths documented for mechanical reconciliation only:

- `AGENTS.md`
- `CLAUDE.md`
- `_bmad-output/implementation-artifacts/11-5-dead-css-remediation-and-visual-conformance-guards.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/prompt-fc-nip-continuation-2026-07-12.md`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.Memories`
- `references/Hexalith.PolymorphicSerializations`
- `references/Hexalith.Tenants`
- `tests/e2e/specs/specimen-accessibility.spec.ts`
