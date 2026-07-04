---
blocked_by: "_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md#blocking-follow-up"
created: 2026-07-04
---

# Story 9.2: Wire `FcNewItemIndicator` producer and generated-grid consumer

Status: blocked-by-contract

<!-- Note: Story context is created, but implementation must not start until the FC-NIP blocking follow-up is resolved. -->

## Story

As an operator,
I want rows created or materially changed by a confirmed command outcome to be marked as new,
so that live command results are discoverable in projection grids.

## Acceptance Criteria

1. Given the FC-NIP payload contract from Story 9.1, when a command reaches the relevant terminal outcome, then the command outcome path calls `INewItemIndicatorStateService.Add(...)` with the confirmed view/lane, `EntityKey`, `MessageId`, and timestamp.

2. Given a generated projection grid for that view/lane, when `INewItemIndicatorStateService.Snapshot(viewKey)` contains entries, then the grid or shell-level grid wrapper renders `FcNewItemIndicator` with localized copy, `role="status"`, and `aria-live="polite"` for the matching lane only.

3. Given the row materializes, the filter changes, the TTL expires, or tenant/user scope changes, then the indicator is dismissed through the existing state-service semantics.

4. Given SourceTools output changes, then generated Verify snapshots and FC-TBL public-surface tests are updated intentionally.

## Blocking Gate

Story 9.2 is not ready for code implementation in the current repository state. Story 9.1 confirmed the FC-NIP contract with an upstream blocking gap: the current FrontComposer and pinned EventStore seams do not prove a framework-controlled row-identity payload end to end. The required follow-up must be resolved before this story moves to `ready-for-dev` or `in-progress`.

Required unblocker:

- Define and pin a typed command outcome or projection metadata payload carrying `ProjectionTypeName`, lane/view key, exact row `EntityKey`, command `MessageId`, and any status-slot metadata required for FC-NIP.
- If EventStore supplies the payload, document it as a bounded typed contract; do not hide it in optional domain-defined `ResultPayload`.
- Verify the payload can be produced without diffing visible rows, marking every row in a lane, treating projection nudges as row identity, or assuming EventStore `AggregateId` is a universal FrontComposer row `EntityKey`.

## Tasks / Subtasks

- [ ] Re-validate the FC-NIP implementation gate before making code changes. (AC: 1)
  - [ ] Read `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md` and confirm the Blocking Follow-Up is resolved by a source-level or contract-level artifact.
  - [ ] Confirm a framework-controlled payload supplies non-empty `ProjectionTypeName`, lane/view key, exact row `EntityKey`, command `MessageId`, and required status-slot metadata.
  - [x] If the payload is still absent or ambiguous, stop implementation, keep this story blocked, and do not add best-effort producer code.

- [ ] Wire the producer from the command outcome path only after the payload exists. (AC: 1, 3)
  - [ ] Prefer the existing pending-command outcome path: `PendingCommandOutcomeObservation` -> `PendingCommandOutcomeResolver` -> `PendingCommandStateService`.
  - [ ] Preserve `MessageId`-first terminal resolution and the existing unknown/ambiguous no-mutation behavior.
  - [ ] Add `INewItemIndicatorStateService.Add(new NewItemIndicatorEntry(...))` only for terminal outcomes that include proven row identity and a matching generated-grid lane.
  - [ ] Enforce producer-side first-wins semantics by de-duplicating terminal outcomes by `MessageId`; do not call `Add(...)` again for duplicate observations because `NewItemIndicatorStateService` is last-wins for the same `(ViewKey, EntityKey)` and would reset the TTL.
  - [ ] Use the trusted terminal observation timestamp when supplied; otherwise use the injected `TimeProvider` at Add time.

