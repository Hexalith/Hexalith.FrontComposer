---
baseline_commit: b85aec624624b7128096d5ce645118c6f8f1d4e8
---

# Story 9.1: Confirm the FC-NIP row-identity producer contract

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a FrontComposer maintainer,
I want a confirmed row-identity payload contract for fresh-row indicators,
so that FrontComposer can mark newly materialized rows without guessing from projection nudges.

## Acceptance Criteria

1. Given a command outcome that can create or materially change a projection row, when the producer contract is reviewed, then the contract identifies the exact payload fields required to call `INewItemIndicatorStateService.Add(...)`: `ViewKey` or lane key, row `EntityKey`, command `MessageId`, projection type, and any status-slot metadata needed to avoid ambiguity.

2. Given the current EventStore status endpoint and projection nudge contracts, when they do not provide precise row identity, then the story records a blocking follow-up with owner/date instead of fabricating identity through diffing or broad row marking.

3. Given the contract is confirmed, then `fc-tbl`, `fc-cmd`, and DataGrid documentation name FC-NIP as the owner of automatic row-level fresh-item marking.

## Tasks / Subtasks

- [x] Create the FC-NIP contract artifact. (AC: 1, 2, 3)
  - [x] Add `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`.
  - [x] Record a disposition table for every candidate producer input: EventStore command status, submit result payload, projection nudge, projection detail nudge metadata, pending-command registration metadata, and generated command metadata.
  - [x] Define the minimum valid payload shape for Story 9.2: `ViewKey`/lane key, `EntityKey`, `MessageId`, `ProjectionTypeName`, optional `ExpectedStatusSlot`, timestamp source, tenant/user scope assumptions, and first-wins/duplicate behavior.
  - [x] If the current upstream payload is insufficient, record the blocking follow-up with an owner and date in the contract and keep Story 9.2 blocked by design.

- [x] Verify the existing FrontComposer seams instead of inventing a new one. (AC: 1, 2)
  - [x] Confirm `NewItemIndicatorEntry` and `INewItemIndicatorStateService.Add(...)` remain the consumer-side primitive and require non-empty `ViewKey` and `EntityKey`.
  - [x] Confirm `PendingCommandRegistration` already has optional `ProjectionTypeName`, `LaneKey`, `EntityKey`, `ExpectedStatusSlot`, and `PriorStatusSlot`, but the producer must prove these are populated from framework-controlled metadata before relying on them.
  - [x] Confirm `PendingCommandOutcomeResolver` resolves by `MessageId` first and only falls back to `EntityKey` plus optional projection/lane/status metadata when exactly one pending command matches.
  - [x] Confirm `EventStorePendingCommandStatusQuery` currently maps terminal status by pending `MessageId` and does not pass `AggregateId`, projection type, lane/view key, or status-slot metadata into `PendingCommandOutcomeObservation`.

- [x] Verify the EventStore side of the cross-repo contract. (AC: 1, 2)
  - [x] Use the pinned `references/Hexalith.EventStore` source already present in the root submodule; do not initialize nested submodules and do not move submodules off pinned commits.
  - [x] Confirm `CommandStatusResponse` currently exposes `CorrelationId`, `Status`, `StatusCode`, `Timestamp`, `AggregateId`, `EventCount`, `RejectionEventType`, `FailureReason`, and `TimeoutDuration`.
  - [x] Decide explicitly whether `AggregateId` is sufficient to become FrontComposer row `EntityKey`; if it is not projection-row identity for every generated grid, treat it as insufficient and record the gap.
  - [x] Do not use EventStore `ResultPayload` as a hidden contract unless EventStore and FrontComposer have a documented, bounded, typed shape for FC-NIP row identity.

