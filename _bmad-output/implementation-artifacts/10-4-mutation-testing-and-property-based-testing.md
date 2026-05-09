# Story 10.4: Mutation Testing & Property-Based Testing

Status: ready-for-dev

> **Epic 10** - Framework Quality & Adopter Confidence. Covers **FR79**, **FR81**, **NFR56**, and **NFR58**. Adds mutation testing for SourceTools Parse/Transform logic and property-based command idempotency checks without absorbing Pact, accessibility, flaky quarantine, release, or LLM benchmark scope. Applies lessons **L01**, **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 10-4 turns two high-risk quality assumptions into executable evidence:

- Configure Stryker.NET for `Hexalith.FrontComposer.SourceTools` Parse and Transform stages only, excluding emitters, generated code, migration fixtures, benchmarks, and unrelated Shell/runtime code.
- Baseline mutation scores for the happy-path generation pipeline and error-handling paths, then fail the nightly gate only when configured thresholds are violated or evidence is missing.
- Add FsCheck property-based tests for command idempotency and replay safety across generated commands, lifecycle transitions, reconnect/retry observations, and duplicate `MessageId` handling.
- Convert every shrunk FsCheck counterexample that fixes a real defect into a named deterministic regression fixture.
- Keep CI runtime bounded: PR lanes may run a narrow changed-file mutation smoke, while the full Stryker and high-count FsCheck suites belong to nightly.

---

## Story

As a developer,
I want mutation testing on the source generator and property-based testing for command idempotency,
so that I have confidence the generator produces correct output and commands are replay-safe under all conditions.

### Adopter Job To Preserve

An adopter should be able to trust that FrontComposer's generated command and projection code is protected by tests that kill meaningful generator mutations and that command replay/reconnect behavior does not duplicate user-visible outcomes, lose terminal states, or leak tenant/user context.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Mutation target | Configure `dotnet-stryker` against `src/Hexalith.FrontComposer.SourceTools` and the SourceTools test project. Mutate Parse and Transform stages only. Do not mutate Emitters, generated files, Shell runtime, MCP, Contract public APIs, or samples in this story. |
| Config home | Add version-controlled Stryker config near `tests/Hexalith.FrontComposer.SourceTools.Tests` or a repo-level `stryker-config.json/yaml` that clearly names solution, project, reporters, thresholds, mutate globs, and exclusions. |
| Thresholds | Enforce at least 80 percent kill score for happy-path generation pipeline and 60 percent for error-handling paths, with baseline evidence and ratchet notes. Avoid one global percentage that hides weak error coverage. |
| Reports | Publish bounded HTML and JSON mutation reports as CI artifacts. Report survived mutants by file, method, mutation kind, owning AC, and action: killed by new test, accepted with rationale, or deferred with owner. |
| Property target | Add FsCheck tests for command replay/idempotency around `LifecycleStateService`, generated command metadata, `CommandResult`, duplicate `MessageId` handling, reconnect/retry observations, and exactly-one visible outcome. |
| Test counts | CI property tests use deterministic seeds and 1000 sequences. Nightly uses 10000 sequences with random seed, but persists the seed and any shrunk counterexample. |
| Regression fixtures | Every accepted counterexample that finds a real bug becomes a deterministic xUnit test or JSON fixture under the owning test project. Do not leave it only as a random seed in a CI log. |
| CI lane | Add or extend nightly CI for full mutation/property runs. PR CI may run a short smoke or changed-file subset only if it cannot hide nightly failures. Keep flaky quarantine and CI diet automation for Story 10-5. |
| Package pins | Use central package management. Current repo already references `FsCheck.Xunit.v3`; verify whether to keep the current pin or update deliberately. Add `dotnet-stryker` tooling/config without broad package-train upgrades. |
| Submodules | Do not initialize nested submodules. This story does not need EventStore provider internals unless a property fixture explicitly consumes a root-level submodule contract already checked out. |