- [ ] Populate or consume framework row metadata without breaking generated command semantics. (AC: 1)
  - [ ] If generated command forms are changed, update `CommandFormEmitter` so accepted pending registrations populate `ProjectionTypeName`, `LaneKey`, `EntityKey`, `ExpectedStatusSlot`, and `PriorStatusSlot` only from framework-controlled runtime context.
  - [ ] Keep the generated form comment honest: if runtime row context is still unavailable, do not remove the Story 3.3 guardrail that says SourceTools only knows correlation id, message id, and command type at form-emit time.
  - [ ] Do not add EventStore references to `Contracts` or `SourceTools`.

- [ ] Render the generated-grid consumer for matching lanes only. (AC: 2, 3)
  - [ ] Add production rendering where generated grid views or a shell-level grid wrapper read `INewItemIndicatorStateService.Snapshot(_viewKey)`.
  - [ ] Render one `FcNewItemIndicator` per matching entry or a deliberate consolidated indicator if Product/UX approves; keep localized copy through `FcShellResources`.
  - [ ] Set stable keys from `EntityKey` so repeated renders do not duplicate visible indicators.
  - [ ] Dismiss indicators on materialization by calling `DismissMaterialized(viewKey, entityKey)` from the grid path that can prove the row is present.
  - [ ] Preserve existing dismissal on filter changes, TTL, and tenant/user scope transitions.

- [ ] Update SourceTools output and public surface evidence only if output changes. (AC: 2, 4)
  - [ ] Regenerate affected Verify `.verified.txt` snapshots intentionally.
  - [ ] Extend SourceTools emitter tests for generated-grid consumer output.
  - [ ] Update `FcTblPackageBoundaryTests` / `PublicAPI.FcTbl.Shipped.txt` only for intentional public FC-TBL surface changes.

- [ ] Add focused runtime and regression coverage. (AC: 1, 2, 3, 4)
  - [ ] Add resolver/producer tests that prove valid payloads add exactly one indicator and duplicate terminal observations do not reset TTL.
  - [ ] Add negative tests for absent `EntityKey`, absent lane/view key, ambiguous metadata, projection-nudge-only input, and aggregate-id-only input.
  - [ ] Extend `FcNewItemIndicatorLaneIntegrationTests` or generated-grid tests so production generated-grid rendering replaces the current test-only `LaneHost` stand-in.
  - [ ] Run focused Shell pending-command/DataGrid tests and focused SourceTools emitter/snapshot tests.

## Dev Notes

### Story Context

Epic 9 resolves the accepted-deferred Story 2.6 AC1(b) gap: "new-item indicator marks fresh rows." Story 9.1 confirmed the component/state primitive and the required row-identity payload, but it also recorded that the current upstream seams are insufficient. Story 9.2 may implement only after that gap is resolved.

This story file is intentionally blocked because the source of record says "Story 9.2 remains blocked by design until the row-identity producer payload is supplied by a framework-controlled seam." Treat this as a hard implementation precondition, not a warning.

### Current State To Preserve

- `FcNewItemIndicator` already renders a localized status region with `role="status"`, `aria-live="polite"`, and `data-testid="fc-new-item-indicator"`.
- `NewItemIndicatorEntry` contains `ViewKey`, `EntityKey`, `MessageId`, and `CreatedAt`.
- `NewItemIndicatorStateService.Add(...)` rejects empty `ViewKey` and `EntityKey`, stores entries by `(ViewKey, EntityKey)`, replaces an existing entry for the same row, and resets the 10-second timer. Producer code must de-duplicate duplicates before calling `Add(...)`.
- `Snapshot(viewKey)` filters by exact view key and orders by `CreatedAt`.
- `DismissForFilterChange(viewKey)`, `DismissMaterialized(viewKey, entityKey)`, TTL expiry, `Clear(reason)`, and scope-boundary clearing already exist.
- `PendingCommandRegistration` and `PendingCommandEntry` already have optional `ProjectionTypeName`, `LaneKey`, `EntityKey`, `ExpectedStatusSlot`, and `PriorStatusSlot`.
- `PendingCommandOutcomeObservation` already carries optional `ProjectionTypeName`, `LaneKey`, `EntityKey`, and `ExpectedStatusSlot`.
- `PendingCommandOutcomeResolver` resolves by `MessageId` first. Without `MessageId`, it falls back to `EntityKey` plus optional projection/lane/status metadata only when exactly one pending command matches. It returns `Unknown` or `AmbiguousMatch` without state mutation otherwise.
- `EventStorePendingCommandStatusQuery` currently reads EventStore status by pending `MessageId` and emits terminal observations with `MessageId` only. It does not forward `AggregateId`, projection type, lane/view key, or status-slot metadata.
- `CommandFormEmitter` currently registers pending commands with `CorrelationId`, `MessageId`, and `CommandTypeName` only. Its emitted comment explicitly states that `ProjectionTypeName`, `LaneKey`, `EntityKey`, `ExpectedStatusSlot`, and `PriorStatusSlot` require runtime context the source generator does not have.
- Generated grid views already have `_viewKey`, `CurrentGridSnapshot()`, `QueryFilters(...)`, `SearchQuery(...)`, `RegisterVisibleProjectionLane()`, and grid render hooks. Use these existing seams rather than inventing a parallel lane identity model.

