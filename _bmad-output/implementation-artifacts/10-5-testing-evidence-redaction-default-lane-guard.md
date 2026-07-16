---
baseline_commit: e13a50257c6ba78fc65882950fa9aadec793c441
---

# Story 10.5: Testing evidence redaction default-lane guard

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want Testing package evidence redaction to stay in the default lane,
so that assertion helpers cannot leak tenant, user, token, secret, password, oversized, or punctuation-heavy secret values.

## Acceptance Criteria

1. Given Testing package evidence formatters or fakes change, when the default Testing lane runs, then it includes redaction cases for tenant/user IDs, token/secret/password keys, oversized payloads, and punctuation-heavy string secret values.

2. Given a new public Testing helper emits evidence, then `PublicAPI.Shipped.txt`, README guidance, and redaction tests are updated intentionally.

## Tasks / Subtasks

- [x] Audit the existing Testing package evidence surface before editing. (AC: 1, 2)
  - [x] Read `src/Hexalith.FrontComposer.Testing/Evidence.cs` completely, especially `RedactedEvidenceFormatter.Format`, `RedactKey`, and `ValueEnd`.
  - [x] Read `src/Hexalith.FrontComposer.Testing/TestCommandService.cs`, `TestQueryService.cs`, `TestProjectionPageLoader.cs`, and `TestFaultInjectionProvider.cs` completely to verify every fake evidence path.
  - [x] Read `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs` and reuse the existing Testing test lane instead of creating a duplicate privacy harness.
  - [x] Read `src/Hexalith.FrontComposer.Testing/README.md`, `docs/how-to/test-generated-components.md`, `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md`, and `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt` before changing public guidance or API.
  - [x] Inspect the pre-existing dirty diff in `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` and classify it before editing that file; do not revert unrelated work.

- [x] Add or strengthen default-lane redaction matrix coverage. (AC: 1)
  - [x] Add a focused `RedactedEvidenceFormatter` test that proves configured tenant and user values are removed from nested payloads and command evidence.
  - [x] Prove token, secret, and password keys are matched case-insensitively and redacted for string, numeric, boolean, null, nested-object, and array values where applicable.
  - [x] Prove punctuation-heavy string secret values are fully redacted, including commas, braces, brackets, quotes/escaped quotes, colons, backslashes, equals signs, semicolons, and URL/query-string-like values.
  - [x] Prove oversized payloads are truncated with ...<truncated> and that the truncated output does not expose tenant, user, token, secret, or password values.
  - [x] Prove benign non-secret values remain useful enough for assertions after redaction, so the formatter is not reduced to an all-fields scrubber.

- [x] Pin fake-service evidence paths through the formatter. (AC: 1)
  - [x] Add command dispatch evidence coverage using `TestCommandService` so a command object containing tenant/user and multiple secret-like properties cannot leak through `CommandDispatchEvidence.RedactedPayload`.
  - [x] Verify query, page-loader, and fault evidence do not add raw payload fields; if any new evidence helper is introduced, route sensitive payload text through `RedactedEvidenceFormatter`.
  - [x] Preserve bounded evidence retention through `MaxEvidenceRecords`; privacy tests must not rely on unbounded queues or global shared state.

- [x] Handle public API and documentation intentionally. (AC: 2)
  - [x] If no public Testing helper is added or changed, keep `PublicAPI.Shipped.txt` unchanged and state that in completion notes.
  - [x] If a public Testing helper or evidence record changes, update `PublicAPI.Shipped.txt` intentionally and explain the baseline impact in the story record.
  - [x] Update `src/Hexalith.FrontComposer.Testing/README.md`, `docs/how-to/test-generated-components.md`, and the Testing host contract only if the implementation changes adopter-facing guidance or the documented redaction guarantee.
  - [x] Do not edit `docs/_site/**` or generated output.