Start here: T1 Stryker config and target map -> T2 mutation baseline -> T3 property model/vocabulary -> T4 CI/nightly wiring -> T5 report/redaction/regression fixtures -> T6 documentation and handoff.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- |
| AC1 | Stryker.NET is configured | Mutation testing runs | Mutations are limited to `src/Hexalith.FrontComposer.SourceTools/Parsing/**` and `src/Hexalith.FrontComposer.SourceTools/Transforms/**`, plus any explicitly named pure helper needed by those stages. |
| AC2 | Mutation testing selects files | The mutate list is evaluated | Emitters, generated outputs, snapshots, benchmarks, migrations, Shell runtime, MCP runtime, Contract public API files, and test fixtures are excluded unless a future story explicitly expands scope. |
| AC3 | Happy-path generator mutations run | The SourceTools tests execute | The happy-path generation pipeline kill score is at least 80 percent, with report evidence naming killed and survived mutants. |
| AC4 | Error-handling mutations run | Diagnostics and rejection paths are mutated | Error-handling kill score is at least 60 percent, including parser diagnostics, unsupported-type behavior, duplicate/collision handling, and schema/drift validation where those paths are in Parse/Transform scope. |
| AC5 | A mutant survives | The report is reviewed | Each survivor is either killed with a new focused test, documented as equivalent/non-actionable with rationale, or deferred with an owner and follow-up story. |
| AC6 | Mutation testing runs in CI | The nightly lane completes | Full mutation testing completes within the nightly budget and publishes bounded HTML and JSON reports as artifacts. |
| AC7 | Mutation testing is too slow for PR CI | PR validation is designed | PR CI uses a smoke or changed-file subset only as an advisory/fast signal, while nightly remains the authoritative full mutation gate. |
| AC8 | Mutation reports are generated | Artifacts are uploaded | Reports do not include local user paths, environment dumps, secrets, bearer tokens, or full generated source beyond bounded snippets needed to review a mutant. |
| AC9 | FsCheck property-based tests are added | CI runs the property suite | Command idempotency properties execute 1000 generated sequences with deterministic seed and record that seed in test output or CI summary. |
| AC10 | Nightly property tests run | The high-count suite executes | Nightly runs 10000 generated sequences with random seed, records the seed, and fails on any counterexample. |
| AC11 | Command sequences are generated | The property model creates inputs | The vocabulary is bounded and meaningful: submit, acknowledge, syncing, confirmed, rejected, duplicate terminal, reconnect observation, retry observation, stale observation, and reset/idling events. |
| AC12 | Command replay is evaluated | A sequence is replayed | `replay(commands)` yields the same terminal command outcomes and user-visible notifications as the original sequence for the same seed and vocabulary. |
| AC13 | Duplicate `MessageId` cases are generated | Cross-correlation and same-correlation duplicates occur | The service preserves exactly-one user-visible outcome per correlation, marks idempotent confirmations correctly, and treats cross-correlation duplicate IDs according to the existing warning/fresh-submission contract. |
| AC14 | Reconnect/retry observations are generated | Lifecycle transitions arrive out of ideal order | Invalid transitions are dropped with diagnostic behavior, terminal states remain monotonic, and no sequence produces duplicate success/rejection/error notifications. |
| AC15 | Shrinking finds a minimal failing case | A property fails locally or in CI | The shrunk command sequence, seed, size, and failure reason are captured and converted into a deterministic regression fixture before the fix is marked complete. |
| AC16 | Property tests inspect tenant/user-sensitive values | Failures are reported | Failure messages redact tenant/user IDs, command payloads, tokens, and oversized values while retaining command type, state transition, seed, and step index. |
| AC17 | FsCheck configuration changes | Tests are reviewed | `MaxTest`, seed/replay, custom `Arbitrary` providers, shrinkers, and CI/nightly counts are explicit in code or configuration, not implicit defaults. |
| AC18 | SourceTools pure functions are mutated | Tests evaluate results | Parse/Transform behavior is asserted through existing public/internal seams rather than brittle full emitted-source string comparisons unless a snapshot is the existing oracle. |
| AC19 | Existing FsCheck tests are present | New property tests are added | They follow the current xUnit v3 + `FsCheck.Xunit.v3` pattern and avoid broad rewrites of unrelated property tests. |
| AC20 | Central package versions are touched | Restore graph changes | `Directory.Packages.props` changes are minimal, intentional, and documented; no Roslyn, Fluent UI, Fluxor, xUnit, bUnit, or .NET SDK upgrade is bundled into this story. |
| AC21 | Nightly or mutation workflow checks out submodules | CI runs | Checkout initializes root-level submodules only and never uses recursive nested submodule initialization. |
| AC22 | Quality evidence is summarized | The run completes | CI or local handoff lists Stryker version, FsCheck package version, config path, mutated file globs, thresholds, kill scores, property seeds, report artifact paths, and accepted/deferred survivors. |
| AC23 | Mutation or property checks are temporarily below target | Implementation cannot close the gap in this story | The story records the blocking survivor/counterexample, owner, follow-up story, and whether the gate is advisory or blocking; it does not silently mark the quality gate complete. |
| AC24 | Property tests require random values | Generators are built | Generators avoid unbounded strings, unbounded payloads, wall-clock dependence, real network calls, thread sleeps, static mutable global state, and non-deterministic tenant/user fallbacks. |
| AC25 | Mutation testing touches schema/drift Parse/Transform paths | Schema fixtures are used | Existing schema/drift fixtures are reused and extended only where they kill a meaningful mutant; fixture growth remains bounded and named by behavior. |

