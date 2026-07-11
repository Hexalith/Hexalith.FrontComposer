---
title: 'Story 11.11: Create Contracts.UI assembly and migrate Blazor rendering surface'
type: 'refactor'
created: '2026-07-11T16:00:00+02:00'
status: 'blocked'
baseline_revision: '522d83573d36fb24b321922356d2b3c627f8e6fd'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '_bmad-output/project-context.md'
  - '_bmad-output/contracts/fc-contracts-kernel-split-compatibility-plan-2026-07-05.md'
warnings: [oversized]
---

<intent-contract>

## Intent

**Problem:** The net10 face of `Hexalith.FrontComposer.Contracts` owns Blazor `RenderFragment`/`KeyboardEventArgs` APIs and Fluent typography tokens, so every kernel consumer inherits the pinned Fluent prerelease and the package exposes TFM-dependent UI surface.

**Approach:** Create a packable net10-only `Hexalith.FrontComposer.Contracts.UI` assembly, move only the approved Blazor/Fluent rendering and shortcut types into it without changing their namespaces, and make Shell/generated UI consumers reference it while keeping Contracts and SourceTools UI-dependency-free.

## Boundaries & Constraints

**Always:** Preserve moved type namespaces and behavior; keep `Contracts` on `net10.0;netstandard2.0` so Story 11.13 HFC0001 metadata and descriptor trim annotations survive; keep `SourceTools` netstandard2.0 with only a Contracts project reference; retain schema, MCP, CLI, EventStore, generated route/output, and wire shapes; split multi-type source files during the move; intentionally pin the new package public API.

**Block If:** A moved public type requires a Contracts-to-Contracts.UI reference/type forwarder, unchanged generated code cannot compile with an explicit UI reference, or preserving HFC0001/trim metadata requires a new public contract not approved by Stories 11.8/11.13.