- [x] Update documentation and contract references to name FC-NIP clearly. (AC: 3)
  - [x] Verify or update `_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md` open item text so FC-NIP Story 9.1 owns the row-identity contract and Story 9.2 owns producer wiring.
  - [x] Verify or update `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md` so FC-CMD v1 remains out of scope for row-level `FcNewItemIndicator` producer wiring.
  - [x] Verify or update `_bmad-output/project-docs/architecture.md` runtime composition text so projection nudges are not treated as fresh-row producers.
  - [x] Verify or update `docs/reference/components/datagrid.md` so adopter-facing DataGrid docs say automatic row-level producer wiring is tracked by Epic 9 / FC-NIP.

- [x] Add focused validation for the contract decision. (AC: 1, 2, 3)
  - [x] Prefer document/contract tests if a test already validates `_bmad-output/contracts` or DocFX snippets for the affected docs.
  - [x] If production source changes are made, add focused unit tests around the changed seam and keep `DiffEngine_Disabled=true`.
  - [x] Do not regenerate SourceTools Verify snapshots unless this story intentionally changes generated output; Story 9.1 should normally be a contract/documentation confirmation.

## Dev Notes

### Story Context

Epic 9 resolves the accepted-deferred Story 2.6 AC1(b) gap: "new-item indicator marks fresh rows." The accepted mitigation is a new FC-NIP backlog home, not reopening Epics 2 or 3. Story 9.1 must run before Story 9.2. If row identity cannot be proven, Story 9.2 is blocked rather than allowed to guess.

The implementation-readiness report explicitly flags the cross-repo EventStore payload as the risk for Epic 9. If EventStore cannot supply `EntityKey`, treat the Story 9.1 follow-up as epic-gating.

### Existing Implementation Facts

- `FcNewItemIndicator` is already a confirmed public FC-TBL component with `Text` and `AriaLabelOverride`. It renders localized copy with `role="status"` and `aria-live="polite"`.
- `NewItemIndicatorEntry` currently contains `ViewKey`, `EntityKey`, `MessageId`, and `CreatedAt`. `NewItemIndicatorStateService` is circuit-local, clears on tenant/user transition when a user context is available, auto-dismisses after 10 seconds, and dismisses by filter change or materialized row.
- `FcNewItemIndicatorLaneIntegrationTests` are intentionally a stand-in consumer: they prove state-to-component rendering for a lane, but the production producer is still absent.
- `IProjectionChangeNotifier` carries only projection type; `IProjectionChangeNotifierWithTenant` adds tenant; `IProjectionChangeDetailNotifier` can carry opaque metadata, but FrontComposer currently treats it as metadata only and adds no domain interpretation.
- `PendingCommandRegistration` and `PendingCommandEntry` already have optional projection/lane/entity/status metadata. That is a potential producer input, not proof that current generated command paths populate enough identity.
- `PendingCommandOutcomeResolver` is the shared resolver for live nudge refresh, reconnect reconciliation, fallback polling, and status-query inputs. It must not mutate state on absent/ambiguous identity.
- `EventStorePendingCommandStatusQuery` currently parses EventStore `AggregateId` but only emits `PendingCommandOutcomeObservation` with the pending `MessageId` for `Completed`; no row identity is forwarded.

### EventStore Contract Facts

The pinned EventStore submodule shows `CommandStatusResponse` fields are:

- `CorrelationId`
- `Status`
- `StatusCode`
- `Timestamp`
- `AggregateId`
- `EventCount`
- `RejectionEventType`
- `FailureReason`
- `TimeoutDuration`

This is not automatically the FC-NIP payload. `AggregateId` may identify the domain aggregate, but Story 9.1 must decide whether that is exactly the generated grid row `EntityKey` for every target projection/lane. If not, the contract must require an additional typed payload or metadata from EventStore/domain command outcomes.

### Architecture Compliance

- Keep FrontComposer dependency direction intact. `SourceTools` still references only `Contracts`; do not introduce EventStore references into `Contracts` or SourceTools.
- Do not change `CanonicalSchemaMaterial`, schema fingerprint algorithms, generated-output paths, MCP resource URI rules, or public API baselines unless directly and explicitly owned by this story.
- UI-related docs and any sample component changes must keep Fluent v5 / FrontComposer component policy. Do not introduce raw interactive controls or legacy Fluent v4/FAST tokens.
- Do not hand-edit generated code under `obj/**/generated/HexalithFrontComposer/`.
- Do not modify `references/Hexalith.*` submodule files for this story unless the user explicitly approves cross-repo changes. Inspection is allowed; edits are not.