### Anti-Patterns To Avoid

- Do not infer row identity by diffing visible grid rows.
- Do not broadly mark every row in a projection or lane as new.
- Do not treat `IProjectionChangeNotifier` or `IProjectionChangeNotifierWithTenant` as row identity; those nudges carry projection type and tenant, not a row key.
- Do not treat opaque projection detail metadata as FC-NIP row metadata unless a typed contract is added.
- Do not silently use EventStore `AggregateId` as `EntityKey`; it is insufficient as a universal generated-grid row key.
- Do not use optional EventStore `ResultPayload` as a hidden contract.
- Do not hand-edit generated files under `obj/**/generated/HexalithFrontComposer/`.
- Do not modify files inside `references/Hexalith.*` submodules without explicit approval.

### Architecture Compliance

- Preserve dependency direction: `SourceTools` references only `Contracts`; `Shell` owns runtime EventStore integration.
- Preserve Fluxor single-writer discipline. Reducers stay pure; effects/services own persistence, polling, interop, and mutation boundaries.
- Keep scoped lifetime discipline for pending state, user context, EventStore clients, and indicator state.
- Use Fluent v5 / FrontComposer components for UI. Do not introduce raw interactive controls, Fluent v4/FAST tokens, or new icon packages.
- Do not change schema fingerprint algorithms, canonical JSON material, generated-output path contracts, MCP resource URI rules, or unrelated public API baselines.

### File And Module Guidance

Likely files to inspect or change after the blocking payload exists:

- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcNewItemIndicator.razor`
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcNewItemIndicator.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.fr.resx`

Likely tests:

- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandOutcomeResolverTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandStateServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingCoordinatorTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStorePendingCommandStatusQueryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorLaneIntegrationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`
- Generated projection render / Razor emitter snapshot tests under `tests/Hexalith.FrontComposer.SourceTools.Tests`.

### Testing Requirements

- Required broad lane before Done:
  `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
- If Verify-backed SourceTools output changes, run the relevant approval/snapshot tests and commit intentional `.verified.txt` updates.
- Run focused Shell lanes for pending-command state/resolver/polling/EventStore status query and DataGrid indicator behavior.
- If local VSTest, DocFX, NuGet restore, or Playwright/Kestrel lanes are socket/network blocked, record exact blockers and focused in-process evidence. Do not claim unrun lanes passed.

### Latest Technical Information

No package, framework, or external API upgrade is part of Story 9.2. Use the repository-pinned stack from `_bmad-output/project-context.md`: .NET SDK `10.0.301`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, xUnit v3 `3.2.2`, bUnit `2.8.4-preview`, Verify `31.20.0`, and Playwright `1.61.0`. The current blocker is a missing local/cross-repo typed payload, not stale package knowledge.

### Previous Story Intelligence

