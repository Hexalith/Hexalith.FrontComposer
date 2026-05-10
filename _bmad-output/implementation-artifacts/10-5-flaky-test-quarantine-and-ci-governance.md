# Story 10.5: Flaky Test Quarantine & CI Governance

Status: ready-for-dev

> **Epic 10** - Framework Quality & Adopter Confidence. Covers **FR80**, **NFR54**, **NFR57**, and **NFR64-NFR67**. Builds on Stories **10-1** through **10-4** and the current CI gates. Applies lessons **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 10-5 keeps the main CI lane trustworthy while preserving evidence from unstable tests:

- Exclude quarantined xUnit tests from the blocking main lane using `[Trait("Category", "Quarantined")]` and `dotnet test --filter "Category!=Quarantined"`.
- Add a separate quarantine lane that runs quarantined tests, warns on failure, publishes bounded evidence, and never hides main-lane failures.
- Add automated flake detection and quarantine proposal tooling that opens a PR to add the quarantine trait, rather than silently mutating main.
- Add a nightly reintroduction gate: after 5 consecutive nightly passes, automation opens a PR to remove the quarantine trait and return the test to the main lane.
- Monitor CI duration budgets and create a mandatory CI-diet issue when full CI exceeds 15 minutes for 3 consecutive days.

---

## Story

As a developer,
I want flaky tests automatically detected and quarantined so they do not erode CI trust,
so that the main CI lane is always reliable and I can confidently treat a red build as a real problem.

### Adopter Job To Preserve

An adopter should see FrontComposer as a framework with disciplined quality gates: a red blocking build means real product risk, quarantined tests remain visible with owners and hypotheses, and CI duration pressure cannot be solved by deleting useful evidence.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Main lane | Update blocking `dotnet test` commands so default tests exclude `Category=Quarantined` while keeping Governance blocking. |
| Quarantine lane | Add a non-blocking quarantine lane that runs only `Category=Quarantined`, uploads TRX/summary evidence, and warns without failing the whole workflow. |
| Detection | Add deterministic automation that classifies a test as flaky only after mixed pass/fail evidence across repeated runs, then opens an issue and PR to add `[Trait("Category", "Quarantined")]`. |
| Reintroduction | Add nightly/manual automation that tracks 5 consecutive quarantine-lane passes and opens a PR to remove the trait. |
| Scrutiny | If a reintroduced test fails again, re-quarantine it with higher scrutiny: linked issue, failure window, previous quarantine history, and explicit owner. |
| CI budgets | Measure inner-loop, full-CI, and nightly durations. Create a `ci-diet` issue after full CI exceeds 15 minutes for 3 consecutive days. |
| E2E scope | Keep Playwright E2E governance to one suite per reference microservice covering happy path, disconnect/reconnect, and rejection rollback. Do not expand product behavior. |
| Evidence | Publish sanitized TRX/JSON/markdown summaries. No secrets, local machine paths, tenant/user IDs, command payloads, or unbounded logs. |
| Submodules | Use root-level submodules only. Do not use recursive nested submodule initialization. |
| Existing state | There is no `.github/workflows/nightly.yml` today. `ci.yml`, `release.yml`, and `ide-parity-revalidation.yml` exist; add or extend workflows deliberately. |