---

## Tasks / Subtasks

- [ ] T1. Add Stryker.NET tooling and configuration (AC1, AC2, AC6, AC7, AC20, AC21, AC22)
  - [ ] Add `dotnet-stryker` usage through a local tool manifest, CI tool install, or documented reproducible command; prefer a version-pinned approach that does not require global machine state.
  - [ ] Add a version-controlled `stryker-config.json` or `stryker-config.yaml` that names `Hexalith.FrontComposer.sln`, the SourceTools project, the SourceTools test project, reporters, coverage analysis, thresholds, concurrency, mutate globs, and exclusions.
  - [ ] Mutate only Parse and Transform source paths by default.
  - [ ] Exclude `src/Hexalith.FrontComposer.SourceTools/Emitters/**`, generated files, snapshots, benchmarks, test fixtures, and unrelated packages.
  - [ ] Use reporters that produce console progress plus HTML and JSON artifacts.
  - [ ] Document the local command and CI command in the story completion notes.
  - [ ] Ensure all CI checkout commands use only root-level submodules.

- [ ] T2. Establish mutation baselines and survivor triage (AC3-AC8, AC18, AC22, AC23, AC25)
  - [ ] Run the baseline mutation suite for SourceTools Parse/Transform.
  - [ ] Split or label evidence so happy-path generation and error-handling paths can be evaluated against their separate thresholds.
  - [ ] Add focused tests for meaningful survived mutants in `Parsing`, `Transforms`, diagnostics, schema fingerprinting, drift classification, unsupported-column handling, command density, and registration/manifest model transforms where relevant.
  - [ ] Document equivalent mutants with rationale rather than adding brittle tests.
  - [ ] Keep new tests targeted to behavior. Do not approve huge snapshots solely to kill a trivial mutant.
  - [ ] Add report validation that fails if the mutation report is missing, empty, malformed, or shows zero mutants in the expected target set.
  - [ ] Redact or bound mutation artifacts before upload if report content includes machine-local paths or large generated-source bodies.

- [ ] T3. Add command idempotency property model and generators (AC9-AC17, AC19, AC24)
  - [ ] Add property tests under `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/` or a focused `Infrastructure/Lifecycle/` folder, using xUnit v3 and `FsCheck.Xunit.v3`.
  - [ ] Define a bounded command sequence model with explicit operations for submit, acknowledge, syncing, confirmed, rejected, duplicate terminal, reconnect observation, retry observation, stale observation, and reset/idling events.
  - [ ] Generate bounded strings and IDs using synthetic command, tenant, user, correlation, and `MessageId` values.
  - [ ] Add custom `Arbitrary` providers and shrinkers that keep counterexamples readable and small.
  - [ ] Assert replay determinism: original and replayed sequences produce the same terminal outcomes, visible notification count, final lifecycle state, and idempotency flags.
  - [ ] Assert exactly-one user-visible outcome per correlation across duplicate terminal and reconnect/retry observations.
  - [ ] Assert cross-correlation duplicate `MessageId` behavior matches the current `LifecycleStateService` warning/fresh-submission contract.
  - [ ] Avoid wall-clock nondeterminism by using `FakeTimeProvider` or deterministic timestamps where time matters.