Story 9.1 is the direct prerequisite. Its review fixed an important duplicate-behavior footgun: `NewItemIndicatorStateService.Add(...)` is last-wins for a repeated `(ViewKey, EntityKey)`, so the 9.2 producer must enforce first-wins by de-duplicating `MessageId` observations before calling `Add(...)`.

Story 2.6 proved live projection nudge refresh and reconnect reconciliation, but accepted deferral of row-level new-item marking because the nudge seam lacks per-row identity. Story 8.6 review showed bUnit can miss dead scoped CSS on Fluent components; if this story changes UI layout/CSS, prove behavior through rendered DOM or computed-style evidence, not only component markup.

### Git Intelligence

Recent relevant commits:

- `e6dc465 feat(story-9.1): Confirm the FC-NIP row-identity producer contract`
- `b85aec6 feat: add orchestration and complexity files for Epic 9, update agent configuration timestamps`
- `22378cf Update submodule references and add implementation readiness report`

The current worktree had an unrelated modified `_bmad-output/story-automator/orchestration-9-20260704-182122.md` before this story file was created. Do not revert or include unrelated orchestration drift as a Story 9.2 deliverable.

### Project Structure Notes

- Story file location: `_bmad-output/implementation-artifacts/9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer.md`.
- Sprint-status key: `9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer`.
- Sprint status should remain `backlog` until the FC-NIP blocking follow-up is resolved; there is no valid code path to implement the acceptance criteria in the current source state without violating the Story 9.1 contract.

### References