- [x] Run validation and reconcile story evidence. (AC: 1, 2)
  - [x] Run the focused Testing package test lane. Prefer the direct xUnit v3 in-process executable if local VSTest sockets fail.
  - [x] Run `PackageBoundaryTests.PublicApi_ExportedTypes_MatchIntentionalBaseline` when public surface is touched, or document why unchanged public API makes the full package boundary lane advisory.
  - [x] Run `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-5-testing-evidence-redaction-default-lane-guard.md` before review.
  - [x] Attempt the standard filtered solution lane when feasible: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`.
  - [x] If broad validation is locally blocked, record the exact command, exact blocker, whether the blocker occurs before test execution, focused fallback result, and CI authority.
  - [x] Reconcile the File List against the Story 10.1 validator output before moving to review.

## Dev Notes

### Story Context

Epic 10 carries Epic 7 tooling-governance follow-through without reopening completed Stories 7.1-7.5. Story 10.5 implements `E7-AI-5` / `E10-AI-5`: keep Testing package evidence redaction in the default Testing lane for tenant, user, secret, and oversized payload cases. [Source: `_bmad-output/planning-artifacts/epics.md#Story 10.5: Testing evidence redaction default-lane guard`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md#4.3 epics.md - Add Stories 10.1 Through 10.5`; `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md#5. Action Items`]

This is a privacy and regression-guard story. The expected outcome is default-lane test coverage and, only if a real leak is found, a narrow formatter fix. Do not rebuild the Testing harness or create a separate sanitizer framework. [Source: `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md#3. Key Learnings`; `_bmad-output/implementation-artifacts/7-5-testing-library-bunit-host-and-deterministic-fakes.md#Senior Developer Review (AI)`]

### Existing Implementation Facts

- `RedactedEvidenceFormatter.Format(...)` serializes with `System.Text.Json`, replaces `FrontComposerTestOptions.TestTenantId` with `<tenant>` and `TestUserId` with `<user>`, redacts keys containing `token`, `secret`, or `password` case-insensitively, and truncates output beyond `MaxDiagnosticPayloadCharacters` with `...<truncated>`. [Source: `src/Hexalith.FrontComposer.Testing/Evidence.cs`]
- Story 7.5 review already fixed one real leak: string secret values containing commas used to leak everything after the first comma. Current `ValueEnd(...)` is JSON-string-aware and `RedactedEvidenceFormatter_Format_RedactsSecretValuesContainingCommas` pins comma handling. [Source: `_bmad-output/implementation-artifacts/7-5-testing-library-bunit-host-and-deterministic-fakes.md#Senior Developer Review (AI)`; `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs`]
- Current redaction tests cover tenant/user replacement, basic token/secret/password keys, oversized payload truncation, and comma-containing secret values. They do not yet form a comprehensive matrix for punctuation-heavy secrets or non-string secret values. [Source: `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs`]
- `TestCommandService` is the only current fake that records serialized payload text, through `CommandDispatchEvidence.RedactedPayload`. `TestQueryService`, `TestProjectionPageLoader`, and `TestFaultInjectionProvider` record structured evidence fields only. [Source: `src/Hexalith.FrontComposer.Testing/TestCommandService.cs`; `src/Hexalith.FrontComposer.Testing/TestQueryService.cs`; `src/Hexalith.FrontComposer.Testing/TestProjectionPageLoader.cs`; `src/Hexalith.FrontComposer.Testing/TestFaultInjectionProvider.cs`]
- The Testing host contract states that evidence records must not log raw command payloads, tenant IDs, user IDs, tokens, secrets, passwords, or external paths in failure messages. [Source: `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md#Evidence And Redaction`]
- `PublicAPI.Shipped.txt` pins the Testing package public surface; record, helper, or public member shape changes intentionally affect the baseline. [Source: `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`; `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`]

### Current Files To Read Before Editing

Read each likely UPDATE file completely before changing it:

- `src/Hexalith.FrontComposer.Testing/Evidence.cs` - redaction implementation and evidence record shapes.
- `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs` - existing host, fake, redaction, Counter, and fault coverage; add focused matrix tests here unless a new test file clearly improves organization.
- `src/Hexalith.FrontComposer.Testing/TestCommandService.cs` - command payload evidence path and bounded queue behavior.
- `src/Hexalith.FrontComposer.Testing/TestQueryService.cs`, `TestProjectionPageLoader.cs`, `TestFaultInjectionProvider.cs` - structured evidence paths to preserve.
- `src/Hexalith.FrontComposer.Testing/FrontComposerTestOptions.cs` - `MaxDiagnosticPayloadCharacters`, test tenant/user defaults, and public API surface.
- `src/Hexalith.FrontComposer.Testing/README.md` and `docs/how-to/test-generated-components.md` - adopter-facing redaction guidance if behavior or examples change.
- `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md` - v1 Testing host/evidence contract if the guarantee changes.
- `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt` and `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` - only if public Testing APIs change or the public baseline must be revalidated.

