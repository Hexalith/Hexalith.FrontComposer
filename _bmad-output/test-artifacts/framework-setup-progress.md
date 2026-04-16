---
stepsCompleted: ['step-01-preflight', 'step-02-select-framework']
lastStep: 'step-02-select-framework'
lastSaved: '2026-04-16'
---

# Test Framework Setup — Progress

## Step 1: Preflight

### Stack Detection

- **Config `test_stack_type`**: `auto`
- **Auto-detected stack**: `fullstack`
  - Backend indicators: `Hexalith.FrontComposer.sln`, 3 src `.csproj` projects (.NET 10), 3 test `.csproj` projects (xUnit)
  - Frontend indicators: Blazor (`Microsoft.NET.Sdk.Razor`) in `Hexalith.FrontComposer.Shell` with FluentUI components; sample Blazor web app at `samples/Counter/Counter.Web`
  - Root `package.json` is tooling-only (husky/commitlint/semantic-release)

### Project Context

- **Primary language/runtime**: C# / .NET 10
- **UI framework**: Blazor + Microsoft FluentUI AspNetCore Components + Fluxor (state mgmt)
- **Sample host**: `samples/Counter/Counter.AppHost` (.NET Aspire AppHost)
- **Architecture doc**: `_bmad-output/planning-artifacts/architecture.md`
  - **Playwright explicitly required** for:
    - Five-state command lifecycle E2E assertions (v0.1 basic → v1 full)
    - axe-core accessibility enforcement at Playwright level (WCAG 2.1 AA)
  - Target: v0.1 smoke tests (attribute→generator→render pipeline)
- **Prior test artifacts**: `_bmad-output/test-artifacts/atdd-checklist-2-4.md` (Story 2-4 ATDD scope)

### Prerequisites Check

| Requirement | Status |
|---|---|
| `package.json` exists (frontend/fullstack path) | ✅ |
| No existing E2E framework at root (`playwright.config.*`, `cypress.config.*`) | ✅ |
| Backend project manifest exists (`*.csproj`/`*.sln`) | ✅ |
| No conflicting backend test suite | ✅ (xUnit unit tests are compatible) |
| Architecture/stack context available | ✅ (`architecture.md`) |

### Notes / Flags for Subsequent Steps

- Auto-detect biased toward JS/TS frontends — manually confirmed Blazor web UI target exists and Playwright is the architecture-mandated E2E tool.
- Isolated submodule Playwright config at `Hexalith.Tenants/Hexalith.FrontShell/apps/shell/e2e/` is NOT part of main project scope.
- Test projects directory: `D:/Hexalith.FrontComposer/tests/` (existing xUnit projects).
- Suggested E2E test root: `D:/Hexalith.FrontComposer/tests/e2e/` (or `tests/Hexalith.FrontComposer.E2E/`) — to be decided in next steps.
- CI platform config: `auto` (to be detected later).

## Step 2: Framework Selection

### Decision

| Layer | Framework | Reason |
|---|---|---|
| **Browser E2E** | **Playwright** (TypeScript) | Architecture-mandated; multi-browser WCAG coverage; CI parallelism; already in use in sibling submodule (`Hexalith.Tenants/Hexalith.FrontShell`) |
| **Backend / unit** | **xUnit** | Default for .NET; already established in existing `tests/Hexalith.FrontComposer.*.Tests/` projects |

### Rationale Highlights

- **Playwright** chosen over Cypress for:
  - Multi-browser support (axe-core accessibility pass must cover Chromium + Firefox + WebKit per WCAG 2.1 AA architecture row)
  - Better parallelism for the five-state command lifecycle assertions (v0.1 smoke → v1 full)
  - Native API + UI interaction for Blazor server-side render roundtrips
  - Organizational consistency with sibling submodule Playwright setup
- **xUnit** chosen because:
  - `Hexalith.FrontComposer.Shell.Tests`, `Hexalith.FrontComposer.Contracts.Tests`, `Hexalith.FrontComposer.SourceTools.Tests` already use it
  - bUnit (Blazor component testing) layers cleanly on top of xUnit
  - FsCheck (property-based) integrates with xUnit in `SourceTools.Tests`

### Config Respect

- `config.test_framework = "auto"` → auto-selected Playwright + xUnit
- No explicit overrides; decision is documented for future revisit if user wants Cypress

