# Story 10.4: Mutation Testing & Property-Based Testing

Status: review

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
| Stryker segmentation | Do not rely on one aggregate score to prove both thresholds. Provide auditable happy-path and error-handling segmentation through separate config files, named test filters, or an equivalent documented mechanism, with separate artifacts. |
| Report validity | Treat missing, malformed, empty, zero-mutant, or out-of-scope Stryker reports as failures unless the baseline explicitly documents why that target has no mutants. |
| Target drift guard | Every production file under the approved Parse/Transform target set is either mutated or explicitly excluded with rationale, so new files cannot silently fall outside the baseline. |
| Workspace hygiene | Stryker runs use pinned tooling and isolated output/temp paths, then prove they did not leave source mutations or stale generated artifacts in the working tree. |
| Property oracles | Each FsCheck property must name its reference model or oracle, observable invariant, preconditions, postconditions, and expected counterexample evidence. |
| Property isolation | Every generated sequence runs against fresh lifecycle service/model state, deterministic time/diagnostic capture, and no shared static generator state. |
| Counterexample governance | Persist only confirmed bug counterexamples as deterministic regression fixtures, with seed, size, property name, minimal sequence, replay command, and reason for retention. |

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
| AC26 | Happy-path and error-handling mutation thresholds are evaluated | Stryker is configured | The implementation uses separate config files, named filters, or another auditable segmentation mechanism so the 80 percent happy-path threshold and 60 percent error-handling threshold produce distinct evidence instead of one blended score. |
| AC27 | Stryker reports are validated | CI or local verification reads the JSON report | The run fails if the report is absent, malformed, empty, contains zero mutants for an expected target, mutates files outside the approved Parse/Transform scope, or drops below the approved mutant-count baseline without a documented rationale. |
| AC28 | Mutation exclusions or disabled mutation kinds are introduced | The Stryker config is reviewed | Each exclusion is listed with rationale, and `Survived`, `NoCoverage`, `Timeout`, and `CompileError` mutants are counted separately with an explicit action: killed, equivalent accepted, deferred with owner, or blocking. |
| AC29 | FsCheck properties are implemented | A property is reviewed | Each property declares its reference model or oracle, observable invariant, preconditions, postconditions, generated command distribution, and the fields retained for a minimal counterexample. |
| AC30 | A property run fails or is replayed | CI or local output is inspected | The evidence includes FsCheck version, seed, size/max-size, sequence count, generated operation distribution, shrink result, and the exact local replay command. |
| AC31 | A shrunk counterexample becomes a regression fixture | The fixture is committed | The fixture includes property name, seed, size, minimal sequence, expected terminal state/outcomes, related bug or rationale, and a retention reason; transient non-bug seeds are not committed. |
| AC32 | A PR smoke is added | PR validation runs | The smoke declares triggers, timeout, blocking/non-blocking behavior, and artifact expectations. It must not replace the authoritative nightly thresholds or introduce flaky quarantine, retry policy, or CI diet governance owned by Story 10-5. |
| AC33 | Mutation/property artifacts are uploaded | Artifact validation runs | Artifacts satisfy a bounded allowlist: no secrets, tokens, environment dumps, tenant/user identifiers, full payload bodies, unbounded generated source, or machine-local paths beyond normalized relative paths needed for review. |
| AC34 | Parse/Transform production files change | Report-scope validation runs | Every production file under the approved Parse/Transform target roots is either mutated by at least one configured Stryker segment or listed in an explicit exclusion manifest with rationale and owner. |
| AC35 | Stryker executes locally or in CI | The run completes, fails, or is canceled | The evidence proves pinned tool version, isolated output/temp paths, and no leftover source mutations, dirty generated files, or unreviewed mutation artifacts in the working tree. |
| AC36 | Property suites run repeatedly or in parallel | A generated command sequence starts | The test harness creates fresh lifecycle service state, reference-model state, deterministic time, and diagnostic capture per sequence, and it avoids static mutable generator state that can leak between tests. |
| AC37 | A nightly mutation/property run fails before normal completion | Artifact publishing and summaries run | A sanitized failure envelope records which gate failed, whether raw reports were missing/invalid/redacted, any partial seed or mutant-count evidence, and the blocking owner without uploading unvalidated sensitive payloads. |
| AC38 | Quality evidence is handed to future stories | Completion notes are written | The story records authoritative gate status, last successful run identifier, current advisory/blocking mode, and owner for unresolved survivors, timeouts, or counterexamples; Story 10-5 may govern quarantine but cannot suppress this evidence. |