- [ ] T4. Persist counterexamples as regression fixtures (AC15, AC16, AC17, AC22, AC23)
  - [ ] Add a small fixture format for shrunk command sequences, or deterministic xUnit theories when a file format would be unnecessary ceremony.
  - [ ] Capture seed, size, sequence steps, expected terminal states, expected visible notification count, and the original property name.
  - [ ] Add at least one representative regression fixture path and test harness even if the first implementation finds no real defect, so future counterexamples have an obvious home.
  - [ ] Redact failure messages and serialized fixtures for tenant/user IDs, tokens, command payloads, local paths, and oversized values.
  - [ ] Document how to replay a failing seed locally.

- [ ] T5. Wire CI/nightly execution and artifacts without Story 10-5 governance (AC6-AC10, AC21-AC23)
  - [ ] Add or extend a nightly workflow for full Stryker plus 10000-sequence FsCheck execution.
  - [ ] Add a PR smoke only if it remains within the current full CI budget and is clearly not the authoritative mutation gate.
  - [ ] Upload Stryker HTML/JSON reports and property-test seed/counterexample summaries with stable artifact names.
  - [ ] Fail the nightly lane on missing reports, zero mutants, threshold breach, untriaged survivors, missing seeds, or uncaptured counterexamples.
  - [ ] Keep flaky quarantine automation, quarantine labels, five-pass reintroduction, and CI diet issue creation out of this story.
  - [ ] Keep the workflow root-submodule-only and avoid recursive submodule commands.

- [ ] T6. Add documentation and handoff evidence (AC5, AC8, AC15-AC17, AC22, AC23)
  - [ ] Document local mutation command, local property replay command, config paths, artifact paths, and threshold meaning.
  - [ ] Add a short "survived mutant triage" note explaining killed, equivalent, accepted, and deferred outcomes.
  - [ ] Add a "property counterexample handling" note explaining seed capture, shrink capture, regression-fixture conversion, and redaction.
  - [ ] Update completion notes with Stryker version, FsCheck version, mutation score, property counts, seeds, survivors, deferred work, and CI artifact names.
  - [ ] If the target thresholds are not met, record the exact blocking survivor/counterexample and whether the job remains advisory until the follow-up lands.

- [ ] T7. Final verification and handoff (AC1-AC25)
  - [ ] Run `dotnet restore Hexalith.FrontComposer.sln`.
  - [ ] Run SourceTools tests touched by mutation coverage.
  - [ ] Run Shell lifecycle/idempotency property tests.
  - [ ] Run the Stryker baseline command locally if runtime is practical; otherwise document the attempted command, reason it was not completed locally, and the CI/nightly lane that owns it.
  - [ ] Run the deterministic 1000-sequence property suite locally.
  - [ ] Verify mutation reports, property seed output, regression fixtures, and redaction/bounding checks.
  - [ ] Review git diff to ensure this story did not modify unrelated Epic 10 scopes or nested submodule state.

---

## Dev Notes

### Current Repository State

- `Directory.Packages.props` currently pins `FsCheck.Xunit.v3` 3.3.1 and the test projects already reference it.
- NuGet currently lists `FsCheck.Xunit.v3` 3.3.3 and `dotnet-stryker` 4.14.1. Treat any update from the existing FsCheck pin as an intentional package change, not incidental cleanup.
- Existing FsCheck examples include `CommandDensityTests` in SourceTools and `LifecycleThresholdTimerPropertyTests` in Shell. Follow their xUnit v3 style unless the implementation deliberately introduces a shared property-test helper.
- `LifecycleStateService` already owns the correlation-keyed lifecycle state index, duplicate `MessageId` cache, monotonic state transition rules, idempotent terminal handling, and exactly-one-outcome invariant. It is the primary property-based test target for command replay safety.
- SourceTools Parse/Transform targets include `Parsing/AttributeParser.cs`, `Parsing/CommandParser.cs`, `Parsing/FieldTypeMapper.cs`, `Transforms/*Transform.cs`, schema fingerprinting, drift comparison, and related pure model helpers.
- Existing SourceTools tests already cover parsing, transforms, diagnostics, drift, schema, incremental behavior, snapshots, and benchmarks. Mutation testing should expose weak assertions in those tests, not require a new parallel test suite.
- Current CI has `ci.yml` and no dedicated general nightly mutation workflow. Story 10-4 may add one, but Story 10-5 owns broader flaky quarantine and CI duration governance.
- The repository has root-level submodules. This story does not require recursive nested submodule initialization.