- Source: `_bmad-output/planning-artifacts/epics.md` - Epic 9 and Story 9.2 acceptance criteria.
- Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01.md` - Epic 9 correct-course source of record.
- Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-04.md` - Epic 9 readiness watch item and 9.1-before-9.2 gate.
- Source: `_bmad-output/implementation-artifacts/9-1-confirm-the-fc-nip-row-identity-producer-contract.md` - previous story intelligence and review findings.
- Source: `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md` - FC-NIP contract and blocking follow-up.
- Source: `_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md` - confirmed FC-TBL component surface and open row-identity producer item.
- Source: `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md` - FC-CMD identity/correlation scope and FC-NIP out-of-scope note.
- Source: `_bmad-output/project-docs/architecture.md` - runtime composition and FC-NIP statement.
- Source: `docs/reference/components/datagrid.md` - adopter-facing DataGrid surface and FC-NIP tracking note.
- Source: `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs` - state primitive and dismissal semantics.
- Source: `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs` - pending metadata and terminal observation contracts.
- Source: `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs` - resolver matching behavior.
- Source: `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs` - current MessageId-only EventStore terminal observation.
- Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` - generated pending registration behavior.
- Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` - generated grid view/lane helper behavior.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-04: Create-story analysis loaded BMAD workflow/config/project-context, Hexalith LLM instructions, sprint status, Epic 9 source, Story 9.1 story/review output, FC-NIP/FC-TBL/FC-CMD contracts, implementation-readiness report, architecture/component/source-tree docs, current pending-command/new-item/EventStore/SourceTools source files, relevant tests, and recent git history.
- 2026-07-04: Discovery loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; no planning-artifact PRD, architecture, or UX markdown matched the workflow patterns, so `_bmad-output/project-context.md` and `_bmad-output/project-docs/*.md` were used as architecture/project context.
- 2026-07-04: Confirmed `sprint-status.yaml` has Epic 9 `in-progress`, Story 9.1 `done`, and Story 9.2 `backlog`.
- 2026-07-04: Confirmed Story 9.1 contract status is "confirmed with upstream blocking gap" and explicitly says Story 9.2 remains blocked until the row-identity producer payload is supplied by a framework-controlled seam.
- 2026-07-04: Validated this story context against the create-story checklist by adding explicit blocking status, no-guessing anti-patterns, current source seam state, likely file/test targets, and the duplicate-observation TTL footgun from Story 9.1 review.
- 2026-07-04: Dev-story gate re-validation loaded BMAD workflow/config/project-context, the full Story 9.2 file, sprint status, and `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`; the contract still has `Status: confirmed with upstream blocking gap` and its Blocking Follow-Up remains unresolved.
- 2026-07-04: Source-level check confirmed the required framework-controlled row-identity payload is still absent: `PendingCommandRegistration`/`PendingCommandOutcomeObservation` only have optional metadata fields, `EventStorePendingCommandStatusQuery` emits terminal observations with `MessageId` only, and `CommandFormEmitter` still registers only `CorrelationId`, `MessageId`, and `CommandTypeName` while documenting that row/lane/status metadata requires runtime context.
- 2026-07-04: Test evidence: `DiffEngine_Disabled=true dotnet test ...Shell.Tests.csproj ... -m:1 /nr:false` and `DiffEngine_Disabled=true dotnet test ...SourceTools.Tests.csproj ... -m:1 /nr:false` built the focused projects but VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`; in-process xUnit fallback passed `Shell.Tests` focused seam lane 52/52 and `SourceTools.Tests` `CommandFormEmitterTests` lane 33/33.
- 2026-07-04: QA generate-e2e-tests added focused negative automation for Story 9.2's blocked seams: EventStore status `AggregateId` is ignored as FC-NIP row identity, generated command forms do not fabricate `ProjectionTypeName`/`LaneKey`/`EntityKey`/status-slot metadata, and the Playwright FC-NIP contract spec pins the Story 9.2 blocked gate plus source-level no-smuggling evidence.
- 2026-07-04: QA validation evidence: direct xUnit v3 fallback passed `EventStorePendingCommandStatusQueryTests` 21/21 and `CommandFormEmitterTests` 34/34; `PLAYWRIGHT_SKIP_WEBSERVER=1 npx playwright test specs/fc-nip-row-identity-contract.spec.ts --project=chromium` passed 4/4. VSTest focused commands, the filtered solution `dotnet test` command, and the initial Playwright web-server run remain socket-blocked with `System.Net.Sockets.SocketException (13): Permission denied`.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-07-04.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 9.2 context was created as blocked-by-contract because Story 9.1 confirmed the required payload is currently absent.
- Sprint status was intentionally not advanced to `ready-for-dev`; doing so would contradict the FC-NIP contract's "review-by before Story 9.2 leaves backlog" gate.
- Dev-story implementation halted at the Story 9.2 Blocking Gate. During the dev-story phase, no producer, generated-grid consumer, SourceTools output, public API, or test code was changed because the required typed framework-controlled row-identity payload is still absent. (The later QA phase added negative guard tests only; see the QA bullets below and the Senior Developer Review.)
- Story status and sprint status remain blocked/backlog until the upstream FC-NIP row-identity payload contract is supplied and pinned.
- QA generated tests only; production producer and generated-grid consumer code remain unchanged because the blocking FC-NIP payload contract is still unresolved.
- Added test coverage for aggregate-id-only input, generated metadata fabrication, and Story 9.2 no-smuggling source evidence.

### File List

- `_bmad-output/implementation-artifacts/9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer.md`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStorePendingCommandStatusQueryTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`
- `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts`

#### Submodule pointer bumps (accepted — see "Escalation Resolution" in the Senior Developer Review)

These `references/Hexalith.*` submodule pointers moved during the Story 9.2 commits. They are **accepted and retained** at their current worktree values by maintainer decisions on 2026-07-04; the previous pinned commits are **not** restored. They are orthogonal to the FC-NIP block (no producer or generated-grid code changed). Net movement from the Story 9.1 baseline (`e6dc465`) to the accepted current worktree values:

- `references/Hexalith.EventStore` — `b8bf0e0` → `ad93f7d` (**v3.33.6**). Hop 1: `b8bf0e0` → `aaac942` in `c23a1890`; Hop 2: `aaac942` → `b779298` in `914279b`; Hop 3: `b779298` → `ad93f7d` accepted by maintainer follow-up decision on 2026-07-04.
- `references/Hexalith.Tenants` — `e7b3597` → `3aaf2cf` (**v2.2.0-5-g3aaf2cf**). Hop 1: `e7b3597` → `b7ae7bd` in `c23a1890`; Hop 2: `b7ae7bd` → `3aaf2cf` accepted by maintainer follow-up decision on 2026-07-04.

