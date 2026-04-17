# Epic 10: Framework Quality & Adopter Confidence

Framework provides test host/utilities for adopters, automated CI gates (accessibility checks, visual specimens, Pact contracts, mutation testing, property-based idempotency, flaky quarantine), LLM code-generation benchmark, and signed releases with SBOM. Built incrementally alongside earlier epics -- quality gates are woven into each phase, not deferred to the end.

### Story 10.1: Adopter Test Host & Component Testing Utilities

As a developer,
I want a test host and utilities that let me write component tests for my customization overrides and auto-generated views,
So that I can verify my customizations work correctly without manually running the application.

**Acceptance Criteria:**

**Given** the framework's test host package
**When** an adopter references it in a test project
**Then** FrontComposerTestBase (optional base class) is available
**And** it pre-configures: Fluxor store with all framework features, InMemoryStorageService, fake IOverrideRegistry, and mock ICommandDispatcher/IQueryExecutor

**Given** an adopter writes a bUnit test for a customization override
**When** the test renders the custom component
**Then** the framework's lifecycle wrapper, density context, theme context, and render context are available
**And** the component renders within the framework's expected environment (Fluxor state, storage, DI)

**Given** an adopter writes a bUnit test for an auto-generated DataGrid view
**When** the test provides mock projection data
**Then** the generated DataGrid renders with correct columns, formatting, badges, and empty states
**And** the test can assert on: column count, column headers, cell values, badge states, and accessibility attributes

**Given** the test utilities
**When** test data is created
**Then** the Builder pattern is available for domain model construction
**And** test naming follows the project convention: {Method}_{Scenario}_{Expected} or Should_{Behavior}_When_{Condition}

**Given** unit test coverage targets
**When** coverage is measured on core framework code (generator core, command pipeline, SignalR reconnection logic)
**Then** line coverage >= 80% (NFR51)
**And** component test coverage on auto-generated Razor components >= 15% line coverage (NFR52)
**And** integration tests: minimum 3 tests per API boundary (NFR53)

**References:** FR71, NFR51-53

---

### Story 10.2: Accessibility CI Gates & Visual Specimen Verification

As a developer,
I want automated accessibility checks and visual specimen verification that block merge on violations,
So that every release maintains WCAG 2.1 AA conformance and visual consistency across themes and densities.

**Acceptance Criteria:**

**Given** a pull request with UI changes
**When** the accessibility CI gate runs
**Then** axe-core runs via Playwright on the type specimen and data formatting specimen views
**And** "serious" or "critical" WCAG violations block merge (NFR37)
**And** contrast verification runs via axe-core
**And** keyboard navigation tests run via Playwright scripted tab-order tests
**And** focus visibility is verified via Playwright screenshot diff

**Given** the type specimen verification view
**When** it renders in CI
**Then** it displays: every type ramp slot, every semantic color token, both Light and Dark themes, all three density levels
**And** it contains: one DataGrid with column headers and six badge states, one flat command form with five-state lifecycle wrapper, one expanded detail view, one multi-level nav group

**Given** the data formatting specimen view
**When** it renders in CI
**Then** a single DataGrid with one row per data type exercises all formatting rules: locale-formatted numbers, absolute and relative timestamps, truncated IDs, null em dashes, collection counts, currency, boolean Yes/No, truncated enums

**Given** specimen screenshots
**When** they are compared against committed baselines
**Then** v1 compares per theme x density (6 specimens: 2 themes x 3 densities). RTL and zoom-level specimens deferred to v1.x
**And** baseline updates require a rationale paragraph and before/after screenshots

**Given** additional accessibility CI checks
**When** the full suite runs
**Then** forced-colors mode emulation is tested
**And** reduced-motion emulation is tested
**And** zoom/reflow at 100%/200%/400% is tested
**And** density parity testing renders specimens 3x (one per density level)

**Given** manual screen reader verification
**When** a release branch is cut
**Then** manual verification with NVDA+Firefox, JAWS+Chrome, VoiceOver+Safari is performed
**And** verification logs are committed to docs/accessibility-verification/

**References:** FR76, FR77, UX-DR32, UX-DR33, UX-DR34, NFR37-38

---

### Story 10.3: Consumer-Driven Contract Tests (Pact)

As a developer,
I want contract tests that verify generated UI components consume EventStore API contracts correctly,
So that API changes never silently break the generated UI and I catch contract drift before deployment.

**Acceptance Criteria:**

**Given** the Pact contract testing setup
**When** contracts are defined
**Then** they are file-based (not Pact Broker), checked into Shell.Tests/Pact/
**And** contracts cover the REST-to-generated-UI seam: command dispatch (POST /api/v1/commands), query execution (POST /api/v1/queries), and ETag handling

**Given** a Pact contract for command dispatch
**When** the consumer test runs
**Then** it verifies: the generated UI sends correctly shaped command payloads, includes ULID message IDs, includes TenantId in headers, and expects 202 Accepted responses

**Given** a Pact contract for query execution
**When** the consumer test runs
**Then** it verifies: the generated UI sends correctly shaped query parameters, sends If-None-Match headers with cached ETags, and correctly handles 200 with data and 304 Not Modified responses

**Given** the provider verification
**When** it runs per release
**Then** the EventStore API provider verifies all consumer contracts
**And** contract violations fail the build (never-cut gate, NFR55)

**Given** Pact implementation timing
**When** Epic 5 (Reliable Real-Time Experience) is being built
**Then** Pact contracts should be authored alongside the EventStore communication stories
**And** they serve as living documentation of the API contract

**References:** FR78, NFR55

---