### Architecture Compliance

- Keep the Testing package a consumer-layer leaf for adopter tests. Do not introduce EventStore, SignalR, DAPR, browser storage, Shell runtime side effects, or network calls into fakes. [Source: `_bmad-output/project-docs/architecture.md#2. Layered structure`; `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md#Scope`]
- Reuse `RedactedEvidenceFormatter`; do not route Testing evidence through CLI `OutputSanitizer` or duplicate a second sanitizer abstraction unless a demonstrated formatter limitation requires a small private helper. [Source: `_bmad-output/implementation-artifacts/7-5-testing-library-bunit-host-and-deterministic-fakes.md#Dev Notes`]
- Preserve public API discipline. Prefer private/internal helpers for implementation details; only update `PublicAPI.Shipped.txt` when an adopter-facing Testing member intentionally changes. [Source: `_bmad-output/project-context.md#Testing Rules`; `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`]
- Use the repo-pinned test stack: xUnit v3, Shouldly, bUnit where rendering is needed, central package versions only, `.slnx` only, and `DiffEngine_Disabled=true` for Verify-backed lanes. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/project-context.md#Testing Rules`]
- Do not modify submodules, package version files, SourceTools, CLI, MCP, Shell runtime, generated `obj/**`, pacts, or docs site output for this story unless a failing Story 10.5 acceptance test proves direct ownership.

### Anti-Patterns To Avoid

- Do not add privacy tests only in an excluded, nightly, performance, quarantined, or e2e-only lane. The redaction matrix must run in the default Testing package lane.
- Do not assert redaction with the same literal as the replacement marker only; tests must prove original sensitive substrings are absent.
- Do not use realistic production secrets, live tokens, or real tenant/user identifiers as fixtures. Use synthetic but adversarial strings.
- Do not over-redact all non-secret fields. Evidence must remain useful for adopter assertions after sensitive values are removed.
- Do not make tests depend on JSON property ordering beyond the formatter's stable observable output needed for the redaction guarantee.
- Do not mark File List or validation tasks complete without mechanical evidence; Story 10.1 will fail stale claims.
- Do not revert the pre-existing dirty `PackageBoundaryTests.cs` Microsoft.NET.Test.Sdk version change; classify it as unrelated unless Story 10.5 intentionally owns it.

### Testing Requirements

- Minimum focused lane: run the Testing test project and include the new redaction matrix in its default lane. Local fallback pattern:
  `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests -noLogo`
- If VSTest is attempted and blocked, record the exact command and `System.Net.Sockets.SocketException (13): Permission denied` or other exact blocker. Do not claim VSTest passed when using the direct xUnit v3 runner.
- Run `PackageBoundaryTests.PublicApi_ExportedTypes_MatchIntentionalBaseline` if public Testing APIs or evidence record shapes change. If no public surface changes, state that `PublicAPI.Shipped.txt` remained unchanged.
- Required artifact gate before review:
  `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-5-testing-evidence-redaction-default-lane-guard.md`
- Required broad lane when feasible:
  `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`

### Latest Technical Information

No external package/API research is required for Story 10.5. Use the repository-pinned stack in `_bmad-output/project-context.md`: .NET SDK `10.0.302`, `System.Text.Json` `10.0.9`, bUnit `2.8.4-preview`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and FluentUI Blazor `5.0.0-rc.3-26138.1`. The risk is local privacy coverage, not stale third-party API knowledge. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]

### Previous Story Intelligence

Story 10.1 implemented the mechanical story evidence gate. The dev agent must run `python3 eng/validate-story-artifacts.py --story <story>` before review and should expect File List and checked-task claims to be mechanically checked. [Source: `_bmad-output/implementation-artifacts/10-1-mechanical-story-evidence-reconciliation.md#Senior Developer Review (AI)`]

Story 10.2 shows the right Epic 10 pattern: audit adopter-facing docs first, update only owned text, and preserve useful provenance instead of sweeping unrelated files. [Source: `_bmad-output/implementation-artifacts/10-2-adopter-facing-historical-label-cleanup.md`]

Story 10.3 shows how to add coverage in an existing default lane without inventing a new schema or broad framework. Apply that same focused-assertion discipline to Testing evidence redaction. [Source: `_bmad-output/implementation-artifacts/10-3-cli-text-output-parity-guard.md`]

