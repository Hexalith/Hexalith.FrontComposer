# Fluent UI v5 Contingency Plan

> **Audience:** A developer who has *never* read the architecture doc. This plan must be actionable on its own.
>
> **Current state:** `Microsoft.FluentUI.AspNetCore.Components` is pinned at the **exact** RC version `5.0.0-rc.2-26098.1` in `Directory.Packages.props` (line 15). FrontComposer's shell, DataGrid, navigation, and Epic 2 form components are built against this RC. Any subsequent Fluent UI RC or GA release may contain breaking changes.
>
> **Purpose of this document:** Give an adopter a concrete, step-by-step procedure to (a) move to a newer Fluent UI version when it ships, (b) validate that the load-bearing APIs still work, (c) roll back cleanly if something breaks, and (d) understand the canary workflow that will (W2 / Epic 3) catch regressions against pre-release versions automatically.

---

## 1. Version Pin Update Procedure

All Fluent UI versions are pinned centrally in `Directory.Packages.props`. Never bump a version in a project-level `.csproj`.

1. **Identify the target version.** Either a new RC (e.g., `5.0.0-rc.3-...`) or the GA release (`5.0.0`). Prefer exact pins (no floating `*` or `-*` ranges) so builds remain reproducible.
2. **Update the pin.** Edit `Directory.Packages.props` and replace the existing line with the new exact version:
   ```xml
   <PackageVersion Include="Microsoft.FluentUI.AspNetCore.Components" Version="5.0.0-rc.3-<suffix>" />
   ```
   Keep the central-package-management layout; do not add a `Version="..."` attribute inside any `<PackageReference>`.
3. **Restore and rebuild.**
   ```bash
   dotnet restore
   dotnet build
   ```
   If restore fails with `NU1102` (package not found on configured feeds), the target RC is not yet published to the public NuGet feed — wait, or add the Fluent UI nightly feed if you know the version exists on it.
4. **Validate the load-bearing APIs** (see §2). Every row in that checklist must be green before the pin is considered adopted.
5. **Run the full test suite:**
   ```bash
   dotnet build
   dotnet test --no-build
   ```
   Baseline is 204 tests (Contracts 9, Shell 43, SourceTools 152). A bump must leave this green.
6. **Manually verify the Counter sample** (`samples/Counter/Counter.Web`) via `dotnet watch` — see `docs/hot-reload-guide.md` for the human-validation steps.
7. **Commit the pin change in its own commit** with message `chore(deps): bump Fluent UI to <version>`. This gives a single clean revert target if rollback is needed (§4).

### Validating a rollback command

The rollback / pin-update command template is:

```bash
dotnet add <project> package Microsoft.FluentUI.AspNetCore.Components --version "5.0.0-rc.2-26098.1"
```

**Quoting / escaping notes for this command:**

- The version **must** be quoted because the suffix contains a `-` that some shells can misinterpret as an option flag if omitted.
- On **PowerShell (pwsh)** on Windows, use single quotes around the whole value if you need to paste a version containing `$` or `!` (neither occurs in the current pin, but future RC suffixes could). Example: `--version '5.0.0-rc.3-26xxxxx.1'`.
- On **cmd.exe**, double quotes are mandatory and single quotes are treated literally — do not use single quotes there.
- On **bash / zsh**, either single or double quotes work; prefer double quotes for consistency with the Windows guidance above.
- `dotnet add package` alone (without `--version`) resolves the **highest** version permitted by the central-package-management floor, which defeats the point of an exact pin — always pass `--version` explicitly when scripting.

> **Preferred path:** Edit `Directory.Packages.props` directly rather than calling `dotnet add package`. `dotnet add package` under central-package-management writes a `<PackageVersion>` line into `Directory.Packages.props`, which can produce a duplicate entry if one already exists — a manual edit is less error-prone.

## 2. Load-Bearing API Validation Checklist

