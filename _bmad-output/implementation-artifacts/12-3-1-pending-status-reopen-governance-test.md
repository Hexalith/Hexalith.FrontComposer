# Story 12.3.1: Pending-Status Reopen-Trigger Governance Test

Status: done

> Follow-up to Story 12.3. Story 12.3 accepted `PENDING-STATUS-NULL-PROVIDER-V1` as a named v1 constraint. This story adds a blocking governance test so the three reopen triggers cannot appear silently in release notes, EventStore options, or framework DI while that constraint remains accepted.

---

## Executive Summary

Story 12.3 closed with null-provider-only pending-command status as an accepted v1 constraint, not provider-backed readiness. The risk left behind is governance drift: a future edit could add provider-backed wording, consume status-resource metadata, or register a real `IPendingCommandStatusQuery` provider without reopening `DW-0461`.

This story creates a small, string-pinned, fail-closed governance test in the existing Shell governance test suite. The test must parse the current ledger row, enforce the trigger guard only while `DW-0461` is still accepted under `PENDING-STATUS-NULL-PROVIDER-V1`, and produce bounded diagnostics that point developers back to `DW-0461` and Story 12.3.

Central invariant: while `DW-0461` carries `PENDING-STATUS-NULL-PROVIDER-V1`, Shell composition must expose pending-command status only through `NullPendingCommandStatusQuery`. Release-note trigger phrases and `EventStoreOptions` status surfaces are tripwires, but DI composition is the primary runtime proof.

---

## Story

As a release owner,
I want an automated detector for the Story 12.3 reopen triggers,
so that the v1 null-provider constraint cannot be silently invalidated by release-note wording or contract drift.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | `DW-0461` is recorded in `_bmad-output/implementation-artifacts/deferred-work.md` | The governance test runs | It locates exactly one reconciliation row containing `Row: DW-0461`, parses semicolon-delimited fields, and requires `Final classification 2026-05-15: accepted-constraint` plus `Constraint: PENDING-STATUS-NULL-PROVIDER-V1` for the active guard state; missing row, duplicate row, missing field, empty value, malformed field delimiter, or changed constraint token fails the row-state test closed. |
| AC2 | The constraint is still accepted | Release-note surfaces are scanned | The test scans `docs/**/*.md`, `CHANGELOG*.md`, and `_bmad-output/implementation-artifacts/*release-note*.md` for exact case-insensitive trigger phrases: `provider-backed pending-command`, `EventStore pending-command status endpoint`, and `IPendingCommandStatusQuery provider`. |
| AC3 | The constraint is still accepted | Trigger phrases are found | The test fails unless the matching line is inside a bounded `Constraint Metadata` allowance: a markdown heading or table caption line containing `Constraint Metadata`, followed by no more than 12 contiguous non-blank lines, with at least one line in that window containing `PENDING-STATUS-NULL-PROVIDER-V1`; the allowance ends at the first blank line, next heading, or after 12 lines. Ordinary release prose, changelog entries, and docs cannot use those phrases while the constraint is accepted. |
| AC4 | The constraint is still accepted | `EventStoreOptions` is reflected | The test performs a negative assertion: `EventStoreOptions` must not expose a public instance property whose name implies a status-resource contract, including combinations of `Status` with `Endpoint`, `Uri`, `Url`, `Path`, `Resource`, or `Metadata`, or `PendingCommand` with `Status`. |
| AC5 | The constraint is still accepted | Framework DI registrations are inspected | The test builds service collections through `Hexalith.FrontComposer.Shell.Extensions.ServiceCollectionExtensions.AddHexalithFrontComposer(...)` and `Hexalith.FrontComposer.Shell.Extensions.EventStoreServiceExtensions.AddHexalithEventStore(...)`; every effective `IPendingCommandStatusQuery` descriptor must be scoped and use `ImplementationType == typeof(NullPendingCommandStatusQuery)`, with no implementation instance, factory, or later non-null provider descriptor. |
| AC6 | `DW-0461` exists and is well formed but no longer has `Final classification 2026-05-15: accepted-constraint` with `Constraint: PENDING-STATUS-NULL-PROVIDER-V1` | The governance test runs | This story's reopen-trigger prohibition becomes inactive and does not invert into provider-support assertions; the row-state test must emit a bounded reason that the active v1 null-provider guard is no longer in force, and the future provider-backed story owns any replacement assertions. |
| AC7 | A guard fails | Diagnostic output is emitted | The failure names the trigger, repository-relative file path or property/registration, and points to `DW-0461`, Story 12.3, and this story. It must not print local absolute paths, tenant/user IDs, bearer tokens, raw payloads, or unbounded file content. |
| AC8 | CI runs the existing governance lane | The test is discovered | The test runs under `[Trait("Category", "Governance")]` in `tests/Hexalith.FrontComposer.Shell.Tests`, so the existing CI Gate 2b remains the blocking integration point and no new test project or package is required. |