### Anti-Patterns To Avoid

- Do not infer fresh rows by diffing the currently visible grid rows.
- Do not broadly mark every row in a projection/lane as new.
- Do not treat projection nudge `(projectionType, tenantId)` as row identity.
- Do not silently use EventStore `AggregateId` as FrontComposer `EntityKey` without proving it is the row key for the generated projection.
- Do not hide an upstream contract gap by making Story 9.2 "best effort."
- Do not add package dependencies or change pinned Fluent/.NET/Roslyn versions.

### Testing Requirements

- Run a focused documentation/contract validation lane for any changed docs or contract artifacts.
- If C# source changes are needed, run focused tests for the touched project plus the relevant Shell tests.
- Use `DiffEngine_Disabled=true` for Verify-backed tests.
- Expected broader lane before Done remains:
  `dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
- If local VSTest or Playwright lanes are socket-blocked, record exact blockers and focused in-process evidence; do not claim unrun lanes passed.

### Latest Technical Information

No external package or API upgrade is part of Story 9.1. Use the repository-pinned stack: .NET SDK `10.0.302`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Fluxor `6.9.0`, xUnit v3 `3.2.2`, bUnit `2.8.4-preview`, and Playwright `1.61.0`. The risk is the current local/pinned EventStore payload shape, not an upstream package version.

### Previous Story Intelligence

There is no previous story in Epic 9. Relevant historical learning comes from Story 2.6: the read-path live nudge and lane consumer were pinned, but the row-level producer was consciously deferred because the nudge seam carried no per-row identity. Story 8.6 review also found that bUnit can miss dead scoped CSS on Fluent components; if this story unexpectedly changes UI rendering, prove layout via rendered DOM or computed styles, not only component markup.

### Git Intelligence

Recent work added Epic 9 orchestration artifacts and the 2026-07-04 readiness report. The current worktree already has modified submodule pointers under `references/Hexalith.Builds` and `references/Hexalith.EventStore`; do not revert or move them. A recent architecture quick-win commit touched shell navigation, EventStore projection subscription, generated literal escaping, and command emitter tests, so any source edits should check for interaction with those changes rather than assuming an older baseline.

### Project Structure Notes

- Story file location: `_bmad-output/implementation-artifacts/9-1-confirm-the-fc-nip-row-identity-producer-contract.md`.
- Proposed contract artifact: `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`.
- Likely documentation/contracts to verify or touch:
  - `_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md`
  - `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md`
  - `_bmad-output/project-docs/architecture.md`
  - `docs/reference/components/datagrid.md`
- Likely source files to inspect if source change becomes necessary:
  - `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs`
  - `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs`
  - `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs`
  - `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs`
  - `src/Hexalith.FrontComposer.Contracts/Communication/IProjectionChangeNotifier.cs`
  - `src/Hexalith.FrontComposer.Contracts/Communication/ProjectionChangedDetail.cs`
- EventStore source to inspect only:
  - `references/Hexalith.EventStore/src/Hexalith.EventStore/Models/CommandStatusResponse.cs`
  - `references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandStatusRecord.cs`
  - `references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs`
  - `references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs`

### References

- Source: `_bmad-output/planning-artifacts/epics.md` - Epic 9 and Story 9.1/9.2.
- Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01.md` - Epic 9 correct-course source of record and Story 2.6 deferral normalization.
- Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-04.md` - Epic 9 readiness and EventStore cross-repo payload watch item.
- Source: `_bmad-output/project-context.md` - pinned stack, architecture rules, testing rules, submodule rules.
- Source: `_bmad-output/project-docs/architecture.md` - runtime composition and FC-NIP statement.
- Source: `_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md` - confirmed DataGrid surface and open row-identity producer item.
- Source: `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md` - FC-CMD identity/correlation scope and FC-NIP out-of-scope note.
- Source: `_bmad-output/contracts/fc-cmd-eventstore-status-endpoint-contract-2026-06-04.md` - EventStore status endpoint shape and status mapping.
- Source: `docs/reference/components/datagrid.md` - adopter-facing FC-TBL DataGrid surface.
- Source: `references/Hexalith.EventStore/src/Hexalith.EventStore/Models/CommandStatusResponse.cs` - current EventStore status response fields.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-04: Create-story analysis loaded BMAD workflow/config/project-context, Hexalith LLM and UX rules, sprint status, Epic 9 source, the 2026-07-01 correct-course proposal, the 2026-07-04 readiness report, architecture/project context, FC-TBL and FC-CMD contracts, current new-item indicator/pending-command/EventStore status source files, pinned EventStore status response source, recent git history, and current git status.
- 2026-07-04: Discovery loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; no planning-artifact PRD, architecture, or UX markdown files matched the workflow patterns, so project architecture/context docs were loaded as the relevant architecture source.
- 2026-07-04: Confirmed Story 9.1 status was `backlog` in `sprint-status.yaml`; Epic 9 moved to `in-progress` because 9.1 is the first Epic 9 story.
- 2026-07-04: Validated against the create-story checklist; no blocking context gaps remained after adding explicit no-diffing, no-broad-marking, no-unproven-`AggregateId`, and Story-9.2-blocking guardrails.
- 2026-07-04: Dev-story loaded BMAD workflow/config/project-context, Hexalith LLM and UX rules, sprint status, story file, FC-TBL and FC-CMD contracts, architecture/DataGrid docs, FrontComposer pending-command/new-item seams, generated command registration emitter, and pinned EventStore status/submit sources.
- 2026-07-04: Added RED contract guard `FcNipRowIdentityProducerContractTests`; in-process red run failed 2/2 because the FC-NIP contract artifact was absent and architecture wording was not pinned to the FC-NIP ownership sentence.
- 2026-07-04: Authored the FC-NIP contract artifact, pinned the architecture sentence, and verified focused in-process guard green 2/2.
- 2026-07-04: Required solution build/test and DocFX validation were attempted; broad local lanes are blocked by NuGet network denial, an existing CLI Roslyn package downgrade restore/build failure, DocFX MSBuild named-pipe socket failure, and VSTest socket creation failure.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-07-04.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 9.1 created as a contract/documentation confirmation story for FC-NIP row identity, with source pointers and anti-pattern guardrails for the downstream Story 9.2 implementation.
- Created the FC-NIP row-identity producer contract and recorded the current upstream gap: `AggregateId` and projection nudges are insufficient as universal row identity, `ResultPayload` is not a hidden contract, and Story 9.2 remains blocked until a typed framework-controlled payload exists.
- Verified FrontComposer consumer/resolver seams and pinned EventStore status/submit response evidence without editing submodule files or moving submodule pointers.
- Added focused governance validation for the FC-NIP contract decision and cross-document FC-NIP ownership references.
- QA-generated Story 9.1 Playwright coverage now pins the FC-NIP contract artifact, no-guessing guardrails, and FC-NIP ownership wording from the E2E workspace without starting the sample host.

### Test Evidence

- Required command attempted: `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false -p:UseSharedCompilation=false`
  - Local result: Blocked before full build completion. Exact blockers: NuGet network denied for packages such as `Hexalith.EventStore.Aspire` / `Hexalith.Parties.*` / `Hexalith.Tenants.UI`, and existing CLI Roslyn restore conflict `NU1608` / `NU1109` (`Microsoft.CodeAnalysis.CSharp.Workspaces 5.6.0` resolving above central `Microsoft.CodeAnalysis.Workspaces.Common 5.3.0`).
  - Fallback evidence: `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --no-restore -m:1 /nr:false -p:UseSharedCompilation=false` passed with 0 warnings / 0 errors.
  - CI authority: Required for full solution build.
  - Blocker timing: Before story-owned test execution in broad lane.
- Required command attempted: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-restore --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false -p:UseSharedCompilation=false`
  - Local result: Blocked. Exact blockers: same CLI `NU1608` / `NU1109` build failure plus repeated VSTest `System.Net.Sockets.SocketException (13): Permission denied` while creating local socket transport for test assemblies.
  - Fallback evidence: `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -class Hexalith.FrontComposer.SourceTools.Tests.Docs.FcNipRowIdentityProducerContractTests` passed 2/2 through the direct xUnit v3 in-process runner.
  - CI authority: Required for solution-level VSTest; local in-process runner is advisory fallback.
  - Blocker timing: Before VSTest test body execution.
