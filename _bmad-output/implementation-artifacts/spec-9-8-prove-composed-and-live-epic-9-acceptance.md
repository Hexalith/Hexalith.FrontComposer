---
title: 'Story 9.8: Prove composed and live Epic 9 acceptance'
type: 'feature'
created: '2026-08-27'
status: 'blocked'
baseline_commit: '1cc9c2774ca6368322b7aa7b2e89cee4a5f5fbf3'
baseline_revision: '1cc9c2774ca6368322b7aa7b2e89cee4a5f5fbf3'
story_id: '9.8'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-context.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** Epic 9's implementation is present, but its story spec was deleted after merge and its retained live evidence identifies a dirty pre-implementation commit. Existing composed and artifact checks also leave tenant-scope transition and several live-proof claims weakly asserted.

**Approach:** Restore the Story 9.8 contract at the current clean baseline, close the observable composition and artifact-validation gaps without reopening the FC-NIP design, and produce fresh isolated Aspire/Playwright evidence bound to a committed candidate.

## Boundaries & Constraints

**Always:** Exercise the generated command, pending registration, resolver, scoped indicator service, and already-rendered generated-grid boundaries. Preserve explicit pre-dispatch target identity, callback/polling parity, atomic per-row first-wins provenance, localized `role="status"`/`aria-live="polite"` feedback, and scope-before-read behavior. Start FrontComposer through isolated Aspire orchestration, discover and wait for `counter-web`, retain redacted command/browser artifacts, and stop only the exact AppHost started by the proof. Treat `_bmad-output/implementation-artifacts/sprint-status.yaml` as read-only orchestrator bookkeeping.

**Block If:** The safe isolated live run cannot complete, any required composed scenario cannot be observed through the outer generated-grid surface, or evidence cannot be bound to a committed candidate. Record the exact command and blocker; focused tests never substitute for the live gate.

**Never:** Infer identity from projection nudges, aggregate IDs, visible-row diffs, result payloads, or ambient undeclared context. Do not add public APIs, edit generated `obj/` output or submodules, weaken materiality/scope/first-wins rules, stop or reuse an unrelated AppHost, write or revert sprint tracking, or represent stale/dirty evidence as final acceptance.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Composed command matrix | Provider create, row-context update, cross-row/status move, delete; callback and polling | Each crosses generated command through pending/resolver/indicator/grid with its intended indicator or suppression | Unknown identity/materiality and delete suppress only FC-NIP; lifecycle remains truthful |
| Already-rendered grid | Add/materialization, filter/re-query, TTL, clear, tenant or user transition | DOM rerenders automatically, clears stale scope, stays lane-scoped, and preserves first-wins provenance and accessible localized status | No manual render or unrelated state mutation may stand in for notification |
| Live create/update | Fresh exact key absent before dispatch, then create and overlapping updates | Same key materializes, counts converge, one first-wins announcement appears, and materialization dismisses it | Mismatched/pre-seeded key or empty/unlocalized announcement fails |
| Artifact validation | Runtime metadata, browser evidence, redacted logs, JUnit/HTML, screenshot, trace, checksums | Candidate/endpoint correlate and every required observed claim is type/value checked | Missing, contradictory, sensitive, or stale fields fail closed |
| Existing AppHost or live failure | FrontComposer run already exists, or isolated run cannot reach `counter-web` | Unrelated run is untouched and exact failure evidence is retained | Story, Epic 9, FR-13, and FR-26 remain open |

</intent-contract>

## Code Map