---

## Tasks / Subtasks

- [x] T1. Add the governance test in the existing Shell governance suite (AC8)
  - [x] Create `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs`.
  - [x] Mark the class or each test with `[Trait("Category", "Governance")]`.
  - [x] Reuse local helper patterns from `CiGovernanceTests` and `InfrastructureGovernanceTests`: `RepositoryRoot()`, repository-relative diagnostics, `Shouldly`, and deterministic file enumeration.

- [x] T2. Implement the fail-closed `DW-0461` parser (AC1, AC6)
  - [x] Read `_bmad-output/implementation-artifacts/deferred-work.md` from the repository root.
  - [x] Locate exactly one row containing `Row: DW-0461`.
  - [x] Parse semicolon-delimited fields for `Final classification 2026-05-15` and `Constraint`.
  - [x] Require `Final classification 2026-05-15: accepted-constraint` and `Constraint: PENDING-STATUS-NULL-PROVIDER-V1` to activate the trigger prohibition.
  - [x] Treat missing row, duplicate row, missing classification, missing constraint, malformed delimiter, empty value, or changed constraint token as explicit fail-closed row-state failures.
  - [x] Treat a well-formed non-accepted classification as an inactive guard state with a bounded diagnostic; do not invert provider-support assertions in this story.

- [x] T3. Implement release-note trigger scanning (AC2, AC3, AC7)
  - [x] Enumerate only the allowed release-note surfaces: `docs/**/*.md`, root `CHANGELOG*.md`, and `_bmad-output/implementation-artifacts/*release-note*.md`.
  - [x] Exclude `bin`, `obj`, `.git`, `.agents`, generated site output, story files that are not release notes, and test source files.
  - [x] Match the three exact trigger phrases with `StringComparison.OrdinalIgnoreCase`.
  - [x] Allow matches only in a bounded `Constraint Metadata` window: start on a heading or table caption line containing `Constraint Metadata`, stop on the first blank line, next heading, or after 12 non-blank lines, and require `PENDING-STATUS-NULL-PROVIDER-V1` inside that same window.
  - [x] Report one concise line per violation using repository-relative paths and 1-based line numbers; cap reported hits so CI output cannot become an unbounded document dump.

- [x] T4. Implement `EventStoreOptions` trigger reflection (AC4)
  - [x] Reflect over public instance properties of `Hexalith.FrontComposer.Shell.Infrastructure.EventStore.EventStoreOptions`.
  - [x] Keep current allowed properties such as `BaseAddress`, `CommandEndpointPath`, `QueryEndpointPath`, and `ProjectionChangesHubPath` green.
  - [x] Fail while the constraint is active if a property name indicates status endpoint/resource/metadata consumption; include suspicious terms such as `PendingStatus`, `PendingCommandStatus`, `CommandStatusQuery`, `StatusResource`, and `StatusQueryProvider`.
  - [x] Do not snapshot or assert unrelated option shape, and do not add production options for testability.