Story 10.4 reinforced that local environment blockers must be recorded precisely and focused direct xUnit fallback evidence is acceptable only as local evidence, with CI remaining authoritative for blocked VSTest/solution lanes. [Source: `_bmad-output/implementation-artifacts/10-4-hfcm9002-production-emission-decision.md#Dev Agent Record`]

Story 7.5 is the primary brownfield source. It introduced and reviewed the Testing package host/evidence contract, and the review caught the comma-containing secret leak. Do not regress that fix while expanding the punctuation matrix. [Source: `_bmad-output/implementation-artifacts/7-5-testing-library-bunit-host-and-deterministic-fakes.md`]

### Git Intelligence

Recent commits show the immediate Epic 10 sequence:

- `e13a502 test(story-10.4): pin HFCM9002 production decision`
- `4ca6bbd feat(story-10.3): CLI text-output parity guard`
- `832a022 docs(story-10.2): clean adopter historical labels`
- `86b6a92 feat(story-10.1): reconcile story review evidence`
- `88f0342 docs(story-9.2): document accepted submodule pointer updates`

Current worktree at story creation has unrelated dirty Shell/docs/CI/sample files plus a pre-existing one-line change in `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` from `Microsoft.NET.Test.Sdk` `18.3.0` to `18.7.0` inside the clean-consumer fixture. Treat these as pre-existing unless Story 10.5 deliberately edits them; do not revert them. [Source: `git status --short`; `git diff -- tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`]

### Project Structure Notes

- Story file location: `_bmad-output/implementation-artifacts/10-5-testing-evidence-redaction-default-lane-guard.md`.
- Sprint-status key: `10-5-testing-evidence-redaction-default-lane-guard`.
- Primary implementation file if a leak is found: `src/Hexalith.FrontComposer.Testing/Evidence.cs`.
- Primary test file: `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs`.
- Public API baseline: `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`.
- Testing package docs: `src/Hexalith.FrontComposer.Testing/README.md`, `docs/how-to/test-generated-components.md`, and `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md`.
- Avoid generated `docs/_site/**`, `obj/**`, submodules, package version files, and unrelated Shell/CLI/MCP/SourceTools changes.

### References

