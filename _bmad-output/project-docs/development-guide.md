# Hexalith.FrontComposer — Development Guide

> **Generated:** 2026-06-02 · deep scan. Commands verified against [.github/workflows/ci.yml](.github/workflows/ci.yml). Source-generator debugging guidance from [CONTRIBUTING.md](CONTRIBUTING.md).

## Prerequisites

| Tool | Version / note |
|---|---|
| .NET SDK | **`10.0.301`** (pinned in [global.json](global.json), `rollForward: latestPatch`) |
| Node.js | **`>=24.0.0`** for the Playwright e2e workspace; `tests/e2e/.nvmrc` pins Node `24` |
| npm | **`>=10`** for the e2e workspace dependencies |
| PowerShell (`pwsh`) | for the `eng/*.ps1` validation scripts and the docs gate |
| dotnet local tools | `dotnet tool restore` (provides DocFX for the docs gate) |
| Playwright | only for the accessibility/visual e2e lane (`tests/e2e`) |
| Aspire CLI | only to run the `samples/Counter` orchestrated sample |

> **MCP servers** configured for this repo ([.mcp.json](.mcp.json)): `fluent-ui-blazor` (component/icon reference) and `aspire`. Recommended for contributors per [ONBOARDING.md](ONBOARDING.md).

## Get the code (submodules)

```bash
git clone https://github.com/Hexalith/Hexalith.FrontComposer.git
cd Hexalith.FrontComposer
git submodule update --init   # root-declared references/ submodules only
```

> **Submodule rule:** initialize/update only the **root-declared** submodules under `references/` declared in [.gitmodules](.gitmodules). **Do not** recurse into nested submodules; deinit any that get pulled in accidentally. Never modify submodule files without explicit approval.

Debug builds reference the submodules as **local `ProjectReference`s** ([deps.local.props](deps.local.props)). Release/package builds reference the published NuGet packages instead ([deps.nuget.props](deps.nuget.props)); direct `references/Hexalith.*` solution entries are disabled for `Release|*`. Use `-p:UseHexalithProjectReferences=true` only for an intentional source-debug Release session. `-p:UseNuGetDeps=true|false` remains the legacy inverse switch.

## Solution & build

**Use [Hexalith.FrontComposer.slnx](Hexalith.FrontComposer.slnx) only** — the modern XML solution format. Never create/use `.sln`.

```bash
# Restore (centralized package versions live in Directory.Packages.props)
dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release

# Gate 1 — Contracts must build in isolation on netstandard2.0 (analyzer-host TFM)
dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj \
  -f netstandard2.0 --configuration Release --no-restore

# Gate 2 — full solution
dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore
```

Build invariants ([Directory.Build.props](Directory.Build.props)): `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, **`TreatWarningsAsErrors=true`** (analyzer/style warnings fail the build). Never put `Version=` in a `.csproj` — edit [Directory.Packages.props](Directory.Packages.props).

## Test

> **Divergence from the EventStore submodule:** FrontComposer runs **solution-level `dotnet test`** with **trait filters** (all tests are one tier). This is the opposite of `Hexalith.EventStore`, which runs per-project. Follow FrontComposer's model in this repo.

```bash
# Default blocking lane — unit + bUnit, with coverage.
# DiffEngine_Disabled=true is REQUIRED so a Verify snapshot mismatch doesn't launch a diff tool and hang.
DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build \
  --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" \
  --collect:"XPlat Code Coverage"