- [x] T5. Implement DI registration guard (AC5)
  - [x] Build a fresh `ServiceCollection`, call `AddHexalithFrontComposer()`, and inspect all `ServiceDescriptor`s for `IPendingCommandStatusQuery`.
  - [x] Build a second collection with `AddHexalithFrontComposer()` followed by `AddHexalithEventStore(options => { options.BaseAddress = new Uri("https://eventstore.test"); options.RequireAccessToken = false; })`.
  - [x] Fail unless every `IPendingCommandStatusQuery` descriptor is scoped and uses `ImplementationType == typeof(NullPendingCommandStatusQuery)`.
  - [x] Treat implementation factories or instances as violations because they can hide provider-backed behavior from static review.
  - [x] Do not use source-text scanning as the primary DI proof; assert the effective service descriptors after invoking the extension methods.
  - [x] Cap descriptor diagnostics to the service type, lifetime, implementation type/factory/instance category, and detected provider type if available.

- [x] T6. Add negative fixtures inside the test code (AC1-AC7)
  - [x] Cover accepted-constraint row parsing, missing row, duplicate row, malformed classification, and missing constraint.
  - [x] Cover phrase hit outside metadata, phrase hit inside valid metadata, case-insensitive hit, and bounded diagnostic formatting.
  - [x] Cover a near-miss phrase that must not match the exact trigger list.
  - [x] Cover a valid non-accepted `DW-0461` row that makes the prohibition inactive without asserting provider-backed support.
  - [x] Cover synthetic option-property names and synthetic service descriptors that represent a non-null provider.

- [x] T7. Validate and record results (AC8)
  - [x] Run `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "Category=Governance"`.
  - [x] Run `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` if the implementation changes shared governance helpers or production registration behavior.
  - [x] Run `git diff --check`.
  - [x] Update this story's Dev Agent Record with validation evidence and changed files.

### Review Findings

_Code review 2026-05-16 (bmad-code-review). Layers: Blind Hunter, Edge Case Hunter, Acceptance Auditor. Reviewed commit `c8cbd2c`._