- `_bmad-output/planning-artifacts/epics.md:1520-1541` -- canonical Story 9.8 Given/When/Then acceptance and live-failure rule.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9CompositionTests.cs:45-254` -- composed callback/polling command matrix and automatic grid invalidations; user transition exists, tenant transition and explicit re-query evidence need hardening.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:651-727,1107-1245` -- read-only generated-command target capture, early callback buffering, and accepted association boundary.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:105-246,375-440` -- read-only single terminal owner and eligible publication boundary.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:1040-1088,1433-1494,1884-1889` -- read-only generated-grid subscription, dismissal, and indicator render boundary.
- `tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts:7-105` -- live exact-key create/update, first-wins, accessibility, materialization, and durable evidence producer; announcement content is captured but not asserted.
- `tests/e2e/scripts/validate-epic9-artifacts.mjs:21-72` -- retained-bundle/redaction validator; currently checks only correlation and two observed claims.
- `eng/run-epic9-live-proof.sh:34-197` -- safe isolated Aspire start/wait/describe, serialized-build fallback, browser execution, redacted logs, validation, checksums, and exact cleanup.
- `.github/workflows/quality.yml:522-583` -- blocking live lane and 14-day artifact upload; artifact name is the source of truth for refreshed evidence wording.
- `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md:1-95` -- stale but valid prior proof; refresh it without treating its dirty `6891baef...` candidate as final evidence.
- `artifacts/epic-9/` -- ignored prior bundle; preserve it and use a new empty proof root until final evidence is intentionally promoted.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- orchestrator-owned and strictly read-only.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9CompositionTests.cs` -- extend the already-rendered-grid proof to cover both tenant and user transitions and make filter/re-query invalidation observable without manual render.
- [x] `tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts` -- assert non-empty localized announcements and emit explicit exact-key, count, accessibility, first-wins, scope, and dismissal claims.
- [x] `tests/e2e/scripts/validate-epic9-artifacts.mjs`, `tests/e2e/scripts/validate-epic9-artifacts.test.mjs`, `tests/e2e/scripts/run-epic9-live-proof.test.mjs`, `tests/e2e/package.json`, and `.github/workflows/quality.yml` -- validate every required claim, redaction boundary, strict preflight, and unrelated-AppHost refusal with positive/negative fixtures, and run those checks in the blocking live job.
- [x] `eng/run-epic9-live-proof.sh` -- keep safe isolated lifecycle behavior and add a strict final-evidence mode that rejects a dirty or mismatched candidate while preserving an explicit development-mode diagnostic run.
- [x] `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md` and `artifacts/epic-9-final-f4f43fdc/` -- run the composed and live gates, retain checksummed artifacts against a committed candidate, and refresh exact commands, versions, endpoint, result counts, artifact name, and blockers.
- [x] `_bmad-output/implementation-artifacts/spec-9-8-prove-composed-and-live-epic-9-acceptance.md` -- reconcile verification, File List, review findings, and commit scope without touching sprint tracking.

**Acceptance Criteria:**
- Given Stories 9.3-9.7 are independently complete, when the automated composition lane runs, then standalone create, row-context update, cross-row/status move, callback confirmation, and polling cross the generated-command-to-grid chain with the intended indicator disposition.
- Given the grid is already rendered, when add/materialization, filter/re-query, TTL, clear, tenant transition, or user transition occurs, then the DOM updates without manual render, prior-scope state is unreadable, and first-wins accessible localized feedback remains correct.
- Given no unrelated FrontComposer AppHost is running and the candidate is committed, when the isolated live proof runs, then `counter-web` is discovered, the fresh exact key is absent before create and materializes under that key, create/update/dismissal/first-wins evidence passes, and the redacted checksummed bundle records the candidate and endpoint.
- Given an existing AppHost or environment failure, when the proof stops, then it leaves unrelated processes untouched, records the exact command and blocker, and does not substitute focused success for Story 9.8 acceptance.

## Spec Change Log

- 2026-08-27: Hardened composed scope/filter proofs, browser claims, artifact validation, CI wiring, and final/development proof modes. A complete isolated development proof passed and is recorded without claiming final acceptance; the clean committed-candidate proof remains open.
- 2026-08-27: Committed the hardened candidate as `f4f43fdc3053e45ffb939b718c670afb4cfcecd0`; strict isolated Aspire/Playwright acceptance, final artifact validation, checksums, and AppHost cleanup all passed against that clean SHA.

## Review Triage Log

## Design Notes

The implementation merged in `f1b16a25d2a0a32ee437f5d8dfa786577402b416`; its spec was later deleted by `25cd54bd502b933900fceeb439ee7f6238c44553`. This iteration starts at `1cc9c2774ca6368322b7aa7b2e89cee4a5f5fbf3`, owns only its new diff, and does not absorb later submodule pointer movement. The existing ignored proof bundle remains historical evidence until a fresh committed-candidate run succeeds.

## Verification

**Commands:**
- `npm --prefix tests/e2e run typecheck && npm --prefix tests/e2e run test:epic-9-evidence` -- expected: browser sources compile and artifact-validator plus proof-runner positive/negative cases pass.
- `DiffEngine_Disabled=true dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release -m:1 /nr:false -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false` followed by the built xUnit assembly filtered to `Epic9CompositionTests`, the seeded Counter page contract, and the identifier seal -- expected: all focused acceptance checks pass.
- `FC_EPIC9_ARTIFACT_ROOT=/home/administrator/projects/hexalith/frontcomposer/artifacts/epic-9-refresh FC_EPIC9_REQUIRE_CLEAN=true ./eng/run-epic9-live-proof.sh` followed by `npm --prefix tests/e2e run validate:epic-9-artifacts -- artifacts/epic-9-refresh` and `(cd artifacts/epic-9-refresh && sha256sum -c checksums.sha256)` -- expected: isolated live proof passes against a clean committed candidate and retains a redacted, internally correlated bundle without stopping another AppHost.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: broad gate passes, or any pre-existing dependency blocker is recorded separately with its exact output.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-8-prove-composed-and-live-epic-9-acceptance.md --candidate HEAD` -- expected: Story 9.8 commits and changed paths reconcile with no sprint-status write.