Start here: T1 current CI filters -> T2 quarantine lane -> T3 flake detection and PR tooling -> T4 reintroduction gate -> T5 duration governance -> T6 evidence/docs/tests.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- |
| AC1 | A test is known to be flaky | The quarantine proposal is applied | The test method or class is tagged with `[Trait("Category", "Quarantined")]` and includes a linked issue or metadata comment naming the reason and owner. |
| AC2 | The main CI lane runs | Tests execute | Blocking test commands exclude quarantined tests with `Category!=Quarantined` while still running normal unit, bUnit, SourceTools, Contracts, MCP, CLI, and Governance coverage. |
| AC3 | A quarantined test exists | CI runs | A separate quarantine lane runs `Category=Quarantined` tests and is warning-only; it must not make the whole CI workflow green if any non-quarantined blocking lane failed. |
| AC4 | The quarantine lane finds no quarantined tests | The job completes | The result is a clear zero-quarantined summary, not a silent success that looks like tests ran. |
| AC5 | Flaky detection evaluates a candidate | Mixed pass/fail evidence is present across repeated attempts or recent run history | Automation classifies the candidate as flaky only when the same test has at least one pass and one failure under the same code revision or approved evidence window. |
| AC6 | A candidate is classified as flaky | Automation acts | It opens or updates a GitHub issue with root-cause hypothesis, observed attempts, test identity, branch/commit, owner placeholder, and recommended quarantine patch. |
| AC7 | A quarantine patch is proposed | Automation has write permission | It opens a PR that adds the quarantine trait and links the issue; it does not push directly to `main`. |
| AC8 | The PR adds a quarantine trait | Review checks run | Governance tests verify every new `Category=Quarantined` trait has a linked issue/reference and an owner or explicit `owner-needed` marker. |
| AC9 | A quarantined test passes nightly | The reintroduction tracker updates | Consecutive pass count increments only for valid quarantine-lane runs on the protected branch and resets to zero on failure, cancellation, missing evidence, or malformed evidence. |
| AC10 | A quarantined test reaches 5 consecutive nightly passes | Reintroduction automation runs | A PR is opened to remove the quarantine trait and return the test to the main lane. |
| AC11 | A reintroduced test fails again | Detection evaluates it | It is re-quarantined with increased scrutiny: previous quarantine issue linked, recurrence count incremented, and the new issue/PR marks it as a repeat flake. |
| AC12 | Main CI fails | Quarantine lane passes or is skipped | The workflow remains failed; quarantine success cannot override blocking failure. |
| AC13 | Quarantine lane fails | Blocking lanes pass | The workflow conclusion remains suitable for merge according to warning-only semantics, but summaries and artifacts clearly show the quarantine failure. |
| AC14 | Artifacts are uploaded | The upload step runs | TRX/JSON/markdown evidence is bounded and sanitized. Raw dumps, bearer tokens, tenant/user IDs, command payload bodies, local absolute paths, and unbounded logs are rejected or redacted. |
| AC15 | `dotnet test` filters are used | xUnit traits are filtered | Filter syntax follows current .NET behavior for xUnit category traits, including `Category=Quarantined` and `Category!=Quarantined`. |
| AC16 | Existing category filters exist | New quarantine filters are composed | Governance, Performance, and `e2e-palette` filters keep their intended behavior; no test category is accidentally dropped from both main and quarantine lanes. |
| AC17 | CI duration is measured | A workflow completes | Inner-loop, full-CI, nightly duration, lane name, run id, and commit are recorded in a machine-readable summary. |
| AC18 | Full CI exceeds 15 minutes for 3 consecutive days | The monitor runs | Automation opens a mandatory `ci-diet` issue before new feature work, with duration history and suspected slow lanes. |
| AC19 | CI duration stays within budget | The monitor runs | No issue is opened; the summary still records current durations against targets: inner loop under 5 minutes, full CI under 12 minutes, nightly under 45 minutes. |
| AC20 | A quarantine or CI-diet issue already exists | Automation runs again | It updates the existing issue instead of opening duplicates. |
| AC21 | The repository is checked out in CI | A workflow needs submodules | Checkout uses root-level submodules only, for example `submodules: true`; it never uses `recursive` or `git submodule update --init --recursive`. |
| AC22 | Release CI runs | Tests execute before release | Release remains blocking for non-quarantined tests and cannot publish if main-lane tests fail. |
| AC23 | Playwright E2E governance is configured | E2E smoke/lifecycle suites run | One suite per reference microservice covers happy path, disconnect/reconnect, and rejection rollback without relying on fixed sleeps. |
| AC24 | A test is quarantined | Documentation is generated | The docs explain local commands to run main, quarantine, and reintroduction checks, and how to remove quarantine after root-cause fix. |
| AC25 | A developer tries to quarantine a test manually | Governance tests run | The change fails unless metadata, issue link, owner, and root-cause hypothesis are present. |
| AC26 | Quarantine automation creates a branch or PR | Labels are available | It applies `flaky-test`, `ci-governance`, and `codex-automation` labels when available; if labels are unavailable, the PR/issue body records the missing labels. |
| AC27 | Automated scripts parse TRX or workflow summaries | Input is malformed or missing | The scripts fail closed for quarantine/reintroduction decisions and publish a bounded diagnostic summary. |
| AC28 | Quarantine evidence references failed assertions | Failure text is copied into summaries | Evidence retains test name, outcome, attempt number, category, seed when present, and normalized relative path only; sensitive payloads are redacted. |
| AC29 | The implementation adds workflow permissions | GitHub token scope is reviewed | Workflows use the minimum needed permissions: `contents: read` for read-only jobs, and narrowly scoped `issues: write` / `pull-requests: write` / `contents: write` only for PR or issue automation. |
| AC30 | Story 10-4 mutation/property gates fail intermittently | Story 10-5 quarantine evaluates them | Quarantine may isolate flaky test cases but must not suppress invalid mutation/property evidence, missing reports, or threshold failures that Story 10-4 marks blocking. |