- Required command attempted: `pwsh ./eng/validate-docs.ps1 -SkipSnippetBuild`
  - Local result: Blocked during `dotnet docfx metadata docs/docfx.json` by `System.Net.Sockets.SocketException (13): Permission denied` from Roslyn/MSBuild build-host named-pipe connection.
  - Fallback evidence: focused contract/docs guard above passed 2/2 and pins the changed architecture wording plus FC-TBL/FC-CMD/DataGrid FC-NIP references.
  - CI authority: Required for DocFX/docs validation.
  - Blocker timing: Before docs metadata validation completed.
- Additional quality check: `git diff --check` passed; local output only reported existing LF-to-CRLF normalization warnings for touched markdown/yaml files.
- QA E2E command: `npm run test:fc-nip`
  - Local result: Passed 3/3 in Chromium with `PLAYWRIGHT_SKIP_WEBSERVER=1`.
  - Coverage: FC-NIP minimum payload, Story 9.2 blocking gap, no-diffing/no-broad-marking/no-projection-nudge guardrails, and FC-NIP ownership wording across contract/docs.
- QA TypeScript command: `npm run typecheck`
  - Local result: Passed.

### File List

- `_bmad-output/implementation-artifacts/9-1-confirm-the-fc-nip-row-identity-producer-contract.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/project-docs/architecture.md`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs`
- `tests/e2e/package.json`
- `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts`
- `references/Hexalith.Builds` (pre-existing modified submodule pointer observed at story start; not changed by this implementation)
- `references/Hexalith.Commons` (pre-existing modified root submodule pointer observed during QA artifact validation; not changed by this QA run)
- `references/Hexalith.EventStore` (pre-existing modified submodule pointer observed at story start; inspected only, not changed by this implementation)
- `references/Hexalith.Memories` (pre-existing modified root submodule pointer observed during QA artifact validation; not changed by this QA run)

### Change Log

- 2026-07-04: Confirmed FC-NIP row-identity producer contract, recorded upstream row-identity payload gap with owner/date, pinned FC-NIP documentation ownership, and added focused governance validation.
- 2026-07-04: Added QA-generated Playwright contract coverage for Story 9.1 and recorded the BMAD test automation summary.
- 2026-07-04: Story-automator adversarial review (auto-fix). Outcome APPROVED / done. Corrected the contract's duplicate-behavior claim to match the last-wins consumer seam, hardened both governance guards against markdown reflow, and clarified the blocking follow-up review-by date. No CRITICAL/HIGH issues remained. Recorded a concurrent-agent blocker on `validate-story-artifacts.py`.

## Senior Developer Review (AI)

Reviewer: Jérôme Piquot
Date: 2026-07-04
Outcome: **Approved (done)** — all 3 Acceptance Criteria implemented; every `[x]` task independently verified against source; 0 CRITICAL, 0 HIGH.

### What was verified
- **AC1** — the FC-NIP contract's "Minimum Valid Payload For Story 9.2" table names `ViewKey`/lane key, `EntityKey`, `MessageId`, `ProjectionTypeName`, and status-slot metadata required to call `INewItemIndicatorStateService.Add(...)`. IMPLEMENTED.
- **AC2** — the contract records a "Blocking Follow-Up" with owner and date; Story 9.2 kept blocked by design. IMPLEMENTED.
- **AC3** — FC-NIP ownership wording confirmed present in `fc-tbl-table-api-contract`, `fc-cmd-pending-identity-correlation-contract`, `architecture.md`, and `docs/reference/components/datagrid.md`. IMPLEMENTED.
- Contract factual claims cross-checked against live source and all found accurate: `NewItemIndicatorEntry` fields, `Add(...)` empty-key rejection, `PendingCommandRegistration` optional metadata, `PendingCommandOutcomeResolver` MessageId-first / single-match fallback, `EventStorePendingCommandStatusQuery` MessageId-only observation emission, `CommandStatusResponse` fields, and `CommandsController` bounded optional `ResultPayload`.

### Findings and dispositions
- **MEDIUM — FIXED (contract accuracy):** The "Duplicate behavior" row asserted `first-wins`, but the consumer `NewItemIndicatorStateService.Add(...)` is last-wins for the same `(ViewKey, EntityKey)` — a repeat `Add(...)` replaces the entry and resets the 10s auto-dismiss timer (`src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs:92-108`). The contract now states `first-wins` is a producer responsibility (dedup by `MessageId`, call `Add(...)` once per row) and documents the consumer's last-wins/TTL-reset behavior in the seam-verification section, closing a Story 9.2 footgun.
- **MEDIUM — FIXED (test robustness):** Both governance guards (`FcNipRowIdentityProducerContractTests.cs`, `fc-nip-row-identity-contract.spec.ts`) asserted exact single-line substrings against wrap-prone markdown — the dev had to un-wrap an `architecture.md` line solely to satisfy them. Assertions now read whitespace-normalized content (`\s+`→space) so a benign reflow/formatter cannot cause false failures, while still catching real content deletion.
- **LOW — FIXED (follow-up date):** The blocking follow-up recorded only the authoring date; added an explicit `review-by: before Story 9.2 leaves backlog`.
- **LOW — INFORMATIONAL (git File List drift):** `_bmad-output/story-automator/orchestration-9-20260704-182122.md` is modified in git but excluded from the File List; it is the automator's own run log, not a Story 9.1 deliverable, so it is intentionally not listed.

### Blocker recorded (not a Story 9.1 defect)
`python3 eng/validate-story-artifacts.py --story <this story>` exits non-zero because a **concurrent GPT-5 Codex session** (`codex --dangerously-bypass-approvals-and-sandbox`, PID 2254, cwd `/home/administrator/projects/hexalith/eventstore`) is live-editing unrelated build/dependency files in this working tree during the review: `Directory.Packages.props` (Roslyn `Microsoft.CodeAnalysis.Workspaces.Common` 5.3.0→5.6.0), `Hexalith.FrontComposer.slnx`, `src/Hexalith.FrontComposer.UI/Hexalith.FrontComposer.UI.csproj`, `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/AuthBoundaryTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` (Hexalith.EventStore.Aspire 3.20.0→3.33.4), `tests/e2e/tsconfig.json`, and `.github/workflows/release.yml` (set still growing). These are NOT Story 9.1 changes and were deliberately excluded from the File List; the reviewer did not touch, revert, or stage them. Story 9.1's own artifacts validate cleanly.

### Test evidence (this review)
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/...csproj -c Release --no-restore -m:1 /nr:false -p:UseSharedCompilation=false` — 0 warnings / 0 errors.
- `DiffEngine_Disabled=true .../SourceTools.Tests -noLogo -noColor -class ...FcNipRowIdentityProducerContractTests` — 2/2 passed (direct xUnit v3 in-process runner; VSTest sockets remain blocked).
- `PLAYWRIGHT_SKIP_WEBSERVER=1 npm run test:fc-nip` — 3/3 passed (Chromium). `npm run typecheck` — passed.