### Change Log

- 2026-07-04: Revalidated the FC-NIP implementation gate, confirmed the upstream row-identity payload is still absent, recorded focused test evidence and VSTest blocker, and halted implementation per the story's blocking condition.
- 2026-07-04: QA-generated Story 9.2 negative tests and Playwright contract evidence, updated the test automation summary, and kept the story blocked because no framework-controlled row-identity payload exists.
- 2026-07-04: Adversarial senior-developer review (auto-fix mode) confirmed the block is legitimate, verified the added guard tests are real, flagged undocumented submodule-pointer bumps and a scope-overstating commit message in `c23a1890`, clarified a contradictory completion note, and kept status `blocked-by-contract` / sprint `backlog`. See "Senior Developer Review (AI)".
- 2026-07-04: Re-review escalation resolved by maintainer decision (option 1 — accept/document), followed by a second maintainer decision to accept/document fresh submodule drift. Accepted the `references/Hexalith.EventStore` (`b8bf0e0` → `aaac942` → `b779298` → `ad93f7d`, v3.33.6) and `references/Hexalith.Tenants` (`e7b3597` → `b7ae7bd` → `3aaf2cf`, v2.2.0-5-g3aaf2cf) pointer bumps at current worktree values; did **not** restore the old pinned commits. Documented both pointers in the File List and recorded the full pointer history. Story remains `blocked-by-contract`; sprint remains `backlog` (FC-NIP row-identity payload contract still unresolved).

## Senior Developer Review (AI)

Reviewer: Administrator (adversarial automated review) on 2026-07-04
Outcome: **Changes Requested (documentation / commit hygiene)** — the story correctly remains `blocked-by-contract`.

### Block legitimacy: CONFIRMED

- `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md` is `Status: confirmed with upstream blocking gap`; its **Blocking Follow-Up** is unresolved ("Story 9.2 remains blocked by design until the row-identity producer payload is supplied by a framework-controlled seam").
- Verified in source that the required framework-controlled payload does not exist: `EventStorePendingCommandStatusQuery` emits terminal observations with `MessageId` only (`src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs:64`), and `CommandFormEmitter` still registers only `CorrelationId`, `MessageId`, and `CommandTypeName` (`src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:595-608`).
- ACs 1–4 are intentionally unimplemented; the single `[x]` subtask ("stop implementation, keep this story blocked, and do not add best-effort producer code") is honestly completed. No falsely-checked tasks and no false AC claims. The block is valid, not a cop-out.

### Test evidence: VERIFIED REAL

- The three added tests are genuine assertions (not placeholders) that pin the current no-metadata-smuggling behavior. Asserted source strings were cross-checked and match `CommandFormEmitter.cs` and `EventStorePendingCommandStatusQuery.cs`, and the `PendingCommandOutcomeObservation` properties the Shell test reads exist (`src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:28-37`). The Dev Agent Record direct-xUnit pass counts are credible; VSTest/Playwright web-server socket blockers match the known environment baseline.

### Findings