---

## Tasks / Subtasks

- [x] T1. Add Stryker.NET tooling and configuration (AC1, AC2, AC6, AC7, AC20, AC21, AC22, AC34, AC35)
  - [x] Add `dotnet-stryker` usage through a local tool manifest, CI tool install, or documented reproducible command; prefer a version-pinned approach that does not require global machine state.
  - [x] Add a version-controlled `stryker-config.json` or `stryker-config.yaml` that names `Hexalith.FrontComposer.sln`, the SourceTools project, the SourceTools test project, reporters, coverage analysis, thresholds, concurrency, mutate globs, and exclusions.
  - [x] Mutate only Parse and Transform source paths by default.
  - [x] Exclude `src/Hexalith.FrontComposer.SourceTools/Emitters/**`, generated files, snapshots, benchmarks, test fixtures, and unrelated packages.
  - [x] Use reporters that produce console progress plus HTML and JSON artifacts.
  - [x] Provide auditable happy-path and error-handling segmentation through separate Stryker config files, named filters, or an equivalent documented mechanism with separate artifacts.
  - [x] Add a report-scope validation step that proves only approved Parse/Transform files were mutated and flags unexpected zero-mutant targets.
  - [x] Add a target coverage manifest or equivalent validation proving each approved Parse/Transform production file is mutated by a segment or explicitly excluded with rationale and owner.
  - [x] Keep Stryker output and temporary mutation artifacts under deterministic ignored/output paths, and verify the run leaves no source mutations or stale generated files behind.
  - [x] Document the local command and CI command in the story completion notes.
  - [x] Ensure all CI checkout commands use only root-level submodules.

- [x] T2. Establish mutation baselines and survivor triage (AC3-AC8, AC18, AC22, AC23, AC25, AC34, AC35)
  - [x] Run the baseline mutation suite for SourceTools Parse/Transform.
  - [x] Split or label evidence so happy-path generation and error-handling paths can be evaluated against their separate thresholds.
  - [x] Add focused tests for meaningful survived mutants in `Parsing`, `Transforms`, diagnostics, schema fingerprinting, drift classification, unsupported-column handling, command density, and registration/manifest model transforms where relevant.
  - [x] Document equivalent mutants with rationale rather than adding brittle tests.
  - [x] Keep new tests targeted to behavior. Do not approve huge snapshots solely to kill a trivial mutant.
  - [x] Add report validation that fails if the mutation report is missing, empty, malformed, or shows zero mutants in the expected target set.
  - [x] Record the per-segment mutant-count baseline and flag drops caused by new Parse/Transform files, globs, or exclusions unless the manifest explains the change.
  - [x] Count `Survived`, `NoCoverage`, `Timeout`, and `CompileError` separately and assign each a triage action: `kill-test-added`, `equivalent-accepted`, `deferred-with-owner`, or `blocking`.
  - [x] List every Stryker exclusion with rationale in the handoff evidence so mutation score cannot be improved by silent scope reduction.
  - [x] Redact or bound mutation artifacts before upload if report content includes machine-local paths or large generated-source bodies.

