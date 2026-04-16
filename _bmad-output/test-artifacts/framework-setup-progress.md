---
stepsCompleted: ['step-01-preflight', 'step-02-select-framework', 'step-03-scaffold-framework', 'step-04-docs-and-scripts', 'step-05-validate-and-summary']
lastStep: 'step-05-validate-and-summary'
lastSaved: '2026-04-16'
status: 'complete'
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

## Step 3: Scaffold Framework

### Execution Mode

- Resolved: `sequential` (single-agent run; no subagent orchestration used)
- Probe not executed (single-agent context)

### Files Created

```
tests/e2e/
├── .env.example
├── .gitignore
├── .nvmrc                     (Node 24 LTS)
├── package.json               (Playwright + playwright-utils + axe-core + Faker + TypeScript)
├── playwright.config.ts       (3 projects: chromium/firefox/webkit; HTML+JUnit+list reporters;
│                               trace retain-on-failure, screenshot only-on-failure,
│                               video retain-on-failure; actionTimeout 15s, navTimeout 30s, test 60s)
├── tsconfig.json              (strict TS, ESNext, path aliases)
├── factories/
│   └── counter.factory.ts     (Faker-based IncrementCommand factory + batch helper)
├── fixtures/
│   ├── index.ts               (mergeTests composition root)
│   ├── lifecycle.fixture.ts   (five-state command lifecycle assertions — architecture Row 2)
│   └── tenant.fixture.ts      (multi-tenancy fixture — architecture Row 1)
├── helpers/
│   ├── a11y.ts                (axe-core WCAG 2.1 AA helper — architecture Row 5)
│   ├── api-client.ts          (APIRequestContext factory with tenant/user headers)
│   └── auth.ts                (seedDemoSession: localStorage + cookie seeding)
├── page-objects/
│   └── counter.page.ts        (CounterPage POM: goto/increment/decrement/value)
└── specs/
    ├── lifecycle.spec.ts      (increment command: idle -> submitting -> success)
    └── smoke.spec.ts          (render + axe-core zero-violations)
```

### Knowledge Patterns Applied

- ✅ `fixtures-composition.md`: `mergeTests` pattern for fixture composition at `fixtures/index.ts`
- ✅ `auth-session.md`: session seeding helper bound to `DemoUserContextAccessor` contract
- ✅ `data-factories.md`: Faker-based factories with overrides pattern
- ✅ `api-request.md`: dedicated `APIRequestContext` factory separate from browser context
- ✅ `playwright-config.md`: timeouts, artifacts, reporters per step guidance
- ✅ Architecture-specific: axe-core fixture (Row 5), five-state lifecycle fixture (Row 2), tenant fixture (Row 1)

### Deferred / Not Applied

- ⏭️ `intercept-network-call.md`, `network-error-monitor.md`: not yet needed (Counter sample has no external API surface to intercept). Add when downstream stories introduce REST calls.
- ⏭️ `recurse.md`, `burn-in.md`: pattern knowledge documented in next step's README; not wired into specs yet.
- ⏭️ Pact/CDC: disabled via `config.tea_use_pactjs_utils = false`.
- ⏭️ Backend xUnit scaffold: existing `tests/Hexalith.FrontComposer.*.Tests/` projects already satisfy the C#/.NET requirement.

### Decisions & Assumptions (to validate before running tests)

- **data-testid contract**: specs assume UI exposes `data-testid="fc-counter-value"`, `fc-counter-increment`, `fc-counter-decrement`, and `fc-lifecycle-{commandId}`. If the generator does not emit these yet, specs will fail — this is intentional and drives generator instrumentation (ATDD-friendly).
- **Session seeding keys**: `hfc.session.tenantId` / `hfc.session.userId` — confirm against `IStorageService` key scheme before first run.
- **BASE_URL default**: `https://localhost:7000` is a placeholder; Counter.AppHost prints the real URL on startup. User should copy `.env.example` to `.env.local`.
- **Node 24 LTS**: enforced via `.nvmrc` and `engines.node` in `tests/e2e/package.json`.

## Step 4: Documentation & Scripts

### Files Created / Updated

| File | Action | Purpose |
|---|---|---|
| `tests/README.md` | created | Top-level guide: layer map, quick start (dotnet + Playwright), E2E architecture, best practices, CI integration, extension recipes |
| `package.json` (root) | updated | Added convenience scripts: `test:e2e`, `test:e2e:install`, `test:e2e:ui`, `test:e2e:report`, `test:dotnet` |

### Script Surface

```bash
# From repo root — .NET
npm run test:dotnet                 # dotnet test across entire solution
dotnet test --collect:"XPlat Code Coverage"  # with coverage

# From repo root — E2E (convenience)
npm run test:e2e:install            # install deps + browsers (one-time)
npm run test:e2e                    # headless, all browsers
npm run test:e2e:ui                 # Playwright UI mode
npm run test:e2e:report             # open last HTML report

# From tests/e2e — fine-grained
npm --prefix tests/e2e run test:chromium
npm --prefix tests/e2e run test:smoke
npm --prefix tests/e2e run test:lifecycle
npm --prefix tests/e2e run typecheck
```

### Deferred

- CI pipeline wiring (GitHub Actions workflow) — the next skill `bmad-testarch-ci` owns this.

## Step 5: Validation & Summary

### Checklist Result

| Section | Result | Notes |
|---|---|---|
| Prerequisites | ✅ pass | manifests present, no conflicts |
| Process steps 1–11 | ✅ pass | one ⚠ on factory `cleanup()` — deferred with rationale |
| Output validation | ✅ pass | syntactically valid; no secrets; no TODO/FIXME |
| Code quality | ✅ pass | strict TS, no `any`, no unused imports |
| Best practices | ✅ pass | fixture composition, data-testid, no hard waits |
| Knowledge base alignment | ✅ pass | `fixtures-composition`, `data-factories`, `auth-session`, `api-request`, `playwright-config`, `test-quality` patterns applied |
| Security | ✅ pass | env placeholders only; `.env*.local` git-ignored |

### Framework Selected

- **E2E (browser)**: Playwright 1.49+ (TypeScript, Node 24 LTS)
- **Backend (unit)**: xUnit (pre-existing across `tests/Hexalith.FrontComposer.*.Tests/`)

### Artifacts Created (summary)

- `tests/README.md` — top-level test architecture guide
- `tests/e2e/` — 15 files (see Step 3 tree)
- `package.json` (root) — 5 new convenience scripts

### Next Steps for User

1. `cp tests/e2e/.env.example tests/e2e/.env.local` — configure `BASE_URL`
2. `npm run test:e2e:install` — install Playwright + browsers (one-time)
3. In another terminal: `dotnet run --project samples/Counter/Counter.AppHost`
4. Update `.env.local` with the Counter.Web URL Aspire prints
5. `npm run test:e2e` — initial run (expect spec failures until generator emits the `data-testid` contract — that drives the ATDD loop)

### Recommended Follow-Up Workflows

- `bmad-testarch-ci` — wire Playwright + dotnet tests into CI
- `bmad-testarch-atdd` — use the lifecycle fixture in acceptance tests for open stories (2-4 ATDD checklist already exists)
- `bmad-testarch-test-design` — broader coverage plan once more UI surfaces land

### Status

**COMPLETE** — Framework setup workflow finished. No blockers.