### Architecture and Package Boundaries

| Surface | Story 10-4 responsibility |
| --- | --- |
| `tests/Hexalith.FrontComposer.SourceTools.Tests` | Stryker config ownership, Parse/Transform mutation tests, report validation, and targeted survivor-killing tests. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing` | Mutation target for attribute, command, projection, field, diagnostic, and input-shape parsing behavior. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms` | Mutation target for IR-to-model pure transforms and schema/drift transform behavior. |
| `tests/Hexalith.FrontComposer.Shell.Tests` | FsCheck command idempotency/replay properties and deterministic regression fixtures. |
| `src/Hexalith.FrontComposer.Shell/Services/Lifecycle` | Primary runtime behavior under property test. Keep public semantics stable. |
| `.github/workflows` | Nightly mutation/property lane, optional PR smoke, report upload, and root-level submodule checkout. |
| Documentation/process notes | Local commands, triage rules, counterexample replay, and completion evidence. |

### Mutation Testing Contract

- Mutate Parse and Transform only for v1. Emission snapshots are expensive and brittle; emitter mutation coverage is a future expansion after Parse/Transform baselines stabilize.
- Use separate evidence for happy-path and error-handling thresholds. A single aggregate score can hide untested diagnostics and rejection paths.
- Equivalent mutants are allowed only with rationale. "Too hard to test" is not equivalent; it is a deferred test gap.
- Do not change product behavior just to kill a mutant. If Stryker exposes ambiguous intended behavior, record the decision and add a focused test after the decision is clear.
- Keep artifacts bounded. Mutation reports are useful evidence, but they should not become large generated-source archives.

### Property-Based Idempotency Contract

- The property model must compare observable outcomes, not private implementation details. Observable outputs include final lifecycle state, message ID, idempotency flag, notification count, diagnostics category when available, and subscriber transition stream.
- Generated command sequences must include valid and invalid transition orders. Invalid inputs are important because reconnect/retry paths can observe stale or duplicated lifecycle events.
- Same-correlation duplicate terminal transitions should not create a second visible outcome.
- Cross-correlation duplicate `MessageId` behavior must match the existing service contract: warn and treat as a fresh submission rather than merging unrelated correlations.
- Shrunk counterexamples become deterministic regression fixtures before closing the bug. Seeds alone are too easy to lose.
- Generators must stay bounded and synthetic. Avoid unbounded payloads, real tenant/user IDs, live EventStore, network, sleeps, random wall-clock time, and shared static state.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Stories 1-1 through 1-8 | Story 10-4 | SourceTools Parse/Transform IR and incremental-generator behavior must remain testable as pure or near-pure functions. |
| Stories 2-3 through 2-5 | Story 10-4 | Command lifecycle, `MessageId`, idempotent outcomes, rejection behavior, and exactly-one-visible-outcome rules define the property model. |
| Stories 4-1 through 4-6 | Story 10-4 | Projection/DataGrid Parse/Transform behavior, unsupported-field placeholders, formatting categories, and drift/schema fixtures are mutation targets. |
| Stories 5-1 through 5-7 | Story 10-4 | Reconnect, retry, stale observation, and EventStore communication timing shape property sequences; Pact contracts remain Story 10-3. |
| Story 10-1 | Story 10-4 | Testing helpers may reduce setup friction, but mutation/property gates must not depend on an adopter-facing package being complete unless it already exists. |
| Story 10-3 | Story 10-4 | Pact verifies REST contract shape; property tests verify replay/idempotency semantics. Do not duplicate Pact provider verification. |
| Story 10-5 | Story 10-4 | Quarantine automation and CI diet governance are out of scope. This story may produce nightly failures; Story 10-5 decides quarantine mechanics. |
| Story 10-6 | Story 10-4 | Release signing, SBOM, provenance, and LLM benchmark evidence are out of scope. |

### Latest Technical Notes