---

## Tasks / Subtasks

- [ ] T1. Harden current CI test filters (AC2, AC12, AC15, AC16, AC21, AC22)
  - [ ] Update `.github/workflows/ci.yml` Gate 3a so the default lane excludes `Category=Quarantined` in addition to existing `Performance` and `e2e-palette` exclusions.
  - [ ] Preserve Gate 2b Governance as blocking and ensure Governance tests are not accidentally excluded by quarantine filtering.
  - [ ] Update `.github/workflows/release.yml` so release tests exclude quarantined tests but still fail on every non-quarantined failure.
  - [ ] Keep checkout at root-level submodules only. Do not introduce `recursive`.
  - [ ] Add governance tests that parse workflow YAML/text and prove the blocking lanes include the quarantine exclusion and no recursive submodule checkout.

- [ ] T2. Add quarantine lane and evidence publishing (AC3, AC4, AC13, AC14, AC16, AC28, AC29)
  - [ ] Add a CI job or step that runs `dotnet test Hexalith.FrontComposer.sln --configuration Release --no-build --filter "Category=Quarantined"` with `continue-on-error: true`.
  - [ ] Emit a clear summary for zero quarantined tests, passed quarantined tests, and failed quarantined tests.
  - [ ] Upload quarantine TRX and bounded markdown/JSON summaries on `always()`.
  - [ ] Validate artifacts before upload or sanitize during generation.
  - [ ] Ensure quarantine lane result cannot rewrite or mask the status of blocking lanes.

- [ ] T3. Implement flake detection and quarantine proposal tooling (AC5-AC8, AC20, AC25-AC29)
  - [ ] Add script(s) under `jobs/` or `.github/scripts/` that parse TRX/workflow evidence and classify a candidate as flaky only from mixed pass/fail evidence.
  - [ ] Use stable test identity: fully qualified name, display name, project, optional trait set, and normalized source path if discoverable.
  - [ ] Open/update a GitHub issue with root-cause hypothesis, failure window, run links, owner marker, recurrence count, and proposed patch.
  - [ ] Open a PR that adds `[Trait("Category", "Quarantined")]` and metadata. Do not commit directly to `main`.
  - [ ] Add governance tests for manual and automated quarantine metadata.
  - [ ] Label with `flaky-test`, `ci-governance`, and `codex-automation` when labels are available.

