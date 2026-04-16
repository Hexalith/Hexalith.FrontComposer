# Hexalith.FrontComposer — Test Architecture

Top-level guide for every test layer in this repository. Each layer has its own project + tooling; this file is the map.

## Test Layers

| Layer | Location | Framework | Purpose |
|---|---|---|---|
| **Unit — Contracts** | `tests/Hexalith.FrontComposer.Contracts.Tests/` | xUnit | Contract types, options, diagnostics |
| **Unit — Shell** | `tests/Hexalith.FrontComposer.Shell.Tests/` | xUnit + bUnit | Services, lifecycle state machine, components |
| **Unit — SourceTools** | `tests/Hexalith.FrontComposer.SourceTools.Tests/` | xUnit + FsCheck | Roslyn analyzers, source generators, property-based |
| **E2E — Browser** | `tests/e2e/` | Playwright (TS) | Five-state command lifecycle, WCAG 2.1 AA, smoke |

Submodule test suites (`Hexalith.Tenants/**`, `Hexalith.EventStore/**`) are run from their own roots and are out of scope for this guide.

---

## Quick Start

### .NET (unit + bUnit + FsCheck)

```bash
# From repo root
dotnet test
# or a single project
dotnet test tests/Hexalith.FrontComposer.Shell.Tests
# with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Playwright E2E

```bash
# One-time setup (from repo root)
npm --prefix tests/e2e install
npm --prefix tests/e2e run install:browsers

# Start the Counter sample (in another terminal)
dotnet run --project samples/Counter/Counter.AppHost

# Copy env template and point BASE_URL at the URL Aspire printed
cp tests/e2e/.env.example tests/e2e/.env.local
# edit tests/e2e/.env.local → BASE_URL=https://localhost:xxxx

# Run the E2E suite
npm --prefix tests/e2e test                 # all browsers, headless
npm --prefix tests/e2e run test:headed      # watch the browser
npm --prefix tests/e2e run test:ui          # time-travel UI
npm --prefix tests/e2e run test:chromium    # single browser
npm --prefix tests/e2e run test:smoke       # smoke specs only
npm --prefix tests/e2e run test:lifecycle   # lifecycle specs only
npm --prefix tests/e2e run report           # open last HTML report
```

---

## E2E Architecture (`tests/e2e/`)

```
tests/e2e/
├── fixtures/                 Playwright fixture composition root
│   ├── index.ts              mergeTests(tenantTest, lifecycleTest)
│   ├── tenant.fixture.ts     TenantContext (tenantId + userId)
│   └── lifecycle.fixture.ts  Five-state command lifecycle assertions
├── factories/                Faker-powered data builders
│   └── counter.factory.ts    buildIncrementCommand + batch helper
├── helpers/                  Test-time utilities
│   ├── api-client.ts         APIRequestContext factory with tenant/user headers
│   ├── auth.ts               seedDemoSession (localStorage + cookie)
│   └── a11y.ts               expectNoAxeViolations (WCAG 2.1 AA)
├── page-objects/             Page Object Models
│   └── counter.page.ts       CounterPage
├── specs/                    Test cases
│   ├── smoke.spec.ts         Render + axe-core zero-violations
│   └── lifecycle.spec.ts     idle → submitting → success transition
├── playwright.config.ts      Timeouts, projects, reporters, artifacts
├── tsconfig.json             Strict TS, ESNext, path aliases
├── package.json              Isolated dependency graph
├── .env.example              Template; copy to .env.local for overrides
└── .nvmrc                    Node 24 LTS pin
```

### Selector Strategy

- **Primary**: `data-testid` attributes (`testIdAttribute` set in `playwright.config.ts`)
- **Lifecycle wrapper**: `data-testid="fc-lifecycle-{commandId}"` + `data-lifecycle-state` on the `FcLifecycleWrapper`
- **Counter controls**: `fc-counter-value`, `fc-counter-increment`, `fc-counter-decrement`
- **Never**: CSS class selectors, raw text (except `getByRole` / `getByLabel` for a11y)

### Fixture Composition

Every spec imports from `fixtures/index.ts`, which calls `mergeTests(tenantTest, lifecycleTest)`. Add new fixtures by creating a file in `fixtures/` and merging it into the root export.

### Data Factories

Factories live in `factories/` and follow the `build{Aggregate}` pattern. Each accepts a `Partial<T>` overrides object so specs declare only what they care about. Randomness comes from `@faker-js/faker`.

### Accessibility

`helpers/a11y.ts` wraps `@axe-core/playwright`. Default tag set: `wcag2a, wcag2aa, wcag21a, wcag21aa`. Violations are asserted with `expect.soft` so one failure does not mask others in the same run.

### Artifacts

- `playwright-report/` — HTML report (git-ignored)
- `test-results/` — per-test artifacts (traces, screenshots, videos) + `junit.xml` for CI
- Video / screenshot / trace retention: on failure and retries only (see `playwright.config.ts`)

---

## Best Practices

1. **Own your state.** Seed data via factories + `auth.ts`; never rely on previous-test leftovers.
2. **Wait for state, not for time.** Use `expect.toHaveAttribute` / `toBeVisible` — never `page.waitForTimeout` in committed specs.
3. **One assertion per behavior.** Use `test.step` for Given/When/Then grouping inside a spec, not a spec per assertion.
4. **data-testid is a contract.** If the generator does not emit the id a spec depends on, the spec must fail — that signals a generator instrumentation gap.
5. **Soft-assert accessibility.** `expectNoAxeViolations` uses `expect.soft` so a11y regressions surface the full violation set.
6. **Tag slow specs.** Prefix `test.describe` names with `@slow` and run them with `--grep` filters in CI when needed.

---

## CI Integration

- **JUnit** report at `tests/e2e/test-results/junit.xml` — ingest into any CI dashboard.
- **HTML** report at `tests/e2e/playwright-report/` — upload as an artifact.
- `CI=true` triggers:
  - `forbidOnly` (fails the run if any `test.only` slips through)
  - 2 retries per failing test
  - 50% worker cap (tuneable via Playwright flags)
- Architecture mandates smoke E2E in v0.1 and full lifecycle assertions in v1 (see `_bmad-output/planning-artifacts/architecture.md` rows 2 and 5).

---

## Progress & Planning Artifacts

- Setup progress log: `_bmad-output/test-artifacts/framework-setup-progress.md`
- Story 2-4 ATDD checklist: `_bmad-output/test-artifacts/atdd-checklist-2-4.md`
- Architecture reference: `_bmad-output/planning-artifacts/architecture.md` (rows 1, 2, 5 inform E2E fixtures)

---

## Extending the Suite

- **New fixture** → `fixtures/<name>.fixture.ts` + merge into `fixtures/index.ts`
- **New factory** → `factories/<aggregate>.factory.ts`, exporting `build{Aggregate}` and `build{Aggregate}s`
- **New page** → `page-objects/<name>.page.ts` exposing verbs, not DOM chains
- **New spec** → `specs/<feature>.spec.ts`, import from `../fixtures/index.js`

When you need patterns that are not yet wired (network interception, recurse retries, burn-in for flake hunting, pact consumer contracts), the knowledge base under `.claude/skills/bmad-testarch-framework/resources/knowledge/` is the source of truth — browse it before inventing.