- Stryker.NET supports `stryker-config.json` and `stryker-config.yaml` with `solution`, `project`, `reporters`, `mutation-level`, `thresholds`, `coverage-analysis`, `concurrency`, `mutate`, `ignore-mutations`, and `ignore-methods` settings.
- Current Stryker.NET docs show HTML and JSON reporters as standard report outputs and threshold fields `high`, `low`, and `break`.
- Stryker.NET CI examples commonly install/run `dotnet-stryker` and pass a break threshold such as `--break-at 80`; this story should prefer version-controlled config for repeatable local and CI runs.
- FsCheck xUnit integration uses `[Property]` and supports explicit `MaxTest` and custom `Arbitrary` providers. Current docs also describe replay with `Config.Quick.WithReplay(...)`; the implementation should ensure failing seeds are reproducible in the chosen xUnit v3 integration path.
- FsCheck shrinking is part of the value of the suite. Preserve minimal counterexamples as deterministic fixtures rather than treating shrinking output as transient console text.

### Scope Guardrails

Do not implement these in Story 10-4:

- Pact consumer/provider contracts, Pact Broker/PactFlow, or provider verification. Owner: Story 10-3.
- Accessibility and visual specimen gates. Owner: Story 10-2.
- Flaky-test quarantine automation, quarantine PRs, or CI diet issue creation. Owner: Story 10-5.
- LLM benchmark, release signing, SBOM, or package provenance. Owner: Story 10-6.
- Mutation testing for Emitters, Shell components, MCP runtime, public Contracts APIs, generated code, or samples unless a future story expands the target.
- Broad package upgrades for Roslyn, Fluent UI, Fluxor, xUnit, bUnit, .NET SDK, or Playwright.
- Recursive or nested submodule initialization.
- Live EventStore, DAPR, Aspire, browser, or network dependencies for the default property suite.
- A new command lifecycle implementation created only for tests. Test the existing lifecycle contract.
- Unbounded random strings, payloads, or artifact output.

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Emitter mutation testing after Parse/Transform baselines are stable. | Future SourceTools quality story |
| Flaky quarantine for intermittently failing property or mutation tests. | Story 10-5 |
| CI diet automation if mutation/property lanes breach duration budgets repeatedly. | Story 10-5 |
| Pact REST provider contract failures discovered while modelling idempotency. | Story 10-3 follow-up or EventStore handoff |
| Release evidence rollup across mutation, Pact, accessibility, SBOM, and signing. | Story 10-6 |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-10-framework-quality-adopter-confidence.md#Story-10.4`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR79`] - mutation testing capability.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR81`] - command idempotency property-based testing capability.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#Innovation-critical-test-types-Never-Cut`] - Stryker and FsCheck quality gates.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Test-Infrastructure-Conventions`] - SourceTools Parse/Transform mutation scope, FsCheck counts, test naming, and fixture guidance.
- [Source: `_bmad-output/implementation-artifacts/10-1-adopter-test-host-and-component-testing-utilities.md`] - Testing package scope boundary and mutation/property deferral.
- [Source: `_bmad-output/implementation-artifacts/10-2-accessibility-ci-gates-and-visual-specimen-verification.md`] - accessibility/visual gate boundary.
- [Source: `_bmad-output/implementation-artifacts/10-3-consumer-driven-contract-tests-pact.md`] - Pact boundary and Story 10-4 deferral.
- [Source: `Directory.Packages.props`] - current FsCheck, xUnit v3, bUnit, and test package pins.
- [Source: `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandDensityTests.cs`] - existing FsCheck style in SourceTools.
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/LifecycleThresholdTimerPropertyTests.cs`] - existing Shell property-test style.
- [Source: `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs`] - command lifecycle and idempotency behavior under property test.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Parsing/`] - SourceTools Parse mutation target.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Transforms/`] - SourceTools Transform mutation target.
- [Source: Stryker.NET documentation via Context7 `/stryker-mutator/stryker-net`] - current config file and reporter concepts.
- [Source: FsCheck documentation via Context7 `/fscheck/fscheck`] - current property, MaxTest, custom Arbitrary, replay, and shrinking concepts.
- [Source: NuGet flat container `dotnet-stryker`] - latest observed package version 4.14.1 at story creation time.
- [Source: NuGet flat container `FsCheck.Xunit.v3`] - latest observed package version 3.3.3 at story creation time.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-09: Story created via `/bmad-create-story 10-4-mutation-testing-and-property-based-testing` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### File List

(to be filled in by dev agent)