- [ ] T4. Implement reintroduction gate (AC9-AC11, AC20, AC24, AC27-AC29)
  - [ ] Add or extend a scheduled/manual workflow for quarantine reintroduction. There is no general `nightly.yml` today, so create one only if that is the cleanest owner.
  - [ ] Persist reintroduction state in a reviewable file or issue comment, keyed by stable test identity and protected-branch evidence.
  - [ ] Count 5 consecutive valid nightly passes before opening a removal PR.
  - [ ] Reset pass count on failure, cancellation, missing evidence, malformed evidence, or changed test identity.
  - [ ] If a reintroduced test fails again, open/update a repeat-flake issue and quarantine PR with increased scrutiny.

- [ ] T5. Add CI duration budget governance (AC17-AC20, AC29)
  - [ ] Record inner-loop, full-CI, and nightly durations with run id, lane, commit, and timestamp.
  - [ ] Compare against budgets: inner loop under 5 minutes, full CI under 12 minutes, nightly under 45 minutes.
  - [ ] Open/update a mandatory `ci-diet` issue when full CI exceeds 15 minutes for 3 consecutive days.
  - [ ] Include suspected slow lanes and links to run summaries.
  - [ ] Keep advisory performance and quarantine lanes visible but separate from blocking failure accounting.

- [ ] T6. Preserve E2E and cross-story quality boundaries (AC23, AC30)
  - [ ] Ensure Playwright E2E governance references one suite per reference microservice for happy path, disconnect/reconnect, and rejection rollback.
  - [ ] Do not implement new product E2E scenarios beyond governance wiring unless already present in `tests/e2e`.
  - [ ] Do not suppress Story 10-4 invalid mutation/property reports or threshold failures through quarantine.
  - [ ] Keep Pact, accessibility, visual specimens, release signing, SBOM, and LLM benchmark ownership with their existing stories.

- [ ] T7. Document local and CI operations (AC24, AC27, AC28)
  - [ ] Update `tests/README.md` or a focused process note with commands for main lane, quarantine lane, reintroduction dry run, and CI-diet monitor.
  - [ ] Document how to remove quarantine after root-cause fix.
  - [ ] Document evidence retention and redaction rules.
  - [ ] Add troubleshooting guidance for malformed TRX, zero-test discovery, and missing GitHub labels.

- [ ] T8. Validate and hand off (AC1-AC30)
  - [ ] Run targeted governance tests.
  - [ ] Run local command(s) proving `Category!=Quarantined` and `Category=Quarantined` behavior.
  - [ ] Run scripts in dry-run mode against sample TRX fixtures for pass, fail, mixed, zero-test, malformed, and repeat-flake cases.
  - [ ] Record final workflow paths, script paths, command examples, labels, issue/PR behavior, and any deferred decisions in completion notes.

---

## Dev Notes

### Current Repository State

- `.github/workflows/ci.yml` exists and has `commitlint` plus `build-and-test` jobs. `build-and-test` is blocking, uses `actions/checkout` with `submodules: true`, and runs Gate 2b Governance plus Gate 3a default, Gate 3b `e2e-palette`, and Gate 3c Performance.
- `.github/workflows/release.yml` exists and currently runs all tests without a quarantine filter before semantic-release.
- `.github/workflows/nightly.yml` does not exist. The reintroduction gate may add it or use another clearly named scheduled workflow.
- `.github/workflows/ide-parity-revalidation.yml` is weekly IDE governance and should not absorb flaky quarantine responsibilities.
- `Directory.Packages.props` currently pins xUnit v3 packages (`xunit.v3` 3.2.2, runner 3.1.5), `Microsoft.NET.Test.Sdk` 18.3.0, `bunit` 2.7.2, `Verify.XunitV3`, and `FsCheck.Xunit.v3` 3.3.1.
- Existing category traits include `Governance`, `Performance`, and `e2e-palette`. No committed test currently uses `Category=Quarantined`.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` already parses workflow text for governance invariants. Extend this pattern for quarantine invariants before introducing a separate YAML parser unless the implementation needs structured parsing.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`. The user instruction forbids nested recursive submodule initialization unless explicitly requested.