After a version bump, **every** row below must be verified. A red cell blocks adoption and triggers the rollback procedure (§4).

| # | API / component                                  | Where it is used                                        | Verify                                                                                      | Status |
|---|--------------------------------------------------|---------------------------------------------------------|---------------------------------------------------------------------------------------------|--------|
| 1 | `FluentLayout` + `FluentLayoutItem`              | Shell layout (`MainLayout.razor` in `Counter.Web`)      | Shell renders with header / sidebar / content regions; no console errors.                   | ☐ |
| 2 | `<FluentProviders />` (v5 replacement for v4 `<FluentDesignSystemProvider>`) | App-level provider registration             | Page loads without JS provider-resolution errors in the browser console.                    | ☐ |
| 3 | `DefaultValues` (application-wide component defaults — button appearance, density, etc.) | Shell startup configuration                        | Default appearance for buttons / inputs matches what it was before the bump.                | ☐ |
| 4 | `FluentDataGrid`                                  | Primary projection rendering                            | Columns render, sorting / virtualization still work, accessibility tree includes `<table>`. | ☐ |
| 5 | `FluentNav` (renamed from `FluentNavMenu` in v5) | Sidebar navigation                                      | Bounded-context groups render; active item highlights; keyboard nav works.                  | ☐ |
| 6 | Epic 2 form components: `FluentTextField`, `FluentCheckbox`, `FluentDatePicker`, `FluentSelect`, `FluentNumberField` | Generated command forms (when Epic 2 lands) | Each component binds correctly to the Fluxor store; validation messages display.            | ☐ |
| 7 | Toast / message bar (`FluentMessageBar`, Toast component in RC2) | Command lifecycle feedback                                  | Success / error notifications render; `IToastService` is **not** referenced (removed in v5). | ☐ |

### Known v4 → v5 breaking changes to watch for

- `FluentNavMenu` → `FluentNav` (**renamed**).
- `IToastService` → **removed**; use `FluentMessageBar` or the new Toast component shipped in RC2.
- `SelectedOptions` → `SelectedItems` (binding property renamed).
- `FluentDesignTheme` → CSS custom properties (theming moved to CSS vars).
- `<FluentDesignSystemProvider>` → `<FluentProviders />` (simplified).
- Miscellaneous property / attribute renames to align with Fluent UI React v9.

If you hit a breaking change not listed above, the Fluent UI **MCP Server migration service** (new in RC2) can assist component-by-component. It is recommended for large surface-area migrations rather than best-effort grep-and-replace.

## 3. Migration Effort Estimate

- **Budget:** **1–2 weeks** for a solo developer to bump from the current pin to Fluent UI v5 GA.
- **Driver:** Surface area of breaking renames across shell, DataGrid, nav, and Epic 2 form components — not algorithmic complexity.
- **Accelerators:**
  - Fluent UI MCP Server migration service (RC2+) for mechanical renames.
  - `rg --multiline` against `FluentNavMenu`, `IToastService`, `SelectedOptions`, `FluentDesignTheme`, `FluentDesignSystemProvider` as a first pass.
  - The load-bearing checklist in §2 as the acceptance gate — do not declare migration complete until every row is green.

## 4. Rollback Procedure

If the load-bearing checklist (§2) or the test suite fails after a version bump, roll back immediately:

1. **Revert the commit** that bumped the pin (the "one commit per bump" rule in §1 makes this a single revert):
   ```bash
   git revert <commit-sha>
   ```
2. **Or** edit `Directory.Packages.props` back to the previous exact version — currently `5.0.0-rc.2-26098.1`:
   ```xml
   <PackageVersion Include="Microsoft.FluentUI.AspNetCore.Components" Version="5.0.0-rc.2-26098.1" />
   ```
3. Run `dotnet restore && dotnet build && dotnet test --no-build` to confirm the baseline is green again.
4. Open an issue describing what broke (which checklist row, what error, what environment) and link it from this document.

