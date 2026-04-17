# Non-Functional Requirements

*Consolidated from §Success Criteria, §Developer Tool Specific Requirements, §Innovation Risk Mitigation, and Party Mode rounds 1–3. Revised after Party Mode NFR validation (Winston, Murat, Amelia) + Advanced Elicitation failure-mode analysis. Each NFR is specific and measurable. Categories without relevance to this product are omitted.*

### Performance

**User-facing latency (web surface)**

| Metric | Target (v1) | Target (v1.x) | Measurement |
|---|---|---|---|
| Command click → confirmed state (**P95, cold actor**) | < 800 ms | < 500 ms | Playwright task timer, localhost Aspire. Cold = first command after app restart. |
| Command click → confirmed state (**P50, warm actor**) | < 400 ms | < 300 ms | Same. Warm = subsequent commands within same session. |
| First interactive render | < 300 ms | < 200 ms | Custom `Performance.mark('hfc-shell-interactive')` in `OnAfterRender(firstRender: true)` of root shell component, validated via Playwright `performance.getEntriesByName()`. NOT Lighthouse CI (which does not understand Blazor Server's two-phase render). |
| DataGrid render with 500 virtualized rows (P95) | < 300 ms | < 200 ms | bUnit render benchmark |
| Command palette search response | < 100 ms | < 50 ms | Synthetic keystroke-to-results timer |

*Click-to-confirmed qualifications: single-aggregate command, single projection update. Cross-aggregate or multi-stream commands are not bound by this target. The progressive visibility thresholds handle the UX for longer operations.*

**Agent-surface latency (chat/MCP)**

| Metric | Target (v1) | Target (v1.x) | Measurement |
|---|---|---|---|
| Agent command → projection read-your-writes (P95) | < 1500 ms | < 800 ms | MCP tool-call round-trip benchmark, localhost |
| MCP hallucination-rejection response time (P95) | < 100 ms | < 50 ms | Unit test timer on rejection path |

**Source generator performance (developer-facing)**

| Metric | Target (v1) | Measurement |
|---|---|---|
| Incremental rebuild per domain assembly | < 500 ms | CI gate on incremental timing via `IIncrementalGenerator` diagnostics |
| Full solution rebuild, 50-aggregate reference domain | < 4 s | CI benchmark; gate on incremental only, full-rebuild is advisory |
| Hot reload latency for domain attribute change | < 2 s | Manual verification against dev-loop SLO |

**Progressive visibility thresholds (lifecycle wrapper)**

| Lifecycle phase | Threshold | Behavior |
|---|---|---|
| Happy path | < 300 ms | No lifecycle indicator visible (invisible to user) |
| Brief delay | 300 ms – 2 s | Subtle sync pulse animation |
| Moderate delay | 2 s – 10 s | Explicit "Still syncing…" inline text |
| Extended delay | > 10 s | Action prompt with manual refresh option |
| Connection lost | Immediate on `HubConnectionState.Disconnected` | Warning-colored inline note; ETag polling fallback |
| Reconnection | On `HubConnectionState.Reconnected` | Batched animation sweep + 3 s auto-dismissing toast |

### Security & Data Handling

**Framework-layer data posture**

- Framework persists ONLY UI preference state (theme, density, nav, filters, sort) in client-side storage. Zero PII, zero business data at the framework layer.
- All business data lives in adopter microservices and Hexalith.EventStore. Framework never reads, writes, or caches it beyond ETag-validated query results.
- ETag cache entries contain projection snapshots scoped to `{tenantId}:{userId}` with bounded eviction. Cache is opportunistic; correctness comes from server queries.

**Authentication & authorization**

- OIDC/SAML integration with Keycloak, Microsoft Entra ID, GitHub, Google. No custom auth UI.
- JWT bearer tokens propagated through all command and query operations.
- Tenant isolation enforced at framework layer via `TenantId` from JWT claims.
- `[RequiresPolicy]` attributes integrate with ASP.NET Core authorization middleware. Missing policies produce build-time warnings.

**Supply chain integrity**

- Stable NuGet packages signed with OSS-signing certificate.
- CycloneDX SBOM generated per release.
- Symbols (`.snupkg`) published for IDE debugging.

**MCP security boundary**

- Typed-contract hallucination rejection: unknown tool names rejected with suggestion + tenant-scoped tool list. Command never reaches backend.
- Cross-tenant tool visibility is a security bug.

### Accessibility

**Baseline: WCAG 2.1 AA conformance on all auto-generated output.**

| Commitment | Enforcement |
|---|---|
| All generated forms have associated `<label>` elements | axe-core CI gate |
| All interactive elements keyboard-navigable | Manual screen-reader verification per release |
| Color contrast ≥ 4.5:1 (text), ≥ 3:1 (UI components) | axe-core CI gate |
| Focus management on navigation transitions | Playwright focus-tracking assertions |
| ARIA landmarks, roles, and live regions | axe-core CI gate + manual audit |
| Screen reader compatibility (NVDA, JAWS, VoiceOver) | Manual verification checklist, logged in release notes |
| Custom overrides checked for a11y contract compliance | Build-time Roslyn analyzer with WCAG citation + user scenario |

**CI enforcement:**

- axe-core via Playwright fails build on "serious" or "critical" violations.
- Visual specimen baseline: Light/Dark × Compact/Comfortable/Roomy. Unexplained drift fails merge. RTL deferred to v2.

### Reliability & Resilience

**SignalR connection lifecycle**

- Auto-reconnect with exponential backoff.
- Automatic group rejoin + ETag-gated catch-up query on reconnection.
- Batched reconciliation: N stale rows as one sweep, not N individual flashes.
- Auto-dismissing "Reconnected — data refreshed" toast (3 s).
- In-progress form state preserved across connection interruptions.

**Command reliability**

- Every submission produces exactly one user-visible outcome: success, rejection, or error notification.
- Idempotent handling via ULID message IDs with deterministic duplicate detection.
- Domain-specific rejection messages name conflicting entity and propose resolution.
- Zero silent failures across all surfaces.

**Serialized schema stability**

- All persisted event schemas and MCP tool schemas must be **bidirectionally compatible** within a major version:
  - **Backward-compatible reads:** new code (v1.3) must successfully deserialize events written by any prior minor version (v1.0, v1.1, v1.2).
  - **Forward-compatible serialization:** old code (v1.0) must tolerate unknown fields in events written by newer versions (v1.3). Unknown fields are ignored, not rejected.
- Schema evolution tests required: bidirectional deserialization matrix covering `v1.0 event × v1.N code` and `v1.N event × v1.0 code` for all shipped minor versions.
- Migration delta or breaking-change diagnostic emitted when schema hash diverges from prior deployed version.

### Testability & Quality Gates

**Test coverage floors (not ratios)**

*Murat round: ratios are descriptive, floors are enforceable. The pyramid split (70/20/8/2) is a design intent; the floors below are the CI gates.*

| Level | Coverage Floor | Tooling | Scope |
|---|---|---|---|
| Unit | ≥ 80% line coverage on core framework code (generator core, command pipeline, SignalR reconnection logic) | xUnit + Shouldly, measured by `dotnet-coverage` | Business logic, attribute parsing, contract validation |
| Component | ≥ 15% line coverage on auto-generated Razor components | bUnit, measured by `dotnet-coverage` | Generated form rendering, DataGrid binding, lifecycle states |
| Integration | Minimum 3 tests per API boundary | xUnit + SignalR fault injection | EventStore ↔ framework seam, reconnection behavior |
| E2E | One suite per reference microservice | Playwright | Happy path + disconnect/reconnect + rejection rollback |

*"Core framework code" defined narrowly: `Hexalith.FrontComposer.SourceTools`, `Hexalith.FrontComposer.Shell/Lifecycle`, `Hexalith.FrontComposer.EventStore/SignalR`. Scaffolding, DI wiring, and Razor template boilerplate are excluded from the 80% denominator.*

**Innovation-critical test types (Never-Cut)**

| Test Type | Purpose | Gate |
|---|---|---|
| Pact contract tests (REST ↔ generated UI) | Verify generated components consume EventStore API contracts correctly | Consumer-driven; provider verification per release |
| Stryker.NET mutation testing on source generator | Ensure mutations produce detectable failures | ≥ 80% kill score on happy-path generation pipeline; ≥ 60% on error-handling paths |
| Flaky-test quarantine lane | Automatic detection, isolation, separate CI lane, reintroduction gate | Zero flaky tests in main CI lane |
| FsCheck property-based testing (command idempotency) | Verify replay-safety across random command sequences | `replay(commands) == original_outcomes` for 1000 generated sequences |
| SignalR fault-injection test wrapper | Simulate drop, delay, partial delivery, reorder without live server | 90% of reconnection behaviors tested at unit level |

**LLM benchmark quality gate**

| Parameter | Value |
|---|---|
| Cadence | Nightly on `main`, NOT per-PR |
| Model versions | Pinned explicitly; upgrades are deliberate, scheduled events |
| Temperature | 0, fixed seed where available |
| Initial gate | Week-8 measured median + 5pp grace |
| **Ratchet rule** | Gate = max(initial gate, trailing **28-day** median minus 3pp). Monotonically non-decreasing — gate never drops. |
| **Model transition** | Ratchet pauses during model transitions. Gate reverts to pre-transition value until 7 days of new-model data stabilize. Fresh calibration then sets the new floor. |
| Prompt corpus | 20 prompts v1, 50+ prompts v1.x; cached prompt-response pairs |
| Legitimate misses | 4/20 may legitimately fail at v1 |
| Budget | Published monthly budget cap for LLM API costs |

### Build, CI & Release

**CI pipeline time targets**

| Tier | Target | Contents |
|---|---|---|
| Inner loop (unit + component) | < 5 min | xUnit + bUnit + Shouldly |
| Full CI (excluding nightly) | < 12 min | Inner loop + integration + Pact verification + Stryker incremental (changed files only) + axe-core + specimen verification |
| Nightly | < 45 min | Full CI + LLM benchmark + full Stryker |

**CI time enforcement:** automated CI step checks "did this run exceed threshold?" and creates a GitHub issue on breach. If full CI exceeds **15 minutes for 3 consecutive days**, a mandatory "CI diet" task is auto-created before new feature work. CI time is treated like flakiness: it compounds if ignored.

**Build enforcement**

- Trim warnings block CI (`IsTrimmable="true"` on all framework assemblies).
- `PublicApiAnalyzers` fail CI on accidental breaking changes within a minor version.
- Conventional commit-msg hook shipped with project template; CI lint validates.
- `CS1591` (missing XML doc comments): warning from day one; **error after the v1.0-rc1 PR** (API freeze milestone). All types in `PublicAPI.Shipped.txt` must have `<summary>` XML doc.

**Trim compatibility (front-loaded, week 2 evaluation)**

Per Amelia + Winston: trim budget is front-loaded, not back-loaded. Week-2 evaluation with pass/fail criteria:

| Dependency | Week-2 Evaluation | Pass/Fail Criterion | If Fail |
|---|---|---|---|
| FluentValidation | `dotnet publish -p:PublishTrimmed=true -p:TrimMode=full` against test project | ILC warnings = 0 | **Defer to v1.1.** Ship v1 with `<SuppressTrimAnalysisWarnings>true</SuppressTrimAnalysisWarnings>` scoped to FluentValidation. Document as known limitation. |
| DAPR SDK | Same test | ILC warnings = 0 | Budget 2 weeks for pin + regression. |
| Fluent UI Blazor v5 | Same test | ILC warnings = 0 | Budget 3 weeks for `[DynamicallyAccessedMembers]` wrapper. If upstream won't merge, maintain wrapper layer. |

Total trim budget: **3 weeks front-loaded** (weeks 2–4). FluentValidation deferred if it exceeds 1 week. No back-loaded trim surprises.

**Release automation**

- Semantic-release from conventional commits.
- NuGet prerelease suffix convention.
- CycloneDX SBOM per release. Stable releases signed.
- Symbols published.

### Deployment & Portability

| Topology | CI Validation | Status |
|---|---|---|
| Local development (Aspire) | Primary dev experience | CI target |
| On-premise (local Kubernetes) | CI target | Validated |
| Azure Container Apps | CI target | Validated |
| AWS ECS/EKS | Manual verification at v1 | Not CI-gated |
| GCP Cloud Run | Manual verification at v1 | Not CI-gated |
| Sovereign cloud (generic K8s) | Implied by on-premise target | Validated |

**Zero direct infrastructure coupling:** automated CI check — no direct references to Redis, Kafka, Postgres, CosmosDB, or DAPR SDK types from framework assemblies. All infrastructure through DAPR component bindings.

### Maintainability & Sustainability

**Versioning**

- Lockstep across all 8 packages for v1. Cross-package mismatch = build error.
- v2 escape hatch: compile-contract lockstep + satellite independence. Decision data-driven at month 18.
- Binary compatibility within minor versions enforced by `PublicApiAnalyzers`.

**Deprecation policy**

- One minor version minimum window.
- `[Obsolete]` convention: "`<old>` replaced by `<new>` in v`<target>`. See HFC`<id>`. Removed in v`<removal>`."
- Migration guide for any change breaking a shipped skill corpus example, regardless of semver bucket.

**Structured logging**

- OpenTelemetry semantic conventions. End-to-end tracing: click → backend → projection → SignalR → UI.
- Compatible with Grafana, Jaeger, Application Insights.

**Diagnostic ID scheme**

| Package | Range |
|---|---|
| Contracts | HFC0001–HFC0999 |
| SourceTools | HFC1000–HFC1999 |
| Shell | HFC2000–HFC2999 |
| EventStore | HFC3000–HFC3999 |
| Mcp | HFC4000–HFC4999 |
| Aspire | HFC5000–HFC5999 |

**Solo-maintainer sustainability filter** (PRD-wide): every quality gate, CI check, doc page, and test suite must survive the question *"Can Jerome sustain this at 2am after a release for 12 months?"*