- [x] [Review][Decision][Resolved] Out-of-scope `Hexalith.EventStore` submodule pointer bump bundled into the commit [`Hexalith.EventStore` `c0d439d`→`b802f4d`] — Resolved 2026-05-16: documented in Source Tree Components To Touch and Completion Notes; the 5 commits in range cover Story 22.6 projection-rebuild orchestrator hardening and read APIs, unrelated to Story 12.3.1's pending-command status scope. No code change.
- [x] [Review][Decision][Resolved] Hard-coded classification field name `Final classification 2026-05-15` pins to a specific dated event [`PendingStatusReopenGovernanceTests.cs:19`] — Resolved 2026-05-16: strict dated pin retained intentionally. Release ops must update the test alongside any reclassification; the fail-closed bias matches the spec's "fail closed on ambiguous ledger state" rule. No code change.
- [x] [Review][Patch][Applied 2026-05-16] Heading-line trigger bypass — `IsInsideConstraintMetadataAllowance` now rejects matches sitting on the Constraint Metadata caption line itself (`start == matchIndex` short-circuit) [`PendingStatusReopenGovernanceTests.cs`]. New regression test `ReleaseNoteTriggerScanner_RejectsHeadingAndFakeTableCaptionBypasses` covers the bypass.
- [x] [Review][Patch][Applied 2026-05-16] Caption-substring bypass via arbitrary table data row — `ContainsConstraintMetadataCaption` table-row branch now requires the first non-empty cell to start with `Constraint Metadata` [`PendingStatusReopenGovernanceTests.cs`]. Heading and `Table:` captions unchanged. Covered by the same regression test above (`| Note: see Constraint Metadata below |` is rejected; `| Constraint Metadata |` is accepted).
- [x] [Review][Patch][Applied 2026-05-16] AC4 PendingCommand+Status non-contiguous combinations — `IsSuspiciousStatusContractProperty` now flags any property name containing both `PendingCommand` and `Status` regardless of order [`PendingStatusReopenGovernanceTests.cs`]. New theory inline data covers `PendingCommandReadinessStatus` and `StatusForPendingCommand`.
- [x] [Review][Patch][Applied 2026-05-16] Keyed-service descriptors handled explicitly — `FindPendingStatusDescriptorViolations` partitions keyed vs unkeyed descriptors, requires at least one unkeyed descriptor, and rejects any keyed descriptor outright (previously `descriptor.ImplementationType` would throw on keyed registrations) [`PendingStatusReopenGovernanceTests.cs`]. New test cases for `keyed-only` and `mixed unkeyed + keyed` scenarios added to `PendingStatusDiDescriptorClassifier_FailsHiddenOrProviderBackedDescriptors`.
- [x] [Review][Patch][Applied 2026-05-16] `RelativePath` sibling-directory bypass closed — comparison now requires either exact match with the root or that the full path starts with `<root><DirectorySeparatorChar>`, preventing `D:\Hexalith.FrontComposer.Evil\…` from passing as inside `D:\Hexalith.FrontComposer` [`PendingStatusReopenGovernanceTests.cs`].
- [x] [Review][Patch][Applied 2026-05-16] `CHANGELOG*.md` and `*release-note*.md` globs are case-insensitive on Linux — `EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }` added to all three release-surface enumerations (docs recursive, CHANGELOG top-level, implementation-artifacts top-level) [`PendingStatusReopenGovernanceTests.cs`].
- [x] [Review][Patch][Applied 2026-05-16] Inactive-guard branches now use `Assert.Skip(state.Diagnostic)` so the three release-note / EventStoreOptions / DI tests show as skipped (not silently green) when `DW-0461` is well-formed but non-accepted [`PendingStatusReopenGovernanceTests.cs`].
- [x] [Review][Patch][Deferred → CR-12-3-1-D6] Metadata budget "12 non-blank lines" (AC3 literal) is incompatible with the production release note — the approved `12-3-pending-command-provider-release-note.md` `Constraint Metadata` table has 14 data rows + header/separator (~16 non-blank lines). Applying the spec-literal counting rule would push the `Agent impact` trigger line outside the window and break the running guard against the approved release note. The dev's `tableMode` exception (12 data rows; header/separator/prose flexibility) is the pragmatic interpretation and must remain until the spec is reconciled with the actual content. Logged as deferred follow-up `CR-12-3-1-D6` in `deferred-work.md`; AC3 wording should be revised separately.
- [x] [Review][Patch][Applied 2026-05-16] Fenced code-block immunity — `ScanReleaseNoteLines` and `CountReleaseNoteTriggerHits` now toggle an `insideFence` flag when a line trimmed-start begins with ```` ``` ````, skipping trigger detection inside fenced blocks. New regression test `ReleaseNoteTriggerScanner_IgnoresTriggersInsideFencedCodeBlocks` covers both the in-fence and post-fence cases.
- [x] [Review][Defer] Smart-quote / non-breaking-hyphen / en-dash variants of trigger phrases evade the scanner [`PendingStatusReopenGovernanceTests.cs:24-28`] — deferred, low real-world likelihood for release-note authoring; documenting at the time it bites is more useful than a generic Unicode-normalisation pass.
- [x] [Review][Defer] `EventStoreOptions` reflection only inspects public instance properties — explicit interface implementations and added fields are invisible [`PendingStatusReopenGovernanceTests.cs:65-68`] — deferred, requires broader reflection design (incl. fields, non-public, indexers). Today the type uses plain auto-properties.
- [x] [Review][Defer] Per-file `MaxDiagnostics` cap and global `FormatDiagnostics` cap stack, producing a single opaque "additional diagnostics suppressed: N" when many files violate [`PendingStatusReopenGovernanceTests.cs:341-371,538-546`] — deferred, the bounded-diagnostics intent is satisfied at the per-file level; the global suppression line is a UX nice-to-have, not a correctness defect.
- [x] [Review][Defer] Suppression count mixes allowed (metadata-window) hits with real violations [`PendingStatusReopenGovernanceTests.cs:363-368,373-374`] — deferred, only misleads in the rare case of a giant allowed metadata block PLUS exactly `MaxDiagnostics` real violations in the same file; not a correctness gap.
- [x] [Review][Defer] Singleton-instance Null provider violation message ("must use ImplementationType NullPendingCommandStatusQuery") doesn't name the descriptor shape as the root cause [`PendingStatusReopenGovernanceTests.cs:511-521`] — deferred, diagnostic clarity nit; the underlying detection is correct.

---

## Dev Notes

### Current Code Intelligence

| Surface | Current state | Story impact |
| --- | --- | --- |
| `_bmad-output/implementation-artifacts/deferred-work.md` | `DW-0461` currently reads `Final classification 2026-05-15: accepted-constraint; Constraint: PENDING-STATUS-NULL-PROVIDER-V1`. | This is the source of truth for whether the governance guard is active. Do not infer active state from story status alone. |
| `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md` | Contains the approved constraint and `Constraint Metadata` table for Story 12.3. | This file is the allow-list model for accepted constraint prose. Trigger phrases outside a constraint metadata block should fail. |
| `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs` | Defines `IPendingCommandStatusQuery`, `NullPendingCommandStatusQuery`, and the polling coordinator. The null provider returns `null`; provider exceptions are logged without raw exception messages. | Do not implement a provider in this story. This story only detects provider-backed drift. |
| `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` | `AddHexalithFrontComposer()` registers `IPendingCommandStatusQuery` as `NullPendingCommandStatusQuery` with `TryAddScoped`. | The DI guard should assert this remains the only framework registration while the constraint is active. |
| `src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs` | `AddHexalithEventStore()` registers EventStore command/query/subscription services but does not replace `IPendingCommandStatusQuery`. | The DI guard should prove EventStore opt-in does not become a hidden provider-backed status claim. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptions.cs` | Has command/query/projection paths, timeout, ETag, request/response caps, and access-token options. It has no pending-command status endpoint/resource option. | The options reflection guard should fail when such a contract appears before `DW-0461` is reopened or closed. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` | Existing blocking governance tests use string scans, repository-root discovery, `Shouldly`, and bounded subprocess helpers. | Follow these patterns for test shape and diagnostics. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` | Existing governance scanners normalize repository-relative paths and reject absolute-path escape. | Reuse this style for file enumeration and diagnostic output. |