### Story 10.4: Mutation Testing & Property-Based Testing

As a developer,
I want mutation testing on the source generator and property-based testing for command idempotency,
So that I have confidence the generator produces correct output and commands are replay-safe under all conditions.

**Acceptance Criteria:**

**Given** Stryker.NET mutation testing configuration
**When** mutations are applied to the source generator
**Then** targets are Parse and Transform stages only (not Emit)
**And** kill score >= 80% on the happy-path generation pipeline (NFR56)
**And** kill score >= 60% on error-handling paths
**And** mutations that survive are reviewed and either killed with new tests or documented as acceptable

**Given** Stryker.NET runs
**When** it executes in the nightly CI pipeline
**Then** it completes within the nightly budget (< 45 minutes total, NFR66)
**And** results are published as a CI artifact

**Given** mutation testing timing
**When** Epic 1 (source generator) is complete
**Then** Stryker targets should be configured and initial kill score baselined
**And** the kill score gate ratchets upward over time

**Given** FsCheck property-based testing for command idempotency
**When** test sequences are generated
**Then** replay(commands) == original_outcomes for randomly generated command sequences
**And** CI runs use 1000 sequences with deterministic seed (NFR58)
**And** nightly runs use 10000 sequences with random seed
**And** shrunk failure cases are converted to regression fixtures (deterministic unit tests)

**Given** property-based testing timing
**When** Epic 5 (command resilience) is complete
**Then** FsCheck tests should cover: command replay, duplicate detection, reconnection scenarios
**And** a bounded command vocabulary ensures meaningful test generation

**References:** FR79, FR81, NFR56, NFR58

---

### Story 10.5: Flaky Test Quarantine & CI Governance

As a developer,
I want flaky tests automatically detected and quarantined so they don't erode CI trust,
So that the main CI lane is always reliable and I can confidently treat a red build as a real problem.

**Acceptance Criteria:**

**Given** a test that intermittently fails
**When** the flaky detection system identifies it
**Then** the test is automatically tagged with xUnit Trait("Category", "Quarantined")
**And** the main CI lane excludes quarantined tests
**And** a separate quarantine CI lane runs them and warns on failure (does not block)

**Given** a quarantined test
**When** it passes 5 consecutive nightly runs
**Then** an automated PR is created to remove the Quarantined trait
**And** the test is reintroduced to the main CI lane
**And** if it fails again after reintroduction, it is re-quarantined with increased scrutiny

**Given** zero flaky tests in the main CI lane (NFR57)
**When** the main CI runs
**Then** all tests are deterministic and reliable
**And** any test failure in the main lane is treated as a real issue requiring investigation

**Given** CI pipeline time budgets
**When** pipeline durations are monitored
**Then** inner loop (unit + component) < 5 minutes (NFR64)
**And** full CI (excluding nightly) < 12 minutes (NFR65)
**And** nightly CI < 45 minutes (NFR66)
**And** if full CI exceeds 15 minutes for 3 consecutive days, a mandatory "CI diet" task is auto-created before new feature work (NFR67)

**Given** E2E test requirements
**When** E2E tests are configured
**Then** one Playwright suite per reference microservice covers: happy path, disconnect/reconnect, and rejection rollback (NFR54)

**References:** FR80, NFR54, NFR57, NFR64-67

---

### Story 10.6: LLM Benchmark, Signed Releases & SBOM

As a developer,
I want a nightly LLM code-generation benchmark that validates AI-assisted development quality, and signed releases with supply chain transparency,
So that I can trust the framework's AI development story and verify the provenance of every package I install.

**Acceptance Criteria:**

**Given** the nightly LLM benchmark
**When** it runs on the main branch
**Then** pinned model versions are used with temperature 0 and fixed seed
**And** the prompt corpus contains 20 prompts at v1 scope (50+ at v1.x)
**And** cached prompt-response pairs are stored for regression detection
**And** 4 out of 20 legitimate misses are allowed
**And** a published monthly budget cap limits LLM API costs

**Given** the benchmark ratchet rule
**When** results are evaluated
**Then** the v1 gate = initial baseline measured at week 8 + 5 percentage points grace
**And** the benchmark must not regress below the initial baseline
**And** the 28-day rolling ratchet rule and model transition rules are deferred to v1.x once sufficient data exists (NFR61-62 deferred)

**Given** a release is tagged
**When** the release pipeline runs
**Then** a CycloneDX SBOM is generated for the release (NFR25)
**And** NuGet packages are signed with an OSS-signing certificate (NFR24)
**And** symbol packages (.snupkg) are published for IDE debugging (NFR26)
**And** all packages use lockstep versioning (same version number)

**Given** CI resource monitoring
**When** GitHub Actions billable minutes per release tag exceed 90 minutes OR wall-clock from git tag to nuget.org exceeds 2 hours across 3 consecutive releases
**Then** the package count collapse trigger activates: consider collapsing 8 packages to 5 (NFR100)

**References:** FR73, FR75, NFR24-26, NFR60-63, NFR100

---

**Epic 10 Summary:**
- 6 stories covering all 9 FRs (FR71, FR73, FR75-81)
- Relevant NFRs woven into acceptance criteria (NFR24-26, NFR37-38, NFR51-67, NFR100)
- Relevant UX-DRs addressed (UX-DR32-34)
- Timing alignment noted: each story aligns with the epic where its test target is built
- Stories are sequentially completable: 10.1 (test host) -> 10.2 (accessibility CI) -> 10.3 (Pact) -> 10.4 (mutation/property) -> 10.5 (flaky quarantine) -> 10.6 (LLM benchmark/releases)