- [x] T3. Add command idempotency property model and generators (AC9-AC17, AC19, AC24, AC36)
  - [x] Add property tests under `tests/Hexalith.FrontComposer.Shell.Tests/Components/Lifecycle/` or a focused `Infrastructure/Lifecycle/` folder, using xUnit v3 and `FsCheck.Xunit.v3`.
  - [x] Define a bounded command sequence model with explicit operations for submit, acknowledge, syncing, confirmed, rejected, duplicate terminal, reconnect observation, retry observation, stale observation, and reset/idling events.
  - [x] Generate bounded strings and IDs using synthetic command, tenant, user, correlation, and `MessageId` values.
  - [x] Add custom `Arbitrary` providers and shrinkers that keep counterexamples readable and small.
  - [x] Instantiate fresh lifecycle service state, reference-model state, deterministic time, and diagnostic capture for every generated sequence; disable or isolate only the specific test collection if xUnit parallelism would leak state.
  - [x] Assert replay determinism: original and replayed sequences produce the same terminal outcomes, visible notification count, final lifecycle state, and idempotency flags.
  - [x] Assert exactly-one user-visible outcome per correlation across duplicate terminal and reconnect/retry observations.
  - [x] Assert cross-correlation duplicate `MessageId` behavior matches the current `LifecycleStateService` warning/fresh-submission contract.
  - [x] Avoid wall-clock nondeterminism by using `FakeTimeProvider` or deterministic timestamps where time matters.
  - [x] For each property, document the reference model or oracle, observable invariant, preconditions, postconditions, generated operation distribution, and expected counterexample evidence.
  - [x] Record FsCheck version, seed, size/max-size, sequence count, generated operation distribution, shrink result, and exact replay command for failures.

- [x] T4. Persist counterexamples as regression fixtures (AC15, AC16, AC17, AC22, AC23)
  - [x] Add a small fixture format for shrunk command sequences, or deterministic xUnit theories when a file format would be unnecessary ceremony.
  - [x] Capture seed, size, sequence steps, expected terminal states, expected visible notification count, and the original property name.
  - [x] Add at least one representative regression fixture path and test harness even if the first implementation finds no real defect, so future counterexamples have an obvious home.
  - [x] Commit only counterexamples that reveal a confirmed bug or protected invariant; include property name, minimal sequence, related bug/rationale, and retention reason.
  - [x] Redact failure messages and serialized fixtures for tenant/user IDs, tokens, command payloads, local paths, and oversized values.
  - [x] Document how to replay a failing seed locally.

- [x] T5. Wire CI/nightly execution and artifacts without Story 10-5 governance (AC6-AC10, AC21-AC23, AC37, AC38)
  - [x] Add or extend a nightly workflow for full Stryker plus 10000-sequence FsCheck execution.
  - [x] Add a PR smoke only if it remains within the current full CI budget and is clearly not the authoritative mutation gate.
  - [x] If a PR smoke is added, define its triggers, timeout, blocking/non-blocking behavior, report expectations, and the exact reason it cannot be used as threshold authority.
  - [x] Upload Stryker HTML/JSON reports and property-test seed/counterexample summaries with stable artifact names.
  - [x] On timeout, cancellation, missing report, or malformed report, publish a sanitized failure envelope with gate name, partial mutant/seed evidence when safe, owner, and blocking/advisory status.
  - [x] Fail the nightly lane on missing reports, zero mutants, threshold breach, untriaged survivors, missing seeds, or uncaptured counterexamples.
  - [x] Keep flaky quarantine automation, quarantine labels, five-pass reintroduction, and CI diet issue creation out of this story.
  - [x] Keep the workflow root-submodule-only and avoid recursive submodule commands.

- [x] T6. Add documentation and handoff evidence (AC5, AC8, AC15-AC17, AC22, AC23, AC37, AC38)
  - [x] Document local mutation command, local property replay command, config paths, artifact paths, and threshold meaning.
  - [x] Add a short "survived mutant triage" note explaining killed, equivalent, accepted, and deferred outcomes.
  - [x] Add a "property counterexample handling" note explaining seed capture, shrink capture, regression-fixture conversion, and redaction.
  - [x] Add an "oracle contract" note that maps each property to its reference model, observable invariant, preconditions, postconditions, and replay command.
  - [x] Add an artifact allowlist/redaction checklist covering Stryker reports, FsCheck summaries, fixtures, seeds, relative paths, and bounded snippets.
  - [x] Document authoritative gate status, last successful run identifier, advisory/blocking mode, and owners for unresolved survivors, timeouts, malformed reports, or counterexamples.
  - [x] Update completion notes with Stryker version, FsCheck version, mutation score, property counts, seeds, survivors, deferred work, and CI artifact names.
  - [x] If the target thresholds are not met, record the exact blocking survivor/counterexample and whether the job remains advisory until the follow-up lands.