Do **not** attempt to "patch forward" (shim missing APIs, revert only part of the change) — the exact pin exists precisely so rollback is atomic.

## 5. Canary Workflow (W2 / Epic 3 implementation — skeleton documented here)

The canary workflow is **not yet implemented**. It is described here so that W2 / Epic 3 implementation has an actionable specification:

**Filename:** `.github/workflows/canary-fluentui.yml`
**Trigger:** Weekly, Monday 06:00 UTC (`cron: "0 6 * * 1"`) and `workflow_dispatch`.

**Structure:**

```yaml
# .github/workflows/canary-fluentui.yml
# Weekly canary that overrides the Fluent UI pin to the latest pre-release
# and verifies FrontComposer still builds and tests green. Does NOT block
# PRs (advisory only). Failures open a labeled GitHub issue.
name: canary-fluentui
on:
  schedule:
    - cron: "0 6 * * 1"          # Mondays 06:00 UTC
  workflow_dispatch:

jobs:
  canary:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          submodules: recursive
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Resolve latest Fluent UI pre-release
        id: fluentui
        run: |
          # NOTE: `dotnet nuget search --format json | jq` is FRAGILE —
          # the JSON shape has changed between dotnet SDK minors.
          # Before trusting the output below, W2 must:
          #   1) Add a `dotnet nuget search --format json ... | jq 'has("searchResult")'`
          #      validation step (fail the job if shape changes).
          #   2) Provide a fallback: on jq failure, call the NuGet v3 Search
          #      API directly via `curl` + parse — treat that result as
          #      authoritative.
          #   3) Pin the jq version used in CI for reproducibility.
          version=$(dotnet nuget search Microsoft.FluentUI.AspNetCore.Components \
                      --prerelease --format json \
                    | jq -r '.searchResult[0].packages[0].latestVersion')
          echo "version=$version" >> "$GITHUB_OUTPUT"

      - name: Override pin
        run: |
          # Temporarily bump the central package version to the resolved pre-release
          sed -i -E "s|(<PackageVersion Include=\"Microsoft.FluentUI.AspNetCore.Components\" Version=\")[^\"]+(\".*)|\1${{ steps.fluentui.outputs.version }}\2|" Directory.Packages.props

      - name: Build
        run: dotnet build

      - name: Test
        run: dotnet test --no-build

      - name: Create issue on failure
        if: failure()
        uses: actions/github-script@v7
        with:
          script: |
            github.rest.issues.create({
              owner: context.repo.owner,
              repo: context.repo.repo,
              title: `[canary] Fluent UI ${{ steps.fluentui.outputs.version }} broke the build`,
              labels: ['canary-failure'],
              body: 'See workflow run: ' + context.runId
            });
```

**Known fragility (must be addressed before the canary is trusted):**

- `dotnet nuget search --format json | jq` output shape has changed between dotnet SDK minors. W2 **must** add a validation step (fail fast if the expected keys are missing) and a fallback to the NuGet v3 Search HTTP API.
- `sed -i -E` with a regex over XML is brittle; prefer an `XmlPoke`-style MSBuild target or a small dotnet script once W2 implementation begins.

## 6. Human Process Step (ADR-003 requirement)

**Subscribe to `microsoft/fluentui-blazor` releases** on GitHub to be notified of pre-release and GA drops. This is a **manual** action — the repository should subscribe a team-owned watcher account (not a single individual) so notifications don't disappear when ownership changes.

- Repository: <https://github.com/microsoft/fluentui-blazor>
- Watch setting: **Releases only**.

## 7. References

- Architecture: **ADR-003** (Fluent UI contingency).
- Central pin: `Directory.Packages.props` (line 15, pinned to `5.0.0-rc.2-26098.1`).
- Hot reload / inner-loop: `docs/hot-reload-guide.md`.
- Story spec: `_bmad-output/implementation-artifacts/1-8-hot-reload-and-fluent-ui-contingency.md`.
