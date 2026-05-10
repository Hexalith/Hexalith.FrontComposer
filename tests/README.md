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

### CI Quarantine Governance

```bash
# Main blocking lane: excludes advisory and quarantined tests.
dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"

# Quarantine lane: runs only quarantined tests and writes TRX evidence.
dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category=Quarantined" --results-directory ./TestResults --logger "trx;LogFileName=test-results-quarantine.trx"

# Summarize quarantine evidence without publishing raw logs.
python .github/scripts/ci_governance.py summarize-quarantine --results-dir ./TestResults --markdown artifacts/quarantine/quarantine-summary.md --json artifacts/quarantine/quarantine-summary.json

# Validate that every manually quarantined test has issue, owner, reason, and reintroduction metadata.
python .github/scripts/ci_governance.py validate-quarantine-metadata --root .
```

A quarantined test must carry both the xUnit trait and a nearby metadata comment:

```csharp
// frontcomposer-quarantine: issue=https://github.com/Hexalith/Hexalith.FrontComposer/issues/123 owner=owner-needed reason=timer-race reintroduction=5-nightly-passes
[Trait("Category", "Quarantined")]
```

The quarantine trait is removed only after the same stable test identity records 5 consecutive valid nightly passes on protected-branch evidence. A valid pass uses the expected `Category=Quarantined` filter, complete bounded artifacts, no cancellation, no partial run, no dynamic skip, no rerun-only evidence, and unchanged identity. Failures, missing or malformed evidence, wrong filters, cancellations, partial runs, and identity drift reset the count to zero.

```bash
# Reintroduction dry run against a sample evidence file.
python .github/scripts/ci_governance.py reintroduction --evidence artifacts/quarantine/reintroduction-evidence.json --state tests/ci-governance/quarantine-reintroduction-state.json --output artifacts/quarantine/reintroduction-decision.json

# CI-diet duration monitor dry run.
python .github/scripts/ci_governance.py duration-monitor --evidence artifacts/ci-duration/full-ci-evidence.json --output artifacts/ci-duration/full-ci-summary.json --markdown artifacts/ci-duration/full-ci-summary.md
```

Duration governance records inner-loop, full-CI, and nightly lane evidence with run id, lane, commit, timestamp, conclusion, and blocking/advisory classification. Full-CI duration authority comes from protected-branch workflow evidence. Reruns, canceled runs, and partial evidence are excluded from the 3-day breach count or recorded as invalid evidence. The budgets are: inner loop under 5 minutes, full CI under 12 minutes, nightly under 45 minutes, and a mandatory `ci-diet` issue after full CI exceeds 15 minutes for 3 consecutive protected-branch days.

Quarantine evidence is published as bounded TRX, JSON, and markdown summaries. Summaries keep only allowlisted fields such as test identity, outcome, attempt number, category, seed when present, and normalized relative paths. Raw dumps, bearer tokens, workflow commands, HTML/script fragments, tenant/user identifiers, command payload bodies, local absolute paths, and unbounded logs are redacted, escaped, truncated, or rejected before publication.

Troubleshooting:

- **Malformed TRX**: treat the decision as invalid evidence, regenerate the run, and do not count it toward quarantine or reintroduction.
- **Zero quarantined tests**: the quarantine lane should emit a clear zero-test summary; this is not the same as silently skipping the lane.
- **Missing labels**: automation should still record the missing `flaky-test`, `ci-governance`, or `codex-automation` labels in the issue or PR body instead of opening duplicate issues.
- **Manual quarantine failure**: add the metadata comment with issue, owner or `owner-needed`, root-cause hypothesis, and `reintroduction=5-nightly-passes`.

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
- CI governance keeps Playwright E2E scope to one suite per reference microservice covering happy path, disconnect/reconnect, and rejection rollback. Do not add broader product E2E behavior from quarantine work alone.

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