**Never:** Add Contracts.UI, Blazor, ASP.NET Components, or Fluent dependencies to Contracts/SourceTools/MCP/Schema/CLI; duplicate same-FQN types or add upward type forwarders/shims; change QueryRequest, customization semantics, submodule source, broad architecture/UX/package-migration guidance, or `eng/release-package-inventory.json` (Story 11.14 owns final compatibility docs/inventory).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Kernel-only consumer | Clean netstandard2.0 or net10 project references only packed Contracts | Restore/build succeeds without Contracts.UI, ASP.NET Components, or Fluent in assets/nuspec | Any transitive UI dependency fails the boundary test |
| UI consumer | Clean net10 project references packed Contracts.UI and uses existing namespaces/FQNs | Typography, fragment contexts, and shortcut event APIs compile and behave unchanged | Missing/mis-owned API fails consumer/public-API tests |
| Generated consumer | SourceTools emits projection customization code with Contracts, Contracts.UI, and Shell available | Generated output compiles without SourceTools referencing UI | Snapshot/FQN churn or analyzer dependency leakage fails |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Contracts/{Rendering,Shortcuts}` -- current guarded Blazor/Fluent surface and retained kernel descriptor/context dependencies.
- `src/Hexalith.FrontComposer.{Shell,SourceTools}` -- runtime consumer and kernel-only generator boundary.
- `tests/Hexalith.FrontComposer.{Contracts,SourceTools,Shell}.Tests` -- ownership, generated-compilation, and behavior evidence.
- `docs/docfx.json`, `.github/workflows/release.yml`, and `Hexalith.FrontComposer.slnx` -- API metadata, explicit release test lane, and solution membership.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`, `src/Hexalith.FrontComposer.Contracts.UI/PublicAPI.Shipped.txt`, and `Hexalith.FrontComposer.slnx` -- add a documented, packable `net10.0` SDK library referencing Contracts, `Microsoft.AspNetCore.App`, and Fluent UI; embed its intentional public-API baseline.
- `src/Hexalith.FrontComposer.Contracts.UI/Rendering/{Typography,FcTypoToken,TypographyStyle,FieldSlotContext,ProjectionTemplateContext,ProjectionTemplateSectionRenderer,ProjectionTemplateRowRenderer,ProjectionTemplateFieldRenderer,ProjectionTemplateColumnDescriptor,ProjectionTemplateSectionDescriptor,ProjectionViewContext}.cs`, `src/Hexalith.FrontComposer.Contracts.UI/Shortcuts/{IShortcutService,ShortcutBinding}.cs`, and the same-named current files under `src/Hexalith.FrontComposer.Contracts/{Rendering,Shortcuts}` -- move the approved types, retain namespaces, remove obsolete TFM guards, and put every type/delegate in its own named file. Keep plain kernel descriptors/registries/version constants/`ShortcutRegistration` in Contracts and replace their cross-assembly XML `cref` links with non-binding code references where required.
- `src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj`, `src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj`, `src/Hexalith.FrontComposer.Schema/Hexalith.FrontComposer.Schema.csproj`, and `src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj` -- remove Contracts' Fluent reference and obsolete NU5104 suppressions, retain both kernel TFMs, and add Shell's direct Contracts.UI dependency without changing SourceTools.
- `samples/Counter/Counter.Domain/Counter.Domain.csproj`, `samples/Counter/Counter.Specimens.Domain/Counter.Specimens.Domain.csproj`, `samples/Counter/Counter.Specimens/Counter.Specimens.csproj`, `samples/Counter/Counter.Web/Counter.Web.csproj`, `samples/IdeParityCounter/IdeParityCounter.csproj`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj`, and `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj` -- add explicit UI references wherever authored/generated code consumes the moved surface; do not rely on accidental transitivity.
- `tests/Hexalith.FrontComposer.Contracts.UI.Tests/Hexalith.FrontComposer.Contracts.UI.Tests.csproj`, `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`, `tests/Hexalith.FrontComposer.Contracts.UI.Tests/Rendering/{TypographyConstantsTests,ProjectionSlotContractsTests,ProjectionTemplateContractsTests,ProjectionViewOverrideContractsTests}.cs`, `tests/Hexalith.FrontComposer.Contracts.Tests/Architecture/{ContractsKernelOwnershipTests,ContractsPackageBoundaryTests}.cs`, and `tests/Hexalith.FrontComposer.Contracts.Tests/Rendering/{TypographyConstantsTests,ProjectionSlotContractsTests,ProjectionTemplateContractsTests,ProjectionViewOverrideContractsTests}.cs` -- create the UI test project, relocate only UI-owned assertions while retaining kernel descriptor/registry assertions, pin old/new assembly-qualified ownership, compare exported API with the baseline, inspect both nuspec/assets graphs, and compile clean kernel-only and UI consumers.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/CompilationHelper.cs` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs` -- add the explicit Contracts.UI metadata/package reference needed by generated compilations while proving SourceTools still embeds/references only SourceTools and Contracts and generated FQNs/snapshots do not drift.
- `docs/docfx.json` and `.github/workflows/release.yml` -- include the Contracts.UI assembly in API metadata and its tests in the release test list; leave Story 11.14's architecture/UX/migration prose and release inventory untouched.

**Acceptance Criteria:**
- Given a clean kernel-only project targeting netstandard2.0 or net10, when it restores/builds against packed Contracts, then neither its assets nor the Contracts nuspec/assembly references contain Contracts.UI, ASP.NET Components, or Fluent, while HFC0001 and net10 trim metadata remain intact.
- Given existing adopter source using the moved `Hexalith.FrontComposer.Contracts.Rendering` and `.Shortcuts` FQNs, when it references Contracts.UI, then it compiles without namespace/source changes and the new package baseline contains every approved moved type exactly once.
- Given SourceTools and a generated projection/template consumer, when analyzer and compile/package tests run, then SourceTools references only Contracts, generated UI compiles with Contracts.UI, and generated text, routes, schema/wire contracts, and snapshots have no unrelated drift.
- Given Shell, Testing, Counter, and IDE-parity consumers, when focused and solution Release validation runs, then all moved rendering/shortcut behavior remains green and no repository-owned project depends on the removed Contracts UI face accidentally.

## Spec Change Log

## Review Triage Log

## Design Notes

Type forwarding from Contracts is forbidden because it would require the kernel to depend upward on Contracts.UI and would reintroduce Fluent transitively. Unchanged namespaces plus an explicit package reference preserve source compatibility; the assembly-qualified identity move is an intentional pre-v1.0 binary break pinned by ownership/API tests. Keeping both Contracts TFMs is the narrow implementation adjustment required to preserve the already-landed Story 11.13 HFC0001 and net10 trim annotations; both faces become UI-dependency-free.

## Verification

**Commands:**
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false && dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 /nr:false` -- owned solution and both Contracts TFMs build warning-free.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj -c Release --no-restore && DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Contracts.UI.Tests/Hexalith.FrontComposer.Contracts.UI.Tests.csproj -c Release --no-restore` -- kernel/UI ownership, package graphs, clean consumers, and public API pass.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --no-restore && DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore` -- generator boundary and runtime behavior pass.
- `pwsh ./eng/validate-docs.ps1 && DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" && git diff --check` -- docs, default lane, and whitespace gate pass.
- `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --format Json --non-interactive --nologo` -- broad runtime baseline; known pre-story blocker is the Tenants submodule's stale pre-11.12 `FcShellOptions` reference and must be reported separately if unchanged.

## Auto Run Result

Status: blocked
Blocking condition: implementation verification failed because the required packable `Contracts.UI` project is rejected by the mandatory release-inventory governance gate unless `eng/release-package-inventory.json` is updated, while this spec's read-only intent contract explicitly forbids editing that Story 11.14-owned file; marking the project non-packable would instead violate Story 11.11's package requirement.

Focused evidence: Contracts.UI behavior/package tests pass (9/9), Contracts kernel/package tests pass (200/200), and Shell behavior tests pass 2220/2221 with only `PackageInventory_IsExplicitLockstepAndReviewable` failing on the missing Contracts.UI inventory row. The Aspire baseline remains independently blocked by stale pre-11.12 `FcShellOptions` references in the Tenants submodule.