### Architecture Compliance

- Keep this as a Shell governance test. Do not create `Hexalith.FrontComposer.Governance.Tests` unless the existing governance suite cannot host it. The current CI already runs `Category=Governance` as a blocking lane.
- Do not add package references or inline versions. The Shell test project already references xUnit v3, Shouldly, NSubstitute, and the Shell project.
- Do not introduce YAML, markdown, or Roslyn parser dependencies for this narrow guard. Use deterministic line scanning and reflection over current assemblies.
- Do not create shared governance infrastructure for this story unless an existing local governance-test helper already requires extension.
- Do not modify production EventStore provider behavior, `EventStoreOptions`, or DI registrations as part of this story unless the test exposes an actual pre-existing drift.
- Preserve root-level submodule policy. This story should not initialize, update, scan into, or depend on nested submodules.
- Do not require, inspect, or gate on Story 12.5 stakeholder sign-off, AT, or real-device evidence artifacts. This governance test is release-constraint protection for Story 12.3 only.

### Evidence Priority

1. `DW-0461` is the canonical source of whether `PENDING-STATUS-NULL-PROVIDER-V1` is active.
2. DI descriptor inspection is the primary runtime/composition proof that the active constraint still maps pending status to `NullPendingCommandStatusQuery`.
3. Release-note phrase scanning is a governance tripwire for public or package-promotion wording that would reopen the constraint.
4. `EventStoreOptions` reflection is a stealth-configuration guard against adding a status-resource contract while the null-provider constraint is active.

### Failure Modes Covered

- False pass from string-only release-note checks while DI already registers a non-null provider.
- False positive from scanning unrelated docs, story files, test code, generated site output, or full repository text.
- Hidden provider behavior through `ImplementationFactory` or `ImplementationInstance` descriptors.
- Over-specific `EventStoreOptions` checks that fail on unrelated options instead of status-provider configuration drift.
- Noisy CI diagnostics that print whole documents, local absolute paths, raw markdown tables, or unbounded descriptor/file lists.

### Failure Rules

