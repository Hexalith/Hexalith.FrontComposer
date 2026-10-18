---
title: '13.7 [I · UX-F] Provide Reusable UX Assertions for Adopters'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-design.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Adopter test engineers have no deterministic Testing-package helpers for the canonical UX matrices (UX-AM-1, VR-1, FM-1, OF-1, SS-1, AE-1), so downstream modules need app-specific selectors and machine-dependent waits to verify generated failure and recovery UX.

**Approach:** Extend `Hexalith.FrontComposer.Testing` (fake time, redacted evidence, assertions) with message-sequence, coalescing-boundary, channel-distinction, origin-capture, unobscured-focus geometry, timing-constant and AE-evidence helpers, each exercised by a consumer test against a realistic failure/policy state.

## Boundaries & Constraints

**Always:** Extend existing `Evidence.cs` / `RedactedEvidenceFormatter` / `Assertions.cs` / `FrontComposerTestOptions.TimeProvider`; never duplicate. Identify messages by AM row ID plus canonical copy. Role/name assertions first; `data-testid` only supplements. Evidence redacted by default (no token, JWT, tenant payload, stack trace, raw backend metadata, PII). Update `PublicAPI.Shipped.txt` baseline for any public API change. Re-seal analyzer test-identifier inventory in `analyzer-policy-exception-ledger-v1.json` (count + sha256) for added underscore test names.

**Decisions:** (1) Helpers live in the .NET Testing package only: pure assertions over caller-supplied measured data (geometry rects, AX snapshots) plus fake-time helpers; no browser dependency and no new TypeScript distribution. (2) Ship a minimal in-package fake `TimeProvider`; no `Microsoft.Extensions.TimeProvider.Testing` reference.

**Never:** Close OI-16, G-4, FLUENT-APP-1 or Product approval. Re-seal `AnalyzerPolicy_GovernanceContract_FailsClosed` drift (GOV-1 owned). Change Shell runtime behavior or constants. Use real-time waits.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Sequence match | Ordered [AM-11, AM-13], count 2, one operation | Passes | N/A |
| Sequence drift | Extra, missing, reordered or duplicate message | Fails naming AM row and channel | Redacted diff |
| Coalescing | Advance 249ms then 250ms | Nothing at 249; last eligible at 250; stale discarded | Fails on early/late emit |
| Terminal cancel | Terminal outcome / focused summary / nav failure while pending | Pending cancelled, immediate announce | Fails if intermediate emitted |
| Channel misuse | One event hits live region and focus | Fails | Names wrong channel |
| Origin removed | Origin removed/disabled before close | Asserts route-h1 (FM-04/05) or form-heading (FM-11) fallback, never body | Fails on body |
| Time boundary | 1,999/2,000ms, 119,999/120,000ms | N-1 not fired, N fired | Fails on drift |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Testing/Evidence.cs` -- `RedactedEvidenceFormatter` (l.50-70), extend for AE evidence.
- `src/Hexalith.FrontComposer.Testing/Assertions.cs` -- add new assertion classes alongside.
- `src/Hexalith.FrontComposer.Testing/TestFaultEvidenceRecorder.cs` -- recorder pattern to copy.
- `src/Hexalith.FrontComposer.Testing/FrontComposerTestOptions.cs:33`, `FrontComposerTestHostBuilder.cs:215` -- TimeProvider seam.
- `src/Hexalith.FrontComposer.Testing/PublicAPI.Shipped.txt`; `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs:21` -- baseline check.
- `src/Hexalith.FrontComposer.Shell/Services/Announcements/SurfaceAnnouncementCoordinator.cs` (250ms, l.8) -- behavior under test; `FreshRowAnnouncementLedger.cs:11` (10s).
- `src/Hexalith.FrontComposer.Shell/Options/FcShellOptions.cs` -- 2000/3000/15s/1000/120000 constants; `ProjectionHubRetryPolicy.cs:9` (30000, internal: duplicate + drift test); `FcLifecycleWrapper.razor.cs:26` (10000).
- `_bmad-output/planning-artifacts/ux-design.md` l.144-163 (AM channels, coalescing), 164-196 (AM rows), 198-332 (VR/FM/OF/SS/AE) -- authority.
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` -- canonical copy; no AM-ID catalog exists, helper defines one resolved from resources.
- `tests/e2e/helpers/responsive-accessibility.ts` (`focusGeometry` l.536, `expectTargetSizes` l.257) -- reference semantics only.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` l.98-104 -- identifier seal.
- Docs: `src/Hexalith.FrontComposer.Testing/README.md`, `docs/how-to/test-generated-components.md`.

## Tasks & Acceptance

**Execution:**
- [ ] `Testing/` -- add fake clock + timing-constant set with N-1/N helpers -- deterministic time
- [ ] `Testing/` -- AM message-sequence, coalescing-boundary, channel-distinction assertions -- AM-01..31
- [ ] `Testing/` -- origin-capture and unobscured-focus geometry assertions -- OF/FM/AE-05
- [ ] `Testing/Evidence.cs` -- AE-01..09 redacted evidence recorder -- extend, not duplicate
- [ ] `Testing.Tests/` -- consumer tests on unmapped rejection, authorization denial, offline, polling exhaustion; drift test for constants vs Shell
- [ ] `PublicAPI.Shipped.txt`, ledger seal, README/how-to -- baseline, reseal, docs

**Acceptance Criteria:**
- Given the epics.md Story 13.7 criteria, when the Testing suite runs, then every helper passes against realistic failure states and fails on seeded drift.
- Given evidence output, when inspected, then it contains no token, JWT, tenant payload, stack trace or PII.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet test tests/Hexalith.FrontComposer.Testing.Tests` -- expected: pass
- `dotnet build src/Hexalith.FrontComposer.Testing -c Release` -- expected: 0 warnings/errors