### Critical Decisions

| Decision | Rule | Rationale |
| --- | --- | --- |
| D1 | Quarantine changes are proposed through PRs, not direct pushes to `main`. | Automatic source mutation on protected branches hides risk and bypasses review. |
| D2 | Reintroduction requires 5 consecutive valid nightly passes. | Epic 10.5 and the detailed architecture mechanics specify 5. An earlier architecture prerequisite mentions 10; treat that as stale drift unless a human changes the story. |
| D3 | Main CI excludes quarantined tests; quarantine CI runs only quarantined tests and is warning-only. | Preserves a deterministic blocking lane while keeping unstable tests visible. |
| D4 | Detection requires mixed pass/fail evidence under the same code revision or approved evidence window. | A single failure is a bug until proven flaky. A single pass after failure is not enough without stable identity and repeated evidence. |
| D5 | Quarantine metadata is required. | Every quarantined test needs issue link, owner marker, root-cause hypothesis, and reintroduction path. |
| D6 | CI-diet automation opens issues, not silent workflow rewrites. | Duration pressure is a product/process decision; automation should surface evidence and force prioritization. |
| D7 | Story 10-5 cannot weaken Story 10-4 quality gates. | Flaky isolation is not a license to ignore invalid mutation/property evidence or threshold failures. |

### Architecture and Package Boundaries