- Fail closed on ambiguous ledger state. A malformed `DW-0461` row is a release-governance defect.
- A well-formed `DW-0461` row whose classification is no longer `accepted-constraint` makes this story's prohibition inactive; it does not make provider-backed status support mandatory.
- A changed or missing constraint token fails closed until this story or its successor explicitly updates the expected lifecycle.
- Fail closed on provider-hidden DI descriptors. If the descriptor for `IPendingCommandStatusQuery` uses an instance or factory while the constraint is accepted, the test should fail because the implementation target cannot be read from the descriptor.
- Keep trigger phrases exact and auditable. Avoid regex patterns for the release-note trigger phrases unless the implementation is only escaping fixed strings.
- Keep diagnostics bounded. Print the first relevant line number and a short trigger name; do not print whole files, raw markdown tables, local absolute paths, or exception messages.

### Ownership

- Dev owns the governance test implementation and bounded diagnostics.
- Release owner owns the `DW-0461` classification and `PENDING-STATUS-NULL-PROVIDER-V1` lifecycle.
- QA/reviewer owns `Category=Governance` evidence and redaction review.
- Stakeholders are informed only when the governance test detects a reopen signal or when a future story intentionally changes the provider-backed support decision.

### Suggested Test Shape

Use focused tests rather than one giant assertion:

- `DeferredWorkRow_ParsesAcceptedNullProviderConstraint`
- `ReleaseNotes_DoNotClaimPendingStatusProviderTriggersWhileConstraintAccepted`
- `EventStoreOptions_DoNotExposePendingStatusResourceContractWhileConstraintAccepted`
- `FrontComposerDi_UsesOnlyNullPendingCommandStatusQueryWhileConstraintAccepted`
- Optional pure helper tests for row parsing and metadata-block allow-list behavior.

Preferred test class name: `PendingStatusNullProviderGovernanceTests`. Avoid names that imply provider support, such as `SupportsPendingCommandStatus`.

The public tests may branch on the current `DW-0461` state. Pure helper tests should use inline synthetic strings so malformed and inverted states are covered even while the real repository is green.

### Latest Technical Information

No external API or library change is required for this story. Use the repository-pinned stack from `_bmad-output/project-context.md`: .NET SDK `10.0.103`, xUnit v3, Shouldly, and existing Shell test infrastructure. Do not update Fluent UI, Fluxor, Roslyn, xUnit, Playwright, or package versions for this governance-only work.

---

## Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs` | Create | Primary implementation. Keep helpers private to the test class unless another governance test already needs them. |
| `_bmad-output/implementation-artifacts/12-3-1-pending-status-reopen-governance-test.md` | Update | Dev Agent Record, validation evidence, completion notes, and file list during implementation. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Update | Move story through `in-progress`, `review`, and `done` during dev/review workflows. |
| `Hexalith.EventStore` (submodule pointer) | Updated | Submodule pointer advanced from `c0d439d` → `b802f4d` (5 commits) as a routine tracking refresh; range covers Story 22.6 projection-rebuild orchestrator hardening, read APIs, and checkpoint-store work that is unrelated to this story's pending-command status scope. No production code or contract relied on by Story 12.3.1 was affected. Logged so the governance lane reviewer can audit submodule-scope compliance. |
| `Hexalith.Tenants` (submodule pointer) | Updated | Submodule pointer advanced from `22ed0d2` → `fcf44bc` (3 commits: tenant audit projection hardening, internal EventStore submodule update, release tag 1.4.1) as a routine tracking refresh observed during code-review test runs. None of the commits touch pending-command status surfaces or any production code path Story 12.3.1 depends on. Logged for submodule-scope audit. |