# Single project (e.g. the generator tests)
DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj
```

**Trait categories** used as filters: `Governance`, `Contract`, `Performance`, `e2e-palette`, `NightlyProperty`, `Quarantined`. CI runs Governance and the default lane as **blocking**; palette/perf/quarantine lanes are advisory/warning-only.

**Test stack:** xUnit **v3** (`xunit.v3` 3.2.2), Shouldly 4.3.0 (assertions — never raw `Assert.*`), NSubstitute 5.3.0 (mocks), bUnit 2.7.2 (Blazor components), Verify 31.19.0 (snapshots; `Verify.XunitV3`, not `Verify.Xunit`), FsCheck.Xunit.v3 3.3.3 (property tests), PactNet 5.0.1 (consumer contracts), BenchmarkDotNet 0.15.8 (in a **separate** `Shell.Tests.Bench` exe), coverlet 10.0.1.

**Test conventions:** three-part names `Subject_Scenario_Expectation`; `.verified.txt` snapshots are committed and updated intentionally; generator tests go through `CompilationHelper.CreateCompilation()`; Blazor component tests use `GeneratedComponentTestBase`/`AddFrontComposerTestHost` with `JSInterop.Mode = Loose`; public API baselines are enforced intentionally (`PublicAPI.Shipped.txt` in the Testing library via `PackageBoundaryTests`, and the focused Shell FC-TBL surface in `PublicAPI.FcTbl.Shipped.txt` via `FcTblPackageBoundaryTests`); the **NFR17 tripwire** must be updated alongside any new `IStorageService.SetAsync` call site; CI fails on a **stale pact diff**.

### End-to-end (accessibility / visual)

```bash
cd tests/e2e
nvm use        # optional, but recommended; reads tests/e2e/.nvmrc (Node 24)
npm ci
npx playwright install --with-deps chromium
npm run typecheck
npm run test:a11y          # accessibility + keyboard + media + zoom + visual specimen gate
```

CI installs Chromium only for the accessibility/visual lane. The Playwright config also declares Firefox and WebKit projects for local cross-browser checks; install those optional browser payloads with `npm run install:browsers` from `tests/e2e` or `npm run test:e2e:install` from the repository root.

The e2e lane builds the `samples/Counter/Counter.Web` specimen host with `Hexalith__FrontComposer__Specimens__Enabled=true` and `ASPNETCORE_ENVIRONMENT=Test`. Root convenience scripts in [package.json](package.json): `npm run test:e2e:install`, `test:e2e`, `test:e2e:a11y`, `test:e2e:visual(:update)`, `test:e2e:ui`, `test:e2e:report`.

## Using the CLI locally

```bash
# Pack + install the frontcomposer dotnet tool from a local source
dotnet pack src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj -c Release -o ./nupkgs -p:PackageVersion=0.0.0-ci
dotnet new tool-manifest --force
dotnet tool install Hexalith.FrontComposer.Cli --add-source ./nupkgs --version 0.0.0-ci

# Inspect generated output / diagnostics for a consuming project
dotnet tool run frontcomposer inspect --project path/to/App.csproj --configuration Debug --framework net10.0 --format json

# Plan/apply a migration
dotnet tool run frontcomposer migrate --project path/to/App.csproj --from 9.1.0 --to 9.2.0 --dry-run --format json
```

See [api-contracts.md](./api-contracts.md) §3 for all options and exit codes.

## Debugging the source generator (contributors)

From [CONTRIBUTING.md](CONTRIBUTING.md):

- Use `Debugger.Launch()` only on short-lived local branches, behind a narrow condition (a specific generated type name or analyzer-config flag), and **remove it before review**. Production source under `src/**/*.cs` must contain no `Debugger.Launch()` — the IDE-parity regression suite source-scans for it.
- If a generator change appears stale: close design-time builds, run `dotnet build-server shutdown`, then rebuild. The compiler server caches analyzer/generator assemblies.
- If breakpoints don't hit, rebuild with shared compilation off:
  ```powershell
  dotnet build Hexalith.FrontComposer.slnx -p:UseSharedCompilation=false
  ```
- Validate generated-output layout in **both Debug and Release** — `obj/{Config}/{TFM}/generated/HexalithFrontComposer/` is a public path contract (`GeneratedOutputPathContract.Template`).
- Don't broaden Roslyn package pins (`Microsoft.CodeAnalysis.CSharp` 5.3.0) as part of generator debugging — IDE loading is sensitive to it.
- Inspect generated output without an IDE via `frontcomposer inspect`.

## Documentation site (DocFX)

The **published** docs site lives under [docs/](docs/) (Diataxis). It is CI-gated (Gate 2d) and **separate from this BMAD output**:

```bash
dotnet tool restore
pwsh ./eng/validate-docs.ps1      # structure + diagnostics + API-summary validation
dotnet docfx docs/docfx.json      # build the site
dotnet docfx serve docs/_site     # preview
```

> Do not write generated/scratch docs into `docs/` — it's referenced by product code, tests, CI, and validation fixtures.

## Samples

| Sample | What it is |
|---|---|
| `samples/Counter/` | FrontComposer demo: `Counter.Domain`, `Counter.Web` (Blazor shell host), `Counter.Specimens(.Domain)` (a11y/visual specimen host). The `counter-web` resource is orchestrated by the **single platform host** `src/Hexalith.FrontComposer.AppHost` (`aspire run` / `dotnet run` on that host); `Counter.Web` also runs standalone for the specimen/e2e gate. |
| `samples/IdeParityCounter/` | Minimal counter used by the IDE-parity revalidation job. |

## eng/ scripts

`validate-docs.ps1` (docs gate), `validate-contract-artifacts.ps1` (Pact), `validate-property-artifacts.ps1` + `run-lifecycle-property-suite.ps1` (FsCheck property suite), `validate-stryker-reports.ps1` (mutation testing), `release_evidence.py` (release pipeline), `llm_benchmark.py` (skill-corpus prompt benchmark), `release-package-inventory.json` (expected NuGet package set).