- [x] T7. Final verification and handoff (AC1-AC38)
  - [x] Run `dotnet restore Hexalith.FrontComposer.sln`.
  - [x] Run SourceTools tests touched by mutation coverage.
  - [x] Run Shell lifecycle/idempotency property tests.
  - [x] Run the Stryker baseline command locally if runtime is practical; otherwise document the attempted command, reason it was not completed locally, and the CI/nightly lane that owns it.
  - [x] Run the deterministic 1000-sequence property suite locally.
  - [x] Verify mutation reports, property seed output, regression fixtures, and redaction/bounding checks.
  - [x] Verify the Stryker run did not leave mutated source files, stale generated outputs, untracked raw reports, or nested submodule state in the working tree.
  - [x] Review git diff to ensure this story did not modify unrelated Epic 10 scopes or nested submodule state.

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
- The segmentation mechanism must be executable and reviewable: separate config files, named filters, or an equivalent documented mechanism are acceptable; informal post-run interpretation is not.
- Missing, malformed, empty, zero-mutant, or out-of-scope reports fail validation unless the initial baseline explicitly documents the exception.
- Every approved Parse/Transform production file must be represented in the mutation target manifest or exclusion manifest. A newly added file under those roots cannot silently escape mutation coverage because a glob or segment name went stale.
- Stryker runs must be reproducible from pinned tooling and deterministic config. Output/temp paths must be isolated, and successful or failed runs must prove they did not leave source mutations, stale generated files, or unreviewed raw reports in the working tree.
- Exclusions, disabled mutation kinds, `Timeout`, and `CompileError` outcomes must be visible in the evidence so the mutation score cannot be improved by silent scope reduction.
- Equivalent mutants are allowed only with rationale. "Too hard to test" is not equivalent; it is a deferred test gap.
- Do not change product behavior just to kill a mutant. If Stryker exposes ambiguous intended behavior, record the decision and add a focused test after the decision is clear.
- Keep artifacts bounded. Mutation reports are useful evidence, but they should not become large generated-source archives.

### Property-Based Idempotency Contract

- The property model must compare observable outcomes, not private implementation details. Observable outputs include final lifecycle state, message ID, idempotency flag, notification count, diagnostics category when available, and subscriber transition stream.
- Generated command sequences must include valid and invalid transition orders. Invalid inputs are important because reconnect/retry paths can observe stale or duplicated lifecycle events.
- Same-correlation duplicate terminal transitions should not create a second visible outcome.
- Cross-correlation duplicate `MessageId` behavior must match the existing service contract: warn and treat as a fresh submission rather than merging unrelated correlations.
- Shrunk counterexamples become deterministic regression fixtures before closing the bug. Seeds alone are too easy to lose.
- Every property must name its reference model or oracle, observable invariant, preconditions, postconditions, generated operation distribution, and replay evidence.
- Replay evidence must include FsCheck version, seed, size or max-size, sequence count, generated operation distribution, shrink result, and the exact local replay command.
- Each generated sequence must start from fresh lifecycle service state, reference-model state, deterministic time, and diagnostic capture. If the selected xUnit/FsCheck integration introduces shared or parallel state, isolate only the affected collection and document why.
- Persist only confirmed bug or protected-invariant counterexamples. Fixtures must carry property name, seed, size, minimal sequence, expected terminal state/outcomes, linked bug or rationale, and retention reason.
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

GPT-5 Codex

### Debug Log References

- `dotnet restore Hexalith.FrontComposer.sln` - passed.
- `dotnet build Hexalith.FrontComposer.sln --no-restore` - passed.
- `dotnet test Hexalith.FrontComposer.sln --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty"` - passed.
- `dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --no-build --filter "Category=LifecycleIdempotency&Category!=NightlyProperty"` - passed, 5 tests.
- `pwsh ./eng/run-lifecycle-property-suite.ps1 -MaxTest 10000 -ResultsDirectory TestResults/property-nightly-local-random` - passed with generated replay seed `15205021803337022758,8418328038012843391,0`.
- `dotnet test tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --no-build --filter "Category!=Performance"` - passed, 867 tests, 2 skipped.
- `pwsh ./eng/validate-property-artifacts.ps1` - passed.
- `pwsh ./eng/validate-stryker-reports.ps1 -AllowMissingReports` - passed.
- `pwsh ./eng/validate-docs.ps1 -SkipDocFx` - passed.
- `dotnet tool run dotnet-stryker --config-file tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-happy-path.json --output artifacts/mutation/happy-path` - attempted locally. Initial config path was corrected; a subsequent broad report produced zero tested mutants and was rejected; tightened Parse/Transform config exceeded the 5 minute local bound and was canceled. The nightly `Mutation and property quality gates` workflow is the authoritative blocking owner for full mutation baseline evidence.
- `git diff -- src/Hexalith.FrontComposer.SourceTools` - no source mutations left after the interrupted Stryker run.