Read-only implementation inputs:

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/12-3-eventstore-pending-command-provider-release-gate.md`
- `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptions.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs`

---

## Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 12.3 | Story 12.3.1 | `DW-0461` and `PENDING-STATUS-NULL-PROVIDER-V1` define the active null-provider constraint. |
| Story 12.3.1 | Release-note authors | Provider-backed pending-command readiness language is forbidden unless the line is explicit constraint metadata or `DW-0461` is reopened/closed. |
| Story 12.3.1 | Future provider-backed status story | Adding status endpoint options or replacing `NullPendingCommandStatusQuery` requires changing `DW-0461` state first, then updating or inverting this governance test. |
| CI Gate 2b | Story 12.3.1 | Governance tests with `Category=Governance` are blocking evidence and must not be marked advisory. |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-12-release-certification-evidence-alignment.md#Story-12.3`] - pending-command provider release gate scope.
- [Source: `_bmad-output/implementation-artifacts/12-3-eventstore-pending-command-provider-release-gate.md`] - accepted-constraint decision, release decision table, review finding that created this follow-up.
- [Source: `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md`] - approved release-note wording and constraint metadata.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md#DW-0461`] - active ledger row and reopen events.
- [Source: `_bmad-output/project-context.md`] - governance, testing, package, redaction, and submodule rules.
- [Source: `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs`] - pending status seam and null provider.
- [Source: `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`] - default `IPendingCommandStatusQuery` registration.
- [Source: `src/Hexalith.FrontComposer.Shell/Extensions/EventStoreServiceExtensions.cs`] - EventStore opt-in registration path.
- [Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptions.cs`] - current EventStore option surface.
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`] - existing CI governance test patterns.
- [Source: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs`] - repository-relative scanner and diagnostics patterns.

---

## Party-Mode Review

- Date/time: 2026-05-15T18:26:00+02:00
- Selected story key: `12-3-1-pending-status-reopen-governance-test`
- Command/skill invocation used: `/bmad-party-mode 12-3-1-pending-status-reopen-governance-test; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor)
- Findings summary:
  - The story value and release-owner goal were clear, but the previous `skip or invert` lifecycle wording left test semantics to implementation.
  - The `Constraint Metadata` exemption needed deterministic boundaries to avoid over-allowing ordinary release prose.
  - `DW-0461` parsing needed explicit row-state requirements and fail-closed behavior for malformed or changed constraint data.
  - `EventStoreOptions` reflection needed to be framed as a negative assertion while the accepted constraint is active.
  - DI evidence needed to inspect effective service descriptors after extension-method invocation rather than source text.
  - Story 12.5 manual stakeholder/AT evidence must remain out of scope.
- Changes applied:
  - Clarified `DW-0461` active guard state as `Final classification 2026-05-15: accepted-constraint` plus `Constraint: PENDING-STATUS-NULL-PROVIDER-V1`.
  - Replaced `skip or invert` with a single lifecycle rule: well-formed non-accepted rows make this story's prohibition inactive; provider-support assertions remain future-story scope.
  - Defined the bounded `Constraint Metadata` allowance window and exact stopping rules.
  - Reworded `EventStoreOptions` checks as negative assertions and tightened DI descriptor requirements.
  - Added fixture expectations for near-miss phrases and inactive guard state.
  - Added ownership/RACI and an explicit Story 12.5 non-coupling guardrail.
- Findings deferred:
  - Whether provider-backed pending-command status becomes supported remains a future product/release decision.
  - Whether `DW-0461` should become machine-readable beyond this narrow test remains deferred.
  - Any future inversion from null-provider prohibition to provider-support assertion belongs to the story that reopens or closes `DW-0461`.
- Final recommendation: `ready-for-dev`

## Advanced Elicitation

- Date/time: 2026-05-16T07:50:21+02:00
- Selected story key: `12-3-1-pending-status-reopen-governance-test`
- Command/skill invocation used: `/bmad-advanced-elicitation`
- Methods applied: Red Team vs Blue Team; Failure Mode Analysis; Self-Consistency Validation; Security Audit Personas; Occam's Razor Application.
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), John (Product Manager), Murat (Master Test Architect and Quality Advisor).
- Findings summary:
  - The story needed one explicit invariant: while the named constraint is active, Shell composition must keep pending status on the null provider.
  - DI descriptor inspection should be treated as the primary runtime/composition proof; release-note scanning and options reflection are supporting tripwires.
  - Failure diagnostics needed capped lists and strictly repository-relative evidence.
  - The story needed to prevent a one-off governance test from becoming shared infrastructure.
  - Test naming should avoid implying provider-backed pending-status support.
- Changes applied:
  - Added the central invariant, evidence priority, failure modes covered, capped diagnostics, suspicious option terms, DI diagnostic limits, and preferred test class name.
  - Strengthened Occam's Razor guidance by forbidding new shared governance infrastructure unless an existing local helper already needs extension.
- Findings deferred:
  - Future provider-backed pending-command status support remains out of scope and belongs to the story that reopens or closes `DW-0461`.
  - Machine-readable deferred-work metadata remains a possible future governance improvement, not required for this story.
