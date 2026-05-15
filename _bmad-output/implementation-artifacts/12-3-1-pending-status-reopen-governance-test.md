# Story 12.3.1: Pending-Status Reopen-Trigger Governance Test

Status: backlog

> Follow-up to Story 12.3 (`PENDING-STATUS-NULL-PROVIDER-V1` accepted v1 constraint). Adds an automated detector for the three reopen triggers so they cannot fire silently in release notes or production code while the constraint is still accepted.

---

## Executive Summary

Story 12.3 accepted null-provider-only pending-command status as a named v1 constraint, with three reopen events: (1) any provider-backed readiness claim, (2) status-resource metadata consumption, (3) EventStore endpoint promotion. All three fire in human-authored release notes / endpoint configuration with no analyzer, lint rule, PR-template marker, or governance test detecting them — so a future story could ship provider-backed wording or contract changes without anyone reopening `DW-0461`.

This follow-up adds a **string-pinned governance test** that fails CI when those reopen signals appear in the repository while `PENDING-STATUS-NULL-PROVIDER-V1` is still marked accepted.

---

## Story

As a release owner,
I want an automated detector for the three Story 12.3 reopen triggers,
so that the v1 constraint cannot be silently invalidated by release-note wording or contract drift.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | `DW-0461` is `accepted-constraint` with `Constraint: PENDING-STATUS-NULL-PROVIDER-V1` | Governance test runs in main lane | The test reads `_bmad-output/implementation-artifacts/deferred-work.md` and parses the `DW-0461` row to determine whether the constraint is still accepted. If parsing fails the test fails closed. |
| AC2 | The constraint is accepted | Release notes under `docs/`, `CHANGELOG*.md`, or `_bmad-output/implementation-artifacts/*release-note*.md` are scanned | The test fails if it finds the phrases (case-insensitive) `provider-backed pending-command`, `EventStore pending-command status endpoint`, `IPendingCommandStatusQuery provider` in any of those files, unless the line is inside a constraint-metadata block referencing `PENDING-STATUS-NULL-PROVIDER-V1`. |
| AC3 | The constraint is accepted | `EventStoreOptions` properties are introspected via reflection | The test fails if any property name contains `Status` and `Endpoint`, `Status` and `Uri`, `PendingCommand` and `Status`, or similar combinations that imply a status-resource contract has been added. |
| AC4 | The constraint is accepted | DI registrations are reflected from `ServiceCollectionExtensions` | The test fails if any registration other than `NullPendingCommandStatusQuery` is bound to `IPendingCommandStatusQuery`. |
| AC5 | The constraint is later closed | `DW-0461` is no longer `accepted-constraint` or the constraint name disappears from the ledger | The governance test inverts: AC2–AC4 are skipped or transition to enforcing the new state (e.g., assert provider registration exists). |
| AC6 | The test fails | Diagnostic output is emitted | The failure message names the trigger phrase or property, the file/line where it appears, and points the developer to `DW-0461` and Story 12.3 for context. |
| AC7 | The test runs | Output is bounded | No tenant/user IDs, bearer tokens, raw payloads, absolute paths, or unbounded log lines appear in test output. |

---

## Tasks / Subtasks

- [ ] T1. Decide test home (AC1)
  - [ ] Pick between `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/` (Shell scope) or a new `tests/Hexalith.FrontComposer.Governance.Tests` project.
  - [ ] If new project, register in `Hexalith.FrontComposer.sln`, `Directory.Packages.props`, and CI lane.

- [ ] T2. Build the ledger parser (AC1)
  - [ ] Read `_bmad-output/implementation-artifacts/deferred-work.md` from the repo root.
  - [ ] Locate `DW-0461`; parse its single-line reconciliation entry for `Final classification` and `Constraint`.
  - [ ] Fail closed on missing/malformed row.

- [ ] T3. Build the release-note phrase scan (AC2)
  - [ ] Enumerate target files (release notes + CHANGELOG + `_bmad-output/implementation-artifacts/*release-note*.md`).
  - [ ] Apply case-insensitive substring match for the trigger phrases.
  - [ ] Allow-list lines inside a constraint-metadata block referencing `PENDING-STATUS-NULL-PROVIDER-V1`.

- [ ] T4. Build the EventStoreOptions / DI reflection check (AC3, AC4)
  - [ ] Reflect over `EventStoreOptions` for property name combinations.
  - [ ] Inspect `ServiceCollectionExtensions.AddHexalithFrontComposer` and `AddHexalithEventStore` to verify the `IPendingCommandStatusQuery` registration target.

- [ ] T5. Diagnostic and redaction (AC6, AC7)
  - [ ] Emit bounded failure messages with file/line context and pointer to `DW-0461`.
  - [ ] Add unit tests proving the test output never includes tenant/user IDs, tokens, or paths beyond repository-relative.

- [ ] T6. Lifecycle inversion (AC5)
  - [ ] When `DW-0461` no longer reads `accepted-constraint`, switch the test to assert the new state (provider implementation present, release-note language present, options properties exist).
  - [ ] Document the inversion logic in the test class XML doc.

- [ ] T7. Wire into CI
  - [ ] Add the test to the main lane filter (`Category!=Performance&...`).
  - [ ] Confirm the test runs on `ci.yml` and is required for PR merge.

---

## Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Governance test is string-pinned, not semantic. | A semantic check (NLP-style) would over- or under-fire; string pinning is auditable and predictable. |
| D2 | Failure mode is fail-closed on parser errors. | A silently passing governance test is worse than a noisy one. |
| D3 | Trigger phrases are case-insensitive substrings, not regex. | Regex creep is a common source of false positives; substrings are explicit and testable. |
| D4 | Constraint lifecycle inversion is part of the same test, not a separate test. | One source of truth keeps the governance gate consistent across the constraint's life. |
| D5 | Test scope is repository-bound only. | The test does not reach out to EventStore or any external service; it reasons over files in this repo. |

---

## Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingStatusReopenGovernanceTests.cs` (or new governance project) | Create | New test class; xUnit v3 + Shouldly per project conventions. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Read-only | Parsed by the test; not modified. |
| `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md` | Read-only | Allow-listed when the constraint-metadata block references the constraint name. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptions.cs` | Read-only | Reflected for property-name guard. |
| `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` | Read-only | Reflected for DI registration guard. |

---

## Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 12.3 | Story 12.3.1 | `PENDING-STATUS-NULL-PROVIDER-V1` constraint name and `DW-0461` row identity are stable. |
| Story 12.3.1 | Future provider-backed story | Removing the constraint or implementing the provider also requires unblocking this governance test by inverting its assertions (T6). |
| Story 12.3.1 | Release notes / CHANGELOG authors | Adding the trigger phrases without renaming/removing the constraint is a CI failure. |

---

## References

- [Source: `_bmad-output/implementation-artifacts/12-3-eventstore-pending-command-provider-release-gate.md`] — accepted-constraint outcome and reopen-trigger list.
- [Source: `_bmad-output/implementation-artifacts/12-3-pending-command-provider-release-note.md`] — constraint-metadata block format.
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md` rows `DW-0461`, `DW-0465`, `DW-0232`, `DW-0469`] — ledger rows that drive the lifecycle.
- [Source: `_bmad-output/project-context.md`] — testing rules (xUnit v3, Shouldly, bounded output, no submodule mixing).

---

## Provenance

- 2026-05-15: Created as a follow-up from Story 12.3 code review (`/bmad-code-review 12.3`) decision 5 — reopen-trigger observability gap resolved with "Governance test (string-pinned)" option.