**Results (2026-08-27):** TypeScript typecheck passed; artifact-validator and proof-runner fixtures passed 40/40; the Release Shell.Tests build passed with 0 warnings and 0 errors; `Epic9CompositionTests` passed 2/2; the seeded Counter page and identifier-seal facts each passed 1/1. Strict final proof passed Playwright 1/1, live artifact validation, full checksums, and exact cleanup against clean candidate `f4f43fdc3053e45ffb939b718c670afb4cfcecd0` in `artifacts/epic-9-final-f4f43fdc/`. The exact solution default lane remains blocked at restore by pre-existing `NU1109` (`FsCheck.Xunit.v3 3.3.4` requires `FsCheck 3.3.4`; the central catalog selects `FsCheck 3.3.3`).

## File List

- `_bmad-output/implementation-artifacts/spec-9-8-prove-composed-and-live-epic-9-acceptance.md`
- `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `.github/workflows/quality.yml`
- `eng/run-epic9-live-proof.sh`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9CompositionTests.cs`
- `tests/e2e/package.json`
- `tests/e2e/scripts/validate-epic9-artifacts.mjs`
- `tests/e2e/scripts/validate-epic9-artifacts.test.mjs`
- `tests/e2e/scripts/run-epic9-live-proof.test.mjs`
- `tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts`

## Auto Run Result

Status: blocked

Blocking condition: commit-scope validation failed before adversarial review.

Exact failing command:

```text
python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-8-prove-composed-and-live-epic-9-acceptance.md --candidate HEAD
```

Exact failure output:

```text
unmapped story delivery commit a7de1113e50b1e98bb1f5d7d9dcacb1ac9b78c32 does not match story 9.8 but touches listed paths: _bmad-output/contracts/analyzer-policy-exception-ledger-v1.json, _bmad-output/implementation-artifacts/spec-9-8-prove-composed-and-live-epic-9-acceptance.md, _bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md, eng/run-epic9-live-proof.sh, tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9CompositionTests.cs, tests/e2e/scripts/run-epic9-live-proof.test.mjs, tests/e2e/scripts/validate-epic9-artifacts.mjs, tests/e2e/scripts/validate-epic9-artifacts.test.mjs, tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts
unmapped story delivery commit d928f5f7149feaadf0e384e6e7c90c1472bc0e4d does not match story 9.8 but touches listed paths: tests/e2e/package.json
unmapped story delivery commit 9ad4312fb93fe0fef389d40e5abbeed241a2d73d does not match story 9.8 but touches listed paths: _bmad-output/contracts/analyzer-policy-exception-ledger-v1.json
```

The validator resolved baseline `1cc9c2774ca6368322b7aa7b2e89cee4a5f5fbf3` and candidate `9ad4312fb93fe0fef389d40e5abbeed241a2d73d`. The working tree was clean before this result write-back. No reviewer subagents were launched because commit-scope validation is a prerequisite hard gate. `_bmad-output/implementation-artifacts/sprint-status.yaml` was not written or reverted.