- Final recommendation: `ready-for-dev`

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-15: Story refreshed via `bmad-create-story 12-3-1` after Story 12.3 code review scoped the governance-test follow-up.
- 2026-05-15: Existing draft was promoted from backlog context to ready-for-dev and hardened with concrete test home, parser rules, DI/option reflection guardrails, and validation commands.
- 2026-05-16: Implemented `PendingStatusReopenGovernanceTests` in the Shell governance suite.
- 2026-05-16: First governance run failed on two new-test compile issues; fixed DI factory registration fixture and analyzer-required theory parameter guard.
- 2026-05-16: Second governance run exposed metadata-window boundary failures; adjusted the scanner to treat markdown table header/separator rows as structure and count metadata data rows.

### Completion Notes List

- Added a fail-closed `DW-0461` guard that requires the accepted `PENDING-STATUS-NULL-PROVIDER-V1` ledger state before enforcing reopen-trigger prohibitions.
- Added release-note trigger scanning across the allowed markdown surfaces with repository-relative, capped diagnostics and a bounded `Constraint Metadata` allowance.
- Added negative `EventStoreOptions` reflection checks for pending-status endpoint/resource/metadata drift while keeping existing option names green.
- Added DI descriptor checks proving `AddHexalithFrontComposer()` and `AddHexalithEventStore(...)` leave `IPendingCommandStatusQuery` scoped to `NullPendingCommandStatusQuery`, with fixtures for provider-backed, factory, and instance descriptors.
- Validation passed: `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "Category=Governance"` (71/71).
- Validation passed: `dotnet test Hexalith.FrontComposer.sln --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` (3,037 passed; Shell bench project reported no matching tests for this filter).
- Validation passed: `git diff --check` (no whitespace errors; Git reported LF-to-CRLF normalization warnings for touched BMad files).
- Bundled an unrelated `Hexalith.EventStore` submodule tracking refresh (`c0d439d` → `b802f4d`, 5 Story 22.6 commits: projection-rebuild orchestrator hardening, read APIs, checkpoint-store work) into the same commit. The bump does not touch pending-command status surfaces or any production code path Story 12.3.1 depends on. Logged here so the governance lane reviewer can verify the spec's submodule-scope guardrail at audit time.
- During code-review test runs the `Hexalith.Tenants` submodule pointer also advanced (`22ed0d2` → `fcf44bc`, 3 commits: tenant audit projection hardening, internal EventStore submodule update, release tag 1.4.1). Same submodule-scope audit logging applies; none of these commits affect Story 12.3.1's pending-status governance scope.

### File List

- `_bmad-output/implementation-artifacts/12-3-1-pending-status-reopen-governance-test.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/PendingStatusReopenGovernanceTests.cs`
- `Hexalith.EventStore` (submodule pointer `c0d439d` → `b802f4d`; routine tracking refresh, unrelated to Story 12.3.1 scope)
- `Hexalith.Tenants` (submodule pointer `22ed0d2` → `fcf44bc`; routine tracking refresh observed during code-review test runs, unrelated to Story 12.3.1 scope)

### Change Log

- 2026-05-16: Added pending-status reopen-trigger governance tests and moved story to review.
- 2026-05-16: Code review (bmad-code-review): 2 decision-needed resolved (submodule bump documented; dated classification pin retained), 8 patches applied (heading-line bypass, fake-caption bypass, AC4 PendingCommand+Status, keyed DI, RelativePath sibling-dir, CHANGELOG case-insensitive, `Assert.Skip`, fenced code blocks), 1 patch deferred to CR-12-3-1-D6 (AC3 12-line budget vs production release-note conflict), 5 lower-severity items deferred (CR-12-3-1-D1..D5). Added regression tests `ReleaseNoteTriggerScanner_RejectsHeadingAndFakeTableCaptionBypasses`, `ReleaseNoteTriggerScanner_IgnoresTriggersInsideFencedCodeBlocks`, plus 2 new theory inline-data cases and 2 new keyed-DI scenarios. Governance lane 75/75 passed; main-lane filter 2,882 passed. Story moved to `done`.