- Source: `_bmad-output/planning-artifacts/epics.md` - Epic 10 and Story 10.5 source of record.
- Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-epic-7-retro-follow-through.md` - approved Epic 7 follow-through proposal and redaction action.
- Source: `_bmad-output/implementation-artifacts/epic-7-retro-2026-06-05.md` - E7-AI-5 action and privacy learning.
- Source: `_bmad-output/implementation-artifacts/7-5-testing-library-bunit-host-and-deterministic-fakes.md` - original Testing package story and comma-secret review fix.
- Source: `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md` - Testing host/evidence/redaction v1 contract.
- Source: `_bmad-output/implementation-artifacts/10-1-mechanical-story-evidence-reconciliation.md` - mechanical evidence gate.
- Source: `_bmad-output/project-context.md` - project stack, coding, testing, docs, and submodule rules.
- Source: `_bmad-output/project-docs/architecture.md` - layered architecture and Testing consumer placement.
- Source: `src/Hexalith.FrontComposer.Testing/Evidence.cs` - current evidence record and redaction implementation.
- Source: `src/Hexalith.FrontComposer.Testing/TestCommandService.cs` - command payload evidence path.
- Source: `src/Hexalith.FrontComposer.Testing/TestQueryService.cs` - query evidence path.
- Source: `src/Hexalith.FrontComposer.Testing/TestProjectionPageLoader.cs` - page-loader evidence path.
- Source: `src/Hexalith.FrontComposer.Testing/TestFaultInjectionProvider.cs` - fault evidence path.
- Source: `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs` - existing Testing default-lane coverage.
- Source: `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` - public API and package boundary tests.
- Source: `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt` - authoritative public API baseline.
- Source: `src/Hexalith.FrontComposer.Testing/README.md` and `docs/how-to/test-generated-components.md` - adopter-facing Testing guidance.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-05: Create-story analysis loaded root Hexalith LLM instructions, BMAD create-story workflow/config/discovery/template/checklist, project context, sprint status, Epic 10 source, Epic 7 follow-through proposal and retro, Story 7.5, Stories 10.1-10.4, Testing source/tests/docs/contract, project architecture, recent git history, and current git status.
- 2026-07-05: Discovery loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; no planning-artifact PRD, architecture, or UX files matched the workflow patterns, so `_bmad-output/project-context.md`, `_bmad-output/project-docs/*`, contracts, previous stories, and live Testing source/tests supplied project context.
- 2026-07-05: Confirmed `sprint-status.yaml` had Epic 10 in progress, Stories 10.1-10.4 done, and Story 10.5 in `backlog` before story creation.
- 2026-07-05: Validated story context against the create-story checklist by adding concrete Testing UPDATE files, current redaction facts, prior comma-secret leak intelligence, anti-patterns, default-lane test requirements, public API guardrails, and current dirty-worktree classification.
- 2026-07-05: Dev-story audit read the Testing evidence implementation, command/query/page-loader/fault fakes, FrontComposerTestHostTests, Testing README, adopter how-to, Testing host contract, PublicAPI baseline, and PackageBoundaryTests dirty diff before editing. Classified the PackageBoundaryTests Microsoft.NET.Test.Sdk 18.3.0 -> 18.7.0 fixture version change as pre-existing and unrelated.
- 2026-07-05: Audit evidence paths/symbols read before edit: `src/Hexalith.FrontComposer.Testing/Evidence.cs`, `RedactedEvidenceFormatter.Format`, `src/Hexalith.FrontComposer.Testing/TestCommandService.cs`, `TestQueryService.cs`, `TestProjectionPageLoader.cs`, `TestFaultInjectionProvider.cs`, `src/Hexalith.FrontComposer.Testing/README.md`, `docs/how-to/test-generated-components.md`, `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md`, and `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`.
- 2026-07-05: RED evidence: `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests -method Hexalith.FrontComposer.Testing.Tests.FrontComposerTestHostTests.RedactedEvidenceFormatter_Format_RedactsSecretKeysCaseInsensitivelyAcrossValueShapes -method Hexalith.FrontComposer.Testing.Tests.FrontComposerTestHostTests.TestCommandService_Dispatch_RedactsCommandPayloadEvidenceThroughFormatter -noLogo` failed 2/2 before the formatter change because nested object values under secret-like keys leaked `object-secret-b` and `nested-secret-b`.
- 2026-07-05: GREEN focused evidence: `dotnet build tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release -m:1 /nr:false` passed 0 warnings/0 errors, then direct xUnit focused redaction lane passed 6/6. Covered `...<truncated>` oversized evidence and `CommandDispatchEvidence.RedactedPayload` command payload evidence.
- 2026-07-05: Default Testing lane evidence: `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests -noLogo` passed 29/29, including `PackageBoundaryTests.PublicApi_ExportedTypes_MatchIntentionalBaseline`.
- 2026-07-05: Broad validation attempt `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` was blocked before test execution during restore by `NU1900` warning-as-error because vulnerability data could not be fetched: `Permission denied (api.nuget.org:443)`.
- 2026-07-05: Broad no-restore fallback `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --no-restore --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined" -m:1 /nr:false` was blocked inside VSTest by `System.Net.Sockets.SocketException (13): Permission denied` when creating the TCP listener; one CLI test project still reported `NU1900` despite `--no-restore`. CI remains authoritative for the broad solution lane.
- 2026-07-05: Story artifact validation evidence: `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/10-5-testing-evidence-redaction-default-lane-guard.md --base e13a50257c6ba78fc65882950fa9aadec793c441` passed after documenting unrelated dirty files and reconciling story-owned changes.
- 2026-07-05: QA generate E2E tests workflow loaded the skill checklist, confirmed Story 10.5 has no HTTP API endpoint or browser UI workflow, and treated the existing Testing-host xUnit lane as the applicable end-to-end automation surface for the Testing package.
- 2026-07-05: QA validation evidence: `DiffEngine_Disabled=true dotnet build tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release --no-restore -m:1 /nr:false` passed with 0 warnings/0 errors, and `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests -noLogo` passed 29/29.

### Completion Notes List

- Story context created by BMAD create-story workflow on 2026-07-05.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Replaced delimiter-based evidence redaction with a JSON DOM traversal in `RedactedEvidenceFormatter` so token/secret/password property values are redacted structurally across strings, numbers, booleans, nulls, nested objects, and arrays before truncation.
- Preserved configured tenant/user replacement in non-secret string values and kept literal `<tenant>`, `<user>`, and `<redacted>` markers in emitted JSON.
- Added default-lane Testing coverage for nested tenant/user values, case-insensitive token/secret/password keys, punctuation-heavy secrets, oversized payload truncation after redaction, benign non-secret assertion values, and `TestCommandService` command payload evidence.
- Verified query, page-loader, and fault fakes record structured evidence fields only and do not add raw payload text.
- No public Testing helper, evidence record shape, adopter-facing guidance, or documented redaction guarantee changed. `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`, `PublicAPI.Shipped.txt`, `src/Hexalith.FrontComposer.Testing/README.md`, `docs/how-to/test-generated-components.md`, and `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md` are intentionally unchanged.
- BMAD QA generate E2E tests workflow completed for Story 10.5. No additional test-code gaps were found beyond the missing workflow summary artifact; `_bmad-output/implementation-artifacts/tests/test-summary.md` now records the Story 10.5 coverage, validation commands, and checklist result.

## Documented Unrelated Changes

These paths were dirty before Story 10.5 creation and are unrelated to the create-story artifact work. They are documented for the Story 10.1 mechanical evidence gate and are intentionally not modified by this story.

- `.github/workflows/ci.yml` - Unrelated CI workflow drift; outside Testing evidence redaction story creation.
- `_bmad-output/story-automator/orchestration-9-20260704-182122.md` - Unrelated story-automator orchestration state.
- `docs/reference/pact-contracts.md` - Unrelated pact-contract documentation drift.
- `samples/Counter/Counter.Web/Program.cs` - Unrelated Counter sample host drift.
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnFilterCell.razor` - Unrelated Shell UI change.
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnFilterCell.razor.cs` - Unrelated Shell UI code-behind change.
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcProjectionGlobalSearch.razor` - Unrelated Shell UI change.
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcProjectionGlobalSearch.razor.cs` - Unrelated Shell UI code-behind change.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor` - Unrelated Shell toolbar change.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor.cs` - Unrelated Shell toolbar code-behind change.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageToolbarTests.cs` - Unrelated Shell toolbar test change.
- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` - Pre-existing clean-consumer fixture package-version change; Story 10.5 must audit before owning further edits.

### File List

- `_bmad-output/implementation-artifacts/10-5-testing-evidence-redaction-default-lane-guard.md` - Story 10.5 status, task, file list, and validation evidence updates.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` - BMAD QA generate E2E tests summary for Story 10.5.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - Story 10.5 status moved through implementation workflow.
- `src/Hexalith.FrontComposer.Testing/Evidence.cs` - structural JSON redaction for sensitive evidence values.
- `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs` - default-lane redaction matrix and command evidence coverage.
- `RedactedEvidenceFormatter.Format` - named exception: audited and changed inside `src/Hexalith.FrontComposer.Testing/Evidence.cs`.
- `src/Hexalith.FrontComposer.Testing/TestCommandService.cs` - named exception: audited unchanged command evidence path.
- `src/Hexalith.FrontComposer.Testing/TestQueryService.cs` - named exception: audited unchanged structured query evidence path.
- `src/Hexalith.FrontComposer.Testing/TestProjectionPageLoader.cs` - named exception: audited unchanged structured page-loader evidence path.
- `src/Hexalith.FrontComposer.Testing/TestFaultInjectionProvider.cs` - named exception: audited unchanged structured fault evidence path.
- `src/Hexalith.FrontComposer.Testing/README.md` - named exception: audited unchanged adopter-facing guidance.
- `docs/how-to/test-generated-components.md` - named exception: audited unchanged adopter-facing guidance.
- `_bmad-output/contracts/fc-testing-library-host-contract-2026-06-05.md` - named exception: audited unchanged Testing host contract.
- `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt` - named exception: audited unchanged public API baseline.
- `...<truncated>` - named exception: output marker verified by focused redaction test evidence.
- `CommandDispatchEvidence.RedactedPayload` - named exception: evidence property verified by command dispatch test.
- `PackageBoundaryTests.PublicApi_ExportedTypes_MatchIntentionalBaseline` - named exception: public API baseline test passed in the default Testing lane.

### Change Log

- 2026-07-05: Created Story 10.5 context and moved sprint status to ready-for-dev.
- 2026-07-05: Implemented structural Testing evidence redaction and default-lane privacy matrix coverage.
- 2026-07-05: Validated Story 10.5 artifacts and moved status to review.
- 2026-07-05: Executed BMAD QA generate E2E tests workflow and recorded Story 10.5 test automation summary.
- 2026-07-05: Senior Developer Review (AI) fixed a tenant/user property-name redaction regression, added the property-name redaction pin (30/30 default lane), re-ran the artifact validator (exit 0), and moved status to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot
**Date:** 2026-07-05
**Outcome:** Changes Requested → fixed automatically during review → Approved.

### Scope validated

- Read every story-owned file in the File List against git reality: `src/Hexalith.FrontComposer.Testing/Evidence.cs` and `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs` are the only modified source/test files attributable to this story; the remaining File List entries are `named exception` audit markers and the two documented tracking artifacts (`test-summary.md`, `sprint-status.yaml`). All match `git status`.
- Confirmed AC2 is satisfied by omission: no public Testing helper, evidence record shape, README/how-to guidance, or contract text changed. `PackageBoundaryTests.PublicApi_ExportedTypes_MatchIntentionalBaseline` passes, so `PublicAPI.Shipped.txt` is intentionally unchanged.
- Re-ran `python3 eng/validate-story-artifacts.py --story ...` → exit 0 (mechanical reconciliation gate passed) both before and after the fix.
- Independently reproduced the dev evidence: Release build 0 warnings / 0 errors; direct xUnit v3 lane `tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests -noLogo` initially 29/29, then 30/30 after the review fix. VSTest/solution lanes remain socket-blocked locally; CI stays authoritative for the broad lane, matching the dev record.

### Findings

**[HIGH → fixed] Tenant/user IDs leaked through JSON property names (privacy regression).**
The dev rewrite replaced whole-payload `string.Replace` with a structural `RedactNode` walk that only ran `RedactConfiguredValues` on string *values*, never on property *names*. The previous implementation redacted the configured tenant/user IDs anywhere in the serialized payload, including object keys. Result: a command/evidence payload containing a `Dictionary<string,…>` keyed by the tenant or user ID (or any object whose property name is the configured ID) leaked the raw identifier — a direct violation of the Testing host contract "must not log … tenant IDs, user IDs" guarantee this story exists to strengthen.

- Proof (RED): `RedactedEvidenceFormatter.Format(new { ByTenant = new Dictionary<string,string> { ["tenant-keyed"] = "…" } }, …)` emitted `{"ByTenant":{"tenant-keyed":"…"}}` — raw `tenant-keyed` present.
- Fix (`src/Hexalith.FrontComposer.Testing/Evidence.cs`): keep the structural secret-key (`token`/`secret`/`password`) redaction, but apply the configured tenant/user replacement once over the whole redacted JSON string in `Format` (restoring the pre-rewrite guarantee for keys and values), then truncate. Removed the now-redundant per-node string replacement and the unused `options` parameter on `RedactNode`.
- Regression test (`FrontComposerTestHostTests.cs`): `RedactedEvidenceFormatter_Format_RedactsConfiguredTenantAndUserInPropertyNames` — dictionary keyed by tenant/user ID; asserts raw IDs absent, `<tenant>`/`<user>` markers present, benign label preserved. GREEN after fix; full lane 30/30.

### Considered and intentionally not changed

- **Substring key over-redaction** (e.g. `TokenCount` fully redacted): conservative-by-design and identical to the pre-rewrite behavior; erring toward redaction is correct for a privacy guard. No change.
- **Empty `TestTenantId`/`TestUserId` would throw in `string.Replace`**: pre-existing (the old whole-payload replace had the same behavior) and the defaults (`test-tenant`/`test-user`) are non-empty. Out of scope for this story. No change.
- **External-path redaction** named in the contract: not part of Story 10.5's ACs (which enumerate tenant/user/token/secret/password/oversized/punctuation cases only). No change.
- **`Microsoft.NET.Test.Sdk` 18.3.0 → 18.7.0** inside the `PackageBoundaryTests` clean-consumer fixture string: correctly classified as pre-existing/unrelated and left untouched per the story's documented-unrelated set.

### Result

1 HIGH issue fixed in code + covered by a new default-lane regression test. 0 CRITICAL issues remain. Mechanical reconciliation gate green. Status → done.