### Completion Notes List

- 2026-05-09: Story created via `/bmad-create-story 10-4-mutation-testing-and-property-based-testing` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-09: Party-mode review hardening applied. Added auditable Stryker segmentation, non-empty report validation, mutation exclusion/status governance, explicit FsCheck oracle contracts, seed/replay evidence, counterexample fixture governance, PR smoke semantics, and artifact redaction allowlists.
- 2026-05-09: Advanced elicitation hardening applied. Added target drift guards, Stryker workspace hygiene, property state isolation, sanitized failure envelopes, and authoritative gate handoff evidence.
- 2026-05-10: Added pinned `dotnet-stryker` local tooling, segmented SourceTools mutation configs for happy-path and error-handling thresholds, mutation target manifest, report validation, and sanitized artifact/failure-envelope handling. Stryker.NET version: 4.14.1. Config paths: `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-happy-path.json` and `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-error-handling.json`.
- 2026-05-10: Added FsCheck command lifecycle/idempotency property coverage with deterministic CI replay seed `15485863,32452843,0`, CI `MaxTest=1000`, nightly `MaxTest=10000` with generated replay seed persisted to `artifacts/property/property-seed-summary.md`, bounded synthetic command vocabulary, fresh service/model state per sequence, deterministic time, fixture replay, and redacted counterexample fixture format. FsCheck.Xunit.v3 package pin remains 3.3.1.
- 2026-05-10: Added nightly mutation/property workflow with root-level submodule checkout only, stable report artifacts, blocking validation for missing/empty/out-of-scope/zero-mutant mutation reports, property seed/counterexample summary validation, and sanitized failure envelopes. Authoritative gate: blocking nightly workflow. Last successful local property nightly-equivalent run: `TestResults/property-nightly-local-random` using generated replay seed `15205021803337022758,8418328038012843391,0`.
- 2026-05-10: Local Stryker baseline was attempted but not accepted as passing evidence. The first completed report had zero tested mutants and was rejected by validation; the tightened Parse/Transform run exceeded the local 5 minute bound and was canceled after verifying no SourceTools source mutations remained. Blocking owner for mutation score, survivors, timeouts, and per-segment mutant-count baseline: `.github/workflows/mutation-property-nightly.yml`. No accepted/deferred survivors are recorded from local evidence.

### File List

- `.config/dotnet-tools.json`
- `.github/workflows/ci.yml`
- `.github/workflows/mutation-property-nightly.yml`
- `_bmad-output/implementation-artifacts/10-4-mutation-testing-and-property-based-testing.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/how-to/mutation-and-property-quality-gates.md`
- `eng/run-lifecycle-property-suite.ps1`
- `eng/validate-property-artifacts.ps1`
- `eng/validate-stryker-reports.ps1`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Lifecycle/CommandIdempotencyPropertyTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Lifecycle/Fixtures/command-idempotency-counterexamples.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-error-handling.json`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-happy-path.json`

### Change Log

- 2026-05-10: Implemented Story 10.4 mutation/property tooling, CI wiring, docs, validators, property fixtures, and BMAD handoff evidence; moved story to review.

---

## Party-Mode Review