| Surface | Story 10-5 responsibility |
| --- | --- |
| `.github/workflows/ci.yml` | Main-lane quarantine exclusion, quarantine lane, summaries, artifacts, blocking/warning semantics. |
| `.github/workflows/release.yml` | Release test filter and blocking behavior for non-quarantined tests. |
| `.github/workflows/nightly.yml` or equivalent | Scheduled quarantine reintroduction and duration budget checks if a new nightly owner is introduced. |
| `jobs/` or `.github/scripts/` | Flake detection, TRX parsing, issue/PR proposal, reintroduction state, CI-diet monitor. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/` | Workflow/script governance tests, quarantine metadata checks, no-recursive-submodule guard. |
| `tests/README.md` or process notes | Local commands, quarantine lifecycle, evidence rules, troubleshooting. |

### Flaky Quarantine Contract

- Trait key/value is `Category=Quarantined`.
- Blocking main lane uses `Category!=Quarantined` and retains existing exclusions for `Performance` and `e2e-palette` where applicable.
- Quarantine lane uses `Category=Quarantined` and is warning-only.
- A quarantine PR must link an issue and identify the root-cause hypothesis. If ownership is unknown, use an explicit `owner-needed` marker rather than omitting ownership.
- Every quarantined test must have a reintroduction path. "Quarantine forever" is not an allowed final state.
- Reintroduction PRs remove the trait only after 5 consecutive valid nightly passes.
- A repeat flake after reintroduction must carry recurrence history; do not overwrite the original issue evidence.
- Missing or malformed evidence fails closed for automation decisions.

### CI Duration Governance Contract

- Budgets:
  - Inner loop: under 5 minutes.
  - Full CI excluding nightly: under 12 minutes.
  - Nightly: under 45 minutes.
  - Mandatory CI-diet issue trigger: full CI exceeds 15 minutes for 3 consecutive days.
- Duration summaries must include run id, commit, workflow, job/lane, start/end or duration, conclusion, and whether the lane is blocking or advisory.
- Existing advisory lanes remain visible. They must not pollute blocking-lane duration accounting unless the story explicitly documents why.
- Duplicate issue prevention is required. Repeated breaches update the existing `ci-diet` issue.

### Latest Technical Notes

- Microsoft Learn documents `dotnet test --filter <Expression>` with boolean composition and xUnit category/trait filtering. Use `Category=Quarantined` and `Category!=Quarantined` rather than inventing a custom trait parser for test selection.
- Microsoft Learn documents `--blame`, `--blame-crash`, and `--blame-hang` for isolating crashes/hangs. Use these only for bounded diagnostic evidence, not as routine artifact dumps.
- The current `actions/checkout` README distinguishes `submodules: true` from `submodules: recursive`. This repository must stay on root-level submodules only.
- GitHub issue/PR automation should use minimum token permissions and handle missing labels gracefully.

### Scope Guardrails

Do not implement these in Story 10-5:

- Pact consumer/provider contract changes. Owner: Story 10-3.
- Accessibility or visual specimen gates. Owner: Story 10-2.
- Mutation target, Stryker threshold, or FsCheck oracle changes. Owner: Story 10-4.
- LLM benchmark, release signing, SBOM, provenance, or package count collapse. Owner: Story 10-6.
- Broad package upgrades for xUnit, Microsoft.NET.Test.Sdk, bUnit, Playwright, GitHub Actions, or .NET SDK.
- Direct pushes to `main` from quarantine automation.
- Recursive nested submodule checkout.
- Retrying tests in the main lane until they pass. Retries are evidence for detection, not a way to hide failures.
- Uploading raw crash dumps, full console logs, local absolute paths, tokens, tenant/user IDs, command payloads, or unbounded generated artifacts.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 10-1 | Story 10-5 | Test host/utilities may reduce local reproduction friction, but CI governance owns quarantine decisions. |
| Story 10-2 | Story 10-5 | Accessibility and visual gates may produce test evidence; quarantine cannot suppress serious/critical accessibility violations as "flaky" without issue/owner/reintroduction path. |
| Story 10-3 | Story 10-5 | Pact failures remain contract evidence. Quarantine can isolate unstable test execution only, not invalid provider/consumer state. |
| Story 10-4 | Story 10-5 | Mutation/property runs can be quarantined only for execution instability; invalid reports, missing artifacts, or threshold failures remain blocking quality issues. |
| Story 10-6 | Story 10-5 | Release evidence rollup and LLM benchmark remain out of scope, but CI duration data should be consumable by release governance later. |
| Epic 9 diagnostics | Story 10-5 | Governance tests and documentation should reuse diagnostic/process conventions instead of creating an unrelated policy style. |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Dashboard-grade historical CI analytics beyond issue updates. | Future CI observability story |
| Long-term ownership rotation for quarantined tests. | Process follow-up after first quarantine cycle |
| Automatic source-code trait insertion for complex test declarations where method/class mapping is ambiguous. | Story 10-5 implementation decision; may require safe manual patch mode |
| Release evidence rollup across Pact, mutation, accessibility, quarantine, SBOM, and signing. | Story 10-6 |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-10-framework-quality-adopter-confidence.md#Story-10.5`] - story statement and acceptance criteria foundation.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR80`] - flaky-test quarantine capability.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#Innovation-critical-test-types-Never-Cut`] - zero flaky tests in main CI lane.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Test-Infrastructure-Conventions`] - flaky quarantine mechanics, CI budgets, and root-level test conventions.
- [Source: `_bmad-output/implementation-artifacts/10-4-mutation-testing-and-property-based-testing.md`] - Story 10-4 boundary and quality-gate handoff.
- [Source: `.github/workflows/ci.yml`] - current CI gates, category filters, artifacts, and root-level submodule checkout.
- [Source: `.github/workflows/release.yml`] - current release test gate.
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`] - existing workflow governance test pattern.
- [Source: `Directory.Packages.props`] - current xUnit v3, Microsoft.NET.Test.Sdk, bUnit, Verify, and FsCheck package pins.
- [Source: `tests/README.md`] - current test architecture and E2E guidance.
- [Source: `.gitmodules`] - root-level submodule list.
- [Source: Microsoft Learn `dotnet test` selective unit tests and VSTest options] - current filter and blame/hang/crash behavior.
- [Source: `actions/checkout` README] - current `submodules: true` versus `recursive` behavior.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-10: Story created via `/bmad-create-story 10-5-flaky-test-quarantine-and-ci-governance` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### File List

(to be filled in by dev agent)