- **[HIGH → RESOLVED 2026-07-04] Undocumented, approval-gated submodule pointer bumps.** `references/Hexalith.EventStore` (`b8bf0e0` → `aaac942`) and `references/Hexalith.Tenants` (`e7b3597` → `b7ae7bd`) were committed in `c23a1890` but were absent from the File List, Change Log, and Completion Notes. This violated this story's own Anti-Pattern ("Do not modify files inside `references/Hexalith.*` submodules without explicit approval") and the submodule discipline in `CLAUDE.md`. **Re-review addendum:** a further, still-undocumented EventStore hop (`aaac942` → `b779298`, v3.33.5) later landed in `914279b`; that commit's message claimed to "document submodule pointer bumps" but touched only the orchestration state file and the EventStore pointer, not this story. **Follow-up addendum:** fresh accepted worktree drift moved EventStore again (`b779298` → `ad93f7d`, v3.33.6) and Tenants again (`b7ae7bd` → `3aaf2cf`, v2.2.0-5-g3aaf2cf). **Resolution:** maintainer chose option 1 twice (accept/document). The pointers are retained at the current accepted worktree values (`references/Hexalith.EventStore` = `ad93f7d` / v3.33.6, `references/Hexalith.Tenants` = `3aaf2cf` / v2.2.0-5-g3aaf2cf); the old pins are **not** restored. Both are now recorded in the File List and Change Log with full hop history. See "Escalation Resolution" below.
- **[MEDIUM] Completion Notes contradicted git on "test code".** The dev-story note stated no test code changed while the QA notes said tests were added; git confirms three test files changed. The dev-story note has been scoped to the dev-story phase to remove the contradiction.
- **[LOW] Commit message overstates scope.** `c23a1890` ("add tests and documentation for FcNewItemIndicator producer and generated-grid consumer") contains no producer or generated-grid consumer code — only negative guard tests and story documentation. Already pushed; not amended, to avoid rewriting published history. Use accurate messages for blocked stories going forward.
- **[LOW] Unrelated orchestration drift bundled into the feature commit.** `_bmad-output/story-automator/orchestration-9-20260704-182122.md` was committed alongside the story despite this story's Git Intelligence note to keep orchestration drift out of Story 9.2 deliverables.

### Escalation Resolution (2026-07-04)

Maintainer decisions on the escalated HIGH submodule-pointer finding: **option 1 — accept and document**, followed by **option 1 — accept and document fresh drift**.

- **Accepted** the `references/Hexalith.EventStore` and `references/Hexalith.Tenants` pointer bumps at the current worktree values (`EventStore` `ad93f7d`, `Tenants` `3aaf2cf`). The previously pinned commits (`b8bf0e0` / `e7b3597`) are **not** restored, and no revert/follow-up pin commit is landed.
- **Documented** both pointers in the File List and Change Log with the full hop history, including the second EventStore hop (`aaac942` → `b779298`, v3.33.5) that landed in `914279b` after the original review was written, the fresh EventStore hop (`b779298` → `ad93f7d`, v3.33.6), and the fresh Tenants hop (`b7ae7bd` → `3aaf2cf`, v2.2.0-5-g3aaf2cf).
- **Verified during re-review:** the accepted pointer bumps are entirely orthogonal to the FC-NIP block (no producer, generated-grid consumer, SourceTools output, or public-API code changed); `python3 eng/validate-story-artifacts.py --story <this file>` passed during the first re-review; and the later fresh drift is documented here by explicit maintainer decision rather than silently folded into the story.
- **Residual risk (honest):** this acceptance is a documentation/traceability decision, not a fresh build verification of the bumped dependencies. The required broad lane (`dotnet test Hexalith.FrontComposer.slnx ...`) remains socket-blocked locally (`System.Net.Sockets.SocketException (13): Permission denied`, the known environment baseline), so CI is the authoritative check that HEAD builds/tests green against `Hexalith.EventStore` v3.33.6 and `Hexalith.Tenants` v2.2.0-5-g3aaf2cf.

The two LOW findings (scope-overstating commit message on `c23a1890`, orchestration drift bundled into the feature commit) concern already-published commit history and are left as-is per the "do not rewrite pushed history" note; they carry no code or documentation debt beyond this record.

### Outcome

Escalated HIGH finding **resolved** (accepted + documented). Story remains `blocked-by-contract`; sprint status remains `backlog`. No CRITICAL issues (no falsely-completed tasks, no false AC claims), and the FC-NIP row-identity payload contract (`Status: confirmed with upstream blocking gap`) is still unresolved, so the story is **not** eligible to move to `done` — marking a story with four intentionally-unimplemented ACs as done would itself be a false claim. Status transition therefore correctly holds at `blocked-by-contract` rather than the naive "0 CRITICAL → done" mapping.