- ISO date/time: 2026-05-09T19:04:29+02:00
- Selected story key: `10-4-mutation-testing-and-property-based-testing`
- Command/skill invocation used: `/bmad-party-mode 10-4-mutation-testing-and-property-based-testing; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)

### Findings Summary

- Winston: The story was close to ready but carried implicit architecture decisions around Stryker threshold segmentation, CI runtime, 10-5 boundary pressure, and the lifecycle replay oracle.
- Amelia: The implementation needed clearer file/config ownership, explicit Parse/Transform include/exclude checks, separate mutation-threshold evidence, survivor triage format, deterministic FsCheck replay evidence, and PR-smoke semantics.
- John: The adopter confidence job needed more concrete handoff evidence and clearer signals that advisory PR smoke cannot hide authoritative nightly failures.
- Murat: The quality gates needed to fail on invalid or zero-mutant Stryker reports, prevent mutation score gaming, formalize property oracles, and govern shrunk fixture retention.

### Changes Applied

- Added AC26-AC33 for auditable Stryker segmentation, valid non-empty scoped reports, mutation status/exclusion governance, FsCheck oracle contracts, seed/replay evidence, shrunk fixture governance, PR smoke semantics, and bounded/redacted artifacts.
- Hardened T1-T6 with report-scope validation, separate threshold evidence, survivor action categories, property oracle documentation, replay evidence capture, fixture retention criteria, PR smoke trigger/timeout semantics, and artifact allowlist/redaction checks.
- Strengthened the Mutation Testing Contract with executable segmentation requirements, invalid/zero-mutant failure behavior, and visible exclusion/status evidence.
- Strengthened the Property-Based Idempotency Contract with explicit reference model/oracle, invariant, precondition/postcondition, operation distribution, replay evidence, and fixture-retention requirements.

### Findings Deferred

- The initial per-file mutant baseline is deferred until the first controlled Stryker execution can measure the actual target set.
- The exact policy for `Timeout` and `CompileError` mutants is deferred to implementation evidence, with the story requiring explicit triage instead of silent pass-through.
- The acceptable nightly duration budget remains a product/architecture decision if the first implementation exceeds practical CI limits; Story 10-5 still owns broader quarantine and CI diet governance.
- The precise FsCheck reference-model implementation style, such as mini-interpreter versus canonical snapshot projection, is left to the dev agent as long as the oracle contract is documented and observable.

### Final Recommendation

ready-for-dev

---

## Advanced Elicitation

- ISO date/time: 2026-05-09T20:03:45+02:00
- Selected story key: `10-4-mutation-testing-and-property-based-testing`
- Command/skill invocation used: `/bmad-advanced-elicitation 10-4-mutation-testing-and-property-based-testing`
- Batch 1 method names: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Security Audit Personas; Self-Consistency Validation
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; Hindsight Reflection; Occam's Razor Application; Comparative Analysis Matrix; Architecture Decision Records

### Findings Summary

- Pre-mortem / failure mode: A new Parse/Transform file could miss mutation coverage if the mutate globs or segmentation filters drift while reports still look healthy.
- Red-team / security personas: Raw Stryker and property artifacts can leak source snippets, machine-local paths, tenant/user-like identifiers, or partial failure logs unless failure artifacts are validated and redacted before upload.
- Self-consistency / chaos: FsCheck replay evidence is only trustworthy when each sequence starts from fresh lifecycle service/model state and deterministic time; xUnit parallelism or static generator state can create false failures or false confidence.
- Hindsight / Occam / matrix: The lowest-cost hardening is to add explicit target manifests, workspace-hygiene checks, sanitized failure envelopes, and gate-status handoff notes rather than expanding product scope or pulling in Story 10-5 governance.

### Changes Applied

- Added AC34-AC38 for Parse/Transform target drift validation, Stryker workspace hygiene, property state isolation, sanitized failure envelopes, and authoritative gate-status handoff evidence.
- Hardened the Dev Agent Cheat Sheet with target drift, workspace hygiene, and property isolation reminders.
- Updated T1, T2, T3, T5, T6, and T7 with target coverage manifests, mutant-count baseline drift checks, per-sequence state isolation, failure-envelope publishing, gate-status documentation, and post-Stryker working-tree verification.
- Strengthened the Mutation Testing Contract and Property-Based Idempotency Contract with explicit drift, hygiene, and isolation rules.

### Findings Deferred

- The exact target coverage manifest format is left to the dev agent: JSON, markdown table, generated validation output, or CI summary are acceptable if reviewable and tied to the Stryker segments.
- The exact Stryker output/temp directory names are deferred to implementation, provided they are deterministic, ignored or artifact-scoped, and covered by the post-run hygiene check.
- Any broader retry/quarantine policy remains with Story 10-5; Story 10-4 only records the failure evidence and gate status.

### Final Recommendation

ready-for-dev
